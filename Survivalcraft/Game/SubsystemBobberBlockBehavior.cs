using TemplatesDatabase;

namespace Game;

public class SubsystemBobberBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

	private SubsystemFishingFloats m_subsystemFishingFloats;

	private SubsystemGameInfo m_subsystemGameInfo;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[1] { 396 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemFishingFloats = Project.FindSubsystem<SubsystemFishingFloats>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	public void Update(float dt)
	{
	}

	public override int GetProcessInventoryItemCapacity(IInventory inv, int slot, int value)
	{
		int slotValue = inv.GetSlotValue(slot);
		if (Terrain.ExtractContents(slotValue) != 396)
		{
			return 0;
		}
		int num = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(slotValue);
		int baitValue = BobberBlock.GetBaitValue(data);
		if (value == 0 || num == 0)
		{
			if (!BobberBlock.HasBait(data))
			{
				return 0;
			}
			return 1;
		}
		if (!BobberBaitInfo.IsAllowedBaitValue(value))
		{
			return 0;
		}
		if (baitValue != 0 && Terrain.ExtractContents(baitValue) == Terrain.ExtractContents(value) && Terrain.ExtractData(baitValue) == Terrain.ExtractData(value))
		{
			return 0;
		}
		return 1;
	}

	public override void ProcessInventoryItem(IInventory inv, int slot, int value, int count, int processCapacity, out int pv, out int pcnt)
	{
		pv = value;
		pcnt = count;
		if (processCapacity != 1)
		{
			return;
		}
		int slotValue = inv.GetSlotValue(slot);
		int num = Terrain.ExtractContents(slotValue);
		if (num != 396)
		{
			return;
		}
		int data = Terrain.ExtractData(slotValue);
		int baitValue = BobberBlock.GetBaitValue(data);
		if (value == 0 || Terrain.ExtractContents(value) == 0)
		{
			if (baitValue != 0)
			{
				int baitRot = BobberBlock.GetBaitRot(data);
				int num2 = ComponentInventoryBase.FindAcquireSlotForItem(inv, baitValue);
				if (num2 == slot)
				{
					num2 = -1;
				}
				if (num2 < 0)
				{
					for (int i = 0; i < inv.SlotsCount; i++)
					{
						if (i != slot && inv.GetSlotValue(i) == 0)
						{
							num2 = i;
							break;
						}
					}
				}
				if (num2 < 0)
				{
					return;
				}
				data = BobberBlock.SetBait(data, 0, 0, 0);
				int value2 = Terrain.MakeBlockValue(num, 0, data);
				inv.RemoveSlotItems(slot, 1);
				inv.AddSlotItems(slot, value2, 1);
				int value3 = baitValue;
				int num3 = Terrain.ExtractContents(baitValue);
				if (num3 != 318)
				{
					value3 = BlocksManager.Blocks[num3].SetDamage(value3, baitRot);
				}
				inv.AddSlotItems(num2, value3, 1);
			}
			pv = 0;
			pcnt = 0;
			return;
		}
		int num4 = Terrain.ExtractContents(value);
		if (!BobberBaitInfo.IsAllowedBaitValue(value) || (baitValue != 0 && Terrain.ExtractContents(baitValue) == num4 && Terrain.ExtractData(baitValue) == Terrain.ExtractData(value)))
		{
			return;
		}
		int baitRot2 = BobberBlock.GetBaitRot(data);
		int num5 = -1;
		if (baitValue != 0)
		{
			int num6 = Terrain.ExtractContents(baitValue);
			int value4 = baitValue;
			if (num6 != 318)
			{
				value4 = BlocksManager.Blocks[num6].SetDamage(value4, baitRot2);
			}
			int num7 = ComponentInventoryBase.FindAcquireSlotForItem(inv, value4);
			if (num7 == slot || (num7 >= 0 && inv.GetSlotValue(num7) == value))
			{
				num7 = -1;
			}
			if (num7 < 0)
			{
				for (int j = 0; j < inv.SlotsCount; j++)
				{
					if (j != slot && inv.GetSlotValue(j) == 0)
					{
						num7 = j;
						break;
					}
				}
			}
			if (num7 < 0)
			{
				return;
			}
			num5 = num7;
		}
		int rot = BlocksManager.Blocks[num4].GetDamage(value) & 1;
		int baitData = Terrain.ExtractData(value) & 0xF;
		data = BobberBlock.SetBait(data, num4, baitData, rot);
		int value5 = Terrain.MakeBlockValue(num, 0, data);
		inv.RemoveSlotItems(slot, 1);
		inv.AddSlotItems(slot, value5, 1);
		if (num5 >= 0 && baitValue != 0)
		{
			int num8 = Terrain.ExtractContents(baitValue);
			int value6 = baitValue;
			if (num8 != 318)
			{
				value6 = BlocksManager.Blocks[num8].SetDamage(value6, baitRot2);
			}
			inv.AddSlotItems(num5, value6, 1);
		}
		pv = 0;
		pcnt = 0;
	}

	public override bool OnEditInventoryItem(IInventory inv, int slot, ComponentPlayer pl)
	{
		pl.ComponentGui.ModalPanelWidget = ((pl.ComponentGui.ModalPanelWidget == null) ? new BobberWidget(inv, slot) : null);
		return true;
	}
}
