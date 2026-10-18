using Engine;

namespace Game;

public abstract class MountedElectricElementBlock : Block, IElectricElementBlock, IWaterloggable
{
	public abstract int GetFace(int value);

	public abstract ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z);

	public abstract ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z);

	public virtual int GetConnectionMask(int value)
	{
		return int.MaxValue;
	}

	public virtual bool IsWaterLogged(int data)
	{
		return (data & 0x400) != 0;
	}

	public virtual int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (!isWaterLogged)
		{
			return data & -1025;
		}
		return data | 0x400;
	}

	public void DrawWaterInside(BlockGeometryGenerator generator, TerrainGeometry geometry, int x, int y, int z, int data)
	{
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsWaterBlock focalorsWaterBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsWaterBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
	}
}
