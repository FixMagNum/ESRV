using Engine;
using Engine.Graphics;

namespace Game;

public class FishingRodBlock : Block
{
	public const int Index = 395;

	public BlockMesh[] m_standaloneBlockMeshes;

	public BlockMesh m_bobberBlockMesh;

	public Vector3[] m_lineOffsets = new Vector3[3];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/FishingRod");
		ModelMesh modelMesh = model.FindMesh("RodRelaxed");
		ModelMesh modelMesh2 = model.FindMesh("RodTensed1");
		ModelMesh modelMesh3 = model.FindMesh("RodTensed2");
		ModelMesh modelMesh4 = model.FindMesh("Bobber");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(modelMesh3.ParentBone);
		Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(modelMesh4.ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(modelMesh2.MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh3 = new BlockMesh();
		blockMesh3.AppendModelMeshPart(modelMesh3.MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMeshes[0] = blockMesh;
		m_standaloneBlockMeshes[1] = blockMesh2;
		m_standaloneBlockMeshes[2] = blockMesh3;
		m_bobberBlockMesh = new BlockMesh();
		m_bobberBlockMesh.AppendModelMeshPart(modelMesh4.MeshParts[0], boneAbsoluteTransform4 * Matrix.CreateTranslation(0f, 0.4f, -0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		ModelMesh modelMesh5 = model.FindMesh("FishlineRelaxed");
		ModelMesh modelMesh6 = model.FindMesh("FishlineTensed1");
		ModelMesh modelMesh7 = model.FindMesh("FishlineTensed2");
		Matrix m = BlockMesh.GetBoneAbsoluteTransform(modelMesh5.ParentBone) * Matrix.CreateTranslation(0f, -0.5f, 0f);
		Matrix m2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh6.ParentBone) * Matrix.CreateTranslation(0f, -0.5f, 0f);
		Matrix m3 = BlockMesh.GetBoneAbsoluteTransform(modelMesh7.ParentBone) * Matrix.CreateTranslation(0f, -0.5f, 0f);
		m_lineOffsets[0] = Vector3.Transform(Vector3.Zero, m);
		m_lineOffsets[1] = Vector3.Transform(Vector3.Zero, m2);
		m_lineOffsets[2] = Vector3.Transform(Vector3.Zero, m3);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = Terrain.ExtractData(value);
		int num2 = num & 3;
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[num2], color, 2f * size, ref matrix, environmentData);
		if (HasBobber(num))
		{
			Matrix matrix2 = Matrix.CreateRotationX(1.5708f) * matrix;
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_bobberBlockMesh, Color.White, 2.5f * size, ref matrix2, environmentData);
			int bobberBait = GetBobberBait(num);
			if (bobberBait != 0)
			{
				int data = GetRodBaitData(num) & 0xF;
				Block obj = BlocksManager.Blocks[bobberBait];
				Vector3 vector = -matrix2.Up;
				Vector3 forward = matrix2.Forward;
				float num3 = size * -0.15f;
				float num4 = size * 1.3f;
				Matrix matrix3 = matrix2;
				matrix3.Translation += vector * num3 + forward * num4;
				float size2 = size * 0.6f;
				int value2 = Terrain.MakeBlockValue(bobberBait, 0, data);
				obj.DrawBlock(primitivesRenderer, value2, Color.White, size2, ref matrix3, environmentData);
			}
		}
	}

	public override int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 13) & 7;
	}

	public override int SetDamage(int value, int damage)
	{
		int num = Terrain.ExtractData(value);
		num &= -57345;
		num |= MathUtils.Clamp(damage, 0, 7) << 13;
		return Terrain.ReplaceData(value, num);
	}

	public FishingRodBlock()
	{
		m_standaloneBlockMeshes = new BlockMesh[3];
	}

	public static bool HasBobber(int data)
	{
		return (data & 4) != 0;
	}

	public static int GetBobberBait(int data)
	{
		return BobberBaitInfo.GetContentsByIndex((data >> 3) & 0x1F);
	}

	public static int GetBobberBaitRot(int data)
	{
		return (data >> 16) & 1;
	}

	public static int SetBobber(int data, bool hasBobber, int contents, int rot)
	{
		data &= -253;
		data &= -65537;
		if (hasBobber)
		{
			data |= 4;
		}
		int index = BobberBaitInfo.GetIndex(contents);
		data |= (index & 0x1F) << 3;
		if ((rot & 1) != 0)
		{
			data |= 0x10000;
		}
		return data;
	}

	public static int GetRodBaitData(int data)
	{
		return (data >> 8) & 0xF;
	}

	public static int SetRodBaitData(int data, int baitData)
	{
		data &= -3841;
		data |= (baitData & 0xF) << 8;
		return data;
	}
}
