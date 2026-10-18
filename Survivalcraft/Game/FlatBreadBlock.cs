using Engine;

namespace Game;

public class FlatBreadBlock : FoodBlock
{
	public const int Index = 316;

	public FlatBreadBlock()
		: base("Models/FlatBread", Matrix.Identity, Color.White, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 313;
	}
}
