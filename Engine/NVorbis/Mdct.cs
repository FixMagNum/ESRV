using System;
using System.Collections.Generic;

namespace NVorbis;

internal class Mdct
{
	private const float M_PI = (float)Math.PI;

	private static Dictionary<int, Mdct> _setupCache = new Dictionary<int, Mdct>(2);

	private int _n;

	private int _n2;

	private int _n4;

	private int _n8;

	private int _ld;

	private float[] _A;

	private float[] _B;

	private float[] _C;

	private ushort[] _bitrev;

	private Dictionary<int, float[]> _threadLocalBuffers = new Dictionary<int, float[]>(1);

	public static void Reverse(float[] samples, int sampleCount)
	{
		GetSetup(sampleCount).CalcReverse(samples);
	}

	private static Mdct GetSetup(int n)
	{
		lock (_setupCache)
		{
			if (!_setupCache.ContainsKey(n))
			{
				_setupCache[n] = new Mdct(n);
			}
			return _setupCache[n];
		}
	}

	private Mdct(int n)
	{
		_n = n;
		_n2 = n >> 1;
		_n4 = _n2 >> 1;
		_n8 = _n4 >> 1;
		_ld = Utils.ilog(n) - 1;
		_A = new float[_n2];
		_B = new float[_n2];
		_C = new float[_n4];
		int num2;
		int num = (num2 = 0);
		while (num < _n4)
		{
			_A[num2] = (float)Math.Cos((float)(4 * num) * (float)Math.PI / (float)n);
			_A[num2 + 1] = (float)(0.0 - Math.Sin((float)(4 * num) * (float)Math.PI / (float)n));
			_B[num2] = (float)Math.Cos((float)(num2 + 1) * (float)Math.PI / (float)n / 2f) * 0.5f;
			_B[num2 + 1] = (float)Math.Sin((float)(num2 + 1) * (float)Math.PI / (float)n / 2f) * 0.5f;
			num++;
			num2 += 2;
		}
		num = (num2 = 0);
		while (num < _n8)
		{
			_C[num2] = (float)Math.Cos((float)(2 * (num2 + 1)) * (float)Math.PI / (float)n);
			_C[num2 + 1] = (float)(0.0 - Math.Sin((float)(2 * (num2 + 1)) * (float)Math.PI / (float)n));
			num++;
			num2 += 2;
		}
		_bitrev = new ushort[_n8];
		for (int i = 0; i < _n8; i++)
		{
			_bitrev[i] = (ushort)(Utils.BitReverse((uint)i, _ld - 3) << 2);
		}
	}

	private float[] GetBuffer()
	{
		lock (_threadLocalBuffers)
		{
			if (!_threadLocalBuffers.TryGetValue(Environment.CurrentManagedThreadId, out var value))
			{
				return _threadLocalBuffers[Environment.CurrentManagedThreadId] = new float[_n2];
			}
			return value;
		}
	}

