using Engine;

namespace Game;

public class CoalBlock : CubeBlock
{
	public const int Index = 150;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}
}
