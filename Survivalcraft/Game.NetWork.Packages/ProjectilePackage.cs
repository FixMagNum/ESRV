using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ProjectilePackage : IPackage
{
	public enum ProjectileTailInfo : byte
	{
		None = 0,
		Smoke = 1,
		Fireworks = 2,
		IsOffsetZero = 4
	}

	public int m_value;

	public ProjectileTailInfo m_tailInfo;

	public Vector3 m_position;

	public Vector3 m_velocity;

	public Vector3 m_angularVelocity;

	public Vector3 m_trailOfffset;

	public ushort m_ownerId;

	public bool m_isFireProjectile;

	public byte m_particlesCount;

	public float m_size;

	public float m_duration;

	public Color m_color;

	public byte ID => 16;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ProjectilePackage()
	{
	}

	public ProjectilePackage(Projectile projectile)
	{
		m_value = projectile.Value;
		m_position = projectile.Position;
		m_velocity = projectile.Velocity;
		m_trailOfffset = projectile.TrailOffset;
		m_angularVelocity = projectile.AngularVelocity;
		m_ownerId = (ushort)((projectile.Owner != null) ? projectile.Owner.Entity.EntityId : 0);
		m_isFireProjectile = projectile.IsFireProjectile;
		if (projectile.TrailParticleSystem is SmokeTrailParticleSystem zipArchive)
		{
			m_particlesCount = (byte)zipArchive.Particles.Length;
			m_size = zipArchive.m_size;
			m_duration = zipArchive.m_duration;
			m_color = zipArchive.m_color;
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_tailInfo);
		writer.Write(m_value);
		writer.Write(m_position);
		writer.Write(m_velocity);
		writer.Write(m_trailOfffset);
		writer.Write(m_angularVelocity);
		writer.Write(m_ownerId);
		writer.Write(m_isFireProjectile);
		if (m_tailInfo == ProjectileTailInfo.Smoke)
		{
			writer.Write(m_particlesCount);
			writer.Write(m_size);
			writer.Write(m_duration);
			writer.Write(m_color);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_tailInfo = reader.ReadEnum<ProjectileTailInfo>();
		m_value = reader.ReadInt32();
		m_position = reader.ReadVector3();
		m_velocity = reader.ReadVector3();
		m_trailOfffset = reader.ReadVector3();
		m_angularVelocity = reader.ReadVector3();
		m_ownerId = reader.ReadUInt16();
		m_isFireProjectile = reader.ReadBoolean();
		if (m_tailInfo == ProjectileTailInfo.Smoke)
		{
			m_particlesCount = reader.ReadByte();
			m_size = reader.ReadSingle();
			m_duration = reader.ReadSingle();
			m_color = reader.ReadColor();
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		// Projectiles are created only by the server (FireProjectile/AddProjectile return null on clients),
		// so a client sending one is refused.
		if (isServer)
		{
			ServerSecurity.Warn(From, "projectile packet from a client", "owner=" + m_ownerId);
			return;
		}
		SubsystemProjectiles xElement = projectNet.FindSubsystem<SubsystemProjectiles>();
		ComponentCreature creature = null;
		projectNet.FindEntityById(m_ownerId, (Entity entity) =>
		{
			creature = entity.FindComponent<ComponentCreature>();
		});
		Projectile projectile = (m_isFireProjectile ? xElement.FireProjectileNet(m_value, m_position, m_velocity, m_angularVelocity, creature) : xElement.AddProjectileNet(m_value, m_position, m_velocity, m_angularVelocity, creature));
		if (m_tailInfo == ProjectileTailInfo.Smoke)
		{
			xElement.AddTrail(projectile, m_trailOfffset, new SmokeTrailParticleSystem(m_particlesCount, m_size, m_duration, m_color));
		}
		else if (m_tailInfo == ProjectileTailInfo.Fireworks)
		{
			xElement.AddTrail(projectile, m_trailOfffset, new FireworksTrailParticleSystem());
		}
	}
}
