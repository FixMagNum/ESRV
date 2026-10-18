using Engine;
using Engine.Graphics;

namespace Game;

public class IronSpearBlock : Block
{
	public const int Index = 82;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	private BlockMesh m_worldMesh = new BlockMesh();

	private BlockMesh m_iconMesh = new BlockMesh();

	public IronSpearBlock()
	{
		Initialize();
	}

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/IronSpear");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Handle").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Head").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(model.FindMesh("Handle").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(model.FindMesh("Head").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_worldMesh = new BlockMesh();
		m_worldMesh.AppendBlockMesh(blockMesh);
		m_worldMesh.AppendBlockMesh(blockMesh2);
		Model model2 = ContentManager.Get<Model>("Models/IronSpear_Short");
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model2.FindMesh("Handle_Short").ParentBone);
		Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(model2.FindMesh("Head_Short").ParentBone);
		BlockMesh blockMesh3 = new BlockMesh();
		blockMesh3.AppendModelMeshPart(model2.FindMesh("Handle_Short").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh4 = new BlockMesh();
		blockMesh4.AppendModelMeshPart(model2.FindMesh("Head_Short").MeshParts[0], boneAbsoluteTransform4 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_iconMesh = new BlockMesh();
		m_iconMesh.AppendBlockMesh(blockMesh3);
		m_iconMesh.AppendBlockMesh(blockMesh4);
		base.Initialize();
	}

	public override void DrawBlock(PrimitivesRenderer3D pr, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		BlockMesh blockMesh = ((env != null && env.ViewProjectionMatrix.HasValue && !env.BillboardDirection.HasValue) ? m_iconMesh : m_worldMesh);
		BlocksManager.DrawMeshBlock(pr, blockMesh, color, size, ref m, env);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateShadedMeshVertices(this, x, y, z, m_standaloneBlockMesh, Color.White, null, null, geometry.SubsetAlphaTest);
	}
}
