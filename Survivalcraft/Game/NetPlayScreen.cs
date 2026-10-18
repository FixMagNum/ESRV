using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Game.NetWork;
using LiteNetLib;

namespace Game;

public class NetPlayScreen : Screen
{
	public enum ConnectState
	{
		Unavaliable,
		Checking,
		Avaliable
	}

	public class ServerList
	{
		public List<Connect> list;
	}

	public class Connect
	{
		public string name;

		public string ip;

		public string version;

		public ushort playerCount;

		public ConnectState state;

		public float TimeOfDay;

		public long ValidTime;

		public long usedTime;

		public GameMode gameMode;

		public bool HasPassword;

		public ushort maxCount;

		public bool IsNeedLoginCommunity;

		public string savedPassword = string.Empty;

		public int? seasonRaw;

		public bool seasonError;

		public bool RequirePersonalPassword;

		public bool IsUndeletable { get; set; }

		public override int GetHashCode()
		{
			return ip.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			Connect connect = (Connect)obj;
			if (connect != null)
			{
				return connect.ip == ip;
			}
			return false;
		}

		public override string ToString()
		{
			return name + " | " + ip;
		}
	}

	public ListPanelWidget m_worldsListWidget;

	public static string fName;

	private List<Thread> runningTasks;

	private bool lookingForServer;

	private List<Connect> localConnects;

	public bool RequirePersonalPassword;

