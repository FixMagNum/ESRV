using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game.NetWork.Packages;

public class ComponentPlayerPackage : IPackage
{
	public enum PlayerAction
	{
		BodyUpdate,
		InteractEvent,
		AimEvent,
		DigEvent,
		Hit,
		CreativeFlyChange,
		IntoPlaying,
		Restart,
		AddExperience,
		Drop,
		DragDrop,
		SyncStat,
		PositionSet,
		PickBlockType
	}

	public enum ChangFlag : byte
	{
		None = 0,
		ParentBodyChange = 1,
		LookAnglesChange = 2,
		LadderChange = 4,
		PositionChange = 8,
		RotationChange = 0x10,
		VelocityChange = 0x20,
		ChildLookAnglesChange = 0x40,
		SneakChange = 0x80
	}

	public bool NeedHandleMainPlayer;

	public PlayerData m_playerData;

	public PlayerAction m_type;

	public int m_ActiveSlot;

	public Vector3 Position;

	public Vector3 Velocity;

	public Quaternion Rotation;

	public Vector2 LookOrder;

	public Vector2 LookAngles;

	public Vector2 ChildLookAngles;

	public int m_inventory_id;

	public ChangFlag changFlag;

	public byte m_fromPlayerId;

	public ushort bodyId;

	public Vector3 m_hitPosition;

	public Vector3 m_hitDirection;

	public int m_count;

	public ComponentPlayer.InteractEvent m_interactEvent;

	public ComponentPlayer.AimEvent m_aimEvent;

	public ComponentPlayer.DigEvent m_digEvent;

	public TerrainRaycastResult? m_netInteractRaycast;

	public Ray3 m_netInteractRay;

	public Ray3? m_netAimRay;

	public TerrainRaycastResult? m_netDigRaycast;

	public Ray3? m_netDigRay;

	public int m_ladderValue;

	public float m_level;

	public bool m_isCreativeFly;

	public bool m_playSound;

	public ValuesDictionary m_stat;

	public bool m_sneaking;

	public static readonly Vector3 VanishParkedPosition = new Vector3(100000f, 100f, 100000f);

	public byte ID => 3;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public int? PickBlockTypeValue { get; set; }

	public ComponentPlayerPackage()
	{
		NeedHandleMainPlayer = true;
	}

	public ComponentPlayerPackage(ComponentPlayer player, PlayerAction type)
	{
		NeedHandleMainPlayer = true;
		m_type = type;
		m_playerData = player.PlayerData;
		switch (type)
		{
		case PlayerAction.BodyUpdate:
		{
			ComponentBody componentBody = player.ComponentBody.ParentBody;
			if (componentBody != null)
			{
				changFlag |= ChangFlag.ParentBodyChange;
				if (componentBody.Locomotion != null && componentBody.Locomotion.SendLookAngles.HasValue)
				{
					changFlag |= ChangFlag.LookAnglesChange;
					LookAngles = componentBody.Locomotion.SendLookAngles.Value;
					componentBody.Locomotion.SendLookAngles = null;
				}
				if (player.ComponentLocomotion.SendLookAngles.HasValue)
				{
					changFlag |= ChangFlag.ChildLookAnglesChange;
					ChildLookAngles = player.ComponentLocomotion.SendLookAngles.Value;
					player.ComponentLocomotion.SendLookAngles = null;
				}
			}
			else
			{
				componentBody = player.ComponentBody;
				if (player.ComponentLocomotion.SendLookAngles.HasValue)
				{
					changFlag |= ChangFlag.LookAnglesChange;
					LookAngles = player.ComponentLocomotion.SendLookAngles.Value;
					player.ComponentLocomotion.SendLookAngles = null;
				}
				if (player.ComponentLocomotion.LadderValue.HasValue)
				{
					changFlag |= ChangFlag.LadderChange;
					m_ladderValue = player.ComponentLocomotion.LadderValue.Value;
				}
				if (player.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.GameMode == GameMode.Embrace && componentBody.HumanModel != null)
				{
					changFlag |= ChangFlag.SneakChange;
					m_sneaking = componentBody.EmbraceAirCrouchLatched;
				}
				else if (componentBody.m_isSneaking != (componentBody.TargetCrouchFactor == 1f))
				{
					changFlag |= ChangFlag.SneakChange;
					m_sneaking = componentBody.m_isSneaking;
				}
			}
			if (componentBody.SendPosition.HasValue)
			{
				Position = componentBody.SendPosition.Value;
				changFlag |= ChangFlag.PositionChange;
				componentBody.SendPosition = null;
			}
			if (componentBody.SendRotation.HasValue)
			{
				changFlag |= ChangFlag.RotationChange;
				Rotation = componentBody.SendRotation.Value;
				componentBody.SendRotation = null;
			}
			if (componentBody.SendVelocity.HasValue)
			{
				changFlag |= ChangFlag.VelocityChange;
				Velocity = componentBody.SendVelocity.Value;
				componentBody.SendVelocity = null;
			}
			break;
		}
		case PlayerAction.InteractEvent:
			m_interactEvent = player.interactEvent.interactEvent;
			m_netInteractRay = player.interactEvent.NetInteractRay;
			m_netInteractRaycast = player.interactEvent.NetPlaceRaycast;
			break;
		case PlayerAction.AimEvent:
			m_aimEvent = player.aimEvent.aimEvent;
			m_netAimRay = player.aimEvent.NetAim;
			break;
		case PlayerAction.DigEvent:
			m_digEvent = player.digEvent.digEvent;
			m_netDigRay = player.digEvent.NetDigRay;
			m_netDigRaycast = player.digEvent.NetDigRaycast;
			break;
		case PlayerAction.CreativeFlyChange:
			m_isCreativeFly = player.ComponentLocomotion.IsCreativeFlyEnabled;
			break;
		case PlayerAction.SyncStat:
			m_stat = new ValuesDictionary();
			player.PlayerStats.Save(m_stat);
			break;
		case PlayerAction.PositionSet:
			Position = player.ComponentBody.Position;
			Velocity = player.ComponentBody.Velocity;
			break;
		case PlayerAction.PickBlockType:
			break;
		case PlayerAction.Hit:
		case PlayerAction.IntoPlaying:
		case PlayerAction.Restart:
		case PlayerAction.AddExperience:
		case PlayerAction.Drop:
		case PlayerAction.DragDrop:
			break;
		}
	}

