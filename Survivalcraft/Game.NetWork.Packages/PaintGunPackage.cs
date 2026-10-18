namespace Game.NetWork.Packages;

public class PaintGunPackage : IPackage
{
	public int m_inventoryId;

	public int m_slotIndex;

	public byte ID => 29;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public PaintGunPackage()
	{
	}

	public PaintGunPackage(int inventoryId, int slotIndex)
	{
		m_inventoryId = inventoryId;
		m_slotIndex = slotIndex;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_inventoryId);
		writer.Write(m_slotIndex);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_inventoryId = reader.ReadInt32();
		m_slotIndex = reader.ReadInt32();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (!isServer)
		{
			return;
		}
		SubsystemInventories subsystemInventories = projectNet.FindSubsystem<SubsystemInventories>();
		if (subsystemInventories == null)
		{
			return;
		}
		IInventory inventoryById = subsystemInventories.GetInventoryById(m_inventoryId);
		if (inventoryById == null || m_slotIndex < 0 || m_slotIndex >= inventoryById.SlotsCount)
		{
			return;
		}
		int slotCount = inventoryById.GetSlotCount(m_slotIndex);
		int slotValue = inventoryById.GetSlotValue(m_slotIndex);
		if (slotCount > 0 && slotValue != 0)
		{
			int num = Terrain.ExtractContents(slotValue);
			if (num == 410 || num == 411)
			{
				int data = Terrain.ExtractData(slotValue);
				int value = Terrain.MakeBlockValue((num == 410) ? 411 : 410, 0, data);
				inventoryById.RemoveSlotItems(m_slotIndex, 1);
				inventoryById.AddSlotItems(m_slotIndex, value, 1);
			}
		}
	}
}
