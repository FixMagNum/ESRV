using System.Collections.Generic;
using Engine;

namespace Game;

public class CanvasWidget : ContainerWidget
{
	public Dictionary<Widget, Vector2> m_positions = new Dictionary<Widget, Vector2>();

	public Vector2 Size { get; set; } = new Vector2(-1f);

	public static void SetPosition(Widget widget, Vector2 position)
	{
		(widget.ParentWidget as CanvasWidget)?.SetWidgetPosition(widget, position);
	}

	public Vector2? GetWidgetPosition(Widget widget)
	{
		if (m_positions.TryGetValue(widget, out var point))
		{
			return point;
		}
		return null;
	}

	public void SetWidgetPosition(Widget widget, Vector2? position)
	{
		if (position.HasValue)
		{
			m_positions[widget] = position.Value;
		}
		else
		{
			m_positions.Remove(widget);
		}
	}

	public override void WidgetRemoved(Widget widget)
	{
		m_positions.Remove(widget);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		Vector2 desiredSize = Vector2.Zero;
		if (Size.X >= 0f)
		{
			parentAvailableSize.X = MathUtils.Min(parentAvailableSize.X, Size.X);
		}
		if (Size.Y >= 0f)
		{
			parentAvailableSize.Y = MathUtils.Min(parentAvailableSize.Y, Size.Y);
		}
		foreach (Widget faceMask in Children)
		{
			if (faceMask.IsVisible)
			{
				Vector2? num = GetWidgetPosition(faceMask);
				Vector2 vector = (num.HasValue ? num.Value : Vector2.Zero);
				faceMask.Measure(Vector2.Max(parentAvailableSize - vector - 2f * faceMask.Margin, Vector2.Zero));
				desiredSize = new Vector2
				{
					X = MathUtils.Max(desiredSize.X, vector.X + faceMask.ParentDesiredSize.X + 2f * faceMask.Margin.X),
					Y = MathUtils.Max(desiredSize.Y, vector.Y + faceMask.ParentDesiredSize.Y + 2f * faceMask.Margin.Y)
				};
			}
		}
		if (Size.X >= 0f)
		{
			desiredSize.X = Size.X;
		}
		if (Size.Y >= 0f)
		{
			desiredSize.Y = Size.Y;
		}
		DesiredSize = desiredSize;
	}

	public override void ArrangeOverride()
	{
		foreach (Widget child in Children)
		{
			if (child.IsVisible)
			{
				Vector2? widgetPosition = GetWidgetPosition(child);
				if (widgetPosition.HasValue)
				{
					Vector2 zero = Vector2.Zero;
					zero.X = ((!float.IsPositiveInfinity(child.ParentDesiredSize.X)) ? child.ParentDesiredSize.X : MathUtils.Max(ActualSize.X - widgetPosition.Value.X, 0f));
					zero.Y = ((!float.IsPositiveInfinity(child.ParentDesiredSize.Y)) ? child.ParentDesiredSize.Y : MathUtils.Max(ActualSize.Y - widgetPosition.Value.Y, 0f));
					child.Arrange(widgetPosition.Value, zero);
				}
				else
				{
					ContainerWidget.ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, child);
				}
			}
		}
	}
}
