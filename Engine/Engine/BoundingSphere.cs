using System;

namespace Engine;

public struct BoundingSphere(Vector3 center, float radius) : IEquatable<BoundingSphere>
{
	public Vector3 Center = center;

	public float Radius = radius;

	public override bool Equals(object obj)
	{
		if (!(obj is BoundingSphere))
		{
			return false;
		}
		return Equals((BoundingSphere)obj);
	}

	public override int GetHashCode()
	{
		return Center.GetHashCode() + Radius.GetHashCode();
	}

	public bool Equals(BoundingSphere other)
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

	public static bool operator ==(BoundingSphere s1, BoundingSphere s2)
	{
		return s1.Equals(s2);
	}

	public static bool operator !=(BoundingSphere s1, BoundingSphere s2)
	{
		return !s1.Equals(s2);
	}
}
