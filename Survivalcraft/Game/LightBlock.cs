using Engine;
using Engine.Graphics;

namespace Game;

public class LightBlock : Block
{
	public const int Index = 382;

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override int GetEmittedLightAmount(int value)
	{
		return MathUtils.Clamp(Terrain.ExtractData(value), 0, 15);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain terrain, Vector3 pos, int value, float strength)
	{
		return new BlockDebrisParticleSystem(terrain, pos, 0f, 0f, Color.Transparent, 0);
	}
}
