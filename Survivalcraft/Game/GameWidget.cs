using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Game.NetWork;
using GameEntitySystem;

namespace Game;

public class GameWidget : CanvasWidget
{
	public List<Camera> m_cameras;

	public Camera m_activeCamera;

	public SubsystemTime subsystemTime;

	public NetMessageWidget messageWidget;

	public BitmapButtonWidget bitmapButtonWidget;

	public NetPlayerListWidget NetPlayerList;

	public BitmapButtonWidget NetPlayerlistButton;

	public CanvasWidget ControlsWidget;

	public SubsystemAudio subsystemAudio;

	private int m_serverViewIndex = -1;

	public CanvasWidget EmbraceChargeContainer;

	public ValueBarWidget EmbraceChargeBar;

	public ViewWidget ViewWidget { get; set; }

	public ContainerWidget GuiWidget { get; set; }

	public int GameWidgetIndex { get; set; }

	public SubsystemGameWidgets SubsystemGameWidgets { get; set; }

	public PlayerData PlayerData { get; set; }

	public ReadOnlyList<Camera> Cameras => new ReadOnlyList<Camera>(m_cameras);

	public Camera ActiveCamera
	{
		get
		{
			return m_activeCamera;
		}
		set
		{
			if (value == null || value.GameWidget != this)
			{
				throw new InvalidOperationException("Invalid camera.");
			}
			if (!IsCameraAllowed(value))
			{
				value = FindCamera<FppCamera>();
			}
			if (value != m_activeCamera)
			{
				Camera activeCamera = m_activeCamera;
				m_activeCamera = value;
				m_activeCamera.Activate(activeCamera);
			}
		}
	}

	public ComponentCreature Target { get; set; }

	public GameWidget(PlayerData playerData, int gameViewIndex)
	{
		m_cameras = new List<Camera>();
		PlayerData = playerData;
		NetPlayerList = new NetPlayerListWidget(playerData);
		messageWidget = new NetMessageWidget(playerData, NetPlayerList);
		GameWidgetIndex = gameViewIndex;
		SubsystemGameWidgets = playerData.SubsystemGameWidgets;
		subsystemTime = SubsystemGameWidgets.Project.FindSubsystem<SubsystemTime>();
		subsystemAudio = SubsystemGameWidgets.Project.FindSubsystem<SubsystemAudio>();
		LoadContents(this, ContentManager.Get<XElement>("Widgets/GameWidget"));
		bitmapButtonWidget = Children.Find<BitmapButtonWidget>("MsgButton");
		NetPlayerlistButton = Children.Find<BitmapButtonWidget>("PlayerlistButton");
		bitmapButtonWidget.Text = "";
		messageWidget.IsVisible = false;
		if (CommonLib.WorkType == WorkType.Local)
		{
			bitmapButtonWidget.IsVisible = false;
			NetPlayerlistButton.IsVisible = false;
		}
		ViewWidget = Children.Find<ViewWidget>("View");
		GuiWidget = Children.Find<ContainerWidget>("Gui");
		ControlsWidget = GuiWidget.Children.Find<CanvasWidget>("ControlsContainer");
		EmbraceChargeContainer = GuiWidget.Children.Find<CanvasWidget>("EmbraceChargeContainer");
		EmbraceChargeBar = GuiWidget.Children.Find<ValueBarWidget>("EmbraceChargeBar");
		EmbraceChargeContainer.IsVisible = false;
		if (CommonLib.Net.IsConnected && playerData.IsMainPlayer)
		{
			NetPlayerlistButton.IsChecked = true;
			NetPlayerList.Margin = new Vector2(68f, 5f);
			ControlsWidget.Children.Insert(0, NetPlayerList);
			ControlsWidget.Children.Add(messageWidget);
		}
		m_cameras.Add(new FppCamera(this));
		m_cameras.Add(new DeathCamera(this));
		m_cameras.Add(new IntroCamera(this));
		m_cameras.Add(new TppCamera(this));
		m_cameras.Add(new OrbitCamera(this));
		m_cameras.Add(new FixedCamera(this));
		m_cameras.Add(new LoadingCamera(this));
		m_activeCamera = FindCamera<LoadingCamera>();
	}

	public T FindCamera<T>(bool throwOnError = true) where T : Camera
	{
		T val = (T)m_cameras.FirstOrDefault((Camera c) => c is T);
		if (val != null || !throwOnError)
		{
			return val;
		}
		throw new InvalidOperationException("Camera with type \"" + typeof(T).Name + "\" not found.");
	}

	public bool IsEntityTarget(Entity entity)
	{
		if (Target != null)
		{
			return Target.Entity == entity;
		}
		return false;
	}

	public bool IsEntityFirstPersonTarget(Entity entity)
	{
		if (IsEntityTarget(entity))
		{
			return ActiveCamera is FppCamera;
		}
		return false;
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		if (CommonLib.WorkType != WorkType.Local)
		{
			IsVisible = PlayerData.IsMainPlayer;
		}
		base.MeasureOverride(parentAvailableSize);
	}

