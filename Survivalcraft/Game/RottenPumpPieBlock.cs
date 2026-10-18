using Engine;

namespace Game;

public class RottenPumpPieBlock : FoodBlock
{
	public const int Index = 340;

	public RottenPumpPieBlock()
		: base("Models/PumpPie", Matrix.CreateTranslation(-0.375f, 0.25f, 0f), new Color(192, 255, 212), FoodBlock.m_compostValue)
	{
	}
}
