using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCreativeInventory : Component, IInventory
{
	public List<int> m_slots = new List<int>();

	public int m_activeSlotIndex;

	public int m_visibleSlotsCount = 10;

	public const int m_largeNumber = 9999;

	public int Id { get; private set; }

	public int OpenSlotsCount { get; set; }

	public int CategoryIndex { get; set; }

	public int PageIndex { get; set; }

	public int ActiveSlotIndex
	{
		get
		{
			return m_activeSlotIndex;
		}
		set
		{
			m_activeSlotIndex = MathUtils.Clamp(value, 0, VisibleSlotsCount - 1);
		}
	}

	public int SlotsCount => m_slots.Count;

	public ushort Generation { get; set; }

	public int VisibleSlotsCount
	{
		get
		{
			return m_visibleSlotsCount;
		}
		set
		{
			value = MathUtils.Clamp(value, 0, 10);
			if (value == m_visibleSlotsCount)
			{
				return;
			}
			m_visibleSlotsCount = value;
			ActiveSlotIndex = ActiveSlotIndex;
			ComponentFrame componentFrame = Entity.FindComponent<ComponentFrame>();
			if (componentFrame != null)
			{
				Vector3 position = componentFrame.Position + new Vector3(0f, 0.5f, 0f);
				Vector3 velocity = 1f * componentFrame.Rotation.GetForwardVector();
				for (int i = m_visibleSlotsCount; i < 10; i++)
				{
					DropSlotItems(i, position, velocity);
				}
			}
		}
	}

	public void DropAllItems(Vector3 position)
	{
	}

	public virtual void SetSlotValue(int slotIndex, object obj)
	{
		ComponentInventoryBase.Slot slot = (ComponentInventoryBase.Slot)obj;
		m_slots[slotIndex] = slot.Value;
	}

	public virtual void OnSlotChange(int slotIndex)
	{
		SubsystemInventories.pushSyncItem(this, slotIndex);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_activeSlotIndex = valuesDictionary.GetValue<int>("ActiveSlotIndex");
		OpenSlotsCount = valuesDictionary.GetValue<int>("OpenSlotsCount");
		CategoryIndex = valuesDictionary.GetValue<int>("CategoryIndex");
		PageIndex = valuesDictionary.GetValue<int>("PageIndex");
		Id = valuesDictionary.GetValue("Id", -1);
		SubsystemInventories array = Project.FindSubsystem<SubsystemInventories>();
		if (Id == -1)
		{
			Id = array.ProduceInventoryId(this);
		}
		else
		{
			Id = array.RegisterInventory(this);
		}
		for (int i = 0; i < OpenSlotsCount; i++)
		{
			m_slots.Add(0);
		}
		foreach (Block item in BlocksManager.Blocks.OrderBy((Block b) => b.DisplayOrder))
		{
			foreach (int creativeValue in item.GetCreativeValues())
			{
				m_slots.Add(creativeValue);
			}
		}
		ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("Slots", null);
		if (value == null)
		{
			return;
		}
		for (int num = 0; num < OpenSlotsCount; num++)
		{
			ValuesDictionary value2 = value.GetValue<ValuesDictionary>("Slot" + num.ToString(CultureInfo.InvariantCulture), null);
			if (value2 != null)
			{
				m_slots[num] = value2.GetValue<int>("Contents");
			}
		}
		Generation = valuesDictionary.GetValue("Generation", Generation);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("ActiveSlotIndex", m_activeSlotIndex);
		valuesDictionary.SetValue("CategoryIndex", CategoryIndex);
		valuesDictionary.SetValue("PageIndex", PageIndex);
		valuesDictionary.SetValue("Id", Id);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Slots", valuesDictionary2);
		for (int i = 0; i < OpenSlotsCount; i++)
		{
			if (m_slots[i] != 0)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary2.SetValue("Slot" + i.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				valuesDictionary3.SetValue("Contents", m_slots[i]);
			}
		}
		valuesDictionary.SetValue("Generation", Generation);
	}

	public int GetSlotValue(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			return m_slots[slotIndex];
		}
		return 0;
	}

	public int GetSlotCount(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			if (m_slots[slotIndex] == 0)
			{
				return 0;
			}
			return 9999;
		}
		return 0;
	}

	public int GetSlotCapacity(int slotIndex, int value)
	{
		if (slotIndex >= VisibleSlotsCount && slotIndex < 10)
		{
			return 0;
		}
		if (slotIndex >= 0 && slotIndex < OpenSlotsCount)
		{
			return 99980001;
		}
		if (BlocksManager.Blocks[Terrain.ExtractContents(value)].IsNonDuplicable)
		{
			return 9999;
		}
		return 99980001;
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
		if (slotIndex < OpenSlotsCount)
		{
			return 0;
		}
		return 9999;
	}

	public void AddSlotItems(int slotIndex, int value, int count)
	{
		AddNetSlotItems(slotIndex, value, count);
		OnSlotChange(slotIndex);
	}

	public bool AddNetSlotItems(int slotIndex, int value, int count)
	{
		if (slotIndex >= 0 && slotIndex < OpenSlotsCount && m_slots[slotIndex] != value)
		{
			m_slots[slotIndex] = value;
			return true;
		}
		return false;
	}

	public int RemoveSlotItems(int slotIndex, int count)
	{
		count = RemoveNetSlotItems(slotIndex, count);
		OnSlotChange(slotIndex);
		return count;
	}

	public int RemoveNetSlotItems(int slotIndex, int count)
	{
		if (slotIndex >= 0 && slotIndex < OpenSlotsCount)
		{
			int num = Terrain.ExtractContents(m_slots[slotIndex]);
			if (BlocksManager.Blocks[num].IsNonDuplicable)
			{
				m_slots[slotIndex] = 0;
			}
			if (count >= 9999)
			{
				m_slots[slotIndex] = 0;
			}
		}
		return 1;
	}

	public virtual void ProcessSlotItems(IInventory sourceInventory, int sourceSlotIndex, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		int slotCount = GetSlotCount(slotIndex);
		int slotValue = GetSlotValue(slotIndex);
		if (slotCount > 0 && slotValue != 0)
		{
			SubsystemBlockBehavior[] blockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(slotValue));
			foreach (SubsystemBlockBehavior subsystemBlockBehavior in blockBehaviors)
			{
				int processInventoryItemCapacity = subsystemBlockBehavior.GetProcessInventoryItemCapacity(this, slotIndex, value);
				if (processInventoryItemCapacity > 0)
				{
					subsystemBlockBehavior.ProcessInventoryItem(this, slotIndex, value, count, MathUtils.Min(processInventoryItemCapacity, processCount), out processedValue, out processedCount);
					return;
				}
			}
		}
		if (slotIndex >= OpenSlotsCount)
		{
			processedValue = 0;
			processedCount = 0;
		}
		else
		{
			processedValue = value;
			processedCount = count;
		}
	}

	public void DropSlotItems(int slotIndex, Vector3 position, Vector3 velocity)
	{
	}

	public void DropNetSlotItems(int slotIndex, Vector3 position, Vector3 velocity)
	{
	}

	public void DropNetAllItems(Vector3 position)
	{
	}
}
