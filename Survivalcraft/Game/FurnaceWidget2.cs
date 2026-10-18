using System.Xml.Linq;
using Engine;

namespace Game;

public class FurnaceWidget2 : CanvasWidget
{
	public GridPanelWidget m_inventoryGrid;

	public GridPanelWidget m_furnaceGrid;

	public InventorySlotWidget m_fuelSlot;

	public InventorySlotWidget m_resultSlot;

	public InventorySlotWidget m_remainsSlot;

	public FireWidget m_fire;

	public ValueBarWidget m_progress;

	public ComponentFurnace2 m_componentFurnace2;

	public override void Update()
	{
		m_fire.ParticlesPerSecond = ((m_componentFurnace2.HeatLevel > 0f) ? 24 : 0);
		m_progress.Value = m_componentFurnace2.SmeltingProgress;
		if (m_componentFurnace2.Entity == null)
		{
			ParentWidget.Children.Remove(this);
		}
	}

	public FurnaceWidget2(IInventory inventory, ComponentFurnace2 componentFurnace2)
	{
		m_componentFurnace2 = componentFurnace2;
		XElement node = ContentManager.Get<XElement>("Widgets/FurnaceWidget2");
		LoadContents(this, node);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_furnaceGrid = Children.Find<GridPanelWidget>("FurnaceGrid");
		m_fire = Children.Find<FireWidget>("Fire");
		m_progress = Children.Find<ValueBarWidget>("Progress");
		m_resultSlot = Children.Find<InventorySlotWidget>("ResultSlot");
		m_remainsSlot = Children.Find<InventorySlotWidget>("RemainsSlot");
		m_fuelSlot = Children.Find<InventorySlotWidget>("FuelSlot");
		int num = 10;
		for (int i = 0; i < m_inventoryGrid.RowsCount; i++)
		{
			for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(inventory, num++);
				m_inventoryGrid.Children.Add(inventorySlotWidget);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
			}
		}
		num = 0;
		for (int k = 0; k < m_furnaceGrid.RowsCount; k++)
		{
			for (int l = 0; l < m_furnaceGrid.ColumnsCount; l++)
			{
				InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget();
				inventorySlotWidget2.AssignInventorySlot(componentFurnace2, num++);
				m_furnaceGrid.Children.Add(inventorySlotWidget2);
				m_furnaceGrid.SetWidgetCell(inventorySlotWidget2, new Point2(l, k));
			}
		}
		m_fuelSlot.AssignInventorySlot(componentFurnace2, componentFurnace2.FuelSlotIndex);
		m_resultSlot.AssignInventorySlot(componentFurnace2, componentFurnace2.ResultSlotIndex);
		m_remainsSlot.AssignInventorySlot(componentFurnace2, componentFurnace2.RemainsSlotIndex);
	}
}
