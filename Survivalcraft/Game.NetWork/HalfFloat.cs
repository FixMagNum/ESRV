using System;

namespace Game.NetWork;

public class HalfFloat
{
	private static int expSize = 6;

	private static int mSize = 15 - expSize;

	public static byte[] FloatToHalf(float f)
	{
		byte[] text = BitConverter.GetBytes(f);
		byte path = 128;
		byte[] array = new byte[2];
		path = (byte)(text[3] & path);
		sbyte b = (sbyte)(text[3] << 1);
		b += (sbyte)(text[2] >> 7);
		b -= 127;
		b += (sbyte)((1 << expSize - 1) - 1);
		if (b < 0)
		{
			b = 0;
		}
		ushort num = (ushort)(text[2] & 0x7F);
		num = (ushort)(num << mSize - 7);
		num += (ushort)(text[1] >> 15 - mSize);
		if (((text[1] >> 15 - mSize - 1) & 1) == 1)
		{
			num++;
		}
		if (num >= (ushort)Math.Pow(2.0, mSize))
		{
			num--;
		}
		ushort num2 = path;
		num2 <<= 8;
		short num3 = b;
		num3 = (short)(num3 << 15 - expSize);
		num2 += (ushort)num3;
		num2 += num;
		array[0] = (byte)num2;
		array[1] = (byte)(num2 >> 8);
		return array;
	}

	public static float HalfToFloat(byte[] myByte)
	{
		ushort num = (ushort)((ushort)(myByte[1] << 8) + myByte[0]);
		int num2 = 1;
		double num3 = 0.0;
		if (num >> 15 == 1)
		{
			num2 = -1;
		}
		int num4 = num & 0x7FFF;
		num4 >>= mSize;
		num4 -= (1 << expSize - 1) - 1;
		uint num5 = (uint)(num << expSize + 1) >> expSize + 1;
		for (int i = 0; i < mSize; i++)
		{
			if ((num5 & 1) == 1)
			{
				num3 += Math.Pow(2.0, i - mSize);
			}
			num5 >>= 1;
		}
		num3++;
		num3 *= Math.Pow(2.0, num4);
		return (float)num3 * (float)num2;
	}
}
