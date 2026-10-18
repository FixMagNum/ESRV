using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine;

namespace Game;

public class TerrainContentsGenerator21 : ITerrainContentsGenerator
{
	public class Random
	{
		public static int m_counter;

		public ulong m_seed;

		public const ulong m_multiplier = 25214903917uL;

		public const ulong m_addend = 11uL;

		public const ulong m_mask = 281474976710655uL;

		public static readonly Random GlobalRandom;

		public Random()
			: this(997 * m_counter++)
		{
		}

		public Random(int seed)
		{
			Reset(seed);
		}

		public void Reset(int seed)
		{
			m_seed = (ulong)(seed ^ 0x5DEECE66DL);
		}

		public int Sign()
		{
			if (Int() % 2 != 0)
			{
				return 1;
			}
			return -1;
		}

		public bool Bool()
		{
			return Int() % 2 == 0;
		}

		public bool Bool(float probability)
		{
			return (float)Int() / 2.147484E+09f < probability;
		}

		public int Int()
		{
			m_seed = (m_seed * 25214903917L + 11) & 0xFFFFFFFFFFFFL;
			return (int)(m_seed >> 17);
		}

		public int UniformInt(int min, int max)
		{
			if (min > max)
			{
				throw new ArgumentException("min должен быть меньше или равен max");
			}
			return min + (int)((double)(Int() - int.MinValue) / 4294967296.0 * (double)(max - min + 1));
		}

		public float UniformFloat(float min, float max)
		{
			float cellValue = (float)Int() / 2.147484E+09f;
			return min + cellValue * (max - min);
		}

		public float NormalFloat(float mean, float stddev)
		{
			float i = UniformFloat(0f, 1f);
			if ((double)i < 0.5)
			{
				float y2 = MathUtils.Sqrt(-2f * MathUtils.Log(i));
				float cellValueFast2 = 0.32223243f + y2 * (1f + y2 * (0.3422421f + y2 * (0.020423122f + y2 * 4.536422E-05f)));
				float num3 = 0.09934846f + y2 * (0.58858156f + y2 * (0.5311035f + y2 * (0.10353775f + y2 * 0.00385607f)));
				return mean + stddev * (cellValueFast2 / num3 - y2);
			}
			float num4 = MathUtils.Sqrt(-2f * MathUtils.Log(1f - i));
			float cellValueFast3 = 0.32223243f + num4 * (1f + num4 * (0.3422421f + num4 * (0.020423122f + num4 * 4.536422E-05f)));
			float num5 = 0.09934846f + num4 * (0.58858156f + num4 * (0.5311035f + num4 * (0.10353775f + num4 * 0.00385607f)));
			return mean - stddev * (cellValueFast3 / num5 - num4);
		}

		public Vector2 Vector2(float length, bool circular = false)
		{
			Vector2 vector;
			float num;
			do
			{
				vector = new Vector2(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f));
				num = vector.LengthSquared();
			}
			while (circular && num > 1f);
			return vector * (length / MathUtils.Sqrt(num));
		}

