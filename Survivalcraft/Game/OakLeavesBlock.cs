namespace Game;

public class OakLeavesBlock : LeavesBlock
{
	public const int Index = 12;

	public OakLeavesBlock()
		: base(BlockColorsMap.OakLeavesColorsMap)
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
			int num = Terrain.ExtractContents(neighborValue);
			if (BlocksManager.Blocks[num] is LeavesBlock)
			{
				return false;
			}
		}
		return base.ShouldGenerateFace(st, face, value, neighborValue);
	}
}
