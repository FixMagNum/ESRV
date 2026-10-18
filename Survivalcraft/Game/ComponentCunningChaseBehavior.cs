using System;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCunningChaseBehavior : ComponentBehavior, IUpdateable
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

	private float m_gazeTime;

	private float m_damageMemoryTime;

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

	private bool IsPlayerLookingAtMe(ComponentPlayer player)
	{
		Vector3 eyePosition = player.ComponentMiner.ComponentCreature.ComponentCreatureModel.EyePosition;
		Vector3 forward = player.ComponentMiner.ComponentCreature.ComponentBody.Matrix.Forward;
		Vector3 vector = m_componentCreature.ComponentBody.Position - eyePosition;
		float num = vector.Length();
		if (num < 0.01f)
		{
			return false;
		}
		Vector3 vector2 = vector / num;
		if (Vector3.Dot(forward, vector2) <= 0.55f)
		{
			return false;
		}
		float num2 = 60f;
		if (num > num2)
		{
			return false;
		}
		SubsystemTerrain subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		Vector3 start = eyePosition;
		Vector3 end = eyePosition + vector2 * num;
		return !subsystemTerrain.Raycast(start, end, useInteractionBoxes: false, skipAirBlocks: true, (int cellValue, float d) =>
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
			return block.IsCollidable && !block.IsTransparent;
		}).HasValue;
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
			return;
		}
		m_damageMemoryTime -= dt;
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
						float x3 = (m_isPersistent ? m_random.Float(8f, 10f) : 2f);
						m_chaseTime = MathUtils.Max(m_chaseTime, x3);
						m_componentMiner.Hit(hitBody, hitPoint, m_componentCreature.ComponentBody.Matrix.Forward);
						m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
					}
				}
			}
		}
		if (m_subsystemTime.GameTime >= m_nextUpdateTime)
		{
			m_dt = m_random.Float(0.25f, 0.35f) + MathUtils.Min((float)(m_subsystemTime.GameTime - m_nextUpdateTime), 0.1f);
			m_nextUpdateTime = m_subsystemTime.GameTime + (double)m_dt;
			m_stateMachine.Update();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
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
		m_gazeTime = 0f;
		m_componentCreature.ComponentHealth.Attacked += (ComponentCreature attacker) =>
		{
			m_damageMemoryTime = 5f;
			m_gazeTime = 0f;
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				if (attacker != null && attacker.Entity != null && ((m_subsystemPlayers != null && m_subsystemPlayers.IsPlayer(attacker.Entity)) || attacker.Entity.FindComponent<ComponentPlayer>() != null))
				{
					Attack(attacker, 30f, 60f, isPersistent: true);
				}
			}
			else if (m_random.Float(0f, 1f) < m_chaseWhenAttackedProbability)
			{
				bool flag = m_chaseWhenAttackedProbability >= 1f;
				float maxRange = (flag ? 30f : 7f);
				float maxChaseTime = (flag ? 60f : 7f);
				Attack(attacker, maxRange, maxChaseTime, flag);
				m_componentBodies.Clear();
				m_subsystemBodies.FindBodiesAroundPoint(new Vector2(m_componentCreature.ComponentBody.Position.X, m_componentCreature.ComponentBody.Position.Z), 15f, m_componentBodies);
				for (int i = 0; i < m_componentBodies.Count; i++)
				{
					ComponentCreature componentCreature = m_componentBodies.Array[i].Entity.FindComponent<ComponentCreature>();
					if (componentCreature != null && componentCreature != m_componentCreature && componentCreature.Category == m_componentCreature.Category)
					{
						componentCreature.Entity.FindComponent<ComponentCunningChaseBehavior>()?.Attack(attacker, maxRange, maxChaseTime, flag);
					}
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
			m_componentPathfinding.SetDestination(m_componentCreature.ComponentBody.Position + new Vector3(6f * m_random.Float(-1f, 1f), 0f, 6f * m_random.Float(-1f, 1f)), 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
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
			m_gazeTime = 0f;
		}, () =>
		{
			if (m_target != null)
			{
				ComponentPlayer componentPlayer = m_target.Entity.FindComponent<ComponentPlayer>();
				float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, m_target.ComponentBody.Position);
				if (componentPlayer != null && num > 0.1f)
				{
					if (!(m_damageMemoryTime > 0f) && !(num < 3.5f) && IsPlayerLookingAtMe(componentPlayer))
					{
						m_gazeTime += m_dt;
						if (m_gazeTime >= 30f)
						{
							PlayerAchievements playerAchievements = componentPlayer.PlayerAchievements;
							if (playerAchievements != null && playerAchievements.TheLookFlayer == 0L)
							{
								playerAchievements.TheLookFlayer = 1L;
								playerAchievements.EnqueueTheLookFlayerAchievement();
							}
							m_stateMachine.TransitionTo("Fleeing");
						}
						else
						{
							m_componentCreatureModel.AttackOrder = false;
						}
						return;
					}
					m_gazeTime = 0f;
				}
			}
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
					float num2 = Vector3.Distance(v, vector);
					float num3 = ((num2 < 4f) ? 0.2f : 0f);
					m_componentPathfinding.SetDestination(vector + num3 * num2 * m_target.ComponentBody.Velocity, 1f, 1.5f, maxPathfindingPositions, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
					if (m_random.Float(0f, 1f) < 0.33f * m_dt)
					{
						m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
					}
				}
			}
		}, null);
		m_stateMachine.AddState("Fleeing", () =>
		{
			m_importanceLevel = 200f;
			ComponentPlayer componentPlayer = m_target?.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null)
			{
				Vector3 vector = Vector3.Normalize(m_componentCreature.ComponentBody.Position - componentPlayer.ComponentBody.Position);
				Vector3 value = m_componentCreature.ComponentBody.Position + vector * 10f;
				m_componentPathfinding.SetDestination(value, 1f, 3f, 500, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
			}
		}, () =>
		{
			ComponentPlayer componentPlayer = m_target?.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer == null || !IsPlayerLookingAtMe(componentPlayer))
			{
				m_stateMachine.TransitionTo("LookingForTarget");
			}
			else
			{
				Vector3 vector = Vector3.Normalize(m_componentCreature.ComponentBody.Position - componentPlayer.ComponentBody.Position);
				Vector3 value = m_componentCreature.ComponentBody.Position + vector * 10f;
				m_componentPathfinding.SetDestination(value, 1f, 3f, 500, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
			}
		}, null);
		m_stateMachine.TransitionTo("LookingForTarget");
	}

	public ComponentCreature FindTarget()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		ComponentCreature result = null;
		float data = 0f;
		m_componentBodies.Clear();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), m_range, m_componentBodies);
		for (int craftingRecipe = 0; craftingRecipe < m_componentBodies.Count; craftingRecipe++)
		{
			ComponentCreature componentCreature = m_componentBodies.Array[craftingRecipe].Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null)
			{
				float num = ScoreTarget(componentCreature);
				if (num > data)
				{
					data = num;
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
}
