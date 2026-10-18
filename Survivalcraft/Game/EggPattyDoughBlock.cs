using Engine;

namespace Game;

public class EggPattyDoughBlock : FoodBlock
{
	public const int Index = 334;

	public EggPattyDoughBlock()
		: base("Models/EggPatty", Matrix.CreateTranslation(0.5625f, -0.875f, 0f), new Color(241, 231, 214), 247)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 339;
	}
}
