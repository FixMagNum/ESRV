using System.Collections.Generic;
using Engine;

namespace Game;

public class LargeDryBushBlock : CrossBlock
{
	public const int Index = 99;

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		dropValues.Add(new BlockDropValue
		{
			Value = 23,
			Count = 1
		});
		showDebris = true;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season num = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		int num2 = ((num == SubsystemWeather.Season.Spring || num == SubsystemWeather.Season.LateSpring || num == SubsystemWeather.Season.EarlySummer) ? 234 : 71);
		Color num3 = BlockColorsMap.GrassColorsMap.Lookup(generator.Terrain, x, y, z);
		generator.GenerateCrossfaceVertices(this, value, x, y, z, num3, num2, geometry.SubsetAlphaTest);
	}
}
