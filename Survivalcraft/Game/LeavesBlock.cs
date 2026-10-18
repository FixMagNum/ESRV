using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class LeavesBlock : AlphaTestCubeBlock, IWaterloggable
{
	protected readonly BlockColorsMap m_map;

	protected static readonly Random _rand;

	public const int WaterLoggedBit = 1024;

	protected LeavesBlock(BlockColorsMap map)
	{
		m_map = map;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		int model = Terrain.ExtractData(value);
		if (!IsInvisible(g.SubsystemTerrain, value))
		{
			if (IsWaterLogged(model) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
			{
				Color sideColor;
				Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(g.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(g.Terrain, x, y, z)));
				sideColor.A = byte.MaxValue;
				Color topColor = color;
				topColor.A = 0;
				focalorsFluidBlock.GenerateWaterloggedFluidVertices(g, x, y, z, sideColor, topColor, geom.TransparentSubsetsByFace);
			}
			Terrain matrix = g.Terrain;
			bool flag = BlocksManager.Blocks[Terrain.ExtractContents(matrix.GetCellValue(x, y + 1, z))] is LeavesBlock;
			bool flag2 = BlocksManager.Blocks[Terrain.ExtractContents(matrix.GetCellValue(x, y - 1, z))] is LeavesBlock;
			bool flag3 = !flag && !flag2;
			bool flag4 = (model & 1) == 0;
			Color color2 = m_map.Lookup(matrix, x, y, z);
			if (flag4)
			{
				SubsystemWeather.Season currentSeason = g.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
				color2 = m_map.LookupSeasonalLeavesColor(currentSeason, color2, Terrain.ExtractContents(value));
			}
			if (SettingsManager.VegetationMode == SettingsManager.VegetationRenderMode.OpaqueLow)
			{
				color2.A = byte.MaxValue;
				g.GenerateNaturalLeavesVertices(this, value, x, y, z, color2, geom.OpaqueSubsetsByFace);
			}
			else
			{
				byte b = (byte)(flag3 ? 2u : 0u);
				color2.A = (byte)((color2.A & 0xFC) | b);
				g.GenerateNaturalLeavesVertices(this, value, x, y, z, color2, geom.AlphaTestSubsetsByFace);
			}
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		color *= m_map.Lookup(env.Temperature, env.Humidity);
		BlocksManager.DrawCubeBlock(r, value, new Vector3(size), ref m, color, color, env);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain sst, Vector3 pos, int value, float strength)
	{
		if (IsInvisible(sst, value))
		{
			strength = 0f;
		}
		bool flag = (Terrain.ExtractData(value) & 1) != 0;
		int x = Terrain.ToCell(pos.X);
		int y = Terrain.ToCell(pos.Y);
		int z = Terrain.ToCell(pos.Z);
		Color color = m_map.Lookup(sst.Terrain, x, y, z);
		if (!flag)
		{
			SubsystemWeather.Season currentSeason = sst.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
			color = m_map.LookupSeasonalLeavesColor(currentSeason, color, Terrain.ExtractContents(value));
		}
		return new BlockDebrisParticleSystem(sst, pos, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
	}

	public override void GetDropValues(SubsystemTerrain sst, int oldValue, int newValue, int toolLevel, List<BlockDropValue> drops, out bool showDebris)
	{
		if (IsInvisible(sst, oldValue))
		{
			showDebris = false;
			return;
		}
		if (_rand.Bool(0.15f))
		{
			drops.Add(new BlockDropValue
			{
				Value = 23,
				Count = 1
			});
			showDebris = true;
			return;
		}
		int count = drops.Count;
		base.GetDropValues(sst, oldValue, newValue, toolLevel, drops, out showDebris);
		for (int i = count; i < drops.Count; i++)
		{
			BlockDropValue value = drops[i];
			int num = Terrain.ExtractData(value.Value);
			if ((num & 0x400) != 0)
			{
				value.Value = Terrain.ReplaceData(value.Value, num & -1025);
				drops[i] = value;
			}
		}
	}

	static LeavesBlock()
	{
		_rand = new Random();
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner miner, int value, TerrainRaycastResult raycastResult)
	{
		int num = Terrain.ExtractData(value);
		num |= 1;
		value = Terrain.ReplaceData(value, num);
		return new BlockPlacementData
		{
			Value = value,
			CellFace = raycastResult.CellFace
		};
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

	public bool IsInvisible(SubsystemTerrain sst, int value)
	{
		if ((Terrain.ExtractData(value) & 1) != 0)
		{
			return false;
		}
		SubsystemWeather subsystemWeather = sst.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		return m_map.LookupSeasonalLeavesColor(subsystemWeather.CurrentSeason, Color.White, Terrain.ExtractContents(value)).A == 0;
	}

	public override string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null && IsInvisible(subsystemTerrain, value))
		{
			return string.Empty;
		}
		return base.GetSoundMaterialName(subsystemTerrain, value);
	}
}
