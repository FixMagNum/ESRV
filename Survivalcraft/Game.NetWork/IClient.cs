using System;
using LiteNetLib;

namespace Game.NetWork;

public interface IClient
{
	ProjectNet ProjectNet { get; }

	NetPeer Peer { get; }

	PlayerData PlayerData { get; }

	byte ID { get; }

	Guid GUID { get; }

	bool Granted { get; }

	bool IsConnected { get; }

	ClientState State { get; set; }

	void OnGranteStateChange(bool state);

	void SetPeer(NetPeer peer);

	void UpdateKnowsPlayer(Guid playerGuid);
}
