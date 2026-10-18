using Engine;
using Engine.Graphics;

namespace Game;

public class StrawBasketBlock : Block
{
	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	private BlockMesh m_blockMesh;

	public const int Index = 308;

	private BoundingBox[] m_collisionBoxes;

	private BlockMesh m_blockMeshWithStalk;

	private BlockMesh m_handMesh = new BlockMesh();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/StrawBasket");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Basket").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Basket").ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Stalk").ParentBone);
		m_blockMesh.AppendModelMeshPart(model.FindMesh("Basket").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMesh.AppendModelMeshPart(model.FindMesh("Basket").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshWithStalk.AppendModelMeshPart(model.FindMesh("Basket").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshWithStalk.AppendModelMeshPart(model.FindMesh("Stalk").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.7f, 1f))
		};
		Matrix matrix = Matrix.CreateScale(1.3f) * Matrix.CreateTranslation(0f, -0.5f, 0f);
		m_handMesh.AppendModelMeshPart(model.FindMesh("Basket").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		BlockMesh blockMesh = m_blockMesh;
		if (Terrain.ExtractContents(generator.SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)) == 308)
		{
			blockMesh = m_blockMeshWithStalk;
		}
		generator.GenerateMeshVertices(this, x, y, z, blockMesh, Color.White, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_handMesh, color, size, ref matrix, environmentData);
	}

	public StrawBasketBlock()
	{
		m_blockMesh = new BlockMesh();
		m_blockMeshWithStalk = new BlockMesh();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}
}
