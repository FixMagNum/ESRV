using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using LiteNetLib.Utils;
using TemplatesDatabase;
using XmlUtilities;

namespace Game.NetWork;

public static class CommonLib
{
	public const int DISCONNECT_TIMEOUT = 5000;

	public static NetNode Net;

	public static Texture2D BlockTexture;

	public static bool IsOfflineMode;

	private static WorkType m_workType;

	private static string m_hardwareId;

	private static ComponentPlayer m_cachedMainPlayer;

	public static Guid UserId { get; private set; }

	public static WorkType WorkType
	{
		get
		{
			return m_workType;
		}
		set
		{
			if (value != WorkType.Client)
			{
				IsOfflineMode = true;
			}
			m_workType = value;
			UserId = new Guid(IsOfflineMode ? SettingsManager.OfflineToken : SettingsManager.CommunityAccessToken);
		}
	}

	public static ComponentPlayer MainPlayer
	{
		get
		{
			if (GameManager.Project != null && Net.Self != null)
			{
				if (m_cachedMainPlayer != null && m_cachedMainPlayer.Project == GameManager.Project && m_cachedMainPlayer.PlayerData.PlayerGUID == Net.Self.GUID)
				{
					return m_cachedMainPlayer;
				}
				foreach (ComponentPlayer componentPlayer in GameManager.Project.FindSubsystem<SubsystemPlayers>().ComponentPlayers)
				{
					if (componentPlayer.PlayerData.PlayerGUID == Net.Self.GUID)
					{
						m_cachedMainPlayer = componentPlayer;
						return componentPlayer;
					}
				}
			}
			m_cachedMainPlayer = null;
			return null;
		}
	}

	public static string HardwareId
	{
		get
		{
			if (string.IsNullOrEmpty(m_hardwareId))
			{
				m_hardwareId = CalculateHardwareId();
			}
			return m_hardwareId;
		}
	}

	public static bool IsWorkTypeValid()
	{
		return WorkType == GetCurrentWorkType();
	}

	public static WorkType GetCurrentWorkType()
	{
		if (Net.IsServer)
		{
			return WorkType.Server;
		}
		if (Net.CurrentStage == NetNode.Stage.Connected)
		{
			return WorkType.Client;
		}
		return WorkType.Local;
	}

	public static bool StartServer()
	{
		IsOfflineMode = true;
		WorkType = WorkType.Server;
		Net.OnUnconnectedRecieve += ReplyServerInfo;
		Net.OnBroadcastRecieve += ReplyDiscoveryRequest;
		bool flag = Net.StartServer(SettingsManager.ServerPort, SettingsManager.BroadcastPort);
		if (!flag)
		{
			WorkType = WorkType.Local;
		}
		return flag;
	}

	public static void ReplyServerInfo(IPEndPoint ep, Package p)
	{
		if (p.Flag == PackageFlag.ServerInfoRequest && WorkType == WorkType.Server && GameManager.Project != null)
		{
			Net.netManager.SendUnconnectedMessage(Net.MakePackage(PackageFlag.ServerInfoReply, (NetDataWriter w) =>
			{
				WriteServerInfo(w);
			}).Writer, ep);
		}
	}

	public static void ReplyDiscoveryRequest(IPEndPoint ep, Package p)
	{
		if (p.Flag == PackageFlag.DiscoveryRequest && WorkType == WorkType.Server && GameManager.Project != null)
		{
			Net.netManager.SendUnconnectedMessage(Net.MakePackage(PackageFlag.DiscoveryReply, (NetDataWriter w) =>
			{
				WriteServerInfo(w);
			}).Writer, ep);
		}
	}

