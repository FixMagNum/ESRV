using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentHealthPackage : IPackage
{
	public enum EventType
	{
		HitResult,
		Injure,
		RequestInjure,
		SyncHealth
	}

	public float m_amount;

	public ushort m_attackerId;

	public string m_cuase;

	public ushort m_targetId;

	public bool m_ignoreInvulnerability;

	public Vector3 m_position;

	public Vector3 m_velocity;

	public Color m_color;

	public string m_text;

	public float m_health;

	public EventType m_type;

	public byte ID => 14;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentHealthPackage()
	{
	}

	public ComponentHealthPackage(ComponentHealth health)
	{
		m_targetId = health.Entity.EntityId;
		m_health = health.Health;
		m_type = EventType.SyncHealth;
	}

	public ComponentHealthPackage(ComponentHealth target, ComponentCreature attacker, float amount, string cuase, bool ignoreInvulnerability = false, bool isRequest = false)
	{
		m_type = ((!isRequest) ? EventType.Injure : EventType.RequestInjure);
		m_amount = amount;
		m_attackerId = attacker?.Entity.EntityId ?? 0;
		m_health = target.Health;
		m_targetId = target.Entity.EntityId;
		m_cuase = cuase;
		m_ignoreInvulnerability = ignoreInvulnerability;
	}

	public ComponentHealthPackage(Vector3 position, Vector3 velocity, Color color, string text)
	{
		m_position = position;
		m_velocity = velocity;
		m_color = color;
		m_text = text;
		m_type = EventType.HitResult;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		switch (m_type)
		{
		case EventType.HitResult:
			writer.Write(m_position);
			writer.Write(m_velocity);
			writer.Write(m_color);
			writer.Write(m_text);
			break;
		case EventType.Injure:
		case EventType.RequestInjure:
			writer.Write(m_targetId);
			writer.Write(m_attackerId);
			writer.Write(m_health);
			writer.Write(m_ignoreInvulnerability);
			writer.Write(m_amount);
			writer.Write(m_cuase);
			break;
		case EventType.SyncHealth:
			writer.Write(m_targetId);
			writer.Write(m_health);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<EventType>();
		switch (m_type)
		{
		case EventType.HitResult:
			m_position = reader.ReadVector3();
			m_velocity = reader.ReadVector3();
			m_color = reader.ReadColor();
			m_text = reader.ReadString();
			break;
		case EventType.Injure:
		case EventType.RequestInjure:
			m_targetId = reader.ReadUInt16();
			m_attackerId = reader.ReadUInt16();
			m_health = reader.ReadSingle();
			m_ignoreInvulnerability = reader.ReadBoolean();
			m_amount = reader.ReadSingle();
			m_cuase = reader.ReadString();
			break;
		case EventType.SyncHealth:
			m_targetId = reader.ReadUInt16();
			m_health = reader.ReadSingle();
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (isServer && !IsServerRequestAllowed(projectNet))
		{
			return;
		}
		switch (m_type)
		{
		case EventType.HitResult:
		{
			HitValueParticleSystem particleSystem = new HitValueParticleSystem(m_position, m_velocity, m_color, m_text);
			float pitch = new Random().Float(-0.2f, 0.2f);
			projectNet.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
			projectNet.FindSubsystem<SubsystemAudio>().PlaySound("Audio/Swoosh", 1f, pitch, m_position, 3f, autoDelay: false);
			break;
		}
		case EventType.Injure:
			projectNet.FindEntityById(m_targetId, (Entity entity) =>
			{
				ComponentHealth health = entity.FindComponent<ComponentHealth>();
				ComponentCreature attacker = null;
				if (health != null)
				{
					if (m_attackerId == 0)
					{
						health.NetInjure(m_amount, null, m_cuase);
						if (!isServer)
						{
							health.Health = m_health;
						}
					}
					else
					{
						projectNet.FindEntityById(m_attackerId, (Entity entity2) =>
						{
							attacker = entity2.FindComponent<ComponentCreature>();
							health.NetInjure(m_amount, attacker, m_cuase);
							if (!isServer)
							{
								health.Health = m_health;
							}
						});
					}
				}
			});
			break;
		case EventType.RequestInjure:
			projectNet.FindEntityById(m_targetId, (Entity entity) =>
			{
				ComponentHealth health = entity.FindComponent<ComponentHealth>();
				if (health != null)
				{
					if (m_attackerId == 0)
					{
						health.Injure(m_amount, null, m_ignoreInvulnerability, m_cuase);
					}
					else
					{
						projectNet.FindEntityById(m_attackerId, (Entity entity2) =>
						{
							ComponentCreature attacker = entity2.FindComponent<ComponentCreature>();
							health.Injure(m_amount, attacker, m_ignoreInvulnerability, m_cuase);
						});
					}
				}
			});
			break;
		case EventType.SyncHealth:
			projectNet.FindEntityById(m_targetId, (Entity e) =>
			{
				ComponentHealth componentHealth = e.FindComponent<ComponentHealth>();
				if (componentHealth != null)
				{
					componentHealth.Health = m_health;
				}
			});
			break;
		}
	}

	private bool IsServerRequestAllowed(ProjectNet projectNet)
	{
		if (m_type == EventType.SyncHealth)
		{
			if (From != null)
			{
				ServerSecurity.Warn(From, "health sync from a client");
			}
			return false;
		}
		if (m_type != EventType.Injure && m_type != EventType.RequestInjure)
		{
			return true;
		}
		if (From == null || From.PlayerData == null || From.PlayerData.ComponentPlayer == null)
		{
			return false;
		}
		ComponentPlayer componentPlayer = From.PlayerData.ComponentPlayer;
		Entity target = null;
		projectNet.FindEntityById(m_targetId, (Entity e) =>
		{
			target = e;
		});
		if (target == null || m_amount <= 0f)
		{
			return false;
		}
		if (target == componentPlayer.Entity)
		{
			if (m_attackerId == 0)
			{
				return true;
			}
			Entity self = null;
			projectNet.FindEntityById(m_attackerId, (Entity e) =>
			{
				self = e;
			});
			if (self == componentPlayer.Entity)
			{
				return true;
			}
			ServerSecurity.Warn(From, "self damage with another attacker");
			return false;
		}
		Entity attacker = null;
		projectNet.FindEntityById(m_attackerId, (Entity e) =>
		{
			attacker = e;
		});
		if (attacker != componentPlayer.Entity)
		{
			ServerSecurity.Warn(From, "health change for another entity", "target=" + m_targetId);
			return false;
		}
		ComponentBody componentBody = target.FindComponent<ComponentBody>();
		if (componentBody != null && componentPlayer.ComponentBody != null && Vector3.Distance(componentPlayer.ComponentBody.Position, componentBody.Position) > 12f)
		{
			ServerSecurity.Warn(From, "attack out of reach", "target=" + m_targetId);
			return false;
		}
		return true;
	}
}
