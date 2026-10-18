using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CottonBlock : CrossBlock
{
	public const int Index = 204;

	public new static string fName;

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(204, 0, SetIsWild(SetSize(0, 2), isWild: true));
		yield return Terrain.MakeBlockValue(204, 0, SetIsWild(SetSize(0, 1), isWild: false));
		yield return Terrain.MakeBlockValue(204, 0, SetIsWild(SetSize(0, 2), isWild: false));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		bool flatBatch2D = !GetIsWild(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"{fName}:{((!flatBatch2D) ? 1 : 0)}", "DisplayName");
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int count = Terrain.ExtractData(oldValue);
		if (GetSize(count) == 2)
		{
			BlockDropValue num = new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(173, 0, 6),
				Count = Random.Int(1, 2)
			};
			dropValues.Add(num);
			if (!GetIsWild(count))
			{
				int fill = Random.Int(1, 2);
				for (int color = 0; color < fill; color++)
				{
					num = new BlockDropValue
					{
						Value = Terrain.MakeBlockValue(205, 0, 0),
						Count = 1
					};
					dropValues.Add(num);
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
						for (int i = 0; i < 3; i++)
						{
							BlockDropValue item = new BlockDropValue
							{
								Value = 248,
								Count = 1
							};
							dropValues.Add(item);
						}
						subsystemTerrain.Project.FindSubsystem<SubsystemAudio>().PlayRandomSound("Audio/RakeBuff", 1f, Random.Float(-0.2f, 0.2f), componentPlayer.ComponentBody.Position, 4f, 0f);
					}
				}
			}
		}
		showDebris = true;
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		return GetSize(Terrain.ExtractData(value)) switch
		{
			1 => 29, 
			0 => 11, 
			_ => 30, 
		};
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		GetSize(Terrain.ExtractData(value));
		Color count = ((!GetIsWild(Terrain.ExtractData(value))) ? Color.Lerp(BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity), new Color(255, 255, 255), 0.5f) : BlockColorsMap.GrassColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity));
		BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color * count, isEmissive: false, environmentData);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		GetSize(Terrain.ExtractData(value));
		SubsystemWeather.Season currentSeason = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = ((!GetIsWild(Terrain.ExtractData(value))) ? Color.Lerp(BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, x, y, z), new Color(255, 255, 255), 0.5f) : BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, x, y, z));
		generator.GenerateCrossfaceVertices(this, value, x, y, z, color, GetFaceTextureSlot(0, value), geometry.SubsetAlphaTest);
	}

	public override int GetShadowStrength(int value)
	{
		int size = GetSize(Terrain.ExtractData(value));
		return 2 + size * 2;
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		GetSize(Terrain.ExtractData(value));
		SubsystemWeather.Season num = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		return new BlockDebrisParticleSystem(color: (!GetIsWild(Terrain.ExtractData(value))) ? Color.Lerp(BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(num, subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z)), new Color(255, 255, 255), 0.5f) : BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(num, subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z)), terrain: subsystemTerrain, position: position, strength: strength, scale: DestructionDebrisScale, textureSlot: GetFaceTextureSlot(4, value));
	}

	public static int GetSize(int data)
	{
		return data & 3;
	}

	public static int SetSize(int data, int size)
	{
		size = MathUtils.Clamp(size, 0, 2);
		return (data & -4) | (size & 3);
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

	static CottonBlock()
	{
		fName = "CottonBlock";
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
