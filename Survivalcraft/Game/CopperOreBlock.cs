using Engine;

namespace Game;

public class CopperOreBlock : CubeBlock
{
	public const int Index = 41;

	private static readonly Random _random;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}

	static CopperOreBlock()
	{
		_random = new Random();
	}
}
