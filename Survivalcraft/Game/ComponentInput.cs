using Engine;
using Engine.Input;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentInput : Component, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public ComponentGui m_componentGui;

	public ComponentPlayer m_componentPlayer;

	public PlayerInput m_playerInput;

	public bool m_isViewHoldStarted;

	public double m_lastJumpTime;

	public float m_lastLeftTrigger;

	public float m_lastRightTrigger;

	public Vector2 m_vrSmoothLook;

	private bool isChatCursorCentered;

	private bool isCapsLockActivated;

	public SubsystemGameInfo m_subsystemGameInfo;

	public bool m_jumpHoldKeyboard;

	public bool m_jumpHoldGamepad;

	public bool m_jumpHoldTouch;

	public bool m_autoJumpConsumed;

	public double m_lastFreezeDamageTime;

	public PlayerInput PlayerInput => m_playerInput;

	public bool IsControlledByTouch { get; set; } = true;

	public bool IsControlledByVr { get; set; }

	public bool AllowHandleInput { get; set; } = true;

	public IInventory SplitSourceInventory { get; set; }

	public int SplitSourceSlotIndex { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Input;

	public IInventory Split10SourceInventory { get; set; }

	public int Split10SourceSlotIndex { get; set; }

	public bool IsSplit10ModeActive => Split10SourceInventory != null;

	public bool IsCreativeFlySpeedActive => isCapsLockActivated;

	public void SetSplitSourceInventoryAndSlot(IInventory inventory, int slotIndex)
	{
		SplitSourceInventory = inventory;
		SplitSourceSlotIndex = slotIndex;
	}

	public Ray3? CalculateVrHandRay()
	{
		return null;
	}

	public void Update(float dt)
	{
		m_playerInput = default;
		m_jumpHoldKeyboard = false;
		m_jumpHoldGamepad = false;
		m_jumpHoldTouch = false;
		if (CommonLib.WorkType != WorkType.Local && !m_componentPlayer.PlayerData.IsMainPlayer)
		{
			return;
		}
		UpdateInputFromMouseAndKeyboard(m_componentPlayer.GameWidget.Input);
		UpdateInputFromGamepad(m_componentPlayer.GameWidget.Input);
		UpdateInputFromWidgets(m_componentPlayer.GameWidget.Input);
		bool flag = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
		if (!flag && AllowHandleInput)
		{
			bool flag2 = m_jumpHoldKeyboard || m_jumpHoldGamepad || m_jumpHoldTouch;
			ComponentCreature componentCreature = m_componentPlayer.Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null)
			{
				bool flag3 = (componentCreature.ComponentBody.StandingOnValue.HasValue || componentCreature.ComponentBody.ImmersionFactor > 0.5f) && !componentCreature.ComponentBody.IsSneaking;
				if ((flag2 & flag3) && !m_autoJumpConsumed)
				{
					m_playerInput.Jump = true;
					m_autoJumpConsumed = true;
				}
				if (!flag3 || !flag2)
				{
					m_autoJumpConsumed = false;
				}
			}
		}
		else
		{
			m_autoJumpConsumed = false;
		}
		if (m_componentPlayer.GameWidget.messageWidget != null)
		{
			if (m_componentPlayer.GameWidget.messageWidget.IsVisible)
			{
				m_componentPlayer.GameWidget.Input.IsMouseCursorVisible = true;
				if (!isChatCursorCentered)
				{
					ViewWidget viewWidget = m_componentPlayer.ViewWidget;
					Vector2 value = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
					m_componentPlayer.GameWidget.Input.MousePosition = value;
					isChatCursorCentered = true;
				}
			}
			else if (m_componentGui.ModalPanelWidget == null && !DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
			{
				m_componentPlayer.GameWidget.Input.IsMouseCursorVisible = false;
				isChatCursorCentered = false;
			}
		}
		if (m_playerInput.Jump)
		{
			if (flag)
			{
				if (Time.RealTime - m_lastJumpTime < 0.3)
				{
					m_playerInput.ToggleCreativeFly = true;
					m_lastJumpTime = 0.0;
				}
				else
				{
					m_lastJumpTime = Time.RealTime;
				}
			}
			else
			{
				m_lastJumpTime = Time.RealTime;
			}
		}
		m_playerInput.CameraMove = m_playerInput.Move;
		m_playerInput.CameraSneakMove = m_playerInput.SneakMove;
		m_playerInput.CameraLook = m_playerInput.Look;
		if (m_componentPlayer.ComponentVitalStats != null && m_componentPlayer.ComponentVitalStats.Temperature == 0f && m_componentPlayer.ComponentHealth.Health > 0f && (m_playerInput.Move != Vector3.Zero || m_playerInput.Jump || m_playerInput.Hit.HasValue || m_playerInput.Interact.HasValue || m_playerInput.Drop || m_playerInput.ToggleInventory) && Time.RealTime - m_lastFreezeDamageTime > 1.0)
		{
			m_lastFreezeDamageTime = Time.RealTime;
			if (CommonLib.WorkType == WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentHealthPackage(m_componentPlayer.ComponentHealth, null, 0.1f, LanguageControl.GetContentWidgets(VitalStatsWidget.fName, "Suicide"), ignoreInvulnerability: true, isRequest: true));
			}
			else
			{
				m_componentPlayer.ComponentHealth.Injure(0.1f, null, ignoreInvulnerability: true, LanguageControl.GetContentWidgets(VitalStatsWidget.fName, "Suicide"));
			}
		}
		if (!Window.IsActive || !m_componentPlayer.PlayerData.IsReadyForPlaying)
		{
			m_playerInput = default;
		}
		else if (m_componentPlayer.ComponentHealth.Health <= 0f || m_componentPlayer.ComponentSleep.SleepFactor > 0f || !m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled || m_componentPlayer.ComponentVitalStats.Temperature == 0f)
		{
			m_playerInput = new PlayerInput
			{
				CameraMove = m_playerInput.CameraMove,
				CameraSneakMove = m_playerInput.CameraSneakMove,
				CameraLook = m_playerInput.CameraLook,
				TimeOfDay = m_playerInput.TimeOfDay,
				TakeScreenshot = m_playerInput.TakeScreenshot,
				KeyboardHelp = m_playerInput.KeyboardHelp
			};
		}
		else if (m_componentPlayer.GameWidget.ActiveCamera.UsesMovementControls)
		{
			m_playerInput.Move = Vector3.Zero;
			m_playerInput.SneakMove = Vector3.Zero;
			m_playerInput.Look = Vector2.Zero;
			m_playerInput.Jump = false;
			m_playerInput.ToggleSneak = false;
			m_playerInput.ToggleCreativeFly = false;
		}
		if (m_playerInput.Move.LengthSquared() > 1f)
		{
			m_playerInput.Move = Vector3.Normalize(m_playerInput.Move);
		}
		if (m_playerInput.SneakMove.LengthSquared() > 1f)
		{
			m_playerInput.SneakMove = Vector3.Normalize(m_playerInput.SneakMove);
		}
		if (SplitSourceInventory != null && SplitSourceInventory.GetSlotCount(SplitSourceSlotIndex) == 0)
		{
			SetSplitSourceInventoryAndSlot(null, -1);
		}
		if (Split10SourceInventory != null && Split10SourceInventory.GetSlotCount(Split10SourceSlotIndex) == 0)
		{
			SetSplit10SourceInventoryAndSlot(null, -1);
		}
		if (m_componentGui.ModalPanelWidget == null && !DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
		{
			SetSplitSourceInventoryAndSlot(null, -1);
			SetSplit10SourceInventoryAndSlot(null, -1);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_componentGui = Entity.FindComponent<ComponentGui>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	public void UpdateInputFromMouseAndKeyboard(WidgetInput input)
	{
		Vector3 viewPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
		Vector3 viewDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
		float num = MathUtils.Pow(1.25f, 10f * (SettingsManager.LookSensitivity - 0.5f));
		if (m_componentGui.ModalPanelWidget != null || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
		{
			if (!input.IsMouseCursorVisible)
			{
				ViewWidget viewWidget = m_componentPlayer.ViewWidget;
				Vector2 value = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
				input.IsMouseCursorVisible = true;
				input.MousePosition = value;
			}
		}
		else if (AllowHandleInput)
		{
			input.IsMouseCursorVisible = false;
			Vector2 zero = Vector2.Zero;
			int num2 = 0;
			if (Window.IsActive && Time.FrameDuration > 0f)
			{
				Point2 mouseMovement = input.MouseMovement;
				int mouseWheelMovement = input.MouseWheelMovement;
				zero.X = 0.02f * (float)mouseMovement.X / Time.FrameDuration / 60f * num;
				zero.Y = -0.02f * (float)mouseMovement.Y / Time.FrameDuration / 60f * num;
				num2 = mouseWheelMovement / 120;
				if (mouseMovement != Point2.Zero)
				{
					IsControlledByTouch = false;
				}
			}
			Vector3 vector = default(Vector3) + Vector3.UnitX * (KeyBindings.IsActionDown(input, KeyAction.MoveRight) ? 1f : 0f);
			vector += -Vector3.UnitZ * (KeyBindings.IsActionDown(input, KeyAction.MoveBackward) ? 1f : 0f);
			vector += Vector3.UnitZ * (KeyBindings.IsActionDown(input, KeyAction.MoveForward) ? 1f : 0f);
			vector += -Vector3.UnitX * (KeyBindings.IsActionDown(input, KeyAction.MoveLeft) ? 1f : 0f);
			if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled)
			{
				vector += Vector3.UnitY * (KeyBindings.IsActionDown(input, KeyAction.FlyUp) ? 1f : 0f);
				vector += -Vector3.UnitY * (KeyBindings.IsActionDown(input, KeyAction.FlyDown) ? 1f : 0f);
			}
			m_jumpHoldKeyboard = KeyBindings.IsActionDown(input, KeyAction.Jump);
			m_playerInput.Look += new Vector2(MathUtils.Clamp(zero.X, -15f, 15f), MathUtils.Clamp(zero.Y, -15f, 15f));
			m_playerInput.Move += vector;
			m_playerInput.SneakMove += vector;
			m_playerInput.Jump |= KeyBindings.IsActionDownOnce(input, KeyAction.Jump);
			m_playerInput.ScrollInventory -= num2;
			m_playerInput.Dig = (input.IsMouseButtonDown(MouseButton.Left) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Dig);
			m_playerInput.Hit = (input.IsMouseButtonDownOnce(MouseButton.Left) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Hit);
			m_playerInput.Aim = (input.IsMouseButtonDown(MouseButton.Right) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Aim);
			m_playerInput.Interact = (input.IsMouseButtonDownOnce(MouseButton.Right) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Interact);
			m_playerInput.ToggleSneak |= KeyBindings.IsActionDownOnce(input, KeyAction.ToggleSneak);
			m_playerInput.ToggleMount |= KeyBindings.IsActionDownOnce(input, KeyAction.ToggleMount);
			m_playerInput.ToggleCreativeFly |= KeyBindings.IsActionDownOnce(input, KeyAction.ToggleCreativeFly);
			bool flag = m_componentGui.m_pickBlockButtonWidget != null && m_componentGui.m_pickBlockButtonWidget.IsClicked;
			m_playerInput.PickBlockType = ((input.IsMouseButtonDownOnce(MouseButton.Middle) | flag) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.PickBlockType);
			if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled && KeyBindings.IsActionDownOnce(input, KeyAction.ToggleSpeedCreativeFly))
			{
				if (!isCapsLockActivated)
				{
					m_componentPlayer.ComponentLocomotion.CreativeFlySpeed *= 6f;
					isCapsLockActivated = true;
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 20), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					m_componentPlayer.ComponentLocomotion.CreativeFlySpeed /= 6f;
					isCapsLockActivated = false;
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(ComponentGui.fName, 21), Color.White, blinking: false, playNotificationSound: false);
				}
			}
		}
		if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget) && AllowHandleInput)
		{
			m_playerInput.ToggleInventory |= KeyBindings.IsActionDownOnce(input, KeyAction.ToggleInventory);
			m_playerInput.ToggleClothing |= KeyBindings.IsActionDownOnce(input, KeyAction.ToggleClothing);
			m_playerInput.TakeScreenshot |= KeyBindings.IsActionDownOnce(input, KeyAction.TakeScreenshot);
			m_playerInput.SwitchCameraMode |= KeyBindings.IsActionDownOnce(input, KeyAction.SwitchCameraMode);
			m_playerInput.TimeOfDay |= KeyBindings.IsActionDownOnce(input, KeyAction.TimeOfDay);
			m_playerInput.Lighting |= KeyBindings.IsActionDownOnce(input, KeyAction.Lighting);
			m_playerInput.Drop |= KeyBindings.IsActionDownOnce(input, KeyAction.Drop);
			m_playerInput.EditItem |= KeyBindings.IsActionDownOnce(input, KeyAction.EditItem);
			m_playerInput.KeyboardHelp |= KeyBindings.IsActionDownOnce(input, KeyAction.KeyboardHelp);
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar1))
			{
				SetHotbarSlotEmbraceLimited(0);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar2))
			{
				SetHotbarSlotEmbraceLimited(1);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar3))
			{
				SetHotbarSlotEmbraceLimited(2);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar4))
			{
				SetHotbarSlotEmbraceLimited(3);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar5))
			{
				SetHotbarSlotEmbraceLimited(4);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar6))
			{
				SetHotbarSlotEmbraceLimited(5);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar7))
			{
				SetHotbarSlotEmbraceLimited(6);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar8))
			{
				SetHotbarSlotEmbraceLimited(7);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar9))
			{
				SetHotbarSlotEmbraceLimited(8);
			}
			if (KeyBindings.IsActionDownOnce(input, KeyAction.Hotbar0))
			{
				SetHotbarSlotEmbraceLimited(9);
			}
		}
	}

	public void UpdateInputFromGamepad(WidgetInput input)
	{
		Vector3 viewPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
		Vector3 viewDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
		if (m_componentGui.ModalPanelWidget != null || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget))
		{
			if (!input.IsPadCursorVisible)
			{
				ViewWidget viewWidget = m_componentPlayer.ViewWidget;
				Vector2 padCursorPosition = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
				input.IsPadCursorVisible = true;
				input.PadCursorPosition = padCursorPosition;
			}
		}
		else
		{
			input.IsPadCursorVisible = false;
			Vector3 zero = Vector3.Zero;
			Vector2 padStickPosition = input.GetPadStickPosition(GamePadStick.Left, SettingsManager.GamepadDeadZone);
			Vector2 padStickPosition2 = input.GetPadStickPosition(GamePadStick.Right, SettingsManager.GamepadDeadZone);
			float padTriggerPosition = input.GetPadTriggerPosition(GamePadTrigger.Left);
			float padTriggerPosition2 = input.GetPadTriggerPosition(GamePadTrigger.Right);
			zero += new Vector3(2f * padStickPosition.X, 0f, 2f * padStickPosition.Y);
			if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled)
			{
				zero += Vector3.UnitY * (input.IsPadButtonDown(GamePadButton.A) ? 1 : 0);
				zero += -Vector3.UnitY * (input.IsPadButtonDown(GamePadButton.RightShoulder) ? 1 : 0);
			}
			m_jumpHoldGamepad = input.IsPadButtonDown(GamePadButton.A);
			m_playerInput.Move += zero;
			m_playerInput.SneakMove += zero;
			m_playerInput.Look += 0.75f * padStickPosition2 * MathUtils.Pow(padStickPosition2.LengthSquared(), 0.25f);
			m_playerInput.Jump |= input.IsPadButtonDownOnce(GamePadButton.A);
			m_playerInput.Dig = ((padTriggerPosition2 >= 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Dig);
			m_playerInput.Hit = ((padTriggerPosition2 >= 0.5f && m_lastRightTrigger < 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Hit);
			m_playerInput.Aim = ((padTriggerPosition >= 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Aim);
			m_playerInput.Interact = ((padTriggerPosition >= 0.5f && m_lastLeftTrigger < 0.5f) ? new Ray3?(new Ray3(viewPosition, viewDirection)) : m_playerInput.Interact);
			m_playerInput.Drop |= input.IsPadButtonDownOnce(GamePadButton.B);
			m_playerInput.ToggleMount |= input.IsPadButtonDownOnce(GamePadButton.LeftThumb) || input.IsPadButtonDownOnce(GamePadButton.DPadUp);
			m_playerInput.EditItem |= input.IsPadButtonDownOnce(GamePadButton.LeftShoulder);
			m_playerInput.ToggleSneak |= input.IsPadButtonDownOnce(GamePadButton.RightShoulder);
			m_playerInput.SwitchCameraMode |= input.IsPadButtonDownOnce(GamePadButton.RightThumb) || input.IsPadButtonDownOnce(GamePadButton.DPadDown);
			if (input.IsPadButtonDownRepeat(GamePadButton.DPadLeft))
			{
				m_playerInput.ScrollInventory--;
			}
			if (input.IsPadButtonDownRepeat(GamePadButton.DPadRight))
			{
				m_playerInput.ScrollInventory++;
			}
			if (padStickPosition != Vector2.Zero || padStickPosition2 != Vector2.Zero)
			{
				IsControlledByTouch = false;
			}
			m_lastLeftTrigger = padTriggerPosition;
			m_lastRightTrigger = padTriggerPosition2;
		}
		if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget) && AllowHandleInput)
		{
			m_playerInput.ToggleInventory |= input.IsPadButtonDownOnce(GamePadButton.X);
			m_playerInput.ToggleClothing |= input.IsPadButtonDownOnce(GamePadButton.Y);
			m_playerInput.GamepadHelp |= input.IsPadButtonDownOnce(GamePadButton.Start);
		}
	}

	public void UpdateInputFromWidgets(WidgetInput input)
	{
		float num = MathUtils.Pow(1.25f, 10f * (SettingsManager.MoveSensitivity - 0.5f));
		float num2 = MathUtils.Pow(1.25f, 10f * (SettingsManager.LookSensitivity - 0.5f));
		float num3 = MathUtils.Clamp(m_subsystemTime.GameTimeDelta, 0f, 0.1f);
		ViewWidget viewWidget = m_componentPlayer.ViewWidget;
		m_componentGui.MoveWidget.Radius = 30f / num * m_componentGui.MoveWidget.GlobalScale;
		if (m_componentGui.ModalPanelWidget != null || m_subsystemTime.GameTimeFactor <= 0f || num3 <= 0f)
		{
			return;
		}
		Vector2 v = new Vector2(SettingsManager.LeftHandedLayout ? 96 : (-96), -96f);
		v = Vector2.TransformNormal(v, input.Widget.GlobalTransform);
		if (m_componentGui.ViewWidget != null && m_componentGui.ViewWidget.TouchInput.HasValue)
		{
			IsControlledByTouch = true;
			TouchInput value = m_componentGui.ViewWidget.TouchInput.Value;
			Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
			Vector3 viewPosition = activeCamera.ViewPosition;
			Vector3 viewDirection = activeCamera.ViewDirection;
			Vector3 vector = Vector3.Normalize(activeCamera.ScreenToWorld(new Vector3(value.Position, 1f), Matrix.Identity) - viewPosition);
			Vector3 vector2 = Vector3.Normalize(activeCamera.ScreenToWorld(new Vector3(value.Position + v, 1f), Matrix.Identity) - viewPosition);
			if (value.InputType == TouchInputType.Tap)
			{
				if (SettingsManager.LookControlMode == LookControlMode.SplitTouch)
				{
					m_playerInput.Interact = new Ray3(viewPosition, viewDirection);
					m_playerInput.Hit = new Ray3(viewPosition, viewDirection);
				}
				else
				{
					m_playerInput.Interact = new Ray3(viewPosition, vector);
					m_playerInput.Hit = new Ray3(viewPosition, vector);
				}
			}
			else if (value.InputType == TouchInputType.Hold && value.DurationFrames > 1 && value.Duration > 0.2f)
			{
				bool isPlaceable = BlocksManager.Blocks[Terrain.ExtractContents(m_componentPlayer.ComponentMiner.ActiveBlockValue)].IsPlaceable;
				bool flag = SettingsManager.BuildSpamEnabled & isPlaceable;
				if (SettingsManager.LookControlMode == LookControlMode.SplitTouch)
				{
					m_playerInput.Dig = new Ray3(viewPosition, viewDirection);
					m_playerInput.Aim = new Ray3(viewPosition, flag ? viewDirection : vector2);
				}
				else
				{
					m_playerInput.Dig = new Ray3(viewPosition, vector);
					m_playerInput.Aim = new Ray3(viewPosition, flag ? vector : vector2);
				}
				m_isViewHoldStarted = true;
			}
			else if (value.InputType == TouchInputType.Move)
			{
				if (SettingsManager.LookControlMode == LookControlMode.EntireScreen || SettingsManager.LookControlMode == LookControlMode.SplitTouch)
				{
					Vector2 vector3 = Vector2.TransformNormal(value.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
					Vector2 vector4 = num2 / num3 * new Vector2(0.0006f, -0.0006f) * vector3 * MathUtils.Pow(vector3.LengthSquared(), 0.125f);
					m_playerInput.Look += vector4;
				}
				if (m_isViewHoldStarted)
				{
					bool isPlaceable2 = BlocksManager.Blocks[Terrain.ExtractContents(m_componentPlayer.ComponentMiner.ActiveBlockValue)].IsPlaceable;
					bool flag2 = SettingsManager.BuildSpamEnabled & isPlaceable2;
					if (SettingsManager.LookControlMode == LookControlMode.SplitTouch)
					{
						m_playerInput.Dig = new Ray3(viewPosition, viewDirection);
						m_playerInput.Aim = new Ray3(viewPosition, flag2 ? viewDirection : vector2);
					}
					else
					{
						m_playerInput.Dig = new Ray3(viewPosition, vector);
						m_playerInput.Aim = new Ray3(viewPosition, flag2 ? vector : vector2);
					}
				}
			}
		}
		else
		{
			m_isViewHoldStarted = false;
		}
		if (m_componentGui.MoveWidget != null && m_componentGui.MoveWidget.TouchInput.HasValue)
		{
			IsControlledByTouch = true;
			float radius = m_componentGui.MoveWidget.Radius;
			TouchInput value2 = m_componentGui.MoveWidget.TouchInput.Value;
			if (value2.InputType == TouchInputType.Tap)
			{
				m_playerInput.Jump = true;
			}
			else if (value2.InputType == TouchInputType.Move || value2.InputType == TouchInputType.Hold)
			{
				Vector2 vector5 = Vector2.TransformNormal(value2.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
				Vector2 vector6 = num / num3 * new Vector2(0.003f, -0.003f) * vector5 * MathUtils.Pow(vector5.LengthSquared(), 0.175f);
				m_playerInput.SneakMove.X = m_playerInput.SneakMove.X + vector6.X;
				m_playerInput.SneakMove.Z = m_playerInput.SneakMove.Z + vector6.Y;
				Vector2 vector7 = Vector2.TransformNormal(value2.TotalMoveLimited, m_componentGui.ViewWidget.InvertedGlobalTransform);
				m_playerInput.Move.X = m_playerInput.Move.X + ProcessInputValue(vector7.X * viewWidget.GlobalScale, 0.2f * radius, radius);
				m_playerInput.Move.Z = m_playerInput.Move.Z + ProcessInputValue((0f - vector7.Y) * viewWidget.GlobalScale, 0.2f * radius, radius);
			}
		}
		if (m_componentGui.MoveRoseWidget != null)
		{
			if (m_componentGui.MoveRoseWidget.Direction != Vector3.Zero || m_componentGui.MoveRoseWidget.Jump)
			{
				IsControlledByTouch = true;
			}
			m_playerInput.Move += m_componentGui.MoveRoseWidget.Direction;
			m_playerInput.SneakMove += m_componentGui.MoveRoseWidget.Direction;
			m_jumpHoldTouch = m_componentGui.MoveRoseWidget.Jump;
		}
		if (m_componentGui.LookWidget != null && m_componentGui.LookWidget.TouchInput.HasValue)
		{
			IsControlledByTouch = true;
			TouchInput value3 = m_componentGui.LookWidget.TouchInput.Value;
			if (value3.InputType == TouchInputType.Tap)
			{
				m_playerInput.Jump = true;
			}
			else if (value3.InputType == TouchInputType.Move)
			{
				Vector2 vector8 = Vector2.TransformNormal(value3.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
				Vector2 vector9 = num2 / num3 * new Vector2(0.0006f, -0.0006f) * vector8 * MathUtils.Pow(vector8.LengthSquared(), 0.125f);
				m_playerInput.Look += vector9;
			}
		}
	}

	public static float ProcessInputValue(float value, float deadZone, float saturationZone)
	{
		return MathUtils.Sign(value) * MathUtils.Clamp((MathUtils.Abs(value) - deadZone) / (saturationZone - deadZone), 0f, 1f);
	}

	public void SetSplit10SourceInventoryAndSlot(IInventory inventory, int slotIndex)
	{
		Split10SourceInventory = inventory;
		Split10SourceSlotIndex = slotIndex;
	}

	private void SetHotbarSlotEmbraceLimited(int slotIndex)
	{
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && m_componentPlayer != null)
		{
			int num = 2;
			if (slotIndex >= num)
			{
				return;
			}
		}
		m_playerInput.SelectInventorySlot = slotIndex;
	}
}
