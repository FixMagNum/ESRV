using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Engine;

namespace TerminalEntry
{
	/// <summary>
	/// Console front-end for the server.
	///
	/// Layout: all output scrolls normally above a single prompt line ("> input") that is always the last line.
	/// Every write (log lines, Console.Write/WriteLine from the game, command echo, key echo) happens under
	/// one lock and goes through the same path: erase the prompt, write the text, redraw the prompt.
	/// Only relative cursor movement and ANSI erase sequences are used (no Console.CursorLeft/Top, no WindowWidth
	/// math for positioning), so it behaves the same on Windows (VT mode enabled in Program.Main) and on Linux terminals.
	/// Colors are emitted inline with the text, so they cannot leak into other lines.
	/// </summary>
	// Token: 0x0200000E RID: 14
	public class TerminalLogSink : ILogSink, IDisposable
	{
		// Token: 0x17000005 RID: 5
		public LogType MinimumLogType { get; set; }

		// Token: 0x04000037 RID: 55
		public LogMode CurrentMode;

		// Single lock for everything that touches the terminal (output and input).
		public readonly object consoleLock = new object();

		private const string Reset = "\u001b[0m";

		private const int MaxPendingLines = 2000;

		private static TextWriter RealOut;

		// true когда stdin перенаправлен (запуск из веб-панели), false когда запущен в окне консоли
		private readonly bool m_inputRedirected;

		// Token: 0x0600004D RID: 77
		public TerminalLogSink(Action<string> line, Action onClose)
		{
			// onClose is intentionally not called when stdin is lost (e.g. the server runs without a terminal),
			// so that a missing terminal never shuts the server down.
			this.ProcessLine = line;
			this.CurrentMode = LogMode.All;
			this.m_inputRedirected = Console.IsInputRedirected;

			if (Environment.OSVersion.Platform != PlatformID.Unix)
			{
				try
				{
					Console.OutputEncoding = Encoding.UTF8;
				}
				catch (Exception)
				{
				}
			}

			UTF8Encoding encoding = new UTF8Encoding(false);
			TerminalLogSink.RealOut = new StreamWriter(Console.OpenStandardOutput(), encoding)
			{
				AutoFlush = true
			};
			Console.SetOut(new TerminalLogSink.AsyncWriter(this, false));
			Console.SetError(new TerminalLogSink.AsyncWriter(this, true));

			lock (this.consoleLock)
			{
				this.RedrawPrompt();
			}

			Window.Frame += this.UpdateTasks;
			new Thread(new ThreadStart(this.InputLoop))
			{
				IsBackground = true,
				Name = "Terminal input"
			}.Start();
		}

		// Called (on the input thread) after the user leaves the full-screen view with Esc or Q.
		public Action FullScreenExited { get; set; }

		// Token: 0x0600004E RID: 78
		public void Log(LogType logType, string message)
		{
			if (logType < this.MinimumLogType || message == null)
			{
				return;
			}
			lock (this.consoleLock)
			{
				if (message == this.m_lastMessage)
				{
					this.m_duplicateCount++;
					return;
				}
				this.m_lastMessage = message;
				this.m_duplicateCount = 0;
				if (this.CurrentMode == LogMode.Chat && !message.Contains("["))
				{
					return;
				}
				if (this.CurrentMode == LogMode.Inventory && !message.Contains("Inventory"))
				{
					return;
				}
				this.EmitLocked(TerminalLogSink.FormatLogLine(logType, message));
			}
		}

		// Token: 0x0600004F RID: 79
		public int QueueEvent(Action a)
		{
			return this.QueueEvent(new TerminalLogSink.SimpleTask(a));
		}

		// Token: 0x06000050 RID: 80
		public int QueueEvent(Func<IEnumerator<TerminalLogSink.Task>> a)
		{
			return this.QueueEvent(new TerminalLogSink.MultiTask(a));
		}

		// Token: 0x06000051 RID: 81
		public int QueueEvent(TerminalLogSink.Task t)
		{
			lock (this.tasksLock)
			{
				int num = 0;
				foreach (TerminalLogSink.Task task in this.tasks)
				{
					if (task.Tag > num)
					{
						num = task.Tag + 1;
					}
				}
				t.Tag = num;
				this.tasks.Enqueue(t);
				return t.Tag;
			}
		}

