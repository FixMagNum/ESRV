using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentAlcoholPackage : IPackage
{
	public enum EventType : byte
	{
		SyncStat,
		FunnySound
	}

	public ushort entityId;

	public float m_alcohol;

	public EventType m_eventType;

	public byte m_soundIndex;

	public byte ID => 32;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentAlcoholPackage()
	{
	}

	public ComponentAlcoholPackage(ComponentAlcohol alcohol)
	{
		entityId = alcohol.Entity.EntityId;
		m_alcohol = alcohol.m_alcohol;
		m_eventType = EventType.SyncStat;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(entityId);
		writer.Write(m_alcohol);
		writer.Write((byte)m_eventType);
		if (m_eventType == EventType.FunnySound)
		{
			writer.Write(m_soundIndex);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		entityId = reader.ReadUInt16();
		m_alcohol = reader.ReadSingle();
		m_eventType = (EventType)reader.ReadByte();
		if (m_eventType == EventType.FunnySound)
		{
			m_soundIndex = reader.ReadByte();
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(entityId, (Entity entity) =>
		{
			ComponentAlcohol componentAlcohol = entity.FindComponent<ComponentAlcohol>();
			if (componentAlcohol != null)
			{
				componentAlcohol.m_alcohol = m_alcohol;
				if (m_eventType == EventType.FunnySound)
				{
					componentAlcohol.PlayFunnySound(m_soundIndex);
				}
			}
		});
	}

	public ComponentAlcoholPackage(ComponentAlcohol alcohol, EventType ev, int soundIndex)
	{
		entityId = alcohol.Entity.EntityId;
		m_alcohol = alcohol.m_alcohol;
		m_eventType = ev;
		m_soundIndex = (byte)MathUtils.Clamp(soundIndex, 0, 255);
	}
}
