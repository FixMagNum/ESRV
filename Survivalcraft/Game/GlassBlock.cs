namespace Game;

public class GlassBlock : AlphaTestCubeBlock
{
	public const int Index = 15;

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (Terrain.ExtractContents(neighborValue) == 15)
		{
			return false;
		}
		return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
	}
}
