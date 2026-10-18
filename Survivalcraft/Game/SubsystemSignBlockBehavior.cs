using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemSignBlockBehavior : SubsystemBlockBehavior, IDrawable, IUpdateable
{
	public class TextData
	{
		public Point3 Point;

		public string[] Lines = new string[4]
		{
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty
		};

		public Color[] Colors = new Color[4]
		{
			Color.Black,
			Color.Black,
			Color.Black,
			Color.Black
		};

		public string Url = string.Empty;

		public int? TextureLocation;

		public float UsedTextureWidth;

		public float UsedTextureHeight;

		public float Distance;

		public int ToBeRenderedFrame;

		public int Light;

		public Guid? OwnerGuid;

		public string OwnerName = string.Empty;
	}

	public const float m_maxVisibilityDistanceSqr = 400f;

	public const float m_minUpdateDistance = 2f;

	public const int m_textWidth = 128;

	public const int m_textHeight = 32;

	public const int m_maxTexts = 32;

	public SubsystemGameWidgets m_subsystemViews;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemGameInfo m_subsystemGameInfo;

	public Dictionary<Point3, TextData> m_textsByPoint;

	public List<RenderTarget2D> m_texturesByPoint;

	public TextData[] m_textureLocations;

	public List<TextData> m_nearTexts;

	public BitmapFont m_font;

	public RenderTarget2D m_renderTarget;

	public List<Vector3> m_lastUpdatePositions;

	public PrimitivesRenderer2D m_primitivesRenderer2D;

	public PrimitivesRenderer3D m_primitivesRenderer3D;

	public bool ShowSignsTexture;

	public bool CopySignsText;

	public static int[] m_drawOrders = new int[1] { 50 };

	private Dictionary<Point3, Tuple<Guid, string>> m_ownerInfoByPoint;

	public float m_textureScale = 1f;

	public override int[] HandledBlocks => new int[5] { 23, 97, 98, 210, 211 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => m_drawOrders;

	public SignData GetSignData(Point3 point)
	{
		if (m_textsByPoint.TryGetValue(point, out var value))
		{
			return new SignData
			{
				Lines = value.Lines.ToArray(),
				Colors = value.Colors.ToArray(),
				Url = value.Url
			};
		}
		return null;
	}

	public void SetSignData(Point3 point, string[] lines, Color[] colors, string url)
	{
		TextData textData = new TextData();
		textData.Point = point;
		for (int i = 0; i < 4; i++)
		{
			textData.Lines[i] = lines[i];
			textData.Colors[i] = colors[i];
		}
		textData.Url = url;
		m_textsByPoint[point] = textData;
		m_lastUpdatePositions.Clear();
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
		int num = Terrain.ExtractContents(cellValueFast);
		int data = Terrain.ExtractData(cellValueFast);
		Block block = BlocksManager.Blocks[num];
		if (block is AttachedSignBlock)
		{
			Point3 point = CellFace.FaceToPoint3(AttachedSignBlock.GetFace(data));
			int x2 = x - point.X;
			int y2 = y - point.Y;
			int z2 = z - point.Z;
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
			int num2 = Terrain.ExtractContents(cellValue);
			if (!BlocksManager.Blocks[num2].IsCollidable_(cellValue))
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}
		else if (block is PostedSignBlock)
		{
			int value = (PostedSignBlock.GetHanging(data) ? SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z) : SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z));
			if (!BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value))
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		Point3 i = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure)
		{
			SignData signData = GetSignData(i);
			if (signData != null && !string.IsNullOrEmpty(signData.Url))
			{
				WebBrowserManager.LaunchBrowser(signData.Url);
			}
		}
		else if (componentMiner.ComponentPlayer != null)
		{
			if (CommonLib.WorkType == WorkType.Client && componentMiner.ComponentPlayer == CommonLib.MainPlayer)
			{
				BlockEditPackage package = new BlockEditPackage(i, BlockEditPackage.EventType.EditSign);
				CommonLib.Net.QueuePackage(package);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				return true;
			}
			PlayerData playerData = componentMiner.ComponentPlayer.PlayerData;
			if (playerData.IsMainPlayer)
			{
				if (!m_ownerInfoByPoint.TryGetValue(i, out var value) || value.Item1 == playerData.PlayerGUID)
				{
					m_ownerInfoByPoint[i] = Tuple.Create(playerData.PlayerGUID, playerData.Name);
					DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GuiWidget, new EditSignDialog(this, i));
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
				else
				{
					string text = LanguageControl.Get("SubsystemSignBlockBehavior", "not_sign_owner");
					componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Yellow, blinking: false, playNotificationSound: true);
				}
			}
		}
		return true;
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		Point3 data = new Point3(x, y, z);
		m_textsByPoint.Remove(data);
		m_lastUpdatePositions.Clear();
	}

	public void Update(float dt)
	{
		UpdateRenderTarget();
	}

	public void Draw(Camera camera, int drawOrder)
	{
		DrawSigns(camera);
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		CreateRenderTarget();
		foreach (ValuesDictionary value13 in valuesDictionary.GetValue<ValuesDictionary>("Texts").Values)
		{
			Point3 value = value13.GetValue<Point3>("Point");
			string value2 = value13.GetValue("Line1", string.Empty);
			string value3 = value13.GetValue("Line2", string.Empty);
			string value4 = value13.GetValue("Line3", string.Empty);
			string value5 = value13.GetValue("Line4", string.Empty);
			Color value6 = value13.GetValue("Color1", Color.Black);
			Color value7 = value13.GetValue("Color2", Color.Black);
			Color value8 = value13.GetValue("Color3", Color.Black);
			Color value9 = value13.GetValue("Color4", Color.Black);
			string value10 = value13.GetValue("Url", string.Empty);
			Guid value11 = value13.GetValue("OwnerGuid", Guid.Empty);
			string value12 = value13.GetValue("OwnerName", string.Empty);
			if (value11 != Guid.Empty)
			{
				m_ownerInfoByPoint[value] = Tuple.Create(value11, value12);
			}
			SetSignData(value, new string[4] { value2, value3, value4, value5 }, new Color[4] { value6, value7, value8, value9 }, value10);
		}
		Display.DeviceReset += Display_DeviceReset;
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		int num = 0;
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Texts", valuesDictionary2);
		foreach (TextData value2 in m_textsByPoint.Values)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary3.SetValue("Point", value2.Point);
			if (!string.IsNullOrEmpty(value2.Lines[0]))
			{
				valuesDictionary3.SetValue("Line1", value2.Lines[0]);
			}
			if (!string.IsNullOrEmpty(value2.Lines[1]))
			{
				valuesDictionary3.SetValue("Line2", value2.Lines[1]);
			}
			if (!string.IsNullOrEmpty(value2.Lines[2]))
			{
				valuesDictionary3.SetValue("Line3", value2.Lines[2]);
			}
			if (!string.IsNullOrEmpty(value2.Lines[3]))
			{
				valuesDictionary3.SetValue("Line4", value2.Lines[3]);
			}
			if (value2.Colors[0] != Color.Black)
			{
				valuesDictionary3.SetValue("Color1", value2.Colors[0]);
			}
			if (value2.Colors[1] != Color.Black)
			{
				valuesDictionary3.SetValue("Color2", value2.Colors[1]);
			}
			if (value2.Colors[2] != Color.Black)
			{
				valuesDictionary3.SetValue("Color3", value2.Colors[2]);
			}
			if (value2.Colors[3] != Color.Black)
			{
				valuesDictionary3.SetValue("Color4", value2.Colors[3]);
			}
			if (!string.IsNullOrEmpty(value2.Url))
			{
				valuesDictionary3.SetValue("Url", value2.Url);
			}
			if (m_ownerInfoByPoint.TryGetValue(value2.Point, out var value))
			{
				valuesDictionary3.SetValue("OwnerGuid", value.Item1);
				valuesDictionary3.SetValue("OwnerName", value.Item2);
			}
			valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
		}
	}

	public override void Dispose()
	{
		Utilities.Dispose(ref m_renderTarget);
		Display.DeviceReset -= Display_DeviceReset;
	}

	public void Display_DeviceReset()
	{
		InvalidateRenderTarget();
	}

	public void CreateRenderTarget()
	{
		m_textureScale = 1f;
		float designIndex = m_font.GlyphHeight * 4f * 32f;
		if (SettingsManager.LowResSignsTexture && designIndex > 2048f)
		{
			m_textureScale = 2048f / designIndex;
		}
		int design = (int)MathUtils.NextPowerOf2((uint)(m_font.GlyphHeight * 16f * m_textureScale));
		int mainValue = (int)MathUtils.NextPowerOf2((uint)(designIndex * m_textureScale));
		if (SettingsManager.LowResSignsTexture)
		{
			design = MathUtils.Clamp(design, 16, 2048);
			mainValue = MathUtils.Clamp(mainValue, 16, 2048);
		}
		m_renderTarget = new RenderTarget2D(design, mainValue, 1, ColorFormat.Rgba8888, DepthFormat.None);
	}

	public void InvalidateRenderTarget()
	{
		m_lastUpdatePositions.Clear();
		for (int designIndex = 0; designIndex < m_textureLocations.Length; designIndex++)
		{
			m_textureLocations[designIndex] = null;
		}
		foreach (TextData value in m_textsByPoint.Values)
		{
			value.TextureLocation = null;
		}
	}

	public void RenderText(FontBatch2D fontBatch, FlatBatch2D flatBatch, TextData textData)
	{
		if (!textData.TextureLocation.HasValue)
		{
			return;
		}
		List<string> designIndex = new List<string>();
		List<Color> rotation = new List<Color>();
		for (int design = 0; design < textData.Lines.Length; design++)
		{
			if (!string.IsNullOrEmpty(textData.Lines[design]))
			{
				designIndex.Add(textData.Lines[design].Replace("\\", "").ToUpper());
				rotation.Add(textData.Colors[design]);
			}
		}
		if (designIndex.Count > 0)
		{
			float num = (float)designIndex.Max((string l) => l.Length) * m_font.GlyphHeight;
			float num2 = (float)designIndex.Count * m_font.GlyphHeight;
			float num3 = 4f;
			float num4;
			float num5;
			if (num / num2 < num3)
			{
				num4 = num2 * num3;
				num5 = num2;
			}
			else
			{
				num4 = num;
				num5 = num / num3;
			}
			bool flag = !string.IsNullOrEmpty(textData.Url);
			for (int num6 = 0; num6 < designIndex.Count; num6++)
			{
				Vector2 position = new Vector2(num4 / 2f, (float)num6 * m_font.GlyphHeight + (float)textData.TextureLocation.Value * (4f * m_font.GlyphHeight) + (num5 - num2) / 2f);
				position *= m_textureScale;
				fontBatch.QueueText(designIndex[num6], position, 0f, flag ? new Color(0, 0, 64) : rotation[num6], TextAnchor.HorizontalCenter, new Vector2(m_textureScale / m_font.Scale), Vector2.Zero);
			}
			textData.UsedTextureWidth = num4 * m_textureScale;
			textData.UsedTextureHeight = num5 * m_textureScale;
		}
	}

	public void UpdateRenderTarget()
	{
		bool faceTextureSlot = false;
		foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
		{
			bool flag = false;
			foreach (Vector3 lastUpdatePosition in m_lastUpdatePositions)
			{
				if (Vector3.DistanceSquared(gameWidget.ActiveCamera.ViewPosition, lastUpdatePosition) < 4f)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				faceTextureSlot = true;
				break;
			}
		}
		if (!faceTextureSlot)
		{
			return;
		}
		m_lastUpdatePositions.Clear();
		m_lastUpdatePositions.AddRange(m_subsystemViews.GameWidgets.Select((GameWidget v) => v.ActiveCamera.ViewPosition));
		m_nearTexts.Clear();
		foreach (TextData value in m_textsByPoint.Values)
		{
			Point3 point = value.Point;
			float num = m_subsystemViews.CalculateSquaredDistanceFromNearestView(new Vector3(point));
			if (num <= 400f)
			{
				value.Distance = num;
				m_nearTexts.Add(value);
			}
		}
		m_nearTexts.Sort((TextData d1, TextData d2) => Comparer<float>.Default.Compare(d1.Distance, d2.Distance));
		if (m_nearTexts.Count > 32)
		{
			m_nearTexts.RemoveRange(32, m_nearTexts.Count - 32);
		}
		foreach (TextData nearText in m_nearTexts)
		{
			nearText.ToBeRenderedFrame = Time.FrameIndex;
		}
		bool designIndex = false;
		for (int num2 = 0; num2 < MathUtils.Min(m_nearTexts.Count, 32); num2++)
		{
			TextData textData = m_nearTexts[num2];
			if (textData.TextureLocation.HasValue)
			{
				continue;
			}
			int num3 = m_textureLocations.FirstIndex((TextData d) => d == null);
			if (num3 < 0)
			{
				num3 = m_textureLocations.FirstIndex((TextData d) => d.ToBeRenderedFrame != Time.FrameIndex);
			}
			if (num3 >= 0)
			{
				TextData textData2 = m_textureLocations[num3];
				if (textData2 != null)
				{
					textData2.TextureLocation = null;
					m_textureLocations[num3] = null;
				}
				m_textureLocations[num3] = textData;
				textData.TextureLocation = num3;
				designIndex = true;
			}
		}
		if (!designIndex)
		{
			return;
		}
		RenderTarget2D design = Display.RenderTarget;
		Display.RenderTarget = m_renderTarget;
		try
		{
			Display.Clear(new Vector4(Color.Transparent));
			FlatBatch2D flatBatch = m_primitivesRenderer2D.FlatBatch(0, DepthStencilState.None, null, BlendState.Opaque);
			FontBatch2D fontBatch = m_primitivesRenderer2D.FontBatch(m_font, 1, DepthStencilState.None, null, BlendState.Opaque, SamplerState.PointClamp);
			for (int num4 = 0; num4 < m_textureLocations.Length; num4++)
			{
				TextData textData3 = m_textureLocations[num4];
				if (textData3 != null)
				{
					RenderText(fontBatch, flatBatch, textData3);
				}
			}
			m_primitivesRenderer2D.Flush();
		}
		finally
		{
			Display.RenderTarget = design;
		}
	}

	public void DrawSigns(Camera camera)
	{
		if (m_nearTexts.Count <= 0)
		{
			return;
		}
		TexturedBatch3D rotation = m_primitivesRenderer3D.TexturedBatch(m_renderTarget, useAlphaTest: false, 0, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
		foreach (TextData num in m_nearTexts)
		{
			if (!num.TextureLocation.HasValue)
			{
				continue;
			}
			int num2 = m_subsystemTerrain.Terrain.GetCellValue(num.Point.X, num.Point.Y, num.Point.Z);
			int num3 = Terrain.ExtractContents(num2);
			if (!(BlocksManager.Blocks[num3] is SignBlock num4))
			{
				continue;
			}
			int data2 = Terrain.ExtractData(num2);
			BlockMesh signSurfaceBlockMesh = num4.GetSignSurfaceBlockMesh(data2);
			if (signSurfaceBlockMesh != null)
			{
				TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(num.Point.X, num.Point.Z);
				if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1)
				{
					num.Light = Terrain.ExtractLight(num2);
				}
				float num5 = LightingManager.LightIntensityByLightValue[num.Light];
				Color color = new Color(num5, num5, num5);
				float x = 0f;
				float x2 = num.UsedTextureWidth / (float)m_renderTarget.Width;
				float num6 = (float)num.TextureLocation.Value * m_font.GlyphHeight * 4f * m_textureScale / (float)m_renderTarget.Height;
				float x3 = num6 + num.UsedTextureHeight / (float)m_renderTarget.Height;
				Vector3 signSurfaceNormal = num4.GetSignSurfaceNormal(data2);
				Vector3 vector = new Vector3(num.Point.X, num.Point.Y, num.Point.Z);
				float num7 = Vector3.Dot(camera.ViewPosition - (vector + new Vector3(0.5f)), signSurfaceNormal);
				Vector3 vector2 = MathUtils.Max(0.01f * num7, 0.005f) * signSurfaceNormal;
				for (int i = 0; i < signSurfaceBlockMesh.Indices.Count / 3; i++)
				{
					BlockMeshVertex blockMeshVertex = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3]];
					BlockMeshVertex blockMeshVertex2 = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3 + 1]];
					BlockMeshVertex blockMeshVertex3 = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3 + 2]];
					Vector3 p = blockMeshVertex.Position + vector + vector2;
					Vector3 p2 = blockMeshVertex2.Position + vector + vector2;
					Vector3 p3 = blockMeshVertex3.Position + vector + vector2;
					Vector2 textureCoordinates = blockMeshVertex.TextureCoordinates;
					Vector2 textureCoordinates2 = blockMeshVertex2.TextureCoordinates;
					Vector2 textureCoordinates3 = blockMeshVertex3.TextureCoordinates;
					textureCoordinates.X = MathUtils.Lerp(x, x2, textureCoordinates.X);
					textureCoordinates2.X = MathUtils.Lerp(x, x2, textureCoordinates2.X);
					textureCoordinates3.X = MathUtils.Lerp(x, x2, textureCoordinates3.X);
					textureCoordinates.Y = MathUtils.Lerp(num6, x3, textureCoordinates.Y);
					textureCoordinates2.Y = MathUtils.Lerp(num6, x3, textureCoordinates2.Y);
					textureCoordinates3.Y = MathUtils.Lerp(num6, x3, textureCoordinates3.Y);
					rotation.QueueTriangle(p, p2, p3, textureCoordinates, textureCoordinates2, textureCoordinates3, color);
				}
			}
		}
		m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
	}

	public SubsystemSignBlockBehavior()
	{
		m_ownerInfoByPoint = new Dictionary<Point3, Tuple<Guid, string>>();
		m_textsByPoint = new Dictionary<Point3, TextData>();
		m_texturesByPoint = new List<RenderTarget2D>();
		m_textureLocations = new TextData[32];
		m_nearTexts = new List<TextData>();
		m_font = LabelWidget.BitmapFont;
		m_lastUpdatePositions = new List<Vector3>();
		m_primitivesRenderer2D = new PrimitivesRenderer2D();
		m_primitivesRenderer3D = new PrimitivesRenderer3D();
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		base.OnBlockAdded(value, oldValue, x, y, z, miner);
		int value2 = Terrain.ExtractContents(value);
		if (Array.IndexOf(HandledBlocks, value2) >= 0 && miner?.ComponentPlayer != null)
		{
			Point3 key = new Point3(x, y, z);
			PlayerData playerData = miner.ComponentPlayer.PlayerData;
			m_ownerInfoByPoint[key] = Tuple.Create(playerData.PlayerGUID, playerData.Name);
		}
	}
}
