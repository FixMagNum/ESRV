using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentFurnacePackage : IPackage
{
	public float m_fireTimeRemaining;

	public float m_smeltingProgress;

	public float m_heatLevel;

	public ushort m_entityID;

	public byte ID => 22;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentFurnacePackage()
	{
	}

	public ComponentFurnacePackage(ushort entityId, float fireTimeRemaining, float smeltingProgress, float heatLevel)
	{
		m_entityID = entityId;
		m_fireTimeRemaining = fireTimeRemaining;
		m_smeltingProgress = smeltingProgress;
		m_heatLevel = heatLevel;
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(m_entityID, (Entity e) =>
		{
			ComponentFurnace componentFurnace = e.FindComponent<ComponentFurnace>();
			if (componentFurnace != null)
			{
				componentFurnace.m_smeltingProgress = m_smeltingProgress;
				componentFurnace.m_fireTimeRemaining = m_fireTimeRemaining;
				componentFurnace.m_heatLevel = m_heatLevel;
			}
		});
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_entityID = reader.ReadUInt16();
		m_fireTimeRemaining = reader.ReadSingle();
		m_smeltingProgress = reader.ReadSingle();
		m_heatLevel = reader.ReadSingle();
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_entityID);
		writer.Write(m_fireTimeRemaining);
		writer.Write(m_smeltingProgress);
		writer.Write(m_heatLevel);
	}
}
