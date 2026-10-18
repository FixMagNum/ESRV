using Engine;

namespace Game;

public class FlatDoughBlock : FoodBlock
{
	public const int Index = 314;

	public FlatDoughBlock()
		: base("Models/FlatBread", Matrix.CreateTranslation(0.5625f, -0.875f, 0f), new Color(241, 231, 214), 247)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 315;
	}
}
