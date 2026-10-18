using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Engine;
using SimpleJson;
using XmlUtilities;

namespace Game;

public static class MotdManager
{
	public class Message
	{
		public List<Line> Lines = new List<Line>();
	}

	public class Line
	{
		public float Time;

		public XElement Node;

		public string Text;
	}

	public class Bulletin
	{
		public string Title = string.Empty;

		public string EnTitle = string.Empty;

		public string Time = string.Empty;

		public string Content = string.Empty;

		public string EnContent = string.Empty;
	}

	public static Bulletin m_bulletin;

	public static bool CanShowBulletin;

	public static bool CanDownloadMotd;

	public static Message m_message;

	public static JsonObject UpdateResult;

	public static Message MessageOfTheDay
	{
		get
		{
			return m_message;
		}
		set
		{
			m_message = value;
			MessageOfTheDayUpdated?.Invoke();
		}
	}

	public static event Action MessageOfTheDayUpdated;

	public static void ForceRedownload()
	{
		SettingsManager.MotdLastUpdateTime = DateTime.MinValue;
	}

	public static void Initialize()
	{
		if (VersionsManager.Version != VersionsManager.LastLaunchedVersion)
		{
			ForceRedownload();
		}
	}

	public static void UpdateVersion()
	{
		WebManager.Get(string.Format(SettingsManager.MotdUpdateCheckUrl, VersionsManager.SerializationVersion, VersionsManager.Platform, "1.42", LanguageControl.LName()), null, null, new CancellableProgress(), (byte[] data) =>
		{
			UpdateResult = global::SimpleJson.SimpleJson.DeserializeObject<JsonObject>(Encoding.UTF8.GetString(data));
		}, (Exception ex) =>
		{
			Log.Error("Failed processing Update check. Reason: " + ex.Message);
		});
	}

	public static void DownloadMotd()
	{
		WebManager.Get(GetMotdUrl(), null, null, null, (byte[] result) =>
		{
			try
			{
				string motdLastDownloadedData = UnpackMotd(result);
				MessageOfTheDay = null;
				SettingsManager.MotdLastDownloadedData = motdLastDownloadedData;
				Log.Information("Downloaded MOTD");
			}
			catch (Exception ex)
			{
				Log.Error("Failed processing MOTD string. Reason: " + ex.Message);
			}
		}, (Exception error) =>
		{
			Log.Error("Failed downloading MOTD. Reason: {0}", error.Message);
		});
	}

	public static void Update()
	{
		if (CanDownloadMotd)
		{
			DownloadMotd();
			CanDownloadMotd = false;
		}
		if (MessageOfTheDay != null || string.IsNullOrEmpty(SettingsManager.MotdLastDownloadedData))
		{
			return;
		}
		MessageOfTheDay = ParseMotd(SettingsManager.MotdLastDownloadedData);
		if (MessageOfTheDay == null)
		{
			SettingsManager.MotdLastDownloadedData = string.Empty;
		}
		if (m_bulletin != null && SettingsManager.BulletinTime != m_bulletin.Time)
		{
			if (IsCNLanguageType() && m_bulletin.Title.ToLower() != "null")
			{
				CanShowBulletin = true;
			}
			else if (!IsCNLanguageType() && m_bulletin.EnTitle.ToLower() != "null")
			{
				CanShowBulletin = true;
			}
		}
	}

	public static string UnpackMotd(byte[] data)
	{
		using MemoryStream stream = new MemoryStream(data);
		return new StreamReader(stream).ReadToEnd();
	}

	public static Message ParseMotd(string dataString)
	{
		try
		{
			int key = dataString.IndexOf("<Motd");
			if (key < 0)
			{
				throw new InvalidOperationException("Invalid MOTD data string.");
			}
			int value = dataString.IndexOf("</Motd>");
			if (value >= 0 && value > key)
			{
				value += 7;
			}
			XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(key, value - key), throwOnError: true);
			SettingsManager.MotdUpdatePeriodHours = XmlUtils.GetAttributeValue(xElement, "UpdatePeriodHours", 24);
			SettingsManager.MotdUpdateUrl = XmlUtils.GetAttributeValue(xElement, "UpdateUrl", SettingsManager.MotdUpdateUrl);
			Message message = new Message();
			foreach (XElement item2 in xElement.Elements())
			{
				if (Widget.IsNodeIncludedOnCurrentPlatform(item2))
				{
					Line item = new Line
					{
						Time = XmlUtils.GetAttributeValue<float>(item2, "Time"),
						Node = item2.Elements().FirstOrDefault(),
						Text = item2.Value
					};
					message.Lines.Add(item);
				}
			}
			LoadBulletin(dataString);
			return message;
		}
		catch (Exception ex)
		{
			Log.Warning("Failed extracting MOTD string. Reason: " + ex.Message);
		}
		return null;
	}

	public static void LoadBulletin(string dataString)
	{
		int num = dataString.IndexOf("<Motd2");
		if (num < 0)
		{
			throw new InvalidOperationException("Invalid MOTD data string.");
		}
		int num2 = dataString.IndexOf("</Motd2>");
		if (num2 >= 0 && num2 > num)
		{
			num2 += 8;
		}
		XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(num, num2 - num), throwOnError: true);
		string text = ((!ModsManager.Configs.ContainsKey("Language")) ? "zh-CN" : ModsManager.Configs["Language"]);
		foreach (XElement item in xElement.Elements())
		{
			if (item.Name.LocalName == "Bulletin")
			{
				m_bulletin = new Bulletin();
				m_bulletin.Title = item.Attribute("Title").Value;
				m_bulletin.EnTitle = item.Attribute("EnTitle").Value;
				m_bulletin.Time = text + "$" + item.Attribute("Time").Value;
				m_bulletin.Content = item.Element("Content").Value;
				m_bulletin.EnContent = item.Element("EnContent").Value;
				break;
			}
		}
	}

	public static void ShowBulletin()
	{
		try
		{
			string list = (m_bulletin.Time.Contains("$") ? m_bulletin.Time.Split(new char[1] { '$' }, StringSplitOptions.RemoveEmptyEntries)[1] : string.Empty);
			if (!string.IsNullOrEmpty(list))
			{
				list = (IsCNLanguageType() ? "公告发布时间: " : "Time: ") + list;
			}
			CanShowBulletin = false;
		}
		catch (Exception ex)
		{
			Log.Warning("Failed ShowBulletin. Reason: " + ex.Message);
		}
	}

	public static bool IsCNLanguageType()
	{
		return ((!ModsManager.Configs.ContainsKey("Language")) ? "zh-CN" : ModsManager.Configs["Language"]) == "zh-CN";
	}

	public static string GetMotdUrl()
	{
		string list = ((!ModsManager.Configs.ContainsKey("Language")) ? "zh-CN" : ModsManager.Configs["Language"]);
		return string.Format(SettingsManager.MotdUpdateUrl, VersionsManager.SerializationVersion, list);
	}

	static MotdManager()
	{
		CanShowBulletin = false;
		CanDownloadMotd = true;
		UpdateResult = null;
	}
}
