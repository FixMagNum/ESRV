using System;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class FocalorsFluidBlock : CubeBlock
{
	public readonly BoundingBox[][] m_boundingBoxesByLevel = new BoundingBox[16][];

	private static bool[] m_isFluidBlockCache;

	private static bool[] m_isWaterloggableCache;

	private static bool m_cachesInitialized;

	public FocalorsFluidBlock()
	{
		for (int attacker = 0; attacker < 16; attacker++)
		{
			float y = 0.995f * MathUtils.Saturate(1f - (float)attacker / 16f);
			m_boundingBoxesByLevel[attacker] = new BoundingBox[1]
			{
				new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, y, 1f))
			};
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_boundingBoxesByLevel[(int)(GetLevel(Terrain.ExtractData(value)) * 15f)];
	}

	public static float GetLevelHeight(float level)
	{
		return 0.995f - level;
	}

	public void GenerateFluidTerrainVertices(BlockGeometryGenerator generator, int value, int x, int y, int z, Color sideColor, Color topColor, TerrainGeometrySubset[] subset)
	{
		int data = Terrain.ExtractData(value);
		Terrain terrain = generator.Terrain;
		int value2 = Terrain.ExtractContents(terrain.GetCellValueFast(x, y + 1, z));
		bool flag = IsCompatibleFluid(value2);
		if (GetIsTop(data))
		{
			float levelHeight = GetLevelHeight(GetLevel(data));
			float h = CalculateNeighborHeight(terrain.GetCellValueFast(x - 1, y, z - 1), levelHeight);
			float num = CalculateNeighborHeight(terrain.GetCellValueFast(x, y, z - 1), levelHeight);
			float h2 = CalculateNeighborHeight(terrain.GetCellValueFast(x + 1, y, z - 1), levelHeight);
			float num2 = CalculateNeighborHeight(terrain.GetCellValueFast(x - 1, y, z), levelHeight);
			float num3 = CalculateNeighborHeight(terrain.GetCellValueFast(x + 1, y, z), levelHeight);
			float h3 = CalculateNeighborHeight(terrain.GetCellValueFast(x - 1, y, z + 1), levelHeight);
			float num4 = CalculateNeighborHeight(terrain.GetCellValueFast(x, y, z + 1), levelHeight);
			float h4 = CalculateNeighborHeight(terrain.GetCellValueFast(x + 1, y, z + 1), levelHeight);
			float height = CalculateFluidVertexHeight(h, num, num2, levelHeight);
			float height2 = CalculateFluidVertexHeight(num, h2, levelHeight, num3);
			float height3 = CalculateFluidVertexHeight(levelHeight, num3, num4, h4);
			float height4 = CalculateFluidVertexHeight(num2, levelHeight, h3, num4);
			float value3 = ZeroSubst(num3, levelHeight) - ZeroSubst(num2, levelHeight);
			float value4 = ZeroSubst(num4, levelHeight) - ZeroSubst(num, levelHeight);
			int overrideTopTextureSlot = DefaultTextureSlot - Math.Sign(value3) - 16 * Math.Sign(value4);
			generator.GenerateCubeVertices(this, value, x, y, z, height, height2, height3, height4, sideColor, topColor, topColor, topColor, topColor, overrideTopTextureSlot, subset);
		}
		else
		{
			float num5 = (flag ? 1f : 0.995f);
			generator.GenerateCubeVertices(this, value, x, y, z, num5, num5, num5, num5, sideColor, topColor, topColor, topColor, topColor, DefaultTextureSlot, subset);
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
		return Math.Min(MathUtils.Max(h1, h2, h3, h4), 0.995f);
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (face < 4)
		{
			return DefaultTextureSlot + 16;
		}
		return DefaultTextureSlot;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		Terrain.ExtractContents(neighborValue);
		if (!IsCompatibleFluid(neighborValue))
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}

	public void DrawFocalorsFluidBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size, size * (1f - GetLevel(Terrain.ExtractData(value))), size), ref matrix, color, color, environmentData);
	}

	public override bool IsHeatBlocker(int value)
	{
		return true;
	}

	public static float GetLevel(int data)
	{
		return (float)((data >> 2) & 0xFFFF) / 65535f;
	}

	public static int SetLevel(int data, float level)
	{
		return (data & -4) | ((int)(Math.Min(level, 1f) * 65535f) << 2);
	}

	public static bool GetIsTop(int data)
	{
		return (data & 1) != 0;
	}

	public static int SetIsTop(int data, bool isTop)
	{
		if (!isTop)
		{
			return data & -2;
		}
		return data | 1;
	}

	public bool IsCompatibleFluid(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return false;
		}
		if (num == BlockIndex)
		{
			return true;
		}
		EnsureCachesInitialized();
		if (m_isFluidBlockCache[num])
		{
			return true;
		}
		if (m_isWaterloggableCache[num])
		{
			return ((IWaterloggable)BlocksManager.Blocks[num]).IsWaterLogged(Terrain.ExtractData(value));
		}
		return false;
	}

	public float CalculateNeighborHeight(int value, float? requesterHeight = null)
	{
		int num = Terrain.ExtractContents(value);
		if (!IsCompatibleFluid(num))
		{
			return 0f;
		}
		if (num != BlockIndex)
		{
			return 0.995f;
		}
		int data = Terrain.ExtractData(value);
		if (!GetIsTop(data))
		{
			return 1f;
		}
		return GetLevelHeight(GetLevel(data));
	}

	public void GenerateWaterloggedFluidVertices(BlockGeometryGenerator generator, int x, int y, int z, Color sideColor, Color topColor, TerrainGeometrySubset[] subset)
	{
		Terrain terrain = generator.Terrain;
		int num = Terrain.ExtractContents(terrain.GetCellValueFast(x, y + 1, z));
		if (IsCompatibleFluid(num))
		{
			generator.GenerateCubeVertices(this, 0, x, y, z, 1f, 1f, 1f, 1f, sideColor, topColor, topColor, topColor, topColor, DefaultTextureSlot, subset);
			return;
		}
		if (num != 0)
		{
			bool flag = false;
			int[] array = new int[8] { -1, 0, 1, 0, 0, -1, 0, 1 };
			for (int i = 0; i < 4; i++)
			{
				int cellValueFast = terrain.GetCellValueFast(x + array[i * 2], y, z + array[i * 2 + 1]);
				if (Terrain.ExtractContents(cellValueFast) == BlockIndex && !GetIsTop(Terrain.ExtractData(cellValueFast)))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				generator.GenerateCubeVertices(this, 0, x, y, z, sideColor, subset);
				return;
			}
		}
		float num2 = 0f;
		bool flag2 = false;
		bool flag3 = false;
		int[] array2 = new int[8] { -1, 0, 1, 0, 0, -1, 0, 1 };
		for (int j = 0; j < 4; j++)
		{
			int x2 = x + array2[j * 2];
			int z2 = z + array2[j * 2 + 1];
			int cellValueFast2 = terrain.GetCellValueFast(x2, y, z2);
			int num3 = Terrain.ExtractContents(cellValueFast2);
			if (num3 == BlockIndex)
			{
				float levelHeight = GetLevelHeight(GetLevel(Terrain.ExtractData(cellValueFast2)));
				if (levelHeight > num2)
				{
					num2 = levelHeight;
				}
				flag2 = true;
				continue;
			}
			IWaterloggable waterloggable = BlocksManager.Blocks[num3] as IWaterloggable;
			bool flag4 = num3 == 18;
			bool flag5 = waterloggable?.IsWaterLogged(Terrain.ExtractData(cellValueFast2)) ?? false;
			if (flag4 | flag5)
			{
				flag3 = true;
				if (flag4)
				{
					num2 = 0.995f;
					flag2 = true;
				}
			}
		}
		float num4 = 0f;
		if (flag3)
		{
			num4 = (flag2 ? num2 : 0.5f);
		}
		float h = CalculateNeighborHeight(terrain.GetCellValueFast(x - 1, y, z - 1));
		float num5 = CalculateNeighborHeight(terrain.GetCellValueFast(x, y, z - 1));
		float h2 = CalculateNeighborHeight(terrain.GetCellValueFast(x + 1, y, z - 1));
		float num6 = CalculateNeighborHeight(terrain.GetCellValueFast(x - 1, y, z));
		float num7 = CalculateNeighborHeight(terrain.GetCellValueFast(x + 1, y, z));
		float h3 = CalculateNeighborHeight(terrain.GetCellValueFast(x - 1, y, z + 1));
		float num8 = CalculateNeighborHeight(terrain.GetCellValueFast(x, y, z + 1));
		float h4 = CalculateNeighborHeight(terrain.GetCellValueFast(x + 1, y, z + 1));
		float num9 = CalculateFluidVertexHeight(h, num5, num6, 0f);
		float num10 = CalculateFluidVertexHeight(num5, h2, 0f, num7);
		float num11 = CalculateFluidVertexHeight(0f, num7, num8, h4);
		float num12 = CalculateFluidVertexHeight(num6, 0f, h3, num8);
		if (num9 == 0f && num4 > 0f)
		{
			num9 = num4;
		}
		if (num10 == 0f && num4 > 0f)
		{
			num10 = num4;
		}
		if (num11 == 0f && num4 > 0f)
		{
			num11 = num4;
		}
		if (num12 == 0f && num4 > 0f)
		{
			num12 = num4;
		}
		float value = ZeroSubst(num7, num4) - ZeroSubst(num6, num4);
		float value2 = ZeroSubst(num8, num4) - ZeroSubst(num5, num4);
		int overrideTopTextureSlot = DefaultTextureSlot - Math.Sign(value) - 16 * Math.Sign(value2);
		generator.GenerateCubeVertices(this, 0, x, y, z, num9, num10, num11, num12, sideColor, topColor, topColor, topColor, topColor, overrideTopTextureSlot, subset);
	}

	private static void EnsureCachesInitialized()
	{
		if (m_cachesInitialized)
		{
			return;
		}
		m_isFluidBlockCache = new bool[BlocksManager.Blocks.Length];
		m_isWaterloggableCache = new bool[BlocksManager.Blocks.Length];
		for (int i = 0; i < BlocksManager.Blocks.Length; i++)
		{
			Block block = BlocksManager.Blocks[i];
			if (block != null)
			{
				m_isFluidBlockCache[i] = block is FocalorsFluidBlock;
				m_isWaterloggableCache[i] = block is IWaterloggable;
			}
		}
		m_cachesInitialized = true;
	}
}