		// Token: 0x06000052 RID: 82
		public void DropTask(int tag)
		{
			lock (this.tasksLock)
			{
				foreach (TerminalLogSink.Task task in this.tasks)
				{
					if (task.Tag == tag)
					{
						task.Drop();
					}
				}
			}
		}

		// Token: 0x06000055 RID: 85
		public int TriggerCmd(string line, int delay = 0)
		{
			if (this.ProcessLine == null)
			{
				return -1;
			}
			if (delay > 0)
			{
				return this.QueueEvent(delegate
				{
					Console.WriteLine("> " + line);
					return new List<TerminalLogSink.Task>
					{
						new TerminalLogSink.WaitFrames(delay),
						new TerminalLogSink.SimpleTask(delegate
						{
							this.ProcessLine(line);
						})
					}.GetEnumerator();
				});
			}
			return this.QueueEvent(delegate
			{
				Console.WriteLine("> " + line);
				this.ProcessLine(line);
			});
		}

		// Clears the screen (scrollback above is kept) and redraws the prompt.
		public void ClearScreen()
		{
			lock (this.consoleLock)
			{
				if (this.m_fullScreen)
				{
					return;
				}
				RealOut.Write("\u001b[H\u001b[2J");
				this.m_promptRows = 1;
				this.RedrawPrompt();
			}
		}

		// Sets the foreground color for all following output (null = terminal default).
		// Used by the command handlers instead of Console.ForegroundColor, so the color is tracked in one place
		// and is restored after every prompt redraw and log line.
		public void SetForeground(ConsoleColor? color)
		{
			lock (this.consoleLock)
			{
				this.m_color = color;
				RealOut.Write(TerminalLogSink.ColorCode(color));
			}
		}

		private static string ColorCode(ConsoleColor? color)
		{
			return color.HasValue ? TerminalLogSink.Fg(color.Value) : Reset;
		}

		// Switches to the alternate screen buffer (used by the 'monitor' dashboard).
		// The normal buffer, its scrollback and the prompt are left untouched and restored by ExitFullScreen.
		public void EnterFullScreen()
		{
			lock (this.consoleLock)
			{
				RealOut.Write("\u001b[?1049h\u001b[?25l\u001b[H\u001b[2J");
				this.m_color = null;
				this.m_fullScreen = true;
			}
		}

		public void ExitFullScreen()
		{
			lock (this.consoleLock)
			{
				if (!this.m_fullScreen)
				{
					return;
				}
				this.m_fullScreen = false;
				RealOut.Write(Reset + "\u001b[?25h\u001b[?1049l");
				this.ErasePrompt();
				foreach (string line in this.m_pendingWhileFullScreen)
				{
					RealOut.Write(line);
				}
				this.m_pendingWhileFullScreen.Clear();
				this.RedrawPrompt();
			}
		}

		// Replaces the whole dashboard frame in the alternate screen buffer.
		public void DrawFullScreenFrame(string frame)
		{
			lock (this.consoleLock)
			{
				if (this.m_fullScreen)
				{
					RealOut.Write(frame);
				}
			}
		}

		// ANSI foreground color sequence for a ConsoleColor (used for pre-rendered text).
		public static string Fg(ConsoleColor color)
		{
			int code;
			switch (color)
			{
				case ConsoleColor.Black: code = 30; break;
				case ConsoleColor.DarkBlue: code = 34; break;
				case ConsoleColor.DarkGreen: code = 32; break;
				case ConsoleColor.DarkCyan: code = 36; break;
				case ConsoleColor.DarkRed: code = 31; break;
				case ConsoleColor.DarkMagenta: code = 35; break;
				case ConsoleColor.DarkYellow: code = 33; break;
				case ConsoleColor.Gray: code = 37; break;
				case ConsoleColor.DarkGray: code = 90; break;
				case ConsoleColor.Blue: code = 94; break;
				case ConsoleColor.Green: code = 92; break;
				case ConsoleColor.Cyan: code = 96; break;
				case ConsoleColor.Red: code = 91; break;
				case ConsoleColor.Magenta: code = 95; break;
				case ConsoleColor.Yellow: code = 93; break;
				default: code = 97; break;
			}
			return "\u001b[" + code + "m";
		}

