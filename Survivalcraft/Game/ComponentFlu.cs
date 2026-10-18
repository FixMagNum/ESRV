using System;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFlu : Component, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemParticles m_subsystemParticles;

	public ComponentPlayer m_componentPlayer;

	public Random m_random = new Random();

	public float m_fluOnset;

	public static string fName = "ComponentFlu";

	public float m_fluDuration;

	public float m_coughDuration;

	public float m_sneezeDuration;

	public float m_blackoutDuration;

	public float m_blackoutFactor;

	public double m_lastEffectTime = -1000.0;

	public double m_lastCoughTime = -1000.0;

	public double m_lastMessageTime = -1000.0;

	private float m_netLastFluOnset;

	private float m_netLastFluDuration;

	public bool HasFlu => m_fluDuration > 0f;

	public bool IsCoughing => m_coughDuration > 0f;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void StartFlu()
	{
		if (m_fluDuration == 0f)
		{
			m_componentPlayer.PlayerStats.TimesHadFlu++;
		}
		m_fluDuration = 2700f;
		m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + 10.0, () =>
		{
			m_componentPlayer.ComponentVitalStats.MakeSleepy(0.2f);
		});
	}

	public void Sneeze()
	{
		m_sneezeDuration = 1f;
		m_componentPlayer.ComponentCreatureSounds.PlaySneezeSound();
		Project.FindSubsystem<SubsystemNoise>(throwOnError: true).MakeNoise(m_componentPlayer.ComponentBody.Position, 0.25f, 10f);
	}

	public void Cough()
	{
		m_lastCoughTime = m_subsystemTime.GameTime;
		m_coughDuration = 4f;
		m_componentPlayer.ComponentCreatureSounds.PlayCoughSound();
		Project.FindSubsystem<SubsystemNoise>(throwOnError: true).MakeNoise(m_componentPlayer.ComponentBody.Position, 0.25f, 10f);
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			double list = (double)(GetHashCode() % 1000) / 1000.0;
			if (Time.PeriodicEvent(1.0, list))
			{
				bool flag = false;
				if (Math.Abs(m_fluDuration - m_netLastFluDuration) > 0.01f || Math.Abs(m_fluOnset - m_netLastFluOnset) > 0.01f)
				{
					flag = true;
				}
				if (m_fluDuration == 0f && m_netLastFluDuration > 0f)
				{
					flag = true;
				}
				if (m_fluOnset == 0f && m_netLastFluOnset > 0f)
				{
					flag = true;
				}
				if (flag)
				{
					CommonLib.Net.QueuePackage(new ComponentFluPackage(this, ComponentFluPackage.EventType.SyncStat));
					m_netLastFluDuration = m_fluDuration;
					m_netLastFluOnset = m_fluOnset;
				}
			}
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || !m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			m_fluDuration = 0f;
			m_fluOnset = 0f;
			return;
		}
		if (m_fluDuration > 0f)
		{
			m_fluOnset = 0f;
			float screen = 1f;
			if (m_componentPlayer.ComponentVitalStats.Temperature > 16f)
			{
				screen = 2f;
			}
			else if (m_componentPlayer.ComponentVitalStats.Temperature > 12f)
			{
				screen = 1.5f;
			}
			else if (m_componentPlayer.ComponentVitalStats.Temperature < 8f)
			{
				screen = 0.5f;
			}
			m_fluDuration = MathUtils.Max(m_fluDuration - screen * dt, 0f);
			if (m_componentPlayer.ComponentHealth.Health > 0f && !m_componentPlayer.ComponentSleep.IsSleeping && m_subsystemTime.PeriodicGameTimeEvent(15.0, -0.009999999776482582) && m_subsystemTime.GameTime - m_lastEffectTime > 39.0 && CommonLib.WorkType != WorkType.Client)
			{
				FluEffect();
				CommonLib.Net.QueuePackage(new ComponentFluPackage(this, ComponentFluPackage.EventType.FluEffect));
			}
		}
		else if (m_componentPlayer.ComponentVitalStats.Temperature < 6f)
		{
			float num = 13f;
			m_fluOnset += dt;
			if (m_fluOnset > 120f)
			{
				num = 9f;
				if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0) && m_random.Bool(0.025f) && CommonLib.WorkType != WorkType.Client)
				{
					StartFlu();
					CommonLib.Net.QueuePackage(new ComponentFluPackage(this, ComponentFluPackage.EventType.StartFlu));
				}
				if (m_subsystemTime.GameTime - m_lastMessageTime > 60.0)
				{
					m_lastMessageTime = m_subsystemTime.GameTime;
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.Cyan, blinking: true, playNotificationSound: true);
				}
			}
			if (m_fluOnset > 60f && m_subsystemTime.PeriodicGameTimeEvent(num, -0.009999999776482582) && m_random.Bool(0.75f) && CommonLib.WorkType != WorkType.Client)
			{
				Sneeze();
				CommonLib.Net.QueuePackage(new ComponentFluPackage(this, ComponentFluPackage.EventType.Sneeze));
			}
		}
		else
		{
			m_fluOnset = 0f;
		}
		if ((m_coughDuration > 0f || m_sneezeDuration > 0f) && m_componentPlayer.ComponentHealth.Health > 0f && !m_componentPlayer.ComponentSleep.IsSleeping)
		{
			m_coughDuration = MathUtils.Max(m_coughDuration - dt, 0f);
			m_sneezeDuration = MathUtils.Max(m_sneezeDuration - dt, 0f);
			float item = MathUtils.DegToRad(MathUtils.Lerp(-35f, -65f, SimplexNoise.Noise(4f * (float)MathUtils.Remainder(m_subsystemTime.GameTime, 10000.0))));
			m_componentPlayer.ComponentLocomotion.LookOrder = new Vector2(m_componentPlayer.ComponentLocomotion.LookOrder.X, MathUtils.Clamp(item - m_componentPlayer.ComponentLocomotion.LookAngles.Y, -3f, 3f));
			if (m_random.Bool(2f * dt) && CommonLib.WorkType != WorkType.Client)
			{
				Vector3 impulse = -1.2f * m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
				m_componentPlayer.ComponentBody.ApplyImpulse(impulse);
			}
		}
		if (m_blackoutDuration > 0f)
		{
			m_blackoutDuration = MathUtils.Max(m_blackoutDuration - dt, 0f);
			m_blackoutFactor = MathUtils.Min(m_blackoutFactor + 0.5f * dt, 0.95f);
		}
		else if (m_blackoutFactor > 0f)
		{
			m_blackoutFactor = MathUtils.Max(m_blackoutFactor - 0.5f * dt, 0f);
		}
		m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_blackoutFactor, m_componentPlayer.ComponentScreenOverlays.BlackoutFactor);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_fluDuration = valuesDictionary.GetValue<float>("FluDuration");
		m_fluOnset = valuesDictionary.GetValue<float>("FluOnset");
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("FluDuration", m_fluDuration);
		valuesDictionary.SetValue("FluOnset", m_fluOnset);
	}

	public void FluEffect()
	{
		m_lastEffectTime = m_subsystemTime.GameTime;
		m_blackoutDuration = MathUtils.Lerp(4f, 2f, m_componentPlayer.ComponentHealth.Health);
		if (Time.FrameStartTime - m_lastMessageTime > 60.0)
		{
			m_lastMessageTime = Time.FrameStartTime;
			m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + 1.5, () =>
			{
				if (m_componentPlayer.ComponentVitalStats.Temperature < 8f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.Cyan, blinking: true, playNotificationSound: true);
				}
				else
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.Cyan, blinking: true, playNotificationSound: true);
				}
			});
		}
		if (m_coughDuration == 0f && (m_subsystemTime.GameTime - m_lastCoughTime > 120.0 || m_random.Bool(0.167f)))
		{
			Cough();
		}
		else if (m_sneezeDuration == 0f)
		{
			Sneeze();
		}
	}
}
