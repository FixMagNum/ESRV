using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentSleep : Component, IUpdateable
{
	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemTime m_subsystemTime;

	public SubsystemUpdate m_subsystemUpdate;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentPlayer m_componentPlayer;

	public double? m_sleepStartTime;

	public float m_sleepFactor;

	public bool m_allowManualWakeUp;

	public static string fName;

	public float m_minWetness;

	public float m_messageFactor;

	private static readonly Random m_random;

	private string m_currentSleepHint;

	public bool IsSleeping => m_sleepStartTime.HasValue;

	public float SleepFactor => m_sleepFactor;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public bool CanSleep(out string reason)
	{
		bool isAmphibian = m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water;
		float immersionDepth = m_componentPlayer.ComponentBody.ImmersionDepth;
		Block block = (m_componentPlayer.ComponentBody.StandingOnValue.HasValue ? BlocksManager.Blocks[Terrain.ExtractContents(m_componentPlayer.ComponentBody.StandingOnValue.Value)] : null);
		if (((!isAmphibian || (isAmphibian && immersionDepth <= 0f)) && block == null) || (isAmphibian ? (immersionDepth <= 0f) : (immersionDepth > 0f)))
		{
			reason = LanguageControl.Get(fName, (!isAmphibian) ? 1 : 100);
			return false;
		}
		if (block != null && block.SleepSuitability == 0f)
		{
			reason = LanguageControl.Get(fName, 2);
			return false;
		}
		if (m_componentPlayer.ComponentVitalStats.Sleep > 0.99f)
		{
			reason = LanguageControl.Get(fName, 3);
			return false;
		}
		if (isAmphibian ? (m_componentPlayer.ComponentVitalStats.Wetness < 0.5f) : (m_componentPlayer.ComponentVitalStats.Wetness > 0.95f))
		{
			reason = LanguageControl.Get(fName, isAmphibian ? 101 : 4);
			return false;
		}
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Vector3 vector = m_componentPlayer.ComponentBody.Position + new Vector3(i, 1f, j);
				Vector3 vector2 = new Vector3(vector.X, 255f, vector.Z);
				SubsystemTerrain subsystemTerrain = m_subsystemTerrain;
				Vector3 start = vector;
				Vector3 end = vector2;
				bool useInteractionBoxes = false;
				bool skipAirBlocks = true;
				if (!subsystemTerrain.Raycast(start, end, useInteractionBoxes, skipAirBlocks, (int value, float distance) =>
				{
					int num = Terrain.ExtractContents(value);
					if (!isAmphibian)
					{
						return num != 0;
					}
					return num != 0 && !(BlocksManager.Blocks[num] is FluidBlock);
				}).HasValue)
				{
					reason = LanguageControl.Get(fName, isAmphibian ? 103 : 5);
					return false;
				}
			}
		}
		reason = string.Empty;
		return true;
	}

	public void Sleep(bool allowManualWakeup)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new ComponentSleepPackage(this, ComponentSleepPackage.EventType.SleepRequest, allowManualWakeup));
			return;
		}
		NetSleep(allowManualWakeup);
		CommonLib.Net.QueuePackage(new ComponentSleepPackage(this, ComponentSleepPackage.EventType.Sleep, allowManualWakeup, result: true));
	}

	public void WakeUp()
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new ComponentSleepPackage(this, ComponentSleepPackage.EventType.WakeupRequest));
			return;
		}
		NetWakeUp();
		CommonLib.Net.QueuePackage(new ComponentSleepPackage(this, ComponentSleepPackage.EventType.WakeUp));
	}

	public void NetSleep(bool allowManualWakeup)
	{
		if (!IsSleeping)
		{
			m_sleepStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
			m_allowManualWakeUp = allowManualWakeup;
			m_minWetness = float.MaxValue;
			m_messageFactor = 0f;
			m_currentSleepHint = GetRandomHint();
			if (m_componentPlayer.PlayerStats != null)
			{
				m_componentPlayer.PlayerStats.TimesWentToSleep++;
			}
		}
	}

	public void NetWakeUp()
	{
		if (m_sleepStartTime.HasValue)
		{
			m_sleepStartTime = null;
			m_componentPlayer.PlayerData.SpawnPosition = m_componentPlayer.ComponentBody.Position + new Vector3(0f, 0.1f, 0f);
		}
	}

	public void Update(float dt)
	{
		if (IsSleeping && m_componentPlayer.ComponentHealth.Health > 0f)
		{
			float num = 0.33f * Time.FrameDuration;
			if (CommonLib.WorkType == WorkType.Server && m_componentPlayer.ComponentSleep.m_subsystemUpdate.UpdatesPerFrame == 1 && m_componentPlayer.ComponentSleep.IsSleeping)
			{
				num *= m_subsystemGameInfo.WorldSettings.RecoverFator;
			}
			m_sleepFactor = MathUtils.Min(m_sleepFactor + num, 1f);
			m_minWetness = MathUtils.Min(m_minWetness, m_componentPlayer.ComponentVitalStats.Wetness);
			m_componentPlayer.PlayerStats.TimeSlept += m_subsystemGameInfo.TotalElapsedGameTimeDelta;
			if ((m_componentPlayer.ComponentVitalStats.Sleep >= 1f || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) && m_subsystemTimeOfDay.TimeOfDay > 0.3f && m_subsystemTimeOfDay.TimeOfDay < 0.59999996f && m_sleepStartTime.HasValue && m_subsystemGameInfo.TotalElapsedGameTime > m_sleepStartTime + 180.0)
			{
				WakeUp();
			}
			if (m_componentPlayer.ComponentHealth.HealthChange < -0.005f && (m_componentPlayer.ComponentHealth.Health < 0.5f || m_componentPlayer.ComponentVitalStats.Sleep > 0.5f))
			{
				WakeUp();
			}
			bool isAmphibian = m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water;
			if ((isAmphibian ? (m_componentPlayer.ComponentVitalStats.Wetness < 0.4f) : (m_componentPlayer.ComponentVitalStats.Wetness > m_minWetness + 0.05f)) && m_componentPlayer.ComponentVitalStats.Sleep > 0.2f)
			{
				WakeUp();
				m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + 1.0, () =>
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, isAmphibian ? 102 : 6), Color.DarkMagenta, blinking: true, playNotificationSound: true);
				});
			}
			if (m_sleepStartTime.HasValue)
			{
				float num2 = (float)(m_subsystemGameInfo.TotalElapsedGameTime - m_sleepStartTime.Value);
				if (m_allowManualWakeUp && num2 > 10f)
				{
					if (m_componentPlayer.GameWidget.Input.Any && !DialogsManager.HasDialogs(m_componentPlayer.GameWidget))
					{
						m_componentPlayer.GameWidget.Input.Clear();
						WakeUp();
						m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + 2.0, () =>
						{
							m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 7), Color.DarkMagenta, blinking: true, playNotificationSound: false);
						});
					}
					m_messageFactor = MathUtils.Min(m_messageFactor + 0.5f * Time.FrameDuration, 1f);
					m_componentPlayer.ComponentScreenOverlays.Message = m_currentSleepHint;
					m_componentPlayer.ComponentScreenOverlays.MessageFactor = m_messageFactor;
				}
				if (!m_allowManualWakeUp && num2 > 5f)
				{
					m_messageFactor = MathUtils.Min(m_messageFactor + 1f * Time.FrameDuration, 1f);
					m_componentPlayer.ComponentScreenOverlays.Message = m_currentSleepHint;
					m_componentPlayer.ComponentScreenOverlays.MessageFactor = m_messageFactor;
				}
			}
		}
		else
		{
			m_sleepFactor = MathUtils.Max(m_sleepFactor - 1f * Time.FrameDuration, 0f);
		}
		m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_componentPlayer.ComponentScreenOverlays.BlackoutFactor, m_sleepFactor);
		if (m_sleepFactor > 0.01f)
		{
			m_componentPlayer.ComponentScreenOverlays.FloatingMessage = LanguageControl.Get(fName, 10);
			m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_sleepFactor - 0.9f));
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemUpdate = Project.FindSubsystem<SubsystemUpdate>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_sleepStartTime = valuesDictionary.GetValue<double>("SleepStartTime");
		m_allowManualWakeUp = valuesDictionary.GetValue<bool>("AllowManualWakeUp");
		if (m_sleepStartTime == 0.0)
		{
			m_sleepStartTime = null;
		}
		if (m_sleepStartTime.HasValue)
		{
			m_sleepFactor = 1f;
			m_minWetness = float.MaxValue;
		}
		m_componentPlayer.ComponentHealth.Attacked += (ComponentCreature creature) =>
		{
			if (IsSleeping && m_componentPlayer.ComponentVitalStats.Sleep > 0.25f)
			{
				WakeUp();
			}
		};
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("SleepStartTime", m_sleepStartTime.HasValue ? m_sleepStartTime.Value : 0.0);
		valuesDictionary.SetValue("AllowManualWakeUp", m_allowManualWakeUp);
	}

	static ComponentSleep()
	{
		m_random = new Random();
		fName = "ComponentSleep";
	}

	public string GetRandomHint()
	{
		int clientByGUID = 50;
		return LanguageControl.Get("ComponentSleepHints", m_random.Int(1, clientByGUID).ToString());
	}
}
