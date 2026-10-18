using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemCaveInstability : SubsystemBlockBehavior
{
	public const string IdString = "CaveInstability";

	public static int[] m_handledBlocks;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemMovingBlocks m_subsystemMovingBlocks;

	public HashSet<Point3> m_internalRemovals;

	public List<int> m_tmpIndices;

	public float m_collapseChanceLower;

	public float m_collapseChanceUpper;

	public float m_upperVerticalMultiplier;

	public int m_maxBlocksPerEvent;

	public Random m_random;

	public HashSet<Point3> m_playerBreakMarks = new HashSet<Point3>();

	public List<Point3> m_bottomCandidates;

	public List<Point3> m_layerCells;

	public List<Point3> m_baseCluster;

	public List<Point3> m_upBuffer;

	public HashSet<Point3> m_toRemove;

	public SubsystemAudio m_subsystemAudio;

	public float m_heavyCollapseChance;

	public float m_heavyUpperVerticalMultiplier;

	public int m_heavyFootprintRadius;

	public float m_heavyLayerTakeBoost;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemWeather m_subsystemWeather;

	public List<Point3> m_supportBlocks;

	private SubsystemGameInfo m_subsystemGameInfo;

	public override int[] HandledBlocks => m_handledBlocks;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		m_subsystemMovingBlocks.CollidedWithTerrain += MovingBlocksCollidedWithTerrain;
		m_subsystemMovingBlocks.Stopped += MovingBlocksStopped;
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_collapseChanceLower = valuesDictionary.GetValue("CollapseChanceLower", 0.34f);
		m_collapseChanceUpper = valuesDictionary.GetValue("CollapseChanceUpper", 0.18f);
		m_upperVerticalMultiplier = valuesDictionary.GetValue("UpperVerticalMultiplier", 0.6f);
		m_maxBlocksPerEvent = valuesDictionary.GetValue("MaxBlocksPerEvent", 256);
		m_heavyCollapseChance = valuesDictionary.GetValue("HeavyCollapseChance", 0.12f);
		m_heavyUpperVerticalMultiplier = valuesDictionary.GetValue("HeavyUpperVerticalMultiplier", 0.85f);
		m_heavyFootprintRadius = valuesDictionary.GetValue("HeavyFootprintRadius", 6);
		m_heavyLayerTakeBoost = valuesDictionary.GetValue("HeavyLayerTakeBoost", 1.25f);
	}

	public override void Dispose()
	{
		if (m_subsystemMovingBlocks != null)
		{
			m_subsystemMovingBlocks.CollidedWithTerrain -= MovingBlocksCollidedWithTerrain;
			m_subsystemMovingBlocks.Stopped -= MovingBlocksStopped;
		}
		base.Dispose();
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		base.OnBlockRemoved(value, newValue, x, y, z);
		int value2 = Terrain.ExtractContents(value);
		if (Array.IndexOf(HandledBlocks, value2) >= 0)
		{
			Point3 point = new Point3(x, y, z);
			if (!m_internalRemovals.Remove(point) && m_playerBreakMarks.Remove(point))
			{
				TrySectionCollapse(point);
			}
		}
	}

	public void TrySectionCollapse(Point3 removedCell)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		bool flag = m_random.Float(0f, 1f) < m_heavyCollapseChance;
		float num = (flag ? MathUtils.Saturate(m_collapseChanceLower + 0.25f) : m_collapseChanceLower);
		int num2 = (flag ? m_heavyFootprintRadius : 3);
		int num3 = (flag ? 6 : 3);
		int num4 = (flag ? 6 : 2);
		Point3 secMin = new Point3(removedCell.X - num2, MathUtils.Clamp(removedCell.Y - num3, 0, 255), removedCell.Z - num2);
		Point3 secMax = new Point3(removedCell.X + (num2 - 1), MathUtils.Clamp(removedCell.Y + num4, 0, 255), removedCell.Z + (num2 - 1));
		m_bottomCandidates.Clear();
		m_layerCells.Clear();
		m_baseCluster.Clear();
		m_upBuffer.Clear();
		m_toRemove.Clear();
		m_supportBlocks.Clear();
		Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
		for (int i = secMin.Y; i <= secMax.Y; i++)
		{
			for (int j = secMin.Z; j <= secMax.Z; j++)
			{
				for (int k = secMin.X; k <= secMax.X; k++)
				{
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(k, i, j);
					int num5 = Terrain.ExtractContents(cellValue);
					if (Array.IndexOf(HandledBlocks, num5) >= 0)
					{
						int num6 = Terrain.ExtractContents((i > 0) ? m_subsystemTerrain.Terrain.GetCellValue(k, i - 1, j) : 0);
						if (num6 == 0 || num6 == 18)
						{
							m_bottomCandidates.Add(new Point3(k, i, j));
						}
					}
					if (num5 == 0)
					{
						continue;
					}
					if (!dictionary.TryGetValue(num5, out var value))
					{
						value = false;
						Block block = BlocksManager.Blocks[num5];
						if (block.IsCollidable)
						{
							string soundMaterialName = block.GetSoundMaterialName(m_subsystemTerrain, cellValue);
							if (!string.IsNullOrEmpty(soundMaterialName) && soundMaterialName == "Wood")
							{
								value = true;
							}
						}
						dictionary[num5] = value;
					}
					if (value)
					{
						m_supportBlocks.Add(new Point3(k, i, j));
					}
				}
			}
		}
		if (m_bottomCandidates.Count == 0 || m_random.Float(0f, 1f) >= num)
		{
			return;
		}
		Point3 seed = m_bottomCandidates[m_random.Int(0, m_bottomCandidates.Count - 1)];
		int y = seed.Y;
		for (int l = secMin.Z; l <= secMax.Z; l++)
		{
			for (int m = secMin.X; m <= secMax.X; m++)
			{
				int value2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(m, y, l));
				if (Array.IndexOf(HandledBlocks, value2) >= 0)
				{
					int num7 = Terrain.ExtractContents((y > 0) ? m_subsystemTerrain.Terrain.GetCellValue(m, y - 1, l) : 0);
					if (num7 == 0 || num7 == 18)
					{
						m_layerCells.Add(new Point3(m, y, l));
					}
				}
			}
		}
		if (m_layerCells.Count == 0)
		{
			return;
		}
		if (flag)
		{
			for (int n = 0; n < m_layerCells.Count; n++)
			{
				m_baseCluster.Add(m_layerCells[n]);
				if (m_baseCluster.Count >= m_maxBlocksPerEvent)
				{
					break;
				}
			}
		}
		else
		{
			int num8 = MathUtils.Clamp(flag ? 12 : 3, 1, m_layerCells.Count);
			int num9 = MathUtils.Clamp(flag ? 48 : 12, num8, m_layerCells.Count);
			int need = BiasToHigh(num8, num9);
			FloodFillClusterXZ(seed, y, secMin, secMax, need, m_baseCluster);
			if (m_baseCluster.Count < MathUtils.Min(num8, m_layerCells.Count))
			{
				HashSet<Point3> hashSet = new HashSet<Point3>(m_baseCluster);
				for (int num10 = 0; num10 < m_layerCells.Count; num10++)
				{
					if (m_baseCluster.Count >= MathUtils.Min(num8, num9))
					{
						break;
					}
					Point3 item = m_layerCells[m_random.Int(0, m_layerCells.Count - 1)];
					if (!hashSet.Contains(item))
					{
						m_baseCluster.Add(item);
						hashSet.Add(item);
					}
				}
			}
		}
		for (int num11 = 0; num11 < m_baseCluster.Count; num11++)
		{
			m_toRemove.Add(m_baseCluster[num11]);
			if (m_toRemove.Count >= m_maxBlocksPerEvent)
			{
				break;
			}
		}
		if (m_toRemove.Count == 0)
		{
			return;
		}
		float num12 = 1f;
		for (int num13 = 1; num13 < 16; num13++)
		{
			if (m_toRemove.Count >= m_maxBlocksPerEvent)
			{
				break;
			}
			int num14 = y + num13;
			if (num14 > secMax.Y)
			{
				break;
			}
			num12 *= (flag ? m_heavyUpperVerticalMultiplier : m_upperVerticalMultiplier);
			if (m_random.Float(0f, 1f) >= num12)
			{
				break;
			}
			m_upBuffer.Clear();
			for (int num15 = 0; num15 < m_baseCluster.Count; num15++)
			{
				Point3 point = m_baseCluster[num15];
				Point3 item2 = new Point3(point.X, num14, point.Z);
				int value3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(item2.X, item2.Y, item2.Z));
				if (Array.IndexOf(HandledBlocks, value3) >= 0)
				{
					m_upBuffer.Add(item2);
				}
			}
			if (m_upBuffer.Count == 0)
			{
				break;
			}
			float num16 = (flag ? m_heavyLayerTakeBoost : 1f);
			int num17 = MathUtils.Clamp((int)((float)m_baseCluster.Count * num12 * num16), 1, m_upBuffer.Count);
			int num18 = MathUtils.Clamp(m_random.Int(MathUtils.Max(1, num17 - 2), MathUtils.Max(1, num17 + 1)), 1, m_upBuffer.Count);
			m_tmpIndices.Clear();
			for (int num19 = 0; num19 < m_upBuffer.Count; num19++)
			{
				m_tmpIndices.Add(num19);
			}
			for (int num20 = 0; num20 < num18; num20++)
			{
				if (m_tmpIndices.Count <= 0)
				{
					break;
				}
				if (m_toRemove.Count >= m_maxBlocksPerEvent)
				{
					break;
				}
				int index = m_random.Int(0, m_tmpIndices.Count - 1);
				int index2 = m_tmpIndices[index];
				m_tmpIndices.RemoveAt(index);
				m_toRemove.Add(m_upBuffer[index2]);
			}
		}
		if (m_toRemove.Count == 0)
		{
			return;
		}
		List<Point3> list = new List<Point3>(m_toRemove);
		for (int num21 = 0; num21 < list.Count; num21++)
		{
			Point3 point2 = list[num21];
			for (int num22 = point2.Y + 1; num22 <= 255; num22++)
			{
				int num23 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, num22, point2.Z));
				if (num23 != 6 && num23 != 7 && num23 != 376 && num23 != 377)
				{
					break;
				}
				m_toRemove.Add(new Point3(point2.X, num22, point2.Z));
				if (m_toRemove.Count >= m_maxBlocksPerEvent)
				{
					break;
				}
			}
			if (m_toRemove.Count >= m_maxBlocksPerEvent)
			{
				break;
			}
		}
		HashSet<Point3> hashSet2 = new HashSet<Point3>(m_toRemove);
		List<Point3> list2 = new List<Point3>(hashSet2.Count);
		int num24 = m_heavyFootprintRadius * 2;
		int num25 = 6;
		foreach (Point3 item5 in m_toRemove)
		{
			bool flag2 = false;
			if (m_supportBlocks.Count > 0)
			{
				for (int num26 = 0; num26 < m_supportBlocks.Count; num26++)
				{
					Point3 point3 = m_supportBlocks[num26];
					int num27 = item5.X - point3.X;
					if (num27 < -num24 || num27 > num24)
					{
						continue;
					}
					int num28 = item5.Z - point3.Z;
					if (num28 >= -num24 && num28 <= num24)
					{
						int num29 = item5.Y - point3.Y;
						if (num29 >= -num25 && num29 <= num25)
						{
							flag2 = true;
							break;
						}
					}
				}
			}
			if (!flag2)
			{
				Point3 item3 = new Point3(item5.X, item5.Y - 1, item5.Z);
				int num30 = Terrain.ExtractContents((item5.Y > 0) ? m_subsystemTerrain.Terrain.GetCellValue(item3.X, item3.Y, item3.Z) : 0);
				if (num30 == 0 || num30 == 18 || hashSet2.Contains(item3))
				{
					list2.Add(item5);
				}
			}
		}
		if (list2.Count == 0)
		{
			return;
		}
		Point3 tightAnchor = GetTightAnchor(new HashSet<Point3>(list2));
		List<MovingBlock> list3 = new List<MovingBlock>(list2.Count);
		for (int num31 = 0; num31 < list2.Count; num31++)
		{
			Point3 point4 = list2[num31];
			int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(point4.X, point4.Y, point4.Z);
			int num32 = Terrain.ExtractContents(cellValue2);
			if (Array.IndexOf(HandledBlocks, num32) >= 0 || num32 == 6 || num32 == 7 || num32 == 376 || num32 == 377)
			{
				list3.Add(new MovingBlock
				{
					Value = cellValue2,
					Offset = new Point3(point4.X - tightAnchor.X, point4.Y - tightAnchor.Y, point4.Z - tightAnchor.Z)
				});
			}
		}
		if (list3.Count == 0)
		{
			return;
		}
		Vector3 position = new Vector3(tightAnchor);
		Vector3 targetPosition = new Vector3(tightAnchor.X, (float)tightAnchor.Y - 48f, tightAnchor.Z);
		if (m_subsystemMovingBlocks.AddMovingBlockSet(position, targetPosition, 0f, 10f, 0.08f, new Vector2(0f, 0f), list3, "CaveInstability", null, testCollision: true) != null)
		{
			int num33 = 24;
			string directory = ((list3.Count >= num33) ? "Audio/CaveCollapse/CaveCollapseHeavy" : "Audio/CaveCollapse/CaveCollapseLight");
			if (m_subsystemAudio != null)
			{
				float pitch = MathUtils.Clamp(1f + m_random.Float(-0.3f, 0.3f), 0.5f, 1.5f);
				m_subsystemAudio.PlayRandomSound(directory, 1f, pitch, new Vector3(removedCell), 24f, 0.2f);
			}
			for (int num34 = 0; num34 < list2.Count; num34++)
			{
				Point3 item4 = list2[num34];
				m_internalRemovals.Add(item4);
				m_subsystemTerrain.ChangeCell(item4.X, item4.Y, item4.Z, 0);
			}
		}
	}

	public void MovingBlocksCollidedWithTerrain(IMovingBlockSet movingBlockSet, Point3 p)
	{
		if (movingBlockSet.Id != "CaveInstability")
		{
			return;
		}
		Point3 point = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.X), MathUtils.Round(movingBlockSet.Position.Y), MathUtils.Round(movingBlockSet.Position.Z));
		bool flag = false;
		ReadOnlyList<MovingBlock> blocks = movingBlockSet.Blocks;
		for (int i = 0; i < blocks.Count; i++)
		{
			Point3 point2 = new Point3(point.X + blocks[i].Offset.X, point.Y + blocks[i].Offset.Y - 1, point.Z + blocks[i].Offset.Z);
			if (point2.X == p.X && point2.Y == p.Y && point2.Z == p.Z)
			{
				flag = true;
				break;
			}
		}
		m_subsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
		if (flag)
		{
			movingBlockSet.Stop();
		}
	}

	public void MovingBlocksStopped(IMovingBlockSet movingBlockSet)
	{
		if (movingBlockSet.Id != "CaveInstability")
		{
			return;
		}
		if (CommonLib.WorkType == WorkType.Client && m_subsystemAudio != null)
		{
			int count = movingBlockSet.Blocks.Count;
			int num = 24;
			string directory = ((count >= num) ? "Audio/CaveCollapse/CaveCollapseHeavy" : "Audio/CaveCollapse/CaveCollapseLight");
			float pitch = MathUtils.Clamp(1f + m_random.Float(-0.3f, 0.3f), 0.5f, 1.5f);
			m_subsystemAudio.PlayRandomSound(directory, 1f, pitch, movingBlockSet.Position, 24f, 0.2f);
		}
		Point3 point = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.X), MathUtils.Round(movingBlockSet.Position.Y), MathUtils.Round(movingBlockSet.Position.Z));
		ReadOnlyList<MovingBlock> blocks = movingBlockSet.Blocks;
		for (int i = 0; i < blocks.Count; i++)
		{
			Point3 point2 = new Point3(point.X + blocks[i].Offset.X, point.Y + blocks[i].Offset.Y, point.Z + blocks[i].Offset.Z);
			int num2 = -1;
			int num3 = FindRestY(point2.X, MathUtils.Clamp(point2.Y, 0, 255), point2.Z);
			bool flag = false;
			if (num3 >= 0 && num3 <= 255)
			{
				int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, num3, point2.Z));
				if (num4 == 0 || num4 == 18)
				{
					m_subsystemTerrain.ChangeCell(point2.X, num3, point2.Z, blocks[i].Value);
					flag = true;
					num2 = num3;
				}
			}
			if (!flag)
			{
				int num5 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, point2.Y, point2.Z));
				if (num5 == 0 || num5 == 18)
				{
					m_subsystemTerrain.ChangeCell(point2.X, point2.Y, point2.Z, blocks[i].Value);
					flag = true;
					num2 = point2.Y;
				}
			}
			if (!flag)
			{
				int startY = MathUtils.Min(point2.Y + 8, 255);
				int num6 = FindRestY(point2.X, startY, point2.Z);
				if (num6 >= 0 && num6 <= 255)
				{
					int num7 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, num6, point2.Z));
					if (num7 == 0 || num7 == 18)
					{
						m_subsystemTerrain.ChangeCell(point2.X, num6, point2.Z, blocks[i].Value);
						flag = true;
						num2 = num6;
					}
				}
			}
			if (!flag)
			{
				int num8 = FindRestY(point2.X, 255, point2.Z);
				if (num8 >= 0 && num8 <= 255)
				{
					int num9 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, num8, point2.Z));
					if (num9 == 0 || num9 == 18)
					{
						m_subsystemTerrain.ChangeCell(point2.X, num8, point2.Z, blocks[i].Value);
						flag = true;
						num2 = num8;
					}
				}
			}
			if (flag && m_subsystemParticles != null)
			{
				int num10 = Terrain.ExtractContents(blocks[i].Value);
				Block block = BlocksManager.Blocks[num10];
				if (num10 == 2 || num10 == 3 || block is SoilBlock)
				{
					Vector3 spawnInsideBlock = new Vector3((float)point2.X + 0.5f, (float)num2 + 0.5f, (float)point2.Z + 0.5f);
					m_subsystemParticles.AddParticleSystem(new CaveDustBurstParticleSystem(m_subsystemTerrain, m_subsystemWeather, spawnInsideBlock));
				}
			}
		}
		m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet);
	}

	public bool IsSupportBlock(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return false;
		}
		Block block = BlocksManager.Blocks[num];
		if (block is TrapdoorBlock)
		{
			int data = Terrain.ExtractData(value);
			if (TrapdoorBlock.GetUpsideDown(data))
			{
				return !TrapdoorBlock.GetOpen(data);
			}
			return false;
		}
		if (!(block is FluidBlock))
		{
			if (block.BlockIndex != 238 && block.IsFaceTransparent(SubsystemTerrain, 4, value))
			{
				return block is SoilBlock;
			}
			return true;
		}
		return false;
	}

	public bool IsDestructible(int value)
	{
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (block is FluidBlock)
		{
			return false;
		}
		if (block is TrapdoorBlock)
		{
			int data = Terrain.ExtractData(value);
			if (TrapdoorBlock.GetUpsideDown(data) && TrapdoorBlock.GetOpen(data))
			{
				return false;
			}
		}
		return true;
	}

	public SubsystemCaveInstability()
	{
		m_internalRemovals = new HashSet<Point3>();
		m_tmpIndices = new List<int>();
		m_collapseChanceLower = 0.35f;
		m_collapseChanceUpper = 0.08f;
		m_upperVerticalMultiplier = 0.35f;
		m_maxBlocksPerEvent = 256;
		m_random = new Random();
		m_playerBreakMarks = new HashSet<Point3>();
		m_bottomCandidates = new List<Point3>(128);
		m_layerCells = new List<Point3>(128);
		m_baseCluster = new List<Point3>(128);
		m_upBuffer = new List<Point3>(128);
		m_toRemove = new HashSet<Point3>();
		m_supportBlocks = new List<Point3>(64);
	}

	static SubsystemCaveInstability()
	{
		m_handledBlocks = new int[6] { 3, 2, 5, 41, 16, 397 };
	}

	private void FloodFillClusterXZ(Point3 seed, int y, Point3 secMin, Point3 secMax, int need, List<Point3> dst)
	{
		HashSet<Point3> hashSet = new HashSet<Point3>();
		Queue<Point3> queue = new Queue<Point3>();
		Point3 item = new Point3(seed.X, y, seed.Z);
		int value = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z));
		int num = Terrain.ExtractContents((y > 0) ? m_subsystemTerrain.Terrain.GetCellValue(item.X, y - 1, item.Z) : 0);
		if (Array.IndexOf(HandledBlocks, value) < 0 || (num != 0 && num != 18))
		{
			return;
		}
		queue.Enqueue(item);
		hashSet.Add(item);
		while (queue.Count > 0 && dst.Count < need)
		{
			Point3 item2 = queue.Dequeue();
			int value2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(item2.X, item2.Y, item2.Z));
			int num2 = Terrain.ExtractContents((item2.Y > 0) ? m_subsystemTerrain.Terrain.GetCellValue(item2.X, item2.Y - 1, item2.Z) : 0);
			if (Array.IndexOf(HandledBlocks, value2) >= 0 && (num2 == 0 || num2 == 18))
			{
				dst.Add(item2);
				Point3 item3 = new Point3(item2.X + 1, y, item2.Z);
				Point3 item4 = new Point3(item2.X - 1, y, item2.Z);
				Point3 item5 = new Point3(item2.X, y, item2.Z + 1);
				Point3 item6 = new Point3(item2.X, y, item2.Z - 1);
				if (item3.X >= secMin.X && item3.X <= secMax.X && item3.Z >= secMin.Z && item3.Z <= secMax.Z && !hashSet.Contains(item3))
				{
					hashSet.Add(item3);
					queue.Enqueue(item3);
				}
				if (item4.X >= secMin.X && item4.X <= secMax.X && item4.Z >= secMin.Z && item4.Z <= secMax.Z && !hashSet.Contains(item4))
				{
					hashSet.Add(item4);
					queue.Enqueue(item4);
				}
				if (item5.X >= secMin.X && item5.X <= secMax.X && item5.Z >= secMin.Z && item5.Z <= secMax.Z && !hashSet.Contains(item5))
				{
					hashSet.Add(item5);
					queue.Enqueue(item5);
				}
				if (item6.X >= secMin.X && item6.X <= secMax.X && item6.Z >= secMin.Z && item6.Z <= secMax.Z && !hashSet.Contains(item6))
				{
					hashSet.Add(item6);
					queue.Enqueue(item6);
				}
			}
		}
	}

	private int BiasToHigh(int minValue, int maxValue)
	{
		if (maxValue <= minValue)
		{
			return minValue;
		}
		float num = m_random.Float(0f, 1f);
		float num2 = 1f - num * num * num;
		return MathUtils.Clamp(minValue + (int)MathUtils.Round((float)(maxValue - minValue) * num2), minValue, maxValue);
	}

	private Point3 GetTightAnchor(HashSet<Point3> cells)
	{
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		foreach (Point3 cell in cells)
		{
			if (cell.X < num)
			{
				num = cell.X;
			}
			if (cell.Y < num2)
			{
				num2 = cell.Y;
			}
			if (cell.Z < num3)
			{
				num3 = cell.Z;
			}
		}
		return new Point3(num, num2, num3);
	}

	private int FindRestY(int x, int startY, int z)
	{
		int num;
		for (num = startY; num > 0; num--)
		{
			int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num - 1, z));
			if (num2 != 0 && num2 != 18)
			{
				break;
			}
		}
		return num;
	}

	public void MarkPlayerBreak(Point3 p)
	{
		m_playerBreakMarks.Add(p);
	}
}
