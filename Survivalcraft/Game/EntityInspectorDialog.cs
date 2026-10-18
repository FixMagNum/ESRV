using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Media;
using GameEntitySystem;

namespace Game;

public class EntityInspectorDialog : Dialog
{
	private class PropertyBinding
	{
		public Component Component;

		public PropertyInfo PropInfo;

		public FieldInfo FieldInfo;

		public TextBoxWidget TextBox;

		public ButtonWidget BoolButton;

		public string CustomName;

		public Type CustomType;
	}

	private class SimpleButtonWidget : ButtonWidget
	{
		private LabelWidget m_label;

		private bool m_isClicked;

		public override bool IsClicked => m_isClicked;

		public override bool IsChecked { get; set; }

		public override bool IsAutoCheckingEnabled { get; set; }

		public override string Text
		{
			get
			{
				return m_label.Text;
			}
			set
			{
				m_label.Text = value;
			}
		}

		public override BitmapFont Font
		{
			get
			{
				return m_label.Font;
			}
			set
			{
				m_label.Font = value;
			}
		}

		public override Color Color
		{
			get
			{
				return m_label.Color;
			}
			set
			{
				m_label.Color = value;
			}
		}

		public SimpleButtonWidget()
		{
			m_label = new LabelWidget
			{
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			};
			Children.Add(m_label);
			IsHitTestVisible = true;
		}

		public override void Update()
		{
			m_isClicked = false;
			if (Input.Tap.HasValue && HitTest(Input.Tap.Value))
			{
				m_isClicked = true;
			}
		}
	}

	private Entity m_entity;

	private LabelWidget m_titleLabel;

	private StackPanelWidget m_listPanel;

	private ButtonWidget m_saveButton;

	private ButtonWidget m_cancelButton;

	private List<PropertyBinding> m_bindings = new List<PropertyBinding>();

	private static Dictionary<Entity, float> m_savedScrollPositions;

	private ButtonWidget m_resetButtonWidget;

	private StackPanelWidget m_navigationPanel;

	private ScrollPanelWidget m_mainScroll;

