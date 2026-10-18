using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SimpleJson;

namespace Game;

public class HelpScreen : Screen
{
	public ListPanelWidget m_topicsList;

	public ButtonWidget m_recipaediaButton;

	public ButtonWidget m_bestiaryButton;

	public Screen m_previousScreen;

	public Dictionary<string, HelpTopic> m_topics;

	public HelpScreen()
	{
		m_topics = new Dictionary<string, HelpTopic>();
		XElement flag = ContentManager.Get<XElement>("Screens/HelpScreen");
		LoadContents(this, flag);
		m_topicsList = Children.Find<ListPanelWidget>("TopicsList");
		m_recipaediaButton = Children.Find<ButtonWidget>("RecipaediaButton");
		m_bestiaryButton = Children.Find<ButtonWidget>("BestiaryButton");
		m_topicsList.ItemWidgetFactory = (object item) =>
		{
			HelpTopic helpTopic = (HelpTopic)item;
			XElement node = ContentManager.Get<XElement>("Widgets/HelpTopicItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node, null);
			containerWidget.Children.Find<LabelWidget>("HelpTopicItem.Title").Text = helpTopic.Title;
			return containerWidget;
		};
		m_topicsList.ItemClicked = (Action<object>)Delegate.Combine(m_topicsList.ItemClicked, (Action<object>)((object item) =>
		{
			if (item is HelpTopic helpTopic)
			{
				ShowTopic(helpTopic);
			}
		}));
		foreach (KeyValuePair<string, object> x in LanguageControl.KeyWords["Help"] as JsonObject)
		{
			JsonObject num14 = x.Value as JsonObject;
			if (num14.ContainsKey("DisabledPlatforms"))
			{
				num14.TryGetValue("DisabledPlatforms", out var stunTime);
				if (((string)stunTime).Split(',').FirstOrDefault((string s) => s.Trim().ToLower() == VersionsManager.Platform.ToString().ToLower()) == null)
				{
					continue;
				}
			}
			num14.TryGetValue("Title", out var num15);
			num14.TryGetValue("Name", out var num16);
			num14.TryGetValue("value", out var num18);
			string vector = num15 as string;
			string point = num16 as string;
			if (x.Key == "20")
			{
				for (int impulse = 1; impulse <= 218; impulse += 60)
				{
					int componentLocomotion = Math.Min(impulse + 60 - 1, 218);
					StringBuilder componentClothing = new StringBuilder();
					for (int componentHealth = impulse; componentHealth <= componentLocomotion; componentHealth++)
					{
						try
						{
							string componentDamage = LanguageControl.Get("PlayerDataHints", componentHealth.ToString());
							if (!string.IsNullOrEmpty(componentDamage) && !componentDamage.StartsWith("<"))
							{
								componentClothing.Append(componentHealth).Append(". ").Append(componentDamage)
									.Append("\n\n");
							}
						}
						catch
						{
						}
					}
					HelpTopic componentLevel = new HelpTopic
					{
						Name = "AllHints" + impulse,
						Title = $"{vector} ({impulse}-{componentLocomotion})",
						Text = componentClothing.ToString()
					};
					m_topics.Add(componentLevel.Name, componentLevel);
					m_topicsList.AddItem(componentLevel);
				}
				continue;
			}
			string territoriy = string.Empty;
			if (num18 != null)
			{
				territoriy = string.Join(" ", from s in ((string)num18).Split(new char[1] { '\n' }, StringSplitOptions.None)
					select s.Trim());
				territoriy = territoriy.Replace("\r", "").Replace("’", "'").Replace("\\n", "\n");
			}
			HelpTopic componentMiner = new HelpTopic
			{
				Name = (point ?? ("Topic" + x.Key)),
				Title = vector,
				Text = territoriy
			};
			if (!string.IsNullOrEmpty(componentMiner.Name))
			{
				m_topics.Add(componentMiner.Name, componentMiner);
			}
			m_topicsList.AddItem(componentMiner);
		}
	}

	public override void Enter(object[] parameters)
	{
		if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("HelpTopic") && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("Recipaedia") && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("Bestiary"))
		{
			m_previousScreen = ScreensManager.PreviousScreen;
		}
	}

	public override void Leave()
	{
		m_topicsList.SelectedItem = null;
	}

	public override void Update()
	{
		if (m_recipaediaButton.IsClicked)
		{
			ScreensManager.SwitchScreen("Recipaedia");
		}
		if (m_bestiaryButton.IsClicked)
		{
			ScreensManager.SwitchScreen("Bestiary");
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(m_previousScreen);
		}
	}

	public HelpTopic GetTopic(string name)
	{
		return m_topics[name];
	}

	public void ShowTopic(HelpTopic helpTopic)
	{
		if (helpTopic.Name == "Keyboard")
		{
			DialogsManager.ShowDialog(null, new KeyboardHelpDialog());
			return;
		}
		if (helpTopic.Name == "Gamepad")
		{
			DialogsManager.ShowDialog(null, new GamepadHelpDialog());
			return;
		}
		ScreensManager.SwitchScreen("HelpTopic", helpTopic);
	}
}