		// Called (on the input thread) when the user presses a key while not in the monitor.
		private void InputLoop()
		{
			if (this.m_inputRedirected)
			{
				this.LineInputLoop();
				return;
			}

			while (true)
			{
				ConsoleKeyInfo key;
				try
				{
					key = Console.ReadKey(true);
				}
				catch (Exception)
				{
					// stdin is not a terminal or was closed: stop reading keys, keep the server running
					return;
				}
				bool fullScreen;
				lock (this.consoleLock)
				{
					fullScreen = this.m_fullScreen;
				}
				if (fullScreen)
				{
					// Esc / Q leave the full-screen view (monitor); the owner is told through FullScreenExited
					if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
					{
						this.ExitFullScreen();
						Action exited = this.FullScreenExited;
						if (exited != null)
						{
							exited();
						}
					}
					continue;
				}
				lock (this.consoleLock)
				{
					this.HandleKeyLocked(key);
				}
			}
		}

		// Используется когда stdin это pipe (веб-панель): каждая строка на stdin это одна команда.
		private void LineInputLoop()
		{
			for (; ; )
			{
				string line;
				try
				{
					line = Console.In.ReadLine();
				}
				catch (Exception)
				{
					return;
				}
				if (line == null)
				{
					break;
				}
				string text = line.Trim();
				if (text.Length != 0)
				{
					// В полноэкранном режиме (monitor) "q" или "esc" выходят из него, как Esc на клавиатуре
					bool inFullScreen;
					object fullScreenLock = this.consoleLock;
					lock (fullScreenLock)
					{
						inFullScreen = this.m_fullScreen;
					}
					string lower = text.ToLowerInvariant();
					if (inFullScreen && (lower == "q" || lower == "esc"))
					{
						this.ExitFullScreen();
						Action fullScreenExited = this.FullScreenExited;
						if (fullScreenExited != null)
						{
							fullScreenExited();
						}
						continue;
					}
					object obj = this.consoleLock;
					lock (obj)
					{
						this.EmitLocked("> " + text);
					}
					this.QueueEvent(delegate
					{
						this.ProcessLine(text);
					});
				}
			}
		}

		// Must be called with consoleLock held.
		private void HandleKeyLocked(ConsoleKeyInfo key)
		{
			if (key.Key == ConsoleKey.Enter)
			{
				string text = this.consoleIn.ToString().Trim();
				this.consoleIn.Clear();
				if (text.Length > 0)
				{
					this.history.Put(text);
					this.EmitLocked("> " + text);
					this.QueueEvent(delegate
					{
						this.ProcessLine(text);
					});
				}
				else
				{
					this.RefreshPromptLocked();
				}
			}
			else if (key.Key == ConsoleKey.Backspace)
			{
				if (this.consoleIn.Length > 0)
				{
					this.consoleIn.Length = this.consoleIn.Length - 1;
					this.RefreshPromptLocked();
				}
			}
			else if (key.Key == ConsoleKey.UpArrow)
			{
				string older;
				if (this.history.Backward(out older))
				{
					this.consoleIn.Clear();
					this.consoleIn.Append(older);
					this.RefreshPromptLocked();
				}
			}
			else if (key.Key == ConsoleKey.DownArrow)
			{
				string newer;
				if (this.history.Forward(out newer))
				{
					this.consoleIn.Clear();
					this.consoleIn.Append(newer);
				}
				else
				{
					this.consoleIn.Clear();
				}
				this.RefreshPromptLocked();
			}
			else if (!char.IsControl(key.KeyChar))
			{
				this.consoleIn.Append(key.KeyChar);
				this.RefreshPromptLocked();
			}
		}

		// Writes one complete log/console entry above the prompt. Must be called with consoleLock held.
		private void EmitLocked(string text)
		{
			string s = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
			if (!s.EndsWith("\r\n"))
			{
				s += "\r\n";
			}
			if (this.m_fullScreen)
			{
				if (this.m_pendingWhileFullScreen.Count >= MaxPendingLines)
				{
					this.m_pendingWhileFullScreen.RemoveAt(0);
				}
				this.m_pendingWhileFullScreen.Add(s);
				return;
			}
			this.ErasePrompt();
			RealOut.Write(s);
			this.RedrawPrompt();
		}

