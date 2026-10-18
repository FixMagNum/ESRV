using System;
using System.Collections.Generic;
using System.Text;
using Engine;

namespace Game;

public class EditMemoryBankDialogAPI : Dialog
{
	public MemoryBankData memory;

	public DynamicArray<byte> Data = new DynamicArray<byte>();

	public StackPanelWidget MainView;

	public Action onCancel;

	public int clickpos;

	public bool isSetPos;

	public int setPosN;

	public int lastvalue;

	public bool isclick = true;

	public List<ClickTextWidget> list = new List<ClickTextWidget>();

	public byte LastOutput { get; set; }

	public EditMemoryBankDialogAPI(MemoryBankData memoryBankData, Action onCancel)
	{
		memory = memoryBankData;
		Data.Clear();
		Data.AddRange(memory.Data);
		CanvasWidget canvasWidget = new CanvasWidget
		{
			Size = new Vector2(600f, 500f),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
		RectangleWidget id = new RectangleWidget
		{
			FillColor = new Color(0, 0, 0, 255),
			OutlineColor = new Color(128, 128, 128, 128),
			OutlineThickness = 2f
		};
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical
		};
		LabelWidget widget = new LabelWidget
		{
			Text = LanguageControl.GetContentWidgets(GetType().Name, 0),
			HorizontalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(0f, 10f)
		};
		StackPanelWidget stackPanelWidget2 = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			HorizontalAlignment = WidgetAlignment.Near,
			VerticalAlignment = WidgetAlignment.Near,
			Margin = new Vector2(10f, 10f)
		};
		Children.Add(canvasWidget);
		canvasWidget.Children.Add(id);
		canvasWidget.Children.Add(stackPanelWidget);
		stackPanelWidget.Children.Add(widget);
		stackPanelWidget.Children.Add(stackPanelWidget2);
		stackPanelWidget2.Children.Add(initData());
		stackPanelWidget2.Children.Add(initButton());
		MainView = stackPanelWidget;
		this.onCancel = onCancel;
		lastvalue = memory.Read(0);
	}

	public byte Read(int address)
	{
		if (address >= 0 && address < Data.Count)
		{
			return Data.Array[address];
		}
		return 0;
	}

	public void Write(int address, byte data)
	{
		if (address >= 0 && address < Data.Count)
		{
			Data.Array[address] = data;
		}
		else if (address >= 0 && address < 256 && data != 0)
		{
			Data.Count = MathUtils.Max(Data.Count, address + 1);
			Data.Array[address] = data;
		}
	}

	public void LoadString(string data)
	{
		string[] array = data.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length >= 1)
		{
			string text = array[0];
			text = text.TrimEnd('0');
			Data.Clear();
			for (int i = 0; i < MathUtils.Min(text.Length, 256); i++)
			{
				int num = MemoryBankData.m_hexChars.IndexOf(char.ToUpperInvariant(text[i]));
				if (num < 0)
				{
					num = 0;
				}
				Data.Add((byte)num);
			}
		}
		if (array.Length >= 2)
		{
			string text2 = array[1];
			int num2 = MemoryBankData.m_hexChars.IndexOf(char.ToUpperInvariant(text2[0]));
			if (num2 < 0)
			{
				num2 = 0;
			}
			LastOutput = (byte)num2;
		}
	}

	public string SaveString()
	{
		return SaveString(saveLastOutput: true);
	}

	public string SaveString(bool saveLastOutput)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int count = Data.Count;
		for (int i = 0; i < count; i++)
		{
			int index = MathUtils.Clamp(Data.Array[i], 0, 15);
			stringBuilder.Append(MemoryBankData.m_hexChars[index]);
		}
		if (saveLastOutput)
		{
			stringBuilder.Append(';');
			stringBuilder.Append(MemoryBankData.m_hexChars[MathUtils.Clamp(LastOutput, 0, 15)]);
		}
		return stringBuilder.ToString();
	}

	public Widget initData()
	{
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			VerticalAlignment = WidgetAlignment.Center,
			HorizontalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(10f, 0f)
		};
		for (int i = 0; i < 17; i++)
		{
			StackPanelWidget stackPanelWidget2 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal
			};
			for (int j = 0; j < 17; j++)
			{
				int addr = (i - 1) * 16 + (j - 1);
				if (j > 0 && i > 0)
				{
					ClickTextWidget clickTextWidget = new ClickTextWidget(new Vector2(22f), $"{MemoryBankData.m_hexChars[Read(addr)]}", () =>
					{
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
						clickpos = addr;
						isclick = true;
					});
					list.Add(clickTextWidget);
					stackPanelWidget2.Children.Add(clickTextWidget);
					continue;
				}
				int num = 0;
				if (i == 0 && j > 0)
				{
					num = j - 1;
				}
				else
				{
					if (j != 0 || i <= 0)
					{
						ClickTextWidget widget = new ClickTextWidget(new Vector2(22f), "", null);
						stackPanelWidget2.Children.Add(widget);
						continue;
					}
					num = i - 1;
				}
				ClickTextWidget clickTextWidget2 = new ClickTextWidget(new Vector2(22f), MemoryBankData.m_hexChars[num].ToString(), () =>
				{
				});
				clickTextWidget2.labelWidget.Color = Color.DarkGray;
				stackPanelWidget2.Children.Add(clickTextWidget2);
			}
			stackPanelWidget.Children.Add(stackPanelWidget2);
		}
		return stackPanelWidget;
	}

	public Widget makeFuncButton(string txt, Action func)
	{
		return new ClickTextWidget(new Vector2(40f), txt, func, box: true)
		{
			BorderColor = Color.White,
			Margin = new Vector2(2f),
			labelWidget = 
			{
				FontScale = ((txt.Length > 1) ? 0.7f : 1f),
				Color = Color.White
			}
		};
	}

	public Widget initButton()
	{
		StackPanelWidget pickables = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical,
			VerticalAlignment = WidgetAlignment.Center,
			HorizontalAlignment = WidgetAlignment.Far,
			Margin = new Vector2(10f, 10f)
		};
		for (int num7 = 0; num7 < 6; num7++)
		{
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal
			};
			for (int i = 0; i < 3; i++)
			{
				int componentPlayer = num7 * 3 + i;
				if (componentPlayer < 15)
				{
					int pp = componentPlayer + 1;
					stackPanelWidget.Children.Add(makeFuncButton($"{MemoryBankData.m_hexChars[pp]}", () =>
					{
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
						if (!isSetPos)
						{
							Write(clickpos, (byte)pp);
							lastvalue = pp;
							clickpos++;
							if (clickpos >= 255)
							{
								clickpos = 0;
							}
							isclick = true;
						}
						else
						{
							if (setPosN == 0)
							{
								clickpos = 16 * pp;
							}
							else if (setPosN == 1)
							{
								clickpos += pp;
							}
							setPosN++;
							if (setPosN == 2)
							{
								if (clickpos > 255)
								{
									clickpos = 0;
								}
								setPosN = 0;
								isclick = true;
								isSetPos = false;
							}
						}
					}));
					continue;
				}
				switch (componentPlayer)
				{
				case 15:
					stackPanelWidget.Children.Add(makeFuncButton($"{MemoryBankData.m_hexChars[0]}", () =>
					{
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
						if (!isSetPos)
						{
							Write(clickpos, 0);
							lastvalue = 0;
							clickpos++;
							if (clickpos >= 255)
							{
								clickpos = 0;
							}
							isclick = true;
						}
						else
						{
							if (setPosN == 0)
							{
								clickpos = 0;
							}
							else if (setPosN == 1)
							{
								clickpos = clickpos;
							}
							setPosN++;
							if (setPosN == 2)
							{
								if (clickpos > 255)
								{
									clickpos = 0;
								}
								setPosN = 0;
								isclick = true;
								isSetPos = false;
							}
						}
					}));
					break;
				case 16:
					stackPanelWidget.Children.Add(makeFuncButton(LanguageControl.GetContentWidgets(GetType().Name, 1), () =>
					{
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
						for (int j = 0; j < Data.Count; j++)
						{
							Write(j, 0);
						}
						isclick = true;
					}));
					break;
				case 17:
					stackPanelWidget.Children.Add(makeFuncButton(LanguageControl.GetContentWidgets(GetType().Name, 2), () =>
					{
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
						DynamicArray<byte> dynamicArray = new DynamicArray<byte>();
						dynamicArray.AddRange(Data);
						dynamicArray.Count = 256;
						for (int j = 0; j < 16; j++)
						{
							for (int k = 0; k < 16; k++)
							{
								Write(j + k * 16, dynamicArray[j * 16 + k]);
							}
						}
						clickpos = 0;
						isclick = true;
					}));
					break;
				}
			}
			pickables.Children.Add(stackPanelWidget);
		}
		LabelWidget num8 = new LabelWidget
		{
			FontScale = 0.8f,
			Text = LanguageControl.GetContentWidgets(GetType().Name, 3),
			HorizontalAlignment = WidgetAlignment.Center,
			Margin = new Vector2(0f, 10f),
			Color = Color.DarkGray
		};
		pickables.Children.Add(num8);
		pickables.Children.Add(makeTextBox((TextBoxWidget textBoxWidget) =>
		{
			LoadString(textBoxWidget.Text);
			isclick = true;
		}, memory.SaveString(saveLastOutput: false)));
		pickables.Children.Add(makeButton(LanguageControl.GetContentWidgets(GetType().Name, 4), () =>
		{
			for (int j = 0; j < Data.Count; j++)
			{
				memory.Write(j, Data[j]);
			}
			onCancel?.Invoke();
			AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			DialogsManager.HideDialog(this);
		}));
		pickables.Children.Add(makeButton(LanguageControl.GetContentWidgets(GetType().Name, 5), () =>
		{
			AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			DialogsManager.HideDialog(this);
			isclick = true;
		}));
		return pickables;
	}

	public Widget makeTextBox(Action<TextBoxWidget> ac, string text = "")
	{
		CanvasWidget canvasWidget = new CanvasWidget
		{
			HorizontalAlignment = WidgetAlignment.Center
		};
		RectangleWidget widget = new RectangleWidget
		{
			FillColor = Color.Black,
			OutlineColor = Color.White,
			Size = new Vector2(120f, 30f)
		};
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Vertical
		};
		TextBoxWidget textBoxWidget = new TextBoxWidget
		{
			VerticalAlignment = WidgetAlignment.Center,
			Color = new Color(255, 255, 255),
			Margin = new Vector2(4f, 0f),
			Size = new Vector2(120f, 30f),
			MaximumLength = 256
		};
		textBoxWidget.FontScale = 0.7f;
		textBoxWidget.Text = text;
		textBoxWidget.TextChanged += ac;
		stackPanelWidget.Children.Add(textBoxWidget);
		canvasWidget.Children.Add(widget);
		canvasWidget.Children.Add(stackPanelWidget);
		return canvasWidget;
	}

	public Widget makeButton(string txt, Action tas)
	{
		return new ClickTextWidget(new Vector2(120f, 30f), txt, tas)
		{
			BorderColor = Color.White,
			Margin = new Vector2(0f, 3f),
			labelWidget = 
			{
				FontScale = 0.7f,
				Color = Color.Green
			}
		};
	}

	public override void Update()
	{
		if (Input.Back || Input.Cancel)
		{
			DialogsManager.HideDialog(this);
		}
		if (isSetPos)
		{
			list[clickpos].BorderColor = Color.Red;
		}
		else
		{
			if (!isclick)
			{
				return;
			}
			int num = clickpos / 16;
			int num2 = clickpos % 16;
			Color fillColor = new Color(42, 42, 42, 60);
			for (int i = 0; i < list.Count; i++)
			{
				int num3 = i / 16;
				int num4 = i % 16;
				list[i].BorderColor = Color.Transparent;
				list[i].rectangleWidget.FillColor = Color.Transparent;
				if (i == clickpos)
				{
					list[i].BorderColor = Color.Yellow;
				}
				else if (num3 == num || num4 == num2)
				{
					list[i].rectangleWidget.FillColor = fillColor;
				}
				list[i].labelWidget.Text = $"{MemoryBankData.m_hexChars[Read(i)]}";
			}
			isclick = false;
		}
	}
}
