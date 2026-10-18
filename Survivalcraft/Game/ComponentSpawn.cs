using System;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentSpawn : Component, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public float _SpawnDuration;

	public ComponentBody Body;

	public ComponentFrame ComponentFrame { get; set; }

	public ComponentCreature ComponentCreature { get; set; }

	public bool AutoDespawn { get; set; }

	public bool IsDespawning => DespawnTime.HasValue;

	public double SpawnTime { get; set; }

	public double? DespawnTime { get; set; }

	public float SpawnDuration { get; set; }

	public float DespawnDuration { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<ComponentSpawn> Despawned;

	public void Despawn()
	{
		if (!DespawnTime.HasValue)
		{
			DespawnTime = m_subsystemGameInfo.TotalElapsedGameTime;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		Body = Entity.FindComponent<ComponentBody>();
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		ComponentFrame = Entity.FindComponent<ComponentFrame>(throwOnError: true);
		ComponentCreature = Entity.FindComponent<ComponentCreature>();
		AutoDespawn = valuesDictionary.GetValue<bool>("AutoDespawn");
		double worldInfo = valuesDictionary.GetValue<double>("SpawnTime");
		double node2 = valuesDictionary.GetValue<double>("DespawnTime");
		if (CommonLib.WorkType == WorkType.Client)
		{
			SpawnDuration = 0f;
		}
		else
		{
			SpawnDuration = 2f;
		}
		DespawnDuration = 2f;
		SpawnTime = ((worldInfo < 0.0) ? m_subsystemGameInfo.TotalElapsedGameTime : worldInfo);
		DespawnTime = ((node2 >= 0.0) ? new double?(node2) : ((double?)null));
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("SpawnTime", SpawnTime);
		if (DespawnTime.HasValue)
		{
			valuesDictionary.SetValue("DespawnTime", DespawnTime.Value);
		}
	}

	public void Update(float dt)
	{
		if (DespawnTime.HasValue && m_subsystemGameInfo.TotalElapsedGameTime >= DespawnTime.Value + (double)DespawnDuration)
		{
			Project.RemoveEntity(Entity, disposeEntity: true);
			Despawned?.Invoke(this);
		}
	}
}
