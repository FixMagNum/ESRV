using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class GrassSlabBlock : CubeBlock, IWaterloggable
{
	public const int Index = 356;

	private const float ScaleFactor = 0.7f;

	public const int WaterLoggedBit = 1024;

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
		float subsystemProjectiles = GetSizeFromValue(value);
		SubsystemWeather.Season projectile = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color sideColor = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(projectile, generator.Terrain, x, y, z);
		Color topColor2 = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(projectile, generator.Terrain, x, y, z);
		Color topColor3 = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(projectile, generator.Terrain, x + 1, y, z);
		Color topColor4 = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(projectile, generator.Terrain, x + 1, y, z + 1);
		Color topColor5 = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(projectile, generator.Terrain, x, y, z + 1);
		if (IsAshNearby(generator.Terrain, x, y, z, 4, out var distance))
		{
			float num = distance switch
			{
				3 => 0.5f, 
				2 => 0.3f, 
				1 => 0.1f, 
				_ => 0.7f, 
			};
			sideColor *= num;
			topColor2 *= num;
			topColor3 *= num;
			topColor4 *= num;
			topColor5 *= num;
		}
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, subsystemProjectiles, subsystemProjectiles, subsystemProjectiles, subsystemProjectiles, sideColor, topColor2, topColor3, topColor4, topColor5, -1, geometry.OpaqueSubsetsByFace);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if ((double)Random.Float(0f, 1f) < 0.05)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(318),
				Count = 1
			});
		}
		else
		{
			dropValues.Add(new BlockDropValue
			{
				Value = oldValue,
				Count = 1
			});
		}
	}

	public static float GetSizeFromValue(int value)
	{
		int num = Terrain.ExtractData(value) & -1025;
		if (num == 0)
		{
			return 0.9f;
		}
		return 0.07f * (float)num;
	}

	public static int SetSizeInValue(int value, float size)
	{
		int num = ((size != 0.9f) ? MathUtils.Clamp((int)(size / 0.07f), 1, 13) : 0);
		if (GetIsWaterLoggedFromValue(value))
		{
			num |= 0x400;
		}
		return Terrain.ReplaceData(value, num);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, GetSizeFromValue(value), 1f))
		};
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (face == 5)
		{
			return false;
		}
		int num = Terrain.ExtractContents(neighborValue);
		if (face >= 0 && face <= 3 && (num == 355 || num == 356 || num == 440))
		{
			float sizeFromValue = GetSizeFromValue(value);
			float num2 = 0f;
			switch (num)
			{
			case 355:
				num2 = DirtSlabBlock.GetSizeFromValue(neighborValue);
				break;
			case 356:
				num2 = GetSizeFromValue(neighborValue);
				break;
			case 440:
				num2 = ArchaeologicalDirtSlabBlock.GetSizeFromValue(neighborValue);
				break;
			}
			if (Math.Abs(sizeFromValue - num2) < 0.001f)
			{
				return false;
			}
		}
		if (num != 2)
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		return face switch
		{
			4 => 0, 
			5 => 2, 
			_ => 3, 
		};
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		float y = GetSizeFromValue(value) * 0.7f;
		Vector3 vector = new Vector3(0.7f, y, 0.7f);
		Matrix matrix2 = Matrix.CreateScale(vector) * matrix;
		Color color2 = color * BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
		Color topColor = color2;
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, vector, ref matrix2, color2, topColor, environmentData);
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
			for (int body = -radius; body <= radius; body++)
			{
				for (int j = -radius; j <= radius; j++)
				{
					int num = Terrain.ExtractContents(terrain.GetCellValue(x + i, y + body, z + j));
					if (num == 376 || num == 377 || num == 378)
					{
						int num2 = Math.Abs(i) + Math.Abs(body) + Math.Abs(j);
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

	private static bool GetIsWaterLoggedFromValue(int value)
	{
		return (Terrain.ExtractData(value) & 0x400) != 0;
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		if (face == 5)
		{
			return false;
		}
		return base.IsFaceTransparent(subsystemTerrain, face, value);
	}
}
