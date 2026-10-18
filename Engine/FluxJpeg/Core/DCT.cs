using System;

namespace FluxJpeg.Core;

internal class DCT
{
	private float[] _temp = new float[64];

	private static readonly float[,] c = buildC();

	private static readonly float[,] cT = buildCT();

	public const int N = 8;

	public int[][] quantum = new int[2][];

	public double[][] divisors = new double[2][];

	public double[] DivisorsLuminance = new double[64];

	public double[] DivisorsChrominance = new double[64];

	internal DCT()
	{
	}

	private static float[,] buildC()
	{
		float[,] array = new float[8, 8];
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				array[i, j] = ((i == 0) ? 0.35355338f : ((float)(0.5 * Math.Cos((2.0 * (double)j + 1.0) * (double)i * Math.PI / 16.0))));
			}
		}
		return array;
	}

	private static float[,] buildCT()
	{
		float[,] array = new float[8, 8];
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				array[j, i] = c[i, j];
			}
		}
		return array;
	}

	public static void SetValueClipped(byte[,] arr, int i, int j, float val)
	{
		arr[i, j] = (byte)((!(val < 0f)) ? ((val > 255f) ? byte.MaxValue : ((byte)((double)val + 0.5))) : 0);
	}

	internal byte[,] FastIDCT(float[] input)
	{
		byte[,] array = new byte[8, 8];
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				num = 0f;
				for (int k = 0; k < 8; k++)
				{
					num += input[i * 8 + k] * c[k, j];
				}
				_temp[num2++] = num;
			}
		}
		for (int l = 0; l < 8; l++)
		{
			for (int m = 0; m < 8; m++)
			{
				float num3 = 128f;
				for (int n = 0; n < 8; n++)
				{
					num3 += cT[l, n] * _temp[n * 8 + m];
				}
				if (num3 < 0f)
				{
					array[l, m] = 0;
				}
				else if (num3 > 255f)
				{
					array[l, m] = byte.MaxValue;
				}
				else
				{
					array[l, m] = (byte)((double)num3 + 0.5);
				}
			}
		}
		return array;
	}

	public DCT(int quality)
		: this()
	{
		Initialize(quality);
	}

	private void Initialize(int quality)
	{
		double[] array = new double[8] { 1.0, 1.387039845, 1.306562965, 1.175875602, 1.0, 0.785694958, 0.5411961, 0.275899379 };
		int num;
		if (quality <= 0)
		{
			num = 1;
		}
		if (quality > 100)
		{
			num = 100;
		}
		num = ((quality >= 50) ? (200 - quality * 2) : (5000 / quality));
		int[] table = JpegQuantizationTable.K1Luminance.getScaledInstance((float)num / 100f, forceBaseline: true).Table;
		int num2 = 0;
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				DivisorsLuminance[num2] = 1.0 / ((double)table[num2] * array[i] * array[j] * 8.0);
				num2++;
			}
		}
		int[] table2 = JpegQuantizationTable.K2Chrominance.getScaledInstance((float)num / 100f, forceBaseline: true).Table;
		num2 = 0;
		for (int k = 0; k < 8; k++)
		{
			for (int l = 0; l < 8; l++)
			{
				DivisorsChrominance[num2] = 1.0 / ((double)table2[num2] * array[k] * array[l] * 8.0);
				num2++;
			}
		}
		quantum[0] = table;
		divisors[0] = DivisorsLuminance;
		quantum[1] = table2;
		divisors[1] = DivisorsChrominance;
	}

	internal float[,] FastFDCT(float[,] input)
	{
		float[,] array = new float[8, 8];
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				array[i, j] = input[i, j] - 128f;
			}
		}
		for (int k = 0; k < 8; k++)
		{
			float num = array[k, 0] + array[k, 7];
			float num2 = array[k, 0] - array[k, 7];
			float num3 = array[k, 1] + array[k, 6];
			float num4 = array[k, 1] - array[k, 6];
			float num5 = array[k, 2] + array[k, 5];
			float num6 = array[k, 2] - array[k, 5];
			float num7 = array[k, 3] + array[k, 4];
			float num8 = array[k, 3] - array[k, 4];
			float num9 = num + num7;
			float num10 = num - num7;
			float num11 = num3 + num5;
			float num12 = num3 - num5;
			array[k, 0] = num9 + num11;
			array[k, 4] = num9 - num11;
			float num13 = (num12 + num10) * 0.70710677f;
			array[k, 2] = num10 + num13;
			array[k, 6] = num10 - num13;
			num9 = num8 + num6;
			num11 = num6 + num4;
			num12 = num4 + num2;
			float num14 = (num9 - num12) * 0.38268343f;
			float num15 = 0.5411961f * num9 + num14;
			float num16 = 1.306563f * num12 + num14;
			float num17 = num11 * 0.70710677f;
			float num18 = num2 + num17;
			float num19 = num2 - num17;
			array[k, 5] = num19 + num15;
			array[k, 3] = num19 - num15;
			array[k, 1] = num18 + num16;
			array[k, 7] = num18 - num16;
		}
		for (int l = 0; l < 8; l++)
		{
			float num20 = array[0, l] + array[7, l];
			float num21 = array[0, l] - array[7, l];
			float num22 = array[1, l] + array[6, l];
			float num23 = array[1, l] - array[6, l];
			float num24 = array[2, l] + array[5, l];
			float num25 = array[2, l] - array[5, l];
			float num26 = array[3, l] + array[4, l];
			float num27 = array[3, l] - array[4, l];
			float num28 = num20 + num26;
			float num29 = num20 - num26;
			float num30 = num22 + num24;
			float num31 = num22 - num24;
			array[0, l] = num28 + num30;
			array[4, l] = num28 - num30;
			float num32 = (num31 + num29) * 0.70710677f;
			array[2, l] = num29 + num32;
			array[6, l] = num29 - num32;
			num28 = num27 + num25;
			num30 = num25 + num23;
			num31 = num23 + num21;
			float num33 = (num28 - num31) * 0.38268343f;
			float num34 = 0.5411961f * num28 + num33;
			float num35 = 1.306563f * num31 + num33;
			float num36 = num30 * 0.70710677f;
			float num37 = num21 + num36;
			float num38 = num21 - num36;
			array[5, l] = num38 + num34;
			array[3, l] = num38 - num34;
			array[1, l] = num37 + num35;
			array[7, l] = num37 - num35;
		}
		return array;
	}

	internal int[] QuantizeBlock(float[,] inputData, int code)
	{
		int[] array = new int[64];
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				array[num] = (int)Math.Round((double)inputData[i, j] * divisors[code][num]);
				num++;
			}
		}
		return array;
	}
}
