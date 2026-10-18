using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class TreeBarkBlock : Block
{
	public const int Index = 443;

	private BlockMesh[,,] m_blockMeshes;

	private BlockMesh[,] m_standaloneMeshes;

	private BoundingBox[] m_collisionByFace;

	private Vector2[] m_textureOffsets;

	private Random m_random = new Random();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/TreeBark");
		m_textureOffsets = new Vector2[5]
		{
			new Vector2(0.25f, 0.0625f),
			new Vector2(0.3125f, 0.4375f),
			new Vector2(0.25f, 0.4375f),
			new Vector2(0.25f, 0.4375f),
			new Vector2(0.25f, 0.4375f)
		};
		int num = 3;
		int num2 = 6;
		int num3 = m_textureOffsets.Length;
		m_blockMeshes = new BlockMesh[num, num2, num3];
		m_standaloneMeshes = new BlockMesh[num, num3];
		m_collisionByFace = new BoundingBox[num2];
		m_collisionByFace[4] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.1f, 1f));
		m_collisionByFace[5] = new BoundingBox(new Vector3(0f, 0.9f, 0f), new Vector3(1f, 1f, 1f));
		m_collisionByFace[0] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 0.1f));
		m_collisionByFace[2] = new BoundingBox(new Vector3(0f, 0f, 0.9f), new Vector3(1f, 1f, 1f));
		m_collisionByFace[1] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.1f, 1f, 1f));
		m_collisionByFace[3] = new BoundingBox(new Vector3(0.9f, 0f, 0f), new Vector3(1f, 1f, 1f));
		for (int i = 0; i < num; i++)
		{
			string name = "Bark" + i;
			ModelMesh modelMesh = model.FindMesh(name);
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
			Matrix[] array = new Matrix[num2];
			array[4] = Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			array[5] = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 1f, 0.5f);
			array[0] = Matrix.Identity * Matrix.CreateTranslation(0.5f, 0.5f, 0f);
			array[2] = Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, 0.5f, 1f);
			array[1] = Matrix.CreateRotationY((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.5f, 0.5f);
			array[3] = Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateTranslation(1f, 0.5f, 0.5f);
			for (int j = 0; j < num3; j++)
			{
				BlockMesh blockMesh = new BlockMesh();
				blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.25f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(m_textureOffsets[j].X, m_textureOffsets[j].Y, 0f));
				m_standaloneMeshes[i, j] = blockMesh;
				for (int k = 0; k < num2; k++)
				{
					BlockMesh blockMesh2 = new BlockMesh();
					blockMesh2.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * array[k], makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					blockMesh2.TransformTextureCoordinates(Matrix.CreateTranslation(m_textureOffsets[j].X, m_textureOffsets[j].Y, 0f));
					m_blockMeshes[i, k, j] = blockMesh2;
				}
			}
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		int num = GetFace(data);
		int variant = GetVariant(data);
		int num2 = GetWoodType(data);
		if (num2 >= m_textureOffsets.Length)
		{
			num2 = 0;
		}
		if (num < 0 || num > 5)
		{
			num = 4;
		}
		uint num3 = MathUtils.Hash((uint)(x + y * 451 + z * 77437));
		float num4 = 0.8f + (float)(num3 % 40) / 100f;
		float x2 = ((float)((num3 >> 6) % 10) - 5f) / 100f;
		float y2 = ((float)((num3 >> 10) % 10) - 5f) / 100f;
		float radians = (((num3 >> 14) % 2 == 0) ? ((float)Math.PI) : 0f);
		Matrix matrix = Matrix.CreateScale(num4, num4, 1f) * Matrix.CreateRotationZ(radians) * Matrix.CreateTranslation(x2, y2, 0f);
		BlockMesh blockMesh = m_blockMeshes[variant, 0, num2];
		BlockMesh blockMesh2 = new BlockMesh();
		blockMesh2.AppendBlockMesh(blockMesh);
		blockMesh2.TransformPositions(Matrix.CreateTranslation(-0.5f, -0.5f, 0f));
		blockMesh2.TransformPositions(matrix);
		blockMesh2.TransformPositions(Matrix.CreateTranslation(0.5f, 0.5f, 0f));
		Matrix matrix2 = Matrix.Identity;
		switch (num)
		{
		case 4:
			matrix2 = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			break;
		case 5:
			matrix2 = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 1f, 0.5f);
			break;
		case 2:
			matrix2 = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, 0.5f, 1f);
			break;
		case 1:
			matrix2 = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationY((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.5f, 0.5f);
			break;
		case 3:
			matrix2 = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateTranslation(1f, 0.5f, 0.5f);
			break;
		}
		if (num != 0)
		{
			blockMesh2.TransformPositions(matrix2);
		}
		Point3 point = CellFace.FaceToPoint3(num);
		int x3 = x + point.X;
		int y3 = y + point.Y;
		int z2 = z + point.Z;
		int num5 = Terrain.ExtractLight(generator.Terrain.GetCellValueFast(x3, y3, z2));
		float num6 = LightingManager.LightIntensityByLightValueAndFace[num5 + 64];
		Color white = Color.White;
		Color color = new Color((byte)((float)(int)white.R * num6), (byte)((float)(int)white.G * num6), (byte)((float)(int)white.B * num6));
		DynamicArray<TerrainVertex> vertices = geometry.SubsetOpaque.Vertices;
		DynamicArray<int> indices = geometry.SubsetOpaque.Indices;
		int count = vertices.Count;
		vertices.Count += blockMesh2.Vertices.Count;
		for (int i = 0; i < blockMesh2.Vertices.Count; i++)
		{
			BlockMeshVertex blockMeshVertex = blockMesh2.Vertices.Array[i];
			BlockGeometryGenerator.SetupVertex(color: new Color((byte)((float)(int)color.R * ((float)(int)blockMeshVertex.Color.R / 255f)), (byte)((float)(int)color.G * ((float)(int)blockMeshVertex.Color.G / 255f)), (byte)((float)(int)color.B * ((float)(int)blockMeshVertex.Color.B / 255f))), x: (float)x + blockMeshVertex.Position.X, y: (float)y + blockMeshVertex.Position.Y, z: (float)z + blockMeshVertex.Position.Z, tx: blockMeshVertex.TextureCoordinates.X, ty: blockMeshVertex.TextureCoordinates.Y, vertex: ref vertices.Array[count + i]);
		}
		for (int j = 0; j < blockMesh2.Indices.Count; j++)
		{
			indices.Add(count + blockMesh2.Indices.Array[j]);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int data = Terrain.ExtractData(value);
		int variant = GetVariant(data);
		int num = GetWoodType(data);
		if (num >= m_textureOffsets.Length)
		{
			num = 0;
		}
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMeshes[variant, num], color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain terrain, ComponentMiner miner, int value, TerrainRaycastResult r)
	{
		int data = Terrain.ExtractData(value);
		int face = r.CellFace.Face;
		int variant = m_random.Int(0, 2);
		int woodType = GetWoodType(data);
		int data2 = SetData(face, variant, woodType);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data2),
			CellFace = r.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = GetFace(Terrain.ExtractData(value));
		if (num < 0 || num > 5)
		{
			num = 4;
		}
		return new BoundingBox[1] { m_collisionByFace[num] };
	}

	public static int GetFace(int data)
	{
		return data & 7;
	}

	public static int GetVariant(int data)
	{
		return (data >> 3) & 3;
	}

	public static int GetWoodType(int data)
	{
		return (data >> 5) & 0x1F;
	}

	public static int SetData(int face, int variant, int woodType)
	{
		return 0 | (MathUtils.Clamp(face, 0, 5) & 7) | ((MathUtils.Clamp(variant, 0, 2) & 3) << 3) | ((MathUtils.Clamp(woodType, 0, 31) & 0x1F) << 5);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetData(4, 0, 0));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetData(4, 0, 1));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetData(4, 0, 2));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int woodType = GetWoodType(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"TreeBarkBlock:{woodType}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int woodType = GetWoodType(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"TreeBarkBlock:{woodType}", "Description");
	}
}
