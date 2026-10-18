using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemBags : Subsystem
{
	private readonly Dictionary<int, BagInventory> m_bags = new Dictionary<int, BagInventory>();

	private int m_nextId = 1;

	private int m_nextInventoryId = 100000;

	private SubsystemItemsScanner m_itemsScanner;

	public BagInventory GetOrCreate(int bagId, int capacity, out int actualId)
	{
		int num = MathUtils.Clamp(capacity, 1, 16);
		int num2 = 4095 - m_bags.Count;
		if (CommonLib.WorkType == WorkType.Client)
		{
			if (bagId <= 0)
			{
				actualId = bagId;
				return null;
			}
			if (m_bags.TryGetValue(bagId, out var value))
			{
				if (value.SlotsCount < num)
				{
					value.SetCapacity(num);
					Log.Information($"[Bags] RESIZE CLIENT ID:{bagId} Cap:{num}");
				}
				actualId = bagId;
				return value;
			}
			int id = 100000 + bagId;
			BagInventory bagInventory = new BagInventory(Project, id, num);
			m_bags[bagId] = bagInventory;
			actualId = bagId;
			return bagInventory;
		}
		if (bagId <= 0 || !m_bags.TryGetValue(bagId, out var value2))
		{
			int num3 = ProduceId();
			int id2 = 100000 + num3;
			BagInventory bagInventory2 = new BagInventory(Project, id2, num);
			m_bags[num3] = bagInventory2;
			actualId = num3;
			Log.Information($"[Bags] CREATE NEW | ReqID:{bagId} -> ActID:{num3} | Type:{GetBagTypeBySlots(num)} | FreeIDs:{num2}->{num2 - 1}");
			return bagInventory2;
		}
		if (value2.SlotsCount < num)
		{
			Log.Information($"[Bags] RESIZE | ID:{bagId} | OldCap:{value2.SlotsCount} -> NewCap:{num} | Type:{GetBagTypeBySlots(num)}");
			value2.SetCapacity(num);
		}
		actualId = bagId;
		return value2;
	}

	private int RegisterInventory(IInventory inv)
	{
		Project.FindSubsystem<SubsystemInventories>(throwOnError: true);
		return m_nextInventoryId++;
	}

	public BagInventory Find(int bagId)
	{
		m_bags.TryGetValue(bagId, out var value);
		return value;
	}

	public override void Load(ValuesDictionary vd)
	{
		base.Load(vd);
		ValuesDictionary value = vd.GetValue<ValuesDictionary>("Bags", null);
		if (value != null)
		{
			foreach (string key in value.Keys)
			{
				if (!key.StartsWith("Bag"))
				{
					continue;
				}
				ValuesDictionary value2 = value.GetValue<ValuesDictionary>(key);
				int value3 = value2.GetValue<int>("Id");
				int num = MathUtils.Clamp(value2.GetValue("SlotsCount", 16), 1, 16);
				int id = 100000 + value3;
				BagInventory bagInventory = new BagInventory(Project, id, num);
				ValuesDictionary value4 = value2.GetValue<ValuesDictionary>("Slots", null);
				if (value4 != null)
				{
					for (int i = 0; i < num; i++)
					{
						ValuesDictionary value5 = value4.GetValue<ValuesDictionary>("S" + i, null);
						if (value5 != null)
						{
							int value6 = value5.GetValue("V", 0);
							int value7 = value5.GetValue("C", 0);
							if (value7 > 0)
							{
								bagInventory.LoadSlotItems(i, value6, value7);
							}
						}
					}
				}
				m_bags[value3] = bagInventory;
				if (value3 >= m_nextId && value3 < 4096)
				{
					m_nextId = value3 + 1;
				}
			}
		}
		m_itemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(throwOnError: true);
		m_itemsScanner.ItemsScanned += GarbageCollectBags;
	}

	public override void Save(ValuesDictionary vd)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			GarbageCollectBags();
		}
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		vd.SetValue("Bags", valuesDictionary);
		foreach (KeyValuePair<int, BagInventory> bag in m_bags)
		{
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Bag" + bag.Key, valuesDictionary2);
			valuesDictionary2.SetValue("Id", bag.Key);
			valuesDictionary2.SetValue("SlotsCount", bag.Value.SlotsCount);
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary2.SetValue("Slots", valuesDictionary3);
			for (int i = 0; i < bag.Value.SlotsCount; i++)
			{
				int slotCount = bag.Value.GetSlotCount(i);
				if (slotCount > 0)
				{
					ValuesDictionary valuesDictionary4 = new ValuesDictionary();
					valuesDictionary3.SetValue("S" + i, valuesDictionary4);
					valuesDictionary4.SetValue("V", bag.Value.GetSlotValue(i));
					valuesDictionary4.SetValue("C", slotCount);
				}
			}
		}
	}

	private int ProduceId()
	{
		for (int stream = 0; stream < 4095; stream++)
		{
			int num = m_nextId++;
			if (m_nextId > 4095)
			{
				m_nextId = 1;
			}
			if (!m_bags.ContainsKey(num))
			{
				return num;
			}
		}
		throw new InvalidOperationException("No free BagId.");
	}

	public void GarbageCollectBags(ReadOnlyList<ScannedItemData> allExistingItems)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		SubsystemPlayers subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: false);
		if (subsystemPlayers == null)
		{
			Log.Warning("[Bags] ABORT GC: SubsystemPlayers not found.");
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (ScannedItemData item in allExistingItems)
		{
			ProcessItemValueForBags(item.Value, hashSet);
		}
		if (subsystemPlayers.m_offlinePlayerEntities != null)
		{
			foreach (EntityData value2 in subsystemPlayers.m_offlinePlayerEntities.Values)
			{
				DeepScanEntityDataForBags(value2.ValuesDictionary, hashSet);
			}
		}
		if (subsystemPlayers.m_keptInventories != null)
		{
			foreach (SubsystemPlayers.KeptInventoryData value3 in subsystemPlayers.m_keptInventories.Values)
			{
				if (value3.SlotValues != null)
				{
					for (int i = 0; i < value3.SlotValues.Length; i++)
					{
						ProcessItemValueForBags(value3.SlotValues[i], hashSet);
					}
				}
			}
		}
		SubsystemStringsBlockBehavior subsystemStringsBlockBehavior = Project.FindSubsystem<SubsystemStringsBlockBehavior>(throwOnError: false);
		if (subsystemStringsBlockBehavior != null)
		{
			foreach (int value4 in subsystemStringsBlockBehavior.m_foodValues.Values)
			{
				ProcessItemValueForBags(value4, hashSet);
			}
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, BagInventory> bag in m_bags)
		{
			if (!hashSet.Contains(bag.Key))
			{
				list.Add(bag.Key);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		int count = m_bags.Count;
		foreach (int item2 in list)
		{
			if (m_bags.TryGetValue(item2, out var value))
			{
				string bagTypeBySlots = GetBagTypeBySlots(value.SlotsCount);
				string bagContentsString = GetBagContentsString(value);
				int num = 4095 - (m_bags.Count - 1);
				Log.Warning($"[Bags] DELETE GC | ID:{item2} | Type:{bagTypeBySlots} | FreeIDs:{num} | Reason:NotFound | Items:[{bagContentsString}]");
			}
			m_bags.Remove(item2);
		}
		Log.Information($"[Bags] GC Finished. Removed {list.Count} bags. Total Bags: {count} -> {m_bags.Count}");
	}

	public void GarbageCollectBags()
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			if (m_itemsScanner == null)
			{
				m_itemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(throwOnError: true);
			}
			ReadOnlyList<ScannedItemData> allExistingItems = m_itemsScanner.ScanItems();
			GarbageCollectBags(allExistingItems);
		}
	}

	private void ProcessItemValueForBags(int value, HashSet<int> foundBagIds)
	{
		if (value == 0)
		{
			return;
		}
		int item = Terrain.ExtractContents(value);
		if (item == 364 || item == 416 || item == 421 || item == 422)
		{
			int data = Terrain.ExtractData(value);
			Block block = BlocksManager.Blocks[item];
			int num = 0;
			if (block is BagBlock)
			{
				num = BagBlock.GetBagId(data);
			}
			else if (block is LargeBagBlock)
			{
				num = LargeBagBlock.GetBagId(data);
			}
			else if (block is BackpackBlock)
			{
				num = BackpackBlock.GetBagId(data);
			}
			else if (block is ClimbingBackpackBlock)
			{
				num = ClimbingBackpackBlock.GetBagId(data);
			}
			if (num > 0)
			{
				foundBagIds.Add(num);
			}
		}
	}

	private void DeepScanEntityDataForBags(ValuesDictionary dictionary, HashSet<int> foundBagIds)
	{
		foreach (object value2 in dictionary.Values)
		{
			if (value2 is int value)
			{
				ProcessItemValueForBags(value, foundBagIds);
			}
			else if (value2 is ValuesDictionary dictionary2)
			{
				DeepScanEntityDataForBags(dictionary2, foundBagIds);
			}
			else
			{
				if (!(value2 is string text) || string.IsNullOrEmpty(text))
				{
					continue;
				}
				string[] array = text.Split(new char[2] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array.Length; i++)
				{
					if (int.TryParse(array[i], out var result))
					{
						ProcessItemValueForBags(result, foundBagIds);
					}
				}
			}
		}
	}

	private string GetBagTypeBySlots(int slots)
	{
		return slots switch
		{
			4 => "Canvas(4)", 
			6 => "Leather(6)", 
			9 => "Backpack(9)", 
			16 => "Climbing(16)", 
			_ => $"Unknown({slots})", 
		};
	}

	private string GetBagContentsString(BagInventory inv)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		for (int i = 0; i < inv.SlotsCount; i++)
		{
			if (inv.GetSlotCount(i) > 0)
			{
				int slotValue = inv.GetSlotValue(i);
				int slotCount = inv.GetSlotCount(i);
				int num = Terrain.ExtractContents(slotValue);
				string arg = "Unk";
				try
				{
					arg = BlocksManager.Blocks[num].GetType().Name.Replace("Block", "");
				}
				catch
				{
				}
				stringBuilder.Append($"{arg}(x{slotCount}), ");
				flag = false;
			}
		}
		if (flag)
		{
			return "EMPTY";
		}
		if (stringBuilder.Length > 2)
		{
			stringBuilder.Length -= 2;
		}
		return stringBuilder.ToString();
	}
}
