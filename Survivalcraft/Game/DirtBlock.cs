using System;
using System.Collections.Generic;
using Engine;

namespace Game;

public class DirtBlock : CubeBlock
{
	public const int Index = 2;

	private static readonly Random _random;

	private static int s_lastDigX;

	private static int s_lastDigZ;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season currentSeason = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = new BlockColorsMap(Color.White, Color.White, Color.White, Color.White).LookupSeasonalDirtColor(currentSeason, generator.Terrain, x, y, z);
		Color color2 = Color.White * color;
		if (IsAshNearby(generator.Terrain, x, y, z, 4, out var distance))
		{
			color2 *= distance switch
			{
				3 => 0.5f, 
				2 => 0.3f, 
				1 => 0.1f, 
				_ => 0.7f, 
			};
		}
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, color2, geometry.OpaqueSubsetsByFace);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if ((double)_random.Float(0f, 1f) < 0.05)
		{
			bool flag = false;
			if (s_lastDigX != int.MinValue)
			{
				int temperature = subsystemTerrain.Terrain.GetTemperature(s_lastDigX, s_lastDigZ);
				int humidity = subsystemTerrain.Terrain.GetHumidity(s_lastDigX, s_lastDigZ);
				flag = temperature >= 12 && humidity >= 8;
			}
			WormBlock.WormKind wormKind = (flag ? WormBlock.WormKind.StripedCaterpillar : WormBlock.WormKind.Normal);
			WormBlock.WormKind k = ((_random.Float(0f, 1f) < 0.01f) ? WormBlock.WormKind.TitanitusColossus : wormKind);
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(318, 0, WormBlock.SetKind(0, k)),
				Count = 1
			});
		}
		else
		{
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.ReplaceData(oldValue, 0),
				Count = 1
			});
		}
	}

	static DirtBlock()
	{
		s_lastDigX = int.MinValue;
		s_lastDigZ = int.MinValue;
		_random = new Random();
	}

	private bool IsAshNearby(Terrain terrain, int x, int y, int z, int radius, out int distance)
	{
		distance = radius + 1;
		for (int result = -radius; result <= radius; result++)
		{
			for (int array = -radius; array <= radius; array++)
			{
				for (int i = -radius; i <= radius; i++)
				{
					int i2 = Terrain.ExtractContents(terrain.GetCellValue(x + result, y + array, z + i));
					if (i2 == 376 || i2 == 377 || i2 == 378)
					{
						int num = Math.Abs(result) + Math.Abs(array) + Math.Abs(i);
						if (num < distance)
						{
							distance = num;
						}
					}
				}
			}
		}
		return distance <= radius;
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		SubsystemWeather.Season currentSeason = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = new BlockColorsMap(Color.White, Color.White, Color.White, Color.White).LookupSeasonalDirtColor(currentSeason, subsystemTerrain.Terrain, x, y, z);
		Color color2 = Color.White * color;
		if (IsAshNearby(subsystemTerrain.Terrain, x, y, z, 4, out var distance))
		{
			color2 *= distance switch
			{
				3 => 0.5f, 
				2 => 0.3f, 
				1 => 0.1f, 
				_ => 0.7f, 
			};
		}
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color2, 2);
	}

	public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		s_lastDigX = raycastResult.CellFace.X;
		s_lastDigZ = raycastResult.CellFace.Z;
		return base.GetDigValue(subsystemTerrain, componentMiner, value, toolValue, raycastResult);
	}
}
