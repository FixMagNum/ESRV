using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemKnittingNeedlesBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemBodies m_subsystemBodies;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemNoise m_subsystemNoise;

	public Random m_random;

	public SubsystemTime m_subsystemTime;

	private Dictionary<Guid, DateTime> playerLastUseTime = new Dictionary<Guid, DateTime>();

	public override int[] HandledBlocks => new int[1] { 351 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		Guid playerGUID = componentMiner.ComponentPlayer.PlayerData.PlayerGUID;
		if (playerLastUseTime.ContainsKey(playerGUID) && (DateTime.Now - playerLastUseTime[playerGUID]).TotalSeconds < 0.5)
		{
			return false;
		}
		playerLastUseTime[playerGUID] = DateTime.Now;
		m_subsystemAudio.PlayRandomSound("Audio/Knitting needles", 5f, m_random.Float(-0.6f, 0f), ray.Position, 4f, autoDelay: true);
		m_subsystemNoise.MakeNoise(componentMiner.ComponentCreature.ComponentBody, 0.5f, 30f);
		componentMiner.DamageActiveTool(1);
		int num2 = componentMiner.ActiveBlockValue;
		if (BlocksManager.Blocks[Terrain.ExtractContents(num2)].GetDurability(num2) <= 0)
		{
			GivePlayerFabric(componentMiner.ComponentPlayer);
		}
		return true;
	}

	private void GivePlayerFabric(ComponentPlayer player)
	{
		IInventory inventory = player.ComponentMiner.Inventory;
		int value = Terrain.MakeBlockValue(206);
		int furnitureSet = ComponentInventoryBase.FindAcquireSlotForItem(inventory, value);
		inventory.AddSlotItems(furnitureSet, value, 1);
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public SubsystemKnittingNeedlesBlockBehavior()
	{
		m_random = new Random();
	}
}
