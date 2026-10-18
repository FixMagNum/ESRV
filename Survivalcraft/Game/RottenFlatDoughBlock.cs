using Engine;

namespace Game;

public class RottenFlatDoughBlock : FoodBlock
{
	public const int Index = 315;

	public RottenFlatDoughBlock()
		: base("Models/FlatBread", Matrix.CreateTranslation(-0.375f, -0.25f, 0f), new Color(192, 255, 212), FoodBlock.m_compostValue)
	{
	}
}
