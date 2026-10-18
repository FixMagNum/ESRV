using Game.NetWork;

namespace Game;

public class SubsystemBatteryBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[1] { 138 };

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		int value = inventory.GetSlotValue(slotIndex);
		int data = Terrain.ExtractData(value);
		int result = BatteryBlock.GetVoltageLevel(data);
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditBatteryDialog(result, (int newVoltageLevel) =>
		{
			int data2 = BatteryBlock.SetVoltageLevel(data, newVoltageLevel);
			if (Terrain.ReplaceData(value, data2) != value)
			{
				if (CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.SendEditItemRequest(EditableItemType.Battery, isEditAsItem: true, slotIndex, default, newVoltageLevel);
				}
				else
				{
					CommonLib.PerformEditItem(Project, EditableItemType.Battery, (slotIndex, inventory), newVoltageLevel);
				}
			}
		}));
		return true;
	}

	public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
	{
		int data = Terrain.ExtractData(value);
		int num2 = BatteryBlock.GetVoltageLevel(data);
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditBatteryDialog(num2, (int newVoltageLevel) =>
		{
			if (BatteryBlock.SetVoltageLevel(data, newVoltageLevel) != data)
			{
				int mountingFace = BatteryBlock.GetMountingFace(data);
				if (CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.SendEditItemRequest(EditableItemType.Battery, isEditAsItem: false, 0, new CellFace(x, y, z, mountingFace), newVoltageLevel);
				}
				else
				{
					CommonLib.PerformEditItem(Project, EditableItemType.Battery, new CellFace(x, y, z, mountingFace), newVoltageLevel);
				}
			}
		}));
		return true;
	}
}
