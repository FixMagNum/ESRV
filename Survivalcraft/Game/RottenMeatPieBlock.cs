using Engine;

namespace Game;

public class RottenMeatPieBlock : FoodBlock
{
	public const int Index = 342;

	public RottenMeatPieBlock()
		: base("Models/MeatPie", Matrix.CreateTranslation(-0.375f, 0.25f, 0f), Color.White, FoodBlock.m_compostValue)
	{
	}
}
