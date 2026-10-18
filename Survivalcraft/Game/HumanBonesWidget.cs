using System.Xml.Linq;
using Engine;

namespace Game;

public class HumanBonesWidget : CanvasWidget
{
	public GridPanelWidget m_inventoryGrid;

	public ComponentHumanBones m_componentHumanBones;

	public GridPanelWidget m_humanbonesGrid;

	public override void Update()
	{
		if (m_componentHumanBones.Entity == null)
		{
			ParentWidget.Children.Remove(this);
		}
	}

	public HumanBonesWidget(IInventory inventory, ComponentHumanBones componentHumanBones)
	{
		m_componentHumanBones = componentHumanBones;
		XElement num = ContentManager.Get<XElement>("Widgets/HumanBonesWidget");
		LoadContents(this, num);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_humanbonesGrid = Children.Find<GridPanelWidget>("ChestGrid");
		int num2 = 0;
		for (int i = 0; i < m_humanbonesGrid.RowsCount; i++)
		{
			for (int j = 0; j < m_humanbonesGrid.ColumnsCount; j++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(componentHumanBones, num2++);
				m_humanbonesGrid.Children.Add(inventorySlotWidget);
				m_humanbonesGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
			}
		}
		num2 = 10;
		for (int k = 0; k < m_inventoryGrid.RowsCount; k++)
		{
			for (int l = 0; l < m_inventoryGrid.ColumnsCount; l++)
			{
				InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget();
				inventorySlotWidget2.AssignInventorySlot(inventory, num2++);
				m_inventoryGrid.Children.Add(inventorySlotWidget2);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget2, new Point2(l, k));
			}
		}
	}
}
