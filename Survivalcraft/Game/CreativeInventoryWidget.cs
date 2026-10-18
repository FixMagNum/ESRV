using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using GameEntitySystem;

namespace Game;

public class CreativeInventoryWidget : CanvasWidget
{
	public class Category
	{
		public string Name;

		public Color Color = Color.White;

		public ContainerWidget Panel;
	}

	public List<Category> m_categories = new List<Category>();

	public int m_activeCategoryIndex = -1;

	public ComponentCreativeInventory m_componentCreativeInventory;

	public ButtonWidget m_pageUpButton;

	public ButtonWidget m_pageDownButton;

	public LabelWidget m_pageLabel;

	public ButtonWidget m_categoryLeftButton;

	public ButtonWidget m_categoryRightButton;

	public static string fName = "CreativeInventoryWidget";

	public ButtonWidget m_categoryButton;

	public ContainerWidget m_panelContainer;

	public FurnitureInventoryPanel furnitureInventoryPanel;

	public CreativeInventoryPanel creativeInventoryPanel;

	public Entity Entity => m_componentCreativeInventory.Entity;

	public ButtonWidget PageDownButton => m_pageDownButton;

	public ButtonWidget PageUpButton => m_pageUpButton;

	public LabelWidget PageLabel => m_pageLabel;

	public CreativeInventoryWidget(Entity entity)
	{
		m_componentCreativeInventory = entity.FindComponent<ComponentCreativeInventory>(throwOnError: true);
		XElement vector = ContentManager.Get<XElement>("Widgets/CreativeInventoryWidget");
		LoadContents(this, vector);
		m_categoryLeftButton = Children.Find<ButtonWidget>("CategoryLeftButton");
		m_categoryRightButton = Children.Find<ButtonWidget>("CategoryRightButton");
		m_categoryButton = Children.Find<ButtonWidget>("CategoryButton");
		m_pageUpButton = Children.Find<ButtonWidget>("PageUpButton");
		m_pageDownButton = Children.Find<ButtonWidget>("PageDownButton");
		m_pageLabel = Children.Find<LabelWidget>("PageLabel");
		m_panelContainer = Children.Find<ContainerWidget>("PanelContainer");
		creativeInventoryPanel = new CreativeInventoryPanel(this)
		{
			IsVisible = false
		};
		furnitureInventoryPanel = new FurnitureInventoryPanel(this)
		{
			IsVisible = false
		};
		m_panelContainer.Children.Add(creativeInventoryPanel);
		m_panelContainer.Children.Add(furnitureInventoryPanel);
		foreach (string category in BlocksManager.Categories)
		{
			m_categories.Add(new Category
			{
				Name = category,
				Panel = creativeInventoryPanel
			});
		}
		m_categories.Add(new Category
		{
			Name = LanguageControl.Get(fName, 1),
			Panel = furnitureInventoryPanel
		});
		m_categories.Add(new Category
		{
			Name = LanguageControl.Get(fName, 2),
			Panel = creativeInventoryPanel
		});
		for (int i = 0; i < m_categories.Count; i++)
		{
			if (m_categories[i].Name == "Electrics")
			{
				m_categories[i].Color = new Color(128, 140, 255);
			}
			if (m_categories[i].Name == "Plants")
			{
				m_categories[i].Color = new Color(64, 160, 64);
			}
			if (m_categories[i].Name == "Weapons")
			{
				m_categories[i].Color = new Color(255, 128, 112);
			}
		}
	}

	public string GetCategoryName(int index)
	{
		return m_categories[index].Name;
	}

	public override void Update()
	{
		if (m_categoryLeftButton.IsClicked || Input.Left)
		{
			ComponentCreativeInventory componentCreativeInventory = m_componentCreativeInventory;
			int categoryIndex = componentCreativeInventory.CategoryIndex - 1;
			componentCreativeInventory.CategoryIndex = categoryIndex;
		}
		if (m_categoryRightButton.IsClicked || Input.Right)
		{
			ComponentCreativeInventory componentCreativeInventory2 = m_componentCreativeInventory;
			int categoryIndex2 = componentCreativeInventory2.CategoryIndex + 1;
			componentCreativeInventory2.CategoryIndex = categoryIndex2;
		}
		if (m_categoryButton.IsClicked)
		{
			ComponentPlayer componentPlayer = Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null)
			{
				DialogsManager.ShowDialog(componentPlayer.GuiWidget, new ListSelectionDialog(string.Empty, m_categories, 56f, (object c) => new LabelWidget
				{
					Text = LanguageControl.Get("BlocksManager", ((Category)c).Name),
					Color = ((Category)c).Color,
					HorizontalAlignment = WidgetAlignment.Center,
					VerticalAlignment = WidgetAlignment.Center
				}, (object c) =>
				{
					if (c != null)
					{
						m_componentCreativeInventory.CategoryIndex = m_categories.IndexOf((Category)c);
					}
				}));
			}
		}
		m_componentCreativeInventory.CategoryIndex = MathUtils.Clamp(m_componentCreativeInventory.CategoryIndex, 0, m_categories.Count - 1);
		m_categoryButton.Text = LanguageControl.Get("BlocksManager", m_categories[m_componentCreativeInventory.CategoryIndex].Name);
		m_categoryLeftButton.IsEnabled = m_componentCreativeInventory.CategoryIndex > 0;
		m_categoryRightButton.IsEnabled = m_componentCreativeInventory.CategoryIndex < m_categories.Count - 1;
		if (m_componentCreativeInventory.CategoryIndex == m_activeCategoryIndex)
		{
			return;
		}
		foreach (Category category in m_categories)
		{
			category.Panel.IsVisible = false;
		}
		m_categories[m_componentCreativeInventory.CategoryIndex].Panel.IsVisible = true;
		m_activeCategoryIndex = m_componentCreativeInventory.CategoryIndex;
	}
}
