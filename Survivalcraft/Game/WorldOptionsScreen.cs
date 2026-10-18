using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public class WorldOptionsScreen : Screen
{
	public static string fName;

	public Widget m_newWorldOnlyPanel;

	public ButtonWidget m_terrainGenerationButton;

	public Widget m_continentTerrainPanel;

	public Widget m_islandTerrainPanel;

	public SliderWidget m_islandSizeEW;

	public SliderWidget m_islandSizeNS;

	public Widget m_flatTerrainPanel;

	public SliderWidget m_flatTerrainLevelSlider;

	public SliderWidget m_flatTerrainShoreRoughnessSlider;

	public BlockIconWidget m_flatTerrainBlock;

	public LabelWidget m_flatTerrainBlockLabel;

	public ButtonWidget m_flatTerrainBlockButton;

	public CheckboxWidget m_flatTerrainMagmaOceanCheckbox;

	public SliderWidget m_seaLevelOffsetSlider;

	public SliderWidget m_temperatureOffsetSlider;

	public SliderWidget m_humidityOffsetSlider;

	public SliderWidget m_biomeSizeSlider;

	public RectangleWidget m_blocksTextureIcon;

	public LabelWidget m_blocksTextureLabel;

	public LabelWidget m_blocksTextureDetails;

	public ButtonWidget m_blocksTextureButton;

	public ButtonWidget m_paletteButton;

	public ButtonWidget m_supernaturalCreaturesButton;

	public ButtonWidget m_friendlyFireButton;

	public Widget m_creativeModePanel;

	public ButtonWidget m_environmentBehaviorButton;

	public ButtonWidget m_timeOfDayButton;

	public ButtonWidget m_weatherEffectsButton;

	public ButtonWidget m_adventureRespawnButton;

	public ButtonWidget m_adventureSurvivalMechanicsButton;

	public LabelWidget m_descriptionLabel;

	public WorldSettings m_worldSettings;

	public bool m_isExistingWorld;

	public BlocksTexturesCache m_blockTexturesCache = new BlocksTexturesCache();

	public static float[] m_islandSizes;

	public static float[] m_biomeSizes;

	public ButtonWidget m_explosionDropsButton;

	public ButtonWidget m_requiresLevelsForCraftingButton;

	public SliderWidget m_immutableWorldLevelSlider;

	public ButtonWidget m_lockWorldSettingsButton;

	public SliderWidget m_maxHealthMultiplierSlider;

	public ButtonWidget m_keepInventoryAfterDeathButton;

	public SliderWidget m_playerSizeMultiplierSlider;

	public ButtonWidget m_fluidsDoNotBreakBlocksButton;

	public ButtonWidget m_fireSpreadEnabledButton;

	public ButtonWidget m_fallDamageEnabledButton;

	public SliderWidget m_itemDurabilityMultiplierSlider;

	public SliderWidget m_experienceGainMultiplierSlider;

	public ButtonWidget m_explosionsDestroyTerrainButton;

	public CheckboxWidget m_immutableWorldLevelCheckbox;

	public Widget m_immutableWorldLevelPanel;

	public SliderWidget m_dayDurationSlider;

	public SliderWidget m_seasonDurationSlider;

	public static int[] m_fluidUpdateQuotas;

	public ButtonWidget m_disableNightPredatorSpamButton;

	public SliderWidget m_fluidUpdateQuotaSlider;

	public SliderWidget m_livesLimitSlider;

	public SliderWidget m_lifeRestorationCostSlider;

	public Widget m_lifeRestorationPanel;

	public ButtonWidget m_breathingModeButton;

	public ButtonWidget m_craftingShuffleButton;

	private List<Tuple<Widget, Widget>> m_lockPairs = new List<Tuple<Widget, Widget>>();

	public SliderWidget m_fluidSpreadDistanceSlider;

	public ButtonWidget m_delayHeavyChunkLoadingButton;

	public WorldOptionsScreen()
	{
		XElement num = ContentManager.Get<XElement>("Screens/WorldOptionsScreen");
		LoadContents(this, num);
		m_creativeModePanel = Children.Find<Widget>("CreativeModePanel");
		m_newWorldOnlyPanel = Children.Find<Widget>("NewWorldOnlyPanel");
		m_continentTerrainPanel = Children.Find<Widget>("ContinentTerrainPanel");
		m_islandTerrainPanel = Children.Find<Widget>("IslandTerrainPanel");
		m_islandSizeNS = Children.Find<SliderWidget>("IslandSizeNS");
		m_islandSizeEW = Children.Find<SliderWidget>("IslandSizeEW");
		m_flatTerrainPanel = Children.Find<Widget>("FlatTerrainPanel");
		m_blocksTextureIcon = Children.Find<RectangleWidget>("BlocksTextureIcon");
		m_blocksTextureLabel = Children.Find<LabelWidget>("BlocksTextureLabel");
		m_blocksTextureDetails = Children.Find<LabelWidget>("BlocksTextureDetails");
		m_blocksTextureButton = Children.Find<ButtonWidget>("BlocksTextureButton");
		m_seaLevelOffsetSlider = Children.Find<SliderWidget>("SeaLevelOffset");
		m_temperatureOffsetSlider = Children.Find<SliderWidget>("TemperatureOffset");
		m_humidityOffsetSlider = Children.Find<SliderWidget>("HumidityOffset");
		m_biomeSizeSlider = Children.Find<SliderWidget>("BiomeSize");
		m_paletteButton = Children.Find<ButtonWidget>("Palette");
		m_supernaturalCreaturesButton = Children.Find<ButtonWidget>("SupernaturalCreatures");
		m_friendlyFireButton = Children.Find<ButtonWidget>("FriendlyFire");
		m_explosionDropsButton = Children.Find<ButtonWidget>("ExplosionDrops");
		m_environmentBehaviorButton = Children.Find<ButtonWidget>("EnvironmentBehavior");
		m_timeOfDayButton = Children.Find<ButtonWidget>("TimeOfDay");
		m_weatherEffectsButton = Children.Find<ButtonWidget>("WeatherEffects");
		m_adventureRespawnButton = Children.Find<ButtonWidget>("AdventureRespawn");
		m_adventureSurvivalMechanicsButton = Children.Find<ButtonWidget>("AdventureSurvivalMechanics");
		m_terrainGenerationButton = Children.Find<ButtonWidget>("TerrainGeneration");
		m_flatTerrainLevelSlider = Children.Find<SliderWidget>("FlatTerrainLevel");
		m_flatTerrainShoreRoughnessSlider = Children.Find<SliderWidget>("FlatTerrainShoreRoughness");
		m_flatTerrainBlock = Children.Find<BlockIconWidget>("FlatTerrainBlock");
		m_flatTerrainBlockLabel = Children.Find<LabelWidget>("FlatTerrainBlockLabel");
		m_flatTerrainBlockButton = Children.Find<ButtonWidget>("FlatTerrainBlockButton");
		m_flatTerrainMagmaOceanCheckbox = Children.Find<CheckboxWidget>("MagmaOcean");
		m_requiresLevelsForCraftingButton = Children.Find<ButtonWidget>("RequiresLevelsForCrafting");
		m_immutableWorldLevelCheckbox = Children.Find<CheckboxWidget>("ImmutableWorldLevelEnabled");
		m_immutableWorldLevelPanel = Children.Find<Widget>("ImmutableWorldLevelPanel");
		m_immutableWorldLevelSlider = Children.Find<SliderWidget>("ImmutableWorldLevel");
		m_lockWorldSettingsButton = Children.Find<ButtonWidget>("LockWorldSettings");
		m_maxHealthMultiplierSlider = Children.Find<SliderWidget>("MaxHealthMultiplier");
		m_keepInventoryAfterDeathButton = Children.Find<ButtonWidget>("KeepInventoryAfterDeath");
		m_playerSizeMultiplierSlider = Children.Find<SliderWidget>("PlayerSizeMultiplier");
		m_fluidsDoNotBreakBlocksButton = Children.Find<ButtonWidget>("FluidsDoNotBreakBlocks");
		m_fireSpreadEnabledButton = Children.Find<ButtonWidget>("FireSpreadEnabled");
		m_fallDamageEnabledButton = Children.Find<ButtonWidget>("FallDamageEnabled");
		m_itemDurabilityMultiplierSlider = Children.Find<SliderWidget>("ItemDurabilityMultiplier");
		m_experienceGainMultiplierSlider = Children.Find<SliderWidget>("ExperienceGainMultiplier");
		m_explosionsDestroyTerrainButton = Children.Find<ButtonWidget>("ExplosionsDestroyTerrain");
		m_descriptionLabel = Children.Find<LabelWidget>("Description");
		m_dayDurationSlider = Children.Find<SliderWidget>("DayDuration");
		m_seasonDurationSlider = Children.Find<SliderWidget>("SeasonDuration");
		m_disableNightPredatorSpamButton = Children.Find<ButtonWidget>("DisableNightPredatorSpam");
		m_fluidUpdateQuotaSlider = Children.Find<SliderWidget>("FluidUpdateQuota");
		m_fluidSpreadDistanceSlider = Children.Find<SliderWidget>("FluidSpreadDistance");
		m_livesLimitSlider = Children.Find<SliderWidget>("LivesLimit");
		m_lifeRestorationCostSlider = Children.Find<SliderWidget>("LifeRestorationCost");
		m_lifeRestorationPanel = Children.Find<Widget>("LifeRestorationPanel");
		m_breathingModeButton = Children.Find<ButtonWidget>("BreathingMode");
		m_craftingShuffleButton = Children.Find<ButtonWidget>("CraftingShuffle");
		m_delayHeavyChunkLoadingButton = Children.Find<ButtonWidget>("DelayHeavyChunkLoading");
		m_dayDurationSlider.MinValue = 3f;
		m_dayDurationSlider.MaxValue = 43200f;
		m_dayDurationSlider.Granularity = 1f;
		m_seasonDurationSlider.MinValue = 60f;
		m_seasonDurationSlider.MaxValue = 1440000f;
		m_seasonDurationSlider.Granularity = 10f;
		m_islandSizeEW.MinValue = 0f;
		m_islandSizeEW.MaxValue = m_islandSizes.Length - 1;
		m_islandSizeEW.Granularity = 1f;
		m_islandSizeNS.MinValue = 0f;
		m_islandSizeNS.MaxValue = m_islandSizes.Length - 1;
		m_islandSizeNS.Granularity = 1f;
		m_biomeSizeSlider.MinValue = 0f;
		m_biomeSizeSlider.MaxValue = m_biomeSizes.Length - 1;
		m_biomeSizeSlider.Granularity = 1f;
		m_fluidUpdateQuotaSlider.MinValue = 0f;
		m_fluidUpdateQuotaSlider.MaxValue = m_fluidUpdateQuotas.Length - 1;
		m_fluidUpdateQuotaSlider.Granularity = 1f;
		m_fluidSpreadDistanceSlider.MinValue = 1f;
		m_fluidSpreadDistanceSlider.MaxValue = 9f;
		m_fluidSpreadDistanceSlider.Granularity = 1f;
		m_livesLimitSlider.MinValue = 0f;
		m_livesLimitSlider.MaxValue = 100f;
		m_livesLimitSlider.Granularity = 1f;
		m_lifeRestorationCostSlider.MinValue = 0f;
		m_lifeRestorationCostSlider.MaxValue = 2f;
		m_lifeRestorationCostSlider.Granularity = 1f;
		FindLocksRecursive(this);
	}

	public static string FormatOffset(float value)
	{
		if (value != 0f)
		{
			return ((value >= 0f) ? "+" : "") + value;
		}
		return LanguageControl.Get(fName, 6);
	}

	public override void Enter(object[] parameters)
	{
		m_worldSettings = (WorldSettings)parameters[0];
		m_isExistingWorld = (bool)parameters[1];
		m_descriptionLabel.Text = StringsManager.GetString("EnvironmentBehaviorMode." + m_worldSettings.EnvironmentBehaviorMode.ToString() + ".Description");
		m_immutableWorldLevelCheckbox.IsChecked = m_worldSettings.IsImmutableWorldLevelEnabled;
		m_immutableWorldLevelPanel.IsVisible = m_worldSettings.IsImmutableWorldLevelEnabled;
		m_immutableWorldLevelSlider.Value = m_worldSettings.ImmutableWorldLevel;
		m_dayDurationSlider.Value = m_worldSettings.DayCycleDuration;
		m_seasonDurationSlider.Value = m_worldSettings.SeasonCycleDuration;
		m_dayDurationSlider.Text = FormatTime(m_worldSettings.DayCycleDuration);
		m_seasonDurationSlider.Text = FormatTime(m_worldSettings.SeasonCycleDuration);
		m_itemDurabilityMultiplierSlider.Value = m_worldSettings.ItemDurabilityMultiplier;
		m_experienceGainMultiplierSlider.Value = m_worldSettings.ExperienceGainMultiplier;
		m_fluidUpdateQuotaSlider.Value = FindNearestIndex(m_fluidUpdateQuotas, m_worldSettings.FluidUpdateQuota);
		m_fluidUpdateQuotaSlider.Text = m_worldSettings.FluidUpdateQuota.ToString();
		m_fluidSpreadDistanceSlider.Value = m_worldSettings.FluidSpreadDistance;
		m_fluidSpreadDistanceSlider.Text = string.Format("{0}x{0}x{0}", m_worldSettings.FluidSpreadDistance);
		m_livesLimitSlider.Value = m_worldSettings.LivesLimit;
		m_lifeRestorationCostSlider.Value = (float)m_worldSettings.LifeRestoration;
		m_breathingModeButton.Text = LanguageControl.Get("WorldOptionsScreen", "BreathingMode." + m_worldSettings.BreathingMode);
		m_craftingShuffleButton.Text = (m_worldSettings.IsCraftingShuffleEnabled ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		m_delayHeavyChunkLoadingButton.Text = (m_worldSettings.DelayHeavyChunkLoading ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
	}

	public override void Leave()
	{
		m_blockTexturesCache.Clear();
	}

	public override void Update()
	{
		bool flag = m_worldSettings.GameMode == GameMode.Cruel || m_worldSettings.GameMode == GameMode.RealChallenging || m_worldSettings.GameMode == GameMode.Embrace;
		if (flag)
		{
			m_worldSettings.TerrainGenerationMode = TerrainGenerationMode.Continent;
			m_worldSettings.TemperatureOffset = 0f;
			m_worldSettings.HumidityOffset = 0f;
			m_worldSettings.EnvironmentBehaviorMode = EnvironmentBehaviorMode.Living;
			m_worldSettings.TimeOfDayMode = TimeOfDayMode.Changing;
			m_worldSettings.AreWeatherEffectsEnabled = true;
			m_worldSettings.AreAdventureSurvivalMechanicsEnabled = true;
			m_worldSettings.IsAdventureRespawnAllowed = true;
			m_worldSettings.FluidsDoNotBreakBlocks = false;
			m_worldSettings.FireSpreadEnabled = true;
			m_worldSettings.FallDamageEnabled = true;
			m_worldSettings.ExplosionsDestroyTerrain = true;
			m_worldSettings.MaxHealthMultiplier = 1f;
			m_worldSettings.PlayerSizeMultiplier = 1f;
			m_worldSettings.LockWorldSettings = true;
			m_worldSettings.DisableNightPredatorSpam = false;
			m_worldSettings.BreathingMode = BreathingMode.Air;
			m_worldSettings.IsCraftingShuffleEnabled = false;
			m_worldSettings.IsFriendlyFireEnabled = true;
			m_worldSettings.AreSupernaturalCreaturesEnabled = true;
			if (m_worldSettings.GameMode == GameMode.Embrace)
			{
				m_worldSettings.ItemDurabilityMultiplier = 10f;
				m_worldSettings.ExperienceGainMultiplier = 0.1f;
				m_worldSettings.LivesLimit = 7;
				m_worldSettings.AreExplosionDropsEnabled = false;
				m_worldSettings.SeaLevelOffset = -10;
			}
			else
			{
				m_worldSettings.ItemDurabilityMultiplier = 1f;
				m_worldSettings.ExperienceGainMultiplier = 1f;
				m_worldSettings.AreExplosionDropsEnabled = true;
				m_worldSettings.SeaLevelOffset = 0;
				if (m_worldSettings.GameMode == GameMode.Cruel)
				{
					m_worldSettings.LivesLimit = 1;
				}
				else if (m_worldSettings.GameMode == GameMode.RealChallenging)
				{
					m_worldSettings.LivesLimit = 0;
				}
			}
		}
		m_creativeModePanel.IsVisible = true;
		if (m_terrainGenerationButton.IsClicked && !m_isExistingWorld)
		{
			IList<int> enumValues = EnumUtils.GetEnumValues(typeof(TerrainGenerationMode));
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(fName, 1), enumValues, 56f, (object e) => StringsManager.GetString("TerrainGenerationMode." + ((TerrainGenerationMode)e/*cast due to constrained. prefix*/).ToString() + ".Name"), (object e) =>
			{
				m_worldSettings.TerrainGenerationMode = (TerrainGenerationMode)e;
				m_descriptionLabel.Text = StringsManager.GetString("TerrainGenerationMode." + m_worldSettings.TerrainGenerationMode.ToString() + ".Description");
			}));
		}
		if (m_islandSizeEW.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.IslandSize.X = m_islandSizes[MathUtils.Clamp((int)m_islandSizeEW.Value, 0, m_islandSizes.Length - 1)];
		}
		if (m_islandSizeNS.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.IslandSize.Y = m_islandSizes[MathUtils.Clamp((int)m_islandSizeNS.Value, 0, m_islandSizes.Length - 1)];
		}
		if (m_flatTerrainLevelSlider.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.TerrainLevel = MathUtils.Clamp((int)m_flatTerrainLevelSlider.Value / (int)m_flatTerrainLevelSlider.Granularity * (int)m_flatTerrainLevelSlider.Granularity, 2, 252);
			m_descriptionLabel.Text = StringsManager.GetString("FlatTerrainLevel.Description");
		}
		if (m_flatTerrainShoreRoughnessSlider.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.ShoreRoughness = m_flatTerrainShoreRoughnessSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("FlatTerrainShoreRoughness.Description");
		}
		if (m_flatTerrainBlockButton.IsClicked && !m_isExistingWorld)
		{
			List<int> list = new List<int>();
			for (int num = 0; num < BlocksManager.Blocks.Length; num++)
			{
				if (BlocksManager.Blocks[num] != null)
				{
					list.Add(num);
				}
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(fName, 2), list, 72f, (object index) =>
			{
				XElement node = ContentManager.Get<XElement>("Widgets/SelectBlockItem");
				ContainerWidget containerWidget2 = (ContainerWidget)Widget.LoadWidget(null, node, null);
				int num4 = (int)index;
				containerWidget2.Children.Find<BlockIconWidget>("SelectBlockItem.Block").Contents = num4;
				Block block = BlocksManager.Blocks[num4];
				string text3 = ((block != null) ? block.GetDisplayName(null, Terrain.MakeBlockValue(num4)) : ("#" + num4));
				containerWidget2.Children.Find<LabelWidget>("SelectBlockItem.Text").Text = text3;
				return containerWidget2;
			}, (object index) =>
			{
				m_worldSettings.TerrainBlockIndex = (int)index;
			}));
		}
		if (m_flatTerrainMagmaOceanCheckbox.IsClicked)
		{
			m_worldSettings.TerrainOceanBlockIndex = ((m_worldSettings.TerrainOceanBlockIndex == 18) ? 92 : 18);
			m_descriptionLabel.Text = StringsManager.GetString("FlatTerrainMagmaOcean.Description");
		}
		if (m_seaLevelOffsetSlider.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.SeaLevelOffset = (int)m_seaLevelOffsetSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("SeaLevelOffset.Description");
		}
		if (m_temperatureOffsetSlider.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.TemperatureOffset = m_temperatureOffsetSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("TemperatureOffset.Description");
		}
		if (m_breathingModeButton.IsClicked)
		{
			IList<int> enumValues2 = EnumUtils.GetEnumValues(typeof(BreathingMode));
			m_worldSettings.BreathingMode = (BreathingMode)((enumValues2.IndexOf((int)m_worldSettings.BreathingMode) + 1) % enumValues2.Count);
			m_breathingModeButton.Text = LanguageControl.Get("WorldOptionsScreen", "BreathingMode." + m_worldSettings.BreathingMode);
			m_descriptionLabel.Text = StringsManager.GetString("BreathingMode." + m_worldSettings.BreathingMode.ToString() + ".Description");
		}
		if (m_craftingShuffleButton.IsClicked)
		{
			m_worldSettings.IsCraftingShuffleEnabled = !m_worldSettings.IsCraftingShuffleEnabled;
			m_craftingShuffleButton.Text = (m_worldSettings.IsCraftingShuffleEnabled ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
			m_descriptionLabel.Text = StringsManager.GetString("CraftingShuffle." + m_worldSettings.IsCraftingShuffleEnabled);
		}
		if (m_delayHeavyChunkLoadingButton.IsClicked)
		{
			m_worldSettings.DelayHeavyChunkLoading = !m_worldSettings.DelayHeavyChunkLoading;
			m_delayHeavyChunkLoadingButton.Text = (m_worldSettings.DelayHeavyChunkLoading ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
			m_descriptionLabel.Text = StringsManager.GetString("DelayHeavyChunkLoading." + m_worldSettings.DelayHeavyChunkLoading);
		}
		if (m_humidityOffsetSlider.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.HumidityOffset = m_humidityOffsetSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("HumidityOffset.Description");
		}
		if (m_biomeSizeSlider.IsSliding && !m_isExistingWorld)
		{
			m_worldSettings.BiomeSize = m_biomeSizes[MathUtils.Clamp((int)m_biomeSizeSlider.Value, 0, m_biomeSizes.Length - 1)];
			m_descriptionLabel.Text = StringsManager.GetString("BiomeSize.Description");
		}
		if (m_blocksTextureButton.IsClicked)
		{
			BlocksTexturesManager.UpdateBlocksTexturesList();
			ListSelectionDialog dialog = new ListSelectionDialog(LanguageControl.Get(fName, 3), BlocksTexturesManager.BlockTexturesNames, 64f, (object item) =>
			{
				XElement node = ContentManager.Get<XElement>("Widgets/BlocksTextureItem");
				ContainerWidget containerWidget2 = (ContainerWidget)Widget.LoadWidget(this, node, null);
				Texture2D texture2 = m_blockTexturesCache.GetTexture((string)item);
				containerWidget2.Children.Find<LabelWidget>("BlocksTextureItem.Text").Text = BlocksTexturesManager.GetDisplayName((string)item);
				containerWidget2.Children.Find<LabelWidget>("BlocksTextureItem.Details").Text = $"{texture2.Width}x{texture2.Height}";
				containerWidget2.Children.Find<RectangleWidget>("BlocksTextureItem.Icon").Subtexture = new Subtexture(texture2, Vector2.Zero, Vector2.One);
				return containerWidget2;
			}, (object item) =>
			{
				m_worldSettings.BlocksTextureName = (string)item;
			});
			DialogsManager.ShowDialog(null, dialog);
			m_descriptionLabel.Text = StringsManager.GetString("BlocksTexture.Description");
		}
		if (m_paletteButton.IsClicked)
		{
			DialogsManager.ShowDialog(null, new EditPaletteDialog(m_worldSettings.Palette));
		}
		if (m_supernaturalCreaturesButton.IsClicked)
		{
			m_worldSettings.AreSupernaturalCreaturesEnabled = !m_worldSettings.AreSupernaturalCreaturesEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("SupernaturalCreatures." + m_worldSettings.AreSupernaturalCreaturesEnabled);
		}
		if (m_friendlyFireButton.IsClicked)
		{
			m_worldSettings.IsFriendlyFireEnabled = !m_worldSettings.IsFriendlyFireEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("FriendlyFire." + m_worldSettings.IsFriendlyFireEnabled);
		}
		if (m_explosionDropsButton.IsClicked)
		{
			m_worldSettings.AreExplosionDropsEnabled = !m_worldSettings.AreExplosionDropsEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("ExplosionDropsMode." + m_worldSettings.AreExplosionDropsEnabled);
		}
		if (m_environmentBehaviorButton.IsClicked)
		{
			IList<int> enumValues3 = EnumUtils.GetEnumValues(typeof(EnvironmentBehaviorMode));
			m_worldSettings.EnvironmentBehaviorMode = (EnvironmentBehaviorMode)((enumValues3.IndexOf((int)m_worldSettings.EnvironmentBehaviorMode) + 1) % enumValues3.Count);
			m_descriptionLabel.Text = StringsManager.GetString("EnvironmentBehaviorMode." + m_worldSettings.EnvironmentBehaviorMode.ToString() + ".Description");
		}
		if (m_timeOfDayButton.IsClicked)
		{
			IList<int> enumValues4 = EnumUtils.GetEnumValues(typeof(TimeOfDayMode));
			m_worldSettings.TimeOfDayMode = (TimeOfDayMode)((enumValues4.IndexOf((int)m_worldSettings.TimeOfDayMode) + 1) % enumValues4.Count);
			m_descriptionLabel.Text = StringsManager.GetString("TimeOfDayMode." + m_worldSettings.TimeOfDayMode.ToString() + ".Description");
		}
		if (m_weatherEffectsButton.IsClicked)
		{
			m_worldSettings.AreWeatherEffectsEnabled = !m_worldSettings.AreWeatherEffectsEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("WeatherMode." + m_worldSettings.AreWeatherEffectsEnabled);
		}
		if (m_adventureRespawnButton.IsClicked)
		{
			m_worldSettings.IsAdventureRespawnAllowed = !m_worldSettings.IsAdventureRespawnAllowed;
			m_descriptionLabel.Text = StringsManager.GetString("AdventureRespawnMode." + m_worldSettings.IsAdventureRespawnAllowed);
		}
		if (m_adventureSurvivalMechanicsButton.IsClicked)
		{
			m_worldSettings.AreAdventureSurvivalMechanicsEnabled = !m_worldSettings.AreAdventureSurvivalMechanicsEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("AdventureSurvivalMechanics." + m_worldSettings.AreAdventureSurvivalMechanicsEnabled);
		}
		if (m_dayDurationSlider.IsSliding)
		{
			m_worldSettings.DayCycleDuration = m_dayDurationSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("DayCycleDuration.Description");
		}
		m_dayDurationSlider.Text = FormatTime(m_worldSettings.DayCycleDuration);
		if (m_seasonDurationSlider.IsSliding)
		{
			m_worldSettings.SeasonCycleDuration = m_seasonDurationSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("SeasonCycleDuration.Description");
		}
		m_seasonDurationSlider.Text = FormatTime(m_worldSettings.SeasonCycleDuration);
		m_newWorldOnlyPanel.IsVisible = !m_isExistingWorld;
		m_continentTerrainPanel.IsVisible = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Continent || m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatContinent;
		m_islandTerrainPanel.IsVisible = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.Island || m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatIsland;
		m_flatTerrainPanel.IsVisible = m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatContinent || m_worldSettings.TerrainGenerationMode == TerrainGenerationMode.FlatIsland;
		m_terrainGenerationButton.Text = StringsManager.GetString("TerrainGenerationMode." + m_worldSettings.TerrainGenerationMode.ToString() + ".Name");
		m_islandSizeEW.Value = FindNearestIndex(m_islandSizes, m_worldSettings.IslandSize.X);
		m_islandSizeEW.Text = m_worldSettings.IslandSize.X.ToString();
		m_islandSizeNS.Value = FindNearestIndex(m_islandSizes, m_worldSettings.IslandSize.Y);
		m_islandSizeNS.Text = m_worldSettings.IslandSize.Y.ToString();
		m_flatTerrainLevelSlider.Value = m_worldSettings.TerrainLevel;
		m_flatTerrainLevelSlider.Text = m_worldSettings.TerrainLevel.ToString();
		m_flatTerrainShoreRoughnessSlider.Value = m_worldSettings.ShoreRoughness;
		m_flatTerrainShoreRoughnessSlider.Text = $"{m_worldSettings.ShoreRoughness * 100f:0}%";
		m_flatTerrainBlock.Contents = m_worldSettings.TerrainBlockIndex;
		m_flatTerrainMagmaOceanCheckbox.IsChecked = m_worldSettings.TerrainOceanBlockIndex == 92;
		string text = ((BlocksManager.Blocks[m_worldSettings.TerrainBlockIndex] != null) ? BlocksManager.Blocks[m_worldSettings.TerrainBlockIndex].GetDisplayName(null, Terrain.MakeBlockValue(m_worldSettings.TerrainBlockIndex)) : string.Empty);
		m_flatTerrainBlockLabel.Text = ((text.Length > 10) ? (text.Substring(0, 10) + "...") : text);
		Texture2D texture = m_blockTexturesCache.GetTexture(m_worldSettings.BlocksTextureName);
		m_blocksTextureIcon.Subtexture = new Subtexture(texture, Vector2.Zero, Vector2.One);
		m_blocksTextureLabel.Text = BlocksTexturesManager.GetDisplayName(m_worldSettings.BlocksTextureName);
		m_blocksTextureDetails.Text = $"{texture.Width}x{texture.Height}";
		m_seaLevelOffsetSlider.Value = m_worldSettings.SeaLevelOffset;
		m_seaLevelOffsetSlider.Text = FormatOffset(m_worldSettings.SeaLevelOffset);
		m_temperatureOffsetSlider.Value = m_worldSettings.TemperatureOffset;
		m_temperatureOffsetSlider.Text = FormatOffset(m_worldSettings.TemperatureOffset);
		m_humidityOffsetSlider.Value = m_worldSettings.HumidityOffset;
		m_humidityOffsetSlider.Text = FormatOffset(m_worldSettings.HumidityOffset);
		m_biomeSizeSlider.Value = FindNearestIndex(m_biomeSizes, m_worldSettings.BiomeSize);
		m_biomeSizeSlider.Text = m_worldSettings.BiomeSize + "x";
		m_environmentBehaviorButton.Text = LanguageControl.Get("EnvironmentBehaviorMode", m_worldSettings.EnvironmentBehaviorMode.ToString());
		m_timeOfDayButton.Text = LanguageControl.Get("TimeOfDayMode", m_worldSettings.TimeOfDayMode.ToString());
		m_weatherEffectsButton.Text = (m_worldSettings.AreWeatherEffectsEnabled ? LanguageControl.Get("Usual", "enable") : LanguageControl.Get("Usual", "disable"));
		m_adventureRespawnButton.Text = (m_worldSettings.IsAdventureRespawnAllowed ? LanguageControl.Get("Usual", "allowed") : LanguageControl.Get("Usual", "not allowed"));
		m_adventureSurvivalMechanicsButton.Text = (m_worldSettings.AreAdventureSurvivalMechanicsEnabled ? LanguageControl.Get("Usual", "enable") : LanguageControl.Get("Usual", "disable"));
		m_supernaturalCreaturesButton.Text = (m_worldSettings.AreSupernaturalCreaturesEnabled ? LanguageControl.Get("Usual", "enable") : LanguageControl.Get("Usual", "disable"));
		m_explosionDropsButton.Text = (m_worldSettings.AreExplosionDropsEnabled ? LanguageControl.Get("Usual", "enable") : LanguageControl.Get("Usual", "disable"));
		m_friendlyFireButton.Text = (m_worldSettings.IsFriendlyFireEnabled ? LanguageControl.Get("Usual", "allowed") : LanguageControl.Get("Usual", "not allowed"));
		if (m_requiresLevelsForCraftingButton.IsClicked)
		{
			m_worldSettings.RequiresLevelsForCrafting = !m_worldSettings.RequiresLevelsForCrafting;
			m_descriptionLabel.Text = StringsManager.GetString("RequiresLevelsForCrafting." + m_worldSettings.RequiresLevelsForCrafting);
		}
		m_requiresLevelsForCraftingButton.Text = (m_worldSettings.RequiresLevelsForCrafting ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		m_worldSettings.IsImmutableWorldLevelEnabled = m_immutableWorldLevelCheckbox.IsChecked;
		m_immutableWorldLevelPanel.IsVisible = m_immutableWorldLevelCheckbox.IsChecked;
		if (m_immutableWorldLevelPanel.IsVisible)
		{
			if (m_immutableWorldLevelSlider.IsSliding)
			{
				m_worldSettings.ImmutableWorldLevel = (int)m_immutableWorldLevelSlider.Value;
				m_descriptionLabel.Text = StringsManager.GetString("ImmutableWorldLevel.Description");
			}
			m_immutableWorldLevelSlider.Text = m_worldSettings.ImmutableWorldLevel.ToString();
		}
		if (m_lockWorldSettingsButton.IsClicked)
		{
			if (m_worldSettings.LockWorldSettings)
			{
				m_worldSettings.LockWorldSettings = false;
				m_descriptionLabel.Text = StringsManager.GetString("LockWorldSettings.False");
			}
			else
			{
				MessageDialog messageDialog = new MessageDialog(LanguageControl.Get(fName, "LockWarningTitle"), LanguageControl.Get(fName, "LockWarningText"), LanguageControl.Get("Usual", "yes"), LanguageControl.Get("Usual", "no"), (MessageDialogButton button) =>
				{
					if (button == MessageDialogButton.Button1)
					{
						m_worldSettings.LockWorldSettings = true;
						m_lockWorldSettingsButton.Text = LanguageControl.Get("Usual", "yes");
						m_descriptionLabel.Text = StringsManager.GetString("LockWorldSettings.True");
					}
				});
				try
				{
					List<Widget> list2 = new List<Widget>();
					list2.Add(messageDialog);
					for (int num2 = 0; num2 < list2.Count; num2++)
					{
						Widget widget = list2[num2];
						if (widget is LabelWidget { Text: var text2 } labelWidget && text2 == LanguageControl.Get(fName, "LockWarningText"))
						{
							labelWidget.Color = Color.Red;
						}
						if (!(widget is ContainerWidget containerWidget))
						{
							continue;
						}
						foreach (Widget child in containerWidget.Children)
						{
							list2.Add(child);
						}
					}
				}
				catch
				{
				}
				DialogsManager.ShowDialog(null, messageDialog);
			}
		}
		m_lockWorldSettingsButton.Text = (m_worldSettings.LockWorldSettings ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (m_maxHealthMultiplierSlider.IsSliding)
		{
			m_worldSettings.MaxHealthMultiplier = m_maxHealthMultiplierSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("MaxHealthMultiplier.Description");
		}
		m_maxHealthMultiplierSlider.Text = $"{m_worldSettings.MaxHealthMultiplier:0.0}x";
		if (m_keepInventoryAfterDeathButton.IsClicked)
		{
			m_worldSettings.KeepInventoryAfterDeath = !m_worldSettings.KeepInventoryAfterDeath;
			m_descriptionLabel.Text = StringsManager.GetString("KeepInventoryAfterDeath." + m_worldSettings.KeepInventoryAfterDeath);
		}
		m_keepInventoryAfterDeathButton.Text = (m_worldSettings.KeepInventoryAfterDeath ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (m_playerSizeMultiplierSlider.IsSliding)
		{
			m_worldSettings.PlayerSizeMultiplier = m_playerSizeMultiplierSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("PlayerSizeMultiplier.Description");
		}
		m_playerSizeMultiplierSlider.Text = $"{m_worldSettings.PlayerSizeMultiplier:0.00}x";
		if (m_fluidsDoNotBreakBlocksButton.IsClicked)
		{
			m_worldSettings.FluidsDoNotBreakBlocks = !m_worldSettings.FluidsDoNotBreakBlocks;
			m_descriptionLabel.Text = StringsManager.GetString("FluidsDoNotBreakBlocks." + m_worldSettings.FluidsDoNotBreakBlocks);
		}
		m_fluidsDoNotBreakBlocksButton.Text = (m_worldSettings.FluidsDoNotBreakBlocks ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (m_fireSpreadEnabledButton.IsClicked)
		{
			m_worldSettings.FireSpreadEnabled = !m_worldSettings.FireSpreadEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("FireSpreadEnabled." + m_worldSettings.FireSpreadEnabled);
		}
		m_fireSpreadEnabledButton.Text = (m_worldSettings.FireSpreadEnabled ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (m_fallDamageEnabledButton.IsClicked)
		{
			m_worldSettings.FallDamageEnabled = !m_worldSettings.FallDamageEnabled;
			m_descriptionLabel.Text = StringsManager.GetString("FallDamageEnabled." + m_worldSettings.FallDamageEnabled);
		}
		m_fallDamageEnabledButton.Text = (m_worldSettings.FallDamageEnabled ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (m_itemDurabilityMultiplierSlider.IsSliding)
		{
			m_worldSettings.ItemDurabilityMultiplier = m_itemDurabilityMultiplierSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("ItemDurabilityMultiplier.Description");
		}
		m_itemDurabilityMultiplierSlider.Text = $"{m_worldSettings.ItemDurabilityMultiplier:0.0}x";
		if (m_experienceGainMultiplierSlider.IsSliding)
		{
			m_worldSettings.ExperienceGainMultiplier = m_experienceGainMultiplierSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("ExperienceGainMultiplier.Description");
		}
		m_experienceGainMultiplierSlider.Text = $"{m_worldSettings.ExperienceGainMultiplier:0.0}x";
		if (m_explosionsDestroyTerrainButton.IsClicked)
		{
			m_worldSettings.ExplosionsDestroyTerrain = !m_worldSettings.ExplosionsDestroyTerrain;
			m_descriptionLabel.Text = StringsManager.GetString("ExplosionsDestroyTerrain." + m_worldSettings.ExplosionsDestroyTerrain);
		}
		m_explosionsDestroyTerrainButton.Text = (m_worldSettings.ExplosionsDestroyTerrain ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
		}
		if (m_disableNightPredatorSpamButton.IsClicked)
		{
			m_worldSettings.DisableNightPredatorSpam = !m_worldSettings.DisableNightPredatorSpam;
			m_descriptionLabel.Text = StringsManager.GetString("DisableNightPredatorSpam." + m_worldSettings.DisableNightPredatorSpam);
		}
		m_disableNightPredatorSpamButton.Text = (m_worldSettings.DisableNightPredatorSpam ? LanguageControl.Get("Usual", "yes") : LanguageControl.Get("Usual", "no"));
		if (m_fluidUpdateQuotaSlider.IsSliding)
		{
			int num3 = MathUtils.Clamp((int)m_fluidUpdateQuotaSlider.Value, 0, m_fluidUpdateQuotas.Length - 1);
			m_worldSettings.FluidUpdateQuota = m_fluidUpdateQuotas[num3];
			m_descriptionLabel.Text = StringsManager.GetString("FluidUpdateQuota.Description");
		}
		m_fluidUpdateQuotaSlider.Text = m_worldSettings.FluidUpdateQuota.ToString();
		if (m_fluidSpreadDistanceSlider.IsSliding)
		{
			m_worldSettings.FluidSpreadDistance = (int)m_fluidSpreadDistanceSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("FluidSpreadDistance.Description");
		}
		m_fluidSpreadDistanceSlider.Text = string.Format("{0}x{0}x{0}", m_worldSettings.FluidSpreadDistance);
		bool flag2 = (!m_isExistingWorld || !m_worldSettings.LockWorldSettings) && !flag;
		m_lockWorldSettingsButton.IsEnabled = flag2;
		m_environmentBehaviorButton.IsEnabled = flag2;
		m_timeOfDayButton.IsEnabled = flag2;
		m_weatherEffectsButton.IsEnabled = flag2;
		m_adventureRespawnButton.IsEnabled = flag2;
		m_adventureSurvivalMechanicsButton.IsEnabled = flag2;
		m_supernaturalCreaturesButton.IsEnabled = flag2;
		m_friendlyFireButton.IsEnabled = flag2;
		m_explosionDropsButton.IsEnabled = flag2;
		m_explosionsDestroyTerrainButton.IsEnabled = flag2;
		m_fireSpreadEnabledButton.IsEnabled = flag2;
		m_fallDamageEnabledButton.IsEnabled = flag2;
		m_fluidsDoNotBreakBlocksButton.IsEnabled = flag2;
		m_keepInventoryAfterDeathButton.IsEnabled = flag2;
		m_requiresLevelsForCraftingButton.IsEnabled = flag2;
		m_immutableWorldLevelCheckbox.IsEnabled = flag2;
		m_immutableWorldLevelSlider.IsEnabled = flag2;
		m_maxHealthMultiplierSlider.IsEnabled = flag2;
		m_playerSizeMultiplierSlider.IsEnabled = flag2;
		m_itemDurabilityMultiplierSlider.IsEnabled = flag2;
		m_experienceGainMultiplierSlider.IsEnabled = flag2;
		m_dayDurationSlider.IsEnabled = flag2;
		m_seasonDurationSlider.IsEnabled = flag2;
		bool flag3 = m_worldSettings.GameMode == GameMode.Cruel || m_worldSettings.GameMode == GameMode.RealChallenging || m_worldSettings.GameMode == GameMode.Embrace;
		m_breathingModeButton.IsEnabled = flag2 && !flag3;
		m_craftingShuffleButton.IsEnabled = flag2 && !flag3;
		m_disableNightPredatorSpamButton.IsEnabled = flag2 && !flag3;
		m_fluidUpdateQuotaSlider.IsEnabled = true;
		m_fluidSpreadDistanceSlider.IsEnabled = true;
		if (m_worldSettings.GameMode == GameMode.Cruel)
		{
			m_worldSettings.LivesLimit = 1;
			m_worldSettings.LifeRestoration = LifeRestorationMode.None;
			m_livesLimitSlider.IsEnabled = false;
			m_lifeRestorationCostSlider.IsEnabled = false;
		}
		else if (m_worldSettings.GameMode == GameMode.Embrace)
		{
			m_worldSettings.LivesLimit = 7;
			m_worldSettings.LifeRestoration = LifeRestorationMode.Doubling;
			m_livesLimitSlider.IsEnabled = false;
			m_lifeRestorationCostSlider.IsEnabled = false;
		}
		else
		{
			m_livesLimitSlider.IsEnabled = flag2;
			m_lifeRestorationCostSlider.IsEnabled = flag2 && m_worldSettings.LivesLimit > 0;
		}
		if (m_livesLimitSlider.IsEnabled && m_livesLimitSlider.IsSliding)
		{
			m_worldSettings.LivesLimit = (int)m_livesLimitSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("LivesLimit.Description");
		}
		m_lifeRestorationPanel.IsVisible = m_worldSettings.LivesLimit > 0;
		if (m_worldSettings.LivesLimit == 0)
		{
			m_livesLimitSlider.Text = LanguageControl.Get("WorldOptionsScreen", "Unlimited");
		}
		else
		{
			m_livesLimitSlider.Text = m_worldSettings.LivesLimit.ToString();
		}
		m_lifeRestorationCostSlider.IsVisible = m_worldSettings.LivesLimit > 0;
		if (!m_lifeRestorationPanel.IsVisible)
		{
			return;
		}
		if (m_lifeRestorationCostSlider.IsEnabled && m_lifeRestorationCostSlider.IsSliding)
		{
			m_worldSettings.LifeRestoration = (LifeRestorationMode)m_lifeRestorationCostSlider.Value;
			m_descriptionLabel.Text = StringsManager.GetString("LifeRestorationMode.Description");
		}
		m_lifeRestorationCostSlider.Value = (float)m_worldSettings.LifeRestoration;
		switch (m_worldSettings.LifeRestoration)
		{
		case LifeRestorationMode.None:
			m_lifeRestorationCostSlider.Text = LanguageControl.Get("WorldOptionsScreen", "RestorationNone");
			break;
		case LifeRestorationMode.OneLevel:
			m_lifeRestorationCostSlider.Text = LanguageControl.Get("WorldOptionsScreen", "RestorationOneLevel");
			break;
		case LifeRestorationMode.Doubling:
			m_lifeRestorationCostSlider.Text = LanguageControl.Get("WorldOptionsScreen", "RestorationDoubling");
			break;
		}
		foreach (Tuple<Widget, Widget> lockPair in m_lockPairs)
		{
			lockPair.Item2.IsVisible = !lockPair.Item1.IsEnabled && lockPair.Item1.IsVisible;
		}
	}

	public static int FindNearestIndex(IList<float> list, float v)
	{
		int matrix = 0;
		for (int vector = 0; vector < list.Count; vector++)
		{
			if (MathUtils.Abs(list[vector] - v) < MathUtils.Abs(list[matrix] - v))
			{
				matrix = vector;
			}
		}
		return matrix;
	}

	static WorldOptionsScreen()
	{
		m_fluidUpdateQuotas = new int[12]
		{
			0, 100, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000,
			9000, 10000
		};
		fName = "WorldOptionsScreen";
		m_islandSizes = new float[20]
		{
			30f, 40f, 50f, 60f, 80f, 100f, 120f, 150f, 200f, 250f,
			300f, 400f, 500f, 600f, 800f, 1000f, 1200f, 1500f, 2000f, 2500f
		};
		m_biomeSizes = new float[9] { 0.25f, 0.33f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f, 4f };
	}

	public static string FormatTime(float seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		if (timeSpan.TotalHours >= 1.0)
		{
			return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
		}
		if (timeSpan.TotalMinutes >= 1.0)
		{
			return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
		}
		return $"{timeSpan.Seconds}s";
	}

	public static int FindNearestIndex(IList<int> list, int v)
	{
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (MathUtils.Abs((float)list[i] - (float)v) < MathUtils.Abs((float)list[num] - (float)v))
			{
				num = i;
			}
		}
		return num;
	}

	private void FindLocksRecursive(Widget widget)
	{
		if (widget is RectangleWidget rectangleWidget && !string.IsNullOrEmpty(rectangleWidget.Name) && rectangleWidget.Name.StartsWith("Lock."))
		{
			string name = rectangleWidget.Name.Substring(5);
			Widget widget2 = Children.Find<Widget>(name);
			if (widget2 != null)
			{
				m_lockPairs.Add(new Tuple<Widget, Widget>(widget2, rectangleWidget));
			}
		}
		if (!(widget is ContainerWidget containerWidget))
		{
			return;
		}
		foreach (Widget child in containerWidget.Children)
		{
			FindLocksRecursive(child);
		}
	}
}
