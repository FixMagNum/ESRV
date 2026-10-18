using Engine;

namespace Game;

public class MagmaBlock : FluidBlock
{
	public const int Index = 438;

	public static int MagmaMaxLevel;

	public MagmaBlock()
		: base(MagmaMaxLevel)
	{
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		if (FluidBlock.GetIsTop(Terrain.ExtractData(value)))
		{
			return face != 5;
		}
		return false;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		GenerateFluidTerrainVertices(generator, value, x, y, z, Color.White, Color.White, geometry.OpaqueSubsetsByFace);
	}

	public override bool ShouldAvoid(int value)
	{
		return true;
	}

	static MagmaBlock()
	{
		MagmaMaxLevel = 4;
	}
}
