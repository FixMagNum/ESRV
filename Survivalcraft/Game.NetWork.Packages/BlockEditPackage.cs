using Engine;

namespace Game.NetWork.Packages;

public class BlockEditPackage : IPackage
{
	public enum EventType
	{
		OpenInventoryByID,
		OpenInventoryByPoint,
		CrossbowPull,
		EditSign
	}

	public int m_inventoryId;

	public int m_slotIndex;

	public EventType m_type;

	public Point3 m_point3;

	public byte ID => 27;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public BlockEditPackage()
	{
	}

	public BlockEditPackage(IInventory inventory)
	{
		m_inventoryId = inventory.Id;
		m_type = EventType.OpenInventoryByID;
	}

	public BlockEditPackage(Point3 point3, EventType type)
	{
		m_point3 = point3;
		m_type = type;
	}

	public BlockEditPackage(IInventory inventory, int slotIndex, EventType type)
	{
		m_inventoryId = inventory.Id;
		m_slotIndex = slotIndex;
		m_type = type;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		switch (m_type)
		{
		case EventType.OpenInventoryByID:
			writer.Write(m_inventoryId);
			break;
		case EventType.OpenInventoryByPoint:
			writer.Write(m_point3);
			break;
		case EventType.CrossbowPull:
			writer.Write(m_inventoryId);
			writer.Write(m_slotIndex);
			break;
		case EventType.EditSign:
			writer.Write(m_point3);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<EventType>();
		switch (m_type)
		{
		case EventType.OpenInventoryByID:
			m_inventoryId = reader.ReadInt32();
			break;
		case EventType.OpenInventoryByPoint:
			m_point3 = reader.ReadPoint3();
			break;
		case EventType.CrossbowPull:
			m_inventoryId = reader.ReadInt32();
			m_slotIndex = reader.ReadInt32();
			break;
		case EventType.EditSign:
			m_point3 = reader.ReadPoint3();
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (From == null)
		{
			Log.Information("出现空玩家打开背包");
			return;
		}
		SubsystemInventories subsystemInventories = projectNet.FindSubsystem<SubsystemInventories>();
		switch (m_type)
		{
		case EventType.OpenInventoryByID:
		{
			if (isServer)
			{
				ServerSecurity.Warn(From, "open inventory by id is not allowed", "inventory=" + m_inventoryId);
				break;
			}
			IInventory inventoryById = subsystemInventories.GetInventoryById(m_inventoryId);
			if (inventoryById != null)
			{
				ComponentPlayer mainPlayer = CommonLib.MainPlayer;
				if (inventoryById is ComponentChest componentChest)
				{
					mainPlayer.ComponentGui.ModalPanelWidget = new ChestWidget(mainPlayer.ComponentMiner.Inventory, componentChest);
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentStrawBasket componentStrawBasket)
				{
					mainPlayer.ComponentGui.ModalPanelWidget = new StrawBasketWidget(mainPlayer.ComponentMiner.Inventory, componentStrawBasket);
					AudioManager.PlaySound("Audio/Impacts/Plant/ImpactPlant1", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentHumanBones componentHumanBones)
				{
					mainPlayer.ComponentGui.ModalPanelWidget = new HumanBonesWidget(mainPlayer.ComponentMiner.Inventory, componentHumanBones);
					AudioManager.PlaySound("Audio/Footsteps/Bones/HumanBones", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentFurnace componentFurnace)
				{
					mainPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget(mainPlayer.ComponentMiner.Inventory, componentFurnace);
					AudioManager.PlaySound("Audio/FurnaceOpen", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentFurnace2 componentFurnace2)
				{
					mainPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget2(mainPlayer.ComponentMiner.Inventory, componentFurnace2);
					AudioManager.PlaySound("Audio/FurnaceOpen", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentDispenser componentDispenser)
				{
					mainPlayer.ComponentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new DispenserWidget(mainPlayer.ComponentMiner.Inventory, componentDispenser);
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentFermentationBarrel barrel)
				{
					mainPlayer.ComponentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new FermentationBarrelWidget(mainPlayer.ComponentMiner.Inventory, barrel);
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
				else if (inventoryById is ComponentCraftingTable componentCraftingTable)
				{
					mainPlayer.ComponentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(mainPlayer.ComponentMiner.Inventory, componentCraftingTable);
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
			}
			break;
		}
		case EventType.OpenInventoryByPoint:
		{
			if (!isServer)
			{
				break;
			}
			ComponentBlockEntity blockEntity = projectNet.FindSubsystem<SubsystemBlockEntities>().GetBlockEntity(m_point3.X, m_point3.Y, m_point3.Z);
			if (blockEntity != null && !ServerSecurity.IsInReach(From.PlayerData, m_point3))
			{
				ServerSecurity.Warn(From, "open container out of reach", "point=" + m_point3.X + "/" + m_point3.Y + "/" + m_point3.Z);
			}
			else if (blockEntity != null)
			{
				IInventory inventory = blockEntity.Entity.FindComponent<IInventory>(throwOnError: false);
				if (inventory != null)
				{
					IPackage package = new BlockEditPackage(inventory);
					package.To = From;
					CommonLib.Net.QueuePackage(package);
				}
			}
			break;
		}
		case EventType.CrossbowPull:
		{
			if (!isServer)
			{
				break;
			}
			IInventory inventoryById2 = subsystemInventories.GetInventoryById(m_inventoryId);
			if (!ServerSecurity.CanAccessInventory(From.PlayerData, inventoryById2) || m_slotIndex < 0 || m_slotIndex >= inventoryById2.SlotsCount)
			{
				ServerSecurity.Warn(From, "crossbow pull on an inaccessible inventory", "inventory=" + m_inventoryId);
				break;
			}
			int slotValue = inventoryById2.GetSlotValue(m_slotIndex);
			if (Terrain.ExtractContents(slotValue) == 200)
			{
				int data = Terrain.ExtractData(slotValue);
				int value = Terrain.MakeBlockValue(200, 0, CrossbowBlock.SetDraw(data, 15));
				inventoryById2.RemoveSlotItems(m_slotIndex, 1);
				inventoryById2.AddSlotItems(m_slotIndex, value, 1);
			}
			break;
		}
		case EventType.EditSign:
			if (isServer)
			{
				To = From;
				CommonLib.Net.QueuePackage(this);
			}
			else
			{
				DialogsManager.ShowDialog(CommonLib.MainPlayer.GuiWidget, new EditSignDialog(projectNet.FindSubsystem<SubsystemSignBlockBehavior>(), m_point3));
			}
			break;
		}
	}
}
