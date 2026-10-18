using Engine;

namespace Game;

public class SchnitzelFishBlock : AmazingFoodBlock
{
	public const int Index = 331;

	public SchnitzelFishBlock()
		: base("Models/SchnitzelFish", Matrix.Identity, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 241;
	}
}
