using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemFocalorsMagmaBlockBehavior : SubsystemFocalorsFluidBlockBehavior, IUpdateable
{
	public new Random m_random = new Random();

	public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	public new SubsystemParticles m_subsystemParticles;

	public float m_soundVolume;

	public SubsystemWeather m_subsystemWeather;

	private static readonly Point3[] m_neighborOffsets;

	public SubsystemProjectiles m_subsystemProjectiles;

	public SubsystemPlayers m_subsystemPlayers;

	public override int[] HandledBlocks => new int[1] { 92 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public SubsystemFocalorsMagmaBlockBehavior()
		: base(92)
	{
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
	}

	public void Update(float dt)
	{
		if (SubsystemTime.PeriodicGameTimeEvent(2.0, 0.0))
		{
			SpreadFluid();
		}
		if (SubsystemTime.PeriodicGameTimeEvent(1.0, 0.75))
		{
			float num = float.MaxValue;
			foreach (Vector3 listenerPosition in SubsystemAudio.ListenerPositions)
			{
				float? num2 = CalculateDistanceToFluid(listenerPosition, 8, flowingFluidOnly: false);
				if (num2.HasValue && num2.Value < num)
				{
					num = num2.Value;
				}
			}
			m_soundVolume = SubsystemAudio.CalculateVolume(num, 2f, 3.5f);
		}
		SubsystemAmbientSounds.MagmaSoundVolume = MathUtils.Max(SubsystemAmbientSounds.MagmaSoundVolume, m_soundVolume);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		base.OnBlockAdded(value, oldValue, x, y, z);
		ApplyMagmaNeighborhoodEffect(x, y, z);
		Point3[] neighborOffsets = m_neighborOffsets;
		for (int i = 0; i < neighborOffsets.Length; i++)
		{
			Point3 point = neighborOffsets[i];
			ApplyMagmaNeighborhoodEffect(x + point.X, y + point.Y, z + point.Z);
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		base.OnNeighborBlockChanged(x, y, z, neighborX, neighborY, neighborZ);
		ApplyMagmaNeighborhoodEffect(neighborX, neighborY, neighborZ);
	}

	public override bool OnFluidInteract(int x, int y, int z, int fluidValue)
	{
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) != 18)
		{
			return base.OnFluidInteract(x, y, z, fluidValue);
		}
		SubsystemAudio.PlayRandomSound("Audio/Sizzles", 0.5f, m_random.Float(-0.2f, 0.2f), new Vector3(x, y, z), 10f, autoDelay: true);
		if (m_random.Float(0f, 1f) < 0.5f)
		{
			SpawnSteamParticles(x, y, z, 10);
		}
		SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(3));
		return true;
	}

	public void ApplyMagmaNeighborhoodEffect(int x, int y, int z)
	{
		if (!m_subsystemGameInfo.WorldSettings.FluidsDoNotBreakBlocks)
		{
			m_subsystemFireBlockBehavior.SetCellOnFire(x, y, z, 1f);
			switch (SubsystemTerrain.Terrain.GetCellContents(x, y, z))
			{
			case 61:
			case 62:
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, new Vector3((float)x + 0.5f, y + 1, (float)z + 0.5f)));
				break;
			case 8:
				SubsystemTerrain.ChangeCell(x, y, z, 2);
				m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, new Vector3((float)x + 0.5f, y + 1, (float)z + 0.5f)));
				break;
			}
		}
	}

	private void SpawnSteamParticles(int x, int y, int z, int count)
	{
		GunSmokeParticleSystem particleSystem = new GunSmokeParticleSystem(position: new Vector3((float)x + 0.5f, (float)y + 0.8f, (float)z + 0.5f), direction: new Vector3(m_random.Float(-0.2f, 0.2f), m_random.Float(0.1f, 0.2f), m_random.Float(-0.2f, 0.2f)), terrain: SubsystemTerrain)
		{
			ParticleSize = m_random.Float(0.3f, 0.6f),
			SpawnRateMultiplier = 0.1f
		};
		m_subsystemParticles.AddParticleSystem(particleSystem);
	}

	static SubsystemFocalorsMagmaBlockBehavior()
	{
		m_neighborOffsets = new Point3[10]
		{
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1),
			new Point3(1, 1, 0),
			new Point3(-1, 1, 0),
			new Point3(0, 1, 1),
			new Point3(0, 1, -1)
		};
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living && CommonLib.WorkType != WorkType.Client)
		{
			ComponentPlayer componentPlayer = m_subsystemPlayers.FindNearestPlayer(new Vector3(x, y, z));
			if (FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value)) >= 0.5f && componentPlayer != null && Vector3.DistanceSquared(new Vector3(x, y, z), componentPlayer.ComponentBody.Position) < 900f && m_random.Float(0f, 1f) < 0.015f)
			{
				SpitStone(x, y, z);
			}
		}
	}

	private void SpitStone(int x, int y, int z)
	{
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 1.2f, (float)z + 0.5f);
		int value = Terrain.MakeBlockValue(79, 0, StoneChunkBlock.SetKind(0, StoneChunkBlock.StoneKind.Heated));
		Vector3 velocity = new Vector3(m_random.Float(-6f, 6f), m_random.Float(7f, 24f), m_random.Float(-6f, 6f));
		m_subsystemProjectiles.AddProjectile(value, position, velocity, m_random.Vector3(5f), null);
		SubsystemAudio.PlayRandomSound("Audio/Sizzles", 0.3f, 0f, position, 6f, autoDelay: true);
	}
}
