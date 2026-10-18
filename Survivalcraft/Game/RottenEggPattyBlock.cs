using Engine;

namespace Game;

public class RottenEggPattyBlock : FoodBlock
{
	public const int Index = 338;

	public RottenEggPattyBlock()
		: base("Models/EggPatty", Matrix.CreateTranslation(-0.375f, 0.25f, 0f), Color.White, FoodBlock.m_compostValue)
	{
	}
}