	public static void WriteServerInfo(NetDataWriter w)
	{
		w.Put(SettingsManager.CurrentModVersion);
		w.Put((ushort)Net.ClientCount);
		w.Put(ProjectNet.subsystemGameInfo.WorldSettings.MaxOnlinePlayerCount);
		w.Put((byte)ProjectNet.subsystemGameInfo.WorldSettings.GameMode);
		w.Put(!string.IsNullOrEmpty(ProjectNet.subsystemGameInfo.WorldSettings.Password));
		w.Put(ProjectNet.subsystemGameInfo.WorldSettings.IsNeedCommunityLogin);
		w.Put(ProjectNet.subsystemTimeOfDay.CalculateTimeOfDay());
		int value = (int)(((GameManager.Project != null) ? GameManager.Project.FindSubsystem<SubsystemWeather>(throwOnError: true) : null)?.CurrentSeason ?? ((SubsystemWeather.Season)(-1)));
		w.Put(value);
		w.Put(ProjectNet.subsystemGameInfo.WorldSettings.RequirePassword);
	}

	public static void ReadServerInfo(NetPlayScreen.Connect c, NetDataReader r, Stopwatch s, IPEndPoint ip, bool isLocal = false)
	{
		c.version = r.GetString();
		c.playerCount = r.GetUShort();
		c.maxCount = r.GetUShort();
		c.gameMode = (GameMode)r.GetByte();
		c.HasPassword = r.GetBool();
		c.IsNeedLoginCommunity = r.GetBool();
		c.TimeOfDay = r.GetFloat();
		c.state = NetPlayScreen.ConnectState.Avaliable;
		c.usedTime = s.ElapsedMilliseconds;
		if (isLocal)
		{
			c.name = "[Local] " + c.name;
			c.ip = string.Concat(ip.Address, ":", ip.Port);
		}
		if (r.AvailableBytes >= 4)
		{
			try
			{
				c.seasonRaw = r.GetInt();
				c.seasonError = false;
			}
			catch
			{
				c.seasonRaw = null;
				c.seasonError = true;
			}
		}
		else
		{
			c.seasonRaw = null;
			c.seasonError = true;
		}
		if (r.AvailableBytes >= 1)
		{
			try
			{
				c.RequirePersonalPassword = r.GetBool();
			}
			catch
			{
				c.RequirePersonalPassword = false;
			}
		}
	}

	public static string GetInnerIp()
	{
		IPAddress[] hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
		IPAddress[] array = hostAddresses;
		foreach (IPAddress iPAddress in array)
		{
			if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
			{
				return iPAddress.ToString();
			}
		}
		return hostAddresses[0].ToString();
	}

	public static bool Resolve(string ip, out IPEndPoint ep)
	{
		if (Uri.TryCreate("http://" + ip, UriKind.Absolute, out var result))
		{
			int port = (result.IsDefaultPort ? SettingsManager.ServerPort : result.Port);
			if (IPAddress.TryParse(result.Host, out var address))
			{
				ep = new IPEndPoint(address, port);
				return true;
			}
			IPHostEntry hostEntry = Dns.GetHostEntry(result.Host);
			if (hostEntry.AddressList.Length != 0)
			{
				ep = new IPEndPoint(hostEntry.AddressList[0], port);
				return true;
			}
		}
		ep = null;
		return false;
	}

	public static string GetNowProject(Project project)
	{
		if (project is ProjectNet path)
		{
			path.SendToClientMode = true;
			XElement xElement = new XElement("Project");
			project.Save().Save(xElement);
			xElement.Element("Entities")?.Remove();
			string result = xElement.ToString();
			path.SendToClientMode = false;
			return result;
		}
		return string.Empty;
	}

	public static byte[] Compress(Stream stream)
	{
		stream.Position = 0L;
		byte[] array = new byte[stream.Length];
		stream.Read(array, 0, (int)stream.Length);
		return Compress(array);
	}

	public static byte[] Compress(byte[] datas)
	{
		byte[] array = null;
		using MemoryStream memoryStream = new MemoryStream(datas.Length);
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
		{
			deflateStream.Write(datas, 0, datas.Length);
		}
		return memoryStream.ToArray();
	}

