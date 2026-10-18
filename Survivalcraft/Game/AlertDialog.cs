using System;
using System.Xml.Linq;
using Engine;

namespace Game;

public class AlertDialog : Dialog
{
	public Action dismiss_handler;

	public Action ok_handler;

	public LabelWidget m_largeLabelWidget;

	public LabelWidget m_smallLabelWidget;

	public ButtonWidget m_button1Widget;

	public ButtonWidget m_button2Widget;

	public ScrollPanelWidget scrollPanel;

	public CanvasWidget CanvasWidget = new CanvasWidget();

	public bool AutoHide { get; set; }

	public AlertDialog(string largeMessage, string smallMessage, string button1Text, string button2Text, Action ok = null, Action dismiss = null)
	{
		ok_handler = ok;
		dismiss_handler = dismiss;
		if (string.IsNullOrEmpty(button2Text) && dismiss != null && ok == null)
		{
			ok_handler = dismiss;
		}
		XElement node = ContentManager.Get<XElement>("Dialogs/MessageDialog");
		LoadContents(this, node);
		Vector2 vector = new Vector2(-1f);
		Size = new Vector2((vector.X >= 0f) ? vector.X : Size.X, (vector.Y >= 0f) ? vector.Y : Size.Y);
		CanvasWidget.Size = new Vector2(vector.X, 80f);
		m_largeLabelWidget = Children.Find<LabelWidget>("MessageDialog.LargeLabel");
		m_smallLabelWidget = Children.Find<LabelWidget>("MessageDialog.SmallLabel");
		m_button1Widget = Children.Find<ButtonWidget>("MessageDialog.Button1");
		m_button2Widget = Children.Find<ButtonWidget>("MessageDialog.Button2");
		scrollPanel = Children.Find<ScrollPanelWidget>();
		m_largeLabelWidget.IsVisible = !string.IsNullOrEmpty(largeMessage);
		m_largeLabelWidget.Text = largeMessage ?? string.Empty;
		m_smallLabelWidget.IsVisible = !string.IsNullOrEmpty(smallMessage);
		m_smallLabelWidget.Text = smallMessage ?? string.Empty;
		m_button1Widget.IsVisible = !string.IsNullOrEmpty(button1Text);
		m_button1Widget.Text = button1Text ?? string.Empty;
		m_button2Widget.IsVisible = !string.IsNullOrEmpty(button2Text);
		m_button2Widget.Text = button2Text ?? string.Empty;
		if (!m_button1Widget.IsVisible && !m_button2Widget.IsVisible)
		{
			throw new InvalidOperationException("MessageDialog must have at least one button.");
		}
		m_smallLabelWidget.HorizontalAlignment = WidgetAlignment.Center;
		scrollPanel.Children.Clear();
		scrollPanel.Children.Add(CanvasWidget);
		SetContent(smallMessage);
		AutoHide = true;
	}

	public void SetContent(Widget widget)
	{
		CanvasWidget.Children.Clear();
		CanvasWidget.Children.Add(m_smallLabelWidget);
		CanvasWidget.Children.Add(widget);
	}

	public void SetContent(string text)
	{
		CanvasWidget.Children.Clear();
		m_smallLabelWidget.Text = text;
		CanvasWidget.Children.Add(m_smallLabelWidget);
	}

	public override void Update()
	{
		if (Input.Cancel)
		{
			Dismiss();
		}
		else if (Input.Ok || m_button1Widget.IsClicked)
		{
			Ok();
		}
		else if (m_button2Widget.IsClicked)
		{
			Dismiss();
		}
	}

	public void Ok()
	{
		ok_handler?.Invoke();
	}

	public void Dismiss()
	{
		if (AutoHide)
		{
			DialogsManager.HideDialog(this);
		}
		dismiss_handler?.Invoke();
	}
}
