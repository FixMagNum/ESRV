using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemBagBlockBehavior : SubsystemBlockBehavior
{
	private SubsystemBags m_storage;

	public override int[] HandledBlocks => new int[4] { 364, 416, 421, 422 };

	public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
	{
		int slotValue = inventory.GetSlotValue(slotIndex);
		int slotCount = inventory.GetSlotCount(slotIndex);
		int num = Terrain.ExtractContents(slotValue);
		Block block = BlocksManager.Blocks[num];
		if ((!(block is BagBlock) && !(block is LargeBagBlock) && !(block is BackpackBlock) && !(block is ClimbingBackpackBlock)) || slotCount <= 0)
		{
			componentPlayer.ComponentGui.ModalPanelWidget = null;
			return false;
		}
		int data = Terrain.ExtractData(slotValue);
		int num2;
		if (block is BagBlock)
		{
			num2 = BagBlock.GetBagId(data);
		}
		else if (block is LargeBagBlock)
		{
			num2 = LargeBagBlock.GetBagId(data);
		}
		else
		{
			num2 = ((!(block is BackpackBlock)) ? ClimbingBackpackBlock.GetBagId(data) : BackpackBlock.GetBagId(data));
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			if (num2 <= 0)
			{
				CommonLib.Net.QueuePackage(new BagPackage(inventory.Id, slotIndex));
				componentPlayer.ComponentGui.ModalPanelWidget = null;
				return true;
			}
			CommonLib.Net.QueuePackage(new BagPackage(inventory.Id, slotIndex));
		}
		int capacity;
		if (block is BagBlock)
		{
			capacity = 4;
		}
		else if (block is LargeBagBlock)
		{
			capacity = 6;
		}
		else
		{
			capacity = ((block is BackpackBlock) ? 9 : 16);
		}
		BagInventory bagInventory = m_storage.GetOrCreate(num2, capacity, out var actualId);
		if (CommonLib.WorkType != WorkType.Client && actualId != num2)
		{
			int data2;
			if (block is BagBlock)
			{
				data2 = BagBlock.SetBagId(data, actualId);
			}
			else if (block is LargeBagBlock)
			{
				data2 = LargeBagBlock.SetBagId(data, actualId);
			}
			else
			{
				data2 = ((!(block is BackpackBlock)) ? ClimbingBackpackBlock.SetBagId(data, actualId) : BackpackBlock.SetBagId(data, actualId));
			}
			int value = Terrain.MakeBlockValue(num, 0, data2);
			inventory.RemoveSlotItems(slotIndex, 1);
			inventory.AddSlotItems(slotIndex, value, 1);
			bagInventory = m_storage.Find(actualId);
		}
		if (bagInventory == null)
		{
			componentPlayer.ComponentGui.ModalPanelWidget = null;
			return true;
		}
		componentPlayer.ComponentGui.ModalPanelWidget = ((componentPlayer.ComponentGui.ModalPanelWidget == null) ? new BagWidget(inventory, slotIndex, bagInventory) : null);
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_storage = Project.FindSubsystem<SubsystemBags>(throwOnError: true);
		base.Load(valuesDictionary);
	}
}
