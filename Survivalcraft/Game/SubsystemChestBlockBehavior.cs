using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemChestBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemBlockEntities m_subsystemBlockEntities;

	public SubsystemAudio m_subsystemAudio;

	public override int[] HandledBlocks => new int[1] { 45 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		m_subsystemBlockEntities.CreateBlockEntity("Chest", new Point3(x, y, z), miner);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		ComponentBlockEntity weMaxBlocksAllowed = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
		if (weMaxBlocksAllowed == null)
		{
			return;
		}
		Vector3 point = new Vector3(x, y, z) + new Vector3(0.5f);
		foreach (IInventory item in weMaxBlocksAllowed.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(point);
		}
		Project.RemoveEntity(weMaxBlocksAllowed.Entity, disposeEntity: true);
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
		{
			IPackage max = new BlockEditPackage(new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z), BlockEditPackage.EventType.OpenInventoryByPoint);
			CommonLib.Net.QueuePackage(max);
			return true;
		}
		ComponentBlockEntity min = m_subsystemBlockEntities.GetBlockEntity(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (min != null && componentMiner.ComponentPlayer != null)
		{
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				ComponentChest num = min.Entity.FindComponent<ComponentChest>(throwOnError: true);
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new ChestWidget(componentMiner.Inventory, num);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			return true;
		}
		return false;
	}

	public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
	{
		if (worldItem.ToRemove)
		{
			return;
		}
		ComponentBlockEntity min = m_subsystemBlockEntities.GetBlockEntity(cellFace.X, cellFace.Y, cellFace.Z);
		if (min != null)
		{
			ComponentChest inventory = min.Entity.FindComponent<ComponentChest>(throwOnError: true);
			Pickable max = worldItem as Pickable;
			int i = max?.Count ?? 1;
			int j = ComponentInventoryBase.AcquireItems(inventory, worldItem.Value, i);
			if (j < i)
			{
				m_subsystemAudio.PlaySound("Audio/PickableCollected", 1f, 0f, worldItem.Position, 3f, autoDelay: true);
			}
			if (j <= 0)
			{
				worldItem.ToRemove = true;
			}
			else if (max != null)
			{
				max.Count = j;
			}
		}
	}

	public override bool OnInteract(MovingBlocksRaycastResult raycastResult, Point3 offset, ComponentMiner componentMiner)
	{
		if (!(raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet { AttachedEntities: not null } min))
		{
			return false;
		}
		MovingEntityData max = null;
		foreach (MovingEntityData num3 in min.AttachedEntities)
		{
			if (num3.BlockOffset == offset)
			{
				max = num3;
				break;
			}
		}
		if (max == null)
		{
			return false;
		}
		ComponentChest num4 = max.Entity.Entity.FindComponent<ComponentChest>();
		if (num4 == null)
		{
			return false;
		}
		if (componentMiner.ComponentPlayer != null)
		{
			if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
			{
				IPackage num5 = new MovingBlockEditPackage(num4);
				CommonLib.Net.QueuePackage(num5);
				return true;
			}
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new ChestWidget(componentMiner.Inventory, num4);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				return true;
			}
		}
		return true;
	}
}
