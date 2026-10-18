using System;
using System.Collections.Generic;
using Engine;

namespace Game;

public class TerrainContentsGenerator22 : ITerrainContentsGenerator
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
			int value = (int)MathUtils.Floor(x);
			int num = (int)MathUtils.Floor(y);
			int matrix = (int)MathUtils.Ceiling(x);
			int position = (int)MathUtils.Ceiling(y);
			float f = x - (float)value;
			float f2 = y - (float)num;
			float x2 = m_data[value + num * m_sizeX];
			float x3 = m_data[matrix + num * m_sizeX];
			float x4 = m_data[value + position * m_sizeX];
			float x5 = m_data[matrix + position * m_sizeX];
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

	public static Action<TerrainChunk> GenerateMinerals1;

	public static Action<TerrainChunk> GenerateMinerals2;

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

	public int OceanLevel => 64 + m_worldSettings.SeaLevelOffset;

	static TerrainContentsGenerator22()
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
		CreateBrushes();
	}

	public TerrainContentsGenerator22(SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		m_subsystemBottomSuckerBlockBehavior = subsystemTerrain.Project.FindSubsystem<SubsystemBottomSuckerBlockBehavior>(throwOnError: true);
		SubsystemGameInfo subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
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
		TGBiomeScaling = ((m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island) ? 1f : 1.5f) * m_worldSettings.BiomeSize;
		TGShoreFluctuations = MathUtils.Clamp(2f * num, 0f, 150f);
		TGShoreFluctuationsScaling = MathUtils.Clamp(0.04f * num, 0.5f, 3f);
		TGOceanSlope = 0.006f;
		TGOceanSlopeVariation = 0.004f;
		TGIslandsFrequency = 0.01f;
		TGDensityBias = 55f;
		TGHeightBias = 1f;
		TGRiversStrength = 1f;
		TGMountainsStrength = 255f;
		TGMountainRangeFreq = 0.0006f;
		TGMountainsPercentage = 0.15f;
		TGMountainsDetailFreq = 0.001f;
		TGMountainsDetailOctaves = 1;
		TGMountainsDetailPersistence = 1.53f;
		TGHillsPercentage = 0.32f;
		TGHillsStrength = 32f;
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
		Vector2 num = Vector2.Zero;
		float num2 = float.MinValue;
		for (int i = 0; i < 800; i += 5)
		{
			for (int j = 0; j <= 12; j += 4)
			{
				for (int k = 0; k < 2; k++)
				{
					float num3;
					float x;
					if (k == 0)
					{
						num3 = m_oceanCorner.Y + (float)i;
						x = CalculateOceanShoreX(num3) + (float)j;
					}
					else
					{
						x = m_oceanCorner.X + (float)i;
						num3 = CalculateOceanShoreZ(x) + (float)j;
					}
					float num4 = ScoreSpawnPosition(Terrain.ToCell(x), Terrain.ToCell(num3));
					if (num4 > num2)
					{
						num = new Vector2(x, num3);
						num2 = num4;
					}
				}
			}
		}
		return new Vector3(num.X, CalculateHeight(num.X, num.Y), num.Y);
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
		GenerateCaves(chunk);
		GeneratePockets(chunk);
		GenerateMinerals(chunk);
		GenerateMinerals2?.Invoke(chunk);
		GenerateSurface(chunk);
		PropagateFluidsDownwards(chunk);
	}

	public void GenerateChunkContentsPass4(TerrainChunk chunk)
	{
		GenerateGrassAndPlants(chunk);
		GenerateTreesAndLogs(chunk);
		GenerateCacti(chunk);
		GeneratePumpkins(chunk);
		GenerateGraniteBoulders(chunk);
		GenerateKelp(chunk);
		GenerateBushes(chunk);
		GenerateSeagrass(chunk);
		GenerateBottomSuckers(chunk);
		GenerateDesertBoulders(chunk);
		GenerateTraps(chunk);
		GenerateIvy(chunk);
		GenerateGraves(chunk);
		GenerateSnowAndIce(chunk);
		GenerateBedrockAndAir(chunk);
		GenerateBasaltBoulders(chunk);
		UpdateFluidIsTop(chunk);
	}

	public float CalculateOceanShoreDistance(float x, float z)
	{
		if (m_islandSize.HasValue)
		{
			float componentFurnace = CalculateOceanShoreX(z);
			float num = CalculateOceanShoreZ(x);
			float attachedEntity = CalculateOceanShoreX(z + 1000f) + m_islandSize.Value.X;
			float package = CalculateOceanShoreZ(x + 1000f) + m_islandSize.Value.Y;
			return MathUtils.Min(x - componentFurnace, z - num, attachedEntity - x, package - z);
		}
		float movingBlockSet = CalculateOceanShoreX(z);
		float movingEntityData = CalculateOceanShoreZ(x);
		return MathUtils.Min(x - movingBlockSet, z - movingEntityData);
	}

	public float CalculateMountainRangeFactor(float x, float z)
	{
		return SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, TGMountainRangeFreq / TGBiomeScaling, 3, 1.91f, 0.75f, ridged: true);
	}

	public float CalculateHeight(float x, float z)
	{
		float terrain = TGOceanSlope + TGOceanSlopeVariation * MathUtils.PowSign(2f * SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, 0.01f, 1, 2f, 0.5f) - 1f, 0.5f);
		float num = CalculateOceanShoreDistance(x, z);
		float vector = MathUtils.Saturate(2f - 0.05f * MathUtils.Abs(num));
		float x2 = MathUtils.Saturate(MathUtils.Sin(TGIslandsFrequency * num));
		float z2 = MathUtils.Saturate(MathUtils.Saturate((0f - terrain) * num) - 0.85f * x2);
		float array = MathUtils.Saturate(MathUtils.Saturate(0.05f * (0f - num - 10f)) - x2);
		float i = CalculateMountainRangeFactor(x, z);
		float vector2 = (1f - vector) * SimplexNoise.OctavedNoise(x, z, 0.001f / TGBiomeScaling, 2, 2f, 0.5f);
		float x3 = (1f - vector) * SimplexNoise.OctavedNoise(x, z, 0.0017f / TGBiomeScaling, 2, 4f, 0.7f);
		float z3 = (1f - array) * (1f - vector) * Squish(i, 1f - TGHillsPercentage, 1f - TGMountainsPercentage);
		float num2 = (1f - array) * Squish(i, 1f - TGMountainsPercentage, 1f);
		float num3 = 1f * SimplexNoise.OctavedNoise(x, z, TGHillsFrequency, TGHillsOctaves, 1.93f, TGHillsPersistence);
		float amplitudeStep = MathUtils.Lerp(0.75f * TGMountainsDetailPersistence, 1.33f * TGMountainsDetailPersistence, vector2);
		float num4 = 1.5f * SimplexNoise.OctavedNoise(x, z, TGMountainsDetailFreq, TGMountainsDetailOctaves, 1.98f, amplitudeStep) - 0.5f;
		float num5 = MathUtils.Lerp(60f, 30f, MathUtils.Saturate(1f * num2 + 0.5f * z3 + MathUtils.Saturate(1f - num / 30f)));
		float x4 = MathUtils.Lerp(-2f, -4f, MathUtils.Saturate(num2 + 0.5f * z3));
		float num6 = MathUtils.Saturate(1.5f - num5 * MathUtils.Abs(2f * SimplexNoise.OctavedNoise(x + m_riversOffset.X, z + m_riversOffset.Y, 0.001f, 4, 2f, 0.5f) - 1f));
		float num7 = -50f * z2 + TGHeightBias;
		float num8 = MathUtils.Lerp(0f, 8f, vector2);
		float num9 = MathUtils.Lerp(0f, -6f, x3);
		float num10 = TGHillsStrength * z3 * num3;
		float num11 = TGMountainsStrength * num2 * num4;
		float f = TGRiversStrength * num6;
		float num12 = num7 + num8 + num9 + num11 + num10;
		float num13 = MathUtils.Min(MathUtils.Lerp(num12, x4, f), num12);
		return MathUtils.Clamp(64f + num13, 10f, 251f);
	}

	public int CalculateTemperature(float x, float z)
	{
		return MathUtils.Clamp((int)(MathUtils.Saturate(3f * SimplexNoise.OctavedNoise(x + m_temperatureOffset.X, z + m_temperatureOffset.Y, 0.0015f / TGBiomeScaling, 5, 2f, 0.6f) - 1.1f + m_worldSettings.TemperatureOffset / 16f) * 16f), 0, 15);
	}

	public int CalculateHumidity(float x, float z)
	{
		return MathUtils.Clamp((int)(MathUtils.Saturate(3f * SimplexNoise.OctavedNoise(x + m_humidityOffset.X, z + m_humidityOffset.Y, 0.0012f / TGBiomeScaling, 5, 2f, 0.6f) - 0.9f + m_worldSettings.HumidityOffset / 16f) * 16f), 0, 15);
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
		float num3 = CalculateMountainRangeFactor(x, z);
		float num4 = CalculateHeight(x, z);
		float x2 = CalculateHeight(x - 5, z - 5);
		float x3 = CalculateHeight(x - 5, z + 5);
		float x4 = CalculateHeight(x + 5, z - 5);
		float x5 = CalculateHeight(x + 5, z + 5);
		float num5 = MathUtils.Min(num4, MathUtils.Max(x2, x3, x4, x5));
		float num6 = MathUtils.Max(num4, MathUtils.Max(x2, x3, x4, x5));
		float num7 = num6 - num5;
		float num8 = 0f;
		if (MathUtils.Max(MathUtils.Abs(x), MathUtils.Abs(z)) > 400)
		{
			num8 -= 0.25f;
		}
		if (num4 < 65f)
		{
			num8--;
		}
		if (num4 < 66f)
		{
			num8 -= 0.5f;
		}
		if (num3 > 0.9f)
		{
			num8--;
		}
		if (num3 > 0.85f)
		{
			num8 -= 0.5f;
		}
		if (num3 > 0.8f)
		{
			num8 -= 0.25f;
		}
		switch (m_subsystemTerrain.SubsystemGameInfo.WorldSettings.StartingPositionMode)
		{
		case StartingPositionMode.Easy:
			if (num < 9)
			{
				num8 -= 0.5f;
			}
			if (num < 7)
			{
				num8--;
			}
			if (num2 > 2 && num2 < 10)
			{
				num8 -= 0.5f;
			}
			if (num6 > 75f)
			{
				num8--;
			}
			if (num7 > 5f)
			{
				num8--;
			}
			break;
		case StartingPositionMode.Medium:
			if (num < 3)
			{
				num8 -= 0.5f;
			}
			if (num > 6)
			{
				num8--;
			}
			if (num2 > 3 && num2 < 8)
			{
				num8 -= 0.5f;
			}
			if (num2 > 10)
			{
				num8 -= 0.5f;
			}
			if (num6 > 80f)
			{
				num8--;
			}
			if (num7 > 10f)
			{
				num8--;
			}
			break;
		default:
			if (num > 0)
			{
				num8 -= 0.5f;
			}
			if (num > 2)
			{
				num8--;
			}
			if (num2 > 6)
			{
				num8 -= 0.5f;
			}
			if (num2 > 8)
			{
				num8--;
			}
			if (num6 > 85f)
			{
				num8--;
			}
			if (num7 > 15f)
			{
				num8--;
			}
			if (num7 < 5f)
			{
				num8 -= 0.5f;
			}
			break;
		}
		return num8;
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
		int furnitureDesign = x2 - x1;
		int num = z2 - z1;
		int num2 = chunk.Origin.X + x1;
		int num3 = chunk.Origin.Y + z1;
		Grid2d grid2d = new Grid2d(furnitureDesign, num);
		Grid2d grid2d2 = new Grid2d(furnitureDesign, num);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < furnitureDesign; j++)
			{
				grid2d.Set(j, i, CalculateOceanShoreDistance(j + num2, i + num3));
				grid2d2.Set(j, i, CalculateMountainRangeFactor(j + num2, i + num3));
			}
		}
		Grid3d grid3d = new Grid3d(furnitureDesign / 4 + 1, 33, num / 4 + 1);
		for (int k = 0; k < grid3d.SizeX; k++)
		{
			for (int l = 0; l < grid3d.SizeZ; l++)
			{
				int num4 = k * 4 + num2;
				int num5 = l * 4 + num3;
				float num6 = CalculateHeight(num4, num5);
				float v = CalculateMountainRangeFactor(num4, num5);
				float num7 = MathUtils.Lerp(TGMinTurbulence, 1f, Squish(v, TGTurbulenceZero, 1f));
				for (int m = 0; m < grid3d.SizeY; m++)
				{
					int num8 = m * 8;
					float num9 = TGTurbulenceStrength * num7 * MathUtils.Saturate(num6 - (float)num8) * (2f * SimplexNoise.OctavedNoise(num4, num8, num5, TGTurbulenceFreq, TGTurbulenceOctaves, 4f, TGTurbulencePersistence) - 1f);
					float num10 = (float)num8 + num9;
					float num11 = num6 - num10;
					num11 += MathUtils.Max(4f * (TGDensityBias - (float)num8), 0f);
					grid3d.Set(k, m, l, num11);
				}
			}
		}
		int oceanLevel = OceanLevel;
		for (int n = 0; n < grid3d.SizeX - 1; n++)
		{
			for (int num12 = 0; num12 < grid3d.SizeZ - 1; num12++)
			{
				for (int num13 = 0; num13 < grid3d.SizeY - 1; num13++)
				{
					grid3d.Get8(n, num13, num12, out var v2, out var v3, out var v4, out var v5, out var v6, out var v7, out var v8, out var v9);
					float num14 = (v3 - v2) / 4f;
					float num15 = (v5 - v4) / 4f;
					float num16 = (v7 - v6) / 4f;
					float num17 = (v9 - v8) / 4f;
					float num18 = v2;
					float num19 = v4;
					float num20 = v6;
					float num21 = v8;
					for (int num22 = 0; num22 < 4; num22++)
					{
						float num23 = (num20 - num18) / 4f;
						float num24 = (num21 - num19) / 4f;
						float num25 = num18;
						float num26 = num19;
						for (int num27 = 0; num27 < 4; num27++)
						{
							float num28 = (num26 - num25) / 8f;
							float num29 = num25;
							int num30 = num22 + n * 4;
							int num31 = num27 + num12 * 4;
							int x3 = x1 + num30;
							int z3 = z1 + num31;
							float x4 = grid2d.Get(num30, num31);
							float num32 = grid2d2.Get(num30, num31);
							int temperatureFast = chunk.GetTemperatureFast(x3, z3);
							int humidityFast = chunk.GetHumidityFast(x3, z3);
							float f = num32 - 0.01f * (float)humidityFast;
							float num33 = MathUtils.Lerp(100f, 0f, f);
							float num34 = MathUtils.Lerp(300f, 30f, f);
							bool flag = (temperatureFast > 8 && humidityFast < 8 && num32 < 0.97f) || (MathUtils.Abs(x4) < 16f && num32 < 0.97f);
							int num35 = TerrainChunk.CalculateCellIndex(x3, 0, z3);
							for (int num36 = 0; num36 < 8; num36++)
							{
								int num37 = num36 + num13 * 8;
								int value = 0;
								if (num29 < 0f)
								{
									if (num37 <= oceanLevel)
									{
										value = 18;
									}
								}
								else if (flag)
								{
									if (num29 < num33)
									{
										value = 4;
									}
									else
									{
										value = ((!(num29 < num34)) ? 67 : 3);
									}
								}
								else
								{
									value = ((!(num29 < num34)) ? 67 : 3);
								}
								chunk.SetCellValueFast(num35 + num37, value);
								num29 += num28;
							}
							num25 += num23;
							num26 += num24;
						}
						num18 += num14;
						num19 += num15;
						num20 += num16;
						num21 += num17;
					}
				}
			}
		}
	}

	public void GenerateSurface(TerrainChunk chunk)
	{
		Terrain i = m_subsystemTerrain.Terrain;
		Random j = new Random(m_seed + chunk.Coords.X + 101 * chunk.Coords.Y);
		for (int k = 0; k < 16; k++)
		{
			for (int l = 0; l < 16; l++)
			{
				int num = k + chunk.Origin.X;
				int num2 = l + chunk.Origin.Y;
				int num3 = TerrainChunk.CalculateCellIndex(k, 254, l);
				int num4 = 254;
				while (num4 >= 0)
				{
					int num5 = Terrain.ExtractContents(chunk.GetCellValueFast(num3));
					if (!BlocksManager.Blocks[num5].IsTransparent)
					{
						float num6 = CalculateMountainRangeFactor(num, num2);
						int temperature = i.GetTemperature(num, num2);
						int humidity = i.GetHumidity(num, num2);
						int num7;
						if (num5 == 4)
						{
							num7 = ((temperature > 4 && temperature < 7) ? 6 : 7);
						}
						else
						{
							int num8 = temperature / 4;
							int num9 = ((num4 + 1 < 255) ? chunk.GetCellContentsFast(k, num4 + 1, l) : 0);
							if (num4 > 120 && SubsystemWeather.IsPlaceFrozen(temperature, num4))
							{
								num7 = 62;
							}
							else if ((num4 < 66 || num4 == 84 + num8 || num4 == 103 + num8) && humidity == 9 && temperature % 6 == 1)
							{
								num7 = 66;
							}
							else
							{
								num7 = ((num9 != 18 || humidity <= 8 || humidity % 2 != 0 || temperature % 3 != 0) ? 2 : 72);
							}
						}
						int num10;
						if (num7 == 62)
						{
							num10 = (int)MathUtils.Clamp(1f * (float)(-temperature), 1f, 7f);
						}
						else
						{
							float num11 = MathUtils.Saturate(((float)num4 - 100f) * 0.05f);
							float f = MathUtils.Saturate(MathUtils.Saturate((num6 - 0.9f) / 0.1f) - MathUtils.Saturate(((float)humidity - 3f) / 12f) + TGSurfaceMultiplier * num11);
							int min = (int)MathUtils.Lerp(4f, 0f, f);
							int max = (int)MathUtils.Lerp(7f, 0f, f);
							num10 = MathUtils.Min(j.Int(min, max), num4);
						}
						int num12 = TerrainChunk.CalculateCellIndex(k, num4 + 1, l);
						for (int m = num12 - num10; m < num12; m++)
						{
							if (Terrain.ExtractContents(chunk.GetCellValueFast(m)) != 0)
							{
								int value = Terrain.ReplaceContents(0, num7);
								chunk.SetCellValueFast(m, value);
							}
						}
						break;
					}
					num4--;
					num3--;
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
		if (GenerateMinerals1 != null)
		{
			GenerateMinerals1(chunk);
			return;
		}
		int furnitureDesign = chunk.Coords.X;
		int list = chunk.Coords.Y;
		for (int num = furnitureDesign - 1; num <= furnitureDesign + 1; num++)
		{
			for (int i = list - 1; i <= list + 1; i++)
			{
				Random random = new Random(m_seed + num + 119 * i);
				int num2 = random.Int(0, 10);
				for (int j = 0; j < num2; j++)
				{
					random.Int(0, 1);
				}
				float num3 = CalculateMountainRangeFactor(num * 16, i * 16);
				int num4 = (int)(5f + 3f * num3 * SimplexNoise.OctavedNoise(num, i, 0.33f, 1, 1f, 1f));
				for (int k = 0; k < num4; k++)
				{
					int x = num * 16 + random.Int(0, 15);
					int y = random.Int(5, 200);
					int z = i * 16 + random.Int(0, 15);
					m_coalBrushes[random.Int(0, m_coalBrushes.Count - 1)].PaintFastSelective(chunk, x, y, z, 3);
				}
				int num5 = (int)(6f + 2f * num3 * SimplexNoise.OctavedNoise(num + 1211, i + 396, 0.33f, 1, 1f, 1f));
				for (int l = 0; l < num5; l++)
				{
					int x2 = num * 16 + random.Int(0, 15);
					int y2 = random.Int(20, 65);
					int z2 = i * 16 + random.Int(0, 15);
					m_copperBrushes[random.Int(0, m_copperBrushes.Count - 1)].PaintFastSelective(chunk, x2, y2, z2, 3);
				}
				int num6 = (int)(5f + 2f * num3 * SimplexNoise.OctavedNoise(num + 713, i + 211, 0.33f, 1, 1f, 1f));
				for (int m = 0; m < num6; m++)
				{
					int x3 = num * 16 + random.Int(0, 15);
					int y3 = random.Int(2, 40);
					int z3 = i * 16 + random.Int(0, 15);
					m_ironBrushes[random.Int(0, m_ironBrushes.Count - 1)].PaintFastSelective(chunk, x3, y3, z3, 67);
				}
				int num7 = (int)(3f + 3f * num3 * SimplexNoise.OctavedNoise(num + 915, i + 272, 0.33f, 1, 1f, 1f));
				for (int n = 0; n < num7; n++)
				{
					int x4 = num * 16 + random.Int(0, 15);
					int y4 = random.Int(50, 90);
					int z4 = i * 16 + random.Int(0, 15);
					m_saltpeterBrushes[random.Int(0, m_saltpeterBrushes.Count - 1)].PaintFastSelective(chunk, x4, y4, z4, 4);
				}
				int num8 = (int)(3f + 2f * num3 * SimplexNoise.OctavedNoise(num + 711, i + 1194, 0.33f, 1, 1f, 1f));
				for (int num9 = 0; num9 < num8; num9++)
				{
					int x5 = num * 16 + random.Int(0, 15);
					int y5 = random.Int(2, 40);
					int z5 = i * 16 + random.Int(0, 15);
					m_sulphurBrushes[random.Int(0, m_sulphurBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z5, 67);
				}
				int num10 = (int)(0.5f + 2f * num3 * SimplexNoise.OctavedNoise(num + 432, i + 907, 0.33f, 1, 1f, 1f));
				for (int num11 = 0; num11 < num10; num11++)
				{
					int x6 = num * 16 + random.Int(0, 15);
					int y6 = random.Int(2, 15);
					int z6 = i * 16 + random.Int(0, 15);
					m_diamondBrushes[random.Int(0, m_diamondBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z6, 67);
				}
				int num12 = (int)(3f + 2f * num3 * SimplexNoise.OctavedNoise(num + 799, i + 131, 0.33f, 1, 1f, 1f));
				for (int num13 = 0; num13 < num12; num13++)
				{
					int x7 = num * 16 + random.Int(0, 15);
					int y7 = random.Int(2, 50);
					int z7 = i * 16 + random.Int(0, 15);
					m_germaniumBrushes[random.Int(0, m_germaniumBrushes.Count - 1)].PaintFastSelective(chunk, x7, y7, z7, 67);
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
			for (int furnitureDesign = -1; furnitureDesign <= 1; furnitureDesign++)
			{
				int point = i + chunk.Coords.X;
				int point2 = furnitureDesign + chunk.Coords.Y;
				Random num = new Random(m_seed + point + 71 * point2);
				int dialog = num.Int(0, 10);
				for (int stack = 0; stack < dialog; stack++)
				{
					num.Int(0, 1);
				}
				float designIndex = CalculateMountainRangeFactor(point * 16, point2 * 16);
				for (int j = 0; j < 3; j++)
				{
					int num2 = point * 16 + num.Int(0, 15);
					int array = num.Int(50, 100);
					int steps = point2 * 16 + num.Int(0, 15);
					TerrainBrush terrainBrush = m_dirtPocketBrushes[num.Int(0, m_dirtPocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush, num);
					terrainBrush.PaintFastSelective(chunk, num2, array, steps, 3);
				}
				for (int location = 0; location < 10; location++)
				{
					int point5 = point * 16 + num.Int(0, 15);
					int delta = num.Int(20, 80);
					int key = point2 * 16 + num.Int(0, 15);
					TerrainBrush terrainBrush2 = m_gravelPocketBrushes[num.Int(0, m_gravelPocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush2, num);
					terrainBrush2.PaintFastSelective(chunk, point5, delta, key, 3);
				}
				for (int cellValue = 0; cellValue < 6; cellValue++)
				{
					int x = point * 16 + num.Int(0, 15);
					int item = num.Int(40, 80);
					int point6 = point2 * 16 + num.Int(0, 15);
					TerrainBrush terrainBrush3 = m_sandPocketBrushes[num.Int(0, m_sandPocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush3, num);
					terrainBrush3.PaintFastSelective(chunk, x, item, point6, 4);
				}
				for (int k = 0; k < 6; k++)
				{
					int x2 = point * 16 + num.Int(0, 15);
					int y = num.Int(4, 50);
					int z = point2 * 16 + num.Int(0, 15);
					TerrainBrush terrainBrush4 = m_granitePocketBrushes[num.Int(0, m_granitePocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush4, num);
					terrainBrush4.PaintFastSelective(chunk, x2, y, z, 67);
				}
				for (int l = 0; l < 2; l++)
				{
					int x3 = point * 16 + num.Int(0, 15);
					int y2 = num.Int(20, 120);
					int z2 = point2 * 16 + num.Int(0, 15);
					m_limestonePocketBrushes[num.Int(0, m_limestonePocketBrushes.Count - 1)].PaintFastSelective(chunk, x3, y2, z2, 3);
				}
				for (int m = 0; m < 1; m++)
				{
					int x4 = point * 16 + num.Int(0, 15);
					int y3 = num.Int(50, 70);
					int z3 = point2 * 16 + num.Int(0, 15);
					m_clayPocketBrushes[num.Int(0, m_clayPocketBrushes.Count - 1)].PaintFastSelective(chunk, x4, y3, z3, 3);
				}
				for (int n = 0; n < 4; n++)
				{
					int x5 = point * 16 + num.Int(0, 15);
					int y4 = num.Int(40, 60);
					int z4 = point2 * 16 + num.Int(0, 15);
					m_basaltPocketBrushes[num.Int(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x5, y4, z4, 4);
				}
				for (int num3 = 0; num3 < 3; num3++)
				{
					int x6 = point * 16 + num.Int(0, 15);
					int y5 = num.Int(20, 40);
					int z5 = point2 * 16 + num.Int(0, 15);
					m_basaltPocketBrushes[num.Int(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x6, y5, z5, 3);
				}
				if (num.Bool(0.02f + 0.01f * designIndex))
				{
					int num4 = point * 16;
					int num5 = num.Int(40, 60);
					int num6 = point2 * 16;
					int num7 = num.Int(1, 3);
					for (int num8 = 0; num8 < num7; num8++)
					{
						Vector2 vector = num.Vector2(7f);
						int num9 = 8 + (int)MathUtils.Round(vector.X);
						int num10 = 0;
						int num11 = 8 + (int)MathUtils.Round(vector.Y);
						m_waterPocketBrushes[num.Int(0, m_waterPocketBrushes.Count - 1)].PaintFast(chunk, num4 + num9, num5 + num10, num6 + num11);
					}
				}
				if (num.Bool(0.06f + 0.05f * designIndex))
				{
					int num12 = point * 16;
					int num13 = num.Int(15, 42);
					int num14 = point2 * 16;
					int num15 = num.Int(1, 2);
					for (int num16 = 0; num16 < num15; num16++)
					{
						Vector2 vector2 = num.Vector2(7f);
						int num17 = 8 + (int)MathUtils.Round(vector2.X);
						int num18 = num.Int(0, 1);
						int num19 = 8 + (int)MathUtils.Round(vector2.Y);
						m_magmaPocketBrushes[num.Int(0, m_magmaPocketBrushes.Count - 1)].PaintFast(chunk, num12 + num17, num13 + num18, num14 + num19);
					}
				}
			}
		}
	}

	public void GenerateCaves(TerrainChunk chunk)
	{
		if (!TGCavesAndPockets)
		{
			return;
		}
		List<CavePoint> list = new List<CavePoint>();
		int num = chunk.Coords.X;
		int action = chunk.Coords.Y;
		for (int i = num - 2; i <= num + 2; i++)
		{
			for (int item = action - 2; item <= action + 2; item++)
			{
				list.Clear();
				Random num2 = new Random(m_seed + i + 9973 * item);
				int num3 = i * 16 + num2.Int(0, 15);
				int num4 = item * 16 + num2.Int(0, 15);
				float probability = 0.5f;
				if (!num2.Bool(probability))
				{
					continue;
				}
				int num5 = (int)CalculateHeight(num3, num4);
				int num6 = (int)CalculateHeight(num3 + 3, num4);
				int num7 = (int)CalculateHeight(num3, num4 + 3);
				Vector3 position = new Vector3(num3, num5 - 1, num4);
				Vector3 v = new Vector3(3f, num6 - num5, 0f);
				Vector3 v2 = new Vector3(0f, num7 - num5, 3f);
				Vector3 direction = Vector3.Normalize(Vector3.Cross(v, v2));
				if (direction.Y > -0.6f)
				{
					list.Add(new CavePoint
					{
						Position = position,
						Direction = direction,
						BrushType = 0,
						Length = num2.Int(80, 240)
					});
				}
				int num8 = i * 16 + 8;
				int num9 = item * 16 + 8;
				int num10 = 0;
				while (num10 < list.Count)
				{
					CavePoint cavePoint = list[num10];
					List<TerrainBrush> list2 = m_caveBrushesByType[cavePoint.BrushType];
					list2[num2.Int(0, list2.Count - 1)].PaintFastAvoidWater(chunk, Terrain.ToCell(cavePoint.Position.X), Terrain.ToCell(cavePoint.Position.Y), Terrain.ToCell(cavePoint.Position.Z));
					cavePoint.Position += 2f * cavePoint.Direction;
					cavePoint.StepsTaken += 2;
					float num11 = cavePoint.Position.X - (float)num8;
					float num12 = cavePoint.Position.Z - (float)num9;
					if (num2.Bool(0.5f))
					{
						Vector3 vector = Vector3.Normalize(num2.Vector3(1f));
						if ((num11 < -25.5f && vector.X < 0f) || (num11 > 25.5f && vector.X > 0f))
						{
							vector.X = 0f - vector.X;
						}
						if ((num12 < -25.5f && vector.Z < 0f) || (num12 > 25.5f && vector.Z > 0f))
						{
							vector.Z = 0f - vector.Z;
						}
						if ((cavePoint.Direction.Y < -0.5f && vector.Y < -10f) || (cavePoint.Direction.Y > 0.1f && vector.Y > 0f))
						{
							vector.Y = 0f - vector.Y;
						}
						cavePoint.Direction = Vector3.Normalize(cavePoint.Direction + 0.5f * vector);
					}
					if (cavePoint.StepsTaken > 20 && num2.Bool(0.06f))
					{
						cavePoint.Direction = Vector3.Normalize(num2.Vector3(1f) * new Vector3(1f, 0.33f, 1f));
					}
					if (cavePoint.StepsTaken > 20 && num2.Bool(0.05f))
					{
						cavePoint.Direction.Y = 0f;
						cavePoint.BrushType = MathUtils.Min(cavePoint.BrushType + 2, m_caveBrushesByType.Count - 1);
					}
					if (cavePoint.StepsTaken > 30 && num2.Bool(0.03f))
					{
						cavePoint.Direction.X = 0f;
						cavePoint.Direction.Y = -1f;
						cavePoint.Direction.Z = 0f;
					}
					if (cavePoint.StepsTaken > 30 && cavePoint.Position.Y < 30f && num2.Bool(0.02f))
					{
						cavePoint.Direction.X = 0f;
						cavePoint.Direction.Y = 1f;
						cavePoint.Direction.Z = 0f;
					}
					if (num2.Bool(0.33f))
					{
						cavePoint.BrushType = (int)(MathUtils.Pow(num2.Float(0f, 0.999f), 7f) * (float)m_caveBrushesByType.Count);
					}
					if (num2.Bool(0.06f) && list.Count < 12 && cavePoint.StepsTaken > 20 && cavePoint.Position.Y < 58f)
					{
						list.Add(new CavePoint
						{
							Position = cavePoint.Position,
							Direction = Vector3.Normalize(num2.Vector3(1f, 1f) * new Vector3(1f, 0.33f, 1f)),
							BrushType = (int)(MathUtils.Pow(num2.Float(0f, 0.999f), 7f) * (float)m_caveBrushesByType.Count),
							Length = num2.Int(40, 180)
						});
					}
					if (cavePoint.StepsTaken >= cavePoint.Length || MathUtils.Abs(num11) > 34f || MathUtils.Abs(num12) > 34f || cavePoint.Position.Y < 5f || cavePoint.Position.Y > 246f)
					{
						num10++;
					}
					else if (cavePoint.StepsTaken % 20 == 0)
					{
						float num13 = CalculateHeight(cavePoint.Position.X, cavePoint.Position.Z);
						if (cavePoint.Position.Y > num13 + 1f)
						{
							num10++;
						}
					}
				}
			}
		}
	}

	public void GenerateTreesAndLogs(TerrainChunk chunk)
	{
		if (!TGExtras)
		{
			return;
		}
		Terrain hashSet = m_subsystemTerrain.Terrain;
		int list = chunk.Origin.X;
		int num = list + 16;
		int item = chunk.Origin.Y;
		int cellValue = item + 16;
		int data = chunk.Coords.X;
		int designIndex = chunk.Coords.Y;
		for (int design = data; design <= data; design++)
		{
			for (int i = designIndex; i <= designIndex; i++)
			{
				Random value = new Random(m_seed + design + 3943 * i);
				int humidity = CalculateHumidity(design * 16, i * 16);
				int num2 = CalculateTemperature(design * 16, i * 16);
				float num3 = MathUtils.Saturate((SimplexNoise.OctavedNoise(design, i, 0.1f, 2, 2f, 0.5f) - 0.25f) / 0.2f + (value.Bool(0.25f) ? 0.5f : 0f));
				int num4 = 0;
				if (num3 > 0.95f)
				{
					num4 = 1 + (value.Bool(0.25f) ? 1 : 0);
				}
				else if (num3 > 0.5f)
				{
					num4 = (value.Bool(0.25f) ? 1 : 0);
				}
				int num5 = 0;
				for (int j = 0; j < 8; j++)
				{
					if (num5 >= num4)
					{
						break;
					}
					int num6 = design * 16 + value.Int(0, 15);
					int num7 = i * 16 + value.Int(0, 15);
					int num8 = hashSet.CalculateTopmostCellHeight(num6, num7);
					if (num8 < 66)
					{
						continue;
					}
					int cellContentsFast = hashSet.GetCellContentsFast(num6, num8, num7);
					if (cellContentsFast != 2 && cellContentsFast != 8)
					{
						continue;
					}
					num8++;
					int num9 = value.Int(3, 7);
					Point3 point = CellFace.FaceToPoint3(value.Int(0, 3));
					if (point.X < 0 && num6 - num9 + 1 < 0)
					{
						point.X *= -1;
					}
					if (point.X > 0 && num6 + num9 - 1 > 15)
					{
						point.X *= -1;
					}
					if (point.Z < 0 && num7 - num9 + 1 < 0)
					{
						point.Z *= -1;
					}
					if (point.Z > 0 && num7 + num9 - 1 > 15)
					{
						point.Z *= -1;
					}
					bool flag = true;
					bool flag2 = false;
					bool flag3 = false;
					for (int k = 0; k < num9; k++)
					{
						int num10 = num6 + point.X * k;
						int num11 = num7 + point.Z * k;
						if (num10 < list + 1 || num10 >= num - 1 || num11 < item + 1 || num11 >= cellValue - 1)
						{
							flag = false;
							break;
						}
						if (BlocksManager.Blocks[hashSet.GetCellContentsFast(num10, num8, num11)].IsCollidable)
						{
							flag = false;
							break;
						}
						if (BlocksManager.Blocks[hashSet.GetCellContentsFast(num10, num8 - 1, num11)].IsCollidable)
						{
							if (k <= MathUtils.Max(num9 / 2, 0))
							{
								flag2 = true;
							}
							if (k >= MathUtils.Min(num9 / 2 + 1, num9 - 1))
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
					TreeType? treeType = PlantsManager.GenerateRandomTreeType(value, num2 + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num8), humidity, num8);
					if (treeType.HasValue)
					{
						int treeTrunkValue = PlantsManager.GetTreeTrunkValue(treeType.Value);
						treeTrunkValue = Terrain.ReplaceData(treeTrunkValue, WoodBlock.SetCutFace(Terrain.ExtractData(treeTrunkValue), (point.X != 0) ? 1 : 0));
						int treeLeavesValue = PlantsManager.GetTreeLeavesValue(treeType.Value);
						for (int l = 0; l < num9; l++)
						{
							int num12 = num6 + point.X * l;
							int num13 = num7 + point.Z * l;
							hashSet.SetCellValueFast(num12, num8, num13, treeTrunkValue);
							if (l > num9 / 2)
							{
								if (value.Bool(0.3f) && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num12 + point2.X, num8, num13 + point2.Z)].IsCollidable)
								{
									hashSet.SetCellValueFast(num12 + point2.X, num8, num13 + point2.Z, treeLeavesValue);
								}
								if (value.Bool(0.05f) && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num12 + point2.X, num8, num13 + point2.Z)].IsCollidable)
								{
									hashSet.SetCellValueFast(num12 + point2.X, num8, num13 + point2.Z, treeTrunkValue);
								}
								if (value.Bool(0.3f) && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num12 - point2.X, num8, num13 - point2.Z)].IsCollidable)
								{
									hashSet.SetCellValueFast(num12 - point2.X, num8, num13 - point2.Z, treeLeavesValue);
								}
								if (value.Bool(0.05f) && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num12 - point2.X, num8, num13 - point2.Z)].IsCollidable)
								{
									hashSet.SetCellValueFast(num12 - point2.X, num8, num13 - point2.Z, treeTrunkValue);
								}
								if (value.Bool(0.1f) && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num12, num8 + 1, num13)].IsCollidable)
								{
									hashSet.SetCellValueFast(num12, num8 + 1, num13, treeLeavesValue);
								}
							}
						}
					}
					num5++;
				}
				int num14 = (int)(5f * num3);
				int num15 = 0;
				for (int m = 0; m < 32; m++)
				{
					if (num15 >= num14)
					{
						break;
					}
					int num16 = design * 16 + value.Int(2, 13);
					int num17 = i * 16 + value.Int(2, 13);
					int num18 = hashSet.CalculateTopmostCellHeight(num16, num17);
					if (num18 < 66)
					{
						continue;
					}
					int cellContentsFast2 = hashSet.GetCellContentsFast(num16, num18, num17);
					if (cellContentsFast2 != 2 && cellContentsFast2 != 8)
					{
						continue;
					}
					num18++;
					if (!BlocksManager.Blocks[hashSet.GetCellContentsFast(num16 + 1, num18, num17)].IsCollidable && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num16 - 1, num18, num17)].IsCollidable && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num16, num18, num17 + 1)].IsCollidable && !BlocksManager.Blocks[hashSet.GetCellContentsFast(num16, num18, num17 - 1)].IsCollidable)
					{
						TreeType? treeType2 = PlantsManager.GenerateRandomTreeType(value, num2 + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num18), humidity, num18);
						if (treeType2.HasValue)
						{
							ReadOnlyList<TerrainBrush> treeBrushes = PlantsManager.GetTreeBrushes(treeType2.Value);
							treeBrushes[value.Int(treeBrushes.Count)].PaintFast(chunk, num16, num18, num17);
						}
						num15++;
					}
				}
				int num19 = list + 16;
				int num20 = item;
				int num21 = item + 16;
				for (int n = list; n < num19; n++)
				{
					for (int num22 = num20; num22 < num21; num22++)
					{
						int num23 = hashSet.CalculateTopmostCellHeight(n, num22);
						int cellContentsFast3 = hashSet.GetCellContentsFast(n, num23, num22);
						if (num23 >= 80 && num23 <= 240 && (cellContentsFast3 == 3 || cellContentsFast3 == 62 || cellContentsFast3 == 8) && !BlocksManager.Blocks[hashSet.GetCellContentsFast(n, num23 + 1, num22)].IsCollidable && value.Float(0f, 1f) < 0.2f)
						{
							ReadOnlyList<TerrainBrush> treeBrushes2 = PlantsManager.GetTreeBrushes(TreeType.Spruce);
							treeBrushes2[value.Int(treeBrushes2.Count)].PaintFast(chunk, n, num23 + 1, num22);
						}
					}
				}
			}
		}
	}

	public void GenerateBedrockAndAir(TerrainChunk chunk)
	{
		int value = Terrain.MakeBlockValue(1);
		int value2 = Terrain.MakeBlockValue(344);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = i + chunk.Origin.X;
				int num2 = j + chunk.Origin.Y;
				float num3 = 2 + (int)(2f * SimplexNoise.OctavedNoise(num, num2, 0.1f, 1, 1f, 1f));
				for (int k = 0; (float)k < num3; k++)
				{
					chunk.SetCellValueFast(i, k, j, value);
				}
				float num4 = 2 + (int)(5f * SimplexNoise.OctavedNoise(num, num2, 0.1f, 1, 1f, 1f));
				for (int l = 0; (float)l < num4; l++)
				{
					chunk.SetCellValueFast(i, l + (int)num3, j, value2);
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
		Random random = new Random(m_seed + chunk.Coords.X + 3943 * chunk.Coords.Y);
		for (int num = 0; num < 16; num++)
		{
			for (int furnitureSet = 0; furnitureSet < 16; furnitureSet++)
			{
				for (int num2 = 254; num2 >= 0; num2--)
				{
					int cellValueFast = chunk.GetCellValueFast(num, num2, furnitureSet);
					int num3 = Terrain.ExtractContents(cellValueFast);
					if (num3 != 0)
					{
						if (!(BlocksManager.Blocks[num3] is FluidBlock))
						{
							int temperatureFast = chunk.GetTemperatureFast(num, furnitureSet);
							int humidityFast = chunk.GetHumidityFast(num, furnitureSet);
							int num4 = PlantsManager.GenerateRandomPlantValue(random, cellValueFast, temperatureFast, humidityFast, num2 + 1);
							if (num4 != 0)
							{
								chunk.SetCellValueFast(num, num2 + 1, furnitureSet, num4);
							}
							if (num3 == 2)
							{
								chunk.SetCellValueFast(num, num2, furnitureSet, Terrain.MakeBlockValue(8, 0, 0));
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
		int num = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + num + 1991 * y);
		if (!random.Bool(0.5f))
		{
			return;
		}
		int num2 = random.Int(0, MathUtils.Max(1, 1));
		for (int i = 0; i < num2; i++)
		{
			int num3 = random.Int(3, 12);
			int num4 = random.Int(3, 12);
			int humidityFast = chunk.GetHumidityFast(num3, num4);
			int temperatureFast = chunk.GetTemperatureFast(num3, num4);
			if (humidityFast >= 6 || temperatureFast <= 8)
			{
				continue;
			}
			for (int j = 0; j < 8; j++)
			{
				int num5 = num3 + random.Int(-2, 2);
				int num6 = num4 + random.Int(-2, 2);
				for (int num7 = 251; num7 >= 0; num7--)
				{
					switch (Terrain.ExtractContents(chunk.GetCellValueFast(num5, num7, num6)))
					{
					case 7:
					{
						for (int k = num7 + 1; k <= num7 + 3 && chunk.GetCellContentsFast(num5 + 1, k, num6) == 0 && chunk.GetCellContentsFast(num5 - 1, k, num6) == 0 && chunk.GetCellContentsFast(num5, k, num6 + 1) == 0 && chunk.GetCellContentsFast(num5, k, num6 - 1) == 0; k++)
						{
							chunk.SetCellValueFast(num5, k, num6, Terrain.MakeBlockValue(127));
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
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(0);
		float num = 0f;
		for (int i = 0; i < 9; i++)
		{
			int num2 = i % 3 - 1;
			int num3 = i / 3 - 1;
			random.Seed(m_seed + x + num2 + 850 * (y + num3));
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
		random.Seed(m_seed + x + 850 * y);
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
				int x2 = num5 + random.Int(-2, 2);
				int z = num6 + random.Int(-2, 2);
				int num11 = 0;
				for (int num12 = 254; num12 >= 0; num12--)
				{
					int num13 = Terrain.ExtractContents(chunk.GetCellValueFast(x2, num12, z));
					Block block = BlocksManager.Blocks[num13];
					if (num13 != 0)
					{
						if (!(block is WaterBlock))
						{
							if ((num13 == 2 || num13 == 7 || num13 == 72) && num11 >= 2)
							{
								int num14 = (flag ? random.Int(num11 - 2, num11 - 1) : random.Int(num11 - 1, num11));
								for (int l = 0; l < num14; l++)
								{
									chunk.SetCellValueFast(x2, num12 + 1 + l, z, Terrain.MakeBlockValue(232));
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
		int list = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random item2 = new Random(m_seed + list + 378 * y);
		for (int valuesDictionary2 = 0; valuesDictionary2 < 6; valuesDictionary2++)
		{
			int item3 = item2.Int(1, 14);
			int num = item2.Int(1, 14);
			int num2 = chunk.Origin.X + item3;
			int num3 = chunk.Origin.Y + num;
			bool flag = CalculateOceanShoreDistance(num2, num3) < 10f;
			int num4 = item2.Int(1, 3);
			for (int i = 0; i < num4; i++)
			{
				int x = item3 + item2.Int(-1, 1);
				int z = num + item2.Int(-1, 1);
				int num5 = 0;
				for (int num6 = 254; num6 >= 0; num6--)
				{
					int num7 = Terrain.ExtractContents(chunk.GetCellValueFast(x, num6, z));
					switch (num7)
					{
					case 18:
						num5++;
						if (num5 <= 16)
						{
							continue;
						}
						break;
					default:
						if (num5 > 1 && (num7 == 2 || num7 == 7 || num7 == 72 || num7 == 3))
						{
							int x2 = ((!item2.Bool(0.1f)) ? 1 : 2);
							x2 = (flag ? MathUtils.Min(x2, num5 - 1) : MathUtils.Min(x2, num5));
							for (int j = 0; j < x2; j++)
							{
								chunk.SetCellValueFast(x, num6 + 1 + j, z, Terrain.MakeBlockValue(233));
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
		int design = random.Int(0, MathUtils.Max(12, 1));
		for (int i = 0; i < design; i++)
		{
			int num = random.Int(4, 11);
			int num2 = random.Int(4, 11);
			int humidityFast = chunk.GetHumidityFast(num, num2);
			int temperatureFast = chunk.GetTemperatureFast(num, num2);
			if (humidityFast < 10 || temperatureFast < 10)
			{
				continue;
			}
			int num3 = chunk.CalculateTopmostCellHeight(num, num2);
			for (int j = 0; j < 100; j++)
			{
				int num4 = num + random.Int(-3, 3);
				int num5 = MathUtils.Clamp(num3 + random.Int(-12, 1), 1, 255);
				int num6 = num2 + random.Int(-3, 3);
				switch (Terrain.ExtractContents(chunk.GetCellValueFast(num4, num5, num6)))
				{
				case 2:
				case 3:
				case 8:
				case 9:
				case 12:
				case 66:
				case 67:
				{
					int num7 = random.Int(0, 3);
					for (int k = 0; k < 4; k++)
					{
						int face = (k + num7) % 4;
						Point3 point = CellFace.FaceToPoint3(face);
						if (chunk.GetCellContentsFast(num4 + point.X, num5, num6 + point.Z) != 0)
						{
							continue;
						}
						int num8 = num5 - 1;
						while (num8 >= 1 && chunk.GetCellContentsFast(num4 + point.X, num8, num6 + point.Z) == 0 && chunk.GetCellContentsFast(num4, num8, num6) != 0)
						{
							num8--;
						}
						if (chunk.GetCellContentsFast(num4 + point.X, num8, num6 + point.Z) != 0)
						{
							break;
						}
						num8++;
						int value = Terrain.MakeBlockValue(197, 0, IvyBlock.SetFace(0, CellFace.OppositeFace(face)));
						while (num8 >= 1 && chunk.GetCellContentsFast(num4 + point.X, num8, num6 + point.Z) == 0)
						{
							chunk.SetCellValueFast(num4 + point.X, num8, num6 + point.Z, value);
							if (IvyBlock.IsGrowthStopCell(num4 + point.X, num8, num6 + point.Z))
							{
								break;
							}
							num8--;
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
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		Random random = new Random(m_seed + x + 2113 * y);
		if (!random.Bool(0.15f) || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 50f))
		{
			return;
		}
		int num = random.Int(0, MathUtils.Max(2, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.Int(2, 5);
			int num3 = random.Int(2, 5);
			int num4 = random.Int(1, 16 - num2 - 2);
			int num5 = random.Int(1, 16 - num3 - 2);
			bool flag = random.Float(0f, 1f) < 0.5f;
			int num6 = random.Int(3, 5);
			int? num7 = null;
			for (int j = num4 - 1; j < num4 + num2 + 1; j++)
			{
				for (int k = num5 - 1; k < num5 + num3 + 1; k++)
				{
					int num8 = chunk.CalculateTopmostCellHeight(j, k);
					int num9 = MathUtils.Max(num8 - 20, 5);
					while (num8 >= num9 && chunk.GetCellContentsFast(j, num8, k) != 8)
					{
						num8--;
					}
					if (num7.HasValue && num7 != num8)
					{
						break;
					}
					num7 = num8;
					if (chunk.GetCellContentsFast(j, num8, k) != 8)
					{
						break;
					}
				}
			}
			if (!num7.HasValue || num7 - num6 < 5)
			{
				continue;
			}
			for (int l = num4; l < num4 + num2; l++)
			{
				for (int m = num5; m < num5 + num3; m++)
				{
					for (int num10 = num7.Value - 1; num10 >= num7 - num6 + 1; num10--)
					{
						chunk.SetCellValueFast(l, num10, m, Terrain.MakeBlockValue(0));
					}
					chunk.SetCellValueFast(l, num7.Value, m, Terrain.MakeBlockValue(87));
					if (flag)
					{
						int data = SpikedPlankBlock.SetSpikesState(0, random.Float(0f, 1f) < 0.33f);
						chunk.SetCellValueFast(l, num7.Value - num6 + 1, m, Terrain.MakeBlockValue(86, 0, data));
					}
				}
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
		if (!(random.Float(0f, 1f) < 0.033f) || !(CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) > 10f))
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
							chunk.SetCellValueFast(num23, num21 - 2, num24, Terrain.MakeBlockValue(190));
							if (num16.HasValue)
							{
								chunk.SetCellValueFast(num23, num21 - 1, num24, num16.Value);
								if (num22 == 1)
								{
									int num25 = 0;
									if (num18 < 0.2f)
									{
										num25 = Terrain.MakeBlockValue(20);
									}
									else if (num18 < 0.3f)
									{
										num25 = Terrain.MakeBlockValue(24);
									}
									else if (num18 < 0.4f)
									{
										num25 = Terrain.MakeBlockValue(25);
									}
									else if (num18 < 0.5f)
									{
										num25 = Terrain.MakeBlockValue(31, 0, 4);
									}
									else if (num18 < 0.6f)
									{
										num25 = Terrain.MakeBlockValue(132, 0, CellFace.OppositeFace(num9));
									}
									if (num25 != 0)
									{
										chunk.SetCellValueFast(num23, num21, num24, num25);
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
							int value = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.Point3ToFace(p2)));
							int value2 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.OppositeFace(CellFace.Point3ToFace(p2))));
							chunk.SetCellValueFast(num23 + p2.X, num21 - 1, num24 + p2.Z, value);
							chunk.SetCellValueFast(num23 - p2.X, num21 - 1, num24 - p2.Z, value2);
							if (num22 == -1)
							{
								int value3 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.OppositeFace(CellFace.Point3ToFace(p))));
								chunk.SetCellValueFast(num23, num21 - 1, num24, value3);
							}
							if (num22 == 2)
							{
								int value4 = Terrain.MakeBlockValue(contents, 0, StairsBlock.SetRotation(0, CellFace.Point3ToFace(p)));
								chunk.SetCellValueFast(num23, num21 - 1, num24, value4);
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

	public void GenerateSnowAndIce(TerrainChunk chunk)
	{
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num = i + chunk.Origin.X;
				int num2 = j + chunk.Origin.Y;
				for (int num3 = 254; num3 >= 0; num3--)
				{
					int cellValueFast = chunk.GetCellValueFast(i, num3, j);
					int num4 = Terrain.ExtractContents(cellValueFast);
					if (num4 != 0)
					{
						if (!SubsystemWeather.IsPlaceFrozen(chunk.GetTemperatureFast(i, j), num3))
						{
							break;
						}
						if (BlocksManager.Blocks[num4] is WaterBlock)
						{
							if (CalculateOceanShoreDistance(num, num2) > -20f)
							{
								float num5 = 1 + (int)(2f * MathUtils.Sqr(SimplexNoise.OctavedNoise(num, num2, 0.2f, 1, 2f, 1f)));
								for (int k = 0; (float)k < num5; k++)
								{
									if (num3 - k > 0)
									{
										if (!(BlocksManager.Blocks[chunk.GetCellContentsFast(i, num3 - k, j)] is WaterBlock))
										{
											break;
										}
										chunk.SetCellValueFast(i, num3 - k, j, 62);
									}
								}
								if (SubsystemWeather.ShaftHasSnowOnIce(num, num2))
								{
									chunk.SetCellValueFast(i, num3 + 1, j, 61);
								}
							}
						}
						else if (SubsystemSnowBlockBehavior.CanSupportSnow(cellValueFast))
						{
							chunk.SetCellValueFast(i, num3 + 1, j, 61);
						}
						if (num4 == 8)
						{
							chunk.SetCellValueFast(i, num3, j, Terrain.MakeBlockValue(8, 0, 1));
						}
						break;
					}
				}
			}
		}
	}

	public void PropagateFluidsDownwards(TerrainChunk chunk)
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
					int num4 = Terrain.ExtractContents(chunk.GetCellValueFast(num));
					if (num4 == 0 && num2 != 0 && BlocksManager.FluidBlocks[num2] != null)
					{
						chunk.SetCellValueFast(num, num2);
						num4 = num2;
					}
					num2 = num4;
					num3--;
					num--;
				}
			}
		}
	}

	public void UpdateFluidIsTop(TerrainChunk chunk)
	{
		for (int list = 0; list < 16; list++)
		{
			for (int i = 0; i < 16; i++)
			{
				int key = TerrainChunk.CalculateCellIndex(list, 255, i);
				int num = 0;
				int item = 255;
				while (item >= 0)
				{
					int cellValueFast = chunk.GetCellValueFast(key);
					int num2 = Terrain.ExtractContents(cellValueFast);
					if (num2 != num && BlocksManager.FluidBlocks[num2] != null && BlocksManager.FluidBlocks[num] == null)
					{
						int data = Terrain.ExtractData(cellValueFast);
						chunk.SetCellValueFast(key, Terrain.MakeBlockValue(num2, 0, FluidBlock.SetIsTop(data, isTop: true)));
					}
					num = num2;
					item--;
					key--;
				}
			}
		}
	}

	public static void CreateBrushes()
	{
		Random cellValue = new Random(17);
		for (int i = 0; i < 16; i++)
		{
			TerrainBrush terrainBrush = new TerrainBrush();
			int num = cellValue.Int(4, 12);
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f)));
				int num2 = cellValue.Int(3, 8);
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
			int num3 = cellValue.Int(3, 7);
			for (int m = 0; m < num3; m++)
			{
				Vector3 vector2 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f)));
				int num4 = cellValue.Int(3, 6);
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
			int num6 = cellValue.Int(4, 10);
			for (int num7 = 0; num7 < num6; num7++)
			{
				Vector3 vector3 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-2f, 2f), cellValue.Float(-1f, 1f)));
				int num8 = cellValue.Int(3, 6);
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
			int num11 = cellValue.Int(8, 16);
			for (int num12 = 0; num12 < num11; num12++)
			{
				Vector3 vector4 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.25f, 0.25f), cellValue.Float(-1f, 1f)));
				int num13 = cellValue.Int(4, 8);
				Vector3 zero4 = Vector3.Zero;
				for (int num14 = 0; num14 < num13; num14++)
				{
					terrainBrush4.AddBox((int)MathUtils.Floor(zero4.X), (int)MathUtils.Floor(zero4.Y), (int)MathUtils.Floor(zero4.Z), 1, 1, 1, 100);
					zero4 += vector4;
				}
			}
			terrainBrush4.Compile();
			m_saltpeterBrushes.Add(terrainBrush4);
		}
		for (int num15 = 0; num15 < 16; num15++)
		{
			TerrainBrush terrainBrush5 = new TerrainBrush();
			int num16 = cellValue.Int(4, 10);
			for (int num17 = 0; num17 < num16; num17++)
			{
				Vector3 vector5 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f)));
				int num18 = cellValue.Int(3, 6);
				Vector3 zero5 = Vector3.Zero;
				for (int num19 = 0; num19 < num18; num19++)
				{
					terrainBrush5.AddBox((int)MathUtils.Floor(zero5.X), (int)MathUtils.Floor(zero5.Y), (int)MathUtils.Floor(zero5.Z), 1, 1, 1, 101);
					zero5 += vector5;
				}
			}
			terrainBrush5.Compile();
			m_sulphurBrushes.Add(terrainBrush5);
		}
		for (int num20 = 0; num20 < 16; num20++)
		{
			TerrainBrush terrainBrush6 = new TerrainBrush();
			int num21 = cellValue.Int(2, 6);
			for (int num22 = 0; num22 < num21; num22++)
			{
				Vector3 vector6 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f)));
				int num23 = cellValue.Int(3, 6);
				Vector3 zero6 = Vector3.Zero;
				for (int num24 = 0; num24 < num23; num24++)
				{
					terrainBrush6.AddBox((int)MathUtils.Floor(zero6.X), (int)MathUtils.Floor(zero6.Y), (int)MathUtils.Floor(zero6.Z), 1, 1, 1, 112);
					zero6 += vector6;
				}
			}
			terrainBrush6.Compile();
			m_diamondBrushes.Add(terrainBrush6);
		}
		for (int num25 = 0; num25 < 16; num25++)
		{
			TerrainBrush terrainBrush7 = new TerrainBrush();
			int num26 = cellValue.Int(4, 10);
			for (int num27 = 0; num27 < num26; num27++)
			{
				Vector3 vector7 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f)));
				int num28 = cellValue.Int(3, 6);
				Vector3 zero7 = Vector3.Zero;
				for (int num29 = 0; num29 < num28; num29++)
				{
					terrainBrush7.AddBox((int)MathUtils.Floor(zero7.X), (int)MathUtils.Floor(zero7.Y), (int)MathUtils.Floor(zero7.Z), 1, 1, 1, 148);
					zero7 += vector7;
				}
			}
			terrainBrush7.Compile();
			m_germaniumBrushes.Add(terrainBrush7);
		}
		for (int num30 = 0; num30 < 16; num30++)
		{
			TerrainBrush terrainBrush8 = new TerrainBrush();
			int num31 = cellValue.Int(16, 32);
			for (int num32 = 0; num32 < num31; num32++)
			{
				Vector3 vector8 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.75f, 0.75f), cellValue.Float(-1f, 1f)));
				int num33 = cellValue.Int(6, 12);
				Vector3 zero8 = Vector3.Zero;
				for (int num34 = 0; num34 < num33; num34++)
				{
					terrainBrush8.AddBox((int)MathUtils.Floor(zero8.X), (int)MathUtils.Floor(zero8.Y), (int)MathUtils.Floor(zero8.Z), 1, 1, 1, 2);
					zero8 += vector8;
				}
			}
			terrainBrush8.Compile();
			m_dirtPocketBrushes.Add(terrainBrush8);
		}
		for (int num35 = 0; num35 < 16; num35++)
		{
			TerrainBrush terrainBrush9 = new TerrainBrush();
			int num36 = cellValue.Int(16, 32);
			for (int num37 = 0; num37 < num36; num37++)
			{
				Vector3 vector9 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.75f, 0.75f), cellValue.Float(-1f, 1f)));
				int num38 = cellValue.Int(6, 12);
				Vector3 zero9 = Vector3.Zero;
				for (int num39 = 0; num39 < num38; num39++)
				{
					terrainBrush9.AddBox((int)MathUtils.Floor(zero9.X), (int)MathUtils.Floor(zero9.Y), (int)MathUtils.Floor(zero9.Z), 1, 1, 1, 6);
					zero9 += vector9;
				}
			}
			terrainBrush9.Compile();
			m_gravelPocketBrushes.Add(terrainBrush9);
		}
		for (int num40 = 0; num40 < 16; num40++)
		{
			TerrainBrush terrainBrush10 = new TerrainBrush();
			int num41 = cellValue.Int(16, 32);
			for (int num42 = 0; num42 < num41; num42++)
			{
				Vector3 vector10 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.75f, 0.75f), cellValue.Float(-1f, 1f)));
				int num43 = cellValue.Int(6, 12);
				Vector3 zero10 = Vector3.Zero;
				for (int num44 = 0; num44 < num43; num44++)
				{
					terrainBrush10.AddBox((int)MathUtils.Floor(zero10.X), (int)MathUtils.Floor(zero10.Y), (int)MathUtils.Floor(zero10.Z), 1, 1, 1, 66);
					zero10 += vector10;
				}
			}
			terrainBrush10.Compile();
			m_limestonePocketBrushes.Add(terrainBrush10);
		}
		for (int num45 = 0; num45 < 16; num45++)
		{
			TerrainBrush terrainBrush11 = new TerrainBrush();
			int num46 = cellValue.Int(16, 32);
			for (int num47 = 0; num47 < num46; num47++)
			{
				Vector3 vector11 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.1f, 0.1f), cellValue.Float(-1f, 1f)));
				int num48 = cellValue.Int(6, 12);
				Vector3 zero11 = Vector3.Zero;
				for (int num49 = 0; num49 < num48; num49++)
				{
					terrainBrush11.AddBox((int)MathUtils.Floor(zero11.X), (int)MathUtils.Floor(zero11.Y), (int)MathUtils.Floor(zero11.Z), 1, 1, 1, 72);
					zero11 += vector11;
				}
			}
			terrainBrush11.Compile();
			m_clayPocketBrushes.Add(terrainBrush11);
		}
		for (int num50 = 0; num50 < 16; num50++)
		{
			TerrainBrush terrainBrush12 = new TerrainBrush();
			int num51 = cellValue.Int(16, 32);
			for (int num52 = 0; num52 < num51; num52++)
			{
				Vector3 vector12 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.75f, 0.75f), cellValue.Float(-1f, 1f)));
				int num53 = cellValue.Int(6, 12);
				Vector3 zero12 = Vector3.Zero;
				for (int num54 = 0; num54 < num53; num54++)
				{
					terrainBrush12.AddBox((int)MathUtils.Floor(zero12.X), (int)MathUtils.Floor(zero12.Y), (int)MathUtils.Floor(zero12.Z), 1, 1, 1, 7);
					zero12 += vector12;
				}
			}
			terrainBrush12.Compile();
			m_sandPocketBrushes.Add(terrainBrush12);
		}
		for (int num55 = 0; num55 < 16; num55++)
		{
			TerrainBrush terrainBrush13 = new TerrainBrush();
			int num56 = cellValue.Int(16, 32);
			for (int num57 = 0; num57 < num56; num57++)
			{
				Vector3 vector13 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-0.75f, 0.75f), cellValue.Float(-1f, 1f)));
				int num58 = cellValue.Int(6, 12);
				Vector3 zero13 = Vector3.Zero;
				for (int num59 = 0; num59 < num58; num59++)
				{
					terrainBrush13.AddBox((int)MathUtils.Floor(zero13.X), (int)MathUtils.Floor(zero13.Y), (int)MathUtils.Floor(zero13.Z), 1, 1, 1, 67);
					zero13 += vector13;
				}
			}
			terrainBrush13.Compile();
			m_basaltPocketBrushes.Add(terrainBrush13);
		}
		for (int num60 = 0; num60 < 16; num60++)
		{
			TerrainBrush terrainBrush14 = new TerrainBrush();
			int num61 = cellValue.Int(16, 32);
			for (int num62 = 0; num62 < num61; num62++)
			{
				Vector3 vector14 = 0.5f * Vector3.Normalize(new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f), cellValue.Float(-1f, 1f)));
				int num63 = cellValue.Int(5, 10);
				Vector3 zero14 = Vector3.Zero;
				for (int num64 = 0; num64 < num63; num64++)
				{
					terrainBrush14.AddBox((int)MathUtils.Floor(zero14.X), (int)MathUtils.Floor(zero14.Y), (int)MathUtils.Floor(zero14.Z), 1, 1, 1, 3);
					zero14 += vector14;
				}
			}
			terrainBrush14.Compile();
			m_granitePocketBrushes.Add(terrainBrush14);
		}
		int[] designIndex = new int[3] { 4, 6, 8 };
		for (int num65 = 0; num65 < 4 * designIndex.Length; num65++)
		{
			TerrainBrush terrainBrush15 = new TerrainBrush();
			int num66 = designIndex[num65 / 4];
			int num67 = num65 % 2 + 1;
			float num68 = ((num65 % 4 == 2) ? 0.5f : 1f);
			int num69 = ((num65 % 4 == 1) ? (num66 * num66) : (2 * num66 * num66));
			for (int num70 = 0; num70 < num69; num70++)
			{
				Vector2 vector15 = cellValue.Vector2(0f, num66);
				float num71 = vector15.Length();
				int num72 = cellValue.Int(3, 4);
				int sizeY = 1 + (int)MathUtils.Lerp(MathUtils.Max(num66 / 3, 2.5f) * num68, 0f, num71 / (float)num66) + cellValue.Int(0, 1);
				terrainBrush15.AddBox((int)MathUtils.Floor(vector15.X), 0, (int)MathUtils.Floor(vector15.Y), num72, sizeY, num72, 0);
				terrainBrush15.AddBox((int)MathUtils.Floor(vector15.X), -num67, (int)MathUtils.Floor(vector15.Y), num72, num67, num72, 18);
			}
			terrainBrush15.Compile();
			m_waterPocketBrushes.Add(terrainBrush15);
		}
		int[] design = new int[4] { 8, 12, 14, 16 };
		for (int num73 = 0; num73 < 4 * design.Length; num73++)
		{
			TerrainBrush terrainBrush16 = new TerrainBrush();
			int num74 = design[num73 / 4];
			int num75 = num74 + 2;
			float num76 = ((num73 % 4 == 2) ? 0.5f : 1f);
			int num77 = ((num73 % 4 == 1) ? (num74 * num74) : (2 * num74 * num74));
			for (int num78 = 0; num78 < num77; num78++)
			{
				Vector2 vector16 = cellValue.Vector2(0f, num74);
				float num79 = vector16.Length();
				int num80 = cellValue.Int(3, 4);
				int sizeY2 = 1 + (int)MathUtils.Lerp(MathUtils.Max(num74 / 3, 2.5f) * num76, 0f, num79 / (float)num74) + cellValue.Int(0, 1);
				int num81 = 1 + (int)MathUtils.Lerp(num75, 0f, num79 / (float)num74) + cellValue.Int(0, 1);
				terrainBrush16.AddBox((int)MathUtils.Floor(vector16.X), 0, (int)MathUtils.Floor(vector16.Y), num80, sizeY2, num80, 0);
				terrainBrush16.AddBox((int)MathUtils.Floor(vector16.X), -num81, (int)MathUtils.Floor(vector16.Y), num80, num81, num80, 92);
			}
			terrainBrush16.Compile();
			m_magmaPocketBrushes.Add(terrainBrush16);
		}
		for (int num82 = 0; num82 < 7; num82++)
		{
			m_caveBrushesByType.Add(new List<TerrainBrush>());
			for (int num83 = 0; num83 < 3; num83++)
			{
				TerrainBrush terrainBrush17 = new TerrainBrush();
				int num84 = 6 + 4 * num82;
				int max = 3 + num82 / 3;
				int max2 = 9 + num82;
				for (int num85 = 0; num85 < num84; num85++)
				{
					int num86 = cellValue.Int(2, max);
					int num87 = cellValue.Int(8, max2) - 2 * num86;
					Vector3 vector17 = 0.5f * new Vector3(cellValue.Float(-1f, 1f), cellValue.Float(0f, 1f), cellValue.Float(-1f, 1f));
					Vector3 zero15 = Vector3.Zero;
					for (int num88 = 0; num88 < num87; num88++)
					{
						terrainBrush17.AddBox((int)MathUtils.Floor(zero15.X) - num86 / 2, (int)MathUtils.Floor(zero15.Y) - num86 / 2, (int)MathUtils.Floor(zero15.Z) - num86 / 2, num86, num86, num86, 0);
						zero15 += vector17;
					}
				}
				terrainBrush17.Compile();
				m_caveBrushesByType[num82].Add(terrainBrush17);
			}
		}
	}

	public void GenerateBasaltBoulders(TerrainChunk chunk)
	{
		Terrain value = m_subsystemTerrain.Terrain;
		int x = chunk.Origin.X;
		int item = x + 16;
		int y = chunk.Origin.Y;
		int value2 = y + 16;
		int value3 = chunk.Coords.X;
		int value4 = chunk.Coords.Y;
		for (int furnitureSet = value3; furnitureSet <= value3; furnitureSet++)
		{
			for (int i = value4; i <= value4; i++)
			{
				Random random = new Random(m_seed + furnitureSet + 3943 * i);
				float num2 = 0.01f;
				if (!random.Bool(num2))
				{
					continue;
				}
				int num3 = random.Int(1, 4);
				int num4 = furnitureSet * 16 + random.Int(0, 15);
				int num5 = i * 16 + random.Int(0, 15);
				int num6 = value.CalculateTopmostCellHeight(num4, num5);
				if (num6 < 66)
				{
					continue;
				}
				int cellContentsFast = value.GetCellContentsFast(num4, num6, num5);
				if (cellContentsFast != 2 && cellContentsFast != 8)
				{
					continue;
				}
				num6++;
				int num7 = num4;
				int num8 = num6;
				int num9 = num5;
				int value5 = 67;
				if (random.Bool(0.5f))
				{
					float num10 = random.Float(0f, 1f);
					if (num10 < 0.33f)
					{
						value5 = 39;
					}
					else
					{
						value5 = ((!(num10 < 0.66f)) ? 148 : 101);
					}
				}
				for (int j = -num3; j <= num3; j++)
				{
					for (int k = -num3; k <= num3; k++)
					{
						for (int l = -num3; l <= num3; l++)
						{
							if (!(random.Float(0f, 1f) > 0.1f) || j * j + k * k + l * l > num3 * num3)
							{
								continue;
							}
							int num11 = num7 + j;
							int num12 = num8 + k;
							int num13 = num9 + l;
							if (num11 >= x && num11 < item && num13 >= y && num13 < value2 && num12 >= 0 && num12 < 128)
							{
								if (j == 0 && k == 0 && l == 0)
								{
									value.SetCellValueFast(num11, num12, num13, value5);
								}
								else
								{
									value.SetCellValueFast(num11, num12, num13, 67);
								}
							}
						}
					}
				}
			}
		}
	}

	public void GenerateDesertBoulders(TerrainChunk chunk)
	{
		Terrain valuesDictionary2 = m_subsystemTerrain.Terrain;
		int valuesDictionary3 = chunk.Coords.X;
		int num = chunk.Coords.Y;
		for (int i = valuesDictionary3; i <= valuesDictionary3; i++)
		{
			for (int j = num; j <= num; j++)
			{
				Random furnitureSet = new Random(m_seed + i + 3943 * j);
				bool valuesDictionary4 = furnitureSet.Bool(0.01f);
				if (!furnitureSet.Bool(0.04f) && !valuesDictionary4)
				{
					return;
				}
				int value = i * 16 + furnitureSet.Int(0, 15);
				int num2 = j * 16 + furnitureSet.Int(0, 15);
				int num3 = valuesDictionary2.CalculateTopmostCellHeight(value, num2);
				if (valuesDictionary2.GetCellContentsFast(value, num3, num2) != 7)
				{
					continue;
				}
				int num4 = furnitureSet.Int(4, 5) + (valuesDictionary4 ? 2 : 0);
				int contents = (furnitureSet.Bool(0.5f) ? 4 : 7);
				for (int k = 0; k < num4; k++)
				{
					if (valuesDictionary2.GetCellContentsFast(value, num3 + k, num2) == 0)
					{
						valuesDictionary2.SetCellValueFast(value, num3 + k, num2, Terrain.MakeBlockValue(contents));
					}
				}
				int num5 = 5;
				for (int l = 0; l < num5; l++)
				{
					int num6 = furnitureSet.Int(2, 3);
					int num7 = furnitureSet.Int(-2, 2);
					int num8 = furnitureSet.Int(-2, 2);
					if (!furnitureSet.Bool(0.8f) && Math.Abs(num7) <= 1 && Math.Abs(num8) <= 1)
					{
						continue;
					}
					for (int m = 0; m < num6; m++)
					{
						if (valuesDictionary2.GetCellContentsFast(value + num7, num3 + m, num2 + num8) == 0)
						{
							valuesDictionary2.SetCellValueFast(value + num7, num3 + m, num2 + num8, Terrain.MakeBlockValue(contents));
						}
					}
				}
				int num9 = 10;
				for (int n = 0; n < num9; n++)
				{
					int x = value + furnitureSet.Int(-3, 3);
					int z = num2 + furnitureSet.Int(-3, 3);
					int num10 = furnitureSet.Int(1, 2);
					for (int num11 = 0; num11 < num10; num11++)
					{
						if (valuesDictionary2.GetCellContentsFast(x, num3 + num11, z) == 0)
						{
							valuesDictionary2.SetCellValueFast(x, num3 + num11, z, Terrain.MakeBlockValue(contents));
						}
					}
				}
				if (!(furnitureSet.Float(0f, 1f) < 0.4f))
				{
					continue;
				}
				int num12 = ((!valuesDictionary4) ? 1 : 2);
				for (int num13 = 0; num13 < num12; num13++)
				{
					int x2 = value + furnitureSet.Int(-1, 1);
					int y = num3 + furnitureSet.Int(0, num4 - 1);
					int z2 = num2 + furnitureSet.Int(-1, 1);
					if (furnitureSet.Bool(0.5f))
					{
						y = num3 - 1;
					}
					if (valuesDictionary2.GetCellContentsFast(x2, y, z2) == 0)
					{
						valuesDictionary2.SetCellValueFast(x2, y, z2, Terrain.MakeBlockValue(100));
					}
				}
			}
		}
	}

	public void GenerateBushes(TerrainChunk chunk)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		int x = chunk.Origin.X;
		int num = x + 16;
		int y = chunk.Origin.Y;
		int num2 = y + 16;
		int x2 = chunk.Coords.X;
		int y2 = chunk.Coords.Y;
		for (int i = x2; i <= x2; i++)
		{
			for (int j = y2; j <= y2; j++)
			{
				Random random = new Random(m_seed + i + 3943 * j);
				int num3 = CalculateHumidity(i * 16, j * 16);
				int num4 = CalculateTemperature(i * 16, j * 16);
				float num5 = 0.2f;
				float x3 = num5 + (float)num4 / 100f * 0.1f;
				x3 = MathUtils.Clamp(x3, num5, 0.2f);
				if (num3 > 50)
				{
					x3 *= 3f;
				}
				if (!random.Bool(x3))
				{
					continue;
				}
				int num6 = 1;
				if (random.Bool(0.05f))
				{
					num6 = random.Int(2, 3);
				}
				for (int k = 0; k < num6; k++)
				{
					int num7 = random.Int(0, 15);
					int num8 = random.Int(0, 15);
					int num9 = i * 16 + num7;
					int num10 = j * 16 + num8;
					int num11 = terrain.CalculateTopmostCellHeight(num9, num10);
					switch (terrain.GetCellContentsFast(num9, num11, num10))
					{
					case 2:
					case 6:
					case 8:
					{
						num11++;
						int value = 9;
						int value2 = 13;
						terrain.SetCellValueFast(num9, num11, num10, value);
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
										int num12 = num9 + l;
										int num13 = num11 + m;
										int num14 = num10 + n;
										if (num12 >= x && num12 < num && num14 >= y && num14 < num2 && num13 >= 0 && num13 < 128)
										{
											int cellContentsFast = terrain.GetCellContentsFast(num12, num13, num14);
											if (!BlocksManager.Blocks[cellContentsFast].IsCollidable)
											{
												terrain.SetCellValueFast(num12, num13, num14, value2);
											}
										}
									}
								}
							}
						}
						else
						{
							for (int num15 = -1; num15 <= 1; num15++)
							{
								for (int num16 = -1; num16 <= 1; num16++)
								{
									for (int num17 = -1; num17 <= 1; num17++)
									{
										if ((num15 == 0 && num16 == 0 && num17 == 0) || Math.Abs(num15) + Math.Abs(num16) + Math.Abs(num17) != 1)
										{
											continue;
										}
										int num18 = num9 + num15;
										int num19 = num11 + num16;
										int num20 = num10 + num17;
										if (num18 >= x && num18 < num && num20 >= y && num20 < num2 && num19 >= 0 && num19 < 128)
										{
											int cellContentsFast2 = terrain.GetCellContentsFast(num18, num19, num20);
											if (!BlocksManager.Blocks[cellContentsFast2].IsCollidable)
											{
												terrain.SetCellValueFast(num18, num19, num20, value2);
											}
										}
									}
								}
							}
							if (random.Bool(0.2f))
							{
								List<Point3> list = new List<Point3>();
								for (int num21 = -1; num21 <= 1; num21++)
								{
									for (int num22 = -1; num22 <= 1; num22++)
									{
										for (int num23 = -1; num23 <= 1; num23++)
										{
											if ((num21 == 0 && num22 == 0 && num23 == 0) || Math.Abs(num21) + Math.Abs(num22) + Math.Abs(num23) != 1)
											{
												continue;
											}
											int num24 = num9 + num21;
											int num25 = num11 + num22;
											int num26 = num10 + num23;
											if (num24 >= x && num24 < num && num26 >= y && num26 < num2 && num25 >= 0 && num25 < 128)
											{
												int cellContentsFast3 = terrain.GetCellContentsFast(num24, num25, num26);
												if (!BlocksManager.Blocks[cellContentsFast3].IsCollidable)
												{
													list.Add(new Point3(num24, num25, num26));
												}
											}
										}
									}
								}
								if (list.Count > 0)
								{
									Point3 point = list[random.Int(list.Count)];
									terrain.SetCellValueFast(point.X, point.Y, point.Z, value2);
								}
							}
						}
						if (num6 <= 1)
						{
							break;
						}
						for (int num27 = 1; num27 < num6; num27++)
						{
							int num28 = num7 + random.Int(-5, 5);
							int num29 = num8 + random.Int(-5, 5);
							int x4 = i * 16 + num28;
							int z = j * 16 + num29;
							int num30 = terrain.CalculateTopmostCellHeight(x4, z);
							switch (terrain.GetCellContentsFast(x4, num30, z))
							{
							case 2:
							case 6:
							case 8:
								num30++;
								terrain.SetCellValueFast(x4, num30, z, value);
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
		Terrain designIndex = m_subsystemTerrain.Terrain;
		int x = chunk.Origin.X;
		int num = x + 16;
		int y = chunk.Origin.Y;
		int num2 = y + 16;
		int x2 = chunk.Coords.X;
		int y2 = chunk.Coords.Y;
		for (int i = x2; i <= x2; i++)
		{
			for (int j = y2; j <= y2; j++)
			{
				Random random = new Random(m_seed + i + 3943 * j);
				float probability = 0.15f;
				if (!random.Bool(probability))
				{
					continue;
				}
				int num3 = random.Int(0, 3);
				int num4 = i * 16 + random.Int(0, 15);
				int num5 = j * 16 + random.Int(0, 15);
				int num6 = designIndex.CalculateTopmostCellHeight(num4, num5) - 1;
				if (num6 < 65)
				{
					continue;
				}
				int cellContentsFast = designIndex.GetCellContentsFast(num4, num6, num5);
				if (cellContentsFast != 2 && cellContentsFast != 8)
				{
					continue;
				}
				num6++;
				int num7 = num4;
				int num8 = num6;
				int num9 = num5;
				int value = 3;
				switch (num3)
				{
				case 0:
					designIndex.SetCellValueFast(num7, num8, num9, value);
					designIndex.SetCellValueFast(num7 + 1, num8, num9, value);
					if (random.Bool(0.5f))
					{
						int num22 = random.Int(-1, 1);
						int num23 = random.Int(-1, 1);
						int num24 = random.Int(-1, 1);
						designIndex.SetCellValueFast(num7 + num22, num8 + num23, num9 + num24, value);
					}
					if (random.Bool(0.3f))
					{
						int num25 = random.Int(3, 7);
						int num26 = random.Int(3, 7);
						designIndex.SetCellValueFast(num7 + num25, num8, num9 + num26, value);
						designIndex.SetCellValueFast(num7 + num25 + 1, num8, num9 + num26, value);
					}
					break;
				case 1:
				{
					for (int num27 = 0; num27 < 4 + random.Int(0, 1); num27++)
					{
						designIndex.SetCellValueFast(num7 + num27, num8, num9, value);
					}
					break;
				}
				case 2:
				{
					int num14 = random.Int(1, 2);
					for (int num15 = -num14; num15 <= num14; num15++)
					{
						for (int num16 = -num14 / 2; num16 <= num14 / 2; num16++)
						{
							for (int num17 = -num14; num17 <= num14; num17++)
							{
								if (random.Float(0f, 1f) > 0.1f && num15 * num15 + 2 * num16 * num16 + num17 * num17 <= num14 * num14)
								{
									int num18 = num7 + num15;
									int num19 = num8 + num16;
									int num20 = num9 + num17;
									if (num18 >= x && num18 < num && num20 >= y && num20 < num2 && num19 >= 0 && num19 < 128)
									{
										designIndex.SetCellValueFast(num18, num19, num20, value);
									}
								}
							}
						}
					}
					if (random.Bool(0.3f))
					{
						for (int num21 = 0; num21 < random.Int(1, 2); num21++)
						{
							designIndex.SetCellValueFast(num7 + random.Int(-num14, num14), num8 + random.Int(-num14, num14 / 2), num9 + random.Int(-num14, num14), 16);
						}
					}
					if (random.Bool(0.01f))
					{
						designIndex.SetCellValueFast(num7 + random.Int(-num14, num14), num8 + random.Int(-num14, num14 / 2), num9 + random.Int(-num14, num14), 150);
					}
					break;
				}
				case 3:
				{
					if (!random.Bool(0.05f))
					{
						break;
					}
					int num10 = random.Int(2, 3);
					for (int k = -num10; k <= num10; k++)
					{
						for (int l = -num10 / 2; l <= num10 / 2; l++)
						{
							for (int m = -num10; m <= num10; m++)
							{
								if (random.Float(0f, 1f) > 0.1f && k * k + 2 * l * l + m * m <= num10 * num10)
								{
									int num11 = num7 + k;
									int num12 = num8 + l;
									int num13 = num9 + m;
									if (num11 >= x && num11 < num && num13 >= y && num13 < num2 && num12 >= 0 && num12 < 128)
									{
										designIndex.SetCellValueFast(num11, num12, num13, value);
									}
								}
							}
						}
					}
					if (random.Bool(0.3f))
					{
						for (int n = 0; n < random.Int(1, 2); n++)
						{
							designIndex.SetCellValueFast(num7 + random.Int(-num10, num10), num8 + random.Int(-num10, num10 / 2), num9 + random.Int(-num10, num10), 16);
						}
					}
					if (random.Bool(0.01f))
					{
						designIndex.SetCellValueFast(num7 + random.Int(-num10, num10), num8 + random.Int(-num10, num10 / 2), num9 + random.Int(-num10, num10), 150);
					}
					break;
				}
				}
			}
		}
	}

	public static void ApplyRandomRotationToBrush(TerrainBrush brush, Random random)
	{
		for (int designIndex = 0; designIndex < brush.Cells.Length; designIndex++)
		{
			TerrainBrush.Cell cell = brush.Cells[designIndex];
			int value = cell.Value;
			int num = random.Int(0, 3);
			value = Terrain.ReplaceData(value, (Terrain.ExtractData(value) & -4) | (num & 3));
			brush.Cells[designIndex] = new TerrainBrush.Cell
			{
				X = cell.X,
				Y = cell.Y,
				Z = cell.Z,
				Value = value
			};
		}
	}
}
