using Engine;

namespace Game.NetWork;

public class NetVector2 : NetSimulate<Vector2>
{
	public NetVector2(Vector2 vector)
		: base(vector)
	{
		LerpFunc = (Vector2 b, Vector2 e, float f) => (f >= 1f) ? e : Vector2.Lerp(b, e, f);
	}
}
