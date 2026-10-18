using System.Xml.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class PaintGunWidget : CanvasWidget
{
	public static string fName;

	private IInventory m_inv;

	private int m_slot;

	private GridPanelWidget m_grid;

	private InventorySlotWidget m_slotWidget;

	private LabelWidget m_label;

	private ButtonWidget m_btn;

	private ValueBarWidget m_fluidBar;

	private SubsystemTerrain m_terrain;

	public PaintGunWidget(IInventory inv, int slot)
	{
		m_inv = inv;
		m_slot = slot;
		XElement node = ContentManager.Get<XElement>("Widgets/PaintGunWidget");
		LoadContents(this, node);
		m_grid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_slotWidget = Children.Find<InventorySlotWidget>("InventorySlot");
		m_label = Children.Find<LabelWidget>("InstructionsLabel");
		m_btn = Children.Find<ButtonWidget>("RangeButton");
		m_fluidBar = Children.Find<ValueBarWidget>("FluidValueBar");
		m_fluidBar.FlipDirection = true;
		if (inv is ComponentInventory componentInventory)
		{
			m_terrain = componentInventory.Entity.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		}
		for (int i = 0; i < m_grid.RowsCount; i++)
		{
			for (int j = 0; j < m_grid.ColumnsCount; j++)
			{
				InventorySlotWidget widget = new InventorySlotWidget();
				m_grid.Children.Add(widget);
				m_grid.SetWidgetCell(widget, new Point2(j, i));
			}
		}
		int num = 10;
		foreach (Widget child in m_grid.Children)
		{
			if (child is InventorySlotWidget inventorySlotWidget)
			{
				inventorySlotWidget.AssignInventorySlot(inv, num++);
			}
		}
		m_slotWidget.AssignInventorySlot(inv, slot);
		Matrix matrix = Matrix.CreateLookAt(new Vector3(1f, 0f, 0f), Vector3.Zero, -Vector3.UnitZ);
		m_slotWidget.CustomViewMatrix = Matrix.CreateScale(0.6f) * matrix;
		bool flag = Terrain.ExtractContents(inv.GetSlotValue(slot)) == 411;
		m_btn.Text = LanguageControl.Get(fName, flag ? 2 : 3);
	}

	public override void Update()
	{
		int slotValue = m_inv.GetSlotValue(m_slot);
		int slotCount = m_inv.GetSlotCount(m_slot);
		int num = Terrain.ExtractContents(slotValue);
		if ((num != 410 && num != 411) || slotCount == 0)
		{
			ParentWidget.Children.Remove(this);
			return;
		}
		int num2 = Terrain.ExtractData(slotValue);
		bool flag = PaintGunNearBlock.GetColor(num2) != 0;
		bool cleaner = PaintGunNearBlock.GetCleaner(num2);
		int key = (flag ? 1 : (cleaner ? 5 : 0));
		m_label.Text = LanguageControl.Get(fName, key);
		int fluid = PaintGunNearBlock.GetFluid(num2);
		m_fluidBar.Value = (float)fluid / 60f;
		if (cleaner)
		{
			m_fluidBar.LitBarColor = new Color(255, 255, 255, 100);
		}
		else
		{
			int color = PaintGunNearBlock.GetColor(num2);
			Color color2 = SubsystemPalette.GetColor(m_terrain, color);
			m_fluidBar.LitBarColor = new Color(color2, (byte)((float)(int)color2.A * 0.5f));
		}
		if (m_btn.IsClicked)
		{
			int num3 = ((num == 410) ? 411 : 410);
			int value = Terrain.MakeBlockValue(num3, 0, num2);
			m_inv.RemoveSlotItems(m_slot, 1);
			m_inv.AddSlotItems(m_slot, value, 1);
			m_slotWidget.AssignInventorySlot(m_inv, m_slot);
			m_btn.Text = LanguageControl.Get(fName, (num3 == 411) ? 2 : 3);
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new PaintGunPackage(m_inv.Id, m_slot));
			}
		}
	}

	static PaintGunWidget()
	{
		fName = "PaintGunWidget";
	}
}
