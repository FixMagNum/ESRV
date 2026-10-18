using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemCraftingTableBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemBlockEntities m_subsystemBlockEntities;

	public override int[] HandledBlocks => new int[1] { 27 };

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		m_subsystemBlockEntities.CreateBlockEntity("CraftingTable", new Point3(x, y, z), miner);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		ComponentBlockEntity weAction = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(x, y, z);
		if (weAction == null)
		{
			return;
		}
		Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
		foreach (IInventory item in weAction.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(position);
		}
		SubsystemTerrain.Project.RemoveEntity(weAction.Entity, disposeEntity: true);
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
		{
			IPackage package = new BlockEditPackage(new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z), BlockEditPackage.EventType.OpenInventoryByPoint);
			CommonLib.Net.QueuePackage(package);
			return true;
		}
		ComponentBlockEntity weAction = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (weAction != null && componentMiner.ComponentPlayer != null)
		{
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				ComponentCraftingTable componentCraftingTable = weAction.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true);
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(componentMiner.Inventory, componentCraftingTable);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			return true;
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>();
	}

	public override bool OnInteract(MovingBlocksRaycastResult raycastResult, Point3 offset, ComponentMiner componentMiner)
	{
		if (!(raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet { AttachedEntities: not null } movingBlockSet))
		{
			return false;
		}
		MovingEntityData movingEntityData = null;
		foreach (MovingEntityData attachedEntity in movingBlockSet.AttachedEntities)
		{
			if (attachedEntity.BlockOffset == offset)
			{
				movingEntityData = attachedEntity;
				break;
			}
		}
		if (movingEntityData == null)
		{
			return false;
		}
		ComponentCraftingTable componentCraftingTable = movingEntityData.Entity.Entity.FindComponent<ComponentCraftingTable>();
		if (componentCraftingTable == null)
		{
			return false;
		}
		if (componentMiner.ComponentPlayer != null)
		{
			if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
			{
				IPackage package = new MovingBlockEditPackage(componentCraftingTable);
				CommonLib.Net.QueuePackage(package);
				return true;
			}
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new CraftingTableWidget(componentMiner.Inventory, componentCraftingTable);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				return true;
			}
		}
		return true;
	}
}
