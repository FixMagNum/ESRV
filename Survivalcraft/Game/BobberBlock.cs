using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class BobberBlock : Block
{
	public const int Index = 396;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	private const int BaitIndexMask = 31;

	private const int BaitRotBit = 32;

	private const int BaitDataMask = 960;

	private const int BaitDataShift = 6;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2.5f * size, ref matrix, environmentData);
		int baitValue = GetBaitValue(Terrain.ExtractData(value));
		if (baitValue != 0)
		{
			Block obj = BlocksManager.Blocks[Terrain.ExtractContents(baitValue)];
			Vector3 vector = -matrix.Up;
			Vector3 forward = matrix.Forward;
			float num = size * 0.85f;
			float num2 = size * 0f;
			Matrix matrix2 = matrix;
			Vector3 translation = matrix.Translation + vector * num + forward * num2;
			matrix2.Translation = translation;
			float size2 = size * 0.6f;
			obj.DrawBlock(primitivesRenderer, baitValue, Color.White, size2, ref matrix2, environmentData);
		}
	}

	public override float GetProjectilePower(int value)
	{
		return 0f;
	}

	public override float GetExplosionPressure(int value)
	{
		return 0f;
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(396, 0, 0);
	}

	public override void Initialize()
	{
		ModelMesh modelMesh = ContentManager.Get<Model>("Models/FishingRod").FindMesh("Bobber");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		m_standaloneBlockMesh = new BlockMesh();
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public static int GetBait(int data)
	{
		return BobberBaitInfo.GetContentsByIndex(data & 0x1F);
	}

	public static bool HasBait(int data)
	{
		return (data & 0x1F) != 0;
	}

	public static int SetBait(int data, int contents, int rot)
	{
		return SetBait(data, contents, 0, rot);
	}

	public static int GetBaitRot(int data)
	{
		return (data >> 5) & 1;
	}

	public static int GetBaitData(int data)
	{
		return (data & 0x3C0) >> 6;
	}

	public static int GetBaitValue(int data)
	{
		int bait = GetBait(data);
		if (bait == 0)
		{
			return 0;
		}
		int data2 = GetBaitData(data) & 0xF;
		return Terrain.MakeBlockValue(bait, 0, data2);
	}

	public static int SetBait(int data, int contents, int baitData, int rot)
	{
		int num = -1024;
		data &= num;
		int num2 = BobberBaitInfo.GetIndex(contents);
		if (num2 < 0)
		{
			num2 = 0;
		}
		data |= num2 & 0x1F;
		if ((rot & 1) != 0)
		{
			data |= 0x20;
		}
		data |= (baitData & 0xF) << 6;
		return data;
	}
}
