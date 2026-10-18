using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class FishingFloatPackage : IPackage
{
	public enum FishingFloatAction : byte
	{
		Cast,
		Reel,
		Attach,
		Detach,
		Remove,
		Sync
	}

	public FishingFloatAction m_action;

	public Vector3 m_position;

	public Vector3 m_velocity;

	public Vector3 m_angularVelocity;

	public ushort m_ownerId;

	public int m_bobberValue;

	public ushort m_bodyId;

	public Vector3 m_bodyOffset;

	public byte ID => 31;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public FishingFloatPackage()
	{
	}

	public FishingFloatPackage(FishingFloatAction action, FishingFloat f)
	{
		m_action = action;
		if (f != null)
		{
			m_position = f.Position;
			m_velocity = f.Velocity;
			m_angularVelocity = f.AngularVelocity;
			m_ownerId = (ushort)((f.Owner != null) ? f.Owner.Entity.EntityId : 0);
			m_bobberValue = f.BobberValue;
			m_bodyId = (ushort)((f.StuckBody != null) ? f.StuckBody.Entity.EntityId : 0);
			m_bodyOffset = f.BodyOffset;
		}
	}

	public FishingFloatPackage(FishingFloatAction action, ComponentCreature owner)
	{
		m_action = action;
		m_ownerId = owner?.Entity.EntityId ?? 0;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(m_action);
		writer.Write(m_ownerId);
		writer.Write(m_bobberValue);
		writer.Write(m_position);
		writer.Write(m_velocity);
		writer.Write(m_angularVelocity);
		writer.Write(m_bodyId);
		writer.Write(m_bodyOffset);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_action = reader.ReadEnum<FishingFloatAction>();
		m_ownerId = reader.ReadUInt16();
		m_bobberValue = reader.ReadInt32();
		m_position = reader.ReadVector3();
		m_velocity = reader.ReadVector3();
		m_angularVelocity = reader.ReadVector3();
		m_bodyId = reader.ReadUInt16();
		m_bodyOffset = reader.ReadVector3();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemFishingFloats subsystemFishingFloats = projectNet.FindSubsystem<SubsystemFishingFloats>();
		ComponentCreature owner = null;
		projectNet.FindEntityById(m_ownerId, (Entity e) =>
		{
			owner = e.FindComponent<ComponentCreature>();
		});
		switch (m_action)
		{
		case FishingFloatAction.Cast:
			subsystemFishingFloats.CastFloatNet(m_position, m_velocity, m_angularVelocity, owner, m_bobberValue);
			break;
		case FishingFloatAction.Reel:
			subsystemFishingFloats.ReelFloatNet(owner);
			break;
		case FishingFloatAction.Attach:
		{
			FishingFloat fishingFloat2 = subsystemFishingFloats.FindFloat(owner);
			if (fishingFloat2 != null)
			{
				ComponentBody body = null;
				projectNet.FindEntityById(m_bodyId, (Entity e) =>
				{
					body = e.FindComponent<ComponentBody>();
				});
				if (body != null)
				{
					fishingFloat2.StuckBody = body;
					fishingFloat2.BodyOffset = m_bodyOffset;
					fishingFloat2.EffectTimer = subsystemFishingFloats.StuckEffectPeriod;
					subsystemFishingFloats.InitMiniGame(fishingFloat2);
				}
			}
			break;
		}
		case FishingFloatAction.Detach:
		{
			FishingFloat fishingFloat4 = subsystemFishingFloats.FindFloat(owner);
			if (fishingFloat4 != null)
			{
				fishingFloat4.StuckBody = null;
				subsystemFishingFloats.ClearMiniGame(fishingFloat4);
				subsystemFishingFloats.ResetPlayerSightColor(owner);
			}
			break;
		}
		case FishingFloatAction.Remove:
		{
			FishingFloat fishingFloat3 = subsystemFishingFloats.FindFloat(owner);
			if (fishingFloat3 != null)
			{
				subsystemFishingFloats.RemoveFloatClient(fishingFloat3);
			}
			break;
		}
		case FishingFloatAction.Sync:
		{
			FishingFloat fishingFloat = subsystemFishingFloats.FindFloat(owner);
			if (fishingFloat != null)
			{
				fishingFloat.BobberValue = m_bobberValue;
			}
			break;
		}
		}
		if (isServer)
		{
			netNode.QueuePackage(this);
		}
	}
}
