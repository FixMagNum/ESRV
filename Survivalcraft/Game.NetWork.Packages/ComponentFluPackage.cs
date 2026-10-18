using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentFluPackage : IPackage
{
	public enum EventType
	{
		SyncStat,
		FluEffect,
		StartFlu,
		Sneeze
	}

	public float m_fluDuration;

	public float m_coughDuration;

	public float m_sneezeDuration;

	public float m_fluOnset;

	public Vector3 m_Impulse;

	public EventType m_eventType;

	public ushort entityId;

	public byte ID => 19;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentFluPackage()
	{
	}

	public ComponentFluPackage(ComponentFlu flu, EventType eventType)
	{
		entityId = flu.Entity.EntityId;
		m_eventType = eventType;
		m_fluOnset = flu.m_fluOnset;
		m_fluDuration = flu.m_fluDuration;
		m_coughDuration = flu.m_coughDuration;
		m_sneezeDuration = flu.m_sneezeDuration;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(entityId);
		writer.WriteEnum(m_eventType);
		writer.Write(m_fluOnset);
		writer.Write(m_fluDuration);
		writer.Write(m_coughDuration);
		writer.Write(m_sneezeDuration);
	}

	public void ReadData(PackageStreamReader reader)
	{
		entityId = reader.ReadUInt16();
		m_eventType = reader.ReadEnum<EventType>();
		m_fluOnset = reader.ReadSingle();
		m_fluDuration = reader.ReadSingle();
		m_coughDuration = reader.ReadSingle();
		m_sneezeDuration = reader.ReadSingle();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		switch (m_eventType)
		{
		case EventType.SyncStat:
			projectNet.FindEntityById(entityId, (Entity entity) =>
			{
				ComponentFlu componentFlu = entity.FindComponent<ComponentFlu>();
				if (componentFlu != null)
				{
					componentFlu.m_fluOnset = m_fluOnset;
					componentFlu.m_sneezeDuration = m_sneezeDuration;
					componentFlu.m_coughDuration = m_coughDuration;
					componentFlu.m_fluDuration = m_fluDuration;
				}
			});
			break;
		case EventType.FluEffect:
			projectNet.FindEntityById(entityId, (Entity entity) =>
			{
				ComponentFlu componentFlu = entity.FindComponent<ComponentFlu>();
				if (componentFlu != null)
				{
					componentFlu.m_fluOnset = m_fluOnset;
					componentFlu.m_sneezeDuration = m_sneezeDuration;
					componentFlu.m_coughDuration = m_coughDuration;
					componentFlu.m_fluDuration = m_fluDuration;
					componentFlu.FluEffect();
				}
			});
			break;
		case EventType.StartFlu:
			projectNet.FindEntityById(entityId, (Entity entity) =>
			{
				ComponentFlu componentFlu = entity.FindComponent<ComponentFlu>();
				if (componentFlu != null)
				{
					componentFlu.m_fluOnset = m_fluOnset;
					componentFlu.m_sneezeDuration = m_sneezeDuration;
					componentFlu.m_coughDuration = m_coughDuration;
					componentFlu.m_fluDuration = m_fluDuration;
					componentFlu.StartFlu();
				}
			});
			break;
		case EventType.Sneeze:
			projectNet.FindEntityById(entityId, (Entity entity) =>
			{
				ComponentFlu componentFlu = entity.FindComponent<ComponentFlu>();
				if (componentFlu != null)
				{
					componentFlu.m_fluOnset = m_fluOnset;
					componentFlu.m_sneezeDuration = m_sneezeDuration;
					componentFlu.m_coughDuration = m_coughDuration;
					componentFlu.m_fluDuration = m_fluDuration;
					componentFlu.Sneeze();
				}
			});
			break;
		}
	}
}
