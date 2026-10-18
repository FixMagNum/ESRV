using Engine;
using Engine.Graphics;

namespace Game;

public class RhinoHornBlock : Block
{
	public const int Index = 349;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Rhino horn");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Rhino horn").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Rhino horn").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.04f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, new Color(255, 255, 255));
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Rhino horn").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.04f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: false, new Color(64, 64, 64));
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 9f * size, ref matrix, environmentData);
	}
}
