using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentPeacefulFlyAroundBehavior : ComponentBehavior, IUpdateable
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

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public bool IsSleeping { get; private set; }

	public void Update(float dt)
	{
		if (string.IsNullOrEmpty(m_stateMachine.CurrentState))
		{
			m_stateMachine.TransitionTo("Inactive");
		}
		if (m_random.Float(0f, 1f) < 0.2f * dt)
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
			if (IsActive)
			{
				RestParams restParams = ComputeRestParams();
				if (!m_wantsRest && m_random.Float(0f, 1f) < restParams.ChancePerSecond * m_subsystemTime.GameTimeDelta)
				{
					m_wantsRest = true;
					m_importanceLevel = MathUtils.Max(m_importanceLevel, 1.5f);
				}
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
				if (FindTargetSoilOrCrop(position, out var targetPoint))
				{
					Vector3 value = new Vector3(targetPoint.X, targetPoint.Y, targetPoint.Z);
					m_componentPathfinding.SetDestination(value, m_random.Float(0.6f, 1.05f), 6f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
				}
				else if (!m_componentPathfinding.Destination.HasValue)
				{
					float num = ((m_random.Float(0f, 1f) < 0.05f) ? m_random.Float(0.4f, 0.6f) : (0f - m_random.Float(0.4f, 0.6f)));
					m_angle = MathUtils.NormalizeAngle(m_angle + num);
					Vector2 vector = Vector2.CreateFromAngle(m_angle);
					Vector3 value2 = position + new Vector3(vector.X, 0f, vector.Y) * 10f;
					value2.Y = EstimateHeight(new Vector2(value2.X, value2.Z), 8) + m_random.Float(1f, 2f);
					m_componentPathfinding.SetDestination(value2, m_random.Float(0.6f, 1.05f), 6f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
					if (m_random.Float(0f, 1f) < 0.15f)
					{
						m_componentCreature.ComponentCreatureSounds.PlayIdleSound(skipIfRecentlyPlayed: false);
					}
				}
				else if (m_componentPathfinding.IsStuck)
				{
					m_stateMachine.TransitionTo("Stuck");
				}
				if (m_componentPathfinding.Destination.HasValue && Vector3.Distance(m_componentCreature.ComponentBody.Position, m_componentPathfinding.Destination.Value) < 1f)
				{
					OnReachTarget(targetPoint);
				}
			}
		}, null);
		m_stateMachine.AddState("LieDown", () =>
		{
			RestParams restParams = ComputeRestParams();
			m_restTimeLeft = m_random.Float(restParams.DurationMin, restParams.DurationMax);
			IsSleeping = true;
			SignalRelaxToModel(on: true);
			m_componentPathfinding.Stop();
		}, () =>
		{
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

	private bool FindTargetSoilOrCrop(Vector3 position, out Point3 targetPoint)
	{
		int num = 20;
		float num2 = float.MaxValue;
		Point3 point = default;
		bool result = false;
		for (int i = -num; i <= num; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				int num3 = Terrain.ToCell(position.X) + i;
				int num4 = Terrain.ToCell(position.Y);
				int num5 = Terrain.ToCell(position.Z) + j;
				int num6 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(num3, num4, num5));
				if (num6 == 168 || IsCrop(num6))
				{
					float num7 = Vector3.Distance(position, new Vector3(num3, num4, num5));
					if (num7 < num2)
					{
						num2 = num7;
						point = new Point3(num3, num4, num5);
						result = true;
					}
				}
			}
		}
		targetPoint = point;
		return result;
	}

	private void OnReachTarget(Point3 targetPoint)
	{
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(targetPoint.X, targetPoint.Y, targetPoint.Z);
		int num = Terrain.ExtractContents(cellValue);
		if (num == 168)
		{
			m_subsystemTerrain.ChangeCell(targetPoint.X, targetPoint.Y, targetPoint.Z, Terrain.ReplaceContents(cellValue, 2));
		}
		else if (IsCrop(num))
		{
			m_subsystemTerrain.ChangeCell(targetPoint.X, targetPoint.Y, targetPoint.Z, 0);
		}
	}

	private bool IsCrop(int value)
	{
		if (value != 204 && value != 174)
		{
			return value == 131;
		}
		return true;
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
				DurationMin = 15f,
				DurationMax = 45f,
				ExtendProbability = 0.75f
			};
		}
		if (flag2)
		{
			return new RestParams
			{
				ChancePerSecond = 0.12f,
				DurationMin = 25f,
				DurationMax = 90f,
				ExtendProbability = 0.85f
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
}
