using Engine;

namespace Game;

public class FriedEggBlock : FoodBlock
{
	public const int Index = 321;

	public FriedEggBlock()
		: base("Models/FriedEgg", Matrix.Identity, Color.White, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 246;
	}
}
