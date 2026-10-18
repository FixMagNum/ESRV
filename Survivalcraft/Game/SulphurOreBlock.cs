using Engine;

namespace Game;

public class SulphurOreBlock : CubeBlock
{
	public const int Index = 101;

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner m, int value, TerrainRaycastResult hit)
	{
		return new BlockPlacementData
		{
			Value = value,
			CellFace = hit.CellFace
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		g.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geom.OpaqueSubsetsByFace);
	}
}