		// Called by AsyncWriter (Console.Out / Console.Error) on complete lines.
		internal void WriteRaw(string text, bool isError)
		{
			lock (this.consoleLock)
			{
				this.EmitLocked(isError ? "\u001b[91m" + text + Reset : text);
			}
		}

		// Must be called with consoleLock held.
		private void ErasePrompt()
		{
			if (this.m_inputRedirected)
			{
				return;
			}

			// The prompt may have wrapped over several rows: go back to its first row, then erase to the end of the screen.
			if (this.m_promptRows > 1)
			{
				RealOut.Write("\u001b[" + (this.m_promptRows - 1) + "A");
			}
			RealOut.Write("\r\u001b[J");
			this.m_promptRows = 1;
		}

		// Must be called with consoleLock held.
		private void RedrawPrompt()
		{
			if (this.m_inputRedirected)
			{
				return;
			}

			string prompt = "> " + this.consoleIn.ToString();
			// The prompt is drawn in the default color, then the color chosen by the caller is restored
			RealOut.Write(Reset + prompt + TerminalLogSink.ColorCode(this.m_color));
			int width = TerminalLogSink.ConsoleWidth;
			this.m_promptRows = Math.Max(1, (prompt.Length + width - 1) / width);
		}

		// Must be called with consoleLock held.
		private void RefreshPromptLocked()
		{
			if (this.m_fullScreen)
			{
				return;
			}
			this.ErasePrompt();
			this.RedrawPrompt();
		}

		private static int ConsoleWidth
		{
			get
			{
				try
				{
					int width = Console.WindowWidth;
					return width > 0 ? width : 80;
				}
				catch (Exception)
				{
					return 80;
				}
			}
		}

		private static string FormatLogLine(LogType logType, string message)
		{
			string label;
			ConsoleColor labelColor;
			switch (logType)
			{
				case LogType.Debug:
					label = "DEBUG: ";
					labelColor = ConsoleColor.Magenta;
					break;
				case LogType.Warning:
					label = "WARNING: ";
					labelColor = ConsoleColor.Yellow;
					break;
				case LogType.Error:
					label = "ERROR: ";
					labelColor = ConsoleColor.Red;
					break;
				default:
					label = "INFO: ";
					labelColor = ConsoleColor.White;
					break;
			}

			StringBuilder sb = new StringBuilder();
			sb.Append(Fg(ConsoleColor.DarkGray)).Append(DateTime.Now.ToString("HH:mm:ss.fff ")).Append(Fg(labelColor)).Append(label);
			if (message.Contains("[Система]"))
			{
				sb.Append(Fg(ConsoleColor.Red)).Append("[Система] ");
				string text = message.Replace("<c=red>", "").Replace("</c>", "").Replace("[Система]", "");
				sb.Append(Fg(ConsoleColor.Yellow)).Append(text.Trim());
			}
			else
			{
				int open = message.IndexOf('[');
				int close = (open >= 0) ? message.IndexOf(']', open) : -1;
				if (open >= 0 && close > open)
				{
					sb.Append(Fg(ConsoleColor.White)).Append(message.Substring(0, open));
					sb.Append(Fg(ConsoleColor.Cyan)).Append(message.Substring(open, close - open + 1));
					sb.Append(Fg(ConsoleColor.White)).Append(message.Substring(close + 1));
				}
				else
				{
					sb.Append(Fg((logType == LogType.Error) ? ConsoleColor.Red : ConsoleColor.Gray)).Append(message);
				}
			}
			sb.Append(Reset);
			return sb.ToString();
		}

		// Token: 0x06000058 RID: 88
		private void UpdateTasks()
		{
			TerminalLogSink.Task current;
			lock (this.tasksLock)
			{
				if (this.tasks.Count == 0)
				{
					return;
				}
				current = this.tasks.Peek();
				if (current.Done)
				{
					this.tasks.Dequeue();
					return;
				}
			}
			current.Update();
		}

		// Token: 0x06000056 RID: 86
		public void Dispose()
		{
			Window.Frame -= this.UpdateTasks;
		}

		private readonly Action<string> ProcessLine;

		private readonly Queue<TerminalLogSink.Task> tasks = new Queue<TerminalLogSink.Task>();

		private readonly object tasksLock = new object();

