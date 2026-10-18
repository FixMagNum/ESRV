using Engine;
using Engine.Graphics;

namespace Game;

public class IronOreChunkBlock : Block
{
	public const int Index = 249;

	private BlockMesh m_blockMesh = new BlockMesh();

	public override void Initialize()
	{
		Model num = ContentManager.Get<Model>("Models/IronOreChunk");
		Matrix result = BlockMesh.GetBoneAbsoluteTransform(num.FindMesh("IronOreChunk").ParentBone);
		m_blockMesh.AppendModelMeshPart(num.FindMesh("IronOreChunk").MeshParts[0], result, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMesh, Color.White, null, null, geometry.SubsetAlphaTest);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_blockMesh, color, size, ref matrix, environmentData);
	}
}
