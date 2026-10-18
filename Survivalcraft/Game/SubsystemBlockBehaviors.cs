using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemBlockBehaviors : Subsystem
{
	public SubsystemBlockBehavior[][] m_blockBehaviorsByContents;

	public List<SubsystemBlockBehavior> m_blockBehaviors = new List<SubsystemBlockBehavior>();

	private SubsystemGameInfo m_subsystemGameInfo;

	public ReadOnlyList<SubsystemBlockBehavior> BlockBehaviors => new ReadOnlyList<SubsystemBlockBehavior>(m_blockBehaviors);

	public SubsystemBlockBehavior[] GetBlockBehaviors(int contents, ComponentMiner miner = null, Point3? point = null)
	{
		if (point.HasValue && miner != null)
		{
			bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(point.Value.X, point.Value.Z, out var territoriy);
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.RestrictWorldInteraction && miner.ComponentPlayer != null && !miner.ComponentPlayer.PlayerData.ServerManager && !flag)
			{
				bool flag2 = contents == 1;
				bool flag3 = m_subsystemGameInfo.WorldSettings.RegionPrivateLimit != 0;
				if (!flag2 || !flag3)
				{
					string text = LanguageControl.Get("SubsystemBlockBehaviors", "no_permissions");
					miner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Yellow, blinking: false, playNotificationSound: true);
					return new SubsystemBlockBehavior[0];
				}
			}
			if (flag)
			{
				bool flag4 = miner.ComponentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
				if (((flag4 && !territoriy.ApplyToFirend) || !flag4) && territoriy.OwnerGuid != miner.ComponentPlayer.PlayerData.PlayerGUID && !territoriy.AllowDig && !miner.ComponentPlayer.PlayerData.ServerManager)
				{
					string text2 = LanguageControl.Get("SubsystemBlockBehaviors", "no_permissions");
					miner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.Yellow, blinking: false, playNotificationSound: true);
					return new SubsystemBlockBehavior[0];
				}
			}
		}
		return m_blockBehaviorsByContents[contents];
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_blockBehaviorsByContents = new SubsystemBlockBehavior[BlocksManager.Blocks.Length][];
		Dictionary<int, List<SubsystemBlockBehavior>> dictionary = new Dictionary<int, List<SubsystemBlockBehavior>>();
		for (int i = 0; i < m_blockBehaviorsByContents.Length; i++)
		{
			dictionary[i] = new List<SubsystemBlockBehavior>();
			string[] array = BlocksManager.Blocks[i].Behaviors.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				SubsystemBlockBehavior item = Project.FindSubsystem<SubsystemBlockBehavior>(text.Trim(), throwOnError: true);
				dictionary[i].Add(item);
			}
		}
		foreach (SubsystemBlockBehavior item2 in Project.FindSubsystems<SubsystemBlockBehavior>())
		{
			m_blockBehaviors.Add(item2);
			int[] handledBlocks = item2.HandledBlocks;
			foreach (int key in handledBlocks)
			{
				dictionary[key].Add(item2);
			}
		}
		for (int k = 0; k < m_blockBehaviorsByContents.Length; k++)
		{
			m_blockBehaviorsByContents[k] = dictionary[k].ToArray();
		}
	}
}
