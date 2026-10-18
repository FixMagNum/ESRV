using Engine;
using Engine.Graphics;

namespace Game;

public class FocalorsMagmaBlock : FocalorsFluidBlock
{
	public const int Index = 92;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		Color color = new Color(255, 255, 255);
		Color topColor = color;
		GenerateFluidTerrainVertices(generator, value, x, y, z, color, topColor, geometry.OpaqueSubsetsByFace);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		DrawFocalorsFluidBlock(primitivesRenderer, value, Color.White, size, ref matrix, environmentData);
	}

	public override bool ShouldAvoid(int value)
	{
		return true;
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		if (FocalorsFluidBlock.GetIsTop(Terrain.ExtractData(value)))
		{
			return face != 5;
		}
		return false;
	}
}
