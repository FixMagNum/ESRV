using System.Collections.Generic;
using System.Linq;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFurnitureInventory : Component, IInventory
{
	public SubsystemFurnitureBlockBehavior m_subsystemFurnitureBlockBehavior;

	public List<int> m_slots = new List<int>();

	public const int m_largeNumber = 9999;

	public const int maxDesign = 65535;

	public int PageIndex { get; set; }

	public int Id { get; set; }

	public FurnitureSet FurnitureSet { get; set; }

	Project IInventory.Project => Project;

	public int ActiveSlotIndex
	{
		get
		{
			return -1;
		}
		set
		{
		}
	}

	public int SlotsCount => m_slots.Count;

	public int VisibleSlotsCount
	{
		get
		{
			return SlotsCount;
		}
		set
		{
		}
	}

	public ushort Generation { get; set; }

	public void FillSlots()
	{
		m_subsystemFurnitureBlockBehavior.GarbageCollectDesigns();
		m_slots.Clear();
		for (int i = 0; i < 65535; i++)
		{
			FurnitureDesign design = m_subsystemFurnitureBlockBehavior.GetDesign(i);
			if (design != null)
			{
				int num = (from f in design.ListChain()
					select f.Index).Min();
				if (design.Index == num)
				{
					int data = FurnitureBlock2.SetDesignIndex(0, i, design.ShadowStrengthFactor, design.IsLightEmitter);
					int item = Terrain.MakeBlockValue(366, 0, data);
					m_slots.Add(item);
				}
			}
		}
	}

	public virtual void OnSlotChange(int slotIndex)
	{
		SubsystemInventories.pushSyncItem(this, slotIndex);
	}

	public virtual void SetSlotValue(int slotIndex, object obj)
	{
	}

	public void ClearSlots()
	{
		m_slots.Clear();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		Id = valuesDictionary.GetValue("Id", -1);
		Generation = valuesDictionary.GetValue("Generation", Generation);
		SubsystemInventories subsystemInventories = Project.FindSubsystem<SubsystemInventories>();
		if (Id == -1)
		{
			Id = subsystemInventories.ProduceInventoryId(this);
		}
		else
		{
			Id = subsystemInventories.RegisterInventory(this);
		}
		m_subsystemFurnitureBlockBehavior = Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
		string furnitureSetName = valuesDictionary.GetValue<string>("FurnitureSet");
		FurnitureSet = m_subsystemFurnitureBlockBehavior.FurnitureSets.FirstOrDefault((FurnitureSet f) => f.Name == furnitureSetName);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("FurnitureSet", (FurnitureSet != null) ? FurnitureSet.Name : string.Empty);
		valuesDictionary.SetValue("Id", Id);
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
		if (slotIndex < 0 || slotIndex >= m_slots.Count)
		{
			return 0;
		}
		if (m_slots[slotIndex] == 0)
		{
			return 0;
		}
		return 9999;
	}

	public int GetSlotCapacity(int slotIndex, int value)
	{
		return 99980001;
	}

	public int GetSlotProcessCapacity(int slotIndex, int value)
	{
		int slotCount = GetSlotCount(slotIndex);
		int i = GetSlotValue(slotIndex);
		if (slotCount > 0 && i != 0)
		{
			SubsystemBlockBehavior[] j = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(i));
			for (int k = 0; k < j.Length; k++)
			{
				int processInventoryItemCapacity = j[k].GetProcessInventoryItemCapacity(this, slotIndex, value);
				if (processInventoryItemCapacity > 0)
				{
					return processInventoryItemCapacity;
				}
			}
		}
		return 9999;
	}

	public void AddSlotItems(int slotIndex, int value, int count)
	{
	}

	public virtual void ProcessSlotItems(IInventory sourceInventory, int sourceSlotIndex, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		int slotCount = GetSlotCount(slotIndex);
		int i = GetSlotValue(slotIndex);
		if (slotCount > 0 && i != 0)
		{
			SubsystemBlockBehavior[] j = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(i));
			foreach (SubsystemBlockBehavior subsystemBlockBehavior in j)
			{
				int processInventoryItemCapacity = subsystemBlockBehavior.GetProcessInventoryItemCapacity(this, slotIndex, value);
				if (processInventoryItemCapacity > 0)
				{
					subsystemBlockBehavior.ProcessInventoryItem(this, slotIndex, value, count, MathUtils.Min(processInventoryItemCapacity, processCount), out processedValue, out processedCount);
					return;
				}
			}
		}
		processedValue = 0;
		processedCount = 0;
	}

	public int RemoveSlotItems(int slotIndex, int count)
	{
		return 1;
	}

	public void DropAllItems(Vector3 position)
	{
	}

	public bool AddNetSlotItems(int slotIndex, int value, int count)
	{
		return false;
	}

	public int RemoveNetSlotItems(int slotIndex, int count)
	{
		return 0;
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
