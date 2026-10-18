using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Game;

public static class AsyncDispatcher
{
	public interface ITask
	{
		bool Update();
	}

	public class WaitUntil : ITask
	{
		private Func<bool> predicate;

		private Action action;

		public WaitUntil(Func<bool> predicate, Action action)
		{
			this.predicate = predicate;
			this.action = action;
		}

		public bool Update()
		{
			if (predicate())
			{
				action?.Invoke();
				return true;
			}
			return false;
		}
	}

	public class SimpleTask : ITask
	{
		private Action action;

		public SimpleTask(Action action)
		{
			this.action = action;
		}

		public bool Update()
		{
			action?.Invoke();
			return true;
		}
	}

	public class RepeatTask : ITask
	{
		private Func<bool> func;

		public RepeatTask(Func<bool> func)
		{
			this.func = func;
		}

		public bool Update()
		{
			return func();
		}
	}

	public class Task : ITask
	{
		private IEnumerator<bool> generator;

		private Task next;

		public Task(IEnumerator<bool> generator)
		{
			this.generator = generator;
		}

		public bool Update()
		{
			if (!generator.MoveNext() || generator.Current)
			{
				if (next == null)
				{
					return true;
				}
				generator = next.generator;
				next = next.next;
			}
			return false;
		}

		public Task ContinueWith(IEnumerator<bool> generator)
		{
			next = new Task(generator);
			return next;
		}
	}

	private static ConcurrentQueue<ITask> actions = new ConcurrentQueue<ITask>();

	private static List<ITask> toAdd = new List<ITask>();

	public static void Update()
	{
		if (ScreensManager.CurrentScreen == null)
		{
			return;
		}
		ITask result;
		while (actions.TryDequeue(out result))
		{
			try
			{
				if (!result.Update())
				{
					toAdd.Add(result);
				}
			}
			catch
			{
			}
		}
		foreach (ITask item in toAdd)
		{
			Dispatch(item);
		}
		toAdd.Clear();
	}

	public static void Dispatch(Action action)
	{
		Dispatch(new SimpleTask(action));
	}

	public static void Dispatch(Func<IEnumerator<bool>> func)
	{
		Dispatch(func());
	}

	public static void Dispatch(IEnumerator<bool> action)
	{
		Dispatch(new Task(action));
	}

	public static void Dispatch(Func<bool> func)
	{
		Dispatch(new RepeatTask(func));
	}

	public static void Dispatch(ITask task)
	{
		actions.Enqueue(task);
	}
}
