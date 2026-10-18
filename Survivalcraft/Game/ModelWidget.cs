using Engine;
using Engine.Graphics;

namespace Game;

public class ModelWidget : Widget
{
	public static LitShader m_shader = new LitShader(1, useEmissionColor: false, useVertexColor: false, useTexture: true, useFog: false, useAlphaThreshold: false);

	public static LitShader m_shaderAlpha = new LitShader(1, useEmissionColor: false, useVertexColor: false, useTexture: true, useFog: false, useAlphaThreshold: true);

	public Model m_model;

	public Matrix?[] m_boneTransforms;

	public Matrix[] m_absoluteBoneTransforms;

	public Vector2 Size { get; set; }

	public Color Color { get; set; }

	public bool UseAlphaThreshold { get; set; }

	public bool IsPerspective { get; set; }

	public Vector3 OrthographicFrustumSize { get; set; }

	public Vector3 ViewPosition { get; set; }

	public Vector3 ViewTarget { get; set; }

	public float ViewFov { get; set; }

	public Matrix ModelMatrix { get; set; } = Matrix.Identity;

	public Vector3 AutoRotationVector { get; set; }

	public Model Model
	{
		get
		{
			return m_model;
		}
		set
		{
			if (value != m_model)
			{
				m_model = value;
				if (m_model != null)
				{
					m_boneTransforms = new Matrix?[m_model.Bones.Count];
					m_absoluteBoneTransforms = new Matrix[m_model.Bones.Count];
				}
				else
				{
					m_boneTransforms = null;
					m_absoluteBoneTransforms = null;
				}
			}
		}
	}

	public Texture2D TextureOverride { get; set; }

	public ModelWidget()
	{
		Size = new Vector2(float.PositiveInfinity);
		IsHitTestVisible = false;
		Color = Color.White;
		UseAlphaThreshold = false;
		IsPerspective = true;
		ViewPosition = new Vector3(0f, 0f, -5f);
		ViewTarget = new Vector3(0f, 0f, 0f);
		ViewFov = 1f;
		OrthographicFrustumSize = new Vector3(0f, 10f, 10f);
	}

	public Matrix? GetBoneTransform(int boneIndex)
	{
		return m_boneTransforms[boneIndex];
	}

	public void SetBoneTransform(int boneIndex, Matrix? transformation)
	{
		m_boneTransforms[boneIndex] = transformation;
	}

	public override void Draw(DrawContext dc)
	{
		if (Model == null)
		{
			return;
		}
		LitShader vector = (UseAlphaThreshold ? m_shaderAlpha : m_shader);
		vector.Texture = TextureOverride;
		vector.SamplerState = SamplerState.PointClamp;
		vector.MaterialColor = new Vector4(Color * GlobalColorTransform);
		vector.AmbientLightColor = new Vector3(0.66f, 0.66f, 0.66f);
		vector.DiffuseLightColor1 = new Vector3(1f, 1f, 1f);
		vector.LightDirection1 = Vector3.Normalize(new Vector3(1f, 1f, 1f));
		if (UseAlphaThreshold)
		{
			vector.AlphaThreshold = 0f;
		}
		vector.Transforms.View = Matrix.CreateLookAt(ViewPosition, ViewTarget, Vector3.UnitY);
		Viewport vector2 = Display.Viewport;
		float vector3 = ActualSize.X / ActualSize.Y;
		if (IsPerspective)
		{
			vector.Transforms.Projection = Matrix.CreatePerspectiveFieldOfView(ViewFov, vector3, 0.1f, 100f) * MatrixUtils.CreateScaleTranslation(0.5f * ActualSize.X, -0.5f * ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f) * GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)vector2.Width, -2f / (float)vector2.Height, -1f, 1f);
		}
		else
		{
			Vector3 i = OrthographicFrustumSize;
			if (i.X < 0f)
			{
				i.X = i.Y / vector3;
			}
			else if (i.Y < 0f)
			{
				i.Y = i.X * vector3;
			}
			vector.Transforms.Projection = Matrix.CreateOrthographic(i.X, i.Y, 0f, OrthographicFrustumSize.Z) * MatrixUtils.CreateScaleTranslation(0.5f * ActualSize.X, -0.5f * ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f) * GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)vector2.Width, -2f / (float)vector2.Height, -1f, 1f);
		}
		Display.DepthStencilState = DepthStencilState.Default;
		Display.BlendState = BlendState.AlphaBlend;
		Display.RasterizerState = RasterizerState.CullNoneScissor;
		ProcessBoneHierarchy(Model.RootBone, Matrix.Identity, m_absoluteBoneTransforms);
		float num = (float)Time.RealTime + (float)(GetHashCode() % 1000) / 100f;
		Matrix vector4 = ((AutoRotationVector.LengthSquared() > 0f) ? Matrix.CreateFromAxisAngle(Vector3.Normalize(AutoRotationVector), AutoRotationVector.Length() * num) : Matrix.Identity);
		foreach (ModelMesh mesh in Model.Meshes)
		{
			vector.Transforms.World[0] = m_absoluteBoneTransforms[mesh.ParentBone.Index] * ModelMatrix * vector4;
			foreach (ModelMeshPart meshPart in mesh.MeshParts)
			{
				if (meshPart.IndicesCount > 0)
				{
					Display.DrawIndexed(PrimitiveType.TriangleList, vector, meshPart.VertexBuffer, meshPart.IndexBuffer, meshPart.StartIndex, meshPart.IndicesCount);
				}
			}
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = Model != null;
		DesiredSize = Size;
	}

	public void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform, Matrix[] transforms)
	{
		Matrix cells = modelBone.Transform;
		if (m_boneTransforms[modelBone.Index].HasValue)
		{
			Vector3 i = cells.Translation;
			cells.Translation = Vector3.Zero;
			cells *= m_boneTransforms[modelBone.Index].Value;
			cells.Translation += i;
			Matrix.MultiplyRestricted(ref cells, ref currentTransform, out transforms[modelBone.Index]);
		}
		else
		{
			Matrix.MultiplyRestricted(ref cells, ref currentTransform, out transforms[modelBone.Index]);
		}
		foreach (ModelBone num2 in modelBone.ChildBones)
		{
			ProcessBoneHierarchy(num2, transforms[modelBone.Index], transforms);
		}
	}
}
