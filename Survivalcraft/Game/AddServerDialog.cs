using System;
using Engine;

namespace Game;

public class AddServerDialog : Dialog
{
	public CanvasWidget canvasWidget = new CanvasWidget
	{
		ClampToBounds = true,
		HorizontalAlignment = WidgetAlignment.Center,
		VerticalAlignment = WidgetAlignment.Center
	};

	public RectangleWidget rectangleWidget = new RectangleWidget
	{
		FillColor = Color.Black,
		OutlineColor = Color.White,
		OutlineThickness = 2f
	};

	public StackPanelWidget panelWidget = new StackPanelWidget
	{
		Direction = LayoutDirection.Vertical
	};

	public BevelledButtonWidget add = new BevelledButtonWidget
	{
		Text = LanguageControl.Ok,
		Margin = new Vector2(180f, 0f),
		Size = new Vector2(120f, 66f),
		HorizontalAlignment = WidgetAlignment.Near
	};

	public BevelledButtonWidget cancel = new BevelledButtonWidget
	{
		Text = LanguageControl.Cancel,
		Margin = new Vector2(140f, 0f),
		Size = new Vector2(120f, 66f),
		HorizontalAlignment = WidgetAlignment.Far
	};

	public CanvasWidget autoCanvas = new CanvasWidget
	{
		Margin = new Vector2(0f, 10f)
	};

	public TextBoxWidget name1;

	public TextBoxWidget ip1;

	public Action<string, string> succ;

	public static string fName = "AddServerDialog";

	public AddServerDialog(Action<string, string> succd)
	{
		succ = succd;
		canvasWidget.Size = new Vector2(600f, 240f);
		autoCanvas.Size = new Vector2(canvasWidget.Size.X, 60f);
		canvasWidget.Children.Add(rectangleWidget);
		canvasWidget.Children.Add(panelWidget);
		Children.Add(canvasWidget);
		autoCanvas.Children.Add(add);
		autoCanvas.Children.Add(cancel);
		panelWidget.Children.Add(EditText(new Vector2(550f, 60f), LanguageControl.Get(fName, 1), "name", new Vector2(0f, 10f)));
		panelWidget.Children.Add(EditText(new Vector2(550f, 60f), LanguageControl.Get(fName, 2), "ip", new Vector2(0f, 10f)));
		panelWidget.Children.Add(autoCanvas);
		name1 = Children.Find<TextBoxWidget>("name");
		ip1 = Children.Find<TextBoxWidget>("ip");
	}

	public Widget EditText(Vector2 size, string title, string name, Vector2 margin)
	{
		CanvasWidget canvasWidget = new CanvasWidget
		{
			Size = size,
			HorizontalAlignment = WidgetAlignment.Center,
			Margin = margin
		};
		CanvasWidget num = new CanvasWidget
		{
			Size = size - new Vector2(160f, 0f),
			HorizontalAlignment = WidgetAlignment.Far
		};
		RectangleWidget timeOfDay = new RectangleWidget
		{
			OutlineColor = Color.White
		};
		LabelWidget num2 = new LabelWidget
		{
			Text = title
		};
		TextBoxWidget camXZ = new TextBoxWidget
		{
			Name = name,
			Size = size,
			HorizontalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(3f, 0f)
		};
		num.Children.Add(timeOfDay);
		num.Children.Add(camXZ);
		canvasWidget.Children.Add(num2);
		canvasWidget.Children.Add(num);
		return canvasWidget;
	}

	public override void Update()
	{
		if (cancel.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (add.IsClicked)
		{
			if (string.IsNullOrEmpty(ip1.Text) || string.IsNullOrEmpty(name1.Text))
			{
				DialogsManager.HideAllDialogs();
				DialogsManager.Alert(LanguageControl.Get(fName, 3));
			}
			else
			{
				DialogsManager.HideDialog(this);
				succ?.Invoke(name1.Text, ip1.Text);
			}
		}
	}
}
