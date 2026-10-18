using Engine;

namespace Game;

public class RecipeSlotPreviewWidget : CanvasWidget
{
	private CanvasWidget m_root;

	private BlockIconWidget m_icon;

	private LabelWidget m_count;

	private LabelWidget m_hint;

	private int m_value;

	private int m_countInt;

	public RecipeSlotPreviewWidget()
	{
		m_root = new CanvasWidget();
		m_icon = new BlockIconWidget();
		m_count = new LabelWidget();
		m_hint = new LabelWidget();
		Children.Add(m_root);
		m_root.Size = new Vector2(72f, 72f);
		m_icon.Size = new Vector2(48f, 48f);
		m_icon.HorizontalAlignment = WidgetAlignment.Center;
		m_icon.VerticalAlignment = WidgetAlignment.Center;
		m_count.Color = new Color(255, 255, 255, 220);
		m_count.HorizontalAlignment = WidgetAlignment.Far;
		m_count.VerticalAlignment = WidgetAlignment.Near;
		m_count.Margin = new Vector2(0f, 0f);
		m_hint.Color = new Color(255, 255, 255, 160);
		m_hint.HorizontalAlignment = WidgetAlignment.Center;
		m_hint.VerticalAlignment = WidgetAlignment.Far;
		m_hint.FontScale = 0.75f;
		m_root.Children.Add(new BevelledRectangleWidget
		{
			Size = m_root.Size,
			BevelSize = -2f,
			CenterColor = new Color(0, 0, 0, 0)
		});
		m_root.Children.Add(m_icon);
		m_root.Children.Add(m_count);
		m_root.Children.Add(m_hint);
	}

	public void SetItem(int value, int count, string hint = null)
	{
		m_value = value;
		m_countInt = MathUtils.Max(1, count);
		m_icon.Value = value;
		m_count.Text = ((m_countInt > 1) ? ("×" + m_countInt) : string.Empty);
		m_hint.Text = ((hint != null) ? hint : string.Empty);
	}

	public void Clear()
	{
		m_value = 0;
		m_countInt = 0;
		m_icon.Value = 0;
		m_count.Text = string.Empty;
		m_hint.Text = string.Empty;
	}
}
