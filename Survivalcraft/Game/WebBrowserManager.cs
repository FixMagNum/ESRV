using System;
using System.Diagnostics;
using Engine;

namespace Game;

public static class WebBrowserManager
{
	public static void LaunchBrowser(string url)
	{
		AnalyticsManager.LogEvent("[WebBrowserManager] Launching browser", new AnalyticsParameter("Url", url));
		if (!url.Contains("://"))
		{
			url = "http://" + url;
		}
		try
		{
			Process.Start(url);
		}
		catch (Exception ex)
		{
			Log.Error(string.Format("Error launching web browser with URL \"{0}\". Reason: {1}", new object[2] { url, ex.Message }));
		}
	}
}
