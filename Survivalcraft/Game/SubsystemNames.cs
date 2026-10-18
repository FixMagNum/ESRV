using System;
using System.Collections.Generic;
using GameEntitySystem;

namespace Game;

public class SubsystemNames : Subsystem
{
	public Dictionary<string, ComponentName> m_componentsByName = new Dictionary<string, ComponentName>();

	public Component FindComponentByName(string name, Type componentType, string componentName)
	{
		return FindEntityByName(name)?.FindComponent(componentType, componentName, throwOnError: false);
	}

	public T FindComponentByName<T>(string name, string componentName) where T : Component
	{
		Entity entity = FindEntityByName(name);
		if (entity == null)
		{
			return null;
		}
		return entity.FindComponent<T>(componentName, throwOnError: false);
	}

	public Entity FindEntityByName(string name)
	{
		m_componentsByName.TryGetValue(name, out var node);
		return node?.Entity;
	}

	public static string GetEntityName(Entity entity)
	{
		ComponentName subsystemTerrain = entity.FindComponent<ComponentName>();
		if (subsystemTerrain != null)
		{
			return subsystemTerrain.Name;
		}
		return string.Empty;
	}

	public override void OnEntityAdded(Entity entity)
	{
		foreach (ComponentName num3 in entity.FindComponents<ComponentName>())
		{
			m_componentsByName.Add(num3.Name, num3);
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		foreach (ComponentName vector in entity.FindComponents<ComponentName>())
		{
			m_componentsByName.Remove(vector.Name);
		}
	}
}
