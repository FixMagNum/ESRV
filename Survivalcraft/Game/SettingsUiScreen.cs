using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using Engine;

namespace Game;

public class SettingsUiScreen : Screen
{
	public ContainerWidget m_windowModeContainer;

	public ButtonWidget m_windowModeButton;

	public ButtonWidget m_languageButton;

	public ButtonWidget m_displayLogButton;

	public SliderWidget m_uiScaleSlider;

	public ButtonWidget m_upsideDownButton;

	public ButtonWidget m_hideMoveLookPadsButton;

	public ButtonWidget m_showGuiInScreenshotsButton;

	public ButtonWidget m_showLogoInScreenshotsButton;

	public ButtonWidget m_screenshotSizeButton;

	public ButtonWidget m_communityContentModeButton;

	public static string fName;

	public ButtonWidget m_toggleFirstPersonCameraButton;

	private SliderWidget m_uiTransparentSlider;

	public ButtonWidget m_uiDimmingButton;

	public ButtonWidget m_buttonsStyleButton;

	public ButtonWidget m_panoramaModeButton;

	public SettingsUiScreen()
	{
		XElement areSupernaturalCreaturesEnabled = ContentManager.Get<XElement>("Screens/SettingsUiScreen");
		LoadContents(this, areSupernaturalCreaturesEnabled);
		m_windowModeContainer = Children.Find<ContainerWidget>("WindowModeContainer");
		m_languageButton = Children.Find<ButtonWidget>("LanguageButton");
		m_displayLogButton = Children.Find<ButtonWidget>("DisplayLogButton");
		m_windowModeButton = Children.Find<ButtonWidget>("WindowModeButton");
		m_uiScaleSlider = Children.Find<SliderWidget>("UIScaleSlider");
		m_uiTransparentSlider = Children.Find<SliderWidget>("UITransparentSlider");
		m_upsideDownButton = Children.Find<ButtonWidget>("UpsideDownButton");
		m_hideMoveLookPadsButton = Children.Find<ButtonWidget>("HideMoveLookPads");
		m_showGuiInScreenshotsButton = Children.Find<ButtonWidget>("ShowGuiInScreenshotsButton");
		m_showLogoInScreenshotsButton = Children.Find<ButtonWidget>("ShowLogoInScreenshotsButton");
		m_screenshotSizeButton = Children.Find<ButtonWidget>("ScreenshotSizeButton");
		m_communityContentModeButton = Children.Find<ButtonWidget>("CommunityContentModeButton");
		m_toggleFirstPersonCameraButton = Children.Find<ButtonWidget>("ToggleFirstPersonCameraButton");
		m_uiDimmingButton = Children.Find<ButtonWidget>("UiDimmingButton");
		m_buttonsStyleButton = Children.Find<ButtonWidget>("ButtonsStyleButton");
		m_panoramaModeButton = Children.Find<ButtonWidget>("PanoramaModeButton");
	}

	public override void Enter(object[] parameters)
	{
		m_windowModeContainer.IsVisible = true;
	}

