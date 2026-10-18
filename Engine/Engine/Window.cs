using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using Engine.Audio;
using Engine.Graphics;
using Engine.Input;

namespace Engine;

public static class Window
{
	[DllImport("winmm.dll")]
	internal static extern uint timeBeginPeriod(uint period);

	[DllImport("winmm.dll")]
	internal static extern uint timeEndPeriod(uint period);

	private enum State
	{
		Uncreated,
		Inactive,
		Active
	}

	private static bool m_closing;

	private static float m_dpiScale;

	private static int? m_swapInterval;

	private static State m_state;

	private static readonly Stopwatch _watchUpdate;

	private const double MaxFrequency = 500.0;

	private static double _updateEpsilon;

	private static double _updateFrequency;

	public static Point2 ScreenSize => new Point2(800, 600);

	public static WindowMode WindowMode
	{
		get
		{
			VerifyWindowOpened();
			return WindowMode.Fullscreen;
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static Point2 Position
	{
		get
		{
			VerifyWindowOpened();
			return Point2.Zero;
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static Point2 Size
	{
		get
		{
			VerifyWindowOpened();
			return new Point2(800, 600);
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static string Title
	{
		get
		{
			VerifyWindowOpened();
			return "Survivalcraft";
		}
		set
		{
			VerifyWindowOpened();
		}
	}

	public static int PresentationInterval
	{
		get
		{
			VerifyWindowOpened();
			if (!m_swapInterval.HasValue)
			{
				return 0;
			}
			return m_swapInterval.Value;
		}
		set
		{
			VerifyWindowOpened();
			value = MathUtils.Clamp(value, 0, 4);
			if (value != PresentationInterval)
			{
				m_swapInterval = value;
			}
		}
	}

	public static bool IsCreated => m_state != State.Uncreated;

	public static bool IsActive => m_state == State.Active;

	public static double UpdateFrequency
	{
		get
		{
			return _updateFrequency;
		}
		set
		{
			if (value < 1.0)
			{
				_updateFrequency = 0.0;
			}
			else if (value <= 500.0)
			{
				_updateFrequency = value;
			}
			else
			{
				_updateFrequency = 500.0;
			}
		}
	}

	public static event Action Created;

	public static event Action Resized;

	public static event Action Activated;

	public static event Action Deactivated;

	public static event Action Closed;

	public static event Action Frame;

	public static event Action<UnhandledExceptionInfo> UnhandledException;

	public static event Action<Uri> HandleUri;

	static Window()
	{
		_watchUpdate = new Stopwatch();
		_updateFrequency = 60.0;
		m_dpiScale = 1f;
	}

	public static void Run(int width = 0, int height = 0, WindowMode windowMode = WindowMode.Fixed, string title = "")
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			try { timeBeginPeriod(1); } catch { }
		}

		if ((width != 0 || height != 0) && (width <= 0 || height <= 0))
		{
			throw new ArgumentOutOfRangeException("size");
		}
		AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args)
		{
			Exception ex = args.ExceptionObject as Exception;
			if (ex == null)
			{
				ex = new Exception($"Unknown exception. Additional information: {args.ExceptionObject}");
			}
			UnhandledExceptionInfo unhandledExceptionInfo = new UnhandledExceptionInfo(ex);
			if (Window.UnhandledException != null)
			{
				Window.UnhandledException(unhandledExceptionInfo);
			}
			if (!unhandledExceptionInfo.IsHandled)
			{
				Log.Error("Application terminating due to unhandled exception {0}", unhandledExceptionInfo.Exception);
				Environment.Exit(1);
			}
		};
		m_dpiScale = (float)Size.X / 400f;
		width = ((width == 0) ? (ScreenSize.X * 3 / 4) : width);
		height = ((height == 0) ? (ScreenSize.Y * 3 / 4) : height);
		WindowMode = windowMode;
		_watchUpdate.Start();
		LoadHandler(null, null);
		Console.WriteLine("Initialized");
		while (true)
		{
			_watchUpdate.Restart();
			RenderFrameHandler(null, null);
			if (m_closing)
			{
				break;
			}
			double num = ((UpdateFrequency == 0.0) ? 0.0 : (1.0 / UpdateFrequency));
			while (_watchUpdate.Elapsed.TotalSeconds < num)
			{
				double remaining = num - _watchUpdate.Elapsed.TotalSeconds;
				if (remaining > 0.005)
				{
					Thread.Sleep(1);
				}
				else if (remaining > 0.001)
				{
					Thread.Sleep(0);
				}
				else
				{
					Thread.Yield();
				}
			}
		}
		ClosedHandler(null, null);
	}

	public static void Close()
	{
		VerifyWindowOpened();
		m_closing = true;
	}

	private static void LoadHandler(object sender, EventArgs args)
	{
		InitializeAll();
		SubscribeToEvents();
		m_state = State.Inactive;
		if (Window.Created != null)
		{
			Window.Created();
		}
		if (m_state == State.Inactive)
		{
			m_state = State.Active;
			if (Window.Activated != null)
			{
				Window.Activated();
			}
		}
	}

	private static void FocusedChangedHandler(object sender, EventArgs args)
	{
		Keyboard.Clear();
		Mouse.Clear();
		Touch.Clear();
	}

	private static void ClosedHandler(object sender, EventArgs args)
	{
		if (m_state == State.Active)
		{
			m_state = State.Inactive;
			if (Window.Deactivated != null)
			{
				Window.Deactivated();
			}
		}
		if (m_state == State.Inactive)
		{
			m_state = State.Uncreated;
			if (Window.Closed != null)
			{
				Window.Closed();
			}
		}
		UnsubscribeFromEvents();
		DisposeAll();
	}

	private static void ResizeHandler(object sender, EventArgs args)
	{
		Display.Resize();
		if (Window.Resized != null)
		{
			Window.Resized();
		}
	}

	private static void RenderFrameHandler(object sender, EventArgs args)
	{
		BeforeFrameAll();
		if (Window.Frame != null)
		{
			Window.Frame();
		}
		AfterFrameAll();
	}

	private static void VerifyWindowOpened()
	{
	}

	private static void SubscribeToEvents()
	{
	}

	private static void UnsubscribeFromEvents()
	{
	}

	private static void InitializeAll()
	{
		Dispatcher.Initialize();
		Display.Initialize();
		Keyboard.Initialize();
		Mouse.Initialize();
		Touch.Initialize();
		GamePad.Initialize();
		Mixer.Initialize();
	}

	private static void DisposeAll()
	{
		Dispatcher.Dispose();
		Display.Dispose();
		Keyboard.Dispose();
		Mouse.Dispose();
		Touch.Dispose();
		GamePad.Dispose();
		Mixer.Dispose();
	}

	private static void BeforeFrameAll()
	{
		Time.BeforeFrame();
		Dispatcher.BeforeFrame();
		Display.BeforeFrame();
		Keyboard.BeforeFrame();
		Mouse.BeforeFrame();
		Touch.BeforeFrame();
		GamePad.BeforeFrame();
		Mixer.BeforeFrame();
	}

	private static void AfterFrameAll()
	{
		Time.AfterFrame();
		Dispatcher.AfterFrame();
		Display.AfterFrame();
		Keyboard.AfterFrame();
		Mouse.AfterFrame();
		Touch.AfterFrame();
		GamePad.AfterFrame();
		Mixer.AfterFrame();
	}
}
