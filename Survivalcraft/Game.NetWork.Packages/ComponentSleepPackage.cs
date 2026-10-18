using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentSleepPackage : IPackage
{
	public enum EventType
	{
		SleepRequest,
		Sleep,
		WakeupRequest,
		WakeUp
	}

	public ushort m_entityId;

	public string reason = "";

	public EventType m_type;

	public bool m_result;

	public bool allowManualWakeup;

	public byte ID => 21;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentSleepPackage()
	{
	}

	public ComponentSleepPackage(ComponentSleep sleep, EventType eventType, bool allow = false, bool result = false, string msg = "")
	{
		reason = msg;
		m_entityId = sleep.Entity.EntityId;
		m_type = eventType;
		allowManualWakeup = allow;
		m_result = result;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_entityId);
		writer.WriteEnum(m_type);
		writer.Write(allowManualWakeup);
		writer.Write(m_result);
		writer.Write(reason);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_entityId = reader.ReadUInt16();
		m_type = reader.ReadEnum<EventType>();
		allowManualWakeup = reader.ReadBoolean();
		m_result = reader.ReadBoolean();
		reason = reader.ReadString();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(m_entityId, (Entity e) =>
		{
			ComponentSleep componentSleep = e.FindComponent<ComponentSleep>();
			if (componentSleep != null)
			{
				if (isServer)
				{
					switch (m_type)
					{
					case EventType.SleepRequest:
					{
						if (componentSleep.CanSleep(out var msg))
						{
							componentSleep.NetSleep(allowManualWakeup);
							CommonLib.Net.QueuePackage(new ComponentSleepPackage(componentSleep, EventType.Sleep, allowManualWakeup, result: true));
						}
						else
						{
							netNode.QueuePackage(new ComponentSleepPackage(componentSleep, EventType.Sleep, allowManualWakeup, result: false, msg)
							{
								To = From
							});
						}
						break;
					}
					case EventType.WakeupRequest:
						componentSleep.NetWakeUp();
						CommonLib.Net.QueuePackage(new ComponentSleepPackage(componentSleep, EventType.WakeUp));
						break;
					}
				}
				else
				{
					switch (m_type)
					{
					case EventType.Sleep:
						if (m_result)
						{
							componentSleep.NetSleep(allowManualWakeup);
						}
						else
						{
							ComponentPlayer componentPlayer = componentSleep.Entity.FindComponent<ComponentPlayer>();
							if (componentPlayer != null && componentPlayer.ComponentGui != null)
							{
								componentPlayer.ComponentGui.DisplaySmallMessage(reason, Color.White, blinking: false, playNotificationSound: true);
							}
						}
						break;
					case EventType.WakeUp:
						componentSleep.NetWakeUp();
						break;
					}
				}
			}
		});
	}
}
