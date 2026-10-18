using System;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFourLeggedModel : ComponentCreatureModel
{
	public enum Gait
	{
		Walk,
		Trot,
		Canter
	}

	public SubsystemAudio m_subsystemAudio;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public ModelBone m_bodyBone;

	public ModelBone m_neckBone;

	public ModelBone m_headBone;

	public ModelBone m_leg1Bone;

	public ModelBone m_leg2Bone;

	public ModelBone m_leg3Bone;

	public ModelBone m_leg4Bone;

	public float m_walkAnimationSpeed;

	public float m_canterLegsAngleFactor;

	public float m_walkFrontLegsAngle;

	public float m_walkHindLegsAngle;

	public float m_walkBobHeight;

	public bool m_moveLegWhenFeeding;

	public bool m_canCanter;

	public bool m_canTrot;

	public bool m_useCanterSound;

	public Gait m_gait;

	public float m_feedFactor;

	public float m_buttFactor;

	public float m_buttPhase;

	public float m_footstepsPhase;

	public float m_legAngle1;

	public float m_legAngle2;

	public float m_legAngle3;

	public float m_legAngle4;

	public float m_headAngleY;

	public float LastMove;

	private float m_fallShakeTime;

	public SubsystemTerrain m_subsystemTerrain;

	public float m_fallSpinPhase;

	public float m_fallPitchPhase;

	public bool m_isSaddled;

	public float m_fallTime;

	private float m_relaxLieFactor;

	private float m_currentStareAngle;

	private Vector3 m_smoothedModelUp = Vector3.UnitY;

	public bool RelaxLieOrder { get; set; }

	public float StareAngleOrder { get; set; }

	public override void Update(float dt)
	{
		float footstepsPhase = m_footstepsPhase;
		float num = m_componentCreature.ComponentLocomotion.SlipSpeed ?? Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
		if (m_canCanter && num > 0.7f * m_componentCreature.ComponentLocomotion.WalkSpeed)
		{
			m_gait = Gait.Canter;
			MovementAnimationPhase += num * dt * 0.7f * m_walkAnimationSpeed;
			m_footstepsPhase += 0.7f * m_walkAnimationSpeed * num * dt;
		}
		else if (m_canTrot && num > 0.5f * m_componentCreature.ComponentLocomotion.WalkSpeed)
		{
			m_gait = Gait.Trot;
			MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
			m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
		}
		else if (MathUtils.Abs(num) > 0.2f)
		{
			m_gait = Gait.Walk;
			MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
			m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
		}
		else
		{
			m_gait = Gait.Walk;
			MovementAnimationPhase = 0f;
			m_footstepsPhase = 0f;
		}
		if (m_componentCreature.ComponentBody.ImmersionFactor < 0.05f && m_componentCreature.ComponentBody.Velocity.Y < -1f)
		{
			m_fallTime += dt;
			if (m_fallTime >= 1.5f)
			{
				float num2 = MathUtils.Min(m_fallTime - 1.5f, 1f);
				m_fallSpinPhase = MathUtils.Remainder(m_fallSpinPhase + 0.6f * dt * num2, (float)Math.PI * 2f);
				m_fallPitchPhase = MathUtils.Remainder(m_fallPitchPhase + 2f * dt * num2, (float)Math.PI * 2f);
			}
		}
		else
		{
			m_fallTime = 0f;
			m_fallSpinPhase = 0f;
			m_fallPitchPhase = 0f;
		}
		float num3;
		if (m_gait == Gait.Canter)
		{
			num3 = (0f - m_walkBobHeight) * 1.5f * MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
		}
		else
		{
			num3 = ((m_gait != Gait.Trot) ? ((0f - m_walkBobHeight) * MathUtils.Sqr(MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase))) : (m_walkBobHeight * 1.5f * MathUtils.Sqr(MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase))));
		}
		float num4 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
		Bob += num4 * (num3 - Bob);
		m_relaxLieFactor = MathUtils.Clamp(m_relaxLieFactor + (RelaxLieOrder ? 1f : (-1f)) * 2.5f * dt, 0f, 1f);
		if (m_relaxLieFactor > 0.01f)
		{
			MovementAnimationPhase = 0f;
			m_footstepsPhase = 0f;
		}
		if (m_relaxLieFactor <= 0.01f)
		{
			if (m_gait == Gait.Canter && m_useCanterSound)
			{
				float num5 = MathUtils.Floor(m_footstepsPhase);
				if (m_footstepsPhase > num5 && footstepsPhase <= num5)
				{
					string footstepSoundMaterialName = m_subsystemSoundMaterials.GetFootstepSoundMaterialName(m_componentCreature);
					if (!string.IsNullOrEmpty(footstepSoundMaterialName) && footstepSoundMaterialName != "Water")
					{
						m_subsystemAudio.PlayRandomSound("Audio/Footsteps/CanterDirt", 0.75f, m_random.Float(-0.25f, 0f), m_componentCreature.ComponentBody.Position, 3f, autoDelay: true);
					}
				}
			}
			else
			{
				float num6 = MathUtils.Floor(m_footstepsPhase);
				if (m_footstepsPhase > num6 && footstepsPhase <= num6)
				{
					m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
				}
			}
		}
		m_feedFactor = (FeedOrder ? MathUtils.Min(m_feedFactor + 2f * dt, 1f) : MathUtils.Max(m_feedFactor - 2f * dt, 0f));
		IsAttackHitMoment = false;
		if (AttackOrder)
		{
			m_buttFactor = MathUtils.Min(m_buttFactor + 4f * dt, 1f);
			float buttPhase = m_buttPhase;
			m_buttPhase = MathUtils.Remainder(m_buttPhase + dt * 2f, 1f);
			if (buttPhase < 0.5f && m_buttPhase >= 0.5f)
			{
				IsAttackHitMoment = true;
			}
		}
		else
		{
			m_buttFactor = MathUtils.Max(m_buttFactor - 4f * dt, 0f);
			if (m_buttPhase != 0f)
			{
				if (m_buttPhase > 0.5f)
				{
					m_buttPhase = MathUtils.Remainder(MathUtils.Min(m_buttPhase + dt * 2f, 1f), 1f);
				}
				else if (m_buttPhase > 0f)
				{
					m_buttPhase = MathUtils.Max(m_buttPhase - dt * 2f, 0f);
				}
			}
		}
		if (m_currentStareAngle != StareAngleOrder)
		{
			float num7 = 50f * dt;
			if (m_currentStareAngle < StareAngleOrder)
			{
				m_currentStareAngle = MathUtils.Min(m_currentStareAngle + num7, StareAngleOrder);
			}
			else
			{
				m_currentStareAngle = MathUtils.Max(m_currentStareAngle - num7, StareAngleOrder);
			}
		}
		LastAttackOrder = AttackOrder;
		LastFeedOrder = FeedOrder;
		m_attackOrder = false;
		m_feedOrder = false;
		base.Update(dt);
		if (!m_componentCreature.ComponentBody.StandingOnValue.HasValue)
		{
			m_fallShakeTime += dt;
		}
		RelaxLieOrder = false;
	}

	public override void Animate()
	{
		ComponentBody componentBody = m_componentCreature.ComponentBody;
		Vector3 position = componentBody.Position;
		float gameTimeDelta = m_subsystemTime.GameTimeDelta;
		Vector3 v = ((componentBody.m_embraceUpVector != Vector3.Zero) ? componentBody.m_embraceUpVector : Vector3.UnitY);
		m_smoothedModelUp = Vector3.Lerp(m_smoothedModelUp, v, 6f * gameTimeDelta);
		if (m_smoothedModelUp.LengthSquared() < 0.001f)
		{
			m_smoothedModelUp = Vector3.UnitY;
		}
		else
		{
			m_smoothedModelUp = Vector3.Normalize(m_smoothedModelUp);
		}
		Vector3 forward = componentBody.Matrix.Forward;
		Vector3 v2 = Vector3.Cross(m_smoothedModelUp, forward);
		if (v2.LengthSquared() < 0.01f)
		{
			v2 = Vector3.Cross(m_smoothedModelUp, Vector3.UnitX);
		}
		v2 = Vector3.Normalize(v2);
		Vector3 vector = Vector3.Normalize(Vector3.Cross(v2, m_smoothedModelUp));
		Matrix matrix = new Matrix(0f - v2.X, 0f - v2.Y, 0f - v2.Z, 0f, m_smoothedModelUp.X, m_smoothedModelUp.Y, m_smoothedModelUp.Z, 0f, 0f - vector.X, 0f - vector.Y, 0f - vector.Z, 0f, 0f, 0f, 0f, 1f);
		Vector3 vector2 = componentBody.Rotation.ToYawPitchRoll();
		if (m_componentCreature.ComponentHealth.Health > 0f)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			if (MovementAnimationPhase != 0f && (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f))
			{
				if (m_gait == Gait.Canter)
				{
					float num6 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
					float num7 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.25f));
					float num8 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.15f));
					float num9 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.4f));
					num = m_walkFrontLegsAngle * m_canterLegsAngleFactor * num6;
					num2 = m_walkFrontLegsAngle * m_canterLegsAngleFactor * num7;
					num3 = m_walkHindLegsAngle * m_canterLegsAngleFactor * num8;
					num4 = m_walkHindLegsAngle * m_canterLegsAngleFactor * num9;
					num5 = MathUtils.DegToRad(8f) * MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
				}
				else if (m_gait == Gait.Trot)
				{
					float num10 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
					float num11 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
					float num12 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
					float num13 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
					num = m_walkFrontLegsAngle * num10;
					num2 = m_walkFrontLegsAngle * num11;
					num3 = m_walkHindLegsAngle * num12;
					num4 = m_walkHindLegsAngle * num13;
					num5 = MathUtils.DegToRad(3f) * MathUtils.Sin((float)Math.PI * 4f * MovementAnimationPhase);
				}
				else
				{
					float num14 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
					float num15 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
					float num16 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.25f));
					float num17 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.75f));
					num = m_walkFrontLegsAngle * num14;
					num2 = m_walkFrontLegsAngle * num15;
					num3 = m_walkHindLegsAngle * num16;
					num4 = m_walkHindLegsAngle * num17;
					num5 = MathUtils.DegToRad(3f) * MathUtils.Sin((float)Math.PI * 4f * MovementAnimationPhase);
				}
			}
			float num18 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
			m_legAngle1 += num18 * (num - m_legAngle1);
			m_legAngle2 += num18 * (num2 - m_legAngle2);
			m_legAngle3 += num18 * (num3 - m_legAngle3);
			m_legAngle4 += num18 * (num4 - m_legAngle4);
			m_headAngleY += num18 * (num5 - m_headAngleY);
			Vector2 vector3 = m_componentCreature.ComponentLocomotion.LookAngles;
			vector3.Y += m_headAngleY;
			vector3.X = MathUtils.Clamp(vector3.X, 0f - MathUtils.DegToRad(65f), MathUtils.DegToRad(65f));
			vector3.Y = MathUtils.Clamp(vector3.Y, 0f - MathUtils.DegToRad(55f), MathUtils.DegToRad(55f));
			Vector2 vector4 = Vector2.Zero;
			if (m_neckBone != null)
			{
				vector4 = 0.6f * vector3;
				vector3 = 0.4f * vector3;
			}
			float x = MathUtils.DegToRad(80f);
			float x2 = MathUtils.DegToRad(60f);
			float num19 = MathUtils.DegToRad(13f);
			float num20 = MathUtils.DegToRad(-35f);
			float num21 = MathUtils.DegToRad(25f) * m_relaxLieFactor;
			float num22 = MathUtils.Lerp(m_legAngle1, x, m_relaxLieFactor);
			float num23 = MathUtils.Lerp(m_legAngle2, x, m_relaxLieFactor);
			float num24 = MathUtils.Lerp(m_legAngle3, x2, m_relaxLieFactor);
			float num25 = MathUtils.Lerp(m_legAngle4, x2, m_relaxLieFactor);
			Vector2 vector5 = new Vector2(vector3.X, vector3.Y + num20 * m_relaxLieFactor);
			Vector2 vector6 = ((m_neckBone != null) ? new Vector2(vector4.X, vector4.Y + 0.5f * num20 * m_relaxLieFactor) : Vector2.Zero);
			Matrix value = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			if (!m_isSaddled && (m_fallSpinPhase != 0f || m_fallPitchPhase != 0f))
			{
				value *= Matrix.CreateRotationZ(m_fallSpinPhase);
				value *= Matrix.CreateRotationX(m_fallPitchPhase);
			}
			value *= Matrix.CreateRotationX(num19 * m_relaxLieFactor);
			float num26 = 0.45f * m_componentCreature.ComponentBody.BoxSize.Y * m_relaxLieFactor;
			if (m_currentStareAngle != 0f)
			{
				float num27 = MathUtils.DegToRad(m_currentStareAngle);
				value *= Matrix.CreateRotationX(num27);
				float z = m_componentCreature.ComponentBody.BoxSize.Z;
				float y = m_componentCreature.ComponentBody.BoxSize.Y;
				float num28 = z * 1.05f;
				float z2 = 0f;
				float num29;
				if (m_currentStareAngle > 0f)
				{
					num29 = MathUtils.Sin(num27) * num28;
					z2 = (0f - (1f - MathUtils.Cos(num27))) * num28;
					num29 += (1f - MathUtils.Cos(num27)) * (y * 0.3f);
				}
				else
				{
					num29 = MathUtils.Sin(num27) * (y * 0.4f);
				}
				value *= Matrix.CreateTranslation(0f, num29, z2);
			}
			value *= matrix;
			float num30 = Bob - num26;
			Vector3 position2 = position + m_smoothedModelUp * num30;
			value *= Matrix.CreateTranslation(position2);
			SetBoneTransform(m_bodyBone.Index, value);
			float num31 = MathUtils.DegToRad(m_currentStareAngle);
			float num32 = 0f;
			if (m_currentStareAngle > 0f)
			{
				float num33 = MathUtils.Saturate(m_currentStareAngle / 90f);
				num32 = 0.2f * m_componentCreature.ComponentBody.BoxSize.Y * num33;
			}
			Matrix value2 = Matrix.CreateRotationX(vector5.Y - num31) * Matrix.CreateRotationZ(0f - vector5.X);
			value2 *= Matrix.CreateTranslation(0f, 0f, 0f - num32);
			SetBoneTransform(m_headBone.Index, value2);
			if (m_neckBone != null)
			{
				SetBoneTransform(m_neckBone.Index, Matrix.CreateRotationX(vector6.Y) * Matrix.CreateRotationZ(0f - vector6.X));
			}
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(num22 - num31));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(num23 - num31));
			SetBoneTransform(m_leg3Bone.Index, Matrix.CreateFromYawPitchRoll(num21, num24 - num31, 0f));
			SetBoneTransform(m_leg4Bone.Index, Matrix.CreateFromYawPitchRoll(0f - num21, num25 - num31, 0f));
			if (!IsStandingOnBlock())
			{
				float gameTimeDelta2 = m_subsystemTime.GameTimeDelta;
				m_fallShakeTime += gameTimeDelta2;
				float num34 = Math.Abs(m_componentCreature.ComponentBody.Velocity.Y);
				float num35 = MathUtils.Lerp(0f, 1f, MathUtils.Clamp(num34 / 5f, 0f, 1f));
				float num36 = num35 * MathUtils.Sin(m_fallShakeTime * 5f + 0f);
				float num37 = num35 * MathUtils.Sin(m_fallShakeTime * 5f + 1f);
				float num38 = num35 * MathUtils.Sin(m_fallShakeTime * 5f + 2f);
				float num39 = num35 * MathUtils.Sin(m_fallShakeTime * 5f + 3f);
				SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(num22 + num36));
				SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(num23 + num37));
				SetBoneTransform(m_leg3Bone.Index, Matrix.CreateRotationX(num24 - num39));
				SetBoneTransform(m_leg4Bone.Index, Matrix.CreateRotationX(num25 - num38));
			}
		}
		else
		{
			float num40 = 1f - DeathPhase;
			float num41 = ((Vector3.Dot(m_componentCreature.ComponentBody.Matrix.Right, DeathCauseOffset) > 0f) ? 1f : (-1f));
			float num42 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
			Matrix value3 = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			if (m_fallSpinPhase != 0f || m_fallPitchPhase != 0f)
			{
				value3 *= Matrix.CreateRotationZ(m_fallSpinPhase);
				value3 *= Matrix.CreateRotationX(m_fallPitchPhase);
			}
			value3 *= Matrix.CreateTranslation(-0.5f * num42 * Vector3.UnitY * DeathPhase);
			value3 *= Matrix.CreateFromYawPitchRoll(vector2.X, 0f, (float)Math.PI / 2f * DeathPhase * num41);
			value3 *= Matrix.CreateTranslation(0.2f * num42 * Vector3.UnitY * DeathPhase);
			value3 *= Matrix.CreateTranslation(position);
			SetBoneTransform(m_bodyBone.Index, value3);
			SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(MathUtils.DegToRad(50f) * DeathPhase));
			if (m_neckBone != null)
			{
				SetBoneTransform(m_neckBone.Index, Matrix.Identity);
			}
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(m_legAngle1 * num40));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(m_legAngle2 * num40));
			SetBoneTransform(m_leg3Bone.Index, Matrix.CreateRotationX(m_legAngle3 * num40));
			SetBoneTransform(m_leg4Bone.Index, Matrix.CreateRotationX(m_legAngle4 * num40));
		}
		base.Animate();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
		m_walkFrontLegsAngle = valuesDictionary.GetValue<float>("WalkFrontLegsAngle");
		m_walkHindLegsAngle = valuesDictionary.GetValue<float>("WalkHindLegsAngle");
		m_canterLegsAngleFactor = valuesDictionary.GetValue<float>("CanterLegsAngleFactor");
		m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
		m_moveLegWhenFeeding = valuesDictionary.GetValue<bool>("MoveLegWhenFeeding");
		m_canCanter = valuesDictionary.GetValue<bool>("CanCanter");
		m_canTrot = valuesDictionary.GetValue<bool>("CanTrot");
		m_useCanterSound = valuesDictionary.GetValue<bool>("UseCanterSound");
		m_isSaddled = Entity.ValuesDictionary.DatabaseObject.Name.EndsWith("_Saddled");
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		if (Model != null)
		{
			m_bodyBone = Model.FindBone("Body");
			m_neckBone = Model.FindBone("Neck", throwIfNotFound: false);
			m_headBone = Model.FindBone("Head");
			m_leg1Bone = Model.FindBone("Leg1");
			m_leg2Bone = Model.FindBone("Leg2");
			m_leg3Bone = Model.FindBone("Leg3");
			m_leg4Bone = Model.FindBone("Leg4");
		}
		else
		{
			m_bodyBone = null;
			m_neckBone = null;
			m_headBone = null;
			m_leg1Bone = null;
			m_leg2Bone = null;
			m_leg3Bone = null;
			m_leg4Bone = null;
		}
	}

	private bool IsStandingOnBlock()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position.X), Terrain.ToCell(position.Y - 0.1f), Terrain.ToCell(position.Z));
		return BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable;
	}
}
