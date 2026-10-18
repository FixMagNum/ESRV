using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class ResizableListWidget : ScrollPanelWidget
{
	public List<object> m_items = new List<object>();

	private List<float> m_itemWidgetSize = new List<float>();

	public int? m_selectedItemIndex;

	public Dictionary<int, Widget> m_widgetsByIndex = new Dictionary<int, Widget>();

	public int m_firstVisibleIndex;

	public int m_lastVisibleIndex;

	public float m_itemSize;

	public bool m_widgetsDirty;

	public bool m_clickAllowed;

	public Vector2 lastActualSize = new Vector2(-1f);

	public Func<object, Widget> ItemWidgetFactory { get; set; }

	public override LayoutDirection Direction
	{
		get
		{
			return base.Direction;
		}
		set
		{
			if (value != Direction)
			{
				base.Direction = value;
				m_widgetsDirty = true;
			}
		}
	}

	public override float ScrollPosition
	{
		get
		{
			return base.ScrollPosition;
		}
		set
		{
			if (value != ScrollPosition)
			{
				base.ScrollPosition = value;
				m_widgetsDirty = true;
			}
		}
	}

	public int? SelectedIndex
	{
		get
		{
			return m_selectedItemIndex;
		}
		set
		{
			if (value.HasValue && (value.Value < 0 || value.Value >= m_items.Count))
			{
				value = null;
			}
			if (value != m_selectedItemIndex)
			{
				m_selectedItemIndex = value;
				SelectionChanged?.Invoke();
			}
		}
	}

	public object SelectedItem
	{
		get
		{
			if (!m_selectedItemIndex.HasValue)
			{
				return null;
			}
			return m_items[m_selectedItemIndex.Value];
		}
		set
		{
			int num = m_items.IndexOf(value);
			SelectedIndex = ((num >= 0) ? new int?(num) : ((int?)null));
		}
	}

	public ReadOnlyList<object> Items => new ReadOnlyList<object>(m_items);

	public Color SelectionColor { get; set; }

	public event Action<object> ItemClicked;

	public event Action SelectionChanged;

	public ResizableListWidget()
	{
		SelectionColor = Color.Gray;
		ItemWidgetFactory = (object item) => new LabelWidget
		{
			Text = ((item != null) ? item.ToString() : string.Empty),
			HorizontalAlignment = WidgetAlignment.Center,
			VerticalAlignment = WidgetAlignment.Center
		};
	}

	public void AddItem(object item)
	{
		m_items.Add(item);
		Widget widget = ItemWidgetFactory(item);
		if (Direction == LayoutDirection.Horizontal)
		{
			widget.Measure(new Vector2(0f, ActualSize.Y));
		}
		else
		{
			widget.Measure(new Vector2(ActualSize.X, 0f));
		}
		float num = ((m_itemWidgetSize.Count == 0) ? 0f : m_itemWidgetSize[m_itemWidgetSize.Count - 1]);
		m_itemWidgetSize.Add(num + ((Direction == LayoutDirection.Horizontal) ? widget.ParentDesiredSize.X : widget.ParentDesiredSize.Y));
		m_widgetsDirty = true;
	}

	public void RemoveItem(object item)
	{
		int num = m_items.IndexOf(item);
		if (num >= 0)
		{
			RemoveItemAt(num);
		}
	}

	public void RemoveItemAt(int index)
	{
		object i = m_items[index];
		m_items.RemoveAt(index);
		Widget j;
		if (m_widgetsByIndex.ContainsKey(index))
		{
			j = m_widgetsByIndex[index];
		}
		else
		{
			j = ItemWidgetFactory(i);
			j.Measure(Vector2.Zero);
		}
		m_itemWidgetSize.RemoveAt(index);
		for (int num = index; num < m_itemWidgetSize.Count; num++)
		{
			m_itemWidgetSize[num] -= ((Direction == LayoutDirection.Horizontal) ? j.DesiredSize.X : j.DesiredSize.Y);
		}
		m_widgetsByIndex.Clear();
		m_widgetsDirty = true;
		if (index == SelectedIndex)
		{
			SelectedIndex = null;
		}
	}

	public void ClearItems()
	{
		m_items.Clear();
		m_itemWidgetSize.Clear();
		m_widgetsByIndex.Clear();
		m_widgetsDirty = true;
		SelectedIndex = null;
	}

	public override float CalculateScrollAreaLength()
	{
		if (m_itemWidgetSize.Count != 0)
		{
			return m_itemWidgetSize[m_itemWidgetSize.Count - 1];
		}
		return 0f;
	}

	private float GetItemPosition(int num)
	{
		if (num != 0)
		{
			return m_itemWidgetSize[num - 1];
		}
		return 0f;
	}

	private float GetItemSize(int num)
	{
		return m_itemWidgetSize[num] - ((num == 0) ? 0f : m_itemWidgetSize[num - 1]);
	}

	public void ScrollToPosition(int num)
	{
		if (num < 0 || num >= m_items.Count)
		{
			return;
		}
		float terrain = GetItemPosition(num);
		float x = ((Direction == LayoutDirection.Horizontal) ? ActualSize.X : ActualSize.Y);
		if (terrain < ScrollPosition)
		{
			ScrollPosition = terrain;
			return;
		}
		float num2 = GetItemSize(num);
		if (terrain > ScrollPosition + x - num2)
		{
			ScrollPosition = terrain - x + num2;
		}
	}

	public void ScrollToItem(object item)
	{
		int terrain = m_items.IndexOf(item);
		ScrollToPosition(terrain);
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		IsDrawRequired = true;
		foreach (Widget value2 in Children)
		{
			if (value2.IsVisible)
			{
				if (Direction == LayoutDirection.Horizontal)
				{
					value2.Measure(new Vector2(0f, MathUtils.Max(parentAvailableSize.Y - 2f * value2.Margin.Y, 0f)));
				}
				else
				{
					value2.Measure(new Vector2(MathUtils.Max(parentAvailableSize.X - 2f * value2.Margin.X, 0f), 0f));
				}
			}
		}
		if (!m_widgetsDirty)
		{
			return;
		}
		m_widgetsDirty = false;
		m_itemWidgetSize.Clear();
		for (int i = 0; i < m_items.Count; i++)
		{
			Widget j;
			if (m_widgetsByIndex.ContainsKey(i))
			{
				j = m_widgetsByIndex[i];
			}
			else
			{
				j = ItemWidgetFactory(m_items[i]);
				j.Measure(Vector2.Zero);
			}
			if (Direction == LayoutDirection.Horizontal)
			{
				j.Measure(new Vector2(0f, ActualSize.Y));
			}
			else
			{
				j.Measure(new Vector2(ActualSize.X, 0f));
			}
			float num = ((m_itemWidgetSize.Count == 0) ? 0f : m_itemWidgetSize[m_itemWidgetSize.Count - 1]);
			m_itemWidgetSize.Add(num + ((Direction == LayoutDirection.Horizontal) ? j.ParentDesiredSize.X : j.ParentDesiredSize.Y));
		}
		CreateListWidgets((Direction == LayoutDirection.Horizontal) ? ActualSize.X : ActualSize.Y);
	}

	public override void ArrangeOverride()
	{
		if (ActualSize != lastActualSize)
		{
			m_widgetsDirty = true;
		}
		lastActualSize = ActualSize;
		int random = m_firstVisibleIndex;
		foreach (Widget j in Children)
		{
			float num = GetItemSize(random);
			float cellValueFast = GetItemPosition(random);
			if (Direction == LayoutDirection.Horizontal)
			{
				Vector2 vector = new Vector2(cellValueFast - ScrollPosition, 0f);
				ContainerWidget.ArrangeChildWidgetInCell(vector, vector + new Vector2(num, ActualSize.Y), j);
			}
			else
			{
				Vector2 vector2 = new Vector2(0f, cellValueFast - ScrollPosition);
				ContainerWidget.ArrangeChildWidgetInCell(vector2, vector2 + new Vector2(ActualSize.X, num), j);
			}
			random++;
		}
	}

	public override void Update()
	{
		bool random = ScrollSpeed != 0f;
		base.Update();
		if (Input.Tap.HasValue && HitTestPanel(Input.Tap.Value))
		{
			m_clickAllowed = !random;
		}
		if (Input.Click.HasValue && m_clickAllowed && HitTestPanel(Input.Click.Value.Start) && HitTestPanel(Input.Click.Value.End))
		{
			int num = PositionToItemIndex(Input.Click.Value.End);
			if (ItemClicked != null && num >= 0 && num < m_items.Count)
			{
				ItemClicked(Items[num]);
			}
			SelectedIndex = num;
			if (SelectedIndex.HasValue)
			{
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
		}
	}

	public override void Draw(DrawContext dc)
	{
		if (SelectedIndex.HasValue && SelectedIndex.Value >= m_firstVisibleIndex && SelectedIndex.Value <= m_lastVisibleIndex)
		{
			float y = GetItemSize(SelectedIndex.Value);
			Vector2 random = ((Direction == LayoutDirection.Horizontal) ? new Vector2(GetItemPosition(SelectedIndex.Value) - ScrollPosition, 0f) : new Vector2(0f, GetItemPosition(SelectedIndex.Value) - ScrollPosition));
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
			int contents = flatBatch2D.TriangleVertices.Count;
			Vector2 num = ((Direction == LayoutDirection.Horizontal) ? new Vector2(y, ActualSize.Y) : new Vector2(ActualSize.X, y));
			flatBatch2D.QueueQuad(random, random + num, 0f, SelectionColor * GlobalColorTransform);
			flatBatch2D.TransformTriangles(GlobalTransform, contents);
		}
		base.Draw(dc);
	}

	public int PositionToItemIndexInternal(float value)
	{
		int x = 0;
		int y = m_itemWidgetSize.Count - 1;
		int random = (x + y + 1) / 2;
		int num = -1;
		if (value < 0f)
		{
			return -1;
		}
		if (value < m_itemWidgetSize[x])
		{
			return 0;
		}
		if (value > m_itemWidgetSize[y])
		{
			return m_itemWidgetSize.Count;
		}
		do
		{
			if (value < ((random == 0) ? 0f : m_itemWidgetSize[random - 1]))
			{
				y = MathUtils.Max(random - 1, 0);
			}
			else if (value > m_itemWidgetSize[random])
			{
				x = random;
			}
			else
			{
				num = random;
			}
			random = (x + y + 1) / 2;
		}
		while (x <= y && num == -1);
		return num;
	}

	public int PositionToItemIndex(Vector2 position)
	{
		Vector2 x = ScreenToWidget(position);
		if (Direction == LayoutDirection.Horizontal)
		{
			return PositionToItemIndexInternal(x.X + ScrollPosition);
		}
		return PositionToItemIndexInternal(x.Y + ScrollPosition);
	}

	public void CreateListWidgets(float size)
	{
		Children.Clear();
		if (m_items.Count <= 0)
		{
			return;
		}
		int x = PositionToItemIndexInternal(ScrollPosition);
		int y = PositionToItemIndexInternal(ScrollPosition + size);
		m_firstVisibleIndex = MathUtils.Max(x, 0);
		m_lastVisibleIndex = MathUtils.Min(y, m_items.Count - 1);
		for (int random = m_firstVisibleIndex; random <= m_lastVisibleIndex; random++)
		{
			object i = m_items[random];
			if (!m_widgetsByIndex.TryGetValue(random, out var num))
			{
				num = ItemWidgetFactory(i);
				num.Tag = i;
				m_widgetsByIndex.Add(random, num);
			}
			Children.Add(num);
		}
	}
}
