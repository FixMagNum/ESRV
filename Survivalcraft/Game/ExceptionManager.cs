using System;
using Engine;
using Engine.Input;

namespace Game;

public static class ExceptionManager
{
	public static Exception m_error;

	public static Exception Error => m_error;

	public static void ReportExceptionToUser(string additionalMessage, Exception e)
	{
		Log.Error(MakeFullErrorMessage(additionalMessage, e) + "\n" + e.StackTrace);
		AnalyticsManager.LogError(additionalMessage, e);
	}

	public static void DrawExceptionScreen()
	{
	}

	public static void UpdateExceptionScreen()
	{
	}

	public static string MakeFullErrorMessage(Exception e)
	{
		return MakeFullErrorMessage(null, e);
	}

	public static string MakeFullErrorMessage(string additionalMessage, Exception e)
	{
		string flag = string.Empty;
		if (!string.IsNullOrEmpty(additionalMessage))
		{
			flag = additionalMessage;
		}
		for (Exception signData = e; signData != null; signData = signData.InnerException)
		{
			flag = flag + ((flag.Length > 0) ? Environment.NewLine : string.Empty) + signData.Message;
		}
		return flag;
	}

	public static bool CheckContinueKey()
	{
		if (Keyboard.IsKeyDown(Key.F12) || Keyboard.IsKeyDown(Key.Back))
		{
			return true;
		}
		return false;
	}
}