		private readonly StringBuilder consoleIn = new StringBuilder();

		private readonly DropOutList<string> history = new DropOutList<string>(50);

		private readonly List<string> m_pendingWhileFullScreen = new List<string>();

		private string m_lastMessage = string.Empty;

		private int m_duplicateCount;

		private int m_promptRows = 1;

		private bool m_fullScreen;

		// Foreground color chosen by the caller (null = terminal default). Owned here, not by Console.ForegroundColor.
		private ConsoleColor? m_color;

		// Token: 0x0200000F RID: 15
		public abstract class Task
		{
			// Token: 0x17000006 RID: 6
			public bool Done { get; protected set; }

			public void Drop()
			{
				this.Done = true;
			}

			public virtual void Update()
			{
			}

			public int Tag;
		}

		// Token: 0x02000010 RID: 16
		public class WaitFrames : TerminalLogSink.Task
		{
			public WaitFrames(int n)
			{
				this.n = n;
			}

			public override void Update()
			{
				if (this.n > 0)
				{
					this.n--;
					return;
				}
				base.Done = true;
			}

			private int n;
		}

		// Token: 0x02000011 RID: 17
		private class SimpleTask : TerminalLogSink.Task
		{
			public SimpleTask(Action a)
			{
				this.a = a;
			}

			public override void Update()
			{
				Action action = this.a;
				if (action != null)
				{
					action();
				}
				base.Done = true;
			}

			private Action a;
		}

		// Token: 0x02000012 RID: 18
		private class MultiTask : TerminalLogSink.Task
		{
			public MultiTask(Func<IEnumerator<TerminalLogSink.Task>> func)
			{
				this.func = func;
			}

			public override void Update()
			{
				if (this.subTasks == null)
				{
					this.subTasks = this.func();
					return;
				}
				if (this.currentTask != null && !this.currentTask.Done)
				{
					this.currentTask.Update();
					return;
				}
				if (this.subTasks.MoveNext())
				{
					this.currentTask = this.subTasks.Current;
					return;
				}
				base.Done = true;
			}

			private Func<IEnumerator<TerminalLogSink.Task>> func;

			private TerminalLogSink.Task currentTask;

			private IEnumerator<TerminalLogSink.Task> subTasks;
		}

		// Token: 0x02000013 RID: 19
		public enum LogMode
		{
			All,
			Chat,
			Inventory,
			System
		}

		// Token: 0x02000014 RID: 20
		// Console.Out / Console.Error are redirected here: text is buffered per line and written under consoleLock,
		// so game output and log output never interleave inside a line.
		public class AsyncWriter : TextWriter
		{
			public AsyncWriter(TerminalLogSink sink, bool isError)
			{
				this.m_sink = sink;
				this.m_error = isError;
			}

			public override Encoding Encoding
			{
				get
				{
					return System.Text.Encoding.UTF8;
				}
			}

			public override void Write(char value)
			{
				this.Append(value.ToString());
			}

			public override void Write(string value)
			{
				if (!string.IsNullOrEmpty(value))
				{
					this.Append(value);
				}
			}

			public override void WriteLine(string value)
			{
				this.Append((value ?? string.Empty) + "\n");
			}

			public override void WriteLine()
			{
				this.Append("\n");
			}

			public override void Flush()
			{
				lock (this.m_sink.consoleLock)
				{
					if (this.m_partial.Length > 0)
					{
						string rest = this.m_partial.ToString();
						this.m_partial.Clear();
						this.m_sink.WriteRaw(rest, this.m_error);
					}
				}
			}

			private void Append(string text)
			{
				lock (this.m_sink.consoleLock)
				{
					this.m_partial.Append(text);
					int cut = -1;
					for (int i = this.m_partial.Length - 1; i >= 0; i--)
					{
						if (this.m_partial[i] == '\n')
						{
							cut = i;
							break;
						}
					}
					if (cut < 0)
					{
						return;
					}
					string complete = this.m_partial.ToString(0, cut + 1);
					this.m_partial.Remove(0, cut + 1);
					this.m_sink.WriteRaw(complete, this.m_error);
				}
			}

			private readonly TerminalLogSink m_sink;

			private readonly bool m_error;

			private readonly StringBuilder m_partial = new StringBuilder();
		}
	}
}
