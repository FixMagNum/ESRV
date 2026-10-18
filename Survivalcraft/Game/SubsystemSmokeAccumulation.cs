using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemSmokeAccumulation : Subsystem, IUpdateable
{
	public class Point3EqualityComparer : IEqualityComparer<Point3>
	{
		public bool Equals(Point3 a, Point3 b)
		{
			if (a.X == b.X && a.Y == b.Y)
			{
				return a.Z == b.Z;
			}
			return false;
		}

		public int GetHashCode(Point3 p)
		{
			return (p.X * 73856093) ^ (p.Y * 19349663) ^ (p.Z * 83492791);
		}
	}

	public struct CachedVent
	{
		public float Value;

		public int Stamp;
	}

	public float m_kIn;

	public float m_kBaseDecay;

	public float m_kVentDecay;

	public float m_kWindDecay;

	public float m_kDiffusion;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemTime m_subsystemTime;

	public Dictionary<Point3, float> m_smokeByCell;

	public Dictionary<Point3, float> m_reports;

	private static readonly List<HeavyRoomSmokeParticleSystem> s_tmpParticles;

	private static readonly List<Point3> s_tmpBottomCells;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemWeather m_weather;

	private readonly Dictionary<Point3, HeavyRoomSmokeParticleSystem> m_cellToParticle;

	private readonly Dictionary<HeavyRoomSmokeParticleSystem, Point3> m_particleToCell;

	private double m_visualNextTick;

	private const float SmokeThreshold = 0.05f;

	public float m_kUp;

	public float m_kSide;

	public float m_kCeilFill;

	public float m_kDecay;

	private double m_particlesSteerNext;

	public SubsystemBlocksScanner m_subsystemBlocksScanner;

	private Dictionary<Point2, double> m_chunkGraceUntil;

	private double m_loadGraceEndTime;

	public double SmokeGraceSeconds = 5.0;

	private const float PersistEpsilon = 0.0005f;

	public DynamicArray<Point3> m_activeCells;

	public DynamicArray<Point3> m_smokeDecayCursor;

	public int m_smokeDecayCursorIndex;

	public Dictionary<int, CachedVent> m_ventCache;

	public int m_ventCacheStamp;

	private static readonly List<Point3> s_tmpProcess;

	private Dictionary<Point3, int> m_cachedVentDist;

	private int m_cachedVentHash;

	private Point3 m_cachedMin;

	private Point3 m_cachedMax;

	private double m_nextVentRebuildTime;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_weather = m_subsystemWeather;
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemBlocksScanner = Project.FindSubsystem<SubsystemBlocksScanner>(throwOnError: true);
		SmokeGraceSeconds = valuesDictionary.GetValue("SmokeGraceSeconds", 5.0);
		double num = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0);
		m_loadGraceEndTime = num + SmokeGraceSeconds;
		if (m_chunkGraceUntil == null)
		{
			m_chunkGraceUntil = new Dictionary<Point2, double>();
		}
		if (m_subsystemBlocksScanner != null)
		{
			m_subsystemBlocksScanner.ScanningChunkCompleted += (TerrainChunk chunk) =>
			{
				double num3 = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0);
				m_chunkGraceUntil[chunk.Coords] = num3 + SmokeGraceSeconds;
			};
		}
		ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("SmokeCells", null);
		m_smokeByCell.Clear();
		m_reports.Clear();
		m_activeCells.Clear();
		if (value != null)
		{
			foreach (object value2 in value.Values)
			{
				string text = value2 as string;
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				string[] array = text.Split(',');
				if (array.Length == 4 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2) && int.TryParse(array[2], out var result3) && float.TryParse(array[3], out var result4))
				{
					Point3 point = new Point3(result, result2, result3);
					float num2 = MathUtils.Saturate(result4);
					if (num2 > 0.0005f)
					{
						m_smokeByCell[point] = num2;
						m_activeCells.Add(point);
					}
				}
			}
		}
		m_visualNextTick = num + 0.25;
	}

	public float GetSmokeLevel(int x, int y, int z)
	{
		if (m_smokeByCell.TryGetValue(new Point3(x, y, z), out var value))
		{
			return MathUtils.Saturate(value);
		}
		return 0f;
	}

	public void Update(float dt)
	{
		dt = MathUtils.Clamp(dt, 0f, 1f);
		if (m_subsystemTime != null && m_subsystemTime.GameTime < m_loadGraceEndTime)
		{
			dt = MathUtils.Min(dt, 0.05f);
		}
		float num = m_kSide * dt;
		s_tmpProcess.Clear();
		foreach (KeyValuePair<Point3, float> item in m_smokeByCell)
		{
			if (item.Value > 0.0005f)
			{
				s_tmpProcess.Add(item.Key);
			}
		}
		for (int i = 0; i < m_activeCells.Count; i++)
		{
			Point3 point = m_activeCells.Array[i];
			if (!m_smokeByCell.ContainsKey(point))
			{
				s_tmpProcess.Add(point);
			}
		}
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		int num4 = int.MaxValue;
		int num5 = int.MinValue;
		int num6 = int.MinValue;
		int num7 = int.MinValue;
		for (int j = 0; j < s_tmpProcess.Count; j++)
		{
			Point3 point2 = s_tmpProcess[j];
			if (point2.X < num2)
			{
				num2 = point2.X;
			}
			if (point2.Y < num3)
			{
				num3 = point2.Y;
			}
			if (point2.Z < num4)
			{
				num4 = point2.Z;
			}
			if (point2.X > num5)
			{
				num5 = point2.X;
			}
			if (point2.Y > num6)
			{
				num6 = point2.Y;
			}
			if (point2.Z > num7)
			{
				num7 = point2.Z;
			}
		}
		if (num2 == int.MaxValue)
		{
			num5 = (num2 = (num3 = (num6 = (num4 = (num7 = 0)))));
		}
		num2 -= 8;
		num3 = MathUtils.Clamp(num3 - 8, 0, 255);
		num4 -= 8;
		num5 += 8;
		num6 = MathUtils.Clamp(num6 + 8, 0, 255);
		num7 += 8;
		Dictionary<Point3, bool> dictionary = new Dictionary<Point3, bool>(new Point3EqualityComparer());
		Terrain terrain = ((m_subsystemTerrain != null) ? m_subsystemTerrain.Terrain : null);
		int num8 = 0;
		if (terrain != null)
		{
			for (int k = 0; k < s_tmpProcess.Count; k++)
			{
				Point3 point3 = s_tmpProcess[k];
				TerrainChunk chunkAtCell = terrain.GetChunkAtCell(point3.X, point3.Z);
				bool flag = IsInGrace(point3);
				bool flag2;
				if (chunkAtCell != null)
				{
					int x = point3.X & 0xF;
					int z = point3.Z & 0xF;
					int topHeightFast = chunkAtCell.GetTopHeightFast(x, z);
					flag2 = point3.Y >= topHeightFast;
				}
				else
				{
					flag2 = !flag;
					if (!flag)
					{
						for (int l = point3.Y + 1; l <= 255; l++)
						{
							int num9 = Terrain.ExtractContents(terrain.GetCellValue(point3.X, l, point3.Z));
							if (BlocksManager.Blocks[num9].IsCollidable)
							{
								flag2 = false;
								break;
							}
						}
					}
				}
				if (!flag && !flag2 && m_subsystemWeather != null && m_subsystemWeather.CurrentWindStrength > 6f)
				{
					Vector3 position = new Vector3((float)point3.X + 0.5f, (float)point3.Y + 0.5f, (float)point3.Z + 0.5f);
					if (m_subsystemWeather.GetWindExposureFactor(position) > 0f)
					{
						flag2 = true;
					}
				}
				if (flag2 && !flag)
				{
					dictionary[point3] = true;
					num8 ^= (point3.X * 73856093) ^ (point3.Y * 19349663) ^ (point3.Z * 83492791);
				}
			}
		}
		bool flag3 = dictionary.Count > 0;
		Dictionary<Point3, int> cachedVentDist = m_cachedVentDist;
		bool flag4 = false;
		if (flag3)
		{
			double num10 = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0);
			if (cachedVentDist.Count == 0)
			{
				flag4 = true;
			}
			if (num8 != m_cachedVentHash)
			{
				flag4 = true;
			}
			if (num10 >= m_nextVentRebuildTime)
			{
				flag4 = true;
			}
			if (!flag4 && (num2 < m_cachedMin.X - 2 || num3 < m_cachedMin.Y - 2 || num4 < m_cachedMin.Z - 2 || num5 > m_cachedMax.X + 2 || num6 > m_cachedMax.Y + 2 || num7 > m_cachedMax.Z + 2))
			{
				flag4 = true;
			}
			if (flag4)
			{
				cachedVentDist.Clear();
				List<Point3> list = new List<Point3>(dictionary.Count * 8 + 16);
				foreach (KeyValuePair<Point3, bool> item2 in dictionary)
				{
					cachedVentDist[item2.Key] = 0;
					list.Add(item2.Key);
				}
				for (int m = 0; m < list.Count; m++)
				{
					Point3 key = list[m];
					int num11 = cachedVentDist[key];
					if (num11 < 48)
					{
						Point3 point4 = new Point3(key.X + 1, key.Y, key.Z);
						if (point4.X >= num2 && point4.X <= num5 && point4.Y >= num3 && point4.Y <= num6 && point4.Z >= num4 && point4.Z <= num7 && !cachedVentDist.ContainsKey(point4) && IsPassable(point4.X, point4.Y, point4.Z))
						{
							cachedVentDist[point4] = num11 + 1;
							list.Add(point4);
						}
						point4 = new Point3(key.X - 1, key.Y, key.Z);
						if (point4.X >= num2 && point4.X <= num5 && point4.Y >= num3 && point4.Y <= num6 && point4.Z >= num4 && point4.Z <= num7 && !cachedVentDist.ContainsKey(point4) && IsPassable(point4.X, point4.Y, point4.Z))
						{
							cachedVentDist[point4] = num11 + 1;
							list.Add(point4);
						}
						point4 = new Point3(key.X, key.Y, key.Z + 1);
						if (point4.X >= num2 && point4.X <= num5 && point4.Y >= num3 && point4.Y <= num6 && point4.Z >= num4 && point4.Z <= num7 && !cachedVentDist.ContainsKey(point4) && IsPassable(point4.X, point4.Y, point4.Z))
						{
							cachedVentDist[point4] = num11 + 1;
							list.Add(point4);
						}
						point4 = new Point3(key.X, key.Y, key.Z - 1);
						if (point4.X >= num2 && point4.X <= num5 && point4.Y >= num3 && point4.Y <= num6 && point4.Z >= num4 && point4.Z <= num7 && !cachedVentDist.ContainsKey(point4) && IsPassable(point4.X, point4.Y, point4.Z))
						{
							cachedVentDist[point4] = num11 + 1;
							list.Add(point4);
						}
						point4 = new Point3(key.X, key.Y + 1, key.Z);
						if (point4.X >= num2 && point4.X <= num5 && point4.Y >= num3 && point4.Y <= num6 && point4.Z >= num4 && point4.Z <= num7 && !cachedVentDist.ContainsKey(point4) && IsPassable(point4.X, point4.Y, point4.Z))
						{
							cachedVentDist[point4] = num11 + 1;
							list.Add(point4);
						}
						point4 = new Point3(key.X, key.Y - 1, key.Z);
						if (point4.X >= num2 && point4.X <= num5 && point4.Y >= num3 && point4.Y <= num6 && point4.Z >= num4 && point4.Z <= num7 && !cachedVentDist.ContainsKey(point4) && IsPassable(point4.X, point4.Y, point4.Z))
						{
							cachedVentDist[point4] = num11 + 1;
							list.Add(point4);
						}
					}
				}
				m_cachedVentHash = num8;
				m_cachedMin = new Point3(num2, num3, num4);
				m_cachedMax = new Point3(num5, num6, num7);
				m_nextVentRebuildTime = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0) + 0.5;
			}
		}
		else
		{
			cachedVentDist.Clear();
		}
		Dictionary<Point3, float> dictionary2 = new Dictionary<Point3, float>(m_smokeByCell.Count * 2 + m_activeCells.Count, new Point3EqualityComparer());
		for (int n = 0; n < s_tmpProcess.Count; n++)
		{
			Point3 key2 = s_tmpProcess[n];
			float value = 0f;
			m_smokeByCell.TryGetValue(key2, out value);
			float value2 = 0f;
			m_reports.TryGetValue(key2, out value2);
			if (value2 < 0f)
			{
				value2 = 0f;
			}
			value += m_kIn * value2 * dt;
			if (value <= 0f)
			{
				m_reports[key2] = 0f;
				continue;
			}
			int x2 = key2.X;
			int y = key2.Y;
			int z2 = key2.Z;
			bool flag5 = y < 255 && IsPassable(x2, y + 1, z2);
			bool flag6 = y > 0 && IsPassable(x2, y - 1, z2);
			bool flag7 = IsPassable(x2 + 1, y, z2);
			bool flag8 = IsPassable(x2 - 1, y, z2);
			bool flag9 = IsPassable(x2, y, z2 + 1);
			bool flag10 = IsPassable(x2, y, z2 - 1);
			float value3 = 0f;
			float value4 = 0f;
			float value5 = (flag7 ? 0f : 1f);
			float value6 = (flag8 ? 0f : 1f);
			float value7 = (flag9 ? 0f : 1f);
			float value8 = (flag10 ? 0f : 1f);
			if (flag5)
			{
				m_smokeByCell.TryGetValue(new Point3(x2, y + 1, z2), out value3);
			}
			if (flag6)
			{
				m_smokeByCell.TryGetValue(new Point3(x2, y - 1, z2), out value4);
			}
			if (flag7)
			{
				m_smokeByCell.TryGetValue(new Point3(x2 + 1, y, z2), out value5);
			}
			if (flag8)
			{
				m_smokeByCell.TryGetValue(new Point3(x2 - 1, y, z2), out value6);
			}
			if (flag9)
			{
				m_smokeByCell.TryGetValue(new Point3(x2, y, z2 + 1), out value7);
			}
			if (flag10)
			{
				m_smokeByCell.TryGetValue(new Point3(x2, y, z2 - 1), out value8);
			}
			float num12 = (flag7 ? MathUtils.Max(value - value5 - 0.05f, 0f) : 0f);
			float num13 = (flag8 ? MathUtils.Max(value - value6 - 0.05f, 0f) : 0f);
			float num14 = (flag9 ? MathUtils.Max(value - value7 - 0.05f, 0f) : 0f);
			float num15 = (flag10 ? MathUtils.Max(value - value8 - 0.05f, 0f) : 0f);
			if (!flag3 || !cachedVentDist.ContainsKey(key2))
			{
				float num16 = num12 + num13 + num14 + num15;
				if (num16 > 0f && value > 0f && num > 0f)
				{
					float num17 = MathUtils.Min(1f, MathUtils.Min(num, value) / num16);
					if (num12 > 0f)
					{
						AddTo(dictionary2, new Point3(x2 + 1, y, z2), num12 * num17);
					}
					if (num13 > 0f)
					{
						AddTo(dictionary2, new Point3(x2 - 1, y, z2), num13 * num17);
					}
					if (num14 > 0f)
					{
						AddTo(dictionary2, new Point3(x2, y, z2 + 1), num14 * num17);
					}
					if (num15 > 0f)
					{
						AddTo(dictionary2, new Point3(x2, y, z2 - 1), num15 * num17);
					}
					value -= num16 * num17;
				}
			}
			else
			{
				int num18 = cachedVentDist[key2];
				float num19 = 0f;
				float num20 = 0f;
				float num21 = 0f;
				float num22 = 0f;
				float num23 = 0f;
				if (flag7 && num12 > 0f && cachedVentDist.TryGetValue(new Point3(x2 + 1, y, z2), out var value9) && value9 < num18)
				{
					num19 = num12 * 3f;
					num23 += num19;
				}
				if (flag8 && num13 > 0f && cachedVentDist.TryGetValue(new Point3(x2 - 1, y, z2), out value9) && value9 < num18)
				{
					num20 = num13 * 3f;
					num23 += num20;
				}
				if (flag9 && num14 > 0f && cachedVentDist.TryGetValue(new Point3(x2, y, z2 + 1), out value9) && value9 < num18)
				{
					num21 = num14 * 3f;
					num23 += num21;
				}
				if (flag10 && num15 > 0f && cachedVentDist.TryGetValue(new Point3(x2, y, z2 - 1), out value9) && value9 < num18)
				{
					num22 = num15 * 3f;
					num23 += num22;
				}
				if (num23 > 0f && value > 0f)
				{
					float num24 = MathUtils.Min(value, num * 2f);
					float num25 = MathUtils.Min(1f, num24 / num23);
					if (num19 > 0f)
					{
						AddTo(dictionary2, new Point3(x2 + 1, y, z2), num19 * num25);
					}
					if (num20 > 0f)
					{
						AddTo(dictionary2, new Point3(x2 - 1, y, z2), num20 * num25);
					}
					if (num21 > 0f)
					{
						AddTo(dictionary2, new Point3(x2, y, z2 + 1), num21 * num25);
					}
					if (num22 > 0f)
					{
						AddTo(dictionary2, new Point3(x2, y, z2 - 1), num22 * num25);
					}
					value -= (num19 + num20 + num21 + num22) * num25;
				}
			}
			if (flag5)
			{
				float num26 = 1f - value3;
				if (num26 > 0.05f && value > value3 + 0.05f)
				{
					float num27 = 1f;
					if (flag3 && cachedVentDist.ContainsKey(key2))
					{
						int num28 = cachedVentDist[key2];
						if (cachedVentDist.TryGetValue(new Point3(x2, y + 1, z2), out var value10) && value10 < num28)
						{
							num27 = 1.7f;
						}
					}
					float num29 = MathUtils.Min(MathUtils.Min(m_kUp * dt * num27, num26), value - value3);
					if (num29 > 0f)
					{
						AddTo(dictionary2, new Point3(x2, y + 1, z2), num29);
						value -= num29;
					}
				}
			}
			if (flag6 && value > 0f)
			{
				bool flag11 = !flag5;
				float num30 = MathUtils.Max(value - value4 - 0.05f, 0f);
				if (num30 > 0f)
				{
					float x3 = (flag11 ? (m_kUp * 0.6f) : (m_kSide * 0.9f)) * dt;
					float num31 = MathUtils.Min(MathUtils.Min(num30, x3), value);
					if (num31 > 0f)
					{
						AddTo(dictionary2, new Point3(x2, y - 1, z2), num31);
						value -= num31;
					}
				}
				if (value > 1f)
				{
					float amount = value - 1f;
					AddTo(dictionary2, new Point3(x2, y - 1, z2), amount);
					value = 1f;
				}
			}
			if (value > 0f)
			{
				AddTo(dictionary2, key2, value);
			}
			m_reports[key2] = 0f;
		}
		m_smokeByCell.Clear();
		foreach (KeyValuePair<Point3, float> item3 in dictionary2)
		{
			Point3 key3 = item3.Key;
			if (!IsPassable(key3.X, key3.Y, key3.Z) || (flag3 && dictionary.ContainsKey(key3)))
			{
				continue;
			}
			bool flag12 = IsInGrace(key3);
			bool flag13 = false;
			if (terrain != null)
			{
				TerrainChunk chunkAtCell2 = terrain.GetChunkAtCell(key3.X, key3.Z);
				if (chunkAtCell2 != null)
				{
					int x4 = key3.X & 0xF;
					int z3 = key3.Z & 0xF;
					int topHeightFast2 = chunkAtCell2.GetTopHeightFast(x4, z3);
					flag13 = key3.Y >= topHeightFast2;
				}
				else
				{
					flag13 = false;
				}
			}
			if (!flag12 && !flag13 && m_subsystemWeather != null && m_subsystemWeather.CurrentWindStrength > 6f)
			{
				Vector3 position2 = new Vector3((float)key3.X + 0.5f, (float)key3.Y + 0.5f, (float)key3.Z + 0.5f);
				if (m_subsystemWeather.GetWindExposureFactor(position2) > 0f)
				{
					flag13 = true;
				}
			}
			float num32;
			if (flag12)
			{
				num32 = 0f;
			}
			else
			{
				num32 = (flag13 ? (m_kDecay * dt) : 0f);
				if (flag3)
				{
					num32 += 0.005f * dt;
				}
			}
			float num33 = MathUtils.Saturate(item3.Value - num32);
			if (num33 > 0.0005f)
			{
				m_smokeByCell[key3] = num33;
			}
		}
		m_activeCells.Clear();
		foreach (KeyValuePair<Point3, float> item4 in m_smokeByCell)
		{
			m_activeCells.Add(item4.Key);
		}
		if (m_subsystemTime == null || m_subsystemTime.GameTime >= m_visualNextTick)
		{
			SpawnAndTrackHeavySmoke();
			m_visualNextTick = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0) + 0.25;
		}
	}

	private void DistributeToNeighbor(int x, int y, int z, float amount)
	{
		if (y < 0 || y >= 256)
		{
			return;
		}
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
		if (cellContents != 0)
		{
			Block block = BlocksManager.Blocks[cellContents];
			if (block.IsCollidable || block is FluidBlock)
			{
				return;
			}
		}
		Point3 key = new Point3(x, y, z);
		m_smokeByCell.TryGetValue(key, out var value);
		m_smokeByCell[key] = MathUtils.Saturate(value + amount);
	}

	public SubsystemSmokeAccumulation()
	{
		m_kUp = 2.5f;
		m_kSide = 0.8f;
		m_kCeilFill = 0.6f;
		m_kDecay = 0.02f;
		m_kIn = 0.35f;
		m_kBaseDecay = 0.02f;
		m_kVentDecay = 0.25f;
		m_kWindDecay = 0.25f;
		m_kDiffusion = 0.15f;
		m_smokeByCell = new Dictionary<Point3, float>(new Point3EqualityComparer());
		m_reports = new Dictionary<Point3, float>(new Point3EqualityComparer());
		m_activeCells = new DynamicArray<Point3>();
		m_smokeDecayCursor = new DynamicArray<Point3>();
		m_ventCache = new Dictionary<int, CachedVent>();
		m_cellToParticle = new Dictionary<Point3, HeavyRoomSmokeParticleSystem>(new Point3EqualityComparer());
		m_particleToCell = new Dictionary<HeavyRoomSmokeParticleSystem, Point3>();
		m_cachedVentDist = new Dictionary<Point3, int>(new Point3EqualityComparer());
		m_cachedVentHash = 0;
		m_cachedMin = new Point3(0, 0, 0);
		m_cachedMax = new Point3(0, 0, 0);
		m_nextVentRebuildTime = 0.0;
		m_particlesSteerNext = 0.0;
		m_chunkGraceUntil = new Dictionary<Point2, double>();
		m_loadGraceEndTime = 0.0;
		SmokeGraceSeconds = 5.0;
	}

	private void SpawnAndTrackHeavySmoke()
	{
		bool flag = false;
		Terrain terrain = ((m_subsystemTerrain != null) ? m_subsystemTerrain.Terrain : null);
		if (terrain != null)
		{
			foreach (KeyValuePair<Point3, float> item in m_smokeByCell)
			{
				Point3 key = item.Key;
				if (!(item.Value >= 0.05f))
				{
					continue;
				}
				TerrainChunk chunkAtCell = terrain.GetChunkAtCell(key.X, key.Z);
				if (chunkAtCell != null)
				{
					int x = key.X & 0xF;
					int z = key.Z & 0xF;
					int topHeightFast = chunkAtCell.GetTopHeightFast(x, z);
					if (key.Y >= topHeightFast)
					{
						flag = true;
						break;
					}
				}
			}
		}
		if (m_particleToCell.Count > 0)
		{
			s_tmpParticles.Clear();
			s_tmpParticles.AddRange(m_particleToCell.Keys);
			for (int i = 0; i < s_tmpParticles.Count; i++)
			{
				HeavyRoomSmokeParticleSystem heavyRoomSmokeParticleSystem = s_tmpParticles[i];
				if (!heavyRoomSmokeParticleSystem.IsAlive)
				{
					if (m_particleToCell.TryGetValue(heavyRoomSmokeParticleSystem, out var value))
					{
						if (m_cellToParticle.TryGetValue(value, out var value2) && value2 == heavyRoomSmokeParticleSystem)
						{
							m_cellToParticle.Remove(value);
						}
						m_particleToCell.Remove(heavyRoomSmokeParticleSystem);
					}
					continue;
				}
				Vector3 position = heavyRoomSmokeParticleSystem.Position;
				Point3 point = new Point3(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
				Point3 point2 = m_particleToCell[heavyRoomSmokeParticleSystem];
				if (!point.Equals(point2))
				{
					if (m_cellToParticle.TryGetValue(point2, out var value3) && value3 == heavyRoomSmokeParticleSystem)
					{
						m_cellToParticle.Remove(point2);
					}
					if (!m_cellToParticle.ContainsKey(point))
					{
						m_cellToParticle[point] = heavyRoomSmokeParticleSystem;
						m_particleToCell[heavyRoomSmokeParticleSystem] = point;
					}
					else
					{
						m_particleToCell[heavyRoomSmokeParticleSystem] = point;
					}
				}
				if (!m_particleToCell.TryGetValue(heavyRoomSmokeParticleSystem, out var value4))
				{
					continue;
				}
				int num = FindTopOfSmokeColumn(value4);
				if (num <= value4.Y)
				{
					num = value4.Y + 1;
				}
				float num2 = num - value4.Y;
				heavyRoomSmokeParticleSystem.TargetTopY = (float)num + 0.5f;
				heavyRoomSmokeParticleSystem.DriftSpeed = MathUtils.Lerp(0.02f, 0.08f, MathUtils.Saturate(num2 / 12f));
				double num3 = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0);
				if (!flag || terrain == null || !(num3 >= m_particlesSteerNext))
				{
					continue;
				}
				m_particlesSteerNext = num3 + 0.5;
				int x2 = value4.X;
				int y = value4.Y;
				int z2 = value4.Z;
				bool flag2 = false;
				int num4 = x2;
				int num5 = z2;
				for (int j = 1; j <= 32; j++)
				{
					if (flag2)
					{
						break;
					}
					Point3[] array = new Point3[4]
					{
						new Point3(x2 + j, y, z2),
						new Point3(x2 - j, y, z2),
						new Point3(x2, y, z2 + j),
						new Point3(x2, y, z2 - j)
					};
					for (int k = 0; k < array.Length; k++)
					{
						if (flag2)
						{
							break;
						}
						Point3 key2 = array[k];
						if (!m_smokeByCell.TryGetValue(key2, out var value5) || !(value5 >= 0.05f))
						{
							continue;
						}
						TerrainChunk chunkAtCell2 = terrain.GetChunkAtCell(key2.X, key2.Z);
						if (chunkAtCell2 != null)
						{
							int x3 = key2.X & 0xF;
							int z3 = key2.Z & 0xF;
							int topHeightFast2 = chunkAtCell2.GetTopHeightFast(x3, z3);
							if (key2.Y >= topHeightFast2)
							{
								flag2 = true;
								num4 = key2.X;
								num5 = key2.Z;
							}
						}
					}
				}
				if (flag2)
				{
					Vector2 v = new Vector2(num4 - x2, num5 - z2);
					if (v.Length() > 0.0001f)
					{
						heavyRoomSmokeParticleSystem.DriftDir = Vector2.Normalize(v);
					}
				}
			}
		}
		s_tmpBottomCells.Clear();
		foreach (KeyValuePair<Point3, float> item2 in m_smokeByCell)
		{
			Point3 key3 = item2.Key;
			if (item2.Value >= 0.05f)
			{
				m_smokeByCell.TryGetValue(new Point3(key3.X, key3.Y - 1, key3.Z), out var value6);
				if (key3.Y == 0 || value6 < 0.05f)
				{
					s_tmpBottomCells.Add(key3);
				}
			}
		}
		for (int l = 0; l < s_tmpBottomCells.Count; l++)
		{
			Point3 point3 = s_tmpBottomCells[l];
			if (!m_cellToParticle.ContainsKey(point3))
			{
				int num6 = FindTopOfSmokeColumn(point3);
				if (num6 <= point3.Y)
				{
					num6 = point3.Y + 1;
				}
				float num7 = num6 - point3.Y;
				Vector3 startWorldPos = new Vector3((float)point3.X + 0.5f, (float)point3.Y + 0.05f, (float)point3.Z + 0.5f);
				float targetTopY = (float)num6 + 0.5f;
				bool flag3 = IsInGrace(point3);
				float initialAge = 0f;
				if (flag3)
				{
					float value7 = 0f;
					m_smokeByCell.TryGetValue(point3, out value7);
					float num8 = MathUtils.Saturate(num7 / 12f);
					initialAge = MathUtils.Saturate(0.85f * num8 + 0.15f * value7);
					initialAge = MathUtils.Min(initialAge, 0.9f);
				}
				HeavyRoomSmokeParticleSystem heavyRoomSmokeParticleSystem2 = new HeavyRoomSmokeParticleSystem(m_subsystemTerrain, m_weather, startWorldPos, targetTopY, num7, this, initialAge);
				m_subsystemParticles.AddParticleSystem(heavyRoomSmokeParticleSystem2);
				m_cellToParticle[point3] = heavyRoomSmokeParticleSystem2;
				m_particleToCell[heavyRoomSmokeParticleSystem2] = point3;
			}
		}
	}

	private int FindTopOfSmokeColumn(Point3 bottom)
	{
		int num;
		float value;
		for (int i = (num = bottom.Y); i < 256 && m_smokeByCell.TryGetValue(new Point3(bottom.X, i, bottom.Z), out value); i++)
		{
			if (!(value >= 0.05f))
			{
				break;
			}
			num = i;
		}
		return num + 1;
	}

	static SubsystemSmokeAccumulation()
	{
		s_tmpProcess = new List<Point3>(4096);
		s_tmpParticles = new List<HeavyRoomSmokeParticleSystem>(128);
		s_tmpBottomCells = new List<Point3>(512);
	}

	private bool IsPassable(int x, int y, int z)
	{
		if (y < 0 || y > 255)
		{
			return false;
		}
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
		if (cellContents == 0)
		{
			return true;
		}
		Block block = BlocksManager.Blocks[cellContents];
		if (!(block is FluidBlock) && !(block is FocalorsFluidBlock))
		{
			return !block.IsCollidable;
		}
		return false;
	}

	private void AddTo(Dictionary<Point3, float> map, Point3 key, float amount)
	{
		if (!(amount <= 0f))
		{
			if (!map.TryGetValue(key, out var value))
			{
				value = 0f;
			}
			map[key] = value + amount;
		}
	}

	public void ReportSmokeAtScaled(Vector3 position, float intensity, float strength01)
	{
		Point3 point = new Point3(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		m_reports.TryGetValue(point, out var value);
		float f = MathUtils.Saturate(strength01);
		float num = MathUtils.Lerp(0f, 2f, f);
		m_reports[point] = value + MathUtils.Max(intensity * num, 0f);
		m_activeCells.Add(point);
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("SmokeGraceSeconds", SmokeGraceSeconds);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		int num = 0;
		foreach (KeyValuePair<Point3, float> item in m_smokeByCell)
		{
			if (item.Value > 0.0005f)
			{
				Point3 key = item.Key;
				float num2 = (float)Math.Round(item.Value, 3);
				valuesDictionary2.SetValue(num.ToString(), key.X + "," + key.Y + "," + key.Z + "," + num2);
				num++;
			}
		}
		valuesDictionary.SetValue("SmokeCells", valuesDictionary2);
	}

	private bool IsInGrace(Point3 c)
	{
		double num = ((m_subsystemTime != null) ? m_subsystemTime.GameTime : 0.0);
		if (num < m_loadGraceEndTime)
		{
			return true;
		}
		if (m_subsystemTerrain != null)
		{
			Terrain terrain = m_subsystemTerrain.Terrain;
			if (terrain != null)
			{
				TerrainChunk chunkAtCell = terrain.GetChunkAtCell(c.X, c.Z);
				if (chunkAtCell != null && m_chunkGraceUntil != null && m_chunkGraceUntil.TryGetValue(chunkAtCell.Coords, out var value) && num < value)
				{
					return true;
				}
			}
		}
		return false;
	}

	private float GetVentilationFactor(Point3 c)
	{
		int key = (c.X * 73856093) ^ (c.Y * 19349663) ^ (c.Z * 83492791);
		if (m_ventCache.TryGetValue(key, out var value) && m_ventCacheStamp - value.Stamp <= 60)
		{
			return value.Value;
		}
		int num = 0;
		int num2 = 0;
		Terrain terrain = m_subsystemTerrain.Terrain;
		for (int i = -6; i <= 6; i += 6)
		{
			for (int j = -6; j <= 6; j += 6)
			{
				for (int k = -6; k <= 6; k += 6)
				{
					if (MathUtils.Abs(i) + MathUtils.Abs(j) + MathUtils.Abs(k) == 6)
					{
						int num3 = c.X + i;
						int num4 = MathUtils.Clamp(c.Y + j, 0, 255);
						int num5 = c.Z + k;
						bool flag = terrain.GetCellContents(num3, num4, num5) == 0;
						bool flag2 = true;
						TerrainChunk chunkAtCell = terrain.GetChunkAtCell(num3, num5);
						if (chunkAtCell != null)
						{
							int x = num3 & 0xF;
							int z = num5 & 0xF;
							int topHeightFast = chunkAtCell.GetTopHeightFast(x, z);
							flag2 = num4 >= topHeightFast;
						}
						num++;
						if (flag | flag2)
						{
							num2++;
						}
					}
				}
			}
		}
		float num6 = ((num > 0) ? ((float)num2 / (float)num) : 0f);
		m_ventCache[key] = new CachedVent
		{
			Value = num6,
			Stamp = m_ventCacheStamp
		};
		m_ventCacheStamp++;
		return num6;
	}
}
