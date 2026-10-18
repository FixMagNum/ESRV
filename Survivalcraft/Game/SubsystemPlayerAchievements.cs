using System;
using System.Collections.Generic;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemPlayerAchievements : Subsystem
{
	public Dictionary<Guid, PlayerAchievements> m_playerAchievements = new Dictionary<Guid, PlayerAchievements>();

	public SubsystemGameInfo m_subsystemGameInfo;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue("Achievements", new ValuesDictionary()))
		{
			if (Guid.TryParse(item.Key, out var result))
			{
				PlayerAchievements playerAchievements = new PlayerAchievements();
				playerAchievements.m_subsystemGameInfo = m_subsystemGameInfo;
				playerAchievements.Load((ValuesDictionary)item.Value);
				playerAchievements.m_playerGuid = result;
				m_playerAchievements[result] = playerAchievements;
			}
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Achievements", valuesDictionary2);
		foreach (KeyValuePair<Guid, PlayerAchievements> playerAchievement in m_playerAchievements)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			playerAchievement.Value.Save(valuesDictionary3);
			valuesDictionary2.SetValue(playerAchievement.Key.ToString(), valuesDictionary3);
		}
	}

	public PlayerAchievements GetPlayerAchievements(Guid playerGuid)
	{
		if (!m_playerAchievements.TryGetValue(playerGuid, out var value))
		{
			value = new PlayerAchievements();
			value.m_subsystemGameInfo = m_subsystemGameInfo;
			m_playerAchievements.Add(playerGuid, value);
			value.m_playerGuid = playerGuid;
		}
		return value;
	}
}
