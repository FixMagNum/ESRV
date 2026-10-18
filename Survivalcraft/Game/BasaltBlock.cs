using Engine;

namespace Game;

public class BasaltBlock : PaintedCubeBlock
{
	public const int Index = 67;

	public BasaltBlock()
		: base(40)
	{
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int face = Terrain.ExtractData(value);
		Color color = SubsystemPalette.GetColor(generator, PaintedCubeBlock.GetColor(face));
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, color, geometry.OpaqueSubsetsByFace);
	}
}
