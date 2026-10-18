using System;
using System.Collections.Generic;
using System.IO;
using Engine;

namespace Game;

public class GameLogSink : ILogSink
{
	public static Stream m_stream;

	public static StreamWriter m_writer;

	public GameLogSink()
	{
		try
		{
			if (m_stream != null)
			{
				throw new InvalidOperationException("GameLogSink already created.");
			}
			string path = Storage.CombinePaths("app:/Bugs", "Game.log");
			Storage.CreateDirectory("app:/Bugs");
			m_stream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
			if (m_stream.Length > 10485760)
			{
				m_stream.Dispose();
				m_stream = Storage.OpenFile(path, OpenFileMode.Create);
			}
			m_stream.Position = m_stream.Length;
			m_writer = new StreamWriter(m_stream);
		}
		catch (Exception ex)
		{
			Engine.Log.Error("Error creating GameLogSink. Reason: {0}", ex.Message);
		}
	}

	public static string GetRecentLog(int bytesCount)
	{
		if (m_stream == null)
		{
			return string.Empty;
		}
		lock (m_stream)
		{
			try
			{
				m_stream.Position = MathUtils.Max(m_stream.Position - bytesCount, 0L);
				return new StreamReader(m_stream).ReadToEnd();
			}
			finally
			{
				m_stream.Position = m_stream.Length;
			}
		}
	}

	public static List<string> GetRecentLogLines(int bytesCount)
	{
		if (m_stream == null)
		{
			return new List<string>();
		}
		lock (m_stream)
		{
			try
			{
				m_stream.Position = MathUtils.Max(m_stream.Position - bytesCount, 0L);
				StreamReader color2 = new StreamReader(m_stream);
				List<string> sideColor = new List<string>();
				while (true)
				{
					string topColor = color2.ReadLine();
					if (topColor == null)
					{
						break;
					}
					sideColor.Add(topColor);
				}
				return sideColor;
			}
			finally
			{
				m_stream.Position = m_stream.Length;
			}
		}
	}

	public void Log(LogType type, string message)
	{
		if (m_stream != null)
		{
			lock (m_stream)
			{
				string num3 = type switch
				{
					LogType.Debug => "DEBUG: ", 
					LogType.Verbose => "INFO: ", 
					LogType.Information => "INFO: ", 
					LogType.Warning => "WARNING: ", 
					LogType.Error => "ERROR: ", 
					_ => string.Empty, 
				};
				m_writer.Write(DateTime.Now.ToString("HH:mm:ss.fff"));
				m_writer.Write(" ");
				m_writer.Write(num3);
				m_writer.WriteLine(message);
				m_writer.Flush();
			}
		}
	}
}
