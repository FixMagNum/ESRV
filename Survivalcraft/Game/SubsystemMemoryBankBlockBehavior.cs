using Engine;
using Game.NetWork;

namespace Game;

public class SubsystemMemoryBankBlockBehavior : SubsystemEditableItemBehavior<MemoryBankData>
{
	public override int[] HandledBlocks => new int[1] { 186 };

	public SubsystemMemoryBankBlockBehavior()
		: base(186)
	{
	}

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		int slotValue = inventory.GetSlotValue(slotIndex);
		inventory.GetSlotCount(slotIndex);
		int obj = Terrain.ExtractData(slotValue);
		MemoryBankData memoryBankData = GetItemData(obj);
		memoryBankData = ((memoryBankData != null) ? ((MemoryBankData)memoryBankData.Copy()) : new MemoryBankData());
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditMemoryBankDialogAPI(memoryBankData, () =>
		{
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.SendEditItemRequest(EditableItemType.MemoryBank, isEditAsItem: true, slotIndex, default, memoryBankData);
			}
			else
			{
				CommonLib.PerformEditItem(Project, EditableItemType.MemoryBank, (slotIndex, inventory), memoryBankData);
			}
		}));
		return true;
	}

	public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
	{
		MemoryBankData memoryBankData = GetBlockData(new Point3(x, y, z)) ?? new MemoryBankData();
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditMemoryBankDialogAPI(memoryBankData, () =>
		{
			int face = ((MemoryBankBlock)BlocksManager.Blocks[186]).GetFace(value);
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.SendEditItemRequest(EditableItemType.MemoryBank, isEditAsItem: false, 0, new CellFace(x, y, z, face), memoryBankData);
			}
			else
			{
				CommonLib.PerformEditItem(Project, EditableItemType.MemoryBank, new CellFace(x, y, z, face), memoryBankData);
			}
		}));
		return true;
	}
}
