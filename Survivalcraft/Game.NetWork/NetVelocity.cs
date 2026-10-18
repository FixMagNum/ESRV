using Engine;

namespace Game.NetWork;

public class NetVelocity : NetSimulate<Vector3>
{
	public NetVelocity(Vector3 vector)
		: base(vector)
	{
		LerpFunc = (Vector3 b, Vector3 e, float f) => (f >= 1f) ? e : Vector3.Lerp(b, e, f);
	}
}
