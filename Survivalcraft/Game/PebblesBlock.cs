using Engine;

namespace Game;

public class PebblesBlock : PaintedCubeBlock
{
	public const int Index = 401;

	public PebblesBlock()
		: base(242)
	{
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		Color color = SubsystemPalette.GetColor(generator, PaintedCubeBlock.GetColor(data));
		generator.GenerateCubeVertices(this, value, x, y, z, color, geometry.OpaqueSubsetsByFace);
	}
}
