using Engine;
using GameEntitySystem;

namespace Game.NetWork.Packages;

public class ComponentBehaviorPackage : IPackage
{
	public enum EventType
	{
		ChaseBehavior = 0,
		RandomPeck = 2,
		RandomFeed = 3,
		EatPickable = 4,
		HumanRow = 5,
		CreatureSound = 6,
		DigInMud = 7,
		FishOutOfWater = 8,
		SmartChaseBehavior = 1,
		KnockWindow = 9,
		RhinoChaseBehavior = 10,
		CunningChaseBehavior = 11,
		MarineChaseBehavior = 12,
		OrcasChaseBehavior = 13,
		CircularChaseBehavior = 14
	}

	public ushort m_entityId;

	public EventType eventType;

	public float? m_bend;

	public float m_digin;

	public bool m_rowLeft;

	public bool m_rowRight;

	public byte m_type;

	public Vector3 m_windowPosition;

	public byte ID => 12;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public ComponentBehaviorPackage()
	{
	}

	public ComponentBehaviorPackage(ComponentFishOutOfWaterBehavior fishOutOfWaterBehavior, bool isBend)
	{
		m_entityId = fishOutOfWaterBehavior.Entity.EntityId;
		eventType = EventType.FishOutOfWater;
		m_rowLeft = isBend;
	}