	private void CalcReverse(float[] buffer)
	{
		float[] buffer2 = GetBuffer();
		int num = _n2 - 2;
		int num2 = 0;
		int i = 0;
		for (int n = _n2; i != n; i += 4)
		{
			buffer2[num + 1] = buffer[i] * _A[num2] - buffer[i + 2] * _A[num2 + 1];
			buffer2[num] = buffer[i] * _A[num2 + 1] + buffer[i + 2] * _A[num2];
			num -= 2;
			num2 += 2;
		}
		i = _n2 - 3;
		while (num >= 0)
		{
			buffer2[num + 1] = (0f - buffer[i + 2]) * _A[num2] - (0f - buffer[i]) * _A[num2 + 1];
			buffer2[num] = (0f - buffer[i + 2]) * _A[num2 + 1] + (0f - buffer[i]) * _A[num2];
			num -= 2;
			num2 += 2;
			i -= 4;
		}
		float[] array = buffer2;
		int num3 = _n2 - 8;
		int num4 = _n4;
		int num5 = 0;
		int num6 = _n4;
		int num7 = 0;
		while (num3 >= 0)
		{
			float num8 = array[num4 + 1] - array[num5 + 1];
			float num9 = array[num4] - array[num5];
			buffer[num6 + 1] = array[num4 + 1] + array[num5 + 1];
			buffer[num6] = array[num4] + array[num5];
			buffer[num7 + 1] = num8 * _A[num3 + 4] - num9 * _A[num3 + 5];
			buffer[num7] = num9 * _A[num3 + 4] + num8 * _A[num3 + 5];
			num8 = array[num4 + 3] - array[num5 + 3];
			num9 = array[num4 + 2] - array[num5 + 2];
			buffer[num6 + 3] = array[num4 + 3] + array[num5 + 3];
			buffer[num6 + 2] = array[num4 + 2] + array[num5 + 2];
			buffer[num7 + 3] = num8 * _A[num3] - num9 * _A[num3 + 1];
			buffer[num7 + 2] = num9 * _A[num3] + num8 * _A[num3 + 1];
			num3 -= 8;
			num6 += 4;
			num7 += 4;
			num4 += 4;
			num5 += 4;
		}
		int n2 = _n >> 4;
		int i_off = _n2 - 1;
		_ = _n4;
		step3_iter0_loop(n2, buffer, i_off, -_n8);
		step3_iter0_loop(_n >> 4, buffer, _n2 - 1 - _n4, -_n8);
		int lim = _n >> 5;
		int d = _n2 - 1;
		_ = _n8;
		step3_inner_r_loop(lim, buffer, d, -(_n >> 4), 16);
		step3_inner_r_loop(_n >> 5, buffer, _n2 - 1 - _n8, -(_n >> 4), 16);
		step3_inner_r_loop(_n >> 5, buffer, _n2 - 1 - _n8 * 2, -(_n >> 4), 16);
		step3_inner_r_loop(_n >> 5, buffer, _n2 - 1 - _n8 * 3, -(_n >> 4), 16);
		int j;
		for (j = 2; j < _ld - 3 >> 1; j++)
		{
			int num10 = _n >> j + 2;
			int num11 = num10 >> 1;
			int num12 = 1 << j + 1;
			for (int k = 0; k < num12; k++)
			{
				step3_inner_r_loop(_n >> j + 4, buffer, _n2 - 1 - num10 * k, -num11, 1 << j + 3);
			}
		}
		for (; j < _ld - 6; j++)
		{
			int num13 = _n >> j + 2;
			int num14 = 1 << j + 3;
			int num15 = num13 >> 1;
			int num16 = _n >> j + 6;
			int n3 = 1 << j + 1;
			int num17 = _n2 - 1;
			int num18 = 0;
			for (int num19 = num16; num19 > 0; num19--)
			{
				step3_inner_s_loop(n3, buffer, num17, -num15, num18, num14, num13);
				num18 += num14 * 4;
				num17 -= 8;
			}
		}
		step3_inner_s_loop_ld654(_n >> 5, buffer, _n2 - 1, _n);
		int num20 = 0;
		int num21 = _n4 - 4;
		int num22 = _n2 - 4;
		while (num21 >= 0)
		{
			int num23 = _bitrev[num20];
			array[num22 + 3] = buffer[num23];
			array[num22 + 2] = buffer[num23 + 1];
			array[num21 + 3] = buffer[num23 + 2];
			array[num21 + 2] = buffer[num23 + 3];
			num23 = _bitrev[num20 + 1];
			array[num22 + 1] = buffer[num23];
			array[num22] = buffer[num23 + 1];
			array[num21 + 1] = buffer[num23 + 2];
			array[num21] = buffer[num23 + 3];
			num21 -= 4;
			num22 -= 4;
			num20 += 2;
		}
		int num24 = 0;
		int num25 = 0;
		int num26 = _n2 - 4;
		while (num25 < num26)
		{
			float num27 = array[num25] - array[num26 + 2];
			float num28 = array[num25 + 1] + array[num26 + 3];
			float num29 = _C[num24 + 1] * num27 + _C[num24] * num28;
			float num30 = _C[num24 + 1] * num28 - _C[num24] * num27;
			float num31 = array[num25] + array[num26 + 2];
			float num32 = array[num25 + 1] - array[num26 + 3];
			array[num25] = num31 + num29;
			array[num25 + 1] = num32 + num30;
			array[num26 + 2] = num31 - num29;
			array[num26 + 3] = num30 - num32;
			num27 = array[num25 + 2] - array[num26];
			num28 = array[num25 + 3] + array[num26 + 1];
			num29 = _C[num24 + 3] * num27 + _C[num24 + 2] * num28;
			num30 = _C[num24 + 3] * num28 - _C[num24 + 2] * num27;
			num31 = array[num25 + 2] + array[num26];
			num32 = array[num25 + 3] - array[num26 + 1];
			array[num25 + 2] = num31 + num29;
			array[num25 + 3] = num32 + num30;
			array[num26] = num31 - num29;
			array[num26 + 1] = num30 - num32;
			num24 += 4;
			num25 += 4;
			num26 -= 4;
		}
		int num33 = _n2 - 8;
		int num34 = _n2 - 8;
		int num35 = 0;
		int num36 = _n2 - 4;
		int num37 = _n2;
		int num38 = _n - 4;
		while (num34 >= 0)
		{
			float num39 = buffer2[num34 + 6] * _B[num33 + 7] - buffer2[num34 + 7] * _B[num33 + 6];
			float num40 = (0f - buffer2[num34 + 6]) * _B[num33 + 6] - buffer2[num34 + 7] * _B[num33 + 7];
			buffer[num35] = num39;
			buffer[num36 + 3] = 0f - num39;
			buffer[num37] = num40;
			buffer[num38 + 3] = num40;
			float num41 = buffer2[num34 + 4] * _B[num33 + 5] - buffer2[num34 + 5] * _B[num33 + 4];
			float num42 = (0f - buffer2[num34 + 4]) * _B[num33 + 4] - buffer2[num34 + 5] * _B[num33 + 5];
			buffer[num35 + 1] = num41;
			buffer[num36 + 2] = 0f - num41;
			buffer[num37 + 1] = num42;
			buffer[num38 + 2] = num42;
			num39 = buffer2[num34 + 2] * _B[num33 + 3] - buffer2[num34 + 3] * _B[num33 + 2];
			num40 = (0f - buffer2[num34 + 2]) * _B[num33 + 2] - buffer2[num34 + 3] * _B[num33 + 3];
			buffer[num35 + 2] = num39;
			buffer[num36 + 1] = 0f - num39;
			buffer[num37 + 2] = num40;
			buffer[num38 + 1] = num40;
			num41 = buffer2[num34] * _B[num33 + 1] - buffer2[num34 + 1] * _B[num33];
			num42 = (0f - buffer2[num34]) * _B[num33] - buffer2[num34 + 1] * _B[num33 + 1];
			buffer[num35 + 3] = num41;
			buffer[num36] = 0f - num41;
			buffer[num37 + 3] = num42;
			buffer[num38] = num42;
			num33 -= 8;
			num34 -= 8;
			num35 += 4;
			num37 += 4;
			num36 -= 4;
			num38 -= 4;
		}
	}

