using System;
using System.Collections.Generic;
using Engine;

namespace Game.NetWork.Packages;

public class SubsystemBodyPackage : IPackage
{
	public enum EventType
	{
		BodyUpdate,
		ApplyImpulse,
		HandleAxisCollision
	}

	private struct BodyItem
	{
		public ushort CreatureId;

		public Vector3 Position;

		public Vector3 Velocity;

		public Quaternion Rotation;

		public Vector2 LookAngles;

		public Vector3? FlyOrder;

		public ChangeFlag changeFlag;
	}

	public enum ChangeFlag : byte
	{
		None = 0,
		LookAnglesChange = 1,
		FlyOrderChange = 2,
		PositionChange = 4,
		RotationChange = 8,
		VelocityChange = 0x10
	}

	public ushort CreatureId;

	public ushort TargetCreatureId;

	public Vector3 m_Impulse;

	public EventType m_eventType;

	public ComponentModel CreatureModel;

	private List<BodyItem> m_bodyList = new List<BodyItem>();

	public byte ID => 1;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public SubsystemBodyPackage()
	{
	}

	public SubsystemBodyPackage(List<ComponentBody> bodies)
	{
		m_eventType = EventType.BodyUpdate;
		foreach (ComponentBody netDataWriter in bodies)
		{
			AddItem(netDataWriter);
		}
	}

	public void AddItem(ComponentBody body)
	{
		BodyItem item = new BodyItem
		{
			CreatureId = body.Entity.EntityId
		};
		if (body.SendPosition.HasValue)
		{
			item.changeFlag |= ChangeFlag.PositionChange;
			item.Position = body.SendPosition.Value;
		}
		if (body.SendRotation.HasValue)
		{
			item.changeFlag |= ChangeFlag.RotationChange;
			item.Rotation = body.SendRotation.Value;
		}
		if (body.SendVelocity.HasValue)
		{
			item.changeFlag |= ChangeFlag.VelocityChange;
			item.Velocity = body.SendVelocity.Value;
		}
		if (body.Locomotion != null)
		{
			if (body.Locomotion.SendLookAngles.HasValue)
			{
				item.changeFlag |= ChangeFlag.LookAnglesChange;
				item.LookAngles = body.Locomotion.SendLookAngles.Value;
			}
			if (body.Locomotion.FlyOrderChange)
			{
				item.changeFlag |= ChangeFlag.FlyOrderChange;
				item.FlyOrder = body.Locomotion.LastFlyOrder;
			}
		}
		m_bodyList.Add(item);
	}

	public SubsystemBodyPackage(ComponentBody body, Vector3 vector)
	{
		CreatureId = body.Entity.EntityId;
		m_eventType = EventType.ApplyImpulse;
		m_Impulse = vector;
	}

	public SubsystemBodyPackage(ComponentBody from, ComponentBody target, Vector3 velocity)
	{
		m_eventType = EventType.HandleAxisCollision;
		CreatureId = from.Entity.EntityId;
		TargetCreatureId = target.Entity.EntityId;
		m_Impulse = velocity;
	}

	private BodyItem ReadItem(PackageStreamReader reader)
	{
		BodyItem result = new BodyItem
		{
			changeFlag = reader.ReadEnum<ChangeFlag>(),
			CreatureId = reader.ReadUInt16()
		};
		if (result.changeFlag.HasFlag(ChangeFlag.PositionChange))
		{
			result.Position = reader.ReadVector3();
		}
		if (result.changeFlag.HasFlag(ChangeFlag.RotationChange))
		{
			result.Rotation = reader.ReadQuaternion();
		}
		if (result.changeFlag.HasFlag(ChangeFlag.VelocityChange))
		{
			result.Velocity = reader.ReadVector3();
		}
		if (result.changeFlag.HasFlag(ChangeFlag.LookAnglesChange))
		{
			result.LookAngles = reader.ReadVector2();
		}
		if (result.changeFlag.HasFlag(ChangeFlag.FlyOrderChange))
		{
			result.FlyOrder = reader.ReadVector3Nullable();
		}
		return result;
	}

