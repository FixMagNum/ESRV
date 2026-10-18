using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class ModifyWorldScreen : Screen
{
	public TextBoxWidget m_nameTextBox;

	public LabelWidget m_seedLabel;

	public ButtonWidget m_gameModeButton;

	public ButtonWidget m_worldOptionsButton;

	public LabelWidget m_errorLabel;

	public LabelWidget m_descriptionLabel;

	public ButtonWidget m_applyButton;

	public ButtonWidget m_deleteButton;

	public ButtonWidget m_uploadButton;

	public string m_directoryName;

	public WorldSettings m_worldSettings;

	public ValuesDictionary m_currentWorldSettingsData = new ValuesDictionary();

	public ValuesDictionary m_originalWorldSettingsData = new ValuesDictionary();

	public static string fName = "ModifyWorldScreen";

	public CheckboxWidget RunServer;

	public CheckboxWidget NeedLogin;

	public CheckboxWidget RandomSpawPostion;

	public UniformSpacingPanelWidget ServerConfigPanelWidget;

	public UniformSpacingPanelWidget MaxPlayerConfigPanelWidget;

	public UniformSpacingPanelWidget LoginConfigPanelWidget;

	public UniformSpacingPanelWidget DisableBlocksPanel;

	public UniformSpacingPanelWidget RandomSpawnPostionPanel;

	public TextBoxWidget Password;

	public TextBoxWidget MaxPlayer;

	public TextBoxWidget DisableBlocks;

	public CheckboxWidget RequirePassword;

	public UniformSpacingPanelWidget RequirePasswordPanelWidget;

	public CheckboxWidget WorldEditIntegratedCheckbox;

	public SliderWidget WEInterventionSlider;

	public UniformSpacingPanelWidget WEInterventionPanel;

	public UniformSpacingPanelWidget PrivateLimitPanel;

	public SliderWidget PrivateLimitSlider;

	public UniformSpacingPanelWidget RestrictInteractPanel;

	public CheckboxWidget RestrictWorldInteractionCheckbox;

	public UniformSpacingPanelWidget WEManagersOnlyPanel;

	public CheckboxWidget WEManagersOnlyCheckbox;

	public UniformSpacingPanelWidget ResetPasswordsPanelWidget;

	public ButtonWidget ResetPasswordsButton;

	public LabelWidget ResetPasswordsInfo;

	public ModifyWorldScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/ModifyWorldScreen");
		LoadContents(this, node);
		m_nameTextBox = Children.Find<TextBoxWidget>("Name");
		m_seedLabel = Children.Find<LabelWidget>("Seed");
		m_gameModeButton = Children.Find<ButtonWidget>("GameMode");
		m_worldOptionsButton = Children.Find<ButtonWidget>("WorldOptions");
		m_errorLabel = Children.Find<LabelWidget>("Error");
		m_descriptionLabel = Children.Find<LabelWidget>("Description");
		m_applyButton = Children.Find<ButtonWidget>("Apply");
		m_deleteButton = Children.Find<ButtonWidget>("Delete");
		m_uploadButton = Children.Find<ButtonWidget>("Upload");
		MaxPlayerConfigPanelWidget = Children.Find<UniformSpacingPanelWidget>("MaxPlayerConfig");
		ServerConfigPanelWidget = Children.Find<UniformSpacingPanelWidget>("ServerConfig");
		LoginConfigPanelWidget = Children.Find<UniformSpacingPanelWidget>("LoginConfig");
		RequirePasswordPanelWidget = Children.Find<UniformSpacingPanelWidget>("RequirePasswordPanel");
		ResetPasswordsPanelWidget = Children.Find<UniformSpacingPanelWidget>("ResetPasswordsPanel");
		ResetPasswordsButton = Children.Find<ButtonWidget>("ResetPasswords");
		ResetPasswordsInfo = Children.Find<LabelWidget>("ResetPasswordsInfo");
		DisableBlocksPanel = Children.Find<UniformSpacingPanelWidget>("DisableBlocksPanel");
		RandomSpawnPostionPanel = Children.Find<UniformSpacingPanelWidget>("RandomSpawnPositionPanel");
		RunServer = Children.Find<CheckboxWidget>("RunServer");
		NeedLogin = Children.Find<CheckboxWidget>("NeedLogin");
		RequirePassword = Children.Find<CheckboxWidget>("RequirePassword");
		RandomSpawPostion = Children.Find<CheckboxWidget>("RandomSpawnPosition");
		Password = Children.Find<TextBoxWidget>("Password");
		MaxPlayer = Children.Find<TextBoxWidget>("MaxPlayers");
		DisableBlocks = Children.Find<TextBoxWidget>("DisableBlocks");
		WorldEditIntegratedCheckbox = Children.Find<CheckboxWidget>("WorldEditIntegrated");
		WEInterventionSlider = Children.Find<SliderWidget>("WEInterventionSlider");
		WEInterventionPanel = Children.Find<UniformSpacingPanelWidget>("WEInterventionPanel");
		PrivateLimitPanel = Children.Find<UniformSpacingPanelWidget>("PrivateLimitPanel");
		PrivateLimitSlider = Children.Find<SliderWidget>("PrivateLimitSlider");
		RestrictInteractPanel = Children.Find<UniformSpacingPanelWidget>("RestrictInteractPanel");
		RestrictWorldInteractionCheckbox = Children.Find<CheckboxWidget>("RestrictWorldInteraction");
		WEManagersOnlyPanel = Children.Find<UniformSpacingPanelWidget>("WEManagersOnlyPanel");
		WEManagersOnlyCheckbox = Children.Find<CheckboxWidget>("WEManagersOnly");
		DisableBlocks.MaximumLength = int.MaxValue;
		NeedLogin.IsVisible = false;
		m_nameTextBox.TextChanged += (TextBoxWidget sender) =>
		{
			m_worldSettings.Name = sender.Text;
		};
	}

	public override void Enter(object[] parameters)
	{
		if (!(ScreensManager.PreviousScreen.GetType() != typeof(WorldOptionsScreen)))
		{
			return;
		}
		m_directoryName = (string)parameters[0];
		m_worldSettings = (WorldSettings)parameters[1];
		PlayerPasswordRepository.Initialize(m_worldSettings);
		if (m_worldSettings.TimeOfDayMode != TimeOfDayMode.Day && m_worldSettings.TimeOfDayMode != TimeOfDayMode.Night && m_worldSettings.TimeOfDayMode != TimeOfDayMode.Sunrise && m_worldSettings.TimeOfDayMode != TimeOfDayMode.Sunset)
		{
			m_worldSettings.TimeOfDayMode = TimeOfDayMode.Changing;
		}
		if (m_worldSettings.DaySpeed <= 0f)
		{
			m_worldSettings.DaySpeed = 1f;
		}
		if (m_worldSettings.GameMode == GameMode.Embrace)
		{
			m_worldSettings.AreExplosionDropsEnabled = false;
		}
		m_originalWorldSettingsData.Clear();
		MaxPlayer.Text = m_worldSettings.MaxOnlinePlayerCount.ToString();
		RunServer.IsChecked = m_worldSettings.RunServer;
		NeedLogin.IsChecked = false;
		NeedLogin.IsEnabled = false;
		m_worldSettings.IsNeedCommunityLogin = false;
		RequirePassword.IsChecked = m_worldSettings.RequirePassword;
		RandomSpawPostion.IsChecked = m_worldSettings.RandomSpawnPosition;
		DisableBlocks.Text = m_worldSettings.DisableBlocks;
		WorldEditIntegratedCheckbox.IsChecked = m_worldSettings.IsWorldEditIntegrated;
		if (m_worldSettings.WEInterventionUnlimited)
		{
			WEInterventionSlider.Value = 1f;
		}
		else
		{
			int componentCreature = m_worldSettings.WEInterventionLimit;
			if (componentCreature < 10000)
			{
				componentCreature = 10000;
			}
			if (componentCreature > 1000000)
			{
				componentCreature = 1000000;
			}
			float flag = (float)(componentCreature - 10000) / 990000f;
			if (flag < 0f)
			{
				flag = 0f;
			}
			if (flag > 0.999f)
			{
				flag = 0.999f;
			}
			WEInterventionSlider.Value = flag;
		}
		WEInterventionPanel.IsVisible = WorldEditIntegratedCheckbox.IsChecked;
		if (m_worldSettings.RegionPrivateLimit >= 101)
		{
			PrivateLimitSlider.Value = 1f;
			PrivateLimitSlider.Text = LanguageControl.Get("NewWorldScreen", "we_unlimited");
		}
		else
		{
			int flag2 = m_worldSettings.RegionPrivateLimit;
			if (flag2 < 0)
			{
				flag2 = 0;
			}
			if (flag2 > 100)
			{
				flag2 = 100;
			}
			PrivateLimitSlider.Value = (float)flag2 / 100f;
			PrivateLimitSlider.Text = flag2.ToString();
		}
		RestrictWorldInteractionCheckbox.IsChecked = m_worldSettings.RestrictWorldInteraction;
		WEManagersOnlyPanel.IsVisible = WorldEditIntegratedCheckbox.IsChecked;
		WEManagersOnlyCheckbox.IsChecked = m_worldSettings.WEManagersOnly;
		m_worldSettings.Save(m_originalWorldSettingsData, liveModifiableParametersOnly: true);
	}

	public override void Update()
	{
		ServerConfigPanelWidget.IsVisible = RunServer.IsChecked;
		MaxPlayerConfigPanelWidget.IsVisible = RunServer.IsChecked;
		LoginConfigPanelWidget.IsVisible = false;
		RequirePasswordPanelWidget.IsVisible = RunServer.IsChecked;
		ResetPasswordsPanelWidget.IsVisible = RunServer.IsChecked && RequirePassword.IsChecked;
		DisableBlocksPanel.IsVisible = RunServer.IsChecked;
		if (ResetPasswordsPanelWidget.IsVisible)
		{
			int passwordsCount = PlayerPasswordRepository.GetPasswordsCount();
			ResetPasswordsInfo.Text = string.Format(LanguageControl.Get(fName, "PasswordsCountLabel"), passwordsCount);
			ResetPasswordsButton.Text = LanguageControl.Get(fName, "ResetPasswordsBtn");
			ResetPasswordsButton.IsEnabled = passwordsCount > 0;
		}
		RandomSpawnPostionPanel.IsVisible = RunServer.IsChecked;
		PrivateLimitPanel.IsVisible = RunServer.IsChecked;
		RestrictInteractPanel.IsVisible = RunServer.IsChecked;
		if (m_worldSettings.TimeOfDayMode != TimeOfDayMode.Day && m_worldSettings.TimeOfDayMode != TimeOfDayMode.Night && m_worldSettings.TimeOfDayMode != TimeOfDayMode.Sunrise && m_worldSettings.TimeOfDayMode != TimeOfDayMode.Sunset)
		{
			m_worldSettings.TimeOfDayMode = TimeOfDayMode.Changing;
		}
		if (m_worldSettings.DaySpeed <= 0f)
		{
			m_worldSettings.DaySpeed = 1f;
		}
		int.TryParse(MaxPlayer.Text, out var result);
		m_worldSettings.MaxOnlinePlayerCount = (ushort)MathUtils.Max(result, 1);
		m_worldSettings.DisableBlocks = DisableBlocks.Text;
		NeedLogin.IsChecked = false;
		NeedLogin.IsEnabled = false;
		m_worldSettings.IsNeedCommunityLogin = false;
		if (m_gameModeButton.IsClicked && m_worldSettings.GameMode != GameMode.Cruel && m_worldSettings.GameMode != GameMode.Embrace && m_worldSettings.GameMode != GameMode.RealChallenging && !m_worldSettings.LockWorldSettings)
		{
			GameMode[] array = (GameMode[])Enum.GetValues(typeof(GameMode));
			List<GameMode> list = new List<GameMode>();
			GameMode[] array2 = array;
			foreach (GameMode gameMode in array2)
			{
				if (gameMode != GameMode.Cruel && gameMode != GameMode.Embrace && gameMode != GameMode.RealChallenging)
				{
					list.Add(gameMode);
				}
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get("ModifyWorldScreen", "GameModeListTitle"), list, 110f, (object item) =>
			{
				GameMode gameMode2 = (GameMode)item;
				StackPanelWidget stackPanelWidget = new StackPanelWidget
				{
					Direction = LayoutDirection.Vertical,
					VerticalAlignment = WidgetAlignment.Center,
					HorizontalAlignment = WidgetAlignment.Center
				};
				string text2 = LanguageControl.Get("GameMode", gameMode2.ToString());
				LabelWidget labelWidget2 = new LabelWidget
				{
					Text = text2,
					HorizontalAlignment = WidgetAlignment.Center,
					FontScale = 1f
				};
				string text3 = StringsManager.GetString("GameMode." + gameMode2.ToString() + ".Description");
				LabelWidget widget2 = new LabelWidget
				{
					Text = text3,
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					FontScale = 0.8f,
					Color = Color.Gray,
					WordWrap = true,
					TextAnchor = TextAnchor.HorizontalCenter
				};
				switch (gameMode2)
				{
				case GameMode.Embrace:
					labelWidget2.Color = new Color(128, 0, 32);
					break;
				case GameMode.Cruel:
				case GameMode.RealChallenging:
					labelWidget2.Color = new Color(50, 160, 255);
					break;
				default:
					labelWidget2.Color = Color.White;
					break;
				}
				stackPanelWidget.Children.Add(labelWidget2);
				stackPanelWidget.Children.Add(widget2);
				return stackPanelWidget;
			}, (object item) =>
			{
				GameMode gameMode2 = (GameMode)item;
				m_worldSettings.GameMode = gameMode2;
				if (gameMode2 == GameMode.Embrace)
				{
					m_worldSettings.AreExplosionDropsEnabled = false;
				}
				m_descriptionLabel.Text = StringsManager.GetString("GameMode." + m_worldSettings.GameMode.ToString() + ".Description");
			}));
		}
		m_worldSettings.RunServer = RunServer.IsChecked;
		m_worldSettings.Password = Password.Text;
		m_worldSettings.RequirePassword = RequirePassword.IsChecked;
		m_worldSettings.RandomSpawnPosition = RandomSpawPostion.IsChecked;
		m_worldSettings.IsWorldEditIntegrated = WorldEditIntegratedCheckbox.IsChecked;
		WEInterventionPanel.IsVisible = WorldEditIntegratedCheckbox.IsChecked;
		if (WEInterventionPanel.IsVisible)
		{
			float value = WEInterventionSlider.Value;
			if (value >= 0.999f)
			{
				m_worldSettings.WEInterventionUnlimited = true;
				m_worldSettings.WEInterventionLimit = 0;
				WEInterventionSlider.Text = LanguageControl.Get("NewWorldScreen", "we_unlimited");
			}
			else
			{
				int num = 10000 + (int)MathUtils.Round(value * 990000f);
				if (num < 10000)
				{
					num = 10000;
				}
				if (num > 1000000)
				{
					num = 1000000;
				}
				num = num / 10000 * 10000;
				m_worldSettings.WEInterventionUnlimited = false;
				m_worldSettings.WEInterventionLimit = num;
				WEInterventionSlider.Text = num.ToString();
			}
		}
		if (PrivateLimitPanel.IsVisible)
		{
			float value2 = PrivateLimitSlider.Value;
			if (value2 >= 0.999f)
			{
				m_worldSettings.RegionPrivateLimit = 101;
				PrivateLimitSlider.Text = LanguageControl.Get("NewWorldScreen", "we_unlimited");
			}
			else
			{
				int num2;
				if (value2 <= 0.1f)
				{
					num2 = (int)MathUtils.Round(value2 / 0.1f * 10f);
				}
				else
				{
					float num3 = (value2 - 0.1f) / 0.9f;
					num2 = (1 + (int)MathUtils.Round(num3 * 9f)) * 10;
				}
				if (num2 < 0)
				{
					num2 = 0;
				}
				if (num2 > 100)
				{
					num2 = 100;
				}
				m_worldSettings.RegionPrivateLimit = num2;
				PrivateLimitSlider.Text = num2.ToString();
			}
		}
		if (RestrictInteractPanel.IsVisible)
		{
			m_worldSettings.RestrictWorldInteraction = RestrictWorldInteractionCheckbox.IsChecked;
		}
		WEManagersOnlyPanel.IsVisible = WorldEditIntegratedCheckbox.IsChecked;
		if (WEManagersOnlyPanel.IsVisible)
		{
			m_worldSettings.WEManagersOnly = WEManagersOnlyCheckbox.IsChecked;
		}
		m_currentWorldSettingsData.Clear();
		m_worldSettings.Save(m_currentWorldSettingsData, liveModifiableParametersOnly: true);
		bool flag = !CompareValueDictionaries(m_originalWorldSettingsData, m_currentWorldSettingsData);
		bool flag2 = WorldsManager.ValidateWorldName(m_worldSettings.Name);
		m_nameTextBox.Text = m_worldSettings.Name;
		m_seedLabel.Text = m_worldSettings.Seed;
		m_gameModeButton.Text = LanguageControl.Get("GameMode", m_worldSettings.GameMode.ToString());
		try
		{
			if (m_worldSettings.GameMode == GameMode.Embrace)
			{
				m_gameModeButton.Color = new Color(128, 0, 32);
			}
			else if (m_worldSettings.GameMode == GameMode.RealChallenging || m_worldSettings.GameMode == GameMode.Cruel)
			{
				m_gameModeButton.Color = new Color(50, 160, 255);
			}
			else
			{
				m_gameModeButton.Color = Color.White;
			}
		}
		catch
		{
		}
		m_gameModeButton.IsEnabled = m_worldSettings.GameMode != GameMode.Cruel && m_worldSettings.GameMode != GameMode.Embrace && m_worldSettings.GameMode != GameMode.RealChallenging && !m_worldSettings.LockWorldSettings;
		m_errorLabel.IsVisible = !flag2;
		m_descriptionLabel.IsVisible = flag2;
		m_uploadButton.IsEnabled = flag2 && !flag;
		m_applyButton.IsEnabled = flag2 & flag;
		m_descriptionLabel.Text = StringsManager.GetString("GameMode." + m_worldSettings.GameMode.ToString() + ".Description");
		if (m_worldOptionsButton.IsClicked)
		{
			ScreensManager.SwitchScreen("WorldOptions", m_worldSettings, true);
		}
		if (ResetPasswordsButton.IsClicked)
		{
			MessageDialog messageDialog = new MessageDialog(LanguageControl.Get(fName, "ResetPasswordsWarningTitle"), LanguageControl.Get(fName, "ResetPasswordsWarningText"), LanguageControl.Get("Usual", "yes"), LanguageControl.Get("Usual", "no"), (MessageDialogButton button) =>
			{
				if (button == MessageDialogButton.Button1)
				{
					PlayerPasswordRepository.ClearAll();
				}
			});
			try
			{
				List<Widget> list2 = new List<Widget>();
				list2.Add(messageDialog);
				for (int num4 = 0; num4 < list2.Count; num4++)
				{
					Widget widget = list2[num4];
					if (widget is LabelWidget { Text: var text } labelWidget && text == LanguageControl.Get(fName, "ResetPasswordsWarningText"))
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
		if (m_deleteButton.IsClicked)
		{
			MessageDialog dialog = null;
			dialog = new MessageDialog(LanguageControl.Get(fName, 1), LanguageControl.Get(fName, 2), LanguageControl.Get("Usual", "yes"), LanguageControl.Get("Usual", "no"), (MessageDialogButton button) =>
			{
				if (button == MessageDialogButton.Button1)
				{
					WorldsManager.DeleteWorld(m_directoryName);
					ScreensManager.SwitchScreen("Play");
					DialogsManager.HideDialog(dialog);
				}
				else
				{
					DialogsManager.HideDialog(dialog);
				}
			})
			{
				AutoHide = false
			};
			DialogsManager.ShowDialog(null, dialog);
		}
		if ((m_uploadButton.IsClicked & flag2) && !flag)
		{
			ExternalContentManager.ShowUploadUi(ExternalContentType.World, m_directoryName);
		}
		if (m_applyButton.IsClicked & flag2 & flag)
		{
			if (m_worldSettings.GameMode != GameMode.Creative && m_worldSettings.GameMode != GameMode.Adventure)
			{
				m_worldSettings.ResetOptionsForNonCreativeMode();
			}
			WorldsManager.ChangeWorld(m_directoryName, m_worldSettings);
			ScreensManager.SwitchScreen("Play");
		}
		if (!Input.Back && !Input.Cancel && !Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			return;
		}
		if (flag)
		{
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 3), LanguageControl.Get(fName, 4), LanguageControl.Get("Usual", "yes"), LanguageControl.Get("Usual", "no"), (MessageDialogButton button) =>
			{
				if (button == MessageDialogButton.Button1)
				{
					ScreensManager.SwitchScreen("Play");
				}
			}));
		}
		else
		{
			ScreensManager.SwitchScreen("Play");
		}
	}

	public static bool CompareValueDictionaries(ValuesDictionary d1, ValuesDictionary d2)
	{
		if (d1.Count != d2.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, object> flag in d1)
		{
			object num = d2.GetValue<object>(flag.Key, null);
			if (num is ValuesDictionary num2)
			{
				if (!(flag.Value is ValuesDictionary d3) || !CompareValueDictionaries(num2, d3))
				{
					return false;
				}
			}
			else if (!object.Equals(num, flag.Value))
			{
				return false;
			}
		}
		return true;
	}
}
