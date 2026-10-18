using System;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class LadderBlock : Block, IWaterloggable
{
	public string m_modelName;

	public float m_offset;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshesByData = new BlockMesh[4];

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[4][];

	public const int WaterLoggedBit = 1024;

	public LadderBlock(string modelName, float offset)
	{
		m_modelName = modelName;
		m_offset = offset;
	}

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>(m_modelName);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Ladder").ParentBone);
		for (int i = 0; i < 4; i++)
		{
			m_blockMeshesByData[i] = new BlockMesh();
			Matrix matrix = Matrix.CreateTranslation(0f, 0f, 0f - (0.5f - m_offset)) * Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_blockMeshesByData[i].AppendModelMeshPart(model.FindMesh("Ladder").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMeshesByData[i].GenerateSidesData();
			m_collisionBoxesByData[i] = new BoundingBox[1] { m_blockMeshesByData[i].CalculateBoundingBox() };
		}
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Ladder").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int face = GetFace(data);
		if (face < m_blockMeshesByData.Length)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByData[face], Color.White, null, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		if (raycastResult.CellFace.Face >= 4)
		{
			return default;
		}
		int data = SetFace(0, raycastResult.CellFace.Face);
		int cellContents = subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		Block block = BlocksManager.Blocks[cellContents];
		if (block is WaterBlock || block is FocalorsFluidBlock)
		{
			data = SetWaterLogged(data, isWaterLogged: true);
		}
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int face = GetFace(Terrain.ExtractData(value));
		if (face < m_collisionBoxesByData.Length)
		{
			return m_collisionBoxesByData[face];
		}
		return base.GetCustomCollisionBoxes(terrain, value);
	}

	public static int GetFace(int data)
	{
		return data & 3;
	}

	public static int SetFace(int data, int face)
	{
		return (data & -4) | (face & 3);
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
