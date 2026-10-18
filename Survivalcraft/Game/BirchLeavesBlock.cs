namespace Game;

public class BirchLeavesBlock : LeavesBlock
{
	public const int Index = 13;

	public BirchLeavesBlock()
		: base(BlockColorsMap.BirchLeavesColorsMap)
	{
	}

	public override int GetShadowStrength(int value)
	{
		return DefaultShadowStrength = 0;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain st, int face, int value, int neighborValue)
	{
		if (SettingsManager.VegetationMode != SettingsManager.VegetationRenderMode.Full)
		{
			int slotValue = Terrain.ExtractContents(neighborValue);
			if (BlocksManager.Blocks[slotValue] is LeavesBlock)
			{
				return false;
			}
		}
		return base.ShouldGenerateFace(st, face, value, neighborValue);
	}
}
