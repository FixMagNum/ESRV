using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemClockBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemAudio m_subsystemAudio;

	private SubsystemTimeOfDay m_subsystemTimeOfDay;

	private Random m_random;

	private double m_lastUseTime;

	private SubsystemTime m_subsystemTime;

	private SubsystemPlayers m_subsystemPlayers;

	private Dictionary<ComponentPlayer, double> m_nextTickTimes;

	public override int[] HandledBlocks => new int[1] { 380 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_random = new Random();
		m_nextTickTimes = new Dictionary<ComponentPlayer, double>();
		m_lastUseTime = 0.0;
	}

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		double realTime = Time.RealTime;
		if (realTime - m_lastUseTime < 0.4)
		{
			return false;
		}
		m_lastUseTime = realTime;
		string text = default(DateTime).AddDays(m_subsystemTimeOfDay.TimeOfDay).ToString("HH:mm");
		componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Yellow, blinking: true, playNotificationSound: true);
		m_subsystemAudio.PlaySound("Audio/UseClock", 0.3f, m_random.Float(0.3f, 0.5f), ray.Position, 4f, autoDelay: true);
		componentMiner.DamageActiveTool(1);
		return true;
	}

	public void Update(float dt)
	{
		if (Project.FindSubsystem<SubsystemGameInfo>().WorldSettings.GameMode == GameMode.Creative)
		{
			return;
		}
		double gameTime = m_subsystemTime.GameTime;
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			IInventory inventory = componentPlayer.ComponentMiner?.Inventory;
			if (inventory == null)
			{
				continue;
			}
			bool flag = false;
			for (int i = 0; i < inventory.SlotsCount; i++)
			{
				if (Terrain.ExtractContents(inventory.GetSlotValue(i)) == 380)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				if (!m_nextTickTimes.TryGetValue(componentPlayer, out var value))
				{
					value = 0.0;
				}
				if (gameTime >= value)
				{
					Vector3 position = componentPlayer.ComponentBody.Position;
					m_subsystemAudio.PlaySound("Audio/ClockTick", 0.1f, m_random.Float(-0.05f, 0.05f), position, 6f, autoDelay: true);
					m_nextTickTimes[componentPlayer] = gameTime + 1.0;
				}
			}
			else
			{
				m_nextTickTimes.Remove(componentPlayer);
			}
		}
		foreach (ComponentPlayer item in new List<ComponentPlayer>(m_nextTickTimes.Keys))
		{
			if (!m_subsystemPlayers.ComponentPlayers.Contains(item))
			{
				m_nextTickTimes.Remove(item);
			}
		}
	}
}
