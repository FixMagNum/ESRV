using Engine;

namespace Game;

public class MeatPieDoughBlock : FoodBlock
{
	public const int Index = 337;

	public MeatPieDoughBlock()
		: base("Models/MeatPie", Matrix.CreateTranslation(0.5625f, -0.875f, 0f), new Color(241, 231, 214), 247)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 343;
	}
}
