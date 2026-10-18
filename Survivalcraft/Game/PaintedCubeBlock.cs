using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class PaintedCubeBlock : CubeBlock, IPaintableBlock
{
	public int m_coloredTextureSlot;

	public PaintedCubeBlock(int coloredTextureSlot)
	{
		m_coloredTextureSlot = coloredTextureSlot;
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (!IsColored(MigrateData(Terrain.ExtractData(value))))
		{
			return DefaultTextureSlot;
		}
		return m_coloredTextureSlot;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = MigrateData(Terrain.ExtractData(value));
		Color color = SubsystemPalette.GetColor(generator, GetColor(data));
		int value2 = Terrain.ReplaceData(value, data);
		generator.GenerateCubeVertices(this, value2, x, y, z, color, geometry.OpaqueSubsetsByFace);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int data = MigrateData(Terrain.ExtractData(value));
		Color color2 = SubsystemPalette.GetColor(environmentData, GetColor(data));
		color *= color2;
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
		for (int i = 0; i < 16; i++)
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
		}
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int f = MigrateData(Terrain.ExtractData(oldValue));
		if (GetColor(f).HasValue)
		{
			showDebris = true;
			if (toolLevel >= RequiredToolLevel)
			{
				dropValues.Add(new BlockDropValue
				{
					Value = Terrain.MakeBlockValue(DefaultDropContent, 0, f),
					Count = (int)DefaultDropCount
				});
			}
		}
		else
		{
			base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
		}
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		int array = MigrateData(Terrain.ExtractData(value));
		Color i = SubsystemPalette.GetColor(subsystemTerrain, GetColor(array));
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, i, GetFaceTextureSlot(0, value));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int num = MigrateData(Terrain.ExtractData(value));
		return SubsystemPalette.GetName(subsystemTerrain, GetColor(num), LanguageControl.GetBlock($"{GetType().Name}:{num}", "DisplayName"));
	}

	public override string GetCategory(int value)
	{
		if (!GetColor(MigrateData(Terrain.ExtractData(value))).HasValue)
		{
			return base.GetCategory(value);
		}
		return "Painted";
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(MigrateData(Terrain.ExtractData(value)));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = SetColor(MigrateData(Terrain.ExtractData(value)), color);
		return Terrain.ReplaceData(value, data);
	}

	public static bool IsColored(int data)
	{
		return (data & 1) != 0;
	}

	public static int? GetColor(int data)
	{
		if ((data & 1) != 0)
		{
			return (data >> 1) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -32) | 1 | ((color.Value & 0xF) << 1);
		}
		return data & -32;
	}

	private static int MigrateData(int rawData)
	{
		if (rawData >= 32)
		{
			int num = (rawData >> 5) & 0xF;
			return SetColor(0, num);
		}
		return rawData;
	}
}
