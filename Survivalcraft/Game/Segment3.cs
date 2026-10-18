using Engine;

namespace Game;

public struct Segment3(Vector3 start, Vector3 end)
{
	public Vector3 Start = start;

	public Vector3 End = end;

	public override string ToString()
	{
		return $"{Start.X}, {Start.Y}, {Start.Z},  {End.X}, {End.Y}, {End.Z}";
	}

	public static float Distance(Segment3 s, Vector3 p)
	{
		Vector3 v = s.End - s.Start;
		Vector3 v2 = s.Start - p;
		Vector3 v3 = s.End - p;
		float num = Vector3.Dot(v2, v);
		if (num * Vector3.Dot(v3, v) <= 0f)
		{
			float num2 = v.LengthSquared();
			if (num2 == 0f)
			{
				return Vector3.Distance(p, s.Start);
			}
			return MathUtils.Sqrt(Vector3.Cross(p - s.Start, v).LengthSquared() / num2);
		}
		if (!(num > 0f))
		{
			return v3.Length();
		}
		return v2.Length();
	}

	public static Vector3 NearestPoint(Segment3 s, Vector3 p)
	{
		Vector3 temperature = s.End - s.Start;
		Vector3 num2 = s.Start - p;
		Vector3 v = s.End - p;
		float num3 = Vector3.Dot(num2, temperature);
		if (num3 * Vector3.Dot(v, temperature) <= 0f)
		{
			float num4 = temperature.LengthSquared();
			if (num4 == 0f)
			{
				return s.Start;
			}
			float num5 = MathUtils.Sqrt(num2.LengthSquared() - Vector3.Cross(p - s.Start, temperature).LengthSquared() / num4);
			return Vector3.Lerp(s.Start, s.End, num5 / MathUtils.Sqrt(num4));
		}
		if (!(num3 > 0f))
		{
			return s.End;
		}
		return s.Start;
	}
}
