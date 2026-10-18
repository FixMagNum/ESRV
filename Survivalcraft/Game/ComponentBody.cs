using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBody : ComponentFrame, IUpdateable
{
	public struct CollisionBox
	{
		public int BlockValue;

		public Vector3 BlockVelocity;

		public ComponentBody ComponentBody;

		public BoundingBox Box;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemMovingBlocks m_subsystemMovingBlocks;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

	public Random m_random;

	public DynamicArray<CollisionBox> m_collisionBoxes;

	public DynamicArray<ComponentBody> m_componentBodies;

	public DynamicArray<IMovingBlockSet> m_movingBlockSets;

	public DynamicArray<CollisionBox> m_bodiesCollisionBoxes;

	public DynamicArray<CollisionBox> m_movingBlocksCollisionBoxes;

	public ComponentBody m_parentBody;

	public List<ComponentBody> m_childBodies;

	public Vector3 m_velocity;

	public bool m_isSneaking;

	public Vector3 m_totalImpulse;

	public Vector3 m_directMove;

	public bool m_fluidEffectsPlayed;

	public float m_stoppedTime;

	public static Vector3[] m_freeSpaceOffsets;

	public static bool DrawBodiesBounds;

	public const float SleepThresholdSpeed = 1E-05f;

	public const float MaxSpeed = 25f;

	public Vector3? SendVelocity;

	public Vector3 lastVelocity;

	public Action<ComponentBody> CollidedWithBody;

	public ComponentPlayer Player;

	public ComponentLocomotion Locomotion;

	public ComponentBirdModel BirdModel;

	private ushort parentEntityId;

	public NetPosition netPosition;

	public NetRotation netRotation;

	public NetVelocity netVelocity;

	private ComponentHumanModel humanModel;

	private float m_targetCrouchFactor;

	private float m_crouchFactor;

	private Vector3 defaultBoxSize;

	public float m_sneakCooldownTimer;

	public ComponentPlayer m_componentPlayer;

	public SubsystemSky m_subsystemSky;

	private float m_currentGravityAcceleration;

	private const float NormalGravity = 10f;

	private const float EclipseGravity = 5f;

	private const float GravityChangeRate = 2f;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemOceanScanner m_subsystemOceanScanner;

	private float defaultMass;

	private float size;

	private Vector3 m_smoothedWaveUp = Vector3.UnitY;

	private Vector3 m_lastTiltAxis = Vector3.UnitZ;

	public Vector3 m_embraceUpVector;

	public bool m_embraceStuck;

	public float m_embraceStickDistance;

	public int m_embraceStickBlockValue;

	public int m_embraceStickFace;

	public bool EmbraceAirCrouchLatched;

	private double m_lastBodyDamageTime;

	private int m_lastBodyDamageTargetId;

	public ComponentAdrenaline ComponentAdrenaline;

	public SubsystemFocalorsFluidBlockBehavior m_subsystemFocalorsFluidBlockBehavior;

	public ComponentInput m_componentInput;

	private float? m_pendingFallSpeed;

	public Vector3 BoxSize { get; set; }

	public float Mass { get; set; }

	public float Density { get; set; }

	public Vector2 AirDrag { get; set; }

	public Vector2 WaterDrag { get; set; }

	public float WaterSwayAngle { get; set; }

	public float WaterTurnSpeed { get; set; }

	public float ImmersionDepth { get; set; }

	public float ImmersionFactor { get; set; }

	public FluidBlock ImmersionFluidBlock { get; set; }

	public int? StandingOnValue { get; set; }

	public ComponentBody StandingOnBody { get; set; }

	public Vector3 StandingOnVelocity { get; set; }

	public Vector3 Velocity
	{
		get
		{
			if (m_velocity.LengthSquared() < 1E-05f)
			{
				m_velocity = Vector3.Zero;
			}
			if (lastVelocity != m_velocity)
			{
				lastVelocity = m_velocity;
				SendVelocity = m_velocity;
			}
			return m_velocity;
		}
		set
		{
			float num = 25f;
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && Player != null && ComponentAdrenaline != null && ComponentAdrenaline.Adrenaline > 0f)
			{
				float adrenaline = ComponentAdrenaline.Adrenaline;
				float i = MathUtils.Min(adrenaline, 1.5f);
				float num2 = MathUtils.Max(adrenaline - 1.5f, 0f);
				float num3 = 1f + 0.6f * i + 0.4f * (1f - 1f / (1f + num2));
				num *= num3;
			}
			float subsystemFurnitureBlockBehavior = num * num;
			if (value.LengthSquared() > subsystemFurnitureBlockBehavior)
			{
				m_velocity = num * Vector3.Normalize(value);
			}
			else
			{
				m_velocity = value;
			}
		}
	}

	public bool IsSneaking
	{
		get
		{
			return m_isSneaking;
		}
		set
		{
			bool num = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && HumanModel != null;
			bool a = Player != null && Player.PlayerData.IsMainPlayer;
			if (a && !StandingOnValue.HasValue && (!IsBlockAbove() || IsSafeDrop()))
			{
				if (num & value)
				{
					EmbraceAirCrouchLatched = true;
				}
				value = false;
			}
			bool a2 = m_sneakCooldownTimer <= 0f || !a;
			if (((m_isSneaking != value) & a2) && (value || !IsBlockAbove() || IsSafeDrop()))
			{
				TargetCrouchFactor = (value ? 1f : 0f);
				m_isSneaking = value;
				if (a)
				{
					SendSneakState();
				}
				UpdateCrouchState(value);
				UpdateBoxSize(value);
				m_sneakCooldownTimer = 0.7f;
			}
		}
	}

	public bool IsGravityEnabled { get; set; }

	public bool IsGroundDragEnabled { get; set; }

	public bool IsWaterDragEnabled { get; set; }

	public bool IsSmoothRiseEnabled { get; set; }

	public float MaxSmoothRiseHeight { get; set; }

	public Vector3 CollisionVelocityChange { get; set; }

	public BoundingBox BoundingBox
	{
		get
		{
			Vector3 vector = (IsSneaking ? SneakBoxSize : BoxSize);
			Vector3 position = base.Position;
			return new BoundingBox(position - new Vector3(vector.X / 2f, 0f, vector.Z / 2f), position + new Vector3(vector.X / 2f, vector.Y, vector.Z / 2f));
		}
	}

	public ReadOnlyList<ComponentBody> ChildBodies => new ReadOnlyList<ComponentBody>(m_childBodies);

	public ComponentBody ParentBody
	{
		get
		{
			return m_parentBody;
		}
		set
		{
			if (value != m_parentBody)
			{
				if (m_parentBody != null)
				{
					m_parentBody.m_childBodies.Remove(this);
				}
				m_parentBody = value;
				if (m_parentBody != null)
				{
					m_parentBody.m_childBodies.Add(this);
				}
			}
		}
	}

	public Vector3 ParentBodyPositionOffset { get; set; }

	public Quaternion ParentBodyRotationOffset { get; set; }

	public UpdateOrder UpdateOrder
	{
		get
		{
			if (m_parentBody == null)
			{
				return UpdateOrder.Body;
			}
			return m_parentBody.UpdateOrder + 1;
		}
	}

	public ComponentHumanModel HumanModel { get; private set; }

	public float TargetCrouchFactor
	{
		get
		{
			return m_targetCrouchFactor;
		}
		set
		{
			if (!StandingOnValue.HasValue && !IsBlockAbove())
			{
				value = 0f;
			}
			m_targetCrouchFactor = value;
		}
	}

	public float CrouchFactor
	{
		get
		{
			return m_crouchFactor;
		}
		set
		{
			if (!StandingOnValue.HasValue)
			{
				value = 0f;
			}
			m_crouchFactor = value;
			m_targetCrouchFactor = value;
		}
	}

	public Vector3 SneakBoxSize { get; set; }

	public float WindResistance { get; set; }

	public float Size
	{
		get
		{
			return size;
		}
		set
		{
			size = MathUtils.Clamp(value, 0f, 100f);
			UpdateBoxSize(IsSneaking);
			Mass = defaultMass * size;
		}
	}

	public FocalorsFluidBlock ImmersionFocalorsFluidBlock { get; set; }

	public float EffectiveRiseHeight
	{
		get
		{
			float slotValue = Size;
			if (IsHighStepEnabled && slotValue < 2f)
			{
				slotValue = 2f;
			}
			float slotValue2 = MaxSmoothRiseHeight * slotValue;
			return MathUtils.Max(0f, MathUtils.Min(slotValue2, BoxSize.Y - 0.15f));
		}
	}

	public bool IsHighStepEnabled { get; set; }

	static ComponentBody()
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = -2; i <= 2; i++)
		{
			for (int j = -2; j <= 2; j++)
			{
				for (int k = -2; k <= 2; k++)
				{
					Vector3 item = new Vector3(0.25f * (float)i, 0.25f * (float)j, 0.25f * (float)k);
					list.Add(item);
				}
			}
		}
		list.Sort((Vector3 o1, Vector3 o2) => Comparer<float>.Default.Compare(o1.LengthSquared(), o2.LengthSquared()));
		m_freeSpaceOffsets = list.ToArray();
	}

	public void ApplyImpulse(Vector3 impulse)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new SubsystemBodyPackage(this, impulse));
			ApplyImpulseNet(impulse);
		}
	}

	public void ApplyImpulseNet(Vector3 impulse)
	{
		m_totalImpulse += impulse;
	}

	public void ApplyDirectMove(Vector3 directMove)
	{
		m_directMove += directMove;
	}

	public bool IsChildOfBody(ComponentBody componentBody)
	{
		if (ParentBody != componentBody)
		{
			if (ParentBody != null)
			{
				return ParentBody.IsChildOfBody(componentBody);
			}
			return false;
		}
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		HumanModel = Entity.FindComponent<ComponentHumanModel>();
		humanModel = Entity.FindComponent<ComponentHumanModel>();
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
		ComponentAdrenaline = Entity.FindComponent<ComponentAdrenaline>();
		m_subsystemOceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentInput = Entity.FindComponent<ComponentInput>();
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
		m_subsystemFocalorsFluidBlockBehavior = Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		BoxSize = valuesDictionary.GetValue<Vector3>("BoxSize");
		defaultBoxSize = BoxSize;
		SneakBoxSize = valuesDictionary.GetValue<Vector3>("SneakBoxSize");
		Mass = valuesDictionary.GetValue<float>("Mass");
		defaultMass = Mass;
		IsHighStepEnabled = valuesDictionary.GetValue<bool>("IsHighStepEnabled");
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentPlayer == null)
		{
			IsHighStepEnabled = true;
		}
		if (valuesDictionary.ContainsKey("Size"))
		{
			float value = valuesDictionary.GetValue<float>("Size");
			Size = value;
		}
		if (m_componentPlayer != null)
		{
			float playerSizeMultiplier = m_subsystemGameInfo.WorldSettings.PlayerSizeMultiplier;
			if (Math.Abs(playerSizeMultiplier - 1f) > 0.001f)
			{
				Size *= playerSizeMultiplier;
			}
		}
		Density = valuesDictionary.GetValue<float>("Density");
		AirDrag = valuesDictionary.GetValue<Vector2>("AirDrag");
		WaterDrag = valuesDictionary.GetValue<Vector2>("WaterDrag");
		WaterSwayAngle = valuesDictionary.GetValue<float>("WaterSwayAngle");
		WaterTurnSpeed = valuesDictionary.GetValue<float>("WaterTurnSpeed");
		Velocity = valuesDictionary.GetValue<Vector3>("Velocity");
		if (valuesDictionary.ContainsKey("SavedFallSpeed"))
		{
			m_pendingFallSpeed = valuesDictionary.GetValue<float>("SavedFallSpeed");
		}
		MaxSmoothRiseHeight = valuesDictionary.GetValue<float>("MaxSmoothRiseHeight");
		WindResistance = (valuesDictionary.ContainsKey("WindResistance") ? MathUtils.Saturate(valuesDictionary.GetValue<float>("WindResistance")) : 0f);
		object value2 = valuesDictionary.GetValue<object>("ParentBody");
		if (value2 is ushort)
		{
			parentEntityId = (ushort)value2;
		}
		ParentBodyPositionOffset = valuesDictionary.GetValue<Vector3>("ParentBodyPositionOffset");
		ParentBodyRotationOffset = valuesDictionary.GetValue<Quaternion>("ParentBodyRotationOffset");
		IsSmoothRiseEnabled = true;
		IsGravityEnabled = true;
		IsGroundDragEnabled = true;
		IsWaterDragEnabled = true;
		Player = Entity.FindComponent<ComponentPlayer>();
		Locomotion = Entity.FindComponent<ComponentLocomotion>();
		BirdModel = Entity.FindComponent<ComponentBirdModel>();
		if (parentEntityId > 0)
		{
			Entity entity = idToEntityMap.FindEntity(parentEntityId);
			if (entity != null)
			{
				ParentBody = entity.FindComponent<ComponentBody>();
			}
		}
		netPosition = new NetPosition(Position);
		netRotation = new NetRotation(Rotation);
		netVelocity = new NetVelocity(Velocity);
		if (valuesDictionary.ContainsKey("IsSneaking"))
		{
			IsSneaking = valuesDictionary.GetValue<bool>("IsSneaking");
		}
		else
		{
			IsSneaking = false;
		}
		TargetCrouchFactor = (IsSneaking ? 1f : 0f);
		UpdateCrouchState(IsSneaking);
		UpdateBoxSize(IsSneaking);
		m_crouchFactor = (IsSneaking ? 1f : 0f);
		m_targetCrouchFactor = m_crouchFactor;
		if (valuesDictionary.ContainsKey("IsWrenchFrozen"))
		{
			Entity.ValuesDictionary.SetValue("IsWrenchFrozen", valuesDictionary.GetValue<bool>("IsWrenchFrozen"));
		}
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		if (Velocity != Vector3.Zero)
		{
			valuesDictionary.SetValue("Velocity", Velocity);
		}
		if (Velocity.Y < -0.1f)
		{
			valuesDictionary.SetValue("SavedFallSpeed", Velocity.Y);
		}
		if (ParentBody != null)
		{
			valuesDictionary.SetValue("ParentBody", ParentBody.Entity.EntityId);
			valuesDictionary.SetValue("ParentBodyPositionOffset", ParentBodyPositionOffset);
			valuesDictionary.SetValue("ParentBodyRotationOffset", ParentBodyRotationOffset);
		}
		else
		{
			valuesDictionary.SetValue("ParentBody", 0);
		}
		float num = 1f;
		if (m_componentPlayer != null)
		{
			num = m_subsystemGameInfo.WorldSettings.PlayerSizeMultiplier;
		}
		valuesDictionary.SetValue("IsHighStepEnabled", IsHighStepEnabled);
		valuesDictionary.SetValue("Size", Size / num);
		valuesDictionary.SetValue("IsSneaking", IsSneaking);
		if (Entity.ValuesDictionary.ContainsKey("IsWrenchFrozen"))
		{
			valuesDictionary.SetValue("IsWrenchFrozen", Entity.ValuesDictionary.GetValue<bool>("IsWrenchFrozen"));
		}
	}

	public override void OnEntityRemoved()
	{
		ParentBody = null;
		ComponentBody[] array = ChildBodies.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ParentBody = null;
		}
	}

	public void Update(float dt)
	{
		if (Entity.ValuesDictionary.GetValue("IsWrenchFrozen", defaultValue: false))
		{
			m_velocity = Vector3.Zero;
			return;
		}
		Quaternion rotation = Rotation;
		m_crouchFactor = MathUtils.Lerp(m_crouchFactor, m_targetCrouchFactor, 10f * dt);
		if (m_subsystemWeather != null && BoundingBox.Max.Y >= 45f && m_subsystemWeather.CurrentWindStrength >= 10f && m_sneakCooldownTimer <= 0f && (Player == null || m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative))
		{
			float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
			Vector3 position = new Vector3(Position.X, BoundingBox.Max.Y - 0.1f, Position.Z);
			float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(position);
			if (windExposureFactor > 0f && !IsBlockAbove())
			{
				float num = (currentWindStrength - 9f) * 0.05f;
				if (m_isSneaking && m_random.Float(0f, 1f) < num * windExposureFactor * dt)
				{
					StopSneak();
				}
			}
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			if ((Player != null && !Player.PlayerData.IsMainPlayer && Player.ComponentBody.ParentBody == null) || (Player == null && ChildBodies.Count == 0) || (ChildBodies.Count > 0 && ChildBodies[0].Player != null && !ChildBodies[0].Player.PlayerData.IsMainPlayer))
			{
				Position = netPosition.Get(dt);
				Rotation = netRotation.Get(dt);
				Velocity = netVelocity.Get(dt);
				return;
			}
			netPosition.SetNext(Position);
			netRotation.SetNext(Rotation);
			netVelocity.SetNext(Velocity);
		}
		if (m_sneakCooldownTimer > 0f)
		{
			m_sneakCooldownTimer -= dt;
		}
		if (CommonLib.WorkType == WorkType.Server)
		{
			if ((Player != null && !Player.PlayerData.IsMainPlayer && Player.ComponentBody.ParentBody == null) || (Player == null && ChildBodies.Count > 0 && ChildBodies[0].Player != null && !ChildBodies[0].Player.PlayerData.IsMainPlayer))
			{
				Position = netPosition.Get(dt);
				Rotation = netRotation.Get(dt);
				Velocity = netVelocity.Get(dt);
			}
			else
			{
				netPosition.SetNext(Position);
				netRotation.SetNext(Rotation);
				netVelocity.SetNext(Velocity);
			}
			bool isSneaking = m_isSneaking;
			m_isSneaking = TargetCrouchFactor == 1f || (m_isSneaking && IsBlockAbove() && !IsSafeDrop());
			if (isSneaking != m_isSneaking)
			{
				SendSneakState();
			}
		}
		Velocity += m_totalImpulse;
		m_totalImpulse = Vector3.Zero;
		if (m_parentBody != null || m_velocity.LengthSquared() > 9.9999994E-11f || m_directMove != Vector3.Zero || (ImmersionFactor > 0f && (ImmersionFluidBlock != null || ImmersionFocalorsFluidBlock != null)))
		{
			m_stoppedTime = 0f;
		}
		else
		{
			m_stoppedTime += dt;
			if (m_stoppedTime > 0.5f && !Time.PeriodicEvent(0.25, 0.0))
			{
				return;
			}
		}
		float num2 = 10f;
		if (m_subsystemSky != null && m_subsystemSky.IsSolarEclipse)
		{
			num2 = 5f;
		}
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && Player != null && ComponentAdrenaline != null && ComponentAdrenaline.Adrenaline > 0f)
		{
			float adrenaline = ComponentAdrenaline.Adrenaline;
			float num3 = MathUtils.Min(adrenaline, 1.5f);
			float num4 = MathUtils.Max(adrenaline - 1.5f, 0f);
			float num5 = 1f + 0.5f * num3 + 0.35f * (1f - 1f / (1f + num4));
			num2 *= num5;
		}
		if (m_currentGravityAcceleration < num2)
		{
			m_currentGravityAcceleration = MathUtils.Min(m_currentGravityAcceleration + 2f * dt, num2);
		}
		else if (m_currentGravityAcceleration > num2)
		{
			m_currentGravityAcceleration = MathUtils.Max(m_currentGravityAcceleration - 2f * dt, num2);
		}
		Vector3 position2 = base.Position;
		TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(position2.X), Terrain.ToCell(position2.Z));
		if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
		{
			Velocity = Vector3.Zero;
			return;
		}
		if (m_pendingFallSpeed.HasValue)
		{
			m_velocity.Y = m_pendingFallSpeed.Value;
			m_pendingFallSpeed = null;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && m_componentInput != null && m_componentInput.IsCreativeFlySpeedActive && Locomotion != null && Locomotion.IsCreativeFlyEnabled)
		{
			Position += Velocity * dt;
			m_bodiesCollisionBoxes.Clear();
			m_movingBlocksCollisionBoxes.Clear();
			return;
		}
		m_bodiesCollisionBoxes.Clear();
		FindBodiesCollisionBoxes(position2, m_bodiesCollisionBoxes);
		m_movingBlocksCollisionBoxes.Clear();
		FindMovingBlocksCollisionBoxes(position2, m_movingBlocksCollisionBoxes);
		if (!MoveToFreeSpace())
		{
			if (IsBlockAbove() && !IsSafeDrop())
			{
				StartSneak();
			}
			return;
		}
		float num6 = 0f;
		if (Player != null)
		{
			ComponentClothing componentClothing = m_componentPlayer.Entity.FindComponent<ComponentClothing>();
			if (componentClothing != null)
			{
				foreach (ClothingSlot value2 in Enum.GetValues(typeof(ClothingSlot)))
				{
					foreach (int item in componentClothing.GetClothes(value2))
					{
						ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item)].GetClothingData(Terrain.ExtractData(item));
						if (clothingData != null)
						{
							num6 += clothingData.Weight;
						}
					}
				}
			}
		}
		bool flag = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && Player == null;
		if (!flag)
		{
			m_embraceStuck = false;
			m_embraceUpVector = Vector3.UnitY;
		}
		else
		{
			if (m_embraceStuck && m_velocity.Y > 0.1f)
			{
				m_embraceStuck = false;
				m_embraceUpVector = Vector3.UnitY;
			}
			int num7 = Terrain.ToCell(Position.X);
			int num8 = Terrain.ToCell(Position.Y);
			int num9 = Terrain.ToCell(Position.Z);
			if (BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num7, num8 - 1, num9)].IsCollidable)
			{
				m_embraceStuck = false;
				m_embraceUpVector = Vector3.UnitY;
			}
			else
			{
				int num10 = Terrain.ToCell(Position.Y - 0.01f);
				int num11 = Terrain.ToCell(Position.Y + BoxSize.Y - 0.01f);
				float num12 = BoxSize.X * 0.5f;
				float num13 = BoxSize.Z * 0.5f;
				float num14 = 1E+09f;
				Vector3 embraceUpVector = Vector3.UnitY;
				int embraceStickBlockValue = 0;
				float embraceStickDistance = m_embraceStickDistance;
				float num15 = 0.06f;
				bool flag2 = false;
				int num16 = 0;
				for (int i = num10; i <= num11; i++)
				{
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(num7 + 1, i, num9);
					if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable)
					{
						flag2 = true;
						num16 = cellValue;
						break;
					}
				}
				float num17 = (float)(num7 + 1) - (Position.X + num12);
				float num18 = MathUtils.Abs(num17);
				if (flag2 && num17 <= embraceStickDistance && num17 >= 0f - num15 && num18 < num14)
				{
					num14 = num18;
					embraceUpVector = -Vector3.UnitX;
					embraceStickBlockValue = num16;
					m_embraceStickFace = 1;
				}
				bool flag3 = false;
				int num19 = 0;
				for (int j = num10; j <= num11; j++)
				{
					int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(num7 - 1, j, num9);
					if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)].IsCollidable)
					{
						flag3 = true;
						num19 = cellValue2;
						break;
					}
				}
				float num20 = num7;
				float num21 = Position.X - num12 - num20;
				float num22 = MathUtils.Abs(num21);
				if (flag3 && num21 <= embraceStickDistance && num21 >= 0f - num15 && num22 < num14)
				{
					num14 = num22;
					embraceUpVector = Vector3.UnitX;
					embraceStickBlockValue = num19;
					m_embraceStickFace = 3;
				}
				bool flag4 = false;
				int num23 = 0;
				for (int k = num10; k <= num11; k++)
				{
					int cellValue3 = m_subsystemTerrain.Terrain.GetCellValue(num7, k, num9 + 1);
					if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue3)].IsCollidable)
					{
						flag4 = true;
						num23 = cellValue3;
						break;
					}
				}
				float num24 = (float)(num9 + 1) - (Position.Z + num13);
				float num25 = MathUtils.Abs(num24);
				if (flag4 && num24 <= embraceStickDistance && num24 >= 0f - num15 && num25 < num14)
				{
					num14 = num25;
					embraceUpVector = -Vector3.UnitZ;
					embraceStickBlockValue = num23;
					m_embraceStickFace = 0;
				}
				bool flag5 = false;
				int num26 = 0;
				for (int l = num10; l <= num11; l++)
				{
					int cellValue4 = m_subsystemTerrain.Terrain.GetCellValue(num7, l, num9 - 1);
					if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue4)].IsCollidable)
					{
						flag5 = true;
						num26 = cellValue4;
						break;
					}
				}
				float num27 = num9;
				float num28 = Position.Z - num13 - num27;
				float num29 = MathUtils.Abs(num28);
				if (flag5 && num28 <= embraceStickDistance && num28 >= 0f - num15 && num29 < num14)
				{
					num14 = num29;
					embraceUpVector = Vector3.UnitZ;
					embraceStickBlockValue = num26;
					m_embraceStickFace = 2;
				}
				int num30 = Terrain.ToCell(Position.Y + BoxSize.Y + 0.001f);
				int cellValue5 = m_subsystemTerrain.Terrain.GetCellValue(num7, num30, num9);
				bool isCollidable = BlocksManager.Blocks[Terrain.ExtractContents(cellValue5)].IsCollidable;
				float num31 = (float)num30 - (Position.Y + BoxSize.Y);
				float num32 = MathUtils.Abs(num31);
				if (isCollidable && num31 <= embraceStickDistance && num31 >= 0f - num15 && num32 < num14)
				{
					num14 = num32;
					embraceUpVector = -Vector3.UnitY;
					embraceStickBlockValue = cellValue5;
					m_embraceStickFace = 4;
				}
				if (num14 < 1E+09f)
				{
					m_embraceStuck = true;
					m_embraceUpVector = embraceUpVector;
					m_embraceStickBlockValue = embraceStickBlockValue;
				}
				else
				{
					m_embraceStuck = false;
					m_embraceUpVector = Vector3.UnitY;
				}
			}
		}
		if (IsGravityEnabled)
		{
			float num33 = m_currentGravityAcceleration;
			if (Player != null)
			{
				float num34 = 0f;
				ComponentClothing componentClothing2 = Player.Entity.FindComponent<ComponentClothing>();
				if (componentClothing2 != null)
				{
					foreach (ClothingSlot value3 in Enum.GetValues(typeof(ClothingSlot)))
					{
						foreach (int item2 in componentClothing2.GetClothes(value3))
						{
							ClothingData clothingData2 = BlocksManager.Blocks[Terrain.ExtractContents(item2)].GetClothingData(Terrain.ExtractData(item2));
							if (clothingData2 != null)
							{
								num34 += clothingData2.Weight;
							}
						}
					}
				}
				num33 += num34;
			}
			Vector3 vector = (flag ? m_embraceUpVector : Vector3.UnitY);
			m_velocity += (0f - num33) * vector * dt;
			if (ImmersionFactor > 0f)
			{
				if (ImmersionFluidBlock is WaterBlock || ImmersionFocalorsFluidBlock is FocalorsWaterBlock)
				{
					float num35 = 0f;
					if (IsBelowBedrockOcean(Position))
					{
						num35 = ImmersionFactor;
					}
					if (m_subsystemOceanScanner != null)
					{
						int x = Terrain.ToCell(position2.X);
						int y = Terrain.ToCell(position2.Y + 0.01f);
						int z = Terrain.ToCell(position2.Z);
						float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z, out var surfaceFluidBlock);
						FocalorsFluidBlock surfaceFluidBlock2 = null;
						float? num36 = ((m_subsystemFocalorsFluidBlockBehavior != null) ? m_subsystemFocalorsFluidBlockBehavior.GetSurfaceHeight(x, y, z, out surfaceFluidBlock2) : ((float?)null));
						if (surfaceHeight.HasValue && surfaceFluidBlock is WaterBlock)
						{
							num35 = MathUtils.Saturate(MathUtils.Pow(MathUtils.Max(m_subsystemOceanScanner.GetBlockWaveSurfaceY(Position, surfaceHeight.Value) - Position.Y, 0f) / BoxSize.Y, 0.7f));
						}
						else if (num36.HasValue && surfaceFluidBlock2 is FocalorsWaterBlock)
						{
							num35 = MathUtils.Saturate(MathUtils.Pow(MathUtils.Max(m_subsystemOceanScanner.GetBlockWaveSurfaceY(Position, num36.Value) - Position.Y, 0f) / BoxSize.Y, 0.7f));
						}
						else if (m_subsystemOceanScanner.IsOcean)
						{
							num35 = MathUtils.Saturate(MathUtils.Pow(MathUtils.Max(m_subsystemOceanScanner.GetWaveSurfaceY(Position) - Position.Y, 0f) / BoxSize.Y, 0.7f));
						}
					}
					else
					{
						num35 = ImmersionFactor;
					}
					m_velocity.Y += m_currentGravityAcceleration * (1f / Density * num35) * dt;
				}
				else if (ImmersionFluidBlock is QuicksandBlock)
				{
					float num37 = ImmersionFactor * (1f + 0.03f * MathUtils.Sin((float)MathUtils.Remainder(2.0 * m_subsystemTime.GameTime, 6.2831854820251465)));
					m_velocity.Y += m_currentGravityAcceleration * (1f / Density * num37) * dt * 0.1f;
					m_velocity.Y -= 2f * ImmersionFactor * dt;
				}
				else
				{
					float num38 = ImmersionFactor * (1f + 0.03f * MathUtils.Sin((float)MathUtils.Remainder(2.0 * m_subsystemTime.GameTime, 6.2831854820251465)));
					m_velocity.Y += m_currentGravityAcceleration * (1f / Density * num38) * dt;
				}
			}
		}
		float num39 = MathUtils.Saturate(AirDrag.X * dt);
		float num40 = MathUtils.Saturate(AirDrag.Y * dt);
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && Player != null && ComponentAdrenaline != null && ComponentAdrenaline.Adrenaline > 0f)
		{
			float adrenaline2 = ComponentAdrenaline.Adrenaline;
			float num41 = MathUtils.Min(adrenaline2, 1.5f);
			float num42 = MathUtils.Max(adrenaline2 - 1.5f, 0f);
			float num43 = 1f + 0.5f * num41 + 0.25f * (1f - 1f / (1f + num42));
			float num44 = 1f / num43;
			num39 *= num44;
			num40 *= num44;
		}
		m_velocity.X *= 1f - num39;
		m_velocity.Y *= 1f - num40;
		m_velocity.Z *= 1f - num39;
		if (m_subsystemWeather != null)
		{
			float currentWindStrength2 = m_subsystemWeather.CurrentWindStrength;
			Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
			Vector3 position3 = new Vector3(Position.X, BoundingBox.Max.Y - 0.1f, Position.Z);
			float windExposureFactor2 = m_subsystemWeather.GetWindExposureFactor(position3);
			float windEffectCoefficient = GetWindEffectCoefficient(Mass, currentWindStrength2);
			float adjustedWindResistance = GetAdjustedWindResistance();
			windEffectCoefficient *= 1f - adjustedWindResistance;
			float num45 = 0f;
			if (Player != null)
			{
				ComponentClothing componentClothing3 = Player.Entity.FindComponent<ComponentClothing>();
				if (componentClothing3 != null)
				{
					foreach (ClothingSlot value4 in Enum.GetValues(typeof(ClothingSlot)))
					{
						foreach (int item3 in componentClothing3.GetClothes(value4))
						{
							ClothingData clothingData3 = BlocksManager.Blocks[Terrain.ExtractContents(item3)].GetClothingData(Terrain.ExtractData(item3));
							if (clothingData3 != null)
							{
								num45 += clothingData3.Weight;
							}
						}
					}
				}
			}
			float num46 = MathUtils.Saturate(1f - num45 * 1.5f / 8f);
			if (BoundingBox.Max.Y >= 45f && (Player == null || m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative) && windEffectCoefficient > 0f && windExposureFactor2 > 0f)
			{
				float num47 = 1f;
				float num48 = (float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0);
				float num49 = (Position.X + Position.Z) * 0.05f;
				if (m_subsystemWeather.IsGustyWind)
				{
					float num50 = SimplexNoise.Noise(num48 * 2.5f + num49, num48 * 2.5f - num49);
					num47 = MathUtils.Clamp(1f + num50 * 2f, 0.2f, 3f);
				}
				Vector3 vector2 = new Vector3(currentWindVector.X, 0f, currentWindVector.Y) * windEffectCoefficient * windExposureFactor2 * num46 * num47;
				m_velocity += vector2 * dt;
				m_velocity += vector2 * dt;
				if (currentWindStrength2 >= 10f && SimplexNoise.Noise(num48 * 4f - num49, num49) > 0.4f)
				{
					float num51 = 0f;
					float num52 = SimplexNoise.Noise(num49, num48);
					if (currentWindStrength2 >= 14f)
					{
						num51 = MathUtils.Lerp(0.1f, 0.2f, (num52 + 1f) * 0.5f) * windEffectCoefficient;
					}
					else if (currentWindStrength2 >= 10f)
					{
						num51 = MathUtils.Lerp(0.05f, 0.1f, (num52 + 1f) * 0.5f) * windEffectCoefficient;
					}
					m_velocity.Y += num51 * num47 * dt;
				}
				if (Mass <= 70f && currentWindStrength2 >= 7f && Locomotion != null && Locomotion.WalkOrder.HasValue)
				{
					float num53 = MathUtils.Saturate((currentWindStrength2 - 6f) / 3f);
					Vector2 value = Locomotion.WalkOrder.Value * (1f - 0.5f * num53);
					Locomotion.WalkOrder = value;
				}
			}
		}
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && Player != null && ComponentAdrenaline != null && ComponentAdrenaline.Adrenaline > 0f)
		{
			float adrenaline3 = ComponentAdrenaline.Adrenaline;
			float num54 = MathUtils.Min(adrenaline3, 1.5f);
			float num55 = MathUtils.Max(adrenaline3 - 1.5f, 0f);
			float num56 = 1f + 0.5f * num54 + 0.25f * (1f - 1f / (1f + num55));
			float num57 = 1f / num56;
			num39 *= num57;
			num40 *= num57;
		}
		m_velocity.X *= 1f - num39;
		m_velocity.Y *= 1f - num40;
		m_velocity.Z *= 1f - num39;
		if (IsWaterDragEnabled && ImmersionFactor > 0f && (ImmersionFluidBlock != null || ImmersionFocalorsFluidBlock != null))
		{
			Vector2? vector3 = ((ImmersionFluidBlock != null) ? m_subsystemFluidBlockBehavior.CalculateFlowSpeed(Terrain.ToCell(position2.X), Terrain.ToCell(position2.Y), Terrain.ToCell(position2.Z)) : ((Vector2?)null));
			Vector2? vector4 = ((ImmersionFocalorsFluidBlock != null) ? m_subsystemFocalorsFluidBlockBehavior.CalculateFlowSpeed(Terrain.ToCell(position2.X), Terrain.ToCell(position2.Y), Terrain.ToCell(position2.Z)) : ((Vector2?)null));
			float num58 = 20.3f;
			if (!vector3.HasValue && vector4.HasValue)
			{
				vector3 = vector4;
			}
			Vector3 vector5 = (vector3.HasValue ? (new Vector3(vector3.Value.X, 0f, vector3.Value.Y) * ((ImmersionFocalorsFluidBlock != null) ? num58 : 1f)) : Vector3.Zero);
			float num59 = 1f;
			if ((ImmersionFluidBlock != null && ImmersionFluidBlock.FrictionFactor != 1f) || (ImmersionFocalorsFluidBlock != null && ImmersionFocalorsFluidBlock.FrictionFactor != 1f))
			{
				float num60 = ((ImmersionFluidBlock != null) ? ImmersionFluidBlock.FrictionFactor : ImmersionFocalorsFluidBlock.FrictionFactor);
				num59 = ((SimplexNoise.Noise((float)MathUtils.Remainder(6.0 * Time.FrameStartTime + (double)(GetHashCode() % 1000), 1000.0)) > 0.5f) ? num60 : 1f);
			}
			float f = MathUtils.Saturate(WaterDrag.X * num59 * ImmersionFactor * dt);
			float f2 = MathUtils.Saturate(WaterDrag.Y * num59 * dt);
			if (ImmersionFluidBlock is QuicksandBlock)
			{
				f = MathUtils.Saturate(WaterDrag.X * num59 * ImmersionFactor * dt * 5f);
				f2 = MathUtils.Saturate(WaterDrag.Y * num59 * dt * 5f);
			}
			m_velocity.X = MathUtils.Lerp(m_velocity.X, vector5.X, f);
			m_velocity.Y = MathUtils.Lerp(m_velocity.Y, vector5.Y, f2);
			m_velocity.Z = MathUtils.Lerp(m_velocity.Z, vector5.Z, f);
			if (m_parentBody == null && !StandingOnValue.HasValue && m_parentBody == null && vector3.HasValue && !StandingOnValue.HasValue)
			{
				if (WaterTurnSpeed > 0f && vector3.HasValue)
				{
					float num61 = MathUtils.Saturate(MathUtils.Lerp(1f, 0f, m_velocity.Length()));
					Vector2 vector6 = Vector2.Normalize(vector3.Value) * num61;
					Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, WaterTurnSpeed * (-1f * vector6.X + 0.71f * vector6.Y) * dt);
				}
				if (WaterSwayAngle > 0f)
				{
					if (((m_subsystemWeather != null) ? m_subsystemWeather.CurrentWindStrength : 0f) > 6f)
					{
						Vector3 v = Vector3.UnitY;
						if (m_subsystemOceanScanner != null)
						{
							int x2 = Terrain.ToCell(Position.X);
							int y2 = Terrain.ToCell(Position.Y + 0.01f);
							int z2 = Terrain.ToCell(Position.Z);
							float? surfaceHeight2 = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x2, y2, z2, out var surfaceFluidBlock3);
							v = ((!surfaceHeight2.HasValue || !(surfaceFluidBlock3 is WaterBlock)) ? m_subsystemOceanScanner.GetWaveNormal(Position) : m_subsystemOceanScanner.GetBlockWaveNormal(Position, surfaceHeight2.Value));
						}
						float f3 = 1f - MathUtils.Exp((0f - dt) / 0.15f);
						m_smoothedWaveUp = Vector3.Normalize(Vector3.Lerp(m_smoothedWaveUp, v, f3));
						Vector3 v2 = Vector3.Transform(Vector3.UnitY, Rotation);
						Vector3 vector7 = Vector3.Cross(v2, m_smoothedWaveUp);
						float num62 = vector7.Length();
						vector7 = ((num62 > 1E-06f) ? (vector7 / num62) : m_lastTiltAxis);
						float num63 = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v2, m_smoothedWaveUp), -1f, 1f));
						float f4 = ((m_subsystemOceanScanner != null) ? m_subsystemOceanScanner.OceanWaveStrength : 0f);
						float num64 = MathUtils.Max(0.4f, WaterSwayAngle);
						float num65 = MathUtils.Lerp(8f, 40f, f4) * ImmersionFactor;
						float num66 = MathUtils.Min(num63 * num64, num65 * dt);
						if (num66 > 1E-05f)
						{
							Rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(vector7, num66) * Rotation);
							m_lastTiltAxis = vector7;
						}
					}
					else
					{
						Vector3 unitY = Vector3.UnitY;
						Vector3 v3 = Vector3.Transform(Vector3.UnitY, Rotation);
						Vector3 vector8 = Vector3.Cross(v3, unitY);
						float num67 = vector8.Length();
						if (num67 > 1E-06f)
						{
							vector8 /= num67;
						}
						float x3 = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v3, unitY), -1f, 1f));
						float num68 = 40f;
						float num69 = MathUtils.Min(x3, num68 * dt);
						if (num69 > 1E-05f)
						{
							Rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(vector8, num69) * Rotation);
							m_lastTiltAxis = vector8;
						}
						float f5 = 1f - MathUtils.Exp((0f - dt) / 0.15f);
						m_smoothedWaveUp = Vector3.Normalize(Vector3.Lerp(m_smoothedWaveUp, unitY, f5));
					}
				}
			}
		}
		if (m_parentBody != null)
		{
			Vector3 vector9 = Vector3.Transform(ParentBodyPositionOffset, m_parentBody.Rotation) + m_parentBody.Position - position2;
			m_velocity = ((dt > 0f) ? (vector9 / dt) : Vector3.Zero);
			Rotation = ParentBodyRotationOffset * m_parentBody.Rotation;
		}
		if (IsSneaking && m_subsystemTerrain != null && (Locomotion == null || !Locomotion.IsCreativeFlyEnabled) && new Vector2(m_velocity.X, m_velocity.Z).LengthSquared() > 0.001f)
		{
			Vector3 vector10 = Vector3.Normalize(new Vector3(m_velocity.X, 0f, m_velocity.Z));
			Vector3 vector11 = Position + vector10 * (BoxSize.X * 0.5f + 0.2f);
			int x4 = Terrain.ToCell(vector11.X);
			int num70 = Terrain.ToCell(Position.Y + 0.1f);
			int z3 = Terrain.ToCell(vector11.Z);
			int cellValue6 = m_subsystemTerrain.Terrain.GetCellValue(x4, num70, z3);
			int num71 = Terrain.ExtractContents(cellValue6);
			int num72 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x4, num70 + 1, z3));
			if (num71 != 0 && BlocksManager.Blocks[num71].IsCollidable && (num72 == 0 || !BlocksManager.Blocks[num72].IsCollidable))
			{
				bool flag6 = true;
				BoundingBox[] customCollisionBoxes = BlocksManager.Blocks[num71].GetCustomCollisionBoxes(m_subsystemTerrain, cellValue6);
				if (customCollisionBoxes != null && customCollisionBoxes.Length != 0)
				{
					flag6 = false;
					for (int m = 0; m < customCollisionBoxes.Length; m++)
					{
						Vector3 vector12 = customCollisionBoxes[m].Max - customCollisionBoxes[m].Min;
						if (vector12.Y >= 0.9f && vector12.X >= 0.8f && vector12.Z >= 0.8f)
						{
							flag6 = true;
							break;
						}
					}
				}
				if (flag6)
				{
					m_velocity.Y = MathUtils.Max(m_velocity.Y, 3.5f);
				}
			}
		}
		StandingOnValue = null;
		StandingOnBody = null;
		StandingOnVelocity = Vector3.Zero;
		Vector3 velocity = m_velocity;
		float num73 = m_velocity.Length();
		if (num73 > 0f)
		{
			float x5 = 0.25f * MathUtils.Min(BoxSize.X, BoxSize.Y, BoxSize.Z) / num73;
			float num74 = dt;
			while (num74 > 0f)
			{
				float num75 = MathUtils.Min(num74, x5);
				MoveWithCollision(num75, m_velocity * num75 + m_directMove);
				m_directMove = Vector3.Zero;
				num74 -= num75;
			}
		}
		CollisionVelocityChange = m_velocity - velocity;
		if (flag && m_embraceStuck && m_embraceStickBlockValue != 0)
		{
			StandingOnValue = m_embraceStickBlockValue;
			StandingOnBody = null;
			StandingOnVelocity = Vector3.Zero;
		}
		if (IsGroundDragEnabled && StandingOnValue.HasValue)
		{
			m_velocity = Vector3.Lerp(m_velocity, StandingOnVelocity, 6f * dt);
		}
		UpdateImmersionData();
		if ((ImmersionFluidBlock is WaterBlock || ImmersionFocalorsFluidBlock is FocalorsWaterBlock) && ImmersionDepth > 0.3f && !m_fluidEffectsPlayed)
		{
			m_fluidEffectsPlayed = true;
			m_subsystemAudio.PlayRandomSound("Audio/WaterFallIn", m_random.Float(0.75f, 1f), m_random.Float(-0.3f, 0f), position2, 4f, autoDelay: true);
			m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, position2, (BoundingBox.Max - BoundingBox.Min).Length() > 0.8f));
			return;
		}
		if ((ImmersionFluidBlock is MagmaBlock || ImmersionFocalorsFluidBlock is FocalorsMagmaBlock) && ImmersionDepth > 0f && !m_fluidEffectsPlayed)
		{
			m_fluidEffectsPlayed = true;
			m_subsystemAudio.PlaySound("Audio/SizzleLong", 1f, 0f, position2, 4f, autoDelay: true);
			m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, position2, (BoundingBox.Max - BoundingBox.Min).Length() > 0.8f));
			return;
		}
		if (ImmersionFluidBlock == null && ImmersionFocalorsFluidBlock == null)
		{
			m_fluidEffectsPlayed = false;
		}
		if (flag)
		{
			Vector3 v4 = Vector3.Transform(Vector3.UnitY, Rotation);
			Vector3 embraceUpVector2 = m_embraceUpVector;
			float num76 = MathUtils.Acos(MathUtils.Clamp(Vector3.Dot(v4, embraceUpVector2), -1f, 1f));
			if (num76 > 0.001f)
			{
				Vector3 axis = Vector3.Cross(v4, embraceUpVector2);
				float num77 = axis.Length();
				if (num77 > 1E-06f)
				{
					axis /= num77;
				}
				float x6 = 80f * dt;
				float angle = MathUtils.Min(num76, x6);
				Rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(axis, angle) * Rotation);
			}
		}
		Quaternion rotation2 = Rotation;
		float num78 = ((ImmersionFluidBlock is WaterBlock && ImmersionFactor > 0f) ? MathUtils.Lerp(10f, 45f, (m_subsystemOceanScanner != null) ? m_subsystemOceanScanner.OceanWaveStrength : 0f) : 5f);
		Rotation = Quaternion.Slerp(rotation, rotation2, MathUtils.Clamp(num78 * dt, 0f, 1f));
	}

	public void UpdateImmersionData()
	{
		Vector3 position = base.Position;
		int x = Terrain.ToCell(position.X);
		int num = Terrain.ToCell(position.Y + 0.01f);
		int z = Terrain.ToCell(position.Z);
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num, z);
		int num2 = Terrain.ExtractContents(cellValue);
		SubsystemFocalorsFluidBlockBehavior subsystemFocalorsFluidBlockBehavior = null;
		if (m_subsystemBlockBehaviors != null)
		{
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				if (blockBehaviors[i] is SubsystemFocalorsFluidBlockBehavior subsystemFocalorsFluidBlockBehavior2)
				{
					subsystemFocalorsFluidBlockBehavior = subsystemFocalorsFluidBlockBehavior2;
					break;
				}
			}
		}
		float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, num, z, out var surfaceFluidBlock);
		FocalorsFluidBlock surfaceFluidBlock2 = null;
		float? num3 = subsystemFocalorsFluidBlockBehavior?.GetSurfaceHeight(x, num, z, out surfaceFluidBlock2);
		ImmersionFluidBlock = null;
		ImmersionFocalorsFluidBlock = null;
		ImmersionDepth = 0f;
		ImmersionFactor = 0f;
		if (BlocksManager.Blocks[num2] is IWaterloggable waterloggable && waterloggable.IsWaterLogged(Terrain.ExtractData(cellValue)))
		{
			ImmersionDepth = MathUtils.Max((float)(num + 1) - position.Y, 0f);
			ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
			ImmersionFocalorsFluidBlock = BlocksManager.Blocks[18] as FocalorsFluidBlock;
			return;
		}
		int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num - 1, z));
		int num5 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num + 1, z));
		if (num2 == 92 || num2 == 391 || num2 == 438)
		{
			float? num6 = ((num2 == 92) ? num3 : surfaceHeight);
			if (num6.HasValue)
			{
				float value = num6.Value;
				ImmersionDepth = MathUtils.Max(value - position.Y, 0f);
				ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
				if (num2 == 92)
				{
					ImmersionFocalorsFluidBlock = surfaceFluidBlock2;
				}
				else
				{
					ImmersionFluidBlock = surfaceFluidBlock;
				}
			}
			return;
		}
		float bedrockTopY = GetBedrockTopY(Terrain.ToCell(position.X), Terrain.ToCell(position.Z));
		if (position.Y <= bedrockTopY + 0.001f)
		{
			float num7 = bedrockTopY;
			ImmersionDepth = MathUtils.Max(num7 - position.Y, 0f);
			ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
			ImmersionFocalorsFluidBlock = BlocksManager.Blocks[18] as FocalorsFluidBlock;
			return;
		}
		bool flag = num2 == 437 || num4 == 437 || num5 == 437;
		bool num8 = num2 == 18 || num4 == 18 || num5 == 18;
		int y = num;
		if (num2 == 18)
		{
			y = num;
		}
		else if (num4 == 18)
		{
			y = num - 1;
		}
		else if (num5 == 18)
		{
			y = num + 1;
		}
		if (num8)
		{
			FocalorsFluidBlock surfaceFluidBlock3 = null;
			float? num9 = ((m_subsystemFocalorsFluidBlockBehavior != null) ? m_subsystemFocalorsFluidBlockBehavior.GetSurfaceHeight(x, y, z, out surfaceFluidBlock3) : ((float?)null));
			if (num9.HasValue && surfaceFluidBlock3 is FocalorsWaterBlock)
			{
				if (m_subsystemOceanScanner != null)
				{
					num9 = m_subsystemOceanScanner.GetBlockWaveSurfaceY(position, num9.Value);
				}
				ImmersionDepth = MathUtils.Max(num9.Value - position.Y, 0f);
				ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
				ImmersionFocalorsFluidBlock = surfaceFluidBlock3;
				return;
			}
		}
		int y2 = num;
		if (num2 == 437)
		{
			y2 = num;
		}
		else if (num4 == 437)
		{
			y2 = num - 1;
		}
		else if (num5 == 437)
		{
			y2 = num + 1;
		}
		if (flag)
		{
			float? surfaceHeight2 = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y2, z, out var surfaceFluidBlock4);
			if (surfaceHeight2.HasValue && surfaceFluidBlock4 is WaterBlock)
			{
				float num10 = ((m_subsystemOceanScanner != null) ? m_subsystemOceanScanner.GetBlockWaveSurfaceY(position, surfaceHeight2.Value) : surfaceHeight2.Value);
				ImmersionDepth = MathUtils.Max(num10 - position.Y, 0f);
				ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
				ImmersionFluidBlock = surfaceFluidBlock4;
				return;
			}
		}
		if (surfaceHeight.HasValue)
		{
			float num11 = surfaceHeight.Value;
			if (surfaceFluidBlock is WaterBlock && m_subsystemOceanScanner != null)
			{
				num11 = m_subsystemOceanScanner.GetBlockWaveSurfaceY(position, num11);
			}
			ImmersionDepth = MathUtils.Max(num11 - position.Y, 0f);
			ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
			ImmersionFluidBlock = surfaceFluidBlock;
		}
		else if (num3.HasValue)
		{
			float num12 = num3.Value;
			if (surfaceFluidBlock2 is FocalorsWaterBlock && m_subsystemOceanScanner != null)
			{
				num12 = m_subsystemOceanScanner.GetBlockWaveSurfaceY(position, num12);
			}
			ImmersionDepth = MathUtils.Max(num12 - position.Y, 0f);
			ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
			ImmersionFocalorsFluidBlock = surfaceFluidBlock2;
		}
	}

	public bool MoveToFreeSpace()
	{
		Vector3 boxSize = BoxSize;
		Vector3 position = Position;
		for (int i = 0; i < m_freeSpaceOffsets.Length; i++)
		{
			Vector3? vector = null;
			Vector3 vector2 = position + m_freeSpaceOffsets[i];
			if (Terrain.ToCell(vector2) != Terrain.ToCell(position))
			{
				continue;
			}
			BoundingBox box = new BoundingBox(vector2 - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), vector2 + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
			box.Min += new Vector3(0.01f, EffectiveRiseHeight + 0.01f, 0.01f);
			box.Max -= new Vector3(0.01f);
			m_collisionBoxes.Clear();
			FindTerrainCollisionBoxes(box, m_collisionBoxes);
			m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
			m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
			if (!IsColliding(box, m_collisionBoxes))
			{
				vector = vector2;
			}
			else
			{
				m_stoppedTime = 0f;
				float num = CalculatePushBack(box, 0, m_collisionBoxes, out var _);
				float num2 = CalculatePushBack(box, 1, m_collisionBoxes, out var _);
				float num3 = CalculatePushBack(box, 2, m_collisionBoxes, out var _);
				float num4 = num * num;
				float num5 = num2 * num2;
				float num6 = num3 * num3;
				List<Vector3> list = new List<Vector3>();
				if (num4 <= num5 && num4 <= num6)
				{
					list.Add(vector2 + new Vector3(num, 0f, 0f));
					if (num5 <= num6)
					{
						list.Add(vector2 + new Vector3(0f, num2, 0f));
						list.Add(vector2 + new Vector3(0f, 0f, num3));
					}
					else
					{
						list.Add(vector2 + new Vector3(0f, 0f, num3));
						list.Add(vector2 + new Vector3(0f, num2, 0f));
					}
				}
				else if (num5 <= num4 && num5 <= num6)
				{
					list.Add(vector2 + new Vector3(0f, num2, 0f));
					if (num4 <= num6)
					{
						list.Add(vector2 + new Vector3(num, 0f, 0f));
						list.Add(vector2 + new Vector3(0f, 0f, num3));
					}
					else
					{
						list.Add(vector2 + new Vector3(0f, 0f, num3));
						list.Add(vector2 + new Vector3(num, 0f, 0f));
					}
				}
				else
				{
					list.Add(vector2 + new Vector3(0f, 0f, num3));
					if (num4 <= num5)
					{
						list.Add(vector2 + new Vector3(num, 0f, 0f));
						list.Add(vector2 + new Vector3(0f, num2, 0f));
					}
					else
					{
						list.Add(vector2 + new Vector3(0f, num2, 0f));
						list.Add(vector2 + new Vector3(num, 0f, 0f));
					}
				}
				foreach (Vector3 item in list)
				{
					box = new BoundingBox(item - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), item + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
					box.Min += new Vector3(0.02f, EffectiveRiseHeight + 0.02f, 0.02f);
					box.Max -= new Vector3(0.02f);
					m_collisionBoxes.Clear();
					FindTerrainCollisionBoxes(box, m_collisionBoxes);
					m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
					m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
					if (!IsColliding(box, m_collisionBoxes))
					{
						vector = item;
						break;
					}
				}
			}
			if (vector.HasValue)
			{
				Position = vector.Value;
				return true;
			}
		}
		return false;
	}

	public void MoveWithCollision(float dt, Vector3 move)
	{
		Vector3 position = base.Position;
		bool isSmoothRising = IsSmoothRiseEnabled && EffectiveRiseHeight > 0f && HandleSmoothRise(ref move, position, dt);
		HandleAxisCollision(1, move.Y, ref position, isSmoothRising);
		HandleAxisCollision(0, move.X, ref position, isSmoothRising);
		HandleAxisCollision(2, move.Z, ref position, isSmoothRising);
		Position = position;
	}

	public bool HandleSmoothRise(ref Vector3 move, Vector3 position, float dt)
	{
		Vector3 boxSize = BoxSize;
		Vector3 vector = position + move;
		BoundingBox boundingBox = new BoundingBox(vector - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), vector + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
		boundingBox = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
		boundingBox.Min += new Vector3(0.04f, 0f, 0.04f);
		boundingBox.Max -= new Vector3(0.04f, 0f, 0.04f);
		m_collisionBoxes.Clear();
		FindTerrainCollisionBoxes(boundingBox, m_collisionBoxes);
		m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
		float num = MathUtils.Max(CalculatePushBack(boundingBox, 1, m_collisionBoxes, out var pushingCollisionBox), 0f);
		if (!BlocksManager.Blocks[Terrain.ExtractContents(pushingCollisionBox.BlockValue)].NoSmoothRise && num > 0.04f)
		{
			float x = MathUtils.Min(4.5f * ((Size > 1f) ? (Size * 0.8f) : 1f) * dt, num);
			move.Y = MathUtils.Max(move.Y, x);
			m_velocity.Y = MathUtils.Max(m_velocity.Y, 0f);
			StandingOnValue = pushingCollisionBox.BlockValue;
			StandingOnBody = pushingCollisionBox.ComponentBody;
			m_stoppedTime = 0f;
			return true;
		}
		return false;
	}

	public void HandleAxisCollision(int axis, float move, ref Vector3 position, bool isSmoothRising)
	{
		Vector3 boxSize = BoxSize;
		m_collisionBoxes.Clear();
		bool flag = Locomotion != null && Locomotion.IsCreativeFlyEnabled;
		if (IsSneaking && axis != 1 && !flag && Player != null)
		{
			FindSneakCollisionBoxes(position, new Vector2(boxSize.X - 0.08f, boxSize.Z - 0.08f), m_collisionBoxes);
		}
		Vector3 vector;
		switch (axis)
		{
		default:
			position.Z += move;
			vector = new Vector3(0.04f, 0.04f, 0f);
			break;
		case 1:
			position.Y += move;
			vector = new Vector3(0.04f, 0f, 0.04f);
			break;
		case 0:
			position.X += move;
			vector = new Vector3(0f, 0.04f, 0.04f);
			break;
		}
		BoundingBox boundingBox = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f) + vector, position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f) - vector);
		FindTerrainCollisionBoxes(boundingBox, m_collisionBoxes);
		m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
		float num;
		CollisionBox pushingCollisionBox;
		if ((axis != 1) | isSmoothRising)
		{
			BoundingBox smoothRiseBox = boundingBox;
			smoothRiseBox.Min.Y = smoothRiseBox.Min.Y + EffectiveRiseHeight;
			num = CalculateSmoothRisePushBack(boundingBox, smoothRiseBox, axis, m_collisionBoxes, out pushingCollisionBox);
		}
		else
		{
			num = CalculatePushBack(boundingBox, axis, m_collisionBoxes, out pushingCollisionBox);
		}
		BoundingBox box = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f) + vector, position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f) - vector);
		float num2 = CalculatePushBack(box, axis, m_bodiesCollisionBoxes, out var pushingCollisionBox2);
		if (axis == 1 && move < 0f && pushingCollisionBox2.ComponentBody != null)
		{
			float num3 = MathUtils.Abs(m_velocity.Y);
			if (num3 > 8f)
			{
				ComponentBody componentBody = pushingCollisionBox2.ComponentBody;
				int entityId = componentBody.Entity.EntityId;
				if (m_lastBodyDamageTargetId != entityId || Time.FrameStartTime - m_lastBodyDamageTime > 0.15)
				{
					ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
					ComponentHealth componentHealth = componentBody.Entity.FindComponent<ComponentHealth>();
					ComponentCreature attacker = Entity.FindComponent<ComponentCreature>();
					if (componentHealth != null && componentCreature != null)
					{
						float amount = (num3 - 8f) * 0.05f * (Mass / 80f);
						componentHealth.Injure(amount, attacker, ignoreInvulnerability: false, "Crushed by falling object");
						m_lastBodyDamageTargetId = entityId;
						m_lastBodyDamageTime = Time.FrameStartTime;
					}
				}
			}
		}
		if (MathUtils.Abs(num) > MathUtils.Abs(num2))
		{
			if (num == 0f)
			{
				return;
			}
			int num4 = Terrain.ExtractContents(pushingCollisionBox.BlockValue);
			if (BlocksManager.Blocks[num4].HasCollisionBehavior)
			{
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num4);
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					Vector3 vector2 = (pushingCollisionBox.Box.Min + pushingCollisionBox.Box.Max) / 2f;
					CellFace cellFace = CellFace.FromAxisAndDirection(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Y), Terrain.ToCell(vector2.Z), axis, 0f - GetVectorComponent(m_velocity, axis));
					blockBehaviors[i].OnCollide(cellFace, GetVectorComponent(m_velocity, axis), this);
				}
			}
			switch (axis)
			{
			case 0:
				position.X += num;
				m_velocity.X = pushingCollisionBox.BlockVelocity.X;
				break;
			case 1:
				position.Y += num;
				m_velocity.Y = pushingCollisionBox.BlockVelocity.Y;
				if (move < 0f)
				{
					StandingOnValue = pushingCollisionBox.BlockValue;
					StandingOnBody = pushingCollisionBox.ComponentBody;
					StandingOnVelocity = pushingCollisionBox.BlockVelocity;
				}
				break;
			default:
				position.Z += num;
				m_velocity.Z = pushingCollisionBox.BlockVelocity.Z;
				break;
			}
		}
		else
		{
			if (num2 == 0f)
			{
				return;
			}
			ComponentBody componentBody2 = pushingCollisionBox2.ComponentBody;
			switch (axis)
			{
			case 0:
				InelasticCollision(m_velocity.X, componentBody2.m_velocity.X, Mass, componentBody2.Mass, 0.5f, out m_velocity.X, out componentBody2.m_velocity.X);
				position.X += num2;
				break;
			case 1:
			{
				InelasticCollision(m_velocity.Y, componentBody2.m_velocity.Y, Mass, componentBody2.Mass, 0.5f, out m_velocity.Y, out componentBody2.m_velocity.Y);
				position.Y += num2;
				if (!(move < 0f))
				{
					break;
				}
				StandingOnValue = pushingCollisionBox2.BlockValue;
				StandingOnBody = pushingCollisionBox2.ComponentBody;
				StandingOnVelocity = new Vector3(componentBody2.m_velocity.X, 0f, componentBody2.m_velocity.Z);
				if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && Player != null)
				{
					float num5 = ((Locomotion != null) ? Locomotion.JumpSpeed : 6f);
					float num6 = 1f;
					ComponentLevel componentLevel = Entity.FindComponent<ComponentLevel>();
					if (componentLevel != null)
					{
						num6 = 1f + 0.5f * MathUtils.Saturate(componentLevel.SpeedFactor - 1f);
					}
					float num7 = 1f;
					ComponentAdrenaline componentAdrenaline = ComponentAdrenaline;
					if (componentAdrenaline != null && componentAdrenaline.Adrenaline > 0f)
					{
						float adrenaline = componentAdrenaline.Adrenaline;
						float num8 = MathUtils.Min(adrenaline, 1.5f);
						float num9 = MathUtils.Max(adrenaline - 1.5f, 0f);
						num7 = 1f + 0.5f * num8 + 0.35f * (1f - 1f / (1f + num9));
					}
					float num10 = num5 * num7 * num6 * 0.6f;
					if (m_velocity.Y < num10)
					{
						m_velocity.Y = num10;
					}
				}
				if ((CommonLib.WorkType != WorkType.Server && CommonLib.WorkType != WorkType.Local) || m_subsystemGameInfo == null || m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace || Player == null)
				{
					return;
				}
				int entityId2 = componentBody2.Entity.EntityId;
				double frameStartTime = Time.FrameStartTime;
				if (m_lastBodyDamageTargetId == entityId2 && !(frameStartTime - m_lastBodyDamageTime > 0.15))
				{
					return;
				}
				Vector3 v = m_velocity - componentBody2.m_velocity;
				float num11 = ((Locomotion != null) ? Locomotion.JumpSpeed : 6f);
				float num12 = MathUtils.Abs(GetVectorComponent(v, 1));
				float num13 = 0.75f * num11;
				float num14 = 6f * num11;
				if (!(num12 > num13))
				{
					return;
				}
				float f = MathUtils.Saturate((num12 - num13) / (num14 - num13));
				ComponentLevel componentLevel2 = Entity.FindComponent<ComponentLevel>();
				ComponentAdrenaline componentAdrenaline2 = ComponentAdrenaline;
				float num15 = MathUtils.Lerp(0.02f, 0.12f, f);
				float num16 = 1f;
				if (componentLevel2 != null)
				{
					num16 = 1f + 0.5f * MathUtils.Saturate(componentLevel2.SpeedFactor - 1f);
				}
				float num17 = 1f;
				if (componentAdrenaline2 != null && componentAdrenaline2.Adrenaline > 0f)
				{
					float adrenaline2 = componentAdrenaline2.Adrenaline;
					num17 = 1f + 2f * (1f - 1f / (1f + adrenaline2));
				}
				float x = num15 * num16 * num17;
				x = MathUtils.Min(x, 0.95f);
				ComponentCreature componentCreature2 = Entity.FindComponent<ComponentCreature>();
				ComponentHealth componentHealth2 = componentBody2.Entity.FindComponent<ComponentHealth>();
				if (componentHealth2 != null && componentCreature2 != null)
				{
					float health = componentHealth2.Health;
					componentHealth2.Injure(x, componentCreature2, ignoreInvulnerability: false, "Embrace body hit");
					float num18 = health - componentHealth2.Health;
					if (num18 > 0f && Player != null)
					{
						float num19 = num18 * componentHealth2.AttackResilience;
						string text = (0f - num19).ToString("0", CultureInfo.InvariantCulture);
						Vector3 v2 = componentBody2.Position - Position;
						if (v2.LengthSquared() < 1E-06f)
						{
							v2 = Vector3.UnitY;
						}
						v2 = Vector3.Normalize(v2);
						Vector3 position2 = Position + 0.75f * v2;
						Vector3 velocity = v2 + m_velocity;
						Color white = Color.White;
						if (CommonLib.WorkType != WorkType.Client)
						{
							CommonLib.Net.QueuePackage(new ComponentHealthPackage(position2, velocity, white, text));
							HitValueParticleSystem particleSystem = new HitValueParticleSystem(position2, velocity, white, text);
							Entity.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
						}
					}
					if (componentHealth2.Health < health)
					{
						ComponentAdrenaline?.AddAdrenaline(0.05f);
					}
				}
				m_lastBodyDamageTargetId = entityId2;
				m_lastBodyDamageTime = frameStartTime;
				return;
			}
			default:
				InelasticCollision(m_velocity.Z, componentBody2.m_velocity.Z, Mass, componentBody2.Mass, 0.5f, out m_velocity.Z, out componentBody2.m_velocity.Z);
				position.Z += num2;
				break;
			}
			if ((CommonLib.WorkType != WorkType.Server && CommonLib.WorkType != WorkType.Local) || m_subsystemGameInfo == null || m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace || Player == null)
			{
				return;
			}
			int entityId3 = componentBody2.Entity.EntityId;
			double frameStartTime2 = Time.FrameStartTime;
			if (m_lastBodyDamageTargetId == entityId3 && !(frameStartTime2 - m_lastBodyDamageTime > 0.15))
			{
				return;
			}
			Vector3 v3 = m_velocity - componentBody2.m_velocity;
			float num20 = ((Locomotion != null) ? Locomotion.JumpSpeed : 6f);
			float num21 = MathUtils.Abs(GetVectorComponent(v3, axis));
			float num22 = 0.75f * num20;
			float num23 = 6f * num20;
			if (!(num21 > num22))
			{
				return;
			}
			float f2 = MathUtils.Saturate((num21 - num22) / (num23 - num22));
			ComponentLevel componentLevel3 = Entity.FindComponent<ComponentLevel>();
			ComponentAdrenaline componentAdrenaline3 = ComponentAdrenaline;
			float num24 = MathUtils.Lerp(0.02f, 0.12f, f2);
			float num25 = 1f;
			if (componentLevel3 != null)
			{
				num25 = 1f + 0.5f * MathUtils.Saturate(componentLevel3.SpeedFactor - 1f);
			}
			float num26 = 1f;
			if (componentAdrenaline3 != null && componentAdrenaline3.Adrenaline > 0f)
			{
				float adrenaline3 = componentAdrenaline3.Adrenaline;
				num26 = 1f + 2f * (1f - 1f / (1f + adrenaline3));
			}
			float x2 = num24 * num25 * num26;
			x2 = MathUtils.Min(x2, 0.95f);
			ComponentCreature componentCreature3 = Entity.FindComponent<ComponentCreature>();
			ComponentHealth componentHealth3 = componentBody2.Entity.FindComponent<ComponentHealth>();
			if (componentHealth3 != null && componentCreature3 != null)
			{
				float health2 = componentHealth3.Health;
				componentHealth3.Injure(x2, componentCreature3, ignoreInvulnerability: false, "IHELPU");
				float num27 = health2 - componentHealth3.Health;
				if (num27 > 0f && Player != null)
				{
					float num28 = num27 * componentHealth3.AttackResilience;
					string text2 = (0f - num28).ToString("0", CultureInfo.InvariantCulture);
					Vector3 v4 = componentBody2.Position - Position;
					if (v4.LengthSquared() < 1E-06f)
					{
						v4 = Vector3.UnitY;
					}
					v4 = Vector3.Normalize(v4);
					Vector3 position3 = Position + 0.75f * v4;
					Vector3 velocity2 = v4 + m_velocity;
					Color white2 = Color.White;
					if (CommonLib.WorkType != WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new ComponentHealthPackage(position3, velocity2, white2, text2));
						HitValueParticleSystem particleSystem2 = new HitValueParticleSystem(position3, velocity2, white2, text2);
						Entity.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem2);
					}
				}
				if (componentHealth3.Health < health2)
				{
					ComponentAdrenaline?.AddAdrenaline(0.05f);
					float num29 = health2 - componentHealth3.Health;
					if (num29 > 0f)
					{
						Entity.FindComponent<ComponentHealth>()?.Heal(num29);
					}
				}
			}
			m_lastBodyDamageTargetId = entityId3;
			m_lastBodyDamageTime = frameStartTime2;
		}
	}

	public void FindBodiesCollisionBoxes(Vector3 position, DynamicArray<CollisionBox> result)
	{
		m_componentBodies.Clear();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), 4f, m_componentBodies);
		for (int i = 0; i < m_componentBodies.Count; i++)
		{
			ComponentBody componentBody = m_componentBodies.Array[i];
			if (componentBody != this && componentBody != m_parentBody && componentBody.m_parentBody != this)
			{
				result.Add(new CollisionBox
				{
					Box = componentBody.BoundingBox,
					ComponentBody = componentBody
				});
			}
		}
	}

	public void FindMovingBlocksCollisionBoxes(Vector3 position, DynamicArray<CollisionBox> result)
	{
		Vector3 boxSize = BoxSize;
		BoundingBox boundingBox = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
		boundingBox.Min -= new Vector3(1f);
		boundingBox.Max += new Vector3(1f);
		m_movingBlockSets.Clear();
		m_subsystemMovingBlocks.FindMovingBlocks(boundingBox, extendToFillCells: false, m_movingBlockSets);
		for (int i = 0; i < m_movingBlockSets.Count; i++)
		{
			IMovingBlockSet movingBlockSet = m_movingBlockSets.Array[i];
			Quaternion q = Quaternion.Identity;
			Vector3 v = Vector3.Zero;
			if (movingBlockSet is SubsystemMovingBlocks.MovingBlockSet movingBlockSet2)
			{
				q = movingBlockSet2.Rotation;
				if (movingBlockSet2.Tag is ValuesDictionary valuesDictionary)
				{
					v = valuesDictionary.GetValue<Vector3>("AngularVelocity");
				}
			}
			Vector3 position2 = movingBlockSet.Position;
			Vector3 currentVelocity = movingBlockSet.CurrentVelocity;
			for (int j = 0; j < movingBlockSet.Blocks.Count; j++)
			{
				MovingBlock movingBlock = movingBlockSet.Blocks[j];
				int num = Terrain.ExtractContents(movingBlock.Value);
				Block block = BlocksManager.Blocks[num];
				if (!block.IsCollidable)
				{
					continue;
				}
				BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, movingBlock.Value);
				Vector3 vector = new Vector3(movingBlock.Offset);
				Vector3 v2 = Vector3.Transform(vector, q);
				Vector3 blockVelocity = currentVelocity + Vector3.Cross(v, v2);
				BoundingBox[] array = customCollisionBoxes;
				for (int k = 0; k < array.Length; k++)
				{
					BoundingBox boundingBox2 = array[k];
					Vector3 vector2 = new Vector3(float.MaxValue);
					Vector3 vector3 = new Vector3(float.MinValue);
					for (int l = 0; l < 8; l++)
					{
						Vector3 vector4 = new Vector3(((l & 1) == 0) ? boundingBox2.Min.X : boundingBox2.Max.X, ((l & 2) == 0) ? boundingBox2.Min.Y : boundingBox2.Max.Y, ((l & 4) == 0) ? boundingBox2.Min.Z : boundingBox2.Max.Z);
						Vector3 v3 = position2 + Vector3.Transform(vector + vector4, q);
						vector2 = Vector3.Min(vector2, v3);
						vector3 = Vector3.Max(vector3, v3);
					}
					result.Add(new CollisionBox
					{
						Box = new BoundingBox(vector2, vector3),
						BlockValue = movingBlock.Value,
						BlockVelocity = blockVelocity
					});
				}
			}
		}
	}

	public void FindTerrainCollisionBoxes(BoundingBox box, DynamicArray<CollisionBox> result)
	{
		Point3 x = Terrain.ToCell(box.Min);
		Point3 point = Terrain.ToCell(box.Max);
		x.Y = MathUtils.Max(x.Y, 0);
		point.Y = MathUtils.Min(point.Y, 255);
		if (x.Y > point.Y)
		{
			return;
		}
		for (int i = x.X; i <= point.X; i++)
		{
			for (int j = x.Z; j <= point.Z; j++)
			{
				TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(i, j);
				if (chunkAtCell == null)
				{
					continue;
				}
				int num = TerrainChunk.CalculateCellIndex(i & 0xF, x.Y, j & 0xF);
				int num2 = x.Y;
				while (num2 <= point.Y)
				{
					int cellValueFast = chunkAtCell.GetCellValueFast(num);
					int num3 = Terrain.ExtractContents(cellValueFast);
					if (num3 != 0)
					{
						Block block = BlocksManager.Blocks[num3];
						if (block.IsCollidable)
						{
							BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, cellValueFast);
							Vector3 vector = new Vector3(i, num2, j);
							for (int k = 0; k < customCollisionBoxes.Length; k++)
							{
								result.Add(new CollisionBox
								{
									Box = new BoundingBox(vector + customCollisionBoxes[k].Min, vector + customCollisionBoxes[k].Max),
									BlockValue = cellValueFast
								});
							}
						}
					}
					num2++;
					num++;
				}
			}
		}
	}

	public void FindSneakCollisionBoxes(Vector3 position, Vector2 overhang, DynamicArray<CollisionBox> result)
	{
		int num = Terrain.ToCell(position.X);
		int num2 = Terrain.ToCell(position.Y);
		int num3 = Terrain.ToCell(position.Z);
		if (BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3)].IsCollidable)
		{
			return;
		}
		bool flag = position.X < (float)num + 0.5f;
		bool flag2 = position.Z < (float)num3 + 0.5f;
		if (flag)
		{
			if (flag2)
			{
				bool isCollidable = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 - 1)].IsCollidable;
				bool isCollidable2 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3)].IsCollidable;
				bool isCollidable3 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3 - 1)].IsCollidable;
				if ((isCollidable && !isCollidable2) || ((!isCollidable && !isCollidable2) & isCollidable3))
				{
					CollisionBox item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, (float)num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item);
				}
				if ((!isCollidable & isCollidable2) || ((!isCollidable && !isCollidable2) & isCollidable3))
				{
					CollisionBox item2 = new CollisionBox
					{
						Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item2);
				}
				if (isCollidable & isCollidable2)
				{
					CollisionBox item3 = new CollisionBox
					{
						Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, (float)num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item3);
				}
			}
			else
			{
				bool isCollidable4 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 + 1)].IsCollidable;
				bool isCollidable5 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3)].IsCollidable;
				bool isCollidable6 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3 + 1)].IsCollidable;
				if ((isCollidable4 && !isCollidable5) || ((!isCollidable4 && !isCollidable5) & isCollidable6))
				{
					CollisionBox item4 = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, (float)(num3 + 1) - overhang.Y)),
						BlockValue = 0
					};
					result.Add(item4);
				}
				if ((!isCollidable4 & isCollidable5) || ((!isCollidable4 && !isCollidable5) & isCollidable6))
				{
					CollisionBox item5 = new CollisionBox
					{
						Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item5);
				}
				if (isCollidable4 & isCollidable5)
				{
					CollisionBox item6 = new CollisionBox
					{
						Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, (float)(num3 + 1) - overhang.Y)),
						BlockValue = 0
					};
					result.Add(item6);
				}
			}
		}
		else if (flag2)
		{
			bool isCollidable7 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 - 1)].IsCollidable;
			bool isCollidable8 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3)].IsCollidable;
			bool isCollidable9 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3 - 1)].IsCollidable;
			if ((isCollidable7 && !isCollidable8) || ((!isCollidable7 && !isCollidable8) & isCollidable9))
			{
				CollisionBox item7 = new CollisionBox
				{
					Box = new BoundingBox(new Vector3(num, num2, (float)num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
					BlockValue = 0
				};
				result.Add(item7);
			}
			if ((!isCollidable7 & isCollidable8) || ((!isCollidable7 && !isCollidable8) & isCollidable9))
			{
				CollisionBox item8 = new CollisionBox
				{
					Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3((float)(num + 1) - overhang.X, num2 + 1, num3 + 1)),
					BlockValue = 0
				};
				result.Add(item8);
			}
			if (isCollidable7 & isCollidable8)
			{
				CollisionBox item9 = new CollisionBox
				{
					Box = new BoundingBox(new Vector3(num, num2, (float)num3 + overhang.Y), new Vector3((float)(num + 1) - overhang.X, num2 + 1, num3 + 1)),
					BlockValue = 0
				};
				result.Add(item9);
			}
		}
		else
		{
			bool isCollidable10 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 + 1)].IsCollidable;
			bool isCollidable11 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3)].IsCollidable;
			bool isCollidable12 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3 + 1)].IsCollidable;
			if ((isCollidable10 && !isCollidable11) || ((!isCollidable10 && !isCollidable11) & isCollidable12))
			{
				CollisionBox item10 = new CollisionBox
				{
					Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, (float)(num3 + 1) - overhang.Y)),
					BlockValue = 0
				};
				result.Add(item10);
			}
			if ((!isCollidable10 & isCollidable11) || ((!isCollidable10 && !isCollidable11) & isCollidable12))
			{
				CollisionBox item11 = new CollisionBox
				{
					Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3((float)(num + 1) - overhang.X, num2 + 1, num3 + 1)),
					BlockValue = 0
				};
				result.Add(item11);
			}
			if (isCollidable10 & isCollidable11)
			{
				CollisionBox item12 = new CollisionBox
				{
					Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3((float)(num + 1) - overhang.X, num2 + 1, (float)(num3 + 1) - overhang.Y)),
					BlockValue = 0
				};
				result.Add(item12);
			}
		}
	}

	public bool IsColliding(BoundingBox box, DynamicArray<CollisionBox> collisionBoxes)
	{
		for (int i = 0; i < collisionBoxes.Count; i++)
		{
			if (box.Intersection(collisionBoxes.Array[i].Box))
			{
				return true;
			}
		}
		return false;
	}

	public float CalculatePushBack(BoundingBox box, int axis, DynamicArray<CollisionBox> collisionBoxes, out CollisionBox pushingCollisionBox)
	{
		pushingCollisionBox = default;
		float num = 0f;
		for (int i = 0; i < collisionBoxes.Count; i++)
		{
			float num2 = CalculateBoxBoxOverlap(ref box, ref collisionBoxes.Array[i].Box, axis);
			if (MathUtils.Abs(num2) > MathUtils.Abs(num))
			{
				num = num2;
				pushingCollisionBox = collisionBoxes.Array[i];
			}
		}
		return num;
	}

	public float CalculateSmoothRisePushBack(BoundingBox normalBox, BoundingBox smoothRiseBox, int axis, DynamicArray<CollisionBox> collisionBoxes, out CollisionBox pushingCollisionBox)
	{
		pushingCollisionBox = default;
		float num = 0f;
		for (int i = 0; i < collisionBoxes.Count; i++)
		{
			float num2 = ((!BlocksManager.Blocks[Terrain.ExtractContents(collisionBoxes.Array[i].BlockValue)].NoSmoothRise) ? CalculateBoxBoxOverlap(ref smoothRiseBox, ref collisionBoxes.Array[i].Box, axis) : CalculateBoxBoxOverlap(ref normalBox, ref collisionBoxes.Array[i].Box, axis));
			if (MathUtils.Abs(num2) > MathUtils.Abs(num))
			{
				num = num2;
				pushingCollisionBox = collisionBoxes.Array[i];
			}
		}
		return num;
	}

	public static float CalculateBoxBoxOverlap(ref BoundingBox b1, ref BoundingBox b2, int axis)
	{
		if (b1.Max.X <= b2.Min.X || b1.Min.X >= b2.Max.X || b1.Max.Y <= b2.Min.Y || b1.Min.Y >= b2.Max.Y || b1.Max.Z <= b2.Min.Z || b1.Min.Z >= b2.Max.Z)
		{
			return 0f;
		}
		switch (axis)
		{
		case 0:
		{
			float num13 = b1.Min.X + b1.Max.X;
			float num14 = b2.Min.X + b2.Max.X;
			float num15 = b1.Max.X - b1.Min.X;
			float num16 = b2.Max.X - b2.Min.X;
			float num17 = num14 - num13;
			float num18 = num15 + num16;
			return 0.5f * ((num17 > 0f) ? (num17 - num18) : (num17 + num18));
		}
		default:
		{
			float num7 = b1.Min.Z + b1.Max.Z;
			float num8 = b2.Min.Z + b2.Max.Z;
			float num9 = b1.Max.Z - b1.Min.Z;
			float num10 = b2.Max.Z - b2.Min.Z;
			float num11 = num8 - num7;
			float num12 = num9 + num10;
			return 0.5f * ((num11 > 0f) ? (num11 - num12) : (num11 + num12));
		}
		case 1:
		{
			float num = b1.Min.Y + b1.Max.Y;
			float num2 = b2.Min.Y + b2.Max.Y;
			float num3 = b1.Max.Y - b1.Min.Y;
			float num4 = b2.Max.Y - b2.Min.Y;
			float num5 = num2 - num;
			float num6 = num3 + num4;
			return 0.5f * ((num5 > 0f) ? (num5 - num6) : (num5 + num6));
		}
		}
	}

	public static float GetVectorComponent(Vector3 v, int axis)
	{
		return axis switch
		{
			0 => v.X, 
			1 => v.Y, 
			_ => v.Z, 
		};
	}

	public static void InelasticCollision(float v1, float v2, float m1, float m2, float cr, out float result1, out float result2)
	{
		float num = 1f / (m1 + m2);
		result1 = (cr * m2 * (v2 - v1) + m1 * v1 + m2 * v2) * num;
		result2 = (cr * m1 * (v1 - v2) + m1 * v1 + m2 * v2) * num;
	}

	public ComponentBody()
	{
		size = 1f;
		m_currentGravityAcceleration = 10f;
		m_random = new Random();
		m_collisionBoxes = new DynamicArray<CollisionBox>();
		m_componentBodies = new DynamicArray<ComponentBody>();
		m_movingBlockSets = new DynamicArray<IMovingBlockSet>();
		m_bodiesCollisionBoxes = new DynamicArray<CollisionBox>();
		m_movingBlocksCollisionBoxes = new DynamicArray<CollisionBox>();
		m_childBodies = new List<ComponentBody>();
		m_embraceUpVector = Vector3.UnitY;
		m_embraceStuck = false;
		m_embraceStickDistance = 0.3f;
		m_embraceStickBlockValue = 0;
		m_embraceStickFace = 4;
	}

	public void StartSneak()
	{
		IsSneaking = true;
		UpdateCrouchState(isSneaking: true);
	}

	public void StopSneak()
	{
		if (!IsBlockAbove() || IsSafeDrop())
		{
			m_isSneaking = false;
			m_isSneaking = false;
			TargetCrouchFactor = 0f;
			SendSneakState();
			UpdateCrouchState(isSneaking: false);
			UpdateBoxSize(isSneaking: false);
			m_sneakCooldownTimer = 0.7f;
		}
	}

	public void UpdateCrouchState(bool isSneaking)
	{
		if (isSneaking)
		{
			HumanModel?.StartCrouch();
		}
		else
		{
			HumanModel?.StopCrouch();
		}
		BoxSize = (isSneaking ? SneakBoxSize : defaultBoxSize);
	}

	public void SendSneakState()
	{
		if (Player != null && (CommonLib.WorkType != WorkType.Client || Player.PlayerData.IsMainPlayer))
		{
			ComponentPlayerPackage componentPlayerPackage = new ComponentPlayerPackage(Player, ComponentPlayerPackage.PlayerAction.BodyUpdate);
			componentPlayerPackage.changFlag |= ComponentPlayerPackage.ChangFlag.SneakChange;
			componentPlayerPackage.m_sneaking = m_isSneaking;
			CommonLib.Net.QueuePackage(componentPlayerPackage);
		}
	}

	public void UpdateBoxSize(bool isSneaking)
	{
		if (isSneaking)
		{
			Vector3 boxSize = SneakBoxSize * Size;
			if (Size < 1f)
			{
				boxSize.X = MathUtils.Max(boxSize.X, 0.3f);
				boxSize.Z = MathUtils.Max(boxSize.Z, 0.3f);
			}
			BoxSize = boxSize;
		}
		else
		{
			Vector3 boxSize2 = defaultBoxSize * Size;
			if (Size < 1f)
			{
				boxSize2.X = MathUtils.Max(boxSize2.X, 0.3f);
				boxSize2.Z = MathUtils.Max(boxSize2.Z, 0.3f);
			}
			BoxSize = boxSize2;
		}
	}

	public bool IsBlockAbove()
	{
		Point3 parentWidget = CellFace.FaceToPoint3(CellFace.Vector3ToFace((m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && HumanModel != null) ? m_embraceUpVector : Vector3.UnitY));
		int gameWidget = Terrain.ToCell(Position.X) + parentWidget.X;
		int y = Terrain.ToCell(Position.Y) + parentWidget.Y;
		int z = Terrain.ToCell(Position.Z) + parentWidget.Z;
		int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(gameWidget, y, z));
		if (num != 0)
		{
			return BlocksManager.Blocks[num].IsCollidable;
		}
		return false;
	}

	private float GetWindEffectCoefficient(float mass, float windStrength)
	{
		if (mass <= 5f)
		{
			if (windStrength >= 4f)
			{
				return (windStrength - 3f) * 2.1f;
			}
		}
		else if (mass <= 70f)
		{
			if (windStrength >= 10f)
			{
				return (windStrength - 9f) * 1.55f;
			}
		}
		else if (mass <= 450f)
		{
			if (windStrength >= 10f)
			{
				return (windStrength - 9f) * 1.12f;
			}
		}
		else if (mass <= 3000f && windStrength >= 14f)
		{
			return (windStrength - 13f) * 0.5f;
		}
		return 0f;
	}

	private float GetAdjustedWindResistance()
	{
		float slotValue = MathUtils.Saturate(WindResistance);
		ComponentShipModel center = Entity?.FindComponent<ComponentShipModel>();
		if (center != null)
		{
			if (center.ShowSailFull)
			{
				slotValue *= 1f / 3f;
			}
			else if (center.ShowSailHalf)
			{
				slotValue *= 0.5f;
			}
		}
		return MathUtils.Saturate(slotValue);
	}

	private bool IsEmbraceModeActive()
	{
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return HumanModel != null;
		}
		return false;
	}

	private float GetBedrockTopY(int cx, int cz)
	{
		return 2 + (int)(2f * SimplexNoise.OctavedNoise(cx, cz, 0.1f, 1, 1f, 1f));
	}

	private bool IsBelowBedrockOcean(Vector3 pos)
	{
		int cx = Terrain.ToCell(pos.X);
		int cz = Terrain.ToCell(pos.Z);
		float bedrockTopY = GetBedrockTopY(cx, cz);
		return pos.Y <= bedrockTopY + 0.001f;
	}

	public bool IsSafeDrop()
	{
		if (m_subsystemTerrain == null)
		{
			return false;
		}
		int x = Terrain.ToCell(Position.X);
		int num = Terrain.ToCell(Position.Y);
		int z = Terrain.ToCell(Position.Z);
		int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num - 1, z));
		if (num2 == 0 || !BlocksManager.Blocks[num2].IsCollidable)
		{
			int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num - 2, z));
			if (num3 != 0 && BlocksManager.Blocks[num3].IsCollidable)
			{
				return true;
			}
		}
		return false;
	}
}
