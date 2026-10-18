using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class SubsystemCoalCampfireBlockBehavior : SubsystemBlockBehavior, IUpdateable, IDrawable
{
	private SubsystemTime m_subsystemTime;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemWeather m_subsystemWeather;

	private SubsystemAmbientSounds m_subsystemAmbientSounds;

	private Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell;

	private float m_fireSoundVolume;

	private Random m_random;

	private int m_updateIndex;

	private List<Point3> m_toReduce;

	private SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	private Dictionary<Point3, NaturalSmokeParticleSystem> m_smokeParticleSystemsByCell;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemSmokeAccumulation m_subsystemSmokeAccumulation;

	public Dictionary<Point3, int> m_stoneCounts = new Dictionary<Point3, int>();

	public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

	private Dictionary<Point3, LightSmoother> m_lightSmoothers = new Dictionary<Point3, LightSmoother>();

	public Dictionary<Point3, FireParticleSystem>.KeyCollection Campfires => m_particleSystemsByCell.Keys;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[0];

	public int[] DrawOrders => new int[1] { 100 };

	public void Update(float dt)
	{
		foreach (Point3 key in m_particleSystemsByCell.Keys)
		{
			if (!m_lightSmoothers.TryGetValue(key, out var value))
			{
				value = new LightSmoother();
				m_lightSmoothers[key] = value;
			}
			value.Update(dt, new Vector3(key.X, key.Y, key.Z), m_subsystemTerrain, Project);
		}
		if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0))
		{
			m_updateIndex++;
			foreach (Point3 key2 in m_particleSystemsByCell.Keys)
			{
				IgniteSurroundingBlocks(key2.X, key2.Y, key2.Z, 1f);
			}
			m_toReduce.Clear();
		}
		if (Time.PeriodicEvent(0.5, 0.0))
		{
			float num = float.MaxValue;
			foreach (Point3 key3 in m_particleSystemsByCell.Keys)
			{
				float x = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(key3.X, key3.Y, key3.Z));
				num = MathUtils.Min(num, x);
			}
			m_fireSoundVolume = m_subsystemAmbientSounds.SubsystemAudio.CalculateVolume(MathUtils.Sqrt(num), 2f);
		}
		m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_fireSoundVolume);
		foreach (Point3 key4 in m_particleSystemsByCell.Keys)
		{
			if (!m_smokeParticleSystemsByCell.ContainsKey(key4))
			{
				int campfireSize = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(key4.X, key4.Y, key4.Z));
				AddSmokeParticleSystem(key4.X, key4.Y, key4.Z, campfireSize);
				continue;
			}
			NaturalSmokeParticleSystem naturalSmokeParticleSystem = m_smokeParticleSystemsByCell[key4];
			if (naturalSmokeParticleSystem.IsStopped)
			{
				naturalSmokeParticleSystem.IsStopped = false;
			}
		}
		if (m_subsystemSmokeAccumulation == null || m_subsystemWeather == null)
		{
			return;
		}
		foreach (Point3 key5 in m_particleSystemsByCell.Keys)
		{
			int num2 = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(key5.X, key5.Y, key5.Z));
			if (num2 > 0)
			{
				int yLimit = m_subsystemWeather.GetPrecipitationShaftInfo(key5.X, key5.Z).YLimit;
				int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(key5.X, key5.Z);
				if (key5.Y < topHeight || !IsSkyOpen(key5.X, key5.Y, key5.Z, yLimit))
				{
					float num3 = MathUtils.Lerp(0.8f, 1.1f, MathUtils.Saturate((float)num2 / 15f));
					new Vector3((float)key5.X + 0.5f, (float)key5.Y + num3, (float)key5.Z + 0.5f);
					MathUtils.Lerp(0.25f, 1f, MathUtils.Saturate((float)num2 / 15f));
				}
			}
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
		if (BlocksManager.Blocks[cellContents].IsTransparent)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		AddCampfireParticleSystem(value, x, y, z);
		int campfireSize = Terrain.ExtractData(value);
		AddSmokeParticleSystem(x, y, z, campfireSize);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveCampfireParticleSystem(x, y, z);
		RemoveSmokeParticleSystem(x, y, z);
		DropStones(x, y, z);
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		RemoveCampfireParticleSystem(x, y, z);
		if (num > 0)
		{
			AddCampfireParticleSystem(value, x, y, z);
		}
		if (m_smokeParticleSystemsByCell.TryGetValue(new Point3(x, y, z), out var value2))
		{
			value2.ParticleSize = MathUtils.Lerp(0.1f, 0.5f, (float)num / 15f);
			value2.SpawnRateMultiplier = MathUtils.Lerp(0.1f, 1f, (float)num / 15f);
			value2.VerticalSpeed = MathUtils.Lerp(0.5f, 1.5f, (float)num / 15f);
			value2.Position = new Vector3((float)x + 0.5f, (float)y + MathUtils.Lerp(0.8f, 1.1f, (float)num / 15f), (float)z + 0.5f);
			value2.RandomOffsetEnabled = num == 0;
		}
		else
		{
			AddSmokeParticleSystem(x, y, z, num);
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		AddCampfireParticleSystem(value, x, y, z);
		int campfireSize = Terrain.ExtractData(value);
		AddSmokeParticleSystem(x, y, z, campfireSize);
	}

	public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
	{
		if (!worldItem.ToRemove)
		{
			int x = cellFace.X;
			int y = cellFace.Y;
			int z = cellFace.Z;
			int value = worldItem.Value;
			int count = ((!(worldItem is Pickable pickable)) ? 1 : pickable.Count);
			if (AddColdItem(x, y, z, value, count))
			{
				worldItem.ToRemove = true;
			}
			else if (AddFuel(x, y, z, value, count))
			{
				worldItem.ToRemove = true;
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int x = raycastResult.CellFace.X;
		int y = raycastResult.CellFace.Y;
		int z = raycastResult.CellFace.Z;
		int activeBlockValue = componentMiner.ActiveBlockValue;
		if (AddColdItem(x, y, z, activeBlockValue, 1))
		{
			componentMiner.RemoveActiveTool(1);
			return true;
		}
		if (AddStone(x, y, z, activeBlockValue))
		{
			componentMiner.RemoveActiveTool(1);
			return true;
		}
		if (AddFuel(x, y, z, activeBlockValue, 1, componentMiner))
		{
			componentMiner.RemoveActiveTool(1);
		}
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemSmokeAccumulation = Project.FindSubsystem<SubsystemSmokeAccumulation>(throwOnError: true);
		if (!valuesDictionary.ContainsKey("StonesData"))
		{
			return;
		}
		foreach (ValuesDictionary value in valuesDictionary.GetValue<ValuesDictionary>("StonesData").Values)
		{
			m_stoneCounts[value.GetValue<Point3>("Pos")] = value.GetValue<int>("Count");
		}
	}

	public void AddCampfireParticleSystem(int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (num > 0)
		{
			Vector3 vector = new Vector3(0.5f, 0.15f, 0.5f);
			float size = MathUtils.Lerp(0.2f, 0.5f, (float)num / 15f);
			FireParticleSystem fireParticleSystem = new FireParticleSystem(new Vector3(x, y, z) + vector, size, 256f);
			m_particleSystemsByCell[new Point3(x, y, z)] = fireParticleSystem;
			if (SettingsManager.FireParticlesFromCampfires)
			{
				m_subsystemParticles.AddParticleSystem(fireParticleSystem);
			}
		}
	}

	public void RemoveCampfireParticleSystem(int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		if (m_particleSystemsByCell.TryGetValue(key, out var value))
		{
			value.IsStopped = true;
			m_particleSystemsByCell.Remove(key);
		}
	}

	public bool ResizeCampfire(int x, int y, int z, int steps, bool playSound)
	{
		int subsystemFloatingBlockBehavior = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int subsystemMovingBlocks = Terrain.ExtractData(subsystemFloatingBlockBehavior);
		if (subsystemMovingBlocks > 0)
		{
			int movingBlockSet = MathUtils.Clamp(subsystemMovingBlocks + steps, 0, 60);
			if (movingBlockSet != subsystemMovingBlocks)
			{
				int value = Terrain.ReplaceData(subsystemFloatingBlockBehavior, movingBlockSet);
				SubsystemTerrain.ChangeCell(x, y, z, value);
				if (playSound)
				{
					if (steps >= 0)
					{
						m_subsystemAmbientSounds.SubsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: false);
					}
					else
					{
						m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
					}
				}
				return true;
			}
		}
		return false;
	}

	public SubsystemCoalCampfireBlockBehavior()
	{
		m_smokeParticleSystemsByCell = new Dictionary<Point3, NaturalSmokeParticleSystem>();
		m_particleSystemsByCell = new Dictionary<Point3, FireParticleSystem>();
		m_random = new Random();
		m_toReduce = new List<Point3>();
	}

	public void IgniteSurroundingBlocks(int x, int y, int z, float fireExpandability)
	{
		for (int i = -3; i <= 3; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				for (int k = -3; k <= 3; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						m_subsystemFireBlockBehavior.SetCellOnFire(x + i, y + j, z + k, fireExpandability);
					}
				}
			}
		}
	}

	public void RemoveSmokeParticleSystem(int x, int y, int z)
	{
		if (m_smokeParticleSystemsByCell.TryGetValue(new Point3(x, y, z), out var value))
		{
			value.IsStopped = true;
			m_smokeParticleSystemsByCell.Remove(new Point3(x, y, z));
		}
	}

	public void AddSmokeParticleSystem(int x, int y, int z, int campfireSize)
	{
		if (SettingsManager.SmokeFromBothCampfires)
		{
			float particleSize = ((campfireSize > 0) ? MathUtils.Lerp(0.1f, 0.5f, (float)campfireSize / 15f) : 0.1f);
			float spawnRateMultiplier = ((campfireSize > 0) ? 1f : 0.05f);
			float num = ((campfireSize > 0) ? MathUtils.Lerp(0.8f, 1.1f, (float)campfireSize / 15f) : 0.1f);
			Vector3 position = new Vector3((float)x + 0.5f, (float)y + num, (float)z + 0.5f);
			float maxVisibilityDistance = 256f;
			NaturalSmokeParticleSystem naturalSmokeParticleSystem = new NaturalSmokeParticleSystem(m_subsystemTerrain, m_subsystemWeather, position, maxVisibilityDistance)
			{
				Position = position,
				Direction = new Vector3(0f, 1f, 0f),
				ParticleSize = particleSize,
				SpawnRateMultiplier = spawnRateMultiplier,
				Color = Color.White,
				VerticalSpeed = ((campfireSize > 0) ? MathUtils.Lerp(0.5f, 1.5f, (float)campfireSize / 15f) : 0.2f)
			};
			if (campfireSize == 0)
			{
				naturalSmokeParticleSystem.RandomOffsetEnabled = true;
			}
			m_subsystemParticles.AddParticleSystem(naturalSmokeParticleSystem);
			m_smokeParticleSystemsByCell[new Point3(x, y, z)] = naturalSmokeParticleSystem;
		}
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> list = new List<Point3>();
		foreach (Point3 key in m_particleSystemsByCell.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				list.Add(key);
			}
		}
		foreach (Point3 item in list)
		{
			RemoveCampfireParticleSystem(item.X, item.Y, item.Z);
			RemoveSmokeParticleSystem(item.X, item.Y, item.Z);
		}
	}

	public bool AddFuel(int x, int y, int z, int value, int count, ComponentMiner miner = null)
	{
		if (Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) > 0)
		{
			int num = Terrain.ExtractContents(value);
			if (num == 150)
			{
				if (miner != null && miner.ComponentCreature != null && miner.ComponentCreature.PlayerAchievements != null)
				{
					PlayerAchievements playerAchievements = miner.ComponentCreature.PlayerAchievements;
					playerAchievements.UselessDeal++;
					if (playerAchievements.UselessDeal == 1)
					{
						playerAchievements.EnqueueUselessDealAchievement();
					}
				}
				return ResizeCampfire(x, y, z, 5, playSound: true);
			}
			Block block = BlocksManager.Blocks[num];
			if (Project.FindSubsystem<SubsystemExplosions>(throwOnError: true).TryExplodeBlock(x, y, z, value))
			{
				return true;
			}
			if (block is SnowBlock || block is SnowballBlock || block is IceBlock || block is LargeSnowBlock || block is IcySnowBlock)
			{
				return ResizeCampfire(x, y, z, -1, playSound: true);
			}
			if (block.FuelHeatLevel > 0f)
			{
				float num2 = (float)count * MathUtils.Min(block.FuelFireDuration, 20f) / 5f;
				int num3 = (int)num2;
				float num4 = num2 - (float)num3;
				if (m_random.Float(0f, 1f) < num4)
				{
					num3++;
				}
				if (num3 > 0)
				{
					return ResizeCampfire(x, y, z, num3, playSound: true);
				}
				return true;
			}
		}
		return false;
	}

	private bool IsSkyOpen(int x, int y, int z, int yLimit)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		for (int i = y + 1; i <= yLimit; i++)
		{
			int num = Terrain.ExtractContents(terrain.GetCellValue(x, i, z));
			if (BlocksManager.Blocks[num].IsCollidable)
			{
				return false;
			}
		}
		return true;
	}

	public bool AddStone(int x, int y, int z, int value)
	{
		if (Terrain.ExtractContents(value) != 79)
		{
			return false;
		}
		Point3 key = new Point3(x, y, z);
		int value2 = 0;
		m_stoneCounts.TryGetValue(key, out value2);
		if (value2 < 8)
		{
			m_stoneCounts[key] = value2 + 1;
			m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Impacts/Stone", 1f, 0f, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), 3f, autoDelay: false);
			return true;
		}
		return false;
	}

	private void DropStones(int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		if (m_stoneCounts.TryGetValue(key, out var value))
		{
			Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
			Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(Terrain.MakeBlockValue(79), value, position, null, null);
			m_stoneCounts.Remove(key);
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		foreach (KeyValuePair<Point3, int> inventoryById2 in m_stoneCounts)
		{
			Point3 inventoryById3 = inventoryById2.Key;
			Vector3 package = new Vector3((float)inventoryById3.X + 0.5f, (float)inventoryById3.Y + 0.5f, (float)inventoryById3.Z + 0.5f);
			if (m_subsystemTerrain.Terrain.GetChunkAtCell(inventoryById3.X, inventoryById3.Z) != null && Vector3.DistanceSquared(camera.ViewPosition, package) <= 3600f && Vector3.Dot(camera.ViewDirection, Vector3.Normalize(package - camera.ViewPosition)) >= 0.3f)
			{
				int mainPlayer = inventoryById2.Value;
				float componentChest = MathUtils.Clamp((float)Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(inventoryById3.X, inventoryById3.Y, inventoryById3.Z)) / 15f, 0f, 1f);
				float componentCraftingTable = MathUtils.Lerp(0.35f, 0.75f, componentChest);
				for (int componentStrawBasket = 0; componentStrawBasket < mainPlayer; componentStrawBasket++)
				{
					float componentHumanBones = (float)componentStrawBasket * ((float)Math.PI / 4f);
					float componentFurnace = (float)inventoryById3.X + 0.5f + (float)Math.Cos(componentHumanBones) * componentCraftingTable;
					float componentFurnace2 = (float)inventoryById3.Z + 0.5f + (float)Math.Sin(componentHumanBones) * componentCraftingTable;
					float componentDispenser = (float)inventoryById3.Y + 0.05f;
					Matrix barrel = Matrix.CreateFromYawPitchRoll((float)((inventoryById3.GetHashCode() + componentStrawBasket * 333) % 100) / 100f * ((float)Math.PI * 2f), 0f, 0f) * Matrix.CreateTranslation(new Vector3(componentFurnace, componentDispenser, componentFurnace2));
					DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData();
					drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
					drawBlockEnvironmentData.Light = (m_lightSmoothers.TryGetValue(inventoryById3, out var value) ? value.SmoothedLightInt : m_subsystemTerrain.Terrain.GetCellLight(inventoryById3.X, inventoryById3.Y, inventoryById3.Z));
					BlocksManager.Blocks[79].DrawBlock(m_primitivesRenderer, 79, Color.White, 0.4f, ref barrel, drawBlockEnvironmentData);
				}
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		int num = 0;
		foreach (KeyValuePair<Point3, int> stoneCount in m_stoneCounts)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary3.SetValue("Pos", stoneCount.Key);
			valuesDictionary3.SetValue("Count", stoneCount.Value);
			valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
			num++;
		}
		valuesDictionary.SetValue("StonesData", valuesDictionary2);
	}

	public bool AddColdItem(int x, int y, int z, int value, int count)
	{
		if (Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) > 0)
		{
			switch (Terrain.ExtractContents(value))
			{
			case 62:
				return ResizeCampfire(x, y, z, -60, playSound: true);
			case 61:
			case 85:
			case 348:
			case 350:
			case 352:
				return ResizeCampfire(x, y, z, -count, playSound: true);
			}
		}
		return false;
	}
}
