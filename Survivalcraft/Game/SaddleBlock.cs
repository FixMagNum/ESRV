using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class SaddleBlock : Block
{
	public enum SaddleType
	{
		Normal,
		Rawhide
	}

	public const int Index = 158;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh m_rawhideBlockMesh = new BlockMesh();

	public override void Initialize()
	{
		Model num = ContentManager.Get<Model>("Models/Saddle");
		Matrix i = BlockMesh.GetBoneAbsoluteTransform(num.FindMesh("Saddle").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(num.FindMesh("Saddle").MeshParts[0], i * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, new Color(224, 224, 224));
		m_standaloneBlockMesh.AppendModelMeshPart(num.FindMesh("Saddle").MeshParts[0], i * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: false, new Color(96, 96, 96));
		Model num2 = ContentManager.Get<Model>("Models/RawhideSaddle");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(num2.FindMesh("RawhideSaddle").ParentBone);
		m_rawhideBlockMesh.AppendModelMeshPart(num2.FindMesh("RawhideSaddle").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, new Color(255, 140, 120));
		m_rawhideBlockMesh.AppendModelMeshPart(num2.FindMesh("RawhideSaddle").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: false, new Color(150, 60, 50));
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlockMesh blockMesh = ((GetType(Terrain.ExtractData(value)) == SaddleType.Rawhide) ? m_rawhideBlockMesh : m_standaloneBlockMesh);
		BlocksManager.DrawMeshBlock(primitivesRenderer, blockMesh, color, 2f * size, ref matrix, environmentData);
	}

	public static SaddleType GetType(int data)
	{
		return (SaddleType)(data & 1);
	}

	public static int SetType(int data, SaddleType type)
	{
		return (data & -2) | (int)(type & SaddleType.Rawhide);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetType(0, SaddleType.Normal));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetType(0, SaddleType.Rawhide));
	}
}
