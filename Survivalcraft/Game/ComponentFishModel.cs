using System;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFishModel : ComponentCreatureModel
{
	public ModelBone m_bodyBone;

	public ModelBone m_tail1Bone;

	public ModelBone m_tail2Bone;

	public ModelBone m_jawBone;

	public float m_swimAnimationSpeed;

	public bool m_hasVerticalTail;

	public float m_bitingPhase;

	public float m_tailWagPhase;

	public Vector2 m_tailTurn;

	public float m_digInDepth;

	public float m_digInTailPhase;

	public float? BendOrder;

	public float DigInOrder;

	public float m_fallSpinPhase;

	public float m_fallPitchPhase;

	public override void Update(float dt)
	{
		if (m_componentCreature.ComponentLocomotion.LastSwimOrder.HasValue && m_componentCreature.ComponentLocomotion.LastSwimOrder.Value != Vector3.Zero)
		{
			float num = ((m_componentCreature.ComponentLocomotion.LastSwimOrder.Value.LengthSquared() > 0.99f) ? 1.75f : 1f);
			MovementAnimationPhase = MathUtils.Remainder(MovementAnimationPhase + m_swimAnimationSpeed * num * dt, 1000f);
		}
		else
		{
			MovementAnimationPhase = MathUtils.Remainder(MovementAnimationPhase + 0.15f * m_swimAnimationSpeed * dt, 1000f);
		}
		if (BendOrder.HasValue)
		{
			if (m_hasVerticalTail)
			{
				m_tailTurn.X = 0f;
				m_tailTurn.Y = BendOrder.Value;
			}
			else
			{
				m_tailTurn.X = BendOrder.Value;
				m_tailTurn.Y = 0f;
			}
		}
		else
		{
			m_tailTurn.X += MathUtils.Saturate(2f * m_componentCreature.ComponentLocomotion.TurnSpeed * dt) * (0f - m_componentCreature.ComponentLocomotion.LastTurnOrder.X - m_tailTurn.X);
		}
		if (DigInOrder > m_digInDepth)
		{
			float num2 = (DigInOrder - m_digInDepth) * MathUtils.Min(1.5f * dt, 1f);
			m_digInDepth += num2;
			m_digInTailPhase += 20f * num2;
		}
		else if (DigInOrder < m_digInDepth)
		{
			m_digInDepth += (DigInOrder - m_digInDepth) * MathUtils.Min(5f * dt, 1f);
		}
		if (m_componentCreature.ComponentBody.ImmersionFactor < 0.05f && m_componentCreature.ComponentBody.Velocity.Y < -1f)
		{
			m_fallSpinPhase = MathUtils.Remainder(m_fallSpinPhase + 6f * dt, (float)Math.PI * 2f);
			m_fallPitchPhase = MathUtils.Remainder(m_fallPitchPhase + 4f * dt, (float)Math.PI * 2f);
		}
		else
		{
			m_fallSpinPhase = 0f;
			m_fallPitchPhase = 0f;
		}
		float num3 = 0.33f * m_componentCreature.ComponentLocomotion.TurnSpeed;
		float child = 1f * m_componentCreature.ComponentLocomotion.TurnSpeed;
		IsAttackHitMoment = false;
		if (AttackOrder || FeedOrder)
		{
			if (AttackOrder)
			{
				m_tailWagPhase = MathUtils.Remainder(m_tailWagPhase + num3 * dt, 1f);
			}
			float bitingPhase = m_bitingPhase;
			m_bitingPhase = MathUtils.Remainder(m_bitingPhase + child * dt, 1f);
			if (AttackOrder && bitingPhase < 0.5f && m_bitingPhase >= 0.5f)
			{
				IsAttackHitMoment = true;
			}
		}
		else
		{
			if (m_tailWagPhase != 0f)
			{
				m_tailWagPhase = MathUtils.Remainder(MathUtils.Min(m_tailWagPhase + num3 * dt, 1f), 1f);
			}
			if (m_bitingPhase != 0f)
			{
				m_bitingPhase = MathUtils.Remainder(MathUtils.Min(m_bitingPhase + child * dt, 1f), 1f);
			}
		}
		LastAttackOrder = AttackOrder;
		LastFeedOrder = FeedOrder;
		AttackOrder = false;
		FeedOrder = false;
		BendOrder = null;
		DigInOrder = 0f;
		base.Update(dt);
	}

	public override void Animate()
	{
		Vector3 num = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
		if (m_componentCreature.ComponentHealth.Health > 0f)
		{
			float child = m_digInTailPhase + m_tailWagPhase;
			float num2;
			float num3;
			float num4;
			float num5;
			if (m_hasVerticalTail)
			{
				num2 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * child) - m_tailTurn.X, -1f, 1f);
				num3 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0.5f * MathUtils.Sin(2f * ((float)Math.PI * MathUtils.Max(child - 0.25f, 0f))) - m_tailTurn.X, -1f, 1f);
				num4 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * MovementAnimationPhase) - m_tailTurn.Y, -1f, 1f);
				num5 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * MathUtils.Max(MovementAnimationPhase - 0.25f, 0f)) - m_tailTurn.Y, -1f, 1f);
			}
			else
			{
				num2 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * (MovementAnimationPhase + child)) - m_tailTurn.X, -1f, 1f);
				num3 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0.5f * MathUtils.Sin(2f * ((float)Math.PI * MathUtils.Max(MovementAnimationPhase + child - 0.25f, 0f))) - m_tailTurn.X, -1f, 1f);
				num4 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0f - m_tailTurn.Y, -1f, 1f);
				num5 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0f - m_tailTurn.Y, -1f, 1f);
			}
			float radians = 0f;
			if (m_bitingPhase > 0f)
			{
				radians = (0f - MathUtils.DegToRad(30f)) * MathUtils.Sin((float)Math.PI * m_bitingPhase);
			}
			Matrix matrix = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			if (m_fallSpinPhase != 0f || m_fallPitchPhase != 0f)
			{
				matrix *= Matrix.CreateRotationZ(m_fallSpinPhase);
				matrix *= Matrix.CreateRotationX(m_fallPitchPhase);
			}
			Matrix value = matrix * Matrix.CreateFromYawPitchRoll(num.X, 0f, 0f) * Matrix.CreateTranslation(m_componentCreature.ComponentBody.Position + new Vector3(0f, 0f - m_digInDepth, 0f));
			SetBoneTransform(m_bodyBone.Index, value);
			Matrix identity = Matrix.Identity;
			if (num2 != 0f)
			{
				identity *= Matrix.CreateRotationZ(num2);
			}
			if (num4 != 0f)
			{
				identity *= Matrix.CreateRotationX(num4);
			}
			Matrix identity2 = Matrix.Identity;
			if (num3 != 0f)
			{
				identity2 *= Matrix.CreateRotationZ(num3);
			}
			if (num5 != 0f)
			{
				identity2 *= Matrix.CreateRotationX(num5);
			}
			SetBoneTransform(m_tail1Bone.Index, identity);
			SetBoneTransform(m_tail2Bone.Index, identity2);
			if (m_jawBone != null)
			{
				SetBoneTransform(m_jawBone.Index, Matrix.CreateRotationX(radians));
			}
		}
		else
		{
			float num6 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
			Vector3 position = m_componentCreature.ComponentBody.Position + 1f * num6 * DeathPhase * Vector3.UnitY;
			Matrix value2 = Matrix.CreateScale(m_componentCreature.ComponentBody.Size);
			if (m_fallSpinPhase != 0f || m_fallPitchPhase != 0f)
			{
				value2 *= Matrix.CreateRotationZ(m_fallSpinPhase);
				value2 *= Matrix.CreateRotationX(m_fallPitchPhase);
			}
			value2 *= Matrix.CreateFromYawPitchRoll(num.X, 0f, (float)Math.PI * DeathPhase);
			value2 *= Matrix.CreateTranslation(position);
			SetBoneTransform(m_bodyBone.Index, value2);
			SetBoneTransform(m_bodyBone.Index, value2);
			SetBoneTransform(m_tail1Bone.Index, Matrix.Identity);
			SetBoneTransform(m_tail2Bone.Index, Matrix.Identity);
			if (m_jawBone != null)
			{
				SetBoneTransform(m_jawBone.Index, Matrix.Identity);
			}
		}
		base.Animate();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_hasVerticalTail = valuesDictionary.GetValue<bool>("HasVerticalTail");
		m_swimAnimationSpeed = valuesDictionary.GetValue<float>("SwimAnimationSpeed");
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		if (Model != null)
		{
			m_bodyBone = Model.FindBone("Body");
			m_tail1Bone = Model.FindBone("Tail1");
			m_tail2Bone = Model.FindBone("Tail2");
			m_jawBone = Model.FindBone("Jaw", throwIfNotFound: false);
		}
		else
		{
			m_bodyBone = null;
			m_tail1Bone = null;
			m_tail2Bone = null;
			m_jawBone = null;
		}
	}

	public override Vector3 CalculateEyePosition()
	{
		Matrix vector = m_componentCreature.ComponentBody.Matrix;
		return m_componentCreature.ComponentBody.Position + vector.Up * 1f * m_componentCreature.ComponentBody.BoxSize.Y + vector.Forward * 0.45f * m_componentCreature.ComponentBody.BoxSize.Z;
	}

	public override void EmbraceMutate(float t)
	{
		if (m_bodyBone == null)
		{
			return;
		}
		Vector3 boxSize = m_componentCreature.ComponentBody.BoxSize;
		Matrix matrix = Matrix.Identity;
		Matrix? boneTransform = GetBoneTransform(m_bodyBone.Index);
		if (boneTransform.HasValue)
		{
			matrix = boneTransform.Value;
		}
		Vector3 vector = new Vector3(matrix.M41, matrix.M42, matrix.M43);
		matrix.M41 = 0f;
		matrix.M42 = 0f;
		matrix.M43 = 0f;
		float pitch = MathUtils.DegToRad(10f) * MathUtils.Sin(1.1f * t);
		matrix = Matrix.CreateFromYawPitchRoll(0f, pitch, 0f) * matrix;
		matrix.M41 += vector.X;
		matrix.M42 += vector.Y + 0.8f * boxSize.Y;
		matrix.M43 += vector.Z;
		SetBoneTransform(m_bodyBone.Index, matrix);
		float num = MathUtils.DegToRad(40f) * (0.7f + 0.3f * MathUtils.Sin(1.6f * t));
		float yaw = 1.35f * num;
		float roll = MathUtils.DegToRad(6f) * MathUtils.Sin(2f * t);
		float num2 = 0.5f * boxSize.Y;
		if (m_tail1Bone != null)
		{
			Matrix matrix2 = Matrix.Identity;
			Matrix? boneTransform2 = GetBoneTransform(m_tail1Bone.Index);
			if (boneTransform2.HasValue)
			{
				matrix2 = boneTransform2.Value;
			}
			matrix2 = Matrix.CreateTranslation(0f, 0f - num2, 0f) * Matrix.CreateFromYawPitchRoll(num, 0f, roll) * Matrix.CreateTranslation(0f, num2, 0f) * matrix2;
			SetBoneTransform(m_tail1Bone.Index, matrix2);
		}
		if (m_tail2Bone != null)
		{
			Matrix matrix3 = Matrix.Identity;
			Matrix? boneTransform3 = GetBoneTransform(m_tail2Bone.Index);
			if (boneTransform3.HasValue)
			{
				matrix3 = boneTransform3.Value;
			}
			matrix3 = Matrix.CreateTranslation(0f, 0f - num2, 0f) * Matrix.CreateFromYawPitchRoll(yaw, 0f, 0f) * Matrix.CreateTranslation(0f, num2, 0f) * matrix3;
			SetBoneTransform(m_tail2Bone.Index, matrix3);
		}
		if (m_jawBone != null)
		{
			Matrix matrix4 = Matrix.Identity;
			Matrix? boneTransform4 = GetBoneTransform(m_jawBone.Index);
			if (boneTransform4.HasValue)
			{
				matrix4 = boneTransform4.Value;
			}
			matrix4 = Matrix.CreateRotationX(MathUtils.DegToRad(8f) * MathUtils.Sin(1.8f * t)) * matrix4;
			SetBoneTransform(m_jawBone.Index, matrix4);
		}
	}
}
