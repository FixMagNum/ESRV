using Engine;

namespace Game;

public static class LightingManager
{
	public static readonly float LightAmbient;

	public static readonly Vector3 DirectionToLight1;

	public static readonly Vector3 DirectionToLight2;

	public static readonly float[] LightIntensityByLightValue;

	public static readonly float[] LightIntensityByLightValueAndFace;

	public static void Initialize()
	{
		SettingsManager.SettingChanged += (string name) =>
		{
			if (name == "Brightness" || name == "NightVisionEnabled")
			{
				CalculateLightingTables();
			}
		};
		CalculateLightingTables();
	}

	public static float CalculateLighting(Vector3 normal)
	{
		return LightAmbient + MathUtils.Max(Vector3.Dot(normal, DirectionToLight1), 0f) + MathUtils.Max(Vector3.Dot(normal, DirectionToLight2), 0f);
	}

	public static float? CalculateSmoothLight(SubsystemTerrain subsystemTerrain, Vector3 p)
	{
		p -= new Vector3(0.5f);
		int color2 = (int)MathUtils.Floor(p.X);
		int color3 = (int)MathUtils.Floor(p.Y);
		int color4 = (int)MathUtils.Floor(p.Z);
		int x = (int)MathUtils.Ceiling(p.X);
		int num = (int)MathUtils.Ceiling(p.Y);
		int z = (int)MathUtils.Ceiling(p.Z);
		Terrain terrain = subsystemTerrain.Terrain;
		if (color3 >= 0 && num <= 255)
		{
			TerrainChunk chunkAtCell = terrain.GetChunkAtCell(color2, color4);
			TerrainChunk chunkAtCell2 = terrain.GetChunkAtCell(x, color4);
			TerrainChunk chunkAtCell3 = terrain.GetChunkAtCell(color2, z);
			TerrainChunk chunkAtCell4 = terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1 && chunkAtCell2 != null && chunkAtCell2.State >= TerrainChunkState.InvalidVertices1 && chunkAtCell3 != null && chunkAtCell3.State >= TerrainChunkState.InvalidVertices1 && chunkAtCell4 != null && chunkAtCell4.State >= TerrainChunkState.InvalidVertices1)
			{
				float f = p.X - (float)color2;
				float f2 = p.Y - (float)color3;
				float f3 = p.Z - (float)color4;
				float x2 = terrain.GetCellLightFast(color2, color3, color4);
				float x3 = terrain.GetCellLightFast(color2, color3, z);
				float x4 = terrain.GetCellLightFast(color2, num, color4);
				float x5 = terrain.GetCellLightFast(color2, num, z);
				float x6 = terrain.GetCellLightFast(x, color3, color4);
				float x7 = terrain.GetCellLightFast(x, color3, z);
				float x8 = terrain.GetCellLightFast(x, num, color4);
				float x9 = terrain.GetCellLightFast(x, num, z);
				float x10 = MathUtils.Lerp(x2, x6, f);
				float x11 = MathUtils.Lerp(x3, x7, f);
				float x12 = MathUtils.Lerp(x4, x8, f);
				float x13 = MathUtils.Lerp(x5, x9, f);
				float x14 = MathUtils.Lerp(x10, x12, f2);
				float x15 = MathUtils.Lerp(x11, x13, f2);
				float num2 = MathUtils.Lerp(x14, x15, f3);
				int num3 = (int)MathUtils.Floor(num2);
				int num4 = (int)MathUtils.Ceiling(num2);
				float f4 = num2 - (float)num3;
				return MathUtils.Lerp(LightIntensityByLightValue[num3], LightIntensityByLightValue[num4], f4);
			}
		}
		return null;
	}

	public static void CalculateLightingTables()
	{
		float x = ((!SettingsManager.NightVisionEnabled) ? MathUtils.Lerp(-0.1f, 0f, SettingsManager.Brightness) : MathUtils.Lerp(0.9f, 1f, SettingsManager.Brightness));
		for (int i = 0; i < 16; i++)
		{
			float num = MathUtils.Saturate(MathUtils.Lerp(x, 1f, MathUtils.Pow((float)i / 15f, 1.25f)));
			LightIntensityByLightValue[i] = num;
		}
		for (int j = 0; j < 6; j++)
		{
			float num2 = CalculateLighting(CellFace.FaceToVector3(j));
			for (int k = 0; k < 16; k++)
			{
				LightIntensityByLightValueAndFace[k + j * 16] = LightIntensityByLightValue[k] * num2;
			}
		}
	}

	static LightingManager()
	{
		LightAmbient = 0.5f;
		DirectionToLight1 = new Vector3(0.12f, 0.25f, 0.34f);
		DirectionToLight2 = new Vector3(-0.12f, 0.25f, -0.34f);
		LightIntensityByLightValue = new float[16];
		LightIntensityByLightValueAndFace = new float[96];
	}
}
