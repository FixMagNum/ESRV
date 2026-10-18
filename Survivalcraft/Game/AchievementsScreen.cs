using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;

namespace Game;

public class AchievementsScreen : Screen
{
	private ListPanelWidget m_achievementsList;

	private Screen m_prev;

	private PlayerAchievements m_playerAchievements;

	public AchievementsScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/AchievementsScreen");
		LoadContents(this, node);
		m_achievementsList = Children.Find<ListPanelWidget>("AchievementsList");
		m_achievementsList.ItemWidgetFactory = (object item) =>
		{
			AchievementInfo achievementInfo = (AchievementInfo)item;
			XElement node2 = ContentManager.Get<XElement>("Widgets/AchievementItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
			BlockIconWidget blockIconWidget = containerWidget.Children.Find<BlockIconWidget>("AchievementItem.Icon");
			LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("AchievementItem.Name");
			LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("AchievementItem.Description");
			if (achievementInfo.Texture != null)
			{
				blockIconWidget.IsVisible = false;
				Texture2D texture = ContentManager.Get<Texture2D>(achievementInfo.Texture);
				float scale = blockIconWidget.Scale;
				RectangleWidget widget = new RectangleWidget
				{
					Subtexture = new Subtexture(texture, Vector2.Zero, Vector2.One),
					Size = blockIconWidget.Size * scale,
					HorizontalAlignment = blockIconWidget.HorizontalAlignment,
					VerticalAlignment = blockIconWidget.VerticalAlignment,
					Margin = blockIconWidget.Margin,
					FillColor = Color.White,
					OutlineColor = Color.Transparent,
					OutlineThickness = 0f,
					TextureLinearFilter = false
				};
				containerWidget.Children.Insert(containerWidget.Children.IndexOf(blockIconWidget), widget);
			}
			else if (achievementInfo.IconValue.HasValue)
			{
				blockIconWidget.Value = achievementInfo.IconValue.Value;
			}
			else
			{
				blockIconWidget.IsVisible = false;
			}
			labelWidget.Text = achievementInfo.DisplayName ?? achievementInfo.Key;
			labelWidget2.Text = achievementInfo.Description ?? string.Empty;
			switch (achievementInfo.Tier)
			{
			case PlayerAchievements.AchievementTier.Master:
				labelWidget.Color = new Color(180, 140, 255);
				return containerWidget;
			default:
				labelWidget.Color = new Color(255, 224, 96);
				return containerWidget;
			case PlayerAchievements.AchievementTier.Evil:
				labelWidget.Color = new Color(255, 80, 80);
				return containerWidget;
			}
		};
		ListPanelWidget achievementsList = m_achievementsList;
		achievementsList.ItemClicked = (Action<object>)Delegate.Combine(achievementsList.ItemClicked, (Action<object>)((object item) =>
		{
			ScreensManager.SwitchScreen("AchievementDescription", item, m_achievementsList.Items.Cast<AchievementInfo>().ToList());
		}));
		PlayerAchievements playerAchievements = new PlayerAchievements();
		foreach (MethodInfo item in from m in typeof(PlayerAchievements).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where m.Name.StartsWith("Enqueue") && m.Name.EndsWith("Achievement") && m.GetParameters().Length == 0
			select m)
		{
			item.Invoke(playerAchievements, null);
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (FieldInfo achievement in playerAchievements.Achievements)
		{
			dictionary[LanguageControl.Get("PlayerAchievements", achievement.Name + "Name")] = LanguageControl.Get("PlayerAchievements", achievement.Name + "LargeDescription");
		}
		List<AchievementInfo> list = new List<AchievementInfo>();
		while (playerAchievements.AchievementQueue.Count > 0)
		{
			PlayerAchievements.AchievementDisplay achievementDisplay = playerAchievements.AchievementQueue.Dequeue();
			list.Add(new AchievementInfo
			{
				Key = achievementDisplay.Name,
				Completed = false,
				DisplayName = achievementDisplay.Name,
				Description = achievementDisplay.Description,
				LargeDescription = (dictionary.ContainsKey(achievementDisplay.Name) ? dictionary[achievementDisplay.Name] : string.Empty),
				IconValue = achievementDisplay.IconValue,
				Texture = achievementDisplay.Texture,
				Tier = achievementDisplay.Tier
			});
		}
		foreach (AchievementInfo item2 in from a in list
			orderby GetTierOrder(a.Tier), a.DisplayName
			select a)
		{
			m_achievementsList.AddItem(item2);
		}
	}

	public override void Enter(object[] p)
	{
		if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("AchievementDescription"))
		{
			m_prev = ScreensManager.PreviousScreen;
		}
		m_playerAchievements = null;
		if (p != null && p.Length != 0)
		{
			m_playerAchievements = p[0] as PlayerAchievements;
		}
		if (m_playerAchievements == null)
		{
			Project project = GameManager.Project;
			if (project != null)
			{
				SubsystemPlayers subsystemPlayers = project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
				SubsystemPlayerAchievements subsystemPlayerAchievements = project.FindSubsystem<SubsystemPlayerAchievements>(throwOnError: true);
				ComponentPlayer componentPlayer = subsystemPlayers?.MainPlayer;
				if (componentPlayer != null && subsystemPlayerAchievements != null)
				{
					m_playerAchievements = subsystemPlayerAchievements.GetPlayerAchievements(componentPlayer.PlayerData.PlayerGUID);
				}
			}
		}
		UpdateListCompletion();
		m_achievementsList.SelectedItem = null;
	}

	public override void Update()
	{
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(m_prev);
		}
	}

	private void UpdateListCompletion()
	{
		if (m_playerAchievements == null)
		{
			return;
		}
		string text = LanguageControl.Get("PlayerAchievements", "Uncompleted");
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		foreach (FieldInfo achievement in m_playerAchievements.Achievements)
		{
			if (!(achievement.FieldType != typeof(long)))
			{
				bool value = (long)achievement.GetValue(m_playerAchievements) > 0;
				string key = LanguageControl.Get("PlayerAchievements", achievement.Name + "Name");
				string value2 = LanguageControl.Get("PlayerAchievements", achievement.Name + "Description");
				dictionary[key] = value;
				dictionary2[key] = value2;
			}
		}
		List<AchievementInfo> list = m_achievementsList.Items.Cast<AchievementInfo>().ToList();
		foreach (AchievementInfo item in list)
		{
			bool flag = (item.Completed = dictionary.TryGetValue(item.Key, out var value3) & value3);
			item.DisplayName = (flag ? item.Key : text);
			ref string description = ref item.Description;
			string text2;
			if (flag)
			{
				text2 = (dictionary2.ContainsKey(item.Key) ? dictionary2[item.Key] : string.Empty);
			}
			else
			{
				text2 = text;
			}
			description = text2;
		}
		m_achievementsList.ClearItems();
		foreach (AchievementInfo item2 in from a in list
			orderby GetTierOrder(a.Tier), a.DisplayName
			select a)
		{
			m_achievementsList.AddItem(item2);
		}
	}

	private static int GetTierOrder(PlayerAchievements.AchievementTier t)
	{
		return t switch
		{
			PlayerAchievements.AchievementTier.Master => 0, 
			PlayerAchievements.AchievementTier.Common => 1, 
			_ => 2, 
		};
	}
}
