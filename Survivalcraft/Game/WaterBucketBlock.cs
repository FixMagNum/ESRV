using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class WaterBucketBlock : BucketBlock
{
	public const int Index = 91;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh m_bucketMesh = new BlockMesh();

	public BlockMesh m_waterMesh = new BlockMesh();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/FullBucket");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bucket").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Contents").ParentBone);
		m_bucketMesh.AppendModelMeshPart(model.FindMesh("Bucket").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_waterMesh.AppendModelMeshPart(model.FindMesh("Contents").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, new Color(32, 80, 224, 255));
		m_waterMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.8125f, 0.6875f, 0f));
		m_standaloneBlockMesh.AppendBlockMesh(m_waterMesh);
		m_standaloneBlockMesh.AppendBlockMesh(m_bucketMesh);
		base.Initialize();
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_bucketMesh, color, 2f * size, ref matrix, environmentData);
		float level = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value));
		float num = 1f - level;
		if (num > 0.01f)
		{
			float num2 = MathUtils.Lerp(0.8f, 1f, num);
			float y = MathUtils.Lerp(-0.2f, 0f, num);
			Matrix matrix2 = Matrix.CreateScale(num2, 1f, num2) * Matrix.CreateTranslation(0f, y, 0f) * matrix;
			matrix2 = Matrix.CreateScale(1f, num, 1f) * matrix2;
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_waterMesh, color, 2f * size, ref matrix2, environmentData);
		}
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		for (int i = 0; i < 8; i++)
		{
			float level = (float)i / 8f;
			int data = FocalorsFluidBlock.SetLevel(0, level);
			yield return Terrain.MakeBlockValue(BlockIndex, 0, data);
		}
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		string num = base.GetDisplayName(subsystemTerrain, value);
		float num2 = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value));
		float num3 = 1f - num2;
		return $"{num} : {num3:0.00}";
	}
}
