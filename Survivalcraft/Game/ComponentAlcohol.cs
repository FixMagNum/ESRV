using System;
using System.Globalization;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentAlcohol : Component, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public ComponentPlayer m_componentPlayer;

	public float m_alcohol;

	public float m_lastAlcohol;

	public static string fName;

	public Random m_random = new Random();

	public float m_stumbleDuration;

	public float m_stumbleStrength;

	public Vector3 m_stumbleDirection;

	public SubsystemAudio m_subsystemAudio;

	public float m_funnyTimer;

	private static readonly string[] s_funnyIds;

	private static readonly string FunnyDir;

	private static readonly string FunnyName;

	public float Alcohol
	{
		get
		{
			return m_alcohol;
		}
		set
		{
			m_alcohol = MathUtils.Max(value, 0f);
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void AddAlcohol(float amount)
	{
		if (!(amount <= 0f))
		{
			Alcohol += amount;
		}
	}

	public float GetSleepinessMultiplier()
	{
		if (m_alcohol < 0.1f)
		{
			return 1f;
		}
		float obj2 = 7.777778f;
		return MathUtils.Clamp(1f + (m_alcohol - 0.1f) * obj2, 1f, 100f);
	}

	public void Update(float dt)
	{
		if (m_componentPlayer.ComponentHealth.Health <= 0f)
		{
			return;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || !m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			if (m_alcohol > 0f)
			{
				m_alcohol = MathUtils.Max(m_alcohol - dt, 0f);
			}
			m_stumbleDuration = 0f;
			TrySync();
			return;
		}
		m_alcohol = MathUtils.Max(m_alcohol - dt / 14000f, 0f);
		if (m_componentPlayer.ComponentSleep.IsSleeping && m_componentPlayer.ComponentSleep.m_subsystemUpdate.UpdatesPerFrame == 1 && CommonLib.WorkType == WorkType.Server)
		{
			m_alcohol = MathUtils.Max(m_alcohol - dt / 2600f, 0f);
		}
		if (m_alcohol > 1f && CommonLib.WorkType != WorkType.Client && m_componentPlayer.ComponentHealth.Health > 0f && !m_componentPlayer.ComponentSleep.IsSleeping && m_subsystemTime.PeriodicGameTimeEvent(60.0, -0.07))
		{
			ComponentSickness componentSickness = m_componentPlayer.ComponentSickness;
			if (componentSickness != null)
			{
				if (componentSickness.m_sicknessDuration < 10f)
				{
					componentSickness.m_sicknessDuration = 10f;
				}
				componentSickness.NauseaEffect();
				CommonLib.Net.QueuePackage(new ComponentSicknessPackage(componentSickness));
			}
		}
		if (CommonLib.WorkType != WorkType.Client && !m_componentPlayer.ComponentSleep.IsSleeping && m_alcohol >= 1f)
		{
			float f = MathUtils.Saturate((m_alcohol - 1f) / 9f);
			float funnyTimer = MathUtils.Lerp(30f, 5f, f);
			m_funnyTimer -= dt;
			if (m_funnyTimer <= 0f)
			{
				m_funnyTimer = funnyTimer;
				int num = m_random.Int(0, 9);
				PlayFunnySound(num);
				CommonLib.Net.QueuePackage(new ComponentAlcoholPackage(this, ComponentAlcoholPackage.EventType.FunnySound, num));
			}
		}
		else if (m_alcohol < 1f)
		{
			m_funnyTimer = 0f;
		}
		if (!m_componentPlayer.ComponentSleep.IsSleeping && m_alcohol >= 0.5f)
		{
			if (m_stumbleDuration <= 0f)
			{
				float f2 = MathUtils.Saturate((m_alcohol - 0.5f) / 0.5f);
				float f3 = MathUtils.Saturate((m_alcohol - 1f) / 9f);
				float x = MathUtils.Lerp(8f, 3f, f2);
				x = MathUtils.Lerp(x, 0.6f, f3);
				if (m_subsystemTime.PeriodicGameTimeEvent(x, -0.013))
				{
					m_stumbleDuration = MathUtils.Lerp(0.8f, 3f, f2) + MathUtils.Lerp(0f, 27f, f3);
					m_stumbleStrength = MathUtils.Lerp(0.6f, 3f, f2) + MathUtils.Lerp(0f, 12f, f3);
					Vector3 forwardVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
					Vector3 rightVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetRightVector();
					forwardVector.Y = 0f;
					rightVector.Y = 0f;
					forwardVector = Vector3.Normalize(forwardVector);
					rightVector = Vector3.Normalize(rightVector);
					float num2 = m_random.Float(0f, 1f);
					if (num2 < 0.25f)
					{
						m_stumbleDirection = forwardVector;
					}
					else if (num2 < 0.5f)
					{
						m_stumbleDirection = -forwardVector;
					}
					else if (num2 < 0.75f)
					{
						m_stumbleDirection = rightVector;
					}
					else
					{
						m_stumbleDirection = -rightVector;
					}
				}
			}
			if (m_stumbleDuration > 0f)
			{
				m_stumbleDuration = MathUtils.Max(m_stumbleDuration - dt, 0f);
				if (CommonLib.WorkType != WorkType.Client)
				{
					Vector3 impulse = m_stumbleDirection * (m_stumbleStrength * dt);
					impulse.Y = 0f;
					m_componentPlayer.ComponentBody.ApplyImpulse(impulse);
				}
			}
		}
		TrySync();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_alcohol = valuesDictionary.GetValue<float>("Alcohol");
		m_lastAlcohol = m_alcohol;
		if (m_random == null)
		{
			m_random = new Random();
		}
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Alcohol", m_alcohol);
	}

	static ComponentAlcohol()
	{
		FunnyDir = "Audio/Creatures/DrunkFunny/";
		FunnyName = "DrunkFunny";
		s_funnyIds = InitFunnyIds();
		fName = "ComponentAlcohol";
	}

	private void TrySync()
	{
		double jsonObject2 = (double)(GetHashCode() % 1000) / 1000.0;
		if (Time.PeriodicEvent(1.0, jsonObject2))
		{
			bool obj2 = Math.Abs(m_alcohol - m_lastAlcohol) > 0.01f;
			if (m_alcohol == 0f && m_lastAlcohol > 0f)
			{
				obj2 = true;
			}
			if (obj2)
			{
				CommonLib.Net.QueuePackage(new ComponentAlcoholPackage(this));
				m_lastAlcohol = m_alcohol;
			}
		}
	}

	public void PlayFunnySound(int index)
	{
		index = MathUtils.Clamp(index, 0, 9);
		float f = (float)(((m_componentPlayer.Entity.EntityId * 73856093) ^ (index * 19349663)) & 0x7FFFFFFF) / 2.1474836E+09f;
		float pitch = MathUtils.Lerp(-0.12f, 0.12f, f);
		Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		Vector3 upVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetUpVector();
		Vector3 forwardVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
		Vector3 position = eyePosition - 0.08f * upVector + 0.15f * forwardVector;
		m_subsystemAudio.PlaySound(s_funnyIds[index], 1f, pitch, position, 8f, autoDelay: false);
	}

	private static string[] InitFunnyIds()
	{
		string[] array = new string[21];
		for (int i = 0; i < 21; i++)
		{
			array[i] = FunnyDir + FunnyName + i.ToString(CultureInfo.InvariantCulture);
		}
		return array;
	}
}
