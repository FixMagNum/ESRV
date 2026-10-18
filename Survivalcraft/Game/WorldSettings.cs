using System;
using Engine;
using TemplatesDatabase;

namespace Game;

public class WorldSettings
{
	public string Name;

	public string OriginalSerializationVersion;

	public string Seed;

	public GameMode GameMode;

	public EnvironmentBehaviorMode EnvironmentBehaviorMode;

	public TimeOfDayMode TimeOfDayMode;

	public ushort MaxOnlinePlayerCount;

	public StartingPositionMode StartingPositionMode;

	public bool AreWeatherEffectsEnabled;

	public bool IsAdventureRespawnAllowed;

	public bool AreAdventureSurvivalMechanicsEnabled;

	public bool AreSupernaturalCreaturesEnabled;

	public bool IsFriendlyFireEnabled;

	public bool IsNeedCommunityLogin;

	public TerrainGenerationMode TerrainGenerationMode;

	public Vector2 IslandSize;

	public float BiomeSize;

	public int TerrainLevel;

	public float ShoreRoughness;

	public int TerrainBlockIndex;

	public int TerrainOceanBlockIndex;

	public float TemperatureOffset;

	public float HumidityOffset;

	public int SeaLevelOffset;

	public string Password;

	public string BlocksTextureName;

	public bool RunServer;

	public WorldPalette Palette;

	public float DaySpeed;

	public float RecoverFator;

	public string DisableBlocks;

	public bool RandomSpawnPosition;

	public bool AreExplosionDropsEnabled = true;

	public bool RequirePassword;

	public bool IsWorldEditIntegrated;

	public bool WEInterventionUnlimited;

	public int WEInterventionLimit;

	public int RegionPrivateLimit;

	public bool RestrictWorldInteraction;

	public bool WEManagersOnly;

	public bool FireSpreadEnabled;

	public bool FallDamageEnabled;

	public float ItemDurabilityMultiplier;

	public float ExperienceGainMultiplier;

	public bool ExplosionsDestroyTerrain;

	public bool RequiresLevelsForCrafting;

	public int ImmutableWorldLevel;

	public bool LockWorldSettings;

	public float MaxHealthMultiplier;

	public bool KeepInventoryAfterDeath;

	public float PlayerSizeMultiplier;

	public bool FluidsDoNotBreakBlocks;

	public bool IsImmutableWorldLevelEnabled;

	public float DayCycleDuration;

	public float SeasonCycleDuration;

	public bool DisableNightPredatorSpam;

	public int FluidUpdateQuota;

	public int LivesLimit;

	public LifeRestorationMode LifeRestoration;

	public BreathingMode BreathingMode;

	public bool IsCraftingShuffleEnabled;

	public string WorldGuid;

	public int FluidSpreadDistance;

	public bool DelayHeavyChunkLoading;

	public void ResetOptionsForNonCreativeMode()
	{
		if (GameMode == GameMode.Embrace)
		{
			EnforceEmbraceSettings();
		}
		else if (GameMode == GameMode.Cruel)
		{
			EnforceCruelSettings();
		}
		else if (GameMode == GameMode.RealChallenging)
		{
			EnforceRealChallengingSettings();
		}
	}

