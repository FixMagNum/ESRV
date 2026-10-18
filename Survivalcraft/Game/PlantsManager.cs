using System;
using System.Collections.Generic;
using Engine;

namespace Game;

public static class PlantsManager
{
	public static List<TerrainBrush>[] m_treeBrushesByType;

	public static int[] m_treeTrunksByType;

	public static int[] m_treeLeavesByType;

	static PlantsManager()
	{
		m_treeBrushesByType = new List<TerrainBrush>[Enum.GetValues(typeof(TreeType)).Length];
		m_treeTrunksByType = new int[10] { 9, 10, 11, 11, 255, 9, 10, 11, 11, 255 };
		m_treeLeavesByType = new int[10] { 12, 13, 14, 225, 256, 12, 13, 14, 225, 256 };
		Random random = new Random(33);
		m_treeBrushesByType[0] = new List<TerrainBrush>();
		for (int i = 0; i < 16; i++)
		{
			int[] array = new int[16]
			{
				5, 6, 7, 7, 8, 8, 9, 9, 9, 10,
				10, 11, 12, 13, 14, 16
			};
			int height4 = array[i];
			int branchesCount = (int)MathUtils.Lerp(10f, 20f, (float)i / 16f);
			TerrainBrush item = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Oak), GetTreeLeavesValue(TreeType.Oak), height4, branchesCount, (int y) =>
			{
				float num10 = 0.4f;
				if ((float)y < 0.2f * (float)height4)
				{
					num10 = 0f;
				}
				else if ((float)y >= 0.2f * (float)height4 && y <= height4)
				{
					num10 *= 1.5f;
				}
				return num10;
			}, (int y) =>
			{
				if ((float)y < (float)height4 * 0.3f || (float)y > (float)height4 * 0.9f)
				{
					return 0f;
				}
				float num10 = (((float)y < (float)height4 * 0.7f) ? (0.5f * (float)height4) : (0.35f * (float)height4));
				return random.Float(0.33f, 1f) * num10;
			});
			m_treeBrushesByType[0].Add(item);
		}
		m_treeBrushesByType[1] = new List<TerrainBrush>();
		for (int num = 0; num < 16; num++)
		{
			int[] array2 = new int[16]
			{
				4, 5, 6, 6, 7, 7, 7, 8, 8, 8,
				9, 9, 9, 10, 10, 11
			};
			int height5 = array2[num];
			int branchesCount2 = (int)MathUtils.Lerp(0f, 20f, (float)num / 16f);
			TerrainBrush item2 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Birch), GetTreeLeavesValue(TreeType.Birch), height5, branchesCount2, (int y) =>
			{
				float num10 = 0.66f;
				if (y < height5 / 2 - 1)
				{
					num10 = 0f;
				}
				else if (y > height5 / 2 && y <= height5)
				{
					num10 *= 1.5f;
				}
				return num10;
			}, (int y) => ((float)y >= (float)height5 * 0.35f && (float)y <= (float)height5 * 0.75f) ? random.Float(0f, 0.33f * (float)height5) : 0f);
			m_treeBrushesByType[1].Add(item2);
		}
		m_treeBrushesByType[2] = new List<TerrainBrush>();
		for (int num2 = 0; num2 < 16; num2++)
		{
			int[] array3 = new int[16]
			{
				7, 8, 9, 10, 10, 11, 11, 12, 12, 13,
				13, 14, 14, 15, 16, 17
			};
			int height6 = array3[num2];
			int branchesCount3 = height6 * 3;
			TerrainBrush item3 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.Spruce), GetTreeLeavesValue(TreeType.Spruce), height6, branchesCount3, (int y) =>
			{
				float num10 = MathUtils.Lerp(1.4f, 0.3f, (float)y / (float)height6);
				if (y < 3)
				{
					num10 = 0f;
				}
				if (y % 2 == 0)
				{
					num10 *= 0.3f;
				}
				return num10;
			}, (int y) =>
			{
				if (y < 3 || (float)y > (float)height6 * 0.8f)
				{
					return 0f;
				}
				return (y % 2 != 0) ? MathUtils.Lerp(0.3f * (float)height6, 0f, MathUtils.Saturate((float)y / (float)height6)) : 0f;
			});
			m_treeBrushesByType[2].Add(item3);
		}
		m_treeBrushesByType[3] = new List<TerrainBrush>();
		for (int num3 = 0; num3 < 16; num3++)
		{
			int[] array4 = new int[18]
			{
				20, 21, 22, 23, 24, 24, 25, 25, 26, 26,
				27, 27, 28, 28, 29, 29, 30, 30
			};
			int height7 = array4[num3];
			int branchesCount4 = height7 * 3;
			float startHeight = (0.3f + (float)(num3 % 4) * 0.05f) * (float)height7;
			TerrainBrush item4 = CreateTreeBrush(random, GetTreeTrunkValue(TreeType.TallSpruce), GetTreeLeavesValue(TreeType.TallSpruce), height7, branchesCount4, (int y) =>
			{
				float num10 = MathUtils.Saturate((float)y / (float)height7);
				float num11 = MathUtils.Lerp(1.5f, 0f, MathUtils.Saturate((num10 - 0.6f) / 0.4f));
				if ((float)y < startHeight)
				{
					num11 = 0f;
				}
				if (y % 3 != 0 && y < height7 - 4)
				{
					num11 *= 0.2f;
				}
				return num11;
			}, (int y) =>
			{
				float num10 = MathUtils.Saturate((float)y / (float)height7);
				if (y % 3 != 0)
				{
					return 0f;
				}
				if ((float)y >= startHeight)
				{
					return MathUtils.Lerp(0.18f * (float)height7, 0f, MathUtils.Saturate((num10 - 0.6f) / 0.4f));
				}
				return ((float)y < startHeight - 4f) ? 0f : (0.1f * (float)height7);
			});
			m_treeBrushesByType[3].Add(item4);
		}
		m_treeBrushesByType[4] = new List<TerrainBrush>();
		for (int num4 = 0; num4 < 16; num4++)
		{
			m_treeBrushesByType[4].Add(CreateMimosaBrush(random, MathUtils.Lerp(6f, 9f, (float)num4 / 15f)));
		}
		m_treeBrushesByType[5] = new List<TerrainBrush>();
		for (int num5 = 0; num5 < 16; num5++)
		{
			int height8 = 12 + random.Int(0, 4);
			TerrainBrush item5 = CreateAncientOakBrush(random, GetTreeTrunkValue(TreeType.OldOak), GetTreeLeavesValue(TreeType.OldOak), height8);
			m_treeBrushesByType[5].Add(item5);
		}
		m_treeBrushesByType[6] = new List<TerrainBrush>();
		for (int num6 = 0; num6 < 16; num6++)
		{
			int height9 = 18 + random.Int(0, 4);
			TerrainBrush item6 = CreateAncientBirchBrush(random, GetTreeTrunkValue(TreeType.OldBirch), GetTreeLeavesValue(TreeType.OldBirch), height9);
			m_treeBrushesByType[6].Add(item6);
		}
		m_treeBrushesByType[7] = new List<TerrainBrush>();
		for (int num7 = 0; num7 < 16; num7++)
		{
			int height10 = 10 + random.Int(0, 3);
			TerrainBrush item7 = CreateAncientSmallSpruceBrush(random, GetTreeTrunkValue(TreeType.OldSpruce), GetTreeLeavesValue(TreeType.OldSpruce), height10);
			m_treeBrushesByType[7].Add(item7);
		}
		m_treeBrushesByType[8] = new List<TerrainBrush>();
		for (int num8 = 0; num8 < 16; num8++)
		{
			int height11 = 30 + random.Int(0, 5);
			TerrainBrush item8 = CreateAncientTallSpruceBrush(random, GetTreeTrunkValue(TreeType.OldTallSpruce), GetTreeLeavesValue(TreeType.OldTallSpruce), height11);
			m_treeBrushesByType[8].Add(item8);
		}
		m_treeBrushesByType[9] = new List<TerrainBrush>();
		for (int num9 = 0; num9 < 16; num9++)
		{
			int height12 = 20 + random.Int(0, 8);
			TerrainBrush item9 = CreateAncientMimosaBrush(random, GetTreeTrunkValue(TreeType.OldMimosa), GetTreeLeavesValue(TreeType.OldMimosa), height12);
			m_treeBrushesByType[9].Add(item9);
		}
	}

	public static int GetTreeTrunkValue(TreeType treeType)
	{
		return m_treeTrunksByType[(int)treeType];
	}

	public static int GetTreeLeavesValue(TreeType treeType)
	{
		return m_treeLeavesByType[(int)treeType];
	}

	public static ReadOnlyList<TerrainBrush> GetTreeBrushes(TreeType treeType)
	{
		return new ReadOnlyList<TerrainBrush>(m_treeBrushesByType[(int)treeType]);
	}

	public static int GenerateRandomPlantValue(Random random, int groundValue, int temperature, int humidity, int y)
	{
		return GenerateRandomPlantValue(random, groundValue, temperature, humidity, y, 0);
	}

	public static TreeType? GenerateRandomTreeType(Random random, int temperature, int humidity, int y, float densityMultiplier = 1f)
	{
		TreeType? result = null;
		float num = random.Float() * CalculateTreeProbability(TreeType.Oak, temperature, humidity, y);
		float num2 = random.Float() * CalculateTreeProbability(TreeType.Birch, temperature, humidity, y);
		float num3 = random.Float() * CalculateTreeProbability(TreeType.Spruce, temperature, humidity, y);
		float num4 = random.Float() * CalculateTreeProbability(TreeType.TallSpruce, temperature, humidity, y);
		float num5 = random.Float() * CalculateTreeProbability(TreeType.Mimosa, temperature, humidity, y);
		float num6 = random.Float() * CalculateTreeProbability(TreeType.OldOak, temperature, humidity, y);
		float num7 = random.Float() * CalculateTreeProbability(TreeType.OldBirch, temperature, humidity, y);
		float num8 = random.Float() * CalculateTreeProbability(TreeType.OldSpruce, temperature, humidity, y);
		float num9 = random.Float() * CalculateTreeProbability(TreeType.OldTallSpruce, temperature, humidity, y);
		float num10 = random.Float() * CalculateTreeProbability(TreeType.OldMimosa, temperature, humidity, y);
		float x = MathUtils.Max(num, num2, num3, num4);
		float x2 = MathUtils.Max(num5, num6, num7);
		float x3 = MathUtils.Max(num8, num9, num10);
		float num11 = MathUtils.Max(x, x2, x3);
		if (num11 > 0f)
		{
			if (num11 == num)
			{
				result = TreeType.Oak;
			}
			else if (num11 == num2)
			{
				result = TreeType.Birch;
			}
			else if (num11 == num3)
			{
				result = TreeType.Spruce;
			}
			else if (num11 == num4)
			{
				result = TreeType.TallSpruce;
			}
			else if (num11 == num5)
			{
				result = TreeType.Mimosa;
			}
			else if (num11 == num6)
			{
				result = TreeType.OldOak;
			}
			else if (num11 == num7)
			{
				result = TreeType.OldBirch;
			}
			else if (num11 == num8)
			{
				result = TreeType.OldSpruce;
			}
			else if (num11 == num9)
			{
				result = TreeType.OldTallSpruce;
			}
			else if (num11 == num10)
			{
				result = TreeType.OldMimosa;
			}
		}
		if (result.HasValue && random.Bool(densityMultiplier * CalculateTreeDensity(result.Value, temperature, humidity, y)))
		{
			return result;
		}
		return null;
	}

	public static float CalculateTreeDensity(TreeType treeType, int temperature, int humidity, int y)
	{
		return treeType switch
		{
			TreeType.Oak => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.Birch => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.Spruce => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.TallSpruce => RangeProbability(humidity, 4f, 15f, 15f, 15f), 
			TreeType.Mimosa => 0.03f, 
			TreeType.OldOak => 0.07f, 
			TreeType.OldBirch => 0.07f, 
			TreeType.OldSpruce => 0.07f, 
			TreeType.OldTallSpruce => 0.07f, 
			TreeType.OldMimosa => 0.008f, 
			_ => 0f, 
		};
	}

	public static float CalculateTreeProbability(TreeType treeType, int temperature, int humidity, int y)
	{
		return treeType switch
		{
			TreeType.Oak => RangeProbability(temperature, 4f, 10f, 15f, 15f) * RangeProbability(humidity, 6f, 8f, 15f, 15f) * RangeProbability(y, 0f, 0f, 82f, 87f), 
			TreeType.Birch => RangeProbability(temperature, 5f, 9f, 9f, 14f) * RangeProbability(humidity, 3f, 15f, 15f, 15f) * RangeProbability(y, 0f, 0f, 82f, 87f), 
			TreeType.Spruce => RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 3f, 10f, 11f, 12f), 
			TreeType.TallSpruce => 0.25f * RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 9f, 11f, 15f, 15f) * RangeProbability(y, 0f, 0f, 95f, 100f), 
			TreeType.Mimosa => RangeProbability(temperature, 2f, 4f, 12f, 14f) * RangeProbability(humidity, 0f, 0f, 4f, 6f), 
			TreeType.OldOak => 0.5f * RangeProbability(temperature, 4f, 10f, 15f, 15f) * RangeProbability(humidity, 5f, 10f, 10f, 15f), 
			TreeType.OldBirch => 0.5f * RangeProbability(temperature, 5f, 9f, 9f, 14f) * RangeProbability(humidity, 4f, 12f, 12f, 15f), 
			TreeType.OldSpruce => 0.5f * RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 5f, 10f, 10f, 12f), 
			TreeType.OldTallSpruce => 0.5f * RangeProbability(temperature, 0f, 0f, 6f, 10f) * RangeProbability(humidity, 9f, 11f, 15f, 15f) * RangeProbability(y, 0f, 0f, 90f, 100f), 
			TreeType.OldMimosa => 0.5f * RangeProbability(temperature, 2f, 4f, 12f, 14f) * RangeProbability(humidity, 0f, 0f, 4f, 6f), 
			_ => 0f, 
		};
	}

	public static float RangeProbability(float v, float a, float b, float c, float d)
	{
		if (v < a)
		{
			return 0f;
		}
		if (v < b)
		{
			return (v - a) / (b - a);
		}
		if (v <= c)
		{
			return 1f;
		}
		if (v <= d)
		{
			return 1f - (v - c) / (d - c);
		}
		return 0f;
	}

	public static TerrainBrush CreateTreeBrush(Random random, int woodIndex, int leavesIndex, int height, int branchesCount, Func<int, float> leavesProbabilityByHeight, Func<int, float> branchesLengthByHeight)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		terrainBrush.AddRay(0, -1, 0, 0, height, 0, 1, 1, 1, woodIndex);
		for (int i = 0; i < branchesCount; i++)
		{
			int num = random.Int(0, height);
			float num2 = branchesLengthByHeight(num);
			Vector3 vector = Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(0f, 0.33f), random.Float(-1f, 1f))) * num2;
			int x = (int)MathUtils.Round(vector.X);
			int y = num + (int)MathUtils.Round(vector.Y);
			int z = (int)MathUtils.Round(vector.Z);
			terrainBrush.AddRay(0, num, 0, x, y, z, 1, 1, 1, woodIndex);
		}
		Func<int?, int> func = (int? value) => (value.HasValue && Terrain.ExtractContents(value.Value) == woodIndex) ? 1 : 0;
		for (int num3 = 0; num3 < 3; num3++)
		{
			terrainBrush.CalculateBounds(out var min, out var max);
			for (int num4 = min.X - 1; num4 <= max.X + 1; num4++)
			{
				for (int num5 = min.Z - 1; num5 <= max.Z + 1; num5++)
				{
					for (int num6 = 1; num6 <= max.Y + 1; num6++)
					{
						float num7 = leavesProbabilityByHeight(num6);
						if (random.Float(0f, 1f) < num7 && !terrainBrush.GetValue(num4, num6, num5).HasValue)
						{
							if (terrainBrush.CountNonDiagonalNeighbors(num4, num6, num5, leavesIndex) > 0)
							{
								terrainBrush.AddCell(num4, num6, num5, 0);
							}
							else if (terrainBrush.CountNonDiagonalNeighbors(num4, num6, num5, func) > 0)
							{
								terrainBrush.AddCell(num4, num6, num5, 0);
							}
						}
					}
				}
			}
			terrainBrush.Replace(0, leavesIndex);
		}
		terrainBrush.AddCell(0, height, 0, leavesIndex);
		AddTreeBark(terrainBrush, random, woodIndex, GetBarkWoodType(woodIndex), 0, 3, height / 2);
		terrainBrush.Compile();
		return terrainBrush;
	}

	public static TerrainBrush CreateMimosaBrush(Random random, float size)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		int num = m_treeTrunksByType[4];
		int num2 = m_treeLeavesByType[4];
		terrainBrush.AddRay(0, -1, 0, 0, 0, 0, 1, 1, 1, num);
		List<Point3> list = new List<Point3>();
		float num3 = random.Float(0f, (float)Math.PI * 2f);
		for (int i = 0; i < 3; i++)
		{
			float radians = num3 + (float)i * MathUtils.DegToRad(120f);
			Vector3 vector = Vector3.Transform(Vector3.Normalize(new Vector3(1f, random.Float(1f, 1.5f), 0f)), Matrix.CreateRotationY(radians));
			int num4 = random.Int((int)(0.7f * size), (int)size);
			Point3 p = new Point3(0, 0, 0);
			Point3 item = new Point3(Vector3.Round(new Vector3(p) + vector * num4));
			terrainBrush.AddRay(p.X, p.Y, p.Z, item.X, item.Y, item.Z, 1, 1, 1, num);
			list.Add(item);
		}
		foreach (Point3 item2 in list)
		{
			float num5 = random.Float(0.3f * size, 0.45f * size);
			int num6 = (int)MathUtils.Ceiling(num5);
			for (int j = item2.X - num6; j <= item2.X + num6; j++)
			{
				for (int k = item2.Y - num6; k <= item2.Y + num6; k++)
				{
					for (int l = item2.Z - num6; l <= item2.Z + num6; l++)
					{
						int num7 = MathUtils.Abs(j - item2.X) + MathUtils.Abs(k - item2.Y) + MathUtils.Abs(l - item2.Z);
						float num8 = ((new Vector3(j, k, l) - new Vector3(item2)) * new Vector3(1f, 1.7f, 1f)).Length();
						if (num8 <= num5 && (num5 - num8 > 1f || num7 <= 2 || random.Bool(0.95f)) && !terrainBrush.GetValue(j, k, l).HasValue)
						{
							terrainBrush.AddCell(j, k, l, num2);
						}
					}
				}
			}
		}
		AddTreeBark(terrainBrush, random, num, GetBarkWoodType(num), 0, 3, (int)(size / 2f));
		terrainBrush.Compile();
		return terrainBrush;
	}

	public static TerrainBrush CreateAncientOakBrush(Random random, int woodIndex, int leavesIndex, int height)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		bool flag = random.Bool(0.1f);
		if (flag)
		{
			height *= 2;
		}
		bool flag2 = random.Bool(0.5f);
		int num = (flag ? 3 : 2);
		for (int i = 0; i < height; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					if (flag2)
					{
						if (Math.Abs(j) + Math.Abs(k) <= num)
						{
							terrainBrush.AddCell(j, i, k, woodIndex);
						}
					}
					else if (Math.Abs(j) != num || Math.Abs(k) != num)
					{
						terrainBrush.AddCell(j, i, k, woodIndex);
					}
				}
			}
		}
		int num2 = num + ((!flag) ? 1 : 3);
		int num3 = (flag ? 5 : 3);
		for (int l = 0; l < num3; l++)
		{
			for (int m = -num2; m <= num2; m++)
			{
				for (int n = -num2; n <= num2; n++)
				{
					if (Math.Sqrt(m * m + n * n) <= (double)(num2 - l))
					{
						terrainBrush.AddCell(m, l - 1, n, woodIndex);
					}
				}
			}
		}
		int num4 = (flag ? random.Int(2, 4) : random.Int(8, 12));
		for (int num5 = 0; num5 < num4; num5++)
		{
			int num6 = random.Int(height / 2, height - 2);
			Vector3 vector = Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(0.3f, 0.7f), random.Float(-1f, 1f)));
			int num7 = random.Int(4, 6);
			terrainBrush.AddRay(0, num6, 0, (int)(vector.X * (float)num7), num6 + (int)(vector.Y * (float)num7), (int)(vector.Z * (float)num7), 1, 1, 1, woodIndex);
			int num8 = (flag ? random.Int(4, 6) : random.Int(2, 4));
			for (int num9 = -num8; num9 <= num8; num9++)
			{
				for (int num10 = -num8; num10 <= num8; num10++)
				{
					for (int num11 = -num8; num11 <= num8; num11++)
					{
						if (Math.Sqrt(num9 * num9 + num10 * num10 + num11 * num11) <= (double)num8 && random.Bool(0.75f))
						{
							terrainBrush.AddCell((int)(vector.X * (float)num7) + num9, num6 + (int)(vector.Y * (float)num7) + num10, (int)(vector.Z * (float)num7) + num11, leavesIndex);
						}
					}
				}
			}
		}
		AddTreeBark(terrainBrush, random, woodIndex, GetBarkWoodType(woodIndex), 3, 10, height);
		terrainBrush.Compile();
		return terrainBrush;
	}

	public static TerrainBrush CreateAncientBirchBrush(Random random, int woodIndex, int leavesIndex, int height)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		int num = 1;
		for (int i = 0; i < height; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					if (Math.Sqrt(j * j + k * k) <= (double)num)
					{
						terrainBrush.AddCell(j, i, k, woodIndex);
					}
				}
			}
		}
		int num2 = random.Int(8, 12);
		for (int l = 0; l < num2; l++)
		{
			int num3 = random.Int(height / 2, height - 3);
			Vector3 vector = Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(0.2f, 0.6f), random.Float(-1f, 1f)));
			int num4 = random.Int(3, 5);
			terrainBrush.AddRay(0, num3, 0, (int)(vector.X * (float)num4), num3 + (int)(vector.Y * (float)num4), (int)(vector.Z * (float)num4), 1, 1, 1, woodIndex);
			int num5 = random.Int(2, 3);
			for (int m = -num5; m <= num5; m++)
			{
				for (int n = -num5; n <= num5; n++)
				{
					for (int num6 = -num5; num6 <= num5; num6++)
					{
						if (Math.Sqrt(m * m + n * n + num6 * num6) <= (double)num5 && random.Bool(0.7f))
						{
							terrainBrush.AddCell((int)(vector.X * (float)num4) + m, num3 + (int)(vector.Y * (float)num4) + n, (int)(vector.Z * (float)num4) + num6, leavesIndex);
						}
					}
				}
			}
		}
		int num7 = random.Int(2, 3);
		for (int num8 = height - num7; num8 <= height; num8++)
		{
			int num9 = random.Int(2, 3);
			for (int num10 = -num9; num10 <= num9; num10++)
			{
				for (int num11 = -num9; num11 <= num9; num11++)
				{
					if (Math.Sqrt(num10 * num10 + num11 * num11) <= (double)num9 && random.Bool(0.8f))
					{
						terrainBrush.AddCell(num10, num8, num11, leavesIndex);
					}
				}
			}
		}
		for (int num12 = -1; num12 <= 1; num12++)
		{
			for (int num13 = -1; num13 <= 1; num13++)
			{
				if (!terrainBrush.GetValue(num12, -1, num13).HasValue)
				{
					terrainBrush.AddCell(num12, -1, num13, woodIndex);
				}
			}
		}
		AddTreeBark(terrainBrush, random, woodIndex, GetBarkWoodType(woodIndex), 3, 10, height);
		terrainBrush.Compile();
		return terrainBrush;
	}

	public static TerrainBrush CreateAncientSmallSpruceBrush(Random random, int woodIndex, int leavesIndex, int height)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		for (int i = 0; i < height; i++)
		{
			terrainBrush.AddCell(0, i, 0, woodIndex);
		}
		int num = 1;
		for (int j = num; j < height; j++)
		{
			float num2 = ((j - num >= height / 3) ? MathUtils.Lerp(3f, 1f, (float)(j - height / 3) / (float)(height - height / 3)) : MathUtils.Lerp(5f, 3f, (float)(j - num) / (float)(height / 3)));
			int num3 = Math.Max(1, (int)Math.Ceiling(num2));
			for (int k = -num3; k <= num3; k++)
			{
				for (int l = -num3; l <= num3; l++)
				{
					float num4 = (float)Math.Sqrt(k * k + l * l);
					if (num4 <= num2 + random.Float(-0.2f, 0.2f) && random.Bool(0.9f) && (j == height - 1 || num4 < num2 - 0.5f))
					{
						terrainBrush.AddCell(k, j, l, leavesIndex);
					}
				}
			}
		}
		for (int m = -1; m <= 1; m++)
		{
			for (int n = -1; n <= 1; n++)
			{
				if (!terrainBrush.GetValue(m, -1, n).HasValue)
				{
					terrainBrush.AddCell(m, -1, n, woodIndex);
				}
			}
		}
		AddTreeBark(terrainBrush, random, woodIndex, GetBarkWoodType(woodIndex), 2, 6, height);
		terrainBrush.Compile();
		return terrainBrush;
	}

	public static TerrainBrush CreateAncientTallSpruceBrush(Random random, int woodIndex, int leavesIndex, int height)
	{
		TerrainBrush subsystemGameInfo = new TerrainBrush();
		int num = 2;
		subsystemGameInfo.AddBox(-1, 0, -1, num, height, num, woodIndex);
		for (int i = height / 4; i < height; i++)
		{
			float num2 = MathUtils.Lerp(5f, 1f, (float)(i - height / 4) / (float)(height - height / 4)) + random.Float(-1f, 1f);
			for (int j = -(int)num2; j <= (int)num2; j++)
			{
				for (int k = -(int)num2; k <= (int)num2; k++)
				{
					if (Math.Sqrt(j * j + k * k) <= (double)num2 && random.Bool(0.8f))
					{
						subsystemGameInfo.AddCell(j, i, k, leavesIndex);
					}
				}
			}
		}
		int num3 = random.Int(2, 3);
		for (int l = height; l < height + num3; l++)
		{
			float num4 = MathUtils.Lerp(2f, 0f, (float)(l - height) / (float)num3) + random.Float(-0.5f, 0.5f);
			for (int m = -(int)num4; m <= (int)num4; m++)
			{
				for (int n = -(int)num4; n <= (int)num4; n++)
				{
					if (Math.Sqrt(m * m + n * n) <= (double)num4 && random.Bool(0.7f))
					{
						subsystemGameInfo.AddCell(m, l, n, leavesIndex);
					}
				}
			}
		}
		for (int num5 = -1; num5 <= 0; num5++)
		{
			for (int num6 = -1; num6 <= 0; num6++)
			{
				if (!subsystemGameInfo.GetValue(num5, -1, num6).HasValue)
				{
					subsystemGameInfo.AddCell(num5, -1, num6, woodIndex);
				}
			}
		}
		AddTreeBark(subsystemGameInfo, random, woodIndex, GetBarkWoodType(woodIndex), 4, 12, height);
		subsystemGameInfo.Compile();
		return subsystemGameInfo;
	}

	public static int GenerateRandomPlantValue(Random random, int groundValue, int temperature, int humidity, int y, int snowTemperatureThreshold)
	{
		switch (Terrain.ExtractContents(groundValue))
		{
		default:
			return 0;
		case 7:
			if (humidity >= 8 || random.Float(0f, 1f) >= 0.01f)
			{
				return 0;
			}
			if (random.Float(0f, 1f) < 0.05f)
			{
				return Terrain.MakeBlockValue(99, 0, 0);
			}
			return Terrain.MakeBlockValue(28, 0, 0);
		case 2:
		case 8:
		case 356:
			if (humidity >= 6)
			{
				if (random.Float(0f, 1f) < (float)humidity / 60f)
				{
					int result = Terrain.MakeBlockValue(19, 0, TallGrassBlock.SetIsSmall(0, isSmall: false));
					if (!SubsystemWeather.IsPlaceFrozenExtended(temperature, y, snowTemperatureThreshold))
					{
						float num = random.Float(0f, 1f);
						if (num < 0.04f)
						{
							result = Terrain.MakeBlockValue(20);
						}
						else if (num < 0.07f)
						{
							result = Terrain.MakeBlockValue(24);
						}
						else if (num < 0.09f)
						{
							result = Terrain.MakeBlockValue(25);
						}
						else if (num < 0.17f)
						{
							result = Terrain.MakeBlockValue(174, 0, RyeBlock.SetIsWild(RyeBlock.SetSize(0, 7), isWild: true));
						}
						else if (num < 0.19f)
						{
							result = Terrain.MakeBlockValue(204, 0, CottonBlock.SetIsWild(CottonBlock.SetSize(0, 2), isWild: true));
						}
					}
					return result;
				}
			}
			else if (random.Float(0f, 1f) < 0.025f)
			{
				if (random.Float(0f, 1f) < 0.2f)
				{
					return Terrain.MakeBlockValue(99, 0, 0);
				}
				return Terrain.MakeBlockValue(28, 0, 0);
			}
			if (random.Float(0f, 1f) >= 0.7f)
			{
				return 0;
			}
			if (temperature < 2)
			{
				return 0;
			}
			return Terrain.MakeBlockValue(19, 0, TallGrassBlock.SetIsSmall(0, isSmall: true));
		}
	}

	public static TerrainBrush CreateAncientMimosaBrush(Random random, int woodIndex, int leavesIndex, int height)
	{
		TerrainBrush terrainBrush = new TerrainBrush();
		int num = height + random.Int(-3, 11);
		float num2 = random.Float(0.1f, 0.3f);
		int num3 = (int)((float)num * random.Float(0.35f, 0.65f));
		for (int i = -1; i <= num3; i++)
		{
			int num4 = ((!((float)i < (float)num * num2)) ? 1 : 2);
			for (int j = -num4; j <= num4; j++)
			{
				for (int k = -num4; k <= num4; k++)
				{
					if (Math.Sqrt(j * j + k * k) <= (double)((float)num4 + 0.2f))
					{
						terrainBrush.AddCell(j, i, k, woodIndex);
					}
				}
			}
		}
		List<Point3> list = new List<Point3>();
		bool flag = random.Bool(0.2f);
		if (flag)
		{
			terrainBrush.AddRay(0, num3, 0, 0, num, 0, 1, 1, 1, woodIndex);
			list.Add(new Point3(0, num, 0));
		}
		else
		{
			int num5 = random.Int(3, 11);
			for (int l = 0; l < num5; l++)
			{
				int num6 = num3 - random.Int(0, 4);
				float num7 = random.Float(0f, 6.283185f);
				Vector3 vector = Vector3.Normalize(new Vector3((float)Math.Cos(num7), random.Float(0.8f, 2f), (float)Math.Sin(num7)));
				int num8 = random.Int(num / 3, (int)((float)num * 0.8f));
				Point3 item = new Point3((int)(vector.X * (float)num8), num6 + (int)(vector.Y * (float)num8), (int)(vector.Z * (float)num8));
				terrainBrush.AddRay(0, num6, 0, item.X, item.Y, item.Z, 1, 1, 1, woodIndex);
				list.Add(item);
			}
		}
		foreach (Point3 item2 in list)
		{
			int num9 = (flag ? random.Int(10, 14) : random.Int(5, 9));
			int num10 = random.Int(1, 3);
			for (int m = item2.X - num9; m <= item2.X + num9; m++)
			{
				for (int n = item2.Y - num10; n <= item2.Y + num10; n++)
				{
					for (int num11 = item2.Z - num9; num11 <= item2.Z + num9; num11++)
					{
						float num12 = (float)(m - item2.X) / (float)num9;
						float num13 = (float)(n - item2.Y) / (float)num10;
						float num14 = (float)(num11 - item2.Z) / (float)num9;
						if (num12 * num12 + num13 * num13 + num14 * num14 <= 1f && random.Bool(0.95f) && !terrainBrush.GetValue(m, n, num11).HasValue)
						{
							terrainBrush.AddCell(m, n, num11, leavesIndex);
						}
					}
				}
			}
		}
		AddTreeBark(terrainBrush, random, woodIndex, GetBarkWoodType(woodIndex), 3, 10, height);
		terrainBrush.Compile();
		return terrainBrush;
	}

	private static int GetBarkWoodType(int woodIndex)
	{
		switch (woodIndex)
		{
		case 10:
			return 1;
		case 11:
		case 255:
			return 2;
		default:
			return 0;
		}
	}

	private static void AddTreeBark(TerrainBrush brush, Random random, int woodIndex, int woodType, int minPieces, int maxPieces, int maxHeight)
	{
		int num = random.Int(minPieces, maxPieces);
		if (num <= 0)
		{
			return;
		}
		List<KeyValuePair<Point3, int>> list = new List<KeyValuePair<Point3, int>>();
		brush.CalculateBounds(out var min, out var max);
		for (int i = Math.Max(1, min.Y); i <= Math.Min(max.Y, maxHeight); i++)
		{
			for (int j = min.X; j <= max.X; j++)
			{
				for (int k = min.Z; k <= max.Z; k++)
				{
					int? value = brush.GetValue(j, i, k);
					if (value.HasValue && Terrain.ExtractContents(value.Value) == woodIndex)
					{
						if (!brush.GetValue(j, i, k - 1).HasValue)
						{
							list.Add(new KeyValuePair<Point3, int>(new Point3(j, i, k - 1), 2));
						}
						if (!brush.GetValue(j - 1, i, k).HasValue)
						{
							list.Add(new KeyValuePair<Point3, int>(new Point3(j - 1, i, k), 3));
						}
						if (!brush.GetValue(j, i, k + 1).HasValue)
						{
							list.Add(new KeyValuePair<Point3, int>(new Point3(j, i, k + 1), 0));
						}
						if (!brush.GetValue(j + 1, i, k).HasValue)
						{
							list.Add(new KeyValuePair<Point3, int>(new Point3(j + 1, i, k), 1));
						}
					}
				}
			}
		}
		int num2 = 0;
		while (list.Count > 0 && num2 < num)
		{
			int index = random.Int(0, list.Count - 1);
			KeyValuePair<Point3, int> keyValuePair = list[index];
			list.RemoveAt(index);
			Point3 key = keyValuePair.Key;
			int value2 = keyValuePair.Value;
			if (!brush.GetValue(key.X, key.Y, key.Z).HasValue && key.Y > 0)
			{
				int num3 = random.Int(0, 2);
				int data = (value2 & 7) | ((num3 & 3) << 3) | ((woodType & 0x1F) << 5);
				brush.AddCell(key.X, key.Y, key.Z, Terrain.MakeBlockValue(443, 0, data));
				num2++;
			}
		}
	}
}
