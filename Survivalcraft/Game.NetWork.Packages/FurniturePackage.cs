using System.Collections.Generic;
using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game.NetWork.Packages;

public class FurniturePackage : IPackage
{
	public enum EventType
	{
		RequestAdd,
		Add,
		NewFurnitureSet,
		DeleteFurnitureSet,
		RenameFurnitureSet,
		MoveFurnitureSet,
		AddToFurnitureSet,
		TryAddDesignChain,
		RemoveFurnitureDesigns
	}

	public EventType eventType;

	public string addXml;

	public string fromName;

	public int furnitureIndex;

	public CellFace cellFace;

	public int startValue;

	public Dictionary<Point3, int> list = new Dictionary<Point3, int>();

	public List<int> toRemoveList = new List<int>();

	public byte ID => 24;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public FurniturePackage()
	{
	}

	public FurniturePackage(FurnitureSet furnitureSet)
	{
		eventType = EventType.NewFurnitureSet;
		addXml = furnitureSet.Name;
		fromName = furnitureSet.ImportedFrom;
	}

	public FurniturePackage(List<int> list)
	{
		eventType = EventType.RemoveFurnitureDesigns;
		toRemoveList.AddRange(list);
	}

	public FurniturePackage(string designName)
	{
		eventType = EventType.DeleteFurnitureSet;
		addXml = designName;
	}

	public FurniturePackage(string oldName, string newName)
	{
		eventType = EventType.RenameFurnitureSet;
		addXml = oldName;
		fromName = newName;
	}

	public FurniturePackage(FurnitureSet furnitureSet, int move)
	{
		eventType = EventType.MoveFurnitureSet;
		furnitureIndex = move;
		addXml = furnitureSet.Name;
	}

	public FurniturePackage(FurnitureDesign design, FurnitureSet furnitureSet)
	{
		eventType = EventType.AddToFurnitureSet;
		furnitureIndex = design.Index;
		addXml = furnitureSet.Name;
	}

	public FurniturePackage(FurnitureDesign design, bool garbageCollectIfNeeded)
	{
		eventType = EventType.TryAddDesignChain;
		furnitureIndex = design.Index;
		ValuesDictionary dict = design.Save();
		addXml = CommonLib.SerializeVDict(dict);
		startValue = (garbageCollectIfNeeded ? 1 : 0);
	}

