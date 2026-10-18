using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Input;

namespace Game;

public class SettingsKeyBindingsScreen : Screen
{
	private StackPanelWidget m_listHost;

	private BevelledButtonWidget m_resetButton;

	private (KeyAction, bool)? m_listening;

	private Dictionary<KeyAction, (LabelWidget, BevelledButtonWidget, BevelledButtonWidget)> m_rows;

	private Screen m_previousScreen;

	private BevelledButtonWidget m_clearButton;

	public SettingsKeyBindingsScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/SettingsKeyBindingsScreen");
		LoadContents(this, node);
		m_listHost = Children.Find<StackPanelWidget>("KeyBindings.ListHost");
		m_resetButton = Children.Find<BevelledButtonWidget>("KeyBindings.Reset");
		m_clearButton = Children.Find<BevelledButtonWidget>("KeyBindings.Clear");
		m_rows = new Dictionary<KeyAction, (LabelWidget, BevelledButtonWidget, BevelledButtonWidget)>();
		foreach (KeyAction allAction in KeyBindings.AllActions)
		{
			Widget widget = CreateRow(allAction);
			m_listHost.Children.Add(widget);
		}
		RefreshTexts();
	}

	public override void Update()
	{
		ButtonWidget buttonWidget = Children.Find<ButtonWidget>("TopBar.Back", throwIfNotFound: false);
		if (Input.Back || Input.Cancel || (buttonWidget != null && buttonWidget.IsClicked))
		{
			SettingsManager.KeyBindingsData = BuildBindingsData();
			SettingsManager.SaveSettings();
			ScreensManager.SwitchScreen(m_previousScreen ?? ScreensManager.PreviousScreen);
			m_previousScreen = null;
			return;
		}
		if (m_resetButton.IsClicked)
		{
			KeyBindings.ResetToDefaults();
			RefreshTexts();
			SettingsManager.KeyBindingsData = BuildBindingsData();
			SettingsManager.SaveSettings();
		}
		m_clearButton.IsVisible = m_listening.HasValue;
		if (m_listening.HasValue)
		{
			if (m_clearButton.IsClicked)
			{
				(KeyAction, bool) value = m_listening.Value;
				if (value.Item2)
				{
					KeyBindings.SetSecondaryNullable(value.Item1, null);
				}
				else
				{
					KeyBindings.SetPrimaryNullable(value.Item1, null);
				}
				m_listening = null;
				RefreshTexts();
				SettingsManager.KeyBindingsData = BuildBindingsData();
				SettingsManager.SaveSettings();
				return;
			}
			Key? lastKey = Input.LastKey;
			if (!lastKey.HasValue)
			{
				return;
			}
			if (lastKey.Value == Key.Escape)
			{
				m_listening = null;
				RefreshTexts();
				return;
			}
			(KeyAction, bool) value2 = m_listening.Value;
			if (value2.Item2)
			{
				KeyBindings.SetSecondary(value2.Item1, lastKey.Value);
			}
			else
			{
				KeyBindings.SetPrimary(value2.Item1, lastKey.Value);
			}
			m_listening = null;
			RefreshTexts();
			SettingsManager.KeyBindingsData = BuildBindingsData();
			SettingsManager.SaveSettings();
			return;
		}
		foreach (KeyValuePair<KeyAction, (LabelWidget, BevelledButtonWidget, BevelledButtonWidget)> row in m_rows)
		{
			KeyAction key = row.Key;
			(LabelWidget, BevelledButtonWidget, BevelledButtonWidget) value3 = row.Value;
			if (value3.Item2.IsClicked)
			{
				m_listening = (key, false);
				value3.Item2.Text = LanguageControl.Get("SettingsKeyBindingsScreen", "PressKey");
			}
			else if (value3.Item3.IsClicked)
			{
				m_listening = (key, true);
				value3.Item3.Text = LanguageControl.Get("SettingsKeyBindingsScreen", "PressKey");
			}
		}
	}

	private Widget CreateRow(KeyAction action)
	{
		UniformSpacingPanelWidget uniformSpacingPanelWidget = new UniformSpacingPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			Margin = new Vector2(0f, 3f)
		};
		LabelWidget labelWidget = new LabelWidget
		{
			Text = "[SettingsKeyBindingsScreen:" + action.ToString() + "]",
			HorizontalAlignment = WidgetAlignment.Far,
			VerticalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(20f, 0f)
		};
		BevelledButtonWidget bevelledButtonWidget = new BevelledButtonWidget
		{
			Name = "Primary_" + action,
			Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
			VerticalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(20f, 0f)
		};
		BevelledButtonWidget bevelledButtonWidget2 = new BevelledButtonWidget
		{
			Name = "Secondary_" + action,
			Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
			VerticalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(20f, 0f)
		};
		uniformSpacingPanelWidget.Children.Add(labelWidget);
		uniformSpacingPanelWidget.Children.Add(bevelledButtonWidget);
		uniformSpacingPanelWidget.Children.Add(bevelledButtonWidget2);
		m_rows[action] = (labelWidget, bevelledButtonWidget, bevelledButtonWidget2);
		return uniformSpacingPanelWidget;
	}

	private void RefreshTexts()
	{
		Children.Find<LabelWidget>("TopBar.Label").Text = LanguageControl.Get("SettingsKeyBindingsScreen", "Title");
		Dictionary<Key, int> dictionary = new Dictionary<Key, int>(128);
		foreach (KeyAction allAction in KeyBindings.AllActions)
		{
			Key? primaryNullable = KeyBindings.GetPrimaryNullable(allAction);
			if (primaryNullable.HasValue)
			{
				if (!dictionary.TryGetValue(primaryNullable.Value, out var value))
				{
					value = 0;
				}
				dictionary[primaryNullable.Value] = value + 1;
			}
			Key? secondaryNullable = KeyBindings.GetSecondaryNullable(allAction);
			if (secondaryNullable.HasValue)
			{
				if (!dictionary.TryGetValue(secondaryNullable.Value, out var value2))
				{
					value2 = 0;
				}
				dictionary[secondaryNullable.Value] = value2 + 1;
			}
		}
		Color white = Color.White;
		Color color = new Color(255, 216, 64);
		foreach (KeyValuePair<KeyAction, (LabelWidget, BevelledButtonWidget, BevelledButtonWidget)> row in m_rows)
		{
			KeyAction key = row.Key;
			(LabelWidget, BevelledButtonWidget, BevelledButtonWidget) value3 = row.Value;
			Key? primaryNullable2 = KeyBindings.GetPrimaryNullable(key);
			Key? secondaryNullable2 = KeyBindings.GetSecondaryNullable(key);
			value3.Item2.Text = KeyBindings.FormatKey(primaryNullable2);
			value3.Item3.Text = KeyBindings.FormatKey(secondaryNullable2);
			bool flag = primaryNullable2.HasValue && dictionary.ContainsKey(primaryNullable2.Value) && dictionary[primaryNullable2.Value] > 1;
			bool flag2 = secondaryNullable2.HasValue && dictionary.ContainsKey(secondaryNullable2.Value) && dictionary[secondaryNullable2.Value] > 1;
			value3.Item2.Color = (flag ? color : white);
			value3.Item3.Color = (flag2 ? color : white);
			value3.Item1.Color = Color.White;
		}
		m_resetButton.Text = LanguageControl.Get("SettingsKeyBindingsScreen", "Reset");
		m_clearButton.Text = LanguageControl.Get("SettingsKeyBindingsScreen", "Clear");
	}

	private string BuildBindingsData()
	{
		return KeyBindings.Serialize();
	}

	public override void Enter(object[] parameters)
	{
		m_previousScreen = ScreensManager.PreviousScreen;
	}
}
