using Engine;
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

	private bool IsServerMountAllowed()
	{
		PlayerData playerData = (From != null) ? From.PlayerData : null;
		ComponentPlayer player = (playerData != null) ? playerData.ComponentPlayer : null;
		if (player == null || player.Entity == null)
		{
			return false;
		}
		if (m_fromId != player.Entity.EntityId)
		{
			ServerSecurity.Warn(From, "mount action for another entity", "type=" + m_type + ", from=" + m_fromId + ", own=" + player.Entity.EntityId);
			return false;
		}
		return true;
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (isServer && !IsServerMountAllowed())
		{
			return;
		}
		// Clients only ask (MountRequest, DismountRequest). Mount and Dismount are sent by the server only.
		if (isServer && (m_type == EventType.Mount || m_type == EventType.Dismount))
		{
			ServerSecurity.Warn(From, "mount state set by a client", "type=" + m_type);
			return;
		}
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
						// On the server the rider must be close to the mount (same reach as an interaction).
						if (isServer && Vector3.Distance(rider.ComponentCreature.ComponentBody.Position, componentMount.ComponentBody.Position) > 2.5f)
						{
							ServerSecurity.Warn(From, "mount request out of reach", "target=" + m_targetId);
							return;
						}
						rider.StartMounting(componentMount);
					}
				});
			});
			break;
		}
	}
}
