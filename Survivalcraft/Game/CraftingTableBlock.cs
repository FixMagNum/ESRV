using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CraftingTableBlock : Block, IWaterloggable
{
	public enum CraftingTableMode
	{
		Normal,
		Small
	}

	public const int Index = 27;

	private BlockMesh[,] m_blockMeshesByData = new BlockMesh[2, 4];

	private BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[2];

	private BoundingBox[][] m_collisionBoxes = new BoundingBox[2][];

	public const int WaterLoggedBit = 1024;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/CraftingTable");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("CraftingTable").ParentBone);
		Model model2 = ContentManager.Get<Model>("Models/CraftingTableSmall");
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model2.FindMesh("CraftingTableSmall").ParentBone);
		for (int i = 0; i < 4; i++)
		{
			m_blockMeshesByData[0, i] = new BlockMesh();
			Matrix matrix = Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_blockMeshesByData[0, i].AppendModelMeshPart(model.FindMesh("CraftingTable").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMeshesByData[1, i] = new BlockMesh();
			Matrix matrix2 = Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_blockMeshesByData[1, i].AppendModelMeshPart(model2.FindMesh("CraftingTableSmall").MeshParts[0], boneAbsoluteTransform2 * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		m_standaloneBlockMeshes[0] = new BlockMesh();
		m_standaloneBlockMeshes[0].AppendModelMeshPart(model.FindMesh("CraftingTable").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMeshes[1] = new BlockMesh();
		m_standaloneBlockMeshes[1].AppendModelMeshPart(model2.FindMesh("CraftingTableSmall").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collisionBoxes[0] = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f))
		};
		m_collisionBoxes[1] = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f))
		};
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsWaterLogged(num) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int data = num & 0xF;
		int rotation = GetRotation(data);
		int mode = (int)GetMode(data);
		generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshesByData[mode, rotation], Color.White, null, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int mode = (int)GetMode(Terrain.ExtractData(value));
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[mode], color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 b = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		b.Y = 0f;
		if (b.LengthSquared() > 0f)
		{
			b = Vector3.Normalize(b);
		}
		int rotation = 0;
		float num = Vector3.Dot(b, -Vector3.UnitZ);
		float num2 = Vector3.Dot(b, Vector3.UnitX);
		if (num2 > num)
		{
			num = num2;
			rotation = 1;
		}
		num2 = Vector3.Dot(b, Vector3.UnitZ);
		if (num2 > num)
		{
			num = num2;
			rotation = 2;
		}
		num2 = Vector3.Dot(b, -Vector3.UnitX);
		if (num2 > num)
		{
			rotation = 3;
		}
		CraftingTableMode mode = GetMode(Terrain.ExtractData(value));
		int data = 0;
		data = SetRotation(data, rotation);
		data = SetMode(data, mode);
		int cellContents = subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		Block block = BlocksManager.Blocks[cellContents];
		bool isWaterLogged = block is WaterBlock || block is FocalorsFluidBlock;
		data = SetWaterLogged(data, isWaterLogged);
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(value, BlockIndex), data),
			CellFace = raycastResult.CellFace
		};
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetMode(SetRotation(0, 0), CraftingTableMode.Normal));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetMode(SetRotation(0, 0), CraftingTableMode.Small));
	}

	public static int SetMode(int data, CraftingTableMode mode)
	{
		return (data & -5) | ((int)(mode & CraftingTableMode.Small) << 2);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		CraftingTableMode mode = GetMode(Terrain.ExtractData(oldValue));
		int data = 0;
		data = SetRotation(data, 0);
		data = SetMode(data, mode);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			Count = 1
		});
		showDebris = true;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int mode = (int)GetMode(Terrain.ExtractData(value));
		return m_collisionBoxes[mode];
	}

	public static CraftingTableMode GetMode(int data)
	{
		return (CraftingTableMode)((data >> 2) & 1);
	}

	public static int GetRotation(int data)
	{
		return data & 3;
	}

	public static int SetRotation(int data, int rotation)
	{
		return (data & -4) | (rotation & 3);
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int mode = (int)GetMode(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"CraftingTableBlock:{mode}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int mode = (int)GetMode(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"CraftingTableBlock:{mode}", "Description");
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
