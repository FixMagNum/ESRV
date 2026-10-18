using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;

namespace Game;

public class PlayerScreen : Screen
{
	public enum Mode
	{
		Initial,
		Add,
		Edit
	}

	public PlayerData m_playerData;

	public Mode m_mode;

	public CharacterSkinsCache m_characterSkinsCache;

	public bool m_nameWasInvalid;

	public PlayerModelWidget m_playerModel;

	public ButtonWidget m_playerClassButton;

	public TextBoxWidget m_nameTextBox;

	public LabelWidget m_characterSkinLabel;

	public ButtonWidget m_characterSkinButton;

	public LabelWidget m_controlsLabel;

	public ButtonWidget m_controlsButton;

	public LabelWidget m_descriptionLabel;

	public ButtonWidget m_addButton;

	public ButtonWidget m_addAnotherButton;

	public ButtonWidget m_deleteButton;

	public ButtonWidget m_playButton;

	public static string fName;

	public double EnterTime;

	public WidgetInputDevice[] m_inputDevices = new WidgetInputDevice[5]
	{
		WidgetInputDevice.None,
		WidgetInputDevice.GamePad1,
		WidgetInputDevice.GamePad2,
		WidgetInputDevice.GamePad3,
		WidgetInputDevice.GamePad4
	};

	private BusyDialog m_fetchingPlayerDialog = new BusyDialog("Downloading player...", null);

	public ButtonWidget m_bodyTagButton;

	public CheckboxWidget m_weManagerCheckbox;

	public ButtonWidget m_muteButton;

	public ButtonWidget m_banButton;

	public ButtonWidget m_kickButton;

	public bool m_isAdmin;

	public bool m_isTargetSelf;

	public StackPanelWidget m_adminPanel;

	public ButtonWidget m_resetPasswordButton;

	public PlayerScreen()
	{
		XElement componentCreature = ContentManager.Get<XElement>("Screens/PlayerScreen");
		LoadContents(this, componentCreature);
		m_playerModel = Children.Find<PlayerModelWidget>("Model");
		m_playerClassButton = Children.Find<ButtonWidget>("PlayerClassButton");
		m_bodyTagButton = Children.Find<ButtonWidget>("BodyTagButton");
		m_nameTextBox = Children.Find<TextBoxWidget>("Name");
		m_characterSkinLabel = Children.Find<LabelWidget>("CharacterSkinLabel");
		m_characterSkinButton = Children.Find<ButtonWidget>("CharacterSkinButton");
		m_controlsLabel = Children.Find<LabelWidget>("ControlsLabel");
		m_controlsButton = Children.Find<ButtonWidget>("ControlsButton");
		m_descriptionLabel = Children.Find<LabelWidget>("DescriptionLabel");
		m_addButton = Children.Find<ButtonWidget>("AddButton");
		m_addAnotherButton = Children.Find<ButtonWidget>("AddAnotherButton");
		m_deleteButton = Children.Find<ButtonWidget>("DeleteButton");
		m_playButton = Children.Find<ButtonWidget>("PlayButton");
		m_weManagerCheckbox = Children.Find<CheckboxWidget>("WEManagerCheckbox", throwIfNotFound: false);
		m_muteButton = Children.Find<ButtonWidget>("MuteButton", throwIfNotFound: false);
		m_banButton = Children.Find<ButtonWidget>("BanButton", throwIfNotFound: false);
		m_kickButton = Children.Find<ButtonWidget>("KickButton", throwIfNotFound: false);
		m_characterSkinsCache = new CharacterSkinsCache();
		m_adminPanel = Children.Find<StackPanelWidget>("AdminPanel", throwIfNotFound: false);
		m_resetPasswordButton = Children.Find<ButtonWidget>("ResetPasswordButton", throwIfNotFound: false);
		m_playerModel.CharacterSkinsCache = m_characterSkinsCache;
		m_nameTextBox.FocusLost += (TextBoxWidget sender) =>
		{
			if (VerifyName())
			{
				m_playerData.Name = m_nameTextBox.Text.Trim();
			}
			else
			{
				m_nameWasInvalid = true;
			}
		};
	}

