using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public class MugFermentationRecipeWidget : CanvasWidget
{
	private LabelWidget m_name;

	private LabelWidget m_desc;

	private GridPanelWidget m_ingGrid;

	private CraftingRecipeSlotWidget m_result;

	private ValueBarWidget[] m_waterBars;

	public MugFermentationRecipeWidget()
	{
		XElement node = ContentManager.Get<XElement>("Widgets/MugFermentationRecipeWidget");
		LoadContents(this, node);
		m_name = Children.Find<LabelWidget>("MugFR.Name");
		m_desc = Children.Find<LabelWidget>("MugFR.Desc");
		m_ingGrid = Children.Find<GridPanelWidget>("MugFR.Ingredients");
		m_result = Children.Find<CraftingRecipeSlotWidget>("MugFR.Result");
		m_waterBars = new ValueBarWidget[3];
		m_waterBars[0] = Children.Find<ValueBarWidget>("MugFR.WaterBar0");
		m_waterBars[1] = Children.Find<ValueBarWidget>("MugFR.WaterBar1");
		m_waterBars[2] = Children.Find<ValueBarWidget>("MugFR.WaterBar2");
		for (int i = 0; i < m_ingGrid.RowsCount; i++)
		{
			for (int j = 0; j < m_ingGrid.ColumnsCount; j++)
			{
				RecipeSlotPreviewWidget widget = new RecipeSlotPreviewWidget();
				m_ingGrid.Children.Add(widget);
				m_ingGrid.SetWidgetCell(widget, new Point2(j, i));
			}
		}
		InitWaterBarsTexture();
	}

	public void SetDrink(int mugValue, MugBlock.DrinkType drink)
	{
		Block block = BlocksManager.Blocks[Terrain.ExtractContents(mugValue)];
		m_name.Text = block.GetDisplayName(null, mugValue);
		m_result.SetResult(mugValue, 1);
		for (int i = 0; i < 3; i++)
		{
			m_waterBars[i].Value = 1f;
		}
		FillCanonicalSlots(drink);
	}

	private void FillCanonicalSlots(MugBlock.DrinkType drink)
	{
		RecipeSlotPreviewWidget recipeSlotPreviewWidget = (RecipeSlotPreviewWidget)m_ingGrid.Children[0];
		RecipeSlotPreviewWidget recipeSlotPreviewWidget2 = (RecipeSlotPreviewWidget)m_ingGrid.Children[1];
		RecipeSlotPreviewWidget recipeSlotPreviewWidget3 = (RecipeSlotPreviewWidget)m_ingGrid.Children[2];
		RecipeSlotPreviewWidget recipeSlotPreviewWidget4 = (RecipeSlotPreviewWidget)m_ingGrid.Children[3];
		switch (drink)
		{
		case MugBlock.DrinkType.Beer:
		{
			int value2 = Terrain.MakeBlockValue(173, 0, 5);
			recipeSlotPreviewWidget.SetItem(value2, 100);
			recipeSlotPreviewWidget2.SetItem(value2, 100);
			recipeSlotPreviewWidget3.SetItem(value2, 100);
			int value3 = Terrain.MakeBlockValue(313, 0, 0);
			recipeSlotPreviewWidget4.SetItem(value3, 1, "Bread yeast");
			break;
		}
		case MugBlock.DrinkType.Kvass:
			recipeSlotPreviewWidget.SetItem(Terrain.MakeBlockValue(24, 0, 0), 1);
			recipeSlotPreviewWidget2.SetItem(Terrain.MakeBlockValue(25, 0, 0), 1);
			recipeSlotPreviewWidget3.SetItem(Terrain.MakeBlockValue(20, 0, 0), 1);
			recipeSlotPreviewWidget4.SetItem(Terrain.MakeBlockValue(329, 0, 0), 12);
			break;
		case MugBlock.DrinkType.PumpkinTincture:
			recipeSlotPreviewWidget.SetItem(Terrain.MakeBlockValue(24, 0, 0), 1);
			recipeSlotPreviewWidget2.SetItem(Terrain.MakeBlockValue(25, 0, 0), 1);
			recipeSlotPreviewWidget3.SetItem(Terrain.MakeBlockValue(20, 0, 0), 1);
			recipeSlotPreviewWidget4.SetItem(Terrain.MakeBlockValue(131, 0, 0), 10);
			break;
		case MugBlock.DrinkType.BlackBrew:
		{
			recipeSlotPreviewWidget.SetItem(Terrain.MakeBlockValue(394, 0, 0), 100);
			recipeSlotPreviewWidget2.SetItem(Terrain.MakeBlockValue(362, 0, 0), 10);
			recipeSlotPreviewWidget3.SetItem(Terrain.MakeBlockValue(311, 0, 0), 100);
			int data = ClothingBlock.SetClothingColor(ClothingBlock.SetClothingIndex(0, 16), 15);
			int value = Terrain.MakeBlockValue(203, 0, data);
			recipeSlotPreviewWidget4.SetItem(value, 1);
			break;
		}
		default:
			recipeSlotPreviewWidget.Clear();
			recipeSlotPreviewWidget2.Clear();
			recipeSlotPreviewWidget3.Clear();
			recipeSlotPreviewWidget4.Clear();
			break;
		}
	}

	private void InitWaterBarsTexture()
	{
		Texture2D texture = ContentManager.Get<Texture2D>("Textures/Blocks");
		int value = Terrain.MakeBlockValue(18, 0, 0);
		Vector4 vector = BlocksManager.TextureSlotToTextureCoords(BlocksManager.Blocks[18].GetFaceTextureSlot(4, value));
		Subtexture barSubtexture = new Subtexture(texture, new Vector2(vector.X, vector.Y), new Vector2(vector.Z, vector.W));
		for (int i = 0; i < 3; i++)
		{
			m_waterBars[i].BarSubtexture = barSubtexture;
			m_waterBars[i].TextureLinearFilter = false;
		}
	}
}
