namespace Game;

public class TallSpruceLeavesBlock : LeavesBlock
{
	public const int Index = 225;

	public TallSpruceLeavesBlock()
		: base(BlockColorsMap.TallSpruceLeavesColorsMap)
	{
	}

	public override bool ShouldGenerateFace(SubsystemTerrain st, int face, int value, int neighborValue)
	{
		if (SettingsManager.VegetationMode != SettingsManager.VegetationRenderMode.Full)
		{
			int boundingBox = Terrain.ExtractContents(neighborValue);
			if (BlocksManager.Blocks[boundingBox] is LeavesBlock)
			{
				return false;
			}
		}
		return base.ShouldGenerateFace(st, face, value, neighborValue);
	}
}
