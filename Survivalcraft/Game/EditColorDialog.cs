using System;
using Engine;
using Engine.Media;
using Engine.Serialization;

namespace Game;

public class EditColorDialog : Dialog
{
	public SliderWidget m_sliderR;

	public SliderWidget m_sliderG;

	public SliderWidget m_sliderB;

	public LabelWidget m_label;

	public ButtonWidget m_okButton;

	public ButtonWidget m_cancelButton;

	public Action<Color?> m_handler;

	public Color m_color;

	public BevelledRectangleWidget m_rectangle;

	public EditColorDialog(Color color, Action<Color?> handler)
	{
		WidgetsList children = Children;
		CanvasWidget canvasWidget = new CanvasWidget
		{
			Size = new Vector2(660f, 420f),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center,
			Children = { (Widget)new RectangleWidget
			{
				FillColor = new Color(0, 0, 0, 255),
				OutlineColor = new Color(128, 128, 128, 128),
				OutlineThickness = 2f
			} }
		};
		WidgetsList children2 = canvasWidget.Children;
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			Margin = new Vector2(15f),
			HorizontalAlignment = WidgetAlignment.Center,
			Children = 
			{
				(Widget)new LabelWidget
				{
					Text = "Edit Color",
					HorizontalAlignment = WidgetAlignment.Center
				},
				(Widget)new CanvasWidget
				{
					Size = new Vector2(0f, float.PositiveInfinity)
				}
			}
		};
		WidgetsList children3 = stackPanelWidget.Children;
		StackPanelWidget stackPanelWidget2 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal
		};
		WidgetsList children4 = stackPanelWidget2.Children;
		StackPanelWidget stackPanelWidget3 = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			VerticalAlignment = WidgetAlignment.Center
		};
		WidgetsList children5 = stackPanelWidget3.Children;
		StackPanelWidget stackPanelWidget4 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(0f, 10f),
			Children = 
			{
				(Widget)new LabelWidget
				{
					Text = "Red:",
					Color = Color.Gray,
					VerticalAlignment = WidgetAlignment.Center,
					Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
				},
				(Widget)new CanvasWidget
				{
					Size = new Vector2(10f, 0f)
				}
			}
		};
		stackPanelWidget4.Children.Add(m_sliderR = new SliderWidget
		{
			Size = new Vector2(300f, 50f),
			IsLabelVisible = false,
			MinValue = 0f,
			MaxValue = 255f,
			Granularity = 1f,
			SoundName = ""
		});
		children5.Add(stackPanelWidget4);
		WidgetsList children6 = stackPanelWidget3.Children;
		StackPanelWidget stackPanelWidget5 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(0f, 10f),
			Children = 
			{
				(Widget)new LabelWidget
				{
					Text = "Green:",
					Color = Color.Gray,
					VerticalAlignment = WidgetAlignment.Center,
					Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
				},
				(Widget)new CanvasWidget
				{
					Size = new Vector2(10f, 0f)
				}
			}
		};
		stackPanelWidget5.Children.Add(m_sliderG = new SliderWidget
		{
			Size = new Vector2(300f, 50f),
			IsLabelVisible = false,
			MinValue = 0f,
			MaxValue = 255f,
			Granularity = 1f,
			SoundName = ""
		});
		children6.Add(stackPanelWidget5);
		WidgetsList children7 = stackPanelWidget3.Children;
		StackPanelWidget stackPanelWidget6 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(0f, 10f),
			Children = 
			{
				(Widget)new LabelWidget
				{
					Text = "Blue:",
					Color = Color.Gray,
					VerticalAlignment = WidgetAlignment.Center,
					Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
				},
				(Widget)new CanvasWidget
				{
					Size = new Vector2(10f, 0f)
				}
			}
		};
		stackPanelWidget6.Children.Add(m_sliderB = new SliderWidget
		{
			Size = new Vector2(300f, 50f),
			IsLabelVisible = false,
			MinValue = 0f,
			MaxValue = 255f,
			Granularity = 1f,
			SoundName = ""
		});
		children7.Add(stackPanelWidget6);
		children4.Add(stackPanelWidget3);
		stackPanelWidget2.Children.Add(new CanvasWidget
		{
			Size = new Vector2(20f, 0f)
		});
		stackPanelWidget2.Children.Add(new CanvasWidget
		{
			Children = 
			{
				(Widget)(m_rectangle = new BevelledRectangleWidget
				{
					Size = new Vector2(200f, 240f),
					AmbientLight = 1f,
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					IsHitTestVisible = true
				}),
				(Widget)(m_label = new LabelWidget
				{
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
				})
			}
		});
		children3.Add(stackPanelWidget2);
		stackPanelWidget.Children.Add(new CanvasWidget
		{
			Size = new Vector2(0f, float.PositiveInfinity)
		});
		WidgetsList children8 = stackPanelWidget.Children;
		StackPanelWidget stackPanelWidget7 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Center
		};
		stackPanelWidget7.Children.Add(m_okButton = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 60f),
			Text = "OK"
		});
		stackPanelWidget7.Children.Add(new CanvasWidget
		{
			Size = new Vector2(50f, 0f)
		});
		stackPanelWidget7.Children.Add(m_cancelButton = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 60f),
			Text = "Cancel"
		});
		children8.Add(stackPanelWidget7);
		children2.Add(stackPanelWidget);
		children.Add(canvasWidget);
		m_handler = handler;
		m_color = color;
		UpdateControls();
	}

	public override void Update()
	{
		if (m_rectangle.Input.Tap.HasValue && m_rectangle.HitTest(m_rectangle.Input.Tap.Value))
		{
			DialogsManager.ShowDialog(this, new TextBoxDialog("Enter Color", GetColorString(), 20, (string s) =>
			{
				if (s != null)
				{
					try
					{
						m_color.RGB = HumanReadableConverter.ConvertFromString<Color>(s);
					}
					catch
					{
						DialogsManager.ShowDialog(this, new MessageDialog("Invalid Color", "Use R,G,B or #HEX notation, e.g. 255,92,13 or #FF5C0D", "OK", null, null));
					}
				}
			}));
		}
		if (m_sliderR.IsSliding)
		{
			m_color.R = (byte)m_sliderR.Value;
		}
		if (m_sliderG.IsSliding)
		{
			m_color.G = (byte)m_sliderG.Value;
		}
		if (m_sliderB.IsSliding)
		{
			m_color.B = (byte)m_sliderB.Value;
		}
		if (m_okButton.IsClicked)
		{
			Dismiss(m_color);
		}
		if (Input.Cancel || m_cancelButton.IsClicked)
		{
			Dismiss(null);
		}
		UpdateControls();
	}

	public void UpdateControls()
	{
		m_rectangle.DisableThemeColors = true;
		m_rectangle.CenterColor = m_color;
		m_rectangle.BevelColor = Color.Lerp(m_color, Color.White, 0.3f);
		m_sliderR.Value = (int)m_color.R;
		m_sliderG.Value = (int)m_color.G;
		m_sliderB.Value = (int)m_color.B;
		m_label.Text = GetColorString();
		int i = m_color.R + m_color.G + m_color.B;
		m_label.Color = ((i > 380) ? Color.Black : Color.White);
	}

	public string GetColorString()
	{
		return $"#{m_color.R:X2}{m_color.G:X2}{m_color.B:X2}";
	}

	public void Dismiss(Color? result)
	{
		DialogsManager.HideDialog(this);
		m_handler?.Invoke(result);
	}
}