	public void Load(ValuesDictionary valuesDictionary)
	{
		Name = valuesDictionary.GetValue<string>("WorldName");
		WorldGuid = valuesDictionary.GetValue("WorldGuid", Guid.NewGuid().ToString("N"));
		OriginalSerializationVersion = valuesDictionary.GetValue("OriginalSerializationVersion", string.Empty);
		Seed = valuesDictionary.GetValue("WorldSeedString", string.Empty);
		GameMode = valuesDictionary.GetValue("GameMode", GameMode.Challenging);
		EnvironmentBehaviorMode = valuesDictionary.GetValue("EnvironmentBehaviorMode", EnvironmentBehaviorMode.Living);
		TimeOfDayMode = valuesDictionary.GetValue("TimeOfDayMode", TimeOfDayMode.Changing);
		StartingPositionMode = valuesDictionary.GetValue("StartingPositionMode", StartingPositionMode.Easy);
		AreWeatherEffectsEnabled = valuesDictionary.GetValue("AreWeatherEffectsEnabled", defaultValue: true);
		IsAdventureRespawnAllowed = valuesDictionary.GetValue("IsAdventureRespawnAllowed", defaultValue: true);
		AreAdventureSurvivalMechanicsEnabled = valuesDictionary.GetValue("AreAdventureSurvivalMechanicsEnabled", defaultValue: true);
		AreSupernaturalCreaturesEnabled = valuesDictionary.GetValue("AreSupernaturalCreaturesEnabled", defaultValue: true);
		IsFriendlyFireEnabled = valuesDictionary.GetValue("IsFriendlyFireEnabled", defaultValue: true);
		TerrainGenerationMode = valuesDictionary.GetValue("TerrainGenerationMode", TerrainGenerationMode.Continent);
		IslandSize = valuesDictionary.GetValue("IslandSize", new Vector2(200f, 200f));
		TerrainLevel = valuesDictionary.GetValue("TerrainLevel", 64);
		Password = valuesDictionary.GetValue("Password", string.Empty);
		ShoreRoughness = valuesDictionary.GetValue("ShoreRoughness", 0f);
		DayCycleDuration = valuesDictionary.GetValue("DayCycleDuration", 6400f);
		SeasonCycleDuration = valuesDictionary.GetValue("SeasonCycleDuration", 144000f);
		BreathingMode = valuesDictionary.GetValue("BreathingMode", BreathingMode.Air);
		IsCraftingShuffleEnabled = valuesDictionary.GetValue("IsCraftingShuffleEnabled", defaultValue: false);
		DelayHeavyChunkLoading = valuesDictionary.GetValue("DelayHeavyChunkLoading", defaultValue: true);
		DaySpeed = valuesDictionary.GetValue("DaySpeed", 1f);
		if (DaySpeed <= 0f)
		{
			DaySpeed = 1f;
		}
		RecoverFator = valuesDictionary.GetValue("RecoverFator", 1f);
		if (RecoverFator <= 0f)
		{
			RecoverFator = 1f;
		}
		TerrainBlockIndex = valuesDictionary.GetValue("TerrainBlockIndex", 8);
		AreExplosionDropsEnabled = valuesDictionary.GetValue("AreExplosionDropsEnabled", defaultValue: true);
		TerrainOceanBlockIndex = valuesDictionary.GetValue("TerrainOceanBlockIndex", 18);
		TemperatureOffset = valuesDictionary.GetValue("TemperatureOffset", 0f);
		HumidityOffset = valuesDictionary.GetValue("HumidityOffset", 0f);
		SeaLevelOffset = valuesDictionary.GetValue("SeaLevelOffset", 0);
		BiomeSize = valuesDictionary.GetValue("BiomeSize", 1f);
		RunServer = valuesDictionary.GetValue("RunServer", defaultValue: false);
		IsNeedCommunityLogin = valuesDictionary.GetValue("IsNeedCommunityLogin", defaultValue: false);
		RequirePassword = valuesDictionary.GetValue("RequirePassword", defaultValue: false);
		MaxOnlinePlayerCount = valuesDictionary.GetValue("MaxOnlinePlayerCount", MaxOnlinePlayerCount);
		BlocksTextureName = valuesDictionary.GetValue("BlockTextureName", string.Empty);
		DisableBlocks = valuesDictionary.GetValue("DisableBlocks", DisableBlocks);
		Palette = new WorldPalette(valuesDictionary.GetValue("Palette", new ValuesDictionary()));
		RandomSpawnPosition = valuesDictionary.GetValue("RandomSpawnPosition", RandomSpawnPosition);
		IsWorldEditIntegrated = valuesDictionary.GetValue("WEIntegrated", defaultValue: true);
		WEInterventionUnlimited = valuesDictionary.GetValue("WEInterventionUnlimited", defaultValue: true);
		WEInterventionLimit = valuesDictionary.GetValue("WEInterventionLimit", 0);
		RegionPrivateLimit = valuesDictionary.GetValue("RegionPrivateLimit", 10);
		RestrictWorldInteraction = valuesDictionary.GetValue("RestrictWorldInteraction", defaultValue: false);
		WEManagersOnly = valuesDictionary.GetValue("WEManagersOnly", defaultValue: false);
		RequiresLevelsForCrafting = valuesDictionary.GetValue("RequiresLevelsForCrafting", defaultValue: true);
		ImmutableWorldLevel = valuesDictionary.GetValue("ImmutableWorldLevel", 1);
		IsImmutableWorldLevelEnabled = valuesDictionary.GetValue("IsImmutableWorldLevelEnabled", defaultValue: false);
		LockWorldSettings = valuesDictionary.GetValue("LockWorldSettings", defaultValue: false);
		MaxHealthMultiplier = valuesDictionary.GetValue("MaxHealthMultiplier", 1f);
		KeepInventoryAfterDeath = valuesDictionary.GetValue("KeepInventoryAfterDeath", defaultValue: false);
		PlayerSizeMultiplier = valuesDictionary.GetValue("PlayerSizeMultiplier", 1f);
		FluidsDoNotBreakBlocks = valuesDictionary.GetValue("FluidsDoNotBreakBlocks", defaultValue: false);
		FireSpreadEnabled = valuesDictionary.GetValue("FireSpreadEnabled", defaultValue: true);
		FallDamageEnabled = valuesDictionary.GetValue("FallDamageEnabled", defaultValue: true);
		ItemDurabilityMultiplier = valuesDictionary.GetValue("ItemDurabilityMultiplier", 1f);
		ExperienceGainMultiplier = valuesDictionary.GetValue("ExperienceGainMultiplier", 1f);
		ExplosionsDestroyTerrain = valuesDictionary.GetValue("ExplosionsDestroyTerrain", defaultValue: true);
		DisableNightPredatorSpam = valuesDictionary.GetValue("DisableNightPredatorSpam", defaultValue: true);
		FluidUpdateQuota = valuesDictionary.GetValue("FluidUpdateQuota", 4000);
		FluidSpreadDistance = valuesDictionary.GetValue("FluidSpreadDistance", 3);
		LivesLimit = valuesDictionary.GetValue("LivesLimit", 0);
		LifeRestoration = valuesDictionary.GetValue("LifeRestorationMode", LifeRestorationMode.None);
		if (GameMode == GameMode.Embrace)
		{
			EnforceEmbraceSettings();
		}
		else
		{
			ResetOptionsForNonCreativeMode();
		}
	}

