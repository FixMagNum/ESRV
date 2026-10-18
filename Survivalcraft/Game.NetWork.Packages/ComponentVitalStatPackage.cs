using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentVitalStatPackage : IPackage
{
	public enum EventType
	{
		SyncStat,
		RequestEat,
		Eat
	}

	public int m_inventoryId;

	public int m_slotIndex;

	public int m_foodValue;

	public EventType m_eventType;

	public ushort m_entityId;

	public float Food;

	public float Stamina;

	public float Sleep;

	public float Wetness;

	public float Temperature;

	public byte ID => 4;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public ComponentVitalStatPackage()
	{
	}

	public ComponentVitalStatPackage(ComponentVitalStats vitalStats)
	{
		m_eventType = EventType.SyncStat;
		m_entityId = vitalStats.Entity.EntityId;
		Food = vitalStats.Food;
		Sleep = vitalStats.Sleep;
		Stamina = vitalStats.Stamina;
		Wetness = vitalStats.Wetness;
		Temperature = vitalStats.Temperature;
	}

	public ComponentVitalStatPackage(ComponentVitalStats vitalStats, IInventory inventory, int slotIndex, int value)
	{
		m_eventType = EventType.Eat;
		m_foodValue = value;
		m_inventoryId = inventory.Id;
		m_slotIndex = slotIndex;
		m_entityId = vitalStats.Entity.EntityId;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_entityId);
		writer.WriteEnum(m_eventType);
		switch (m_eventType)
		{
		case EventType.SyncStat:
			writer.Write(Food);
			writer.Write(Sleep);
			writer.Write(Wetness);
			writer.Write(Temperature);
			writer.Write(Stamina);
			break;
		case EventType.Eat:
			writer.Write(m_inventoryId);
			writer.Write(m_slotIndex);
			writer.Write(m_foodValue);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_entityId = reader.ReadUInt16();
		m_eventType = reader.ReadEnum<EventType>();
		switch (m_eventType)
		{
		case EventType.SyncStat:
			Food = reader.ReadSingle();
			Sleep = reader.ReadSingle();
			Wetness = reader.ReadSingle();
			Temperature = reader.ReadSingle();
			Stamina = reader.ReadSingle();
			break;
		case EventType.Eat:
			m_inventoryId = reader.ReadInt32();
			m_slotIndex = reader.ReadInt32();
			m_foodValue = reader.ReadInt32();
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		switch (m_eventType)
		{
		case EventType.SyncStat:
			projectNet.FindEntityById(m_entityId, (Entity entity) =>
			{
				ComponentVitalStats componentVitalStats = entity.FindComponent<ComponentVitalStats>();
				if (componentVitalStats != null)
				{
					if (isServer)
					{
						componentVitalStats.Stamina = Stamina;
					}
					else
					{
						componentVitalStats.Food = Food;
						componentVitalStats.Sleep = Sleep;
						componentVitalStats.Wetness = Wetness;
						componentVitalStats.Temperature = Temperature;
					}
				}
			});
			break;
		case EventType.Eat:
			projectNet.FindEntityById(m_entityId, (Entity entity) =>
			{
				ComponentVitalStats m_vitalStats = entity.FindComponent<ComponentVitalStats>();
				if (m_vitalStats != null)
				{
					projectNet.FindSubsystem<SubsystemInventories>().FindInventoryById(m_inventoryId, (IInventory inventory) =>
					{
						if (inventory.GetSlotValue(m_slotIndex) == m_foodValue)
						{
							m_vitalStats.NetEat(inventory, m_slotIndex, m_foodValue);
						}
					});
				}
			});
			break;
		}
	}
}
