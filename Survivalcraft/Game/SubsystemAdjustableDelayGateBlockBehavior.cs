using Game.NetWork;

namespace Game;

public class SubsystemAdjustableDelayGateBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[1] { 224 };

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		int value = inventory.GetSlotValue(slotIndex);
		inventory.GetSlotCount(slotIndex);
		int data = Terrain.ExtractData(value);
		int delay = AdjustableDelayGateBlock.GetDelay(data);
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditAdjustableDelayGateDialog(delay, (int newDelay) =>
		{
			int data2 = AdjustableDelayGateBlock.SetDelay(data, newDelay);
			if (Terrain.ReplaceData(value, data2) != value)
			{
				if (CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.SendEditItemRequest(EditableItemType.AdjustableDelayGate, isEditAsItem: true, slotIndex, default, newDelay);
				}
				else
				{
					CommonLib.PerformEditItem(Project, EditableItemType.AdjustableDelayGate, (slotIndex, inventory), newDelay);
				}
			}
		}));
		return true;
	}

	public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
	{
		int data = Terrain.ExtractData(value);
		int delay = AdjustableDelayGateBlock.GetDelay(data);
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditAdjustableDelayGateDialog(delay, (int newDelay) =>
		{
			if (AdjustableDelayGateBlock.SetDelay(data, newDelay) != data)
			{
				int face = ((AdjustableDelayGateBlock)BlocksManager.Blocks[224]).GetFace(value);
				if (CommonLib.WorkType == WorkType.Client)
				{
					CommonLib.SendEditItemRequest(EditableItemType.AdjustableDelayGate, isEditAsItem: false, 0, new CellFace(x, y, z, face), newDelay);
				}
				else
				{
					CommonLib.PerformEditItem(Project, EditableItemType.AdjustableDelayGate, new CellFace(x, y, z, face), newDelay);
				}
			}
		}));
		return true;
	}
}
