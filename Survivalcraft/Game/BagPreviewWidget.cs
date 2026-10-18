using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game;

public class BagPreviewWidget : CanvasWidget
{
	private RectangleWidget m_background;

	private StackPanelWidget m_gridContainer;

	private StackPanelWidget m_mainContainer;

	private LabelWidget m_emptyLabel;

	private SubsystemTerrain m_subsystemTerrain;

	public BagPreviewWidget()
	{
		XElement node = ContentManager.Get<XElement>("Widgets/BagPreviewWidget");
		LoadContents(this, node);
		m_background = Children.Find<RectangleWidget>("Background");
		m_gridContainer = Children.Find<StackPanelWidget>("GridContainer");
		m_mainContainer = Children.Find<StackPanelWidget>("MainContainer");
		m_emptyLabel = Children.Find<LabelWidget>("EmptyLabel");
		IsHitTestVisible = false;
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		m_mainContainer.Measure(parentAvailableSize);
		Vector2 desiredSize = m_mainContainer.DesiredSize;
		m_background.Size = desiredSize;
		DesiredSize = desiredSize;
	}

	public void Setup(BagInventory bag, SubsystemTerrain terrain)
	{
		m_subsystemTerrain = terrain;
		m_gridContainer.Children.Clear();
		int num = 0;
		for (int i = 0; i < bag.SlotsCount; i++)
		{
			if (bag.GetSlotCount(i) > 0)
			{
				num++;
			}
		}
		if (num == 0)
		{
			m_emptyLabel.IsVisible = true;
			m_gridContainer.IsVisible = false;
		}
		else
		{
			m_emptyLabel.IsVisible = false;
			m_gridContainer.IsVisible = true;
			PopulateGrid(bag);
		}
	}

	private void PopulateGrid(BagInventory bag)
	{
		StackPanelWidget stackPanelWidget = null;
		int num = 4;
		int num2 = 0;
		for (int i = 0; i < bag.SlotsCount; i++)
		{
			int slotCount = bag.GetSlotCount(i);
			if (slotCount > 0)
			{
				if (num2 % num == 0)
				{
					stackPanelWidget = new StackPanelWidget
					{
						Direction = LayoutDirection.Horizontal,
						Margin = new Vector2(0f, 2f)
					};
					m_gridContainer.Children.Add(stackPanelWidget);
				}
				int slotValue = bag.GetSlotValue(i);
				Widget widget = CreateSlotWidget(slotValue, slotCount);
				stackPanelWidget?.Children.Add(widget);
				num2++;
			}
		}
	}

	private Widget CreateSlotWidget(int value, int count)
	{
		CanvasWidget canvasWidget = new CanvasWidget
		{
			DesiredSize = new Vector2(48f, 48f),
			Margin = new Vector2(2f, 0f)
		};
		int value2 = Terrain.ReplaceLight(value, 15);
		BlockIconWidget blockIconWidget = new BlockIconWidget
		{
			Value = value2,
			Size = new Vector2(40f, 40f),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center,
			Scale = 1.2f
		};
		if (m_subsystemTerrain != null)
		{
			blockIconWidget.DrawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			blockIconWidget.DrawBlockEnvironmentData.InWorldMatrix = Matrix.Identity;
		}
		LabelWidget widget = new LabelWidget
		{
			Text = count.ToString(),
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
			FontScale = 0.6f,
			Color = Color.Black,
			HorizontalAlignment = WidgetAlignment.Far,
			VerticalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(0f, 0f),
			IsVisible = (count > 1)
		};
		LabelWidget widget2 = new LabelWidget
		{
			Text = count.ToString(),
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
			FontScale = 0.6f,
			Color = Color.White,
			HorizontalAlignment = WidgetAlignment.Far,
			VerticalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(2f, 2f),
			IsVisible = (count > 1)
		};
		canvasWidget.Children.Add(blockIconWidget);
		canvasWidget.Children.Add(widget);
		canvasWidget.Children.Add(widget2);
		return canvasWidget;
	}
}
