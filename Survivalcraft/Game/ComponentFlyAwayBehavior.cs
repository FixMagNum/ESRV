using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFlyAwayBehavior : ComponentBehavior, IUpdateable, INoiseListener
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemTime m_subsystemTime;

	public SubsystemNoise m_subsystemNoise;

	public ComponentCreature m_componentCreature;

	public ComponentPathfinding m_componentPathfinding;

	public DynamicArray<ComponentBody> m_componentBodies = new DynamicArray<ComponentBody>();

	public Random m_random = new Random();

	public float m_importanceLevel;

	public double m_nextUpdateTime;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public override bool IsActive
	{
		set
		{
			base.IsActive = value;
			if (IsActive)
			{
				m_nextUpdateTime = 0.0;
			}
		}
	}

	public void Update(float dt)
	{
		if (m_componentCreature.ComponentHealth.HealthChange < 0f)
		{
			m_stateMachine.TransitionTo("DangerDetected");
		}
		if (m_subsystemTime.GameTime >= m_nextUpdateTime)
		{
			m_nextUpdateTime = m_subsystemTime.GameTime + (double)m_random.Float(0.5f, 1f);
			m_stateMachine.Update();
		}
	}

	public void HearNoise(ComponentBody sourceBody, Vector3 sourcePosition, float loudness)
	{
		if (loudness >= 0.25f && m_stateMachine.CurrentState != "RunningAway")
		{
			m_stateMachine.TransitionTo("DangerDetected");
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		componentBody.CollidedWithBody = (Action<ComponentBody>)Delegate.Combine(componentBody.CollidedWithBody, (Action<ComponentBody>)delegate
		{
			if (m_stateMachine.CurrentState != "RunningAway")
			{
				m_stateMachine.TransitionTo("DangerDetected");
			}
		});
		m_stateMachine.AddState("LookingForDanger", null, () =>
		{
			if (ScanForDanger())
			{
				m_stateMachine.TransitionTo("DangerDetected");
			}
		}, null);
		m_stateMachine.AddState("DangerDetected", () =>
		{
			m_importanceLevel = ((m_componentCreature.ComponentHealth.Health < 0.33f) ? 300 : 100);
			m_nextUpdateTime = 0.0;
		}, () =>
		{
			if (IsActive)
			{
				m_stateMachine.TransitionTo("RunningAway");
				m_nextUpdateTime = 0.0;
			}
		}, null);
		m_stateMachine.AddState("RunningAway", () =>
		{
			m_componentPathfinding.SetDestination(FindSafePlace(), 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			m_subsystemAudio.PlayRandomSound("Audio/Creatures/Wings", 0.8f, m_random.Float(-0.1f, 0.2f), m_componentCreature.ComponentBody.Position, 3f, autoDelay: true);
			m_componentCreature.ComponentCreatureSounds.PlayPainSound();
			m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 6f);
		}, () =>
		{
			if (!IsActive || !m_componentPathfinding.Destination.HasValue || m_componentPathfinding.IsStuck)
			{
				m_stateMachine.TransitionTo("LookingForDanger");
			}
			else if (ScoreSafePlace(m_componentCreature.ComponentBody.Position, m_componentPathfinding.Destination.Value, null) < 4f)
			{
				m_componentPathfinding.SetDestination(FindSafePlace(), 1f, 0.5f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			}
		}, () =>
		{
			m_importanceLevel = 0f;
		});
		m_stateMachine.TransitionTo("LookingForDanger");
	}

	public bool ScanForDanger()
	{
		Matrix widget = m_componentCreature.ComponentBody.Matrix;
		Vector3 translation = widget.Translation;
		Vector3 forward = widget.Forward;
		if (ScoreSafePlace(translation, translation, forward) < 7f)
		{
			return true;
		}
		return false;
	}

	public Vector3 FindSafePlace()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		float num = float.NegativeInfinity;
		Vector3 result = position;
		for (int i = 0; i < 20; i++)
		{
			int num2 = Terrain.ToCell(position.X + m_random.Float(-35f, 35f));
			int num3 = Terrain.ToCell(position.Z + m_random.Float(-35f, 35f));
			for (int num4 = 255; num4 >= 0; num4--)
			{
				int cellContents = m_subsystemTerrain.Terrain.GetCellContents(num2, num4, num3);
				if (BlocksManager.Blocks[cellContents].IsCollidable || cellContents == 18)
				{
					Vector3 vector = new Vector3((float)num2 + 0.5f, (float)num4 + 1.1f, (float)num3 + 0.5f);
					float num5 = ScoreSafePlace(position, vector, null);
					if (num5 > num)
					{
						num = num5;
						result = vector;
					}
					break;
				}
			}
		}
		return result;
	}

	public float ScoreSafePlace(Vector3 currentPosition, Vector3 safePosition, Vector3? lookDirection)
	{
		float jsonObject = 16f;
		Vector3 num = m_componentCreature.ComponentBody.Position;
		m_componentBodies.Clear();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2(num.X, num.Z), 16f, m_componentBodies);
		for (int i = 0; i < m_componentBodies.Count; i++)
		{
			ComponentBody componentBody = m_componentBodies.Array[i];
			if (!IsPredator(componentBody.Entity))
			{
				continue;
			}
			Vector3 position = componentBody.Position;
			Vector3 v = safePosition - position;
			if (!lookDirection.HasValue || 0f - Vector3.Dot(lookDirection.Value, v) > 0f)
			{
				if (v.Y >= 4f)
				{
					v *= 2f;
				}
				jsonObject = MathUtils.Min(jsonObject, v.Length());
			}
		}
		float text = Vector3.Distance(currentPosition, safePosition);
		if (text < 8f)
		{
			return jsonObject * 0.5f;
		}
		return jsonObject * MathUtils.Lerp(1f, 0.75f, MathUtils.Saturate(text / 20f));
	}

	public bool IsPredator(Entity entity)
	{
		if (entity != Entity)
		{
			ComponentCreature canvasWidget = entity.FindComponent<ComponentCreature>();
			if (canvasWidget != null && (canvasWidget.Category == CreatureCategory.LandPredator || canvasWidget.Category == CreatureCategory.WaterPredator || canvasWidget.Category == CreatureCategory.LandOther))
			{
				return true;
			}
		}
		return false;
	}
}
