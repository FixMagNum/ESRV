using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public abstract class SubsystemFluidBlockBehavior : SubsystemBlockBehavior
{
	public static Point2[] m_sideNeighbors;

	public FluidBlock m_fluidBlock;

	public Dictionary<Point3, bool> m_toUpdate = new Dictionary<Point3, bool>();

	public Dictionary<Point3, int> m_toSet = new Dictionary<Point3, int>();

	public Dictionary<Point3, int> m_visited = new Dictionary<Point3, int>();

	public Dictionary<Point3, Vector2> m_fluidRandomFlowDirections = new Dictionary<Point3, Vector2>();

	public bool m_generateSources;

	private bool m_flowUpwards;

	public SubsystemTime SubsystemTime { get; set; }

	public SubsystemAudio SubsystemAudio { get; set; }

	public SubsystemAmbientSounds SubsystemAmbientSounds { get; set; }

	public SubsystemFluidBlockBehavior(FluidBlock fluidBlock, bool generateSources)
	{
		m_fluidBlock = fluidBlock;
		m_generateSources = generateSources;
	}

	public void UpdateIsTop(int value, int x, int y, int z)
	{
		Terrain terrain = SubsystemTerrain.Terrain;
		if (y < 255)
		{
			TerrainChunk chunkAtCell = terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null)
			{
				int num = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
				int contents = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num + 1));
				int data = Terrain.ExtractData(value);
				bool isTop = !m_fluidBlock.IsTheSameFluid(contents);
				chunkAtCell.SetCellValueFast(num, Terrain.ReplaceData(value, FluidBlock.SetIsTop(data, isTop)));
			}
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		UpdateIsTop(value, x, y, z);
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		UpdateIsTop(value, x, y, z);
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		m_toUpdate[new Point3
		{
			X = x,
			Y = y,
			Z = z
		}] = true;
		if (neighborY == y + 1)
		{
			UpdateIsTop(SubsystemTerrain.Terrain.GetCellValueFast(x, y, z), x, y, z);
		}
	}

	public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue)
	{
		newBlockValue = Terrain.MakeBlockValue(m_fluidBlock.BlockIndex);
		dropValue.Value = 0;
		dropValue.Count = 0;
	}

	public float? GetSurfaceHeight(int x, int y, int z, out FluidBlock surfaceFluidBlock)
	{
		if (y >= 0 && y < 255)
		{
			TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null)
			{
				int num = TerrainChunk.CalculateCellIndex(x & 0xF, 0, z & 0xF);
				while (y < 255)
				{
					int num2 = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num + y + 1));
					if (BlocksManager.FluidBlocks[num2] == null)
					{
						int cellValueFast = chunkAtCell.GetCellValueFast(num + y);
						int num3 = Terrain.ExtractContents(cellValueFast);
						FluidBlock fluidBlock = BlocksManager.FluidBlocks[num3];
						if (fluidBlock != null)
						{
							surfaceFluidBlock = fluidBlock;
							int level = FluidBlock.GetLevel(Terrain.ExtractData(cellValueFast));
							return (float)y + surfaceFluidBlock.GetLevelHeight(level);
						}
						surfaceFluidBlock = null;
						return null;
					}
					y++;
				}
			}
		}
		surfaceFluidBlock = null;
		return null;
	}

	public float? GetSurfaceHeight(int x, int y, int z)
	{
		FluidBlock surfaceFluidBlock;
		return GetSurfaceHeight(x, y, z, out surfaceFluidBlock);
	}

	public Vector2? CalculateFlowSpeed(int x, int y, int z, out FluidBlock surfaceBlock, out float? surfaceHeight)
	{
		surfaceHeight = GetSurfaceHeight(x, y, z, out surfaceBlock);
		if (surfaceHeight.HasValue)
		{
			y = (int)surfaceHeight.Value;
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			if (BlocksManager.Blocks[num] is FluidBlock)
			{
				int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x - 1, y, z);
				int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x + 1, y, z);
				int cellValue4 = SubsystemTerrain.Terrain.GetCellValue(x, y, z - 1);
				int cellValue5 = SubsystemTerrain.Terrain.GetCellValue(x, y, z + 1);
				int num2 = Terrain.ExtractContents(cellValue2);
				int num3 = Terrain.ExtractContents(cellValue3);
				int num4 = Terrain.ExtractContents(cellValue4);
				int num5 = Terrain.ExtractContents(cellValue5);
				int level = FluidBlock.GetLevel(Terrain.ExtractData(cellValue));
				int num6 = ((num2 == num) ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue2)) : level);
				int num7 = ((num3 == num) ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue3)) : level);
				int num8 = ((num4 == num) ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue4)) : level);
				int num9 = ((num5 == num) ? FluidBlock.GetLevel(Terrain.ExtractData(cellValue5)) : level);
				Vector2 vector = new Vector2
				{
					X = MathUtils.Sign(level - num6) - MathUtils.Sign(level - num7),
					Y = MathUtils.Sign(level - num8) - MathUtils.Sign(level - num9)
				};
				if (vector.LengthSquared() > 1f)
				{
					vector = Vector2.Normalize(vector);
				}
				if (!m_fluidRandomFlowDirections.TryGetValue(new Point3(x, y, z), out var value))
				{
					value.X = 0.05f * (2f * SimplexNoise.OctavedNoise((float)x + 0.2f * (float)SubsystemTime.GameTime, z, 0.1f, 1, 1f, 1f) - 1f);
					value.Y = 0.05f * (2f * SimplexNoise.OctavedNoise((float)x + 0.2f * (float)SubsystemTime.GameTime + 100f, z, 0.1f, 1, 1f, 1f) - 1f);
					if (m_fluidRandomFlowDirections.Count < 1000)
					{
						m_fluidRandomFlowDirections[new Point3(x, y, z)] = value;
					}
					else
					{
						m_fluidRandomFlowDirections.Clear();
					}
				}
				vector += value;
				return vector * 2f;
			}
		}
		return null;
	}

	public Vector2? CalculateFlowSpeed(int x, int y, int z)
	{
		FluidBlock surfaceBlock;
		float? surfaceHeight;
		return CalculateFlowSpeed(x, y, z, out surfaceBlock, out surfaceHeight);
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		SubsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		SubsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		SubsystemGameInfo saveFileDialog = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		if (saveFileDialog != null && saveFileDialog.WorldSettings.GameMode == GameMode.Embrace)
		{
			m_flowUpwards = true;
		}
		else
		{
			m_flowUpwards = false;
		}
	}

	public void SpreadFluid()
	{
		for (int i = 0; i < 2; i++)
		{
			int num = (m_flowUpwards ? 1 : (-1));
			foreach (Point3 key in m_toUpdate.Keys)
			{
				int x = key.X;
				int y = key.Y;
				int z = key.Z;
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
				int contents = Terrain.ExtractContents(cellValue);
				int num2 = Terrain.ExtractData(cellValue);
				int level = FluidBlock.GetLevel(num2 & 0x1F);
				if (!m_fluidBlock.IsTheSameFluid(contents))
				{
					continue;
				}
				int y2 = y + num;
				int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y2, z);
				int num3 = Terrain.ExtractContents(cellValue2);
				if (num3 == 0)
				{
					Set(x, y2, z, cellValue);
					Set(x, y, z, 0);
					continue;
				}
				m_fluidBlock.IsTheSameFluid(num3);
				int y3 = y - num;
				int data = Terrain.ExtractData(cellValue2);
				int level2 = FluidBlock.GetLevel(data);
				int num4 = m_fluidBlock.MaxLevel + 1;
				int num5 = 0;
				for (int j = 0; j < 4; j++)
				{
					Point2 point = m_sideNeighbors[j];
					int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y, z + point.Y);
					if (m_fluidBlock.IsTheSameFluid(Terrain.ExtractContents(cellValue3)))
					{
						int level3 = FluidBlock.GetLevel(Terrain.ExtractData(cellValue3));
						num4 = MathUtils.Min(num4, level3);
						if (level3 == 0)
						{
							num5++;
						}
					}
				}
				if (level != 0 && level <= num4)
				{
					int contents2 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y3, z));
					if (!m_fluidBlock.IsTheSameFluid(contents2))
					{
						if (num4 + 1 > m_fluidBlock.MaxLevel)
						{
							Set(x, y, z, 0);
							continue;
						}
						int data2 = FluidBlock.SetLevel(num2, num4 + 1);
						Set(x, y, z, Terrain.MakeBlockValue(contents, 0, data2));
						continue;
					}
				}
				if (m_generateSources && level != 0 && num5 >= 2)
				{
					int data3 = FluidBlock.SetLevel(num2, 0);
					Set(x, y, z, Terrain.MakeBlockValue(contents, 0, data3));
				}
				else if (m_fluidBlock.IsTheSameFluid(num3))
				{
					if (level2 > 1)
					{
						int data4 = FluidBlock.SetLevel(data, 1);
						Set(x, y2, z, Terrain.MakeBlockValue(num3, 0, data4));
					}
				}
				else if (!OnFluidInteract(cellValue2, x, y2, z, Terrain.MakeBlockValue(m_fluidBlock.BlockIndex, 0, FluidBlock.SetLevel(num2, 1))) && level < m_fluidBlock.MaxLevel)
				{
					m_visited.Clear();
					int num6 = LevelAtNearestFall(x + 1, y, z, level + 1, m_visited);
					int num7 = LevelAtNearestFall(x - 1, y, z, level + 1, m_visited);
					int num8 = LevelAtNearestFall(x, y, z + 1, level + 1, m_visited);
					int num9 = LevelAtNearestFall(x, y, z - 1, level + 1, m_visited);
					int num10 = MathUtils.Min(num6, num7, num8, num9);
					if (num6 == num10)
					{
						FlowTo(x + 1, y, z, level + 1, num2);
						FlowTo(x, y, z - 1, m_fluidBlock.MaxLevel, num2);
						FlowTo(x, y, z + 1, m_fluidBlock.MaxLevel, num2);
					}
					if (num7 == num10)
					{
						FlowTo(x - 1, y, z, level + 1, num2);
						FlowTo(x, y, z - 1, m_fluidBlock.MaxLevel, num2);
						FlowTo(x, y, z + 1, m_fluidBlock.MaxLevel, num2);
					}
					if (num8 == num10)
					{
						FlowTo(x, y, z + 1, level + 1, num2);
						FlowTo(x - 1, y, z, m_fluidBlock.MaxLevel, num2);
						FlowTo(x + 1, y, z, m_fluidBlock.MaxLevel, num2);
					}
					if (num9 == num10)
					{
						FlowTo(x, y, z - 1, level + 1, num2);
						FlowTo(x - 1, y, z, m_fluidBlock.MaxLevel, num2);
						FlowTo(x + 1, y, z, m_fluidBlock.MaxLevel, num2);
					}
				}
			}
			m_toUpdate.Clear();
			ApplyChangesToTerrain();
		}
	}

	public virtual bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue)
	{
		if (!BlocksManager.Blocks[Terrain.ExtractContents(interactValue)].IsFluidBlocker)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			Set(x, y, z, fluidValue);
			return true;
		}
		return false;
	}

	public float? CalculateDistanceToFluid(Vector3 p, int radius, bool flowingFluidOnly)
	{
		float num = float.MaxValue;
		Terrain num2 = SubsystemTerrain.Terrain;
		int num3 = Terrain.ToCell(p.X) - radius;
		int num4 = Terrain.ToCell(p.X) + radius;
		int num5 = MathUtils.Clamp(Terrain.ToCell(p.Y) - radius, 0, 254);
		int num6 = MathUtils.Clamp(Terrain.ToCell(p.Y) + radius, 0, 254);
		int num7 = Terrain.ToCell(p.Z) - radius;
		int num8 = Terrain.ToCell(p.Z) + radius;
		for (int direction = num7; direction <= num8; direction++)
		{
			for (int i = num3; i <= num4; i++)
			{
				TerrainChunk chunkAtCell = num2.GetChunkAtCell(i, direction);
				if (chunkAtCell == null)
				{
					continue;
				}
				int j = TerrainChunk.CalculateCellIndex(i & 0xF, num5, direction & 0xF);
				for (int k = num5; k <= num6; k++, j++)
				{
					int cellValueFast = chunkAtCell.GetCellValueFast(j);
					int contents = Terrain.ExtractContents(cellValueFast);
					if (!m_fluidBlock.IsTheSameFluid(contents))
					{
						continue;
					}
					if (flowingFluidOnly)
					{
						if (FluidBlock.GetLevel(Terrain.ExtractData(cellValueFast)) == 0)
						{
							continue;
						}
						int contents2 = Terrain.ExtractContents(chunkAtCell.GetCellValueFast(j + 1));
						if (m_fluidBlock.IsTheSameFluid(contents2))
						{
							continue;
						}
					}
					float num9 = p.X - ((float)i + 0.5f);
					float num10 = p.Y - ((float)k + 1f);
					float num11 = p.Z - ((float)direction + 0.5f);
					float num12 = num9 * num9 + num10 * num10 + num11 * num11;
					if (num12 < num)
					{
						num = num12;
					}
				}
			}
		}
		if (num == float.MaxValue)
		{
			return null;
		}
		return MathUtils.Sqrt(num);
	}

	public void Set(int x, int y, int z, int value)
	{
		Point3 key = new Point3(x, y, z);
		if (!m_toSet.ContainsKey(key))
		{
			m_toSet[key] = value;
		}
	}

	public int LevelAtNearestFall(int x, int y, int z, int level, Dictionary<Point3, int> levels)
	{
		if (level > m_fluidBlock.MaxLevel)
		{
			return int.MaxValue;
		}
		if (!levels.TryGetValue(new Point3(x, y, z), out var value))
		{
			value = int.MaxValue;
		}
		if (level >= value)
		{
			return int.MaxValue;
		}
		levels[new Point3(x, y, z)] = level;
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		if (m_fluidBlock.IsTheSameFluid(num))
		{
			if (FluidBlock.GetLevel(Terrain.ExtractData(cellValue)) < level)
			{
				return int.MaxValue;
			}
		}
		else if (BlocksManager.Blocks[num].IsFluidBlocker)
		{
			return int.MaxValue;
		}
		int num2 = (m_flowUpwards ? 1 : (-1));
		int y2 = y + num2;
		int num3 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y2, z));
		Block block = BlocksManager.Blocks[num3];
		if (m_fluidBlock.IsTheSameFluid(num3) || !block.IsFluidBlocker)
		{
			return level;
		}
		int x2 = LevelAtNearestFall(x - 1, y, z, level + 1, levels);
		int x3 = LevelAtNearestFall(x + 1, y, z, level + 1, levels);
		int x4 = LevelAtNearestFall(x, y, z - 1, level + 1, levels);
		int x5 = LevelAtNearestFall(x, y, z + 1, level + 1, levels);
		return MathUtils.Min(x2, x3, x4, x5);
	}

	static SubsystemFluidBlockBehavior()
	{
		m_sideNeighbors = new Point2[4]
		{
			new Point2(-1, 0),
			new Point2(1, 0),
			new Point2(0, -1),
			new Point2(0, 1)
		};
	}

	private int GetTopFilledY(Terrain terrain, int x, int z)
	{
		for (int num = 255; num >= 0; num--)
		{
			if (Terrain.ExtractContents(terrain.GetCellValueFast(x, num, z)) != 0)
			{
				return num;
			}
		}
		return -1;
	}

	private bool CheckSurroundingFluidBlocks(int x, int y, int z)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						int num = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x + i, y + j, z + k));
						if (!m_fluidBlock.IsTheSameFluid(num) && num != 0)
						{
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	private void ApplyChangesToTerrain()
	{
		foreach (KeyValuePair<Point3, int> item in m_toSet)
		{
			SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
		}
		m_toSet.Clear();
		SubsystemTerrain.ProcessModifiedCells();
	}

	public void FlowTo(int x, int y, int z, int level, int sourceData)
	{
		if (level > m_fluidBlock.MaxLevel)
		{
			return;
		}
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int contents = Terrain.ExtractContents(cellValue);
		int data = Terrain.ExtractData(cellValue);
		if (m_fluidBlock.IsTheSameFluid(contents))
		{
			int level2 = FluidBlock.GetLevel(data);
			if (level < level2)
			{
				Set(x, y, z, Terrain.MakeBlockValue(contents, 0, FluidBlock.SetLevel(data, level)));
			}
		}
		else
		{
			int data2 = FluidBlock.SetLevel(sourceData, level);
			data2 = FluidBlock.SetIsTop(data2, isTop: false);
			OnFluidInteract(cellValue, x, y, z, Terrain.MakeBlockValue(m_fluidBlock.BlockIndex, 0, data2));
		}
	}
}
