using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Engine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.NetWork;

public class NetNode
{
	public enum Stage
	{
		NotConnected,
		WaitForClientList,
		Connected
	}

	private struct ResCheck
	{
		public bool systemMod;

		public string resMd5;

		public string modName;

		public string modVer;
	}

	public readonly EventBasedNetListener listener;

	public readonly NetManager netManager;

	private readonly Dictionary<int, IClient> clients = new Dictionary<int, IClient>();

	private NetPeer pendingPeer;

	private HashSet<byte> agreeOnPendingPeer = new HashSet<byte>();

	private List<IPackage> PendingPackages = new List<IPackage>();

	public static string CommunityNickName;

	private readonly NetManager broadcastNetManager;

	private Dictionary<IClient, string> clientsToRemove = new Dictionary<IClient, string>();

	private bool isStopping;

	public long sec_length;

	public long sec_length_total;

	public long sec_total;

	public static Stream m_networkStream;

	public static StreamWriter m_networkWriter;

	private Dictionary<IClient, int> packetFloodCounters = new Dictionary<IClient, int>();

	public IClient Self { get; private set; }

	public IClient Server { get; private set; }

	public string Error { get; private set; }

	public Stage CurrentStage { get; private set; }

	public bool IsConnected => CurrentStage == Stage.Connected;

	public bool IsServer
	{
		get
		{
			if (CurrentStage == Stage.Connected)
			{
				return Self == Server;
			}
			return false;
		}
	}

	public int ClientCount
	{
		get
		{
			int num = 0;
			foreach (KeyValuePair<int, IClient> client in clients)
			{
				if (client.Value != Self)
				{
					num++;
				}
			}
			return num;
		}
	}

	private IClient pendingClient => (IClient)(pendingPeer?.Tag);

	public IEnumerable<IClient> Peers => clients.Values.Where((IClient c) => c != Self);

	public event Action Stopped;

	public event Action<Package> OnRecieve;

	public event Action<IClient> OnClientStateChanged;

	public event Action<IPEndPoint, Package> OnUnconnectedRecieve;

	public event Action<IPEndPoint, Package> OnBroadcastRecieve;

	public NetNode()
	{
		EnsureNetworkLogOpened();
		listener = new EventBasedNetListener();
		netManager = new NetManager(listener)
		{
			MaxConnectAttempts = 6,
			DisconnectTimeout = 5000,
			UnconnectedMessagesEnabled = true,
			UseSafeMtu = true,
			UpdateTime = 25
		};
		EventBasedNetListener eventBasedNetListener = new EventBasedNetListener();
		eventBasedNetListener.NetworkReceiveUnconnectedEvent += UnconnectedReceiveEvent;
		broadcastNetManager = new NetManager(eventBasedNetListener);
		broadcastNetManager.UnconnectedMessagesEnabled = true;
		broadcastNetManager.BroadcastReceiveEnabled = true;
	}

	public void QueuePackage(IPackage package)
	{
		lock (PendingPackages)
		{
			PendingPackages.Add(package);
		}
	}

	public Package MakePackage(PackageFlag flag, Action<NetDataWriter> f = null)
	{
		Package num = new Package(Self, flag);
		f?.Invoke(num.Writer);
		return num;
	}

	public void SendPackage(PackageFlag flag, Action<NetDataWriter> f, IClient to = null, DeliveryMethod m = DeliveryMethod.ReliableOrdered)
	{
		Send(MakePackage(flag, f), to, m);
	}

	private byte FindUnusedIndex()
	{
		for (byte node = 0; node < byte.MaxValue; node++)
		{
			if (!clients.ContainsKey(node))
			{
				return node;
			}
		}
		throw new Exception("The server is full");
	}

	private Client CreateClient(NetPeer peer, Guid guid)
	{
		if (!IsServer)
		{
			throw new InvalidOperationException("creating client as non-server");
		}
		foreach (IClient item in new List<IClient>(Peers))
		{
			if (item.GUID == guid)
			{
				RemoveClient(item);
			}
		}
		Client client = (Client)(peer.Tag = new Client(FindUnusedIndex(), guid, ProjectNet.project));
		client.SetPeer(peer);
		return client;
	}

