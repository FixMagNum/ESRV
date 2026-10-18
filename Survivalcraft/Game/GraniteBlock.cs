using Engine;

namespace Game;

public class GraniteBlock : PaintedCubeBlock
{
	public const int Index = 3;

	public GraniteBlock()
		: base(24)
	{
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		Color color = SubsystemPalette.GetColor(generator, PaintedCubeBlock.GetColor(data));
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, color, geometry.OpaqueSubsetsByFace);
	}
}
