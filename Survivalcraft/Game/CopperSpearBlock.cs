using Engine;
using Engine.Graphics;

namespace Game;

public class CopperSpearBlock : Block
{
	public const int Index = 220;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public CopperSpearBlock()
	{
		Initialize();
	}

	public override void Initialize()
	{
		Model num = ContentManager.Get<Model>("Models/CopperSpear");
		Matrix i = BlockMesh.GetBoneAbsoluteTransform(num.FindMesh("Handle").ParentBone);
		Matrix x = BlockMesh.GetBoneAbsoluteTransform(num.FindMesh("Head").ParentBone);
		BlockMesh x2 = new BlockMesh();
		x2.AppendModelMeshPart(num.FindMesh("Handle").MeshParts[0], i * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh p = new BlockMesh();
		p.AppendModelMeshPart(num.FindMesh("Head").MeshParts[0], x * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendBlockMesh(x2);
		m_standaloneBlockMesh.AppendBlockMesh(p);
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
