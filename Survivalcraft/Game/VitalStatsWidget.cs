using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;

namespace Game;

public class VitalStatsWidget : CanvasWidget
{
	public ComponentPlayer m_componentPlayer;

	public ButtonWidget m_chokeButton;

	public LabelWidget m_titleLabel;

	public LinkWidget m_healthLink;

	public ValueBarWidget m_healthValueBar;

	public LinkWidget m_staminaLink;

	public ValueBarWidget m_staminaValueBar;

	public LinkWidget m_foodLink;

	public ValueBarWidget m_foodValueBar;

	public LinkWidget m_sleepLink;

	public ValueBarWidget m_sleepValueBar;

	public LinkWidget m_temperatureLink;

	public ValueBarWidget m_temperatureValueBar;

	public LinkWidget m_wetnessLink;

	public ValueBarWidget m_wetnessValueBar;

	public LinkWidget m_strengthLink;

	public LabelWidget m_strengthLabel;

	public LinkWidget m_resilienceLink;

	public LabelWidget m_resilienceLabel;

	public LinkWidget m_speedLink;

	public LabelWidget m_speedLabel;

	public LinkWidget m_hungerLink;

	public LabelWidget m_hungerLabel;

	public LinkWidget m_experienceLink;

	public ValueBarWidget m_experienceValueBar;

	public LinkWidget m_insulationLink;

	public LabelWidget m_insulationLabel;

	public static string fName;

	public Widget m_livesPanel;

	public ValueBarWidget m_livesBar;

	public ButtonWidget m_buyLifeButton;

	public LabelWidget m_lifeCostLabel;

	public LabelWidget m_livesOverflowLabel;

	public VitalStatsWidget(ComponentPlayer componentPlayer)
	{
		m_componentPlayer = componentPlayer;
		XElement node = ContentManager.Get<XElement>("Widgets/VitalStatsWidget");
		LoadContents(this, node);
		m_titleLabel = Children.Find<LabelWidget>("TitleLabel");
		m_livesPanel = Children.Find<Widget>("LivesPanel");
		m_livesBar = Children.Find<ValueBarWidget>("LivesBar");
		m_livesOverflowLabel = Children.Find<LabelWidget>("LivesOverflowLabel");
		m_buyLifeButton = Children.Find<ButtonWidget>("BuyLifeButton");
		m_lifeCostLabel = Children.Find<LabelWidget>("LifeCostLabel");
		m_healthLink = Children.Find<LinkWidget>("HealthLink");
		m_healthValueBar = Children.Find<ValueBarWidget>("HealthValueBar");
		m_staminaLink = Children.Find<LinkWidget>("StaminaLink");
		m_staminaValueBar = Children.Find<ValueBarWidget>("StaminaValueBar");
		m_foodLink = Children.Find<LinkWidget>("FoodLink");
		m_foodValueBar = Children.Find<ValueBarWidget>("FoodValueBar");
		m_sleepLink = Children.Find<LinkWidget>("SleepLink");
		m_sleepValueBar = Children.Find<ValueBarWidget>("SleepValueBar");
		m_temperatureLink = Children.Find<LinkWidget>("TemperatureLink");
		m_temperatureValueBar = Children.Find<ValueBarWidget>("TemperatureValueBar");
		m_wetnessLink = Children.Find<LinkWidget>("WetnessLink");
		m_wetnessValueBar = Children.Find<ValueBarWidget>("WetnessValueBar");
		m_chokeButton = Children.Find<ButtonWidget>("ChokeButton");
		m_strengthLink = Children.Find<LinkWidget>("StrengthLink");
		m_strengthLabel = Children.Find<LabelWidget>("StrengthLabel");
		m_resilienceLink = Children.Find<LinkWidget>("ResilienceLink");
		m_resilienceLabel = Children.Find<LabelWidget>("ResilienceLabel");
		m_speedLink = Children.Find<LinkWidget>("SpeedLink");
		m_speedLabel = Children.Find<LabelWidget>("SpeedLabel");
		m_hungerLink = Children.Find<LinkWidget>("HungerLink");
		m_hungerLabel = Children.Find<LabelWidget>("HungerLabel");
		m_experienceLink = Children.Find<LinkWidget>("ExperienceLink");
		m_experienceValueBar = Children.Find<ValueBarWidget>("ExperienceValueBar");
		m_insulationLink = Children.Find<LinkWidget>("InsulationLink");
		m_insulationLabel = Children.Find<LabelWidget>("InsulationLabel");
	}

