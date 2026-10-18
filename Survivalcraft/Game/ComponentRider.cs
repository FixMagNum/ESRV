using System.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentRider : Component, IUpdateable
{
	public SubsystemBodies m_subsystemBodies;

	public SubsystemTerrain m_subsystemTerrain;

	public DynamicArray<ComponentBody> m_componentBodies = new DynamicArray<ComponentBody>();

	public Vector3 m_riderOffset;

	public float m_animationTime;

	public bool m_isAnimating;

	public bool m_isDismounting;

	public Vector3 m_targetPositionOffset;

	public Quaternion m_targetRotationOffset;

	public float m_outOfMountTime;

	public ComponentCreature ComponentCreature { get; set; }

	public ComponentMount Mount
	{
		get
		{
			if (ComponentCreature.ComponentBody.ParentBody != null)
			{
				return ComponentCreature.ComponentBody.ParentBody.Entity.FindComponent<ComponentMount>();
			}
			return null;
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public ComponentMount FindNearestMount()
	{
		Vector2 point = new Vector2(ComponentCreature.ComponentBody.Position.X, ComponentCreature.ComponentBody.Position.Z);
		m_componentBodies.Clear();
		m_subsystemBodies.FindBodiesAroundPoint(point, 2.5f, m_componentBodies);
		float num = 0f;
		ComponentMount result = null;
		foreach (ComponentMount item in from b in m_componentBodies
			select b.Entity.FindComponent<ComponentMount>() into m
			where m != null && m.Entity != Entity
			select m)
		{
			float num2 = ScoreMount(item, 2.5f);
			if (num2 > num)
			{
				num = num2;
				result = item;
			}
		}
		return result;
	}

	public void StartNetMounting(ComponentMount componentMount)
	{
		m_isAnimating = true;
		m_animationTime = 0f;
		m_isDismounting = false;
		ComponentCreature.ComponentBody.ParentBody = componentMount.ComponentBody;
		ComponentCreature.ComponentBody.ParentBodyPositionOffset = Vector3.Transform(ComponentCreature.ComponentBody.Position - componentMount.ComponentBody.Position, Quaternion.Conjugate(componentMount.ComponentBody.Rotation));
		ComponentCreature.ComponentBody.ParentBodyRotationOffset = Quaternion.Conjugate(componentMount.ComponentBody.Rotation) * ComponentCreature.ComponentBody.Rotation;
		m_targetPositionOffset = componentMount.MountOffset + m_riderOffset;
		m_targetRotationOffset = Quaternion.Identity;
		ComponentCreature.ComponentLocomotion.IsCreativeFlyEnabled = false;
	}

	public void StartMounting(ComponentMount componentMount)
	{
		if (componentMount.IsBroken)
		{
			if (ComponentCreature.ComponentBody.Player != null)
			{
				string text = LanguageControl.Get("ComponentRider", "SaddleBroken");
				Color color = new Color(255, 100, 80);
				ComponentCreature.ComponentBody.Player.ComponentGui.DisplaySmallMessage(text, color, blinking: false, playNotificationSound: true);
			}
		}
		else if (!m_isAnimating && Mount == null)
		{
			if (CommonLib.WorkType != WorkType.Client)
			{
				StartNetMounting(componentMount);
				CommonLib.Net.QueuePackage(new ComponentMountPackage(this, componentMount));
			}
			else
			{
				CommonLib.Net.QueuePackage(new ComponentMountPackage(this, componentMount, isRequest: true));
			}
		}
	}

	public void StartNetDismounting()
	{
		ComponentCreature.ComponentBody.Entity.FindComponent<ComponentLocomotion>();
		float x = 0f;
		if (Mount.DismountOffset.X > 0f)
		{
			float num = Mount.DismountOffset.X + 0.5f;
			Vector3 vector = 0.5f * (ComponentCreature.ComponentBody.BoundingBox.Min + ComponentCreature.ComponentBody.BoundingBox.Max);
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(vector, vector - num * ComponentCreature.ComponentBody.Matrix.Right, useInteractionBoxes: false, skipAirBlocks: true, null);
			TerrainRaycastResult? terrainRaycastResult2 = m_subsystemTerrain.Raycast(vector, vector + num * ComponentCreature.ComponentBody.Matrix.Right, useInteractionBoxes: false, skipAirBlocks: true, null);
			if (terrainRaycastResult.HasValue)
			{
				if (terrainRaycastResult2.HasValue)
				{
					x = ((terrainRaycastResult.Value.Distance <= terrainRaycastResult2.Value.Distance) ? MathUtils.Min(terrainRaycastResult2.Value.Distance, Mount.DismountOffset.X) : (0f - MathUtils.Min(terrainRaycastResult.Value.Distance, Mount.DismountOffset.X)));
				}
				else
				{
					x = Mount.DismountOffset.X;
				}
			}
			else
			{
				x = 0f - Mount.DismountOffset.X;
			}
		}
		m_isAnimating = true;
		m_animationTime = 0f;
		m_isDismounting = true;
		m_targetPositionOffset = Mount.MountOffset + m_riderOffset + new Vector3(x, Mount.DismountOffset.Y, Mount.DismountOffset.Z);
		m_targetRotationOffset = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathUtils.Sign(x) * MathUtils.DegToRad(60f));
	}

	public void StartDismounting()
	{
		if (!m_isAnimating && Mount != null)
		{
			if (CommonLib.WorkType != WorkType.Client)
			{
				StartNetDismounting();
				CommonLib.Net.QueuePackage(new ComponentMountPackage(this));
			}
			else
			{
				CommonLib.Net.QueuePackage(new ComponentMountPackage(this, isRequest: true));
			}
		}
	}

	public void Update(float dt)
	{
		if (!m_isAnimating)
		{
			return;
		}
		float f = 8f * dt;
		ComponentBody componentBody = ComponentCreature.ComponentBody;
		componentBody.ParentBodyPositionOffset = Vector3.Lerp(componentBody.ParentBodyPositionOffset, m_targetPositionOffset, f);
		componentBody.ParentBodyRotationOffset = Quaternion.Slerp(componentBody.ParentBodyRotationOffset, m_targetRotationOffset, f);
		m_animationTime += dt;
		if (Vector3.DistanceSquared(componentBody.ParentBodyPositionOffset, m_targetPositionOffset) < 0.010000001f || m_animationTime > 0.75f)
		{
			m_isAnimating = false;
			if (m_isDismounting)
			{
				if (componentBody.ParentBody != null)
				{
					componentBody.Velocity = componentBody.ParentBody.Velocity;
					componentBody.ParentBody = null;
				}
			}
			else
			{
				componentBody.ParentBodyPositionOffset = m_targetPositionOffset;
				componentBody.ParentBodyRotationOffset = m_targetRotationOffset;
				m_outOfMountTime = 0f;
			}
		}
		ComponentMount mount = Mount;
		if (mount == null || m_isAnimating)
		{
			return;
		}
		ComponentBody componentBody2 = ComponentCreature.ComponentBody;
		ComponentBody parentBody = ComponentCreature.ComponentBody.ParentBody;
		float num = Vector3.DistanceSquared(parentBody.Position + Vector3.Transform(componentBody2.ParentBodyPositionOffset, parentBody.Rotation), componentBody2.Position);
		if (CommonLib.WorkType == WorkType.Local || ComponentCreature.ComponentBody.Player == null || ComponentCreature.ComponentBody.Player.PlayerData.IsMainPlayer)
		{
			if (num > 0.16000001f)
			{
				m_outOfMountTime += dt;
			}
			else
			{
				m_outOfMountTime = 0f;
			}
			ComponentHealth componentHealth = mount.Entity.FindComponent<ComponentHealth>();
			if (m_outOfMountTime > 0.1f || (componentHealth != null && componentHealth.Health <= 0f) || ComponentCreature.ComponentHealth.Health <= 0f)
			{
				StartDismounting();
			}
			ComponentCreature.ComponentBody.ParentBodyPositionOffset = mount.MountOffset + m_riderOffset;
			ComponentCreature.ComponentBody.ParentBodyRotationOffset = Quaternion.Identity;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		ComponentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_riderOffset = valuesDictionary.GetValue<Vector3>("RiderOffset");
	}

	public float ScoreMount(ComponentMount componentMount, float maxDistance)
	{
		if (componentMount.ComponentBody.Velocity.LengthSquared() < 1f)
		{
			Vector3 v = componentMount.ComponentBody.Position + Vector3.Transform(componentMount.MountOffset, componentMount.ComponentBody.Rotation) - ComponentCreature.ComponentCreatureModel.EyePosition;
			if (v.Length() < maxDistance)
			{
				Vector3 forward = Matrix.CreateFromQuaternion(ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
				if (Vector3.Dot(Vector3.Normalize(v), forward) > 0.33f)
				{
					return maxDistance - v.Length();
				}
			}
		}
		return 0f;
	}
}
