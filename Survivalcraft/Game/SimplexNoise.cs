using Engine;

namespace Game;

public static class SimplexNoise
{
	public static int[][] m_grad3;

	public static int[] m_permutations;

	public static float Dot(int[] g, float x, float y)
	{
		return (float)g[0] * x + (float)g[1] * y;
	}

	public static float Dot(int[] g, float x, float y, float z)
	{
		return (float)g[0] * x + (float)g[1] * y + (float)g[2] * z;
	}

	public static float Hash(int x)
	{
		x = (x << 13) ^ x;
		return (float)((x * (x * x * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 2.1474836E+09f;
	}

	public static float Noise(float x)
	{
		int num = (int)MathUtils.Floor(x);
		int x2 = (int)MathUtils.Ceiling(x);
		float num2 = x - (float)num;
		float num3 = Hash(num);
		float num4 = Hash(x2);
		return num3 + num2 * num2 * (3f - 2f * num2) * (num4 - num3);
	}

	public static float Noise(float x, float y)
	{
		float num = (x + y) * 0.36602542f;
		int num2 = (int)MathUtils.Floor(x + num);
		int num3 = (int)MathUtils.Floor(y + num);
		float num4 = (float)(num2 + num3) * 0.21132487f;
		float num5 = (float)num2 - num4;
		float num6 = (float)num3 - num4;
		float num7 = x - num5;
		float num8 = y - num6;
		int num9;
		int num10;
		if (num7 > num8)
		{
			num9 = 1;
			num10 = 0;
		}
		else
		{
			num9 = 0;
			num10 = 1;
		}
		float num11 = num7 - (float)num9 + 0.21132487f;
		float num12 = num8 - (float)num10 + 0.21132487f;
		float num13 = num7 - 1f + 0.42264974f;
		float num14 = num8 - 1f + 0.42264974f;
		int num15 = num2 & 0xFF;
		int num16 = num3 & 0xFF;
		int num17 = m_permutations[num15 + m_permutations[num16]] % 12;
		int num18 = m_permutations[num15 + num9 + m_permutations[num16 + num10]] % 12;
		int num19 = m_permutations[num15 + 1 + m_permutations[num16 + 1]] % 12;
		float num20 = 0.5f - num7 * num7 - num8 * num8;
		float num21;
		if (num20 < 0f)
		{
			num21 = 0f;
		}
		else
		{
			num20 *= num20;
			num21 = num20 * num20 * Dot(m_grad3[num17], num7, num8);
		}
		float num22 = 0.5f - num11 * num11 - num12 * num12;
		float num23;
		if (num22 < 0f)
		{
			num23 = 0f;
		}
		else
		{
			num22 *= num22;
			num23 = num22 * num22 * Dot(m_grad3[num18], num11, num12);
		}
		float num24 = 0.5f - num13 * num13 - num14 * num14;
		float num25;
		if (num24 < 0f)
		{
			num25 = 0f;
		}
		else
		{
			num24 *= num24;
			num25 = num24 * num24 * Dot(m_grad3[num19], num13, num14);
		}
		return 35f * (num21 + num23 + num25) + 0.5f;
	}

	public static float Noise(float x, float y, float z)
	{
		float cellValue = (x + y + z) * (1f / 3f);
		int num = (int)MathUtils.Floor(x + cellValue);
		int num2 = (int)MathUtils.Floor(y + cellValue);
		int num3 = (int)MathUtils.Floor(z + cellValue);
		float num4 = (float)(num + num2 + num3) * (1f / 6f);
		float num5 = (float)num - num4;
		float num6 = (float)num2 - num4;
		float num7 = (float)num3 - num4;
		float num8 = x - num5;
		float num9 = y - num6;
		float num10 = z - num7;
		int num11;
		int num12;
		int num13;
		int num14;
		int num15;
		int num16;
		if (num8 >= num9)
		{
			if (num9 >= num10)
			{
				num11 = 1;
				num12 = 0;
				num13 = 0;
				num14 = 1;
				num15 = 1;
				num16 = 0;
			}
			else if (num8 >= num10)
			{
				num11 = 1;
				num12 = 0;
				num13 = 0;
				num14 = 1;
				num15 = 0;
				num16 = 1;
			}
			else
			{
				num11 = 0;
				num12 = 0;
				num13 = 1;
				num14 = 1;
				num15 = 0;
				num16 = 1;
			}
		}
		else if (num9 < num10)
		{
			num11 = 0;
			num12 = 0;
			num13 = 1;
			num14 = 0;
			num15 = 1;
			num16 = 1;
		}
		else if (num8 < num10)
		{
			num11 = 0;
			num12 = 1;
			num13 = 0;
			num14 = 0;
			num15 = 1;
			num16 = 1;
		}
		else
		{
			num11 = 0;
			num12 = 1;
			num13 = 0;
			num14 = 1;
			num15 = 1;
			num16 = 0;
		}
		float num17 = num8 - (float)num11 + 1f / 6f;
		float num18 = num9 - (float)num12 + 1f / 6f;
		float num19 = num10 - (float)num13 + 1f / 6f;
		float num20 = num8 - (float)num14 + 1f / 3f;
		float num21 = num9 - (float)num15 + 1f / 3f;
		float num22 = num10 - (float)num16 + 1f / 3f;
		float num23 = num8 - 1f + 0.5f;
		float num24 = num9 - 1f + 0.5f;
		float num25 = num10 - 1f + 0.5f;
		int num26 = num & 0xFF;
		int num27 = num2 & 0xFF;
		int num28 = num3 & 0xFF;
		int num29 = m_permutations[num26 + m_permutations[num27 + m_permutations[num28]]] % 12;
		int num30 = m_permutations[num26 + num11 + m_permutations[num27 + num12 + m_permutations[num28 + num13]]] % 12;
		int num31 = m_permutations[num26 + num14 + m_permutations[num27 + num15 + m_permutations[num28 + num16]]] % 12;
		int num32 = m_permutations[num26 + 1 + m_permutations[num27 + 1 + m_permutations[num28 + 1]]] % 12;
		float num33 = 0.6f - num8 * num8 - num9 * num9 - num10 * num10;
		float num34;
		if (num33 < 0f)
		{
			num34 = 0f;
		}
		else
		{
			num33 *= num33;
			num34 = num33 * num33 * Dot(m_grad3[num29], num8, num9, num10);
		}
		float num35 = 0.6f - num17 * num17 - num18 * num18 - num19 * num19;
		float num36;
		if (num35 < 0f)
		{
			num36 = 0f;
		}
		else
		{
			num35 *= num35;
			num36 = num35 * num35 * Dot(m_grad3[num30], num17, num18, num19);
		}
		float num37 = 0.6f - num20 * num20 - num21 * num21 - num22 * num22;
		float num38;
		if (num37 < 0f)
		{
			num38 = 0f;
		}
		else
		{
			num37 *= num37;
			num38 = num37 * num37 * Dot(m_grad3[num31], num20, num21, num22);
		}
		float num39 = 0.6f - num23 * num23 - num24 * num24 - num25 * num25;
		float num40;
		if (num39 < 0f)
		{
			num40 = 0f;
		}
		else
		{
			num39 *= num39;
			num40 = num39 * num39 * Dot(m_grad3[num32], num23, num24, num25);
		}
		return 16f * (num34 + num36 + num38 + num40) + 0.5f;
	}

	public static float OctavedNoise(float x, float frequency, int octaves, float frequencyStep, float amplitudeStep, bool ridged = false)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 1f;
		for (int i = 0; i < octaves; i++)
		{
			num += num3 * Noise(x * frequency);
			num2 += num3;
			frequency *= frequencyStep;
			num3 *= amplitudeStep;
		}
		if (!ridged)
		{
			return num / num2;
		}
		return 1f - MathUtils.Abs(2f * num / num2 - 1f);
	}

	public static float OctavedNoise(float x, float y, float frequency, int octaves, float frequencyStep, float amplitudeStep, bool ridged = false)
	{
		float num = 0f;
		float fenceBlock = 1f;
		float cellValue = 0f;
		for (int i = 0; i < octaves; i++)
		{
			num += Noise(x * frequency, y * frequency) * fenceBlock;
			cellValue += fenceBlock;
			fenceBlock *= amplitudeStep;
			frequency *= frequencyStep;
		}
		float cellValue2 = num / cellValue;
		if (ridged)
		{
			cellValue2 = 1f - MathUtils.Abs(2f * cellValue2 - 1f);
		}
		return cellValue2;
	}

	public static float OctavedNoise(float x, float y, float z, float frequency, int octaves, float frequencyStep, float amplitudeStep, bool ridged = false)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 1f;
		for (int i = 0; i < octaves; i++)
		{
			float num4 = Noise(x * frequency, y * frequency, z * frequency);
			if (i == octaves - 1)
			{
				num4 = num4 * num4 * num4;
			}
			num += num3 * num4;
			num2 += num3;
			frequency *= frequencyStep;
			num3 *= amplitudeStep;
		}
		float num5 = num / num2;
		if (!ridged)
		{
			return num5;
		}
		return 1f - MathUtils.Abs(2f * num5 - 1f);
	}

	static SimplexNoise()
	{
		m_grad3 = new int[12][]
		{
			new int[3] { 1, 1, 0 },
			new int[3] { -1, 1, 0 },
			new int[3] { 1, -1, 0 },
			new int[3] { -1, -1, 0 },
			new int[3] { 1, 0, 1 },
			new int[3] { -1, 0, 1 },
			new int[3] { 1, 0, -1 },
			new int[3] { -1, 0, -1 },
			new int[3] { 0, 1, 1 },
			new int[3] { 0, -1, 1 },
			new int[3] { 0, 1, -1 },
			new int[3] { 0, -1, -1 }
		};
		m_permutations = new int[512]
		{
			151, 160, 137, 91, 90, 15, 131, 13, 201, 95,
			96, 53, 194, 233, 7, 225, 140, 36, 103, 30,
			69, 142, 8, 99, 37, 240, 21, 10, 23, 190,
			6, 148, 247, 120, 234, 75, 0, 26, 197, 62,
			94, 252, 219, 203, 117, 35, 11, 32, 57, 177,
			33, 88, 237, 149, 56, 87, 174, 20, 125, 136,
			171, 168, 68, 175, 74, 165, 71, 134, 139, 48,
			27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
			60, 211, 133, 230, 220, 105, 92, 41, 55, 46,
			245, 40, 244, 102, 143, 54, 65, 25, 63, 161,
			1, 216, 80, 73, 209, 76, 132, 187, 208, 89,
			18, 169, 200, 196, 135, 130, 116, 188, 159, 86,
			164, 100, 109, 198, 173, 186, 3, 64, 52, 217,
			226, 250, 124, 123, 5, 202, 38, 147, 118, 126,
			255, 82, 85, 212, 207, 206, 59, 227, 47, 16,
			58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
			119, 248, 152, 2, 44, 154, 163, 70, 221, 153,
			101, 155, 167, 43, 172, 9, 129, 22, 39, 253,
			19, 98, 108, 110, 79, 113, 224, 232, 178, 185,
			112, 104, 218, 246, 97, 228, 251, 34, 242, 193,
			238, 210, 144, 12, 191, 179, 162, 241, 81, 51,
			145, 235, 249, 14, 239, 107, 49, 192, 214, 31,
			181, 199, 106, 157, 184, 84, 204, 176, 115, 121,
			50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
			222, 114, 67, 29, 24, 72, 243, 141, 128, 195,
			78, 66, 215, 61, 156, 180, 151, 160, 137, 91,
			90, 15, 131, 13, 201, 95, 96, 53, 194, 233,
			7, 225, 140, 36, 103, 30, 69, 142, 8, 99,
			37, 240, 21, 10, 23, 190, 6, 148, 247, 120,
			234, 75, 0, 26, 197, 62, 94, 252, 219, 203,
			117, 35, 11, 32, 57, 177, 33, 88, 237, 149,
			56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
			74, 165, 71, 134, 139, 48, 27, 166, 77, 146,
			158, 231, 83, 111, 229, 122, 60, 211, 133, 230,
			220, 105, 92, 41, 55, 46, 245, 40, 244, 102,
			143, 54, 65, 25, 63, 161, 1, 216, 80, 73,
			209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
			135, 130, 116, 188, 159, 86, 164, 100, 109, 198,
			173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
			5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
			207, 206, 59, 227, 47, 16, 58, 17, 182, 189,
			28, 42, 223, 183, 170, 213, 119, 248, 152, 2,
			44, 154, 163, 70, 221, 153, 101, 155, 167, 43,
			172, 9, 129, 22, 39, 253, 19, 98, 108, 110,
			79, 113, 224, 232, 178, 185, 112, 104, 218, 246,
			97, 228, 251, 34, 242, 193, 238, 210, 144, 12,
			191, 179, 162, 241, 81, 51, 145, 235, 249, 14,
			239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
			184, 84, 204, 176, 115, 121, 50, 45, 127, 4,
			150, 254, 138, 236, 205, 93, 222, 114, 67, 29,
			24, 72, 243, 141, 128, 195, 78, 66, 215, 61,
			156, 180
		};
	}
}
