using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemFocalorsWaterBlockBehavior : SubsystemFocalorsFluidBlockBehavior, IUpdateable
{
	public new Random m_random;

	public SubsystemWeather m_subsystemWeather;

	public new SubsystemParticles m_subsystemParticles;

	public float GlobalWaterTemperatureOffset;

	private static readonly HashSet<int> m_hotBlocks;

	public SubsystemPickables m_subsystemPickables;

	public new SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemAmbientSounds m_subsystemAmbientSounds;

	public Dictionary<Pickable, double> m_boilingTimers;

	public static readonly Dictionary<int, int> m_rawToBoiledFoodMap;

	public HashSet<Pickable> m_candidates = new HashSet<Pickable>();

	private new SubsystemOceanScanner m_subsystemOceanScanner;

	private float m_surfVol;

	private float m_roughVol;

	private float m_stormVol;

	private float m_calmVol;

	private float m_fightVol;

	private float m_whirlVol;

	private float m_boilVol;

	public override int[] HandledBlocks => new int[1] { 18 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public SubsystemFocalorsWaterBlockBehavior()
		: base(18)
	{
		m_boilingTimers = new Dictionary<Pickable, double>();
		m_random = new Random();
	}

	public void Update(float dt)
	{
		if (SubsystemTime.PeriodicGameTimeEvent(0.25, 0.0))
		{
			SpreadFluid();
		}
		if (SubsystemTime.PeriodicGameTimeEvent(0.5, 0.0))
		{
			CalculateSoundParams();
		}
		m_subsystemAmbientSounds.OceanSurfVolume = MathUtils.Max(m_subsystemAmbientSounds.OceanSurfVolume, m_surfVol);
		m_subsystemAmbientSounds.OceanRoughVolume = MathUtils.Max(m_subsystemAmbientSounds.OceanRoughVolume, m_roughVol);
		m_subsystemAmbientSounds.OceanStormVolume = MathUtils.Max(m_subsystemAmbientSounds.OceanStormVolume, m_stormVol);
		m_subsystemAmbientSounds.RiverCalmVolume = MathUtils.Max(m_subsystemAmbientSounds.RiverCalmVolume, m_calmVol);
		m_subsystemAmbientSounds.RiverFightVolume = MathUtils.Max(m_subsystemAmbientSounds.RiverFightVolume, m_fightVol);
		m_subsystemAmbientSounds.RiverWhirlpoolVolume = MathUtils.Max(m_subsystemAmbientSounds.RiverWhirlpoolVolume, m_whirlVol);
		m_subsystemAmbientSounds.FermentationSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FermentationSoundVolume, m_boilVol);
		if (SubsystemTime.PeriodicGameTimeEvent(0.1, 0.0))
		{
			UpdateSteamParticles(dt);
		}
		if (SubsystemTime.PeriodicGameTimeEvent(0.25, 0.0))
		{
			ProcessBoiling();
		}
	}

	public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue)
	{
		if (y > 80 && SubsystemWeather.IsPlaceFrozen(SubsystemTerrain.Terrain.GetSeasonalTemperature(x, z), y))
		{
			dropValue.Value = Terrain.MakeBlockValue(62);
		}
		else
		{
			base.OnItemHarvested(x, y, z, blockValue, ref dropValue, ref newBlockValue);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_subsystemOceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		m_subsystemPickables.PickableAdded += (Pickable p) =>
		{
			if (IsPotentiallyBoilable(p))
			{
				m_candidates.Add(p);
			}
		};
		m_subsystemPickables.PickableRemoved += (Pickable p) =>
		{
			m_candidates.Remove(p);
			m_boilingTimers.Remove(p);
		};
	}

	public override bool OnFluidInteract(int x, int y, int z, int fluidValue)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) != 92)
		{
			return base.OnFluidInteract(x, y, z, fluidValue);
		}
		SubsystemAudio.PlayRandomSound("Audio/Sizzles", 0.5f, m_random.Float(-0.2f, 0.2f), new Vector3(x, y, z), 8f, autoDelay: true);
		if (m_random.Float(0f, 1f) < 0.3f)
		{
			GunSmokeParticleSystem gunSmokeParticleSystem = new GunSmokeParticleSystem(SubsystemTerrain, new Vector3((float)x + 0.5f, (float)y + 0.8f, (float)z + 0.5f), Vector3.Zero);
			gunSmokeParticleSystem.ParticleSize = 0.3f;
			m_subsystemParticles.AddParticleSystem(gunSmokeParticleSystem);
		}
		int contents = ((FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValue)) < 0.5f || m_random.Float(0f, 1f) > 0.5f) ? 3 : 67);
		SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(contents));
		return true;
	}

	public bool IsWaterHot(int x, int y, int z)
	{
		if (m_subsystemGameInfo.WorldSettings.TemperatureOffset >= 30f)
		{
			return true;
		}
		if (y < 5 || y > 250)
		{
			return false;
		}
		for (int i = 1; i <= 5; i++)
		{
			int num = y - i;
			int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, num, z);
			int num2 = Terrain.ExtractContents(cellValueFast);
			if (m_hotBlocks.Contains(num2) && (num2 != 209 || Terrain.ExtractData(cellValueFast) > 0))
			{
				return true;
			}
			if (num2 != 18)
			{
				int y2 = num - 1;
				int cellValueFast2 = SubsystemTerrain.Terrain.GetCellValueFast(x, y2, z);
				int num3 = Terrain.ExtractContents(cellValueFast2);
				if (!m_hotBlocks.Contains(num3) || (num3 == 209 && Terrain.ExtractData(cellValueFast2) <= 0))
				{
					break;
				}
				return true;
			}
		}
		return false;
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		base.OnPoll(value, x, y, z, pollPass);
	}

	private void SpawnHotSteamParticles(int x, int y, int z)
	{
		float num = m_random.Float(1.1f, 2.5f);
		Vector3 position = new Vector3((float)x + m_random.Float(0.1f, 0.9f), (float)y + num, (float)z + m_random.Float(0.1f, 0.9f));
		Vector3 vector = new Vector3(m_subsystemWeather.CurrentWindVector.X, 0f, m_subsystemWeather.CurrentWindVector.Y);
		Vector3 direction = Vector3.Normalize(new Vector3(0f, 1.5f, 0f) + vector * 0.5f);
		BreathSteamParticleSystem breathSteamParticleSystem = new BreathSteamParticleSystem(SubsystemTerrain, m_subsystemWeather);
		breathSteamParticleSystem.Position = position;
		breathSteamParticleSystem.Direction = direction;
		breathSteamParticleSystem.AlphaMultiplier = 0.2f;
		breathSteamParticleSystem.BaseSize = 0.12f * m_random.Float(1f, 3f);
		m_subsystemParticles.AddParticleSystem(breathSteamParticleSystem);
	}

	static SubsystemFocalorsWaterBlockBehavior()
	{
		m_rawToBoiledFoodMap = new Dictionary<int, int>
		{
			{ 77, 347 },
			{ 88, 346 },
			{ 161, 345 }
		};
		m_hotBlocks = new HashSet<int> { 209, 354, 65, 304, 92, 104 };
	}

	private void UpdateSteamParticles(float dt)
	{
		foreach (GameWidget gameWidget in Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true).GameWidgets)
		{
			Camera activeCamera = gameWidget.ActiveCamera;
			if (activeCamera == null)
			{
				continue;
			}
			Vector3 viewPosition = activeCamera.ViewPosition;
			Vector3 viewDirection = activeCamera.ViewDirection;
			for (int i = 0; i < 60; i++)
			{
				Vector3 vector = new Vector3(m_random.Float(-16f, 16f), m_random.Float(-6f, 6f), m_random.Float(-16f, 16f));
				Vector3 vector2 = viewPosition + vector;
				if (Vector3.Dot(Vector3.Normalize(vector2 - viewPosition), viewDirection) >= 0.4f)
				{
					int x = Terrain.ToCell(vector2.X);
					int num = Terrain.ToCell(vector2.Y);
					int z = Terrain.ToCell(vector2.Z);
					if (num >= 0 && num <= 255 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x, num, z)) == 18 && IsWaterHot(x, num, z) && SubsystemTerrain.Terrain.GetCellContents(x, num + 1, z) == 0)
					{
						SpawnHotSteamParticles(x, num, z);
					}
				}
			}
		}
	}

	private bool IsPotentiallyBoilable(Pickable p)
	{
		int num = Terrain.ExtractContents(p.Value);
		return num switch
		{
			118 => !EggBlock.GetIsCooked(Terrain.ExtractData(p.Value)), 
			442 => CactusChunkBlock.GetCookState(Terrain.ExtractData(p.Value)) == CactusChunkBlock.CookState.Raw, 
			198 => StringBlock.GetKind(Terrain.ExtractData(p.Value)) == StringBlock.StringKind.Bark, 
			_ => m_rawToBoiledFoodMap.ContainsKey(num), 
		};
	}

	private void ProcessBoiling()
	{
		double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		foreach (Pickable item in m_candidates.ToList())
		{
			if (item.ToRemove)
			{
				continue;
			}
			int x = Terrain.ToCell(item.Position.X);
			int z = Terrain.ToCell(item.Position.Z);
			int num = Terrain.ToCell(item.Position.Y);
			int num2 = Terrain.ToCell(item.Position.Y - 0.15f);
			int num3 = -1;
			if (num >= 0 && num <= 255 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x, num, z)) == 18)
			{
				num3 = num;
			}
			else if (num2 >= 0 && num2 <= 255 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x, num2, z)) == 18)
			{
				num3 = num2;
			}
			if (num3 != -1 && IsWaterHot(x, num3, z))
			{
				if (!m_boilingTimers.ContainsKey(item))
				{
					m_boilingTimers[item] = totalElapsedGameTime;
				}
				if (totalElapsedGameTime - m_boilingTimers[item] >= 280.0)
				{
					BoilItem(item);
				}
			}
			else
			{
				m_boilingTimers.Remove(item);
			}
		}
	}

	private void BoilItem(Pickable pickable)
	{
		int num = Terrain.ExtractContents(pickable.Value);
		bool flag = false;
		switch (num)
		{
		case 118:
		{
			int data = Terrain.ExtractData(pickable.Value);
			pickable.Value = Terrain.ReplaceData(pickable.Value, EggBlock.SetIsCooked(data, isCooked: true));
			flag = true;
			break;
		}
		case 442:
		{
			int data3 = CactusChunkBlock.SetCookState(Terrain.ExtractData(pickable.Value), CactusChunkBlock.CookState.Boiled);
			pickable.Value = Terrain.ReplaceData(pickable.Value, data3);
			flag = true;
			break;
		}
		case 198:
		{
			int data2 = StringBlock.SetKind(Terrain.ExtractData(pickable.Value), StringBlock.StringKind.BoiledBark);
			pickable.Value = Terrain.ReplaceData(pickable.Value, data2);
			flag = true;
			break;
		}
		default:
		{
			if (m_rawToBoiledFoodMap.TryGetValue(num, out var value))
			{
				pickable.Value = Terrain.MakeBlockValue(value);
				flag = true;
			}
			break;
		}
		}
		if (flag)
		{
			pickable.Velocity += new Vector3(m_random.Float(-1.5f, 1.5f), m_random.Float(4f, 6f), m_random.Float(-1.5f, 1.5f));
			SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 4f, autoDelay: true);
		}
		m_boilingTimers.Remove(pickable);
		m_candidates.Remove(pickable);
	}

	private void CalculateSoundParams()
	{
		m_surfVol = 0f;
		m_roughVol = 0f;
		m_stormVol = 0f;
		m_calmVol = 0f;
		m_fightVol = 0f;
		m_whirlVol = 0f;
		m_boilVol = 0f;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 22;
		int num5 = 2;
		Terrain terrain = SubsystemTerrain.Terrain;
		foreach (Vector3 listenerPosition in SubsystemAudio.ListenerPositions)
		{
			int num6 = Terrain.ToCell(listenerPosition.X);
			int num7 = Terrain.ToCell(listenerPosition.Z);
			int num8 = Terrain.ToCell(listenerPosition.Y);
			for (int i = num6 - num4; i <= num6 + num4; i += num5)
			{
				for (int j = num7 - num4; j <= num7 + num4; j += num5)
				{
					if (Math.Abs(i - num6) + Math.Abs(j - num7) > 30)
					{
						continue;
					}
					float? num9 = null;
					for (int num10 = MathUtils.Clamp(num8 + 12, 0, 255); num10 >= MathUtils.Clamp(num8 - 8, 0, 255); num10--)
					{
						if (Terrain.ExtractContents(terrain.GetCellValueFast(i, num10, j)) == 18)
						{
							float num11 = listenerPosition.X - ((float)i + 0.5f);
							float num12 = listenerPosition.Y - ((float)num10 + 0.5f);
							float num13 = listenerPosition.Z - ((float)j + 0.5f);
							float num14 = num11 * num11 + num12 * num12 + num13 * num13;
							if (num14 <= 625f)
							{
								float num15 = SubsystemAudio.CalculateVolume((float)Math.Sqrt(num14), 10f, 1f);
								if (num15 > 0.01f)
								{
									if (m_subsystemOceanScanner.IsBlockInOcean(i, j))
									{
										float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
										m_surfVol = MathUtils.Max(m_surfVol, num15 * (1f - MathUtils.Saturate(currentWindStrength / 8f)));
										m_roughVol = MathUtils.Max(m_roughVol, num15 * MathUtils.Clamp((currentWindStrength - 2f) / 6f, 0f, 1f) * 1.2f);
										m_stormVol = MathUtils.Max(m_stormVol, num15 * MathUtils.Saturate((currentWindStrength - 6f) / 8f) * 1.5f);
										break;
									}
									if (!num9.HasValue)
									{
										num9 = CalculateFlowSpeed(i, num10, j)?.Length() ?? 0f;
									}
									float value = num9.Value;
									num += num15 * (1f - MathUtils.Saturate(value / 0.3f));
									num2 += num15 * MathUtils.Saturate((value - 0.1f) * 3.5f);
									num3 += num15 * MathUtils.Saturate((value - 0.35f) * 5f);
								}
							}
						}
						else if (num9.HasValue)
						{
							num9 = null;
						}
					}
				}
			}
			for (int k = num6 - 10; k <= num6 + 10; k++)
			{
				for (int l = num7 - 10; l <= num7 + 10; l++)
				{
					float num16 = (float)k + 0.5f - listenerPosition.X;
					float num17 = (float)l + 0.5f - listenerPosition.Z;
					float num18 = num16 * num16 + num17 * num17;
					if (!(num18 <= 100f))
					{
						continue;
					}
					for (int num19 = MathUtils.Clamp(num8 + 10, 0, 255); num19 >= MathUtils.Clamp(num8 - 10, 0, 255); num19--)
					{
						if (Terrain.ExtractContents(terrain.GetCellValueFast(k, num19, l)) == 18)
						{
							float num20 = (float)num19 + 0.5f - listenerPosition.Y;
							float num21 = num18 + num20 * num20;
							if (num21 < 100f && IsWaterHot(k, num19, l))
							{
								m_boilVol = MathUtils.Max(m_boilVol, 1f - (float)Math.Sqrt(num21) / 10f);
							}
						}
					}
				}
			}
		}
		float num22 = (float)num4 * 2f / (float)num5 * ((float)num4 * 2f / (float)num5);
		m_calmVol = MathUtils.Clamp(num * 12f / num22, 0f, 0.8f);
		m_fightVol = MathUtils.Clamp(num2 * 20f / num22, 0f, 1f);
		m_whirlVol = MathUtils.Clamp(num3 * 30f / num22, 0f, 1.2f);
	}
}