	public static byte[] Decompress(byte[] inputBytes)
	{
		using MemoryStream stream = new MemoryStream(inputBytes);
		using DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
		using MemoryStream memoryStream = new MemoryStream(inputBytes.Length * 4);
		deflateStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public static string SerializeVDict(ValuesDictionary dict)
	{
		XElement node = new XElement("Values");
		dict.Save(node);
		return XmlUtils.SaveXmlToString(node, throwOnError: true);
	}

	public static ValuesDictionary ReadVDict(string str)
	{
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		XElement overridesNode = XmlUtils.LoadXmlFromString(str, throwOnError: true);
		valuesDictionary.ApplyOverrides(overridesNode);
		return valuesDictionary;
	}

	public static long GetMicroSeconds()
	{
		return DateTime.Now.Ticks / 10000;
	}

	public static void SendEditItemRequest(EditableItemType editableItemType, bool isEditAsItem, int slotIndex, CellFace cellFace, object obj)
	{
		Net.SendPackage(PackageFlag.ChangeEditableRequest, (NetDataWriter w) =>
		{
			WriteEditItem(w, editableItemType, isEditAsItem, slotIndex, cellFace, obj);
		});
	}

	public static void WriteEditItem(NetDataWriter w, EditableItemType editableItemType, bool isEditAsItem, int slotIndex, CellFace cellFace, object obj)
	{
		w.Put((byte)((uint)editableItemType | (uint)(isEditAsItem ? 128 : 0)));
		if (isEditAsItem)
		{
			w.Put(slotIndex);
		}
		else
		{
			w.PutCellFace(cellFace);
		}
		PutEditItemData(w, editableItemType, obj);
	}

	public static void PutEditItemData(NetDataWriter w, EditableItemType type, object obj)
	{
		if ((int)type <= 1)
		{
			WriteEditableItemData(w, (IEditableItemData)obj);
		}
		else if ((int)(type - 2) <= 2)
		{
			int num2 = (int)obj;
			w.Put(num2);
		}
	}

	public static object GetEditItemData(NetDataReader r, EditableItemType type)
	{
		if ((int)type <= 1)
		{
			return ReadEditableItemData(r, type);
		}
		if ((int)(type - 2) > 2)
		{
			return null;
		}
		return r.GetInt();
	}

	public static void WriteEditableItemData(NetDataWriter w, IEditableItemData data)
	{
		if (data is MemoryBankData memoryBankData)
		{
			w.PutBytesWithLength(memoryBankData.Data.Array);
		}
		else if (data is TruthTableData truthTableData)
		{
			w.PutBytesWithLength(truthTableData.Data);
		}
	}

	public static IEditableItemData ReadEditableItemData(NetDataReader r, EditableItemType type)
	{
		switch (type)
		{
		default:
			return null;
		case EditableItemType.TruthTable:
			return new TruthTableData
			{
				Data = r.GetBytesWithLength()
			};
		case EditableItemType.MemoryBank:
		{
			MemoryBankData memoryBankData = new MemoryBankData();
			byte[] bytesWithLength = r.GetBytesWithLength();
			memoryBankData.Data = new DynamicArray<byte>(bytesWithLength);
			return memoryBankData;
		}
		}
	}

	public static void ReplaceDataAtSlot(IInventory inventory, int slotIndex, Func<int, int> newData)
	{
		int slotValue = inventory.GetSlotValue(slotIndex);
		int slotCount = inventory.GetSlotCount(slotIndex);
		inventory.RemoveSlotItems(slotIndex, slotCount);
		inventory.AddSlotItems(slotIndex, Terrain.ReplaceData(slotValue, newData(Terrain.ExtractData(slotValue))), slotCount);
	}

	public static void SendChangeEditableItemData(int id, IEditableItemData data)
	{
		Net.SendPackage(PackageFlag.ChangeEditableItemData, (NetDataWriter w) =>
		{
			w.Put((byte)(1 | (((data is MemoryBankData) ? 1u : 0u) << 1)));
			w.Put((ushort)id);
			WriteEditableItemData(w, data);
		});
	}

	public static void SendChangeEditableItemData(Point3 point, IEditableItemData data)
	{
		Net.SendPackage(PackageFlag.ChangeEditableItemData, (NetDataWriter w) =>
		{
			w.Put((byte)(((data is MemoryBankData) ? 1u : 0u) << 1));
			w.PutPoint3(point);
			WriteEditableItemData(w, data);
		});
	}

	public static void RecieveChangeEditableItemData(Package p, Project project)
	{
		NetDataReader reader = p.Reader;
		byte b = reader.GetByte();
		bool flag = (b & 1) > 0;
		bool flag2 = (b & 2) > 0;
		if (flag)
		{
			int uShort = reader.GetUShort();
			if (flag2)
			{
				MemoryBankData value = (MemoryBankData)ReadEditableItemData(reader, EditableItemType.MemoryBank);
				project.FindSubsystem<SubsystemMemoryBankBlockBehavior>().m_itemsData[uShort] = value;
			}
			else
			{
				TruthTableData value2 = (TruthTableData)ReadEditableItemData(reader, EditableItemType.TruthTable);
				project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>().m_itemsData[uShort] = value2;
			}
		}
		else
		{
			Point3 point = reader.GetPoint3();
			if (flag2)
			{
				MemoryBankData t = (MemoryBankData)ReadEditableItemData(reader, EditableItemType.MemoryBank);
				project.FindSubsystem<SubsystemMemoryBankBlockBehavior>().SetBlockData(point, t);
			}
			else
			{
				TruthTableData t2 = (TruthTableData)ReadEditableItemData(reader, EditableItemType.TruthTable);
				project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>().SetBlockData(point, t2);
			}
		}
	}

	public static void PerformEditItem(Project project, EditableItemType editableItemType, object extraData, object itemData)
	{
		IInventory inventory = null;
		int slotIndex = 0;
		CellFace cellFace = default;
		bool flag = extraData is ValueTuple<int, IInventory>;
		if (flag)
		{
			(slotIndex, inventory) = ((int, IInventory))extraData;
		}
		else
		{
			cellFace = (CellFace)extraData;
		}
		switch (editableItemType)
		{
		case EditableItemType.MemoryBank:
		{
			MemoryBankData memoryBankData = (MemoryBankData)itemData;
			if (flag)
			{
				int id = project.FindSubsystem<SubsystemMemoryBankBlockBehavior>().StoreItemDataAtUniqueId(memoryBankData);
				SendChangeEditableItemData(id, memoryBankData);
				ReplaceDataAtSlot(inventory, slotIndex, (int d) => id);
			}
			else
			{
				project.FindSubsystem<SubsystemMemoryBankBlockBehavior>().SetBlockData(cellFace.Point, memoryBankData);
				SendChangeEditableItemData(cellFace.Point, memoryBankData);
			}
			break;
		}
		case EditableItemType.TruthTable:
		{
			TruthTableData truthTableData = (TruthTableData)itemData;
			if (flag)
			{
				int id2 = project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>().StoreItemDataAtUniqueId(truthTableData);
				SendChangeEditableItemData(id2, truthTableData);
				ReplaceDataAtSlot(inventory, slotIndex, (int d) => id2);
			}
			else
			{
				project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>().SetBlockData(cellFace.Point, truthTableData);
				SendChangeEditableItemData(cellFace.Point, truthTableData);
			}
			break;
		}
		case EditableItemType.AdjustableDelayGate:
		{
			itemData.GetType();
			int delay = (int)itemData;
			if (flag)
			{
				ReplaceDataAtSlot(inventory, slotIndex, (int d) => AdjustableDelayGateBlock.SetDelay(d, delay));
			}
			else
			{
				SubsystemTerrain subsystemTerrain2 = project.FindSubsystem<SubsystemTerrain>();
				int cellValue = subsystemTerrain2.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				subsystemTerrain2.ChangeCell(value: Terrain.ReplaceData(cellValue, AdjustableDelayGateBlock.SetDelay(Terrain.ExtractData(cellValue), delay)), x: cellFace.X, y: cellFace.Y, z: cellFace.Z);
			}
			break;
		}
		case EditableItemType.Battery:
		{
			int voltage = (int)itemData;
			if (flag)
			{
				ReplaceDataAtSlot(inventory, slotIndex, (int d) => BatteryBlock.SetVoltageLevel(d, voltage));
			}
			else
			{
				SubsystemTerrain subsystemTerrain3 = project.FindSubsystem<SubsystemTerrain>();
				int cellValue2 = subsystemTerrain3.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				subsystemTerrain3.ChangeCell(value: Terrain.ReplaceData(cellValue2, BatteryBlock.SetVoltageLevel(Terrain.ExtractData(cellValue2), voltage)), x: cellFace.X, y: cellFace.Y, z: cellFace.Z);
			}
			break;
		}
		case EditableItemType.Piston:
		{
			int newData = (int)itemData;
			if (flag)
			{
				ReplaceDataAtSlot(inventory, slotIndex, (int d) => newData);
			}
			else
			{
				SubsystemTerrain subsystemTerrain = project.FindSubsystem<SubsystemTerrain>();
				subsystemTerrain.ChangeCell(value: Terrain.ReplaceData(subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z), newData), x: cellFace.X, y: cellFace.Y, z: cellFace.Z);
			}
			break;
		}
		}
	}

