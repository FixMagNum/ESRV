using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBlockEntity : Component
{
	private SubsystemPlayers SubPlayers;

	public Point3 Coordinates
	{
		get
		{
			return new Point3((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y), (int)Math.Floor(Position.Z));
		}
		set
		{
			Position = new Vector3(value.X, value.Y, value.Z);
		}
	}

	public Guid Owner { get; set; }

	public PlayerData OwnPlayerData
	{
		get
		{
			if (SubPlayers == null)
			{
				return null;
			}
			PlayerData playerData = SubPlayers.PlayersData.Find((PlayerData x) => x.PlayerGUID == Owner);
			if (playerData == null)
			{
				playerData = SubPlayers.m_offlinePlayers.Find((PlayerData x) => x.PlayerGUID == Owner);
			}
			return playerData;
		}
	}

	public Vector3 Position { get; set; }

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		SubPlayers = Project.FindSubsystem<SubsystemPlayers>();
		if (valuesDictionary.ContainsKey("Position"))
		{
			Position = valuesDictionary.GetValue<Vector3>("Position");
		}
		else if (valuesDictionary.ContainsKey("Coordinates"))
		{
			Point3 value = valuesDictionary.GetValue<Point3>("Coordinates");
			Position = new Vector3(value.X, value.Y, value.Z);
		}
		else
		{
			Position = Vector3.Zero;
		}
		Owner = valuesDictionary.GetValue("Owner", Owner);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Position", Position);
		valuesDictionary.SetValue("Owner", Owner);
	}
}
