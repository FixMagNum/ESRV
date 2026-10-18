using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;
using Game.NetWork;
using LiteNetLib;

namespace Game;

public static class PerformanceManager
{
	public struct FrameData
	{
		public float CpuTime;

		public float TotalTime;
	}

	public class VisualLogMessage
	{
		public string Text;

		public Color Color;

		public float TimeAlive;
	}

	public class SpikeInfo
	{
		public float TimeAlive;

		public string Source;

		public float Duration;

		public double GameTimeOccurred;

		public string PeriodicityNote;
	}

	public class SystemLoadInfo
	{
		public string Name;

		public double SmoothedTime;

		public double LastFrameTime;

		public Color Color;

		public int LastFrameCount;
	}

	public static PrimitivesRenderer2D m_primitivesRenderer;

	public static RunningAverage m_averageFrameTime;

	public static RunningAverage m_averageCpuFrameTime;

	public static float? m_longTermAverageFrameTime;

	public static long m_totalMemoryUsed;

	public static long m_totalGpuMemoryUsed;

	public static StateMachine m_stateMachine;

	public static double m_totalGameTime;

	public static double m_totalFrameTime;

	public static double m_totalCpuFrameTime;

	public static int m_frameCount;

	public static string m_statsString;

	public static FrameData[] m_frameData;

	public static int m_frameDataIndex;

	public static string m_debugString;

	public static Widget m_lastHoveredWidget;

	public static List<VisualLogMessage> m_visualLogs;

	public static object m_visualLogsLock;

	public const float MessageLifetime = 7f;

	public const float MessageFadeTime = 1f;

	public static string CurrentTask;

	private static List<SpikeInfo> m_spikeLogs;

	private static List<double> m_lastSpikeTimes;

	public static Dictionary<string, SystemLoadInfo> m_systemLoads;

	private static List<SystemLoadInfo> m_sortedLoads;

	public static float CurrentTps;

	private static float m_tpsTimer;

	private static int m_ticksCount;

	public static float? LongTermAverageFrameTime => m_longTermAverageFrameTime;

	public static float AverageFrameTime => m_averageFrameTime.Value;

	public static float AverageCpuFrameTime => m_averageCpuFrameTime.Value;

	public static long TotalMemoryUsed => m_totalMemoryUsed;

	public static long TotalGpuMemoryUsed => m_totalGpuMemoryUsed;

	public static float Fps => 1f / m_averageFrameTime.Value;

