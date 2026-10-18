using Engine;

namespace Game;

public class GaletteBlock : NewFoodBlock
{
	public const int Index = 329;

	public GaletteBlock()
		: base("Models/Galette", Matrix.Identity, 242)
	{
	}
}
