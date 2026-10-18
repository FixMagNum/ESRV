namespace Game;

public class DecorStrawBlock : StrawBehaviorBlock
{
	public const int Index = 309;

	public DecorStrawBlock()
		: base(144, 144)
	{
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (Terrain.ExtractContents(neighborValue) != 309)
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}
}
