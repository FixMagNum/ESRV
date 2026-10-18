using Engine;
using Engine.Graphics;

namespace Game;

public class CattleBellBlock : Block
{
	public const int Index = 358;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/CattleBell");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("CattleBell").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("CattleBell").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, new Color(224, 224, 224));
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("CattleBell").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: false, new Color(96, 96, 96));
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
	}
}
