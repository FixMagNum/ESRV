using Engine;

namespace Game;

public class StackPanelWidget : ContainerWidget
{
	public float m_fixedSize;

	public int m_fillCount;

	public LayoutDirection Direction { get; set; }

	public bool IsInverted { get; set; }

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		m_fixedSize = 0f;
		m_fillCount = 0;
		float num = 0f;
		foreach (Widget num2 in Children)
		{
			if (!num2.IsVisible)
			{
				continue;
			}
			num2.Measure(Vector2.Max(parentAvailableSize - 2f * num2.Margin, Vector2.Zero));
			if (Direction == LayoutDirection.Horizontal)
			{
				if (num2.ParentDesiredSize.X != float.PositiveInfinity)
				{
					m_fixedSize += num2.ParentDesiredSize.X + 2f * num2.Margin.X;
					parentAvailableSize.X = MathUtils.Max(parentAvailableSize.X - (num2.ParentDesiredSize.X + 2f * num2.Margin.X), 0f);
				}
				else
				{
					m_fillCount++;
				}
				num = MathUtils.Max(num, num2.ParentDesiredSize.Y + 2f * num2.Margin.Y);
			}
			else
			{
				if (num2.ParentDesiredSize.Y != float.PositiveInfinity)
				{
					m_fixedSize += num2.ParentDesiredSize.Y + 2f * num2.Margin.Y;
					parentAvailableSize.Y = MathUtils.Max(parentAvailableSize.Y - (num2.ParentDesiredSize.Y + 2f * num2.Margin.Y), 0f);
				}
				else
				{
					m_fillCount++;
				}
				num = MathUtils.Max(num, num2.ParentDesiredSize.X + 2f * num2.Margin.X);
			}
		}
		if (Direction == LayoutDirection.Horizontal)
		{
			DesiredSize = ((m_fillCount == 0) ? new Vector2(m_fixedSize, num) : new Vector2(float.PositiveInfinity, num));
		}
		else
		{
			DesiredSize = ((m_fillCount == 0) ? new Vector2(num, m_fixedSize) : new Vector2(num, float.PositiveInfinity));
		}
	}

	public override void ArrangeOverride()
	{
		float viewPosition = 0f;
		foreach (Widget v2 in Children)
		{
			if (!v2.IsVisible)
			{
				continue;
			}
			if (Direction == LayoutDirection.Horizontal)
			{
				float v3;
				if (v2.ParentDesiredSize.X != float.PositiveInfinity)
				{
					v3 = v2.ParentDesiredSize.X + 2f * v2.Margin.X;
				}
				else
				{
					v3 = ((m_fillCount > 0) ? (MathUtils.Max(ActualSize.X - m_fixedSize, 0f) / (float)m_fillCount) : 0f);
				}
				Vector2 v4;
				Vector2 x;
				if (!IsInverted)
				{
					v4 = new Vector2(viewPosition, 0f);
					x = new Vector2(viewPosition + v3, ActualSize.Y);
				}
				else
				{
					v4 = new Vector2(ActualSize.X - (viewPosition + v3), 0f);
					x = new Vector2(ActualSize.X - viewPosition, ActualSize.Y);
				}
				ContainerWidget.ArrangeChildWidgetInCell(v4, x, v2);
				viewPosition += v3;
			}
			else
			{
				float x2;
				if (v2.ParentDesiredSize.Y != float.PositiveInfinity)
				{
					x2 = v2.ParentDesiredSize.Y + 2f * v2.Margin.Y;
				}
				else
				{
					x2 = ((m_fillCount > 0) ? (MathUtils.Max(ActualSize.Y - m_fixedSize, 0f) / (float)m_fillCount) : 0f);
				}
				Vector2 x3;
				Vector2 x4;
				if (!IsInverted)
				{
					x3 = new Vector2(0f, viewPosition);
					x4 = new Vector2(ActualSize.X, viewPosition + x2);
				}
				else
				{
					x3 = new Vector2(0f, ActualSize.Y - (viewPosition + x2));
					x4 = new Vector2(ActualSize.X, ActualSize.Y - viewPosition);
				}
				ContainerWidget.ArrangeChildWidgetInCell(x3, x4, v2);
				viewPosition += x2;
			}
		}
	}
}
