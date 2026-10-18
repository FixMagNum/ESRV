using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCreatureModel : ComponentModel, IUpdateable
{
	public struct Detachable
	{
		public int MeshIndex;

		public int ParentBoneIndex;

		public BoundingBox Bounds;

		public float Score;

		public bool Hidden;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemGameInfo m_subsystemGameInfo;

	public ComponentCreature m_componentCreature;

	public Vector3? m_eyePosition;

	public Quaternion? m_eyeRotation;

	public float m_injuryColorFactor;

	public Vector3 m_randomLookPoint;

	public Random m_random;

	public bool LastAttackOrder;

	public bool LastFeedOrder;

	public bool m_feedOrder;

	public bool m_attackOrder;

	private Vector3 m_paintColor;

	private float m_paintFactor;

	private float m_paintTimer;

	public List<Detachable> m_detachables;

	public bool m_detachablesBuilt;

	public int m_visibleMeshesVersion;

	public float CorpseTintStart = 500f;

	public float CorpseHalfTint = 1000f;

	public float CorpseFullTint = 2000f;

	public Vector3 CorpseTintColor = new Vector3(0.5f, 0.15f, 0.45f);

	private float m_corpseTintFactor;

	private HashSet<int> m_loadedHiddenMeshes;

	public float Bob { get; set; }

	public float MovementAnimationPhase { get; set; }

	public float DeathPhase { get; set; }

	public Vector3 DeathCauseOffset { get; set; }

	public float HeadShakeOrder { get; set; }

	public bool RowLeftOrder { get; set; }

	public bool RowRightOrder { get; set; }

	public float AimHandAngleOrder { get; set; }

	public Vector3 InHandItemOffsetOrder { get; set; }

	public Vector3 InHandItemRotationOrder { get; set; }

	public bool IsAttackHitMoment { get; set; }

	public Vector3? LookAtOrder { get; set; }

	public bool LookRandomOrder { get; set; }

	public bool AttackOrder
	{
		get
		{
			return m_attackOrder;
		}
		set
		{
			m_attackOrder = value;
		}
	}

	public bool FeedOrder
	{
		get
		{
			return m_feedOrder;
		}
		set
		{
			m_feedOrder = value;
		}
	}

	public Vector3 EyePosition
	{
		get
		{
			if (!m_eyePosition.HasValue)
			{
				m_eyePosition = CalculateEyePosition();
			}
			return m_eyePosition.Value;
		}
	}

	public Quaternion EyeRotation
	{
		get
		{
			if (!m_eyeRotation.HasValue)
			{
				m_eyeRotation = CalculateEyeRotation();
			}
			return m_eyeRotation.Value;
		}
	}

	public UpdateOrder UpdateOrder
	{
		get
		{
			ComponentBody parentBody = m_componentCreature.ComponentBody.ParentBody;
			if (parentBody != null)
			{
				ComponentCreatureModel componentCreatureModel = parentBody.Entity.FindComponent<ComponentCreatureModel>();
				if (componentCreatureModel != null)
				{
					return componentCreatureModel.UpdateOrder + 1;
				}
			}
			return UpdateOrder.CreatureModels;
		}
	}

	public bool IsFullyDismembered
	{
		get
		{
			if (m_detachables == null)
			{
				return true;
			}
			for (int i = 0; i < m_detachables.Count; i++)
			{
				if (!m_detachables[i].Hidden)
				{
					return false;
				}
			}
			return true;
		}
	}

	public override void Animate()
	{
		float num = ((m_componentCreature.ComponentSpawn.SpawnDuration > 0f) ? MathUtils.Saturate((float)((m_subsystemGameInfo.TotalElapsedGameTime - m_componentCreature.ComponentSpawn.SpawnTime) / (double)m_componentCreature.ComponentSpawn.SpawnDuration)) : 1f);
		if (m_componentCreature.ComponentSpawn.DespawnTime.HasValue)
		{
			num = MathUtils.Min(num, MathUtils.Saturate((float)(1.0 - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentCreature.ComponentSpawn.DespawnTime.Value) / (double)m_componentCreature.ComponentSpawn.DespawnDuration)));
		}
		Opacity = num;
		Vector3 vector = Vector3.Lerp(Vector3.One, new Vector3(1f, 0f, 0f), m_injuryColorFactor);
		Vector3 vector2 = Vector3.Lerp(Vector3.One, m_paintColor, m_paintFactor);
		Vector3 vector3 = Vector3.Lerp(Vector3.One, CorpseTintColor, m_corpseTintFactor);
		ComponentLoot componentLoot = Entity.FindComponent<ComponentLoot>();
		Vector3 vector4 = ((componentLoot != null && componentLoot.m_charred) ? Vector3.Zero : Vector3.One);
		DiffuseColor = vector * vector2 * vector3 * vector4;
		if (Opacity < 1f)
		{
			bool flag = m_componentCreature.ComponentBody.ImmersionFactor >= 1f;
			bool flag2 = m_subsystemSky.ViewUnderWaterDepth > 0f;
			RenderingMode = ((flag == flag2) ? ModelRenderingMode.TransparentAfterWater : ModelRenderingMode.TransparentBeforeWater);
		}
		else
		{
			RenderingMode = ModelRenderingMode.Solid;
		}
		if (!m_detachablesBuilt)
		{
			BuildDetachablePartsIfNeeded();
			return;
		}
		RebuildVisibleMeshOrder();
		ApplyHiddenBonesScale();
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentCreature.ComponentHealth.Health > 0f && Model != null)
		{
			EmbraceMutate((float)m_subsystemGameInfo.TotalElapsedGameTime);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_componentCreature.ComponentHealth.Attacked += (ComponentCreature attacker) =>
		{
			if (DeathPhase == 0f && m_componentCreature.ComponentHealth.Health == 0f)
			{
				DeathCauseOffset = attacker.ComponentBody.BoundingBox.Center() - m_componentCreature.ComponentBody.BoundingBox.Center();
			}
		};
		string value = valuesDictionary.GetValue("HiddenMeshes", string.Empty);
		if (!string.IsNullOrEmpty(value))
		{
			m_loadedHiddenMeshes = new HashSet<int>();
			string[] array = value.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			for (int num = 0; num < array.Length; num++)
			{
				if (int.TryParse(array[num], out var result))
				{
					m_loadedHiddenMeshes.Add(result);
				}
			}
		}
		BuildDetachablePartsIfNeeded();
	}

	public override void OnEntityAdded()
	{
		m_componentCreature.ComponentBody.PositionChanged += (ComponentFrame f) =>
		{
			m_eyePosition = null;
		};
		m_componentCreature.ComponentBody.RotationChanged += (ComponentFrame f) =>
		{
			m_eyeRotation = null;
		};
		m_detachablesBuilt = false;
		BuildDetachablePartsIfNeeded();
	}

	public virtual void Update(float dt)
	{
		if (LookRandomOrder)
		{
			Matrix matrix = m_componentCreature.ComponentBody.Matrix;
			Vector3 v = Vector3.Normalize(m_randomLookPoint - EyePosition);
			if (m_random.Float(0f, 1f) < 0.25f * dt || Vector3.Dot(matrix.Forward, v) < 0.2f)
			{
				float num = m_random.Float(-5f, 5f);
				float num2 = m_random.Float(-1f, 1f);
				float num3 = m_random.Float(3f, 8f);
				m_randomLookPoint = EyePosition + num3 * matrix.Forward + num2 * matrix.Up + num * matrix.Right;
			}
			LookAtOrder = m_randomLookPoint;
		}
		if (LookAtOrder.HasValue)
		{
			Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
			Vector3 v2 = LookAtOrder.Value - EyePosition;
			float x = Vector2.Angle(new Vector2(forward.X, forward.Z), new Vector2(v2.X, v2.Z));
			float y = MathUtils.Asin(0.99f * Vector3.Normalize(v2).Y);
			Vector2 lookOrder = new Vector2(x, y) - m_componentCreature.ComponentLocomotion.LookAngles;
			lookOrder.X = MathUtils.NormalizeAngle(lookOrder.X);
			m_componentCreature.ComponentLocomotion.LookOrder = lookOrder;
		}
		if (HeadShakeOrder > 0f)
		{
			HeadShakeOrder = MathUtils.Max(HeadShakeOrder - dt, 0f);
			float num4 = 1f * MathUtils.Saturate(4f * HeadShakeOrder);
			m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(num4 * (float)MathUtils.Sin(16.0 * m_subsystemTime.GameTime + (double)(0.01f * (float)GetHashCode())), 0f) - m_componentCreature.ComponentLocomotion.LookAngles;
		}
		if (m_componentCreature.ComponentHealth.Health == 0f)
		{
			DeathPhase = MathUtils.Min(DeathPhase + 3f * dt, 1f);
		}
		if (m_componentCreature.ComponentHealth.HealthChange < 0f)
		{
			m_injuryColorFactor = 1f;
		}
		m_injuryColorFactor = MathUtils.Saturate(m_injuryColorFactor - 3f * dt);
		if (m_paintFactor > 0f)
		{
			m_paintTimer -= dt;
			if (m_paintTimer <= 0f)
			{
				m_paintFactor = MathUtils.Max(m_paintFactor - dt * 0.5f, 0f);
			}
		}
		ComponentHealth componentHealth = m_componentCreature?.ComponentHealth;
		if (componentHealth != null && componentHealth.Health <= 0f && componentHealth.DeathTime.HasValue)
		{
			double num5 = m_subsystemGameInfo.TotalElapsedGameTime - componentHealth.DeathTime.Value;
			float x2 = 0f;
			if (num5 > (double)CorpseTintStart)
			{
				if (num5 <= (double)CorpseHalfTint)
				{
					x2 = (float)((num5 - (double)CorpseTintStart) / (double)(CorpseHalfTint - CorpseTintStart)) * 0.5f;
				}
				else
				{
					x2 = ((!(num5 <= (double)CorpseFullTint)) ? 1f : (0.5f + (float)((num5 - (double)CorpseHalfTint) / (double)(CorpseFullTint - CorpseHalfTint)) * 0.5f));
				}
			}
			m_corpseTintFactor = MathUtils.Clamp(x2, 0f, 1f);
		}
		else
		{
			m_corpseTintFactor = 0f;
		}
		m_eyePosition = null;
		m_eyeRotation = null;
		LookRandomOrder = false;
		LookAtOrder = null;
	}

	public virtual Vector3 CalculateEyePosition()
	{
		Matrix currentSeason = m_componentCreature.ComponentBody.Matrix;
		return m_componentCreature.ComponentBody.Position + currentSeason.Up * 0.95f * m_componentCreature.ComponentBody.BoxSize.Y + currentSeason.Forward * 0.45f * m_componentCreature.ComponentBody.BoxSize.Z;
	}

	public virtual Quaternion CalculateEyeRotation()
	{
		return m_componentCreature.ComponentBody.Rotation * Quaternion.CreateFromYawPitchRoll(0f - m_componentCreature.ComponentLocomotion.LookAngles.X, m_componentCreature.ComponentLocomotion.LookAngles.Y, 0f);
	}

	public ComponentCreatureModel()
	{
		m_paintColor = Vector3.Zero;
		m_random = new Random();
	}

	public void ApplyPaint(Vector3 rgb, float duration = 20f)
	{
		m_paintColor = rgb;
		m_paintFactor = 1f;
		m_paintTimer = duration;
	}

	public bool CutNextDetachablePart()
	{
		BuildDetachablePartsIfNeeded();
		if (m_detachables == null || m_detachables.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < m_detachables.Count; i++)
		{
			if (!m_detachables[i].Hidden)
			{
				int parentBoneIndex = m_detachables[i].ParentBoneIndex;
				if (parentBoneIndex >= 0)
				{
					HideAllMeshesByBone(parentBoneIndex);
				}
				else
				{
					Detachable value = m_detachables[i];
					value.Hidden = true;
					m_detachables[i] = value;
				}
				RebuildVisibleMeshOrder();
				m_visibleMeshesVersion++;
				return true;
			}
		}
		return false;
	}

	public void BuildDetachablePartsIfNeeded()
	{
		if (m_detachablesBuilt)
		{
			return;
		}
		m_detachablesBuilt = true;
		m_detachables = new List<Detachable>();
		Model num = Model;
		if (num == null || num.Meshes.Count == 0)
		{
			return;
		}
		BoundingBox i = default;
		bool flag = false;
		for (int j = 0; j < num.Meshes.Count; j++)
		{
			BoundingBox boundingBox = num.Meshes[j].BoundingBox;
			if (!flag)
			{
				i = boundingBox;
				flag = true;
			}
			else
			{
				i = BoundingBox.Union(i, boundingBox);
			}
		}
		Vector3 vector = (i.Min + i.Max) * 0.5f;
		float num2 = (i.Max - i.Min).Length();
		if (num2 < 0.0001f)
		{
			num2 = 1f;
		}
		for (int k = 0; k < num.Meshes.Count; k++)
		{
			ModelMesh modelMesh = num.Meshes[k];
			BoundingBox boundingBox2 = modelMesh.BoundingBox;
			Vector3 vector2 = (boundingBox2.Min + boundingBox2.Max) * 0.5f;
			Vector3 vector3 = boundingBox2.Max - boundingBox2.Min;
			float num3 = Math.Max(1E-06f, vector3.X * vector3.Y * vector3.Z);
			float num4 = (vector2 - vector).Length() / num2;
			float num5 = Math.Max(vector3.X, Math.Max(vector3.Y, vector3.Z));
			float num6 = Math.Min(vector3.X, Math.Min(vector3.Y, vector3.Z));
			float num7 = ((num5 > 1E-06f) ? MathUtils.Saturate(1f - num6 / num5) : 0f);
			float num8 = 1f / (float)Math.Pow(num3, 1.0 / 3.0);
			float score = num4 * 2f + num7 * 1f + num8 * 0.5f;
			bool hidden = m_loadedHiddenMeshes != null && m_loadedHiddenMeshes.Contains(k);
			Detachable item = new Detachable
			{
				MeshIndex = k,
				ParentBoneIndex = ((modelMesh.ParentBone != null) ? modelMesh.ParentBone.Index : (-1)),
				Bounds = boundingBox2,
				Score = score,
				Hidden = hidden
			};
			m_detachables.Add(item);
		}
		m_detachables.Sort((Detachable a, Detachable b) => b.Score.CompareTo(a.Score));
		int num9 = -1;
		float num10 = float.NegativeInfinity;
		for (int num11 = 0; num11 < m_detachables.Count; num11++)
		{
			Vector3 vector4 = m_detachables[num11].Bounds.Max - m_detachables[num11].Bounds.Min;
			float num12 = vector4.X * vector4.Y * vector4.Z;
			Vector3 vector5 = (m_detachables[num11].Bounds.Min + m_detachables[num11].Bounds.Max) * 0.5f;
			float num13 = 1f - MathUtils.Saturate((vector5 - vector).Length() / num2);
			float num14 = num12 * num13;
			if (num14 > num10)
			{
				num10 = num14;
				num9 = num11;
			}
		}
		if (num9 >= 0)
		{
			Detachable item2 = m_detachables[num9];
			m_detachables.RemoveAt(num9);
			m_detachables.Add(item2);
		}
		RebuildVisibleMeshOrder();
	}

	public void RebuildVisibleMeshOrder()
	{
		if (Model == null || Model.Meshes.Count == 0)
		{
			MeshDrawOrders = null;
			return;
		}
		if (m_detachables == null || m_detachables.Count == 0)
		{
			int count = Model.Meshes.Count;
			int[] array = new int[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = i;
			}
			MeshDrawOrders = array;
			return;
		}
		List<int> list = new List<int>(Model.Meshes.Count);
		for (int j = 0; j < Model.Meshes.Count; j++)
		{
			bool flag = false;
			for (int k = 0; k < m_detachables.Count; k++)
			{
				if (m_detachables[k].MeshIndex == j)
				{
					flag = m_detachables[k].Hidden;
					break;
				}
			}
			if (!flag)
			{
				list.Add(j);
			}
		}
		MeshDrawOrders = list.ToArray();
	}

	public void TryCollapseBoneIfUnique(int boneIndex)
	{
		CollapseBone(boneIndex);
	}

	public void HideAllMeshesByBone(int boneIndex)
	{
		if (m_detachables == null)
		{
			return;
		}
		for (int i = 0; i < m_detachables.Count; i++)
		{
			if (m_detachables[i].ParentBoneIndex == boneIndex)
			{
				Detachable value = m_detachables[i];
				value.Hidden = true;
				m_detachables[i] = value;
			}
		}
	}

	public void CollapseBone(int boneIndex)
	{
		if (boneIndex >= 0)
		{
			Matrix data = Matrix.CreateScale(0.0001f);
			SetBoneTransform(boneIndex, data);
		}
	}

	public void ApplyHiddenBonesScale()
	{
		if (m_detachables == null || Model == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < m_detachables.Count; i++)
		{
			if (m_detachables[i].Hidden && m_detachables[i].ParentBoneIndex >= 0)
			{
				hashSet.Add(m_detachables[i].ParentBoneIndex);
			}
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		Matrix matrix = Matrix.CreateScale(0.0001f);
		foreach (int item in hashSet)
		{
			Matrix matrix2 = GetBoneTransform(item) ?? Matrix.Identity;
			SetBoneTransform(item, matrix2 * matrix);
		}
	}

	public virtual void EmbraceMutate(float t)
	{
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		if (m_detachables == null || m_detachables.Count <= 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < m_detachables.Count; i++)
		{
			if (m_detachables[i].Hidden)
			{
				if (flag)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(m_detachables[i].MeshIndex.ToString(CultureInfo.InvariantCulture));
				flag = true;
			}
		}
		if (stringBuilder.Length > 0)
		{
			valuesDictionary.SetValue("HiddenMeshes", stringBuilder.ToString());
		}
	}
}
