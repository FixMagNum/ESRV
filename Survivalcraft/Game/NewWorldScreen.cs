using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game.NetWork;

namespace Game;

public class NewWorldScreen : Screen
{
	public TextBoxWidget m_nameTextBox;

	public TextBoxWidget m_seedTextBox;

	public ButtonWidget m_gameModeButton;

	public ButtonWidget m_startingPositionButton;

	public ButtonWidget m_worldOptionsButton;

	public LabelWidget m_blankSeedLabel;

	public LabelWidget m_descriptionLabel;

	public LabelWidget m_errorLabel;

	public static string fName;

	public ButtonWidget m_playButton;

	public Random m_random = new Random();

	public WorldSettings m_worldSettings;

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

	public RectangleWidget m_embraceLock;

	public NewWorldScreen()
	{
		XElement restParams = ContentManager.Get<XElement>("Screens/NewWorldScreen");
		LoadContents(this, restParams);
		m_nameTextBox = Children.Find<TextBoxWidget>("Name");
		m_seedTextBox = Children.Find<TextBoxWidget>("Seed");
		m_gameModeButton = Children.Find<ButtonWidget>("GameMode");
		m_startingPositionButton = Children.Find<ButtonWidget>("StartingPosition");
		m_worldOptionsButton = Children.Find<ButtonWidget>("WorldOptions");
		m_blankSeedLabel = Children.Find<LabelWidget>("BlankSeed");
		m_descriptionLabel = Children.Find<LabelWidget>("Description");
		m_errorLabel = Children.Find<LabelWidget>("Error");
		m_playButton = Children.Find<ButtonWidget>("Play");
		m_embraceLock = Children.Find<RectangleWidget>("EmbraceLock");
		ServerConfigPanelWidget = Children.Find<UniformSpacingPanelWidget>("ServerConfig");
		MaxPlayerConfigPanelWidget = Children.Find<UniformSpacingPanelWidget>("MaxPlayerConfig");
		LoginConfigPanelWidget = Children.Find<UniformSpacingPanelWidget>("LoginConfig");
		RequirePasswordPanelWidget = Children.Find<UniformSpacingPanelWidget>("RequirePasswordPanel");
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
		m_seedTextBox.TextChanged += (TextBoxWidget sender) =>
		{
			m_worldSettings.Seed = sender.Text;
		};
	}

