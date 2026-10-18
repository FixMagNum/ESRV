using System;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCircularChaseBehavior : ComponentBehavior, IUpdateable
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

	public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentHerdBehavior m_componentHerd;

	private float m_circlingAngle;

	private int m_circlingDirection;

	private bool m_usedScanner;

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
					CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this, m_isAttack));
				}
			}
		}
	}

	public void Attack(ComponentCreature componentCreature, float maxRange, float maxChaseTime, bool isPersistent)
	{
		if (componentCreature == null)
		{
			return;
		}
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			if ((m_subsystemPlayers == null || !m_subsystemPlayers.IsPlayer(componentCreature.Entity)) && componentCreature.Entity.FindComponent<ComponentPlayer>() == null)
			{
				return;
			}
			maxRange = 40f;
			maxChaseTime = float.MaxValue;
			isPersistent = true;
		}
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
			return;
		}
		m_autoChaseSuppressionTime -= dt;
		if (IsActive && m_target != null)
		{
			m_chaseTime -= dt;
			m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
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
					float x = (m_isPersistent ? m_random.Float(8f, 10f) : 2f);
					m_chaseTime = MathUtils.Max(m_chaseTime, x);
					m_componentMiner.Hit(hitBody, hitPoint, m_componentCreature.ComponentBody.Matrix.Forward);
					m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
				}
			}
		}
		if (m_subsystemTime.GameTime >= m_nextUpdateTime)
		{
			m_dt = m_random.Float(0.25f, 0.45f) + MathUtils.Min((float)(m_subsystemTime.GameTime - m_nextUpdateTime), 0.1f);
			m_nextUpdateTime = m_subsystemTime.GameTime + (double)m_dt;
			m_stateMachine.Update();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_componentHerd = Entity.FindComponent<ComponentHerdBehavior>();
		m_circlingDirection = (m_random.Bool(0.5f) ? 1 : (-1));
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
		m_componentMiner = Entity.FindComponent<ComponentMiner>(throwOnError: true);
		m_componentFeedBehavior = Entity.FindComponent<ComponentRandomFeedBehavior>();
		m_componentCreatureModel = Entity.FindComponent<ComponentCreatureModel>(throwOnError: true);
		m_dayChaseRange = valuesDictionary.GetValue<float>("DayChaseRange");
		m_nightChaseRange = valuesDictionary.GetValue<float>("NightChaseRange");
		m_dayChaseTime = valuesDictionary.GetValue<float>("DayChaseTime");
		m_nightChaseTime = valuesDictionary.GetValue<float>("NightChaseTime");
		m_autoChaseMask = valuesDictionary.GetValue<CreatureCategory>("AutoChaseMask");
		m_chaseNonPlayerProbability = valuesDictionary.GetValue<float>("ChaseNonPlayerProbability");
		m_chaseWhenAttackedProbability = valuesDictionary.GetValue<float>("ChaseWhenAttackedProbability");
		m_chaseOnTouchProbability = valuesDictionary.GetValue<float>("ChaseOnTouchProbability");
		m_componentCreature.ComponentHealth.Attacked += (ComponentCreature attacker) =>
		{
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				if (attacker != null && attacker.Entity != null && ((m_subsystemPlayers != null && m_subsystemPlayers.IsPlayer(attacker.Entity)) || attacker.Entity.FindComponent<ComponentPlayer>() != null))
				{
					Attack(attacker, 30f, 60f, isPersistent: true);
				}
			}
			else if (m_random.Float(0f, 1f) < m_chaseWhenAttackedProbability)
			{
				float maxRange = ((m_chaseWhenAttackedProbability >= 1f) ? 30f : 7f);
				float maxChaseTime = ((m_chaseWhenAttackedProbability >= 1f) ? 60f : 7f);
				bool isPersistent = m_chaseWhenAttackedProbability >= 1f;
				Attack(attacker, maxRange, maxChaseTime, isPersistent);
				m_componentBodies.Clear();
				m_subsystemBodies.FindBodiesAroundPoint(new Vector2(m_componentCreature.ComponentBody.Position.X, m_componentCreature.ComponentBody.Position.Z), 15f, m_componentBodies);
				for (int i = 0; i < m_componentBodies.Count; i++)
				{
					ComponentCreature componentCreature = m_componentBodies.Array[i].Entity.FindComponent<ComponentCreature>();
					if (componentCreature != null && componentCreature != m_componentCreature && componentCreature.Category == m_componentCreature.Category)
					{
						componentCreature.Entity.FindComponent<ComponentCircularChaseBehavior>()?.Attack(attacker, maxRange, maxChaseTime, isPersistent);
					}
				}
			}
		};
		m_componentCreature.ComponentBody.CollidedWithBody = (Action<ComponentBody>)Delegate.Combine(m_componentCreature.ComponentBody.CollidedWithBody, (Action<ComponentBody>)((ComponentBody body) =>
		{
			bool flag = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
			if (m_target == null && (flag || m_autoChaseSuppressionTime <= 0f) && (flag || m_random.Float(0f, 1f) < m_chaseOnTouchProbability))
			{
				ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					bool flag2 = (m_subsystemPlayers != null && m_subsystemPlayers.IsPlayer(body.Entity)) || body.Entity.FindComponent<ComponentPlayer>() != null;
					if (flag & flag2)
					{
						Attack(componentCreature, 7f, 7f, isPersistent: true);
					}
					else if (!flag2 && (componentCreature.Category & m_autoChaseMask) > (CreatureCategory)0)
					{
						Attack(componentCreature, 7f, 7f, isPersistent: false);
					}
				}
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
			else if (m_componentCreature.ComponentHealth.Health > 0.4f)
			{
				m_range = ((m_subsystemSky.SkyLightIntensity < 0.2f) ? m_nightChaseRange : m_dayChaseRange);
				ComponentCreature componentCreature = FindTarget();
				if (componentCreature != null)
				{
					Attack(componentCreature, m_range, 60f, isPersistent: true);
				}
			}
		}, null);
		m_stateMachine.AddState("Chasing", () =>
		{
			m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 6f);
			m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: false);
			m_nextUpdateTime = 0.0;
		}, () =>
		{
			if (!IsActive || m_target == null || m_target.ComponentHealth.Health <= 0f)
			{
				m_stateMachine.TransitionTo("LookingForTarget");
			}
			else if (m_componentPathfinding.IsStuck)
			{
				m_stateMachine.TransitionTo("StuckScanning");
			}
			else
			{
				ComponentHerdBehavior componentHerd = m_componentHerd;
				if (m_target.ComponentHealth.Health > 0.5f && componentHerd != null && componentHerd.FindHerdCenter().HasValue && m_random.Float(0f, 1f) < 0.45f * m_dt)
				{
					m_stateMachine.TransitionTo("Circling");
				}
				else
				{
					float speed = 1f;
					ComponentRunAwayBehavior componentRunAwayBehavior = m_target.Entity.FindComponent<ComponentRunAwayBehavior>();
					if (componentRunAwayBehavior != null && componentRunAwayBehavior.IsActive)
					{
						speed = 1.35f;
					}
					Vector3 position = m_target.ComponentBody.Position;
					m_componentPathfinding.SetDestination(position, speed, 1.5f, 500, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
				}
			}
		}, null);
		m_stateMachine.AddState("Circling", () =>
		{
			Vector3 vector = m_componentCreature.ComponentBody.Position - m_target.ComponentBody.Position;
			m_circlingAngle = MathUtils.Atan2(vector.Z, vector.X);
		}, () =>
		{
			if (!IsActive || m_target == null)
			{
				m_stateMachine.TransitionTo("LookingForTarget");
			}
			else if (m_target.ComponentHealth.Health < 0.5f)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
			else if (Vector3.Distance(m_componentCreature.ComponentBody.Position, m_target.ComponentBody.Position) < 3.5f)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
			else
			{
				m_circlingAngle += 0.5f * m_dt * (float)m_circlingDirection;
				Vector3 vector = new Vector3(MathUtils.Cos(m_circlingAngle), 0f, MathUtils.Sin(m_circlingAngle)) * 10f;
				m_componentPathfinding.SetDestination(m_target.ComponentBody.Position + vector, 1f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
				if (m_random.Float(0f, 1f) < 0.15f * m_dt)
				{
					m_stateMachine.TransitionTo("Chasing");
				}
			}
		}, null);
		m_stateMachine.AddState("StuckScanning", () =>
		{
			m_stateMachine.TransitionTo("Observing");
		}, null, null);
		m_stateMachine.AddState("Ventilating", null, () =>
		{
			if (m_componentPathfinding.IsStuck || !m_componentPathfinding.Destination.HasValue)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
		}, null);
		float observationTimer = 0f;
		m_stateMachine.AddState("Observing", () =>
		{
			m_componentPathfinding.Stop();
			observationTimer = 0f;
			m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: true);
		}, () =>
		{
			observationTimer += m_dt;
			if (m_target != null)
			{
				m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
			}
			if (m_target != null && Vector3.DistanceSquared(m_componentCreature.ComponentBody.Position, m_target.ComponentBody.Position) < 25f)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
			if (observationTimer > m_random.Float(2.5f, 4.5f))
			{
				m_stateMachine.TransitionTo("RandomMoving");
			}
		}, null);
		m_stateMachine.AddState("RandomMoving", () =>
		{
			m_componentPathfinding.SetDestination(m_componentCreature.ComponentBody.Position + new Vector3(6f * m_random.Float(-1f, 1f), 0f, 6f * m_random.Float(-1f, 1f)), 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
		}, () =>
		{
			if (m_componentPathfinding.IsStuck || !m_componentPathfinding.Destination.HasValue)
			{
				m_stateMachine.TransitionTo("Chasing");
			}
		}, () =>
		{
			m_componentPathfinding.Stop();
		});
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
		if (componentCreature == null)
		{
			return 0f;
		}
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			if (componentCreature == m_componentCreature)
			{
				return 0f;
			}
			if (!componentCreature.Entity.IsAddedToProject || componentCreature.ComponentHealth.Health <= 0f)
			{
				return 0f;
			}
			if ((m_subsystemPlayers == null || !m_subsystemPlayers.IsPlayer(componentCreature.Entity)) && componentCreature.Entity.FindComponent<ComponentPlayer>() == null)
			{
				return 0f;
			}
			float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, componentCreature.ComponentBody.Position);
			float num2 = MathUtils.Min(m_range, 40f);
			if (num >= num2)
			{
				return 0f;
			}
			return num2 - num;
		}
		bool flag = componentCreature.Entity.FindComponent<ComponentPlayer>() != null;
		bool flag2 = m_componentCreature.Category != CreatureCategory.WaterPredator && m_componentCreature.Category != CreatureCategory.WaterOther;
		bool flag3 = componentCreature == Target || m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless;
		bool flag4 = (componentCreature.Category & m_autoChaseMask) > (CreatureCategory)0;
		bool flag5 = componentCreature == Target || (flag4 && MathUtils.Remainder(0.004999999888241291 * m_subsystemTime.GameTime + (double)((float)(GetHashCode() % 1000) / 1000f) + (double)((float)(componentCreature.GetHashCode() % 1000) / 1000f), 1.0) < (double)m_chaseNonPlayerProbability);
		if (componentCreature == m_componentCreature)
		{
			return 0f;
		}
		if ((flag || !flag5) && (!flag || !flag3))
		{
			return 0f;
		}
		if (!componentCreature.Entity.IsAddedToProject || componentCreature.ComponentHealth.Health <= 0f)
		{
			return 0f;
		}
		if (!flag2 && !IsTargetInWater(componentCreature.ComponentBody))
		{
			return 0f;
		}
		float num3 = Vector3.Distance(m_componentCreature.ComponentBody.Position, componentCreature.ComponentBody.Position);
		if (num3 <= 3f)
		{
			return m_range - num3;
		}
		float num4 = m_range;
		ComponentBody componentBody = componentCreature.ComponentBody;
		if (componentBody != null)
		{
			float num5 = ((componentBody.Size < 1f) ? MathUtils.Lerp(0.05f, 1f, componentBody.Size) : MathUtils.Pow(componentBody.Size, 0.5f));
			num4 *= num5;
			if (componentBody.IsSneaking)
			{
				num4 *= 0.5f;
			}
		}
		if (num3 >= num4)
		{
			return 0f;
		}
		float num6 = MathUtils.Cos(MathUtils.DegToRad(75f));
		Vector3 eyePosition = m_componentCreature.ComponentCreatureModel.EyePosition;
		ComponentCreatureModel componentCreatureModel = componentCreature.Entity.FindComponent<ComponentCreatureModel>();
		Vector3 vector;
		if (componentCreatureModel != null)
		{
			vector = componentCreatureModel.EyePosition;
		}
		else
		{
			BoundingBox boundingBox = componentCreature.ComponentBody.BoundingBox;
			vector = 0.5f * (boundingBox.Min + boundingBox.Max);
		}
		Vector3 v = vector - eyePosition;
		float num7 = v.Length();
		if (num7 < 0.001f)
		{
			return 0f;
		}
		v /= num7;
		if (Vector3.Dot(m_componentCreature.ComponentBody.Matrix.Forward, v) < num6)
		{
			return 0f;
		}
		if (m_subsystemTerrain.Raycast(eyePosition, vector, useInteractionBoxes: false, skipAirBlocks: true, (int cellValue, float d) =>
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
			return block.IsCollidable && !block.IsTransparent;
		}).HasValue)
		{
			return 0f;
		}
		return num4 - num3;
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
}
