using Engine;

namespace Game;

public class BlockColorsMap
{
	public Color[] m_map = new Color[256];

	public static BlockColorsMap WaterColorsMap;

	public static BlockColorsMap GrassColorsMap;

	public static BlockColorsMap OakLeavesColorsMap;

	public static BlockColorsMap BirchLeavesColorsMap;

	public static BlockColorsMap SpruceLeavesColorsMap;

	public static BlockColorsMap TallSpruceLeavesColorsMap;

	public static BlockColorsMap MimosaLeavesColorsMap;

	public static BlockColorsMap IvyColorsMap;

	public static BlockColorsMap KelpColorsMap;

	public static BlockColorsMap SeagrassColorsMap;

	public static BlockColorsMap SandColorsMap;

	public BlockColorsMap(Color th11, Color th21, Color th12, Color th22)
	{
		for (int timestamp = 0; timestamp < 16; timestamp++)
		{
			for (int i = 0; i < 16; i++)
			{
				float f = MathUtils.Saturate((float)timestamp / 8f);
				float f2 = MathUtils.Saturate((float)(i - 4) / 10f);
				Color c = Color.Lerp(th11, th21, f);
				Color c2 = Color.Lerp(th12, th22, f);
				Color color = Color.Lerp(c, c2, f2);
				int num = timestamp + i * 16;
				m_map[num] = color;
			}
		}
	}

	public Color Lookup(int temperature, int humidity)
	{
		int num = MathUtils.Clamp(temperature, 0, 15) + 16 * MathUtils.Clamp(humidity, 0, 15);
		return m_map[num];
	}

	public Color Lookup(Terrain terrain, int x, int y, int z)
	{
		int shaftValue = terrain.GetShaftValue(x, z);
		int num = terrain.GetSeasonalTemperature(shaftValue) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
		int seasonalHumidity = terrain.GetSeasonalHumidity(shaftValue);
		float num2 = MathUtils.Saturate((float)num / 15f);
		float num3 = MathUtils.Saturate((float)seasonalHumidity / 15f);
		float f = MathUtils.Lerp(0f, 1f, (1f - num3) * num2);
		Color c = Lookup(num, seasonalHumidity);
		Color c2 = new Color(204, 153, 51);
		return Color.Lerp(c, c2, f);
	}

	static BlockColorsMap()
	{
		SandColorsMap = new BlockColorsMap(new Color(255, 240, 220), new Color(255, 240, 200), new Color(255, 240, 200), new Color(255, 140, 80));
		WaterColorsMap = new BlockColorsMap(new Color(0, 27, 70), new Color(0, 50, 90), new Color(0, 60, 85), new Color(0, 113, 97));
		GrassColorsMap = new BlockColorsMap(new Color(141, 198, 166), new Color(210, 201, 93), new Color(141, 198, 166), new Color(79, 225, 56));
		OakLeavesColorsMap = new BlockColorsMap(new Color(96, 161, 123), new Color(174, 164, 42), new Color(96, 161, 123), new Color(30, 191, 1));
		BirchLeavesColorsMap = new BlockColorsMap(new Color(96, 161, 96), new Color(174, 109, 42), new Color(96, 161, 96), new Color(107, 191, 1));
		SpruceLeavesColorsMap = new BlockColorsMap(new Color(96, 161, 150), new Color(129, 174, 42), new Color(96, 161, 150), new Color(1, 191, 53));
		TallSpruceLeavesColorsMap = new BlockColorsMap(new Color(90, 141, 160), new Color(119, 152, 51), new Color(86, 141, 162), new Color(1, 158, 65));
		MimosaLeavesColorsMap = new BlockColorsMap(new Color(146, 191, 176), new Color(160, 191, 176), new Color(146, 191, 166), new Color(150, 201, 141));
		IvyColorsMap = new BlockColorsMap(new Color(96, 161, 123), new Color(174, 164, 42), new Color(96, 161, 123), new Color(30, 191, 1));
		KelpColorsMap = new BlockColorsMap(new Color(80, 110, 90), new Color(110, 110, 50), new Color(80, 110, 90), new Color(110, 110, 50));
		SeagrassColorsMap = new BlockColorsMap(new Color(50, 120, 110), new Color(80, 120, 70), new Color(50, 120, 110), new Color(80, 120, 70));
	}

	public Color LookupSand(Terrain terrain, int x, int y, int z)
	{
		int shaftValue = terrain.GetShaftValue(x, z);
		int temperature = terrain.GetSeasonalTemperature(shaftValue) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
		int seasonalHumidity = terrain.GetSeasonalHumidity(shaftValue);
		return LookupSand(temperature, seasonalHumidity);
	}

	public Color LookupSand(int temperature, int humidity)
	{
		Color currentSeason = new Color(255, 230, 180);
		Color color = new Color(255, 240, 210);
		Color c = new Color(255, 240, 200);
		if (temperature >= 12 && humidity <= 4)
		{
			return currentSeason;
		}
		if (temperature <= 4 || (temperature >= 12 && humidity >= 8))
		{
			return color;
		}
		if (temperature < 8 && humidity > 4)
		{
			float f = MathUtils.Saturate((float)(8 - temperature) / 8f);
			return Color.Lerp(c, color, f);
		}
		float f2 = MathUtils.Saturate((float)(temperature - humidity) / 15f);
		return Color.Lerp(c, currentSeason, f2);
	}

