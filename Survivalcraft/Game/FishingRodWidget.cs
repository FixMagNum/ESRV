using System.Xml.Linq;
using Engine;

namespace Game;

public class FishingRodWidget : CanvasWidget
{
	private IInventory m_inventory;

	private int m_slotIndex;

	private GridPanelWidget m_inventoryGrid;

	private InventorySlotWidget m_inventorySlotWidget;

	private LabelWidget m_instructionsLabel;

	private ButtonWidget m_removeBobberButton;

	private ButtonWidget m_removeBaitButton;

	public static string fName;

	public FishingRodWidget(IInventory inventory, int slotIndex)
	{
		m_inventory = inventory;
		m_slotIndex = slotIndex;
		XElement node = ContentManager.Get<XElement>("Widgets/FishingRodWidget");
		LoadContents(this, node);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_inventorySlotWidget = Children.Find<InventorySlotWidget>("InventorySlot");
		m_instructionsLabel = Children.Find<LabelWidget>("InstructionsLabel");
		m_removeBobberButton = Children.Find<ButtonWidget>("RemoveBobberButton");
		m_removeBaitButton = Children.Find<ButtonWidget>("RemoveBaitButton");
		for (int i = 0; i < m_inventoryGrid.RowsCount; i++)
		{
			for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++)
			{
				InventorySlotWidget widget = new InventorySlotWidget();
				m_inventoryGrid.Children.Add(widget);
				m_inventoryGrid.SetWidgetCell(widget, new Point2(j, i));
			}
		}
		int num = 10;
		foreach (Widget child in m_inventoryGrid.Children)
		{
			if (child is InventorySlotWidget inventorySlotWidget)
			{
				inventorySlotWidget.AssignInventorySlot(inventory, num++);
			}
		}
		m_inventorySlotWidget.AssignInventorySlot(inventory, slotIndex);
		Matrix matrix = Matrix.CreateLookAt(new Vector3(-1f, 0.2f, 0.6f), new Vector3(0f, 0.2f, 0f), Vector3.UnitY);
		Matrix matrix2 = Matrix.CreateScale(0.5f);
		Matrix matrix3 = Matrix.CreateRotationY(MathUtils.DegToRad(-90f));
		Matrix matrix4 = Matrix.CreateTranslation(0.3f, 0f, 0f);
		m_inventorySlotWidget.CustomViewMatrix = matrix2 * matrix3 * matrix4 * matrix;
	}

	public override void Update()
	{
		int slotValue = m_inventory.GetSlotValue(m_slotIndex);
		if (Terrain.ExtractContents(slotValue) != 395 || m_inventory.GetSlotCount(m_slotIndex) == 0)
		{
			ParentWidget.Children.Remove(this);
			return;
		}
		int data = Terrain.ExtractData(slotValue);
		bool flag = FishingRodBlock.HasBobber(data);
		int bobberBait = FishingRodBlock.GetBobberBait(data);
		bool flag2 = flag && bobberBait != 0;
		m_removeBobberButton.IsVisible = flag;
		m_removeBobberButton.IsEnabled = flag;
		m_removeBaitButton.IsVisible = flag2;
		m_removeBaitButton.IsEnabled = flag2;
		if (flag && m_removeBobberButton.IsClicked)
		{
			int rot = FishingRodBlock.GetBobberBaitRot(data) & 1;
			int baitData = FishingRodBlock.GetRodBaitData(data) & 0xF;
			int data2 = BobberBlock.SetBait(0, bobberBait, baitData, rot);
			int value = Terrain.MakeBlockValue(396, 0, data2);
			int num = ComponentInventoryBase.FindAcquireSlotForItem(m_inventory, value);
			if (num >= 0)
			{
				m_inventory.AddSlotItems(num, value, 1);
				data = FishingRodBlock.SetRodBaitData(data, 0);
				data = FishingRodBlock.SetBobber(data, hasBobber: false, 0, 0);
				int value2 = Terrain.MakeBlockValue(395, 0, data);
				m_inventory.RemoveSlotItems(m_slotIndex, 1);
				m_inventory.AddSlotItems(m_slotIndex, value2, 1);
				flag = false;
				flag2 = false;
			}
		}
		if (flag2 && m_removeBaitButton.IsClicked)
		{
			int damage = FishingRodBlock.GetBobberBaitRot(data) & 1;
			int data3 = FishingRodBlock.GetRodBaitData(data) & 0xF;
			int value3 = Terrain.MakeBlockValue(bobberBait, 0, data3);
			if (bobberBait != 318)
			{
				value3 = BlocksManager.Blocks[bobberBait].SetDamage(value3, damage);
			}
			int num2 = ComponentInventoryBase.FindAcquireSlotForItem(m_inventory, value3);
			if (num2 >= 0)
			{
				m_inventory.AddSlotItems(num2, value3, 1);
				data = FishingRodBlock.SetRodBaitData(data, 0);
				data = FishingRodBlock.SetBobber(data, hasBobber: true, 0, 0);
				int value4 = Terrain.MakeBlockValue(395, 0, data);
				m_inventory.RemoveSlotItems(m_slotIndex, 1);
				m_inventory.AddSlotItems(m_slotIndex, value4, 1);
				flag2 = false;
			}
		}
		string text;
		if (flag)
		{
			text = (flag2 ? LanguageControl.Get(fName, 5) : LanguageControl.Get(fName, 4));
		}
		else
		{
			text = LanguageControl.Get(fName, 3);
		}
		int num3 = 0;
		string text2 = "0x0";
		if (flag2)
		{
			num3 = BobberBaitInfo.GetBaitStrength(bobberBait);
			text2 = BobberBaitInfo.GetRadiusString(num3);
		}
		string text3 = LanguageControl.Get(BobberWidget.fName, 1);
		string text4 = LanguageControl.Get(BobberWidget.fName, 2);
		m_instructionsLabel.Text = $"{text}\n{text3}: {num3}/4\n{text4}: {text2}";
	}

	static FishingRodWidget()
	{
		fName = "FishingRodWidget";
	}
}
