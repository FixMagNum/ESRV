using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentMountPackage : IPackage
{
	public enum EventType
	{
		Mount,
		Dismount,
		MountRequest,
		DismountRequest
	}

	public ushort m_targetId;

	public ushort m_fromId;

	public EventType m_type;

	public byte ID => 17;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentMountPackage()
	{
	}

	public ComponentMountPackage(ComponentRider rider, ComponentMount mount, bool isRequest = false)
	{
		m_type = (isRequest ? EventType.MountRequest : EventType.Mount);
		m_fromId = rider.Entity.EntityId;
		m_targetId = mount.Entity.EntityId;
	}

	public ComponentMountPackage(ComponentRider rider, bool isRequest = false)
	{
		m_type = ((!isRequest) ? EventType.Dismount : EventType.DismountRequest);
		m_fromId = rider.Entity.EntityId;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		writer.Write(m_fromId);
		writer.Write(m_targetId);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<EventType>();
		m_fromId = reader.ReadUInt16();
		m_targetId = reader.ReadUInt16();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		switch (m_type)
		{
		case EventType.Dismount:
			projectNet.FindEntityById(m_fromId, (Entity entity) =>
			{
				ComponentRider componentRider = entity.FindComponent<ComponentRider>();
				componentRider?.StartNetDismounting();
				if (!isServer)
				{
					ComponentBehaviorSelector componentBehaviorSelector = componentRider.Mount.Entity.FindComponent<ComponentBehaviorSelector>();
					if (componentBehaviorSelector != null)
					{
						componentBehaviorSelector.IsDisableBehavior = true;
					}
				}
			});
			break;
		case EventType.DismountRequest:
			projectNet.FindEntityById(m_fromId, (Entity entity) =>
			{
				entity.FindComponent<ComponentRider>()?.StartDismounting();
			});
			break;
		case EventType.Mount:
			projectNet.FindEntityById(m_fromId, (Entity entity) =>
			{
				ComponentRider rider = entity.FindComponent<ComponentRider>();
				projectNet.FindEntityById(m_targetId, (Entity entity2) =>
				{
					ComponentMount componentMount = entity2.FindComponent<ComponentMount>();
					if (componentMount != null && rider != null)
					{
						rider.StartNetMounting(componentMount);
						if (!isServer)
						{
							ComponentBehaviorSelector componentBehaviorSelector = rider.Mount.Entity.FindComponent<ComponentBehaviorSelector>();
							if (componentBehaviorSelector != null)
							{
								componentBehaviorSelector.IsDisableBehavior = false;
							}
						}
					}
				});
			});
			break;
		case EventType.MountRequest:
			projectNet.FindEntityById(m_fromId, (Entity entity) =>
			{
				ComponentRider rider = entity.FindComponent<ComponentRider>();
				projectNet.FindEntityById(m_targetId, (Entity entity2) =>
				{
					ComponentMount componentMount = entity2.FindComponent<ComponentMount>();
					if (componentMount != null && rider != null)
					{
						rider.StartMounting(componentMount);
					}
				});
			});
			break;
		}
	}
}