	public Color LookupSeasonalLeavesColor(SubsystemWeather.Season season, Color baseColor, int blockIndex)
	{
		if (blockIndex == 14 || blockIndex == 225)
		{
			return baseColor;
		}
		switch (season)
		{
		case SubsystemWeather.Season.EarlyAutumn:
			return Color.Lerp(baseColor, new Color(255, 220, 50), 0.75f);
		case SubsystemWeather.Season.Autumn:
			return Color.Lerp(baseColor, new Color(204, 102, 51), 0.6f);
		case SubsystemWeather.Season.LateAutumn:
			return Color.Lerp(baseColor, new Color(255, 120, 50), 0.8f);
		case SubsystemWeather.Season.Anabiosis:
			if (blockIndex == 13)
			{
				return Color.Transparent;
			}
			return Color.Lerp(baseColor, new Color(240, 240, 240), 0.9f);
		case SubsystemWeather.Season.Winter:
			if (blockIndex == 12 || blockIndex == 13)
			{
				return Color.Transparent;
			}
			return Color.Lerp(baseColor, new Color(240, 240, 240), 0.9f);
		case SubsystemWeather.Season.LateWinter:
			if (blockIndex == 12 || blockIndex == 13)
			{
				return Color.Transparent;
			}
			return Color.Lerp(baseColor, new Color(240, 240, 240), 0.9f);
		case SubsystemWeather.Season.EarlySpring:
			if (blockIndex == 12)
			{
				return Color.Transparent;
			}
			return Color.Lerp(baseColor, new Color(255, 182, 193), 0.3f);
		case SubsystemWeather.Season.Spring:
			return Color.Lerp(baseColor, new Color(255, 182, 193), 0.5f);
		case SubsystemWeather.Season.LateSpring:
			return Color.Lerp(baseColor, new Color(122, 255, 110), 0.7f);
		case SubsystemWeather.Season.Summer:
			return baseColor;
		default:
			return baseColor;
		}
	}

	public Color LookupSeasonalGrassColor(SubsystemWeather.Season season, Terrain terrain, int x, int y, int z)
	{
		Color num = Lookup(terrain, x, y, z);
		return season switch
		{
			SubsystemWeather.Season.EarlyAutumn => Color.Lerp(num, new Color(153, 102, 51), 0.5f), 
			SubsystemWeather.Season.Autumn => Color.Lerp(num, new Color(128, 64, 48), 0.7f), 
			SubsystemWeather.Season.LateAutumn => Color.Lerp(num, new Color(118, 64, 48), 0.8f), 
			SubsystemWeather.Season.Anabiosis => Color.Lerp(num, new Color(88, 64, 48), 0.7f), 
			SubsystemWeather.Season.Winter => Color.Lerp(num, new Color(88, 64, 48), 0.9f), 
			SubsystemWeather.Season.LateWinter => Color.Lerp(num, new Color(88, 64, 48), 0.65f), 
			SubsystemWeather.Season.EarlySpring => Color.Lerp(num, new Color(88, 64, 48), 0.4f), 
			SubsystemWeather.Season.Spring => Color.Lerp(num, new Color(88, 64, 48), 0.3f), 
			SubsystemWeather.Season.LateSpring => Color.Lerp(num, new Color(122, 255, 110), 0.5f), 
			SubsystemWeather.Season.Summer => num, 
			_ => num, 
		};
	}

	public Color LookupSeasonalWaterColor(SubsystemWeather.Season season, Color baseColor)
	{
		return season switch
		{
			SubsystemWeather.Season.EarlyAutumn => Color.Lerp(baseColor, new Color(0, 20, 20), 0.3f), 
			SubsystemWeather.Season.Autumn => Color.Lerp(baseColor, new Color(0, 20, 20), 0.5f), 
			SubsystemWeather.Season.LateAutumn => Color.Lerp(baseColor, new Color(0, 20, 20), 0.75f), 
			SubsystemWeather.Season.Anabiosis => Color.Lerp(baseColor, new Color(0, 20, 20), 0.85f), 
			SubsystemWeather.Season.Winter => Color.Lerp(baseColor, new Color(0, 20, 20), 0.95f), 
			SubsystemWeather.Season.LateWinter => Color.Lerp(baseColor, new Color(0, 20, 20), 0.75f), 
			SubsystemWeather.Season.EarlySpring => Color.Lerp(baseColor, new Color(0, 20, 20), 0.5f), 
			SubsystemWeather.Season.Spring => Color.Lerp(baseColor, new Color(0, 20, 20), 0.3f), 
			SubsystemWeather.Season.LateSpring => Color.Lerp(baseColor, new Color(0, 120, 90), 0.25f), 
			_ => baseColor, 
		};
	}

	public Color LookupSeasonalDirtColor(SubsystemWeather.Season season, Terrain terrain, int x, int y, int z)
	{
		Color white = Color.White;
		return season switch
		{
			SubsystemWeather.Season.EarlyAutumn => Color.Lerp(white, new Color(101, 67, 33), 0.4f), 
			SubsystemWeather.Season.Autumn => Color.Lerp(white, new Color(85, 50, 25), 0.4f), 
			SubsystemWeather.Season.LateAutumn => Color.Lerp(white, new Color(85, 50, 25), 0.6f), 
			SubsystemWeather.Season.Anabiosis => Color.Lerp(white, new Color(125, 120, 125), 0.7f), 
			SubsystemWeather.Season.Winter => Color.Lerp(white, new Color(180, 200, 220), 0.8f), 
			SubsystemWeather.Season.LateWinter => Color.Lerp(white, new Color(180, 200, 220), 0.6f), 
			SubsystemWeather.Season.EarlySpring => Color.Lerp(white, new Color(140, 100, 60), 0.3f), 
			SubsystemWeather.Season.Spring => Color.Lerp(white, new Color(140, 100, 60), 0.2f), 
			SubsystemWeather.Season.LateSpring => Color.Lerp(white, new Color(140, 100, 60), 0.1f), 
			SubsystemWeather.Season.Summer => white, 
			_ => white, 
		};
	}
}
