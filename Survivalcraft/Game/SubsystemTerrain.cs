using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemTerrain : Subsystem, IDrawable, IUpdateable
{
	public static bool TerrainRenderingEnabled;

	public DynamicArray<Point3> m_modifiedList;

	public DynamicArray<Point3> m_cellsToSend;

	public static Point3[] m_neighborOffsets;

	public SubsystemGameWidgets m_subsystemViews;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public List<BlockDropValue> m_dropValues;

	public PrimitivesRenderer3D primitivesRenderer;

	public static int[] m_drawOrders;

	private readonly Stopwatch m_stopwatch;

	private long m_totalUpdateTime;

	private int m_updateCount;

	public HashSet<Point3> m_modifiedCells;

	private List<TerrainChunk> m_visibleChunksCache;

	private List<Point3> m_networkBatchCoords = new List<Point3>();

	private List<int> m_networkBatchValues = new List<int>();

	private string m_spamTrackerTrace = "";

	private double m_lastSpamLogTime;

	public SubsystemGameInfo SubsystemGameInfo { get; set; }

	public SubsystemAnimatedTextures SubsystemAnimatedTextures { get; set; }

	public SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior { get; set; }

	public SubsystemPalette SubsystemPalette { get; set; }

	public Terrain Terrain { get; set; }

	public TerrainUpdater TerrainUpdater { get; set; }

	public TerrainRenderer TerrainRenderer { get; set; }

	public TerrainSerializer23 TerrainSerializer { get; set; }

	public ITerrainContentsGenerator TerrainContentsGenerator { get; set; }

	public BlockGeometryGenerator BlockGeometryGenerator { get; set; }

	public int[] DrawOrders => m_drawOrders;

	public UpdateOrder UpdateOrder => UpdateOrder.Terrain;

	private (Point2, int) FindMajority(IEnumerable<Point2> list)
	{
		Point2 point = default;
		int num = 0;
		foreach (Point2 item in list)
		{
			if (num == 0)
			{
				point = item;
				num++;
			}
			else
			{
				num = ((!(item == point)) ? (num - 1) : (num + 1));
			}
		}
		return (point, num);
	}

	public void ProcessModifiedCells()
	{
		m_modifiedList.Count = 0;
		int num = 2000;
		List<Point3> list = new List<Point3>();
		foreach (Point3 modifiedCell in m_modifiedCells)
		{
			if (m_modifiedList.Count < num)
			{
				m_modifiedList.Add(modifiedCell);
			}
			else
			{
				list.Add(modifiedCell);
			}
		}
		m_modifiedCells.Clear();
		foreach (Point3 item in list)
		{
			m_modifiedCells.Add(item);
		}
		if (m_modifiedCells.Count == 0 && m_modifiedList.Count > 2000)
		{
			m_modifiedCells = new HashSet<Point3>();
		}
		for (int i = 0; i < m_modifiedList.Count; i++)
		{
			Point3 point = m_modifiedList.Array[i];
			for (int j = 0; j < m_neighborOffsets.Length; j++)
			{
				Point3 point2 = m_neighborOffsets[j];
				int num2 = point.X + point2.X;
				int num3 = point.Y + point2.Y;
				int num4 = point.Z + point2.Z;
				int cellContents = Terrain.GetCellContents(num2, num3, num4);
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(cellContents);
				for (int k = 0; k < blockBehaviors.Length; k++)
				{
					try
					{
						blockBehaviors[k].OnNeighborBlockChanged(num2, num3, num4, point.X, point.Y, point.Z);
					}
					catch (KeyNotFoundException ex)
					{
						int cellValue = Terrain.GetCellValue(num2, num3, num4);
						int num5 = Terrain.ExtractContents(cellValue);
						int num6 = Terrain.ExtractData(cellValue);
						if (num6 != 0)
						{
							int value = Terrain.ReplaceData(cellValue, 0);
							Terrain.SetCellValueFast(num2, num3, num4, value);
							m_modifiedCells.Add(new Point3(num2, num3, num4));
						}
						if (SettingsManager.DebugMode)
						{
							Log.Warning("ProcessModifiedCells: cleared broken block data at {0},{1},{2}, contents={3}, oldData={4}. Ex={5}", num2, num3, num4, num5, num6, ex.Message);
						}
					}
				}
			}
		}
	}

	public TerrainRaycastResult? Raycast(Vector3 start, Vector3 end, bool useInteractionBoxes, bool skipAirBlocks, Func<int, float, bool> action)
	{
		float num = Vector3.Distance(start, end);
		if (num > 1000f)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Warning("Terrain raycast too long, trimming.");
			}
			end = start + 1000f * Vector3.Normalize(end - start);
		}
		Ray3 ray = new Ray3(start, Vector3.Normalize(end - start));
		float x = start.X;
		float y = start.Y;
		float z = start.Z;
		float x2 = end.X;
		float y2 = end.Y;
		float z2 = end.Z;
		int num2 = Terrain.ToCell(x);
		int num3 = Terrain.ToCell(y);
		int num4 = Terrain.ToCell(z);
		int num5 = Terrain.ToCell(x2);
		int num6 = Terrain.ToCell(y2);
		int num7 = Terrain.ToCell(z2);
		int num8 = ((x < x2) ? 1 : ((x > x2) ? (-1) : 0));
		int num9 = ((y < y2) ? 1 : ((y > y2) ? (-1) : 0));
		int num10 = ((z < z2) ? 1 : ((z > z2) ? (-1) : 0));
		float num11 = MathUtils.Floor(x);
		float num12 = num11 + 1f;
		float num13 = ((x > x2) ? (x - num11) : (num12 - x)) / Math.Abs(x2 - x);
		float num14 = MathUtils.Floor(y);
		float num15 = num14 + 1f;
		float num16 = ((y > y2) ? (y - num14) : (num15 - y)) / Math.Abs(y2 - y);
		float num17 = MathUtils.Floor(z);
		float num18 = num17 + 1f;
		float num19 = ((z > z2) ? (z - num17) : (num18 - z)) / Math.Abs(z2 - z);
		float num20 = 1f / Math.Abs(x2 - x);
		float num21 = 1f / Math.Abs(y2 - y);
		float num22 = 1f / Math.Abs(z2 - z);
		while (true)
		{
			BoundingBox boundingBox = default;
			int collisionBoxIndex = 0;
			float? num23 = null;
			int cellValue = Terrain.GetCellValue(num2, num3, num4);
			int num24 = Terrain.ExtractContents(cellValue);
			if (num24 != 0 || !skipAirBlocks)
			{
				Ray3 ray2 = new Ray3(ray.Position - new Vector3(num2, num3, num4), ray.Direction);
				int nearestBoxIndex = 0;
				BoundingBox nearestBox = default;
				float? num25 = null;
				try
				{
					num25 = BlocksManager.Blocks[num24].Raycast(ray2, this, cellValue, useInteractionBoxes, out nearestBoxIndex, out nearestBox);
				}
				catch (Exception ex)
				{
					if (SettingsManager.DebugMode)
					{
						Log.Error($"[RAYCAST CRASH] Block ID: {num24}, Data: {Terrain.ExtractData(cellValue)}, RawValue: {cellValue}, Pos: ({num2}, {num3}, {num4}). Err: {ex.Message}");
					}
				}
				if (num25.HasValue && (!num23.HasValue || num25.Value < num23.Value))
				{
					num23 = num25;
					collisionBoxIndex = nearestBoxIndex;
					boundingBox = nearestBox;
				}
			}
			if (!num23.HasValue || !(num23.Value <= num) || (action != null && !action(cellValue, num23.Value)))
			{
				if (num13 <= num16 && num13 <= num19)
				{
					if (num2 == num5)
					{
						break;
					}
					num13 += num20;
					num2 += num8;
				}
				else if (num16 <= num13 && num16 <= num19)
				{
					if (num3 == num6)
					{
						break;
					}
					num16 += num21;
					num3 += num9;
				}
				else
				{
					if (num4 == num7)
					{
						break;
					}
					num19 += num22;
					num4 += num10;
				}
				continue;
			}
			int face = 0;
			Vector3 vector = start - new Vector3(num2, num3, num4) + num23.Value * ray.Direction;
			float num26 = float.MaxValue;
			float num27 = MathUtils.Abs(vector.X - boundingBox.Min.X);
			if (num27 < num26)
			{
				num26 = num27;
				face = 3;
			}
			num27 = MathUtils.Abs(vector.X - boundingBox.Max.X);
			if (num27 < num26)
			{
				num26 = num27;
				face = 1;
			}
			num27 = MathUtils.Abs(vector.Y - boundingBox.Min.Y);
			if (num27 < num26)
			{
				num26 = num27;
				face = 5;
			}
			num27 = MathUtils.Abs(vector.Y - boundingBox.Max.Y);
			if (num27 < num26)
			{
				num26 = num27;
				face = 4;
			}
			num27 = MathUtils.Abs(vector.Z - boundingBox.Min.Z);
			if (num27 < num26)
			{
				num26 = num27;
				face = 2;
			}
			num27 = MathUtils.Abs(vector.Z - boundingBox.Max.Z);
			if (num27 < num26)
			{
				face = 0;
			}
			return new TerrainRaycastResult
			{
				Ray = ray,
				Value = cellValue,
				CellFace = new CellFace
				{
					X = num2,
					Y = num3,
					Z = num4,
					Face = face
				},
				CollisionBoxIndex = collisionBoxIndex,
				Distance = num23.Value
			};
		}
		return null;
	}

	public List<TerrainChunk> GetVisibleChunks(Vector3 viewPosition, Vector3 viewDirection)
	{
		m_visibleChunksCache.Clear();
		Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(viewDirection, vector));
		Vector3[] array = new Vector3[6]
		{
			viewPosition,
			viewPosition + 6f * viewDirection,
			viewPosition + 6f * viewDirection - 6f * vector,
			viewPosition + 6f * viewDirection + 6f * vector,
			viewPosition + 6f * viewDirection - 2f * vector2,
			viewPosition + 6f * viewDirection + 2f * vector2
		};
		for (int i = 0; i < array.Length; i++)
		{
			Vector3 vector3 = array[i];
			TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(Terrain.ToCell(vector3.X), Terrain.ToCell(vector3.Z));
			if (chunkAtCell != null && chunkAtCell.State == TerrainChunkState.Valid && !m_visibleChunksCache.Contains(chunkAtCell))
			{
				m_visibleChunksCache.Add(chunkAtCell);
			}
		}
		return m_visibleChunksCache;
	}

	public void ChangeCellNet(int x, int y, int z, int value, bool updateModificationCounter = true, ComponentMiner miner = null)
	{
		ChangeCellNet(x, y, z, value, updateModificationCounter, miner, sendToNetwork: true);
	}

	public void ChangeCellNoSend(int x, int y, int z, int value, bool updateModificationCounter = true, ComponentMiner miner = null)
	{
		ChangeCellNet(x, y, z, value, updateModificationCounter, miner, sendToNetwork: false);
	}

	public void ChangeCell(int x, int y, int z, int value, bool updateModificationCounter = true, ComponentMiner miner = null)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			ChangeCellNet(x, y, z, value, updateModificationCounter, miner, sendToNetwork: true);
		}
	}

	public void DestroyCell(int toolLevel, int x, int y, int z, int newValue, bool noDrop, bool noParticleSystem, ComponentMiner miner = null)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		int cellValue = Terrain.GetCellValue(x, y, z);
		int realTime2 = Terrain.ExtractContents(cellValue);
		Block block = BlocksManager.Blocks[realTime2];
		if (realTime2 != 0)
		{
			bool showDebris = true;
			if (!noDrop)
			{
				m_dropValues.Clear();
				block.GetDropValues(this, cellValue, newValue, toolLevel, m_dropValues, out showDebris);
				for (int i = 0; i < m_dropValues.Count; i++)
				{
					BlockDropValue dropValue = m_dropValues[i];
					if (dropValue.Count > 0)
					{
						SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(dropValue.Value));
						for (int j = 0; j < blockBehaviors.Length; j++)
						{
							blockBehaviors[j].OnItemHarvested(x, y, z, cellValue, ref dropValue, ref newValue);
						}
						if (dropValue.Count > 0 && Terrain.ExtractContents(dropValue.Value) != 0)
						{
							Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
							m_subsystemPickables.AddPickable(dropValue.Value, dropValue.Count, position, null, null);
						}
					}
				}
			}
			if (showDebris && !noParticleSystem && m_subsystemViews.CalculateDistanceFromNearestView(new Vector3(x, y, z)) < 16f)
			{
				m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(this, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), cellValue, 1f));
			}
		}
		ChangeCell(x, y, z, newValue, updateModificationCounter: true, miner);
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (TerrainRenderingEnabled)
		{
			if (drawOrder == m_drawOrders[0])
			{
				TerrainUpdater.PrepareForDrawing(camera);
				TerrainRenderer.PrepareForDrawing(camera);
				TerrainRenderer.DrawOpaque(camera);
				TerrainRenderer.DrawAlphaTested(camera);
			}
			else if (drawOrder == m_drawOrders[1])
			{
				TerrainRenderer.DrawTransparent(camera);
			}
		}
	}

	public void Update(float dt)
	{
		if (TerrainUpdater == null)
		{
			return;
		}
		if (SettingsManager.DebugMode)
		{
			m_stopwatch.Restart();
		}
		TerrainUpdater.Update();
		if (SettingsManager.DebugMode)
		{
			m_stopwatch.Stop();
			m_totalUpdateTime += m_stopwatch.ElapsedTicks;
			m_updateCount++;
			if (m_stopwatch.ElapsedMilliseconds > 50)
			{
				Log.Warning($"[LAG UPDATE] SubsystemTerrain.TerrainUpdater update took {m_stopwatch.ElapsedMilliseconds}ms.");
			}
		}
		ProcessModifiedCells();
		FlushNetworkBatch();
		if (SettingsManager.DebugMode)
		{
			if (m_updateCount > 0)
			{
				_ = (double)(m_totalUpdateTime / m_updateCount) / (double)Stopwatch.Frequency;
			}
			m_totalUpdateTime = 0L;
			m_updateCount = 0;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		SubsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		SubsystemAnimatedTextures = Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
		SubsystemFurnitureBlockBehavior = Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
		SubsystemPalette = Project.FindSubsystem<SubsystemPalette>(throwOnError: true);
		Terrain = new Terrain();
		TerrainRenderer = new TerrainRenderer(this);
		TerrainUpdater = new TerrainUpdater(this);
		if (CommonLib.WorkType != WorkType.Client)
		{
			TerrainSerializer = new TerrainSerializer23(SubsystemGameInfo.DirectoryName);
		}
		else
		{
			TerrainSerializer = new TerrainSerializerNet();
		}
		BlockGeometryGenerator = new BlockGeometryGenerator(Terrain, this, Project.FindSubsystem<SubsystemElectricity>(throwOnError: true), SubsystemFurnitureBlockBehavior, Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true), SubsystemPalette);
		GameMode stream = SubsystemGameInfo.WorldSettings.GameMode;
		if (string.CompareOrdinal(SubsystemGameInfo.WorldSettings.OriginalSerializationVersion, "2.1") <= 0)
		{
			TerrainGenerationMode terrainGenerationMode = SubsystemGameInfo.WorldSettings.TerrainGenerationMode;
			ITerrainContentsGenerator terrainContentsGenerator = ((terrainGenerationMode == TerrainGenerationMode.FlatContinent || terrainGenerationMode == TerrainGenerationMode.FlatIsland) ? ((ITerrainContentsGenerator)new TerrainContentsGeneratorFlat(this)) : ((ITerrainContentsGenerator)new TerrainContentsGenerator21(this)));
			TerrainContentsGenerator = terrainContentsGenerator;
		}
		else
		{
			TerrainGenerationMode terrainGenerationMode2 = SubsystemGameInfo.WorldSettings.TerrainGenerationMode;
			ITerrainContentsGenerator terrainContentsGenerator2;
			if (terrainGenerationMode2 == TerrainGenerationMode.FlatContinent || terrainGenerationMode2 == TerrainGenerationMode.FlatIsland)
			{
				terrainContentsGenerator2 = new TerrainContentsGeneratorFlat(this);
			}
			else
			{
				terrainContentsGenerator2 = ((stream != GameMode.Embrace) ? ((ITerrainContentsGenerator)new TerrainContentsGenerator23(this)) : ((ITerrainContentsGenerator)new TerrainContentsGeneratorEmbrace(this)));
			}
			TerrainContentsGenerator = terrainContentsGenerator2;
		}
	}

	public void SaveChunk()
	{
		if (TerrainUpdater == null)
		{
			return;
		}
		TerrainUpdater.UpdateEvent.WaitOne();
		try
		{
			TerrainChunk[] valuesDictionary = Terrain.AllocatedChunks;
			foreach (TerrainChunk valuesDictionary3 in valuesDictionary)
			{
				TerrainSerializer.SaveChunk(valuesDictionary3);
			}
		}
		finally
		{
			if (TerrainUpdater != null)
			{
				TerrainUpdater.UpdateEvent.Set();
			}
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		SaveChunk();
	}

	public override void Dispose()
	{
		if (TerrainUpdater != null)
		{
			try
			{
				TerrainUpdater.Dispose();
			}
			catch (Exception ex)
			{
				if (SettingsManager.DebugMode)
				{
					Log.Warning("Error disposing TerrainUpdater: " + ex.Message);
				}
			}
			TerrainUpdater = null;
		}
		if (TerrainRenderer != null)
		{
			try
			{
				TerrainRenderer.Dispose();
			}
			catch
			{
			}
			TerrainRenderer = null;
		}
		if (TerrainSerializer != null)
		{
			try
			{
				TerrainSerializer.Dispose();
			}
			catch
			{
			}
			TerrainSerializer = null;
		}
		BlockGeometryGenerator = null;
		TerrainContentsGenerator = null;
		if (Terrain != null)
		{
			try
			{
				Terrain.Dispose();
			}
			catch
			{
			}
			Terrain = null;
		}
		if (m_modifiedCells != null)
		{
			m_modifiedCells.Clear();
			m_modifiedCells = null;
		}
		if (m_modifiedList != null)
		{
			m_modifiedList.Clear();
			m_modifiedList = null;
		}
		if (m_cellsToSend != null)
		{
			m_cellsToSend.Clear();
			m_cellsToSend = null;
		}
		if (m_dropValues != null)
		{
			m_dropValues.Clear();
			m_dropValues = null;
		}
	}

	public SubsystemTerrain()
	{
		m_modifiedCells = new HashSet<Point3>();
		m_modifiedList = new DynamicArray<Point3>();
		m_cellsToSend = new DynamicArray<Point3>();
		m_dropValues = new List<BlockDropValue>();
		primitivesRenderer = new PrimitivesRenderer3D();
		m_stopwatch = new Stopwatch();
	}

	static SubsystemTerrain()
	{
		TerrainRenderingEnabled = true;
		m_neighborOffsets = new Point3[7]
		{
			new Point3(0, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(1, 0, 0),
			new Point3(0, -1, 0),
			new Point3(0, 1, 0),
			new Point3(0, 0, -1),
			new Point3(0, 0, 1)
		};
		m_drawOrders = new int[2] { 0, 100 };
	}

	public void ChangeCellFluid(int x, int y, int z, int value, bool updateModificationCounter = true, ComponentMiner miner = null)
	{
		int cellValueFast = Terrain.GetCellValueFast(x, y, z);
		int num = Terrain.ExtractContents(cellValueFast);
		int num2 = Terrain.ExtractContents(value);
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		bool flag = true;
		if (num == num2)
		{
			int light = Terrain.ExtractLight(cellValueFast);
			value = Terrain.ReplaceLight(value, light);
			flag = false;
		}
		else
		{
			value = Terrain.ReplaceLight(value, 0);
		}
		if (cellValueFast == value)
		{
			return;
		}
		Terrain.SetCellValueFast(x, y, z, value);
		if (chunkAtCell != null)
		{
			if (updateModificationCounter)
			{
				chunkAtCell.ModificationCounter++;
			}
			if (flag)
			{
				chunkAtCell.IsFluidUpdateOnly = true;
				TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
			}
		}
		m_modifiedCells.Add(new Point3(x, y, z));
		if (CommonLib.WorkType != WorkType.Client)
		{
			m_networkBatchCoords.Add(new Point3(x, y, z));
			m_networkBatchValues.Add(value);
			if (m_networkBatchCoords.Count >= 200)
			{
				FlushNetworkBatch();
			}
		}
		try
		{
			if (num == num2)
			{
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					blockBehaviors[i].OnBlockModified(value, cellValueFast, x, y, z);
				}
				return;
			}
			if (num != 0)
			{
				SubsystemBlockBehavior[] blockBehaviors2 = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
				for (int j = 0; j < blockBehaviors2.Length; j++)
				{
					blockBehaviors2[j].OnBlockRemoved(cellValueFast, value, x, y, z);
				}
			}
			if (num2 != 0)
			{
				SubsystemBlockBehavior[] blockBehaviors3 = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
				for (int k = 0; k < blockBehaviors3.Length; k++)
				{
					blockBehaviors3[k].OnBlockAdded(value, cellValueFast, x, y, z);
					blockBehaviors3[k].OnBlockAdded(value, cellValueFast, x, y, z, miner);
				}
			}
		}
		catch (Exception ex)
		{
			if (SettingsManager.DebugMode)
			{
				Log.Warning($"Error in ChangeCellFluid at {x},{y},{z}: {ex.Message}");
			}
		}
	}

	private void FlushNetworkBatch()
	{
		if (m_networkBatchCoords.Count <= 0 || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		if (m_networkBatchCoords.Count > 10 && Time.RealTime - m_lastSpamLogTime > 2.0)
		{
			int white = m_networkBatchValues[0];
			if (SettingsManager.DebugMode)
			{
				NetNode.LogNetworkIssue($"=== SPAM ({m_networkBatchCoords.Count} BLOCKS) ===\nID block: {Terrain.ExtractContents(white)}\nCalls:\n{m_spamTrackerTrace}");
			}
			m_lastSpamLogTime = Time.RealTime;
		}
		SubsystemTerrainPackage font = new SubsystemTerrainPackage(m_networkBatchCoords, m_networkBatchValues, request: false);
		CommonLib.Net.QueuePackage(font);
		m_networkBatchCoords.Clear();
		m_networkBatchValues.Clear();
	}

	public void ChangeCellNet(int x, int y, int z, int value, bool updateModificationCounter, ComponentMiner miner, bool sendToNetwork)
	{
		int cellValueFast = Terrain.GetCellValueFast(x, y, z);
		value = Terrain.ReplaceLight(value, 0);
		cellValueFast = Terrain.ReplaceLight(cellValueFast, 0);
		if (value == cellValueFast)
		{
			return;
		}
		Terrain.SetCellValueFast(x, y, z, value);
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null)
		{
			if (updateModificationCounter)
			{
				chunkAtCell.ModificationCounter++;
			}
			if (miner != null)
			{
				chunkAtCell.IsFluidUpdateOnly = false;
			}
			TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
		}
		m_modifiedCells.Add(new Point3(x, y, z));
		if ((CommonLib.WorkType != WorkType.Client) & sendToNetwork)
		{
			if (m_networkBatchCoords.Count == 0)
			{
				m_spamTrackerTrace = new StackTrace(2, fNeedFileInfo: false).ToString();
			}
			m_networkBatchCoords.Add(new Point3(x, y, z));
			m_networkBatchValues.Add(value);
			if (m_networkBatchCoords.Count >= 200)
			{
				FlushNetworkBatch();
			}
		}
		int num = Terrain.ExtractContents(cellValueFast);
		int num2 = Terrain.ExtractContents(value);
		try
		{
			if (num2 != num)
			{
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					blockBehaviors[i].OnBlockRemoved(cellValueFast, value, x, y, z);
				}
				SubsystemBlockBehavior[] blockBehaviors2 = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
				for (int j = 0; j < blockBehaviors2.Length; j++)
				{
					blockBehaviors2[j].OnBlockAdded(value, cellValueFast, x, y, z);
					ComponentMiner miner2 = ((value == 0) ? null : miner);
					blockBehaviors2[j].OnBlockAdded(value, cellValueFast, x, y, z, miner2);
				}
			}
			else
			{
				SubsystemBlockBehavior[] blockBehaviors3 = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
				for (int k = 0; k < blockBehaviors3.Length; k++)
				{
					blockBehaviors3[k].OnBlockModified(value, cellValueFast, x, y, z);
				}
			}
		}
		catch (KeyNotFoundException ex)
		{
			int num3 = Terrain.ExtractContents(value);
			int num4 = Terrain.ExtractData(value);
			if (num4 != 0)
			{
				int value2 = Terrain.ReplaceData(value, 0);
				Terrain.SetCellValueFast(x, y, z, value2);
				m_modifiedCells.Add(new Point3(x, y, z));
			}
			if (SettingsManager.DebugMode)
			{
				Log.Warning("ChangeCellNet: cleared broken block data at {0},{1},{2}, contents={3}, oldData={4}. Ex={5}", x, y, z, num3, num4, ex.Message);
			}
		}
	}
}
