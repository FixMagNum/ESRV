using Engine;
using Engine.Graphics;

namespace Game;

public class DiamondShovelBlock : Block
{
	public const int Index = 113;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public DiamondShovelBlock()
	{
		Initialize();
	}

	public override void Initialize()
	{
		Model durability = ContentManager.Get<Model>("Models/DiamondShovel");
		Matrix damage = BlockMesh.GetBoneAbsoluteTransform(durability.FindMesh("Handle").ParentBone);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(durability.FindMesh("Head").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(durability.FindMesh("Handle").MeshParts[0], damage * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(durability.FindMesh("Head").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
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
