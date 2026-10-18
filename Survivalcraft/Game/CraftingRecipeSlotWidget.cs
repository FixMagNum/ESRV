using System.Collections.Generic;
using System.Xml.Linq;
using Engine;

namespace Game;

public class CraftingRecipeSlotWidget : CanvasWidget
{
	public BlockIconWidget m_blockIconWidget;

	public LabelWidget m_labelWidget;

	public string m_ingredient;

	public int m_resultValue;

	public int m_resultCount;

	public List<int> m_cachedValues = new List<int>();

	public CraftingRecipeSlotWidget()
	{
		XElement node = ContentManager.Get<XElement>("Widgets/CraftingRecipeSlot");
		LoadContents(this, node);
		m_blockIconWidget = Children.Find<BlockIconWidget>("CraftingRecipeSlotWidget.Icon");
		m_labelWidget = Children.Find<LabelWidget>("CraftingRecipeSlotWidget.Count");
	}

	public void SetIngredient(string ingredient)
	{
		m_ingredient = ingredient;
		m_resultValue = 0;
		m_resultCount = 0;
		m_cachedValues.Clear();
		if (string.IsNullOrEmpty(ingredient))
		{
			return;
		}
		string[] array = ingredient.Split('|');
		for (int i = 0; i < array.Length; i++)
		{
			CraftingRecipesManager.DecodeIngredient(array[i], out var craftingId, out var data);
			Block[] array2 = BlocksManager.FindBlocksByCraftingId(craftingId);
			Block[] array3 = array2;
			foreach (Block block in array3)
			{
				if (data.HasValue)
				{
					m_cachedValues.Add(Terrain.MakeBlockValue(block.BlockIndex, 0, data.Value));
					continue;
				}
				foreach (int creativeValue in block.GetCreativeValues())
				{
					m_cachedValues.Add(creativeValue);
				}
			}
			if (m_cachedValues.Count == 0 && array2.Length != 0)
			{
				array3 = array2;
				foreach (Block block2 in array3)
				{
					m_cachedValues.Add(Terrain.MakeBlockValue(block2.BlockIndex, 0, 4));
				}
			}
		}
	}

	public void SetResult(int value, int count)
	{
		m_resultValue = value;
		m_resultCount = count;
		m_ingredient = null;
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		m_blockIconWidget.IsVisible = false;
		m_labelWidget.IsVisible = false;
		if (!string.IsNullOrEmpty(m_ingredient))
		{
			if (m_cachedValues.Count > 0)
			{
				int index = (int)(1.0 * Time.RealTime) % m_cachedValues.Count;
				m_blockIconWidget.Value = m_cachedValues[index];
				m_blockIconWidget.Light = 15;
				m_blockIconWidget.IsVisible = true;
			}
		}
		else if (m_resultValue != 0)
		{
			m_blockIconWidget.Value = m_resultValue;
			m_blockIconWidget.Light = 15;
			m_labelWidget.Text = m_resultCount.ToString();
			m_blockIconWidget.IsVisible = true;
			m_labelWidget.IsVisible = true;
		}
		base.MeasureOverride(parentAvailableSize);
	}
}
