using System;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentHumanModel : ComponentCreatureModel
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemModelsRenderer m_subsystemModelsRenderer;

	public SubsystemNoise m_subsystemNoise;

	public SubsystemAudio m_subsystemAudio;

	public ComponentMiner m_componentMiner;

	public ComponentRider m_componentRider;

	public ComponentSleep m_componentSleep;

	public ComponentPlayer m_componentPlayer;

	public DrawBlockEnvironmentData m_drawBlockEnvironmentData;

	public ModelBone m_bodyBone;

	public ModelBone m_headBone;

	public ModelBone m_leg1Bone;

	public ModelBone m_leg2Bone;

	public ModelBone m_hand1Bone;

	public ModelBone m_hand2Bone;

	public float m_sneakFactor;

	public float m_lieDownFactorEye;

	public float m_lieDownFactorModel;

	public float m_walkAnimationSpeed;

	public float m_walkLegsAngle;

	public float m_walkBobHeight;

	public float m_headingOffset;

	public float m_punchFactor;

	public float m_punchPhase;

	public int m_punchCounter;

	public float m_footstepsPhase;

	public bool m_rowLeft;

	public bool m_rowRight;

	public float m_aimHandAngle;

	public Vector3 m_inHandItemOffset;

	public Vector3 m_inHandItemRotation;

	public Vector2 m_headAngles;

	public Vector2 m_handAngles1;

	public Vector2 m_handAngles2;

	public Vector2 m_legAngles1;

	public Vector2 m_legAngles2;

	public bool hasData;

	private bool isCrouching;

	private bool m_isSleeping;

	private bool m_isDead;

	private bool m_sleepPositionSelected;

	private int m_sleepBodyPosition;

	private Vector2 m_sleepHeadRotation;

	private Vector2 m_sleepHand1Rotation;

	private Vector2 m_sleepHand2Rotation;

	private Vector2 m_sleepLeg1Rotation;

	private Vector2 m_sleepLeg2Rotation;

	private bool m_randomDeathRotationsGenerated;

	private Vector3 m_deathBodyRotation;

	private Vector2 m_deathHeadRotation;

	private Vector2 m_deathHand1Rotation;

	private Vector2 m_deathHand2Rotation;

	private Vector2 m_deathLeg1Rotation;

	private Vector2 m_deathLeg2Rotation;

	private new Random m_random;

	private bool m_deathPositionSelected;

	private bool m_deathRotationsGenerated;

	private float shakeTimeAccumulator;

	private float currentSneakBoxSize;

	private float targetSneakBoxSize;

	private ComponentMount m_lastMount;

	private Vector2 m_deathBodyTilt;

	private bool m_isSwimming;

	private float m_swimBodyPitch;

	private float m_swimPhase;

	private float m_swimBlend;

	public bool ForceLeftRodWhileFishing;

	public bool FishingLineUseLeftHand;

	public int OffhandRodValue;

	public int m_headAccessoryValue;

	public static readonly int[] EmbraceHeadItems = new int[14]
	{
		Terrain.MakeBlockValue(24, 0, 0),
		Terrain.MakeBlockValue(25, 0, 0),
		Terrain.MakeBlockValue(20, 0, 0),
		Terrain.MakeBlockValue(28, 0, 0),
		Terrain.MakeBlockValue(99, 0, 0),
		Terrain.MakeBlockValue(226, 0, 0),
		CottonBlock.SetIsWild(Terrain.MakeBlockValue(204, 0, CottonBlock.SetSize(0, 2)), isWild: false),
		CottonBlock.SetIsWild(Terrain.MakeBlockValue(204, 0, CottonBlock.SetSize(0, 1)), isWild: false),
		CottonBlock.SetIsWild(Terrain.MakeBlockValue(204, 0, CottonBlock.SetSize(0, 2)), isWild: true),
		Terrain.MakeBlockValue(119, 0, 0),
		Terrain.MakeBlockValue(119, 0, 1),
		Terrain.MakeBlockValue(119, 0, 2),
		Terrain.MakeBlockValue(119, 0, 3),
		Terrain.MakeBlockValue(119, 0, 4)
	};

	private float m_crawlBlend;

	private Vector2 m_resolvedHeadAngles;

	private Vector2 m_resolvedHandAngles1;

	private Vector2 m_resolvedHandAngles2;

	private Vector2 m_resolvedLegAngles1;

	private Vector2 m_resolvedLegAngles2;

	private float m_stickyHead;

	private float m_stickyHand1;

	private float m_stickyHand2;

	private float m_stickyLeg1;

	private float m_stickyLeg2;

	private bool m_poseInit;

	private float m_smoothedLight;

	private float m_rawLight;

	private float m_lightStableTime;

	private LightSmoother m_lightSmoother = new LightSmoother();

	private float m_idleInWaterTime;

	private float m_floatBlend;

	private float m_floatBodyPitch;

	public override void Update(float dt)
	{
		float size = m_componentCreature.ComponentBody.Size;
		if (size > 1f)
		{
			float num = (float)Math.Pow(size, 1.45);
			dt /= num;
		}
		if (m_componentCreature.ComponentBody.IsSneaking)
		{
			targetSneakBoxSize = m_componentCreature.ComponentBody.SneakBoxSize.Y;
		}
		else
		{
			targetSneakBoxSize = m_componentCreature.ComponentBody.BoxSize.Y;
		}
		currentSneakBoxSize = MathUtils.Lerp(currentSneakBoxSize, targetSneakBoxSize, 5f * dt);
		float num2 = m_componentCreature.Entity.FindComponent<ComponentVitalStats>()?.Temperature ?? 5f;
		if (num2 > 0.1f && num2 < 5.5f)
		{
			float num3 = MathUtils.Lerp(0f, 0.02f, (5.5f - num2) / 5.5f);
			shakeTimeAccumulator += dt * 10f;
			float num4 = MathUtils.Sin(shakeTimeAccumulator + (float)m_random.Int(0, 100)) * num3;
			float num5 = MathUtils.Sin(shakeTimeAccumulator + (float)m_random.Int(100, 200)) * num3;
			m_headAngles += new Vector2(num4, num5);
			m_handAngles1 += new Vector2(num4 * 0.5f, num5 * 0.5f);
			m_handAngles2 += new Vector2(num4 * 0.5f, num5 * 0.5f);
			m_legAngles1 += new Vector2(num4 * 0.5f, num5 * 0.5f);
			m_legAngles2 += new Vector2(num4 * 0.5f, num5 * 0.5f);
		}
		if (num2 < 2.5f)
		{
			m_handAngles1 = Vector2.Lerp(m_handAngles1, new Vector2(MathUtils.DegToRad(160f), MathUtils.DegToRad(-240f)), 5f * dt);
			m_handAngles2 = Vector2.Lerp(m_handAngles2, new Vector2(MathUtils.DegToRad(160f), MathUtils.DegToRad(240f)), 5f * dt);
		}
		if (m_componentCreature.ComponentLocomotion.LadderValue.HasValue)
		{
			ActivateLadderClimbingAnimation(dt);
			return;
		}
		ComponentOnFire componentOnFire = m_componentCreature.Entity.FindComponent<ComponentOnFire>();
		if (componentOnFire != null && componentOnFire.IsOnFire)
		{
			shakeTimeAccumulator += dt * 10f;
			float num6 = 0.15f;
			float x = MathUtils.Sin(shakeTimeAccumulator + (float)m_random.Int(0, 100)) * num6;
			float y = MathUtils.Cos(shakeTimeAccumulator + (float)m_random.Int(100, 200)) * num6;
			float x2 = MathUtils.Sin(shakeTimeAccumulator + (float)m_random.Int(0, 50)) * num6;
			float y2 = MathUtils.Cos(shakeTimeAccumulator + (float)m_random.Int(50, 100)) * num6;
			float x3 = MathUtils.Sin(shakeTimeAccumulator + (float)m_random.Int(100, 150)) * num6;
			float y3 = MathUtils.Cos(shakeTimeAccumulator + (float)m_random.Int(150, 200)) * num6;
			m_headAngles += new Vector2(x, y);
			m_handAngles1 += new Vector2(x2, y2);
			m_handAngles2 += new Vector2(x3, y3);
		}
		else
		{
			shakeTimeAccumulator = 0f;
		}
		m_sneakFactor = (m_componentCreature.ComponentBody.IsSneaking ? MathUtils.Min(m_sneakFactor + 2f * dt, 0.3f) : MathUtils.Max(m_sneakFactor - 2f * dt, 0f));
		if ((m_componentSleep != null && m_componentSleep.IsSleeping) || m_componentCreature.ComponentHealth.Health <= 0f)
		{
			m_lieDownFactorEye = MathUtils.Min(m_lieDownFactorEye + 1f * dt, 1f);
			m_lieDownFactorModel = MathUtils.Min(m_lieDownFactorModel + 3f * dt, 1f);
		}
		else
		{
			m_lieDownFactorEye = MathUtils.Max(m_lieDownFactorEye - 1f * dt, 0f);
			m_lieDownFactorModel = MathUtils.Max(m_lieDownFactorModel - 3f * dt, 0f);
		}
		m_isSleeping = m_componentSleep != null && m_componentSleep.IsSleeping;
		m_isDead = m_componentCreature.ComponentHealth.Health <= 0f;
		if (!m_isDead && !m_isSleeping)
		{
			m_sleepPositionSelected = false;
			m_randomDeathRotationsGenerated = false;
			m_deathPositionSelected = false;
			m_deathRotationsGenerated = false;
		}
		bool flag = m_componentCreature.ComponentBody.Velocity.Y < -5f && !IsStandingOnBlock();
		if ((m_componentCreature.ComponentBody.ImmersionFactor > 0.85f) | flag)
		{
			if ((m_componentCreature.ComponentBody.Velocity.LengthSquared() > 0.5f) | flag)
			{
				m_isSwimming = true;
			}
			else
			{
				m_isSwimming = false;
			}
		}
		else
		{
			m_isSwimming = false;
		}
		if (m_isSwimming)
		{
			m_swimBlend = MathUtils.Min(m_swimBlend + dt * 1.5f, 1f);
		}
		else
		{
			m_swimBlend = MathUtils.Max(m_swimBlend - dt * 1.5f, 0f);
		}
		bool flag2 = m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue && m_componentCreature.ComponentLocomotion.LastWalkOrder.Value.LengthSquared() > 0.01f;
		if (m_componentCreature.ComponentBody.ImmersionFactor > 0.7f && !IsStandingOnBlock() && !flag2)
		{
			m_idleInWaterTime += dt;
		}
		else
		{
			m_idleInWaterTime = 0f;
		}
		if (m_idleInWaterTime > 3f)
		{
			m_floatBlend = MathUtils.Min(m_floatBlend + dt * 0.5f, 1f);
		}
		else
		{
			m_floatBlend = MathUtils.Max(m_floatBlend - dt * 2f, 0f);
		}
		bool flag3 = true;
		bool flag4 = true;
		float footstepsPhase = m_footstepsPhase;
		if (m_componentCreature.ComponentLocomotion.LadderValue.HasValue)
		{
			m_footstepsPhase += 1.5f * m_walkAnimationSpeed * m_componentCreature.ComponentBody.Velocity.Length() * dt;
			flag4 = false;
		}
		else if (!m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled)
		{
			float num7 = m_componentCreature.ComponentLocomotion.SlipSpeed ?? (m_componentCreature.ComponentBody.Velocity.XZ - m_componentCreature.ComponentBody.StandingOnVelocity.XZ).Length();
			if (num7 > 0.5f && !m_isSwimming)
			{
				MovementAnimationPhase += num7 * dt * m_walkAnimationSpeed;
				m_footstepsPhase += 1f * m_walkAnimationSpeed * num7 * dt;
				flag3 = false;
				flag4 = false;
			}
		}
		if (flag3)
		{
			float num8 = 0.5f * MathUtils.Floor(2f * MovementAnimationPhase);
			if (MovementAnimationPhase != num8)
			{
				MovementAnimationPhase = ((MovementAnimationPhase - num8 > 0.25f) ? MathUtils.Min(MovementAnimationPhase + 2f * dt, num8 + 0.5f) : MathUtils.Max(MovementAnimationPhase - 2f * dt, num8));
			}
		}
		if (flag4)
		{
			m_footstepsPhase = 0f;
		}
		float num9 = 0f;
		ComponentMount componentMount = ((m_componentRider != null) ? m_componentRider.Mount : null);
		if (componentMount != null)
		{
			ComponentCreatureModel componentCreatureModel = componentMount.Entity.FindComponent<ComponentCreatureModel>();
			if (componentCreatureModel != null)
			{
				Bob = componentCreatureModel.Bob;
				num9 = Bob;
			}
			m_headingOffset = 0f;
		}
		else
		{
			float x4 = MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
			num9 = m_walkBobHeight * MathUtils.Sqr(x4);
			float num10 = 0f;
			if (m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue && m_componentCreature.ComponentLocomotion.LastWalkOrder != Vector2.Zero)
			{
				num10 = Vector2.Angle(Vector2.UnitY, m_componentCreature.ComponentLocomotion.LastWalkOrder.Value);
			}
			m_headingOffset += MathUtils.NormalizeAngle(num10 - m_headingOffset) * MathUtils.Saturate(8f * m_subsystemTime.GameTimeDelta);
			m_headingOffset = MathUtils.NormalizeAngle(m_headingOffset);
		}
		float num11 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
		Bob += num11 * (num9 - Bob);
		IsAttackHitMoment = false;
		if (AttackOrder)
		{
			m_punchFactor = MathUtils.Min(m_punchFactor + 4f * dt, 1f);
			float punchPhase = m_punchPhase;
			m_punchPhase = MathUtils.Remainder(m_punchPhase + dt * 2f, 1f);
			float y4 = m_componentCreature.ComponentLocomotion.LookAngles.Y;
			float y5 = MathUtils.Lerp(-2.4f, 2.4f, (y4 + 1.5708f) / 3.1416f);
			int num12 = ((m_componentMiner != null) ? m_componentMiner.ActiveBlockValue : 0);
			int num13 = 0;
			if (m_componentMiner != null && m_componentMiner.Inventory != null)
			{
				int slotIndex = ((m_componentMiner.Inventory.ActiveSlotIndex == 0) ? 1 : 0);
				num13 = m_componentMiner.Inventory.GetSlotValue(slotIndex);
			}
			SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			bool num14 = subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
			bool flag5 = true;
			bool flag6 = true;
			if (num14)
			{
				if (num12 == 0 && num13 != 0)
				{
					flag5 = false;
				}
				if (num13 == 0 && num12 != 0)
				{
					flag6 = false;
				}
			}
			if (flag6)
			{
				m_handAngles1.Y = y5;
			}
			if (flag5)
			{
				m_handAngles2.Y = y5;
			}
			if (punchPhase < 0.5f && m_punchPhase >= 0.5f)
			{
				IsAttackHitMoment = true;
				m_punchCounter++;
			}
		}
		else
		{
			m_punchFactor = MathUtils.Max(m_punchFactor - 4f * dt, 0f);
			if (m_punchPhase != 0f)
			{
				if (m_punchPhase > 0.5f)
				{
					m_punchPhase = MathUtils.Remainder(MathUtils.Min(m_punchPhase + dt * 2f, 1f), 1f);
				}
				else if (m_punchPhase > 0f)
				{
					m_punchPhase = MathUtils.Max(m_punchPhase - dt * m_punchPhase, 0f);
				}
			}
		}
		if (!hasData)
		{
			m_rowLeft = RowLeftOrder;
			m_rowRight = RowRightOrder;
		}
		else if (Time.PeriodicEvent(1.0, 0.9))
		{
			hasData = false;
		}
		if ((m_rowLeft || m_rowRight) && componentMount != null && componentMount.ComponentBody.ImmersionFactor > 0f && MathUtils.Floor(1.100000023841858 * m_subsystemTime.GameTime) != MathUtils.Floor(1.100000023841858 * (m_subsystemTime.GameTime - (double)m_subsystemTime.GameTimeDelta)))
		{
			CommonLib.Net.QueuePackage(new ComponentBehaviorPackage(this));
			m_subsystemAudio.PlayRandomSound("Audio/Rowing", m_random.Float(0.4f, 0.6f), m_random.Float(-0.3f, 0.2f), m_componentCreature.ComponentBody.Position, 3f, autoDelay: true);
		}
		float num15 = MathUtils.Floor(m_footstepsPhase);
		if (m_footstepsPhase > num15 && footstepsPhase <= num15)
		{
			if (!m_componentCreature.ComponentBody.IsSneaking)
			{
				m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 8f);
			}
			if (!m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f))
			{
				m_footstepsPhase = 0f;
			}
		}
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Terrain.ToCell(position.X);
		Terrain.ToCell(position.Z);
		Terrain.ToCell(position.Y + 0.1f);
		Terrain.ToCell(position.Y + 1.5f);
		m_lightSmoother.Update(dt, position, m_subsystemTerrain, Project);
		m_smoothedLight = m_lightSmoother.SmoothedLight;
		m_aimHandAngle = AimHandAngleOrder;
		m_inHandItemOffset = Vector3.Lerp(m_inHandItemOffset, InHandItemOffsetOrder, 10f * dt);
		m_inHandItemRotation = Vector3.Lerp(m_inHandItemRotation, InHandItemRotationOrder, 10f * dt);
		RowLeftOrder = false;
		RowRightOrder = false;
		LastAttackOrder = AttackOrder;
		AttackOrder = false;
		AimHandAngleOrder = 0f;
		InHandItemOffsetOrder = Vector3.Zero;
		InHandItemRotationOrder = Vector3.Zero;
		base.Update(dt);
	}

	public override void Animate()
	{
		base.Animate();
		Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		Vector3 vector = m_componentCreature.ComponentBody.Position;
		Vector3 vector2 = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
		bool flag = false;
		if (m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer && SettingsManager.FirstPersonCameraEnabled)
		{
			flag = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true).GameWidgets.Any((GameWidget w) => w.ActiveCamera != null && w.ActiveCamera.GameWidget.IsEntityFirstPersonTarget(Entity));
		}
		Vector3 scale = Vector3.One;
		if (m_componentPlayer != null)
		{
			switch (m_componentPlayer.PlayerData.Tag)
			{
			case PlayerData.PlayerTag.Short:
				scale = new Vector3(0.85f, 0.85f, 0.85f);
				break;
			case PlayerData.PlayerTag.Fat:
				scale = new Vector3(1.5f, 1f, 1.5f);
				break;
			case PlayerData.PlayerTag.Tall:
				scale = new Vector3(1f, 1f, 1f);
				break;
			case PlayerData.PlayerTag.Dwarf:
				scale = new Vector3(1.25f, 0.7f, 1.25f);
				break;
			}
		}
		scale *= m_componentCreature.ComponentBody.Size;
		if (m_lieDownFactorModel == 0f)
		{
			float x = ((isCrouching && IsStandingOnBlock() && m_componentCreature.ComponentBody.Velocity.XZ.LengthSquared() > 0.05f) ? 1f : 0f);
			m_crawlBlend = MathUtils.Lerp(m_crawlBlend, x, 10f * m_subsystemTime.GameTimeDelta);
			ComponentMount lastMount = ((m_componentRider != null) ? m_componentRider.Mount : null);
			m_lastMount = lastMount;
			ComponentMount lastMount2 = m_lastMount;
			float num = MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
			vector.Y += Bob;
			vector2.X += m_headingOffset;
			float num2 = (float)MathUtils.Remainder(0.75 * m_subsystemGameInfo.TotalElapsedGameTime + (double)(GetHashCode() & 0xFFFF), 10000.0);
			float x2 = MathUtils.Clamp(MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(1.02f * num2 - 100f)) + m_componentCreature.ComponentLocomotion.LookAngles.X + 1f * m_componentCreature.ComponentLocomotion.LastTurnOrder.X + m_headingOffset, 0f - MathUtils.DegToRad(80f), MathUtils.DegToRad(80f));
			float num3 = MathUtils.Clamp(MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(0.96f * num2 - 200f)) + m_componentCreature.ComponentLocomotion.LookAngles.Y, 0f - MathUtils.DegToRad(45f), MathUtils.DegToRad(45f));
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = 0f;
			float num11 = 0f;
			float num12 = 0f;
			if (lastMount2 != null)
			{
				switch (lastMount2.Entity.ValuesDictionary.DatabaseObject.Name)
				{
				case "Boat":
				{
					float num14 = 0.2f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Short:
							num14 = 0.1f;
							break;
						case PlayerData.PlayerTag.Dwarf:
							num14 = -0.1f;
							break;
						}
					}
					Vector3 v = new Vector3(0f, 0f - num14, 0f);
					vector = lastMount2.ComponentBody.Position + Vector3.Transform(v, Matrix.CreateFromQuaternion(lastMount2.ComponentBody.Rotation));
					vector2 = new Vector3(vector2.X, 0f, vector2.Z);
					num8 += 0.4f;
					num10 += 0.4f;
					num9 += 0.2f;
					num11 += -0.2f;
					num4 += 1.1f;
					num6 += 1.1f;
					num5 += 0.2f;
					num7 += -0.2f;
					break;
				}
				case "Raft":
				{
					float num17 = 0.45f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Short:
							num17 = 0.4f;
							break;
						case PlayerData.PlayerTag.Dwarf:
							num17 = 0.25f;
							break;
						}
					}
					Vector3 v3 = new Vector3(0f, 0f - num17, 0f);
					vector = lastMount2.ComponentBody.Position + Vector3.Transform(v3, Matrix.CreateFromQuaternion(lastMount2.ComponentBody.Rotation));
					vector2 = new Vector3(vector2.X, 0f, vector2.Z);
					num9 += 0.2f;
					num11 += -0.2f;
					num4 += 1.6f;
					num6 += 1.6f;
					num5 += 0.2f;
					num7 += -0.2f;
					break;
				}
				case "Sailing_Boat":
				{
					float num15 = 0.2f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Short:
							num15 = 0.1f;
							break;
						case PlayerData.PlayerTag.Dwarf:
							num15 = -0.1f;
							break;
						}
					}
					Vector3 v2 = new Vector3(0f, 0f - num15, 0f);
					vector = lastMount2.ComponentBody.Position + Vector3.Transform(v2, Matrix.CreateFromQuaternion(lastMount2.ComponentBody.Rotation));
					num8 += 0.4f;
					num10 += 0.4f;
					num9 += 0.2f;
					num11 += -0.2f;
					num4 += 1.1f;
					num6 += 1.1f;
					num5 += 0.2f;
					num7 += -0.2f;
					break;
				}
				case "Moose_Saddled":
				{
					float num18 = 0f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Dwarf:
							num18 = -0.3f;
							break;
						case PlayerData.PlayerTag.Short:
							num18 = -0.15f;
							break;
						}
					}
					vector.Y -= num18;
					num4 += 1.8f;
					num6 += 1.8f;
					num8 += 1.5f;
					num10 += 1.5f;
					num9 += 0.15f;
					num11 += -0.15f;
					num5 += 0.4f;
					num7 += -0.4f;
					num12 = MathUtils.DegToRad(-45f);
					break;
				}
				case "Camel_Saddled":
				{
					float num19 = 0f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Dwarf:
							num19 = -0.3f;
							break;
						case PlayerData.PlayerTag.Short:
							num19 = -0.15f;
							break;
						}
					}
					vector.Y -= num19;
					num4 += 1.25f;
					num6 += 1.25f;
					num8 += 0.3f;
					num10 += 0.3f;
					num9 += 0.15f;
					num11 += -0.15f;
					num5 += 0.51f;
					num7 += -0.51f;
					num12 = 0f;
					break;
				}
				case "Camel_humpback_Saddled":
				{
					float num16 = 0f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Dwarf:
							num16 = -0.3f;
							break;
						case PlayerData.PlayerTag.Short:
							num16 = -0.15f;
							break;
						}
					}
					vector.Y -= num16;
					num4 += 1.25f;
					num6 += 1.25f;
					num8 += 0.3f;
					num10 += 0.3f;
					num9 += 0.15f;
					num11 += -0.15f;
					num5 += 0.51f;
					num7 += -0.51f;
					num12 = 0f;
					break;
				}
				default:
				{
					float num13 = 0f;
					if (m_componentPlayer != null)
					{
						switch (m_componentPlayer.PlayerData.Tag)
						{
						case PlayerData.PlayerTag.Dwarf:
							num13 = -0.3f;
							break;
						case PlayerData.PlayerTag.Short:
							num13 = -0.15f;
							break;
						}
					}
					vector.Y -= num13;
					num4 += 1.8f;
					num6 += 1.8f;
					num8 += 1.5f;
					num10 += 1.5f;
					num9 += -0.15f;
					num11 += 0.15f;
					num5 += 0.4f;
					num7 += -0.4f;
					num12 = MathUtils.DegToRad(-45f);
					break;
				}
				}
			}
			else if (m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled)
			{
				float num20 = (m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue ? MathUtils.Min(0.03f * m_componentCreature.ComponentBody.Velocity.XZ.LengthSquared(), 0.5f) : 0f);
				num4 = (num6 = -0.1f - num20);
				num5 = MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(1.07f * num2 + 400f));
				num7 = 0f - MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(0.93f * num2 + 500f));
			}
			else if (MovementAnimationPhase != 0f)
			{
				num8 = -0.5f * num;
				num10 = 0.5f * num;
				num4 = m_walkLegsAngle * num;
				num6 = 0f - num4;
			}
			if (isCrouching)
			{
				float num21 = 0.65f;
				if (m_componentPlayer != null && m_componentPlayer.PlayerData.Tag == PlayerData.PlayerTag.Dwarf)
				{
					num21 = 0.42f;
				}
				else if (m_componentPlayer != null && m_componentPlayer.PlayerData.Tag == PlayerData.PlayerTag.Short)
				{
					num21 = 0.55f;
				}
				vector.Y -= MathUtils.Lerp(num21, num21 + 0.35f, m_crawlBlend) * m_componentCreature.ComponentBody.Size;
				float num22 = 1.6f;
				float x3 = 1.6f;
				num4 = MathUtils.Lerp(0.2f, x3, m_crawlBlend);
				num5 = MathUtils.Lerp(num22, MathUtils.DegToRad(15f), m_crawlBlend);
				num6 = MathUtils.Lerp(0f - 0.2f, x3, m_crawlBlend);
				num7 = MathUtils.Lerp(0f - num22, 0f - MathUtils.DegToRad(15f), m_crawlBlend);
				if (m_crawlBlend > 0.01f && MovementAnimationPhase != 0f)
				{
					float num23 = MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
					num8 = 0.5f * num23 * m_crawlBlend;
					num10 = -0.5f * num23 * m_crawlBlend;
				}
				else
				{
					num8 = 0f;
					num10 = 0f;
				}
				float x4 = -0.2f;
				float x5 = 1.6f;
				float x6 = 1.6f;
				float x7 = 0.2f;
				float x8 = -0.2f;
				MathUtils.DegToRad(90f);
				float x9 = MathUtils.DegToRad(40f);
				float num24 = MathUtils.DegToRad(15f);
				num9 = MathUtils.Lerp(0.2f, 0f, m_crawlBlend);
				num11 = MathUtils.Lerp(x4, 0f, m_crawlBlend);
				num4 = MathUtils.Lerp(x5, x9, m_crawlBlend);
				num6 = MathUtils.Lerp(x6, x9, m_crawlBlend);
				num5 = MathUtils.Lerp(x7, num24, m_crawlBlend);
				num7 = MathUtils.Lerp(x8, 0f - num24, m_crawlBlend);
			}
			if (m_swimBlend > 0f)
			{
				float x10 = 0f - MathUtils.DegToRad(90f) + m_componentCreature.ComponentLocomotion.LookAngles.Y;
				m_swimBodyPitch = MathUtils.Lerp(m_swimBodyPitch, x10, 6f * m_subsystemTime.GameTimeDelta);
				double num25 = m_subsystemTime.GameTime * 0.8;
				float num26 = (float)(num25 - Math.Floor(num25));
				float num27 = MathUtils.Sin(num26 * 6.28318f);
				float num28 = MathUtils.Cos(num26 * 6.28318f);
				float num29 = MathUtils.Sin((num26 + 0.25f) % 1f * 6.28318f);
				float x11 = MathUtils.DegToRad(160f) + num28 * MathUtils.DegToRad(30f);
				float num30 = num27 * MathUtils.DegToRad(60f);
				float num31 = num29 * MathUtils.DegToRad(60f);
				num8 = MathUtils.Lerp(num8, x11, m_swimBlend);
				num9 = MathUtils.Lerp(num9, num30, m_swimBlend);
				num10 = MathUtils.Lerp(num10, x11, m_swimBlend);
				num11 = MathUtils.Lerp(num11, 0f - num30, m_swimBlend);
				num4 = MathUtils.Lerp(num4, num31, m_swimBlend);
				num5 = MathUtils.Lerp(num5, 0f, m_swimBlend);
				num6 = MathUtils.Lerp(num6, 0f - num31, m_swimBlend);
				num7 = MathUtils.Lerp(num7, 0f, m_swimBlend);
			}
			bool flag2 = lastMount2 != null;
			float num32 = MathUtils.Lerp(0f, m_swimBodyPitch, m_swimBlend) + 1.5f * m_floatBlend;
			Matrix matrix = (flag2 ? (Matrix.CreateRotationX(num12) * Matrix.CreateFromQuaternion(lastMount2.ComponentBody.Rotation)) : (Matrix.CreateRotationX(num32) * Matrix.CreateRotationY(vector2.X)));
			if (!flag2 && m_crawlBlend > 0f)
			{
				matrix = Matrix.CreateRotationX((0f - MathUtils.DegToRad(90f)) * m_crawlBlend) * Matrix.CreateRotationY(vector2.X);
			}
			Matrix matrix2 = Matrix.CreateScale(scale);
			float y = m_componentCreature.ComponentBody.BoxSize.Y;
			Vector3 vector3 = new Vector3(0f, MathUtils.Lerp(y * 0.5f, y, m_crawlBlend), 0f);
			Vector3 vector4 = Vector3.Zero;
			if (!flag2 && m_crawlBlend > 0f)
			{
				vector4 = -m_componentCreature.ComponentBody.Matrix.Forward * (0.4f * m_crawlBlend);
			}
			Vector3 vector5 = Vector3.Zero;
			if (!flag2 && (m_swimBlend > 0.01f || m_floatBlend > 0.01f))
			{
				float num33 = y * 0.45f;
				float num34 = MathUtils.Sin(num32);
				float num35 = MathUtils.Cos(num32);
				float num36 = -0.45f * m_floatBlend;
				vector5 = Vector3.Transform(new Vector3(0f, num33 * (1f - num35) + num36, (0f - num33) * num34), Matrix.CreateRotationY(vector2.X));
			}
			Vector3 vector6 = Vector3.Zero;
			if (!flag2 && m_crawlBlend > 0f)
			{
				vector6 = m_componentCreature.ComponentBody.Matrix.Up * (0.45f * m_crawlBlend);
			}
			float y2 = m_componentCreature.ComponentLocomotion.LookAngles.Y;
			float size = m_componentCreature.ComponentBody.Size;
			Vector3 vector7;
			if (lastMount2 != null || m_swimBlend > 0.01f || m_floatBlend > 0.01f)
			{
				vector7 = Vector3.Zero;
			}
			else
			{
				float num37 = MathUtils.Clamp(MathUtils.Max(0f, 0f - y2), 0f, 1.5f) * 0.15f * size;
				vector7 = -m_componentCreature.ComponentBody.Matrix.Forward * num37;
			}
			Matrix value = matrix2 * Matrix.CreateTranslation(-vector3) * matrix * Matrix.CreateTranslation(vector + vector3 + vector5 + vector4 + vector6 + vector7);
			SetBoneTransform(m_bodyBone.Index, value);
			Matrix hand = Matrix.CreateRotationY(m_handAngles1.Y) * Matrix.CreateRotationX(m_handAngles1.X);
			Matrix hand2 = Matrix.CreateRotationY(m_handAngles2.Y) * Matrix.CreateRotationX(m_handAngles2.X);
			Entity.FindComponent<ComponentOuterClothingModel>()?.SetSleeveTransforms(hand, hand2);
			float num38 = 0f;
			float num39 = 0f;
			if (m_componentMiner != null && m_componentMiner.Inventory != null)
			{
				int slotValue = m_componentMiner.Inventory.GetSlotValue(1);
				int slotValue2 = m_componentMiner.Inventory.GetSlotValue(0);
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(slotValue)];
				Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(slotValue2)];
				int pokingCounter = m_componentMiner.PokingCounter;
				if (m_componentMiner.DigCellFace.HasValue)
				{
					float pokingPhase = m_componentMiner.PokingPhase;
					float num40 = 0f;
					if (pokingPhase > 0f)
					{
						float num41 = 0.35f;
						float num42 = 0.5f;
						if (pokingPhase >= num41 && pokingPhase < num42)
						{
							num40 = MathUtils.Cos((pokingPhase - num41) / (num42 - num41) * 1.570796f);
						}
						else
						{
							float num43 = 1f - num42 + num41;
							num40 = MathUtils.Sin(((pokingPhase >= num42) ? ((pokingPhase - num42) / num43) : ((pokingPhase + (1f - num42)) / num43)) * 1.570796f);
						}
					}
					num38 = ((m_componentMiner.ActiveBlockValue == 0) ? (2.5f * num40) : (0.3f + 3.5f * num40));
					if (AttackOrder || pokingPhase > 0f)
					{
						float y3 = m_componentCreature.ComponentLocomotion.LookAngles.Y;
						num38 += MathUtils.Lerp(-1.2f, 2.6f, (y3 + 1.5708f) / 6.1416f);
					}
					SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
					if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
					{
						bool flag3 = false;
						int cellValue = m_subsystemTerrain.Terrain.GetCellValue(m_componentMiner.DigCellFace.Value.X, m_componentMiner.DigCellFace.Value.Y, m_componentMiner.DigCellFace.Value.Z);
						Block block3 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
						BlockDigMethod digMethod = block3.GetBlockDigMethod(cellValue);
						Func<Block, int, float> func = (Block toolBlock, int toolVal) =>
						{
							if (digMethod == BlockDigMethod.Shovel)
							{
								return toolBlock.ShovelPower;
							}
							if (digMethod == BlockDigMethod.Hack)
							{
								return toolBlock.HackPower;
							}
							return (digMethod == BlockDigMethod.Quarry) ? toolBlock.QuarryPower : 1f;
						};
						float num44 = func(block, slotValue);
						float num45 = func(block2, slotValue2);
						if (num44 > num45)
						{
							flag3 = false;
						}
						else if (num45 > num44)
						{
							flag3 = true;
						}
						else if ((pokingCounter & 1) != 0)
						{
							flag3 = true;
						}
						if (flag3)
						{
							num39 = num38;
							num38 = 0f;
						}
					}
				}
				else
				{
					float num46 = MathUtils.Sin(MathUtils.Sqrt(m_componentMiner.PokingPhase) * 2.7015927f);
					num38 = ((m_componentMiner.ActiveBlockValue == 0) ? (2.5f * num46) : (0.3f + 3.5f * num46));
					if (AttackOrder || m_componentMiner.PokingPhase > 0f)
					{
						float y4 = m_componentCreature.ComponentLocomotion.LookAngles.Y;
						float num47 = MathUtils.Lerp(-1.2f, 2.6f, (y4 + 1.5708f) / 6.1416f);
						num38 += num47;
					}
					SubsystemGameInfo subsystemGameInfo2 = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
					if (subsystemGameInfo2 != null && subsystemGameInfo2.WorldSettings.GameMode == GameMode.Embrace)
					{
						bool flag4 = false;
						float meleePower = block.GetMeleePower(slotValue);
						float meleePower2 = block2.GetMeleePower(slotValue2);
						if (meleePower > 1.1f && meleePower2 <= 1.1f)
						{
							flag4 = false;
						}
						else if (meleePower2 > 1.1f && meleePower <= 1.1f)
						{
							flag4 = true;
						}
						else if ((pokingCounter & 1) != 0)
						{
							flag4 = true;
						}
						if (flag4)
						{
							num39 = num38;
							num38 = 0f;
						}
					}
				}
			}
			float num48 = ((m_punchPhase != 0f) ? ((0f - MathUtils.DegToRad(90f)) * MathUtils.Sin((float)Math.PI * 2f * MathUtils.Sigmoid(m_punchPhase, 4f))) : 0f);
			float num49 = (((m_punchCounter & 1) == 0) ? num48 : 0f);
			float num50 = (((m_punchCounter & 1) != 0) ? num48 : 0f);
			float num51 = 0f;
			float num52 = 0f;
			float num53 = 0f;
			float num54 = 0f;
			if (m_rowRight)
			{
				ComponentMount componentMount = ((m_componentRider != null) ? m_componentRider.Mount : null);
				if (componentMount != null)
				{
					ComponentSimpleModel componentSimpleModel = componentMount.Entity.FindComponent<ComponentSimpleModel>();
					if (componentSimpleModel != null && componentSimpleModel.ShowPaddle)
					{
						float paddlePhase = componentSimpleModel.GetPaddlePhase();
						float paddleActivity = componentSimpleModel.GetPaddleActivity();
						float num55 = MathUtils.Sin(paddlePhase);
						float x12 = MathUtils.Cos(paddlePhase);
						num53 = 0.65f * num55 * paddleActivity + 0.12f;
						num54 = 0f - (0.24f + 0.24f * MathUtils.Max(0f, x12)) * paddleActivity * 1.25f + 0.06f;
					}
					else
					{
						float num56 = (float)m_subsystemTime.GameTime;
						float num57 = 0.6f * (float)MathUtils.Sin(6.91 * (double)num56);
						float num58 = 0.2f + 0.2f * (float)MathUtils.Cos(6.91 * ((double)num56 + 0.5));
						num53 = num57 + 0.12f;
						num54 = 0f - num58 * 1.25f + 0.06f;
					}
				}
			}
			float num59 = 0f;
			float num60 = 0f;
			float num61 = 0f;
			float num62 = 0f;
			if (m_aimHandAngle != 0f)
			{
				float y5 = m_componentCreature.ComponentLocomotion.LookAngles.Y;
				float num63 = MathUtils.Sin(MathUtils.Sqrt(m_aimHandAngle) * 2.4515927f);
				num59 = m_aimHandAngle * 1f + num63 + MathUtils.Lerp(-1.2f, 1.2f, (y5 + MathUtils.DegToRad(90f)) / MathUtils.DegToRad(180f)) - MathUtils.DegToRad(15f);
				num60 = 0f;
				num61 = 1.5f + num63 * 0.5f + MathUtils.Lerp(-1f, 1f, (y5 + MathUtils.DegToRad(90f)) / MathUtils.DegToRad(180f)) - MathUtils.DegToRad(15f);
				num62 = -0.8f;
				if (lastMount2 != null)
				{
					float num64 = 1.5f;
					switch (lastMount2.Entity.ValuesDictionary.DatabaseObject.Name)
					{
					case "Boat":
					case "Sailing_Boat":
						num64 = 0.4f;
						break;
					case "Raft":
						num64 = 0.2f;
						break;
					case "Camel_Saddled":
					case "Camel_humpback_Saddled":
						num64 = 0.3f;
						break;
					}
					num59 -= num64 + num12;
					num61 -= num64 + num12;
					float num65 = MathUtils.Clamp(m_componentCreature.ComponentLocomotion.LookAngles.X, 0f - MathUtils.DegToRad(80f), MathUtils.DegToRad(80f));
					num60 -= num65;
					num62 -= num65;
				}
			}
			float num66 = ((!m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled) ? 1 : 4);
			num8 += MathUtils.Lerp(-0.1f, 0.1f, SimplexNoise.Noise(num2)) + num50 + num51 + num61 + num39;
			num9 += MathUtils.Lerp(0f, num66 * 0.15f, SimplexNoise.Noise(1.1f * num2 + 100f)) + num52 + num62;
			num10 += num38 + MathUtils.Lerp(-0.1f, 0.1f, SimplexNoise.Noise(0.9f * num2 + 200f)) + num49 + num53 + num59;
			num11 += 0f - MathUtils.Lerp(0f, num66 * 0.15f, SimplexNoise.Noise(1.05f * num2 + 300f)) + num54 + num60;
			if (m_componentMiner != null)
			{
				int num67 = Terrain.ExtractContents(m_componentMiner.ActiveBlockValue);
				int num68 = Terrain.ExtractData(m_componentMiner.ActiveBlockValue);
				float num69 = (float)m_subsystemTime.GameTime;
				float num70 = MathUtils.Sin(num69 * 1.1f) * 0.02f + SimplexNoise.Noise(num69 * 0.8f) * 0.03f;
				float num71 = MathUtils.Sin(num69 * 0.9f) * 0.02f + SimplexNoise.Noise(num69 * 0.8f + 100f) * 0.03f;
				if (num67 == 410 || num67 == 411)
				{
					float y6 = m_componentCreature.ComponentLocomotion.LookAngles.Y;
					float num72 = MathUtils.Lerp(-1.2f, 1.2f, (y6 + MathUtils.DegToRad(90f)) / MathUtils.DegToRad(180f));
					num10 += MathUtils.DegToRad(65f) + num70 + num72;
					num11 += num71;
					num8 += MathUtils.DegToRad(35f) + num70;
					num9 += MathUtils.DegToRad(-45f) + num71;
					m_aimHandAngle = 0f;
				}
				else if (num67 == 116 || num67 == 82 || num67 == 408)
				{
					if (m_aimHandAngle <= 0.05f)
					{
						num10 += MathUtils.DegToRad(65f) + num70;
						num11 += num71;
					}
				}
				else if (num67 == 379 && (num68 & 1) == 1)
				{
					num10 += MathUtils.DegToRad(25f) + num70;
					num11 += num71;
					num8 += MathUtils.DegToRad(35f) + num70;
					num9 += MathUtils.DegToRad(-45f) + num71;
				}
				else if (num67 == 429)
				{
					num11 += MathUtils.DegToRad(5f);
				}
			}
			bool flag5 = m_componentCreature.Entity.FindComponent<ComponentRider>()?.Mount != null;
			if (!IsStandingOnBlock() && !flag5)
			{
				float f = MathUtils.Clamp(MathUtils.Abs(m_componentCreature.ComponentBody.Velocity.Y) / 10f, 0f, 1f);
				float num73 = (float)m_subsystemTime.GameTime * 2f;
				float num74 = MathUtils.Lerp(-0.3f, -1.5f, f) * MathUtils.Sin(num73);
				float num75 = MathUtils.Lerp(0f, 0.75f, f) * MathUtils.Sin(num73);
				float num76 = MathUtils.Lerp(0f, 0.5f, f) * MathUtils.Sin(num73 * 2f);
				float num77 = MathUtils.Lerp(0f, MathUtils.DegToRad(90f), f);
				float num78 = MathUtils.Lerp(-0.1f, -0.5f, f) * MathUtils.Sin(num73);
				float num79 = MathUtils.Lerp(0f, MathUtils.DegToRad(120f), f) * MathUtils.Sin(num73 * 2f);
				num4 += num74 + num76;
				num5 += num75;
				num6 += 0f - num74 - num76;
				num7 += 0f - num75;
				num8 += num77 + num79;
				num9 += num78;
				num10 += 0f - num77 - num79;
				num11 += num78;
				if (m_floatBlend > 0.01f)
				{
					float num80 = 1.3f * m_floatBlend;
					float num81 = 0.7f * m_floatBlend;
					num9 += num80;
					num11 -= num80;
					num5 += num81;
					num7 -= num81;
					num4 *= 1f - m_floatBlend;
					num6 *= 1f - m_floatBlend;
					num8 *= 1f - m_floatBlend;
					num10 *= 1f - m_floatBlend;
				}
				if (m_componentCreature.ComponentBody.Velocity.XZ.LengthSquared() < 0.2f && isCrouching)
				{
					float num82 = MathUtils.DegToRad(-20f);
					num8 += num82;
					num10 += num82;
					float num83 = MathUtils.DegToRad(-50f);
					num4 += num83;
					num6 += num83;
				}
			}
			if (lastMount2 != null)
			{
				num3 -= num12;
			}
			float num84 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
			m_headAngles += num84 * (new Vector2(x2, num3) - m_headAngles);
			m_handAngles1 += num84 * (new Vector2(num8, num9) - m_handAngles1);
			m_handAngles2 += num84 * (new Vector2(num10, num11) - m_handAngles2);
			m_legAngles1 += num84 * (new Vector2(num4, num5) - m_legAngles1);
			m_legAngles2 += num84 * (new Vector2(num6, num7) - m_legAngles2);
			if (m_crawlBlend > 0f)
			{
				Vector2 vector8 = new Vector2(MathUtils.DegToRad(200f), 0f);
				Vector2 vector9 = new Vector2(MathUtils.DegToRad(40f), MathUtils.DegToRad(15f));
				Vector2 vector10 = new Vector2(MathUtils.DegToRad(40f), 0f - MathUtils.DegToRad(15f));
				float f2 = 20f * m_subsystemTime.GameTimeDelta * m_crawlBlend;
				float degrees = 25f;
				float x13 = m_componentCreature.ComponentBody.Velocity.Length();
				float num85 = MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
				float num86 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
				float num87 = MathUtils.Saturate(x13);
				float num88 = 0.3f * num87 * m_crawlBlend;
				float num89 = 0.2f * num87 * m_crawlBlend;
				Vector2 vector11 = new Vector2(num88 * num85, 0f);
				Vector2 vector12 = new Vector2(num88 * num86, 0f);
				Vector2 vector13 = new Vector2(num89 * num86, 0.5f * num89 * num85);
				Vector2 vector14 = new Vector2((0f - num89) * num86, -0.5f * num89 * num85);
				m_headAngles.Y = MathUtils.Clamp(m_headAngles.Y + MathUtils.DegToRad(degrees) * m_crawlBlend, 0f - MathUtils.DegToRad(80f), MathUtils.DegToRad(80f));
				m_handAngles1 = Vector2.Lerp(m_handAngles1, vector8 + vector11, f2);
				m_handAngles2 = Vector2.Lerp(m_handAngles2, vector8 + vector12, f2);
				m_legAngles1 = Vector2.Lerp(m_legAngles1, vector9 + vector13, f2);
				m_legAngles2 = Vector2.Lerp(m_legAngles2, vector10 + vector14, f2);
			}
			if (flag)
			{
				Matrix value2 = Matrix.CreateRotationX(m_headAngles.Y) * Matrix.CreateRotationZ(0f - m_headAngles.X);
				value2.Translation += new Vector3(0f, 1f, 0f);
				value2 *= Matrix.CreateScale(0.001f);
				SetBoneTransform(m_headBone.Index, value2);
			}
			else
			{
				float num90 = -0.6f * m_floatBlend;
				SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(m_headAngles.Y + num90) * Matrix.CreateRotationZ(0f - m_headAngles.X));
			}
			SetBoneTransform(m_hand1Bone.Index, Matrix.CreateRotationY(m_handAngles1.Y) * Matrix.CreateRotationX(m_handAngles1.X));
			SetBoneTransform(m_hand2Bone.Index, Matrix.CreateRotationY(m_handAngles2.Y) * Matrix.CreateRotationX(m_handAngles2.X));
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationY(m_legAngles1.Y) * Matrix.CreateRotationX(m_legAngles1.X));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationY(m_legAngles2.Y) * Matrix.CreateRotationX(m_legAngles2.X));
			m_sleepPositionSelected = false;
			m_randomDeathRotationsGenerated = false;
			return;
		}
		float num91 = MathUtils.Max(DeathPhase, m_lieDownFactorModel);
		if (m_isSleeping)
		{
			if (!m_sleepPositionSelected)
			{
				m_sleepPositionSelected = true;
				int num92 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.X);
				int num93 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Z);
				float x14 = SimplexNoise.Noise((float)num92 * 0.1f + 50f, (float)num93 * 0.1f + 50f);
				m_sleepBodyPosition = (int)MathUtils.Floor(MathUtils.Saturate(x14) * 4f);
				float f3 = SimplexNoise.Noise((float)num92 * 0.1f + 100f, (float)num93 * 0.1f + 100f);
				float f4 = SimplexNoise.Noise((float)num92 * 0.1f + 200f, (float)num93 * 0.1f + 200f);
				m_sleepHeadRotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(10f), MathUtils.DegToRad(10f), f3), MathUtils.Lerp(0f - MathUtils.DegToRad(10f), MathUtils.DegToRad(10f), f4));
				float f5 = SimplexNoise.Noise((float)num92 * 0.1f + 300f, (float)num93 * 0.1f + 300f);
				float f6 = SimplexNoise.Noise((float)num92 * 0.1f + 400f, (float)num93 * 0.1f + 400f);
				m_sleepHand1Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(20f), MathUtils.DegToRad(20f), f5), MathUtils.Lerp(0f - MathUtils.DegToRad(20f), MathUtils.DegToRad(20f), f6));
				float f7 = SimplexNoise.Noise((float)num92 * 0.1f + 500f, (float)num93 * 0.1f + 500f);
				float f8 = SimplexNoise.Noise((float)num92 * 0.1f + 600f, (float)num93 * 0.1f + 600f);
				m_sleepHand2Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(20f), MathUtils.DegToRad(20f), f7), MathUtils.Lerp(0f - MathUtils.DegToRad(20f), MathUtils.DegToRad(20f), f8));
				float f9 = SimplexNoise.Noise((float)num92 * 0.1f + 700f, (float)num93 * 0.1f + 700f);
				float f10 = SimplexNoise.Noise((float)num92 * 0.1f + 800f, (float)num93 * 0.1f + 800f);
				m_sleepLeg1Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(10f), MathUtils.DegToRad(10f), f9), MathUtils.Lerp(0f - MathUtils.DegToRad(10f), MathUtils.DegToRad(10f), f10));
				float f11 = SimplexNoise.Noise((float)num92 * 0.1f + 900f, (float)num93 * 0.1f + 900f);
				float f12 = SimplexNoise.Noise((float)num92 * 0.1f + 1000f, (float)num93 * 0.1f + 1000f);
				m_sleepLeg2Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(10f), MathUtils.DegToRad(10f), f11), MathUtils.Lerp(0f - MathUtils.DegToRad(10f), MathUtils.DegToRad(10f), f12));
			}
			float x15 = vector2.X;
			float num94 = 0f;
			float num95 = 0f;
			switch (m_sleepBodyPosition)
			{
			case 0:
				num94 = MathUtils.DegToRad(90f);
				break;
			case 1:
				num94 = MathUtils.DegToRad(-90f);
				break;
			case 2:
				num95 = MathUtils.DegToRad(90f);
				break;
			case 3:
				num95 = MathUtils.DegToRad(-90f);
				break;
			}
			Vector3 v4 = m_componentCreature.ComponentBody.BoxSize * 0.5f;
			Matrix matrix3 = Matrix.CreateFromYawPitchRoll(x15, num94 * num91, num95 * num91);
			Matrix matrix4 = Matrix.CreateScale(scale) * matrix3;
			Vector3 vector15 = Vector3.Transform(v4, matrix4);
			Matrix value3 = matrix4 * Matrix.CreateTranslation(vector - vector15 + new Vector3(0f, 0.4f, 0f));
			SetBoneTransform(m_bodyBone.Index, value3);
			Matrix value4 = Matrix.CreateRotationX(m_sleepHeadRotation.Y * num91) * Matrix.CreateRotationZ((0f - m_sleepHeadRotation.X) * num91);
			SetBoneTransform(m_headBone.Index, value4);
			Matrix value5 = Matrix.CreateRotationY(m_sleepHand1Rotation.Y * num91) * Matrix.CreateRotationX(m_sleepHand1Rotation.X * num91);
			SetBoneTransform(m_hand1Bone.Index, value5);
			Matrix value6 = Matrix.CreateRotationY(m_sleepHand2Rotation.Y * num91) * Matrix.CreateRotationX(m_sleepHand2Rotation.X * num91);
			SetBoneTransform(m_hand2Bone.Index, value6);
			Matrix value7 = Matrix.CreateRotationY(m_sleepLeg1Rotation.Y * num91) * Matrix.CreateRotationX(m_sleepLeg1Rotation.X * num91);
			SetBoneTransform(m_leg1Bone.Index, value7);
			Matrix value8 = Matrix.CreateRotationY(m_sleepLeg2Rotation.Y * num91) * Matrix.CreateRotationX(m_sleepLeg2Rotation.X * num91);
			SetBoneTransform(m_leg2Bone.Index, value8);
		}
		else if (m_isDead)
		{
			if (!m_deathRotationsGenerated)
			{
				m_deathRotationsGenerated = true;
				int num96 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.X);
				int num97 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Z);
				float f13 = SimplexNoise.Noise((float)num96 * 0.2f + 1100f, (float)num97 * 0.2f + 1100f);
				float f14 = SimplexNoise.Noise((float)num96 * 0.2f + 1200f, (float)num97 * 0.2f + 1200f);
				m_deathHeadRotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(45f), MathUtils.DegToRad(45f), f13), MathUtils.Lerp(0f - MathUtils.DegToRad(45f), MathUtils.DegToRad(45f), f14));
				float f15 = SimplexNoise.Noise((float)num96 * 0.2f + 1300f, (float)num97 * 0.2f + 1300f);
				float f16 = SimplexNoise.Noise((float)num96 * 0.2f + 1400f, (float)num97 * 0.2f + 1400f);
				m_deathHand1Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f15), MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f16));
				float f17 = SimplexNoise.Noise((float)num96 * 0.2f + 1500f, (float)num97 * 0.2f + 1500f);
				float f18 = SimplexNoise.Noise((float)num96 * 0.2f + 1600f, (float)num97 * 0.2f + 1600f);
				m_deathHand2Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f17), MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f18));
				float f19 = SimplexNoise.Noise((float)num96 * 0.2f + 1700f, (float)num97 * 0.2f + 1700f);
				float f20 = SimplexNoise.Noise((float)num96 * 0.2f + 1800f, (float)num97 * 0.2f + 1800f);
				m_deathLeg1Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f19), MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f20));
				float f21 = SimplexNoise.Noise((float)num96 * 0.2f + 1900f, (float)num97 * 0.2f + 1900f);
				float f22 = SimplexNoise.Noise((float)num96 * 0.2f + 2000f, (float)num97 * 0.2f + 2000f);
				m_deathLeg2Rotation = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f21), MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), f22));
				float f23 = SimplexNoise.Noise((float)num96 * 0.2f + 2100f, (float)num97 * 0.2f + 2100f);
				float f24 = SimplexNoise.Noise((float)num96 * 0.2f + 2200f, (float)num97 * 0.2f + 2200f);
				m_deathBodyTilt = new Vector2(MathUtils.Lerp(0f - MathUtils.DegToRad(15f), MathUtils.DegToRad(15f), f23), MathUtils.Lerp(0f - MathUtils.DegToRad(15f), MathUtils.DegToRad(15f), f24));
			}
			if (!m_deathPositionSelected)
			{
				m_deathPositionSelected = true;
				float num98 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.X);
				int num99 = Terrain.ToCell(m_componentCreature.ComponentBody.Position.Z);
				float x16 = SimplexNoise.Noise(num98 * 0.2f + 2300f, (float)num99 * 0.2f + 2300f);
				m_sleepBodyPosition = (int)MathUtils.Floor(MathUtils.Saturate(x16) * 4f);
			}
			float num100 = 0f;
			float num101 = 0f;
			switch (m_sleepBodyPosition)
			{
			case 0:
				num100 = MathUtils.DegToRad(90f);
				break;
			case 1:
				num100 = MathUtils.DegToRad(-90f);
				break;
			case 2:
				num101 = MathUtils.DegToRad(90f);
				break;
			case 3:
				num101 = MathUtils.DegToRad(-90f);
				break;
			}
			float num102 = MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), SimplexNoise.Noise((float)Terrain.ToCell(m_componentCreature.ComponentBody.Position.X) * 0.2f + 2400f, (float)Terrain.ToCell(m_componentCreature.ComponentBody.Position.Z) * 0.2f + 2400f));
			float num103 = num100 + m_deathBodyTilt.X;
			float num104 = num101 + m_deathBodyTilt.Y;
			Vector3 v5 = m_componentCreature.ComponentBody.BoxSize * 0.5f;
			Matrix matrix5 = Matrix.CreateFromYawPitchRoll(num102 * num91, num103 * num91, num104 * num91);
			Matrix matrix6 = Matrix.CreateScale(scale) * matrix5;
			Vector3 vector16 = Vector3.Transform(v5, matrix6);
			Matrix value9 = matrix6 * Matrix.CreateTranslation(vector - vector16 + new Vector3(0f, 0.4f, 0f));
			SetBoneTransform(m_bodyBone.Index, value9);
			Matrix value10 = Matrix.CreateRotationX(m_deathHeadRotation.Y * num91) * Matrix.CreateRotationZ((0f - m_deathHeadRotation.X) * num91);
			SetBoneTransform(m_headBone.Index, value10);
			Matrix value11 = Matrix.CreateRotationY(m_deathHand1Rotation.Y * num91) * Matrix.CreateRotationX(m_deathHand1Rotation.X * num91);
			SetBoneTransform(m_hand1Bone.Index, value11);
			Matrix value12 = Matrix.CreateRotationY(m_deathHand2Rotation.Y * num91) * Matrix.CreateRotationX(m_deathHand2Rotation.X * num91);
			SetBoneTransform(m_hand2Bone.Index, value12);
			Matrix value13 = Matrix.CreateRotationY(m_deathLeg1Rotation.Y * num91) * Matrix.CreateRotationX(m_deathLeg1Rotation.X * num91);
			SetBoneTransform(m_leg1Bone.Index, value13);
			Matrix value14 = Matrix.CreateRotationY(m_deathLeg2Rotation.Y * num91) * Matrix.CreateRotationX(m_deathLeg2Rotation.X * num91);
			SetBoneTransform(m_leg2Bone.Index, value14);
		}
	}

	public override void DrawExtras(Camera camera)
	{
		Vector3 vector = Vector3.One;
		if (m_componentPlayer != null)
		{
			switch (m_componentPlayer.PlayerData.Tag)
			{
			case PlayerData.PlayerTag.Short:
				vector = new Vector3(0.85f, 0.85f, 0.85f);
				break;
			case PlayerData.PlayerTag.Fat:
				vector = new Vector3(1f, 1f, 1f);
				break;
			case PlayerData.PlayerTag.Tall:
				vector = new Vector3(1f, 1f, 1f);
				break;
			case PlayerData.PlayerTag.Dwarf:
				vector = new Vector3(0.7f, 0.7f, 0.7f);
				break;
			}
		}
		vector *= m_componentCreature.ComponentBody.Size;
		Vector3 vector2 = new Vector3(0.05f, 0.05f, -0.56f);
		int num = 0;
		if (m_componentMiner != null)
		{
			num = m_componentMiner.ActiveBlockValue;
			SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentMiner.Inventory != null)
			{
				num = m_componentMiner.Inventory.GetSlotValue(1);
			}
		}
		if (m_componentCreature.ComponentHealth.Health > 0f && m_componentMiner != null && num != 0)
		{
			int num2 = Terrain.ExtractContents(num);
			Block block = BlocksManager.Blocks[num2];
			Matrix matrix = AbsoluteBoneTransformsForCamera[m_hand2Bone.Index];
			matrix *= camera.InvertedViewMatrix;
			matrix.Right = Vector3.Normalize(matrix.Right);
			matrix.Up = Vector3.Normalize(matrix.Up);
			matrix.Forward = Vector3.Normalize(matrix.Forward);
			Vector3 position = block.InHandOffset + m_inHandItemOffset;
			if (isCrouching)
			{
				position += new Vector3(0f, 0.1f, 0f);
			}
			Vector3 position2 = vector2 * vector;
			Matrix matrix2 = Matrix.CreateRotationY(MathUtils.DegToRad(block.InHandRotation.Y) + m_inHandItemRotation.Y) * Matrix.CreateRotationZ(MathUtils.DegToRad(block.InHandRotation.Z) + m_inHandItemRotation.Z) * Matrix.CreateRotationX(MathUtils.DegToRad(block.InHandRotation.X) + m_inHandItemRotation.X) * Matrix.CreateTranslation(position) * Matrix.CreateTranslation(position2) * matrix;
			int x = Terrain.ToCell(matrix2.Translation.X);
			int y = Terrain.ToCell(matrix2.Translation.Y);
			int z = Terrain.ToCell(matrix2.Translation.Z);
			m_drawBlockEnvironmentData.InWorldMatrix = matrix2;
			m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
			m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
			m_drawBlockEnvironmentData.Light = (int)(m_smoothedLight * 15f);
			m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			Matrix matrix3 = matrix2 * camera.ViewMatrix;
			block.DrawBlock(m_subsystemModelsRenderer.PrimitivesRenderer, num, Color.White, block.InHandScale * m_componentCreature.ComponentBody.Size, ref matrix3, m_drawBlockEnvironmentData);
		}
		SubsystemGameInfo subsystemGameInfo2 = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		if (subsystemGameInfo2 != null && subsystemGameInfo2.WorldSettings.GameMode == GameMode.Embrace && m_componentCreature.ComponentHealth.Health > 0f && m_componentMiner != null && m_componentMiner.Inventory != null)
		{
			int slotValue = m_componentMiner.Inventory.GetSlotValue(0);
			if (slotValue != 0 && (OffhandRodValue == 0 || slotValue != OffhandRodValue))
			{
				int num3 = Terrain.ExtractContents(slotValue);
				Block block2 = BlocksManager.Blocks[num3];
				Matrix matrix4 = AbsoluteBoneTransformsForCamera[m_hand1Bone.Index];
				matrix4 *= camera.InvertedViewMatrix;
				matrix4.Right = Vector3.Normalize(matrix4.Right);
				matrix4.Up = Vector3.Normalize(matrix4.Up);
				matrix4.Forward = Vector3.Normalize(matrix4.Forward);
				Vector3 position3 = block2.InHandOffset + m_inHandItemOffset;
				if (isCrouching)
				{
					position3 += new Vector3(0f, 0.1f, -0.2f);
				}
				Vector3 position4 = new Vector3(-0.05f, 0.05f, -0.56f) * vector;
				Matrix matrix5 = Matrix.CreateRotationY(MathUtils.DegToRad(block2.InHandRotation.Y) + m_inHandItemRotation.Y) * Matrix.CreateRotationZ(MathUtils.DegToRad(block2.InHandRotation.Z) + m_inHandItemRotation.Z) * Matrix.CreateRotationX(MathUtils.DegToRad(block2.InHandRotation.X) + m_inHandItemRotation.X) * Matrix.CreateTranslation(position3) * Matrix.CreateTranslation(position4) * matrix4;
				int x2 = Terrain.ToCell(matrix5.Translation.X);
				int y2 = Terrain.ToCell(matrix5.Translation.Y);
				int z2 = Terrain.ToCell(matrix5.Translation.Z);
				m_drawBlockEnvironmentData.InWorldMatrix = matrix5;
				m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x2, z2);
				m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x2, z2) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
				m_drawBlockEnvironmentData.Light = (int)(m_smoothedLight * 15f);
				m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
				m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
				Matrix matrix6 = matrix5 * camera.ViewMatrix;
				block2.DrawBlock(m_subsystemModelsRenderer.PrimitivesRenderer, slotValue, Color.White, block2.InHandScale * m_componentCreature.ComponentBody.Size, ref matrix6, m_drawBlockEnvironmentData);
			}
		}
		if (ForceLeftRodWhileFishing && OffhandRodValue != 0)
		{
			int num4 = Terrain.ExtractContents(OffhandRodValue);
			Block block3 = BlocksManager.Blocks[num4];
			Matrix matrix7 = AbsoluteBoneTransformsForCamera[m_hand1Bone.Index];
			matrix7 *= camera.InvertedViewMatrix;
			matrix7.Right = Vector3.Normalize(matrix7.Right);
			matrix7.Up = Vector3.Normalize(matrix7.Up);
			matrix7.Forward = Vector3.Normalize(matrix7.Forward);
			Vector3 position5 = block3.InHandOffset + m_inHandItemOffset;
			if (isCrouching)
			{
				position5 += new Vector3(0f, 0.1f, -0.2f);
			}
			Vector3 position6 = new Vector3(0f - vector2.X, vector2.Y, vector2.Z) * vector;
			Matrix matrix8 = Matrix.CreateRotationY(MathUtils.DegToRad(block3.InHandRotation.Y) + m_inHandItemRotation.Y) * Matrix.CreateRotationZ(MathUtils.DegToRad(block3.InHandRotation.Z) + m_inHandItemRotation.Z) * Matrix.CreateRotationX(MathUtils.DegToRad(block3.InHandRotation.X) + m_inHandItemRotation.X) * Matrix.CreateTranslation(position5) * Matrix.CreateTranslation(position6) * matrix7;
			int x3 = Terrain.ToCell(matrix8.Translation.X);
			int y3 = Terrain.ToCell(matrix8.Translation.Y);
			int z3 = Terrain.ToCell(matrix8.Translation.Z);
			m_drawBlockEnvironmentData.InWorldMatrix = matrix8;
			m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x3, z3);
			m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x3, z3) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y3);
			m_drawBlockEnvironmentData.Light = (int)(m_smoothedLight * 15f);
			m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			Matrix matrix9 = matrix8 * camera.ViewMatrix;
			block3.DrawBlock(m_subsystemModelsRenderer.PrimitivesRenderer, OffhandRodValue, Color.White, block3.InHandScale * m_componentCreature.ComponentBody.Size, ref matrix9, m_drawBlockEnvironmentData);
		}
		ComponentSmartChaseBehavior componentSmartChaseBehavior = m_componentCreature.Entity.FindComponent<ComponentSmartChaseBehavior>();
		if (componentSmartChaseBehavior != null && componentSmartChaseBehavior.PickedItemCount > 0)
		{
			int num5 = Terrain.ExtractContents(componentSmartChaseBehavior.PickedItemType);
			Block block4 = BlocksManager.Blocks[num5];
			Matrix matrix10 = AbsoluteBoneTransformsForCamera[m_hand2Bone.Index];
			matrix10 *= camera.InvertedViewMatrix;
			matrix10.Right = Vector3.Normalize(matrix10.Right);
			matrix10.Up = Vector3.Normalize(matrix10.Up);
			matrix10.Forward = Vector3.Normalize(matrix10.Forward);
			Vector3 position7 = block4.InHandOffset + m_inHandItemOffset;
			if (isCrouching)
			{
				position7 += new Vector3(0f, 0.1f, -0.2f);
			}
			Vector3 position8 = vector2 * vector;
			Matrix matrix11 = Matrix.CreateRotationY(MathUtils.DegToRad(block4.InHandRotation.Y) + m_inHandItemRotation.Y) * Matrix.CreateRotationZ(MathUtils.DegToRad(block4.InHandRotation.Z) + m_inHandItemRotation.Z) * Matrix.CreateRotationX(MathUtils.DegToRad(block4.InHandRotation.X) + m_inHandItemRotation.X) * Matrix.CreateTranslation(position7) * Matrix.CreateTranslation(position8) * matrix10;
			int x4 = Terrain.ToCell(matrix11.Translation.X);
			int y4 = Terrain.ToCell(matrix11.Translation.Y);
			int z4 = Terrain.ToCell(matrix11.Translation.Z);
			m_drawBlockEnvironmentData.InWorldMatrix = matrix11;
			m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x4, z4);
			m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x4, z4) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y4);
			m_drawBlockEnvironmentData.Light = (int)(m_smoothedLight * 15f);
			m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			Matrix matrix12 = matrix11 * camera.ViewMatrix;
			block4.DrawBlock(m_subsystemModelsRenderer.PrimitivesRenderer, componentSmartChaseBehavior.PickedItemType, Color.White, block4.InHandScale * m_componentCreature.ComponentBody.Size, ref matrix12, m_drawBlockEnvironmentData);
		}
		DrawBodyBags(camera);
		DrawHeadAccessory(camera);
		base.DrawExtras(camera);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemModelsRenderer = Project.FindSubsystem<SubsystemModelsRenderer>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_componentMiner = Entity.FindComponent<ComponentMiner>();
		m_componentRider = Entity.FindComponent<ComponentRider>();
		m_componentSleep = Entity.FindComponent<ComponentSleep>();
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
		m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
		m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
		m_walkLegsAngle = valuesDictionary.GetValue<float>("WalkLegsAngle");
		m_headAccessoryValue = 0;
		if (m_componentPlayer != null)
		{
			m_headAccessoryValue = ComputeHeadAccessoryFromGuid(m_componentPlayer.PlayerData.PlayerGUID);
		}
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		if (Model != null)
		{
			m_bodyBone = Model.FindBone("Body");
			m_headBone = Model.FindBone("Head");
			m_leg1Bone = Model.FindBone("Leg1");
			m_leg2Bone = Model.FindBone("Leg2");
			m_hand1Bone = Model.FindBone("Hand1");
			m_hand2Bone = Model.FindBone("Hand2");
		}
		else
		{
			m_bodyBone = null;
			m_headBone = null;
			m_leg1Bone = null;
			m_leg2Bone = null;
			m_hand1Bone = null;
			m_hand2Bone = null;
		}
	}

	public override Vector3 CalculateEyePosition()
	{
		float num = 0f;
		float f = MathUtils.Sigmoid(m_lieDownFactorEye, 1f);
		float num2 = MathUtils.Sigmoid(m_sneakFactor, 4f);
		float num3 = 0.875f * currentSneakBoxSize;
		float num4 = MathUtils.Lerp(MathUtils.Lerp(num3, 0.45f * num3, num2), 0.2f * num3, f);
		Matrix matrix = m_componentCreature.ComponentBody.Matrix;
		Math.Abs(m_componentCreature.ComponentLocomotion.LookAngles.Y);
		float num5 = 0f;
		ComponentMount componentMount = m_componentCreature.Entity.FindComponent<ComponentRider>()?.Mount;
		float num6 = 0f;
		if (componentMount != null)
		{
			string name = componentMount.Entity.ValuesDictionary.DatabaseObject.Name;
			switch (name)
			{
			case "Boat":
			case "Raft":
			case "Sailing_Boat":
				matrix = Matrix.CreateFromQuaternion(componentMount.ComponentBody.Rotation);
				break;
			}
			switch (name)
			{
			case "Raft":
				num6 = -0.45f;
				break;
			case "Boat":
				num6 = -0.15f;
				break;
			case "Sailing_Boat":
				num6 = -0.15f;
				break;
			case "Camel_Saddled":
			case "Camel_humpback_Saddled":
				num6 = 0f;
				break;
			default:
				num = 0f;
				num6 = -0.185f;
				num5 = 0.4f;
				break;
			}
			if (m_componentPlayer != null)
			{
				switch (m_componentPlayer.PlayerData.Tag)
				{
				case PlayerData.PlayerTag.Short:
					num6 += 0.06f;
					break;
				case PlayerData.PlayerTag.Dwarf:
					num6 += 0.2f;
					break;
				}
			}
		}
		float num7 = 0f;
		if (m_componentPlayer != null)
		{
			switch (m_componentPlayer.PlayerData.Tag)
			{
			case PlayerData.PlayerTag.Dwarf:
				num7 = -0.5f;
				break;
			case PlayerData.PlayerTag.Short:
				num7 = -0.25f;
				break;
			}
		}
		if (isCrouching && m_componentPlayer != null)
		{
			switch (m_componentPlayer.PlayerData.Tag)
			{
			case PlayerData.PlayerTag.Dwarf:
				num7 = -0.2f;
				break;
			case PlayerData.PlayerTag.Short:
				num7 = -0.1f;
				break;
			}
		}
		float size = m_componentCreature.ComponentBody.Size;
		if (Math.Abs(size - 1f) > 0.001f)
		{
			float num8 = MathUtils.Lerp(1f, size, num2);
			num4 *= num8;
			num7 *= num8;
		}
		return m_componentCreature.ComponentBody.Position + matrix.Up * (num4 + 2f * Bob + num6 + num7 + num) + matrix.Forward * (-0.2f * num2 + num5);
	}

	public override Quaternion CalculateEyeRotation()
	{
		float num = 0f;
		if (m_lieDownFactorEye != 0f)
		{
			num += MathUtils.DegToRad(80f) * MathUtils.Sigmoid(MathUtils.Max(m_lieDownFactorEye - 0.2f, 0f) / 0.8f, 4f);
		}
		Quaternion rotation = m_componentCreature.ComponentBody.Rotation;
		ComponentMount componentMount = m_componentCreature.Entity.FindComponent<ComponentRider>()?.Mount;
		if (componentMount != null)
		{
			switch (componentMount.Entity.ValuesDictionary.DatabaseObject.Name)
			{
			case "Boat":
			case "Raft":
			case "Sailing_Boat":
				rotation = componentMount.ComponentBody.Rotation;
				break;
			}
		}
		return rotation * Quaternion.CreateFromYawPitchRoll(0f - m_componentCreature.ComponentLocomotion.LookAngles.X, m_componentCreature.ComponentLocomotion.LookAngles.Y, num);
	}

	public ComponentHumanModel()
	{
		m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();
		m_random = new Random();
	}

	public void StartCrouch()
	{
		isCrouching = true;
	}

	public void StopCrouch()
	{
		isCrouching = false;
	}

	private bool IsStandingOnBlock()
	{
		if (m_componentCreature.Entity.FindComponent<ComponentRider>()?.Mount != null)
		{
			return true;
		}
		Vector3 position = m_componentCreature.ComponentBody.Position;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position.X), Terrain.ToCell(position.Y - 0.1f), Terrain.ToCell(position.Z));
		return BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable;
	}

	private void ActivateLadderClimbingAnimation(float dt)
	{
		if (!(Math.Abs(m_componentCreature.ComponentBody.Velocity.Y) <= 0.01f))
		{
			float f = 0.75f;
			float num = 2f;
			float num2 = (float)m_subsystemTime.GameTime * num;
			float num3 = MathUtils.Lerp(-0.225f, -1.125f, f) * MathUtils.Sin(num2);
			float num4 = MathUtils.Lerp(0f, 0.575f, f) * MathUtils.Sin(num2);
			float num5 = MathUtils.Lerp(0f, 0.375f, f) * MathUtils.Sin(num2 * 2f);
			float num6 = MathUtils.Lerp(0f, MathUtils.DegToRad(67.5f), f);
			float y = MathUtils.Lerp(-0.075f, -0.375f, f) * MathUtils.Sin(num2);
			float num7 = MathUtils.Lerp(0f, MathUtils.DegToRad(90f), f) * MathUtils.Sin(num2 * 2f);
			m_legAngles1 = Vector2.Lerp(m_legAngles1, new Vector2(num3 + num5, num4), 10f * dt);
			m_legAngles2 = Vector2.Lerp(m_legAngles2, new Vector2(0f - num3 - num5, 0f - num4), 10f * dt);
			m_handAngles1 = Vector2.Lerp(m_handAngles1, new Vector2(num6 + num7, y), 30f * dt);
			m_handAngles2 = Vector2.Lerp(m_handAngles2, new Vector2(0f - num6 - num7, y), 30f * dt);
		}
	}

	public Matrix GetFishingHandMatrix(Camera camera, bool useLeftHand)
	{
		Matrix matrix = AbsoluteBoneTransformsForCamera[useLeftHand ? m_hand1Bone.Index : m_hand2Bone.Index];
		matrix *= camera.InvertedViewMatrix;
		matrix.Right = Vector3.Normalize(matrix.Right);
		matrix.Up = Vector3.Normalize(matrix.Up);
		matrix.Forward = Vector3.Normalize(matrix.Forward);
		return Matrix.CreateTranslation(new Vector3(useLeftHand ? (-0.05f) : 0.05f, 0.05f, -0.56f) * (m_componentCreature.ComponentBody.BoxSize.Y / 1.77f)) * matrix;
	}

	private void DrawBodyBags(Camera camera)
	{
		if (m_componentMiner == null || m_componentMiner.Inventory == null)
		{
			return;
		}
		IInventory inventory = m_componentMiner.Inventory;
		float typeMul = 1f;
		if (m_componentPlayer != null)
		{
			switch (m_componentPlayer.PlayerData.Tag)
			{
			case PlayerData.PlayerTag.Short:
				typeMul = 0.9f;
				break;
			case PlayerData.PlayerTag.Fat:
				typeMul = 1f;
				break;
			case PlayerData.PlayerTag.Tall:
				typeMul = 1f;
				break;
			case PlayerData.PlayerTag.Dwarf:
				typeMul = 0.85f;
				break;
			}
		}
		float crouchMul = (isCrouching ? 1.15f : 1f);
		int num = ((Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.GameMode == GameMode.Creative && inventory is ComponentCreativeInventory) ? MathUtils.Min(25, inventory.SlotsCount) : inventory.SlotsCount);
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int[] array = new int[25];
		int num6 = 0;
		int[] array2 = new int[25];
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		for (int i = 0; i < num; i++)
		{
			int slotValue = inventory.GetSlotValue(i);
			int slotCount = inventory.GetSlotCount(i);
			if (slotValue == 0 || slotCount == 0)
			{
				continue;
			}
			switch (Terrain.ExtractContents(slotValue))
			{
			case 416:
				num2++;
				if (num6 < array.Length)
				{
					array[num6++] = slotValue;
				}
				break;
			case 364:
				num3++;
				if (num7 < array2.Length)
				{
					array2[num7++] = slotValue;
				}
				break;
			case 421:
				num4++;
				if (num8 == 0)
				{
					num8 = slotValue;
				}
				break;
			case 422:
				num5++;
				if (num9 == 0)
				{
					num9 = slotValue;
				}
				break;
			}
		}
		int num10 = Terrain.ExtractContents(m_componentMiner.ActiveBlockValue);
		if (num10 == 416 && num2 > 0)
		{
			num2--;
			if (num6 > 0)
			{
				array[0] = array[num6 - 1];
				num6--;
			}
		}
		else if (num10 == 364 && num3 > 0)
		{
			num3--;
			if (num7 > 0)
			{
				array2[0] = array2[num7 - 1];
				num7--;
			}
		}
		else if (num10 == 421 && num4 > 0)
		{
			num4--;
			num8 = 0;
		}
		else if (num10 == 422 && num5 > 0)
		{
			num5--;
			num9 = 0;
		}
		int num11 = 5;
		int num12 = MathUtils.Min(num6, num11);
		num11 -= num12;
		int num13 = MathUtils.Min(num7, num11);
		bool flag = num9 != 0;
		bool flag2 = num8 != 0 && !flag;
		if (num12 + num13 == 0 && !flag && !flag2)
		{
			return;
		}
		Matrix matrix = AbsoluteBoneTransformsForCamera[m_bodyBone.Index] * camera.InvertedViewMatrix;
		matrix.Right = Vector3.Normalize(matrix.Right);
		matrix.Up = Vector3.Normalize(matrix.Up);
		matrix.Forward = Vector3.Normalize(matrix.Forward);
		Matrix matrix2 = Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * matrix;
		matrix2.Right = Vector3.Normalize(matrix2.Right);
		matrix2.Up = Vector3.Normalize(matrix2.Up);
		matrix2.Forward = Vector3.Normalize(matrix2.Forward);
		float y = m_componentCreature.ComponentBody.BoxSize.Y;
		float num14 = 0.1f;
		float num15 = 0f;
		float num16 = 0f;
		float num17 = 0.2f;
		float num18 = 0f;
		float num19 = 0.6f;
		if (m_componentPlayer != null)
		{
			switch (m_componentPlayer.PlayerData.Tag)
			{
			case PlayerData.PlayerTag.Short:
				num18 = num14;
				num19 = 0.55f;
				break;
			case PlayerData.PlayerTag.Fat:
				num18 = num15;
				num19 = 0.7f;
				break;
			case PlayerData.PlayerTag.Tall:
				num18 = num16;
				num19 = 0.6f;
				break;
			case PlayerData.PlayerTag.Dwarf:
				num18 = num17;
				num19 = 0.5f;
				break;
			}
		}
		float num20 = 0.28f;
		Vector3 translation = matrix2.Translation + matrix2.Up * (0.9f * y) - matrix2.Up * (0.2f * y) - matrix2.Up * (num18 * y) + (isCrouching ? (matrix2.Up * (num20 * y)) : Vector3.Zero);
		float baseScale = 0.75f * m_componentCreature.ComponentBody.Size;
		float num21 = 0.2f * y * baseScale;
		float num22 = 0.18f * y * baseScale;
		float num23 = 0.2f * y * baseScale;
		float y2 = -0.03f * y * baseScale - 0.2f * y;
		Vector3[] array3 = new Vector3[5]
		{
			new Vector3((0f - num21) * 0.8f, y2, num22 * 0.6f),
			new Vector3(num21 * 0.8f, y2, num22 * 0.6f),
			new Vector3(0f, y2, num22 * 0.6f),
			new Vector3((0f - num21) * num19, y2, (0f - num23) * 0.8f),
			new Vector3(num21 * num19, y2, (0f - num23) * 0.8f)
		};
		Matrix baseLocalRot = Matrix.CreateRotationY(MathUtils.DegToRad(90f));
		Matrix carrier = Matrix.Identity;
		carrier.Right = matrix2.Right;
		carrier.Up = matrix2.Up;
		carrier.Forward = matrix2.Forward;
		carrier.Translation = translation;
		Action<int, Vector3, float> action = (int value, Vector3 localPos, float sizeMul) =>
		{
			int num28 = Terrain.ExtractContents(value);
			Block obj = BlocksManager.Blocks[num28];
			Matrix matrix3 = Matrix.CreateRotationY(MathUtils.DegToRad((localPos.Z >= 0f) ? 90f : (-90f)));
			Matrix matrix4 = baseLocalRot * matrix3 * Matrix.CreateTranslation(localPos) * carrier;
			int x = Terrain.ToCell(matrix4.Translation.X);
			int y3 = Terrain.ToCell(matrix4.Translation.Y);
			int z = Terrain.ToCell(matrix4.Translation.Z);
			m_drawBlockEnvironmentData.InWorldMatrix = matrix4;
			m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
			m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y3);
			m_drawBlockEnvironmentData.Light = (int)(m_smoothedLight * 15f);
			m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			Matrix matrix5 = matrix4 * camera.ViewMatrix;
			obj.DrawBlock(size: obj.InHandScale * (baseScale * sizeMul * typeMul * crouchMul), primitivesRenderer: m_subsystemModelsRenderer.PrimitivesRenderer, value: value, color: Color.White, matrix: ref matrix5, environmentData: m_drawBlockEnvironmentData);
		};
		int num24 = 0;
		int num25 = 0;
		while (num25 < num12 && num24 < 5)
		{
			action(array[num25], array3[num24], 1f);
			num25++;
			num24++;
		}
		int num26 = 0;
		while (num26 < num13 && num24 < 5)
		{
			action(array2[num26], array3[num24], 1f);
			num26++;
			num24++;
		}
		if (flag | flag2)
		{
			Vector3 vector = array3[2];
			Vector3 arg = new Vector3(vector.X, vector.Y + 0.12f * y, vector.Z);
			float arg2 = 1.25f;
			int num27 = (flag ? num9 : num8);
			if (num27 != 0)
			{
				action(num27, arg, arg2);
			}
		}
	}

	private int ComputeHeadAccessoryFromGuid(Guid g)
	{
		if (EmbraceHeadItems == null || EmbraceHeadItems.Length == 0)
		{
			return 0;
		}
		byte[] array = g.ToByteArray();
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			num = num * 31 + array[i];
		}
		if (num < 0)
		{
			num = -num;
		}
		int num2 = num % EmbraceHeadItems.Length;
		return EmbraceHeadItems[num2];
	}

	private void DrawHeadAccessory(Camera camera)
	{
		if (m_componentPlayer == null || m_headBone == null || m_headAccessoryValue == 0)
		{
			return;
		}
		SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			Matrix matrix = AbsoluteBoneTransformsForCamera[m_headBone.Index];
			matrix *= camera.InvertedViewMatrix;
			matrix.Right = Vector3.Normalize(matrix.Right);
			matrix.Up = Vector3.Normalize(matrix.Up);
			matrix.Forward = Vector3.Normalize(matrix.Forward);
			float num = m_componentCreature.ComponentBody.BoxSize.Y / 1.77f;
			Matrix matrix2 = Matrix.CreateTranslation(new Vector3(0f, 0f, 0.82f * num)) * matrix;
			int x = Terrain.ToCell(matrix2.Translation.X);
			int y = Terrain.ToCell(matrix2.Translation.Y);
			int z = Terrain.ToCell(matrix2.Translation.Z);
			m_drawBlockEnvironmentData.InWorldMatrix = matrix2;
			m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
			m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
			m_drawBlockEnvironmentData.Light = (int)(m_smoothedLight * 15f);
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			float x2 = MathUtils.DegToRad(88f);
			Vector3 vector = Vector3.Normalize(Vector3.TransformNormal(matrix.Forward, camera.ViewMatrix));
			Vector3 vector2 = Vector3.Normalize(Vector3.TransformNormal(matrix.Up, camera.ViewMatrix));
			Vector3 vector3 = Vector3.Normalize(Vector3.TransformNormal(matrix.Right, camera.ViewMatrix));
			Vector3 vector4 = Vector3.Normalize(vector * MathUtils.Cos(x2) + vector2 * MathUtils.Sin(x2));
			if (MathUtils.Abs(Vector3.Dot(vector4, Vector3.UnitY)) > 0.995f)
			{
				vector4 = Vector3.Normalize(vector4 + 0.02f * vector3);
			}
			Vector3 v = -Vector3.UnitZ;
			if (Vector3.Dot(vector4, v) < 0f)
			{
				vector4 = -vector4;
			}
			m_drawBlockEnvironmentData.BillboardDirection = vector4;
			int num2 = Terrain.ExtractContents(m_headAccessoryValue);
			Block obj = BlocksManager.Blocks[num2];
			float size = ((num2 == 226) ? 0.18f : 0.6f) * num;
			Matrix matrix3 = matrix2 * camera.ViewMatrix;
			obj.DrawBlock(m_subsystemModelsRenderer.PrimitivesRenderer, m_headAccessoryValue, Color.White, size, ref matrix3, m_drawBlockEnvironmentData);
		}
	}
}
