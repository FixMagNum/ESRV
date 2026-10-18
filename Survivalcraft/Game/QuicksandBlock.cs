using System.Collections.Generic;
using Engine;

namespace Game;

public class QuicksandBlock : FluidBlock
{
	public enum QuicksandKind
	{
		Sand,
		Mud
	}

	public const int Index = 391;

	public static int QuicksandMaxLevel;

	public const int KindMask = 64;

	public QuicksandBlock()
		: base(QuicksandMaxLevel)
	{
		m_heightByLevel[0] = 0.95f;
		m_boundingBoxesByLevel[0] = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.95f, 1f))
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		bool flag = GetKind(data) == QuicksandKind.Mud;
		Color color;
		int overrideTopTextureSlot;
		if (flag)
		{
			color = Color.White;
			overrideTopTextureSlot = 248;
		}
		else
		{
			color = BlockColorsMap.SandColorsMap.LookupSand(g.Terrain, x, y, z);
			color *= 0.95f;
			color.A = byte.MaxValue;
			overrideTopTextureSlot = DefaultTextureSlot;
		}
		if (FluidBlock.GetIsTop(data))
		{
			Terrain terrain = g.Terrain;
			int cellValueFast = terrain.GetCellValueFast(x - 1, y, z - 1);
			int cellValueFast2 = terrain.GetCellValueFast(x, y, z - 1);
			int cellValueFast3 = terrain.GetCellValueFast(x + 1, y, z - 1);
			int cellValueFast4 = terrain.GetCellValueFast(x - 1, y, z);
			int cellValueFast5 = terrain.GetCellValueFast(x + 1, y, z);
			int cellValueFast6 = terrain.GetCellValueFast(x - 1, y, z + 1);
			int cellValueFast7 = terrain.GetCellValueFast(x, y, z + 1);
			int cellValueFast8 = terrain.GetCellValueFast(x + 1, y, z + 1);
			float num;
			float h;
			float h2;
			float num2;
			float num3;
			float h3;
			float num4;
			float h4;
			if (flag)
			{
				num = (h = (h2 = (num2 = (num3 = (h3 = (num4 = (h4 = 1f)))))));
			}
			else
			{
				h = CalculateNeighborHeight(cellValueFast);
				num = CalculateNeighborHeight(cellValueFast2);
				h2 = CalculateNeighborHeight(cellValueFast3);
				num2 = CalculateNeighborHeight(cellValueFast4);
				num3 = CalculateNeighborHeight(cellValueFast5);
				h3 = CalculateNeighborHeight(cellValueFast6);
				num4 = CalculateNeighborHeight(cellValueFast7);
				h4 = CalculateNeighborHeight(cellValueFast8);
			}
			float num5 = (flag ? 1f : GetLevelHeight(FluidBlock.GetLevel(data)));
			float height = FluidBlock.CalculateFluidVertexHeight(h, num, num2, num5);
			float height2 = FluidBlock.CalculateFluidVertexHeight(num, h2, num5, num3);
			float height3 = FluidBlock.CalculateFluidVertexHeight(num5, num3, num4, h4);
			float height4 = FluidBlock.CalculateFluidVertexHeight(num2, num5, h3, num4);
			if (flag)
			{
				g.GenerateNaturalCubeVertices(this, value, x, y, z, height, height2, height3, height4, color, color, color, color, color, overrideTopTextureSlot, geom.OpaqueSubsetsByFace);
				return;
			}
			float x2 = FluidBlock.ZeroSubst(num3, num5) - FluidBlock.ZeroSubst(num2, num5);
			float x3 = FluidBlock.ZeroSubst(num4, num5) - FluidBlock.ZeroSubst(num, num5);
			int overrideTopTextureSlot2 = ((MaxLevel > 1) ? (DefaultTextureSlot - (int)MathUtils.Sign(x2) - 16 * (int)MathUtils.Sign(x3)) : DefaultTextureSlot);
			g.GenerateCubeVertices(this, value, x, y, z, height, height2, height3, height4, color, color, color, color, color, overrideTopTextureSlot2, geom.OpaqueSubsetsByFace);
		}
		else if (flag)
		{
			g.GenerateNaturalCubeVertices(this, value, x, y, z, 1f, 1f, 1f, 1f, color, color, color, color, color, overrideTopTextureSlot, geom.OpaqueSubsetsByFace);
		}
		else
		{
			g.GenerateCubeVertices(this, value, x, y, z, color, geom.OpaqueSubsetsByFace);
		}
	}

	static QuicksandBlock()
	{
		QuicksandMaxLevel = 1;
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == QuicksandKind.Mud)
		{
			return 248;
		}
		return DefaultTextureSlot;
	}

	public new void GenerateFluidTerrainVertices(BlockGeometryGenerator generator, int value, int x, int y, int z, Color sideColor, Color topColor, TerrainGeometrySubset[] subset)
	{
		int id = Terrain.ExtractData(value);
		QuicksandKind kind = GetKind(id);
		int num = DefaultTextureSlot;
		if (kind == QuicksandKind.Mud)
		{
			sideColor = Color.White;
			topColor = Color.White;
			num = 248;
		}
		if (FluidBlock.GetIsTop(id))
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
			float num2 = CalculateNeighborHeight(cellValueFast2);
			float h2 = CalculateNeighborHeight(cellValueFast3);
			float num3 = CalculateNeighborHeight(cellValueFast4);
			float num4 = CalculateNeighborHeight(cellValueFast5);
			float h3 = CalculateNeighborHeight(cellValueFast6);
			float num5 = CalculateNeighborHeight(cellValueFast7);
			float h4 = CalculateNeighborHeight(cellValueFast8);
			float levelHeight = GetLevelHeight(FluidBlock.GetLevel(id));
			float height = FluidBlock.CalculateFluidVertexHeight(h, num2, num3, levelHeight);
			float height2 = FluidBlock.CalculateFluidVertexHeight(num2, h2, levelHeight, num4);
			float height3 = FluidBlock.CalculateFluidVertexHeight(levelHeight, num4, num5, h4);
			float height4 = FluidBlock.CalculateFluidVertexHeight(num3, levelHeight, h3, num5);
			float x2 = FluidBlock.ZeroSubst(num4, levelHeight) - FluidBlock.ZeroSubst(num3, levelHeight);
			float x3 = FluidBlock.ZeroSubst(num5, levelHeight) - FluidBlock.ZeroSubst(num2, levelHeight);
			int overrideTopTextureSlot = ((MaxLevel > 1) ? (num - (int)MathUtils.Sign(x2) - 16 * (int)MathUtils.Sign(x3)) : num);
			generator.GenerateCubeVertices(this, value, x, y, z, height, height2, height3, height4, sideColor, topColor, topColor, topColor, topColor, overrideTopTextureSlot, subset);
		}
		else
		{
			generator.GenerateCubeVertices(this, value, x, y, z, sideColor, subset);
		}
	}

	public override bool IsFaceTransparent(SubsystemTerrain sst, int face, int value)
	{
		if (FluidBlock.GetIsTop(Terrain.ExtractData(value)))
		{
			return face != 5;
		}
		return false;
	}

	public static QuicksandKind GetKind(int data)
	{
		if ((data & 0x40) == 0)
		{
			return QuicksandKind.Sand;
		}
		return QuicksandKind.Mud;
	}

	public static int SetKind(int data, QuicksandKind kind)
	{
		int num = data & -65;
		int num26 = ((kind == QuicksandKind.Mud) ? 64 : 0);
		return num | num26;
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int kind = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"QuicksandBlock:{kind}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int text = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"QuicksandBlock:{text}", "Description");
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = false;
	}

	public override string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == QuicksandKind.Mud)
		{
			return "DirtySquishy";
		}
		return base.GetSoundMaterialName(subsystemTerrain, value);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		BlockPlacementData placementValue = base.GetPlacementValue(subsystemTerrain, componentMiner, value, raycastResult);
		QuicksandKind kind = GetKind(Terrain.ExtractData(value));
		int value2 = placementValue.Value;
		value2 = FluidBlock.SetLevel(value2, 0);
		value2 = FluidBlock.SetIsTop(value2, isTop: true);
		value2 = SetKind(value2, kind);
		placementValue.Value = Terrain.ReplaceData(placementValue.Value, value2);
		return placementValue;
	}
}
