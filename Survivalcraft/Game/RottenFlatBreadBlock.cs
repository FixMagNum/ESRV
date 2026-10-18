using Engine;

namespace Game;

public class RottenFlatBreadBlock : FoodBlock
{
	public const int Index = 313;

	public RottenFlatBreadBlock()
		: base("Models/FlatBread", Matrix.CreateTranslation(-0.375f, -0.25f, 0f), Color.White, FoodBlock.m_compostValue)
	{
	}
}
