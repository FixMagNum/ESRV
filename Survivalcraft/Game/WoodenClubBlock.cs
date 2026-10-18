using Engine;
using Engine.Graphics;

namespace Game;

public class WoodenClubBlock : Block
{
	public const int Index = 122;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public override void Initialize()
	{
		int offset = 47;
		Model flag = ContentManager.Get<Model>("Models/WoodenClub");
		Matrix num = BlockMesh.GetBoneAbsoluteTransform(flag.FindMesh("Handle").ParentBone);
		BlockMesh num2 = new BlockMesh();
		num2.AppendModelMeshPart(flag.FindMesh("Handle").MeshParts[0], num * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		num2.TransformTextureCoordinates(Matrix.CreateTranslation((float)(offset % 16) / 16f, (float)(offset / 16) / 16f, 0f));
		m_standaloneBlockMesh.AppendBlockMesh(num2);
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
