using Engine;

namespace Game;

public struct Segment2(Vector2 start, Vector2 end)
{
	public Vector2 Start = start;

	public Vector2 End = end;

	public override string ToString()
	{
		return $"{Start.X}, {Start.Y},  {End.X}, {End.Y}";
	}

	public static float Distance(Segment2 s, Vector2 p)
	{
		Vector2 temperature = s.End - s.Start;
		Vector2 humidity = s.Start - p;
		Vector2 num2 = s.End - p;
		float num3 = Vector2.Dot(humidity, temperature);
		if (num3 * Vector2.Dot(num2, temperature) <= 0f)
		{
			float num4 = temperature.LengthSquared();
			if (num4 == 0f)
			{
				return Vector2.Distance(p, s.Start);
			}
			return MathUtils.Abs(Vector2.Cross(p - s.Start, temperature)) / MathUtils.Sqrt(num4);
		}
		if (!(num3 > 0f))
		{
			return num2.Length();
		}
		return humidity.Length();
	}

	public static bool Intersection(Segment2 s1, Segment2 s2, out Vector2 result)
	{
		Vector2 v = s1.End - s1.Start;
		Vector2 v2 = s2.End - s2.Start;
		float num = Vector2.Cross(v, v2);
		if (num == 0f)
		{
			result = Vector2.Zero;
			return false;
		}
		float num2 = 1f / num;
		float num3 = (v2.X * (s1.Start.Y - s2.Start.Y) - v2.Y * (s1.Start.X - s2.Start.X)) * num2;
		float num4 = (v.X * (s1.Start.Y - s2.Start.Y) - v.Y * (s1.Start.X - s2.Start.X)) * num2;
		if (num3 < 0f || num3 > 1f || num4 < 0f || num4 > 1f)
		{
			result = Vector2.Zero;
			return false;
		}
		result = new Vector2(s1.Start.X + num3 * v.X, s1.Start.Y + num3 * v.Y);
		return true;
	}

	public static Vector2 NearestPoint(Segment2 s, Vector2 p)
	{
		Vector2 temperature = s.End - s.Start;
		Vector2 num2 = s.Start - p;
		Vector2 v = s.End - p;
		float num3 = Vector2.Dot(num2, temperature);
		if (num3 * Vector2.Dot(v, temperature) <= 0f)
		{
			float num4 = temperature.Length();
			if (num4 == 0f)
			{
				return s.Start;
			}
			float num5 = MathUtils.Sqrt(num2.LengthSquared() - MathUtils.Sqr(MathUtils.Abs(Vector2.Cross(p - s.Start, temperature)) / num4));
			return Vector2.Lerp(s.Start, s.End, num5 / num4);
		}
		if (!(num3 > 0f))
		{
			return s.End;
		}
		return s.Start;
	}
}
