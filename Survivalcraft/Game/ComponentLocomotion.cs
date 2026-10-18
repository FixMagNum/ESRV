using System;
using System.Linq;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentLocomotion : Component, IUpdateable
{
	public static class GameModeHelpers
	{
		public static int GetDifficultyWeight(GameMode mode)
		{
			return mode switch
			{
				GameMode.Creative => 0, 
				GameMode.Harmless => 10, 
				GameMode.Challenging => 30, 
				GameMode.Cruel => 50, 
				GameMode.Adventure => 20, 
				GameMode.Embrace => 60, 
				GameMode.RealChallenging => 40, 
				_ => 100, 
			};
		}

		public static bool IsHardcore(GameMode mode)
		{
			if (mode != GameMode.Cruel && mode != GameMode.RealChallenging)
			{
				return mode == GameMode.Embrace;
			}
			return true;
		}
	}

	private SubsystemTime m_subsystemTime;

	private SubsystemNoise m_subsystemNoise;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemGameInfo m_subsystemGameInfo;

	private ComponentCreature m_componentCreature;

	private ComponentPlayer m_componentPlayer;

	private ComponentLevel m_componentLevel;

	private ComponentMount m_componentMount;

	private ComponentRider m_componentRider;

	private Random m_random = new Random();

	private Vector2? m_walkOrder;

	private Vector3? m_flyOrder;

	private Vector3? m_swimOrder;

	private Vector2 m_turnOrder;

	private Vector2 m_lookOrder;

	private float m_jumpOrder;

	private bool m_lookAutoLevelX;

	private bool m_lookAutoLevelY;

	private double m_shoesWarningTime;

	private float m_walkSpeedWhenTurning;

	private float m_minFrictionFactor;

	private double m_ladderActivationTime;

	private float m_swimBurstRemaining;

	public Vector2 m_lookAngles;

	private Vector3? m_lastPosition;

	private bool m_walking;

	private bool m_falling;

	public bool m_climbing;

	private bool m_jumping;

	private bool m_swimming;

	private bool m_flying;

	public Vector2? SendLookAngles;

	public bool FlyOrderChange;

	public ComponentPlayer componentPlayer;

	public NetVector2 NetLookAngles;

	private double m_lastSnowStepTime;

	private SubsystemWeather m_subsystemWeather;

	private double m_lastLadderStepTime;

	private float m_extraSlipTime;

	private bool m_embraceCharging;

	private float m_embraceChargeTime;

	private Vector2 m_embraceCachedDir;

	private SubsystemParticles m_subsystemParticles;

	private SpeedFlybyParticleSystem m_speedFlybyFx;

	private float m_speedFlybyIntensity;

	private float m_uiAmbientLight;

	private float m_uiAmbientRawLight;

	private float m_uiAmbientStableTime;

	public float AccelerationFactor { get; set; }

	public float WalkSpeed { get; set; }

	public float LadderSpeed { get; set; }

	public float JumpSpeed { get; set; }

	public float FlySpeed { get; set; }

	public float CreativeFlySpeed { get; set; }

	public float SwimSpeed { get; set; }

	public float TurnSpeed { get; set; }

	public float LookSpeed { get; set; }

	public float InAirWalkFactor { get; set; }

	public float? SlipSpeed { get; set; }

	public Vector2 LookAngles
	{
		get
		{
			return m_lookAngles;
		}
		set
		{
			value.X = MathUtils.Clamp(value.X, 0f - MathUtils.DegToRad(140f), MathUtils.DegToRad(140f));
			value.Y = MathUtils.Clamp(value.Y, 0f - MathUtils.DegToRad(82f), MathUtils.DegToRad(82f));
			if (m_lookAngles != value)
			{
				SendLookAngles = value;
			}
			m_lookAngles = value;
		}
	}

	public int? LadderValue { get; set; }

	public Vector2? WalkOrder
	{
		get
		{
			return m_walkOrder;
		}
		set
		{
			m_walkOrder = value;
			if (m_walkOrder.HasValue)
			{
				float num = m_walkOrder.Value.LengthSquared();
				if (num > 1f)
				{
					m_walkOrder = m_walkOrder.Value / MathUtils.Sqrt(num);
				}
			}
		}
	}

	public Vector3? FlyOrder
	{
		get
		{
			return m_flyOrder;
		}
		set
		{
			if (m_flyOrder != value)
			{
				FlyOrderChange = true;
			}
			m_flyOrder = value;
			if (m_flyOrder.HasValue)
			{
				float vector = m_flyOrder.Value.LengthSquared();
				if (vector > 1f)
				{
					m_flyOrder = m_flyOrder.Value / MathUtils.Sqrt(vector);
				}
			}
		}
	}

	public Vector3? SwimOrder
	{
		get
		{
			return m_swimOrder;
		}
		set
		{
			m_swimOrder = value;
			if (m_swimOrder.HasValue)
			{
				float num = m_swimOrder.Value.LengthSquared();
				if (num > 1f)
				{
					m_swimOrder = m_swimOrder.Value / MathUtils.Sqrt(num);
				}
			}
		}
	}

	public Vector2 TurnOrder
	{
		get
		{
			return m_turnOrder;
		}
		set
		{
			m_turnOrder = value;
		}
	}

	public Vector2 LookOrder
	{
		get
		{
			return m_lookOrder;
		}
		set
		{
			m_lookOrder = value;
		}
	}

	public float JumpOrder
	{
		get
		{
			return m_jumpOrder;
		}
		set
		{
			m_jumpOrder = MathUtils.Saturate(value);
		}
	}

	public float StunTime { get; set; }

	public Vector2? LastWalkOrder { get; set; }

	public float LastJumpOrder { get; set; }

	public Vector3? LastFlyOrder { get; set; }

	public Vector3? LastSwimOrder { get; set; }

	public Vector2 LastTurnOrder { get; set; }

	public bool IsCreativeFlyEnabled { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Locomotion;

	public virtual void Update(float dt)
	{
		if (Entity.ValuesDictionary.GetValue("IsWrenchFrozen", defaultValue: false))
		{
			return;
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			if (m_componentPlayer == null)
			{
				if (m_componentCreature.ComponentBody.ChildBodies.Count == 0)
				{
					LookAngles = NetLookAngles.Get(dt);
					return;
				}
				ComponentBody componentBody = m_componentCreature.ComponentBody.ChildBodies[0];
				if (componentBody.Entity.FindComponent<ComponentPlayer>() != null && !componentBody.Entity.FindComponent<ComponentPlayer>().PlayerData.IsMainPlayer)
				{
					LookAngles = NetLookAngles.Get(dt);
					return;
				}
			}
			if (m_componentPlayer != null && !m_componentPlayer.PlayerData.IsMainPlayer)
			{
				LookAngles = NetLookAngles.Get(dt);
				NormalMovement(dt);
				return;
			}
		}
		else if (CommonLib.WorkType == WorkType.Server)
		{
			if (m_componentPlayer == null && m_componentCreature.ComponentBody.ChildBodies.Count > 0)
			{
				ComponentBody componentBody2 = m_componentCreature.ComponentBody.ChildBodies[0];
				if (componentBody2.Entity.FindComponent<ComponentPlayer>() != null && !componentBody2.Entity.FindComponent<ComponentPlayer>().PlayerData.IsMainPlayer)
				{
					LookAngles = NetLookAngles.Get(dt);
					return;
				}
			}
			if (m_componentPlayer != null && !m_componentPlayer.PlayerData.IsMainPlayer)
			{
				LookAngles = NetLookAngles.Get(dt);
				NormalMovement(dt);
				return;
			}
		}
		SlipSpeed = null;
		bool thread = m_componentPlayer != null && m_componentPlayer.PlayerData.ServerManager;
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && !thread)
		{
			IsCreativeFlyEnabled = false;
		}
		StunTime = MathUtils.Max(StunTime - dt, 0f);
		if (m_componentCreature.ComponentHealth.Health > 0f && StunTime <= 0f)
		{
			Vector3 position = m_componentCreature.ComponentBody.Position;
			int x = Terrain.ToCell(position.X);
			int num = Terrain.ToCell(position.Y);
			int z = Terrain.ToCell(position.Z);
			float num2 = 1f;
			bool flag = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
			if (flag)
			{
				if (m_componentPlayer != null)
				{
					ComponentAdrenaline componentAdrenaline = Entity.FindComponent<ComponentAdrenaline>();
					if (componentAdrenaline != null && componentAdrenaline.Adrenaline > 0f)
					{
						MathUtils.Pow(2f, componentAdrenaline.Adrenaline);
					}
				}
				else
				{
					num2 = MathUtils.Max(0.01f, m_componentCreature.ComponentBody.Size);
				}
			}
			float num3 = 1f;
			if (flag && m_componentPlayer != null)
			{
				ComponentAdrenaline componentAdrenaline2 = Entity.FindComponent<ComponentAdrenaline>();
				if (componentAdrenaline2 != null && componentAdrenaline2.Adrenaline > 0f)
				{
					float n = MathUtils.Min(componentAdrenaline2.Adrenaline, 1f);
					num3 = MathUtils.Pow(2f, n);
				}
			}
			float num4 = TurnSpeed * num3 * num2;
			float num5 = LookSpeed * num3 * num2;
			if (m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer)
			{
				int x2 = Terrain.ToCell(position.X);
				int z2 = Terrain.ToCell(position.Z);
				int y = Terrain.ToCell(position.Y + 0.1f);
				int y2 = Terrain.ToCell(position.Y + 1.6f);
				int cellLight = m_subsystemTerrain.Terrain.GetCellLight(x2, y, z2);
				int cellLight2 = m_subsystemTerrain.Terrain.GetCellLight(x2, y2, z2);
				float num6 = MathUtils.Saturate(0.5f * ((float)cellLight + (float)cellLight2) / 15f);
				if (MathUtils.Abs(num6 - m_uiAmbientRawLight) < 0.05f)
				{
					m_uiAmbientStableTime += dt;
				}
				else
				{
					m_uiAmbientStableTime = 0f;
				}
				m_uiAmbientRawLight = num6;
				float x3 = num6;
				if (num6 < m_uiAmbientLight && m_uiAmbientLight - num6 > 0.5f && m_uiAmbientStableTime < 0.15f)
				{
					x3 = m_uiAmbientLight;
				}
				float f = MathUtils.Saturate(dt / 0.33f);
				m_uiAmbientLight = MathUtils.Lerp(m_uiAmbientLight, x3, f);
				ContainerWidget.SetAmbientUiLight(m_uiAmbientLight);
			}
			bool flag2 = false;
			Vector3[] array = new Vector3[4]
			{
				Vector3.UnitX,
				-Vector3.UnitX,
				Vector3.UnitZ,
				-Vector3.UnitZ
			};
			for (int i = 0; i < array.Length; i++)
			{
				Vector3 vector = array[i];
				float num7 = 0.5f;
				int x4 = Terrain.ToCell(position.X + vector.X * num7);
				int num8 = num;
				int z3 = Terrain.ToCell(position.Z + vector.Z * num7);
				int num9 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x4, num8, z3));
				if (BlocksManager.Blocks[num9] is StrawBlock && num8 == num)
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				m_componentCreature.ComponentBody.IsGravityEnabled = false;
				if (WalkOrder.HasValue && Math.Abs(WalkOrder.Value.Y) > 0.1f)
				{
					m_componentCreature.ComponentBody.Velocity = new Vector3(m_componentCreature.ComponentBody.Velocity.X, LadderSpeed * WalkOrder.Value.Y, m_componentCreature.ComponentBody.Velocity.Z);
					m_climbing = true;
					if (Time.FrameStartTime - m_lastLadderStepTime > 0.6)
					{
						int? ladderValue = LadderValue;
						LadderValue = Terrain.MakeBlockValue(310, 0, 0);
						m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
						LadderValue = ladderValue;
						m_lastLadderStepTime = Time.FrameStartTime;
					}
				}
			}
			else
			{
				m_componentCreature.ComponentBody.IsGravityEnabled = true;
				m_climbing = false;
			}
			PlayerStats playerStats = m_componentCreature.PlayerStats;
			if (playerStats != null)
			{
				float x5 = (m_lastPosition.HasValue ? Vector3.Distance(position, m_lastPosition.Value) : 0f);
				x5 = MathUtils.Min(x5, 25f * m_subsystemTime.PreviousGameTimeDelta);
				playerStats.DistanceTravelled += x5;
				if (m_componentRider != null && m_componentRider.Mount != null)
				{
					playerStats.DistanceRidden += x5;
				}
				else
				{
					if (m_walking)
					{
						playerStats.DistanceWalked += x5;
						m_walking = false;
					}
					if (m_falling)
					{
						playerStats.DistanceFallen += x5;
						m_falling = false;
					}
					if (m_climbing)
					{
						playerStats.DistanceClimbed += x5;
						m_climbing = false;
					}
					if (m_jumping)
					{
						playerStats.Jumps++;
						m_jumping = false;
					}
					if (m_swimming)
					{
						playerStats.DistanceSwam += x5;
						m_swimming = false;
					}
					if (m_flying)
					{
						playerStats.DistanceFlown += x5;
						m_flying = false;
					}
				}
				playerStats.DeepestDive = MathUtils.Max(playerStats.DeepestDive, m_componentCreature.ComponentBody.ImmersionDepth);
				playerStats.LowestAltitude = MathUtils.Min(playerStats.LowestAltitude, position.Y);
				playerStats.HighestAltitude = MathUtils.Max(playerStats.HighestAltitude, position.Y);
				GameMode gameMode = m_subsystemGameInfo.WorldSettings.GameMode;
				GameMode easiestModeUsed = playerStats.EasiestModeUsed;
				if (easiestModeUsed == GameMode.Creative && gameMode != GameMode.Creative)
				{
					playerStats.EasiestModeUsed = gameMode;
				}
				else
				{
					int difficultyWeight = GameModeHelpers.GetDifficultyWeight(gameMode);
					int difficultyWeight2 = GameModeHelpers.GetDifficultyWeight(easiestModeUsed);
					if (difficultyWeight < difficultyWeight2)
					{
						playerStats.EasiestModeUsed = gameMode;
					}
				}
			}
			m_lastPosition = position;
			m_swimBurstRemaining = MathUtils.Saturate(0.1f * m_swimBurstRemaining + dt);
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num, z);
			int num10 = Terrain.ExtractContents(cellValue);
			Block block = BlocksManager.Blocks[num10];
			if (LadderSpeed > 0f && !LadderValue.HasValue && block is LadderBlock && m_subsystemTime.GameTime >= m_ladderActivationTime && !IsCreativeFlyEnabled && m_componentCreature.ComponentBody.ParentBody == null)
			{
				int face = LadderBlock.GetFace(Terrain.ExtractData(cellValue));
				if ((face == 0 && m_componentCreature.ComponentBody.CollisionVelocityChange.Z > 0f) || (face == 1 && m_componentCreature.ComponentBody.CollisionVelocityChange.X > 0f) || (face == 2 && m_componentCreature.ComponentBody.CollisionVelocityChange.Z < 0f) || (face == 3 && m_componentCreature.ComponentBody.CollisionVelocityChange.X < 0f) || !m_componentCreature.ComponentBody.StandingOnValue.HasValue)
				{
					LadderValue = cellValue;
					m_ladderActivationTime = m_subsystemTime.GameTime + 0.20000000298023224;
					m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
				}
			}
			Quaternion rotation = m_componentCreature.ComponentBody.Rotation;
			if (flag && m_componentCreature.ComponentBody.HumanModel != null)
			{
				Vector3 axis = Vector3.Transform(Vector3.UnitY, rotation);
				float angle = (0f - num4) * TurnOrder.X * dt;
				m_componentCreature.ComponentBody.Rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(axis, angle) * rotation);
			}
			else
			{
				float num11 = MathUtils.Atan2(2f * rotation.Y * rotation.W - 2f * rotation.X * rotation.Z, 1f - 2f * rotation.Y * rotation.Y - 2f * rotation.Z * rotation.Z);
				num11 += (0f - num4) * TurnOrder.X * dt;
				m_componentCreature.ComponentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num11);
			}
			LookAngles += num5 * LookOrder * dt;
			if (LadderValue.HasValue)
			{
				LadderMovement(dt, cellValue);
			}
			else
			{
				NormalMovement(dt);
			}
		}
		else
		{
			m_componentCreature.ComponentBody.IsGravityEnabled = true;
			m_componentCreature.ComponentBody.IsGroundDragEnabled = true;
			m_componentCreature.ComponentBody.IsWaterDragEnabled = true;
		}
		LastWalkOrder = WalkOrder;
		LastFlyOrder = FlyOrder;
		LastSwimOrder = SwimOrder;
		LastTurnOrder = TurnOrder;
		LastJumpOrder = JumpOrder;
		WalkOrder = null;
		FlyOrder = null;
		SwimOrder = null;
		TurnOrder = Vector2.Zero;
		JumpOrder = 0f;
		LookOrder = new Vector2(m_lookAutoLevelX ? (-10f * LookAngles.X / LookSpeed) : 0f, m_lookAutoLevelY ? (-10f * LookAngles.Y / LookSpeed) : 0f);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
		m_componentLevel = Entity.FindComponent<ComponentLevel>();
		m_componentMount = Entity.FindComponent<ComponentMount>();
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_componentRider = Entity.FindComponent<ComponentRider>();
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		IsCreativeFlyEnabled = valuesDictionary.GetValue<bool>("IsCreativeFlyEnabled");
		AccelerationFactor = valuesDictionary.GetValue<float>("AccelerationFactor");
		WalkSpeed = valuesDictionary.GetValue<float>("WalkSpeed");
		LadderSpeed = valuesDictionary.GetValue<float>("LadderSpeed");
		JumpSpeed = valuesDictionary.GetValue<float>("JumpSpeed");
		CreativeFlySpeed = valuesDictionary.GetValue<float>("CreativeFlySpeed");
		FlySpeed = valuesDictionary.GetValue<float>("FlySpeed");
		SwimSpeed = valuesDictionary.GetValue<float>("SwimSpeed");
		TurnSpeed = valuesDictionary.GetValue<float>("TurnSpeed");
		LookSpeed = valuesDictionary.GetValue<float>("LookSpeed");
		InAirWalkFactor = valuesDictionary.GetValue<float>("InAirWalkFactor");
		m_walkSpeedWhenTurning = valuesDictionary.GetValue<float>("WalkSpeedWhenTurning");
		m_minFrictionFactor = valuesDictionary.GetValue<float>("MinFrictionFactor");
		m_lookAutoLevelX = valuesDictionary.GetValue<bool>("LookAutoLevelX");
		m_lookAutoLevelY = valuesDictionary.GetValue<bool>("LookAutoLevelY");
		if ((m_componentPlayer = Entity.FindComponent<ComponentPlayer>()) == null)
		{
			WalkSpeed *= m_random.Float(0.85f, 1f);
			FlySpeed *= m_random.Float(0.85f, 1f);
			SwimSpeed *= m_random.Float(0.85f, 1f);
		}
		NetLookAngles = new NetVector2(LookAngles);
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && IsCreativeFlyEnabled)
		{
			IsCreativeFlyEnabled = false;
		}
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("IsCreativeFlyEnabled", IsCreativeFlyEnabled);
	}

	private void NormalMovement(float dt)
	{
		bool num = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentCreature.ComponentBody.HumanModel != null;
		m_extraSlipTime = MathUtils.Max(m_extraSlipTime - dt, 0f);
		m_componentCreature.ComponentBody.IsGravityEnabled = true;
		m_componentCreature.ComponentBody.IsGroundDragEnabled = true;
		m_componentCreature.ComponentBody.IsWaterDragEnabled = true;
		float i = m_componentCreature.ComponentBody.Size;
		float num2 = ((!(i < 1f)) ? ((float)Math.Pow(i, 0.65)) : MathUtils.Lerp(0.45f, 1f, i));
		float num3 = 1f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentPlayer != null)
		{
			num3 = GetEmbracePlayerSpeedFactor();
		}
		float num4 = num2 * num3;
		float num5 = 1f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentPlayer == null && i >= 2.5f)
		{
			float f = MathUtils.Saturate((i - 2.5f) / 1.5f);
			num5 = MathUtils.Lerp(1f, 1.75f, f);
		}
		bool flag = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers.Any((ComponentPlayer p) => p.PlayerData.IsCheater);
		float num6 = (((m_componentPlayer == null) & flag) ? 2f : 1f);
		float num7 = AccelerationFactor * num4 * num5 * num6;
		float num8 = WalkSpeed * num4 * num6;
		float num9 = JumpSpeed * num4 * num6;
		float num10 = FlySpeed * num4 * num6;
		float num11 = CreativeFlySpeed * num4;
		float num12 = SwimSpeed * num4 * num6;
		if (m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water)
		{
			float immersionFactor = m_componentCreature.ComponentBody.ImmersionFactor;
			if (immersionFactor > 0.05f)
			{
				m_componentCreature.ComponentBody.IsGravityEnabled = false;
			}
			if (immersionFactor < 0.2f)
			{
				num8 *= 0.45f;
				num9 *= 0.6f;
				num7 *= 0.5f;
			}
			else if (immersionFactor > 0.4f)
			{
				num12 *= 3.7f;
				num7 *= 2.5f;
			}
		}
		Vector3 vector = Vector3.Transform(q: m_componentCreature.ComponentBody.Rotation, v: Vector3.UnitY);
		Vector3 right = m_componentCreature.ComponentBody.Matrix.Right;
		Vector3 vector2 = Vector3.Transform(m_componentCreature.ComponentBody.Matrix.Forward, Quaternion.CreateFromAxisAngle(right, LookAngles.Y));
		Vector3 vector3 = Vector3.Normalize(right - vector * Vector3.Dot(right, vector));
		Vector3 vector4 = Vector3.Normalize(vector2 - vector * Vector3.Dot(vector2, vector));
		Vector3 velocity = m_componentCreature.ComponentBody.Velocity;
		Vector3 position = m_componentCreature.ComponentBody.Position;
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) == 61 && CommonLib.WorkType != WorkType.Client && Time.FrameStartTime - m_lastSnowStepTime > 0.2 && new Vector2(velocity.X, velocity.Z).LengthSquared() > 0.01f)
		{
			float sizeFromValue = SnowBlock.GetSizeFromValue(cellValue);
			if (sizeFromValue > 0f)
			{
				sizeFromValue = Math.Max(0f, sizeFromValue - 0.14f);
				int value = SnowBlock.SetSizeInValue(cellValue, sizeFromValue);
				m_subsystemTerrain.ChangeCell(x, y, z, value);
				m_lastSnowStepTime = Time.FrameStartTime;
			}
		}
		if (num8 > 0f && WalkOrder.HasValue)
		{
			if (IsCreativeFlyEnabled)
			{
				Vector3 vector5 = new Vector3(WalkOrder.Value.X, 0f, WalkOrder.Value.Y);
				if (FlyOrder.HasValue)
				{
					vector5 += FlyOrder.Value;
				}
				Vector3 vector6 = ((!SettingsManager.HorizontalCreativeFlight || m_componentPlayer == null || m_componentPlayer.ComponentInput.IsControlledByTouch) ? Vector3.Normalize(vector2 + 0.1f * vector) : Vector3.Normalize(vector2 - vector * Vector3.Dot(vector2, vector)));
				Vector3 vector7 = num11 * (vector3 * vector5.X + vector * vector5.Y + vector6 * vector5.Z);
				float num13 = ((vector5 == Vector3.Zero) ? 5f : 3f);
				velocity += MathUtils.Saturate(num13 * dt) * (vector7 - velocity);
				m_componentCreature.ComponentBody.IsGravityEnabled = false;
				m_componentCreature.ComponentBody.IsGroundDragEnabled = false;
				m_flying = true;
			}
			else
			{
				Vector2 value2 = WalkOrder.Value;
				bool hasValue = m_componentCreature.ComponentBody.StandingOnValue.HasValue;
				if (m_walkSpeedWhenTurning > 0f && MathUtils.Abs(TurnOrder.X) > 0.02f)
				{
					value2.Y = MathUtils.Max(value2.Y, MathUtils.Lerp(0f, m_walkSpeedWhenTurning, MathUtils.Saturate(2f * MathUtils.Abs(TurnOrder.X))));
				}
				float num14 = num8;
				Vector3 position2 = m_componentCreature.ComponentBody.Position;
				int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position2.X), Terrain.ToCell(position2.Y), Terrain.ToCell(position2.Z));
				int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position2.X), Terrain.ToCell(position2.Y + 1.8f), Terrain.ToCell(position2.Z));
				bool flag2 = Terrain.ExtractContents(cellValue2) == 348;
				bool flag3 = m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Anabiosis || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Winter || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.LateWinter;
				int num15 = Terrain.ExtractContents(cellValue2);
				int num16 = Terrain.ExtractContents(cellValue3);
				bool flag4 = BlocksManager.Blocks[num15] is LeavesBlock && (!flag3 || (num15 != 12 && num15 != 13));
				bool flag5 = BlocksManager.Blocks[num16] is LeavesBlock && (!flag3 || (num16 != 12 && num16 != 13));
				bool flag6 = Terrain.ExtractContents(cellValue3) == 348;
				float num17 = 1f;
				if ((flag2 | flag4) && (flag6 | flag5))
				{
					num17 -= 0.7f;
				}
				else if (flag2 | flag4 | flag6 | flag5)
				{
					num17 -= 0.35f;
				}
				num14 *= num17;
				if (IsSpeedBoostBlock(Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position2.X), Terrain.ToCell(position2.Y - 0.1f), Terrain.ToCell(position2.Z)))))
				{
					num14 *= 1.1f;
				}
				if (m_componentCreature.ComponentBody.ImmersionFactor > 0.2f)
				{
					num14 *= 0.66f;
				}
				if (value2.Y < 0f)
				{
					num14 *= 0.6f;
				}
				if (m_componentLevel != null)
				{
					num14 *= m_componentLevel.SpeedFactor;
				}
				if (m_componentMount != null)
				{
					ComponentRider rider = m_componentMount.Rider;
					if (rider != null)
					{
						ComponentClothing componentClothing = rider.Entity.FindComponent<ComponentClothing>();
						if (componentClothing != null)
						{
							num14 *= componentClothing.SteedMovementSpeedFactor;
						}
					}
				}
				Vector3 vector8 = value2.X * vector3 + value2.Y * vector4;
				Vector3 v = num14 * vector8 + m_componentCreature.ComponentBody.StandingOnVelocity;
				if (hasValue)
				{
					if (Terrain.ExtractContents(m_componentCreature.ComponentBody.StandingOnValue.Value) == 393)
					{
						m_extraSlipTime = 5f;
					}
					float x2 = BlocksManager.Blocks[Terrain.ExtractContents(m_componentCreature.ComponentBody.StandingOnValue.Value)].FrictionFactor;
					if (m_extraSlipTime > 0f)
					{
						x2 = MathUtils.Min(x2, 0.05f);
					}
					x2 = MathUtils.Max(x2, m_minFrictionFactor);
					float num18 = MathUtils.Saturate(dt * 6f * num7 * x2);
					float num19 = Vector3.Dot(velocity, vector3);
					float num20 = Vector3.Dot(velocity, vector4);
					float num21 = Vector3.Dot(v, vector3);
					float num22 = Vector3.Dot(v, vector4);
					float num23 = num19 + num18 * (num21 - num19);
					float num24 = num20 + num18 * (num22 - num20);
					velocity += (num23 - num19) * vector3 + (num24 - num20) * vector4;
					if (x2 < 0.25f || m_extraSlipTime > 0f)
					{
						SlipSpeed = num14 * value2.Length();
					}
					m_walking = true;
				}
				else
				{
					float num25 = MathUtils.Saturate(dt * 6f * num7 * InAirWalkFactor);
					Vector3 v2 = (value2.X * vector3 + value2.Y * vector4) * num8;
					float num26 = Vector3.Dot(velocity, vector3);
					float num27 = Vector3.Dot(velocity, vector4);
					float num28 = Vector3.Dot(v2, vector3);
					float num29 = Vector3.Dot(v2, vector4);
					float num30 = num26 + num25 * (num28 - num26);
					float num31 = num27 + num25 * (num29 - num27);
					velocity += (num30 - num26) * vector3 + (num31 - num27) * vector4;
					if (m_componentCreature.ComponentBody.ImmersionFactor > 0f)
					{
						m_swimming = true;
					}
					else
					{
						m_falling = true;
					}
				}
				Vector3 v3 = value2.X * right + value2.Y * vector2;
				if (m_componentLevel != null)
				{
					v3 *= m_componentLevel.SpeedFactor;
				}
				float num32 = Vector3.Dot(v3, vector);
				velocity += vector * (10f * num7 * num32 * m_componentCreature.ComponentBody.ImmersionFactor * dt);
				m_componentCreature.ComponentBody.IsGroundDragEnabled = false;
				if (m_componentPlayer != null && Time.PeriodicEvent(10.0, 0.0) && (m_shoesWarningTime == 0.0 || Time.FrameStartTime - m_shoesWarningTime > 300.0) && m_componentCreature.ComponentBody.StandingOnValue.HasValue && m_componentCreature.ComponentBody.ImmersionFactor < 0.1f)
				{
					bool flag7 = false;
					int value3 = m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Feet).LastOrDefault();
					if (Terrain.ExtractContents(value3) == 203)
					{
						flag7 = BlocksManager.Blocks[Terrain.ExtractContents(value3)].GetClothingData(Terrain.ExtractData(value3)).MovementSpeedFactor > 1f;
					}
					if (!flag7 && v.LengthSquared() / velocity.LengthSquared() > 0.99f && WalkOrder.Value.LengthSquared() > 0.99f)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(GetType().Name, 0), Color.White, blinking: true, playNotificationSound: true);
						m_shoesWarningTime = Time.FrameStartTime;
					}
				}
			}
		}
		if (num10 > 0f && FlyOrder.HasValue)
		{
			Vector3 value4 = FlyOrder.Value;
			Vector3 vector9 = num10 * value4;
			velocity += MathUtils.Saturate(2f * num7 * dt) * (vector9 - velocity);
			m_componentCreature.ComponentBody.IsGravityEnabled = false;
			m_flying = true;
		}
		if (num12 > 0f && SwimOrder.HasValue && m_componentCreature.ComponentBody.ImmersionFactor > 0.5f)
		{
			Vector3 value5 = SwimOrder.Value;
			Vector3 vector10 = num12 * value5;
			float num33 = 2f;
			if (value5.LengthSquared() >= 0.99f)
			{
				vector10 *= MathUtils.Lerp(1f, 2f, m_swimBurstRemaining);
				num33 *= MathUtils.Lerp(1f, 4f, m_swimBurstRemaining);
				m_swimBurstRemaining -= dt;
			}
			velocity += MathUtils.Saturate(num33 * num7 * dt) * (vector10 - velocity);
			m_componentCreature.ComponentBody.IsGravityEnabled = MathUtils.Abs(value5.Y) <= 0.07f;
			m_componentCreature.ComponentBody.IsWaterDragEnabled = false;
			m_componentCreature.ComponentBody.IsGroundDragEnabled = false;
			m_swimming = true;
		}
		bool flag8 = (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0.5f) && !m_componentCreature.ComponentBody.IsSneaking;
		if (num)
		{
			bool flag9 = false;
			if (m_componentPlayer != null && m_componentPlayer.ComponentInput != null)
			{
				ComponentInput componentInput = m_componentPlayer.ComponentInput;
				flag9 = componentInput.m_jumpHoldKeyboard || componentInput.m_jumpHoldGamepad || componentInput.m_jumpHoldTouch;
			}
			if (flag8 && !m_embraceCharging && ((JumpOrder > 0f) | flag9))
			{
				m_embraceCharging = true;
				m_embraceChargeTime = 0f;
			}
			if (m_embraceCharging)
			{
				if (!flag8)
				{
					m_embraceCharging = false;
					m_embraceChargeTime = 0f;
				}
				else
				{
					if (flag9 && m_embraceChargeTime < 0.8f)
					{
						m_embraceChargeTime = MathUtils.Min(m_embraceChargeTime + dt, 0.8f);
					}
					if (!flag9 || m_embraceChargeTime >= 0.8f)
					{
						float num34 = num9;
						if (m_componentLevel != null)
						{
							num34 *= 0.25f * (m_componentLevel.SpeedFactor - 1f) + 1f;
						}
						float num35 = MathUtils.Saturate(m_embraceChargeTime / 0.8f);
						float num36 = 1f + 5f * num35;
						float num37 = 1f + 2f * num35;
						bool flag10 = WalkOrder.HasValue && WalkOrder.Value.LengthSquared() > 0.0025f;
						Vector3 vector11 = Vector3.Zero;
						if (flag10)
						{
							Vector2 value6 = WalkOrder.Value;
							Vector3 v4 = value6.X * vector3 + value6.Y * vector4;
							if (v4.LengthSquared() > 1E-06f)
							{
								vector11 = Vector3.Normalize(v4);
							}
						}
						float num38 = (flag10 ? (num34 * 0.5f) : (num34 * num37));
						float num39 = (flag10 ? (num34 * num36) : 0f);
						bool embraceStuck = m_componentCreature.ComponentBody.m_embraceStuck;
						bool flag11 = Vector3.Dot(m_componentCreature.ComponentBody.m_embraceUpVector, Vector3.UnitY) < -0.9f;
						Vector3 position3 = m_componentCreature.ComponentBody.Position;
						int x3 = Terrain.ToCell(position3.X);
						int y2 = Terrain.ToCell(position3.Y + 1.6f);
						int z2 = Terrain.ToCell(position3.Z);
						int num40 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x3, y2, z2));
						Block block = BlocksManager.Blocks[num40];
						bool flag12 = !(block is AirBlock) && !(block is LeavesBlock);
						if (embraceStuck & flag11 & flag12 & flag10)
						{
							num38 = 0f;
						}
						velocity += vector * num38;
						if (flag10)
						{
							velocity += vector11 * num39;
						}
						m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
						m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 10f);
						m_jumping = true;
						m_embraceCharging = false;
						m_embraceChargeTime = 0f;
						m_componentCreature.ComponentBody.m_embraceStuck = false;
						m_componentCreature.ComponentBody.m_embraceUpVector = Vector3.UnitY;
					}
				}
			}
		}
		else if ((JumpOrder > 0f) & flag8)
		{
			float num41 = num9;
			if (m_componentLevel != null)
			{
				num41 *= 0.25f * (m_componentLevel.SpeedFactor - 1f) + 1f;
			}
			velocity.Y = MathUtils.Min(velocity.Y + MathUtils.Saturate(JumpOrder) * num41, num41);
			m_jumping = true;
			m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
			m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 10f);
		}
		if (MathUtils.Abs(m_componentCreature.ComponentBody.CollisionVelocityChange.Y) > 3f)
		{
			m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
			m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 10f);
		}
		if (m_subsystemParticles != null)
		{
			SubsystemGameWidgets subsystemGameWidgets = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: false);
			SubsystemOceanScanner subsystemOceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: false);
			if ((subsystemGameWidgets != null && subsystemGameWidgets.GameWidgets.Count > 0) || CommonLib.WorkType != WorkType.Server)
			{
				float num42 = velocity.Length();
				float num43 = MathUtils.Max(((num8 > 0f) ? num8 : 6f) * 1.1f, 5f);
				if (m_componentPlayer != null && (m_componentPlayer.PlayerData == null || m_componentPlayer.PlayerData.IsMainPlayer))
				{
					float num44 = ((num43 > 0f) ? (num42 / num43) : 0f);
					float num45 = 0f;
					float num46 = 0f;
					if (num44 >= 1.5f)
					{
						num46 = 1f;
						if (num44 < 1f)
						{
							float x4 = (num44 - 0.7f) / 0.3f;
							num45 = MathUtils.Lerp(0f, 3f, MathUtils.Saturate(x4));
						}
						else if (num44 < 2f)
						{
							float x5 = num44 - 1f;
							num45 = MathUtils.Lerp(3f, 8f, MathUtils.Saturate(x5));
						}
						else if (num44 < 6f)
						{
							float x6 = (num44 - 2f) / 4f;
							num45 = MathUtils.Lerp(8f, 20f, MathUtils.Saturate(x6));
						}
						else
						{
							float f2 = MathUtils.Saturate((num44 - 6f) / 4f);
							num45 = MathUtils.Lerp(20f, 40f, f2);
						}
					}
					float num47 = 5f;
					float num48 = 2f;
					if (m_speedFlybyIntensity < num46)
					{
						m_speedFlybyIntensity = MathUtils.Min(m_speedFlybyIntensity + dt / num47, num46);
					}
					else
					{
						m_speedFlybyIntensity = MathUtils.Max(m_speedFlybyIntensity - dt / num48, num46);
					}
					float num49 = num45 * m_speedFlybyIntensity;
					if (IsCreativeFlyEnabled)
					{
						num49 *= 0.8f;
					}
					if (num49 > 0f)
					{
						if (m_speedFlybyFx == null)
						{
							m_speedFlybyFx = new SpeedFlybyParticleSystem(m_subsystemTerrain);
							m_speedFlybyFx.MaxVisibilityDistance = 160f;
							m_speedFlybyFx.LifetimeMinMax = new Vector2(0.6f, 1.2f);
							m_speedFlybyFx.SpawnAreaHalfSize = new Vector3(1.6f, 1.2f, 0f);
							m_subsystemParticles.AddParticleSystem(m_speedFlybyFx);
						}
						float y3 = m_componentCreature.ComponentBody.BoxSize.Y * 0.85f;
						Vector3 vector12 = m_componentCreature.ComponentBody.Position + new Vector3(0f, y3, 0f);
						SubsystemGameWidgets subsystemGameWidgets2 = subsystemGameWidgets;
						Camera camera = (m_componentPlayer.GameWidget ?? ((subsystemGameWidgets2 != null && subsystemGameWidgets2.GameWidgets.Count > 0) ? subsystemGameWidgets2.GameWidgets[0] : null))?.ActiveCamera;
						bool flag13 = m_componentCreature.ComponentBody.ImmersionFactor > 0.95f && m_componentCreature.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsWaterBlock;
						Vector3 anchorPosition = ((flag13 && camera != null) ? camera.ViewPosition : vector12);
						Vector3 vector13 = ((num42 > 1E-06f) ? (velocity / num42) : Vector3.UnitZ);
						Vector3 movementDir = ((flag13 && camera != null) ? camera.ViewDirection : vector13);
						float spawnFrontOffset = (flag13 ? MathUtils.Clamp(0.3f * num42, 5f, 35f) : MathUtils.Clamp(0.25f * num42, 4f, 28f));
						bool flag14 = false;
						if (!flag13 && subsystemOceanScanner != null && subsystemOceanScanner.IsOcean)
						{
							float waveSurfaceY = subsystemOceanScanner.GetWaveSurfaceY(position);
							if (position.Y > waveSurfaceY && position.Y < waveSurfaceY + 6f)
							{
								flag14 = true;
							}
						}
						bool flag15 = flag13 | flag14;
						int x7 = Terrain.ToCell(vector12.X);
						int z3 = Terrain.ToCell(vector12.Z);
						int shaftValue = m_subsystemTerrain.Terrain.GetShaftValue(x7, z3);
						int y4 = Terrain.ExtractTopHeight(shaftValue);
						int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(shaftValue);
						int seasonalHumidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(shaftValue);
						int snowTemperatureThreshold = m_subsystemWeather.GetSnowTemperatureThreshold();
						bool flag16 = SubsystemWeather.IsPlaceFrozenExtended(seasonalTemperature, y4, snowTemperatureThreshold);
						bool flag17 = seasonalTemperature > 8 && seasonalHumidity < 8;
						int rowMax;
						int rowMin;
						int colMin;
						int colMax;
						if (vector12.Y < 55f)
						{
							rowMax = (rowMin = 3);
							colMin = 0;
							colMax = 3;
						}
						else if (flag16)
						{
							rowMax = (rowMin = 0);
							colMin = 0;
							colMax = 3;
						}
						else if (flag17)
						{
							rowMin = 0;
							rowMax = 3;
							colMax = (colMin = 2);
						}
						else
						{
							rowMax = (rowMin = 1);
							colMin = 0;
							colMax = 3;
						}
						float num50 = MathUtils.Saturate((num42 - num43) / num43);
						float particleSize = 0.26f * (1f + 0.8f * num50);
						m_speedFlybyFx.AnchorPosition = anchorPosition;
						m_speedFlybyFx.MovementDir = movementDir;
						m_speedFlybyFx.UnderwaterMode = flag15;
						m_speedFlybyFx.UnderwaterMode = flag15;
						m_speedFlybyFx.SpawnFrontOffset = spawnFrontOffset;
						m_speedFlybyFx.SpawnAreaHalfSize = (flag15 ? new Vector3(2.4f, 2.4f, 4.5f) : new Vector3(1.6f, 1.2f, 0f));
						float num51 = num49;
						if (flag14 && !flag13)
						{
							num51 *= 0.5f;
						}
						m_speedFlybyFx.SpawnRatePerSecond = num51;
						m_speedFlybyFx.ParticleSize = particleSize;
						m_speedFlybyFx.SetTextureRange(rowMin, rowMax, colMin, colMax);
					}
					else if (m_speedFlybyFx != null)
					{
						m_speedFlybyFx.SpawnRatePerSecond = 0f;
					}
				}
			}
		}
		m_componentCreature.ComponentBody.Velocity = velocity;
	}

	private void LadderMovement(float dt, int value)
	{
		m_componentCreature.ComponentBody.IsGravityEnabled = false;
		Vector3 num = m_componentCreature.ComponentBody.Position;
		Vector3 velocity = m_componentCreature.ComponentBody.Velocity;
		int num2 = Terrain.ExtractContents(value);
		float num3 = 1f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			num3 = ((m_componentPlayer == null) ? GetEmbraceCreatureSizeFactor() : GetEmbracePlayerSpeedFactor());
		}
		float num4 = LadderSpeed * num3;
		float num5 = JumpSpeed * num3;
		if (BlocksManager.Blocks[num2] is LadderBlock)
		{
			LadderValue = value;
			if (WalkOrder.HasValue)
			{
				Vector2 value2 = WalkOrder.Value;
				float num6 = num4;
				switch (num2)
				{
				case 310:
					num6 *= 0.8f;
					break;
				case 213:
					num6 *= 1.4f;
					break;
				}
				float num7 = num6 * value2.Y;
				velocity.X = 5f * (MathUtils.Floor(num.X) + 0.5f - num.X);
				velocity.Z = 5f * (MathUtils.Floor(num.Z) + 0.5f - num.Z);
				velocity.Y += MathUtils.Saturate(20f * dt) * (num7 - velocity.Y);
				m_climbing = true;
				if (Math.Abs(value2.Y) > 0.1f && Time.FrameStartTime - m_lastLadderStepTime > 0.6)
				{
					m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
					m_lastLadderStepTime = Time.FrameStartTime;
				}
			}
			if (m_componentCreature.ComponentBody.StandingOnValue.HasValue && m_subsystemTime.GameTime >= m_ladderActivationTime)
			{
				LadderValue = null;
				m_ladderActivationTime = m_subsystemTime.GameTime + 0.20000000298023224;
			}
		}
		else
		{
			LadderValue = null;
			m_ladderActivationTime = m_subsystemTime.GameTime + 0.20000000298023224;
		}
		if (JumpOrder > 0f)
		{
			m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
			velocity += num5 * m_componentCreature.ComponentBody.Matrix.Forward;
			m_ladderActivationTime = m_subsystemTime.GameTime + 0.33000001311302185;
			LadderValue = null;
			m_jumping = true;
		}
		if (IsCreativeFlyEnabled)
		{
			m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
			LadderValue = null;
		}
		if (m_componentCreature.ComponentBody.ParentBody != null)
		{
			LadderValue = null;
		}
		m_componentCreature.ComponentBody.Velocity = velocity;
	}

	public static bool IsSpeedBoostBlock(int blockId)
	{
		if (blockId != 26 && blockId != 73 && blockId != 54 && blockId != 75 && blockId != 50 && blockId != 76 && blockId != 68 && blockId != 70 && blockId != 69 && blockId != 359 && blockId != 360 && blockId != 361 && blockId != 388 && blockId != 389)
		{
			return blockId == 390;
		}
		return true;
	}

	public float GetEmbraceCharge()
	{
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return MathUtils.Saturate(m_embraceChargeTime / 0.8f);
		}
		return 0f;
	}

	public float GetEmbraceChargeMas()
	{
		return 1.8f * GetEmbraceCharge();
	}

	private float GetEmbracePlayerSpeedFactor()
	{
		if (m_subsystemGameInfo == null || m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace)
		{
			return 1f;
		}
		if (m_componentPlayer == null)
		{
			return 1f;
		}
		ComponentAdrenaline componentAdrenaline = Entity.FindComponent<ComponentAdrenaline>();
		if (componentAdrenaline == null || componentAdrenaline.Adrenaline <= 0f)
		{
			return 1f;
		}
		float adrenaline = componentAdrenaline.Adrenaline;
		if (adrenaline <= 0f)
		{
			return 1f;
		}
		adrenaline = MathUtils.Min(adrenaline, 10f);
		float n = MathUtils.Min(adrenaline, 1.5f);
		float num = MathUtils.Pow(2f, n);
		if (adrenaline <= 1.5f)
		{
			return num;
		}
		float num2 = adrenaline - 1.5f;
		float num3 = num2 / (1f + num2);
		float num4 = 1f + 0.6f * num3;
		return num * num4;
	}

	private float GetEmbraceCreatureSizeFactor()
	{
		if (m_subsystemGameInfo == null || m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace)
		{
			return 1f;
		}
		if (m_componentPlayer != null)
		{
			return 1f;
		}
		float size = m_componentCreature.ComponentBody.Size;
		if (size <= 0f)
		{
			return 1f;
		}
		float num = MathUtils.Clamp(size, 0.25f, 4f);
		if (num <= 1f)
		{
			return MathUtils.Lerp(0.6f, 1f, (num - 0.25f) / 0.75f);
		}
		if (num <= 2.5f)
		{
			return MathUtils.Lerp(1f, 1.5f, (num - 1f) / 1.5f);
		}
		return 1.5f;
	}
}
