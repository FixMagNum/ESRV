using Engine;

namespace Game;

public class MeatPieBlock : AmazingFoodBlock
{
	public const int Index = 332;

	public MeatPieBlock()
		: base("Models/MeatPie", Matrix.Identity, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 342;
	}
}
