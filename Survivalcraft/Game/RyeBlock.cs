using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class RyeBlock : CrossBlock
{
	public const int Index = 174;

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 7), isWild: true));
		yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 1), isWild: false));
		yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 3), isWild: false));
		yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 5), isWild: false));
		yield return Terrain.MakeBlockValue(174, 0, SetIsWild(SetSize(0, 7), isWild: false));
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		if (GetIsWild(Terrain.ExtractData(value)))
		{
			color *= BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
			BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: false, environmentData);
		}
		else
		{
			BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: false, environmentData);
		}
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		if (GetIsWild(Terrain.ExtractData(value)))
		{
			SubsystemWeather.Season currentSeason = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
			Color color = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, x, y, z);
			generator.GenerateCrossfaceVertices(this, value, x, y, z, color, GetFaceTextureSlot(0, value), geometry.SubsetAlphaTest);
		}
		else
		{
			generator.GenerateCrossfaceVertices(this, value, x, y, z, Color.White, GetFaceTextureSlot(0, value), geometry.SubsetAlphaTest);
		}
	}

	public override int GetShadowStrength(int value)
	{
		return GetSize(Terrain.ExtractData(value)) + 1;
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		if (GetIsWild(Terrain.ExtractData(value)))
		{
			SubsystemWeather.Season currentSeason = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
			Color color = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
		}
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, GetFaceTextureSlot(4, value));
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		int data = Terrain.ExtractData(value);
		int size = GetSize(data);
		if (!GetIsWild(data))
		{
			return 88 + size;
		}
		if (size > 2)
		{
			return 87;
		}
		return 86;
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int num = 0;
		int x = 0;
		int data = Terrain.ExtractData(oldValue);
		int size = GetSize(data);
		bool isWild = GetIsWild(data);
		if (isWild)
		{
			num = ((size > 2 && Random.Float(0f, 1f) < 0.33f) ? 1 : 0);
			x = 4;
		}
		else
		{
			switch (size)
			{
			case 5:
				num = 1;
				x = 4;
				break;
			case 6:
				num = Random.Int(1, 2);
				x = 4;
				break;
			case 7:
				num = Random.Int(1, 3);
				x = 5;
				break;
			}
		}
		showDebris = true;
		for (int i = 0; i < num; i++)
		{
			BlockDropValue item = new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(173, 0, x),
				Count = 1
			};
			dropValues.Add(item);
		}
		if (size != 7 || isWild)
		{
			return;
		}
		foreach (ComponentPlayer componentPlayer in subsystemTerrain.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers)
		{
			if (componentPlayer == null)
			{
				continue;
			}
			int slotValue = componentPlayer.ComponentMiner.Inventory.GetSlotValue(componentPlayer.ComponentMiner.Inventory.ActiveSlotIndex);
			if (IsRake(slotValue))
			{
				for (int j = 0; j < 3; j++)
				{
					BlockDropValue item2 = new BlockDropValue
					{
						Value = 248,
						Count = 1
					};
					dropValues.Add(item2);
				}
				subsystemTerrain.Project.FindSubsystem<SubsystemAudio>().PlayRandomSound("Audio/RakeBuff", 1f, Random.Float(-0.2f, 0.2f), componentPlayer.ComponentBody.Position, 4f, 0f);
			}
		}
	}

	public static int GetSize(int data)
	{
		return data & 7;
	}

	public static int SetSize(int data, int size)
	{
		size = MathUtils.Clamp(size, 0, 7);
		return (data & -8) | (size & 7);
	}

	public static bool GetIsWild(int data)
	{
		return (data & 8) != 0;
	}

	public static int SetIsWild(int data, bool isWild)
	{
		if (!isWild)
		{
			return data & -9;
		}
		return data | 8;
	}

	private bool IsRake(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num != 169 && num != 219 && num != 171 && num != 172)
		{
			return num == 409;
		}
		return true;
	}
}
