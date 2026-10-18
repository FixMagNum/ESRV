using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public class StrongCottonBlock : PaintedCubeBlock
{
	public enum CottonMode
	{
		Dry,
		Wet
	}

	public const int Index = 365;

	private static readonly Random _random;

	public StrongCottonBlock()
		: base(224)
	{
	}

	public int GetRandomRotation()
	{
		return _random.Int(0, 4);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		int? color = PaintedCubeBlock.GetColor(data);
		Color color2 = (color.HasValue ? SubsystemPalette.GetColor(g, color) : Color.White);
		if (GetMode(data) == CottonMode.Wet && !color.HasValue)
		{
			color2 *= new Color(0.8f, 0.9f, 1f);
		}
		g.GenerateNaturalCubeVertices(this, value, x, y, z, color2, geom.OpaqueSubsetsByFace);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner m, int value, TerrainRaycastResult hit)
	{
		return new BlockPlacementData
		{
			Value = value,
			CellFace = hit.CellFace
		};
	}

	static StrongCottonBlock()
	{
		_random = new Random();
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		CottonMode mode = GetMode(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"StrongCottonBlock:{((mode == CottonMode.Wet) ? 1 : 0)}", "DisplayName");
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetMode(0, CottonMode.Dry));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetMode(0, CottonMode.Wet));
		HashSet<int> uniqueValues = new HashSet<int>();
		for (int i = 0; i < 16; i++)
		{
			int num = PaintedCubeBlock.SetColor(SetMode(0, CottonMode.Dry), i);
			int wetColoredValue = PaintedCubeBlock.SetColor(SetMode(0, CottonMode.Wet), i);
			if (uniqueValues.Add(num))
			{
				yield return Terrain.MakeBlockValue(BlockIndex, 0, num);
			}
			if (uniqueValues.Add(wetColoredValue))
			{
				yield return Terrain.MakeBlockValue(BlockIndex, 0, wetColoredValue);
			}
		}
	}

	public static CottonMode GetMode(int data)
	{
		return (CottonMode)((data >> 1) & 1);
	}

	public static int SetMode(int data, CottonMode mode)
	{
		return (data & -3) | ((int)mode << 1);
	}

	public override string GetCategory(int value)
	{
		if (!PaintedCubeBlock.GetColor(Terrain.ExtractData(value)).HasValue)
		{
			return "Items";
		}
		return base.GetCategory(value);
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		CraftingRecipe craftingRecipe = new CraftingRecipe
		{
			ResultCount = 1,
			ResultValue = Terrain.MakeBlockValue(BlockIndex, 0, SetMode(0, CottonMode.Dry)),
			RequiredHeatLevel = 1f,
			Description = LanguageControl.GetBlock("StrongCottonBlock:Dry", "CRDescription")
		};
		int value = Terrain.MakeBlockValue(BlockIndex, 0, SetMode(0, CottonMode.Wet));
		craftingRecipe.Ingredients[0] = "strongcotton:" + Terrain.ExtractData(value).ToString(CultureInfo.InvariantCulture);
		yield return craftingRecipe;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int data = Terrain.ExtractData(value);
		int? color2 = PaintedCubeBlock.GetColor(data);
		Color color3 = (color2.HasValue ? SubsystemPalette.GetColor(environmentData, color2) : color);
		Color color4 = ((GetMode(data) == CottonMode.Wet && !color2.HasValue) ? (color3 * new Color(0.8f, 0.9f, 1f)) : color3);
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color4, color4, environmentData);
	}

	public override float GetHeat(int value)
	{
		if (GetMode(Terrain.ExtractData(value)) != CottonMode.Dry)
		{
			return -0.1f;
		}
		return 0.4f;
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		CottonMode mode = GetMode(Terrain.ExtractData(oldValue));
		int num2 = Terrain.MakeBlockValue(BlockIndex, 0, SetMode(0, mode));
		dropValues.Add(new BlockDropValue
		{
			Value = num2,
			Count = 1
		});
		showDebris = true;
	}
}
