using Engine;

namespace Game;

public class RawSchnitzelFishBlock : FoodBlock
{
	public const int Index = 336;

	public RawSchnitzelFishBlock()
		: base("Models/SchnitzelFish", Matrix.CreateTranslation(0.5625f, -0.875f, 0f), new Color(210, 183, 115), 247)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 241;
	}
}
