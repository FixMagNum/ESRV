using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBoat : Component, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public ComponentMount m_componentMount;

	public ComponentBody m_componentBody;

	public ComponentDamage m_componentDamage;

	public float m_turnSpeed;

	public float MoveOrder { get; set; }

	public float TurnOrder { get; set; }

	public float Health { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void Injure(float amount, ComponentCreature attacker, bool ignoreInvulnerability)
	{
		if (amount > 0f)
		{
			Health = MathUtils.Max(Health - amount, 0f);
		}
	}

	public void Update(float dt)
	{
		if (m_componentDamage.Hitpoints < 0.33f)
		{
			m_componentBody.Density = 1.15f;
			if (m_componentDamage.Hitpoints - m_componentDamage.HitpointsChange >= 0.33f && m_componentBody.ImmersionFactor > 0f)
			{
				m_subsystemAudio.PlaySound("Audio/Sinking", 1f, 0f, m_componentBody.Position, 4f, autoDelay: true);
			}
		}
		else if (m_componentDamage.Hitpoints < 0.66f)
		{
			m_componentBody.Density = 0.7f;
			if (m_componentDamage.Hitpoints - m_componentDamage.HitpointsChange >= 0.66f && m_componentBody.ImmersionFactor > 0f)
			{
				m_subsystemAudio.PlaySound("Audio/Sinking", 1f, 0f, m_componentBody.Position, 4f, autoDelay: true);
			}
		}
		bool flag = m_componentBody.ImmersionFactor > 0.95f;
		object obj = !flag && m_componentBody.ImmersionFactor > 0.01f && !m_componentBody.StandingOnValue.HasValue && m_componentBody.StandingOnBody == null;
		m_turnSpeed += 2.5f * m_subsystemTime.GameTimeDelta * (1f * TurnOrder - m_turnSpeed);
		if (obj != null)
		{
			float angle = (0f - m_turnSpeed) * dt;
			m_componentBody.Rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle) * m_componentBody.Rotation);
		}
		if (obj != null && MoveOrder != 0f)
		{
			m_componentBody.Velocity += dt * 3f * MoveOrder * m_componentBody.Matrix.Forward;
		}
		if (flag)
		{
			m_componentDamage.Damage(0.005f * dt);
			if (m_componentMount.Rider != null)
			{
				m_componentMount.Rider.StartDismounting();
			}
		}
		MoveOrder = 0f;
		TurnOrder = 0f;
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_componentMount = Entity.FindComponent<ComponentMount>(throwOnError: true);
		m_componentBody = Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_componentDamage = Entity.FindComponent<ComponentDamage>(throwOnError: true);
	}
}