	private void WriteItem(PackageStreamWriter writer, BodyItem bodyItem)
	{
		writer.WriteEnum(bodyItem.changeFlag);
		writer.Write(bodyItem.CreatureId);
		if (bodyItem.changeFlag.HasFlag(ChangeFlag.PositionChange))
		{
			writer.Write(bodyItem.Position);
		}
		if (bodyItem.changeFlag.HasFlag(ChangeFlag.RotationChange))
		{
			writer.Write(bodyItem.Rotation);
		}
		if (bodyItem.changeFlag.HasFlag(ChangeFlag.VelocityChange))
		{
			writer.Write(bodyItem.Velocity);
		}
		if (bodyItem.changeFlag.HasFlag(ChangeFlag.LookAnglesChange))
		{
			writer.Write(bodyItem.LookAngles);
		}
		if (bodyItem.changeFlag.HasFlag(ChangeFlag.FlyOrderChange))
		{
			writer.Write(bodyItem.FlyOrder);
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_eventType = reader.ReadEnum<EventType>();
		switch (m_eventType)
		{
		case EventType.BodyUpdate:
		{
			ushort peer = reader.ReadUInt16();
			for (ushort num = 0; num < peer; num++)
			{
				try
				{
					m_bodyList.Add(ReadItem(reader));
				}
				catch (Exception ex)
				{
					Log.Warning("[SubsystemBodyPackage] Error reading body item: " + ex.Message);
					break;
				}
			}
			break;
		}
		case EventType.ApplyImpulse:
			CreatureId = reader.ReadUInt16();
			m_Impulse = reader.ReadVector3();
			break;
		case EventType.HandleAxisCollision:
			CreatureId = reader.ReadUInt16();
			TargetCreatureId = reader.ReadUInt16();
			m_Impulse = reader.ReadVector3();
			break;
		}
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_eventType);
		switch (m_eventType)
		{
		case EventType.BodyUpdate:
			writer.Write((ushort)m_bodyList.Count);
			{
				foreach (BodyItem body in m_bodyList)
				{
					WriteItem(writer, body);
				}
				break;
			}
		case EventType.HandleAxisCollision:
			writer.Write(CreatureId);
			writer.Write(TargetCreatureId);
			writer.Write(m_Impulse);
			break;
		case EventType.ApplyImpulse:
			writer.Write(CreatureId);
			writer.Write(m_Impulse);
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		switch (m_eventType)
		{
		case EventType.BodyUpdate:
		{
			foreach (BodyItem item in m_bodyList)
			{
				projectNet.FindSubsystem<SubsystemBodies>().FindBodyByCreatureID(item.CreatureId, (ComponentBody body) =>
				{
					if (item.changeFlag.HasFlag(ChangeFlag.PositionChange))
					{
						body.netPosition.SetNext(item.Position);
					}
					if (item.changeFlag.HasFlag(ChangeFlag.RotationChange))
					{
						body.netRotation.SetNext(item.Rotation);
					}
					if (item.changeFlag.HasFlag(ChangeFlag.VelocityChange))
					{
						body.netVelocity.SetNext(item.Velocity);
					}
					if (body.Locomotion != null)
					{
						if (item.changeFlag.HasFlag(ChangeFlag.LookAnglesChange))
						{
							body.Locomotion.NetLookAngles.SetNext(item.LookAngles);
						}
						if (item.changeFlag.HasFlag(ChangeFlag.FlyOrderChange))
						{
							body.Locomotion.LastFlyOrder = item.FlyOrder;
						}
					}
				});
			}
			break;
		}
		case EventType.HandleAxisCollision:
			projectNet.FindSubsystem<SubsystemBodies>().FindBodyByCreatureID(CreatureId, (ComponentBody from) =>
			{
				projectNet.FindSubsystem<SubsystemBodies>().FindBodyByCreatureID(TargetCreatureId, (ComponentBody target) =>
				{
					target.Velocity = m_Impulse;
					target.netVelocity.SetNext(m_Impulse);
					from.CollidedWithBody?.Invoke(target);
					target.CollidedWithBody?.Invoke(from);
				});
			});
			break;
		case EventType.ApplyImpulse:
			projectNet.FindSubsystem<SubsystemBodies>().FindBodyByCreatureID(CreatureId, (ComponentBody body) =>
			{
				body.ApplyImpulseNet(m_Impulse);
			});
			break;
		}
	}
}
