using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public class LoadingScreen : Screen
{
	public enum LogType
	{
		Info,
		Warning,
		Error,
		Advice
	}

	private class LogItem
	{
		public LogType LogType;

		public string Message;

		public LogItem(LogType type, string log)
		{
			LogType = type;
			Message = log;
		}
	}

	private List<Action> LoadingActoins = new List<Action>();

	private List<Action> ModLoadingActoins = new List<Action>();

	private CanvasWidget Canvas = new CanvasWidget();

	private RectangleWidget Background = new RectangleWidget
	{
		FillColor = (SettingsManager.DisplayLog ? Color.Black : Color.White),
		OutlineThickness = 0f,
		DepthWriteEnabled = true
	};

	private static ListPanelWidget LogList;

	private bool HasError;

	static LoadingScreen()
	{
		LogList = new ListPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			PlayClickSound = false
		};
		LogList.ItemWidgetFactory = (object obj) =>
		{
			LogItem logItem = obj as LogItem;
			CanvasWidget canvasWidget = new CanvasWidget
			{
				Size = new Vector2(Display.Viewport.Width, 40f),
				Margin = new Vector2(0f, 2f),
				HorizontalAlignment = WidgetAlignment.Near
			};
			FontTextWidget widget = new FontTextWidget
			{
				FontScale = 0.6f,
				Text = logItem.Message,
				Color = GetColor(logItem.LogType),
				VerticalAlignment = WidgetAlignment.Center,
				HorizontalAlignment = WidgetAlignment.Near
			};
			canvasWidget.Children.Add(widget);
			canvasWidget.IsVisible = SettingsManager.DisplayLog;
			LogList.IsEnabled = SettingsManager.DisplayLog;
			return canvasWidget;
		};
		LogList.ItemSize = 30f;
	}

	public static Color GetColor(LogType type)
	{
		return type switch
		{
			LogType.Info => Color.White, 
			LogType.Warning => Color.Yellow, 
			LogType.Error => Color.Red, 
			LogType.Advice => Color.Cyan, 
			_ => Color.White, 
		};
	}

	public LoadingScreen()
	{
		Canvas.Size = new Vector2(float.PositiveInfinity);
		Canvas.AddChildren(Background);
		Canvas.AddChildren(LogList);
		AddChildren(Canvas);
		Info("Initilizing Mods Manager. Api Version: 1.42");
	}

	public void ContentLoaded()
	{
		if (!SettingsManager.DisplayLog)
		{
			ClearChildren();
			RectangleWidget rectangleWidget = new RectangleWidget
			{
				FillColor = Color.White,
				OutlineColor = Color.Transparent,
				Size = new Vector2(256f),
				VerticalAlignment = WidgetAlignment.Center,
				HorizontalAlignment = WidgetAlignment.Center
			};
			rectangleWidget.Subtexture = ContentManager.Get<Subtexture>("Textures/Gui/CandyRufusLogo");
			RectangleWidget rectangleWidget2 = new RectangleWidget
			{
				FillColor = Color.White,
				OutlineColor = Color.Transparent,
				Size = new Vector2(80f),
				VerticalAlignment = WidgetAlignment.Far,
				HorizontalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(10f)
			};
			rectangleWidget2.Subtexture = ContentManager.Get<Subtexture>("Textures/Gui/EngineLogo");
			BusyBarWidget widget = new BusyBarWidget
			{
				VerticalAlignment = WidgetAlignment.Far,
				HorizontalAlignment = WidgetAlignment.Center,
				Margin = new Vector2(0f, 40f)
			};
			Canvas.AddChildren(Background);
			Canvas.AddChildren(rectangleWidget);
			Canvas.AddChildren(rectangleWidget2);
			Canvas.AddChildren(widget);
			Canvas.AddChildren(LogList);
			AddChildren(Canvas);
		}
	}

	public static void Error(string mesg)
	{
		Add(LogType.Error, "[Error]" + mesg);
	}

	public static void Info(string mesg)
	{
		Add(LogType.Info, "[Info]" + mesg);
	}

	public static void Warning(string mesg)
	{
		Add(LogType.Warning, "[Warning]" + mesg);
	}

	public static void Advice(string mesg)
	{
		Add(LogType.Advice, "[Advice]" + mesg);
	}

	public static void Add(LogType type, string mesg)
	{
		Dispatcher.Dispatch(() =>
		{
			LogItem item = new LogItem(type, mesg);
			LogList.AddItem(item);
			switch (type)
			{
			case LogType.Info:
			case LogType.Advice:
				Log.Information(mesg);
				break;
			case LogType.Warning:
				Log.Warning(mesg);
				break;
			case LogType.Error:
				Log.Error(mesg);
				break;
			}
			LogList.ScrollToItem(item);
		});
	}

	private void InitActions()
	{
		AddLoadAction(() =>
		{
			ContentManager.Initialize();
			ModsManager.Initialize();
		});
		AddLoadAction(ContentLoaded);
		AddLoadAction(() =>
		{
			ModsManager.ModListAllDo((ModEntity modEntity) =>
			{
				modEntity.CheckDependencies();
			});
		});
		AddLoadAction(() =>
		{
			ReadOnlyList<ContentInfo> readOnlyList = ContentManager.List("Lang");
			LanguageControl.LanguageTypes.Clear();
			foreach (ContentInfo item in readOnlyList)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.Filename);
				if (!LanguageControl.LanguageTypes.Contains(fileNameWithoutExtension))
				{
					LanguageControl.LanguageTypes.Add(fileNameWithoutExtension);
				}
			}
			if (ModsManager.Configs.ContainsKey("Language"))
			{
				LanguageControl.Initialize(ModsManager.Configs["Language"]);
			}
			else
			{
				LanguageControl.Initialize("en-US");
			}
			ModsManager.ModListAllDo((ModEntity modEntity) =>
			{
				modEntity.LoadLauguage();
			});
		});
		AddLoadAction(() =>
		{
			ModsManager.ModListAllDo((ModEntity modEntity) =>
			{
				modEntity.LoadDll();
			});
		});
		AddLoadAction(() =>
		{
			Info("Execute Initialization Tasks");
			List<Action> actions = new List<Action>();
			ModsManager.ModListAllDo((ModEntity modEntity) =>
			{
				modEntity.Loader?.OnLoadingStart(actions);
			});
			foreach (Action item2 in actions)
			{
				ModLoadingActoins.Add(item2);
			}
		});
		AddLoadAction(() =>
		{
			Info("Initialize The Texture Map");
			TextureAtlasManager.Initialize();
		});
		AddLoadAction(() =>
		{
			try
			{
				DatabaseManager.Initialize();
				ModsManager.ModListAllDo((ModEntity modEntity) =>
				{
					modEntity.LoadXdb(ref DatabaseManager.DatabaseNode);
				});
			}
			catch (Exception ex)
			{
				Warning(ex.Message);
			}
		});
		AddLoadAction(() =>
		{
			Info("Read Database");
			try
			{
				DatabaseManager.LoadDataBaseFromXml(DatabaseManager.DatabaseNode);
			}
			catch (Exception ex)
			{
				Warning(ex.Message);
			}
		});
		AddLoadAction(() =>
		{
			Info("Initialize Block Manager");
			BlocksManager.Initialize();
		});
		AddLoadAction(() =>
		{
			CraftingRecipesManager.Initialize();
		});
		InitScreens();
		AddLoadAction(() =>
		{
			BlocksTexturesManager.Initialize();
			CharacterSkinsManager.Initialize();
			CommunityContentManager.Initialize();
			ExternalContentManager.Initialize();
			FurniturePacksManager.Initialize();
			LightingManager.Initialize();
			MotdManager.Initialize();
			VersionsManager.Initialize();
			WorldsManager.Initialize();
		});
		AddLoadAction(() =>
		{
			Info("Initialize Mod Settings Parameters");
			if (Storage.FileExists("app:/ModSettings.xml"))
			{
				using (Stream stream = Storage.OpenFile("app:/ModSettings.xml", OpenFileMode.Read))
				{
					try
					{
						ModsManager.LoadModSettings(XElement.Load(stream));
					}
					catch (Exception ex)
					{
						Warning(ex.Message);
					}
				}
			}
		});
		AddLoadAction(() =>
		{
			ModsManager.ModListAllDo((ModEntity modEntity) =>
			{
				Info("Wait For The Rest Of The Tasks To Complete:" + modEntity.modInfo?.PackageName);
				modEntity.Loader?.OnLoadingFinished(ModLoadingActoins);
			});
		});
		AddLoadAction(() =>
		{
			ScreensManager.SwitchScreen("Play");
		});
	}

	private void InitScreens()
	{
		AddLoadAction(() =>
		{
			AddScreen("NetPlay", new NetPlayScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("MainMenu", new MainMenuScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Recipaedia", new RecipaediaScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("RecipaediaRecipes", new RecipaediaRecipesScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("RecipaediaDescription", new RecipaediaDescriptionScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Bestiary", new BestiaryScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("BestiaryDescription", new BestiaryDescriptionScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Help", new HelpScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("HelpTopic", new HelpTopicScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Settings", new SettingsScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("SettingsPerformance", new SettingsPerformanceScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("SettingsGraphics", new SettingsGraphicsScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("SettingsUi", new SettingsUiScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("SettingsCompatibility", new SettingsCompatibilityScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("SettingsAudio", new SettingsAudioScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("SettingsControls", new SettingsControlsScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Play", new PlayScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("NewWorld", new NewWorldScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("ModifyWorld", new ModifyWorldScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("WorldOptions", new WorldOptionsScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("GameLoading", new GameLoadingScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Game", new GameScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("ExternalContent", new ExternalContentScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("CommunityContent", new CommunityContentScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Content", new ContentScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("ManageContent", new ManageContentScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("ModsManageContent", new ModsManageContentScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Players", new PlayersScreen());
		});
		AddLoadAction(() =>
		{
			AddScreen("Player", new PlayerScreen());
		});
	}

	public void AddScreen(string name, Screen screen)
	{
		ScreensManager.AddScreen(name, screen);
	}

	private void AddLoadAction(Action action)
	{
		LoadingActoins.Add(action);
	}

	public override void Leave()
	{
		LogList.ClearItems();
		Window.PresentationInterval = SettingsManager.PresentationInterval;
		ContentManager.Dispose("Textures/Gui/CandyRufusLogo");
		ContentManager.Dispose("Textures/Gui/EngineLogo");
	}

	public override void Enter(object[] parameters)
	{
		Window.PresentationInterval = 0;
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, Screen> screen in ScreensManager.m_screens)
		{
			if (screen.Value != this)
			{
				list.Add(screen.Key);
			}
		}
		foreach (string item in list)
		{
			ScreensManager.m_screens.Remove(item);
		}
		InitActions();
		base.Enter(parameters);
	}

	public override void Update()
	{
		if (Input.Back || Input.Cancel)
		{
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Warning, "Quit?", LanguageControl.Ok, LanguageControl.No, (MessageDialogButton vt) =>
			{
				if (vt == MessageDialogButton.Button1)
				{
					Environment.Exit(0);
				}
				else
				{
					DialogsManager.HideAllDialogs();
				}
			}));
		}
		if (!ModsManager.GetAllowContinue())
		{
			return;
		}
		if (ModLoadingActoins.Count > 0)
		{
			try
			{
				ModLoadingActoins[0]();
				return;
			}
			catch (Exception ex)
			{
				Error(ex.Message);
				return;
			}
			finally
			{
				ModLoadingActoins.RemoveAt(0);
			}
		}
		if (LoadingActoins.Count <= 0)
		{
			return;
		}
		try
		{
			LoadingActoins[0]();
		}
		catch (Exception ex2)
		{
			Error(ex2.Message);
		}
		finally
		{
			LoadingActoins.RemoveAt(0);
		}
	}
}
