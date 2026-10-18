using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game;

public class ModsManageContentScreen : Screen
{
	public enum StateFilter
	{
		UninstallState,
		InstallState
	}

	public class ModItem
	{
		public string Name;

		public ModInfo ModInfo;

		public ExternalContentEntry ExternalContentEntry;

		public Subtexture Subtexture;

		public ModItem()
		{
		}

		public ModItem(string Name, ModInfo ModInfo, ExternalContentEntry ExternalContentEntry, Subtexture Subtexture)
		{
			this.Name = Name;
			this.ModInfo = ModInfo;
			this.ExternalContentEntry = ExternalContentEntry;
			this.Subtexture = Subtexture;
		}
	}

	public static string fName = "ModsManageContentScreen";

	public ListPanelWidget m_modsContentList;

	public LabelWidget m_topBarLabel;

	public LabelWidget m_modsContentLabel;

	public LabelWidget m_filterLabel;

	public ButtonWidget m_actionButton;

	public ButtonWidget m_actionButton2;

	public ButtonWidget m_changeFilterButton;

	public ButtonWidget m_upDirectoryButton;

	public StateFilter m_filter;

	public List<ModItem> m_installModList = new List<ModItem>();

	public List<ModItem> m_uninstallModList = new List<ModItem>();

	public List<ModInfo> m_installModInfo = new List<ModInfo>();

	public List<ModInfo> m_lastInstallModInfo = new List<ModInfo>();

	public int m_count;

	public bool androidSystem;

	public string m_path;

	public string m_lastPath;

	public string m_uninstallPath = "app:/ModsCache";

	public string m_installPath = "app:/Mods";

	public bool m_updatable;

	public bool m_firstEnterInstallScreen;

	public bool m_firstEnterScreen;

	public bool m_cancelScan;

	public List<string> m_scanFailPaths = new List<string>();

	public CancellableBusyDialog m_cancellableBusyDialog;

	public List<string> m_commonPathList = new List<string>();

	public string[] m_commonPaths = new string[10] { "android:/Download", "android:/Android/data/com.tencent.mobileqq/Tencent/QQfile_recv", "android:/Android/data/com.tencent.tim/Tencent/TIMfile_recv", "android:tencent/TIMfile_recv", "android:tencent/QQfile_recv", "android:/Quark/Download", "android:/BaiduNetdisk", "android:/UCDownloads", "android:/baidu/searchbox/downloads", "android:/SurvivalCraft2.3/Mods" };

