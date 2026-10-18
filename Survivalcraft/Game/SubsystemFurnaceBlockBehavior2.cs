using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemFurnaceBlockBehavior2 : SubsystemBlockBehavior, IUpdateable
{
	private class FurnaceSmokeCache
	{
		public Vector3 Position;

		public float Intensity;

		public bool UseFallback;

		public double NextUpdateTime;
	}

	private SubsystemParticles m_subsystemParticles;

	private SubsystemBlockEntities m_subsystemBlockEntities;

	private Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell;

	private SubsystemAmbientSounds m_subsystemAmbientSounds;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	private SubsystemSmokeAccumulation m_subsystemSmokeAccumulation;

	private Dictionary<Point3, NaturalSmokeParticleSystem> m_smokeParticleSystemsByCell = new Dictionary<Point3, NaturalSmokeParticleSystem>();

	private float m_clayFurnaceSoundVolume;

	public const int ChimneyBlockIndex = 439;

	private SubsystemTime m_subsystemTime;

	private Random m_random = new Random();

	private Dictionary<Point3, FurnaceSmokeCache> m_smokeCache = new Dictionary<Point3, FurnaceSmokeCache>();

	public override int[] HandledBlocks => new int[2] { 303, 304 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		int num = Terrain.ExtractContents(value);
		if ((num == 303 || num == 304) && m_subsystemBlockEntities.GetBlockEntity(x, y, z) == null)
		{
			m_subsystemBlockEntities.CreateBlockEntity("Furnace2", new Point3(x, y, z), miner);
		}
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveFurnaceParticleSystem(x, y, z);
		RemoveFurnaceSmokeParticleSystem(x, y, z);
		if (Terrain.ExtractContents(value) == 304)
		{
			m_clayFurnaceSoundVolume = 0f;
			UpdateFurnaceSoundVolume();
		}
		if (CommonLib.WorkType == WorkType.Client || Terrain.ExtractContents(newValue) == 303 || Terrain.ExtractContents(newValue) == 304)
		{
			return;
		}
		ComponentBlockEntity blockEntity = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(x, y, z);
		if (blockEntity == null)
		{
			return;
		}
		Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
		foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(position);
		}
		SubsystemTerrain.Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
		{
			IPackage package = new BlockEditPackage(new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z), BlockEditPackage.EventType.OpenInventoryByPoint);
			CommonLib.Net.QueuePackage(package);
			return true;
		}
		ComponentBlockEntity blockEntity = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (blockEntity != null && componentMiner.ComponentPlayer != null)
		{
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				ComponentFurnace2 componentFurnace = blockEntity.Entity.FindComponent<ComponentFurnace2>(throwOnError: true);
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget2(componentMiner.Inventory, componentFurnace);
				AudioManager.PlaySound("Audio/FurnaceOpen", 1f, 0f, 0f);
			}
			return true;
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemSmokeAccumulation = Project.FindSubsystem<SubsystemSmokeAccumulation>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public SubsystemFurnaceBlockBehavior2()
	{
		m_particleSystemsByCell = new Dictionary<Point3, FireParticleSystem>();
	}

	public void Update(float dt)
	{
		if (!Time.PeriodicEvent(0.5, 0.0))
		{
			return;
		}
		float num = float.MaxValue;
		float num2 = 0f;
		List<Point3> list = new List<Point3>();
		double gameTime = m_subsystemTime.GameTime;
		foreach (KeyValuePair<Point3, FireParticleSystem> item in m_particleSystemsByCell)
		{
			Point3 key = item.Key;
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z)) != 304)
			{
				list.Add(key);
				continue;
			}
			float num3 = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(key.X, key.Y, key.Z));
			if (num3 <= 50f)
			{
				num2 = Math.Max(num2, 1f - Math.Min(num3 / 50f, 1f));
			}
			num = Math.Min(num, num3);
			if (!m_smokeParticleSystemsByCell.ContainsKey(key))
			{
				AddFurnaceSmokeParticleSystem(key.X, key.Y, key.Z);
			}
			else
			{
				NaturalSmokeParticleSystem naturalSmokeParticleSystem = m_smokeParticleSystemsByCell[key];
				if (naturalSmokeParticleSystem.IsStopped)
				{
					naturalSmokeParticleSystem.IsStopped = false;
				}
			}
			if (m_subsystemSmokeAccumulation != null && m_subsystemWeather != null)
			{
				if (!m_smokeCache.TryGetValue(key, out var value))
				{
					value = new FurnaceSmokeCache();
					m_smokeCache[key] = value;
				}
				if (gameTime >= value.NextUpdateTime)
				{
					value.NextUpdateTime = gameTime + (double)m_random.Float(4f, 6f);
					UpdateSmokeCacheForFurnace(key, value);
				}
				if (value.UseFallback)
				{
					ReportFallbackSmoke(key);
				}
				else if (value.Intensity > 0f)
				{
					m_subsystemSmokeAccumulation.ReportSmokeAtScaled(value.Position, value.Intensity, 0.5f);
				}
			}
		}
		foreach (Point3 item2 in list)
		{
			RemoveFurnaceParticleSystem(item2.X, item2.Y, item2.Z);
			RemoveFurnaceSmokeParticleSystem(item2.X, item2.Y, item2.Z);
			m_smokeCache.Remove(item2);
		}
		m_clayFurnaceSoundVolume = ((num > 50f) ? 0f : num2);
		m_subsystemAmbientSounds.BrickFurnaceSoundVolume = m_clayFurnaceSoundVolume;
	}

	public void AddFurnaceParticleSystem(int x, int y, int z)
	{
		FireParticleSystem fireParticleSystem = new FireParticleSystem(new Vector3((float)x + 0.5f, (float)y + 0.22f, (float)z + 0.5f), 0.28f, 28f);
		m_particleSystemsByCell[new Point3(x, y, z)] = fireParticleSystem;
		if (SettingsManager.FireParticlesInFurnaces)
		{
			m_subsystemParticles.AddParticleSystem(fireParticleSystem);
		}
	}

	public void RemoveFurnaceParticleSystem(int x, int y, int z)
	{
		if (m_particleSystemsByCell.TryGetValue(new Point3(x, y, z), out var value))
		{
			value.IsStopped = true;
			m_subsystemParticles.RemoveParticleSystem(value);
			m_particleSystemsByCell.Remove(new Point3(x, y, z));
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		if (Terrain.ExtractContents(value) == 304)
		{
			AddFurnaceParticleSystem(x, y, z);
		}
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractContents(oldValue);
		if (num == 304 && num2 != 304)
		{
			AddFurnaceParticleSystem(x, y, z);
			if (!m_smokeParticleSystemsByCell.ContainsKey(new Point3(x, y, z)))
			{
				AddFurnaceSmokeParticleSystem(x, y, z);
			}
		}
		else if (num != 304 && num2 == 304)
		{
			RemoveFurnaceParticleSystem(x, y, z);
			RemoveFurnaceSmokeParticleSystem(x, y, z);
		}
	}

	private void UpdateFurnaceSoundVolume()
	{
		float num = float.MaxValue;
		float num2 = 0f;
		foreach (KeyValuePair<Point3, FireParticleSystem> item in m_particleSystemsByCell)
		{
			Point3 key = item.Key;
			float num3 = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(key.X, key.Y, key.Z));
			if (num3 <= 50f)
			{
				num2 = Math.Max(num2, 1f - Math.Min(num3 / 50f, 1f));
			}
			num = Math.Min(num, num3);
		}
		m_clayFurnaceSoundVolume = ((num > 50f) ? 0f : num2);
		m_subsystemAmbientSounds.BrickFurnaceSoundVolume = m_clayFurnaceSoundVolume;
	}

	private void AddFurnaceSmokeParticleSystem(int x, int y, int z)
	{
		if (SettingsManager.SmokeFromBothCampfires)
		{
			Vector3 furnaceFrontOffset = GetFurnaceFrontOffset(x, y, z, 0.505f, 0.9f);
			float maxVisibilityDistance = 160f;
			NaturalSmokeParticleSystem naturalSmokeParticleSystem = new NaturalSmokeParticleSystem(m_subsystemTerrain, m_subsystemWeather, furnaceFrontOffset, maxVisibilityDistance)
			{
				Position = furnaceFrontOffset,
				Direction = new Vector3(0f, 1f, 0f),
				ParticleSize = 0.16f,
				SpawnRateMultiplier = 0.22f,
				Color = Color.White,
				VerticalSpeed = 0.26f,
				RandomOffsetEnabled = false
			};
			m_subsystemParticles.AddParticleSystem(naturalSmokeParticleSystem);
			m_smokeParticleSystemsByCell[new Point3(x, y, z)] = naturalSmokeParticleSystem;
		}
	}

	private void RemoveFurnaceSmokeParticleSystem(int x, int y, int z)
	{
		if (m_smokeParticleSystemsByCell.TryGetValue(new Point3(x, y, z), out var value))
		{
			value.IsStopped = true;
			m_subsystemParticles.RemoveParticleSystem(value);
			m_smokeParticleSystemsByCell.Remove(new Point3(x, y, z));
		}
	}

	private Vector3 GetFurnaceFrontOffset(int x, int y, int z, float distance, float yOffset)
	{
		int num = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, y, z)) & 3;
		Vector3 vector = Vector3.Zero;
		switch (num)
		{
		case 0:
			vector = new Vector3(0f, 0f, 1f);
			break;
		case 1:
			vector = new Vector3(1f, 0f, 0f);
			break;
		case 2:
			vector = new Vector3(0f, 0f, -1f);
			break;
		case 3:
			vector = new Vector3(-1f, 0f, 0f);
			break;
		}
		return new Vector3((float)x + 0.5f, (float)y + yOffset, (float)z + 0.5f) + vector * distance;
	}

	public override bool OnInteract(MovingBlocksRaycastResult raycastResult, Point3 offset, ComponentMiner componentMiner)
	{
		if (!(raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet { AttachedEntities: not null } movingBlockSet))
		{
			return false;
		}
		MovingEntityData movingEntityData = null;
		foreach (MovingEntityData attachedEntity in movingBlockSet.AttachedEntities)
		{
			if (attachedEntity.BlockOffset == offset)
			{
				movingEntityData = attachedEntity;
				break;
			}
		}
		if (movingEntityData == null)
		{
			return false;
		}
		ComponentFurnace2 componentFurnace = movingEntityData.Entity.Entity.FindComponent<ComponentFurnace2>();
		if (componentFurnace == null)
		{
			return false;
		}
		if (componentMiner.ComponentPlayer != null)
		{
			if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
			{
				IPackage package = new MovingBlockEditPackage(componentFurnace);
				CommonLib.Net.QueuePackage(package);
				return true;
			}
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget2(componentMiner.Inventory, componentFurnace);
				AudioManager.PlaySound("Audio/FurnaceOpen", 1f, 0f, 0f);
				return true;
			}
		}
		return true;
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)) == 0)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	private Vector3? GetBestFreeSmokePosition(int x, int y, int z)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		int num = Terrain.ExtractData(terrain.GetCellValue(x, y, z)) & 3;
		Vector3 vector = Vector3.Zero;
		switch (num)
		{
		case 0:
			vector = new Vector3(0f, 0f, 1f);
			break;
		case 1:
			vector = new Vector3(1f, 0f, 0f);
			break;
		case 2:
			vector = new Vector3(0f, 0f, -1f);
			break;
		case 3:
			vector = new Vector3(-1f, 0f, 0f);
			break;
		}
		int x2 = x + (int)vector.X;
		int z2 = z + (int)vector.Z;
		if (!BlocksManager.Blocks[Terrain.ExtractContents(terrain.GetCellValue(x2, y, z2))].IsCollidable)
		{
			return new Vector3((float)x + 0.5f, (float)y + 0.9f, (float)z + 0.5f) + vector * 0.505f;
		}
		if (!BlocksManager.Blocks[Terrain.ExtractContents(terrain.GetCellValue(x, y + 1, z))].IsCollidable)
		{
			return new Vector3((float)x + 0.5f, (float)y + 1.2f, (float)z + 0.5f);
		}
		Vector3[] array = new Vector3[4]
		{
			new Vector3(1f, 0f, 0f),
			new Vector3(-1f, 0f, 0f),
			new Vector3(0f, 0f, 1f),
			new Vector3(0f, 0f, -1f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector2 = array[i];
			if (!(vector2 == vector))
			{
				int x3 = x + (int)vector2.X;
				int z3 = z + (int)vector2.Z;
				if (!BlocksManager.Blocks[Terrain.ExtractContents(terrain.GetCellValue(x3, y, z3))].IsCollidable)
				{
					return new Vector3((float)x + 0.5f, (float)y + 0.9f, (float)z + 0.5f) + vector2 * 0.505f;
				}
			}
		}
		return null;
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (Terrain.ExtractContents(value) == 304)
		{
			AddFurnaceParticleSystem(x, y, z);
			if (!m_smokeParticleSystemsByCell.ContainsKey(new Point3(x, y, z)))
			{
				AddFurnaceSmokeParticleSystem(x, y, z);
			}
		}
	}

	private void ReportFallbackSmoke(Point3 furnacePos)
	{
		Vector3? bestFreeSmokePosition = GetBestFreeSmokePosition(furnacePos.X, furnacePos.Y, furnacePos.Z);
		float intensity = 0.7f;
		if (bestFreeSmokePosition.HasValue)
		{
			m_subsystemSmokeAccumulation.ReportSmokeAtScaled(bestFreeSmokePosition.Value, intensity, 0.5f);
			return;
		}
		Vector3 position = new Vector3((float)furnacePos.X + 0.5f, (float)furnacePos.Y + 0.5f, (float)furnacePos.Z + 0.5f);
		m_subsystemSmokeAccumulation.ReportSmokeAtScaled(position, intensity, 0.5f);
	}

	private void UpdateSmokeCacheForFurnace(Point3 key3, FurnaceSmokeCache cache)
	{
		Terrain subsystemInventories = m_subsystemTerrain.Terrain;
		if (Terrain.ExtractContents(subsystemInventories.GetCellValue(key3.X, key3.Y + 1, key3.Z)) == 439)
		{
			int i;
			for (i = key3.Y + 1; i < 255 && Terrain.ExtractContents(subsystemInventories.GetCellValue(key3.X, i + 1, key3.Z)) == 439; i++)
			{
			}
			int inventoryById2 = Terrain.ExtractContents(subsystemInventories.GetCellValue(key3.X, i + 1, key3.Z));
			if (!BlocksManager.Blocks[inventoryById2].IsCollidable)
			{
				int inventoryById3 = 0;
				for (int package2 = -1; package2 <= 1; package2++)
				{
					for (int mainPlayer = -1; mainPlayer <= 1; mainPlayer++)
					{
						if (Terrain.ExtractContents(subsystemInventories.GetCellValue(key3.X + package2, key3.Y, key3.Z + mainPlayer)) == 304)
						{
							inventoryById3++;
						}
					}
				}
				cache.UseFallback = false;
				cache.Intensity = 0.7f * (float)inventoryById3;
				cache.Position = new Vector3((float)key3.X + 0.5f, (float)i + 1.5f, (float)key3.Z + 0.5f);
			}
			else
			{
				cache.UseFallback = true;
			}
			return;
		}
		bool componentChest = false;
		for (int componentStrawBasket = -1; componentStrawBasket <= 1; componentStrawBasket++)
		{
			for (int componentHumanBones = -1; componentHumanBones <= 1; componentHumanBones++)
			{
				if ((componentStrawBasket != 0 || componentHumanBones != 0) && Terrain.ExtractContents(subsystemInventories.GetCellValue(key3.X + componentStrawBasket, key3.Y + 1, key3.Z + componentHumanBones)) == 439)
				{
					componentChest = true;
					break;
				}
			}
			if (componentChest)
			{
				break;
			}
		}
		if (!componentChest)
		{
			int componentFurnace = subsystemInventories.GetTopHeight(key3.X, key3.Z);
			if (key3.Y < componentFurnace)
			{
				cache.UseFallback = true;
				return;
			}
			cache.UseFallback = false;
			cache.Intensity = 0f;
		}
		else
		{
			cache.UseFallback = false;
			cache.Intensity = 0f;
		}
	}
}
