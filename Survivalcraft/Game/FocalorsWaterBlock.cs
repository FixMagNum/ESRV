using Engine;
using Engine.Graphics;

namespace Game;

public class FocalorsWaterBlock : FocalorsFluidBlock
{
	public const int Index = 18;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season componentMount = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color baseColor = BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z);
		Color color2;
		Color color = (color2 = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(componentMount, baseColor));
		color2.A = byte.MaxValue;
		Color topColor = color;
		topColor.A = 0;
		if (y < 4)
		{
			Color color3 = new Color(15, 0, 25);
			color3.A = byte.MaxValue;
			color2 = color3;
			topColor = color2;
			topColor.A = 0;
		}
		GenerateFluidTerrainVertices(generator, value, x, y, z, color2, topColor, geometry.TransparentSubsetsByFace);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		DrawFocalorsFluidBlock(primitivesRenderer, value, color * BlockColorsMap.WaterColorsMap.Lookup(12, 12), size, ref matrix, environmentData);
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (Terrain.ExtractContents(neighborValue) != 391)
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}
}
