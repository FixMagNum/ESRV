using System.Xml.Linq;
using Engine;

namespace Game;

public class StrawBasketWidget : CanvasWidget
{
	public ComponentStrawBasket m_componentStrawBasket;

	public GridPanelWidget m_inventoryGrid;

	public GridPanelWidget m_chestGrid;

	public StrawBasketWidget(IInventory inventory, ComponentStrawBasket componentStrawBasket)
	{
		m_componentStrawBasket = componentStrawBasket;
		XElement node = ContentManager.Get<XElement>("Widgets/StrawBasketWidget");
		LoadContents(this, node);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_chestGrid = Children.Find<GridPanelWidget>("ChestGrid");
		int num = 0;
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(componentStrawBasket, num++);
				m_chestGrid.Children.Add(inventorySlotWidget);
				m_chestGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
			}
		}
		num = 10;
		for (int k = 0; k < m_inventoryGrid.RowsCount; k++)
		{
			for (int l = 0; l < m_inventoryGrid.ColumnsCount; l++)
			{
				InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget();
				inventorySlotWidget2.AssignInventorySlot(inventory, num++);
				m_inventoryGrid.Children.Add(inventorySlotWidget2);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget2, new Point2(l, k));
			}
		}
	}

	public override void Update()
	{
		if (m_componentStrawBasket.Entity == null)
		{
			ParentWidget.Children.Remove(this);
		}
	}
}
