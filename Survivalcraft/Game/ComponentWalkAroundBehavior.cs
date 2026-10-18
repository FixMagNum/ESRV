using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentWalkAroundBehavior : ComponentBehavior, IUpdateable
{
	private struct RestParams
	{
		public float ChancePerSecond;

		public float DurationMin;

		public float DurationMax;

		public float ExtendProbability;
	}

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemTime m_subsystemTime;

	public ComponentCreature m_componentCreature;

	public ComponentPathfinding m_componentPathfinding;

	public Random m_random;

	public float m_importanceLevel;

	private float m_restChancePerSecond = 0.03f;

	private float m_restDurationMin = 3f;

	private float m_restDurationMax = 8f;

	private bool m_wantsRest;

	private float m_restTimeLeft;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	private float m_sleepImportance;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public bool IsSleeping { get; private set; }

	public void Update(float dt)
	{
		m_stateMachine.Update();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_stateMachine.AddState("Inactive", () =>
		{
			m_importanceLevel = m_random.Float(0f, 1f);
			m_wantsRest = false;
			IsSleeping = false;
		}, () =>
		{
			if (m_random.Float(0f, 1f) < 0.05f * m_subsystemTime.GameTimeDelta)
			{
				m_importanceLevel = m_random.Float(1f, 2f);
			}
			RestParams restParams = ComputeRestParams();
			if (!m_wantsRest && m_random.Float(0f, 1f) < restParams.ChancePerSecond * m_subsystemTime.GameTimeDelta)
			{
				m_wantsRest = true;
				m_importanceLevel = MathUtils.Max(m_importanceLevel, m_random.Float(1f, 2f));
			}
			if (IsActive)
			{
				if (m_wantsRest && CanLieHere())
				{
					m_stateMachine.TransitionTo("LieDown");
				}
				else
				{
					m_stateMachine.TransitionTo("Walk");
				}
			}
		}, () =>
		{
			m_wantsRest = false;
		});
		m_stateMachine.AddState("Walk", () =>
		{
			float speed = ((m_componentCreature.ComponentBody.ImmersionFactor > 0.5f) ? 1f : m_random.Float(0.25f, 0.35f));
			m_componentPathfinding.SetDestination(FindDestination(), speed, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			IsSleeping = false;
		}, () =>
		{
			if (m_componentPathfinding.IsStuck || !IsActive)
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			else if (m_componentPathfinding.Destination.HasValue)
			{
				if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta)
				{
					m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: false);
				}
				m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
			}
			else if (m_random.Float(0f, 1f) < 0.5f)
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			else
			{
				m_stateMachine.TransitionTo(null);
				m_stateMachine.TransitionTo("Walk");
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
		}, () =>
		{
			m_sleepImportance = ComputeSleepImportance();
			m_importanceLevel = MathUtils.Max(m_importanceLevel, m_sleepImportance);
			if (IsMorningOrDay())
			{
				IsSleeping = false;
				SignalRelaxToModel(on: false);
				m_importanceLevel = 0f;
				m_stateMachine.TransitionTo("Inactive");
			}
			else if (!IsActive || !CanLieHere())
			{
				IsSleeping = false;
				SignalRelaxToModel(on: false);
				m_importanceLevel = 0f;
				m_stateMachine.TransitionTo("Inactive");
			}
			else
			{
				SignalRelaxToModel(on: true);
				RestParams restParams = ComputeRestParams();
				m_restTimeLeft -= m_subsystemTime.GameTimeDelta;
				if (m_restTimeLeft <= 0f)
				{
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

	public Vector3 FindDestination()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		float num = 0f;
		Vector3 result = position;
		for (int i = 0; i < 16; i++)
		{
			Vector2 vector = Vector2.Normalize(m_random.Vector2(1f)) * m_random.Float(6f, 12f);
			Vector3 vector2 = new Vector3(position.X + vector.X, 0f, position.Z + vector.Y);
			vector2.Y = m_subsystemTerrain.Terrain.GetTopHeight(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Z)) + 1;
			float num2 = ScoreDestination(vector2);
			if (num2 > num)
			{
				num = num2;
				result = vector2;
			}
		}
		return result;
	}

	public float ScoreDestination(Vector3 destination)
	{
		float num = 8f - MathUtils.Abs(m_componentCreature.ComponentBody.Position.Y - destination.Y);
		if (m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(destination.X), Terrain.ToCell(destination.Y) - 1, Terrain.ToCell(destination.Z)) == 18)
		{
			num -= 5f;
		}
		return num;
	}

	public ComponentWalkAroundBehavior()
	{
		m_random = new Random();
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
		ComponentFourLeggedModel componentFourLeggedModel = Entity.FindComponent<ComponentFourLeggedModel>();
		if (componentFourLeggedModel != null)
		{
			componentFourLeggedModel.RelaxLieOrder = on;
		}
		ComponentFlightlessBirdModel componentFlightlessBirdModel = Entity.FindComponent<ComponentFlightlessBirdModel>();
		if (componentFlightlessBirdModel != null)
		{
			componentFlightlessBirdModel.RelaxLieOrder = on;
		}
	}

	private RestParams ComputeRestParams()
	{
		float modelMesh = m_subsystemTimeOfDay.TimeOfDay;
		bool modelMesh2 = (m_componentCreature.Category & (CreatureCategory.LandPredator | CreatureCategory.WaterPredator)) > (CreatureCategory)0;
		bool boneAbsoluteTransform = modelMesh >= 0f && modelMesh < m_subsystemTimeOfDay.SunriseOffset;
		bool num = modelMesh >= m_subsystemTimeOfDay.SunriseOffset && modelMesh < m_subsystemTimeOfDay.DayOffset;
		bool boneAbsoluteTransform2 = modelMesh >= m_subsystemTimeOfDay.DayOffset && modelMesh < m_subsystemTimeOfDay.SunsetOffset;
		bool i = modelMesh >= m_subsystemTimeOfDay.SunsetOffset && modelMesh < 1f;
		if (num | boneAbsoluteTransform2)
		{
			return new RestParams
			{
				ChancePerSecond = 0f,
				DurationMin = 0f,
				DurationMax = 0f,
				ExtendProbability = 0f
			};
		}
		if (i)
		{
			if (modelMesh2)
			{
				return new RestParams
				{
					ChancePerSecond = 0.01f,
					DurationMin = 5f,
					DurationMax = 12f,
					ExtendProbability = 0.35f
				};
			}
			return new RestParams
			{
				ChancePerSecond = 0.06f,
				DurationMin = 15f,
				DurationMax = 40f,
				ExtendProbability = 0.75f
			};
		}
		if (!boneAbsoluteTransform)
		{
			return new RestParams
			{
				ChancePerSecond = 0f,
				DurationMin = 0f,
				DurationMax = 0f,
				ExtendProbability = 0f
			};
		}
		if (modelMesh2)
		{
			return new RestParams
			{
				ChancePerSecond = 0.03f,
				DurationMin = 8f,
				DurationMax = 20f,
				ExtendProbability = 0.4f
			};
		}
		return new RestParams
		{
			ChancePerSecond = 0.15f,
			DurationMin = 30f,
			DurationMax = 120f,
			ExtendProbability = 0.9f
		};
	}

	private bool IsMorningOrDay()
	{
		float num = m_subsystemTimeOfDay.TimeOfDay;
		if (num >= m_subsystemTimeOfDay.SunriseOffset)
		{
			return num < m_subsystemTimeOfDay.SunsetOffset;
		}
		return false;
	}

	private float ComputeSleepImportance()
	{
		bool flag = (m_componentCreature.Category & (CreatureCategory.LandPredator | CreatureCategory.WaterPredator)) != 0;
		float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
		bool num = timeOfDay >= m_subsystemTimeOfDay.SunsetOffset && timeOfDay < 1f;
		bool flag2 = timeOfDay >= 0f && timeOfDay < m_subsystemTimeOfDay.SunriseOffset;
		if (num)
		{
			if (!flag)
			{
				return 5.8f;
			}
			return 5.2f;
		}
		if (flag2)
		{
			if (!flag)
			{
				return 6.5f;
			}
			return 5.1f;
		}
		return 0f;
	}
}
