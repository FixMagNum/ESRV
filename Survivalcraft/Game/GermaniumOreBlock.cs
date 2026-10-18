using Engine;

namespace Game;

public class GermaniumOreBlock : CubeBlock
{
	public const int Index = 148;

	private static readonly Random _random;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}

	static GermaniumOreBlock()
	{
		_random = new Random();
	}
}
