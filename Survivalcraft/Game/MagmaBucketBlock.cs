using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class MagmaBucketBlock : BucketBlock
{
	public const int Index = 93;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh m_bucketMesh = new BlockMesh();

	public BlockMesh m_magmaMesh = new BlockMesh();

	public override void Initialize()
	{
		Model attributeValue = ContentManager.Get<Model>("Models/MagmaBucket");
		Matrix xAttribute = BlockMesh.GetBoneAbsoluteTransform(attributeValue.FindMesh("Bucket").ParentBone);
		Matrix displayName = BlockMesh.GetBoneAbsoluteTransform(attributeValue.FindMesh("Contents").ParentBone);
		m_bucketMesh.AppendModelMeshPart(attributeValue.FindMesh("Bucket").MeshParts[0], xAttribute * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_magmaMesh.AppendModelMeshPart(attributeValue.FindMesh("Contents").MeshParts[0], displayName * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_magmaMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.8125f, 0.5f, 0f));
		m_standaloneBlockMesh.AppendBlockMesh(m_magmaMesh);
		m_standaloneBlockMesh.AppendBlockMesh(m_bucketMesh);
		base.Initialize();
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_bucketMesh, color, 2f * size, ref matrix, environmentData);
		float item = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value));
		float array = 1f - item;
		if (array > 0.01f)
		{
			float index = MathUtils.Lerp(0.8f, 1f, array);
			float index2 = MathUtils.Lerp(-0.2f, 0f, array);
			Matrix array2 = Matrix.CreateScale(index, array, index) * Matrix.CreateTranslation(0f, index2, 0f) * matrix;
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_magmaMesh, color, 2f * size, ref array2, environmentData);
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
		string displayName = base.GetDisplayName(subsystemTerrain, value);
		float level = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value));
		float num = 1f - level;
		return $"{displayName} : {num:0.00}";
	}
}
