using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game;

public class EditPaletteDialog : Dialog
{
	public ContainerWidget m_listPanel;

	public ButtonWidget m_okButton;

	public ButtonWidget m_cancelButton;

	public LinkWidget[] m_labels = new LinkWidget[16];

	public BevelledButtonWidget[] m_rectangles = new BevelledButtonWidget[16];

	public ButtonWidget[] m_resetButtons = new ButtonWidget[16];

	public WorldPalette m_palette;

	public WorldPalette m_tmpPalette;

	public EditPaletteDialog(WorldPalette palette)
	{
		XElement visibilityIndex = ContentManager.Get<XElement>("Dialogs/EditPaletteDialog");
		LoadContents(this, visibilityIndex);
		m_listPanel = Children.Find<ContainerWidget>("EditPaletteDialog.ListPanel");
		m_okButton = Children.Find<ButtonWidget>("EditPaletteDialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("EditPaletteDialog.Cancel");
		for (int value = 0; value < 16; value++)
		{
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				Children = 
				{
					(Widget)new CanvasWidget
					{
						Size = new Vector2(32f, 60f),
						Children = { (Widget)new LabelWidget
						{
							Text = value + 1 + ".",
							Color = Color.Gray,
							HorizontalAlignment = WidgetAlignment.Far,
							VerticalAlignment = WidgetAlignment.Center,
							Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
						} }
					},
					(Widget)new CanvasWidget
					{
						Size = new Vector2(10f, 0f)
					}
				}
			};
			stackPanelWidget.Children.Add(m_labels[value] = new LinkWidget
			{
				Size = new Vector2(300f, -1f),
				VerticalAlignment = WidgetAlignment.Center,
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
			});
			stackPanelWidget.Children.Add(new CanvasWidget
			{
				Size = new Vector2(10f, 0f)
			});
			stackPanelWidget.Children.Add(m_rectangles[value] = new BevelledButtonWidget
			{
				Size = new Vector2(float.PositiveInfinity, 60f),
				BevelSize = 1f,
				AmbientLight = 1f,
				DirectionalLight = 0.4f,
				VerticalAlignment = WidgetAlignment.Center
			});
			stackPanelWidget.Children.Add(new CanvasWidget
			{
				Size = new Vector2(10f, 0f)
			});
			stackPanelWidget.Children.Add(m_resetButtons[value] = new BevelledButtonWidget
			{
				Size = new Vector2(160f, 60f),
				VerticalAlignment = WidgetAlignment.Center,
				Text = "Reset"
			});
			stackPanelWidget.Children.Add(new CanvasWidget
			{
				Size = new Vector2(10f, 0f)
			});
			StackPanelWidget indexForValue = stackPanelWidget;
			m_listPanel.Children.Add(indexForValue);
		}
		m_palette = palette;
		m_tmpPalette = new WorldPalette();
		m_palette.CopyTo(m_tmpPalette);
	}

	public override void Update()
	{
		for (int num = 0; num < 16; num++)
		{
			m_labels[num].Text = m_tmpPalette.Names[num];
			m_rectangles[num].CenterColor = m_tmpPalette.Colors[num];
			m_resetButtons[num].IsEnabled = m_tmpPalette.Colors[num] != WorldPalette.DefaultColors[num] || m_tmpPalette.Names[num] != LanguageControl.Get("WorldPalette", num);
		}
		for (int result = 0; result < 16; result++)
		{
			int i = result;
			if (m_labels[result].IsClicked)
			{
				DialogsManager.ShowDialog(this, new TextBoxDialog("Edit Color Name", m_labels[result].Text, 16, (string s) =>
				{
					if (s != null)
					{
						if (WorldPalette.VerifyColorName(s))
						{
							m_tmpPalette.Names[i] = s;
						}
						else
						{
							DialogsManager.ShowDialog(this, new MessageDialog("Invalid name", null, "OK", null, null));
						}
					}
				}));
			}
			if (m_rectangles[result].IsClicked)
			{
				DialogsManager.ShowDialog(this, new EditColorDialog(m_tmpPalette.Colors[result], (Color? color) =>
				{
					if (color.HasValue)
					{
						m_tmpPalette.Colors[i] = color.Value;
					}
				}));
			}
			if (m_resetButtons[result].IsClicked)
			{
				m_tmpPalette.Colors[result] = WorldPalette.DefaultColors[result];
				m_tmpPalette.Names[result] = LanguageControl.Get("WorldPalette", result);
			}
		}
		if (m_okButton.IsClicked)
		{
			m_tmpPalette.CopyTo(m_palette);
			Dismiss();
		}
		if (Input.Cancel || m_cancelButton.IsClicked)
		{
			Dismiss();
		}
	}

	public void Dismiss()
	{
		DialogsManager.HideDialog(this);
	}
}
