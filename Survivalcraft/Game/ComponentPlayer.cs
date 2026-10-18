using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentPlayer : ComponentCreature, IUpdateable
{
	public enum InteractEvent
	{
		None,
		Start,
		Place
	}

	public enum AimEvent
	{
		None,
		InProgress,
		Cancel,
		Complete
	}

	public enum DigEvent
	{
		None,
		Start,
		Cancel,
		End
	}

	public struct DigEventItem
	{
		public DigEvent digEvent;

		public Ray3? NetDigRay;

		public TerrainRaycastResult? NetDigRaycast;
	}

	public struct AimEventItem
	{
		public AimEvent aimEvent;

		public Ray3? NetAim;
	}

	public struct InteractEventItem
	{
		public InteractEvent interactEvent;

		public Ray3 NetInteractRay;

		public TerrainRaycastResult? NetPlaceRaycast;
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemTerrain m_subsystemTerrain;

	public bool m_aimHintIssued;

	public static string fName = "ComponentPlayer";

	public double m_lastActionTime;

	public bool m_speedOrderBlocked;

	public Ray3? m_aim;

	public bool m_isAimBlocked;

	public bool m_isDigBlocked;

	public Guid PlayerGuid;

	public InteractEventItem interactEvent;

	public AimEventItem aimEvent;

	public DigEventItem digEvent;

	public InteractEventItem last_interactEvent;

	public AimEventItem last_aimEvent;

	public DigEventItem last_digEvent;

	public DragHostWidget m_dragHostWidget;

	public List<AimEventItem> aimEvents = new List<AimEventItem>();

	public List<DigEventItem> digEvents = new List<DigEventItem>();

	public List<InteractEventItem> interactEvents = new List<InteractEventItem>();

	public byte LastActiveSlot;

	private float m_buildHoldTimer;

	private bool m_isBuildHolding;

	private float m_singleClickCooldown;

	private float m_timeSinceSingleClick;

	private bool m_hasClicked;

	private Vector3 m_securityLastPos;

	private bool m_securityHasLastPos;

	private double m_securityGraceUntil;

	private double m_securityGraceStart;

	private bool m_securityGraceWaitChunks;

	public PlayerData PlayerData { get; set; }

	public GameWidget GameWidget => PlayerData.GameWidget;

	public ContainerWidget GuiWidget => PlayerData.GameWidget.GuiWidget;

	public ViewWidget ViewWidget => PlayerData.GameWidget.ViewWidget;

	public ComponentGui ComponentGui { get; set; }

	public ComponentInput ComponentInput { get; set; }

	public ComponentBlockHighlight ComponentBlockHighlight { get; set; }

	public ComponentScreenOverlays ComponentScreenOverlays { get; set; }

	public ComponentAimingSights ComponentAimingSights { get; set; }

	public ComponentMiner ComponentMiner { get; set; }

	public ComponentRider ComponentRider { get; set; }

	public ComponentSleep ComponentSleep { get; set; }

	public ComponentVitalStats ComponentVitalStats { get; set; }

	public ComponentSickness ComponentSickness { get; set; }

	public ComponentFlu ComponentFlu { get; set; }

	public ComponentLevel ComponentLevel { get; set; }

	public ComponentClothing ComponentClothing { get; set; }

	public ComponentOuterClothingModel ComponentOuterClothingModel { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public DragHostWidget DragHostWidget
	{
		get
		{
			if (m_dragHostWidget == null)
			{
				m_dragHostWidget = ((GameWidget != null) ? GameWidget.Children.Find<DragHostWidget>(throwIfNotFound: false) : null);
			}
			return m_dragHostWidget;
		}
	}

	public ComponentAlcohol ComponentAlcohol { get; set; }

	public ComponentWorldEditIntegration ComponentWorldEditIntegration { get; set; }

	public ComponentWorldEditOverlay ComponentWorldEditOverlay { get; set; }

	public ComponentAdrenaline ComponentAdrenaline { get; set; }

	public void AddDigEvent(DigEvent digEvent, Ray3? digRay, TerrainRaycastResult? raycastResult)
	{
		if (digEvents.Count <= 3)
		{
			digEvents.Add(new DigEventItem
			{
				digEvent = digEvent,
				NetDigRay = digRay,
				NetDigRaycast = raycastResult
			});
		}
	}

	public void AddAimEvent(AimEvent aimEvent, Ray3? aimRay)
	{
		if (aimEvents.Count <= 3)
		{
			aimEvents.Add(new AimEventItem
			{
				aimEvent = aimEvent,
				NetAim = aimRay
			});
		}
	}

	public void AddInteractEvent(InteractEvent interactEvent, Ray3 digRay, TerrainRaycastResult? raycastResult)
	{
		if (interactEvents.Count <= 3)
		{
			interactEvents.Add(new InteractEventItem
			{
				interactEvent = interactEvent,
				NetInteractRay = digRay,
				NetPlaceRaycast = raycastResult
			});
		}
	}

	public void DoDrop()
	{
		IInventory inventory = ComponentMiner.Inventory;
		int slotValue = inventory.GetSlotValue(inventory.ActiveSlotIndex);
		int num = inventory.RemoveSlotItems(count: inventory.GetSlotCount(inventory.ActiveSlotIndex), slotIndex: inventory.ActiveSlotIndex);
		Vector3 value = 8f * Matrix.CreateFromQuaternion(ComponentCreatureModel.EyeRotation).Forward;
		if (slotValue != 0 && num != 0)
		{
			Vector3 position = ComponentBody.Position + new Vector3(0f, ComponentBody.BoxSize.Y * 0.66f, 0f) + 0.25f * ComponentBody.Matrix.Forward;
			m_subsystemPickables.AddPickable(slotValue, num, position, value, null);
		}
	}

	public void Update(float dt)
	{
		if (PlayerData.IsMainPlayer && LastActiveSlot != ComponentMiner.Inventory.ActiveSlotIndex)
		{
			LastActiveSlot = (byte)ComponentMiner.Inventory.ActiveSlotIndex;
			CommonLib.Net.QueuePackage(new ComponentInventoryPackage(ComponentMiner.Inventory, ComponentMiner.Inventory.ActiveSlotIndex));
		}
		PlayerInput terrainBrush = ComponentInput.PlayerInput;
		if (SettingsManager.BuildSpamEnabled)
		{
			if (terrainBrush.Aim.HasValue && !m_isBuildHolding)
			{
				if (!m_hasClicked)
				{
					m_hasClicked = true;
					m_buildHoldTimer = SettingsManager.BuildHoldTimerStart;
					m_isBuildHolding = true;
				}
			}
			else if (!terrainBrush.Aim.HasValue)
			{
				m_hasClicked = false;
				m_isBuildHolding = false;
			}
			if (m_isBuildHolding)
			{
				m_buildHoldTimer -= dt;
				if (m_buildHoldTimer <= 0f)
				{
					if (terrainBrush.Aim.HasValue)
					{
						TerrainRaycastResult? num4 = ComponentMiner.Raycast<TerrainRaycastResult>(terrainBrush.Aim.Value, RaycastMode.Interaction);
						if (num4.HasValue)
						{
							interactEvent.interactEvent = InteractEvent.Place;
							interactEvent.NetInteractRay = terrainBrush.Aim.Value;
							interactEvent.NetPlaceRaycast = num4;
							if (ComponentMiner.Place(num4.Value))
							{
								CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.InteractEvent));
								m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
								m_buildHoldTimer = SettingsManager.BuildHoldTimerInterval;
							}
							else
							{
								m_isBuildHolding = false;
							}
						}
						else
						{
							m_isBuildHolding = false;
						}
					}
					else
					{
						m_isBuildHolding = false;
					}
				}
			}
		}
		else
		{
			m_isBuildHolding = false;
			m_hasClicked = false;
		}
		if (ComponentInput.IsControlledByTouch && m_aim.HasValue)
		{
			terrainBrush.Look = Vector2.Zero;
		}
		if (ComponentMiner.Inventory != null)
		{
			ComponentMiner.Inventory.ActiveSlotIndex += terrainBrush.ScrollInventory;
			if (terrainBrush.SelectInventorySlot.HasValue)
			{
				ComponentMiner.Inventory.ActiveSlotIndex = MathUtils.Clamp(terrainBrush.SelectInventorySlot.Value, 0, 9);
			}
		}
		ComponentSteedBehavior num5 = null;
		ComponentBoat num6 = null;
		ComponentMount num7 = ComponentRider.Mount;
		if (num7 != null)
		{
			num5 = num7.Entity.FindComponent<ComponentSteedBehavior>();
			num6 = num7.Entity.FindComponent<ComponentBoat>();
		}
		if (num5 != null)
		{
			if (terrainBrush.Move.Z > 0.5f && !m_speedOrderBlocked)
			{
				if (PlayerData.PlayerClass == PlayerClass.Male)
				{
					m_subsystemAudio.PlayRandomSound("Audio/Creatures/MaleYellFast", 0.75f, 0f, ComponentBody.Position, 2f, autoDelay: false);
				}
				else
				{
					m_subsystemAudio.PlayRandomSound("Audio/Creatures/FemaleYellFast", 0.75f, 0f, ComponentBody.Position, 2f, autoDelay: false);
				}
				num5.SpeedOrder = 1;
				m_speedOrderBlocked = true;
			}
			else if (terrainBrush.Move.Z < -0.5f && !m_speedOrderBlocked)
			{
				if (PlayerData.PlayerClass == PlayerClass.Male)
				{
					m_subsystemAudio.PlayRandomSound("Audio/Creatures/MaleYellSlow", 0.75f, 0f, ComponentBody.Position, 2f, autoDelay: false);
				}
				else
				{
					m_subsystemAudio.PlayRandomSound("Audio/Creatures/FemaleYellSlow", 0.75f, 0f, ComponentBody.Position, 2f, autoDelay: false);
				}
				num5.SpeedOrder = -1;
				m_speedOrderBlocked = true;
			}
			else if (MathUtils.Abs(terrainBrush.Move.Z) <= 0.25f)
			{
				m_speedOrderBlocked = false;
			}
			num5.TurnOrder = terrainBrush.Move.X;
			num5.JumpOrder = (terrainBrush.Jump ? 1f : 0f);
			ComponentLocomotion.LookOrder = new Vector2(terrainBrush.Look.X, 0f);
		}
		else if (num6 != null)
		{
			num6.TurnOrder = terrainBrush.Move.X;
			num6.MoveOrder = terrainBrush.Move.Z;
			ComponentLocomotion.LookOrder = new Vector2(terrainBrush.Look.X, 0f);
			ComponentCreatureModel.RowLeftOrder = terrainBrush.Move.X < -0.2f || terrainBrush.Move.Z > 0.2f;
			ComponentCreatureModel.RowRightOrder = terrainBrush.Move.X > 0.2f || terrainBrush.Move.Z > 0.2f;
		}
		else
		{
			ComponentLocomotion.WalkOrder = (ComponentBody.IsSneaking ? (0.66f * new Vector2(terrainBrush.SneakMove.X, terrainBrush.SneakMove.Z)) : new Vector2(terrainBrush.Move.X, terrainBrush.Move.Z));
			ComponentLocomotion.FlyOrder = new Vector3(0f, terrainBrush.Move.Y, 0f);
			ComponentLocomotion.TurnOrder = terrainBrush.Look * new Vector2(1f, 0f);
			ComponentLocomotion.JumpOrder = MathUtils.Max(terrainBrush.Jump ? 1f : 0f, ComponentLocomotion.JumpOrder);
		}
		ComponentLocomotion.LookOrder += terrainBrush.Look * (SettingsManager.FlipVerticalAxis ? new Vector2(0f, -1f) : new Vector2(0f, 1f));
		int list = Terrain.ExtractContents(ComponentMiner.ActiveBlockValue);
		Block flag = BlocksManager.Blocks[list];
		double i = (double)(Math.Abs(GetHashCode()) % 100) / 1000.0;
		if (Time.PeriodicEvent(0.1, i))
		{
			ComponentPlayerPackage j = new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.BodyUpdate);
			if (j.changFlag != ComponentPlayerPackage.ChangFlag.None)
			{
				if (CommonLib.WorkType != WorkType.Client)
				{
					if (!PlayerData.IsVanish)
					{
						j.Except = PlayerData.Client;
						CommonLib.Net.QueuePackage(j);
					}
				}
				else if (PlayerData.IsMainPlayer)
				{
					CommonLib.Net.QueuePackage(j);
				}
			}
		}
		if (CommonLib.WorkType != WorkType.Client && PlayerData != null && PlayerData.Client != null && Time.PeriodicEvent(1.0, i))
		{
			Vector3 k = ComponentBody.Position;
			bool num8 = SecurityIsInGrace();
			if (m_securityHasLastPos && !num8 && !PlayerData.IsVanish && !ComponentLocomotion.IsCreativeFlyEnabled)
			{
				float num9 = k.X - m_securityLastPos.X;
				float l = k.Z - m_securityLastPos.Z;
				float num10 = (float)Math.Sqrt(num9 * num9 + l * l);
				float num11 = ((ComponentLevel != null) ? MathUtils.Max(1f, ComponentLevel.SpeedFactor) : 1f);
				ComponentRider vector = Entity.FindComponent<ComponentRider>();
				float num12 = 25f * num11;
				bool item = vector != null && vector.Mount != null && vector.Mount.Entity.FindComponent<ComponentBoat>() != null;
				if (vector != null && vector.Mount != null && ComponentClothing != null)
				{
					float num13 = MathUtils.Max(1f, ComponentClothing.SteedMovementSpeedFactor);
					num12 = 18f * num13;
				}
				float num14 = 8f * (1f + 0.25f * (num11 - 1f));
				if (num10 > num12 && !item)
				{
					ServerSecurity.Warn(PlayerData.Client, "fast horizontal movement (log only)", "distance/s=" + num10.ToString("0.0") + " limit=" + num12.ToString("0.0"));
				}
				float item2 = k.Y - m_securityLastPos.Y;
				if (item2 > num14)
				{
					ServerSecurity.Warn(PlayerData.Client, "fast vertical movement (log only)", "rise/s=" + item2.ToString("0.0") + " limit=" + num14.ToString("0.0"));
				}
			}
			m_securityLastPos = k;
			m_securityHasLastPos = true;
		}
		if (CommonLib.WorkType != WorkType.Client && PlayerData.IsVanish && PlayerData.Client != null && Time.PeriodicEvent(1.0, i))
		{
			ComponentPlayerPackage num15 = ComponentPlayerPackage.CreatePositionUpdate(this, ComponentPlayerPackage.VanishParkedPosition, null);
			num15.Except = PlayerData.Client;
			CommonLib.Net.QueuePackage(num15);
		}
		if (CommonLib.WorkType != WorkType.Local && !PlayerData.IsMainPlayer)
		{
			if (digEvents.Count > 0)
			{
				digEvent = digEvents[0];
				digEvents.RemoveAt(0);
			}
			if (digEvent.digEvent == DigEvent.Start)
			{
				if (digEvent.NetDigRay.HasValue && digEvent.NetDigRaycast.HasValue)
				{
					ComponentMiner.Dig(digEvent.NetDigRaycast.Value);
					ComponentMiner.m_digProgress += dt;
				}
			}
			else if (digEvent.digEvent == DigEvent.Cancel)
			{
				digEvent = default;
				digEvent.NetDigRaycast = null;
				digEvent.NetDigRay = null;
				ComponentMiner.DigCellFace = null;
				ComponentMiner.m_digProgress = 0f;
			}
			else if (digEvent.digEvent == DigEvent.End)
			{
				if (digEvent.NetDigRay.HasValue)
				{
					ComponentMiner.m_digProgress = 1f;
					if (digEvent.NetDigRaycast.HasValue)
					{
						ComponentMiner.Dig(digEvent.NetDigRaycast.Value, isEnd: true);
					}
				}
				digEvent = default;
				digEvent.NetDigRaycast = null;
				digEvent.NetDigRay = null;
				ComponentMiner.DigCellFace = null;
			}
			if (aimEvents.Count > 0)
			{
				aimEvent = aimEvents[0];
				aimEvents.RemoveAt(0);
			}
			if (aimEvent.aimEvent == AimEvent.InProgress)
			{
				if (aimEvent.NetAim.HasValue)
				{
					ComponentMiner.Aim(aimEvent.NetAim.Value, AimState.InProgress);
				}
			}
			else if (aimEvent.aimEvent == AimEvent.Cancel)
			{
				if (aimEvent.NetAim.HasValue)
				{
					ComponentMiner.Aim(aimEvent.NetAim.Value, AimState.Cancelled);
				}
				aimEvent.NetAim = null;
				aimEvent = default;
			}
			else if (aimEvent.aimEvent == AimEvent.Complete)
			{
				if (aimEvent.NetAim.HasValue)
				{
					ComponentMiner.Aim(aimEvent.NetAim.Value, AimState.Completed);
				}
				m_isAimBlocked = false;
				aimEvent.NetAim = null;
				aimEvent = default;
			}
			if (interactEvents.Count > 0)
			{
				interactEvent = interactEvents[0];
				interactEvents.RemoveAt(0);
			}
			if (interactEvent.interactEvent == InteractEvent.Start)
			{
				if (!ComponentMiner.Use(interactEvent.NetInteractRay) && interactEvent.NetPlaceRaycast.HasValue)
				{
					float num16 = ComponentMiner.PokingPhase;
					ComponentMiner.Interact(interactEvent.NetPlaceRaycast.Value);
					if (num16 > 0.01f)
					{
						ComponentMiner.PokingPhase = num16;
					}
				}
				interactEvent = default;
			}
			else if (interactEvent.interactEvent == InteractEvent.Place)
			{
				if (interactEvent.NetPlaceRaycast.HasValue)
				{
					float num17 = ComponentMiner.PokingPhase;
					ComponentMiner.Place(interactEvent.NetPlaceRaycast.Value);
					if (num17 > 0.01f)
					{
						ComponentMiner.PokingPhase = num17;
					}
				}
				interactEvent = default;
			}
		}
		if (CommonLib.WorkType != WorkType.Local && (CommonLib.WorkType == WorkType.Local || !PlayerData.IsMainPlayer))
		{
			return;
		}
		digEvent = default;
		interactEvent = default;
		aimEvent = default;
		bool num18 = false;
		if (terrainBrush.Interact.HasValue && !num18 && m_subsystemTime.GameTime - m_lastActionTime > 0.33000001311302185)
		{
			interactEvent.interactEvent = InteractEvent.Start;
			interactEvent.NetInteractRay = terrainBrush.Interact.Value;
			if (!ComponentMiner.Use(interactEvent.NetInteractRay))
			{
				TerrainRaycastResult? terrainRaycastResult = ComponentMiner.Raycast<TerrainRaycastResult>(terrainBrush.Interact.Value, RaycastMode.Interaction);
				if (terrainRaycastResult.HasValue)
				{
					interactEvent.NetPlaceRaycast = terrainRaycastResult.Value;
					if (!ComponentMiner.Interact(terrainRaycastResult.Value))
					{
						interactEvent.interactEvent = InteractEvent.Place;
						if (ComponentMiner.Place(terrainRaycastResult.Value))
						{
							m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
							num18 = true;
							m_isAimBlocked = true;
						}
					}
					else
					{
						m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
						num18 = true;
						m_isAimBlocked = true;
					}
				}
			}
			else
			{
				m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
				num18 = true;
				m_isAimBlocked = true;
			}
		}
		float num19 = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? 0.1f : 1.4f);
		if (terrainBrush.Aim.HasValue && flag.IsAimable && m_subsystemTime.GameTime - m_lastActionTime > (double)num19)
		{
			if (!m_isAimBlocked)
			{
				Ray3 value = terrainBrush.Aim.Value;
				Vector3 vector2 = GameWidget.ActiveCamera.WorldToScreen(value.Position + value.Direction, Matrix.Identity);
				Point2 size = Window.Size;
				if (ComponentInput.IsControlledByVr || (vector2.X >= (float)size.X * 0.02f && vector2.X < (float)size.X * 0.98f && vector2.Y >= (float)size.Y * 0.02f && vector2.Y < (float)size.Y * 0.98f))
				{
					m_aim = value;
					aimEvent.aimEvent = AimEvent.InProgress;
					aimEvent.NetAim = value;
					if (ComponentMiner.Aim(value, AimState.InProgress))
					{
						aimEvent.aimEvent = AimEvent.Cancel;
						ComponentMiner.Aim(m_aim.Value, AimState.Cancelled);
						m_aim = null;
						m_isAimBlocked = true;
					}
					else if (!m_aimHintIssued && Time.PeriodicEvent(1.0, 0.0))
					{
						Time.QueueTimeDelayedExecution(Time.RealTime + 3.0, () =>
						{
							if (!m_aimHintIssued && m_aim.HasValue && !ComponentBody.IsSneaking)
							{
								m_aimHintIssued = true;
								ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, blinking: true, playNotificationSound: true);
							}
						});
					}
				}
				else if (m_aim.HasValue)
				{
					aimEvent.NetAim = m_aim;
					aimEvent.aimEvent = AimEvent.Cancel;
					ComponentMiner.Aim(m_aim.Value, AimState.Cancelled);
					m_aim = null;
					m_isAimBlocked = true;
				}
			}
		}
		else
		{
			m_isAimBlocked = false;
			if (m_aim.HasValue)
			{
				aimEvent.NetAim = m_aim;
				aimEvent.aimEvent = AimEvent.Complete;
				ComponentMiner.Aim(m_aim.Value, AimState.Completed);
				m_aim = null;
				m_lastActionTime = m_subsystemTime.GameTime;
			}
		}
		num18 |= m_aim.HasValue;
		float num20 = 0.13f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			float num21 = SettingsManager.CreativeDigTime;
			if (num21 > 1f)
			{
				num21 /= 1000f;
			}
			num20 += num21;
		}
		if (terrainBrush.Dig.HasValue && !num18 && !m_isDigBlocked && m_subsystemTime.GameTime - m_lastActionTime > (double)num20)
		{
			TerrainRaycastResult? netDigRaycast = ComponentMiner.Raycast<TerrainRaycastResult>(terrainBrush.Dig.Value, RaycastMode.Digging);
			digEvent.digEvent = DigEvent.Start;
			digEvent.NetDigRay = terrainBrush.Dig;
			digEvent.NetDigRaycast = netDigRaycast;
			if (netDigRaycast.HasValue && ComponentMiner.Dig(netDigRaycast.Value))
			{
				digEvent.digEvent = DigEvent.End;
				m_lastActionTime = m_subsystemTime.GameTime;
				m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
			}
		}
		if (last_digEvent.digEvent == DigEvent.Start && digEvent.digEvent == DigEvent.None)
		{
			digEvent.digEvent = DigEvent.Cancel;
		}
		if (terrainBrush.Hit.HasValue && !num18 && m_subsystemTime.GameTime - m_lastActionTime > 0.25000001311302184)
		{
			BodyRaycastResult? bodyRaycastResult = ComponentMiner.Raycast<BodyRaycastResult>(terrainBrush.Hit.Value, RaycastMode.Interaction);
			if (bodyRaycastResult.HasValue)
			{
				num18 = true;
				m_isDigBlocked = true;
				if (Vector3.Distance(bodyRaycastResult.Value.HitPoint(), ComponentCreatureModel.EyePosition) <= 2f)
				{
					Vector3 vector3 = bodyRaycastResult.Value.HitPoint();
					Vector3 direction = terrainBrush.Hit.Value.Direction;
					ComponentMiner.Hit(bodyRaycastResult.Value.ComponentBody, vector3, direction);
					if (CommonLib.WorkType == WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, bodyRaycastResult.Value.ComponentBody, vector3, direction));
					}
				}
			}
		}
		if (!terrainBrush.Dig.HasValue)
		{
			m_isDigBlocked = false;
		}
		if (terrainBrush.Drop && ComponentMiner.Inventory != null)
		{
			if (CommonLib.WorkType != WorkType.Client)
			{
				DoDrop();
			}
			else
			{
				CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.Drop));
			}
		}
		if (last_digEvent.digEvent != digEvent.digEvent || ComponentMiner.DigFaceChange)
		{
			ComponentMiner.DigFaceChange = false;
			last_digEvent = digEvent;
			CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.DigEvent));
		}
		if (last_interactEvent.interactEvent != interactEvent.interactEvent)
		{
			last_interactEvent = interactEvent;
			CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.InteractEvent));
		}
		if (last_aimEvent.aimEvent != aimEvent.aimEvent)
		{
			last_aimEvent = aimEvent;
			CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.AimEvent));
		}
		if (!terrainBrush.PickBlockType.HasValue || num18)
		{
			return;
		}
		int num22 = 0;
		bool flag2 = false;
		BodyRaycastResult? bodyRaycastResult2 = ComponentMiner.Raycast<BodyRaycastResult>(terrainBrush.PickBlockType.Value, RaycastMode.Interaction, raycastTerrain: true, raycastBodies: false, raycastMovingBlocks: false);
		if (bodyRaycastResult2.HasValue)
		{
			string name = bodyRaycastResult2.Value.ComponentBody.Entity.ValuesDictionary.DatabaseObject.Name;
			if (BlocksManager.Blocks[118] is EggBlock eggBlock)
			{
				EggBlock.EggType eggTypeByCreatureTemplateName = eggBlock.GetEggTypeByCreatureTemplateName(name);
				if (eggTypeByCreatureTemplateName != null && eggTypeByCreatureTemplateName.ShowEgg)
				{
					num22 = Terrain.MakeBlockValue(118, 0, EggBlock.SetEggType(0, eggTypeByCreatureTemplateName.EggTypeIndex));
					flag2 = true;
				}
			}
		}
		if (!flag2)
		{
			TerrainRaycastResult? terrainRaycastResult2 = ComponentMiner.Raycast<TerrainRaycastResult>(terrainBrush.PickBlockType.Value, RaycastMode.Digging, raycastTerrain: true, raycastBodies: false, raycastMovingBlocks: false);
			if (!terrainRaycastResult2.HasValue)
			{
				return;
			}
			int num23 = Terrain.ReplaceLight(terrainRaycastResult2.Value.Value, 0);
			int num24 = Terrain.ExtractContents(num23);
			Block block = BlocksManager.Blocks[num24];
			if (block.DefaultCreativeData < 0)
			{
				return;
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
			{
				int num25 = 0;
				if (Terrain.ExtractData(num23) != 0)
				{
					num25 = num23;
				}
				else
				{
					IEnumerable<int> creativeValues = block.GetCreativeValues();
					if (creativeValues.Contains(num23))
					{
						num25 = num23;
					}
					if (num25 == 0 && !block.IsNonDuplicable)
					{
						List<BlockDropValue> list2 = new List<BlockDropValue>();
						block.GetDropValues(m_subsystemTerrain, num23, 0, int.MaxValue, list2, out var _);
						if (list2.Count > 0 && list2[0].Count > 0)
						{
							num25 = list2[0].Value;
						}
					}
					if (num25 == 0)
					{
						num25 = creativeValues.FirstOrDefault();
					}
				}
				if (num25 == 0)
				{
					return;
				}
				num22 = num25;
			}
			else
			{
				num22 = num23;
			}
		}
		if (num22 == 0)
		{
			return;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.PickBlockType)
				{
					PickBlockTypeValue = num22
				});
				return;
			}
			ComponentCreativeInventory componentCreativeInventory = ComponentMiner.Inventory as ComponentCreativeInventory;
			int num26 = -1;
			for (int num27 = 0; num27 < 10; num27++)
			{
				if (componentCreativeInventory.GetSlotCapacity(num27, num22) > 0 && componentCreativeInventory.GetSlotCount(num27) > 0 && componentCreativeInventory.GetSlotValue(num27) == num22)
				{
					num26 = num27;
					break;
				}
			}
			if (num26 < 0)
			{
				for (int num28 = 0; num28 < 10; num28++)
				{
					if (componentCreativeInventory.GetSlotCapacity(num28, num22) > 0 && (componentCreativeInventory.GetSlotCount(num28) == 0 || componentCreativeInventory.GetSlotValue(num28) == 0))
					{
						num26 = num28;
						break;
					}
				}
			}
			if (num26 < 0)
			{
				num26 = componentCreativeInventory.ActiveSlotIndex;
			}
			componentCreativeInventory.RemoveSlotItems(num26, int.MaxValue);
			componentCreativeInventory.AddSlotItems(num26, num22, 1);
			componentCreativeInventory.ActiveSlotIndex = num26;
			int num29 = Terrain.ExtractContents(num22);
			ComponentGui.DisplaySmallMessage(BlocksManager.Blocks[num29].GetDisplayName(m_subsystemTerrain, num22), Color.White, blinking: false, playNotificationSound: false);
			m_subsystemAudio.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f, 0f);
			if (CommonLib.WorkType == WorkType.Server)
			{
				CommonLib.Net.QueuePackage(new ComponentPlayerPackage(this, ComponentPlayerPackage.PlayerAction.PickBlockType)
				{
					PickBlockTypeValue = num22
				});
			}
			return;
		}
		IInventory inventory = ComponentMiner.Inventory;
		if (inventory == null)
		{
			return;
		}
		int activeSlotIndex = inventory.ActiveSlotIndex;
		int num30 = Terrain.ExtractContents(num22);
		int num31 = -1;
		for (int num32 = 0; num32 < inventory.SlotsCount; num32++)
		{
			if (num32 != activeSlotIndex && inventory.GetSlotCount(num32) > 0 && Terrain.ExtractContents(inventory.GetSlotValue(num32)) == num30)
			{
				num31 = num32;
				break;
			}
		}
		if (num31 >= 0)
		{
			int slotValue = inventory.GetSlotValue(activeSlotIndex);
			int slotCount = inventory.GetSlotCount(activeSlotIndex);
			int slotValue2 = inventory.GetSlotValue(num31);
			int slotCount2 = inventory.GetSlotCount(num31);
			inventory.RemoveSlotItems(num31, slotCount2);
			inventory.RemoveSlotItems(activeSlotIndex, slotCount);
			if (slotCount > 0)
			{
				inventory.AddSlotItems(num31, slotValue, slotCount);
			}
			inventory.AddSlotItems(activeSlotIndex, slotValue2, slotCount2);
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentInventoryPackage(inventory, activeSlotIndex));
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		ComponentGui = Entity.FindComponent<ComponentGui>(throwOnError: true);
		ComponentWorldEditIntegration = Entity.FindComponent<ComponentWorldEditIntegration>(throwOnError: true);
		ComponentWorldEditOverlay = Entity.FindComponent<ComponentWorldEditOverlay>(throwOnError: true);
		ComponentInput = Entity.FindComponent<ComponentInput>(throwOnError: true);
		ComponentScreenOverlays = Entity.FindComponent<ComponentScreenOverlays>(throwOnError: true);
		ComponentBlockHighlight = Entity.FindComponent<ComponentBlockHighlight>(throwOnError: true);
		ComponentAimingSights = Entity.FindComponent<ComponentAimingSights>(throwOnError: true);
		ComponentMiner = Entity.FindComponent<ComponentMiner>(throwOnError: true);
		ComponentRider = Entity.FindComponent<ComponentRider>(throwOnError: true);
		ComponentSleep = Entity.FindComponent<ComponentSleep>(throwOnError: true);
		ComponentVitalStats = Entity.FindComponent<ComponentVitalStats>(throwOnError: true);
		ComponentSickness = Entity.FindComponent<ComponentSickness>(throwOnError: true);
		ComponentFlu = Entity.FindComponent<ComponentFlu>(throwOnError: true);
		ComponentAlcohol = Entity.FindComponent<ComponentAlcohol>(throwOnError: true);
		ComponentAdrenaline = Entity.FindComponent<ComponentAdrenaline>(throwOnError: true);
		ComponentLevel = Entity.FindComponent<ComponentLevel>(throwOnError: true);
		ComponentClothing = Entity.FindComponent<ComponentClothing>(throwOnError: true);
		ComponentOuterClothingModel = Entity.FindComponent<ComponentOuterClothingModel>(throwOnError: true);
		int playerIndex = valuesDictionary.GetValue<int>("PlayerIndex");
		try
		{
			PlayerGuid = valuesDictionary.GetValue("PlayerGuid", CommonLib.UserId);
		}
		catch
		{
			PlayerGuid = CommonLib.UserId;
		}
		SubsystemPlayers subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>();
		if (CommonLib.WorkType == WorkType.Client)
		{
			subsystemPlayers.MakePlayerOnline(PlayerGuid);
		}
		if (ProjectNet.subsystemGameInfo.WorldSettings.RunServer)
		{
			PlayerData = subsystemPlayers.PlayersData.FirstOrDefault((PlayerData d) => d.PlayerGUID == PlayerGuid);
		}
		else
		{
			PlayerData = subsystemPlayers.PlayersData.FirstOrDefault((PlayerData d) => d.PlayerIndex == playerIndex);
			if (PlayerData == null)
			{
				PlayerData = subsystemPlayers.PlayersData.FirstOrDefault();
			}
			PlayerGuid = PlayerData.PlayerGUID;
		}
		if (PlayerData == null)
		{
			throw new Exception($"Player data not found for guid {PlayerGuid}");
		}
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		valuesDictionary.SetValue("PlayerIndex", PlayerData.PlayerIndex);
		valuesDictionary.SetValue("PlayerGuid", PlayerData.PlayerGUID);
	}

	public void SecurityGrace(double seconds)
	{
		SecurityGrace(seconds, true);
	}

	public void SecurityGrace(double seconds, bool waitForChunks = true)
	{
		m_securityGraceStart = Time.RealTime;
		m_securityGraceUntil = Time.RealTime + seconds;
		m_securityGraceWaitChunks = waitForChunks;
	}

	private bool SecurityIsInGrace()
	{
		double realTime = Time.RealTime;
		if (realTime < m_securityGraceUntil)
		{
			return true;
		}
		if (m_securityGraceWaitChunks && PlayerData != null && m_subsystemTerrain != null && realTime - m_securityGraceStart < 10.0)
		{
			return m_subsystemTerrain.TerrainUpdater.GetUpdateProgress(PlayerData.PlayerIndex, 64f, 64f) < 1f;
		}
		return false;
	}
}
