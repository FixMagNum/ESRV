using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Game;

public class VersionInfoDialog : Dialog
{
	public ButtonWidget m_okButtonWidget;

	public ButtonWidget m_telegramButtonWidget;

	public LabelWidget m_changelogLabelWidget;

	private static string cachedChangelogText;

	public ButtonWidget m_schubButtonWidget;

	public ButtonWidget m_discordButtonWidget;

	public VersionInfoDialog()
	{
		XElement node = ContentManager.Get<XElement>("Dialogs/VersionInfoDialog");
		LoadContents(this, node);
		m_okButtonWidget = Children.Find<ButtonWidget>("VersionInfoDialog.OK");
		m_telegramButtonWidget = Children.Find<ButtonWidget>("VersionInfoDialog.Telegram");
		m_schubButtonWidget = Children.Find<ButtonWidget>("VersionInfoDialog.schub");
		m_discordButtonWidget = Children.Find<ButtonWidget>("VersionInfoDialog.Discord");
		m_changelogLabelWidget = Children.Find<LabelWidget>("VersionInfoDialog.ChangelogContent");
		if (string.IsNullOrEmpty(cachedChangelogText))
		{
			LoadChangelogFromGoogleDrive();
		}
		else
		{
			m_changelogLabelWidget.Text = cachedChangelogText;
		}
	}

