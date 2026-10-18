using System;
using System.Collections.Generic;
using Engine;

namespace Game;

public static class EllihumGenerations
{
	public static Dictionary<string, List<TerrainBrush>> m_generatorsByType;

	static EllihumGenerations()
	{
		m_generatorsByType = new Dictionary<string, List<TerrainBrush>>();
		m_generatorsByType["DesertBoulders"] = CreateDesertBouldersBrushes();
	}

	public static ReadOnlyList<TerrainBrush> GetGeneratorBrushes(string generatorType)
	{
		if (m_generatorsByType.ContainsKey(generatorType))
		{
			return new ReadOnlyList<TerrainBrush>(m_generatorsByType[generatorType]);
		}
		return new ReadOnlyList<TerrainBrush>(new List<TerrainBrush>());
	}

	private static List<TerrainBrush> CreateDesertBouldersBrushes()
	{
		List<TerrainBrush> list = new List<TerrainBrush>();
		Random random = new Random(12345);
		for (int i = 0; i < 5; i++)
		{
			TerrainBrush terrainBrush = new TerrainBrush();
			int num = random.Int(2, 4);
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					for (int l = -num; l <= num; l++)
					{
						if (j * j + k * k + l * l <= num * num)
						{
							int num2 = Terrain.MakeBlockValue(7);
							terrainBrush.AddCell(j, k, l, num2);
						}
					}
				}
			}
			terrainBrush.Compile();
			list.Add(terrainBrush);
		}
		return list;
	}

	private static List<TerrainBrush> CreateRuinedHousesBrushes()
	{
		List<TerrainBrush> result = new List<TerrainBrush>();
		new Random(67890);
		return result;
	}

	public static void GenerateDesertBoulders(Terrain terrain, int seed, int chunkX, int chunkY)
	{
		Random random = new Random(seed + chunkX + 3943 * chunkY);
		bool flag = random.Bool(0.01f);
		if (!random.Bool(0.04f) && !flag)
		{
			return;
		}
		int num = chunkX * 16 + random.Int(0, 15);
		int num2 = chunkY * 16 + random.Int(0, 15);
		int num3 = terrain.CalculateTopmostCellHeight(num, num2);
		if (terrain.GetCellContentsFast(num, num3, num2) != 7)
		{
			return;
		}
		int num4 = random.Int(4, 5) + (flag ? 2 : 0);
		int contents = (random.Bool(0.5f) ? 4 : 7);
		for (int i = 0; i < num4; i++)
		{
			if (terrain.GetCellContentsFast(num, num3 + i, num2) == 0)
			{
				terrain.SetCellValueFast(num, num3 + i, num2, Terrain.MakeBlockValue(contents));
			}
		}
		int num5 = 5;
		for (int j = 0; j < num5; j++)
		{
			int num6 = random.Int(2, 3);
			int num7 = random.Int(-2, 2);
			int num8 = random.Int(-2, 2);
			if (!random.Bool(0.8f) && Math.Abs(num7) <= 1 && Math.Abs(num8) <= 1)
			{
				continue;
			}
			for (int k = 0; k < num6; k++)
			{
				if (terrain.GetCellContentsFast(num + num7, num3 + k, num2 + num8) == 0)
				{
					terrain.SetCellValueFast(num + num7, num3 + k, num2 + num8, Terrain.MakeBlockValue(contents));
				}
			}
		}
		int num9 = 10;
		for (int l = 0; l < num9; l++)
		{
			int x = num + random.Int(-3, 3);
			int z = num2 + random.Int(-3, 3);
			int num10 = random.Int(1, 2);
			for (int m = 0; m < num10; m++)
			{
				if (terrain.GetCellContentsFast(x, num3 + m, z) == 0)
				{
					terrain.SetCellValueFast(x, num3 + m, z, Terrain.MakeBlockValue(contents));
				}
			}
		}
		if (!(random.Float(0f, 1f) < 0.4f))
		{
			return;
		}
		int num11 = ((!flag) ? 1 : 2);
		for (int n = 0; n < num11; n++)
		{
			int x2 = num + random.Int(-1, 1);
			int y = num3 + random.Int(0, num4 - 1);
			int z2 = num2 + random.Int(-1, 1);
			if (random.Bool(0.5f))
			{
				y = num3 - 1;
			}
			if (terrain.GetCellContentsFast(x2, y, z2) == 0)
			{
				terrain.SetCellValueFast(x2, y, z2, Terrain.MakeBlockValue(100));
			}
		}
	}
}
