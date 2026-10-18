using System;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class BarrelBlock : Block, IPaintableBlock
{
	public string m_modelName;

	public BlockMesh[] m_blockMeshesByVariant;

	public BoundingBox[][] m_collisionBoxes;

	public BlockMesh m_standaloneBlockMesh;

	public BarrelBlock(string modelName)
	{
		m_modelName = modelName;
	}

	public override void Initialize()
	{
		ModelMesh modelMesh = ContentManager.Get<Model>(m_modelName).FindMesh("Keg");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		m_blockMeshesByVariant = new BlockMesh[6];
		for (int i = 0; i < 6; i++)
		{
			m_blockMeshesByVariant[i] = new BlockMesh();
		}
		m_blockMeshesByVariant[4].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshesByVariant[5].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshesByVariant[0].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshesByVariant[2].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 1f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshesByVariant[1].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationZ(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.5f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_blockMeshesByVariant[3].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationZ((float)Math.PI / 2f) * Matrix.CreateTranslation(1f, 0.5f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh = new BlockMesh();
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collisionBoxes = new BoundingBox[6][];
		for (int j = 0; j < 6; j++)
		{
			m_collisionBoxes[j] = new BoundingBox[1] { m_blockMeshesByVariant[j].CalculateBoundingBox() };
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		Color color = SubsystemPalette.GetColor(generator, GetColor(data));
		int variant = GetVariant(data);
		if (variant < m_blockMeshesByVariant.Length)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByVariant[variant], color, null, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int data = Terrain.ExtractData(value);
		color *= SubsystemPalette.GetColor(environmentData, GetColor(data));
		matrix.Translation += new Vector3(0f, 0.25f, 0f);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		return Terrain.ReplaceData(value, SetColor(data, color));
	}

	public static int? GetColor(int data)
	{
		if ((data & 1) != 0)
		{
			return (data >> 5) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		data = ((!color.HasValue) ? (data & -481) : ((data & -481) | (1 | (color.Value << 5))));
		return data;
	}

	public static int GetVariant(int data)
	{
		return (data >> 9) & 7;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain terrain, ComponentMiner miner, int value, TerrainRaycastResult raycastResult)
	{
		int contents = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		int num = raycastResult.CellFace.Face;
		if (num < 0 || num > 5)
		{
			num = 4;
		}
		data = SetVariant(data, num);
		int value2 = Terrain.MakeBlockValue(contents, 0, data);
		return new BlockPlacementData
		{
			Value = value2,
			CellFace = raycastResult.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int variant = GetVariant(Terrain.ExtractData(value));
		if (m_collisionBoxes != null && variant >= 0 && variant < m_collisionBoxes.Length && m_collisionBoxes[variant] != null)
		{
			return m_collisionBoxes[variant];
		}
		return null;
	}

	public static int SetVariant(int data, int variant)
	{
		return (data & -3585) | ((variant & 7) << 9);
	}
}
