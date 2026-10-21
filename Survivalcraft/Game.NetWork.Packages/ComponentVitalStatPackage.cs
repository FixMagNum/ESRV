using Engine;
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

	private bool IsServerOwnStatAllowed()
	{
		PlayerData playerData = (From != null) ? From.PlayerData : null;
		ComponentPlayer componentPlayer = (playerData != null) ? playerData.ComponentPlayer : null;
		if (componentPlayer == null || componentPlayer.Entity == null || componentPlayer.Entity.EntityId != m_entityId)
		{
			string own = (componentPlayer == null) ? "player=null" : ((componentPlayer.Entity == null) ? "entity=null" : "own=" + componentPlayer.Entity.EntityId.ToString());
			ServerSecurity.Warn(From, "vital stats of another entity", "target=" + m_entityId + ", " + own + ", type=" + m_eventType.ToString());
			return false;
		}
		return true;
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		// Eating is applied only through the inventory drag-drop path, which also removes the item on the server.
		// A client's Eat packet would grant food without consuming anything, so the server ignores it.
		if (isServer && m_eventType == EventType.Eat)
		{
			return;
		}
		// On the server a client may only change its own vital stats.
		if (isServer && !IsServerOwnStatAllowed())
		{
			return;
		}
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
						// Food is server-authoritative: the client may only lower it; raising comes from eating on the server.
						if (Food < componentVitalStats.Food && Time.RealTime - componentVitalStats.m_lastFoodGainTime > 2.0)
						{
							componentVitalStats.Food = Food;
						}
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
		}
	}
}
