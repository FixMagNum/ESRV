namespace Game.NetWork;

public interface IPackage
{
	byte ID { get; }

	IClient To { get; set; }

	IClient Except { get; set; }

	IClient From { get; set; }

	ClientState MinNeedState { get; }

	void WriteData(PackageStreamWriter writer);

	void ReadData(PackageStreamReader reader);

	void Handle(ProjectNet projectNet, NetNode netNode, bool isServer);
}
