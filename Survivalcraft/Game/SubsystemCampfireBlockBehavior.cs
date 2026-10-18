using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class SubsystemCampfireBlockBehavior : SubsystemBlockBehavior, IUpdateable, IDrawable
{
	private struct PendingDrop
	{
		public Vector3 Position;

		public int Value;

		public int Count;
	}

	private SubsystemTime m_subsystemTime;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemWeather m_subsystemWeather;

	private SubsystemAmbientSounds m_subsystemAmbientSounds;

	private Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell;

	private float m_fireSoundVolume;

	private Random m_random;

	private int m_updateIndex;

	private List<Point3> m_toReduce;

	private Dictionary<Point3, float> m_cookingTimers;

	private Dictionary<Point3, int> m_rawFoodCounts;

	private Dictionary<int, int> rawToCookedFoodMap;

	private SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	private float m_lastSmokeTime;

	private static readonly float[] FireHeightTable;

	private SubsystemTerrain m_subsystemTerrain;

	private Dictionary<Point3, NaturalSmokeParticleSystem> m_smokeParticleSystemsByCell;

	private SubsystemSmokeAccumulation m_subsystemSmokeAccumulation;

	public PrimitivesRenderer3D m_primitivesRenderer;

	private List<PendingDrop> m_pendingDrops;

	public SubsystemGameInfo m_subsystemGameInfo;

	public Dictionary<Point3, double> m_rawFoodAddTimes;

	public Dictionary<Point3, int> m_stoneCounts;

	private Dictionary<Point3, LightSmoother> m_lightSmoothers;

	private Dictionary<Point3, WaterDripsParticleSystem> m_fatDripSystems = new Dictionary<Point3, WaterDripsParticleSystem>();

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
		if (m_pendingDrops.Count > 0)
		{
			foreach (PendingDrop pendingDrop in m_pendingDrops)
			{
				Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(pendingDrop.Value, pendingDrop.Count, pendingDrop.Position, null, null);
			}
			m_pendingDrops.Clear();
		}
		if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0))
		{
			foreach (Point3 key2 in m_particleSystemsByCell.Keys)
			{
				IgniteSurroundingBlocks(key2.X, key2.Y, key2.Z, 1f);
			}
			m_updateIndex++;
			foreach (Point3 key3 in m_particleSystemsByCell.Keys)
			{
				if (m_subsystemTerrain.Terrain.GetChunkAtCell(key3.X, key3.Z) == null)
				{
					continue;
				}
				int value2 = 0;
				m_stoneCounts.TryGetValue(key3, out value2);
				bool flag = false;
				if (m_updateIndex % 5 == 0)
				{
					float num = (float)value2 / 16f;
					if (m_random.Float(0f, 1f) >= num)
					{
						flag = true;
					}
				}
				PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(key3.X, key3.Z);
				if (precipitationShaftInfo.Intensity > 0f && key3.Y >= precipitationShaftInfo.YLimit - 1)
				{
					float num2 = (float)value2 / 8f;
					if (m_random.Float(0f, 1f) >= num2)
					{
						float lightningIntensity = m_subsystemWeather.m_lightningIntensity;
						float num3;
						if (lightningIntensity > 0.1f && lightningIntensity < 0.5f)
						{
							num3 = 1f;
						}
						else
						{
							num3 = ((lightningIntensity >= 0.5f) ? 3f : 0.2f);
						}
						for (int i = 0; (float)i < num3; i++)
						{
							m_toReduce.Add(key3);
						}
						flag = false;
					}
				}
				float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
				if (currentWindStrength > 9f)
				{
					Vector3 position = new Vector3((float)key3.X + 0.5f, (float)key3.Y + 0.5f, (float)key3.Z + 0.5f);
					float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(position);
					if (windExposureFactor > 0f)
					{
						float num4 = (float)value2 / 8f;
						if (m_random.Float(0f, 1f) >= num4 && m_random.Float(0f, 1f) < windExposureFactor)
						{
							int num5 = ((!(currentWindStrength > 13f)) ? 1 : 2);
							for (int j = 0; j < num5; j++)
							{
								m_toReduce.Add(key3);
							}
							flag = false;
						}
					}
				}
				if (flag)
				{
					m_toReduce.Add(key3);
				}
			}
			foreach (Point3 item in m_toReduce)
			{
				ResizeCampfire(item.X, item.Y, item.Z, -1, playSound: true);
			}
			m_toReduce.Clear();
		}
		foreach (Point3 key4 in m_particleSystemsByCell.Keys)
		{
			if (!m_smokeParticleSystemsByCell.ContainsKey(key4))
			{
				if (m_subsystemTerrain.Terrain.GetChunkAtCell(key4.X, key4.Z) != null)
				{
					int campfireSize = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(key4.X, key4.Y, key4.Z));
					AddSmokeParticleSystem(key4.X, key4.Y, key4.Z, campfireSize);
				}
			}
			else
			{
				NaturalSmokeParticleSystem naturalSmokeParticleSystem = m_smokeParticleSystemsByCell[key4];
				if (naturalSmokeParticleSystem.IsStopped)
				{
					naturalSmokeParticleSystem.IsStopped = false;
				}
			}
		}
		if (m_subsystemSmokeAccumulation != null && m_subsystemWeather != null)
		{
			foreach (Point3 key5 in m_particleSystemsByCell.Keys)
			{
				if (m_subsystemTerrain.Terrain.GetChunkAtCell(key5.X, key5.Z) == null)
				{
					continue;
				}
				int num6 = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(key5.X, key5.Y, key5.Z));
				if (num6 > 0)
				{
					int yLimit = m_subsystemWeather.GetPrecipitationShaftInfo(key5.X, key5.Z).YLimit;
					int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(key5.X, key5.Z);
					if (key5.Y < topHeight || !IsSkyOpen(key5.X, key5.Y, key5.Z, yLimit))
					{
						float num7 = MathUtils.Lerp(0.8f, 1.1f, (float)num6 / 15f);
						Vector3 position2 = new Vector3((float)key5.X + 0.5f, (float)key5.Y + num7, (float)key5.Z + 0.5f);
						float intensity = MathUtils.Lerp(0.25f, 1f, MathUtils.Saturate((float)num6 / 15f));
						m_subsystemSmokeAccumulation.ReportSmokeAtScaled(position2, intensity, 0.5f);
					}
				}
			}
		}
		UpdateCookingTimers(dt);
		if (Time.PeriodicEvent(0.5, 0.0))
		{
			float num8 = float.MaxValue;
			float num9 = 0f;
			foreach (Point3 key6 in m_particleSystemsByCell.Keys)
			{
				if (m_subsystemTerrain.Terrain.GetChunkAtCell(key6.X, key6.Z) != null)
				{
					float x = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(key6.X, key6.Y, key6.Z));
					num8 = MathUtils.Min(num8, x);
					int num10 = Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(key6.X, key6.Y, key6.Z));
					num9 = MathUtils.Max(num9, num10);
				}
			}
			float num11 = num9 / 10f;
			m_fireSoundVolume = m_subsystemAmbientSounds.SubsystemAudio.CalculateVolume(MathUtils.Sqrt(num8), 2f) * num11;
		}
		m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_fireSoundVolume);
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		if (BlocksManager.Blocks[SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z)].IsTransparent)
		{
			DropRawItemIfCooking(x, y, z);
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		int campfireSize = Terrain.ExtractData(value);
		AddCampfireParticleSystem(value, x, y, z);
		AddSmokeParticleSystem(x, y, z, campfireSize);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveCampfireParticleSystem(x, y, z);
		RemoveSmokeParticleSystem(x, y, z);
		DropRawItemIfCooking(x, y, z);
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
		int campfireSize = Terrain.ExtractData(value);
		AddCampfireParticleSystem(value, x, y, z);
		AddSmokeParticleSystem(x, y, z, campfireSize);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> componentBody = new List<Point3>();
		foreach (Point3 y in m_particleSystemsByCell.Keys)
		{
			if (y.X >= chunk.Origin.X && y.X < chunk.Origin.X + 16 && y.Z >= chunk.Origin.Y && y.Z < chunk.Origin.Y + 16)
			{
				componentBody.Add(y);
			}
		}
		foreach (Point3 cellValue in componentBody)
		{
			RemoveCampfireParticleSystem(cellValue.X, cellValue.Y, cellValue.Z);
			RemoveSmokeParticleSystem(cellValue.X, cellValue.Y, cellValue.Z);
			RemoveFatDrips(cellValue);
		}
	}

	public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
	{
		if (!worldItem.ToRemove)
		{
			int componentFourLeggedModel = cellFace.X;
			int componentFlightlessBirdModel = cellFace.Y;
			int z = cellFace.Z;
			int value = worldItem.Value;
			int count = 1;
			if (worldItem is Pickable pickable)
			{
				count = pickable.Count;
			}
			if (AddColdItem(componentFourLeggedModel, componentFlightlessBirdModel, z, value, count))
			{
				worldItem.ToRemove = true;
			}
			else if (AddFuel(componentFourLeggedModel, componentFlightlessBirdModel, z, value, count))
			{
				worldItem.ToRemove = true;
			}
			else if (AddRawFood(componentFourLeggedModel, componentFlightlessBirdModel, z, value, count))
			{
				worldItem.ToRemove = true;
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int timeOfDay = raycastResult.CellFace.X;
		int flag = raycastResult.CellFace.Y;
		int flag2 = raycastResult.CellFace.Z;
		Point3 flag3 = new Point3(timeOfDay, flag, flag2);
		if (m_rawFoodCounts.TryGetValue(flag3, out var flag4))
		{
			int key = Terrain.ExtractContents(flag4);
			if (!rawToCookedFoodMap.ContainsKey(key))
			{
				Vector3 position = new Vector3((float)timeOfDay + 0.5f, (float)flag + 1f, (float)flag2 + 0.5f);
				m_pendingDrops.Add(new PendingDrop
				{
					Value = flag4,
					Count = 1,
					Position = position
				});
				m_rawFoodCounts.Remove(flag3);
				m_cookingTimers.Remove(flag3);
				m_rawFoodAddTimes.Remove(flag3);
				RemoveFatDrips(flag3);
				m_subsystemAmbientSounds.SubsystemAudio.PlaySound("Audio/PickableCollected", 0.5f, 0f, position, 2f, autoDelay: true);
				return true;
			}
		}
		int activeBlockValue = componentMiner.ActiveBlockValue;
		int num = Terrain.ExtractContents(activeBlockValue);
		bool num2 = BlocksManager.Blocks[num].FuelHeatLevel > 0f || num == 150;
		bool flag5 = rawToCookedFoodMap.ContainsKey(num);
		bool flag6 = num == 79;
		bool flag7 = num == 85 || num == 61 || num == 348 || num == 350 || num == 62 || num == 352;
		if (num2 | flag5 | flag6 | flag7)
		{
			if (AddColdItem(timeOfDay, flag, flag2, activeBlockValue, 1) || AddFuel(timeOfDay, flag, flag2, activeBlockValue, 1, componentMiner) || AddRawFood(timeOfDay, flag, flag2, activeBlockValue) || AddStone(timeOfDay, flag, flag2, activeBlockValue))
			{
				componentMiner.RemoveActiveTool(1);
			}
			return true;
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemSmokeAccumulation = Project.FindSubsystem<SubsystemSmokeAccumulation>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		if (!valuesDictionary.ContainsKey("CampfiresData"))
		{
			return;
		}
		foreach (ValuesDictionary value2 in valuesDictionary.GetValue<ValuesDictionary>("CampfiresData").Values)
		{
			Point3 value = value2.GetValue<Point3>("Pos");
			if (value2.ContainsKey("FoodVal"))
			{
				m_rawFoodCounts[value] = value2.GetValue<int>("FoodVal");
				m_cookingTimers[value] = value2.GetValue("CookTime", 10f);
				if (value2.ContainsKey("AddTime"))
				{
					m_rawFoodAddTimes[value] = value2.GetValue<double>("AddTime");
				}
			}
			if (value2.ContainsKey("Stones"))
			{
				m_stoneCounts[value] = value2.GetValue<int>("Stones");
			}
		}
	}

	public void AddCampfireParticleSystem(int value, int x, int y, int z)
	{
		RemoveCampfireParticleSystem(x, y, z);
		int flag = Terrain.ExtractData(value);
		if (flag > 0)
		{
			FireParticleSystem timeOfDay = new FireParticleSystem(new Vector3(x, y, z) + new Vector3(0.5f, 0.15f, 0.5f), MathUtils.Lerp(0.2f, 0.5f, (float)flag / 15f), 256f);
			m_particleSystemsByCell[new Point3(x, y, z)] = timeOfDay;
			if (SettingsManager.FireParticlesFromCampfires)
			{
				m_subsystemParticles.AddParticleSystem(timeOfDay);
			}
		}
	}

	public void RemoveCampfireParticleSystem(int x, int y, int z)
	{
		if (m_particleSystemsByCell.TryGetValue(new Point3(x, y, z), out var value))
		{
			value.IsStopped = true;
			m_particleSystemsByCell.Remove(new Point3(x, y, z));
			m_lightSmoothers.Remove(new Point3(x, y, z));
		}
	}

	public bool ResizeCampfire(int x, int y, int z, int steps, bool playSound)
	{
		int restParams = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractData(restParams);
		int num2 = MathUtils.Clamp(num + steps, 0, 15);
		if (num != num2)
		{
			int value = Terrain.ReplaceData(restParams, num2);
			SubsystemTerrain.ChangeCell(x, y, z, value);
			if (num2 == 0)
			{
				DropRawItemIfCooking(x, y, z);
				DropStones(x, y, z);
			}
			if (playSound)
			{
				if (steps > 0)
				{
					m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Impacts/Wood", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: false);
				}
				else if (steps < 0 && num2 == 0)
				{
					m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
				}
				else if (steps < 0)
				{
					m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
				}
			}
			return true;
		}
		return false;
	}

	public SubsystemCampfireBlockBehavior()
	{
		m_lightSmoothers = new Dictionary<Point3, LightSmoother>();
		m_stoneCounts = new Dictionary<Point3, int>();
		m_rawFoodAddTimes = new Dictionary<Point3, double>();
		m_pendingDrops = new List<PendingDrop>();
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_particleSystemsByCell = new Dictionary<Point3, FireParticleSystem>();
		m_random = new Random();
		m_toReduce = new List<Point3>();
		m_cookingTimers = new Dictionary<Point3, float>();
		m_rawFoodCounts = new Dictionary<Point3, int>();
		rawToCookedFoodMap = new Dictionary<int, int>
		{
			{ 161, 162 },
			{ 77, 78 },
			{ 88, 89 },
			{ 333, 330 },
			{ 336, 331 }
		};
		m_smokeParticleSystemsByCell = new Dictionary<Point3, NaturalSmokeParticleSystem>();
	}

	private void DropRawItemIfCooking(int x, int y, int z)
	{
		Point3 speed = new Point3(x, y, z);
		RemoveFatDrips(speed);
		if (!m_rawFoodCounts.TryGetValue(speed, out var value))
		{
			return;
		}
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f) + new Vector3(m_random.Float(-1f, 1f), 0.5f, m_random.Float(-1f, 1f));
		if (rawToCookedFoodMap.ContainsKey(Terrain.ExtractContents(value)) && m_rawFoodAddTimes.TryGetValue(speed, out var value2))
		{
			double num = m_subsystemGameInfo.TotalElapsedGameTime - value2;
			float itemDurabilityMultiplier = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
			int num2 = (int)(num / 60.0 * (double)itemDurabilityMultiplier);
			if (num2 > 0)
			{
				int num3 = Terrain.ExtractContents(value);
				Block block = BlocksManager.Blocks[num3];
				int rotPeriod = block.GetRotPeriod(value);
				if (rotPeriod > 0)
				{
					int damage = block.GetDamage(value);
					int num4 = num2 / rotPeriod;
					if (num4 == 0 && num2 > rotPeriod / 2)
					{
						num4 = 1;
					}
					int num5 = damage + num4;
					value = ((num5 <= 1) ? block.SetDamage(value, num5) : BlocksManager.DamageItem(value, num4));
				}
			}
		}
		m_cookingTimers.Remove(speed);
		m_rawFoodAddTimes.Remove(speed);
		m_rawFoodCounts.Remove(speed);
		m_pendingDrops.Add(new PendingDrop
		{
			Value = value,
			Count = 1,
			Position = position
		});
	}

	private void UpdateCookingTimers(float dt)
	{
		foreach (Point3 item in new List<Point3>(m_rawFoodCounts.Keys))
		{
			int x = item.X;
			int y = item.Y;
			int z = item.Z;
			if (SubsystemTerrain.Terrain.GetChunkAtCell(x, z) == null)
			{
				continue;
			}
			int num = Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z));
			if (num < 6)
			{
				DropRawItemIfCooking(x, y, z);
				continue;
			}
			int num2 = Terrain.ExtractContents(m_rawFoodCounts[item]);
			bool flag = rawToCookedFoodMap.ContainsKey(num2);
			if (flag)
			{
				float num3 = ((num > 0 && num < FireHeightTable.Length) ? FireHeightTable[num] : 0f);
				Vector3 position = new Vector3((float)x + 0.5f, (float)y + num3 + 0.25f, (float)z + 0.5f);
				if (!m_fatDripSystems.ContainsKey(item))
				{
					WaterDripsParticleSystem waterDripsParticleSystem = new WaterDripsParticleSystem(m_subsystemTerrain, m_subsystemWeather, position, isBlockSource: true);
					waterDripsParticleSystem.UseHangingPhase = true;
					waterDripsParticleSystem.DripRateMultiplier = 0.8f;
					m_subsystemParticles.AddParticleSystem(waterDripsParticleSystem);
					m_fatDripSystems[item] = waterDripsParticleSystem;
				}
				else
				{
					m_fatDripSystems[item].Position = position;
				}
			}
			else
			{
				RemoveFatDrips(item);
			}
			if (m_random.Float(0f, 1f) < 2f * dt)
			{
				float num4 = ((num > 0 && num < FireHeightTable.Length) ? FireHeightTable[num] : 0f);
				float y2 = (float)y + num4 + 0.2f;
				Vector3 position2 = new Vector3((float)x + 0.5f, y2, (float)z + 0.5f);
				Vector3 direction = new Vector3(m_random.Float(-0.01f, 0.01f), 0.02f, m_random.Float(-0.01f, 0.01f));
				GunSmokeParticleSystem particleSystem = new GunSmokeParticleSystem(m_subsystemTerrain, position2, direction)
				{
					ParticleSize = 0.2f,
					SpawnRateMultiplier = 0.1f,
					m_direction = direction
				};
				m_subsystemParticles.AddParticleSystem(particleSystem);
			}
			if (!m_cookingTimers.ContainsKey(item))
			{
				continue;
			}
			m_cookingTimers[item] -= dt;
			if (m_cookingTimers[item] <= 0f)
			{
				if (flag)
				{
					int contents = rawToCookedFoodMap[num2];
					m_rawFoodCounts[item] = Terrain.MakeBlockValue(contents);
					m_cookingTimers[item] = m_random.Float(10f, 20f);
					m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
					RemoveFatDrips(item);
				}
				else if (num2 != 79)
				{
					m_rawFoodCounts[item] = Terrain.MakeBlockValue(79, 0, StoneChunkBlock.SetKind(0, StoneChunkBlock.StoneKind.BurnedFood));
					m_cookingTimers.Remove(item);
					m_subsystemAmbientSounds.SubsystemAudio.PlaySound("Audio/SizzleLong", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
				}
			}
		}
	}

	static SubsystemCampfireBlockBehavior()
	{
		FireHeightTable = new float[16]
		{
			0f, 0.35f, 0.4f, 0.45f, 0.5f, 0.55f, 0.6f, 0.65f, 0.7f, 0.75f,
			0.8f, 0.85f, 0.9f, 0.95f, 1f, 1.05f
		};
	}

	public void RemoveSmokeParticleSystem(int x, int y, int z)
	{
		if (m_smokeParticleSystemsByCell.TryGetValue(new Point3(x, y, z), out var restParams))
		{
			restParams.IsStopped = true;
			m_smokeParticleSystemsByCell.Remove(new Point3(x, y, z));
		}
	}

	public void AddSmokeParticleSystem(int x, int y, int z, int campfireSize)
	{
		RemoveSmokeParticleSystem(x, y, z);
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

	public bool AddRawFood(int x, int y, int z, int value, int count = 1)
	{
		Point3 key = new Point3(x, y, z);
		int num = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, y, z));
		if (num < 6)
		{
			return false;
		}
		if (rawToCookedFoodMap.ContainsKey(Terrain.ExtractContents(value)) && num > 0)
		{
			DropRawItemIfCooking(x, y, z);
			if (count > 1)
			{
				int contents = Terrain.ExtractContents(value);
				Vector3 position = new Vector3((float)x + 0.5f, (float)y + 1f, (float)z + 0.5f);
				m_pendingDrops.Add(new PendingDrop
				{
					Value = Terrain.MakeBlockValue(contents),
					Count = count - 1,
					Position = position
				});
			}
			m_cookingTimers[key] = m_random.Float(10f, 20f);
			m_rawFoodCounts[key] = value;
			m_rawFoodAddTimes[key] = m_subsystemGameInfo.TotalElapsedGameTime;
			return true;
		}
		return false;
	}

	public bool AddFuel(int x, int y, int z, int value, int count, ComponentMiner miner = null)
	{
		int num = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, y, z));
		if (num <= 0)
		{
			return false;
		}
		int num2 = Terrain.ExtractContents(value);
		if (num2 == 150)
		{
			if (miner != null && miner.ComponentCreature != null && miner.ComponentCreature.PlayerAchievements != null)
			{
				PlayerAchievements playerAchievements = miner.ComponentCreature.PlayerAchievements;
				if (playerAchievements.Master == 0L)
				{
					playerAchievements.UselessDeal++;
					if (playerAchievements.UselessDeal == 1)
					{
						playerAchievements.EnqueueUselessDealAchievement();
					}
				}
			}
			int value2 = Terrain.ReplaceData(Terrain.MakeBlockValue(354), 15);
			m_subsystemTerrain.ChangeCell(x, y, z, value2);
			return true;
		}
		Block block = BlocksManager.Blocks[num2];
		if (Project.FindSubsystem<SubsystemExplosions>(throwOnError: true).TryExplodeBlock(x, y, z, value))
		{
			return true;
		}
		if (num >= 1 && num <= 5 && block.FuelFireDuration >= 10f)
		{
			ResizeCampfire(x, y, z, -num, playSound: true);
			if (miner != null && miner.ComponentCreature != null && miner.ComponentCreature.PlayerAchievements != null)
			{
				PlayerAchievements playerAchievements2 = miner.ComponentCreature.PlayerAchievements;
				playerAchievements2.FailedStoker++;
				if (playerAchievements2.FailedStoker == 1)
				{
					playerAchievements2.EnqueueFailedStokerAchievement();
				}
			}
			return true;
		}
		if (block.FuelHeatLevel <= 0f)
		{
			return false;
		}
		float num3 = MathUtils.Min(block.FuelFireDuration, 20f);
		int num4 = (int)((float)count * num3 / 5f);
		if (m_random.Float(0f, 1f) < (float)count * num3 % 5f)
		{
			num4++;
		}
		if (num4 > 0)
		{
			bool flag = ResizeCampfire(x, y, z, num4, playSound: true);
			if (flag && miner != null)
			{
				IgniteSurroundingBlocks(x, y, z, 1f, miner);
			}
			return flag;
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

	public void Draw(Camera camera, int drawOrder)
	{
		foreach (KeyValuePair<Point3, int> rawFoodCount in m_rawFoodCounts)
		{
			Point3 key = rawFoodCount.Key;
			Vector3 vector = new Vector3((float)key.X + 0.5f, (float)key.Y + 0.5f, (float)key.Z + 0.5f);
			if (m_subsystemTerrain.Terrain.GetChunkAtCell(key.X, key.Z) != null && Vector3.DistanceSquared(camera.ViewPosition, vector) <= 3600f && Vector3.Dot(camera.ViewDirection, Vector3.Normalize(vector - camera.ViewPosition)) >= 0.3f)
			{
				int value = rawFoodCount.Value;
				int num = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z));
				float num2 = ((num > 0 && num < FireHeightTable.Length) ? FireHeightTable[num] : 0f);
				num2 += 0.1f;
				Vector3 position = new Vector3((float)key.X + 0.5f, (float)key.Y + num2, (float)key.Z + 0.5f);
				Matrix matrix = Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(MathUtils.DegToRad(45f)) * Matrix.CreateTranslation(position);
				DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData();
				drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
				drawBlockEnvironmentData.InWorldMatrix = matrix;
				drawBlockEnvironmentData.Light = (m_lightSmoothers.TryGetValue(key, out var value2) ? value2.SmoothedLightInt : m_subsystemTerrain.Terrain.GetCellLight(key.X, key.Y, key.Z));
				drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(key.X, key.Z);
				drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(key.X, key.Z);
				BlocksManager.Blocks[Terrain.ExtractContents(value)].DrawBlock(m_primitivesRenderer, value, Color.White, 0.35f, ref matrix, drawBlockEnvironmentData);
			}
		}
		foreach (KeyValuePair<Point3, int> stoneCount in m_stoneCounts)
		{
			Point3 key2 = stoneCount.Key;
			Vector3 vector2 = new Vector3((float)key2.X + 0.5f, (float)key2.Y + 0.5f, (float)key2.Z + 0.5f);
			if (m_subsystemTerrain.Terrain.GetChunkAtCell(key2.X, key2.Z) != null && Vector3.DistanceSquared(camera.ViewPosition, vector2) <= 3600f && Vector3.Dot(camera.ViewDirection, Vector3.Normalize(vector2 - camera.ViewPosition)) >= 0.3f)
			{
				int value3 = stoneCount.Value;
				float f = MathUtils.Clamp((float)Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(key2.X, key2.Y, key2.Z)) / 15f, 0f, 1f);
				float num3 = MathUtils.Lerp(0.35f, 0.65f, f);
				for (int i = 0; i < value3; i++)
				{
					float num4 = (float)i * ((float)Math.PI / 4f);
					float x = (float)key2.X + 0.5f + (float)Math.Cos(num4) * num3;
					float z = (float)key2.Z + 0.5f + (float)Math.Sin(num4) * num3;
					float y = (float)key2.Y + 0.05f;
					Vector3 position2 = new Vector3(x, y, z);
					int num5 = key2.GetHashCode() + i * 333;
					float yaw = (float)(num5 % 100) / 100f * ((float)Math.PI * 2f);
					float pitch = (float)((num5 >> 2) % 20) / 100f;
					float roll = (float)((num5 >> 4) % 20) / 100f;
					Matrix matrix2 = Matrix.CreateFromYawPitchRoll(yaw, pitch, roll) * Matrix.CreateTranslation(position2);
					BlocksManager.Blocks[79].DrawBlock(environmentData: new DrawBlockEnvironmentData
					{
						Light = (m_lightSmoothers.TryGetValue(key2, out var value4) ? value4.SmoothedLightInt : m_subsystemTerrain.Terrain.GetCellLight(key2.X, key2.Y, key2.Z))
					}, primitivesRenderer: m_primitivesRenderer, value: 79, color: Color.White, size: 0.4f, matrix: ref matrix2);
				}
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public bool AddStone(int x, int y, int z, int value)
	{
		if (Terrain.ExtractContents(value) != 79 || StoneChunkBlock.GetKind(Terrain.ExtractData(value)) != StoneChunkBlock.StoneKind.Normal)
		{
			return false;
		}
		Point3 key = new Point3(x, y, z);
		if (Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, y, z)) <= 0)
		{
			return false;
		}
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
			m_pendingDrops.Add(new PendingDrop
			{
				Value = Terrain.MakeBlockValue(79),
				Count = value,
				Position = position
			});
			m_stoneCounts.Remove(key);
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		int num = 0;
		HashSet<Point3> hashSet = new HashSet<Point3>();
		foreach (Point3 key in m_rawFoodCounts.Keys)
		{
			hashSet.Add(key);
		}
		foreach (Point3 key2 in m_stoneCounts.Keys)
		{
			hashSet.Add(key2);
		}
		foreach (Point3 item in hashSet)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary3.SetValue("Pos", item);
			if (m_rawFoodCounts.ContainsKey(item))
			{
				valuesDictionary3.SetValue("FoodVal", m_rawFoodCounts[item]);
				if (m_cookingTimers.ContainsKey(item))
				{
					valuesDictionary3.SetValue("CookTime", m_cookingTimers[item]);
				}
				if (m_rawFoodAddTimes.ContainsKey(item))
				{
					valuesDictionary3.SetValue("AddTime", m_rawFoodAddTimes[item]);
				}
			}
			if (m_stoneCounts.ContainsKey(item))
			{
				valuesDictionary3.SetValue("Stones", m_stoneCounts[item]);
			}
			valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
			num++;
		}
		valuesDictionary.SetValue("CampfiresData", valuesDictionary2);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		OnBlockAdded(value, oldValue, x, y, z);
		if (Terrain.ExtractData(value) > 0)
		{
			IgniteSurroundingBlocks(x, y, z, 1f, miner);
		}
	}

	public void IgniteSurroundingBlocks(int x, int y, int z, float fireExpandability, ComponentMiner miner = null)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						m_subsystemFireBlockBehavior.SetCellOnFire(x + i, y + j, z + k, fireExpandability, miner);
					}
				}
			}
		}
	}

	private void RemoveFatDrips(Point3 key)
	{
		if (m_fatDripSystems.TryGetValue(key, out var value))
		{
			value.IsStopped = true;
			m_fatDripSystems.Remove(key);
		}
	}

	public bool AddColdItem(int x, int y, int z, int value, int count)
	{
		if (Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, y, z)) <= 0)
		{
			return false;
		}
		switch (Terrain.ExtractContents(value))
		{
		case 62:
			ResizeCampfire(x, y, z, -15, playSound: true);
			return true;
		case 61:
		case 85:
		case 348:
		case 350:
		case 352:
			ResizeCampfire(x, y, z, -count, playSound: true);
			return true;
		default:
			return false;
		}
	}
}
