using Engine;
using Engine.Graphics;

namespace Game;

public class IcySnowBlock : CubeBlock
{
	public const int Index = 350;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		Color color = Color.White * new Color(0.9f, 0.95f, 1f);
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, color, geometry.OpaqueSubsetsByFace);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Color color2 = color * new Color(0.9f, 0.95f, 1f);
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color2, color2, environmentData);
	}
}
