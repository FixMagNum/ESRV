using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game.NetWork.Packages;

public class ComponentInventoryPackage : IPackage
{
	public enum EventType
	{
		ActiveSlotChange,
		InventorySync,
		QueryErrorInventoryInfo,
		FurnaceSync,
		SyncGeneration,
		SetSlotsItem,
		SetInventory,
		HandleMoveItem,
		HandleDragDrop
	}

	public class Slot
	{
		public int slotIndex;

		public ushort Generaion;

		public byte type;

		public ComponentInventoryBase.Slot SlotItem;

		public List<int> clothingList;
	}

	public EventType m_eventType;

	public ushort m_generation;

	public int m_inventory_id;

	public int m_activeSlot;

	public InventorySlot m_sourceInventorySlot;

	public InventorySlot m_targetInventorySlot;

	public DragMode m_dragMode;

	public bool m_processingOnly;

	public Dictionary<int, List<Slot>> m_slots;

	public Dictionary<IInventory, List<int>> m_syncItems;

	public byte ID => 11;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ComponentInventoryPackage()
	{
	}

	public ComponentInventoryPackage(IInventory inventory, int activeSlot)
	{
		m_inventory_id = inventory.Id;
		m_activeSlot = activeSlot;
		m_eventType = EventType.ActiveSlotChange;
	}

	public ComponentInventoryPackage(int inventory_id, ushort generation)
	{
		m_inventory_id = inventory_id;
		m_generation = generation;
		m_eventType = EventType.SyncGeneration;
	}

	public ComponentInventoryPackage(int inventory_id, EventType eventType)
	{
		m_inventory_id = inventory_id;
		m_eventType = eventType;
	}

	public ComponentInventoryPackage(InventorySlot sourceInventorySlot, InventorySlot targetInventorySlot, EventType type)
	{
		m_sourceInventorySlot = sourceInventorySlot;
		m_targetInventorySlot = targetInventorySlot;
		m_eventType = type;
	}

	public ComponentInventoryPackage(InventorySlot inventorySlot)
	{
		m_sourceInventorySlot = inventorySlot;
		m_eventType = EventType.SetSlotsItem;
	}

