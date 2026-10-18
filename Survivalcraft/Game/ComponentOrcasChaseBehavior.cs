using System;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentOrcasChaseBehavior : ComponentBehavior, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemSky m_subsystemSky;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemTime m_subsystemTime;

	public SubsystemNoise m_subsystemNoise;

	public SubsystemTerrain m_subsystemTerrain;

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

	private bool m_modeCurious;

	private bool m_modeAggressive;

	private float m_interestTimeLeft;

	private float m_curiosityCooldown;

	private float m_curiosityCircleTime;

	private float m_circleAngle;

	private float m_circleRadius;

	private float m_fleeTimeLeft;

	private int m_breachPhase;

	private float m_breachTimer;

	private Vector3 m_breachUnderPoint;

	private Vector3 m_breachUpPoint;

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

	public void StartPursuit(ComponentCreature target, float maxRange, float maxChaseTime, bool isPersistent, bool isCurious)
	{
		m_target = target;
		m_nextUpdateTime = 0.0;
		m_range = maxRange;
		m_chaseTime = maxChaseTime;
		m_isPersistent = isPersistent;
		m_importanceLevel = 200f;
		m_modeCurious = isCurious;
		m_modeAggressive = !isCurious;
		if (m_modeCurious)
		{
			m_interestTimeLeft = m_random.Float(120f, 300f);
			ScheduleNextCuriousAction(0f);
			m_circleRadius = m_random.Float(2f, 10f);
			m_circleAngle = 0f;
		}
		else
		{
			m_interestTimeLeft = 0f;
		}
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
			if (m_modeAggressive && IsTargetInAttackRange(m_target.ComponentBody))
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
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
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
		m_componentCreature.ComponentHealth.Attacked += (ComponentCreature attacker) =>
		{
			if (attacker?.Entity.FindComponent<ComponentPlayer>() == null)
			{
				if (m_random.Float(0f, 1f) < m_chaseWhenAttackedProbability)
				{
					if (m_chaseWhenAttackedProbability >= 1f)
					{
						StartPursuit(attacker, 30f, 60f, isPersistent: true, isCurious: false);
					}
					else
					{
						StartPursuit(attacker, 7f, 7f, isPersistent: false, isCurious: false);
					}
				}
			}
			else if (!IsBodyExplicitlyInWater18(attacker.ComponentBody) || IsBodyInBoat(attacker.ComponentBody))
			{
				StartFleeFrom(attacker.ComponentBody.Position, m_random.Float(12f, 20f));
				m_autoChaseSuppressionTime = m_random.Float(12f, 30f);
				m_importanceLevel = 0f;
			}
			else
			{
				AttackAggressively(attacker);
			}
		};
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		componentBody.CollidedWithBody = (Action<ComponentBody>)Delegate.Combine(componentBody.CollidedWithBody, (Action<ComponentBody>)((ComponentBody other) =>
		{
			if (m_target == null && m_autoChaseSuppressionTime <= 0f && m_random.Float(0f, 1f) < m_chaseOnTouchProbability)
			{
				ComponentCreature componentCreature = other.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					bool flag = m_subsystemPlayers.IsPlayer(other.Entity);
					bool flag2 = (componentCreature.Category & m_autoChaseMask) > (CreatureCategory)0;
					if (flag && m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless)
					{
						StartCuriosity(componentCreature);
					}
					else if (!flag & flag2)
					{
						StartPursuit(componentCreature, 7f, 7f, isPersistent: false, isCurious: false);
					}
				}
			}
			if (m_target != null && other == m_target.ComponentBody && other.StandingOnBody == m_componentCreature.ComponentBody)
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
				if (m_targetInRangeTime > 3f && componentCreature != null)
				{
					bool flag = m_subsystemSky.SkyLightIntensity >= 0.1f;
					float num = (flag ? (m_dayChaseRange + 6f) : (m_nightChaseRange + 6f));
					float num2 = (flag ? (m_dayChaseTime * m_random.Float(0.75f, 1f)) : (m_nightChaseTime * m_random.Float(0.75f, 1f)));
					if (componentCreature.Entity.FindComponent<ComponentPlayer>() != null)
					{
						StartCuriosity(componentCreature, num, num2, !flag);
					}
					else
					{
						StartPursuit(componentCreature, num, num2, !flag, isCurious: false);
					}
				}
			}
		}, null);
		m_stateMachine.AddState("RandomMoving", () =>
		{
			Vector3 position = m_componentCreature.ComponentBody.Position;
			m_componentPathfinding.SetDestination(position + new Vector3(6f * m_random.Float(-1f, 1f), 0f, 6f * m_random.Float(-1f, 1f)), 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
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
		m_stateMachine.AddState("Fleeing", null, () =>
		{
			if (m_fleeTimeLeft <= 0f)
			{
				m_stateMachine.TransitionTo("LookingForTarget");
			}
			else
			{
				m_fleeTimeLeft -= m_dt;
				if (m_componentPathfinding.IsStuck || !m_componentPathfinding.Destination.HasValue)
				{
					PickFleeDestination();
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
			else
			{
				bool flag = m_target.Entity.FindComponent<ComponentPlayer>() != null;
				if (m_modeAggressive & flag)
				{
					bool flag2 = IsBodyExplicitlyInWater18(m_target.ComponentBody);
					bool flag3 = IsBodyInBoat(m_target.ComponentBody);
					if (!flag2 && !flag3)
					{
						m_importanceLevel = 0f;
						return;
					}
				}
				if (m_target.ComponentHealth.Health <= 0f)
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
					else if (!m_modeCurious || !flag)
					{
						int maxPathfindingPositions = (m_isPersistent ? (m_subsystemTime.FixedTimeStep.HasValue ? 1500 : 500) : 0);
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
					else
					{
						m_interestTimeLeft -= m_dt;
						if (m_interestTimeLeft <= 0f)
						{
							m_importanceLevel = 0f;
						}
						else
						{
							UpdateCuriosityBehavior();
						}
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

	public float ScoreTarget(ComponentCreature c)
	{
		if (c == m_componentCreature || !c.Entity.IsAddedToProject || c.ComponentHealth.Health <= 0f)
		{
			return 0f;
		}
		bool flag = c.Entity.FindComponent<ComponentPlayer>() != null;
		bool flag2 = m_componentCreature.Category == CreatureCategory.WaterPredator || m_componentCreature.Category == CreatureCategory.WaterOther;
		if (flag)
		{
			float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, c.ComponentBody.Position);
			if (num >= m_range)
			{
				return 0f;
			}
			return m_range - num;
		}
		if ((c.Category & m_autoChaseMask) <= (CreatureCategory)0 || !flag2 || !IsBodyExplicitlyInWater18(c.ComponentBody))
		{
			return 0f;
		}
		float num2 = Vector3.Distance(m_componentCreature.ComponentBody.Position, c.ComponentBody.Position);
		if (num2 >= m_range)
		{
			return 0f;
		}
		return m_range - num2;
	}

	private void StartCuriosity(ComponentCreature player, float range = 12f, float time = 30f, bool persistent = false)
	{
		StartPursuit(player, range, time, persistent, isCurious: true);
	}

	private void UpdateCuriosityBehavior()
	{
		if (m_target == null)
		{
			return;
		}
		if (m_breachPhase != 0)
		{
			Vector3 vector = m_target.ComponentBody.BoundingBox.Center();
			if (m_breachPhase == 1)
			{
				m_componentPathfinding.SetDestination(m_breachUnderPoint, 1.4f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
				if (Vector3.Distance(m_componentCreature.ComponentBody.Position, m_breachUnderPoint) < 1.4f)
				{
					m_breachPhase = 2;
					m_breachTimer = m_random.Float(0.6f, 1f);
					m_breachUpPoint = new Vector3(vector.X, vector.Y + 1f, vector.Z);
				}
				return;
			}
			if (m_breachPhase == 2)
			{
				m_breachTimer -= m_dt;
				m_componentPathfinding.SetDestination(m_breachUpPoint, 1.8f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
				if (m_componentCreature.ComponentBody.ImmersionFactor > 0.5f)
				{
					m_componentCreature.ComponentLocomotion.JumpOrder = 1f;
				}
				if (m_breachTimer <= 0f || Vector3.Distance(m_componentCreature.ComponentBody.Position, m_breachUpPoint) < 1.2f)
				{
					m_breachPhase = 0;
					m_curiosityCooldown = m_random.Float(6f, 12f);
				}
				return;
			}
		}
		if (m_curiosityCircleTime > 0f)
		{
			m_curiosityCircleTime -= m_dt;
			Vector3 vector2 = m_target.ComponentBody.BoundingBox.Center();
			m_circleAngle -= 0.8f * m_dt;
			Vector3 vector3 = new Vector3(MathUtils.Cos(m_circleAngle), 0f, MathUtils.Sin(m_circleAngle)) * m_circleRadius;
			Vector3 value = new Vector3(vector2.X + vector3.X, vector2.Y, vector2.Z + vector3.Z);
			value += 0.2f * m_target.ComponentBody.Velocity;
			m_componentPathfinding.SetDestination(value, 1.3f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
			return;
		}
		if (m_curiosityCooldown > 0f)
		{
			m_curiosityCooldown -= m_dt;
			m_curiosityCircleTime = MathUtils.Clamp(m_curiosityCircleTime, 0f, 0f);
			Vector3 vector4 = m_target.ComponentBody.BoundingBox.Center();
			Vector3 value2 = vector4 + 0.5f * (m_componentCreature.ComponentBody.Position - vector4);
			m_componentPathfinding.SetDestination(value2, 0.6f, 1.2f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
			return;
		}
		switch (m_random.Int(0, 2))
		{
		case 0:
			m_curiosityCircleTime = 12f;
			m_circleRadius = m_random.Float(2f, 10f);
			return;
		case 1:
		{
			Vector3 vector5 = m_target.ComponentBody.BoundingBox.Center();
			m_breachUnderPoint = new Vector3(vector5.X, vector5.Y - m_random.Float(2.5f, 4f), vector5.Z);
			m_breachPhase = 1;
			return;
		}
		}
		Vector3 vector6 = m_target.ComponentBody.BoundingBox.Center();
		float x = m_random.Float(0f, MathUtils.DegToRad(360f));
		Vector3 vector7 = new Vector3(MathUtils.Cos(x), 0f, MathUtils.Sin(x));
		Vector3 value3 = vector6 + vector7 * m_random.Float(1.5f, 3f);
		value3.Y = vector6.Y + m_random.Float(-0.5f, 0.5f);
		m_componentPathfinding.SetDestination(value3, 1.8f, 1.3f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
		m_curiosityCooldown = m_random.Float(6f, 12f);
	}

	private void ScheduleNextCuriousAction(float delay)
	{
		m_curiosityCooldown = delay;
		m_curiosityCircleTime = 0f;
	}

	private void AttackAggressively(ComponentCreature attacker)
	{
		m_modeCurious = false;
		m_modeAggressive = true;
		if (m_chaseWhenAttackedProbability >= 1f)
		{
			StartPursuit(attacker, 30f, 60f, isPersistent: true, isCurious: false);
		}
		else
		{
			StartPursuit(attacker, 7f, 7f, isPersistent: false, isCurious: false);
		}
	}

	public bool IsTargetInAttackRange(ComponentBody target)
	{
		if (IsBodyInAttackRange(target))
		{
			return true;
		}
		BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
		BoundingBox boundingBox2 = target.BoundingBox;
		Vector3 vector = 0.5f * (boundingBox2.Min + boundingBox2.Max) - 0.5f * (boundingBox.Min + boundingBox.Max);
		float num = vector.Length();
		Vector3 v = vector / num;
		float num2 = 0.5f * (boundingBox.Size().X + boundingBox2.Size().X);
		float num3 = 0.5f * (boundingBox.Size().Y + boundingBox2.Size().Y);
		if (MathUtils.Abs(vector.Y) < num3 * 0.99f)
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

	public bool IsBodyInAttackRange(ComponentBody target)
	{
		BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
		BoundingBox boundingBox2 = target.BoundingBox;
		Vector3 vector = 0.5f * (boundingBox2.Min + boundingBox2.Max) - 0.5f * (boundingBox.Min + boundingBox.Max);
		float num = vector.Length();
		Vector3 v = vector / num;
		float num2 = 0.5f * (boundingBox.Size().X + boundingBox2.Size().X);
		float num3 = 0.5f * (boundingBox.Size().Y + boundingBox2.Size().Y);
		if (MathUtils.Abs(vector.Y) < num3 * 0.99f)
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

	private bool IsBodyExplicitlyInWater18(ComponentBody body)
	{
		if (body == null || m_subsystemTerrain == null || body.ImmersionDepth <= 0f)
		{
			return false;
		}
		Vector3 position = body.Position;
		int x = Terrain.ToCell(position.X);
		int num = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		Terrain terrain = m_subsystemTerrain.Terrain;
		if (Terrain.ExtractContents(terrain.GetCellValueFast(x, num, z)) != 18)
		{
			if (num > 0)
			{
				return Terrain.ExtractContents(terrain.GetCellValueFast(x, num - 1, z)) == 18;
			}
			return false;
		}
		return true;
	}

	private bool IsBodyInBoat(ComponentBody body)
	{
		for (ComponentBody componentBody = body?.ParentBody; componentBody != null; componentBody = componentBody.ParentBody)
		{
			string text = componentBody.Entity?.ValuesDictionary?.DatabaseObject?.Name;
			if ((text ?? string.Empty).IndexOf("Boat", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private void StartFleeFrom(Vector3 dangerPos, float duration)
	{
		m_fleeTimeLeft = duration;
		m_target = null;
		PickFleeDestination(dangerPos);
		m_stateMachine.TransitionTo("Fleeing");
	}

	private void PickFleeDestination()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 num2 = Vector3.Normalize(new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f)));
		Vector3 num3 = position + num2 * m_random.Float(10f, 18f);
		m_componentPathfinding.SetDestination(num3, 1f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
	}

	private void PickFleeDestination(Vector3 from)
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 i = Vector3.Normalize(position - from + new Vector3(m_random.Float(-0.5f, 0.5f), 0f, m_random.Float(-0.5f, 0.5f)));
		Vector3 value = position + i * m_random.Float(12f, 20f);
		m_componentPathfinding.SetDestination(value, 1f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
	}
}
