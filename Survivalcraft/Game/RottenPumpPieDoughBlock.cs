using Engine;

namespace Game;

public class RottenPumpPieDoughBlock : FoodBlock
{
	public const int Index = 341;

	public RottenPumpPieDoughBlock()
		: base("Models/PumpPie", Matrix.CreateTranslation(-0.375f, 0.25f, 0f), new Color(192, 255, 212), FoodBlock.m_compostValue)
	{
	}
}
