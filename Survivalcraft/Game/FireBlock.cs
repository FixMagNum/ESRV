using Engine;
using Engine.Graphics;

namespace Game;

public class FireBlock : Block
{
	public const int Index = 104;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
	}

	public override bool ShouldAvoid(int value)
	{
		return true;
	}

	public static bool HasFireOnFace(int data, int face)
	{
		if (data != 0)
		{
			return (data & (1 << face)) != 0;
		}
		return true;
	}
}
