using System;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentSimpleModel : ComponentModel
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public ComponentSpawn m_componentSpawn;

	public SubsystemTime m_subsystemTime;

	public ComponentBody m_componentBody;

	public string PaddleMeshName;

	public bool ShowPaddle;

	public float PaddleMaxYaw;

	public float PaddleMaxPitch;

	public float PaddlePhasePerSpeed;

	public float PaddleSpeedThreshold;

	public Vector3 PaddleHingeOffset;

	private int m_paddleIndex;

	private ModelBone m_paddleBone;

	private float m_paddlePhase;

	private float m_paddleActivity;

	public float PaddleMidLift = 0.65f;

	public override void Animate()
	{
		if (m_componentSpawn != null)
		{
			Opacity = ((m_componentSpawn.SpawnDuration > 0f) ? ((float)MathUtils.Saturate((m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.SpawnTime) / (double)m_componentSpawn.SpawnDuration)) : 1f);
			if (m_componentSpawn.DespawnTime.HasValue)
			{
				Opacity = MathUtils.Min(Opacity.Value, (float)MathUtils.Saturate(1.0 - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.DespawnTime.Value) / (double)m_componentSpawn.DespawnDuration));
			}
		}
		SetBoneTransform(Model.RootBone.Index, m_componentFrame.Matrix);
		if (ShowPaddle && m_paddleBone != null && Model != null && m_paddleBone != Model.RootBone && m_subsystemTime != null)
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			float num = 0f;
			if (m_componentBody != null)
			{
				num = Vector3.Dot(m_componentBody.Velocity, m_componentBody.Matrix.Forward);
			}
			float num2 = MathUtils.Abs(num);
			float num3 = ((num2 > PaddleSpeedThreshold) ? 1f : 0f);
			float num4 = MathUtils.Min(8f * gameTimeDelta, 1f);
			m_paddleActivity += num4 * (num3 - m_paddleActivity);
			m_paddlePhase = MathUtils.Remainder(m_paddlePhase + num2 * PaddlePhasePerSpeed * gameTimeDelta, (float)Math.PI * 2f);
			float num5 = MathUtils.Sin(m_paddlePhase);
			float num6 = MathUtils.Cos(m_paddlePhase);
			bool flag = num > PaddleSpeedThreshold;
			bool flag2 = num < 0f - PaddleSpeedThreshold;
			bool flag3 = num6 > 0f;
			if (flag2)
			{
				flag3 = !flag3;
			}
			if (!flag && !flag2)
			{
				flag3 = false;
			}
			float radians = num5 * PaddleMaxYaw * m_paddleActivity;
			float num7 = (0f - MathUtils.Abs(num5)) * PaddleMaxPitch * m_paddleActivity;
			float num8 = (flag3 ? ((1f - MathUtils.Abs(num5)) * PaddleMidLift * m_paddleActivity) : 0f);
			float radians2 = num7 + num8;
			Matrix matrix = Matrix.CreateRotationY(radians) * Matrix.CreateRotationZ(radians2);
			Matrix value = ((PaddleHingeOffset != Vector3.Zero) ? (Matrix.CreateTranslation(-PaddleHingeOffset) * matrix * Matrix.CreateTranslation(PaddleHingeOffset)) : matrix);
			SetBoneTransform(m_paddleBone.Index, value);
		}
		base.Animate();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_componentSpawn = Entity.FindComponent<ComponentSpawn>();
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: false);
		m_componentBody = Entity.FindComponent<ComponentBody>(throwOnError: false);
		PaddleMeshName = valuesDictionary.GetValue("PaddleMeshName", PaddleMeshName);
		ShowPaddle = valuesDictionary.GetValue("ShowPaddle", ShowPaddle);
		PaddleMaxYaw = valuesDictionary.GetValue("PaddleMaxYaw", PaddleMaxYaw);
		PaddleMaxPitch = valuesDictionary.GetValue("PaddleMaxPitch", PaddleMaxPitch);
		PaddlePhasePerSpeed = valuesDictionary.GetValue("PaddlePhasePerSpeed", PaddlePhasePerSpeed);
		PaddleHingeOffset = valuesDictionary.GetValue("PaddleHingeOffset", PaddleHingeOffset);
		PaddleSpeedThreshold = valuesDictionary.GetValue("PaddleSpeedThreshold", PaddleSpeedThreshold);
		PaddleMidLift = valuesDictionary.GetValue("PaddleMidLift", PaddleMidLift);
		base.Load(valuesDictionary, idToEntityMap);
	}

	public ComponentSimpleModel()
	{
		PaddleMeshName = "Paddle";
		ShowPaddle = true;
		PaddleMaxYaw = 0.6f;
		PaddleMaxPitch = 0.35f;
		PaddlePhasePerSpeed = 2f;
		PaddleSpeedThreshold = 0.05f;
		PaddleHingeOffset = Vector3.Zero;
		m_paddleIndex = -1;
	}

	private void ResolvePaddle()
	{
		m_paddleIndex = -1;
		m_paddleBone = null;
		if (Model != null && Model.Meshes.Count != 0)
		{
			m_paddleIndex = FindMeshIndex(PaddleMeshName, "Paddle", "Oar");
			if (m_paddleIndex >= 0)
			{
				m_paddleBone = Model.Meshes[m_paddleIndex].ParentBone;
			}
			if (m_paddleBone == null || m_paddleBone == Model.RootBone)
			{
				m_paddleBone = Model.FindBone("Paddle", throwIfNotFound: false);
			}
		}
	}

	private int FindMeshIndex(string primaryName, params string[] fallbacks)
	{
		if (Model == null)
		{
			return -1;
		}
		for (int i = 0; i < Model.Meshes.Count; i++)
		{
			if (string.Equals(Model.Meshes[i].Name, primaryName, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		foreach (string value in fallbacks)
		{
			for (int k = 0; k < Model.Meshes.Count; k++)
			{
				if ((Model.Meshes[k].Name ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return k;
				}
			}
		}
		return -1;
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		ResolvePaddle();
	}

	public float GetPaddlePhase()
	{
		return m_paddlePhase;
	}

	public float GetPaddleActivity()
	{
		return m_paddleActivity;
	}
}
