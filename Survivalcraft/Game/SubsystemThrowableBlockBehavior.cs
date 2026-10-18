using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemThrowableBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemProjectiles m_subsystemProjectiles;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[0];

	public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state)
	{
		switch (state)
		{
		case AimState.Completed:
		{
			Vector3 vector = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition + componentMiner.ComponentCreature.ComponentBody.Matrix.Right * 0.4f;
			Vector3 vector2 = Vector3.Normalize(vector + aim.Direction * 10f - vector);
			if (componentMiner.Inventory == null)
			{
				break;
			}
			int activeSlotIndex = componentMiner.Inventory.ActiveSlotIndex;
			int slotValue = componentMiner.Inventory.GetSlotValue(activeSlotIndex);
			int slotCount = componentMiner.Inventory.GetSlotCount(activeSlotIndex);
			int num = Terrain.ExtractContents(slotValue);
			Block block2 = BlocksManager.Blocks[num];
			if (slotCount > 0)
			{
				float num2 = block2.ProjectileSpeed;
				if (componentMiner.ComponentPlayer != null)
				{
					num2 *= 0.5f * (componentMiner.ComponentPlayer.ComponentLevel.StrengthFactor - 1f) + 1f;
				}
				if (m_subsystemProjectiles.FireProjectile(slotValue, vector, vector2 * num2, m_random.Vector3(5f, 10f), componentMiner.ComponentCreature) != null)
				{
					if (CommonLib.WorkType != WorkType.Client)
					{
						componentMiner.Inventory.RemoveSlotItems(activeSlotIndex, 1);
					}
					m_subsystemAudio.PlaySound("Audio/Throw", m_random.Float(0.2f, 0.3f), m_random.Float(-0.2f, 0.2f), aim.Position, 2f, autoDelay: true);
					componentMiner.Poke(forceRestart: false);
				}
			}
			return true;
		}
		case AimState.InProgress:
		{
			componentMiner.ComponentCreature.ComponentCreatureModel.AimHandAngleOrder = 4.2f;
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(componentMiner.ActiveBlockValue)];
			ComponentFirstPersonModel componentFirstPersonModel = componentMiner.Entity.FindComponent<ComponentFirstPersonModel>();
			if (componentFirstPersonModel != null)
			{
				componentMiner.ComponentPlayer?.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
				componentFirstPersonModel.ItemOffsetOrder = new Vector3(0f, 0.35f, 0.17f);
				if (block is WoodenSpearBlock || block is StoneSpearBlock || block is CopperSpearBlock || block is IronSpearBlock || block is DiamondSpearBlock)
				{
					componentFirstPersonModel.ItemRotationOrder = new Vector3(-1.5f, 0f, 0f);
				}
			}
			if (block is WoodenSpearBlock || block is StoneSpearBlock || block is CopperSpearBlock || block is IronSpearBlock || block is DiamondSpearBlock)
			{
				componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemOffsetOrder = new Vector3(0f, -0.25f, 0f);
				componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemRotationOrder = new Vector3(3.14159f, 0f, 0f);
			}
			break;
		}
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		base.Load(valuesDictionary);
	}
}
