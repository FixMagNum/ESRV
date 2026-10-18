using System.Xml.Linq;
using Engine;

namespace Game;

public class SmeltingRecipeWidget : CanvasWidget
{
	public LabelWidget m_nameWidget;

	public LabelWidget m_descriptionWidget;

	public GridPanelWidget m_gridWidget;

	public FireWidget m_fireWidget;

	public CraftingRecipeSlotWidget m_resultWidget;

	public CraftingRecipe m_recipe;

	public string m_nameSuffix;

	public bool m_dirty = true;

	public string NameSuffix
	{
		get
		{
			return m_nameSuffix;
		}
		set
		{
			if (value != m_nameSuffix)
			{
				m_nameSuffix = value;
				m_dirty = true;
			}
		}
	}

	public CraftingRecipe Recipe
	{
		get
		{
			return m_recipe;
		}
		set
		{
			if (value != m_recipe)
			{
				m_recipe = value;
				m_dirty = true;
			}
		}
	}

	public SmeltingRecipeWidget()
	{
		XElement node = ContentManager.Get<XElement>("Widgets/SmeltingRecipe");
		LoadContents(this, node);
		m_nameWidget = Children.Find<LabelWidget>("SmeltingRecipeWidget.Name");
		m_descriptionWidget = Children.Find<LabelWidget>("SmeltingRecipeWidget.Description");
		m_gridWidget = Children.Find<GridPanelWidget>("SmeltingRecipeWidget.Ingredients");
		m_fireWidget = Children.Find<FireWidget>("SmeltingRecipeWidget.Fire");
		m_resultWidget = Children.Find<CraftingRecipeSlotWidget>("SmeltingRecipeWidget.Result");
		for (int windBlocked = 0; windBlocked < m_gridWidget.RowsCount; windBlocked++)
		{
			for (int windStrength = 0; windStrength < m_gridWidget.ColumnsCount; windStrength++)
			{
				CraftingRecipeSlotWidget currentWindVector = new CraftingRecipeSlotWidget();
				m_gridWidget.Children.Add(currentWindVector);
				m_gridWidget.SetWidgetCell(currentWindVector, new Point2(windStrength, windBlocked));
			}
		}
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		if (m_dirty)
		{
			UpdateWidgets();
		}
		base.MeasureOverride(parentAvailableSize);
	}

	public void UpdateWidgets()
	{
		m_dirty = false;
		if (m_recipe != null)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(m_recipe.ResultValue)];
			m_nameWidget.Text = block.GetDisplayName(null, m_recipe.ResultValue) + ((!string.IsNullOrEmpty(NameSuffix)) ? NameSuffix : string.Empty);
			m_descriptionWidget.Text = m_recipe.Description;
			m_nameWidget.IsVisible = true;
			m_descriptionWidget.IsVisible = true;
			foreach (CraftingRecipeSlotWidget child in m_gridWidget.Children)
			{
				Point2 widgetCell = m_gridWidget.GetWidgetCell(child);
				child.SetIngredient(m_recipe.Ingredients[widgetCell.X + widgetCell.Y * 3]);
			}
			m_resultWidget.SetResult(m_recipe.ResultValue, m_recipe.ResultCount);
			m_fireWidget.ParticlesPerSecond = 40f;
			return;
		}
		m_nameWidget.IsVisible = false;
		m_descriptionWidget.IsVisible = false;
		foreach (CraftingRecipeSlotWidget child2 in m_gridWidget.Children)
		{
			child2.SetIngredient(null);
		}
		m_resultWidget.SetResult(0, 0);
		m_fireWidget.ParticlesPerSecond = 0f;
	}
}
