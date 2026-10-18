using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentSmartChaseBehavior : ComponentBehavior, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemSky m_subsystemSky;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemTime m_subsystemTime;

	public SubsystemNoise m_subsystemNoise;

	public ComponentCreature m_componentCreature;

	public ComponentPathfinding m_componentPathfinding;

	public ComponentMiner m_componentMiner;

	public ComponentRandomFeedBehavior m_componentFeedBehavior;

	public ComponentCreatureModel m_componentCreatureModel;

	public DynamicArray<ComponentBody> m_componentBodies = new DynamicArray<ComponentBody>();

	public Random m_random = new Random();

	public float m_dayChaseRange;

	public float m_nightChaseRange;

	public float m_dayChaseTime;

	public float m_nightChaseTime;

	public float m_chaseNonPlayerProbability;

	public float m_chaseWhenAttackedProbability;

	public float m_chaseOnTouchProbability;

	public CreatureCategory m_autoChaseMask;

	public float m_importanceLevel;

	public float m_targetUnsuitableTime;

	public float m_targetInRangeTime;

	public double m_nextUpdateTime;

	public ComponentCreature m_target;

	public float m_dt;

	public float m_range;

	public float m_chaseTime;

	public bool m_isPersistent;

	public float m_autoChaseSuppressionTime;

	private bool m_isAttack;

	private bool m_canOpenDoors;

	private double m_lastDoorOpenTime;

	private bool m_canKnockWindows;

	private double m_lastKnockTime;

	public SubsystemTerrain m_subsystemTerrain;

	private bool m_canPickItems;

	private SubsystemPickables m_subsystemPickables;

	private SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

	private bool m_hasDroppedItemsOnDeath;

	private SubsystemProjectiles m_subsystemProjectiles;

	private double m_lastThrowTime;

	public ComponentClothing m_componentClothing;

	private bool m_isSneaking;

	private float m_sneakTimer;

	public ComponentCreature Target => m_target;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public bool IsAttack
	{
		get
		{
			return m_isAttack;
		}
		set
		{
			if (m_isAttack != value)
			{
				m_isAttack = value;
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, m_isAttack, m_isSneaking));
				}
			}
		}
	}

	public int PickedItemType { get; set; } = -1;

	public int PickedItemCount { get; set; }

	public bool IsSneaking
	{
		get
		{
			return m_isSneaking;
		}
		set
		{
			if (m_isSneaking != value)
			{
				m_isSneaking = value;
				if (CommonLib.WorkType == WorkType.Server)
				{
					CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, m_isAttack, m_isSneaking));
				}
			}
		}
	}

	public void Attack(ComponentCreature componentCreature, float maxRange, float maxChaseTime, bool isPersistent)
	{
		m_target = componentCreature;
		m_nextUpdateTime = 0.0;
		m_range = maxRange;
		m_chaseTime = maxChaseTime;
		m_isPersistent = isPersistent;
		m_importanceLevel = 200f;
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			m_componentCreatureModel.AttackOrder = IsAttack;
			m_componentCreature.ComponentBody.IsSneaking = IsSneaking;
			return;
		}
		if (m_componentCreature.ComponentHealth.Health <= 0f)
		{
			if (m_hasDroppedItemsOnDeath)
			{
				return;
			}
			if (m_componentClothing != null)
			{
				m_componentClothing.DropAllItems(m_componentCreature.ComponentBody.Position);
			}
			if (PickedItemCount > 0 && PickedItemType != -1)
			{
				for (int i = 0; i < PickedItemCount; i++)
				{
					Vector3 position = m_componentCreature.ComponentBody.Position + new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f));
					m_subsystemPickables.AddPickable(PickedItemType, 1, position, null, null);
				}
				PickedItemType = -1;
				PickedItemCount = 0;
			}
			m_hasDroppedItemsOnDeath = true;
			return;
		}
		m_autoChaseSuppressionTime -= dt;
		if (IsActive && m_target != null)
		{
			int x = Terrain.ToCell(m_target.ComponentBody.Position.X);
			int y = Terrain.ToCell(m_target.ComponentBody.Position.Y);
			int z = Terrain.ToCell(m_target.ComponentBody.Position.Z);
			int x2 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.X);
			int y2 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Y);
			int z2 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Z);
			if (m_subsystemMetersBlockBehavior.IsRoomClosed(x, y, z) && !m_subsystemMetersBlockBehavior.IsRoomClosed(x2, y2, z2))
			{
				m_stateMachine.TransitionTo("RandomMoving");
			}
			else
			{
				Vector3 position2 = m_componentCreature.ComponentBody.Position;
				Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
				Vector3 vector = position2 + forward * 0.8f;
				int y3 = Terrain.ToCell(position2.Y + m_componentCreature.ComponentBody.BoxSize.Y);
				int y4 = Terrain.ToCell(position2.Y + m_componentCreature.ComponentBody.BoxSize.Y);
				int y5 = Terrain.ToCell(position2.Y + 0.1f);
				int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(position2.X), y3, Terrain.ToCell(position2.Z));
				int cellContents2 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(vector.X), y4, Terrain.ToCell(vector.Z));
				int cellContents3 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(vector.X), y5, Terrain.ToCell(vector.Z));
				bool isCollidable = BlocksManager.Blocks[cellContents].IsCollidable;
				bool isCollidable2 = BlocksManager.Blocks[cellContents2].IsCollidable;
				bool isCollidable3 = BlocksManager.Blocks[cellContents3].IsCollidable;
				m_sneakTimer -= dt;
				bool flag = isCollidable || (isCollidable2 && !isCollidable3);
				if (IsSneaking != flag && m_sneakTimer <= 0f)
				{
					IsSneaking = flag;
					m_sneakTimer = 0.5f;
				}
				m_componentCreature.ComponentBody.IsSneaking = IsSneaking;
				m_chaseTime -= dt;
				m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
				TryThrowItem();
				if (IsTargetInAttackRange(m_target.ComponentBody))
				{
					m_componentCreatureModel.AttackOrder = true;
					IsAttack = true;
				}
				else
				{
					IsAttack = false;
				}
				if (m_componentCreatureModel.IsAttackHitMoment)
				{
					ComponentBody hitBody = GetHitBody(m_target.ComponentBody, out var hitPoint);
					if (hitBody != null)
					{
						float x3 = (m_isPersistent ? m_random.Float(8f, 10f) : 2f);
						m_chaseTime = MathUtils.Max(m_chaseTime, x3);
						m_componentMiner.Hit(hitBody, hitPoint, m_componentCreature.ComponentBody.Matrix.Forward);
						m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
					}
				}
				TryOpenDoor();
				TryKnockWindows();
			}
		}
		else
		{
			Pickable pickable = null;
			float num = float.MaxValue;
			Vector3 position3 = m_componentCreature.ComponentBody.Position;
			foreach (Pickable pickable2 in m_subsystemPickables.Pickables)
			{
				float num2 = Vector3.Distance(position3, pickable2.Position);
				if (num2 < num && !pickable2.IsSpecialPickable && !pickable2.IsDeletePickable)
				{
					num = num2;
					pickable = pickable2;
				}
			}
			if (pickable != null && num < 10f && PickedItemCount < 20)
			{
				int num3 = Terrain.ExtractContents(PickedItemType);
				bool flag2 = false;
				bool flag3 = false;
				if (PickedItemType != -1)
				{
					flag2 = !BlocksManager.Blocks[num3].IsPlaceable_(PickedItemType);
					flag3 = BlocksManager.Blocks[num3].GetMeleePower(PickedItemType) >= 3f;
				}
				if (!flag3 && !flag2 && PickedItemCount < 20)
				{
					m_componentPathfinding.SetDestination(pickable.Position, 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
				}
			}
		}
		if (m_subsystemTime.GameTime >= m_nextUpdateTime)
		{
			m_dt = m_random.Float(0.25f, 0.35f) + MathUtils.Min((float)(m_subsystemTime.GameTime - m_nextUpdateTime), 0.1f);
			m_nextUpdateTime = m_subsystemTime.GameTime + (double)m_dt;
			m_stateMachine.Update();
		}
		if (m_random.Float(0f, 1f) < 0.5f * dt)
		{
			TryPickItem();
		}
		if (m_random.Float(0f, 1f) < 1f * dt)
		{
			PlacePickedItem();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_componentMiner = Entity.FindComponent<ComponentMiner>(throwOnError: true);
		m_componentFeedBehavior = Entity.FindComponent<ComponentRandomFeedBehavior>();
		m_componentCreatureModel = Entity.FindComponent<ComponentCreatureModel>(throwOnError: true);
		m_componentClothing = Entity.FindComponent<ComponentClothing>(throwOnError: false);
		m_dayChaseRange = valuesDictionary.GetValue<float>("DayChaseRange");
		m_canOpenDoors = valuesDictionary.GetValue<bool>("CanOpenDoors");
		m_canKnockWindows = valuesDictionary.GetValue<bool>("CanKnockWindows");
		m_canPickItems = valuesDictionary.GetValue<bool>("CanPickItems");
		m_nightChaseRange = valuesDictionary.GetValue<float>("NightChaseRange");
		m_dayChaseTime = valuesDictionary.GetValue<float>("DayChaseTime");
		m_nightChaseTime = valuesDictionary.GetValue<float>("NightChaseTime");
		m_autoChaseMask = valuesDictionary.GetValue<CreatureCategory>("AutoChaseMask");
		m_chaseNonPlayerProbability = valuesDictionary.GetValue<float>("ChaseNonPlayerProbability");
		m_chaseWhenAttackedProbability = valuesDictionary.GetValue<float>("ChaseWhenAttackedProbability");
		m_chaseOnTouchProbability = valuesDictionary.GetValue<float>("ChaseOnTouchProbability");
		m_componentCreature.ComponentHealth.Attacked += (ComponentCreature attacker) =>
		{
			if (m_random.Float(0f, 1f) < m_chaseWhenAttackedProbability)
			{
				if (m_chaseWhenAttackedProbability >= 1f)
				{
					Attack(attacker, 30f, 60f, isPersistent: true);
				}
				else
				{
					Attack(attacker, 7f, 7f, isPersistent: false);
				}
			}
		};
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		componentBody.CollidedWithBody = (Action<ComponentBody>)Delegate.Combine(componentBody.CollidedWithBody, (Action<ComponentBody>)((ComponentBody body) =>
		{
			if (m_target == null && m_autoChaseSuppressionTime <= 0f && m_random.Float(0f, 1f) < m_chaseOnTouchProbability)
			{
				ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					bool flag = m_subsystemPlayers.IsPlayer(body.Entity);
					bool flag2 = (componentCreature.Category & m_autoChaseMask) > (CreatureCategory)0;
					if ((flag && m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless) || (!flag & flag2))
					{
						Attack(componentCreature, 7f, 7f, isPersistent: false);
					}
				}
			}
			if (m_target != null && body == m_target.ComponentBody && body.StandingOnBody == m_componentCreature.ComponentBody)
			{
				m_componentCreature.ComponentLocomotion.JumpOrder = 1f;
			}
		}));
		m_stateMachine.AddState("LookingForTarget", () =>
		{
			m_importanceLevel = 0f;
			m_target = null;
		}, () =>
		{
			if (IsActive)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
			else if (m_autoChaseSuppressionTime <= 0f && (m_target == null || ScoreTarget(m_target) <= 0f) && m_componentCreature.ComponentHealth.Health > 0.4f)
			{
				m_range = ((m_subsystemSky.SkyLightIntensity < 0.2f) ? m_nightChaseRange : m_dayChaseRange);
				ComponentCreature componentCreature = FindTarget();
				if (componentCreature != null)
				{
					m_targetInRangeTime += m_dt;
				}
				else
				{
					m_targetInRangeTime = 0f;
				}
				if (m_targetInRangeTime > 3f)
				{
					bool flag = m_subsystemSky.SkyLightIntensity >= 0.1f;
					float maxRange = (flag ? (m_dayChaseRange + 6f) : (m_nightChaseRange + 6f));
					float maxChaseTime = (flag ? (m_dayChaseTime * m_random.Float(0.75f, 1f)) : (m_nightChaseTime * m_random.Float(0.75f, 1f)));
					Attack(componentCreature, maxRange, maxChaseTime, !flag);
				}
			}
		}, null);
		m_stateMachine.AddState("RandomMoving", () =>
		{
			Pickable pickable = null;
			float num = float.MaxValue;
			Vector3 position = m_componentCreature.ComponentBody.Position;
			foreach (Pickable pickable2 in m_subsystemPickables.Pickables)
			{
				float num2 = Vector3.Distance(position, pickable2.Position);
				if (num2 < num && !pickable2.IsSpecialPickable && !pickable2.IsDeletePickable)
				{
					num = num2;
					pickable = pickable2;
				}
			}
			if (pickable != null && num < 10f && PickedItemCount < 20)
			{
				m_componentPathfinding.SetDestination(pickable.Position, 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			}
			else
			{
				m_componentPathfinding.SetDestination(m_componentCreature.ComponentBody.Position + new Vector3(6f * m_random.Float(-1f, 1f), 0f, 6f * m_random.Float(-1f, 1f)), 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			}
		}, () =>
		{
			if (m_componentPathfinding.IsStuck || !m_componentPathfinding.Destination.HasValue)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
			if (!IsActive)
			{
				m_stateMachine.TransitionTo("LookingForTarget");
			}
		}, () =>
		{
			m_componentPathfinding.Stop();
		});
		m_stateMachine.AddState("Chasing", () =>
		{
			m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 6f);
			m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: false);
			m_nextUpdateTime = 0.0;
		}, () =>
		{
			if (!IsActive)
			{
				m_stateMachine.TransitionTo("LookingForTarget");
			}
			else if (m_chaseTime <= 0f)
			{
				m_autoChaseSuppressionTime = m_random.Float(10f, 60f);
				m_importanceLevel = 0f;
			}
			else if (m_target == null)
			{
				m_importanceLevel = 0f;
			}
			else if (m_target.ComponentHealth.Health <= 0f)
			{
				if (m_componentFeedBehavior != null)
				{
					m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + (double)m_random.Float(1f, 3f), () =>
					{
						if (m_target != null)
						{
							m_componentFeedBehavior.Feed(m_target.ComponentBody.Position);
						}
					});
				}
				m_importanceLevel = 0f;
			}
			else if (!m_isPersistent && m_componentPathfinding.IsStuck)
			{
				m_importanceLevel = 0f;
			}
			else if (m_isPersistent && m_componentPathfinding.IsStuck)
			{
				m_stateMachine.TransitionTo("RandomMoving");
			}
			else
			{
				if (ScoreTarget(m_target) <= 0f)
				{
					m_targetUnsuitableTime += m_dt;
				}
				else
				{
					m_targetUnsuitableTime = 0f;
				}
				if (m_targetUnsuitableTime > 3f)
				{
					m_importanceLevel = 0f;
				}
				else
				{
					int maxPathfindingPositions = 0;
					if (m_isPersistent)
					{
						maxPathfindingPositions = (m_subsystemTime.FixedTimeStep.HasValue ? 1500 : 500);
					}
					BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
					BoundingBox boundingBox2 = m_target.ComponentBody.BoundingBox;
					Vector3 v = 0.5f * (boundingBox.Min + boundingBox.Max);
					Vector3 vector = 0.5f * (boundingBox2.Min + boundingBox2.Max);
					float num = Vector3.Distance(v, vector);
					float num2 = ((num < 4f) ? 0.2f : 0f);
					m_componentPathfinding.SetDestination(vector + num2 * num * m_target.ComponentBody.Velocity, 1f, 1.5f, maxPathfindingPositions, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
					if (m_random.Float(0f, 1f) < 0.33f * m_dt)
					{
						m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
					}
				}
			}
		}, null);
		m_stateMachine.TransitionTo("LookingForTarget");
	}

	public ComponentCreature FindTarget()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		ComponentCreature result = null;
		float num = 0f;
		m_componentBodies.Clear();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), m_range, m_componentBodies);
		for (int i = 0; i < m_componentBodies.Count; i++)
		{
			ComponentCreature componentCreature = m_componentBodies.Array[i].Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null)
			{
				float num2 = ScoreTarget(componentCreature);
				if (num2 > num)
				{
					num = num2;
					result = componentCreature;
				}
			}
		}
		return result;
	}

	public float ScoreTarget(ComponentCreature componentCreature)
	{
		bool flag = componentCreature.Entity.FindComponent<ComponentPlayer>() != null;
		bool flag2 = m_componentCreature.Category != CreatureCategory.WaterPredator && m_componentCreature.Category != CreatureCategory.WaterOther;
		bool flag3 = componentCreature == Target || m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless;
		bool flag4 = (componentCreature.Category & m_autoChaseMask) > (CreatureCategory)0;
		bool flag5 = componentCreature == Target || (flag4 && MathUtils.Remainder(0.004999999888241291 * m_subsystemTime.GameTime + (double)((float)(GetHashCode() % 1000) / 1000f) + (double)((float)(componentCreature.GetHashCode() % 1000) / 1000f), 1.0) < (double)m_chaseNonPlayerProbability);
		if (componentCreature != m_componentCreature && ((!flag & flag5) || (flag & flag3)) && componentCreature.Entity.IsAddedToProject && componentCreature.ComponentHealth.Health > 0f && (flag2 || IsTargetInWater(componentCreature.ComponentBody)))
		{
			float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, componentCreature.ComponentBody.Position);
			if (num < m_range)
			{
				return m_range - num;
			}
		}
		return 0f;
	}

	public bool IsTargetInWater(ComponentBody target)
	{
		if (!(target.ImmersionDepth > 0f) && (target.ParentBody == null || !IsTargetInWater(target.ParentBody)))
		{
			if (target.StandingOnBody != null && target.StandingOnBody.Position.Y < target.Position.Y)
			{
				return IsTargetInWater(target.StandingOnBody);
			}
			return false;
		}
		return true;
	}

	public bool IsTargetInAttackRange(ComponentBody target)
	{
		if (IsBodyInAttackRange(target))
		{
			return true;
		}
		BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
		BoundingBox boundingBox2 = target.BoundingBox;
		Vector3 vector = 0.5f * (boundingBox.Min + boundingBox.Max);
		Vector3 vector2 = 0.5f * (boundingBox2.Min + boundingBox2.Max) - vector;
		float num = vector2.Length();
		Vector3 v = vector2 / num;
		float num2 = 0.5f * (boundingBox.Max.X - boundingBox.Min.X + boundingBox2.Max.X - boundingBox2.Min.X);
		float num3 = 0.5f * (boundingBox.Max.Y - boundingBox.Min.Y + boundingBox2.Max.Y - boundingBox2.Min.Y);
		if (MathUtils.Abs(vector2.Y) < num3 * 0.99f)
		{
			if (num < num2 + 0.99f && Vector3.Dot(v, m_componentCreature.ComponentBody.Matrix.Forward) > 0.25f)
			{
				return true;
			}
		}
		else if (num < num3 + 0.3f && MathUtils.Abs(Vector3.Dot(v, Vector3.UnitY)) > 0.8f)
		{
			return true;
		}
		if (target.ParentBody == null || !IsTargetInAttackRange(target.ParentBody))
		{
			if (target.StandingOnBody != null && target.StandingOnBody.Position.Y < target.Position.Y)
			{
				return IsTargetInAttackRange(target.StandingOnBody);
			}
			return false;
		}
		return true;
	}

	public bool IsBodyInAttackRange(ComponentBody target)
	{
		BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
		BoundingBox boundingBox2 = target.BoundingBox;
		Vector3 vector = 0.5f * (boundingBox.Min + boundingBox.Max);
		Vector3 vector2 = 0.5f * (boundingBox2.Min + boundingBox2.Max) - vector;
		float num = vector2.Length();
		Vector3 v = vector2 / num;
		float num2 = 0.5f * (boundingBox.Max.X - boundingBox.Min.X + boundingBox2.Max.X - boundingBox2.Min.X);
		float num3 = 0.5f * (boundingBox.Max.Y - boundingBox.Min.Y + boundingBox2.Max.Y - boundingBox2.Min.Y);
		if (MathUtils.Abs(vector2.Y) < num3 * 0.99f)
		{
			if (num < num2 + 0.99f && Vector3.Dot(v, m_componentCreature.ComponentBody.Matrix.Forward) > 0.25f)
			{
				return true;
			}
		}
		else if (num < num3 + 0.3f && MathUtils.Abs(Vector3.Dot(v, Vector3.UnitY)) > 0.8f)
		{
			return true;
		}
		return false;
	}

	public ComponentBody GetHitBody(ComponentBody target, out Vector3 hitPoint)
	{
		Vector3 vector = m_componentCreature.ComponentBody.BoundingBox.Center();
		Vector3 vector2 = target.BoundingBox.Center();
		Ray3 ray = new Ray3(vector, Vector3.Normalize(vector2 - vector));
		BodyRaycastResult? bodyRaycastResult = m_componentMiner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
		if (bodyRaycastResult.HasValue && bodyRaycastResult.Value.Distance < 1.75f && (bodyRaycastResult.Value.ComponentBody == target || bodyRaycastResult.Value.ComponentBody.IsChildOfBody(target) || target.IsChildOfBody(bodyRaycastResult.Value.ComponentBody) || target.StandingOnBody == bodyRaycastResult.Value.ComponentBody))
		{
			hitPoint = bodyRaycastResult.Value.HitPoint();
			return bodyRaycastResult.Value.ComponentBody;
		}
		hitPoint = default;
		return null;
	}

	private void TryOpenDoor()
	{
		if (m_componentCreature.ComponentHealth.Health <= 0f || m_subsystemTime.GameTime < m_lastDoorOpenTime + 1.0 || !m_canOpenDoors || m_target == null)
		{
			return;
		}
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 vector = Vector3.Normalize(m_target.ComponentBody.Position - position);
		SubsystemTerrain subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (j == 0 && k == 0)
					{
						continue;
					}
					Vector3 position2 = position + vector * 1f + new Vector3(j, i, k);
					int x = (int)position2.X;
					int y = (int)position2.Y;
					int z = (int)position2.Z;
					int cellValue = subsystemTerrain.Terrain.GetCellValue(x, y, z);
					int num = Terrain.ExtractContents(cellValue);
					Block block = BlocksManager.Blocks[num];
					if (block is DoorBlock)
					{
						if ((num != 57 && num != 58) || !Project.FindSubsystem<SubsystemDoorBlockBehavior>(throwOnError: true).IsDoorElectricallyConnected(x, y, z))
						{
							int data = Terrain.ExtractData(cellValue);
							bool open = DoorBlock.GetOpen(data);
							bool flag = IsDoorBlockingPath(data, vector);
							bool flag2 = open;
							if (!open)
							{
								flag2 = true;
							}
							else if (open & flag)
							{
								flag2 = false;
							}
							if (flag2 != open)
							{
								subsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(cellValue, DoorBlock.SetOpen(data, flag2)));
								string name = (flag2 ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose");
								Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound(name, 0.7f, m_random.Float(-0.1f, 0.1f), position2, 4f, autoDelay: true);
								m_lastDoorOpenTime = m_subsystemTime.GameTime;
								return;
							}
						}
					}
					else if (block is FenceGateBlock)
					{
						if (Project.FindSubsystem<SubsystemFenceGateBlockBehavior>(throwOnError: true).OpenCloseGate(x, y, z, open: true))
						{
							m_lastDoorOpenTime = m_subsystemTime.GameTime;
							return;
						}
					}
					else if (block is TrapdoorBlock && (num != 84 || !Project.FindSubsystem<SubsystemTrapdoorBlockBehavior>(throwOnError: true).IsTrapdoorElectricallyConnected(x, y, z)))
					{
						int data2 = Terrain.ExtractData(cellValue);
						bool open2 = TrapdoorBlock.GetOpen(data2);
						bool flag3 = IsTrapdoorBlockingPath(data2, vector);
						bool flag4 = open2;
						if (!open2)
						{
							flag4 = true;
						}
						else if (open2 & flag3)
						{
							flag4 = false;
						}
						if (flag4 != open2)
						{
							Project.FindSubsystem<SubsystemTrapdoorBlockBehavior>(throwOnError: true).OpenCloseTrapdoor(x, y, z, flag4);
							m_lastDoorOpenTime = m_subsystemTime.GameTime;
							return;
						}
					}
				}
			}
		}
	}

	private bool IsDoorBlockingPath(DoorBlock doorBlock, Vector3 doorPosition, int data)
	{
		Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
		int rotation = DoorBlock.GetRotation(data);
		bool rightHanded = DoorBlock.GetRightHanded(data);
		Vector3 v = Vector3.Zero;
		switch (rotation)
		{
		case 0:
			v = (rightHanded ? Vector3.UnitZ : (-Vector3.UnitZ));
			break;
		case 1:
			v = (rightHanded ? (-Vector3.UnitX) : Vector3.UnitX);
			break;
		case 2:
			v = (rightHanded ? (-Vector3.UnitZ) : Vector3.UnitZ);
			break;
		case 3:
			v = (rightHanded ? Vector3.UnitX : (-Vector3.UnitX));
			break;
		}
		return Vector3.Dot(forward, v) > 0.5f;
	}

	private void TryKnockWindows()
	{
		if (m_componentCreature.ComponentHealth.Health <= 0f || !m_canKnockWindows || m_target == null)
		{
			return;
		}
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 vector = Vector3.Normalize(m_target.ComponentBody.Position - position);
		SubsystemTerrain subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		for (int i = 0; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (j == 0 && k == 0)
					{
						continue;
					}
					Vector3 vector2 = position + vector * 1f + new Vector3(j, i, k);
					int num = Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue((int)vector2.X, (int)vector2.Y, (int)vector2.Z));
					if ((num != 353 && num != 15 && num != 44 && num != 60) || !(m_subsystemTime.GameTime >= m_lastKnockTime + (double)m_random.Int(10, 60)))
					{
						continue;
					}
					Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
					Vector3 v = vector2 - position;
					v.Y = 0f;
					if (Vector3.Dot(forward, v) > 0.9f)
					{
						Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlayRandomSound("Audio/KnockWindow", 1f, m_random.Float(-0.3f, 0.1f), vector2, 4f, autoDelay: true);
						m_lastKnockTime = m_subsystemTime.GameTime;
						if (CommonLib.WorkType == WorkType.Server)
						{
							CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, vector2));
						}
					}
				}
			}
		}
	}

	public void TryPickItem()
	{
		if (m_componentCreature.ComponentHealth.Health <= 0f || !m_canPickItems)
		{
			return;
		}
		Vector3 position = m_componentCreature.ComponentBody.Position;
		foreach (Pickable pickable in m_subsystemPickables.Pickables)
		{
			if (Vector3.Distance(position, pickable.Position) < 1.5f && !pickable.IsSpecialPickable && !pickable.IsDeletePickable)
			{
				int num = Terrain.ExtractContents(pickable.Value);
				Block block = BlocksManager.Blocks[num];
				if (block is ClothingBlock && m_componentClothing != null)
				{
					ClothingData clothingData = block.GetClothingData(Terrain.ExtractData(pickable.Value));
					List<int> list = new List<int>(m_componentClothing.GetClothes(clothingData.Slot));
					list.Add(pickable.Value);
					m_componentClothing.SetClothes(clothingData.Slot, list);
					pickable.ToRemove = true;
					Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound("Audio/PickableCollected", 1f, m_random.Float(-0.1f, 0.1f), position, 3f, autoDelay: true);
					break;
				}
				if (block.GetMeleePower(pickable.Value) >= 3f)
				{
					PickedItemType = pickable.Value;
					PickedItemCount += pickable.Count;
					pickable.ToRemove = true;
					Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound("Audio/PickableCollected", 1f, m_random.Float(-0.1f, 0.1f), position, 3f, autoDelay: true);
					break;
				}
				if (PickedItemType == -1)
				{
					PickedItemType = pickable.Value;
					PickedItemCount = pickable.Count;
					pickable.ToRemove = true;
					Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound("Audio/PickableCollected", 1f, m_random.Float(-0.1f, 0.1f), position, 3f, autoDelay: true);
					break;
				}
				if (PickedItemType == pickable.Value)
				{
					PickedItemCount += pickable.Count;
					pickable.ToRemove = true;
					Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound("Audio/PickableCollected", 1f, m_random.Float(-0.1f, 0.1f), position, 3f, autoDelay: true);
					break;
				}
			}
		}
	}

	public void PlacePickedItem()
	{
		if (m_componentCreature.ComponentHealth.Health <= 0f || !m_canPickItems || PickedItemType == -1 || PickedItemCount <= 0)
		{
			return;
		}
		SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.RestrictWorldInteraction)
		{
			return;
		}
		Vector3 position = m_componentCreature.ComponentBody.Position + new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f));
		int num = Terrain.ExtractContents(PickedItemType);
		if (BlocksManager.Blocks[num].GetMeleePower(PickedItemType) >= 3f)
		{
			return;
		}
		bool flag = num == 91 || num == 93 || num == 392;
		if (!flag && !BlocksManager.Blocks[num].IsPlaceable_(PickedItemType))
		{
			return;
		}
		int value = Terrain.MakeBlockValue(PickedItemType);
		switch (num)
		{
		case 91:
			value = Terrain.MakeBlockValue(18);
			break;
		case 93:
			value = Terrain.MakeBlockValue(92);
			break;
		case 392:
			value = Terrain.MakeBlockValue(391);
			break;
		}
		if (!BlocksManager.Blocks[Terrain.ExtractContents(value)].IsPlaceable_(value))
		{
			return;
		}
		int num2 = Terrain.ToCell(position.X);
		int num3 = Terrain.ToCell(position.Y);
		int num4 = Terrain.ToCell(position.Z);
		if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(num2, num4, out var territoriy))
		{
			ComponentPlayer componentPlayer = m_componentCreature.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null)
			{
				bool flag2 = componentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
				if (((flag2 && !territoriy.ApplyToFirend) || !flag2) && territoriy.OwnerGuid != componentPlayer.PlayerData.PlayerGUID && !territoriy.AllowPlace)
				{
					return;
				}
			}
			else if (!territoriy.AllowPlace)
			{
				return;
			}
		}
		if ((m_subsystemTerrain.Terrain.GetCellContents(num2, num3 - 1, num4) == 0 && m_subsystemTerrain.Terrain.GetCellContents(num2 - 1, num3, num4) == 0 && m_subsystemTerrain.Terrain.GetCellContents(num2 + 1, num3, num4) == 0 && m_subsystemTerrain.Terrain.GetCellContents(num2, num3, num4 - 1) == 0 && m_subsystemTerrain.Terrain.GetCellContents(num2, num3, num4 + 1) == 0) || m_subsystemTerrain.Terrain.GetCellContents(num2, num3, num4) != 0)
		{
			return;
		}
		m_subsystemTerrain.ChangeCell(num2, num3, num4, value);
		Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlayRandomSound("Audio/Impacts/Soft", 1f, m_random.Float(-0.1f, 0.1f), position, 3f, autoDelay: true);
		if (flag)
		{
			PickedItemType = 90;
			PickedItemCount = 1;
			return;
		}
		PickedItemCount--;
		if (PickedItemCount <= 0)
		{
			PickedItemType = -1;
		}
	}

	private void TryThrowItem()
	{
		if (m_target == null || PickedItemCount <= 0 || PickedItemType == -1 || m_subsystemTime.GameTime < m_lastThrowTime + 2.0)
		{
			return;
		}
		float projectileSpeed = BlocksManager.Blocks[Terrain.ExtractContents(PickedItemType)].GetProjectileSpeed(PickedItemType);
		if (projectileSpeed <= 0f)
		{
			return;
		}
		float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, m_target.ComponentBody.Position);
		if (!(num > 4f) || !(num < 15f))
		{
			return;
		}
		Vector3 eyePosition = m_componentCreature.ComponentCreatureModel.EyePosition;
		Vector3 vector = m_target.ComponentBody.BoundingBox.Center();
		if (!m_subsystemTerrain.Raycast(eyePosition, vector, useInteractionBoxes: false, skipAirBlocks: true, (int cellValue, float distance) =>
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
			return block.IsCollidable && !block.IsTransparent;
		}).HasValue)
		{
			Vector3 v = Vector3.Normalize(vector - eyePosition);
			v.Y += MathUtils.Clamp(num * 0.02f, 0f, 0.5f);
			v = Vector3.Normalize(v);
			Vector3 velocity = v * projectileSpeed;
			m_subsystemProjectiles.FireProjectile(PickedItemType, eyePosition, velocity, Vector3.Zero, m_componentCreature);
			m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
			m_lastThrowTime = m_subsystemTime.GameTime;
			PickedItemCount--;
			if (PickedItemCount <= 0)
			{
				PickedItemType = -1;
				PickedItemCount = 0;
			}
		}
	}

	private bool IsDoorBlockingPath(int data, Vector3 directionToTarget)
	{
		int num = DoorBlock.GetRotation(data);
		bool open = DoorBlock.GetOpen(data);
		bool rightHanded = DoorBlock.GetRightHanded(data);
		if (open)
		{
			num = ((!rightHanded) ? ((num + 3) % 4) : ((num + 1) % 4));
		}
		Vector3 v = Vector3.Zero;
		switch (num)
		{
		case 0:
			v = Vector3.UnitZ;
			break;
		case 1:
			v = Vector3.UnitX;
			break;
		case 2:
			v = -Vector3.UnitZ;
			break;
		case 3:
			v = -Vector3.UnitX;
			break;
		}
		return MathUtils.Abs(Vector3.Dot(directionToTarget, v)) > 0.5f;
	}

	private bool IsTrapdoorBlockingPath(int data, Vector3 directionToTarget)
	{
		bool open = TrapdoorBlock.GetOpen(data);
		int mountingFace = TrapdoorBlock.GetMountingFace(data);
		Vector3 zero = Vector3.Zero;
		if (!open)
		{
			zero = CellFace.FaceToVector3(mountingFace);
			return MathUtils.Abs(Vector3.Dot(directionToTarget, zero)) > 0.5f;
		}
		if (MathUtils.Abs(directionToTarget.Y) > 0.8f)
		{
			return false;
		}
		CellFace.FaceToVector3(mountingFace);
		if (mountingFace != 4)
		{
			return mountingFace == 5;
		}
		return true;
	}
}
