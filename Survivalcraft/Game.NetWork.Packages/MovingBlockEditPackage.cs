using Engine;

namespace Game.NetWork.Packages;

public class MovingBlockEditPackage : IPackage
{
	public enum EventType
	{
		OpenInventoryByID
	}

	public int m_inventoryId;

	public Point3 m_offset;

	public EventType m_type;

	public byte ID => 37;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public MovingBlockEditPackage()
	{
	}

	public MovingBlockEditPackage(IInventory inventory)
	{
		m_inventoryId = inventory.Id;
		m_type = EventType.OpenInventoryByID;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		if (m_type == EventType.OpenInventoryByID)
		{
			writer.Write(m_inventoryId);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<EventType>();
		if (m_type == EventType.OpenInventoryByID)
		{
			m_inventoryId = reader.ReadInt32();
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (From == null)
		{
			return;
		}
		SubsystemInventories subsystemInventories = projectNet.FindSubsystem<SubsystemInventories>();
		if (m_type != EventType.OpenInventoryByID)
		{
			return;
		}
		if (isServer)
		{
			IInventory inventoryById = subsystemInventories.GetInventoryById(m_inventoryId);
			if (inventoryById != null)
			{
				IPackage package = new MovingBlockEditPackage(inventoryById);
				package.To = From;
				CommonLib.Net.QueuePackage(package);
			}
			return;
		}
		IInventory inventoryById2 = subsystemInventories.GetInventoryById(m_inventoryId);
		if (inventoryById2 != null)
		{
			ComponentPlayer mainPlayer = CommonLib.MainPlayer;
			if (inventoryById2 is ComponentChest componentChest)
			{
				mainPlayer.ComponentGui.ModalPanelWidget = new ChestWidget(mainPlayer.ComponentMiner.Inventory, componentChest);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentCraftingTable componentCraftingTable)
			{
				mainPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(mainPlayer.ComponentMiner.Inventory, componentCraftingTable);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentStrawBasket componentStrawBasket)
			{
				mainPlayer.ComponentGui.ModalPanelWidget = new StrawBasketWidget(mainPlayer.ComponentMiner.Inventory, componentStrawBasket);
				AudioManager.PlaySound("Audio/Impacts/Plant/ImpactPlant1", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentHumanBones componentHumanBones)
			{
				mainPlayer.ComponentGui.ModalPanelWidget = new HumanBonesWidget(mainPlayer.ComponentMiner.Inventory, componentHumanBones);
				AudioManager.PlaySound("Audio/Footsteps/Bones/HumanBones", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentFurnace componentFurnace)
			{
				mainPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget(mainPlayer.ComponentMiner.Inventory, componentFurnace);
				AudioManager.PlaySound("Audio/FurnaceOpen", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentFurnace2 componentFurnace2)
			{
				mainPlayer.ComponentGui.ModalPanelWidget = new FurnaceWidget2(mainPlayer.ComponentMiner.Inventory, componentFurnace2);
				AudioManager.PlaySound("Audio/FurnaceOpen", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentDispenser componentDispenser)
			{
				mainPlayer.ComponentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new DispenserWidget(mainPlayer.ComponentMiner.Inventory, componentDispenser);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			else if (inventoryById2 is ComponentFermentationBarrel barrel)
			{
				mainPlayer.ComponentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new FermentationBarrelWidget(mainPlayer.ComponentMiner.Inventory, barrel);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
		}
	}
}
