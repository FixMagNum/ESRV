using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemStrawBasketBlockBehavior : SubsystemPollableBlockBehavior
{
	public SubsystemBlockEntities m_subsystemBlockEntities;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemWeather m_subsystemWeather;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 308 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		m_subsystemBlockEntities.CreateBlockEntity("StrawBasket", new Point3(x, y, z), miner);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
		if (blockEntity == null)
		{
			return;
		}
		Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
		foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(position);
		}
		Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
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
				ComponentStrawBasket componentStrawBasket = blockEntity.Entity.FindComponent<ComponentStrawBasket>(throwOnError: true);
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new StrawBasketWidget(componentMiner.Inventory, componentStrawBasket);
				AudioManager.PlaySound("Audio/Impacts/Plant/ImpactPlant1", 1f, 0f, 0f);
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
			ComponentStrawBasket inventory = blockEntity.Entity.FindComponent<ComponentStrawBasket>(throwOnError: true);
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

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_random.Float(0f, 1f) < 0.25f)
		{
			PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
			if (precipitationShaftInfo.Intensity > 0f && y >= precipitationShaftInfo.YLimit - 1)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
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
		ComponentStrawBasket componentStrawBasket = movingEntityData.Entity.Entity.FindComponent<ComponentStrawBasket>();
		if (componentStrawBasket == null)
		{
			return false;
		}
		if (componentMiner.ComponentPlayer != null)
		{
			if (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == componentMiner.ComponentPlayer)
			{
				IPackage package = new MovingBlockEditPackage(componentStrawBasket);
				CommonLib.Net.QueuePackage(package);
				return true;
			}
			if (componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
			{
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new StrawBasketWidget(componentMiner.Inventory, componentStrawBasket);
				AudioManager.PlaySound("Audio/Impacts/Plant/ImpactPlant1", 1f, 0f, 0f);
				return true;
			}
		}
		return true;
	}
}
