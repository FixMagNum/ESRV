using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CampfireBlock : Block
{
	public const int Index = 209;

	public BlockMesh[] m_meshesByData = new BlockMesh[16];

	public BlockMesh m_standaloneMesh = new BlockMesh();

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[16][];

	private static readonly Random _random = new Random();

	public override void Initialize()
	{
		Model size = ContentManager.Get<Model>("Models/Campfire");
		Matrix value = BlockMesh.GetBoneAbsoluteTransform(size.FindMesh("Wood").ParentBone);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(size.FindMesh("Ashes").ParentBone);
		Matrix flag = BlockMesh.GetBoneAbsoluteTransform(size.FindMesh("Stick").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(size.FindMesh("Brushwood").ParentBone);
		for (int slotValue2 = 0; slotValue2 < 16; slotValue2++)
		{
			m_meshesByData[slotValue2] = new BlockMesh();
			if (slotValue2 == 0)
			{
				m_meshesByData[slotValue2].AppendModelMeshPart(size.FindMesh("Ashes").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(3f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			}
			else
			{
				float scale = MathUtils.Lerp(1.5f, 4f, (float)slotValue2 / 15f);
				float radians = (float)slotValue2 * (float)Math.PI / 2f;
				m_meshesByData[slotValue2].AppendModelMeshPart(size.FindMesh("Ashes").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(scale) * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				if (slotValue2 >= 1 && slotValue2 <= 5)
				{
					m_meshesByData[slotValue2].AppendModelMeshPart(size.FindMesh("Brushwood").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateScale(scale) * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				}
				else
				{
					m_meshesByData[slotValue2].AppendModelMeshPart(size.FindMesh("Wood").MeshParts[0], value * Matrix.CreateScale(scale) * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_meshesByData[slotValue2].AppendModelMeshPart(size.FindMesh("Stick").MeshParts[0], flag * Matrix.CreateScale(scale) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				}
			}
			BoundingBox i = m_meshesByData[slotValue2].CalculateBoundingBox();
			i.Min.X = 0f;
			i.Min.Z = 0f;
			i.Max.X = 1f;
			i.Max.Y = MathUtils.Min(i.Max.Y, 1f);
			i.Max.Z = 1f;
			m_collisionBoxesByData[slotValue2] = new BoundingBox[1] { i };
		}
		Model model = ContentManager.Get<Model>("Models/Campfire");
		Matrix componentPlayer = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Wood").ParentBone);
		Matrix slotValue3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Ashes").ParentBone);
		Matrix componentPlayer2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Stick").ParentBone);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Wood").MeshParts[0], componentPlayer * Matrix.CreateScale(3f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Ashes").MeshParts[0], slotValue3 * Matrix.CreateScale(3f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Stick").MeshParts[0], componentPlayer2 * Matrix.CreateScale(4f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int size = Terrain.ExtractData(value);
		if (size < m_collisionBoxesByData.Length)
		{
			return m_collisionBoxesByData[size];
		}
		return null;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (data < m_meshesByData.Length)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_meshesByData[data], Color.White, null, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		return new BlockPlacementData
		{
			CellFace = raycastResult.CellFace,
			Value = Terrain.MakeBlockValue(209, 0, 1)
		};
	}

	public override bool ShouldAvoid(int value)
	{
		return Terrain.ExtractData(value) > 0;
	}

	public override int GetEmittedLightAmount(int value)
	{
		int num = Terrain.ExtractData(value);
		if (num > 0)
		{
			return MathUtils.Min(8 + num / 2, 15);
		}
		return 0;
	}

	public override float GetHeat(int value)
	{
		if (Terrain.ExtractData(value) <= 0)
		{
			return 0f;
		}
		return base.GetHeat(value);
	}

	public void UpdateCampfireMesh(SubsystemTerrain subsystemTerrain, int x, int y, int z)
	{
		int num = subsystemTerrain.Terrain.GetCellValue(x, y, z);
		subsystemTerrain.ChangeCell(x, y, z, num);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if (toolLevel >= RequiredToolLevel && _random.Float(0f, 1f) < 0.1f)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(22),
				Count = 1
			});
		}
	}
}
