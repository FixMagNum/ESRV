using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentFermentationBarrelPackage : IPackage
{
	public float m_progressSeconds;

	public int m_waterLayers;

	public byte m_resultDrink;

	public ushort m_entityID;

	public byte ID => 33;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentFermentationBarrelPackage()
	{
	}

	public ComponentFermentationBarrelPackage(ushort entityId, float progressSeconds, int waterLayers, byte resultDrink)
	{
		m_entityID = entityId;
		m_progressSeconds = progressSeconds;
		m_waterLayers = waterLayers;
		m_resultDrink = resultDrink;
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(m_entityID, (Entity e) =>
		{
			e.FindComponent<ComponentFermentationBarrel>()?.NetSync(m_progressSeconds, m_waterLayers, (ComponentFermentationBarrel.DrinkType)m_resultDrink);
		});
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_entityID = reader.ReadUInt16();
		m_progressSeconds = reader.ReadSingle();
		m_waterLayers = reader.ReadInt32();
		m_resultDrink = reader.ReadByte();
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_entityID);
		writer.Write(m_progressSeconds);
		writer.Write(m_waterLayers);
		writer.Write(m_resultDrink);
	}
}
