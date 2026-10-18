using Engine;
using Engine.Graphics;

namespace Game;

public class UmbrellaBlock : Block
{
	public const int Index = 379;

	public BlockMesh[] m_meshesByData = new BlockMesh[2];

	public BlockMesh m_standaloneMesh = new BlockMesh();

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[2][];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Umbrella");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("UmbrellaClose").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("UmbrellaOpen").ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Stick").ParentBone);
		for (int i = 0; i < 2; i++)
		{
			m_meshesByData[i] = new BlockMesh();
			if (i == 0)
			{
				m_meshesByData[i].AppendModelMeshPart(model.FindMesh("UmbrellaClose").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			}
			else
			{
				m_meshesByData[i].AppendModelMeshPart(model.FindMesh("UmbrellaOpen").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			}
			m_meshesByData[i].AppendModelMeshPart(model.FindMesh("Stick").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			BoundingBox boundingBox = m_meshesByData[i].CalculateBoundingBox();
			boundingBox.Min.X = 0f;
			boundingBox.Min.Z = 0f;
			boundingBox.Max.X = 1f;
			boundingBox.Max.Z = 1f;
			m_collisionBoxesByData[i] = new BoundingBox[1] { boundingBox };
		}
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("UmbrellaClose").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Stick").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = Terrain.ExtractData(value) & 1;
		if (num < m_collisionBoxesByData.Length)
		{
			return m_collisionBoxesByData[num];
		}
		return null;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = Terrain.ExtractData(value) & 1;
		if (num < m_meshesByData.Length)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_meshesByData[num], color, size, ref matrix, environmentData);
		}
		else
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
		}
	}
}
