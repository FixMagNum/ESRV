using System.Collections.Generic;
using Engine;

namespace Game.NetWork.Packages;

public class PickablePackage : IPackage
{
	public enum PickType
	{
		Create,
		Update,
		Delete,
		RequestSync,
		SetFlyToPosition
	}

	public PickType m_type;

	public ushort m_id;

	public int m_value;

	public byte m_count;

	public Vector3 m_position;

	public Vector3 m_velocity;

	public Vector3? m_flyToPosition;

	public byte? m_getPlayer;

	public Vector3 m_positionFix;

	public float m_distance;

	public Matrix? stuckMatrix;

	public bool PlaySound;

	public List<Pickable> m_pickables = new List<Pickable>();

	public bool IsSpecialPickable;

	public Vector3 m_rotation;

	public byte ID => 0;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public PickablePackage()
	{
	}

	public PickablePackage(List<Pickable> pickables)
	{
		m_type = PickType.Update;
		m_pickables.AddRange(pickables);
	}

	public PickablePackage(Pickable pickable, PickType pickType)
	{
		m_id = pickable.Id;
		m_value = pickable.Value;
		m_count = (byte)pickable.Count;
		m_position = pickable.Position;
		m_velocity = pickable.Velocity;
		m_getPlayer = pickable.GetPickPlayer;
		stuckMatrix = pickable.StuckMatrix;
		m_flyToPosition = pickable.FlyToPosition;
		m_rotation = pickable.Rotation;
		m_type = pickType;
		PlaySound = pickable.PlaySound;
		IsSpecialPickable = pickable.IsSpecialPickable;
	}

	public PickablePackage(ushort pickId)
	{
		m_id = pickId;
		m_type = PickType.Delete;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		writer.Write(m_id);
		switch (m_type)
		{
		case PickType.Create:
			writer.Write(m_count);
			writer.Write(m_value);
			writer.Write(m_position);
			writer.Write(m_velocity);
			writer.Write(stuckMatrix);
			writer.Write(m_rotation);
			writer.Write(IsSpecialPickable);
			break;
		case PickType.Update:
		{
			writer.Write(m_pickables.Count);
			for (int i = 0; i < m_pickables.Count; i++)
			{
				Pickable pickable = m_pickables[i];
				writer.Write(pickable.Id);
				writer.Write(pickable.Position);
				writer.Write(pickable.Velocity);
			}
			break;
		}
		case PickType.Delete:
			writer.Write(PlaySound);
			break;
		case PickType.RequestSync:
			break;
		case PickType.SetFlyToPosition:
			writer.Write(m_flyToPosition.Value);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		try
		{
			m_type = reader.ReadEnum<PickType>();
			m_id = reader.ReadUInt16();
			switch (m_type)
			{
			case PickType.Create:
				m_count = reader.ReadByte();
				m_value = reader.ReadInt32();
				m_position = reader.ReadVector3();
				m_velocity = reader.ReadVector3();
				stuckMatrix = reader.ReadMatrixNullable();
				m_rotation = reader.ReadVector3();
				IsSpecialPickable = reader.ReadBoolean();
				break;
			case PickType.Update:
			{
				int num = reader.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					Pickable pickable = new Pickable();
					pickable.Id = reader.ReadUInt16();
					pickable.Position = reader.ReadVector3();
					pickable.Velocity = reader.ReadVector3();
					m_pickables.Add(pickable);
				}
				break;
			}
			case PickType.Delete:
				PlaySound = reader.ReadBoolean();
				break;
			case PickType.SetFlyToPosition:
				m_flyToPosition = reader.ReadVector3();
				break;
			case PickType.RequestSync:
				break;
			}
		}
		catch
		{
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemPickables subsystemPickable = projectNet.FindSubsystem<SubsystemPickables>();
		// Only the server creates, updates and deletes pickables (drops, loot, pickups are all server-side).
		// A client sending one of these is refused. Clients may still ask for a missing pickable (RequestSync).
		if (isServer && (m_type == PickType.Create || m_type == PickType.Update || m_type == PickType.Delete))
		{
			ServerSecurity.Warn(From, "pickable packet from a client", "type=" + m_type + ", id=" + m_id);
			return;
		}
		switch (m_type)
		{
		case PickType.Create:
		{
			bool flag = false;
			Pickable pickable = null;
			foreach (Pickable pickable2 in subsystemPickable.Pickables)
			{
				if (pickable2.Id == m_id)
				{
					pickable = pickable2;
					flag = true;
					break;
				}
			}
			if (flag)
			{
				pickable.Value = m_value;
				pickable.Count = m_count;
				pickable.Velocity = m_velocity;
				pickable.StuckMatrix = stuckMatrix;
				pickable.Rotation = m_rotation;
				pickable.IsSpecialPickable = IsSpecialPickable;
			}
			else
			{
				subsystemPickable.CreatePickable(m_id, m_value, m_count, m_position, m_velocity, stuckMatrix).Rotation = m_rotation;
			}
			break;
		}
		case PickType.Update:
		{
			foreach (Pickable c2 in m_pickables)
			{
				subsystemPickable.PickableAction(c2.Id, (Pickable pick) =>
				{
					pick.Position = c2.Position;
					pick.Velocity = c2.Velocity;
				});
			}
			List<Pickable> list = new List<Pickable>();
			foreach (Pickable c3 in subsystemPickable.Pickables)
			{
				if (m_pickables.Find((Pickable x) => x.Id == c3.Id) == null)
				{
					list.Add(c3);
				}
			}
			foreach (Pickable item in list)
			{
				subsystemPickable.m_pickables.Remove(item);
			}
			list.Clear();
			break;
		}
		case PickType.Delete:
			subsystemPickable.PickableAction(m_id, (Pickable pick) =>
			{
				if (PlaySound)
				{
					subsystemPickable.PlayPickableCollectedSound(pick);
				}
				subsystemPickable.m_pickables.Remove(pick);
			}, requestSync: false);
			break;
		case PickType.RequestSync:
			if (!subsystemPickable.PickableAction(m_id, (Pickable pick) =>
			{
				netNode.QueuePackage(new PickablePackage(pick, PickType.Create));
			}))
			{
				netNode.QueuePackage(new PickablePackage(m_id));
			}
			break;
		case PickType.SetFlyToPosition:
			subsystemPickable.PickableAction(m_id, (Pickable pick) =>
			{
				pick.FlyToPosition = m_flyToPosition;
			});
			break;
		}
	}
}
