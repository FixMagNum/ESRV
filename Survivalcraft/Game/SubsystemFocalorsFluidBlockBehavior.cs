using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public abstract class SubsystemFocalorsFluidBlockBehavior : SubsystemPollableBlockBehavior
{
	public struct SpreadData(float volume, bool destroy)
	{
		public float Volume = volume;

		public bool Destroy = destroy;
	}

	public static readonly Point2[] m_sideNeighbors;

	public SubsystemTime SubsystemTime;

	public readonly int m_handleBlock;

	public Dictionary<Point3, Vector2> m_fluidRandomFlowDirections;

	protected bool m_lastSourceArtificial;

	private Dictionary<Point3, float> m_cacheDictionary;

	private HashSet<Point3> m_cacheHashSet;

	private HashSet<Point3> m_cacheHashSet2;

	public SubsystemOceanScanner m_subsystemOceanScanner;

	protected Random m_random;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemGameInfo m_subsystemGameInfo;

	public readonly Dictionary<Point3, SpreadData> m_toSpread;

	private static readonly Point3[] m_displacementOffsets;

	private SubsystemPlayers m_subsystemPlayers;

	private Queue<Point3>[] m_priorityQueues;

	public SubsystemFluidDynamics m_subsystemFluidDynamics;

	private static Dictionary<int, double> m_lastLogTimes;

	private Dictionary<Point3, int> m_updatePriority;

	private double m_lastCacheUpdateTime;

	private List<Point2> m_cachedPlayerPositions = new List<Point2>();

	public SubsystemAudio SubsystemAudio { get; set; }

	public SubsystemAmbientSounds SubsystemAmbientSounds { get; set; }

	public SubsystemFocalorsFluidBlockBehavior(int handleBlock)
	{
		m_lastCacheUpdateTime = -1.0;
		m_random = new Random();
		m_updatePriority = new Dictionary<Point3, int>();
		m_cacheDictionary = new Dictionary<Point3, float>();
		m_cacheHashSet = new HashSet<Point3>();
		m_cacheHashSet2 = new HashSet<Point3>();
		m_fluidRandomFlowDirections = new Dictionary<Point3, Vector2>();
		m_priorityQueues = new Queue<Point3>[4];
		for (int i = 0; i < 4; i++)
		{
			m_priorityQueues[i] = new Queue<Point3>();
		}
		m_updatePriority = new Dictionary<Point3, int>();
		m_toSpread = new Dictionary<Point3, SpreadData>();
		m_handleBlock = handleBlock;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemOceanScanner = Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: false);
		SubsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		SubsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemFluidDynamics = Project.FindSubsystem<SubsystemFluidDynamics>(throwOnError: true);
	}

	public void SpreadFluid()
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			num += m_priorityQueues[i].Count;
		}
		if (num == 0 && m_toSpread.Count == 0)
		{
			return;
		}
		if (SettingsManager.DebugMode && num > 0)
		{
			if (!m_lastLogTimes.ContainsKey(m_handleBlock))
			{
				m_lastLogTimes[m_handleBlock] = -100.0;
			}
			if (SubsystemTime.GameTime - m_lastLogTimes[m_handleBlock] > 10.0)
			{
				m_lastLogTimes[m_handleBlock] = SubsystemTime.GameTime;
				Func<int, string> func = (int n) => n.ToString("N0").Replace(',', '.');
				string text;
				if (m_handleBlock == 18)
				{
					text = "WATER";
				}
				else
				{
					text = ((m_handleBlock == 92) ? "MAGMA" : "FLUID");
				}
				string message = $"[{text}]\u00a0Near:{func(m_priorityQueues[0].Count)}\u00a0||\u00a0Medium:{func(m_priorityQueues[1].Count)}\u00a0||\u00a0Far:{func(m_priorityQueues[2].Count)}\u00a0||\u00a0VeryFar:{func(m_priorityQueues[3].Count)}\u00a0||\u00a0Total:{func(num)}\u00a0|\u00a0FPS:{PerformanceManager.Fps:0.0}";
				if (m_handleBlock == 92)
				{
					Log.Error(message);
				}
				else if (m_handleBlock == 18)
				{
					Log.Warning(message);
				}
				else
				{
					Log.Information(message);
				}
			}
		}
		Terrain terrain = SubsystemTerrain.Terrain;
		int fluidUpdateQuota = m_subsystemGameInfo.WorldSettings.FluidUpdateQuota;
		int num2 = (TerrainUpdater.IsBusy ? Math.Min(20, fluidUpdateQuota) : fluidUpdateQuota);
		for (int num3 = 0; num3 < 4; num3++)
		{
			if (num2 <= 0)
			{
				break;
			}
			Queue<Point3> queue = m_priorityQueues[num3];
			while (queue.Count > 0 && num2 > 0)
			{
				Point3 point = queue.Dequeue();
				if (!m_updatePriority.TryGetValue(point, out var _))
				{
					continue;
				}
				m_updatePriority.Remove(point);
				num2--;
				int cellValueFast = terrain.GetCellValueFast(point.X, point.Y, point.Z);
				if (Terrain.ExtractContents(cellValueFast) != m_handleBlock)
				{
					m_toSpread.Remove(point);
					continue;
				}
				bool flag = false;
				if (m_handleBlock == 18 && m_subsystemOceanScanner != null && point.Y <= m_subsystemOceanScanner.OceanLevel && m_subsystemOceanScanner.IsBlockInOcean(point.X, point.Z))
				{
					flag = true;
				}
				bool flag2 = false;
				float num4;
				if (!m_toSpread.TryGetValue(point, out var value2))
				{
					num4 = ((!flag) ? (1f - FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast))) : 1f);
				}
				else
				{
					num4 = value2.Volume;
					flag2 = true;
				}
				bool flag3 = true;
				if (m_handleBlock == 18)
				{
					PrecipitationType? precipitationAtPoint = m_subsystemFluidDynamics.GetPrecipitationAtPoint(point);
					if (num4 < 0.15f && precipitationAtPoint == PrecipitationType.Rain)
					{
						flag3 = false;
					}
				}
				if (!(num4 >= 0.01f))
				{
					continue;
				}
				float num5 = num4;
				if (point.Y > 0)
				{
					Point3 key = new Point3
					{
						X = point.X,
						Y = point.Y - 1,
						Z = point.Z
					};
					int cellValueFast2 = terrain.GetCellValueFast(point.X, point.Y - 1, point.Z);
					int num6 = Terrain.ExtractContents(cellValueFast2);
					if (BlocksManager.Blocks[num6] is IWaterloggable && point.Y > 1)
					{
						key.Y = point.Y - 2;
						cellValueFast2 = terrain.GetCellValueFast(point.X, point.Y - 2, point.Z);
						num6 = Terrain.ExtractContents(cellValueFast2);
					}
					if (m_toSpread.TryGetValue(key, out var value3))
					{
						float num7 = 1f - value3.Volume;
						if (num7 >= num4)
						{
							if (flag2)
							{
								value2.Volume = 0f;
								m_toSpread[point] = value2;
							}
							else
							{
								m_toSpread.Add(point, new SpreadData(0f, destroy: false));
							}
							value3.Volume += num4;
							m_toSpread[key] = value3;
							continue;
						}
						num4 -= num7;
						value3.Volume = 1f;
						m_toSpread[key] = value3;
					}
					else
					{
						if (num6 == 0)
						{
							if (flag2)
							{
								value2.Volume = 0f;
								m_toSpread[point] = value2;
							}
							else
							{
								m_toSpread.Add(point, new SpreadData(0f, destroy: false));
							}
							m_toSpread.Add(key, new SpreadData(num4, destroy: false));
							continue;
						}
						if (num6 == m_handleBlock)
						{
							float level = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast2));
							if (level >= num4)
							{
								if (flag2)
								{
									value2.Volume = 0f;
									m_toSpread[point] = value2;
								}
								else
								{
									m_toSpread.Add(point, new SpreadData(0f, destroy: false));
								}
								m_toSpread.Add(key, new SpreadData(1f - level + num4, destroy: false));
								continue;
							}
							if (level > 0f)
							{
								m_toSpread.Add(key, new SpreadData(1f, destroy: false));
								num4 -= level;
							}
						}
						else if (!m_subsystemGameInfo.WorldSettings.FluidsDoNotBreakBlocks && !BlocksManager.Blocks[num6].IsFluidBlocker_(cellValueFast2) && !(BlocksManager.Blocks[num6] is IWaterloggable))
						{
							if (flag2)
							{
								value2.Volume = 0f;
								m_toSpread[point] = value2;
							}
							else
							{
								m_toSpread.Add(point, new SpreadData(0f, destroy: false));
							}
							m_toSpread.Add(key, new SpreadData(num4, destroy: true));
							continue;
						}
					}
					m_cacheDictionary.Clear();
					m_cacheHashSet.Clear();
					if (point.Y > 0)
					{
						Point2[] sideNeighbors = m_sideNeighbors;
						for (int num8 = 0; num8 < sideNeighbors.Length; num8++)
						{
							Point2 point2 = sideNeighbors[num8];
							if (!IsNeighborChunkLoaded(point.X + point2.X, point.Z + point2.Y))
							{
								continue;
							}
							Point3 point3 = new Point3
							{
								X = point.X + point2.X,
								Y = point.Y - 1,
								Z = point.Z + point2.Y
							};
							if (m_toSpread.TryGetValue(point3, out var value4))
							{
								if (value4.Volume < 1f)
								{
									m_cacheDictionary.Add(point3, value4.Volume);
								}
								continue;
							}
							int cellValueFast3 = terrain.GetCellValueFast(point3.X, point3.Y, point3.Z);
							int num9 = Terrain.ExtractContents(cellValueFast3);
							if (num9 == 0)
							{
								m_cacheDictionary.Add(point3, 0f);
							}
							else if (num9 == m_handleBlock)
							{
								float num10 = 1f - FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast3));
								if (num10 < 1f)
								{
									m_cacheDictionary.Add(point3, num10);
								}
							}
							else if (!m_subsystemGameInfo.WorldSettings.FluidsDoNotBreakBlocks && !BlocksManager.Blocks[num9].IsFluidBlocker_(cellValueFast3) && !(BlocksManager.Blocks[num9] is IWaterloggable))
							{
								m_cacheDictionary.Add(point3, 0f);
								m_cacheHashSet.Add(point3);
							}
						}
					}
					if (m_cacheDictionary.Count > 0)
					{
						if (flag)
						{
							foreach (KeyValuePair<Point3, float> item in m_cacheDictionary)
							{
								Point3 key2 = item.Key;
								if (!m_toSpread.ContainsKey(key2))
								{
									m_toSpread.Add(key2, new SpreadData(1f, destroy: false));
								}
							}
							m_cacheDictionary.Clear();
							num4 = 0f;
						}
						else
						{
							while (m_cacheDictionary.Count > 0)
							{
								float num11 = 0f;
								foreach (float value12 in m_cacheDictionary.Values)
								{
									if (value12 > num11)
									{
										num11 = value12;
									}
								}
								float num12 = 1f - num11;
								float num13 = num12 * (float)m_cacheDictionary.Count;
								if (num4 >= num13)
								{
									m_cacheHashSet2.Clear();
									foreach (KeyValuePair<Point3, float> item2 in m_cacheDictionary)
									{
										Point3 key3 = item2.Key;
										float value5 = item2.Value;
										if (m_toSpread.TryGetValue(key3, out var value6))
										{
											value6.Volume += num12;
											if (value6.Volume >= 0.99f)
											{
												m_cacheHashSet2.Add(key3);
											}
											m_toSpread[key3] = value6;
										}
										else
										{
											m_toSpread.Add(key3, new SpreadData(value5 + num12, destroy: false));
										}
									}
									foreach (Point3 item3 in m_cacheHashSet2)
									{
										m_cacheDictionary.Remove(item3);
									}
									num4 -= num13;
									continue;
								}
								float num14 = num4 / (float)m_cacheDictionary.Count;
								foreach (KeyValuePair<Point3, float> item4 in m_cacheDictionary)
								{
									Point3 key4 = item4.Key;
									float value7 = item4.Value;
									if (m_toSpread.TryGetValue(key4, out var value8))
									{
										value8.Volume += num14;
										m_toSpread[key4] = value8;
									}
									else if (m_cacheHashSet.Contains(key4))
									{
										m_toSpread.Add(key4, new SpreadData(num14, destroy: true));
									}
									else
									{
										m_toSpread.Add(key4, new SpreadData(value7 + num14, destroy: false));
									}
								}
								m_cacheDictionary.Clear();
								num4 = 0f;
							}
						}
					}
				}
				if ((num4 > 0.01f) & flag3)
				{
					m_cacheDictionary.Clear();
					m_cacheHashSet.Clear();
					int num15 = m_subsystemGameInfo.WorldSettings.FluidSpreadDistance;
					if (m_handleBlock == 18 && m_subsystemOceanScanner != null && m_subsystemOceanScanner.IsBlockInOcean(point.X, point.Z))
					{
						num15 = 9;
					}
					Point2[] sideNeighbors = m_sideNeighbors;
					for (int num8 = 0; num8 < sideNeighbors.Length; num8++)
					{
						Point2 point4 = sideNeighbors[num8];
						if (num2 <= 0)
						{
							break;
						}
						for (int num16 = 1; num16 <= num15; num16++)
						{
							int x = point.X + point4.X * num16;
							int z = point.Z + point4.Y * num16;
							if (!IsNeighborChunkLoaded(x, z))
							{
								break;
							}
							if (num16 > 1)
							{
								int x2 = point.X + point4.X * (num16 - 1);
								int z2 = point.Z + point4.Y * (num16 - 1);
								if (point.Y > 0)
								{
									int cellValueFast4 = terrain.GetCellValueFast(x2, point.Y - 1, z2);
									int num17 = Terrain.ExtractContents(cellValueFast4);
									if (!BlocksManager.Blocks[num17].IsFluidBlocker_(cellValueFast4) && num17 != m_handleBlock)
									{
										break;
									}
								}
							}
							Point3 point5 = new Point3(x, point.Y, z);
							int cellValueFast5 = terrain.GetCellValueFast(x, point.Y, z);
							int num18 = Terrain.ExtractContents(cellValueFast5);
							Block block = BlocksManager.Blocks[num18];
							if (block.IsFluidBlocker_(cellValueFast5) && !(block is IWaterloggable) && !m_subsystemGameInfo.WorldSettings.FluidsDoNotBreakBlocks && num18 != m_handleBlock)
							{
								break;
							}
							if (m_toSpread.TryGetValue(point5, out var value9))
							{
								if (value9.Volume < num4 && !m_cacheDictionary.ContainsKey(point5))
								{
									m_cacheDictionary.Add(point5, value9.Volume);
								}
								continue;
							}
							if (num18 == 0)
							{
								m_cacheDictionary.Add(point5, 0f);
								continue;
							}
							if (num18 == m_handleBlock)
							{
								float num19 = 1f - FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast5));
								if (num19 < num4)
								{
									m_cacheDictionary.Add(point5, num19);
								}
								continue;
							}
							if ((m_subsystemGameInfo.WorldSettings.FluidsDoNotBreakBlocks || block.IsFluidBlocker_(cellValueFast5)) && !(block is IWaterloggable))
							{
								break;
							}
							if (!(block is IWaterloggable))
							{
								m_cacheDictionary.Add(point5, 0f);
								m_cacheHashSet.Add(point5);
							}
						}
					}
					if (m_cacheDictionary.Count > 0)
					{
						float num20 = 0f;
						foreach (float value13 in m_cacheDictionary.Values)
						{
							num20 += value13;
						}
						float num21 = (num20 + num4) / (float)(m_cacheDictionary.Count + 1);
						if (num21 > 0.005f)
						{
							num4 = num21;
							foreach (KeyValuePair<Point3, float> item5 in m_cacheDictionary)
							{
								Point3 key5 = item5.Key;
								if (Math.Abs(item5.Value - num21) > 0.005f)
								{
									if (m_toSpread.TryGetValue(key5, out var value10))
									{
										value10.Volume = num21;
										m_toSpread[key5] = value10;
									}
									else if (m_cacheHashSet.Contains(key5))
									{
										m_toSpread.Add(key5, new SpreadData(num21, destroy: true));
									}
									else
									{
										m_toSpread.Add(key5, new SpreadData(num21, destroy: false));
									}
								}
							}
						}
						else
						{
							num4 = 0f;
						}
					}
				}
				if (!flag && Math.Abs(num5 - num4) > 0.005f)
				{
					if (flag2)
					{
						value2.Volume = ((num4 > 0.01f) ? num4 : 0f);
						m_toSpread[point] = value2;
					}
					else
					{
						m_toSpread.Add(point, new SpreadData((num4 > 0.01f) ? num4 : 0f, destroy: false));
					}
				}
			}
		}
		foreach (KeyValuePair<Point3, SpreadData> item6 in m_toSpread)
		{
			Point3 key6 = item6.Key;
			SpreadData value11 = item6.Value;
			int num22 = 0;
			if (value11.Volume >= 0.01f)
			{
				float num23 = (((double)value11.Volume > 0.99) ? 0f : (1f - value11.Volume));
				int data = FocalorsFluidBlock.SetLevel(0, num23);
				bool isTop = key6.Y >= 255 || SubsystemTerrain.Terrain.GetCellContentsFast(key6.X, key6.Y + 1, key6.Z) != m_handleBlock || num23 > 0f;
				data = FocalorsFluidBlock.SetIsTop(data, isTop);
				num22 = Terrain.MakeBlockValue(m_handleBlock, 0, data);
			}
			else
			{
				value11.Destroy = true;
			}
			if (key6.Y < 0 || key6.Y >= 256 || (num22 != 0 && OnFluidInteract(key6.X, key6.Y, key6.Z, num22)))
			{
				continue;
			}
			if (value11.Destroy)
			{
				int num24 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(key6.X, key6.Y, key6.Z));
				if (num24 == 19 || num24 == 61 || num24 == 204 || num24 == 28 || num24 == 99 || num24 == 174 || num24 == 119 || num24 == 25 || num24 == 24 || num24 == 20)
				{
					SubsystemTerrain.DestroyCell(0, key6.X, key6.Y, key6.Z, num22, noDrop: true, noParticleSystem: false);
				}
				else
				{
					SubsystemTerrain.DestroyCell(0, key6.X, key6.Y, key6.Z, num22, noDrop: false, noParticleSystem: false);
				}
			}
			else
			{
				SubsystemTerrain.ChangeCellFluid(key6.X, key6.Y, key6.Z, num22);
			}
		}
		m_toSpread.Clear();
		m_cacheDictionary.Clear();
		m_cacheHashSet.Clear();
		m_cacheHashSet2.Clear();
	}

	public float FluidDropDown(int x, int y, int z, int startValue)
	{
		int value = startValue;
		Terrain terrain = SubsystemTerrain.Terrain;
		while (y < 255)
		{
			int num = ((y != 254) ? terrain.GetCellValueFast(x, y + 1, z) : 0);
			if (y == 254 || Terrain.ExtractContents(num) != m_handleBlock || (m_toSpread.TryGetValue(new Point3(x, y + 1, z), out var value2) && value2.Volume < 0.01f))
			{
				Point3 key = new Point3
				{
					X = x,
					Y = y,
					Z = z
				};
				float result;
				if (m_toSpread.TryGetValue(key, out var value3))
				{
					result = value3.Volume;
					value3.Volume = 0f;
					m_toSpread[key] = value3;
				}
				else
				{
					result = 1f - FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value));
					m_toSpread.Add(key, new SpreadData(0f, destroy: false));
				}
				return result;
			}
			value = num;
			y++;
		}
		return 0f;
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		AddToUpdateQueue(x, y, z, isGeneration: false);
		UpdateIsTop(value, x, y, z);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		int componentSleep = Terrain.ExtractContents(newValue);
		if (componentSleep != 0 && componentSleep != m_handleBlock && (m_handleBlock != 18 || (componentSleep != 62 && componentSleep != 352)) && (m_handleBlock != 92 || (componentSleep != 3 && componentSleep != 67)))
		{
			TryDisplaceFluid(x, y, z, value);
		}
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		UpdateIsTop(value, x, y, z);
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		AddToUpdateQueue(x, y, z, isGeneration: false);
		if (neighborY == y + 1 || neighborY == y - 1)
		{
			UpdateIsTop(SubsystemTerrain.Terrain.GetCellValueFast(x, y, z), x, y, z);
		}
	}

	public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue)
	{
		newBlockValue = m_handleBlock;
		dropValue.Value = 0;
		dropValue.Count = 0;
	}

	public void UpdateIsTop(int value, int x, int y, int z)
	{
		Terrain terrain = SubsystemTerrain.Terrain;
		if (terrain.GetChunkAtCell(x, z) != null)
		{
			int data = Terrain.ExtractData(value);
			bool isTop = y >= 255 || terrain.GetCellContentsFast(x, y + 1, z) != m_handleBlock || FocalorsFluidBlock.GetLevel(data) > 0f;
			int data2 = FocalorsFluidBlock.SetIsTop(data, isTop);
			terrain.SetCellValueFast(x, y, z, Terrain.ReplaceData(value, data2));
		}
	}

	static SubsystemFocalorsFluidBlockBehavior()
	{
		m_displacementOffsets = new Point3[5]
		{
			new Point3(0, 1, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
		m_lastLogTimes = new Dictionary<int, double>();
		m_sideNeighbors = new Point2[4]
		{
			new Point2(-1, 0),
			new Point2(1, 0),
			new Point2(0, -1),
			new Point2(0, 1)
		};
	}

	public float? GetSurfaceHeight(int x, int y, int z)
	{
		FocalorsFluidBlock surfaceFluidBlock;
		return GetSurfaceHeight(x, y, z, out surfaceFluidBlock);
	}

	public Vector2? CalculateFlowSpeed(int x, int y, int z)
	{
		FocalorsFluidBlock surfaceBlock;
		float? surfaceHeight;
		return CalculateFlowSpeed(x, y, z, out surfaceBlock, out surfaceHeight);
	}

	public virtual bool OnFluidInteract(int x, int y, int z, int fluidValue)
	{
		int num = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x, y, z));
		if (m_subsystemGameInfo.WorldSettings.FluidsDoNotBreakBlocks && num != 0)
		{
			return false;
		}
		if (!BlocksManager.Blocks[num].IsFluidBlocker && !(BlocksManager.Blocks[num] is IWaterloggable))
		{
			if (num == 19 || num == 61 || num == 204 || num == 28 || num == 99 || num == 174 || num == 119 || num == 25 || num == 24 || num == 20)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
			}
			else
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			SubsystemTerrain.ChangeCellFluid(x, y, z, fluidValue);
			return true;
		}
		return false;
	}

	public float? CalculateDistanceToFluid(Vector3 p, int radius, bool flowingFluidOnly)
	{
		float num = float.MaxValue;
		Terrain terrain = SubsystemTerrain.Terrain;
		int num2 = Terrain.ToCell(p.X) - radius;
		int num3 = Terrain.ToCell(p.X) + radius;
		int num4 = MathUtils.Clamp(Terrain.ToCell(p.Y) - radius, 0, 254);
		int num5 = MathUtils.Clamp(Terrain.ToCell(p.Y) + radius, 0, 254);
		int num6 = Terrain.ToCell(p.Z) - radius;
		int num7 = Terrain.ToCell(p.Z) + radius;
		for (int i = num6; i <= num7; i++)
		{
			for (int j = num2; j <= num3; j++)
			{
				TerrainChunk chunkAtCell = terrain.GetChunkAtCell(j, i);
				if (chunkAtCell == null)
				{
					continue;
				}
				int num8 = TerrainChunk.CalculateCellIndex(j & 0xF, num4, i & 0xF);
				for (int k = num4; k <= num5; k++)
				{
					if (Terrain.ExtractContents(chunkAtCell.GetCellValueFast(num8)) == m_handleBlock)
					{
						float num9 = p.X - ((float)j + 0.5f);
						float num10 = p.Y - ((float)k + 1f);
						float num11 = p.Z - ((float)i + 0.5f);
						float num12 = num9 * num9 + num10 * num10 + num11 * num11;
						if (num12 < num)
						{
							num = num12;
							if (num < 1.1f)
							{
								return MathUtils.Sqrt(num);
							}
						}
					}
					num8++;
				}
			}
		}
		if (num == float.MaxValue)
		{
			return null;
		}
		return MathUtils.Sqrt(num);
	}

	private float GetNeighborLevel(int val, float defaultLvl)
	{
		if (Terrain.ExtractContents(val) != m_handleBlock)
		{
			return defaultLvl;
		}
		return FocalorsFluidBlock.GetLevel(Terrain.ExtractData(val));
	}

	public void Set(int x, int y, int z, int value)
	{
		if (m_lastSourceArtificial)
		{
			int num = Terrain.ExtractData(value);
			num |= 0x20;
			value = Terrain.ReplaceData(value, num);
		}
		SubsystemTerrain.ChangeCellFluid(x, y, z, value);
	}

	public float? GetSurfaceHeight(int x, int y, int z, out FocalorsFluidBlock surfaceFluidBlock)
	{
		int i;
		for (i = y; i < 255; i++)
		{
			if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x, i, z)) != m_handleBlock)
			{
				i--;
				break;
			}
		}
		int num = MathUtils.Clamp(i, 0, 255);
		if (num >= 0 && num < 256)
		{
			int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, num, z);
			int num2 = Terrain.ExtractContents(cellValueFast);
			if (num2 == m_handleBlock && BlocksManager.Blocks[num2] is FocalorsFluidBlock focalorsFluidBlock)
			{
				surfaceFluidBlock = focalorsFluidBlock;
				float level = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast));
				return (float)num + FocalorsFluidBlock.GetLevelHeight(level);
			}
		}
		surfaceFluidBlock = null;
		return null;
	}

	public Vector2? CalculateFlowSpeed(int x, int y, int z, out FocalorsFluidBlock surfaceBlock, out float? surfaceHeight)
	{
		surfaceHeight = GetSurfaceHeight(x, y, z, out surfaceBlock);
		if (surfaceHeight.HasValue)
		{
			int y2 = (int)surfaceHeight.Value;
			int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y2, z);
			if (Terrain.ExtractContents(cellValueFast) == m_handleBlock)
			{
				int cellValueFast2 = SubsystemTerrain.Terrain.GetCellValueFast(x - 1, y2, z);
				int cellValueFast3 = SubsystemTerrain.Terrain.GetCellValueFast(x + 1, y2, z);
				int cellValueFast4 = SubsystemTerrain.Terrain.GetCellValueFast(x, y2, z - 1);
				int cellValueFast5 = SubsystemTerrain.Terrain.GetCellValueFast(x, y2, z + 1);
				float level = FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValueFast));
				float neighborLevel = GetNeighborLevel(cellValueFast2, level);
				float neighborLevel2 = GetNeighborLevel(cellValueFast3, level);
				float neighborLevel3 = GetNeighborLevel(cellValueFast4, level);
				float neighborLevel4 = GetNeighborLevel(cellValueFast5, level);
				Vector2 vector = new Vector2(level - neighborLevel - (level - neighborLevel2), level - neighborLevel3 - (level - neighborLevel4));
				if (vector.LengthSquared() < 0.005f)
				{
					return Vector2.Zero;
				}
				if (vector.LengthSquared() > 1f)
				{
					vector = Vector2.Normalize(vector);
				}
				Point3 key = new Point3(x, y2, z);
				if (!m_fluidRandomFlowDirections.TryGetValue(key, out var value))
				{
					value.X = 0.05f * (2f * SimplexNoise.OctavedNoise((float)x + 0.2f * (float)SubsystemTime.GameTime, z, 0.1f, 1, 1f, 1f) - 1f);
					value.Y = 0.05f * (2f * SimplexNoise.OctavedNoise((float)x + 0.2f * (float)SubsystemTime.GameTime + 100f, z, 0.1f, 1, 1f, 1f) - 1f);
					if (m_fluidRandomFlowDirections.Count < 1000)
					{
						m_fluidRandomFlowDirections[key] = value;
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
		surfaceBlock = null;
		return null;
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (m_handleBlock != 18 || m_subsystemOceanScanner == null || y > m_subsystemOceanScanner.OceanLevel || !m_subsystemOceanScanner.IsBlockInOcean(x, z))
		{
			AddToUpdateQueue(x, y, z, isGeneration: true);
		}
	}

	private bool IsNeighborChunkLoaded(int x, int z)
	{
		TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null)
		{
			return chunkAtCell.State >= TerrainChunkState.InvalidContents4;
		}
		return false;
	}

	public override void OnExplosion(int value, int x, int y, int z, float pressure)
	{
		if (Terrain.ExtractContents(value) != m_handleBlock || pressure < 10f)
		{
			return;
		}
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
		if (m_handleBlock == 18)
		{
			m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(SubsystemTerrain, position, large: true));
			SubsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), position, 8f, autoDelay: true);
		}
		else if (m_handleBlock == 92)
		{
			m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(SubsystemTerrain, position, large: true));
			SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), position, 6f, autoDelay: true);
		}
		if (!(pressure > 20f))
		{
			return;
		}
		List<Point3> list = new List<Point3>();
		Point3[] array = new Point3[6]
		{
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Point3 point = array[i];
			int x2 = x + point.X;
			int num = y + point.Y;
			int z2 = z + point.Z;
			if (num > 0 && num < 255 && IsNeighborChunkLoaded(x2, z2) && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x2, num, z2)) == 0)
			{
				list.Add(new Point3(x2, num, z2));
			}
		}
		SubsystemTerrain.ChangeCellFluid(x, y, z, 0);
		AddToUpdateQueue(x, y, z, isGeneration: false);
		if (list.Count > 0)
		{
			Point3 point2 = new Point3(x, y + 1, z);
			Point3 point3 = ((!list.Contains(point2) || !(m_random.Float(0f, 1f) < 0.8f)) ? list[m_random.Int(0, list.Count - 1)] : point2);
			float num2 = MathUtils.Max(FocalorsFluidBlock.GetLevel(Terrain.ExtractData(value)) * 0.9f, 0.1f);
			float num3 = 1f - num2;
			int data = FocalorsFluidBlock.SetLevel(0, num3);
			bool isTop = point3.Y >= 255 || SubsystemTerrain.Terrain.GetCellContentsFast(point3.X, point3.Y + 1, point3.Z) != m_handleBlock || num3 > 0f;
			data = FocalorsFluidBlock.SetIsTop(data, isTop);
			int value2 = Terrain.MakeBlockValue(m_handleBlock, 0, data);
			SubsystemTerrain.ChangeCellFluid(point3.X, point3.Y, point3.Z, value2);
			AddToUpdateQueue(point3.X, point3.Y, point3.Z, isGeneration: false);
		}
	}

	public bool TryDisplaceFluid(int x, int y, int z, int fluidValue, float volumeToPush = -1f, int depth = 0)
	{
		if (depth > 3)
		{
			return false;
		}
		Terrain keyValuePair = SubsystemTerrain.Terrain;
		int num = Terrain.ExtractContents(fluidValue);
		int data = Terrain.ExtractData(fluidValue);
		float num2 = ((volumeToPush < 0f) ? (1f - FocalorsFluidBlock.GetLevel(data)) : volumeToPush);
		if (num2 <= 0.01f)
		{
			return true;
		}
		bool flag = false;
		Point3[] displacementOffsets = m_displacementOffsets;
		for (int i = 0; i < displacementOffsets.Length; i++)
		{
			Point3 point = displacementOffsets[i];
			int num3 = x + point.X;
			int num4 = y + point.Y;
			int num5 = z + point.Z;
			if (num4 < 0 || num4 >= 256 || !IsNeighborChunkLoaded(num3, num5))
			{
				continue;
			}
			int cellValueFast = keyValuePair.GetCellValueFast(num3, num4, num5);
			int num6 = Terrain.ExtractContents(cellValueFast);
			if (m_handleBlock == 18 && m_subsystemOceanScanner != null && num4 <= m_subsystemOceanScanner.OceanLevel + 1 && m_subsystemOceanScanner.IsBlockInOcean(num3, num5) && (num6 == 0 || num6 == m_handleBlock))
			{
				flag = true;
				continue;
			}
			if (num6 == 0)
			{
				int data2 = FocalorsFluidBlock.SetLevel(0, 1f - num2);
				int value = Terrain.MakeBlockValue(num, 0, data2);
				SubsystemTerrain.ChangeCellFluid(num3, num4, num5, value);
				Vector3 position = new Vector3((float)num3 + 0.5f, (float)num4 + 0.5f, (float)num5 + 0.5f);
				switch (num)
				{
				case 18:
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(SubsystemTerrain, position, large: false));
					SubsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), position, 6f, autoDelay: true);
					break;
				case 92:
					m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, position));
					SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), position, 4f, autoDelay: true);
					break;
				}
				flag = true;
				break;
			}
			if (num6 != num)
			{
				continue;
			}
			int data3 = Terrain.ExtractData(cellValueFast);
			float num7 = 1f - FocalorsFluidBlock.GetLevel(data3);
			float num8 = MathUtils.Min(1f - num7, num2);
			float num9 = num2 - num8;
			if (num9 <= 0.01f || TryDisplaceFluid(num3, num4, num5, fluidValue, num9, depth + 1))
			{
				float num10 = num7 + num8;
				if (num9 > 0.01f)
				{
					num10 = 1f;
				}
				float num11 = 1f - num10;
				int data4 = FocalorsFluidBlock.SetLevel(0, num11);
				bool isTop = num4 >= 255 || keyValuePair.GetCellContentsFast(num3, num4 + 1, num5) != m_handleBlock || num11 > 0f;
				data4 = FocalorsFluidBlock.SetIsTop(data4, isTop);
				int value2 = Terrain.MakeBlockValue(num, 0, data4);
				SubsystemTerrain.ChangeCellFluid(num3, num4, num5, value2);
				Vector3 position2 = new Vector3((float)num3 + 0.5f, (float)num4 + 0.5f, (float)num5 + 0.5f);
				switch (num)
				{
				case 18:
					m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(SubsystemTerrain, position2, large: false));
					SubsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), position2, 6f, autoDelay: true);
					break;
				case 92:
					m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, position2));
					SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), position2, 4f, autoDelay: true);
					break;
				}
				flag = true;
				break;
			}
		}
		if (depth == 0 && !flag)
		{
			int num12 = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValueFast(x, y, z));
			if (BlocksManager.Blocks[num12] is IWaterloggable)
			{
				return false;
			}
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: true);
			int data5 = FocalorsFluidBlock.SetLevel(0, 1f - num2);
			int value3 = Terrain.MakeBlockValue(num, 0, data5);
			SubsystemTerrain.ChangeCellFluid(x, y, z, value3);
		}
		return flag;
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
	}

	public bool DisplaceFluidVertically(int x, int y, int z)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return false;
		}
		Terrain terrain = SubsystemTerrain.Terrain;
		if (Terrain.ExtractContents(terrain.GetCellValueFast(x, y, z)) != m_handleBlock)
		{
			return true;
		}
		int num = 0;
		int i;
		for (i = y; i < 256 && Terrain.ExtractContents(terrain.GetCellValueFast(x, i, z)) == m_handleBlock; i++)
		{
			num++;
		}
		if (num > 4)
		{
			return false;
		}
		if (i >= 256)
		{
			return false;
		}
		int num2 = Terrain.ExtractContents(terrain.GetCellValueFast(x, i, z));
		Block block = BlocksManager.Blocks[num2];
		if (num2 != 0 && block.IsFluidBlocker)
		{
			return false;
		}
		if (num2 != 0)
		{
			SubsystemTerrain.DestroyCell(0, x, i, z, 0, noDrop: true, noParticleSystem: false);
		}
		for (int num3 = i - 1; num3 >= y; num3--)
		{
			int cellValueFast = terrain.GetCellValueFast(x, num3, z);
			SubsystemTerrain.ChangeCellFluid(x, num3 + 1, z, cellValueFast);
		}
		Vector3 position = new Vector3((float)x + 0.5f, (float)i + 0.5f, (float)z + 0.5f);
		if (m_handleBlock == 18)
		{
			SubsystemAudio.PlayRandomSound("Audio/Splashes", 1f, 0f, position, 5f, autoDelay: true);
		}
		else if (m_handleBlock == 92)
		{
			SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, position, 5f, autoDelay: true);
		}
		return true;
	}

	private void AddToUpdateQueue(int x, int y, int z, bool isGeneration)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		Point3 point = new Point3(x, y, z);
		if (isGeneration)
		{
			if (!m_updatePriority.ContainsKey(point))
			{
				m_updatePriority.Add(point, 3);
				m_priorityQueues[3].Enqueue(point);
			}
			return;
		}
		double gameTime = SubsystemTime.GameTime;
		if (gameTime - m_lastCacheUpdateTime > 0.25)
		{
			m_lastCacheUpdateTime = gameTime;
			m_cachedPlayerPositions.Clear();
			foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
			{
				if (componentPlayer.ComponentBody != null)
				{
					Vector3 position = componentPlayer.ComponentBody.Position;
					m_cachedPlayerPositions.Add(new Point2((int)position.X, (int)position.Z));
				}
			}
		}
		int num = int.MaxValue;
		int count = m_cachedPlayerPositions.Count;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				Point2 point2 = m_cachedPlayerPositions[i];
				int num2 = Math.Abs(x - point2.X) + Math.Abs(z - point2.Y);
				if (num2 < num)
				{
					num = num2;
					if (num < 20)
					{
						break;
					}
				}
			}
		}
		int num3 = ((num >= 24) ? ((num < 56) ? 1 : ((num >= 110) ? 3 : 2)) : 0);
		if (m_updatePriority.TryGetValue(point, out var value))
		{
			if (num3 < value)
			{
				m_updatePriority[point] = num3;
				m_priorityQueues[num3].Enqueue(point);
			}
		}
		else
		{
			m_updatePriority.Add(point, num3);
			m_priorityQueues[num3].Enqueue(point);
		}
	}
}
