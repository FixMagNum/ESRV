using System;
using System.Windows.Forms;

namespace Game;

public static class ClipboardManager
{
	public static string ClipboardString
	{
		get
		{
			return Clipboard.GetText();
		}
		set
		{
			try
			{
				Clipboard.SetText(value);
			}
			catch (Exception ex)
			{
				DialogsManager.Alert("复制文本失败：" + ex.Message);
			}
		}
	}
}
