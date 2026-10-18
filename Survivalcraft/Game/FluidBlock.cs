using System;
using System.Reflection;
using Engine;

namespace Game;

public abstract class FluidBlock : CubeBlock
{
	public float[] m_heightByLevel = new float[16];

	public BoundingBox[][] m_boundingBoxesByLevel = new BoundingBox[16][];

	public bool[] m_theSameFluidsByIndex;

	public int MaxLevel;

	public const int ArtificialPlacementFlag = 32;

	public FluidBlock(int maxLevel)
	{
		MaxLevel = maxLevel;
		for (int i = 0; i < 16; i++)
		{
			float num = 0.875f * MathUtils.Saturate(1f - (float)i / (float)MaxLevel);
			m_heightByLevel[i] = num;
			m_boundingBoxesByLevel[i] = new BoundingBox[1]
			{
				new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, num, 1f))
			};
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		TypeInfo typeInfo = null;
		TypeInfo typeInfo2 = GetType().GetTypeInfo();
		while (typeInfo2 != null)
		{
			if (typeInfo2.BaseType == typeof(FluidBlock))
			{
				typeInfo = typeInfo2;
				break;
			}
			typeInfo2 = typeInfo2.BaseType.GetTypeInfo();
		}
		if (typeInfo == null)
		{
			throw new InvalidOperationException("Fluid type not found.");
		}
		m_theSameFluidsByIndex = new bool[BlocksManager.Blocks.Length];
		for (int i = 0; i < BlocksManager.Blocks.Length; i++)
		{
			Block block = BlocksManager.Blocks[i];
			m_theSameFluidsByIndex[i] = block.GetType().GetTypeInfo() == typeInfo || block.GetType().GetTypeInfo().IsSubclassOf(typeInfo.AsType());
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_boundingBoxesByLevel[GetLevel(Terrain.ExtractData(value))];
	}

	public bool IsTheSameFluid(int contents)
	{
		return m_theSameFluidsByIndex[contents];
	}

	public float GetLevelHeight(int level)
	{
		return m_heightByLevel[level];
	}

