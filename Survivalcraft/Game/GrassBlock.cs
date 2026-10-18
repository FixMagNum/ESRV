using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class GrassBlock : CubeBlock
{
	public const int Index = 8;

	private static readonly Random _random;

	public override int GetFaceTextureSlot(int face, int value)
	{
		if ((Terrain.ExtractData(value) & 1) != 0)
		{
			return 68;
		}
		return 3;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Color color2 = color * BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
		Color topColor = color * BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color2, topColor, environmentData);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		bool flag = (Terrain.ExtractData(value) & 1) != 0;
		Color model = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(g.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, g.Terrain, x, y, z);
		Color boneAbsoluteTransform = (flag ? Color.White : model);
		if (IsAshNearby(g.Terrain, x, y, z, 4, out var distance))
		{
			boneAbsoluteTransform *= distance switch
			{
				3 => 0.5f, 
				2 => 0.3f, 
				1 => 0.1f, 
				_ => 0.7f, 
			};
		}
		g.GenerateNaturalCubeVertices(this, value, x, y, z, boneAbsoluteTransform, geom.OpaqueSubsetsByFace);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		SubsystemWeather.Season currentSeason = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, 2);
	}

	private bool IsAshNearby(Terrain terrain, int x, int y, int z, int radius, out int distance)
	{
		distance = radius + 1;
		for (int i = -radius; i <= radius; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				for (int k = -radius; k <= radius; k++)
				{
					int num = Terrain.ExtractContents(terrain.GetCellValue(x + i, y + j, z + k));
					if (num == 376 || num == 377 || num == 378)
					{
						int num2 = Math.Abs(i) + Math.Abs(j) + Math.Abs(k);
						if (num2 < distance)
						{
							distance = num2;
						}
					}
				}
			}
		}
		return distance <= radius;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner miner, int value, TerrainRaycastResult hit)
	{
		return new BlockPlacementData
		{
			Value = value,
			CellFace = hit.CellFace
		};
	}

	static GrassBlock()
	{
		_random = new Random();
	}
}