	static PerformanceManager()
	{
		m_systemLoads = new Dictionary<string, SystemLoadInfo>();
		m_sortedLoads = new List<SystemLoadInfo>();
		CurrentTask = "Idle";
		m_visualLogs = new List<VisualLogMessage>();
		m_visualLogsLock = new object();
		m_spikeLogs = new List<SpikeInfo>();
		m_lastSpikeTimes = new List<double>();
		m_debugString = string.Empty;
		m_primitivesRenderer = new PrimitivesRenderer2D();
		m_averageFrameTime = new RunningAverage(1f);
		m_averageCpuFrameTime = new RunningAverage(1f);
		m_stateMachine = new StateMachine();
		m_statsString = string.Empty;
		Log.OnLogMessage = (Action<LogType, string>)Delegate.Combine(Log.OnLogMessage, (Action<LogType, string>)((LogType type, string msg) =>
		{
			Color color = Color.White;
			switch (type)
			{
			case LogType.Debug:
				color = Color.Cyan;
				break;
			case LogType.Verbose:
				color = Color.Gray;
				break;
			case LogType.Information:
				color = Color.White;
				break;
			case LogType.Warning:
				color = Color.Yellow;
				break;
			case LogType.Error:
				color = new Color(255, 80, 80);
				break;
			}
			lock (m_visualLogsLock)
			{
				m_visualLogs.Add(new VisualLogMessage
				{
					Text = msg,
					Color = color,
					TimeAlive = 0f
				});
				if (m_visualLogs.Count > 20)
				{
					m_visualLogs.RemoveAt(0);
				}
			}
		}));
		m_stateMachine.AddState("PreMeasure", () =>
		{
			m_totalGameTime = 0.0;
		}, () =>
		{
			if (GameManager.Project != null)
			{
				m_totalGameTime += Time.FrameDuration;
				if (m_totalGameTime > 60.0)
				{
					m_stateMachine.TransitionTo("Measuring");
				}
			}
		}, null);
		m_stateMachine.AddState("Measuring", () =>
		{
			m_totalFrameTime = 0.0;
			m_totalCpuFrameTime = 0.0;
			m_frameCount = 0;
		}, () =>
		{
			if (GameManager.Project != null)
			{
				if (ScreensManager.CurrentScreen != null && ScreensManager.CurrentScreen.GetType() == typeof(GameScreen))
				{
					float lastFrameTime = Program.LastFrameTime;
					float lastCpuFrameTime = Program.LastCpuFrameTime;
					if (lastFrameTime > 0f && lastFrameTime < 1f && lastCpuFrameTime > 0f && lastCpuFrameTime < 1f)
					{
						m_totalFrameTime += lastFrameTime;
						m_totalCpuFrameTime += lastCpuFrameTime;
						m_frameCount++;
					}
					if (m_totalFrameTime > 180.0)
					{
						m_stateMachine.TransitionTo("PostMeasure");
					}
				}
			}
			else
			{
				m_stateMachine.TransitionTo("PreMeasure");
			}
		}, null);
		m_stateMachine.AddState("PostMeasure", () =>
		{
			if (m_frameCount > 0)
			{
				m_longTermAverageFrameTime = (float)(m_totalFrameTime / (double)m_frameCount);
				float num = (int)MathUtils.Round(MathUtils.Round(m_totalFrameTime / (double)m_frameCount / 0.004999999888241291) * 0.004999999888241291 * 1000.0);
				float num2 = (int)MathUtils.Round(MathUtils.Round(m_totalCpuFrameTime / (double)m_frameCount / 0.004999999888241291) * 0.004999999888241291 * 1000.0);
				AnalyticsManager.LogEvent("[PerformanceManager] Measurement", new AnalyticsParameter("FrameCount", m_frameCount.ToString()), new AnalyticsParameter("AverageFrameTime", num + "ms"), new AnalyticsParameter("AverageFrameCpuTime", num2 + "ms"));
				Log.Information("PerformanceManager Measurement: frames=" + m_frameCount + ", avgFrameTime=" + num + "ms, avgFrameCpuTime=" + num2 + "ms");
			}
		}, () =>
		{
			if (GameManager.Project == null)
			{
				m_stateMachine.TransitionTo("PreMeasure");
			}
		}, null);
		m_stateMachine.TransitionTo("PreMeasure");
	}

	public static void Update()
	{
		m_averageFrameTime.AddSample(Program.LastFrameTime);
		m_averageCpuFrameTime.AddSample(Program.LastCpuFrameTime);
		if (Time.PeriodicEvent(1.0, 0.0))
		{
			m_totalMemoryUsed = GC.GetTotalMemory(forceFullCollection: false);
			m_totalGpuMemoryUsed = Display.GetGpuMemoryUsage();
		}
		m_stateMachine.Update();
		float frameDuration = Time.FrameDuration;
		m_tpsTimer += frameDuration;
		m_ticksCount++;
		if (m_tpsTimer >= 1f)
		{
			CurrentTps = (float)m_ticksCount / m_tpsTimer;
			m_ticksCount = 0;
			m_tpsTimer = 0f;
		}
		lock (m_visualLogsLock)
		{
			for (int num = m_visualLogs.Count - 1; num >= 0; num--)
			{
				m_visualLogs[num].TimeAlive += frameDuration;
				if (m_visualLogs[num].TimeAlive > 7f)
				{
					m_visualLogs.RemoveAt(num);
				}
			}
		}
		UpdateSpikeDetection(frameDuration);
	}

