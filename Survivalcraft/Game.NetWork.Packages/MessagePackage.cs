using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game.NetWork.Packages;

public class MessagePackage : IPackage
{
	public enum MessageMode : byte
	{
		BaseMessage,
		LargeMessage
	}

	public string m_message;

	public string m_playerName;

	public byte m_messageType;

	public List<byte> m_toClients = new List<byte>();

	public string m_largeText;

	public string m_smallText;

	public float m_duration;

	public float m_delay;

	public MessageMode m_messageMode;

	private static readonly Dictionary<Guid, Guid> s_pendingTeleportRequests;

	private const double TeleportRequestLifetimeSeconds = 60.0;

	private static readonly Dictionary<Guid, DateTime> s_pendingTeleportRequestTimes;

	public byte ID => 7;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public MessagePackage()
	{
	}

	public MessagePackage(string playerName, string Message, byte type, List<byte> toClients)
	{
		m_messageMode = MessageMode.BaseMessage;
		m_message = Message;
		m_playerName = playerName;
		m_messageType = type;
		if (toClients != null)
		{
			m_toClients.AddRange(toClients);
		}
	}

	public MessagePackage(string largeText, string smallText, float duration, float delay)
	{
		m_messageMode = MessageMode.LargeMessage;
		m_largeText = ((largeText == null) ? "" : largeText);
		m_smallText = ((smallText == null) ? "" : smallText);
		m_duration = duration;
		m_delay = delay;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_messageMode);
		switch (m_messageMode)
		{
		case MessageMode.LargeMessage:
			writer.Write(m_largeText);
			writer.Write(m_smallText);
			writer.Write(m_duration);
			writer.Write(m_delay);
			break;
		case MessageMode.BaseMessage:
		{
			writer.Write(m_messageType);
			writer.Write((byte)m_toClients.Count);
			for (byte b = 0; b < m_toClients.Count; b++)
			{
				writer.Write(m_toClients[b]);
			}
			writer.Write(m_message);
			if (m_playerName == null)
			{
				writer.Write(value: false);
				break;
			}
			writer.Write(value: true);
			writer.Write(m_playerName);
			break;
		}
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_messageMode = reader.ReadEnum<MessageMode>();
		switch (m_messageMode)
		{
		case MessageMode.LargeMessage:
			m_largeText = reader.ReadString();
			m_smallText = reader.ReadString();
			m_duration = reader.ReadSingle();
			m_delay = reader.ReadSingle();
			break;
		case MessageMode.BaseMessage:
		{
			m_messageType = reader.ReadByte();
			byte b = reader.ReadByte();
			for (int i = 0; i < b; i++)
			{
				m_toClients.Add(reader.ReadByte());
			}
			m_message = reader.ReadString();
			if (reader.ReadBoolean())
			{
				m_playerName = reader.ReadString();
			}
			break;
		}
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		switch (m_messageMode)
		{
		case MessageMode.LargeMessage:
			if (isServer)
			{
				break;
			}
			{
				foreach (PlayerData playersDatum in projectNet.FindSubsystem<SubsystemPlayers>().PlayersData)
				{
					playersDatum.ComponentPlayer?.ComponentGui?.DisplayLargeMessage(m_largeText, m_smallText, m_duration, m_delay);
				}
				break;
			}
		case MessageMode.BaseMessage:
		{
			int num = (m_message.StartsWith("/") ? 1 : 0);
			if (num == 0)
			{
				projectNet.FindSubsystem<SubsystemGameWidgets>().AddNetMessage(m_message, m_playerName, m_messageType, m_toClients);
			}
			else if (isServer && GameScreen.m_chatWriter != null)
			{
				string text = ((m_playerName != null) ? m_playerName : "Unknown");
				GameScreen.m_chatWriter.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [CMD] [" + text + "]: " + m_message);
				GameScreen.m_chatWriter.Flush();
			}
			_ = From?.PlayerData?.ComponentPlayer;
			if (!isServer)
			{
				break;
			}
			try
			{
				if (m_message.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
				{
					string text2 = "Доступные команды:\n/mychunk - Координаты вашего чанка\n/sethome 1, 2 или 3 - Установить точку дома\n/home 1, 2 или 3 - Телепорт домой\n/homes - Список ваших точек дома\n/delhome 1, 2 или 3 - Удалить точку дома\n/tp <X>,<Y>,<Z> - Телепорт по координатам (включен, цена: 1 уровень на 1000 блоков)\n/tpp <игрок> - Отправить заявку на телепорт к игроку\n/tpa - Принять входящую заявку на телепорт\n/spawn - Телепорт на спавн";
					SendPrivateMessage(From, text2, netNode);
				}
				else if (m_message.StartsWith("/ahelp", StringComparison.OrdinalIgnoreCase))
				{
					if (From.PlayerData.ServerManager)
					{
						string text3 = "Админские команды:\n/ban <игрок> - Забанить\n/unban <игрок> - Разбанить\n/banlist - Список забаненных\n/kick <игрок> - Кикнуть\n/kickall - Кикнуть всех кроме ServerManager\n/mute <игрок> - Выдать мут\n/unmute <игрок> - Снять мут\n/mutelist - Список замученных\n/tpp <игрок> - ТП к игроку (мгновенно, без /tpa)\n/tpto <игрок> - ТП игрока к себе\n/gmc / /gms - Креатив (опасно) / Выживание\n/god <игрок> on/off - Вкл/выкл бессмертие\n/fly - Вкл/выкл полёт + бессмертие себе\n/level <игрок> <число> - Выдать уровень (не сразу обновит)\n/op / /deop <игрок> - СерверМенеджер\n/oplist - Список ServerManager\n/delete fire/pickable - Очистка огня/мусора\n/kill <игрок> - Убить\n/password \"пароль\" - Установить пароль на сервер (clear - убрать)\n/clear <игрок> - Очистка перса в 0\n/weather clear/rain/thunder - Погода\n/h <игрок> - Вылечить\n/v - Ваниш (невидимость)";
						SendPrivateMessage(From, text3, netNode);
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/ban "))
				{
					if (From.PlayerData.ServerManager)
					{
						string targetName2 = m_message.Substring(5).Trim();
						SubsystemPlayers subsystemPlayers = projectNet.FindSubsystem<SubsystemPlayers>();
						PlayerData playerData = subsystemPlayers.FindPlayerData((PlayerData p) => p.Name.Equals(targetName2, StringComparison.OrdinalIgnoreCase));
						if (playerData != null)
						{
							subsystemPlayers.AddBlackList(playerData);
							SendPrivateMessage(From, playerData.Name + " забанен.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "Игрок не найден.", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/banlist", StringComparison.OrdinalIgnoreCase))
				{
					if (From.PlayerData.ServerManager)
					{
						SubsystemPlayers subsystemPlayers2 = projectNet.FindSubsystem<SubsystemPlayers>();
						if (subsystemPlayers2.BlackPlayerNames == null || subsystemPlayers2.BlackPlayerNames.Count == 0)
						{
							SendPrivateMessage(From, "Список забаненных пуст.", netNode);
						}
						else
						{
							List<string> list = (from n in subsystemPlayers2.BlackPlayerNames.Values
								where !string.IsNullOrEmpty(n)
								select n.Split('|')[0]).OrderBy((string n) => n, StringComparer.OrdinalIgnoreCase).ToList();
							string text4 = ((list.Count > 0) ? string.Join("\n", list) : ("(имена неизвестны, " + subsystemPlayers2.BlackPlayerNames.Count + " записей по GUID)"));
							SendPrivateMessage(From, "Забанены (" + subsystemPlayers2.BlackPlayerGuidList.Count + "):\n" + text4, netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/mutelist", StringComparison.OrdinalIgnoreCase))
				{
					if (From.PlayerData.ServerManager)
					{
						SubsystemPlayers subsystemPlayers3 = projectNet.FindSubsystem<SubsystemPlayers>();
						if (subsystemPlayers3.NoMsgPlayerNames == null || subsystemPlayers3.NoMsgPlayerNames.Count == 0)
						{
							SendPrivateMessage(From, "Список замученных пуст.", netNode);
						}
						else
						{
							List<string> list2 = subsystemPlayers3.NoMsgPlayerNames.Values.Where((string n) => !string.IsNullOrEmpty(n)).OrderBy((string n) => n, StringComparer.OrdinalIgnoreCase).ToList();
							string text5 = ((list2.Count > 0) ? string.Join("\n", list2) : ("(имена неизвестны, " + subsystemPlayers3.NoMsgPlayerGuidList.Count + " записей по GUID)"));
							SendPrivateMessage(From, "В муте (" + subsystemPlayers3.NoMsgPlayerGuidList.Count + "):\n" + text5, netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/unban "))
				{
					if (From.PlayerData.ServerManager)
					{
						string targetNameUnban = m_message.Substring(7).Trim();
						SubsystemPlayers subsystemPlayers4 = projectNet.FindSubsystem<SubsystemPlayers>();
						PlayerData playerData2 = subsystemPlayers4.FindPlayerData((PlayerData p) => p.Name.Equals(targetNameUnban, StringComparison.OrdinalIgnoreCase));
						if (playerData2 != null)
						{
							subsystemPlayers4.RemoveBlackList(playerData2);
							SendPrivateMessage(From, playerData2.Name + " разбанен.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "Игрок не найден.", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/mute "))
				{
					if (From.PlayerData.ServerManager)
					{
						string targetName3 = m_message.Substring(6).Trim();
						SubsystemPlayers subsystemPlayers5 = projectNet.FindSubsystem<SubsystemPlayers>();
						PlayerData playerData3 = subsystemPlayers5.FindPlayerData((PlayerData p) => p.Name.Equals(targetName3, StringComparison.OrdinalIgnoreCase));
						if (playerData3 != null)
						{
							subsystemPlayers5.AddNoMsgList(playerData3);
							SendPrivateMessage(From, playerData3.Name + " получил мут.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "Игрок не найден.", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/unmute "))
				{
					if (From.PlayerData.ServerManager)
					{
						string targetNameUnmute = m_message.Substring(8).Trim();
						SubsystemPlayers subsystemPlayers6 = projectNet.FindSubsystem<SubsystemPlayers>();
						PlayerData playerData4 = subsystemPlayers6.FindPlayerData((PlayerData p) => p.Name.Equals(targetNameUnmute, StringComparison.OrdinalIgnoreCase));
						if (playerData4 != null)
						{
							subsystemPlayers6.RemoveNoMsgList(playerData4);
							SendPrivateMessage(From, playerData4.Name + " больше не в муте.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "Игрок не найден.", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/kick "))
				{
					if (From.PlayerData.ServerManager)
					{
						string targetName4 = m_message.Substring(6).Trim();
						PlayerData playerData5 = projectNet.FindSubsystem<SubsystemPlayers>().FindPlayerData((PlayerData p) => p.Name.Equals(targetName4, StringComparison.OrdinalIgnoreCase));
						if (playerData5 != null && playerData5.Client != null)
						{
							CommonLib.Net.RemoveClientImmediate(playerData5.Client, "You have been kicked by an admin.");
							SendPrivateMessage(From, playerData5.Name + " был кикнут.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "Игрок не найден или не в сети.", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.StartsWith("/kickall"))
				{
					if (From.PlayerData.ServerManager)
					{
						List<PlayerData> list3 = projectNet.FindSubsystem<SubsystemPlayers>().PlayersData.Where((PlayerData p) => p.Client != null && p.PlayerGUID != From.PlayerData.PlayerGUID && !p.ServerManager && !p.IsServerAdmin).ToList();
						if (list3.Count == 0)
						{
							SendPrivateMessage(From, "Некого кикать.", netNode);
						}
						else
						{
							foreach (PlayerData item in list3)
							{
								CommonLib.Net.RemoveClientImmediate(item.Client, "You have been kicked by an admin.");
							}
							SendPrivateMessage(From, "Кикнуто игроков: " + list3.Count + ".", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.Trim().StartsWith("/homes", StringComparison.OrdinalIgnoreCase))
				{
					Dictionary<int, Vector3> homePoints = From.PlayerData.HomePoints;
					if (homePoints == null || homePoints.Count == 0)
					{
						SendPrivateMessage(From, "У вас нет точек дома.", netNode);
					}
					else
					{
						List<string> list4 = new List<string>();
						foreach (KeyValuePair<int, Vector3> item2 in homePoints.OrderBy((KeyValuePair<int, Vector3> p) =>
						{
							KeyValuePair<int, Vector3> keyValuePair = p;
							return keyValuePair.Key;
						}))
						{
							object[] array = new object[8] { "Home ", item2.Key, ": ", null, null, null, null, null };
							int num2 = 3;
							array[num2] = item2.Value.X.ToString("F1");
							array[4] = ", ";
							int num3 = 5;
							array[num3] = item2.Value.Y.ToString("F1");
							array[6] = ", ";
							int num4 = 7;
							array[num4] = item2.Value.Z.ToString("F1");
							list4.Add(string.Concat(array));
						}
						SendPrivateMessage(From, "Ваши точки дома:\n" + string.Join("\n", list4), netNode);
					}
				}
				else if (m_message.StartsWith("/delhome "))
				{
					if (!int.TryParse(m_message.Substring(9).Trim(), out var result) || result < 1 || result > 3)
					{
						SendPrivateMessage(From, "Использование: /delhome 1, 2 или 3", netNode);
					}
					else if (From.PlayerData.HomePoints == null || !From.PlayerData.HomePoints.ContainsKey(result))
					{
						SendPrivateMessage(From, "Точка дома " + result + " не установлена.", netNode);
					}
					else
					{
						From.PlayerData.HomePoints.Remove(result);
						SendPrivateMessage(From, "Точка дома " + result + " удалена.", netNode);
					}
				}
				else if (m_message.Trim().StartsWith("/home", StringComparison.OrdinalIgnoreCase))
				{
					string text6 = ((m_message.Length > 5) ? m_message.Substring(5).Trim() : "");
					Dictionary<int, Vector3> homePoints2 = From.PlayerData.HomePoints;
					int result2;
					if (homePoints2 == null || homePoints2.Count == 0)
					{
						SendPrivateMessage(From, "У вас не установлены точки дома.", netNode);
					}
					else if (string.IsNullOrEmpty(text6))
					{
						if (homePoints2.Count == 1)
						{
							int num5 = homePoints2.Keys.First();
							Vector3 targetPosition = homePoints2[num5];
							TeleportToHome(From, targetPosition, num5, netNode);
						}
						else
						{
							SendPrivateMessage(From, "У вас несколько точек дома. Используйте /home 1, 2 или 3", netNode);
						}
					}
					else if (!int.TryParse(text6, out result2) || result2 < 1 || result2 > 3)
					{
						SendPrivateMessage(From, "Использование: /home 1, 2 или 3", netNode);
					}
					else if (!homePoints2.ContainsKey(result2))
					{
						SendPrivateMessage(From, "Точка дома " + result2 + " не установлена.", netNode);
					}
					else
					{
						Vector3 targetPosition2 = homePoints2[result2];
						TeleportToHome(From, targetPosition2, result2, netNode);
					}
				}
				else if (m_message.StartsWith("/sethome "))
				{
					if (!int.TryParse(m_message.Substring(9).Trim(), out var result3) || result3 < 1 || result3 > 3)
					{
						SendPrivateMessage(From, "Использование: /sethome 1, 2 или 3", netNode);
					}
					else
					{
						if (From.PlayerData.HomePoints == null)
						{
							From.PlayerData.HomePoints = new Dictionary<int, Vector3>();
						}
						ComponentPlayer componentPlayer = From.PlayerData.ComponentPlayer;
						Point3 point = Terrain.ToCell(componentPlayer.ComponentBody.Position);
						if (!SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(point.X, point.Z, out var territoriy))
						{
							SendPrivateMessage(From, "Для установки дома вы должны быть на территории.", netNode);
						}
						else
						{
							Guid playerGUID = From.PlayerData.PlayerGUID;
							bool flag = territoriy.OwnerGuid == playerGUID;
							bool flag2 = From.PlayerData.Friends.Contains(territoriy.OwnerGuid) && territoriy.ApplyToFirend;
							if (!flag && !flag2)
							{
								SendPrivateMessage(From, "Вы должны быть на своей территории или территории друга.", netNode);
							}
							else
							{
								From.PlayerData.HomePoints[result3] = componentPlayer.ComponentBody.Position;
								SendPrivateMessage(From, "Точка дома " + result3 + " установлена.", netNode);
							}
						}
					}
				}
				else if (m_message.StartsWith("/tpp "))
				{
					string rawName2 = m_message.Substring(5).Trim();
					List<ComponentPlayer> list5 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name.Trim(), rawName2, StringComparison.OrdinalIgnoreCase)).ToList();
					if (list5.Count == 0)
					{
						SendPrivateMessage(From, "Игрок не найден.", netNode);
					}
					else
					{
						ComponentPlayer componentPlayer2 = list5.OrderBy((ComponentPlayer p) => Vector3.DistanceSquared(From.PlayerData.ComponentPlayer.ComponentBody.Position, p.ComponentBody.Position)).First();
						if (From.PlayerData.ServerManager)
						{
							Point3 point2 = Terrain.ToCell(componentPlayer2.ComponentBody.Position);
							if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(point2.X, point2.Z, out var territoriy2))
							{
								Guid ownerGuid = territoriy2.OwnerGuid;
								bool flag3 = From.PlayerData.Friends.Contains(ownerGuid);
								if (!From.PlayerData.ServerManager && !(From.PlayerData.PlayerGUID == ownerGuid) && (!territoriy2.ApplyToFirend || !flag3))
								{
									SendPrivateMessage(From, "У вас нет прав для телепорта в этот регион.", netNode);
									break;
								}
							}
							From.PlayerData.ComponentPlayer.ComponentBody.Position = componentPlayer2.ComponentBody.Position;
							From.PlayerData.ComponentPlayer.SecurityGrace(3.0);
							From.PlayerData.ComponentPlayer.ComponentBody.Velocity = Vector3.Zero;
							netNode.QueuePackage(new ComponentPlayerPackage(From.PlayerData, 0, playSound: true, From.PlayerData.Level));
							netNode.QueuePackage(new ComponentPlayerPackage(From.PlayerData.ComponentPlayer, ComponentPlayerPackage.PlayerAction.PositionSet));
							Log.Information("Teleported " + m_playerName + " to " + componentPlayer2.PlayerData.Name);
						}
						else
						{
							if (componentPlayer2.PlayerData.PlayerGUID == From.PlayerData.PlayerGUID)
							{
								SendPrivateMessage(From, "Нельзя телепортироваться к самому себе.", netNode);
								break;
							}
							Guid targetGuid = componentPlayer2.PlayerData.PlayerGUID;
							s_pendingTeleportRequests[targetGuid] = From.PlayerData.PlayerGUID;
							s_pendingTeleportRequestTimes[targetGuid] = DateTime.UtcNow;
							SendPrivateMessage(From, "Заявка на телепорт к " + componentPlayer2.PlayerData.Name + " отправлена. Ожидайте подтверждения (/tpa).", netNode);
							IClient client = netNode.Peers.FirstOrDefault((IClient c) => c.PlayerData != null && c.PlayerData.PlayerGUID == targetGuid);
							if (client != null)
							{
								SendPrivateMessage(client, From.PlayerData.Name + " хочет телепортироваться к вам. Введите /tpa, чтобы принять (заявка активна " + 60 + " сек.).", netNode);
							}
						}
					}
				}
				else if (m_message.StartsWith("/tpa", StringComparison.OrdinalIgnoreCase))
				{
					Guid playerGUID2 = From.PlayerData.PlayerGUID;
					if (!s_pendingTeleportRequests.TryGetValue(playerGUID2, out var requesterGuid))
					{
						SendPrivateMessage(From, "У вас нет активных заявок на телепорт.", netNode);
					}
					else
					{
						bool num6 = s_pendingTeleportRequestTimes.TryGetValue(playerGUID2, out var value) && (DateTime.UtcNow - value).TotalSeconds > 60.0;
						s_pendingTeleportRequests.Remove(playerGUID2);
						s_pendingTeleportRequestTimes.Remove(playerGUID2);
						if (num6)
						{
							SendPrivateMessage(From, "Заявка на телепорт истекла.", netNode);
						}
						else
						{
							ComponentPlayer componentPlayer3 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.FirstOrDefault((ComponentPlayer p) => p.PlayerData.PlayerGUID == requesterGuid);
							IClient client2 = netNode.Peers.FirstOrDefault((IClient c) => c.PlayerData != null && c.PlayerData.PlayerGUID == requesterGuid);
							if (componentPlayer3 == null || client2 == null)
							{
								SendPrivateMessage(From, "Игрок, отправивший заявку, больше не в сети.", netNode);
							}
							else
							{
								componentPlayer3.ComponentBody.Position = From.PlayerData.ComponentPlayer.ComponentBody.Position;
								componentPlayer3.SecurityGrace(3.0);
								componentPlayer3.ComponentBody.Velocity = Vector3.Zero;
								netNode.QueuePackage(new ComponentPlayerPackage(client2.PlayerData, 0, playSound: true, client2.PlayerData.Level));
								netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer3, ComponentPlayerPackage.PlayerAction.PositionSet));
								Log.Information("Teleported " + client2.PlayerData.Name + " to " + From.PlayerData.Name + " (via /tpa)");
								SendPrivateMessage(client2, "Заявка принята. Вы телепортированы к " + From.PlayerData.Name + ".", netNode);
								SendPrivateMessage(From, "Вы приняли заявку от " + client2.PlayerData.Name + ".", netNode);
							}
						}
					}
				}
				else if (m_message.StartsWith("/spawn", StringComparison.OrdinalIgnoreCase))
				{
					From.PlayerData.ComponentPlayer.ComponentBody.Position = projectNet.FindSubsystem<SubsystemPlayers>().GlobalSpawnPosition;
					From.PlayerData.ComponentPlayer.SecurityGrace(3.0);
					From.PlayerData.ComponentPlayer.ComponentBody.Velocity = Vector3.Zero;
					netNode.QueuePackage(new ComponentPlayerPackage(From.PlayerData, 0, playSound: true, From.PlayerData.Level));
					netNode.QueuePackage(new ComponentPlayerPackage(From.PlayerData.ComponentPlayer, ComponentPlayerPackage.PlayerAction.PositionSet));
					Log.Information("Teleported " + m_playerName + " to spawn");
					SendPrivateMessage(From, "Вы телепортированы на спавн.", netNode);
				}
				else if (m_message.StartsWith("/tpto "))
				{
					if (From.PlayerData.ServerManager)
					{
						string rawName3 = m_message.Substring(6).Trim();
						List<ComponentPlayer> list6 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name.Trim(), rawName3, StringComparison.OrdinalIgnoreCase)).ToList();
						if (list6.Count == 0)
						{
							SendPrivateMessage(From, "Игрок не найден.", netNode);
						}
						else
						{
							ComponentPlayer componentPlayer4 = list6.OrderBy((ComponentPlayer p) => Vector3.DistanceSquared(From.PlayerData.ComponentPlayer.ComponentBody.Position, p.ComponentBody.Position)).First();
							componentPlayer4.ComponentBody.Position = From.PlayerData.ComponentPlayer.ComponentBody.Position;
							componentPlayer4.SecurityGrace(3.0);
							componentPlayer4.ComponentBody.Velocity = Vector3.Zero;
							netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer4.PlayerData, 0, playSound: true, componentPlayer4.PlayerData.Level));
							netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer4, ComponentPlayerPackage.PlayerAction.PositionSet));
							Log.Information("Teleported " + componentPlayer4.PlayerData.Name + " to " + m_playerName);
							SendPrivateMessage(From, "Teleported " + componentPlayer4.PlayerData.Name + " to you.", netNode);
						}
					}
					else
					{
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
					}
				}
				else if (m_message.Contains("/mychunk"))
				{
					TerrainRaycastResult? terrainRaycastResult = GameManager.Project.FindSubsystem<SubsystemTerrain>().Raycast(From.PlayerData.ComponentPlayer.ComponentBody.Position, Vector3.Zero, useInteractionBoxes: false, skipAirBlocks: false, null);
					if (terrainRaycastResult.HasValue)
					{
						Point3 point3 = terrainRaycastResult.Value.CellFace.Point;
						SendPrivateMessage(From, "XZ: " + GameManager.Project.FindSubsystem<SubsystemTerrain>().Terrain.GetChunkAtCell(point3.X, point3.Z).Coords.ToString(), netNode);
					}
				}
				if (m_message.StartsWith("/tp "))
				{
					string[] array2 = m_message.Substring(4).Trim().Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length == 3)
					{
						float num7 = float.Parse(array2[0]);
						float num8 = float.Parse(array2[1]);
						float num9 = float.Parse(array2[2]);
						if ((Math.Abs(num7) > 15000f || Math.Abs(num8) > 15000f || Math.Abs(num9) > 15000f) && !From.PlayerData.ServerManager)
						{
							SendPrivateMessage(From, "Слишком далеко для телепорта", netNode);
						}
						else
						{
							ComponentPlayer componentPlayer5 = From.PlayerData.ComponentPlayer;
							Vector3 vector = new Vector3(num7, num8, num9);
							float num10 = Vector3.Distance(componentPlayer5.ComponentBody.Position, vector);
							float num11 = num10 / 1000f;
							float level = From.PlayerData.Level;
							if (level < num11)
							{
								SendPrivateMessage(From, $"Недостаточно уровней! Расстояние: {num10:F1} блоков. Нужно: {num11:F2}, у вас: {level:F2}.", netNode);
							}
							else
							{
								From.PlayerData.Level = level - num11;
								componentPlayer5.ComponentBody.Position = vector;
								componentPlayer5.SecurityGrace(3.0);
								componentPlayer5.ComponentBody.Velocity = Vector3.Zero;
								netNode.QueuePackage(new ComponentPlayerPackage(From.PlayerData, 0, playSound: true, From.PlayerData.Level));
								netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer5, ComponentPlayerPackage.PlayerAction.PositionSet));
								SendPrivateMessage(From, $"Телепортация выполнена! Расстояние: {num10:F1} блоков. Списано: {num11:F2} ур. Осталось: {From.PlayerData.Level:F2}.", netNode);
								Log.Information("Teleported " + m_playerName + " to coordinates: " + string.Join(",", array2) + ". Distance: " + num10.ToString("F1") + ". Cost: " + num11.ToString("F2"));
							}
						}
					}
					else
					{
						SendPrivateMessage(From, "Неверный формат команды телепорта.", netNode);
					}
				}
				else
				{
					if (m_message.Contains("/gmc"))
					{
						if (From.PlayerData.ServerManager)
						{
							try
							{
								ChangeGameMode(GameMode.Creative);
								SendPrivateMessage(From, "Режим игры изменён на креатив.", netNode);
								break;
							}
							catch (Exception ex)
							{
								Log.Error(ex.Message);
								break;
							}
						}
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						break;
					}
					if (m_message.Contains("/gms"))
					{
						if (From.PlayerData.ServerManager)
						{
							try
							{
								ChangeGameMode(GameMode.Challenging);
								SendPrivateMessage(From, "Режим игры изменён на выживание.", netNode);
								break;
							}
							catch (Exception ex2)
							{
								Log.Error(ex2.Message);
								break;
							}
						}
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						break;
					}
					if (m_message.StartsWith("/god "))
					{
						if (From.PlayerData.ServerManager)
						{
							string text7 = m_message.Substring(5).Trim();
							int num12 = text7.LastIndexOf(' ');
							if (num12 > 0)
							{
								string targetPlayerName2 = text7.Substring(0, num12).Trim();
								string text8 = text7.Substring(num12 + 1).Trim().ToLower();
								List<ComponentPlayer> list7 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name, targetPlayerName2, StringComparison.OrdinalIgnoreCase)).ToList();
								if (list7.Count == 0)
								{
									SendPrivateMessage(From, "Игрок не найден.", netNode);
								}
								else
								{
									ComponentPlayer componentPlayer6 = list7.First();
									ComponentHealth componentHealth = componentPlayer6.Entity.FindComponent<ComponentHealth>();
									ComponentVitalStats componentVitalStats = componentPlayer6.Entity.FindComponent<ComponentVitalStats>();
									ComponentFlu componentFlu = componentPlayer6.Entity.FindComponent<ComponentFlu>();
									ComponentSickness componentSickness = componentPlayer6.Entity.FindComponent<ComponentSickness>();
									ComponentOnFire componentOnFire = componentPlayer6.Entity.FindComponent<ComponentOnFire>();
									if (text8 == "on")
									{
										if (componentHealth != null)
										{
											componentHealth.IsInvulnerable = true;
											componentHealth.Heal(1f);
											componentHealth.Air = 1f;
											CommonLib.Net.QueuePackage(new ComponentHealthPackage(componentHealth));
										}
										if (componentVitalStats != null)
										{
											componentVitalStats.Food = 1f;
											componentVitalStats.Stamina = float.PositiveInfinity;
											componentVitalStats.Sleep = 1f;
											componentVitalStats.Temperature = 12f;
											componentVitalStats.Wetness = 0f;
										}
										if (componentFlu != null)
										{
											componentFlu.m_fluDuration = 0f;
										}
										if (componentSickness != null)
										{
											componentSickness.m_sicknessDuration = 0f;
											componentSickness.m_pukeParticleSystem = null;
										}
										componentOnFire?.Extinguish();
										SendPrivateMessage(From, "Режим бога включён для " + targetPlayerName2, netNode);
									}
									else if (text8 == "off")
									{
										if (componentHealth != null)
										{
											componentHealth.IsInvulnerable = false;
											CommonLib.Net.QueuePackage(new ComponentHealthPackage(componentHealth));
										}
										if (componentVitalStats != null)
										{
											componentVitalStats.Food = 0.5f;
											componentVitalStats.Stamina = componentPlayer6.PlayerData.Level;
										}
										SendPrivateMessage(From, "Режим бога выключен для " + targetPlayerName2, netNode);
									}
									else
									{
										SendPrivateMessage(From, "Неверное состояние (используйте on или off).", netNode);
									}
								}
							}
							else
							{
								SendPrivateMessage(From, "Неверный формат команды.", netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.Trim().Equals("/fly", StringComparison.OrdinalIgnoreCase))
					{
						if (From.PlayerData.ServerManager)
						{
							ComponentPlayer componentPlayer7 = From.PlayerData.ComponentPlayer;
							bool flag4 = !componentPlayer7.ComponentLocomotion.IsCreativeFlyEnabled;
							componentPlayer7.ComponentLocomotion.IsCreativeFlyEnabled = flag4;
							CommonLib.Net.QueuePackage(new ComponentPlayerPackage(componentPlayer7, ComponentPlayerPackage.PlayerAction.CreativeFlyChange));
							ComponentHealth componentHealth2 = componentPlayer7.Entity.FindComponent<ComponentHealth>();
							if (componentHealth2 != null)
							{
								componentHealth2.IsInvulnerable = flag4;
								if (flag4)
								{
									componentHealth2.Heal(1f);
									componentHealth2.Air = 1f;
								}
								CommonLib.Net.QueuePackage(new ComponentHealthPackage(componentHealth2));
							}
							SendPrivateMessage(From, "Полёт " + (flag4 ? "включён (+ бессмертие)" : "выключен (+ бессмертие снято)") + ".", netNode);
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.StartsWith("/level "))
					{
						if (From.PlayerData.ServerManager)
						{
							string text9 = m_message.Substring(7).Trim();
							int num13 = text9.LastIndexOf(' ');
							if (num13 > 0)
							{
								string targetPlayerName3 = text9.Substring(0, num13).Trim();
								if (!float.TryParse(text9.Substring(num13 + 1).Trim(), out var result4) || result4 < 0f || result4 > 10000.1f)
								{
									SendPrivateMessage(From, "Неверный формат уровня.", netNode);
									break;
								}
								List<ComponentPlayer> list8 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name, targetPlayerName3, StringComparison.OrdinalIgnoreCase)).ToList();
								if (list8.Count == 0)
								{
									SendPrivateMessage(From, "Игрок не найден.", netNode);
								}
								else
								{
									list8.First().PlayerData.Level = result4;
									SendPrivateMessage(From, "Level set to " + result4.ToString("0.00") + " for " + targetPlayerName3, netNode);
									Log.Information("Level set to " + result4.ToString("0.00") + " for " + targetPlayerName3);
								}
							}
							else
							{
								SendPrivateMessage(From, "Неверный формат команды.", netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.StartsWith("/op "))
					{
						string targetName5 = m_message.Substring(4).Trim();
						if (From.PlayerData.IsServerAdmin)
						{
							List<ComponentPlayer> list9 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => p.PlayerData.Name.Equals(targetName5, StringComparison.OrdinalIgnoreCase)).ToList();
							if (list9.Count == 0)
							{
								SendPrivateMessage(From, "Игрок не найден.", netNode);
							}
							else
							{
								ComponentPlayer componentPlayer8 = list9.First();
								componentPlayer8.PlayerData.ServerManager = true;
								SendPrivateMessage(From, componentPlayer8.PlayerData.Name + " is now a Server Manager.", netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "Только создатель сервера может использовать эту команду.", netNode);
						}
					}
					else if (m_message.StartsWith("/deop "))
					{
						string targetName6 = m_message.Substring(6).Trim();
						if (From.PlayerData.IsServerAdmin)
						{
							List<ComponentPlayer> list10 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => p.PlayerData.Name.Equals(targetName6, StringComparison.OrdinalIgnoreCase)).ToList();
							if (list10.Count == 0)
							{
								SendPrivateMessage(From, "Игрок не найден.", netNode);
							}
							else
							{
								ComponentPlayer componentPlayer9 = list10.First();
								componentPlayer9.PlayerData.ServerManager = false;
								SendPrivateMessage(From, componentPlayer9.PlayerData.Name + " is no longer a Server Manager.", netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "Только создатель сервера может использовать эту команду.", netNode);
						}
					}
					else if (m_message.StartsWith("/oplist", StringComparison.OrdinalIgnoreCase))
					{
						if (From.PlayerData.ServerManager)
						{
							List<string> list11 = (from p in projectNet.FindSubsystem<SubsystemPlayers>().AllPlayersData
								where p.ServerManager || p.IsServerAdmin
								orderby p.Name.ToLower()
								select p.Name + (p.IsServerAdmin ? " [Admin]" : " [ServerManager]")).ToList();
							if (list11.Count == 0)
							{
								SendPrivateMessage(From, "Список пуст.", netNode);
							}
							else
							{
								SendPrivateMessage(From, "С правами (" + list11.Count + "):\n" + string.Join("\n", list11), netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.Trim().Equals("/delete fire", StringComparison.OrdinalIgnoreCase))
					{
						if (From.PlayerData.ServerManager)
						{
							int num14 = GameManager.Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true).ClearAllFires();
							SendPrivateMessage(From, "Removed " + num14 + " fire sources.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.Trim().Equals("/delete pickable", StringComparison.OrdinalIgnoreCase))
					{
						if (From.PlayerData.ServerManager)
						{
							int num15 = GameManager.Project.FindSubsystem<SubsystemPickables>(throwOnError: true).ClearLoosePickables();
							SendPrivateMessage(From, "Removed " + num15 + " pickables.", netNode);
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.StartsWith("/kill "))
					{
						if (From.PlayerData.ServerManager)
						{
							string targetPlayerName4 = m_message.Substring(6).Trim();
							List<ComponentPlayer> list12 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name, targetPlayerName4, StringComparison.OrdinalIgnoreCase)).ToList();
							if (list12.Count == 0)
							{
								SendPrivateMessage(From, "Игрок не найден.", netNode);
							}
							else if (list12.Count > 1)
							{
								SendPrivateMessage(From, "Уточните ник, найдено несколько совпадений.", netNode);
							}
							else
							{
								ComponentPlayer componentPlayer10 = list12.First();
								if (componentPlayer10.PlayerData.ServerManager)
								{
									SendPrivateMessage(From, "Нельзя убить администратора сервера.", netNode);
								}
								else
								{
									componentPlayer10.Entity.FindComponent<ComponentHealth>().Injure(float.MaxValue, null, ignoreInvulnerability: true, "Admin kill command");
									SendPrivateMessage(From, componentPlayer10.PlayerData.Name + " has been killed.", netNode);
									Log.Information("Killed " + componentPlayer10.PlayerData.Name);
								}
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					if (m_message.StartsWith("/password "))
					{
						if (From.PlayerData.ServerManager)
						{
							try
							{
								string[] array3 = m_message.Substring(10).Trim().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
								if (array3.Length == 2 && array3[0] == array3[1])
								{
									string text10 = array3[0].Trim();
									if (string.Equals(text10, "clear", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(text10))
									{
										ChangeWorldPassword("");
										SendPrivateMessage(From, "World password cleared", netNode);
									}
									else
									{
										ChangeWorldPassword(text10);
										SendPrivateMessage(From, "World password changed", netNode);
									}
								}
								else
								{
									SendPrivateMessage(From, "Пароли не совпадают!", netNode);
								}
								break;
							}
							catch (Exception ex3)
							{
								Log.Error(ex3.Message);
								break;
							}
						}
						SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						break;
					}
					if (m_message.StartsWith("/clear "))
					{
						if (From.PlayerData.ServerManager)
						{
							string targetPlayerName5 = m_message.Substring(7).Trim();
							List<ComponentPlayer> list13 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name, targetPlayerName5, StringComparison.OrdinalIgnoreCase)).ToList();
							if (list13.Count == 0)
							{
								SendPrivateMessage(From, "Игрок не найден.", netNode);
							}
							else if (list13.Count > 1)
							{
								SendPrivateMessage(From, "Уточните ник, найдено несколько совпадений.", netNode);
							}
							else
							{
								ComponentPlayer componentPlayer11 = list13.First();
								if (componentPlayer11.PlayerData.ServerManager)
								{
									SendPrivateMessage(From, "Нельзя очистить инвентарь администратора сервера.", netNode);
								}
								else
								{
									ComponentInventoryBase componentInventoryBase = componentPlayer11.Entity.FindComponent<ComponentInventoryBase>();
									if (componentInventoryBase != null)
									{
										for (int num16 = 0; num16 < componentInventoryBase.SlotsCount; num16++)
										{
											componentInventoryBase.RemoveSlotItems(num16, componentInventoryBase.GetSlotCount(num16));
										}
									}
									componentPlayer11.PlayerData.Level = 0f;
									componentPlayer11.Entity.FindComponent<ComponentHealth>().Health = float.MaxValue;
									componentPlayer11.ComponentBody.Position = new Vector3(0f, 2f, 0f);
									componentPlayer11.SecurityGrace(3.0);
									componentPlayer11.ComponentBody.Velocity = Vector3.Zero;
									netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer11.PlayerData, 0, playSound: true, componentPlayer11.PlayerData.Level));
									netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer11, ComponentPlayerPackage.PlayerAction.PositionSet));
									SendPrivateMessage(From, componentPlayer11.PlayerData.Name + " has been cleared and moved to hub.", netNode);
									Log.Information("Cleared and moved " + componentPlayer11.PlayerData.Name);
								}
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.StartsWith("/weather "))
					{
						if (From.PlayerData.ServerManager)
						{
							string[] array4 = m_message.Substring(9).Trim().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
							if (array4.Length != 0)
							{
								string text11 = array4[0].ToLower();
								SubsystemWeather subsystemWeather = GameManager.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
								SubsystemGameInfo subsystemGameInfo = GameManager.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
								Random random = new Random();
								double totalElapsedGameTime = subsystemGameInfo.TotalElapsedGameTime;
								double num17;
								double num18;
								float num19;
								string text12;
								switch (text11)
								{
								case "clear":
								{
									double num20 = 60.0 * (double)random.Float(5f, 45f);
									num17 = totalElapsedGameTime + num20;
									num18 = num17 + 60.0 * (double)random.Float(3f, 6f);
									num19 = 0f;
									text12 = "Weather changed to: Clear (next rain in " + (int)(num20 / 60.0) + " min)";
									break;
								}
								case "rain":
									num17 = totalElapsedGameTime;
									num18 = totalElapsedGameTime + 60.0 * (double)random.Float(3f, 6f);
									num19 = 0.1f;
									text12 = "Weather changed to: Rain";
									break;
								default:
									SendPrivateMessage(From, "Использование: /weather clear/rain/thunder", netNode);
									return;
								case "thunder":
									num17 = totalElapsedGameTime;
									num18 = totalElapsedGameTime + 60.0 * (double)random.Float(3f, 6f);
									num19 = 1f;
									text12 = "Weather changed to: Thunderstorm";
									break;
								}
								subsystemWeather.m_precipitationStartTime = num17;
								subsystemWeather.m_precipitationEndTime = num18;
								subsystemWeather.m_lightningIntensity = num19;
								CommonLib.Net.QueuePackage(new SubsystemWeatherPackage(num17, num18, num19, num19));
								SendPrivateMessage(From, text12, netNode);
							}
							else
							{
								SendPrivateMessage(From, "Использование: /weather clear/rain/thunder", netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.StartsWith("/wind "))
					{
						if (From.PlayerData.ServerManager)
						{
							string[] array5 = m_message.Substring(6).Trim().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
							if (array5.Length != 0)
							{
								string text13 = array5[0].ToLower();
								SubsystemWeather subsystemWeather2 = GameManager.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
								SubsystemGameInfo subsystemGameInfo2 = GameManager.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
								Random random2 = new Random();
								double totalElapsedGameTime2 = subsystemGameInfo2.TotalElapsedGameTime;
								double windStartTime = totalElapsedGameTime2;
								double windEndTime = totalElapsedGameTime2 + 60.0 * (double)random2.Float(3f, 6f);
								float targetWindStrength;
								string text14;
								switch (text13)
								{
								case "off":
								case "выкл":
								case "выключить":
									windEndTime = totalElapsedGameTime2;
									targetWindStrength = 0f;
									text14 = "Wind changed to: Off";
									break;
								case "weak":
								case "слабый":
									targetWindStrength = 1f;
									text14 = "Wind changed to: Weak (1)";
									break;
								case "medium":
								case "средний":
									targetWindStrength = 6f;
									text14 = "Wind changed to: Medium (6)";
									break;
								case "strong":
								case "сильный":
									targetWindStrength = 9f;
									text14 = "Wind changed to: Strong (9)";
									break;
								default:
									SendPrivateMessage(From, "Использование: /wind off/weak/medium/strong/storm", netNode);
									return;
								case "storm":
								case "шторм":
									targetWindStrength = 12f;
									text14 = "Wind changed to: Storm (12)";
									break;
								}
								subsystemWeather2.WindStartTime = windStartTime;
								subsystemWeather2.WindEndTime = windEndTime;
								subsystemWeather2.TargetWindStrength = targetWindStrength;
								CommonLib.Net.QueuePackage(new SubsystemWeatherPackage(subsystemWeather2.m_precipitationStartTime, subsystemWeather2.m_precipitationEndTime, subsystemWeather2.m_lightningIntensity, subsystemWeather2.TargetLightningIntensity, windStartTime, windEndTime, targetWindStrength, subsystemWeather2.WindDirection, subsystemWeather2.CurrentWindStrength, (int)subsystemWeather2.CurrentSeason, subsystemWeather2.IsGustyWind));
								SendPrivateMessage(From, text14, netNode);
							}
							else
							{
								SendPrivateMessage(From, "Использование: /wind off/weak/medium/strong/storm", netNode);
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.StartsWith("/h "))
					{
						if (From.PlayerData.ServerManager)
						{
							string targetName7 = m_message.Substring(3).Trim();
							List<ComponentPlayer> list14 = projectNet.FindSubsystem<SubsystemPlayers>().ComponentPlayers.Where((ComponentPlayer p) => string.Equals(p.PlayerData.Name, targetName7, StringComparison.OrdinalIgnoreCase)).ToList();
							if (list14.Count == 0)
							{
								SendPrivateMessage(From, "Игрок не найден.", netNode);
							}
							else
							{
								ComponentPlayer componentPlayer12 = list14.First();
								ComponentHealth componentHealth3 = componentPlayer12.Entity.FindComponent<ComponentHealth>();
								ComponentVitalStats componentVitalStats2 = componentPlayer12.Entity.FindComponent<ComponentVitalStats>();
								ComponentFlu componentFlu2 = componentPlayer12.Entity.FindComponent<ComponentFlu>();
								ComponentSickness componentSickness2 = componentPlayer12.Entity.FindComponent<ComponentSickness>();
								if (componentHealth3 != null)
								{
									componentHealth3.Heal(1f);
									CommonLib.Net.QueuePackage(new ComponentHealthPackage(componentHealth3));
								}
								if (componentVitalStats2 != null)
								{
									componentVitalStats2.Food = 1f;
									componentVitalStats2.Sleep = 1f;
								}
								if (componentFlu2 != null)
								{
									componentFlu2.m_fluDuration = 0f;
								}
								if (componentSickness2 != null)
								{
									componentSickness2.m_sicknessDuration = 0f;
									componentSickness2.m_pukeParticleSystem = null;
								}
								SendPrivateMessage(From, componentPlayer12.PlayerData.Name + " has been fully healed.", netNode);
								Log.Information("Healed " + componentPlayer12.PlayerData.Name);
							}
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
					else if (m_message.Trim().Equals("/v", StringComparison.OrdinalIgnoreCase))
					{
						if (From.PlayerData.ServerManager)
						{
							PlayerData playerData6 = From.PlayerData;
							playerData6.IsVanish = !playerData6.IsVanish;
							ComponentPlayer componentPlayer13 = playerData6.ComponentPlayer;
							if (componentPlayer13 != null)
							{
								if (componentPlayer13.ComponentCreatureModel != null)
								{
									componentPlayer13.ComponentCreatureModel.IsVanished = false;
								}
								for (int num21 = 0; num21 < 2; num21++)
								{
									ComponentPlayerPackage componentPlayerPackage = (playerData6.IsVanish ? ComponentPlayerPackage.CreatePositionUpdate(componentPlayer13, ComponentPlayerPackage.VanishParkedPosition, null) : ComponentPlayerPackage.CreatePositionUpdate(componentPlayer13, componentPlayer13.ComponentBody.Position, componentPlayer13.ComponentBody.Rotation));
									componentPlayerPackage.Except = playerData6.Client;
									CommonLib.Net.QueuePackage(componentPlayerPackage);
								}
							}
							string text15 = (playerData6.IsVanish ? "включён" : "выключен");
							SendPrivateMessage(From, "Ваниш " + text15, netNode);
						}
						else
						{
							SendPrivateMessage(From, "А-та-та, а по жопе?", netNode);
						}
					}
				}
			}
			catch (Exception ex4)
			{
				Log.Error(ex4.Message);
			}
			if (num == 0 && !projectNet.FindSubsystem<SubsystemPlayers>().NoMsgPlayerGuidList.Contains(From.GUID.ToString()))
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		}
		}
	}

	private void ChangeGameMode(GameMode gameMode)
	{
		SubsystemGameInfo subsystemGameInfo = From.PlayerData.SubsystemGameWidgets.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		WorldSettings worldSettings = subsystemGameInfo.WorldSettings;
		worldSettings.GameMode = gameMode;
		if (gameMode == GameMode.Creative)
		{
			worldSettings.ResetOptionsForNonCreativeMode();
		}
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		worldSettings.Save(valuesDictionary, liveModifiableParametersOnly: false);
		subsystemGameInfo.Save(valuesDictionary);
	}

	private void ChangeWorldPassword(string newPassword)
	{
		SubsystemGameInfo subsystemGameInfo = From.PlayerData.SubsystemGameWidgets.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		WorldSettings worldSettings = subsystemGameInfo.WorldSettings;
		worldSettings.Password = newPassword;
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		worldSettings.Save(valuesDictionary, liveModifiableParametersOnly: false);
		subsystemGameInfo.Save(valuesDictionary);
	}

	private static uint ComputeStringHash(string s)
	{
		uint num = 2166136261u;
		if (s != null)
		{
			for (int i = 0; i < s.Length; i++)
			{
				num = (s[i] ^ num) * 16777619;
			}
		}
		return num;
	}

	private void TeleportToHome(IClient client, Vector3 targetPosition, int slot, NetNode netNode)
	{
		ComponentPlayer componentPlayer = client.PlayerData.ComponentPlayer;
		if (client.PlayerData.ServerManager)
		{
			componentPlayer.ComponentBody.Position = targetPosition;
			componentPlayer.SecurityGrace(3.0);
			componentPlayer.ComponentBody.Velocity = Vector3.Zero;
			netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer.PlayerData, 0, playSound: true, componentPlayer.PlayerData.Level));
			netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer, ComponentPlayerPackage.PlayerAction.PositionSet));
			SendPrivateMessage(client, "Телепортированы домой " + slot + " (бесплатно для админа)", netNode);
			if (CommonLib.WorkType == WorkType.Server && GameScreen.m_positionWriter != null)
			{
				SubsystemTerrain subsystemTerrain = componentPlayer.Project.FindSubsystem<SubsystemTerrain>();
				int num = Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(targetPosition.X), Terrain.ToCell(targetPosition.Y - 0.1f), Terrain.ToCell(targetPosition.Z)));
				string displayName = BlocksManager.Blocks[num].GetDisplayName(subsystemTerrain, num);
				GameScreen.m_positionWriter.WriteLine(string.Format("{0} [{1}] Teleported to Home {2}. Pos: {3:F1}, {4:F1}, {5:F1}. Block below: {6}", DateTime.Now.ToString("HH:mm:ss"), client.PlayerData.Name, slot, targetPosition.X, targetPosition.Y, targetPosition.Z, displayName));
				GameScreen.m_positionWriter.Flush();
				SubsystemPlayers subsystemPlayers = componentPlayer.Project.FindSubsystem<SubsystemPlayers>();
				if (subsystemPlayers.m_playerSnapshots.ContainsKey(client.PlayerData.PlayerGUID))
				{
					subsystemPlayers.m_playerSnapshots[client.PlayerData.PlayerGUID].Position = targetPosition;
				}
			}
			return;
		}
		float num2 = Vector3.Distance(componentPlayer.ComponentBody.Position, targetPosition);
		int num3 = 0;
		if (num2 >= 10000f)
		{
			num3 = 9;
		}
		else if (num2 >= 9000f)
		{
			num3 = 8;
		}
		else if (num2 >= 8000f)
		{
			num3 = 7;
		}
		else if (num2 >= 7000f)
		{
			num3 = 6;
		}
		else if (num2 >= 6000f)
		{
			num3 = 5;
		}
		else if (num2 >= 5000f)
		{
			num3 = 4;
		}
		else if (num2 >= 4000f)
		{
			num3 = 3;
		}
		else if (num2 >= 3000f)
		{
			num3 = 2;
		}
		else if (num2 >= 2000f)
		{
			num3 = 1;
		}
		if (num3 > 0 && client.PlayerData.Level < (float)num3)
		{
			SendPrivateMessage(client, "Недостаточно уровней для телепортации (нужно " + num3 + ").", netNode);
			return;
		}
		if (num3 > 0)
		{
			client.PlayerData.Level -= num3;
		}
		componentPlayer.ComponentBody.Position = targetPosition;
		componentPlayer.SecurityGrace(3.0);
		componentPlayer.ComponentBody.Velocity = Vector3.Zero;
		netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer.PlayerData, 0, playSound: true, componentPlayer.PlayerData.Level));
		netNode.QueuePackage(new ComponentPlayerPackage(componentPlayer, ComponentPlayerPackage.PlayerAction.PositionSet));
		string text = "Телепортация на точку дома " + slot;
		if (num3 > 0)
		{
			text = text + " (-" + num3 + " ур.)";
		}
		SendPrivateMessage(client, text, netNode);
		if (CommonLib.WorkType == WorkType.Server && GameScreen.m_positionWriter != null)
		{
			SubsystemTerrain subsystemTerrain2 = componentPlayer.Project.FindSubsystem<SubsystemTerrain>();
			int num4 = Terrain.ExtractContents(subsystemTerrain2.Terrain.GetCellValue(Terrain.ToCell(targetPosition.X), Terrain.ToCell(targetPosition.Y - 0.1f), Terrain.ToCell(targetPosition.Z)));
			string displayName2 = BlocksManager.Blocks[num4].GetDisplayName(subsystemTerrain2, num4);
			GameScreen.m_positionWriter.WriteLine(string.Format("{0} [{1}] Teleported to Home {2}. Pos: {3:F1}, {4:F1}, {5:F1}. Block below: {6}", DateTime.Now.ToString("HH:mm:ss"), client.PlayerData.Name, slot, targetPosition.X, targetPosition.Y, targetPosition.Z, displayName2));
			GameScreen.m_positionWriter.Flush();
			SubsystemPlayers subsystemPlayers2 = componentPlayer.Project.FindSubsystem<SubsystemPlayers>();
			if (subsystemPlayers2.m_playerSnapshots.ContainsKey(client.PlayerData.PlayerGUID))
			{
				subsystemPlayers2.m_playerSnapshots[client.PlayerData.PlayerGUID].Position = targetPosition;
			}
		}
	}

	private void SendPrivateMessage(IClient toClient, string text, NetNode netNode)
	{
		netNode.QueuePackage(new MessagePackage(null, text, 0, null)
		{
			To = toClient
		});
	}

	static MessagePackage()
	{
		s_pendingTeleportRequests = new Dictionary<Guid, Guid>();
		s_pendingTeleportRequestTimes = new Dictionary<Guid, DateTime>();
	}
}
