using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;

namespace Game;

public class RecipaediaScreen : Screen
{
	internal class Order
	{
		public Block block;

		public int order;

		public int value;

		public Order(Block b, int o, int v)
		{
			block = b;
			order = o;
			value = v;
		}
	}

	public ListPanelWidget m_blocksList;

	public LabelWidget m_categoryLabel;

	public ButtonWidget m_prevCategoryButton;

	public ButtonWidget m_nextCategoryButton;

	public ButtonWidget m_detailsButton;

	public ButtonWidget m_recipesButton;

	public Screen m_previousScreen;

	public List<string> m_categories;

	public int m_categoryIndex;

	public int m_listCategoryIndex;

	public TextBoxWidget m_searchBox;

	public ButtonWidget m_searchButton;

	public LabelWidget m_searchPlaceholder;

	private string previousSearchText = string.Empty;

	public RecipaediaScreen()
	{
		m_categories = new List<string>();
		m_listCategoryIndex = -1;
		XElement node = ContentManager.Get<XElement>("Screens/RecipaediaScreen");
		LoadContents(this, node);
		m_blocksList = Children.Find<ListPanelWidget>("BlocksList");
		m_categoryLabel = Children.Find<LabelWidget>("Category");
		m_prevCategoryButton = Children.Find<ButtonWidget>("PreviousCategory");
		m_nextCategoryButton = Children.Find<ButtonWidget>("NextCategory");
		m_detailsButton = Children.Find<ButtonWidget>("DetailsButton");
		m_recipesButton = Children.Find<ButtonWidget>("RecipesButton");
		m_searchBox = Children.Find<TextBoxWidget>("SearchBox");
		m_searchPlaceholder = Children.Find<LabelWidget>("SearchPlaceholder");
		m_categories.Add(null);
		m_categories.AddRange(BlocksManager.Categories);
		m_blocksList.ItemWidgetFactory = (object item) =>
		{
			int value = (int)item;
			int num = Terrain.ExtractContents(value);
			Block block = BlocksManager.Blocks[num];
			XElement node2 = ContentManager.Get<XElement>("Widgets/RecipaediaItem");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
			containerWidget.Children.Find<BlockIconWidget>("RecipaediaItem.Icon").Value = value;
			containerWidget.Children.Find<LabelWidget>("RecipaediaItem.Text").Text = block.GetDisplayName(null, value);
			containerWidget.Children.Find<LabelWidget>("RecipaediaItem.Details").Text = block.GetDescription(value);
			return containerWidget;
		};
		ListPanelWidget blocksList = m_blocksList;
		blocksList.ItemClicked = (Action<object>)Delegate.Combine(blocksList.ItemClicked, (Action<object>)((object item) =>
		{
			if (m_blocksList.SelectedItem == item && item is int)
			{
				ScreensManager.SwitchScreen("RecipaediaDescription", item, m_blocksList.Items.Cast<int>().ToList());
			}
		}));
	}

	public override void Enter(object[] parameters)
	{
		if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaRecipes") && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaDescription"))
		{
			m_previousScreen = ScreensManager.PreviousScreen;
		}
	}

	public override void Update()
	{
		if (m_listCategoryIndex != m_categoryIndex)
		{
			PopulateBlocksList();
		}
		m_searchPlaceholder.IsVisible = string.IsNullOrEmpty(m_searchBox.Text);
		if (m_searchBox.Text != previousSearchText)
		{
			previousSearchText = m_searchBox.Text;
			FilterBlocksList();
		}
		string value = ((m_categories[m_categoryIndex] == null) ? LanguageControl.Get("BlocksManager", "All Blocks") : LanguageControl.Get("BlocksManager", m_categories[m_categoryIndex]));
		m_categoryLabel.Text = $"{value} ({m_blocksList.Items.Count})";
		m_prevCategoryButton.IsEnabled = m_categoryIndex > 0;
		m_nextCategoryButton.IsEnabled = m_categoryIndex < m_categories.Count - 1;
		int? selectedValue = null;
		int num = 0;
		if (m_blocksList.SelectedItem is int)
		{
			selectedValue = (int)m_blocksList.SelectedItem;
			num = CraftingRecipesManager.Recipes.Count((CraftingRecipe r) => r.ResultValue == selectedValue);
		}
		if (selectedValue.HasValue)
		{
			int value2 = selectedValue.Value;
			if (Terrain.ExtractContents(value2) == 429 && MugBlock.GetDrink(value2) != MugBlock.DrinkType.None && num == 0)
			{
				num = 1;
			}
		}
		if (num > 0)
		{
			m_recipesButton.Text = $"{num} {((num == 1) ? LanguageControl.Get(GetType().Name, 1) : LanguageControl.Get(GetType().Name, 2))}";
			m_recipesButton.IsEnabled = true;
		}
		else
		{
			m_recipesButton.Text = LanguageControl.Get(GetType().Name, 3);
			m_recipesButton.IsEnabled = false;
		}
		m_detailsButton.IsEnabled = selectedValue.HasValue;
		if (m_prevCategoryButton.IsClicked || Input.Left)
		{
			m_categoryIndex = MathUtils.Max(m_categoryIndex - 1, 0);
		}
		if (m_nextCategoryButton.IsClicked || Input.Right)
		{
			m_categoryIndex = MathUtils.Min(m_categoryIndex + 1, m_categories.Count - 1);
		}
		if (selectedValue.HasValue && m_detailsButton.IsClicked)
		{
			ScreensManager.SwitchScreen("RecipaediaDescription", selectedValue.Value, m_blocksList.Items.Cast<int>().ToList());
		}
		if (selectedValue.HasValue && m_recipesButton.IsClicked)
		{
			ScreensManager.SwitchScreen("RecipaediaRecipes", selectedValue.Value);
		}
		if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
		{
			UnfocusSearchBox();
			ScreensManager.SwitchScreen(m_previousScreen);
		}
	}

	public void PopulateBlocksList()
	{
		m_listCategoryIndex = m_categoryIndex;
		string text = m_categories[m_categoryIndex];
		m_blocksList.ScrollPosition = 0f;
		m_blocksList.ClearItems();
		List<Order> list = new List<Order>();
		Block[] blocks = BlocksManager.Blocks;
		foreach (Block block in blocks)
		{
			foreach (int creativeValue in block.GetCreativeValues())
			{
				if (string.IsNullOrEmpty(text) || block.GetCategory(creativeValue) == text)
				{
					list.Add(new Order(block, block.GetDisplayOrder(creativeValue), creativeValue));
				}
			}
		}
		foreach (Order item in list.OrderBy((Order o) => o.order))
		{
			m_blocksList.AddItem(item.value);
		}
	}

	private void FilterBlocksList()
	{
		string text = m_searchBox.Text.ToLower();
		bool flag = int.TryParse(text, out var result);
		m_blocksList.ClearItems();
		Block[] blocks = BlocksManager.Blocks;
		foreach (Block block in blocks)
		{
			foreach (int creativeValue in block.GetCreativeValues())
			{
				string text2 = block.GetDisplayName(null, creativeValue).ToLower();
				if (flag && block.BlockIndex == result)
				{
					m_blocksList.AddItem(creativeValue);
				}
				else if (!flag && text2.Contains(text))
				{
					m_blocksList.AddItem(creativeValue);
				}
			}
		}
	}

	private void UnfocusSearchBox()
	{
		if (m_searchBox != null)
		{
			m_searchBox.HasFocus = false;
		}
	}
}
