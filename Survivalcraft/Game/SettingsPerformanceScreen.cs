using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Game.NetWork;

namespace Game;

public class SettingsPerformanceScreen : Screen
{
	public static List<int> m_presentationIntervals;

	public static List<int> m_visibilityRanges;

	public ButtonWidget m_resolutionButton;

	public SliderWidget m_visibilityRangeSlider;

	public LabelWidget m_visibilityRangeWarningLabel;

	public ButtonWidget m_viewAnglesButton;

	public ButtonWidget m_terrainMipmapsButton;

	public ButtonWidget m_skyRenderingModeButton;

	public ButtonWidget m_objectShadowsButton;

	public SliderWidget m_framerateLimitSlider;

	public ButtonWidget m_displayFpsCounterButton;

	public ButtonWidget m_displayFpsRibbonButton;

	public int m_enterVisibilityRange;

	public string fName;

	public ButtonWidget m_fireParticlesFurnacesButton;

	public ButtonWidget m_fireParticlesLightsButton;

	public ButtonWidget m_fireParticlesCampfireButton;

	public ButtonWidget m_vomitParticlesButton;

	public ButtonWidget m_cottonParticlesButton;

	public ButtonWidget m_puddleParticlesButton;

	public ButtonWidget m_campfireSmokeButton;

	public ButtonWidget m_simplifySmokePhysicsButton;

	public SliderWidget m_smokeDensitySlider;

	public ButtonWidget m_blockAnimationsButton;

	public ButtonWidget m_weatherPrecipitationOutsideViewButton;

	public ButtonWidget m_fogButton;

	public ButtonWidget m_blockBreakParticlesButton;

	public ButtonWidget m_blockDebris3DButton;

	public ButtonWidget m_displayPaintParticlesButton;

	public ButtonWidget m_precisePrecipitationHitsButton;

	public ButtonWidget m_displayBloodParticlesButton;

	public ButtonWidget m_cycloneFlybyDebrisButton;

	public ButtonWidget m_tornadoDustButton;

	public ButtonWidget m_tornadoDebrisButton;

	public ButtonWidget m_windParticlesButton;

	public SliderWidget m_distantChunksSlider;

	public LabelWidget m_distantChunksWarningLabel;

	public ButtonWidget m_distantChunksDetailButton;

	public LabelWidget m_distantChunksDetailLabel;

	public static List<int> m_distantRanges;

	public ButtonWidget m_lowResSignsButton;

	public ButtonWidget m_displayWaterParticlesButton;

	public ButtonWidget m_displayMagmaParticlesButton;

