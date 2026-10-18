using System;

namespace Engine;

public struct BoundingCircle(Vector2 center, float radius) : IEquatable<BoundingCircle>
{
	public Vector2 Center = center;

	public float Radius = radius;

	public override bool Equals(object obj)
	{
		if (!(obj is BoundingCircle))
		{
			return false;
		}
		return Equals((BoundingCircle)obj);
	}

	public override int GetHashCode()
	{
		return Center.GetHashCode() + Radius.GetHashCode();
	}

	public bool Equals(BoundingCircle other)
	{
		if (Center == other.Center)
		{
			return Radius == other.Radius;
		}
		return false;
	}

	public override string ToString()
	{
		return $"{Center},{Radius}";
	}

	public static bool operator ==(BoundingCircle c1, BoundingCircle c2)
	{
		return c1.Equals(c2);
	}

	public static bool operator !=(BoundingCircle c1, BoundingCircle c2)
	{
		return !c1.Equals(c2);
	}
}
