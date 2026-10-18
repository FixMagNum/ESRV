using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine;

public static class Dispatcher
{
	private struct ActionInfo
	{
		public Action Action;

		public ManualResetEventSlim Event;
	}

	private static int? m_mainThreadId;

	private static List<ActionInfo> m_actionInfos = new List<ActionInfo>();

	private static List<ActionInfo> m_currentActionInfos = new List<ActionInfo>();

	public static int MainThreadId
	{
		get
		{
			if (!m_mainThreadId.HasValue)
			{
				throw new InvalidOperationException("Dispatcher is not initialized.");
			}
			return m_mainThreadId.Value;
		}
	}

	public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

	public static void Dispatch(Action action, bool waitUntilCompleted = false)
	{
		if (!m_mainThreadId.HasValue)
		{
			throw new InvalidOperationException("Dispatcher is not initialized.");
		}
		if (m_mainThreadId.Value == Environment.CurrentManagedThreadId)
		{
			action();
			return;
		}
		if (waitUntilCompleted)
		{
			ActionInfo item = new ActionInfo
			{
				Action = action,
				Event = new ManualResetEventSlim(initialState: false)
			};
			lock (m_actionInfos)
			{
				m_actionInfos.Add(item);
			}
			item.Event.Wait();
			item.Event.Dispose();
			return;
		}
		lock (m_actionInfos)
		{
			List<ActionInfo> actionInfos = m_actionInfos;
			ActionInfo item2 = new ActionInfo
			{
				Action = action
			};
			actionInfos.Add(item2);
		}
	}

	internal static void Initialize()
	{
		m_mainThreadId = Environment.CurrentManagedThreadId;
	}

	internal static void Dispose()
	{
	}

	internal static void BeforeFrame()
	{
		m_currentActionInfos.Clear();
		lock (m_actionInfos)
		{
			m_currentActionInfos.AddRange(m_actionInfos);
			m_actionInfos.Clear();
		}
		foreach (ActionInfo currentActionInfo in m_currentActionInfos)
		{
			try
			{
				currentActionInfo.Action();
			}
			catch (Exception ex)
			{
				Log.Error("Dispatched action failed. Reason: {0}", ex);
			}
			finally
			{
				if (currentActionInfo.Event != null)
				{
					currentActionInfo.Event.Set();
				}
			}
		}
	}

	internal static void AfterFrame()
	{
	}
}
