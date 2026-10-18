using System.Xml.Linq;

namespace Game;

public class SettingsCompatibilityScreen : Screen
{
	public ButtonWidget m_singlethreadedTerrainUpdateButton;

	public ButtonWidget m_useAudioTrackCachingButton;

	public ContainerWidget m_disableAudioTrackCachingContainer;

	public ButtonWidget m_useReducedZRangeButton;

	public ContainerWidget m_useReducedZRangeContainer;

	public ButtonWidget m_viewGameLogButton;

	public ButtonWidget m_resetDefaultsButton;

	public ButtonWidget m_enableModButton;

	public LabelWidget m_descriptionLabel;

	public UniformSpacingPanelWidget AudioCrack;

	public ButtonWidget m_translationProviderButton;

	public SettingsCompatibilityScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/SettingsCompatibilityScreen");
		LoadContents(this, node);
		m_singlethreadedTerrainUpdateButton = Children.Find<ButtonWidget>("SinglethreadedTerrainUpdateButton");
		m_useAudioTrackCachingButton = Children.Find<ButtonWidget>("UseAudioTrackCachingButton");
		m_disableAudioTrackCachingContainer = Children.Find<ContainerWidget>("DisableAudioTrackCachingContainer");
		m_useReducedZRangeButton = Children.Find<ButtonWidget>("UseReducedZRangeButton");
		m_enableModButton = Children.Find<ButtonWidget>("EnableMod");
		m_useReducedZRangeContainer = Children.Find<ContainerWidget>("UseReducedZRangeContainer");
		m_viewGameLogButton = Children.Find<ButtonWidget>("ViewGameLogButton");
		m_translationProviderButton = Children.Find<ButtonWidget>("TranslationProviderButton");
		m_resetDefaultsButton = Children.Find<ButtonWidget>("ResetDefaultsButton");
		m_descriptionLabel = Children.Find<LabelWidget>("Description");
		AudioCrack = Children.Find<UniformSpacingPanelWidget>("DisableAudioTrackCachingContainer");
	}

	public override void Enter(object[] parameters)
	{
		m_descriptionLabel.Text = string.Empty;
		AudioCrack.IsVisible = false;
		m_useAudioTrackCachingButton.IsVisible = false;
		m_useReducedZRangeContainer.IsVisible = false;
	}

	public override void Update()
	{
		if (m_singlethreadedTerrainUpdateButton.IsClicked)
		{
			SettingsManager.MultithreadedTerrainUpdate = !SettingsManager.MultithreadedTerrainUpdate;
			m_descriptionLabel.Text = StringsManager.GetString("Settings.Compatibility.SinglethreadedTerrainUpdate.Description");
		}
		if (m_useReducedZRangeButton.IsClicked)
		{
			SettingsManager.UseReducedZRange = !SettingsManager.UseReducedZRange;
			m_descriptionLabel.Text = StringsManager.GetString("Settings.Compatibility.UseReducedZRange.Description");
		}
		if (m_enableModButton.IsClicked)
		{
			SettingsManager.EnableMod = !SettingsManager.EnableMod;
		}
		if (m_viewGameLogButton.IsClicked)
		{
			DialogsManager.ShowDialog(null, new ViewGameLogDialog());
		}
		if (m_resetDefaultsButton.IsClicked)
		{
			SettingsManager.MultithreadedTerrainUpdate = true;
			SettingsManager.UseReducedZRange = false;
		}
		if (m_translationProviderButton.IsClicked)
		{
			SettingsManager.TranslationProviderState = (SettingsManager.TranslationProviderState + 1) % 3;
			switch (SettingsManager.TranslationProviderState)
			{
			case 0:
				m_translationProviderButton.Text = "Auto";
				break;
			case 1:
				m_translationProviderButton.Text = "Google";
				break;
			case 2:
				m_translationProviderButton.Text = "Gitee";
				break;
			}
		}
		m_singlethreadedTerrainUpdateButton.Text = (SettingsManager.MultithreadedTerrainUpdate ? LanguageControl.Off : LanguageControl.On);
		m_useReducedZRangeButton.Text = (SettingsManager.UseReducedZRange ? LanguageControl.On : LanguageControl.Off);
		m_enableModButton.Text = (SettingsManager.EnableMod ? LanguageControl.On : LanguageControl.Off);
		m_resetDefaultsButton.IsEnabled = !SettingsManager.MultithreadedTerrainUpdate || SettingsManager.UseReducedZRange;
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
		}
	}
}
