using Engine;

namespace Game;

public class SubsystemWaterBlockBehavior : SubsystemFluidBlockBehavior, IUpdateable
{
	public Random m_random = new Random();

	public float m_soundVolume;

	public override int[] HandledBlocks => new int[1] { 437 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public SubsystemWaterBlockBehavior()
		: base(BlocksManager.FluidBlocks[437], generateSources: true)
	{
	}

	public void Update(float dt)
	{
		if (SubsystemTime.PeriodicGameTimeEvent(0.25, 0.0))
		{
			SpreadFluid();
		}
		if (SubsystemTime.PeriodicGameTimeEvent(1.0, 0.25))
		{
			float num = float.MaxValue;
			foreach (Vector3 listenerPosition in SubsystemAudio.ListenerPositions)
			{
				float? num2 = CalculateDistanceToFluid(listenerPosition, 8, flowingFluidOnly: true);
				if (num2.HasValue && num2.Value < num)
				{
					num = num2.Value;
				}
			}
			m_soundVolume = 0.5f * SubsystemAudio.CalculateVolume(num, 2f, 3.5f);
		}
		SubsystemAmbientSounds.WaterSoundVolume = MathUtils.Max(SubsystemAmbientSounds.WaterSoundVolume, m_soundVolume);
	}

	public override bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue)
	{
		if (BlocksManager.Blocks[Terrain.ExtractContents(interactValue)] is MagmaBlock)
		{
			SubsystemAudio.PlayRandomSound("Audio/Sizzles", 0.5f, m_random.Float(-0.2f, 0.2f), new Vector3(x, y, z), 5f, autoDelay: true);
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			Set(x, y, z, 3);
			if (m_random.Float(0f, 1f) <= 0.025f)
			{
				Project.FindSubsystem<SubsystemExplosions>(throwOnError: true).AddExplosion(x, y, z, 60f, isIncendiary: false, noExplosionSound: false);
				SubsystemBodies subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
				DynamicArray<ComponentBody> model = new DynamicArray<ComponentBody>();
				subsystemBodies.FindBodiesAroundPoint(new Vector2(x, z), 6f, model);
				for (int i = 0; i < model.Count; i++)
				{
					ComponentHealth variant = model.Array[i].Entity.FindComponent<ComponentHealth>();
					if (variant != null && !variant.IsInvulnerable)
					{
						string radians = LanguageControl.Get("ComponentHealth", "lava_explosion");
						variant.Injure(variant.Health, null, ignoreInvulnerability: false, radians);
					}
				}
				SubsystemAudio.PlaySound("Audio/ExplosionMedium", 4f, m_random.Float(-0.3f, 0f), new Vector3(x, y, z), 20f, 0f);
			}
			return true;
		}
		return base.OnFluidInteract(interactValue, x, y, z, fluidValue);
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

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		int num = Terrain.ExtractData(value);
		int num2 = num & 0xF;
		bool isTop = (num & 0x10) != 0;
		float level = (float)num2 / 15f;
		int data = FocalorsFluidBlock.SetLevel(0, level);
		data = FocalorsFluidBlock.SetIsTop(data, isTop);
		int value2 = Terrain.MakeBlockValue(18, 0, data);
		SubsystemTerrain.ChangeCell(x, y, z, value2, updateModificationCounter: false);
	}
}
