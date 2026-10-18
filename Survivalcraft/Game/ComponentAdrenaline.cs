using System;
using System.Globalization;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentAdrenaline : Component, IUpdateable
{
	public float m_decayDelay;

	public const float DecayDelaySeconds = 20f;

	public const float DecayRatePerSecond = 0.05f;

	public const float MinPulseToReset = 0.0001f;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public ComponentPlayer m_componentPlayer;

	public float m_adrenaline;

	public float m_lastAdrenaline;

	public static string fName;

	public Random m_random = new Random();

	public float m_funnyTimer;

	private const int SoundVariants = 10;

	private static readonly string[] s_soundIds;

	private static readonly string SoundDir;

	private static readonly string SoundName;

	public float Adrenaline
	{
		get
		{
			return m_adrenaline;
		}
		set
		{
			m_adrenaline = MathUtils.Max(value, 0f);
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void AddAdrenaline(float amount)
	{
		if (!(amount <= 0f))
		{
			Adrenaline += amount;
			if (amount >= 0.0001f)
			{
				m_decayDelay = 20f;
			}
		}
	}

	public void Update(float dt)
	{
		if (m_componentPlayer.ComponentHealth.Health <= 0f)
		{
			return;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace)
		{
			m_funnyTimer = 0f;
			TrySync();
			return;
		}
		if (m_decayDelay > 0f)
		{
			m_decayDelay = MathUtils.Max(m_decayDelay - dt, 0f);
		}
		else if (m_adrenaline > 0f)
		{
			float num = 0.05f * dt;
			m_adrenaline = MathUtils.Max(m_adrenaline - num, 0f);
		}
		if (CommonLib.WorkType != WorkType.Client && !m_componentPlayer.ComponentSleep.IsSleeping && m_adrenaline >= 1f)
		{
			float f = MathUtils.Saturate((m_adrenaline - 1f) / 9f);
			float funnyTimer = MathUtils.Lerp(30f, 5f, f);
			m_funnyTimer -= dt;
			if (m_funnyTimer <= 0f)
			{
				m_funnyTimer = funnyTimer;
				int num2 = m_random.Int(0, 9);
				PlayAdrenalineSound(num2);
				CommonLib.Net.QueuePackage(new ComponentAdrenalinePackage(this, ComponentAdrenalinePackage.EventType.Sound, num2));
			}
		}
		else if (m_adrenaline < 1f)
		{
			m_funnyTimer = 0f;
		}
		TrySync();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_adrenaline = valuesDictionary.GetValue<float>("Adrenaline");
		m_lastAdrenaline = m_adrenaline;
		if (m_random == null)
		{
			m_random = new Random();
		}
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Adrenaline", m_adrenaline);
	}

	static ComponentAdrenaline()
	{
		SoundDir = "Audio/Creatures/Adrenaline/";
		SoundName = "Adrenaline";
		s_soundIds = InitSoundIds();
		fName = "ComponentAdrenaline";
	}

	private void TrySync()
	{
		double offset = (double)(GetHashCode() % 1000) / 1000.0;
		if (Time.PeriodicEvent(1.0, offset))
		{
			bool flag = Math.Abs(m_adrenaline - m_lastAdrenaline) > 0.01f;
			if (m_adrenaline == 0f && m_lastAdrenaline > 0f)
			{
				flag = true;
			}
			if (flag)
			{
				CommonLib.Net.QueuePackage(new ComponentAdrenalinePackage(this));
				m_lastAdrenaline = m_adrenaline;
			}
		}
	}

	public void PlayAdrenalineSound(int index)
	{
		index = MathUtils.Clamp(index, 0, 9);
		float flag = (float)(((m_componentPlayer.Entity.EntityId * 73856093) ^ (index * 19349663)) & 0x7FFFFFFF) / 2.1474836E+09f;
		float scissorRectangle2 = MathUtils.Lerp(-0.08f, 0.08f, flag);
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		Vector3 containerWidget = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetUpVector();
		Vector3 forwardVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
		Vector3 num = eyePosition - 0.08f * containerWidget + 0.15f * forwardVector;
		m_subsystemAudio.PlaySound(s_soundIds[index], 1f, scissorRectangle2, num, 8f, autoDelay: false);
	}

	private static string[] InitSoundIds()
	{
		string[] i = new string[10];
		for (int drawItem = 0; drawItem < 10; drawItem++)
		{
			i[drawItem] = SoundDir + SoundName + drawItem.ToString(CultureInfo.InvariantCulture);
		}
		return i;
	}
}
