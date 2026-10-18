using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentGui : Component, IUpdateable
{
	public class ModalPanelAnimationData
	{
		public Widget NewWidget;

		public Widget OldWidget;

		public float Factor;
	}

	public class Message
	{
		public string LargeText;

		public string SmallText;

		public double StartTime;

		public float Duration;
	}

	public static string fName;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public ComponentPlayer m_componentPlayer;

	public ContainerWidget m_leftControlsContainerWidget;

	public int CloseTime;

	public ContainerWidget m_rightControlsContainerWidget;

	public ContainerWidget m_moveContainerWidget;

	public ContainerWidget m_lookContainerWidget;

	public RectangleWidget m_moveRectangleWidget;

	public RectangleWidget m_lookRectangleWidget;

	public ContainerWidget m_moveRectangleContainerWidget;

	public ContainerWidget m_lookRectangleContainerWidget;

	public ContainerWidget m_movePadContainerWidget;

	public ContainerWidget m_lookPadContainerWidget;

	public ContainerWidget m_moveButtonsContainerWidget;

	public ContainerWidget m_modalPanelContainerWidget;

	public ContainerWidget m_largeMessageWidget;

	public MessageWidget m_messageWidget;

	public ButtonWidget m_backButtonWidget;

	public ButtonWidget m_inventoryButtonWidget;

	public ButtonWidget m_clothingButtonWidget;

	public ButtonWidget m_moreButtonWidget;

	public Widget m_moreContentsWidget;

	public ButtonWidget m_lightningButtonWidget;

	public ButtonWidget m_photoButtonWidget;

	public ButtonWidget m_helpButtonWidget;

	public ButtonWidget m_timeOfDayButtonWidget;

	public ButtonWidget m_cameraButtonWidget;

	public ButtonWidget m_creativeFlyButtonWidget;

	public ButtonWidget m_sneakButtonWidget;

	public ButtonWidget m_mountButtonWidget;

	public ButtonWidget m_editItemButton;

	public float m_sidePanelsFactor;

	public ModalPanelAnimationData m_modalPanelAnimationData;

	public Message m_message;

	public KeyboardHelpDialog m_keyboardHelpDialog;

	public GamepadHelpDialog m_gamepadHelpDialog;

	public double m_lastMountableCreatureSearchTime;

	public bool m_keyboardHelpMessageShown;

	public bool m_gamepadHelpMessageShown;

	public LabelWidget CloseTimeLable;

	public ButtonWidget m_weatherControlButtonWidget;

	public Widget m_weatherControlMenuWidget;

	public ButtonWidget m_weatherWindButtonWidget;

	public ButtonWidget m_weatherRainButtonWidget;

	public ButtonWidget m_weatherSeasonNextButtonWidget;

	public ButtonWidget m_weatherGustinessButtonWidget;

	public ButtonWidget m_weatherCycloneButtonWidget;

	public SubsystemWeather m_subsystemWeather;

	public SubsystemPlayers m_subsystemPlayers;

	private const float CycloneWindThreshold = 14f;

	public ButtonWidget m_creativeFlyBoostButtonWidget;

	public float m_defaultFlySpeed;

	public bool m_flyBoostActive;

	public HashSet<string> m_availableSeasonIcons;

	public string m_lastSeasonIconKey;

	public ButtonWidget m_timeControlButtonWidget;

	public Widget m_timeControlMenuWidget;

	public ButtonWidget m_timeHalfButtonWidget;

	public ButtonWidget m_time1xButtonWidget;

	public ButtonWidget m_time2xButtonWidget;

	public ButtonWidget m_time4xButtonWidget;

	private float m_timeScaleUi;

	public CanvasWidget m_timeControlContainerWidget;

	public ButtonWidget m_weatherDrizzleButtonWidget;

	public ButtonWidget m_weatherThunderButtonWidget;

	public ButtonWidget m_weatherBreezeButtonWidget;

	public ButtonWidget m_weatherHurricaneButtonWidget;

	private StackPanelWidget m_weatherWindRowWidget;

	private StackPanelWidget m_weatherRainRowWidget;

	public CanvasWidget m_weatherControlContainerWidget;

	public ButtonWidget m_worldEditMenuButton;

	public Widget m_worldEditMenuContainerTop;

	public Widget m_worldEditMenuContainerBottom;

	private ButtonWidget m_buildSpamButtonWidget;

	public SubsystemBags m_subsystemBags;

	private BagPreviewWidget m_bagPreviewWidget;

	public ButtonWidget m_pickBlockButtonWidget;

	public ContainerWidget ControlsContainerWidget { get; set; }

	public TouchInputWidget ViewWidget { get; set; }

	public TouchInputWidget MoveWidget { get; set; }

	public MoveRoseWidget MoveRoseWidget { get; set; }

	public TouchInputWidget LookWidget { get; set; }

	public ShortInventoryWidget ShortInventoryWidget { get; set; }

	public ValueBarWidget FoodBarWidget { get; set; }

	public ValueBarWidget TemperatureBarWidget { get; set; }

	public LabelWidget LevelLabelWidget { get; set; }

	public Widget ModalPanelWidget
	{
		get
		{
			if (m_modalPanelContainerWidget.Children.Count <= 0)
			{
				return null;
			}
			return m_modalPanelContainerWidget.Children[0];
		}
		set
		{
			if (value != ModalPanelWidget)
			{
				if (m_modalPanelAnimationData != null)
				{
					EndModalPanelAnimation();
				}
				m_modalPanelAnimationData = new ModalPanelAnimationData
				{
					OldWidget = ModalPanelWidget,
					NewWidget = value
				};
				if (value != null)
				{
					value.HorizontalAlignment = WidgetAlignment.Center;
					m_modalPanelContainerWidget.Children.Insert(0, value);
				}
				UpdateModalPanelAnimation();
				m_componentPlayer.GameWidget.Input.Clear();
				m_componentPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(null, -1);
			}
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public GridValueBarWidget HealthBarWidget { get; set; }

	public void DisplayLargeMessage(string largeText, string smallText, float duration, float delay)
	{
		m_message = new Message
		{
			LargeText = largeText,
			SmallText = smallText,
			Duration = duration,
			StartTime = Time.RealTime + (double)delay
		};
	}

	public void DisplaySmallMessage(string text, Color color, bool blinking, bool playNotificationSound)
	{
		m_messageWidget.DisplayMessage(text.Replace("\u200b", ""), color, blinking);
		if (CommonLib.WorkType != WorkType.Local)
		{
			playNotificationSound = m_componentPlayer.PlayerData.IsMainPlayer;
		}
		if (playNotificationSound && text.EndsWith("\u200b"))
		{
			m_subsystemAudio.PlaySound("Audio/UI/Sms", 1f, 0f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
		}
	}

	public bool IsGameMenuDialogVisible()
	{
		foreach (Dialog dialog in DialogsManager.Dialogs)
		{
			if (dialog.ParentWidget == m_componentPlayer.GuiWidget && dialog is GameMenuDialog)
			{
				return true;
			}
		}
		return false;
	}

	public void Update(float dt)
	{
		if (CloseTime > 0)
		{
			CloseTimeLable.IsVisible = true;
			CloseTimeLable.Text = "Time until server shutdown" + CloseTime + "seconds";
		}
		if (Time.PeriodicEvent(1.0, 0.9))
		{
			CloseTime--;
		}
		if (CloseTimeLable.IsVisible && CloseTime == 0)
		{
			CommonLib.Net.StopImmediate();
		}
		HandleInput();
		UpdateWidgets();
		UpdateBagTooltip();
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		ContainerWidget num = m_componentPlayer.GuiWidget;
		num.Children.Insert(0, CloseTimeLable);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_backButtonWidget = num.Children.Find<ButtonWidget>("BackButton");
		m_inventoryButtonWidget = num.Children.Find<ButtonWidget>("InventoryButton");
		m_clothingButtonWidget = num.Children.Find<ButtonWidget>("ClothingButton");
		m_pickBlockButtonWidget = num.Children.Find<ButtonWidget>("PickBlockButton");
		m_buildSpamButtonWidget = num.Children.Find<ButtonWidget>("BuildSpamButton");
		m_moreButtonWidget = num.Children.Find<ButtonWidget>("MoreButton");
		m_moreContentsWidget = num.Children.Find<Widget>("MoreContents");
		m_subsystemBags = Project.FindSubsystem<SubsystemBags>(throwOnError: true);
		m_helpButtonWidget = num.Children.Find<ButtonWidget>("HelpButton");
		m_photoButtonWidget = num.Children.Find<ButtonWidget>("PhotoButton");
		m_lightningButtonWidget = num.Children.Find<ButtonWidget>("LightningButton");
		m_timeOfDayButtonWidget = num.Children.Find<ButtonWidget>("TimeOfDayButton");
		m_cameraButtonWidget = num.Children.Find<ButtonWidget>("CameraButton");
		m_creativeFlyButtonWidget = num.Children.Find<ButtonWidget>("CreativeFlyButton");
		m_creativeFlyBoostButtonWidget = num.Children.Find<ButtonWidget>("CreativeFlyBoostButton");
		m_defaultFlySpeed = m_componentPlayer.ComponentLocomotion.CreativeFlySpeed;
		m_sneakButtonWidget = num.Children.Find<ButtonWidget>("SneakButton");
		m_mountButtonWidget = num.Children.Find<ButtonWidget>("MountButton");
		m_editItemButton = num.Children.Find<ButtonWidget>("EditItemButton");
		MoveWidget = num.Children.Find<TouchInputWidget>("Move");
		MoveRoseWidget = num.Children.Find<MoveRoseWidget>("MoveRose");
		LookWidget = num.Children.Find<TouchInputWidget>("Look");
		ViewWidget = m_componentPlayer.ViewWidget;
		HealthBarWidget = num.Children.Find<GridValueBarWidget>("HealthBar");
		FoodBarWidget = num.Children.Find<ValueBarWidget>("FoodBar");
		TemperatureBarWidget = num.Children.Find<ValueBarWidget>("TemperatureBar");
		LevelLabelWidget = num.Children.Find<LabelWidget>("LevelLabel");
		m_modalPanelContainerWidget = num.Children.Find<ContainerWidget>("ModalPanelContainer");
		ControlsContainerWidget = num.Children.Find<ContainerWidget>("ControlsContainer");
		m_leftControlsContainerWidget = num.Children.Find<ContainerWidget>("LeftControlsContainer");
		m_rightControlsContainerWidget = num.Children.Find<ContainerWidget>("RightControlsContainer");
		m_moveContainerWidget = num.Children.Find<ContainerWidget>("MoveContainer");
		m_lookContainerWidget = num.Children.Find<ContainerWidget>("LookContainer");
		m_moveRectangleWidget = num.Children.Find<RectangleWidget>("MoveRectangle");
		m_lookRectangleWidget = num.Children.Find<RectangleWidget>("LookRectangle");
		m_moveRectangleContainerWidget = num.Children.Find<ContainerWidget>("MoveRectangleContainer");
		m_lookRectangleContainerWidget = num.Children.Find<ContainerWidget>("LookRectangleContainer");
		m_moveRectangleWidget = num.Children.Find<RectangleWidget>("MoveRectangle");
		m_lookRectangleWidget = num.Children.Find<RectangleWidget>("LookRectangle");
		m_movePadContainerWidget = num.Children.Find<ContainerWidget>("MovePadContainer");
		m_lookPadContainerWidget = num.Children.Find<ContainerWidget>("LookPadContainer");
		m_moveButtonsContainerWidget = num.Children.Find<ContainerWidget>("MoveButtonsContainer");
		ShortInventoryWidget = num.Children.Find<ShortInventoryWidget>("ShortInventory");
		m_largeMessageWidget = num.Children.Find<ContainerWidget>("LargeMessage");
		m_messageWidget = num.Children.Find<MessageWidget>("Message");
		m_weatherControlButtonWidget = num.Children.Find<ButtonWidget>("WeatherControlButton");
		m_weatherControlMenuWidget = num.Children.Find<Widget>("WeatherControlMenu");
		m_weatherWindRowWidget = num.Children.Find<StackPanelWidget>("WeatherWindRow");
		m_weatherRainRowWidget = num.Children.Find<StackPanelWidget>("WeatherRainRow");
		m_weatherWindButtonWidget = num.Children.Find<ButtonWidget>("WeatherWindButton");
		m_weatherRainButtonWidget = num.Children.Find<ButtonWidget>("WeatherRainButton");
		m_weatherSeasonNextButtonWidget = num.Children.Find<ButtonWidget>("WeatherSeasonNextButton");
		m_weatherGustinessButtonWidget = num.Children.Find<ButtonWidget>("WeatherGustinessButton");
		m_weatherCycloneButtonWidget = num.Children.Find<ButtonWidget>("WeatherCycloneButton");
		m_keyboardHelpMessageShown = valuesDictionary.GetValue<bool>("KeyboardHelpMessageShown");
		m_keyboardHelpMessageShown = valuesDictionary.GetValue<bool>("KeyboardHelpMessageShown");
		m_gamepadHelpMessageShown = valuesDictionary.GetValue<bool>("GamepadHelpMessageShown");
		m_timeControlContainerWidget = num.Children.Find<CanvasWidget>("TimeControlContainer");
		m_timeControlButtonWidget = num.Children.Find<ButtonWidget>("TimeControlButton");
		m_timeControlMenuWidget = num.Children.Find<Widget>("TimeControlMenu");
		m_timeHalfButtonWidget = num.Children.Find<ButtonWidget>("TimeHalfButton");
		m_time1xButtonWidget = num.Children.Find<ButtonWidget>("Time1xButton");
		m_time2xButtonWidget = num.Children.Find<ButtonWidget>("Time2xButton");
		m_time4xButtonWidget = num.Children.Find<ButtonWidget>("Time4xButton");
		m_weatherDrizzleButtonWidget = num.Children.Find<ButtonWidget>("WeatherDrizzleButton");
		m_weatherThunderButtonWidget = num.Children.Find<ButtonWidget>("WeatherThunderButton");
		m_weatherBreezeButtonWidget = num.Children.Find<ButtonWidget>("WeatherBreezeButton");
		m_weatherHurricaneButtonWidget = num.Children.Find<ButtonWidget>("WeatherHurricaneButton");
		m_weatherControlContainerWidget = num.Children.Find<CanvasWidget>("WeatherControlContainer");
		m_worldEditMenuButton = num.Children.Find<ButtonWidget>("WorldEditMenu");
		m_worldEditMenuContainerTop = num.Children.Find<Widget>("WorldEditMenuContainerTop");
		m_worldEditMenuContainerBottom = num.Children.Find<Widget>("WorldEditMenuContainerBottom");
		if (m_worldEditMenuContainerBottom != null)
		{
			ContainerWidget guiWidget = m_componentPlayer.GuiWidget;
			StackPanelWidget stackPanelWidget = guiWidget.Children.Find<StackPanelWidget>("WorldEditMenuContainerTop");
			StackPanelWidget stackPanelWidget2 = guiWidget.Children.Find<StackPanelWidget>("WorldEditMenuContainer1");
			StackPanelWidget stackPanelWidget3 = guiWidget.Children.Find<StackPanelWidget>("WorldEditMenuContainer2");
			List<Widget> list = new List<Widget>();
			List<Widget> list2 = new List<Widget>();
			List<Widget> list3 = new List<Widget>();
			Dictionary<string, Widget> dictionary = new Dictionary<string, Widget>(StringComparer.Ordinal);
			if (stackPanelWidget != null)
			{
				for (int i = 0; i < stackPanelWidget.Children.Count; i++)
				{
					Widget widget = stackPanelWidget.Children[i];
					list.Add(widget);
					if (!string.IsNullOrEmpty(widget.Name) && !dictionary.ContainsKey(widget.Name))
					{
						dictionary.Add(widget.Name, widget);
					}
				}
			}
			if (stackPanelWidget2 != null)
			{
				for (int j = 0; j < stackPanelWidget2.Children.Count; j++)
				{
					Widget widget2 = stackPanelWidget2.Children[j];
					list2.Add(widget2);
					if (!string.IsNullOrEmpty(widget2.Name) && !dictionary.ContainsKey(widget2.Name))
					{
						dictionary.Add(widget2.Name, widget2);
					}
				}
			}
			if (stackPanelWidget3 != null)
			{
				for (int k = 0; k < stackPanelWidget3.Children.Count; k++)
				{
					Widget widget3 = stackPanelWidget3.Children[k];
					list3.Add(widget3);
					if (!string.IsNullOrEmpty(widget3.Name) && !dictionary.ContainsKey(widget3.Name))
					{
						dictionary.Add(widget3.Name, widget3);
					}
				}
			}
			ContainerWidget containerWidget = ((m_worldEditMenuButton != null) ? m_worldEditMenuButton.ParentWidget : null);
			object[] tag = new object[9] { list, list2, list3, containerWidget, m_worldEditMenuButton, dictionary, -1, -1, -1 };
			m_worldEditMenuContainerBottom.Tag = tag;
			ApplyWorldEditLocalizedIcons(dictionary);
		}
		string[] array = new string[12]
		{
			"Winter", "Spring", "Summer", "Autumn", "EarlySpring", "LateSpring", "EarlySummer", "LateSummer", "EarlyAutumn", "LateAutumn",
			"LateWinter", "Anabiosis"
		};
		foreach (string text in array)
		{
			try
			{
				ContentManager.Get<Subtexture>("Textures/Gui/Seasons/" + text);
				m_availableSeasonIcons.Add(text);
			}
			catch
			{
			}
		}
		UpdateSeasonButtonIcon();
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("KeyboardHelpMessageShown", m_keyboardHelpMessageShown);
		valuesDictionary.SetValue("GamepadHelpMessageShown", m_gamepadHelpMessageShown);
	}

	public override void OnEntityAdded()
	{
		ShortInventoryWidget.AssignComponents(m_componentPlayer.ComponentMiner.Inventory);
	}

	public override void OnEntityRemoved()
	{
		ShortInventoryWidget.AssignComponents(null);
		m_message = null;
	}

	public override void Dispose()
	{
		m_componentPlayer.GuiWidget.Children.Remove(CloseTimeLable);
		ModalPanelWidget = null;
		m_keyboardHelpDialog = null;
		if (ShortInventoryWidget != null)
		{
			ShortInventoryWidget.AssignComponents(null);
		}
	}

	public void UpdateSidePanelsAnimation()
	{
		float num = MathUtils.Min(Time.FrameDuration, 0.1f);
		bool flag = ModalPanelWidget != null && (m_modalPanelAnimationData == null || m_modalPanelAnimationData.NewWidget != null);
		float num2 = ((!m_componentPlayer.ComponentInput.IsControlledByTouch && !flag) ? 1 : 0);
		float x = num2 - m_sidePanelsFactor;
		if (MathUtils.Abs(x) > 0.01f)
		{
			m_sidePanelsFactor += MathUtils.Clamp(12f * MathUtils.PowSign(x, 0.75f) * num, 0f - MathUtils.Abs(x), MathUtils.Abs(x));
		}
		else
		{
			m_sidePanelsFactor = num2;
		}
		m_leftControlsContainerWidget.RenderTransform = Matrix.CreateTranslation(m_leftControlsContainerWidget.ActualSize.X * (0f - m_sidePanelsFactor), 0f, 0f);
		m_rightControlsContainerWidget.RenderTransform = Matrix.CreateTranslation(m_rightControlsContainerWidget.ActualSize.X * m_sidePanelsFactor, 0f, 0f);
	}

	public void UpdateModalPanelAnimation()
	{
		m_modalPanelAnimationData.Factor += 6f * MathUtils.Min(Time.FrameDuration, 0.1f);
		if (m_modalPanelAnimationData.Factor < 1f)
		{
			float factor = m_modalPanelAnimationData.Factor;
			float num = 0.5f + 0.5f * MathUtils.Pow(1f - factor, 0.1f);
			float num2 = 0.5f + 0.5f * MathUtils.Pow(factor, 0.1f);
			float num3 = 1f - factor;
			float num4 = factor;
			if (m_modalPanelAnimationData.OldWidget != null)
			{
				Vector2 actualSize = m_modalPanelAnimationData.OldWidget.ActualSize;
				m_modalPanelAnimationData.OldWidget.ColorTransform = Color.White * num3;
				m_modalPanelAnimationData.OldWidget.RenderTransform = Matrix.CreateTranslation((0f - actualSize.X) / 2f, (0f - actualSize.Y) / 2f, 0f) * Matrix.CreateScale(num, num, 1f) * Matrix.CreateTranslation(actualSize.X / 2f, actualSize.Y / 2f, 0f);
			}
			if (m_modalPanelAnimationData.NewWidget != null)
			{
				Vector2 actualSize2 = m_modalPanelAnimationData.NewWidget.ActualSize;
				m_modalPanelAnimationData.NewWidget.ColorTransform = Color.White * num4;
				m_modalPanelAnimationData.NewWidget.RenderTransform = Matrix.CreateTranslation((0f - actualSize2.X) / 2f, (0f - actualSize2.Y) / 2f, 0f) * Matrix.CreateScale(num2, num2, 1f) * Matrix.CreateTranslation(actualSize2.X / 2f, actualSize2.Y / 2f, 0f);
			}
		}
		else
		{
			EndModalPanelAnimation();
		}
	}

	public void EndModalPanelAnimation()
	{
		if (m_modalPanelAnimationData.OldWidget != null)
		{
			m_modalPanelContainerWidget.Children.Remove(m_modalPanelAnimationData.OldWidget);
		}
		if (m_modalPanelAnimationData.NewWidget != null)
		{
			m_modalPanelAnimationData.NewWidget.ColorTransform = Color.White;
			m_modalPanelAnimationData.NewWidget.RenderTransform = Matrix.Identity;
		}
		m_modalPanelAnimationData = null;
	}

	public void UpdateWidgets()
	{
		ComponentRider componentRider = m_componentPlayer.ComponentRider;
		ComponentSleep componentSleep = m_componentPlayer.ComponentSleep;
		ComponentInput componentInput = m_componentPlayer.ComponentInput;
		WorldSettings worldSettings = m_subsystemGameInfo.WorldSettings;
		GameMode gameMode = worldSettings.GameMode;
		UpdateSidePanelsAnimation();
		if (m_modalPanelAnimationData != null)
		{
			UpdateModalPanelAnimation();
		}
		if (m_componentPlayer.ComponentVitalStats.Food > 0.75f)
		{
			FoodBarWidget.LitBarColor = new Color(140, 212, 212);
		}
		else if (m_componentPlayer.ComponentVitalStats.Food <= 0.1f)
		{
			FoodBarWidget.LitBarColor = new Color(255, 158, 177);
		}
		else
		{
			FoodBarWidget.LitBarColor = new Color(140, 158, 177);
		}
		if (m_message != null)
		{
			double realTime = Time.RealTime;
			m_largeMessageWidget.IsVisible = true;
			LabelWidget labelWidget = m_largeMessageWidget.Children.Find<LabelWidget>("LargeLabel");
			LabelWidget labelWidget2 = m_largeMessageWidget.Children.Find<LabelWidget>("SmallLabel");
			labelWidget.Text = m_message.LargeText;
			labelWidget2.Text = m_message.SmallText;
			labelWidget.IsVisible = !string.IsNullOrEmpty(m_message.LargeText);
			labelWidget2.IsVisible = !string.IsNullOrEmpty(m_message.SmallText);
			float num = (float)MathUtils.Min(MathUtils.Saturate(2.0 * (realTime - m_message.StartTime)), MathUtils.Saturate(2.0 * (m_message.StartTime + (double)m_message.Duration - realTime)));
			labelWidget.Color = new Color(num, num, num, num);
			labelWidget2.Color = new Color(num, num, num, num);
			if (Time.RealTime > m_message.StartTime + (double)m_message.Duration)
			{
				m_message = null;
			}
		}
		else
		{
			m_largeMessageWidget.IsVisible = false;
		}
		float num2 = 1f - MathUtils.Saturate(SettingsManager.UITransparent);
		Color colorTransform = Color.White * num2;
		if (ControlsContainerWidget != null)
		{
			ControlsContainerWidget.ColorTransform = colorTransform;
		}
		if (m_leftControlsContainerWidget != null)
		{
			m_leftControlsContainerWidget.ColorTransform = Color.White;
		}
		if (m_rightControlsContainerWidget != null)
		{
			m_rightControlsContainerWidget.ColorTransform = Color.White;
		}
		if (m_moreContentsWidget != null)
		{
			m_moreContentsWidget.ColorTransform = Color.White;
		}
		if (m_weatherControlContainerWidget != null)
		{
			m_weatherControlContainerWidget.ColorTransform = Color.White;
		}
		if (m_weatherControlMenuWidget != null)
		{
			m_weatherControlMenuWidget.ColorTransform = Color.White;
		}
		if (m_timeControlContainerWidget != null)
		{
			m_timeControlContainerWidget.ColorTransform = Color.White;
		}
		if (m_moveRectangleContainerWidget != null)
		{
			m_moveRectangleContainerWidget.ColorTransform = Color.White;
		}
		if (m_lookRectangleContainerWidget != null)
		{
			m_lookRectangleContainerWidget.ColorTransform = Color.White;
		}
		if (m_movePadContainerWidget != null)
		{
			m_movePadContainerWidget.ColorTransform = Color.White;
		}
		if (m_lookPadContainerWidget != null)
		{
			m_lookPadContainerWidget.ColorTransform = Color.White;
		}
		if (m_moveButtonsContainerWidget != null)
		{
			m_moveButtonsContainerWidget.ColorTransform = Color.White;
		}
		if (m_worldEditMenuContainerTop != null)
		{
			m_worldEditMenuContainerTop.ColorTransform = Color.White;
		}
		if (m_worldEditMenuContainerBottom != null)
		{
			m_worldEditMenuContainerBottom.ColorTransform = Color.White;
		}
		if (m_buildSpamButtonWidget != null)
		{
			m_buildSpamButtonWidget.IsChecked = false;
			if (m_buildSpamButtonWidget is BitmapButtonWidget bitmapButtonWidget)
			{
				if (SettingsManager.BuildSpamEnabled)
				{
					bitmapButtonWidget.NormalSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/With_spam");
					bitmapButtonWidget.ClickedSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/With_spam_Pressed");
				}
				else
				{
					bitmapButtonWidget.NormalSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/Without_spam");
					bitmapButtonWidget.ClickedSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/Without_spam_Pressed");
				}
			}
		}
		ControlsContainerWidget.IsVisible = m_componentPlayer.PlayerData.IsReadyForPlaying && m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled && componentSleep.SleepFactor <= 0f;
		m_moveRectangleContainerWidget.IsVisible = !SettingsManager.HideMoveLookPads && componentInput.IsControlledByTouch;
		m_lookRectangleContainerWidget.IsVisible = !SettingsManager.HideMoveLookPads && componentInput.IsControlledByTouch && (SettingsManager.LookControlMode != LookControlMode.EntireScreen || SettingsManager.MoveControlMode != MoveControlMode.Buttons);
		m_lookPadContainerWidget.IsVisible = SettingsManager.LookControlMode != LookControlMode.SplitTouch;
		MoveRoseWidget.IsVisible = componentInput.IsControlledByTouch;
		m_moreContentsWidget.IsVisible = m_moreButtonWidget.IsChecked;
		HealthBarWidget.IsVisible = gameMode > GameMode.Creative;
		FoodBarWidget.IsVisible = gameMode != GameMode.Creative && worldSettings.AreAdventureSurvivalMechanicsEnabled;
		TemperatureBarWidget.IsVisible = gameMode != GameMode.Creative && worldSettings.AreAdventureSurvivalMechanicsEnabled;
		LevelLabelWidget.IsVisible = gameMode != GameMode.Creative && worldSettings.AreAdventureSurvivalMechanicsEnabled;
		m_creativeFlyButtonWidget.IsVisible = gameMode == GameMode.Creative;
		m_lightningButtonWidget.IsVisible = gameMode == GameMode.Creative;
		m_moveButtonsContainerWidget.IsVisible = SettingsManager.MoveControlMode == MoveControlMode.Buttons;
		m_movePadContainerWidget.IsVisible = SettingsManager.MoveControlMode == MoveControlMode.Pad;
		if (SettingsManager.LeftHandedLayout)
		{
			m_moveContainerWidget.HorizontalAlignment = WidgetAlignment.Far;
			m_lookContainerWidget.HorizontalAlignment = WidgetAlignment.Near;
			m_moveRectangleWidget.FlipHorizontal = true;
			m_lookRectangleWidget.FlipHorizontal = false;
		}
		else
		{
			m_moveContainerWidget.HorizontalAlignment = WidgetAlignment.Near;
			m_lookContainerWidget.HorizontalAlignment = WidgetAlignment.Far;
			m_moveRectangleWidget.FlipHorizontal = false;
			m_lookRectangleWidget.FlipHorizontal = true;
		}
		m_sneakButtonWidget.IsChecked = m_componentPlayer.ComponentBody.IsSneaking;
		m_creativeFlyButtonWidget.IsChecked = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
		ComponentLocomotion componentLocomotion = m_componentPlayer.ComponentLocomotion;
		bool isCreativeFlyEnabled = componentLocomotion.IsCreativeFlyEnabled;
		m_creativeFlyBoostButtonWidget.IsVisible = ((gameMode == GameMode.Creative) & isCreativeFlyEnabled) && ControlsContainerWidget.IsVisible;
		if (m_defaultFlySpeed <= 0f)
		{
			m_defaultFlySpeed = componentLocomotion.CreativeFlySpeed;
		}
		if (isCreativeFlyEnabled)
		{
			if (!m_flyBoostActive && componentLocomotion.CreativeFlySpeed >= m_defaultFlySpeed * 5.5f)
			{
				m_flyBoostActive = true;
			}
			m_creativeFlyBoostButtonWidget.IsChecked = m_flyBoostActive && m_defaultFlySpeed * 6f - componentLocomotion.CreativeFlySpeed < 0.001f;
		}
		else
		{
			m_creativeFlyBoostButtonWidget.IsChecked = false;
		}
		m_inventoryButtonWidget.IsChecked = IsInventoryVisible();
		m_clothingButtonWidget.IsChecked = IsClothingVisible();
		if (m_pickBlockButtonWidget != null)
		{
			m_pickBlockButtonWidget.IsVisible = componentInput.IsControlledByTouch && gameMode == GameMode.Creative;
		}
		if (IsActiveSlotEditable() || m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.HasValue)
		{
			m_sneakButtonWidget.IsVisible = false;
			m_mountButtonWidget.IsVisible = false;
			m_editItemButton.IsVisible = true;
		}
		else if (componentRider != null && componentRider.Mount != null)
		{
			m_sneakButtonWidget.IsVisible = false;
			m_mountButtonWidget.IsChecked = true;
			m_mountButtonWidget.IsVisible = true;
			m_editItemButton.IsVisible = false;
		}
		else
		{
			m_mountButtonWidget.IsChecked = false;
			if (componentRider != null && Time.FrameStartTime - m_lastMountableCreatureSearchTime > 0.5)
			{
				m_lastMountableCreatureSearchTime = Time.FrameStartTime;
				if (componentRider.FindNearestMount() != null)
				{
					m_sneakButtonWidget.IsVisible = false;
					m_mountButtonWidget.IsVisible = true;
					m_editItemButton.IsVisible = false;
				}
				else
				{
					m_sneakButtonWidget.IsVisible = true;
					m_mountButtonWidget.IsVisible = false;
					m_editItemButton.IsVisible = false;
				}
			}
		}
		if (!m_componentPlayer.IsAddedToProject || m_componentPlayer.ComponentHealth.Health == 0f || componentSleep.IsSleeping || m_componentPlayer.ComponentSickness.IsPuking)
		{
			ModalPanelWidget = null;
		}
		if (m_componentPlayer.ComponentSickness.IsSick)
		{
			HealthBarWidget.LitBarColor = new Color(166, 175, 103);
		}
		else
		{
			HealthBarWidget.LitBarColor = (m_componentPlayer.ComponentFlu.HasFlu ? new Color(0, 158, 255) : new Color(224, 24, 0));
		}
		if (m_subsystemWeather != null)
		{
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			bool isChecked = (totalElapsedGameTime >= m_subsystemWeather.WindStartTime && totalElapsedGameTime <= m_subsystemWeather.WindEndTime) || m_subsystemWeather.CurrentWindStrength > 0.1f;
			if (m_weatherRainButtonWidget != null)
			{
				m_weatherRainButtonWidget.IsChecked = m_subsystemWeather.IsRainActive();
			}
			if (m_weatherWindButtonWidget != null)
			{
				m_weatherWindButtonWidget.IsChecked = isChecked;
			}
			if (m_weatherGustinessButtonWidget != null)
			{
				m_weatherGustinessButtonWidget.IsChecked = m_subsystemWeather.IsGustyWind;
			}
			if (m_weatherCycloneButtonWidget != null)
			{
				m_weatherCycloneButtonWidget.IsChecked = m_subsystemWeather.IsCycloneActive;
			}
			SubsystemWeather subsystemWeather = m_subsystemWeather;
			bool flag = subsystemWeather.IsRainActive();
			if (m_weatherDrizzleButtonWidget != null)
			{
				m_weatherDrizzleButtonWidget.IsChecked = flag && subsystemWeather.TargetLightningIntensity >= 0f && subsystemWeather.TargetLightningIntensity <= 0.33f;
			}
			if (m_weatherThunderButtonWidget != null)
			{
				m_weatherThunderButtonWidget.IsChecked = flag && subsystemWeather.TargetLightningIntensity >= 0.8f && subsystemWeather.TargetLightningIntensity <= 1f;
			}
			double totalElapsedGameTime2 = m_subsystemGameInfo.TotalElapsedGameTime;
			bool flag2 = (totalElapsedGameTime2 >= subsystemWeather.WindStartTime && totalElapsedGameTime2 <= subsystemWeather.WindEndTime) || subsystemWeather.CurrentWindStrength > 0.1f;
			if (m_weatherBreezeButtonWidget != null)
			{
				m_weatherBreezeButtonWidget.IsChecked = flag2 && subsystemWeather.CurrentWindStrength >= 1f && subsystemWeather.CurrentWindStrength <= 6f;
			}
			if (m_weatherHurricaneButtonWidget != null)
			{
				m_weatherHurricaneButtonWidget.IsChecked = flag2 && subsystemWeather.CurrentWindStrength >= 12f && subsystemWeather.CurrentWindStrength <= 16f;
			}
		}
		if (m_weatherControlContainerWidget != null)
		{
			bool flag3 = HasServerManagementRights();
			bool flag4 = ((gameMode == GameMode.Creative) & flag3) && worldSettings.AreWeatherEffectsEnabled;
			m_weatherControlContainerWidget.IsVisible = flag4;
			if (!flag4 && m_weatherControlMenuWidget != null)
			{
				m_weatherControlMenuWidget.IsVisible = false;
			}
		}
		if (m_timeOfDayButtonWidget != null)
		{
			m_timeOfDayButtonWidget.IsVisible = gameMode == GameMode.Creative && HasServerManagementRights();
		}
		if (!ControlsContainerWidget.IsVisible)
		{
			if (m_worldEditMenuContainerTop != null)
			{
				m_worldEditMenuContainerTop.IsVisible = false;
			}
			if (m_worldEditMenuContainerBottom != null)
			{
				m_worldEditMenuContainerBottom.IsVisible = false;
			}
			if (m_worldEditMenuButton != null)
			{
				m_worldEditMenuButton.IsChecked = false;
			}
		}
		if (CommonLib.WorkType != WorkType.Local)
		{
			bool? flag5 = m_componentPlayer?.PlayerData?.ServerManager;
			bool? flag6 = flag5;
		}
		if (m_timeControlContainerWidget != null)
		{
			bool flag7 = gameMode == GameMode.Creative && HasServerManagementRights();
			m_timeControlContainerWidget.IsVisible = flag7;
			if (!flag7 && m_timeControlMenuWidget != null)
			{
				m_timeControlMenuWidget.IsVisible = false;
			}
		}
		SubsystemTime subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_timeScaleUi = subsystemTime.TimeControlFactor;
		if (m_timeHalfButtonWidget != null)
		{
			m_timeHalfButtonWidget.IsChecked = m_timeScaleUi <= 0.75f;
		}
		if (m_time1xButtonWidget != null)
		{
			m_time1xButtonWidget.IsChecked = m_timeScaleUi > 0.75f && m_timeScaleUi < 1.5f;
		}
		if (m_time2xButtonWidget != null)
		{
			m_time2xButtonWidget.IsChecked = m_timeScaleUi >= 1.5f && m_timeScaleUi < 3f;
		}
		if (m_time4xButtonWidget != null)
		{
			m_time4xButtonWidget.IsChecked = m_timeScaleUi >= 3f;
		}
		UpdateSeasonButtonIcon();
		CenterWeatherRows();
		bool isWorldEditIntegrated = worldSettings.IsWorldEditIntegrated;
		bool flag8 = false;
		if (isWorldEditIntegrated)
		{
			if (!worldSettings.WEManagersOnly)
			{
				flag8 = true;
			}
			else if (m_componentPlayer != null)
			{
				PlayerData playerData = m_componentPlayer.PlayerData;
				if (playerData != null)
				{
					flag8 = playerData.WorldEditManager;
				}
			}
		}
		if (m_worldEditMenuButton != null)
		{
			m_worldEditMenuButton.IsVisible = isWorldEditIntegrated;
			if (!flag8)
			{
				m_worldEditMenuButton.IsChecked = false;
			}
		}
		bool isVisible = (isWorldEditIntegrated & flag8) && m_worldEditMenuButton != null && m_worldEditMenuButton.IsChecked;
		if (m_worldEditMenuContainerTop != null)
		{
			m_worldEditMenuContainerTop.IsVisible = isVisible;
		}
		if (m_worldEditMenuContainerBottom != null)
		{
			m_worldEditMenuContainerBottom.IsVisible = isVisible;
		}
		object[] array = ((m_worldEditMenuContainerBottom != null) ? (m_worldEditMenuContainerBottom.Tag as object[]) : null);
		Dictionary<string, Widget> dictionary = ((array != null && array.Length >= 6) ? (array[5] as Dictionary<string, Widget>) : null);
		if (dictionary == null)
		{
			return;
		}
		if (gameMode > GameMode.Creative)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { "ButtonOverlay", "ButtonMirrorVertical", "ButtonMirrorHorizontal", "ButtonRotationVertical", "ButtonRotationHorizontal", "F1", "F2", "F3", "F9", "F11" };
			{
				foreach (KeyValuePair<string, Widget> item in dictionary)
				{
					if (item.Value is ButtonWidget buttonWidget)
					{
						bool isVisible2 = hashSet.Contains(item.Key);
						buttonWidget.IsVisible = isVisible2;
					}
				}
				return;
			}
		}
		foreach (KeyValuePair<string, Widget> item2 in dictionary)
		{
			if (item2.Value is ButtonWidget buttonWidget2)
			{
				buttonWidget2.IsVisible = true;
			}
		}
	}

	public void HandleInput()
	{
		WorldSettings worldSettings = ((m_subsystemGameInfo != null) ? m_subsystemGameInfo.WorldSettings : null);
		GameMode gameMode = worldSettings?.GameMode ?? GameMode.Creative;
		WidgetInput input = m_componentPlayer.GameWidget.Input;
		PlayerInput playerInput = m_componentPlayer.ComponentInput.PlayerInput;
		ComponentRider componentRider = m_componentPlayer.ComponentRider;
		if (m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled)
		{
			if (!m_keyboardHelpMessageShown && (m_componentPlayer.PlayerData.InputDevice & WidgetInputDevice.Keyboard) != 0 && Time.PeriodicEvent(7.0, 0.0))
			{
				m_keyboardHelpMessageShown = true;
				DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, blinking: true, playNotificationSound: true);
			}
			else if (!m_gamepadHelpMessageShown && (m_componentPlayer.PlayerData.InputDevice & WidgetInputDevice.Gamepads) != 0 && Time.PeriodicEvent(7.0, 0.0))
			{
				m_gamepadHelpMessageShown = true;
				DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, blinking: true, playNotificationSound: true);
			}
		}
		if (playerInput.KeyboardHelp)
		{
			if (m_keyboardHelpDialog == null)
			{
				m_keyboardHelpDialog = new KeyboardHelpDialog();
			}
			if (m_keyboardHelpDialog.ParentWidget != null)
			{
				DialogsManager.HideDialog(m_keyboardHelpDialog);
			}
			else
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, m_keyboardHelpDialog);
			}
		}
		if (playerInput.GamepadHelp)
		{
			if (m_gamepadHelpDialog == null)
			{
				m_gamepadHelpDialog = new GamepadHelpDialog();
			}
			if (m_gamepadHelpDialog.ParentWidget != null)
			{
				DialogsManager.HideDialog(m_gamepadHelpDialog);
			}
			else
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, m_gamepadHelpDialog);
			}
		}
		if (m_helpButtonWidget.IsClicked)
		{
			ScreensManager.SwitchScreen("Help");
		}
		if (playerInput.ToggleInventory || m_inventoryButtonWidget.IsClicked)
		{
			if (IsInventoryVisible())
			{
				ModalPanelWidget = null;
			}
			else
			{
				Widget modalPanelWidget = ((!(m_componentPlayer.ComponentMiner.Inventory is ComponentCreativeInventory)) ? ((CanvasWidget)new FullInventoryWidget(m_componentPlayer.ComponentMiner.Inventory, m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true))) : ((CanvasWidget)new CreativeInventoryWidget(m_componentPlayer.Entity)));
				ModalPanelWidget = modalPanelWidget;
			}
		}
		if (playerInput.ToggleClothing || m_clothingButtonWidget.IsClicked)
		{
			ModalPanelWidget = (IsClothingVisible() ? null : new ClothingWidget(m_componentPlayer));
		}
		if (m_sneakButtonWidget.IsClicked || playerInput.ToggleSneak)
		{
			bool isSneaking = m_componentPlayer.ComponentBody.IsSneaking;
			m_componentPlayer.ComponentBody.IsSneaking = !isSneaking;
			if (m_componentPlayer.ComponentBody.IsSneaking != isSneaking)
			{
				if (m_componentPlayer.ComponentBody.IsSneaking)
				{
					DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					DisplaySmallMessage(LanguageControl.Get(fName, 4), Color.White, blinking: false, playNotificationSound: false);
				}
			}
		}
		if (componentRider != null && (m_mountButtonWidget.IsClicked || playerInput.ToggleMount))
		{
			bool flag = componentRider.Mount != null;
			if (flag)
			{
				componentRider.StartDismounting();
			}
			else
			{
				ComponentMount componentMount = componentRider.FindNearestMount();
				if (componentMount != null)
				{
					componentRider.StartMounting(componentMount);
				}
			}
			if (componentRider.Mount != null != flag)
			{
				if (componentRider.Mount != null)
				{
					DisplaySmallMessage(LanguageControl.Get(fName, 5), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					DisplaySmallMessage(LanguageControl.Get(fName, 6), Color.White, blinking: false, playNotificationSound: false);
				}
			}
		}
		if (m_buildSpamButtonWidget != null && m_buildSpamButtonWidget.IsClicked)
		{
			SettingsManager.BuildSpamEnabled = !SettingsManager.BuildSpamEnabled;
			string text = LanguageControl.Get(fName, SettingsManager.BuildSpamEnabled ? "buildspam_on" : "buildspam_off");
			DisplaySmallMessage(text, Color.White, blinking: false, playNotificationSound: false);
		}
		if ((m_editItemButton.IsClicked || playerInput.EditItem) && m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.HasValue)
		{
			Point3 value = m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.Value;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(value.X, value.Y, value.Z);
			int contents = Terrain.ExtractContents(cellValue);
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents, m_componentPlayer.ComponentMiner, value);
			for (int i = 0; i < blockBehaviors.Length && !blockBehaviors[i].OnEditBlock(value.X, value.Y, value.Z, cellValue, m_componentPlayer); i++)
			{
			}
		}
		else if ((m_editItemButton.IsClicked || playerInput.EditItem) && IsActiveSlotEditable())
		{
			IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
			if (inventory != null)
			{
				int activeSlotIndex = inventory.ActiveSlotIndex;
				int num = Terrain.ExtractContents(inventory.GetSlotValue(activeSlotIndex));
				if (BlocksManager.Blocks[num].IsEditable)
				{
					SubsystemBlockBehavior[] blockBehaviors2 = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
					for (int j = 0; j < blockBehaviors2.Length && !blockBehaviors2[j].OnEditInventoryItem(inventory, activeSlotIndex, m_componentPlayer); j++)
					{
					}
				}
			}
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && (m_creativeFlyButtonWidget.IsClicked || playerInput.ToggleCreativeFly) && componentRider.Mount == null)
		{
			bool isCreativeFlyEnabled = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
			bool isCreativeFlyEnabled2 = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
			if ((!isCreativeFlyEnabled & isCreativeFlyEnabled2) && m_flyBoostActive)
			{
				if (m_defaultFlySpeed <= 0f)
				{
					m_defaultFlySpeed = m_componentPlayer.ComponentLocomotion.CreativeFlySpeed;
				}
				m_componentPlayer.ComponentLocomotion.CreativeFlySpeed = m_defaultFlySpeed * 6f;
			}
			m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled = !isCreativeFlyEnabled;
			if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled != isCreativeFlyEnabled)
			{
				if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled)
				{
					m_componentPlayer.ComponentLocomotion.JumpOrder = 1f;
					DisplaySmallMessage(LanguageControl.Get(fName, 7), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					DisplaySmallMessage(LanguageControl.Get(fName, 8), Color.White, blinking: false, playNotificationSound: false);
				}
			}
			CommonLib.Net.QueuePackage(new ComponentPlayerPackage(m_componentPlayer, ComponentPlayerPackage.PlayerAction.CreativeFlyChange));
		}
		if (!m_componentPlayer.ComponentInput.IsControlledByVr && (m_cameraButtonWidget.IsClicked || playerInput.SwitchCameraMode))
		{
			GameWidget gameWidget = m_componentPlayer.GameWidget;
			if (gameWidget.ActiveCamera.GetType() == typeof(FppCamera))
			{
				gameWidget.ActiveCamera = gameWidget.FindCamera<TppCamera>();
				DisplaySmallMessage(LanguageControl.Get(fName, 9), Color.White, blinking: false, playNotificationSound: false);
			}
			else if (gameWidget.ActiveCamera.GetType() == typeof(TppCamera))
			{
				gameWidget.ActiveCamera = gameWidget.FindCamera<OrbitCamera>();
				DisplaySmallMessage(LanguageControl.Get(fName, 10), Color.White, blinking: false, playNotificationSound: false);
			}
			else if (gameWidget.ActiveCamera.GetType() == typeof(OrbitCamera))
			{
				gameWidget.ActiveCamera = gameWidget.FindCamera<FixedCamera>();
				DisplaySmallMessage(LanguageControl.Get(fName, 11), Color.White, blinking: false, playNotificationSound: false);
			}
			else
			{
				gameWidget.ActiveCamera = gameWidget.FindCamera<FppCamera>();
				DisplaySmallMessage(LanguageControl.Get(fName, 12), Color.White, blinking: false, playNotificationSound: false);
			}
		}
		if (m_photoButtonWidget.IsClicked || playerInput.TakeScreenshot)
		{
			ScreenCaptureManager.CapturePhoto(() =>
			{
				DisplaySmallMessage(LanguageControl.Get(fName, 13), Color.White, blinking: false, playNotificationSound: false);
			}, delegate
			{
				DisplaySmallMessage(LanguageControl.Get(fName, 14), Color.White, blinking: false, playNotificationSound: false);
			});
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && HasServerManagementRights() && m_weatherControlButtonWidget != null && m_weatherControlButtonWidget.IsClicked && m_weatherControlMenuWidget != null)
		{
			m_weatherControlMenuWidget.IsVisible = !m_weatherControlMenuWidget.IsVisible;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && (m_lightningButtonWidget.IsClicked || playerInput.Lighting))
		{
			Matrix matrix = Matrix.CreateFromQuaternion(m_componentPlayer.ComponentCreatureModel.EyeRotation);
			if (CommonLib.WorkType != WorkType.Client)
			{
				Project.FindSubsystem<SubsystemWeather>(throwOnError: true).ManualLightingStrike(m_componentPlayer.ComponentCreatureModel.EyePosition, matrix.Forward);
			}
			else
			{
				CommonLib.Net.QueuePackage(new SubsystemSkyPackage(m_componentPlayer.ComponentCreatureModel.EyePosition, matrix.Forward));
			}
		}
		if (m_worldEditMenuButton != null && m_worldEditMenuButton.IsClicked)
		{
			if (worldSettings != null && worldSettings.IsWorldEditIntegrated && gameMode != GameMode.Embrace && worldSettings.WEManagersOnly)
			{
				bool flag2 = false;
				if (m_componentPlayer != null)
				{
					PlayerData playerData = m_componentPlayer.PlayerData;
					if (playerData != null)
					{
						flag2 = playerData.WorldEditManager;
					}
				}
				if (!flag2)
				{
					string text2 = LanguageControl.Get(fName, "no_we_manager");
					DisplaySmallMessage(text2, Color.White, blinking: false, playNotificationSound: true);
					return;
				}
			}
			Widget worldEditMenuContainerTop = m_worldEditMenuContainerTop;
			bool flag3 = worldEditMenuContainerTop == null || !worldEditMenuContainerTop.IsVisible;
			if (m_worldEditMenuContainerTop != null)
			{
				m_worldEditMenuContainerTop.IsVisible = flag3;
			}
			if (m_worldEditMenuContainerBottom != null)
			{
				m_worldEditMenuContainerBottom.IsVisible = flag3;
			}
			if (flag3)
			{
				if (m_weatherControlMenuWidget != null)
				{
					m_weatherControlMenuWidget.IsVisible = false;
				}
				if (m_timeControlMenuWidget != null)
				{
					m_timeControlMenuWidget.IsVisible = false;
				}
			}
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && m_subsystemWeather != null && HasServerManagementRights())
		{
			SubsystemWeather subsystemWeather = m_subsystemWeather;
			Random random = subsystemWeather.m_random;
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			if (m_weatherRainButtonWidget.IsClicked)
			{
				if (!subsystemWeather.IsRainActive())
				{
					subsystemWeather.m_precipitationStartTime = totalElapsedGameTime + 0.01;
					float num2 = random.Float(0f, 1f);
					double num3;
					if (num2 < 0.7f)
					{
						num3 = 60.0 * (double)random.Float(5.85f, 11.7f);
					}
					else
					{
						num3 = ((!(num2 < 0.97f)) ? (60.0 * (double)random.Float(80f, 140f)) : (60.0 * (double)random.Float(40f, 80f)));
					}
					if (subsystemWeather.CurrentSeason == SubsystemWeather.Season.EarlyAutumn || subsystemWeather.CurrentSeason == SubsystemWeather.Season.Autumn || subsystemWeather.CurrentSeason == SubsystemWeather.Season.LateAutumn)
					{
						num3 *= 1.2;
					}
					subsystemWeather.m_precipitationEndTime = subsystemWeather.m_precipitationStartTime + num3;
					SubsystemWeather subsystemWeather2 = subsystemWeather;
					float targetLightningIntensity;
					if (subsystemWeather.CurrentSeason == SubsystemWeather.Season.Anabiosis)
					{
						targetLightningIntensity = 1f;
					}
					else
					{
						targetLightningIntensity = ((random.Float(0f, 1f) < 0.5f) ? random.Float(0.33f, 1f) : 0f);
					}
					subsystemWeather2.TargetLightningIntensity = targetLightningIntensity;
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 22), GetCellPrecipKind(subsystemWeather), subsystemWeather.TargetLightningIntensity, subsystemWeather.m_precipitationEndTime - subsystemWeather.m_precipitationStartTime), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					subsystemWeather.m_precipitationEndTime = totalElapsedGameTime - 0.01;
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 23), GetCellPrecipKind(subsystemWeather)), Color.White, blinking: false, playNotificationSound: false);
				}
				m_weatherRainButtonWidget.IsChecked = subsystemWeather.IsRainActive();
			}
			if (m_weatherWindButtonWidget.IsClicked)
			{
				if ((totalElapsedGameTime < subsystemWeather.WindStartTime || totalElapsedGameTime > subsystemWeather.WindEndTime) && subsystemWeather.CurrentWindStrength <= 0.1f)
				{
					subsystemWeather.WindStartTime = totalElapsedGameTime + 0.01;
					double num4 = ((random.Float(0f, 1f) < 0.8f) ? (60.0 * (double)random.Float(13f, 25f)) : (60.0 * (double)random.Float(25f, 60f)));
					subsystemWeather.WindEndTime = subsystemWeather.WindStartTime + num4;
					float min;
					float max;
					switch (m_subsystemGameInfo.WorldSettings.StartingPositionMode)
					{
					case StartingPositionMode.Hard:
						min = 6f;
						max = 14f;
						break;
					case StartingPositionMode.Medium:
						min = 3f;
						max = 10f;
						break;
					default:
						min = 1f;
						max = 7f;
						break;
					}
					subsystemWeather.TargetWindStrength = random.Float(min, max);
					string[] array = new string[8] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
					subsystemWeather.WindDirection = array[random.Int(0, array.Length - 1)];
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 24), subsystemWeather.TargetWindStrength, subsystemWeather.WindDirection, subsystemWeather.IsGustyWind ? LanguageControl.Get(fName, 31) : LanguageControl.Get(fName, 32)), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					subsystemWeather.WindEndTime = totalElapsedGameTime - 0.01;
					subsystemWeather.TargetWindStrength = 0f;
					subsystemWeather.IsGustyWind = false;
					BroadcastWeather();
					DisplaySmallMessage(LanguageControl.Get(fName, 25), Color.White, blinking: false, playNotificationSound: false);
				}
				m_weatherWindButtonWidget.IsChecked = totalElapsedGameTime >= subsystemWeather.WindStartTime && totalElapsedGameTime <= subsystemWeather.WindEndTime;
				m_weatherGustinessButtonWidget.IsChecked = subsystemWeather.IsGustyWind;
			}
			if (m_weatherDrizzleButtonWidget != null && m_weatherDrizzleButtonWidget.IsClicked)
			{
				bool flag4 = subsystemWeather.IsRainActive();
				bool flag5 = flag4 && subsystemWeather.TargetLightningIntensity >= 0f && subsystemWeather.TargetLightningIntensity <= 0.33f;
				if (flag4 & flag5)
				{
					subsystemWeather.m_precipitationEndTime = totalElapsedGameTime - 0.01;
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 23), GetCellPrecipKind(subsystemWeather)), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					float targetLightningIntensity2 = random.Float(0f, 0.33f);
					if (!flag4)
					{
						subsystemWeather.m_precipitationStartTime = totalElapsedGameTime + 0.01;
						float num5 = random.Float(0f, 1f);
						double num6;
						if (num5 < 0.7f)
						{
							num6 = 60.0 * (double)random.Float(5.85f, 11.7f);
						}
						else
						{
							num6 = ((!(num5 < 0.97f)) ? (60.0 * (double)random.Float(80f, 140f)) : (60.0 * (double)random.Float(40f, 80f)));
						}
						if (subsystemWeather.CurrentSeason == SubsystemWeather.Season.EarlyAutumn || subsystemWeather.CurrentSeason == SubsystemWeather.Season.Autumn || subsystemWeather.CurrentSeason == SubsystemWeather.Season.LateAutumn)
						{
							num6 *= 1.2;
						}
						subsystemWeather.m_precipitationEndTime = subsystemWeather.m_precipitationStartTime + num6;
					}
					subsystemWeather.TargetLightningIntensity = targetLightningIntensity2;
					BroadcastWeather();
					double num7 = Math.Max(0.0, subsystemWeather.m_precipitationEndTime - totalElapsedGameTime);
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 22), GetCellPrecipKind(subsystemWeather), subsystemWeather.TargetLightningIntensity, num7), Color.White, blinking: false, playNotificationSound: false);
				}
				m_weatherRainButtonWidget.IsChecked = subsystemWeather.IsRainActive();
			}
			if (m_weatherThunderButtonWidget != null && m_weatherThunderButtonWidget.IsClicked)
			{
				bool flag6 = subsystemWeather.IsRainActive();
				bool flag7 = flag6 && subsystemWeather.TargetLightningIntensity >= 0.8f && subsystemWeather.TargetLightningIntensity <= 1f;
				if (flag6 & flag7)
				{
					subsystemWeather.m_precipitationEndTime = totalElapsedGameTime - 0.01;
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 23), GetCellPrecipKind(subsystemWeather)), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					float targetLightningIntensity3 = random.Float(0.8f, 1f);
					if (!flag6)
					{
						subsystemWeather.m_precipitationStartTime = totalElapsedGameTime + 0.01;
						float num8 = random.Float(0f, 1f);
						double num9;
						if (num8 < 0.7f)
						{
							num9 = 60.0 * (double)random.Float(5.85f, 11.7f);
						}
						else
						{
							num9 = ((!(num8 < 0.97f)) ? (60.0 * (double)random.Float(80f, 140f)) : (60.0 * (double)random.Float(40f, 80f)));
						}
						if (subsystemWeather.CurrentSeason == SubsystemWeather.Season.EarlyAutumn || subsystemWeather.CurrentSeason == SubsystemWeather.Season.Autumn || subsystemWeather.CurrentSeason == SubsystemWeather.Season.LateAutumn)
						{
							num9 *= 1.2;
						}
						subsystemWeather.m_precipitationEndTime = subsystemWeather.m_precipitationStartTime + num9;
					}
					subsystemWeather.TargetLightningIntensity = targetLightningIntensity3;
					BroadcastWeather();
					double num10 = Math.Max(0.0, subsystemWeather.m_precipitationEndTime - totalElapsedGameTime);
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 22), GetCellPrecipKind(subsystemWeather), subsystemWeather.TargetLightningIntensity, num10), Color.White, blinking: false, playNotificationSound: false);
				}
				m_weatherRainButtonWidget.IsChecked = subsystemWeather.IsRainActive();
			}
			if (m_weatherBreezeButtonWidget != null && m_weatherBreezeButtonWidget.IsClicked)
			{
				double num11 = totalElapsedGameTime;
				bool flag8 = (num11 < subsystemWeather.WindEndTime && num11 > subsystemWeather.WindStartTime) || subsystemWeather.CurrentWindStrength > 0.1f;
				bool flag9 = flag8 && subsystemWeather.CurrentWindStrength >= 1f && subsystemWeather.CurrentWindStrength <= 6f;
				if (flag8 & flag9)
				{
					subsystemWeather.WindEndTime = num11 - 0.01;
					subsystemWeather.TargetWindStrength = 0f;
					subsystemWeather.IsGustyWind = false;
					BroadcastWeather();
					DisplaySmallMessage(LanguageControl.Get(fName, 25), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					float targetWindStrength = random.Float(1f, 6f);
					if (!flag8)
					{
						subsystemWeather.WindStartTime = num11 + 0.01;
						double num12 = ((random.Float(0f, 1f) < 0.8f) ? (60.0 * (double)random.Float(13f, 25f)) : (60.0 * (double)random.Float(25f, 60f)));
						subsystemWeather.WindEndTime = subsystemWeather.WindStartTime + num12;
						string[] array2 = new string[8] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
						subsystemWeather.WindDirection = array2[random.Int(0, array2.Length - 1)];
					}
					subsystemWeather.TargetWindStrength = targetWindStrength;
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 24), subsystemWeather.TargetWindStrength, subsystemWeather.WindDirection, subsystemWeather.IsGustyWind ? LanguageControl.Get(fName, 31) : LanguageControl.Get(fName, 32)), Color.White, blinking: false, playNotificationSound: false);
				}
			}
			if (m_weatherHurricaneButtonWidget != null && m_weatherHurricaneButtonWidget.IsClicked)
			{
				double num13 = totalElapsedGameTime;
				bool flag10 = (num13 < subsystemWeather.WindEndTime && num13 > subsystemWeather.WindStartTime) || subsystemWeather.CurrentWindStrength > 0.1f;
				bool flag11 = flag10 && subsystemWeather.CurrentWindStrength >= 12f && subsystemWeather.CurrentWindStrength <= 16f;
				if (flag10 & flag11)
				{
					subsystemWeather.WindEndTime = num13 - 0.01;
					subsystemWeather.TargetWindStrength = 0f;
					subsystemWeather.IsGustyWind = false;
					BroadcastWeather();
					DisplaySmallMessage(LanguageControl.Get(fName, 25), Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					float targetWindStrength2 = random.Float(12f, 16f);
					if (!flag10)
					{
						subsystemWeather.WindStartTime = num13 + 0.01;
						double num14 = ((random.Float(0f, 1f) < 0.8f) ? (60.0 * (double)random.Float(13f, 25f)) : (60.0 * (double)random.Float(25f, 60f)));
						subsystemWeather.WindEndTime = subsystemWeather.WindStartTime + num14;
						string[] array3 = new string[8] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
						subsystemWeather.WindDirection = array3[random.Int(0, array3.Length - 1)];
					}
					subsystemWeather.TargetWindStrength = targetWindStrength2;
					BroadcastWeather();
					DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 24), subsystemWeather.TargetWindStrength, subsystemWeather.WindDirection, subsystemWeather.IsGustyWind ? LanguageControl.Get(fName, 31) : LanguageControl.Get(fName, 32)), Color.White, blinking: false, playNotificationSound: false);
				}
			}
			if (m_weatherGustinessButtonWidget.IsClicked)
			{
				subsystemWeather.IsGustyWind = !subsystemWeather.IsGustyWind;
				BroadcastWeather();
				DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 26), subsystemWeather.IsGustyWind ? LanguageControl.Get(fName, 33) : LanguageControl.Get(fName, 34), subsystemWeather.CurrentWindStrength), Color.White, blinking: false, playNotificationSound: false);
				m_weatherGustinessButtonWidget.IsChecked = subsystemWeather.IsGustyWind;
			}
			if (m_weatherSeasonNextButtonWidget.IsClicked)
			{
				SubsystemWeather.Season s = (subsystemWeather.PreviousSeason = subsystemWeather.CurrentSeason);
				subsystemWeather.CurrentSeason = NextSeason(s);
				subsystemWeather.NextSeasonChangeTime = totalElapsedGameTime + subsystemWeather.SeasonCycleDuration;
				TerrainChunk[] allocatedChunks = subsystemWeather.SubsystemTerrain.Terrain.AllocatedChunks;
				foreach (TerrainChunk terrainChunk in allocatedChunks)
				{
					if (terrainChunk.State >= TerrainChunkState.InvalidVertices1)
					{
						subsystemWeather.SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(terrainChunk.Coords, 0, TerrainChunkState.InvalidLight, forceGeometryRegeneration: true);
					}
				}
				BroadcastWeather();
				string arg = LocalizeSeason(s);
				string arg2 = LocalizeSeason(subsystemWeather.CurrentSeason);
				DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 27), arg, arg2), Color.White, blinking: false, playNotificationSound: false);
				UpdateSeasonButtonIcon();
			}
			if (m_timeControlButtonWidget != null && m_timeControlButtonWidget.IsClicked && m_timeControlMenuWidget != null)
			{
				m_timeControlMenuWidget.IsVisible = !m_timeControlMenuWidget.IsVisible;
			}
			if (HasServerManagementRights())
			{
				if (m_timeHalfButtonWidget != null && m_timeHalfButtonWidget.IsClicked)
				{
					ApplyTimeScale(0.5f);
				}
				if (m_time1xButtonWidget != null && m_time1xButtonWidget.IsClicked)
				{
					ApplyTimeScale(1f);
				}
				if (m_time2xButtonWidget != null && m_time2xButtonWidget.IsClicked)
				{
					ApplyTimeScale(2f);
				}
				if (m_time4xButtonWidget != null && m_time4xButtonWidget.IsClicked)
				{
					ApplyTimeScale(4f);
				}
			}
			if (m_weatherCycloneButtonWidget.IsClicked)
			{
				if (!subsystemWeather.IsCycloneActive)
				{
					if (MathUtils.Max(subsystemWeather.CurrentWindStrength, subsystemWeather.TargetWindStrength) < 14f)
					{
						DisplaySmallMessage(LanguageControl.Get(fName, 28), Color.White, blinking: false, playNotificationSound: false);
					}
					else
					{
						subsystemWeather.ActivateCyclone();
						DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 29), subsystemWeather.CycloneRadius, subsystemWeather.CycloneCenter.X, subsystemWeather.CycloneCenter.Y, subsystemWeather.TargetWindStrength), Color.White, blinking: false, playNotificationSound: false);
					}
				}
				else
				{
					subsystemWeather.IsCycloneActive = false;
					subsystemWeather.ActiveTornadoes.Clear();
					subsystemWeather.TornadoSpawnPositions.Clear();
					subsystemWeather.TornadoSpawnTimes.Clear();
					subsystemWeather.TornadoSpeedMultipliers.Clear();
					DisplaySmallMessage(LanguageControl.Get(fName, 30), Color.White, blinking: false, playNotificationSound: false);
				}
				m_weatherCycloneButtonWidget.IsChecked = subsystemWeather.IsCycloneActive;
			}
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && HasServerManagementRights() && (m_timeOfDayButtonWidget.IsClicked || playerInput.TimeOfDay))
		{
			float sunriseOffset = m_subsystemTimeOfDay.SunriseOffset;
			float dayOffset = m_subsystemTimeOfDay.DayOffset;
			float sunsetOffset = m_subsystemTimeOfDay.SunsetOffset;
			float nightOffset = m_subsystemTimeOfDay.NightOffset;
			float num16 = MathUtils.Remainder(sunriseOffset - m_subsystemTimeOfDay.TimeOfDay, 1f);
			float num17 = MathUtils.Remainder(dayOffset - m_subsystemTimeOfDay.TimeOfDay, 1f);
			float num18 = MathUtils.Remainder(sunsetOffset - m_subsystemTimeOfDay.TimeOfDay, 1f);
			float num19 = MathUtils.Remainder(nightOffset - m_subsystemTimeOfDay.TimeOfDay, 1f);
			float num20 = MathUtils.Min(num16, num17, num18, num19);
			byte? b = null;
			if (num16 == num20)
			{
				m_subsystemTimeOfDay.TimeOfDayOffset += num16;
				DisplaySmallMessage(LanguageControl.Get(fName, 15), Color.White, blinking: false, playNotificationSound: false);
				b = 0;
			}
			else if (num17 == num20)
			{
				m_subsystemTimeOfDay.TimeOfDayOffset += num17;
				DisplaySmallMessage(LanguageControl.Get(fName, 16), Color.White, blinking: false, playNotificationSound: false);
				b = 1;
			}
			else if (num18 == num20)
			{
				m_subsystemTimeOfDay.TimeOfDayOffset += num18;
				DisplaySmallMessage(LanguageControl.Get(fName, 17), Color.White, blinking: false, playNotificationSound: false);
				b = 2;
			}
			else if (num19 == num20)
			{
				m_subsystemTimeOfDay.TimeOfDayOffset += num19;
				DisplaySmallMessage(LanguageControl.Get(fName, 18), Color.White, blinking: false, playNotificationSound: false);
				b = 3;
			}
			if (b.HasValue)
			{
				CommonLib.Net.QueuePackage(new SubsystemTimePackage(m_subsystemTimeOfDay.m_subsystemGameInfo.TotalElapsedGameTime, m_subsystemTimeOfDay.TimeOfDayOffset, Project.FindSubsystem<SubsystemTime>(throwOnError: true).TimeControlFactor));
			}
		}
		if (m_creativeFlyBoostButtonWidget.IsClicked)
		{
			ComponentLocomotion componentLocomotion = m_componentPlayer.ComponentLocomotion;
			if (!m_flyBoostActive)
			{
				if (m_defaultFlySpeed <= 0f)
				{
					m_defaultFlySpeed = componentLocomotion.CreativeFlySpeed;
				}
				if (componentLocomotion.IsCreativeFlyEnabled)
				{
					componentLocomotion.CreativeFlySpeed = m_defaultFlySpeed * 6f;
					DisplaySmallMessage(LanguageControl.Get(fName, 20), Color.White, blinking: false, playNotificationSound: false);
				}
				m_flyBoostActive = true;
			}
			else
			{
				if (componentLocomotion.IsCreativeFlyEnabled)
				{
					componentLocomotion.CreativeFlySpeed = m_defaultFlySpeed;
					DisplaySmallMessage(LanguageControl.Get(fName, 21), Color.White, blinking: false, playNotificationSound: false);
				}
				m_flyBoostActive = false;
			}
		}
		if (ModalPanelWidget != null)
		{
			if (input.Cancel || input.Back || m_backButtonWidget.IsClicked)
			{
				ModalPanelWidget = null;
				return;
			}
		}
		else if (input.Back || m_backButtonWidget.IsClicked)
		{
			DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new GameMenuDialog(m_componentPlayer));
		}
		float value2 = 0f;
		float value3 = 0f;
		if (m_componentPlayer != null)
		{
			if (m_componentPlayer.ComponentHealth != null)
			{
				value2 = MathUtils.Saturate(m_componentPlayer.ComponentHealth.Health);
				float maxHealthMultiplier = m_subsystemGameInfo.WorldSettings.MaxHealthMultiplier;
				float num21 = MathUtils.Max(0.1f, m_componentPlayer.ComponentBody.Size);
				HealthBarWidget.BarsCount = Math.Max(1, (int)(10f * maxHealthMultiplier * num21));
				int num22 = worldSettings.LivesLimit;
				if (worldSettings.GameMode == GameMode.Cruel)
				{
					num22 = 1;
				}
				else if (worldSettings.GameMode == GameMode.Embrace)
				{
					num22 = 7;
				}
				if (num22 > 0)
				{
					int num23 = num22;
					if (worldSettings.GameMode != GameMode.Cruel)
					{
						num23 += m_componentPlayer.PlayerData.ExtraLives;
					}
					int extraLivesCount = MathUtils.Max(0, num23 - m_componentPlayer.PlayerData.SpawnsCount + 1);
					HealthBarWidget.ExtraLivesCount = extraLivesCount;
				}
				else
				{
					HealthBarWidget.ExtraLivesCount = 0;
				}
			}
			if (m_componentPlayer.ComponentVitalStats != null)
			{
				value3 = MathUtils.Saturate(m_componentPlayer.ComponentVitalStats.Food);
			}
		}
		HealthBarWidget.Value = value2;
		FoodBarWidget.Value = value3;
	}

	public bool IsClothingVisible()
	{
		return ModalPanelWidget is ClothingWidget;
	}

	public bool IsInventoryVisible()
	{
		if (ModalPanelWidget != null)
		{
			return !IsClothingVisible();
		}
		return false;
	}

	public bool IsActiveSlotEditable()
	{
		IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
		if (inventory != null)
		{
			int activeSlotIndex = inventory.ActiveSlotIndex;
			int num = Terrain.ExtractContents(inventory.GetSlotValue(activeSlotIndex));
			if (BlocksManager.Blocks[num].IsEditable)
			{
				return true;
			}
		}
		return false;
	}

	public ComponentGui()
	{
		m_timeScaleUi = 1f;
		m_availableSeasonIcons = new HashSet<string>(StringComparer.Ordinal);
		CloseTimeLable = new LabelWidget
		{
			Name = "CloseTime",
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Near,
			IsVisible = false
		};
	}

	static ComponentGui()
	{
		fName = "ComponentGui";
	}

	private void BroadcastWeather()
	{
		SubsystemWeather subsystemWeather = m_subsystemWeather;
		CommonLib.Net.QueuePackage(new SubsystemWeatherPackage(subsystemWeather.m_precipitationStartTime, subsystemWeather.m_precipitationEndTime, subsystemWeather.m_lightningIntensity, subsystemWeather.TargetLightningIntensity, subsystemWeather.WindStartTime, subsystemWeather.WindEndTime, subsystemWeather.TargetWindStrength, subsystemWeather.WindDirection, subsystemWeather.CurrentWindStrength, (int)subsystemWeather.CurrentSeason, subsystemWeather.IsGustyWind));
	}

	private static SubsystemWeather.Season NextSeason(SubsystemWeather.Season s)
	{
		return s switch
		{
			SubsystemWeather.Season.EarlyAutumn => SubsystemWeather.Season.Autumn, 
			SubsystemWeather.Season.Autumn => SubsystemWeather.Season.LateAutumn, 
			SubsystemWeather.Season.LateAutumn => SubsystemWeather.Season.Anabiosis, 
			SubsystemWeather.Season.Anabiosis => SubsystemWeather.Season.Winter, 
			SubsystemWeather.Season.Winter => SubsystemWeather.Season.LateWinter, 
			SubsystemWeather.Season.LateWinter => SubsystemWeather.Season.EarlySpring, 
			SubsystemWeather.Season.EarlySpring => SubsystemWeather.Season.Spring, 
			SubsystemWeather.Season.Spring => SubsystemWeather.Season.LateSpring, 
			SubsystemWeather.Season.LateSpring => SubsystemWeather.Season.EarlySummer, 
			SubsystemWeather.Season.EarlySummer => SubsystemWeather.Season.Summer, 
			SubsystemWeather.Season.Summer => SubsystemWeather.Season.LateSummer, 
			_ => SubsystemWeather.Season.EarlyAutumn, 
		};
	}

	private string GetCellPrecipKind(SubsystemWeather weather)
	{
		Vector3 position = m_componentPlayer.ComponentBody.Position;
		return weather.GetPrecipitationShaftInfo(Terrain.ToCell(position.X), Terrain.ToCell(position.Z)).Type switch
		{
			PrecipitationType.Snow => LanguageControl.Get(fName, 35), 
			PrecipitationType.Sand => LanguageControl.Get(fName, 37), 
			_ => LanguageControl.Get(fName, 36), 
		};
	}

	private string LocalizeSeason(SubsystemWeather.Season s)
	{
		bool flag = false;
		bool flag2 = false;
		SubsystemWeather.Season season = s;
		switch (s)
		{
		case SubsystemWeather.Season.EarlyAutumn:
			flag = true;
			season = SubsystemWeather.Season.Autumn;
			break;
		case SubsystemWeather.Season.LateAutumn:
			flag2 = true;
			season = SubsystemWeather.Season.Autumn;
			break;
		case SubsystemWeather.Season.LateWinter:
			flag2 = true;
			season = SubsystemWeather.Season.Winter;
			break;
		case SubsystemWeather.Season.EarlySpring:
			flag = true;
			season = SubsystemWeather.Season.Spring;
			break;
		case SubsystemWeather.Season.LateSpring:
			flag2 = true;
			season = SubsystemWeather.Season.Spring;
			break;
		case SubsystemWeather.Season.EarlySummer:
			flag = true;
			season = SubsystemWeather.Season.Summer;
			break;
		case SubsystemWeather.Season.LateSummer:
			flag2 = true;
			season = SubsystemWeather.Season.Summer;
			break;
		}
		string text = LanguageControl.Get("Seasons", season.ToString());
		string text2;
		if (flag)
		{
			text2 = LanguageControl.Get("Seasons", "Early");
		}
		else
		{
			text2 = (flag2 ? LanguageControl.Get("Seasons", "Late") : null);
		}
		if (!string.IsNullOrEmpty(text2))
		{
			return text2 + " " + text;
		}
		return text;
	}

	private string GetSeasonFamily(SubsystemWeather.Season s)
	{
		switch (s)
		{
		case SubsystemWeather.Season.EarlyAutumn:
		case SubsystemWeather.Season.Autumn:
		case SubsystemWeather.Season.LateAutumn:
			return "Autumn";
		case SubsystemWeather.Season.EarlySpring:
		case SubsystemWeather.Season.Spring:
		case SubsystemWeather.Season.LateSpring:
			return "Spring";
		case SubsystemWeather.Season.EarlySummer:
		case SubsystemWeather.Season.Summer:
		case SubsystemWeather.Season.LateSummer:
			return "Summer";
		default:
			return "Winter";
		}
	}

	private string GetIconKeyForSeason(SubsystemWeather.Season s)
	{
		string text = s.ToString();
		if (m_availableSeasonIcons.Contains(text))
		{
			return text;
		}
		return GetSeasonFamily(s);
	}

	public void UpdateSeasonButtonIcon()
	{
		if (m_weatherSeasonNextButtonWidget == null || m_subsystemWeather == null)
		{
			return;
		}
		string iconKeyForSeason = GetIconKeyForSeason(m_subsystemWeather.CurrentSeason);
		if (!(m_lastSeasonIconKey == iconKeyForSeason) && m_weatherSeasonNextButtonWidget is BitmapButtonWidget bitmapButtonWidget)
		{
			try
			{
				bitmapButtonWidget.NormalSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/Seasons/" + iconKeyForSeason);
			}
			catch
			{
				return;
			}
			try
			{
				bitmapButtonWidget.ClickedSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/Seasons/" + iconKeyForSeason + "_Pressed");
			}
			catch
			{
				bitmapButtonWidget.ClickedSubtexture = bitmapButtonWidget.NormalSubtexture;
			}
			m_lastSeasonIconKey = iconKeyForSeason;
		}
	}

	private void ApplyTimeScale(float scale)
	{
		Project.FindSubsystem<SubsystemTime>(throwOnError: true).SetTimeControl(scale);
		DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 38), scale.ToString("0.##")), Color.White, blinking: false, playNotificationSound: false);
		CommonLib.Net.QueuePackage(new SubsystemTimePackage(m_subsystemGameInfo.TotalElapsedGameTime, m_subsystemTimeOfDay.TimeOfDayOffset, scale));
	}

	private void CenterWeatherRows()
	{
		TryCenterRow(m_weatherWindRowWidget, m_weatherWindButtonWidget);
		TryCenterRow(m_weatherRainRowWidget, m_weatherRainButtonWidget);
	}

	private void TryCenterRow(StackPanelWidget row, ButtonWidget middle)
	{
		if (row != null && middle != null)
		{
			float num = ((middle.ActualSize.X > 0f) ? middle.ActualSize.X : 68f);
			float num2 = 4f;
			float x = 0f - (num + num2);
			row.RenderTransform = Matrix.CreateTranslation(x, 0f, 0f);
		}
	}

	private string GetWeLanguageFolder()
	{
		string text = LanguageControl.LName();
		switch (text)
		{
		case "ru-RU":
		case "en-US":
		case "es-MX":
			return text;
		default:
			return "en-US";
		}
	}

	private Subtexture TryLoadWe(string key, string lang)
	{
		try
		{
			return ContentManager.Get<Subtexture>("WE/" + lang + "/" + key);
		}
		catch
		{
		}
		try
		{
			return ContentManager.Get<Subtexture>("WE/en-US/" + key);
		}
		catch
		{
		}
		try
		{
			return ContentManager.Get<Subtexture>("WE/" + key);
		}
		catch
		{
		}
		return null;
	}

	private void ApplyWorldEditLocalizedIcons(Dictionary<string, Widget> dict)
	{
		if (dict == null)
		{
			return;
		}
		string weLanguageFolder = GetWeLanguageFolder();
		foreach (KeyValuePair<string, Tuple<string, string>> item3 in new Dictionary<string, Tuple<string, string>>(StringComparer.Ordinal)
		{
			{
				"F1",
				Tuple.Create("Button1", "Button1_pressed")
			},
			{
				"F2",
				Tuple.Create("Button2", "Button2_pressed")
			},
			{
				"F3",
				Tuple.Create("Button3", "Button3_pressed")
			},
			{
				"WorldEditMenu",
				Tuple.Create("WEBTN", "WEBTNP")
			},
			{
				"ButtonOverlay",
				Tuple.Create("ButtonOverlay", "ButtonOverlay_pressed")
			},
			{
				"ButtonMirrorVertical",
				Tuple.Create("ButtonMirrorVertical", "ButtonMirrorVertical_pressed")
			},
			{
				"ButtonMirrorHorizontal",
				Tuple.Create("ButtonMirrorHorizontal", "ButtonMirrorHorizontal_pressed")
			},
			{
				"F5",
				Tuple.Create("ButtonPaste", "ButtonPaste_pressed")
			},
			{
				"F6",
				Tuple.Create("ButtonFill", "ButtonFill_pressed")
			},
			{
				"F7",
				Tuple.Create("ButtonReplace", "ButtonReplace_pressed")
			},
			{
				"F8",
				Tuple.Create("ButtonClear", "ButtonClear_pressed")
			},
			{
				"F9",
				Tuple.Create("ButtonMemCopy", "ButtonMemCopy_pressed")
			},
			{
				"F10",
				Tuple.Create("ButtonMemPaste", "ButtonMemPaste_pressed")
			},
			{
				"F11",
				Tuple.Create("Button_geo", "Button_geo_press")
			},
			{
				"ButtonRotationVertical",
				Tuple.Create("ButtonRotationVertical", "ButtonRotationVertical_pressed")
			},
			{
				"ButtonRotationHorizontal",
				Tuple.Create("ButtonRotationHorizontal", "ButtonRotationHorizontal_pressed")
			},
			{
				"UndoButton",
				Tuple.Create("Button_Undo", "Button_Undo_pressed")
			},
			{
				"RedoButton",
				Tuple.Create("Button_Redo", "Button_Redo_pressed")
			}
		})
		{
			if (dict.TryGetValue(item3.Key, out var value) && value is BitmapButtonWidget bitmapButtonWidget)
			{
				string item = item3.Value.Item1;
				string item2 = item3.Value.Item2;
				Subtexture subtexture = TryLoadWe(item, weLanguageFolder);
				Subtexture subtexture2 = TryLoadWe(item2, weLanguageFolder);
				if (subtexture != null)
				{
					bitmapButtonWidget.NormalSubtexture = subtexture;
				}
				if (subtexture2 != null)
				{
					bitmapButtonWidget.ClickedSubtexture = subtexture2;
				}
				else if (subtexture != null && bitmapButtonWidget.ClickedSubtexture == null)
				{
					bitmapButtonWidget.ClickedSubtexture = subtexture;
				}
			}
		}
	}

	private bool HasServerManagementRights()
	{
		if (CommonLib.WorkType == WorkType.Local)
		{
			return true;
		}
		return (m_componentPlayer?.PlayerData)?.ServerManager ?? false;
	}

	private void UpdateBagTooltip()
	{
		WidgetInput input = m_componentPlayer.GameWidget.Input;
		bool flag = false;
		if (m_componentPlayer.ComponentInput.SplitSourceInventory != null)
		{
			flag = true;
		}
		if (!flag || !input.MousePosition.HasValue)
		{
			RemoveBagPreview();
			return;
		}
		Widget widget = input.Widget.HitTestGlobal(input.MousePosition.Value);
		if (widget == null)
		{
			RemoveBagPreview();
			return;
		}
		InventorySlotWidget inventorySlotWidget = widget as InventorySlotWidget;
		if (inventorySlotWidget == null)
		{
			inventorySlotWidget = widget.ParentWidget as InventorySlotWidget;
		}
		if (inventorySlotWidget == null)
		{
			RemoveBagPreview();
			return;
		}
		if (inventorySlotWidget.m_inventory == null)
		{
			RemoveBagPreview();
			return;
		}
		int slotValue = inventorySlotWidget.m_inventory.GetSlotValue(inventorySlotWidget.m_slotIndex);
		if (inventorySlotWidget.m_inventory.GetSlotCount(inventorySlotWidget.m_slotIndex) <= 0)
		{
			RemoveBagPreview();
			return;
		}
		int num = Terrain.ExtractContents(slotValue);
		int data = Terrain.ExtractData(slotValue);
		Block block = BlocksManager.Blocks[num];
		int num2 = 0;
		if (block is BagBlock)
		{
			num2 = BagBlock.GetBagId(data);
		}
		else if (block is LargeBagBlock)
		{
			num2 = LargeBagBlock.GetBagId(data);
		}
		else if (block is BackpackBlock)
		{
			num2 = BackpackBlock.GetBagId(data);
		}
		else if (block is ClimbingBackpackBlock)
		{
			num2 = ClimbingBackpackBlock.GetBagId(data);
		}
		if (num2 == 0)
		{
			RemoveBagPreview();
			return;
		}
		BagInventory bagInventory = m_subsystemBags.Find(num2);
		if (bagInventory == null)
		{
			RemoveBagPreview();
			return;
		}
		if (m_bagPreviewWidget == null)
		{
			m_bagPreviewWidget = new BagPreviewWidget();
			m_componentPlayer.GameWidget.Children.Add(m_bagPreviewWidget);
		}
		m_bagPreviewWidget.Setup(bagInventory, m_subsystemTerrain);
		Vector2 actualSize = m_componentPlayer.GameWidget.ActualSize;
		m_bagPreviewWidget.Measure(new Vector2(float.PositiveInfinity));
		Vector2 desiredSize = m_bagPreviewWidget.DesiredSize;
		float x = actualSize.X / 2f + 340f;
		float x2 = actualSize.Y / 2f - desiredSize.Y / 2f;
		x = MathUtils.Clamp(x, 0f, actualSize.X - desiredSize.X);
		x2 = MathUtils.Clamp(x2, 0f, actualSize.Y - desiredSize.Y);
		m_bagPreviewWidget.Margin = new Vector2(x, x2);
		m_bagPreviewWidget.HorizontalAlignment = WidgetAlignment.Near;
		m_bagPreviewWidget.VerticalAlignment = WidgetAlignment.Near;
	}

	private void RemoveBagPreview()
	{
		if (m_bagPreviewWidget != null)
		{
			if (m_bagPreviewWidget.ParentWidget != null)
			{
				m_bagPreviewWidget.ParentWidget.Children.Remove(m_bagPreviewWidget);
			}
			m_bagPreviewWidget = null;
		}
	}
}
