namespace Game;

public class SpruceLeavesBlock : LeavesBlock
{
	public const int Index = 14;

	public SpruceLeavesBlock()
		: base(BlockColorsMap.SpruceLeavesColorsMap)
	{
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
