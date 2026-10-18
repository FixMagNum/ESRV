using System.Collections.Generic;

namespace Game.NetWork;

public class TestNumSerial
{
	public static byte[] SerialFloat(float f)
	{
		int num = (int)f;
		return null;
	}

	public static void DeSerialFloat()
	{
	}

	public static byte[] SerialInt(int v)
	{
		List<byte> list = new List<byte>();
		byte b = 0;
		if (v < 0)
		{
			b |= 0x80;
		}
		uint num = (uint)(v & 0x7FFFFFFF);
		if (num <= 255)
		{
			b |= 1;
		}
		else if (num <= 65535)
		{
			b |= 2;
		}
		else if (num <= 16777215)
		{
			b |= 3;
		}
		else
		{
			b |= 4;
		}
		return list.ToArray();
	}

	public static int DeSerialInt(byte[] buf)
	{
		byte[] path = CommonLib.Decompress(buf);
		int xElement = 0;
		for (int stream2 = path.Length - 1; stream2 >= 0; stream2--)
		{
			xElement |= path[stream2];
			xElement <<= 8;
		}
		return xElement;
	}
}
