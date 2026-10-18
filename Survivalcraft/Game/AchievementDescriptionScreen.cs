using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public class AchievementDescriptionScreen : Screen
{
	private BlockIconWidget m_icon;

	private LabelWidget m_name;

	private LabelWidget m_desc;

	private ButtonWidget m_left;

	private ButtonWidget m_right;

	private IList<AchievementInfo> m_list;

	private int m_index;

	private LabelWidget m_largeDesc;

	private RectangleWidget m_texRect;

	public AchievementDescriptionScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/AchievementDescriptionScreen");
		LoadContents(this, node);
		m_icon = Children.Find<BlockIconWidget>("Icon");
		m_name = Children.Find<LabelWidget>("Name");
		m_desc = Children.Find<LabelWidget>("Description");
		m_left = Children.Find<ButtonWidget>("Left");
		m_right = Children.Find<ButtonWidget>("Right");
		m_largeDesc = Children.Find<LabelWidget>("LargeDescription");
	}

	public override void Enter(object[] p)
	{
		m_list = (IList<AchievementInfo>)p[1];
		m_index = m_list.IndexOf((AchievementInfo)p[0]);
		Show();
	}

	public override void Update()
	{
		m_left.IsEnabled = m_index > 0;
		m_right.IsEnabled = m_index < m_list.Count - 1;
		if (m_left.IsClicked || Input.Left)
		{
			m_index--;
			Show();
		}
		if (m_right.IsClicked || Input.Right)
		{
			m_index++;
			Show();
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
		}
	}

	private void Show()
	{
		if (m_index < 0 || m_index >= m_list.Count)
		{
			return;
		}
		AchievementInfo achievementInfo = m_list[m_index];
		ContainerWidget parentWidget = m_icon.ParentWidget;
		if (parentWidget == null)
		{
			return;
		}
		if (m_texRect != null)
		{
			ContainerWidget parentWidget2 = m_texRect.ParentWidget;
			if (parentWidget2 != null && parentWidget2.Children.Contains(m_texRect))
			{
				parentWidget2.Children.Remove(m_texRect);
			}
			m_texRect = null;
		}
		if (!parentWidget.Children.Contains(m_icon))
		{
			parentWidget.Children.Add(m_icon);
		}
		if (achievementInfo.IconValue.HasValue)
		{
			m_icon.Value = achievementInfo.IconValue.Value;
			m_icon.IsVisible = true;
		}
		else if (achievementInfo.Texture != null)
		{
			m_icon.IsVisible = false;
			Texture2D texture = ContentManager.Get<Texture2D>(achievementInfo.Texture);
			m_texRect = new RectangleWidget
			{
				Subtexture = new Subtexture(texture, Vector2.Zero, Vector2.One),
				Size = m_icon.Size,
				Margin = Vector2.Zero,
				HorizontalAlignment = m_icon.HorizontalAlignment,
				VerticalAlignment = m_icon.VerticalAlignment,
				FillColor = Color.White,
				OutlineColor = Color.Transparent,
				OutlineThickness = 0f,
				TextureLinearFilter = false,
				IsHitTestVisible = false,
				IsDrawRequired = true
			};
			parentWidget.Children.Insert(parentWidget.Children.IndexOf(m_icon), m_texRect);
		}
		else
		{
			m_icon.IsVisible = false;
		}
		Color color;
		if (achievementInfo.Tier == PlayerAchievements.AchievementTier.Master)
		{
			color = new Color(180, 140, 255);
		}
		else
		{
			color = ((achievementInfo.Tier == PlayerAchievements.AchievementTier.Evil) ? new Color(255, 80, 80) : new Color(255, 224, 96));
		}
		m_name.Color = color;
		m_largeDesc.Color = color;
		m_name.Text = achievementInfo.DisplayName ?? achievementInfo.Key;
		m_desc.Text = achievementInfo.Description ?? string.Empty;
		m_largeDesc.Text = achievementInfo.LargeDescription ?? string.Empty;
	}
}
