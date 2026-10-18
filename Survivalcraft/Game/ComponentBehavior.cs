using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public abstract class ComponentBehavior : Component
{
	public bool m_active;

	public StateMachine m_stateMachine = new StateMachine();

	public abstract float ImportanceLevel { get; }

	public virtual bool IsActive
	{
		get
		{
			return m_active;
		}
		set
		{
			m_active = value;
		}
	}

	public virtual string DebugInfo => string.Empty;

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
	}
}
