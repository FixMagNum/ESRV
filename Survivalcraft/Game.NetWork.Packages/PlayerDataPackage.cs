using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game.NetWork.Packages;

public class PlayerDataPackage : IPackage
{
	public enum DataType
	{
		Create,
		Modify,
		Delete,
		ClientKnownPlayer,
		AddPlayer,
		SetUpdateLocation,
		CloseTime,
		AddNoMsg,
		RemoveNoMsg
	}

	public int m_visibility;

	public ValuesDictionary m_vd;

	public Guid m_playerGuid;

	public string m_playerName;

	public string m_skinName;

	public PlayerClass m_playerClass;

	public DataType m_type;

	public TerrainUpdater.UpdateLocation m_updateLocation;

	public byte ID => 6;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public PlayerDataPackage()
	{
	}

	public PlayerDataPackage(PlayerData playerData, DataType dataType)
	{
		m_vd = new ValuesDictionary();
		m_type = dataType;
		m_playerName = playerData.Name;
		m_skinName = playerData.CharacterSkinName;
		m_playerGuid = playerData.PlayerGUID;
		m_playerClass = playerData.m_playerClass;
		playerData.Save(m_vd);
	}

	public PlayerDataPackage(Guid guid, bool add)
	{
		m_type = (add ? DataType.AddNoMsg : DataType.RemoveNoMsg);
		m_playerGuid = guid;
	}

	public PlayerDataPackage(int time, string msg)
	{
		m_playerName = msg;
		m_type = DataType.CloseTime;
		m_visibility = time;
	}

