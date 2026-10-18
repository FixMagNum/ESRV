using Engine;

namespace Game;

public class ActivatedCoalBlock : NewFoodBlock
{
	public const int Index = 322;

	public ActivatedCoalBlock()
		: base("Models/ActivatedCoal", Matrix.Identity, 242)
	{
	}
}
