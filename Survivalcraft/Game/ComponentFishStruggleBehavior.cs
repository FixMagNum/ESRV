using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFishStruggleBehavior : ComponentBehavior, IUpdateable
{
	public enum StruggleMode
	{
		Dart,
		Power
	}

	public enum AttemptQuality
	{
		Green,
		Yellow,
		Red
	}

	public SubsystemTime m_subsystemTime;

	public StruggleMode Mode;

	public float ChangeIntervalMin;

	public float ChangeIntervalMax;

	public float BurstDurationMin;

	public float BurstDurationMax;

	public float VerticalChance;

	public float VerticalStrength;

	public float AlongLineBias;

	public float StrengthBase;

	public float StrengthDistanceScale;

	public float SideAngleMinDeg;

	public float SideAngleMaxDeg;

	public float ActivePriority;

	public float ExpectedMaxLineLength;

	public Random m_random = new Random();

	public ComponentCreature m_componentCreature;

	public ComponentLocomotion m_locomotion;

	private float m_importanceLevel;

	private float m_forceActiveTime;

	private float m_burstTimeLeft;

	private float m_changeTimeLeft;

	private float m_severity = 1f;

	private float m_attemptMul = 1f;

	private float m_maxLineLen = 15f;

	private Vector3 m_targetPos;

	private Vector3 m_currentDir;

	private float m_intensity = 1f;

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentPathfinding m_path;

	private float m_repathCooldown;

	public float ImmediateRepathDeg;

	private Vector3 m_lastDir;

	public SubsystemFishingFloats m_subsystemFishingFloats;

	public float FatigueStopAt;

	public float InactiveFadeStartFatigue;

	public float BurstDurMulAtFatigue1;

	public float ChangeIntervalMulAtFatigue1;

	public float AngleRangeMulAtFatigue1;

	public float AlongLineBiasAddAtFatigue1;

	public float VerticalChanceMulAtFatigue1;

	public float VerticalStrengthMulAtFatigue1;

	public float MagnitudeMulStartAtFatigue;

	public float MagnitudeMulAtFatigue1;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override float ImportanceLevel => m_importanceLevel;

	public void ActivateStruggle(Vector3 playerPosition, float severity01, float seconds, AttemptQuality quality, float maxLineLength)
	{
		m_targetPos = playerPosition;
		m_severity = MathUtils.Saturate(severity01);
		m_attemptMul = QualityToMul(quality);
		m_maxLineLen = MathUtils.Max(0.01f, maxLineLength);
		m_forceActiveTime = MathUtils.Max(m_forceActiveTime, MathUtils.Max(0.05f, seconds));
		m_importanceLevel = MathUtils.Max(m_importanceLevel, ActivePriority);
		m_changeTimeLeft = 0f;
		if (string.IsNullOrEmpty(m_stateMachine.CurrentState))
		{
			m_stateMachine.TransitionTo("Inactive");
		}
		if (m_stateMachine.CurrentState != "Burst")
		{
			m_stateMachine.TransitionTo("Burst");
		}
	}

	public void ActivateStruggle(Vector3 playerPosition)
	{
		ActivateStruggle(playerPosition, 1f, 1f, AttemptQuality.Yellow, ExpectedMaxLineLength);
	}

	public void DeactivateStruggle()
	{
		m_forceActiveTime = 0f;
		m_importanceLevel = 0f;
		ClearOrders();
		if (m_path != null)
		{
			m_path.Stop();
		}
		if (m_stateMachine.CurrentState != "Inactive")
		{
			m_stateMachine.TransitionTo("Inactive");
		}
	}

	public void Update(float dt)
	{
		m_stateMachine.Update();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_locomotion = Entity.FindComponent<ComponentLocomotion>(throwOnError: true);
		m_path = Entity.FindComponent<ComponentPathfinding>();
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemFishingFloats = Project.FindSubsystem<SubsystemFishingFloats>(throwOnError: true);
		Mode = ((string.Compare(valuesDictionary.GetValue<string>("Mode"), "Dart", ignoreCase: true) != 0) ? StruggleMode.Power : StruggleMode.Dart);
		ChangeIntervalMin = valuesDictionary.GetValue<float>("ChangeIntervalMin");
		ChangeIntervalMax = valuesDictionary.GetValue<float>("ChangeIntervalMax");
		BurstDurationMin = valuesDictionary.GetValue<float>("BurstDurationMin");
		BurstDurationMax = valuesDictionary.GetValue<float>("BurstDurationMax");
		VerticalChance = valuesDictionary.GetValue<float>("VerticalChance");
		VerticalStrength = valuesDictionary.GetValue<float>("VerticalStrength");
		AlongLineBias = MathUtils.Saturate(valuesDictionary.GetValue<float>("AlongLineBias"));
		StrengthBase = valuesDictionary.GetValue<float>("StrengthBase");
		StrengthDistanceScale = valuesDictionary.GetValue<float>("StrengthDistanceScale");
		SideAngleMinDeg = valuesDictionary.GetValue<float>("SideAngleMin");
		SideAngleMaxDeg = valuesDictionary.GetValue<float>("SideAngleMax");
		ActivePriority = valuesDictionary.GetValue<float>("ActivePriority");
		ExpectedMaxLineLength = valuesDictionary.GetValue<float>("ExpectedMaxLineLength");
		ImmediateRepathDeg = valuesDictionary.GetValue<float>("ImmediateRepathDeg");
		FatigueStopAt = valuesDictionary.GetValue<float>("FatigueStopAt");
		InactiveFadeStartFatigue = valuesDictionary.GetValue<float>("InactiveFadeStartFatigue");
		BurstDurMulAtFatigue1 = valuesDictionary.GetValue<float>("BurstDurMulAtFatigue1");
		ChangeIntervalMulAtFatigue1 = valuesDictionary.GetValue<float>("ChangeIntervalMulAtFatigue1");
		AngleRangeMulAtFatigue1 = valuesDictionary.GetValue<float>("AngleRangeMulAtFatigue1");
		AlongLineBiasAddAtFatigue1 = valuesDictionary.GetValue<float>("AlongLineBiasAddAtFatigue1");
		VerticalChanceMulAtFatigue1 = valuesDictionary.GetValue<float>("VerticalChanceMulAtFatigue1");
		VerticalStrengthMulAtFatigue1 = valuesDictionary.GetValue<float>("VerticalStrengthMulAtFatigue1");
		MagnitudeMulStartAtFatigue = valuesDictionary.GetValue<float>("MagnitudeMulStartAtFatigue");
		MagnitudeMulAtFatigue1 = valuesDictionary.GetValue<float>("MagnitudeMulAtFatigue1");
		m_stateMachine.AddState("Inactive", () =>
		{
			ClearOrders();
			if (m_path != null)
			{
				m_path.Stop();
			}
			m_repathCooldown = 0f;
			m_lastDir = Vector3.Zero;
			m_importanceLevel = 0f;
		}, () =>
		{
			if (GetFatigue() >= FatigueStopAt)
			{
				m_forceActiveTime = 0f;
				m_importanceLevel = 0f;
			}
			else if (m_forceActiveTime > 0f)
			{
				m_stateMachine.TransitionTo("Burst");
			}
		}, null);
		m_stateMachine.AddState("Burst", () =>
		{
			float f = MathUtils.Saturate(GetFatigue());
			float num = MathUtils.Lerp(1f, BurstDurMulAtFatigue1, f);
			m_burstTimeLeft = RandomRange(BurstDurationMin, BurstDurationMax) * num;
			m_changeTimeLeft = 0f;
			m_repathCooldown = 0f;
			m_importanceLevel = ActivePriority;
		}, () =>
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			float num = MathUtils.Saturate(GetFatigue());
			float num2 = MathUtils.Saturate(1f - MathUtils.Saturate((num - InactiveFadeStartFatigue) / Math.Max(1E-06f, 1f - InactiveFadeStartFatigue)));
			m_forceActiveTime -= gameTimeDelta;
			if (m_forceActiveTime <= 0f || num2 <= 0.01f)
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			else
			{
				m_burstTimeLeft -= gameTimeDelta;
				m_changeTimeLeft -= gameTimeDelta;
				if (m_changeTimeLeft <= 0f)
				{
					float num3 = MathUtils.Lerp(1f, ChangeIntervalMulAtFatigue1, num);
					m_changeTimeLeft = RandomRange(ChangeIntervalMin, ChangeIntervalMax) * num3;
					Vector3 vector = ComputeBurstDirection();
					if (m_lastDir.LengthSquared() > 1E-06f)
					{
						if (Vector3.Dot(new Vector3(m_lastDir.X, 0f, m_lastDir.Z).Normalized(), new Vector3(vector.X, 0f, vector.Z).Normalized()) > 0.98f)
						{
							float yawRad = MathUtils.DegToRad(RandomRange(20f, 45f)) * ((m_random.Float(0f, 1f) < 0.5f) ? (-1f) : 1f);
							vector = RotateHoriz(vector, yawRad).Normalized();
						}
						if (RadToDeg(AngleHoriz(m_lastDir, vector)) >= ImmediateRepathDeg)
						{
							m_repathCooldown = 0f;
						}
					}
					m_currentDir = vector;
					m_lastDir = vector;
				}
				float magnitude = ComputeBurstMagnitude();
				if (m_componentCreature.ComponentBody.ImmersionFactor > 0.3f)
				{
					DriveWithPathfinding(m_currentDir, magnitude, gameTimeDelta);
				}
				else
				{
					ApplyOrders(m_currentDir, magnitude);
				}
				m_importanceLevel = ActivePriority * num2;
				if (m_burstTimeLeft <= 0f)
				{
					if (m_path != null)
					{
						m_path.Stop();
					}
					ClearOrders();
					float num4 = MathUtils.Lerp(1f, BurstDurMulAtFatigue1, num);
					m_burstTimeLeft = RandomRange(BurstDurationMin, BurstDurationMax) * num4;
					m_changeTimeLeft = 0.05f;
				}
			}
		}, () =>
		{
			ClearOrders();
			if (m_path != null)
			{
				m_path.Stop();
			}
			m_repathCooldown = 0f;
			m_lastDir = Vector3.Zero;
			m_importanceLevel = 0f;
		});
		m_stateMachine.TransitionTo("Inactive");
	}

	private float ComputeBurstMagnitude()
	{
		float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, m_targetPos);
		float num2 = MathUtils.Saturate((m_maxLineLen > 0f) ? (num / m_maxLineLen) : 0f);
		float num3 = (StrengthBase + StrengthDistanceScale * num2) * MathUtils.Lerp(1f, 1.25f, m_severity) * m_attemptMul;
		float num4 = MathUtils.Saturate(GetFatigue());
		float f = 0f;
		if (num4 >= MagnitudeMulStartAtFatigue)
		{
			f = MathUtils.Saturate((num4 - MagnitudeMulStartAtFatigue) / Math.Max(1E-06f, 1f - MagnitudeMulStartAtFatigue));
		}
		float num5 = MathUtils.Lerp(1f, MagnitudeMulAtFatigue1, f);
		return MathUtils.Saturate(num3 * num5);
	}

	private Vector3 ComputeBurstDirection()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 vector = m_targetPos - position;
		if (vector.LengthSquared() < 1E-06f)
		{
			vector = m_componentCreature.ComponentBody.Matrix.Forward;
		}
		Vector3 v = new Vector3(vector.X, 0f, vector.Z);
		if (v.LengthSquared() < 1E-06f)
		{
			v = m_componentCreature.ComponentBody.Matrix.Forward;
		}
		v = v.Normalized();
		Vector3 vector2 = (-v).Normalized();
		Vector3 vector3 = Vector3.Cross(Vector3.UnitY, vector2).Normalized();
		if (vector3.LengthSquared() < 1E-06f)
		{
			vector3 = Vector3.Cross(Vector3.UnitX, vector2).Normalized();
		}
		float num = SideAngleMinDeg;
		float num2 = SideAngleMaxDeg;
		float num3 = AlongLineBias;
		if (Mode == StruggleMode.Power)
		{
			num = MathUtils.Max(num, 25f);
			num2 = MathUtils.Max(num2, 65f);
			num3 = MathUtils.Clamp(AlongLineBias + m_random.Float(-0.2f, -0.05f), 0.55f, 0.9f);
		}
		float num4 = MathUtils.Saturate(GetFatigue());
		float num5 = MathUtils.Lerp(1f, AngleRangeMulAtFatigue1, num4);
		num *= num5;
		num2 *= num5;
		num3 = MathUtils.Clamp(num3 + AlongLineBiasAddAtFatigue1 * num4, 0f, 0.98f);
		float degrees = RandomRange(num, num2);
		float num6 = ((m_random.Float(0f, 1f) < 0.5f) ? (-1f) : 1f);
		float x = MathUtils.DegToRad(degrees) * num6;
		Vector3 vector4 = Vector3.Normalize((MathUtils.Cos(x) * vector2 + MathUtils.Sin(x) * vector3).Normalized() * (1f - num3) + vector2 * num3);
		float num7 = MathUtils.Lerp(1f, VerticalChanceMulAtFatigue1, num4);
		float num8 = MathUtils.Lerp(1f, VerticalStrengthMulAtFatigue1, num4);
		float y = 0f;
		if (m_componentCreature.ComponentBody.ImmersionFactor > 0.3f && m_random.Float(0f, 1f) < VerticalChance * num7)
		{
			y = ((Mode != StruggleMode.Power) ? (m_random.Float(-1f, -0.2f) * VerticalStrength * num8) : ((0f - VerticalStrength) * num8));
		}
		return new Vector3(vector4.X, y, vector4.Z).Normalized();
	}

	private void ApplyOrders(Vector3 worldDir, float magnitude)
	{
		magnitude = MathUtils.Saturate(magnitude);
		if (magnitude <= 0f || worldDir.LengthSquared() <= 1E-06f || m_locomotion == null)
		{
			return;
		}
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		if (componentBody.ImmersionFactor > 0.3f)
		{
			Vector3 v = worldDir;
			if (!(v.LengthSquared() < 1E-06f))
			{
				v = v.Normalized() * MathUtils.Max(0.15f, magnitude);
				m_locomotion.SwimOrder = v;
				m_locomotion.WalkOrder = null;
				AimNoseTowards(worldDir);
			}
			return;
		}
		Vector3 v2 = new Vector3(componentBody.Matrix.Right.X, 0f, componentBody.Matrix.Right.Z).Normalized();
		Vector3 v3 = new Vector3(componentBody.Matrix.Forward.X, 0f, componentBody.Matrix.Forward.Z).Normalized();
		Vector3 v4 = new Vector3(worldDir.X, 0f, worldDir.Z);
		if (!(v4.LengthSquared() < 1E-06f))
		{
			v4 = v4.Normalized();
			float x = MathUtils.Clamp(Vector3.Dot(v4, v2), -1f, 1f) * magnitude;
			float y = MathUtils.Clamp(Vector3.Dot(v4, v3), -1f, 1f) * magnitude;
			Vector2 value = new Vector2(x, y);
			m_locomotion.WalkOrder = value;
			AimNoseTowards(worldDir, 0.6f);
		}
	}

	private void ClearOrders()
	{
		if (m_locomotion != null)
		{
			m_locomotion.SwimOrder = null;
			m_locomotion.WalkOrder = null;
			m_locomotion.TurnOrder = Vector2.Zero;
		}
	}

	private float QualityToMul(AttemptQuality q)
	{
		return q switch
		{
			AttemptQuality.Red => 1.3f, 
			AttemptQuality.Yellow => 1.15f, 
			_ => 1f, 
		};
	}

	private float RandomRange(float a, float b)
	{
		if (b < a)
		{
			float num = a;
			a = b;
			b = num;
		}
		return a + m_random.Float(0f, 1f) * (b - a);
	}

	public void ForceActivate(float seconds, float intensity = 1f)
	{
		if (GetFatigue() >= 0.98f)
		{
			m_forceActiveTime = 0f;
			m_importanceLevel = 0f;
			if (m_stateMachine.CurrentState != "Inactive")
			{
				m_stateMachine.TransitionTo("Inactive");
			}
			return;
		}
		m_forceActiveTime = MathUtils.Max(m_forceActiveTime, seconds);
		m_intensity = MathUtils.Saturate(intensity);
		m_importanceLevel = ActivePriority;
		m_changeTimeLeft = 0f;
		if (string.IsNullOrEmpty(m_stateMachine.CurrentState))
		{
			m_stateMachine.TransitionTo("Burst");
		}
	}

	private void AimNoseTowards(Vector3 worldDir, float gain = 1f)
	{
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		Vector3 v = new Vector3(componentBody.Matrix.Forward.X, 0f, componentBody.Matrix.Forward.Z);
		Vector3 v2 = new Vector3(worldDir.X, 0f, worldDir.Z);
		if (!(v.LengthSquared() < 1E-06f) && !(v2.LengthSquared() < 1E-06f))
		{
			v = v.Normalized();
			v2 = v2.Normalized();
			float num = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v, v2), -1f, 1f));
			float x = MathUtils.Sign(Vector3.Cross(v, v2).Y) * MathUtils.Saturate(num / 1.2f) * gain;
			if (m_locomotion != null)
			{
				m_locomotion.TurnOrder = new Vector2(x, 0f);
			}
		}
	}

	private Vector3? FindBurstDestination(Vector3 dir, float step)
	{
		Vector3 vector = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
		Vector3 vector2 = dir.Normalized();
		Vector3 vector3 = vector + step * vector2;
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(vector, vector3, useInteractionBoxes: false, skipAirBlocks: false, (int val, float d) =>
		{
			int num = Terrain.ExtractContents(val);
			Block obj = BlocksManager.Blocks[num];
			bool flag = num == 0;
			bool flag2 = obj is FocalorsWaterBlock;
			return !flag && !flag2;
		});
		if (!terrainRaycastResult.HasValue)
		{
			return vector3;
		}
		if (terrainRaycastResult.Value.Distance > 0.8f)
		{
			return vector + vector2 * (terrainRaycastResult.Value.Distance - 0.6f);
		}
		float yawRad = ((m_random.Float(0f, 1f) < 0.5f) ? (-1f) : 1f) * MathUtils.DegToRad(60f);
		Vector3 vector4 = RotateHoriz(dir, yawRad).Normalized();
		Vector3 vector5 = vector + step * vector4;
		TerrainRaycastResult? terrainRaycastResult2 = m_subsystemTerrain.Raycast(vector, vector5, useInteractionBoxes: false, skipAirBlocks: false, (int val, float d) =>
		{
			int num = Terrain.ExtractContents(val);
			Block obj = BlocksManager.Blocks[num];
			bool flag = num == 0;
			bool flag2 = obj is FocalorsWaterBlock;
			return !flag && !flag2;
		});
		if (!terrainRaycastResult2.HasValue)
		{
			return vector5;
		}
		if (terrainRaycastResult2.Value.Distance > 0.8f)
		{
			return vector + vector4 * (terrainRaycastResult2.Value.Distance - 0.6f);
		}
		return null;
	}

	private void DriveWithPathfinding(Vector3 worldDir, float magnitude, float dt)
	{
		if (m_path == null)
		{
			ApplyOrders(worldDir, magnitude);
			AimNoseTowards(worldDir);
			return;
		}
		Vector3 worldDir2 = worldDir;
		if (m_path.Destination.HasValue)
		{
			Vector3 vector = m_path.Destination.Value - m_componentCreature.ComponentBody.Position;
			if (vector.LengthSquared() > 1E-06f)
			{
				worldDir2 = new Vector3(vector.X, worldDir.Y, vector.Z).Normalized();
			}
		}
		ApplyOrders(worldDir2, MathUtils.Max(0.2f, magnitude));
		if (!m_path.Destination.HasValue || m_path.IsStuck || m_repathCooldown <= 0f)
		{
			float num = MathUtils.Lerp(6f, 12f, MathUtils.Saturate(m_severity));
			float speed = MathUtils.Lerp(0.35f, 1.2f, MathUtils.Saturate(magnitude));
			Vector3 dir = AdjustHorizForObstacles(worldDir, num);
			Vector3? destination = FindBurstDestination(dir, num);
			if (destination.HasValue)
			{
				m_path.SetDestination(destination, speed, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
				m_repathCooldown = 0.2f;
			}
			else
			{
				m_repathCooldown = 0.05f;
			}
		}
		else
		{
			m_repathCooldown -= dt;
		}
		AimNoseTowards(worldDir2, 0.9f);
	}

	private float AngleHoriz(Vector3 a, Vector3 b)
	{
		Vector3 v = new Vector3(a.X, 0f, a.Z);
		Vector3 v2 = new Vector3(b.X, 0f, b.Z);
		if (v.LengthSquared() < 1E-06f || v2.LengthSquared() < 1E-06f)
		{
			return 0f;
		}
		v = v.Normalized();
		v2 = v2.Normalized();
		return MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v, v2), -1f, 1f));
	}

	private Vector3 RotateHoriz(Vector3 dir, float yawRad)
	{
		Vector3 v = new Vector3(dir.X, 0f, dir.Z);
		if (v.LengthSquared() < 1E-06f)
		{
			return dir;
		}
		v = v.Normalized();
		float num = MathUtils.Cos(yawRad);
		float num2 = MathUtils.Sin(yawRad);
		return new Vector3(v.X * num - v.Z * num2, dir.Y, v.X * num2 + v.Z * num);
	}

	private float RadToDeg(float r)
	{
		return r * 57.29578f;
	}

	private Vector3 AdjustHorizForObstacles(Vector3 dir, float feelerLen)
	{
		Vector3 vector = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
		Vector3 v = new Vector3(dir.X, 0f, dir.Z);
		if (v.LengthSquared() < 1E-06f)
		{
			return dir;
		}
		v = v.Normalized();
		float[] array = new float[7]
		{
			0f,
			MathUtils.DegToRad(30f),
			0f - MathUtils.DegToRad(30f),
			MathUtils.DegToRad(60f),
			0f - MathUtils.DegToRad(60f),
			MathUtils.DegToRad(90f),
			0f - MathUtils.DegToRad(90f)
		};
		float num = -1f;
		float yawRad = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 v2 = RotateHoriz(v, array[i]);
			if (v2.LengthSquared() >= 1E-06f)
			{
				v2 = v2.Normalized();
				Vector3 end = vector + v2 * feelerLen;
				TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(vector, end, useInteractionBoxes: false, skipAirBlocks: false, (int val, float d) =>
				{
					int num3 = Terrain.ExtractContents(val);
					Block obj = BlocksManager.Blocks[num3];
					bool flag = num3 == 0;
					bool flag2 = obj is FocalorsWaterBlock;
					return !flag && !flag2;
				});
				float num2 = (terrainRaycastResult.HasValue ? terrainRaycastResult.Value.Distance : feelerLen);
				if (num2 > num)
				{
					num = num2;
					yawRad = array[i];
				}
			}
		}
		if (num < 1f)
		{
			yawRad = MathUtils.DegToRad(180f);
		}
		Vector3 vector2 = RotateHoriz(v, yawRad).Normalized();
		Vector3 v3 = new Vector3(vector2.X, dir.Y, vector2.Z);
		if (v3.LengthSquared() <= 1E-06f)
		{
			return dir;
		}
		return v3.Normalized();
	}

	private float GetFatigue()
	{
		if (m_subsystemFishingFloats == null || m_componentCreature == null)
		{
			return 0f;
		}
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		if (componentBody == null)
		{
			return 0f;
		}
		return m_subsystemFishingFloats.GetFatigueForBody(componentBody);
	}
}
