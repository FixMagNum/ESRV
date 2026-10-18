using System.Collections.Generic;

namespace Game.NetWork.Packages;

public class SubsystemPlayersPackage : IPackage
{
	public List<ComponentPlayerPackage> componentPlayerPackageList = new List<ComponentPlayerPackage>();

	public byte ID => 28;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public void AddPackage(ComponentPlayerPackage componentPlayerPackage)
	{
		lock (componentPlayerPackageList)
		{
			componentPlayerPackageList.Add(componentPlayerPackage);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		componentPlayerPackageList.Clear();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			ComponentPlayerPackage i2 = new ComponentPlayerPackage();
			i2.ReadData(reader);
			i2.NeedHandleMainPlayer = false;
			componentPlayerPackageList.Add(i2);
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		lock (componentPlayerPackageList)
		{
			writer.Write(componentPlayerPackageList.Count);
			foreach (ComponentPlayerPackage componentPlayerPackage in componentPlayerPackageList)
			{
				componentPlayerPackage.WriteData(writer);
			}
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		foreach (ComponentPlayerPackage componentPlayerPackage in componentPlayerPackageList)
		{
			componentPlayerPackage.Handle(projectNet, netNode, isServer);
		}
	}
}
