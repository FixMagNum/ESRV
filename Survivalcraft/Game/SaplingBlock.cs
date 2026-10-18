using System.Collections.Generic;

namespace Game;

public class SaplingBlock : CrossBlock
{
	public const int Index = 119;

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		return base.GetDisplayName(subsystemTerrain, value);
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		return Terrain.ExtractData(value) switch
		{
			0 => 56, 
			1 => 72, 
			2 => 73, 
			3 => 73, 
			4 => 72, 
			_ => 56, 
		};
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(119, 0, 0);
		yield return Terrain.MakeBlockValue(119, 0, 1);
		yield return Terrain.MakeBlockValue(119, 0, 2);
		yield return Terrain.MakeBlockValue(119, 0, 3);
		yield return Terrain.MakeBlockValue(119, 0, 4);
	}
}
