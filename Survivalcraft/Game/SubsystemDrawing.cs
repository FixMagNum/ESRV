using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemDrawing : Subsystem
{
	public Dictionary<IDrawable, bool> m_drawables = new Dictionary<IDrawable, bool>();

	public SortedMultiCollection<int, IDrawable> m_sortedDrawables = new SortedMultiCollection<int, IDrawable>();

	private readonly Stopwatch m_stopwatch = new Stopwatch();

	private List<IDrawable> m_crashedDrawables = new List<IDrawable>();

	public int DrawablesCount => m_drawables.Count;

	public void AddDrawable(IDrawable drawable)
	{
		m_drawables.Add(drawable, value: true);
	}

	public void RemoveDrawable(IDrawable drawable)
	{
		m_drawables.Remove(drawable);
	}

	public void Draw(Camera camera)
	{
		m_sortedDrawables.Clear();
		foreach (IDrawable key2 in m_drawables.Keys)
		{
			int[] drawOrders = key2.DrawOrders;
			foreach (int key in drawOrders)
			{
				m_sortedDrawables.Add(key, key2);
			}
		}
		m_crashedDrawables.Clear();
		long num = 0L;
		string arg = "Draw\u00a0Loop";
		for (int j = 0; j < m_sortedDrawables.Count; j++)
		{
			KeyValuePair<int, IDrawable> keyValuePair = m_sortedDrawables[j];
			if (SettingsManager.DebugMode)
			{
				m_stopwatch.Restart();
			}
			try
			{
				keyValuePair.Value.Draw(camera, keyValuePair.Key);
			}
			catch (Exception ex)
			{
				string fullName = keyValuePair.Value.GetType().FullName;
				Log.Error("[CRITICAL]\u00a0Drawable\u00a0'" + fullName + "'\u00a0crashed\u00a0during\u00a0draw\u00a0and\u00a0will\u00a0be\u00a0disabled.\nError:\u00a0" + ex.Message + "\nStack:\u00a0" + ex.StackTrace);
				m_crashedDrawables.Add(keyValuePair.Value);
			}
			if (SettingsManager.DebugMode)
			{
				m_stopwatch.Stop();
				long elapsedMilliseconds = m_stopwatch.ElapsedMilliseconds;
				if (elapsedMilliseconds > num)
				{
					num = elapsedMilliseconds;
					arg = keyValuePair.Value.GetType().Name;
				}
				if (elapsedMilliseconds > 60)
				{
					Log.Warning($"[LAG\u00a0DRAW]\u00a0{keyValuePair.Value.GetType().Name}:\u00a0{elapsedMilliseconds}ms");
				}
			}
		}
		if (SettingsManager.DebugMode && num > 2)
		{
			PerformanceManager.CurrentTask = $"Draw {arg} ({num}ms)";
		}
		if (m_crashedDrawables.Count <= 0)
		{
			return;
		}
		foreach (IDrawable crashedDrawable in m_crashedDrawables)
		{
			RemoveDrawable(crashedDrawable);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		foreach (IDrawable item in Project.FindSubsystems<IDrawable>())
		{
			AddDrawable(item);
		}
	}

	public override void OnEntityAdded(Entity entity)
	{
		foreach (IDrawable item in entity.FindComponents<IDrawable>())
		{
			AddDrawable(item);
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		foreach (IDrawable item in entity.FindComponents<IDrawable>())
		{
			RemoveDrawable(item);
		}
	}
}
