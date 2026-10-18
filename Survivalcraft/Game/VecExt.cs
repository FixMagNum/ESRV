using Engine;

namespace Game;

internal static class VecExt
{
	public static Vector3 Normalized(this Vector3 v)
	{
		float num = v.Length();
		if (!(num > 1E-06f))
		{
			return v;
		}
		return v / num;
	}
}
