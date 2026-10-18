using System;
using Engine;
using TemplatesDatabase;

namespace Game.NetWork.Packages;

public class MovingBlockPackage : IPackage
{
	[Flags]
	public enum EventType
	{
		Add = 1,
		Remove = 2,
		HagTag = 4,
		Stopped = 8,
		Sync = 0x10,
		RequestFullSync = 0x20
	}

	public EventType m_type;

	public ValuesDictionary AddData;

	public string movingBlockId;

	public Point3 m_position;

	public int CollisionGroupId;

	public Vector3 CurrentPosition;

	public Quaternion CurrentRotation;

	public Vector3 CurrentVelocity;

	public byte ID => 26;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Connected;

	public MovingBlockPackage()
	{
	}

	public MovingBlockPackage(SubsystemMovingBlocks.MovingBlockSet movingSet)
	{
		m_type = EventType.Add;
		AddData = SubsystemMovingBlocks.SaveMovingItem(movingSet);
	}

	public MovingBlockPackage(IMovingBlockSet movingSet, bool stop)
	{
		m_type = EventType.Remove;
		movingBlockId = movingSet.Id;
		if (stop)
		{
			m_type |= EventType.Stopped;
		}
		if (movingSet.Tag is Point3)
		{
			m_type |= EventType.HagTag;
			m_position = (Point3)movingSet.Tag;
		}
		if (movingSet is SubsystemMovingBlocks.MovingBlockSet movingBlockSet)
		{
			CollisionGroupId = movingBlockSet.CollisionGroupId;
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_type);
		if (m_type == EventType.RequestFullSync)
		{
			return;
		}
		if (m_type.HasFlag(EventType.Add))
		{
			writer.Write(AddData);
			return;
		}
		writer.Write(movingBlockId);
		writer.Write(CollisionGroupId);
		if (m_type.HasFlag(EventType.HagTag))
		{
			writer.Write(m_position);
		}
		if (m_type.HasFlag(EventType.Sync))
		{
			writer.Write(CurrentPosition);
			writer.Write(CurrentRotation);
			writer.Write(CurrentVelocity);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_type = reader.ReadEnum<EventType>();
		if (m_type == EventType.RequestFullSync)
		{
			return;
		}
		if (m_type.HasFlag(EventType.Add))
		{
			AddData = reader.ReadValuesDictionary();
			return;
		}
		movingBlockId = reader.ReadString();
		CollisionGroupId = reader.ReadInt32();
		if (m_type.HasFlag(EventType.HagTag))
		{
			m_position = reader.ReadPoint3();
		}
		if (m_type.HasFlag(EventType.Sync))
		{
			CurrentPosition = reader.ReadVector3();
			CurrentRotation = reader.ReadQuaternion();
			CurrentVelocity = reader.ReadVector3();
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemMovingBlocks subsystemMovingBlocks = projectNet.FindSubsystem<SubsystemMovingBlocks>();
		if (isServer)
		{
			if (m_type != EventType.RequestFullSync || From == null)
			{
				return;
			}
			{
				foreach (SubsystemMovingBlocks.MovingBlockSet corner3 in subsystemMovingBlocks.m_movingBlockSets)
				{
					netNode.QueuePackage(new MovingBlockPackage(corner3)
					{
						To = From
					});
				}
				return;
			}
		}
		if (m_type.HasFlag(EventType.Add))
		{
			int count = AddData.GetValue("CollisionGroupId", 0);
			foreach (SubsystemMovingBlocks.MovingBlockSet movingBlockSet in subsystemMovingBlocks.m_movingBlockSets)
			{
				if (movingBlockSet.CollisionGroupId == count && count != 0)
				{
					return;
				}
			}
			SubsystemMovingBlocks.MovingBlockSet texture2D2 = subsystemMovingBlocks.LoadAndAddMovingItem(AddData) as SubsystemMovingBlocks.MovingBlockSet;
			if (texture2D2 != null && !subsystemMovingBlocks.m_movingBlockSets.Contains(texture2D2))
			{
				subsystemMovingBlocks.m_movingBlockSets.Add(texture2D2);
			}
			if (texture2D2 != null && texture2D2.Id == "Piston")
			{
				projectNet.FindSubsystem<SubsystemAudio>().PlaySound("Audio/Piston", 1f, 0f, texture2D2.Position, 2f, autoDelay: true);
			}
			return;
		}
		SubsystemMovingBlocks.MovingBlockSet texture2D3 = null;
		if (CollisionGroupId != 0)
		{
			foreach (SubsystemMovingBlocks.MovingBlockSet corner5 in subsystemMovingBlocks.m_movingBlockSets)
			{
				if (corner5.CollisionGroupId == CollisionGroupId)
				{
					texture2D3 = corner5;
					break;
				}
			}
		}
		if (texture2D3 == null)
		{
			object count2 = (m_type.HasFlag(EventType.HagTag) ? ((object)m_position) : null);
			texture2D3 = subsystemMovingBlocks.FindMovingBlocks(movingBlockId, count2) as SubsystemMovingBlocks.MovingBlockSet;
		}
		if (texture2D3 == null)
		{
			return;
		}
		if (m_type.HasFlag(EventType.Stopped))
		{
			subsystemMovingBlocks.DoStop(texture2D3);
		}
		else if (m_type.HasFlag(EventType.Remove))
		{
			subsystemMovingBlocks.RemoveMovingBlockSetLogic(texture2D3);
		}
		else
		{
			if (!m_type.HasFlag(EventType.Sync))
			{
				return;
			}
			if (Vector3.Distance(texture2D3.Position, CurrentPosition) > 5f)
			{
				texture2D3.Position = CurrentPosition;
				texture2D3.Rotation = CurrentRotation;
				texture2D3.CurrentVelocity = CurrentVelocity;
				if (texture2D3.NetPosition != null)
				{
					texture2D3.NetPosition = new NetPosition(CurrentPosition);
					texture2D3.NetRotation = new NetRotation(CurrentRotation);
					texture2D3.NetVelocity = new NetVelocity(CurrentVelocity);
				}
			}
			else if (texture2D3.NetPosition != null)
			{
				texture2D3.NetPosition.SetNext(CurrentPosition);
				texture2D3.NetRotation.SetNext(CurrentRotation);
				texture2D3.NetVelocity.SetNext(CurrentVelocity);
			}
			else
			{
				texture2D3.Position = Vector3.Lerp(texture2D3.Position, CurrentPosition, 0.1f);
				texture2D3.Rotation = Quaternion.Slerp(texture2D3.Rotation, CurrentRotation, 0.1f);
			}
		}
	}

	public MovingBlockPackage(SubsystemMovingBlocks.MovingBlockSet movingSet, bool isSyncDummy)
	{
		m_type = EventType.Sync;
		movingBlockId = movingSet.Id;
		CollisionGroupId = movingSet.CollisionGroupId;
		CurrentPosition = movingSet.Position;
		CurrentRotation = movingSet.Rotation;
		CurrentVelocity = movingSet.CurrentVelocity;
		if (movingSet.Tag is Point3)
		{
			m_type |= EventType.HagTag;
			m_position = (Point3)movingSet.Tag;
		}
	}

	public MovingBlockPackage(bool isRequest)
	{
		if (isRequest)
		{
			m_type = EventType.RequestFullSync;
		}
	}
}