	public static void RecieveEditItemRequest(Package package, Guid guid, Project project)
	{
		NetDataReader reader = package.Reader;
		byte b = reader.GetByte();
		EditableItemType editableItemType = (EditableItemType)(b & 0x7F);
		bool flag = (b & 0x80) > 0;
		CellFace cellFace = default;
		IInventory item = ProjectNet.subsystemPlayers.ComponentPlayers.FirstOrDefault((ComponentPlayer p) => p.PlayerData.PlayerGUID == guid)?.ComponentMiner.Inventory;
		if (flag)
		{
			int item2 = reader.GetInt();
			PerformEditItem(project, editableItemType, (item2, item), GetEditItemData(reader, editableItemType));
		}
		else
		{
			cellFace = reader.GetCellFace();
			PerformEditItem(project, editableItemType, cellFace, GetEditItemData(reader, editableItemType));
		}
	}

	public static void DeSerializeSetPlayerData(byte[] data, Guid PlayerGuid)
	{
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(data));
		string path = binaryReader.ReadString();
		string xElement = binaryReader.ReadString();
		binaryReader.ReadByte();
		PlayerData stream2 = null;
		foreach (PlayerData playersDatum in ProjectNet.subsystemPlayers.PlayersData)
		{
			if (playersDatum.PlayerGUID == PlayerGuid)
			{
				stream2 = playersDatum;
				break;
			}
		}
		if (stream2 != null)
		{
			stream2.Name = path;
			stream2.CharacterSkinName = xElement;
		}
	}

	static CommonLib()
	{
		Net = new NetNode();
	}

	private static string CalculateHardwareId()
	{
		string text = "-";
		string text2 = "-";
		string text3 = "-";
		string text4 = "-";
		string text5 = "-";
		string text6 = "-";
		try
		{
			text = Environment.OSVersion.Platform.ToString();
		}
		catch
		{
		}
		try
		{
			string machineName = Environment.MachineName;
			if (!string.IsNullOrEmpty(machineName))
			{
				text2 = machineName;
			}
		}
		catch
		{
		}
		try
		{
			string versionString = Environment.OSVersion.VersionString;
			if (!string.IsNullOrEmpty(versionString))
			{
				text3 = versionString;
			}
		}
		catch
		{
		}
		try
		{
			text4 = Environment.ProcessorCount.ToString();
		}
		catch
		{
		}
		try
		{
			text5 = (Environment.Is64BitOperatingSystem ? "64" : "32");
		}
		catch
		{
		}
		try
		{
			text6 = (Environment.Is64BitProcess ? "64" : "32");
		}
		catch
		{
		}
		string text7 = (text ?? "-") + "|" + (text2 ?? "-") + "|" + (text3 ?? "-") + "|" + (text4 ?? "-") + "|" + (text5 ?? "-") + "|" + (text6 ?? "-");
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text7);
			using SHA1 sHA = SHA1.Create();
			byte[] array = sHA.ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("X2"));
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return text7;
		}
	}
}