	private void PopulateList()
	{
		try
		{
			m_listPanel.Children.Clear();
			m_navigationPanel.Children.Clear();
			m_bindings.Clear();
			foreach (Component component in m_entity.Components)
			{
				try
				{
					string text = component.GetType().Name;
					Color color = Color.Yellow;
					if (component is ComponentPlayer)
					{
						text = "Player Data";
						color = Color.Cyan;
					}
					LabelWidget labelWidget = new LabelWidget
					{
						Text = "--- " + text + " ---",
						Color = color,
						HorizontalAlignment = WidgetAlignment.Center,
						Margin = new Vector2(0f, 25f),
						FontScale = 1.1f
					};
					m_listPanel.Children.Add(labelWidget);
					SimpleButtonWidget simpleButtonWidget = new SimpleButtonWidget
					{
						Text = text.Replace("Component", ""),
						DesiredSize = new Vector2(220f, 40f),
						Color = Color.Gray,
						Margin = new Vector2(0f, 2f)
					};
					simpleButtonWidget.Tag = labelWidget;
					m_navigationPanel.Children.Add(simpleButtonWidget);
					if (component is ComponentPlayer { PlayerData: var playerData })
					{
						AddEditorRow(component, null, null, "Level", typeof(float), playerData.Level);
						AddEditorRow(component, null, null, "ServerManager", typeof(bool), playerData.ServerManager);
						AddEditorRow(component, null, null, "WorldEditManager", typeof(bool), playerData.WorldEditManager);
						continue;
					}
					foreach (PropertyInfo runtimeProperty in component.GetType().GetRuntimeProperties())
					{
						if (runtimeProperty.GetMethod != null && runtimeProperty.SetMethod != null && runtimeProperty.GetMethod.IsPublic && IsTypeSupported(runtimeProperty.PropertyType))
						{
							AddEditorRow(component, runtimeProperty, null);
						}
					}
					foreach (FieldInfo runtimeField in component.GetType().GetRuntimeFields())
					{
						if (runtimeField.IsPublic && !runtimeField.IsStatic && IsTypeSupported(runtimeField.FieldType))
						{
							AddEditorRow(component, null, runtimeField);
						}
					}
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("Populate Error: " + ex.Message);
		}
	}

	private bool IsTypeSupported(Type t)
	{
		if (!(t == typeof(int)) && !(t == typeof(float)) && !(t == typeof(string)) && !(t == typeof(bool)))
		{
			return t == typeof(Vector3);
		}
		return true;
	}

	public override void Update()
	{
		if (m_navigationPanel != null && m_listPanel != null)
		{
			float scrollPosition = m_mainScroll.ScrollPosition;
			foreach (Widget child in m_navigationPanel.Children)
			{
				if (child is SimpleButtonWidget simpleButtonWidget)
				{
					Widget widget = simpleButtonWidget.Tag as Widget;
					if (simpleButtonWidget.IsClicked && widget != null)
					{
						m_mainScroll.ScrollPosition = CalculateScrollPosition(widget);
					}
					if (widget != null)
					{
						CalculateScrollPosition(widget);
						simpleButtonWidget.Color = Color.Gray;
					}
				}
			}
			float num = float.MaxValue;
			foreach (Widget child2 in m_navigationPanel.Children)
			{
				if ((child2 as SimpleButtonWidget).Tag is Widget targetWidget)
				{
					float num2 = CalculateScrollPosition(targetWidget);
					float num3 = scrollPosition - num2;
					if (!(num3 >= -50f))
					{
					}
				}
			}
			SimpleButtonWidget simpleButtonWidget2 = null;
			foreach (Widget child3 in m_navigationPanel.Children)
			{
				SimpleButtonWidget simpleButtonWidget3 = child3 as SimpleButtonWidget;
				if (simpleButtonWidget3.Tag is Widget targetWidget2 && CalculateScrollPosition(targetWidget2) <= scrollPosition + 50f)
				{
					simpleButtonWidget2 = simpleButtonWidget3;
				}
			}
			if (simpleButtonWidget2 != null)
			{
				simpleButtonWidget2.Color = Color.White;
			}
			else if (m_navigationPanel.Children.Count > 0)
			{
				(m_navigationPanel.Children[0] as SimpleButtonWidget).Color = Color.White;
			}
		}
		base.Update();
		if (m_saveButton == null)
		{
			return;
		}
		try
		{
			foreach (PropertyBinding binding in m_bindings)
			{
				if (binding.BoolButton != null && binding.BoolButton.IsClicked)
				{
					bool flag = !(binding.BoolButton.Text == "TRUE");
					binding.BoolButton.Text = (flag ? "TRUE" : "FALSE");
					binding.BoolButton.Color = (flag ? Color.Green : Color.Red);
				}
				if (binding.TextBox == null || !binding.TextBox.HasFocus)
				{
					continue;
				}
				Type type = ((binding.PropInfo != null) ? binding.PropInfo.PropertyType : binding.FieldInfo.FieldType);
				if (type == typeof(int) || type == typeof(float))
				{
					string text = binding.TextBox.Text;
					string text2 = SanitizeNumericInput(text, type == typeof(float));
					if (text != text2)
					{
						binding.TextBox.Text = text2;
					}
				}
			}
			if (m_saveButton.IsClicked)
			{
				SaveChanges();
				m_savedScrollPositions[m_entity] = m_mainScroll.ScrollPosition;
				DialogsManager.HideDialog(this);
			}
			if (m_resetButtonWidget != null && m_resetButtonWidget.IsClicked)
			{
				ShowResetConfirmation();
			}
			if (m_cancelButton.IsClicked || Input.Cancel)
			{
				m_savedScrollPositions[m_entity] = m_mainScroll.ScrollPosition;
				DialogsManager.HideDialog(this);
			}
		}
		catch
		{
		}
	}

	private void SaveChanges()
	{
		foreach (PropertyBinding binding in m_bindings)
		{
			try
			{
				Type type;
				if (binding.CustomType != null)
				{
					type = binding.CustomType;
				}
				else
				{
					type = ((binding.PropInfo != null) ? binding.PropInfo.PropertyType : binding.FieldInfo.FieldType);
				}
				object obj = null;
				if (type == typeof(bool) && binding.BoolButton != null)
				{
					obj = binding.BoolButton.Text == "TRUE";
				}
				else if (binding.TextBox != null)
				{
					string text = binding.TextBox.Text;
					if (type == typeof(string))
					{
						obj = text;
					}
					else if (type == typeof(int))
					{
						obj = int.Parse(text);
					}
					else if (type == typeof(float))
					{
						obj = float.Parse(text);
					}
					else if (type == typeof(Vector3))
					{
						string[] array = text.Replace("<", "").Replace(">", "").Split(',');
						if (array.Length == 3)
						{
							obj = new Vector3(float.Parse(array[0].Trim()), float.Parse(array[1].Trim()), float.Parse(array[2].Trim()));
						}
					}
				}
				if (obj == null)
				{
					continue;
				}
				if (binding.CustomName != null && binding.Component is ComponentPlayer componentPlayer)
				{
					if (binding.CustomName == "Level")
					{
						componentPlayer.PlayerData.Level = (float)obj;
					}
					else if (binding.CustomName == "ServerManager")
					{
						componentPlayer.PlayerData.ServerManager = (bool)obj;
					}
					else if (binding.CustomName == "WorldEditManager")
					{
						componentPlayer.PlayerData.WorldEditManager = (bool)obj;
					}
				}
				else if (binding.PropInfo != null)
				{
					binding.PropInfo.SetValue(binding.Component, obj, null);
				}
				else if (binding.FieldInfo != null)
				{
					binding.FieldInfo.SetValue(binding.Component, obj);
				}
			}
			catch
			{
			}
		}
	}

	public EntityInspectorDialog(Entity entity)
	{
		try
		{
			m_entity = entity;
			XElement node = ContentManager.Get<XElement>("Dialogs/EntityInspectorDialog");
			LoadContents(this, node);
			m_titleLabel = Children.Find<LabelWidget>("Inspector.Title");
			m_listPanel = Children.Find<StackPanelWidget>("Inspector.List");
			m_navigationPanel = Children.Find<StackPanelWidget>("Inspector.Navigation");
			m_mainScroll = Children.Find<ScrollPanelWidget>("Inspector.Scroll");
			m_saveButton = Children.Find<ButtonWidget>("Inspector.Save");
			m_cancelButton = Children.Find<ButtonWidget>("Inspector.Cancel");
			m_resetButtonWidget = new SimpleButtonWidget
			{
				Text = "Reset",
				DesiredSize = new Vector2(100f, 40f),
				Margin = new Vector2(10f, 0f),
				HorizontalAlignment = WidgetAlignment.Far,
				VerticalAlignment = WidgetAlignment.Center,
				Color = Color.White
			};
			m_cancelButton.ParentWidget?.Children.Insert(0, m_resetButtonWidget);
			string text = "Unknown Entity";
			ComponentPlayer componentPlayer = entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null)
			{
				text = componentPlayer.PlayerData.Name;
			}
			else if (entity.ValuesDictionary.DatabaseObject != null)
			{
				text = entity.ValuesDictionary.DatabaseObject.Name;
			}
			m_titleLabel.Text = text;
			PopulateList();
			if (m_savedScrollPositions.ContainsKey(entity))
			{
				m_mainScroll.ScrollPosition = m_savedScrollPositions[entity];
			}
		}
		catch (Exception ex)
		{
			Log.Error("Init Error: " + ex.Message);
			DialogsManager.HideDialog(this);
		}
	}

	private void ShowResetConfirmation()
	{
		DialogsManager.ShowDialog(null, new MessageDialog("Reset Defaults", "Reset component values?", "Yes", "No", (MessageDialogButton button) =>
		{
			if (button == MessageDialogButton.Button1)
			{
				ResetToDefaults();
			}
		}));
	}

	private void ResetToDefaults()
	{
		try
		{
			foreach (PropertyBinding binding in m_bindings)
			{
				Type type = binding.Component.GetType();
				if (type.Name.Contains("Body") || type.Name.Contains("Transform"))
				{
					continue;
				}
				object obj = Activator.CreateInstance(type);
				if (obj != null)
				{
					object obj2 = null;
					if (binding.PropInfo != null && binding.PropInfo.CanRead)
					{
						obj2 = binding.PropInfo.GetValue(obj, null);
					}
					else if (binding.FieldInfo != null)
					{
						obj2 = binding.FieldInfo.GetValue(obj);
					}
					if (binding.TextBox != null)
					{
						binding.TextBox.Text = ((obj2 != null) ? obj2.ToString() : "null");
					}
					if (binding.BoolButton != null)
					{
						bool flag = obj2 is bool && (bool)obj2;
						binding.BoolButton.Text = (flag ? "TRUE" : "FALSE");
						binding.BoolButton.Color = (flag ? Color.Green : Color.Red);
					}
				}
			}
		}
		catch
		{
		}
	}

	static EntityInspectorDialog()
	{
		m_savedScrollPositions = new Dictionary<Entity, float>();
	}

	private string SanitizeNumericInput(string input, bool allowDecimal)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		string text = "0123456789-";
		if (allowDecimal)
		{
			text += ".,";
		}
		string text2 = "";
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (text.Contains(c.ToString()) && (c != '-' || text2.Length <= 0) && ((c != '.' && c != ',') || (!text2.Contains(".") && !text2.Contains(","))))
			{
				text2 += c;
			}
		}
		return text2;
	}

	private void AddEditorRow(Component component, PropertyInfo prop, FieldInfo field, string customName = null, Type customType = null, object customValue = null)
	{
		try
		{
			string text = customName ?? ((prop != null) ? prop.Name : field.Name);
			object obj;
			if (customName != null)
			{
				obj = customValue;
			}
			else
			{
				obj = ((prop != null) ? prop.GetValue(component, null) : field.GetValue(component));
			}
			string text2 = ((obj != null) ? obj.ToString() : "null");
			Type type = customType ?? ((prop != null) ? prop.PropertyType : field.FieldType);
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				VerticalAlignment = WidgetAlignment.Center,
				HorizontalAlignment = WidgetAlignment.Stretch,
				Margin = new Vector2(5f, 2f)
			};
			CanvasWidget canvasWidget = new CanvasWidget
			{
				Size = new Vector2(350f, 40f),
				VerticalAlignment = WidgetAlignment.Center
			};
			LabelWidget widget = new LabelWidget
			{
				Text = text,
				HorizontalAlignment = WidgetAlignment.Near,
				VerticalAlignment = WidgetAlignment.Center,
				Color = Color.LightGray,
				Ellipsis = true,
				Margin = new Vector2(10f, 0f)
			};
			canvasWidget.Children.Add(widget);
			TextBoxWidget textBoxWidget = null;
			Widget widget2;
			if (type == typeof(bool))
			{
				bool flag = (bool)obj;
				widget2 = new SimpleButtonWidget
				{
					Text = (flag ? "TRUE" : "FALSE"),
					DesiredSize = new Vector2(120f, 40f),
					VerticalAlignment = WidgetAlignment.Center,
					HorizontalAlignment = WidgetAlignment.Near,
					Margin = new Vector2(20f, 0f),
					Color = (flag ? Color.Green : Color.Red)
				};
			}
			else
			{
				CanvasWidget canvasWidget2 = new CanvasWidget
				{
					Size = new Vector2(400f, 40f),
					VerticalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(20f, 0f)
				};
				RectangleWidget widget3 = new RectangleWidget
				{
					FillColor = new Color(20, 20, 20),
					OutlineColor = new Color(100, 100, 100),
					OutlineThickness = 1f,
					Size = new Vector2(400f, 40f)
				};
				textBoxWidget = new TextBoxWidget
				{
					Text = text2,
					Size = new Vector2(380f, 40f),
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center,
					Color = Color.White,
					FontScale = 0.8f
				};
				canvasWidget2.Children.Add(widget3);
				canvasWidget2.Children.Add(textBoxWidget);
				widget2 = canvasWidget2;
			}
			stackPanelWidget.Children.Add(canvasWidget);
			stackPanelWidget.Children.Add(widget2);
			m_listPanel.Children.Add(stackPanelWidget);
			m_bindings.Add(new PropertyBinding
			{
				Component = component,
				PropInfo = prop,
				FieldInfo = field,
				TextBox = textBoxWidget,
				BoolButton = (widget2 as ButtonWidget),
				CustomName = customName,
				CustomType = customType
			});
		}
		catch (Exception ex)
		{
			Log.Error("AddRow Error: " + ex.Message);
		}
	}

	private float CalculateScrollPosition(Widget targetWidget)
	{
		float num = 0f;
		foreach (Widget child in m_listPanel.Children)
		{
			if (child == targetWidget)
			{
				return num;
			}
			float num2 = child.DesiredSize.Y;
			if (num2 == 0f)
			{
				num2 = 50f;
			}
			num += num2 + child.Margin.Y * 2f;
		}
		return num;
	}
}
