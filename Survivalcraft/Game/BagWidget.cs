using System.Xml.Linq;
using Engine;

namespace Game;

public class BagWidget : CanvasWidget
{
	private GridPanelWidget m_inventoryGrid;

	private GridPanelWidget m_bagGrid;

	private LabelWidget m_bagLabel;

	private LabelWidget m_inventoryLabel;

	private IInventory m_playerInventory;

	private int m_bagItemSlotIndex;

	private IInventory m_bagInventory;

	public override void Update()
	{
		int slotValue = m_playerInventory.GetSlotValue(m_bagItemSlotIndex);
		int slotCount = m_playerInventory.GetSlotCount(m_bagItemSlotIndex);
		int num = Terrain.ExtractContents(slotValue);
		Block block = BlocksManager.Blocks[num];
		if ((!(block is BagBlock) && !(block is LargeBagBlock) && !(block is BackpackBlock) && !(block is ClimbingBackpackBlock)) || slotCount <= 0)
		{
			ParentWidget.Children.Remove(this);
		}
	}

	public BagWidget(IInventory playerInventory, int bagItemSlotIndex, IInventory bagInventory)
	{
		m_playerInventory = playerInventory;
		m_bagItemSlotIndex = bagItemSlotIndex;
		m_bagInventory = bagInventory;
		XElement node = ContentManager.Get<XElement>("Widgets/BagWidget");
		LoadContents(this, node);
		m_bagGrid = Children.Find<GridPanelWidget>("BagGrid");
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_bagLabel = Children.Find<LabelWidget>("BagLabel");
		m_inventoryLabel = Children.Find<LabelWidget>("InventoryLabel");
		int num = MathUtils.Clamp(m_bagInventory.SlotsCount, 1, 16);
		int num2 = ((num <= 4) ? 2 : ((num <= 9) ? 3 : 4));
		int num3 = (num + num2 - 1) / num2;
		Vector2 position = ((num2 == 2 && num3 == 2) ? new Vector2(46f, 110f) : new Vector2(12f, 52f));
		CanvasWidget.SetPosition(m_bagGrid, position);
		m_bagGrid.ColumnsCount = num2;
		m_bagGrid.RowsCount = num3;
		m_bagGrid.Children.Clear();
		for (int i = 0; i < num; i++)
		{
			InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
			inventorySlotWidget.AssignInventorySlot(m_bagInventory, i);
			m_bagGrid.Children.Add(inventorySlotWidget);
			int columnsCount = m_bagGrid.ColumnsCount;
			m_bagGrid.SetWidgetCell(inventorySlotWidget, new Point2(i % columnsCount, i / columnsCount));
		}
		m_inventoryGrid.Children.Clear();
		for (int j = 0; j < m_inventoryGrid.RowsCount; j++)
		{
			for (int k = 0; k < m_inventoryGrid.ColumnsCount; k++)
			{
				InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget();
				inventorySlotWidget2.AssignInventorySlot(m_playerInventory, 10 + j * m_inventoryGrid.ColumnsCount + k);
				m_inventoryGrid.Children.Add(inventorySlotWidget2);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget2, new Point2(k, j));
			}
		}
	}
}
