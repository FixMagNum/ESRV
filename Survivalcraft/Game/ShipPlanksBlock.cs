using System.Collections.Generic;
using Engine;

namespace Game;

public class ShipPlanksBlock : CubeBlock
{
	public const int Index = 434;

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner m, int value, TerrainRaycastResult hit)
	{
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(434, 0, 0),
			CellFace = hit.CellFace
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		g.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geom.OpaqueSubsetsByFace);
	}

	public override void Initialize()
	{
		base.Initialize();
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(434, 0, 0),
			Count = 1
		});
		showDebris = true;
	}
}