	public ModsManageContentScreen()
	{
		androidSystem = Environment.CurrentDirectory == "/";
		if (androidSystem)
		{
			m_uninstallPath = m_uninstallPath.Replace("app:", "android:/SurvivalCraft2.3");
			m_installPath = m_installPath.Replace("app:", "android:/SurvivalCraft2.3");
		}
		m_updatable = true;
		XElement node = ContentManager.Get<XElement>("Screens/ModsManageContentScreen");
		LoadContents(this, node);
		m_modsContentList = Children.Find<ListPanelWidget>("ModsContentList");
		m_topBarLabel = Children.Find<LabelWidget>("TopBar.Label");
		m_modsContentLabel = Children.Find<LabelWidget>("ModsContentLabel");
		m_filterLabel = Children.Find<LabelWidget>("Filter");
		m_actionButton = Children.Find<ButtonWidget>("ActionButton");
		m_actionButton2 = Children.Find<ButtonWidget>("ActionButton2");
		m_changeFilterButton = Children.Find<ButtonWidget>("ChangeFilter");
		m_upDirectoryButton = Children.Find<ButtonWidget>("UpDirectory");
		m_topBarLabel.Text = LanguageControl.Get(fName, 1);
		m_filterLabel.Text = LanguageControl.Get(fName, 36);
		m_firstEnterScreen = false;
		m_modsContentList.ItemWidgetFactory = (object item) =>
		{
			ModItem modItem = (ModItem)item;
			XElement node2 = ContentManager.Get<XElement>("Widgets/ExternalContentItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
			string text = LanguageControl.Get(fName, 2);
			if (modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
			{
				text = string.Format(LanguageControl.Get(fName, 3), modItem.ModInfo.Version, modItem.ModInfo.Author, MathUtils.Round(modItem.ExternalContentEntry.Size / 1000));
			}
			containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Text").Text = modItem.Name;
			containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Details").Text = text;
			RectangleWidget rectangleWidget = containerWidget.Children.Find<RectangleWidget>("ExternalContentItem.Icon");
			rectangleWidget.Subtexture = modItem.Subtexture;
			rectangleWidget.Size = new Vector2(50f, 50f);
			rectangleWidget.Margin = new Vector2(10f, 10f);
			return containerWidget;
		};
		ListPanelWidget modsContentList = m_modsContentList;
		modsContentList.ItemClicked = (Action<object>)Delegate.Combine(modsContentList.ItemClicked, (Action<object>)((object item) =>
		{
			if (item != null && m_modsContentList.SelectedItem == item)
			{
				ModItem modItem = (ModItem)item;
				if (modItem.ExternalContentEntry.Type == ExternalContentType.Directory && modItem.ExternalContentEntry.Path != m_installPath)
				{
					try
					{
						SetPath(modItem.ExternalContentEntry.Path);
						UpdateListWithBusyDialog();
						return;
					}
					catch
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 5) + "\n" + modItem.ExternalContentEntry.Path, LanguageControl.Get("Usual", "ok"), null, null));
						return;
					}
				}
				if (modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
				{
					if (m_filter == StateFilter.UninstallState)
					{
						string smallMessage = LanguageControl.Get(fName, 6) + modItem.ModInfo.Description + "\n" + LanguageControl.Get(fName, 7) + modItem.ModInfo.PackageName + "，" + LanguageControl.Get(fName, 8);
						DialogsManager.ShowDialog(null, new MessageDialog(modItem.ModInfo.Name, smallMessage, LanguageControl.Get(fName, 9), LanguageControl.Get(fName, 10), (MessageDialogButton result) =>
						{
							if (result == MessageDialogButton.Button1)
							{
								Storage.DeleteFile(modItem.ExternalContentEntry.Path);
								UpdateListWithBusyDialog();
							}
						}));
					}
					else
					{
						string smallMessage2 = LanguageControl.Get(fName, 6) + modItem.ModInfo.Description + "\n" + LanguageControl.Get(fName, 7) + modItem.ModInfo.PackageName;
						DialogsManager.ShowDialog(null, new MessageDialog(modItem.ModInfo.Name, smallMessage2, "更新", "返回", (MessageDialogButton result) =>
						{
							if (result == MessageDialogButton.Button1)
							{
								DialogsManager.ShowDialog(null, new MessageDialog("该功能正在开发中", null, LanguageControl.Ok, null, null));
							}
						}));
					}
				}
			}
		}));
	}

	public override void Enter(object[] parameters)
	{
		if (!Storage.DirectoryExists(m_uninstallPath))
		{
			Storage.CreateDirectory(m_uninstallPath);
		}
		BusyDialog busyDialog = new BusyDialog(LanguageControl.Get(fName, 26), LanguageControl.Get(fName, 32));
		DialogsManager.ShowDialog(null, busyDialog);
		string[] commonPaths = m_commonPaths;
		foreach (string text in commonPaths)
		{
			if ((androidSystem && text.StartsWith("android:")) || (!androidSystem && !text.StartsWith("android:")))
			{
				AddCommonPath(text);
			}
		}
		string path = Storage.CombinePaths(m_uninstallPath, "CommonPaths.txt");
		if (Storage.FileExists(path))
		{
			Stream stream = Storage.OpenFile(path, OpenFileMode.Read);
			StreamReader streamReader = new StreamReader(stream);
			string text2;
			while ((text2 = streamReader.ReadLine()) != null)
			{
				AddCommonPath(text2.Replace("\n", "").Replace("\r", ""));
			}
			stream.Dispose();
		}
		if (!m_firstEnterScreen)
		{
			m_firstEnterScreen = true;
			string text3 = "";
			if (androidSystem)
			{
				bool flag = true;
				string path2 = "android:/Android/data";
				try
				{
					Storage.ListFileNames(path2);
				}
				catch
				{
					flag = false;
				}
				if (!flag)
				{
					text3 += "注意：检测到你的设备不能访问/Android/data目录,无法扫描QQ下载的mod文件，建议用自带浏览器到中文社区下载mod。\n\n";
				}
			}
			text3 += "下载mod后可以进行一次全盘扫描，扫描到mod的目录会自动列入自动扫描目录。";
			if (m_commonPathList.Count > 0)
			{
				text3 += "\n\n以下为进入管理器时自动扫描的目录:";
				for (int j = 0; j < m_commonPathList.Count; j++)
				{
					text3 = text3 + "\n" + (j + 1) + ". " + m_commonPathList[j];
				}
				text3 = text3 + "\n\n" + LanguageControl.Get(fName, 12);
			}
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 14), text3, LanguageControl.Get(fName, 15), null, null));
		}
		Task.Run(() =>
		{
			FastScanModFile(showTips: false);
			SetPath(m_installPath);
			m_filter = StateFilter.InstallState;
			UpdateList();
			SetPath(m_uninstallPath);
			m_filter = StateFilter.UninstallState;
			UpdateList();
			m_updatable = true;
			m_firstEnterInstallScreen = false;
			Dispatcher.Dispatch(() =>
			{
				foreach (ModInfo item in m_installModInfo)
				{
					m_lastInstallModInfo.Add(item);
				}
				if (parameters.Length != 0 && (bool)parameters[0])
				{
					SetPath(m_installPath);
					m_filter = StateFilter.InstallState;
					UpdateList();
				}
				UpdateList(fast: true);
				DialogsManager.HideDialog(busyDialog);
			});
		});
	}

	public override void Leave()
	{
		m_modsContentList.ClearItems();
		m_installModInfo.Clear();
		m_lastInstallModInfo.Clear();
		m_uninstallModList.Clear();
		m_installModList.Clear();
		m_scanFailPaths.Clear();
		if (!Storage.DirectoryExists(m_uninstallPath))
		{
			Storage.CreateDirectory(m_uninstallPath);
		}
		string path = Storage.CombinePaths(m_uninstallPath, "CommonPaths.txt");
		if (m_commonPathList.Count > 0)
		{
			Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
			StreamWriter streamWriter = new StreamWriter(stream);
			foreach (string commonPath in m_commonPathList)
			{
				streamWriter.WriteLine(commonPath);
			}
			streamWriter.Flush();
			stream.Dispose();
		}
		m_commonPathList.Clear();
	}

	public override void Update()
	{
		if (m_filter == StateFilter.InstallState)
		{
			m_upDirectoryButton.IsVisible = false;
			m_actionButton2.IsVisible = false;
		}
		else
		{
			m_upDirectoryButton.IsVisible = true;
			m_upDirectoryButton.IsEnabled = true;
			m_actionButton2.IsVisible = true;
			m_actionButton2.IsEnabled = true;
			m_actionButton2.Text = ((m_path == m_uninstallPath) ? LanguageControl.Get(fName, 16) : LanguageControl.Get(fName, 17));
		}
		ModItem modItem = null;
		if (m_modsContentList.SelectedIndex.HasValue)
		{
			modItem = m_modsContentList.Items[m_modsContentList.SelectedIndex.Value] as ModItem;
		}
		if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
		{
			m_actionButton.Text = ((m_filter == StateFilter.InstallState) ? LanguageControl.Get(fName, 18) : LanguageControl.Get(fName, 19));
			m_actionButton.IsEnabled = true;
		}
		else if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Directory)
		{
			m_actionButton.Text = LanguageControl.Get(fName, 20);
			m_actionButton.IsEnabled = modItem.ExternalContentEntry.Path != "android:/Android";
		}
		else
		{
			m_actionButton.Text = LanguageControl.Get(fName, 21);
			m_actionButton.IsEnabled = false;
		}
		if (m_actionButton.IsClicked)
		{
			if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Mod)
			{
				string fileName = Storage.GetFileName(modItem.ExternalContentEntry.Path);
				string installPathName = Storage.CombinePaths(m_installPath, fileName);
				string uninstallPathName = modItem.ExternalContentEntry.Path;
				if (m_filter == StateFilter.InstallState)
				{
					string smallMessage = LanguageControl.Get(fName, 6) + modItem.ModInfo.Description + "\n" + LanguageControl.Get(fName, 7) + modItem.ModInfo.PackageName + "，" + LanguageControl.Get(fName, 8);
					DialogsManager.ShowDialog(null, new MessageDialog("确定卸载该Mod?", smallMessage, "确定", "取消", (MessageDialogButton result) =>
					{
						if (result == MessageDialogButton.Button1)
						{
							try
							{
								Storage.DeleteFile(installPathName);
								UpdateListWithBusyDialog();
								m_updatable = false;
							}
							catch (Exception ex)
							{
								DialogsManager.ShowDialog(null, new MessageDialog("Mod卸载失败", "发生了异常:" + ex.Message, LanguageControl.Get("Usual", "ok"), null, null));
							}
						}
					}));
				}
				else
				{
					ModInfo samePackmModInfo = null;
					foreach (ModInfo item in m_installModInfo)
					{
						if (item.PackageName == modItem.ModInfo.PackageName)
						{
							samePackmModInfo = item;
						}
					}
					if (!Storage.FileExists(installPathName) && samePackmModInfo == null)
					{
						Storage.CopyFile(uninstallPathName, installPathName);
						m_installModInfo.Add(modItem.ModInfo);
						m_installModList.Add(modItem);
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 23), fileName, LanguageControl.Get("Usual", "ok"), null, null));
					}
					else if (samePackmModInfo != null)
					{
						if (samePackmModInfo.Version == modItem.ModInfo.Version)
						{
							DialogsManager.ShowDialog(null, new MessageDialog("已安装相同的Mod", "安装的Mod版本一致，无需重复安装", LanguageControl.Get("Usual", "ok"), null, null));
						}
						else
						{
							string smallMessage2 = $"当前Mod版本为{modItem.ModInfo.Version},已安装Mod版本为{samePackmModInfo.Version},是否替换";
							DialogsManager.ShowDialog(null, new MessageDialog("已安装相同的Mod", smallMessage2, "替换", "取消", (MessageDialogButton result) =>
							{
								if (result == MessageDialogButton.Button1)
								{
									foreach (ModItem current2 in m_installModList)
									{
										if (current2.ModInfo.PackageName == samePackmModInfo.PackageName)
										{
											try
											{
												Storage.DeleteFile(current2.ExternalContentEntry.Path);
												Storage.CopyFile(uninstallPathName, installPathName);
												m_updatable = true;
												break;
											}
											catch (Exception ex)
											{
												DialogsManager.ShowDialog(null, new MessageDialog("Mod替换失败", "发生了异常:" + ex.Message, LanguageControl.Get("Usual", "ok"), null, null));
												break;
											}
										}
									}
								}
							}));
						}
					}
					else if (Storage.FileExists(installPathName))
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 24), fileName + "文件已存在", LanguageControl.Get("Usual", "ok"), null, null));
					}
				}
			}
			else if (modItem != null && modItem.ExternalContentEntry.Type == ExternalContentType.Directory)
			{
				CancellableBusyDialog busyDialog = new CancellableBusyDialog(LanguageControl.Get(fName, 26), autoHideOnCancel: true);
				ReadyForScan(busyDialog);
				Task.Run(() =>
				{
					string path = modItem.ExternalContentEntry.Path;
					int num = ScanModFile(path, busyDialog);
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 28), string.Format(LanguageControl.Get(fName, 29), num), LanguageControl.Get(fName, 30), LanguageControl.Get(fName, 31), (MessageDialogButton result) =>
					{
						if (result == MessageDialogButton.Button1)
						{
							SetPath(m_uninstallPath);
							UpdateListWithBusyDialog();
						}
					}));
				});
			}
		}
		if (m_actionButton2.IsClicked)
		{
			if (m_path == m_uninstallPath)
			{
				if (m_cancellableBusyDialog != null)
				{
					DialogsManager.ShowDialog(null, m_cancellableBusyDialog);
					return;
				}
				m_cancellableBusyDialog = new CancellableBusyDialog(LanguageControl.Get(fName, 26), autoHideOnCancel: true, CanHideDialog: true);
				ReadyForScan(m_cancellableBusyDialog);
				Task.Run(() =>
				{
					string path;
					if (androidSystem)
					{
						path = "android:";
					}
					else
					{
						string systemPath2 = Storage.GetSystemPath(m_path);
						systemPath2 = systemPath2.Replace("\\", "/");
						int length2 = systemPath2.IndexOf('/');
						path = "system:" + systemPath2.Substring(0, length2) + "/";
					}
					int num = ScanModFile(path, m_cancellableBusyDialog);
					DialogsManager.HideDialog(m_cancellableBusyDialog);
					m_cancellableBusyDialog = null;
					if (num == 0)
					{
						DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 33), LanguageControl.Get(fName, 34), LanguageControl.Get(fName, 10), (MessageDialogButton result) =>
						{
							if (result == MessageDialogButton.Button1)
							{
								ScreensManager.SwitchScreen("CommunityContent", "Mod");
							}
						}));
					}
					else
					{
						string text2 = string.Format(LanguageControl.Get(fName, 35), num);
						if (m_scanFailPaths.Count > 0)
						{
							text2 += "\n\n以下路径无法扫描:\n";
							foreach (string scanFailPath in m_scanFailPaths)
							{
								text2 = text2 + scanFailPath + "\n";
							}
						}
						if (ScreensManager.CurrentScreen == this)
						{
							DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 28), text2, LanguageControl.Get(fName, 30), null, (MessageDialogButton result) =>
							{
								SetPath(m_uninstallPath);
								UpdateListWithBusyDialog();
							}));
						}
						else
						{
							DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 28), text2, LanguageControl.Ok, null, null));
						}
					}
				});
			}
			else
			{
				SetPath(m_uninstallPath);
				UpdateListWithBusyDialog();
			}
		}
		if (m_changeFilterButton.IsClicked)
		{
			if (m_filter == StateFilter.UninstallState)
			{
				m_filter = StateFilter.InstallState;
				SetPath(m_installPath);
				if (!m_firstEnterInstallScreen)
				{
					m_firstEnterInstallScreen = true;
					m_updatable = true;
				}
				UpdateList(fast: true);
			}
			else
			{
				m_filter = StateFilter.UninstallState;
				SetPath(m_uninstallPath);
				UpdateList(fast: true);
			}
		}
		if (m_upDirectoryButton.IsClicked)
		{
			string text = Storage.GetDirectoryName(m_path);
			if (m_path != "android:" && m_path != "app:")
			{
				if (text.StartsWith("system:") && !text.Contains("/"))
				{
					text += "/";
				}
				SetPath(text);
				UpdateListWithBusyDialog();
			}
			else if (m_path == "app:")
			{
				string systemPath = Storage.GetSystemPath(m_path);
				systemPath = systemPath.Replace("\\", "/");
				int length = systemPath.LastIndexOf('/');
				text = "system:" + systemPath.Substring(0, length);
				SetPath(text);
				UpdateListWithBusyDialog();
			}
		}
		if (!Input.Back && !Input.Cancel && !Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			return;
		}
		if (InstallModChange())
		{
			DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), LanguageControl.Get(fName, 38), LanguageControl.Get(fName, 39), LanguageControl.Get(fName, 31), (MessageDialogButton result) =>
			{
				if (result == MessageDialogButton.Button1)
				{
					Environment.Exit(0);
				}
				if (result == MessageDialogButton.Button2)
				{
					ScreensManager.SwitchScreen("Content");
				}
			}));
		}
		else
		{
			ScreensManager.SwitchScreen("Content");
		}
	}

	public void UpdateListWithBusyDialog(bool fast = false)
	{
		BusyDialog busyDialog = new BusyDialog(LanguageControl.Get(fName, 43), null);
		DialogsManager.ShowDialog(null, busyDialog);
		Task.Run(() =>
		{
			UpdateList(fast);
			Dispatcher.Dispatch(() =>
			{
				UpdateList(fast: true);
				DialogsManager.HideDialog(busyDialog);
			});
		});
	}

	public void UpdateList(bool fast = false)
	{
		m_modsContentLabel.Text = LanguageControl.Get(fName, 40) + SetPathText(m_path);
		m_filterLabel.Text = ((m_filter == StateFilter.UninstallState) ? LanguageControl.Get(fName, 36) : LanguageControl.Get(fName, 37));
		if (!fast || m_updatable)
		{
			SetModItemList();
			if (fast)
			{
				m_updatable = false;
			}
		}
		m_modsContentList.ClearItems();
		if (m_filter == StateFilter.InstallState)
		{
			foreach (ModItem installMod in m_installModList)
			{
				m_modsContentList.AddItem(installMod);
			}
			return;
		}
		foreach (ModItem uninstallMod in m_uninstallModList)
		{
			m_modsContentList.AddItem(uninstallMod);
		}
	}

	public void SetModItemList()
	{
		m_updatable = true;
		if (m_filter == StateFilter.InstallState)
		{
			m_installModInfo.Clear();
			m_installModList.Clear();
		}
		else
		{
			m_uninstallModList.Clear();
		}
		foreach (string item in Storage.ListFileNames(m_path))
		{
			string extension = Storage.GetExtension(item);
			if (string.IsNullOrEmpty(extension) || !(extension.ToLower() == ".scmod"))
			{
				continue;
			}
			ModItem modItem = GetModItem(item, IsDirectory: false);
			if (modItem != null && modItem.ModInfo != null && !string.IsNullOrEmpty(modItem.ModInfo.PackageName) && modItem.ModInfo.ApiVersion.Contains("1.4"))
			{
				if (m_filter == StateFilter.InstallState)
				{
					m_installModInfo.Add(modItem.ModInfo);
					m_installModList.Add(modItem);
				}
				else
				{
					m_uninstallModList.Add(modItem);
				}
			}
		}
		foreach (string item2 in Storage.ListDirectoryNames(m_path))
		{
			ModItem modItem2 = GetModItem(item2, IsDirectory: true);
			if (m_filter == StateFilter.InstallState)
			{
				m_installModList.Add(modItem2);
			}
			else
			{
				m_uninstallModList.Add(modItem2);
			}
		}
	}

	public void ReadyForScan(CancellableBusyDialog busyDialog)
	{
		m_cancelScan = false;
		m_scanFailPaths.Clear();
		m_count = 0;
		DialogsManager.ShowDialog(null, busyDialog);
		busyDialog.ShowProgressMessage = false;
		busyDialog.Progress.Cancelled += () =>
		{
			m_cancelScan = true;
		};
	}

	public int ScanModFile(string path, CancellableBusyDialog busyDialog = null)
	{
		string text = path;
		if (m_cancelScan)
		{
			return m_count;
		}
		try
		{
			foreach (string item in Storage.ListFileNames(text))
			{
				if (m_cancelScan)
				{
					return m_count;
				}
				if (text.EndsWith("/"))
				{
					text = path.Substring(0, text.Length - 1);
				}
				if (busyDialog != null)
				{
					string text2 = text;
					if (text.Length > 40)
					{
						text2 = text.Substring(0, 40) + "...";
					}
					busyDialog.SmallMessage = string.Format("已扫描到{0}个有效的MOD文件\n" + text2, m_count);
				}
				if (text == m_uninstallPath)
				{
					continue;
				}
				string extension = Storage.GetExtension(item);
				if (string.IsNullOrEmpty(extension) || !(extension.ToLower() == ".scmod"))
				{
					continue;
				}
				string text3 = Storage.CombinePaths(text, item);
				Stream stream = null;
				ModInfo modInfo = null;
				try
				{
					stream = Storage.OpenFile(text3, OpenFileMode.Read);
					ZipArchive zipArchive = ZipArchive.Open(stream, keepStreamOpen: true);
					foreach (ZipArchiveEntry item2 in zipArchive.ReadCentralDir())
					{
						if (item2.FilenameInZip == "modinfo.json")
						{
							MemoryStream memoryStream = new MemoryStream();
							zipArchive.ExtractFile(item2, memoryStream);
							memoryStream.Position = 0L;
							modInfo = ModsManager.DeserializeJson<ModInfo>(ModsManager.StreamToString(memoryStream));
							memoryStream.Dispose();
							break;
						}
					}
					stream.Dispose();
				}
				catch
				{
				}
				if (stream != null && modInfo != null && !string.IsNullOrEmpty(modInfo.PackageName) && modInfo.ApiVersion.Contains("1.4"))
				{
					string text4 = Storage.CombinePaths(m_uninstallPath, item);
					if (!Storage.FileExists(text4))
					{
						Storage.CopyFile(text3, text4);
						Storage.DeleteFile(text3);
						AddCommonPath(text);
						m_count++;
					}
					stream?.Close();
				}
			}
			foreach (string item3 in Storage.ListDirectoryNames(path))
			{
				if (m_cancelScan)
				{
					return m_count;
				}
				if (text.EndsWith("/"))
				{
					text = path.Substring(0, text.Length - 1);
				}
				string path2 = Storage.CombinePaths(text, item3);
				ScanModFile(path2, busyDialog);
			}
		}
		catch
		{
			m_scanFailPaths.Add(text);
		}
		return m_count;
	}

	public int FastScanModFile(bool showTips = true)
	{
		int num = 0;
		foreach (string commonPath in m_commonPathList)
		{
			if ((!androidSystem || !commonPath.StartsWith("android:")) && (androidSystem || commonPath.StartsWith("android:")))
			{
				continue;
			}
			try
			{
				if (Storage.DirectoryExists(commonPath))
				{
					m_count = 0;
					int num2 = ScanModFile(commonPath);
					num += num2;
				}
			}
			catch
			{
				if (showTips)
				{
					DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 4), string.Format(LanguageControl.Get(fName, 41), commonPath), LanguageControl.Get(fName, 15), null, null));
				}
			}
		}
		return num;
	}

	public ModItem GetModItem(string fileName, bool IsDirectory)
	{
		ModItem num = new ModItem();
		string path = Storage.CombinePaths(m_path, fileName);
		num.Name = fileName;
		num.Subtexture = ExternalContentManager.GetEntryTypeIcon(IsDirectory ? ExternalContentType.Directory : ExternalContentType.Mod);
		num.ExternalContentEntry = new ExternalContentEntry
		{
			Type = (IsDirectory ? ExternalContentType.Directory : ExternalContentType.Mod),
			Path = path,
			Size = (IsDirectory ? 0 : Storage.GetFileSize(path)),
			Time = Storage.GetFileLastWriteTime(path)
		};
		if (IsDirectory)
		{
			return num;
		}
		Stream item = Storage.OpenFile(path, OpenFileMode.Read);
		try
		{
			ZipArchive zipArchive = ZipArchive.Open(item, keepStreamOpen: true);
			foreach (ZipArchiveEntry item2 in zipArchive.ReadCentralDir())
			{
				if (item2.FilenameInZip == "icon.png")
				{
					MemoryStream memoryStream = new MemoryStream();
					zipArchive.ExtractFile(item2, memoryStream);
					memoryStream.Position = 0L;
					num.Subtexture = new Subtexture(Texture2D.Load(memoryStream), Vector2.Zero, Vector2.One);
					memoryStream.Dispose();
				}
				else if (item2.FilenameInZip == "modinfo.json")
				{
					MemoryStream memoryStream2 = new MemoryStream();
					zipArchive.ExtractFile(item2, memoryStream2);
					memoryStream2.Position = 0L;
					num.ModInfo = ModsManager.DeserializeJson<ModInfo>(ModsManager.StreamToString(memoryStream2));
					memoryStream2.Dispose();
				}
			}
		}
		catch
		{
			num = null;
		}
		finally
		{
			item.Dispose();
		}
		return num;
	}

	public bool InstallModChange()
	{
		bool result = false;
		if (m_installModInfo.Count != m_lastInstallModInfo.Count)
		{
			return true;
		}
		foreach (ModInfo item in m_installModInfo)
		{
			if (!m_lastInstallModInfo.Contains(item))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void SetPath(string path)
	{
		path = path.Replace("\\", "/");
		if (path != m_path)
		{
			m_lastPath = m_path;
			m_path = path;
		}
	}

	public string SetPathText(string path)
	{
		string result = Storage.GetSystemPath(path);
		string[] array = path.Split('/');
		if (array.Length > 5)
		{
			result = ".../" + array[array.Length - 3] + "/" + array[array.Length - 2] + "/" + array[array.Length - 1];
		}
		return result;
	}

	public void AddCommonPath(string path)
	{
		if (!m_commonPathList.Contains(path) && !string.IsNullOrEmpty(path))
		{
			m_commonPathList.Add(path);
		}
	}

	public void UpdateModFromCommunity(ModInfo modInfo)
	{
	}
}
