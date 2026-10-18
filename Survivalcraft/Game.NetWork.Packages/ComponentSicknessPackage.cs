using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentSicknessPackage : IPackage
{
	public enum EventType
	{
		SyncStat,
		NauseaEffect
	}

	public float m_sicknessDuration;

	public bool m_isNausea;

	public EventType m_eventType;

	public ushort entityId;

	public byte ID => 18;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentSicknessPackage()
	{
	}

	public ComponentSicknessPackage(ComponentSickness sickness)
	{
		entityId = sickness.Entity.EntityId;
		m_sicknessDuration = sickness.m_sicknessDuration;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(entityId);
		writer.Write(m_sicknessDuration);
	}

	public void ReadData(PackageStreamReader reader)
	{
		entityId = reader.ReadUInt16();
		m_sicknessDuration = reader.ReadSingle();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(entityId, (Entity entity) =>
		{
			ComponentSickness componentSickness = entity.FindComponent<ComponentSickness>();
			if (componentSickness != null)
			{
				componentSickness.m_sicknessDuration = m_sicknessDuration;
				if (m_sicknessDuration > 0f)
				{
					componentSickness.NauseaEffect();
				}
			}
		});
	}

	public void Cure()
	{
		if (CommonLib.WorkType != WorkType.Client && m_sicknessDuration > 0f)
		{
			m_sicknessDuration = 0f;
		}
	}
}
