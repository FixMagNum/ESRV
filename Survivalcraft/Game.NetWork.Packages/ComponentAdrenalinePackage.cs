using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentAdrenalinePackage : IPackage
{
	public enum EventType : byte
	{
		SyncStat,
		Sound
	}

	public ushort entityId;

	public float m_adrenaline;

	public EventType m_eventType;

	public byte m_soundIndex;

	public byte ID => 34;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentAdrenalinePackage()
	{
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(entityId);
		writer.Write(m_adrenaline);
		writer.Write((byte)m_eventType);
		if (m_eventType == EventType.Sound)
		{
			writer.Write(m_soundIndex);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		entityId = reader.ReadUInt16();
		m_adrenaline = reader.ReadSingle();
		m_eventType = (EventType)reader.ReadByte();
		if (m_eventType == EventType.Sound)
		{
			m_soundIndex = reader.ReadByte();
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(entityId, (Entity entity) =>
		{
			ComponentAdrenaline componentAdrenaline = entity.FindComponent<ComponentAdrenaline>();
			if (componentAdrenaline != null)
			{
				componentAdrenaline.m_adrenaline = m_adrenaline;
				if (m_eventType == EventType.Sound)
				{
					componentAdrenaline.PlayAdrenalineSound(m_soundIndex);
				}
			}
		});
	}

	public ComponentAdrenalinePackage(ComponentAdrenaline comp)
	{
		entityId = comp.Entity.EntityId;
		m_adrenaline = comp.m_adrenaline;
		m_eventType = EventType.SyncStat;
	}

	public ComponentAdrenalinePackage(ComponentAdrenaline comp, EventType ev, int soundIndex)
	{
		entityId = comp.Entity.EntityId;
		m_adrenaline = comp.m_adrenaline;
		m_eventType = ev;
		m_soundIndex = (byte)MathUtils.Clamp(soundIndex, 0, 255);
	}
}
