using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class FermentationBarrelBlock : Block
{
	public const int Index = 428;

	private BlockMesh m_standalone;

	private BoundingBox[] m_collision;

	private readonly BlockMesh[] m_meshByFace = new BlockMesh[6];

	public override void Initialize()
	{
		ModelMesh modelMesh = ContentManager.Get<Model>("Models/FermentationBarrel").FindMesh("Keg");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		m_meshByFace[4] = new BlockMesh();
		m_meshByFace[4].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.Identity * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
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
		m_standalone = new BlockMesh();
		m_standalone.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collision = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f))
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
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standalone, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain terrain, ComponentMiner miner, int value, TerrainRaycastResult r)
	{
		int face = r.CellFace.Face;
		int data = 0;
		data = SetMountFace(data, face);
		data = SetActive(data, active: false);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = r.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> drops, out bool showDebris)
	{
		drops.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, SetMountFace(0, 4)),
			Count = 1
		});
		showDebris = true;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collision;
	}

	public static bool GetActive(int data)
	{
		return (data & 8) != 0;
	}

	public static int SetActive(int data, bool active)
	{
		if (!active)
		{
			return data & -9;
		}
		return data | 8;
	}

	public static int GetMountFace(int data)
	{
		return (data >> 4) & 7;
	}

	public static int SetMountFace(int data, int face)
	{
		return (data & -113) | ((MathUtils.Clamp(face, 0, 5) & 7) << 4);
	}
}
