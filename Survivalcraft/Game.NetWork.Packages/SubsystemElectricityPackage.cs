using System.Collections.Generic;
using Engine;

namespace Game.NetWork.Packages;

public class SubsystemElectricityPackage : IPackage
{
	public SubsystemElectricity m_subsystem;

	public List<SubsystemElectricity.NetSimulate> NetSimulates = new List<SubsystemElectricity.NetSimulate>();

	public byte ID => 15;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public SubsystemElectricityPackage()
	{
	}

	public SubsystemElectricityPackage(List<SubsystemElectricity.NetSimulate> netSimulates)
	{
		NetSimulates.AddRange(netSimulates);
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write((byte)NetSimulates.Count);
		for (int i = 0; i < NetSimulates.Count; i++)
		{
			SubsystemElectricity.NetSimulate netSimulate = NetSimulates[i];
			writer.Write(netSimulate.StartStep);
			writer.Write((ushort)netSimulate.SaveData.Count);
			foreach (KeyValuePair<Point3, float> saveDatum in netSimulate.SaveData)
			{
				writer.Write(saveDatum.Key, useByteY: true);
				writer.Write(saveDatum.Value);
			}
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		byte b = reader.ReadByte();
		for (byte b2 = 0; b2 < b; b2++)
		{
			SubsystemElectricity.NetSimulate netSimulate = new SubsystemElectricity.NetSimulate();
			netSimulate.StartStep = reader.ReadInt32();
			ushort num = reader.ReadUInt16();
			for (int i = 0; i < num; i++)
			{
				netSimulate.SaveData.Add(reader.ReadPoint3(useByteY: true), reader.ReadSingle());
			}
			NetSimulates.Add(netSimulate);
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		m_subsystem = projectNet.FindSubsystem<SubsystemElectricity>();
		m_subsystem.list.AddRange(NetSimulates);
	}
}
