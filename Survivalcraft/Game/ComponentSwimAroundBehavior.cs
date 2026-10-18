using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentSwimAroundBehavior : ComponentBehavior, IUpdateable
{
	public SubsystemTerrain m_subsystemTerrain;

	public ComponentCreature m_componentCreature;

	public ComponentPathfinding m_componentPathfinding;

	public float m_importanceLevel;

	public Random m_random;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public SubsystemSky m_subsystemSky;

	private bool m_jumpingFish = true;

	private float m_jumpTimer;

	private float m_jumpMin = 5f;

	private float m_jumpMax = 20f;

	private bool m_surfaceApproach;

	private float m_surfaceApproachTime;

	private Vector3? m_surfacePoint;

	private ComponentFishOutOfWaterBehavior m_outOfWater;

	public SubsystemFishingFloats m_subsystemFishingFloats;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public void Update(float dt)
	{
		if (string.IsNullOrEmpty(m_stateMachine.CurrentState))
		{
			m_stateMachine.TransitionTo("Inactive");
		}
		if (m_random.Float(0f, 1f) < 0.05f * dt)
		{
			m_importanceLevel = m_random.Float(1f, 3f);
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
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
		m_jumpingFish = valuesDictionary.GetValue("JumpingFish", defaultValue: true);
		m_outOfWater = Entity.FindComponent<ComponentFishOutOfWaterBehavior>();
		m_subsystemFishingFloats = Project.FindSubsystem<SubsystemFishingFloats>(throwOnError: true);
		ResetJumpTimer();
		m_stateMachine.AddState("Inactive", null, () =>
		{
			if (IsActive)
			{
				m_stateMachine.TransitionTo("Swim");
			}
		}, null);
		m_stateMachine.AddState("Stuck", () =>
		{
			if (m_random.Float(0f, 1f) < 0.5f)
			{
				m_importanceLevel = 1f;
			}
			m_stateMachine.TransitionTo("Swim");
		}, null, null);
		m_stateMachine.AddState("Swim", () =>
		{
			m_componentPathfinding.Stop();
		}, () =>
		{
			if (m_jumpingFish)
			{
				UpdateJumpLogic();
			}
			if (m_componentPathfinding.Destination.HasValue)
			{
				if (m_componentPathfinding.IsStuck)
				{
					m_stateMachine.TransitionTo("Stuck");
				}
			}
			else
			{
				Vector3? destination = FindDestination();
				if (destination.HasValue)
				{
					m_componentPathfinding.SetDestination(destination, m_random.Float(0.3f, 0.4f), 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
				}
				else
				{
					m_importanceLevel = 1f;
				}
			}
		}, null);
	}

	public Vector3? FindDestination()
	{
		Vector3 num = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
		float block = 2f;
		Vector3? result = null;
		float num2 = m_random.Float(10f, 16f);
		float depthBias = GetDepthBias(num);
		for (int i = 0; i < 16; i++)
		{
			Vector2 vector = m_random.Vector2(1f, 1f);
			float x = m_random.Float(-0.9f, 1f);
			x = MathUtils.Lerp(x, -1f, depthBias);
			float y = 0.3f * MathUtils.Clamp(x, -1f, 1f);
			Vector3 vector2 = Vector3.Normalize(new Vector3(vector.X, y, vector.Y));
			Vector3 vector3 = num + num2 * vector2;
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(num, vector3, useInteractionBoxes: false, skipAirBlocks: false, (int value, float d) =>
			{
				int num3 = Terrain.ExtractContents(value);
				return !(BlocksManager.Blocks[num3] is FocalorsWaterBlock);
			});
			if (!terrainRaycastResult.HasValue)
			{
				if (num2 > block)
				{
					result = vector3;
					block = num2;
				}
			}
			else if (terrainRaycastResult.Value.Distance > block)
			{
				result = num + vector2 * terrainRaycastResult.Value.Distance;
				block = terrainRaycastResult.Value.Distance;
			}
		}
		return result;
	}

	public ComponentSwimAroundBehavior()
	{
		m_importanceLevel = 1f;
		m_random = new Random();
	}

	private float GetDepthBias(Vector3 pos)
	{
		float timeOfDayMultiplier = GetTimeOfDayMultiplier();
		float weatherMultiplier = GetWeatherMultiplier(pos);
		float name = GetWindMultiplier();
		float num = MathUtils.Clamp(timeOfDayMultiplier * weatherMultiplier * name, 0f, 8f);
		return MathUtils.Saturate(1f - num / 8f);
	}

	private float GetTimeOfDayMultiplier()
	{
		if (m_subsystemTimeOfDay == null)
		{
			return 1f;
		}
		float num = m_subsystemTimeOfDay.TimeOfDay;
		float num2 = m_subsystemTimeOfDay.SunriseOffset;
		float text = m_subsystemTimeOfDay.SunsetOffset;
		float num3 = num2 + 0.05f;
		float num4 = text - 0.05f;
		if (MathUtils.Abs(num - num2) <= 0.07f)
		{
			return 4f;
		}
		if (MathUtils.Abs(num - text) <= 0.07f)
		{
			return 3f;
		}
		if (num >= num3 && num <= num4)
		{
			return 0.4f;
		}
		float num5 = ((m_componentCreature != null && m_componentCreature.Category == CreatureCategory.WaterPredator) ? 4f : 0.6f);
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
			return MathUtils.Lerp(num5, 4f, f);
		}
		return num5;
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

	private void UpdateJumpLogic()
	{
		float frameDuration = Time.FrameDuration;
		if (m_surfaceApproach)
		{
			m_surfaceApproachTime -= frameDuration;
			m_surfacePoint = FindSurfacePoint(m_componentCreature.ComponentBody.Position);
			if (m_surfacePoint.HasValue)
			{
				Vector3 value = m_surfacePoint.Value - new Vector3(0f, 0.25f, 0f);
				m_componentPathfinding.SetDestination(value, 0.5f, 0.6f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			}
			if (m_componentCreature.ComponentBody.ImmersionFactor > 0.5f && IsJustBelowSurface())
			{
				if (m_outOfWater != null)
				{
					m_outOfWater.IsBend = true;
				}
				if (m_subsystemFishingFloats != null && m_subsystemFishingFloats.GetFatigueForBody(m_componentCreature.ComponentBody) > 0f)
				{
					m_componentCreature.ComponentLocomotion.JumpOrder = 0f;
					m_componentCreature.ComponentLocomotion.SwimOrder = 1f * -Vector3.UnitY;
				}
				else
				{
					m_componentCreature.ComponentLocomotion.JumpOrder = m_random.Float(0.6f, 1f);
					m_componentCreature.ComponentLocomotion.SwimOrder = 0.6f * m_componentCreature.ComponentBody.Matrix.Forward;
				}
				m_surfaceApproach = false;
				if (m_outOfWater != null)
				{
					m_outOfWater.IsBend = false;
				}
				ResetJumpTimer();
			}
			else if (m_surfaceApproachTime <= 0f)
			{
				m_surfaceApproach = false;
				if (m_outOfWater != null)
				{
					m_outOfWater.IsBend = false;
				}
				ResetJumpTimer();
			}
			return;
		}
		m_jumpTimer -= frameDuration;
		if (m_jumpTimer > 0f)
		{
			return;
		}
		float depthBias = GetDepthBias(m_componentCreature.ComponentBody.Position);
		float num = MathUtils.Saturate(1f - depthBias);
		float num2 = ((GetWindMultiplier() <= 0f) ? 0f : (0.1f + 0.7f * num));
		if (m_random.Float(0f, 1f) < num2)
		{
			m_surfacePoint = FindSurfacePoint(m_componentCreature.ComponentBody.Position);
			if (m_surfacePoint.HasValue)
			{
				m_surfaceApproach = true;
				m_surfaceApproachTime = m_random.Float(1f, 3f);
				if (m_outOfWater != null)
				{
					m_outOfWater.IsBend = true;
				}
				return;
			}
		}
		ResetJumpTimer();
	}

	private bool IsJustBelowSurface()
	{
		Vector3 num = m_componentCreature.ComponentBody.Position;
		Vector3? vector = FindSurfacePoint(num);
		if (!vector.HasValue)
		{
			return false;
		}
		float num2 = vector.Value.Y - num.Y;
		if (num2 > 0f)
		{
			return num2 <= 0.35f;
		}
		return false;
	}

	private Vector3? FindSurfacePoint(Vector3 from)
	{
		Vector3 end = from + new Vector3(0f, 8f, 0f);
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(from, end, useInteractionBoxes: false, skipAirBlocks: false, (int value, float d) =>
		{
			int num = Terrain.ExtractContents(value);
			return !(BlocksManager.Blocks[num] is FocalorsWaterBlock);
		});
		if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.Distance > 0.1f)
		{
			return from + new Vector3(0f, terrainRaycastResult.Value.Distance, 0f);
		}
		return null;
	}

	private void ResetJumpTimer()
	{
		m_jumpTimer = m_random.Float(m_jumpMin, m_jumpMax);
	}
}
