using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Engine;
using Engine.Input;
using Game;
using Game.NetWork;
using Game.NetWork.Packages;

namespace TerminalEntry
{
	// Token: 0x02000003 RID: 3
	internal class Program
	{
		// Token: 0x06000006 RID: 6
		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleCtrlHandler(Program.ControlCtrlDelegate HandlerRoutine, bool Add);

		// Token: 0x06000007 RID: 7 RVA: 0x000025A0 File Offset: 0x000007A0
		public static bool IsWindows()
		{
			return Environment.OSVersion.Platform.ToString().ToLower().Contains("win");
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002075 File Offset: 0x00000275
		public static bool HandlerRoutine(int CtrlType)
		{
			Program.ProcessExit();
			return true;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000025D4 File Offset: 0x000007D4
		private static void Main(string[] args)
		{
			if (Program.IsWindows())
			{
				IntPtr stdHandle = Program.GetStdHandle(-10);
				uint num;
				if (Program.GetConsoleMode(stdHandle, out num))
				{
					num &= 4294967231U;
					num |= 128U;
					Program.SetConsoleMode(stdHandle, num);
				}
				IntPtr outHandle = Program.GetStdHandle(-11);
				uint outMode;
				if (Program.GetConsoleMode(outHandle, out outMode))
				{
					// ENABLE_VIRTUAL_TERMINAL_PROCESSING: ANSI colors, line erase and the alternate screen work in the Windows console
					Program.SetConsoleMode(outHandle, outMode | 4U);
				}
				Program.SetConsoleCtrlHandler(Program.cancelHandler, true);
				IntPtr consoleWindow = Program.GetConsoleWindow();
				if (consoleWindow != IntPtr.Zero)
				{
					Program.DeleteMenu(Program.GetSystemMenu(consoleWindow, false), 61536U, 0U);
				}
			}
			else
			{
				Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
				{
					Program.ProcessExit();
				};
			}
			Console.Title = "Survivalcraft Server Terminal";
			Console.Write("\u001b[96m");
			Console.WriteLine("=========================================================");
			Console.WriteLine("                 SERVER TERMINAL ONLINE                  ");
			Console.WriteLine("=========================================================");
			Console.Write("\u001b[0m");
			ScreensManager.OnEnterScreen += Program.OnEnterScreen;
			Window.Frame += Program.Initialize;
			Game.Program.Main();
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000207D File Offset: 0x0000027D
		private static uint ComputeStringHash(string s)
		{
			uint num = 2166136261U;
			if (s != null)
			{
				for (int i = 0; i < s.Length; i++)
				{
					num = ((uint)s[i] ^ num) * 16777619U;
				}
			}
			return num;
		}

		private static void ProcessExit()
		{
			Console.WriteLine("Stopping server...");
			if (ScreensManager.CurrentScreen != null)
			{
				ScreensManager.CurrentScreen.Leave();
			}
			Window.Close();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000026CC File Offset: 0x000008CC
		private static void Initialize()
		{
			if (Time.FrameIndex == 0)
			{
				Log.RemoveAllLogSinks();
				Program.term = new TerminalLogSink(new Action<string>(Program.ProcessLine), new Action(Program.ProcessExit));
			Program.term.FullScreenExited = delegate
			{
				Program.IsMonitoring = false;
				Console.WriteLine("Выход из режима мониторинга.");
			};
				Log.AddLogSink(Program.term);
				Window.Frame -= Program.Initialize;
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002724 File Offset: 0x00000924
		private static void OnEnterScreen()
		{
			Program.term.DropTask(Program.last_ls);
			Program.last_ls = Program.term.TriggerCmd("ls", 20);
			if (ScreensManager.CurrentScreen.GetType() == typeof(PlayScreen) && File.Exists("AutorunWorldDirectory.txt"))
			{
				((PlayScreen)ScreensManager.CurrentScreen).Play(WorldsManager.GetWorldInfo(File.ReadAllText("AutorunWorldDirectory.txt")));
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000279C File Offset: 0x0000099C
		private static void ProcessLine(string line)
		{
			int num;
			if (int.TryParse(line, out num))
			{
				line = "click " + line;
			}
			Program.Command command = new Program.Command(line);
			if (command.Name == null)
			{
				return;
			}
			string text = command.Name.ToLower();
			uint num2 = ComputeStringHash(text);
			if (num2 <= 1550717474U)
			{
				if (num2 <= 946971642U)
				{
					if (num2 <= 377881970U)
					{
						if (num2 != 43002936U)
						{
							if (num2 != 59780555U)
							{
								if (num2 != 377881970U)
								{
									goto IL_0D57;
								}
								if (!(text == "updater"))
								{
									goto IL_0D57;
								}
								string text2 = command.GetRest().Trim();
								if (string.IsNullOrEmpty(text2))
								{
									Console.WriteLine("Limit: " + TerrainUpdater.MaxServerChunksPerFrame);
									return;
								}
								if (text2 == "clear" || text2 == "default")
								{
									TerrainUpdater.MaxServerChunksPerFrame = 20;
									Console.WriteLine("Reset to 20.");
									return;
								}
								int num3;
								if (int.TryParse(text2, out num3))
								{
									TerrainUpdater.MaxServerChunksPerFrame = num3;
									Console.WriteLine("Set to " + num3);
									return;
								}
								return;
							}
							else if (!(text == "cls"))
							{
								goto IL_0D57;
							}
						}
						else
						{
							if (!(text == "clr"))
							{
								goto IL_0D57;
							}
							GC.Collect();
							Console.WriteLine("GC Collected.");
							return;
						}
					}
					else if (num2 != 667630371U)
					{
						if (num2 != 726139854U)
						{
							if (num2 != 946971642U)
							{
								goto IL_0D57;
							}
							if (!(text == "help"))
							{
								goto IL_0D57;
							}
							goto IL_0753;
						}
						else
						{
							if (!(text == "abp"))
							{
								goto IL_0D57;
							}
							int num4;
							if (command.ReadArg(out num4) && GameManager.Project != null)
							{
								SubsystemPlayers subsystemPlayers = GameManager.Project.FindSubsystem<SubsystemPlayers>();
								subsystemPlayers.AddBlackList(subsystemPlayers.PlayersData[num4]);
								Console.WriteLine(subsystemPlayers.PlayersData[num4].Name + " blacklisted.");
								return;
							}
							return;
						}
					}
					else
					{
						if (!(text == "close"))
						{
							goto IL_0D57;
						}
						if (GameManager.Project == null)
						{
							return;
						}
						string[] array = new string[2];
						if (command.ReadArgs(array, true))
						{
							int num5 = (int.TryParse(array[0], out num5) ? num5 : 15);
							CommonLib.Net.QueuePackage(new PlayerDataPackage(num5, array[1]));
							Console.WriteLine(string.Format("Shutting down in {0} seconds...", num5));
							Time.QueueTimeDelayedExecution(Time.FrameStartTime + (double)num5, delegate
							{
								Program.ProcessExit();
							});
							return;
						}
						return;
					}
				}
				else if (num2 <= 1446109160U)
				{
					if (num2 != 1162757945U)
					{
						if (num2 != 1322827881U)
						{
							if (num2 != 1446109160U)
							{
								goto IL_0D57;
							}
							if (!(text == "ls"))
							{
								goto IL_0D57;
							}
							Dialog dialog;
							if (Program.FindDialog(out dialog))
							{
								Console.WriteLine(Program.FindAllText(dialog));
								Program.ListInteractive(dialog.Children);
								return;
							}
							Program.ListInteractive(Program.FindContainerInFocus().Children);
							return;
						}
						else
						{
							if (!(text == "rpl"))
							{
								goto IL_0D57;
							}
							int num6;
							if (!command.ReadArg(out num6) || GameManager.Project == null)
							{
								return;
							}
							SubsystemPlayers subsystemPlayers2 = GameManager.Project.FindSubsystem<SubsystemPlayers>();
							if (num6 < subsystemPlayers2.PlayersData.Count)
							{
								CommonLib.Net.RemoveClient(subsystemPlayers2.PlayersData[num6].Client, "Ты был кикнут консолью");
								Console.WriteLine("Kicked: " + subsystemPlayers2.PlayersData[num6].Name);
								return;
							}
							return;
						}
					}
					else
					{
						if (!(text == "pl"))
						{
							goto IL_0D57;
						}
						if (GameManager.Project == null)
						{
							return;
						}
						int num7 = 0;
						using (ReadOnlyList<PlayerData>.Enumerator enumerator = GameManager.Project.FindSubsystem<SubsystemPlayers>().PlayersData.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								PlayerData playerData = enumerator.Current;
								Console.WriteLine(string.Format("[{0}] {1} | {2}", num7++, playerData.Name, playerData.PlayerGUID));
							}
							return;
						}
						goto IL_0753;
					}
				}
				else if (num2 != 1483009432U)
				{
					if (num2 != 1538531746U)
					{
						if (num2 != 1550717474U)
						{
							goto IL_0D57;
						}
						if (!(text == "clear"))
						{
							goto IL_0D57;
						}
					}
					else
					{
						if (!(text == "back"))
						{
							goto IL_0D57;
						}
						ContainerWidget containerWidget = Program.FindContainerInFocus();
						containerWidget.Input.Back = true;
						containerWidget.Update();
						Console.WriteLine("Back command sent.");
						return;
					}
				}
				else
				{
					if (!(text == "debug"))
					{
						goto IL_0D57;
					}
					SettingsManager.DebugMode = !SettingsManager.DebugMode;
					Program.term.SetForeground(ConsoleColor.Yellow);
					Console.WriteLine("Debug Mode is now: " + (SettingsManager.DebugMode ? "ON" : "OFF"));
					Program.term.SetForeground(null);
					return;
				}
				Program.term.ClearScreen();
				Console.WriteLine("Console cleared.");
				return;
				IL_0753:
				Program.term.SetForeground(ConsoleColor.Yellow);
				Console.WriteLine("===================== List of SC server commands =================");
				Console.WriteLine("[help]                    View help");
				Console.WriteLine("[cls/clear]               Clear console screen (NEW)");
				Console.WriteLine("[mode chat/inv/all]       Filter logs Linux-style (NEW)");
				Console.WriteLine("[ls]                      Current interface options");
				Console.WriteLine("[click 0]                 Click the option number 0");
				Console.WriteLine("[touch 0]                 Get coordinates/size of widget 0");
				Console.WriteLine("[text 0 1234]             Enter text into textbox 0");
				Console.WriteLine("[back]                    Click the 'back' button");
				Console.WriteLine("[pws]                     View current screen name");
				Console.WriteLine("[stop]                    Shut down the server and exit");
				Console.WriteLine("[say 1234]                Send message to the whole server");
				Console.WriteLine("[pl]                      Show list of online players");
				Console.WriteLine("[rpl 0]                   Kick player number 0");
				Console.WriteLine("[cpl]                     Delete all player data from server");
				Console.WriteLine("[bpl]                     Show blacklist");
				Console.WriteLine("[abp 0]                   Add player 0 to blacklist");
				Console.WriteLine("[cbp 0]                   Remove player 0 from blacklist");
				Console.WriteLine("[anmp 0]                  Mute player 0");
				Console.WriteLine("[rnmp 0]                  Unmute player 0");
				Console.WriteLine("[clr]                     Clean server memory (GC)");
				Console.WriteLine("[close 15 msg]            Shutdown with timer and notification");
				Console.WriteLine("[updater 20]              Set max chunks per frame limit");
				Console.WriteLine("[debug]                   Toggle Debug Mode to collect performance data");
				Console.WriteLine("[monitor]                 Show live performance dashboard");
				Console.WriteLine("===================================================================");
				Program.term.SetForeground(null);
				return;
			}
			if (num2 <= 2340474172U)
			{
				if (num2 <= 1684935905U)
				{
					if (num2 != 1551804527U)
					{
						if (num2 != 1591883865U)
						{
							if (num2 != 1684935905U)
							{
								goto IL_0D57;
							}
							if (!(text == "pws"))
							{
								goto IL_0D57;
							}
							Console.WriteLine(ScreensManager.GetScreenName(ScreensManager.CurrentScreen));
							return;
						}
						else
						{
							if (!(text == "bpl"))
							{
								goto IL_0D57;
							}
							if (GameManager.Project == null)
							{
								return;
							}
							int num8 = 0;
							using (List<string>.Enumerator enumerator2 = GameManager.Project.FindSubsystem<SubsystemPlayers>().BlackPlayerGuidList.GetEnumerator())
							{
								while (enumerator2.MoveNext())
								{
									string text3 = enumerator2.Current;
									Console.WriteLine(string.Format("[{0}] {1}", num8++, text3));
								}
								return;
							}
						}
					}
					else if (!(text == "click"))
					{
						goto IL_0D57;
					}
					int num9;
					if (command.ReadArg(out num9) && num9 >= 0 && num9 < Program.interactives.Count)
					{
						Widget w = Program.interactives[num9];
						Program.term.QueueEvent(() => Program.Click(w));
						Console.WriteLine("Success: Clicked index " + num9);
						Program.last_ls = Program.term.TriggerCmd("ls", 6);
						return;
					}
					return;
				}
				else if (num2 != 1770621400U)
				{
					if (num2 != 1983775257U)
					{
						if (num2 == 2340474172U)
						{
							if (text == "rnmp")
							{
								int num10;
								if (!int.TryParse(command.GetRest(), out num10) || GameManager.Project == null)
								{
									return;
								}
								List<string> noMsgPlayerGuidList = GameManager.Project.FindSubsystem<SubsystemPlayers>().NoMsgPlayerGuidList;
								if (num10 < noMsgPlayerGuidList.Count)
								{
									string text4 = noMsgPlayerGuidList[num10];
									noMsgPlayerGuidList.RemoveAt(num10);
									Console.WriteLine(text4 + " unmuted.");
									return;
								}
								return;
							}
						}
					}
					else if (text == "monitor")
					{
						if (Program.IsMonitoring)
						{
							Console.WriteLine("Мониторинг уже запущен. Нажмите ESC для выхода.");
							return;
						}
						if (!SettingsManager.DebugMode)
						{
							Program.term.SetForeground(ConsoleColor.Yellow);
							Console.WriteLine("Включаю DebugMode для сбора метрик...");
							SettingsManager.DebugMode = true;
							Task.Run(delegate
							{
								Thread.Sleep(1000);
							}).Wait();
						}
						Program.IsMonitoring = true;
						term.EnterFullScreen();
						Program.monitorThread = new Thread(new ThreadStart(Program.MonitorLoop));
						Program.monitorThread.IsBackground = true;
						Program.monitorThread.Start();
						return;
					}
				}
				else if (text == "touch")
				{
					int num11;
					if (command.ReadArg(out num11) && num11 >= 0 && num11 < Program.interactives.Count)
					{
						Widget widget = Program.interactives[num11];
						Console.WriteLine("Size: " + widget.ActualSize);
						Console.WriteLine("Pos: " + widget.GlobalTransform.Translation);
						return;
					}
					return;
				}
			}
			else if (num2 <= 3374087776U)
			{
				if (num2 != 2865982835U)
				{
					if (num2 != 3185987134U)
					{
						if (num2 == 3374087776U)
						{
							if (text == "say")
							{
								string rest = command.GetRest();
								if (!string.IsNullOrEmpty(rest) && GameManager.Project != null)
								{
									GameManager.Project.FindSubsystem<SubsystemGameWidgets>().AddMessage(rest, null, 0, null);
									return;
								}
								return;
							}
						}
					}
					else if (text == "text")
					{
						string[] array2 = new string[2];
						if (!command.ReadArgs(array2, true))
						{
							return;
						}
						int num12;
						Widget widget2 = ((int.TryParse(array2[0], out num12) && num12 < Program.interactives.Count) ? Program.interactives[num12] : Program.FindContainerInFocus().Children.Find<Widget>(array2[0], false));
						if (widget2 is TextBoxWidget)
						{
							(widget2 as TextBoxWidget).Text = array2[1];
							return;
						}
						Console.WriteLine("Target is not a text box.");
						return;
					}
				}
				else if (text == "anmp")
				{
					int num13;
					if (command.ReadArg(out num13) && GameManager.Project != null)
					{
						SubsystemPlayers subsystemPlayers3 = GameManager.Project.FindSubsystem<SubsystemPlayers>();
						subsystemPlayers3.AddNoMsgList(subsystemPlayers3.PlayersData[num13]);
						Console.WriteLine(subsystemPlayers3.PlayersData[num13].Name + " muted.");
						return;
					}
					return;
				}
			}
			else if (num2 <= 3966689298U)
			{
				if (num2 != 3411225317U)
				{
					if (num2 == 3966689298U)
					{
						if (text == "mode")
						{
							string text5 = command.GetRest().Trim().ToLower();
							string logName = "";
							string filePath = "";
							if (text5 == "chat")
							{
								Program.term.CurrentMode = TerminalLogSink.LogMode.Chat;
								logName = "chat";
								filePath = "Bugs/Chat.log";
							}
							else
							{
								if (!(text5 == "inv") && !(text5 == "inventory"))
								{
									Program.term.CurrentMode = TerminalLogSink.LogMode.All;
									Console.WriteLine("Mode: [ALL LOGS]");
									return;
								}
								Program.term.CurrentMode = TerminalLogSink.LogMode.Inventory;
								logName = "inventory";
								filePath = "Bugs/Inventory.log";
							}
							if (File.Exists(filePath))
							{
								Program.term.QueueEvent(delegate
								{
									Program.term.SetForeground(ConsoleColor.Green);
									Console.WriteLine("=== MODE: " + logName.ToUpper() + " (PAST + LIVE) ===");
									Program.term.SetForeground(null);
									try
									{
										if (logName == "chat" && GameScreen.m_chatWriter != null)
										{
											GameScreen.m_chatWriter.Flush();
										}
										if (logName == "inventory" && GameScreen.m_inventoryWriter != null)
										{
											GameScreen.m_inventoryWriter.Flush();
										}
										using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
										{
											using (StreamReader streamReader = new StreamReader(fileStream))
											{
												Console.WriteLine(streamReader.ReadToEnd());
											}
										}
									}
									catch (Exception ex)
									{
										Program.term.SetForeground(ConsoleColor.Red);
										Console.WriteLine("Error reading past logs: " + ex.Message);
										Program.term.SetForeground(null);
									}
								});
								return;
							}
							Program.term.SetForeground(ConsoleColor.Yellow);
							Console.WriteLine("Mode changed, but past log file not found: " + filePath);
							Program.term.SetForeground(null);
							return;
						}
					}
				}
				else if (text == "stop")
				{
					Program.ProcessExit();
					return;
				}
			}
			else if (num2 != 4031441758U)
			{
				if (num2 == 4033707424U)
				{
					if (text == "cbp")
					{
						int num14;
						if (!int.TryParse(command.GetRest(), out num14) || GameManager.Project == null)
						{
							return;
						}
						List<string> blackPlayerGuidList = GameManager.Project.FindSubsystem<SubsystemPlayers>().BlackPlayerGuidList;
						if (num14 < blackPlayerGuidList.Count)
						{
							string text6 = blackPlayerGuidList[num14];
							blackPlayerGuidList.RemoveAt(num14);
							Console.WriteLine(text6 + " removed from blacklist.");
							return;
						}
						return;
					}
				}
			}
			else if (text == "cpl")
			{
				if (GameManager.Project != null)
				{
					SubsystemPlayers subsystemPlayers4 = GameManager.Project.FindSubsystem<SubsystemPlayers>();
					List<PlayerData> list = new List<PlayerData>(subsystemPlayers4.PlayersData);
					subsystemPlayers4.m_offlinePlayerEntities.Clear();
					subsystemPlayers4.m_offlinePlayers.Clear();
					foreach (PlayerData playerData2 in list)
					{
						subsystemPlayers4.RemovePlayerData(playerData2, true);
					}
					Console.WriteLine("All player data deleted.");
					return;
				}
				return;
			}
			IL_0D57:
			Program.term.SetForeground(ConsoleColor.Red);
			Console.WriteLine("Unknown command: " + text);
			Program.term.SetForeground(null);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000209F File Offset: 0x0000029F
		private static IEnumerator<TerminalLogSink.Task> Click(Widget widget)
		{
			Vector2 pos = widget.GlobalTransform.Translation.XY + widget.ActualSize / 2f;
			Touch.NextBeforeFrame += delegate
			{
				Touch.ProcessTouchPressed(0, pos);
			};
			yield return null;
			Touch.NextBeforeFrame += delegate
			{
				Touch.ProcessTouchReleased(0, pos);
			};
			yield break;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00003544 File Offset: 0x00001744
		private static ContainerWidget FindContainerInFocus()
		{
			Dialog dialog;
			if (Program.FindDialog(out dialog))
			{
				return dialog;
			}
			if (ScreensManager.CurrentScreen != null)
			{
				return ScreensManager.CurrentScreen;
			}
			return ScreensManager.RootWidget;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00003570 File Offset: 0x00001770
		private static void ListInteractive(WidgetsList children)
		{
			Program.interactives.Clear();
			foreach (Widget widget in Program.AllChildern(children))
			{
				if (widget.IsEnabled && widget.IsVisible)
				{
					if (widget is ButtonWidget)
					{
						Console.WriteLine(string.Format("[{0}] {1} : Button", Program.interactives.Count, Program.FindAllText(widget)));
						Program.interactives.Add(widget);
					}
					else if (widget is CheckboxWidget)
					{
						Console.Write(string.Format("[{0}] {1} : Checkbox", Program.interactives.Count, Program.FindAllText(widget)));
						if ((widget as CheckboxWidget).IsChecked)
						{
							Console.WriteLine("  [√]");
						}
						else
						{
							Console.WriteLine("  [ ]");
						}
						Program.interactives.Add((widget as CheckboxWidget).m_clickableWidget);
					}
					else
					{
						if (widget is ListPanelWidget)
						{
							Console.WriteLine(widget.Name + " : ListPanel");
							ListPanelWidget listPanelWidget = widget as ListPanelWidget;
							int num = 0;
							using (Dictionary<int, Widget>.Enumerator enumerator2 = listPanelWidget.m_widgetsByIndex.GetEnumerator())
							{
								while (enumerator2.MoveNext())
								{
									KeyValuePair<int, Widget> keyValuePair = enumerator2.Current;
									Console.WriteLine(string.Format("[{0}]:\n{1}", Program.interactives.Count, Program.FindAllText(keyValuePair.Value)));
									int? selectedIndex = listPanelWidget.SelectedIndex;
									int key = keyValuePair.Key;
									if ((selectedIndex.GetValueOrDefault() == key) & (selectedIndex != null))
									{
										Console.WriteLine("  [√]");
									}
									else
									{
										Console.WriteLine("  [ ]");
									}
									num++;
									Program.interactives.Add(keyValuePair.Value);
								}
								continue;
							}
						}
						if (widget is TextBoxWidget)
						{
							Console.WriteLine(string.Format("[{0}] {1} ({2}) : TextBox", Program.interactives.Count, Program.FindAllText(widget), (widget as TextBoxWidget).Text));
							Program.interactives.Add(widget);
						}
					}
				}
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000020AE File Offset: 0x000002AE
		private static bool FindDialog(out Dialog dialog)
		{
			dialog = DialogsManager.Dialogs.LastOrDefault<Dialog>();
			return dialog != null;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000020C6 File Offset: 0x000002C6
		private static IEnumerable<Widget> AllChildern(WidgetsList widgets)
		{
			int num = 0;
			foreach (Widget w in widgets)
			{
				int num2 = num;
				num = num2 + 1;
				if (w.Name == null)
				{
					w.Name = ((w.ParentWidget.Name == null) ? "" : (w.ParentWidget.Name + num.ToString()));
				}
				yield return w;
				ContainerWidget containerWidget = w as ContainerWidget;
				if (containerWidget != null)
				{
					foreach (Widget widget in containerWidget.AllChildren)
					{
						num2 = num;
						num = num2 + 1;
						if (widget.Name == null)
						{
							widget.Name = ((widget.ParentWidget.Name == null) ? "" : (widget.ParentWidget.Name + num.ToString()));
						}
						yield return widget;
					}
				}
			}
			yield break;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000037C4 File Offset: 0x000019C4
		private static string FindAllText(Widget w)
		{
			if (w is LabelWidget)
			{
				return (w as LabelWidget).Text;
			}
			bool flag = false;
			string text = "";
			if (w is ContainerWidget)
			{
				foreach (Widget widget in (w as ContainerWidget).AllChildren)
				{
					if (widget is LabelWidget)
					{
						if (flag)
						{
							text += "\n";
						}
						text += (widget as LabelWidget).Text;
						flag = true;
					}
				}
			}
			if (string.IsNullOrEmpty(text) || text == "...")
			{
				return w.Name;
			}
			return text;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000387C File Offset: 0x00001A7C
		public static void FireEvent(object obj, string eventName, params object[] eventParams)
		{
			foreach (Delegate @delegate in ((MulticastDelegate)obj.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj)).GetInvocationList())
			{
				@delegate.Method.Invoke(@delegate.Target, eventParams);
			}
		}

		// Token: 0x06000017 RID: 23
		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		// Token: 0x06000018 RID: 24
		[DllImport("kernel32.dll", ExactSpelling = true)]
		private static extern IntPtr GetConsoleWindow();

		// Token: 0x06000019 RID: 25
		[DllImport("user32.dll")]
		private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

		// Token: 0x0600001A RID: 26 RVA: 0x000038D0 File Offset: 0x00001AD0
		private static void MonitorLoop()
		{
			while (Program.IsMonitoring)
			{
				List<PerformanceManager.SystemLoadInfo> list = new List<PerformanceManager.SystemLoadInfo>();
				Dictionary<string, PerformanceManager.SystemLoadInfo> systemLoads = PerformanceManager.m_systemLoads;
				lock (systemLoads)
				{
					foreach (KeyValuePair<string, PerformanceManager.SystemLoadInfo> keyValuePair in systemLoads)
					{
						if (keyValuePair.Value.SmoothedTime > 0.1)
						{
							list.Add(keyValuePair.Value);
						}
					}
				}
				list.Sort((PerformanceManager.SystemLoadInfo load1, PerformanceManager.SystemLoadInfo load2) => load2.SmoothedTime.CompareTo(load1.SmoothedTime));

				// Whole frame is built first and written in one go; \u001b[H = home, \u001b[K = erase to end of line, \u001b[J = erase below
				StringBuilder sb = new StringBuilder();
				sb.Append("\u001b[H");
				sb.Append(TerminalLogSink.Fg(ConsoleColor.Cyan)).Append("=== LIVE SERVER PERFORMANCE (Press ESC to exit) ===\u001b[K\r\n");
				sb.Append("\u001b[0m");
				foreach (string statsLine in (PerformanceManager.m_statsString ?? "").Replace("\r", "").Split('\n'))
				{
					sb.Append(statsLine).Append("\u001b[K\r\n");
				}
				sb.Append(TerminalLogSink.Fg(ConsoleColor.Cyan)).Append("--- CONSTANT LOAD (TOP 10) ---\u001b[K\r\n");
				for (int i = 0; i < 10; i++)
				{
					if (i < list.Count)
					{
						PerformanceManager.SystemLoadInfo info = list[i];
						ConsoleColor color = (info.SmoothedTime > 4.0) ? ConsoleColor.Red : ((info.SmoothedTime > 1.0) ? ConsoleColor.Yellow : ConsoleColor.Green);
						sb.Append(TerminalLogSink.Fg(color)).Append(string.Format("{0:0.00}ms : {1} (x{2})", info.SmoothedTime, info.Name, info.LastFrameCount));
					}
					sb.Append("\u001b[K\r\n");
				}
				sb.Append("\u001b[0m\u001b[J");
				Program.term.DrawFullScreenFrame(sb.ToString());
				Thread.Sleep(500);
			}
		}

		// Token: 0x0600001B RID: 27
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		// Token: 0x0600001C RID: 28
		[DllImport("kernel32.dll")]
		private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		// Token: 0x0600001D RID: 29
		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		// Token: 0x04000006 RID: 6
		private static TerminalLogSink term;

		// Token: 0x04000007 RID: 7
		private static List<Widget> interactives = new List<Widget>();

		// Token: 0x04000008 RID: 8
		private static int last_ls = -1;

		// Token: 0x04000009 RID: 9
		private static Program.ControlCtrlDelegate cancelHandler = new Program.ControlCtrlDelegate(Program.HandlerRoutine);

		// Token: 0x0400000A RID: 10
		private const uint SC_CLOSE = 61536U;

		// Token: 0x0400000B RID: 11
		private const uint MF_BYCOMMAND = 0U;

		// Token: 0x0400000C RID: 12
		public static bool IsMonitoring;

		// Token: 0x0400000D RID: 13
		private static Thread monitorThread;

		// Token: 0x0400000E RID: 14
		private const int STD_INPUT_HANDLE = -10;

		// Token: 0x0400000F RID: 15
		private const uint ENABLE_QUICK_EDIT_MODE = 64U;

		// Token: 0x04000010 RID: 16
		private const uint ENABLE_EXTENDED_FLAGS = 128U;

		// Token: 0x02000004 RID: 4
		// (Invoke) Token: 0x0600001F RID: 31
		public delegate bool ControlCtrlDelegate(int CtrlType);

		// Token: 0x02000005 RID: 5
		private class Command
		{
			// Token: 0x06000022 RID: 34 RVA: 0x00002101 File Offset: 0x00000301
			public Command(string str)
			{
				this.args = str.Split(new char[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
				this.ReadArg(out this.Name);
			}

			// Token: 0x06000023 RID: 35 RVA: 0x00003B38 File Offset: 0x00001D38
			public bool ReadArg(out int res)
			{
				string text;
				if (this.ReadArg(out text))
				{
					try
					{
						res = int.Parse(text);
						return true;
					}
					catch
					{
					}
				}
				res = 0;
				return false;
			}

			// Token: 0x06000024 RID: 36 RVA: 0x00002134 File Offset: 0x00000334
			public bool ReadArg(out string res)
			{
				if (this.currentIndex < this.args.Length)
				{
					res = this.args[this.currentIndex].Trim();
					this.currentIndex++;
					return true;
				}
				res = null;
				return false;
			}

			// Token: 0x06000025 RID: 37 RVA: 0x00003B74 File Offset: 0x00001D74
			public bool ReadArgs(string[] ress, bool printError = true)
			{
				for (int i = 0; i < ress.Length; i++)
				{
					if (!this.ReadArg(out ress[i]))
					{
						if (printError)
						{
							Console.WriteLine("{0} expected {1} arguments", this.Name, ress.Length);
						}
						return false;
					}
				}
				return true;
			}

			// Token: 0x06000026 RID: 38 RVA: 0x00003BBC File Offset: 0x00001DBC
			public string GetRest()
			{
				StringBuilder stringBuilder = new StringBuilder();
				while (this.currentIndex < this.args.Length)
				{
					stringBuilder.Append(this.args[this.currentIndex].Trim());
					stringBuilder.Append(' ');
					this.currentIndex++;
				}
				return stringBuilder.ToString();
			}

			// Token: 0x04000011 RID: 17
			private int currentIndex;

			// Token: 0x04000012 RID: 18
			private string[] args;

			// Token: 0x04000013 RID: 19
			public readonly string Name;
		}
	}
}