	public PlayerDataPackage(TerrainUpdater.UpdateLocation updateLocation)
	{
		m_updateLocation = updateLocation;
		m_type = DataType.SetUpdateLocation;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		switch (m_type)
		{
		case DataType.Create:
		case DataType.AddPlayer:
			writer.Write(m_vd);
			break;
		case DataType.Modify:
			writer.Write(m_playerGuid);
			writer.Write(m_playerName);
			writer.Write(m_skinName);
			writer.WriteEnum(m_playerClass);
			break;
		case DataType.Delete:
		case DataType.ClientKnownPlayer:
		case DataType.AddNoMsg:
		case DataType.RemoveNoMsg:
			writer.Write(m_playerGuid);
			break;
		case DataType.SetUpdateLocation:
			writer.Write(m_updateLocation.Center);
			writer.Write((ushort)m_updateLocation.ContentDistance);
			writer.Write((ushort)m_updateLocation.VisibilityDistance);
			writer.Write(m_updateLocation.LastChunksUpdateCenter);
			break;
		case DataType.CloseTime:
			writer.Write(m_visibility);
			writer.Write(m_playerName);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<DataType>();
		switch (m_type)
		{
		case DataType.Create:
			m_vd = reader.ReadValuesDictionary();
			break;
		case DataType.Modify:
			m_playerGuid = reader.ReadGuid();
			m_playerName = reader.ReadString();
			m_skinName = reader.ReadString();
			m_playerClass = reader.ReadEnum<PlayerClass>();
			break;
		case DataType.ClientKnownPlayer:
		case DataType.AddNoMsg:
		case DataType.RemoveNoMsg:
			m_playerGuid = reader.ReadGuid();
			break;
		case DataType.AddPlayer:
			m_vd = reader.ReadPlayerData();
			break;
		case DataType.SetUpdateLocation:
			m_updateLocation = default;
			m_updateLocation.Center = reader.ReadVector2();
			m_updateLocation.ContentDistance = (int)reader.ReadUInt16();
			m_updateLocation.VisibilityDistance = (int)reader.ReadUInt16();
			m_updateLocation.LastChunksUpdateCenter = reader.ReadVector2Nullable();
			break;
		case DataType.CloseTime:
			m_visibility = reader.ReadInt32();
			m_playerName = reader.ReadString();
			break;
		case DataType.Delete:
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (isServer)
		{
			string arg = ((From != null) ? From.ID.ToString() : "NULL");
			if (m_type != DataType.SetUpdateLocation)
			{
				Log.Warning($"[PACKET_HANDLE] Тип: {m_type} | От Клиента: {arg}");
			}
		}
		SubsystemPlayers subsystemPlayers = projectNet.FindSubsystem<SubsystemPlayers>();
		switch (m_type)
		{
		case DataType.Create:
		{
			if (isServer)
			{
				Log.Warning("[CREATE] Обработка пакета Create...");
			}
			if (isServer && From != null && From.PlayerData != null)
			{
				ServerSecurity.Warn(From, "Create while already bound to a player");
				break;
			}
			PlayerData playerData2 = new PlayerData(projectNet);
			playerData2.Load(m_vd);
			PlayerData playerData3 = subsystemPlayers.FindPlayerData((PlayerData p) => p.PlayerGUID == playerData2.PlayerGUID);
			if (isServer)
			{
				ServerSecurity.ApplyServerRecord(playerData2, playerData3);
			}
			if ((playerData3 != null) & isServer)
			{
				Log.Error($"[КРИТИЧЕСКИЙ КОНФЛИКТ] Попытка создать игрока с GUID, который уже есть! Имя: {playerData3.Name}, ClientID: {playerData3.ClientId}");
			}
			subsystemPlayers.AddPlayerData(playerData2);
			if (isServer)
			{
				DebugCheckPlayers(subsystemPlayers, "После Create");
			}
			netNode.QueuePackage(new PlayerDataPackage(playerData2, DataType.AddPlayer));
			break;
		}
		case DataType.Modify:
		{
			if (isServer && (From == null || From.PlayerData == null || (From.PlayerData.PlayerGUID != m_playerGuid && !From.PlayerData.ServerManager)))
			{
				ServerSecurity.Warn(From, "Modify for another player");
				break;
			}
			PlayerData playerData6 = subsystemPlayers.FindPlayerData((PlayerData p) => p.PlayerGUID == m_playerGuid);
			if (playerData6 == null)
			{
				break;
			}
			if (string.Equals(playerData6.Name, m_playerName, StringComparison.OrdinalIgnoreCase))
			{
				playerData6.CharacterSkinName = m_skinName;
				playerData6.m_playerClass = m_playerClass;
				if (isServer)
				{
					netNode.QueuePackage(this);
				}
			}
			else if (subsystemPlayers.AllPlayersData.Any((PlayerData p) => p.PlayerGUID != playerData6.PlayerGUID && string.Equals(p.Name, m_playerName, StringComparison.OrdinalIgnoreCase)))
			{
				if (playerData6.Client != null)
				{
					CommonLib.Net.QueuePackage(new MessagePackage(null, LanguageControl.Get("SubsystemGameWidgets", "name_already_used"), 0, new List<byte> { playerData6.ClientId.Value }));
				}
				Log.Information("Rename denied " + playerData6.Name + " -> " + m_playerName + " (busy)");
			}
			else
			{
				string name = playerData6.Name;
				playerData6.Name = m_playerName;
				playerData6.CharacterSkinName = m_skinName;
				playerData6.m_playerClass = m_playerClass;
				Log.Information("Player renamed " + name + " -> " + m_playerName);
				if (isServer)
				{
					netNode.QueuePackage(this);
				}
			}
			break;
		}
		case DataType.Delete:
			netNode.RemoveClient(From);
			break;
		case DataType.ClientKnownPlayer:
			From.UpdateKnowsPlayer(m_playerGuid);
			break;
		case DataType.AddPlayer:
		{
			if (isServer)
			{
				Log.Warning("[AddPlayer] Обработка пакета AddPlayer...");
			}
			if (isServer && From != null && From.PlayerData != null)
			{
				ServerSecurity.Warn(From, "AddPlayer from a connection that is already bound");
				break;
			}
			PlayerData playerData5 = new PlayerData(projectNet);
			playerData5.Load(m_vd);
			if (isServer)
			{
				PlayerData serverRecord = subsystemPlayers.FindPlayerData((PlayerData p) => p.PlayerGUID == playerData5.PlayerGUID);
				ServerSecurity.ApplyServerRecord(playerData5, serverRecord);
			}
			subsystemPlayers.AddPlayerData(playerData5);
			if (isServer)
			{
				DebugCheckPlayers(subsystemPlayers, "После AddPlayer");
			}
			netNode.QueuePackage(new PlayerDataPackage(playerData5, DataType.ClientKnownPlayer));
			break;
		}
		case DataType.SetUpdateLocation:
		{
			PlayerData playerData = subsystemPlayers.PlayersData.Find((PlayerData x) => x.Client == From);
			if (playerData != null)
			{
				if (m_updateLocation.VisibilityDistance > (float)SettingsManager.VisibilityRange)
				{
					CommonLib.Net.RemoveClient(playerData.Client, "Adjust visibility range to " + SettingsManager.VisibilityRange + " or below");
					break;
				}
				TerrainUpdater terrainUpdater = projectNet.FindSubsystem<SubsystemTerrain>().TerrainUpdater;
				terrainUpdater.SetLastChunksUpdateCenter(playerData.PlayerIndex, m_updateLocation.LastChunksUpdateCenter);
				terrainUpdater.SetUpdateLocation(playerData.PlayerIndex, m_updateLocation.Center, m_updateLocation.VisibilityDistance, m_updateLocation.ContentDistance);
			}
			break;
		}
		case DataType.CloseTime:
		{
			ComponentPlayer mainPlayer = projectNet.FindSubsystem<SubsystemPlayers>().MainPlayer;
			if (mainPlayer != null)
			{
				mainPlayer.ComponentGui.CloseTime = m_visibility;
				DialogsManager.ShowDialog(null, new MessageDialog("Server Down Reminder", m_playerName, LanguageControl.Yes, LanguageControl.No, (MessageDialogButton button) =>
				{
					DialogsManager.HideAllDialogs();
				}));
			}
			break;
		}
		case DataType.AddNoMsg:
			if (isServer && (From == null || From.PlayerData == null || !From.PlayerData.ServerManager))
			{
				ServerSecurity.Warn(From, "AddNoMsg from a non-manager");
			}
			else
			{
				projectNet.FindSubsystem<SubsystemPlayers>().NoMsgPlayerGuidList.Add(m_playerGuid.ToString());
			}
			break;
		case DataType.RemoveNoMsg:
			if (isServer && (From == null || From.PlayerData == null || !From.PlayerData.ServerManager))
			{
				ServerSecurity.Warn(From, "RemoveNoMsg from a non-manager");
			}
			else
			{
				projectNet.FindSubsystem<SubsystemPlayers>().NoMsgPlayerGuidList.Remove(m_playerGuid.ToString());
			}
			break;
		}
	}

	private void DebugCheckPlayers(SubsystemPlayers subsystem, string context)
	{
		Log.Warning($"--- [DEBUG: {context}] Список игроков (Всего: {subsystem.PlayersData.Count}) ---");
		foreach (PlayerData playersDatum in subsystem.PlayersData)
		{
			string text = ((playersDatum.Client != null) ? $"ID {playersDatum.Client.ID}" : "NULL (ПРИЗРАК!)");
			string text2 = ((playersDatum.Client == null) ? " <!!!>" : "");
			Log.Warning($"Игрок: {playersDatum.Name} | GUID: {playersDatum.PlayerGUID} | Client: {text}{text2}");
		}
		Log.Warning("--------------------------------------------------");
	}
}
