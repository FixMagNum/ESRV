using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class QuicksandBucketBlock : BucketBlock
{
	public const int Index = 392;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh m_bucketMesh = new BlockMesh();

	public BlockMesh m_contentsMesh = new BlockMesh();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/FullBucket");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bucket").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Contents").ParentBone);
		m_bucketMesh.AppendModelMeshPart(model.FindMesh("Bucket").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_contentsMesh.AppendModelMeshPart(model.FindMesh("Contents").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_contentsMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.125f, 0.0625f, 0f));
		m_standaloneBlockMesh.AppendBlockMesh(m_contentsMesh);
		m_standaloneBlockMesh.AppendBlockMesh(m_bucketMesh);
		base.Initialize();
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_bucketMesh, color, 2f * size, ref matrix, environmentData);
		BlocksManager.DrawMeshBlock(color: (Terrain.ExtractData(value) != 1) ? (new Color(255, 230, 180) * color) : (new Color(66, 50, 36) * color), primitivesRenderer: primitivesRenderer, blockMesh: m_contentsMesh, size: 2f * size, matrix: ref matrix, environmentData: environmentData);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, 0);
		yield return Terrain.MakeBlockValue(BlockIndex, 0, 1);
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int num = Terrain.ExtractData(value);
		return LanguageControl.GetBlock($"QuicksandBucketBlock:{num}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int num = Terrain.ExtractData(value);
		return LanguageControl.GetBlock($"QuicksandBucketBlock:{num}", "Description");
	}
}
