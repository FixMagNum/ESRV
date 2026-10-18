using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class NetMessageWidget : CanvasWidget, IDragTargetWidget
{
	private BevelledRectangleWidget rectangleWidget;

	private ResizableListWidget MessageList;

	private CanvasWidget canvas_msglist;

	private SubsystemGameWidgets m_subsystemGameWidgets;

	private CanvasWidget canvas_inputerea;

	private BevelledRectangleWidget Bevelled;

	public TextBoxWidget EditText;

	private LabelWidget Hint;

	private BevelledButtonWidget SendMessageButton;

	private BevelledButtonWidget AddEmojiButton;

	private BevelledButtonWidget MessageTypeButton;

	private ListPanelWidget VLine;

	private NetPlayerListWidget m_netPlayerListWidget;

	public CanvasWidget MoreCanvas;

	private byte MessageType;

	public static string fName;

	private BevelledButtonWidget SendToVoidButton;

	private double m_lastSendTime = -1000.0;

	private double m_secondLastSendTime = -1000.0;

	private bool m_autoModActive;

	private double m_autoModWindowEnd;

	private int m_sentInWindow;

	private string m_sendButtonDefaultText;

	private PlayerData PlayerData { get; set; }

	public NetMessageWidget(PlayerData playerData, NetPlayerListWidget netPlayerListWidget)
	{
		rectangleWidget = new BevelledRectangleWidget
		{
			CenterColor = new Color(0, 0, 0, 75),
			BevelColor = Color.White,
			BevelSize = 1f
		};
		MessageList = new ResizableListWidget
		{
			Direction = LayoutDirection.Vertical
		};
		canvas_msglist = new CanvasWidget
		{
			HorizontalAlignment = WidgetAlignment.Center
		};
		canvas_inputerea = new CanvasWidget
		{
			VerticalAlignment = WidgetAlignment.Far
		};
		Bevelled = new BevelledRectangleWidget
		{
			CenterColor = new Color(0, 0, 0, 124),
			BevelColor = Color.White,
			VerticalAlignment = WidgetAlignment.Center,
			BevelSize = 2f
		};
		EditText = new TextBoxWidget
		{
			FontScale = 1f,
			Margin = new Vector2(5f, 0f)
		};
		Hint = new LabelWidget
		{
			FontScale = 1f,
			VerticalAlignment = WidgetAlignment.Center,
			Text = LanguageControl.Get(fName, 1),
			Color = new Color(125, 125, 125),
			Margin = new Vector2(5f, 0f)
		};
		SendMessageButton = new BevelledButtonWidget
		{
			CenterColor = Color.SkyBlue,
			BevelColor = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Far,
			Text = LanguageControl.Get(fName, 2)
		};
		AddEmojiButton = new BevelledButtonWidget
		{
			CenterColor = Color.SkyBlue,
			BevelColor = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Far,
			Text = LanguageControl.Get(fName, 3)
		};
		MessageTypeButton = new BevelledButtonWidget
		{
			CenterColor = Color.SkyBlue,
			BevelColor = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Far,
			Text = LanguageControl.Get(fName, 4)
		};
		VLine = new ListPanelWidget
		{
			Direction = LayoutDirection.Vertical
		};
		MoreCanvas = new CanvasWidget
		{
			IsVisible = false
		};
		PlayerData = playerData;
		m_netPlayerListWidget = netPlayerListWidget;
		Size = new Vector2(660f, 240f);
		canvas_msglist.Size = new Vector2(Size.X, 188f);
		MoreCanvas.Size = canvas_msglist.Size;
		canvas_msglist.Margin = new Vector2(5f, 0f);
		canvas_inputerea.Size = new Vector2(Size.X - 144f, 48f);
		Bevelled.Size = new Vector2(Size.X, 48f);
		EditText.Size = Bevelled.Size;
		SendMessageButton.Size = new Vector2(60f, 60f);
		SendMessageButton.Margin = new Vector2(-6f);
		AddEmojiButton.Size = new Vector2(60f, 60f);
		AddEmojiButton.Margin = new Vector2(-6f);
		MessageTypeButton.Size = new Vector2(60f, 60f);
		MessageTypeButton.Margin = new Vector2(-6f);
		SendToVoidButton = new BevelledButtonWidget
		{
			CenterColor = Color.Gray,
			BevelColor = Color.White,
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Far,
			Text = LanguageControl.Get(fName, 11)
		};
		SendToVoidButton.Size = new Vector2(60f, 60f);
		SendToVoidButton.Margin = new Vector2(-6f);
		HorizontalAlignment = WidgetAlignment.Center;
		VerticalAlignment = WidgetAlignment.Near;
		MoreCanvas.Children.Add(VLine);
		Children.Add(rectangleWidget);
		canvas_inputerea.Children.Add(Bevelled);
		canvas_inputerea.Children.Add(EditText);
		canvas_inputerea.Children.Add(Hint);
		StackPanelWidget widget = new StackPanelWidget
		{
			Margin = Vector2.Zero,
			VerticalAlignment = WidgetAlignment.Far,
			Direction = LayoutDirection.Horizontal,
			Children = 
			{
				(Widget)canvas_inputerea,
				(Widget)MessageTypeButton,
				(Widget)AddEmojiButton,
				(Widget)SendMessageButton,
				(Widget)SendToVoidButton
			}
		};
		Children.Add(canvas_msglist);
		Children.Add(widget);
		Children.Add(MoreCanvas);
		canvas_msglist.Children.Add(MessageList);
		MessageList.ItemWidgetFactory = (object obj) => SimpleTextParse.Parse(obj.ToString());
		VLine.ItemWidgetFactory = (object obj) => obj as Widget;
		for (int num = 1; num < 9; num++)
		{
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal
			};
			for (int num2 = 0; num2 < 11; num2++)
			{
				int num3 = num2 + num * 10;
				if (num3 > 19)
				{
					break;
				}
				BitmapButtonWidget bitmapButtonWidget = new BitmapButtonWidget();
				bitmapButtonWidget.Size = new Vector2(54f);
				bitmapButtonWidget.NormalSubtexture = new Subtexture(ContentManager.Get<Texture2D>("Textures/emojis/" + num3), Vector2.Zero, Vector2.One);
				bitmapButtonWidget.ClickedSubtexture = bitmapButtonWidget.NormalSubtexture;
				int localId = num3;
				bitmapButtonWidget.m_clickableWidget.OnClick = () =>
				{
					TextBoxWidget editText = EditText;
					editText.Text = editText.Text + "<em>" + localId + "</em>";
					MoreCanvas.IsVisible = false;
				};
				stackPanelWidget.Children.Add(bitmapButtonWidget);
			}
			VLine.AddItem(stackPanelWidget);
		}
		m_subsystemGameWidgets = playerData.SubsystemGameWidgets;
		m_subsystemGameWidgets.OnMessageRecieved += AddNetMsg;
	}

	private void AddNetMsg(string msg)
	{
		MessageList.AddItem(msg);
		if (MessageList.Items.Count > 200)
		{
			MessageList.RemoveItemAt(0);
		}
		MessageList.ScrollToItem(MessageList.Items[MessageList.Items.Count - 1]);
		ComponentPlayer componentPlayer = PlayerData.ComponentPlayer;
		if (componentPlayer != null)
		{
			string text = "\u200b";
			componentPlayer.ComponentGui.DisplaySmallMessage(msg + text, Color.White, blinking: true, playNotificationSound: true);
		}
	}

	public override void Update()
	{
		if (m_sendButtonDefaultText == null)
		{
			m_sendButtonDefaultText = SendMessageButton.Text;
		}
		if (EditText.HasFocus && KeyBindings.IsActionDownOnce(Input, KeyAction.SendChat))
		{
			SendMessageButton.m_clickableWidget.IsClicked = true;
		}
		if (MessageTypeButton.IsClicked)
		{
			switch (MessageType)
			{
			case 0:
				MessageType = 1;
				MessageTypeButton.Text = LanguageControl.Get(fName, 5);
				break;
			case 1:
				MessageType = 2;
				MessageTypeButton.Text = LanguageControl.Get(fName, 6);
				break;
			case 2:
				MessageType = 3;
				MessageTypeButton.Text = LanguageControl.Get(fName, 10);
				break;
			case 3:
				MessageType = 0;
				MessageTypeButton.Text = LanguageControl.Get(fName, 4);
				break;
			}
		}
		if (AddEmojiButton.IsClicked)
		{
			MoreCanvas.IsVisible = !MoreCanvas.IsVisible;
		}
		if (SendToVoidButton.IsClicked)
		{
			DialogsManager.ShowDialog(null, new MessageDialog("Confirmation", "Are you sure you want to clear the chat?", "Yes", "No", (MessageDialogButton button) =>
			{
				if (button == MessageDialogButton.Button1)
				{
					MessageList.ClearItems();
				}
			}));
		}
		double frameStartTime = Time.FrameStartTime;
		bool flag = PlayerData != null && PlayerData.ServerManager;
		if (m_autoModActive && frameStartTime > m_autoModWindowEnd)
		{
			m_autoModActive = false;
			m_sentInWindow = 0;
		}
		if (!flag && m_autoModActive)
		{
			int num = m_sentInWindow + 1;
			double num2 = ((num <= 3) ? 0.0 : (num switch
			{
				4 => 3.5, 
				5 => 5.0, 
				6 => 5.5, 
				7 => 7.0, 
				_ => 9.5, 
			}));
			double num3 = Math.Max(0.0, m_lastSendTime + num2 - frameStartTime);
			if (num3 > 0.0 && num2 > 0.0)
			{
				int num4 = Math.Max(1, (int)Math.Ceiling(num3));
				SendMessageButton.Text = num4.ToString();
			}
			else
			{
				SendMessageButton.Text = m_sendButtonDefaultText;
			}
		}
		else
		{
			SendMessageButton.Text = m_sendButtonDefaultText;
		}
		if (SendMessageButton.IsClicked)
		{
			if (MessageType == 3)
			{
				EditText.Text = string.Empty;
				return;
			}
			if (MessageType == 2 && !m_netPlayerListWidget.players.SelectedIndex.HasValue)
			{
				AddNetMsg("<c=red>" + LanguageControl.Get(fName, 7) + "</c>");
			}
			else if (MessageType == 1 && PlayerData.Friends.Count == 0)
			{
				AddNetMsg("<c=red>" + LanguageControl.Get(fName, 8) + "</c>");
			}
			else
			{
				List<byte> list = new List<byte>();
				switch (MessageType)
				{
				case 1:
					foreach (Guid item in PlayerData.Friends)
					{
						PlayerData playerData2 = PlayerData.SubsystemPlayers.FindPlayerData((PlayerData p) => p.PlayerGUID == item);
						if (playerData2 != null && playerData2.ClientId.HasValue)
						{
							list.Add(playerData2.ClientId.Value);
						}
					}
					break;
				case 2:
				{
					PlayerData playerData = m_netPlayerListWidget.players.Items[m_netPlayerListWidget.players.SelectedIndex.Value] as PlayerData;
					if (playerData.ClientId.HasValue)
					{
						if (PlayerData.Friends.Contains(playerData.PlayerGUID))
						{
							list.Add(playerData.ClientId.Value);
						}
						else
						{
							AddNetMsg("<c=red>To start a private chat with a player, you need to become friends first.</c>");
						}
					}
					break;
				}
				}
				if (!string.IsNullOrEmpty(EditText.Text))
				{
					if (PlayerData.Project.FindSubsystem<SubsystemPlayers>().NoMsgPlayerGuidList.Contains(PlayerData.PlayerGUID.ToString()))
					{
						DialogsManager.Alert(LanguageControl.Get(fName, 9));
					}
					else
					{
						if (!flag && m_autoModActive)
						{
							int num5 = m_sentInWindow + 1;
							double num6 = ((num5 <= 3) ? 0.0 : (num5 switch
							{
								4 => 3.5, 
								5 => 5.0, 
								6 => 5.5, 
								7 => 7.0, 
								_ => 9.5, 
							}));
							double num7 = m_lastSendTime + num6;
							if (frameStartTime < num7)
							{
								return;
							}
						}
						m_subsystemGameWidgets.AddMessage(EditText.Text, PlayerData.Name, MessageType, list);
						EditText.Text = string.Empty;
						if (!flag)
						{
							bool flag2 = !m_autoModActive && m_lastSendTime > -0.5 && frameStartTime - m_lastSendTime <= 2.0;
							if (m_autoModActive)
							{
								m_sentInWindow++;
								m_autoModWindowEnd = frameStartTime + 10.0;
							}
							else if (flag2)
							{
								m_autoModActive = true;
								m_sentInWindow = 2;
								m_autoModWindowEnd = frameStartTime + 10.0;
							}
						}
						else
						{
							m_autoModActive = false;
							m_sentInWindow = 0;
						}
						m_secondLastSendTime = m_lastSendTime;
						m_lastSendTime = frameStartTime;
					}
				}
			}
		}
		Hint.IsVisible = EditText.Text == string.Empty;
		if (!IsVisible)
		{
			PlayerData.ComponentPlayer.ComponentInput.AllowHandleInput = true;
		}
		else
		{
			PlayerData.ComponentPlayer.ComponentInput.AllowHandleInput = false;
		}
	}

	public void DragOver(Widget dragWidget, object data)
	{
	}

	public void DragDrop(Widget dragWidget, object data)
	{
		if (data is InventoryDragData inventoryDragData)
		{
			int slotValue = inventoryDragData.Inventory.GetSlotValue(inventoryDragData.SlotIndex);
			EditText.Text += $"<b>{slotValue}</b>";
		}
	}

	static NetMessageWidget()
	{
		fName = "NetMessageWidget";
	}
}
