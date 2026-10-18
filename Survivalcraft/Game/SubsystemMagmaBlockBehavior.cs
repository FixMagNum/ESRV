using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemMagmaBlockBehavior : SubsystemFluidBlockBehavior, IUpdateable
{
	public Random m_random = new Random();

	public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	public SubsystemParticles m_subsystemParticles;

	public float m_soundVolume;

	public override int[] HandledBlocks => new int[1] { 438 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public SubsystemMagmaBlockBehavior()
		: base(BlocksManager.FluidBlocks[438], generateSources: false)
	{
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
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					ApplyMagmaNeighborhoodEffect(x + i, y + j, z + k);
				}
			}
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		base.OnNeighborBlockChanged(x, y, z, neighborX, neighborY, neighborZ);
		ApplyMagmaNeighborhoodEffect(neighborX, neighborY, neighborZ);
	}

	public override bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue)
	{
		if (BlocksManager.Blocks[Terrain.ExtractContents(interactValue)] is WaterBlock)
		{
			SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 5f, autoDelay: true);
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			float num = m_random.Float(0f, 1f);
			if (num <= 1E-05f)
			{
				Set(x, y, z, 344);
			}
			else if (num <= 0.50001f)
			{
				Set(x, y, z, 5);
			}
			else
			{
				Set(x, y, z, 67);
			}
			if (CheckForWaterAround(x, y, z) && m_random.Float(0f, 1f) <= 0.025f)
			{
				Project.FindSubsystem<SubsystemExplosions>(throwOnError: true).AddExplosion(x, y, z, 60f, isIncendiary: false, noExplosionSound: false);
				SubsystemBodies subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
				DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
				subsystemBodies.FindBodiesAroundPoint(new Vector2(x, z), 6f, dynamicArray);
				for (int i = 0; i < dynamicArray.Count; i++)
				{
					ComponentHealth componentHealth = dynamicArray.Array[i].Entity.FindComponent<ComponentHealth>();
					if (componentHealth != null && !componentHealth.IsInvulnerable)
					{
						string cause = LanguageControl.Get("ComponentHealth", "lava_explosion");
						componentHealth.Injure(componentHealth.Health, null, ignoreInvulnerability: false, cause);
					}
				}
				SubsystemAudio.PlaySound("Audio/ExplosionMedium", 4f, m_random.Float(-0.3f, 0f), new Vector3(x, y, z), 20f, 0f);
			}
			return true;
		}
		return base.OnFluidInteract(interactValue, x, y, z, fluidValue);
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
	}

	public void ApplyMagmaNeighborhoodEffect(int x, int y, int z)
	{
		m_subsystemFireBlockBehavior.SetCellOnFire(x, y, z, 1f);
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y, z);
		if (cellContents != 8)
		{
			if (cellContents - 61 <= 1)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, new Vector3((float)x + 0.5f, y + 1, (float)z + 0.5f)));
			}
		}
		else
		{
			SubsystemTerrain.ChangeCell(x, y, z, 2);
			m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, new Vector3((float)x + 0.5f, y + 1, (float)z + 0.5f)));
		}
	}

	private bool CheckForWaterAround(int x, int y, int z)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x + i, y + j, z + k)) == 18)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		int num = Terrain.ExtractData(value);
		int num2 = num & 0xF;
		bool isTop = (num & 0x10) != 0;
		float level = (float)num2 / 15f;
		int data = FocalorsFluidBlock.SetLevel(0, level);
		data = FocalorsFluidBlock.SetIsTop(data, isTop);
		int value2 = Terrain.MakeBlockValue(92, 0, data);
		SubsystemTerrain.ChangeCell(x, y, z, value2, updateModificationCounter: false);
	}
}
