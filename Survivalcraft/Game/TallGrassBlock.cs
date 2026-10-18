using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class TallGrassBlock : CrossBlock
{
	public const int Index = 19;

	private static int s_lastDigX = int.MinValue;

	private static int s_lastDigZ = int.MinValue;

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int data = Terrain.ExtractData(oldValue);
		bool flag = false;
		if (s_lastDigX != int.MinValue)
		{
			int temperature = subsystemTerrain.Terrain.GetTemperature(s_lastDigX, s_lastDigZ);
			int pickable = subsystemTerrain.Terrain.GetHumidity(s_lastDigX, s_lastDigZ);
			flag = temperature >= 12 && pickable >= 8;
		}
		if (flag && Random.Float(0f, 1f) < 0.05f)
		{
			int value = Terrain.MakeBlockValue(318, 0, WormBlock.SetKind(0, WormBlock.WormKind.StripedCaterpillar));
			dropValues.Add(new BlockDropValue
			{
				Value = value,
				Count = 1
			});
		}
		else
		{
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(19, 0, data),
				Count = 1
			});
		}
		showDebris = true;
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (!GetIsSmall(Terrain.ExtractData(value)))
		{
			return 85;
		}
		return 84;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season currentSeason = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, x, y, z);
		generator.GenerateCrossfaceVertices(this, value, x, y, z, color, GetFaceTextureSlot(0, value), geometry.SubsetAlphaTest);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		SubsystemWeather.Season baitStrengthFromBobberValue = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color num = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(baitStrengthFromBobberValue, subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, num, GetFaceTextureSlot(4, value));
	}

	public static bool GetIsSmall(int data)
	{
		return (data & 8) != 0;
	}

	public static int SetIsSmall(int data, bool isSmall)
	{
		if (!isSmall)
		{
			return data & -9;
		}
		return data | 8;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color * BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity), isEmissive: false, environmentData);
	}

	public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		s_lastDigX = raycastResult.CellFace.X;
		s_lastDigZ = raycastResult.CellFace.Z;
		return base.GetDigValue(subsystemTerrain, componentMiner, value, toolValue, raycastResult);
	}
}
