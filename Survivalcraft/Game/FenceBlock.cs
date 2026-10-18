using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class FenceBlock : Block, IPaintableBlock, IWaterloggable
{
	public string m_modelName;

	public bool m_doubleSidedPlanks;

	public bool m_useAlphaTest;

	public int m_coloredTextureSlot;

	public Color m_postColor;

	public Color m_unpaintedColor;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh m_standaloneColoredBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshes = new BlockMesh[16];

	public BlockMesh[] m_coloredBlockMeshes = new BlockMesh[16];

	public BoundingBox[][] m_collisionBoxes = new BoundingBox[16][];

	public const int WaterLoggedBit = 1024;

	public FenceBlock(string modelName, bool doubleSidedPlanks, bool useAlphaTest, int coloredTextureSlot, Color postColor, Color unpaintedColor)
	{
		m_modelName = modelName;
		m_doubleSidedPlanks = doubleSidedPlanks;
		m_useAlphaTest = useAlphaTest;
		m_coloredTextureSlot = coloredTextureSlot;
		m_postColor = postColor;
		m_unpaintedColor = unpaintedColor;
	}

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>(m_modelName);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Post").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Planks").ParentBone);
		for (int i = 0; i < 16; i++)
		{
			bool flag = (i & 1) != 0;
			bool flag2 = (i & 2) != 0;
			bool flag3 = (i & 4) != 0;
			bool flag4 = (i & 8) != 0;
			List<BoundingBox> list = new List<BoundingBox>();
			Matrix matrix = Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			BlockMesh blockMesh = new BlockMesh();
			blockMesh.AppendModelMeshPart(model.FindMesh("Post").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			BoundingBox item = blockMesh.CalculateBoundingBox();
			item.Min.X = item.Min.X - 0.2f;
			item.Min.Z = item.Min.Z - 0.2f;
			item.Max.X = item.Max.X + 0.2f;
			item.Max.Z = item.Max.Z + 0.2f;
			list.Add(item);
			BlockMesh blockMesh2 = new BlockMesh();
			if (flag)
			{
				BlockMesh blockMesh3 = new BlockMesh();
				Matrix matrix2 = Matrix.CreateRotationY(0f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				blockMesh3.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				if (m_doubleSidedPlanks)
				{
					blockMesh3.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix2, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, Color.White);
				}
				blockMesh2.AppendBlockMesh(blockMesh3);
				BoundingBox item2 = blockMesh3.CalculateBoundingBox();
				list.Add(item2);
			}
			if (flag2)
			{
				BlockMesh blockMesh4 = new BlockMesh();
				Matrix matrix3 = Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				blockMesh4.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				if (m_doubleSidedPlanks)
				{
					blockMesh4.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix3, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, Color.White);
				}
				blockMesh2.AppendBlockMesh(blockMesh4);
				BoundingBox item3 = blockMesh4.CalculateBoundingBox();
				list.Add(item3);
			}
			if (flag3)
			{
				BlockMesh blockMesh5 = new BlockMesh();
				Matrix matrix4 = Matrix.CreateRotationY(4.712389f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				blockMesh5.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix4, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				if (m_doubleSidedPlanks)
				{
					blockMesh5.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix4, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, Color.White);
				}
				blockMesh2.AppendBlockMesh(blockMesh5);
				BoundingBox item4 = blockMesh5.CalculateBoundingBox();
				list.Add(item4);
			}
			if (flag4)
			{
				BlockMesh blockMesh6 = new BlockMesh();
				Matrix matrix5 = Matrix.CreateRotationY((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				blockMesh6.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix5, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				if (m_doubleSidedPlanks)
				{
					blockMesh6.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * matrix5, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, Color.White);
				}
				blockMesh2.AppendBlockMesh(blockMesh6);
				BoundingBox item5 = blockMesh6.CalculateBoundingBox();
				list.Add(item5);
			}
			blockMesh.ModulateColor(m_postColor);
			m_blockMeshes[i] = new BlockMesh();
			m_blockMeshes[i].AppendBlockMesh(blockMesh);
			m_blockMeshes[i].AppendBlockMesh(blockMesh2);
			m_blockMeshes[i].TransformTextureCoordinates(Matrix.CreateTranslation((float)(DefaultTextureSlot % 16) / 16f, (float)(DefaultTextureSlot / 16) / 16f, 0f));
			m_blockMeshes[i].GenerateSidesData();
			m_coloredBlockMeshes[i] = new BlockMesh();
			m_coloredBlockMeshes[i].AppendBlockMesh(blockMesh);
			m_coloredBlockMeshes[i].AppendBlockMesh(blockMesh2);
			m_coloredBlockMeshes[i].TransformTextureCoordinates(Matrix.CreateTranslation((float)(m_coloredTextureSlot % 16) / 16f, (float)(m_coloredTextureSlot / 16) / 16f, 0f));
			m_coloredBlockMeshes[i].GenerateSidesData();
			m_collisionBoxes[i] = list.ToArray();
		}
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Post").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(-0.5f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Post").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(0f) * Matrix.CreateTranslation(-0.5f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		if (m_doubleSidedPlanks)
		{
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(0f) * Matrix.CreateTranslation(-0.5f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, Color.White);
		}
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		if (m_doubleSidedPlanks)
		{
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Planks").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY((float)Math.PI) * Matrix.CreateTranslation(0.5f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, Color.White);
		}
		m_standaloneColoredBlockMesh.AppendBlockMesh(m_standaloneBlockMesh);
		m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation((float)(DefaultTextureSlot % 16) / 16f, (float)(DefaultTextureSlot / 16) / 16f, 0f));
		m_standaloneColoredBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation((float)(m_coloredTextureSlot % 16) / 16f, (float)(m_coloredTextureSlot / 16) / 16f, 0f));
		base.Initialize();
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int? i = GetColor(Terrain.ExtractData(value));
		return SubsystemPalette.GetName(subsystemTerrain, i, base.GetDisplayName(subsystemTerrain, value));
	}

	public override string GetCategory(int value)
	{
		if (!GetColor(Terrain.ExtractData(value)).HasValue)
		{
			return base.GetCategory(value);
		}
		return LanguageControl.Get("BlocksManager", "Painted");
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
		int i = 0;
		while (i < 16)
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
			int num = i + 1;
			i = num;
		}
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		int list = SetVariant(Terrain.ExtractData(oldValue), 0);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, list),
			Count = 1
		});
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		int? color = GetColor(Terrain.ExtractData(value));
		if (color.HasValue)
		{
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, SubsystemPalette.GetColor(subsystemTerrain, color), m_coloredTextureSlot);
		}
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, DefaultTextureSlot);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int texturedBatch3D = Terrain.ExtractData(value);
		if (IsWaterLogged(texturedBatch3D) && BlocksManager.Blocks[18] is FocalorsWaterBlock vector2)
		{
			Color vector3;
			Color color = (vector3 = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			vector3.A = byte.MaxValue;
			Color v = color;
			v.A = 0;
			vector2.GenerateWaterloggedFluidVertices(generator, x, y, z, vector3, v, geometry.TransparentSubsetsByFace);
		}
		int s = GetVariant(texturedBatch3D);
		int? vector4 = GetColor(texturedBatch3D);
		if (vector4.HasValue)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_coloredBlockMeshes[s], SubsystemPalette.GetColor(generator, vector4), null, m_useAlphaTest ? geometry.SubsetAlphaTest : geometry.SubsetOpaque);
		}
		else
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshes[s], m_unpaintedColor, null, m_useAlphaTest ? geometry.SubsetAlphaTest : geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int? color2 = GetColor(Terrain.ExtractData(value));
		if (color2.HasValue)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneColoredBlockMesh, color * SubsystemPalette.GetColor(environmentData, color2), size, ref matrix, environmentData);
		}
		else
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color * m_unpaintedColor, size, ref matrix, environmentData);
		}
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int num = Terrain.ExtractData(value);
		bool block = IsWaterLogged(num);
		int imageExtrusionBlockMesh = SetColor(num, color);
		imageExtrusionBlockMesh = SetWaterLogged(imageExtrusionBlockMesh, block);
		return Terrain.ReplaceData(value, imageExtrusionBlockMesh);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int key = GetVariant(Terrain.ExtractData(value));
		return m_collisionBoxes[key];
	}

	public virtual bool ShouldConnectTo(int value)
	{
		int texture = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[texture];
		if (!(block is FenceBlock))
		{
			return block is FenceGateBlock;
		}
		return true;
	}

	public static int GetVariant(int data)
	{
		return data & 0xF;
	}

	public static int SetVariant(int data, int variant)
	{
		return (data & -16) | (variant & 0xF);
	}

	public static int? GetColor(int data)
	{
		if ((data & 0x10) != 0)
		{
			return (data >> 5) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -497) | 0x10 | ((color.Value & 0xF) << 5);
		}
		return data & -497;
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x400) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (!isWaterLogged)
		{
			return data & -1025;
		}
		return data | 0x400;
	}
}
