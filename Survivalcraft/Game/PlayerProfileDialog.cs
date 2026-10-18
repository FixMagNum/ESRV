using System;
using System.Xml.Linq;
using Engine;

namespace Game;

public class PlayerProfileDialog : Dialog
{
	private int _index;

	private PlayerProfiles.Entry _entry;

	private readonly Action<int, PlayerProfiles.Entry> _onSaveOrDelete;

	private readonly Action<PlayerProfiles.Entry> _onUse;

	private LabelWidget m_title;

	private LabelWidget m_name;

	private LabelWidget m_token;

	private LabelWidget m_password;

	private ButtonWidget m_photoBtn;

	private ButtonWidget m_delBtn;

	private ButtonWidget m_useBtn;

	private ButtonWidget m_cancel;

	private ImageWidget m_preview;

	public PlayerProfileDialog(int index, PlayerProfiles.Entry entry, Action<int, PlayerProfiles.Entry> onSaveOrDelete, Action<PlayerProfiles.Entry> onUse)
	{
		_index = index;
		_entry = entry;
		_onSaveOrDelete = onSaveOrDelete;
		_onUse = onUse;
		XElement node = ContentManager.Get<XElement>("Dialogs/PlayerProfileDialog");
		LoadContents(this, node);
		m_title = Children.Find<LabelWidget>("ProfileDialog.Title");
		m_name = Children.Find<LabelWidget>("ProfileDialog.Name");
		m_token = Children.Find<LabelWidget>("ProfileDialog.Token");
		m_password = Children.Find<LabelWidget>("ProfileDialog.Password");
		m_photoBtn = Children.Find<ButtonWidget>("ProfileDialog.Photo");
		m_delBtn = Children.Find<ButtonWidget>("ProfileDialog.Delete");
		m_useBtn = Children.Find<ButtonWidget>("ProfileDialog.Use");
		m_cancel = Children.Find<ButtonWidget>("ProfileDialog.Cancel");
		m_preview = Children.Find<ImageWidget>("ProfileDialog.PhotoPreview");
		m_name.Text = "Имя: " + _entry.Name;
		m_token.Text = "ID: " + _entry.Token;
		m_password.Text = "Пароль: " + _entry.Password;
		RefreshPreview();
	}

	public override void Update()
	{
		if (m_photoBtn.IsClicked)
		{
			DialogsManager.ShowDialog(this, new ImageDialog(null, null)
			{
				OnPickProfilePhoto = (string path) =>
				{
					if (!string.IsNullOrEmpty(path))
					{
						_entry.PhotoPath = path;
						RefreshPreview();
						_onSaveOrDelete?.Invoke(_index, _entry);
					}
				}
			});
		}
		if (m_delBtn.IsClicked)
		{
			DialogsManager.ShowDialog(this, new MessageDialog(LanguageControl.Get("ProfileDialog", "1"), LanguageControl.Get("ProfileDialog", "3"), "Да", "Нет", (MessageDialogButton b) =>
			{
				if (b == MessageDialogButton.Button1)
				{
					_entry = default;
					_onSaveOrDelete?.Invoke(_index, _entry);
					DialogsManager.HideDialog(this);
				}
			}));
		}
		if (m_useBtn.IsClicked)
		{
			if (!_entry.IsEmpty)
			{
				_onUse?.Invoke(_entry);
			}
			DialogsManager.HideDialog(this);
		}
		if (Input.Cancel || m_cancel.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}

	private void RefreshPreview()
	{
		try
		{
			if (!string.IsNullOrEmpty(_entry.PhotoPath) && Storage.FileExists(_entry.PhotoPath))
			{
				m_preview.SetSource(_entry.PhotoPath);
			}
			else
			{
				m_preview.SetSource(null);
			}
			if (m_preview != null)
			{
				m_preview.ConfigureFilter(quantize16: false, 1, 0, null);
			}
		}
		catch
		{
			if (m_preview != null)
			{
				m_preview.SetSource(null);
				m_preview.ConfigureFilter(quantize16: false, 1, 0, null);
			}
		}
	}
}
