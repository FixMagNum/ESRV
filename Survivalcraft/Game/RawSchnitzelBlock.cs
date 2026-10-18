using Engine;

namespace Game;

public class RawSchnitzelBlock : FoodBlock
{
	public const int Index = 333;

	public RawSchnitzelBlock()
		: base("Models/Schnitzel", Matrix.CreateTranslation(0.5625f, -0.875f, 0f), new Color(210, 183, 115), 247)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 240;
	}
}
