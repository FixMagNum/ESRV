using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class SettingsControlsScreen : Screen
{
	public ButtonWidget m_moveControlModeButton;

	public ButtonWidget m_lookControlModeButton;

	public ButtonWidget m_leftHandedLayoutButton;

	public ButtonWidget m_flipVerticalAxisButton;

	public ButtonWidget m_autoJumpButton;

	public ButtonWidget m_horizontalCreativeFlightButton;

	public ContainerWidget m_horizontalCreativeFlightPanel;

	public SliderWidget m_moveSensitivitySlider;

	public SliderWidget m_lookSensitivitySlider;

	public SliderWidget m_gamepadCursorSpeedSlider;

	public SliderWidget m_gamepadDeadZoneSlider;

	public SliderWidget m_creativeDigTimeSlider;

	public SliderWidget m_creativeReachSlider;

	public SliderWidget m_holdDurationSlider;

	public SliderWidget m_dragDistanceSlider;

	public static string fName;

	public SliderWidget m_buildHoldTimerStartSlider;

	public SliderWidget m_buildHoldTimerIntervalSlider;

	public ButtonWidget m_keyBindingsButton;

	private Screen m_previousScreen;

	public SettingsControlsScreen()
	{
		XElement viewportSize = ContentManager.Get<XElement>("Screens/SettingsControlsScreen");
		LoadContents(this, viewportSize);
		m_buildHoldTimerStartSlider = Children.Find<SliderWidget>("BuildHoldTimerStartSlider");
		m_buildHoldTimerIntervalSlider = Children.Find<SliderWidget>("BuildHoldTimerIntervalSlider");
		m_moveControlModeButton = Children.Find<ButtonWidget>("MoveControlMode");
		m_lookControlModeButton = Children.Find<ButtonWidget>("LookControlMode");
		m_leftHandedLayoutButton = Children.Find<ButtonWidget>("LeftHandedLayout");
		m_flipVerticalAxisButton = Children.Find<ButtonWidget>("FlipVerticalAxis");
		m_autoJumpButton = Children.Find<ButtonWidget>("AutoJump");
		m_horizontalCreativeFlightButton = Children.Find<ButtonWidget>("HorizontalCreativeFlight");
		m_horizontalCreativeFlightPanel = Children.Find<ContainerWidget>("HorizontalCreativeFlightPanel");
		m_moveSensitivitySlider = Children.Find<SliderWidget>("MoveSensitivitySlider");
		m_lookSensitivitySlider = Children.Find<SliderWidget>("LookSensitivitySlider");
		m_gamepadCursorSpeedSlider = Children.Find<SliderWidget>("GamepadCursorSpeedSlider");
		m_gamepadDeadZoneSlider = Children.Find<SliderWidget>("GamepadDeadZoneSlider");
		m_creativeDigTimeSlider = Children.Find<SliderWidget>("CreativeDigTimeSlider");
		m_creativeReachSlider = Children.Find<SliderWidget>("CreativeReachSlider");
		m_holdDurationSlider = Children.Find<SliderWidget>("HoldDurationSlider");
		m_dragDistanceSlider = Children.Find<SliderWidget>("DragDistanceSlider");
		m_keyBindingsButton = Children.Find<ButtonWidget>("KeyBindingsButton");
		m_horizontalCreativeFlightPanel.IsVisible = false;
	}

	public override void Update()
	{
		if (m_buildHoldTimerStartSlider.IsSliding)
		{
			SettingsManager.BuildHoldTimerStart = MathUtils.Clamp(m_buildHoldTimerStartSlider.Value, 0.12f, 1f);
		}
		if (m_buildHoldTimerIntervalSlider.IsSliding)
		{
			SettingsManager.BuildHoldTimerInterval = MathUtils.Clamp(m_buildHoldTimerIntervalSlider.Value, 0.12f, 1f);
		}
		if (m_moveControlModeButton.IsClicked)
		{
			IList<int> font = EnumUtils.GetEnumValues(typeof(MoveControlMode));
			int num = font.IndexOf((int)SettingsManager.MoveControlMode);
			SettingsManager.MoveControlMode = (MoveControlMode)font[(num + 1) % font.Count];
		}
		if (m_lookControlModeButton.IsClicked)
		{
			IList<int> position = EnumUtils.GetEnumValues(typeof(LookControlMode));
			int count = position.IndexOf((int)SettingsManager.LookControlMode);
			SettingsManager.LookControlMode = (LookControlMode)position[(count + 1) % position.Count];
		}
		if (m_leftHandedLayoutButton.IsClicked)
		{
			SettingsManager.LeftHandedLayout = !SettingsManager.LeftHandedLayout;
		}
		if (m_flipVerticalAxisButton.IsClicked)
		{
			SettingsManager.FlipVerticalAxis = !SettingsManager.FlipVerticalAxis;
		}
		if (m_autoJumpButton.IsClicked)
		{
			SettingsManager.AutoJump = !SettingsManager.AutoJump;
		}
		if (m_horizontalCreativeFlightButton.IsClicked)
		{
			SettingsManager.HorizontalCreativeFlight = !SettingsManager.HorizontalCreativeFlight;
		}
		if (m_moveSensitivitySlider.IsSliding)
		{
			SettingsManager.MoveSensitivity = m_moveSensitivitySlider.Value;
		}
		if (m_lookSensitivitySlider.IsSliding)
		{
			SettingsManager.LookSensitivity = m_lookSensitivitySlider.Value;
		}
		if (m_gamepadCursorSpeedSlider.IsSliding)
		{
			SettingsManager.GamepadCursorSpeed = m_gamepadCursorSpeedSlider.Value;
		}
		if (m_gamepadDeadZoneSlider.IsSliding)
		{
			SettingsManager.GamepadDeadZone = m_gamepadDeadZoneSlider.Value;
		}
		if (m_creativeDigTimeSlider.IsSliding)
		{
			SettingsManager.CreativeDigTime = m_creativeDigTimeSlider.Value;
		}
		if (m_creativeReachSlider.IsSliding)
		{
			SettingsManager.CreativeReach = m_creativeReachSlider.Value;
		}
		if (m_holdDurationSlider.IsSliding)
		{
			SettingsManager.MinimumHoldDuration = m_holdDurationSlider.Value;
		}
		if (m_dragDistanceSlider.IsSliding)
		{
			SettingsManager.MinimumDragDistance = m_dragDistanceSlider.Value;
		}
		m_moveControlModeButton.Text = LanguageControl.Get("MoveControlMode", SettingsManager.MoveControlMode.ToString());
		m_lookControlModeButton.Text = LanguageControl.Get("LookControlMode", SettingsManager.LookControlMode.ToString());
		m_leftHandedLayoutButton.Text = (SettingsManager.LeftHandedLayout ? LanguageControl.Get("Usual", "on") : LanguageControl.Get("Usual", "off"));
		m_flipVerticalAxisButton.Text = (SettingsManager.FlipVerticalAxis ? LanguageControl.Get("Usual", "on") : LanguageControl.Get("Usual", "off"));
		m_autoJumpButton.Text = (SettingsManager.AutoJump ? LanguageControl.Get("Usual", "on") : LanguageControl.Get("Usual", "off"));
		m_horizontalCreativeFlightButton.Text = (SettingsManager.HorizontalCreativeFlight ? LanguageControl.Get("Usual", "on") : LanguageControl.Get("Usual", "off"));
		m_buildHoldTimerStartSlider.Text = $"{SettingsManager.BuildHoldTimerStart:0.00}";
		m_buildHoldTimerIntervalSlider.Text = $"{SettingsManager.BuildHoldTimerInterval:0.00}";
		m_moveSensitivitySlider.Value = SettingsManager.MoveSensitivity;
		m_moveSensitivitySlider.Text = MathUtils.Round(SettingsManager.MoveSensitivity * 10f).ToString();
		m_lookSensitivitySlider.Value = SettingsManager.LookSensitivity;
		m_lookSensitivitySlider.Text = MathUtils.Round(SettingsManager.LookSensitivity * 10f).ToString();
		m_gamepadCursorSpeedSlider.Value = SettingsManager.GamepadCursorSpeed;
		m_gamepadCursorSpeedSlider.Text = $"{SettingsManager.GamepadCursorSpeed:0.0}x";
		m_gamepadDeadZoneSlider.Value = SettingsManager.GamepadDeadZone;
		m_gamepadDeadZoneSlider.Text = $"{SettingsManager.GamepadDeadZone * 100f:0}%";
		m_creativeDigTimeSlider.Value = SettingsManager.CreativeDigTime;
		m_creativeDigTimeSlider.Text = $"{MathUtils.Round(1000f * SettingsManager.CreativeDigTime)}ms";
		m_creativeReachSlider.Value = SettingsManager.CreativeReach;
		m_creativeReachSlider.Text = string.Format(LanguageControl.Get(fName, 1), $"{SettingsManager.CreativeReach:0.0} ");
		m_holdDurationSlider.Value = SettingsManager.MinimumHoldDuration;
		m_holdDurationSlider.Text = $"{MathUtils.Round(1000f * SettingsManager.MinimumHoldDuration)}ms";
		m_dragDistanceSlider.Value = SettingsManager.MinimumDragDistance;
		m_dragDistanceSlider.Text = $"{MathUtils.Round(SettingsManager.MinimumDragDistance)} " + LanguageControl.Get(fName, 2);
		if (m_keyBindingsButton != null && m_keyBindingsButton.IsClicked)
		{
			ScreensManager.SwitchScreen(new SettingsKeyBindingsScreen());
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen(m_previousScreen ?? ScreensManager.PreviousScreen);
			m_previousScreen = null;
		}
	}

	static SettingsControlsScreen()
	{
		fName = "SettingsControlsScreen";
	}

	public override void Enter(object[] parameters)
	{
		if (m_previousScreen == null)
		{
			m_previousScreen = ScreensManager.PreviousScreen;
		}
	}
}
