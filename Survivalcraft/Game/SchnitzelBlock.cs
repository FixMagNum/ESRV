using Engine;

namespace Game;

public class SchnitzelBlock : AmazingFoodBlock
{
	public const int Index = 330;

	public SchnitzelBlock()
		: base("Models/Schnitzel", Matrix.Identity, 242)
	{
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 240;
	}
}
