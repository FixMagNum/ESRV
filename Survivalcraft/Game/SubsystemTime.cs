using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemTime : Subsystem
{
	public struct DelayedExecutionRequest
	{
		public double GameTime;

		public Action Action;
	}

	public const float MaxGameTimeDelta = 0.1f;

	public double m_gameTime;

	public float m_gameTimeDelta;

	public float m_prevGameTimeDelta;

	public float m_gameTimeFactor = 1f;

	public List<DelayedExecutionRequest> m_delayedExecutionsRequests = new List<DelayedExecutionRequest>();

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemUpdate m_subsystemUpdate;

	private SubsystemTimeOfDay m_subsystemTimeOfDay;

	private SubsystemGameInfo m_subsystemGameInfo;

	public double GameTime => m_gameTime;

	public float GameTimeDelta => m_gameTimeDelta;

	public float PreviousGameTimeDelta => m_prevGameTimeDelta;

	public float GameTimeFactor
	{
		get
		{
			return m_gameTimeFactor;
		}
		set
		{
			m_gameTimeFactor = MathUtils.Clamp(value, 0f, 256f);
		}
	}

	public float? FixedTimeStep { get; set; }

	public float TimeControlFactor { get; private set; } = 1f;

	public void NextFrame()
	{
		m_prevGameTimeDelta = m_gameTimeDelta;
		m_gameTimeDelta = ((!FixedTimeStep.HasValue) ? MathUtils.Min(Time.FrameDuration * m_gameTimeFactor, 0.1f) : MathUtils.Min(FixedTimeStep.Value * m_gameTimeFactor, 0.1f));
		m_gameTime += m_gameTimeDelta;
		int realTime = 0;
		while (realTime < m_delayedExecutionsRequests.Count)
		{
			DelayedExecutionRequest delayedExecutionRequest = m_delayedExecutionsRequests[realTime];
			if (delayedExecutionRequest.GameTime >= 0.0 && GameTime >= delayedExecutionRequest.GameTime)
			{
				m_delayedExecutionsRequests.RemoveAt(realTime);
				delayedExecutionRequest.Action();
			}
			else
			{
				realTime++;
			}
		}
		bool ex = false;
		bool flag = true;
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			if (componentPlayer.ComponentHealth.Health > 0f)
			{
				ComponentSleep componentSleep = componentPlayer.ComponentSleep;
				if (componentSleep != null && componentSleep.IsSleeping)
				{
					ex = true;
					if (componentSleep.SleepFactor < 1f)
					{
						flag = false;
					}
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				double? deathTime = componentPlayer.ComponentHealth.DeathTime;
				if (deathTime.HasValue && m_subsystemGameInfo.TotalElapsedGameTime - deathTime.Value >= 10.0)
				{
					ex = true;
				}
				else
				{
					flag = false;
				}
			}
		}
		if (ex)
		{
			m_subsystemTimeOfDay.TimeOfDayOffset += 0.005f * m_gameTimeDelta;
			if (flag)
			{
				FixedTimeStep = 0.3125f;
				m_subsystemUpdate.UpdatesPerFrame = 20;
			}
			else
			{
				FixedTimeStep = null;
				m_subsystemUpdate.UpdatesPerFrame = 1;
			}
		}
		else
		{
			FixedTimeStep = null;
			m_subsystemUpdate.UpdatesPerFrame = 1;
		}
		bool flag2 = false;
		if (CommonLib.WorkType != WorkType.Client)
		{
			foreach (ComponentPlayer componentPlayer2 in m_subsystemPlayers.ComponentPlayers)
			{
				if (componentPlayer2.PlayerData.IsMainPlayer && componentPlayer2.ComponentGui.IsGameMenuDialogVisible())
				{
					flag2 = true;
					break;
				}
			}
			if (flag2 && m_gameTimeFactor != 0f)
			{
				m_gameTimeFactor = 0f;
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new SubsystemTimePackage(GameTime, m_subsystemTimeOfDay.TimeOfDayOffset, 0f));
				}
			}
			else if (!flag2 && m_gameTimeFactor == 0f)
			{
				m_gameTimeFactor = TimeControlFactor;
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new SubsystemTimePackage(GameTime, m_subsystemTimeOfDay.TimeOfDayOffset, TimeControlFactor));
				}
			}
		}
	}

	public void QueueGameTimeDelayedExecution(double gameTime, Action action)
	{
		m_delayedExecutionsRequests.Add(new DelayedExecutionRequest
		{
			GameTime = gameTime,
			Action = action
		});
	}

	public bool PeriodicGameTimeEvent(double period, double offset)
	{
		double num = GameTime - offset;
		double num2 = MathUtils.Floor(num / period) * period;
		if (num >= num2)
		{
			return num - (double)GameTimeDelta < num2;
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemUpdate = Project.FindSubsystem<SubsystemUpdate>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	public void SetTimeControl(float factor)
	{
		if (factor == 0f)
		{
			TimeControlFactor = 0f;
		}
		else
		{
			TimeControlFactor = MathUtils.Clamp(factor, 0.5f, 4f);
		}
		GameTimeFactor = TimeControlFactor;
	}
}