	public override void Enter(object[] parameters)
	{
		EnterTime = Time.RealTime;
		m_mode = (Mode)parameters[0];
		m_playerData = ((m_mode == Mode.Edit) ? ((PlayerData)parameters[1]) : new PlayerData((Project)parameters[1]));
		PlayerData playerData = ((m_playerData.SubsystemPlayers != null) ? m_playerData.SubsystemPlayers.MainPlayerData : null);
		m_isAdmin = playerData?.ServerManager ?? false;
		m_isTargetSelf = playerData != null && playerData.PlayerGUID == m_playerData.PlayerGUID;
		if (m_mode == Mode.Edit && CommonLib.WorkType != WorkType.Local && !m_isAdmin && !m_isTargetSelf)
		{
			ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
			return;
		}
		bool flag = false;
		if (CommonLib.WorkType == WorkType.Client)
		{
			SubsystemGameInfo subsystemGameInfo = m_playerData.Project.FindSubsystem<SubsystemGameInfo>();
			if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.IsNeedCommunityLogin)
			{
				m_playerData.Name = NetNode.CommunityNickName;
				flag = true;
			}
		}
		if (!flag && (m_mode == Mode.Initial || m_mode == Mode.Add) && (m_playerData.IsDefaultName || string.IsNullOrEmpty(m_playerData.Name)) && !string.IsNullOrEmpty(SettingsManager.PlayerName))
		{
			m_playerData.Name = SettingsManager.PlayerName;
		}
		if (m_mode == Mode.Initial)
		{
			m_playerClassButton.IsEnabled = true;
			m_addButton.IsVisible = false;
			m_deleteButton.IsVisible = false;
			m_playButton.IsVisible = true;
			m_addAnotherButton.IsVisible = m_playerData.SubsystemPlayers.PlayersData.Count < 3;
		}
		else if (m_mode == Mode.Add)
		{
			m_playerClassButton.IsEnabled = true;
			m_addButton.IsVisible = true;
			m_deleteButton.IsVisible = false;
			m_playButton.IsVisible = false;
			m_addAnotherButton.IsVisible = false;
		}
		else if (m_mode == Mode.Edit)
		{
			m_playerClassButton.IsEnabled = false;
			m_addButton.IsVisible = false;
			m_deleteButton.IsVisible = m_playerData.SubsystemPlayers.PlayersData.Count > 1;
			m_playButton.IsVisible = false;
			m_addAnotherButton.IsVisible = false;
		}
		m_addAnotherButton.IsVisible &= CommonLib.WorkType == WorkType.Local;
		m_deleteButton.IsVisible &= CommonLib.WorkType != WorkType.Client;
		bool isAdmin = m_isAdmin;
		bool flag2 = m_isAdmin && !m_isTargetSelf && CommonLib.WorkType == WorkType.Server;
		if (m_adminPanel != null)
		{
			m_adminPanel.IsVisible = isAdmin;
		}
		if (m_weManagerCheckbox != null)
		{
			m_weManagerCheckbox.IsChecked = m_playerData.WorldEditManager;
		}
		if (m_muteButton != null)
		{
			m_muteButton.IsVisible = flag2;
		}
		if (m_banButton != null)
		{
			m_banButton.IsVisible = flag2;
		}
		if (m_kickButton != null)
		{
			m_kickButton.IsVisible = flag2;
		}
		if (flag2)
		{
			m_addButton.IsVisible = false;
			m_addAnotherButton.IsVisible = false;
			m_playButton.IsVisible = false;
			m_deleteButton.IsVisible = false;
		}
		if (m_addAnotherButton != null)
		{
			m_addAnotherButton.IsVisible = false;
			m_addAnotherButton.IsEnabled = false;
		}
		SubsystemGameInfo subsystemGameInfo2 = m_playerData.Project.FindSubsystem<SubsystemGameInfo>();
		if (CommonLib.WorkType == WorkType.Server && subsystemGameInfo2 != null && subsystemGameInfo2.WorldSettings != null && subsystemGameInfo2.WorldSettings.RequirePassword)
		{
			PlayerPasswordRepository.Initialize(subsystemGameInfo2.WorldSettings);
		}
	}

	public override void Leave()
	{
		try
		{
			if (m_characterSkinsCache != null)
			{
				m_characterSkinsCache.Clear();
			}
		}
		catch (Exception)
		{
		}
		m_playerData = null;
	}

	public override void Update()
	{
		if (m_mode == Mode.Initial && DialogsManager.Dialogs.Contains(m_fetchingPlayerDialog) && Time.RealTime - EnterTime > 120.0 && CommonLib.WorkType == WorkType.Client && CommonLib.Net.netManager.IsRunning)
		{
			CommonLib.Net.Stop();
			CommonLib.Net.Stopped += () =>
			{
				ScreensManager.SwitchScreen("MainMenu");
				DialogsManager.Alert("Client closed connection");
			};
		}
		if (DialogsManager.Dialogs.Contains(m_fetchingPlayerDialog))
		{
			if (m_playerData.SubsystemPlayers.FindPlayerData((PlayerData p) => p.IsMainPlayer) != null)
			{
				DialogsManager.HideAllDialogs();
				ScreensManager.SwitchScreen("Game", WorkType.Client);
			}
			return;
		}
		m_characterSkinsCache.GetTexture(m_playerData.CharacterSkinName);
		m_playerModel.PlayerClass = m_playerData.PlayerClass;
		m_playerModel.CharacterSkinName = m_playerData.CharacterSkinName;
		m_playerModel.BodyTag = m_playerData.Tag;
		m_playerClassButton.Text = m_playerData.PlayerClass.ToString();
		m_bodyTagButton.Text = m_playerData.Tag.ToString();
		if (!m_nameTextBox.HasFocus)
		{
			m_nameTextBox.Text = m_playerData.Name;
		}
		m_characterSkinLabel.Text = CharacterSkinsManager.GetDisplayName(m_playerData.CharacterSkinName);
		m_controlsLabel.Text = GetDeviceDisplayName(m_inputDevices.Where((WidgetInputDevice id) => (id & m_playerData.InputDevice) > WidgetInputDevice.None).FirstOrDefault());
		string text = DatabaseManager.FindValuesDictionaryForComponent(DatabaseManager.FindEntityValuesDictionary(m_playerData.GetEntityTemplateName(), throwIfNotFound: true), typeof(ComponentCreature)).GetValue<string>("Description");
		if (text.StartsWith("[") && text.EndsWith("]"))
		{
			string[] array = text.Substring(1, text.Length - 2).Split(new string[1] { ":" }, StringSplitOptions.RemoveEmptyEntries);
			text = LanguageControl.GetDatabase("Description", array[1]);
		}
		bool flag = false;
		if (m_playerData.Project != null)
		{
			SubsystemGameInfo subsystemGameInfo = m_playerData.Project.FindSubsystem<SubsystemGameInfo>();
			if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				flag = true;
			}
		}
		if (flag)
		{
			m_bodyTagButton.IsEnabled = m_mode != Mode.Edit;
		}
		else
		{
			m_bodyTagButton.IsEnabled = true;
		}
		if (flag)
		{
			string text2 = m_playerData.Tag.ToString();
			string text3 = null;
			switch (text2)
			{
			case "Dwarf":
				text3 = "HP: 1  |  Damage: 0  |  Mining Speed x3  |  Height: 1 Block";
				break;
			case "Short":
				text3 = "Wall Climbing  |  Evasion 50% (Agile)";
				break;
			case "Fat":
				text3 = "HP x2  |  Damage x2  |  Knockback x3";
				break;
			}
			if (!string.IsNullOrEmpty(text3))
			{
				text = text + "\n\n>>> " + text3 + " <<<";
			}
		}
		m_descriptionLabel.Text = text;
		PlayerData playerData = ((m_playerData.SubsystemPlayers != null) ? m_playerData.SubsystemPlayers.MainPlayerData : null);
		m_isAdmin = playerData?.ServerManager ?? false;
		m_isTargetSelf = playerData != null && playerData.PlayerGUID == m_playerData.PlayerGUID;
		bool isAdmin = m_isAdmin;
		bool num = m_isAdmin && !m_isTargetSelf && CommonLib.WorkType == WorkType.Server;
		if (m_adminPanel != null)
		{
			m_adminPanel.IsVisible = isAdmin;
		}
		if ((m_weManagerCheckbox != null) & isAdmin)
		{
			m_playerData.WorldEditManager = m_weManagerCheckbox.IsChecked;
		}
		WorldSettings worldSettings = null;
		SubsystemGameInfo subsystemGameInfo2 = m_playerData.Project.FindSubsystem<SubsystemGameInfo>();
		if (subsystemGameInfo2 != null)
		{
			worldSettings = subsystemGameInfo2.WorldSettings;
		}
		bool flag2 = worldSettings?.RequirePassword ?? false;
		if (num)
		{
			Guid playerGuid = m_playerData.PlayerGUID;
			string item = playerGuid.ToString();
			bool isMuted = m_playerData.SubsystemPlayers.NoMsgPlayerGuidList.Contains(item);
			bool isBanned = m_playerData.SubsystemPlayers.IsGuidBanned(playerGuid);
			bool flag3 = m_playerData.ComponentPlayer != null || m_playerData.Client != null;
			if (m_resetPasswordButton != null)
			{
				bool flag4 = false;
				if (flag2)
				{
					try
					{
						flag4 = PlayerPasswordRepository.HasEntry(playerGuid);
					}
					catch (InvalidOperationException)
					{
						flag4 = false;
					}
				}
				m_resetPasswordButton.IsVisible = flag2 & flag4;
				m_resetPasswordButton.IsEnabled = flag2 & flag4;
				if ((flag2 & flag4) && m_resetPasswordButton.IsClicked)
				{
					DialogsManager.Confirm(LanguageControl.Get("PlayerScreen", "confirm_resetpassword").Replace("{0}", m_playerData.Name), (MessageDialogButton btn) =>
					{
						if (btn == MessageDialogButton.Button1)
						{
							PlayerPasswordRepository.Remove(playerGuid);
						}
					});
				}
			}
			if (m_muteButton != null)
			{
				m_muteButton.IsVisible = true;
				m_muteButton.Text = (isMuted ? LanguageControl.Get("PlayerScreen", "Unmute") : LanguageControl.Get("PlayerScreen", "Mute"));
				if (m_muteButton.IsClicked)
				{
					DialogsManager.Confirm(LanguageControl.Get("PlayerScreen", isMuted ? "confirm_unmute" : "confirm_mute").Replace("{0}", m_playerData.Name), (MessageDialogButton btn) =>
					{
						if (btn == MessageDialogButton.Button1)
						{
							if (isMuted)
							{
								m_playerData.SubsystemPlayers.RemoveNoMsgList(m_playerData);
							}
							else
							{
								m_playerData.SubsystemPlayers.AddNoMsgList(m_playerData);
							}
						}
					});
				}
			}
			if (m_banButton != null)
			{
				m_banButton.IsVisible = true;
				m_banButton.Text = (isBanned ? LanguageControl.Get("PlayerScreen", "Unban") : LanguageControl.Get("PlayerScreen", "Ban"));
				if (m_banButton.IsClicked)
				{
					DialogsManager.Confirm(LanguageControl.Get("PlayerScreen", isBanned ? "confirm_unban" : "confirm_ban").Replace("{0}", m_playerData.Name), (MessageDialogButton btn) =>
					{
						if (btn == MessageDialogButton.Button1)
						{
							if (isBanned)
							{
								if (m_playerData.SubsystemPlayers.IsGuidBanned(playerGuid))
								{
									m_playerData.SubsystemPlayers.RemoveBlackList(m_playerData);
									DialogsManager.Alert(LanguageControl.Get("SubsystemGameWidgets", "unblacklist_success").Replace("{0}", m_playerData.Name));
								}
							}
							else
							{
								m_playerData.SubsystemPlayers.AddBlackList(m_playerData);
							}
						}
					});
				}
			}
			if (m_kickButton != null)
			{
				m_kickButton.IsVisible = true;
				m_kickButton.IsEnabled = flag3;
				if (m_kickButton.IsClicked & flag3)
				{
					DialogsManager.Confirm(LanguageControl.Get("PlayerScreen", "confirm_kick").Replace("{0}", m_playerData.Name), (MessageDialogButton btn) =>
					{
						if (btn == MessageDialogButton.Button1)
						{
							IClient clientByGUID = CommonLib.Net.GetClientByGUID(m_playerData.PlayerGUID);
							if (clientByGUID != null)
							{
								CommonLib.Net.RemoveClient(clientByGUID);
							}
						}
					});
				}
			}
		}
		else
		{
			if (m_resetPasswordButton != null)
			{
				m_resetPasswordButton.IsVisible = false;
			}
			if (m_muteButton != null)
			{
				m_muteButton.IsVisible = false;
			}
			if (m_banButton != null)
			{
				m_banButton.IsVisible = false;
			}
			if (m_kickButton != null)
			{
				m_kickButton.IsVisible = false;
			}
		}
		if (m_playerClassButton.IsClicked)
		{
			m_playerData.PlayerClass = ((m_playerData.PlayerClass == PlayerClass.Male) ? PlayerClass.Female : PlayerClass.Male);
			m_playerData.RandomizeCharacterSkin();
			if (m_playerData.IsDefaultName)
			{
				m_playerData.ResetName();
				if ((m_mode == Mode.Initial || m_mode == Mode.Add) && !string.IsNullOrEmpty(SettingsManager.PlayerName))
				{
					m_playerData.Name = SettingsManager.PlayerName;
				}
			}
		}
		if (m_bodyTagButton.IsClicked)
		{
			int length = Enum.GetValues(typeof(PlayerData.PlayerTag)).Length;
			int tag = (int)m_playerData.Tag;
			tag = (tag + 1) % length;
			m_playerData.Tag = (PlayerData.PlayerTag)tag;
			m_bodyTagButton.Text = m_playerData.Tag.ToString();
		}
		if (m_characterSkinButton.IsClicked)
		{
			CharacterSkinsManager.UpdateCharacterSkinsList();
			IEnumerable<string> items = CharacterSkinsManager.CharacterSkinsNames.Where((string n) => CharacterSkinsManager.GetPlayerClass(n) == m_playerData.PlayerClass || !CharacterSkinsManager.GetPlayerClass(n).HasValue);
			ListSelectionDialog dialog = new ListSelectionDialog(LanguageControl.Get(fName, 1), items, 64f, (object obj) =>
			{
				XElement node = ContentManager.Get<XElement>("Widgets/CharacterSkinItem");
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node, null);
				Texture2D texture = m_characterSkinsCache.GetTexture((string)obj);
				containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Text").Text = CharacterSkinsManager.GetDisplayName((string)obj);
				containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Details").Text = $"{texture.Width}x{texture.Height}";
				PlayerModelWidget playerModelWidget = containerWidget.Children.Find<PlayerModelWidget>("CharacterSkinItem.Model");
				playerModelWidget.PlayerClass = m_playerData.PlayerClass;
				playerModelWidget.CharacterSkinTexture = texture;
				return containerWidget;
			}, (object obj) =>
			{
				m_playerData.CharacterSkinName = (string)obj;
				if (m_playerData.IsDefaultName)
				{
					m_playerData.ResetName();
					if ((m_mode == Mode.Initial || m_mode == Mode.Add) && !string.IsNullOrEmpty(SettingsManager.PlayerName))
					{
						m_playerData.Name = SettingsManager.PlayerName;
					}
				}
			});
			DialogsManager.ShowDialog(null, dialog);
		}
		if (m_controlsButton.IsClicked)
		{
			DialogsManager.ShowDialog(null, new ListSelectionDialog(LanguageControl.Get(fName, 2), m_inputDevices, 56f, (object d) => GetDeviceDisplayName((WidgetInputDevice)d), (object d) =>
			{
				WidgetInputDevice widgetInputDevice = (WidgetInputDevice)d;
				m_playerData.InputDevice = widgetInputDevice;
				foreach (PlayerData playersDatum in m_playerData.SubsystemPlayers.PlayersData)
				{
					if (playersDatum != m_playerData && (playersDatum.InputDevice & widgetInputDevice) != 0)
					{
						playersDatum.InputDevice &= ~widgetInputDevice;
					}
				}
			}));
		}
		if (m_addButton.IsClicked && VerifyName())
		{
			if (CommonLib.WorkType > WorkType.Local)
			{
				DialogsManager.Alert("Operation not allowed");
			}
			else
			{
				m_playerData.SubsystemPlayers.AddPlayerData(m_playerData);
				ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
			}
		}
		if (m_deleteButton.IsClicked)
		{
			if (CommonLib.WorkType == WorkType.Server)
			{
				DialogsManager.Confirm("Are you sure you want to kick this player?", (MessageDialogButton btn) =>
				{
					IClient clientByGUID = CommonLib.Net.GetClientByGUID(m_playerData.PlayerGUID);
					if (btn == MessageDialogButton.Button1)
					{
						CommonLib.Net.RemoveClient(clientByGUID);
					}
					else
					{
						DialogsManager.HideAllDialogs();
					}
				});
			}
			else if (CommonLib.WorkType == WorkType.Client)
			{
				DialogsManager.Alert("Operation not possible");
			}
			else
			{
				DialogsManager.Confirm(LanguageControl.Get(fName, 3), (MessageDialogButton btn) =>
				{
					if (btn == MessageDialogButton.Button1)
					{
						m_playerData.SubsystemPlayers.RemovePlayerData(m_playerData);
						ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
					}
				});
			}
		}
		if (m_playButton.IsClicked && VerifyName() && m_mode == Mode.Initial)
		{
			if (CommonLib.WorkType == WorkType.Client)
			{
				WorldSettings worldSettings2 = m_playerData.Project.FindSubsystem<SubsystemGameInfo>().WorldSettings;
				if (worldSettings2.RunServer && worldSettings2.RequirePassword && string.IsNullOrWhiteSpace(SettingsManager.OnlinePassword))
				{
					DialogsManager.Alert("This server requires your personal password. Enter it in the 'Player ID' button.");
					return;
				}
				CommonLib.Net.QueuePackage(new PlayerDataPackage(m_playerData, PlayerDataPackage.DataType.Create));
				DialogsManager.ShowDialog(this, m_fetchingPlayerDialog);
			}
			else
			{
				m_playerData.SetMain();
				m_playerData.SubsystemPlayers.AddPlayerData(m_playerData);
				ScreensManager.SwitchScreen("Game");
			}
		}
		if (m_addAnotherButton.IsVisible && m_addAnotherButton.IsClicked && VerifyName())
		{
			m_playerData.SubsystemPlayers.AddPlayerData(m_playerData);
			ScreensManager.SwitchScreen("Player", Mode.Initial, m_playerData.SubsystemPlayers.Project);
		}
		if ((Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) && VerifyName())
		{
			if (m_mode == Mode.Initial)
			{
				GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
				GameManager.DisposeProject();
				CommonLib.Net.Stop();
				ScreensManager.SwitchScreen("MainMenu");
			}
			else if (m_mode == Mode.Add || m_mode == Mode.Edit)
			{
				ScreensManager.SwitchScreen("Players", m_playerData.SubsystemPlayers);
			}
			if (m_mode == Mode.Edit)
			{
				CommonLib.Net.QueuePackage(new PlayerDataPackage(m_playerData, PlayerDataPackage.DataType.Modify));
			}
		}
		m_nameWasInvalid = false;
	}

	public static string GetDeviceDisplayName(WidgetInputDevice device)
	{
		return device switch
		{
			WidgetInputDevice.Keyboard | WidgetInputDevice.Mouse => LanguageControl.Get(fName, 4), 
			WidgetInputDevice.GamePad1 => LanguageControl.Get(fName, 5) + (GamePad.IsConnected(0) ? "" : LanguageControl.Get(fName, 9)), 
			WidgetInputDevice.GamePad2 => LanguageControl.Get(fName, 6) + (GamePad.IsConnected(1) ? "" : LanguageControl.Get(fName, 9)), 
			WidgetInputDevice.GamePad3 => LanguageControl.Get(fName, 7) + (GamePad.IsConnected(2) ? "" : LanguageControl.Get(fName, 9)), 
			WidgetInputDevice.GamePad4 => LanguageControl.Get(fName, 8) + (GamePad.IsConnected(3) ? "" : LanguageControl.Get(fName, 9)), 
			WidgetInputDevice.VrControllers => LanguageControl.Get(fName, 11) + (VrManager.IsVrAvailable ? "" : LanguageControl.Get(fName, 9)), 
			_ => LanguageControl.Get(fName, 10), 
		};
	}

	public bool VerifyName()
	{
		if (m_nameWasInvalid)
		{
			return false;
		}
		if (PlayerData.VerifyName(m_nameTextBox.Text.Trim()))
		{
			return true;
		}
		DialogsManager.Alert(LanguageControl.Get(fName, 12));
		return false;
	}

	static PlayerScreen()
	{
		fName = "PlayerScreen";
	}
}
