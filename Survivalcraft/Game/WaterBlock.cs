using Engine;

namespace Game;

public class WaterBlock : FluidBlock
{
	public const int Index = 437;

	public static int WaterMaxLevel;

	public WaterBlock()
		: base(WaterMaxLevel)
	{
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season num = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color num2 = BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z);
		Color num3;
		Color color = (num3 = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(num, num2));
		num3.A = byte.MaxValue;
		Color flag = color;
		flag.A = 0;
		float i = SimplexNoise.OctavedNoise(x, z, 0.1f, 1, 1f, 1f);
		int count = 2 + (int)(2f * i);
		if (y < count)
		{
			num3 = new Color(15, 0, 25);
			Color color2 = num3;
			num3.A = byte.MaxValue;
			flag = color2;
			flag.A = 0;
		}
		GenerateFluidTerrainVertices(generator, value, x, y, z, num3, flag, geometry.TransparentSubsetsByFace);
	}

	static WaterBlock()
	{
		WaterMaxLevel = 7;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		BlockPlacementData result = new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, BlockIndex), 0),
			CellFace = raycastResult.CellFace
		};
		int num = Terrain.ExtractData(result.Value);
		num |= 0x20;
		result.Value = Terrain.ReplaceData(result.Value, num);
		return result;
	}
}
