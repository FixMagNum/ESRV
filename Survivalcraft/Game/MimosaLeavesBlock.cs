namespace Game;

public class MimosaLeavesBlock : LeavesBlock
{
	public const int Index = 256;

	public MimosaLeavesBlock()
		: base(BlockColorsMap.MimosaLeavesColorsMap)
	{
	}

	public override bool ShouldGenerateFace(SubsystemTerrain st, int face, int value, int neighborValue)
	{
		if (SettingsManager.VegetationMode != SettingsManager.VegetationRenderMode.Full)
		{
			int packedValue = Terrain.ExtractContents(neighborValue);
			if (BlocksManager.Blocks[packedValue] is LeavesBlock)
			{
				return false;
			}
		}
		return base.ShouldGenerateFace(st, face, value, neighborValue);
	}
}
