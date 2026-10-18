using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Engine;
using Engine.Graphics;
using Engine.Serialization;

namespace Game;

public class SimpleTextParse
{
	public class Item
	{
		public string BeforeText;

		public string Name;

		public string Content;

		public StringBuilder Caches = new StringBuilder();

		public string Value;

		public void SetName(string name)
		{
			string[] num = name.Split(new char[1] { '=' }, StringSplitOptions.None);
			if (num.Length == 2)
			{
				Name = num[0];
				Value = num[1];
			}
			else
			{
				Name = name;
			}
		}

		public void SetContent(string text)
		{
			Content = Format(text);
		}

		public void SetBeforeText(string text)
		{
			BeforeText = Format(text);
		}

		public string Format(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}
			return text.Replace("\\<", "<").Replace("\\>", ">").Replace("\\\\", "\\");
		}
	}

	public static Dictionary<string, Color> colorMap;

	public static AutoCanvasWidget Parse(string text)
	{
		AutoCanvasWidget autoCanvasWidget = new AutoCanvasWidget
		{
			Size = new Vector2(660f, 180f)
		};
		try
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			int num = 0;
			List<Item> list = new List<Item>();
			Item item = null;
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				switch (c)
				{
				case '<':
					if (!flag)
					{
						if (flag2)
						{
							item.SetContent(item.Caches.ToString());
							flag2 = false;
							flag3 = true;
							item.Caches.Clear();
						}
						else
						{
							flag = true;
							item = new Item();
							string beforeText = text.Substring(num, i - num);
							num = i + 1;
							item.SetBeforeText(beforeText);
						}
						break;
					}
					throw new Exception("invalid char " + c + " at position " + i);
				case '\\':
					i++;
					break;
				case '>':
					if (flag)
					{
						string name = text.Substring(num, i - num);
						item.SetName(name);
						flag = false;
						flag2 = true;
						break;
					}
					if (flag4)
					{
						string text2 = item.Caches.ToString();
						item.Caches.Clear();
						list.Add(item);
						flag3 = false;
						flag4 = false;
						num = i + 1;
						if (item.Name != text2)
						{
							throw new Exception("invalid element " + text2 + " <> " + item.Name);
						}
						item = new Item();
						break;
					}
					throw new Exception("invalid char " + c + " at position " + i);
				default:
					if (flag2)
					{
						item.Caches.Append(c);
					}
					if (!flag3)
					{
						break;
					}
					if (flag4)
					{
						item.Caches.Append(c);
						break;
					}
					if (c != '/')
					{
						throw new Exception("invalid char " + c + " at position " + i);
					}
					num = i + 1;
					flag4 = true;
					break;
				}
			}
			if (num != text.Length)
			{
				item = new Item();
				item.SetBeforeText(text.Substring(num, text.Length - num));
				list.Add(item);
			}
			foreach (Item item2 in list)
			{
				LabelWidget widget = new LabelWidget
				{
					Text = item2.BeforeText,
					Margin = new Vector2(0f, -5f),
					FontScale = 1f,
					WordWrap = true
				};
				autoCanvasWidget.Add(widget);
				if (string.IsNullOrEmpty(item2.Name))
				{
					continue;
				}
				switch (item2.Name)
				{
				case "c":
				{
					LabelWidget labelWidget = new LabelWidget
					{
						Text = item2.Content,
						Margin = new Vector2(0f, -5f),
						Color = Color.White,
						FontScale = 1f,
						WordWrap = true
					};
					if (colorMap.TryGetValue(item2.Value.ToLower(), out var value))
					{
						labelWidget.Color = value;
					}
					else
					{
						HumanReadableConverter.TryConvertFromString(typeof(Color), item2.Content, out var result3);
						Color color = (Color)result3;
						labelWidget.Color = color;
					}
					autoCanvasWidget.Add(labelWidget);
					break;
				}
				case "em":
				{
					int.TryParse(item2.Content, out var result2);
					result2 = MathUtils.Clamp(result2, 10, 90);
					RectangleWidget rectangleWidget = new RectangleWidget();
					rectangleWidget.Size = new Vector2(24f);
					rectangleWidget.VerticalAlignment = WidgetAlignment.Far;
					rectangleWidget.HorizontalAlignment = WidgetAlignment.Center;
					rectangleWidget.FillColor = Color.White;
					rectangleWidget.OutlineColor = Color.Transparent;
					rectangleWidget.Subtexture = new Subtexture(ContentManager.Get<Texture2D>("Textures/emojis/" + result2), Vector2.Zero, Vector2.One);
					autoCanvasWidget.Add(rectangleWidget);
					break;
				}
				case "b":
				{
					int.TryParse(item2.Content, out var result);
					BlockIconWidget blockIconWidget = new BlockIconWidget
					{
						VerticalAlignment = WidgetAlignment.Center
					};
					blockIconWidget.Size = new Vector2(24f);
					blockIconWidget.Value = result;
					autoCanvasWidget.Children.Add(blockIconWidget);
					break;
				}
				}
			}
		}
		catch
		{
			autoCanvasWidget.Children.Add(new LabelWidget
			{
				FontScale = 1f,
				Text = text,
				WordWrap = true,
				HorizontalAlignment = WidgetAlignment.Center
			});
		}
		return autoCanvasWidget;
	}

	static SimpleTextParse()
	{
		colorMap = new Dictionary<string, Color>();
		Type typeFromHandle = typeof(Color);
		Color color = default;
		FieldInfo[] fields = typeFromHandle.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.IsStatic)
			{
				colorMap.Add(fieldInfo.Name.ToLower(), (Color)fieldInfo.GetValue(color));
			}
		}
	}
}
