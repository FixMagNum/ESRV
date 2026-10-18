using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemStrawBlockBehavior : SubsystemPollableBlockBehavior
{
	protected SubsystemTerrain m_subsystemTerrain;

	protected SubsystemWeather m_subsystemWeather;

	protected SubsystemTime m_subsystemTime;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[3] { 307, 309, 310 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
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
		if (componentMiner.ComponentPlayer != null && componentMiner.ComponentPlayer.PlayerData.IsMainPlayer)
		{
			componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new StrawBasketWidget(componentMiner.Inventory, componentStrawBasket);
			AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			return true;
		}
		return true;
	}
}