	private void step3_iter0_loop(int n, float[] e, int i_off, int k_off)
	{
		int num = i_off;
		int num2 = num + k_off;
		int num3 = 0;
		for (int num4 = n >> 2; num4 > 0; num4--)
		{
			float num5 = e[num] - e[num2];
			float num6 = e[num - 1] - e[num2 - 1];
			e[num] += e[num2];
			e[num - 1] += e[num2 - 1];
			e[num2] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 1] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += 8;
			num5 = e[num - 2] - e[num2 - 2];
			num6 = e[num - 3] - e[num2 - 3];
			e[num - 2] += e[num2 - 2];
			e[num - 3] += e[num2 - 3];
			e[num2 - 2] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 3] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += 8;
			num5 = e[num - 4] - e[num2 - 4];
			num6 = e[num - 5] - e[num2 - 5];
			e[num - 4] += e[num2 - 4];
			e[num - 5] += e[num2 - 5];
			e[num2 - 4] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 5] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += 8;
			num5 = e[num - 6] - e[num2 - 6];
			num6 = e[num - 7] - e[num2 - 7];
			e[num - 6] += e[num2 - 6];
			e[num - 7] += e[num2 - 7];
			e[num2 - 6] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 7] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += 8;
			num -= 8;
			num2 -= 8;
		}
	}

	private void step3_inner_r_loop(int lim, float[] e, int d0, int k_off, int k1)
	{
		int num = d0;
		int num2 = num + k_off;
		int num3 = 0;
		for (int num4 = lim >> 2; num4 > 0; num4--)
		{
			float num5 = e[num] - e[num2];
			float num6 = e[num - 1] - e[num2 - 1];
			e[num] += e[num2];
			e[num - 1] += e[num2 - 1];
			e[num2] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 1] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += k1;
			num5 = e[num - 2] - e[num2 - 2];
			num6 = e[num - 3] - e[num2 - 3];
			e[num - 2] += e[num2 - 2];
			e[num - 3] += e[num2 - 3];
			e[num2 - 2] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 3] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += k1;
			num5 = e[num - 4] - e[num2 - 4];
			num6 = e[num - 5] - e[num2 - 5];
			e[num - 4] += e[num2 - 4];
			e[num - 5] += e[num2 - 5];
			e[num2 - 4] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 5] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += k1;
			num5 = e[num - 6] - e[num2 - 6];
			num6 = e[num - 7] - e[num2 - 7];
			e[num - 6] += e[num2 - 6];
			e[num - 7] += e[num2 - 7];
			e[num2 - 6] = num5 * _A[num3] - num6 * _A[num3 + 1];
			e[num2 - 7] = num6 * _A[num3] + num5 * _A[num3 + 1];
			num3 += k1;
			num -= 8;
			num2 -= 8;
		}
	}

	private void step3_inner_s_loop(int n, float[] e, int i_off, int k_off, int a, int a_off, int k0)
	{
		float num = _A[a];
		float num2 = _A[a + 1];
		float num3 = _A[a + a_off];
		float num4 = _A[a + a_off + 1];
		float num5 = _A[a + a_off * 2];
		float num6 = _A[a + a_off * 2 + 1];
		float num7 = _A[a + a_off * 3];
		float num8 = _A[a + a_off * 3 + 1];
		int num9 = i_off;
		int num10 = num9 + k_off;
		for (int num11 = n; num11 > 0; num11--)
		{
			float num12 = e[num9] - e[num10];
			float num13 = e[num9 - 1] - e[num10 - 1];
			e[num9] += e[num10];
			e[num9 - 1] += e[num10 - 1];
			e[num10] = num12 * num - num13 * num2;
			e[num10 - 1] = num13 * num + num12 * num2;
			num12 = e[num9 - 2] - e[num10 - 2];
			num13 = e[num9 - 3] - e[num10 - 3];
			e[num9 - 2] += e[num10 - 2];
			e[num9 - 3] += e[num10 - 3];
			e[num10 - 2] = num12 * num3 - num13 * num4;
			e[num10 - 3] = num13 * num3 + num12 * num4;
			num12 = e[num9 - 4] - e[num10 - 4];
			num13 = e[num9 - 5] - e[num10 - 5];
			e[num9 - 4] += e[num10 - 4];
			e[num9 - 5] += e[num10 - 5];
			e[num10 - 4] = num12 * num5 - num13 * num6;
			e[num10 - 5] = num13 * num5 + num12 * num6;
			num12 = e[num9 - 6] - e[num10 - 6];
			num13 = e[num9 - 7] - e[num10 - 7];
			e[num9 - 6] += e[num10 - 6];
			e[num9 - 7] += e[num10 - 7];
			e[num10 - 6] = num12 * num7 - num13 * num8;
			e[num10 - 7] = num13 * num7 + num12 * num8;
			num9 -= k0;
			num10 -= k0;
		}
	}

	private void step3_inner_s_loop_ld654(int n, float[] e, int i_off, int base_n)
	{
		int num = base_n >> 3;
		float num2 = _A[num];
		int num3 = i_off;
		int num4 = num3 - 16 * n;
		while (num3 > num4)
		{
			float num5 = e[num3] - e[num3 - 8];
			float num6 = e[num3 - 1] - e[num3 - 9];
			e[num3] += e[num3 - 8];
			e[num3 - 1] += e[num3 - 9];
			e[num3 - 8] = num5;
			e[num3 - 9] = num6;
			num5 = e[num3 - 2] - e[num3 - 10];
			num6 = e[num3 - 3] - e[num3 - 11];
			e[num3 - 2] += e[num3 - 10];
			e[num3 - 3] += e[num3 - 11];
			e[num3 - 10] = (num5 + num6) * num2;
			e[num3 - 11] = (num6 - num5) * num2;
			num5 = e[num3 - 12] - e[num3 - 4];
			num6 = e[num3 - 5] - e[num3 - 13];
			e[num3 - 4] += e[num3 - 12];
			e[num3 - 5] += e[num3 - 13];
			e[num3 - 12] = num6;
			e[num3 - 13] = num5;
			num5 = e[num3 - 14] - e[num3 - 6];
			num6 = e[num3 - 7] - e[num3 - 15];
			e[num3 - 6] += e[num3 - 14];
			e[num3 - 7] += e[num3 - 15];
			e[num3 - 14] = (num5 + num6) * num2;
			e[num3 - 15] = (num5 - num6) * num2;
			iter_54(e, num3);
			iter_54(e, num3 - 8);
			num3 -= 16;
		}
	}

	private static void iter_54(float[] e, int z)
	{
		float num = e[z] - e[z - 4];
		float num2 = e[z] + e[z - 4];
		float num3 = e[z - 2] + e[z - 6];
		float num4 = e[z - 2] - e[z - 6];
		e[z] = num2 + num3;
		e[z - 2] = num2 - num3;
		float num5 = e[z - 3] - e[z - 7];
		e[z - 4] = num + num5;
		e[z - 6] = num - num5;
		float num6 = e[z - 1] - e[z - 5];
		float num7 = e[z - 1] + e[z - 5];
		float num8 = e[z - 3] + e[z - 7];
		e[z - 1] = num7 + num8;
		e[z - 3] = num7 - num8;
		e[z - 5] = num6 - num4;
		e[z - 7] = num6 + num4;
	}
}