	public ComponentBehaviorPackage(ComponentChaseBehavior behavior, bool isChase)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.ChaseBehavior;
		m_rowLeft = isChase;
	}

	public ComponentBehaviorPackage(ComponentCreatureSounds creatureSound, byte type, bool skip = false)
	{
		m_entityId = creatureSound.Entity.EntityId;
		eventType = EventType.CreatureSound;
		m_type = type;
		m_rowLeft = skip;
	}

	public ComponentBehaviorPackage(ComponentRandomPeckBehavior behavior, bool inRange)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.RandomPeck;
		m_rowLeft = inRange;
	}

	public ComponentBehaviorPackage(ComponentRandomFeedBehavior behavior, bool inRange)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.RandomFeed;
		m_rowLeft = inRange;
	}

	public ComponentBehaviorPackage(ComponentEatPickableBehavior behavior, bool inRange)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.EatPickable;
		m_rowLeft = inRange;
	}

	public ComponentBehaviorPackage(ComponentDigInMudBehavior behavior, bool IsDigIn)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.DigInMud;
		m_rowLeft = IsDigIn;
	}

	public ComponentBehaviorPackage(ComponentHumanModel componentModel)
	{
		eventType = EventType.HumanRow;
		m_entityId = componentModel.Entity.EntityId;
		m_rowLeft = componentModel.m_rowLeft;
		m_rowRight = componentModel.m_rowRight;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.WriteEnum(eventType);
		writer.Write(m_entityId);
		switch (eventType)
		{
		case EventType.ChaseBehavior:
		case EventType.RandomPeck:
		case EventType.RandomFeed:
		case EventType.EatPickable:
		case EventType.DigInMud:
		case EventType.FishOutOfWater:
		case EventType.RhinoChaseBehavior:
		case EventType.CunningChaseBehavior:
		case EventType.MarineChaseBehavior:
		case EventType.OrcasChaseBehavior:
		case EventType.CircularChaseBehavior:
			writer.Write(m_rowLeft);
			break;
		case EventType.SmartChaseBehavior:
		case EventType.HumanRow:
			writer.Write(m_rowLeft);
			writer.Write(m_rowRight);
			break;
		case EventType.CreatureSound:
			writer.Write(m_type);
			writer.Write(m_rowLeft);
			break;
		case EventType.KnockWindow:
			writer.Write(m_windowPosition);
			break;
		}
	}

	public void ReadData(PackageStreamReader reader)
	{
		eventType = reader.ReadEnum<EventType>();
		m_entityId = reader.ReadUInt16();
		switch (eventType)
		{
		case EventType.ChaseBehavior:
		case EventType.RandomPeck:
		case EventType.RandomFeed:
		case EventType.EatPickable:
		case EventType.DigInMud:
		case EventType.FishOutOfWater:
		case EventType.RhinoChaseBehavior:
		case EventType.CunningChaseBehavior:
		case EventType.MarineChaseBehavior:
		case EventType.OrcasChaseBehavior:
		case EventType.CircularChaseBehavior:
			m_rowLeft = reader.ReadBoolean();
			break;
		case EventType.SmartChaseBehavior:
		case EventType.HumanRow:
			m_rowLeft = reader.ReadBoolean();
			m_rowRight = reader.ReadBoolean();
			break;
		case EventType.CreatureSound:
			m_type = reader.ReadByte();
			m_rowLeft = reader.ReadBoolean();
			break;
		case EventType.KnockWindow:
			m_windowPosition = reader.ReadVector3();
			break;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindEntityById(m_entityId, (Entity entity) =>
		{
			switch (eventType)
			{
			case EventType.ChaseBehavior:
			{
				ComponentChaseBehavior componentChaseBehavior = entity.FindComponent<ComponentChaseBehavior>();
				if (componentChaseBehavior != null)
				{
					componentChaseBehavior.IsAttack = m_rowLeft;
				}
				break;
			}
			case EventType.SmartChaseBehavior:
			{
				ComponentSmartChaseBehavior componentSmartChaseBehavior = entity.FindComponent<ComponentSmartChaseBehavior>();
				if (componentSmartChaseBehavior != null)
				{
					componentSmartChaseBehavior.IsAttack = m_rowLeft;
					componentSmartChaseBehavior.IsSneaking = m_rowRight;
				}
				break;
			}
			case EventType.RandomPeck:
			{
				ComponentRandomPeckBehavior componentRandomPeckBehavior = entity.FindComponent<ComponentRandomPeckBehavior>();
				if (componentRandomPeckBehavior != null)
				{
					componentRandomPeckBehavior.IsFeed = m_rowLeft;
				}
				break;
			}
			case EventType.RandomFeed:
			{
				ComponentRandomFeedBehavior componentRandomFeedBehavior = entity.FindComponent<ComponentRandomFeedBehavior>();
				if (componentRandomFeedBehavior != null)
				{
					componentRandomFeedBehavior.IsFeed = m_rowLeft;
				}
				break;
			}
			case EventType.EatPickable:
			{
				ComponentEatPickableBehavior componentEatPickableBehavior = entity.FindComponent<ComponentEatPickableBehavior>();
				if (componentEatPickableBehavior != null)
				{
					componentEatPickableBehavior.IsFeed = m_rowLeft;
				}
				break;
			}
			case EventType.HumanRow:
			{
				ComponentHumanModel componentHumanModel = entity.FindComponent<ComponentHumanModel>();
				if (componentHumanModel != null)
				{
					componentHumanModel.hasData = true;
					componentHumanModel.m_rowLeft = m_rowLeft;
					componentHumanModel.m_rowRight = m_rowRight;
					Random random = new Random();
					projectNet.FindSubsystem<SubsystemAudio>().PlayRandomSound("Audio/Rowing", random.Float(0.4f, 0.6f), random.Float(-0.3f, 0.2f), componentHumanModel.m_componentCreature.ComponentBody.Position, 3f, autoDelay: true);
					if (isServer)
					{
						Except = From;
						netNode.QueuePackage(this);
					}
				}
				break;
			}
			case EventType.CreatureSound:
			{
				ComponentCreatureSounds componentCreatureSounds = entity.FindComponent<ComponentCreatureSounds>();
				if (componentCreatureSounds != null)
				{
					switch (m_type)
					{
					case 0:
						componentCreatureSounds.PlayIdleSoundLogic(m_rowLeft);
						break;
					case 1:
						componentCreatureSounds.PlayPainSoundLogic();
						break;
					case 2:
						componentCreatureSounds.PlayMoanSoundLogic();
						break;
					case 3:
						componentCreatureSounds.PlaySneezeSoundLogic();
						break;
					case 4:
						componentCreatureSounds.PlayCoughSoundLogic();
						break;
					case 5:
						componentCreatureSounds.PlayPukeSoundLogic();
						break;
					case 6:
						componentCreatureSounds.PlayAttackSoundLogic();
						break;
					case 7:
						componentCreatureSounds.PlayFallDamageSoundLogic();
						break;
					}
				}
				break;
			}
			case EventType.DigInMud:
			{
				ComponentDigInMudBehavior componentDigInMudBehavior = entity.FindComponent<ComponentDigInMudBehavior>();
				if (componentDigInMudBehavior != null)
				{
					componentDigInMudBehavior.IsDigIn = m_rowLeft;
				}
				break;
			}
			case EventType.FishOutOfWater:
			{
				ComponentFishOutOfWaterBehavior componentFishOutOfWaterBehavior = entity.FindComponent<ComponentFishOutOfWaterBehavior>();
				if (componentFishOutOfWaterBehavior != null)
				{
					componentFishOutOfWaterBehavior.IsBend = m_rowLeft;
				}
				break;
			}
			case EventType.KnockWindow:
			{
				Vector3 windowPosition = m_windowPosition;
				projectNet.FindSubsystem<SubsystemAudio>().PlayRandomSound("Audio/KnockWindow", 1f, 0.1f, windowPosition, 4f, autoDelay: true);
				if (isServer)
				{
					Except = From;
					netNode.QueuePackage(this);
				}
				break;
			}
			case EventType.RhinoChaseBehavior:
			{
				ComponentRhinoChaseBehavior componentRhinoChaseBehavior = entity.FindComponent<ComponentRhinoChaseBehavior>();
				if (componentRhinoChaseBehavior != null)
				{
					componentRhinoChaseBehavior.IsAttack = m_rowLeft;
				}
				break;
			}
			case EventType.CunningChaseBehavior:
			{
				ComponentCunningChaseBehavior componentCunningChaseBehavior = entity.FindComponent<ComponentCunningChaseBehavior>();
				if (componentCunningChaseBehavior != null)
				{
					componentCunningChaseBehavior.IsAttack = m_rowLeft;
				}
				break;
			}
			case EventType.MarineChaseBehavior:
			{
				ComponentMarineChaseBehavior componentMarineChaseBehavior = entity.FindComponent<ComponentMarineChaseBehavior>();
				if (componentMarineChaseBehavior != null)
				{
					componentMarineChaseBehavior.IsAttack = m_rowLeft;
				}
				break;
			}
			case EventType.OrcasChaseBehavior:
			{
				ComponentOrcasChaseBehavior componentOrcasChaseBehavior = entity.FindComponent<ComponentOrcasChaseBehavior>();
				if (componentOrcasChaseBehavior != null)
				{
					componentOrcasChaseBehavior.IsAttack = m_rowLeft;
				}
				break;
			}
			case EventType.CircularChaseBehavior:
			{
				ComponentCircularChaseBehavior componentCircularChaseBehavior = entity.FindComponent<ComponentCircularChaseBehavior>();
				if (componentCircularChaseBehavior != null)
				{
					componentCircularChaseBehavior.IsAttack = m_rowLeft;
				}
				break;
			}
			}
		});
	}

	public ComponentBehaviorPackage(ComponentSmartChaseBehavior behavior, Vector3 windowPosition)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.KnockWindow;
		m_windowPosition = windowPosition;
	}

	public ComponentBehaviorPackage(ComponentRhinoChaseBehavior behavior, bool isChase)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.RhinoChaseBehavior;
		m_rowLeft = isChase;
	}

	public ComponentBehaviorPackage(ComponentCunningChaseBehavior behavior, bool isChase)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.CunningChaseBehavior;
		m_rowLeft = isChase;
	}

	public ComponentBehaviorPackage(ComponentMarineChaseBehavior behavior, bool isChase)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.MarineChaseBehavior;
		m_rowLeft = isChase;
	}

	public ComponentBehaviorPackage(ComponentOrcasChaseBehavior behavior, bool isChase)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.OrcasChaseBehavior;
		m_rowLeft = isChase;
	}

	public ComponentBehaviorPackage(ComponentCircularChaseBehavior behavior, bool isChase)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.CircularChaseBehavior;
		m_rowLeft = isChase;
	}

	public ComponentBehaviorPackage(ComponentSmartChaseBehavior behavior, bool isChase, bool isSneaking)
	{
		m_entityId = behavior.Entity.EntityId;
		eventType = EventType.SmartChaseBehavior;
		m_rowLeft = isChase;
		m_rowRight = isSneaking;
	}
}
