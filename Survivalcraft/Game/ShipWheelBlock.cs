using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class ShipWheelBlock : Block
{
	public enum WheelMode
	{
		Rest,
		Left,
		Right
	}

	private readonly BlockMesh[] m_meshByFace = new BlockMesh[6];

	private BlockMesh m_standaloneBlockMesh;

	public const int Index = 435;

	private readonly BoundingBox[][] m_collisionBoxesByFace = new BoundingBox[6][];

	public override void Initialize()
	{
		ModelMesh modelMesh = ContentManager.Get<Model>("Models/Wheel").FindMesh("Wheel");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		m_meshByFace[4] = new BlockMesh();
		m_meshByFace[4].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_meshByFace[5] = new BlockMesh();
		m_meshByFace[5].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_meshByFace[0] = new BlockMesh();
		m_meshByFace[0].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_meshByFace[2] = new BlockMesh();
		m_meshByFace[2].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 1f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_meshByFace[1] = new BlockMesh();
		m_meshByFace[1].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationZ(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.5f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_meshByFace[3] = new BlockMesh();
		m_meshByFace[3].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationZ((float)Math.PI / 2f) * Matrix.CreateTranslation(1f, 0.5f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh = new BlockMesh();
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collisionBoxesByFace[4] = new BoundingBox[1]
		{
			new BoundingBox(0f, 0f, 0f, 1f, 0.2f, 1f)
		};
		m_collisionBoxesByFace[5] = new BoundingBox[1]
		{
			new BoundingBox(0f, 0.8f, 0f, 1f, 1f, 1f)
		};
		m_collisionBoxesByFace[0] = new BoundingBox[1]
		{
			new BoundingBox(0f, 0f, 0f, 1f, 1f, 0.2f)
		};
		m_collisionBoxesByFace[2] = new BoundingBox[1]
		{
			new BoundingBox(0f, 0f, 0.8f, 1f, 1f, 1f)
		};
		m_collisionBoxesByFace[1] = new BoundingBox[1]
		{
			new BoundingBox(0f, 0f, 0f, 0.2f, 1f, 1f)
		};
		m_collisionBoxesByFace[3] = new BoundingBox[1]
		{
			new BoundingBox(0.8f, 0f, 0f, 1f, 1f, 1f)
		};
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = GetMountFace(Terrain.ExtractData(value));
		if (num < 0 || num > 5)
		{
			num = 4;
		}
		generator.GenerateShadedMeshVertices(this, x, y, z, m_meshByFace[num], Color.White, null, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int face = raycastResult.CellFace.Face;
		int data = SetMode(SetMountFace(0, face), WheelMode.Rest);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int data = SetMode(0, WheelMode.Rest);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			Count = 1
		});
		showDebris = true;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int mountFace = GetMountFace(Terrain.ExtractData(value));
		if (mountFace < 0 || mountFace > 5)
		{
			return m_collisionBoxesByFace[4];
		}
		return m_collisionBoxesByFace[mountFace];
	}

	public static int GetMountFace(int data)
	{
		return data & 7;
	}

	public static int SetMountFace(int data, int face)
	{
		return (data & -8) | (face & 7);
	}

	public static WheelMode GetMode(int data)
	{
		return (WheelMode)((data >> 3) & 3);
	}

	public static int SetMode(int data, WheelMode mode)
	{
		return (data & -25) | ((int)(mode & (WheelMode)3) << 3);
	}
}
