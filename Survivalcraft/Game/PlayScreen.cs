using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Game.NetWork;

namespace Game;

public class PlayScreen : Screen
{
	public static int ModChunkVer;

	public ListPanelWidget m_worldsListWidget;

	public static int MaxWorlds;

	public static string fName;

	public PlayScreen()
	{
		XElement slotValue = ContentManager.Get<XElement>("Screens/PlayScreen");
		LoadContents(this, slotValue);
		m_worldsListWidget = Children.Find<ListPanelWidget>("WorldsList");
		Children.Find<BevelledButtonWidget>("Play").Text = "Enter world";
		ListPanelWidget worldsListWidget = m_worldsListWidget;
		worldsListWidget.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(worldsListWidget.ItemWidgetFactory, (Func<object, Widget>)((object item) =>
		{
			WorldInfo worldInfo = (WorldInfo)item;
			XElement node = ContentManager.Get<XElement>("Widgets/SavedWorldItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node, null);
			LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("WorldItem.Name");
			LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("WorldItem.Details");
			containerWidget.Tag = worldInfo;
			labelWidget.Text = (worldInfo.WorldSettings.RunServer ? (worldInfo.WorldSettings.Name + " [Server]") : worldInfo.WorldSettings.Name);
			string arg = DataSizeFormatter.Format(worldInfo.Size);
			string arg2 = $"{worldInfo.LastSaveTime.ToLocalTime():dd MMM yyyy HH:mm}";
			string arg3 = ((worldInfo.PlayerInfos.Count > 1) ? string.Format(LanguageControl.GetContentWidgets(fName, 9), worldInfo.PlayerInfos.Count) : string.Format(LanguageControl.GetContentWidgets(fName, 10), 1));
			string text = LanguageControl.Get("GameMode", worldInfo.WorldSettings.GameMode.ToString());
			string text2 = LanguageControl.Get("EnvironmentBehaviorMode", worldInfo.WorldSettings.EnvironmentBehaviorMode.ToString());
			Color color = new Color(128, 0, 32);
			Color color2 = ((worldInfo.WorldSettings.GameMode == GameMode.Embrace) ? color : labelWidget2.Color);
			string text3 = "";
			if (worldInfo.SerializationVersion != VersionsManager.SerializationVersion)
			{
				text3 = " | " + (string.IsNullOrEmpty(worldInfo.SerializationVersion) ? LanguageControl.GetContentWidgets("Usual", "Unknown") : ("(" + worldInfo.SerializationVersion + ")"));
			}
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = labelWidget2.HorizontalAlignment,
				VerticalAlignment = labelWidget2.VerticalAlignment,
				Margin = labelWidget2.Margin,
				IsHitTestVisible = labelWidget2.IsHitTestVisible
			};
			stackPanelWidget.Children.Add(new LabelWidget
			{
				Text = $"{arg} | {arg2} | {arg3} | ",
				Color = labelWidget2.Color,
				FontScale = labelWidget2.FontScale
			});
			stackPanelWidget.Children.Add(new LabelWidget
			{
				Text = text,
				Color = color2,
				FontScale = labelWidget2.FontScale
			});
			stackPanelWidget.Children.Add(new LabelWidget
			{
				Text = " | " + text2 + text3,
				Color = labelWidget2.Color,
				FontScale = labelWidget2.FontScale
			});
			if (worldInfo.SeasonRaw.HasValue && !worldInfo.SeasonError)
			{
				BuildSeasonUi(worldInfo.SeasonRaw.Value, out var mainText, out var mainColor, out var phaseText, out var phaseColor);
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = " | ",
					Color = labelWidget2.Color,
					FontScale = labelWidget2.FontScale
				});
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = mainText,
					Color = mainColor,
					FontScale = labelWidget2.FontScale
				});
				if (!string.IsNullOrEmpty(phaseText))
				{
					stackPanelWidget.Children.Add(new LabelWidget
					{
						Text = " ",
						Color = labelWidget2.Color,
						FontScale = labelWidget2.FontScale
					});
					stackPanelWidget.Children.Add(new LabelWidget
					{
						Text = phaseText,
						Color = phaseColor,
						FontScale = labelWidget2.FontScale
					});
				}
			}
			ContainerWidget parentWidget = labelWidget2.ParentWidget;
			if (parentWidget != null)
			{
				int index = parentWidget.Children.IndexOf(labelWidget2);
				parentWidget.Children.Remove(labelWidget2);
				parentWidget.Children.Insert(index, stackPanelWidget);
			}
			return containerWidget;
		}));
		m_worldsListWidget.ScrollPosition = 0f;
		m_worldsListWidget.ScrollSpeed = 0f;
		ListPanelWidget worldsListWidget2 = m_worldsListWidget;
		worldsListWidget2.ItemClicked = (Action<object>)Delegate.Combine(worldsListWidget2.ItemClicked, (Action<object>)((object clicked) =>
		{
			if (clicked != null && m_worldsListWidget.SelectedItem == clicked)
			{
				Play(clicked);
			}
		}));
	}

	public override void Enter(object[] parameters)
	{
		BusyDialog dialog = new BusyDialog(LanguageControl.GetContentWidgets(fName, 5), null);
		DialogsManager.ShowDialog(null, dialog);
		Task.Run(() =>
		{
			WorldInfo selectedItem = (WorldInfo)m_worldsListWidget.SelectedItem;
			WorldsManager.UpdateWorldsList();
			List<WorldInfo> worldInfos = new List<WorldInfo>(WorldsManager.WorldInfos);
			worldInfos.Sort((WorldInfo w1, WorldInfo w2) => DateTime.Compare(w2.LastSaveTime, w1.LastSaveTime));
			Dispatcher.Dispatch(() =>
			{
				m_worldsListWidget.ClearItems();
				foreach (WorldInfo item in worldInfos)
				{
					m_worldsListWidget.AddItem(item);
				}
				if (selectedItem != null)
				{
					m_worldsListWidget.SelectedItem = worldInfos.FirstOrDefault((WorldInfo wi) => wi.DirectoryName == selectedItem.DirectoryName);
				}
				DialogsManager.HideDialog(dialog);
			});
		});
	}

	public override void Update()
	{
		if (m_worldsListWidget.SelectedItem != null && WorldsManager.WorldInfos.IndexOf((WorldInfo)m_worldsListWidget.SelectedItem) < 0)
		{
			m_worldsListWidget.SelectedItem = null;
		}
		Children.Find<LabelWidget>("TopBar.Label").Text = string.Format(LanguageControl.GetContentWidgets(fName, 6), m_worldsListWidget.Items.Count);
		Children.Find("Play").IsEnabled = m_worldsListWidget.SelectedItem != null;
		Children.Find("Properties").IsEnabled = m_worldsListWidget.SelectedItem != null;
		if (Children.Find<ButtonWidget>("Play").IsClicked && m_worldsListWidget.SelectedItem != null)
		{
			AlertDialog dialog = new AlertDialog("Clarification", "Enter a single world? You can always open it to other players.", "Yes", "No", () =>
			{
				((WorldInfo)m_worldsListWidget.SelectedItem).WorldSettings.RunServer = true;
				Play(m_worldsListWidget.SelectedItem);
			}, DialogsManager.HideAllDialogs);
			DialogsManager.ShowDialog(null, dialog);
		}
		if (Children.Find<ButtonWidget>("NewWorld").IsClicked)
		{
			if (WorldsManager.WorldInfos.Count >= MaxWorlds)
			{
				DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.GetContentWidgets(fName, 7), string.Format(LanguageControl.GetContentWidgets(fName, 8), MaxWorlds), LanguageControl.GetContentWidgets("Usual", "ok"), null, null));
			}
			else
			{
				ScreensManager.SwitchScreen("NewWorld");
				m_worldsListWidget.SelectedItem = null;
			}
		}
		if (Children.Find<ButtonWidget>("Properties").IsClicked && m_worldsListWidget.SelectedItem != null)
		{
			WorldInfo worldInfo = (WorldInfo)m_worldsListWidget.SelectedItem;
			ScreensManager.SwitchScreen("ModifyWorld", worldInfo.DirectoryName, worldInfo.WorldSettings);
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen("MainMenu");
			m_worldsListWidget.SelectedItem = null;
		}
	}

	public void Play(object item)
	{
		DialogsManager.HideAllDialogs();
		WorldInfo worldInfo = (WorldInfo)item;
		new List<ModInfo>();
		if (worldInfo.WorldSettings.RunServer)
		{
			if (CommonLib.StartServer())
			{
				ScreensManager.SwitchScreen("GameLoading", item, null);
			}
			else
			{
				DialogsManager.ShowDialog(this, new MessageDialog("Alert", "Failed to create server: Port is occupied", "Ok", null, (MessageDialogButton txt) =>
				{
					CommonLib.Net.StopImmediate();
					DialogsManager.HideAllDialogs();
				}));
			}
		}
		else
		{
			ScreensManager.SwitchScreen("GameLoading", item, null);
		}
		m_worldsListWidget.SelectedItem = null;
	}

	static PlayScreen()
	{
		ModChunkVer = 1;
		MaxWorlds = 300;
		fName = "PlayScreen";
	}

	private static void BuildSeasonUi(int seasonRaw, out string mainText, out Color mainColor, out string phaseText, out Color phaseColor)
	{
		mainText = "";
		phaseText = "";
		mainColor = Color.White;
		phaseColor = Color.White;
		bool flag = false;
		bool flag2 = false;
		SubsystemWeather.Season season = (SubsystemWeather.Season)seasonRaw;
		switch (seasonRaw)
		{
		case 0:
			flag = true;
			season = SubsystemWeather.Season.Autumn;
			break;
		case 2:
			flag2 = true;
			season = SubsystemWeather.Season.Autumn;
			break;
		case 5:
			flag2 = true;
			season = SubsystemWeather.Season.Winter;
			break;
		case 6:
			flag = true;
			season = SubsystemWeather.Season.Spring;
			break;
		case 8:
			flag2 = true;
			season = SubsystemWeather.Season.Spring;
			break;
		case 9:
			flag = true;
			season = SubsystemWeather.Season.Summer;
			break;
		case 11:
			flag2 = true;
			season = SubsystemWeather.Season.Summer;
			break;
		}
		string text = LanguageControl.Get("Seasons", season.ToString());
		string text2;
		if (flag)
		{
			text2 = LanguageControl.Get("Seasons", "Early");
		}
		else
		{
			text2 = (flag2 ? LanguageControl.Get("Seasons", "Late") : null);
		}
		Color color = season switch
		{
			SubsystemWeather.Season.Autumn => new Color(210, 120, 40), 
			SubsystemWeather.Season.Anabiosis => new Color(90, 140, 220), 
			SubsystemWeather.Season.Winter => new Color(120, 180, 255), 
			SubsystemWeather.Season.Spring => new Color(96, 180, 72), 
			SubsystemWeather.Season.Summer => new Color(240, 200, 60), 
			_ => new Color(200, 200, 200), 
		};
		Color color2 = color;
		if (flag)
		{
			color2 = new Color((byte)(int)((float)(int)color.R + (255f - (float)(int)color.R) * 0.35f), (byte)(int)((float)(int)color.G + (255f - (float)(int)color.G) * 0.35f), (byte)(int)((float)(int)color.B + (255f - (float)(int)color.B) * 0.35f));
		}
		else if (flag2)
		{
			color2 = new Color((byte)(int)((float)(int)color.R * 0.75f), (byte)(int)((float)(int)color.G * 0.75f), (byte)(int)((float)(int)color.B * 0.75f));
		}
		mainText = text;
		mainColor = color;
		phaseText = text2 ?? "";
		phaseColor = color2;
	}
}
