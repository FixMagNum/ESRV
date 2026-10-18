using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;

namespace Game;

public class NetPlayerListWidget : CanvasWidget
{
	public ListPanelWidget players = new ListPanelWidget
	{
		Direction = LayoutDirection.Vertical
	};

	private PlayerData MainPlayer;

	private Project Project;

	public SubsystemPlayers SubsystemPlayers;

	public NetPlayerListWidget(PlayerData Main)
	{
		MainPlayer = Main;
		Project = Main.Project;
		Size = new Vector2(220f, 120f);
		Children.Add(players);
		VerticalAlignment = WidgetAlignment.Near;
		HorizontalAlignment = WidgetAlignment.Near;
		players.ItemSize = 30f;
		players.ItemWidgetFactory = (object obj) => new PlayerItemWidget(obj as PlayerData, MainPlayer, this);
		SubsystemPlayers = Project.FindSubsystem<SubsystemPlayers>();
		SubsystemPlayers.PlayerAdded += PlayerAdded;
		SubsystemPlayers.PlayerRemoved += PlayerRemoved;
		SortPlayerList();
	}

	public void SortPlayerList()
	{
		players.ClearItems();
		List<PlayerData> vector = new List<PlayerData>();
		List<PlayerData> num = new List<PlayerData>();
		foreach (PlayerData k in SubsystemPlayers.PlayersData)
		{
			if (k.Client != null && !k.IsMainPlayer)
			{
				if (MainPlayer.Friends.Contains(k.PlayerGUID))
				{
					vector.Add(k);
				}
				else
				{
					num.Add(k);
				}
			}
		}
		foreach (PlayerData x in vector)
		{
			players.AddItem(x);
		}
		foreach (PlayerData num3 in num)
		{
			players.AddItem(num3);
		}
		vector.Clear();
		num.Clear();
		if (!MainPlayer.ServerManager)
		{
			return;
		}
		foreach (string blackPlayerGuid in SubsystemPlayers.BlackPlayerGuidList)
		{
			Guid guid = new Guid(blackPlayerGuid);
			PlayerData playerData = SubsystemPlayers.PlayersData.Find((PlayerData playerData2) => playerData2.PlayerGUID == guid);
			if (playerData == null)
			{
				playerData = SubsystemPlayers.m_offlinePlayers.Find((PlayerData playerData2) => playerData2.PlayerGUID == guid);
			}
			if (playerData != null)
			{
				players.AddItem(playerData);
			}
		}
	}

	public override void Dispose()
	{
		SubsystemPlayers.PlayerAdded -= PlayerAdded;
		SubsystemPlayers.PlayerRemoved -= PlayerRemoved;
	}

	public void PlayerAdded(PlayerData playerData)
	{
		SortPlayerList();
	}

	public void PlayerRemoved(PlayerData playerData)
	{
		SortPlayerList();
	}

	public override void Update()
	{
	}
}