	public ComponentPlayerPackage(PlayerData playerData, PlayerAction type)
	{
		NeedHandleMainPlayer = true;
		m_type = type;
		m_playerData = playerData;
	}

	public ComponentPlayerPackage(PlayerData playerData, int count, bool playSound, float level)
	{
		NeedHandleMainPlayer = true;
		m_playerData = playerData;
		m_type = PlayerAction.AddExperience;
		m_count = count;
		m_playSound = playSound;
		m_level = level;
	}

	public ComponentPlayerPackage(PlayerData playerData, int inventory_id, int slotIndex, Vector3 postion, int count)
	{
		NeedHandleMainPlayer = true;
		m_playerData = playerData;
		m_type = PlayerAction.DragDrop;
		m_inventory_id = inventory_id;
		m_ActiveSlot = slotIndex;
		m_hitPosition = postion;
		m_count = count;
	}

	public ComponentPlayerPackage(ComponentPlayer player, ComponentBody body, Vector3 hitPosition, Vector3 hitDirection)
	{
		NeedHandleMainPlayer = true;
		m_playerData = player.PlayerData;
		bodyId = body.Entity.EntityId;
		m_hitPosition = hitPosition;
		m_hitDirection = hitDirection;
		m_type = PlayerAction.Hit;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		if (m_playerData.ClientId.HasValue)
		{
			writer.Write(m_playerData.ClientId.Value);
		}
		else
		{
			writer.Write(0);
		}
		writer.WriteEnum(m_type);
		switch (m_type)
		{
		case PlayerAction.BodyUpdate:
			writer.WriteEnum(changFlag);
			if (changFlag.HasFlag(ChangFlag.ParentBodyChange))
			{
				if (changFlag.HasFlag(ChangFlag.LookAnglesChange))
				{
					writer.Write(LookAngles);
				}
				if (changFlag.HasFlag(ChangFlag.ChildLookAnglesChange))
				{
					writer.Write(ChildLookAngles);
				}
			}
			else if (changFlag.HasFlag(ChangFlag.LookAnglesChange))
			{
				writer.Write(LookAngles);
			}
			if (changFlag.HasFlag(ChangFlag.PositionChange))
			{
				writer.Write(Position);
			}
			if (changFlag.HasFlag(ChangFlag.RotationChange))
			{
				writer.Write(Rotation);
			}
			if (changFlag.HasFlag(ChangFlag.VelocityChange))
			{
				writer.Write(Velocity);
			}
			if (changFlag.HasFlag(ChangFlag.LadderChange))
			{
				writer.Write(m_ladderValue);
			}
			if (changFlag.HasFlag(ChangFlag.SneakChange))
			{
				writer.Write(m_sneaking);
			}
			break;
		case PlayerAction.InteractEvent:
			writer.WriteEnum(m_interactEvent);
			writer.Write(m_netInteractRay);
			writer.Write(m_netInteractRaycast);
			break;
		case PlayerAction.AimEvent:
			writer.WriteEnum(m_aimEvent);
			writer.Write(m_netAimRay);
			break;
		case PlayerAction.DigEvent:
			writer.WriteEnum(m_digEvent);
			writer.Write(m_netDigRay);
			writer.Write(m_netDigRaycast);
			break;
		case PlayerAction.Hit:
			writer.Write(bodyId);
			writer.Write(m_hitPosition);
			writer.Write(m_hitDirection);
			break;
		case PlayerAction.CreativeFlyChange:
			writer.Write(m_isCreativeFly);
			break;
		case PlayerAction.AddExperience:
			writer.Write(m_count);
			writer.Write(m_playSound);
			writer.Write(m_level);
			break;
		case PlayerAction.DragDrop:
			writer.Write(m_inventory_id);
			writer.Write(m_ActiveSlot);
			writer.Write(m_hitPosition);
			writer.Write(m_count);
			break;
		case PlayerAction.SyncStat:
			writer.Write(m_stat);
			break;
		case PlayerAction.PositionSet:
			writer.Write(Position);
			writer.Write(Velocity);
			break;
		case PlayerAction.PickBlockType:
			writer.Write(PickBlockTypeValue.Value);
			break;
		case PlayerAction.IntoPlaying:
		case PlayerAction.Restart:
		case PlayerAction.Drop:
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_fromPlayerId = reader.ReadByte();
		m_type = reader.ReadEnum<PlayerAction>();
		switch (m_type)
		{
		case PlayerAction.BodyUpdate:
			changFlag = reader.ReadEnum<ChangFlag>();
			if (changFlag.HasFlag(ChangFlag.ParentBodyChange))
			{
				if (changFlag.HasFlag(ChangFlag.LookAnglesChange))
				{
					LookAngles = reader.ReadVector2();
				}
				if (changFlag.HasFlag(ChangFlag.ChildLookAnglesChange))
				{
					ChildLookAngles = reader.ReadVector2();
				}
			}
			else if (changFlag.HasFlag(ChangFlag.LookAnglesChange))
			{
				LookAngles = reader.ReadVector2();
			}
			if (changFlag.HasFlag(ChangFlag.PositionChange))
			{
				Position = reader.ReadVector3();
			}
			if (changFlag.HasFlag(ChangFlag.RotationChange))
			{
				Rotation = reader.ReadQuaternion();
			}
			if (changFlag.HasFlag(ChangFlag.VelocityChange))
			{
				Velocity = reader.ReadVector3();
			}
			if (changFlag.HasFlag(ChangFlag.LadderChange))
			{
				m_ladderValue = reader.ReadInt32();
			}
			if (changFlag.HasFlag(ChangFlag.SneakChange))
			{
				m_sneaking = reader.ReadBoolean();
			}
			break;
		case PlayerAction.InteractEvent:
			m_interactEvent = reader.ReadEnum<ComponentPlayer.InteractEvent>();
			m_netInteractRay = reader.ReadRay3();
			m_netInteractRaycast = reader.ReadTerrainRaycastResultNullable();
			break;
		case PlayerAction.AimEvent:
			m_aimEvent = reader.ReadEnum<ComponentPlayer.AimEvent>();
			m_netAimRay = reader.ReadRay3Nullable();
			break;
		case PlayerAction.DigEvent:
			m_digEvent = reader.ReadEnum<ComponentPlayer.DigEvent>();
			m_netDigRay = reader.ReadRay3Nullable();
			m_netDigRaycast = reader.ReadTerrainRaycastResultNullable();
			break;
		case PlayerAction.Hit:
			bodyId = reader.ReadUInt16();
			m_hitPosition = reader.ReadVector3();
			m_hitDirection = reader.ReadVector3();
			break;
		case PlayerAction.CreativeFlyChange:
			m_isCreativeFly = reader.ReadBoolean();
			break;
		case PlayerAction.AddExperience:
			m_count = reader.ReadInt32();
			m_playSound = reader.ReadBoolean();
			m_level = reader.ReadSingle();
			break;
		case PlayerAction.DragDrop:
			m_inventory_id = reader.ReadInt32();
			m_ActiveSlot = reader.ReadInt32();
			m_hitPosition = reader.ReadVector3();
			m_count = reader.ReadInt32();
			break;
		case PlayerAction.SyncStat:
			m_stat = reader.ReadValuesDictionary();
			break;
		case PlayerAction.PositionSet:
			Position = reader.ReadVector3();
			Velocity = reader.ReadVector3();
			break;
		case PlayerAction.PickBlockType:
			PickBlockTypeValue = reader.ReadInt32();
			break;
		case PlayerAction.IntoPlaying:
		case PlayerAction.Restart:
		case PlayerAction.Drop:
			break;
		}
	}

	public void PlayerEvent(Action<ComponentPlayer> action)
	{
		if (m_playerData != null && m_playerData.ComponentPlayer != null)
		{
			action?.Invoke(m_playerData.ComponentPlayer);
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (isServer)
		{
			if (From == null)
			{
				return;
			}
			if (m_fromPlayerId != 0 && m_fromPlayerId != From.ID)
			{
				ServerSecurity.Warn(From, "player action names another player", "claimed=" + m_fromPlayerId + ", type=" + m_type);
			}
			m_playerData = From.PlayerData;
			if (m_playerData == null)
			{
				return;
			}
		}
		else
		{
			m_playerData = projectNet.FindSubsystem<SubsystemPlayers>().FindPlayerData((PlayerData playerData) => playerData.ClientId.HasValue && playerData.ClientId.Value == m_fromPlayerId);
		}
		if (!NeedHandleMainPlayer && m_playerData != null && m_playerData.IsMainPlayer)
		{
			return;
		}
		switch (m_type)
		{
		case PlayerAction.BodyUpdate:
			PlayerEvent((ComponentPlayer player) =>
			{
				ComponentBody componentBody;
				if (changFlag.HasFlag(ChangFlag.ParentBodyChange))
				{
					if (player.ComponentBody.ParentBody != null)
					{
						componentBody = player.ComponentBody.ParentBody;
						if (changFlag.HasFlag(ChangFlag.LookAnglesChange))
						{
							componentBody.Locomotion.NetLookAngles.SetNext(LookAngles);
						}
						if (changFlag.HasFlag(ChangFlag.ChildLookAnglesChange))
						{
							player.ComponentBody.Locomotion.NetLookAngles.SetNext(ChildLookAngles);
						}
					}
					else
					{
						componentBody = player.ComponentBody;
					}
				}
				else
				{
					componentBody = player.ComponentBody;
					if (changFlag.HasFlag(ChangFlag.LookAnglesChange))
					{
						player.ComponentLocomotion.NetLookAngles.SetNext(LookAngles);
					}
				}
				if (changFlag.HasFlag(ChangFlag.VelocityChange))
				{
					componentBody.netVelocity.SetNext(Velocity);
				}
				if (componentBody.Locomotion != null)
				{
					if (changFlag.HasFlag(ChangFlag.LadderChange))
					{
						componentBody.Locomotion.LadderValue = m_ladderValue;
					}
					else
					{
						componentBody.Locomotion.LadderValue = null;
					}
				}
				if (changFlag.HasFlag(ChangFlag.PositionChange))
				{
					componentBody.netPosition.SetNext(Position);
				}
				if (changFlag.HasFlag(ChangFlag.RotationChange))
				{
					componentBody.netRotation.SetNext(Rotation);
				}
				if (changFlag.HasFlag(ChangFlag.SneakChange) && componentBody.m_isSneaking != m_sneaking)
				{
					componentBody.m_isSneaking = m_sneaking;
					componentBody.TargetCrouchFactor = (m_sneaking ? 1f : 0f);
					componentBody.UpdateCrouchState(m_sneaking);
					componentBody.UpdateBoxSize(m_sneaking);
					componentBody.m_sneakCooldownTimer = 0.7f;
					if (isServer)
					{
						ComponentPlayerPackage componentPlayerPackage = new ComponentPlayerPackage(player, PlayerAction.BodyUpdate);
						componentPlayerPackage.changFlag |= ChangFlag.SneakChange;
						componentPlayerPackage.m_sneaking = m_sneaking;
						componentPlayerPackage.Except = From;
						netNode.QueuePackage(componentPlayerPackage);
					}
				}
			});
			break;
		case PlayerAction.InteractEvent:
			PlayerEvent((ComponentPlayer player) =>
			{
				player.AddInteractEvent(m_interactEvent, m_netInteractRay, m_netInteractRaycast);
				if (isServer)
				{
					Except = From;
					netNode.QueuePackage(this);
				}
			});
			break;
		case PlayerAction.AimEvent:
			PlayerEvent((ComponentPlayer player) =>
			{
				player.AddAimEvent(m_aimEvent, m_netAimRay);
				if (isServer)
				{
					Except = From;
					netNode.QueuePackage(this);
				}
			});
			break;
		case PlayerAction.DigEvent:
			PlayerEvent((ComponentPlayer player) =>
			{
				player.AddDigEvent(m_digEvent, m_netDigRay, m_netDigRaycast);
				if (isServer)
				{
					Except = From;
					netNode.QueuePackage(this);
				}
			});
			break;
		case PlayerAction.Hit:
			PlayerEvent((ComponentPlayer player) =>
			{
				projectNet.FindEntityById(bodyId, (Entity entity) =>
				{
					ComponentBody componentBody = entity.FindComponent<ComponentBody>();
					player.ComponentMiner.Hit(componentBody, m_hitPosition, m_hitDirection);
					if (isServer)
					{
						Except = From;
						netNode.QueuePackage(this);
					}
				});
			});
			break;
		case PlayerAction.CreativeFlyChange:
			if (isServer && m_isCreativeFly && !ServerSecurity.CanUseCreativeFly(projectNet, m_playerData))
			{
				ServerSecurity.Warn(From, "creative fly outside Creative mode");
				break;
			}
			PlayerEvent((ComponentPlayer player) =>
			{
				player.ComponentLocomotion.IsCreativeFlyEnabled = m_isCreativeFly;
				if (isServer)
				{
					Except = From;
					netNode.QueuePackage(this);
				}
			});
			break;
		case PlayerAction.IntoPlaying:
			PlayerEvent((ComponentPlayer player) =>
			{
				player.ComponentHealth.IsInvulnerable = false;
			});
			break;
		case PlayerAction.Restart:
			PlayerEvent((ComponentPlayer player) =>
			{
				player.PlayerData.readyToRestart = true;
			});
			break;
		case PlayerAction.AddExperience:
			if (isServer)
			{
				ServerSecurity.Warn(From, "client tried to add experience", "count=" + m_count);
				break;
			}
			PlayerEvent((ComponentPlayer player) =>
			{
				player.ComponentLevel.NetAddExperience(m_count, m_playSound);
				player.PlayerData.Level = m_level;
			});
			break;
		case PlayerAction.Drop:
			PlayerEvent((ComponentPlayer player) =>
			{
				player.DoDrop();
			});
			break;
		case PlayerAction.DragDrop:
			PlayerEvent((ComponentPlayer player) =>
			{
				projectNet.FindSubsystem<SubsystemInventories>().FindInventoryById(m_inventory_id, (IInventory inventory) =>
				{
					player.ViewWidget.NetDragDrop(m_hitPosition, new InventoryDragData
					{
						Inventory = inventory,
						SlotIndex = m_ActiveSlot
					}, m_count);
				});
			});
			break;
		case PlayerAction.SyncStat:
			if (isServer)
			{
				ServerSecurity.Warn(From, "client tried to set player stats");
				break;
			}
			PlayerEvent((ComponentPlayer player) =>
			{
				player.PlayerStats.Load(m_stat);
			});
			break;
		case PlayerAction.PositionSet:
			if (isServer)
			{
				ServerSecurity.Warn(From, "client tried to set its own position");
				break;
			}
			PlayerEvent((ComponentPlayer player) =>
			{
				player.ComponentBody.Position = Position;
				player.ComponentBody.Velocity = Velocity;
			});
			break;
		case PlayerAction.PickBlockType:
			PlayerEvent((ComponentPlayer player) =>
			{
				if (PickBlockTypeValue.HasValue && player.ComponentMiner.Inventory is ComponentCreativeInventory { ActiveSlotIndex: var activeSlotIndex } componentCreativeInventory)
				{
					componentCreativeInventory.RemoveSlotItems(activeSlotIndex, int.MaxValue);
					componentCreativeInventory.AddSlotItems(activeSlotIndex, PickBlockTypeValue.Value, 1);
					if (isServer)
					{
						Except = From;
						netNode.QueuePackage(this);
					}
				}
			});
			break;
		}
	}

	public static ComponentPlayerPackage CreatePositionUpdate(ComponentPlayer player, Vector3 position, Quaternion? rotation)
	{
		ComponentPlayerPackage componentPlayerPackage = new ComponentPlayerPackage();
		componentPlayerPackage.m_type = PlayerAction.BodyUpdate;
		componentPlayerPackage.m_playerData = player.PlayerData;
		componentPlayerPackage.Position = position;
		componentPlayerPackage.changFlag = ChangFlag.PositionChange;
		if (rotation.HasValue)
		{
			componentPlayerPackage.Rotation = rotation.Value;
			componentPlayerPackage.changFlag |= ChangFlag.RotationChange;
		}
		return componentPlayerPackage;
	}
}
