using Engine;

namespace Game;

public class PumpPieBlock : AmazingFoodBlock
{
	public const int Index = 327;

	public PumpPieBlock()
		: base("Models/PumpPie", Matrix.Identity, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 340;
	}
}
