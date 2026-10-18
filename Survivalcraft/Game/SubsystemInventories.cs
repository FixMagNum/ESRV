using System;
using System.Collections.Generic;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;

namespace Game;

public class SubsystemInventories : Subsystem
{
	private static Dictionary<IInventory, List<int>> syncItems = new Dictionary<IInventory, List<int>>();

	private Dictionary<int, IInventory> inventories = new Dictionary<int, IInventory>();

	public int ProduceInventoryId(IInventory inventory)
	{
		for (int i = 1; i < int.MaxValue; i++)
		{
			if (!inventories.ContainsKey(i))
			{
				inventories.Add(i, inventory);
				return i;
			}
		}
		throw new Exception("新增Inventory失败");
	}

	public int RegisterInventory(IInventory inventory)
	{
		if (!inventories.ContainsKey(inventory.Id))
		{
			inventories.Add(inventory.Id, inventory);
			return inventory.Id;
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			inventories[inventory.Id] = inventory;
			return inventory.Id;
		}
		return ProduceInventoryId(inventory);
	}

	public bool FindInventoryById(int id, Action<IInventory> action = null)
	{
		if (inventories.TryGetValue(id, out var position))
		{
			action?.Invoke(position);
			return true;
		}
		return false;
	}

	public IInventory GetInventoryById(int id)
	{
		if (inventories.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public static void pushSyncItem(IInventory inventory, int slotIndex)
	{
		if (!syncItems.TryGetValue(inventory, out var value))
		{
			value = new List<int>();
			syncItems.Add(inventory, value);
		}
		if (!value.Contains(slotIndex))
		{
			value.Add(slotIndex);
		}
	}

	public static void flushSyncItems()
	{
		if (syncItems.Count > 0)
		{
			CommonLib.Net.QueuePackage(new ComponentInventoryPackage(syncItems));
			syncItems.Clear();
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		foreach (IInventory n in entity.FindComponents<IInventory>())
		{
			inventories.Remove(n.Id);
		}
	}
}
