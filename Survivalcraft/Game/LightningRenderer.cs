using System;
using Engine;
using Engine.Graphics;

namespace Game;

public static class LightningRenderer
{
	public static class Config
	{
		public static float ThicknessMultiplier;

		public static float JaggednessMultiplier;

		public static int ExtraBranches;

		public static float ArcIntensity;

		public static bool DrawCore;

		public static bool EveryFrameRandom;

		public static Color? ColorOverride;

		static Config()
		{
			ThicknessMultiplier = 1f;
			JaggednessMultiplier = 1f;
			ExtraBranches = 2;
			ArcIntensity = 0.4f;
			DrawCore = true;
			EveryFrameRandom = false;
			ColorOverride = null;
		}
	}

	private static DynamicArray<Vector3> m_points;

	private static Random m_rand;

	public static void DrawLightning(FlatBatch3D fb, Vector3 start, Vector3 end, Camera camera, Color color, float thickness, float jaggedness, int steps, int seed)
	{
		float num = thickness * Config.ThicknessMultiplier;
		float num2 = jaggedness * Config.JaggednessMultiplier;
		Color col = Config.ColorOverride ?? color;
		int num3 = (Config.EveryFrameRandom ? (seed + Time.FrameIndex * 31) : seed);
		m_points.Clear();
		GenerateAdvancedPath(start, end, num2, Config.ArcIntensity, num3, m_points);
		DrawPathSegments(fb, m_points, camera, col, num, Config.DrawCore);
		if (Config.ExtraBranches > 0 && m_points.Count > 3)
		{
			for (int i = 0; i < Config.ExtraBranches; i++)
			{
				int num4 = (int)(1 + MathUtils.Hash((uint)(num3 + i)) % (uint)Math.Max(1, m_points.Count - 2));
				Vector3 vector = m_points.Array[num4];
				uint num5 = MathUtils.Hash((uint)(num3 + i + 500));
				Vector3 vector2 = new Vector3((float)(num5 & 0xFF) / 255f - 0.5f, (float)((num5 >> 8) & 0xFF) / 255f - 0.5f, (float)((num5 >> 16) & 0xFF) / 255f - 0.5f);
				Vector3 vector3 = Vector3.Normalize(Vector3.Normalize(end - start) * 0.3f + vector2);
				Vector3 end2 = vector + vector3 * (Vector3.Distance(start, end) * 0.35f);
				DynamicArray<Vector3> dynamicArray = new DynamicArray<Vector3>();
				GenerateAdvancedPath(vector, end2, num2 * 0.7f, 0.1f, num3 + i + 77, dynamicArray);
				DrawPathSegments(fb, dynamicArray, camera, col, num * 0.6f, Config.DrawCore);
			}
		}
	}

	static LightningRenderer()
	{
		m_points = new DynamicArray<Vector3>();
		m_rand = new Random();
	}

	private static void GenerateAdvancedPath(Vector3 start, Vector3 end, float jagged, float arc, int seed, DynamicArray<Vector3> result)
	{
		Vector3 vector = end - start;
		float num = vector.Length();
		Vector3 v = ((MathUtils.Abs(Vector3.Dot(vector / num, Vector3.UnitY)) > 0.9f) ? Vector3.UnitX : Vector3.UnitY);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, v));
		Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector, vector2));
		uint num2 = MathUtils.Hash((uint)seed);
		Vector3 vector4 = vector2 * MathUtils.Cos(num2) + vector3 * MathUtils.Sin(num2);
		result.Add(start);
		float num3 = 0f;
		while (num3 < 1f)
		{
			float f = (float)(MathUtils.Hash((uint)(seed + (int)(num3 * 1000f))) & 0xFF) / 255f;
			num3 += MathUtils.Lerp(0.04f, 0.12f, f);
			if (num3 > 1f)
			{
				num3 = 1f;
			}
			Vector3 item = Vector3.Lerp(start, end, num3);
			if (num3 > 0f && num3 < 1f)
			{
				float num4 = (1f - MathUtils.Sqr(2f * num3 - 1f)) * arc * num;
				item += vector4 * num4;
				uint num5 = MathUtils.Hash((uint)(seed + (int)(num3 * 137f)));
				float num6 = (float)(num5 & 0xFF) / 255f * jagged * num * 0.15f;
				item += vector2 * (MathUtils.Cos(num5 >> 8) * num6) + vector3 * (MathUtils.Sin(num5 >> 8) * num6);
			}
			result.Add(item);
		}
	}

	private static void DrawPathSegments(FlatBatch3D fb, DynamicArray<Vector3> points, Camera camera, Color col, float thick, bool drawCore)
	{
		Vector3 viewDirection = camera.ViewDirection;
		for (int i = 0; i < points.Count - 1; i++)
		{
			Vector3 vector = points.Array[i];
			Vector3 vector2 = points.Array[i + 1];
			Vector3 v = vector2 - vector;
			if (v.LengthSquared() >= 1E-05f)
			{
				Vector3 vector3 = Vector3.Normalize(Vector3.Cross(viewDirection, v));
				Vector3 vector4 = vector3 * (thick * 0.5f);
				fb.QueueQuad(vector - vector4, vector + vector4, vector2 + vector4, vector2 - vector4, col);
				if (drawCore)
				{
					Vector3 vector5 = vector3 * (thick * 0.18f);
					fb.QueueQuad(vector - vector5, vector + vector5, vector2 + vector5, vector2 - vector5, Color.White * ((float)(int)col.A / 255f));
				}
			}
		}
	}
}
