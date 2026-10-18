using System;
using System.Xml.Linq;
using Engine;

namespace Game;

public class ManipulateOfflineTokenDialog : Dialog
{
	public TextBoxWidget m_tokenTextBoxWidget;

	public ButtonWidget m_createButtonWidget;

	public ButtonWidget m_copyButtonWidget;

	public ButtonWidget m_pasteButtonWidget;

	public ButtonWidget m_saveButtonWidget;

	public ButtonWidget m_cancelButtonWidget;

	public TextBoxWidget m_nameTextBoxWidget;

	public TextBoxWidget m_passwordTextBoxWidget;

	private readonly CanvasWidget[] m_slotCanvas = new CanvasWidget[5];

	private readonly ImageWidget[] m_slotImage = new ImageWidget[5];

	private readonly ButtonWidget[] m_slotPlus = new ButtonWidget[5];

	private readonly ButtonWidget[] m_slotBtn = new ButtonWidget[5];

	private PlayerProfiles.Entry[] m_profiles;

	public ManipulateOfflineTokenDialog()
	{
		XElement node = ContentManager.Get<XElement>("Dialogs/ManipulateOfflineTokenDialog");
		LoadContents(this, node);
		m_tokenTextBoxWidget = Children.Find<TextBoxWidget>("ManipulateOfflineTokenDialog.Token");
		m_nameTextBoxWidget = Children.Find<TextBoxWidget>("ManipulateOfflineTokenDialog.Name");
		m_passwordTextBoxWidget = Children.Find<TextBoxWidget>("ManipulateOfflineTokenDialog.Password");
		m_createButtonWidget = Children.Find<ButtonWidget>("ManipulateOfflineTokenDialog.Create");
		m_copyButtonWidget = Children.Find<ButtonWidget>("ManipulateOfflineTokenDialog.Copy");
		m_pasteButtonWidget = Children.Find<ButtonWidget>("ManipulateOfflineTokenDialog.Paste");
		m_saveButtonWidget = Children.Find<ButtonWidget>("ManipulateOfflineTokenDialog.Save");
		m_cancelButtonWidget = Children.Find<ButtonWidget>("ManipulateOfflineTokenDialog.Cancel");
		for (int i = 0; i < 5; i++)
		{
			m_slotCanvas[i] = Children.Find<CanvasWidget>("ProfileSlot" + i, throwIfNotFound: false);
			m_slotImage[i] = Children.Find<ImageWidget>("ProfileSlot" + i + ".Image", throwIfNotFound: false);
			m_slotPlus[i] = Children.Find<ButtonWidget>("ProfileSlot" + i + ".Plus", throwIfNotFound: false);
			m_slotBtn[i] = Children.Find<ButtonWidget>("ProfileSlot" + i + ".Button", throwIfNotFound: false);
		}
		m_tokenTextBoxWidget.Text = SettingsManager.OfflineToken;
		m_nameTextBoxWidget.Text = SettingsManager.PlayerName ?? string.Empty;
		m_passwordTextBoxWidget.Text = SettingsManager.OnlinePassword ?? string.Empty;
		m_profiles = PlayerProfiles.LoadAll();
		RebuildProfilesBar(force: true);
	}