		public Vector2 UniformVector2(float minLength, float maxLength, bool circular = false)
		{
			Vector2 num;
			float position;
			do
			{
				num = new Vector2(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f));
				position = num.LengthSquared();
			}
			while (circular && position > 1f);
			return num * (UniformFloat(minLength, maxLength) / MathUtils.Sqrt(position));
		}

		public Vector3 Vector3(float length, bool spherical = false)
		{
			Vector3 vector;
			float num;
			do
			{
				vector = new Vector3(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f), UniformFloat(-1f, 1f));
				num = vector.LengthSquared();
			}
			while (spherical && num > 1f);
			return vector * (length / MathUtils.Sqrt(num));
		}

		public Vector3 UniformVector3(float minLength, float maxLength, bool spherical = false)
		{
			Vector3 vector;
			float num;
			do
			{
				vector = new Vector3(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f), UniformFloat(-1f, 1f));
				num = vector.LengthSquared();
			}
			while (spherical && num > 1f);
			return vector * (UniformFloat(minLength, maxLength) / MathUtils.Sqrt(num));
		}

		static Random()
		{
			m_counter = (int)Stopwatch.GetTimestamp();
			GlobalRandom = new Random(0);
		}
	}

	public class OldRandom
	{
		public class InternalRandom
		{
			public int m_inext;

			public int m_inextp;

			public int[] m_seedArray;

			public InternalRandom(int seed)
			{
				m_seedArray = new int[56];
				int num2 = 161803398 - ((seed == int.MinValue) ? int.MaxValue : Math.Abs(seed));
				m_seedArray[55] = num2;
				int list = 1;
				for (int gameTime = 1; gameTime < 55; gameTime++)
				{
					int num3 = 21 * gameTime % 55;
					m_seedArray[num3] = list;
					list = num2 - list;
					if (list < 0)
					{
						list += int.MaxValue;
					}
					num2 = m_seedArray[num3];
				}
				for (int item = 1; item < 5; item++)
				{
					for (int key = 1; key < 56; key++)
					{
						m_seedArray[key] -= m_seedArray[1 + (key + 30) % 55];
						if (m_seedArray[key] < 0)
						{
							m_seedArray[key] += int.MaxValue;
						}
					}
				}
				m_inext = 0;
				m_inextp = 21;
			}

			public double GetSampleForLargeRange()
			{
				int fireParticleSystem = InternalSample();
				if (InternalSample() % 2 == 0)
				{
					fireParticleSystem = -fireParticleSystem;
				}
				return ((double)fireParticleSystem + 2147483646.0) / 4294967293.0;
			}

			public int InternalSample()
			{
				int value = m_inext;
				int inextp = m_inextp;
				if (++value >= 56)
				{
					value = 1;
				}
				if (++inextp >= 56)
				{
					inextp = 1;
				}
				int num = m_seedArray[value] - m_seedArray[inextp];
				if (num == int.MaxValue)
				{
					num--;
				}
				if (num < 0)
				{
					num += int.MaxValue;
				}
				m_seedArray[value] = num;
				m_inext = value;
				m_inextp = inextp;
				return num;
			}

			public int Next()
			{
				return InternalSample();
			}

			public int Next(int maxValue)
			{
				if (maxValue < 0)
				{
					throw new ArgumentOutOfRangeException("maxValue");
				}
				return (int)(Sample() * (double)maxValue);
			}

			public int Next(int minValue, int maxValue)
			{
				if (minValue > maxValue)
				{
					throw new ArgumentOutOfRangeException("minValue");
				}
				long num = maxValue - minValue;
				if (num <= int.MaxValue)
				{
					return (int)(Sample() * (double)num) + minValue;
				}
				return (int)(long)(GetSampleForLargeRange() * (double)num) + minValue;
			}

			public void NextBytes(byte[] buffer)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException("buffer");
				}
				for (int terrain = 0; terrain < buffer.Length; terrain++)
				{
					buffer[terrain] = (byte)(InternalSample() % 256);
				}
			}

			public double NextDouble()
			{
				return Sample();
			}

			public double Sample()
			{
				return (double)InternalSample() * 4.656612875245797E-10;
			}
		}

		public InternalRandom m_random;

		public static int m_seed;

		public static readonly OldRandom GlobalRandom;

		public OldRandom()
		{
			m_random = new InternalRandom(997 * m_seed++);
		}

		public OldRandom(int seed)
		{
			m_random = new InternalRandom(seed);
		}

		public int Sign()
		{
			if (m_random.Next() % 2 != 0)
			{
				return 1;
			}
			return -1;
		}

		public bool Bool()
		{
			return m_random.Next() % 2 == 0;
		}

		public int UniformInt(int min, int max)
		{
			return m_random.Next(min, max + 1);
		}

		public float UniformFloat(float min, float max)
		{
			return (float)m_random.NextDouble() * (max - min) + min;
		}

		public float NormalFloat(float mean, float stddev)
		{
			float num = UniformFloat(0f, 1f);
			if ((double)num < 0.5)
			{
				float num2 = MathUtils.Sqrt(-2f * MathUtils.Log(num));
				float num3 = 0.32223243f + num2 * (1f + num2 * (0.3422421f + num2 * (0.020423122f + num2 * 4.536422E-05f)));
				float num4 = 0.09934846f + num2 * (0.58858156f + num2 * (0.5311035f + num2 * (0.10353775f + num2 * 0.00385607f)));
				return mean + stddev * (num3 / num4 - num2);
			}
			float num5 = MathUtils.Sqrt(-2f * MathUtils.Log(1f - num));
			float num6 = 0.32223243f + num5 * (1f + num5 * (0.3422421f + num5 * (0.020423122f + num5 * 4.536422E-05f)));
			float num7 = 0.09934846f + num5 * (0.58858156f + num5 * (0.5311035f + num5 * (0.10353775f + num5 * 0.00385607f)));
			return mean - stddev * (num6 / num7 - num5);
		}

		public Vector2 Vector2(float length)
		{
			return Engine.Vector2.Normalize(new Vector2(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f))) * length;
		}

		public Vector2 UniformVector2(float minLength, float maxLength)
		{
			return Engine.Vector2.Normalize(new Vector2(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f))) * UniformFloat(minLength, maxLength);
		}

		public Vector3 Vector3(float length)
		{
			return Engine.Vector3.Normalize(new Vector3(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f), UniformFloat(-1f, 1f))) * length;
		}

		public Vector3 UniformVector3(float minLength, float maxLength)
		{
			return Engine.Vector3.Normalize(new Vector3(UniformFloat(-1f, 1f), UniformFloat(-1f, 1f), UniformFloat(-1f, 1f))) * UniformFloat(minLength, maxLength);
		}

		static OldRandom()
		{
			m_seed = (int)Stopwatch.GetTimestamp();
			GlobalRandom = new OldRandom(0);
		}
	}

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
			int terrain = (int)MathUtils.Floor(x);
			int flag = (int)MathUtils.Floor(y);
			int i = (int)MathUtils.Ceiling(x);
			int num = (int)MathUtils.Ceiling(y);
			float num2 = x - (float)terrain;
			float j = y - (float)flag;
			float x2 = m_data[terrain + flag * m_sizeX];
			float k = m_data[i + flag * m_sizeX];
			float l = m_data[terrain + num * m_sizeX];
			float m = m_data[i + num * m_sizeX];
			float x3 = MathUtils.Lerp(x2, k, num2);
			float topHeight = MathUtils.Lerp(l, m, num2);
			return MathUtils.Lerp(x3, topHeight, j);
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
			int blockEntity = x + y * m_sizeX + z * m_sizeXY;
			v111 = m_data[blockEntity];
			v211 = m_data[blockEntity + 1];
			v121 = m_data[blockEntity + m_sizeX];
			v221 = m_data[blockEntity + 1 + m_sizeX];
			v112 = m_data[blockEntity + m_sizeXY];
			v212 = m_data[blockEntity + 1 + m_sizeXY];
			v122 = m_data[blockEntity + m_sizeX + m_sizeXY];
			v222 = m_data[blockEntity + 1 + m_sizeX + m_sizeXY];
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
			int list = (int)MathUtils.Floor(y);
			int gameTime = (int)MathUtils.Ceiling(y);
			int num3 = (int)MathUtils.Floor(z);
			int item = (int)MathUtils.Ceiling(z);
			float key = x - (float)num;
			float num4 = y - (float)list;
			float naturalSmokeParticleSystem = z - (float)num3;
			float x2 = m_data[num + list * m_sizeX + num3 * m_sizeX * m_sizeY];
			float value = m_data[num2 + list * m_sizeX + num3 * m_sizeX * m_sizeY];
			float x3 = m_data[num + gameTime * m_sizeX + num3 * m_sizeX * m_sizeY];
			float item2 = m_data[num2 + gameTime * m_sizeX + num3 * m_sizeX * m_sizeY];
			float x4 = m_data[num + list * m_sizeX + item * m_sizeX * m_sizeY];
			float x5 = m_data[num2 + list * m_sizeX + item * m_sizeX * m_sizeY];
			float x6 = m_data[num + gameTime * m_sizeX + item * m_sizeX * m_sizeY];
			float x7 = m_data[num2 + gameTime * m_sizeX + item * m_sizeX * m_sizeY];
			float x8 = MathUtils.Lerp(x2, value, key);
			float x9 = MathUtils.Lerp(x3, item2, key);
			float x10 = MathUtils.Lerp(x4, x5, key);
			float x11 = MathUtils.Lerp(x6, x7, key);
			float x12 = MathUtils.Lerp(x8, x9, num4);
			float x13 = MathUtils.Lerp(x10, x11, num4);
			return MathUtils.Lerp(x12, x13, naturalSmokeParticleSystem);
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

	public bool TGNewBiomeNoise;

	public float TGBiomeScaling;

	public float TGShoreFluctuations;

	public float TGShoreFluctuationsScaling;

	public float TGOceanSlope;

	public float TGOceanSlopeVariation;

	public float TGIslandsFrequency;

	public float TGDensityBias;

	public float TGHeightBias;

	public float TGHillsStrength;

	public float TGMountainsStrength;

	public float TGMountainsPeriod;

	public float TGMountainsPercentage;

	public float TGRiversStrength;

	public float TGTurbulenceStrength;

	public float TGTurbulenceTopOffset;

	public float TGTurbulencePower;

	public static float TGSurfaceMultiplier;

	public bool TGWater;

	public bool TGExtras;

	public bool TGCavesAndPockets;

	public int OceanLevel => 64 + m_worldSettings.SeaLevelOffset;

	static TerrainContentsGenerator21()
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

	public TerrainContentsGenerator21(SubsystemTerrain subsystemTerrain)
	{
		m_subsystemTerrain = subsystemTerrain;
		m_subsystemBottomSuckerBlockBehavior = subsystemTerrain.Project.FindSubsystem<SubsystemBottomSuckerBlockBehavior>(throwOnError: true);
		SubsystemGameInfo subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_worldSettings = subsystemGameInfo.WorldSettings;
		m_seed = subsystemGameInfo.WorldSeed;
		m_islandSize = ((m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island) ? new Vector2?(m_worldSettings.IslandSize) : ((Vector2?)null));
		OldRandom oldRandom = new OldRandom(100 + m_seed);
		Random random = new Random(m_seed);
		if (string.IsNullOrEmpty(subsystemGameInfo.WorldSettings.OriginalSerializationVersion))
		{
			m_oceanCorner = new Vector2(oldRandom.UniformFloat(2000f, 4000f), oldRandom.UniformFloat(2000f, 4000f));
			m_temperatureOffset = new Vector2(1000f, 1000f);
			m_humidityOffset = new Vector2(0f, 0f);
			m_mountainsOffset = new Vector2(0f, 0f);
			m_riversOffset = new Vector2(0f, 0f);
			TGNewBiomeNoise = false;
			TGBiomeScaling = 1f;
			TGShoreFluctuations = 100f;
			TGShoreFluctuationsScaling = 1f;
			TGOceanSlope = 0.015f;
			TGOceanSlopeVariation = 0f;
			TGIslandsFrequency = 0.017f;
			TGDensityBias = 57f;
			TGHeightBias = 1f;
			TGRiversStrength = 0f;
			TGMountainsStrength = 200f;
			TGMountainsPeriod = 0.0014f;
			TGMountainsPercentage = 0.15f;
			TGHillsStrength = 13f;
			TGTurbulenceStrength = 13f;
			TGTurbulenceTopOffset = 3f;
			TGTurbulencePower = 0.5f;
			TGSurfaceMultiplier = 1f;
			TGWater = true;
			TGExtras = true;
			TGCavesAndPockets = true;
		}
		else if (string.CompareOrdinal(subsystemGameInfo.WorldSettings.OriginalSerializationVersion, "2.1") < 0)
		{
			m_oceanCorner = new Vector2(oldRandom.UniformFloat(2000f, 4000f), oldRandom.UniformFloat(2000f, 4000f));
			m_temperatureOffset = new Vector2(1000f, 1000f);
			m_humidityOffset = new Vector2(0f, 0f);
			m_mountainsOffset = new Vector2(0f, 0f);
			m_riversOffset = new Vector2(0f, 0f);
			TGNewBiomeNoise = false;
			TGBiomeScaling = 1f;
			TGShoreFluctuations = 100f;
			TGShoreFluctuationsScaling = 1f;
			TGOceanSlope = 0.015f;
			TGOceanSlopeVariation = 0f;
			TGIslandsFrequency = 0.017f;
			TGDensityBias = 57f;
			TGHeightBias = 1f;
			TGRiversStrength = 0f;
			TGMountainsStrength = 200f;
			TGMountainsPeriod = 0.0014f;
			TGMountainsPercentage = 0.15f;
			TGHillsStrength = 10f;
			TGTurbulenceStrength = 24f;
			TGTurbulenceTopOffset = 0f;
			TGTurbulencePower = 0.3f;
			TGSurfaceMultiplier = 1f;
			TGWater = true;
			TGExtras = true;
			TGCavesAndPockets = true;
		}
		else
		{
			float num = (m_islandSize.HasValue ? MathUtils.Min(m_islandSize.Value.X, m_islandSize.Value.Y) : float.MaxValue);
			m_oceanCorner = new Vector2(random.UniformFloat(-100f, -100f), random.UniformFloat(-100f, -100f));
			m_temperatureOffset = new Vector2(random.UniformFloat(-2000f, 2000f), random.UniformFloat(-2000f, 2000f));
			m_humidityOffset = new Vector2(random.UniformFloat(-2000f, 2000f), random.UniformFloat(-2000f, 2000f));
			m_mountainsOffset = new Vector2(random.UniformFloat(-2000f, 2000f), random.UniformFloat(-2000f, 2000f));
			m_riversOffset = new Vector2(random.UniformFloat(-2000f, 2000f), random.UniformFloat(-2000f, 2000f));
			TGNewBiomeNoise = true;
			TGBiomeScaling = 1.5f * m_worldSettings.BiomeSize;
			TGShoreFluctuations = MathUtils.Clamp(2f * num, 0f, 150f);
			TGShoreFluctuationsScaling = MathUtils.Clamp(0.04f * num, 0.5f, 3f);
			TGOceanSlope = 0.006f;
			TGOceanSlopeVariation = 0.004f;
			TGIslandsFrequency = 0.01f;
			TGDensityBias = 55f;
			TGHeightBias = 1f;
			TGRiversStrength = 1f;
			TGMountainsStrength = 200f;
			TGMountainsPeriod = 0.0015f;
			TGMountainsPercentage = 0.15f;
			TGHillsStrength = 8f;
			TGTurbulenceStrength = 35f;
			TGTurbulenceTopOffset = 0f;
			TGTurbulencePower = 0.3f;
			TGSurfaceMultiplier = 2f;
			TGWater = true;
			TGExtras = true;
			TGCavesAndPockets = true;
		}
	}

	public Vector3 FindCoarseSpawnPosition()
	{
		Vector2 vector = Vector2.Zero;
		float num = float.MinValue;
		for (int i = 0; i < 800; i += 2)
		{
			for (int j = 4; j <= 8; j += 2)
			{
				for (int k = 0; k < 2; k++)
				{
					float num2;
					float x;
					if (k == 0)
					{
						num2 = m_oceanCorner.Y + (float)i;
						x = CalculateOceanShoreX(num2) + (float)j;
					}
					else
					{
						x = m_oceanCorner.X + (float)i;
						num2 = CalculateOceanShoreZ(x) + (float)j;
					}
					float num3 = ScoreSpawnPosition(Terrain.ToCell(x), Terrain.ToCell(num2));
					if (num3 > num)
					{
						vector = new Vector2(x, num2);
						num = num3;
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
		GenerateCaves(chunk);
		GeneratePockets(chunk);
		GenerateMinerals(chunk);
		GenerateSurface(chunk);
		PropagateFluidsDownwards(chunk);
	}

	public void GenerateChunkContentsPass4(TerrainChunk chunk)
	{
		GenerateGrassAndPlants(chunk);
		GenerateTreesAndLogs(chunk);
		GenerateGraniteBoulders(chunk);
		GenerateCacti(chunk);
		GeneratePumpkins(chunk);
		GenerateKelp(chunk);
		GenerateBushes(chunk);
		GenerateBasaltBoulders(chunk);
		GenerateSeagrass(chunk);
		GenerateDesertBoulders(chunk);
		GenerateBottomSuckers(chunk);
		GenerateTraps(chunk);
		GenerateIvy(chunk);
		GenerateGraves(chunk);
		GenerateSnowAndIce(chunk);
		GenerateBedrockAndAir(chunk);
		UpdateFluidIsTop(chunk);
	}

	public float CalculateOceanShoreDistance(float x, float z)
	{
		if (m_islandSize.HasValue)
		{
			float num = CalculateOceanShoreX(z);
			float value2 = CalculateOceanShoreZ(x);
			float key = CalculateOceanShoreX(z + 1000f) + m_islandSize.Value.X;
			float result = CalculateOceanShoreZ(x + 1000f) + m_islandSize.Value.Y;
			return MathUtils.Min(x - num, z - value2, key - x, result - z);
		}
		float value3 = CalculateOceanShoreX(z);
		float terrain = CalculateOceanShoreZ(x);
		return MathUtils.Min(x - value3, z - terrain);
	}

	public float CalculateMountainRangeFactor(float x, float z)
	{
		return 1f - MathUtils.Abs(2f * SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, TGMountainsPeriod / TGBiomeScaling, 3, 1.91f, 0.75f) - 1f);
	}

	public float CalculateHeight(float x, float z)
	{
		float num = TGOceanSlope + TGOceanSlopeVariation * MathUtils.PowSign(2f * SimplexNoise.OctavedNoise(x + m_mountainsOffset.X, z + m_mountainsOffset.Y, 0.01f, 1, 2f, 0.5f) - 1f, 0.5f);
		float num2 = CalculateOceanShoreDistance(x, z);
		float num3 = MathUtils.Saturate(1f - 0.05f * MathUtils.Abs(num2));
		float num4 = MathUtils.Saturate(MathUtils.Sin(TGIslandsFrequency * num2));
		float num5 = MathUtils.Saturate(MathUtils.Saturate((0f - num) * num2) - 0.85f * num4);
		float num6 = MathUtils.Saturate(MathUtils.Saturate(0.05f * (0f - num2 - 10f)) - num4);
		float num7 = CalculateMountainRangeFactor(x, z);
		float f = (1f - num3) * SimplexNoise.OctavedNoise(x, z, 0.001f / TGBiomeScaling, 2, 1.97f, 0.8f);
		float f2 = (1f - num3) * SimplexNoise.OctavedNoise(x, z, 0.0017f / TGBiomeScaling, 2, 1.93f, 0.7f);
		float num8 = (1f - num6) * (1f - num3) * MathUtils.Saturate((num7 - 0.6f) / 0.4f);
		float num9 = (1f - num6) * MathUtils.Saturate((num7 - (1f - TGMountainsPercentage)) / TGMountainsPercentage);
		float num10 = 2f * SimplexNoise.OctavedNoise(x, z, 0.02f, 3, 1.93f, 0.8f) - 1f;
		float num11 = 1.5f * SimplexNoise.OctavedNoise(x, z, 0.004f, 4, 1.98f, 0.9f) - 0.5f;
		float num12 = MathUtils.Lerp(60f, 30f, MathUtils.Saturate(1f * num9 + 0.5f * num8 + MathUtils.Saturate(1f - num2 / 30f)));
		float x2 = MathUtils.Lerp(-2f, -4f, MathUtils.Saturate(num9 + 0.5f * num8));
		float num13 = MathUtils.Saturate(1.5f - num12 * MathUtils.Abs(2f * SimplexNoise.OctavedNoise(x + m_riversOffset.X, z + m_riversOffset.Y, 0.001f, 4, 2f, 0.5f) - 1f));
		float num14 = -50f * num5 + TGHeightBias;
		float num15 = MathUtils.Lerp(0f, 8f, f);
		float num16 = MathUtils.Lerp(0f, -6f, f2);
		float num17 = TGHillsStrength * num8 * num10;
		float num18 = TGMountainsStrength * num9 * num11;
		float f3 = TGRiversStrength * num13;
		float num19 = num14 + num15 + num16 + num18 + num17;
		float num20 = MathUtils.Min(MathUtils.Lerp(num19, x2, f3), num19);
		return MathUtils.Clamp(64f + num20, 10f, 251f);
	}

	public int CalculateTemperature(float x, float z)
	{
		if (TGNewBiomeNoise)
		{
			return MathUtils.Clamp((int)(MathUtils.Saturate(4f * SimplexNoise.OctavedNoise(x + m_temperatureOffset.X, z + m_temperatureOffset.Y, 0.0015f / TGBiomeScaling, 5, 2f, 0.7f) - 1.6f + m_worldSettings.TemperatureOffset / 16f) * 16f), 0, 15);
		}
		return MathUtils.Clamp((int)((MathUtils.Saturate(4f * SimplexNoise.OctavedNoise(x + m_temperatureOffset.X, z + m_temperatureOffset.Y, 0.0006f / TGBiomeScaling, 4, 1.93f, 1f) - 1.6f) + m_worldSettings.TemperatureOffset / 16f) * 16f), 0, 15);
	}

	public int CalculateHumidity(float x, float z)
	{
		if (TGNewBiomeNoise)
		{
			return MathUtils.Clamp((int)(MathUtils.Saturate(4f * SimplexNoise.OctavedNoise(x + m_humidityOffset.X, z + m_humidityOffset.Y, 0.0012f / TGBiomeScaling, 5, 2f, 0.7f) - 1.2f + m_worldSettings.HumidityOffset / 16f) * 16f), 0, 15);
		}
		return MathUtils.Clamp((int)((MathUtils.Saturate(4f * SimplexNoise.OctavedNoise(x + m_humidityOffset.X, z + m_humidityOffset.Y, 0.0008f / TGBiomeScaling, 5, 1.97f, 1f) - 1.5f) + m_worldSettings.HumidityOffset / 16f) * 16f), 0, 15);
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
		float num = 0f;
		float num2 = CalculateOceanShoreDistance(x, z);
		float num3 = CalculateMountainRangeFactor(x, z);
		int num4 = CalculateHumidity(x, z);
		int num5 = CalculateTemperature(x, z);
		if (num2 < 0f)
		{
			num--;
		}
		if (num2 > 10f)
		{
			num--;
		}
		if (num3 > 0.66f)
		{
			num -= 0.5f;
		}
		if (num4 < 10)
		{
			num--;
		}
		if (num5 < 2)
		{
			num -= 0.5f;
		}
		float x2 = CalculateHeight(x, z);
		float x3 = CalculateHeight(x - 5, z - 5);
		float x4 = CalculateHeight(x - 5, z + 5);
		float x5 = CalculateHeight(x + 5, z - 5);
		float x6 = CalculateHeight(x + 5, z + 5);
		float num6 = MathUtils.Min(x2, MathUtils.Min(x3, x4, x5, x6));
		float num7 = MathUtils.Max(x2, MathUtils.Max(x3, x4, x5, x6));
		if (num6 < 64f || num7 > 75f)
		{
			num--;
		}
		return num;
	}

	public void GenerateSurfaceParameters(TerrainChunk chunk, int x1, int z1, int x2, int z2)
	{
		for (int surfaceFluidBlock = x1; surfaceFluidBlock < x2; surfaceFluidBlock++)
		{
			for (int i = z1; i < z2; i++)
			{
				int num = surfaceFluidBlock + chunk.Origin.X;
				int num2 = i + chunk.Origin.Y;
				int temperature = CalculateTemperature(num, num2);
				int humidity = CalculateHumidity(num, num2);
				chunk.SetTemperatureFast(surfaceFluidBlock, i, temperature);
				chunk.SetHumidityFast(surfaceFluidBlock, i, humidity);
			}
		}
	}

	public void GenerateTerrain(TerrainChunk chunk, int x1, int z1, int x2, int z2)
	{
		int surfaceBlock = x2 - x1;
		int surfaceHeight = z2 - z1;
		int num = chunk.Origin.X + x1;
		int num2 = chunk.Origin.Y + z1;
		Grid2d grid2d = new Grid2d(surfaceBlock, surfaceHeight);
		Grid2d grid2d2 = new Grid2d(surfaceBlock, surfaceHeight);
		for (int i = 0; i < surfaceHeight; i++)
		{
			for (int j = 0; j < surfaceBlock; j++)
			{
				grid2d.Set(j, i, CalculateOceanShoreDistance(j + num, i + num2));
				grid2d2.Set(j, i, CalculateMountainRangeFactor(j + num, i + num2));
			}
		}
		Grid3d grid3d = new Grid3d(surfaceBlock / 4 + 1, 33, surfaceHeight / 4 + 1);
		for (int k = 0; k < grid3d.SizeX; k++)
		{
			for (int l = 0; l < grid3d.SizeZ; l++)
			{
				int num3 = k * 4 + num;
				int num4 = l * 4 + num2;
				float num5 = CalculateHeight(num3, num4);
				float num6 = CalculateMountainRangeFactor(num3, num4);
				float num7 = MathUtils.Saturate(0.9f * (num6 - 0.8f) / 0.2f + 0.1f);
				for (int m = 0; m < grid3d.SizeY; m++)
				{
					int num8 = m * 8;
					float num9 = num5 - TGTurbulenceTopOffset;
					float num10 = MathUtils.Lerp(0f, TGTurbulenceStrength * num7, MathUtils.Saturate((num9 - (float)num8) * 0.2f)) * MathUtils.PowSign(2f * SimplexNoise.OctavedNoise(num3, num8 + 1000, num4, 0.008f, 3, 2f, 0.75f) - 1f, TGTurbulencePower);
					float num11 = (float)num8 + num10;
					float num12 = num5 - num11;
					num12 += MathUtils.Max(4f * (TGDensityBias - (float)num8), 0f);
					grid3d.Set(k, m, l, num12);
				}
			}
		}
		int oceanLevel = OceanLevel;
		for (int n = 0; n < grid3d.SizeX - 1; n++)
		{
			for (int num13 = 0; num13 < grid3d.SizeZ - 1; num13++)
			{
				for (int num14 = 0; num14 < grid3d.SizeY - 1; num14++)
				{
					grid3d.Get8(n, num14, num13, out var v, out var v2, out var v3, out var v4, out var v5, out var v6, out var v7, out var v8);
					float num15 = (v2 - v) / 4f;
					float num16 = (v4 - v3) / 4f;
					float num17 = (v6 - v5) / 4f;
					float num18 = (v8 - v7) / 4f;
					float num19 = v;
					float num20 = v3;
					float num21 = v5;
					float num22 = v7;
					for (int num23 = 0; num23 < 4; num23++)
					{
						float num24 = (num21 - num19) / 4f;
						float num25 = (num22 - num20) / 4f;
						float num26 = num19;
						float num27 = num20;
						for (int num28 = 0; num28 < 4; num28++)
						{
							float num29 = (num27 - num26) / 8f;
							float num30 = num26;
							int num31 = num23 + n * 4;
							int num32 = num28 + num13 * 4;
							int x3 = x1 + num31;
							int z3 = z1 + num32;
							float x4 = grid2d.Get(num31, num32);
							float num33 = grid2d2.Get(num31, num32);
							int temperatureFast = chunk.GetTemperatureFast(x3, z3);
							int humidityFast = chunk.GetHumidityFast(x3, z3);
							float f = num33 - 0.01f * (float)humidityFast;
							float num34 = MathUtils.Lerp(100f, 0f, f);
							float num35 = MathUtils.Lerp(300f, 30f, f);
							bool flag = (temperatureFast > 8 && humidityFast < 8 && num33 < 0.95f) || (MathUtils.Abs(x4) < 12f && num33 < 0.9f);
							int num36 = TerrainChunk.CalculateCellIndex(x3, 0, z3);
							for (int num37 = 0; num37 < 8; num37++)
							{
								int num38 = num37 + num14 * 8;
								int value = 0;
								if (num30 < 0f)
								{
									if (num38 <= oceanLevel)
									{
										value = 18;
									}
								}
								else if (flag)
								{
									if (num30 >= num34)
									{
										value = ((num30 >= num35) ? 67 : 3);
									}
									else
									{
										value = 4;
									}
								}
								else
								{
									value = ((num30 >= num35) ? 67 : 3);
								}
								chunk.SetCellValueFast(num36 + num38, value);
								num30 += num29;
							}
							num26 += num24;
							num27 += num25;
						}
						num19 += num15;
						num20 += num16;
						num21 += num17;
						num22 += num18;
					}
				}
			}
		}
	}

	public void GenerateSurface(TerrainChunk chunk)
	{
		Terrain num = m_subsystemTerrain.Terrain;
		Random random = new Random(m_seed + chunk.Coords.X + 101 * chunk.Coords.Y);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int num2 = i + chunk.Origin.X;
				int num3 = j + chunk.Origin.Y;
				int num4 = TerrainChunk.CalculateCellIndex(i, 254, j);
				int num5 = 254;
				while (num5 >= 0)
				{
					int num6 = Terrain.ExtractContents(chunk.GetCellValueFast(num4));
					if (!BlocksManager.Blocks[num6].IsTransparent)
					{
						float num7 = CalculateMountainRangeFactor(num2, num3);
						int temperature = num.GetTemperature(num2, num3);
						int humidity = num.GetHumidity(num2, num3);
						float f = MathUtils.Saturate(MathUtils.Saturate((num7 - 0.9f) / 0.1f) - MathUtils.Saturate(((float)humidity - 3f) / 12f) + TGSurfaceMultiplier * MathUtils.Saturate(((float)num5 - 85f) * 0.05f));
						int min = (int)MathUtils.Lerp(4f, 0f, f);
						int max = (int)MathUtils.Lerp(7f, 0f, f);
						int num8 = MathUtils.Min(random.UniformInt(min, max), num5);
						int contents;
						if (num6 == 4)
						{
							contents = ((temperature > 4 && temperature < 7) ? 6 : 7);
						}
						else
						{
							int num9 = temperature / 4;
							int num10 = ((num5 + 1 < 255) ? chunk.GetCellContentsFast(i, num5 + 1, j) : 0);
							if ((num5 < 66 || num5 == 84 + num9 || num5 == 103 + num9) && humidity == 9 && temperature % 6 == 1)
							{
								contents = 66;
							}
							else
							{
								contents = ((num10 != 18 || humidity <= 8 || humidity % 2 != 0 || temperature % 3 != 0) ? 2 : 72);
							}
						}
						int num11 = TerrainChunk.CalculateCellIndex(i, num5 + 1, j);
						for (int k = num11 - num8; k < num11; k++)
						{
							if (Terrain.ExtractContents(chunk.GetCellValueFast(k)) != 0)
							{
								int value = Terrain.ReplaceContents(0, contents);
								chunk.SetCellValueFast(k, value);
							}
						}
						break;
					}
					num5--;
					num4--;
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
		int num = chunk.Coords.X;
		int terrain = chunk.Coords.Y;
		for (int i = num - 1; i <= num + 1; i++)
		{
			for (int j = terrain - 1; j <= terrain + 1; j++)
			{
				Random num4 = new Random(m_seed + i + 119 * j);
				int num5 = num4.UniformInt(0, 10);
				for (int k = 0; k < num5; k++)
				{
					num4.UniformInt(0, 1);
				}
				float num7 = CalculateMountainRangeFactor(i * 16, j * 16);
				int i2 = (int)(5f + 2f * num7 * SimplexNoise.OctavedNoise(i, j, 0.33f, 1, 1f, 1f));
				for (int l = 0; l < i2; l++)
				{
					int x = i * 16 + num4.UniformInt(0, 15);
					int y = num4.UniformInt(5, 80);
					int z = j * 16 + num4.UniformInt(0, 15);
					m_coalBrushes[num4.UniformInt(0, m_coalBrushes.Count - 1)].PaintFastSelective(chunk, x, y, z, 3);
				}
				int j2 = (int)(6f + 2f * num7 * SimplexNoise.OctavedNoise(i + 1211, j + 396, 0.33f, 1, 1f, 1f));
				for (int m = 0; m < j2; m++)
				{
					int x2 = i * 16 + num4.UniformInt(0, 15);
					int y2 = num4.UniformInt(20, 65);
					int z2 = j * 16 + num4.UniformInt(0, 15);
					m_copperBrushes[num4.UniformInt(0, m_copperBrushes.Count - 1)].PaintFastSelective(chunk, x2, y2, z2, 3);
				}
				int chunkAtCell = (int)(5f + 2f * num7 * SimplexNoise.OctavedNoise(i + 713, j + 211, 0.33f, 1, 1f, 1f));
				for (int n = 0; n < chunkAtCell; n++)
				{
					int x3 = i * 16 + num4.UniformInt(0, 15);
					int y3 = num4.UniformInt(2, 40);
					int z3 = j * 16 + num4.UniformInt(0, 15);
					m_ironBrushes[num4.UniformInt(0, m_ironBrushes.Count - 1)].PaintFastSelective(chunk, x3, y3, z3, 67);
				}
				int num8 = (int)(3f + 2f * num7 * SimplexNoise.OctavedNoise(i + 915, j + 272, 0.33f, 1, 1f, 1f));
				for (int num9 = 0; num9 < num8; num9++)
				{
					int x4 = i * 16 + num4.UniformInt(0, 15);
					int y4 = num4.UniformInt(50, 70);
					int z4 = j * 16 + num4.UniformInt(0, 15);
					m_saltpeterBrushes[num4.UniformInt(0, m_saltpeterBrushes.Count - 1)].PaintFastSelective(chunk, x4, y4, z4, 4);
				}
				int k2 = (int)(3f + 2f * num7 * SimplexNoise.OctavedNoise(i + 711, j + 1194, 0.33f, 1, 1f, 1f));
				for (int num10 = 0; num10 < k2; num10++)
				{
					int x5 = i * 16 + num4.UniformInt(0, 15);
					int y5 = num4.UniformInt(2, 40);
					int z5 = j * 16 + num4.UniformInt(0, 15);
					m_sulphurBrushes[num4.UniformInt(0, m_sulphurBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z5, 67);
				}
				int num11 = (int)(0.5f + 2f * num7 * SimplexNoise.OctavedNoise(i + 432, j + 907, 0.33f, 1, 1f, 1f));
				for (int num12 = 0; num12 < num11; num12++)
				{
					int x6 = i * 16 + num4.UniformInt(0, 15);
					int y6 = num4.UniformInt(2, 15);
					int z6 = j * 16 + num4.UniformInt(0, 15);
					m_diamondBrushes[num4.UniformInt(0, m_diamondBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z6, 67);
				}
				int num13 = (int)(3f + 2f * num7 * SimplexNoise.OctavedNoise(i + 799, j + 131, 0.33f, 1, 1f, 1f));
				for (int num14 = 0; num14 < num13; num14++)
				{
					int x7 = i * 16 + num4.UniformInt(0, 15);
					int y7 = num4.UniformInt(2, 50);
					int z7 = j * 16 + num4.UniformInt(0, 15);
					m_germaniumBrushes[num4.UniformInt(0, m_germaniumBrushes.Count - 1)].PaintFastSelective(chunk, x7, y7, z7, 67);
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
				int num3 = random.UniformInt(0, 10);
				for (int k = 0; k < num3; k++)
				{
					random.UniformInt(0, 1);
				}
				float num4 = CalculateMountainRangeFactor(num * 16, num2 * 16);
				for (int l = 0; l < 3; l++)
				{
					int x = num * 16 + random.UniformInt(0, 15);
					int y = random.UniformInt(50, 100);
					int z = num2 * 16 + random.UniformInt(0, 15);
					TerrainBrush terrainBrush = m_dirtPocketBrushes[random.UniformInt(0, m_dirtPocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush, new Game.Random(random.Int()));
					terrainBrush.PaintFastSelective(chunk, x, y, z, 3);
				}
				for (int m = 0; m < 10; m++)
				{
					int x2 = num * 16 + random.UniformInt(0, 15);
					int y2 = random.UniformInt(20, 80);
					int z2 = num2 * 16 + random.UniformInt(0, 15);
					TerrainBrush terrainBrush2 = m_gravelPocketBrushes[random.UniformInt(0, m_gravelPocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush2, new Game.Random(random.Int()));
					terrainBrush2.PaintFastSelective(chunk, x2, y2, z2, 3);
				}
				for (int n = 0; n < 6; n++)
				{
					int x3 = num * 16 + random.UniformInt(0, 15);
					int y3 = random.UniformInt(40, 80);
					int z3 = num2 * 16 + random.UniformInt(0, 15);
					TerrainBrush terrainBrush3 = m_sandPocketBrushes[random.UniformInt(0, m_sandPocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush3, new Game.Random(random.Int()));
					terrainBrush3.PaintFastSelective(chunk, x3, y3, z3, 4);
				}
				for (int num5 = 0; num5 < 6; num5++)
				{
					int x4 = num * 16 + random.UniformInt(0, 15);
					int y4 = random.UniformInt(4, 50);
					int z4 = num2 * 16 + random.UniformInt(0, 15);
					TerrainBrush terrainBrush4 = m_granitePocketBrushes[random.UniformInt(0, m_granitePocketBrushes.Count - 1)];
					ApplyRandomRotationToBrush(terrainBrush4, new Game.Random(random.Int()));
					terrainBrush4.PaintFastSelective(chunk, x4, y4, z4, 67);
				}
				for (int num6 = 0; num6 < 2; num6++)
				{
					int x5 = num * 16 + random.UniformInt(0, 15);
					int y5 = random.UniformInt(20, 120);
					int z5 = num2 * 16 + random.UniformInt(0, 15);
					m_limestonePocketBrushes[random.UniformInt(0, m_limestonePocketBrushes.Count - 1)].PaintFastSelective(chunk, x5, y5, z5, 3);
				}
				for (int num7 = 0; num7 < 1; num7++)
				{
					int x6 = num * 16 + random.UniformInt(0, 15);
					int y6 = random.UniformInt(50, 70);
					int z6 = num2 * 16 + random.UniformInt(0, 15);
					m_clayPocketBrushes[random.UniformInt(0, m_clayPocketBrushes.Count - 1)].PaintFastSelective(chunk, x6, y6, z6, 3);
				}
				for (int num8 = 0; num8 < 4; num8++)
				{
					int x7 = num * 16 + random.UniformInt(0, 15);
					int y7 = random.UniformInt(40, 60);
					int z7 = num2 * 16 + random.UniformInt(0, 15);
					m_basaltPocketBrushes[random.UniformInt(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x7, y7, z7, 4);
				}
				for (int num9 = 0; num9 < 3; num9++)
				{
					int x8 = num * 16 + random.UniformInt(0, 15);
					int y8 = random.UniformInt(20, 40);
					int z8 = num2 * 16 + random.UniformInt(0, 15);
					m_basaltPocketBrushes[random.UniformInt(0, m_basaltPocketBrushes.Count - 1)].PaintFastSelective(chunk, x8, y8, z8, 3);
				}
				if (random.Bool(0.02f + 0.01f * num4))
				{
					int num10 = num * 16;
					int num11 = random.UniformInt(40, 60);
					int num12 = num2 * 16;
					int num13 = random.UniformInt(1, 3);
					for (int num14 = 0; num14 < num13; num14++)
					{
						Vector2 vector = random.Vector2(7f);
						int num15 = 8 + (int)MathUtils.Round(vector.X);
						int num16 = 0;
						int num17 = 8 + (int)MathUtils.Round(vector.Y);
						m_waterPocketBrushes[random.UniformInt(0, m_waterPocketBrushes.Count - 1)].PaintFast(chunk, num10 + num15, num11 + num16, num12 + num17);
					}
				}
				if (random.Bool(0.06f + 0.05f * num4))
				{
					int num18 = num * 16;
					int num19 = random.UniformInt(15, 42);
					int num20 = num2 * 16;
					int num21 = random.UniformInt(1, 2);
					for (int num22 = 0; num22 < num21; num22++)
					{
						Vector2 vector2 = random.Vector2(7f);
						int num23 = 8 + (int)MathUtils.Round(vector2.X);
						int num24 = random.UniformInt(0, 1);
						int num25 = 8 + (int)MathUtils.Round(vector2.Y);
						m_magmaPocketBrushes[random.UniformInt(0, m_magmaPocketBrushes.Count - 1)].PaintFast(chunk, num18 + num23, num19 + num24, num20 + num25);
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
		List<CavePoint> num = new List<CavePoint>();
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		for (int i = x - 2; i <= x + 2; i++)
		{
			for (int j = y - 2; j <= y + 2; j++)
			{
				num.Clear();
				Random random = new Random(m_seed + i + 9973 * j);
				int num2 = i * 16 + random.UniformInt(0, 15);
				int num3 = j * 16 + random.UniformInt(0, 15);
				float probability = 0.5f;
				if (!random.Bool(probability))
				{
					continue;
				}
				int num4 = (int)CalculateHeight(num2, num3);
				int num5 = (int)CalculateHeight(num2 + 3, num3);
				int num6 = (int)CalculateHeight(num2, num3 + 3);
				Vector3 position = new Vector3(num2, num4 - 1, num3);
				Vector3 v = new Vector3(3f, num5 - num4, 0f);
				Vector3 v2 = new Vector3(0f, num6 - num4, 3f);
				Vector3 direction = Vector3.Normalize(Vector3.Cross(v, v2));
				if (direction.Y > -0.6f)
				{
					num.Add(new CavePoint
					{
						Position = position,
						Direction = direction,
						BrushType = 0,
						Length = random.UniformInt(80, 240)
					});
				}
				int num7 = i * 16 + 8;
				int num8 = j * 16 + 8;
				int num9 = 0;
				while (num9 < num.Count)
				{
					CavePoint cavePoint = num[num9];
					List<TerrainBrush> list = m_caveBrushesByType[cavePoint.BrushType];
					list[random.UniformInt(0, list.Count - 1)].PaintFastAvoidWater(chunk, Terrain.ToCell(cavePoint.Position.X), Terrain.ToCell(cavePoint.Position.Y), Terrain.ToCell(cavePoint.Position.Z));
					cavePoint.Position += 2f * cavePoint.Direction;
					cavePoint.StepsTaken += 2;
					float num10 = cavePoint.Position.X - (float)num7;
					float num11 = cavePoint.Position.Z - (float)num8;
					if (random.Bool(0.5f))
					{
						Vector3 vector = Vector3.Normalize(random.Vector3(1f, spherical: true));
						if ((num10 < -25.5f && vector.X < 0f) || (num10 > 25.5f && vector.X > 0f))
						{
							vector.X = 0f - vector.X;
						}
						if ((num11 < -25.5f && vector.Z < 0f) || (num11 > 25.5f && vector.Z > 0f))
						{
							vector.Z = 0f - vector.Z;
						}
						if ((cavePoint.Direction.Y < -0.5f && vector.Y < -10f) || (cavePoint.Direction.Y > 0.1f && vector.Y > 0f))
						{
							vector.Y = 0f - vector.Y;
						}
						cavePoint.Direction = Vector3.Normalize(cavePoint.Direction + 0.5f * vector);
					}
					if (cavePoint.StepsTaken > 20 && random.Bool(0.06f))
					{
						cavePoint.Direction = Vector3.Normalize(random.Vector3(1f, spherical: true) * new Vector3(1f, 0.33f, 1f));
					}
					if (cavePoint.StepsTaken > 20 && random.Bool(0.05f))
					{
						cavePoint.Direction.Y = 0f;
						cavePoint.BrushType = MathUtils.Min(cavePoint.BrushType + 2, m_caveBrushesByType.Count - 1);
					}
					if (cavePoint.StepsTaken > 30 && random.Bool(0.03f))
					{
						cavePoint.Direction.X = 0f;
						cavePoint.Direction.Y = -1f;
						cavePoint.Direction.Z = 0f;
					}
					if (cavePoint.StepsTaken > 30 && cavePoint.Position.Y < 30f && random.Bool(0.02f))
					{
						cavePoint.Direction.X = 0f;
						cavePoint.Direction.Y = 1f;
						cavePoint.Direction.Z = 0f;
					}
					if (random.Bool(0.33f))
					{
						cavePoint.BrushType = (int)(MathUtils.Pow(random.UniformFloat(0f, 0.999f), 7f) * (float)m_caveBrushesByType.Count);
					}
					if (random.Bool(0.06f) && num.Count < 12 && cavePoint.StepsTaken > 20 && cavePoint.Position.Y < 58f)
					{
						num.Add(new CavePoint
						{
							Position = cavePoint.Position,
							Direction = Vector3.Normalize(random.UniformVector3(1f, 1f) * new Vector3(1f, 0.33f, 1f)),
							BrushType = (int)(MathUtils.Pow(random.UniformFloat(0f, 0.999f), 7f) * (float)m_caveBrushesByType.Count),
							Length = random.UniformInt(40, 180)
						});
					}
					if (cavePoint.StepsTaken >= cavePoint.Length || MathUtils.Abs(num10) > 34f || MathUtils.Abs(num11) > 34f || cavePoint.Position.Y < 5f || cavePoint.Position.Y > 246f)
					{
						num9++;
					}
					else if (cavePoint.StepsTaken % 20 == 0)
					{
						float num12 = CalculateHeight(cavePoint.Position.X, cavePoint.Position.Z);
						if (cavePoint.Position.Y > num12 + 1f)
						{
							num9++;
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
		Terrain i = m_subsystemTerrain.Terrain;
		int num = chunk.Origin.X;
		int cellValueFast = num + 16;
		int num2 = chunk.Origin.Y;
		int focalorsFluidBlock = num2 + 16;
		int level = chunk.Coords.X;
		int y = chunk.Coords.Y;
		for (int j = level; j <= level; j++)
		{
			for (int k = y; k <= y; k++)
			{
				Game.Random random = new Game.Random(m_seed + j + 3943 * k);
				int humidity = CalculateHumidity(j * 16, k * 16);
				int num3 = CalculateTemperature(j * 16, k * 16);
				float num4 = MathUtils.Saturate((SimplexNoise.OctavedNoise(j, k, 0.1f, 2, 2f, 0.5f) - 0.25f) / 0.2f + (random.Bool(0.25f) ? 0.5f : 0f));
				int num5 = 0;
				if (num4 > 0.95f)
				{
					num5 = 1 + (random.Bool(0.25f) ? 1 : 0);
				}
				else if (num4 > 0.5f)
				{
					num5 = (random.Bool(0.25f) ? 1 : 0);
				}
				int num6 = 0;
				for (int l = 0; l < 8; l++)
				{
					if (num6 >= num5)
					{
						break;
					}
					int num7 = j * 16 + random.Int(0, 15);
					int num8 = k * 16 + random.Int(0, 15);
					int num9 = i.CalculateTopmostCellHeight(num7, num8);
					if (num9 < 66)
					{
						continue;
					}
					int cellContentsFast = i.GetCellContentsFast(num7, num9, num8);
					if (cellContentsFast != 2 && cellContentsFast != 8)
					{
						continue;
					}
					num9++;
					int num10 = random.Int(3, 7);
					Point3 point = CellFace.FaceToPoint3(random.Int(0, 3));
					if (point.X < 0 && num7 - num10 + 1 < 0)
					{
						point.X *= -1;
					}
					if (point.X > 0 && num7 + num10 - 1 > 15)
					{
						point.X *= -1;
					}
					if (point.Z < 0 && num8 - num10 + 1 < 0)
					{
						point.Z *= -1;
					}
					if (point.Z > 0 && num8 + num10 - 1 > 15)
					{
						point.Z *= -1;
					}
					bool flag = true;
					bool flag2 = false;
					bool flag3 = false;
					for (int m = 0; m < num10; m++)
					{
						int num11 = num7 + point.X * m;
						int num12 = num8 + point.Z * m;
						if (num11 < num + 1 || num11 >= cellValueFast - 1 || num12 < num2 + 1 || num12 >= focalorsFluidBlock - 1)
						{
							flag = false;
							break;
						}
						if (BlocksManager.Blocks[i.GetCellContentsFast(num11, num9, num12)].IsCollidable)
						{
							flag = false;
							break;
						}
						if (BlocksManager.Blocks[i.GetCellContentsFast(num11, num9 - 1, num12)].IsCollidable)
						{
							if (m <= MathUtils.Max(num10 / 2, 0))
							{
								flag2 = true;
							}
							if (m >= MathUtils.Min(num10 / 2 + 1, num10 - 1))
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
					TreeType? treeType = PlantsManager.GenerateRandomTreeType(random, num3 + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num9), humidity, num9);
					if (treeType.HasValue)
					{
						int treeTrunkValue = PlantsManager.GetTreeTrunkValue(treeType.Value);
						treeTrunkValue = Terrain.ReplaceData(treeTrunkValue, WoodBlock.SetCutFace(Terrain.ExtractData(treeTrunkValue), (point.X != 0) ? 1 : 0));
						int treeLeavesValue = PlantsManager.GetTreeLeavesValue(treeType.Value);
						for (int n = 0; n < num10; n++)
						{
							int num13 = num7 + point.X * n;
							int num14 = num8 + point.Z * n;
							i.SetCellValueFast(num13, num9, num14, treeTrunkValue);
							if (n > num10 / 2)
							{
								if (random.Bool(0.3f) && !BlocksManager.Blocks[i.GetCellContentsFast(num13 + point2.X, num9, num14 + point2.Z)].IsCollidable)
								{
									i.SetCellValueFast(num13 + point2.X, num9, num14 + point2.Z, treeLeavesValue);
								}
								if (random.Bool(0.05f) && !BlocksManager.Blocks[i.GetCellContentsFast(num13 + point2.X, num9, num14 + point2.Z)].IsCollidable)
								{
									i.SetCellValueFast(num13 + point2.X, num9, num14 + point2.Z, treeTrunkValue);
								}
								if (random.Bool(0.3f) && !BlocksManager.Blocks[i.GetCellContentsFast(num13 - point2.X, num9, num14 - point2.Z)].IsCollidable)
								{
									i.SetCellValueFast(num13 - point2.X, num9, num14 - point2.Z, treeLeavesValue);
								}
								if (random.Bool(0.05f) && !BlocksManager.Blocks[i.GetCellContentsFast(num13 - point2.X, num9, num14 - point2.Z)].IsCollidable)
								{
									i.SetCellValueFast(num13 - point2.X, num9, num14 - point2.Z, treeTrunkValue);
								}
								if (random.Bool(0.1f) && !BlocksManager.Blocks[i.GetCellContentsFast(num13, num9 + 1, num14)].IsCollidable)
								{
									i.SetCellValueFast(num13, num9 + 1, num14, treeLeavesValue);
								}
							}
						}
					}
					num6++;
				}
				int num15 = (int)(5f * num4);
				int num16 = 0;
				for (int num17 = 0; num17 < 32; num17++)
				{
					if (num16 >= num15)
					{
						break;
					}
					int num18 = j * 16 + random.Int(2, 13);
					int num19 = k * 16 + random.Int(2, 13);
					int num20 = i.CalculateTopmostCellHeight(num18, num19);
					if (num20 < 66)
					{
						continue;
					}
					int cellContentsFast2 = i.GetCellContentsFast(num18, num20, num19);
					if (cellContentsFast2 != 2 && cellContentsFast2 != 8)
					{
						continue;
					}
					num20++;
					if (!BlocksManager.Blocks[i.GetCellContentsFast(num18 + 1, num20, num19)].IsCollidable && !BlocksManager.Blocks[i.GetCellContentsFast(num18 - 1, num20, num19)].IsCollidable && !BlocksManager.Blocks[i.GetCellContentsFast(num18, num20, num19 + 1)].IsCollidable && !BlocksManager.Blocks[i.GetCellContentsFast(num18, num20, num19 - 1)].IsCollidable)
					{
						TreeType? treeType2 = PlantsManager.GenerateRandomTreeType(random, num3 + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num20), humidity, num20);
						if (treeType2.HasValue)
						{
							ReadOnlyList<TerrainBrush> treeBrushes = PlantsManager.GetTreeBrushes(treeType2.Value);
							treeBrushes[random.Int(treeBrushes.Count)].PaintFast(chunk, num18, num20, num19);
						}
						num16++;
					}
				}
				int num21 = num + 16;
				int num22 = num2 + 16;
				for (int num23 = num; num23 < num21; num23++)
				{
					for (int num24 = num2; num24 < num22; num24++)
					{
						int num25 = i.CalculateTopmostCellHeight(num23, num24);
						int cellContentsFast3 = i.GetCellContentsFast(num23, num25, num24);
						if (num25 >= 80 && num25 <= 240 && (cellContentsFast3 == 3 || cellContentsFast3 == 62 || cellContentsFast3 == 8) && !BlocksManager.Blocks[i.GetCellContentsFast(num23, num25 + 1, num24)].IsCollidable && random.Float(0f, 1f) < 0.2f)
						{
							ReadOnlyList<TerrainBrush> treeBrushes2 = PlantsManager.GetTreeBrushes(TreeType.Spruce);
							treeBrushes2[random.Int(treeBrushes2.Count)].PaintFast(chunk, num23, num25 + 1, num24);
						}
					}
				}
			}
		}
	}

	public void GenerateBedrockAndAir(TerrainChunk chunk)
	{
		int y2 = Terrain.MakeBlockValue(1);
		int cellValueFast = Terrain.MakeBlockValue(344);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				int cellValueFast4 = i + chunk.Origin.X;
				int cellValueFast5 = j + chunk.Origin.Y;
				float level = 2 + (int)(2f * SimplexNoise.OctavedNoise(cellValueFast4, cellValueFast5, 0.1f, 1, 1f, 1f));
				for (int neighborLevel2 = 0; (float)neighborLevel2 < level; neighborLevel2++)
				{
					chunk.SetCellValueFast(i, neighborLevel2, j, y2);
				}
				float neighborLevel3 = 2 + (int)(5f * SimplexNoise.OctavedNoise(cellValueFast4, cellValueFast5, 0.1f, 1, 1f, 1f));
				for (int k = 0; (float)k < neighborLevel3; k++)
				{
					chunk.SetCellValueFast(i, k + (int)level, j, cellValueFast);
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
		Game.Random random = new Game.Random(m_seed + chunk.Coords.X + 3943 * chunk.Coords.Y);
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
						if (!(BlocksManager.Blocks[num2] is FluidBlock))
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
								chunk.SetCellValueFast(i, num, j, Terrain.MakeBlockValue(8, 0, 0));
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
		Game.Random chunkAtCell = new Game.Random(m_seed + chunk.Coords.X + 2210 * chunk.Coords.Y);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (!chunkAtCell.Bool(0.2f))
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
						int face = chunkAtCell.Int(0, 5);
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
									float num10 = chunkAtCell.Float(0f, 1f);
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
										int variant = chunkAtCell.Int(0, 3);
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
		int position = chunk.Coords.X;
		int list = chunk.Coords.Y;
		Random array = new Random(m_seed + position + 1991 * list);
		if (!array.Bool(0.5f))
		{
			return;
		}
		int i = array.UniformInt(0, MathUtils.Max(1, 1));
		for (int point = 0; point < i; point++)
		{
			int x2 = array.UniformInt(3, 12);
			int num = array.UniformInt(3, 12);
			int humidityFast = chunk.GetHumidityFast(x2, num);
			int z2 = chunk.GetTemperatureFast(x2, num);
			if (humidityFast >= 6 || z2 <= 8)
			{
				continue;
			}
			for (int j = 0; j < 8; j++)
			{
				int point3 = x2 + array.UniformInt(-2, 2);
				int num2 = num + array.UniformInt(-2, 2);
				for (int num3 = 251; num3 >= 0; num3--)
				{
					switch (Terrain.ExtractContents(chunk.GetCellValueFast(point3, num3, num2)))
					{
					case 7:
					{
						for (int isTop = num3 + 1; isTop <= num3 + 3 && chunk.GetCellContentsFast(point3 + 1, isTop, num2) == 0 && chunk.GetCellContentsFast(point3 - 1, isTop, num2) == 0 && chunk.GetCellContentsFast(point3, isTop, num2 + 1) == 0 && chunk.GetCellContentsFast(point3, isTop, num2 - 1) == 0; isTop++)
						{
							chunk.SetCellValueFast(point3, isTop, num2, Terrain.MakeBlockValue(127));
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
		int terrain = chunk.Coords.X;
		int num = chunk.Coords.Y;
		Random data = new Random(m_seed + terrain + 1495 * num);
		if (!data.Bool(0.2f))
		{
			return;
		}
		int num2 = data.UniformInt(0, MathUtils.Max(1, 1));
		for (int flag = 0; flag < num2; flag++)
		{
			int displacementOffsets = data.UniformInt(1, 14);
			int i = data.UniformInt(1, 14);
			int humidityFast = chunk.GetHumidityFast(displacementOffsets, i);
			int point = chunk.GetTemperatureFast(displacementOffsets, i);
			if (humidityFast < 10 || point <= 6)
			{
				continue;
			}
			for (int j = 0; j < 5; j++)
			{
				int num4 = displacementOffsets + data.UniformInt(-1, 1);
				int num5 = i + data.UniformInt(-1, 1);
				for (int cellValueFast = 254; cellValueFast >= 0; cellValueFast--)
				{
					switch (Terrain.ExtractContents(chunk.GetCellValueFast(num4, cellValueFast, num5)))
					{
					case 8:
						chunk.SetCellValueFast(num4, cellValueFast + 1, num5, data.Bool(0.25f) ? Terrain.MakeBlockValue(244) : Terrain.MakeBlockValue(131));
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
			random.Reset(m_seed + x + num2 + 850 * (y + num3));
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
		random.Reset(m_seed + x + 850 * y);
		int num4 = random.UniformInt(0, MathUtils.Max((int)(256f * num), 1));
		for (int j = 0; j < num4; j++)
		{
			int num5 = random.UniformInt(2, 13);
			int num6 = random.UniformInt(2, 13);
			int num7 = num5 + chunk.Origin.X;
			int num8 = num6 + chunk.Origin.Y;
			int num9 = random.UniformInt(10, 26);
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
				int x2 = num5 + random.UniformInt(-2, 2);
				int z = num6 + random.UniformInt(-2, 2);
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
								int num14 = (flag ? random.UniformInt(num11 - 2, num11 - 1) : random.UniformInt(num11 - 1, num11));
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
		int terrain = chunk.Coords.X;
		int num = chunk.Coords.Y;
		Random i = new Random(m_seed + terrain + 378 * num);
		for (int j = 0; j < 6; j++)
		{
			int block = i.UniformInt(1, 14);
			int position = i.UniformInt(1, 14);
			int num3 = chunk.Origin.X + block;
			int cellValueFast = chunk.Origin.Y + position;
			bool flag = CalculateOceanShoreDistance(num3, cellValueFast) < 10f;
			int num4 = i.UniformInt(1, 3);
			for (int k = 0; k < num4; k++)
			{
				int x = block + i.UniformInt(-1, 1);
				int z = position + i.UniformInt(-1, 1);
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
							int x2 = ((!i.Bool(0.1f)) ? 1 : 2);
							x2 = (flag ? MathUtils.Min(x2, num5 - 1) : MathUtils.Min(x2, num5));
							for (int l = 0; l < x2; l++)
							{
								chunk.SetCellValueFast(x, num6 + 1 + l, z, Terrain.MakeBlockValue(233));
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
		Random point = new Random(m_seed + chunk.Coords.X + 2191 * chunk.Coords.Y);
		int gameTime = point.UniformInt(0, MathUtils.Max(12, 1));
		for (int num = 0; num < gameTime; num++)
		{
			int count = point.UniformInt(4, 11);
			int num3 = point.UniformInt(4, 11);
			int humidityFast = chunk.GetHumidityFast(count, num3);
			int value = chunk.GetTemperatureFast(count, num3);
			if (humidityFast <= 10 || value <= 10)
			{
				continue;
			}
			int num4 = chunk.CalculateTopmostCellHeight(count, num3);
			for (int i = 0; i < 100; i++)
			{
				int componentPlayer = count + point.UniformInt(-3, 3);
				int position = MathUtils.Clamp(num4 + point.UniformInt(-10, 1), 1, 255);
				int i2 = num3 + point.UniformInt(-3, 3);
				int point2 = Terrain.ExtractContents(chunk.GetCellValueFast(componentPlayer, position, i2));
				if (point2 <= 9)
				{
					if (point2 - 2 > 1 && point2 - 8 > 1)
					{
						continue;
					}
				}
				else if (point2 != 12 && point2 - 66 > 1)
				{
					continue;
				}
				int num5 = point.UniformInt(0, 3);
				for (int j = 0; j < 4; j++)
				{
					int face = (j + num5) % 4;
					Point3 point3 = CellFace.FaceToPoint3(face);
					if (chunk.GetCellContentsFast(componentPlayer + point3.X, position, i2 + point3.Z) != 0)
					{
						continue;
					}
					int num6 = position - 1;
					while (num6 >= 1 && chunk.GetCellContentsFast(componentPlayer + point3.X, num6, i2 + point3.Z) == 0 && chunk.GetCellContentsFast(componentPlayer, num6, i2) != 0)
					{
						num6--;
					}
					if (chunk.GetCellContentsFast(componentPlayer + point3.X, num6, i2 + point3.Z) != 0)
					{
						break;
					}
					num6++;
					int value2 = Terrain.MakeBlockValue(197, 0, IvyBlock.SetFace(0, CellFace.OppositeFace(face)));
					while (num6 >= 1 && chunk.GetCellContentsFast(componentPlayer + point3.X, num6, i2 + point3.Z) == 0)
					{
						chunk.SetCellValueFast(componentPlayer + point3.X, num6, i2 + point3.Z, value2);
						if (IvyBlock.IsGrowthStopCell(componentPlayer + point3.X, num6, i2 + point3.Z))
						{
							break;
						}
						num6--;
					}
					break;
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
		if (!random.Bool(0.15f) || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 50f)
		{
			return;
		}
		int num = random.UniformInt(0, MathUtils.Max(2, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.UniformInt(2, 5);
			int num3 = random.UniformInt(2, 5);
			int num4 = random.UniformInt(1, 16 - num2 - 2);
			int num5 = random.UniformInt(1, 16 - num3 - 2);
			bool flag = random.UniformFloat(0f, 1f) < 0.5f;
			int num6 = random.UniformInt(3, 5);
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
						int data = SpikedPlankBlock.SetSpikesState(0, random.UniformFloat(0f, 1f) < 0.33f);
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
		if (random.UniformFloat(0f, 1f) >= 0.033f || CalculateOceanShoreDistance(chunk.Origin.X, chunk.Origin.Y) <= 10f)
		{
			return;
		}
		int num = random.UniformInt(0, MathUtils.Max(1, 1));
		for (int i = 0; i < num; i++)
		{
			int num2 = random.UniformInt(6, 9);
			int num3 = random.UniformInt(6, 9);
			int num4 = (random.Bool(0.2f) ? random.UniformInt(6, 20) : random.UniformInt(1, 5));
			bool flag = random.Bool(0.5f);
			for (int j = 0; j < num4; j++)
			{
				int num5 = num2 + random.UniformInt(-4, 4);
				int num6 = num3 + random.UniformInt(-4, 4);
				int num7 = chunk.CalculateTopmostCellHeight(num5, num6);
				if (num7 < 10 || num7 > 246)
				{
					continue;
				}
				int num8 = random.UniformInt(0, 3);
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
					int num15 = random.UniformInt(0, 7);
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
						else if (random.UniformFloat(0f, 1f) < 0.5f)
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
					float num17 = random.UniformFloat(0f, 1f);
					float num18 = random.UniformFloat(0f, 1f);
					int num19 = random.UniformInt(-1, 0);
					int num20 = random.UniformInt(1, 2);
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
		for (int num = 0; num < 16; num++)
		{
			for (int i = 0; i < 16; i++)
			{
				int num2 = TerrainChunk.CalculateCellIndex(num, 255, i);
				int listenerPosition = 0;
				int num3 = 255;
				while (num3 >= 0)
				{
					int cellValueFast = chunk.GetCellValueFast(num2);
					int num4 = Terrain.ExtractContents(cellValueFast);
					if (num4 != listenerPosition && BlocksManager.FluidBlocks[num4] != null && BlocksManager.FluidBlocks[listenerPosition] == null)
					{
						int data = Terrain.ExtractData(cellValueFast);
						chunk.SetCellValueFast(num2, Terrain.MakeBlockValue(num4, 0, FluidBlock.SetIsTop(data, isTop: true)));
					}
					listenerPosition = num4;
					num3--;
					num2--;
				}
			}
		}
	}

	public static void CreateBrushes()
	{
		Random neighborOffsets = new Random(17);
		for (int i = 0; i < 16; i++)
		{
			TerrainBrush terrainBrush = new TerrainBrush();
			int num = neighborOffsets.UniformInt(4, 12);
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num2 = neighborOffsets.UniformInt(3, 8);
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
			int num3 = neighborOffsets.UniformInt(3, 7);
			for (int m = 0; m < num3; m++)
			{
				Vector3 vector2 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num4 = neighborOffsets.UniformInt(3, 6);
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
			int num6 = neighborOffsets.UniformInt(4, 10);
			for (int num7 = 0; num7 < num6; num7++)
			{
				Vector3 vector3 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-2f, 2f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num8 = neighborOffsets.UniformInt(3, 6);
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
			int num11 = neighborOffsets.UniformInt(8, 16);
			for (int num12 = 0; num12 < num11; num12++)
			{
				Vector3 vector4 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.25f, 0.25f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num13 = neighborOffsets.UniformInt(4, 8);
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
			int num16 = neighborOffsets.UniformInt(4, 10);
			for (int num17 = 0; num17 < num16; num17++)
			{
				Vector3 vector5 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num18 = neighborOffsets.UniformInt(3, 6);
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
			int num21 = neighborOffsets.UniformInt(2, 6);
			for (int num22 = 0; num22 < num21; num22++)
			{
				Vector3 vector6 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num23 = neighborOffsets.UniformInt(3, 6);
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
			int num26 = neighborOffsets.UniformInt(4, 10);
			for (int num27 = 0; num27 < num26; num27++)
			{
				Vector3 vector7 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num28 = neighborOffsets.UniformInt(3, 6);
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
			int num31 = neighborOffsets.UniformInt(16, 32);
			for (int num32 = 0; num32 < num31; num32++)
			{
				Vector3 vector8 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.75f, 0.75f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num33 = neighborOffsets.UniformInt(6, 12);
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
			int num36 = neighborOffsets.UniformInt(16, 32);
			for (int num37 = 0; num37 < num36; num37++)
			{
				Vector3 vector9 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.75f, 0.75f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num38 = neighborOffsets.UniformInt(6, 12);
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
			int num41 = neighborOffsets.UniformInt(16, 32);
			for (int num42 = 0; num42 < num41; num42++)
			{
				Vector3 vector10 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.75f, 0.75f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num43 = neighborOffsets.UniformInt(6, 12);
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
			int num46 = neighborOffsets.UniformInt(16, 32);
			for (int num47 = 0; num47 < num46; num47++)
			{
				Vector3 vector11 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.1f, 0.1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num48 = neighborOffsets.UniformInt(6, 12);
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
			int num51 = neighborOffsets.UniformInt(16, 32);
			for (int num52 = 0; num52 < num51; num52++)
			{
				Vector3 vector12 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.75f, 0.75f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num53 = neighborOffsets.UniformInt(6, 12);
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
			int num56 = neighborOffsets.UniformInt(16, 32);
			for (int num57 = 0; num57 < num56; num57++)
			{
				Vector3 vector13 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-0.75f, 0.75f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num58 = neighborOffsets.UniformInt(6, 12);
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
			int num61 = neighborOffsets.UniformInt(16, 32);
			for (int num62 = 0; num62 < num61; num62++)
			{
				Vector3 vector14 = 0.5f * Vector3.Normalize(new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(-1f, 1f)));
				int num63 = neighborOffsets.UniformInt(5, 10);
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
		int[] i2 = new int[3] { 4, 6, 8 };
		for (int num65 = 0; num65 < 4 * i2.Length; num65++)
		{
			TerrainBrush terrainBrush15 = new TerrainBrush();
			int num66 = i2[num65 / 4];
			int num67 = num65 % 2 + 1;
			float num68 = ((num65 % 4 == 2) ? 0.5f : 1f);
			bool circular = num65 % 4 >= 2;
			int num69 = ((num65 % 4 == 1) ? (num66 * num66) : (2 * num66 * num66));
			for (int num70 = 0; num70 < num69; num70++)
			{
				Vector2 vector15 = neighborOffsets.UniformVector2(0f, num66, circular);
				float num71 = vector15.Length();
				int num72 = neighborOffsets.UniformInt(3, 4);
				int sizeY = 1 + (int)MathUtils.Lerp(MathUtils.Max(num66 / 3, 2.5f) * num68, 0f, num71 / (float)num66) + neighborOffsets.UniformInt(0, 1);
				terrainBrush15.AddBox((int)MathUtils.Floor(vector15.X), 0, (int)MathUtils.Floor(vector15.Y), num72, sizeY, num72, 0);
				terrainBrush15.AddBox((int)MathUtils.Floor(vector15.X), -num67, (int)MathUtils.Floor(vector15.Y), num72, num67, num72, 18);
			}
			terrainBrush15.Compile();
			m_waterPocketBrushes.Add(terrainBrush15);
		}
		int[] point = new int[4] { 8, 12, 14, 16 };
		for (int num73 = 0; num73 < 4 * point.Length; num73++)
		{
			TerrainBrush terrainBrush16 = new TerrainBrush();
			int num74 = point[num73 / 4];
			int num75 = num74 + 2;
			float num76 = ((num73 % 4 == 2) ? 0.5f : 1f);
			bool circular2 = num73 % 4 >= 2;
			int num77 = ((num73 % 4 == 1) ? (num74 * num74) : (2 * num74 * num74));
			for (int num78 = 0; num78 < num77; num78++)
			{
				Vector2 vector16 = neighborOffsets.UniformVector2(0f, num74, circular2);
				float num79 = vector16.Length();
				int num80 = neighborOffsets.UniformInt(3, 4);
				int sizeY2 = 1 + (int)MathUtils.Lerp(MathUtils.Max(num74 / 3, 2.5f) * num76, 0f, num79 / (float)num74) + neighborOffsets.UniformInt(0, 1);
				int num81 = 1 + (int)MathUtils.Lerp(num75, 0f, num79 / (float)num74) + neighborOffsets.UniformInt(0, 1);
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
					int num86 = neighborOffsets.UniformInt(2, max);
					int num87 = neighborOffsets.UniformInt(8, max2) - 2 * num86;
					Vector3 vector17 = 0.5f * new Vector3(neighborOffsets.UniformFloat(-1f, 1f), neighborOffsets.UniformFloat(0f, 1f), neighborOffsets.UniformFloat(-1f, 1f));
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
				Game.Random random = new Game.Random(m_seed + i + 3943 * j);
				float probability = 0.01f;
				if (!random.Bool(probability))
				{
					continue;
				}
				int num3 = random.Int(1, 4);
				int num4 = i * 16 + random.Int(0, 15);
				int num5 = j * 16 + random.Int(0, 15);
				int num6 = terrain.CalculateTopmostCellHeight(num4, num5);
				if (num6 < 66)
				{
					continue;
				}
				int cellContentsFast = terrain.GetCellContentsFast(num4, num6, num5);
				if (cellContentsFast != 2 && cellContentsFast != 8)
				{
					continue;
				}
				num6++;
				int num7 = num4;
				int num8 = num6;
				int num9 = num5;
				int value = 67;
				if (random.Bool(0.5f))
				{
					float num10 = random.Float(0f, 1f);
					if (num10 < 0.33f)
					{
						value = 39;
					}
					else
					{
						value = ((!(num10 < 0.66f)) ? 148 : 101);
					}
				}
				for (int k = -num3; k <= num3; k++)
				{
					for (int l = -num3; l <= num3; l++)
					{
						for (int m = -num3; m <= num3; m++)
						{
							if (!(random.Float(0f, 1f) > 0.1f) || k * k + l * l + m * m > num3 * num3)
							{
								continue;
							}
							int num11 = num7 + k;
							int num12 = num8 + l;
							int num13 = num9 + m;
							if (num11 >= x && num11 < num && num13 >= y && num13 < num2 && num12 >= 0 && num12 < 128)
							{
								if (k == 0 && l == 0 && m == 0)
								{
									terrain.SetCellValueFast(num11, num12, num13, value);
								}
								else
								{
									terrain.SetCellValueFast(num11, num12, num13, 67);
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
		Terrain terrain = m_subsystemTerrain.Terrain;
		int x = chunk.Coords.X;
		int y = chunk.Coords.Y;
		for (int i = x; i <= x; i++)
		{
			for (int j = y; j <= y; j++)
			{
				Game.Random random = new Game.Random(m_seed + i + 3943 * j);
				bool flag = random.Bool(0.01f);
				if (!random.Bool(0.04f) && !flag)
				{
					return;
				}
				int num = i * 16 + random.Int(0, 15);
				int num2 = j * 16 + random.Int(0, 15);
				int num3 = terrain.CalculateTopmostCellHeight(num, num2);
				if (terrain.GetCellContentsFast(num, num3, num2) != 7)
				{
					continue;
				}
				int num4 = random.Int(4, 5) + (flag ? 2 : 0);
				int contents = (random.Bool(0.5f) ? 4 : 7);
				for (int k = 0; k < num4; k++)
				{
					if (terrain.GetCellContentsFast(num, num3 + k, num2) == 0)
					{
						terrain.SetCellValueFast(num, num3 + k, num2, Terrain.MakeBlockValue(contents));
					}
				}
				int num5 = 5;
				for (int l = 0; l < num5; l++)
				{
					int num6 = random.Int(2, 3);
					int num7 = random.Int(-2, 2);
					int num8 = random.Int(-2, 2);
					if (!random.Bool(0.8f) && Math.Abs(num7) <= 1 && Math.Abs(num8) <= 1)
					{
						continue;
					}
					for (int m = 0; m < num6; m++)
					{
						if (terrain.GetCellContentsFast(num + num7, num3 + m, num2 + num8) == 0)
						{
							terrain.SetCellValueFast(num + num7, num3 + m, num2 + num8, Terrain.MakeBlockValue(contents));
						}
					}
				}
				int num9 = 10;
				for (int n = 0; n < num9; n++)
				{
					int x2 = num + random.Int(-3, 3);
					int z = num2 + random.Int(-3, 3);
					int num10 = random.Int(1, 2);
					for (int num11 = 0; num11 < num10; num11++)
					{
						if (terrain.GetCellContentsFast(x2, num3 + num11, z) == 0)
						{
							terrain.SetCellValueFast(x2, num3 + num11, z, Terrain.MakeBlockValue(contents));
						}
					}
				}
				if (!(random.Float(0f, 1f) < 0.4f))
				{
					continue;
				}
				int num12 = ((!flag) ? 1 : 2);
				for (int num13 = 0; num13 < num12; num13++)
				{
					int x3 = num + random.Int(-1, 1);
					int y2 = num3 + random.Int(0, num4 - 1);
					int z2 = num2 + random.Int(-1, 1);
					if (random.Bool(0.5f))
					{
						y2 = num3 - 1;
					}
					if (terrain.GetCellContentsFast(x3, y2, z2) == 0)
					{
						terrain.SetCellValueFast(x3, y2, z2, Terrain.MakeBlockValue(100));
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
				Game.Random random = new Game.Random(m_seed + i + 3943 * j);
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
		Terrain particleSystem = m_subsystemTerrain.Terrain;
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
				Game.Random random = new Game.Random(m_seed + i + 3943 * j);
				float probability = 0.15f;
				if (!random.Bool(probability))
				{
					continue;
				}
				int num3 = random.Int(0, 3);
				int num4 = i * 16 + random.Int(0, 15);
				int num5 = j * 16 + random.Int(0, 15);
				int num6 = particleSystem.CalculateTopmostCellHeight(num4, num5) - 1;
				if (num6 < 65)
				{
					continue;
				}
				int cellContentsFast = particleSystem.GetCellContentsFast(num4, num6, num5);
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
					particleSystem.SetCellValueFast(num7, num8, num9, value);
					particleSystem.SetCellValueFast(num7 + 1, num8, num9, value);
					if (random.Bool(0.5f))
					{
						int num22 = random.Int(-1, 1);
						int num23 = random.Int(-1, 1);
						int num24 = random.Int(-1, 1);
						particleSystem.SetCellValueFast(num7 + num22, num8 + num23, num9 + num24, value);
					}
					if (random.Bool(0.3f))
					{
						int num25 = random.Int(3, 7);
						int num26 = random.Int(3, 7);
						particleSystem.SetCellValueFast(num7 + num25, num8, num9 + num26, value);
						particleSystem.SetCellValueFast(num7 + num25 + 1, num8, num9 + num26, value);
					}
					break;
				case 1:
				{
					for (int num27 = 0; num27 < 4 + random.Int(0, 1); num27++)
					{
						particleSystem.SetCellValueFast(num7 + num27, num8, num9, value);
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
										particleSystem.SetCellValueFast(num18, num19, num20, value);
									}
								}
							}
						}
					}
					if (random.Bool(0.3f))
					{
						for (int num21 = 0; num21 < random.Int(1, 2); num21++)
						{
							particleSystem.SetCellValueFast(num7 + random.Int(-num14, num14), num8 + random.Int(-num14, num14 / 2), num9 + random.Int(-num14, num14), 16);
						}
					}
					if (random.Bool(0.01f))
					{
						particleSystem.SetCellValueFast(num7 + random.Int(-num14, num14), num8 + random.Int(-num14, num14 / 2), num9 + random.Int(-num14, num14), 150);
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
										particleSystem.SetCellValueFast(num11, num12, num13, value);
									}
								}
							}
						}
					}
					if (random.Bool(0.3f))
					{
						for (int n = 0; n < random.Int(1, 2); n++)
						{
							particleSystem.SetCellValueFast(num7 + random.Int(-num10, num10), num8 + random.Int(-num10, num10 / 2), num9 + random.Int(-num10, num10), 16);
						}
					}
					if (random.Bool(0.01f))
					{
						particleSystem.SetCellValueFast(num7 + random.Int(-num10, num10), num8 + random.Int(-num10, num10 / 2), num9 + random.Int(-num10, num10), 150);
					}
					break;
				}
				}
			}
		}
	}

	public static void ApplyRandomRotationToBrush(TerrainBrush brush, Game.Random random)
	{
		for (int i = 0; i < brush.Cells.Length; i++)
		{
			TerrainBrush.Cell cell = brush.Cells[i];
			int value = cell.Value;
			int num = random.Int(0, 3);
			value = Terrain.ReplaceData(value, (Terrain.ExtractData(value) & -4) | (num & 3));
			brush.Cells[i] = new TerrainBrush.Cell
			{
				X = cell.X,
				Y = cell.Y,
				Z = cell.Z,
				Value = value
			};
		}
	}
}
