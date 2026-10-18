using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class SettingsGraphicsScreen : Screen
{
	public BevelledButtonWidget m_virtualRealityButton;

	public SliderWidget m_brightnessSlider;

	public ContainerWidget m_vrPanel;

	public BevelledButtonWidget m_themeButton;

	public BevelledButtonWidget m_shaderButton;

	public SliderWidget m_precipitationSlider;

	public ButtonWidget m_randomVegetationRotationButton;

	public ButtonWidget m_randomNaturalBlocksRotationButton;

	private ButtonWidget m_blockHighlightButton;

	private ButtonWidget m_nightVisionButton;

	private LabelWidget m_nightVisionInfoLabel;

	public Screen m_previousScreen;

	private ButtonWidget m_visionDefectsButton;

	public ButtonWidget m_vegetationModeButton;

	private ButtonWidget m_skyLightUseParticlesButton;

	public BevelledButtonWidget m_customThemeButton;

	public Widget m_customThemeContainer;

	public SliderWidget m_customThemeAlphaSlider;

	public SettingsGraphicsScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/SettingsGraphicsScreen");
		LoadContents(this, node);
		m_virtualRealityButton = Children.Find<BevelledButtonWidget>("VirtualRealityButton");
		m_brightnessSlider = Children.Find<SliderWidget>("BrightnessSlider");
		m_vrPanel = Children.Find<ContainerWidget>("VrPanel");
		m_vrPanel.IsVisible = false;
		m_themeButton = Children.Find<BevelledButtonWidget>("ThemeButton");
		m_customThemeButton = Children.Find<BevelledButtonWidget>("CustomThemeColorButton");
		m_customThemeAlphaSlider = Children.Find<SliderWidget>("CustomThemeAlphaSlider");
		m_customThemeContainer = Children.Find<Widget>("CustomThemeContainer");
		m_precipitationSlider = Children.Find<SliderWidget>("PrecipitationSlider");
		m_randomVegetationRotationButton = Children.Find<ButtonWidget>("RandomVegetationRotationButton");
		m_randomNaturalBlocksRotationButton = Children.Find<ButtonWidget>("RandomNaturalBlocksRotationButton");
		m_blockHighlightButton = Children.Find<ButtonWidget>("BlockHighlightButton");
		m_nightVisionButton = Children.Find<ButtonWidget>("NightVisionButton");
		m_nightVisionInfoLabel = Children.Find<LabelWidget>("NightVisionInfoLabel");
		m_shaderButton = Children.Find<BevelledButtonWidget>("ShaderButton");
		m_visionDefectsButton = Children.Find<ButtonWidget>("VisionDefectsButton");
		m_skyLightUseParticlesButton = Children.Find<ButtonWidget>("SkyLightUseParticlesButton");
		m_vegetationModeButton = Children.Find<ButtonWidget>("VegetationModeButton");
	}

	public override void Update()
	{
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(m_previousScreen);
			m_previousScreen = null;
			return;
		}
		if (m_virtualRealityButton.IsClicked)
		{
			if (SettingsManager.UseVr)
			{
				SettingsManager.UseVr = false;
				VrManager.StopVr();
			}
			else
			{
				SettingsManager.UseVr = true;
				VrManager.StartVr();
			}
		}
		if (m_brightnessSlider.IsSliding)
		{
			SettingsManager.Brightness = m_brightnessSlider.Value;
		}
		if (m_precipitationSlider.IsSliding)
		{
			SettingsManager.StagePrecipitation = m_precipitationSlider.Value;
		}
		if (m_blockHighlightButton.IsClicked)
		{
			SettingsManager.ShowBlockHighlight = !SettingsManager.ShowBlockHighlight;
		}
		m_blockHighlightButton.Text = (SettingsManager.ShowBlockHighlight ? LanguageControl.Yes : LanguageControl.No);
		m_precipitationSlider.Value = SettingsManager.StagePrecipitation;
		float num = MathUtils.Round(SettingsManager.StagePrecipitation * 100f);
		if (MathUtils.Abs(SettingsManager.StagePrecipitation - 0.3f) < 0.05f)
		{
			m_precipitationSlider.Text = LanguageControl.Get("SettingsGraphicsScreen", "PrecipitationNormal");
		}
		else
		{
			m_precipitationSlider.Text = num + "%";
		}
		if (m_themeButton.IsClicked)
		{
			IList<int> viewportSize = EnumUtils.GetEnumValues(typeof(ThemeMode));
			SettingsManager.ThemeMode = (ThemeMode)((viewportSize.IndexOf((int)SettingsManager.ThemeMode) + 1) % viewportSize.Count);
		}
		m_themeButton.Text = LanguageControl.Get("ThemeMode", SettingsManager.ThemeMode.ToString());
		bool color = SettingsManager.ThemeMode == ThemeMode.Custom;
		m_customThemeContainer.IsVisible = color;
		if (color)
		{
			m_customThemeButton.DisableThemeColors = true;
			m_customThemeButton.CenterColor = SettingsManager.CustomInterfaceColor;
			m_customThemeButton.Text = "Edit Color";
			if (m_customThemeButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new EditColorDialog(SettingsManager.CustomInterfaceColor, (Color? result) =>
				{
					if (result.HasValue)
					{
						SettingsManager.CustomInterfaceColor = result.Value;
					}
				}));
			}
			if (m_customThemeAlphaSlider.IsSliding)
			{
				Color p = SettingsManager.CustomInterfaceColor;
				byte vector2 = (byte)(m_customThemeAlphaSlider.Value * 255f);
				SettingsManager.CustomInterfaceColor = new Color(p.R, p.G, p.B, vector2);
			}
			else
			{
				m_customThemeAlphaSlider.Value = (float)(int)SettingsManager.CustomInterfaceColor.A / 255f;
			}
			m_customThemeAlphaSlider.Text = MathUtils.Round(m_customThemeAlphaSlider.Value * 100f) + "%";
		}
		if (m_randomVegetationRotationButton.IsClicked)
		{
			SettingsManager.RandomVegetationRotation = !SettingsManager.RandomVegetationRotation;
		}
		m_randomVegetationRotationButton.Text = (SettingsManager.RandomVegetationRotation ? LanguageControl.Yes : LanguageControl.No);
		if (m_randomNaturalBlocksRotationButton.IsClicked)
		{
			SettingsManager.RandomNaturalBlocksRotation = !SettingsManager.RandomNaturalBlocksRotation;
		}
		if (m_nightVisionButton.IsClicked)
		{
			SettingsManager.NightVisionEnabled = !SettingsManager.NightVisionEnabled;
		}
		if (m_visionDefectsButton.IsClicked)
		{
			SettingsManager.VisionDefectsEnabled = !SettingsManager.VisionDefectsEnabled;
		}
		m_visionDefectsButton.Text = (SettingsManager.VisionDefectsEnabled ? LanguageControl.Yes : LanguageControl.No);
		if (m_skyLightUseParticlesButton.IsClicked)
		{
			SettingsManager.SkyLightUseParticles = !SettingsManager.SkyLightUseParticles;
		}
		m_skyLightUseParticlesButton.Text = (SettingsManager.SkyLightUseParticles ? LanguageControl.Yes : LanguageControl.No);
		m_nightVisionButton.Text = (SettingsManager.NightVisionEnabled ? LanguageControl.Yes : LanguageControl.No);
		m_randomNaturalBlocksRotationButton.Text = (SettingsManager.RandomNaturalBlocksRotation ? LanguageControl.Yes : LanguageControl.No);
		m_virtualRealityButton.IsEnabled = VrManager.IsVrAvailable;
		m_virtualRealityButton.Text = (SettingsManager.UseVr ? "Enabled" : "Disabled");
		m_brightnessSlider.Value = SettingsManager.Brightness;
		m_brightnessSlider.Text = MathUtils.Round(SettingsManager.Brightness * 10f).ToString();
		m_shaderButton.Text = LanguageControl.Get("SettingsGraphicsScreen", "ShadersButton");
		if (m_shaderButton.IsClicked)
		{
			ScreensManager.SwitchScreen(new ShaderSettingsScreen());
		}
		if (m_vegetationModeButton == null)
		{
			return;
		}
		if (m_vegetationModeButton.IsClicked)
		{
			SettingsManager.VegetationRenderMode[] modes = (SettingsManager.VegetationRenderMode[])Enum.GetValues(typeof(SettingsManager.VegetationRenderMode));
			List<string> options = new List<string>(modes.Length);
			for (int num2 = 0; num2 < modes.Length; num2++)
			{
				options.Add(LanguageControl.Get("VegetationRenderMode", modes[num2].ToString()));
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get("SettingsGraphicsScreen", "VegetationTitle"), options, 60f, (object item) => item.ToString(), (object item) =>
			{
				int num3 = options.IndexOf(item.ToString());
				if (num3 >= 0)
				{
					SettingsManager.VegetationMode = modes[num3];
				}
			}));
		}
		m_vegetationModeButton.Text = LanguageControl.Get("VegetationRenderMode", SettingsManager.VegetationMode.ToString());
	}

	public override void Enter(object[] parameters)
	{
		if (m_previousScreen == null)
		{
			m_previousScreen = ScreensManager.PreviousScreen;
		}
	}
}
