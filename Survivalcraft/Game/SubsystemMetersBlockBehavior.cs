using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemMetersBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemSky m_subsystemSky;

	public Dictionary<Point3, int> m_thermometersByPoint = new Dictionary<Point3, int>();

	public DynamicArray<Point3> m_thermometersToSimulate = new DynamicArray<Point3>();

	public int m_thermometersToSimulateIndex;

	public const int m_diameterBits = 6;

	public const int m_diameter = 64;

	public const int m_diameterMask = 63;

	public const int m_radius = 32;

	public DynamicArray<int> m_toVisit = new DynamicArray<int>();

	public int[] m_visited = new int[8192];

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemFocalorsWaterBlockBehavior m_subsystemFocalorsWater;

	public override int[] HandledBlocks => new int[2] { 120, 121 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		Point3 point = CellFace.FaceToPoint3(Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(x - point.X, y - point.Y, z - point.Z);
		if (BlocksManager.Blocks[cellContents].IsTransparent)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		AddMeter(value, x, y, z);
	}

	public override void OnBlockRemoved(int value, int oldValue, int x, int y, int z)
	{
		RemoveMeter(oldValue, x, y, z);
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		RemoveMeter(oldValue, x, y, z);
		AddMeter(value, x, y, z);
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		AddMeter(value, x, y, z);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> result = new List<Point3>();
		foreach (Point3 key in m_thermometersByPoint.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				result.Add(key);
			}
		}
		foreach (Point3 item in result)
		{
			m_thermometersByPoint.Remove(item);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemFocalorsWater = Project.FindSubsystem<SubsystemFocalorsWaterBlockBehavior>(throwOnError: true);
	}

	public void Update(float dt)
	{
		if (m_thermometersToSimulateIndex < m_thermometersToSimulate.Count)
		{
			double period = MathUtils.Max(5.0 / (double)m_thermometersToSimulate.Count, 1.0);
			if (m_subsystemTime.PeriodicGameTimeEvent(period, 0.0))
			{
				Point3 point = m_thermometersToSimulate.Array[m_thermometersToSimulateIndex];
				SimulateThermometer(point.X, point.Y, point.Z, invalidateTerrainOnChange: true);
				m_thermometersToSimulateIndex++;
			}
		}
		else if (m_thermometersByPoint.Count > 0)
		{
			m_thermometersToSimulateIndex = 0;
			m_thermometersToSimulate.Clear();
			m_thermometersToSimulate.AddRange(m_thermometersByPoint.Keys);
		}
	}

	public int GetThermometerReading(int x, int y, int z)
	{
		m_thermometersByPoint.TryGetValue(new Point3(x, y, z), out var value);
		return value;
	}

	public void CalculateTemperature(int x, int y, int z, float meterTemperature, float meterInsulation, out float temperature, out float temperatureFlux)
	{
		m_toVisit.Clear();
		for (int i = 0; i < m_visited.Length; i++)
		{
			m_visited[i] = 0;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(new Vector3(x, y, z));
		float num8 = 0f;
		switch (m_subsystemWeather.CurrentSeason)
		{
		case SubsystemWeather.Season.LateAutumn:
			num8 = -5f;
			break;
		case SubsystemWeather.Season.Anabiosis:
			num8 = -15f;
			break;
		case SubsystemWeather.Season.Winter:
			num8 = -20f;
			break;
		case SubsystemWeather.Season.LateWinter:
			num8 = -15f;
			break;
		case SubsystemWeather.Season.EarlySpring:
			num8 = -5f;
			break;
		case SubsystemWeather.Season.Summer:
			num8 = 5f;
			break;
		}
		float temperatureOffset = m_subsystemGameInfo.WorldSettings.TemperatureOffset;
		float humidityOffset = m_subsystemGameInfo.WorldSettings.HumidityOffset;
		bool flag = MathUtils.Abs(temperatureOffset) > 16f;
		bool flag2 = MathUtils.Abs(humidityOffset) > 16f;
		m_toVisit.Add(133152);
		for (int j = 0; j < m_toVisit.Count; j++)
		{
			int num9 = m_toVisit.Array[j];
			if ((m_visited[num9 / 32] & (1 << num9)) != 0)
			{
				continue;
			}
			m_visited[num9 / 32] |= 1 << num9;
			int num10 = (num9 & 0x3F) - 32;
			int num11 = ((num9 >> 6) & 0x3F) - 32;
			int num12 = ((num9 >> 12) & 0x3F) - 32;
			int num13 = num10 + x;
			int num14 = num11 + y;
			int num15 = num12 + z;
			Terrain terrain = SubsystemTerrain.Terrain;
			TerrainChunk chunkAtCell = terrain.GetChunkAtCell(num13, num15);
			if (chunkAtCell == null || num14 < 0 || num14 >= 256)
			{
				continue;
			}
			int x2 = num13 & 0xF;
			int y2 = num14;
			int z2 = num15 & 0xF;
			int cellValueFast = chunkAtCell.GetCellValueFast(x2, y2, z2);
			int num16 = Terrain.ExtractContents(cellValueFast);
			Block block = BlocksManager.Blocks[num16];
			float num17 = GetHeat(cellValueFast);
			if (block is FocalorsWaterBlock && m_subsystemFocalorsWater.IsWaterHot(num13, num14, num15))
			{
				num17 = 1f;
			}
			if (num17 != 0f)
			{
				int num18 = MathUtils.Abs(num10) + MathUtils.Abs(num11) + MathUtils.Abs(num12);
				int num19 = ((num18 <= 0) ? 1 : (4 * num18 * num18 + 2));
				float num20 = 1f / (float)num19;
				if (num17 > 0f)
				{
					num += num20 * 36f * num17;
					num2 += num20;
				}
				else
				{
					num3 += num20 * 36f * num17;
					num4 += num20;
				}
			}
			else if (block.IsHeatBlocker(cellValueFast))
			{
				int num21 = MathUtils.Abs(num10) + MathUtils.Abs(num11) + MathUtils.Abs(num12);
				int num22 = ((num21 <= 0) ? 1 : (4 * num21 * num21 + 2));
				float num23 = 1f / (float)num22;
				float num24 = (float)terrain.SeasonTemperature + num8;
				if (flag)
				{
					num24 += temperatureOffset;
				}
				float num25 = SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
				float num26;
				if (!(block is FocalorsWaterBlock))
				{
					num26 = ((!(block is IceBlock)) ? ((float)chunkAtCell.GetTemperatureFast(x2, z2) + num24 + num25) : (0f + num24 + num25));
				}
				else if (m_subsystemFocalorsWater.IsWaterHot(num13, num14, num15))
				{
					num26 = 5f + num25;
				}
				else
				{
					float num27 = (float)chunkAtCell.GetTemperatureFast(x2, z2) + num24;
					float num28 = MathUtils.Lerp(12f, 2f, MathUtils.Saturate((num27 - 0f) / 10f));
					num26 = MathUtils.Max(num27 - num28, 0f) + num25;
				}
				int shaftValue = terrain.GetShaftValue(num13, num15);
				int seasonalTemperature = terrain.GetSeasonalTemperature(shaftValue);
				int num29 = terrain.GetSeasonalHumidity(shaftValue);
				if (flag2)
				{
					num29 += (int)humidityOffset;
				}
				float dayBoost = CalcDesertDayBoost(seasonalTemperature, num29, 2f / 3f);
				num26 += ApplyDiurnal(dayBoost);
				num5 += num23 * num26;
				num6 += num23;
			}
			else if (y >= chunkAtCell.GetTopHeightFast(x2, z2))
			{
				int num30 = MathUtils.Abs(num10) + MathUtils.Abs(num11) + MathUtils.Abs(num12);
				int num31 = ((num30 <= 0) ? 1 : (4 * num30 * num30 + 2));
				float num32 = 1f / (float)num31;
				PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
				float num33 = (float)terrain.SeasonTemperature + num8;
				if (flag)
				{
					num33 += temperatureOffset;
				}
				float num34 = ((y >= precipitationShaftInfo.YLimit) ? MathUtils.Lerp(0f, -2f, precipitationShaftInfo.Intensity) : 0f);
				float num35 = MathUtils.Lerp(-6f, 0f, m_subsystemSky.SkyLightIntensity);
				float num36 = SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
				float num37 = (float)chunkAtCell.GetTemperatureFast(x2, z2) + num33 + num34 + num35 + num36;
				int shaftValue2 = terrain.GetShaftValue(num13, num15);
				int seasonalTemperature2 = terrain.GetSeasonalTemperature(shaftValue2);
				int num38 = terrain.GetSeasonalHumidity(shaftValue2);
				if (flag2)
				{
					num38 += (int)humidityOffset;
				}
				float dayBoost2 = CalcDesertDayBoost(seasonalTemperature2, num38);
				num37 += ApplyDiurnal(dayBoost2);
				num5 += num32 * num37;
				num6 += num32;
			}
			else if (m_toVisit.Count < 4090)
			{
				if (num10 > -30)
				{
					m_toVisit.Add(num9 - 1);
				}
				if (num10 < 30)
				{
					m_toVisit.Add(num9 + 1);
				}
				if (num11 > -30)
				{
					m_toVisit.Add(num9 - 64);
				}
				if (num11 < 30)
				{
					m_toVisit.Add(num9 + 64);
				}
				if (num12 > -30)
				{
					m_toVisit.Add(num9 - 4096);
				}
				if (num12 < 30)
				{
					m_toVisit.Add(num9 + 4096);
				}
			}
		}
		if (windExposureFactor > 0f)
		{
			float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
			string windDirection = m_subsystemWeather.WindDirection;
			if (currentWindStrength > 0f && currentWindStrength <= 5f)
			{
				switch (windDirection)
				{
				case "S":
				case "E":
				case "SE":
					num7 = MathUtils.Lerp(1f, 3f, currentWindStrength / 5f);
					break;
				case "N":
				case "W":
				case "NW":
					num7 = 0f - MathUtils.Lerp(1f, 3f, currentWindStrength / 5f);
					break;
				}
			}
			else if (currentWindStrength >= 10f && currentWindStrength <= 16f)
			{
				switch (windDirection)
				{
				case "S":
				case "E":
				case "SE":
					num7 = 0f - MathUtils.Lerp(1f, 6f, (currentWindStrength - 10f) / 6f);
					break;
				case "N":
				case "W":
				case "NW":
					num7 = 0f - MathUtils.Lerp(7f, 11f, (currentWindStrength - 10f) / 6f);
					break;
				}
			}
		}
		float num39 = 0f;
		for (int k = -7; k <= 7; k++)
		{
			for (int l = -7; l <= 7; l++)
			{
				TerrainChunk chunkAtCell2 = SubsystemTerrain.Terrain.GetChunkAtCell(x + k, z + l);
				if (chunkAtCell2 == null || chunkAtCell2.State < TerrainChunkState.InvalidVertices1)
				{
					continue;
				}
				for (int m = -7; m <= 7; m++)
				{
					int num40 = k * k + m * m + l * l;
					if (num40 > 49 || num40 <= 0)
					{
						continue;
					}
					int x3 = (x + k) & 0xF;
					int num41 = y + m;
					int z3 = (z + l) & 0xF;
					if (num41 >= 0 && num41 < 256)
					{
						float num42 = GetHeat(chunkAtCell2.GetCellValueFast(x3, num41, z3));
						if (BlocksManager.Blocks[Terrain.ExtractContents(chunkAtCell2.GetCellValueFast(x3, num41, z3))] is FocalorsWaterBlock && m_subsystemFocalorsWater.IsWaterHot(x + k, num41, z + l))
						{
							num42 = 0.1f;
						}
						if (num42 > 0f && !SubsystemTerrain.Raycast(new Vector3(x, y, z) + new Vector3(0.5f, 0.75f, 0.5f), new Vector3(x + k, y + m, z + l) + new Vector3(0.5f, 0.75f, 0.5f), useInteractionBoxes: false, skipAirBlocks: true, (int raycastValue, float d) =>
						{
							Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(raycastValue)];
							return block2.IsCollidable && !block2.IsTransparent;
						}).HasValue)
						{
							num39 += num42 * 3f / (float)(num40 + 2);
						}
					}
				}
			}
		}
		float num43 = 0f;
		float num44 = 0f;
		if (num39 > 0f)
		{
			float num45 = 3f * num39;
			num43 += 35f * num45;
			num44 += num45;
		}
		if (num6 > 0f)
		{
			num43 += num5 / num6;
			num44++;
		}
		if (num2 > 0f)
		{
			num43 += num / num2 * 4f;
			num44 += 4f;
		}
		if (num4 > 0f)
		{
			num43 += num3 / num4 * 2f;
			num44 += 2f;
		}
		if (meterInsulation > 0f)
		{
			num43 += meterTemperature * meterInsulation;
			num44 += meterInsulation;
		}
		num43 += num7 * windExposureFactor;
		temperature = ((num44 > 0f) ? (num43 / num44) : meterTemperature);
		temperatureFlux = num44 - meterInsulation;
	}

	public static float GetHeat(int value)
	{
		int num = Terrain.ExtractContents(value);
		return BlocksManager.Blocks[num].GetHeat(value);
	}

	public void SimulateThermometer(int x, int y, int z, bool invalidateTerrainOnChange)
	{
		Point3 key = new Point3(x, y, z);
		if (!m_thermometersByPoint.ContainsKey(key))
		{
			return;
		}
		int num = m_thermometersByPoint[key];
		CalculateTemperature(x, y, z, 0f, 0f, out var temperature, out var _);
		int num2 = MathUtils.Clamp((int)MathUtils.Round(temperature), 0, 15);
		if (num2 == num)
		{
			return;
		}
		m_thermometersByPoint[new Point3(x, y, z)] = num2;
		if (invalidateTerrainOnChange)
		{
			TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null)
			{
				SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 0, TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: true);
			}
		}
	}

	public void AddMeter(int value, int x, int y, int z)
	{
		if (Terrain.ExtractContents(value) == 120)
		{
			m_thermometersByPoint.Add(new Point3(x, y, z), 0);
			SimulateThermometer(x, y, z, invalidateTerrainOnChange: false);
		}
	}

	public void RemoveMeter(int value, int x, int y, int z)
	{
		m_thermometersByPoint.Remove(new Point3(x, y, z));
	}

	public bool IsRoomClosed(int x, int y, int z)
	{
		Queue<Point3> queue = new Queue<Point3>();
		HashSet<Point3> hashSet = new HashSet<Point3>();
		queue.Enqueue(new Point3(x, y, z));
		hashSet.Add(new Point3(x, y, z));
		while (queue.Count > 0)
		{
			Point3 point = queue.Dequeue();
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						if (i == 0 && j == 0 && k == 0)
						{
							continue;
						}
						Point3 item = new Point3(point.X + i, point.Y + j, point.Z + k);
						if (!hashSet.Contains(item))
						{
							if (SubsystemTerrain.Terrain.GetCellContents(item.X, item.Y, item.Z) == 0)
							{
								return false;
							}
							queue.Enqueue(item);
							hashSet.Add(item);
						}
					}
				}
			}
		}
		return true;
	}

	private float CalcDesertDayBoost(int seasonalTemperature, int seasonalHumidity, float scale = 1f)
	{
		float num = 1f - MathUtils.Saturate((float)seasonalHumidity / 8f);
		float num2 = MathUtils.Max(0f, (float)seasonalTemperature - 6f);
		return 5.5f * num2 * num * scale;
	}

	private float ApplyDiurnal(float dayBoost)
	{
		float text = MathUtils.Saturate(m_subsystemSky.SkyLightIntensity);
		return MathUtils.Lerp(-0.5f * dayBoost, dayBoost, text);
	}

	public void CalculateTemperatureForEntity(Vector3 position, out float temperature, out float temperatureFlux)
	{
		int x = Terrain.ToCell(position.X);
		int num = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		int num2 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, num, z));
		Block block = BlocksManager.Blocks[num2];
		if (num2 != 0 && !block.IsCollidable)
		{
			int num3 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, num + 1, z));
			if (num3 == 0 || !BlocksManager.Blocks[num3].IsCollidable)
			{
				num++;
			}
		}
		CalculateTemperature(x, num, z, 0f, 0f, out temperature, out temperatureFlux);
	}
}