	public SettingsPerformanceScreen()
	{
		fName = GetType().Name;
		XElement stringBuilder = ContentManager.Get<XElement>("Screens/SettingsPerformanceScreen");
		LoadContents(this, stringBuilder);
		m_resolutionButton = Children.Find<ButtonWidget>("ResolutionButton");
		m_visibilityRangeSlider = Children.Find<SliderWidget>("VisibilityRangeSlider");
		m_visibilityRangeWarningLabel = Children.Find<LabelWidget>("VisibilityRangeWarningLabel");
		m_viewAnglesButton = Children.Find<ButtonWidget>("ViewAnglesButton");
		m_terrainMipmapsButton = Children.Find<ButtonWidget>("TerrainMipmapsButton");
		m_skyRenderingModeButton = Children.Find<ButtonWidget>("SkyRenderingModeButton");
		m_objectShadowsButton = Children.Find<ButtonWidget>("ObjectShadowsButton");
		m_framerateLimitSlider = Children.Find<SliderWidget>("FramerateLimitSlider");
		m_displayFpsCounterButton = Children.Find<ButtonWidget>("DisplayFpsCounterButton");
		m_displayFpsRibbonButton = Children.Find<ButtonWidget>("DisplayFpsRibbonButton");
		m_visibilityRangeSlider.MinValue = 0f;
		m_visibilityRangeSlider.MaxValue = m_visibilityRanges.Count - 1;
		m_fireParticlesFurnacesButton = Children.Find<ButtonWidget>("FireParticlesFurnacesButton");
		m_fireParticlesLightsButton = Children.Find<ButtonWidget>("FireParticlesLightsButton");
		m_fireParticlesCampfireButton = Children.Find<ButtonWidget>("FireParticlesCampfireButton");
		m_vomitParticlesButton = Children.Find<ButtonWidget>("VomitParticlesButton");
		m_cottonParticlesButton = Children.Find<ButtonWidget>("CottonParticlesButton");
		m_puddleParticlesButton = Children.Find<ButtonWidget>("PuddleParticlesButton");
		m_campfireSmokeButton = Children.Find<ButtonWidget>("CampfireSmokeButton");
		m_lowResSignsButton = Children.Find<ButtonWidget>("LowResSignsButton");
		m_simplifySmokePhysicsButton = Children.Find<ButtonWidget>("SimplifySmokePhysicsButton");
		m_smokeDensitySlider = Children.Find<SliderWidget>("SmokeDensitySlider");
		m_blockAnimationsButton = Children.Find<ButtonWidget>("BlockAnimationsButton");
		m_weatherPrecipitationOutsideViewButton = Children.Find<ButtonWidget>("WeatherPrecipitationOutsideViewButton");
		m_fogButton = Children.Find<ButtonWidget>("FogButton");
		m_blockBreakParticlesButton = Children.Find<ButtonWidget>("BlockBreakParticlesButton");
		m_blockDebris3DButton = Children.Find<ButtonWidget>("BlockDebris3DButton");
		m_displayPaintParticlesButton = Children.Find<ButtonWidget>("DisplayPaintParticlesButton");
		m_precisePrecipitationHitsButton = Children.Find<ButtonWidget>("PrecisePrecipitationHitsButton");
		m_displayBloodParticlesButton = Children.Find<ButtonWidget>("BloodParticleButton");
		m_cycloneFlybyDebrisButton = Children.Find<ButtonWidget>("CycloneFlybyDebrisButton");
		m_tornadoDustButton = Children.Find<ButtonWidget>("TornadoDustButton");
		m_tornadoDebrisButton = Children.Find<ButtonWidget>("TornadoDebrisButton");
		m_windParticlesButton = Children.Find<ButtonWidget>("WindParticlesButton");
		m_distantChunksSlider = Children.Find<SliderWidget>("DistantChunksSlider");
		m_distantChunksWarningLabel = Children.Find<LabelWidget>("DistantChunksWarningLabel");
		m_distantChunksDetailButton = Children.Find<ButtonWidget>("DistantChunksDetailButton");
		m_distantChunksDetailLabel = Children.Find<LabelWidget>("DistantChunksDetailLabel");
		m_displayWaterParticlesButton = Children.Find<ButtonWidget>("WaterParticlesButton", throwIfNotFound: false);
		m_displayMagmaParticlesButton = Children.Find<ButtonWidget>("MagmaParticlesButton", throwIfNotFound: false);
		m_distantChunksSlider.MinValue = 0f;
		m_distantChunksSlider.MaxValue = 64f;
		m_smokeDensitySlider.MinValue = 0f;
		m_smokeDensitySlider.MaxValue = 100f;
	}

	public override void Enter(object[] parameters)
	{
		m_enterVisibilityRange = SettingsManager.VisibilityRange;
		if (CommonLib.WorkType == WorkType.Client)
		{
			m_visibilityRangeSlider.MaxValue = m_visibilityRanges.Count;
		}
	}