	public ComponentInventoryPackage(Dictionary<IInventory, List<int>> syncItems)
	{
		m_slots = new Dictionary<int, List<Slot>>();
		m_syncItems = new Dictionary<IInventory, List<int>>();
		m_eventType = EventType.InventorySync;
		foreach (KeyValuePair<IInventory, List<int>> fileNameWithoutExtension in syncItems)
		{
			if (!m_slots.TryGetValue(fileNameWithoutExtension.Key.Id, out var value))
			{
				fileNameWithoutExtension.Key.Generation++;
				value = new List<Slot>();
				m_slots.Add(fileNameWithoutExtension.Key.Id, value);
			}
			if (fileNameWithoutExtension.Key is ComponentClothing includeFurniture)
			{
				foreach (int index2 in fileNameWithoutExtension.Value)
				{
					Slot slot = new Slot
					{
						type = 1,
						Generaion = fileNameWithoutExtension.Key.Generation,
						slotIndex = index2,
						clothingList = includeFurniture.GetClothes((ClothingSlot)index2).ToList()
					};
					Slot slot2 = value.Find((Slot x) => x.slotIndex == index2);
					if (slot2 == null)
					{
						value.Add(slot);
						continue;
					}
					int index3 = value.IndexOf(slot2);
					value[index3] = slot;
				}
				continue;
			}
			foreach (int index4 in fileNameWithoutExtension.Value)
			{
				Slot slot3 = new Slot
				{
					type = 0,
					slotIndex = index4,
					Generaion = fileNameWithoutExtension.Key.Generation,
					SlotItem = new ComponentInventoryBase.Slot
					{
						Count = fileNameWithoutExtension.Key.GetSlotCount(index4),
						Value = fileNameWithoutExtension.Key.GetSlotValue(index4)
					}
				};
				Slot slot4 = value.Find((Slot x) => x.slotIndex == index4);
				if (slot4 == null)
				{
					value.Add(slot3);
					continue;
				}
				int index5 = value.IndexOf(slot4);
				value[index5] = slot3;
			}
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_eventType);
		switch (m_eventType)
		{
		case EventType.ActiveSlotChange:
			writer.Write(m_inventory_id);
			writer.Write(m_activeSlot);
			break;
		case EventType.InventorySync:
			writer.Write(m_slots.Count);
			{
				foreach (KeyValuePair<int, List<Slot>> slot in m_slots)
				{
					writer.Write(slot.Key);
					writer.Write(slot.Value.Count);
					foreach (Slot item in slot.Value)
					{
						writer.Write(item.Generaion);
						writer.Write(item.slotIndex);
						writer.Write(item.type);
						if (item.type == 0)
						{
							writer.Write(item.SlotItem.Count);
							writer.Write(item.SlotItem.Value);
							continue;
						}
						writer.Write((byte)item.clothingList.Count);
						foreach (int clothing in item.clothingList)
						{
							writer.Write(clothing);
						}
					}
				}
				break;
			}
		case EventType.QueryErrorInventoryInfo:
			writer.Write(m_inventory_id);
			break;
		case EventType.FurnaceSync:
		case EventType.SetInventory:
			break;
		case EventType.SyncGeneration:
			writer.Write(m_generation);
			break;
		case EventType.SetSlotsItem:
			writer.Write(m_sourceInventorySlot.inventoryId);
			writer.Write(m_sourceInventorySlot.slotIndex);
			writer.Write(m_sourceInventorySlot.value);
			writer.Write(m_sourceInventorySlot.count);
			break;
		case EventType.HandleDragDrop:
			writer.Write(m_sourceInventorySlot.inventoryId);
			writer.Write(m_sourceInventorySlot.slotIndex);
			writer.WriteEnum(m_dragMode);
			writer.Write(m_targetInventorySlot.inventoryId);
			writer.Write(m_targetInventorySlot.slotIndex);
			writer.Write(m_processingOnly);
			break;
		case EventType.HandleMoveItem:
			writer.Write(m_sourceInventorySlot.inventoryId);
			writer.Write(m_sourceInventorySlot.slotIndex);
			writer.Write(m_targetInventorySlot.inventoryId);
			writer.Write(m_targetInventorySlot.slotIndex);
			writer.Write(m_targetInventorySlot.count);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_eventType = reader.ReadEnum<EventType>();
		switch (m_eventType)
		{
		case EventType.ActiveSlotChange:
			m_inventory_id = reader.ReadInt32();
			m_activeSlot = reader.ReadInt32();
			break;
		case EventType.InventorySync:
		{
			m_slots = new Dictionary<int, List<Slot>>();
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = reader.ReadInt32();
				int num2 = reader.ReadInt32();
				List<Slot> list = new List<Slot>();
				for (int j = 0; j < num2; j++)
				{
					Slot slot = new Slot();
					slot.Generaion = reader.ReadUInt16();
					slot.slotIndex = reader.ReadInt32();
					slot.type = reader.ReadByte();
					if (slot.type == 0)
					{
						slot.SlotItem = new ComponentInventoryBase.Slot
						{
							Count = reader.ReadInt32(),
							Value = reader.ReadInt32()
						};
					}
					else
					{
						slot.clothingList = new List<int>();
						int num3 = reader.ReadByte();
						for (int k = 0; k < num3; k++)
						{
							slot.clothingList.Add(reader.ReadInt32());
						}
					}
					list.Add(slot);
				}
				m_slots.Add(key, list);
			}
			break;
		}
		case EventType.QueryErrorInventoryInfo:
			m_inventory_id = reader.ReadInt32();
			break;
		case EventType.SyncGeneration:
			m_generation = reader.ReadUInt16();
			break;
		case EventType.SetSlotsItem:
			m_sourceInventorySlot = new InventorySlot();
			m_sourceInventorySlot.inventoryId = reader.ReadInt32();
			m_sourceInventorySlot.slotIndex = reader.ReadInt32();
			m_sourceInventorySlot.value = reader.ReadInt32();
			m_sourceInventorySlot.count = reader.ReadInt32();
			break;
		case EventType.HandleMoveItem:
			m_sourceInventorySlot = new InventorySlot();
			m_sourceInventorySlot.inventoryId = reader.ReadInt32();
			m_sourceInventorySlot.slotIndex = reader.ReadInt32();
			m_targetInventorySlot = new InventorySlot();
			m_targetInventorySlot.inventoryId = reader.ReadInt32();
			m_targetInventorySlot.slotIndex = reader.ReadInt32();
			m_targetInventorySlot.count = reader.ReadInt32();
			break;
		case EventType.HandleDragDrop:
			m_sourceInventorySlot = new InventorySlot();
			m_sourceInventorySlot.inventoryId = reader.ReadInt32();
			m_sourceInventorySlot.slotIndex = reader.ReadInt32();
			m_dragMode = reader.ReadEnum<DragMode>();
			m_targetInventorySlot = new InventorySlot();
			m_targetInventorySlot.inventoryId = reader.ReadInt32();
			m_targetInventorySlot.slotIndex = reader.ReadInt32();
			m_processingOnly = reader.ReadBoolean();
			break;
		case EventType.FurnaceSync:
		case EventType.SetInventory:
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemInventories point2 = projectNet.FindSubsystem<SubsystemInventories>();
		if (isServer && !IsServerAccessAllowed(point2))
		{
			return;
		}
		switch (m_eventType)
		{
		case EventType.ActiveSlotChange:
			point2.FindInventoryById(m_inventory_id, (IInventory m_inventory) =>
			{
				m_inventory.ActiveSlotIndex = m_activeSlot;
				if (isServer)
				{
					Except = From;
					netNode.QueuePackage(this);
				}
			});
			break;
		case EventType.InventorySync:
			if (isServer)
			{
				break;
			}
			{
				foreach (KeyValuePair<int, List<Slot>> item in m_slots)
				{
					point2.FindInventoryById(item.Key, (IInventory inventory) =>
					{
						foreach (Slot item2 in item.Value)
						{
							inventory.Generation = item2.Generaion;
							if (item2.type == 0)
							{
								inventory.SetSlotValue(item2.slotIndex, item2.SlotItem);
							}
							else
							{
								inventory.SetSlotValue(item2.slotIndex, item2.clothingList);
							}
						}
					});
				}
				break;
			}
		case EventType.QueryErrorInventoryInfo:
			if (!isServer)
			{
				break;
			}
			point2.FindInventoryById(m_inventory_id, (IInventory m_inventory) =>
			{
				string arg = "";
				if (m_inventory is ComponentCraftingTable componentCraftingTable)
				{
					arg = componentCraftingTable.Entity.ValuesDictionary.DatabaseObject.Name;
				}
				Log.Information($"请求错误的箱子ID[{m_inventory_id}]来自[{m_inventory.GetType().Name}][{arg}]");
			});
			break;
		case EventType.FurnaceSync:
		case EventType.SetInventory:
			break;
		case EventType.SetSlotsItem:
			Console.WriteLine("SetSlotsItem");
			if (!isServer)
			{
				IInventory inventoryById3 = point2.GetInventoryById(m_sourceInventorySlot.inventoryId);
				if (inventoryById3 != null && inventoryById3 is ComponentInventoryBase componentInventoryBase)
				{
					ComponentInventoryBase.Slot slot = new ComponentInventoryBase.Slot();
					slot.Value = m_sourceInventorySlot.value;
					slot.Count = m_sourceInventorySlot.count;
					componentInventoryBase.SetSlotValue(m_sourceInventorySlot.slotIndex, slot);
				}
			}
			break;
		case EventType.HandleMoveItem:
		{
			IInventory inventoryById4 = point2.GetInventoryById(m_sourceInventorySlot.inventoryId);
			IInventory inventoryById5 = point2.GetInventoryById(m_targetInventorySlot.inventoryId);
			if (inventoryById4 != null && inventoryById5 != null)
			{
				InventorySlotWidget.HandleMoveItem(inventoryById4, m_sourceInventorySlot.slotIndex, inventoryById5, m_targetInventorySlot.slotIndex, m_targetInventorySlot.count);
			}
			break;
		}
		case EventType.HandleDragDrop:
		{
			IInventory inventoryById = point2.GetInventoryById(m_sourceInventorySlot.inventoryId);
			IInventory inventoryById2 = point2.GetInventoryById(m_targetInventorySlot.inventoryId);
			if (inventoryById != null && inventoryById2 != null)
			{
				InventorySlotWidget.HandleDragDrop(inventoryById, m_sourceInventorySlot.slotIndex, m_dragMode, inventoryById2, m_targetInventorySlot.slotIndex, m_processingOnly);
			}
			break;
		}
		case EventType.SyncGeneration:
			point2.FindInventoryById(m_inventory_id, (IInventory m_inventory) =>
			{
				m_inventory.Generation = m_generation;
			});
			break;
		}
	}

	private bool IsServerAccessAllowed(SubsystemInventories subsystemInventories)
	{
		PlayerData player = ((From != null) ? From.PlayerData : null);
		switch (m_eventType)
		{
		case EventType.ActiveSlotChange:
		case EventType.SyncGeneration:
			if (!ServerSecurity.CanAccessInventory(player, subsystemInventories.GetInventoryById(m_inventory_id)))
			{
				ServerSecurity.Warn(From, "inventory access denied", "inventory=" + m_inventory_id + ", event=" + m_eventType);
				return false;
			}
			return true;
		case EventType.HandleMoveItem:
		case EventType.HandleDragDrop:
			if (!ServerSecurity.CanAccessInventory(player, subsystemInventories.GetInventoryById(m_sourceInventorySlot.inventoryId)) || !ServerSecurity.CanAccessInventory(player, subsystemInventories.GetInventoryById(m_targetInventorySlot.inventoryId)))
			{
				ServerSecurity.Warn(From, "inventory access denied", "source=" + m_sourceInventorySlot.inventoryId + ", target=" + m_targetInventorySlot.inventoryId + ", event=" + m_eventType);
				return false;
			}
			return true;
		default:
			return true;
		}
	}
}
