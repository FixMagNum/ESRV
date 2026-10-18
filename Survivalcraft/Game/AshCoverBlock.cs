using Engine;

namespace Game;

public class AshCoverBlock : CubeBlock
{
	public const int Index = 377;

	public BoundingBox[] m_collisionBoxes = new BoundingBox[1]
	{
		new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.125f, 1f))
	};

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateCubeVertices(this, value, x, y, z, 0.125f, 0.125f, 0.125f, 0.125f, Color.White, Color.White, Color.White, Color.White, Color.White, -1, geometry.OpaqueSubsetsByFace);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		return face != 5;
	}
}
