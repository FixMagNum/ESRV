using Engine;

namespace Game;

public class RottenMeatPieDoughBlock : FoodBlock
{
	public const int Index = 343;

	public RottenMeatPieDoughBlock()
		: base("Models/MeatPie", Matrix.CreateTranslation(-0.375f, 0.25f, 0f), new Color(192, 255, 212), FoodBlock.m_compostValue)
	{
	}
}
