using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class PlayerAchievements
{
	public class AchievementAttribute : Attribute
	{
		public bool IsDeathRequired;

		public AchievementAttribute(bool isDeathRequired = false)
		{
			IsDeathRequired = isDeathRequired;
		}
	}

	public struct Record
	{
		public string Name;

		public DateTime Date;

		public string Details;

		public void Load(string s)
		{
			string[] array = s.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 3)
			{
				throw new InvalidOperationException("Invalid achievement record.");
			}
			Name = array[0];
			Date = DateTime.Parse(array[1], CultureInfo.InvariantCulture);
			Details = array[2];
		}

		public string Save()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Name);
			stringBuilder.Append(',');
			stringBuilder.Append(Date.ToString("R", CultureInfo.InvariantCulture));
			stringBuilder.Append(',');
			stringBuilder.Append(Details);
			return stringBuilder.ToString();
		}
	}

	public class AchievementDisplay
	{
		public string Name { get; set; }

		public string Description { get; set; }

		public int? IconValue { get; set; }

		public AchievementTier Tier { get; set; }

		public string Texture { get; set; }
	}

	public enum AchievementTier
	{
		Common,
		Master,
		Evil
	}

	public List<Record> m_records = new List<Record>();

	public string RecordsString;

	public Queue<AchievementDisplay> AchievementQueue;

	[Achievement(false)]
	public long TreesChopping;

	[Achievement(false)]
	public long FireIsMyDomain;

	[Achievement(false)]
	public long FailedStoker;

	[Achievement(false)]
	public long UselessDeal;

	[Achievement(false)]
	public long Master;

	[Achievement(false)]
	public long FoundCoalBlock;

	[Achievement(false)]
	public long DurabilityCheck;

	[Achievement(false)]
	public long HeavyBurdenLife;

	[Achievement(false)]
	public long UmbrellaAboveAll;

	[Achievement(false)]
	public long BreadAboveAll;

	[Achievement(false)]
	public long HatedDay;

	[Achievement(false)]
	public long AthleticEndurance;

	[Achievement(false)]
	public long PolarStarJaws;

	[Achievement(true)]
	public long WhatTheH;

	[Achievement(false)]
	public long Abomination;

	[Achievement(false)]
	public long ForTheSafetyOfOurSanity;

	[Achievement(true)]
	public long SweetDreams;

	[Achievement(false)]
	public long TheLookFlayer;

	[Achievement(false)]
	public long Impossible;

	[Achievement(false)]
	public long SkyDemon;

	[Achievement(false)]
	public long Pandemic;

	[Achievement(true)]
	public long MostAccurateShooterInWest;

	[Achievement(true)]
	public long Ascetic;

	[Achievement(false)]
	public long MasochistSecondNatureName;

	[Achievement(false)]
	public long Scavenger;

	[Achievement(true)]
	public long FrozenHeart;

	[Achievement(false)]
	public long CriticalOverheat;

	[Achievement(false)]
	public long MasterPacifist;

	public SubsystemGameInfo m_subsystemGameInfo;

	public Guid m_playerGuid;

	private bool m_suppressChat;

	public IEnumerable<FieldInfo> Achievements
	{
		get
		{
			foreach (FieldInfo item in from f in typeof(PlayerAchievements).GetRuntimeFields()
				where f.GetCustomAttribute<AchievementAttribute>() != null
				select f)
			{
				yield return item;
			}
		}
	}

	public ReadOnlyList<Record> Records => new ReadOnlyList<Record>(m_records);

	public void AddRecord(Record record)
	{
		m_records.Add(record);
	}

	public void Load(ValuesDictionary valuesDictionary)
	{
		RecordsString = string.Empty;
		m_records.Clear();
		foreach (FieldInfo achievement in Achievements)
		{
			if (valuesDictionary.ContainsKey(achievement.Name))
			{
				object value = valuesDictionary.GetValue<object>(achievement.Name);
				achievement.SetValue(this, value);
			}
		}
		if (!string.IsNullOrEmpty(RecordsString))
		{
			string[] array = RecordsString.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string s in array)
			{
				Record item = default;
				item.Load(s);
				m_records.Add(item);
			}
		}
	}

	public void Save(ValuesDictionary valuesDictionary)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Record record in m_records)
		{
			stringBuilder.Append(record.Save());
			stringBuilder.Append(';');
		}
		RecordsString = stringBuilder.ToString();
		foreach (FieldInfo achievement in Achievements)
		{
			object value = achievement.GetValue(this);
			valuesDictionary.SetValue(achievement.Name, value);
		}
	}

	public PlayerAchievements()
	{
		AchievementQueue = new Queue<AchievementDisplay>();
	}

	public void EnqueueTreeChoppingAchievement()
	{
		EnqueueAchievement("TreesChoppingName", "TreesChoppingDescription", Terrain.MakeBlockValue(9));
	}

	public void EnqueueFireIsMyDomainAchievement()
	{
		EnqueueAchievement("FireIsMyDomainName", "FireIsMyDomainDescription", Terrain.MakeBlockValue(209));
	}

	public void EnqueueFailedStokerAchievement()
	{
		EnqueueAchievement("FailedStokerName", "FailedStokerDescription", Terrain.MakeBlockValue(209, 0, 0));
	}

	public void EnqueueUselessDealAchievement()
	{
		EnqueueAchievement("UselessDealName", "UselessDealDescription", Terrain.MakeBlockValue(354));
	}

	public void EnqueueMasterAchievement()
	{
		EnqueueAchievement("MasterName", "MasterDescription", Terrain.MakeBlockValue(27), AchievementTier.Master);
	}

	public void EnqueueFoundCoalBlockAchievement()
	{
		EnqueueAchievement("FoundCoalBlockName", "FoundCoalBlockDescription", Terrain.MakeBlockValue(150), AchievementTier.Master);
	}

	public void EnqueueDurabilityCheckAchievement()
	{
		EnqueueAchievement("DurabilityCheckName", "DurabilityCheckDescription", Terrain.MakeBlockValue(318));
	}

	public void EnqueueHeavyBurdenLifeAchievement()
	{
		EnqueueAchievement("HeavyBurdenLifeName", "HeavyBurdenLifeDescription", null, AchievementTier.Common, "Textures/Achievements/HeavyBurdenLife");
	}

	public void EnqueueUmbrellaAboveAllAchievement()
	{
		EnqueueAchievement("UmbrellaAboveAllName", "UmbrellaAboveAllDescription", Terrain.MakeBlockValue(379));
	}

	public void EnqueueBreadAboveAllAchievement()
	{
		EnqueueAchievement("BreadAboveAllName", "BreadAboveAllDescription", Terrain.MakeBlockValue(332));
	}

	public void EnqueueHatedDayAchievement()
	{
		EnqueueAchievement("HatedDayName", "HatedDayDescription", Terrain.MakeBlockValue(112));
	}

	public void EnqueueAthleticEnduranceAchievement()
	{
		EnqueueAchievement("AthleticEnduranceName", "AthleticEnduranceDescription", null, AchievementTier.Common, "Textures/Achievements/AthleticEndurance");
	}

	public void EnqueuePolarStarJawsAchievement()
	{
		EnqueueAchievement("PolarStarJawsName", "PolarStarJawsDescription", null, AchievementTier.Common, "Textures/Achievements/PolarStarJaws");
	}

	public void EnqueueWhatTheHAchievement()
	{
		EnqueueAchievement("WhatTheHName", "WhatTheHDescription", null, AchievementTier.Common, "Textures/Achievements/WhatTheH");
	}

	public void EnqueueAbominationAchievement()
	{
		EnqueueAchievement("AbominationName", "AbominationDescription", null, AchievementTier.Evil, "Textures/Achievements/Abomination");
	}

	public void EnqueueForTheSafetyOfOurSanityAchievement()
	{
		EnqueueAchievement("ForTheSafetyOfOurSanityName", "ForTheSafetyOfOurSanityDescription", Terrain.MakeBlockValue(380));
	}

	public void EnqueueSweetDreamsAchievement()
	{
		EnqueueAchievement("SweetDreamsName", "SweetDreamsDescription", null, AchievementTier.Common, "Textures/Achievements/SweetDreams");
	}

	private void EnqueueAchievement(string nameKey, string descriptionKey, int? iconValue, AchievementTier tier = AchievementTier.Common, string texture = null)
	{
		string i = nameKey;
		string entitySnapshotOp = descriptionKey;
		try
		{
			i = LanguageControl.Get("PlayerAchievements", nameKey);
			entitySnapshotOp = LanguageControl.Get("PlayerAchievements", descriptionKey);
		}
		catch
		{
		}
		if (m_subsystemGameInfo == null)
		{
			AchievementQueue.Enqueue(new AchievementDisplay
			{
				Name = i,
				Description = entitySnapshotOp,
				IconValue = iconValue,
				Texture = texture,
				Tier = tier
			});
			return;
		}
		GameMode gameMode = m_subsystemGameInfo.WorldSettings.GameMode;
		if (gameMode == GameMode.Adventure || gameMode == GameMode.Challenging || gameMode == GameMode.Creative || gameMode == GameMode.Harmless)
		{
			return;
		}
		PlayerData playerData = GetPlayerData();
		if (playerData == null)
		{
			return;
		}
		if (playerData.IsMainPlayer)
		{
			AchievementQueue.Enqueue(new AchievementDisplay
			{
				Name = i,
				Description = entitySnapshotOp,
				IconValue = iconValue,
				Texture = texture,
				Tier = tier
			});
			if (tier == AchievementTier.Master && playerData.ComponentPlayer != null)
			{
				playerData.ComponentPlayer.Entity.FindComponent<ComponentLevel>()?.AddExperience(360, playSound: true);
			}
		}
		if ((CommonLib.WorkType == WorkType.Server || CommonLib.WorkType == WorkType.Local) && !m_suppressChat)
		{
			SubsystemGameWidgets subsystemGameWidgets = m_subsystemGameInfo.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
			string msg = string.Format("#ACH#|{0}|{1}|{2}", playerData.Name, nameKey, tier switch
			{
				AchievementTier.Evil => (object)"DarkRed", 
				AchievementTier.Master => "Violet", 
				_ => "White", 
			});
			subsystemGameWidgets.AddMessage(msg, null, 0);
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel)
		{
			CheckAndGrantDeathAchievementsForCruel();
		}
	}

	public void EnqueueTheLookFlayerAchievement()
	{
		EnqueueAchievement("TheLookFlayerName", "TheLookFlayerDescription", null, AchievementTier.Common, "Textures/Achievements/TheLookFlayer");
	}

	public void EnqueueImpossibleAchievement()
	{
		EnqueueAchievement("ImpossibleName", "ImpossibleDescription", null, AchievementTier.Master, "Textures/Achievements/Impossible");
	}

	public void EnqueueSkyDemonAchievement()
	{
		EnqueueAchievement("SkyDemonName", "SkyDemonDescription", null, AchievementTier.Common, "Textures/Achievements/SkyDemon");
	}

	public void EnqueuePandemicAchievement()
	{
		EnqueueAchievement("PandemicName", "PandemicDescription", null, AchievementTier.Common, "Textures/Achievements/Pandemic");
	}

	public void EnqueueMostAccurateShooterInWestAchievement()
	{
		EnqueueAchievement("MostAccurateShooterInWestName", "MostAccurateShooterInWestDescription", Terrain.MakeBlockValue(214, 0, 1));
	}

	public void EnqueueAsceticAchievement()
	{
		EnqueueAchievement("AsceticName", "AsceticDescription", null, AchievementTier.Common, "Textures/Achievements/Ascetic");
	}

	public void EnqueueMasochistSecondNatureAchievement()
	{
		EnqueueAchievement("MasochistSecondNatureName", "MasochistSecondNatureDescription", Terrain.MakeBlockValue(112));
	}

	public void EnqueueScavengerAchievement()
	{
		EnqueueAchievement("ScavengerName", "ScavengerDescription", Terrain.MakeBlockValue(2));
	}

	public void EnqueueFrozenHeartAchievement()
	{
		EnqueueAchievement("FrozenHeartName", "FrozenHeartDescription", Terrain.MakeBlockValue(62));
	}

	public void EnqueueCriticalOverheatAchievement()
	{
		EnqueueAchievement("CriticalOverheatName", "CriticalOverheatDescription", Terrain.MakeBlockValue(203, 0, ClothingBlock.SetClothingIndex(0, 44)), AchievementTier.Master);
	}

	public void EnqueueMasterPacifistAchievement()
	{
		EnqueueAchievement("MasterPacifistName", "MasterPacifistDescription", null, AchievementTier.Master, "Textures/Achievements/MasterPacifist");
	}

	private void CheckAndGrantDeathAchievementsForCruel()
	{
		bool flag = true;
		foreach (FieldInfo achievement in Achievements)
		{
			AchievementAttribute customAttribute = achievement.GetCustomAttribute<AchievementAttribute>();
			if (customAttribute != null && !customAttribute.IsDeathRequired && (long)achievement.GetValue(this) == 0L)
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		m_suppressChat = true;
		int num = 0;
		foreach (FieldInfo achievement2 in Achievements)
		{
			AchievementAttribute customAttribute2 = achievement2.GetCustomAttribute<AchievementAttribute>();
			if (customAttribute2 == null || !customAttribute2.IsDeathRequired || (long)achievement2.GetValue(this) != 0L)
			{
				continue;
			}
			achievement2.SetValue(this, 1L);
			try
			{
				MethodInfo method = GetType().GetMethod("Enqueue" + achievement2.Name + "Achievement");
				if (method != null)
				{
					method.Invoke(this, null);
					num++;
				}
			}
			catch
			{
			}
		}
		m_suppressChat = false;
		if (num > 0)
		{
			PlayerData playerData = GetPlayerData();
			if (playerData != null && (CommonLib.WorkType == WorkType.Server || CommonLib.WorkType == WorkType.Local))
			{
				SubsystemGameWidgets subsystemGameWidgets = m_subsystemGameInfo.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
				string msg = LanguageControl.Get("SubsystemGameWidgets", "cruel_mass_unlock").Replace("{0}", playerData.Name);
				subsystemGameWidgets.AddMessage(msg, null, 0);
			}
		}
	}

	private PlayerData GetPlayerData()
	{
		if (m_subsystemGameInfo == null)
		{
			return null;
		}
		return m_subsystemGameInfo.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).FindPlayerData((PlayerData p) => p.PlayerGUID == m_playerGuid);
	}
}