	public override void Update()
	{
		RebuildProfilesBar(force: false);
		string zero = m_tokenTextBoxWidget.Text;
		string text = m_nameTextBoxWidget.Text;
		string text2 = m_passwordTextBoxWidget.Text;
		string clipboardString = ClipboardManager.ClipboardString;
		bool isEnabled = IsOfflineToken(zero);
		m_copyButtonWidget.IsEnabled = isEnabled;
		m_saveButtonWidget.IsEnabled = isEnabled;
		m_pasteButtonWidget.IsEnabled = IsOfflineToken(clipboardString);
		if (m_createButtonWidget.IsClicked)
		{
			m_tokenTextBoxWidget.Text = Guid.NewGuid().ToString();
		}
		if (m_copyButtonWidget.IsClicked)
		{
			ClipboardManager.ClipboardString = zero;
		}
		if (m_pasteButtonWidget.IsClicked)
		{
			m_tokenTextBoxWidget.Text = clipboardString;
		}
		if (m_saveButtonWidget.IsClicked)
		{
			SettingsManager.OfflineToken = zero;
			SettingsManager.PlayerName = text;
			SettingsManager.OnlinePassword = text2;
			DialogsManager.HideDialog(this);
		}
		if (Input.Cancel || m_cancelButtonWidget.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}

	private bool IsOfflineToken(string text)
	{
		if (string.IsNullOrEmpty(text) || text.Length != 36 || text[8] != '-' || text[13] != '-' || text[18] != '-' || text[23] != '-')
		{
			return false;
		}
		int num = 0;
		foreach (char value in text)
		{
			if ("0123456789ABCDEFabcdef".IndexOf(value) >= 0)
			{
				num++;
			}
		}
		return num == 32;
	}

	private void RebuildProfilesBar(bool force)
	{
		int num = PlayerProfiles.Count(m_profiles);
		int num2 = PlayerProfiles.FirstEmpty(m_profiles);
		for (int i = 0; i < 5; i++)
		{
			if (m_slotCanvas[i] == null)
			{
				continue;
			}
			ImageWidget imageWidget = m_slotImage[i];
			ButtonWidget buttonWidget = m_slotPlus[i];
			ButtonWidget buttonWidget2 = m_slotBtn[i];
			bool flag = i < m_profiles.Length && !m_profiles[i].IsEmpty;
			bool flag2 = i == num2 && num < 5;
			if (buttonWidget != null)
			{
				buttonWidget.IsVisible = flag2;
				buttonWidget.IsEnabled = flag2;
			}
			if (buttonWidget2 != null)
			{
				buttonWidget2.IsVisible = flag;
				buttonWidget2.IsEnabled = flag;
				if (flag)
				{
					buttonWidget2.Size = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
				}
			}
			if (imageWidget != null)
			{
				try
				{
					if (flag && !string.IsNullOrEmpty(m_profiles[i].PhotoPath) && Storage.FileExists(m_profiles[i].PhotoPath))
					{
						if (!string.Equals(imageWidget.SourcePath, m_profiles[i].PhotoPath, StringComparison.OrdinalIgnoreCase))
						{
							imageWidget.SetSource(m_profiles[i].PhotoPath);
						}
						imageWidget.IsVisible = true;
					}
					else
					{
						imageWidget.SetSource(null);
						imageWidget.IsVisible = false;
					}
					imageWidget.ConfigureFilter(quantize16: false, 1, 0, null);
				}
				catch
				{
					imageWidget.SetSource(null);
					imageWidget.IsVisible = false;
				}
			}
			if (flag2 && buttonWidget != null && buttonWidget.IsClicked)
			{
				PlayerProfiles.Entry entry = new PlayerProfiles.Entry
				{
					Token = m_tokenTextBoxWidget.Text,
					Name = m_nameTextBoxWidget.Text,
					Password = m_passwordTextBoxWidget.Text,
					PhotoPath = null
				};
				PlayerProfiles.SetAt(ref m_profiles, i, entry);
				PlayerProfiles.SaveAll(m_profiles);
				DialogsManager.ShowDialog(this, new PlayerProfileDialog(i, entry, (int idx, PlayerProfiles.Entry e) =>
				{
					if (e.IsEmpty)
					{
						PlayerProfiles.ClearAt(ref m_profiles, idx);
					}
					else
					{
						PlayerProfiles.SetAt(ref m_profiles, idx, e);
					}
					PlayerProfiles.SaveAll(m_profiles);
					RebuildProfilesBar(force: true);
				}, (PlayerProfiles.Entry entry3) =>
				{
					m_tokenTextBoxWidget.Text = entry3.Token ?? "";
					m_nameTextBoxWidget.Text = entry3.Name ?? "";
					m_passwordTextBoxWidget.Text = entry3.Password ?? "";
				}));
				RebuildProfilesBar(force: true);
				break;
			}
			if (!flag || buttonWidget2 == null || !buttonWidget2.IsClicked)
			{
				continue;
			}
			int num3 = i;
			PlayerProfiles.Entry entry2 = m_profiles[num3];
			DialogsManager.ShowDialog(this, new PlayerProfileDialog(num3, entry2, (int idx, PlayerProfiles.Entry e) =>
			{
				if (e.IsEmpty)
				{
					PlayerProfiles.ClearAt(ref m_profiles, idx);
				}
				else
				{
					PlayerProfiles.SetAt(ref m_profiles, idx, e);
				}
				PlayerProfiles.SaveAll(m_profiles);
				RebuildProfilesBar(force: true);
			}, (PlayerProfiles.Entry entry3) =>
			{
				m_tokenTextBoxWidget.Text = entry3.Token ?? "";
				m_nameTextBoxWidget.Text = entry3.Name ?? "";
				m_passwordTextBoxWidget.Text = entry3.Password ?? "";
			}));
			break;
		}
	}
}
