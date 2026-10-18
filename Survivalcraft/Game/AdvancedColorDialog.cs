using System;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;

namespace Game;

public class AdvancedColorDialog : Dialog
{
	private CanvasWidget m_gradientWidget;

	private SliderWidget m_sliderR;

	private SliderWidget m_sliderG;

	private SliderWidget m_sliderB;

	private SliderWidget m_sliderA;

	private SliderWidget m_sliderSize;

	private BevelledRectangleWidget m_previewRect;

	private LabelWidget m_labelHex;

	private ButtonWidget m_okButton;

	private ButtonWidget m_cancelButton;

	private Color m_color;

	private int m_brushSize;

	private Action<Color?, int> m_handler;

	private CanvasWidget m_hueStripWidget;

	private Texture2D m_svTexture;

	private RectangleWidget m_svRectangle;

	private float m_currentHue;

	private float m_currentSat;

	private float m_currentVal;

	private RectangleWidget m_hueMarker;

	private RectangleWidget m_svMarker;

	public AdvancedColorDialog(Color color, int currentSize, Action<Color?, int> handler)
	{
		m_color = color;
		m_brushSize = currentSize;
		m_handler = handler;
		m_currentHue = 0f;
		m_currentSat = 1f;
		m_currentVal = 1f;
		m_svTexture = new Texture2D(256, 128, 1, ColorFormat.Rgba8888);
		UpdateSvTexture();
		WidgetsList children = Children;
		CanvasWidget canvasWidget = new CanvasWidget
		{
			Size = new Vector2(600f, 500f),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
		canvasWidget.Children.Add(new RectangleWidget
		{
			FillColor = new Color(0, 0, 0, 240),
			OutlineColor = new Color(128, 128, 128, 128),
			OutlineThickness = 2f
		});
		children.Add(canvasWidget);
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			Margin = new Vector2(20f),
			HorizontalAlignment = WidgetAlignment.Center
		};
		canvasWidget.Children.Add(stackPanelWidget);
		stackPanelWidget.Children.Add(new LabelWidget
		{
			Text = "Advanced Color",
			HorizontalAlignment = WidgetAlignment.Center,
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
			Margin = new Vector2(0f, 5f)
		});
		m_gradientWidget = new CanvasWidget
		{
			Size = new Vector2(560f, 150f),
			IsHitTestVisible = true,
			Margin = new Vector2(0f, 5f)
		};
		m_svRectangle = new RectangleWidget
		{
			Size = new Vector2(560f, 150f),
			Subtexture = new Subtexture(m_svTexture, Vector2.Zero, Vector2.One),
			OutlineColor = Color.White,
			OutlineThickness = 1f,
			FillColor = Color.White,
			IsHitTestVisible = false
		};
		m_gradientWidget.Children.Add(m_svRectangle);
		m_svMarker = new RectangleWidget
		{
			Size = new Vector2(5f, 5f),
			OutlineColor = Color.Black,
			FillColor = Color.White,
			OutlineThickness = 1f,
			IsHitTestVisible = false
		};
		m_gradientWidget.Children.Add(m_svMarker);
		m_hueStripWidget = new CanvasWidget
		{
			Size = new Vector2(560f, 32f),
			IsHitTestVisible = true,
			Margin = new Vector2(0f, 5f)
		};
		Texture2D texture = GenerateHueTexture(256, 1);
		m_hueStripWidget.Children.Add(new RectangleWidget
		{
			Size = new Vector2(560f, 32f),
			Subtexture = new Subtexture(texture, Vector2.Zero, Vector2.One),
			OutlineColor = Color.White,
			OutlineThickness = 1f,
			FillColor = Color.White,
			IsHitTestVisible = false
		});
		m_hueMarker = new RectangleWidget
		{
			Size = new Vector2(5f, m_hueStripWidget.Size.Y),
			OutlineColor = Color.Black,
			FillColor = Color.Transparent,
			OutlineThickness = 2f,
			IsHitTestVisible = false
		};
		m_hueStripWidget.Children.Add(m_hueMarker);
		stackPanelWidget.Children.Add(m_hueStripWidget);
		stackPanelWidget.Children.Add(m_gradientWidget);
		stackPanelWidget.Children.Clear();
		stackPanelWidget.Children.Add(new LabelWidget
		{
			Text = "Advanced Color",
			HorizontalAlignment = WidgetAlignment.Center,
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
			Margin = new Vector2(0f, 5f)
		});
		stackPanelWidget.Children.Add(m_gradientWidget);
		stackPanelWidget.Children.Add(m_hueStripWidget);
		StackPanelWidget stackPanelWidget2 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			Margin = new Vector2(0f, 10f)
		};
		StackPanelWidget stackPanelWidget3 = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical
		};
		stackPanelWidget3.Children.Add(CreateSlider("Red", ref m_sliderR, 0f, 255f));
		stackPanelWidget3.Children.Add(CreateSlider("Green", ref m_sliderG, 0f, 255f));
		stackPanelWidget3.Children.Add(CreateSlider("Blue", ref m_sliderB, 0f, 255f));
		stackPanelWidget3.Children.Add(CreateSlider("Alpha", ref m_sliderA, 0f, 255f));
		stackPanelWidget3.Children.Add(new CanvasWidget
		{
			Size = new Vector2(0f, 5f)
		});
		stackPanelWidget3.Children.Add(CreateSlider("Size", ref m_sliderSize, 1f, 10f));
		m_sliderSize.Granularity = 1f;
		stackPanelWidget2.Children.Add(stackPanelWidget3);
		StackPanelWidget stackPanelWidget4 = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			VerticalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(20f, 0f)
		};
		m_previewRect = new BevelledRectangleWidget
		{
			Size = new Vector2(120f, 80f),
			BevelColor = Color.White,
			CenterColor = color,
			DisableThemeColors = true,
			Margin = new Vector2(0f, 5f)
		};
		stackPanelWidget4.Children.Add(m_previewRect);
		m_labelHex = new LabelWidget
		{
			Text = "#000000",
			HorizontalAlignment = WidgetAlignment.Center,
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
			Color = Color.White
		};
		m_labelHex.IsHitTestVisible = true;
		stackPanelWidget4.Children.Add(m_labelHex);
		stackPanelWidget2.Children.Add(stackPanelWidget4);
		stackPanelWidget.Children.Add(stackPanelWidget2);
		StackPanelWidget stackPanelWidget5 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(0f, 5f)
		};
		m_okButton = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 50f),
			Text = "OK"
		};
		m_cancelButton = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 50f),
			Text = "Cancel"
		};
		stackPanelWidget5.Children.Add(m_okButton);
		stackPanelWidget5.Children.Add(new CanvasWidget
		{
			Size = new Vector2(20f, 0f)
		});
		stackPanelWidget5.Children.Add(m_cancelButton);
		stackPanelWidget.Children.Add(stackPanelWidget5);
		UpdateHsvFromColor();
		UpdateControls(fromColorToSliders: true);
	}

	private StackPanelWidget CreateSlider(string name, ref SliderWidget slider, float min, float max)
	{
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			Margin = new Vector2(0f, 1f),
			Children = { (Widget)new LabelWidget
			{
				Text = name + ":",
				Size = new Vector2(70f, -1f),
				Color = Color.Gray,
				VerticalAlignment = WidgetAlignment.Center
			} }
		};
		slider = new SliderWidget
		{
			Size = new Vector2(330f, 30f),
			MinValue = min,
			MaxValue = max,
			IsLabelVisible = true
		};
		stackPanelWidget.Children.Add(slider);
		return stackPanelWidget;
	}

	public override void Update()
	{
		if (m_hueStripWidget.Input.Tap.HasValue)
		{
			Vector2 value = m_hueStripWidget.Input.Tap.Value;
			if (m_hueStripWidget.HitTest(value))
			{
				float num = MathUtils.Clamp((value.X - m_hueStripWidget.GlobalBounds.Min.X) / (m_hueStripWidget.GlobalBounds.Max.X - m_hueStripWidget.GlobalBounds.Min.X), 0f, 1f);
				m_currentHue = num * 360f;
				UpdateSvTexture();
				Color color = HsvToRgb(m_currentHue, m_currentSat, m_currentVal);
				m_color = new Color(color.R, color.G, color.B, m_color.A);
				UpdateControls(fromColorToSliders: true);
			}
		}
		if (m_gradientWidget.Input.Tap.HasValue)
		{
			Vector2 value2 = m_gradientWidget.Input.Tap.Value;
			if (m_gradientWidget.HitTest(value2))
			{
				float num2 = value2.X - m_gradientWidget.GlobalBounds.Min.X;
				float num3 = value2.Y - m_gradientWidget.GlobalBounds.Min.Y;
				m_currentSat = MathUtils.Clamp(num2 / (m_gradientWidget.GlobalBounds.Max.X - m_gradientWidget.GlobalBounds.Min.X), 0f, 1f);
				m_currentVal = 1f - MathUtils.Clamp(num3 / (m_gradientWidget.GlobalBounds.Max.Y - m_gradientWidget.GlobalBounds.Min.Y), 0f, 1f);
				Color color2 = HsvToRgb(m_currentHue, m_currentSat, m_currentVal);
				m_color = new Color(color2.R, color2.G, color2.B, m_color.A);
				UpdateControls(fromColorToSliders: true);
			}
		}
		bool flag = false;
		if (m_sliderR.IsSliding || m_sliderG.IsSliding || m_sliderB.IsSliding || m_sliderA.IsSliding)
		{
			m_color = new Color((byte)m_sliderR.Value, (byte)m_sliderG.Value, (byte)m_sliderB.Value, (byte)m_sliderA.Value);
			UpdateHsvFromColor();
			flag = true;
		}
		if (m_sliderSize.IsSliding)
		{
			m_brushSize = (int)m_sliderSize.Value;
			flag = true;
		}
		if (flag)
		{
			UpdateControls(fromColorToSliders: false);
		}
		if (m_labelHex.Input.Tap.HasValue && m_labelHex.HitTest(m_labelHex.Input.Tap.Value))
		{
			DialogsManager.ShowDialog(this, new TextBoxDialog("Enter Color", GetColorString(), 20, (string s) =>
			{
				if (s != null)
				{
					try
					{
						m_color = HumanReadableConverter.ConvertFromString<Color>(s);
						UpdateHsvFromColor();
						UpdateControls(fromColorToSliders: true);
					}
					catch
					{
					}
				}
			}));
		}
		if (m_okButton.IsClicked)
		{
			Dismiss(m_color, m_brushSize);
		}
		if (m_cancelButton.IsClicked || Input.Cancel)
		{
			Dismiss(null, 0);
		}
	}

	private void UpdateControls(bool fromColorToSliders)
	{
		if (fromColorToSliders)
		{
			m_sliderR.Value = (int)m_color.R;
			m_sliderG.Value = (int)m_color.G;
			m_sliderB.Value = (int)m_color.B;
			m_sliderA.Value = (int)m_color.A;
		}
		m_sliderSize.Value = m_brushSize;
		string arg;
		if (m_brushSize == 1)
		{
			arg = "pixel";
		}
		else
		{
			arg = ((m_brushSize == 2) ? "square" : "circle");
		}
		m_sliderSize.Text = $"Size: {m_brushSize} ({arg})";
		m_previewRect.CenterColor = m_color;
		m_labelHex.Text = $"#{m_color.R:X2}{m_color.G:X2}{m_color.B:X2} (A:{m_color.A})";
		float num = m_currentHue / 360f * m_hueStripWidget.ActualSize.X;
		m_hueMarker.Margin = new Vector2(num - m_hueMarker.Size.X / 2f, 0f);
		float num2 = m_currentSat * m_gradientWidget.ActualSize.X;
		float num3 = (1f - m_currentVal) * m_gradientWidget.ActualSize.Y;
		m_svMarker.Margin = new Vector2(num2 - m_svMarker.Size.X / 2f, num3 - m_svMarker.Size.Y / 2f);
	}

	private void Dismiss(Color? c, int s)
	{
		DialogsManager.HideDialog(this);
		if (m_handler != null)
		{
			m_handler(c, s);
		}
	}

	public static Color HsvToRgb(float h, float s, float v)
	{
		int num = (int)Math.Floor(h / 60f) % 6;
		float num2 = h / 60f - (float)(int)Math.Floor(h / 60f);
		v *= 255f;
		byte b = (byte)v;
		byte b2 = (byte)(v * (1f - s));
		byte b3 = (byte)(v * (1f - num2 * s));
		byte b4 = (byte)(v * (1f - (1f - num2) * s));
		return num switch
		{
			0 => new Color(b, b4, b2, byte.MaxValue), 
			1 => new Color(b3, b, b2, byte.MaxValue), 
			2 => new Color(b2, b, b4, byte.MaxValue), 
			3 => new Color(b2, b3, b, byte.MaxValue), 
			4 => new Color(b4, b2, b, byte.MaxValue), 
			_ => new Color(b, b2, b3, byte.MaxValue), 
		};
	}

	private Texture2D GenerateHueTexture(int width, int height)
	{
		Texture2D texture2D = new Texture2D(width, height, 1, ColorFormat.Rgba8888);
		Color[] array = new Color[width * height];
		for (int i = 0; i < width; i++)
		{
			Color color = HsvToRgb((float)i / (float)(width - 1) * 360f, 1f, 1f);
			for (int j = 0; j < height; j++)
			{
				array[j * width + i] = color;
			}
		}
		texture2D.SetData(0, array);
		return texture2D;
	}

	private void UpdateSvTexture()
	{
		int width = m_svTexture.Width;
		int height = m_svTexture.Height;
		Color[] array = new Color[width * height];
		for (int i = 0; i < width; i++)
		{
			float s = (float)i / (float)(width - 1);
			for (int j = 0; j < height; j++)
			{
				float v = 1f - (float)j / (float)(height - 1);
				array[j * width + i] = HsvToRgb(m_currentHue, s, v);
			}
		}
		m_svTexture.SetData(0, array);
	}

	public string GetColorString()
	{
		return $"#{m_color.R:X2}{m_color.G:X2}{m_color.B:X2}";
	}

	private void UpdateHsvFromColor()
	{
		float num = (float)(int)m_color.R / 255f;
		float num2 = (float)(int)m_color.G / 255f;
		float num3 = (float)(int)m_color.B / 255f;
		float num4 = Math.Max(num, Math.Max(num2, num3));
		float num5 = Math.Min(num, Math.Min(num2, num3));
		float num6 = num4 - num5;
		if (num6 == 0f)
		{
			m_currentHue = 0f;
		}
		else if (num4 == num)
		{
			m_currentHue = 60f * ((num2 - num3) / num6 % 6f);
		}
		else if (num4 == num2)
		{
			m_currentHue = 60f * ((num3 - num) / num6 + 2f);
		}
		else
		{
			m_currentHue = 60f * ((num - num2) / num6 + 4f);
		}
		if (m_currentHue < 0f)
		{
			m_currentHue += 360f;
		}
		if (num4 == 0f)
		{
			m_currentSat = 0f;
		}
		else
		{
			m_currentSat = num6 / num4;
		}
		m_currentVal = num4;
		UpdateSvTexture();
	}
}
