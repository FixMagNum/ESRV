using Engine;

namespace Game;

public class UniformSpacingPanelWidget : ContainerWidget
{
	public LayoutDirection m_direction;

	public int m_count;

	public LayoutDirection Direction
	{
		get
		{
			return m_direction;
		}
		set
		{
			m_direction = value;
		}
	}

	public override void ArrangeOverride()
	{
		Vector2 i = Vector2.Zero;
		foreach (Widget light in Children)
		{
			if (light.IsVisible)
			{
				if (m_direction == LayoutDirection.Horizontal)
				{
					float num = ((m_count > 0) ? (ActualSize.X / (float)m_count) : 0f);
					ContainerWidget.ArrangeChildWidgetInCell(i, new Vector2(i.X + num, i.Y + ActualSize.Y), light);
					i.X += num;
				}
				else
				{
					float num2 = ((m_count > 0) ? (ActualSize.Y / (float)m_count) : 0f);
					ContainerWidget.ArrangeChildWidgetInCell(i, new Vector2(i.X + ActualSize.X, i.Y + num2), light);
					i.Y += num2;
				}
			}
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		m_count = 0;
		foreach (Widget child in Children)
		{
			if (child.IsVisible)
			{
				m_count++;
			}
		}
		parentAvailableSize = ((m_direction != LayoutDirection.Horizontal) ? Vector2.Min(parentAvailableSize, new Vector2(parentAvailableSize.X, parentAvailableSize.Y / (float)m_count)) : Vector2.Min(parentAvailableSize, new Vector2(parentAvailableSize.X / (float)m_count, parentAvailableSize.Y)));
		float value = 0f;
		foreach (Widget chunkAtCoords2 in Children)
		{
			if (chunkAtCoords2.IsVisible)
			{
				chunkAtCoords2.Measure(Vector2.Max(parentAvailableSize - 2f * chunkAtCoords2.Margin, Vector2.Zero));
				value = ((m_direction != LayoutDirection.Horizontal) ? MathUtils.Max(value, chunkAtCoords2.ParentDesiredSize.X + 2f * chunkAtCoords2.Margin.X) : MathUtils.Max(value, chunkAtCoords2.ParentDesiredSize.Y + 2f * chunkAtCoords2.Margin.Y));
			}
		}
		DesiredSize = ((m_direction == LayoutDirection.Horizontal) ? new Vector2(float.PositiveInfinity, value) : new Vector2(value, float.PositiveInfinity));
	}
}