	public override void Update()
	{
		if (Input.Cancel || m_okButtonWidget.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
		if (m_telegramButtonWidget.IsClicked)
		{
			WebBrowserManager.LaunchBrowser("https://t.me/sc_echo_ellihuma");
		}
		if (m_schubButtonWidget.IsClicked)
		{
			WebBrowserManager.LaunchBrowser("https://www.schub.top/ModDetail/902");
		}
		if (m_discordButtonWidget.IsClicked)
		{
			WebBrowserManager.LaunchBrowser("https://discord.gg/HAkgHQTSSb");
		}
	}

	private async void LoadChangelogFromGoogleDrive()
	{
		m_changelogLabelWidget.Text = "Loading changelog...";
		string language = LanguageControl.LName();
		string value = await Task.Run(() => DownloadChangelogFromGoogleDrive(language));
		if (!string.IsNullOrEmpty(value))
		{
			cachedChangelogText = value;
			m_changelogLabelWidget.Text = cachedChangelogText;
		}
		else
		{
			m_changelogLabelWidget.Text = "Failed to load changelog. Check connection.";
		}
	}

	private string DownloadChangelogFromGoogleDrive(string language)
	{
		try
		{
			if (SettingsManager.TranslationProviderState == 0)
			{
				string googleLink;
				string giteeLink;
				switch (language)
				{
				case "ru-RU":
					googleLink = "https://drive.google.com/uc?export=download&id=1WLgrnGnnMBS6-8khI2H8wmzHu6yfFzv1&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20RU.txt?random=" + Guid.NewGuid();
					break;
				case "uk-UA":
					googleLink = "https://drive.google.com/uc?export=download&id=1IGjGJzXbTD7o5z-XQN2T5aPqLnE34i5M&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20UA.txt?random=" + Guid.NewGuid();
					break;
				case "en-US":
					googleLink = "https://drive.google.com/uc?export=download&id=1uiW3t7LacgaXydFr4xSsZAMDDpJquCZZ&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20EN.txt?random=" + Guid.NewGuid();
					break;
				case "zh-CN":
					googleLink = "https://drive.google.com/uc?export=download&id=1q-T3BEPF2wOS7WH0AJvtAE9JUCOqgtMl&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20CN.txt?random=" + Guid.NewGuid();
					break;
				case "es-MX":
					googleLink = "https://drive.google.com/uc?export=download&id=15Vz-Qw2VrVLhxylJop0dah7qcWDyDZfr&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20ES.txt?random=" + Guid.NewGuid();
					break;
				case "pt-BR":
					googleLink = "https://drive.google.com/uc?export=download&id=1KzOrWM1I-G_uwuqw5yeVds3DTwJovyNY&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20PT.txt?random=" + Guid.NewGuid();
					break;
				default:
					googleLink = "https://drive.google.com/uc?export=download&id=1uiW3t7LacgaXydFr4xSsZAMDDpJquCZZ&random=" + Guid.NewGuid();
					giteeLink = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20EN.txt?random=" + Guid.NewGuid();
					break;
				}
				string googleResult = null;
				string giteeResult = null;
				Thread netSimulate = new Thread(() =>
				{
					googleResult = DownloadChangelogByLink(googleLink);
				});
				Thread num = new Thread(() =>
				{
					giteeResult = DownloadChangelogByLink(giteeLink);
				});
				netSimulate.Start();
				num.Start();
				while (netSimulate.IsAlive || num.IsAlive)
				{
					if (!netSimulate.IsAlive && googleResult != null)
					{
						SettingsManager.TranslationProviderState = 1;
						return googleResult;
					}
					if (!num.IsAlive && giteeResult != null)
					{
						SettingsManager.TranslationProviderState = 2;
						return giteeResult;
					}
					Thread.Sleep(10);
				}
				if (googleResult != null)
				{
					SettingsManager.TranslationProviderState = 1;
					return googleResult;
				}
				if (giteeResult != null)
				{
					SettingsManager.TranslationProviderState = 2;
					return giteeResult;
				}
				return null;
			}
			if (SettingsManager.TranslationProviderState == 1)
			{
				return DownloadChangelogByLink(GetGoogleLink(language));
			}
			return DownloadChangelogByLink(GetGiteeLink(language));
		}
		catch
		{
			return null;
		}
	}

	static VersionInfoDialog()
	{
	}

	private string DownloadChangelogByLink(string address)
	{
		try
		{
			using WebClient webClient = new WebClient();
			byte[] bytes = webClient.DownloadData(address);
			return Encoding.UTF8.GetString(bytes).Trim();
		}
		catch
		{
			return null;
		}
	}

	private string GetGoogleLink(string language)
	{
		return language switch
		{
			"ru-RU" => "https://drive.google.com/uc?export=download&id=1WLgrnGnnMBS6-8khI2H8wmzHu6yfFzv1&random=" + Guid.NewGuid(), 
			"uk-UA" => "https://drive.google.com/uc?export=download&id=1IGjGJzXbTD7o5z-XQN2T5aPqLnE34i5M&random=" + Guid.NewGuid(), 
			"en-US" => "https://drive.google.com/uc?export=download&id=1uiW3t7LacgaXydFr4xSsZAMDDpJquCZZ&random=" + Guid.NewGuid(), 
			"zh-CN" => "https://drive.google.com/uc?export=download&id=1q-T3BEPF2wOS7WH0AJvtAE9JUCOqgtMl&random=" + Guid.NewGuid(), 
			"es-MX" => "https://drive.google.com/uc?export=download&id=15Vz-Qw2VrVLhxylJop0dah7qcWDyDZfr&random=" + Guid.NewGuid(), 
			"pt-BR" => "https://drive.google.com/uc?export=download&id=1KzOrWM1I-G_uwuqw5yeVds3DTwJovyNY&random=" + Guid.NewGuid(), 
			_ => "https://drive.google.com/uc?export=download&id=1uiW3t7LacgaXydFr4xSsZAMDDpJquCZZ&random=" + Guid.NewGuid(), 
		};
	}

	private string GetGiteeLink(string language)
	{
		return language switch
		{
			"ru-RU" => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20RU.txt?random=" + Guid.NewGuid(), 
			"uk-UA" => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20UA.txt?random=" + Guid.NewGuid(), 
			"en-US" => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20EN.txt?random=" + Guid.NewGuid(), 
			"zh-CN" => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20CN.txt?random=" + Guid.NewGuid(), 
			"es-MX" => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20ES.txt?random=" + Guid.NewGuid(), 
			"pt-BR" => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20PT.txt?random=" + Guid.NewGuid(), 
			_ => "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/new%20update%20EN.txt?random=" + Guid.NewGuid(), 
		};
	}
}
