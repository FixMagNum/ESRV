using System.Collections.Generic;
using System.Globalization;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public abstract class ComponentInventoryBase : Component, IInventory
{
	public sealed class Slot
	{
		private int EncodeValue;

		private int EncodeCount;

		private int valueKey;

		private int countKey;

		public int Value
		{
			get
			{
				return EncodeValue ^ valueKey;
			}
			set
			{
				valueKey = new Random().Int(1, 65534);
				EncodeValue = value ^ valueKey;
			}
		}

		public int Count
		{
			get
			{
				return EncodeCount ^ countKey;
			}
			set
			{
				countKey = new Random().Int(1, 65534);
				EncodeCount = value ^ countKey;
			}
		}

		public Slot()
		{
			Value = 0;
			Count = 0;
		}
	}

	public List<Slot> m_slots = new List<Slot>();

	public Random m_random = new Random();

	Project IInventory.Project => Project;

	public int Id { get; private set; }

	public virtual int SlotsCount => m_slots.Count;

	public virtual int VisibleSlotsCount
	{
		get
		{
			return SlotsCount;
		}
		set
		{
		}
	}

	public virtual int ActiveSlotIndex
	{
		get
		{
			return -1;
		}
		set
		{
		}
	}

	public ushort Generation { get; set; }

	public static int FindAcquireSlotForItem(IInventory inventory, int value)
	{
		for (int i = 0; i < inventory.SlotsCount; i++)
		{
			if (inventory.GetSlotCount(i) > 0 && inventory.GetSlotValue(i) == value && inventory.GetSlotCount(i) < inventory.GetSlotCapacity(i, value))
			{
				return i;
			}
		}
		for (int j = 0; j < inventory.SlotsCount; j++)
		{
			if (inventory.GetSlotCount(j) == 0 && inventory.GetSlotCapacity(j, value) > 0)
			{
				return j;
			}
		}
		return -1;
	}

	public static int AcquireItems(IInventory inventory, int value, int count)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			while (count > 0)
			{
				int subtexture = FindAcquireSlotForItem(inventory, value);
				if (subtexture < 0)
				{
					break;
				}
				inventory.AddSlotItems(subtexture, value, 1);
				count--;
			}
			return count;
		}
		return 0;
	}

	public ComponentPlayer FindInteractingPlayer()
	{
		ComponentPlayer componentPlayer = Entity.FindComponent<ComponentPlayer>();
		if (componentPlayer == null)
		{
			ComponentBlockEntity componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>();
			if (componentBlockEntity != null)
			{
				Vector3 position = new Vector3(componentBlockEntity.Coordinates);
				componentPlayer = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).FindNearestPlayer(position);
			}
		}
		return componentPlayer;
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		Id = valuesDictionary.GetValue("Id", -1);
		SubsystemInventories subsystemInventories = Project.FindSubsystem<SubsystemInventories>();
		if (Id == -1)
		{
			Id = subsystemInventories.ProduceInventoryId(this);
		}
		else
		{
			Id = subsystemInventories.RegisterInventory(this);
		}
		int value = valuesDictionary.GetValue<int>("SlotsCount");
		for (int i = 0; i < value; i++)
		{
			m_slots.Add(new Slot());
		}
		ValuesDictionary value2 = valuesDictionary.GetValue<ValuesDictionary>("Slots");
		for (int j = 0; j < m_slots.Count; j++)
		{
			ValuesDictionary value3 = value2.GetValue<ValuesDictionary>("Slot" + j.ToString(CultureInfo.InvariantCulture), null);
			if (value3 != null)
			{
				Slot slot = m_slots[j];
				slot.Value = value3.GetValue<int>("Contents");
				slot.Count = value3.GetValue<int>("Count");
			}
		}
		Generation = valuesDictionary.GetValue("Generation", Generation);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Slots", valuesDictionary2);
		valuesDictionary.SetValue("Id", Id);
		for (int i = 0; i < m_slots.Count; i++)
		{
			Slot slot = m_slots[i];
			if (slot.Count > 0)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary2.SetValue("Slot" + i.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
				valuesDictionary3.SetValue("Contents", slot.Value);
				valuesDictionary3.SetValue("Count", slot.Count);
			}
		}
		valuesDictionary.SetValue("Generation", Generation);
	}

	public virtual int GetSlotValue(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			if (m_slots[slotIndex].Count <= 0)
			{
				return 0;
			}
			return m_slots[slotIndex].Value;
		}
		return 0;
	}

	public virtual int GetSlotCount(int slotIndex)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			return m_slots[slotIndex].Count;
		}
		return 0;
	}

	public virtual int GetSlotCapacity(int slotIndex, int value)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			return BlocksManager.Blocks[Terrain.ExtractContents(value)].GetMaxStacking(value);
		}
		return 0;
	}

	public virtual int GetSlotProcessCapacity(int slotIndex, int value)
	{
		int slotCount = GetSlotCount(slotIndex);
		int model = GetSlotValue(slotIndex);
		if (slotCount > 0 && model != 0)
		{
			SubsystemBlockBehavior[] modelMesh = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(model));
			for (int i = 0; i < modelMesh.Length; i++)
			{
				int processInventoryItemCapacity = modelMesh[i].GetProcessInventoryItemCapacity(this, slotIndex, value);
				if (processInventoryItemCapacity > 0)
				{
					return processInventoryItemCapacity;
				}
			}
		}
		return 0;
	}

	public virtual void ProcessSlotItems(IInventory sourceInventory, int sourceSlotIndex, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		int slotCount = GetSlotCount(slotIndex);
		int drink = GetSlotValue(slotIndex);
		if (slotCount > 0 && drink != 0)
		{
			SubsystemBlockBehavior[] blockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(drink));
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
		processedValue = value;
		processedCount = count;
	}

	public virtual void SetSlotValue(int slotIndex, object obj)
	{
		Slot drink = (Slot)obj;
		m_slots[slotIndex].Count = drink.Count;
		m_slots[slotIndex].Value = drink.Value;
		OnSlotChange(slotIndex);
	}

	public virtual void AddSlotItems(int slotIndex, int value, int count)
	{
		AddNetSlotItems(slotIndex, value, count);
		OnSlotChange(slotIndex);
	}

	public virtual int RemoveSlotItems(int slotIndex, int count)
	{
		count = RemoveNetSlotItems(slotIndex, count);
		OnSlotChange(slotIndex);
		return count;
	}

	public virtual void OnSlotChange(int slotIndex)
	{
		if (CommonLib.WorkType == WorkType.Server)
		{
			SubsystemInventories.pushSyncItem(this, slotIndex);
		}
	}

	public virtual bool AddNetSlotItems(int slotIndex, int value, int count)
	{
		if (count > 0 && slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			Slot slot = m_slots[slotIndex];
			if ((GetSlotCount(slotIndex) != 0 && GetSlotValue(slotIndex) != value) || GetSlotCount(slotIndex) + count > GetSlotCapacity(slotIndex, value))
			{
				return false;
			}
			slot.Value = value;
			slot.Count += count;
			return true;
		}
		return false;
	}

	public virtual int RemoveNetSlotItems(int slotIndex, int count)
	{
		if (slotIndex >= 0 && slotIndex < m_slots.Count)
		{
			Slot slot = m_slots[slotIndex];
			count = MathUtils.Min(count, GetSlotCount(slotIndex));
			slot.Count -= count;
			if (slot.Count == 0)
			{
				slot.Value = 0;
			}
			return count;
		}
		return 0;
	}

	public virtual void DropAllItems(Vector3 position)
	{
		for (int drink = 0; drink < SlotsCount; drink++)
		{
			DropSlotItems(drink, position, m_random.Float(-1f, 1f) * Vector3.Normalize(new Vector3(m_random.Float(-1f, 1f), m_random.Float(1f, 2f), m_random.Float(-1f, 1f))));
		}
	}

	public virtual void DropSlotItems(int slotIndex, Vector3 position, Vector3 velocity)
	{
		int drink = GetSlotCount(slotIndex);
		if (drink > 0)
		{
			int slotValue = GetSlotValue(slotIndex);
			int num = RemoveSlotItems(slotIndex, drink);
			if (num > 0)
			{
				Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(slotValue, num, position, velocity, null);
			}
		}
	}
}
