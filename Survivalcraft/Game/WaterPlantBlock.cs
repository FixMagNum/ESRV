using Engine;

namespace Game;

public abstract class WaterPlantBlock : Block, IWaterloggable
{
	public const int WaterLoggedBit = 1024;

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int cellContents = subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		Block block = BlocksManager.Blocks[cellContents];
		bool num = block is WaterBlock || block is FocalorsFluidBlock;
		int data = Terrain.ExtractData(value);
		if (num)
		{
			data = SetWaterLogged(data, isWaterLogged: true);
		}
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		if (IsWaterLogged(Terrain.ExtractData(value)) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			SubsystemWeather subsystemWeather = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			Color color = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(subsystemWeather.CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z));
			color.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, color, topColor, geometry.TransparentSubsetsByFace);
		}
		Color color2 = GetColor(generator.Terrain, x, y, z);
		generator.GenerateCrossfaceVertices(this, value, x, y, z, color2, GetFaceTextureSlot(-1, value), geometry.SubsetAlphaTest);
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

	protected abstract Color GetColor(Terrain terrain, int x, int y, int z);

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		Color color = GetColor(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
	}
}
