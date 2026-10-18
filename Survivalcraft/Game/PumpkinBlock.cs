using System.Collections.Generic;

namespace Game;

public class PumpkinBlock : BasePumpkinBlock
{
	public const int Index = 131;

	public PumpkinBlock()
		: base(isRotten: false)
	{
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
	}
}
