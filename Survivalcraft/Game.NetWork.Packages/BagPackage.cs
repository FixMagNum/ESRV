using System.Collections.Generic;

namespace Game.NetWork.Packages;

public class BagPackage : IPackage
{
	public enum EventType
	{
		RequestOpen,
		SlotUpdate
	}

	public EventType m_eventType;

	public int m_inventoryId;

	public int m_slotIndex;

	public int m_value;

	public int m_count;

	public int m_bagId;

	public byte ID => 36;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public BagPackage()
	{
	}

	public BagPackage(int inventoryId, int slotIndex)
	{
		m_eventType = EventType.RequestOpen;
		m_inventoryId = inventoryId;
		m_slotIndex = slotIndex;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_eventType);
		writer.Write(m_inventoryId);
		writer.Write(m_slotIndex);
		if (m_eventType == EventType.SlotUpdate)
		{
			writer.Write(m_value);
			writer.Write(m_count);
			writer.Write(m_bagId);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_eventType = reader.ReadEnum<EventType>();
		m_inventoryId = reader.ReadInt32();
		m_slotIndex = reader.ReadInt32();
		if (m_eventType == EventType.SlotUpdate)
		{
			m_value = reader.ReadInt32();
			m_count = reader.ReadInt32();
			m_bagId = reader.ReadInt32();
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemInventories subsystemInventories = projectNet.FindSubsystem<SubsystemInventories>();
		if (subsystemInventories == null)
		{
			return;
		}
		if (m_eventType == EventType.SlotUpdate)
		{
			if (!isServer)
			{
				IInventory inventoryById = subsystemInventories.GetInventoryById(m_inventoryId);
				if (inventoryById != null && inventoryById is ComponentInventoryBase componentInventoryBase && m_slotIndex >= 0 && m_slotIndex < componentInventoryBase.SlotsCount)
				{
					ComponentInventoryBase.Slot slot = new ComponentInventoryBase.Slot();
					slot.Value = m_value;
					slot.Count = m_count;
					componentInventoryBase.SetSlotValue(m_slotIndex, slot);
				}
			}
		}
		else
		{
			if (m_eventType != EventType.RequestOpen || !isServer)
			{
				return;
			}
			SubsystemBags subsystemBags = projectNet.FindSubsystem<SubsystemBags>();
			if (subsystemBags == null)
			{
				return;
			}
			IInventory inventoryById2 = subsystemInventories.GetInventoryById(m_inventoryId);
			if (inventoryById2 == null)
			{
				return;
			}
			ComponentPlayer componentPlayer = ((From != null) ? From.PlayerData : null)?.ComponentPlayer;
			if (componentPlayer == null || ((componentPlayer.ComponentMiner == null || componentPlayer.ComponentMiner.Inventory == null || componentPlayer.ComponentMiner.Inventory.Id != m_inventoryId) && (componentPlayer.ComponentClothing == null || componentPlayer.ComponentClothing.Id != m_inventoryId)))
			{
				ServerSecurity.Warn(From, "bag open from a container the player does not own", "inventory=" + m_inventoryId);
			}
			else
			{
				if (m_slotIndex < 0 || m_slotIndex >= inventoryById2.SlotsCount)
				{
					return;
				}
				int slotValue = inventoryById2.GetSlotValue(m_slotIndex);
				if (inventoryById2.GetSlotCount(m_slotIndex) <= 0 || slotValue == 0)
				{
					return;
				}
				int num = Terrain.ExtractContents(slotValue);
				Block block = BlocksManager.Blocks[num];
				if (!(block is BagBlock) && !(block is LargeBagBlock) && !(block is BackpackBlock) && !(block is ClimbingBackpackBlock))
				{
					return;
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
				BagInventory orCreate = subsystemBags.GetOrCreate(num2, capacity, out var actualId);
				if (orCreate != null)
				{
					int value = slotValue;
					if (actualId != num2)
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
						value = Terrain.MakeBlockValue(num, 0, data2);
						inventoryById2.RemoveSlotItems(m_slotIndex, 1);
						inventoryById2.AddSlotItems(m_slotIndex, value, 1);
					}
					netNode.QueuePackage(new BagPackage
					{
						m_eventType = EventType.SlotUpdate,
						m_inventoryId = inventoryById2.Id,
						m_slotIndex = m_slotIndex,
						m_value = value,
						m_count = inventoryById2.GetSlotCount(m_slotIndex),
						m_bagId = actualId,
						To = From
					});
					Dictionary<IInventory, List<int>> dictionary = new Dictionary<IInventory, List<int>>();
					List<int> list = new List<int>();
					for (int i = 0; i < orCreate.SlotsCount; i++)
					{
						list.Add(i);
					}
					dictionary.Add(orCreate, list);
					netNode.QueuePackage(new ComponentInventoryPackage(dictionary)
					{
						To = From
					});
				}
			}
		}
	}
}
