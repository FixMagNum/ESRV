using System.Xml.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class ClothingWidget : CanvasWidget
{
	public GridPanelWidget m_inventoryGrid;

	public StackPanelWidget m_clothingStack;

	public ButtonWidget m_vitalStatsButton;

	public ButtonWidget m_sleepButton;

	public PlayerModelWidget m_innerClothingModelWidget;

	public PlayerModelWidget m_outerClothingModelWidget;

	public ComponentPlayer m_componentPlayer;

	public ClothingWidget(ComponentPlayer componentPlayer)
	{
		m_componentPlayer = componentPlayer;
		XElement node = ContentManager.Get<XElement>("Widgets/ClothingWidget");
		LoadContents(this, node);
		m_clothingStack = Children.Find<StackPanelWidget>("ClothingStack");
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_vitalStatsButton = Children.Find<ButtonWidget>("VitalStatsButton");
		m_sleepButton = Children.Find<ButtonWidget>("SleepButton");
		m_innerClothingModelWidget = Children.Find<PlayerModelWidget>("InnerClothingModel");
		m_outerClothingModelWidget = Children.Find<PlayerModelWidget>("OuterClothingModel");
		for (int i = 0; i < 4; i++)
		{
			InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
			float y = float.PositiveInfinity;
			if (i == 0)
			{
				y = 68f;
			}
			if (i == 3)
			{
				y = 54f;
			}
			inventorySlotWidget.Size = new Vector2(float.PositiveInfinity, y);
			inventorySlotWidget.BevelColor = Color.Transparent;
			inventorySlotWidget.CenterColor = Color.Transparent;
			inventorySlotWidget.AssignInventorySlot(m_componentPlayer.ComponentClothing, i);
			inventorySlotWidget.HideEditOverlay = true;
			inventorySlotWidget.HideInteractiveOverlay = true;
			inventorySlotWidget.HideFoodOverlay = true;
			inventorySlotWidget.HideHighlightRectangle = true;
			inventorySlotWidget.HideBlockIcon = true;
			inventorySlotWidget.HideHealthBar = m_componentPlayer.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.GameMode == GameMode.Creative;
			m_clothingStack.Children.Add(inventorySlotWidget);
		}
		InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget
		{
			Size = new Vector2(60f, 60f),
			Margin = new Vector2(-300f, -8f),
			BevelColor = Color.Transparent,
			CenterColor = Color.Transparent
		};
		inventorySlotWidget2.AssignInventorySlot(m_componentPlayer.ComponentClothing, 4);
		inventorySlotWidget2.HideEditOverlay = true;
		inventorySlotWidget2.HideInteractiveOverlay = true;
		inventorySlotWidget2.HideFoodOverlay = true;
		inventorySlotWidget2.HideHighlightRectangle = true;
		inventorySlotWidget2.HideBlockIcon = true;
		inventorySlotWidget2.HideHealthBar = m_componentPlayer.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.GameMode == GameMode.Creative;
		CanvasWidget canvasWidget = new CanvasWidget
		{
			Size = new Vector2(80f, 80f),
			HorizontalAlignment = WidgetAlignment.Far,
			VerticalAlignment = WidgetAlignment.Center
		};
		canvasWidget.Children.Add(inventorySlotWidget2);
		Children.Add(canvasWidget);
		int num = 10;
		for (int j = 0; j < m_inventoryGrid.RowsCount; j++)
		{
			for (int k = 0; k < m_inventoryGrid.ColumnsCount; k++)
			{
				InventorySlotWidget inventorySlotWidget3 = new InventorySlotWidget();
				inventorySlotWidget3.AssignInventorySlot(componentPlayer.ComponentMiner.Inventory, num++);
				m_inventoryGrid.Children.Add(inventorySlotWidget3);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget3, new Point2(k, j));
			}
		}
		PlayerData.PlayerTag tag = componentPlayer.PlayerData.Tag;
		m_innerClothingModelWidget.PlayerClass = componentPlayer.PlayerData.PlayerClass;
		m_innerClothingModelWidget.CharacterSkinTexture = m_componentPlayer.ComponentClothing.InnerClothedTexture;
		m_innerClothingModelWidget.BodyTag = tag;
		m_innerClothingModelWidget.HorizontalAlignment = WidgetAlignment.Center;
		m_innerClothingModelWidget.VerticalAlignment = WidgetAlignment.Center;
		m_outerClothingModelWidget.PlayerClass = componentPlayer.PlayerData.PlayerClass;
		m_outerClothingModelWidget.OuterClothingTexture = m_componentPlayer.ComponentClothing.OuterClothedTexture;
		m_outerClothingModelWidget.OuterClothing = true;
		m_outerClothingModelWidget.BodyTag = tag;
		m_outerClothingModelWidget.HorizontalAlignment = WidgetAlignment.Center;
		m_outerClothingModelWidget.VerticalAlignment = WidgetAlignment.Center;
	}

	public override void Update()
	{
		if (m_vitalStatsButton.IsClicked && m_componentPlayer != null)
		{
			m_componentPlayer.ComponentGui.ModalPanelWidget = new VitalStatsWidget(m_componentPlayer);
		}
		if (m_sleepButton.IsClicked && m_componentPlayer != null)
		{
			string ex;
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentSleepPackage(m_componentPlayer.ComponentSleep, ComponentSleepPackage.EventType.SleepRequest, allow: true));
			}
			else if (!m_componentPlayer.ComponentSleep.CanSleep(out ex))
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage(ex, Color.White, blinking: false, playNotificationSound: false);
			}
			else
			{
				m_componentPlayer.ComponentSleep.Sleep(allowManualWakeup: true);
			}
		}
	}
}