	public bool IsBlockDiable(int v)
	{
		int cellContents = Terrain.ExtractContents(v);
		Terrain.ExtractData(v);
		string[] i = DisableBlocks.Split(';');
		foreach (string y in i)
		{
			if (string.IsNullOrEmpty(y))
			{
				continue;
			}
			CraftingRecipesManager.DecodeIngredient(y, out var z, out var value);
			Block[] array = BlocksManager.FindBlocksByCraftingId(z);
			for (int j = 0; j < array.Length; j++)
			{
				if (value.HasValue)
				{
					int num = Terrain.MakeBlockValue(array[j].BlockIndex, 0, value.Value);
					if (v == num)
					{
						return true;
					}
				}
				else if (cellContents == array[j].BlockIndex)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void Save(ValuesDictionary valuesDictionary, bool liveModifiableParametersOnly)
	{
		valuesDictionary.SetValue("WorldName", Name);
		valuesDictionary.SetValue("OriginalSerializationVersion", OriginalSerializationVersion);
		valuesDictionary.SetValue("GameMode", GameMode);
		valuesDictionary.SetValue("EnvironmentBehaviorMode", EnvironmentBehaviorMode);
		valuesDictionary.SetValue("TimeOfDayMode", TimeOfDayMode);
		valuesDictionary.SetValue("RecoverFator", RecoverFator);
		valuesDictionary.SetValue("DayCycleDuration", DayCycleDuration);
		if (string.IsNullOrEmpty(WorldGuid))
		{
			WorldGuid = Guid.NewGuid().ToString("N");
		}
		valuesDictionary.SetValue("WorldGuid", WorldGuid);
		valuesDictionary.SetValue("SeasonCycleDuration", SeasonCycleDuration);
		valuesDictionary.SetValue("DaySpeed", DaySpeed);
		valuesDictionary.SetValue("AreWeatherEffectsEnabled", AreWeatherEffectsEnabled);
		valuesDictionary.SetValue("IsAdventureRespawnAllowed", IsAdventureRespawnAllowed);
		valuesDictionary.SetValue("AreAdventureSurvivalMechanicsEnabled", AreAdventureSurvivalMechanicsEnabled);
		valuesDictionary.SetValue("AreSupernaturalCreaturesEnabled", AreSupernaturalCreaturesEnabled);
		valuesDictionary.SetValue("IsFriendlyFireEnabled", IsFriendlyFireEnabled);
		valuesDictionary.SetValue("Password", Password);
		valuesDictionary.SetValue("RunServer", RunServer);
		valuesDictionary.SetValue("AreExplosionDropsEnabled", AreExplosionDropsEnabled);
		valuesDictionary.SetValue("IsNeedCommunityLogin", IsNeedCommunityLogin);
		valuesDictionary.SetValue("RequirePassword", RequirePassword);
		valuesDictionary.SetValue("WEIntegrated", IsWorldEditIntegrated);
		valuesDictionary.SetValue("WEInterventionUnlimited", WEInterventionUnlimited);
		valuesDictionary.SetValue("WEInterventionLimit", WEInterventionLimit);
		valuesDictionary.SetValue("RegionPrivateLimit", RegionPrivateLimit);
		valuesDictionary.SetValue("RestrictWorldInteraction", RestrictWorldInteraction);
		valuesDictionary.SetValue("WEManagersOnly", WEManagersOnly);
		valuesDictionary.SetValue("RequiresLevelsForCrafting", RequiresLevelsForCrafting);
		valuesDictionary.SetValue("ImmutableWorldLevel", ImmutableWorldLevel);
		valuesDictionary.SetValue("LockWorldSettings", LockWorldSettings);
		valuesDictionary.SetValue("IsImmutableWorldLevelEnabled", IsImmutableWorldLevelEnabled);
		valuesDictionary.SetValue("MaxHealthMultiplier", MaxHealthMultiplier);
		valuesDictionary.SetValue("KeepInventoryAfterDeath", KeepInventoryAfterDeath);
		valuesDictionary.SetValue("PlayerSizeMultiplier", PlayerSizeMultiplier);
		valuesDictionary.SetValue("FluidsDoNotBreakBlocks", FluidsDoNotBreakBlocks);
		valuesDictionary.SetValue("FireSpreadEnabled", FireSpreadEnabled);
		valuesDictionary.SetValue("FallDamageEnabled", FallDamageEnabled);
		valuesDictionary.SetValue("ItemDurabilityMultiplier", ItemDurabilityMultiplier);
		valuesDictionary.SetValue("ExperienceGainMultiplier", ExperienceGainMultiplier);
		valuesDictionary.SetValue("ExplosionsDestroyTerrain", ExplosionsDestroyTerrain);
		valuesDictionary.SetValue("DisableNightPredatorSpam", DisableNightPredatorSpam);
		valuesDictionary.SetValue("FluidUpdateQuota", FluidUpdateQuota);
		valuesDictionary.SetValue("FluidSpreadDistance", FluidSpreadDistance);
		valuesDictionary.SetValue("LivesLimit", LivesLimit);
		valuesDictionary.SetValue("LifeRestorationMode", LifeRestoration);
		valuesDictionary.SetValue("BreathingMode", BreathingMode);
		valuesDictionary.SetValue("IsCraftingShuffleEnabled", IsCraftingShuffleEnabled);
		valuesDictionary.SetValue("DelayHeavyChunkLoading", DelayHeavyChunkLoading);
		if (!liveModifiableParametersOnly)
		{
			valuesDictionary.SetValue("WorldSeedString", Seed);
			valuesDictionary.SetValue("TerrainGenerationMode", TerrainGenerationMode);
			valuesDictionary.SetValue("IslandSize", IslandSize);
			valuesDictionary.SetValue("TerrainLevel", TerrainLevel);
			valuesDictionary.SetValue("ShoreRoughness", ShoreRoughness);
			valuesDictionary.SetValue("TerrainBlockIndex", TerrainBlockIndex);
			valuesDictionary.SetValue("TerrainOceanBlockIndex", TerrainOceanBlockIndex);
			valuesDictionary.SetValue("TemperatureOffset", TemperatureOffset);
			valuesDictionary.SetValue("HumidityOffset", HumidityOffset);
			valuesDictionary.SetValue("SeaLevelOffset", SeaLevelOffset);
			valuesDictionary.SetValue("BiomeSize", BiomeSize);
			valuesDictionary.SetValue("StartingPositionMode", StartingPositionMode);
		}
		valuesDictionary.SetValue("BlockTextureName", BlocksTextureName);
		valuesDictionary.SetValue("Palette", Palette.Save());
		valuesDictionary.SetValue("MaxOnlinePlayerCount", MaxOnlinePlayerCount);
		valuesDictionary.SetValue("DisableBlocks", DisableBlocks);
		valuesDictionary.SetValue("RandomSpawnPosition", RandomSpawnPosition);
	}

	public WorldSettings()
	{
		WorldGuid = Guid.NewGuid().ToString("N");
		Name = string.Empty;
		OriginalSerializationVersion = string.Empty;
		Seed = string.Empty;
		GameMode = GameMode.Challenging;
		MaxOnlinePlayerCount = 20;
		AreWeatherEffectsEnabled = true;
		IsAdventureRespawnAllowed = true;
		AreAdventureSurvivalMechanicsEnabled = true;
		AreSupernaturalCreaturesEnabled = true;
		IsFriendlyFireEnabled = true;
		IslandSize = new Vector2(400f, 400f);
		BiomeSize = 4f;
		TerrainLevel = 64;
		ShoreRoughness = 0.5f;
		TerrainBlockIndex = 8;
		TerrainOceanBlockIndex = 18;
		SeaLevelOffset = -1;
		Password = string.Empty;
		BlocksTextureName = string.Empty;
		Palette = new WorldPalette();
		DisableBlocks = string.Empty;
		RequirePassword = false;
		IsWorldEditIntegrated = false;
		WEInterventionUnlimited = true;
		WEInterventionLimit = 0;
		RegionPrivateLimit = 10;
		RestrictWorldInteraction = false;
		WEManagersOnly = false;
		DaySpeed = 1f;
		DayCycleDuration = 6400f;
		SeasonCycleDuration = 144000f;
		RecoverFator = 1f;
		RequiresLevelsForCrafting = true;
		ImmutableWorldLevel = 1;
		LockWorldSettings = false;
		IsImmutableWorldLevelEnabled = false;
		MaxHealthMultiplier = 1f;
		KeepInventoryAfterDeath = false;
		PlayerSizeMultiplier = 1f;
		FluidsDoNotBreakBlocks = false;
		FireSpreadEnabled = true;
		FallDamageEnabled = true;
		ItemDurabilityMultiplier = 1f;
		ExperienceGainMultiplier = 1f;
		ExplosionsDestroyTerrain = true;
		DisableNightPredatorSpam = true;
		FluidUpdateQuota = 4000;
		FluidSpreadDistance = 3;
		LivesLimit = 0;
		LifeRestoration = LifeRestorationMode.None;
		BreathingMode = BreathingMode.Air;
		IsCraftingShuffleEnabled = false;
		DelayHeavyChunkLoading = true;
	}

	public void EnforceEmbraceSettings()
	{
		TemperatureOffset = 0f;
		HumidityOffset = 0f;
		SeaLevelOffset = -10;
		BiomeSize = 4f;
		TerrainGenerationMode = TerrainGenerationMode.Continent;
		StartingPositionMode = StartingPositionMode.Hard;
		AreSupernaturalCreaturesEnabled = true;
		AreExplosionDropsEnabled = true;
		RequiresLevelsForCrafting = true;
		IsImmutableWorldLevelEnabled = false;
		LockWorldSettings = true;
		MaxHealthMultiplier = 1f;
		KeepInventoryAfterDeath = false;
		PlayerSizeMultiplier = 1f;
		FluidsDoNotBreakBlocks = false;
		FireSpreadEnabled = true;
		FallDamageEnabled = true;
		ExplosionsDestroyTerrain = true;
		ItemDurabilityMultiplier = 10f;
		ExperienceGainMultiplier = 0.1f;
		SeasonCycleDuration = 144000f;
		DayCycleDuration = 6400f;
		DisableNightPredatorSpam = false;
		LivesLimit = 7;
		LifeRestoration = LifeRestorationMode.Doubling;
		BreathingMode = BreathingMode.Air;
		IsCraftingShuffleEnabled = false;
	}

	public void EnforceStandardSurvivalSettings()
	{
		EnvironmentBehaviorMode = EnvironmentBehaviorMode.Living;
		TimeOfDayMode = TimeOfDayMode.Changing;
		AreWeatherEffectsEnabled = true;
		AreAdventureSurvivalMechanicsEnabled = true;
		IsAdventureRespawnAllowed = true;
		FluidsDoNotBreakBlocks = false;
		FireSpreadEnabled = true;
		FallDamageEnabled = true;
		ExplosionsDestroyTerrain = true;
		MaxHealthMultiplier = 1f;
		PlayerSizeMultiplier = 1f;
		ItemDurabilityMultiplier = 1f;
		ExperienceGainMultiplier = 1f;
		LockWorldSettings = true;
		DayCycleDuration = 6400f;
		DisableNightPredatorSpam = false;
		SeasonCycleDuration = 144000f;
	}

	public void EnforceCruelSettings()
	{
		EnforceBaseSurvival();
		LivesLimit = 1;
		StartingPositionMode = StartingPositionMode.Hard;
		LifeRestoration = LifeRestorationMode.None;
	}

	public void EnforceRealChallengingSettings()
	{
		EnforceBaseSurvival();
		LivesLimit = 0;
		StartingPositionMode = StartingPositionMode.Hard;
		LifeRestoration = LifeRestorationMode.None;
	}

	private void EnforceBaseSurvival()
	{
		EnvironmentBehaviorMode = EnvironmentBehaviorMode.Living;
		TimeOfDayMode = TimeOfDayMode.Changing;
		AreWeatherEffectsEnabled = true;
		AreAdventureSurvivalMechanicsEnabled = true;
		IsAdventureRespawnAllowed = true;
		FluidsDoNotBreakBlocks = false;
		FireSpreadEnabled = true;
		FallDamageEnabled = true;
		ExplosionsDestroyTerrain = true;
		MaxHealthMultiplier = 1f;
		PlayerSizeMultiplier = 1f;
		ItemDurabilityMultiplier = 1f;
		ExperienceGainMultiplier = 1f;
		LockWorldSettings = true;
		DayCycleDuration = 6400f;
		DisableNightPredatorSpam = false;
		SeasonCycleDuration = 144000f;
		BreathingMode = BreathingMode.Air;
		IsCraftingShuffleEnabled = false;
	}
}
