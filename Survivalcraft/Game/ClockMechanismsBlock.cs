using Engine;
using Engine.Graphics;

namespace Game;

public class ClockMechanismsBlock : Block
{
	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public const int Index = 387;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/ClockMechanismsBlock");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Clock").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Hour hand").ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Second hand").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(model.FindMesh("Clock").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(model.FindMesh("Hour hand").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		new BlockMesh().AppendModelMeshPart(model.FindMesh("Second hand").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
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

	public ClockMechanismsBlock()
	{
		Initialize();
	}
}
