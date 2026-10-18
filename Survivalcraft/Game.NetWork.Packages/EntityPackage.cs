using System.Collections.Generic;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class EntityPackage : IPackage
{
	public enum EventType
	{
		Remove,
		LoadList,
		LoadOne
	}

	public List<Entity> m_entities;

	public EventType m_type;

	public ushort m_entityId;

	public byte ID => 13;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public EntityPackage()
	{
	}

	public EntityPackage(ushort id)
	{
		m_type = EventType.Remove;
		m_entityId = id;
	}

	public EntityPackage(Entity entity)
	{
		m_type = EventType.LoadOne;
		m_entities = new List<Entity> { entity };
	}

	public EntityPackage(List<Entity> entities)
	{
		m_type = EventType.LoadList;
		m_entities = new List<Entity>();
		m_entities.AddRange(entities);
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		switch (m_type)
		{
		case EventType.LoadList:
		case EventType.LoadOne:
			writer.WriteEntityLoadList(m_entities);
			break;
		case EventType.Remove:
			writer.Write(m_entityId);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<EventType>();
		switch (m_type)
		{
		case EventType.LoadList:
		case EventType.LoadOne:
			m_entities = reader.ReadEntityLoadList();
			break;
		case EventType.Remove:
			m_entityId = reader.ReadUInt16();
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		// Only the server creates and sends entity lists and removals. A client that sends one
		// is trying to spawn or delete entities (any id), so it is refused.
		if (isServer)
		{
			ServerSecurity.Warn(From, "entity packet from a client", "type=" + m_type);
			return;
		}
		switch (m_type)
		{
		case EventType.LoadList:
		case EventType.LoadOne:
			projectNet.AddEntities(m_entities);
			break;
		case EventType.Remove:
			projectNet.FindEntityById(m_entityId, (Entity entity) =>
			{
				projectNet.RemoveEntity(entity, disposeEntity: true);
			});
			break;
		}
	}
}
