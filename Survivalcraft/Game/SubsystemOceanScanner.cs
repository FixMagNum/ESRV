using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemOceanScanner : Subsystem, IUpdateable
{
	private SubsystemTerrain m_terrain;

	private SubsystemAudio m_audio;

	private Point2 m_lastChunk = new Point2(int.MinValue, int.MinValue);

	private bool m_first = true;

	private TerrainContentsGenerator23 m_generator;

	private SubsystemPlayers m_players;

	private SubsystemSky m_sky;

	private SubsystemTime m_subsystemTime;

	private SubsystemWeather m_subsystemWeather;

	private SubsystemTimeOfDay m_timeOfDay;

	private SubsystemGameInfo m_subsystemGameInfo;

	private TerrainContentsGeneratorFlat m_generatorFlat;

	public bool IsOcean { get; private set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public float OceanWaveStrength { get; private set; }

	public int OceanLevel
	{
		get
		{
			if (m_generator != null)
			{
				return m_generator.OceanLevel;
			}
			if (m_generatorFlat != null)
			{
				return m_generatorFlat.OceanLevel;
			}
			return 64;
		}
	}

	public override void Load(ValuesDictionary dict)
	{
		base.Load(dict);
		m_terrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_audio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_players = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		SubsystemTerrain terrain = m_terrain;
		m_generator = terrain?.TerrainContentsGenerator as TerrainContentsGenerator23;
		if (m_generator == null)
		{
			m_generatorFlat = terrain?.TerrainContentsGenerator as TerrainContentsGeneratorFlat;
		}
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_timeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	public void Update(float dt)
	{
		Vector3 vector = Vector3.Zero;
		if (m_players != null && m_players.ComponentPlayers.Count > 0)
		{
			ComponentMiner componentMiner = m_players.ComponentPlayers[0].Entity.FindComponent<ComponentMiner>();
			if (componentMiner != null)
			{
				vector = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition;
			}
		}
		if (vector == Vector3.Zero && m_audio != null && m_audio.ListenerPositions.Count > 0)
		{
			vector = m_audio.ListenerPositions[0];
		}
		float num = 0f;
		if (m_generator != null)
		{
			float num2 = m_generator.CalculateOceanShoreDistance(vector.X, vector.Z);
			if (num2 < 0f)
			{
				num = 1f;
			}
			else if (num2 < 100f)
			{
				num = 1f - num2 / 100f;
			}
		}
		else if (m_generatorFlat != null)
		{
			float num3 = m_generatorFlat.CalculateOceanShoreDistance(vector.X, vector.Z);
			if (num3 < 0f)
			{
				num = 1f;
			}
			else if (num3 < 100f)
			{
				num = 1f - num3 / 100f;
			}
		}
		else
		{
			bool flag = true;
			int num4 = Terrain.ToCell(vector.X);
			int num5 = Terrain.ToCell(vector.Z);
			for (int i = -1; (i <= 1) & flag; i++)
			{
				for (int j = -1; (j <= 1) & flag; j++)
				{
					TerrainChunk chunkAtCoords = m_terrain.Terrain.GetChunkAtCoords((num4 >> 4) + j, (num5 >> 4) + i);
					if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.Valid)
					{
						flag = false;
						break;
					}
					for (int k = 0; (k < 16) & flag; k++)
					{
						for (int l = 0; (l < 16) & flag; l++)
						{
							if (Terrain.ExtractContents(chunkAtCoords.GetCellValueFast(l, chunkAtCoords.GetTopHeightFast(l, k), k)) != 18)
							{
								flag = false;
								break;
							}
						}
					}
				}
			}
			num = (flag ? 1f : 0f);
		}
		num = (float)Math.Round(num * 20f) / 20f;
		float num6 = 0.2f * dt;
		OceanWaveStrength = ((num > OceanWaveStrength) ? MathUtils.Min(OceanWaveStrength + num6, num) : MathUtils.Max(OceanWaveStrength - num6, num));
		IsOcean = OceanWaveStrength >= 0.95f;
	}

	private float Rand2(Vector2 p)
	{
		float num = (float)Math.Sin(Vector2.Dot(p, new Vector2(12.9898f, 78.233f))) * 43758.547f;
		return num - MathUtils.Floor(num);
	}

	private float SmoothNoise(Vector2 p)
	{
		Vector2 vector = new Vector2(MathUtils.Floor(p.X), MathUtils.Floor(p.Y));
		Vector2 vector2 = new Vector2(p.X - MathUtils.Floor(p.X), p.Y - MathUtils.Floor(p.Y));
		float x = Rand2(vector);
		float x2 = Rand2(vector + new Vector2(1f, 0f));
		float x3 = Rand2(vector + new Vector2(0f, 1f));
		return MathUtils.Lerp(x2: MathUtils.Lerp(x3, Rand2(vector + new Vector2(1f, 1f)), vector2.X), x1: MathUtils.Lerp(x, x2, vector2.X), f: vector2.Y);
	}

	private float GetWaveHeight(Vector3 worldPos)
	{
		float num = (float)MathUtils.Remainder(m_subsystemGameInfo.TotalElapsedGameTime, 16384.0);
		float num2 = ((MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(0f, m_subsystemWeather.m_windSound.Volume), m_subsystemWeather.m_waterWindSound.Volume), m_subsystemWeather.m_lightWarmWindSound.Volume), m_subsystemWeather.m_averageWarmWindSound.Volume), m_subsystemWeather.m_stormWarmWindSound.Volume), m_subsystemWeather.m_lightColdWindSound.Volume), m_subsystemWeather.m_averageColdWindSound.Volume), m_subsystemWeather.m_stormColdWindSound.Volume), m_subsystemWeather.m_hurricaneColdWindSound.Volume), m_subsystemWeather.m_hurricaneWarmWindSound.Volume) > AudioManager.MinAudibleVolume) ? m_subsystemWeather.CurrentWindStrength : 0f);
		float num3 = ((MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(0f, m_subsystemWeather.m_windSound.Volume), m_subsystemWeather.m_waterWindSound.Volume), m_subsystemWeather.m_lightWarmWindSound.Volume), m_subsystemWeather.m_averageWarmWindSound.Volume), m_subsystemWeather.m_stormWarmWindSound.Volume), m_subsystemWeather.m_lightColdWindSound.Volume), m_subsystemWeather.m_averageColdWindSound.Volume), m_subsystemWeather.m_stormColdWindSound.Volume), m_subsystemWeather.m_hurricaneColdWindSound.Volume), m_subsystemWeather.m_hurricaneWarmWindSound.Volume) > AudioManager.MinAudibleVolume) ? m_subsystemWeather.PhaseWindStrength : 0f);
		Vector2 vector = -m_subsystemWeather.CurrentWindVector;
		Vector2 vector2 = new Vector2(0f - vector.Y, vector.X);
		Vector2 vector3 = Vector2.Normalize(new Vector2(vector.X * 0.70710677f - vector.Y * 0.70710677f, vector.X * 0.70710677f + vector.Y * 0.70710677f));
		Vector2 vector4 = Vector2.Normalize(new Vector2(vector.X * 0.8660254f + vector.Y * 0.5f, (0f - vector.X) * 0.5f + vector.Y * 0.8660254f));
		Vector2 vector5 = new Vector2(worldPos.X, worldPos.Z);
		float num4 = MathUtils.Saturate(MathUtils.Saturate(num3 / 16f) * OceanWaveStrength);
		float num5 = 1f - num4;
		float num6 = (0.015f + num2 * 0.008f) * 1f;
		float num7 = (0.015f + num2 * 0.008f) * 20f;
		float num8 = (1f + num3 * 0.15f) * 1f;
		float num9 = (1f + num3 * 0.15f) * 30f;
		float windPhaseMain = m_subsystemWeather.WindPhaseMain;
		float windPhaseSide = m_subsystemWeather.WindPhaseSide;
		float num10 = 0.7f * num + 3f / 35f * windPhaseMain;
		float num11 = 1.26f * num + 2.7f * windPhaseSide;
		float num12 = 0.42000002f * num + 0.051428575f * windPhaseMain;
		float num13 = 0.21000001f * num + 0.025714288f * windPhaseMain;
		float num14 = 0.35f * num + 3f / 70f * windPhaseMain;
		float num15 = 0.63f * num + 1.35f * windPhaseSide;
		float num16 = 0.21000001f * num + 0.025714288f * windPhaseMain;
		float num17 = 0.105000004f * num + 0.012857144f * windPhaseMain;
		float num18 = Vector2.Dot(vector5, vector * (4f / num8));
		float num19 = MathUtils.Sin(num10 + num18) * num6;
		float num20 = num6 * 0.5f;
		float num21 = num8 * 0.6f;
		float num22 = Vector2.Dot(vector5, vector2 * (4f / num21));
		float num23 = MathUtils.Sin(num11 + num22) * num20;
		float num24 = num6 * 0.4f;
		float num25 = num8 * 1.8f;
		float num26 = Vector2.Dot(vector5, vector3 * (4f / num25)) + Rand2(Vector2.Floor(vector5 * 0.002f)) * 6.2831f;
		float num27 = MathUtils.Sin(num12 + num26) * num24;
		float num28 = num6 * 0.25f;
		float num29 = num8 * 3f;
		float num30 = Vector2.Dot(vector5, vector4 * (4f / num29)) + Rand2(Vector2.Floor(vector5 * 0.003f)) * 6.2831f;
		float num31 = MathUtils.Sin(num13 + num30) * num28;
		float num32 = (SmoothNoise(vector5 * 0.07f + new Vector2(num * 0.09f)) - 0.5f) * num6 * 0.6f;
		float num33 = num19 + num23 + num27 + num31 + num32;
		float num34 = Vector2.Dot(vector5, vector * (4f / num9));
		float num35 = MathUtils.Sin(num14 + num34) * num7;
		float num36 = num7 * 0.5f;
		float num37 = num9 * 0.6f;
		float num38 = Vector2.Dot(vector5, vector2 * (4f / num37));
		float num39 = MathUtils.Sin(num15 + num38) * num36;
		float num40 = num7 * 0.4f;
		float num41 = num9 * 1.8f;
		float num42 = Vector2.Dot(vector5, vector3 * (4f / num41)) + Rand2(Vector2.Floor(vector5 * 0.002f)) * 6.2831f;
		float num43 = MathUtils.Sin(num16 + num42) * num40;
		float num44 = num7 * 0.25f;
		float num45 = num9 * 3f;
		float num46 = Vector2.Dot(vector5, vector4 * (4f / num45)) + Rand2(Vector2.Floor(vector5 * 0.003f)) * 6.2831f;
		float num47 = MathUtils.Sin(num17 + num46) * num44;
		float num48 = (SmoothNoise(vector5 * 0.07f + new Vector2(num * 0.09f)) - 0.5f) * num7 * 0.6f;
		float num49 = num35 + num39 + num43 + num47 + num48;
		return num33 * num5 + num49 * num4;
	}

	public float GetWaveSurfaceY(Vector3 worldPos)
	{
		float jsonObject2 = GetWaveHeight(worldPos);
		return (float)(OceanLevel + 1) + jsonObject2;
	}

	public Vector3 GetMainWaveDirection3D()
	{
		Vector2 v = -m_subsystemWeather.CurrentWindVector;
		if (v.Length() < 0.001f)
		{
			return Vector3.UnitX;
		}
		v = Vector2.Normalize(v);
		return new Vector3(v.X, 0f, v.Y);
	}

	public Vector2 GetWaveSlopeXZ(Vector3 worldPos)
	{
		float error = 0.25f;
		float waveSurfaceY = GetWaveSurfaceY(new Vector3(worldPos.X - error, worldPos.Y, worldPos.Z));
		float waveSurfaceY2 = GetWaveSurfaceY(new Vector3(worldPos.X + error, worldPos.Y, worldPos.Z));
		float waveSurfaceY3 = GetWaveSurfaceY(new Vector3(worldPos.X, worldPos.Y, worldPos.Z - error));
		float waveSurfaceY4 = GetWaveSurfaceY(new Vector3(worldPos.X, worldPos.Y, worldPos.Z + error));
		float x = (waveSurfaceY2 - waveSurfaceY) / (2f * error);
		float y = (waveSurfaceY4 - waveSurfaceY3) / (2f * error);
		return new Vector2(x, y);
	}

	public Vector3 GetWaveNormal(Vector3 worldPos)
	{
		Vector2 waveSlopeXZ = GetWaveSlopeXZ(worldPos);
		return Vector3.Normalize(new Vector3(0f - waveSlopeXZ.X, 1f, 0f - waveSlopeXZ.Y));
	}

	public float GetWaveDirectionalSlope(Vector3 worldPos, Vector3 dir)
	{
		Vector3 v = new Vector3(dir.X, 0f, dir.Z);
		if (v.Length() < 0.001f)
		{
			return 0f;
		}
		v = Vector3.Normalize(v);
		Vector2 waveSlopeXZ = GetWaveSlopeXZ(worldPos);
		return waveSlopeXZ.X * v.X + waveSlopeXZ.Y * v.Z;
	}

	public int ClassifyCrestOrTrough(Vector3 worldPos, Vector3 dir)
	{
		float num = 0.5f;
		Vector3 v = new Vector3(dir.X, 0f, dir.Z);
		if (v.Length() < 0.001f)
		{
			v = GetMainWaveDirection3D();
		}
		v = Vector3.Normalize(v);
		float waveSurfaceY = GetWaveSurfaceY(worldPos - v * num);
		float waveSurfaceY2 = GetWaveSurfaceY(worldPos);
		float waveSurfaceY3 = GetWaveSurfaceY(worldPos + v * num);
		if (MathUtils.Abs((waveSurfaceY3 - waveSurfaceY) / (2f * num)) > 0.01f)
		{
			return 0;
		}
		float num2 = (waveSurfaceY - 2f * waveSurfaceY2 + waveSurfaceY3) / (num * num);
		if (num2 < 0f)
		{
			return 1;
		}
		if (num2 > 0f)
		{
			return -1;
		}
		return 0;
	}

	public float GetBlockWaveSurfaceY(Vector3 worldPos, float blockBaseSurfaceY)
	{
		float f = ComputeKOWeight();
		float x = OceanLevel + 1;
		return MathUtils.Lerp(blockBaseSurfaceY, x, f) + GetWaveHeight(worldPos);
	}

	public Vector2 GetBlockWaveSlopeXZ(Vector3 worldPos, float blockBaseSurfaceY)
	{
		float blockWaveSurfaceY = GetBlockWaveSurfaceY(new Vector3(worldPos.X - 0.25f, worldPos.Y, worldPos.Z), blockBaseSurfaceY);
		float blockWaveSurfaceY2 = GetBlockWaveSurfaceY(new Vector3(worldPos.X + 0.25f, worldPos.Y, worldPos.Z), blockBaseSurfaceY);
		float blockWaveSurfaceY3 = GetBlockWaveSurfaceY(new Vector3(worldPos.X, worldPos.Y, worldPos.Z - 0.25f), blockBaseSurfaceY);
		return new Vector2(y: (GetBlockWaveSurfaceY(new Vector3(worldPos.X, worldPos.Y, worldPos.Z + 0.25f), blockBaseSurfaceY) - blockWaveSurfaceY3) / 0.5f, x: (blockWaveSurfaceY2 - blockWaveSurfaceY) / 0.5f);
	}

	public Vector3 GetBlockWaveNormal(Vector3 worldPos, float blockBaseSurfaceY)
	{
		Vector2 blockWaveSlopeXZ = GetBlockWaveSlopeXZ(worldPos, blockBaseSurfaceY);
		return Vector3.Normalize(new Vector3(0f - blockWaveSlopeXZ.X, 1f, 0f - blockWaveSlopeXZ.Y));
	}

	private float ComputeKOWeight()
	{
		if (m_subsystemWeather == null)
		{
			return 0f;
		}
		return MathUtils.Saturate(MathUtils.Saturate(((MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(MathUtils.Max(0f, m_subsystemWeather.m_windSound.Volume), m_subsystemWeather.m_waterWindSound.Volume), m_subsystemWeather.m_lightWarmWindSound.Volume), m_subsystemWeather.m_averageWarmWindSound.Volume), m_subsystemWeather.m_stormWarmWindSound.Volume), m_subsystemWeather.m_lightColdWindSound.Volume), m_subsystemWeather.m_averageColdWindSound.Volume), m_subsystemWeather.m_stormColdWindSound.Volume), m_subsystemWeather.m_hurricaneColdWindSound.Volume), m_subsystemWeather.m_hurricaneWarmWindSound.Volume) > AudioManager.MinAudibleVolume) ? m_subsystemWeather.PhaseWindStrength : 0f) / 16f) * OceanWaveStrength);
	}

	public bool IsBlockInOcean(int x, int z)
	{
		if (m_generator != null)
		{
			return m_generator.CalculateOceanShoreDistance(x, z) <= 0f;
		}
		if (m_generatorFlat != null)
		{
			return m_generatorFlat.CalculateOceanShoreDistance(x, z) <= 0f;
		}
		return false;
	}

	public float GetLocalOceanWaveStrength(Vector2 cameraPositionXZ)
	{
		float result = 0f;
		if (m_generator != null)
		{
			float num = m_generator.CalculateOceanShoreDistance(cameraPositionXZ.X, cameraPositionXZ.Y);
			if (num < 0f)
			{
				result = 1f;
			}
			else if (num < 100f)
			{
				result = 1f - num / 100f;
			}
		}
		else if (m_generatorFlat != null)
		{
			float num2 = m_generatorFlat.CalculateOceanShoreDistance(cameraPositionXZ.X, cameraPositionXZ.Y);
			if (num2 < 0f)
			{
				result = 1f;
			}
			else if (num2 < 100f)
			{
				result = 1f - num2 / 100f;
			}
		}
		return result;
	}
}
