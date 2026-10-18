using System.Collections.Generic;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBehaviorSelector : Component, IUpdateable
{
	public ComponentCreature m_componentCreature;

	public List<ComponentBehavior> m_behaviors = new List<ComponentBehavior>();

	private bool m_isActive;

	public bool IsDisableBehavior
	{
		get
		{
			return m_isActive;
		}
		set
		{
			m_isActive = value;
			if (value)
			{
				return;
			}
			foreach (ComponentBehavior behavior in m_behaviors)
			{
				behavior.IsActive = false;
			}
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void Update(float dt)
	{
		if (IsDisableBehavior)
		{
			return;
		}
		ComponentBehavior componentBehavior = null;
		if (m_componentCreature.ComponentHealth.Health > 0f)
		{
			float num = 0f;
			foreach (ComponentBehavior behavior in m_behaviors)
			{
				float importanceLevel = behavior.ImportanceLevel;
				if (importanceLevel > num)
				{
					num = importanceLevel;
					componentBehavior = behavior;
				}
			}
		}
		foreach (ComponentBehavior behavior2 in m_behaviors)
		{
			if (behavior2 == componentBehavior)
			{
				if (!behavior2.IsActive)
				{
					behavior2.IsActive = true;
				}
			}
			else if (behavior2.IsActive)
			{
				behavior2.IsActive = false;
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		if (CommonLib.WorkType == WorkType.Client)
		{
			IsDisableBehavior = true;
		}
		foreach (ComponentBehavior item in Entity.FindComponents<ComponentBehavior>())
		{
			m_behaviors.Add(item);
		}
	}
}
