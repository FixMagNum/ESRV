using Engine;
using Engine.Graphics;

namespace Game;

public class SnowballBlock : Block
{
	public const int Index = 85;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public override void Initialize()
	{
		Model coordinates = ContentManager.Get<Model>("Models/Snowball");
		Matrix vector = BlockMesh.GetBoneAbsoluteTransform(coordinates.FindMesh("Snowball").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(coordinates.FindMesh("Snowball").MeshParts[0], vector * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2.5f * size, ref matrix, environmentData);
	}
}