	public FurniturePackage(FurnitureDesign design, Dictionary<Point3, int> m_list, CellFace cellFace_, int value, bool isRequest = false)
	{
		eventType = ((!isRequest) ? EventType.Add : EventType.RequestAdd);
		furnitureIndex = design.Index;
		ValuesDictionary dict = design.Save();
		addXml = CommonLib.SerializeVDict(dict);
		foreach (KeyValuePair<Point3, int> item in m_list)
		{
			list.Add(item.Key, item.Value);
		}
		cellFace = cellFace_;
		startValue = value;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(eventType);
		switch (eventType)
		{
		case EventType.TryAddDesignChain:
			writer.Write(addXml);
			writer.Write(furnitureIndex);
			writer.Write(startValue);
			break;
		case EventType.MoveFurnitureSet:
		case EventType.AddToFurnitureSet:
			writer.Write(addXml);
			writer.Write(furnitureIndex);
			break;
		case EventType.RenameFurnitureSet:
			writer.Write(addXml);
			writer.Write(fromName);
			break;
		case EventType.DeleteFurnitureSet:
			writer.Write(addXml);
			break;
		case EventType.NewFurnitureSet:
			writer.Write(addXml);
			writer.Write(fromName != null);
			if (fromName != null)
			{
				writer.Write(fromName);
			}
			break;
		case EventType.RemoveFurnitureDesigns:
			writer.Write(toRemoveList.Count);
			{
				foreach (int toRemove in toRemoveList)
				{
					writer.Write(toRemove);
				}
				break;
			}
		case EventType.RequestAdd:
		case EventType.Add:
			writer.Write(furnitureIndex);
			writer.Write(addXml);
			writer.Write(list.Count);
			foreach (KeyValuePair<Point3, int> item in list)
			{
				writer.Write(item.Key);
				writer.Write(item.Value);
			}
			writer.Write(cellFace);
			writer.Write(startValue);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		eventType = reader.ReadEnum<EventType>();
		switch (eventType)
		{
		case EventType.TryAddDesignChain:
			addXml = reader.ReadString();
			startValue = reader.ReadInt32();
			furnitureIndex = reader.ReadInt32();
			break;
		case EventType.MoveFurnitureSet:
		case EventType.AddToFurnitureSet:
			addXml = reader.ReadString();
			furnitureIndex = reader.ReadInt32();
			break;
		case EventType.RenameFurnitureSet:
			addXml = reader.ReadString();
			fromName = reader.ReadString();
			break;
		case EventType.DeleteFurnitureSet:
			addXml = reader.ReadString();
			break;
		case EventType.NewFurnitureSet:
			addXml = reader.ReadString();
			if (reader.ReadBoolean())
			{
				fromName = reader.ReadString();
			}
			break;
		case EventType.RemoveFurnitureDesigns:
		{
			int vector3 = reader.ReadInt32();
			for (int j = 0; j < vector3; j++)
			{
				toRemoveList.Add(reader.ReadInt32());
			}
			break;
		}
		case EventType.RequestAdd:
		case EventType.Add:
		{
			furnitureIndex = reader.ReadInt32();
			addXml = reader.ReadString();
			int vector2 = reader.ReadInt32();
			for (int i = 0; i < vector2; i++)
			{
				list.Add(reader.ReadPoint3(), reader.ReadInt32());
			}
			cellFace = reader.ReadCellFace();
			startValue = reader.ReadInt32();
			break;
		}
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		FurnitureSet furnitureSet = null;
		FurnitureDesign furniture = null;
		ValuesDictionary valuesDictionary = null;
		SubsystemPlayers subsystemPlayers = projectNet.FindSubsystem<SubsystemPlayers>();
		FurnitureInventoryPanel furnitureInventoryPanel = projectNet.FindFurnitureInventorPanel(From);
		if (furnitureInventoryPanel == null)
		{
			return;
		}
		SubsystemTerrain subsystemTerrain = projectNet.FindSubsystem<SubsystemTerrain>();
		SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = projectNet.FindSubsystem<SubsystemFurnitureBlockBehavior>();
		switch (eventType)
		{
		case EventType.TryAddDesignChain:
			valuesDictionary = CommonLib.ReadVDict(addXml);
			furniture = new FurnitureDesign(furnitureIndex, subsystemTerrain, valuesDictionary);
			subsystemFurnitureBlockBehavior.TryAddDesignChain(furniture, startValue == 1);
			if (isServer)
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		case EventType.AddToFurnitureSet:
			furnitureSet = subsystemFurnitureBlockBehavior.m_furnitureSets.Find((FurnitureSet f) => f.Name == addXml);
			furniture = subsystemFurnitureBlockBehavior.m_furnitureDesigns.FirstOrDefault((FurnitureDesign f) => f.Index == furnitureIndex);
			if (furniture != null)
			{
				subsystemFurnitureBlockBehavior.AddToFurnitureSet(furniture, furnitureSet);
				furnitureInventoryPanel?.Invalidate();
			}
			if (isServer)
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		case EventType.MoveFurnitureSet:
			furnitureSet = subsystemFurnitureBlockBehavior.m_furnitureSets.Find((FurnitureSet f) => f.Name == addXml);
			if (furnitureSet != null)
			{
				subsystemFurnitureBlockBehavior.MoveFurnitureSet(furnitureSet, furnitureIndex);
				furnitureInventoryPanel?.Invalidate();
			}
			if (isServer)
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		case EventType.RenameFurnitureSet:
			furnitureSet = subsystemFurnitureBlockBehavior.m_furnitureSets.Find((FurnitureSet f) => f.Name == addXml);
			if (furnitureSet != null)
			{
				furnitureSet.Name = addXml;
				furnitureInventoryPanel?.Invalidate();
			}
			if (isServer)
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		case EventType.DeleteFurnitureSet:
			furnitureSet = subsystemFurnitureBlockBehavior.m_furnitureSets.Find((FurnitureSet f) => f.Name == addXml);
			if (furnitureSet != null)
			{
				int num = subsystemFurnitureBlockBehavior.FurnitureSets.IndexOf(furnitureSet);
				subsystemFurnitureBlockBehavior.DeleteFurnitureSet(furnitureSet);
				subsystemFurnitureBlockBehavior.GarbageCollectDesigns();
				if (furnitureInventoryPanel != null)
				{
					if (furnitureInventoryPanel.ComponentFurnitureInventory.FurnitureSet?.Name == addXml)
					{
						furnitureInventoryPanel.ComponentFurnitureInventory.FurnitureSet = ((num > 0) ? subsystemFurnitureBlockBehavior.FurnitureSets[num - 1] : null);
					}
					furnitureInventoryPanel.Invalidate();
				}
			}
			if (isServer)
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		case EventType.NewFurnitureSet:
			furnitureInventoryPanel?.NewFurnitueSetLogic(addXml, fromName);
			if (isServer)
			{
				Except = From;
				netNode.QueuePackage(this);
			}
			break;
		case EventType.Add:
			valuesDictionary = CommonLib.ReadVDict(addXml);
			furniture = new FurnitureDesign(furnitureIndex, subsystemTerrain, valuesDictionary);
			subsystemFurnitureBlockBehavior.CreateDesign(subsystemPlayers.MainPlayer?.ComponentMiner, furniture, list, cellFace, startValue, tryAdd: false);
			break;
		case EventType.RequestAdd:
			valuesDictionary = CommonLib.ReadVDict(addXml);
			furniture = new FurnitureDesign(0, subsystemTerrain, valuesDictionary);
			subsystemPlayers.FindPlayerByClientId(From.ID, (ComponentPlayer player) =>
			{
				furniture = subsystemFurnitureBlockBehavior.CreateDesign(player.ComponentMiner, furniture, list, cellFace, startValue);
				netNode.QueuePackage(new FurniturePackage(furniture, list, cellFace, startValue));
			});
			break;
		case EventType.RemoveFurnitureDesigns:
		{
			for (int i = 0; i < subsystemFurnitureBlockBehavior.m_furnitureDesigns.Length; i++)
			{
				FurnitureDesign furnitureDesign = subsystemFurnitureBlockBehavior.m_furnitureDesigns[i];
				if (furnitureDesign == null)
				{
					continue;
				}
				foreach (int toRemove in toRemoveList)
				{
					if (furnitureDesign.Index == toRemove)
					{
						furnitureDesign.Index = -1;
						subsystemFurnitureBlockBehavior.m_furnitureDesigns[i] = null;
						break;
					}
				}
			}
			break;
		}
		}
	}
}
