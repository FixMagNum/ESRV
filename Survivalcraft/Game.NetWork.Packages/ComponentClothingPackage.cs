using System.IO;
using Engine;

namespace Game.NetWork.Packages;

public class ComponentClothingPackage : IPackage
{
	public enum DataType
	{
		RequestSkin,
		ReplySkin,
		WhoHas,
		WhoHasReply
	}

	public string SkinName;

	public byte[] m_skinData;

	public DataType m_type;

	public byte ID => 5;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentClothingPackage()
	{
	}

	public ComponentClothingPackage(string skinName, DataType type)
	{
		m_type = type;
		SkinName = skinName;
		if (CharacterSkinsManager.HasSkinRes(SkinName))
		{
			using (Stream stream = Storage.OpenFile(Storage.CombinePaths("app:/CharacterSkins", skinName), OpenFileMode.Read))
			{
				m_skinData = ModsManager.StreamToBytes(stream);
			}
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		switch (m_type)
		{
		case DataType.RequestSkin:
		case DataType.WhoHas:
			writer.Write(SkinName);
			break;
		case DataType.ReplySkin:
		case DataType.WhoHasReply:
			writer.Write(SkinName);
			writer.Write(m_skinData.Length);
			writer.Write(m_skinData);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<DataType>();
		switch (m_type)
		{
		case DataType.RequestSkin:
		case DataType.WhoHas:
			SkinName = reader.ReadString();
			break;
		case DataType.ReplySkin:
		case DataType.WhoHasReply:
		{
			SkinName = reader.ReadString();
			int count = reader.ReadInt32();
			m_skinData = reader.ReadBytes(count);
			break;
		}
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		switch (m_type)
		{
		case DataType.RequestSkin:
			if (CharacterSkinsManager.HasSkinRes(SkinName))
			{
				netNode.QueuePackage(new ComponentClothingPackage(SkinName, DataType.ReplySkin));
			}
			else if (!CharacterSkinsManager.waitReplyList.Contains(SkinName))
			{
				netNode.QueuePackage(new ComponentClothingPackage(SkinName, DataType.WhoHas));
				CharacterSkinsManager.waitReplyList.Add(SkinName);
			}
			break;
		case DataType.ReplySkin:
		case DataType.WhoHasReply:
			if (CharacterSkinsManager.waitReplyList.Contains(SkinName))
			{
				CharacterSkinsManager.waitReplyList.Remove(SkinName);
			}
			CharacterSkinsManager.SaveSkinToFile(SkinName, m_skinData);
			break;
		case DataType.WhoHas:
			if (CharacterSkinsManager.HasSkinRes(SkinName))
			{
				netNode.QueuePackage(new ComponentClothingPackage(SkinName, DataType.WhoHasReply));
			}
			break;
		}
	}
}