	public override void Update()
	{
		ComponentPlayer timeOfDay = PlayerData.ComponentPlayer;
		if (timeOfDay != null)
		{
			if (timeOfDay.ComponentSleep.IsSleeping && ControlsWidget.Children.Contains(NetPlayerList))
			{
				ControlsWidget.Children.Remove(NetPlayerList);
				ControlsWidget.Children.Remove(messageWidget);
				GuiWidget.Children.Add(NetPlayerList);
				GuiWidget.Children.Add(messageWidget);
			}
			if (!timeOfDay.ComponentSleep.IsSleeping && GuiWidget.Children.Contains(NetPlayerList))
			{
				GuiWidget.Children.Remove(NetPlayerList);
				GuiWidget.Children.Remove(messageWidget);
				ControlsWidget.Children.Insert(0, NetPlayerList);
				ControlsWidget.Children.Add(messageWidget);
			}
		}
		if (NetPlayerlistButton.IsClicked)
		{
			NetPlayerList.IsVisible = !NetPlayerList.IsVisible;
			NetPlayerlistButton.IsChecked = NetPlayerList.IsVisible;
		}
		if (CommonLib.WorkType != WorkType.Local)
		{
			if (bitmapButtonWidget.IsClicked)
			{
				messageWidget.IsVisible = !messageWidget.IsVisible;
			}
			if (KeyBindings.IsActionDownOnce(Input, KeyAction.OpenChat))
			{
				messageWidget.IsVisible = !messageWidget.IsVisible;
				messageWidget.EditText.HasFocus = messageWidget.IsVisible;
				if (PlayerData.ComponentPlayer != null)
				{
					PlayerData.ComponentPlayer.ComponentInput.AllowHandleInput = !messageWidget.EditText.HasFocus;
				}
				subsystemAudio.PlayRandomSound("Audio/Footsteps/Soft", 1f, 0f, 0f, 0f);
			}
		}
		if (!messageWidget.IsVisible)
		{
			messageWidget.EditText.HasFocus = false;
			messageWidget.EditText.IsEnabled = false;
			messageWidget.EditText.Text = string.Empty;
		}
		else
		{
			messageWidget.EditText.IsEnabled = true;
		}
		WidgetInputDevice totalElapsedGameTime = DetermineInputDevices();
		if (WidgetsHierarchyInput == null || WidgetsHierarchyInput.Devices != totalElapsedGameTime)
		{
			WidgetsHierarchyInput = new WidgetInput(totalElapsedGameTime);
		}
		float num16 = (SettingsManager.UiDimmingEnabled ? (1f - ContainerWidget.AmbientUiBlackFactor) : 1f);
		byte flag = (byte)(255f * num16);
		byte random = (byte)(255f * num16);
		byte num17 = (byte)(255f * num16);
		GuiWidget.ColorTransform = new Color(flag, random, num17, byte.MaxValue);
		if (GuiWidget.ParentWidget == null)
		{
			Widget.UpdateWidgetsHierarchy(GuiWidget);
		}
		if (GuiWidget.ParentWidget == null)
		{
			Widget.UpdateWidgetsHierarchy(GuiWidget);
		}
		if (SubsystemGameWidgets.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.GameMode == GameMode.Embrace && timeOfDay != null && EmbraceChargeContainer != null && EmbraceChargeBar != null)
		{
			ComponentLocomotion num18 = timeOfDay.ComponentLocomotion;
			float num19 = 0f;
			if (num18 != null)
			{
				num19 = MathUtils.Saturate(num18.GetEmbraceChargeMas() / 1.8f);
			}
			EmbraceChargeBar.Value = num19;
			EmbraceChargeContainer.IsVisible = true;
		}
		else if (EmbraceChargeContainer != null)
		{
			EmbraceChargeContainer.IsVisible = false;
		}
	}

	public WidgetInputDevice DetermineInputDevices()
	{
		if ((PlayerData.SubsystemPlayers.PlayersData.Count > 0 && PlayerData.IsMainPlayer) || (CommonLib.WorkType == WorkType.Local && PlayerData == PlayerData.SubsystemPlayers.PlayersData[0]))
		{
			WidgetInputDevice totalElapsedGameTime = WidgetInputDevice.None;
			foreach (PlayerData value4 in PlayerData.SubsystemPlayers.PlayersData)
			{
				if (value4 != PlayerData)
				{
					totalElapsedGameTime |= value4.InputDevice;
				}
			}
			return (WidgetInputDevice.All & ~totalElapsedGameTime) | WidgetInputDevice.Touch | PlayerData.InputDevice;
		}
		WidgetInputDevice value5 = WidgetInputDevice.None;
		foreach (PlayerData value6 in PlayerData.SubsystemPlayers.PlayersData)
		{
			if (value6 == PlayerData)
			{
				break;
			}
			value5 |= value6.InputDevice;
		}
		return (PlayerData.InputDevice & ~value5) | WidgetInputDevice.Touch;
	}

	public bool IsCameraAllowed(Camera camera)
	{
		ComponentPlayer valuesDictionary2 = PlayerData.ComponentPlayer;
		if (valuesDictionary2 != null && valuesDictionary2.ComponentInput.IsControlledByVr && !(camera is FppCamera) && !(camera is LoadingCamera))
		{
			return camera is DeathCamera;
		}
		return true;
	}
}
