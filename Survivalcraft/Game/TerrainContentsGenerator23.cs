using System;
using System.Collections.Generic;
using Engine;

namespace Game;

public class TerrainContentsGenerator23 : ITerrainContentsGenerator
{
	public class CavePoint
	{
		public Vector3 Position;

		public Vector3 Direction;

		public int BrushType;

		public int Length;

		public int StepsTaken;
	}

	public class Grid2d
	{
		public int m_sizeX;

		public int m_sizeY;

		public float[] m_data;

		public int SizeX => m_sizeX;

		public int SizeY => m_sizeY;

		public Grid2d(int sizeX, int sizeY)
		{
			m_sizeX = sizeX;
			m_sizeY = sizeY;
			m_data = new float[m_sizeX * m_sizeY];
		}

		public float Get(int x, int y)
		{
			return m_data[x + y * m_sizeX];
		}

		public void Set(int x, int y, float value)
		{
			m_data[x + y * m_sizeX] = value;
		}

		public float Sample(float x, float y)
		{
			int num = (int)MathUtils.Floor(x);
			int num2 = (int)MathUtils.Floor(y);
			int num3 = (int)MathUtils.Ceiling(x);
			int num4 = (int)MathUtils.Ceiling(y);
			float f = x - (float)num;
			float f2 = y - (float)num2;
			float x2 = m_data[num + num2 * m_sizeX];
			float x3 = m_data[num3 + num2 * m_sizeX];
			float x4 = m_data[num + num4 * m_sizeX];
			float x5 = m_data[num3 + num4 * m_sizeX];
			float x6 = MathUtils.Lerp(x2, x3, f);
			float x7 = MathUtils.Lerp(x4, x5, f);
			return MathUtils.Lerp(x6, x7, f2);
		}
	}

	public class Grid3d
	{
		public int m_sizeX;

		public int m_sizeY;

		public int m_sizeZ;

		public int m_sizeXY;

		public float[] m_data;

		public int SizeX => m_sizeX;

		public int SizeY => m_sizeY;

		public int SizeZ => m_sizeZ;

		public Grid3d(int sizeX, int sizeY, int sizeZ)
		{
			m_sizeX = sizeX;
			m_sizeY = sizeY;
			m_sizeZ = sizeZ;
			m_sizeXY = m_sizeX * m_sizeY;
			m_data = new float[m_sizeX * m_sizeY * m_sizeZ];
		}

		public void Get8(int x, int y, int z, out float v111, out float v211, out float v121, out float v221, out float v112, out float v212, out float v122, out float v222)
		{
			int num = x + y * m_sizeX + z * m_sizeXY;
			v111 = m_data[num];
			v211 = m_data[num + 1];
			v121 = m_data[num + m_sizeX];
			v221 = m_data[num + 1 + m_sizeX];
			v112 = m_data[num + m_sizeXY];
			v212 = m_data[num + 1 + m_sizeXY];
			v122 = m_data[num + m_sizeX + m_sizeXY];
			v222 = m_data[num + 1 + m_sizeX + m_sizeXY];
		}

		public float Get(int x, int y, int z)
		{
			return m_data[x + y * m_sizeX + z * m_sizeXY];
		}

		public void Set(int x, int y, int z, float value)
		{
			m_data[x + y * m_sizeX + z * m_sizeXY] = value;
		}

		public float Sample(float x, float y, float z)
		{
			int num = (int)MathUtils.Floor(x);
			int num2 = (int)MathUtils.Ceiling(x);
			int num3 = (int)MathUtils.Floor(y);
			int num4 = (int)MathUtils.Ceiling(y);
			int num5 = (int)MathUtils.Floor(z);
			int num6 = (int)MathUtils.Ceiling(z);
			float f = x - (float)num;
			float f2 = y - (float)num3;
			float f3 = z - (float)num5;
			float x2 = m_data[num + num3 * m_sizeX + num5 * m_sizeX * m_sizeY];
			float x3 = m_data[num2 + num3 * m_sizeX + num5 * m_sizeX * m_sizeY];
			float x4 = m_data[num + num4 * m_sizeX + num5 * m_sizeX * m_sizeY];
			float x5 = m_data[num2 + num4 * m_sizeX + num5 * m_sizeX * m_sizeY];
			float x6 = m_data[num + num3 * m_sizeX + num6 * m_sizeX * m_sizeY];
			float x7 = m_data[num2 + num3 * m_sizeX + num6 * m_sizeX * m_sizeY];
			float x8 = m_data[num + num4 * m_sizeX + num6 * m_sizeX * m_sizeY];
			float x9 = m_data[num2 + num4 * m_sizeX + num6 * m_sizeX * m_sizeY];
			float x10 = MathUtils.Lerp(x2, x3, f);
			float x11 = MathUtils.Lerp(x4, x5, f);
			float x12 = MathUtils.Lerp(x6, x7, f);
			float x13 = MathUtils.Lerp(x8, x9, f);
			float x14 = MathUtils.Lerp(x10, x11, f2);
			float x15 = MathUtils.Lerp(x12, x13, f2);
			return MathUtils.Lerp(x14, x15, f3);
		}
	}

	public struct TerrainLODPoint
	{
		public float Height;

		public int Temperature;

		public int Humidity;
	}

	public static List<TerrainBrush> m_coalBrushes;

	public static List<TerrainBrush> m_ironBrushes;

	public static List<TerrainBrush> m_copperBrushes;

	public static List<TerrainBrush> m_saltpeterBrushes;

	public static List<TerrainBrush> m_sulphurBrushes;

	public static List<TerrainBrush> m_diamondBrushes;

	public static List<TerrainBrush> m_germaniumBrushes;

	public static List<TerrainBrush> m_dirtPocketBrushes;

	public static List<TerrainBrush> m_gravelPocketBrushes;

	public static List<TerrainBrush> m_limestonePocketBrushes;

	public static List<TerrainBrush> m_sandPocketBrushes;

	public static List<TerrainBrush> m_basaltPocketBrushes;

	public static List<TerrainBrush> m_granitePocketBrushes;

	public static List<TerrainBrush> m_clayPocketBrushes;

	public static List<TerrainBrush> m_waterPocketBrushes;

	public static List<TerrainBrush> m_magmaPocketBrushes;

	public static List<List<TerrainBrush>> m_caveBrushesByType;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemBottomSuckerBlockBehavior m_subsystemBottomSuckerBlockBehavior;

	public WorldSettings m_worldSettings;

	public int m_seed;

	public Vector2? m_islandSize;

	public Vector2 m_oceanCorner;

	public Vector2 m_temperatureOffset;

	public Vector2 m_humidityOffset;

	public Vector2 m_mountainsOffset;

	public Vector2 m_riversOffset;

	public float TGBiomeScaling;

	public float TGShoreFluctuations;

	public float TGShoreFluctuationsScaling;

	public float TGOceanSlope;

	public float TGOceanSlopeVariation;

	public float TGIslandsFrequency;

	public float TGDensityBias;

	public float TGHeightBias;

	public float TGHillsPercentage;

	public float TGHillsStrength;

	public int TGHillsOctaves;

	public float TGHillsFrequency;

	public float TGHillsPersistence;

	public float TGMountainsStrength;

	public float TGMountainRangeFreq;

	public float TGMountainsPercentage;

	public static float TGMountainsDetailFreq;

	public static int TGMountainsDetailOctaves;

	public static float TGMountainsDetailPersistence;

	public float TGRiversStrength;

	public float TGTurbulenceStrength;

	public float TGTurbulenceFreq;

	public int TGTurbulenceOctaves;

	public float TGTurbulencePersistence;

	public float TGMinTurbulence;

	public float TGTurbulenceZero;

	public static float TGSurfaceMultiplier;

	public bool TGWater;

	public bool TGExtras;

	public bool TGCavesAndPockets;

	public float TGIslandsRandomFactor;

	private Vector2 m_islandOffset;

	public static List<TerrainBrush> m_tinBrushes;

	public Vector2 m_lakesWaterOffset;

	public Vector2 m_lakesMagmaOffset;

	public Vector2 m_lakesWaterMaskOffset;

	public Vector2 m_undergroundRiversOffset;

	public Vector2 m_magmaRiversOffset;

	public Vector2 m_cavesScaleOffset;

	public int OceanLevel => 64 + m_worldSettings.SeaLevelOffset;

	static TerrainContentsGenerator23()
	{
		m_coalBrushes = new List<TerrainBrush>();
		m_ironBrushes = new List<TerrainBrush>();
		m_copperBrushes = new List<TerrainBrush>();
		m_saltpeterBrushes = new List<TerrainBrush>();
		m_sulphurBrushes = new List<TerrainBrush>();
		m_diamondBrushes = new List<TerrainBrush>();
		m_germaniumBrushes = new List<TerrainBrush>();
		m_dirtPocketBrushes = new List<TerrainBrush>();
		m_gravelPocketBrushes = new List<TerrainBrush>();
		m_limestonePocketBrushes = new List<TerrainBrush>();
		m_sandPocketBrushes = new List<TerrainBrush>();
		m_basaltPocketBrushes = new List<TerrainBrush>();
		m_granitePocketBrushes = new List<TerrainBrush>();
		m_clayPocketBrushes = new List<TerrainBrush>();
		m_waterPocketBrushes = new List<TerrainBrush>();
		m_magmaPocketBrushes = new List<TerrainBrush>();
		m_caveBrushesByType = new List<List<TerrainBrush>>();
		if (m_tinBrushes == null)
		{
			m_tinBrushes = new List<TerrainBrush>();
		}
		CreateBrushes();
	}

	public TerrainContentsGenerator23(SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		m_subsystemBottomSuckerBlockBehavior = subsystemTerrain.Project.FindSubsystem<SubsystemBottomSuckerBlockBehavior>(throwOnError: true);
		SubsystemGameInfo subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		SpecialSeedsManager.SeedModifiers activeModifiers = subsystemGameInfo.ActiveModifiers;
		m_worldSettings = subsystemGameInfo.WorldSettings;
		m_seed = subsystemGameInfo.WorldSeed;
		m_islandSize = ((m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island) ? new Vector2?(m_worldSettings.IslandSize) : ((Vector2?)null));
		Random random = new Random(m_seed);
		float num = (m_islandSize.HasValue ? MathUtils.Min(m_islandSize.Value.X, m_islandSize.Value.Y) : float.MaxValue);
		m_oceanCorner = new Vector2(-200f, -200f);
		m_temperatureOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_humidityOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_mountainsOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_riversOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_lakesWaterOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_lakesMagmaOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_lakesWaterMaskOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_undergroundRiversOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_magmaRiversOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		m_cavesScaleOffset = new Vector2(random.Float(-3000f, 3000f), random.Float(-3000f, 3000f));
		TGBiomeScaling = ((m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island) ? 1f : 1.75f) * m_worldSettings.BiomeSize;
		TGShoreFluctuations = MathUtils.Clamp(2f * num, 0f, 150f);
		TGShoreFluctuationsScaling = MathUtils.Clamp(0.04f * num, 0.5f, 3f);
		TGOceanSlope = 0.01f;
		TGOceanSlopeVariation = 0.004f;
		TGIslandsFrequency = 0.001f;
		TGIslandsRandomFactor = 0.3f;
		TGDensityBias = 55f;
		TGHeightBias = 1f + activeModifiers.HeightAdd;
		TGRiversStrength = 2.5f;
		TGMountainsStrength = 355f;
		TGMountainRangeFreq = 0.0006f;
		TGMountainsPercentage = 0.13f;
		TGMountainsDetailFreq = 0.001f;
		TGMountainsDetailOctaves = 1;
		TGMountainsDetailPersistence = 1.53f;
		TGHillsPercentage = 0.3f;
		TGHillsStrength = 10f;
		TGHillsOctaves = 1;
		TGHillsFrequency = 0.014f;
		TGHillsPersistence = 0.5f;
		TGTurbulenceStrength = 55f;
		TGTurbulenceFreq = 0.03f;
		TGTurbulenceOctaves = 1;
		TGTurbulencePersistence = 0.5f;
		TGMinTurbulence = 0.04f;
		TGTurbulenceZero = 0.84f;
		TGSurfaceMultiplier = 2f;
		TGWater = true;
		TGExtras = true;
		TGCavesAndPockets = true;
	}

	public Vector3 FindCoarseSpawnPosition()
	{
		Vector2 vector = Vector2.Zero;
		float num = float.MinValue;
		Random random = new Random(m_seed);
		StartingPositionMode startingPositionMode = m_subsystemTerrain.SubsystemGameInfo.WorldSettings.StartingPositionMode;
		for (int i = 0; i < 1000; i += 5)
		{
			for (int j = 0; j <= 12; j += 4)
			{
				for (int k = 0; k < 2; k++)
				{
					float num2 = startingPositionMode switch
					{
						StartingPositionMode.Easy => (float)j + (float)random.Int(-120, 80), 
						StartingPositionMode.Medium => (float)j + (float)random.Int(100, 600), 
						_ => (!random.Bool(0.5f)) ? ((float)random.Int(800, 2000)) : ((float)random.Int(-800, -400)), 
					};
					float num3;
					float x;
					if (k == 0)
					{
						num3 = m_oceanCorner.Y + (float)i;
						x = CalculateOceanShoreX(num3) + num2;
					}
					else
					{
						x = m_oceanCorner.X + (float)i;
						num3 = CalculateOceanShoreZ(x) + num2;
					}
					float num4 = ScoreSpawnPosition(Terrain.ToCell(x), Terrain.ToCell(num3));
					if (num4 > num)
					{
						vector = new Vector2(x, num3);
						num = num4;
					}
				}
			}
		}
		return new Vector3(vector.X, CalculateHeight(vector.X, vector.Y), vector.Y);
	}

	public void GenerateChunkContentsPass1(TerrainChunk chunk)
	{
		GenerateSurfaceParameters(chunk, 0, 0, 16, 8);
		GenerateTerrain(chunk, 0, 0, 16, 8);
	}

	public void GenerateChunkContentsPass2(TerrainChunk chunk)
	{
		GenerateSurfaceParameters(chunk, 0, 8, 16, 16);
		GenerateTerrain(chunk, 0, 8, 16, 16);
	}

	public void GenerateChunkContentsPass3(TerrainChunk chunk)
	{
		GeneratePockets(chunk);
		GenerateMinerals(chunk);
		GenerateSneakCaves(chunk);
		GenerateUniversalCaves(chunk);
		GenerateUndergroundLakes(chunk);
		GenerateSurface(chunk);
		PropagateFluidsDownwards(chunk);
	}

	public void GenerateChunkContentsPass4(TerrainChunk chunk)
	{
		DryDesertRivers(chunk);
		GenerateGrassAndPlants(chunk);
		GenerateLogs(chunk);
		GenerateGraniteBoulders(chunk);
		GenerateMenhirs(chunk);
		GenerateForgottenHouse(chunk);
		GenerateLighthouse(chunk);
		GenerateRuinedWalls(chunk);
		GeneratePortalFrames(chunk);
		GenerateSandTemple(chunk);
		GenerateTrees(chunk);
		GenerateCacti(chunk);
		GeneratePumpkins(chunk);
		GenerateKelp(chunk);
		GenerateBushes(chunk);
		GenerateSeagrass(chunk);
		GenerateBottomSuckers(chunk);
		GenerateTraps(chunk);
		GenerateQuicksandLakes(chunk);
		GenerateOases(chunk);
		GenerateMeteoriteCraters(chunk);
		GenerateDesertBoulders(chunk);
		GenerateIvy(chunk);
		GenerateGraves(chunk);
		GenerateCairns(chunk);
		GenerateGlaciers(chunk);
		GenerateSnowAndIce(chunk);
		GenerateBedrockAndAir(chunk);
		UpdateFluidIsTop(chunk);
		GenerateSeaStones(chunk);
		GenerateArcheologicalSoil(chunk);
		GenerateAnimalBones(chunk);
	}

	public float CalculateOceanShoreDistance(float x, float z)
	{
		float frequency = 0.002f;
		float num = 400f;
		float num2 = SimplexNoise.OctavedNoise(x, z, frequency, 2, 2f, 0.5f) * num;
		float num3 = SimplexNoise.OctavedNoise(x + 500f, z - 500f, frequency, 2, 2f, 0.5f) * num;
		float num4 = x + num2;
		float num5 = z + num3;
		if (m_islandSize.HasValue)
		{
			float num6 = CalculateOceanShoreX(num5);
			float num7 = CalculateOceanShoreZ(num4);
			float num8 = CalculateOceanShoreX(num5 + 1000f) + m_islandSize.Value.X;
			float num9 = CalculateOceanShoreZ(num4 + 1000f) + m_islandSize.Value.Y;
			return MathUtils.Min(num4 - num6, num5 - num7, num8 - num4, num9 - num5);
		}
		float num10 = CalculateOceanShoreX(num5);
		float num11 = CalculateOceanShoreZ(num4);
		return MathUtils.Min(num4 - num10, num5 - num11);
	}

