using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class ClothingPreviewWidget : Widget
{
	private struct PixelChange
	{
		public int Index;

		public Color OldColor;

		public Color NewColor;
	}

	public enum ToolType
	{
		Brush,
		Eraser,
		Pipette
	}

	public static LitShader m_shader;

	public static LitShader m_shaderAlpha;

	private Vector2? m_dragStart;

	private ClothingSlot m_currentSlot;

	private bool m_isOuter;

	private static Texture2D m_dummyWhiteTexture;

	private PlayerClass m_playerClass;

	private PlayerData.PlayerTag m_bodyTag;

	private Model m_bodyModel;

	private Model m_outerModel;

	private Matrix[] m_bodyBoneTransforms;

	private Matrix[] m_outerBoneTransforms;

	private Texture2D m_clothingTexture;

	private Color m_clothingColor;

	private static RasterizerState m_biasState;

	private SliderWidget m_lightSlider;

	private BevelledButtonWidget m_viewModeBtn;

	public HashSet<string> HiddenParts;

	private Color[] m_textureData;

	private int m_texWidth;

	private int m_texHeight;

	private bool m_isTextureDirty;

	private Model m_cachedModelForPaint;

	private bool m_isTextureCloned;

	private Texture2D m_baseTexture;

	private List<List<PixelChange>> m_undoHistory;

	private List<List<PixelChange>> m_redoHistory;

	private List<PixelChange> m_currentStroke;

	public int BrushSize = 1;

	private HashSet<int> m_paintedPixelsInStroke = new HashSet<int>();

	public float CameraRotation = (float)Math.PI;

	public float CameraRotationVertical;

	public Vector2 Size { get; set; }

	public float CameraZoom { get; set; } = 1f;

	public Vector2 CameraPan { get; set; } = Vector2.Zero;

	public bool ShowGrid { get; set; }

	public bool IsPaintMode { get; set; }

	public bool AlwaysShowSkin { get; set; }

	public PlayerClass PlayerClass
	{
		get
		{
			return m_playerClass;
		}
		set
		{
			if (m_playerClass != value)
			{
				m_playerClass = value;
				RequestModelUpdate();
			}
		}
	}

	public PlayerData.PlayerTag BodyTag
	{
		get
		{
			return m_bodyTag;
		}
		set
		{
			if (m_bodyTag != value)
			{
				m_bodyTag = value;
			}
		}
	}

	public Texture2D SkinTexture { get; set; }

	public float LightRotation { get; set; }

	public bool NoShadows { get; set; }

	public ToolType ActiveTool { get; set; }

	public Texture2D WorkingTexture => m_clothingTexture;

	public Color ClothingColor => m_clothingColor;

	public bool IsModified { get; set; }

	public ClothingPreviewWidget()
	{
		m_undoHistory = new List<List<PixelChange>>();
		m_redoHistory = new List<List<PixelChange>>();
		m_currentStroke = new List<PixelChange>();
		HiddenParts = new HashSet<string>();
		CameraRotation = (float)Math.PI;
		m_bodyTag = PlayerData.PlayerTag.Tall;
		m_clothingColor = Color.White;
		Size = new Vector2(float.PositiveInfinity);
		ClampToBounds = true;
	}

	public override void Update()
	{
		FlushTexture();
		if (IsPaintMode)
		{
			m_dragStart = null;
			return;
		}
		if (Input.Tap.HasValue && HitTestGlobal(Input.Tap.Value) == this)
		{
			m_dragStart = Input.Tap;
		}
		if (Input.Press.HasValue && m_dragStart.HasValue)
		{
			Vector2 vector = Input.Press.Value - m_dragStart.Value;
			if (vector.Length() > 2f)
			{
				CameraRotation -= vector.X * 0.01f;
				CameraRotationVertical -= vector.Y * 0.01f;
				CameraRotationVertical = MathUtils.Clamp(CameraRotationVertical, -1.5f, 1.5f);
				m_dragStart = Input.Press;
			}
		}
		else
		{
			m_dragStart = null;
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = m_bodyModel != null;
		DesiredSize = Size;
	}

	public override void Draw(DrawContext dc)
	{
		if (m_bodyModel == null)
		{
			return;
		}
		float aspectRatio = ActualSize.X / ActualSize.Y;
		Vector3 baseScale = Vector3.One;
		switch (m_bodyTag)
		{
		case PlayerData.PlayerTag.Short:
			baseScale = new Vector3(0.85f, 0.85f, 0.85f);
			break;
		case PlayerData.PlayerTag.Fat:
			baseScale = new Vector3(1.5f, 1f, 1.5f);
			break;
		case PlayerData.PlayerTag.Dwarf:
			baseScale = new Vector3(1.25f, 0.7f, 1.25f);
			break;
		}
		float num = ((PlayerClass == PlayerClass.Male) ? 0.9f : 0.86f);
		float z = 2.5f / CameraZoom;
		float radians = (NoShadows ? LightRotation : (CameraRotation + LightRotation));
		float lightRotation = (NoShadows ? (LightRotation - CameraRotation) : LightRotation);
		Matrix m = Matrix.CreateRotationX(CameraRotationVertical) * Matrix.CreateRotationY(radians);
		Vector3 vector = Vector3.Transform(new Vector3(0f - CameraPan.X, 0f, 0f), Matrix.CreateRotationY(radians));
		Vector3 vector2 = new Vector3(0f, num - CameraPan.Y, 0f) + vector;
		Vector3 vector3 = Vector3.Transform(new Vector3(0f, 0f, z), m);
		Matrix view = Matrix.CreateLookAt(vector2 + vector3, vector2, Vector3.UnitY);
		Matrix matrix = Matrix.CreatePerspectiveFieldOfView(1f, aspectRatio, 0.1f, 100f);
		Viewport viewport = Display.Viewport;
		Matrix matrix2 = MatrixUtils.CreateScaleTranslation(ActualSize.X, 0f - ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f) * GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)viewport.Width, -2f / (float)viewport.Height, -1f, 1f);
		Matrix projection = matrix * matrix2;
		Display.DepthStencilState = DepthStencilState.Default;
		Display.BlendState = BlendState.AlphaBlend;
		Display.RasterizerState = RasterizerState.CullNoneScissor;
		if (m_isOuter && m_outerModel != null)
		{
			SyncOuterClothingBones(Vector3.One);
		}
		if (!m_isOuter || AlwaysShowSkin)
		{
			LitShader shader = m_shader;
			shader.Texture = ((SkinTexture != null) ? SkinTexture : GetWhiteTexture());
			shader.SamplerState = SamplerState.PointClamp;
			shader.Transforms.View = view;
			shader.Transforms.Projection = projection;
			DrawModel(m_bodyModel, m_bodyBoneTransforms, shader, baseScale, 1f, lightRotation);
		}
		LitShader shaderAlpha = m_shaderAlpha;
		shaderAlpha.SamplerState = SamplerState.PointClamp;
		shaderAlpha.Transforms.View = view;
		shaderAlpha.Transforms.Projection = projection;
		shaderAlpha.MaterialColor = new Vector4((float)(int)m_clothingColor.R / 255f, (float)(int)m_clothingColor.G / 255f, (float)(int)m_clothingColor.B / 255f, (float)(int)m_clothingColor.A / 255f);
		Model model = ((m_isOuter && m_outerModel != null) ? m_outerModel : m_bodyModel);
		Matrix[] transforms = ((m_isOuter && m_outerModel != null) ? m_outerBoneTransforms : m_bodyBoneTransforms);
		if (m_baseTexture != null)
		{
			shaderAlpha.Texture = m_baseTexture;
			float extraScale = (m_isOuter ? 1f : 1.002f);
			DrawModel(model, transforms, shaderAlpha, baseScale, extraScale, lightRotation);
		}
		if (m_clothingTexture != null)
		{
			shaderAlpha.Texture = m_clothingTexture;
			if (m_biasState == null)
			{
				m_biasState = new RasterizerState
				{
					CullMode = CullMode.None,
					ScissorTestEnable = true,
					DepthBias = -0.0001f,
					SlopeScaleDepthBias = -2f
				};
			}
			Display.RasterizerState = m_biasState;
			float extraScale2 = (m_isOuter ? 1.001f : 1.004f);
			DrawModel(model, transforms, shaderAlpha, baseScale, extraScale2, lightRotation);
			Display.RasterizerState = RasterizerState.CullNoneScissor;
		}
		shaderAlpha.MaterialColor = Vector4.One;
	}

	static ClothingPreviewWidget()
	{
		m_shader = new LitShader(1, useEmissionColor: false, useVertexColor: false, useTexture: true, useFog: false, useAlphaThreshold: false);
		m_shaderAlpha = new LitShader(1, useEmissionColor: false, useVertexColor: false, useTexture: true, useFog: false, useAlphaThreshold: true);
	}

	public void Zoom(float delta)
	{
		CameraZoom = MathUtils.Clamp(CameraZoom + delta, 0.5f, 3f);
	}

	public void Pan(Vector2 delta)
	{
		CameraPan += delta;
	}

	private void RequestModelUpdate()
	{
		m_bodyModel = CharacterSkinsManager.GetPlayerModel(PlayerClass);
		if (m_isOuter)
		{
			m_outerModel = CharacterSkinsManager.GetOuterClothingModel(PlayerClass);
		}
		else
		{
			m_outerModel = null;
		}
		if (m_bodyModel != null)
		{
			m_bodyBoneTransforms = new Matrix[m_bodyModel.Bones.Count];
			m_bodyModel.CopyAbsoluteBoneTransformsTo(m_bodyBoneTransforms);
		}
		if (m_outerModel != null)
		{
			m_outerBoneTransforms = new Matrix[m_outerModel.Bones.Count];
			m_outerModel.CopyAbsoluteBoneTransformsTo(m_outerBoneTransforms);
		}
	}

	private void SyncOuterClothingBones(Vector3 bodyScale)
	{
		for (int i = 0; i < m_outerModel.Bones.Count; i++)
		{
			ModelBone modelBone = m_outerModel.Bones[i];
			ModelBone modelBone2 = m_bodyModel.FindBone(modelBone.Name, throwIfNotFound: false);
			if (modelBone2 == null)
			{
				continue;
			}
			Matrix matrix = m_bodyBoneTransforms[modelBone2.Index];
			if (modelBone.Name == "Head")
			{
				matrix = Matrix.CreateRotationX((float)Math.PI) * matrix;
			}
			if (modelBone.Name == "Hand1" || modelBone.Name == "Hand2")
			{
				matrix = Matrix.CreateScale(1.03f) * matrix;
			}
			if (modelBone.Name == "Hand2")
			{
				matrix = Matrix.CreateScale(-1f, 1f, 1f) * matrix;
				if (PlayerClass == PlayerClass.Female)
				{
					matrix.Translation += matrix.Up * -0.05f;
				}
			}
			m_outerBoneTransforms[i] = matrix;
		}
	}

	private static Texture2D GetWhiteTexture()
	{
		if (m_dummyWhiteTexture == null)
		{
			Image image = new Image(1, 1);
			image.SetPixel(0, 0, Color.White);
			m_dummyWhiteTexture = Texture2D.Load(image);
		}
		return m_dummyWhiteTexture;
	}

	public void Setup(bool isOuter, ClothingSlot slot, Texture2D itemTexture, Color color)
	{
		if (SettingsManager.DebugMode)
		{
			Log.Information($"[PREVIEW DEBUG] Setup Called. Slot: {slot}. HasTexture: {itemTexture != null}");
		}
		if (m_clothingTexture != null && m_isTextureCloned)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Warning("[PREVIEW DEBUG] Disposing OLD m_clothingTexture (Cloned).");
			}
			m_clothingTexture.Dispose();
			m_clothingTexture = null;
		}
		m_currentSlot = slot;
		m_isOuter = isOuter;
		m_clothingColor = color;
		m_baseTexture = itemTexture;
		RequestModelUpdate();
		if (itemTexture != null)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Information($"[PREVIEW DEBUG] Cloning Texture. W:{itemTexture.Width} H:{itemTexture.Height}");
			}
			m_texWidth = itemTexture.Width;
			m_texHeight = itemTexture.Height;
			m_textureData = new Color[m_texWidth * m_texHeight];
			itemTexture.GetData<Color>(m_textureData);
			m_clothingTexture = new Texture2D(m_texWidth, m_texHeight, 1, ColorFormat.Rgba8888);
			m_clothingTexture.SetData(0, m_textureData);
			m_isTextureCloned = true;
		}
		else
		{
			if (SettingsManager.DebugMode)
			{
				Log.Information("[PREVIEW DEBUG] ItemTexture is NULL. m_clothingTexture set to NULL.");
			}
			m_clothingTexture = null;
			m_textureData = null;
			m_isTextureCloned = false;
			IsModified = false;
		}
	}

	private void DrawModel(Model model, Matrix[] transforms, LitShader shader, Vector3 baseScale, float extraScale, float lightRotation)
	{
		if (PlayerClass == PlayerClass.Female)
		{
			extraScale += 0.005f;
		}
		Matrix matrix = Matrix.CreateScale(baseScale * extraScale) * Matrix.CreateRotationY(lightRotation);
		foreach (ModelMesh mesh in model.Meshes)
		{
			string name = mesh.Name;
			if (mesh.ParentBone != null && string.IsNullOrEmpty(name))
			{
				name = mesh.ParentBone.Name;
			}
			if (HiddenParts.Contains(name))
			{
				continue;
			}
			int index = mesh.ParentBone.Index;
			shader.Transforms.World[0] = transforms[index] * matrix;
			foreach (ModelMeshPart meshPart in mesh.MeshParts)
			{
				if (meshPart.IndicesCount > 0)
				{
					Display.DrawIndexed(PrimitiveType.TriangleList, shader, meshPart.VertexBuffer, meshPart.IndexBuffer, meshPart.StartIndex, meshPart.IndicesCount);
				}
			}
		}
	}

	public void PaintTextureUV(Vector2 uv, Color color)
	{
		if (m_textureData == null)
		{
			return;
		}
		uv.X -= (float)Math.Floor(uv.X);
		uv.Y -= (float)Math.Floor(uv.Y);
		int num = (int)(uv.X * (float)m_texWidth);
		int num2 = (int)(uv.Y * (float)m_texHeight);
		if (ActiveTool != ToolType.Brush && ActiveTool != ToolType.Eraser)
		{
			return;
		}
		int num3 = MathUtils.Clamp(BrushSize, 1, 20);
		int num4 = (num3 - 1) / 2;
		int num5 = ((num3 % 2 == 0) ? 1 : 0);
		int num6 = MathUtils.Max(num2 - num4, 0);
		int num7 = MathUtils.Min(num2 + num4 + num5, m_texHeight - 1);
		int num8 = MathUtils.Max(num - num4, 0);
		int num9 = MathUtils.Min(num + num4 + num5, m_texWidth - 1);
		for (int i = num6; i <= num7; i++)
		{
			for (int j = num8; j <= num9; j++)
			{
				if (num3 > 2)
				{
					int num10 = j - num;
					int num11 = i - num2;
					if (num10 * num10 + num11 * num11 > num4 * num4 + 1)
					{
						j++;
						continue;
					}
				}
				int num12 = i * m_texWidth + j;
				if (m_paintedPixelsInStroke.Contains(num12))
				{
					continue;
				}
				Color color2 = m_textureData[num12];
				Color color3;
				if (ActiveTool == ToolType.Eraser)
				{
					color3 = Color.Transparent;
				}
				else if (color.A == byte.MaxValue)
				{
					color3 = color;
				}
				else
				{
					if (color.A == 0)
					{
						j++;
						continue;
					}
					float num13 = (float)(int)color.A / 255f;
					float num14 = 1f - num13;
					byte r = (byte)((float)(int)color.R * num13 + (float)(int)color2.R * num14);
					byte g = (byte)((float)(int)color.G * num13 + (float)(int)color2.G * num14);
					byte b = (byte)((float)(int)color.B * num13 + (float)(int)color2.B * num14);
					float num15 = (float)(int)color2.A / 255f;
					byte a = (byte)((num13 + num15 * num14) * 255f);
					color3 = new Color(r, g, b, a);
				}
				if (color2 != color3)
				{
					m_currentStroke.Add(new PixelChange
					{
						Index = num12,
						OldColor = color2,
						NewColor = color3
					});
					m_textureData[num12] = color3;
					m_isTextureDirty = true;
					IsModified = true;
					m_paintedPixelsInStroke.Add(num12);
				}
			}
		}
	}

	public void Paint3D(Vector2 screenPos, Color brushColor)
	{
		if (m_textureData == null)
		{
			return;
		}
		Ray3 ray = CreateRayFromScreen(screenPos);
		Model model = ((m_isOuter && m_outerModel != null) ? m_outerModel : m_bodyModel);
		Matrix[] array = ((m_isOuter && m_outerModel != null) ? m_outerBoneTransforms : m_bodyBoneTransforms);
		if (model == null)
		{
			return;
		}
		Vector3 scale = Vector3.One;
		switch (m_bodyTag)
		{
		case PlayerData.PlayerTag.Short:
			scale = new Vector3(0.85f, 0.85f, 0.85f);
			break;
		case PlayerData.PlayerTag.Fat:
			scale = new Vector3(1.5f, 1f, 1.5f);
			break;
		case PlayerData.PlayerTag.Dwarf:
			scale = new Vector3(1.25f, 0.7f, 1.25f);
			break;
		}
		float radians = (NoShadows ? (LightRotation - CameraRotation) : LightRotation);
		Matrix matrix = Matrix.CreateScale(scale) * Matrix.CreateRotationY(radians);
		float num = float.MaxValue;
		Vector2? vector = null;
		foreach (ModelMesh mesh in model.Meshes)
		{
			string name = mesh.Name;
			if (mesh.ParentBone != null && string.IsNullOrEmpty(name))
			{
				name = mesh.ParentBone.Name;
			}
			if (HiddenParts.Contains(name))
			{
				continue;
			}
			int index = mesh.ParentBone.Index;
			Matrix m = Matrix.Invert(array[index] * matrix);
			Vector3 position = Vector3.Transform(ray.Position, m);
			Vector3 v = Vector3.TransformNormal(ray.Direction, m);
			v = Vector3.Normalize(v);
			Ray3 ray2 = new Ray3(position, v);
			if (!ray2.Intersection(mesh.BoundingBox).HasValue)
			{
				continue;
			}
			foreach (ModelMeshPart meshPart in mesh.MeshParts)
			{
				byte[] array2 = meshPart.VertexBuffer.Tag as byte[];
				byte[] array3 = meshPart.IndexBuffer.Tag as byte[];
				if (array2 == null || array3 == null)
				{
					continue;
				}
				int num2 = 32;
				int indicesCount = meshPart.IndicesCount;
				int startIndex = meshPart.StartIndex;
				for (int i = 0; i < indicesCount; i += 3)
				{
					int num3 = BitConverter.ToUInt16(array3, (startIndex + i) * 2);
					int num4 = BitConverter.ToUInt16(array3, (startIndex + i + 1) * 2);
					int num5 = BitConverter.ToUInt16(array3, (startIndex + i + 2) * 2);
					Vector3 posFromBytes = GetPosFromBytes(array2, num3 * num2);
					Vector3 posFromBytes2 = GetPosFromBytes(array2, num4 * num2);
					Vector3 posFromBytes3 = GetPosFromBytes(array2, num5 * num2);
					if (RayIntersectsTriangle(ray2, posFromBytes, posFromBytes2, posFromBytes3, out var distance, out var barycentric) && distance < num)
					{
						num = distance;
						Vector2 uVFromBytes = GetUVFromBytes(array2, num3 * num2 + 24);
						Vector2 uVFromBytes2 = GetUVFromBytes(array2, num4 * num2 + 24);
						Vector2 uVFromBytes3 = GetUVFromBytes(array2, num5 * num2 + 24);
						vector = uVFromBytes * barycentric.X + uVFromBytes2 * barycentric.Y + uVFromBytes3 * barycentric.Z;
					}
				}
			}
		}
		if (vector.HasValue)
		{
			PaintTextureUV(vector.Value, brushColor);
		}
	}

	private Ray3 CreateRayFromScreen(Vector2 screenPos)
	{
		Viewport viewport = Display.Viewport;
		float aspectRatio = ActualSize.X / ActualSize.Y;
		float num = ((PlayerClass == PlayerClass.Male) ? 0.9f : 0.86f);
		float z = 2.5f / CameraZoom;
		float radians = (NoShadows ? LightRotation : (CameraRotation + LightRotation));
		Matrix m = Matrix.CreateRotationX(CameraRotationVertical) * Matrix.CreateRotationY(radians);
		Vector3 vector = Vector3.Transform(new Vector3(0f - CameraPan.X, 0f, 0f), Matrix.CreateRotationY(radians));
		Vector3 vector2 = new Vector3(0f, num - CameraPan.Y, 0f) + vector;
		Vector3 vector3 = Vector3.Transform(new Vector3(0f, 0f, z), m);
		Matrix view = Matrix.CreateLookAt(vector2 + vector3, vector2, Vector3.UnitY);
		Matrix matrix = Matrix.CreatePerspectiveFieldOfView(1f, aspectRatio, 0.1f, 100f);
		Matrix matrix2 = MatrixUtils.CreateScaleTranslation(ActualSize.X, 0f - ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f) * GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)viewport.Width, -2f / (float)viewport.Height, -1f, 1f);
		Matrix projection = matrix * matrix2;
		Vector3 vector4 = viewport.Unproject(new Vector3(screenPos.X, screenPos.Y, 0f), projection, view, Matrix.Identity);
		Vector3 v = viewport.Unproject(new Vector3(screenPos.X, screenPos.Y, 1f), projection, view, Matrix.Identity) - vector4;
		v = Vector3.Normalize(v);
		return new Ray3(vector4, v);
	}

	private bool RayIntersectsTriangle(Ray3 ray, Vector3 p0, Vector3 p1, Vector3 p2, out float distance, out Vector3 barycentric)
	{
		distance = 0f;
		barycentric = Vector3.Zero;
		Vector3 vector = p1 - p0;
		Vector3 vector2 = p2 - p0;
		Vector3 v = Vector3.Cross(ray.Direction, vector2);
		float num = Vector3.Dot(vector, v);
		if (num > -1E-05f && num < 1E-05f)
		{
			return false;
		}
		float num2 = 1f / num;
		Vector3 v2 = ray.Position - p0;
		float num3 = num2 * Vector3.Dot(v2, v);
		if (num3 < 0f || num3 > 1f)
		{
			return false;
		}
		Vector3 v3 = Vector3.Cross(v2, vector);
		float num4 = num2 * Vector3.Dot(ray.Direction, v3);
		if (num4 < 0f || num3 + num4 > 1f)
		{
			return false;
		}
		float num5 = num2 * Vector3.Dot(vector2, v3);
		if (num5 > 1E-05f)
		{
			distance = num5;
			barycentric = new Vector3(1f - num3 - num4, num3, num4);
			return true;
		}
		return false;
	}

	private Vector3 GetPosFromBytes(byte[] data, int offset)
	{
		return new Vector3(BitConverter.ToSingle(data, offset), BitConverter.ToSingle(data, offset + 4), BitConverter.ToSingle(data, offset + 8));
	}

	private Vector2 GetUVFromBytes(byte[] data, int offset)
	{
		return new Vector2(BitConverter.ToSingle(data, offset), BitConverter.ToSingle(data, offset + 4));
	}

	public override void UpdateCeases()
	{
		base.UpdateCeases();
	}

	public void BeginStroke()
	{
		m_redoHistory.Clear();
		m_currentStroke.Clear();
		m_paintedPixelsInStroke.Clear();
	}

	public void EndStroke()
	{
		if (m_currentStroke.Count > 0)
		{
			m_undoHistory.Add(new List<PixelChange>(m_currentStroke));
			m_currentStroke.Clear();
			if (m_undoHistory.Count > 100)
			{
				m_undoHistory.RemoveAt(0);
			}
		}
	}

	public void Undo()
	{
		if (m_undoHistory.Count > 0)
		{
			List<PixelChange> list = m_undoHistory[m_undoHistory.Count - 1];
			m_undoHistory.RemoveAt(m_undoHistory.Count - 1);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				m_textureData[list[num].Index] = list[num].OldColor;
			}
			m_redoHistory.Add(list);
			m_isTextureDirty = true;
		}
	}

	public void Redo()
	{
		if (m_redoHistory.Count <= 0)
		{
			return;
		}
		List<PixelChange> list = m_redoHistory[m_redoHistory.Count - 1];
		m_redoHistory.RemoveAt(m_redoHistory.Count - 1);
		foreach (PixelChange item in list)
		{
			m_textureData[item.Index] = item.NewColor;
		}
		m_undoHistory.Add(list);
		m_isTextureDirty = true;
	}

	public Color? PickColor(Vector2 uv)
	{
		if (m_textureData == null)
		{
			return null;
		}
		uv.X -= (float)Math.Floor(uv.X);
		uv.Y -= (float)Math.Floor(uv.Y);
		int num = (int)(uv.X * (float)m_texWidth);
		int num2 = (int)(uv.Y * (float)m_texHeight) * m_texWidth + num;
		if (num2 >= 0 && num2 < m_textureData.Length)
		{
			return m_textureData[num2];
		}
		return null;
	}

	public Color? Pick3D(Vector2 screenPos)
	{
		if (m_textureData == null)
		{
			return null;
		}
		Ray3 ray = CreateRayFromScreen(screenPos);
		Model model = ((m_isOuter && m_outerModel != null) ? m_outerModel : m_bodyModel);
		Matrix[] array = ((m_isOuter && m_outerModel != null) ? m_outerBoneTransforms : m_bodyBoneTransforms);
		if (model == null)
		{
			return null;
		}
		Vector3 scale = Vector3.One;
		switch (m_bodyTag)
		{
		case PlayerData.PlayerTag.Short:
			scale = new Vector3(0.85f, 0.85f, 0.85f);
			break;
		case PlayerData.PlayerTag.Fat:
			scale = new Vector3(1.5f, 1f, 1.5f);
			break;
		case PlayerData.PlayerTag.Dwarf:
			scale = new Vector3(1.25f, 0.7f, 1.25f);
			break;
		}
		float radians = (NoShadows ? (LightRotation - CameraRotation) : LightRotation);
		Matrix matrix = Matrix.CreateScale(scale) * Matrix.CreateRotationY(radians);
		float num = float.MaxValue;
		Vector2? vector = null;
		foreach (ModelMesh mesh in model.Meshes)
		{
			string name = mesh.Name;
			if (mesh.ParentBone != null && string.IsNullOrEmpty(name))
			{
				name = mesh.ParentBone.Name;
			}
			if (HiddenParts.Contains(name))
			{
				continue;
			}
			int index = mesh.ParentBone.Index;
			Matrix m = Matrix.Invert(array[index] * matrix);
			Vector3 position = Vector3.Transform(ray.Position, m);
			Vector3 v = Vector3.TransformNormal(ray.Direction, m);
			v = Vector3.Normalize(v);
			Ray3 ray2 = new Ray3(position, v);
			if (!ray2.Intersection(mesh.BoundingBox).HasValue)
			{
				continue;
			}
			foreach (ModelMeshPart meshPart in mesh.MeshParts)
			{
				byte[] array2 = meshPart.VertexBuffer.Tag as byte[];
				byte[] array3 = meshPart.IndexBuffer.Tag as byte[];
				if (array2 == null || array3 == null)
				{
					continue;
				}
				int num2 = 32;
				int indicesCount = meshPart.IndicesCount;
				int startIndex = meshPart.StartIndex;
				for (int i = 0; i < indicesCount; i += 3)
				{
					int num3 = BitConverter.ToUInt16(array3, (startIndex + i) * 2);
					int num4 = BitConverter.ToUInt16(array3, (startIndex + i + 1) * 2);
					int num5 = BitConverter.ToUInt16(array3, (startIndex + i + 2) * 2);
					Vector3 posFromBytes = GetPosFromBytes(array2, num3 * num2);
					Vector3 posFromBytes2 = GetPosFromBytes(array2, num4 * num2);
					Vector3 posFromBytes3 = GetPosFromBytes(array2, num5 * num2);
					if (RayIntersectsTriangle(ray2, posFromBytes, posFromBytes2, posFromBytes3, out var distance, out var barycentric) && distance < num)
					{
						num = distance;
						Vector2 uVFromBytes = GetUVFromBytes(array2, num3 * num2 + 24);
						Vector2 uVFromBytes2 = GetUVFromBytes(array2, num4 * num2 + 24);
						Vector2 uVFromBytes3 = GetUVFromBytes(array2, num5 * num2 + 24);
						vector = uVFromBytes * barycentric.X + uVFromBytes2 * barycentric.Y + uVFromBytes3 * barycentric.Z;
					}
				}
			}
		}
		if (vector.HasValue)
		{
			return PickColor(vector.Value);
		}
		return null;
	}

	public void FlushTexture()
	{
		if (m_isTextureDirty && m_clothingTexture != null && m_textureData != null)
		{
			if (!m_clothingTexture.IsDisposed())
			{
				m_clothingTexture.SetData(0, m_textureData);
			}
			m_isTextureDirty = false;
		}
	}

	public void DisposeTexture()
	{
		if (m_clothingTexture != null && m_isTextureCloned)
		{
			m_clothingTexture.Dispose();
			m_clothingTexture = null;
		}
	}

	public Color[] GetRawTextureData(out int w, out int h)
	{
		w = m_texWidth;
		h = m_texHeight;
		return m_textureData;
	}

	public void SetRawTextureData(Color[] data, int w, int h)
	{
		if (data != null)
		{
			m_texWidth = w;
			m_texHeight = h;
			m_textureData = data;
			if (m_clothingTexture != null)
			{
				m_clothingTexture.Dispose();
			}
			m_clothingTexture = new Texture2D(m_texWidth, m_texHeight, 1, ColorFormat.Rgba8888);
			m_isTextureCloned = true;
			m_isTextureDirty = true;
			IsModified = true;
		}
	}

	public Image GetImage()
	{
		if (m_textureData == null || m_texWidth <= 0)
		{
			return null;
		}
		Image image = new Image(m_texWidth, m_texHeight);
		for (int i = 0; i < m_textureData.Length; i++)
		{
			int x = i % m_texWidth;
			int y = i / m_texWidth;
			image.SetPixel(x, y, m_textureData[i]);
		}
		return image;
	}

	public void SetImage(Image img)
	{
		if (img == null)
		{
			return;
		}
		m_texWidth = img.Width;
		m_texHeight = img.Height;
		m_textureData = new Color[m_texWidth * m_texHeight];
		for (int i = 0; i < img.Height; i++)
		{
			for (int j = 0; j < img.Width; j++)
			{
				m_textureData[i * img.Width + j] = img.GetPixel(j, i);
			}
		}
		if (m_clothingTexture != null)
		{
			m_clothingTexture.Dispose();
		}
		m_clothingTexture = new Texture2D(m_texWidth, m_texHeight, 1, ColorFormat.Rgba8888);
		m_clothingTexture.SetData(0, m_textureData);
		m_isTextureCloned = true;
		m_isTextureDirty = false;
		IsModified = false;
		m_undoHistory.Clear();
		m_redoHistory.Clear();
	}
}
