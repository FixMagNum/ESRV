using Engine;
using Engine.Graphics;

namespace Game;

public class ScrollPanelWidget : ContainerWidget
{
	public Vector2? m_lastDragPosition;

	public float m_dragSpeed;

	public float m_scrollBarAlpha;

	public float m_scrollAreaLength;

	public virtual LayoutDirection Direction { get; set; }

	public virtual float ScrollPosition { get; set; }

	public virtual float ScrollSpeed { get; set; }

	public ScrollPanelWidget()
	{
		ClampToBounds = true;
		StartInitialScroll();
	}

	public void StartInitialScroll()
	{
		ScrollPosition = 12f;
		ScrollSpeed = -70f;
	}

	public virtual float CalculateScrollAreaLength()
	{
		float terrain = 0f;
		foreach (Widget y in Children)
		{
			if (y.IsVisible)
			{
				terrain = ((Direction != LayoutDirection.Horizontal) ? MathUtils.Max(terrain, y.ParentDesiredSize.Y + 2f * y.Margin.Y) : MathUtils.Max(terrain, y.ParentDesiredSize.X + 2f * y.Margin.X));
			}
		}
		return terrain;
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		foreach (Widget num in Children)
		{
			if (num.IsVisible)
			{
				if (Direction == LayoutDirection.Horizontal)
				{
					num.Measure(new Vector2(float.MaxValue, MathUtils.Max(parentAvailableSize.Y - 2f * num.Margin.Y, 0f)));
				}
				else
				{
					num.Measure(new Vector2(MathUtils.Max(parentAvailableSize.X - 2f * num.Margin.X, 0f), float.MaxValue));
				}
			}
		}
	}

	public override void ArrangeOverride()
	{
		foreach (Widget y in Children)
		{
			Vector2 random = Vector2.Zero;
			Vector2 num = ActualSize;
			if (Direction == LayoutDirection.Horizontal)
			{
				random.X -= ScrollPosition;
				num.X = random.X + y.ParentDesiredSize.X;
			}
			else
			{
				random.Y -= ScrollPosition;
				num.Y = random.Y + y.ParentDesiredSize.Y;
			}
			ContainerWidget.ArrangeChildWidgetInCell(random, num, y);
		}
	}

	public override void Update()
	{
		float x = 50f;
		m_scrollAreaLength = CalculateScrollAreaLength();
		m_scrollBarAlpha = MathUtils.Max(m_scrollBarAlpha - 2f * Time.FrameDuration, 0f);
		if (Input.Tap.HasValue && HitTestPanel(Input.Tap.Value))
		{
			m_lastDragPosition = ScreenToWidget(Input.Tap.Value);
		}
		if (m_lastDragPosition.HasValue)
		{
			if (Input.Press.HasValue)
			{
				Vector2 i = ScreenToWidget(Input.Press.Value);
				Vector2 j = i - m_lastDragPosition.Value;
				float num;
				if (Direction == LayoutDirection.Horizontal)
				{
					ScrollPosition += 0f - j.X;
					num = j.X / Time.FrameDuration;
				}
				else
				{
					ScrollPosition += 0f - j.Y;
					num = j.Y / Time.FrameDuration;
				}
				float num2 = ((MathUtils.Abs(num) < MathUtils.Abs(m_dragSpeed)) ? 20f : 16f);
				m_dragSpeed += MathUtils.Saturate(num2 * Time.FrameDuration) * (num - m_dragSpeed);
				m_scrollBarAlpha = 4f;
				m_lastDragPosition = i;
				ScrollSpeed = 0f;
			}
			else
			{
				ScrollSpeed = 0f - m_dragSpeed;
				m_dragSpeed = 0f;
				m_lastDragPosition = null;
			}
		}
		if (ScrollSpeed != 0f)
		{
			ScrollSpeed *= MathUtils.Pow(0.33f, Time.FrameDuration);
			if (MathUtils.Abs(ScrollSpeed) < 40f)
			{
				ScrollSpeed = 0f;
			}
			ScrollPosition += ScrollSpeed * Time.FrameDuration;
			m_scrollBarAlpha = 3f;
		}
		if (Input.Scroll.HasValue && HitTestPanel(Input.Scroll.Value.XY))
		{
			ScrollPosition -= 40f * Input.Scroll.Value.Z;
			ScrollSpeed = 0f;
			x = 0f;
			m_scrollBarAlpha = 3f;
		}
		float y = MathUtils.Max(m_scrollAreaLength - ActualSize.Y, 0f);
		if (ScrollPosition < 0f)
		{
			if (!m_lastDragPosition.HasValue)
			{
				ScrollPosition = MathUtils.Min(ScrollPosition + 6f * Time.FrameDuration * (0f - ScrollPosition + 5f), 0f);
			}
			ScrollPosition = MathUtils.Max(ScrollPosition, 0f - x);
			ScrollSpeed = 0f;
		}
		if (ScrollPosition > y)
		{
			if (!m_lastDragPosition.HasValue && !float.IsPositiveInfinity(ScrollPosition))
			{
				ScrollPosition = MathUtils.Max(ScrollPosition + 6f * Time.FrameDuration * (y - ScrollPosition - 5f), y);
			}
			ScrollPosition = MathUtils.Min(ScrollPosition, y + x);
			ScrollSpeed = 0f;
		}
		if (m_lastDragPosition.HasValue && (Input.Drag.HasValue || Input.Hold.HasValue))
		{
			Input.Clear();
		}
	}

	public override void Draw(DrawContext dc)
	{
		Color random = new Color((byte)128, (byte)128, (byte)128) * GlobalColorTransform * MathUtils.Saturate(m_scrollBarAlpha);
		if (random.A > 0 && m_scrollAreaLength > 0f)
		{
			FlatBatch2D num = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
			int i = num.TriangleVertices.Count;
			if (Direction == LayoutDirection.Horizontal)
			{
				float num2 = ScrollPosition;
				float num3 = ActualSize.X;
				Vector2 num4 = new Vector2(num2 / m_scrollAreaLength * num3, ActualSize.Y - 5f);
				Vector2 j = new Vector2((num2 + num3) / m_scrollAreaLength * num3, ActualSize.Y - 1f);
				num.QueueQuad(num4, j, 0f, random);
			}
			else
			{
				float num5 = ScrollPosition;
				float num6 = ActualSize.Y;
				Vector2 num7 = new Vector2(ActualSize.X - 5f, num5 / m_scrollAreaLength * num6);
				Vector2 face = new Vector2(ActualSize.X - 1f, (num5 + num6) / m_scrollAreaLength * num6);
				num.QueueQuad(num7, face, 0f, random);
			}
			num.TransformTriangles(GlobalTransform, i);
		}
	}

	public bool HitTestPanel(Vector2 position)
	{
		bool found = false;
		HitTestGlobal(position, (Widget widget) =>
		{
			found = widget.IsChildWidgetOf(this) || widget == this;
			return true;
		});
		return found;
	}
}
