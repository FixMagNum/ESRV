using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemHumanBonesBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemBlockEntities m_subsystemBlockEntities;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemWeather m_subsystemWeather;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 363 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		ComponentBlockEntity num3 = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
		if (num3 == null)
		{
			return;
		}
		Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
		foreach (IInventory item in num3.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(position);
		}
		Project.RemoveEntity(num3.Entity, disposeEntity: true);
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
		{
			IPackage package = new BlockEditPackage(new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z), BlockEditPackage.EventType.OpenInventoryByPoint);
			CommonLib.Net.QueuePackage(package);
			return true;
		}
		ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (blockEntity != null && componentMiner.ComponentPlayer != null)
		{
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				ValuesDictionary valuesDictionary = blockEntity.Entity.ValuesDictionary;
				int value = valuesDictionary.GetValue("OpenCount", 0);
				value++;
				valuesDictionary.SetValue("OpenCount", value);
				if (value >= 2)
				{
					Vector3 position = new Vector3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z) + new Vector3(0.5f);
					foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
					{
						item.DropAllItems(position);
					}
					Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
					return true;
				}
				ComponentHumanBones componentHumanBones = blockEntity.Entity.FindComponent<ComponentHumanBones>(throwOnError: true);
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new HumanBonesWidget(componentMiner.Inventory, componentHumanBones);
				AudioManager.PlaySound("Audio/Footsteps/Bones/HumanBones", 1f, 0f, 0f);
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
		ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(cellFace.X, cellFace.Y, cellFace.Z);
		if (blockEntity != null)
		{
			ComponentHumanBones inventory = blockEntity.Entity.FindComponent<ComponentHumanBones>(throwOnError: true);
			Pickable pickable = worldItem as Pickable;
			int num = pickable?.Count ?? 1;
			int num2 = ComponentInventoryBase.AcquireItems(inventory, worldItem.Value, num);
			if (num2 < num)
			{
				m_subsystemAudio.PlaySound("Audio/PickableCollected", 1f, 0f, worldItem.Position, 3f, autoDelay: true);
			}
			if (num2 <= 0)
			{
				worldItem.ToRemove = true;
			}
			else if (pickable != null)
			{
				pickable.Count = num2;
			}
		}
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
		ComponentHumanBones componentHumanBones = movingEntityData.Entity.Entity.FindComponent<ComponentHumanBones>();
		if (componentHumanBones == null)
		{
			return false;
		}
		if (componentMiner.ComponentPlayer != null && CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
		{
			IPackage package = new MovingBlockEditPackage(componentHumanBones);
			CommonLib.Net.QueuePackage(package);
			return true;
		}
		if (componentMiner.ComponentPlayer != null && componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
		{
			ValuesDictionary valuesDictionary = movingEntityData.Entity.Entity.ValuesDictionary;
			int value = valuesDictionary.GetValue("OpenCount", 0);
			value++;
			valuesDictionary.SetValue("OpenCount", value);
			if (value >= 2)
			{
				Vector3 position = raycastResult.HitPoint();
				foreach (IInventory item in movingEntityData.Entity.Entity.FindComponents<IInventory>())
				{
					item.DropAllItems(position);
				}
				if (movingEntityData.Entity != null && movingEntityData.Entity.Entity != null)
				{
					Project.RemoveEntity(movingEntityData.Entity.Entity, disposeEntity: true);
				}
				movingBlockSet.AttachedEntities.Remove(movingEntityData);
				return true;
			}
			componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new HumanBonesWidget(componentMiner.Inventory, componentHumanBones);
			AudioManager.PlaySound("Audio/Footsteps/Bones/HumanBones", 1f, 0f, 0f);
		}
		return true;
	}
}
