namespace Game.NetWork;

public enum PackageFlag : byte
{
	ClientList,
	ConnectionReject,
	DiscoveryRequest,
	ClientKnowPlayer,
	ServerInfoRequest,
	ChangeEditableRequest,
	ClientState,
	ChangeSignBlockRequest,
	ChangeDispenserRequest,
	AddFriendReuqest,
	GetNickName,
	DiscoveryReply,
	AddClient,
	RemoveClient,
	NetProject,
	ServerInfoReply,
	ChangeEditableItemData,
	ExplodeCells,
	ChangeSignBlock,
	ResetPiston,
	AddFriendReply,
	Package,
	WrenchEntityUpdate
}
