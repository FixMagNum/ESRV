using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class ShipPreviewWidget : Widget
{
	public enum ViewMode
	{
		Perspective,
		Front,
		Side,
		Top
	}

	private struct PreviewBlock
	{
		public int X;

		public int Y;

		public int Z;

		public int Value;
	}

	private SubsystemTerrain m_terrain;

	private BoundingBox m_bounds;

	private float m_scale;

	private Vector3 m_centerOffset;

	private PrimitivesRenderer3D m_primitivesRenderer3d;

	private Vector3 m_direction;

	private Vector2? m_dragStartPoint;

	private float m_waterRadius;

	private List<PreviewBlock> m_displayBlocks = new List<PreviewBlock>();

	private Vector3 m_centerOfMassLocal;

	private float m_waterLevelRender;

	private float m_waterTiltX;

	private float m_waterTiltZ;

	public ViewMode Mode { get; set; }

	public bool ShowWater { get; set; }

	public bool ShowCoM { get; set; }

	public Vector2 Size { get; set; } = new Vector2(float.PositiveInfinity);

	public Color CoMColor { get; set; } = Color.Yellow;

	public ShipPreviewWidget()
	{
		m_primitivesRenderer3d = new PrimitivesRenderer3D();
		m_direction = Vector3.Normalize(new Vector3(1f, 0.5f, 1f));
		ClampToBounds = true;
	}

	public override void Update()
	{
		if (Mode == ViewMode.Perspective)
		{
			if (Input.Tap.HasValue && HitTestGlobal(Input.Tap.Value) == this)
			{
				m_dragStartPoint = Input.Tap;
			}
			if (Input.Press.HasValue && m_dragStartPoint.HasValue)
			{
				Vector2 vector = ScreenToWidget(Input.Press.Value);
				Vector2 componentCraftingTable = ScreenToWidget(m_dragStartPoint.Value);
				Vector2 vector2 = (vector - componentCraftingTable) * 0.01f;
				Rotate(new Vector2(0f - vector2.Y, 0f - vector2.X));
				m_dragStartPoint = Input.Press;
			}
			else
			{
				m_dragStartPoint = null;
				Rotate(new Vector2(0f, 1f) * Time.FrameDuration * 0.2f);
			}
		}
	}

	private void Rotate(Vector2 angles)
	{
		Matrix m = Matrix.CreateRotationX(angles.X) * Matrix.CreateRotationY(angles.Y);
		m_direction = Vector3.TransformNormal(m_direction, m);
		m_direction = Vector3.Normalize(m_direction);
	}

	public override void Draw(DrawContext dc)
	{
		if (m_displayBlocks.Count == 0)
		{
			return;
		}
		float num = ActualSize.X / ActualSize.Y;
		Matrix matrix;
		Matrix matrix2;
		if (Mode == ViewMode.Perspective)
		{
			matrix = Matrix.CreateLookAt(m_direction * 3.5f, Vector3.Zero, Vector3.UnitY);
			matrix2 = Matrix.CreatePerspectiveFieldOfView(MathUtils.DegToRad(45f), num, 0.1f, 1000f);
		}
		else
		{
			Vector3 position = Vector3.Zero;
			Vector3 up = Vector3.UnitY;
			if (Mode == ViewMode.Top)
			{
				position = 10f * Vector3.UnitY;
				up = Vector3.UnitZ;
			}
			else if (Mode == ViewMode.Side)
			{
				position = 10f * Vector3.UnitX;
			}
			else if (Mode == ViewMode.Front)
			{
				position = -10f * Vector3.UnitZ;
			}
			matrix = Matrix.CreateLookAt(position, Vector3.Zero, up);
			matrix2 = Matrix.CreateOrthographic(2f * num, 2f, 0.1f, 1000f);
		}
		Matrix globalTransform = GlobalTransform;
		Matrix matrix3 = Matrix.CreateRotationX(m_waterTiltX) * Matrix.CreateRotationZ(m_waterTiltZ);
		Matrix matrix4 = Matrix.CreateTranslation(m_centerOffset) * matrix3 * Matrix.CreateScale(m_scale);
		DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData
		{
			SubsystemTerrain = m_terrain,
			Light = 15,
			Humidity = 15,
			Temperature = 15
		};
		foreach (PreviewBlock displayBlock in m_displayBlocks)
		{
			int num2 = Terrain.ExtractContents(displayBlock.Value);
			if (num2 != 0)
			{
				Matrix matrix5 = (drawBlockEnvironmentData.InWorldMatrix = Matrix.CreateTranslation(displayBlock.X, displayBlock.Y, displayBlock.Z) * matrix4);
				BlocksManager.Blocks[num2].DrawBlock(m_primitivesRenderer3d, displayBlock.Value, Color.White, 1f, ref matrix5, drawBlockEnvironmentData);
			}
		}
		if (ShowWater)
		{
			FlatBatch3D flatBatch3D = m_primitivesRenderer3d.FlatBatch(1, DepthStencilState.DepthRead, null, BlendState.AlphaBlend);
			Color color = new Color(20, 35, 60, 180);
			Matrix identity = Matrix.Identity;
			Vector3 vector = new Vector3(0f, m_waterLevelRender, 0f);
			int num3 = 40;
			float waterRadius = m_waterRadius;
			for (int i = 0; i < num3; i++)
			{
				float x = (float)i / (float)num3 * (float)Math.PI * 2f;
				float x2 = (float)(i + 1) / (float)num3 * (float)Math.PI * 2f;
				Vector3 v = new Vector3(MathUtils.Cos(x) * waterRadius, 0f, MathUtils.Sin(x) * waterRadius);
				Vector3 v2 = new Vector3(MathUtils.Cos(x2) * waterRadius, 0f, MathUtils.Sin(x2) * waterRadius);
				Vector3 zero = Vector3.Zero;
				Vector3 vector2 = Vector3.Transform(v, identity) + vector;
				Vector3 vector3 = Vector3.Transform(v2, identity) + vector;
				Vector3 p = Vector3.Transform(zero, identity) + vector;
				flatBatch3D.QueueTriangle(p, vector2, vector3, color);
				flatBatch3D.QueueTriangle(p, vector3, vector2, color);
			}
		}
		if (ShowCoM)
		{
			Vector3 vector4 = Vector3.Transform(m_centerOfMassLocal + m_centerOffset, matrix3) * m_scale;
			FlatBatch3D flatBatch3D2 = m_primitivesRenderer3d.FlatBatch(2, DepthStencilState.None, null, BlendState.AlphaBlend);
			float num4 = 0.15f;
			Vector3 vector5 = Vector3.TransformNormal(Vector3.UnitX, matrix3) * num4;
			Vector3 vector6 = Vector3.TransformNormal(Vector3.UnitY, matrix3) * num4;
			Vector3 vector7 = Vector3.TransformNormal(Vector3.UnitZ, matrix3) * num4;
			flatBatch3D2.QueueLine(vector4 - vector5, vector4 + vector5, CoMColor);
			flatBatch3D2.QueueLine(vector4 - vector6, vector4 + vector6, CoMColor);
			flatBatch3D2.QueueLine(vector4 - vector7, vector4 + vector7, CoMColor);
		}
		Viewport viewport = Display.Viewport;
		Matrix matrix6 = MatrixUtils.CreateScaleTranslation(ActualSize.X, 0f - ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f) * globalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)viewport.Width, -2f / (float)viewport.Height, -1f, 1f);
		m_primitivesRenderer3d.Flush(matrix * matrix2 * matrix6);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = m_displayBlocks.Count > 0;
		DesiredSize = Size;
	}

	public void Setup(List<Point3> blocks, SubsystemTerrain terrain, Vector3 comWorld, Vector3 geomCenterWorld, float waterLevelY)
	{
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		int num4 = int.MinValue;
		int num5 = int.MinValue;
		int num6 = int.MinValue;
		foreach (Point3 block in blocks)
		{
			num = Math.Min(num, block.X);
			num2 = Math.Min(num2, block.Y);
			num3 = Math.Min(num3, block.Z);
			num4 = Math.Max(num4, block.X);
			num5 = Math.Max(num5, block.Y);
			num6 = Math.Max(num6, block.Z);
		}
		int val = num4 - num + 1;
		int num7 = num5 - num2 + 1;
		int val2 = num6 - num3 + 1;
		int num8 = Math.Max(val, Math.Max(num7, val2));
		int num9 = 48;
		int num10 = Math.Max(1, (num8 + num9 - 1) / num9);
		Dictionary<Point3, int> dictionary = new Dictionary<Point3, int>();
		m_displayBlocks.Clear();
		foreach (Point3 block2 in blocks)
		{
			int num11 = block2.X - num;
			int num12 = block2.Y - num2;
			int num13 = block2.Z - num3;
			Point3 key = new Point3(num11 / num10, num12 / num10, num13 / num10);
			int cellValue = terrain.Terrain.GetCellValue(block2.X, block2.Y, block2.Z);
			if (!dictionary.ContainsKey(key))
			{
				dictionary[key] = cellValue;
				continue;
			}
			int num14 = Terrain.ExtractContents(dictionary[key]);
			int num15 = Terrain.ExtractContents(cellValue);
			bool num16 = num14 == 0 || BlocksManager.Blocks[num14] is FluidBlock;
			bool flag = num15 != 0 && !(BlocksManager.Blocks[num15] is FluidBlock);
			if (num16 & flag)
			{
				dictionary[key] = cellValue;
			}
		}
		int num17 = int.MaxValue;
		int num18 = int.MaxValue;
		int num19 = int.MaxValue;
		int num20 = int.MinValue;
		int num21 = int.MinValue;
		int num22 = int.MinValue;
		foreach (KeyValuePair<Point3, int> item in dictionary)
		{
			m_displayBlocks.Add(new PreviewBlock
			{
				X = item.Key.X,
				Y = item.Key.Y,
				Z = item.Key.Z,
				Value = item.Value
			});
			num17 = Math.Min(num17, item.Key.X);
			num18 = Math.Min(num18, item.Key.Y);
			num19 = Math.Min(num19, item.Key.Z);
			num20 = Math.Max(num20, item.Key.X);
			num21 = Math.Max(num21, item.Key.Y);
			num22 = Math.Max(num22, item.Key.Z);
		}
		m_bounds = new BoundingBox(new Vector3(num17, num18, num19), new Vector3(num20 + 1, num21 + 1, num22 + 1));
		Vector3 vector = (m_bounds.Min + m_bounds.Max) / 2f;
		m_centerOffset = -vector;
		float num23 = Vector3.Distance(m_bounds.Min, m_bounds.Max);
		m_scale = Math.Min(1.8f / Math.Max(0.1f, num23), 0.8f);
		float x = (comWorld.X - (float)num) / (float)num10;
		float y = (comWorld.Y - (float)num2) / (float)num10;
		float z = (comWorld.Z - (float)num3) / (float)num10;
		m_centerOfMassLocal = new Vector3(x, y, z);
		float num24 = (waterLevelY - (float)num2) / (float)num10;
		m_waterLevelRender = (num24 + m_centerOffset.Y) * m_scale;
		m_waterRadius = num23 / 2f * 1.5f * m_scale;
		float num25 = comWorld.X - geomCenterWorld.X;
		float num26 = comWorld.Z - geomCenterWorld.Z;
		float num27 = Math.Max(1f, (float)num7 / 5f);
		m_waterTiltZ = (0f - num25) * 0.05f * num27;
		m_waterTiltX = num26 * 0.05f * num27;
		m_waterTiltX = MathUtils.Clamp(m_waterTiltX, -0.5f, 0.5f);
		m_waterTiltZ = MathUtils.Clamp(m_waterTiltZ, -0.5f, 0.5f);
		m_terrain = terrain;
	}
}
