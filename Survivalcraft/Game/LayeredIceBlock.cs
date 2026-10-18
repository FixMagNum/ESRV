using Engine;
using Engine.Graphics;

namespace Game;

public class LayeredIceBlock : CubeBlock, IWaterloggable
{
	public const int Index = 352;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color color = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z));
			color.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, color, topColor, geometry.TransparentSubsetsByFace);
		}
		Terrain terrain = generator.Terrain;
		float sizeFromValue = GetSizeFromValue(value);
		float neighborHeight = GetNeighborHeight(terrain.GetCellValueFast(x - 1, y, z));
		float neighborHeight2 = GetNeighborHeight(terrain.GetCellValueFast(x + 1, y, z));
		float neighborHeight3 = GetNeighborHeight(terrain.GetCellValueFast(x, y, z - 1));
		float neighborHeight4 = GetNeighborHeight(terrain.GetCellValueFast(x, y, z + 1));
		float neighborHeight5 = GetNeighborHeight(terrain.GetCellValueFast(x - 1, y, z - 1));
		float neighborHeight6 = GetNeighborHeight(terrain.GetCellValueFast(x + 1, y, z - 1));
		float neighborHeight7 = GetNeighborHeight(terrain.GetCellValueFast(x - 1, y, z + 1));
		float neighborHeight8 = GetNeighborHeight(terrain.GetCellValueFast(x + 1, y, z + 1));
		float height = MathUtils.Max(sizeFromValue, neighborHeight, neighborHeight3, neighborHeight5);
		float height2 = MathUtils.Max(sizeFromValue, neighborHeight2, neighborHeight3, neighborHeight6);
		float height3 = MathUtils.Max(sizeFromValue, neighborHeight2, neighborHeight4, neighborHeight8);
		float height4 = MathUtils.Max(sizeFromValue, neighborHeight, neighborHeight4, neighborHeight7);
		Color white = Color.White;
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, height, height2, height3, height4, white, white, white, white, white, -1, geometry.OpaqueSubsetsByFace);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return new BoundingBox[1]
		{
			new BoundingBox(Vector3.Zero, new Vector3(1f, GetSizeFromValue(value), 1f))
		};
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		float sizeFromValue = GetSizeFromValue(value);
		Matrix matrix2 = Matrix.CreateScale(1f, sizeFromValue, 1f) * matrix;
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix2, color, color, environmentData);
	}

	public static float GetSizeFromValue(int value)
	{
		return MathUtils.Clamp((float)((Terrain.ExtractData(value) & -1025) + 1) / 16f, 0.01f, 1f);
	}

	public static int SetSizeInValue(int value, float size)
	{
		int num = MathUtils.Clamp((int)(size * 16f) - 1, 0, 15);
		if ((Terrain.ExtractData(value) & 0x400) != 0)
		{
			num |= 0x400;
		}
		return Terrain.ReplaceData(value, num);
	}

	private float GetNeighborHeight(int value)
	{
		return Terrain.ExtractContents(value) switch
		{
			352 => GetSizeFromValue(value), 
			62 => 1f, 
			_ => 0f, 
		};
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
