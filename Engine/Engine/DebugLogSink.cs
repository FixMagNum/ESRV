namespace Engine;

public class DebugLogSink : ILogSink
{
	public LogType MinimumLogType { get; set; }

	public void Log(LogType logType, string message)
	{
		if (logType > MinimumLogType)
		{
			if ((uint)logType > 4u)
			{
				_ = string.Empty;
			}
		}
	}

	public void Dispose()
	{
	}
}
