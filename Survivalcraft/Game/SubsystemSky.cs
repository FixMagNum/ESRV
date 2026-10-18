using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemSky : Subsystem, IDrawable, IUpdateable
{
	public struct SkyVertex
	{
		public Vector3 Position;

		public Color Color;
	}

	public class SkyDome : IDisposable
	{
		public const int VerticesCountX = 10;

		public const int VerticesCountY = 8;

		public float? LastUpdateTimeOfDay;

		public float? LastUpdatePrecipitationIntensity;

		public int? LastUpdateTemperature;

		public float LastUpdateLightningStrikeBrightness;

		public SkyVertex[] Vertices = new SkyVertex[80];

		public ushort[] Indices = new ushort[444];

		public VertexBuffer VertexBuffer;

		public IndexBuffer IndexBuffer;

		private float m_lastWindStrength;

		private string m_lastWindDirection;

		public void Dispose()
		{
			Utilities.Dispose(ref VertexBuffer);
			Utilities.Dispose(ref IndexBuffer);
		}
	}

	public struct StarVertex
	{
		public Vector3 Position;

		public Vector2 TextureCoordinate;

		public Color Color;
	}

	private class TornadoTrack
	{
		public Vector2 Pos;

		public double Start;

		public bool Seen;
	}

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public SubsystemTime m_subsystemTime;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemBodies m_subsystemBodies;

	public PrimitivesRenderer2D m_primitivesRenderer2d;

	public PrimitivesRenderer3D m_primitivesRenderer3d;

	public Random m_random;

	public Color m_viewFogColor;

	public Vector2 m_viewFogRange;

	public bool m_viewIsSkyVisible;

	public Texture2D m_sunTexture;

	public Texture2D m_glowTexture;

	public Texture2D m_cloudsTexture;

	public Texture2D[] m_moonTextures;

	public static UnlitShader m_shaderFlat;

	public static UnlitShader m_shaderTextured;

	public VertexDeclaration m_skyVertexDeclaration;

	public Dictionary<GameWidget, SkyDome> m_skyDomes;

	public VertexBuffer m_starsVertexBuffer;

	public IndexBuffer m_starsIndexBuffer;

	public VertexDeclaration m_starsVertexDeclaration;

	public const int m_starsCount = 150;

	public Vector3? m_lightningStrikePosition;

	public float m_lightningStrikeBrightness;

	public double m_lastLightningStrikeTime;

	public const float DawnStart = 0.2f;

	public const float DayStart = 0.3f;

	public const float DuskStart = 0.7f;

	public const float NightStart = 0.8f;

	public bool DrawSkyEnabled;

	public bool DrawCloudsWireframe;

	public bool FogEnabled;

	public int[] m_drawOrders;

	public float[] m_cloudsLayerRadii;

	public Color[] m_cloudsLayerColors;

	public static int[] m_lightValuesMoonless;

	public static int[] m_lightValuesNormal;

	public bool IsSolarEclipse;

	private float m_previousDay;

	private Texture2D m_regularSunTexture;

	private Texture2D m_eclipseSunTexture;

	private Color m_solarEclipseSkyColor;

	private Texture2D[] m_cloudTextures;

	private int m_currentDay;

	private Texture2D m_nullMoonTexture;

	private Texture2D m_downHalfSunTexture;

	private Texture2D m_upHalfSunTexture;

	private static readonly Dictionary<string, Vector2> WindDirectionVectors;

	private float m_lastWindStrength;

	private string m_lastWindDirection;

	private SubsystemPlayers m_subsystemPlayers;

	private float m_cloudTransitionTimer;

	private float m_cloudTransitionDuration;

	private int m_currentCloudTextureType;

	private int m_targetCloudTextureType;

	private float[] m_currentHeights;

	private float[] m_oldHeights;

	private float[] m_targetHeights;

	private int m_oldCloudTextureType;

	private Vector2[] m_cloudLayerOffsets;

	private Vector2[] m_cloudOffsets;

	private int m_oldCloudStage;

	private int m_targetCloudStage;

	private float m_cloudStageTransitionTimer;

	private float m_cloudStageTransitionDuration;

	private float[] m_oldStageAlphas;

	private float[] m_targetStageAlphas;

	private float[] m_currentStageAlphas;

	private Vector2[] m_dustOffsets;

	private Vector2[] m_dustLayerOffsets;

	private readonly float[] m_dustLayerHeights;

	private readonly float[] m_dustLayerSpeedFactors;

	private Texture2D m_dustTexture;

	private int m_activeDustLayers;

	private readonly float[,,] m_dustExposure;

	private SubsystemOceanScanner m_subsystemOceanScanner;

	public byte m_currentLightningType;

	private DynamicArray<Vector3> m_lightMain;

	private DynamicArray<DynamicArray<Vector3>> m_lightBranches;

	private uint m_lightSeed;

	private double m_lightStartTime;

	private float m_lightLifetime;

	private float m_lightThickness;

	private float m_lightVisualDuration;

	private Vector2[] m_tornadoLayerOffsets;

	private float[] m_tornadoLayerRadiusFactors;

	private float[] m_tornadoLayerSpeedFactors;

	private readonly float[] m_tornadoLayerAlphaFactors;

	private float[] m_cycloneLayerRadiusFactors;

	private float[] m_cycloneLayerSpeedFactors;

	private float[] m_cycloneLayerAlphaFactors;

	private Vector2[] m_cycloneLayerUvOffsets;

	private Vector2[] m_cycloneLayerCenterOffsets;

	private float m_cycloneBaseAngle;

	private Texture2D m_sandWallTexture;

	private Texture2D m_snowWallTexture;

	private Texture2D m_rainWallTexture;

	private Vector3 m_lastCameraViewPos;

	private float m_stormGloomTemporal;

	private List<TornadoDustParticleSystem> m_tornadoDustSystems;

	private List<TornadoDebrisParticleSystem> m_tornadoDebrisSystems;

	public SubsystemParticles m_subsystemParticles;

	private const int m_cycloneCylindersCount = 6;

	private float[] m_cycloneCylinderAngleOffsets;

	private float[] m_cycloneCylinderRadialFactors;

	private float[] m_cycloneCylinderRotationDirs;

	private Vector2[] m_cycloneCylinderUvOffsets;

	private double m_cycloneStartWorldTime;

	private const float CycloneSpawnDuration = 300f;

	private const float TornadoRevealDuration = 60f;

	private readonly List<TornadoTrack> m_tornadoTracks;

	public SubsystemFocalorsFluidBlockBehavior m_subsystemFluidBlockBehavior;

	private float m_smoothedQuicksandLight;

	private float m_rawQuicksandLight;

	private float m_quicksandLightStableTime;

	public float SkyLightIntensity { get; set; }

	public int MoonPhase { get; set; }

	public int SkyLightValue { get; set; }

	public float VisibilityRange { get; set; }

	public float VisibilityRangeYMultiplier { get; set; }

	public float ViewUnderWaterDepth { get; set; }

	public float ViewUnderMagmaDepth { get; set; }

	public Color ViewFogColor => m_viewFogColor;

	public Vector2 ViewFogRange => m_viewFogRange;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => m_drawOrders;

	public Vector2 CloudDirection { get; set; }

	public float CloudSpeed { get; set; }

	public float ViewUnderQuicksandDepth { get; set; }

	public void MakeLightningStrike(Vector3 targetPosition)
	{
		MakeLightningStrike(targetPosition, 0);
	}

	public void Update(float dt)
	{
		float[] array = new float[3] { 1f, 0.8f, 0.6f };
		int num = (int)m_subsystemTimeOfDay.Day % 30;
		if (num == 27 || num == 28 || num == 29)
		{
			MoonPhase = 4;
		}
		else
		{
			MoonPhase = ((int)MathUtils.Floor((float)m_subsystemTimeOfDay.Day - 0.5f + 5f) % 8 + 8) % 8;
		}
		if ((int)m_subsystemTimeOfDay.Day != (int)m_previousDay)
		{
			m_previousDay = (float)m_subsystemTimeOfDay.Day;
			IsSolarEclipse = num == 28;
		}
		float c = m_subsystemWeather.CurrentWindStrength;
		string windDirection = m_subsystemWeather.WindDirection;
		UpdateCloudMovement(c, windDirection);
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		if (m_subsystemWeather.IsCycloneActive)
		{
			if (m_cycloneStartWorldTime < 0.0)
			{
				m_cycloneStartWorldTime = totalElapsedGameTime;
			}
		}
		else
		{
			m_cycloneStartWorldTime = -1.0;
		}
		List<Vector2> activeTornadoes = m_subsystemWeather.ActiveTornadoes;
		if (activeTornadoes != null)
		{
			for (int i = 0; i < m_tornadoTracks.Count; i++)
			{
				m_tornadoTracks[i].Seen = false;
			}
			float num2 = 48f * 48f;
			for (int j = 0; j < activeTornadoes.Count; j++)
			{
				Vector2 vector = activeTornadoes[j];
				int num3 = -1;
				float num4 = float.MaxValue;
				for (int k = 0; k < m_tornadoTracks.Count; k++)
				{
					if (!m_tornadoTracks[k].Seen)
					{
						float num5 = Vector2.DistanceSquared(m_tornadoTracks[k].Pos, vector);
						if (num5 < num4)
						{
							num4 = num5;
							num3 = k;
						}
					}
				}
				if (num3 >= 0 && num4 <= num2)
				{
					m_tornadoTracks[num3].Pos = vector;
					m_tornadoTracks[num3].Seen = true;
					continue;
				}
				m_tornadoTracks.Add(new TornadoTrack
				{
					Pos = vector,
					Start = totalElapsedGameTime,
					Seen = true
				});
			}
			for (int num6 = m_tornadoTracks.Count - 1; num6 >= 0; num6--)
			{
				if (!m_tornadoTracks[num6].Seen)
				{
					m_tornadoTracks.RemoveAt(num6);
				}
			}
		}
		else
		{
			m_tornadoTracks.Clear();
		}
		for (int l = 0; l < 3; l++)
		{
			m_cloudOffsets[l] += CloudDirection * CloudSpeed * dt * array[l];
			m_cloudOffsets[l] = new Vector2(MathUtils.Remainder(m_cloudOffsets[l].X, 1f), MathUtils.Remainder(m_cloudOffsets[l].Y, 1f));
		}
		UpdateLightAndViewParameters();
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		double totalElapsedGameTime2 = m_subsystemGameInfo.TotalElapsedGameTime;
		double precipitationStartTime = m_subsystemWeather.m_precipitationStartTime;
		double precipitationEndTime = m_subsystemWeather.m_precipitationEndTime;
		bool num7 = totalElapsedGameTime2 >= precipitationStartTime && totalElapsedGameTime2 < precipitationEndTime;
		double num8 = precipitationStartTime - totalElapsedGameTime2;
		int num9;
		float[] array2;
		if (num7 || num8 <= 150.0)
		{
			num9 = 1;
			array2 = new float[3] { 210f, 220f, 230f };
		}
		else if (num8 > 400.0)
		{
			num9 = 0;
			array2 = new float[3] { 280f, 290f, 300f };
		}
		else
		{
			num9 = 0;
			array2 = new float[3] { 230f, 240f, 250f };
		}
		bool flag = num9 != m_currentCloudTextureType;
		bool flag2 = false;
		for (int m = 0; m < 3; m++)
		{
			if (m_currentHeights[m] != array2[m])
			{
				flag2 = true;
				break;
			}
		}
		if (flag | flag2)
		{
			m_oldCloudTextureType = m_currentCloudTextureType;
			m_targetCloudTextureType = num9;
			for (int n = 0; n < 3; n++)
			{
				m_oldHeights[n] = m_currentHeights[n];
				m_targetHeights[n] = array2[n];
			}
			m_cloudTransitionTimer = 0f;
		}
		if (m_cloudTransitionTimer < m_cloudTransitionDuration)
		{
			m_cloudTransitionTimer += dt;
			float num10 = MathUtils.Saturate(m_cloudTransitionTimer / m_cloudTransitionDuration);
			for (int num11 = 0; num11 < 3; num11++)
			{
				m_currentHeights[num11] = MathUtils.Lerp(m_oldHeights[num11], m_targetHeights[num11], num10);
			}
			if (num10 == 1f)
			{
				m_currentCloudTextureType = m_targetCloudTextureType;
			}
		}
		if (num == 27)
		{
			m_sunTexture = m_downHalfSunTexture;
			m_viewFogColor = new Color(70, 70, 70);
			if (timeOfDay <= 0.2f || timeOfDay > 0.8f)
			{
				SkyLightIntensity = 0f;
				SkyLightValue = 0;
			}
		}
		else if (IsSolarEclipse && (timeOfDay > 0.5f || timeOfDay < 0.2f))
		{
			m_viewFogColor = Color.Black;
			SkyLightIntensity = 0f;
			SkyLightValue = 0;
		}
		else if (IsSolarEclipse)
		{
			m_sunTexture = m_eclipseSunTexture;
			m_viewFogColor = new Color(35, 35, 35);
		}
		else if (num != 29)
		{
			m_sunTexture = m_regularSunTexture;
			SkyLightIntensity = CalculateLightIntensity(timeOfDay);
			SkyLightValue = ((MoonPhase == 4) ? m_lightValuesMoonless[(int)MathUtils.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))] : m_lightValuesNormal[(int)MathUtils.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))]);
		}
		else
		{
			m_sunTexture = m_upHalfSunTexture;
			m_viewFogColor = new Color(70, 70, 70);
			if (timeOfDay <= 0.2f || timeOfDay > 0.8f)
			{
				SkyLightIntensity = 0f;
				SkyLightValue = 0;
			}
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (drawOrder == m_drawOrders[0])
		{
			ViewUnderWaterDepth = 0f;
			ViewUnderMagmaDepth = 0f;
			ViewUnderQuicksandDepth = 0f;
			Vector3 viewPosition = camera.ViewPosition;
			int x = Terrain.ToCell(viewPosition.X);
			int num = Terrain.ToCell(viewPosition.Y);
			int z = Terrain.ToCell(viewPosition.Z);
			FocalorsFluidBlock surfaceFluidBlock = null;
			float? num2 = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, num, z, out surfaceFluidBlock);
			FocalorsFluidBlock surfaceFluidBlock2 = null;
			float? num3 = ((m_subsystemFluidBlockBehavior != null) ? m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, num, z, out surfaceFluidBlock2) : ((float?)null));
			if (!num2.HasValue && num3.HasValue)
			{
				num2 = num3;
			}
			if (num2.HasValue)
			{
				float num4 = num2.Value;
				bool flag = surfaceFluidBlock2 is FocalorsWaterBlock;
				bool flag2 = surfaceFluidBlock2 is FocalorsMagmaBlock;
				bool flag3 = false;
				if (flag)
				{
					if (m_subsystemOceanScanner != null)
					{
						num4 = m_subsystemOceanScanner.GetBlockWaveSurfaceY(viewPosition, num4);
					}
					ViewUnderWaterDepth = num4 + 0.05f - viewPosition.Y;
				}
				else if (flag2)
				{
					ViewUnderMagmaDepth = num4 + 1f - viewPosition.Y;
				}
				else if (flag3)
				{
					ViewUnderQuicksandDepth = num4 + 0.05f - viewPosition.Y;
				}
			}
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num, z);
			int num5 = Terrain.ExtractContents(cellValue);
			if (BlocksManager.Blocks[num5] is IWaterloggable waterloggable && waterloggable.IsWaterLogged(Terrain.ExtractData(cellValue)))
			{
				ViewUnderWaterDepth = (float)num + 1f - viewPosition.Y;
			}
			if (BlocksManager.Blocks[num5] is QuicksandBlock)
			{
				FluidBlock.GetLevel(Terrain.ExtractData(cellValue));
				int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(x, num + 1, z);
				float num6 = ((!(BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)] is QuicksandBlock)) ? 1f : 1f);
				ViewUnderQuicksandDepth = (float)num + num6 + 0.35f - viewPosition.Y;
			}
			else if (num5 == 0)
			{
				int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(x, num - 1, z);
				if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue3)] is QuicksandBlock quicksandBlock)
				{
					int level = FluidBlock.GetLevel(Terrain.ExtractData(cellValue3));
					float num7 = (float)(num - 1) + quicksandBlock.GetLevelHeight(level);
					if (num7 + 0.25f > viewPosition.Y)
					{
						ViewUnderQuicksandDepth = num7 + 0.35f - viewPosition.Y;
					}
				}
			}
			if (ViewUnderWaterDepth > 0f)
			{
				Color baseColor = BlockColorsMap.WaterColorsMap.Lookup(m_subsystemTerrain.Terrain, x, num, z);
				baseColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(m_subsystemWeather.CurrentSeason, baseColor);
				if (m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z) < 6 && m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) > 8)
				{
					baseColor = Color.Lerp(baseColor, new Color(204, 153, 51), 0.5f);
				}
				float f = MathUtils.Saturate((ViewUnderWaterDepth - 1f) / 10f);
				float num8 = MathUtils.Lerp(1f, 0.2f, f);
				float num9 = MathUtils.Lerp(0.5f, 1f, SkyLightIntensity);
				m_viewFogRange.X = 0f;
				m_viewFogRange.Y = MathUtils.Lerp(4f, 12f, num8 * num9);
				m_viewFogColor = Color.MultiplyColorOnly(baseColor, num8 * num9);
				bool flag4 = false;
				ComponentPlayer componentPlayer = m_subsystemPlayers.FindNearestPlayer(camera.ViewPosition);
				if (componentPlayer != null)
				{
					ComponentClothing componentClothing = componentPlayer.Entity.FindComponent<ComponentClothing>();
					if (componentClothing != null)
					{
						foreach (ClothingSlot value in Enum.GetValues(typeof(ClothingSlot)))
						{
							foreach (int item in componentClothing.GetClothes(value))
							{
								ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item)].GetClothingData(Terrain.ExtractData(item));
								if (clothingData == null || !(clothingData.Shade > 0f))
								{
									continue;
								}
								m_viewFogRange.Y *= 2f;
								flag4 = true;
								break;
							}
							if (flag4)
							{
								break;
							}
						}
					}
				}
				if (!flag4 && m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water)
				{
					float num10 = 4f;
					m_viewFogRange.Y *= num10;
				}
				if (SettingsManager.NightVisionEnabled)
				{
					float num26 = 4f;
					m_viewFogRange.Y *= num26;
				}
				VisibilityRangeYMultiplier = 1f;
				m_viewIsSkyVisible = false;
			}
			else if (ViewUnderMagmaDepth > 0f)
			{
				m_viewFogRange.X = 0f;
				m_viewFogRange.Y = 0.1f;
				m_viewFogColor = new Color(255, 80, 0);
				VisibilityRangeYMultiplier = 1f;
				m_viewIsSkyVisible = false;
			}
			else if (ViewUnderQuicksandDepth > 0f)
			{
				float num11 = (float)m_subsystemTerrain.Terrain.GetCellLight(x, num, z) / 15f;
				float gameTimeDelta = m_subsystemTime.GameTimeDelta;
				if (MathUtils.Abs(num11 - m_rawQuicksandLight) < 0.05f)
				{
					m_quicksandLightStableTime += gameTimeDelta;
				}
				else
				{
					m_quicksandLightStableTime = 0f;
				}
				m_rawQuicksandLight = num11;
				float num12 = num11;
				if (num11 < m_smoothedQuicksandLight && m_smoothedQuicksandLight - num11 > 0.2f && m_quicksandLightStableTime < 0.15f)
				{
					num12 = m_smoothedQuicksandLight;
				}
				float num13 = ((m_smoothedQuicksandLight < num12) ? 2f : 10f);
				m_smoothedQuicksandLight = MathUtils.Lerp(m_smoothedQuicksandLight, num12, num13 * gameTimeDelta);
				float s = MathUtils.Lerp(0.05f, 0.55f, m_smoothedQuicksandLight);
				Color c = BlockColorsMap.SandColorsMap.LookupSand(m_subsystemTerrain.Terrain, x, num, z);
				c = Color.MultiplyColorOnly(c, s);
				m_viewFogRange.X = 0f;
				m_viewFogRange.Y = 0.1f;
				m_viewFogColor = c;
				VisibilityRangeYMultiplier = 1f;
				m_viewIsSkyVisible = false;
			}
			else
			{
				float num14 = 1024f;
				float num15 = 128f;
				float num16 = VisibilityRange;
				float num17 = MathUtils.Lerp(0.5f, 0f, m_subsystemWeather.GlobalPrecipitationIntensity);
				float num18 = MathUtils.Lerp(1f, 0.8f, m_subsystemWeather.GlobalPrecipitationIntensity);
				switch (m_subsystemWeather.CurrentSeason)
				{
				case SubsystemWeather.Season.Autumn:
					num16 *= 0.75f;
					break;
				case SubsystemWeather.Season.LateAutumn:
					num16 *= 0.4f;
					break;
				case SubsystemWeather.Season.Anabiosis:
					num16 *= 0.85f;
					break;
				case SubsystemWeather.Season.Winter:
					num16 *= 0.9f;
					break;
				case SubsystemWeather.Season.EarlySummer:
					num16 *= 1.1f;
					break;
				case SubsystemWeather.Season.Summer:
					num16 *= 1.5f;
					break;
				case SubsystemWeather.Season.LateSummer:
					num16 *= 1.1f;
					break;
				}
				m_viewFogRange.X = num16 * num17;
				m_viewFogRange.Y = num16 * num18;
				if (SettingsManager.DistantChunksRange > 0)
				{
					float num19 = (float)SettingsManager.VisibilityRange + (float)(SettingsManager.DistantChunksRange * 16);
					float num20 = MathUtils.Lerp(1f, 0.8f, m_subsystemWeather.GlobalPrecipitationIntensity);
					m_viewFogRange.Y = num19 * num20;
					m_viewFogRange.X = MathUtils.Max(m_viewFogRange.X, (float)SettingsManager.VisibilityRange * 0.8f);
				}
				int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z);
				Color c2 = CalculateSkyColor(new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z), m_subsystemTimeOfDay.TimeOfDay, m_subsystemWeather.GlobalPrecipitationIntensity, seasonalTemperature);
				float y = camera.ViewPosition.Y;
				float x2;
				if (y <= 55f)
				{
					x2 = ((y >= 45f) ? ((y - 45f) / 10f) : 0f);
				}
				else
				{
					x2 = 1f;
				}
				m_viewFogColor = Color.Lerp(Color.Black, c2, MathUtils.Clamp(x2, 0f, 1f));
				VisibilityRangeYMultiplier = MathUtils.Lerp(VisibilityRange / num14, VisibilityRange / num15, MathUtils.Pow(m_subsystemWeather.GlobalPrecipitationIntensity, 4f));
				m_viewIsSkyVisible = true;
				float y2 = m_viewFogRange.Y;
				float num21 = MathUtils.Max(1f, y2 * 3f);
				Vector2 v = new Vector2(viewPosition.X, viewPosition.Z);
				float x3 = 0f;
				if (m_subsystemWeather.IsCycloneActive)
				{
					Vector2 cycloneCenter = m_subsystemWeather.CycloneCenter;
					float num22 = MathUtils.Max(200f, m_subsystemWeather.CycloneRadius);
					float x4 = (num22 * 2.75f + 320f) * 1.5f;
					float num23 = Vector2.Distance(v, cycloneCenter);
					float num24 = MathUtils.Saturate(1f - num23 / num21);
					num24 = num24 * num24 * (3f - 2f * num24);
					float x5 = MathUtils.Pow(MathUtils.Saturate(1f - num23 / MathUtils.Max(1f, x4)), 0.5f);
					float x6 = num22 * 1.25f;
					float x7 = MathUtils.Saturate(1f - num23 / MathUtils.Max(1f, x6));
					x7 = MathUtils.Pow(x7, 0.8f);
					float f2 = MathUtils.Saturate((m_subsystemWeather.CurrentWindStrength - 1f) / 15f);
					float num25 = MathUtils.Lerp(0.35f, 0.95f, f2);
					x3 = MathUtils.Max(x5, num24);
					x3 = MathUtils.Max(x3, x7);
					x3 *= num25;
					if (x7 > 0f)
					{
						float f3 = x7 * x7;
						m_viewFogRange.Y = MathUtils.Max(2f, m_viewFogRange.Y * MathUtils.Lerp(1f, 0.55f, f3));
						m_viewFogRange.X = MathUtils.Max(0f, m_viewFogRange.X * MathUtils.Lerp(1f, 0.65f, f3));
					}
				}
				float x8 = 1f - MathUtils.Pow(0.1f, Time.FrameDuration * 60f);
				m_stormGloomTemporal = MathUtils.Lerp(m_stormGloomTemporal, x3, MathUtils.Saturate(x8));
				m_viewFogColor = Color.Lerp(c2: new Color(4, 4, 6), c1: m_viewFogColor, f: MathUtils.Saturate(m_stormGloomTemporal * 1.8f));
			}
			if ((!FogEnabled || !SettingsManager.RenderFog) && ViewUnderWaterDepth <= 0f && ViewUnderMagmaDepth <= 0f)
			{
				m_viewFogRange = new Vector2(100000f, 100000f);
			}
			if (!DrawSkyEnabled || !m_viewIsSkyVisible || SettingsManager.SkyRenderingMode == SkyRenderingMode.Disabled)
			{
				FlatBatch2D flatBatch2D = m_primitivesRenderer2d.FlatBatch(-1, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.Opaque);
				int count = flatBatch2D.TriangleVertices.Count;
				flatBatch2D.QueueQuad(Vector2.Zero, camera.ViewportSize, 0f, m_viewFogColor);
				flatBatch2D.TransformTriangles(camera.ViewportMatrix, count);
				m_primitivesRenderer2d.Flush();
			}
			return;
		}
		if (drawOrder == m_drawOrders[1])
		{
			if (DrawSkyEnabled && m_viewIsSkyVisible && SettingsManager.SkyRenderingMode != SkyRenderingMode.Disabled)
			{
				DrawSkydome(camera);
				DrawStars(camera);
				DrawSunAndMoon(camera);
				DrawClouds(camera);
				m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
			}
		}
		else
		{
			DrawLightning(camera);
			m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
		}
		return;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemOceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_lastWindStrength = m_subsystemWeather.CurrentWindStrength;
		m_lastWindDirection = m_subsystemWeather.WindDirection;
		UpdateCloudMovement(m_lastWindStrength, m_lastWindDirection);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		m_cloudOffsets = new Vector2[3];
		for (int i = 0; i < 3; i++)
		{
			m_cloudLayerOffsets[i] = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			m_cloudOffsets[i] = Vector2.Zero;
		}
		m_sunTexture = ContentManager.Get<Texture2D>("Textures/Sun");
		m_glowTexture = ContentManager.Get<Texture2D>("Textures/SkyGlow");
		m_regularSunTexture = ContentManager.Get<Texture2D>("Textures/Sun");
		m_eclipseSunTexture = ContentManager.Get<Texture2D>("Textures/SunEclipse");
		m_nullMoonTexture = ContentManager.Get<Texture2D>("Textures/NullMoon");
		m_downHalfSunTexture = ContentManager.Get<Texture2D>("Textures/DownHalfSun");
		m_upHalfSunTexture = ContentManager.Get<Texture2D>("Textures/UpHalfSun");
		m_solarEclipseSkyColor = new Color(70, 70, 70);
		for (int j = 0; j < 8; j++)
		{
			m_moonTextures[j] = ContentManager.Get<Texture2D>("Textures/Moon" + (j + 1).ToString(CultureInfo.InvariantCulture));
		}
		m_cloudTextures = new Texture2D[2];
		for (int k = 0; k < m_cloudTextures.Length; k++)
		{
			m_cloudTextures[k] = ContentManager.Get<Texture2D>($"Textures/Clouds{k + 1}");
		}
		m_sandWallTexture = ContentManager.Get<Texture2D>("Textures/SandClouds");
		m_snowWallTexture = ContentManager.Get<Texture2D>("Textures/SnowClouds");
		m_rainWallTexture = ContentManager.Get<Texture2D>("Textures/RainClouds");
		UpdateLightAndViewParameters();
		Display.DeviceReset += Display_DeviceReset;
		new Random();
		m_cloudLayerOffsets = new Vector2[3];
		for (int l = 0; l < 3; l++)
		{
			m_cloudLayerOffsets[l] = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
		}
		for (int m = 0; m < m_tornadoLayerOffsets.Length; m++)
		{
			m_tornadoLayerOffsets[m] = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
		}
		for (int n = 0; n < m_cycloneLayerUvOffsets.Length; n++)
		{
			m_cycloneLayerUvOffsets[n] = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			m_cycloneLayerCenterOffsets[n] = new Vector2(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
		}
		float num = ((CloudDirection.X >= 0f && CloudDirection.Y <= 0f) ? 1f : (-1f));
		for (int num2 = 0; num2 < 6; num2++)
		{
			float f = (float)num2 / 5f;
			m_cycloneCylinderAngleOffsets[num2] = m_random.Float(0f, 6.283185f);
			m_cycloneCylinderRadialFactors[num2] = MathUtils.Lerp(0.35f, 0.8f, f);
			m_cycloneCylinderRotationDirs[num2] = num;
			m_cycloneCylinderUvOffsets[num2] = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
		}
		if (valuesDictionary.ContainsKey("CloudAlpha0"))
		{
			for (int num3 = 0; num3 < 3; num3++)
			{
				float num4 = MathUtils.Saturate(valuesDictionary.GetValue<float>($"CloudAlpha{num3}"));
				m_currentStageAlphas[num3] = num4;
				m_oldStageAlphas[num3] = num4;
				m_targetStageAlphas[num3] = num4;
			}
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			double precipitationStartTime = m_subsystemWeather.m_precipitationStartTime;
			double precipitationEndTime = m_subsystemWeather.m_precipitationEndTime;
			bool num5 = totalElapsedGameTime >= precipitationStartTime && totalElapsedGameTime < precipitationEndTime;
			double num6 = precipitationStartTime - totalElapsedGameTime;
			ref int targetCloudStage = ref m_targetCloudStage;
			ref int oldCloudStage = ref m_oldCloudStage;
			int num7;
			if (num5)
			{
				num7 = 7;
			}
			else if (num6 <= 1200.0)
			{
				num7 = 6;
			}
			else if (num6 <= 2400.0)
			{
				num7 = 5;
			}
			else if (num6 <= 3600.0)
			{
				num7 = 4;
			}
			else
			{
				num7 = ((num6 <= 4800.0) ? 3 : ((!(num6 <= 6000.0)) ? 1 : 2));
			}
			targetCloudStage = (oldCloudStage = num7);
			m_cloudStageTransitionTimer = m_cloudStageTransitionDuration;
		}
		m_cycloneStartWorldTime = (valuesDictionary.ContainsKey("CycloneStartWorldTime") ? valuesDictionary.GetValue<double>("CycloneStartWorldTime") : (-1.0));
		m_tornadoTracks.Clear();
		if (!valuesDictionary.ContainsKey("TornadoStarts"))
		{
			return;
		}
		string value = valuesDictionary.GetValue<string>("TornadoStarts");
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		string[] array = value.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		for (int num8 = 0; num8 < array.Length; num8++)
		{
			string[] array2 = array[num8].Split('=');
			if (array2.Length == 2)
			{
				string[] array3 = array2[0].Split(',');
				if (array3.Length == 2 && float.TryParse(array3[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && float.TryParse(array3[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && double.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
				{
					m_tornadoTracks.Add(new TornadoTrack
					{
						Pos = new Vector2(result, result2),
						Start = result3,
						Seen = false
					});
				}
			}
		}
	}

	public override void Dispose()
	{
		Display.DeviceReset -= Display_DeviceReset;
		Utilities.Dispose(ref m_starsVertexBuffer);
		Utilities.Dispose(ref m_starsIndexBuffer);
		foreach (SkyDome value in m_skyDomes.Values)
		{
			value.Dispose();
		}
		m_skyDomes.Clear();
	}

	public void Display_DeviceReset()
	{
		Utilities.Dispose(ref m_starsVertexBuffer);
		Utilities.Dispose(ref m_starsIndexBuffer);
		foreach (SkyDome value in m_skyDomes.Values)
		{
			value.Dispose();
		}
		m_skyDomes.Clear();
	}

	public void DrawSkydome(Camera camera)
	{
		if (!m_skyDomes.TryGetValue(camera.GameWidget, out var value))
		{
			value = new SkyDome();
			m_skyDomes.Add(camera.GameWidget, value);
		}
		if (value.VertexBuffer == null || value.IndexBuffer == null)
		{
			Utilities.Dispose(ref value.VertexBuffer);
			Utilities.Dispose(ref value.IndexBuffer);
			value.VertexBuffer = new VertexBuffer(m_skyVertexDeclaration, value.Vertices.Length);
			value.IndexBuffer = new IndexBuffer(IndexFormat.SixteenBits, value.Indices.Length);
			FillSkyIndexBuffer(value);
			value.LastUpdateTimeOfDay = null;
		}
		int x = Terrain.ToCell(camera.ViewPosition.X);
		int z = Terrain.ToCell(camera.ViewPosition.Z);
		float globalPrecipitationIntensity = m_subsystemWeather.GlobalPrecipitationIntensity;
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z);
		if (!value.LastUpdateTimeOfDay.HasValue || !(MathUtils.Abs(timeOfDay - value.LastUpdateTimeOfDay.Value) <= 0.001f) || !value.LastUpdatePrecipitationIntensity.HasValue || !(MathUtils.Abs(globalPrecipitationIntensity - value.LastUpdatePrecipitationIntensity.Value) <= 0.02f) || ((globalPrecipitationIntensity == 0f || globalPrecipitationIntensity == 1f) && value.LastUpdatePrecipitationIntensity.Value != globalPrecipitationIntensity) || m_lightningStrikeBrightness != value.LastUpdateLightningStrikeBrightness || !value.LastUpdateTemperature.HasValue || seasonalTemperature != value.LastUpdateTemperature)
		{
			value.LastUpdateTimeOfDay = timeOfDay;
			value.LastUpdatePrecipitationIntensity = globalPrecipitationIntensity;
			value.LastUpdateLightningStrikeBrightness = m_lightningStrikeBrightness;
			value.LastUpdateTemperature = seasonalTemperature;
		}
		m_lastCameraViewPos = camera.ViewPosition;
		float y = camera.ViewPosition.Y;
		FillSkyVertexBuffer(value, timeOfDay, globalPrecipitationIntensity, seasonalTemperature, y);
		Display.DepthStencilState = DepthStencilState.DepthRead;
		Display.RasterizerState = RasterizerState.CullNoneScissor;
		Display.BlendState = BlendState.Opaque;
		m_shaderFlat.Transforms.World[0] = Matrix.CreateTranslation(camera.ViewPosition) * camera.ViewProjectionMatrix;
		m_shaderFlat.Color = Vector4.One;
		Display.DrawIndexed(PrimitiveType.TriangleList, m_shaderFlat, value.VertexBuffer, value.IndexBuffer, 0, value.IndexBuffer.IndicesCount);
	}

	public void DrawStars(Camera camera)
	{
		if (camera.ViewPosition.Y < 0f)
		{
			return;
		}
		float num = m_subsystemWeather.GlobalPrecipitationIntensity;
		if (num <= 0f)
		{
			Vector2 camXZ = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
			float cyclonePrecipFactorForCamera = GetCyclonePrecipFactorForCamera(camXZ);
			num = MathUtils.Max(num, cyclonePrecipFactorForCamera);
		}
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		int num2 = (int)m_subsystemTimeOfDay.Day % 30;
		if (num2 != 27 && num2 != 28 && num2 != 29)
		{
			float y = camera.ViewPosition.Y;
			int num3 = 9000;
			if (m_starsVertexBuffer == null || m_starsIndexBuffer == null || m_starsVertexBuffer.VerticesCount != 4 * num3)
			{
				Utilities.Dispose(ref m_starsVertexBuffer);
				Utilities.Dispose(ref m_starsIndexBuffer);
				m_starsVertexBuffer = new VertexBuffer(m_starsVertexDeclaration, 4 * num3);
				m_starsIndexBuffer = new IndexBuffer(IndexFormat.SixteenBits, 6 * num3);
				FillStarsBuffers(num3);
			}
			Display.DepthStencilState = DepthStencilState.DepthRead;
			Display.RasterizerState = RasterizerState.CullNoneScissor;
			float num4 = MathUtils.Sqr((1f - CalculateLightIntensity(timeOfDay)) * (1f - num));
			if (num4 > 0.01f)
			{
				Display.BlendState = BlendState.Additive;
				m_shaderTextured.Transforms.World[0] = Matrix.CreateRotationZ(-2f * timeOfDay * (float)Math.PI) * Matrix.CreateTranslation(camera.ViewPosition) * camera.ViewProjectionMatrix;
				m_shaderTextured.Color = new Vector4(1f, 1f, 1f, num4);
				m_shaderTextured.Texture = ContentManager.Get<Texture2D>("Textures/Star");
				m_shaderTextured.SamplerState = SamplerState.LinearClamp;
				Display.DrawIndexed(PrimitiveType.TriangleList, m_shaderTextured, m_starsVertexBuffer, m_starsIndexBuffer, 0, m_starsIndexBuffer.IndicesCount);
			}
		}
	}

	public void DrawSunAndMoon(Camera camera)
	{
		if (camera.ViewPosition.Y < 0f)
		{
			return;
		}
		float num = m_subsystemWeather.GlobalPrecipitationIntensity;
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		if (num <= 0f)
		{
			Vector2 camXZ = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
			float cyclonePrecipFactorForCamera = GetCyclonePrecipFactorForCamera(camXZ);
			num = MathUtils.Max(num, cyclonePrecipFactorForCamera);
		}
		float x = CalculateDawnGlowIntensity(timeOfDay);
		float x2 = CalculateDuskGlowIntensity(timeOfDay);
		float f = MathUtils.Max(x, x2);
		float num2 = 2f * timeOfDay * (float)Math.PI;
		float angle = num2 + (float)Math.PI;
		float num3 = MathUtils.Lerp(90f, 160f, f);
		float num4 = MathUtils.Lerp(60f, 80f, f) * 0.5f;
		Color color = Color.Lerp(new Color(255, 255, 255), new Color(255, 255, 160), f);
		Color white = Color.White;
		white *= 1f - SkyLightIntensity;
		color *= MathUtils.Lerp(1f, 0f, num);
		white *= MathUtils.Lerp(1f, 0f, num);
		Color color2 = color * 0.6f * MathUtils.Lerp(1f, 0f, num);
		Color color3 = color * 0.2f * MathUtils.Lerp(1f, 0f, num);
		int num5 = (int)m_subsystemTimeOfDay.Day % 30;
		if (IsSolarEclipse)
		{
			m_sunTexture = m_eclipseSunTexture;
		}
		else
		{
			switch (num5)
			{
			case 27:
				m_sunTexture = m_downHalfSunTexture;
				break;
			case 29:
				m_sunTexture = m_upHalfSunTexture;
				break;
			default:
				m_sunTexture = m_regularSunTexture;
				break;
			}
		}
		Texture2D texture = ((num5 == 27 || num5 == 28 || num5 == 29) ? m_nullMoonTexture : m_moonTextures[MoonPhase]);
		float y = camera.ViewPosition.Y;
		if (!(y <= 45f))
		{
			float x3 = 1f;
			if (y <= 55f)
			{
				x3 = ((!(y >= 45f)) ? 0f : ((y - 45f) / 10f));
			}
			x3 = MathUtils.Clamp(x3, 0f, 1f);
			color *= x3;
			white *= x3;
			color2 *= x3;
			color3 *= x3;
			float num6 = 0.5f;
			float num7 = num6;
			switch (m_subsystemWeather.CurrentSeason)
			{
			case SubsystemWeather.Season.EarlyAutumn:
				num7 = num6 * 0.9f;
				break;
			case SubsystemWeather.Season.Autumn:
				num7 = 0.8f * num6;
				break;
			case SubsystemWeather.Season.LateAutumn:
				num7 = num6 * 0.7f;
				break;
			case SubsystemWeather.Season.Anabiosis:
				num7 = num6 * 0.9f;
				break;
			case SubsystemWeather.Season.Winter:
				num7 = num6;
				break;
			}
			float radius = num3 * num7;
			TexturedBatch3D batch = m_primitivesRenderer3d.TexturedBatch(m_glowTexture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.Additive);
			TexturedBatch3D batch2 = m_primitivesRenderer3d.TexturedBatch(m_sunTexture, useAlphaTest: false, 1, DepthStencilState.DepthRead, null, BlendState.AlphaBlend);
			TexturedBatch3D batch3 = m_primitivesRenderer3d.TexturedBatch(texture, useAlphaTest: false, 1, DepthStencilState.DepthRead, null, BlendState.AlphaBlend);
			QueueCelestialBody(batch, camera.ViewPosition, color2, 900f, 3.5f * num3, num2);
			QueueCelestialBody(batch, camera.ViewPosition, color3, 900f, 3.5f * num4, angle);
			QueueCelestialBody(batch2, camera.ViewPosition, color, 900f, radius, num2);
			QueueCelestialBody(batch3, camera.ViewPosition, white, 900f, num4, angle);
		}
	}

	public void DrawLightning(Camera camera)
	{
		if (!m_lightningStrikePosition.HasValue || m_lightMain == null || m_lightMain.Count < 2)
		{
			return;
		}
		if (m_lightVisualDuration <= 0f)
		{
			m_lightVisualDuration = ((m_lightLifetime > 0f) ? m_lightLifetime : 1f);
		}
		double num = m_subsystemTime.GameTime - m_lightStartTime;
		float num2 = (float)(num / (double)m_lightVisualDuration);
		if (num2 >= 1f)
		{
			m_lightningStrikePosition = null;
			m_lightningStrikeBrightness = 0f;
			m_lightMain = null;
			m_lightBranches = null;
			return;
		}
		float num3 = 1f - num2;
		num3 *= num3;
		bool flag = m_currentLightningType == 1;
		float num4 = (flag ? 1.2f : 1.1f);
		float num5 = (flag ? num4 : 0.605f);
		float num6 = (flag ? 0.35f : 0.7f);
		float num7 = (flag ? 0.35f : 0.28f);
		float amp = 0.25f;
		float amp2 = 0.18f;
		float num8 = num4 * num3;
		float num9 = num5 * num3;
		FlatBatch3D fb = m_primitivesRenderer3d.FlatBatch(0, DepthStencilState.DepthRead, RasterizerState.CullNoneScissor, BlendState.Additive);
		Vector3 viewDirection = camera.ViewDirection;
		int num10 = m_lightMain.Count - 1;
		for (int i = 0; i < num10; i++)
		{
			Vector3 p = m_lightMain.Array[i] + StableJitter(i, num, amp);
			Vector3 p2 = m_lightMain.Array[i + 1] + StableJitter(i + 1, num, amp);
			float num11 = (float)i / (float)num10;
			float num12 = (float)(i + 1) / (float)num10;
			float w = m_lightThickness * (1f - num6 * num11);
			float w2 = m_lightThickness * (1f - num6 * num12);
			float intensity = (flag ? num8 : (num8 * (0.35f + 0.65f * (1f - num11))));
			float intensity2 = (flag ? num8 : (num8 * (0.35f + 0.65f * (1f - num12))));
			AddLightningQuad(fb, p, p2, viewDirection, w, w2, intensity, intensity2);
		}
		if (m_lightBranches != null)
		{
			for (int j = 0; j < m_lightBranches.Count; j++)
			{
				DynamicArray<Vector3> dynamicArray = m_lightBranches.Array[j];
				if (dynamicArray != null && dynamicArray.Count >= 2)
				{
					int num13 = dynamicArray.Count - 1;
					for (int k = 0; k < num13; k++)
					{
						Vector3 p3 = dynamicArray.Array[k] + StableJitter(1000 + j * 257 + k, num, amp2);
						Vector3 p4 = dynamicArray.Array[k + 1] + StableJitter(1000 + j * 257 + k + 1, num, amp2);
						float num14 = (float)k / (float)num13;
						float num15 = (float)(k + 1) / (float)num13;
						float w3 = m_lightThickness * num7 * (1f - num14);
						float w4 = m_lightThickness * num7 * (1f - num15);
						float intensity3 = (flag ? num9 : (num9 * (1f - num14)));
						float intensity4 = (flag ? num9 : (num9 * (1f - num15)));
						AddLightningQuad(fb, p3, p4, viewDirection, w3, w4, intensity3, intensity4);
					}
				}
			}
		}
		if (m_lightBranches != null)
		{
			for (int l = 0; l < m_lightBranches.Count; l++)
			{
				DynamicArray<Vector3> dynamicArray2 = m_lightBranches.Array[l];
				if (dynamicArray2 != null && dynamicArray2.Count >= 2)
				{
					int num16 = dynamicArray2.Count - 1;
					for (int m = 0; m < num16; m++)
					{
						Vector3 p5 = dynamicArray2.Array[m] + StableJitter(1000 + l * 257 + m, num, 0.18f);
						Vector3 p6 = dynamicArray2.Array[m + 1] + StableJitter(1000 + l * 257 + m + 1, num, 0.18f);
						float num17 = (float)m / (float)num16;
						float num18 = (float)(m + 1) / (float)num16;
						float w5 = 0.28f * m_lightThickness * (1f - num17);
						float w6 = 0.28f * m_lightThickness * (1f - num18);
						float intensity5 = 0.55f * num3 * (1f - num17);
						float intensity6 = 0.55f * num3 * (1f - num18);
						AddLightningQuad(fb, p5, p6, viewDirection, w5, w6, intensity5, intensity6);
					}
				}
			}
		}
		if (m_lightBranches != null)
		{
			for (int n = 0; n < m_lightBranches.Count; n++)
			{
				DynamicArray<Vector3> dynamicArray3 = m_lightBranches.Array[n];
				if (dynamicArray3 != null && dynamicArray3.Count >= 2)
				{
					int num19 = dynamicArray3.Count - 1;
					for (int num20 = 0; num20 < num19; num20++)
					{
						Vector3 p7 = dynamicArray3.Array[num20] + StableJitter(1000 + n * 257 + num20, num, 0.18f);
						Vector3 p8 = dynamicArray3.Array[num20 + 1] + StableJitter(1000 + n * 257 + num20 + 1, num, 0.18f);
						float num21 = (float)num20 / (float)num19;
						float num22 = (float)(num20 + 1) / (float)num19;
						float w7 = m_lightThickness * 0.28f * (1f - num21);
						float w8 = m_lightThickness * 0.28f * (1f - num22);
						float intensity7 = num9 * (1f - num21);
						float intensity8 = num9 * (1f - num22);
						AddLightningQuad(fb, p7, p8, viewDirection, w7, w8, intensity7, intensity8);
					}
				}
			}
		}
		float num23 = MathUtils.Lerp(0.3f, 0.75f, 0.5f * (float)MathUtils.Sin(MathUtils.Remainder(1.0 * m_subsystemTime.GameTime, 6.2831854820251465)) + 0.5f);
		m_lightningStrikeBrightness = MathUtils.Saturate(num3 - m_subsystemTime.GameTimeDelta / num23);
	}

	public void DrawClouds(Camera camera)
	{
		if (SettingsManager.SkyRenderingMode == SkyRenderingMode.NoClouds || SettingsManager.SkyRenderingMode != SkyRenderingMode.Full || camera.ViewPosition.Y < 0f)
		{
			return;
		}
		float value = m_subsystemWeather.GlobalPrecipitationIntensity;
		float num = MathUtils.Lerp(0f, 0.14f, MathUtils.Sqr(SkyLightIntensity)) * MathUtils.Lerp(1f, 0.1f, value);
		float array = MathUtils.Saturate(m_cloudTransitionTimer / m_cloudTransitionDuration);
		Vector3 array2 = camera.ViewPosition;
		Texture2D i = m_cloudTextures[m_oldCloudTextureType];
		Texture2D num2 = m_cloudTextures[m_targetCloudTextureType];
		double num3 = m_subsystemWeather.m_precipitationStartTime;
		double num4 = m_subsystemWeather.m_precipitationEndTime;
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		bool num5 = totalElapsedGameTime >= num3 && totalElapsedGameTime < num4;
		double num6 = num3 - totalElapsedGameTime;
		int num7;
		if (num5)
		{
			num7 = 7;
		}
		else if (num6 <= 1200.0)
		{
			num7 = 6;
		}
		else if (num6 <= 2400.0)
		{
			num7 = 5;
		}
		else if (num6 <= 3600.0)
		{
			num7 = 4;
		}
		else
		{
			num7 = ((num6 <= 4800.0) ? 3 : ((!(num6 <= 6000.0)) ? 1 : 2));
		}
		if (num7 != m_targetCloudStage)
		{
			m_oldCloudStage = m_targetCloudStage;
			for (int j = 0; j < 3; j++)
			{
				m_oldStageAlphas[j] = m_currentStageAlphas[j];
			}
			m_targetCloudStage = num7;
			switch (m_targetCloudStage)
			{
			case 1:
				m_targetStageAlphas[0] = 0f;
				m_targetStageAlphas[1] = 0f;
				m_targetStageAlphas[2] = 0f;
				break;
			case 2:
				m_targetStageAlphas[0] = 0.2f;
				m_targetStageAlphas[1] = 0f;
				m_targetStageAlphas[2] = 0f;
				break;
			case 3:
				m_targetStageAlphas[0] = 0.3f;
				m_targetStageAlphas[1] = 0.1f;
				m_targetStageAlphas[2] = 0f;
				break;
			case 4:
				m_targetStageAlphas[0] = 0.3f;
				m_targetStageAlphas[1] = 0.1f;
				m_targetStageAlphas[2] = 0.1f;
				break;
			case 5:
				m_targetStageAlphas[0] = 0.3f;
				m_targetStageAlphas[1] = 0.3f;
				m_targetStageAlphas[2] = 0.3f;
				break;
			case 6:
				m_targetStageAlphas[0] = 0.4f;
				m_targetStageAlphas[1] = 0.3f;
				m_targetStageAlphas[2] = 0.3f;
				break;
			case 7:
				m_targetStageAlphas[0] = 0.4f;
				m_targetStageAlphas[1] = 0.4f;
				m_targetStageAlphas[2] = 0.4f;
				break;
			}
			m_cloudStageTransitionTimer = 0f;
		}
		if (m_cloudStageTransitionTimer < m_cloudStageTransitionDuration)
		{
			m_cloudStageTransitionTimer += Time.FrameDuration;
			float f = MathUtils.Saturate(m_cloudStageTransitionTimer / m_cloudStageTransitionDuration);
			for (int k = 0; k < 3; k++)
			{
				m_currentStageAlphas[k] = MathUtils.Lerp(m_oldStageAlphas[k], m_targetStageAlphas[k], f);
			}
		}
		for (int l = 0; l < 3; l++)
		{
			float num8 = (1f - array) * m_currentStageAlphas[l];
			float num9 = array * m_currentStageAlphas[l];
			Vector2 vector = m_cloudOffsets[l] + m_cloudLayerOffsets[l];
			vector = new Vector2(MathUtils.Remainder(vector.X, 1f), MathUtils.Remainder(vector.Y, 1f));
			if (num8 > 0f)
			{
				Color layerColor = new Color(num, num, num, num8);
				DrawCloudLayer(i, layerColor, m_currentHeights[l], array2, vector);
			}
			if (num9 > 0f)
			{
				Color layerColor2 = new Color(num, num, num, num9);
				DrawCloudLayer(num2, layerColor2, m_currentHeights[l], array2, vector);
			}
		}
		DrawCyclone(camera);
		DrawTornadoes(camera);
	}

	public void QueueCelestialBody(TexturedBatch3D batch, Vector3 viewPosition, Color color, float distance, float radius, float angle)
	{
		if (color.A > 0)
		{
			Vector3 vector = new Vector3
			{
				X = 0f - MathUtils.Sin(angle),
				Y = 0f - MathUtils.Cos(angle),
				Z = 0f
			};
			Vector3 unitZ = Vector3.UnitZ;
			Vector3 vector2 = Vector3.Cross(unitZ, vector);
			Vector3 p = viewPosition + vector * distance - radius * unitZ - radius * vector2;
			Vector3 p2 = viewPosition + vector * distance + radius * unitZ - radius * vector2;
			Vector3 p3 = viewPosition + vector * distance + radius * unitZ + radius * vector2;
			Vector3 p4 = viewPosition + vector * distance - radius * unitZ + radius * vector2;
			batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), color);
		}
	}

	public void UpdateLightAndViewParameters()
	{
		VisibilityRange = SettingsManager.VisibilityRange;
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		SkyLightIntensity = CalculateLightIntensity(timeOfDay);
		SkyLightValue = ((MoonPhase == 4) ? m_lightValuesMoonless[(int)MathUtils.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))] : m_lightValuesNormal[(int)MathUtils.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))]);
		if (IsSolarEclipse)
		{
			SkyLightValue = Math.Max(SkyLightValue - 5, 0);
			m_viewFogColor = Color.Lerp(m_viewFogColor, Color.Black, 0.3f);
		}
	}

	public float CalculateLightIntensity(float timeOfDay)
	{
		float count;
		if (timeOfDay <= 0.2f || timeOfDay > 0.8f)
		{
			count = 0f;
		}
		else if (timeOfDay > 0.2f && timeOfDay <= 0.3f)
		{
			count = (timeOfDay - 0.2f) / (71f / (226f * (float)Math.PI));
		}
		else
		{
			count = ((!(timeOfDay > 0.3f) || !(timeOfDay <= 0.7f)) ? (1f - (timeOfDay - 0.7f) / 0.100000024f) : 1f);
		}
		if (IsSolarEclipse)
		{
			count *= 0.2f;
		}
		int count2 = (int)m_subsystemTimeOfDay.Day % 30;
		if (count2 == 27 || count2 == 29)
		{
			count *= 0.7f;
		}
		return count;
	}

	public Color CalculateSkyColor(Vector3 direction, float timeOfDay, float precipitationIntensity, int temperature)
	{
		double gameTime = m_subsystemTime.GameTime;
		double precipitationStartTime = m_subsystemWeather.m_precipitationStartTime;
		float num = precipitationIntensity;
		if (num <= 0f)
		{
			Vector2 camXZ = new Vector2(m_lastCameraViewPos.X, m_lastCameraViewPos.Z);
			float cyclonePrecipFactorForCamera = GetCyclonePrecipFactorForCamera(camXZ);
			num = MathUtils.Max(num, cyclonePrecipFactorForCamera);
			if (precipitationStartTime > gameTime && precipitationStartTime - gameTime < 600.0)
			{
				num = MathUtils.Max(num, MathUtils.Saturate(1f - (float)((precipitationStartTime - gameTime) / 600.0)));
			}
		}
		direction = Vector3.Normalize(direction);
		Vector2 vector = Vector2.Normalize(new Vector2(direction.X, direction.Z));
		float num2 = CalculateLightIntensity(timeOfDay);
		float f = (float)temperature / 15f;
		Vector3 v = new Vector3(0.65f, 0.68f, 0.7f) * num2;
		Vector3 v2 = Vector3.Lerp(new Vector3(0.28f, 0.38f, 0.52f), new Vector3(0.24f, 0.58f, 0.8f), f);
		Vector3 v3 = Vector3.Lerp(new Vector3(0.576f, 0.729f, 0.902f), new Vector3(0.576f, 0.729f, 0.902f), f);
		Vector3 vector2 = Vector3.Lerp(v2, v, num) * num2;
		Vector3 vector3 = Vector3.Lerp(v3, v, num) * num2;
		float x = 0f;
		if (precipitationIntensity <= 0f && m_subsystemWeather.IsCycloneActive)
		{
			Vector2 camXZ2 = new Vector2(m_lastCameraViewPos.X, m_lastCameraViewPos.Z);
			x = GetCycloneCoreFactorForCamera(camXZ2);
		}
		float f2 = MathUtils.Max(m_subsystemWeather.m_lightningIntensity, x);
		float num3 = MathUtils.Lerp(1f, 0.4f, f2);
		vector2 *= num3;
		vector3 *= num3;
		Vector3 vector4 = new Vector3(1f, 0.3f, -0.2f);
		Vector3 vector5 = new Vector3(1f, 0.3f, -0.2f);
		int num4 = (int)m_subsystemTimeOfDay.Day % 30;
		if (m_lightningStrikePosition.HasValue)
		{
			vector3 = Vector3.Max(new Vector3(m_lightningStrikeBrightness), vector3);
		}
		if (IsSolarEclipse)
		{
			Vector3 vector6 = new Vector3(35f / 255f, 35f / 255f, 35f / 255f);
			float f3 = 1f - num2;
			if (timeOfDay > 0.5f || timeOfDay < 0.2f)
			{
				float num5 = ((timeOfDay < 0.2f) ? (timeOfDay / 0.2f) : (1f - (timeOfDay - 0.5f) / 0.3f));
				vector6 = Vector3.Lerp(vector6, Vector3.Zero, 1f - num5);
			}
			vector2 = Vector3.Lerp(vector2, vector6, f3);
			vector3 = Vector3.Lerp(vector3, vector6, f3);
		}
		else if (num4 == 27 || num4 == 29)
		{
			if (timeOfDay > 0.2f && timeOfDay <= 0.8f)
			{
				Vector3 v4 = new Vector3(0.2745098f, 0.2745098f, 0.2745098f);
				float f4 = 1f - num2;
				vector2 = Vector3.Lerp(vector2, v4, f4);
				vector4 = Vector3.Lerp(vector4, v4, f4);
			}
			else
			{
				vector2 = (vector4 = Vector3.Zero);
			}
		}
		float num6 = MathUtils.Lerp(CalculateDawnGlowIntensity(timeOfDay), 0f, num);
		float num7 = MathUtils.Lerp(CalculateDuskGlowIntensity(timeOfDay), 0f, num);
		float num8 = MathUtils.Saturate((direction.Y - 0.1f) / 0.4f);
		if (m_lightningStrikePosition.HasValue)
		{
			num8 += m_lightningStrikeBrightness * 0.5f;
			num8 = MathUtils.Saturate(num8);
		}
		float num9 = num6 * MathUtils.Sqr(MathUtils.Saturate(0f - vector.X));
		float num10 = num7 * MathUtils.Sqr(MathUtils.Saturate(vector.X));
		return new Color(Vector3.Lerp(vector3 + vector4 * num9 + vector5 * num10, vector2, num8));
	}

	public void FillSkyIndexBuffer(SkyDome skyDome)
	{
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			for (int j = 0; j < 10; j++)
			{
				int num2 = j;
				int num3 = (j + 1) % 10;
				int num4 = i;
				int num5 = i + 1;
				skyDome.Indices[num++] = (ushort)(num2 + num4 * 10);
				skyDome.Indices[num++] = (ushort)(num3 + num4 * 10);
				skyDome.Indices[num++] = (ushort)(num3 + num5 * 10);
				skyDome.Indices[num++] = (ushort)(num3 + num5 * 10);
				skyDome.Indices[num++] = (ushort)(num2 + num5 * 10);
				skyDome.Indices[num++] = (ushort)(num2 + num4 * 10);
			}
		}
		for (int k = 2; k < 10; k++)
		{
			skyDome.Indices[num++] = 0;
			skyDome.Indices[num++] = (ushort)(k - 1);
			skyDome.Indices[num++] = (ushort)k;
		}
		skyDome.IndexBuffer.SetData(skyDome.Indices, 0, skyDome.Indices.Length);
	}

	public static float CalculateDawnGlowIntensity(float timeOfDay)
	{
		return MathUtils.Max(1f - MathUtils.Abs(timeOfDay - 0.25f) / (71f / (226f * (float)Math.PI)) * 2f, 0f);
	}

	public static float CalculateDuskGlowIntensity(float timeOfDay)
	{
		return MathUtils.Max(1f - MathUtils.Abs(timeOfDay - 0.75f) / 0.100000024f * 2f, 0f);
	}

	public SubsystemSky()
	{
		m_cycloneCylinderAngleOffsets = new float[6];
		m_cycloneCylinderRadialFactors = new float[6];
		m_cycloneCylinderRotationDirs = new float[6];
		m_cycloneCylinderUvOffsets = new Vector2[6];
		m_cycloneStartWorldTime = -1.0;
		m_tornadoTracks = new List<TornadoTrack>();
		m_lightSeed = 1u;
		m_lightLifetime = 1.45f;
		m_lightThickness = 1.45f;
		m_lightVisualDuration = m_lightLifetime;
		m_dustExposure = new float[3, 9, 9];
		m_dustOffsets = new Vector2[3];
		m_dustLayerOffsets = new Vector2[3];
		m_dustLayerHeights = new float[3] { 65.5f, 64.5f, 63.5f };
		m_dustLayerSpeedFactors = new float[3] { 4f, 4.1f, 3.9f };
		m_cloudStageTransitionDuration = 60f;
		m_oldStageAlphas = new float[3];
		m_targetStageAlphas = new float[3];
		m_currentStageAlphas = new float[3];
		m_cloudOffsets = new Vector2[3];
		m_cloudLayerOffsets = new Vector2[3];
		m_cloudTransitionDuration = 60f;
		m_currentHeights = new float[3] { 230f, 240f, 250f };
		m_oldHeights = new float[3] { 230f, 240f, 250f };
		m_targetHeights = new float[3] { 230f, 240f, 250f };
		m_currentDay = -1;
		m_primitivesRenderer2d = new PrimitivesRenderer2D();
		m_primitivesRenderer3d = new PrimitivesRenderer3D();
		m_random = new Random();
		m_moonTextures = new Texture2D[8];
		m_skyVertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color));
		m_skyDomes = new Dictionary<GameWidget, SkyDome>();
		m_starsVertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate), new VertexElement(20, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color));
		DrawSkyEnabled = true;
		FogEnabled = true;
		m_drawOrders = new int[3] { -100, 5, 105 };
		m_cloudsLayerRadii = new float[4] { 0f, 0.8f, 0.95f, 1f };
		m_cloudsLayerColors = new Color[5];
		m_tornadoLayerRadiusFactors = new float[9] { 1.12f, 1.06f, 1.02f, 1f, 0.92f, 0.84f, 0.76f, 0.68f, 0.65f };
		m_tornadoLayerSpeedFactors = new float[9] { 1f, 1.05f, 1.1f, 1.55f, 0.9f, 1.3f, 0.8f, 1.6f, 0.7f };
		m_tornadoLayerAlphaFactors = new float[9] { 0.42f, 0.45f, 0.6f, 0.74f, 0.4f, 0.56f, 0.48f, 0.42f, 0.35f };
		m_tornadoLayerOffsets = new Vector2[9];
		m_cycloneLayerRadiusFactors = new float[7] { 1f, 0.93f, 1.07f, 0.85f, 1.15f, 0.78f, 1.22f };
		m_cycloneLayerSpeedFactors = new float[7] { 0.15f, 0.4f, 0.2f, 0.3f, 0.5f, 0.45f, 0.6f };
		m_cycloneLayerAlphaFactors = new float[7] { 0.09f, 0.2f, 0.48f, 0.35f, 0.62f, 0.74f, 0.7f };
		m_cycloneLayerUvOffsets = new Vector2[7];
		m_cycloneLayerCenterOffsets = new Vector2[7];
	}

	static SubsystemSky()
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
		m_shaderFlat = new UnlitShader(useVertexColor: true, useTexture: false, useAlphaThreshold: false);
		m_shaderTextured = new UnlitShader(useVertexColor: true, useTexture: true, useAlphaThreshold: false);
		m_lightValuesMoonless = new int[6] { 0, 1, 3, 5, 7, 15 };
		m_lightValuesNormal = new int[6] { 3, 5, 8, 10, 13, 15 };
	}

	public void FillStarsBuffers(int starsCount)
	{
		Random list = new Random(7);
		StarVertex[] list2 = new StarVertex[4 * starsCount];
		int i = starsCount / 4;
		Vector3 vector = new Vector3(0f, 1f, 0f);
		for (int j = 0; j < starsCount; j++)
		{
			Vector3 v;
			do
			{
				v = ((j >= i) ? new Vector3(list.Float(-1f, 1f), list.Float(-1f, 1f), list.Float(-1f, 1f)) : (vector + new Vector3(list.NormalFloat(-0.3f, 0.6f), list.NormalFloat(-0.3f, 0.6f), list.NormalFloat(-0.1f, 0.1f))));
			}
			while (v.LengthSquared() > 1f);
			v = Vector3.Normalize(v);
			float num = 3f * list.NormalFloat(1f, 0.1f);
			float w = MathUtils.Saturate(list.NormalFloat(0.6f, 0.4f));
			Color color = new Color(new Vector4(list.Float(0.35f, 0.55f), list.Float(0.35f, 0.55f), list.Float(0.35f, 0.55f), w));
			Vector3 vector2 = 900f * v;
			Vector3 vector3 = Vector3.Normalize(Vector3.Cross((v.X > v.Y) ? Vector3.UnitY : Vector3.UnitX, v));
			Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector3, v));
			Vector3 position = vector2 + num * (-vector3 - vector4);
			Vector3 position2 = vector2 + num * (vector3 - vector4);
			Vector3 position3 = vector2 + num * (vector3 + vector4);
			Vector3 position4 = vector2 + num * (-vector3 + vector4);
			list2[j * 4] = new StarVertex
			{
				Position = position,
				TextureCoordinate = new Vector2(0f, 0f),
				Color = color
			};
			list2[j * 4 + 1] = new StarVertex
			{
				Position = position2,
				TextureCoordinate = new Vector2(1f, 0f),
				Color = color
			};
			list2[j * 4 + 2] = new StarVertex
			{
				Position = position3,
				TextureCoordinate = new Vector2(1f, 1f),
				Color = color
			};
			list2[j * 4 + 3] = new StarVertex
			{
				Position = position4,
				TextureCoordinate = new Vector2(0f, 1f),
				Color = color
			};
		}
		m_starsVertexBuffer.SetData(list2, 0, list2.Length);
		ushort[] array = new ushort[6 * starsCount];
		for (int k = 0; k < starsCount; k++)
		{
			array[k * 6] = (ushort)(k * 4);
			array[k * 6 + 1] = (ushort)(k * 4 + 1);
			array[k * 6 + 2] = (ushort)(k * 4 + 2);
			array[k * 6 + 3] = (ushort)(k * 4 + 2);
			array[k * 6 + 4] = (ushort)(k * 4 + 3);
			array[k * 6 + 5] = (ushort)(k * 4);
		}
		m_starsIndexBuffer.SetData(array, 0, array.Length);
	}

	private void UpdateCloudMovement(float windStrength, string windDirection)
	{
		Vector2 v = (WindDirectionVectors.ContainsKey(windDirection) ? WindDirectionVectors[windDirection] : new Vector2(0f, -1f));
		float num = 1E-05f;
		float num2 = 0.02f;
		if (windStrength <= 0f)
		{
			CloudSpeed = MathUtils.Lerp(CloudSpeed, num, 0.02f);
			return;
		}
		float num3 = MathUtils.Clamp(windStrength, 1f, 16f);
		float x = MathUtils.Lerp(num, num2, (num3 - 1f) / 15f);
		CloudSpeed = MathUtils.Lerp(CloudSpeed, x, 0.05f);
		float f = ((CloudSpeed < num2 * 0.1f) ? 0.002f : 0.0005f);
		CloudDirection = Vector2.Lerp(CloudDirection, v, f);
	}

	public void FillSkyVertexBuffer(SkyDome skyDome, float timeOfDay, float precipitationIntensity, int temperature, float playerY)
	{
		for (int list = 0; list < 8; list++)
		{
			float list2 = (float)Math.PI / 2f * MathUtils.Sqr((float)list / 7f);
			for (int num = 0; num < 10; num++)
			{
				int i = num + list * 10;
				float j = (float)Math.PI * 2f * (float)num / 10f;
				float num2 = 1800f * MathUtils.Cos(list2);
				skyDome.Vertices[i].Position.X = num2 * MathUtils.Sin(j);
				skyDome.Vertices[i].Position.Z = num2 * MathUtils.Cos(j);
				skyDome.Vertices[i].Position.Y = 1800f * MathUtils.Sin(list2) - ((list == 0) ? 450f : 0f);
				if (playerY < 0f)
				{
					skyDome.Vertices[i].Color = Color.Black;
					continue;
				}
				float num3 = Vector3.Dot(Vector3.Normalize(skyDome.Vertices[i].Position), Vector3.UnitY);
				float num4 = 1f - num3;
				float x = 1f;
				if (playerY <= 55f)
				{
					x = ((!(playerY >= 45f)) ? 0f : ((playerY - 45f) / 10f));
				}
				x = MathUtils.Clamp(x, 0f, 1f);
				float f = (1f - x) * num4;
				Color c = Color.Lerp(CalculateSkyColor(skyDome.Vertices[i].Position, timeOfDay, precipitationIntensity, temperature), Color.Black, f);
				Vector3 vector = Vector3.Normalize(skyDome.Vertices[i].Position);
				Vector2 vector2 = new Vector2(vector.X, vector.Z);
				float num5 = vector2.Length();
				Vector2 v = ((num5 > 0.0001f) ? (vector2 / num5) : new Vector2(0f, 1f));
				float num6 = MathUtils.Saturate(1f - MathUtils.Abs(vector.Y));
				float num7 = 0.5f;
				Vector2 vector3 = new Vector2(m_lastCameraViewPos.X, m_lastCameraViewPos.Z);
				if (m_subsystemWeather.IsCycloneActive)
				{
					Vector2 v2 = m_subsystemWeather.CycloneCenter - vector3;
					float num8 = v2.Length();
					if (num8 > 0.0001f)
					{
						v2 /= num8;
					}
					float x2 = MathUtils.Pow(MathUtils.Saturate(Vector2.Dot(v, v2)), 1.2f);
					float num9 = MathUtils.Max(200f, m_subsystemWeather.CycloneRadius);
					float num10 = num9 * 1.5f;
					float num11 = 1f - MathUtils.Saturate((num8 - num9) / MathUtils.Max(1f, num10 - num9));
					num11 = num11 * num11 * (3f - 2f * num11);
					float num12 = num9 * 0.7f;
					float num13 = 1f - MathUtils.Saturate((num8 - num12) / MathUtils.Max(1f, num9 - num12));
					num13 = num13 * num13 * (3f - 2f * num13);
					num7 = MathUtils.Lerp(x2, 1f, num13);
					num7 = MathUtils.Lerp(num7, 1f, num11 * 0.5f);
				}
				else
				{
					List<Vector2> activeTornadoes = m_subsystemWeather.ActiveTornadoes;
					if (activeTornadoes != null && activeTornadoes.Count > 0)
					{
						int index = 0;
						float num14 = float.MaxValue;
						for (int k = 0; k < activeTornadoes.Count; k++)
						{
							float num15 = Vector2.Distance(vector3, activeTornadoes[k]);
							if (num15 < num14)
							{
								num14 = num15;
								index = k;
							}
						}
						Vector2 v3 = activeTornadoes[index] - vector3;
						float num16 = v3.Length();
						if (num16 > 0.0001f)
						{
							v3 /= num16;
							num7 = MathUtils.Pow(MathUtils.Saturate(Vector2.Dot(v, v3)), 1.25f);
						}
					}
				}
				float x3 = MathUtils.Saturate(m_stormGloomTemporal * 1.8f * num7 * (0.35f + 0.65f * num6));
				x3 = MathUtils.Saturate(x3);
				Color c2 = new Color(23, 23, 23);
				c = Color.Lerp(c, c2, x3);
				float x4 = 0f;
				if (m_subsystemOceanScanner != null)
				{
					x4 = m_subsystemOceanScanner.GetLocalOceanWaveStrength(vector3);
				}
				x4 = MathUtils.Saturate(x4);
				float num17 = MathUtils.Saturate(0f - vector.Y);
				float num18 = x4 * num17;
				if (num18 > 0f)
				{
					c = Color.Lerp(c, Color.Black, num18);
				}
				if (x4 > 0f)
				{
					float num19 = MathUtils.Saturate((0f - Vector3.Normalize(skyDome.Vertices[i].Position).Y + 0.05f) / 0.25f);
					c = Color.Lerp(c, Color.Black, x4 * num19);
				}
				skyDome.Vertices[i].Color = c;
			}
		}
		skyDome.VertexBuffer.SetData(skyDome.Vertices, 0, skyDome.Vertices.Length);
	}

	private void DrawCloudLayer(Texture2D texture, Color layerColor, float yPos, Vector3 viewPosition, Vector2 v)
	{
		float[] furnitureDesign = new float[8] { 0f, 0.8f, 0.95f, 1f, 1.01f, 1.02f, 1.03f, 1.04f };
		TexturedBatch3D texturedBatch3D = m_primitivesRenderer3d.TexturedBatch(texture, useAlphaTest: false, 2, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
		DynamicArray<VertexPositionColorTexture> hashSet = texturedBatch3D.TriangleVertices;
		DynamicArray<ushort> list = texturedBatch3D.TriangleIndices;
		int num = furnitureDesign.Length;
		int count = hashSet.Count;
		int count2 = list.Count;
		hashSet.Count += num * num;
		list.Count += 6 * (num - 1) * (num - 1);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num2 = count + i * num + j;
				int num3 = j - num / 2;
				int num4 = i - num / 2;
				int num5 = MathUtils.Max(MathUtils.Abs(num3), MathUtils.Abs(num4));
				float num6 = furnitureDesign[num5];
				float num7 = ((num5 > 0) ? (num6 / MathUtils.Sqrt(num3 * num3 + num4 * num4)) : 0f);
				float num8 = (float)num3 * num7;
				float num9 = (float)num4 * num7;
				Vector3 vector = new Vector3(viewPosition.X + num8 * 1900f, yPos, viewPosition.Z + num9 * 1900f);
				float num10 = Vector3.Distance(viewPosition, vector);
				float num11 = MathUtils.Sqr(num10 / 1900f);
				vector.Y -= num11 * 200f;
				float f = MathUtils.Saturate(num10 / 1900f);
				float num12 = MathUtils.Lerp(1f, 0.1f, f);
				float num13 = ((num5 > 3) ? 0.1f : 1f);
				float a = (float)(int)layerColor.A / 255f * num12 * num13;
				float r = (float)(int)layerColor.R / 255f * num12;
				float g = (float)(int)layerColor.G / 255f * num12;
				float b = (float)(int)layerColor.B / 255f * num12;
				Color color = new Color(r, g, b, a);
				Vector2 texCoord = new Vector2(vector.X, vector.Z) / 1900f * 1.75f - v;
				hashSet.Array[num2] = new VertexPositionColorTexture(vector, color, texCoord);
				if (i > 0 && j > 0)
				{
					ushort num14 = (ushort)(count + (i - 1) * num + (j - 1));
					ushort num15 = (ushort)(count + (i - 1) * num + j);
					ushort num16 = (ushort)(count + i * num + (j - 1));
					ushort num17 = (ushort)num2;
					list.Array[count2++] = num16;
					list.Array[count2++] = num14;
					list.Array[count2++] = num15;
					list.Array[count2++] = num15;
					list.Array[count2++] = num17;
					list.Array[count2++] = num16;
				}
			}
		}
	}

	private void DrawDustClouds(Camera camera)
	{
		float num = m_subsystemWeather.CurrentWindStrength;
		if (num < 7f)
		{
			return;
		}
		Vector3 num2 = camera.ViewPosition;
		int num3 = Terrain.ToCell(num2.X);
		int num4 = Terrain.ToCell(num2.Z);
		m_activeDustLayers = ((num >= 13f) ? 3 : ((!(num >= 10f)) ? 1 : 2));
		if (m_dustTexture == null)
		{
			m_dustTexture = m_cloudTextures[0];
		}
		if (m_dustLayerOffsets[0] == Vector2.Zero)
		{
			for (int j = 0; j < 3; j++)
			{
				m_dustLayerOffsets[j] = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
			}
		}
		bool num5 = m_subsystemOceanScanner != null && m_subsystemOceanScanner.IsOcean;
		PrecipitationShaftInfo num6 = m_subsystemWeather.GetPrecipitationShaftInfo(num3, num4);
		SubsystemWeather.Season i = m_subsystemWeather.CurrentSeason;
		Terrain num7 = m_subsystemTerrain.Terrain;
		Color num8;
		if (num5)
		{
			Color baseColor = BlockColorsMap.WaterColorsMap.Lookup(num7, num3, 64, num4);
			num8 = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(i, baseColor);
		}
		else if (num6.Type == PrecipitationType.Snow)
		{
			num8 = new Color(240, 240, 240);
		}
		else if (num6.Type == PrecipitationType.Sand)
		{
			num8 = BlockColorsMap.SandColorsMap.LookupSand(num7, num3, 64, num4);
		}
		else
		{
			Color c = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(i, num7, num3, 64, num4);
			Color c2 = new Color(70, 60, 50);
			num8 = Color.Lerp(c2: new Color(90, 90, 90), f: 0.35f * MathUtils.Saturate((float)(num7.GetSeasonalHumidity(num7.GetShaftValue(num3, num4)) - 6) / 9f), c1: Color.Lerp(c, c2, 0.75f));
		}
		float num9 = MathUtils.Lerp(0.25f, 1f, SkyLightIntensity);
		float num10 = (float)m_subsystemTime.GameTime;
		Vector2 num11 = CloudDirection;
		float num12 = CloudSpeed * 10f;
		for (int k = 0; k < m_activeDustLayers; k++)
		{
			m_dustOffsets[k] += num11 * num12 * Time.FrameDuration * (1f + (float)(k - 1) * 0.1f);
			m_dustOffsets[k] = new Vector2(MathUtils.Remainder(m_dustOffsets[k].X, 1f), MathUtils.Remainder(m_dustOffsets[k].Y, 1f));
			float num13 = MathUtils.Saturate(((num6.Type == PrecipitationType.Sand) ? 0.4f : (num5 ? 0.62f : 0.6f)) - 0.01f * (float)k);
			Color clr = new Color((float)(int)num8.R / 255f * num9 * num13, (float)(int)num8.G / 255f * num9 * num13, (float)(int)num8.B / 255f * num9 * num13, num13);
			Vector2 vector = m_dustOffsets[k] + m_dustLayerOffsets[k];
			DrawDustLayer(uvOff: new Vector2(MathUtils.Remainder(vector.X, 1f), MathUtils.Remainder(vector.Y, 1f)), tex: m_dustTexture, clr: clr, yPos: m_dustLayerHeights[k], amp: 0.45f, viewPos: num2, windDir: num11, time: num10 + (float)k * 1.5f, layerId: k);
		}
	}

	private float CalcWaveOffset(float x, float z, Vector2 windDir, float time, float amp)
	{
		float array = 0.7f;
		float num = array * 1.8f;
		float num2 = (windDir.X * x + windDir.Y * z) * 6f;
		float num3 = ((0f - windDir.Y) * x + windDir.X * z) * 6f;
		float num4 = MathUtils.Sin(time * array + num2);
		float i = MathUtils.Sin(time * num + num3) * 0.5f;
		float num5 = x * 0.25f + 37f;
		float j = z * 0.23f - 19f;
		float x2 = MathUtils.Sin(num5 * 12.9898f + j * 78.233f) * 43758.547f;
		x2 -= MathUtils.Floor(x2);
		float k = MathUtils.Sin((num5 + 11f) * 12.9898f + (j - 7f) * 78.233f) * 43758.547f;
		k -= MathUtils.Floor(k);
		float l = MathUtils.Lerp(0.65f, 1.35f, MathUtils.Saturate(0.6f * x2 + 0.4f * k));
		float num6 = MathUtils.Abs(MathUtils.Sin(num2 * 0.5f) * MathUtils.Sin(num3 * 0.5f)) * 0.25f;
		return (num4 + i) * amp * (l + num6);
	}

	private void DrawDustLayer(Texture2D tex, Color clr, float yPos, float amp, Vector3 viewPos, Vector2 uvOff, Vector2 windDir, float time, int layerId)
	{
		TexturedBatch3D texturedBatch3D = m_primitivesRenderer3d.TexturedBatch(tex, useAlphaTest: false, 2, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
		DynamicArray<VertexPositionColorTexture> i = texturedBatch3D.TriangleVertices;
		DynamicArray<ushort> num = texturedBatch3D.TriangleIndices;
		float point = MathUtils.Clamp(m_viewFogRange.Y * 0.9f, 220f, 1200f);
		int point2 = i.Count;
		i.Count += 81;
		num.Count += 384;
		bool point3 = m_subsystemOceanScanner != null && m_subsystemOceanScanner.IsOcean;
		float point4 = (point3 ? 2.2f : 1f);
		for (int j = 0; j < 9; j++)
		{
			for (int blockMesh = 0; blockMesh < 9; blockMesh++)
			{
				float blockMesh2 = ((float)blockMesh / 8f - 0.5f) * 2f;
				float blockMesh3 = ((float)j / 8f - 0.5f) * 2f;
				float blockMesh4 = MathUtils.Sqrt(blockMesh2 * blockMesh2 + blockMesh3 * blockMesh3);
				float j2 = 1f - MathUtils.Saturate(blockMesh4);
				j2 = j2 * j2 * (3f - 2f * j2);
				Vector3 array = new Vector3(viewPos.X + blockMesh2 * point, yPos, viewPos.Z + blockMesh3 * point);
				float k = CalcWaveOffset(array.X - viewPos.X, array.Z - viewPos.Z, windDir, time, amp);
				float l = 0f;
				if (point3)
				{
					float waveSurfaceY = m_subsystemOceanScanner.GetWaveSurfaceY(array);
					float value = m_subsystemOceanScanner.OceanLevel + 1;
					l = (waveSurfaceY - value) * 0.9f + k * 0.1f;
				}
				array.Y += (point3 ? l : k);
				array.Y = MathUtils.Clamp(array.Y, yPos - amp * point4, yPos + amp * point4);
				float num2 = m_dustExposure[layerId, blockMesh, j];
				float num3 = m_subsystemWeather.GetWindExposureFactor(array);
				if (num2 == 0f && num3 > 0f)
				{
					num2 = num3;
				}
				float num4 = MathUtils.Lerp(num2, num3, 0.15f);
				m_dustExposure[layerId, blockMesh, j] = num4;
				float num5 = Vector3.Distance(array, viewPos);
				float num6 = MathUtils.Saturate((m_viewFogRange.Y - num5) / Math.Max(1f, m_viewFogRange.Y - m_viewFogRange.X));
				float cell = MathUtils.Saturate(j2 * num4 * num6);
				Color color = new Color((float)(int)clr.R / 255f * cell, (float)(int)clr.G / 255f * cell, (float)(int)clr.B / 255f * cell, (float)(int)clr.A / 255f * cell);
				Vector2 num7 = new Vector2(array.X, array.Z) / point * 1.75f - uvOff;
				i.Array[point2 + j * 9 + blockMesh] = new VertexPositionColorTexture(array, color, num7);
			}
		}
		for (int m = 0; m < 8; m++)
		{
			for (int n = 0; n < 8; n++)
			{
				int value2 = point2 + m * 9 + n;
				int point6 = value2 + 1;
				int num8 = value2 + 9;
				int num9 = num8 + 1;
				num.Add((ushort)value2);
				num.Add((ushort)num8);
				num.Add((ushort)point6);
				num.Add((ushort)point6);
				num.Add((ushort)num8);
				num.Add((ushort)num9);
			}
		}
	}

	private void DrawTornadoes(Camera camera)
	{
		if (!m_subsystemWeather.IsCycloneActive)
		{
			if (m_tornadoDustSystems != null)
			{
				for (int i = 0; i < m_tornadoDustSystems.Count; i++)
				{
					TornadoDustParticleSystem tornadoDustParticleSystem = m_tornadoDustSystems[i];
					if (tornadoDustParticleSystem != null && m_subsystemParticles.ContainsParticleSystem(tornadoDustParticleSystem))
					{
						m_subsystemParticles.RemoveParticleSystem(tornadoDustParticleSystem);
					}
				}
				m_tornadoDustSystems.Clear();
			}
			if (m_tornadoDebrisSystems == null)
			{
				return;
			}
			for (int j = 0; j < m_tornadoDebrisSystems.Count; j++)
			{
				TornadoDebrisParticleSystem tornadoDebrisParticleSystem = m_tornadoDebrisSystems[j];
				if (tornadoDebrisParticleSystem != null && m_subsystemParticles.ContainsParticleSystem(tornadoDebrisParticleSystem))
				{
					m_subsystemParticles.RemoveParticleSystem(tornadoDebrisParticleSystem);
				}
			}
			m_tornadoDebrisSystems.Clear();
			return;
		}
		List<Vector2> subdivision = m_subsystemWeather.ActiveTornadoes;
		if (subdivision == null || subdivision.Count == 0)
		{
			return;
		}
		bool list = SettingsManager.DisableTornadoDust;
		bool list2 = SettingsManager.DisableTornadoDebris;
		float i2 = 1f;
		float box = 1f;
		if (m_subsystemWeather != null)
		{
			try
			{
				Type type = m_subsystemWeather.GetType();
				PropertyInfo property = type.GetProperty("TornadoUvScaleU");
				PropertyInfo property2 = type.GetProperty("TornadoUvScaleV");
				if (property != null)
				{
					object value = property.GetValue(m_subsystemWeather, null);
					if (value != null)
					{
						i2 = Convert.ToSingle(value);
					}
				}
				if (property2 != null)
				{
					object value2 = property2.GetValue(m_subsystemWeather, null);
					if (value2 != null)
					{
						box = Convert.ToSingle(value2);
					}
				}
				FieldInfo field = type.GetField("TornadoUvScaleU");
				FieldInfo field2 = type.GetField("TornadoUvScaleV");
				if (field != null)
				{
					object value3 = field.GetValue(m_subsystemWeather);
					if (value3 != null)
					{
						i2 = Convert.ToSingle(value3);
					}
				}
				if (field2 != null)
				{
					object value4 = field2.GetValue(m_subsystemWeather);
					if (value4 != null)
					{
						box = Convert.ToSingle(value4);
					}
				}
			}
			catch
			{
			}
		}
		Texture2D min = m_cloudTextures[m_currentCloudTextureType];
		TexturedBatch3D max = m_primitivesRenderer3d.TexturedBatch(min, useAlphaTest: false, 2, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
		float j2 = m_subsystemWeather.GlobalPrecipitationIntensity;
		float m = MathUtils.Sqr(SkyLightIntensity);
		float num = MathUtils.Lerp(0f, 0.14f, m);
		float k = MathUtils.Lerp(0.2f, 0.1f, j2);
		float v = MathUtils.Lerp(0.22f, 1f, j2);
		float v2 = num * k * v;
		float boundingBox = 60f;
		float num2 = Math.Max(boundingBox + 10f, m_currentHeights[0] - 10f) - boundingBox;
		float l = MathUtils.Saturate(m_subsystemWeather.TornadoStrength);
		float item = MathUtils.Lerp(0.333333f, 2f, l);
		float flag = 11.2f * item;
		int minSize = 24;
		int b = 16;
		Vector2 b2 = CloudDirection;
		float vector = ((b2.X >= 0f && b2.Y <= 0f) ? 1f : (-1f));
		float n = MathUtils.Clamp(m_subsystemWeather.CurrentWindStrength, 1f, 16f);
		float vector2 = MathUtils.Lerp(0.07f, 0.3f, (n - 1f) / 15f) * 2f;
		float num3 = (float)m_subsystemGameInfo.TotalElapsedGameTime;
		int num4 = MathUtils.Clamp(5 + (int)MathUtils.Round(5f * l), 5, 10);
		Vector2 value5 = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
		if (m_tornadoDustSystems == null)
		{
			m_tornadoDustSystems = new List<TornadoDustParticleSystem>();
		}
		if (m_tornadoDebrisSystems == null)
		{
			m_tornadoDebrisSystems = new List<TornadoDebrisParticleSystem>();
		}
		float num5 = 65f;
		float m2 = MathUtils.Min(num5 + 210f, 245f);
		float num6 = num5 + (m2 - num5) * 0.5f;
		float v3 = MathUtils.Saturate(m_subsystemWeather.TornadoStrength);
		float v4 = 1.75f;
		float boundingBox2 = 1.25f;
		Vector2 cloudDirection = CloudDirection;
		float num7 = ((cloudDirection.X >= 0f && cloudDirection.Y <= 0f) ? 1f : (-1f));
		float num8 = MathUtils.Clamp(m_subsystemWeather.CurrentWindStrength, 1f, 16f);
		float num9 = MathUtils.Lerp(0.07f, 0.3f, (num8 - 1f) / 15f) * 2f;
		float num10 = MathUtils.Clamp(11.2f * MathUtils.Lerp(0.333333f, 2f, v3) * 0.4f, 6f, 64f);
		float num11 = MathUtils.Max(1.2f, num10 * 0.2f);
		float x = num7 * num9 * MathUtils.Lerp(5.6f, 10f, v3) * v4;
		float y = MathUtils.Lerp(2.8f, 6f, v3) * boundingBox2;
		float particleSize = 1.6f * MathUtils.Lerp(1.5f, 3f, v3) * 1.1f;
		if (!list)
		{
			for (int num12 = 0; num12 < m_tornadoDustSystems.Count; num12++)
			{
				TornadoDustParticleSystem tornadoDustParticleSystem2 = m_tornadoDustSystems[num12];
				if (tornadoDustParticleSystem2 != null && m_subsystemParticles.ContainsParticleSystem(tornadoDustParticleSystem2))
				{
					m_subsystemParticles.RemoveParticleSystem(tornadoDustParticleSystem2);
				}
			}
			m_tornadoDustSystems.Clear();
		}
		else
		{
			while (m_tornadoDustSystems.Count < subdivision.Count)
			{
				int count = m_tornadoDustSystems.Count;
				Vector2 vector3 = subdivision[count];
				Vector3 vector4 = new Vector3(vector3.X, num5, vector3.Y);
				TornadoDustParticleSystem tornadoDustParticleSystem3 = new TornadoDustParticleSystem(m_subsystemTerrain, m_subsystemWeather, vector4, 300f)
				{
					BaseY = num5,
					TopY = num6,
					SpawnRadius = num10 * 1.25f,
					TopRadius = num11 * 1.25f,
					Position = vector4,
					SwirlCenter = vector4,
					Direction = new Vector3(0f, 1f, 0f),
					Swirl = new Vector2(x, y),
					ParticleSize = particleSize,
					VerticalSpeed = MathUtils.Lerp(1.8f, 3.2f, v3) * 2.8f,
					SpawnRateMultiplier = MathUtils.Lerp(0.35f, 0.65f, v3) * 1.6f,
					Lifetime = 20f,
					BaseAlpha = 90f,
					RandomOffsetEnabled = true,
					Color = Color.White
				};
				m_subsystemParticles.AddParticleSystem(tornadoDustParticleSystem3);
				m_tornadoDustSystems.Add(tornadoDustParticleSystem3);
			}
			while (m_tornadoDustSystems.Count > subdivision.Count)
			{
				int index = m_tornadoDustSystems.Count - 1;
				TornadoDustParticleSystem tornadoDustParticleSystem4 = m_tornadoDustSystems[index];
				if (tornadoDustParticleSystem4 != null && m_subsystemParticles.ContainsParticleSystem(tornadoDustParticleSystem4))
				{
					m_subsystemParticles.RemoveParticleSystem(tornadoDustParticleSystem4);
				}
				m_tornadoDustSystems.RemoveAt(index);
			}
		}
		float num13 = num7 * num9 * 0.5f;
		float spawnRadius = num10 * 5f;
		float topRadius = num11 * 5f;
		float topY = MathUtils.Max(num5 + 20f, m2 - 10f);
		float particleSize2 = MathUtils.Lerp(0.28f, 0.42f, v3) * 2.5f * 0.85f;
		if (!list2)
		{
			for (int num14 = 0; num14 < m_tornadoDebrisSystems.Count; num14++)
			{
				TornadoDebrisParticleSystem tornadoDebrisParticleSystem2 = m_tornadoDebrisSystems[num14];
				if (tornadoDebrisParticleSystem2 != null && m_subsystemParticles.ContainsParticleSystem(tornadoDebrisParticleSystem2))
				{
					m_subsystemParticles.RemoveParticleSystem(tornadoDebrisParticleSystem2);
				}
			}
			m_tornadoDebrisSystems.Clear();
		}
		else
		{
			while (m_tornadoDebrisSystems.Count < subdivision.Count)
			{
				int count2 = m_tornadoDebrisSystems.Count;
				Vector2 vector5 = subdivision[count2];
				Vector3 vector6 = new Vector3(vector5.X, num5, vector5.Y);
				TornadoDebrisParticleSystem tornadoDebrisParticleSystem3 = new TornadoDebrisParticleSystem(m_subsystemTerrain, m_subsystemWeather, vector6, 300f)
				{
					Position = vector6,
					SwirlCenter = vector6,
					BaseY = num5,
					TopY = topY,
					SpawnRadius = spawnRadius,
					TopRadius = topRadius,
					Omega = num13,
					ParticleSize = particleSize2,
					DesiredCount = 480,
					SpawnRateMultiplier = 2.8f,
					LifetimeMinMax = new Vector2(14f, 24f),
					BaseAlpha = 255f,
					Color = new Color(0.2f, 0.2f, 0.2f)
				};
				m_subsystemParticles.AddParticleSystem(tornadoDebrisParticleSystem3);
				m_tornadoDebrisSystems.Add(tornadoDebrisParticleSystem3);
			}
			while (m_tornadoDebrisSystems.Count > subdivision.Count)
			{
				int index2 = m_tornadoDebrisSystems.Count - 1;
				TornadoDebrisParticleSystem tornadoDebrisParticleSystem4 = m_tornadoDebrisSystems[index2];
				if (tornadoDebrisParticleSystem4 != null && m_subsystemParticles.ContainsParticleSystem(tornadoDebrisParticleSystem4))
				{
					m_subsystemParticles.RemoveParticleSystem(tornadoDebrisParticleSystem4);
				}
				m_tornadoDebrisSystems.RemoveAt(index2);
			}
		}
		for (int num15 = 0; num15 < subdivision.Count; num15++)
		{
			Vector2 pos = subdivision[num15];
			Vector3 vector7 = new Vector3(pos.X, num5, pos.Y);
			float tornadoRevealProgress = GetTornadoRevealProgress(pos);
			float num16 = MathUtils.Max(0.05f, tornadoRevealProgress);
			if (list && num15 < m_tornadoDustSystems.Count)
			{
				TornadoDustParticleSystem tornadoDustParticleSystem5 = m_tornadoDustSystems[num15];
				if (tornadoDustParticleSystem5 != null)
				{
					float num17 = num3 * 0.61f + (float)num15 * 1.37f;
					float num18 = 0.5f + 0.5f * MathUtils.Sin(num17);
					float num19 = ((num18 < 0.22f) ? 0f : (num10 * 0.6f * num18));
					Vector2 vector8 = new Vector2(MathUtils.Cos(num17 * 0.9f), MathUtils.Sin(num17 * 0.9f)) * num19;
					tornadoDustParticleSystem5.Position = vector7;
					tornadoDustParticleSystem5.m_position = vector7;
					tornadoDustParticleSystem5.SwirlCenter = new Vector3(vector7.X + vector8.X, num5, vector7.Z + vector8.Y);
					tornadoDustParticleSystem5.BaseY = num5;
					tornadoDustParticleSystem5.TopY = num6;
					tornadoDustParticleSystem5.SpawnRadius = num10 * 1.25f;
					tornadoDustParticleSystem5.TopRadius = num11 * 1.25f;
					tornadoDustParticleSystem5.Swirl = new Vector2(x, y);
					tornadoDustParticleSystem5.ParticleSize = particleSize;
					tornadoDustParticleSystem5.VerticalSpeed = MathUtils.Lerp(1.8f, 3.2f, v3) * 2.8f;
					tornadoDustParticleSystem5.SpawnRateMultiplier = MathUtils.Lerp(0.35f, 0.65f, v3) * 1.6f * MathUtils.Lerp(0.25f, 1f, tornadoRevealProgress);
					tornadoDustParticleSystem5.Lifetime = 20f;
					tornadoDustParticleSystem5.BaseAlpha = 90f * tornadoRevealProgress;
				}
			}
			if (list2 && num15 < m_tornadoDebrisSystems.Count)
			{
				TornadoDebrisParticleSystem tornadoDebrisParticleSystem5 = m_tornadoDebrisSystems[num15];
				if (tornadoDebrisParticleSystem5 != null)
				{
					tornadoDebrisParticleSystem5.Position = vector7;
					tornadoDebrisParticleSystem5.m_position = vector7;
					tornadoDebrisParticleSystem5.SwirlCenter = vector7;
					tornadoDebrisParticleSystem5.BaseY = num5;
					tornadoDebrisParticleSystem5.TopY = topY;
					tornadoDebrisParticleSystem5.SpawnRadius = spawnRadius;
					tornadoDebrisParticleSystem5.TopRadius = topRadius;
					tornadoDebrisParticleSystem5.Omega = num13 * num16;
					tornadoDebrisParticleSystem5.ParticleSize = particleSize2;
					tornadoDebrisParticleSystem5.DesiredCount = 480;
					tornadoDebrisParticleSystem5.SpawnRateMultiplier = 2.8f * tornadoRevealProgress;
					tornadoDebrisParticleSystem5.LifetimeMinMax = new Vector2(14f, 24f);
					tornadoDebrisParticleSystem5.Color = new Color(0.2f * tornadoRevealProgress, 0.2f * tornadoRevealProgress, 0.2f * tornadoRevealProgress);
				}
			}
		}
		for (int num20 = 0; num20 < subdivision.Count; num20++)
		{
			Vector2 vector9 = subdivision[num20];
			float num21 = num3 * 0.05f + (float)num20 * 1.234567f;
			float num22 = num3 * 0.037f + (float)num20 * 0.913f;
			float num23 = MathUtils.Clamp(flag * 1.2f, 12f, 128f);
			float tornadoRevealProgress2 = GetTornadoRevealProgress(vector9);
			float num24 = MathUtils.Max(0.05f, tornadoRevealProgress2);
			Vector2 vector10 = new Vector2(MathUtils.Sin(num21 * 0.77f), MathUtils.Cos(num21 * 0.73f));
			float num25 = vector10.Length();
			if (num25 < 0.0001f)
			{
				vector10 = new Vector2(1f, 0f);
			}
			else
			{
				vector10 /= num25;
			}
			float num26 = 0.5f + 0.5f * MathUtils.Sin(num22 * 0.63f);
			float num27 = num23 * (0.35f + 0.65f * num26);
			Vector2 vector11 = new Vector2(0f - vector10.Y, vector10.X);
			Vector2[] array = new Vector2[num4 + 1];
			for (int num28 = 0; num28 <= num4; num28++)
			{
				float num29 = (float)num28 / (float)num4;
				float num30 = MathUtils.Pow(num29, 1.7f);
				float num31 = 0.12f * num23 * MathUtils.Sin(num21 * 0.42f + (float)num28 * 0.8f);
				float num32 = num29 * (1f - 0.5f * num30);
				Vector2 vector12 = vector10 * (num27 * num30) + vector11 * (num31 * num32);
				float num33 = vector12.Length();
				float num34 = MathUtils.Max(0.01f, num27 * MathUtils.Max(0.2f, num30));
				if (num33 > num34)
				{
					vector12 *= num34 / num33;
				}
				array[num28] = vector9 + vector12;
			}
			int num35 = m_tornadoLayerRadiusFactors.Length;
			for (int num36 = 0; num36 < num35; num36++)
			{
				float num37 = m_tornadoLayerRadiusFactors[num36];
				float num38 = m_tornadoLayerSpeedFactors[num36];
				float num39 = m_tornadoLayerAlphaFactors[num36];
				float num40 = flag * num37 + 2f;
				if (!(Vector2.Distance(value5, vector9) - num40 <= m_viewFogRange.Y))
				{
					continue;
				}
				Vector2 vector13 = m_cloudOffsets[0] + m_tornadoLayerOffsets[num36];
				vector13 = new Vector2(MathUtils.Remainder(vector13.X, 1f), MathUtils.Remainder(vector13.Y, 1f));
				float x2 = vector * vector2 * num38 * num3 * num24;
				float num41 = MathUtils.Cos(x2);
				float num42 = MathUtils.Sin(x2);
				DynamicArray<VertexPositionColorTexture> triangleVertices = max.TriangleVertices;
				int count3 = triangleVertices.Count;
				triangleVertices.Count += (minSize + 1) * (b + 1);
				for (int num43 = 0; num43 <= b; num43++)
				{
					float num44 = (float)num43 / (float)b;
					float num45 = boundingBox + num2 * num44;
					float num46 = MathUtils.Saturate((num44 - (1f - tornadoRevealProgress2)) / 0.08f);
					float num47 = num44 * (float)num4;
					int num48 = (int)MathUtils.Floor(num47);
					if (num48 < 0)
					{
						num48 = 0;
					}
					if (num48 > num4 - 1)
					{
						num48 = num4 - 1;
					}
					int num49 = num48 + 1;
					float num50 = MathUtils.Saturate(num47 - (float)num48);
					num50 = num50 * num50 * (3f - 2f * num50);
					Vector2 vector14 = Vector2.Lerp(array[num48], array[num49], num50);
					float f = num44 * num44 * (3f - 2f * num44);
					float num51 = MathUtils.Lerp(0.5f, 1.7f, f);
					float num52 = flag * num37 * num51;
					float num53 = MathUtils.Saturate(MathUtils.Lerp(0.3f, 0.14f, num44) * 1.35f * num39);
					num53 *= num46;
					for (int num54 = 0; num54 <= minSize; num54++)
					{
						float num55 = num44;
						float num56 = 1.2f;
						float num57 = (((num36 & 1) == 0) ? 1f : (-1f));
						float x3 = (float)num54 / (float)minSize * 6.283185f + num57 * (6.283185f * num56) * num55;
						float num58 = MathUtils.Cos(x3) * num52;
						float num59 = MathUtils.Sin(x3) * num52;
						float num60 = num58 * num41 - num59 * num42;
						float x4 = vector14.X + num58;
						float z = vector14.Y + num59;
						Vector3 vector15 = new Vector3(x4, num45, z);
						float num61 = Vector3.Distance(vector15, camera.ViewPosition);
						float num62 = MathUtils.Saturate((m_viewFogRange.Y - num61) / Math.Max(1f, m_viewFogRange.Y - m_viewFogRange.X));
						float num63 = num53 * num62;
						if (num46 <= 0f || num63 < 0.01f)
						{
							num63 = 0f;
						}
						Color color = new Color(v2, v2, v2, num63);
						float x5 = (new Vector2(vector14.X + num60, 0f) / 1900f).X * (10.75f * i2) - vector13.X;
						float y2 = num45 / 1900f * (10.75f * box) - vector13.Y;
						triangleVertices.Array[count3 + num43 * (minSize + 1) + num54] = new VertexPositionColorTexture(vector15, color, new Vector2(x5, y2));
					}
				}
				for (int num64 = 0; num64 < b; num64++)
				{
					for (int num65 = 0; num65 < minSize; num65++)
					{
						int num66 = count3 + num64 * (minSize + 1) + num65;
						int num67 = num66 + 1;
						int num68 = num66 + (minSize + 1);
						int num69 = num68 + 1;
						max.TriangleIndices.Add((ushort)num66);
						max.TriangleIndices.Add((ushort)num68);
						max.TriangleIndices.Add((ushort)num67);
						max.TriangleIndices.Add((ushort)num67);
						max.TriangleIndices.Add((ushort)num68);
						max.TriangleIndices.Add((ushort)num69);
					}
				}
			}
		}
	}

	private void DrawCyclone(Camera camera)
	{
		if (!m_subsystemWeather.IsCycloneActive)
		{
			return;
		}
		float list = GetCycloneSpawnProgress();
		Vector3 i = camera.ViewPosition;
		Vector2 j = new Vector2(i.X, i.Z);
		Vector2 k = m_subsystemWeather.CycloneCenter;
		float num = MathUtils.Max(200f, m_subsystemWeather.CycloneRadius) * 1.5f;
		float boundingBox = m_currentHeights[0];
		float num2 = 5f;
		float l = 1.2f;
		Texture2D boundingBox2 = m_cloudTextures[m_currentCloudTextureType];
		TexturedBatch3D vector = m_primitivesRenderer3d.TexturedBatch(boundingBox2, useAlphaTest: false, 2, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
		float vector2 = 700f;
		float m = MathUtils.Clamp(num / MathUtils.Max(1f, vector2), 1f, 4f);
		float m2 = 0.00092105265f * m;
		float n = MathUtils.Clamp(num / MathUtils.Max(1f, vector2), 1f, 4f);
		float v = 0.0056578945f * n;
		float v2 = m_subsystemWeather.GlobalPrecipitationIntensity;
		float f = MathUtils.Sqr(SkyLightIntensity);
		float num3 = MathUtils.Lerp(0f, 0.14f, f);
		float num4 = MathUtils.Lerp(0.2f, 0.1f, v2);
		float num5 = MathUtils.Lerp(0.22f, 1f, v2);
		float num6 = num3 * num4 * num5;
		float num7 = MathUtils.Clamp(m_subsystemWeather.CurrentWindStrength, 1f, 16f);
		float num8 = MathUtils.Lerp(1.6f, 4.4f, (num7 - 1f) / 15f);
		float num9 = 0.15f;
		Vector2 vector3 = m_cloudOffsets[0] + m_cloudLayerOffsets[0];
		int num10 = m_cycloneLayerRadiusFactors.Length;
		if (num10 <= 0)
		{
			return;
		}
		for (int num11 = 0; num11 < num10; num11++)
		{
			float num12 = LayerProgress(list, num11, num10);
			if (!(num12 > 0f))
			{
				continue;
			}
			float x = ((num10 > 1) ? ((float)num11 / (float)(num10 - 1)) : 0f);
			float num13;
			if (m_cycloneLayerRadiusFactors == null || m_cycloneLayerRadiusFactors.Length == 0)
			{
				num13 = 1f;
			}
			else if (m_cycloneLayerRadiusFactors.Length == 1)
			{
				num13 = m_cycloneLayerRadiusFactors[0];
			}
			else
			{
				float num14 = (float)(m_cycloneLayerRadiusFactors.Length - 1) * MathUtils.Saturate(x);
				int num15 = (int)MathUtils.Floor(num14);
				int num16 = MathUtils.Min(num15 + 1, m_cycloneLayerRadiusFactors.Length - 1);
				num13 = MathUtils.Lerp(m_cycloneLayerRadiusFactors[num15], m_cycloneLayerRadiusFactors[num16], num14 - (float)num15);
			}
			float num17;
			if (m_cycloneLayerAlphaFactors == null || m_cycloneLayerAlphaFactors.Length == 0)
			{
				num17 = 1f;
			}
			else if (m_cycloneLayerAlphaFactors.Length == 1)
			{
				num17 = m_cycloneLayerAlphaFactors[0];
			}
			else
			{
				float num18 = (float)(m_cycloneLayerAlphaFactors.Length - 1) * MathUtils.Saturate(x);
				int num19 = (int)MathUtils.Floor(num18);
				int num20 = MathUtils.Min(num19 + 1, m_cycloneLayerAlphaFactors.Length - 1);
				num17 = MathUtils.Lerp(m_cycloneLayerAlphaFactors[num19], m_cycloneLayerAlphaFactors[num20], num18 - (float)num19);
			}
			Vector2 vector4;
			if (m_cycloneLayerUvOffsets == null || m_cycloneLayerUvOffsets.Length == 0)
			{
				vector4 = Vector2.Zero;
			}
			else if (m_cycloneLayerUvOffsets.Length == 1)
			{
				vector4 = m_cycloneLayerUvOffsets[0];
			}
			else
			{
				float num21 = (float)(m_cycloneLayerUvOffsets.Length - 1) * MathUtils.Saturate(x);
				int num22 = (int)MathUtils.Floor(num21);
				int num23 = MathUtils.Min(num22 + 1, m_cycloneLayerUvOffsets.Length - 1);
				vector4 = Vector2.Lerp(m_cycloneLayerUvOffsets[num22], m_cycloneLayerUvOffsets[num23], num21 - (float)num22);
			}
			float y = boundingBox - num2 - (float)num11 * l;
			float num24 = num * num13;
			float num25 = num24 * 2f / 47f;
			if (!(Vector2.Distance(j, k) - num24 <= m_viewFogRange.Y))
			{
				continue;
			}
			Vector2 vector5 = k;
			Vector2 vector6 = vector3 + vector4;
			float num26 = num9 * (float)num11;
			float num27 = MathUtils.Max(80f, num24 * 0.18f);
			DynamicArray<VertexPositionColorTexture> triangleVertices = vector.TriangleVertices;
			int count = triangleVertices.Count;
			triangleVertices.Count += 2304;
			float num28 = vector5.X - 23.5f * num25;
			float num29 = vector5.Y - 23.5f * num25;
			for (int num30 = 0; num30 < 48; num30++)
			{
				for (int num31 = 0; num31 < 48; num31++)
				{
					float num32 = num28 + (float)num31 * num25;
					float num33 = num29 + (float)num30 * num25;
					float num34 = num32 - vector5.X;
					float num35 = num33 - vector5.Y;
					float num36 = MathUtils.Sqrt(num34 * num34 + num35 * num35);
					float num37 = MathUtils.Saturate(num36 / Math.Max(1f, num24));
					float x2 = num26 + num8 * num37;
					float num38 = MathUtils.Cos(x2);
					float num39 = MathUtils.Sin(x2);
					float num40 = num34 * num38 - num35 * num39;
					float num41 = num34 * num39 + num35 * num38;
					Vector2 texCoord = new Vector2(vector5.X + num40, vector5.Y + num41) * m2 - vector6;
					float num42 = MathUtils.Saturate((num36 - (num24 - num27)) / Math.Max(1f, num27));
					float num43 = MathUtils.Pow(1f - num42, 0.6f);
					Vector3 vector7 = new Vector3(num32, y, num33);
					float num44 = Vector3.Distance(vector7, i);
					float num45 = MathUtils.Saturate((m_viewFogRange.Y - num44) / Math.Max(1f, m_viewFogRange.Y - m_viewFogRange.X));
					Color color = new Color(num6, num6, num6, num17 * num12 * num43 * num45);
					triangleVertices.Array[count + num30 * 48 + num31] = new VertexPositionColorTexture(vector7, color, texCoord);
				}
			}
			int count2 = vector.TriangleIndices.Count;
			vector.TriangleIndices.Count += 13254;
			int num46 = count2;
			for (int num47 = 0; num47 < 47; num47++)
			{
				for (int num48 = 0; num48 < 47; num48++)
				{
					int num49 = count + num47 * 48 + num48;
					int num50 = num49 + 1;
					int num51 = num49 + 48;
					int num52 = num51 + 1;
					vector.TriangleIndices.Array[num46++] = (ushort)num49;
					vector.TriangleIndices.Array[num46++] = (ushort)num51;
					vector.TriangleIndices.Array[num46++] = (ushort)num50;
					vector.TriangleIndices.Array[num46++] = (ushort)num50;
					vector.TriangleIndices.Array[num46++] = (ushort)num51;
					vector.TriangleIndices.Array[num46++] = (ushort)num52;
				}
			}
		}
		float num53 = 65f;
		float num54 = MathUtils.Max(num53 + 30f, boundingBox - num2);
		if (num54 <= num53)
		{
			return;
		}
		float num55 = 0.05f;
		bool flag = true;
		float num56 = 0.3f;
		float num57 = 0.1f;
		float num58 = 0.03f;
		float num59 = (float)m_subsystemGameInfo.TotalElapsedGameTime;
		float num60 = MathUtils.Clamp(m_subsystemWeather.CurrentWindStrength, 1f, 16f);
		float num61 = MathUtils.Lerp(0.05f, 0.22f, (num60 - 1f) / 15f);
		float num62 = ((CloudDirection.X >= 0f && CloudDirection.Y <= 0f) ? 1f : (-1f));
		float num64;
		float num63 = (num64 = MathUtils.Lerp(num * 0.04f, num * 0.08f, 0.5f) * 30f) * (1f + num56);
		int num65 = 16;
		int num66 = 10;
		float num67 = MathUtils.Clamp(vector2 / MathUtils.Max(1f, num), 0.25f, 1f);
		float num68 = Vector2.Distance(j, k);
		for (int num69 = 0; num69 < 6; num69++)
		{
			float num70 = LayerProgress(list, num69, 6);
			if (!(num70 > 0f))
			{
				continue;
			}
			float num71 = MathUtils.Max(30f, num * 0.05f);
			float x3 = (num - num71 - num63) / Math.Max(1f, num);
			x3 = MathUtils.Clamp(x3, 0.12f, 0.95f);
			float num72 = MathUtils.Min(MathUtils.Clamp(m_cycloneCylinderRadialFactors[num69], 0.2f, 0.9f), x3);
			float num73 = num * num72;
			float num74 = num62 * num61 * num55 * num67;
			if (flag && num73 > 1f)
			{
				num74 *= num64 / num73;
			}
			float x4 = m_cycloneCylinderAngleOffsets[num69] + num74 * num59;
			float f2 = (float)num69 / 5f;
			float num75 = MathUtils.Lerp(num64, num63, f2);
			float num76 = MathUtils.Max(4f, num - (num73 + num71));
			if (num75 > num76)
			{
				num75 = num76 * 0.95f;
			}
			float num77 = num73 + num75;
			if (!(num68 - num77 <= m_viewFogRange.Y))
			{
				continue;
			}
			float num78 = k.X + MathUtils.Cos(x4) * num73;
			float num79 = k.Y + MathUtils.Sin(x4) * num73;
			DynamicArray<VertexPositionColorTexture> triangleVertices2 = vector.TriangleVertices;
			int count3 = triangleVertices2.Count;
			triangleVertices2.Count += (num66 + 1) * (num65 + 1);
			float x5 = (0f - num74) * num59;
			float num80 = MathUtils.Cos(x5);
			float num81 = MathUtils.Sin(x5);
			Vector2 vector8 = m_cycloneCylinderUvOffsets[num69];
			for (int num82 = 0; num82 <= num66; num82++)
			{
				float num83 = (float)num82 / (float)num66;
				float num84 = MathUtils.Lerp(num53, num54, num83);
				float num85 = num83 * 2f - 1f;
				float num86 = 1f - num57 * (1f - num85 * num85);
				float num87 = num58 * MathUtils.Sin(6.283185f * num83 + m_cycloneCylinderAngleOffsets[num69] * 0.5f);
				float num88 = MathUtils.Clamp(num86 + num87, 0.85f, 1.15f);
				float num89 = num75 * num88;
				if (num89 > num76)
				{
					num89 = num76;
				}
				for (int num90 = 0; num90 <= num65; num90++)
				{
					float x6 = (float)num90 / (float)num65 * 6.283185f;
					float x7 = num78 + MathUtils.Cos(x6) * num89;
					float z = num79 + MathUtils.Sin(x6) * num89;
					Vector3 vector9 = new Vector3(x7, num84, z);
					float num91 = MathUtils.Cos(x6) * num89;
					float num92 = MathUtils.Sin(x6) * num89;
					float num93 = num91 * num80 - num92 * num81;
					float x8 = (num78 + num93) * v - vector8.X + 0.07f * num59;
					float y2 = num84 * v - vector8.Y + 0.025f * num59;
					float num94 = Vector3.Distance(vector9, i);
					float num95 = MathUtils.Saturate((m_viewFogRange.Y - num94) / Math.Max(1f, m_viewFogRange.Y - m_viewFogRange.X));
					float num96 = MathUtils.Saturate(1f - MathUtils.Max(0f, num68 - num77) / MathUtils.Max(1f, m_viewFogRange.Y));
					float a = 0.15f * num95 * num70 * num96;
					triangleVertices2.Array[count3 + num82 * (num65 + 1) + num90] = new VertexPositionColorTexture(vector9, new Color(num6, num6, num6, a), new Vector2(x8, y2));
				}
			}
			for (int num97 = 0; num97 < num66; num97++)
			{
				for (int num98 = 0; num98 < num65; num98++)
				{
					int num99 = count3 + num97 * (num65 + 1) + num98;
					int num100 = num99 + 1;
					int num101 = num99 + (num65 + 1);
					int num102 = num101 + 1;
					vector.TriangleIndices.Add((ushort)num99);
					vector.TriangleIndices.Add((ushort)num101);
					vector.TriangleIndices.Add((ushort)num100);
					vector.TriangleIndices.Add((ushort)num100);
					vector.TriangleIndices.Add((ushort)num101);
					vector.TriangleIndices.Add((ushort)num102);
				}
			}
		}
	}

	public void MakeLightningStrike(Vector3 targetPosition, byte lightningType)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			NetMakeLightningStrike(targetPosition, lightningType);
			CommonLib.Net.QueuePackage(new SubsystemSkyPackage(targetPosition, lightningType));
		}
	}

	public void NetMakeLightningStrike(Vector3 targetPosition, byte lightningType)
	{
		if (m_lightningStrikePosition.HasValue || m_subsystemTime.GameTime - m_lastLightningStrikeTime <= 2.0)
		{
			return;
		}
		m_lastLightningStrikeTime = m_subsystemTime.GameTime;
		m_lightningStrikePosition = targetPosition;
		m_currentLightningType = (byte)(lightningType % 3);
		m_lightningStrikeBrightness = 1f;
		m_lightSeed = (uint)m_random.Int(1, int.MaxValue);
		m_lightStartTime = m_subsystemTime.GameTime;
		m_lightVisualDuration = ((m_lightLifetime > 0f) ? m_lightLifetime : 1f);
		BuildLightningSkeleton(m_currentLightningType);
		float i = float.MaxValue;
		foreach (Vector3 num3 in m_subsystemAudio.ListenerPositions)
		{
			float value6 = Vector2.Distance(new Vector2(num3.X, num3.Z), new Vector2(targetPosition.X, targetPosition.Z));
			if (value6 < i)
			{
				i = value6;
			}
		}
		float j = m_subsystemAudio.CalculateDelay(i);
		float num4 = m_random.Float(-0.2f, 0.2f);
		if (i <= 40f)
		{
			m_subsystemAudio.PlayRandomSound("Audio/ThunderNear", 1f, num4, 0f, j);
		}
		else
		{
			float num5 = MathUtils.Saturate((i - 40f) / 860f);
			float num6 = 0.9f * (1f - num5);
			num6 *= num6;
			if (num6 > 0.02f)
			{
				m_subsystemAudio.PlayRandomSound("Audio/ThunderFar", num6, num4, 0f, j);
			}
		}
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		int value7 = Terrain.ToCell(targetPosition.X);
		int k = Terrain.ToCell(targetPosition.Y);
		int l = Terrain.ToCell(targetPosition.Z);
		for (int n = value7 - 1; n <= value7 + 1; n++)
		{
			for (int num7 = l - 1; num7 <= l + 1; num7++)
			{
				for (int num8 = k - 1; num8 <= k + 1; num8++)
				{
					if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(n, num8, num7)) == 8)
					{
						m_subsystemTerrain.ChangeCell(n, num8, num7, Terrain.MakeBlockValue(2));
					}
				}
			}
		}
		DynamicArray<ComponentBody> value8 = new DynamicArray<ComponentBody>();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2(targetPosition.X, targetPosition.Z), 2f, value8);
		for (int num9 = 0; num9 < value8.Count; num9++)
		{
			value8.Array[num9].Entity.FindComponent<ComponentOnFire>()?.SetOnFire(null, m_random.Float(10f, 15f));
		}
	}

	private void BuildLightningSkeleton(byte type)
	{
		m_lightMain = new DynamicArray<Vector3>();
		m_lightBranches = new DynamicArray<DynamicArray<Vector3>>();
		Vector3 num = m_lightningStrikePosition.Value;
		float result = 260f;
		float num2 = num.Y;
		switch (type)
		{
		case 0:
		{
			Vector3 box7 = new Vector3(num.X, result, num.Z);
			Vector3 subdivision5 = new Vector3(num.X, num2, num.Z);
			m_lightMain = BuildPathBetween(box7, subdivision5, MathUtils.Max(12, (int)((result - num2) / 8f)), 16f);
			return;
		}
		case 1:
		{
			float subdivision6 = result - (result - num2) * 0.5f;
			Vector3 num12 = new Vector3(num.X, result, num.Z);
			Vector3 num13 = new Vector3(num.X, subdivision6, num.Z);
			m_lightMain = BuildPathBetween(num12, num13, MathUtils.Max(10, (int)((result - subdivision6) / 8f)), 14f);
			float num14 = (float)(MathUtils.Hash(m_lightSeed + 915488749) & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
			float x = num14 + (float)Math.PI + m_random.Float(-0.61f, 0.61f);
			float num15 = m_random.Float(60f, 110f);
			float num16 = m_random.Float(70f, 120f);
			float num17 = m_random.Float(12f, 40f);
			float num18 = m_random.Float(12f, 40f);
			Vector3 vector = new Vector3(MathUtils.Cos(num14), 0f, MathUtils.Sin(num14));
			Vector3 vector2 = new Vector3(MathUtils.Cos(x), 0f, MathUtils.Sin(x));
			Vector3 end = num13 + vector * num15 + new Vector3(0f, 0f - num17, 0f);
			Vector3 end2 = num13 + vector2 * num16 + new Vector3(0f, 0f - num18, 0f);
			DynamicArray<Vector3> item = BuildPathBetween(num13, end, 16 + m_random.Int(0, 6), 8f);
			DynamicArray<Vector3> item2 = BuildPathBetween(num13, end2, 16 + m_random.Int(0, 6), 8f);
			m_lightBranches.Add(item);
			m_lightBranches.Add(item2);
			return;
		}
		}
		float box8 = result - num2;
		float box9 = result - box8 / 3f;
		float subdivision7 = result - 2f * box8 / 3f;
		uint subdivision8 = MathUtils.Hash(m_lightSeed + 2654435769u);
		uint num19 = MathUtils.Hash(m_lightSeed + 2246822507u);
		float num20 = (float)(subdivision8 & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
		float num21 = (float)(num19 & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
		float i = MathUtils.Lerp(35f, 80f, (float)((subdivision8 >> 16) & 0xFF) / 255f);
		float box10 = MathUtils.Lerp(25f, 70f, (float)((num19 >> 16) & 0xFF) / 255f);
		Vector3 box11 = new Vector3(num.X, result, num.Z);
		Vector3 subdivision9 = new Vector3(num.X + i * MathUtils.Cos(num20), box9, num.Z + i * MathUtils.Sin(num20));
		Vector3 subdivision10 = new Vector3(num.X + box10 * MathUtils.Cos(num21), subdivision7, num.Z + box10 * MathUtils.Sin(num21));
		Vector3 num22 = new Vector3(num.X, num2, num.Z);
		DynamicArray<Vector3> num23 = BuildPathBetween(box11, subdivision9, MathUtils.Max(8, (int)(Vector3.Distance(box11, subdivision9) / 7f)), 12f);
		DynamicArray<Vector3> num24 = BuildPathBetween(subdivision9, subdivision10, MathUtils.Max(8, (int)(Vector3.Distance(subdivision9, subdivision10) / 7f)), 10f);
		DynamicArray<Vector3> j = BuildPathBetween(subdivision10, num22, MathUtils.Max(12, (int)(Vector3.Distance(subdivision10, num22) / 6f)), 8f);
		m_lightMain.AddRange(num23);
		if (m_lightMain.Count > 0)
		{
			m_lightMain.RemoveAt(m_lightMain.Count - 1);
		}
		m_lightMain.AddRange(num24);
		if (m_lightMain.Count > 0)
		{
			m_lightMain.RemoveAt(m_lightMain.Count - 1);
		}
		m_lightMain.AddRange(j);
		Vector3 box12 = Vector3.Lerp(subdivision10, num22, 0.6f);
		for (int k = 0; k < 2; k++)
		{
			uint num25 = MathUtils.Hash(m_lightSeed + 1234567 + (uint)(int)((long)k * 1013904223L));
			float x2 = (float)(num25 & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
			float num26 = MathUtils.Lerp(8f, 18f, (float)((num25 >> 16) & 0xFF) / 255f);
			float num27 = MathUtils.Lerp(3f, 9f, (float)((num25 >> 8) & 0xFF) / 255f);
			Vector3 vector3 = new Vector3(MathUtils.Cos(x2), 0f, MathUtils.Sin(x2));
			Vector3 end3 = box12 + vector3 * num26 + new Vector3(0f, 0f - num27, 0f);
			m_lightBranches.Add(BuildPathBetween(box12, end3, (int)(6 + (num25 & 3)), 4f));
		}
	}

	private DynamicArray<Vector3> BuildPathBetween(Vector3 start, Vector3 end, int steps, float ampXZ)
	{
		DynamicArray<Vector3> result = new DynamicArray<Vector3>();
		if (steps < 2)
		{
			steps = 2;
		}
		Vector3 num = end - start;
		if (num.LengthSquared() < 1E-06f)
		{
			result.Add(start);
			result.Add(end);
			return result;
		}
		num = Vector3.Normalize(num);
		Vector3 i = Vector3.Normalize(Vector3.Cross(num, Vector3.UnitY));
		if (i.LengthSquared() < 1E-06f)
		{
			i = Vector3.UnitX;
		}
		Vector3 j = Vector3.Normalize(Vector3.Cross(num, i));
		for (int point = 0; point <= steps; point++)
		{
			float num2 = (float)point / (float)steps;
			uint num3 = MathUtils.Hash(m_lightSeed + (uint)(point * -2048144777));
			float x = (float)(num3 & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
			float num4 = ampXZ * (0.25f + 0.75f * ((float)((num3 >> 16) & 0xFF) / 255f));
			float num5 = 1f - MathUtils.Abs(2f * num2 - 1f);
			num4 *= 0.6f + 0.4f * num5;
			Vector3 vector = i * (MathUtils.Cos(x) * num4) + j * (MathUtils.Sin(x) * num4);
			Vector3 item = Vector3.Lerp(start, end, num2) + vector;
			result.Add(item);
		}
		return result;
	}

	private Vector3 StableJitter(int idx, double time, float amp)
	{
		uint num = MathUtils.Hash(m_lightSeed + (uint)(idx * -1028477379));
		float i = (float)(num & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
		float j = (float)((num >> 16) & 0xFFFF) / 65535f * ((float)Math.PI * 2f);
		float x = (float)MathUtils.Sin(time * 45.0 + (double)i) * amp;
		float z = (float)MathUtils.Cos(time * 39.0 + (double)j) * amp;
		return new Vector3(x, 0f, z);
	}

	private void AddLightningQuad(FlatBatch3D fb, Vector3 p0, Vector3 p1, Vector3 camDir, float w0, float w1, float intensity0, float intensity1)
	{
		Vector3 i = p1 - p0;
		if (!(i.LengthSquared() < 1E-06f))
		{
			Vector3 vector = Vector3.Normalize(Vector3.Cross(camDir, i));
			if (vector.LengthSquared() < 1E-06f)
			{
				vector = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, i));
			}
			if (vector.LengthSquared() < 1E-06f)
			{
				vector = Vector3.UnitX;
			}
			Vector3 vector2 = p0 - vector * w0;
			Vector3 p2 = p0 + vector * w0;
			Vector3 vector3 = p1 + vector * w1;
			Vector3 p3 = p1 - vector * w1;
			Color color = Color.White * intensity0;
			Color color2 = Color.White * intensity0;
			Color color3 = Color.White * intensity1;
			Color color4 = Color.White * intensity1;
			fb.QueueTriangle(vector2, p2, vector3, color, color2, color3);
			fb.QueueTriangle(vector3, p3, vector2, color3, color4, color);
		}
	}

	public override void Save(ValuesDictionary values)
	{
		base.Save(values);
		for (int i = 0; i < 3; i++)
		{
			values.SetValue($"CloudAlpha{i}", m_currentStageAlphas[i]);
		}
		values.SetValue("CycloneStartWorldTime", m_cycloneStartWorldTime);
		string text = "";
		bool flag = true;
		for (int j = 0; j < m_tornadoTracks.Count; j++)
		{
			TornadoTrack tornadoTrack = m_tornadoTracks[j];
			if (!flag)
			{
				text += ";";
			}
			flag = false;
			text = text + tornadoTrack.Pos.X.ToString(CultureInfo.InvariantCulture) + "," + tornadoTrack.Pos.Y.ToString(CultureInfo.InvariantCulture) + "=" + tornadoTrack.Start.ToString(CultureInfo.InvariantCulture);
		}
		values.SetValue("TornadoStarts", text);
	}

	private float GetCycloneSpawnProgress()
	{
		if (m_cycloneStartWorldTime >= 0.0)
		{
			return MathUtils.Saturate((float)((m_subsystemGameInfo.TotalElapsedGameTime - m_cycloneStartWorldTime) / 300.0));
		}
		if (!m_subsystemWeather.IsCycloneActive)
		{
			return 0f;
		}
		return 1f;
	}

	private float GetTornadoRevealProgress(Vector2 pos)
	{
		int num = FindTrackIndex(pos, 64f);
		if (num < 0)
		{
			return 0f;
		}
		return MathUtils.Saturate((float)((m_subsystemGameInfo.TotalElapsedGameTime - m_tornadoTracks[num].Start) / 60.0));
	}

	private static float LayerProgress(float p, int layerIndex, int layersCount)
	{
		if (layersCount <= 0)
		{
			return p;
		}
		float num = 1f / (float)layersCount;
		float num2 = (float)layerIndex * num;
		return MathUtils.Saturate((p - num2) / num);
	}

	private int FindTrackIndex(Vector2 pos, float radius)
	{
		float resolution = radius * radius;
		int result = -1;
		float steps = float.MaxValue;
		for (int i = 0; i < m_tornadoTracks.Count; i++)
		{
			float i2 = Vector2.DistanceSquared(m_tornadoTracks[i].Pos, pos);
			if (i2 <= resolution && i2 < steps)
			{
				steps = i2;
				result = i;
			}
		}
		return result;
	}

	private float GetCycloneCoreFactorForCamera(Vector2 camXZ)
	{
		if (m_subsystemWeather == null || !m_subsystemWeather.IsCycloneActive)
		{
			return 0f;
		}
		Vector2 cycloneCenter = m_subsystemWeather.CycloneCenter;
		float x = MathUtils.Max(200f, m_subsystemWeather.CycloneRadius) * 1.25f;
		float num = Vector2.Distance(camXZ, cycloneCenter);
		return MathUtils.Saturate(1f - num / MathUtils.Max(1f, x));
	}

	private float GetCyclonePrecipFactorForCamera(Vector2 camXZ)
	{
		if (m_subsystemWeather == null || !m_subsystemWeather.IsCycloneActive)
		{
			return 0f;
		}
		Vector2 cycloneCenter = m_subsystemWeather.CycloneCenter;
		float num = MathUtils.Max(200f, m_subsystemWeather.CycloneRadius);
		float x = (num * 2.75f + 320f) * 1.5f;
		float num2 = Vector2.Distance(camXZ, cycloneCenter);
		float x2 = MathUtils.Pow(MathUtils.Saturate(1f - num2 / MathUtils.Max(1f, x)), 0.5f);
		float x3 = MathUtils.Saturate(1f - num2 / MathUtils.Max(1f, num * 1.25f));
		float f = MathUtils.Saturate((m_subsystemWeather.CurrentWindStrength - 1f) / 15f);
		float num3 = MathUtils.Lerp(0.35f, 0.95f, f);
		return MathUtils.Saturate(MathUtils.Max(x3, x2) * num3);
	}
}
