using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemPlantBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	public struct Replacement
	{
		public int RequiredValue;

		public int Value;
	}

	private class CricketState
	{
		public Sound Sound;

		public double StartPlayTime;

		public double NextStopTime;

		public double NextResumeTime;

		public double PlayInterval;

		public double PauseInterval;

		public bool IsPaused;

		public bool IsFrozen;

		public double FreezeTime;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemGameInfo m_subsystemGameInfo;

	public Random m_random;

	public Dictionary<Point3, Replacement> m_toReplace;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemSky m_subsystemSky;

	public SubsystemWeather m_subsystemWeather;

	private int m_nextCricketPollPass;

	private SubsystemAudio m_subsystemAudio;

	private int m_currentPollPass;

	private Vector3 m_lastCricketSpawnPos;

	private double m_lastCricketSpawnTime;

	private Dictionary<Point3, CricketState> m_cricketStates = new Dictionary<Point3, CricketState>();

	public override int[] HandledBlocks => new int[11]
	{
		19, 20, 24, 25, 28, 99, 131, 244, 132, 174,
		204
	};

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int num = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z));
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
		int num2 = Terrain.ExtractContents(cellValue);
		bool flag = num2 == 8 || num2 == 2 || num2 == 7 || num2 == 168 || num2 == 355 || num2 == 356;
		switch (num)
		{
		default:
			if (!flag)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		case 132:
		{
			Block block = BlocksManager.Blocks[num2];
			if (block.IsFaceTransparent(SubsystemTerrain, 4, cellValue) && !(block is FenceBlock))
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
		case 131:
		case 244:
			if (!flag)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living || y <= 0 || y >= 255 || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		if (pollPass != m_currentPollPass)
		{
			m_currentPollPass = pollPass;
			if (pollPass >= m_nextCricketPollPass && IsNightOrDawn())
			{
				m_nextCricketPollPass = pollPass + m_random.Int(1, 3);
				AddRandomCricketSources(7);
			}
			else if (!IsNightOrDawn())
			{
				StopAndClearAllCrickets();
			}
		}
		int contents = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[contents];
		int cellValueFast = m_subsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
		bool flag = false;
		bool flag2 = Terrain.ExtractContents(cellValueFast) == 168;
		if (flag2)
		{
			flag = SoilBlock.GetHydration(Terrain.ExtractData(cellValueFast));
		}
		if (flag2 && !flag && (contents == 174 || contents == 204) && m_random.Float(0f, 1f) < 0.2f)
		{
			int value2 = ((m_random.Float(0f, 1f) < 0.5f) ? Terrain.MakeBlockValue(28) : Terrain.MakeBlockValue(99));
			m_toReplace[new Point3(x, y, z)] = new Replacement
			{
				Value = value2,
				RequiredValue = value
			};
			return;
		}
		if (contents != 19 && contents != 24 && contents != 25 && contents != 20 && contents != 99 && contents != 28)
		{
			float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
			if (currentWindStrength > 10f && currentWindStrength <= 16f)
			{
				float num = 0.7f;
				if (m_random.Float(0f, 1f) < num && m_subsystemWeather.GetWindExposureFactor(new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f)) > 0f)
				{
					m_subsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
					return;
				}
			}
		}
		if (contents == 19)
		{
			GrowTallGrass(value, x, y, z, pollPass);
			return;
		}
		if (block is FlowerBlock)
		{
			GrowFlower(value, x, y, z, pollPass);
			return;
		}
		switch (contents)
		{
		case 131:
			GrowPumpkin(value, x, y, z, pollPass);
			break;
		case 174:
			GrowRye(value, x, y, z, pollPass);
			break;
		case 204:
			GrowCotton(value, x, y, z, pollPass);
			break;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_lastCricketSpawnPos = Vector3.Zero;
		m_lastCricketSpawnTime = 0.0;
	}

	public void Update(float dt)
	{
		List<Point3> list = new List<Point3>();
		double gameTime = m_subsystemTime.GameTime;
		foreach (KeyValuePair<Point3, CricketState> cricketState in m_cricketStates)
		{
			Point3 key = cricketState.Key;
			CricketState value = cricketState.Value;
			Sound sound = value.Sound;
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z)) != 19)
			{
				sound.Stop();
				list.Add(key);
				continue;
			}
			Vector3 v = Vector3.Zero;
			if (m_subsystemAudio != null && m_subsystemAudio.ListenerPositions.Count > 0)
			{
				v = m_subsystemAudio.ListenerPositions[0];
			}
			Vector3 v2 = new Vector3((float)key.X + 0.5f, (float)key.Y + 0.5f, (float)key.Z + 0.5f);
			float num = Vector3.Distance(v, v2);
			if (num > 80f)
			{
				sound.Stop();
				list.Add(key);
				continue;
			}
			if (num > 70f)
			{
				if (!value.IsFrozen)
				{
					value.IsFrozen = true;
					value.FreezeTime = gameTime;
				}
				sound.Volume = MathUtils.Lerp(sound.Volume, 0f, MathUtils.Saturate(3f * dt));
				continue;
			}
			if (value.IsFrozen)
			{
				double num2 = gameTime - value.FreezeTime;
				value.NextStopTime += num2;
				value.NextResumeTime += num2;
				value.StartPlayTime += num2;
				value.IsFrozen = false;
			}
			if (!value.IsPaused && gameTime >= value.NextStopTime)
			{
				value.IsPaused = true;
				value.NextResumeTime = gameTime + value.PauseInterval;
				sound.Stop();
			}
			else if (value.IsPaused && gameTime >= value.NextResumeTime)
			{
				value.IsPaused = false;
				value.NextStopTime = gameTime + value.PlayInterval;
				sound.Play();
				value.StartPlayTime = gameTime;
			}
			float x = 1f - num / 30f;
			float num3 = SettingsManager.SoundsVolume * MathUtils.Saturate(x);
			float num4 = 1f;
			double num5 = gameTime - value.StartPlayTime;
			float num6 = 2f;
			if (num5 < (double)num6)
			{
				num4 = (float)(num5 / (double)num6);
				num4 = MathUtils.Saturate(num4);
			}
			if (value.IsPaused)
			{
				num3 = 0f;
				num4 = 1f;
			}
			float x2 = num3 * num4;
			sound.Volume = MathUtils.Lerp(sound.Volume, x2, MathUtils.Saturate(3f * dt));
			if (sound.Volume < 0.01f)
			{
				if (sound.State == SoundState.Playing)
				{
					sound.Stop();
				}
			}
			else if (!value.IsPaused && sound.State != SoundState.Playing)
			{
				sound.Play();
			}
		}
		foreach (Point3 item in list)
		{
			m_cricketStates.Remove(item);
		}
		if (m_subsystemAudio != null && m_subsystemAudio.ListenerPositions.Count > 0)
		{
			Vector3 vector = m_subsystemAudio.ListenerPositions[0];
			if (Vector3.Distance(vector, m_lastCricketSpawnPos) >= 80f && IsNightOrDawn() && m_subsystemWeather.m_lightningIntensity <= 0.5f && m_subsystemWeather.CurrentWindStrength < 12f)
			{
				StopAndClearAllCrickets();
				AddRandomCricketSources(7);
				m_lastCricketSpawnPos = vector;
				m_lastCricketSpawnTime = m_subsystemTime.GameTime;
			}
		}
		if (!m_subsystemTime.PeriodicGameTimeEvent(30.0, 0.0))
		{
			return;
		}
		foreach (KeyValuePair<Point3, Replacement> item2 in m_toReplace)
		{
			Point3 key2 = item2.Key;
			if (Terrain.ReplaceLight(m_subsystemTerrain.Terrain.GetCellValue(key2.X, key2.Y, key2.Z), 0) == Terrain.ReplaceLight(item2.Value.RequiredValue, 0))
			{
				m_subsystemTerrain.ChangeCell(key2.X, key2.Y, key2.Z, item2.Value.Value);
			}
		}
		m_toReplace.Clear();
	}

	public void GrowTallGrass(int value, int x, int y, int z, int pollPass)
	{
		int data = Terrain.ExtractData(value);
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)) == 168 && pollPass % 3 == 0 && CanGrow(x, y, z, out var growthModifier) && pollPass % growthModifier == 0 && TallGrassBlock.GetIsSmall(data))
		{
			int data2 = TallGrassBlock.SetIsSmall(data, isSmall: false);
			int value2 = Terrain.ReplaceData(value, data2);
			m_toReplace[new Point3(x, y, z)] = new Replacement
			{
				Value = value2,
				RequiredValue = value
			};
		}
	}

	public void GrowFlower(int value, int x, int y, int z, int pollPass)
	{
		int data = Terrain.ExtractData(value);
		if (pollPass % 3 == 0 && CanGrow(x, y, z, out var growthModifier) && pollPass % growthModifier == 0 && FlowerBlock.GetIsSmall(data))
		{
			int data2 = FlowerBlock.SetIsSmall(data, isSmall: false);
			int value2 = Terrain.ReplaceData(value, data2);
			m_toReplace[new Point3(x, y, z)] = new Replacement
			{
				Value = value2,
				RequiredValue = value
			};
		}
	}

	public void GrowRye(int value, int x, int y, int z, int pollPass)
	{
		if (pollPass % 3 != 0 || !CanGrow(x, y, z, out var growthModifier) || pollPass % growthModifier != 0)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int size = RyeBlock.GetSize(data);
		if (size == 7)
		{
			return;
		}
		if (RyeBlock.GetIsWild(data))
		{
			if (size < 7)
			{
				int data2 = RyeBlock.SetSize(RyeBlock.SetIsWild(data, isWild: true), size + 1);
				int value2 = Terrain.ReplaceData(value, data2);
				m_toReplace[new Point3(x, y, z)] = new Replacement
				{
					Value = value2,
					RequiredValue = value
				};
			}
			return;
		}
		int cellValueFast = m_subsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
		bool flag = false;
		int num = 0;
		if (Terrain.ExtractContents(cellValueFast) == 168)
		{
			int data3 = Terrain.ExtractData(cellValueFast);
			flag = SoilBlock.GetHydration(data3);
			num = SoilBlock.GetNitrogen(data3);
		}
		float num2 = 0.8f;
		if (num > 0)
		{
			num2 -= 0.4f;
		}
		if (flag)
		{
			num2 -= 0.4f;
		}
		SubsystemWeather.Season currentSeason = m_subsystemWeather.CurrentSeason;
		double x2 = 1.0 - (m_subsystemWeather.NextSeasonChangeTime - m_subsystemTime.GameTime) / 144.0;
		x2 = MathUtils.Saturate(x2);
		switch (currentSeason)
		{
		case SubsystemWeather.Season.EarlyAutumn:
			num2 += 0.1f + (float)(x2 * 0.1);
			break;
		case SubsystemWeather.Season.Autumn:
			num2 += 0.4f + (float)(x2 * 0.2);
			break;
		case SubsystemWeather.Season.LateAutumn:
			num2 += 0.8f + (float)(x2 * 0.1);
			break;
		}
		if (m_random.Float(0f, 1f) < num2 && size == 3)
		{
			data = RyeBlock.SetIsWild(data, isWild: true);
		}
		int num3 = MathUtils.Min(size + 1, 7);
		data = RyeBlock.SetSize(data, num3);
		int value3 = Terrain.ReplaceData(value, data);
		m_toReplace[new Point3(x, y, z)] = new Replacement
		{
			Value = value3,
			RequiredValue = value
		};
		if (num3 == 7 && Terrain.ExtractContents(cellValueFast) == 168)
		{
			int data4 = SoilBlock.SetNitrogen(Terrain.ExtractData(cellValueFast), MathUtils.Max(num - 1, 0));
			int value4 = Terrain.ReplaceData(cellValueFast, data4);
			m_toReplace[new Point3(x, y - 1, z)] = new Replacement
			{
				Value = value4,
				RequiredValue = cellValueFast
			};
		}
	}

	public void GrowCotton(int value, int x, int y, int z, int pollPass)
	{
		if (pollPass % 3 != 0 || !CanGrow(x, y, z, out var growthModifier) || pollPass % growthModifier != 0)
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int size = CottonBlock.GetSize(data);
		if (size == 2)
		{
			return;
		}
		if (CottonBlock.GetIsWild(data))
		{
			if (size < 2)
			{
				int data2 = CottonBlock.SetSize(CottonBlock.SetIsWild(data, isWild: true), size + 1);
				int value2 = Terrain.ReplaceData(value, data2);
				m_toReplace[new Point3(x, y, z)] = new Replacement
				{
					Value = value2,
					RequiredValue = value
				};
			}
			return;
		}
		int cellValueFast = m_subsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
		bool flag = false;
		int num = 0;
		if (Terrain.ExtractContents(cellValueFast) == 168)
		{
			int data3 = Terrain.ExtractData(cellValueFast);
			flag = SoilBlock.GetHydration(data3);
			num = SoilBlock.GetNitrogen(data3);
		}
		float num2 = 0.8f;
		if (num > 0)
		{
			num2 -= 0.4f;
		}
		if (flag)
		{
			num2 -= 0.4f;
		}
		SubsystemWeather.Season currentSeason = m_subsystemWeather.CurrentSeason;
		double x2 = 1.0 - (m_subsystemWeather.NextSeasonChangeTime - m_subsystemTime.GameTime) / 144.0;
		x2 = MathUtils.Saturate(x2);
		switch (currentSeason)
		{
		case SubsystemWeather.Season.EarlyAutumn:
			num2 += 0.1f + (float)(x2 * 0.1);
			break;
		case SubsystemWeather.Season.Autumn:
			num2 += 0.4f + (float)(x2 * 0.2);
			break;
		case SubsystemWeather.Season.LateAutumn:
			num2 += 0.8f + (float)(x2 * 0.1);
			break;
		}
		if (m_random.Float(0f, 1f) < num2 && size == 1)
		{
			data = CottonBlock.SetIsWild(data, isWild: true);
		}
		int num3 = MathUtils.Min(size + 1, 2);
		data = CottonBlock.SetSize(data, num3);
		int value3 = Terrain.ReplaceData(value, data);
		m_toReplace[new Point3(x, y, z)] = new Replacement
		{
			Value = value3,
			RequiredValue = value
		};
		if (num3 == 2 && Terrain.ExtractContents(cellValueFast) == 168)
		{
			int data4 = SoilBlock.SetNitrogen(Terrain.ExtractData(cellValueFast), MathUtils.Max(num - 1, 0));
			int value4 = Terrain.ReplaceData(cellValueFast, data4);
			m_toReplace[new Point3(x, y - 1, z)] = new Replacement
			{
				Value = value4,
				RequiredValue = cellValueFast
			};
		}
	}

	public void GrowPumpkin(int value, int x, int y, int z, int pollPass)
	{
		if (pollPass % 3 != 0 || !CanGrow(x, y, z, out var cellValueFast) || pollPass % cellValueFast != 0)
		{
			return;
		}
		int vertices = Terrain.ExtractData(value);
		int indices = BasePumpkinBlock.GetSize(vertices);
		if (!BasePumpkinBlock.GetIsDead(vertices) && indices < 7)
		{
			int defaultTextureSlot = m_subsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
			bool cellValueFast2 = false;
			int textureSlotCount = 0;
			if (Terrain.ExtractContents(defaultTextureSlot) == 168)
			{
				int data = Terrain.ExtractData(defaultTextureSlot);
				cellValueFast2 = SoilBlock.GetHydration(data);
				textureSlotCount = SoilBlock.GetNitrogen(data);
			}
			float light = 0.15f;
			if (textureSlotCount > 0)
			{
				light -= 0.05f;
			}
			if (cellValueFast2)
			{
				light -= 0.05f;
			}
			SubsystemWeather.Season i = m_subsystemWeather.CurrentSeason;
			double x2 = 1.0 - (m_subsystemWeather.NextSeasonChangeTime - m_subsystemTime.GameTime) / 144.0;
			x2 = MathUtils.Saturate(x2);
			switch (i)
			{
			case SubsystemWeather.Season.EarlyAutumn:
				light += 0.1f + (float)(x2 * 0.1);
				break;
			case SubsystemWeather.Season.Autumn:
				light += 0.4f + (float)(x2 * 0.2);
				break;
			case SubsystemWeather.Season.LateAutumn:
				light += 0.8f + (float)(x2 * 0.1);
				break;
			}
			if (m_random.Float(0f, 1f) < light && indices == 3)
			{
				vertices = BasePumpkinBlock.SetIsDead(vertices, isDead: true);
			}
			int z2 = MathUtils.Min(indices + 1, 7);
			vertices = BasePumpkinBlock.SetSize(vertices, z2);
			int vector = Terrain.ReplaceData(value, vertices);
			m_toReplace[new Point3(x, y, z)] = new Replacement
			{
				Value = vector,
				RequiredValue = value
			};
		}
	}

	public SubsystemPlantBlockBehavior()
	{
		m_lastCricketSpawnPos = Vector3.Zero;
		m_random = new Random();
		m_toReplace = new Dictionary<Point3, Replacement>();
	}

	private bool CanGrow(int x, int y, int z, out int growthModifier)
	{
		int num = Terrain.ExtractLight(m_subsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z));
		float timeOfDay = m_subsystemSky.m_subsystemTimeOfDay.TimeOfDay;
		bool flag = false;
		for (int i = -3; i <= 3; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				for (int k = -3; k <= 3; k++)
				{
					if (i == 0 && j == 0 && k == 0)
					{
						continue;
					}
					int x2 = x + i;
					int y2 = y + j;
					int z2 = z + k;
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
					if (Terrain.ExtractContents(cellValue) != 371 || UVLightbulbBlock.GetLightIntensity(Terrain.ExtractData(cellValue)) <= 0)
					{
						continue;
					}
					bool flag2 = false;
					int num2 = Math.Max(Math.Abs(i), Math.Max(Math.Abs(j), Math.Abs(k)));
					for (int l = 1; l <= num2; l++)
					{
						int x3 = x + i * l / num2;
						int y3 = y + j * l / num2;
						int z3 = z + k * l / num2;
						int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(x3, y3, z3);
						if (!BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)].IsTransparent)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
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
				break;
			}
		}
		if ((timeOfDay >= 0.2f && timeOfDay <= 0.8f) | flag)
		{
			bool flag3 = true;
			int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(x, z);
			for (int m = y + 1; m <= topHeight; m++)
			{
				int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(x, m, z);
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue3)];
				if (block.IsCollidable && !block.IsTransparent)
				{
					flag3 = false;
					break;
				}
			}
			if (((num > 0) & flag3) | flag)
			{
				switch (m_subsystemWeather.CurrentSeason)
				{
				case SubsystemWeather.Season.EarlyAutumn:
				case SubsystemWeather.Season.Autumn:
					growthModifier = 5;
					break;
				case SubsystemWeather.Season.LateAutumn:
					growthModifier = 10;
					break;
				case SubsystemWeather.Season.Anabiosis:
					growthModifier = 2;
					break;
				case SubsystemWeather.Season.Winter:
					growthModifier = 3;
					break;
				default:
					growthModifier = 5;
					break;
				}
				return true;
			}
		}
		growthModifier = 0;
		return false;
	}

	private bool IsNightOrDawn()
	{
		float timeOfDay = m_subsystemSky.m_subsystemTimeOfDay.TimeOfDay;
		if (!(timeOfDay < 0.25f))
		{
			return timeOfDay > 0.75f;
		}
		return true;
	}

	private void StopAndClearAllCrickets()
	{
		foreach (KeyValuePair<Point3, CricketState> cricketState in m_cricketStates)
		{
			cricketState.Value.Sound.Stop();
		}
		m_cricketStates.Clear();
	}

	private void AddRandomCricketSources(int maxCrickets)
	{
		if (m_subsystemWeather.m_lightningIntensity > 0.5f || m_subsystemWeather.CurrentWindStrength >= 12f)
		{
			return;
		}
		Vector3 vector = Vector3.Zero;
		if (m_subsystemAudio != null && m_subsystemAudio.ListenerPositions.Count > 0)
		{
			vector = m_subsystemAudio.ListenerPositions[0];
		}
		if (m_cricketStates.Count >= maxCrickets)
		{
			return;
		}
		for (int componentInput = 0; componentInput < 50; componentInput++)
		{
			if (m_cricketStates.Count >= maxCrickets)
			{
				break;
			}
			float x = m_random.Float(0f, (float)Math.PI * 2f);
			float flag = m_random.Float(30f, 80f);
			float direction = MathUtils.Sin(x) * flag;
			float unitY = MathUtils.Cos(x) * flag;
			int vector3 = Terrain.ToCell(vector.X + direction);
			int num2 = Terrain.ToCell(vector.Z + unitY);
			int zero = MathUtils.Clamp(Terrain.ToCell(vector.Y) + m_random.Int(-10, 10), 0, 255);
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(vector3, zero, num2)) != 19)
			{
				continue;
			}
			Point3 point = new Point3(vector3, zero, num2);
			if (m_cricketStates.ContainsKey(point))
			{
				continue;
			}
			int num3 = m_random.Int(1, 4);
			string name = $"Audio/Cricket/Cricket{num3}";
			Sound sound = m_subsystemAudio.CreateSound(name);
			if (sound == null)
			{
				continue;
			}
			sound.IsLooped = true;
			sound.Volume = 0f;
			sound.Pitch = 1f + m_random.Float(-0.2f, 0.2f);
			double exponentialInterval = GetExponentialInterval(10.0, 60.0);
			double exponentialInterval2 = GetExponentialInterval(1.0, 10.0);
			double num4 = m_subsystemTime.GameTime + (double)m_random.Float(1f, 10f);
			CricketState cricketState = new CricketState();
			cricketState.Sound = sound;
			cricketState.StartPlayTime = num4;
			cricketState.IsPaused = false;
			cricketState.PlayInterval = exponentialInterval;
			cricketState.PauseInterval = exponentialInterval2;
			cricketState.NextStopTime = num4 + exponentialInterval;
			m_cricketStates[point] = cricketState;
			double gameTime = num4;
			m_subsystemTime.QueueGameTimeDelayedExecution(gameTime, () =>
			{
				if (m_cricketStates.ContainsKey(point))
				{
					sound.Play();
				}
			});
		}
	}

	private double GetExponentialInterval(double min, double max)
	{
		double num = 0.0 - Math.Log(1.0 - (double)m_random.Float(0f, 1f));
		double num2 = (max - min) / 5.0;
		return MathUtils.Clamp(min + num * num2, min, max);
	}
}
