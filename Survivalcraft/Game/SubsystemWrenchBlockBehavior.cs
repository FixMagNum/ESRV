using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemWrenchBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private class DragSession
	{
		public ComponentBody Body;

		public float Distance;

		public double StartTime;

		public bool IsMenuTriggered;
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemPlayers m_subsystemPlayers;

	private HashSet<ComponentBody> m_frozenCreatures;

	private Dictionary<ComponentPlayer, double> m_lastHitTimes;

	private Dictionary<ComponentPlayer, DragSession> m_sessions;

	private Dictionary<ComponentPlayer, double> m_lastAirClickTimes = new Dictionary<ComponentPlayer, double>();

	public override int[] HandledBlocks => new int[1] { 368 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
	}

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		return false;
	}

	public void Update(float dt)
	{
		try
		{
			if (Time.PeriodicEvent(1.0, 0.0))
			{
				m_frozenCreatures.RemoveWhere((ComponentBody b) => b.Entity == null || !b.Entity.IsAddedToProject);
				foreach (ComponentBody body in m_subsystemBodies.Bodies)
				{
					try
					{
						if (!m_frozenCreatures.Contains(body))
						{
							ValuesDictionary valuesDictionary = body.Entity.ValuesDictionary;
							if (valuesDictionary.ContainsKey("IsWrenchFrozen") && valuesDictionary.GetValue<bool>("IsWrenchFrozen"))
							{
								m_frozenCreatures.Add(body);
							}
						}
					}
					catch (Exception)
					{
					}
				}
			}
			foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
			{
				ComponentPlayer componentPlayer = playersDatum.ComponentPlayer;
				if (componentPlayer == null)
				{
					continue;
				}
				ComponentMiner componentMiner = componentPlayer.ComponentMiner;
				if (componentMiner.ActiveBlockValue != 368)
				{
					if (m_sessions.ContainsKey(componentPlayer))
					{
						m_sessions.Remove(componentPlayer);
					}
				}
				else
				{
					HandleInput(componentPlayer, componentMiner, dt);
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Critical Wrench Error: {arg}");
		}
	}

	private string GetName(ComponentBody body)
	{
		ComponentPlayer componentPlayer = body.Entity.FindComponent<ComponentPlayer>();
		if (componentPlayer != null)
		{
			return componentPlayer.PlayerData.Name;
		}
		return body.Entity.ValuesDictionary.DatabaseObject.Name ?? "Entity";
	}

	public SubsystemWrenchBlockBehavior()
	{
		m_frozenCreatures = new HashSet<ComponentBody>();
		m_lastHitTimes = new Dictionary<ComponentPlayer, double>();
		m_sessions = new Dictionary<ComponentPlayer, DragSession>();
	}

	public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state)
	{
		return false;
	}

	private void HandleInput(ComponentPlayer player, ComponentMiner miner, float dt)
	{
		if (!player.PlayerData.ServerManager && CommonLib.WorkType != WorkType.Local)
		{
			if (player.ComponentInput.PlayerInput.Hit.HasValue || player.ComponentInput.PlayerInput.Aim.HasValue)
			{
				player.ComponentGui.DisplaySmallMessage("Wrench is restricted to Server Managers only!", Color.Red, blinking: true, playNotificationSound: true);
				miner.Poke(forceRestart: false);
			}
			return;
		}
		PlayerInput playerInput = player.ComponentInput.PlayerInput;
		Camera activeCamera = player.GameWidget.ActiveCamera;
		Ray3 ray = new Ray3(activeCamera.ViewPosition, activeCamera.ViewDirection);
		if (playerInput.Hit.HasValue)
		{
			if (!m_lastHitTimes.ContainsKey(player))
			{
				m_lastHitTimes[player] = 0.0;
			}
			if (Time.RealTime - m_lastHitTimes[player] > 0.3)
			{
				ComponentBody componentBody = FindTargetBody(player, ray);
				if (componentBody != null)
				{
					ToggleFreeze(player, componentBody);
					m_lastHitTimes[player] = Time.RealTime;
					miner.Poke(forceRestart: false);
				}
			}
		}
		if (playerInput.Aim.HasValue)
		{
			if (!m_sessions.ContainsKey(player))
			{
				ComponentBody componentBody2 = FindTargetBody(player, ray);
				if (componentBody2 != null)
				{
					m_sessions[player] = new DragSession
					{
						Body = componentBody2,
						Distance = Vector3.Distance(activeCamera.ViewPosition, componentBody2.Position),
						StartTime = Time.RealTime,
						IsMenuTriggered = false
					};
					return;
				}
				if (!m_lastAirClickTimes.ContainsKey(player))
				{
					m_lastAirClickTimes[player] = 0.0;
				}
				if (Time.RealTime - m_lastAirClickTimes[player] < 0.4)
				{
					OpenMenu(player, player.ComponentBody);
					m_lastAirClickTimes[player] = 0.0;
					miner.Poke(forceRestart: false);
				}
				else
				{
					m_lastAirClickTimes[player] = Time.RealTime;
				}
				return;
			}
			DragSession dragSession = m_sessions[player];
			if (Time.RealTime - dragSession.StartTime > 0.25)
			{
				if (dragSession.Body.Entity != null && dragSession.Body.Entity.IsAddedToProject)
				{
					Vector3 position = activeCamera.ViewPosition + activeCamera.ViewDirection * dragSession.Distance;
					dragSession.Body.Position = position;
					dragSession.Body.Velocity = Vector3.Zero;
				}
				else
				{
					m_sessions.Remove(player);
				}
			}
		}
		else if (m_sessions.ContainsKey(player))
		{
			DragSession dragSession2 = m_sessions[player];
			if (Time.RealTime - dragSession2.StartTime <= 0.25)
			{
				OpenMenu(player, dragSession2.Body);
			}
			m_sessions.Remove(player);
		}
	}

	private void ToggleFreeze(ComponentPlayer player, ComponentBody body)
	{
		if (m_frozenCreatures.Contains(body))
		{
			m_frozenCreatures.Remove(body);
			body.Entity.ValuesDictionary.SetValue("IsWrenchFrozen", value: false);
			player.ComponentGui.DisplaySmallMessage("Unfrozen: " + GetName(body), Color.Green, blinking: true, playNotificationSound: true);
		}
		else
		{
			m_frozenCreatures.Add(body);
			body.Entity.ValuesDictionary.SetValue("IsWrenchFrozen", value: true);
			player.ComponentGui.DisplaySmallMessage("Frozen: " + GetName(body), Color.Cyan, blinking: true, playNotificationSound: true);
		}
	}

	private void OpenMenu(ComponentPlayer player, ComponentBody body)
	{
		if (body.Entity != null)
		{
			EntityInspectorDialog dialog = new EntityInspectorDialog(body.Entity);
			DialogsManager.ShowDialog(player.GuiWidget, dialog);
			m_subsystemAudio.PlaySound("Audio/UI/ButtonClick", 1f, 0f, player.ComponentBody.Position, 2f, autoDelay: true);
		}
	}

	private ComponentBody FindTargetBody(ComponentPlayer player, Ray3 ray)
	{
		ComponentBody result = null;
		float num = 6f;
		foreach (ComponentBody body in m_subsystemBodies.Bodies)
		{
			if (body.Entity != player.Entity)
			{
				BoundingBox boundingBox = body.BoundingBox;
				float? num2 = ray.Intersection(boundingBox);
				if (num2.HasValue && num2.Value < num)
				{
					num = num2.Value;
					result = body;
				}
			}
		}
		return result;
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (Terrain.ExtractContents(value) == 368)
		{
			SubsystemTerrain.ChangeCellNoSend(x, y, z, 0);
		}
	}
}