	public void GenerateFluidTerrainVertices(BlockGeometryGenerator generator, int value, int x, int y, int z, Color sideColor, Color topColor, TerrainGeometrySubset[] subset)
	{
		int data = Terrain.ExtractData(value);
		if (GetIsTop(data))
		{
			Terrain terrain = generator.Terrain;
			int cellValueFast = terrain.GetCellValueFast(x - 1, y, z - 1);
			int cellValueFast2 = terrain.GetCellValueFast(x, y, z - 1);
			int cellValueFast3 = terrain.GetCellValueFast(x + 1, y, z - 1);
			int cellValueFast4 = terrain.GetCellValueFast(x - 1, y, z);
			int cellValueFast5 = terrain.GetCellValueFast(x + 1, y, z);
			int cellValueFast6 = terrain.GetCellValueFast(x - 1, y, z + 1);
			int cellValueFast7 = terrain.GetCellValueFast(x, y, z + 1);
			int cellValueFast8 = terrain.GetCellValueFast(x + 1, y, z + 1);
			float h = CalculateNeighborHeight(cellValueFast);
			float num = CalculateNeighborHeight(cellValueFast2);
			float h2 = CalculateNeighborHeight(cellValueFast3);
			float num2 = CalculateNeighborHeight(cellValueFast4);
			float num3 = CalculateNeighborHeight(cellValueFast5);
			float h3 = CalculateNeighborHeight(cellValueFast6);
			float num4 = CalculateNeighborHeight(cellValueFast7);
			float h4 = CalculateNeighborHeight(cellValueFast8);
			float levelHeight = GetLevelHeight(GetLevel(data));
			float height = CalculateFluidVertexHeight(h, num, levelHeight, num2);
			float height2 = CalculateFluidVertexHeight(num, h2, levelHeight, num3);
			float height3 = CalculateFluidVertexHeight(levelHeight, num3, num4, h4);
			float height4 = CalculateFluidVertexHeight(num2, levelHeight, h3, num4);
			float x2 = ZeroSubst(num3, levelHeight) - ZeroSubst(num2, levelHeight);
			float x3 = ZeroSubst(num4, levelHeight) - ZeroSubst(num, levelHeight);
			int overrideTopTextureSlot = DefaultTextureSlot - (int)MathUtils.Sign(x2) - 16 * (int)MathUtils.Sign(x3);
			generator.GenerateCubeVertices(this, value, x, y, z, height, height2, height3, height4, sideColor, topColor, topColor, topColor, topColor, overrideTopTextureSlot, subset);
			new Vector3(x, y, z);
			float num5 = 20f;
			new Vector3(1f, 0f, 1f);
			if (generator.SubsystemTerrain.TerrainContentsGenerator is TerrainContentsGenerator23 terrainContentsGenerator)
			{
				terrainContentsGenerator.CalculateOceanShoreDistance((float)x + 0.5f, (float)z + 0.5f);
			}
			int cellContentsFast = terrain.GetCellContentsFast(x, y + 1, z);
			IsTheSameFluid(cellContentsFast);
			TerrainChunk chunkAtCell = terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null)
			{
				int x4 = x - chunkAtCell.Origin.X;
				int z2 = z - chunkAtCell.Origin.Y;
				chunkAtCell.GetTopHeightFast(x4, z2);
			}
		}
		else
		{
			generator.GenerateCubeVertices(this, value, x, y, z, sideColor, subset);
		}
	}

	public static float ZeroSubst(float v, float subst)
	{
		if (v != 0f)
		{
			return v;
		}
		return subst;
	}

	public static float CalculateFluidVertexHeight(float h1, float h2, float h3, float h4)
	{
		float data = MathUtils.Max(h1, h2, h3, h4);
		if (data >= 1f)
		{
			return 1f;
		}
		if (h1 == 0.01f || h2 == 0.01f || h3 == 0.01f || h4 == 0.01f)
		{
			return 0f;
		}
		return data;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, BlockIndex), 0),
			CellFace = raycastResult.CellFace
		};
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (face >= 4)
		{
			return DefaultTextureSlot;
		}
		return DefaultTextureSlot + 16;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		int contents = Terrain.ExtractContents(neighborValue);
		if (!IsTheSameFluid(contents))
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}

	public float CalculateNeighborHeight(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (IsTheSameFluid(num))
		{
			int data = Terrain.ExtractData(value);
			if (GetIsTop(data))
			{
				return GetLevelHeight(GetLevel(data));
			}
			return 1f;
		}
		if (num == 0)
		{
			return 0.01f;
		}
		return 0f;
	}

	public override bool IsHeatBlocker(int value)
	{
		return true;
	}

	public static int GetLevel(int data)
	{
		return data & 0xF;
	}

	public static int SetLevel(int data, int level)
	{
		return (data & -16) | (level & 0xF);
	}

	public static bool GetIsTop(int data)
	{
		return (data & 0x10) != 0;
	}

	public static int SetIsTop(int data, bool isTop)
	{
		if (!isTop)
		{
			return data & -17;
		}
		return data | 0x10;
	}

	private void TryGenerateExtension(BlockGeometryGenerator generator, Terrain terrain, int x, int y, int z, int dx, int dz, float height, Color topColor, TerrainGeometrySubset[] subsets, Vector3 origin, Vector3 size, float length)
	{
		int cellValueFast = terrain.GetCellValueFast(x + dx, y, z + dz);
		if (IsTheSameFluid(Terrain.ExtractContents(cellValueFast)))
		{
			return;
		}
		TerrainGeometrySubset obj = subsets[4];
		DynamicArray<TerrainVertex> vertices = obj.Vertices;
		DynamicArray<int> indices = obj.Indices;
		int defaultTextureSlot = DefaultTextureSlot;
		int cellValueFast2 = terrain.GetCellValueFast(x, y, z);
		int textureSlotCount = GetTextureSlotCount(cellValueFast2);
		int light = generator.CalculateVertexLightFace4(x, y + 1, z);
		for (int i = 1; i <= (int)length; i++)
		{
			int x2 = x + dx * i;
			int z2 = z + dz * i;
			if (!IsTheSameFluid(terrain.GetCellContentsFast(x2, y, z2)))
			{
				Vector3 vector = new Vector3(dx, 0f, dz) * i;
				Vector3 vector2 = origin + vector;
				Vector3 vector3 = vector2 + new Vector3(0f, height, 0f);
				Vector3 vector4 = vector2 + new Vector3(1f, height, 0f);
				Vector3 vector5 = vector2 + new Vector3(1f, height, 1f);
				Vector3 vector6 = vector2 + new Vector3(0f, height, 1f);
				int count = vertices.Count;
				vertices.Count += 4;
				BlockGeometryGenerator.SetupCornerVertex(vector3.X, vector3.Y, vector3.Z, topColor, light, 4, defaultTextureSlot, textureSlotCount, 3, ref vertices.Array[count]);
				BlockGeometryGenerator.SetupCornerVertex(vector4.X, vector4.Y, vector4.Z, topColor, light, 4, defaultTextureSlot, textureSlotCount, 2, ref vertices.Array[count + 1]);
				BlockGeometryGenerator.SetupCornerVertex(vector5.X, vector5.Y, vector5.Z, topColor, light, 4, defaultTextureSlot, textureSlotCount, 1, ref vertices.Array[count + 2]);
				BlockGeometryGenerator.SetupCornerVertex(vector6.X, vector6.Y, vector6.Z, topColor, light, 4, defaultTextureSlot, textureSlotCount, 0, ref vertices.Array[count + 3]);
				int count2 = indices.Count;
				indices.Count += 6;
				indices.Array[count2] = count;
				indices.Array[count2 + 1] = count + 1;
				indices.Array[count2 + 2] = count + 2;
				indices.Array[count2 + 3] = count + 2;
				indices.Array[count2 + 4] = count + 3;
				indices.Array[count2 + 5] = count;
				continue;
			}
			break;
		}
	}
}