	public NetPlayScreen()
	{
		runningTasks = new List<Thread>();
		localConnects = new List<Connect>();
		XElement num = ContentManager.Get<XElement>("Screens/PlayScreen");
		LoadContents(this, num);
		m_worldsListWidget = Children.Find<ListPanelWidget>("WorldsList");
		Children.Find<ButtonWidget>("NewWorld").Text = LanguageControl.Get(fName, 1);
		Children.Find<ButtonWidget>("Play").Text = LanguageControl.Get(fName, 2);
		Children.Find<ButtonWidget>("Properties").Text = LanguageControl.Get(fName, 3);
		ListPanelWidget worldsListWidget = m_worldsListWidget;
		worldsListWidget.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(worldsListWidget.ItemWidgetFactory, (Func<object, Widget>)((object obj) =>
		{
			Connect connect = (Connect)obj;
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Vertical
			};
			LabelWidget labelWidget = new LabelWidget
			{
				Name = "line1"
			};
			LabelWidget labelWidget2 = new LabelWidget
			{
				Name = "line2"
			};
			StackPanelWidget stackPanelWidget2 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal
			};
			LabelWidget labelWidget3 = new LabelWidget
			{
				Name = "lineSeasonMain",
				Margin = new Vector2(0f, 2f)
			};
			LabelWidget widget = new LabelWidget
			{
				Text = "  "
			};
			LabelWidget labelWidget4 = new LabelWidget
			{
				Name = "lineSeasonPhase",
				Margin = new Vector2(0f, 2f)
			};
			stackPanelWidget2.Children.Add(labelWidget3);
			stackPanelWidget2.Children.Add(widget);
			stackPanelWidget2.Children.Add(labelWidget4);
			StackPanelWidget stackPanelWidget3 = null;
			string arg = (connect.RequirePersonalPassword ? (" | " + LanguageControl.Get(fName, 4)) : (connect.IsNeedLoginCommunity ? (" | " + LanguageControl.Get(fName, 4)) : ""));
			string arg2 = " | " + SubsystemTimeOfDay.GetTimeOfDayText(connect.TimeOfDay);
			switch (connect.state)
			{
			case ConnectState.Unavaliable:
				labelWidget.Text = $"{connect} ({LanguageControl.Get(fName, 9)})";
				labelWidget.Color = Color.LightRed;
				labelWidget2.Text = "";
				labelWidget3.Text = "ошибка";
				labelWidget3.Color = Color.LightRed;
				labelWidget4.IsVisible = false;
				break;
			case ConnectState.Checking:
				labelWidget.Text = $"{connect} ({LanguageControl.Get(fName, 8)})";
				labelWidget.Color = Color.White;
				labelWidget2.Text = "";
				labelWidget3.Text = "";
				labelWidget4.IsVisible = false;
				break;
			case ConnectState.Avaliable:
			{
				labelWidget.Text = $"{connect} ({connect.usedTime / 2:0} ms)";
				labelWidget.Color = Color.LightGreen;
				BuildSeasonUi(connect, out var mainText, out var mainColor, out var phaseText, out var phaseColor, out var error);
				Color color = ((connect.gameMode == GameMode.Embrace) ? new Color(128, 0, 32) : Color.White);
				string text = LanguageControl.Get("GameMode", connect.gameMode.ToString());
				stackPanelWidget3 = new StackPanelWidget
				{
					Direction = LayoutDirection.Horizontal
				};
				Color color2 = ((connect.version == SettingsManager.CurrentModVersion) ? Color.LightGreen : Color.LightRed);
				LabelWidget widget2 = new LabelWidget
				{
					Text = $"{connect.version} | {LanguageControl.Get(fName, 6)}: {connect.playerCount}/{connect.maxCount} | {LanguageControl.Get(fName, 7)}: ",
					Color = color2
				};
				LabelWidget widget3 = new LabelWidget
				{
					Text = text,
					Color = color
				};
				LabelWidget widget4 = new LabelWidget
				{
					Text = $"{arg2}{arg}"
				};
				stackPanelWidget3.Children.Add(widget2);
				stackPanelWidget3.Children.Add(widget3);
				stackPanelWidget3.Children.Add(widget4);
				stackPanelWidget3.Children.Add(new LabelWidget
				{
					Text = " | "
				});
				LabelWidget widget5 = new LabelWidget
				{
					Text = (error ? "ошибка" : mainText),
					Color = (error ? Color.LightRed : mainColor)
				};
				stackPanelWidget3.Children.Add(widget5);
				if (!string.IsNullOrEmpty(phaseText))
				{
					stackPanelWidget3.Children.Add(new LabelWidget
					{
						Text = " "
					});
					LabelWidget widget6 = new LabelWidget
					{
						Text = phaseText,
						Color = phaseColor
					};
					stackPanelWidget3.Children.Add(widget6);
				}
				labelWidget2.IsVisible = false;
				stackPanelWidget2.IsVisible = false;
				break;
			}
			}
			stackPanelWidget.Children.Add(labelWidget);
			stackPanelWidget.Children.Add(labelWidget2);
			if (stackPanelWidget3 != null)
			{
				stackPanelWidget.Children.Add(stackPanelWidget3);
			}
			stackPanelWidget.Children.Add(stackPanelWidget2);
			return stackPanelWidget;
		}));
		m_worldsListWidget.ScrollPosition = 0f;
		m_worldsListWidget.ScrollSpeed = 0f;
		ListPanelWidget worldsListWidget2 = m_worldsListWidget;
		worldsListWidget2.ItemClicked = (Action<object>)Delegate.Combine(worldsListWidget2.ItemClicked, new Action<object>(OnItemClick));
	}

	public bool CheckConnectExists(Connect connect, out Connect found)
	{
		found = ModsManager.Connects.Find((Connect x) => x.ip == connect.ip);
		if (found == null)
		{
			found = localConnects.Find((Connect x) => x.ip == connect.ip);
		}
		return found != null;
	}

	public void OnItemClick(object item)
	{
		Connect connect = item as Connect;
		if (connect == null || m_worldsListWidget.SelectedItem == null)
		{
			return;
		}
		if (connect.version != SettingsManager.CurrentModVersion)
		{
			DialogsManager.ShowDialog(null, new MessageDialog("Version Mismatch", "Version mismatch: your version is " + SettingsManager.CurrentModVersion + ". The server requires version: " + connect.version + ".", "ОК", null, null));
			return;
		}
		CommonLib.IsOfflineMode = string.IsNullOrEmpty(SettingsManager.CommunityAccessToken);
		if (CommonLib.IsOfflineMode && connect.IsNeedLoginCommunity)
		{
			DialogsManager.Confirm(LanguageControl.Get(fName, 10), (MessageDialogButton btn) =>
			{
				if (btn == MessageDialogButton.Button1)
				{
					DialogsManager.ShowDialog(null, new LoginDialog());
				}
			});
		}
		else if (connect.HasPassword)
		{
			string text = string.Empty;
			if (CheckConnectExists(connect, out var found))
			{
				found.savedPassword = connect.savedPassword;
				text = found.savedPassword;
			}
			DialogsManager.ShowDialog(this, new TextBoxDialog(LanguageControl.Get(fName, 11), text, 16, (string pwd) =>
			{
				if (!string.IsNullOrEmpty(pwd))
				{
					ConnectTo(connect, pwd);
				}
				else
				{
					DialogsManager.HideAllDialogs();
				}
			}));
		}
		else
		{
			ConnectTo(connect);
		}
	}

	public void ConnectTo(Connect connect, string passwd = null)
	{
		if (!CheckConnectExists(connect, out var _))
		{
			ModsManager.Connects.Add(connect);
		}
		if (CommonLib.Resolve(connect.ip, out var ep))
		{
			DialogsManager.HideAllDialogs();
			ScreensManager.SwitchScreen("GameLoading", null, null, ep, passwd);
		}
		else
		{
			DialogsManager.Alert(LanguageControl.Get(fName, 12));
		}
	}

	public void UpdateList()
	{
		Connect selectedItem = (Connect)m_worldsListWidget.SelectedItem;
		m_worldsListWidget.ClearItems();
		List<Connect> list = new List<Connect>(localConnects);
		list.AddRange(ModsManager.Connects);
		list.Sort((Connect c1, Connect c2) => c2.state - c1.state);
		foreach (Connect item in list)
		{
			m_worldsListWidget.AddItem(item);
		}
		m_worldsListWidget.SelectedItem = selectedItem;
	}

	private static void DiscoverLocalServers(Action<Connect> found, Action end)
	{
		EventBasedNetListener eventBasedNetListener = new EventBasedNetListener();
		NetManager netManager = new NetManager(eventBasedNetListener);
		Action value = netManager.PollEvents;
		try
		{
			netManager.UnconnectedMessagesEnabled = true;
			netManager.Start();
			Window.Frame += value;
			Stopwatch s = Stopwatch.StartNew();
			eventBasedNetListener.NetworkReceiveUnconnectedEvent += (IPEndPoint ep, NetPacketReader r, UnconnectedMessageType ty) =>
			{
				if (ep.Address.AddressFamily == AddressFamily.InterNetwork)
				{
					Connect connect = new Connect();
					Package package = new Package(null, r);
					CommonLib.ReadServerInfo(connect, package.Reader, s, ep, isLocal: true);
					found(connect);
				}
			};
			while (s.ElapsedMilliseconds < 5000)
			{
				netManager.SendBroadcast(new Package(null, PackageFlag.DiscoveryRequest).Writer, SettingsManager.BroadcastPort);
				Thread.Sleep(500);
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
		}
		finally
		{
			netManager.Stop();
			Window.Frame -= value;
			end?.Invoke();
		}
	}

	private static void CheckConnect(Connect c)
	{
		EventBasedNetListener eventBasedNetListener = new EventBasedNetListener();
		NetManager netManager = new NetManager(eventBasedNetListener);
		Action value = netManager.PollEvents;
		try
		{
			netManager.UnconnectedMessagesEnabled = true;
			netManager.Start();
			Window.Frame += value;
			c.state = ConnectState.Checking;
			if (CommonLib.Resolve(c.ip, out var cep))
			{
				Stopwatch s = Stopwatch.StartNew();
				eventBasedNetListener.NetworkReceiveUnconnectedEvent += (IPEndPoint ep, NetPacketReader r, UnconnectedMessageType ty) =>
				{
					if (ep.Address.Equals(cep.Address))
					{
						Package package = new Package(null, r);
						CommonLib.ReadServerInfo(c, package.Reader, s, ep);
					}
				};
				while (s.ElapsedMilliseconds < 5000 && c.state == ConnectState.Checking)
				{
					netManager.SendUnconnectedMessage(new Package(null, PackageFlag.ServerInfoRequest).Writer, cep);
					Thread.Sleep(500);
				}
			}
			if (c.state == ConnectState.Checking)
			{
				c.state = ConnectState.Unavaliable;
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
			c.state = ConnectState.Unavaliable;
		}
		finally
		{
			netManager.Stop();
			Window.Frame -= value;
		}
	}

	private void AddIntoCheckList(Connect connect)
	{
		Thread thread = new Thread(() =>
		{
			CheckConnect(connect);
		})
		{
			IsBackground = true
		};
		thread.Start();
		runningTasks.Add(thread);
	}

	private void RefreshConnects()
	{
		foreach (Connect connect in ModsManager.Connects)
		{
			connect.state = ConnectState.Checking;
			AddIntoCheckList(connect);
		}
		localConnects.Clear();
		lookingForServer = true;
		Task.Run(() =>
		{
			DiscoverLocalServers((Connect c) =>
			{
				if (!CheckConnectExists(c, out var _))
				{
					localConnects.Add(c);
					UpdateList();
				}
			}, () =>
			{
				lookingForServer = false;
			});
		});
		UpdateList();
	}

	public override void Enter(object[] parameters)
	{
		RefreshConnects();
		Task.Run(async () =>
		{
			await LoadServersFromFile();
		});
	}

	public override void Leave()
	{
		for (int i = 0; i < runningTasks.Count; i++)
		{
			try
			{
				runningTasks[i].Abort();
			}
			catch
			{
			}
		}
		runningTasks.Clear();
	}

	public override void Update()
	{
		for (int num = runningTasks.Count - 1; num >= 0; num--)
		{
			if (runningTasks[num].ThreadState == System.Threading.ThreadState.Stopped)
			{
				runningTasks.RemoveAt(num);
				UpdateList();
				break;
			}
		}
		Children.Find<ButtonWidget>("NewWorld").IsEnabled = m_worldsListWidget.SelectedItem != null;
		if (Children.Find<ButtonWidget>("NewWorld").IsClicked && m_worldsListWidget.SelectedItem != null)
		{
			ModsManager.Connects.Remove((Connect)m_worldsListWidget.SelectedItem);
			m_worldsListWidget.RemoveItem(m_worldsListWidget.SelectedItem);
		}
		if (Children.Find<ButtonWidget>("Properties").IsClicked)
		{
			foreach (Connect connect2 in ModsManager.Connects)
			{
				connect2.state = ConnectState.Checking;
			}
			RefreshConnects();
		}
		Children.Find<LabelWidget>("TopBar.Label").Text = LanguageControl.Get(fName, 13) + " (" + ModsManager.Connects.Count + ")" + (lookingForServer ? (" [" + LanguageControl.Get(fName, 14) + "]") : "");
		if (Children.Find<ButtonWidget>("Play").IsClicked)
		{
			DialogsManager.ShowDialog(null, new AddServerDialog((string name, string ip) =>
			{
				Connect connect = new Connect
				{
					name = name,
					ip = ip
				};
				ModsManager.Connects.Add(connect);
				AddIntoCheckList(connect);
				UpdateList();
			}));
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen("MainMenu");
			m_worldsListWidget.SelectedItem = null;
		}
	}

	static NetPlayScreen()
	{
		fName = "NetPlayScreen";
	}

	private async Task LoadServersFromFile()
	{
		try
		{
			string[] array;
			if (SettingsManager.TranslationProviderState != 0)
			{
				array = ((SettingsManager.TranslationProviderState != 1) ? (await DownloadFile("https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/ipadress.txt?random=" + Guid.NewGuid())) : (await DownloadFile("https://drive.google.com/uc?export=download&id=1IWZTXBvMAJwna4LdUL_WIXXnm2DN7bCb&random=" + Guid.NewGuid())));
			}
			else
			{
				string url = "https://drive.google.com/uc?export=download&id=1IWZTXBvMAJwna4LdUL_WIXXnm2DN7bCb&random=" + Guid.NewGuid();
				string url2 = "https://gitee.com/clear-coagulation_1_0/echo-mod/releases/download/MOD/ipadress.txt?random=" + Guid.NewGuid();
				Task<string[]> googleTask = DownloadFile(url);
				Task<string[]> giteeTask = DownloadFile(url2);
				Task<string[]> task = await Task.WhenAny<string[]>(googleTask, giteeTask);
				array = task.Result;
				if (array != null && array.Length != 0)
				{
					if (task == googleTask)
					{
						SettingsManager.TranslationProviderState = 1;
					}
					else
					{
						SettingsManager.TranslationProviderState = 2;
					}
				}
				else
				{
					Task<string[]> otherTask = ((task == googleTask) ? giteeTask : googleTask);
					array = await otherTask;
					if (array != null && array.Length != 0)
					{
						if (otherTask == googleTask)
						{
							SettingsManager.TranslationProviderState = 1;
						}
						else
						{
							SettingsManager.TranslationProviderState = 2;
						}
					}
				}
			}
			if (array == null || array.Length == 0)
			{
				return;
			}
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				Match match = Regex.Match(array2[i], "\"([^\"]+)\"\\s+\"([^\"]+)\"");
				if (match.Success)
				{
					string name = match.Groups[1].Value.Trim();
					string ip = match.Groups[2].Value.Trim();
					Connect connect = new Connect();
					connect.name = name;
					connect.ip = ip;
					connect.state = ConnectState.Checking;
					if (!CheckConnectExists(connect, out var _))
					{
						ModsManager.Connects.Add(connect);
						AddIntoCheckList(connect);
					}
				}
			}
			UpdateList();
		}
		catch (Exception ex)
		{
			Log.Error("Error loading servers: " + ex.Message);
		}
	}

	private async Task<string[]> DownloadFile(string url)
	{
		string[] y;
		try
		{
			using WebClient client = new WebClient();
			y = (await client.DownloadStringTaskAsync(url)).Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		}
		catch (Exception ex)
		{
			Log.Error("Error downloading file: " + ex.Message);
			y = null;
		}
		return y;
	}

	private static void BuildSeasonUi(Connect c, out string mainText, out Color mainColor, out string phaseText, out Color phaseColor, out bool error)
	{
		mainText = "";
		phaseText = "";
		mainColor = Color.White;
		phaseColor = Color.White;
		error = false;
		if (c == null || c.seasonError || !c.seasonRaw.HasValue)
		{
			error = true;
			return;
		}
		SubsystemWeather.Season value = (SubsystemWeather.Season)c.seasonRaw.Value;
		bool flag = false;
		bool flag2 = false;
		SubsystemWeather.Season season = value;
		switch (value)
		{
		case SubsystemWeather.Season.EarlyAutumn:
			flag = true;
			season = SubsystemWeather.Season.Autumn;
			break;
		case SubsystemWeather.Season.LateAutumn:
			flag2 = true;
			season = SubsystemWeather.Season.Autumn;
			break;
		case SubsystemWeather.Season.LateWinter:
			flag2 = true;
			season = SubsystemWeather.Season.Winter;
			break;
		case SubsystemWeather.Season.EarlySpring:
			flag = true;
			season = SubsystemWeather.Season.Spring;
			break;
		case SubsystemWeather.Season.LateSpring:
			flag2 = true;
			season = SubsystemWeather.Season.Spring;
			break;
		case SubsystemWeather.Season.EarlySummer:
			flag = true;
			season = SubsystemWeather.Season.Summer;
			break;
		case SubsystemWeather.Season.LateSummer:
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
