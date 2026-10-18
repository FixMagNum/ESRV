using Engine;
using Game.NetWork;

namespace Game;

public class SubsystemTruthTableCircuitBlockBehavior : SubsystemEditableItemBehavior<TruthTableData>
{
	public override int[] HandledBlocks => new int[1] { 188 };

	public SubsystemTruthTableCircuitBlockBehavior()
		: base(188)
	{
	}

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		int slotValue = inventory.GetSlotValue(slotIndex);
		inventory.GetSlotCount(slotIndex);
		int id = Terrain.ExtractData(slotValue);
		TruthTableData truthTableData = GetItemData(id);
		truthTableData = ((truthTableData != null) ? ((TruthTableData)truthTableData.Copy()) : new TruthTableData());
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditTruthTableDialog(truthTableData, delegate
		{
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.SendEditItemRequest(EditableItemType.TruthTable, isEditAsItem: true, slotIndex, default, truthTableData);
			}
			else
			{
				CommonLib.PerformEditItem(Project, EditableItemType.TruthTable, (slotIndex, inventory), truthTableData);
			}
		}));
		return true;
	}

	public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
	{
		TruthTableData truthTableData = GetBlockData(new Point3(x, y, z)) ?? new TruthTableData();
		DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditTruthTableDialog(truthTableData, delegate
		{
			int face = ((TruthTableCircuitBlock)BlocksManager.Blocks[188]).GetFace(value);
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.SendEditItemRequest(EditableItemType.TruthTable, isEditAsItem: false, 0, new CellFace(x, y, z, face), truthTableData);
			}
			else
			{
				CommonLib.PerformEditItem(Project, EditableItemType.TruthTable, new CellFace(x, y, z, face), truthTableData);
			}
		}));
		return true;
	}
}
