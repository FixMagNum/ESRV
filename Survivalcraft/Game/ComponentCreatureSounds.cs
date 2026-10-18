using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCreatureSounds : Component
{
	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public ComponentCreature m_componentCreature;

	public Random m_random = new Random();

	public string m_idleSound;

	public string m_painSound;

	public string m_moanSound;

	public string m_sneezeSound;

	public string m_coughSound;

	public string m_pukeSound;

	public string m_attackSound;

	public float m_idleSoundMinDistance;

	public float m_painSoundMinDistance;

	public float m_moanSoundMinDistance;

	public float m_sneezeSoundMinDistance;

	public float m_coughSoundMinDistance;

	public float m_pukeSoundMinDistance;

	public float m_attackSoundMinDistance;

	public double m_lastSoundTime = -1000.0;

	public double m_lastCoughingSoundTime = -1000.0;

	public double m_lastPukeSoundTime = -1000.0;

	public string m_fallDamageSound;

	public float m_fallDamageSoundMinDistance;

	public void PlayIdleSound(bool skipIfRecentlyPlayed)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayIdleSoundLogic(skipIfRecentlyPlayed);
		}
	}

	public void PlayIdleSoundLogic(bool skipIfRecentlyPlayed)
	{
		if (!string.IsNullOrEmpty(m_idleSound) && m_subsystemTime.GameTime > m_lastSoundTime + (double)(skipIfRecentlyPlayed ? 12f : 1f))
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			float model = m_componentCreature.ComponentBody.Size;
			float boneAbsoluteTransform = MathUtils.Clamp(1f - model, -1f, 1f);
			float num = MathUtils.Clamp(model, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_idleSound, 1f * num, m_random.Float(-0.1f, 0.1f) + boneAbsoluteTransform, m_componentCreature.ComponentBody.Position, m_idleSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 0, skipIfRecentlyPlayed));
		}
	}

	public void PlayPainSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayPainSoundLogic();
		}
	}

	public void PlayPainSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_painSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			float state = m_componentCreature.ComponentBody.Size;
			float color2 = MathUtils.Clamp(1f - state, -1f, 1f);
			float color3 = MathUtils.Clamp(state, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_painSound, 1f * color3, m_random.Float(-0.1f, 0.1f) + color2, m_componentCreature.ComponentBody.Position, m_painSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 1));
		}
	}

	public void PlayMoanSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayMoanSoundLogic();
		}
	}

	public void PlayMoanSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_moanSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			float size = m_componentCreature.ComponentBody.Size;
			float num = MathUtils.Clamp(1f - size, -1f, 1f);
			float num2 = MathUtils.Clamp(size, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_moanSound, 1f * num2, m_random.Float(-0.1f, 0.1f) + num, m_componentCreature.ComponentBody.Position, m_moanSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 2));
		}
	}

	public void PlaySneezeSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlaySneezeSoundLogic();
		}
	}

	public void PlaySneezeSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_sneezeSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			float state = m_componentCreature.ComponentBody.Size;
			float block = MathUtils.Clamp(1f - state, -1f, 1f);
			float num = MathUtils.Clamp(state, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_sneezeSound, 1f * num, m_random.Float(-0.1f, 0.1f) + block, m_componentCreature.ComponentBody.Position, m_sneezeSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 3));
		}
	}

	public void PlayCoughSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayCoughSoundLogic();
		}
	}

	public void PlayCoughSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_coughSound) && m_subsystemTime.GameTime > m_lastCoughingSoundTime + 1.0)
		{
			m_lastCoughingSoundTime = m_subsystemTime.GameTime;
			float size = m_componentCreature.ComponentBody.Size;
			float num = MathUtils.Clamp(1f - size, -1f, 1f);
			float num2 = MathUtils.Clamp(size, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_coughSound, 1f * num2, m_random.Float(-0.1f, 0.1f) + num, m_componentCreature.ComponentBody.Position, m_coughSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 4));
		}
	}

	public void PlayPukeSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayPukeSoundLogic();
		}
	}

	public void PlayPukeSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_pukeSound) && m_subsystemTime.GameTime > m_lastPukeSoundTime + 1.0)
		{
			m_lastPukeSoundTime = m_subsystemTime.GameTime;
			float num = m_componentCreature.ComponentBody.Size;
			float focalorsFluidBlock = MathUtils.Clamp(1f - num, -1f, 1f);
			float terrain = MathUtils.Clamp(num, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_pukeSound, 1f * terrain, m_random.Float(-0.1f, 0.1f) + focalorsFluidBlock, m_componentCreature.ComponentBody.Position, m_pukeSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 5));
		}
	}

	public void PlayAttackSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayAttackSoundLogic();
		}
	}

	public void PlayAttackSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_attackSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			float x = m_componentCreature.ComponentBody.Size;
			float y = MathUtils.Clamp(1f - x, -1f, 1f);
			float z = MathUtils.Clamp(x, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_attackSound, 1f * z, m_random.Float(-0.1f, 0.1f) + y, m_componentCreature.ComponentBody.Position, m_attackSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 6));
		}
	}

	public bool PlayFootstepSound(float loudnessMultiplier)
	{
		return m_subsystemSoundMaterials.PlayFootstepSound(m_componentCreature, loudnessMultiplier);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_idleSound = valuesDictionary.GetValue<string>("IdleSound");
		m_painSound = valuesDictionary.GetValue<string>("PainSound");
		m_moanSound = valuesDictionary.GetValue<string>("MoanSound");
		m_sneezeSound = valuesDictionary.GetValue<string>("SneezeSound");
		m_coughSound = valuesDictionary.GetValue<string>("CoughSound");
		m_pukeSound = valuesDictionary.GetValue<string>("PukeSound");
		m_attackSound = valuesDictionary.GetValue<string>("AttackSound");
		m_fallDamageSound = valuesDictionary.GetValue<string>("FallDamageSound");
		m_idleSoundMinDistance = valuesDictionary.GetValue<float>("IdleSoundMinDistance");
		m_painSoundMinDistance = valuesDictionary.GetValue<float>("PainSoundMinDistance");
		m_moanSoundMinDistance = valuesDictionary.GetValue<float>("MoanSoundMinDistance");
		m_sneezeSoundMinDistance = valuesDictionary.GetValue<float>("SneezeSoundMinDistance");
		m_coughSoundMinDistance = valuesDictionary.GetValue<float>("CoughSoundMinDistance");
		m_pukeSoundMinDistance = valuesDictionary.GetValue<float>("PukeSoundMinDistance");
		m_attackSoundMinDistance = valuesDictionary.GetValue<float>("AttackSoundMinDistance");
		m_fallDamageSoundMinDistance = valuesDictionary.GetValue<float>("FallDamageSoundMinDistance");
	}

	public void PlayFallDamageSound()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			PlayFallDamageSoundLogic();
		}
	}

	public void PlayFallDamageSoundLogic()
	{
		if (!string.IsNullOrEmpty(m_fallDamageSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			float size = m_componentCreature.ComponentBody.Size;
			float num = MathUtils.Clamp(1f - size, -1f, 1f);
			float num2 = MathUtils.Clamp(size, 0.4f, 2f);
			m_subsystemAudio.PlayRandomSound(m_fallDamageSound, 1f * num2, m_random.Float(-0.1f, 0.1f) + num, m_componentCreature.ComponentBody.Position, m_fallDamageSoundMinDistance, autoDelay: false);
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, 7));
		}
	}
}
