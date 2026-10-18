using System.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentModel : Component
{
	public SubsystemSky m_subsystemSky;

	public ComponentFrame m_componentFrame;

	public Model m_model;

	public Matrix?[] m_boneTransforms;

	public float m_boundingSphereRadius;

	public SubsystemWeather m_subsystemWeather;

	public float? Opacity { get; set; }

	public Vector3? DiffuseColor { get; set; }

	public Vector4? EmissionColor { get; set; }

	public Model Model
	{
		get
		{
			return m_model;
		}
		set
		{
			SetModel(value);
		}
	}

	public Texture2D TextureOverride { get; set; }

	public bool CastsShadow { get; set; }

	public int PrepareOrder { get; set; }

	public virtual ModelRenderingMode RenderingMode { get; set; }

	public int[] MeshDrawOrders { get; set; }

	public bool IsVisibleForCamera { get; set; }

	public Matrix[] AbsoluteBoneTransformsForCamera { get; set; }

	public bool IsVanished { get; set; }

	public Matrix? GetBoneTransform(int boneIndex)
	{
		return m_boneTransforms[boneIndex];
	}

	public void SetBoneTransform(int boneIndex, Matrix? transformation)
	{
		m_boneTransforms[boneIndex] = transformation;
	}

	public void CalculateAbsoluteBonesTransforms(Camera camera)
	{
		ProcessBoneHierarchy(Model.RootBone, camera.ViewMatrix, AbsoluteBoneTransformsForCamera);
	}

	public virtual void CalculateIsVisible(Camera camera)
	{
		if (IsVanished)
		{
			IsVisibleForCamera = false;
			return;
		}
		ComponentCreatureModel model = Entity.FindComponent<ComponentCreatureModel>();
		if (model != null && model != this && model.IsVanished)
		{
			IsVisibleForCamera = false;
			return;
		}
		if (camera.GameWidget.IsEntityFirstPersonTarget(Entity))
		{
			IsVisibleForCamera = SettingsManager.FirstPersonCameraEnabled;
			return;
		}
		float boneAbsoluteTransform = MathUtils.Sqr(m_subsystemSky.VisibilityRange);
		Vector3 boneAbsoluteTransform2 = m_componentFrame.Position - camera.ViewPosition;
		boneAbsoluteTransform2.Y *= m_subsystemSky.VisibilityRangeYMultiplier;
		if (boneAbsoluteTransform2.LengthSquared() < boneAbsoluteTransform)
		{
			BoundingSphere sphere = new BoundingSphere(m_componentFrame.Position, m_boundingSphereRadius);
			IsVisibleForCamera = camera.ViewFrustum.Intersection(sphere);
		}
		else
		{
			IsVisibleForCamera = false;
		}
	}

	public virtual void Animate()
	{
	}

	public virtual void DrawExtras(Camera camera)
	{
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_componentFrame = Entity.FindComponent<ComponentFrame>(throwOnError: true);
		IsVanished = valuesDictionary.GetValue("IsVanished", defaultValue: false);
		string value = valuesDictionary.GetValue<string>("ModelName");
		string name = value;
		if (m_subsystemWeather != null)
		{
			if ((m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Anabiosis || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Winter || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.LateWinter) && valuesDictionary.ContainsKey("ModelNameWinter"))
			{
				string value2 = valuesDictionary.GetValue<string>("ModelNameWinter");
				try
				{
					if (ContentManager.Get<Model>(value2) != null)
					{
						name = value2;
					}
				}
				catch
				{
				}
			}
			else if ((m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Spring || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.EarlySpring || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.LateSpring) && valuesDictionary.ContainsKey("ModelNameSpring"))
			{
				string value3 = valuesDictionary.GetValue<string>("ModelNameSpring");
				try
				{
					if (ContentManager.Get<Model>(value3) != null)
					{
						name = value3;
					}
				}
				catch
				{
				}
			}
		}
		Model model;
		try
		{
			model = ContentManager.Get<Model>(name);
		}
		catch
		{
			model = ContentManager.Get<Model>(value);
		}
		Model = model;
		CastsShadow = valuesDictionary.GetValue<bool>("CastsShadow");
		string value4 = valuesDictionary.GetValue<string>("TextureOverride");
		TextureOverride = (string.IsNullOrEmpty(value4) ? null : ContentManager.Get<Texture2D>(value4));
		PrepareOrder = valuesDictionary.GetValue<int>("PrepareOrder");
		m_boundingSphereRadius = valuesDictionary.GetValue<float>("BoundingSphereRadius");
	}

	public virtual void SetModel(Model model)
	{
		m_model = model;
		if (m_model != null)
		{
			m_boneTransforms = new Matrix?[m_model.Bones.Count];
			AbsoluteBoneTransformsForCamera = new Matrix[m_model.Bones.Count];
			MeshDrawOrders = Enumerable.Range(0, m_model.Meshes.Count).ToArray();
		}
		else
		{
			m_boneTransforms = null;
			AbsoluteBoneTransformsForCamera = null;
			MeshDrawOrders = null;
		}
	}

	public void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform, Matrix[] transforms)
	{
		Matrix m = modelBone.Transform;
		if (m_boneTransforms[modelBone.Index].HasValue)
		{
			Vector3 translation = m.Translation;
			m.Translation = Vector3.Zero;
			m *= m_boneTransforms[modelBone.Index].Value;
			m.Translation += translation;
			Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
		}
		else
		{
			Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
		}
		foreach (ModelBone childBone in modelBone.ChildBones)
		{
			ProcessBoneHierarchy(childBone, transforms[modelBone.Index], transforms);
		}
	}

	public void SetModelScale(Vector3 scale)
	{
		if (m_model == null)
		{
			return;
		}
		foreach (ModelBone bone in m_model.Bones)
		{
			Matrix transform = bone.Transform;
			transform.M11 *= scale.X;
			transform.M22 *= scale.Y;
			transform.M33 *= scale.Z;
			bone.Transform = transform;
		}
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("IsVanished", IsVanished);
		base.Save(valuesDictionary, entityToIdMap);
	}
}
