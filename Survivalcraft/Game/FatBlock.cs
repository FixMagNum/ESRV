using Engine;

namespace Game;

public class FatBlock : CubeBlock
{
	public const int Index = 393;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}
}
