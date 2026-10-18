using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class AttachedSignBlock : SignBlock, IElectricElementBlock, IPaintableBlock, IWaterloggable
{
	public string m_modelName;

	public int m_coloredTextureSlot;

	public int m_postedSignBlockIndex;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh m_standaloneColoredBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshes = new BlockMesh[4];

	public BlockMesh[] m_coloredBlockMeshes = new BlockMesh[4];

	public BlockMesh[] m_surfaceMeshes = new BlockMesh[4];

	public Vector3[] m_surfaceNormals = new Vector3[4];

	public BoundingBox[][] m_collisionBoxes = new BoundingBox[4][];

	public AttachedSignBlock(string modelName, int coloredTextureSlot, int postedSignBlockIndex)
	{
		m_modelName = modelName;
		m_coloredTextureSlot = coloredTextureSlot;
		m_postedSignBlockIndex = postedSignBlockIndex;
	}

	public override void Initialize()
	{
		Model result = ContentManager.Get<Model>(m_modelName);
		Matrix count = BlockMesh.GetBoneAbsoluteTransform(result.FindMesh("Sign").ParentBone);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(result.FindMesh("Surface").ParentBone);
		for (int i = 0; i < 4; i++)
		{
			float radians = (float)Math.PI / 2f * (float)i;
			Matrix matrix = Matrix.CreateTranslation(0f, 0f, -15f / 32f) * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, -0.3125f, 0.5f);
			BlockMesh blockMesh = new BlockMesh();
			blockMesh.AppendModelMeshPart(result.FindMesh("Sign").MeshParts[0], count * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMeshes[i] = new BlockMesh();
			m_blockMeshes[i].AppendBlockMesh(blockMesh);
			m_coloredBlockMeshes[i] = new BlockMesh();
			m_coloredBlockMeshes[i].AppendBlockMesh(m_blockMeshes[i]);
			m_blockMeshes[i].TransformTextureCoordinates(Matrix.CreateTranslation((float)(DefaultTextureSlot % 16) / 16f, (float)(DefaultTextureSlot / 16) / 16f, 0f));
			m_coloredBlockMeshes[i].TransformTextureCoordinates(Matrix.CreateTranslation((float)(m_coloredTextureSlot % 16) / 16f, (float)(m_coloredTextureSlot / 16) / 16f, 0f));
			m_collisionBoxes[i] = new BoundingBox[1];
			m_collisionBoxes[i][0] = blockMesh.CalculateBoundingBox();
			m_surfaceMeshes[i] = new BlockMesh();
			m_surfaceMeshes[i].AppendModelMeshPart(result.FindMesh("Surface").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_surfaceNormals[i] = -matrix.Forward;
		}
		m_standaloneBlockMesh.AppendModelMeshPart(result.FindMesh("Sign").MeshParts[0], count * Matrix.CreateTranslation(0f, -0.6f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneColoredBlockMesh.AppendBlockMesh(m_standaloneBlockMesh);
		m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation((float)(DefaultTextureSlot % 16) / 16f, (float)(DefaultTextureSlot / 16) / 16f, 0f));
		m_standaloneColoredBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation((float)(m_coloredTextureSlot % 16) / 16f, (float)(m_coloredTextureSlot / 16) / 16f, 0f));
		base.Initialize();
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		int? pos = GetColor(Terrain.ExtractData(oldValue));
		int i = PostedSignBlock.SetColor(0, pos);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(m_postedSignBlockIndex, 0, i),
			Count = 1
		});
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		int? num = GetColor(Terrain.ExtractData(value));
		if (num.HasValue)
		{
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, SubsystemPalette.GetColor(subsystemTerrain, num), m_coloredTextureSlot);
		}
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, DefaultTextureSlot);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsWaterLogged(num) && BlocksManager.Blocks[18] is FocalorsFluidBlock node2)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			node2.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int num2 = GetFace(num);
		int? node3 = GetColor(num);
		if (node3.HasValue)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_coloredBlockMeshes[num2], SubsystemPalette.GetColor(generator, node3), null, geometry.SubsetOpaque);
		}
		else
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshes[num2], Color.White, null, geometry.SubsetOpaque);
		}
		generator.GenerateWireVertices(value, x, y, z, GetFace(num), 0.375f, Vector2.Zero, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int? color2 = GetColor(Terrain.ExtractData(value));
		if (color2.HasValue)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneColoredBlockMesh, color * SubsystemPalette.GetColor(environmentData, color2), 1.25f * size, ref matrix, environmentData);
		}
		else
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 1.25f * size, ref matrix, environmentData);
		}
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int result = Terrain.ExtractData(value);
		return Terrain.ReplaceData(value, SetColor(result, color));
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int face = GetFace(Terrain.ExtractData(value));
		return m_collisionBoxes[face];
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		return default;
	}

	public override BlockMesh GetSignSurfaceBlockMesh(int data)
	{
		return m_surfaceMeshes[GetFace(data)];
	}

	public override Vector3 GetSignSurfaceNormal(int data)
	{
		return m_surfaceNormals[GetFace(data)];
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		return new SignElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(data)));
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (face != GetFace(data) || !SubsystemElectricity.GetConnectorDirection(face, 0, connectorFace).HasValue)
		{
			return null;
		}
		return ElectricConnectorType.Input;
	}

	public int GetConnectionMask(int value)
	{
		return int.MaxValue;
	}

	public static int GetFace(int data)
	{
		return data & 3;
	}

	public static int SetFace(int data, int face)
	{
		return (data & -4) | (face & 3);
	}

	public static int? GetColor(int data)
	{
		if ((data & 4) != 0)
		{
			return (data >> 3) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -125) | 4 | ((color.Value & 0xF) << 3);
		}
		return data & -125;
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x400) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (isWaterLogged)
		{
			return data | 0x400;
		}
		return data & -1025;
	}
}
