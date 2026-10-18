using System;

namespace Game;

[Flags]
public enum CreatureCategory
{
	LandPredator = 1,
	LandOther = 2,
	WaterPredator = 4,
	WaterOther = 8,
	Bird = 0x10
}
