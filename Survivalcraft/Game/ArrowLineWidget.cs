using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Engine.Serialization;

namespace Game;

public class ArrowLineWidget : Widget
{
	public string m_pointsString;

	public float m_width;

	public float m_arrowWidth;

	public bool m_absoluteCoordinates;

	public List<Vector2> m_vertices = new List<Vector2>();

	public bool m_parsingPending;

	public Vector2 m_startOffset;

	public float Width
	{
		get
		{
			return m_width;
		}
		set
		{
			m_width = value;
			m_parsingPending = true;
		}
	}

	public float ArrowWidth
	{
		get
		{
			return m_arrowWidth;
		}
		set
		{
			m_arrowWidth = value;
			m_parsingPending = true;
		}
	}

	public Color Color { get; set; }

	public string PointsString
	{
		get
		{
			return m_pointsString;
		}
		set
		{
			m_pointsString = value;
			m_parsingPending = true;
		}
	}

	public bool AbsoluteCoordinates
	{
		get
		{
			return m_absoluteCoordinates;
		}
		set
		{
			m_absoluteCoordinates = value;
			m_parsingPending = true;
		}
	}

	public ArrowLineWidget()
	{
		Width = 6f;
		ArrowWidth = 0f;
		Color = Color.White;
		IsHitTestVisible = false;
		PointsString = "0, 0; 50, 0";
	}

	public override void Draw(DrawContext dc)
	{
		if (m_parsingPending)
		{
			ParsePoints();
		}
		Color v = Color * GlobalColorTransform;
		FlatBatch2D num = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None);
		int num2 = num.TriangleVertices.Count;
		for (int i = 0; i < m_vertices.Count; i += 3)
		{
			Vector2 x = m_startOffset + m_vertices[i];
			Vector2 f = m_startOffset + m_vertices[i + 1];
			Vector2 p = m_startOffset + m_vertices[i + 2];
			num.QueueTriangle(x, f, p, 0f, v);
		}
		num.TransformTriangles(GlobalTransform, num2);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		if (m_parsingPending)
		{
			ParsePoints();
		}
		IsDrawRequired = Color.A > 0 && Width > 0f;
	}

	public void ParsePoints()
	{
		m_parsingPending = false;
		List<Vector2> array = new List<Vector2>();
		string[] triangleVertices = m_pointsString.Split(new string[1] { ";" }, StringSplitOptions.None);
		foreach (string num in triangleVertices)
		{
			array.Add(HumanReadableConverter.ConvertFromString<Vector2>(num));
		}
		m_vertices.Clear();
		for (int count = 0; count < array.Count; count++)
		{
			if (count >= 1)
			{
				Vector2 count2 = array[count - 1];
				Vector2 i = array[count];
				Vector2 j = Vector2.Normalize(i - count2);
				Vector2 num2 = j;
				Vector2 num3 = j;
				if (count >= 2)
				{
					num2 = Vector2.Normalize(count2 - array[count - 2]);
				}
				if (count <= array.Count - 2)
				{
					num3 = Vector2.Normalize(array[count + 1] - i);
				}
				Vector2 num4 = Vector2.Perpendicular(num2);
				Vector2 num5 = Vector2.Perpendicular(j);
				float num6 = (float)Math.PI - Vector2.Angle(num2, j);
				float num7 = 0.5f * Width / MathUtils.Tan(num6 / 2f);
				Vector2 num8 = 0.5f * num4 * Width - num2 * num7;
				float num9 = (float)Math.PI - Vector2.Angle(j, num3);
				float vector = 0.5f * Width / MathUtils.Tan(num9 / 2f);
				Vector2 num11 = 0.5f * num5 * Width - j * vector;
				m_vertices.Add(count2 + num8);
				m_vertices.Add(count2 - num8);
				m_vertices.Add(i - num11);
				m_vertices.Add(i - num11);
				m_vertices.Add(i + num11);
				m_vertices.Add(count2 + num8);
				if (count == array.Count - 1)
				{
					m_vertices.Add(i - 0.5f * ArrowWidth * num5);
					m_vertices.Add(i + 0.5f * ArrowWidth * num5);
					m_vertices.Add(i + 0.5f * ArrowWidth * j);
				}
			}
		}
		if (m_vertices.Count > 0)
		{
			float? f = null;
			float? num12 = null;
			float? num13 = null;
			float? a = null;
			for (int r = 0; r < m_vertices.Count; r++)
			{
				if (!f.HasValue || m_vertices[r].X < f)
				{
					f = m_vertices[r].X;
				}
				if (!num12.HasValue || m_vertices[r].Y < num12)
				{
					num12 = m_vertices[r].Y;
				}
				if (!num13.HasValue || m_vertices[r].X > num13)
				{
					num13 = m_vertices[r].X;
				}
				if (!a.HasValue || m_vertices[r].Y > a)
				{
					a = m_vertices[r].Y;
				}
			}
			if (AbsoluteCoordinates)
			{
				DesiredSize = new Vector2(num13.Value, a.Value);
				m_startOffset = Vector2.Zero;
			}
			else
			{
				DesiredSize = new Vector2(num13.Value - f.Value, a.Value - num12.Value);
				m_startOffset = -new Vector2(f.Value, num12.Value);
			}
		}
		else
		{
			DesiredSize = Vector2.Zero;
			m_startOffset = Vector2.Zero;
		}
	}
}
