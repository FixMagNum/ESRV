using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemWeather : Subsystem, IDrawable, IUpdateable
{
	public enum Season
	{
		Autumn = 1,
		Anabiosis = 3,
		Winter = 4,
		Spring = 7,
		Summer = 10,
		EarlyAutumn = 0,
		LateAutumn = 2,
		LateWinter = 5,
		EarlySpring = 6,
		LateSpring = 8,
		EarlySummer = 9,
		LateSummer = 11
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemBlocksScanner m_subsystemBlocksScanner;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemAudio m_subsystemAudio;

	public Random m_random;

	public Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>> m_activeShafts;

	public List<PrecipitationShaftParticleSystem> m_toRemove;

	public Dictionary<GameWidget, Vector2?> m_lastShaftsUpdatePositions;

	public float m_targetRainSoundVolume;

	public double m_precipitationStartTime;

	public double m_precipitationEndTime;

	public float m_lightningIntensity;

	public const int m_rainSoundRadius = 5;

	public float m_rainVolumeFactor;

	public Sound m_rainSound;

	public static int[] m_drawOrders;

	public Sound m_windSound;

	public float m_targetWindSoundVolume;

	public float m_windVolumeFactor;

	private Dictionary<GameWidget, Vector2?> m_lastWindUpdatePositions;

	private readonly List<Tuple<float, float>> windStrengthChances;

	private readonly string[] windDirections;

	public static readonly Dictionary<string, Vector2> WindDirectionVectors;

	public Sound m_lightWarmWindSound;

	public Sound m_averageWarmWindSound;

	public Sound m_stormWarmWindSound;

	public Sound m_lightColdWindSound;

	public Sound m_averageColdWindSound;

	public Sound m_stormColdWindSound;

	private Sound m_rainGlassSound;

	public Sound m_waterWindSound;

	private float rainGlassVolume;

	public Sound m_stormRainSound;

	public Sound m_hurricaneColdWindSound;

	public Sound m_hurricaneWarmWindSound;

	public bool IsGustyWind;

	private double m_nextGustTime;

	private SubsystemPlayers m_subsystemPlayers;

	public bool IsCycloneActive;

	public Vector2 CycloneCenter;

	public Vector2 CycloneDirection;

	public float CycloneRadius;

	public double CycloneStartTime;

	public double CycloneEndTime;

	private double m_nextTornadoCheckTime;

	public readonly List<Vector2> ActiveTornadoes;

	private Dictionary<ulong, byte> m_windExposureCache;

	private int m_cacheWindDirIndex;

	private int m_cacheDeflections;

	private List<TornadoDustParticleSystem> m_tornadoDustSystems;

	private List<TornadoDebrisParticleSystem> m_tornadoDebrisSystems;

	public Vector2 CycloneOrigin;

	public double CycloneSpawnTime;

	public readonly List<Vector2> TornadoSpawnPositions = new List<Vector2>();

	public readonly List<double> TornadoSpawnTimes = new List<double>();

	public readonly List<float> TornadoSpeedMultipliers = new List<float>();

	private Dictionary<GameWidget, CycloneDebrisFlybyParticleSystem> m_cycloneFlybyByWidget;

	public float m_windPhaseMain;

	public float m_windPhaseSide;

	private float m_phaseWs;

	public SubsystemTerrain SubsystemTerrain { get; set; }

	public SubsystemSky SubsystemSky { get; set; }

	public SubsystemTime SubsystemTime { get; set; }

	public RainSplashParticleSystem RainSplashParticleSystem { get; set; }

	public SnowSplashParticleSystem SnowSplashParticleSystem { get; set; }

	public Color RainColor { get; set; }

	public Color SnowColor { get; set; }

	public float GlobalPrecipitationIntensity { get; set; }

	public int[] DrawOrders => m_drawOrders;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public float BasePuddleChance { get; set; }

	public float NearPuddleChance { get; set; }

	public float CurrentWindStrength { get; set; }

	public float TargetWindStrength { get; set; }

	public double WindStartTime { get; set; }

	public double WindEndTime { get; set; }

	public string WindDirection { get; set; }

	public Vector2 CurrentWindVector
	{
		get
		{
			if (!WindDirectionVectors.TryGetValue(WindDirection, out var value))
			{
				value = new Vector2(0f, -1f);
			}
			return value;
		}
	}

	public float TargetLightningIntensity { get; set; }

	public float PrecipitationGrowthSlowdownFactor { get; set; }

	public Color SandColor { get; set; }

	public Season CurrentSeason { get; set; }

	public double SeasonStartTime { get; set; }

	public Season PreviousSeason { get; set; }

	public double NextSeasonChangeTime { get; set; }

	public float TornadoStrength { get; set; }

	public float WindPhaseMain => m_windPhaseMain;

	public float WindPhaseSide => m_windPhaseSide;

	public float PhaseWindStrength => MathUtils.Clamp(m_phaseWs, 0f, 16f);

	public double SeasonCycleDuration
	{
		get
		{
			float node = m_subsystemGameInfo.WorldSettings.SeasonCycleDuration;
			if (node > 0f)
			{
				return node;
			}
			return 144000.0;
		}
	}

	public PrecipitationShaftInfo GetPrecipitationShaftInfo(int x, int z)
	{
		int shaftValue = SubsystemTerrain.Terrain.GetShaftValue(x, z);
		int seasonalTemperature = SubsystemTerrain.Terrain.GetSeasonalTemperature(shaftValue);
		int num = Terrain.ExtractTopHeight(shaftValue);
		int snowTemperatureThreshold = GetSnowTemperatureThreshold();
		if (IsPlaceFrozenExtended(seasonalTemperature, num, snowTemperatureThreshold))
		{
			return new PrecipitationShaftInfo
			{
				Intensity = GlobalPrecipitationIntensity,
				Type = PrecipitationType.Snow,
				YLimit = num + 1
			};
		}
		int seasonalHumidity = SubsystemTerrain.Terrain.GetSeasonalHumidity(shaftValue);
		if (seasonalTemperature > 8 && seasonalHumidity < 8)
		{
			return new PrecipitationShaftInfo
			{
				Intensity = GlobalPrecipitationIntensity,
				Type = PrecipitationType.Sand,
				YLimit = num + 1
			};
		}
		if (seasonalTemperature <= 8 || seasonalHumidity >= 8)
		{
			return new PrecipitationShaftInfo
			{
				Intensity = GlobalPrecipitationIntensity,
				Type = PrecipitationType.Rain,
				YLimit = num + 1
			};
		}
		return new PrecipitationShaftInfo
		{
			Intensity = 0f,
			Type = PrecipitationType.Rain,
			YLimit = num + 1
		};
	}

	public void ManualLightingStrike(Vector3 position, Vector3 direction)
	{
		int num = Terrain.ToCell(position.X + direction.X * 32f);
		int num2 = Terrain.ToCell(position.Z + direction.Z * 32f);
		Vector3? vector = null;
		for (int i = 0; i < 300; i++)
		{
			int num3 = m_random.Int(-8, 8);
			int num4 = m_random.Int(-8, 8);
			int num5 = num + num3;
			int num6 = num2 + num4;
			int num7 = SubsystemTerrain.Terrain.CalculateTopmostCellHeight(num5, num6);
			if (!vector.HasValue || (float)num7 > vector.Value.Y)
			{
				vector = new Vector3(num5, num7, num6);
			}
		}
		if (vector.HasValue)
		{
			SubsystemSky.MakeLightningStrike(vector.Value, (byte)m_random.Int(0, 2));
		}
	}

	public static int GetTemperatureAdjustmentAtHeight(int y)
	{
		return (int)MathUtils.Round((y > 64) ? (-0.0024f * (float)MathUtils.Sqr(y - 64)) : (0.1f * (float)(64 - y)));
	}

	public static bool IsPlaceFrozen(int temperature, int y)
	{
		return temperature + GetTemperatureAdjustmentAtHeight(y) <= 0;
	}

	public static bool ShaftHasSnowOnIce(int x, int z)
	{
		return MathUtils.Hash((uint)((x & 0xFFFF) | (z << 16))) > 429496729;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (drawOrder != DrawOrders[0])
		{
			return;
		}
		UpdateCycloneFlybyForDraw(camera);
		if (SettingsManager.StagePrecipitation >= 1f)
		{
			return;
		}
		int num;
		if (SettingsManager.VisibilityRange > 128)
		{
			num = 9;
		}
		else
		{
			num = ((SettingsManager.VisibilityRange <= 64) ? 7 : 8);
		}
		int num2 = num * num;
		Dictionary<Point2, PrecipitationShaftParticleSystem> activeShafts = GetActiveShafts(camera.GameWidget);
		int cellLight = SubsystemTerrain.Terrain.GetCellLight(Terrain.ToCell(camera.ViewPosition.X), Terrain.ToCell(camera.ViewPosition.Y), Terrain.ToCell(camera.ViewPosition.Z));
		byte b = (byte)(255f * MathUtils.Lerp(0.15f, 1f, (float)cellLight / 15f));
		RainColor = new Color(b, b, b);
		SnowColor = new Color(b, b, b);
		SandColor = new Color(b, b, b);
		Vector2 vector = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
		if (CurrentWindStrength >= 10f && CurrentWindStrength <= 16f)
		{
			Vector2 currentWindVector = CurrentWindVector;
			float num3 = MathUtils.Lerp(3f, 8f, (CurrentWindStrength - 10f) / 6f);
			vector -= currentWindVector * num3;
		}
		Point2 point = Terrain.ToCell(vector);
		m_lastShaftsUpdatePositions.TryGetValue(camera.GameWidget, out var value);
		if (value.HasValue && Vector2.DistanceSquared(value.Value, vector) <= 1f)
		{
			return;
		}
		m_lastShaftsUpdatePositions[camera.GameWidget] = vector;
		m_toRemove.Clear();
		foreach (PrecipitationShaftParticleSystem value2 in activeShafts.Values)
		{
			if (MathUtils.Sqr((float)value2.Point.X + 0.5f - vector.X) + MathUtils.Sqr((float)value2.Point.Y + 0.5f - vector.Y) > (float)num2 + 1f)
			{
				m_toRemove.Add(value2);
			}
		}
		foreach (PrecipitationShaftParticleSystem item in m_toRemove)
		{
			if (m_subsystemParticles.ContainsParticleSystem(item))
			{
				m_subsystemParticles.RemoveParticleSystem(item);
			}
			activeShafts.Remove(item.Point);
		}
		for (int i = point.X - num; i <= point.X + num; i++)
		{
			for (int j = point.Y - num; j <= point.Y + num; j++)
			{
				if (!(MathUtils.Sqr((float)i + 0.5f - vector.X) + MathUtils.Sqr((float)j + 0.5f - vector.Y) <= (float)num2))
				{
					continue;
				}
				Point2 point2 = new Point2(i, j);
				if (!activeShafts.ContainsKey(point2))
				{
					PrecipitationShaftParticleSystem precipitationShaftParticleSystem = new PrecipitationShaftParticleSystem(camera.GameWidget, this, m_random, point2, GetPrecipitationShaftInfo(point2.X, point2.Y).Type);
					if (!m_subsystemParticles.ContainsParticleSystem(precipitationShaftParticleSystem))
					{
						m_subsystemParticles.AddParticleSystem(precipitationShaftParticleSystem);
						activeShafts.Add(point2, precipitationShaftParticleSystem);
					}
				}
			}
		}
	}

	public void Update(float dt)
	{
		UpdateWind(dt);
		UpdateCyclone(dt);
		float timeOfDay = SubsystemSky.m_subsystemTimeOfDay.TimeOfDay;
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		if (totalElapsedGameTime >= NextSeasonChangeTime && (timeOfDay < 0.2f || timeOfDay > 0.8f))
		{
			PreviousSeason = CurrentSeason;
			CurrentSeason = GetNextSeason(CurrentSeason);
			NextSeasonChangeTime = totalElapsedGameTime + SeasonCycleDuration;
			UpdateChunksAroundPlayer();
		}
		if (m_subsystemGameInfo.TotalElapsedGameTime > m_precipitationEndTime && CommonLib.WorkType != WorkType.Client)
		{
			if (m_precipitationEndTime == 0.0)
			{
				Random random = new Random(m_subsystemGameInfo.WorldSeed + 1);
				if (m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard)
				{
					m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * random.Float(0f, 5f));
					TargetLightningIntensity = random.Float(0.5f, 1f);
				}
				else
				{
					m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * random.Float(15f, 240f));
					TargetLightningIntensity = random.Float(0f, 0.4f);
				}
				double num = 60f * random.Float(5.8500004f, 11.700001f);
				float num2 = random.Float(0f, 1f);
				if (num2 < 0.7f)
				{
					num = 60f * random.Float(5.8500004f, 11.700001f);
				}
				else
				{
					num = ((!(num2 < 0.97f)) ? ((double)(60f * random.Float(80f, 140f))) : ((double)(60f * random.Float(40f, 80f))));
				}
				if (CurrentSeason == Season.EarlyAutumn || CurrentSeason == Season.Autumn || CurrentSeason == Season.LateAutumn)
				{
					num *= 1.2;
				}
				m_precipitationEndTime = m_precipitationStartTime + num;
			}
			else
			{
				m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(13f, 240.99998f));
				if (CurrentSeason == Season.Anabiosis)
				{
					TargetLightningIntensity = 1f;
				}
				else
				{
					TargetLightningIntensity = ((m_random.Float(0f, 1f) < 0.5f) ? m_random.Float(0.33f, 1f) : 0f);
				}
				double num3 = 60f * m_random.Float(5.8500004f, 11.700001f);
				float num4 = m_random.Float(0f, 1f);
				if (num4 < 0.7f)
				{
					num3 = 60f * m_random.Float(5.8500004f, 11.700001f);
				}
				else
				{
					num3 = ((!(num4 < 0.97f)) ? ((double)(60f * m_random.Float(80f, 140f))) : ((double)(60f * m_random.Float(40f, 80f))));
				}
				if (CurrentSeason == Season.EarlyAutumn || CurrentSeason == Season.Autumn || CurrentSeason == Season.LateAutumn)
				{
					num3 *= 1.2;
				}
				m_precipitationEndTime = m_precipitationStartTime + num3;
			}
			CommonLib.Net.QueuePackage(new SubsystemWeatherPackage(m_precipitationStartTime, m_precipitationEndTime, m_lightningIntensity, TargetLightningIntensity, WindStartTime, WindEndTime, TargetWindStrength, WindDirection, CurrentWindStrength, (int)CurrentSeason, IsGustyWind));
		}
		if (m_subsystemGameInfo.TotalElapsedGameTime > m_precipitationEndTime)
		{
			float num5 = (float)(m_subsystemGameInfo.TotalElapsedGameTime - m_precipitationEndTime);
			GlobalPrecipitationIntensity = MathUtils.Lerp(GlobalPrecipitationIntensity, 0f, num5 / PrecipitationGrowthSlowdownFactor);
		}
		else
		{
			float num6 = (float)MathUtils.Max(0.0, MathUtils.Min(m_subsystemGameInfo.TotalElapsedGameTime - m_precipitationStartTime, m_precipitationEndTime - m_subsystemGameInfo.TotalElapsedGameTime)) / PrecipitationGrowthSlowdownFactor;
			GlobalPrecipitationIntensity = (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled ? MathUtils.Saturate(num6 * 0.5f) : 0f);
			GlobalPrecipitationIntensity = (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled ? MathUtils.Saturate(num6 * 0.5f) : 0f);
		}
		m_lightningIntensity = MathUtils.Lerp(0f, TargetLightningIntensity, GlobalPrecipitationIntensity);
		TornadoStrength = m_lightningIntensity;
		if (GlobalPrecipitationIntensity == 1f && SubsystemTime.PeriodicGameTimeEvent(1.0, 0.0) && m_lightningIntensity >= 0.8f)
		{
			List<ComponentPlayer> list = new List<ComponentPlayer>();
			foreach (ComponentPlayer componentPlayer2 in m_subsystemPlayers.ComponentPlayers)
			{
				if (CanStrikePlayer(componentPlayer2))
				{
					list.Add(componentPlayer2);
				}
			}
			if (list.Count > 0 && m_random.Float(0f, 1f) < 0.3f)
			{
				ComponentPlayer componentPlayer = list[m_random.Int(0, list.Count - 1)];
				Vector3 position = componentPlayer.ComponentBody.Position;
				int num7 = Terrain.ToCell(position.X);
				int num8 = Terrain.ToCell(position.Z);
				int num9 = SubsystemTerrain.Terrain.CalculateTopmostCellHeight(num7, num8);
				ComponentCreature componentCreature = componentPlayer.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null && componentCreature.PlayerAchievements != null && componentCreature.PlayerAchievements.UmbrellaAboveAll == 0L)
				{
					ComponentMiner componentMiner = componentPlayer.Entity.FindComponent<ComponentMiner>();
					if (componentMiner != null)
					{
						int activeBlockValue = componentMiner.ActiveBlockValue;
						if (Terrain.ExtractContents(activeBlockValue) == 379 && (Terrain.ExtractData(activeBlockValue) & 1) == 1 && position.Y > 80f)
						{
							componentCreature.PlayerAchievements.UmbrellaAboveAll = 1L;
							componentCreature.PlayerAchievements.EnqueueUmbrellaAboveAllAchievement();
						}
					}
				}
				SubsystemSky.MakeLightningStrike(new Vector3(num7, num9, num8), (byte)m_random.Int(0, 2));
				return;
			}
			TerrainChunk[] allocatedChunks = SubsystemTerrain.Terrain.AllocatedChunks;
			for (int i = 0; i < allocatedChunks.Length; i++)
			{
				TerrainChunk terrainChunk = allocatedChunks[m_random.Int(0, allocatedChunks.Length - 1)];
				if (terrainChunk.State < TerrainChunkState.InvalidVertices1 || !m_random.Bool(m_lightningIntensity * 0.0002f))
				{
					continue;
				}
				int num10 = terrainChunk.Origin.X + m_random.Int(0, 15);
				int num11 = terrainChunk.Origin.Y + m_random.Int(0, 15);
				Vector3? vector = null;
				for (int j = 0; j < 300; j++)
				{
					int num12 = m_random.Int(-8, 8);
					int num13 = m_random.Int(-8, 8);
					int num14 = num10 + num12;
					int num15 = num11 + num13;
					int topHeight = SubsystemTerrain.Terrain.GetTopHeight(num14, num15);
					if (!vector.HasValue || (float)topHeight > vector.Value.Y)
					{
						vector = new Vector3(num14, topHeight, num15);
					}
				}
				if (vector.HasValue)
				{
					SubsystemSky.MakeLightningStrike(vector.Value, (byte)m_random.Int(0, 2));
					return;
				}
			}
		}
		float num16 = 0f;
		bool flag = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
		if (GlobalPrecipitationIntensity > 0f)
		{
			float num17 = 0f;
			foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
			{
				int num18 = Terrain.ToCell(listenerPosition.X) - 5;
				int num19 = Terrain.ToCell(listenerPosition.Z) - 5;
				int num20 = Terrain.ToCell(listenerPosition.X) + 5;
				int num21 = Terrain.ToCell(listenerPosition.Z) + 5;
				Vector3 vector2 = default;
				for (int k = num18; k <= num20; k++)
				{
					for (int l = num19; l <= num21; l++)
					{
						PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(k, l);
						if (precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
						{
							vector2.X = (float)k + 0.5f;
							vector2.Y = (flag ? listenerPosition.Y : MathUtils.Max(precipitationShaftInfo.YLimit, listenerPosition.Y));
							vector2.Z = (float)l + 0.5f;
							float num22 = vector2.X - listenerPosition.X;
							float num23 = 8f * (vector2.Y - listenerPosition.Y);
							float num24 = vector2.Z - listenerPosition.Z;
							float distance = MathUtils.Sqrt(num22 * num22 + num23 * num23 + num24 * num24);
							num17 += m_subsystemAudio.CalculateVolume(distance, 1.5f) * precipitationShaftInfo.Intensity;
						}
					}
				}
				num16 = MathUtils.Max(num16, num17);
			}
		}
		m_targetRainSoundVolume = MathUtils.Saturate(1.5f * num16 / m_rainVolumeFactor);
		foreach (Vector3 listenerPosition2 in m_subsystemAudio.ListenerPositions)
		{
			int num25 = Terrain.ToCell(listenerPosition2.X);
			int num26 = Terrain.ToCell(listenerPosition2.Y);
			int num27 = Terrain.ToCell(listenerPosition2.Z);
			float num28 = 0f;
			bool flag2 = false;
			for (int m = -5; m <= 5; m++)
			{
				for (int n = -5; n <= 5; n++)
				{
					int num29 = num25 + m;
					int num30 = num27 + n;
					PrecipitationShaftInfo precipitationShaftInfo2 = GetPrecipitationShaftInfo(num29, num30);
					if (precipitationShaftInfo2.Type != PrecipitationType.Rain || !(precipitationShaftInfo2.Intensity > 0f))
					{
						continue;
					}
					int num31 = (flag ? (num26 + 5) : Math.Min(num26 + 5, precipitationShaftInfo2.YLimit));
					int num32 = Math.Max(num26 - 5, 0);
					for (int num33 = num31; num33 >= num32; num33--)
					{
						int num34 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(num29, num33, num30));
						if ((num34 == 15 || num34 == 44 || num34 == 60 || num34 == 353) && (flag || IsSkyVisibleThroughGlass(num29, num33, num30, precipitationShaftInfo2.YLimit)))
						{
							float num35 = (float)num29 + 0.5f - listenerPosition2.X;
							float num36 = (float)num33 + 0.5f - listenerPosition2.Y;
							float num37 = (float)num30 + 0.5f - listenerPosition2.Z;
							float num38 = MathUtils.Sqrt(num35 * num35 + num36 * num36 + num37 * num37);
							if (num38 <= 5f)
							{
								num28 = Math.Max(num28, m_subsystemAudio.CalculateVolume(num38, 1.5f));
								flag2 = true;
							}
						}
					}
				}
			}
			if (flag2)
			{
				rainGlassVolume = MathUtils.Saturate(num28 * SettingsManager.SoundsVolume);
				m_rainGlassSound.Volume = rainGlassVolume;
				m_rainGlassSound.Play();
			}
			else
			{
				m_rainGlassSound.Volume = 0f;
				m_rainGlassSound.Pause();
			}
		}
		if (m_lightningIntensity > 0.8f && CurrentWindStrength > 12f)
		{
			m_stormRainSound.Volume = MathUtils.Saturate(MathUtils.Lerp(m_stormRainSound.Volume, SettingsManager.SoundsVolume * m_targetRainSoundVolume, 5f * dt));
			if (m_stormRainSound.Volume > AudioManager.MinAudibleVolume)
			{
				m_stormRainSound.Play();
			}
			else
			{
				m_stormRainSound.Pause();
			}
			m_rainSound.Volume = 0f;
			m_rainSound.Pause();
		}
		else
		{
			m_rainSound.Volume = MathUtils.Saturate(MathUtils.Lerp(m_rainSound.Volume, SettingsManager.SoundsVolume * m_targetRainSoundVolume, 5f * dt));
			if (m_rainSound.Volume > AudioManager.MinAudibleVolume)
			{
				m_rainSound.Play();
			}
			else
			{
				m_rainSound.Pause();
			}
			m_stormRainSound.Volume = 0f;
			m_stormRainSound.Pause();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemBlocksScanner = Project.FindSubsystem<SubsystemBlocksScanner>(throwOnError: true);
		SubsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		SubsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		string value = valuesDictionary.GetValue<string>("CurrentSeason", null);
		if (value != null && Enum.IsDefined(typeof(Season), value))
		{
			CurrentSeason = (Season)Enum.Parse(typeof(Season), value);
		}
		else
		{
			InitializeSeason();
		}
		string value2 = valuesDictionary.GetValue<string>("PreviousSeason", null);
		if (value2 != null && Enum.IsDefined(typeof(Season), value2))
		{
			PreviousSeason = (Season)Enum.Parse(typeof(Season), value2);
		}
		else
		{
			PreviousSeason = CurrentSeason;
		}
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		NextSeasonChangeTime = valuesDictionary.GetValue("NextSeasonChangeTime", totalElapsedGameTime + SeasonCycleDuration);
		if (NextSeasonChangeTime <= totalElapsedGameTime)
		{
			NextSeasonChangeTime = totalElapsedGameTime + SeasonCycleDuration;
		}
		m_precipitationStartTime = valuesDictionary.GetValue<double>("WeatherStartTime");
		m_precipitationEndTime = valuesDictionary.GetValue<double>("WeatherEndTime");
		m_lightningIntensity = valuesDictionary.GetValue<float>("LightningIntensity");
		TargetLightningIntensity = valuesDictionary.GetValue("TargetLightningIntensity", 0f);
		TornadoStrength = valuesDictionary.GetValue("TornadoStrength", m_lightningIntensity);
		WindStartTime = valuesDictionary.GetValue("WindStartTime", 0.0);
		WindEndTime = valuesDictionary.GetValue("WindEndTime", 0.0);
		CurrentWindStrength = valuesDictionary.GetValue("CurrentWindStrength", 0f);
		TargetWindStrength = valuesDictionary.GetValue("TargetWindStrength", 0f);
		WindDirection = valuesDictionary.GetValue("WindDirection", "N");
		IsGustyWind = valuesDictionary.GetValue("IsGustyWind", defaultValue: false);
		m_nextGustTime = valuesDictionary.GetValue("NextGustTime", 0.0);
		IsCycloneActive = valuesDictionary.GetValue("CycloneActive", defaultValue: false);
		CycloneCenter = new Vector2(valuesDictionary.GetValue("CycloneCenterX", 0f), valuesDictionary.GetValue("CycloneCenterY", 0f));
		CycloneRadius = valuesDictionary.GetValue("CycloneRadius", 0f);
		CycloneDirection = new Vector2(valuesDictionary.GetValue("CycloneDirX", 0f), valuesDictionary.GetValue("CycloneDirY", 1f));
		CycloneStartTime = valuesDictionary.GetValue("CycloneStartTime", 0.0);
		CycloneEndTime = valuesDictionary.GetValue("CycloneEndTime", 0.0);
		m_nextTornadoCheckTime = valuesDictionary.GetValue("NextTornadoCheckTime", m_subsystemGameInfo.TotalElapsedGameTime + 30.0);
		ActiveTornadoes.Clear();
		ValuesDictionary value3 = valuesDictionary.GetValue<ValuesDictionary>("Tornadoes", null);
		if (value3 != null)
		{
			foreach (string value9 in value3.Values)
			{
				string[] array = value9.Split(',');
				if (array.Length == 2 && float.TryParse(array[0], out var result) && float.TryParse(array[1], out var result2))
				{
					ActiveTornadoes.Add(new Vector2(result, result2));
				}
			}
		}
		TornadoSpawnPositions.Clear();
		TornadoSpawnTimes.Clear();
		TornadoSpeedMultipliers.Clear();
		ValuesDictionary value4 = valuesDictionary.GetValue<ValuesDictionary>("TornadoSpawnPositions", null);
		if (value4 != null)
		{
			for (int i = 0; i < ActiveTornadoes.Count; i++)
			{
				string key = i.ToString();
				string value5 = value4.GetValue<string>(key, null);
				if (!string.IsNullOrEmpty(value5))
				{
					string[] array2 = value5.Split(',');
					if (array2.Length == 2 && float.TryParse(array2[0], out var result3) && float.TryParse(array2[1], out var result4))
					{
						TornadoSpawnPositions.Add(new Vector2(result3, result4));
					}
					else
					{
						TornadoSpawnPositions.Add(ActiveTornadoes[i]);
					}
				}
				else
				{
					TornadoSpawnPositions.Add(ActiveTornadoes[i]);
				}
			}
		}
		else
		{
			for (int j = 0; j < ActiveTornadoes.Count; j++)
			{
				TornadoSpawnPositions.Add(ActiveTornadoes[j]);
			}
		}
		ValuesDictionary value6 = valuesDictionary.GetValue<ValuesDictionary>("TornadoSpawnTimes", null);
		if (value6 != null)
		{
			for (int k = 0; k < ActiveTornadoes.Count; k++)
			{
				string key2 = k.ToString();
				double value7 = value6.GetValue(key2, m_subsystemGameInfo.TotalElapsedGameTime);
				TornadoSpawnTimes.Add(value7);
			}
		}
		else
		{
			for (int l = 0; l < ActiveTornadoes.Count; l++)
			{
				TornadoSpawnTimes.Add(m_subsystemGameInfo.TotalElapsedGameTime);
			}
		}
		ValuesDictionary value8 = valuesDictionary.GetValue<ValuesDictionary>("TornadoSpeedMultipliers", null);
		if (value8 != null)
		{
			for (int m = 0; m < ActiveTornadoes.Count; m++)
			{
				string key3 = m.ToString();
				float num = value8.GetValue(key3, 1f);
				if (num <= 0f)
				{
					num = 1f;
				}
				TornadoSpeedMultipliers.Add(num);
			}
		}
		else
		{
			for (int n = 0; n < ActiveTornadoes.Count; n++)
			{
				TornadoSpeedMultipliers.Add(m_random.Float(0.95f, 1.05f));
			}
		}
		CycloneOrigin = new Vector2(valuesDictionary.GetValue("CycloneOriginX", CycloneCenter.X), valuesDictionary.GetValue("CycloneOriginY", CycloneCenter.Y));
		CycloneSpawnTime = valuesDictionary.GetValue("CycloneSpawnTime", CycloneStartTime);
		m_waterWindSound = m_subsystemAudio.CreateSound("Audio/Wind/WaterWind");
		m_waterWindSound.IsLooped = true;
		m_waterWindSound.Volume = 0f;
		m_rainSound = m_subsystemAudio.CreateSound("Audio/Rain/Rain");
		m_rainSound.IsLooped = true;
		m_rainSound.Volume = 0f;
		m_stormRainSound = m_subsystemAudio.CreateSound("Audio/Rain/StormRain");
		m_stormRainSound.IsLooped = true;
		m_stormRainSound.Volume = 0f;
		m_rainGlassSound = m_subsystemAudio.CreateSound("Audio/Rain/RainGlass");
		m_rainGlassSound.IsLooped = true;
		m_rainGlassSound.Volume = 0f;
		m_windSound = m_subsystemAudio.CreateSound("Audio/Wind/LightWarmWind");
		m_windSound.IsLooped = true;
		m_windSound.Volume = 0f;
		m_lightWarmWindSound = m_subsystemAudio.CreateSound("Audio/Wind/LightWarmWind");
		m_averageWarmWindSound = m_subsystemAudio.CreateSound("Audio/Wind/AverageWarmWind");
		m_stormWarmWindSound = m_subsystemAudio.CreateSound("Audio/Wind/StormWarmWind");
		m_lightColdWindSound = m_subsystemAudio.CreateSound("Audio/Wind/LightColdWind");
		m_averageColdWindSound = m_subsystemAudio.CreateSound("Audio/Wind/AverageColdWind");
		m_stormColdWindSound = m_subsystemAudio.CreateSound("Audio/Wind/StormColdWind");
		m_hurricaneColdWindSound = m_subsystemAudio.CreateSound("Audio/Wind/HurricaneColdWind");
		m_hurricaneWarmWindSound = m_subsystemAudio.CreateSound("Audio/Wind/HurricaneWarmWind");
		Sound[] array3 = new Sound[8] { m_lightWarmWindSound, m_averageWarmWindSound, m_stormWarmWindSound, m_lightColdWindSound, m_averageColdWindSound, m_stormColdWindSound, m_hurricaneColdWindSound, m_hurricaneWarmWindSound };
		foreach (Sound obj in array3)
		{
			obj.IsLooped = true;
			obj.Volume = 0f;
		}
		RainSplashParticleSystem = new RainSplashParticleSystem();
		m_subsystemParticles.AddParticleSystem(RainSplashParticleSystem);
		SnowSplashParticleSystem = new SnowSplashParticleSystem();
		m_subsystemParticles.AddParticleSystem(SnowSplashParticleSystem);
		m_rainVolumeFactor = 0f;
		for (int num3 = -5; num3 <= 5; num3++)
		{
			for (int num4 = -5; num4 <= 5; num4++)
			{
				float distance = MathUtils.Sqrt(num3 * num3 + num4 * num4);
				m_rainVolumeFactor += m_subsystemAudio.CalculateVolume(distance, 3.5f, 4f);
			}
		}
		m_subsystemBlocksScanner.ScanningChunkCompleted += (TerrainChunk chunk) =>
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
			{
				FreezeThawAndDepositSnow(chunk);
				DefrostingMeltingRemovingSnow(chunk);
				ProcessFluidsWeatherInteraction(chunk);
				if (GlobalPrecipitationIntensity > 0f && IsRainActive())
				{
					CreatePuddles(chunk);
				}
			}
		};
		m_subsystemBlocksScanner.ScanningChunkCompleted += (TerrainChunk _) =>
		{
			m_windExposureCache.Clear();
		};
		BasePuddleChance = valuesDictionary.GetValue("BasePuddleChance", 0.05f);
		NearPuddleChance = valuesDictionary.GetValue("NearPuddleChance", 0.25f);
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("WeatherStartTime", Math.Round(m_precipitationStartTime, 3));
		valuesDictionary.SetValue("WeatherEndTime", Math.Round(m_precipitationEndTime, 3));
		valuesDictionary.SetValue("PreviousSeason", PreviousSeason.ToString());
		valuesDictionary.SetValue("CurrentSeason", CurrentSeason.ToString());
		valuesDictionary.SetValue("NextSeasonChangeTime", Math.Round(NextSeasonChangeTime, 3));
		valuesDictionary.SetValue("LightningIntensity", (float)Math.Round(m_lightningIntensity, 3));
		valuesDictionary.SetValue("TargetLightningIntensity", (float)Math.Round(TargetLightningIntensity, 3));
		valuesDictionary.SetValue("TornadoStrength", (float)Math.Round(TornadoStrength, 3));
		valuesDictionary.SetValue("WindStartTime", Math.Round(WindStartTime, 3));
		valuesDictionary.SetValue("WindEndTime", Math.Round(WindEndTime, 3));
		valuesDictionary.SetValue("CurrentWindStrength", (float)Math.Round(CurrentWindStrength, 3));
		valuesDictionary.SetValue("TargetWindStrength", (float)Math.Round(TargetWindStrength, 3));
		valuesDictionary.SetValue("WindDirection", WindDirection);
		valuesDictionary.SetValue("IsGustyWind", IsGustyWind);
		valuesDictionary.SetValue("NextGustTime", Math.Round(m_nextGustTime, 3));
		valuesDictionary.SetValue("CycloneActive", IsCycloneActive);
		valuesDictionary.SetValue("CycloneCenterX", CycloneCenter.X);
		valuesDictionary.SetValue("CycloneCenterY", CycloneCenter.Y);
		valuesDictionary.SetValue("CycloneRadius", CycloneRadius);
		valuesDictionary.SetValue("CycloneDirX", CycloneDirection.X);
		valuesDictionary.SetValue("CycloneDirY", CycloneDirection.Y);
		valuesDictionary.SetValue("CycloneStartTime", Math.Round(CycloneStartTime, 3));
		valuesDictionary.SetValue("CycloneEndTime", Math.Round(CycloneEndTime, 3));
		valuesDictionary.SetValue("NextTornadoCheckTime", Math.Round(m_nextTornadoCheckTime, 3));
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		for (int i = 0; i < ActiveTornadoes.Count; i++)
		{
			Vector2 vector = ActiveTornadoes[i];
			valuesDictionary2.SetValue(i.ToString(), vector.X + "," + vector.Y);
		}
		valuesDictionary.SetValue("Tornadoes", valuesDictionary2);
		ValuesDictionary valuesDictionary3 = new ValuesDictionary();
		ValuesDictionary valuesDictionary4 = new ValuesDictionary();
		ValuesDictionary valuesDictionary5 = new ValuesDictionary();
		for (int j = 0; j < ActiveTornadoes.Count; j++)
		{
			Vector2 vector2 = ((j < TornadoSpawnPositions.Count) ? TornadoSpawnPositions[j] : ActiveTornadoes[j]);
			double value = ((j < TornadoSpawnTimes.Count) ? TornadoSpawnTimes[j] : m_subsystemGameInfo.TotalElapsedGameTime);
			float num = ((j < TornadoSpeedMultipliers.Count) ? TornadoSpeedMultipliers[j] : 1f);
			valuesDictionary3.SetValue(j.ToString(), vector2.X + "," + vector2.Y);
			valuesDictionary4.SetValue(j.ToString(), Math.Round(value, 3));
			valuesDictionary5.SetValue(j.ToString(), (float)Math.Round(num, 3));
		}
		valuesDictionary.SetValue("TornadoSpawnPositions", valuesDictionary3);
		valuesDictionary.SetValue("TornadoSpawnTimes", valuesDictionary4);
		valuesDictionary.SetValue("TornadoSpeedMultipliers", valuesDictionary5);
		valuesDictionary.SetValue("CycloneOriginX", CycloneOrigin.X);
		valuesDictionary.SetValue("CycloneOriginY", CycloneOrigin.Y);
		valuesDictionary.SetValue("CycloneSpawnTime", Math.Round(CycloneSpawnTime, 3));
	}

	public Dictionary<Point2, PrecipitationShaftParticleSystem> GetActiveShafts(GameWidget gameWidget)
	{
		if (!m_activeShafts.TryGetValue(gameWidget, out var value))
		{
			value = new Dictionary<Point2, PrecipitationShaftParticleSystem>();
			m_activeShafts.Add(gameWidget, value);
		}
		return value;
	}

	public void FreezeThawAndDepositSnow(TerrainChunk chunk)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		Terrain terrain = SubsystemTerrain.Terrain;
		int snowTemperatureThreshold = GetSnowTemperatureThreshold();
		bool flag = m_lightningIntensity >= 0.8f;
		int max;
		if (flag && CurrentWindStrength >= 12f)
		{
			max = 13;
		}
		else
		{
			max = ((!flag) ? 39 : 19);
		}
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (m_random.Int(0, max) != 0)
				{
					continue;
				}
				int topHeightFast = chunk.GetTopHeightFast(i, j);
				int cellValueFast = chunk.GetCellValueFast(i, topHeightFast, j);
				int num = Terrain.ExtractContents(cellValueFast);
				int x = chunk.Origin.X + i;
				int num2 = topHeightFast;
				int z = chunk.Origin.Y + j;
				int seasonalTemperature = SubsystemTerrain.Terrain.GetSeasonalTemperature(SubsystemTerrain.Terrain.GetShaftValue(x, z));
				if (num == 18 && IsPlaceFrozenExtended(seasonalTemperature, num2, snowTemperatureThreshold))
				{
					float level = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast));
					float num3 = MathUtils.Saturate(1f - level);
					if (num3 >= 0.99f)
					{
						SubsystemTerrain.ChangeCell(x, num2, z, Terrain.MakeBlockValue(62));
					}
					else if (num3 > 0.01f)
					{
						int value = LayeredIceBlock.SetSizeInValue(Terrain.MakeBlockValue(352), num3);
						SubsystemTerrain.ChangeCell(x, num2, z, value);
					}
				}
				PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(x, z);
				if (num == 348 && seasonalTemperature > snowTemperatureThreshold)
				{
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, num2 + 1, z)) != 61)
					{
						SubsystemTerrain.ChangeCellFluid(x, num2, z, SnowBlock.SetSizeInValue(Terrain.MakeBlockValue(61), 1f));
					}
				}
				else if (num == 350 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, num2 + 1, z)) != 348)
				{
					SubsystemTerrain.ChangeCellFluid(x, num2, z, Terrain.MakeBlockValue(348));
				}
				else
				{
					if (precipitationShaftInfo.Type != PrecipitationType.Snow || !(precipitationShaftInfo.Intensity > 0.5f) || !IsPlaceFrozenExtended(seasonalTemperature, num2, snowTemperatureThreshold))
					{
						continue;
					}
					if (SubsystemSnowBlockBehavior.CanSupportSnow(cellValueFast) && num2 + 1 < 255)
					{
						switch (num)
						{
						case 61:
						{
							float sizeFromValue = SnowBlock.GetSizeFromValue(cellValueFast);
							if (!SubsystemSnowBlockBehavior.IsLeavesBlock(Terrain.ExtractContents(terrain.GetCellValue(x, num2 - 1, z))))
							{
								float num4 = sizeFromValue + 0.07f;
								SubsystemTerrain.ChangeCellFluid(x, num2, z, SnowBlock.SetSizeInValue(cellValueFast, num4));
								if (num4 >= 1f)
								{
									SubsystemTerrain.ChangeCellFluid(x, num2, z, Terrain.MakeBlockValue(348));
								}
							}
							break;
						}
						case 348:
							if (SubsystemSnowBlockBehavior.IsLeavesBlock(Terrain.ExtractContents(cellValueFast)))
							{
								SubsystemTerrain.DestroyCell(0, x, num2, z, 0, noDrop: true, noParticleSystem: true);
							}
							else
							{
								SubsystemTerrain.ChangeCellFluid(x, num2 + 1, z, Terrain.MakeBlockValue(61));
							}
							break;
						case 62:
							SubsystemTerrain.ChangeCellFluid(x, num2 + 1, z, Terrain.MakeBlockValue(61));
							break;
						default:
							if (SubsystemSnowBlockBehavior.IsLeavesBlock(num))
							{
								SubsystemTerrain.ChangeCellFluid(x, num2 + 1, z, Terrain.MakeBlockValue(61));
							}
							else
							{
								SubsystemTerrain.ChangeCellFluid(x, num2 + 1, z, Terrain.MakeBlockValue(61));
							}
							break;
						}
					}
					if (num == 19 && m_random.Float(0f, 1f) < 0.9f)
					{
						SubsystemTerrain.DestroyCell(0, x, num2, z, 0, noDrop: true, noParticleSystem: true);
					}
				}
			}
		}
	}

	public SubsystemWeather()
	{
		m_windExposureCache = new Dictionary<ulong, byte>(1024);
		m_cacheWindDirIndex = -1;
		m_cacheDeflections = -1;
		ActiveTornadoes = new List<Vector2>();
		WindDirection = "N";
		PrecipitationGrowthSlowdownFactor = 100f;
		m_lastWindUpdatePositions = new Dictionary<GameWidget, Vector2?>();
		IsCycloneActive = false;
		CycloneCenter = Vector2.Zero;
		CycloneDirection = Vector2.UnitY;
		CycloneRadius = 0f;
		CycloneStartTime = 0.0;
		CycloneEndTime = 0.0;
		m_nextTornadoCheckTime = 0.0;
		CycloneOrigin = Vector2.Zero;
		CycloneSpawnTime = 0.0;
		windStrengthChances = new List<Tuple<float, float>>
		{
			Tuple.Create(1f, 30f),
			Tuple.Create(2f, 22f),
			Tuple.Create(3f, 17f),
			Tuple.Create(4f, 11f),
			Tuple.Create(5f, 7.5f),
			Tuple.Create(6f, 5.5f),
			Tuple.Create(7f, 3.5f),
			Tuple.Create(8f, 2f),
			Tuple.Create(9f, 0.9f),
			Tuple.Create(10f, 0.5f),
			Tuple.Create(11f, 0.4f),
			Tuple.Create(12f, 0.3f),
			Tuple.Create(13f, 0.2f),
			Tuple.Create(14f, 0.1f),
			Tuple.Create(15f, 0.06f),
			Tuple.Create(16f, 0.04f)
		};
		windDirections = new string[8] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
		BasePuddleChance = 0.005f;
		NearPuddleChance = 0.75f;
		m_random = new Random();
		m_activeShafts = new Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>>();
		m_toRemove = new List<PrecipitationShaftParticleSystem>();
		m_lastShaftsUpdatePositions = new Dictionary<GameWidget, Vector2?>();
		m_tornadoDustSystems = new List<TornadoDustParticleSystem>();
	}

	static SubsystemWeather()
	{
		WindDirectionVectors = new Dictionary<string, Vector2>
		{
			{
				"N",
				new Vector2(0f, -1f)
			},
			{
				"NE",
				new Vector2(0.7071f, -0.7071f)
			},
			{
				"E",
				new Vector2(1f, 0f)
			},
			{
				"SE",
				new Vector2(0.7071f, 0.7071f)
			},
			{
				"S",
				new Vector2(0f, 1f)
			},
			{
				"SW",
				new Vector2(-0.7071f, 0.7071f)
			},
			{
				"W",
				new Vector2(-1f, 0f)
			},
			{
				"NW",
				new Vector2(-0.7071f, -0.7071f)
			}
		};
		m_drawOrders = new int[1] { 50 };
	}

	public bool IsRainActive()
	{
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		if (totalElapsedGameTime >= m_precipitationStartTime)
		{
			return totalElapsedGameTime <= m_precipitationEndTime;
		}
		return false;
	}

	public void CreatePuddles(TerrainChunk chunk)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		Terrain terrain = SubsystemTerrain.Terrain;
		bool flag = m_lightningIntensity >= 0.8f;
		int max;
		float num;
		if (flag && CurrentWindStrength >= 12f)
		{
			max = 10;
			num = 0.15f;
		}
		else if (flag)
		{
			max = 20;
			num = 0.05f;
		}
		else
		{
			max = 50;
			num = 0.02f;
		}
		float level = 0.98f;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (m_random.Int(0, max) != 0)
				{
					continue;
				}
				int x = chunk.Origin.X + i;
				int z = chunk.Origin.Y + j;
				if (GetPrecipitationShaftInfo(x, z).Type != PrecipitationType.Rain)
				{
					continue;
				}
				int topHeightFast = chunk.GetTopHeightFast(i, j);
				int num2 = Terrain.ExtractContents(chunk.GetCellValueFast(i, topHeightFast, j));
				if (num2 != 8 && num2 != 2 && num2 != 21 && num2 != 3 && num2 != 5 && num2 != 18)
				{
					continue;
				}
				int y = topHeightFast + 1;
				if (num2 == 18)
				{
					y = topHeightFast;
				}
				int cellValue = terrain.GetCellValue(x, y, z);
				switch (Terrain.ExtractContents(cellValue))
				{
				case 0:
				{
					int data3 = FocalorsFluidBlock.SetLevel(0, level);
					data3 = FocalorsFluidBlock.SetIsTop(data3, isTop: true);
					int value2 = Terrain.MakeBlockValue(18, 0, data3);
					SubsystemTerrain.ChangeCellFluid(x, y, z, value2);
					break;
				}
				case 18:
				{
					int data = Terrain.ExtractData(cellValue);
					float level2 = FocalorsFluidBlock.GetLevel(data);
					if (level2 > 0.01f)
					{
						float num3 = MathUtils.Max(level2 - num, 0f);
						if (num3 < level2)
						{
							int data2 = FocalorsFluidBlock.SetLevel(data, num3);
							data2 = FocalorsFluidBlock.SetIsTop(data2, isTop: true);
							int value = Terrain.ReplaceData(cellValue, data2);
							SubsystemTerrain.ChangeCellFluid(x, y, z, value);
						}
					}
					break;
				}
				}
			}
		}
	}

	public void UpdateWind(float dt)
	{
		if (!m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled)
		{
			TargetWindStrength = 0f;
			IsCycloneActive = false;
			ActiveTornadoes.Clear();
		}
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		double val = WindEndTime - WindStartTime;
		float targetWindStrength = TargetWindStrength;
		if (totalElapsedGameTime > WindEndTime)
		{
			IsGustyWind = false;
			if (WindEndTime == 0.0)
			{
				Random random = new Random(m_subsystemGameInfo.WorldSeed + 2);
				float min;
				float max;
				if (m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard)
				{
					WindStartTime = totalElapsedGameTime + (double)(60f * random.Float(0f, 10f));
					min = 6f;
					max = 14f;
				}
				else if (m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Medium)
				{
					WindStartTime = totalElapsedGameTime + (double)(60f * random.Float(10f, 20f));
					min = 3f;
					max = 10f;
				}
				else
				{
					WindStartTime = totalElapsedGameTime + (double)random.Float(120f, 1200f);
					min = 1f;
					max = 7f;
				}
				double num = 60f * random.Float(5f, 12f);
				num = ((!(random.Float(0f, 1f) < 0.8f)) ? ((double)(60f * random.Float(25f, 60f))) : ((double)(60f * random.Float(13f, 25f))));
				WindEndTime = WindStartTime + num;
				TargetWindStrength = random.Float(min, max);
				if (!m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled)
				{
					TargetWindStrength = 0f;
				}
				if (TargetWindStrength > 0f)
				{
					WindDirection = windDirections[random.Int(0, windDirections.Length - 1)];
				}
				IsGustyWind = random.Float(0f, 1f) < 0.2f;
			}
			else
			{
				WindStartTime = totalElapsedGameTime + (double)(60f * m_random.Float(13f, 60f));
				double num2 = 60f * m_random.Float(5f, 12f);
				num2 = ((!(m_random.Float(0f, 1f) < 0.8f)) ? ((double)(60f * m_random.Float(25f, 60f))) : ((double)(60f * m_random.Float(13f, 25f))));
				WindEndTime = WindStartTime + num2;
				float num3 = 0f;
				foreach (Tuple<float, float> windStrengthChance in windStrengthChances)
				{
					num3 += windStrengthChance.Item2;
				}
				float num4 = m_random.Float(0f, num3);
				TargetWindStrength = 1f;
				foreach (Tuple<float, float> windStrengthChance2 in windStrengthChances)
				{
					num4 -= windStrengthChance2.Item2;
					if (num4 <= 0f)
					{
						TargetWindStrength = windStrengthChance2.Item1;
						break;
					}
				}
				if (!m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled)
				{
					TargetWindStrength = 0f;
				}
				if (TargetWindStrength > 0f)
				{
					WindDirection = windDirections[m_random.Int(0, windDirections.Length - 1)];
				}
				if (!IsCycloneActive && TargetWindStrength >= 14f && m_random.Float(0f, 1f) < 0.1f)
				{
					ActivateCyclone();
				}
				IsGustyWind = m_random.Float(0f, 1f) < 0.2f;
			}
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new SubsystemWeatherPackage(m_precipitationStartTime, m_precipitationEndTime, m_lightningIntensity, TargetLightningIntensity, WindStartTime, WindEndTime, TargetWindStrength, WindDirection, CurrentWindStrength, (int)CurrentSeason, IsGustyWind));
			}
			val = WindEndTime - WindStartTime;
			targetWindStrength = TargetWindStrength;
		}
		if (CurrentWindStrength > 10f && IsRainActive() && WindEndTime < m_precipitationEndTime)
		{
			WindEndTime = m_precipitationEndTime;
			val = WindEndTime - WindStartTime;
		}
		double num5 = (totalElapsedGameTime - WindStartTime) / Math.Max(1E-06, val);
		if (num5 < 0.0 || num5 > 1.0)
		{
			CurrentWindStrength = MathUtils.Lerp(CurrentWindStrength, 0f, MathUtils.Saturate(dt * 0.1f));
		}
		else
		{
			float num6 = MathUtils.Clamp(targetWindStrength / 16f, 0.1f, 0.2f);
			if (num5 < (double)(1f - num6))
			{
				CurrentWindStrength = MathUtils.Lerp(CurrentWindStrength, targetWindStrength, MathUtils.Saturate(dt * 0.05f));
			}
			else
			{
				float f = (float)((num5 - (double)(1f - num6)) / (double)num6);
				float x = MathUtils.Lerp(targetWindStrength, 0f, f);
				CurrentWindStrength = MathUtils.Lerp(CurrentWindStrength, x, MathUtils.Saturate(dt * 0.05f));
			}
		}
		UpdateWindSound(dt);
		if (IsGustyWind)
		{
			double totalElapsedGameTime2 = m_subsystemGameInfo.TotalElapsedGameTime;
			if (totalElapsedGameTime2 >= m_nextGustTime)
			{
				m_nextGustTime = totalElapsedGameTime2 + 60.0 + (double)m_random.Float(0f, 120f);
				float x2 = CurrentWindStrength + (float)m_random.Int(-1, 1);
				CurrentWindStrength = MathUtils.Clamp(x2, 1f, 16f);
				if (IsCycloneActive && CurrentWindStrength < 13f)
				{
					CurrentWindStrength = 13f;
				}
				TargetWindStrength = CurrentWindStrength;
				int num7 = Array.IndexOf(windDirections, WindDirection);
				if (num7 >= 0)
				{
					num7 = (num7 + m_random.Int(-1, 1) + windDirections.Length) % windDirections.Length;
					WindDirection = windDirections[num7];
				}
				if (CommonLib.WorkType != WorkType.Client)
				{
					CommonLib.Net.QueuePackage(new SubsystemWeatherPackage(m_precipitationStartTime, m_precipitationEndTime, m_lightningIntensity, TargetLightningIntensity, WindStartTime, WindEndTime, TargetWindStrength, WindDirection, CurrentWindStrength, (int)CurrentSeason, IsGustyWind));
				}
			}
		}
		m_phaseWs = MathUtils.Lerp(m_phaseWs, CurrentWindStrength, MathUtils.Saturate(dt * 5f));
		float num8 = MathUtils.Clamp(m_phaseWs, 0f, 16f);
		float num9 = 0.35f * num8;
		float num10 = 0.005f * num8;
		m_windPhaseMain += num9 * dt;
		m_windPhaseSide += num10 * dt;
		if (m_windPhaseMain > (float)Math.PI * 8192f)
		{
			m_windPhaseMain -= (float)Math.PI * 8192f;
		}
		if (m_windPhaseSide > (float)Math.PI * 8192f)
		{
			m_windPhaseSide -= (float)Math.PI * 8192f;
		}
	}

	public void UpdateWindSound(float dt)
	{
		if (SubsystemTerrain.m_modifiedList.Count > 0)
		{
			bool flag = false;
			foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
			{
				for (int i = 0; i < SubsystemTerrain.m_modifiedList.Count; i++)
				{
					Point3 point = SubsystemTerrain.m_modifiedList.Array[i];
					if (MathUtils.Sqr((float)point.X - listenerPosition.X) + MathUtils.Sqr((float)point.Y - listenerPosition.Y) + MathUtils.Sqr((float)point.Z - listenerPosition.Z) <= 10000f)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				m_windExposureCache.Clear();
			}
		}
		int num = Array.IndexOf(windDirections, WindDirection);
		if (num < 0)
		{
			num = 0;
		}
		int maxDeflections = GetMaxDeflections(CurrentWindStrength);
		if (num != m_cacheWindDirIndex || maxDeflections != m_cacheDeflections)
		{
			m_windExposureCache.Clear();
			m_cacheWindDirIndex = num;
			m_cacheDeflections = maxDeflections;
		}
		bool flag2 = false;
		SubsystemFocalorsFluidBlockBehavior subsystemFocalorsFluidBlockBehavior = SubsystemTerrain.Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		SubsystemOceanScanner subsystemOceanScanner = SubsystemTerrain.Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		foreach (Vector3 listenerPosition2 in m_subsystemAudio.ListenerPositions)
		{
			int x = Terrain.ToCell(listenerPosition2.X);
			int y = Terrain.ToCell(listenerPosition2.Y + 0.01f);
			int z = Terrain.ToCell(listenerPosition2.Z);
			FocalorsFluidBlock surfaceFluidBlock = null;
			float? num2 = null;
			if (subsystemFocalorsFluidBlockBehavior != null)
			{
				num2 = subsystemFocalorsFluidBlockBehavior.GetSurfaceHeight(x, y, z, out surfaceFluidBlock);
			}
			if (num2.HasValue && surfaceFluidBlock is FocalorsWaterBlock)
			{
				float num3 = num2.Value;
				if (subsystemOceanScanner != null)
				{
					num3 = subsystemOceanScanner.GetBlockWaveSurfaceY(listenerPosition2, num3);
				}
				if (listenerPosition2.Y < num3 - 0.05f)
				{
					flag2 = true;
					break;
				}
			}
			else if (subsystemOceanScanner != null && listenerPosition2.Y >= 45f && subsystemOceanScanner.IsOcean)
			{
				float waveSurfaceY = subsystemOceanScanner.GetWaveSurfaceY(listenerPosition2);
				if (listenerPosition2.Y < waveSurfaceY - 0.05f)
				{
					flag2 = true;
					break;
				}
			}
		}
		Sound[] array;
		if (flag2)
		{
			float num4 = 0f;
			foreach (Vector3 listenerPosition3 in m_subsystemAudio.ListenerPositions)
			{
				int num5 = Terrain.ToCell(listenerPosition3.X);
				int num6 = Terrain.ToCell(listenerPosition3.Z);
				Vector3 position = new Vector3((float)num5 + 0.5f, listenerPosition3.Y, (float)num6 + 0.5f);
				float windExposureFactor = GetWindExposureFactor(position);
				float num7 = MathUtils.Saturate((listenerPosition3.Y - 45f) / 10f);
				num4 = MathUtils.Max(num4, windExposureFactor * CurrentWindStrength / 16f * num7);
			}
			num4 = MathUtils.Saturate(num4);
			m_waterWindSound.Volume = MathUtils.Lerp(m_waterWindSound.Volume, SettingsManager.SoundsVolume * num4, dt);
			if (m_waterWindSound.Volume > AudioManager.MinAudibleVolume)
			{
				m_waterWindSound.Play();
			}
			else
			{
				m_waterWindSound.Pause();
			}
			array = new Sound[9] { m_lightWarmWindSound, m_averageWarmWindSound, m_stormWarmWindSound, m_lightColdWindSound, m_averageColdWindSound, m_stormColdWindSound, m_windSound, m_hurricaneColdWindSound, m_hurricaneWarmWindSound };
			foreach (Sound sound in array)
			{
				sound.Volume = MathUtils.Lerp(sound.Volume, 0f, dt);
				if (sound.Volume < AudioManager.MinAudibleVolume)
				{
					sound.Pause();
				}
			}
			return;
		}
		m_waterWindSound.Volume = MathUtils.Lerp(m_waterWindSound.Volume, 0f, dt);
		if (m_waterWindSound.Volume < AudioManager.MinAudibleVolume)
		{
			m_waterWindSound.Pause();
		}
		float num8 = 0.2f;
		if (Time.PeriodicEvent(0.5, 0.0))
		{
			MathUtils.Max(0f, (CurrentWindStrength - num8) / 16f);
			float num9 = 0f;
			int num10;
			if (CurrentWindStrength >= 15f)
			{
				num10 = 6;
			}
			else if (CurrentWindStrength >= 13f)
			{
				num10 = 4;
			}
			else
			{
				num10 = ((CurrentWindStrength >= 10f) ? 3 : ((CurrentWindStrength >= 7f) ? 2 : 0));
			}
			foreach (Vector3 listenerPosition4 in m_subsystemAudio.ListenerPositions)
			{
				float num11 = 0f;
				int num12 = Terrain.ToCell(listenerPosition4.X) - num10;
				int num13 = Terrain.ToCell(listenerPosition4.Z) - num10;
				int num14 = Terrain.ToCell(listenerPosition4.X) + num10;
				int num15 = Terrain.ToCell(listenerPosition4.Z) + num10;
				for (int k = num12; k <= num14; k++)
				{
					for (int l = num13; l <= num15; l++)
					{
						Vector3 position2 = new Vector3((float)k + 0.5f, listenerPosition4.Y, (float)l + 0.5f);
						float windExposureFactor2 = GetWindExposureFactor(position2);
						if (windExposureFactor2 > 0f)
						{
							float num16 = position2.X - listenerPosition4.X;
							float num17 = position2.Y - listenerPosition4.Y;
							float num18 = position2.Z - listenerPosition4.Z;
							float distance = MathUtils.Sqrt(num16 * num16 + num17 * num17 + num18 * num18);
							float num19 = MathUtils.Saturate((listenerPosition4.Y - 45f) / 10f);
							float x2 = num8 * windExposureFactor2 * num19 * m_subsystemAudio.CalculateVolume(distance, 3.5f, 4f);
							num11 = MathUtils.Max(num11, x2);
						}
					}
				}
				num9 = MathUtils.Max(num9, num11);
			}
			m_targetWindSoundVolume = MathUtils.Saturate(num9);
		}
		Sound sound2 = null;
		if (CurrentWindStrength >= 13f)
		{
			sound2 = ((GetWindTemperature() > 2f) ? m_hurricaneWarmWindSound : m_hurricaneColdWindSound);
		}
		else if (CurrentWindStrength >= 10f)
		{
			sound2 = ((GetWindTemperature() > 2f) ? m_stormWarmWindSound : m_stormColdWindSound);
		}
		else if (CurrentWindStrength >= 6f)
		{
			sound2 = ((GetWindTemperature() > 2f) ? m_averageWarmWindSound : m_averageColdWindSound);
		}
		else if (CurrentWindStrength >= 1f)
		{
			sound2 = ((GetWindTemperature() > 2f) ? m_lightWarmWindSound : m_lightColdWindSound);
		}
		array = new Sound[8] { m_lightWarmWindSound, m_averageWarmWindSound, m_stormWarmWindSound, m_lightColdWindSound, m_averageColdWindSound, m_stormColdWindSound, m_hurricaneColdWindSound, m_hurricaneWarmWindSound };
		foreach (Sound sound3 in array)
		{
			if (sound3 == sound2)
			{
				sound3.Volume = MathUtils.Lerp(sound3.Volume, SettingsManager.SoundsVolume * m_targetWindSoundVolume, dt);
				sound3.Play();
				continue;
			}
			sound3.Volume = MathUtils.Lerp(sound3.Volume, 0f, dt);
			if (sound3.Volume < AudioManager.MinAudibleVolume)
			{
				sound3.Pause();
			}
		}
		m_windSound.Volume = MathUtils.Lerp(m_windSound.Volume, SettingsManager.SoundsVolume * m_targetWindSoundVolume, dt);
		if (m_windSound.Volume > AudioManager.MinAudibleVolume)
		{
			m_windSound.Play();
		}
		else
		{
			m_windSound.Pause();
		}
	}

	public float GetWindExposureFactor(Vector3 position)
	{
		Vector2 currentWindVector = CurrentWindVector;
		Vector3 windDirection = new Vector3(0f - currentWindVector.X, 0f, 0f - currentWindVector.Y);
		float currentWindStrength = CurrentWindStrength;
		int maxDeflections = GetMaxDeflections(currentWindStrength);
		int num = Array.IndexOf(windDirections, WindDirection);
		if (num < 0)
		{
			num = 0;
		}
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
		{
			return 0f;
		}
		ulong key = MakeWindKey(x, y, z, num, maxDeflections);
		if (m_windExposureCache.TryGetValue(key, out var value))
		{
			if (value == 0)
			{
				return 0f;
			}
			return 1f;
		}
		bool flag = IsExposedToWind(position, windDirection, currentWindStrength, maxDeflections);
		m_windExposureCache[key] = (byte)(flag ? 1u : 0u);
		if (!flag)
		{
			return 0f;
		}
		return 1f;
	}

	private bool IsExposedToWind(Vector3 position, Vector3 windDirection, float currentWindStrength, int deflectionsLeft)
	{
		if (currentWindStrength <= 0f || deflectionsLeft < 0)
		{
			return false;
		}
		float baseBatch = 100f;
		float start = 30f;
		Vector3 count = position + windDirection * start;
		try
		{
			if (!SubsystemTerrain.Raycast(position, count, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) =>
			{
				int num12 = Terrain.ExtractContents(value);
				return BlocksManager.Blocks[num12].IsCollidable;
			}).HasValue)
			{
				Vector3 num2 = position + windDirection * baseBatch;
				if (!SubsystemTerrain.Raycast(count, num2, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) =>
				{
					int num12 = Terrain.ExtractContents(value);
					return BlocksManager.Blocks[num12].IsCollidable;
				}).HasValue)
				{
					return true;
				}
			}
		}
		catch
		{
			return false;
		}
		if (deflectionsLeft == 0)
		{
			return false;
		}
		Vector3[] num4 = new Vector3[6]
		{
			new Vector3(-1f, 0f, 0f),
			new Vector3(1f, 0f, 0f),
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, -1f, 0f),
			new Vector3(0f, 0f, -1f),
			new Vector3(0f, 0f, 1f)
		};
		foreach (Vector3 num6 in num4)
		{
			Vector3 num10 = Vector3.Normalize(windDirection + num6 * 0.7f);
			float num11 = currentWindStrength * 0.5f;
			if (IsExposedToWind(position, num10, num11, deflectionsLeft - 1))
			{
				return true;
			}
		}
		return false;
	}

	private int GetMaxDeflections(float windStrength)
	{
		if (windStrength <= 5f)
		{
			return 0;
		}
		if (windStrength <= 9f)
		{
			return 1;
		}
		if (windStrength <= 12f)
		{
			return 2;
		}
		return 3;
	}

	public static bool IsPlaceHot(int temperature, int y)
	{
		return temperature + GetTemperatureAdjustmentAtHeight(y) >= 8;
	}

	private float GetWindTemperature()
	{
		Vector3 vector = ((m_subsystemAudio.ListenerPositions.Count > 0) ? m_subsystemAudio.ListenerPositions[0] : Vector3.Zero);
		int x = Terrain.ToCell(vector.X);
		int z = Terrain.ToCell(vector.Z);
		SubsystemTerrain subsystemTerrain = SubsystemTerrain;
		if (subsystemTerrain.Terrain.GetChunkAtCell(x, z) != null)
		{
			return subsystemTerrain.Terrain.GetSeasonalTemperature(subsystemTerrain.Terrain.GetShaftValue(x, z));
		}
		return 0f;
	}

	private void InitializeSeason()
	{
		Random cellFace = new Random(m_subsystemGameInfo.WorldSeed);
		switch (m_subsystemGameInfo.WorldSettings.StartingPositionMode)
		{
		case StartingPositionMode.Easy:
			CurrentSeason = (cellFace.Bool() ? Season.Spring : Season.Summer);
			break;
		case StartingPositionMode.Medium:
			CurrentSeason = (cellFace.Bool() ? Season.LateSummer : Season.EarlyAutumn);
			break;
		case StartingPositionMode.Hard:
			CurrentSeason = (cellFace.Bool() ? Season.Anabiosis : Season.Winter);
			break;
		}
		SeasonStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
	}

	public static bool IsPlaceFrozenExtended(int temperature, int y, int threshold)
	{
		return temperature + GetTemperatureAdjustmentAtHeight(y) <= threshold;
	}

	public int GetSnowTemperatureThreshold()
	{
		return CurrentSeason switch
		{
			Season.EarlyAutumn => 1, 
			Season.Autumn => 2, 
			Season.LateAutumn => 3, 
			Season.Anabiosis => 5, 
			Season.Winter => 7, 
			Season.LateWinter => 6, 
			Season.EarlySpring => 3, 
			Season.Spring => 1, 
			_ => 0, 
		};
	}

	private void UpdateChunksAroundPlayer()
	{
		TerrainChunk[] allocatedChunks = SubsystemTerrain.Terrain.AllocatedChunks;
		foreach (TerrainChunk terrainChunk in allocatedChunks)
		{
			if (terrainChunk.State >= TerrainChunkState.InvalidVertices1)
			{
				SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(terrainChunk.Coords, 0, TerrainChunkState.InvalidLight, forceGeometryRegeneration: true);
			}
		}
	}

	private bool IsSkyVisibleThroughGlass(int x, int y, int z, int yLimit)
	{
		for (int i = y + 1; i <= yLimit; i++)
		{
			int num = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, i, z));
			if (BlocksManager.Blocks[num].IsCollidable)
			{
				return false;
			}
		}
		return true;
	}

	public void DefrostingMeltingRemovingSnow(TerrainChunk chunk)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		Terrain num = SubsystemTerrain.Terrain;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int vector = chunk.GetTopHeightFast(i, j);
				int flag = chunk.GetCellValueFast(i, vector, j);
				int i2 = Terrain.ExtractContents(flag);
				int particle = chunk.Origin.X + i;
				int vector2 = vector;
				int z = chunk.Origin.Y + j;
				int seasonalTemperature = num.GetSeasonalTemperature(num.GetShaftValue(particle, z));
				if ((CurrentSeason != Season.EarlySpring || seasonalTemperature < 4) && (CurrentSeason != Season.Spring || seasonalTemperature < 2) && ((CurrentSeason != Season.EarlySummer && CurrentSeason != Season.Summer && CurrentSeason != Season.LateSummer) || seasonalTemperature < 1))
				{
					continue;
				}
				if (i2 == 62 && !IsPlaceFrozen(seasonalTemperature, vector2))
				{
					SubsystemTerrain.ChangeCell(particle, vector2, z, Terrain.MakeBlockValue(18));
					continue;
				}
				if (i2 == 352 && !IsPlaceFrozen(seasonalTemperature, vector2))
				{
					float sizeFromValue = LayeredIceBlock.GetSizeFromValue(flag);
					float level = MathUtils.Saturate(1f - sizeFromValue);
					int data = FocalorsFluidBlock.SetLevel(0, level);
					data = FocalorsFluidBlock.SetIsTop(data, isTop: true);
					int value = Terrain.MakeBlockValue(18, 0, data);
					SubsystemTerrain.ChangeCell(particle, vector2, z, value);
					continue;
				}
				if (i2 == 61 && vector2 < 100)
				{
					float sizeFromValue2 = SnowBlock.GetSizeFromValue(flag);
					if (m_random.Float(0f, 1f) < 0.9f)
					{
						sizeFromValue2 -= 0.07f;
						if (sizeFromValue2 <= 0f)
						{
							SubsystemTerrain.ChangeCellFluid(particle, vector2, z, 0);
						}
						else
						{
							SubsystemTerrain.ChangeCellFluid(particle, vector2, z, SnowBlock.SetSizeInValue(flag, sizeFromValue2));
						}
					}
					continue;
				}
				switch (i2)
				{
				case 348:
					SubsystemTerrain.ChangeCellFluid(particle, vector2, z, SnowBlock.SetSizeInValue(Terrain.MakeBlockValue(61), 0.93f));
					break;
				case 350:
					if (Terrain.ExtractContents(num.GetCellValue(particle, vector2 + 1, z)) != 348)
					{
						SubsystemTerrain.ChangeCellFluid(particle, vector2, z, Terrain.MakeBlockValue(348));
					}
					break;
				}
			}
		}
	}

	private Season GetNextSeason(Season s)
	{
		return s switch
		{
			Season.EarlyAutumn => Season.Autumn, 
			Season.Autumn => Season.LateAutumn, 
			Season.LateAutumn => Season.Anabiosis, 
			Season.Anabiosis => Season.Winter, 
			Season.Winter => Season.LateWinter, 
			Season.LateWinter => Season.EarlySpring, 
			Season.EarlySpring => Season.Spring, 
			Season.Spring => Season.LateSpring, 
			Season.LateSpring => Season.EarlySummer, 
			Season.EarlySummer => Season.Summer, 
			Season.Summer => Season.LateSummer, 
			_ => Season.EarlyAutumn, 
		};
	}

	private void UpdateCyclone(float dt)
	{
		if (!IsCycloneActive)
		{
			return;
		}
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		if (totalElapsedGameTime >= CycloneEndTime || CurrentWindStrength <= 12f)
		{
			IsCycloneActive = false;
			ActiveTornadoes.Clear();
			TornadoSpawnPositions.Clear();
			TornadoSpawnTimes.Clear();
			TornadoSpeedMultipliers.Clear();
			return;
		}
		Vector2 cycloneCenter = CycloneCenter;
		Vector2 vector = CycloneDirection;
		float num = vector.X * vector.X + vector.Y * vector.Y;
		if (num > 1E-12f)
		{
			vector *= 1f / MathUtils.Sqrt(num);
		}
		else
		{
			vector = Vector2.UnitY;
		}
		CycloneCenter += vector * dt;
		Vector2 vector2 = CycloneCenter - cycloneCenter;
		float num2 = MathUtils.Max(50f, CycloneRadius * 0.05f);
		double totalElapsedGameTime2 = m_subsystemGameInfo.TotalElapsedGameTime;
		float num3 = 0.4f + 0.8f * (CurrentWindStrength / 16f);
		float num4 = MathUtils.Max(120f, CycloneRadius * 0.02f);
		float num5 = num4 * num4;
		float num6 = MathUtils.Lerp(60f, 160f, MathUtils.Saturate(CurrentWindStrength / 16f));
		for (int i = 0; i < ActiveTornadoes.Count; i++)
		{
			Vector2 vector3 = ActiveTornadoes[i];
			vector3 += vector2;
			Vector2 vector4 = vector3 - CycloneCenter;
			float num7 = vector4.X * vector4.X + vector4.Y * vector4.Y;
			if (num7 > 1E-12f)
			{
				float num8 = 1f / MathUtils.Sqrt(num7);
				Vector2 vector5 = new Vector2((0f - vector4.Y) * num8, vector4.X * num8);
				float num9 = 0.2f * MathUtils.Sin((float)(totalElapsedGameTime2 * 0.5) + (vector3.X + vector3.Y) * 0.01f + (float)i * 0.7f);
				float num10 = 1f;
				if (i < TornadoSpeedMultipliers.Count)
				{
					num10 = TornadoSpeedMultipliers[i];
				}
				vector3 += vector5 * (num3 + num9) * dt * num10;
			}
			Vector2 zero = Vector2.Zero;
			for (int j = 0; j < ActiveTornadoes.Count; j++)
			{
				if (j != i)
				{
					Vector2 vector6 = ActiveTornadoes[j];
					float num11 = vector3.X - vector6.X;
					float num12 = vector3.Y - vector6.Y;
					float num13 = num11 * num11 + num12 * num12;
					if (num13 > 1E-08f && num13 < num5)
					{
						float num14 = MathUtils.Sqrt(num13);
						float num15 = 1f - num14 / num4;
						float num16 = 1f / MathUtils.Max(num14, 0.001f);
						zero += new Vector2(num11 * num16, num12 * num16) * num15;
					}
				}
			}
			if (zero.X != 0f || zero.Y != 0f)
			{
				float num17 = zero.X * zero.X + zero.Y * zero.Y;
				if (num17 > 1E-12f)
				{
					zero *= 1f / MathUtils.Sqrt(num17);
				}
				vector3 += zero * (num6 * dt);
			}
			float num18 = CycloneRadius - num2 * 0.25f;
			if (num18 < 10f)
			{
				num18 = 10f;
			}
			float num19 = num18 * num18;
			Vector2 vector7 = vector3 - CycloneCenter;
			float num20 = vector7.X * vector7.X + vector7.Y * vector7.Y;
			if (num20 > num19)
			{
				vector3 = ((!(num20 > 1E-12f)) ? (CycloneCenter + new Vector2(num18, 0f)) : (CycloneCenter + vector7 * (num18 / MathUtils.Sqrt(num20))));
			}
			ActiveTornadoes[i] = vector3;
		}
		if (SubsystemTime.PeriodicGameTimeEvent(0.5, 0.0))
		{
			for (int k = 0; k < ActiveTornadoes.Count; k++)
			{
				ApplyTornadoGroundEffect(ActiveTornadoes[k]);
			}
		}
		if (totalElapsedGameTime >= m_nextTornadoCheckTime)
		{
			m_nextTornadoCheckTime = totalElapsedGameTime + 30.0;
			if (m_random.Float(0f, 1f) <= 0.9f)
			{
				int tornadoCapacityByRadius = GetTornadoCapacityByRadius(CycloneRadius);
				if (ActiveTornadoes.Count < tornadoCapacityByRadius && TryPickTornadoSpawnPosition(out var pos))
				{
					ActiveTornadoes.Add(pos);
					TornadoSpawnPositions.Add(pos);
					TornadoSpawnTimes.Add(totalElapsedGameTime);
					TornadoSpeedMultipliers.Add(m_random.Float(0.85f, 1.15f));
				}
			}
		}
		float num21 = CycloneRadius + num2;
		float num22 = num21 * num21;
		for (int num23 = ActiveTornadoes.Count - 1; num23 >= 0; num23--)
		{
			Vector2 vector8 = ActiveTornadoes[num23];
			float num24 = vector8.X - CycloneCenter.X;
			float num25 = vector8.Y - CycloneCenter.Y;
			if (num24 * num24 + num25 * num25 > num22)
			{
				ActiveTornadoes.RemoveAt(num23);
				if (num23 < TornadoSpawnPositions.Count)
				{
					TornadoSpawnPositions.RemoveAt(num23);
				}
				if (num23 < TornadoSpawnTimes.Count)
				{
					TornadoSpawnTimes.RemoveAt(num23);
				}
				if (num23 < TornadoSpeedMultipliers.Count)
				{
					TornadoSpeedMultipliers.RemoveAt(num23);
				}
			}
		}
	}

	public void ActivateCyclone()
	{
		IsCycloneActive = true;
		CycloneStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
		Vector2 cycloneDirection = CurrentWindVector;
		float num = cycloneDirection.X * cycloneDirection.X + cycloneDirection.Y * cycloneDirection.Y;
		if (num > 1E-12f)
		{
			cycloneDirection *= 1f / MathUtils.Sqrt(num);
		}
		else
		{
			cycloneDirection = Vector2.UnitY;
		}
		CycloneDirection = cycloneDirection;
		CycloneRadius = m_random.Float(1000f, 10000f);
		if (!TryPickCycloneSpawnNearPlayers(out var pos))
		{
			Vector2 vector;
			bool flag;
			do
			{
				vector = new Vector2(m_random.Float(-100000f, 100000f), m_random.Float(-100000f, 100000f));
				flag = true;
				foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
				{
					Vector3 position = componentPlayer.ComponentBody.Position;
					if (Vector2.Distance(vector, new Vector2(position.X, position.Z)) < 1000f)
					{
						flag = false;
						break;
					}
				}
			}
			while (!flag);
			pos = vector;
		}
		CycloneCenter = pos;
		CycloneOrigin = pos;
		CycloneSpawnTime = CycloneStartTime;
		double num2 = Math.Max(1.0, WindEndTime - WindStartTime);
		CycloneEndTime = CycloneStartTime + num2 * 2.0;
		ActiveTornadoes.Clear();
		TornadoSpawnPositions.Clear();
		TornadoSpawnTimes.Clear();
		TornadoSpeedMultipliers.Clear();
		m_nextTornadoCheckTime = CycloneStartTime + 30.0;
	}

	private bool CanStrikePlayer(ComponentPlayer player)
	{
		if (player == null)
		{
			return false;
		}
		ComponentMiner componentMiner = player.Entity.FindComponent<ComponentMiner>();
		if (componentMiner == null)
		{
			return false;
		}
		int activeBlockValue = componentMiner.ActiveBlockValue;
		if (Terrain.ExtractContents(activeBlockValue) != 379)
		{
			return false;
		}
		if ((Terrain.ExtractData(activeBlockValue) & 1) == 0)
		{
			return false;
		}
		Vector3 position = player.ComponentBody.Position;
		if (position.Y <= 80f)
		{
			return false;
		}
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y + 1f);
		int z = Terrain.ToCell(position.Z);
		return IsSkyVisibleThroughGlass(x, y, z, 255);
	}

	private static ulong MakeWindKey(int x, int y, int z, int dirIndex, int deflections)
	{
		return (ushort)x | ((ulong)(ushort)z << 16) | ((ulong)(ushort)y << 32) | ((ulong)(byte)dirIndex << 48) | ((ulong)(byte)deflections << 56);
	}

	public void ComputeWindParameters(out float windBlocked, out float windStrength)
	{
		bool num = MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(0f, m_windSound.Volume), m_waterWindSound.Volume), m_lightWarmWindSound.Volume), m_averageWarmWindSound.Volume), m_stormWarmWindSound.Volume), m_lightColdWindSound.Volume), m_averageColdWindSound.Volume), m_stormColdWindSound.Volume), m_hurricaneColdWindSound.Volume), m_hurricaneWarmWindSound.Volume) > AudioManager.MinAudibleVolume;
		bool num2 = false;
		if (!num && m_subsystemAudio != null)
		{
			foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
			{
				int num4 = Terrain.ToCell(listenerPosition.X);
				int num5 = Terrain.ToCell(listenerPosition.Z);
				Vector3 position = new Vector3((float)num4 + 0.5f, listenerPosition.Y, (float)num5 + 0.5f);
				if (GetWindExposureFactor(position) > 0f)
				{
					num2 = true;
					break;
				}
			}
		}
		bool currentSmokeLevel = num | num2;
		windBlocked = (currentSmokeLevel ? 0f : 1f);
		windStrength = (currentSmokeLevel ? CurrentWindStrength : 0f);
	}

	private int GetTornadoCapacityByRadius(float r)
	{
		if (r < 3000f)
		{
			return 1;
		}
		if (r < 9000f)
		{
			return 3;
		}
		return 5;
	}

	private bool TryPickTornadoSpawnPosition(out Vector2 pos)
	{
		pos = Vector2.Zero;
		float cycloneRadius = CycloneRadius;
		if (cycloneRadius <= 0f)
		{
			return false;
		}
		float num = MathUtils.Max(0.1f * cycloneRadius, 50f);
		float num2 = 0.6f * cycloneRadius;
		float num3 = num2;
		float num4 = 0.8f * cycloneRadius;
		float num5 = MathUtils.Max(120f, cycloneRadius * 0.02f);
		float num6 = num5 * num5;
		for (int i = 0; i < 24; i++)
		{
			bool flag = m_random.Float(0f, 1f) >= 0.75f;
			float num7 = (flag ? num3 : num);
			float num8 = (flag ? num4 : num2);
			if (!(num8 > num7))
			{
				continue;
			}
			float f = m_random.Float(0f, 1f);
			float num9 = MathUtils.Sqrt(MathUtils.Lerp(num7 * num7, num8 * num8, f));
			float x = m_random.Float(0f, 6.283185f);
			Vector2 vector = CycloneCenter + new Vector2(MathUtils.Cos(x), MathUtils.Sin(x)) * num9;
			bool flag2 = true;
			for (int j = 0; j < ActiveTornadoes.Count; j++)
			{
				Vector2 vector2 = ActiveTornadoes[j];
				float num10 = vector.X - vector2.X;
				float num11 = vector.Y - vector2.Y;
				if (num10 * num10 + num11 * num11 < num6)
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				pos = vector;
				return true;
			}
		}
		float num12 = -1f;
		Vector2 vector3 = CycloneCenter;
		for (int k = 0; k < 32; k++)
		{
			bool flag3 = m_random.Float(0f, 1f) >= 0.75f;
			float num13 = (flag3 ? num3 : num);
			float num14 = (flag3 ? num4 : num2);
			if (!(num14 > num13))
			{
				continue;
			}
			float f2 = m_random.Float(0f, 1f);
			float num15 = MathUtils.Sqrt(MathUtils.Lerp(num13 * num13, num14 * num14, f2));
			float x2 = m_random.Float(0f, 6.283185f);
			Vector2 vector4 = CycloneCenter + new Vector2(MathUtils.Cos(x2), MathUtils.Sin(x2)) * num15;
			float num16 = float.MaxValue;
			for (int l = 0; l < ActiveTornadoes.Count; l++)
			{
				Vector2 vector5 = ActiveTornadoes[l];
				float num17 = vector4.X - vector5.X;
				float num18 = vector4.Y - vector5.Y;
				float num19 = num17 * num17 + num18 * num18;
				if (num19 < num16)
				{
					num16 = num19;
				}
			}
			if (num16 > num12)
			{
				num12 = num16;
				vector3 = vector4;
			}
		}
		if (num12 > 0f)
		{
			pos = vector3;
			return true;
		}
		return false;
	}

	private void ApplyTornadoGroundEffect(Vector2 pos)
	{
		int num = Terrain.ToCell(pos.X);
		int num2 = Terrain.ToCell(pos.Y);
		int num3 = 7;
		int num4 = 1;
		for (int i = -num3; i <= num3; i++)
		{
			for (int j = -num4; j <= num4; j++)
			{
				ProcessTornadoCell(num + i, num2 + j);
			}
		}
		for (int k = -num3; k <= num3; k++)
		{
			for (int l = -num4; l <= num4; l++)
			{
				ProcessTornadoCell(num + l, num2 + k);
			}
		}
	}

	private void ProcessTornadoCell(int x, int z)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		Terrain terrain = SubsystemTerrain.Terrain;
		int num = terrain.CalculateTopmostCellHeight(x, z);
		switch (Terrain.ExtractContents(terrain.GetCellValue(x, num, z)))
		{
		case 19:
			SubsystemTerrain.DestroyCell(0, x, num, z, 0, noDrop: true, noParticleSystem: true);
			return;
		case 8:
			SubsystemTerrain.ChangeCell(x, num, z, Terrain.MakeBlockValue(2));
			return;
		}
		int num2 = num - 1;
		if (num2 >= 0 && Terrain.ExtractContents(terrain.GetCellValue(x, num2, z)) == 8)
		{
			SubsystemTerrain.ChangeCell(x, num2, z, Terrain.MakeBlockValue(2));
		}
	}

	private bool TryPickCycloneSpawnNearPlayers(out Vector2 pos)
	{
		pos = Vector2.Zero;
		if (m_subsystemPlayers == null || m_subsystemPlayers.ComponentPlayers.Count == 0)
		{
			return false;
		}
		List<Vector2> list = new List<Vector2>();
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			if (componentPlayer != null && componentPlayer.ComponentBody != null)
			{
				Vector3 position = componentPlayer.ComponentBody.Position;
				list.Add(new Vector2(position.X, position.Z));
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		Vector2 vector = CurrentWindVector;
		float num = vector.X * vector.X + vector.Y * vector.Y;
		if (num > 1E-12f)
		{
			vector *= 1f / MathUtils.Sqrt(num);
		}
		else
		{
			vector = Vector2.UnitY;
		}
		Vector2 vector2 = new Vector2(0f - vector.Y, vector.X);
		float min = 1500f;
		float max = 5000f;
		float num2 = 400f;
		float num3 = 800f;
		float num4 = -100000f;
		float num5 = 100000f;
		Vector2 vector3 = Vector2.Zero;
		float num6 = -1f;
		for (int i = 0; i < 80; i++)
		{
			Vector2 vector4 = list[m_random.Int(0, list.Count - 1)];
			float num7 = m_random.Float(min, max);
			float num8 = m_random.Float(0f - num3, num3);
			Vector2 vector5 = vector4 - vector * num7 + vector2 * num8;
			if (!(vector5.X >= num4) || !(vector5.X <= num5) || !(vector5.Y >= num4) || !(vector5.Y <= num5))
			{
				continue;
			}
			bool flag = true;
			float num9 = float.MaxValue;
			for (int j = 0; j < list.Count; j++)
			{
				float num10 = Vector2.Distance(vector5, list[j]);
				if (num10 < num2)
				{
					flag = false;
					break;
				}
				if (num10 < num9)
				{
					num9 = num10;
				}
			}
			if (flag)
			{
				pos = vector5;
				return true;
			}
			if (num9 > num6)
			{
				num6 = num9;
				vector3 = vector5;
			}
		}
		if (num6 >= 0f)
		{
			pos = vector3;
			return true;
		}
		return false;
	}

	private void UpdateCycloneFlybyForDraw(Camera camera)
	{
		if (!SettingsManager.DisableCycloneFlybyDebris)
		{
			return;
		}
		if (m_cycloneFlybyByWidget == null)
		{
			m_cycloneFlybyByWidget = new Dictionary<GameWidget, CycloneDebrisFlybyParticleSystem>();
		}
		if (!m_cycloneFlybyByWidget.TryGetValue(camera.GameWidget, out var value) || value == null)
		{
			value = new CycloneDebrisFlybyParticleSystem(SubsystemTerrain, camera.ViewPosition, 120f);
			value.MaxVisibilityDistance = 120f;
			value.LifetimeMinMax = new Vector2(2.2f, 5f);
			value.ParticleSize = 0.45f;
			value.BaseAlpha = 220f;
			value.SpawnAreaHalfSize = new Vector3(10f, 1.8f, 0.8f);
			value.SpawnBackOffset = 8f;
			value.SpawnRatePerSecond = 0f;
			m_subsystemParticles.AddParticleSystem(value);
			m_cycloneFlybyByWidget[camera.GameWidget] = value;
		}
		Vector3 vector = (value.AnchorPosition = new Vector3(camera.ViewPosition.X, camera.ViewPosition.Y + 1.2f, camera.ViewPosition.Z));
		if (GetWindExposureFactor(vector) <= 0f)
		{
			value.SpawnRatePerSecond = 0f;
			return;
		}
		value.m_anchorPosition = vector;
		if (!IsCycloneActive)
		{
			value.SpawnRatePerSecond = 0f;
			return;
		}
		Vector2 vector3 = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
		float num = vector3.X - CycloneCenter.X;
		float num2 = vector3.Y - CycloneCenter.Y;
		if (num * num + num2 * num2 > (CycloneRadius + 5f) * (CycloneRadius + 5f))
		{
			value.SpawnRatePerSecond = 0f;
			return;
		}
		Vector2 vector4 = CurrentWindVector;
		if (MathUtils.Sqrt(vector4.X * vector4.X + vector4.Y * vector4.Y) < 1E-06f)
		{
			vector4 = Vector2.UnitY;
		}
		Vector3 vector5 = new Vector3(vector4.X, 0f, vector4.Y);
		float num3 = MathUtils.Lerp(6f, 20f, MathUtils.Saturate((CurrentWindStrength - 1f) / 15f));
		value.WindVelocity = vector5 * num3;
		float num4 = 120f;
		float num5 = float.MaxValue;
		for (int i = 0; i < ActiveTornadoes.Count; i++)
		{
			Vector2 vector6 = ActiveTornadoes[i];
			float num6 = vector3.X - vector6.X;
			float num7 = vector3.Y - vector6.Y;
			float num8 = MathUtils.Sqrt(num6 * num6 + num7 * num7);
			if (num8 < num5)
			{
				num5 = num8;
			}
		}
		float f = ((num5 < float.MaxValue) ? MathUtils.Saturate(1f - num5 / num4) : 0f);
		float x = 80f;
		float num9 = 0.9f + 0.2f * m_random.Float(0f, 1f);
		float spawnRatePerSecond = MathUtils.Lerp(40f, x, f) * num9;
		value.SpawnRatePerSecond = spawnRatePerSecond;
		value.SpawnBackOffset = MathUtils.Lerp(6f, 12f, MathUtils.Saturate((CurrentWindStrength - 1f) / 15f));
	}

	public void ProcessFluidsWeatherInteraction(TerrainChunk chunk)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		GetSeasonalChance(out var helpTopic, out var waterChance);
		int num = 65 + m_subsystemGameInfo.WorldSettings.SeaLevelOffset;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num2 = chunk.Origin.X + i;
				int num3 = chunk.Origin.Y + j;
				PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(num2, num3);
				bool flag = precipitationShaftInfo.Intensity > 0.5f;
				int topHeightFast = chunk.GetTopHeightFast(i, j);
				if (topHeightFast < precipitationShaftInfo.YLimit - 1)
				{
					continue;
				}
				int cellValueFast = chunk.GetCellValueFast(i, topHeightFast, j);
				int num4 = Terrain.ExtractContents(cellValueFast);
				if ((num4 == 92) & flag)
				{
					if (m_random.Int(0, helpTopic) == 0)
					{
						if (FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast)) <= 0.3f)
						{
							int contents = (m_random.Bool(0.5f) ? 3 : 67);
							SubsystemTerrain.ChangeCellFluid(num2, topHeightFast, num3, Terrain.MakeBlockValue(contents));
						}
						else
						{
							SubsystemTerrain.ChangeCellFluid(num2, topHeightFast, num3, Terrain.MakeBlockValue(0));
						}
					}
				}
				else if (num4 == 18 && !flag && topHeightFast > num && m_random.Int(0, waterChance) == 0)
				{
					int data = Terrain.ExtractData(cellValueFast);
					float num5 = FocalorsFluidBlock.GetLevel(data) + 0.05f;
					if (num5 >= 0.99f)
					{
						SubsystemTerrain.ChangeCellFluid(num2, topHeightFast, num3, Terrain.MakeBlockValue(0));
						continue;
					}
					int data2 = FocalorsFluidBlock.SetLevel(data, num5);
					data2 = FocalorsFluidBlock.SetIsTop(data2, isTop: true);
					SubsystemTerrain.ChangeCellFluid(num2, topHeightFast, num3, Terrain.ReplaceData(cellValueFast, data2));
					m_subsystemParticles.AddParticleSystem(new BreathSteamParticleSystem(SubsystemTerrain, this)
					{
						Position = new Vector3((float)num2 + 0.5f, (float)topHeightFast + 1f, (float)num3 + 0.5f),
						Direction = Vector3.UnitY,
						AlphaMultiplier = 0.1f,
						BaseSize = 0.15f
					});
				}
			}
		}
	}

	private void GetSeasonalChance(out int magmaChance, out int waterChance)
	{
		switch (CurrentSeason)
		{
		case Season.EarlyAutumn:
		case Season.Autumn:
		case Season.LateAutumn:
			magmaChance = 10;
			waterChance = 80;
			break;
		case Season.Anabiosis:
		case Season.Winter:
		case Season.LateWinter:
			magmaChance = 5;
			waterChance = 150;
			break;
		case Season.EarlySpring:
		case Season.Spring:
		case Season.LateSpring:
			magmaChance = 40;
			waterChance = 50;
			break;
		case Season.EarlySummer:
		case Season.Summer:
		case Season.LateSummer:
			magmaChance = 100;
			waterChance = 15;
			break;
		default:
			magmaChance = 50;
			waterChance = 50;
			break;
		}
	}

	public bool GetTheoreticalWindExposure(Vector3 position)
	{
		int x = Terrain.ToCell(position.X);
		int viewDirection = Terrain.ToCell(position.Y);
		int vector = Terrain.ToCell(position.Z);
		float vector2 = 16f;
		int i = GetMaxDeflections(vector2);
		ulong particle = MakeWindKey(x, viewDirection, vector, 255, i);
		if (m_windExposureCache.TryGetValue(particle, out var num))
		{
			return num > 0;
		}
		bool num3 = false;
		foreach (Vector2 num5 in WindDirectionVectors.Values)
		{
			if (IsExposedToWind(windDirection: new Vector3(0f - num5.X, 0f, 0f - num5.Y), position: position, currentWindStrength: vector2, deflectionsLeft: i))
			{
				num3 = true;
				break;
			}
		}
		m_windExposureCache[particle] = (byte)(num3 ? 1u : 0u);
		return num3;
	}
}
