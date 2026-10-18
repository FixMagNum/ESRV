using System.Xml.Linq;
using Engine;

namespace Game;

public class BobberWidget : CanvasWidget
{
	private IInventory m_inv;

	private int m_slot;

	private GridPanelWidget m_grid;

	public static string fName = "BobberWidget";

	private InventorySlotWidget m_bobberSlotWidget;

	private LabelWidget m_baitNameLabel;

	private LabelWidget m_baitStrengthLabel;

	private LabelWidget m_baitRadiusLabel;

	private ButtonWidget m_removeButton;

	private SubsystemTerrain m_terrain;

	public BobberWidget(IInventory inv, int slot)
	{
		m_inv = inv;
		m_slot = slot;
		XElement node = ContentManager.Get<XElement>("Widgets/BobberWidget");
		LoadContents(this, node);
		m_grid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_bobberSlotWidget = Children.Find<InventorySlotWidget>("BobberSlot");
		m_baitNameLabel = Children.Find<LabelWidget>("BaitNameLabel");
		m_baitStrengthLabel = Children.Find<LabelWidget>("BaitStrengthLabel");
		m_baitRadiusLabel = Children.Find<LabelWidget>("BaitRadiusLabel");
		m_removeButton = Children.Find<ButtonWidget>("RemoveBaitButton");
		LanguageControl.Get(fName, 3);
		LanguageControl.Get(fName, 5);
		if (m_inv is ComponentInventory componentInventory)
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
		m_bobberSlotWidget.AssignInventorySlot(inv, slot);
		SetBobberViewMatrix(hasBait: false);
		UpdateLabels();
	}

	public override void Update()
	{
		int slotValue = m_inv.GetSlotValue(m_slot);
		int slotCount = m_inv.GetSlotCount(m_slot);
		if (Terrain.ExtractContents(slotValue) != 396 || slotCount == 0)
		{
			ParentWidget.Children.Remove(this);
			return;
		}
		if (BobberBlock.GetBait(Terrain.ExtractData(slotValue)) != 0 && m_removeButton.IsClicked)
		{
			RemoveBait();
		}
		UpdateLabels();
	}

	private void UpdateLabels()
	{
		int baitValue = BobberBlock.GetBaitValue(Terrain.ExtractData(m_inv.GetSlotValue(m_slot)));
		bool flag = baitValue != 0;
		m_removeButton.IsVisible = flag;
		m_removeButton.IsEnabled = flag;
		SetBobberViewMatrix(flag);
		if (!flag)
		{
			m_baitNameLabel.Text = LanguageControl.Get(fName, 0);
			m_baitStrengthLabel.Text = $"{LanguageControl.Get(fName, 1)}: 0";
			m_baitRadiusLabel.Text = string.Format("{0}: {1}", LanguageControl.Get(fName, 2), "0x0");
			return;
		}
		int num = Terrain.ExtractContents(baitValue);
		string displayName = BlocksManager.Blocks[num].GetDisplayName(m_terrain, baitValue);
		int baitStrengthByValue = BobberBaitInfo.GetBaitStrengthByValue(baitValue);
		string radiusString = BobberBaitInfo.GetRadiusString(baitStrengthByValue);
		m_baitNameLabel.Text = displayName;
		m_baitStrengthLabel.Text = $"{LanguageControl.Get(fName, 1)}: {baitStrengthByValue}/4";
		m_baitRadiusLabel.Text = $"{LanguageControl.Get(fName, 2)}: {radiusString}";
	}

	private void RemoveBait()
	{
		int slotValue = m_inv.GetSlotValue(m_slot);
		int num = Terrain.ExtractContents(slotValue);
		if (num != 396)
		{
			return;
		}
		int data = Terrain.ExtractData(slotValue);
		int baitValue = BobberBlock.GetBaitValue(data);
		if (baitValue == 0)
		{
			return;
		}
		int baitRot = BobberBlock.GetBaitRot(data);
		int num2 = ComponentInventoryBase.FindAcquireSlotForItem(m_inv, baitValue);
		if (num2 < 0 && m_inv.GetSlotCount(m_slot) == 1)
		{
			num2 = m_slot;
		}
		if (num2 < 0)
		{
			if (m_inv is ComponentInventory componentInventory)
			{
				componentInventory.Entity.FindComponent<ComponentPlayer>()?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 4), Color.White, blinking: true, playNotificationSound: false);
			}
			return;
		}
		data = BobberBlock.SetBait(data, 0, 0, 0);
		int value = Terrain.MakeBlockValue(num, 0, data);
		m_inv.RemoveSlotItems(m_slot, 1);
		m_inv.AddSlotItems(m_slot, value, 1);
		int value2 = baitValue;
		int num3 = Terrain.ExtractContents(baitValue);
		if (num3 != 318)
		{
			value2 = BlocksManager.Blocks[num3].SetDamage(value2, baitRot);
		}
		m_inv.AddSlotItems(num2, value2, 1);
	}

	private void SetBobberViewMatrix(bool hasBait)
	{
		float y = (hasBait ? 0.6f : 0.3f);
		Matrix matrix = Matrix.CreateTranslation(0f, y, 0f);
		Matrix identity = Matrix.Identity;
		m_bobberSlotWidget.CustomViewMatrix = Matrix.CreateScale(0.6f) * matrix * identity;
	}
}
