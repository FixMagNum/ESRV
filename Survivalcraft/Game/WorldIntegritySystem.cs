using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Engine;
using GameEntitySystem;

namespace Game;

public static class WorldIntegritySystem
{
	private const char ENTRY_SEPARATOR = '|';

	private const char DATA_SEPARATOR = ':';

	private const string HIDDEN_FILE_NAME = "System.Lighting.dll";

	private static readonly byte[] _xorKey;

	public static void ValidateWorld(Project project)
	{
		WorldSettings worldSettings = project.FindSubsystem<SubsystemGameInfo>().WorldSettings;
		SubsystemPlayers subsystemPlayers = project.FindSubsystem<SubsystemPlayers>();
		if (!string.IsNullOrEmpty(worldSettings.Seed) && worldSettings.Seed.ToLower() == "cheater")
		{
			MarkCheater(subsystemPlayers, "Legendary");
		}
		string worldGuid = worldSettings.WorldGuid;
		Dictionary<string, string> dictionary = LoadHiddenCache();
		GameMode gameMode = worldSettings.GameMode;
		if (!dictionary.ContainsKey(worldGuid))
		{
			string text = (worldSettings.LockWorldSettings ? SerializeLockedSettings(worldSettings) : "NONE");
			double totalElapsedGameTime = project.FindSubsystem<SubsystemGameInfo>().TotalElapsedGameTime;
			bool flag = false;
			dictionary[worldGuid] = string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2:0.0}:{3}", (int)gameMode, text, totalElapsedGameTime, flag ? 1 : 0);
			SaveHiddenCache(dictionary);
			return;
		}
		string[] array = dictionary[worldGuid].Split(':');
		GameMode gameMode2 = GameMode.Challenging;
		string text2 = "NONE";
		double result = -1.0;
		if (array.Length >= 1 && int.TryParse(array[0], out var result2))
		{
			gameMode2 = (GameMode)result2;
		}
		if (array.Length >= 2)
		{
			text2 = array[1];
		}
		if (array.Length >= 3)
		{
			double.TryParse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}
		bool flag2 = array.Length >= 4 && array[3] == "1";
		bool flag3 = false;
		foreach (PlayerData playersDatum in subsystemPlayers.PlayersData)
		{
			if (playersDatum.IsCheater)
			{
				flag3 = true;
				break;
			}
		}
		if (flag2 | flag3)
		{
			MarkCheater(subsystemPlayers, flag2 ? "Tainted World" : "XML Editor");
		}
		if (IsHardcoreMode(gameMode2))
		{
			double totalElapsedGameTime2 = project.FindSubsystem<SubsystemGameInfo>().TotalElapsedGameTime;
			if (result > 0.0 && totalElapsedGameTime2 < result - 300.0)
			{
				Log.Warning($"INTEGRITY: Time Rollback Detected! World: {totalElapsedGameTime2:0.0}, Cache: {result:0.0}");
				MarkCheater(subsystemPlayers, "Time Walker");
				return;
			}
		}
		bool flag4 = false;
		string text3 = "";
		if (IsHardcoreMode(gameMode2))
		{
			if (gameMode != gameMode2)
			{
				flag4 = true;
				text3 = "Changed Hardcore Mode";
				worldSettings.GameMode = gameMode2;
				worldSettings.ResetOptionsForNonCreativeMode();
			}
		}
		else if (IsCasualMode(gameMode2))
		{
			if (IsHardcoreMode(gameMode))
			{
				flag4 = true;
				text3 = "Illegal switch to Hardcore";
				worldSettings.GameMode = GameMode.Adventure;
			}
			else if (gameMode != gameMode2)
			{
				if (!worldSettings.LockWorldSettings)
				{
					SaveIntegrity(project);
					return;
				}
				worldSettings.GameMode = gameMode2;
				Log.Warning("Integrity: Reverted casual GameMode change (Locked).");
			}
			if (!flag4 && text2 != "NONE")
			{
				if (!worldSettings.LockWorldSettings)
				{
					worldSettings.LockWorldSettings = true;
					RestoreLockedSettings(worldSettings, text2);
				}
				else if (SerializeLockedSettings(worldSettings) != text2)
				{
					RestoreLockedSettings(worldSettings, text2);
				}
			}
		}
		if (flag4)
		{
			Log.Warning("INTEGRITY BREACH: " + text3);
			MarkCheater(subsystemPlayers, "Liar");
		}
	}

	public static void SaveIntegrity(Project project)
	{
		try
		{
			SubsystemGameInfo subsystemGameInfo = project.FindSubsystem<SubsystemGameInfo>();
			WorldSettings worldSettings = subsystemGameInfo.WorldSettings;
			string worldGuid = worldSettings.WorldGuid;
			int gameMode = (int)worldSettings.GameMode;
			string text = (worldSettings.LockWorldSettings ? SerializeLockedSettings(worldSettings) : "NONE");
			double num = subsystemGameInfo.TotalElapsedGameTime;
			bool flag = false;
			foreach (PlayerData playersDatum in project.FindSubsystem<SubsystemPlayers>().PlayersData)
			{
				if (playersDatum.IsCheater)
				{
					flag = true;
					break;
				}
			}
			Dictionary<string, string> dictionary = LoadHiddenCache();
			if (dictionary.ContainsKey(worldGuid))
			{
				string[] array = dictionary[worldGuid].Split(':');
				if (array.Length >= 4 && array[3] == "1")
				{
					flag = true;
				}
				if (array.Length >= 3 && double.TryParse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > num)
				{
					num = result;
				}
			}
			string value = string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2:0.0}:{3}", gameMode, text, num, flag ? 1 : 0);
			dictionary[worldGuid] = value;
			SaveHiddenCache(dictionary);
		}
		catch (Exception ex)
		{
			Log.Warning("Failed to update system cache: " + ex.Message);
		}
	}

	private static bool IsCasualMode(GameMode mode)
	{
		if (mode != GameMode.Creative && mode != GameMode.Harmless && mode != GameMode.Adventure)
		{
			return mode == GameMode.Challenging;
		}
		return true;
	}

	private static bool IsHardcoreMode(GameMode mode)
	{
		if (mode != GameMode.Cruel && mode != GameMode.RealChallenging)
		{
			return mode == GameMode.Embrace;
		}
		return true;
	}

	private static string GetHiddenFilePath()
	{
		return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemLighting.dll");
	}

	private static byte[] ApplyXor(byte[] data)
	{
		byte[] array = new byte[data.Length];
		for (int i = 0; i < data.Length; i++)
		{
			array[i] = (byte)(data[i] ^ _xorKey[i % _xorKey.Length]);
		}
		return array;
	}

	static WorldIntegritySystem()
	{
		_xorKey = new byte[8] { 18, 68, 113, 153, 171, 1, 34, 85 };
	}

	private static string SerializeLockedSettings(WorldSettings s)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Env=").Append((int)s.EnvironmentBehaviorMode).Append(";");
		stringBuilder.Append("Time=").Append((int)s.TimeOfDayMode).Append(";");
		stringBuilder.Append("Weather=").Append(s.AreWeatherEffectsEnabled).Append(";");
		stringBuilder.Append("Respawn=").Append(s.IsAdventureRespawnAllowed).Append(";");
		stringBuilder.Append("Survival=").Append(s.AreAdventureSurvivalMechanicsEnabled).Append(";");
		stringBuilder.Append("Supernatural=").Append(s.AreSupernaturalCreaturesEnabled).Append(";");
		stringBuilder.Append("FriendlyFire=").Append(s.IsFriendlyFireEnabled).Append(";");
		stringBuilder.Append("DayDur=").Append(s.DayCycleDuration).Append(";");
		stringBuilder.Append("SeasonDur=").Append(s.SeasonCycleDuration).Append(";");
		stringBuilder.Append("Breath=").Append((int)s.BreathingMode).Append(";");
		stringBuilder.Append("CraftShuffle=").Append(s.IsCraftingShuffleEnabled).Append(";");
		stringBuilder.Append("ExplDrops=").Append(s.AreExplosionDropsEnabled).Append(";");
		stringBuilder.Append("ReqLevel=").Append(s.RequiresLevelsForCrafting).Append(";");
		stringBuilder.Append("ImmutLevel=").Append(s.IsImmutableWorldLevelEnabled).Append(";");
		stringBuilder.Append("ImmutVal=").Append(s.ImmutableWorldLevel).Append(";");
		stringBuilder.Append("MaxHealth=").Append(s.MaxHealthMultiplier).Append(";");
		stringBuilder.Append("KeepInv=").Append(s.KeepInventoryAfterDeath).Append(";");
		stringBuilder.Append("PlayerSize=").Append(s.PlayerSizeMultiplier).Append(";");
		stringBuilder.Append("FluidsBlock=").Append(s.FluidsDoNotBreakBlocks).Append(";");
		stringBuilder.Append("Fire=").Append(s.FireSpreadEnabled).Append(";");
		stringBuilder.Append("Fall=").Append(s.FallDamageEnabled).Append(";");
		stringBuilder.Append("ItemDur=").Append(s.ItemDurabilityMultiplier).Append(";");
		stringBuilder.Append("ExpGain=").Append(s.ExperienceGainMultiplier).Append(";");
		stringBuilder.Append("ExplDestroy=").Append(s.ExplosionsDestroyTerrain).Append(";");
		stringBuilder.Append("Lives=").Append(s.LivesLimit).Append(";");
		stringBuilder.Append("LifeRest=").Append((int)s.LifeRestoration).Append(";");
		return stringBuilder.ToString();
	}

	private static void RestoreLockedSettings(WorldSettings s, string data)
	{
		try
		{
			string[] array = data.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length == 2)
				{
					string text = array2[0];
					string text2 = array2[1];
					switch (text)
					{
					case "Env":
						s.EnvironmentBehaviorMode = (EnvironmentBehaviorMode)int.Parse(text2);
						break;
					case "Time":
						s.TimeOfDayMode = (TimeOfDayMode)int.Parse(text2);
						break;
					case "Weather":
						s.AreWeatherEffectsEnabled = bool.Parse(text2);
						break;
					case "Respawn":
						s.IsAdventureRespawnAllowed = bool.Parse(text2);
						break;
					case "Survival":
						s.AreAdventureSurvivalMechanicsEnabled = bool.Parse(text2);
						break;
					case "Supernatural":
						s.AreSupernaturalCreaturesEnabled = bool.Parse(text2);
						break;
					case "FriendlyFire":
						s.IsFriendlyFireEnabled = bool.Parse(text2);
						break;
					case "DayDur":
						s.DayCycleDuration = float.Parse(text2);
						break;
					case "SeasonDur":
						s.SeasonCycleDuration = float.Parse(text2);
						break;
					case "Breath":
						s.BreathingMode = (BreathingMode)int.Parse(text2);
						break;
					case "CraftShuffle":
						s.IsCraftingShuffleEnabled = bool.Parse(text2);
						break;
					case "ExplDrops":
						s.AreExplosionDropsEnabled = bool.Parse(text2);
						break;
					case "ReqLevel":
						s.RequiresLevelsForCrafting = bool.Parse(text2);
						break;
					case "ImmutLevel":
						s.IsImmutableWorldLevelEnabled = bool.Parse(text2);
						break;
					case "ImmutVal":
						s.ImmutableWorldLevel = int.Parse(text2);
						break;
					case "MaxHealth":
						s.MaxHealthMultiplier = float.Parse(text2);
						break;
					case "KeepInv":
						s.KeepInventoryAfterDeath = bool.Parse(text2);
						break;
					case "PlayerSize":
						s.PlayerSizeMultiplier = float.Parse(text2);
						break;
					case "FluidsBlock":
						s.FluidsDoNotBreakBlocks = bool.Parse(text2);
						break;
					case "Fire":
						s.FireSpreadEnabled = bool.Parse(text2);
						break;
					case "Fall":
						s.FallDamageEnabled = bool.Parse(text2);
						break;
					case "ItemDur":
						s.ItemDurabilityMultiplier = float.Parse(text2);
						break;
					case "ExpGain":
						s.ExperienceGainMultiplier = float.Parse(text2);
						break;
					case "ExplDestroy":
						s.ExplosionsDestroyTerrain = bool.Parse(text2);
						break;
					case "Lives":
						s.LivesLimit = int.Parse(text2);
						break;
					case "LifeRest":
						s.LifeRestoration = (LifeRestorationMode)int.Parse(text2);
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("Failed to restore settings: " + ex.Message);
		}
	}

	private static Dictionary<string, string> LoadHiddenCache()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string hiddenFilePath = GetHiddenFilePath();
		if (!File.Exists(hiddenFilePath))
		{
			return dictionary;
		}
		try
		{
			byte[] bytes = ApplyXor(File.ReadAllBytes(hiddenFilePath));
			string[] array = Encoding.UTF8.GetString(bytes).Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(':');
				if (array2.Length >= 2)
				{
					string key = array2[0];
					string value = string.Join(":", array2, 1, array2.Length - 1);
					dictionary[key] = value;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warning("System integrity cache corrupted. " + ex.Message);
		}
		return dictionary;
	}

	private static void SaveHiddenCache(Dictionary<string, string> data)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, string> datum in data)
			{
				stringBuilder.Append(datum.Key).Append(':').Append(datum.Value)
					.Append('|');
			}
			string s = stringBuilder.ToString();
			byte[] bytes = ApplyXor(Encoding.UTF8.GetBytes(s));
			File.WriteAllBytes(GetHiddenFilePath(), bytes);
		}
		catch (Exception ex)
		{
			Log.Error("Critical: Failed to write system integrity cache. " + ex.Message);
		}
	}

	private static void MarkCheater(SubsystemPlayers subsystemPlayers, string prefix)
	{
		foreach (PlayerData playersDatum in subsystemPlayers.PlayersData)
		{
			if (!playersDatum.IsCheater)
			{
				playersDatum.IsCheater = true;
				playersDatum.ExtraLives = 0;
				if (!playersDatum.Name.StartsWith(prefix))
				{
					playersDatum.Name = prefix + " " + playersDatum.Name;
				}
			}
		}
		SaveIntegrity(subsystemPlayers.Project);
	}
}