	public static void Draw()
	{
		Vector2 point = ((!Mouse.MousePosition.HasValue) ? Vector2.Zero : new Vector2(Mouse.MousePosition.Value.X, Mouse.MousePosition.Value.Y));
		Vector2 scale = new Vector2(MathUtils.Round(MathUtils.Clamp(ScreensManager.RootWidget.GlobalScale, 1f, 4f)));
		Viewport viewport = Display.Viewport;
		if (SettingsManager.DebugMode)
		{
			lock (m_visualLogsLock)
			{
				Vector2 position = new Vector2(viewport.Width - 10, 10f);
				BitmapFont debugFont = BitmapFont.DebugFont;
				for (int i = 0; i < m_visualLogs.Count; i++)
				{
					VisualLogMessage visualLogMessage = m_visualLogs[i];
					float num = 1f;
					if (visualLogMessage.TimeAlive > 6f)
					{
						num = (7f - visualLogMessage.TimeAlive) / 1f;
					}
					m_primitivesRenderer.FontBatch(debugFont, 0, null, null, null, SamplerState.PointClamp).QueueText(visualLogMessage.Text, position, 0f, visualLogMessage.Color * num, TextAnchor.Right, scale, Vector2.Zero);
					position.Y += debugFont.LineHeight * scale.Y;
				}
			}
			if (m_spikeLogs.Count > 0)
			{
				Vector2 position2 = new Vector2(10f, 80f);
				BitmapFont debugFont2 = BitmapFont.DebugFont;
				m_primitivesRenderer.FontBatch(debugFont2, 0, null, null, null, SamplerState.PointClamp).QueueText("LAG SPIKES HISTORY:", position2, 0f, Color.Red, TextAnchor.Default, scale, Vector2.Zero);
				position2.Y += debugFont2.LineHeight * scale.Y;
				for (int num2 = m_spikeLogs.Count - 1; num2 >= 0; num2--)
				{
					SpikeInfo spikeInfo = m_spikeLogs[num2];
					float num3 = 1f;
					if (spikeInfo.TimeAlive > 12f)
					{
						num3 = (15f - spikeInfo.TimeAlive) / 3f;
					}
					string text = $"[{spikeInfo.Duration:0.0}ms] {spikeInfo.Source}{spikeInfo.PeriodicityNote}";
					Color color;
					if (spikeInfo.Duration > 100f)
					{
						color = Color.Red;
					}
					else
					{
						color = ((spikeInfo.Duration > 60f) ? Color.Orange : Color.Yellow);
					}
					m_primitivesRenderer.FontBatch(debugFont2, 0, null, null, null, SamplerState.PointClamp).QueueText(text, position2, 0f, color * num3, TextAnchor.Default, scale, Vector2.Zero);
					position2.Y += debugFont2.LineHeight * scale.Y;
				}
			}
			float y = 250f;
			if (m_spikeLogs.Count > 0)
			{
				y = MathUtils.Max(250f, 80f + (float)(m_spikeLogs.Count + 1) * (BitmapFont.DebugFont.LineHeight * scale.Y) + 20f);
			}
			Vector2 position3 = new Vector2(10f, y);
			BitmapFont debugFont3 = BitmapFont.DebugFont;
			m_primitivesRenderer.FontBatch(debugFont3, 0, null, null, null, SamplerState.PointClamp).QueueText("-- CONSTANT LOAD (TOP 10) --", position3, 0f, Color.Cyan, TextAnchor.Default, scale, Vector2.Zero);
			position3.Y += debugFont3.LineHeight * scale.Y;
			lock (m_systemLoads)
			{
				m_sortedLoads.Clear();
				foreach (KeyValuePair<string, SystemLoadInfo> systemLoad in m_systemLoads)
				{
					if (systemLoad.Value.SmoothedTime > 0.1)
					{
						m_sortedLoads.Add(systemLoad.Value);
					}
				}
				m_sortedLoads.Sort((SystemLoadInfo a, SystemLoadInfo b) => b.SmoothedTime.CompareTo(a.SmoothedTime));
				int num4 = Math.Min(m_sortedLoads.Count, 10);
				for (int num5 = 0; num5 < num4; num5++)
				{
					SystemLoadInfo systemLoadInfo = m_sortedLoads[num5];
					string text2 = $"{systemLoadInfo.SmoothedTime:0.00}ms : {systemLoadInfo.Name} (x{systemLoadInfo.LastFrameCount})";
					m_primitivesRenderer.FontBatch(debugFont3, 0, null, null, null, SamplerState.PointClamp).QueueText(text2, position3, 0f, systemLoadInfo.Color, TextAnchor.Default, scale, Vector2.Zero);
					position3.Y += debugFont3.LineHeight * scale.Y;
				}
			}
		}
		string text3 = string.Empty;
		if (SettingsManager.DebugMode)
		{
			Widget widget = Widget.HitTestGlobal(ScreensManager.RootWidget, point, null);
			if (widget != null)
			{
				string text4 = (string.IsNullOrEmpty(widget.Name) ? ("(Unnamed " + widget.GetType().Name + ")") : widget.Name);
				string title = widget.Title;
				if (!string.IsNullOrEmpty(title))
				{
					text4 = text4 + " [Title: " + title + "]";
				}
				text3 = $"CURSOR: {text4} @ {point.X:0},{point.Y:0} (Type: {widget.GetType().Name})";
			}
			else
			{
				text3 = "CURSOR: None (No UI Element)";
			}
			m_lastHoveredWidget = widget;
		}
		if (SettingsManager.DisplayFpsCounter || SettingsManager.DebugMode)
		{
			if (Time.PeriodicEvent(1.0, 0.0))
			{
				bool flag = CommonLib.WorkType == WorkType.Client;
				if (flag)
				{
					m_statsString = $"CPUMEM {(float)TotalMemoryUsed / 1024f / 1024f:0}MB, GPUMEM {(float)TotalGpuMemoryUsed / 1024f / 1024f:0}MB, CPU {AverageCpuFrameTime / AverageFrameTime * 100f:0}%, FPS {Fps:0}";
				}
				else
				{
					m_statsString = $"MEM {(float)TotalMemoryUsed / 1024f / 1024f:0}MB, CPU {AverageCpuFrameTime / AverageFrameTime * 100f:0}%, TPS {CurrentTps:0.0}, MSPT {AverageCpuFrameTime * 1000f:0.00}";
				}
				if (flag && CommonLib.Net.Server != null)
				{
					NetPeer peer = CommonLib.Net.Server.Peer;
					if (peer != null)
					{
						m_statsString += $", PL: {peer.Statistics.PacketLossPercent}% Ping:{peer.Ping}";
					}
				}
			}
			string empty = string.Empty;
			if (SettingsManager.DebugMode)
			{
				string statsString = m_statsString;
				statsString = ((!SettingsManager.DisplayFpsCounter) ? "DEBUG MODE ACTIVE" : (statsString + $" | Frame: {Program.LastFrameTime * 1000f:0.0}ms (CPU {Program.LastCpuFrameTime * 1000f:0.0}ms)"));
				empty = statsString + "\n" + text3;
			}
			else
			{
				empty = m_statsString;
			}
			m_primitivesRenderer.FontBatch(BitmapFont.DebugFont, 0, null, null, null, SamplerState.PointClamp).QueueText(empty, new Vector2(0f, 0f), 0f, Color.White, TextAnchor.Default, scale, Vector2.Zero);
		}
		if (SettingsManager.DisplayFpsRibbon)
		{
			float num6 = (((float)viewport.Width / scale.X > 480f) ? (scale.X * 2f) : scale.X);
			float num7 = (float)viewport.Height / -0.1f;
			float num8 = viewport.Height - 1;
			float num9 = 0.5f;
			int num10 = MathUtils.Max((int)((float)viewport.Width / num6), 1);
			if (m_frameData == null || m_frameData.Length != num10)
			{
				m_frameData = new FrameData[num10];
				m_frameDataIndex = 0;
			}
			m_frameData[m_frameDataIndex] = new FrameData
			{
				CpuTime = Program.LastCpuFrameTime,
				TotalTime = Program.LastFrameTime
			};
			m_frameDataIndex = (m_frameDataIndex + 1) % m_frameData.Length;
			FlatBatch2D flatBatch2D = m_primitivesRenderer.FlatBatch();
			Color color2 = Color.Orange * num9;
			Color color3 = Color.Red * num9;
			for (int num11 = m_frameData.Length - 1; num11 >= 0; num11--)
			{
				int num12 = (num11 - m_frameData.Length + 1 + m_frameDataIndex + m_frameData.Length) % m_frameData.Length;
				FrameData frameData = m_frameData[num12];
				float x = (float)num11 * num6;
				float x2 = (float)(num11 + 1) * num6;
				flatBatch2D.QueueQuad(new Vector2(x, num8), new Vector2(x2, num8 + frameData.CpuTime * num7), 0f, color2);
				flatBatch2D.QueueQuad(new Vector2(x, num8 + frameData.CpuTime * num7), new Vector2(x2, num8 + frameData.TotalTime * num7), 0f, color3);
			}
			flatBatch2D.QueueLine(new Vector2(0f, num8 + 1f / 60f * num7), new Vector2(viewport.Width, num8 + 1f / 60f * num7), 0f, Color.Green);
		}
		else
		{
			m_frameData = null;
		}
		m_primitivesRenderer.Flush();
	}

