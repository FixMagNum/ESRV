using System;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBirdModel : ComponentCreatureModel
{
	private bool m_hasWings;

	private ModelBone m_bodyBone;

	private ModelBone m_neckBone;

	private ModelBone m_headBone;

	private ModelBone m_leg1Bone;

	private ModelBone m_leg2Bone;

	private ModelBone m_wing1Bone;

	private ModelBone m_wing2Bone;

	private float m_flyAnimationSpeed;

	private float m_walkAnimationSpeed;

	private float m_peckAnimationSpeed;

	private float m_walkBobHeight;

	private float m_peckPhase;

	public float m_fallSpinPhase;

	public float m_fallPitchPhase;

	private float m_relaxLieFactor;

	public float FlyPhase { get; set; }

	public bool RelaxLieOrder { get; set; }

	public override void Update(float dt)
	{
		float num = Vector3.Dot(m_componentCreature.ComponentBody.Velocity, m_componentCreature.ComponentBody.Matrix.Forward);
		if (MathUtils.Abs(num) > 0.1f)
		{
			MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
		}
		else
		{
			float num2 = MathUtils.Floor(MovementAnimationPhase);
			m_fallSpinPhase = 0f;
			m_fallPitchPhase = 0f;
			if (MovementAnimationPhase != num2)
			{
				if (MovementAnimationPhase - num2 > 0.5f)
				{
					MovementAnimationPhase = MathUtils.Min(MovementAnimationPhase + 2f * dt, num2 + 1f);
				}
				else
				{
					MovementAnimationPhase = MathUtils.Max(MovementAnimationPhase - 2f * dt, num2);
				}
			}
		}
		float num3 = (0f - m_walkBobHeight) * MathUtils.Sqr(MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase));
		float num4 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
		Bob += num4 * (num3 - Bob);
		if (m_hasWings)
		{
			if (m_componentCreature.ComponentLocomotion.LastFlyOrder.HasValue)
			{
				float num5 = ((m_componentCreature.ComponentLocomotion.LastFlyOrder.Value.LengthSquared() > 0.99f) ? 1.5f : 1f);
				FlyPhase = MathUtils.Remainder(FlyPhase + m_flyAnimationSpeed * num5 * dt, 1f);
				if (m_componentCreature.ComponentLocomotion.LastFlyOrder.Value.Y < -0.1f && m_componentCreature.ComponentBody.Velocity.Length() > 4f)
				{
					FlyPhase = 0.72f;
				}
			}
			else if (FlyPhase != 1f)
			{
				FlyPhase = MathUtils.Min(FlyPhase + m_flyAnimationSpeed * dt, 1f);
			}
		}
		if (FeedOrder)
		{
			m_peckPhase += m_peckAnimationSpeed * dt;
			if (m_peckPhase > 0.75f)
			{
				m_peckPhase -= 0.5f;
			}
		}
		else if (m_peckPhase != 0f)
		{
			m_peckPhase = MathUtils.Remainder(MathUtils.Min(m_peckPhase + m_peckAnimationSpeed * dt, 1f), 1f);
		}
		FeedOrder = false;
		m_relaxLieFactor = MathUtils.Clamp(m_relaxLieFactor + (RelaxLieOrder ? 1f : (-1f)) * 2.5f * dt, 0f, 1f);
		if (m_relaxLieFactor > 0.01f)
		{
			MovementAnimationPhase = 0f;
			if (m_hasWings)
			{
				FlyPhase = 1f;
			}
		}
		base.Update(dt);
	}

	public override void Animate()
	{
		float num = 0f;
		if (m_hasWings)
		{
			num += 1.2f * MathUtils.Sin((float)Math.PI * 2f * (FlyPhase + 0.75f));
			if (m_componentCreature.ComponentBody.StandingOnValue.HasValue)
			{
				num += 0.3f * MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
			}
		}
		float num2;
		float num3;
		if (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f || m_componentCreature.ComponentLocomotion.FlySpeed == 0f)
		{
			num2 = 0.6f * MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase);
			num3 = 0f - num2;
		}
		else
		{
			num2 = (num3 = 0f - MathUtils.DegToRad(60f));
		}
		Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
		if (m_componentCreature.ComponentHealth.Health > 0f)
		{
			float x = m_componentCreature.ComponentLocomotion.LookAngles.X / 2f;
			float x2 = m_componentCreature.ComponentLocomotion.LookAngles.X / 2f;
			float num4 = 0f;
			float num5 = 0f;
			if (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0f)
			{
				num4 = 0.5f * MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase / 2f);
				num5 = 0f - num4;
			}
			float num6 = MathUtils.Cos((float)Math.PI * 2f * m_peckPhase);
			num4 -= 1.25f * (1f - ((num6 >= 0f) ? num6 : (-0.5f * num6)));
			num4 += m_componentCreature.ComponentLocomotion.LookAngles.Y;
			float relaxLieFactor = m_relaxLieFactor;
			float x3 = MathUtils.DegToRad(85f);
			float num7 = MathUtils.DegToRad(13f);
			float num8 = MathUtils.DegToRad(-35f);
			float num9 = MathUtils.DegToRad(25f) * relaxLieFactor;
			float pitch = MathUtils.Lerp(num2, x3, relaxLieFactor);
			float pitch2 = MathUtils.Lerp(num3, x3, relaxLieFactor);
			Vector2 vector2 = new Vector2(x2, num4 + num8 * relaxLieFactor);
			Vector2 vector3 = new Vector2(x, num5 + MathUtils.Clamp(vector.Y, -(float)Math.PI / 4f, (float)Math.PI / 4f) + num8 * relaxLieFactor);
			Matrix value = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			value *= Matrix.CreateRotationX(num7 * relaxLieFactor);
			float num10 = 0.4f * m_componentCreature.ComponentBody.BoxSize.Y * relaxLieFactor;
			value *= Matrix.CreateFromYawPitchRoll(vector.X, 0f, 0f);
			value *= Matrix.CreateTranslation(m_componentCreature.ComponentBody.Position + new Vector3(0f, Bob - num10, 0f));
			SetBoneTransform(m_bodyBone.Index, value);
			SetBoneTransform(m_neckBone.Index, Matrix.CreateFromYawPitchRoll(vector2.X, vector2.Y, 0f));
			SetBoneTransform(m_headBone.Index, Matrix.CreateFromYawPitchRoll(vector3.X, vector3.Y, vector.Z));
			if (m_hasWings)
			{
				float num11 = ((relaxLieFactor > 0f) ? 0f : num);
				SetBoneTransform(m_wing1Bone.Index, Matrix.CreateRotationY(num11));
				SetBoneTransform(m_wing2Bone.Index, Matrix.CreateRotationY(0f - num11));
			}
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateFromYawPitchRoll(0f - num9, pitch, 0f));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateFromYawPitchRoll(num9, pitch2, 0f));
		}
		else
		{
			float num12 = 1f - DeathPhase;
			float num13 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
			Vector3 position = m_componentCreature.ComponentBody.Position + 0.5f * num13 * Vector3.Normalize(m_componentCreature.ComponentBody.Matrix.Forward * new Vector3(1f, 0f, 1f));
			Matrix value2 = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			value2 *= Matrix.CreateFromYawPitchRoll(vector.X, (float)Math.PI / 2f * DeathPhase, 0f);
			value2 *= Matrix.CreateTranslation(position);
			SetBoneTransform(m_bodyBone.Index, value2);
			SetBoneTransform(m_neckBone.Index, Matrix.Identity);
			SetBoneTransform(m_headBone.Index, Matrix.Identity);
			if (m_hasWings)
			{
				SetBoneTransform(m_wing1Bone.Index, Matrix.CreateRotationY(num * num12));
				SetBoneTransform(m_wing2Bone.Index, Matrix.CreateRotationY((0f - num) * num12));
			}
			SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(num2 * num12));
			SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationX(num3 * num12));
		}
		base.Animate();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_flyAnimationSpeed = valuesDictionary.GetValue<float>("FlyAnimationSpeed");
		m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
		m_peckAnimationSpeed = valuesDictionary.GetValue<float>("PeckAnimationSpeed");
		m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		if (Model != null)
		{
			m_bodyBone = Model.FindBone("Body");
			m_neckBone = Model.FindBone("Neck");
			m_headBone = Model.FindBone("Head");
			m_leg1Bone = Model.FindBone("Leg1");
			m_leg2Bone = Model.FindBone("Leg2");
			m_wing1Bone = Model.FindBone("Wing1", throwIfNotFound: false);
			m_wing2Bone = Model.FindBone("Wing2", throwIfNotFound: false);
		}
		else
		{
			m_bodyBone = null;
			m_neckBone = null;
			m_headBone = null;
			m_leg1Bone = null;
			m_leg2Bone = null;
			m_wing1Bone = null;
			m_wing2Bone = null;
		}
		m_hasWings = m_wing1Bone != null && m_wing2Bone != null;
	}

	public override void EmbraceMutate(float t)
	{
		if (m_bodyBone == null)
		{
			return;
		}
		Vector3 boxSize = m_componentCreature.ComponentBody.BoxSize;
		float num = MathUtils.DegToRad(3f) * MathUtils.Sin(1.7f * t);
		if (m_neckBone != null)
		{
			Matrix matrix = Matrix.Identity;
			Matrix? boneTransform = GetBoneTransform(m_neckBone.Index);
			if (boneTransform.HasValue)
			{
				matrix = boneTransform.Value;
			}
			float y = 1.05f * boxSize.Y;
			float num2 = 0.5f * boxSize.Y;
			matrix = Matrix.CreateTranslation(0f, y, 0f) * Matrix.CreateTranslation(0f, 0f - num2, 0f) * Matrix.CreateFromYawPitchRoll(0f, MathUtils.DegToRad(-35f) + 0.5f * num, 0f) * Matrix.CreateTranslation(0f, num2, 0f) * matrix;
			SetBoneTransform(m_neckBone.Index, matrix);
		}
		if (m_headBone != null)
		{
			Matrix matrix2 = Matrix.Identity;
			Matrix? boneTransform2 = GetBoneTransform(m_headBone.Index);
			if (boneTransform2.HasValue)
			{
				matrix2 = boneTransform2.Value;
			}
			float y2 = ((m_neckBone != null) ? (0.55f * boxSize.Y) : (1f * boxSize.Y));
			matrix2 = Matrix.CreateTranslation(0f, y2, 0f) * Matrix.CreateFromYawPitchRoll(0f, MathUtils.DegToRad(-20f) + 0.3f * num, 0f) * matrix2;
			SetBoneTransform(m_headBone.Index, matrix2);
		}
		float y3 = 0.95f * boxSize.Y;
		float num3 = 1.3f * (0.12f * boxSize.X);
		float z = -0.06f * boxSize.Z;
		float num4 = MathUtils.DegToRad(28f);
		float num5 = MathUtils.DegToRad(-40f);
		float num6 = MathUtils.DegToRad(12f);
		float num7 = 0.5f * boxSize.Y;
		if (m_leg1Bone != null)
		{
			Matrix matrix3 = Matrix.Identity;
			Matrix? boneTransform3 = GetBoneTransform(m_leg1Bone.Index);
			if (boneTransform3.HasValue)
			{
				matrix3 = boneTransform3.Value;
			}
			matrix3 = Matrix.CreateTranslation(num3, y3, z) * Matrix.CreateTranslation(0f, 0f - num7, 0f) * Matrix.CreateFromYawPitchRoll(num4, num5 + 0.5f * num, 0f - num6) * Matrix.CreateTranslation(0f, num7, 0f) * matrix3;
			SetBoneTransform(m_leg1Bone.Index, matrix3);
		}
		if (m_leg2Bone != null)
		{
			Matrix matrix4 = Matrix.Identity;
			Matrix? boneTransform4 = GetBoneTransform(m_leg2Bone.Index);
			if (boneTransform4.HasValue)
			{
				matrix4 = boneTransform4.Value;
			}
			matrix4 = Matrix.CreateTranslation(0f - num3, y3, z) * Matrix.CreateTranslation(0f, 0f - num7, 0f) * Matrix.CreateFromYawPitchRoll(0f - num4, num5 - 0.5f * num, num6) * Matrix.CreateTranslation(0f, num7, 0f) * matrix4;
			SetBoneTransform(m_leg2Bone.Index, matrix4);
		}
		if (!m_hasWings)
		{
			return;
		}
		float num8 = 1.3f * (0.15f * boxSize.X);
		float z2 = 0.08f * boxSize.Z;
		float num9 = MathUtils.DegToRad(25f);
		float num10 = MathUtils.DegToRad(-50f);
		float num11 = MathUtils.DegToRad(10f);
		if (m_wing1Bone != null)
		{
			Matrix matrix5 = Matrix.Identity;
			Matrix? boneTransform5 = GetBoneTransform(m_wing1Bone.Index);
			if (boneTransform5.HasValue)
			{
				matrix5 = boneTransform5.Value;
			}
			matrix5 = Matrix.CreateTranslation(num8, 0f, z2) * Matrix.CreateFromYawPitchRoll(num9, num10 + num, 0f - num11) * matrix5;
			SetBoneTransform(m_wing1Bone.Index, matrix5);
		}
		if (m_wing2Bone != null)
		{
			Matrix matrix6 = Matrix.Identity;
			Matrix? boneTransform6 = GetBoneTransform(m_wing2Bone.Index);
			if (boneTransform6.HasValue)
			{
				matrix6 = boneTransform6.Value;
			}
			matrix6 = Matrix.CreateTranslation(0f - num8, 0f, z2) * Matrix.CreateFromYawPitchRoll(0f - num9, num10 - num, num11) * matrix6;
			SetBoneTransform(m_wing2Bone.Index, matrix6);
		}
	}
}
