using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class ClickTextWidget : CanvasWidget
{
	public LabelWidget labelWidget;

	public Action click;

	public RectangleWidget rectangleWidget;

	public Color pressColor;

	public Color BorderColor;

	public ClickTextWidget(Vector2 vector2, string text, Action click, bool box = false)
	{
		Size = vector2;
		HorizontalAlignment = WidgetAlignment.Center;
		VerticalAlignment = WidgetAlignment.Center;
		rectangleWidget = new RectangleWidget
		{
			FillColor = Color.Transparent,
			OutlineColor = Color.Transparent,
			HorizontalAlignment = WidgetAlignment.Stretch,
			VerticalAlignment = WidgetAlignment.Stretch
		};
		Children.Add(rectangleWidget);
		labelWidget = new LabelWidget
		{
			Text = text,
			FontScale = 0.8f,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
		Children.Add(labelWidget);
		IsDrawEnabled = true;
		IsDrawRequired = true;
		IsUpdateEnabled = true;
		this.click = click;
	}

	public override void Draw(DrawContext dc)
	{
		Matrix gameMode = GlobalTransform;
		Vector2 terrainGenerationMode = Vector2.Zero;
		Vector2 terrainContentsGenerator = new Vector2(ActualSize.X, 0f);
		Vector2 v = ActualSize;
		Vector2 terrainGenerationMode2 = new Vector2(0f, ActualSize.Y);
		Vector2.Transform(ref terrainGenerationMode, ref gameMode, out var terrainContentsGenerator2);
		Vector2.Transform(ref terrainContentsGenerator, ref gameMode, out var result);
		Vector2.Transform(ref v, ref gameMode, out var result2);
		Vector2.Transform(ref terrainGenerationMode2, ref gameMode, out var result3);
		FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.DepthWrite);
		Vector2 vector = Vector2.Normalize(GlobalTransform.Right.XY);
		Vector2 vector2 = -Vector2.Normalize(GlobalTransform.Up.XY);
		for (int i = 0; i < 1; i++)
		{
			flatBatch2D.QueueLine(terrainContentsGenerator2, result, 1f, BorderColor);
			flatBatch2D.QueueLine(result, result2, 1f, BorderColor);
			flatBatch2D.QueueLine(result2, result3, 1f, BorderColor);
			flatBatch2D.QueueLine(result3, terrainContentsGenerator2, 1f, BorderColor);
			terrainContentsGenerator2 += vector - vector2;
			result += -vector - vector2;
			result2 += -vector + vector2;
			result3 += vector + vector2;
		}
	}

	public override void Update()
	{
		if (Input.Click.HasValue && HitTest(Input.Click.Value.Start) && HitTest(Input.Click.Value.End))
		{
			click?.Invoke();
		}
	}
}
