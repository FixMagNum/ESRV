using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemGameWidgets : Subsystem, IUpdateable
{
	public const int MaxMassageCount = 200;

	public SubsystemPlayers m_subsystemPlayers;

	public List<GameWidget> m_gameWidgets = new List<GameWidget>();

	public GameWidget mainPlayerWidget;

	private Queue<string> m_playerMessages = new Queue<string>();

	public PlayerData MainPlayerData;

	public GamesWidget GamesWidget { get; set; }

	public ReadOnlyList<GameWidget> GameWidgets => new ReadOnlyList<GameWidget>(m_gameWidgets);

	public SubsystemTerrain SubsystemTerrain { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Views;

	public List<string> PlayerMessages => new List<string>(m_playerMessages);

	public event Action<string> OnMessageRecieved;

	public void AddMessage(string msg, string playerName = null, byte type = 0, List<byte> toClients = null)
	{
		CommonLib.Net.QueuePackage(new MessagePackage(playerName, msg, type, toClients));
		if (toClients != null && CommonLib.MainPlayer != null && CommonLib.MainPlayer.PlayerData.ClientId.HasValue)
		{
			toClients.Add(CommonLib.MainPlayer.PlayerData.ClientId.Value);
		}
		AddNetMessage(msg, playerName, type, toClients);
	}

	public void AddNetMessage(string msg, string playerName = null, byte type = 0, List<byte> toClients = null)
	{
		if (msg.StartsWith("#ACH#"))
		{
			try
			{
				string[] array = msg.Split('|');
				if (array.Length >= 4)
				{
					string text = array[1];
					string text2 = array[2];
					string arg = array[3];
					if (MainPlayerData != null && MainPlayerData.Name == text)
					{
						ComponentPlayer componentPlayer = MainPlayerData.ComponentPlayer;
						if (componentPlayer != null && componentPlayer.PlayerAchievements != null)
						{
							string text3 = text2;
							if (text3.EndsWith("Name"))
							{
								text3 = text3.Substring(0, text3.Length - 4);
							}
							FieldInfo field = typeof(PlayerAchievements).GetField(text3);
							if (field != null && (long)field.GetValue(componentPlayer.PlayerAchievements) == 0L)
							{
								field.SetValue(componentPlayer.PlayerAchievements, 1L);
							}
						}
					}
					string arg2 = LanguageControl.Get("PlayerAchievements", text2);
					string msg2 = string.Format(LanguageControl.Get("SubsystemGameWidgets", "achievement_chat_format"), text, arg, arg2);
					insert(msg2, 0);
					return;
				}
			}
			catch (Exception ex)
			{
				Log.Warning("Achievement error: " + ex.Message);
			}
		}
		string arg3 = "";
		switch (type)
		{
		case 2:
			arg3 = LanguageControl.Get("SubsystemGameWidgets", "private_message");
			break;
		case 1:
			arg3 = LanguageControl.Get("SubsystemGameWidgets", "team_message");
			break;
		}
		string msg3 = string.Format("{0}{1}{2}", arg3, (playerName == null) ? LanguageControl.Get("SubsystemGameWidgets", "system_message") : ("[" + playerName + "]"), msg);
		if (toClients != null && toClients.Count > 0 && toClients.Contains(CommonLib.Net.Self.ID))
		{
			insert(msg3, type);
		}
		else if (type == 0)
		{
			insert(msg3, 0);
		}
	}

	public void insert(string msg, byte type = 0)
	{
		if (type == 0)
		{
			while (m_playerMessages.Count >= 200)
			{
				m_playerMessages.Dequeue();
			}
			m_playerMessages.Enqueue(msg);
		}
		Log.Information(msg);
		if (CommonLib.WorkType == WorkType.Server && GameScreen.m_chatWriter != null)
		{
			string text = "[GLOBAL] ";
			switch (type)
			{
			case 1:
				text = "[TEAM] ";
				break;
			case 2:
				text = "[DM] ";
				break;
			default:
				if (msg.Contains("Событие началось") || msg.Contains("Achievement"))
				{
					text = "[SYS] ";
				}
				break;
			}
			GameScreen.m_chatWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + text + msg);
			GameScreen.m_chatWriter.Flush();
		}
		OnMessageRecieved?.Invoke(msg);
	}

	public float CalculateSquaredDistanceFromNearestView(Vector3 p)
	{
		float result = float.MaxValue;
		foreach (GameWidget num in m_gameWidgets)
		{
			float num2 = Vector3.DistanceSquared(p, num.ActiveCamera.ViewPosition);
			if (num2 < result)
			{
				result = num2;
			}
		}
		return result;
	}

	public float CalculateDistanceFromNearestView(Vector3 p)
	{
		return MathUtils.Sqrt(CalculateSquaredDistanceFromNearestView(p));
	}

	public void Update(float dt)
	{
		foreach (GameWidget gameWidget in GameWidgets)
		{
			gameWidget.ActiveCamera.Update(Time.FrameDuration);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemPlayers.PlayerAdded += (PlayerData playerData) =>
		{
			AddGameWidgetForPlayer(playerData);
		};
		m_subsystemPlayers.PlayerRemoved += (PlayerData playerData) =>
		{
			if (playerData.GameWidget != null)
			{
				RemoveGameWidget(playerData.GameWidget);
			}
		};
		if (CommonLib.MainPlayer != null)
		{
			MainPlayerData = CommonLib.MainPlayer.PlayerData;
		}
		GamesWidget = valuesDictionary.GetValue<GamesWidget>("GamesWidget");
		foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
		{
			Console.WriteLine("Widget load : " + playersDatum.Name);
			AddGameWidgetForPlayer(playersDatum);
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
	}

	public override void Dispose()
	{
		GameWidget[] path = GameWidgets.ToArray();
		foreach (GameWidget i in path)
		{
			RemoveGameWidget(i);
			i.Dispose();
		}
	}

	public void AddGameWidgetForPlayer(PlayerData playerData)
	{
		GameWidget gameWidget = new GameWidget(playerData, playerData.PlayerIndex);
		m_gameWidgets.Add(gameWidget);
		GamesWidget.Children.Add(gameWidget);
		playerData.GameWidget = gameWidget;
	}

	public void RemoveGameWidget(GameWidget gameWidget)
	{
		m_gameWidgets.Remove(gameWidget);
		GamesWidget.Children.Remove(gameWidget);
	}
}
