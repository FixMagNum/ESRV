using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

public class MessageWidget : CanvasWidget
{
	private class MessageEntry
	{
		public string Message;

		public double StartTime;

		public float Duration;

		public Color Color;

		public bool Blinking;

		public AutoCanvasWidget LabelWidget;
	}

	private List<MessageEntry> m_messages = new List<MessageEntry>();

	public void DisplayMessage(string text, Color color, bool blinking)
	{
		if (m_messages.Count >= 3)
		{
			MessageEntry messageEntry = m_messages[0];
			Children.Remove(messageEntry.LabelWidget);
			m_messages.RemoveAt(0);
		}
		MessageEntry messageEntry2 = new MessageEntry
		{
			Message = text,
			StartTime = Time.RealTime,
			Color = color,
			Blinking = blinking,
			LabelWidget = SimpleTextParse.Parse(text)
		};
		messageEntry2.Duration = (blinking ? 6f : (4f + MathUtils.Min(1f * (float)text.Count((char c) => c == '\n'), 4f)));
		messageEntry2.LabelWidget.HorizontalAlignment = WidgetAlignment.Center;
		messageEntry2.LabelWidget.VerticalAlignment = WidgetAlignment.Far;
		Children.Add(messageEntry2.LabelWidget);
		m_messages.Add(messageEntry2);
	}

	public override void Update()
	{
		double realTime = Time.RealTime;
		for (int num = m_messages.Count - 1; num >= 0; num--)
		{
			MessageEntry messageEntry = m_messages[num];
			float num2;
			if (messageEntry.Blinking)
			{
				num2 = MathUtils.Saturate(1f * (float)(messageEntry.StartTime + (double)messageEntry.Duration - realTime));
				if (realTime - messageEntry.StartTime < 0.417)
				{
					num2 *= MathUtils.Lerp(0.25f, 1f, 0.5f * (1f - MathUtils.Cos((float)Math.PI * 12f * (float)(realTime - messageEntry.StartTime))));
				}
			}
			else
			{
				num2 = MathUtils.Saturate(MathUtils.Min(3f * (float)(realTime - messageEntry.StartTime), 1f * (float)(messageEntry.StartTime + (double)messageEntry.Duration - realTime)));
			}
			messageEntry.LabelWidget.ColorTransform = messageEntry.Color * num2;
			messageEntry.LabelWidget.IsVisible = true;
			if (realTime - messageEntry.StartTime > (double)messageEntry.Duration)
			{
				Children.Remove(messageEntry.LabelWidget);
				m_messages.RemoveAt(num);
			}
			else
			{
				messageEntry.LabelWidget.Margin = new Vector2(0f, 30f * (float)(m_messages.Count - num - 1));
			}
		}
	}
}