	public override void Update()
	{
		if (m_windowModeButton.IsClicked)
		{
			WindowMode[] array = (WindowMode[])Enum.GetValues(typeof(WindowMode));
			SettingsManager.WindowMode = array[(Array.IndexOf(array, SettingsManager.WindowMode) + 1) % array.Length];
		}
		if (m_uiScaleSlider.SlidingCompleted)
		{
			SettingsManager.UIScale = m_uiScaleSlider.Value;
		}
		if (m_uiTransparentSlider.IsSliding)
		{
			SettingsManager.UITransparent = MathUtils.Saturate(m_uiTransparentSlider.Value);
		}
		if (!m_uiTransparentSlider.IsSliding)
		{
			m_uiTransparentSlider.Value = SettingsManager.UITransparent;
		}
		float uITransparent = SettingsManager.UITransparent;
		if (MathUtils.Abs(uITransparent - 0.3f) < 0.05f)
		{
			m_uiTransparentSlider.Text = LanguageControl.Get("SettingsUiScreenUI", "UITransparentNormal");
		}
		else
		{
			m_uiTransparentSlider.Text = $"{uITransparent * 100f:0}%";
		}
		if (m_buttonsStyleButton != null && m_buttonsStyleButton.IsClicked)
		{
			ButtonStyleMode[] enumValues = (ButtonStyleMode[])Enum.GetValues(typeof(ButtonStyleMode));
			List<string> options = new List<string>(enumValues.Length);
			ButtonStyleMode[] array2 = enumValues;
			for (int i = 0; i < array2.Length; i++)
			{
				ButtonStyleMode buttonStyleMode = array2[i];
				options.Add(LanguageControl.Get("ButtonStyleMode", buttonStyleMode.ToString()));
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get("ButtonsStyleTitle", "ButtonsStyleTitle"), options, 60f, (object item) => item.ToString(), (object item) =>
			{
				int num = options.IndexOf(item.ToString());
				if (num >= 0)
				{
					SettingsManager.ButtonStyleMode = enumValues[num];
				}
			}));
		}
		if (m_languageButton.IsClicked)
		{
			List<string> languageTypes = LanguageControl.LanguageTypes;
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get("Language", "Name"), languageTypes, 60f, (object item) =>
			{
				string text = item.ToString();
				try
				{
					CultureInfo cultureInfo = new CultureInfo(text.Replace("_", "-"));
					return cultureInfo.TextInfo.ToTitleCase(cultureInfo.NativeName) + " (" + text + ")";
				}
				catch
				{
					return text;
				}
			}, (object item) =>
			{
				LanguageControl.Initialize(item.ToString());
				foreach (ModEntity mod in ModsManager.ModList)
				{
					mod.LoadLauguage();
				}
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				foreach (KeyValuePair<string, Screen> screen in ScreensManager.m_screens)
				{
					object value = Activator.CreateInstance(screen.Value.GetType());
					dictionary.Add(screen.Key, value);
				}
				foreach (KeyValuePair<string, object> dictItem in dictionary)
				{
					ScreensManager.m_screens[dictItem.Key] = dictItem.Value as Screen;
				}
				CraftingRecipesManager.Initialize();
				ScreensManager.SwitchScreen("MainMenu");
			}));
		}
		if (m_displayLogButton.IsClicked)
		{
			SettingsManager.DisplayLog = !SettingsManager.DisplayLog;
		}
		if (!m_uiScaleSlider.IsSliding)
		{
			m_uiScaleSlider.Value = SettingsManager.UIScale;
		}
		m_uiScaleSlider.Text = $"{m_uiScaleSlider.Value * 100f:0}%";
		if (m_upsideDownButton.IsClicked)
		{
			SettingsManager.UpsideDownLayout = !SettingsManager.UpsideDownLayout;
		}
		if (m_hideMoveLookPadsButton.IsClicked)
		{
			SettingsManager.HideMoveLookPads = !SettingsManager.HideMoveLookPads;
		}
		if (m_showGuiInScreenshotsButton.IsClicked)
		{
			SettingsManager.ShowGuiInScreenshots = !SettingsManager.ShowGuiInScreenshots;
		}
		if (m_showLogoInScreenshotsButton.IsClicked)
		{
			SettingsManager.ShowLogoInScreenshots = !SettingsManager.ShowLogoInScreenshots;
		}
		if (m_screenshotSizeButton.IsClicked)
		{
			ScreenshotSize[] array3 = (ScreenshotSize[])Enum.GetValues(typeof(ScreenshotSize));
			SettingsManager.ScreenshotSize = array3[(Array.IndexOf(array3, SettingsManager.ScreenshotSize) + 1) % array3.Length];
		}
		if (m_communityContentModeButton.IsClicked)
		{
			CommunityContentMode[] array4 = (CommunityContentMode[])Enum.GetValues(typeof(CommunityContentMode));
			SettingsManager.CommunityContentMode = array4[(Array.IndexOf(array4, SettingsManager.CommunityContentMode) + 1) % array4.Length];
		}
		if (m_uiDimmingButton != null && m_uiDimmingButton.IsClicked)
		{
			SettingsManager.UiDimmingEnabled = !SettingsManager.UiDimmingEnabled;
		}
		if (m_panoramaModeButton != null && m_panoramaModeButton.IsClicked)
		{
			SettingsManager.NewPanoramaEnabled = !SettingsManager.NewPanoramaEnabled;
		}
		m_windowModeButton.Text = LanguageControl.Get("WindowMode", SettingsManager.WindowMode.ToString());
		m_languageButton.Text = LanguageControl.Get("Language", "Name");
		if (m_buttonsStyleButton != null)
		{
			m_buttonsStyleButton.Text = LanguageControl.Get("ButtonStyleMode", SettingsManager.ButtonStyleMode.ToString());
		}
		if (m_toggleFirstPersonCameraButton.IsClicked)
		{
			SettingsManager.FirstPersonCameraEnabled = !SettingsManager.FirstPersonCameraEnabled;
		}
		m_toggleFirstPersonCameraButton.Text = (SettingsManager.FirstPersonCameraEnabled ? LanguageControl.Yes : LanguageControl.No);
		m_displayLogButton.Text = (SettingsManager.DisplayLog ? LanguageControl.Yes : LanguageControl.No);
		m_upsideDownButton.Text = (SettingsManager.UpsideDownLayout ? LanguageControl.Yes : LanguageControl.No);
		m_hideMoveLookPadsButton.Text = (SettingsManager.HideMoveLookPads ? LanguageControl.Yes : LanguageControl.No);
		m_showGuiInScreenshotsButton.Text = (SettingsManager.ShowGuiInScreenshots ? LanguageControl.Yes : LanguageControl.No);
		m_showLogoInScreenshotsButton.Text = (SettingsManager.ShowLogoInScreenshots ? LanguageControl.Yes : LanguageControl.No);
		m_screenshotSizeButton.Text = LanguageControl.Get("ScreenshotSize", SettingsManager.ScreenshotSize.ToString());
		m_communityContentModeButton.Text = LanguageControl.Get("CommunityContentMode", SettingsManager.CommunityContentMode.ToString());
		if (m_uiDimmingButton != null)
		{
			m_uiDimmingButton.Text = (SettingsManager.UiDimmingEnabled ? LanguageControl.Yes : LanguageControl.No);
		}
		if (m_panoramaModeButton != null)
		{
			m_panoramaModeButton.Text = (SettingsManager.NewPanoramaEnabled ? LanguageControl.Yes : LanguageControl.No);
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
		}
	}

	static SettingsUiScreen()
	{
		fName = "SettingsUiScreen";
	}
}
