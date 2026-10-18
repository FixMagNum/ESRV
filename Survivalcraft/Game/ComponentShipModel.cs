using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentShipModel : ComponentSimpleModel, IDrawable
{
	public enum SailMode
	{
		Rolled,
		Half,
		Full
	}

	public string HullMeshName;

	public string MastMeshName;

	public string SailRolledMeshName;

	public string SailHalfMeshName;

	public string SailFullMeshName;

	public bool ShowMast;

	public bool ShowSailRolled;

	public bool ShowSailHalf;

	public bool ShowSailFull;

	private int m_hullIndex;

	private int m_mastIndex;

	private int m_sailRolledIndex;

	private int m_sailHalfIndex;

	private int m_sailFullIndex;

	private int m_lastFlagsHash;

	public float MastYawOffset;

	public float MastTurnSpeedMin;

	public float MastTurnSpeedPerStrength;

	private float m_currentMastYaw;

	public new SubsystemGameInfo m_subsystemGameInfo;

	private new SubsystemTime m_subsystemTime;

	private new SubsystemWeather m_subsystemWeather;

	private ModelBone m_rootBone;

	private ModelBone m_hullBone;

	private ModelBone m_mastBone;

	private ModelBone m_sailRolledBone;

	private ModelBone m_sailHalfBone;

	private ModelBone m_sailFullBone;

	private readonly PrimitivesRenderer3D m_linesRenderer;

	private readonly Vector3[] m_hullOffsets;

	private readonly Vector3[] m_ropeOffsets;

	private readonly float[] m_ropeHeights;

	private readonly Vector3[] m_stayHullOffsets;

	private readonly float[] m_stayMastSideOffsets;

	private readonly float[] m_stayMastHeights;

	private readonly Vector3[] m_sheetHullOffsets;

	private readonly float[] m_sheetMastSideOffsets;

	private readonly float[] m_sheetMastHeights;

	public int[] DrawOrders => new int[1];

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		HullMeshName = valuesDictionary.GetValue("HullMeshName", HullMeshName);
		MastMeshName = valuesDictionary.GetValue("MastMeshName", MastMeshName);
		SailRolledMeshName = valuesDictionary.GetValue("SailRolledMeshName", SailRolledMeshName);
		SailHalfMeshName = valuesDictionary.GetValue("SailHalfMeshName", SailHalfMeshName);
		SailFullMeshName = valuesDictionary.GetValue("SailFullMeshName", SailFullMeshName);
		int value = valuesDictionary.GetValue("SailMode", -1);
		if (value >= 0 && value <= 2)
		{
			ApplySailVisibility((SailMode)value, rebuildOrder: false);
		}
		else
		{
			ApplySailVisibility(SailMode.Rolled, rebuildOrder: false);
		}
		m_currentMastYaw = valuesDictionary.GetValue("MastYaw", m_currentMastYaw);
		MastYawOffset = valuesDictionary.GetValue("MastYawOffset", MastYawOffset);
		MastTurnSpeedMin = valuesDictionary.GetValue("MastTurnSpeedMin", MastTurnSpeedMin);
		MastTurnSpeedPerStrength = valuesDictionary.GetValue("MastTurnSpeedPerStrength", MastTurnSpeedPerStrength);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		ResolveMeshIndices();
		ResolveBones();
		RebuildVisibleMeshOrder(force: true);
	}

	public override void SetModel(Model model)
	{
		base.SetModel(model);
		ResolveMeshIndices();
		ResolveBones();
		RebuildVisibleMeshOrder(force: true);
	}

	public override void Animate()
	{
		RebuildVisibleMeshOrderIfNeeded();
		if (m_componentSpawn != null)
		{
			Opacity = ((m_componentSpawn.SpawnDuration > 0f) ? ((float)MathUtils.Saturate((m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.SpawnTime) / (double)m_componentSpawn.SpawnDuration)) : 1f);
			if (m_componentSpawn.DespawnTime.HasValue)
			{
				Opacity = MathUtils.Min(Opacity.Value, (float)MathUtils.Saturate(1.0 - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.DespawnTime.Value) / (double)m_componentSpawn.DespawnDuration));
			}
		}
		if (Model != null && Model.RootBone != null)
		{
			SetBoneTransform(Model.RootBone.Index, m_componentFrame.Matrix);
		}
		float num;
		if (ShowSailFull)
		{
			num = 1f;
		}
		else
		{
			num = (ShowSailHalf ? 0.5f : 0f);
		}
		Matrix matrix = Matrix.CreateRotationY(m_currentMastYaw);
		Matrix matrix2 = matrix * Matrix.CreateScale(0.0001f);
		if (Model != null)
		{
			if (m_subsystemWeather != null)
			{
				if (num > 0f)
				{
					Vector3 forward = m_componentFrame.Matrix.Forward;
					float num2 = MathUtils.Atan2(forward.X, forward.Z);
					Vector2 currentWindVector = m_subsystemWeather.CurrentWindVector;
					Vector3 vector = new Vector3(currentWindVector.X, 0f, currentWindVector.Y);
					float target = WrapAngle(MathUtils.Atan2(vector.X, vector.Z) - num2 + MastYawOffset);
					float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
					float num3 = (MastTurnSpeedMin + MastTurnSpeedPerStrength * MathUtils.Saturate(currentWindStrength / 16f) * 16f) * num;
					float num4 = ((m_subsystemTime != null) ? m_subsystemTime.GameTimeDelta : 0.016f);
					m_currentMastYaw = StepAngle(m_currentMastYaw, target, num3 * num4);
				}
				matrix = Matrix.CreateRotationY(m_currentMastYaw);
				matrix2 = matrix * Matrix.CreateScale(0.0001f);
			}
			if (m_mastBone != null)
			{
				SetBoneTransform(m_mastBone.Index, ShowMast ? matrix : matrix2);
			}
			int num5 = ((m_sailRolledBone != null) ? m_sailRolledBone.Index : int.MinValue);
			int num6 = ((m_sailHalfBone != null) ? m_sailHalfBone.Index : int.MinValue);
			int num7 = ((m_sailFullBone != null) ? m_sailFullBone.Index : int.MinValue);
			if ((num5 != int.MinValue && (num5 == num6 || num5 == num7)) || (num6 != int.MinValue && num6 == num7))
			{
				if (m_sailRolledBone != null)
				{
					SetBoneTransform(m_sailRolledBone.Index, matrix);
				}
				if (m_sailHalfBone != null)
				{
					SetBoneTransform(m_sailHalfBone.Index, matrix);
				}
				if (m_sailFullBone != null)
				{
					SetBoneTransform(m_sailFullBone.Index, matrix);
				}
			}
			else
			{
				if (m_sailRolledBone != null)
				{
					SetBoneTransform(m_sailRolledBone.Index, ShowSailRolled ? matrix : matrix2);
				}
				if (m_sailHalfBone != null)
				{
					SetBoneTransform(m_sailHalfBone.Index, ShowSailHalf ? matrix : matrix2);
				}
				if (m_sailFullBone != null)
				{
					SetBoneTransform(m_sailFullBone.Index, ShowSailFull ? matrix : matrix2);
				}
			}
		}
		base.Animate();
	}

	private void ResolveMeshIndices()
	{
		m_hullIndex = -1;
		m_mastIndex = -1;
		m_sailRolledIndex = -1;
		m_sailHalfIndex = -1;
		m_sailFullIndex = -1;
		if (Model != null && Model.Meshes.Count != 0)
		{
			m_hullIndex = FindMeshIndex(HullMeshName, "Body");
			m_mastIndex = FindMeshIndex(MastMeshName, "Mast");
			m_sailRolledIndex = FindMeshIndex(SailRolledMeshName, "SailRolled");
			m_sailHalfIndex = FindMeshIndex(SailHalfMeshName, "SailHalf");
			m_sailFullIndex = FindMeshIndex(SailFullMeshName, "SailFull");
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

	private void RebuildVisibleMeshOrderIfNeeded()
	{
		int num = (ShowMast ? 1 : 0) | ((ShowSailRolled ? 1 : 0) << 1) | ((ShowSailHalf ? 1 : 0) << 2) | ((ShowSailFull ? 1 : 0) << 3);
		if (num != m_lastFlagsHash)
		{
			m_lastFlagsHash = num;
			RebuildVisibleMeshOrder(force: true);
		}
	}

	private void RebuildVisibleMeshOrder(bool force = false)
	{
		if (Model == null || Model.Meshes.Count == 0)
		{
			MeshDrawOrders = null;
			return;
		}
		List<int> list = new List<int>(Model.Meshes.Count);
		if (m_hullIndex >= 0)
		{
			list.Add(m_hullIndex);
		}
		if (m_mastIndex >= 0 && ShowMast)
		{
			list.Add(m_mastIndex);
		}
		if (m_sailRolledIndex >= 0)
		{
			list.Add(m_sailRolledIndex);
		}
		if (m_sailHalfIndex >= 0)
		{
			list.Add(m_sailHalfIndex);
		}
		if (m_sailFullIndex >= 0)
		{
			list.Add(m_sailFullIndex);
		}
		for (int i = 0; i < Model.Meshes.Count; i++)
		{
			if (!list.Contains(i))
			{
				list.Add(i);
			}
		}
		MeshDrawOrders = list.ToArray();
	}

	public ComponentShipModel()
	{
		m_linesRenderer = new PrimitivesRenderer3D();
		m_hullOffsets = new Vector3[4]
		{
			new Vector3(1f, 0f, 1f),
			new Vector3(-1f, 0f, 1f),
			new Vector3(1f, 0f, -1f),
			new Vector3(-1f, 0f, -1f)
		};
		m_ropeOffsets = new Vector3[4]
		{
			new Vector3(-0.8f, 0f, 0f),
			new Vector3(0.8f, 0f, 0f),
			new Vector3(-1.2f, 0f, 0f),
			new Vector3(1.2f, 0f, 0f)
		};
		m_ropeHeights = new float[4] { 3.24f, 3.24f, 1.35f, 1.35f };
		m_stayHullOffsets = new Vector3[2]
		{
			new Vector3(-0.6f, 0.4f, 0.65f),
			new Vector3(0.6f, 0.4f, 0.65f)
		};
		m_stayMastSideOffsets = new float[2] { -0.2f, 0.2f };
		m_stayMastHeights = new float[2] { 3.27f, 3.27f };
		m_sheetHullOffsets = new Vector3[2]
		{
			new Vector3(-0.6f, 0.4f, 0f),
			new Vector3(0.6f, 0.4f, 0f)
		};
		m_sheetMastSideOffsets = new float[2] { -0.8f, 0.8f };
		m_sheetMastHeights = new float[2] { 2.04f, 2.04f };
		MastTurnSpeedMin = 0.2f;
		MastTurnSpeedPerStrength = 0.15f;
		HullMeshName = "Boat";
		MastMeshName = "Mast";
		SailRolledMeshName = "SailRolled";
		SailHalfMeshName = "SailHalf";
		SailFullMeshName = "SailFull";
		ShowMast = true;
		ShowSailRolled = true;
		ShowSailHalf = false;
		ShowSailFull = false;
		m_hullIndex = -1;
		m_mastIndex = -1;
		m_sailRolledIndex = -1;
		m_sailHalfIndex = -1;
		m_sailFullIndex = -1;
	}

	private void ResolveBones()
	{
		m_rootBone = ((Model != null) ? Model.RootBone : null);
		m_hullBone = null;
		m_mastBone = null;
		m_sailRolledBone = null;
		m_sailHalfBone = null;
		m_sailFullBone = null;
		if (Model != null)
		{
			if (m_hullIndex >= 0)
			{
				m_hullBone = Model.Meshes[m_hullIndex].ParentBone;
			}
			if (m_mastIndex >= 0)
			{
				m_mastBone = Model.Meshes[m_mastIndex].ParentBone;
			}
			if (m_sailRolledIndex >= 0)
			{
				m_sailRolledBone = Model.Meshes[m_sailRolledIndex].ParentBone;
			}
			if (m_sailHalfIndex >= 0)
			{
				m_sailHalfBone = Model.Meshes[m_sailHalfIndex].ParentBone;
			}
			if (m_sailFullIndex >= 0)
			{
				m_sailFullBone = Model.Meshes[m_sailFullIndex].ParentBone;
			}
			if (m_mastBone == null)
			{
				m_mastBone = Model.FindBone("Mast", throwIfNotFound: false);
			}
			if (m_sailRolledBone == null)
			{
				m_sailRolledBone = Model.FindBone("SailRolled", throwIfNotFound: false);
			}
			if (m_sailHalfBone == null)
			{
				m_sailHalfBone = Model.FindBone("SailHalf", throwIfNotFound: false);
			}
			if (m_sailFullBone == null)
			{
				m_sailFullBone = Model.FindBone("SailFull", throwIfNotFound: false);
			}
		}
	}

	private static float WrapAngle(float a)
	{
		while (a > (float)Math.PI)
		{
			a -= (float)Math.PI * 2f;
		}
		while (a < -(float)Math.PI)
		{
			a += (float)Math.PI * 2f;
		}
		return a;
	}

	private static float StepAngle(float current, float target, float maxStep)
	{
		float savedFileName3 = WrapAngle(target - current);
		if (savedFileName3 > maxStep)
		{
			savedFileName3 = maxStep;
		}
		if (savedFileName3 < 0f - maxStep)
		{
			savedFileName3 = 0f - maxStep;
		}
		return WrapAngle(current + savedFileName3);
	}

	private void ApplySailVisibility(SailMode mode, bool rebuildOrder)
	{
		ShowSailRolled = mode == SailMode.Rolled;
		ShowSailHalf = mode == SailMode.Half;
		ShowSailFull = mode == SailMode.Full;
		m_lastFlagsHash = -1;
		if (rebuildOrder)
		{
			RebuildVisibleMeshOrder(force: true);
		}
	}

	public SailMode GetSailMode()
	{
		if (ShowSailHalf)
		{
			return SailMode.Half;
		}
		if (ShowSailFull)
		{
			return SailMode.Full;
		}
		return SailMode.Rolled;
	}

	public void CycleSail()
	{
		ApplySailVisibility(GetSailMode() switch
		{
			SailMode.Half => SailMode.Full, 
			SailMode.Rolled => SailMode.Half, 
			_ => SailMode.Rolled, 
		}, rebuildOrder: true);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		valuesDictionary.SetValue("HullMeshName", HullMeshName);
		valuesDictionary.SetValue("MastMeshName", MastMeshName);
		valuesDictionary.SetValue("SailRolledMeshName", SailRolledMeshName);
		valuesDictionary.SetValue("SailHalfMeshName", SailHalfMeshName);
		valuesDictionary.SetValue("SailFullMeshName", SailFullMeshName);
		valuesDictionary.SetValue("SailMode", (int)GetSailMode());
		valuesDictionary.SetValue("MastYawOffset", MastYawOffset);
		valuesDictionary.SetValue("MastTurnSpeedMin", MastTurnSpeedMin);
		valuesDictionary.SetValue("MastTurnSpeedPerStrength", MastTurnSpeedPerStrength);
		valuesDictionary.SetValue("MastYaw", m_currentMastYaw);
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (camera == null)
		{
			return;
		}
		Matrix matrix = m_componentFrame.Matrix;
		Vector3 vector = matrix.Translation + matrix.Forward * 1f;
		Matrix matrix2 = Matrix.CreateRotationY(m_currentMastYaw) * matrix;
		Vector3 right = matrix2.Right;
		Vector3 up = matrix2.Up;
		PrimitivesRenderer3D linesRenderer = m_linesRenderer;
		FlatBatch3D batch = linesRenderer.FlatBatch(0, DepthStencilState.Default);
		Vector3 viewPosition = camera.ViewPosition;
		float maxDist = 30f;
		float baseOff = 0.02f;
		for (int i = 0; i < m_stayHullOffsets.Length; i++)
		{
			Vector3 vector2 = Vector3.Transform(m_stayHullOffsets[i], matrix);
			Vector3 vector3 = vector + right * m_stayMastSideOffsets[i] + up * m_stayMastHeights[i];
			Vector3 vector4 = (vector2 + vector3) * 0.5f + new Vector3(0f, -1f, 0f) * MathUtils.Min(Vector3.Distance(vector2, vector3) * 0.3f, 3f);
			Vector3 a = vector2;
			for (int j = 1; j <= 16; j++)
			{
				float num = (float)j / 16f;
				float num2 = 1f - num;
				Vector3 vector5 = num2 * num2 * vector2 + 2f * num2 * num * vector4 + num * num * vector3;
				QueueRope(a, vector5, batch, camera, viewPosition, maxDist, baseOff);
				a = vector5;
			}
		}
		float num3 = (ShowSailFull ? 0.3f : (ShowSailHalf ? 0.55f : 0.7f)) * 1.3f;
		for (int k = 0; k < m_sheetHullOffsets.Length; k++)
		{
			Vector3 vector6 = Vector3.Transform(m_sheetHullOffsets[k], matrix);
			float num4 = m_stayMastHeights[k] * num3;
			if (ShowSailFull)
			{
				num4 *= 1.35f;
			}
			else if (ShowSailHalf)
			{
				num4 *= 1.35f;
			}
			Vector3 vector7 = vector + right * m_sheetMastSideOffsets[k] + up * num4;
			Vector3 vector8 = (vector6 + vector7) * 0.5f + new Vector3(0f, -1f, 0f) * MathUtils.Min(Vector3.Distance(vector6, vector7) * 0.3f, 3f);
			Vector3 a2 = vector6;
			for (int l = 1; l <= 16; l++)
			{
				float num5 = (float)l / 16f;
				float num6 = 1f - num5;
				Vector3 vector9 = num6 * num6 * vector6 + 2f * num6 * num5 * vector8 + num5 * num5 * vector7;
				QueueRope(a2, vector9, batch, camera, viewPosition, maxDist, baseOff);
				a2 = vector9;
			}
		}
		linesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	private static void QueueRope(Vector3 a, Vector3 b, FlatBatch3D batch, Camera camera, Vector3 camPos, float maxDist, float baseOff)
	{
		Vector3 v = (a + b) * 0.5f;
		float num = Vector3.Distance(camPos, v);
		float num2 = MathUtils.Saturate(1f - num / maxDist);
		byte a2 = (byte)MathUtils.Lerp(40f, 255f, num2);
		Color color = Color.PremultiplyAlpha(new Color((byte)0, (byte)0, (byte)0, a2));
		batch.QueueLine(a, b, color);
		Vector3 vector = Vector3.Normalize(Vector3.Cross(Vector3.Normalize(b - a), camera.ViewDirection));
		if (num2 > 0.66f)
		{
			float num3 = baseOff * num2 * 0.5f;
			batch.QueueLine(a + vector * num3, b + vector * num3, color);
			batch.QueueLine(a - vector * num3, b - vector * num3, color);
		}
		else if (num2 > 0.33f)
		{
			float num4 = baseOff * num2 * 0.25f;
			batch.QueueLine(a + vector * num4, b + vector * num4, color);
		}
	}
}
