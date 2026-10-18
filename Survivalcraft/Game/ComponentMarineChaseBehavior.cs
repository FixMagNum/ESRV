using System;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentMarineChaseBehavior : ComponentBehavior, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemSky m_subsystemSky;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemTime m_subsystemTime;

	public SubsystemNoise m_subsystemNoise;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

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

	private float m_curiosityTimeLeft;

	private float m_circleAngle;

	private float m_circleRadius;

	private bool m_nextAttackHasCuriosity;

	private float m_fleeTimeLeft;

	public ComponentLocomotion m_componentLocomotion;

	public float m_landPursuitTime;

	public float m_landPursuitCooldown;

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
		m_target = componentCreature;
		m_nextUpdateTime = 0.0;
		m_range = maxRange;
		m_chaseTime = maxChaseTime;
		m_isPersistent = isPersistent;
		m_importanceLevel = 200f;
		m_curiosityTimeLeft = (m_nextAttackHasCuriosity ? 20f : 0f);
		m_nextAttackHasCuriosity = false;
		if (m_curiosityTimeLeft > 0f && m_target != null)
		{
			m_circleRadius = m_random.Float(2f, 10f);
			m_circleAngle = 0f;
		}
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			m_componentCreatureModel.AttackOrder = IsAttack;
			return;
		}
		if (m_landPursuitCooldown > 0f)
		{
			m_landPursuitCooldown -= dt;
		}
		m_autoChaseSuppressionTime -= dt;
		if (IsActive && m_target != null && (!IsBodyExplicitlyInWater18(m_target.ComponentBody) || IsBodyInBoat(m_target.ComponentBody)))
		{
			m_importanceLevel = 0f;
			m_target = null;
			m_componentPathfinding.Stop();
		}
		if (IsActive && m_target != null)
		{
			m_chaseTime -= dt;
			m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
			if (m_curiosityTimeLeft <= 0f && IsTargetInAttackRange(m_target.ComponentBody))
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
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
		m_componentMiner = Entity.FindComponent<ComponentMiner>(throwOnError: true);
		m_componentLocomotion = Entity.FindComponent<ComponentLocomotion>(throwOnError: true);
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
			if (!IsBodyExplicitlyInWater18(attacker.ComponentBody) || IsBodyInBoat(attacker.ComponentBody))
			{
				StartFleeFrom(attacker.ComponentBody.Position, m_random.Float(3f, 6f));
				m_autoChaseSuppressionTime = m_random.Float(12f, 30f);
				m_importanceLevel = 0f;
			}
			else if (m_random.Float(0f, 1f) < m_chaseWhenAttackedProbability)
			{
				m_nextAttackHasCuriosity = false;
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
		componentBody.CollidedWithBody = (Action<ComponentBody>)Delegate.Combine(componentBody.CollidedWithBody, (Action<ComponentBody>)((ComponentBody other) =>
		{
			if (m_target == null && m_autoChaseSuppressionTime <= 0f && m_random.Float(0f, 1f) < m_chaseOnTouchProbability)
			{
				ComponentCreature componentCreature = other.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					bool flag = m_subsystemPlayers.IsPlayer(other.Entity);
					bool flag2 = (componentCreature.Category & m_autoChaseMask) > (CreatureCategory)0;
					if (IsBodyExplicitlyInWater18(componentCreature.ComponentBody) && !IsBodyInBoat(componentCreature.ComponentBody) && FishingActivityGood(m_componentCreature.ComponentBody.Position) && ((flag && m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless) || (!flag & flag2)))
					{
						m_nextAttackHasCuriosity = false;
						Attack(componentCreature, 7f, 7f, isPersistent: false);
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
				if (FishingActivityGood(m_componentCreature.ComponentBody.Position))
				{
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
						m_nextAttackHasCuriosity = true;
						Attack(componentCreature, maxRange, maxChaseTime, !flag);
					}
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
				bool flag = IsBodyExplicitlyInWater18(m_target.ComponentBody);
				bool flag2 = IsBodyInBoat(m_target.ComponentBody);
				if (!flag | flag2)
				{
					if (m_landPursuitTime > 0f)
					{
						m_landPursuitTime -= m_dt;
						Vector3 vector = Vector3.Normalize(m_target.ComponentBody.Position - m_componentCreature.ComponentBody.Position);
						m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
						if (m_random.Float(0f, 1f) < 5f * m_dt)
						{
							m_componentLocomotion.JumpOrder = 1f;
							Vector3 impulse = new Vector3(vector.X, 0.3f, vector.Z) * 1.5f * m_componentCreature.ComponentBody.Mass;
							m_componentCreature.ComponentBody.ApplyImpulse(impulse);
						}
						if (m_landPursuitTime <= 0f)
						{
							StartFleeFrom(m_target.ComponentBody.Position, 8f);
							return;
						}
					}
					else
					{
						if (!(m_landPursuitCooldown <= 0f))
						{
							m_importanceLevel = 0f;
							return;
						}
						m_landPursuitTime = 4f;
						m_landPursuitCooldown = 20f;
					}
				}
				else
				{
					m_landPursuitTime = 0f;
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
					else if (m_curiosityTimeLeft > 0f)
					{
						m_curiosityTimeLeft -= m_dt;
						Vector3 vector2 = m_target.ComponentBody.BoundingBox.Center();
						m_circleAngle -= 0.6f * m_dt;
						Vector3 vector3 = new Vector3(MathUtils.Cos(m_circleAngle), 0f, MathUtils.Sin(m_circleAngle)) * m_circleRadius;
						Vector3 value = new Vector3(vector2.X + vector3.X, vector2.Y, vector2.Z + vector3.Z);
						value += 0.2f * m_target.ComponentBody.Velocity;
						m_componentPathfinding.SetDestination(value, 1f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
					}
					else
					{
						int maxPathfindingPositions = (m_isPersistent ? (m_subsystemTime.FixedTimeStep.HasValue ? 1500 : 500) : 0);
						BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
						BoundingBox boundingBox2 = m_target.ComponentBody.BoundingBox;
						Vector3 v = 0.5f * (boundingBox.Min + boundingBox.Max);
						Vector3 vector4 = 0.5f * (boundingBox2.Min + boundingBox2.Max);
						float num = Vector3.Distance(v, vector4);
						float num2 = ((num < 4f) ? 0.2f : 0f);
						m_componentPathfinding.SetDestination(vector4 + num2 * num * m_target.ComponentBody.Velocity, 1f, 1.5f, maxPathfindingPositions, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, m_target.ComponentBody);
						if (m_random.Float(0f, 1f) < 0.33f * m_dt)
						{
							m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
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
		if (!IsBodyExplicitlyInWater18(c.ComponentBody) || IsBodyInBoat(c.ComponentBody))
		{
			return 0f;
		}
		bool flag = c.Entity.FindComponent<ComponentPlayer>() != null;
		bool flag2 = m_componentCreature.Category != CreatureCategory.WaterPredator && m_componentCreature.Category != CreatureCategory.WaterOther;
		bool flag3 = c == Target || m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless;
		bool flag4 = (c.Category & m_autoChaseMask) > (CreatureCategory)0;
		bool flag5 = c.ComponentHealth.Health < 0.33f;
		if (!FishingActivityGood(c.ComponentBody.Position))
		{
			return 0f;
		}
		if (c != m_componentCreature && (flag5 || (!flag & flag4) || (flag & flag3)) && c.Entity.IsAddedToProject && c.ComponentHealth.Health > 0f && (flag2 || IsBodyExplicitlyInWater18(c.ComponentBody)))
		{
			float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, c.ComponentBody.Position);
			if (num < m_range)
			{
				float num2 = (flag5 ? 5f : 0f);
				return m_range - num + num2;
			}
		}
		return 0f;
	}

	public bool IsTargetInAttackRange(ComponentBody target)
	{
		if (IsBodyInAttackRange(target))
		{
			return true;
		}
		BoundingBox data2 = m_componentCreature.ComponentBody.BoundingBox;
		BoundingBox boundingBox = target.BoundingBox;
		Vector3 vector = 0.5f * (data2.Min + data2.Max);
		Vector3 vector2 = 0.5f * (boundingBox.Min + boundingBox.Max) - vector;
		float num = vector2.Length();
		Vector3 v = vector2 / num;
		float num2 = 0.5f * (data2.Max.X - data2.Min.X + boundingBox.Max.X - boundingBox.Min.X);
		float num3 = 0.5f * (data2.Max.Y - data2.Min.Y + boundingBox.Max.Y - boundingBox.Min.Y);
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
		Vector3 mountingFace = m_componentCreature.ComponentBody.BoundingBox.Center();
		Vector3 vector = target.BoundingBox.Center();
		Ray3 ray = new Ray3(mountingFace, Vector3.Normalize(vector - mountingFace));
		BodyRaycastResult? bodyRaycastResult = m_componentMiner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
		if (bodyRaycastResult.HasValue && bodyRaycastResult.Value.Distance < 1.75f && (bodyRaycastResult.Value.ComponentBody == target || bodyRaycastResult.Value.ComponentBody.IsChildOfBody(target) || target.IsChildOfBody(bodyRaycastResult.Value.ComponentBody) || target.StandingOnBody == bodyRaycastResult.Value.ComponentBody))
		{
			hitPoint = bodyRaycastResult.Value.HitPoint();
			return bodyRaycastResult.Value.ComponentBody;
		}
		hitPoint = default;
		return null;
	}

	private bool FishingActivityGood(Vector3 pos)
	{
		return GetFishingResponseMultiplier(pos) > 0.5f;
	}

	private float GetFishingResponseMultiplier(Vector3 pos)
	{
		if (m_subsystemWeather == null)
		{
			return 1f;
		}
		float timeOfDayMultiplier = GetTimeOfDayMultiplier();
		float weatherMultiplier = GetWeatherMultiplier(pos);
		float windMultiplier = GetWindMultiplier();
		if (windMultiplier <= 0f)
		{
			return 0f;
		}
		float x = 1f;
		float num = MathUtils.Lerp(0.7f, 1.1f, MathUtils.Saturate(x));
		return MathUtils.Clamp(timeOfDayMultiplier * weatherMultiplier * windMultiplier * num, 0f, 8f);
	}

	private float GetTimeOfDayMultiplier()
	{
		if (m_subsystemTimeOfDay == null)
		{
			return 1f;
		}
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		float sunriseOffset = m_subsystemTimeOfDay.SunriseOffset;
		float sunsetOffset = m_subsystemTimeOfDay.SunsetOffset;
		float num = sunriseOffset + 0.05f;
		float num2 = sunsetOffset - 0.05f;
		if (MathUtils.Abs(timeOfDay - sunriseOffset) <= 0.07f)
		{
			return 4f;
		}
		if (MathUtils.Abs(timeOfDay - sunsetOffset) <= 0.07f)
		{
			return 3f;
		}
		if (timeOfDay >= num && timeOfDay <= num2)
		{
			return 0.4f;
		}
		float num3 = 4f;
		if (m_subsystemSky != null)
		{
			float f;
			switch (m_subsystemSky.MoonPhase)
			{
			case 0:
				f = 1f;
				break;
			case 1:
			case 7:
				f = 0.8f;
				break;
			case 2:
			case 6:
				f = 0.6f;
				break;
			case 3:
			case 5:
				f = 0.4f;
				break;
			default:
				f = 0f;
				break;
			}
			return MathUtils.Lerp(num3, 4f, f);
		}
		return num3;
	}

	private float GetWeatherMultiplier(Vector3 pos)
	{
		if (m_subsystemWeather == null)
		{
			return 1f;
		}
		int x = Terrain.ToCell(pos.X);
		int z = Terrain.ToCell(pos.Z);
		float num = MathUtils.Saturate(m_subsystemWeather.GetPrecipitationShaftInfo(x, z).Intensity);
		if (m_subsystemWeather.m_lightningIntensity >= 0.8f)
		{
			return 0.8f;
		}
		if (num > 0f && num < 0.3f)
		{
			return 4f;
		}
		if (num >= 0.3f && num < 0.7f)
		{
			return 2.4f;
		}
		if (num >= 0.7f)
		{
			return 1.6f;
		}
		return 2f;
	}

	private float GetWindMultiplier()
	{
		if (m_subsystemWeather == null)
		{
			return 1f;
		}
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		if (currentWindStrength <= 4f)
		{
			return 2f;
		}
		if (currentWindStrength <= 8f)
		{
			return 1.4f;
		}
		if (currentWindStrength <= 12f)
		{
			return 0.6f;
		}
		return 0f;
	}

	private bool IsBodyExplicitlyInWater18(ComponentBody body)
	{
		if (body == null)
		{
			return false;
		}
		if (body.ImmersionFactor > 0.01f)
		{
			bool flag = body.ImmersionFluidBlock is WaterBlock;
			bool flag2 = body.ImmersionFocalorsFluidBlock is FocalorsWaterBlock;
			if (flag | flag2)
			{
				return true;
			}
		}
		if (m_subsystemTerrain == null)
		{
			return false;
		}
		Vector3 position = body.Position;
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y + 0.5f);
		int z = Terrain.ToCell(position.Z);
		int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(x, y, z));
		Block block = BlocksManager.Blocks[num];
		if (num != 18 && !(block is WaterBlock))
		{
			return block is FocalorsWaterBlock;
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
		Vector3 vector = Vector3.Normalize(new Vector3(m_random.Float(-1f, 1f), 0f, m_random.Float(-1f, 1f)));
		Vector3 value = position + vector * m_random.Float(10f, 18f);
		m_componentPathfinding.SetDestination(value, 1f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
	}

	private void PickFleeDestination(Vector3 from)
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 vector = Vector3.Normalize(position - from + new Vector3(m_random.Float(-0.5f, 0.5f), 0f, m_random.Float(-0.5f, 0.5f)));
		Vector3 value = position + vector * m_random.Float(12f, 20f);
		m_componentPathfinding.SetDestination(value, 1f, 1.5f, 0, useRandomMovements: true, ignoreHeightDifference: false, raycastDestination: true, null);
	}
}
