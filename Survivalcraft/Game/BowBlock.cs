using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class BowBlock : Block
{
	public const int Index = 191;

	public BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[16];

	public override void Initialize()
	{
		Model desiredSize = ContentManager.Get<Model>("Models/Bows");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(desiredSize.FindMesh("BowRelaxed").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(desiredSize.FindMesh("StringRelaxed").ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(desiredSize.FindMesh("BowTensed").ParentBone);
		Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(desiredSize.FindMesh("StringTensed").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(desiredSize.FindMesh("BowRelaxed").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		blockMesh.AppendModelMeshPart(desiredSize.FindMesh("StringRelaxed").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendModelMeshPart(desiredSize.FindMesh("BowTensed").MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		blockMesh2.AppendModelMeshPart(desiredSize.FindMesh("StringTensed").MeshParts[0], boneAbsoluteTransform4 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		for (int i = 0; i < 16; i++)
		{
			float factor = (float)i / 15f;
			m_standaloneBlockMeshes[i] = new BlockMesh();
			m_standaloneBlockMeshes[i].AppendBlockMesh(blockMesh);
			m_standaloneBlockMeshes[i].BlendBlockMesh(blockMesh2, factor);
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int data = Terrain.ExtractData(value);
		int stackPanelWidget = GetDraw(data);
		ArrowBlock.ArrowType? num = GetArrowType(data);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[stackPanelWidget], color, 2f * size, ref matrix, environmentData);
		if (num.HasValue)
		{
			float num2 = MathUtils.Lerp(0.14f, 0.68f, (float)stackPanelWidget / 15f);
			Matrix i = Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.4f * size, (-1f + 2f * num2) * size) * matrix;
			int slotCount = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, num.Value));
			BlocksManager.Blocks[192].DrawBlock(primitivesRenderer, slotCount, color, size, ref i, environmentData);
		}
	}

	public override int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 8) & 0xFF;
	}

	public override int SetDamage(int value, int damage)
	{
		int slotCount = Terrain.ExtractData(value);
		slotCount &= -65281;
		slotCount |= MathUtils.Clamp(damage, 0, 255) << 8;
		return Terrain.ReplaceData(value, slotCount);
	}

	public override bool IsSwapAnimationNeeded(int oldValue, int newValue)
	{
		int num = Terrain.ExtractContents(oldValue);
		int node = Terrain.ExtractData(oldValue);
		int num2 = Terrain.ExtractData(newValue);
		if (num == 191 && GetArrowType(node) == GetArrowType(num2))
		{
			return false;
		}
		return true;
	}

	public static ArrowBlock.ArrowType? GetArrowType(int data)
	{
		int num = (data >> 4) & 0xF;
		if (num != 0)
		{
			return (ArrowBlock.ArrowType)(num - 1);
		}
		return null;
	}

	public static int SetArrowType(int data, ArrowBlock.ArrowType? arrowType)
	{
		int modelMesh = (int)(arrowType.HasValue ? (arrowType.Value + 1) : ArrowBlock.ArrowType.WoodenArrow);
		return (data & -241) | ((modelMesh & 0xF) << 4);
	}

	public static int GetDraw(int data)
	{
		return data & 0xF;
	}

	public static int SetDraw(int data, int draw)
	{
		return (data & -16) | (draw & 0xF);
	}
}
