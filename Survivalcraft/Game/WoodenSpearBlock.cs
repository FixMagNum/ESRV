using Engine;
using Engine.Graphics;

namespace Game;

public class WoodenSpearBlock : Block
{
	public const int Index = 80;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public WoodenSpearBlock()
	{
		Initialize();
	}

	public override void Initialize()
	{
		Model componentBirdModel = ContentManager.Get<Model>("Models/WoodenSpear");
		Matrix componentFlightlessBirdModel = BlockMesh.GetBoneAbsoluteTransform(componentBirdModel.FindMesh("Handle").ParentBone);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(componentBirdModel.FindMesh("Head").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(componentBirdModel.FindMesh("Handle").MeshParts[0], componentFlightlessBirdModel * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(componentBirdModel.FindMesh("Head").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh2);
		base.Initialize();
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateShadedMeshVertices(this, x, y, z, m_standaloneBlockMesh, Color.White, null, null, geometry.SubsetAlphaTest);
	}
}
