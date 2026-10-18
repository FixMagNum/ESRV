using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemStrongCottonBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	private SubsystemTime m_subsystemTime;

	private SubsystemTerrain m_subsystemTerrain;

	private readonly Random m_random;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemWeather m_subsystemWeather;

	private Dictionary<Point3, WaterDripsParticleSystem> m_dripSystems = new Dictionary<Point3, WaterDripsParticleSystem>();

	public override int[] HandledBlocks => new int[1] { 365 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
	}

	public void Update(float dt)
	{
		if (!SettingsManager.CottonBlockParticles || !m_subsystemTime.PeriodicGameTimeEvent(0.25, 0.0))
		{
			return;
		}
		foreach (ComponentBody body in m_subsystemTerrain.Project.FindSubsystem<SubsystemBodies>().Bodies)
		{
			if (body.Velocity.LengthSquared() > 0.01f)
			{
				Vector3 position = body.Position;
				int num = Terrain.ToCell(position.X);
				int num2 = Terrain.ToCell(position.Y - 0.1f);
				int num3 = Terrain.ToCell(position.Z);
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(num, num2, num3);
				if (Terrain.ExtractContents(cellValue) == 365 && StrongCottonBlock.GetMode(Terrain.ExtractData(cellValue)) == StrongCottonBlock.CottonMode.Dry)
				{
					Vector3 position2 = new Vector3((float)num + m_random.Float(0f, 1f), (float)num2 + 1f, (float)num3 + m_random.Float(0f, 1f));
					m_subsystemParticles.AddParticleSystem(new GunSmokeParticleSystem(m_subsystemTerrain, position2, new Vector3(0f, 0.07f, 0f))
					{
						ParticleSize = 0.1f,
						SpawnRateMultiplier = 0.06f
					});
				}
			}
		}
	}

	public SubsystemStrongCottonBlockBehavior()
	{
		m_random = new Random();
	}

	private bool IsSkyOpen(int x, int y, int z, int yLimit)
	{
		Terrain terrain = m_subsystemTerrain.Terrain;
		for (int i = y + 1; i <= yLimit; i++)
		{
			int num = Terrain.ExtractContents(terrain.GetCellValue(x, i, z));
			if (BlocksManager.Blocks[num].IsCollidable)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsNearHot(int x, int y, int z, int r)
	{
		for (int ex = -r; ex <= r; ex++)
		{
			for (int i = -r; i <= r; i++)
			{
				for (int j = -r; j <= r; j++)
				{
					if (ex != 0 || i != 0 || j != 0)
					{
						int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x + ex, y + i, z + j));
						if (num == 92 || num == 104)
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private void PlayDryFx(int x, int y, int z, float particleSize)
	{
		m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 1f, (float)z + 0.5f);
		m_subsystemParticles.AddParticleSystem(new GunSmokeParticleSystem(m_subsystemTerrain, position, new Vector3(0f, 0.1f, 0f))
		{
			ParticleSize = particleSize,
			SpawnRateMultiplier = 0.1f
		});
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int nx, int ny, int nz)
	{
		if (ny == y - 1)
		{
			int ex = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
			UpdateDripSystem(x, y, z, ex);
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) != 365)
		{
			return;
		}
		int installMod = Terrain.ExtractData(cellValue);
		if (StrongCottonBlock.GetMode(installMod) == StrongCottonBlock.CottonMode.Dry)
		{
			if (AbsorbWater(x, y, z))
			{
				int data = StrongCottonBlock.SetMode(installMod, StrongCottonBlock.CottonMode.Wet);
				m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(365, 0, data));
			}
		}
		else if (IsNearHot(x, y, z, 2))
		{
			int data2 = StrongCottonBlock.SetMode(installMod, StrongCottonBlock.CottonMode.Dry);
			m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(365, 0, data2));
			PlayDryFx(x, y, z, 0.5f);
		}
	}

	private bool AbsorbWater(int x, int y, int z)
	{
		bool result = false;
		for (int i = -2; i <= 2; i++)
		{
			for (int j = -2; j <= 2; j++)
			{
				for (int k = -2; k <= 2; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						int x2 = x + i;
						int y2 = y + j;
						int z2 = z + k;
						if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x2, y2, z2)) == 18)
						{
							m_subsystemTerrain.ChangeCell(x2, y2, z2, 0);
							result = true;
						}
					}
				}
			}
		}
		return result;
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living || Terrain.ExtractContents(value) != 365)
		{
			return;
		}
		int path = Terrain.ExtractData(value);
		StrongCottonBlock.CottonMode num = StrongCottonBlock.GetMode(path);
		if (num == StrongCottonBlock.CottonMode.Wet && IsNearHot(x, y, z, 2))
		{
			int data = StrongCottonBlock.SetMode(path, StrongCottonBlock.CottonMode.Dry);
			m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(365, 0, data));
			PlayDryFx(x, y, z, 0.5f);
			return;
		}
		Terrain terrain = m_subsystemTerrain.Terrain;
		PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
		bool flag = precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f;
		int shaftValue = terrain.GetShaftValue(x, z);
		bool flag2 = SubsystemWeather.IsPlaceHot(terrain.GetSeasonalTemperature(shaftValue), y);
		int seasonalHumidity = terrain.GetSeasonalHumidity(shaftValue);
		int topHeight = terrain.GetTopHeight(x, z);
		bool flag3 = y >= topHeight && IsSkyOpen(x, y, z, precipitationShaftInfo.YLimit);
		if (((num == StrongCottonBlock.CottonMode.Dry) & flag & flag3) && !flag2)
		{
			float lightningIntensity = m_subsystemWeather.m_lightningIntensity;
			float num2;
			if (lightningIntensity >= 0.8f)
			{
				num2 = 0.9f;
			}
			else
			{
				num2 = ((!(lightningIntensity >= 0.1f)) ? 0.25f : 0.55f);
			}
			if (m_random.Float(0f, 1f) < num2)
			{
				int data2 = StrongCottonBlock.SetMode(path, StrongCottonBlock.CottonMode.Wet);
				m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(365, 0, data2));
			}
		}
		else if (num == StrongCottonBlock.CottonMode.Wet && !flag)
		{
			float num3 = 0f;
			if (flag2)
			{
				num3 += 0.6f;
			}
			if (seasonalHumidity < 6)
			{
				num3 += 0.4f;
			}
			num3 = MathUtils.Saturate(num3);
			if (m_random.Float(0f, 1f) < num3)
			{
				int data3 = StrongCottonBlock.SetMode(path, StrongCottonBlock.CottonMode.Dry);
				m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(365, 0, data3));
			}
		}
	}

	private void UpdateDripSystem(int x, int y, int z, int value)
	{
		Point3 key = new Point3(x, y, z);
		bool flag = StrongCottonBlock.GetMode(Terrain.ExtractData(value)) == StrongCottonBlock.CottonMode.Wet;
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
		bool flag2 = !BlocksManager.Blocks[cellContents].IsCollidable;
		if (flag & flag2)
		{
			if (!m_dripSystems.ContainsKey(key))
			{
				WaterDripsParticleSystem waterDripsParticleSystem = new WaterDripsParticleSystem(position: new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), terrain: m_subsystemTerrain, weather: m_subsystemWeather, isBlockSource: true);
				waterDripsParticleSystem.UseHangingPhase = true;
				waterDripsParticleSystem.WetnessLevel = 1f;
				waterDripsParticleSystem.DripRateMultiplier = 0.2f;
				m_subsystemParticles.AddParticleSystem(waterDripsParticleSystem);
				m_dripSystems[key] = waterDripsParticleSystem;
			}
		}
		else
		{
			RemoveDripSystem(key);
		}
	}

	private void RemoveDripSystem(Point3 key)
	{
		if (m_dripSystems.TryGetValue(key, out var value))
		{
			value.IsStopped = true;
			m_dripSystems.Remove(key);
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		UpdateDripSystem(x, y, z, value);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveDripSystem(new Point3(x, y, z));
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		UpdateDripSystem(x, y, z, value);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> list = new List<Point3>();
		foreach (Point3 key in m_dripSystems.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				list.Add(key);
			}
		}
		foreach (Point3 item in list)
		{
			RemoveDripSystem(item);
		}
	}
}
