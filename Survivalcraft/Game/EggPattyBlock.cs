using Engine;

namespace Game;

public class EggPattyBlock : AmazingFoodBlock
{
	public const int Index = 328;

	public EggPattyBlock()
		: base("Models/EggPatty", Matrix.Identity, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 338;
	}
}
