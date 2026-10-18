using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Media;
using Game.NetWork;

namespace Game;

public class PlayersScreen : Screen
{
	public enum Mode
	{
		Online,
		Banned,
		Offline
	}

	public StackPanelWidget m_playersPanel;

	public ButtonWidget m_addPlayerButton;

	public ButtonWidget m_screenLayoutButton;

	public SubsystemPlayers m_subsystemPlayers;

	public CharacterSkinsCache m_characterSkinsCache = new CharacterSkinsCache();

	public Mode m_mode;

	public ButtonWidget m_bannedPlayersButton;

	public ButtonWidget m_offlinePlayersButton;

	public const int PageSize = 20;

	private string m_previousSearchText = string.Empty;

	public TextBoxWidget m_searchBox;

	public LabelWidget m_searchPlaceholder;

	public ButtonWidget m_searchButton;

	public ButtonWidget m_searchPasteButton;

	public ButtonWidget m_prevPageButton;

	public ButtonWidget m_nextPageButton;

	public LabelWidget m_pageLabel;

	public int m_pageIndex;

	public int m_totalPlayersCount;

	public PlayersScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/PlayersScreen");
		LoadContents(this, node);
		m_playersPanel = Children.Find<StackPanelWidget>("PlayersPanel");
		m_addPlayerButton = Children.Find<ButtonWidget>("AddPlayerButton");
		m_screenLayoutButton = Children.Find<ButtonWidget>("ScreenLayoutButton");
		m_bannedPlayersButton = Children.Find<ButtonWidget>("BannedPlayersButton", throwIfNotFound: false);
		m_offlinePlayersButton = Children.Find<ButtonWidget>("OfflinePlayersButton", throwIfNotFound: false);
		m_searchBox = Children.Find<TextBoxWidget>("SearchBox", throwIfNotFound: false);
		m_searchPlaceholder = Children.Find<LabelWidget>("SearchPlaceholder", throwIfNotFound: false);
		m_searchButton = Children.Find<ButtonWidget>("SearchButton", throwIfNotFound: false);
		m_searchPasteButton = Children.Find<ButtonWidget>("SearchPasteButton", throwIfNotFound: false);
		m_prevPageButton = Children.Find<ButtonWidget>("PrevPageButton", throwIfNotFound: false);
		m_nextPageButton = Children.Find<ButtonWidget>("NextPageButton", throwIfNotFound: false);
		m_pageLabel = Children.Find<LabelWidget>("PageLabel", throwIfNotFound: false);
	}

	public override void Enter(object[] parameters)
	{
		m_subsystemPlayers = (SubsystemPlayers)parameters[0];
		m_mode = Mode.Online;
		if (parameters.Length >= 2 && parameters[1] is Mode)
		{
			m_mode = (Mode)parameters[1];
		}
		m_pageIndex = 0;
		m_subsystemPlayers.PlayerAdded += PlayersChanged;
		m_subsystemPlayers.PlayerRemoved += PlayersChanged;
		UpdatePlayersPanel();
		m_addPlayerButton.IsVisible = CommonLib.WorkType == WorkType.Local && m_mode == Mode.Online;
		m_screenLayoutButton.IsVisible = CommonLib.WorkType == WorkType.Local && m_mode == Mode.Online;
		if (m_bannedPlayersButton != null)
		{
			bool isVisible = m_subsystemPlayers.MainPlayerData?.ServerManager ?? false;
			m_bannedPlayersButton.IsVisible = isVisible;
		}
		if (m_offlinePlayersButton != null)
		{
			bool isVisible2 = m_subsystemPlayers.MainPlayerData?.ServerManager ?? false;
			m_offlinePlayersButton.IsVisible = isVisible2;
		}
		if (m_addPlayerButton != null)
		{
			m_addPlayerButton.IsVisible = false;
			m_addPlayerButton.IsEnabled = false;
		}
	}

	public override void Leave()
	{
		if (m_subsystemPlayers != null)
		{
			m_subsystemPlayers.PlayerAdded -= PlayersChanged;
			m_subsystemPlayers.PlayerRemoved -= PlayersChanged;
			m_subsystemPlayers = null;
		}
		if (m_characterSkinsCache != null)
		{
			try
			{
				m_characterSkinsCache.Clear();
			}
			catch (Exception ex)
			{
				Log.Warning("Ошибка при очистке CharacterSkinsCache: " + ex.Message);
			}
		}
		if (m_playersPanel != null && m_playersPanel.Children != null)
		{
			m_playersPanel.Children.Clear();
		}
	}

	public override void Update()
	{
		if (m_searchPlaceholder != null && m_searchBox != null)
		{
			m_searchPlaceholder.IsVisible = string.IsNullOrEmpty(m_searchBox.Text);
		}
		if (m_searchPasteButton != null && m_searchPasteButton.IsClicked && m_searchBox != null)
		{
			string clipboardString = ClipboardManager.ClipboardString;
			if (!string.IsNullOrEmpty(clipboardString))
			{
				m_searchBox.Text = clipboardString.Trim();
			}
		}
		if (m_searchButton != null && m_searchButton.IsClicked)
		{
			m_pageIndex = 0;
			UpdatePlayersPanel();
		}
		if (m_prevPageButton != null && m_prevPageButton.IsClicked && m_pageIndex > 0)
		{
			m_pageIndex--;
			UpdatePlayersPanel();
		}
		if (m_searchBox != null)
		{
			string text = (m_searchBox.Text ?? string.Empty).Trim();
			if (text != m_previousSearchText)
			{
				m_previousSearchText = text;
				m_pageIndex = 0;
				UpdatePlayersPanel();
			}
		}
		if (m_nextPageButton != null && m_nextPageButton.IsClicked)
		{
			int num = (m_totalPlayersCount + 20 - 1) / 20;
			if (num <= 0)
			{
				num = 1;
			}
			if (m_pageIndex < num - 1)
			{
				m_pageIndex++;
				UpdatePlayersPanel();
			}
		}
		if (m_addPlayerButton != null && m_addPlayerButton.IsVisible && m_addPlayerButton.IsClicked)
		{
			if (CommonLib.WorkType != WorkType.Local)
			{
				DialogsManager.Alert("不可进行操作");
			}
			else
			{
				SubsystemGameInfo subsystemGameInfo = m_subsystemPlayers.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
				if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel)
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Unavailable, "不可在参考模式添加玩家", LanguageControl.Ok, null, null));
				}
				else if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure)
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Unavailable, "不可在冒险模式添加玩家", LanguageControl.Ok, null, null));
				}
				else if (m_subsystemPlayers.PlayersData.Count >= ProjectNet.subsystemGameInfo.WorldSettings.MaxOnlinePlayerCount)
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Unavailable, $"超出最大玩家数量{ProjectNet.subsystemGameInfo.WorldSettings.MaxOnlinePlayerCount}", LanguageControl.Ok, null, null));
				}
				else
				{
					ScreensManager.SwitchScreen("Player", PlayerScreen.Mode.Add, m_subsystemPlayers.Project);
				}
			}
		}
		if (m_screenLayoutButton.IsClicked)
		{
			ScreenLayout[] array = null;
			if (m_subsystemPlayers.PlayersData.Count == 1)
			{
				array = new ScreenLayout[1];
			}
			else if (m_subsystemPlayers.PlayersData.Count == 2)
			{
				array = new ScreenLayout[3]
				{
					ScreenLayout.DoubleVertical,
					ScreenLayout.DoubleHorizontal,
					ScreenLayout.DoubleOpposite
				};
			}
			else if (m_subsystemPlayers.PlayersData.Count == 3)
			{
				array = new ScreenLayout[4]
				{
					ScreenLayout.TripleVertical,
					ScreenLayout.TripleHorizontal,
					ScreenLayout.TripleEven,
					ScreenLayout.TripleOpposite
				};
			}
			else if (m_subsystemPlayers.PlayersData.Count == 4)
			{
				array = new ScreenLayout[2]
				{
					ScreenLayout.Quadruple,
					ScreenLayout.QuadrupleOpposite
				};
			}
			if (array != null)
			{
				DialogsManager.ShowDialog(null, new ListSelectionDialog("Select Screen Layout", array, 80f, (object o) =>
				{
					string text2 = o.ToString();
					string name = "Textures/Atlas/ScreenLayout" + text2;
					return new StackPanelWidget
					{
						Direction = LayoutDirection.Horizontal,
						VerticalAlignment = WidgetAlignment.Center,
						Children = 
						{
							(Widget)new RectangleWidget
							{
								Size = new Vector2(98f, 56f),
								Subtexture = ContentManager.Get<Subtexture>(name),
								FillColor = Color.White,
								OutlineColor = Color.Transparent,
								Margin = new Vector2(10f, 0f)
							},
							(Widget)new StackPanelWidget
							{
								Direction = LayoutDirection.Vertical,
								VerticalAlignment = WidgetAlignment.Center,
								Margin = new Vector2(10f, 0f),
								Children = 
								{
									(Widget)new LabelWidget
									{
										Text = StringsManager.GetString("ScreenLayout." + text2 + ".Name"),
										Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
									},
									(Widget)new LabelWidget
									{
										Text = StringsManager.GetString("ScreenLayout." + text2 + ".Description"),
										Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
										Color = Color.Gray
									}
								}
							}
						}
					};
				}, (object o) =>
				{
					if (o != null)
					{
						if (m_subsystemPlayers.PlayersData.Count == 1)
						{
							SettingsManager.ScreenLayout1 = (ScreenLayout)o;
						}
						if (m_subsystemPlayers.PlayersData.Count == 2)
						{
							SettingsManager.ScreenLayout2 = (ScreenLayout)o;
						}
						if (m_subsystemPlayers.PlayersData.Count == 3)
						{
							SettingsManager.ScreenLayout3 = (ScreenLayout)o;
						}
						if (m_subsystemPlayers.PlayersData.Count == 4)
						{
							SettingsManager.ScreenLayout4 = (ScreenLayout)o;
						}
					}
				}));
			}
		}
		if (m_bannedPlayersButton != null && m_bannedPlayersButton.IsClicked)
		{
			ScreensManager.SwitchScreen("Players", m_subsystemPlayers, Mode.Banned);
		}
		if (m_offlinePlayersButton != null && m_offlinePlayersButton.IsClicked)
		{
			ScreensManager.SwitchScreen("Players", m_subsystemPlayers, Mode.Offline);
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen("Game");
		}
	}

	public void UpdatePlayersPanel()
	{
		m_playersPanel.Children.Clear();
		IEnumerable<PlayerData> enumerable;
		if (m_mode != Mode.Banned)
		{
			enumerable = (IEnumerable<PlayerData>)((m_mode != Mode.Offline) ? ((object)m_subsystemPlayers.PlayersData) : m_subsystemPlayers.AllPlayersData.Where((PlayerData p) => p.ComponentPlayer == null));
		}
		else
		{
			List<string> blackList = m_subsystemPlayers.BlackPlayerGuidList;
			enumerable = m_subsystemPlayers.AllPlayersData.Where((PlayerData p) =>
			{
				string item = p.PlayerGUID.ToString();
				return blackList.Contains(item) && p.ComponentPlayer == null;
			});
		}
		string text = string.Empty;
		if (m_searchBox != null)
		{
			text = (m_searchBox.Text ?? string.Empty).Trim();
		}
		List<PlayerData> list;
		if (!string.IsNullOrEmpty(text))
		{
			bool flag = IsGuidString(text);
			bool flag2 = IsFingerprintString(text);
			list = new List<PlayerData>();
			foreach (PlayerData item2 in enumerable)
			{
				bool flag3 = false;
				if (flag)
				{
					if (string.Equals(item2.PlayerGUID.ToString(), text, StringComparison.OrdinalIgnoreCase))
					{
						flag3 = true;
					}
				}
				else if (flag2)
				{
					if (!string.IsNullOrEmpty(item2.CurrentHardwareId) && string.Equals(item2.CurrentHardwareId, text, StringComparison.OrdinalIgnoreCase))
					{
						flag3 = true;
					}
				}
				else if (string.Equals(item2.Name ?? string.Empty, text, StringComparison.OrdinalIgnoreCase))
				{
					flag3 = true;
				}
				if (flag3)
				{
					list.Add(item2);
				}
			}
		}
		else
		{
			list = enumerable.ToList();
		}
		m_totalPlayersCount = list.Count;
		int num = (m_totalPlayersCount + 20 - 1) / 20;
		if (num <= 0)
		{
			num = 1;
		}
		if (m_pageIndex >= num)
		{
			m_pageIndex = num - 1;
		}
		if (m_pageIndex < 0)
		{
			m_pageIndex = 0;
		}
		if (m_pageLabel != null)
		{
			m_pageLabel.Text = $"{m_pageIndex + 1}/{num}";
		}
		if (m_prevPageButton != null)
		{
			m_prevPageButton.IsEnabled = m_pageIndex > 0;
		}
		if (m_nextPageButton != null)
		{
			m_nextPageButton.IsEnabled = m_pageIndex < num - 1;
		}
		int num2 = m_pageIndex * 20;
		int num3 = MathUtils.Min(num2 + 20, m_totalPlayersCount);
		for (int num4 = num2; num4 < num3; num4++)
		{
			m_playersPanel.Children.Add(new PlayerWidget(list[num4], m_characterSkinsCache));
		}
	}

	public void PlayersChanged(PlayerData playerData)
	{
		UpdatePlayersPanel();
	}

	private static bool IsGuidString(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		Guid result;
		return Guid.TryParse(text, out result);
	}

	private static bool IsFingerprintString(string text)
	{
		if (string.IsNullOrEmpty(text) || text.Length != 40)
		{
			return false;
		}
		foreach (char c in text)
		{
			if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
			{
				return false;
			}
		}
		return true;
	}
}