	public override void Update()
	{
		if (m_resolutionButton.IsClicked)
		{
			IList<int> texturedBatch2D2 = EnumUtils.GetEnumValues(typeof(ResolutionMode));
			SettingsManager.ResolutionMode = (ResolutionMode)((texturedBatch2D2.IndexOf((int)SettingsManager.ResolutionMode) + 1) % texturedBatch2D2.Count);
		}
		if (m_visibilityRangeSlider.IsSliding)
		{
			SettingsManager.VisibilityRange = m_visibilityRanges[MathUtils.Clamp((int)m_visibilityRangeSlider.Value, 0, m_visibilityRanges.Count - 1)];
		}
		if (m_distantChunksDetailButton.IsClicked)
		{
			SettingsManager.DistantChunksDetailLevel = (SettingsManager.DistantChunksDetailLevel + 1) % 5;
		}
		string[] viewportSize = new string[5] { "0", "1x", "2x", "4x", "Max" };
		m_distantChunksDetailButton.Text = viewportSize[SettingsManager.DistantChunksDetailLevel];
		if (m_distantChunksSlider.IsSliding)
		{
			SettingsManager.DistantChunksRange = m_distantRanges[MathUtils.Clamp((int)m_distantChunksSlider.Value, 0, m_distantRanges.Count - 1)];
		}
		m_distantChunksSlider.MaxValue = m_distantRanges.Count - 1;
		m_distantChunksSlider.Value = m_distantRanges.IndexOf(SettingsManager.DistantChunksRange);
		m_distantChunksSlider.Text = ((SettingsManager.DistantChunksRange > 0) ? $"+{SettingsManager.DistantChunksRange} blk" : LanguageControl.Get("SettingsPerformanceScreen", "off"));
		UpdateDistantChunksHelpTexts();
		if (m_viewAnglesButton.IsClicked)
		{
			IList<int> start = EnumUtils.GetEnumValues(typeof(ViewAngleMode));
			SettingsManager.ViewAngleMode = (ViewAngleMode)((start.IndexOf((int)SettingsManager.ViewAngleMode) + 1) % start.Count);
		}
		if (m_terrainMipmapsButton.IsClicked)
		{
			SettingsManager.TerrainMipmapsEnabled = !SettingsManager.TerrainMipmapsEnabled;
		}
		if (m_skyRenderingModeButton.IsClicked)
		{
			IList<int> i = EnumUtils.GetEnumValues(typeof(SkyRenderingMode));
			SettingsManager.SkyRenderingMode = (SkyRenderingMode)((i.IndexOf((int)SettingsManager.SkyRenderingMode) + 1) % i.Count);
		}
		if (m_objectShadowsButton.IsClicked)
		{
			SettingsManager.ObjectsShadowsEnabled = !SettingsManager.ObjectsShadowsEnabled;
		}
		if (m_framerateLimitSlider.IsSliding)
		{
			SettingsManager.PresentationInterval = m_presentationIntervals[MathUtils.Clamp((int)m_framerateLimitSlider.Value, 0, m_presentationIntervals.Count - 1)];
		}
		if (m_displayFpsCounterButton.IsClicked)
		{
			SettingsManager.DisplayFpsCounter = !SettingsManager.DisplayFpsCounter;
		}
		if (m_displayFpsRibbonButton.IsClicked)
		{
			SettingsManager.DisplayFpsRibbon = !SettingsManager.DisplayFpsRibbon;
		}
		m_resolutionButton.Text = LanguageControl.Get("ResolutionMode", SettingsManager.ResolutionMode.ToString());
		m_visibilityRangeSlider.Value = ((m_visibilityRanges.IndexOf(SettingsManager.VisibilityRange) >= 0) ? m_visibilityRanges.IndexOf(SettingsManager.VisibilityRange) : 64);
		m_visibilityRangeSlider.Text = string.Format(LanguageControl.Get(fName, 1), SettingsManager.VisibilityRange);
		if (SettingsManager.VisibilityRange <= 48)
		{
			m_visibilityRangeWarningLabel.IsVisible = true;
			m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 2);
		}
		else if (SettingsManager.VisibilityRange <= 64)
		{
			m_visibilityRangeWarningLabel.IsVisible = false;
		}
		else if (SettingsManager.VisibilityRange <= 112)
		{
			m_visibilityRangeWarningLabel.IsVisible = true;
			m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 3);
		}
		else if (SettingsManager.VisibilityRange <= 224)
		{
			m_visibilityRangeWarningLabel.IsVisible = true;
			m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 4);
		}
		else if (SettingsManager.VisibilityRange <= 384)
		{
			m_visibilityRangeWarningLabel.IsVisible = true;
			m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 5);
		}
		else if (SettingsManager.VisibilityRange <= 512)
		{
			m_visibilityRangeWarningLabel.IsVisible = true;
			m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 6);
		}
		else
		{
			m_visibilityRangeWarningLabel.IsVisible = true;
			m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 7);
		}
		m_viewAnglesButton.Text = LanguageControl.Get("ViewAngleMode", SettingsManager.ViewAngleMode.ToString());
		m_terrainMipmapsButton.Text = (SettingsManager.TerrainMipmapsEnabled ? LanguageControl.Enable : LanguageControl.Disable);
		m_skyRenderingModeButton.Text = LanguageControl.Get("SkyRenderingMode", SettingsManager.SkyRenderingMode.ToString());
		m_objectShadowsButton.Text = (SettingsManager.ObjectsShadowsEnabled ? LanguageControl.Enable : LanguageControl.Disable);
		m_framerateLimitSlider.Value = ((m_presentationIntervals.IndexOf(SettingsManager.PresentationInterval) >= 0) ? m_presentationIntervals.IndexOf(SettingsManager.PresentationInterval) : (m_presentationIntervals.Count - 1));
		m_framerateLimitSlider.Text = ((SettingsManager.PresentationInterval != 0) ? string.Format(LanguageControl.Get(fName, 8), SettingsManager.PresentationInterval) : LanguageControl.Get(fName, 9));
		m_displayFpsCounterButton.Text = (SettingsManager.DisplayFpsCounter ? LanguageControl.Yes : LanguageControl.No);
		m_displayFpsRibbonButton.Text = (SettingsManager.DisplayFpsRibbon ? LanguageControl.Yes : LanguageControl.No);
		if (m_fireParticlesFurnacesButton.IsClicked)
		{
			SettingsManager.FireParticlesInFurnaces = !SettingsManager.FireParticlesInFurnaces;
		}
		m_fireParticlesFurnacesButton.Text = (SettingsManager.FireParticlesInFurnaces ? LanguageControl.Yes : LanguageControl.No);
		if (m_fireParticlesLightsButton.IsClicked)
		{
			SettingsManager.FireParticlesFromLights = !SettingsManager.FireParticlesFromLights;
		}
		m_fireParticlesLightsButton.Text = (SettingsManager.FireParticlesFromLights ? LanguageControl.Yes : LanguageControl.No);
		if (m_fireParticlesCampfireButton.IsClicked)
		{
			SettingsManager.FireParticlesFromCampfires = !SettingsManager.FireParticlesFromCampfires;
		}
		m_fireParticlesCampfireButton.Text = (SettingsManager.FireParticlesFromCampfires ? LanguageControl.Yes : LanguageControl.No);
		if (m_vomitParticlesButton.IsClicked)
		{
			SettingsManager.VomitParticles = !SettingsManager.VomitParticles;
		}
		m_vomitParticlesButton.Text = (SettingsManager.VomitParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_cottonParticlesButton.IsClicked)
		{
			SettingsManager.CottonBlockParticles = !SettingsManager.CottonBlockParticles;
		}
		m_cottonParticlesButton.Text = (SettingsManager.CottonBlockParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_puddleParticlesButton.IsClicked)
		{
			SettingsManager.PuddleBlockParticles = !SettingsManager.PuddleBlockParticles;
		}
		m_puddleParticlesButton.Text = (SettingsManager.PuddleBlockParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_campfireSmokeButton.IsClicked)
		{
			SettingsManager.SmokeFromBothCampfires = !SettingsManager.SmokeFromBothCampfires;
		}
		m_campfireSmokeButton.Text = (SettingsManager.SmokeFromBothCampfires ? LanguageControl.Yes : LanguageControl.No);
		if (m_simplifySmokePhysicsButton.IsClicked)
		{
			SettingsManager.SimplifySmokePhysics = !SettingsManager.SimplifySmokePhysics;
		}
		m_simplifySmokePhysicsButton.Text = (SettingsManager.SimplifySmokePhysics ? LanguageControl.Yes : LanguageControl.No);
		m_displayBloodParticlesButton.Text = (SettingsManager.DisplayBloodParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_displayBloodParticlesButton.IsClicked)
		{
			SettingsManager.DisplayBloodParticles = !SettingsManager.DisplayBloodParticles;
		}
		m_displayBloodParticlesButton.Text = (SettingsManager.DisplayBloodParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_smokeDensitySlider.IsSliding)
		{
			SettingsManager.SmokeDensity = MathUtils.Clamp((int)m_smokeDensitySlider.Value, 0, 100);
		}
		m_smokeDensitySlider.Value = SettingsManager.SmokeDensity;
		int texturedBatch2D3 = (int)MathUtils.Round(m_smokeDensitySlider.Value);
		if (texturedBatch2D3 == 50)
		{
			m_smokeDensitySlider.Text = LanguageControl.Get("SettingsPerformanceScreen", "SmokeDensityNormal");
		}
		else
		{
			m_smokeDensitySlider.Text = texturedBatch2D3.ToString();
		}
		if (m_blockAnimationsButton.IsClicked)
		{
			SettingsManager.BlockAnimationsEnabled = !SettingsManager.BlockAnimationsEnabled;
		}
		m_blockAnimationsButton.Text = (SettingsManager.BlockAnimationsEnabled ? LanguageControl.Yes : LanguageControl.No);
		if (m_weatherPrecipitationOutsideViewButton.IsClicked)
		{
			SettingsManager.RenderWeatherPrecipitationOutsideView = !SettingsManager.RenderWeatherPrecipitationOutsideView;
		}
		m_weatherPrecipitationOutsideViewButton.Text = (SettingsManager.RenderWeatherPrecipitationOutsideView ? LanguageControl.Yes : LanguageControl.No);
		if (m_fogButton.IsClicked)
		{
			SettingsManager.RenderFog = !SettingsManager.RenderFog;
		}
		m_fogButton.Text = (SettingsManager.RenderFog ? LanguageControl.Yes : LanguageControl.No);
		if (m_blockBreakParticlesButton.IsClicked)
		{
			SettingsManager.BlockBreakParticles = !SettingsManager.BlockBreakParticles;
		}
		if (m_blockDebris3DButton.IsClicked)
		{
			SettingsManager.BlockDebris3D = !SettingsManager.BlockDebris3D;
		}
		m_blockDebris3DButton.Text = (SettingsManager.BlockDebris3D ? LanguageControl.Yes : LanguageControl.No);
		if (m_lowResSignsButton != null)
		{
			if (m_lowResSignsButton.IsClicked)
			{
				SettingsManager.LowResSignsTexture = !SettingsManager.LowResSignsTexture;
			}
			m_lowResSignsButton.Text = (SettingsManager.LowResSignsTexture ? LanguageControl.Yes : LanguageControl.No);
		}
		if (m_displayPaintParticlesButton.IsClicked)
		{
			SettingsManager.DisplayPaintParticles = !SettingsManager.DisplayPaintParticles;
		}
		m_displayPaintParticlesButton.Text = (SettingsManager.DisplayPaintParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_precisePrecipitationHitsButton.IsClicked)
		{
			SettingsManager.PrecisePrecipitationHits = !SettingsManager.PrecisePrecipitationHits;
		}
		m_precisePrecipitationHitsButton.Text = (SettingsManager.PrecisePrecipitationHits ? LanguageControl.Yes : LanguageControl.No);
		m_blockBreakParticlesButton.Text = (SettingsManager.BlockBreakParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_cycloneFlybyDebrisButton.IsClicked)
		{
			SettingsManager.DisableCycloneFlybyDebris = !SettingsManager.DisableCycloneFlybyDebris;
		}
		m_cycloneFlybyDebrisButton.Text = (SettingsManager.DisableCycloneFlybyDebris ? LanguageControl.Yes : LanguageControl.No);
		if (m_tornadoDustButton.IsClicked)
		{
			SettingsManager.DisableTornadoDust = !SettingsManager.DisableTornadoDust;
		}
		m_tornadoDustButton.Text = (SettingsManager.DisableTornadoDust ? LanguageControl.Yes : LanguageControl.No);
		if (m_tornadoDebrisButton.IsClicked)
		{
			SettingsManager.DisableTornadoDebris = !SettingsManager.DisableTornadoDebris;
		}
		m_tornadoDebrisButton.Text = (SettingsManager.DisableTornadoDebris ? LanguageControl.Yes : LanguageControl.No);
		if (m_windParticlesButton.IsClicked)
		{
			SettingsManager.DisplayWindParticles = !SettingsManager.DisplayWindParticles;
		}
		m_windParticlesButton.Text = (SettingsManager.DisplayWindParticles ? LanguageControl.Yes : LanguageControl.No);
		if (m_displayWaterParticlesButton != null)
		{
			if (m_displayWaterParticlesButton.IsClicked)
			{
				SettingsManager.DisplayWaterParticles = !SettingsManager.DisplayWaterParticles;
			}
			m_displayWaterParticlesButton.Text = (SettingsManager.DisplayWaterParticles ? LanguageControl.Yes : LanguageControl.No);
		}
		if (m_displayMagmaParticlesButton != null)
		{
			if (m_displayMagmaParticlesButton.IsClicked)
			{
				SettingsManager.DisplayMagmaParticles = !SettingsManager.DisplayMagmaParticles;
			}
			m_displayMagmaParticlesButton.Text = (SettingsManager.DisplayMagmaParticles ? LanguageControl.Yes : LanguageControl.No);
		}
		if (!Input.Back && !Input.Cancel && !Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			return;
		}
		bool count = SettingsManager.VisibilityRange > 128;
		if ((SettingsManager.VisibilityRange > m_enterVisibilityRange) & count)
		{
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 10), LanguageControl.Get(fName, 11), LanguageControl.Ok, LanguageControl.Back, (MessageDialogButton button) =>
			{
				if (button == MessageDialogButton.Button1)
				{
					ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
				}
			}));
		}
		else
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
		}
	}

	static SettingsPerformanceScreen()
	{
		m_presentationIntervals = new List<int> { 2, 1, 0 };
		m_distantRanges = new List<int> { 0, 256, 512, 1024, 1536, 2048, 3072, 4096, 6144, 8192 };
		m_visibilityRanges = new List<int>
		{
			0, 10, 32, 48, 64, 80, 96, 112, 128, 160,
			192, 224, 256, 320, 384, 448, 512, 576, 640, 704,
			768, 832, 896, 960, 1024
		};
	}

	private void UpdateDistantChunksHelpTexts()
	{
		if (SettingsManager.DistantChunksRange == 0)
		{
			m_distantChunksWarningLabel.Text = LanguageControl.Get("SettingsPerformanceScreen", "distant_base_help");
		}
		else
		{
			float num2 = (new float[5] { 1f, 1.5f, 2.5f, 5f, 10f })[MathUtils.Clamp(SettingsManager.DistantChunksDetailLevel, 0, 4)];
			float num3 = (float)SettingsManager.DistantChunksRange / 128f;
			long value = (long)(num3 * num3 * 0.5f * num2);
			value = MathUtils.Max(value, 1L);
			string format = LanguageControl.Get("SettingsPerformanceScreen", "distant_ram_help");
			m_distantChunksWarningLabel.Text = string.Format(format, value);
		}
		string num4 = "distant_det_" + SettingsManager.DistantChunksDetailLevel;
		m_distantChunksDetailLabel.Text = LanguageControl.Get("SettingsPerformanceScreen", num4);
	}
}