	public override void Enter(object[] parameters)
	{
		if (ScreensManager.PreviousScreen.GetType() != typeof(WorldOptionsScreen))
		{
			m_worldSettings = new WorldSettings
			{
				Name = WorldsManager.NewWorldNames[m_random.Int(0, WorldsManager.NewWorldNames.Count - 1)],
				OriginalSerializationVersion = VersionsManager.SerializationVersion,
				Seed = GenerateSeed()
			};
		}
		m_worldSettings.TimeOfDayMode = TimeOfDayMode.Changing;
		if (m_worldSettings.DaySpeed <= 0f)
		{
			m_worldSettings.DaySpeed = 1f;
		}
		bool restParams = m_worldSettings.GameMode == GameMode.Embrace || m_worldSettings.GameMode == GameMode.Cruel || m_worldSettings.GameMode == GameMode.RealChallenging;
		m_startingPositionButton.IsEnabled = !restParams;
		if (m_worldSettings.GameMode == GameMode.Embrace)
		{
			m_worldSettings.AreExplosionDropsEnabled = false;
		}
		RunServer.IsChecked = m_worldSettings.RunServer;
		NeedLogin.IsChecked = false;
		NeedLogin.IsEnabled = false;
		m_worldSettings.IsNeedCommunityLogin = false;
		RequirePassword.IsChecked = m_worldSettings.RequirePassword;
		RandomSpawPostion.IsChecked = m_worldSettings.RandomSpawnPosition;
		WorldEditIntegratedCheckbox.IsChecked = m_worldSettings.IsWorldEditIntegrated;
		if (m_worldSettings.WEInterventionUnlimited)
		{
			WEInterventionSlider.Value = 1f;
		}
		else
		{
			int num = m_worldSettings.WEInterventionLimit;
			if (num < 10000)
			{
				num = 10000;
			}
			if (num > 1000000)
			{
				num = 1000000;
			}
			float num2 = (float)(num - 10000) / 990000f;
			if (num2 < 0f)
			{
				num2 = 0f;
			}
			if (num2 > 0.999f)
			{
				num2 = 0.999f;
			}
			WEInterventionSlider.Value = num2;
		}
		WEInterventionPanel.IsVisible = WorldEditIntegratedCheckbox.IsChecked;
		if (m_worldSettings.RegionPrivateLimit >= 101)
		{
			PrivateLimitSlider.Value = 1f;
			PrivateLimitSlider.Text = LanguageControl.Get("NewWorldScreen", "we_unlimited");
		}
		else
		{
			int num3 = m_worldSettings.RegionPrivateLimit;
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num3 > 100)
			{
				num3 = 100;
			}
			PrivateLimitSlider.Value = (float)num3 / 100f;
			PrivateLimitSlider.Text = num3.ToString();
		}
		RestrictWorldInteractionCheckbox.IsChecked = m_worldSettings.RestrictWorldInteraction;
		WEManagersOnlyPanel.IsVisible = WorldEditIntegratedCheckbox.IsChecked;
		WEManagersOnlyCheckbox.IsChecked = m_worldSettings.WEManagersOnly;
	}

	public override void Update()
	{
		m_worldSettings.TimeOfDayMode = TimeOfDayMode.Changing;
		if (m_worldSettings.DaySpeed <= 0f)
		{
			m_worldSettings.DaySpeed = 1f;
		}
		bool flag = m_worldSettings.GameMode == GameMode.Embrace || m_worldSettings.GameMode == GameMode.Cruel || m_worldSettings.GameMode == GameMode.RealChallenging;
		m_startingPositionButton.IsEnabled = !flag;
		if (flag)
		{
			m_worldSettings.StartingPositionMode = StartingPositionMode.Hard;
		}
		m_worldSettings.RunServer = RunServer.IsChecked;
		ServerConfigPanelWidget.IsVisible = m_worldSettings.RunServer;
		MaxPlayerConfigPanelWidget.IsVisible = m_worldSettings.RunServer;
		LoginConfigPanelWidget.IsVisible = false;
		RequirePasswordPanelWidget.IsVisible = m_worldSettings.RunServer;
		DisableBlocksPanel.IsVisible = m_worldSettings.RunServer;
		RandomSpawnPostionPanel.IsVisible = m_worldSettings.RunServer;
		PrivateLimitPanel.IsVisible = m_worldSettings.RunServer;
		RestrictInteractPanel.IsVisible = m_worldSettings.RunServer;
		int.TryParse(MaxPlayer.Text, out var result);
		m_worldSettings.MaxOnlinePlayerCount = (ushort)MathUtils.Max(result, 1);
		m_worldSettings.DisableBlocks = DisableBlocks.Text;
		NeedLogin.IsChecked = false;
		NeedLogin.IsEnabled = false;
		m_worldSettings.IsNeedCommunityLogin = false;
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
		if (m_gameModeButton.IsClicked)
		{
			GameMode[] array = (GameMode[])Enum.GetValues(typeof(GameMode));
			List<GameMode> list = new List<GameMode>();
			GameMode[] array2 = array;
			foreach (GameMode gameMode in array2)
			{
				if (gameMode != GameMode.Adventure)
				{
					list.Add(gameMode);
				}
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get("NewWorldScreen", "GameModeListTitle"), list, 110f, (object item) =>
			{
				GameMode gameMode2 = (GameMode)item;
				StackPanelWidget stackPanelWidget = new StackPanelWidget
				{
					Direction = LayoutDirection.Vertical,
					VerticalAlignment = WidgetAlignment.Center,
					HorizontalAlignment = WidgetAlignment.Center
				};
				string text = LanguageControl.Get("GameMode", gameMode2.ToString());
				LabelWidget labelWidget = new LabelWidget
				{
					Text = text,
					HorizontalAlignment = WidgetAlignment.Center,
					FontScale = 1f
				};
				string text2 = StringsManager.GetString("GameMode." + gameMode2.ToString() + ".Description");
				LabelWidget widget = new LabelWidget
				{
					Text = text2,
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
					labelWidget.Color = new Color(128, 0, 32);
					break;
				case GameMode.Cruel:
				case GameMode.RealChallenging:
					labelWidget.Color = new Color(50, 160, 255);
					break;
				default:
					labelWidget.Color = Color.White;
					break;
				}
				stackPanelWidget.Children.Add(labelWidget);
				stackPanelWidget.Children.Add(widget);
				return stackPanelWidget;
			}, (object item) =>
			{
				GameMode gameMode2 = (GameMode)item;
				m_worldSettings.GameMode = gameMode2;
				if (gameMode2 == GameMode.Embrace || gameMode2 == GameMode.Cruel || gameMode2 == GameMode.RealChallenging)
				{
					m_worldSettings.StartingPositionMode = StartingPositionMode.Hard;
				}
				else
				{
					m_worldSettings.StartingPositionMode = StartingPositionMode.Easy;
				}
				if (gameMode2 == GameMode.Embrace)
				{
					m_worldSettings.AreExplosionDropsEnabled = false;
				}
			}));
		}
		if (m_startingPositionButton.IsClicked)
		{
			IList<int> enumValues = EnumUtils.GetEnumValues(typeof(StartingPositionMode));
			m_worldSettings.StartingPositionMode = (StartingPositionMode)((enumValues.IndexOf((int)m_worldSettings.StartingPositionMode) + 1) % enumValues.Count);
		}
		bool flag2 = WorldsManager.ValidateWorldName(m_worldSettings.Name);
		m_nameTextBox.Text = m_worldSettings.Name;
		m_seedTextBox.Text = m_worldSettings.Seed;
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
		m_startingPositionButton.Text = LanguageControl.Get("StartingPositionMode", m_worldSettings.StartingPositionMode.ToString());
		bool flag3 = m_worldSettings.GameMode == GameMode.Embrace;
		m_playButton.IsVisible = flag2;
		m_playButton.IsEnabled = !flag3;
		if (m_embraceLock != null)
		{
			m_embraceLock.IsVisible = flag3 & flag2;
		}
		m_errorLabel.IsVisible = !flag2;
		m_blankSeedLabel.IsVisible = m_worldSettings.Seed.Length == 0 && !m_seedTextBox.HasFocus;
		m_descriptionLabel.Text = StringsManager.GetString("GameMode." + m_worldSettings.GameMode.ToString() + ".Description");
		if (m_worldOptionsButton.IsClicked)
		{
			ScreensManager.SwitchScreen("WorldOptions", m_worldSettings, false);
		}
		if (m_playButton.IsClicked && WorldsManager.ValidateWorldName(m_nameTextBox.Text))
		{
			WorldInfo worldInfo = WorldsManager.CreateWorld(m_worldSettings);
			if (m_worldSettings.GameMode != GameMode.Creative)
			{
				m_worldSettings.ResetOptionsForNonCreativeMode();
			}
			if (RunServer.IsChecked)
			{
				if (CommonLib.StartServer())
				{
					ScreensManager.SwitchScreen("GameLoading", worldInfo, null);
				}
				else
				{
					DialogsManager.ShowDialog(this, new MessageDialog(LanguageControl.Get("NewWorldScreenL", "notification"), LanguageControl.Get("NewWorldScreenL", "server_creation_failed"), LanguageControl.Get("NewWorldScreenL", "ok_button"), null, (MessageDialogButton txt) =>
					{
						CommonLib.Net.StopImmediate();
						DialogsManager.HideAllDialogs();
					})
					{
						AutoHide = false
					});
				}
			}
			else
			{
				ScreensManager.SwitchScreen("GameLoading", worldInfo, null);
			}
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen("Play");
		}
	}

	static NewWorldScreen()
	{
		fName = "NewWorldScreen";
	}

	private string GenerateSeed()
	{
		System.Random random = new System.Random();
		char[] array = new char[random.Next(8, 13)];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[random.Next("ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Length)];
		}
		return new string(array);
	}
}
