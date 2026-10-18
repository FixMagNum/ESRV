using Engine;

namespace Game;

public class AutoCanvasWidget : CanvasWidget
{
	public AutoCanvasWidget()
	{
		VerticalAlignment = WidgetAlignment.Far;
	}

	public void Add(Widget widget)
	{
		Children.Add(widget);
	}

	public void Remove(Widget widget)
	{
		Children.Remove(widget);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		foreach (Widget child in Children)
		{
			if (child.IsVisible)
			{
				child.Measure(new Vector2(parentAvailableSize.X, 0f));
				num3 = MathUtils.Max(num3, child.DesiredSize.Y);
				if (num + child.DesiredSize.X > parentAvailableSize.X)
				{
					num2 += num3;
					num = 0f;
					num3 = MathUtils.Max(0f, child.DesiredSize.Y);
				}
				num += child.DesiredSize.X;
			}
		}
		DesiredSize = new Vector2(num, num2 + num3);
		Size = DesiredSize;
	}

	public override void ArrangeOverride()
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < Children.Count; i++)
		{
			Widget widget = Children[i];
			if (widget.IsVisible)
			{
				widget.Arrange(new Vector2(num, num2), new Vector2(widget.DesiredSize.X, widget.DesiredSize.Y));
				num3 = MathUtils.Max(num3, widget.ActualSize.Y);
				if (widget.ActualSize.X + num > ActualSize.X)
				{
					num2 += num3;
					num = 0f;
					num3 = MathUtils.Max(0f, widget.ActualSize.Y);
					widget.Arrange(new Vector2(num, num2) + widget.Margin, widget.ActualSize);
				}
				num += widget.ActualSize.X;
			}
		}
	}
}
