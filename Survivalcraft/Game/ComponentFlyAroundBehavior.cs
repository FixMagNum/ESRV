using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFlyAroundBehavior : ComponentBehavior, IUpdateable
{
	private struct RestParams
	{
		public float ChancePerSecond;

		public float DurationMin;

		public float DurationMax;

		public float ExtendProbability;
	}

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentCreature m_componentCreature;

	public ComponentPathfinding m_componentPathfinding;

	public float m_angle;

	public float m_importanceLevel = 1f;

	public Random m_random = new Random();

	public SubsystemTime m_subsystemTime;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	private bool m_wantsRest;

	private float m_restTimeLeft;

	private float m_sleepImportance;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public bool IsSleeping { get; private set; }

	public void Update(float dt)
	{
		if (string.IsNullOrEmpty(m_stateMachine.CurrentState))
		{
			m_stateMachine.TransitionTo("Inactive");
		}
		if (m_random.Float(0f, 1f) < 0.05f * dt)
		{
			m_importanceLevel = m_random.Float(1f, 2f);
		}
		if (IsActive)
		{
			m_stateMachine.Update();
		}
		else
		{
			m_stateMachine.TransitionTo("Inactive");
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_stateMachine.AddState("Inactive", null, () =>
		{
			m_wantsRest = false;
			IsSleeping = false;
			SignalRelaxToModel(on: false);
			if (m_random.Float(0f, 1f) < 0.05f * m_subsystemTime.GameTimeDelta)
			{
				m_importanceLevel = m_random.Float(1f, 2f);
			}
			RestParams restParams = ComputeRestParams();
			if (!m_wantsRest && m_random.Float(0f, 1f) < restParams.ChancePerSecond * m_subsystemTime.GameTimeDelta)
			{
				m_wantsRest = true;
				m_importanceLevel = MathUtils.Max(m_importanceLevel, ComputeSleepImportance());
			}
			if (IsActive)
			{
				if (m_wantsRest && CanLieHere())
				{
					m_stateMachine.TransitionTo("LieDown");
				}
				else
				{
					m_stateMachine.TransitionTo("Fly");
				}
			}
		}, null);
		m_stateMachine.AddState("Stuck", () =>
		{
			m_stateMachine.TransitionTo("Fly");
			if (m_random.Float(0f, 1f) < 0.5f)
			{
				m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: false);
				m_importanceLevel = 1f;
			}
		}, null, null);
		m_stateMachine.AddState("Fly", () =>
		{
			m_angle = m_random.Float(0f, (float)Math.PI * 2f);
			m_componentPathfinding.Stop();
			IsSleeping = false;
			SignalRelaxToModel(on: false);
		}, () =>
		{
			RestParams restParams = ComputeRestParams();
			if (!m_wantsRest && m_random.Float(0f, 1f) < restParams.ChancePerSecond * m_subsystemTime.GameTimeDelta)
			{
				m_wantsRest = true;
			}
			if (m_wantsRest && CanLieHere())
			{
				m_stateMachine.TransitionTo("LieDown");
			}
			else
			{
				Vector3 position = m_componentCreature.ComponentBody.Position;
				if (!m_componentPathfinding.Destination.HasValue)
				{
					float num = ((m_random.Float(0f, 1f) < 0.05f) ? m_random.Float(0.4f, 0.6f) : (0f - m_random.Float(0.4f, 0.6f)));
					m_angle = MathUtils.NormalizeAngle(m_angle + num);
					Vector2 vector = Vector2.CreateFromAngle(m_angle);
					Vector3 value = position + new Vector3(vector.X, 0f, vector.Y) * 10f;
					value.Y = EstimateHeight(new Vector2(value.X, value.Z), 8) + m_random.Float(3f, 20f);
					m_componentPathfinding.SetDestination(value, m_random.Float(0.6f, 1.05f), 6f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
					if (m_random.Float(0f, 1f) < 0.15f)
					{
						m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: false);
					}
				}
				else if (m_componentPathfinding.IsStuck)
				{
					m_stateMachine.TransitionTo("Stuck");
				}
			}
		}, null);
		m_stateMachine.AddState("LieDown", () =>
		{
			RestParams restParams = ComputeRestParams();
			m_restTimeLeft = m_random.Float(restParams.DurationMin, restParams.DurationMax);
			IsSleeping = true;
			SignalRelaxToModel(on: true);
			m_sleepImportance = ComputeSleepImportance();
			m_importanceLevel = MathUtils.Max(m_importanceLevel, m_sleepImportance);
			m_componentPathfinding.Stop();
		}, () =>
		{
			m_sleepImportance = ComputeSleepImportance();
			m_importanceLevel = MathUtils.Max(m_importanceLevel, m_sleepImportance);
			if (IsMorningOrDay() || !IsActive || !CanLieHere())
			{
				IsSleeping = false;
				SignalRelaxToModel(on: false);
				m_importanceLevel = 0f;
				m_stateMachine.TransitionTo("Inactive");
			}
			else
			{
				SignalRelaxToModel(on: true);
				m_restTimeLeft -= m_subsystemTime.GameTimeDelta;
				if (m_restTimeLeft <= 0f)
				{
					RestParams restParams = ComputeRestParams();
					if (m_random.Float(0f, 1f) < restParams.ExtendProbability && CanLieHere())
					{
						m_restTimeLeft = m_random.Float(restParams.DurationMin, restParams.DurationMax);
					}
					else
					{
						IsSleeping = false;
						SignalRelaxToModel(on: false);
						m_importanceLevel = 0f;
						m_stateMachine.TransitionTo("Inactive");
					}
				}
			}
		}, () =>
		{
			IsSleeping = false;
			SignalRelaxToModel(on: false);
			m_sleepImportance = 0f;
			m_importanceLevel = 0f;
		});
		m_stateMachine.TransitionTo("Inactive");
	}

	public float EstimateHeight(Vector2 position, int radius)
	{
		int num = 0;
		for (int i = 0; i < 15; i++)
		{
			int x = Terrain.ToCell(position.X) + m_random.Int(-radius, radius);
			int z = Terrain.ToCell(position.Y) + m_random.Int(-radius, radius);
			num = MathUtils.Max(num, m_subsystemTerrain.Terrain.GetTopHeight(x, z));
		}
		return num;
	}

	private bool CanLieHere()
	{
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		if (componentBody.ImmersionFactor > 0.1f)
		{
			return false;
		}
		int x = Terrain.ToCell(componentBody.Position.X);
		int y = Terrain.ToCell(componentBody.Position.Y - 0.1f);
		int z = Terrain.ToCell(componentBody.Position.Z);
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		return BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable;
	}

	private void SignalRelaxToModel(bool on)
	{
		ComponentBirdModel componentBirdModel = Entity.FindComponent<ComponentBirdModel>();
		if (componentBirdModel != null)
		{
			componentBirdModel.RelaxLieOrder = on;
		}
		ComponentFlightlessBirdModel componentFlightlessBirdModel = Entity.FindComponent<ComponentFlightlessBirdModel>();
		if (componentFlightlessBirdModel != null)
		{
			componentFlightlessBirdModel.RelaxLieOrder = on;
		}
	}

	private RestParams ComputeRestParams()
	{
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		bool flag = timeOfDay >= m_subsystemTimeOfDay.SunsetOffset && timeOfDay < 1f;
		bool flag2 = timeOfDay >= 0f && timeOfDay < m_subsystemTimeOfDay.SunriseOffset;
		if (timeOfDay >= m_subsystemTimeOfDay.SunriseOffset && timeOfDay < m_subsystemTimeOfDay.SunsetOffset)
		{
			return new RestParams
			{
				ChancePerSecond = 0f,
				DurationMin = 0f,
				DurationMax = 0f,
				ExtendProbability = 0f
			};
		}
		if (flag)
		{
			return new RestParams
			{
				ChancePerSecond = 0.08f,
				DurationMin = 20f,
				DurationMax = 60f,
				ExtendProbability = 0.8f
			};
		}
		if (flag2)
		{
			return new RestParams
			{
				ChancePerSecond = 0.12f,
				DurationMin = 30f,
				DurationMax = 120f,
				ExtendProbability = 0.9f
			};
		}
		return new RestParams
		{
			ChancePerSecond = 0f,
			DurationMin = 0f,
			DurationMax = 0f,
			ExtendProbability = 0f
		};
	}

	private bool IsMorningOrDay()
	{
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		if (timeOfDay >= m_subsystemTimeOfDay.SunriseOffset)
		{
			return timeOfDay < m_subsystemTimeOfDay.SunsetOffset;
		}
		return false;
	}

	private float ComputeSleepImportance()
	{
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		bool num = timeOfDay >= m_subsystemTimeOfDay.SunsetOffset && timeOfDay < 1f;
		bool flag = timeOfDay >= 0f && timeOfDay < m_subsystemTimeOfDay.SunriseOffset;
		if (num)
		{
			return 5.5f;
		}
		if (flag)
		{
			return 6.2f;
		}
		return 0f;
	}
}
