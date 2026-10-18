using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemBlockEntities : Subsystem
{
	public Dictionary<Point3, ComponentBlockEntity> m_blockEntities = new Dictionary<Point3, ComponentBlockEntity>();

	public ComponentBlockEntity GetBlockEntity(int x, int y, int z)
	{
		m_blockEntities.TryGetValue(new Point3(x, y, z), out var value);
		return value;
	}

	public void CreateBlockEntity(string EntityName, Point3 point, ComponentMiner miner)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			CreateBlockEntityLogic(EntityName, new Vector3(point.X, point.Y, point.Z), miner);
		}
	}

	public override void OnEntityAdded(Entity entity)
	{
		ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
		if (componentBlockEntity != null)
		{
			Point3 coordinates = componentBlockEntity.Coordinates;
			if (m_blockEntities.ContainsKey(coordinates))
			{
				componentBlockEntity.Project.RemoveEntity(componentBlockEntity.Entity, disposeEntity: true);
			}
			else
			{
				m_blockEntities.Add(coordinates, componentBlockEntity);
			}
		}
	}

	public override void OnEntityRemoved(Entity entity)
	{
		ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
		if (componentBlockEntity != null)
		{
			m_blockEntities.Remove(componentBlockEntity.Coordinates);
		}
	}

	public void CreateBlockEntity(string EntityName, Vector3 exactPosition, ComponentMiner miner)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			CreateBlockEntityLogic(EntityName, exactPosition, miner);
		}
	}

	public void CreateBlockEntityLogic(string EntityName, Vector3 position, ComponentMiner miner)
	{
		DatabaseObject databaseObject = Project.GameDatabase.Database.FindDatabaseObject(EntityName, Project.GameDatabase.EntityTemplateType, throwIfNotFound: true);
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		valuesDictionary.PopulateFromDatabaseObject(databaseObject);
		ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("BlockEntity");
		value.SetValue("Position", position);
		if (value.ContainsKey("Coordinates"))
		{
			value.Remove("Coordinates");
		}
		Guid value2 = Guid.Empty;
		if (miner != null && miner.ComponentPlayer != null)
		{
			value2 = miner.ComponentPlayer.PlayerGuid;
		}
		value.SetValue("Owner", value2);
		Entity entity = Project.CreateEntity(valuesDictionary);
		Project.AddEntity(entity);
	}

	public ComponentBlockEntity DetachBlockEntity(int x, int y, int z)
	{
		ComponentBlockEntity blockEntity = GetBlockEntity(x, y, z);
		if (blockEntity != null)
		{
			m_blockEntities.Remove(blockEntity.Coordinates);
		}
		return blockEntity;
	}

	public void AttachBlockEntity(ComponentBlockEntity entity, Point3 newCoordinates)
	{
		entity.Coordinates = newCoordinates;
		if (m_blockEntities.ContainsKey(newCoordinates))
		{
			ComponentBlockEntity componentBlockEntity = m_blockEntities[newCoordinates];
			Project.RemoveEntity(componentBlockEntity.Entity, disposeEntity: true);
			m_blockEntities.Remove(newCoordinates);
		}
		m_blockEntities.Add(newCoordinates, entity);
	}
}
