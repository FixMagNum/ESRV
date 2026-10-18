using System.Xml.Linq;
using Engine;
using Game.NetWork;

namespace Game;

public class PlayerWidget : CanvasWidget
{
	public PlayerData m_playerData;

	public PlayerModelWidget m_playerModel;

	public LabelWidget m_nameLabel;

	public LabelWidget m_detailsLabel;

	public ButtonWidget m_editButton;

	public static string fName = "PlayerWidget";

	public ButtonWidget m_copyGuidButton;

	public PlayerWidget(PlayerData playerData, CharacterSkinsCache characterSkinsCache)
	{
		XElement x = ContentManager.Get<XElement>("Widgets/PlayerWidget");
		LoadContents(this, x);
		m_playerModel = Children.Find<PlayerModelWidget>("PlayerModel");
		m_nameLabel = Children.Find<LabelWidget>("Name");
		m_detailsLabel = Children.Find<LabelWidget>("Details");
		m_editButton = Children.Find<ButtonWidget>("EditButton");
		m_playerModel.CharacterSkinsCache = characterSkinsCache;
		m_playerData = playerData;
		m_copyGuidButton = new BevelledButtonWidget
		{
			Text = "GUID",
			Size = new Vector2(70f, 40f),
			HorizontalAlignment = WidgetAlignment.Far,
			VerticalAlignment = WidgetAlignment.Near,
			Margin = new Vector2(0f, 0f)
		};
		Children.Add(m_copyGuidButton);
	}

	public override void Update()
	{
		SubsystemGameInfo x = m_playerData.SubsystemPlayers.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		SubsystemPlayers y = m_playerData.SubsystemPlayers;
		bool i = y.MainPlayerData?.ServerManager ?? false;
		if (CommonLib.WorkType == WorkType.Client)
		{
			m_editButton.IsEnabled = m_playerData.IsMainPlayer | i;
		}
		m_copyGuidButton.IsVisible = i;
		if (m_copyGuidButton.IsClicked)
		{
			ClipboardManager.ClipboardString = m_playerData.PlayerGUID.ToString();
		}
		m_playerModel.PlayerClass = m_playerData.PlayerClass;
		m_playerModel.CharacterSkinName = m_playerData.CharacterSkinName;
		m_nameLabel.Text = m_playerData.Name;
		m_detailsLabel.Text = m_playerData.PlayerClass.ToString();
		m_detailsLabel.Text += "\n";
		try
		{
			string num3 = LanguageControl.Get(fName, 1);
			m_detailsLabel.Text = string.Format(num3, m_detailsLabel.Text, PlayerScreen.GetDeviceDisplayName(m_playerData.InputDevice));
		}
		catch
		{
			LabelWidget detailsLabel = m_detailsLabel;
			detailsLabel.Text = detailsLabel.Text + "Dev: " + PlayerScreen.GetDeviceDisplayName(m_playerData.InputDevice);
		}
		m_detailsLabel.Text += "\n";
		try
		{
			if (m_playerData.LastSpawnTime >= 0.0)
			{
				string num4 = LanguageControl.Get(fName, 2);
				double flag = (x.TotalElapsedGameTime - m_playerData.LastSpawnTime) / 1200.0;
				m_detailsLabel.Text += string.Format(num4, $"{flag:N1}");
			}
			else
			{
				m_detailsLabel.Text += LanguageControl.Get(fName, 3);
			}
		}
		catch
		{
			m_detailsLabel.Text += ((m_playerData.LastSpawnTime >= 0.0) ? "Online" : "Offline");
		}
		bool num5 = m_playerData.ComponentPlayer != null;
		if (y.BlackPlayerGuidList != null && y.BlackPlayerGuidList.Contains(m_playerData.PlayerGUID.ToString()))
		{
			m_nameLabel.Color = Color.Red;
		}
		else if (num5)
		{
			m_nameLabel.Color = Color.Green;
		}
		else
		{
			m_nameLabel.Color = Color.White;
		}
		m_detailsLabel.Color = new Color(128, 128, 128, 128);
		if (!m_editButton.IsClicked)
		{
			return;
		}
		if (CommonLib.WorkType == WorkType.Local && !m_playerData.IsMainPlayer && !i)
		{
			DialogsManager.ShowDialog(this, new MessageDialog(LanguageControl.Get("PlayerWidgetL", "operation_hint"), LanguageControl.Get("PlayerWidgetL", "self_operation_only"), LanguageControl.Get("PlayerWidgetL", "ok_button"), LanguageControl.Get("PlayerWidgetL", "cancel_button"), (MessageDialogButton a) =>
			{
				DialogsManager.HideAllDialogs();
			}));
		}
		else
		{
			ScreensManager.SwitchScreen("Player", PlayerScreen.Mode.Edit, m_playerData);
		}
	}
}
