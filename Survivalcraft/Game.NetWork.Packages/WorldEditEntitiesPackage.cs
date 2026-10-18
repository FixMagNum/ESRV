using System;
using System.Collections.Generic;
using Engine;

namespace Game.NetWork.Packages;

public class WorldEditEntitiesPackage : IPackage
{
	public List<Point3> Positions;

	public List<int> Values;

	public List<ComponentWorldEditIntegration.EntitySnapshot> Snapshots;

	public byte ID => 35;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public WorldEditEntitiesPackage()
	{
		Positions = new List<Point3>();
		Values = new List<int>();
		Snapshots = new List<ComponentWorldEditIntegration.EntitySnapshot>();
	}

	public WorldEditEntitiesPackage(List<ComponentWorldEditIntegration.EntitySnapshotOp> ops)
	{
		Positions = new List<Point3>();
		Values = new List<int>();
		Snapshots = new List<ComponentWorldEditIntegration.EntitySnapshot>();
		if (ops == null)
		{
			return;
		}
		for (int i = 0; i < ops.Count; i++)
		{
			ComponentWorldEditIntegration.EntitySnapshotOp entitySnapshotOp = ops[i];
			if (entitySnapshotOp.Snapshot != null)
			{
				Positions.Add(entitySnapshotOp.Position);
				Values.Add(entitySnapshotOp.Value);
				Snapshots.Add(entitySnapshotOp.Snapshot);
			}
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		int num = ((Snapshots != null) ? Snapshots.Count : 0);
		writer.Write(num);
		for (int i = 0; i < num; i++)
		{
			Point3 point = Positions[i];
			int value = Values[i];
			ComponentWorldEditIntegration.EntitySnapshot snapshot = Snapshots[i];
			writer.Write(point.X);
			writer.Write(point.Y);
			writer.Write(point.Z);
			writer.Write(value);
			WriteSnapshot(writer, snapshot);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		int num = reader.ReadInt32();
		Positions = new List<Point3>(num);
		Values = new List<int>(num);
		Snapshots = new List<ComponentWorldEditIntegration.EntitySnapshot>(num);
		for (int i = 0; i < num; i++)
		{
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			int z = reader.ReadInt32();
			int item = reader.ReadInt32();
			Positions.Add(new Point3(x, y, z));
			Values.Add(item);
			Snapshots.Add(ReadSnapshot(reader));
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (!isServer || Positions == null || Snapshots == null)
		{
			return;
		}
		bool flag = projectNet.FindSubsystem<SubsystemTerrain>() != null;
		SubsystemBlockEntities subsystemBlockEntities = projectNet.FindSubsystem<SubsystemBlockEntities>();
		if (!flag || subsystemBlockEntities == null)
		{
			return;
		}
		PlayerData playerData = ((From != null) ? From.PlayerData : null);
		if (From == null || !ServerSecurity.CanUseWorldEdit(projectNet, playerData))
		{
			ServerSecurity.Warn(From, "block entity snapshot while WorldEdit is not allowed", "count=" + Snapshots.Count);
			return;
		}
		bool serverManager = playerData.ServerManager;
		SubsystemTerrain subsystemTerrain = projectNet.FindSubsystem<SubsystemTerrain>();
		WorldSettings worldSettings = projectNet.FindSubsystem<SubsystemGameInfo>()?.WorldSettings;
		int num = 0;
		if (Snapshots.Count > 2048)
		{
			ServerSecurity.Warn(From, "block entity snapshot package is too large", "count=" + Snapshots.Count);
			return;
		}
		Dictionary<IInventory, List<int>> dictionary = new Dictionary<IInventory, List<int>>();
		for (int i = 0; i < Snapshots.Count; i++)
		{
			ComponentWorldEditIntegration.EntitySnapshot entitySnapshot = Snapshots[i];
			if (entitySnapshot == null)
			{
				continue;
			}
			Point3 point = Positions[i];
			int num2 = Terrain.ExtractContents(Values[i]);
			int num3;
			int num4;
			if (!serverManager)
			{
				if (point.Y >= 0 && point.Y < 256 && num2 >= 0 && num2 < BlocksManager.Blocks.Length && BlocksManager.Blocks[num2] != null)
				{
					num3 = ((subsystemTerrain.Terrain.GetChunkAtCell(point.X, point.Z) != null) ? 1 : 0);
					if (num3 != 0)
					{
						num4 = subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
					}
					else
					{
						num4 = 0;
					}
				}
				else
				{
					num3 = 0;
					num4 = 0;
				}
				int oldValue = num4;
				if (num3 == 0 || !ServerSecurity.IsInReach(playerData, point) || !ServerSecurity.CanEditCell(playerData, worldSettings, point.X, point.Z, oldValue, Values[i]))
				{
					num++;
					continue;
				}
				ComponentBlockEntity blockEntity2 = subsystemBlockEntities.GetBlockEntity(point.X, point.Y, point.Z);
				entitySnapshot.Owner = ((blockEntity2 != null && blockEntity2.Owner != Guid.Empty) ? blockEntity2.Owner : playerData.PlayerGUID);
			}
			if (num2 == 0)
			{
				continue;
			}
			ComponentBlockEntity blockEntity = subsystemBlockEntities.GetBlockEntity(point.X, point.Y, point.Z);
			if (blockEntity == null)
			{
				string entityTemplateNameForBlock = ComponentWorldEditIntegration.GetEntityTemplateNameForBlock(num2);
				if (!string.IsNullOrEmpty(entityTemplateNameForBlock))
				{
					subsystemBlockEntities.CreateBlockEntity(entityTemplateNameForBlock, point, null);
					blockEntity = subsystemBlockEntities.GetBlockEntity(point.X, point.Y, point.Z);
				}
			}
			if (blockEntity != null)
			{
				ApplySnapshotToBlockEntity(blockEntity, entitySnapshot, dictionary);
			}
			continue;
		}
		if (num > 0)
		{
			ServerSecurity.Warn(From, "block entity snapshot rejected", "of " + Snapshots.Count + " entries: " + num + " not allowed");
		}
		if (dictionary.Count > 0)
		{
			ComponentInventoryPackage package = new ComponentInventoryPackage(dictionary);
			netNode.QueuePackage(package);
		}
	}

	private static void ApplySnapshotToBlockEntity(ComponentBlockEntity blockEntity, ComponentWorldEditIntegration.EntitySnapshot snap, Dictionary<IInventory, List<int>> syncItems)
	{
		if (blockEntity == null || snap == null)
		{
			return;
		}
		blockEntity.Owner = snap.Owner;
		foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
		{
			ComponentWorldEditIntegration.InventorySnapshot inventorySnapshot = null;
			for (int i = 0; i < snap.Inventories.Count; i++)
			{
				if (snap.Inventories[i].TypeName == item.GetType().FullName)
				{
					inventorySnapshot = snap.Inventories[i];
					break;
				}
			}
			if (inventorySnapshot == null && snap.Inventories.Count > 0)
			{
				inventorySnapshot = snap.Inventories[0];
			}
			if (inventorySnapshot == null)
			{
				continue;
			}
			int slotsCount = item.SlotsCount;
			for (int j = 0; j < slotsCount; j++)
			{
				int slotCount = item.GetSlotCount(j);
				if (slotCount > 0)
				{
					item.RemoveSlotItems(j, slotCount);
				}
				int num = ((inventorySnapshot.Values != null && j < inventorySnapshot.Values.Length) ? inventorySnapshot.Values[j] : 0);
				int num2 = ((inventorySnapshot.Counts != null && j < inventorySnapshot.Counts.Length) ? inventorySnapshot.Counts[j] : 0);
				if (num2 > 0 && num != 0)
				{
					item.AddSlotItems(j, num, num2);
				}
				if (!syncItems.TryGetValue(item, out var value))
				{
					value = new List<int>();
					syncItems.Add(item, value);
				}
				if (!value.Contains(j))
				{
					value.Add(j);
				}
			}
		}
	}

	private static void WriteSnapshot(PackageStreamWriter writer, ComponentWorldEditIntegration.EntitySnapshot snapshot)
	{
		if (snapshot == null)
		{
			writer.Write(value: false);
			return;
		}
		writer.Write(value: true);
		writer.Write(snapshot.Owner.ToString());
		int num = ((snapshot.Inventories != null) ? snapshot.Inventories.Count : 0);
		writer.Write(num);
		for (int i = 0; i < num; i++)
		{
			ComponentWorldEditIntegration.InventorySnapshot inventorySnapshot = snapshot.Inventories[i];
			writer.Write(inventorySnapshot.TypeName ?? string.Empty);
			int num2 = ((inventorySnapshot.Values != null) ? inventorySnapshot.Values.Length : 0);
			writer.Write(num2);
			for (int j = 0; j < num2; j++)
			{
				int value = inventorySnapshot.Values[j];
				int value2 = ((inventorySnapshot.Counts != null && j < inventorySnapshot.Counts.Length) ? inventorySnapshot.Counts[j] : 0);
				writer.Write(value);
				writer.Write(value2);
			}
		}
	}

	private static ComponentWorldEditIntegration.EntitySnapshot ReadSnapshot(PackageStreamReader reader)
	{
		if (!reader.ReadBoolean())
		{
			return null;
		}
		Guid.TryParse(reader.ReadString(), out var result);
		ComponentWorldEditIntegration.EntitySnapshot entitySnapshot = new ComponentWorldEditIntegration.EntitySnapshot();
		entitySnapshot.Owner = result;
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			ComponentWorldEditIntegration.InventorySnapshot inventorySnapshot = new ComponentWorldEditIntegration.InventorySnapshot();
			inventorySnapshot.TypeName = reader.ReadString();
			int num2 = reader.ReadInt32();
			if (num2 > 0)
			{
				inventorySnapshot.Values = new int[num2];
				inventorySnapshot.Counts = new int[num2];
				for (int j = 0; j < num2; j++)
				{
					inventorySnapshot.Values[j] = reader.ReadInt32();
					inventorySnapshot.Counts[j] = reader.ReadInt32();
				}
			}
			entitySnapshot.Inventories.Add(inventorySnapshot);
		}
		return entitySnapshot;
	}
}
