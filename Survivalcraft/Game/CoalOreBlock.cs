using Engine;

namespace Game;

public class CoalOreBlock : CubeBlock
{
	public const int Index = 16;

	private static readonly Random _random;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}

	static CoalOreBlock()
	{
		_random = new Random();
	}
}
