using System;
using System.Diagnostics;
using Engine;

namespace Game;

public class Random
{
	public static int m_counter = (int)(Stopwatch.GetTimestamp() + DateTime.Now.Ticks);

	public uint m_s0;

	public int m_Seed;

	public uint m_s1;

	public ulong State
	{
		get
		{
			return m_s0 + ((ulong)m_s1 << 32);
		}
		set
		{
			m_s0 = (uint)value;
			m_s1 = (uint)(value >> 32);
		}
	}

	public Random()
	{
		Seed();
	}

	public Random(int seed)
	{
		Seed(seed);
	}

	public void Seed()
	{
		Seed(m_counter++);
	}

	public void Seed(int seed)
	{
		m_s0 = MathUtils.Hash((uint)seed);
		m_s1 = MathUtils.Hash((uint)(seed + 1));
	}

	public int Sign()
	{
		return Int() % 2 * 2 - 1;
	}

	public bool Bool()
	{
		return (Int() & 1) != 0;
	}

	public bool Bool(float probability)
	{
		return (float)Int() / 2.147484E+09f < probability;
	}

	public uint UInt()
	{
		uint componentSmartChaseBehavior = m_s0;
		uint componentClothing2 = m_s1;
		componentClothing2 ^= componentSmartChaseBehavior;
		m_s0 = RotateLeft(componentSmartChaseBehavior, 26) ^ componentClothing2 ^ (componentClothing2 << 9);
		m_s1 = RotateLeft(componentClothing2, 13);
		return RotateLeft(componentSmartChaseBehavior * 2654435771u, 5) * 5;
	}

	public int Int()
	{
		return (int)(UInt() & 0x7FFFFFFF);
	}

	public int Int(int bound)
	{
		return (int)((long)Int() * (long)bound / 2147483648u);
	}

	public int Int(int min, int max)
	{
		return (int)(min + (long)Int() * (long)(max - min + 1) / 2147483648u);
	}

	public float Float()
	{
		return (float)Int() / 2.147484E+09f;
	}

	public float Float(float min, float max)
	{
		return min + Float() * (max - min);
	}

	public float NormalFloat(float mean, float stddev)
	{
		float num = Float();
		if ((double)num < 0.5)
		{
			float num2 = MathUtils.Sqrt(-2f * MathUtils.Log(num));
			float num3 = 0.32223243f + num2 * (1f + num2 * (0.3422421f + num2 * (0.020423122f + num2 * 4.536422E-05f)));
			float num4 = 0.09934846f + num2 * (0.58858156f + num2 * (0.5311035f + num2 * (0.10353775f + num2 * 0.00385607f)));
			return mean + stddev * (num3 / num4 - num2);
		}
		float temperature = MathUtils.Sqrt(-2f * MathUtils.Log(1f - num));
		float num5 = 0.32223243f + temperature * (1f + temperature * (0.3422421f + temperature * (0.020423122f + temperature * 4.536422E-05f)));
		float topHeight = 0.09934846f + temperature * (0.58858156f + temperature * (0.5311035f + temperature * (0.10353775f + temperature * 0.00385607f)));
		return mean - stddev * (num5 / topHeight - temperature);
	}

	public Vector2 Vector2()
	{
		float num;
		float num2;
		float num3;
		float num4;
		float num5;
		do
		{
			num = 2f * Float() - 1f;
			num2 = 2f * Float() - 1f;
			num3 = num * num;
			num4 = num2 * num2;
			num5 = num3 + num4;
		}
		while (!(num5 < 1f));
		float num6 = 1f / num5;
		return new Vector2((num3 - num4) * num6, 2f * num * num2 * num6);
	}

	public Vector2 Vector2(float length)
	{
		return Engine.Vector2.Normalize(Vector2()) * length;
	}

	public Vector2 Vector2(float minLength, float maxLength)
	{
		return Engine.Vector2.Normalize(Vector2()) * Float(minLength, maxLength);
	}

	public Vector3 Vector3()
	{
		float humidity;
		float num;
		float num2;
		do
		{
			humidity = 2f * Float() - 1f;
			num = 2f * Float() - 1f;
			num2 = humidity * humidity + num * num;
		}
		while (!(num2 < 1f));
		float num3 = MathUtils.Sqrt(1f - num2);
		return new Vector3(2f * humidity * num3, 2f * num * num3, 1f - 2f * num2);
	}

	public Vector3 Vector3(float length)
	{
		return Engine.Vector3.Normalize(Vector3()) * length;
	}

	public Vector3 Vector3(float minLength, float maxLength)
	{
		return Engine.Vector3.Normalize(Vector3()) * Float(minLength, maxLength);
	}

	public static uint RotateLeft(uint x, int k)
	{
		return (x << k) | (x >> 32 - k);
	}
}
