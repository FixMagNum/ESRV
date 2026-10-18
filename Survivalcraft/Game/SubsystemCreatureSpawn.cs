using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemCreatureSpawn : Subsystem, IUpdateable
{
	public class CreatureType
	{
		public string Name;

		public SpawnLocationType SpawnLocationType;

		public bool RandomSpawn;

		public bool ConstantSpawn;

		public Func<CreatureType, Point3, float> SpawnSuitabilityFunction;

		public Func<CreatureType, Point3, int> SpawnFunction;

		public float SpawnPriority;

		public CreatureType(string name, SpawnLocationType spawnLocationType, bool randomSpawn, bool constantSpawn)
		{
			Name = name;
			SpawnLocationType = spawnLocationType;
			RandomSpawn = randomSpawn;
			ConstantSpawn = constantSpawn;
			SpawnPriority = 1f;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemSpawn m_subsystemSpawn;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemTime m_subsystemTime;

	public SubsystemSky m_subsystemSky;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemGameWidgets m_subsystemViews;

	public Random m_random;

	public List<CreatureType> m_creatureTypes;

	public Dictionary<ComponentCreature, bool> m_creatures;

	public DynamicArray<ComponentBody> m_componentBodies;

	public List<SpawnChunk> m_newSpawnChunks;

	public List<SpawnChunk> m_spawnChunks;

	public static SpawnLocationType[] m_spawnLocations = EnumUtils.GetEnumValues(typeof(SpawnLocationType)).Cast<SpawnLocationType>().ToArray();

	private Dictionary<GameWidget, Dictionary<string, int>> m_genocideCounts;

	private Dictionary<GameWidget, int> m_playerLocalCreatureCounts;

	public int MaxCreaturesPerPlayer;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemPlayers m_subsystemPlayers;

	public int MaxCreaturesInPlayerArea;

	public float PlayerAreaRadius;

	public int SpawnPointMinDistance;

	public int SpawnPointMaxDistance;

	public int AreaLimitGeneral;

	public float AreaLimitRadius;

	public int ChunkLimitConstant;

	public int ChunkLimitNormal;

	public float ChunkRadiusConstant;

	public float ChunkRadiusNormal;

	private Dictionary<string, double> m_lastGenocideLogTimes;

	public Dictionary<ComponentCreature, bool>.KeyCollection Creatures => m_creatures.Keys;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	private bool IsCheaterWorld
	{
		get
		{
			foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
			{
				if (playersDatum.IsCheater)
				{
					return true;
				}
			}
			return false;
		}
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType == WorkType.Client || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return;
		}
		if (IsIntroActive())
		{
			m_newSpawnChunks.Clear();
			m_spawnChunks.Clear();
		}
		else
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
			{
				return;
			}
			GameWidget num = m_subsystemViews.GameWidgets.FirstOrDefault();
			if (num == null)
			{
				return;
			}
			if (m_newSpawnChunks.Count > 0)
			{
				m_newSpawnChunks.RandomShuffle((int max) => m_random.Int(0, max - 1));
				foreach (SpawnChunk newSpawnChunk in m_newSpawnChunks)
				{
					SpawnChunkCreatures(newSpawnChunk, 10, constantSpawn: false, num);
				}
				m_newSpawnChunks.Clear();
			}
			if (m_spawnChunks.Count > 0)
			{
				m_spawnChunks.RandomShuffle((int max) => m_random.Int(0, max - 1));
				foreach (SpawnChunk spawnChunk in m_spawnChunks)
				{
					SpawnChunkCreatures(spawnChunk, 2, constantSpawn: true, num);
				}
				m_spawnChunks.Clear();
			}
			if (m_subsystemTime.PeriodicGameTimeEvent(60.0, 2.0))
			{
				SpawnRandomCreature();
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSpawn = Project.FindSubsystem<SubsystemSpawn>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		InitializeCreatureTypes();
		m_subsystemSpawn.SpawningChunk += (SpawnChunk chunk) =>
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace && !IsIntroActive())
			{
				m_spawnChunks.Add(chunk);
				if (!chunk.IsSpawned)
				{
					m_newSpawnChunks.Add(chunk);
				}
			}
		};
	}

	public override void OnEntityAdded(Entity entity)
	{
		foreach (ComponentCreature oldMemFurniture in entity.FindComponents<ComponentCreature>())
		{
			m_creatures.Add(oldMemFurniture, value: true);
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		foreach (ComponentCreature item in entity.FindComponents<ComponentCreature>())
		{
			m_creatures.Remove(item);
		}
	}

	public void CreateEntity(SpawnEntityData spawnEntityData, ComponentMiner componentMiner, SubsystemAudio subsystemAudio)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			Entity entity = DatabaseManager.CreateEntity(Project, spawnEntityData.TemplateName, throwIfNotFound: true);
			ComponentFrame componentFrame = entity.FindComponent<ComponentFrame>();
			componentFrame.Position = spawnEntityData.Position;
			componentFrame.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
			entity.FindComponent<ComponentSpawn>(throwOnError: true).SpawnDuration = 0f;
			Project.AddEntity(entity);
			componentMiner?.RemoveActiveTool(1);
			subsystemAudio?.PlaySound("Audio/BlockPlaced", 1f, 0f, spawnEntityData.Position, 3f, autoDelay: true);
		}
	}

	public void InitializeCreatureTypes()
	{
		m_creatureTypes.Add(new CreatureType("Duck", SpawnLocationType.Surface, randomSpawn: true, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
				return (humidity <= 8 || temperature <= 4 || num <= 30f || point.Y < topHeight || (!(BlocksManager.Blocks[num2] is LeavesBlock) && num2 != 18 && num2 != 8 && num2 != 2)) ? 0f : 2.5f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Duck", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Raven", SpawnLocationType.Surface, randomSpawn: true, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
				return ((humidity > 8 && temperature > 4) || num <= 30f || point.Y < topHeight || (!(BlocksManager.Blocks[num2] is LeavesBlock) && num2 != 62 && num2 != 8 && num2 != 2 && num2 != 7)) ? 0f : 2.5f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Raven", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Sparrow", SpawnLocationType.Surface, randomSpawn: true, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (temperature > 6 && humidity > 8 && (num == 21 || num == 26 || num == 73 || num == 54 || num == 75 || num == 50 || num == 76 || num == 68 || num == 70 || num == 69 || num == 359 || num == 360 || num == 361)) ? 2f : 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(1, 10);
				return SpawnCreatures(creatureType, "Sparrow", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Pigeon", SpawnLocationType.Surface, randomSpawn: true, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (temperature > 2 && temperature < 6 && humidity > 6 && (num == 21 || num == 26 || num == 73 || num == 54 || num == 75 || num == 50 || num == 76 || num == 68 || num == 70 || num == 69 || num == 359 || num == 360 || num == 361)) ? 2f : 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(1, 10);
				return SpawnCreatures(creatureType, "Pigeon", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Seagull", SpawnLocationType.Surface, randomSpawn: true, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
				return (num <= -100f || num >= 40f || point.Y < topHeight || (num2 != 18 && num2 != 7 && num2 != 6 && num2 != 62)) ? 0f : 2.5f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Seagull", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Wildboar", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || humidity <= 8 || point.Y >= 80 || (num2 != 8 && num2 != 2)) ? 0f : 0.25f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Wildboar", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Brown Cattle", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || humidity <= 4 || temperature < 8 || point.Y >= 70 || (num2 != 8 && num2 != 2)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = m_random.Int(3, 5);
				int num2 = MathUtils.Min(m_random.Int(1, 3), num);
				int count = num - num2;
				return SpawnCreatures(creatureType, "Bull_Brown", point, num2).Count + SpawnCreatures(creatureType, "Cow_Brown", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Black Cattle", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || humidity <= 4 || temperature >= 8 || point.Y >= 70 || (num2 != 8 && num2 != 2)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = m_random.Int(3, 5);
				int num2 = MathUtils.Min(m_random.Int(1, 3), num);
				int count = num - num2;
				return SpawnCreatures(creatureType, "Bull_Black", point, num2).Count + SpawnCreatures(creatureType, "Cow_Black", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("White Bull", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || humidity <= 8 || temperature >= 4 || point.Y >= 70 || (num2 != 8 && num2 != 2)) ? 0f : 0.01f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bull_White", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Snow Wolves", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if ((float)m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z) < 1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					if (num > 20f && point.Y < 100 && (num2 == 62 || num2 == 61 || num2 == 8 || num2 == 2))
					{
						return 0.06f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(1, 3);
				return SpawnCreatures(creatureType, "Wolf_White", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Gray Wolves", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if ((float)m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z) >= 1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					if (num > 40f && humidity >= 8 && point.Y < 100 && (num2 == 8 || num2 == 2))
					{
						return 0.06f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(1, 3);
				return SpawnCreatures(creatureType, "Wolf_Gray", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Coyotes", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || temperature <= 8 || humidity >= 8 || humidity < 2 || point.Y >= 100 || num2 != 7) ? 0f : 0.075f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Wolf_Coyote", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Brown Bears", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || humidity < 4 || temperature < 8 || point.Y >= 110 || (num2 != 8 && num2 != 2 && num2 != 3)) ? 0f : 0.015f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bear_Brown", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Black Bears", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || humidity < 4 || temperature >= 8 || point.Y >= 120 || (num2 != 8 && num2 != 2 && num2 != 3)) ? 0f : 0.015f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bear_Black", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Polar Bears", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 100f || temperature <= 4 || point.Y >= 70 || num2 != 62 || num2 != 61) ? 0f : 0.001f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bear_Polar", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Horses", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 3 || humidity <= 6 || point.Y >= 80 || (num2 != 8 && num2 != 2 && num2 != 3)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num = 0;
				if (m_random.Float(0f, 1f) < 0.35f)
				{
					num += SpawnCreatures(creatureType, "Horse_Black", point, 1).Count;
				}
				if (m_random.Float(0f, 1f) < 0.5f)
				{
					num += SpawnCreatures(creatureType, "Horse_Bay", point, 1).Count;
				}
				if (m_random.Float(0f, 1f) < 0.5f)
				{
					num += SpawnCreatures(creatureType, "Horse_Chestnut", point, 1).Count;
				}
				if (temperature > 8 && m_random.Float(0f, 1f) < 0.3f)
				{
					num += SpawnCreatures(creatureType, "Horse_Palomino", point, 1).Count;
				}
				if (temperature < 8 && m_random.Float(0f, 1f) < 0.3f)
				{
					num += SpawnCreatures(creatureType, "Horse_White", point, 1).Count;
				}
				return num;
			}
		});
		m_creatureTypes.Add(new CreatureType("Camels", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || humidity >= 8 || point.Y >= 80 || num2 != 7) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Camel", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Humpback_Camels", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || humidity >= 8 || point.Y >= 80 || num2 != 7) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Camel_humpback", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Donkeys", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 6 || point.Y >= 120 || (num2 != 8 && num2 != 2 && num2 != 3 && num2 != 7)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Donkey", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Giraffes", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || humidity <= 7 || point.Y >= 75 || (num2 != 8 && num2 != 2 && num2 != 3)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Giraffe", point, m_random.Int(1, 4)).Count
		});
		m_creatureTypes.Add(new CreatureType("Rhinos", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (temperature <= 8 || humidity >= 8 || point.Y >= 75 || num != 7) ? 0f : 0.012f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Rhino", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Tigers", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || humidity <= 8 || point.Y >= 80 || (num2 != 8 && num2 != 2 && num2 != 3 && num2 != 7)) ? 0f : 0.04f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Tiger", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("White Tigers", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || temperature >= 2 || point.Y >= 140 || (num2 != 8 && num2 != 2 && num2 != 3 && num2 != 7 && num2 != 62)) ? 0f : 0.035f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Tiger_White", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Lions", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || temperature <= 8 || point.Y >= 80 || (num2 != 8 && num2 != 2 && num2 != 3 && num2 != 7)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Lion", point, m_random.Int(1, 8)).Count
		});
		m_creatureTypes.Add(new CreatureType("Jaguars", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || humidity <= 8 || temperature <= 8 || point.Y >= 100 || (num2 != 8 && num2 != 2 && num2 != 3 && num2 != 7 && num2 != 12 && (!(BlocksManager.Blocks[num2] is WoodBlock) || m_random.Float(0f, 1f) >= 0.75f))) ? 0f : 0.1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Jaguar", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Leopards", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || temperature <= 8 || point.Y >= 120 || (num2 != 8 && num2 != 2 && num2 != 3 && num2 != 7 && num2 != 12 && (!(BlocksManager.Blocks[num2] is WoodBlock) || m_random.Float(0f, 1f) >= 0.75f))) ? 0f : 0.1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Leopard", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Zebras", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || humidity <= 7 || point.Y >= 80 || (num2 != 8 && num2 != 2 && num2 != 3)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Zebra", point, m_random.Int(1, 20)).Count
		});
		m_creatureTypes.Add(new CreatureType("Gnus", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || point.Y >= 80 || (num2 != 8 && num2 != 2 && num2 != 3)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Gnu", point, m_random.Int(1, 11)).Count
		});
		m_creatureTypes.Add(new CreatureType("Reindeers", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (temperature >= 3 || point.Y >= 90 || (num != 8 && num != 2 && num != 3 && num != 62)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Reindeer", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Mooses", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (temperature >= 7 || point.Y >= 90 || (num != 8 && num != 2 && num != 3 && num != 62)) ? 0f : 0.1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Moose", point, m_random.Int(1, 1)).Count
		});
		m_creatureTypes.Add(new CreatureType("Bisons", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (temperature >= 10 || humidity >= 12 || point.Y >= 80 || (num != 8 && num != 2 && num != 3 && num != 62)) ? 0f : 0.1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bison", point, m_random.Int(1, 4)).Count
		});
		m_creatureTypes.Add(new CreatureType("Ostriches", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || humidity >= 8 || point.Y >= 75 || (num2 != 8 && num2 != 2 && num2 != 7)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Ostrich", point, m_random.Int(1, 2)).Count
		});
		m_creatureTypes.Add(new CreatureType("Cassowaries", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 20f || temperature <= 8 || humidity >= 12 || point.Y >= 75 || (num2 != 8 && num2 != 2 && num2 != 7)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Cassowary", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Hyenas", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num <= 40f || temperature <= 8 || point.Y >= 80 || (num2 != 8 && num2 != 2 && num2 != 7)) ? 0f : 0.05f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(1, 2);
				if (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) > 40f && m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z) > 8 && point.Y < 80 && (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z)) == 8 || Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z)) == 2 || Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z)) == 7) && m_subsystemSky.SkyLightIntensity > 0.5f)
				{
					count = m_random.Int(8, 15);
				}
				return SpawnCreatures(creatureType, "Hyena", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Cave Bears", SpawnLocationType.Cave, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num != 3 && num != 67 && num != 4 && num != 66 && num != 2 && num != 7) ? 0f : 0.5f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = ((m_random.Int(0, 1) == 0) ? "Bear_Black" : "Bear_Brown");
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Cave Tigers", SpawnLocationType.Cave, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num != 3 && num != 67 && num != 4 && num != 66 && num != 2 && num != 7) ? 0f : 0.25f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Tiger", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Cave Lions", SpawnLocationType.Cave, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return ((num != 3 && num != 67 && num != 4 && num != 66 && num != 2 && num != 7) || temperature <= 8 || humidity >= 8) ? 0f : 0.25f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Lion", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Cave Jaguars", SpawnLocationType.Cave, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num != 3 && num != 67 && num != 4 && num != 66 && num != 2 && num != 7) ? 0f : 0.5f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Jaguar", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Cave Leopards", SpawnLocationType.Cave, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return (num != 3 && num != 67 && num != 4 && num != 66 && num != 2 && num != 7) ? 0f : 0.35f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Leopard", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Cave Hyenas", SpawnLocationType.Cave, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				return ((num != 3 && num != 67 && num != 4 && num != 66 && num != 2 && num != 7) || temperature <= 8) ? 0f : 1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Hyena", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Bull Sharks", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) => (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -2f) ? 0.4f : 0f,
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Shark_Bull";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Tiger Sharks", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) => (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -5f) ? 0.3f : 0f,
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Shark_Tiger";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Great White Sharks", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) => (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -20f) ? 0.2f : 0f,
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Shark_GreatWhite";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Barracudas", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) => (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -20f) ? 0.5f : 0f,
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Barracuda";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Bass_Sea", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) => (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -20f) ? 1f : 0f,
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Bass_Sea";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Bass_Freshwater", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				return (num <= 10f || temperature < 4) ? 0f : 1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Bass_Freshwater";
				return SpawnCreatures(creatureType, templateName, point, m_random.Int(1, 2)).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Rays", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) => (m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < 10f) ? 0.5f : 1f,
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int num = 0;
				int num2 = 0;
				for (int i = point.X - 2; i <= point.X + 2; i++)
				{
					for (int j = point.Z - 2; j <= point.Z + 2; j++)
					{
						if (m_subsystemTerrain.Terrain.GetCellContents(point.X, point.Y, point.Z) == 18)
						{
							for (int num3 = point.Y - 1; num3 > 0; num3--)
							{
								switch (m_subsystemTerrain.Terrain.GetCellContents(point.X, num3, point.Z))
								{
								case 2:
									num++;
									break;
								case 7:
									num2++;
									break;
								default:
									continue;
								}
								break;
							}
						}
					}
				}
				string templateName = ((num >= num2) ? "Ray_Brown" : "Ray_Yellow");
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Piranhas", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				return (num <= 10f || humidity < 4 || temperature < 7) ? 0f : 1f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Piranha";
				return SpawnCreatures(creatureType, templateName, point, m_random.Int(2, 4)).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Orcas", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				if (num < -100f)
				{
					return 0.05f;
				}
				return (num >= -20f) ? 0f : 0.01f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Orca";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Capelins", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				return (num < -40f && temperature < 4) ? 0.6f : 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(9, 16);
				return SpawnCreatures(creatureType, "Capelin", point, count).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Belugas", SpawnLocationType.Water, randomSpawn: false, constantSpawn: false)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				if (num < -100f)
				{
					return 0.05f;
				}
				return (num >= -20f) ? 0f : 0.01f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				string templateName = "Beluga";
				return SpawnCreatures(creatureType, templateName, point, 1).Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Constant Werewolves", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnPriority = 9f,
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam && m_random.Float(0f, 1f) > 0.05f)
				{
					return 0f;
				}
				bool isSolarEclipse = m_subsystemSky.IsSolarEclipse;
				float skyLightIntensity = m_subsystemSky.SkyLightIntensity;
				float num = (IsCheaterWorld ? 0.4f : 0.1f);
				if (isSolarEclipse)
				{
					num = 0.5f;
				}
				if (skyLightIntensity >= num)
				{
					return 0f;
				}
				int moonPhase = m_subsystemSky.MoonPhase;
				if (!isSolarEclipse && moonPhase != 0 && moonPhase != 4)
				{
					return 0f;
				}
				float num2 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
				int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
				float num3 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
				int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
				int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
				bool num5 = num3 < 1f && ((num2 > 20f && humidity >= 6) || (num3 <= 8f && point.Y < 90 && cellLightFast <= 7)) && (num4 == 8 || num4 == 2 || num4 == 61 || num4 == 62);
				bool flag = num3 >= 1f && ((num2 > 20f && humidity >= 8) || (num3 <= 8f && point.Y < 90 && cellLightFast <= 7)) && (num4 == 8 || num4 == 2);
				bool flag2 = num2 > 20f && num3 > 8f && humidity < 8 && point.Y < 90 && cellLightFast <= 7 && num4 == 7;
				if (!num5 && !flag && !flag2)
				{
					return 0f;
				}
				if (isSolarEclipse && skyLightIntensity < 0.1f)
				{
					return 4f;
				}
				return isSolarEclipse ? 1f : 2f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) =>
			{
				int count = m_random.Int(1, 2);
				List<Entity> list = SpawnCreatures(creatureType, "Werewolf", point, count);
				foreach (Entity item in list)
				{
					ProcessWerewolfAttributes(item);
				}
				return list.Count;
			}
		});
		m_creatureTypes.Add(new CreatureType("Constant Gray Wolves", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					float num2 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num2 >= 1f && ((num > 20f && humidity >= 8) || (num2 <= 8f && point.Y < 90 && cellLightFast <= 7)) && (num3 == 8 || num3 == 2))
					{
						return 2f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Wolf_Gray", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Snow Wolves", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					float num2 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num2 < 1f && ((num > 20f && humidity >= 6) || (num2 <= 8f && point.Y < 90 && cellLightFast <= 7)) && (num3 == 8 || num3 == 2 || num3 == 61 || num3 == 62))
					{
						return 2f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Wolf_White", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Coyotes", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					float num2 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					float num3 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && num3 > 8f && num2 < 8f && point.Y < 90 && cellLightFast <= 7 && num4 == 7)
					{
						return 2f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Wolf_Coyote", point, m_random.Int(1, 3)).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Brown Bears", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && humidity >= 4 && temperature >= 8 && point.Y < 90 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3))
					{
						return 0.03f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bear_Brown", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Black Bears", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && temperature < 8 && point.Y < 150 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3))
					{
						return 0.0015f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bear_Black", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Polar Bears", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 100f && temperature < 4 && point.Y < 190 && cellLightFast <= 7 && (num2 == 61 || num2 == 62))
					{
						return 0.0001f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Bear_Polar", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Tigers", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && humidity > 8 && point.Y < 90 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3))
					{
						return 0.03f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Tiger", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Lions", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && temperature > 8 && point.Y < 90 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3 || num2 == 7))
					{
						return 0.25f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Lion", point, m_random.Int(1, 2)).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Jaguars", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && temperature > 8 && humidity > 8 && point.Y < 100 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3 || num2 == 12))
					{
						return 0.35f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Jaguar", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Leopards", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && temperature > 8 && point.Y < 110 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3 || num2 == 12))
					{
						return 0.35f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Leopard", point, 1).Count
		});
		m_creatureTypes.Add(new CreatureType("Constant Hyenas", SpawnLocationType.Surface, randomSpawn: false, constantSpawn: true)
		{
			SpawnSuitabilityFunction = (CreatureType creatureType, Point3 point) =>
			{
				if (m_subsystemGameInfo.WorldSettings.DisableNightPredatorSpam)
				{
					return 0f;
				}
				if (m_subsystemSky.SkyLightIntensity < 0.1f)
				{
					float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
					int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
					int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
					int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
					if (num > 20f && temperature > 8 && point.Y < 100 && cellLightFast <= 7 && (num2 == 8 || num2 == 2 || num2 == 3 || num2 == 7))
					{
						return 1f;
					}
				}
				return 0f;
			},
			SpawnFunction = (CreatureType creatureType, Point3 point) => SpawnCreatures(creatureType, "Hyena", point, m_random.Int(1, 2)).Count
		});
		ModsManager.HookAction("InitializeCreatureTypes", (ModLoader loader) =>
		{
			loader.InitializeCreatureTypes(this, m_creatureTypes);
			return false;
		});
	}

	public void SpawnRandomCreature()
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace || (m_subsystemWeather.GlobalPrecipitationIntensity > 0.8f && m_subsystemWeather.TargetLightningIntensity > 0.8f))
		{
			return;
		}
		foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
		{
			Vector2 vector = new Vector2(gameWidget.ActiveCamera.ViewPosition.X, gameWidget.ActiveCamera.ViewPosition.Z);
			if (CountCreaturesInArea(vector - new Vector2(PlayerAreaRadius), vector + new Vector2(PlayerAreaRadius), constantSpawn: false) >= MaxCreaturesInPlayerArea)
			{
				break;
			}
			SpawnLocationType spawnLocationType = GetRandomSpawnLocationType();
			Point3? randomSpawnPoint = GetRandomSpawnPoint(gameWidget.ActiveCamera, spawnLocationType);
			if (!randomSpawnPoint.HasValue)
			{
				continue;
			}
			Vector2 vector2 = new Vector2(randomSpawnPoint.Value.X, randomSpawnPoint.Value.Z);
			if (CountCreaturesInArea(vector2 - new Vector2(AreaLimitRadius), vector2 + new Vector2(AreaLimitRadius), constantSpawn: false) >= AreaLimitGeneral)
			{
				break;
			}
			IEnumerable<CreatureType> enumerable = m_creatureTypes.Where((CreatureType c) => c.SpawnLocationType == spawnLocationType && c.RandomSpawn);
			List<CreatureType> list = new List<CreatureType>();
			List<float> list2 = new List<float>();
			foreach (CreatureType item in enumerable)
			{
				float spawnChance = item.SpawnSuitabilityFunction(item, randomSpawnPoint.Value);
				IsSpawnAllowed(gameWidget, item.Name, ref spawnChance);
				if (spawnChance > 0f)
				{
					list2.Add(spawnChance);
					list.Add(item);
				}
			}
			if (list.Count == 0)
			{
				continue;
			}
			int randomWeightedItem = GetRandomWeightedItem(list2);
			if (randomWeightedItem >= 0 && randomWeightedItem < list.Count)
			{
				CreatureType creatureType = list[randomWeightedItem];
				if (m_random.Float(0f, 1f) <= creatureType.SpawnPriority)
				{
					creatureType.SpawnFunction(creatureType, randomSpawnPoint.Value);
				}
			}
		}
	}

	public List<Entity> SpawnCreatures(CreatureType creatureType, string templateName, Point3 point, int count)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return new List<Entity>();
		}
		if (m_subsystemWeather.GlobalPrecipitationIntensity > 0.8f && m_subsystemWeather.TargetLightningIntensity > 0.8f)
		{
			return new List<Entity>();
		}
		List<Entity> i = new List<Entity>();
		int blockMem = 0;
		while (count > 0 && blockMem < 50)
		{
			Point3 spawnPoint = point;
			if (blockMem > 0)
			{
				spawnPoint.X += m_random.Int(-8, 8);
				spawnPoint.Y += m_random.Int(-4, 8);
				spawnPoint.Z += m_random.Int(-8, 8);
			}
			Point3? point2 = ProcessSpawnPoint(spawnPoint, creatureType.SpawnLocationType);
			if (point2.HasValue && creatureType.SpawnSuitabilityFunction(creatureType, point2.Value) > 0f)
			{
				float num = 1.1f;
				if (creatureType.SpawnLocationType == SpawnLocationType.Water)
				{
					num = -0.5f;
				}
				Vector3 position = new Vector3((float)point2.Value.X + m_random.Float(0.4f, 0.6f), (float)point2.Value.Y + num, (float)point2.Value.Z + m_random.Float(0.4f, 0.6f));
				Entity entity = SpawnCreature(templateName, position, creatureType.ConstantSpawn);
				if (entity != null)
				{
					i.Add(entity);
					count--;
				}
			}
			blockMem++;
		}
		return i;
	}

	public Entity SpawnCreature(string templateName, Vector3 position, bool constantSpawn)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return null;
		}
		try
		{
			Entity num2 = DatabaseManager.CreateEntity(Project, templateName, throwIfNotFound: true);
			ComponentBody direction = num2.FindComponent<ComponentBody>();
			direction.Position = position;
			direction.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
			ComponentCreature componentCreature = num2.FindComponent<ComponentCreature>(throwOnError: true);
			componentCreature.ConstantSpawn = constantSpawn;
			float data6 = 0.5f;
			float num15 = 2f;
			switch (componentCreature.Category)
			{
			case CreatureCategory.LandPredator:
				data6 = 0.8f;
				num15 = 1.2f;
				break;
			case CreatureCategory.LandOther:
				data6 = 0.7f;
				num15 = 1.1f;
				break;
			case CreatureCategory.WaterPredator:
				data6 = 0.7f;
				num15 = 2f;
				break;
			case CreatureCategory.Bird:
				data6 = 0.7f;
				num15 = 1.25f;
				break;
			case CreatureCategory.WaterOther:
				data6 = 0.3f;
				num15 = 3.2f;
				break;
			}
			direction.Size = m_random.Float(data6, num15);
			bool valueOrDefault = false;
			foreach (PlayerData data7 in m_subsystemPlayers.PlayersData)
			{
				if (data7.IsMainPlayer && data7.IsCheater)
				{
					valueOrDefault = true;
					break;
				}
			}
			if (valueOrDefault && m_random.Float(0f, 1f) < 0.1f)
			{
				ComponentLocomotion componentLocomotion = num2.FindComponent<ComponentLocomotion>();
				if (componentLocomotion != null)
				{
					componentLocomotion.FlySpeed = m_random.Float(40f, 50f);
				}
			}
			Project.AddEntity(num2);
			return num2;
		}
		catch (Exception ex)
		{
			Log.Error("Unable to spawn creature with template \"" + templateName + "\". Reason: " + ex.Message);
			return null;
		}
	}

	public Point3? GetRandomChunkSpawnPoint(SpawnChunk chunk, SpawnLocationType spawnLocationType)
	{
		for (int num = 0; num < 5; num++)
		{
			int num2 = 16 * chunk.Point.X + m_random.Int(0, 15);
			int num15 = m_random.Int(10, 246);
			int data6 = 16 * chunk.Point.Y + m_random.Int(0, 15);
			Point3? face4 = ProcessSpawnPoint(new Point3(num2, num15, data6), spawnLocationType);
			if (face4.HasValue)
			{
				return face4;
			}
		}
		return null;
	}

	public Point3? GetRandomSpawnPoint(Camera camera, SpawnLocationType spawnLocationType)
	{
		for (int num = 0; num < 10; num++)
		{
			int num2 = Terrain.ToCell(camera.ViewPosition.X) + m_random.Sign() * m_random.Int(SpawnPointMinDistance, SpawnPointMaxDistance);
			int num3 = Terrain.ToCell(camera.ViewPosition.Z) + m_random.Sign() * m_random.Int(SpawnPointMinDistance, SpawnPointMaxDistance);
			int num4 = MathUtils.Clamp(Terrain.ToCell(camera.ViewPosition.Y) + m_random.Int(-30, 30), 2, 254);
			Point3? num19 = ProcessSpawnPoint(new Point3(num2, num4, num3), spawnLocationType);
			if (num19.HasValue)
			{
				return num19;
			}
		}
		return null;
	}

	public Point3? ProcessSpawnPoint(Point3 spawnPoint, SpawnLocationType spawnLocationType)
	{
		int num = spawnPoint.X;
		int num2 = MathUtils.Clamp(spawnPoint.Y, 1, 254);
		int num3 = spawnPoint.Z;
		TerrainChunk num4 = m_subsystemTerrain.Terrain.GetChunkAtCell(num, num3);
		if (num4 != null && num4.State > TerrainChunkState.InvalidPropagatedLight)
		{
			for (int i = 0; i < 30; i++)
			{
				Point3 wireFacesBitmask = new Point3(num, num2 + i, num3);
				if (TestSpawnPoint(wireFacesBitmask, spawnLocationType))
				{
					return wireFacesBitmask;
				}
				Point3 valueOrDefault = new Point3(num, num2 - i, num3);
				if (TestSpawnPoint(valueOrDefault, spawnLocationType))
				{
					return valueOrDefault;
				}
			}
		}
		return null;
	}

	public bool TestSpawnPoint(Point3 spawnPoint, SpawnLocationType spawnLocationType)
	{
		if (IsInPrivateTerritory(spawnPoint))
		{
			return false;
		}
		int contents = spawnPoint.X;
		int blockEntity = spawnPoint.Y;
		int guid = spawnPoint.Z;
		if (blockEntity <= 3 || blockEntity >= 253)
		{
			return false;
		}
		switch (spawnLocationType)
		{
		case SpawnLocationType.Surface:
		{
			int entityTemplateNameForBlock = m_subsystemTerrain.Terrain.GetCellLightFast(contents, blockEntity, guid);
			if (m_subsystemSky.SkyLightValue - entityTemplateNameForBlock > 3)
			{
				return false;
			}
			int miner = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity - 1, guid);
			int cellContentsFast3 = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity, guid);
			int cellContentsFast4 = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity + 1, guid);
			Block item = BlocksManager.Blocks[miner];
			Block inventorySnapshot = BlocksManager.Blocks[cellContentsFast3];
			Block slotsCount = BlocksManager.Blocks[cellContentsFast4];
			if ((item.IsCollidable || item is FocalorsWaterBlock) && !inventorySnapshot.IsCollidable && !(inventorySnapshot is FocalorsWaterBlock) && !slotsCount.IsCollidable)
			{
				return !(slotsCount is FocalorsWaterBlock);
			}
			return false;
		}
		case SpawnLocationType.Cave:
		{
			int i = m_subsystemTerrain.Terrain.GetCellLightFast(contents, blockEntity, guid);
			if (m_subsystemSky.SkyLightValue - i < 5)
			{
				return false;
			}
			int j = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity - 1, guid);
			int slotCount = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity, guid);
			int num2 = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity + 1, guid);
			Block num3 = BlocksManager.Blocks[j];
			Block block3 = BlocksManager.Blocks[slotCount];
			Block block4 = BlocksManager.Blocks[num2];
			if ((num3.IsCollidable || num3 is FocalorsWaterBlock) && !block3.IsCollidable && !(block3 is FocalorsWaterBlock) && !block4.IsCollidable)
			{
				return !(block4 is FocalorsWaterBlock);
			}
			return false;
		}
		case SpawnLocationType.Water:
		{
			int cellValueFast = m_subsystemTerrain.Terrain.GetCellValueFast(contents, blockEntity, guid);
			int num = Terrain.ExtractContents(cellValueFast);
			int cellContentsFast = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity + 1, guid);
			int cellContentsFast2 = m_subsystemTerrain.Terrain.GetCellContentsFast(contents, blockEntity + 2, guid);
			Block obj = BlocksManager.Blocks[num];
			Block block = BlocksManager.Blocks[cellContentsFast];
			Block block2 = BlocksManager.Blocks[cellContentsFast2];
			bool flag = false;
			if (obj is FocalorsWaterBlock && FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast)) <= 0.01f)
			{
				flag = true;
			}
			if (flag && !block.IsCollidable)
			{
				return !block2.IsCollidable;
			}
			return false;
		}
		default:
			throw new InvalidOperationException("Unknown spawn location type.");
		}
	}

	public int CountCreatures(bool constantSpawn)
	{
		int num = 0;
		foreach (ComponentBody body in m_subsystemBodies.Bodies)
		{
			ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null && componentCreature.ConstantSpawn == constantSpawn)
			{
				num++;
			}
		}
		return num;
	}

	public int CountCreaturesInArea(Vector2 c1, Vector2 c2, bool constantSpawn)
	{
		int dictionary = 0;
		m_componentBodies.Clear();
		m_subsystemBodies.FindBodiesInArea(c1, c2, m_componentBodies);
		for (int value = 0; value < m_componentBodies.Count; value++)
		{
			ComponentBody entitySnapshot = m_componentBodies.Array[value];
			ComponentCreature i = entitySnapshot.Entity.FindComponent<ComponentCreature>();
			if (i != null && i.ConstantSpawn == constantSpawn)
			{
				Vector3 inventorySnapshot = entitySnapshot.Position;
				if (inventorySnapshot.X >= c1.X && inventorySnapshot.X <= c2.X && inventorySnapshot.Z >= c1.Y && inventorySnapshot.Z <= c2.Y)
				{
					dictionary++;
				}
			}
		}
		Point2 point = Terrain.ToChunk(c1);
		Point2 item = Terrain.ToChunk(c2);
		for (int j = point.X; j <= item.X; j++)
		{
			for (int k = point.Y; k <= item.Y; k++)
			{
				SpawnChunk k2 = m_subsystemSpawn.GetSpawnChunk(new Point2(j, k));
				if (k2 == null)
				{
					continue;
				}
				foreach (SpawnEntityData spawnsDatum in k2.SpawnsData)
				{
					if (spawnsDatum.ConstantSpawn == constantSpawn)
					{
						Vector3 position = spawnsDatum.Position;
						if (position.X >= c1.X && position.X <= c2.X && position.Z >= c1.Y && position.Z <= c2.Y)
						{
							dictionary++;
						}
					}
				}
			}
		}
		return dictionary;
	}

	public int GetRandomWeightedItem(IEnumerable<float> items)
	{
		float blockEntity = MathUtils.Max(items.Sum(), 1f);
		float entitySnapshot = m_random.Float(0f, blockEntity);
		int num = 0;
		foreach (float item in items)
		{
			if (entitySnapshot < item)
			{
				return num;
			}
			entitySnapshot -= item;
			num++;
		}
		return -1;
	}

	public SpawnLocationType GetRandomSpawnLocationType()
	{
		return m_spawnLocations[m_random.Int(0, m_spawnLocations.Length - 1)];
	}

	public SubsystemCreatureSpawn()
	{
		MaxCreaturesPerPlayer = 50;
		MaxCreaturesInPlayerArea = 48;
		PlayerAreaRadius = 180f;
		SpawnPointMinDistance = 60;
		SpawnPointMaxDistance = 120;
		AreaLimitGeneral = 3;
		AreaLimitRadius = 48f;
		ChunkLimitConstant = 12;
		ChunkLimitNormal = 9;
		ChunkRadiusConstant = 126f;
		ChunkRadiusNormal = 48f;
		m_creatureTypes = new List<CreatureType>();
		m_creatures = new Dictionary<ComponentCreature, bool>();
		m_componentBodies = new DynamicArray<ComponentBody>();
		m_newSpawnChunks = new List<SpawnChunk>();
		m_spawnChunks = new List<SpawnChunk>();
		m_genocideCounts = new Dictionary<GameWidget, Dictionary<string, int>>();
		m_playerLocalCreatureCounts = new Dictionary<GameWidget, int>();
		m_random = new Random();
	}

	public bool IsInPrivateTerritory(Point3 spawnPoint)
	{
		int x = spawnPoint.X;
		int z = spawnPoint.Z;
		Territoriy territoriy;
		return SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out territoriy);
	}

	private void UpdatePlayerCreatureCount(GameWidget gameWidget, int delta)
	{
		if (!m_playerLocalCreatureCounts.ContainsKey(gameWidget))
		{
			m_playerLocalCreatureCounts[gameWidget] = 0;
		}
		m_playerLocalCreatureCounts[gameWidget] += delta;
		if (m_playerLocalCreatureCounts[gameWidget] < 0)
		{
			m_playerLocalCreatureCounts[gameWidget] = 0;
		}
	}

	public void SpawnChunkCreatures(SpawnChunk chunk, int maxAttempts, bool constantSpawn, GameWidget gameWidget)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace || (m_subsystemWeather.GlobalPrecipitationIntensity > 0.8f && m_subsystemWeather.TargetLightningIntensity > 0.8f))
		{
			return;
		}
		int num = (constantSpawn ? ChunkLimitConstant : ChunkLimitNormal);
		float v = (constantSpawn ? ChunkRadiusConstant : ChunkRadiusNormal);
		int num2 = CountCreatures(constantSpawn);
		Vector2 c = new Vector2(chunk.Point.X * 16, chunk.Point.Y * 16) - new Vector2(v);
		Vector2 c2 = new Vector2((chunk.Point.X + 1) * 16, (chunk.Point.Y + 1) * 16) + new Vector2(v);
		int num3 = CountCreaturesInArea(c, c2, constantSpawn);
		for (int i = 0; i < maxAttempts; i++)
		{
			if (num3 >= num || !IsUnderMaxCreaturesLimit(gameWidget))
			{
				break;
			}
			SpawnLocationType spawnLocationType = GetRandomSpawnLocationType();
			Point3? randomChunkSpawnPoint = GetRandomChunkSpawnPoint(chunk, spawnLocationType);
			if (!randomChunkSpawnPoint.HasValue)
			{
				continue;
			}
			List<CreatureType> list = new List<CreatureType>();
			List<float> list2 = new List<float>();
			float num4 = 0f;
			Func<CreatureType, bool> predicate = (CreatureType ct) => ct.SpawnLocationType == spawnLocationType && ct.ConstantSpawn == constantSpawn;
			foreach (CreatureType item in m_creatureTypes.Where(predicate))
			{
				float spawnChance = item.SpawnSuitabilityFunction(item, randomChunkSpawnPoint.Value);
				IsSpawnAllowed(gameWidget, item.Name, ref spawnChance);
				if (spawnChance > 0f)
				{
					list.Add(item);
					list2.Add(spawnChance);
					num4 += spawnChance;
				}
			}
			if (list.Count <= 0 || !(num4 > 0f))
			{
				continue;
			}
			float num5 = 0.5f / (1f + num4);
			if (!(m_random.Float(0f, 1f) > num5))
			{
				continue;
			}
			for (int num6 = 0; num6 < list.Count; num6++)
			{
				float num7 = list2[num6] / num4;
				if (m_random.Float(0f, 1f) < num7)
				{
					int num8 = list[num6].SpawnFunction(list[num6], randomChunkSpawnPoint.Value);
					num3 += num8;
					num2 += num8;
					if (num3 >= num)
					{
						break;
					}
				}
			}
		}
	}

	private bool IsUnderMaxCreaturesLimit(GameWidget gameWidget)
	{
		if (m_playerLocalCreatureCounts.TryGetValue(gameWidget, out var num))
		{
			return num < MaxCreaturesPerPlayer;
		}
		return true;
	}

	public void RegisterKill(GameWidget gameWidget, string creatureType)
	{
		if (!m_genocideCounts.ContainsKey(gameWidget))
		{
			m_genocideCounts[gameWidget] = new Dictionary<string, int>();
		}
		if (!m_genocideCounts[gameWidget].ContainsKey(creatureType))
		{
			m_genocideCounts[gameWidget][creatureType] = 0;
		}
		m_genocideCounts[gameWidget][creatureType]++;
		int point2 = m_genocideCounts[gameWidget][creatureType];
		if (SettingsManager.DebugMode && point2 >= 10)
		{
			string num = gameWidget.GetHashCode() + creatureType;
			if (!m_lastGenocideLogTimes.ContainsKey(num))
			{
				m_lastGenocideLogTimes[num] = -100.0;
			}
			if (m_subsystemTime.GameTime - m_lastGenocideLogTimes[num] > 10.0)
			{
				m_lastGenocideLogTimes[num] = m_subsystemTime.GameTime;
				Log.Warning($"[GENOCIDE] Type:{creatureType.ToUpper()}\u00a0|\u00a0Count:{point2}\u00a0|\u00a0Status:BLOCKED\u00a0|\u00a0FPS:{PerformanceManager.Fps:0.0}");
			}
		}
	}

	private bool IsSpawnAllowed(GameWidget gameWidget, string creatureType, ref float spawnChance)
	{
		if (m_genocideCounts.TryGetValue(gameWidget, out var min) && min.TryGetValue(creatureType, out var max) && max >= 30)
		{
			spawnChance = 0f;
			return false;
		}
		return true;
	}

	public void EnforceCorpseLimit(int maxCorpses)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		List<ComponentCreature> min = new List<ComponentCreature>();
		foreach (ComponentCreature i in m_creatures.Keys)
		{
			if (i == null)
			{
				continue;
			}
			ComponentHealth j = i.ComponentHealth;
			if (j != null && j.Health == 0f && j.DeathTime.HasValue)
			{
				ComponentSpawn componentSpawn = i.Entity.FindComponent<ComponentSpawn>();
				if (componentSpawn != null && !componentSpawn.IsDespawning)
				{
					min.Add(i);
				}
			}
		}
		if (min.Count <= maxCorpses)
		{
			return;
		}
		min.Sort((ComponentCreature a, ComponentCreature b) =>
		{
			double value2 = a.ComponentHealth.DeathTime.Value;
			double value3 = b.ComponentHealth.DeathTime.Value;
			return value2.CompareTo(value3);
		});
		int max = min.Count - maxCorpses;
		SubsystemTerrain num = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		SubsystemPickables num2 = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		Random num3 = new Random();
		for (int num4 = 0; num4 < max; num4++)
		{
			ComponentCreature componentCreature = min[num4];
			if (componentCreature.Entity.FindComponent<ComponentPlayer>() == null && num != null && num2 != null)
			{
				Vector3 position = (componentCreature.ComponentBody.BoundingBox.Min + componentCreature.ComponentBody.BoundingBox.Max) / 2f;
				int x = Terrain.ToCell(position.X);
				int num5 = Terrain.ToCell(position.Y);
				int z = Terrain.ToCell(position.Z);
				int cellValue = num.Terrain.GetCellValue(x, num5 - 1, z);
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
				int num6 = Terrain.ExtractContents(num.Terrain.GetCellValue(x, num5, z));
				Block block2 = BlocksManager.Blocks[num6];
				if (!SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var _) && block.IsCollidable && !block2.IsCollidable)
				{
					float num7 = num3.Float(0f, 1f);
					int num8 = ((!(num7 < 0.8f)) ? ((num7 < 0.9f) ? 1 : 2) : 0);
					int num9 = num3.Int(0, 3);
					int num10 = num3.Int(0, 3);
					int data = (num8 << 4) | (num9 << 2) | num10;
					int value = Terrain.MakeBlockValue(362, 0, data);
					num.ChangeCell(x, num5, z, value);
				}
				else
				{
					int num11 = num3.Int(1, 2);
					for (int num12 = 0; num12 < num11; num12++)
					{
						num2.CreatePickable(null, Terrain.MakeBlockValue(362, 0, 0), 1, position, null, null);
					}
				}
			}
			componentCreature.Entity.FindComponent<ComponentLoot>()?.DropRemainingNow();
			ComponentSpawn componentSpawn2 = componentCreature.Entity.FindComponent<ComponentSpawn>();
			if (componentSpawn2 != null && !componentSpawn2.IsDespawning)
			{
				componentSpawn2.Despawn();
			}
		}
	}

	private bool IsIntroActive()
	{
		if (m_subsystemPlayers.PlayersData.Count == 0)
		{
			return true;
		}
		foreach (PlayerData j in m_subsystemPlayers.PlayersData)
		{
			if (j.ComponentPlayer == null)
			{
				return true;
			}
			ComponentIntro k = j.ComponentPlayer.Entity.FindComponent<ComponentIntro>();
			if (k != null && k.m_stateMachine.CurrentState != null)
			{
				return true;
			}
		}
		return false;
	}

	public void ProcessWerewolfAttributes(Entity entity)
	{
		ComponentSmartChaseBehavior list = entity.FindComponent<ComponentSmartChaseBehavior>();
		if (IsCheaterWorld)
		{
			if (list != null)
			{
				list.PickedItemType = 116;
				list.PickedItemCount = 10;
			}
			ComponentClothing i = entity.FindComponent<ComponentClothing>();
			if (i != null)
			{
				i.SetClothes(ClothingSlot.Head, new int[1] { MakeClothingBlockValue(21, 0) });
				i.SetClothes(ClothingSlot.Torso, new int[1] { MakeClothingBlockValue(19, 0) });
				i.SetClothes(ClothingSlot.Legs, new int[1] { MakeClothingBlockValue(20, 0) });
				i.SetClothes(ClothingSlot.Feet, new int[1] { MakeClothingBlockValue(22, 0) });
				i.SetClothes(ClothingSlot.Sleeves, new int[1] { MakeClothingBlockValue(53, 0) });
			}
			return;
		}
		if (list != null && m_random.Float(0f, 1f) < 0.8f)
		{
			var j = new[]
			{
				new
				{
					Id = 122,
					Count = 1
				},
				new
				{
					Id = 5,
					Count = 20
				},
				new
				{
					Id = 111,
					Count = 1
				},
				new
				{
					Id = 29,
					Count = 1
				},
				new
				{
					Id = 90,
					Count = 1
				},
				new
				{
					Id = 23,
					Count = 1
				},
				new
				{
					Id = 79,
					Count = 20
				},
				new
				{
					Id = 364,
					Count = 1
				}
			};
			var k = j[m_random.Int(0, j.Length - 1)];
			list.PickedItemType = k.Id;
			list.PickedItemCount = k.Count;
		}
		if (!(m_random.Float(0f, 1f) < 0.4f))
		{
			return;
		}
		ComponentClothing num = entity.FindComponent<ComponentClothing>();
		if (num != null)
		{
			float blockMem = 0.3f;
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Torso, new int[1] { MakeClothingBlockValue(23, 0) });
			}
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Torso, new int[1] { MakeClothingBlockValue(109, 0) });
			}
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Torso, new int[1] { MakeClothingBlockValue(89, 0) });
			}
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Legs, new int[1] { MakeClothingBlockValue(28, 0) });
			}
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Legs, new int[1] { MakeClothingBlockValue(90, 0) });
			}
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Head, new int[1] { MakeClothingBlockValue(74, 0) });
			}
			if (m_random.Float(0f, 1f) < blockMem)
			{
				num.SetClothes(ClothingSlot.Sleeves, new int[1] { MakeClothingBlockValue(113, 0) });
			}
		}
	}

	public static int MakeClothingBlockValue(int index, int color)
	{
		return Terrain.MakeBlockValue(203, 0, ClothingBlock.SetClothingIndex(ClothingBlock.SetClothingColor(0, color), index));
	}
}