	private void AddClient(IClient c)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, IClient> client in clients)
		{
			if (client.Value.GUID == c.GUID)
			{
				list.Add(client.Key);
			}
		}
		foreach (int item in list)
		{
			RemoveClient(clients[item], "Player logged in from another location");
		}
		clients[c.ID] = c;
	}

	public void RemoveClient(IClient c, string reason = null)
	{
		if (clientsToRemove.ContainsKey(c))
		{
			if (string.IsNullOrEmpty(reason))
			{
				Dictionary<IClient, string> dictionary = clientsToRemove;
				dictionary[c] = dictionary[c] + "\n" + reason;
			}
		}
		else
		{
			clientsToRemove[c] = reason ?? string.Empty;
		}
	}

	public void RemoveClientImmediate(IClient c, string reason = null)
	{
		if (c == Self)
		{
			Stop();
			return;
		}
		if (c.Peer != null && c.Peer.ConnectionState != ConnectionState.Disconnected && c.Peer != null && c.Peer.ConnectionState != ConnectionState.Disconnected)
		{
			if (string.IsNullOrEmpty(reason))
			{
				c.Peer.Disconnect();
			}
			else
			{
				c.Peer.Disconnect(NetDataWriter.FromString(reason));
			}
		}
		if (c == Server)
		{
			Stop();
			CurrentStage = Stage.NotConnected;
		}
		else
		{
			if (!clients.ContainsKey(c.ID))
			{
				return;
			}
			if (IsServer)
			{
				SendToAll(MakePackage(PackageFlag.RemoveClient, (NetDataWriter f) =>
				{
					f.Put(c.ID);
				}), sendToNotReady: true);
			}
			c.State = ClientState.NotConnected;
			OnClientStateChanged?.Invoke(c);
			clients.Remove(c.ID);
		}
	}

	public void RemoveAllClients(string reason = null)
	{
		foreach (IClient bytes in new List<IClient>(clients.Values))
		{
			if (bytes == Self)
			{
				continue;
			}
			if (bytes.Peer != null && bytes.Peer.ConnectionState != ConnectionState.Disconnected)
			{
				if (reason != null)
				{
					bytes.Peer.Disconnect(NetDataWriter.FromString(reason));
				}
				else
				{
					bytes.Peer.Disconnect();
				}
			}
			clients.Remove(bytes.ID);
			bytes.State = ClientState.NotConnected;
		}
	}

	public IClient GetClientByID(byte id)
	{
		if (clients.ContainsKey(id))
		{
			return clients[id];
		}
		return null;
	}

	public IClient GetClientByGUID(Guid? guid)
	{
		return clients.Values.FirstOrDefault((IClient c) =>
		{
			Guid gUID = c.GUID;
			Guid? guid2 = guid;
			return gUID == guid2;
		});
	}

	private void SendClientList(NetPeer p)
	{
		NetDataWriter netDataWriter = new NetDataWriter();
		PutByte(netDataWriter, 0);
		PutByte(netDataWriter, clients.Count);
		foreach (IClient value in clients.Values)
		{
			netDataWriter.Put(value.ID);
			netDataWriter.PutGuid(value.GUID);
			PutByte(netDataWriter, (byte)value.State);
		}
		p.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
	}

	private void WaitForClientList(NetPeer peer, NetPacketReader reader, DeliveryMethod method)
	{
		try
		{
			PackageFlag packageFlag = (PackageFlag)reader.GetByte();
			switch (packageFlag)
			{
			case PackageFlag.ClientList:
			{
				byte b = reader.GetByte();
				for (int i = 0; i < b; i++)
				{
					Client c = new Client(reader.GetByte(), reader.GetGuid(), ProjectNet.project)
					{
						State = (ClientState)reader.GetByte()
					};
					AddClient(c);
				}
				Server = clients[0];
				Server.SetPeer(peer);
				peer.Tag = Server;
				foreach (IClient value in clients.Values)
				{
					if (value.GUID == CommonLib.UserId)
					{
						Self = value;
					}
				}
				if (Self == null)
				{
					throw new Exception("cannot find self in client infos");
				}
				CurrentStage = Stage.Connected;
				break;
			}
			case PackageFlag.GetNickName:
				CommunityNickName = reader.GetString();
				break;
			default:
				Log.Information($"Unhandled message type: {packageFlag}");
				break;
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			Error = ex.ToString();
			Stop();
		}
	}

	private void DeliveryEvent(NetPeer peer, object userData)
	{
		Client client = userData as Client;
		if (client != null)
		{
			agreeOnPendingPeer.Remove(client.ID);
		}
		if (agreeOnPendingPeer.Count == 0 && pendingClient != null)
		{
			AddClient(pendingClient);
			SendClientList(pendingPeer);
			pendingPeer = null;
		}
	}

	private void PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
	{
		Log.Information($"{peer.EndPoint}连接关闭: " + disconnectInfo.Reason);
		if (disconnectInfo.AdditionalData != null && disconnectInfo.AdditionalData.TryGetString(out var result))
		{
			Log.Information("----Additional message----");
			Error = result;
			Log.Information(Error);
			Log.Information("----End of message----");
		}
		IClient client = peer.Tag as Client;
		client.State = ClientState.NotConnected;
		RemoveClient(client);
		if (peer == pendingPeer)
		{
			pendingPeer = null;
			agreeOnPendingPeer.Clear();
		}
		else
		{
			agreeOnPendingPeer.Remove(client.ID);
			DeliveryEvent(null, null);
		}
	}

	private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
	{
		if (CurrentStage == Stage.NotConnected)
		{
			return;
		}
		IClient client = peer.Tag as IClient;
		Client client2 = client as Client;
		Package package = null;
		long timestamp = Stopwatch.GetTimestamp();
		try
		{
			if (client != null && IsServer)
			{
				if (!packetFloodCounters.ContainsKey(client))
				{
					packetFloodCounters[client] = 0;
				}
				packetFloodCounters[client]++;
				if (packetFloodCounters[client] > 200)
				{
					return;
				}
				if (client2 != null && client2.IncomingPackages.Count > 5000)
				{
					Log.Warning($"Client {client.ID} queue overflow (>{client2.IncomingPackages.Count} packages). Kicking.");
					RemoveClientImmediate(client, "Connection too slow or packet flood detected.");
					return;
				}
			}
			package = new Package(client, reader);
			if (client2 != null)
			{
				client2.IncomingPackages.Enqueue(package);
			}
			else
			{
				ProcessIncomingPackage(package, client, peer);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			LogNetworkIssue(string.Format("Receive Error from Client {0}: {1}", (client != null) ? client.ID.ToString() : "?", ex.Message));
			if (client != null)
			{
				if (package != null)
				{
					RemoveClient(client, "error on recieving " + package.Flag.ToString() + ":\n" + ex.ToString());
				}
				else
				{
					RemoveClient(client, ex.ToString());
				}
			}
		}
		finally
		{
			reader.Recycle();
			double num = (double)(Stopwatch.GetTimestamp() - timestamp) / ((double)Stopwatch.Frequency / 1000.0);
			if (num > 15.0 && package != null)
			{
				LogNetworkIssue(string.Format("Slow Receive: Parsing packet {0} from Client {1} took {2:0.0}ms", package.Flag, (client != null) ? client.ID.ToString() : "?", num));
			}
		}
	}

	private void UnconnectedReceiveEvent(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
	{
		if (CurrentStage == Stage.NotConnected)
		{
			return;
		}
		try
		{
			Package arg = new Package(null, reader);
			if (messageType == UnconnectedMessageType.BasicMessage)
			{
				OnUnconnectedRecieve?.Invoke(remoteEndPoint, arg);
			}
			else
			{
				OnBroadcastRecieve?.Invoke(remoteEndPoint, arg);
			}
		}
		catch
		{
		}
		finally
		{
			reader.Recycle();
		}
	}

	private void ConnectionRequestEvent(ConnectionRequest request)
	{
		Log.Information("Received connection request " + request.RemoteEndPoint);
		if (ProjectNet.project == null)
		{
			request.Reject();
			NetDataWriter netDataWriter = new NetDataWriter();
			PutByte(netDataWriter, 1);
			netDataWriter.Put("Please wait for the server to start before joining");
			netManager.SendUnconnectedMessage(netDataWriter, request.RemoteEndPoint);
			return;
		}
		if (pendingClient != null)
		{
			request.Reject();
			NetDataWriter netDataWriter2 = new NetDataWriter();
			PutByte(netDataWriter2, 1);
			netDataWriter2.Put($"Too many connection requests to the server. Please try again later. Waiting for client responses: {agreeOnPendingPeer.Count}");
			netManager.SendUnconnectedMessage(netDataWriter2, request.RemoteEndPoint);
			Log.Information("Too many connection requests to the server. Please try again later" + request.RemoteEndPoint);
			Log.Information("pendingClient::" + pendingClient.ID);
			return;
		}
		try
		{
			string magic = request.Data.GetString();
			Guid guid = request.Data.GetGuid();
			string version = request.Data.GetString();
			string pwd = request.Data.GetString();
			int modCount = request.Data.GetInt();
			StringBuilder connectionError = new StringBuilder();
			string empty = string.Empty;
			AcceptClient(magic, guid, version, pwd, modCount, empty, connectionError, request);
		}
		catch (Exception ex)
		{
			request.Reject();
			NetDataWriter netDataWriter3 = new NetDataWriter();
			PutByte(netDataWriter3, 1);
			netDataWriter3.Put("ConnectionRequestEvent Error:" + ex.Message);
			netManager.SendUnconnectedMessage(netDataWriter3, request.RemoteEndPoint);
			Log.Information("ConnectionRequestEvent Error:" + ex.StackTrace);
		}
	}

	public void AcceptClient(string magic, Guid userId, string version, string pwd, int modCount, string nickname, StringBuilder ConnectionError, ConnectionRequest request)
	{
		string password = ProjectNet.subsystemGameInfo.WorldSettings.Password;
		userId.ToString().Replace("-", "");
		NetDataReader data = request.Data;
		List<ResCheck> list = new List<ResCheck>();
		for (int i = 0; i < modCount; i++)
		{
			list.Add(new ResCheck
			{
				systemMod = data.GetBool(),
				resMd5 = data.GetString(),
				modName = data.GetString(),
				modVer = data.GetString()
			});
		}
		string text = string.Empty;
		string text2 = string.Empty;
		if (data.AvailableBytes > 0)
		{
			try
			{
				string text3 = data.GetString() ?? string.Empty;
				int num = text3.IndexOf('|');
				if (num >= 0)
				{
					text = text3.Substring(0, num);
					if (num + 1 < text3.Length)
					{
						text2 = text3.Substring(num + 1);
					}
				}
				else
				{
					text = text3;
				}
			}
			catch
			{
				text = string.Empty;
				text2 = string.Empty;
			}
		}
		if (string.IsNullOrEmpty(text2))
		{
			ConnectionError.AppendLine("Client fingerprint is missing or invalid");
		}
		SubsystemPlayers subsystemPlayers = null;
		if (ProjectNet.project != null)
		{
			subsystemPlayers = ProjectNet.project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		}
		string ipAddress = string.Empty;
		try
		{
			IPEndPoint remoteEndPoint = request.RemoteEndPoint;
			if (remoteEndPoint != null)
			{
				ipAddress = remoteEndPoint.Address.ToString();
			}
		}
		catch
		{
			ipAddress = string.Empty;
		}
		if (!(magic == "Magic"))
		{
			request.Reject();
			NetDataWriter netDataWriter = new NetDataWriter();
			PutByte(netDataWriter, 1);
			netDataWriter.Put("Invalid request");
			netManager.SendUnconnectedMessage(netDataWriter, request.RemoteEndPoint);
			Log.Information("Non-Magic connection request!" + request.RemoteEndPoint.ToString());
			return;
		}
		if (userId == CommonLib.UserId)
		{
			ConnectionError.AppendLine("Client and server tokens are identical");
		}
		else if (Peers.Where((IClient c) => c != Self && c.GUID == userId).FirstOrDefault() != null)
		{
			ConnectionError.AppendLine("Your ID is identical to an online player on the server.");
		}
		else if (subsystemPlayers != null && subsystemPlayers.IsBanned(userId, ipAddress, text2))
		{
			ConnectionError.AppendLine("You are banned on this server");
		}
		else if (version != SettingsManager.CurrentModVersion)
		{
			ConnectionError.AppendLine("Version mismatch: your version is " + version + ". The server requires version: " + SettingsManager.CurrentModVersion + ".");
		}
		else if (clients.Count >= ProjectNet.subsystemGameInfo.WorldSettings.MaxOnlinePlayerCount)
		{
			ConnectionError.AppendLine("Maximum number of online players reached, join request denied");
		}
		else if (!string.IsNullOrEmpty(password) && pwd != password)
		{
			ConnectionError.AppendLine("Password verification failed");
		}
		else
		{
			if (ProjectNet.subsystemGameInfo.WorldSettings.RequirePassword)
			{
				try
				{
					PlayerPasswordRepository.Initialize(ProjectNet.subsystemGameInfo.WorldSettings);
					bool flag = PlayerPasswordRepository.HasEntry(userId);
					bool flag2 = false;
					if (subsystemPlayers != null)
					{
						flag2 = subsystemPlayers.FindPlayerData((PlayerData p) => p.PlayerGUID == userId) != null;
					}
					string password2 = text ?? string.Empty;
					if (!flag)
					{
						PlayerPasswordRepository.Append(nickname, userId, password2);
					}
					else if (!flag2)
					{
						PlayerPasswordRepository.Set(nickname, userId, password2);
					}
					else if (!PlayerPasswordRepository.VerifyPassword(userId, password2))
					{
						ConnectionError.AppendLine("Personal password verification failed");
					}
				}
				catch (Exception ex)
				{
					ConnectionError.AppendLine("Personal password check error: " + ex.Message);
				}
				if (ConnectionError.Length > 0)
				{
					request.Reject();
					NetDataWriter netDataWriter2 = new NetDataWriter();
					PutByte(netDataWriter2, 1);
					netDataWriter2.Put(ConnectionError.ToString());
					netManager.SendUnconnectedMessage(netDataWriter2, request.RemoteEndPoint);
					return;
				}
			}
			subsystemPlayers?.RegisterConnectionInfo(userId, ipAddress, text2);
			pendingPeer = request.Accept();
			Client client = CreateClient(pendingPeer, userId);
			NetDataWriter netDataWriter3 = new NetDataWriter();
			PutByte(netDataWriter3, 12);
			netDataWriter3.Put(pendingClient.ID);
			netDataWriter3.PutGuid(pendingClient.GUID);
			pendingClient.OnGranteStateChange(state: true);
			foreach (IClient peer in Peers)
			{
				if (peer != Self)
				{
					peer.Peer.Send(netDataWriter3, DeliveryMethod.ReliableOrdered);
				}
			}
			NetDataWriter netDataWriter4 = new NetDataWriter();
			PutByte(netDataWriter4, 10);
			netDataWriter4.Put(nickname);
			client.Peer.Send(netDataWriter4, DeliveryMethod.ReliableOrdered);
		}
		if (ConnectionError.Length > 0)
		{
			request.Reject();
			NetDataWriter netDataWriter5 = new NetDataWriter();
			PutByte(netDataWriter5, 1);
			netDataWriter5.Put(ConnectionError.ToString());
			netManager.SendUnconnectedMessage(netDataWriter5, request.RemoteEndPoint);
		}
		else
		{
			Log.Information("Received connection request from " + request.RemoteEndPoint?.ToString() + ", " + ((pendingPeer != null) ? "accepted" : ("rejected -- " + ConnectionError.ToString())));
			DeliveryEvent(null, null);
		}
	}

	public bool StartServer(int port, int? broadcastPort)
	{
		if (netManager.IsRunning)
		{
			throw new InvalidOperationException("Server is already running");
		}
		try
		{
			isStopping = false;
			Self = new Client(0, CommonLib.UserId, ProjectNet.project)
			{
				State = ClientState.Connected
			};
			clients.Clear();
			clients.Add(0, Self);
			bool assemblyName = netManager.Start(port);
			if (broadcastPort.HasValue)
			{
				assemblyName &= broadcastNetManager.Start(broadcastPort.Value);
			}
			if (assemblyName)
			{
				Log.Information($"Server started successfully on port {netManager.LocalPort}");
				Window.Frame += Update;
				Window.Closed += StopImmediate;
				listener.ConnectionRequestEvent += ConnectionRequestEvent;
				listener.NetworkReceiveUnconnectedEvent += UnconnectedReceiveEvent;
				listener.NetworkReceiveEvent += NetworkReceiveEvent;
				listener.PeerDisconnectedEvent += PeerDisconnectedEvent;
				listener.DeliveryEvent += DeliveryEvent;
				CurrentStage = Stage.Connected;
				Server = Self;
			}
			else
			{
				CurrentStage = Stage.NotConnected;
				Stop();
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message);
		}
		return CurrentStage == Stage.Connected;
	}

	public void Send(Package p, IClient to = null, DeliveryMethod method = DeliveryMethod.ReliableOrdered, byte? channel = null)
	{
		try
		{
			if (CurrentStage != Stage.Connected)
			{
				return;
			}
			if (to == null)
			{
				if (IsServer)
				{
					SendToAll(p);
					return;
				}
				to = Server;
			}
			if (to.Peer != null)
			{
				if (channel.HasValue)
				{
					to.Peer.Send(p.Writer, channel.Value, DeliveryMethod.ReliableOrdered);
				}
				else
				{
					to.Peer.Send(p.Writer, method);
				}
			}
			else
			{
				Log.Error($"not connected to client: {to}");
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			LogNetworkIssue(string.Format("Send Error to Client {0}: {1}", (to != null) ? to.ID.ToString() : "?", ex.Message));
			if (to != null)
			{
				if (p != null)
				{
					RemoveClient(to, "error on sending " + p.Flag.ToString() + ":\n" + ex.ToString());
				}
				else
				{
					RemoveClient(to, ex.ToString());
				}
			}
		}
	}

	public void SendToAllExcept(Package p, IClient except, bool sendToNotReady = false, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
	{
		SendToAllExcept(p, except?.ID, sendToNotReady, method);
	}

	public void SendToAllExcept(Package p, byte? except, bool sendToNotReady = false, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
	{
		foreach (Client worldInfo2 in Peers)
		{
			if (worldInfo2.ID != except && (sendToNotReady || worldInfo2.State == ClientState.Playing))
			{
				Send(p, worldInfo2, method);
			}
		}
	}

	public void SendToAll(Package p, bool sendToNotReady = false, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
	{
		foreach (Client array2 in Peers)
		{
			if (sendToNotReady || array2.State == ClientState.Playing)
			{
				Send(p, array2, method);
			}
		}
	}

	private void HandleConnectionReject(IPEndPoint sender, NetDataReader reader, UnconnectedMessageType type)
	{
		try
		{
			if (reader.GetByte() == 1)
			{
				Stop();
				Error = "[Connection Rejected] " + reader.GetString();
			}
		}
		catch
		{
		}
	}

	public bool ConnectServer(IPEndPoint ep, string passwd = "", bool useFrameUpdate = true)
	{
		try
		{
			isStopping = false;
			clients.Clear();
			Log.Information($"connecting to server at {ep.Address}");
			netManager.Start();
			NetDataWriter netDataWriter = new NetDataWriter();
			netDataWriter.Put("Magic");
			netDataWriter.PutGuid(CommonLib.UserId);
			netDataWriter.Put(SettingsManager.CurrentModVersion);
			netDataWriter.Put(passwd);
			List<ModEntity> modList = ModsManager.ModList;
			netDataWriter.Put(modList.Count);
			for (int i = 0; i < modList.Count; i++)
			{
				ModEntity modEntity = modList[i];
				netDataWriter.Put(modEntity.IsSystemMod);
				netDataWriter.Put(modEntity.ResourcesMd5);
				netDataWriter.Put(modEntity.modInfo.PackageName);
				netDataWriter.Put(modEntity.modInfo.Version);
			}
			string text = SettingsManager.OnlinePassword ?? string.Empty;
			string text2 = CommonLib.HardwareId ?? string.Empty;
			string value = text;
			if (!string.IsNullOrEmpty(text2))
			{
				value = text + "|" + text2;
			}
			netDataWriter.Put(value);
			netManager.Connect(ep, netDataWriter);
			CurrentStage = Stage.WaitForClientList;
			listener.NetworkReceiveEvent += WaitForClientList;
			listener.NetworkReceiveUnconnectedEvent += HandleConnectionReject;
			listener.NetworkErrorEvent += (IPEndPoint iPEndPoint, SocketError arg) =>
			{
				Log.Information("Connection error" + arg);
			};
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (CurrentStage == Stage.WaitForClientList && stopwatch.ElapsedMilliseconds < 5000 && netManager.IsRunning)
			{
				Update();
				Thread.Sleep(50);
			}
			stopwatch.Stop();
			listener.NetworkReceiveEvent -= WaitForClientList;
			listener.NetworkReceiveUnconnectedEvent -= HandleConnectionReject;
			if (stopwatch.ElapsedMilliseconds >= 5000)
			{
				Error = "Connection timeout";
			}
			if (CurrentStage != Stage.Connected)
			{
				CurrentStage = Stage.NotConnected;
			}
			if ((CurrentStage == Stage.Connected) & useFrameUpdate)
			{
				Window.Closed += StopImmediate;
				Window.Frame += Update;
				listener.NetworkReceiveEvent += NetworkReceiveEvent;
				listener.ConnectionRequestEvent += ConnectionRequestEvent;
				listener.NetworkReceiveUnconnectedEvent += UnconnectedReceiveEvent;
				listener.PeerDisconnectedEvent += PeerDisconnectedEvent;
				SendPackage(PackageFlag.ClientState, (NetDataWriter w) =>
				{
					PutByte(w, 2);
				}, Server);
			}
		}
		catch
		{
			CurrentStage = Stage.NotConnected;
		}
		return CurrentStage == Stage.Connected;
	}

	public void Stop()
	{
		isStopping = true;
	}

	public void StopImmediate()
	{
		try
		{
			if (CommonLib.WorkType == WorkType.Server)
			{
				RemoveAllClients("Server closed voluntarily");
			}
			netManager.Stop(sendDisconnectMessages: true);
			if (broadcastNetManager.IsRunning)
			{
				broadcastNetManager.Stop();
			}
			Stopped?.Invoke();
		}
		catch (Exception message)
		{
			Log.Error(message);
		}
		finally
		{
			Stopped = null;
			Window.Closed -= StopImmediate;
			Window.Frame -= Update;
			listener.ClearConnectionRequestEvent();
			listener.ClearNetworkReceiveUnconnectedEvent();
			listener.ClearNetworkReceiveEvent();
			listener.ClearPeerDisconnectedEvent();
			listener.ClearDeliveryEvent();
			listener.ClearPeerConnectedEvent();
			OnRecieve = null;
			OnClientStateChanged = null;
			OnUnconnectedRecieve = null;
			OnBroadcastRecieve = null;
			CurrentStage = Stage.NotConnected;
		}
	}

	public void Update()
	{
		SubsystemInventories.flushSyncItems();
		packetFloodCounters.Clear();
		IClient[] isChecked = clients.Values.ToArray();
		for (int i = 0; i < isChecked.Length; i++)
		{
			Client client = isChecked[i] as Client;
			if (client != null)
			{
				int num = 0;
				while (client.IncomingPackages.Count > 0 && num < 50)
				{
					Package package = client.IncomingPackages.Dequeue();
					ProcessIncomingPackage(package, client, client.Peer);
					num++;
				}
			}
		}
		foreach (KeyValuePair<IClient, string> item in clientsToRemove)
		{
			RemoveClientImmediate(item.Key, item.Value);
		}
		clientsToRemove.Clear();
		if (IsServer && Time.PeriodicEvent(1.0, 0.0))
		{
			IClient[] array = clients.Values.ToArray();
			foreach (IClient client2 in array)
			{
				if (client2 != Self && client2.Peer != null && client2.Peer.Ping > 150)
				{
					LogNetworkIssue($"High Ping: Client {client2.ID} has {client2.Peer.Ping}ms");
				}
			}
		}
		if (PendingPackages.Count > 0 && Time.PeriodicEvent(0.05, 0.0))
		{
			List<IPackage> list;
			lock (PendingPackages)
			{
				list = new List<IPackage>(PendingPackages);
				PendingPackages.Clear();
			}
			IClient[] array = clients.Values.ToArray();
			foreach (IClient client3 in array)
			{
				if (client3.IsConnected)
				{
					PackageStreamWriter packageStreamWriter = new PackageStreamWriter();
					packageStreamWriter.IsServer = IsServer;
					packageStreamWriter.Project = ProjectNet.project;
					bool flag = false;
					int num2 = 0;
					Dictionary<string, int> dictionary = new Dictionary<string, int>();
					Vector3? clientWorldPosition = GetClientWorldPosition(client3);
					float num3 = (float)SettingsManager.VisibilityRange + 16f;
					float num4 = num3 * num3;
					foreach (IPackage item2 in list)
					{
						if ((item2.To != null && item2.To != client3) || (item2.Except != null && item2.Except == client3) || (client3.State < item2.MinNeedState && CommonLib.WorkType != WorkType.Client))
						{
							continue;
						}
						bool flag2 = true;
						if (client3 != Server && clientWorldPosition.HasValue && item2 is ISpatialPackage spatialPackage)
						{
							float num5 = clientWorldPosition.Value.X - spatialPackage.SpatialPosition.X;
							float num6 = clientWorldPosition.Value.Z - spatialPackage.SpatialPosition.Z;
							if (num5 * num5 + num6 * num6 > num4)
							{
								flag2 = false;
							}
						}
						if (flag2)
						{
							packageStreamWriter.Write(item2.ID);
							item2.WriteData(packageStreamWriter);
							flag = true;
							num2++;
							string key = item2.GetType().Name.Replace("Package", "");
							if (!dictionary.ContainsKey(key))
							{
								dictionary[key] = 0;
							}
							dictionary[key]++;
						}
					}
					if (flag)
					{
						Package package2 = new Package(client3, PackageFlag.Package);
						byte[] array2 = packageStreamWriter.Data();
						if (num2 > 50 || array2.Length > 30000)
						{
							string text = string.Join(", ", dictionary.Select((KeyValuePair<string, int> kv) =>
							{
								KeyValuePair<string, int> keyValuePair = kv;
								object key2 = keyValuePair.Key;
								keyValuePair = kv;
								return $"{key2}x{keyValuePair.Value}";
							}));
							LogNetworkIssue($"Spam -> C{client3.ID}: {num2}pkgs ({array2.Length}b) | {text}");
						}
						package2.Writer.PutBytesWithLength(array2, 0, array2.Length);
						Send(package2, client3);
						sec_length += array2.Length;
					}
				}
				PendingPackages.Clear();
			}
		}
		if (isStopping)
		{
			StopImmediate();
			return;
		}
		netManager.PollEvents();
		if (broadcastNetManager.IsRunning)
		{
			broadcastNetManager.PollEvents();
		}
	}

	static NetNode()
	{
		CommunityNickName = "";
	}

	private void PutByte(NetDataWriter writer, int value)
	{
		byte value2 = (byte)value;
		writer.Put(value2);
	}

	public static void EnsureNetworkLogOpened()
	{
		try
		{
			if (m_networkStream == null)
			{
				string path = Storage.CombinePaths("app:/Bugs", "NetworkLag.log");
				Storage.CreateDirectory("app:/Bugs");
				m_networkStream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
				m_networkStream.Position = m_networkStream.Length;
				m_networkWriter = new StreamWriter(m_networkStream);
				LogNetworkIssue("=== Network Log Started ===");
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error creating NetworkLag.log. Reason: " + ex.Message);
		}
	}

	public static void LogNetworkIssue(string message)
	{
		try
		{
			if (m_networkWriter != null)
			{
				string arg = DateTime.Now.ToString("HH:mm:ss.fff");
				m_networkWriter.WriteLine($"[{arg}] {message}");
				m_networkWriter.Flush();
			}
		}
		catch
		{
		}
	}

	private Vector3? GetClientWorldPosition(IClient client)
	{
		if (ProjectNet.project == null)
		{
			return null;
		}
		SubsystemPlayers subsystemPlayers = ProjectNet.project.FindSubsystem<SubsystemPlayers>();
		if (subsystemPlayers == null)
		{
			return null;
		}
		PlayerData playerData = subsystemPlayers.FindPlayerData((PlayerData p) => p.Client == client || (p.ClientId.HasValue && p.ClientId.Value == client.ID));
		if (playerData != null && playerData.ComponentPlayer != null && playerData.ComponentPlayer.ComponentBody != null)
		{
			return playerData.ComponentPlayer.ComponentBody.Position;
		}
		return null;
	}

	private void ProcessIncomingPackage(Package package, IClient client, NetPeer peer)
	{
		try
		{
			if (!IsServer && peer.Id != Server.Peer.Id)
			{
				return;
			}
			if (IsServer)
			{
				if (package.Flag == PackageFlag.ClientState)
				{
					ClientState clientState = (ClientState)package.Reader.GetByte();
					if (client != null && client.State != clientState)
					{
						client.State = clientState;
						OnClientStateChanged?.Invoke(client);
					}
				}
				else
				{
					OnRecieve?.Invoke(package);
				}
				return;
			}
			switch (package.Flag)
			{
			default:
				OnRecieve?.Invoke(package);
				break;
			case PackageFlag.RemoveClient:
			{
				byte id3 = package.Reader.GetByte();
				IClient clientByID = GetClientByID(id3);
				if (clientByID != null)
				{
					RemoveClient(clientByID);
				}
				break;
			}
			case PackageFlag.AddClient:
			{
				byte id2 = package.Reader.GetByte();
				Guid guid = package.Reader.GetGuid();
				AddClient(new Client(id2, guid, ProjectNet.project));
				break;
			}
			case PackageFlag.ClientState:
			{
				byte id = package.Reader.GetByte();
				ClientState clientState2 = (ClientState)package.Reader.GetByte();
				client = GetClientByID(id);
				if (client != null && client.State != clientState2)
				{
					client.State = clientState2;
					OnClientStateChanged?.Invoke(client);
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			LogNetworkIssue(string.Format("Receive Error from Client {0}: {1}", (client != null) ? client.ID.ToString() : "?", ex.Message));
			if (client != null)
			{
				if (package != null)
				{
					RemoveClient(client, "error processing package:\n" + ex.ToString());
				}
				else
				{
					RemoveClient(client, ex.ToString());
				}
			}
		}
	}
}