	public override void Update()
	{
		m_titleLabel.Text = $"{m_componentPlayer.PlayerData.Name}, Level {MathUtils.Floor(m_componentPlayer.PlayerData.Level)} {m_componentPlayer.PlayerData.PlayerClass.ToString()}";
		m_healthValueBar.Value = m_componentPlayer.ComponentHealth.Health;
		m_staminaValueBar.Value = m_componentPlayer.ComponentVitalStats.Stamina;
		m_foodValueBar.Value = m_componentPlayer.ComponentVitalStats.Food;
		m_sleepValueBar.Value = m_componentPlayer.ComponentVitalStats.Sleep;
		m_temperatureValueBar.Value = m_componentPlayer.ComponentVitalStats.Temperature / 24f;
		m_wetnessValueBar.Value = m_componentPlayer.ComponentVitalStats.Wetness;
		m_experienceValueBar.Value = m_componentPlayer.PlayerData.Level - MathUtils.Floor(m_componentPlayer.PlayerData.Level);
		m_strengthLabel.Text = string.Format(CultureInfo.InvariantCulture, "x {0:0.00}", m_componentPlayer.ComponentLevel.StrengthFactor);
		m_resilienceLabel.Text = string.Format(CultureInfo.InvariantCulture, "x {0:0.00}", m_componentPlayer.ComponentLevel.ResilienceFactor);
		m_speedLabel.Text = string.Format(CultureInfo.InvariantCulture, "x {0:0.00}", m_componentPlayer.ComponentLevel.SpeedFactor);
		m_hungerLabel.Text = string.Format(CultureInfo.InvariantCulture, "x {0:0.00}", m_componentPlayer.ComponentLevel.HungerFactor);
		m_insulationLabel.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00;-0.00} clo", m_componentPlayer.ComponentClothing.Insulation);
		WorldSettings worldSettings = m_componentPlayer.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings;
		int num = worldSettings.LivesLimit;
		if (worldSettings.GameMode == GameMode.Cruel)
		{
			num = 1;
		}
		else if (worldSettings.GameMode == GameMode.Embrace)
		{
			num = 7;
		}
		if (num > 0)
		{
			m_livesPanel.IsVisible = true;
			int num2 = num;
			if (worldSettings.GameMode != GameMode.Cruel)
			{
				num2 += m_componentPlayer.PlayerData.ExtraLives;
			}
			int num3 = MathUtils.Max(0, num2 - m_componentPlayer.PlayerData.SpawnsCount + 1);
			int num4 = 10;
			int num5 = MathUtils.Min(num2, num4);
			m_livesBar.BarsCount = num5;
			int num6 = MathUtils.Min(num3, num5);
			if (num5 > 0)
			{
				m_livesBar.Value = (float)num6 / (float)num5;
			}
			else
			{
				m_livesBar.Value = 0f;
			}
			if (num3 > num4 || num2 > num4)
			{
				m_livesOverflowLabel.IsVisible = true;
				int num7 = MathUtils.Max(0, num3 - num4);
				m_livesOverflowLabel.Text = $"+{num7} (Total: {num3})";
			}
			else
			{
				m_livesOverflowLabel.IsVisible = false;
			}
			if (worldSettings.LifeRestoration != LifeRestorationMode.None && worldSettings.GameMode != GameMode.Cruel)
			{
				m_buyLifeButton.IsVisible = true;
				int num8 = 1;
				if (worldSettings.LifeRestoration == LifeRestorationMode.Doubling)
				{
					num8 = (int)MathUtils.Pow(2f, m_componentPlayer.PlayerData.ExtraLives);
				}
				m_lifeCostLabel.Text = num8 + " Lvl";
				if (m_componentPlayer.PlayerData.Level - (float)num8 >= 1f)
				{
					m_buyLifeButton.IsEnabled = true;
					if (m_buyLifeButton.IsClicked)
					{
						m_componentPlayer.ComponentLevel.RemoveLevels(num8);
						m_componentPlayer.PlayerData.ExtraLives++;
						GameManager.SaveProject(waitForCompletion: false, showErrorDialog: true);
						WorldIntegritySystem.SaveIntegrity(m_componentPlayer.Project);
					}
				}
				else
				{
					m_buyLifeButton.IsEnabled = false;
				}
			}
			else
			{
				m_buyLifeButton.IsVisible = false;
			}
		}
		else
		{
			m_livesPanel.IsVisible = false;
		}
		if (m_healthLink.IsClicked)
		{
			HelpTopic topic = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Health");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic.Title, topic.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_staminaLink.IsClicked)
		{
			HelpTopic topic2 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Stamina");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic2.Title, topic2.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_foodLink.IsClicked)
		{
			HelpTopic topic3 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Hunger");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic3.Title, topic3.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_sleepLink.IsClicked)
		{
			HelpTopic topic4 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Sleep");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic4.Title, topic4.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_temperatureLink.IsClicked)
		{
			HelpTopic topic5 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Temperature");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic5.Title, topic5.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_wetnessLink.IsClicked)
		{
			HelpTopic topic6 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Wetness");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic6.Title, topic6.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_strengthLink.IsClicked)
		{
			List<ComponentLevel.Factor> factors = new List<ComponentLevel.Factor>();
			float total = m_componentPlayer.ComponentLevel.CalculateStrengthFactor(factors);
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new LevelFactorDialog(LanguageControl.GetContentWidgets(fName, "Strength"), LanguageControl.GetContentWidgets(fName, 16), factors, total));
		}
		if (m_resilienceLink.IsClicked)
		{
			List<ComponentLevel.Factor> factors2 = new List<ComponentLevel.Factor>();
			float total2 = m_componentPlayer.ComponentLevel.CalculateResilienceFactor(factors2);
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new LevelFactorDialog(LanguageControl.GetContentWidgets(fName, "Resilience"), LanguageControl.GetContentWidgets(fName, 17), factors2, total2));
		}
		if (m_speedLink.IsClicked)
		{
			List<ComponentLevel.Factor> factors3 = new List<ComponentLevel.Factor>();
			float total3 = m_componentPlayer.ComponentLevel.CalculateSpeedFactor(factors3);
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new LevelFactorDialog(LanguageControl.GetContentWidgets(fName, "Speed"), LanguageControl.GetContentWidgets(fName, 18), factors3, total3));
		}
		if (m_hungerLink.IsClicked)
		{
			List<ComponentLevel.Factor> factors4 = new List<ComponentLevel.Factor>();
			float total4 = m_componentPlayer.ComponentLevel.CalculateHungerFactor(factors4);
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new LevelFactorDialog(LanguageControl.GetContentWidgets(fName, "Hunger"), LanguageControl.GetContentWidgets(fName, 19), factors4, total4));
		}
		if (m_experienceLink.IsClicked)
		{
			HelpTopic topic7 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Levels");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic7.Title, topic7.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_insulationLink.IsClicked)
		{
			HelpTopic topic8 = ScreensManager.FindScreen<HelpScreen>("Help").GetTopic("Clothing");
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog(topic8.Title, topic8.Text, LanguageControl.Get("Usual", "ok"), null, new Vector2(700f, 360f), null));
		}
		if (m_chokeButton.IsClicked)
		{
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentHealthPackage(m_componentPlayer.ComponentHealth, null, 0.1f, LanguageControl.GetContentWidgets(fName, "Choked"), ignoreInvulnerability: true, isRequest: true));
			}
			else
			{
				m_componentPlayer.ComponentHealth.Injure(0.1f, null, ignoreInvulnerability: true, LanguageControl.GetContentWidgets(fName, "Suicide"));
			}
		}
	}

	static VitalStatsWidget()
	{
		fName = "VitalStatsWidget";
	}
}
