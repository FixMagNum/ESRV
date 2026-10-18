using System.Collections.Generic;
using Engine;
using Game.NetWork;
using GameEntitySystem;

namespace Game;

public class BagInventory : IInventory
{
	private struct Slot
	{
		public int Value;

		public int Count;
	}

	private List<Slot> m_slots = new List<Slot>();

	public Project Project { get; }

	public int SlotsCount => m_slots.Count;

	public int Id { get; private set; }

	public int VisibleSlotsCount { get; set; }

	public int ActiveSlotIndex { get; set; }

	public ushort Generation { get; set; }

	public BagInventory(Project project, int id, int capacity)
	{
		Project = project;
		Id = id;
		VisibleSlotsCount = capacity;
		SetCapacity(capacity);
		SubsystemInventories subsystemInventories = project.FindSubsystem<SubsystemInventories>(throwOnError: true);
		Id = subsystemInventories.RegisterInventory(this);
	}

	public void SetCapacity(int cap)
	{
		int num = MathUtils.Clamp(cap, 1, 16);
		while (m_slots.Count < num)
		{
			m_slots.Add(default);
		}
		while (m_slots.Count > num)
		{
			m_slots.RemoveAt(m_slots.Count - 1);
		}
	}

	public void SetSlotValue(int slotIndex, object obj)
	{
		if (obj is ComponentInventoryBase.Slot slot && slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			Slot value = m_slots[slotIndex];
			value.Value = slot.Value;
			value.Count = slot.Count;
			m_slots[slotIndex] = value;
			OnSlotChange(slotIndex);
		}
	}

	public int GetSlotValue(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= m_slots.Count)
		{
			return 0;
		}
		if (m_slots[slotIndex].Count <= 0)
		{
			return 0;
		}
		return m_slots[slotIndex].Value;
	}

	public int GetSlotCount(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= m_slots.Count)
		{
			return 0;
		}
		return m_slots[slotIndex].Count;
	}

	public void OnSlotChange(int slotIndex)
	{
		if (CommonLib.WorkType == WorkType.Server)
		{
			SubsystemInventories.pushSyncItem(this, slotIndex);
		}
	}

	public int GetSlotCapacity(int slotIndex, int value)
	{
		if (slotIndex < 0 || slotIndex >= m_slots.Count)
		{
			return 0;
		}
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (block is BagBlock || block is LargeBagBlock || block is BackpackBlock || block is ClimbingBackpackBlock)
		{
			return 0;
		}
		return block.GetMaxStacking(value);
	}

	public int GetSlotProcessCapacity(int slotIndex, int value)
	{
		int slotCount = GetSlotCount(slotIndex);
		int slotValue = GetSlotValue(slotIndex);
		if (slotCount > 0 && slotValue != 0)
		{
			SubsystemBlockBehavior[] blockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(slotValue));
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				int processInventoryItemCapacity = blockBehaviors[i].GetProcessInventoryItemCapacity(this, slotIndex, value);
				if (processInventoryItemCapacity > 0)
				{
					return processInventoryItemCapacity;
				}
			}
		}
		return 0;
	}

	public void AddSlotItems(int slotIndex, int value, int count)
	{
		AddNetSlotItems(slotIndex, value, count);
		OnSlotChange(slotIndex);
	}

	public bool AddNetSlotItems(int slotIndex, int value, int count)
	{
		if (count <= 0 || slotIndex < 0 || slotIndex >= m_slots.Count)
		{
			return false;
		}
		Slot value2 = m_slots[slotIndex];
		if ((value2.Count != 0 && value2.Value != value) || value2.Count + count > GetSlotCapacity(slotIndex, value))
		{
			return false;
		}
		value2.Value = value;
		value2.Count += count;
		m_slots[slotIndex] = value2;
		return true;
	}

	public int RemoveSlotItems(int slotIndex, int count)
	{
		count = RemoveNetSlotItems(slotIndex, count);
		OnSlotChange(slotIndex);
		return count;
	}

	public int RemoveNetSlotItems(int slotIndex, int count)
	{
		if (slotIndex < 0 || slotIndex >= m_slots.Count)
		{
			return 0;
		}
		Slot value = m_slots[slotIndex];
		count = MathUtils.Min(count, value.Count);
		value.Count -= count;
		if (value.Count == 0)
		{
			value.Value = 0;
		}
		m_slots[slotIndex] = value;
		return count;
	}

	public void ProcessSlotItems(IInventory sourceInventory, int sourceSlotIndex, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		int slotCount = GetSlotCount(slotIndex);
		int slotValue = GetSlotValue(slotIndex);
		if (slotCount > 0 && slotValue != 0)
		{
			SubsystemBlockBehavior[] blockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(slotValue));
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				int processInventoryItemCapacity = blockBehaviors[i].GetProcessInventoryItemCapacity(this, slotIndex, value);
				if (processInventoryItemCapacity > 0)
				{
					blockBehaviors[i].ProcessInventoryItem(this, slotIndex, value, count, MathUtils.Min(processInventoryItemCapacity, processCount), out processedValue, out processedCount);
					return;
				}
			}
		}
		processedValue = value;
		processedCount = count;
	}

	public void DropAllItems(Vector3 position)
	{
		for (int i = 0; i < SlotsCount; i++)
		{
			DropSlotItems(i, position, new Vector3(0f));
		}
	}

	public void DropSlotItems(int slotIndex, Vector3 position, Vector3 velocity)
	{
		int slotCount = GetSlotCount(slotIndex);
		if (slotCount > 0)
		{
			int slotValue = GetSlotValue(slotIndex);
			int num = RemoveSlotItems(slotIndex, slotCount);
			if (num > 0)
			{
				Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(slotValue, num, position, velocity, null);
			}
		}
	}

	public void LoadSlotItems(int slotIndex, int value, int count)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			Slot value2 = m_slots[slotIndex];
			value2.Value = value;
			value2.Count = count;
			m_slots[slotIndex] = value2;
		}
	}
}
