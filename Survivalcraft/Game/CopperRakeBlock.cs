using Engine;
using Engine.Graphics;

namespace Game;

public class CopperRakeBlock : Block
{
	public const int Index = 219;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public CopperRakeBlock()
	{
		Initialize();
	}

	public override void Initialize()
	{
		Model cShadow = ContentManager.Get<Model>("Models/CopperRake");
		Matrix color = BlockMesh.GetBoneAbsoluteTransform(cShadow.FindMesh("Handle").ParentBone);
		Matrix cBevel = BlockMesh.GetBoneAbsoluteTransform(cShadow.FindMesh("Head").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(cShadow.FindMesh("Handle").MeshParts[0], color * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh c = new BlockMesh();
		c.AppendModelMeshPart(cShadow.FindMesh("Head").MeshParts[0], cBevel * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
		m_standaloneBlockMesh.AppendBlockMesh(c);
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
}
