using Engine;
using Engine.Graphics;

namespace Game;

public class ClockBoxBlock : Block
{
	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public const int Index = 386;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/ClockBoxBlock");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Clock").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(model.FindMesh("Clock").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
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

	public ClockBoxBlock()
	{
		Initialize();
	}
}