	private static void UpdateSpikeDetection(float dt)
	{
		for (int num = m_spikeLogs.Count - 1; num >= 0; num--)
		{
			m_spikeLogs[num].TimeAlive += dt;
			if (m_spikeLogs[num].TimeAlive > 15f)
			{
				m_spikeLogs.RemoveAt(num);
			}
		}
		float num2 = 40f;
		float num3 = Program.LastFrameTime * 1000f;
		if (!(num3 > num2))
		{
			return;
		}
		double realTime = Time.RealTime;
		string periodicityNote = "";
		if (m_lastSpikeTimes.Count > 0)
		{
			if (realTime - m_lastSpikeTimes.Last() < 1.0)
			{
				return;
			}
			m_lastSpikeTimes.Add(realTime);
			if (m_lastSpikeTimes.Count > 5)
			{
				m_lastSpikeTimes.RemoveAt(0);
			}
			if (m_lastSpikeTimes.Count >= 2)
			{
				double num4 = 0.0;
				int num5 = 0;
				for (int i = 1; i < m_lastSpikeTimes.Count; i++)
				{
					num4 += m_lastSpikeTimes[i] - m_lastSpikeTimes[i - 1];
					num5++;
				}
				num4 /= (double)num5;
				if (num4 > 2.0)
				{
					periodicityNote = $" (~{num4:0}s)";
				}
			}
		}
		else
		{
			m_lastSpikeTimes.Add(realTime);
		}
		m_spikeLogs.Add(new SpikeInfo
		{
			TimeAlive = 0f,
			Source = CurrentTask,
			Duration = num3,
			GameTimeOccurred = realTime,
			PeriodicityNote = periodicityNote
		});
		if (m_spikeLogs.Count > 10)
		{
			m_spikeLogs.RemoveAt(0);
		}
	}

	public static void ReportSystemLoad(string systemName, double timeMs, int count)
	{
		lock (m_systemLoads)
		{
			if (!m_systemLoads.TryGetValue(systemName, out var value))
			{
				value = new SystemLoadInfo
				{
					Name = systemName
				};
				m_systemLoads.Add(systemName, value);
			}
			value.LastFrameTime = timeMs;
			value.LastFrameCount = count;
			value.SmoothedTime = MathUtils.Lerp(value.SmoothedTime, timeMs, 0.05000000074505806);
			if (value.SmoothedTime > 4.0)
			{
				value.Color = Color.Red;
			}
			else if (value.SmoothedTime > 1.0)
			{
				value.Color = Color.Yellow;
			}
			else
			{
				value.Color = Color.Green;
			}
		}
	}
}
