using System.Xml.Linq;
using Engine;

namespace Game;

public class ShortInventoryWidget : CanvasWidget
{
	public GridPanelWidget m_inventoryGrid;

	public IInventory m_inventory;

	public ShortInventoryWidget()
	{
		XElement terrain = ContentManager.Get<XElement>("Widgets/ShortInventoryWidget");
		LoadContents(this, terrain);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
	}

	public void AssignComponents(IInventory inventory)
	{
		if (inventory != m_inventory)
		{
			m_inventory = inventory;
			m_inventoryGrid.Children.Clear();
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		if (m_inventory == null)
		{
			return;
		}
		int result = ((m_inventory is ComponentCreativeInventory) ? 10 : 7);
		int num = MathUtils.Clamp((int)((parentAvailableSize.X - 320f - 25f) / 72f), 7, result);
		if (!(m_inventory is ComponentCreativeInventory))
		{
			try
			{
				if (m_inventory.Project != null)
				{
					SubsystemGameInfo subsystemGameInfo = m_inventory.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
					if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
					{
						num = MathUtils.Clamp(2, 1, num);
					}
				}
			}
			catch
			{
			}
		}
		m_inventory.VisibleSlotsCount = num;
		if (m_inventory.VisibleSlotsCount != m_inventoryGrid.Children.Count)
		{
			m_inventoryGrid.Children.Clear();
			m_inventoryGrid.RowsCount = 1;
			m_inventoryGrid.ColumnsCount = m_inventory.VisibleSlotsCount;
			for (int i = 0; i < m_inventoryGrid.ColumnsCount; i++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(m_inventory, i);
				inventorySlotWidget.BevelColor = new Color(181, 172, 154) * 0.6f;
				inventorySlotWidget.CenterColor = new Color(181, 172, 154) * 0.33f;
				m_inventoryGrid.Children.Add(inventorySlotWidget);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(i, 0));
			}
		}
		base.MeasureOverride(parentAvailableSize);
	}

	public override void Update()
	{
		base.Update();
		float num = 1f - MathUtils.Saturate(SettingsManager.UITransparent);
		ColorTransform = Color.White * num;
	}
}
