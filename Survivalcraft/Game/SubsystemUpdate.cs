using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemUpdate : Subsystem
{
	public class UpdateableInfo
	{
		public UpdateOrder UpdateOrder;
	}

	public class Comparer : IComparer<IUpdateable>
	{
		public static Comparer Instance;

		public int Compare(IUpdateable u1, IUpdateable u2)
		{
			int num = u1.UpdateOrder - u2.UpdateOrder;
			if (num != 0)
			{
				return num;
			}
			return u1.GetHashCode() - u2.GetHashCode();
		}

		static Comparer()
		{
			Instance = new Comparer();
		}
	}

	public SubsystemTime m_subsystemTime;

	public Dictionary<IUpdateable, UpdateableInfo> m_updateables;

	public Dictionary<IUpdateable, bool> m_toAddOrRemove;

	public List<IUpdateable> m_sortedUpdateables;

	private readonly Stopwatch m_stopwatch = new Stopwatch();

	private List<IUpdateable> m_crashedComponents = new List<IUpdateable>();

	private Dictionary<string, double> m_frameExecutionTimes;

	private Dictionary<string, int> m_frameExecutionCounts;

	public int UpdateablesCount => m_updateables.Count;

	public int UpdatesPerFrame { get; set; }

	public void Update()
	{
		if (m_frameExecutionTimes == null)
		{
			m_frameExecutionTimes = new Dictionary<string, double>();
		}
		if (m_frameExecutionCounts == null)
		{
			m_frameExecutionCounts = new Dictionary<string, int>();
		}
		m_frameExecutionTimes.Clear();
		m_frameExecutionCounts.Clear();
		for (int i = 0; i < UpdatesPerFrame; i++)
		{
			m_subsystemTime.NextFrame();
			bool flag = false;
			foreach (KeyValuePair<IUpdateable, bool> item in m_toAddOrRemove)
			{
				if (item.Value)
				{
					m_updateables.Add(item.Key, new UpdateableInfo
					{
						UpdateOrder = item.Key.UpdateOrder
					});
					flag = true;
				}
				else
				{
					m_updateables.Remove(item.Key);
					flag = true;
				}
			}
			m_toAddOrRemove.Clear();
			foreach (KeyValuePair<IUpdateable, UpdateableInfo> updateable in m_updateables)
			{
				UpdateOrder updateOrder = updateable.Key.UpdateOrder;
				if (updateOrder != updateable.Value.UpdateOrder)
				{
					flag = true;
					updateable.Value.UpdateOrder = updateOrder;
				}
			}
			if (flag)
			{
				m_sortedUpdateables.Clear();
				foreach (IUpdateable key in m_updateables.Keys)
				{
					m_sortedUpdateables.Add(key);
				}
				m_sortedUpdateables.Sort(Comparer.Instance);
			}
			float dt = MathUtils.Clamp(m_subsystemTime.GameTimeDelta, 0f, 0.1f);
			m_crashedComponents.Clear();
			string arg = "Update Loop";
			long num = 0L;
			long num2 = 0L;
			foreach (IUpdateable sortedUpdateable in m_sortedUpdateables)
			{
				try
				{
					if (SettingsManager.DebugMode)
					{
						m_stopwatch.Restart();
					}
					sortedUpdateable.Update(dt);
					if (SettingsManager.DebugMode)
					{
						m_stopwatch.Stop();
						long elapsedMilliseconds = m_stopwatch.ElapsedMilliseconds;
						double totalMilliseconds = m_stopwatch.Elapsed.TotalMilliseconds;
						string text = sortedUpdateable.GetType().Name;
						if (sortedUpdateable is Component { Entity: not null } component && component.Entity.ValuesDictionary != null)
						{
							string name = component.Entity.ValuesDictionary.DatabaseObject.Name;
							text = text + " [" + name + "]";
						}
						if (!m_frameExecutionTimes.ContainsKey(text))
						{
							m_frameExecutionTimes[text] = 0.0;
							m_frameExecutionCounts[text] = 0;
						}
						m_frameExecutionTimes[text] += totalMilliseconds;
						m_frameExecutionCounts[text]++;
						num2 += elapsedMilliseconds;
						if (elapsedMilliseconds > num)
						{
							num = elapsedMilliseconds;
							arg = sortedUpdateable.GetType().Name;
						}
						if (elapsedMilliseconds > 60)
						{
							Log.Warning($"[LAG] {arg}: {elapsedMilliseconds}ms");
						}
					}
				}
				catch (Exception ex)
				{
					string fullName = sortedUpdateable.GetType().FullName;
					Log.Error("[CRITICAL] Subsystem '" + fullName + "' crashed and will be disabled.\nError: " + ex.Message + "\nStack: " + ex.StackTrace);
					m_crashedComponents.Add(sortedUpdateable);
				}
			}
			if (SettingsManager.DebugMode)
			{
				foreach (KeyValuePair<string, double> frameExecutionTime in m_frameExecutionTimes)
				{
					int count = 0;
					if (m_frameExecutionCounts.ContainsKey(frameExecutionTime.Key))
					{
						count = m_frameExecutionCounts[frameExecutionTime.Key];
					}
					PerformanceManager.ReportSystemLoad(frameExecutionTime.Key, frameExecutionTime.Value, count);
				}
			}
			if (SettingsManager.DebugMode)
			{
				if (num > 2)
				{
					PerformanceManager.CurrentTask = $"{arg}\u00a0({num}ms)";
				}
				else if (!PerformanceManager.CurrentTask.StartsWith("Draw"))
				{
					PerformanceManager.CurrentTask = "Rendering\u00a0/\u00a0GC\u00a0/\u00a0Other";
				}
			}
			if (m_crashedComponents.Count <= 0)
			{
				continue;
			}
			foreach (IUpdateable crashedComponent in m_crashedComponents)
			{
				RemoveUpdateable(crashedComponent);
			}
		}
	}

	public void AddUpdateable(IUpdateable updateable)
	{
		m_toAddOrRemove[updateable] = true;
	}

	public void RemoveUpdateable(IUpdateable updateable)
	{
		m_toAddOrRemove[updateable] = false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		foreach (IUpdateable color in Project.FindSubsystems<IUpdateable>())
		{
			AddUpdateable(color);
		}
		UpdatesPerFrame = 1;
	}

	public override void OnEntityAdded(Entity entity)
	{
		foreach (IUpdateable item in entity.FindComponents<IUpdateable>())
		{
			AddUpdateable(item);
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		foreach (IUpdateable y in entity.FindComponents<IUpdateable>())
		{
			RemoveUpdateable(y);
		}
	}

	public SubsystemUpdate()
	{
		m_updateables = new Dictionary<IUpdateable, UpdateableInfo>();
		m_toAddOrRemove = new Dictionary<IUpdateable, bool>();
		m_sortedUpdateables = new List<IUpdateable>();
		m_frameExecutionTimes = new Dictionary<string, double>();
		m_frameExecutionCounts = new Dictionary<string, int>();
	}
}