	public float CalculateMountainRangeFactor(float x, float z)
	{
		return SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, TGMountainRangeFreq / TGBiomeScaling, 3, 1.91f, 0.75f, ridged: true);
	}

	public float CalculateHeight(float x, float z)
	{
		float vector = CalculateRoughnessFactor(x, z);
		float num = TGOceanSlope + TGOceanSlopeVariation * MathUtils.PowSign(2f * SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, 0.01f, 1, 2f, 0.5f) - 1f, 0.5f);
		float num2 = CalculateOceanShoreDistance(x, z);
		float num3 = MathUtils.Saturate(2f - 0.05f * MathUtils.Abs(num2));
		float num4 = MathUtils.Lerp(0.5f, 1.5f, MathUtils.Saturate(SimplexNoise.OctavedNoise(x + m_islandOffset.X, z + m_islandOffset.Y, 0.002f, 2, 2f, 0.7f)));
		float num5 = MathUtils.Saturate(MathUtils.Sin(TGIslandsFrequency * num2 * num4));
		float num6 = MathUtils.Saturate(MathUtils.Saturate((0f - num) * num2) - 0.85f * num5);
		float num7 = MathUtils.Saturate(MathUtils.Saturate(0.05f * (0f - num2 - 10f)) - num5);
		float v = CalculateMountainRangeFactor(x, z);
		float f = (1f - num3) * SimplexNoise.OctavedNoise(x, z, 0.001f / TGBiomeScaling, 2, 2f, 0.5f);
		float f2 = (1f - num3) * SimplexNoise.OctavedNoise(x, z, 0.0017f / TGBiomeScaling, 2, 4f, 0.7f);
		float num8 = (1f - num7) * (1f - num3) * Squish(v, 1f - TGHillsPercentage, 1f - TGMountainsPercentage);
		float num9 = (1f - num7) * Squish(v, 1f - TGMountainsPercentage, 1f);
		float frequency = MathUtils.Lerp(TGHillsFrequency * 0.8f, TGHillsFrequency * 3f, vector);
		float num10 = 1f * SimplexNoise.OctavedNoise(x, z, frequency, TGHillsOctaves, 1.93f, TGHillsPersistence);
		float amplitudeStep = MathUtils.Lerp(0.75f * TGMountainsDetailPersistence, 1.33f * TGMountainsDetailPersistence, f);
		float num11 = 1.5f * SimplexNoise.OctavedNoise(x, z, TGMountainsDetailFreq, TGMountainsDetailOctaves, 1.98f, amplitudeStep) - 0.5f;
		float num12 = SimplexNoise.OctavedNoise(x, z, 0.06f, 2, 2f, 0.5f);
		float num13 = vector * 4.5f * num12;
		float num14 = MathUtils.Lerp(60f, 30f, MathUtils.Saturate(1f * num9 + 0.5f * num8 + MathUtils.Saturate(1f - num2 / 30f)));
		float x2 = MathUtils.Lerp(-2f, -4f, MathUtils.Saturate(num9 + 0.5f * num8));
		float num15 = MathUtils.Saturate(1.5f - num14 * MathUtils.Abs(2f * SimplexNoise.OctavedNoise(x + m_riversOffset.X, z + m_riversOffset.Y, 0.001f, 4, 2f, 0.5f) - 1f));
		float num16 = -50f * num6 + TGHeightBias;
		float num17 = MathUtils.Lerp(0f, 8f, f);
		float num18 = MathUtils.Lerp(0f, -6f, f2);
		float num19 = MathUtils.Lerp(1f, 1.5f, vector);
		float num20 = TGHillsStrength * num19 * num8 * num10;
		float num21 = 10f * SimplexNoise.OctavedNoise(x + m_temperatureOffset.X, z + m_temperatureOffset.Y, 0.001f, 3, 2f, 0.5f);
		float num22 = 10f * SimplexNoise.OctavedNoise(x + m_humidityOffset.X, z + m_humidityOffset.Y, 0.001f, 3, 2f, 0.5f);
		float x3 = MathUtils.Clamp(num21 / 8f, 0f, 1f);
		float x4 = MathUtils.Clamp(6f / num22, 0f, 1f);
		float f3 = MathUtils.Min(x3, x4);
		float num23 = MathUtils.Lerp(1f, 2.6666667f, f3);
		num20 *= num23;
		float num24 = TGMountainsStrength * num9 * num11;
		float f4 = TGRiversStrength * num15;
		float num25 = num16 + num17 + num18 + num24 + num20 + num13;
		float num26 = SimplexNoise.OctavedNoise(x, z, 0.0004f, 2, 2f, 0.5f);
		if (num26 < -0.4f)
		{
			float num27 = (MathUtils.Abs(num26) - 0.4f) * 180f;
			num25 -= num27;
		}
		float num28 = MathUtils.Min(MathUtils.Lerp(num25, x2, f4), num25);
		return MathUtils.Clamp(64f + num28, 10f, 251f);
	}

	public int CalculateTemperature(float x, float z)
	{
		float num = (float)Math.Sqrt(x * x + z * z);
		float num2 = 1f;
		if (num > 30000f)
		{
			num2 = MathUtils.Clamp(1f - (num - 30000f) / 1000f, 0f, 1f);
		}
		return MathUtils.Clamp((int)(MathUtils.Saturate((3f * SimplexNoise.OctavedNoise(x + m_temperatureOffset.X, z + m_temperatureOffset.Y, 0.0015f / TGBiomeScaling, 5, 2f, 0.6f) - 1.1f + m_worldSettings.TemperatureOffset / 16f) * num2) * 16f), 0, 15);
	}

	public int CalculateHumidity(float x, float z)
	{
		float vector = (float)Math.Sqrt(x * x + z * z);
		float speed = 1f;
		if (vector > 30000f)
		{
			speed = MathUtils.Clamp(1f - (vector - 30000f) / 1000f, 0f, 1f);
		}
		return MathUtils.Clamp((int)(MathUtils.Saturate((3f * SimplexNoise.OctavedNoise(x + m_humidityOffset.X, z + m_humidityOffset.Y, 0.0012f / TGBiomeScaling, 5, 2f, 0.6f) - 0.9f + m_worldSettings.HumidityOffset / 16f) * speed) * 16f), 0, 15);
	}

	public static float Squish(float v, float zero, float one)
	{
		return MathUtils.Saturate((v - zero) / (one - zero));
	}

	public float CalculateOceanShoreX(float z)
	{
		return m_oceanCorner.X + TGShoreFluctuations * SimplexNoise.OctavedNoise(z, 0f, 0.005f / TGShoreFluctuationsScaling, 4, 1.95f, 1f);
	}

	public float CalculateOceanShoreZ(float x)
	{
		return m_oceanCorner.Y + TGShoreFluctuations * SimplexNoise.OctavedNoise(0f, x, 0.005f / TGShoreFluctuationsScaling, 4, 1.95f, 1f);
	}

	public float ScoreSpawnPosition(int x, int z)
	{
		int num = CalculateTemperature(x, z);
		int num2 = CalculateHumidity(x, z);
		float num3 = CalculateHeight(x, z);
		StartingPositionMode startingPositionMode = m_subsystemTerrain.SubsystemGameInfo.WorldSettings.StartingPositionMode;
		float num4 = 0f;
		switch (startingPositionMode)
		{
		case StartingPositionMode.Easy:
			if (num > 12)
			{
				num4 -= 10f;
			}
			if (num < 6)
			{
				num4 -= 10f;
			}
			if (num3 < 64f)
			{
				num4 += 2f;
			}
			else
			{
				num4 = ((!(num3 <= 68f)) ? (num4 - (num3 - 68f) * 0.5f) : (num4 + 5f));
			}
			if (num2 < 4)
			{
				num4 -= 2f;
			}
			break;
		case StartingPositionMode.Medium:
			if (num < 2)
			{
				num4 -= 2f;
			}
			if (num3 > 75f)
			{
				num4 += 5f;
			}
			if (num3 > 90f)
			{
				num4 += 2f;
			}
			if (num2 > 8)
			{
				num4 += 3f;
			}
			if (num3 < 64f)
			{
				num4 -= 5f;
			}
			break;
		default:
			if (num3 < 55f)
			{
				num4 += 10f;
			}
			if (num3 > 110f)
			{
				num4 += 15f;
			}
			else if (num3 > 90f)
			{
				num4 += 5f;
			}
			if (num3 >= 64f && num3 <= 75f)
			{
				num4 -= 20f;
			}
			if (num > 6 && num < 12 && num2 > 10)
			{
				num4 += 2f;
			}
			break;
		}
		return num4;
	}

	public void GenerateSurfaceParameters(TerrainChunk chunk, int x1, int z1, int x2, int z2)
	{
		for (int i = x1; i < x2; i++)
		{
			for (int j = z1; j < z2; j++)
			{
				int num = i + chunk.Origin.X;
				int num2 = j + chunk.Origin.Y;
				int temperature = CalculateTemperature(num, num2);
				int humidity = CalculateHumidity(num, num2);
				chunk.SetTemperatureFast(i, j, temperature);
				chunk.SetHumidityFast(i, j, humidity);
			}
		}
	}

	public void GenerateTerrain(TerrainChunk chunk, int x1, int z1, int x2, int z2)
	{
		int num = x2 - x1;
		int num2 = z2 - z1;
		int originX = chunk.Origin.X + x1;
		int originZ = chunk.Origin.Y + z1;
		Grid2d shoreGrid = new Grid2d(num, num2);
		Grid2d mountainGrid = new Grid2d(num, num2);
		Grid3d densityGrid = new Grid3d(num / 4 + 1, 33, num2 / 4 + 1);
		CalculateTerrainDensity(densityGrid, shoreGrid, mountainGrid, originX, originZ, num, num2);
		FillChunkBlocks(chunk, densityGrid, shoreGrid, mountainGrid, x1, z1, x2, z2);
	}

	public void GenerateSurface(TerrainChunk chunk)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		Random random = new Random(m_seed + chunk.Coords.X + 101 * chunk.Coords.Y);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = i + chunk.Origin.X;
				int num2 = j + chunk.Origin.Y;
				int num3 = TerrainChunk.CalculateCellIndex(i, 254, j);
				int num4 = 254;
				while (num4 >= 0)
				{
					int num5 = Terrain.ExtractContents(chunk.GetCellValueFast(num3));
					if (!BlocksManager.Blocks[num5].IsTransparent)
					{
						float num6 = CalculateMountainRangeFactor(num, num2);
						int temperature = terrain.GetTemperature(num, num2);
						int humidity = terrain.GetHumidity(num, num2);
						int contentsAbove = ((num4 + 1 < 255) ? chunk.GetCellContentsFast(i, num4 + 1, j) : 0);
						float shoreDist = CalculateOceanShoreDistance(num, num2);
						int surfaceBlockIndex = GetSurfaceBlockIndex(temperature, humidity, num4, shoreDist, contentsAbove, num5);
						int num7;
						if (surfaceBlockIndex == 62)
						{
							num7 = (int)MathUtils.Clamp(1f * (0f - (float)temperature), 1f, 7f);
						}
						else
						{
							float num8 = MathUtils.Saturate(((float)num4 - 100f) * 0.05f);
							float f = MathUtils.Saturate(MathUtils.Saturate((num6 - 0.9f) / 0.1f) - MathUtils.Saturate(((float)humidity - 3f) / 12f) + TGSurfaceMultiplier * num8);
							int min = (int)MathUtils.Lerp(4f, 0f, f);
							int max = (int)MathUtils.Lerp(7f, 0f, f);
							num7 = MathUtils.Min(random.Int(min, max), num4);
						}
						int num9 = TerrainChunk.CalculateCellIndex(i, num4 + 1, j);
						for (int k = num9 - num7; k < num9; k++)
						{
							if (Terrain.ExtractContents(chunk.GetCellValueFast(k)) != 0)
							{
								int value = Terrain.ReplaceContents(0, surfaceBlockIndex);
								chunk.SetCellValueFast(k, value);
							}
						}
						break;
					}
					num4--;
					num3--;
				}
			}
		}
		for (int l = 0; l < 16; l++)
		{
			for (int m = 0; m < 16; m++)
			{
				int num10 = l + chunk.Origin.X;
				int num11 = m + chunk.Origin.Y;
				if (!(CalculateOceanShoreDistance(num10, num11) < -8f))
				{
					continue;
				}
				int num12 = TerrainChunk.CalculateCellIndex(l, 0, m);
				for (int n = 0; n <= 254; n++)
				{
					int index = num12 + n;
					int cellValueFast = chunk.GetCellValueFast(index);
					if (Terrain.ExtractContents(cellValueFast) == 92)
					{
						chunk.SetCellValueFast(index, Terrain.ReplaceContents(cellValueFast, 18));
					}
				}
			}
		}
	}

	public void GenerateMinerals(TerrainChunk chunk)
	{
		if (!TGCavesAndPockets)
		{
			return;
		}
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		for (int i = x - 1; i <= x + 1; i++)
		{
			for (int j = y - 1; j <= y + 1; j++)
			{
				Random random = new Random(m_seed + i + 119 * j);
				int num = random.Int(0, 10);
				for (int k = 0; k < num; k++)
				{
					random.Int(0, 1);
				}
				float num2 = CalculateMountainRangeFactor(i * 16, j * 16);
				int num3 = (int)(5f + 3f * num2 * SimplexNoise.OctavedNoise(i, j, 0.33f, 1, 1f, 1f));
				for (int l = 0; l < num3; l++)
				{
					int x2 = i * 16 + random.Int(0, 15);
					int y2 = random.Int(5, 200);
					int z = j * 16 + random.Int(0, 15);
					TerrainBrush terrainBrush = m_coalBrushes[random.Int(0, m_coalBrushes.Count - 1)];
					terrainBrush.PaintFastSelective(chunk, x2, y2, z, 2);
					terrainBrush.PaintFastSelective(chunk, x2, y2, z, 3);
					terrainBrush.PaintFastSelective(chunk, x2, y2, z, 72);
					terrainBrush.PaintFastSelective(chunk, x2, y2, z, 350);
				}
				int num4 = (int)(6f + 2f * num2 * SimplexNoise.OctavedNoise(i + 1211, j + 396, 0.33f, 1, 1f, 1f));
				for (int m = 0; m < num4; m++)
				{
					int x3 = i * 16 + random.Int(0, 15);
					int y3 = random.Int(20, 65);
					int z2 = j * 16 + random.Int(0, 15);
					TerrainBrush terrainBrush2 = m_copperBrushes[random.Int(0, m_copperBrushes.Count - 1)];
					terrainBrush2.PaintFastSelective(chunk, x3, y3, z2, 2);
					terrainBrush2.PaintFastSelective(chunk, x3, y3, z2, 3);
					terrainBrush2.PaintFastSelective(chunk, x3, y3, z2, 72);
					terrainBrush2.PaintFastSelective(chunk, x3, y3, z2, 350);
				}
				int num5 = (int)(5f + 2f * num2 * SimplexNoise.OctavedNoise(i + 713, j + 211, 0.33f, 1, 1f, 1f));
				for (int n = 0; n < num5; n++)
				{
					int x4 = i * 16 + random.Int(0, 15);
					int y4 = random.Int(2, 40);
					int z3 = j * 16 + random.Int(0, 15);
					m_ironBrushes[random.Int(0, m_ironBrushes.Count - 1)].PaintFastSelective(chunk, x4, y4, z3, 67);
				}
				int num6 = (int)(3f + 3f * num2 * SimplexNoise.OctavedNoise(i + 915, j + 272, 0.33f, 1, 1f, 1f));
				for (int num7 = 0; num7 < num6; num7++)
				{
					int x5 = i * 16 + random.Int(0, 15);
					int y5 = random.Int(20, 90);
					int z4 = j * 16 + random.Int(0, 15);
					m_saltpeterBrushes[random.Int(0, m_saltpeterBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z4, 4);
				}
				int num8 = (int)(3f + 2f * num2 * SimplexNoise.OctavedNoise(i + 711, j + 1194, 0.33f, 1, 1f, 1f));
				for (int num9 = 0; num9 < num8; num9++)
				{
					int x6 = i * 16 + random.Int(0, 15);
					int y6 = random.Int(2, 40);
					int z5 = j * 16 + random.Int(0, 15);
					m_sulphurBrushes[random.Int(0, m_sulphurBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z5, 67);
				}
				int num10 = (int)(0.5f + 2f * num2 * SimplexNoise.OctavedNoise(i + 432, j + 907, 0.33f, 1, 1f, 1f));
				for (int num11 = 0; num11 < num10; num11++)
				{
					int num12 = i * 16 + random.Int(0, 15);
					int num13 = j * 16 + random.Int(0, 15);
					int y7 = random.Int(4, 11);
					int num14 = random.Int(3, 7);
					int num15 = random.Int(0, 1);
					for (int num16 = 0; num16 < num14; num16++)
					{
						int num17 = ((num15 == 0) ? num16 : 0);
						int num18 = ((num15 == 1) ? num16 : 0);
						int num19 = num12 + num17;
						int num20 = num13 + num18;
						if (num19 >= i * 16 && num19 < (i + 1) * 16 && num20 >= j * 16 && num20 < (j + 1) * 16)
						{
							m_diamondBrushes[random.Int(0, m_diamondBrushes.Count - 1)].PaintFastSelective(chunk, num19, y7, num20, 67);
						}
					}
				}
				int num21 = (int)(3f + 2f * num2 * SimplexNoise.OctavedNoise(i + 799, j + 131, 0.33f, 1, 1f, 1f));
				for (int num22 = 0; num22 < num21; num22++)
				{
					int x7 = i * 16 + random.Int(0, 15);
					int y8 = random.Int(2, 50);
					int z6 = j * 16 + random.Int(0, 15);
					m_germaniumBrushes[random.Int(0, m_germaniumBrushes.Count - 1)].PaintFastSelective(chunk, x7, y8, z6, 67);
				}
				int num23 = (int)(3f + 2f * num2 * SimplexNoise.OctavedNoise(i + 1319, j + 847, 0.33f, 1, 1f, 1f));
				for (int num24 = 0; num24 < num23; num24++)
				{
					int x8 = i * 16 + random.Int(0, 15);
					int y9 = random.Int(170, 230);
					int z7 = j * 16 + random.Int(0, 15);
					TerrainBrush terrainBrush3 = m_tinBrushes[random.Int(0, m_tinBrushes.Count - 1)];
					terrainBrush3.PaintFastSelective(chunk, x8, y9, z7, 3);
					terrainBrush3.PaintFastSelective(chunk, x8, y9, z7, 66);
					terrainBrush3.PaintFastSelective(chunk, x8, y9, z7, 4);
					terrainBrush3.PaintFastSelective(chunk, x8, y9, z7, 350);
				}
			}
		}
	}

	public void GeneratePockets(TerrainChunk chunk)
	{
		if (!TGCavesAndPockets)
		{
			return;
		}
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				int num = i + chunk.Coords.X;
				int num2 = j + chunk.Coords.Y;
				Random random = new Random(m_seed + num + 71 * num2);
				int num3 = random.Int(0, 10);
				for (int k = 0; k < num3; k++)
				{
					random.Int(0, 1);
				}
				CalculateMountainRangeFactor((float)num * 16f, (float)num2 * 16f);
				for (int l = 0; l < 3; l++)
				{
					int x = num * 16 + random.Int(0, 15);
					int y = random.Int(50, 100);
					int z = num2 * 16 + random.Int(0, 15);
					m_dirtPocketBrushes[random.Int(0, m_dirtPocketBrushes.Count - 1)].PaintFastSelective(chunk, x, y, z, 3);
				}
				for (int m = 0; m < 10; m++)
				{
					int x2 = num * 16 + random.Int(0, 15);
					int y2 = random.Int(20, 80);
					int z2 = num2 * 16 + random.Int(0, 15);
					m_gravelPocketBrushes[random.Int(0, m_gravelPocketBrushes.Count - 1)].PaintFastSelective(chunk, x2, y2, z2, 3);
				}
				for (int n = 0; n < 2; n++)
				{
					int x3 = num * 16 + random.Int(0, 15);
					int y3 = random.Int(20, 120);
					int z3 = num2 * 16 + random.Int(0, 15);
					m_limestonePocketBrushes[random.Int(0, m_limestonePocketBrushes.Count - 1)].PaintFastSelective(chunk, x3, y3, z3, 3);
				}
				for (int num4 = 0; num4 < 1; num4++)
				{
					int x4 = num * 16 + random.Int(0, 15);
					int y4 = random.Int(50, 70);
					int z4 = num2 * 16 + random.Int(0, 15);
					m_clayPocketBrushes[random.Int(0, m_clayPocketBrushes.Count - 1)].PaintFastSelective(chunk, x4, y4, z4, 3);
				}
				for (int num5 = 0; num5 < 6; num5++)
				{
					int x5 = num * 16 + random.Int(0, 15);
					int y5 = random.Int(40, 80);
					int z5 = num2 * 16 + random.Int(0, 15);
					m_sandPocketBrushes[random.Int(0, m_sandPocketBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z5, 4);
				}
				for (int num6 = 0; num6 < 4; num6++)
				{
					int x6 = num * 16 + random.Int(0, 15);
					int y6 = random.Int(40, 60);
					int z6 = num2 * 16 + random.Int(0, 15);
					m_basaltPocketBrushes[random.Int(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z6, 4);
				}
				for (int num7 = 0; num7 < 3; num7++)
				{
					int x7 = num * 16 + random.Int(0, 15);
					int y7 = random.Int(20, 40);
					int z7 = num2 * 16 + random.Int(0, 15);
					m_basaltPocketBrushes[random.Int(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x7, y7, z7, 3);
				}
				for (int num8 = 0; num8 < 6; num8++)
				{
					int x8 = num * 16 + random.Int(0, 15);
					int y8 = random.Int(4, 50);
					int z8 = num2 * 16 + random.Int(0, 15);
					m_granitePocketBrushes[random.Int(0, m_granitePocketBrushes.Count - 1)].PaintFastSelective(chunk, x8, y8, z8, 67);
				}
			}
		}
	}

	public void GenerateLogs(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		Terrain num = m_subsystemTerrain.Terrain;
		int x = chunk.Origin.X;
		int num2 = x + 16;
		int y = chunk.Origin.Y;
		int num3 = y + 16;
		int x2 = chunk.Coords.X;
		int y2 = chunk.Coords.Y;
		for (int i = x2; i <= x2; i++)
		{
			for (int j = y2; j <= y2; j++)
			{
				Random random = new Random(m_seed + i + 5137 * j);
				int humidity = CalculateHumidity(i * 16, j * 16);
				int num4 = CalculateTemperature(i * 16, j * 16);
				float num5 = MathUtils.Saturate((SimplexNoise.OctavedNoise(i, j, 0.1f, 2, 2f, 0.5f) - 0.25f) / 0.2f + (random.Bool(0.25f) ? 0.5f : 0f));
				int num6 = 0;
				if (num5 > 0.9f)
				{
					num6 = random.Int(1, 2);
				}
				else if (num5 > 0.6f)
				{
					num6 = random.Int(0, 1);
				}
				int num7 = 0;
				for (int k = 0; k < 12; k++)
				{
					if (num7 >= num6)
					{
						break;
					}
					int num8 = i * 16 + random.Int(0, 15);
					int num9 = j * 16 + random.Int(0, 15);
					int num10 = num.CalculateTopmostCellHeight(num8, num9);
					if (num10 < 66)
					{
						continue;
					}
					int cellContentsFast = num.GetCellContentsFast(num8, num10, num9);
					if (cellContentsFast != 2 && cellContentsFast != 8)
					{
						continue;
					}
					num10++;
					int num11 = random.Int(3, 7);
					Point3 point = CellFace.FaceToPoint3(random.Int(0, 3));
					if (point.X < 0 && num8 - num11 + 1 < 0)
					{
						point.X *= -1;
					}
					if (point.X > 0 && num8 + num11 - 1 > 15)
					{
						point.X *= -1;
					}
					if (point.Z < 0 && num9 - num11 + 1 < 0)
					{
						point.Z *= -1;
					}
					if (point.Z > 0 && num9 + num11 - 1 > 15)
					{
						point.Z *= -1;
					}
					bool flag = true;
					bool flag2 = false;
					bool flag3 = false;
					for (int l = 0; l < num11; l++)
					{
						int num12 = num8 + point.X * l;
						int num13 = num9 + point.Z * l;
						if (num12 < x + 1 || num12 >= num2 - 1 || num13 < y + 1 || num13 >= num3 - 1)
						{
							flag = false;
							break;
						}
						if (BlocksManager.Blocks[num.GetCellContentsFast(num12, num10, num13)].IsCollidable)
						{
							flag = false;
							break;
						}
						if (BlocksManager.Blocks[num.GetCellContentsFast(num12, num10 - 1, num13)].IsCollidable)
						{
							if (l <= MathUtils.Max(num11 / 2, 0))
							{
								flag2 = true;
							}
							if (l >= MathUtils.Min(num11 / 2 + 1, num11 - 1))
							{
								flag3 = true;
							}
						}
					}
					if (!(flag & flag2 & flag3))
					{
						continue;
					}
					Point3 point2 = ((point.X != 0) ? new Point3(0, 0, 1) : new Point3(1, 0, 0));
					TreeType? treeType = PlantsManager.GenerateRandomTreeType(random, num4 + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num10), humidity, num10, 2f);
					if (treeType.HasValue)
					{
						int treeTrunkValue = PlantsManager.GetTreeTrunkValue(treeType.Value);
						int data = Terrain.ExtractData(treeTrunkValue);
						data = WoodBlock.SetCutFace(data, (point.X != 0) ? 1 : 0);
						data |= 4;
						treeTrunkValue = Terrain.ReplaceData(treeTrunkValue, data);
						int treeLeavesValue = PlantsManager.GetTreeLeavesValue(treeType.Value);
						for (int m = 0; m < num11; m++)
						{
							int num14 = num8 + point.X * m;
							int num15 = num9 + point.Z * m;
							num.SetCellValueFast(num14, num10, num15, treeTrunkValue);
							if (m > num11 / 2)
							{
								if (random.Bool(0.5f) && !BlocksManager.Blocks[num.GetCellContentsFast(num14 + point2.X, num10, num15 + point2.Z)].IsCollidable)
								{
									num.SetCellValueFast(num14 + point2.X, num10, num15 + point2.Z, treeLeavesValue);
								}
								if (random.Bool(0.05f) && !BlocksManager.Blocks[num.GetCellContentsFast(num14 + point2.X, num10, num15 + point2.Z)].IsCollidable)
								{
									num.SetCellValueFast(num14 + point2.X, num10, num15 + point2.Z, treeTrunkValue);
								}
								if (random.Bool(0.5f) && !BlocksManager.Blocks[num.GetCellContentsFast(num14 - point2.X, num10, num15 - point2.Z)].IsCollidable)
								{
									num.SetCellValueFast(num14 - point2.X, num10, num15 - point2.Z, treeLeavesValue);
								}
								if (random.Bool(0.05f) && !BlocksManager.Blocks[num.GetCellContentsFast(num14 - point2.X, num10, num15 - point2.Z)].IsCollidable)
								{
									num.SetCellValueFast(num14 - point2.X, num10, num15 - point2.Z, treeTrunkValue);
								}
								if (random.Bool(0.5f) && !BlocksManager.Blocks[num.GetCellContentsFast(num14, num10 + 1, num15)].IsCollidable)
								{
									num.SetCellValueFast(num14, num10 + 1, num15, treeLeavesValue);
								}
							}
						}
						int num16 = 0;
						if (treeType.Value == TreeType.Birch || treeType.Value == TreeType.OldBirch)
						{
							num16 = 1;
						}
						else if (treeType.Value != TreeType.Oak && treeType.Value != TreeType.OldOak)
						{
							num16 = 2;
						}
						int num17 = random.Int(0, 2);
						for (int n = 0; n < num17; n++)
						{
							int num18 = random.Int(0, num11 - 1);
							int num19 = num8 + point.X * num18;
							int num20 = num9 + point.Z * num18;
							int num21 = num10;
							int[] array = ((point.X != 0) ? new int[4] { 0, 2, 4, 5 } : new int[4] { 1, 3, 4, 5 });
							int num22 = array[random.Int(0, 3)];
							Point3 point3 = CellFace.FaceToPoint3(num22);
							int x3 = num19 + point3.X;
							int y3 = num21 + point3.Y;
							int z = num20 + point3.Z;
							int cellContentsFast2 = num.GetCellContentsFast(x3, y3, z);
							if (cellContentsFast2 == 0 || cellContentsFast2 == treeLeavesValue)
							{
								int num23 = random.Int(0, 2);
								int data2 = (num22 & 7) | ((num23 & 3) << 3) | ((num16 & 0x1F) << 5);
								num.SetCellValueFast(x3, y3, z, Terrain.MakeBlockValue(443, 0, data2));
							}
						}
					}
					num7++;
				}
			}
		}
	}

	public void GenerateTrees(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		Terrain terrain = m_subsystemTerrain.Terrain;
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		for (int i = x; i <= x; i++)
		{
			for (int j = y; j <= y; j++)
			{
				Random random = new Random(m_seed + i + 3943 * j);
				int humidity = CalculateHumidity(i * 16, j * 16);
				int num = CalculateTemperature(i * 16, j * 16);
				float num2 = MathUtils.Saturate((SimplexNoise.OctavedNoise(i, j, 0.1f, 2, 2f, 0.5f) - 0.25f) / 0.2f + (random.Bool(0.25f) ? 0.5f : 0f));
				int num3 = (int)(5f * num2);
				int num4 = 0;
				for (int k = 0; k < 32; k++)
				{
					if (num4 >= num3)
					{
						break;
					}
					int num5 = i * 16 + random.Int(2, 13);
					int num6 = j * 16 + random.Int(2, 13);
					int num7 = terrain.CalculateTopmostCellHeight(num5, num6);
					if (num7 < 66)
					{
						continue;
					}
					int cellContentsFast = terrain.GetCellContentsFast(num5, num7, num6);
					if (cellContentsFast != 2 && cellContentsFast != 8)
					{
						continue;
					}
					num7++;
					if (!BlocksManager.Blocks[terrain.GetCellContentsFast(num5 + 1, num7, num6)].IsCollidable && !BlocksManager.Blocks[terrain.GetCellContentsFast(num5 - 1, num7, num6)].IsCollidable && !BlocksManager.Blocks[terrain.GetCellContentsFast(num5, num7, num6 + 1)].IsCollidable && !BlocksManager.Blocks[terrain.GetCellContentsFast(num5, num7, num6 - 1)].IsCollidable)
					{
						TreeType? treeType = PlantsManager.GenerateRandomTreeType(random, num + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num7), humidity, num7);
						if (treeType.HasValue)
						{
							ReadOnlyList<TerrainBrush> treeBrushes = PlantsManager.GetTreeBrushes(treeType.Value);
							TerrainBrush terrainBrush = treeBrushes[random.Int(treeBrushes.Count)];
							terrainBrush.PaintFast(chunk, num5, num7, num6);
							chunk.AddBrushPaint(num5, num7, num6, terrainBrush);
							num4++;
						}
					}
				}
				int num8 = i * 16;
				int num9 = num8 + 16;
				int num10 = j * 16;
				int num11 = num10 + 16;
				for (int l = num8; l < num9; l++)
				{
					for (int m = num10; m < num11; m++)
					{
						int num12 = terrain.CalculateTopmostCellHeight(l, m);
						int cellContentsFast2 = terrain.GetCellContentsFast(l, num12, m);
						int num13 = CalculateHumidity(l, m);
						if (num12 >= 80 && num12 <= 240 && num13 > 8 && (cellContentsFast2 == 3 || cellContentsFast2 == 62 || cellContentsFast2 == 8) && !BlocksManager.Blocks[terrain.GetCellContentsFast(l, num12 + 1, m)].IsCollidable && random.Float(0f, 1f) < 0.2f)
						{
							ReadOnlyList<TerrainBrush> treeBrushes2 = PlantsManager.GetTreeBrushes(TreeType.Spruce);
							TerrainBrush terrainBrush2 = treeBrushes2[random.Int(treeBrushes2.Count)];
							terrainBrush2.PaintFast(chunk, l, num12 + 1, m);
							chunk.AddBrushPaint(l, num12 + 1, m, terrainBrush2);
						}
					}
				}
			}
		}
	}

	public void GenerateBedrockAndAir(TerrainChunk chunk)
	{
		int num = Terrain.MakeBlockValue(344);
		int num2 = Terrain.MakeBlockValue(18);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num3 = i + chunk.Origin.X;
				int num4 = j + chunk.Origin.Y;
				int num5 = 2 + (int)(2f * SimplexNoise.OctavedNoise(num3, num4, 0.1f, 1, 1f, 1f));
				float num6 = CalculateOceanShoreDistance(num3, num4);
				float num7 = 1f - MathUtils.Saturate((0f - num6 - 32f) / 256f);
				float num8 = MathUtils.Lerp(0.5f, 1.5f, MathUtils.Saturate(SimplexNoise.OctavedNoise((float)num3 + m_islandOffset.X, (float)num4 + m_islandOffset.Y, 0.002f, 2, 2f, 0.7f)));
				float num9 = MathUtils.Saturate((MathUtils.Saturate(MathUtils.Sin(TGIslandsFrequency * num6 * num8)) - 0.6f) * 2f);
				float num10 = MathUtils.Saturate(num7 + 0.75f * num9);
				int num11 = 2 + (int)(5f * SimplexNoise.OctavedNoise(num3, num4, 0.1f, 1, 1f, 1f));
				int num12 = (int)MathUtils.Floor(num10 * (float)num11);
				for (int k = 0; k < num5 + num12; k++)
				{
					int value = ((num12 == 0) ? num2 : num);
					chunk.SetCellValueFast(i, k, j, value);
				}
				chunk.SetCellValueFast(i, 255, j, 0);
			}
		}
	}

	public void GenerateGrassAndPlants(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		Random random = new Random(m_seed + chunk.Coords.X + 4519 * chunk.Coords.Y);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int num = 254; num >= 0; num--)
				{
					int cellValueFast = chunk.GetCellValueFast(i, num, j);
					int num2 = Terrain.ExtractContents(cellValueFast);
					if (num2 != 0)
					{
						if (!(BlocksManager.Blocks[num2] is FocalorsFluidBlock))
						{
							int temperatureFast = chunk.GetTemperatureFast(i, j);
							int humidityFast = chunk.GetHumidityFast(i, j);
							int num3 = PlantsManager.GenerateRandomPlantValue(random, cellValueFast, temperatureFast, humidityFast, num + 1);
							if (num3 != 0)
							{
								chunk.SetCellValueFast(i, num + 1, j, num3);
							}
							if (num2 == 2)
							{
								int data = random.Int(0, 3) << 1;
								int value = Terrain.ReplaceData(Terrain.MakeBlockValue(8, 0, 0), data);
								chunk.SetCellValueFast(i, num, j, value);
							}
						}
						break;
					}
				}
			}
		}
	}

	public void GenerateBottomSuckers(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		Random random = new Random(m_seed + chunk.Coords.X + 2210 * chunk.Coords.Y);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (!random.Bool(0.2f))
				{
					continue;
				}
				int num = chunk.Origin.X + i;
				int num2 = chunk.Origin.Y + j;
				int temperatureFast = chunk.GetTemperatureFast(i, j);
				if (!(CalculateOceanShoreDistance(num, num2) <= 10f))
				{
					continue;
				}
				int num3 = 0;
				for (int num4 = 254; num4 >= 0; num4--)
				{
					if (Terrain.ExtractContents(chunk.GetCellValueFast(i, num4, j)) == 18)
					{
						num3++;
						int face = random.Int(0, 5);
						Point3 point = CellFace.FaceToPoint3(face);
						if (num4 + point.Y >= 0 && num4 + point.Y < 255)
						{
							int num5 = i + point.X;
							int num6 = j + point.Z;
							int y = num4 + point.Y;
							if (num5 >= 0 && num5 < 16 && num6 >= 0 && num6 < 16)
							{
								int cellValueFast = chunk.GetCellValueFast(num5, y, num6);
								if (m_subsystemBottomSuckerBlockBehavior.IsSupport(cellValueFast, CellFace.OppositeFace(face)))
								{
									int num7 = 0;
									float num8 = 0.6f;
									float num9 = 0.4f;
									if (temperatureFast < 8)
									{
										num8 = 0.9f;
										num9 = 0.1f;
									}
									if (num3 > 6)
									{
										num8 *= 0.25f;
									}
									if (num3 > 12)
									{
										num9 *= 0.5f;
									}
									if (num3 < 4)
									{
										num9 *= 0.5f;
									}
									if (num4 < 45)
									{
										num8 *= 0.1f;
										num9 *= 0.1f;
									}
									float num10 = random.Float(0f, 1f);
									num10 -= num8;
									if (num7 == 0 && num10 < 0f)
									{
										num7 = 226;
									}
									num10 -= num9;
									if (num7 == 0 && num10 < 0f)
									{
										num7 = 229;
									}
									if (num7 != 0)
									{
										int variant = random.Int(0, 3);
										int data = 0;
										data = FocalorsAttachedBlock.SetFace(data, CellFace.OppositeFace(face));
										data = FocalorsAttachedBlock.SetVariant(data, variant);
										data |= 0x400;
										int value = Terrain.MakeBlockValue(num7, 0, data);
										chunk.SetCellValueFast(i, num4, j, value);
									}
								}
							}
						}
					}
					else
					{
						num3 = 0;
					}
				}
			}
		}
	}

	public void GenerateCacti(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int coordinates = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + coordinates + 1991 * y);
		if (!random.Bool(0.375f))
		{
			return;
		}
		int contents = (random.Bool(0.5f) ? 127 : 375);
		int num = random.Int(0, MathUtils.Max(1, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.Int(3, 12);
			int num3 = random.Int(3, 12);
			int humidityFast = chunk.GetHumidityFast(num2, num3);
			int temperatureFast = chunk.GetTemperatureFast(num2, num3);
			if (humidityFast >= 6 || temperatureFast <= 8)
			{
				continue;
			}
			for (int j = 0; j < 8; j++)
			{
				int num4 = num2 + random.Int(-2, 2);
				int num5 = num3 + random.Int(-2, 2);
				for (int num6 = 251; num6 >= 0; num6--)
				{
					switch (Terrain.ExtractContents(chunk.GetCellValueFast(num4, num6, num5)))
					{
					case 7:
					{
						int k;
						for (k = random.Int(1, 4); k < 20; k++)
						{
							if (!(random.Float(0f, 1f) < 0.5f))
							{
								break;
							}
						}
						for (int l = num6 + 1; l <= num6 + k && chunk.GetCellContentsFast(num4 + 1, l, num5) == 0 && chunk.GetCellContentsFast(num4 - 1, l, num5) == 0 && chunk.GetCellContentsFast(num4, l, num5 + 1) == 0 && chunk.GetCellContentsFast(num4, l, num5 + 1) == 0 && chunk.GetCellContentsFast(num4, l, num5 - 1) == 0; l++)
						{
							chunk.SetCellValueFast(num4, l, num5, Terrain.MakeBlockValue(contents));
						}
						break;
					}
					case 0:
						continue;
					}
					break;
				}
			}
		}
	}

	public void GeneratePumpkins(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + x + 1495 * y);
		if (!random.Bool(0.2f))
		{
			return;
		}
		int num = random.Int(0, MathUtils.Max(1, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.Int(1, 14);
			int num3 = random.Int(1, 14);
			int humidityFast = chunk.GetHumidityFast(num2, num3);
			int temperatureFast = chunk.GetTemperatureFast(num2, num3);
			if (humidityFast < 10 || temperatureFast <= 6)
			{
				continue;
			}
			for (int j = 0; j < 5; j++)
			{
				int x2 = num2 + random.Int(-1, 1);
				int z = num3 + random.Int(-1, 1);
				for (int num4 = 254; num4 >= 0; num4--)
				{
					switch (Terrain.ExtractContents(chunk.GetCellValueFast(x2, num4, z)))
					{
					case 8:
						chunk.SetCellValueFast(x2, num4 + 1, z, random.Bool(0.25f) ? Terrain.MakeBlockValue(244) : Terrain.MakeBlockValue(131));
						break;
					case 0:
						continue;
					}
					break;
				}
			}
		}
	}

	public void GenerateKelp(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int coordinates = chunk.Coords.X;
		int position = chunk.Coords.Y;
		Random random = new Random(0);
		float num = 0f;
		for (int i = 0; i < 9; i++)
		{
			int num2 = i % 3 - 1;
			int num3 = i / 3 - 1;
			random.Seed(m_seed + (coordinates + num2) + 850 * (position + num3));
			if (random.Bool(0.2f))
			{
				num = MathUtils.Max(num, 0.025f);
				if (i == 4)
				{
					num = MathUtils.Max(num, 0.1f);
				}
			}
		}
		if (num == 0f)
		{
			return;
		}
		random.Seed(m_seed + coordinates + 850 * position);
		int num4 = random.Int(0, MathUtils.Max((int)(256f * num), 1));
		for (int j = 0; j < num4; j++)
		{
			int num5 = random.Int(2, 13);
			int num6 = random.Int(2, 13);
			int num7 = num5 + chunk.Origin.X;
			int num8 = num6 + chunk.Origin.Y;
			int num9 = random.Int(10, 26);
			int num10 = 6;
			bool flag = true;
			if (CalculateOceanShoreDistance(num7, num8) > 5f)
			{
				num10 = 4;
				flag = false;
			}
			if (num9 <= 0)
			{
				continue;
			}
			for (int k = 0; k < num9; k++)
			{
				int x = num5 + random.Int(-2, 2);
				int z = num6 + random.Int(-2, 2);
				int num11 = 0;
				for (int num12 = 254; num12 >= 0; num12--)
				{
					int num13 = Terrain.ExtractContents(chunk.GetCellValueFast(x, num12, z));
					Block block = BlocksManager.Blocks[num13];
					if (num13 != 0)
					{
						if (!(block is FocalorsWaterBlock))
						{
							if ((num13 == 2 || num13 == 7 || num13 == 391 || num13 == 72) && num11 >= 2)
							{
								int num14 = (flag ? random.Int(num11 - 2, num11 - 1) : random.Int(num11 - 1, num11));
								for (int l = 0; l < num14; l++)
								{
									chunk.SetCellValueFast(x, num12 + 1 + l, z, Terrain.MakeBlockValue(232, 0, 1024));
								}
							}
							break;
						}
						num11++;
						if (num11 > num10)
						{
							break;
						}
					}
				}
			}
		}
	}

	public void GenerateSeagrass(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int coordinates = chunk.Coords.X;
		int vector = chunk.Coords.Y;
		Random v = new Random(m_seed + coordinates + 378 * vector);
		for (int drawBlockEnvironmentData = 0; drawBlockEnvironmentData < 6; drawBlockEnvironmentData++)
		{
			int skeletonMatrix = v.Int(1, 14);
			int position = v.Int(1, 14);
			int i = chunk.Origin.X + skeletonMatrix;
			int slotValue = chunk.Origin.Y + position;
			bool bodyPartOffset = CalculateOceanShoreDistance(i, slotValue) < 10f;
			int num = v.Int(1, 3);
			for (int matrix = 0; matrix < num; matrix++)
			{
				int x = skeletonMatrix + v.Int(-1, 1);
				int z = position + v.Int(-1, 1);
				int num2 = 0;
				for (int num3 = 254; num3 >= 0; num3--)
				{
					int num4 = Terrain.ExtractContents(chunk.GetCellValueFast(x, num3, z));
					switch (num4)
					{
					case 18:
						num2++;
						if (num2 <= 16)
						{
							continue;
						}
						break;
					default:
						if (num2 > 1 && (num4 == 2 || num4 == 7 || num4 == 72 || num4 == 391 || num4 == 3))
						{
							int x2 = ((!v.Bool(0.1f)) ? 1 : 2);
							x2 = (bodyPartOffset ? MathUtils.Min(x2, num2 - 1) : MathUtils.Min(x2, num2));
							for (int j = 0; j < x2; j++)
							{
								chunk.SetCellValueFast(x, num3 + 1 + j, z, Terrain.MakeBlockValue(233, 0, 1024));
							}
						}
						break;
					case 0:
						continue;
					}
					break;
				}
			}
		}
	}

	public void GenerateIvy(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		Random random = new Random(m_seed + chunk.Coords.X + 2191 * chunk.Coords.Y);
		int num = random.Int(0, MathUtils.Max(12, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.Int(4, 11);
			int num3 = random.Int(4, 11);
			int humidityFast = chunk.GetHumidityFast(num2, num3);
			int temperatureFast = chunk.GetTemperatureFast(num2, num3);
			if (humidityFast < 10 || temperatureFast < 10)
			{
				continue;
			}
			int num4 = chunk.CalculateTopmostCellHeight(num2, num3);
			for (int j = 0; j < 100; j++)
			{
				int num5 = num2 + random.Int(-3, 3);
				int num6 = MathUtils.Clamp(num4 + random.Int(-12, 1), 1, 255);
				int num7 = num3 + random.Int(-3, 3);
				switch (Terrain.ExtractContents(chunk.GetCellValueFast(num5, num6, num7)))
				{
				case 2:
				case 3:
				case 8:
				case 9:
				case 12:
				case 66:
				case 67:
				{
					int num8 = random.Int(0, 3);
					for (int k = 0; k < 4; k++)
					{
						int face = (k + num8) % 4;
						Point3 point = CellFace.FaceToPoint3(face);
						if (chunk.GetCellContentsFast(num5 + point.X, num6, num7 + point.Z) != 0)
						{
							continue;
						}
						int num9 = num6 - 1;
						while (num9 >= 1 && chunk.GetCellContentsFast(num5 + point.X, num9, num7 + point.Z) == 0 && chunk.GetCellContentsFast(num5, num9, num7) != 0)
						{
							num9--;
						}
						if (chunk.GetCellContentsFast(num5 + point.X, num9, num7 + point.Z) != 0)
						{
							break;
						}
						num9++;
						int value = Terrain.MakeBlockValue(197, 0, IvyBlock.SetFace(0, CellFace.OppositeFace(face)));
						while (num9 >= 1 && chunk.GetCellContentsFast(num5 + point.X, num9, num7 + point.Z) == 0)
						{
							chunk.SetCellValueFast(num5 + point.X, num9, num7 + point.Z, value);
							if (IvyBlock.IsGrowthStopCell(num5 + point.X, num9, num7 + point.Z))
							{
								break;
							}
							num9--;
						}
						break;
					}
					break;
				}
				}
			}
		}
	}

	public void GenerateTraps(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int yaw = chunk.Coords.X;
		int num = chunk.Coords.Y;
		Random num2 = new Random(m_seed + yaw + 2113 * num);
		if (!num2.Bool(0.15f) || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 50f)
		{
			return;
		}
		int num3 = num2.Int(0, MathUtils.Max(2, 1));
		int num4 = 0;
		while (num4 < num3)
		{
			int num5 = num2.Int(2, 5);
			int num6 = num2.Int(2, 5);
			int num7 = num2.Int(1, 16 - num5 - 2);
			int num8 = num2.Int(1, 16 - num6 - 2);
			int num9 = (num2.Bool(0.5f) ? num2.Int(6, 10) : num2.Int(3, 5));
			int? num10 = null;
			bool flag = false;
			for (int i = num7 - 1; i < num7 + num5 + 1; i++)
			{
				if (flag)
				{
					break;
				}
				for (int j = num8 - 1; j < num8 + num6 + 1; j++)
				{
					int num11 = chunk.CalculateTopmostCellHeight(i, j);
					for (int num12 = MathUtils.Max(num11 - 20, 5); num11 >= num12; num11--)
					{
						switch (Terrain.ExtractContents(chunk.GetCellContentsFast(i, num11, j)))
						{
						case 3:
						case 4:
						case 61:
						case 62:
						case 67:
							num11 = -1000;
							break;
						default:
							continue;
						case 8:
							break;
						}
						break;
					}
					if (num11 < 5 || Terrain.ExtractContents(chunk.GetCellContentsFast(i, num11, j)) != 8)
					{
						num4++;
						flag = true;
						break;
					}
					if (num10.HasValue && num10.Value != num11)
					{
						num4++;
						flag = true;
						break;
					}
					if (!num10.HasValue)
					{
						num10 = num11;
					}
				}
			}
			if (flag)
			{
				continue;
			}
			if (num10.HasValue && num10.Value - num9 >= 5)
			{
				int num13 = num2.Int(0, 2);
				if (num13 == 1 && num2.Bool(0.5f))
				{
					num13 = ((!num2.Bool(0.5f)) ? 2 : 0);
				}
				for (int k = num7; k < num7 + num5; k++)
				{
					for (int l = num8; l < num8 + num6; l++)
					{
						for (int num14 = num10.Value - 1; num14 >= num10.Value - num9 + 1; num14--)
						{
							chunk.SetCellValueFast(k, num14, l, Terrain.MakeBlockValue(0));
						}
						switch (num13)
						{
						case 0:
						{
							chunk.SetCellValueFast(k, num10.Value, l, Terrain.MakeBlockValue(87));
							int data = SpikedPlankBlock.SetSpikesState(0, num2.Float(0f, 1f) < 0.33f);
							chunk.SetCellValueFast(k, num10.Value - num9 + 1, l, Terrain.MakeBlockValue(86, 0, data));
							break;
						}
						case 1:
							chunk.SetCellValueFast(k, num10.Value, l, Terrain.MakeBlockValue(87));
							if (num2.Bool(0.5f))
							{
								chunk.SetCellValueFast(k, num10.Value - num9 + 1, l, Terrain.MakeBlockValue(344));
							}
							else
							{
								chunk.SetCellValueFast(k, num10.Value - num9 + 1, l, Terrain.MakeBlockValue(92));
							}
							break;
						case 2:
						{
							chunk.SetCellValueFast(k, num10.Value, l, Terrain.MakeBlockValue(87));
							for (int m = num10.Value - num9 + 1; m <= num10.Value - num9 + 3; m++)
							{
								chunk.SetCellValueFast(k, m, l, Terrain.MakeBlockValue(18));
							}
							break;
						}
						}
					}
				}
				num4++;
			}
			else
			{
				num4++;
			}
		}
	}

	public void GenerateGraves(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random((int)MathUtils.Hash((uint)(m_seed + x + 10323 * y)));
		if (random.Float(0f, 1f) >= 0.033f || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 10f)
		{
			return;
		}
		int num = random.Int(0, MathUtils.Max(1, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.Int(6, 9);
			int num3 = random.Int(6, 9);
			int num4 = (random.Bool(0.2f) ? random.Int(6, 20) : random.Int(1, 5));
			bool flag = random.Bool(0.5f);
			for (int j = 0; j < num4; j++)
			{
				int num5 = num2 + random.Int(-4, 4);
				int num6 = num3 + random.Int(-4, 4);
				int num7 = chunk.CalculateTopmostCellHeight(num5, num6);
				if (num7 < 10 || num7 > 246)
				{
					continue;
				}
				int num8 = random.Int(0, 3);
				for (int k = 0; k < 4; k++)
				{
					int num9 = (k + num8) % 4;
					Point3 p = CellFace.FaceToPoint3(num9);
					Point3 p2 = new Point3(-p.Z, p.Y, p.X);
					int num10 = ((p.X < 0) ? (num5 - 2) : (num5 - 1));
					int num11 = ((p.X > 0) ? (num5 + 2) : (num5 + 1));
					int num12 = ((p.Z < 0) ? (num6 - 2) : (num6 - 1));
					int num13 = ((p.Z > 0) ? (num6 + 2) : (num6 + 1));
					bool flag3 = false;
					for (int l = num10; l <= num11 && !flag3; l++)
					{
						for (int m = num7 - 2; m <= num7 + 2 && !flag3; m++)
						{
							for (int n = num12; n <= num13; n++)
							{
								int num14 = Terrain.ExtractContents(chunk.GetCellValueFast(l, m, n));
								Block block = BlocksManager.Blocks[num14];
								if (m > num7)
								{
									if (!block.IsCollidable)
									{
										continue;
									}
								}
								else if (num14 == 8 || num14 == 2 || num14 == 7 || num14 == 3 || num14 == 4)
								{
									continue;
								}
								flag3 = true;
								break;
							}
						}
					}
					if (flag3)
					{
						continue;
					}
					int num15 = random.Int(0, 7);
					int data = GravestoneBlock.SetVariant(GravestoneBlock.SetRotation(0, num9 % 2), num15);
					int? num16 = null;
					int contents = 217;
					int contents2 = 136;
					if (num15 >= 4 && !flag)
					{
						int cellContentsFast = chunk.GetCellContentsFast(num5, num7, num6);
						if (cellContentsFast == 7 || cellContentsFast == 4)
						{
							num16 = Terrain.MakeBlockValue(4);
							contents = 51;
							contents2 = 52;
						}
						else if (random.Float(0f, 1f) < 0.5f)
						{
							num16 = Terrain.MakeBlockValue(3);
							contents = 217;
							contents2 = 136;
						}
						else
						{
							num16 = Terrain.MakeBlockValue(67);
							contents = 96;
							contents2 = 95;
						}
					}
					bool flag2 = num16.HasValue && random.Bool(0.33f);
					float num17 = random.Float(0f, 1f);
					float num18 = random.Float(0f, 1f);
					int num19 = random.Int(-1, 0);
					int num20 = random.Int(1, 2);
					int num21 = (flag2 ? (num7 + 2) : (num7 + 1));
					chunk.SetCellValueFast(num5, num21, num6, Terrain.MakeBlockValue(189, 0, data));
					for (int num22 = num19; num22 <= num20; num22++)
					{
						int num23 = num5 + p.X * num22;
						int num24 = num6 + p.Z * num22;
						if (num22 == 0 || num22 == 1)
						{
							int y2 = num21 - 2;
							if (num22 == 0)
							{
								chunk.SetCellValueFast(num23, y2, num24, Terrain.MakeBlockValue(190));
							}
							else if (random.Bool(0.5f))
							{
								int num25 = random.Int(0, 3);
								int num26 = random.Int(0, 3);
								int data2 = (random.Int(0, 2) << 4) | (num26 << 2) | (num25 & 3);
								int value = Terrain.MakeBlockValue(362, 0, data2);
								chunk.SetCellValueFast(num23, y2, num24, value);
							}
							else
							{
								chunk.SetCellValueFast(num23, y2, num24, Terrain.MakeBlockValue(363));
							}
							if (num16.HasValue)
							{
								chunk.SetCellValueFast(num23, num21 - 1, num24, num16.Value);
								if (num22 == 1)
								{
									int num27 = 0;
									if (num18 < 0.2f)
									{
										num27 = Terrain.MakeBlockValue(20);
									}
									else if (num18 < 0.3f)
									{
										num27 = Terrain.MakeBlockValue(24);
									}
									else if (num18 < 0.4f)
									{
										num27 = Terrain.MakeBlockValue(25);
									}
									else if (num18 < 0.5f)
									{
										num27 = Terrain.MakeBlockValue(31, 0, 4);
									}
									else if (num18 < 0.6f)
									{
										num27 = Terrain.MakeBlockValue(132, 0, CellFace.OppositeFace(num9));
									}
									if (num27 != 0)
									{
										chunk.SetCellValueFast(num23, num21, num24, num27);
									}
								}
							}
						}
						if (!flag2)
						{
							continue;
						}
						if (num17 < 0.3f)
						{
							int value2 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.Point3ToFace(p2)));
							int value3 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.OppositeFace(CellFace.Point3ToFace(p2))));
							chunk.SetCellValueFast(num23 + p2.X, num21 - 1, num24 + p2.Z, value2);
							chunk.SetCellValueFast(num23 - p2.X, num21 - 1, num24 - p2.Z, value3);
							if (num22 == -1)
							{
								int value4 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.OppositeFace(CellFace.Point3ToFace(p))));
								chunk.SetCellValueFast(num23, num21 - 1, num24, value4);
							}
							if (num22 == 2)
							{
								int value5 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.Point3ToFace(p)));
								chunk.SetCellValueFast(num23, num21 - 1, num24, value5);
							}
						}
						else if (num17 < 0.4f)
						{
							chunk.SetCellValueFast(num23 + p2.X, num21 - 1, num24 + p2.Z, Terrain.MakeBlockValue(contents2));
							chunk.SetCellValueFast(num23 - p2.X, num21 - 1, num24 - p2.Z, Terrain.MakeBlockValue(contents2));
							if (num22 == -1)
							{
								chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(contents2));
							}
							if (num22 == 2)
							{
								chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(contents2));
							}
						}
						else if (num17 < 0.6f)
						{
							if (num22 == 0 || num22 == 1)
							{
								chunk.SetCellValueFast(num23 + p2.X, num21 - 1, num24 + p2.Z, Terrain.MakeBlockValue(31, 0, CellFace.Point3ToFace(p2)));
								chunk.SetCellValueFast(num23 - p2.X, num21 - 1, num24 - p2.Z, Terrain.MakeBlockValue(31, 0, CellFace.OppositeFace(CellFace.Point3ToFace(p2))));
							}
							if (num22 == -1)
							{
								chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(31, 0, CellFace.OppositeFace(num9)));
							}
							if (num22 == 2)
							{
								chunk.SetCellValueFast(num23, num21 - 1, num24, Terrain.MakeBlockValue(31, 0, num9));
							}
						}
					}
					break;
				}
			}
		}
	}

	public void GenerateCairns(TerrainChunk chunk)
	{
		int size = chunk.Coords.X;
		int num2 = chunk.Coords.Y;
		if (new Random((int)MathUtils.Hash((uint)(m_seed + size + 71317 * num2))).Float(0f, 1f) >= 0.5f)
		{
			return;
		}
		int componentOnFire = 190;
		Point2 flag = default;
		bool flag2 = false;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (Terrain.ExtractContents(chunk.GetCellValueFast(i, componentOnFire, j)) == 0)
				{
					continue;
				}
				for (int footstepsPhase = componentOnFire + 1; footstepsPhase < 256; footstepsPhase++)
				{
					if (Terrain.ExtractContents(chunk.GetCellValueFast(i, footstepsPhase, j)) == 0)
					{
						componentOnFire = footstepsPhase;
						flag = new Point2(i, j);
						flag2 = true;
						break;
					}
				}
			}
		}
		if (!flag2 || componentOnFire < 190 || componentOnFire > 255 || flag.X < 1 || flag.X >= 15 || flag.Y < 1 || flag.Y >= 15)
		{
			return;
		}
		int num9 = Terrain.ExtractContents(chunk.GetCellValueFast(flag.X, componentOnFire - 1, flag.Y));
		if (BlocksManager.Blocks[num9].IsCollidable)
		{
			int componentMount = Terrain.ExtractContents(chunk.GetCellValueFast(flag.X, componentOnFire, flag.Y));
			if (componentMount == 0 || componentMount == 258)
			{
				int num11 = MathUtils.Clamp((int)(4f * MathUtils.LinearStep(190f, 256f, componentOnFire)), 0, 3);
				chunk.SetCellValueFast(flag.X, componentOnFire, flag.Y, Terrain.MakeBlockValue(258, 0, num11));
			}
		}
	}

	public void GenerateSnowAndIce(TerrainChunk chunk)
	{
		int vector = m_subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).GetSnowTemperatureThreshold();
		Terrain vector2 = m_subsystemTerrain.Terrain;
		for (int flag = 0; flag < 16; flag++)
		{
			for (int scale = 0; scale < 16; scale++)
			{
				int num92 = flag + chunk.Origin.X;
				int num93 = scale + chunk.Origin.Y;
				int seasonalTemperature = vector2.GetSeasonalTemperature(chunk.GetShaftValueFast(flag, scale));
				for (int x = 254; x >= 0; x--)
				{
					int lastMount = chunk.GetCellValueFast(flag, x, scale);
					int lastMount2 = Terrain.ExtractContents(lastMount);
					if (lastMount2 != 0)
					{
						if (!SubsystemWeather.IsPlaceFrozenExtended(seasonalTemperature, x, vector))
						{
							break;
						}
						if (BlocksManager.Blocks[lastMount2] is FocalorsWaterBlock)
						{
							float num94 = 1 + (int)(2f * MathUtils.Sqr(SimplexNoise.OctavedNoise(num92, num93, 0.2f, 1, 2f, 1f)));
							for (int i = 0; (float)i < num94; i++)
							{
								if (x - i > 0)
								{
									int x2 = chunk.GetCellContentsFast(flag, x - i, scale);
									if (!(BlocksManager.Blocks[x2] is FocalorsWaterBlock))
									{
										break;
									}
									chunk.SetCellValueFast(flag, x - i, scale, 62);
								}
							}
							if (SubsystemWeather.ShaftHasSnowOnIce(num92, num93))
							{
								if (x + 1 > 100)
								{
									chunk.SetCellValueFast(flag, x + 1, scale, 61);
								}
								else
								{
									int num95 = (int)MathUtils.Clamp(MathUtils.Saturate((SimplexNoise.OctavedNoise((float)num92 * 0.07f, (float)num93 * 0.07f, 0.15f, 3, 2f, 0.5f) + 1f) * 0.5f) * 5f, 0f, 4f);
									if (num95 > 0)
									{
										chunk.SetCellValueFast(flag, x + 1, scale, Terrain.MakeBlockValue(61, 0, num95));
									}
								}
							}
						}
						else if (lastMount2 != 61 && lastMount2 != 348 && SubsystemSnowBlockBehavior.CanSupportSnow(lastMount))
						{
							if (x + 1 > 100)
							{
								chunk.SetCellValueFast(flag, x + 1, scale, 61);
							}
							else
							{
								int num96 = (int)MathUtils.Clamp(MathUtils.Saturate((SimplexNoise.OctavedNoise((float)num92 * 0.07f, (float)num93 * 0.07f, 0.15f, 3, 2f, 0.5f) + 1f) * 0.5f) * 5f, 0f, 4f);
								if (num96 > 0)
								{
									chunk.SetCellValueFast(flag, x + 1, scale, Terrain.MakeBlockValue(61, 0, num96));
								}
							}
						}
						if (lastMount2 == 8)
						{
							int num97 = chunk.GetCellValueFast(flag, x, scale);
							int num98 = (Terrain.ExtractData(num97) & -2) | 1;
							chunk.SetCellValueFast(flag, x, scale, Terrain.ReplaceData(num97, num98));
						}
						break;
					}
				}
			}
		}
	}

	public void PropagateFluidsDownwards(TerrainChunk chunk)
	{
		int vector = Terrain.MakeBlockValue(391, 0, QuicksandBlock.SetKind(0, QuicksandBlock.QuicksandKind.Mud));
		int vector2 = OceanLevel;
		for (int num = 0; num < 16; num++)
		{
			for (int subsystemGameInfo2 = 0; subsystemGameInfo2 < 16; subsystemGameInfo2++)
			{
				int componentSmartChaseBehavior = 0;
				for (int num2 = 255; num2 >= 0; num2--)
				{
					int subsystemGameInfo3 = TerrainChunk.CalculateCellIndex(num, num2, subsystemGameInfo2);
					int num3 = Terrain.ExtractContents(chunk.GetCellValueFast(subsystemGameInfo3));
					if (num3 != 0 && num3 != 18 && BlocksManager.FocalorsFluidBlocks[num3] == null)
					{
						if (componentSmartChaseBehavior == 18 && (num3 == 2 || num3 == 8))
						{
							chunk.SetCellValueFast(subsystemGameInfo3, vector);
						}
						componentSmartChaseBehavior = 0;
					}
					else
					{
						if (num2 == vector2 && num3 == 0)
						{
							componentSmartChaseBehavior = 18;
						}
						else if (num3 == 18 || BlocksManager.FocalorsFluidBlocks[num3] != null)
						{
							componentSmartChaseBehavior = num3;
						}
						if (componentSmartChaseBehavior != 0 && num3 == 0)
						{
							chunk.SetCellValueFast(subsystemGameInfo3, componentSmartChaseBehavior);
						}
					}
				}
			}
		}
	}

	public void UpdateFluidIsTop(TerrainChunk chunk)
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = TerrainChunk.CalculateCellIndex(i, 255, j);
				int num2 = 0;
				int num3 = 255;
				while (num3 >= 0)
				{
					int cellValueFast = chunk.GetCellValueFast(num);
					int num4 = Terrain.ExtractContents(cellValueFast);
					if (num4 != num2 && BlocksManager.FocalorsFluidBlocks[num4] != null && BlocksManager.FocalorsFluidBlocks[num2] == null)
					{
						int data = Terrain.ExtractData(cellValueFast);
						chunk.SetCellValueFast(num, Terrain.MakeBlockValue(num4, 0, FocalorsFluidBlock.SetIsTop(data, isTop: true)));
					}
					num2 = num4;
					num3--;
					num--;
				}
			}
		}
	}

	public static void CreateBrushes()
	{
		Random random = new Random(17);
		for (int i = 0; i < 16; i++)
		{
			TerrainBrush terrainBrush = new TerrainBrush();
			int num = random.Int(4, 12);
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
				int num2 = random.Int(3, 8);
				Vector3 zero = Vector3.Zero;
				for (int k = 0; k < num2; k++)
				{
					terrainBrush.AddBox((int)MathUtils.Floor(zero.X), (int)MathUtils.Floor(zero.Y), (int)MathUtils.Floor(zero.Z), 1, 1, 1, 16);
					zero += vector;
				}
			}
			if (i == 0)
			{
				terrainBrush.AddCell(0, 0, 0, 150);
			}
			terrainBrush.Compile();
			m_coalBrushes.Add(terrainBrush);
		}
		for (int l = 0; l < 16; l++)
		{
			TerrainBrush terrainBrush2 = new TerrainBrush();
			int num3 = random.Int(3, 7);
			for (int m = 0; m < num3; m++)
			{
				Vector3 vector2 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
				int num4 = random.Int(3, 6);
				Vector3 zero2 = Vector3.Zero;
				for (int n = 0; n < num4; n++)
				{
					terrainBrush2.AddBox((int)MathUtils.Floor(zero2.X), (int)MathUtils.Floor(zero2.Y), (int)MathUtils.Floor(zero2.Z), 1, 1, 1, 39);
					zero2 += vector2;
				}
			}
			terrainBrush2.Compile();
			m_ironBrushes.Add(terrainBrush2);
		}
		for (int num5 = 0; num5 < 16; num5++)
		{
			TerrainBrush terrainBrush3 = new TerrainBrush();
			int num6 = random.Int(4, 10);
			for (int num7 = 0; num7 < num6; num7++)
			{
				Vector3 vector3 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-2f, 2f), random.Float(-1f, 1f)));
				int num8 = random.Int(3, 6);
				Vector3 zero3 = Vector3.Zero;
				for (int num9 = 0; num9 < num8; num9++)
				{
					terrainBrush3.AddBox((int)MathUtils.Floor(zero3.X), (int)MathUtils.Floor(zero3.Y), (int)MathUtils.Floor(zero3.Z), 1, 1, 1, 41);
					zero3 += vector3;
				}
			}
			terrainBrush3.Compile();
			m_copperBrushes.Add(terrainBrush3);
		}
		for (int num10 = 0; num10 < 16; num10++)
		{
			TerrainBrush terrainBrush4 = new TerrainBrush();
			int num11 = random.Int(4, 10);
			for (int num12 = 0; num12 < num11; num12++)
			{
				Vector3 vector4 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-2f, 2f), random.Float(-1f, 1f)));
				int num13 = random.Int(3, 6);
				Vector3 zero4 = Vector3.Zero;
				for (int num14 = 0; num14 < num13; num14++)
				{
					terrainBrush4.AddBox((int)MathUtils.Floor(zero4.X), (int)MathUtils.Floor(zero4.Y), (int)MathUtils.Floor(zero4.Z), 1, 1, 1, 397);
					zero4 += vector4;
				}
			}
			terrainBrush4.Compile();
			m_tinBrushes.Add(terrainBrush4);
		}
		for (int num15 = 0; num15 < 16; num15++)
		{
			TerrainBrush terrainBrush5 = new TerrainBrush();
			int num16 = random.Int(8, 16);
			for (int num17 = 0; num17 < num16; num17++)
			{
				Vector3 vector5 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.25f, 0.25f), random.Float(-1f, 1f)));
				int num18 = random.Int(4, 8);
				Vector3 zero5 = Vector3.Zero;
				for (int num19 = 0; num19 < num18; num19++)
				{
					terrainBrush5.AddBox((int)MathUtils.Floor(zero5.X), (int)MathUtils.Floor(zero5.Y), (int)MathUtils.Floor(zero5.Z), 1, 1, 1, 100);
					zero5 += vector5;
				}
			}
			terrainBrush5.Compile();
			m_saltpeterBrushes.Add(terrainBrush5);
		}
		for (int num20 = 0; num20 < 16; num20++)
		{
			TerrainBrush terrainBrush6 = new TerrainBrush();
			int num21 = random.Int(4, 10);
			for (int num22 = 0; num22 < num21; num22++)
			{
				Vector3 vector6 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
				int num23 = random.Int(3, 6);
				Vector3 zero6 = Vector3.Zero;
				for (int num24 = 0; num24 < num23; num24++)
				{
					terrainBrush6.AddBox((int)MathUtils.Floor(zero6.X), (int)MathUtils.Floor(zero6.Y), (int)MathUtils.Floor(zero6.Z), 1, 1, 1, 101);
					zero6 += vector6;
				}
			}
			terrainBrush6.Compile();
			m_sulphurBrushes.Add(terrainBrush6);
		}
		for (int num25 = 0; num25 < 16; num25++)
		{
			TerrainBrush terrainBrush7 = new TerrainBrush();
			int num26 = random.Int(2, 6);
			for (int num27 = 0; num27 < num26; num27++)
			{
				Vector3 vector7 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
				int num28 = random.Int(3, 6);
				Vector3 zero7 = Vector3.Zero;
				for (int num29 = 0; num29 < num28; num29++)
				{
					terrainBrush7.AddBox((int)MathUtils.Floor(zero7.X), (int)MathUtils.Floor(zero7.Y), (int)MathUtils.Floor(zero7.Z), 1, 1, 1, 112);
					zero7 += vector7;
				}
			}
			terrainBrush7.Compile();
			m_diamondBrushes.Add(terrainBrush7);
		}
		for (int num30 = 0; num30 < 16; num30++)
		{
			TerrainBrush terrainBrush8 = new TerrainBrush();
			int num31 = random.Int(4, 10);
			for (int num32 = 0; num32 < num31; num32++)
			{
				Vector3 vector8 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
				int num33 = random.Int(3, 6);
				Vector3 zero8 = Vector3.Zero;
				for (int num34 = 0; num34 < num33; num34++)
				{
					terrainBrush8.AddBox((int)MathUtils.Floor(zero8.X), (int)MathUtils.Floor(zero8.Y), (int)MathUtils.Floor(zero8.Z), 1, 1, 1, 148);
					zero8 += vector8;
				}
			}
			terrainBrush8.Compile();
			m_germaniumBrushes.Add(terrainBrush8);
		}
		for (int num35 = 0; num35 < 16; num35++)
		{
			TerrainBrush terrainBrush9 = new TerrainBrush();
			int num36 = random.Int(16, 32);
			for (int num37 = 0; num37 < num36; num37++)
			{
				Vector3 vector9 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
				int num38 = random.Int(6, 12);
				Vector3 zero9 = Vector3.Zero;
				for (int num39 = 0; num39 < num38; num39++)
				{
					terrainBrush9.AddBox((int)MathUtils.Floor(zero9.X), (int)MathUtils.Floor(zero9.Y), (int)MathUtils.Floor(zero9.Z), 1, 1, 1, 2);
					zero9 += vector9;
				}
			}
			terrainBrush9.Compile();
			m_dirtPocketBrushes.Add(terrainBrush9);
		}
		for (int num40 = 0; num40 < 16; num40++)
		{
			TerrainBrush terrainBrush10 = new TerrainBrush();
			int num41 = random.Int(16, 32);
			for (int num42 = 0; num42 < num41; num42++)
			{
				Vector3 vector10 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
				int num43 = random.Int(6, 12);
				Vector3 zero10 = Vector3.Zero;
				for (int num44 = 0; num44 < num43; num44++)
				{
					terrainBrush10.AddBox((int)MathUtils.Floor(zero10.X), (int)MathUtils.Floor(zero10.Y), (int)MathUtils.Floor(zero10.Z), 1, 1, 1, 6);
					zero10 += vector10;
				}
			}
			terrainBrush10.Compile();
			m_gravelPocketBrushes.Add(terrainBrush10);
		}
		for (int num45 = 0; num45 < 16; num45++)
		{
			TerrainBrush terrainBrush11 = new TerrainBrush();
			int num46 = random.Int(16, 32);
			for (int num47 = 0; num47 < num46; num47++)
			{
				Vector3 vector11 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
				int num48 = random.Int(6, 12);
				Vector3 zero11 = Vector3.Zero;
				for (int num49 = 0; num49 < num48; num49++)
				{
					terrainBrush11.AddBox((int)MathUtils.Floor(zero11.X), (int)MathUtils.Floor(zero11.Y), (int)MathUtils.Floor(zero11.Z), 1, 1, 1, 66);
					zero11 += vector11;
				}
			}
			terrainBrush11.Compile();
			m_limestonePocketBrushes.Add(terrainBrush11);
		}
		for (int num50 = 0; num50 < 16; num50++)
		{
			TerrainBrush terrainBrush12 = new TerrainBrush();
			int num51 = random.Int(16, 32);
			for (int num52 = 0; num52 < num51; num52++)
			{
				Vector3 vector12 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.1f, 0.1f), random.Float(-1f, 1f)));
				int num53 = random.Int(6, 12);
				Vector3 zero12 = Vector3.Zero;
				for (int num54 = 0; num54 < num53; num54++)
				{
					terrainBrush12.AddBox((int)MathUtils.Floor(zero12.X), (int)MathUtils.Floor(zero12.Y), (int)MathUtils.Floor(zero12.Z), 1, 1, 1, 72);
					zero12 += vector12;
				}
			}
			terrainBrush12.Compile();
			m_clayPocketBrushes.Add(terrainBrush12);
		}
		for (int num55 = 0; num55 < 16; num55++)
		{
			TerrainBrush terrainBrush13 = new TerrainBrush();
			int num56 = random.Int(16, 32);
			for (int num57 = 0; num57 < num56; num57++)
			{
				Vector3 vector13 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
				int num58 = random.Int(6, 12);
				Vector3 zero13 = Vector3.Zero;
				for (int num59 = 0; num59 < num58; num59++)
				{
					terrainBrush13.AddBox((int)MathUtils.Floor(zero13.X), (int)MathUtils.Floor(zero13.Y), (int)MathUtils.Floor(zero13.Z), 1, 1, 1, 7);
					zero13 += vector13;
				}
			}
			terrainBrush13.Compile();
			m_sandPocketBrushes.Add(terrainBrush13);
		}
		for (int num60 = 0; num60 < 16; num60++)
		{
			TerrainBrush terrainBrush14 = new TerrainBrush();
			int num61 = random.Int(16, 32);
			for (int num62 = 0; num62 < num61; num62++)
			{
				Vector3 vector14 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-0.75f, 0.75f), random.Float(-1f, 1f)));
				int num63 = random.Int(6, 12);
				Vector3 zero14 = Vector3.Zero;
				for (int num64 = 0; num64 < num63; num64++)
				{
					terrainBrush14.AddBox((int)MathUtils.Floor(zero14.X), (int)MathUtils.Floor(zero14.Y), (int)MathUtils.Floor(zero14.Z), 1, 1, 1, 67);
					zero14 += vector14;
				}
			}
			terrainBrush14.Compile();
			m_basaltPocketBrushes.Add(terrainBrush14);
		}
		for (int num65 = 0; num65 < 16; num65++)
		{
			TerrainBrush terrainBrush15 = new TerrainBrush();
			int num66 = random.Int(16, 32);
			for (int num67 = 0; num67 < num66; num67++)
			{
				Vector3 vector15 = 0.5f * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f)));
				int num68 = random.Int(5, 10);
				Vector3 zero15 = Vector3.Zero;
				for (int num69 = 0; num69 < num68; num69++)
				{
					terrainBrush15.AddBox((int)MathUtils.Floor(zero15.X), (int)MathUtils.Floor(zero15.Y), (int)MathUtils.Floor(zero15.Z), 1, 1, 1, 3);
					zero15 += vector15;
				}
			}
			terrainBrush15.Compile();
			m_granitePocketBrushes.Add(terrainBrush15);
		}
		for (int num70 = 0; num70 < 7; num70++)
		{
			m_caveBrushesByType.Add(new List<TerrainBrush>());
			for (int num71 = 0; num71 < 3; num71++)
			{
				TerrainBrush terrainBrush16 = new TerrainBrush();
				int num72 = 6 + 4 * num70;
				int max = 3 + num70 / 3;
				int max2 = 9 + num70;
				for (int num73 = 0; num73 < num72; num73++)
				{
					int num74 = random.Int(2, max);
					int num75 = random.Int(8, max2) - 2 * num74;
					Vector3 vector16 = 0.5f * new Vector3(random.Float(-1f, 1f), random.Float(0f, 1f), random.Float(-1f, 1f));
					Vector3 zero16 = Vector3.Zero;
					for (int num76 = 0; num76 < num75; num76++)
					{
						terrainBrush16.AddBox((int)MathUtils.Floor(zero16.X) - num74 / 2, (int)MathUtils.Floor(zero16.Y) - num74 / 2, (int)MathUtils.Floor(zero16.Z) - num74 / 2, num74, num74, num74, 0);
						zero16 += vector16;
					}
				}
				terrainBrush16.Compile();
				m_caveBrushesByType[num70].Add(terrainBrush16);
			}
		}
	}

	public void GenerateDesertBoulders(TerrainChunk chunk)
	{
		Terrain num = m_subsystemTerrain.Terrain;
		Point2 f = chunk.Origin;
		Random num2 = new Random((int)MathUtils.Hash((uint)(m_seed + chunk.Coords.X + 1943 * chunk.Coords.Y)));
		bool num3 = num2.Bool(0.01f);
		if (!num2.Bool(0.04f) && !num3)
		{
			return;
		}
		HashSet<Point2> num4 = new HashSet<Point2>();
		int matrix = f.X + num2.Int(0, 15);
		int num5 = f.Y + num2.Int(0, 15);
		int componentMount = num2.Int(4, 5) + (num3 ? 2 : 0);
		if (componentMount > 7)
		{
			componentMount = 7;
		}
		Point2 num6 = new Point2(matrix, num5);
		if (!num4.Contains(num6))
		{
			num4.Add(num6);
			int num7 = num.CalculateTopmostCellHeight(matrix, num5);
			if (num.GetCellContentsFast(matrix, num7, num5) == 7 && num.GetCellContentsFast(matrix, num7 - 1, num5) != 0)
			{
				int size = (num2.Bool(0.5f) ? 4 : 7);
				for (int name = 0; name < componentMount; name++)
				{
					if (num.GetCellContentsFast(matrix, num7 + name, num5) == 0)
					{
						num.SetCellValueFast(matrix, num7 + name, num5, Terrain.MakeBlockValue(size));
					}
				}
				if (num2.Bool(0.3f))
				{
					num.SetCellValueFast(matrix, num7 + componentMount, num5, Terrain.MakeBlockValue(52));
				}
			}
		}
		for (int i = 0; i < 5; i++)
		{
			int num8 = matrix + num2.Int(-2, 2);
			int num9 = num5 + num2.Int(-2, 2);
			Point2 item = new Point2(num8, num9);
			if (num4.Contains(item))
			{
				continue;
			}
			num4.Add(item);
			int num10 = num2.Int(2, 3);
			if (num10 > 7)
			{
				num10 = 7;
			}
			int num11 = num.CalculateTopmostCellHeight(num8, num9);
			if (num.GetCellContentsFast(num8, num11, num9) != 7 || num.GetCellContentsFast(num8, num11 - 1, num9) == 0)
			{
				continue;
			}
			int contents = (num2.Bool(0.5f) ? 4 : 7);
			for (int j = 0; j < num10; j++)
			{
				if (num.GetCellContentsFast(num8, num11 + j, num9) == 0)
				{
					num.SetCellValueFast(num8, num11 + j, num9, Terrain.MakeBlockValue(contents));
				}
			}
			if (num2.Bool(0.2f))
			{
				num.SetCellValueFast(num8, num11 + num10, num9, Terrain.MakeBlockValue(52));
			}
		}
		for (int k = 0; k < 10; k++)
		{
			int x = matrix + num2.Int(-3, 3);
			int num12 = num5 + num2.Int(-3, 3);
			Point2 item2 = new Point2(x, num12);
			if (num4.Contains(item2))
			{
				continue;
			}
			num4.Add(item2);
			int num13 = num2.Int(1, 2);
			int num14 = num.CalculateTopmostCellHeight(x, num12);
			if (num.GetCellContentsFast(x, num14, num12) != 7 || num.GetCellContentsFast(x, num14 - 1, num12) == 0)
			{
				continue;
			}
			int contents2 = (num2.Bool(0.5f) ? 4 : 7);
			for (int l = 0; l < num13; l++)
			{
				if (num.GetCellContentsFast(x, num14 + l, num12) == 0)
				{
					num.SetCellValueFast(x, num14 + l, num12, Terrain.MakeBlockValue(contents2));
				}
			}
		}
		if (!(num2.Float(0f, 1f) < 0.4f))
		{
			return;
		}
		int num15 = ((!num3) ? 1 : 2);
		for (int m = 0; m < num15; m++)
		{
			int x2 = matrix + num2.Int(-1, 1);
			int z = num5 + num2.Int(-1, 1);
			int num16 = num.CalculateTopmostCellHeight(x2, z);
			if (num2.Bool(0.5f))
			{
				num16--;
			}
			if (num.GetCellContentsFast(x2, num16, z) == 0)
			{
				num.SetCellValueFast(x2, num16, z, Terrain.MakeBlockValue(100));
			}
		}
	}

	public void GenerateBushes(TerrainChunk chunk)
	{
		Terrain num = m_subsystemTerrain.Terrain;
		int rotation = chunk.Origin.X;
		int componentMount = rotation + 16;
		int y = chunk.Origin.Y;
		int num2 = y + 16;
		int x = chunk.Coords.X;
		int y2 = chunk.Coords.Y;
		for (int i = x; i <= x; i++)
		{
			for (int j = y2; j <= y2; j++)
			{
				Random random = new Random(m_seed + i + 6361 * j);
				int num3 = CalculateHumidity(i * 16, j * 16);
				int num4 = CalculateTemperature(i * 16, j * 16);
				if (num3 == 0)
				{
					continue;
				}
				float num5 = 0.2f;
				float num6 = 1f;
				if (num3 > 8 && num4 > 8)
				{
					num6 = 2f;
				}
				float x2 = num5 * num6 + (float)num4 / 100f * 0.1f;
				x2 = MathUtils.Clamp(x2, num5, 0.4f);
				if (!random.Bool(x2))
				{
					continue;
				}
				int num7 = 1;
				if (random.Bool(0.05f))
				{
					num7 = random.Int(2, 3);
				}
				for (int k = 0; k < num7; k++)
				{
					int num8 = random.Int(0, 15);
					int num9 = random.Int(0, 15);
					int num10 = i * 16 + num8;
					int num11 = j * 16 + num9;
					int num12 = num.CalculateTopmostCellHeight(num10, num11);
					switch (num.GetCellContentsFast(num10, num12, num11))
					{
					case 2:
					case 6:
					case 8:
					{
						num12++;
						int value = Terrain.MakeBlockValue(9, 0, 4);
						int value2 = 13;
						num.SetCellValueFast(num10, num12, num11, value);
						if (random.Bool(0.5f))
						{
							for (int l = -2; l <= 2; l++)
							{
								for (int m = -2; m <= 2; m++)
								{
									for (int n = -2; n <= 2; n++)
									{
										if (l * l + m * m + n * n > 4 || !random.Bool(0.8f))
										{
											continue;
										}
										int num13 = num10 + l;
										int num14 = num12 + m;
										int num15 = num11 + n;
										if (num13 >= rotation && num13 < componentMount && num15 >= y && num15 < num2 && num14 >= 0 && num14 < 128)
										{
											int cellContentsFast = num.GetCellContentsFast(num13, num14, num15);
											if (!BlocksManager.Blocks[cellContentsFast].IsCollidable)
											{
												num.SetCellValueFast(num13, num14, num15, value2);
											}
										}
									}
								}
							}
						}
						else
						{
							for (int num16 = -1; num16 <= 1; num16++)
							{
								for (int num17 = -1; num17 <= 1; num17++)
								{
									for (int num18 = -1; num18 <= 1; num18++)
									{
										if ((num16 == 0 && num17 == 0 && num18 == 0) || Math.Abs(num16) + Math.Abs(num17) + Math.Abs(num18) != 1)
										{
											continue;
										}
										int num19 = num10 + num16;
										int num20 = num12 + num17;
										int num21 = num11 + num18;
										if (num19 >= rotation && num19 < componentMount && num21 >= y && num21 < num2 && num20 >= 0 && num20 < 128)
										{
											int cellContentsFast2 = num.GetCellContentsFast(num19, num20, num21);
											if (!BlocksManager.Blocks[cellContentsFast2].IsCollidable)
											{
												num.SetCellValueFast(num19, num20, num21, value2);
											}
										}
									}
								}
							}
							if (random.Bool(0.2f))
							{
								List<Point3> list = new List<Point3>();
								for (int num22 = -1; num22 <= 1; num22++)
								{
									for (int num23 = -1; num23 <= 1; num23++)
									{
										for (int num24 = -1; num24 <= 1; num24++)
										{
											if ((num22 == 0 && num23 == 0 && num24 == 0) || Math.Abs(num22) + Math.Abs(num23) + Math.Abs(num24) != 1)
											{
												continue;
											}
											int num25 = num10 + num22;
											int num26 = num12 + num23;
											int num27 = num11 + num24;
											if (num25 >= rotation && num25 < componentMount && num27 >= y && num27 < num2 && num26 >= 0 && num26 < 128)
											{
												int cellContentsFast3 = num.GetCellContentsFast(num25, num26, num27);
												if (!BlocksManager.Blocks[cellContentsFast3].IsCollidable)
												{
													list.Add(new Point3(num25, num26, num27));
												}
											}
										}
									}
								}
								if (list.Count > 0)
								{
									Point3 point = list[random.Int(list.Count)];
									num.SetCellValueFast(point.X, point.Y, point.Z, value2);
								}
							}
						}
						if (num7 <= 1)
						{
							break;
						}
						for (int num28 = 1; num28 < num7; num28++)
						{
							int num29 = num8 + random.Int(-5, 5);
							int num30 = num9 + random.Int(-5, 5);
							int x3 = i * 16 + num29;
							int z = j * 16 + num30;
							int num31 = num.CalculateTopmostCellHeight(x3, z);
							switch (num.GetCellContentsFast(x3, num31, z))
							{
							case 2:
							case 6:
							case 8:
								num31++;
								num.SetCellValueFast(x3, num31, z, value);
								break;
							}
						}
						break;
					}
					}
				}
			}
		}
	}

	public void GenerateGraniteBoulders(TerrainChunk chunk)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		float num = 0.05f;
		float num2 = 0.03f;
		float num3 = 0.2f;
		float num4 = 0.0025f;
		for (int i = x; i <= x; i++)
		{
			for (int j = y; j <= y; j++)
			{
				Random random = new Random(m_seed + i + 7919 * j);
				float probability = 0.15f;
				if (!random.Bool(probability))
				{
					continue;
				}
				int num5 = random.Int(0, 2);
				int num6 = i * 16 + random.Int(0, 15);
				int num7 = j * 16 + random.Int(0, 15);
				int num8 = terrain.CalculateTopmostCellHeight(num6, num7) - 1;
				if (num8 < 65)
				{
					continue;
				}
				int cellContentsFast = terrain.GetCellContentsFast(num6, num8, num7);
				if (cellContentsFast != 2 && cellContentsFast != 8)
				{
					continue;
				}
				int value = 3;
				int value2 = 136;
				int value3 = 41;
				int value4 = 39;
				int value5 = 16;
				int num9 = 378;
				int num10 = 376;
				int num11 = num8 + 1;
				switch (num5)
				{
				case 0:
				{
					int num20 = random.Int(1, 2);
					for (int n = 0; n < num20; n++)
					{
						int x2 = num6;
						int num21 = num11;
						int z = num7;
						if (terrain.GetCellContentsFast(x2, num21 - 1, z) == 0)
						{
							terrain.SetCellValueFast(x2, num21 - 1, z, value);
						}
						terrain.SetCellValueFast(x2, num21, z, value2);
					}
					break;
				}
				case 1:
				{
					bool flag = random.Bool(0.5f);
					int num22 = random.Int(3, 4);
					int num23 = 1;
					num11 = num8 + 1 - num23;
					for (int num24 = -num22 / 2; num24 <= num22 / 2; num24++)
					{
						int x3 = num6 + (flag ? num24 : 0);
						int z2 = num7 + ((!flag) ? num24 : 0);
						int num25 = num11;
						terrain.SetCellValueFast(x3, num25, z2, value);
						if (random.Bool(0.5f))
						{
							terrain.SetCellValueFast(x3, num25 + 1, z2, value2);
						}
						else
						{
							terrain.SetCellValueFast(x3, num25 + 1, z2, value);
						}
						if (random.Float(0f, 1f) <= num)
						{
							terrain.SetCellValueFast(x3, num25 + random.Int(0, 1), z2, value3);
						}
						else if (random.Float(0f, 1f) <= num2)
						{
							terrain.SetCellValueFast(x3, num25 + random.Int(0, 1), z2, value4);
						}
						else if (random.Float(0f, 1f) <= num3)
						{
							terrain.SetCellValueFast(x3, num25 + random.Int(0, 1), z2, value5);
						}
						else if (random.Float(0f, 1f) <= num4)
						{
							int value7 = (random.Bool(0.8f) ? num9 : num10);
							terrain.SetCellValueFast(x3, num25 + random.Int(0, 1), z2, value7);
						}
					}
					break;
				}
				case 2:
				{
					int num12 = random.Int(0, 3);
					int num13 = random.Int(4, 6);
					int num14 = random.Int(2, 3);
					int num15 = (random.Bool(0.5f) ? 1 : 2);
					num11 = num8 + 1 - num15;
					for (int k = -num13 / 2; k <= num13 / 2; k++)
					{
						for (int l = -num14 / 2; l <= num14 / 2; l++)
						{
							int num16 = num6;
							int num17 = num7;
							switch (num12)
							{
							case 0:
								num16 += k;
								num17 += l;
								break;
							case 1:
								num16 += l;
								num17 += k;
								break;
							case 2:
								num16 += k;
								num17 += k;
								break;
							case 3:
								num16 += k;
								num17 -= k;
								break;
							}
							int num18 = num11;
							if (!((double)(k * k) / (double)(num13 * num13) + (double)(l * l) / (double)(num14 * num14) <= 0.25))
							{
								continue;
							}
							terrain.SetCellValueFast(num16, num18, num17, value);
							int num19 = random.Int(1, 2);
							for (int m = 1; m <= num19; m++)
							{
								if (m == num19 && random.Bool(0.5f))
								{
									terrain.SetCellValueFast(num16, num18 + m, num17, value2);
								}
								else
								{
									terrain.SetCellValueFast(num16, num18 + m, num17, value);
								}
							}
							if (random.Float(0f, 1f) <= num)
							{
								terrain.SetCellValueFast(num16, num18 + random.Int(0, 1), num17, value3);
							}
							else if (random.Float(0f, 1f) <= num2)
							{
								terrain.SetCellValueFast(num16, num18 + random.Int(0, 1), num17, value4);
							}
							else if (random.Float(0f, 1f) <= num3)
							{
								terrain.SetCellValueFast(num16, num18 + random.Int(0, 1), num17, value5);
							}
							else if (random.Float(0f, 1f) <= num4)
							{
								int value6 = (random.Bool(0.8f) ? num9 : num10);
								terrain.SetCellValueFast(num16, num18 + random.Int(0, 1), num17, value6);
							}
						}
					}
					break;
				}
				}
			}
		}
	}

	public void GenerateMenhirs(TerrainChunk chunk)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		for (int i = x; i <= x; i++)
		{
			for (int j = y; j <= y; j++)
			{
				Random random = new Random(m_seed + i * 374761393 + j * 668265263);
				if (!random.Bool(0.008f) || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 100f))
				{
					continue;
				}
				int num = i * 16 + random.Int(0, 15);
				int num2 = j * 16 + random.Int(0, 15);
				int x2 = num - chunk.Origin.X;
				int z = num2 - chunk.Origin.Y;
				int humidityFast = chunk.GetHumidityFast(x2, z);
				int temperatureFast = chunk.GetTemperatureFast(x2, z);
				int num3;
				int num4;
				if (humidityFast < 6)
				{
					num3 = ((temperatureFast > 8) ? 1 : 0);
					num4 = ((num3 != 0) ? 7 : 5);
				}
				else
				{
					num3 = 0;
					num4 = 5;
				}
				int num5 = num4;
				int num6 = ((num3 != 0) ? 4 : 3);
				int num7 = ((num3 != 0) ? 377 : 53);
				int num8 = ((num3 != 0) ? 52 : 136);
				int num9 = ((num3 != 0) ? 100 : 16);
				int[] array = new int[8] { 19, 20, 24, 25, 28, 99, 174, 204 };
				int num10 = terrain.CalculateTopmostCellHeight(num, num2);
				if (num10 <= 70 && num10 >= 50)
				{
					continue;
				}
				while (num10 > 0)
				{
					int cellContentsFast = terrain.GetCellContentsFast(num, num10, num2);
					if (cellContentsFast != 0 && Array.IndexOf(array, cellContentsFast) < 0)
					{
						if (cellContentsFast == 8 || cellContentsFast == 7 || cellContentsFast == 3)
						{
							break;
						}
						return;
					}
					terrain.SetCellValueFast(num, num10, num2, 0);
					num10--;
				}
				int num11 = random.Int(0, 2);
				num10 = MathUtils.Max(num10 - num11, 4);
				if (num10 < 4)
				{
					continue;
				}
				int num12 = random.Int(5, 6);
				for (int k = 0; k < num12; k++)
				{
					int num13 = (random.Bool(0.5f) ? num5 : num6);
					if (k > 0 && k < num12 - 1 && random.Bool(0.3f))
					{
						num13 = num9;
					}
					int num14 = num10 + 1 + k;
					terrain.SetCellValueFast(num, num14, num2, num13);
					if (k == num12 - 1 && random.Bool(0.4f))
					{
						int value = ((num13 == num5) ? num7 : num8);
						terrain.SetCellValueFast(num, num14 + 1, num2, value);
					}
				}
				int num15 = random.Int(2, 4);
				for (int l = -num15; l <= num15; l++)
				{
					for (int m = -num15; m <= num15; m++)
					{
						if ((l == 0 && m == 0) || !random.Bool(0.7f))
						{
							continue;
						}
						float x3 = MathUtils.Sqrt(l * l + m * m);
						int num16 = num12 - (int)MathUtils.Clamp(x3, 1f, 3f) - random.Int(0, 2);
						if (num16 >= 1)
						{
							int num17 = num10 + 1;
							for (int n = 0; n < num16; n++)
							{
								int value2 = (random.Bool(0.5f) ? num5 : num6);
								terrain.SetCellValueFast(num + l, num17 + n, num2 + m, value2);
							}
							if (random.Bool(0.4f))
							{
								int value3 = (random.Bool(0.5f) ? num7 : num8);
								terrain.SetCellValueFast(num + l, num17 + num16, num2 + m, value3);
							}
						}
					}
				}
				int num18 = num - num15 - 1;
				int num19 = num + num15 + 1;
				int num20 = num2 - num15 - 1;
				int num21 = num2 + num15 + 1;
				for (int num22 = num18; num22 <= num19; num22++)
				{
					for (int num23 = num20; num23 <= num21; num23++)
					{
						for (int num24 = 250; num24 >= 1; num24--)
						{
							int cellContentsFast2 = terrain.GetCellContentsFast(num22, num24, num23);
							if (cellContentsFast2 != 0)
							{
								int cellContentsFast3 = terrain.GetCellContentsFast(num22, num24 - 1, num23);
								int num25 = num24;
								while (num25 > 0 && cellContentsFast3 == 0)
								{
									terrain.SetCellValueFast(num22, num25 - 1, num23, cellContentsFast2);
									terrain.SetCellValueFast(num22, num25, num23, 0);
									num25--;
									if (num25 > 0)
									{
										cellContentsFast3 = terrain.GetCellContentsFast(num22, num25 - 1, num23);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	public void GeneratePortalFrames(TerrainChunk chunk)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random randomPortal = new Random(m_seed + 99113 * x + 54321 * y);
		if (!randomPortal.Bool(0.005f) || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 300f)
		{
			return;
		}
		int num = x * 16 + randomPortal.Int(0, 15);
		int num2 = y * 16 + randomPortal.Int(0, 15);
		int x2 = num - chunk.Origin.X;
		int z = num2 - chunk.Origin.Y;
		int humidityFast = chunk.GetHumidityFast(x2, z);
		int temperatureFast = chunk.GetTemperatureFast(x2, z);
		bool desert = humidityFast < 6 && temperatureFast > 8;
		int cbl = 5;
		int grn = 3;
		int num3 = 53;
		int num4 = 136;
		Func<int, int> fullBlock = (int b) =>
		{
			if (!desert)
			{
				return b;
			}
			return (b != 5) ? 4 : 7;
		};
		Func<int, int> func = (int s) => (!desert) ? s : 52;
		int num5 = terrain.CalculateTopmostCellHeight(num, num2);
		if (num5 > 70)
		{
			return;
		}
		for (; num5 > 0; terrain.SetCellValueFast(num, num5, num2, 0), num5--)
		{
			switch (terrain.GetCellContentsFast(num, num5, num2))
			{
			default:
				return;
			case 0:
			case 18:
			case 19:
			case 20:
			case 24:
			case 25:
			case 28:
			case 99:
			case 174:
			case 204:
				continue;
			case 7:
			case 8:
				break;
			}
			break;
		}
		if (num5 < 4)
		{
			return;
		}
		num5 -= randomPortal.Int(0, 1);
		if (num5 < 4)
		{
			num5 = 4;
		}
		bool flag = randomPortal.Bool(0.5f);
		int[] array = new int[3] { 3, 4, 6 };
		int num6 = array[randomPortal.Int(0, array.Length - 1)];
		int num7 = num;
		int num8 = num2;
		if (flag)
		{
			num7 = num + num6;
		}
		else
		{
			num8 = num2 + num6;
		}
		int[] array3;
		if (!flag)
		{
			int[] array2 = new int[2] { num2, 0 };
			array3 = array2;
			array2[1] = num8;
		}
		else
		{
			int[] array4 = new int[2] { num, 0 };
			array3 = array4;
			array4[1] = num7;
		}
		int[] array5 = array3;
		foreach (int num10 in array5)
		{
			int num11 = (flag ? terrain.CalculateTopmostCellHeight(num10, num2) : terrain.CalculateTopmostCellHeight(num, num10));
			if (terrain.GetCellContentsFast(num10, num11 - 1, flag ? num2 : num) == 0)
			{
				return;
			}
		}
		int num12 = randomPortal.Int(3, 4);
		Func<int> func2 = () =>
		{
			int arg2 = (randomPortal.Bool(0.5f) ? cbl : grn);
			return fullBlock(arg2);
		};
		for (int num13 = 0; num13 < num12; num13++)
		{
			int value = func2();
			terrain.SetCellValueFast(num, num5 + 1 + num13, num2, value);
			int value2 = func2();
			terrain.SetCellValueFast(num7, num5 + 1 + num13, num8, value2);
		}
		int[] array6 = new int[9] { 18, 19, 20, 24, 25, 28, 99, 174, 204 };
		for (int num14 = 0; num14 <= (flag ? num6 : 0); num14++)
		{
			for (int num15 = 0; num15 <= ((!flag) ? num6 : 0); num15++)
			{
				int x3 = num + num14;
				int z2 = num2 + num15;
				int num16 = num5;
				while (Array.IndexOf(array6, terrain.GetCellContentsFast(x3, num16 - 1, z2)) >= 0)
				{
					terrain.SetCellValueFast(x3, num16, z2, terrain.GetCellContentsFast(x3, num16 - 1, z2));
					num16--;
				}
			}
		}
		int y2 = num5 + num12 + 1;
		if (flag)
		{
			for (int num17 = 1; num17 < num6; num17++)
			{
				int value3 = (desert ? 52 : 53);
				terrain.SetCellValueFast(num + num17, y2, num2, value3);
			}
		}
		else
		{
			for (int num18 = 1; num18 < num6; num18++)
			{
				int value4 = (desert ? 52 : 136);
				terrain.SetCellValueFast(num, y2, num2 + num18, value4);
			}
		}
		int num19 = randomPortal.Int(2, 3);
		for (int num20 = -num19; num20 <= num19; num20++)
		{
			for (int num21 = -num19; num21 <= num19; num21++)
			{
				if (!randomPortal.Bool(0.2f))
				{
					continue;
				}
				int x4 = num + num20;
				int z3 = num2 + num21;
				int cellContentsFast = terrain.GetCellContentsFast(x4, num5, z3);
				if (cellContentsFast == 2 || cellContentsFast == 8)
				{
					if (randomPortal.Bool(0.3f))
					{
						terrain.SetCellValueFast(x4, num5 + 1, z3, func2());
						continue;
					}
					int arg = (randomPortal.Bool(0.5f) ? num3 : num4);
					arg = func(arg);
					terrain.SetCellValueFast(x4, num5 + 1, z3, arg);
				}
			}
		}
	}

	public void GenerateRuinedWalls(TerrainChunk chunk)
	{
		Terrain position = m_subsystemTerrain.Terrain;
		int cellValue = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + cellValue + 452113 * y);
		if (!random.Bool(0.007f) || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 300f)
		{
			return;
		}
		int num = random.Int(1, 2);
		for (int i = 0; i < num; i++)
		{
			int num2 = cellValue * 16 + random.Int(0, 15);
			int num3 = y * 16 + random.Int(0, 15);
			int num4 = position.CalculateTopmostCellHeight(num2, num3);
			if (position.GetCellContentsFast(num2, num4 - 1, num3) != 8)
			{
				continue;
			}
			int num5 = random.Int(2, 4);
			for (int j = 0; j < num5; j++)
			{
				int num6 = num2 + random.Int(-3, 3);
				int num7 = num3 + random.Int(-3, 3);
				int num8 = position.CalculateTopmostCellHeight(num6, num7);
				int num9 = random.Int(2, 5);
				int num10 = random.Int(1, 2);
				int cellContentsFast = position.GetCellContentsFast(num6, num8 - 1, num7);
				if (cellContentsFast != 2 && cellContentsFast != 8)
				{
					continue;
				}
				for (int k = 0; k < num9; k++)
				{
					for (int l = 0; l < num10; l++)
					{
						int value = (random.Bool(0.5f) ? 26 : 359);
						position.SetCellValueFast(num6 + k, num8 + l, num7, value);
					}
					if (random.Bool(0.3f))
					{
						int value2 = (random.Bool(0.5f) ? 54 : 360);
						position.SetCellValueFast(num6 + k, num8 + num10, num7, value2);
					}
				}
				int num11 = random.Int(6, 12);
				for (int m = 0; m < num11; m++)
				{
					int x = num6 + random.Int(-3, 3);
					int z = num7 + random.Int(-3, 3);
					int num12 = position.CalculateTopmostCellHeight(x, z);
					int cellContentsFast2 = position.GetCellContentsFast(x, num12 - 1, z);
					if (cellContentsFast2 == 2 || cellContentsFast2 == 8)
					{
						int value3 = 12;
						position.SetCellValueFast(x, num12, z, value3);
					}
				}
			}
		}
	}

	public void GenerateForgottenHouse(TerrainChunk chunk)
	{
		Terrain f = m_subsystemTerrain.Terrain;
		Random num = new Random(m_seed + chunk.Coords.X * 974561393 + chunk.Coords.Y * 874665263);
		if (!num.Bool(0.008f) || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 500f))
		{
			return;
		}
		int num2 = chunk.Origin.X + num.Int(0, 15);
		int num3 = chunk.Origin.Y + num.Int(0, 15);
		int num4 = f.CalculateTopmostCellHeight(num2, num3);
		if (num4 > 70 || f.GetCellContentsFast(num2, num4, num3) != 8)
		{
			return;
		}
		num4 -= num.Int(0, 2);
		int num5 = num.Int(-2, 3);
		int num6 = MathUtils.Clamp(6 + num5, 4, 10);
		int y = MathUtils.Clamp(num.Int(3, 4) + num5, 3, 7);
		int num7 = MathUtils.Clamp(4 + num5, 3, 8);
		for (int i = -num6 / 2; i <= num6 / 2; i++)
		{
			for (int j = -num6 / 2; j <= num6 / 2; j++)
			{
				for (int k = 0; k <= y; k++)
				{
					if (i == -num6 / 2 || i == num6 / 2 || j == -num6 / 2 || j == num6 / 2)
					{
						if ((i == -num6 / 2 || i == num6 / 2) && (j == -num6 / 2 || j == num6 / 2))
						{
							f.SetCellValueFast(num2 + i, num4 + k, num3 + j, 11);
						}
						else if (k < y - num.Int(0, 2))
						{
							f.SetCellValueFast(num2 + i, num4 + k, num3 + j, 21);
						}
					}
				}
				for (int num8 = y - 1; num8 >= 0; num8--)
				{
					if (num.Bool(0.15f))
					{
						f.SetCellValueFast(num2 + i, num4 + num8, num3 + j, 0);
					}
				}
			}
		}
		for (int l = -num6 / 2; l <= num6 / 2; l++)
		{
			for (int m = -num6 / 2; m <= num6 / 2; m++)
			{
				bool num9 = (l == -num6 / 2 || l == num6 / 2) && (m == -num6 / 2 || m == num6 / 2);
				bool flag = l == -num6 / 2 || l == num6 / 2 || m == -num6 / 2 || m == num6 / 2;
				if (num9 && num.Bool(0.8f))
				{
					f.SetCellValueFast(num2 + l, num4 + num7, num3 + m, 0);
				}
				else if (flag && num.Bool(0.5f))
				{
					f.SetCellValueFast(num2 + l, num4 + num7, num3 + m, 0);
				}
				else if (num.Bool(0.1f))
				{
					f.SetCellValueFast(num2 + l, num4 + num7, num3 + m, 0);
				}
				else
				{
					f.SetCellValueFast(num2 + l, num4 + num7, num3 + m, 55);
				}
			}
		}
		if (num.Bool(0.8f))
		{
			int x = num2 + num.Int(-num6 / 2 + 1, num6 / 2 - 1);
			int z = num3 + num.Int(-num6 / 2 + 1, num6 / 2 - 1);
			f.SetCellValueFast(x, num4 + 1, z, Terrain.MakeBlockValue(45));
		}
		if (num.Bool(0.6f))
		{
			int x2 = num2 + num.Int(-num6 / 2 + 1, num6 / 2 - 1);
			int z2 = num3 + num.Int(-num6 / 2 + 1, num6 / 2 - 1);
			f.SetCellValueFast(x2, num4 + 1, z2, Terrain.MakeBlockValue(303));
		}
		if (num.Bool(0.4f))
		{
			int x3 = num2 + num.Int(-num6 / 2 + 1, num6 / 2 - 1);
			int z3 = num3 + num.Int(-num6 / 2 + 1, num6 / 2 - 1);
			f.SetCellValueFast(x3, num4 + 1, z3, Terrain.MakeBlockValue(83));
		}
		int x4 = num2 + num.Int(-1, 2);
		int z4 = num3 + num.Int(-1, 2);
		f.SetCellValueFast(x4, num4 - 1, z4, Terrain.MakeBlockValue(440));
		int num10 = num6 + 2;
		for (int n = -num10 / 2; n <= num10 / 2; n++)
		{
			for (int num11 = -num10 / 2; num11 <= num10 / 2; num11++)
			{
				for (int num12 = 0; num12 <= y + 1; num12++)
				{
					if ((n == -num10 / 2 || n == num10 / 2 || num11 == -num10 / 2 || num11 == num10 / 2) && num12 < y + 1 - num.Int(0, 2))
					{
						f.SetCellValueFast(num2 + n, num4 + num12, num3 + num11, 359);
					}
				}
				for (int num13 = y + 1; num13 >= 0; num13--)
				{
					if (num.Bool(0.25f))
					{
						f.SetCellValueFast(num2 + n, num4 + num13, num3 + num11, 0);
					}
				}
			}
		}
		int num14 = num.Int(5, 10);
		for (int num15 = 0; num15 < num14; num15++)
		{
			int x5 = num2 + num.Int(-num10, num10);
			int z5 = num3 + num.Int(-num10, num10);
			int y2 = f.CalculateTopmostCellHeight(x5, z5);
			f.SetCellValueFast(x5, y2, z5, 360);
		}
	}

	public void GenerateSandTemple(TerrainChunk chunk)
	{
		Terrain matrix = m_subsystemTerrain.Terrain;
		Random random = new Random(m_seed + chunk.Coords.X * 136279841 + chunk.Coords.Y * 846281393);
		if (!random.Bool(0.006f) || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 300f))
		{
			return;
		}
		int num = chunk.Origin.X + random.Int(0, 15);
		int num2 = chunk.Origin.Y + random.Int(0, 15);
		int num3 = matrix.CalculateTopmostCellHeight(num, num2);
		if (num3 > 70 || matrix.GetCellContentsFast(num, num3, num2) != 7)
		{
			return;
		}
		num3 -= random.Int(0, 3);
		int num4 = random.Int(3, 5) / 2;
		for (int i = -num4; i <= num4; i++)
		{
			for (int j = -num4; j <= num4; j++)
			{
				if (i == -num4 || i == num4 || j == -num4 || j == num4)
				{
					for (int k = 0; k <= 2; k++)
					{
						matrix.SetCellValueFast(num + i, num3 + k, num2 + j, 4);
					}
				}
				if ((i == -num4 && j == -num4) || (i == -num4 && j == num4) || (i == num4 && j == -num4) || (i == num4 && j == num4))
				{
					for (int l = 0; l <= 2; l++)
					{
						matrix.SetCellValueFast(num + i, num3 + l, num2 + j, 0);
					}
				}
			}
		}
		for (int m = -num4 + 1; m < num4; m++)
		{
			for (int n = -num4 + 1; n < num4; n++)
			{
				if ((m == -num4 + 1 || m == num4 - 1) && (n == -num4 + 1 || n == num4 - 1))
				{
					matrix.SetCellValueFast(num + m, num3 + 2, num2 + n, 0);
				}
			}
		}
		for (int num5 = -num4 + 1; num5 < num4; num5++)
		{
			for (int num6 = -num4 + 1; num6 < num4; num6++)
			{
				for (int num7 = -1; num7 <= 0; num7++)
				{
					matrix.SetCellValueFast(num + num5, num3 + num7, num2 + num6, 72);
				}
			}
		}
		for (int num8 = -num4 + 1; num8 < num4; num8++)
		{
			for (int num9 = -num4 + 1; num9 < num4; num9++)
			{
				matrix.SetCellValueFast(num + num8, num3 + 3, num2 + num9, 52);
			}
		}
		matrix.SetCellValueFast(num, num3 + 3, num2, 0);
		if (random.Bool(0.3f))
		{
			matrix.SetCellValueFast(num, num3 - 1, num2, 71);
		}
		else
		{
			int num10 = random.Int(9, 13);
			for (int num11 = 1; num11 <= num10; num11++)
			{
				if (num3 - num11 >= 0)
				{
					matrix.SetCellValueFast(num, num3 - num11, num2, (num11 == num10) ? 92 : 0);
				}
			}
		}
		if (random.Bool(0.4f))
		{
			int x = num + random.Int(-num4 + 1, num4 - 1);
			int z = num2 + random.Int(-num4 + 1, num4 - 1);
			matrix.SetCellValueFast(x, num3 + 1, z, Terrain.MakeBlockValue(68));
		}
		if (random.Bool(0.9f))
		{
			int x2 = num + random.Int(-num4 + 1, num4 - 1);
			int z2 = num2 + random.Int(-num4 + 1, num4 - 1);
			matrix.SetCellValueFast(x2, num3 + 1, z2, Terrain.MakeBlockValue(52));
		}
	}

	public void GenerateGlaciers(TerrainChunk chunk)
	{
		Random random = new Random((int)MathUtils.Hash((uint)(m_seed + chunk.Coords.X + 14789 * chunk.Coords.Y)));
		if (CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 5000f || chunk.GetTemperatureFast(8, 8) > 0 || chunk.GetHumidityFast(8, 8) > 0)
		{
			return;
		}
		int inventory = random.Int(4, 12);
		int num = random.Int(4, 12);
		int num2 = random.Int(2, 6);
		int num3 = random.Int(0, 45);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				float array = MathUtils.Sqrt(MathUtils.Sqr(i - inventory) + MathUtils.Sqr(j - num));
				if (!(array <= (float)num2 + SimplexNoise.OctavedNoise((float)i * 0.1f, (float)j * 0.1f, 0.2f, 2, 2f, 0.5f)))
				{
					continue;
				}
				int num6 = num3 - (int)(array / (float)num2 * (float)num3);
				int array2 = chunk.CalculateTopmostCellHeight(i, j);
				for (int k = array2 + 1; k <= array2 + num6; k++)
				{
					if (k == array2 + num6)
					{
						chunk.SetCellValueFast(i, k, j, 350);
					}
					else
					{
						chunk.SetCellValueFast(i, k, j, 62);
					}
				}
			}
		}
	}

	public void GenerateLighthouse(TerrainChunk chunk)
	{
		Random array = new Random(m_seed + chunk.Coords.X * 374761393 + chunk.Coords.Y * 584653217);
		if (!array.Bool(0.0001f) || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) < 1f || new Vector2(chunk.Origin.X, chunk.Origin.Y).Length() < 160f)
		{
			return;
		}
		Terrain num = m_subsystemTerrain.Terrain;
		int num2 = 8 + array.Int(-2, 2);
		int i = 8 + array.Int(-2, 2);
		int num3 = chunk.Origin.X + num2;
		int num4 = chunk.Origin.Y + i;
		int num5 = num.CalculateTopmostCellHeight(num3, num4);
		if (num5 <= 2 || num5 > 160)
		{
			return;
		}
		int num6 = array.Int(0, 10);
		num5 = Math.Max(1, num5 - num6);
		int num7 = 73;
		int num8 = Terrain.MakeBlockValue(73, 0, PaintedCubeBlock.SetColor(0, 0));
		int value = 359;
		int value2 = 21;
		int num9 = array.Int(36, 40);
		if (num5 + num9 > 160)
		{
			return;
		}
		int num10 = array.Int(5, 7);
		int num11 = num10 - 1;
		int num12 = Math.Max(2, num11 - array.Int(2, 4));
		for (int j = -num10; j <= num10; j++)
		{
			for (int k = -num10; k <= num10; k++)
			{
				if (j * j + k * k <= num10 * num10)
				{
					num.SetCellValueFast(num3 + j, num5, num4 + k, value);
				}
			}
		}
		int num13 = num5 + 1;
		int num14 = num9 / 6;
		int num15 = num9 % 6;
		int num16 = 0;
		for (int l = 0; l < num14; l++)
		{
			float f = (float)l / (float)Math.Max(num14 - 1, 1);
			int num17 = (int)MathUtils.Round(MathUtils.Lerp(num11, num12, f));
			num16 = num17;
			for (int m = 0; m < 6; m++)
			{
				int value3 = ((m < 3) ? num8 : num7);
				int y = num13 + m;
				int num18 = num17 * num17;
				for (int n = -num17; n <= num17; n++)
				{
					for (int num19 = -num17; num19 <= num17; num19++)
					{
						int num20 = n * n + num19 * num19;
						if (num20 <= num18 && num20 >= (num17 - 1) * (num17 - 1))
						{
							num.SetCellValueFast(num3 + n, y, num4 + num19, value3);
						}
					}
				}
			}
			int y2 = num13 + 5;
			for (int num21 = -(num17 - 2); num21 <= num17 - 2; num21++)
			{
				for (int num22 = -(num17 - 2); num22 <= num17 - 2; num22++)
				{
					if (num21 * num21 + num22 * num22 < (num17 - 2) * (num17 - 2))
					{
						num.SetCellValueFast(num3 + num21, y2, num4 + num22, value2);
					}
				}
			}
			num13 += 6;
		}
		num13 += num15;
		int num23 = num13 - 1;
		int num24 = 9;
		for (int num25 = -num16; num25 <= num16; num25++)
		{
			for (int num26 = -num16; num26 <= num16; num26++)
			{
				int num27 = num25 * num25 + num26 * num26;
				if (num27 <= num16 * num16 && num27 >= (num16 - 1) * (num16 - 1))
				{
					num.SetCellValueFast(num3 + num25, num23, num4 + num26, value);
				}
			}
		}
		for (int num28 = 0; num28 < num24; num28++)
		{
			int y3 = num23 + num28;
			float f2 = (float)num28 / (float)(num24 - 1);
			int num29 = (int)MathUtils.Round(MathUtils.Lerp(num16, 1f, f2));
			for (int num30 = -num29; num30 <= num29; num30++)
			{
				for (int num31 = -num29; num31 <= num29; num31++)
				{
					if (num30 * num30 + num31 * num31 <= num29 * num29)
					{
						num.SetCellValueFast(num3 + num30, y3, num4 + num31, value);
					}
				}
			}
		}
		int num32 = num23 + num24 - 1;
		HashSet<Point3> hashSet = new HashSet<Point3>();
		int[] array2 = new int[4] { -1, -1, 1, 1 };
		int[] array3 = new int[4] { -1, 1, -1, 1 };
		for (int num33 = 0; num33 < 4; num33++)
		{
			int x = num3 + array2[num33];
			int z = num4 + array3[num33];
			for (int num34 = num5; num34 <= num32; num34++)
			{
				num.SetCellValueFast(x, num34, z, value);
				hashSet.Add(new Point3(x, num34, z));
			}
		}
		float num35 = array.Float(0.3f, 0.6f);
		float num36 = 0.001f + array.Float(0.001f, 0.0015f);
		int octaves = 2;
		int num37 = num10 + 12;
		for (int num38 = num32; num38 >= num5; num38--)
		{
			float num39 = (float)(num38 - num5) / (float)Math.Max(num32 - num5, 1);
			float num40 = MathUtils.Lerp(num35, num35 * 0.05f, 1f - num39);
			for (int num41 = -num37; num41 <= num37; num41++)
			{
				for (int num42 = -num37; num42 <= num37; num42++)
				{
					int num43 = num3 + num41;
					int num44 = num4 + num42;
					if (num.GetCellValueFast(num43, num38, num44) != 0 && !hashSet.Contains(new Point3(num43, num38, num44)))
					{
						float num45 = num43 - num3;
						float num46 = num44 - num4;
						float x2 = (float)(num43 + num38 * 31) * num36 + num45 * 0.07f;
						float y4 = (float)(num44 - num38 * 17) * num36 + num46 * 0.07f;
						if ((SimplexNoise.OctavedNoise(x2, y4, 0f, octaves, 0.5f, 2f) + 1f) * 0.5f < num40)
						{
							num.SetCellValueFast(num43, num38, num44, 0);
						}
					}
				}
			}
		}
		for (int num47 = num32; num47 >= num5; num47--)
		{
			for (int num48 = -num37; num48 <= num37; num48++)
			{
				for (int num49 = -num37; num49 <= num37; num49++)
				{
					int x3 = num3 + num48;
					int z2 = num4 + num49;
					if (num.GetCellValueFast(x3, num47, z2) != 0 && num47 > num5 && num.GetCellValueFast(x3, num47 - 1, z2) == 0 && array.Bool(0.7f) && !hashSet.Contains(new Point3(x3, num47, z2)))
					{
						num.SetCellValueFast(x3, num47, z2, 0);
					}
				}
			}
		}
		float num50 = 0.1f;
		float num51 = 0.08f;
		float num52 = 0.06f;
		float num53 = 0.02f;
		float num54 = 0.01f;
		float num55 = 0.01f;
		float num56 = 0.03f;
		float num57 = 0.02f;
		for (int num58 = -num16; num58 <= num16; num58++)
		{
			for (int num59 = -num16; num59 <= num16; num59++)
			{
				for (int num60 = num5; num60 <= num32; num60++)
				{
					if (num.GetCellValueFast(num3 + num58, num60, num4 + num59) != 21 || num.GetCellValueFast(num3 + num58, num60 + 1, num4 + num59) != 0)
					{
						continue;
					}
					float num61 = num50 + num51 + num52 + num53 + num54 + num55 + num56 + num57;
					float num62 = array.Float(0f, 1f);
					if (num62 <= num61)
					{
						float num63 = num62;
						if ((num63 -= num50) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 374);
						}
						else if ((num63 -= num51) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 373);
						}
						else if ((num63 -= num52) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 372);
						}
						else if ((num63 -= num53) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 105);
						}
						else if ((num63 -= num54) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 106);
						}
						else if ((num63 -= num55) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 107);
						}
						else if ((num63 -= num56) <= 0f)
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 208);
						}
						else
						{
							num.SetCellValueFast(num3 + num58, num60 + 1, num4 + num59, 312);
						}
					}
				}
			}
		}
	}

	public void GenerateQuicksandLakes(TerrainChunk chunk)
	{
		int subsystemGameInfo = chunk.Coords.X;
		int matrix = chunk.Coords.Y;
		Random num = new Random(m_seed + subsystemGameInfo * 9773 + matrix * 1013);
		if (!num.Bool(0.072f))
		{
			return;
		}
		int matrix2 = num.Int(0, 2);
		for (int x = 0; x < matrix2; x++)
		{
			int y = num.Int(2, 13);
			int z = num.Int(2, 13);
			int x2 = chunk.CalculateTopmostCellHeight(y, z);
			if (x2 <= 4 || chunk.GetCellContentsFast(y, x2, z) != 7)
			{
				continue;
			}
			bool vector2 = true;
			for (int i = -2; (i <= 2) & vector2; i++)
			{
				for (int j = -2; j <= 2; j++)
				{
					if (chunk.GetCellContentsFast(y + i, x2, z + j) != 7)
					{
						vector2 = false;
						break;
					}
				}
			}
			if (!vector2)
			{
				continue;
			}
			float v = MathUtils.Saturate((SimplexNoise.OctavedNoise((float)(subsystemGameInfo * 16 + y) * 0.1f, (float)(matrix * 16 + z) * 0.1f, 0.25f, 1, 2f, 1f) + 1f) * 0.5f);
			int num2;
			int size;
			if (v < 0.35f)
			{
				num2 = num.Int(1, 2);
				size = 1;
			}
			else if (v < 0.75f)
			{
				num2 = num.Int(2, 4);
				size = num.Int(2, 3);
			}
			else
			{
				num2 = num.Int(3, 6);
				size = num.Int(4, 6);
			}
			if (y - num2 < 0 || y + num2 > 15 || z - num2 < 0 || z + num2 > 15)
			{
				continue;
			}
			int matrix3 = Math.Max(x2 - size + 1, 1);
			for (int k = -num2; k <= num2; k++)
			{
				for (int l = -num2; l <= num2; l++)
				{
					if (k * k + l * l <= num2 * num2)
					{
						for (int num3 = x2; num3 >= matrix3; num3--)
						{
							int data = 0;
							data = FluidBlock.SetLevel(data, 0);
							data = ((num3 != x2) ? FluidBlock.SetIsTop(data, isTop: false) : FluidBlock.SetIsTop(data, isTop: true));
							data = QuicksandBlock.SetKind(data, QuicksandBlock.QuicksandKind.Sand);
							chunk.SetCellValueFast(y + k, num3, z + l, Terrain.MakeBlockValue(391, 0, data));
						}
						if (x2 + 1 < 255)
						{
							chunk.SetCellValueFast(y + k, x2 + 1, z + l, Terrain.MakeBlockValue(0));
						}
					}
				}
			}
		}
	}

	private void GenerateSeaStones(TerrainChunk chunk)
	{
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + x * 911 + y * 3571 + 401);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = i + chunk.Origin.X;
				int num2 = j + chunk.Origin.Y;
				int num3 = 0;
				int num4 = -1;
				int num5 = 0;
				for (int num6 = 254; num6 >= 1; num6--)
				{
					int num7 = Terrain.ExtractContents(chunk.GetCellValueFast(i, num6, j));
					if (num7 != 0)
					{
						if (!(BlocksManager.Blocks[num7] is FocalorsWaterBlock))
						{
							if (num3 > 0)
							{
								num4 = num6;
								num5 = num7;
							}
							break;
						}
						num3++;
						if (num3 > 64)
						{
							break;
						}
					}
				}
				if (num4 < 0 || (num5 != 2 && num5 != 7 && num5 != 72 && num5 != 3 && num5 != 4 && num5 != 67) || chunk.GetCellContentsFast(i, num4 + 1, j) != 18)
				{
					continue;
				}
				if (CalculateOceanShoreDistance(num, num2) < -2f)
				{
					if (random.Int(0, 199) != 0)
					{
						continue;
					}
					int num8 = random.Int(5, 12);
					int num9 = i;
					int num10 = num4;
					int num11 = j;
					for (int k = 0; k < num8; k++)
					{
						if (num10 >= 1 && num10 < 255 && num9 >= 0 && num9 < 16 && num11 >= 0 && num11 < 16 && chunk.GetCellContentsFast(num9, num10 + 1, num11) == 18)
						{
							int cellContentsFast = chunk.GetCellContentsFast(num9, num10, num11);
							if (cellContentsFast == 2 || cellContentsFast == 6 || cellContentsFast == 7 || cellContentsFast == 72 || cellContentsFast == 3 || cellContentsFast == 4)
							{
								chunk.SetCellValueFast(num9, num10, num11, Terrain.MakeBlockValue(401));
							}
						}
						switch (random.Int(0, 5))
						{
						case 0:
							num9++;
							break;
						case 1:
							num9--;
							break;
						case 2:
							num11++;
							break;
						case 3:
							num11--;
							break;
						case 4:
							num10--;
							break;
						default:
							num10++;
							break;
						}
					}
				}
				else
				{
					if (random.Int(0, 499) != 0)
					{
						continue;
					}
					int num12 = random.Int(1, 5);
					for (int l = 0; l < num12; l++)
					{
						int num13 = MathUtils.Clamp(num4 - random.Int(0, 2), 1, 254);
						if (chunk.GetCellContentsFast(i, num13 + 1, j) == 18)
						{
							int cellContentsFast2 = chunk.GetCellContentsFast(i, num13, j);
							if (cellContentsFast2 == 2 || cellContentsFast2 == 7 || cellContentsFast2 == 72 || cellContentsFast2 == 3 || cellContentsFast2 == 4 || cellContentsFast2 == 67)
							{
								chunk.SetCellValueFast(i, num13, j, Terrain.MakeBlockValue(401));
							}
						}
					}
				}
			}
		}
	}

	public TerrainLODPoint CalculateLODPoint(float x, float z)
	{
		TerrainLODPoint result = default;
		result.Height = CalculateHeight(x, z);
		result.Temperature = CalculateTemperature(x, z);
		result.Humidity = CalculateHumidity(x, z);
		return result;
	}

	public int GetSurfaceBlockIndex(int temperature, int humidity, int y, float shoreDist, int contentsAbove, int contentsSelf)
	{
		if (contentsSelf == 4)
		{
			if (temperature <= 4 || temperature >= 7)
			{
				return 7;
			}
			return 6;
		}
		int num = temperature / 4;
		if (y > 120 && SubsystemWeather.IsPlaceFrozen(temperature, y))
		{
			return 62;
		}
		if ((y < 66 || y == 84 + num || y == 103 + num) && humidity == 9 && temperature % 6 == 1)
		{
			return 66;
		}
		if (contentsAbove == 18 && humidity > 8 && humidity % 2 == 0 && temperature % 3 == 0)
		{
			return 72;
		}
		if (humidity < 6 && temperature > 8)
		{
			return 7;
		}
		return 2;
	}

	private void CalculateTerrainDensity(Grid3d densityGrid, Grid2d shoreGrid, Grid2d mountainGrid, int originX, int originZ, int sizeX, int sizeZ)
	{
		int oceanLevel = OceanLevel;
		for (int i = 0; i < sizeZ; i++)
		{
			for (int j = 0; j < sizeX; j++)
			{
				float x = j + originX;
				float z = i + originZ;
				shoreGrid.Set(j, i, CalculateOceanShoreDistance(x, z));
				mountainGrid.Set(j, i, CalculateMountainRangeFactor(x, z));
			}
		}
		for (int k = 0; k < densityGrid.SizeX; k++)
		{
			for (int l = 0; l < densityGrid.SizeZ; l++)
			{
				int num = k * 4 + originX;
				int num2 = l * 4 + originZ;
				float num3 = CalculateHeight(num, num2);
				float v = CalculateMountainRangeFactor(num, num2);
				float num4 = MathUtils.Lerp(TGMinTurbulence, 1f, Squish(v, TGTurbulenceZero, 1f));
				float num5 = CalculateOceanShoreDistance(num, num2);
				bool num6 = num3 < (float)oceanLevel - 2f && num5 < 0f;
				float num7 = 0f;
				if (num6)
				{
					num7 = MathUtils.Saturate((0f - num5 - 40f) / 80f);
				}
				float num8 = ((num7 > 0f) ? MathUtils.Lerp(TGDensityBias, 1f, num7) : TGDensityBias);
				for (int m = 0; m < densityGrid.SizeY; m++)
				{
					int num9 = m * 8;
					float num10 = SimplexNoise.OctavedNoise(num, num9, num2, TGTurbulenceFreq, TGTurbulenceOctaves, 4f, TGTurbulencePersistence);
					float num11 = TGTurbulenceStrength * num4 * MathUtils.Saturate(num3 - (float)num9) * (2f * num10 - 1f);
					float num12 = num3 - ((float)num9 + num11);
					num12 += MathUtils.Max(4f * (num8 - (float)num9), 0f);
					if (num7 > 0f)
					{
						float num13 = MathUtils.Saturate(1f - (float)num9 / 64f);
						if (num13 > 0f)
						{
							num12 -= 64f * num7 * num13;
						}
					}
					densityGrid.Set(k, m, l, num12);
				}
			}
		}
	}

	private void FillChunkBlocks(TerrainChunk chunk, Grid3d densityGrid, Grid2d shoreGrid, Grid2d mountainGrid, int x1, int z1, int x2, int z2)
	{
		for (int i = 0; i < densityGrid.SizeX - 1; i++)
		{
			for (int j = 0; j < densityGrid.SizeZ - 1; j++)
			{
				for (int k = 0; k < densityGrid.SizeY - 1; k++)
				{
					densityGrid.Get8(i, k, j, out var v, out var v2, out var v3, out var v4, out var v5, out var v6, out var v7, out var v8);
					float num = (v2 - v) * 0.25f;
					float num2 = (v4 - v3) * 0.25f;
					float num3 = (v6 - v5) * 0.25f;
					float num4 = (v8 - v7) * 0.25f;
					float num5 = v;
					float num6 = v3;
					float num7 = v5;
					float num8 = v7;
					for (int l = 0; l < 4; l++)
					{
						float num9 = (num7 - num5) * 0.25f;
						float num10 = (num8 - num6) * 0.25f;
						float num11 = num5;
						float num12 = num6;
						for (int m = 0; m < 4; m++)
						{
							float num13 = (num12 - num11) * 0.125f;
							float num14 = num11;
							int num15 = l + i * 4;
							int num16 = m + j * 4;
							int x3 = x1 + num15;
							int z3 = z1 + num16;
							float x4 = shoreGrid.Get(num15, num16);
							float num17 = mountainGrid.Get(num15, num16);
							int temperatureFast = chunk.GetTemperatureFast(x3, z3);
							int humidityFast = chunk.GetHumidityFast(x3, z3);
							int num18 = TerrainChunk.CalculateCellIndex(x3, 0, z3);
							float f = num17 - 0.01f * (float)humidityFast;
							float sandThreshold = MathUtils.Lerp(100f, 0f, f);
							float stoneThreshold = MathUtils.Lerp(300f, 30f, f);
							bool isSpecial = (temperatureFast > 8 && humidityFast < 8 && num17 < 0.97f) || (MathUtils.Abs(x4) < 16f && num17 < 0.97f);
							for (int n = 0; n < 8; n++)
							{
								int num19 = n + k * 8;
								int blockForPosition = GetBlockForPosition(x3, num19, z3, num14, sandThreshold, stoneThreshold, isSpecial);
								chunk.SetCellValueFast(num18 + num19, blockForPosition);
								num14 += num13;
							}
							num11 += num9;
							num12 += num10;
						}
						num5 += num;
						num6 += num2;
						num7 += num3;
						num8 += num4;
					}
				}
			}
		}
	}

	private void GenerateUndergroundLakes(TerrainChunk chunk)
	{
		int num = 44;
		int num2 = 14;
		float num3 = 0.04f;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				float num4 = i + chunk.Origin.X;
				float num5 = j + chunk.Origin.Y;
				float num6 = CalculateHeight(num4, num5);
				if (!(CalculateOceanShoreDistance(num4, num5) >= -20f))
				{
					continue;
				}
				float num7 = MathUtils.Abs(SimplexNoise.OctavedNoise(num4 + m_lakesWaterOffset.X, num5 + m_lakesWaterOffset.Y, 0.012f, 2, 2f, 0.5f));
				float num8 = SimplexNoise.OctavedNoise(num4 + m_lakesWaterMaskOffset.X, num5 + m_lakesWaterMaskOffset.Y, 0.002f, 2, 2f, 0.5f);
				float num9 = 0.06f;
				float num10 = 0f;
				if (num8 > 0.4f)
				{
					num9 = 0.18f;
					num10 = 1f;
				}
				else if (num8 < -0.3f)
				{
					num9 = 0f;
				}
				float num11 = num9 + num3;
				if (num7 < num11 && num9 > 0.001f)
				{
					float num12 = 1f - num7 / num11;
					if (num12 < 0f)
					{
						num12 = 0f;
					}
					float num13 = MathUtils.Sin(MathUtils.Sqrt(num12) * 1.57f);
					float num14 = 7f;
					float num15 = ((num10 > 0f) ? 5f : 6f);
					int num16 = num - (int)(num13 * num14) + (int)((1f - num13) * 5f);
					int num17 = num + 3 + (int)(num13 * num15);
					if (num16 < 30)
					{
						num16 = 30;
					}
					float x = SimplexNoise.Noise(num4 * 0.07f, num5 * 0.07f);
					int num18 = 4 + (int)(MathUtils.Abs(x) * 3f);
					float num19 = SimplexNoise.Noise(num4 * 0.1f + 100f, num5 * 0.1f + 100f);
					if (num19 > 0.2f)
					{
						num18 += (int)((num19 - 0.2f) * 15f);
					}
					int num20 = num18;
					int num21 = MathUtils.Max(num16 - num20, 0);
					float num22 = SimplexNoise.Noise(num4 * 0.1f, num5 * 0.1f);
					int num23 = 3;
					if (num22 > 0.6f)
					{
						num23 = 3;
					}
					else if (num22 > 0.2f)
					{
						num23 = 67;
					}
					else if (num22 < -0.3f)
					{
						num23 = 4;
					}
					float num24 = SimplexNoise.Noise(num4 * 0.05f, num5 * 0.05f);
					int num25 = 6;
					if (num24 > 0.5f)
					{
						num25 = 72;
					}
					else if (num24 < -0.4f)
					{
						num25 = 66;
					}
					else if (MathUtils.Abs(num24) < 0.15f)
					{
						num25 = 5;
					}
					if (SimplexNoise.Noise(num4 * 0.85f, num5 * 0.85f) > 0.82f)
					{
						num25 = 397;
					}
					float num26 = SimplexNoise.Noise(num4 * 0.35f, num5 * 0.35f);
					float num27 = ((num25 == 397) ? (-1f) : 0.2f);
					int num28 = (int)(MathUtils.Abs(SimplexNoise.Noise(num4 * 0.1f, num5 * 0.1f)) * 2f);
					if ((float)num17 < num6 - 5f)
					{
						for (int k = num21; k <= num17; k++)
						{
							int num29 = TerrainChunk.CalculateCellIndex(i, k, j);
							if (num29 < 0 || num29 >= 65535 || Terrain.ExtractContents(chunk.GetCellValueFast(num29)) == 1)
							{
								continue;
							}
							int num30 = num23;
							if (num26 > num27)
							{
								num30 = num25;
								if (num30 == 6 && k <= num)
								{
									num30 = num23;
								}
							}
							if (k <= num16)
							{
								if (k <= num + num28)
								{
									chunk.SetCellValueFast(num29, num30);
								}
							}
							else if (k <= num)
							{
								if (num7 >= num9)
								{
									chunk.SetCellValueFast(num29, num30);
								}
								else
								{
									chunk.SetCellValueFast(num29, 18);
								}
							}
							else
							{
								chunk.SetCellValueFast(num29, 0);
							}
						}
					}
				}
				float num31 = MathUtils.Abs(SimplexNoise.OctavedNoise(num4 + m_lakesMagmaOffset.X, num5 + m_lakesMagmaOffset.Y, 0.01f, 2, 2f, 0.5f));
				float num32 = SimplexNoise.OctavedNoise(num4 + m_lakesMagmaOffset.X + 234f, num5 + m_lakesMagmaOffset.Y, 0.002f, 2, 2f, 0.5f);
				float num33 = 0.05f;
				bool flag = false;
				if (num32 > 0.5f)
				{
					num33 = 0.22f;
					flag = true;
				}
				else if (num32 < -0.1f)
				{
					num33 = 0f;
				}
				float num34 = num33 + num3;
				if (!(num31 < num34) || !(num33 > 0.001f))
				{
					continue;
				}
				float num35 = MathUtils.Sin((1f - num31 / num34) * 1.57f);
				float num36 = (flag ? 9f : 6f);
				float num37 = (flag ? 6f : 5f);
				int num38 = num2 - (int)(num35 * num36) + (int)((1f - num35) * 4f);
				int num39 = num2 + 2 + (int)(num35 * num37);
				if (num38 < 2)
				{
					num38 = 2;
				}
				if (num39 > 28)
				{
					num39 = 28;
				}
				float x2 = SimplexNoise.Noise(num4 * 0.08f, num5 * 0.08f);
				int num40 = 4 + (int)(MathUtils.Abs(x2) * 3f);
				float num41 = SimplexNoise.Noise(num4 * 0.12f + 50f, num5 * 0.12f + 50f);
				if (num41 > 0.25f)
				{
					num40 += (int)((num41 - 0.25f) * 12f);
				}
				int num42 = num40;
				int num43 = MathUtils.Max(num38 - num42, 0);
				float num44 = SimplexNoise.Noise(num4 * 0.15f, num5 * 0.15f);
				int num45 = 67;
				if (num44 > 0.3f)
				{
					num45 = 344;
					if (SimplexNoise.Noise(num4 * 0.6f, num5 * 0.6f) > 0.93f)
					{
						num45 = 112;
					}
					else if (num44 > 0.85f)
					{
						num45 = 67;
					}
				}
				int num46 = (int)(MathUtils.Abs(SimplexNoise.Noise(num4 * 0.1f, num5 * 0.1f)) * 2f);
				if (!((float)num39 < num6 - 10f))
				{
					continue;
				}
				for (int l = num43; l <= num39; l++)
				{
					int num47 = TerrainChunk.CalculateCellIndex(i, l, j);
					if (num47 < 0 || num47 >= 65535 || Terrain.ExtractContents(chunk.GetCellValueFast(num47)) == 1)
					{
						continue;
					}
					if (l <= num38)
					{
						if (l <= num2 + num46)
						{
							int num48 = num45;
							if (num48 == 6 && l <= num2)
							{
								num48 = 67;
							}
							chunk.SetCellValueFast(num47, num48);
						}
					}
					else if (l <= num2)
					{
						if (num31 >= num33)
						{
							int num49 = num45;
							if (num49 == 6 && l <= num2)
							{
								num49 = 67;
							}
							chunk.SetCellValueFast(num47, num49);
						}
						else
						{
							chunk.SetCellValueFast(num47, 92);
						}
					}
					else
					{
						chunk.SetCellValueFast(num47, 0);
					}
				}
			}
		}
	}

	private void GenerateSneakCaves(TerrainChunk chunk)
	{
		float x = chunk.Origin.X + 8;
		float z = chunk.Origin.Y + 8;
		int num = 14;
		int num2 = (int)((CalculateHeight(x, z) - 6f) / (float)num);
		float num3 = MathUtils.Saturate((CalculateRoughnessFactor(x, z) - 0.2f) * 2.5f);
		float frequency = 0.008f;
		float num4 = 0.004f;
		float num5 = 35f;
		for (int i = 0; i < num2; i++)
		{
			float num6 = i * 1234;
			int num7 = 6 + i * num;
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					int num8 = j + chunk.Origin.X;
					int num9 = k + chunk.Origin.Y;
					float num10 = SimplexNoise.OctavedNoise((float)num8 + m_cavesScaleOffset.X, (float)num9 + m_cavesScaleOffset.Y, 0.0014f / TGBiomeScaling, 5, 2f, 0.6f);
					float f = MathUtils.Saturate(3f * num10 - 1f);
					float num11 = MathUtils.Lerp(0.001f, 1f, f);
					float num12 = SimplexNoise.OctavedNoise((float)num8 + m_riversOffset.X + num6, (float)num9 + m_riversOffset.Y + num6, frequency, 3, 2f, 0.5f);
					float num13 = MathUtils.Abs(2f * num12 - 1f);
					float x2 = SimplexNoise.Noise((float)num8 * 0.05f, (float)num9 * 0.05f) * 0.015f * num3;
					float num14 = 0.022f + MathUtils.Saturate(x2);
					if (!(num13 < num14))
					{
						continue;
					}
					float num15 = SimplexNoise.Noise(((float)num8 + num6) * num4, ((float)num9 + num6) * num4);
					float num16 = SimplexNoise.Noise(((float)num8 + num6) * 0.05f, ((float)num9 + num6) * 0.05f) * 2f;
					int num17 = num7 + (int)(num15 * num5 + num16);
					float num18 = SimplexNoise.Noise(((float)num8 + num6) * 0.01f, ((float)num9 + num6) * 0.01f);
					if (num18 < -0.7f)
					{
						num17 -= 10 + (int)(MathUtils.Abs(num18) * 20f);
					}
					float num19 = SimplexNoise.Noise((float)num8 * 0.15f, (float)num9 * 0.15f);
					float num20 = (2.4f + num19 * 1.2f) * num11;
					if (num20 < 0.6f)
					{
						num20 = 0.6f;
					}
					int num21 = num17 - (int)num20 - 1;
					int num22 = num17 + (int)num20 + 1;
					if (num21 < 2)
					{
						num21 = 2;
					}
					if (num22 > 254)
					{
						num22 = 254;
					}
					int temperatureFast = chunk.GetTemperatureFast(j, k);
					int humidityFast = chunk.GetHumidityFast(j, k);
					if (temperatureFast > 8 && humidityFast < 8)
					{
						int num23 = 254;
						while (num23 >= 0 && Terrain.ExtractContents(chunk.GetCellValueFast(j, num23, k)) == 0)
						{
							num23--;
						}
						if (num22 > num23 - 7)
						{
							num22 = num23 - 7;
						}
					}
					for (int l = num21; l <= num22; l++)
					{
						float num24 = MathUtils.Abs(l - num17);
						float x3 = SimplexNoise.Noise((float)num8 * 0.08f + (float)l * 0.03f, (float)num9 * 0.08f + num6 * 0.1f);
						bool flag = false;
						if (num3 > 0.1f && MathUtils.Abs(x3) < 0.15f * num3)
						{
							flag = true;
						}
						if (flag || !(num13 / num14 * (num13 / num14) + num24 / num20 * (num24 / num20) < 0.9f))
						{
							continue;
						}
						int num25 = TerrainChunk.CalculateCellIndex(j, l, k);
						if (num25 >= 0 && num25 < 65536)
						{
							int num26 = Terrain.ExtractContents(chunk.GetCellValueFast(num25));
							if (num26 != 1 && num26 != 18 && num26 != 92 && num26 != 2 && num26 != 8)
							{
								chunk.SetCellValueFast(num25, 0);
							}
						}
					}
				}
			}
		}
	}

	public int CalculateRoughness(float x, float z)
	{
		return MathUtils.Clamp((int)(CalculateRoughnessFactor(x, z) * 16f), 0, 15);
	}

	private void GenerateColossalCaves(TerrainChunk chunk)
	{
		float num = 0.004f;
		float num2 = 0.008f;
		float num3 = 0.004f;
		float num4 = 0.02f;
		float num5 = 0.4f;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num6 = i + chunk.Origin.X;
				int num7 = j + chunk.Origin.Y;
				if (CalculateRoughness(num6, num7) < 6)
				{
					continue;
				}
				int num8 = (int)CalculateHeight(num6, num7) - 15;
				for (int k = 10; k < num8; k++)
				{
					float num9 = MathUtils.Abs(SimplexNoise.Noise((float)num6 * num, (float)k * num2, (float)num7 * num3));
					num9 = 1f - num9;
					float num10 = SimplexNoise.Noise((float)num6 * num4, (float)num7 * num4);
					if (num10 > 0.4f)
					{
						num9 -= (num10 - 0.4f) * 1.5f;
					}
					if (!(num9 > num5))
					{
						continue;
					}
					float num11 = 1f;
					if (k < 20)
					{
						num11 = (float)(k - 10) / 10f;
					}
					if (k > num8 - 10)
					{
						num11 = (float)(num8 - k) / 10f;
					}
					if (!(num9 * num11 > num5))
					{
						continue;
					}
					int num12 = TerrainChunk.CalculateCellIndex(i, k, j);
					if (num12 >= 0 && num12 < 65536)
					{
						int num13 = Terrain.ExtractContents(chunk.GetCellValueFast(num12));
						if (num13 != 1 && num13 != 18 && num13 != 92)
						{
							chunk.SetCellValueFast(num12, 0);
						}
					}
				}
			}
		}
	}

	private void GenerateUniversalCaves(TerrainChunk chunk)
	{
		float num = 0.02f;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num2 = i + chunk.Origin.X;
				int num3 = j + chunk.Origin.Y;
				if (!(CalculateMountainRangeFactor(num2, num3) <= 1f - TGMountainsPercentage) || !(SimplexNoise.Noise(((float)num2 + m_cavesScaleOffset.X) * 0.004f, ((float)num3 + m_cavesScaleOffset.Y) * 0.004f) >= 0.4f))
				{
					continue;
				}
				int num4 = CalculateRoughness(num2, num3);
				float num5 = CalculateHeight(num2, num3);
				bool flag = false;
				float num6;
				float num7;
				float num8;
				float num9;
				int num10;
				if (num4 < 5)
				{
					num6 = 0.01f;
					num7 = 0.04f;
					num8 = 0.01f;
					num9 = 0.5f;
					num10 = 15;
				}
				else if (num4 < 12)
				{
					num6 = 0.004f;
					num7 = 0.006f;
					num8 = 0.004f;
					num9 = 0.35f;
					num10 = 20;
				}
				else
				{
					num6 = 0.015f;
					num7 = 0.002f;
					num8 = 0.015f;
					num9 = 0.3f;
					num10 = 5;
				}
				int num11 = (int)num5 - num10;
				float num12 = 1f - MathUtils.Abs(SimplexNoise.Noise((float)num2 * 0.12f, (float)num3 * 0.12f));
				float num13 = 0.5f + MathUtils.Saturate(SimplexNoise.Noise((float)num2 * 0.31f, (float)num3 * 0.31f) + 0.5f);
				float num14 = ((num4 >= 12) ? (5f + 15f * num13) : 0f);
				bool flag2 = false;
				int num15 = -1;
				for (int k = 8; k < num11; k++)
				{
					float num16 = MathUtils.Abs(SimplexNoise.Noise(((float)num2 + m_cavesScaleOffset.X) * num6, (float)k * num7, ((float)num3 + m_cavesScaleOffset.Y) * num8));
					num16 = 1f - num16;
					if (num4 >= 12)
					{
						num16 += (1f - MathUtils.Abs(SimplexNoise.Noise((float)num2 * 0.1f, (float)k * 0.4f, (float)num3 * 0.1f))) * 0.15f;
					}
					float num17 = ((num4 >= 12) ? 0.01f : num);
					float num18 = SimplexNoise.Noise(((float)num2 + m_cavesScaleOffset.X) * num17, ((float)num3 + m_cavesScaleOffset.Y) * num17);
					if (num18 > 0.4f)
					{
						num16 -= (num18 - 0.4f) * 1.5f;
					}
					float num19 = num11 - k;
					if (num19 < 8f)
					{
						num16 *= MathUtils.Saturate(num19 / 8f);
					}
					float num20 = 1f;
					if (k < 20)
					{
						num20 = MathUtils.Saturate((float)(k - 8) / 12f);
					}
					if (!flag && k > num11 - 15)
					{
						num20 = MathUtils.Saturate((float)(num11 - k) / 15f);
					}
					if (num16 * num20 > num9)
					{
						if (!flag2)
						{
							num15 = k;
							flag2 = true;
						}
						float num21 = k - num15;
						float num22 = 0.7f + num21 / num14 * 0.25f;
						if (num4 >= 12 && !(num12 <= num22) && !(num21 >= num14))
						{
							continue;
						}
						int num23 = TerrainChunk.CalculateCellIndex(i, k, j);
						if (num23 >= 0 && num23 < 65536)
						{
							int num24 = Terrain.ExtractContents(chunk.GetCellValueFast(num23));
							if (num24 != 1 && num24 != 18 && num24 != 92 && num24 != 2 && num24 != 8)
							{
								chunk.SetCellValueFast(num23, 0);
							}
						}
					}
					else
					{
						if (!flag2)
						{
							continue;
						}
						if (num4 >= 12 && num14 > 0f)
						{
							for (int l = 1; l <= (int)num14; l++)
							{
								float num25 = l;
								float num26 = 0.7f + num25 / num14 * 0.25f;
								if (num12 <= num26 || k - l <= num15)
								{
									break;
								}
								chunk.SetCellValueFast(i, k - l, j, 3);
							}
						}
						flag2 = false;
					}
				}
			}
		}
	}

	private int GetBlockForPosition(int x, int y, int z, float density, float sandThreshold, float stoneThreshold, bool isSpecial)
	{
		if (density < 0f)
		{
			if (y <= OceanLevel)
			{
				return 18;
			}
			return 0;
		}
		if (isSpecial)
		{
			if (density < sandThreshold)
			{
				return 4;
			}
			if (density >= stoneThreshold)
			{
				return 67;
			}
			return 3;
		}
		float num = SimplexNoise.Noise((float)x * 0.05f + (float)m_seed, (float)y * 0.05f, (float)z * 0.05f) * 0.15f;
		if (density >= stoneThreshold + num)
		{
			return 67;
		}
		return 3;
	}

	public void GenerateArcheologicalSoil(TerrainChunk chunk)
	{
		Random random = new Random(m_seed + chunk.Coords.X * 127 + chunk.Coords.Y * 349 + 440);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (!random.Bool(0.0002f))
				{
					continue;
				}
				for (int num = chunk.CalculateTopmostCellHeight(i, j); num > 0; num--)
				{
					if (Terrain.ExtractContents(chunk.GetCellContentsFast(i, num, j)) == 2)
					{
						chunk.SetCellValueFast(i, num, j, Terrain.MakeBlockValue(440));
						break;
					}
				}
			}
		}
	}

	public float CalculateRoughnessFactor(float x, float z)
	{
		float num = SimplexNoise.OctavedNoise(x + m_cavesScaleOffset.X, z + m_cavesScaleOffset.Y, 0.0014f / TGBiomeScaling, 5, 2f, 0.6f);
		return MathUtils.Saturate(3f * num - 1f);
	}

	private void GenerateOases(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int num = chunk.Coords.X * 16;
		int num2 = chunk.Coords.Y * 16;
		int num3 = 64;
		int num4 = (num - 64) / num3;
		int num5 = (num2 - 64) / num3;
		for (int i = num4 - 1; i <= num4 + 1; i++)
		{
			for (int j = num5 - 1; j <= num5 + 1; j++)
			{
				Random random = new Random(m_seed + i * 39841 + j * 98113);
				if (!(random.Float(0f, 1f) < 0.02f))
				{
					continue;
				}
				int num6 = i * num3 + random.Int(16, num3 - 16);
				int num7 = j * num3 + random.Int(16, num3 - 16);
				int num8 = CalculateTemperature(num6, num7);
				int num9 = CalculateHumidity(num6, num7);
				if (num8 <= 8 || num9 >= 6 || !(CalculateOceanShoreDistance(num6, num7) > 100f))
				{
					continue;
				}
				int num10 = (int)CalculateHeight(num6, num7);
				num10 = MathUtils.Max(OceanLevel + 1, num10 - random.Int(1, 3));
				float num11 = random.Float(10f, 18f);
				float num12 = random.Float(2.5f, 5f);
				float num13 = num11 + num12;
				float num14 = num13 + 6f;
				bool flag = random.Bool(0.08f);
				float num15 = (flag ? random.Float(2f, 5f) : 0f);
				int num16 = num10 - random.Int(0, 2);
				random.Bool(0.5f);
				int num17 = (int)num14 + 4;
				for (int k = num6 - num17; k <= num6 + num17; k++)
				{
					for (int l = num7 - num17; l <= num7 + num17; l++)
					{
						if (k < num || k >= num + 16 || l < num2 || l >= num2 + 16)
						{
							continue;
						}
						int x = k - num;
						int z = l - num2;
						int num18 = chunk.CalculateTopmostCellHeight(x, z);
						Random random2 = new Random(m_seed + k * 73856 + l * 19274);
						float num19 = SimplexNoise.Noise((float)k * 0.1f, (float)l * 0.1f) * (num11 * 0.3f);
						num19 += SimplexNoise.Noise((float)k * 0.25f, (float)l * 0.25f) * 1.5f;
						float num20 = Vector2.Distance(new Vector2(num6, num7), new Vector2(k, l)) + num19;
						if (num20 <= num11 && (!flag || num20 >= num15))
						{
							float num21 = num20 / num11;
							if (num21 > 1f)
							{
								num21 = 1f;
							}
							float num22 = (float)Math.Sqrt(1f - num21 * num21);
							float num23 = SimplexNoise.Noise((float)k * 0.4f, (float)l * 0.4f) * 0.3f + 0.8f;
							int num24 = (int)MathUtils.Max(1f, 3f * num22 * num23);
							int num25 = num16 - num24;
							int value = Terrain.MakeBlockValue(7);
							float num26 = random2.Float(0f, 1f);
							if (num26 < 0.001f)
							{
								value = Terrain.MakeBlockValue(100);
							}
							else if (num26 < 0.005f)
							{
								value = Terrain.MakeBlockValue(440, 0, 1024);
							}
							else if (num26 < 0.3f)
							{
								value = Terrain.MakeBlockValue(72);
							}
							else if (num26 < 0.5f)
							{
								value = Terrain.MakeBlockValue(4);
							}
							for (int num27 = num18; num27 >= num25 - 2; num27--)
							{
								if (num27 > num16)
								{
									chunk.SetCellValueFast(x, num27, z, 0);
								}
								else if (num27 > num25)
								{
									chunk.SetCellValueFast(x, num27, z, Terrain.MakeBlockValue(18));
								}
								else
								{
									chunk.SetCellValueFast(x, num27, z, value);
								}
							}
						}
						else
						{
							if (!(num20 <= num14))
							{
								continue;
							}
							bool flag2 = false;
							int num28;
							if (flag && num20 < num15)
							{
								num28 = num16 + 1;
								flag2 = true;
							}
							else
							{
								float num29 = MathUtils.Clamp((num20 - num11) / (num14 - num11), 0f, 1f);
								float f = num29 * num29;
								num28 = (int)MathUtils.Lerp(num16, num18, f);
								if (num20 < num13)
								{
									flag2 = true;
								}
								else
								{
									float num30 = 1f - (num20 - num13) / (num14 - num13);
									if (Terrain.ExtractContents(chunk.GetCellValueFast(x, num18, z)) == 7 && random2.Float(0f, 1f) < num30 * 0.5f)
									{
										flag2 = true;
									}
								}
							}
							int contents = (flag2 ? 8 : 7);
							int contents2 = (flag2 ? 2 : 7);
							for (int num31 = num18; num31 >= num28 - 4; num31--)
							{
								if (num31 > num28)
								{
									chunk.SetCellValueFast(x, num31, z, 0);
								}
								else if (num31 == num28)
								{
									chunk.SetCellValueFast(x, num31, z, Terrain.MakeBlockValue(contents));
								}
								else
								{
									chunk.SetCellValueFast(x, num31, z, Terrain.MakeBlockValue(contents2));
								}
							}
							for (int m = num18 + 1; m <= num28; m++)
							{
								if (m == num28)
								{
									chunk.SetCellValueFast(x, m, z, Terrain.MakeBlockValue(contents));
								}
								else
								{
									chunk.SetCellValueFast(x, m, z, Terrain.MakeBlockValue(contents2));
								}
							}
							if (!flag2)
							{
								continue;
							}
							int temperatureFast = chunk.GetTemperatureFast(x, z);
							int humidity = ((num20 < num13) ? 15 : 12);
							int num32 = PlantsManager.GenerateRandomPlantValue(random2, Terrain.MakeBlockValue(8), temperatureFast, humidity, num28 + 1);
							if (num32 != 0)
							{
								chunk.SetCellValueFast(x, num28 + 1, z, num32);
							}
							if (num20 > num11 + num12 * 0.4f && num20 < num13 && random2.Float(0f, 1f) < 0.015f)
							{
								ReadOnlyList<TerrainBrush> treeBrushes = PlantsManager.GetTreeBrushes((random2.Float(0f, 1f) < 0.02f) ? TreeType.OldMimosa : TreeType.Mimosa);
								if (treeBrushes.Count > 0)
								{
									TerrainBrush terrainBrush = treeBrushes[random2.Int(treeBrushes.Count)];
									terrainBrush.PaintFast(chunk, k, num28 + 1, l);
									chunk.AddBrushPaint(k, num28 + 1, l, terrainBrush);
								}
							}
						}
					}
				}
			}
		}
	}

	private void DryDesertRivers(TerrainChunk chunk)
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = chunk.Coords.X * 16 + i;
				int num2 = chunk.Coords.Y * 16 + j;
				int num3 = CalculateTemperature(num, num2);
				int num4 = CalculateHumidity(num, num2);
				if (num3 <= 8 || num4 >= 6 || !(CalculateOceanShoreDistance(num, num2) > 15f))
				{
					continue;
				}
				int num5 = chunk.CalculateTopmostCellHeight(i, j);
				if (Terrain.ExtractContents(chunk.GetCellValueFast(i, num5, j)) != 18)
				{
					continue;
				}
				float num6 = SimplexNoise.Noise((float)num * 0.04f, (float)num2 * 0.04f);
				int num7 = num5 + (int)(num6 * 3f + 1f);
				for (int num8 = MathUtils.Max(num5, num7); num8 >= 1; num8--)
				{
					int num9 = Terrain.ExtractContents(chunk.GetCellValueFast(i, num8, j));
					if (num8 > num7)
					{
						if (num9 == 18)
						{
							chunk.SetCellValueFast(i, num8, j, 0);
						}
					}
					else
					{
						if (num9 != 18 && (num9 != 0 || num8 <= num5))
						{
							break;
						}
						chunk.SetCellValueFast(i, num8, j, Terrain.MakeBlockValue(7));
					}
				}
			}
		}
	}

	private void GenerateMeteoriteCraters(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int num = chunk.Coords.X * 16;
		int num2 = chunk.Coords.Y * 16;
		int num3 = 128;
		int num4 = (int)Math.Floor((float)num / (float)num3);
		int num5 = (int)Math.Floor((float)num2 / (float)num3);
		for (int i = num4 - 4; i <= num4 + 4; i++)
		{
			for (int j = num5 - 4; j <= num5 + 4; j++)
			{
				Random random = new Random(m_seed + i * 52345 + j * 710145);
				if (!(random.Float(0f, 1f) < 0.004f))
				{
					continue;
				}
				int num6 = i * num3 + random.Int(20, num3 - 20);
				int num7 = j * num3 + random.Int(20, num3 - 20);
				if (CalculateOceanShoreDistance(num6, num7) <= 100f)
				{
					continue;
				}
				float num8 = random.Float(25f, 50f);
				float num9 = num8 / 2.2f;
				bool flag = random.Bool(0.6f);
				Vector2 vector = Vector2.Zero;
				float num10 = 0f;
				if (flag)
				{
					float num11 = random.Float(0f, 6.283185f);
					vector = new Vector2((float)Math.Cos(num11), (float)Math.Sin(num11));
					num10 = num8 * random.Float(4f, 7f);
				}
				int num12 = (int)(num8 + num10 + num8 * 3f);
				Vector2 vector2 = new Vector2(num6, num7);
				Vector2 v = vector2 + (flag ? (vector * (num8 * 0.4f)) : Vector2.Zero);
				for (int k = num6 - num12; k <= num6 + num12; k++)
				{
					for (int l = num7 - num12; l <= num7 + num12; l++)
					{
						if (k < num || k >= num + 16 || l < num2 || l >= num2 + 16)
						{
							continue;
						}
						int x = k - num;
						int z = l - num2;
						Vector2 vector3 = new Vector2(k, l);
						float num13 = SimplexNoise.Noise((float)k * 0.05f, (float)l * 0.05f) * (num8 * 0.15f);
						num13 += SimplexNoise.Noise((float)k * 0.15f, (float)l * 0.15f) * 2f;
						Vector2 v2 = vector3 - vector2;
						float num14 = Vector2.Dot(v2, vector);
						float num15 = num8;
						float num16 = num9;
						bool flag2 = false;
						float num17;
						if (flag && num14 > 0f && num14 < num10)
						{
							num17 = MathUtils.Abs(v2.X * (0f - vector.Y) + v2.Y * vector.X);
							float num18 = num14 / num10;
							num15 = num8 * (1f - (float)Math.Pow(num18, 0.7));
							num16 = num9 * (1f - num18);
							if (num17 + num13 < num15)
							{
								flag2 = true;
							}
						}
						else
						{
							num17 = v2.Length();
							if (num17 + num13 < num15)
							{
								flag2 = true;
							}
						}
						int num19 = chunk.CalculateTopmostCellHeight(x, z);
						int num20 = num19;
						while (num20 > 0 && !BlocksManager.Blocks[Terrain.ExtractContents(chunk.GetCellValueFast(x, num20, z))].IsCollidable)
						{
							num20--;
						}
						float num21 = num17 + num13;
						int temperatureFast = chunk.GetTemperatureFast(x, z);
						bool flag3 = chunk.GetHumidityFast(x, z) < 6 && temperatureFast > 8;
						if (flag2)
						{
							float num24;
							if (flag && num14 > 0f && num14 < num10)
							{
								float num22 = MathUtils.Clamp(num17 / num15, 0f, 1f);
								float num23 = 1f - (float)Math.Pow(num22, 2.0);
								num24 = num16 * num23;
							}
							else
							{
								float num25 = MathUtils.Clamp(Vector2.Distance(vector3, v) / num8, 0f, 1f);
								float num26 = 1f - (float)Math.Pow(num25, 2.5);
								num24 = num9 * num26;
							}
							num24 += SimplexNoise.Noise((float)k * 0.4f, (float)l * 0.4f) * 2.5f;
							int num27 = num20 - (int)MathUtils.Max(1f, num24);
							for (int num28 = num19; num28 >= num27 - 1; num28--)
							{
								if (num28 > num27)
								{
									chunk.SetCellValueFast(x, num28, z, 0);
								}
								else if (num28 == num27)
								{
									chunk.SetCellValueFast(x, num28, z, Terrain.MakeBlockValue(353));
								}
								else
								{
									chunk.SetCellValueFast(x, num28, z, Terrain.MakeBlockValue(376));
								}
							}
						}
						else if (num21 < num15 + num8 * 0.5f)
						{
							float num29 = MathUtils.Clamp((num21 - num15) / (num8 * 0.5f), 0f, 1f);
							double num30 = ((num29 < 0.25f) ? (num29 / 0.25f) : (1f - (num29 - 0.25f) / 0.75f));
							float num31 = SimplexNoise.Noise((float)k * 0.08f, (float)l * 0.08f);
							num31 += SimplexNoise.Noise((float)k * 0.16f, (float)l * 0.16f) * 0.5f;
							float num32 = MathUtils.Max(0f, num31 + 0.3f);
							float num33 = (float)Math.Pow(num30 * (double)num32, 1.2);
							float num34 = ((flag && num14 > 0f) ? (1f - MathUtils.Clamp(num14 / num10, 0f, 1f)) : 1f);
							float num35 = num33 * (num9 * 0.7f) * num34;
							float num36 = MathUtils.Clamp(num33 * 3f, 0f, 1f);
							num35 += SimplexNoise.Noise((float)k * 0.4f, (float)l * 0.4f) * 2f * num36;
							if (!(num35 > 0.5f))
							{
								continue;
							}
							int num37 = num20 + (int)num35;
							for (int m = num20 + 1; m <= num37; m++)
							{
								int contents;
								if (m == num37)
								{
									contents = (flag3 ? 7 : 8);
								}
								else
								{
									contents = (flag3 ? 4 : 2);
								}
								chunk.SetCellValueFast(x, m, z, Terrain.MakeBlockValue(contents));
							}
							if (num37 > num20 && random.Float(0f, 1f) < 0.2f)
							{
								int temperatureFast2 = chunk.GetTemperatureFast(x, z);
								int humidityFast = chunk.GetHumidityFast(x, z);
								int num38 = PlantsManager.GenerateRandomPlantValue(random, Terrain.MakeBlockValue(8), temperatureFast2, humidityFast, num37 + 1);
								if (num38 != 0)
								{
									chunk.SetCellValueFast(x, num37 + 1, z, num38);
								}
							}
						}
						else
						{
							if (!(num21 < num15 + num8 * 3f))
							{
								continue;
							}
							float num39 = SimplexNoise.Noise((float)k * 0.1f, (float)l * 0.1f);
							float num40 = 1f - (num21 - (num15 + num8 * 0.5f)) / (num8 * 2.5f);
							if (!(num39 < num40 * 0.35f))
							{
								continue;
							}
							int num41 = Terrain.ExtractContents(chunk.GetCellValueFast(x, num20, z));
							if (num41 == 8 || num41 == 2 || num41 == 7 || num41 == 6 || num41 == 3)
							{
								int num42 = random.Int(1, 3);
								for (int num43 = num20; num43 > num20 - num42; num43--)
								{
									chunk.SetCellValueFast(x, num43, z, Terrain.MakeBlockValue(flag3 ? 353 : 344));
								}
							}
						}
					}
				}
			}
		}
	}

	public void GenerateAnimalBones(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + x * 812 + y * 543 + 1092);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int humidityFast = chunk.GetHumidityFast(i, j);
				int temperatureFast = chunk.GetTemperatureFast(i, j);
				bool flag = humidityFast < 6 && temperatureFast > 8;
				float probability = (flag ? 0.001f : 0.0001f);
				if (!random.Bool(probability))
				{
					continue;
				}
				int num = chunk.CalculateTopmostCellHeight(i, j);
				if (num > 0 && num < 254)
				{
					int num2 = Terrain.ExtractContents(chunk.GetCellContentsFast(i, num, j));
					if ((num2 == 7 || num2 == 8 || num2 == 2 || num2 == 3) && Terrain.ExtractContents(chunk.GetCellContentsFast(i, num + 1, j)) == 0)
					{
						int num3 = random.Int(0, 3);
						int num4 = random.Int(0, 3);
						int num5 = ((!flag) ? random.Int(0, 2) : random.Int(1, 2));
						int data = (num5 << 4) | (num4 << 2) | (num3 & 3);
						int value = Terrain.MakeBlockValue(362, 0, data);
						chunk.SetCellValueFast(i, num + 1, j, value);
					}
				}
			}
		}
	}
}
