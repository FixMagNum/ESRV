using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public class FermentationBarrelWidget : CanvasWidget
{
	private GridPanelWidget m_inventoryGrid;

	private GridPanelWidget m_ingredientsGrid;

	private InventorySlotWidget m_mugSlot;

	private InventorySlotWidget m_waterSlot;

	private ValueBarWidget m_waterBar;

	private ValueBarWidget m_progressBar;

	private LabelWidget m_statusLabel;

	private ButtonWidget m_safeStopButton;

	private ComponentFermentationBarrel m_barrel;

	private BevelledButtonWidget m_safeStopButtonBevelled;

	private ValueBarWidget[] m_waterBars;

	public static string fName;

	private string m_statusPrefix;

	public FermentationBarrelWidget(IInventory inventory, ComponentFermentationBarrel barrel)
	{
		m_barrel = barrel;
		XElement node = ContentManager.Get<XElement>("Widgets/FermentationBarrelWidget");
		LoadContents(this, node);
		m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		m_ingredientsGrid = Children.Find<GridPanelWidget>("IngredientsGrid");
		m_mugSlot = Children.Find<InventorySlotWidget>("MugSlot");
		m_waterSlot = Children.Find<InventorySlotWidget>("WaterSlot");
		m_mugSlot.AssignInventorySlot(barrel, barrel.MugSlotIndex);
		m_waterSlot.AssignInventorySlot(barrel, barrel.WaterIntakeSlotIndex);
		m_waterSlot.ProcessingOnly = false;
		m_progressBar = Children.Find<ValueBarWidget>("ProgressBar");
		m_statusLabel = Children.Find<LabelWidget>("StatusLabel");
		m_safeStopButton = Children.Find<ButtonWidget>("SafeStopButton", throwIfNotFound: false);
		if (m_safeStopButton == null)
		{
			m_safeStopButtonBevelled = Children.Find<BevelledButtonWidget>("SafeStopButton", throwIfNotFound: false);
		}
		m_statusPrefix = ((m_statusLabel != null) ? m_statusLabel.Text : string.Empty);
		m_waterBars = new ValueBarWidget[3];
		for (int i = 0; i < 3; i++)
		{
			m_waterBars[i] = Children.Find<ValueBarWidget>($"WaterBar{i}");
			m_waterBars[i].LayoutDirection = LayoutDirection.Horizontal;
			m_waterBars[i].FlipDirection = false;
			m_waterBars[i].BarSubtexture = GetBlockFaceSubtexture(18, 0, flowing: true);
			m_waterBars[i].TextureLinearFilter = false;
		}
		int num = 10;
		for (int j = 0; j < m_inventoryGrid.RowsCount; j++)
		{
			for (int k = 0; k < m_inventoryGrid.ColumnsCount; k++)
			{
				InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
				inventorySlotWidget.AssignInventorySlot(inventory, num++);
				m_inventoryGrid.Children.Add(inventorySlotWidget);
				m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(k, j));
			}
		}
		int num2 = 0;
		for (int l = 0; l < m_ingredientsGrid.RowsCount; l++)
		{
			for (int m = 0; m < m_ingredientsGrid.ColumnsCount; m++)
			{
				InventorySlotWidget inventorySlotWidget2 = new InventorySlotWidget();
				inventorySlotWidget2.AssignInventorySlot(barrel, num2++);
				m_ingredientsGrid.Children.Add(inventorySlotWidget2);
				m_ingredientsGrid.SetWidgetCell(inventorySlotWidget2, new Point2(m, l));
			}
		}
		m_progressBar.LayoutDirection = LayoutDirection.Vertical;
		m_progressBar.BarsCount = 8;
		m_progressBar.FlipDirection = false;
	}

	public override void Update()
	{
		if (!m_barrel.IsAddedToProject)
		{
			ParentWidget.Children.Remove(this);
			return;
		}
		SubsystemTerrain subsystemTerrain = m_barrel.Entity.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		Point3 coordinates = m_barrel.Entity.FindComponent<ComponentBlockEntity>(throwOnError: true).Coordinates;
		Color color = BlockColorsMap.WaterColorsMap.Lookup(subsystemTerrain.Terrain, coordinates.X, coordinates.Y, coordinates.Z);
		Color litBarColor = Color.Lerp(color, m_barrel.ResultDrink switch
		{
			ComponentFermentationBarrel.DrinkType.Beer => new Color(186, 138, 52), 
			ComponentFermentationBarrel.DrinkType.Kvass => new Color(110, 70, 40), 
			ComponentFermentationBarrel.DrinkType.PumpkinTincture => new Color(200, 110, 50), 
			ComponentFermentationBarrel.DrinkType.BlackBrew => Color.Black, 
			_ => PredictTargetColor(color), 
		}, MathUtils.Saturate(m_barrel.Progress01));
		bool flowing = m_barrel.State == ComponentFermentationBarrel.FermentationState.Active;
		for (int i = 0; i < 3; i++)
		{
			int num = 2 - i;
			m_waterBars[num].BarSubtexture = GetBlockFaceSubtexture(18, 0, flowing);
			m_waterBars[num].LitBarColor = litBarColor;
			m_waterBars[num].Value = ((m_barrel.WaterLayers > i) ? 1f : 0f);
		}
		m_progressBar.Value = m_barrel.Progress01;
		string text = m_barrel.State switch
		{
			ComponentFermentationBarrel.FermentationState.Ready => LanguageControl.Get(fName, 6), 
			ComponentFermentationBarrel.FermentationState.Active => LanguageControl.Get(fName, 3), 
			_ => LanguageControl.Get(fName, 2), 
		};
		if (m_statusLabel != null)
		{
			m_statusLabel.Text = $"{m_statusPrefix} {text}   {LanguageControl.Get(fName, 1)}: {m_barrel.WaterLayers}/3";
		}
		bool flag = m_barrel.State == ComponentFermentationBarrel.FermentationState.Active;
		if (m_safeStopButton != null)
		{
			m_safeStopButton.IsVisible = flag;
			m_safeStopButton.IsEnabled = flag;
			if (flag && m_safeStopButton.IsClicked)
			{
				m_barrel.SafeStop();
				return;
			}
		}
		if (m_safeStopButtonBevelled != null)
		{
			m_safeStopButtonBevelled.IsVisible = flag;
			m_safeStopButtonBevelled.IsEnabled = flag;
			if (flag && m_safeStopButtonBevelled.IsClicked)
			{
				m_barrel.SafeStop();
			}
		}
	}

	private Color PredictTargetColor(Color baseWater)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = false;
		for (int i = 0; i < 4; i++)
		{
			int slotCount = m_barrel.GetSlotCount(i);
			if (slotCount <= 0)
			{
				continue;
			}
			int slotValue = m_barrel.GetSlotValue(i);
			switch (Terrain.ExtractContents(slotValue))
			{
			case 394:
				num += slotCount;
				break;
			case 362:
				num2 += slotCount;
				break;
			case 311:
				num3 += slotCount;
				break;
			case 203:
			{
				int data = Terrain.ExtractData(slotValue);
				int clothingIndex = ClothingBlock.GetClothingIndex(data);
				int clothingColor = ClothingBlock.GetClothingColor(data);
				if (clothingIndex == 16 && clothingColor == 15 && slotCount >= 1)
				{
					flag = true;
				}
				break;
			}
			}
		}
		if ((num >= 100 && num2 >= 10 && num3 >= 100) & flag)
		{
			return Color.Black;
		}
		bool flag2 = true;
		for (int j = 0; j < 3; j++)
		{
			if (m_barrel.GetSlotCount(j) < 100)
			{
				flag2 = false;
				break;
			}
			int slotValue2 = m_barrel.GetSlotValue(j);
			if (Terrain.ExtractContents(slotValue2) != 173 || Terrain.ExtractData(slotValue2) != 5)
			{
				flag2 = false;
				break;
			}
		}
		if (flag2)
		{
			int num4 = Terrain.ExtractContents(m_barrel.GetSlotValue(3));
			if (m_barrel.GetSlotCount(3) > 0 && (num4 == 343 || num4 == 342 || num4 == 338 || num4 == 339 || num4 == 340 || num4 == 341 || num4 == 242 || num4 == 247 || num4 == 313 || num4 == 315 || num4 == 316 || num4 == 176 || num4 == 337 || num4 == 335 || num4 == 334 || num4 == 314))
			{
				return new Color(186, 138, 52);
			}
		}
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = true;
		for (int k = 0; k < 3; k++)
		{
			if (m_barrel.GetSlotCount(k) <= 0)
			{
				flag6 = false;
				break;
			}
			switch (Terrain.ExtractContents(m_barrel.GetSlotValue(k)))
			{
			case 24:
				flag3 = true;
				continue;
			case 25:
				flag4 = true;
				continue;
			case 20:
				flag5 = true;
				continue;
			}
			flag6 = false;
			break;
		}
		if (flag6 & flag3 & flag4 & flag5)
		{
			int num5 = Terrain.ExtractContents(m_barrel.GetSlotValue(3));
			int slotCount2 = m_barrel.GetSlotCount(3);
			if (num5 == 131 && slotCount2 > 0)
			{
				return new Color(200, 110, 50);
			}
			if (num5 == 329 && slotCount2 >= 12)
			{
				return new Color(110, 70, 40);
			}
		}
		return new Color(110, 30, 50);
	}

	private Subtexture GetBlockFaceSubtexture(int contents, int data, bool flowing)
	{
		int face = ((!flowing) ? 4 : 0);
		m_barrel.Entity.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		Texture2D animatedBlocksTexture = m_barrel.Entity.Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true).AnimatedBlocksTexture;
		int value = Terrain.MakeBlockValue(contents, 0, data);
		Vector4 vector = BlocksManager.TextureSlotToTextureCoords(BlocksManager.Blocks[contents].GetFaceTextureSlot(face, value));
		return new Subtexture(animatedBlocksTexture, new Vector2(vector.X, vector.Y), new Vector2(vector.Z, vector.W));
	}

	static FermentationBarrelWidget()
	{
		fName = "FermentationBarrelWidget";
	}
}
