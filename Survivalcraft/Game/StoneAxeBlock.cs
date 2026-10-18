using Engine;
using Engine.Graphics;

namespace Game;

public class StoneAxeBlock : Block
{
	public const int Index = 29;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public override void Initialize()
	{
		Model stringBuilder = ContentManager.Get<Model>("Models/StoneAxe");
		Matrix flag = BlockMesh.GetBoneAbsoluteTransform(stringBuilder.FindMesh("Handle").ParentBone);
		Matrix i = BlockMesh.GetBoneAbsoluteTransform(stringBuilder.FindMesh("Head").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(stringBuilder.FindMesh("Handle").MeshParts[0], flag * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(stringBuilder.FindMesh("Head").MeshParts[0], i * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh2);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateShadedMeshVertices(this, x, y, z, m_standaloneBlockMesh, Color.White, null, null, geometry.SubsetAlphaTest);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public StoneAxeBlock()
	{
		Initialize();
	}
}
