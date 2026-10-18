using System.Collections.Generic;
using Engine;

namespace Game;

public class AmmoniaBlock : CubeBlock
{
	public const int Index = 312;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if (Random.Float(0f, 1f) < 0.5f)
		{
			int count = Random.Int(8, 9);
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(311),
				Count = count
			});
		}
		else
		{
			dropValues.Add(new BlockDropValue
			{
				Value = oldValue,
				Count = 1
			});
		}
	}
}
