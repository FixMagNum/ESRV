using System.Xml.Linq;
using Engine;
using Game.NetWork;
using LiteNetLib.Utils;

namespace Game;

public class DispenserWidget : CanvasWidget
{
	public SubsystemTerrain m_subsystemTerrain;

	public ComponentDispenser m_componentDispenser;

	public ComponentBlockEntity m_componentBlockEntity;

	public GridPanelWidget m_inventoryGrid;

	public GridPanelWidget m_dispenserGrid;

	public ButtonWidget m_dispenseButton;

	public ButtonWidget m_shootButton;

	public CheckboxWidget m_acceptsDropsBox;

	public DispenserWidget(IInventory inventory, ComponentDispenser componentDispenser)
	{
		m_componentDispenser = componentDispenser;
		m_componentBlockEntity = componentDispenser.Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
		m_subsystemTerrain = componentDispenser.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		XElement node = ContentManager.Get<XElement>("Widgets/DispenserWidget");
		LoadContents(this, node);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_dispenserGrid = Children.Find<GridPanelWidget>("DispenserGrid");
		m_dispenseButton = Children.Find<ButtonWidget>("DispenseButton");
		m_shootButton = Children.Find<ButtonWidget>("ShootButton");
		m_acceptsDropsBox = Children.Find<CheckboxWidget>("AcceptsDropsBox");
		int num = 0;
		for (int i = 0; i < m_dispenserGrid.RowsCount; i++)
		{
			for (int j = 0; j < m_dispenserGrid.ColumnsCount; j++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(componentDispenser, num++);
				m_dispenserGrid.Children.Add(inventorySlotWidget);
				m_dispenserGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
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
		int data = m_subsystemTerrain.Terrain.GetCellValue(m_componentBlockEntity.Coordinates.X, m_componentBlockEntity.Coordinates.Y, m_componentBlockEntity.Coordinates.Z);
		int data2 = Terrain.ExtractData(data);
		bool electricElement = false;
		if (m_dispenseButton.IsClicked)
		{
			data2 = DispenserBlock.SetMode(data2, DispenserBlock.Mode.Dispense);
			data = Terrain.ReplaceData(data, data2);
			electricElement = true;
			m_subsystemTerrain.ChangeCell(m_componentBlockEntity.Coordinates.X, m_componentBlockEntity.Coordinates.Y, m_componentBlockEntity.Coordinates.Z, data);
		}
		if (m_shootButton.IsClicked)
		{
			data2 = DispenserBlock.SetMode(data2, DispenserBlock.Mode.Shoot);
			data = Terrain.ReplaceData(data, data2);
			electricElement = true;
			m_subsystemTerrain.ChangeCell(m_componentBlockEntity.Coordinates.X, m_componentBlockEntity.Coordinates.Y, m_componentBlockEntity.Coordinates.Z, data);
		}
		if (m_acceptsDropsBox.IsClicked)
		{
			data2 = DispenserBlock.SetAcceptsDrops(data2, !DispenserBlock.GetAcceptsDrops(data2));
			data = Terrain.ReplaceData(data, data2);
			electricElement = true;
			m_subsystemTerrain.ChangeCell(m_componentBlockEntity.Coordinates.X, m_componentBlockEntity.Coordinates.Y, m_componentBlockEntity.Coordinates.Z, data);
		}
		DispenserBlock.Mode mode = DispenserBlock.GetMode(data2);
		if (electricElement && CommonLib.WorkType == WorkType.Client)
		{
			CommonLib.Net.SendPackage(PackageFlag.ChangeDispenserRequest, (NetDataWriter w) =>
			{
				w.PutPoint3(m_componentBlockEntity.Coordinates);
				w.Put((byte)(((mode == DispenserBlock.Mode.Shoot) ? 1u : 0u) | (uint)(DispenserBlock.GetAcceptsDrops(data2) ? 2 : 0)));
			});
		}
		m_dispenseButton.IsChecked = mode == DispenserBlock.Mode.Dispense;
		m_shootButton.IsChecked = mode == DispenserBlock.Mode.Shoot;
		m_acceptsDropsBox.IsChecked = DispenserBlock.GetAcceptsDrops(data2);
		if (!m_componentDispenser.IsAddedToProject)
		{
			ParentWidget.Children.Remove(this);
		}
	}
}
