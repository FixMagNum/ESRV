using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using SimpleJson;

namespace Game;

public class LoginDialog : Dialog
{
	public StackPanelWidget MainView = new StackPanelWidget
	{
		Direction = LayoutDirection.Vertical,
		HorizontalAlignment = WidgetAlignment.Center,
		VerticalAlignment = WidgetAlignment.Near,
		Margin = new Vector2(10f, 10f)
	};

	public BevelledButtonWidget LoginBtn;

	public BevelledButtonWidget RegBtn;

	public BevelledButtonWidget CancelBtn;

	public TextBoxWidget AccountInput;

	public TextBoxWidget PasswordInput;

	public LabelWidget Tip = new LabelWidget
	{
		HorizontalAlignment = WidgetAlignment.Center,
		VerticalAlignment = WidgetAlignment.Near,
		Size = new Vector2(-1f, 48f)
	};

	public LoginDialog()
	{
		Size = new Vector2(600f, 320f);
		HorizontalAlignment = WidgetAlignment.Center;
		VerticalAlignment = WidgetAlignment.Center;
		RectangleWidget widget = new RectangleWidget
		{
			FillColor = new Color(0, 0, 0, 255),
			OutlineColor = new Color(128, 128, 128, 128),
			OutlineThickness = 2f
		};
		Children.Add(widget);
		Children.Add(MainView);
		MainView.Children.Add(Tip);
		MainView.Children.Add(makeTextBox("用户名:"));
		MainView.Children.Add(makeTextBox("密  码:"));
		MainView.Children.Add(makeButton());
	}

	public void OnFail(Exception ex)
	{
		LoginBtn.Text = "登录";
		Tip.Text = ex.Message.ToString();
	}

	public void OnSuccess(byte[] data)
	{
		LoginBtn.Text = "登录";
		JsonObject jsonObject = (JsonObject)global::SimpleJson.SimpleJson.DeserializeObject(new StreamReader(new MemoryStream(data)).ReadToEnd());
		int num = int.Parse(jsonObject["code"].ToString());
		string text = jsonObject["msg"].ToString();
		if (num == 200)
		{
			SettingsManager.CommunityAccessToken = ((JsonObject)jsonObject["data"])["accessToken"].ToString();
			DialogsManager.HideAllDialogs();
		}
		else
		{
			Tip.Text = text;
		}
	}

	public Widget makeTextBox(string title)
	{
		CanvasWidget canvasWidget = new CanvasWidget
		{
			Margin = new Vector2(10f)
		};
		RectangleWidget widget = new RectangleWidget
		{
			FillColor = Color.Black,
			OutlineColor = Color.White
		};
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			VerticalAlignment = WidgetAlignment.Center
		};
		LabelWidget widget2 = new LabelWidget
		{
			HorizontalAlignment = WidgetAlignment.Near,
			VerticalAlignment = WidgetAlignment.Center,
			Text = title,
			Size = new Vector2(80f, -1f)
		};
		TextBoxWidget textBoxWidget = new TextBoxWidget
		{
			HorizontalAlignment = WidgetAlignment.Near,
			VerticalAlignment = WidgetAlignment.Center,
			Color = new Color(255, 255, 255),
			Margin = new Vector2(4f, 0f),
			Size = new Vector2(float.PositiveInfinity, 80f)
		};
		if (title == "用户名:")
		{
			AccountInput = textBoxWidget;
		}
		if (title == "密  码:")
		{
			PasswordInput = textBoxWidget;
		}
		stackPanelWidget.Children.Add(widget2);
		stackPanelWidget.Children.Add(canvasWidget);
		canvasWidget.Children.Add(widget);
		canvasWidget.Children.Add(textBoxWidget);
		return stackPanelWidget;
	}

	public Widget makeButton()
	{
		StackPanelWidget stackPanelWidget = new StackPanelWidget
		{
			Direction = LayoutDirection.Horizontal,
			Margin = new Vector2(0f, 10f),
			HorizontalAlignment = WidgetAlignment.Center
		};
		LoginBtn = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 60f),
			Margin = new Vector2(4f, 0f),
			Text = "登陆"
		};
		RegBtn = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 60f),
			Margin = new Vector2(4f, 0f),
			Text = "注册"
		};
		CancelBtn = new BevelledButtonWidget
		{
			Size = new Vector2(160f, 60f),
			Margin = new Vector2(4f, 0f),
			Text = "取消"
		};
		stackPanelWidget.Children.Add(LoginBtn);
		stackPanelWidget.Children.Add(RegBtn);
		stackPanelWidget.Children.Add(CancelBtn);
		return stackPanelWidget;
	}

	public override void Update()
	{
		if (LoginBtn.IsClicked)
		{
			LoginBtn.Text = "登录中...";
			Dictionary<string, string> parameters = new Dictionary<string, string>
			{
				{ "user", AccountInput.Text },
				{ "pass", PasswordInput.Text }
			};
			WebManager.Post("https://m.schub.top/com/api/login", parameters, null, new MemoryStream(), new CancellableProgress(), OnSuccess, OnFail);
		}
		if (RegBtn.IsClicked)
		{
			WebBrowserManager.LaunchBrowser("https://m.schub.top/com/reg");
		}
		if (CancelBtn.IsClicked)
		{
			DialogsManager.HideDialog(this);
		}
	}
}
