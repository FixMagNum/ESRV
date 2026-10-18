using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemItemsScanner : Subsystem, IUpdateable
{
	public const float m_automaticScanPeriod = 60f;

	public double m_nextAutomaticScanTime;

	public List<ScannedItemData> m_items = new List<ScannedItemData>();

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<ReadOnlyList<ScannedItemData>> ItemsScanned;

	public ReadOnlyList<ScannedItemData> ScanItems()
	{
		m_items.Clear();
		foreach (Subsystem subsystem in Project.Subsystems)
		{
			if (subsystem is IInventory inventory)
			{
				ScanInventory(inventory, m_items);
			}
		}
		foreach (Entity entity in Project.Entities)
		{
			foreach (Component component in entity.Components)
			{
				if (component is IInventory inventory2)
				{
					ScanInventory(inventory2, m_items);
				}
			}
		}
		foreach (Pickable pickable in Project.FindSubsystem<SubsystemPickables>(throwOnError: true).Pickables)
		{
			if (pickable.Count > 0 && pickable.Value != 0)
			{
				List<ScannedItemData> items = m_items;
				ScannedItemData item = new ScannedItemData
				{
					Container = pickable,
					Value = pickable.Value,
					Count = pickable.Count
				};
				items.Add(item);
			}
		}
		foreach (Projectile projectile in Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true).Projectiles)
		{
			if (projectile.Value != 0)
			{
				List<ScannedItemData> items2 = m_items;
				ScannedItemData item = new ScannedItemData
				{
					Container = projectile,
					Value = projectile.Value,
					Count = 1
				};
				items2.Add(item);
			}
		}
		foreach (IMovingBlockSet movingBlockSet in Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true).MovingBlockSets)
		{
			for (int i = 0; i < movingBlockSet.Blocks.Count; i++)
			{
				List<ScannedItemData> items3 = m_items;
				ScannedItemData item = new ScannedItemData
				{
					Container = movingBlockSet,
					Value = movingBlockSet.Blocks[i].Value,
					Count = 1,
					IndexInContainer = i
				};
				items3.Add(item);
			}
		}
		return new ReadOnlyList<ScannedItemData>(m_items);
	}

	public bool TryModifyItem(ScannedItemData itemData, int newValue)
	{
		if (itemData.Container is IInventory inventory)
		{
			inventory.RemoveSlotItems(itemData.IndexInContainer, itemData.Count);
			int slotCapacity = inventory.GetSlotCapacity(itemData.IndexInContainer, newValue);
			inventory.AddSlotItems(itemData.IndexInContainer, newValue, MathUtils.Min(itemData.Count, slotCapacity));
			return true;
		}
		if (itemData.Container is WorldItem)
		{
			((WorldItem)itemData.Container).Value = newValue;
			return true;
		}
		if (itemData.Container is IMovingBlockSet movingBlockSet)
		{
			movingBlockSet.SetBlock(movingBlockSet.Blocks.ElementAt(itemData.IndexInContainer).Offset, newValue);
			return true;
		}
		return false;
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Client && Time.FrameStartTime >= m_nextAutomaticScanTime)
		{
			m_nextAutomaticScanTime = Time.FrameStartTime + 60.0;
			ItemsScanned?.Invoke(ScanItems());
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_nextAutomaticScanTime = Time.FrameStartTime + 60.0;
	}

	public void ScanInventory(IInventory inventory, List<ScannedItemData> items)
	{
		for (int i = 0; i < inventory.SlotsCount; i++)
		{
			int slotCount = inventory.GetSlotCount(i);
			if (slotCount > 0)
			{
				int slotValue = inventory.GetSlotValue(i);
				if (slotValue != 0)
				{
					items.Add(new ScannedItemData
					{
						Container = inventory,
						IndexInContainer = i,
						Value = slotValue,
						Count = slotCount
					});
				}
			}
		}
	}
}
