using System;
using System.Collections.Generic;
using LiteNetLib;

namespace Game.NetWork;

public class Client : IClient
{
	private byte m_id;

	private Guid m_guid;

	private bool m_granted;

	private ProjectNet m_project;

	private HashSet<Guid> m_recievedPlayers = new HashSet<Guid>();

	private NetPeer m_peer;

	public byte ID => m_id;

	public Guid GUID => m_guid;

	public bool Granted => m_granted;

	public bool IsConnected
	{
		get
		{
			if (m_peer != null)
			{
				return m_peer.ConnectionState != ConnectionState.Disconnected;
			}
			return false;
		}
	}

	public NetPeer Peer => m_peer;

	public ProjectNet ProjectNet => m_project;

	public ClientState State { get; set; }

	public PlayerData PlayerData => m_project.FindSubsystem<SubsystemPlayers>().PlayersData.Find((PlayerData w) => w.PlayerGUID == GUID);

	public Queue<Package> IncomingPackages { get; private set; } = new Queue<Package>();

	public Client(byte id, Guid guid, ProjectNet projectNet)
	{
		m_id = id;
		m_guid = guid;
		m_project = projectNet;
	}

	public void OnGranteStateChange(bool state)
	{
		m_granted = state;
	}

	public void SetPeer(NetPeer peer)
	{
		m_peer = peer;
	}

	public override bool Equals(object obj)
	{
		Client path = obj as Client;
		if (path != null)
		{
			return path.ID == ID;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return GUID.GetHashCode();
	}

	public static bool operator ==(Client a, Client b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return a.ID == b.ID;
	}

	public static bool operator !=(Client a, Client b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		return $"Client : {{ID : {ID}, GUID : {GUID}}}";
	}

	public void UpdateKnowsPlayer(Guid guid)
	{
		m_recievedPlayers.Add(guid);
	}

	public bool KnowsPlayer(Guid guid)
	{
		return m_recievedPlayers.Contains(guid);
	}
}
