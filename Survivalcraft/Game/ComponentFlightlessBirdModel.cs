using System;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFlightlessBirdModel : ComponentCreatureModel
{
	public ModelBone m_bodyBone;

	public ModelBone m_neckBone;

	public ModelBone m_headBone;

	public ModelBone m_leg1Bone;

	public ModelBone m_leg2Bone;

	public float m_walkAnimationSpeed;

	public float m_walkLegsAngle;

	public float m_walkBobHeight;

	public float m_feedFactor;

	public float m_footstepsPhase;

	public float m_kickFactor;

	public float m_kickPhase;

	public float m_legAngle1;

	public float m_legAngle2;

	public float m_headAngleY;

	public float m_fallSpinPhase;

	public float m_fallPitchPhase;

	public float m_fallTime;

	private float m_relaxLieFactor;

	public bool RelaxLieOrder { get; set; }

	public override void Update(float dt)
	{
		float footstepsPhase = m_footstepsPhase;
		float num = m_componentCreature.ComponentLocomotion.SlipSpeed ?? Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
		if (MathUtils.Abs(num) > 0.2f)
		{
			MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
			m_footstepsPhase += 1.25f * m_walkAnimationSpeed * num * dt;
		}
		else
		{
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
		float num3 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase));
		float num4 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
		Bob += num4 * (num3 - Bob);
		m_relaxLieFactor = MathUtils.Clamp(m_relaxLieFactor + (RelaxLieOrder ? 1f : (-1f)) * 2.5f * dt, 0f, 1f);
		if (m_relaxLieFactor > 0.01f)
		{
			MovementAnimationPhase = 0f;
			m_footstepsPhase = 0f;
		}
		float num5 = MathUtils.Floor(m_footstepsPhase);
		if (m_relaxLieFactor <= 0.01f && m_footstepsPhase > num5 && footstepsPhase <= num5)
		{
			m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
		}
		m_feedFactor = (FeedOrder ? MathUtils.Min(m_feedFactor + 2f * dt, 1f) : MathUtils.Max(m_feedFactor - 2f * dt, 0f));
		IsAttackHitMoment = false;
		if (AttackOrder)
		{
			m_kickFactor = MathUtils.Min(m_kickFactor + 6f * dt, 1f);
			float kickPhase = m_kickPhase;
			m_kickPhase = MathUtils.Remainder(m_kickPhase + dt * 2f, 1f);
			if (kickPhase < 0.5f && m_kickPhase >= 0.5f)
			{
				IsAttackHitMoment = true;
			}
		}
		else
		{
			m_kickFactor = MathUtils.Max(m_kickFactor - 6f * dt, 0f);
			if (m_kickPhase != 0f)
			{
				if (m_kickPhase > 0.5f)
				{
					m_kickPhase = MathUtils.Remainder(MathUtils.Min(m_kickPhase + dt * 2f, 1f), 1f);
				}
				else if (m_kickPhase > 0f)
				{
					m_kickPhase = MathUtils.Max(m_kickPhase - dt * 2f, 0f);
				}
			}
		}
		LastAttackOrder = AttackOrder;
		LastFeedOrder = FeedOrder;
		AttackOrder = false;
		FeedOrder = false;
		base.Update(dt);
		RelaxLieOrder = false;
	}

	public override void Animate()
	{
		Vector3 position = m_componentCreature.ComponentBody.Position;
		Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
		if (m_componentCreature.ComponentHealth.Health > 0f)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			if (MovementAnimationPhase != 0f && (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f))
			{
				float num4 = ((Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward) > 0.75f * m_componentCreature.ComponentLocomotion.WalkSpeed) ? (1.5f * m_walkLegsAngle) : m_walkLegsAngle);
				float num5 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0f));
				float num6 = MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + 0.5f));
				num = num4 * num5 + m_kickPhase;
				num2 = num4 * num6;
				num3 = MathUtils.DegToRad(5f) * MathUtils.Sin((float)Math.PI * 4f * MovementAnimationPhase);
			}
			if (m_kickFactor != 0f)
			{
				float x = MathUtils.DegToRad(60f) * MathUtils.Sin((float)Math.PI * MathUtils.Sigmoid(m_kickPhase, 5f));
				num = MathUtils.Lerp(num, x, m_kickFactor);
			}
			float num7 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
			m_legAngle1 += num7 * (num - m_legAngle1);
			m_legAngle2 += num7 * (num2 - m_legAngle2);
			m_headAngleY += num7 * (num3 - m_headAngleY);
			Vector2 vector2 = m_componentCreature.ComponentLocomotion.LookAngles;
			vector2.Y += m_headAngleY;
			vector2.X = MathUtils.Clamp(vector2.X, 0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f));
			vector2.Y = MathUtils.Clamp(vector2.Y, 0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(50f));
			Vector2 vector3 = Vector2.Zero;
			if (m_neckBone != null)
			{
				vector3 = 0.4f * vector2;
				vector2 = 0.6f * vector2;
			}
			float x2 = MathUtils.DegToRad(85f);
			float num8 = MathUtils.DegToRad(13f);
			float num9 = MathUtils.DegToRad(-35f);
			float num10 = MathUtils.DegToRad(25f) * m_relaxLieFactor;
			float pitch = MathUtils.Lerp(m_legAngle1, x2, m_relaxLieFactor);
			float pitch2 = MathUtils.Lerp(m_legAngle2, x2, m_relaxLieFactor);
			Vector2 vector4 = new Vector2(vector2.X, vector2.Y + num9 * m_relaxLieFactor);
			Vector2 vector5 = ((m_neckBone != null) ? new Vector2(vector3.X, vector3.Y + 0.6f * num9 * m_relaxLieFactor) : Vector2.Zero);
			Matrix value = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			if (m_fallSpinPhase != 0f || m_fallPitchPhase != 0f)
			{
				value *= Matrix.CreateRotationZ(m_fallSpinPhase);
				value *= Matrix.CreateRotationX(m_fallPitchPhase);
			}
			value *= Matrix.CreateRotationX(num8 * m_relaxLieFactor);
			float num11 = 0.45f * m_componentCreature.ComponentBody.BoxSize.Y * m_relaxLieFactor;
			value *= Matrix.CreateRotationY(vector.X);
			value *= Matrix.CreateTranslation(position.X, position.Y + Bob - num11, position.Z);
			SetBoneTransform(m_bodyBone.Index, value);
			SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(vector4.Y) * Matrix.CreateRotationZ(0f - vector4.X));
			if (m_neckBone != null)
			{
				SetBoneTransform(m_neckBone.Index, Matrix.CreateRotationX(vector5.Y) * Matrix.CreateRotationZ(0f - vector5.X));
			}
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateFromYawPitchRoll(0f - num10, pitch, 0f));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateFromYawPitchRoll(num10, pitch2, 0f));
		}
		else
		{
			float num12 = 1f - DeathPhase;
			float num13 = ((Vector3.Dot(m_componentFrame.Matrix.Right, DeathCauseOffset) > 0f) ? 1 : (-1));
			float num14 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
			Matrix value2 = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			if (m_fallSpinPhase != 0f || m_fallPitchPhase != 0f)
			{
				value2 *= Matrix.CreateRotationZ(m_fallSpinPhase);
				value2 *= Matrix.CreateRotationX(m_fallPitchPhase);
			}
			value2 *= Matrix.CreateTranslation(-0.5f * num14 * DeathPhase * Vector3.UnitY);
			value2 *= Matrix.CreateFromYawPitchRoll(vector.X, 0f, (float)Math.PI / 2f * DeathPhase * num13);
			value2 *= Matrix.CreateTranslation(0.2f * num14 * DeathPhase * Vector3.UnitY);
			value2 *= Matrix.CreateTranslation(position);
			SetBoneTransform(m_bodyBone.Index, value2);
			SetBoneTransform(m_bodyBone.Index, value2);
			SetBoneTransform(m_headBone.Index, Matrix.Identity);
			if (m_neckBone != null)
			{
				SetBoneTransform(m_neckBone.Index, Matrix.Identity);
			}
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(m_legAngle1 * num12));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(m_legAngle2 * num12));
		}
		base.Animate();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
		m_walkLegsAngle = valuesDictionary.GetValue<float>("WalkLegsAngle");
		m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
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
		}
		else
		{
			m_bodyBone = null;
			m_neckBone = null;
			m_headBone = null;
			m_leg1Bone = null;
			m_leg2Bone = null;
		}
	}

	public override void EmbraceMutate(float t)
	{
		if (m_bodyBone == null)
		{
			return;
		}
		Vector3 boxSize = m_componentCreature.ComponentBody.BoxSize;
		float num = MathUtils.DegToRad(3.2f) * MathUtils.Sin(1.5f * t);
		float num2 = 1f * boxSize.Y;
		float num3 = 1.3f * (0.12f * boxSize.X);
		float z = -0.06f * boxSize.Z;
		float num4 = MathUtils.DegToRad(26f);
		float num5 = MathUtils.DegToRad(-42f);
		float num6 = MathUtils.DegToRad(12f);
		float num7 = 0.5f * boxSize.Y;
		if (m_neckBone != null)
		{
			Matrix matrix = Matrix.Identity;
			Matrix? boneTransform = GetBoneTransform(m_neckBone.Index);
			if (boneTransform.HasValue)
			{
				matrix = boneTransform.Value;
			}
			matrix = Matrix.CreateTranslation(0f, num2 + 0.1f * boxSize.Y, 0f) * Matrix.CreateTranslation(0f, 0f - num7, 0f) * Matrix.CreateFromYawPitchRoll(0f, MathUtils.DegToRad(-35f) + 0.4f * num, 0f) * Matrix.CreateTranslation(0f, num7, 0f) * matrix;
			SetBoneTransform(m_neckBone.Index, matrix);
		}
		if (m_leg1Bone != null)
		{
			Matrix matrix2 = Matrix.Identity;
			Matrix? boneTransform2 = GetBoneTransform(m_leg1Bone.Index);
			if (boneTransform2.HasValue)
			{
				matrix2 = boneTransform2.Value;
			}
			matrix2 = Matrix.CreateTranslation(num3, num2, z) * Matrix.CreateTranslation(0f, 0f - num7, 0f) * Matrix.CreateFromYawPitchRoll(num4, num5 + 0.5f * num, 0f - num6) * Matrix.CreateTranslation(0f, num7, 0f) * matrix2;
			SetBoneTransform(m_leg1Bone.Index, matrix2);
		}
		if (m_leg2Bone != null)
		{
			Matrix matrix3 = Matrix.Identity;
			Matrix? boneTransform3 = GetBoneTransform(m_leg2Bone.Index);
			if (boneTransform3.HasValue)
			{
				matrix3 = boneTransform3.Value;
			}
			matrix3 = Matrix.CreateTranslation(0f - num3, num2, z) * Matrix.CreateTranslation(0f, 0f - num7, 0f) * Matrix.CreateFromYawPitchRoll(0f - num4, num5 - 0.5f * num, num6) * Matrix.CreateTranslation(0f, num7, 0f) * matrix3;
			SetBoneTransform(m_leg2Bone.Index, matrix3);
		}
		if (m_headBone != null && m_neckBone == null)
		{
			Matrix matrix4 = Matrix.Identity;
			Matrix? boneTransform4 = GetBoneTransform(m_headBone.Index);
			if (boneTransform4.HasValue)
			{
				matrix4 = boneTransform4.Value;
			}
			matrix4 = Matrix.CreateTranslation(0f, num2 * 0.9f, 0f) * Matrix.CreateFromYawPitchRoll(0f, MathUtils.DegToRad(-22f) + 0.3f * num, 0f) * matrix4;
			SetBoneTransform(m_headBone.Index, matrix4);
		}
	}
}
