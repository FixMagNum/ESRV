using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemDistantTerrain : Subsystem, IDrawable
{
	private class ChunkMeshData
	{
		public VertexPositionNormalColor[] Vertices;

		public int[] Indices;

		public int UsedStep;

		public Point2 Coords;

		public VertexPositionColor[] SimpleVertices;
	}

	private SubsystemTerrain m_subsystemTerrain;

	private TerrainContentsGenerator23 m_generator;

	private Dictionary<Point2, DistantChunk> m_lodChunks;

	public SubsystemSky m_subsystemSky;

	private List<Point2> m_generationQueue;

	private HashSet<Point2> m_generationHashSet;

	private List<Point2> m_tmpToRemove;

	private double m_lastUpdateTime;

	private List<DistantChunk> m_tmpChunksToDraw;

	private SubsystemWeather.Season m_lastSeason;

	private Vector2 m_sortPosition;

	private List<Task<ChunkMeshData>> m_activeTasks;

	private List<Point2> m_tmpToAdd = new List<Point2>();

	private float[] m_lodDistancesSquared;

	private Comparison<Point2> m_sortDelegate;

	private int m_updateIndex;

	public int[] DrawOrders => new int[1] { 5 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		SubsystemWeather subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		if (subsystemWeather != null)
		{
			m_lastSeason = subsystemWeather.CurrentSeason;
		}
		m_generator = m_subsystemTerrain.TerrainContentsGenerator as TerrainContentsGenerator23;
		if (m_generator == null)
		{
			Log.Warning("SubsystemDistantTerrain: Generator is not TerrainContentsGenerator23. LOD disabled.");
		}
		m_sortDelegate = SortChunksByDistance;
		m_lodDistancesSquared = new float[6]
		{
			MathUtils.Sqr(192f),
			MathUtils.Sqr(480f),
			MathUtils.Sqr(1000f),
			MathUtils.Sqr(1900f),
			MathUtils.Sqr(3000f),
			MathUtils.Sqr(4200f)
		};
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (m_generator == null)
		{
			return;
		}
		SubsystemWeather subsystemWeather = m_subsystemSky.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		if (subsystemWeather != null && subsystemWeather.CurrentSeason != m_lastSeason)
		{
			m_lastSeason = subsystemWeather.CurrentSeason;
			foreach (Point2 key in m_lodChunks.Keys)
			{
				if (!m_generationHashSet.Contains(key))
				{
					m_generationQueue.Add(key);
					m_generationHashSet.Add(key);
				}
			}
		}
		if (Time.RealTime - m_lastUpdateTime > 0.2)
		{
			ManageLODChunks(camera.ViewPosition);
			m_lastUpdateTime = Time.RealTime;
		}
		for (int num = m_activeTasks.Count - 1; num >= 0; num--)
		{
			Task<ChunkMeshData> task = m_activeTasks[num];
			if (task.IsCompleted)
			{
				try
				{
					ChunkMeshData result = task.Result;
					if (result != null)
					{
						UploadMeshToGPU(result.Coords, result);
					}
				}
				catch (Exception ex)
				{
					Log.Warning("LOD Generation failed: " + ex.Message);
				}
				m_activeTasks.RemoveAt(num);
			}
		}
		int num2 = MathUtils.Clamp(Environment.ProcessorCount / 2, 2, 8);
		while (m_activeTasks.Count < num2 && m_generationQueue.Count > 0)
		{
			Point2 point = m_generationQueue[0];
			m_generationQueue.RemoveAt(0);
			m_generationHashSet.Remove(point);
			SubsystemWeather.Season season = m_lastSeason;
			Vector3 viewPosSnapshot = camera.ViewPosition;
			m_activeTasks.Add(Task.Run(() => CalculateChunkVertices(point, season, viewPosSnapshot)));
		}
		float num3 = m_subsystemSky.m_subsystemTimeOfDay.TimeOfDay * 2f * (float)Math.PI + (float)Math.PI;
		float num4 = (float)Math.Cos(num3);
		float x = (float)Math.Sin(num3);
		float y = MathUtils.Clamp(num4 * 2.5f, -1f, 1f);
		Vector3 vector = Vector3.Normalize(new Vector3(x, y, -0.2f));
		if (vector.Y < 0f)
		{
			vector = -vector;
		}
		if (m_subsystemSky.ViewUnderWaterDepth <= 0f && m_subsystemSky.ViewUnderMagmaDepth <= 0f && m_subsystemSky.ViewUnderQuicksandDepth <= 0f)
		{
			m_subsystemTerrain.TerrainRenderer.DrawDistantChunks(camera, m_lodChunks.Values, vector);
		}
	}

	private void ManageLODChunks(Vector3 viewPos)
	{
		float num = SettingsManager.VisibilityRange;
		float num2 = 128f;
		float num3 = SettingsManager.DistantChunksRange;
		if (num3 <= 0f)
		{
			if (m_lodChunks.Count <= 0)
			{
				return;
			}
			foreach (DistantChunk value3 in m_lodChunks.Values)
			{
				value3.Dispose();
			}
			m_lodChunks.Clear();
			m_generationQueue.Clear();
			m_generationHashSet.Clear();
			return;
		}
		float num4 = num + num3;
		float num5 = MathUtils.Sqr(num4 + 128f);
		int num6 = (int)Math.Floor(viewPos.X / num2);
		int num7 = (int)Math.Floor(viewPos.Z / num2);
		m_tmpToRemove.Clear();
		int num8 = Time.FrameIndex % 5;
		foreach (KeyValuePair<Point2, DistantChunk> lodChunk in m_lodChunks)
		{
			Point2 key = lodChunk.Key;
			DistantChunk value = lodChunk.Value;
			float num9 = ((float)key.X + 0.5f) * num2 - viewPos.X;
			float num10 = ((float)key.Y + 0.5f) * num2 - viewPos.Z;
			if (Math.Abs(num9) > num4 + 200f || Math.Abs(num10) > num4 + 200f)
			{
				m_tmpToRemove.Add(key);
				continue;
			}
			float num11 = num9 * num9 + num10 * num10;
			if (num11 > num5)
			{
				m_tmpToRemove.Add(key);
			}
			else if (SettingsManager.DistantChunksDetailLevel >= 2 && ((key.X ^ key.Y) & 7) % 5 == num8 && value.IsReady && !m_generationHashSet.Contains(key))
			{
				int num12 = CalculateStepForDistanceSquared(num11);
				if (value.LodStep != num12)
				{
					m_generationQueue.Add(key);
					m_generationHashSet.Add(key);
				}
			}
		}
		for (int i = 0; i < m_tmpToRemove.Count; i++)
		{
			Point2 point = m_tmpToRemove[i];
			if (m_lodChunks.TryGetValue(point, out var value2))
			{
				value2.Dispose();
				m_lodChunks.Remove(point);
			}
			if (m_generationHashSet.Contains(point))
			{
				m_generationQueue.Remove(point);
				m_generationHashSet.Remove(point);
			}
		}
		m_tmpToAdd.Clear();
		float num13 = MathUtils.Sqr(num4);
		int num14 = (int)Math.Ceiling(num4 / num2);
		for (int j = -num14; j <= num14; j++)
		{
			for (int k = -num14; k <= num14; k++)
			{
				Point2 point2 = new Point2(num6 + j, num7 + k);
				if (!m_lodChunks.ContainsKey(point2) && !m_generationHashSet.Contains(point2) && Vector2.DistanceSquared(v2: new Vector2(((float)point2.X + 0.5f) * num2, ((float)point2.Y + 0.5f) * num2), v1: viewPos.XZ) <= num13)
				{
					m_tmpToAdd.Add(point2);
				}
			}
		}
		if (m_tmpToAdd.Count > 0)
		{
			m_sortPosition = viewPos.XZ;
			m_tmpToAdd.Sort(m_sortDelegate);
			for (int l = 0; l < m_tmpToAdd.Count; l++)
			{
				Point2 point3 = m_tmpToAdd[l];
				m_lodChunks[point3] = new DistantChunk(point3);
				m_generationQueue.Add(point3);
				m_generationHashSet.Add(point3);
			}
		}
	}

	public SubsystemDistantTerrain()
	{
		m_tmpChunksToDraw = new List<DistantChunk>();
		m_activeTasks = new List<Task<ChunkMeshData>>();
		m_tmpToRemove = new List<Point2>();
		m_lodChunks = new Dictionary<Point2, DistantChunk>();
		m_generationQueue = new List<Point2>();
		m_generationHashSet = new HashSet<Point2>();
	}

	private float GetNoise(float x, float z)
	{
		return MathUtils.Sin(x * 12.9898f + z * 78.233f) * 43758.547f % 1f;
	}

	private void UploadMeshToGPU(Point2 coords, ChunkMeshData data)
	{
		if (!m_lodChunks.TryGetValue(coords, out var value))
		{
			return;
		}
		if (value.IsReady)
		{
			if (value.VertexBuffer != null)
			{
				value.VertexBuffer.Dispose();
			}
			if (value.IndexBuffer != null)
			{
				value.IndexBuffer.Dispose();
			}
			value.IsReady = false;
		}
		if (((data.Vertices != null && data.Vertices.Length != 0) || (data.SimpleVertices != null && data.SimpleVertices.Length != 0)) && data.Indices.Length != 0)
		{
			if (data.Vertices != null && data.Vertices.Length != 0)
			{
				value.VertexBuffer = new VertexBuffer(VertexPositionNormalColor.VertexDeclaration, data.Vertices.Length);
				value.VertexBuffer.SetData(data.Vertices, 0, data.Vertices.Length);
			}
			else
			{
				value.VertexBuffer = new VertexBuffer(VertexPositionColor.VertexDeclaration, data.SimpleVertices.Length);
				value.VertexBuffer.SetData(data.SimpleVertices, 0, data.SimpleVertices.Length);
			}
			value.IndexBuffer = new IndexBuffer(IndexFormat.ThirtyTwoBits, data.Indices.Length);
			value.IndexBuffer.SetData(data.Indices, 0, data.Indices.Length);
			value.IndexCount = data.Indices.Length;
			value.IsReady = true;
			value.LodStep = data.UsedStep;
		}
	}

	private int CalculateStepForDistance(float distance)
	{
		return CalculateStepForDistanceSquared(distance * distance);
	}

	private ChunkMeshData CalculateChunkVertices(Point2 coords, SubsystemWeather.Season currentSeason, Vector3 viewPos)
	{
		bool flag = SettingsManager.DistantChunksDetailLevel >= 4;
		DynamicArray<VertexPositionNormalColor> dynamicArray = (flag ? new DynamicArray<VertexPositionNormalColor>() : null);
		DynamicArray<VertexPositionColor> dynamicArray2 = ((!flag) ? new DynamicArray<VertexPositionColor>() : null);
		DynamicArray<int> dynamicArray3 = new DynamicArray<int>();
		int num = coords.X * 128;
		int num2 = coords.Y * 128;
		float x = (float)num + 64f;
		float num3 = (float)num2 + 64f;
		float distance = Vector2.Distance(new Vector2(x, num3), viewPos.XZ);
		int num4 = CalculateStepForDistance(distance);
		float num5 = (float)Math.Floor(m_generator.CalculateHeight(num, num2));
		float num6 = (float)Math.Floor(m_generator.CalculateHeight(num + 128, num2));
		float num7 = (float)Math.Floor(m_generator.CalculateHeight(num, num2 + 128));
		float num8 = (float)Math.Floor(m_generator.CalculateHeight(num + 128, num2 + 128));
		float num9 = (float)Math.Floor(m_generator.CalculateHeight(x, num3));
		bool flag2 = Math.Abs(num5 - num6) < 2.5f && Math.Abs(num5 - num7) < 2.5f && Math.Abs(num5 - num8) < 2.5f && Math.Abs(num5 - num9) < 2.5f;
		bool flag3 = SettingsManager.DistantChunksDetailLevel >= 2 && ((num4 <= 32) | flag2);
		float num10 = m_generator.OceanLevel;
		float brightness = 0.8f;
		bool flag4 = SettingsManager.DistantChunksDetailLevel >= 1;
		TerrainChunk terrainChunk = null;
		Point2? point = null;
		try
		{
			if (flag3)
			{
				for (int i = 0; i < 128; i += num4)
				{
					for (int j = 0; j < 128; j += num4)
					{
						float num11 = num + i;
						float num12 = num2 + j;
						int num13 = (int)MathUtils.Floor(num11) >> 4;
						int num14 = (int)MathUtils.Floor(num12) >> 4;
						TerrainChunk terrainChunk2 = m_subsystemTerrain.Terrain.GetChunkAtCoords(num13, num14);
						bool flag5 = false;
						if (terrainChunk2 == null)
						{
							if (terrainChunk != null && (terrainChunk.Coords.X != num13 || terrainChunk.Coords.Y != num14))
							{
								terrainChunk.Dispose();
								terrainChunk = null;
							}
							if (!point.HasValue || point.Value.X != num13 || point.Value.Y != num14)
							{
								if (terrainChunk != null)
								{
									terrainChunk2 = terrainChunk;
									flag5 = true;
								}
								else
								{
									TerrainChunk terrainChunk3 = new TerrainChunk(m_subsystemTerrain.Terrain, num13, num14);
									if (m_subsystemTerrain.TerrainSerializer.LoadChunk(terrainChunk3))
									{
										terrainChunk = terrainChunk3;
										terrainChunk2 = terrainChunk3;
										flag5 = true;
										point = null;
									}
									else
									{
										terrainChunk3.Dispose();
										point = new Point2(num13, num14);
									}
								}
							}
						}
						TerrainContentsGenerator23.TerrainLODPoint lodPoint = m_generator.CalculateLODPoint(num11, num12);
						float height = lodPoint.Height;
						float num15 = (float)Math.Floor(height);
						int num16 = lodPoint.Temperature;
						int num17 = lodPoint.Humidity;
						Block block = null;
						int cellValue = 0;
						int light = 0;
						if (terrainChunk2 != null && (flag5 || terrainChunk2.ModificationCounter > 0))
						{
							int x2 = (int)MathUtils.Floor(num11) & 0xF;
							int z = (int)MathUtils.Floor(num12) & 0xF;
							int num18 = ((!flag5) ? terrainChunk2.GetTopHeightFast(x2, z) : terrainChunk2.CalculateTopmostCellHeight(x2, z));
							num15 = num18;
							int index = TerrainChunk.CalculateCellIndex(x2, num18, z);
							int cellValueFast = terrainChunk2.GetCellValueFast(index);
							int num19 = Terrain.ExtractContents(cellValueFast);
							if (num19 != 0)
							{
								block = BlocksManager.Blocks[num19];
								cellValue = cellValueFast;
								light = CalculateNeighborLight(terrainChunk2, x2, num18, z);
								if (flag5)
								{
									int shaftValueFast = terrainChunk2.GetShaftValueFast(x2, z);
									num16 = Terrain.ExtractTemperature(shaftValueFast);
									num17 = Terrain.ExtractHumidity(shaftValueFast);
								}
								else
								{
									num16 = terrainChunk2.GetTemperatureFast(x2, z);
									num17 = terrainChunk2.GetHumidityFast(x2, z);
								}
							}
						}
						bool flag6 = num15 < num10;
						float num20 = (flag6 ? num10 : num15);
						int num21 = num16;
						int hum = num17;
						Vector3 vector = new Vector3(num11, num20, num12);
						int num22 = m_generator.CalculateTemperature(num11, num12);
						int num23 = m_generator.CalculateHumidity(num11, num12);
						float num24 = m_generator.CalculateMountainRangeFactor(num11, num12);
						bool flag7 = SubsystemWeather.IsPlaceFrozen(((block != null) | flag5) ? num21 : num22, (int)MathUtils.Max(height, num10));
						int num25 = 3;
						if ((num22 > 8 && num23 < 8 && num24 < 0.97f) || (MathUtils.Abs(m_generator.CalculateOceanShoreDistance(num11, num12)) < 16f && num24 < 0.97f))
						{
							num25 = 4;
						}
						int terrainType;
						if (num25 == 4)
						{
							terrainType = ((num22 > 4 && num22 < 7) ? 6 : 7);
						}
						else
						{
							int num26 = num22 / 4;
							if ((num20 > 120f) & flag7)
							{
								terrainType = 62;
							}
							else if ((num20 < 66f || num20 == 84f + (float)num26 || num20 == 103f + (float)num26) && num23 == 9 && num22 % 6 == 1)
							{
								terrainType = 66;
							}
							else
							{
								terrainType = ((num23 <= 8 || num23 % 2 != 0 || num22 % 3 != 0) ? 2 : 72);
							}
						}
						Color color = CalculateVertexColor(block != null, currentSeason, brightness, num11, num12, block, cellValue, (int)num11, (int)num20, (int)num12, num21, hum, num10, lodPoint, terrainType, flag7, 0f, num11, num20, num12, num22, num23, light);
						Vector3 vector2 = new Vector3(num11, num20, num12 + (float)num4);
						Vector3 vector3 = new Vector3(num11 + (float)num4, num20, num12 + (float)num4);
						Vector3 vector4 = new Vector3(num11 + (float)num4, num20, num12);
						int num27 = (flag ? dynamicArray.Count : dynamicArray2.Count);
						if (flag)
						{
							Vector3 unitY = Vector3.UnitY;
							dynamicArray.Add(new VertexPositionNormalColor(vector, unitY, color));
							dynamicArray.Add(new VertexPositionNormalColor(vector2, unitY, color));
							dynamicArray.Add(new VertexPositionNormalColor(vector3, unitY, color));
							dynamicArray.Add(new VertexPositionNormalColor(vector4, unitY, color));
						}
						else
						{
							dynamicArray2.Add(new VertexPositionColor(vector, color));
							dynamicArray2.Add(new VertexPositionColor(vector2, color));
							dynamicArray2.Add(new VertexPositionColor(vector3, color));
							dynamicArray2.Add(new VertexPositionColor(vector4, color));
						}
						dynamicArray3.Add(num27);
						dynamicArray3.Add(num27 + 1);
						dynamicArray3.Add(num27 + 2);
						dynamicArray3.Add(num27);
						dynamicArray3.Add(num27 + 2);
						dynamicArray3.Add(num27 + 3);
						if (!flag6)
						{
							if (flag)
							{
								CheckAndBuildWall(num11, num12 - (float)num4, num20, num4, vector, vector4, new Vector3(0f, 0f, -1f), color, dynamicArray, dynamicArray3);
								CheckAndBuildWall(num11, num12 + (float)num4, num20, num4, vector3, vector2, new Vector3(0f, 0f, 1f), color, dynamicArray, dynamicArray3);
								CheckAndBuildWall(num11 + (float)num4, num12, num20, num4, vector4, vector3, new Vector3(1f, 0f, 0f), color, dynamicArray, dynamicArray3);
								CheckAndBuildWall(num11 - (float)num4, num12, num20, num4, vector2, vector, new Vector3(-1f, 0f, 0f), color, dynamicArray, dynamicArray3);
							}
							else
							{
								CheckAndBuildWall(num11, num12 - (float)num4, num20, num4, vector, vector4, color, dynamicArray2, dynamicArray3);
								CheckAndBuildWall(num11, num12 + (float)num4, num20, num4, vector3, vector2, color, dynamicArray2, dynamicArray3);
								CheckAndBuildWall(num11 + (float)num4, num12, num20, num4, vector4, vector3, color, dynamicArray2, dynamicArray3);
								CheckAndBuildWall(num11 - (float)num4, num12, num20, num4, vector2, vector, color, dynamicArray2, dynamicArray3);
							}
						}
					}
				}
			}
			else
			{
				int num28 = 16;
				float num29 = 8f;
				float num30 = 8f;
				for (int k = 0; k <= num28; k++)
				{
					for (int l = 0; l <= num28; l++)
					{
						float num31 = (float)num + (float)l * num29;
						float num32 = (float)num2 + (float)k * num29;
						bool flag8 = false;
						if (flag4)
						{
							int num33 = (int)MathUtils.Floor(num31) >> 4;
							int num34 = (int)MathUtils.Floor(num32) >> 4;
							TerrainChunk terrainChunk4 = m_subsystemTerrain.Terrain.GetChunkAtCoords(num33, num34);
							bool flag9 = false;
							if (terrainChunk4 == null)
							{
								if (terrainChunk != null && (terrainChunk.Coords.X != num33 || terrainChunk.Coords.Y != num34))
								{
									terrainChunk.Dispose();
									terrainChunk = null;
								}
								if (!point.HasValue || point.Value.X != num33 || point.Value.Y != num34)
								{
									if (terrainChunk != null)
									{
										terrainChunk4 = terrainChunk;
										flag9 = true;
									}
									else
									{
										TerrainChunk terrainChunk5 = new TerrainChunk(m_subsystemTerrain.Terrain, num33, num34);
										if (m_subsystemTerrain.TerrainSerializer.LoadChunk(terrainChunk5))
										{
											terrainChunk = terrainChunk5;
											terrainChunk4 = terrainChunk5;
											flag9 = true;
											point = null;
										}
										else
										{
											terrainChunk5.Dispose();
											point = new Point2(num33, num34);
										}
									}
								}
							}
							if (terrainChunk4 != null && (flag9 || terrainChunk4.ModificationCounter > 0))
							{
								try
								{
									int x3 = (int)MathUtils.Floor(num31) & 0xF;
									int z2 = (int)MathUtils.Floor(num32) & 0xF;
									int num35 = ((!flag9) ? terrainChunk4.GetTopHeightFast(x3, z2) : terrainChunk4.CalculateTopmostCellHeight(x3, z2));
									int index2 = TerrainChunk.CalculateCellIndex(x3, num35, z2);
									int num36 = Terrain.ExtractContents(terrainChunk4.GetCellValueFast(index2));
									if (num36 != 0)
									{
										int temp = m_generator.CalculateTemperature(num31, num32);
										int hum2 = m_generator.CalculateHumidity(num31, num32);
										TerrainContentsGenerator23.TerrainLODPoint lodPoint2 = m_generator.CalculateLODPoint(num31, num32);
										Block block2 = BlocksManager.Blocks[num36];
										int cellValueFast2 = terrainChunk4.GetCellValueFast(index2);
										Vector3 position = new Vector3(num31, num35, num32);
										int light2 = CalculateNeighborLight(terrainChunk4, x3, num35, z2);
										Color color2 = CalculateVertexColor(isHighDetail: true, currentSeason, brightness, num31, num32, block2, cellValueFast2, (int)num31, num35, (int)num32, temp, hum2, num10, lodPoint2, 0, isFrozen: false, 0f, num31, num35, num32, lodPoint2.Temperature, lodPoint2.Humidity, light2);
										if (flag)
										{
											dynamicArray.Add(new VertexPositionNormalColor(position, Vector3.UnitY, color2));
										}
										else
										{
											dynamicArray2.Add(new VertexPositionColor(position, color2));
										}
										flag8 = true;
									}
								}
								catch
								{
									flag8 = false;
								}
							}
						}
						if (!flag8)
						{
							float num37 = (float)Math.Floor(num31 / num30) * num30;
							float num38 = (float)Math.Floor(num32 / num30) * num30;
							TerrainContentsGenerator23.TerrainLODPoint lodPoint3 = m_generator.CalculateLODPoint(num31, num32);
							float num39 = (float)Math.Floor(lodPoint3.Height / num30) * num30;
							float x4 = m_generator.CalculateOceanShoreDistance(num31, num32);
							int num40 = (int)MathUtils.Floor(num31);
							int num41 = (int)MathUtils.Floor(num32);
							int num42 = (int)num39;
							int num43 = m_generator.CalculateTemperature(num40, num41);
							int num44 = m_generator.CalculateHumidity(num40, num41);
							float num45 = m_generator.CalculateMountainRangeFactor(num40, num41);
							bool flag10 = SubsystemWeather.IsPlaceFrozen(num43, (int)MathUtils.Max(lodPoint3.Height, num10));
							float num46 = (float)Math.Floor(m_generator.CalculateHeight(num37, num38) / num30) * num30;
							float num47 = (float)Math.Floor(m_generator.CalculateHeight(num37 + num30, num38) / num30) * num30;
							float num48 = (float)Math.Floor(m_generator.CalculateHeight(num37, num38 + num30) / num30) * num30;
							float slope = (float)Math.Sqrt(MathUtils.Sqr(num47 - num46) + MathUtils.Sqr(num48 - num46));
							Vector3 normal = Vector3.UnitY;
							if (flag && SettingsManager.DistantChunksDetailLevel > 0)
							{
								normal = Vector3.Normalize(new Vector3(num46 - num47, 8f, num46 - num47));
							}
							int num49 = 3;
							if ((num43 > 8 && num44 < 8 && num45 < 0.97f) || (MathUtils.Abs(x4) < 16f && num45 < 0.97f))
							{
								num49 = 4;
							}
							int terrainType2;
							if (num49 == 4)
							{
								terrainType2 = ((num43 > 4 && num43 < 7) ? 6 : 7);
							}
							else
							{
								int num50 = num43 / 4;
								if ((num42 > 120) & flag10)
								{
									terrainType2 = 62;
								}
								else if ((num42 < 66 || num42 == 84 + num50 || num42 == 103 + num50) && num44 == 9 && num43 % 6 == 1)
								{
									terrainType2 = 66;
								}
								else
								{
									terrainType2 = ((num44 <= 8 || num44 % 2 != 0 || num43 % 3 != 0) ? 2 : 72);
								}
							}
							float y = ((lodPoint3.Height < num10) ? num10 : num39);
							Color color3 = CalculateVertexColor(isHighDetail: false, currentSeason, brightness, num37, num38, null, 0, 0, 0, 0, 0, 0, num10, lodPoint3, terrainType2, flag10, slope, num37, num39, num38, num43, num44, 0);
							if (flag)
							{
								dynamicArray.Add(new VertexPositionNormalColor(new Vector3(num37, y, num38), normal, color3));
							}
							else
							{
								dynamicArray2.Add(new VertexPositionColor(new Vector3(num37, y, num38), color3));
							}
						}
					}
				}
				for (int m = 0; m < num28; m++)
				{
					for (int n = 0; n < num28; n++)
					{
						int num51 = m * (num28 + 1) + n;
						int item = num51 + 1;
						int num52 = (m + 1) * (num28 + 1) + n;
						int item2 = num52 + 1;
						dynamicArray3.Add(num51);
						dynamicArray3.Add(num52);
						dynamicArray3.Add(item);
						dynamicArray3.Add(item);
						dynamicArray3.Add(num52);
						dynamicArray3.Add(item2);
					}
				}
			}
		}
		finally
		{
			terrainChunk?.Dispose();
		}
		VertexPositionNormalColor[] array = null;
		VertexPositionColor[] array2 = null;
		if (flag)
		{
			array = new VertexPositionNormalColor[dynamicArray.Count];
			if (dynamicArray.Count > 0)
			{
				Array.Copy(dynamicArray.Array, array, dynamicArray.Count);
			}
		}
		else
		{
			array2 = new VertexPositionColor[dynamicArray2.Count];
			if (dynamicArray2.Count > 0)
			{
				Array.Copy(dynamicArray2.Array, array2, dynamicArray2.Count);
			}
		}
		int[] array3 = new int[dynamicArray3.Count];
		if (dynamicArray3.Count > 0)
		{
			Array.Copy(dynamicArray3.Array, array3, dynamicArray3.Count);
		}
		return new ChunkMeshData
		{
			Coords = coords,
			Vertices = array,
			SimpleVertices = array2,
			Indices = array3,
			UsedStep = num4
		};
	}

	private Color ApplySeasonalGrassTint(SubsystemWeather.Season season, Color color)
	{
		return season switch
		{
			SubsystemWeather.Season.EarlyAutumn => Color.Lerp(color, new Color(153, 102, 51), 0.5f), 
			SubsystemWeather.Season.Autumn => Color.Lerp(color, new Color(128, 64, 48), 0.7f), 
			SubsystemWeather.Season.LateAutumn => Color.Lerp(color, new Color(118, 64, 48), 0.8f), 
			SubsystemWeather.Season.Anabiosis => Color.Lerp(color, new Color(88, 64, 48), 0.7f), 
			SubsystemWeather.Season.Winter => Color.Lerp(color, new Color(88, 64, 48), 0.9f), 
			SubsystemWeather.Season.LateWinter => Color.Lerp(color, new Color(88, 64, 48), 0.65f), 
			SubsystemWeather.Season.EarlySpring => Color.Lerp(color, new Color(88, 64, 48), 0.4f), 
			SubsystemWeather.Season.Spring => Color.Lerp(color, new Color(88, 64, 48), 0.3f), 
			SubsystemWeather.Season.LateSpring => Color.Lerp(color, new Color(122, 255, 110), 0.5f), 
			SubsystemWeather.Season.Summer => color, 
			_ => color, 
		};
	}

	private int SortGenerationQueue(Point2 p1, Point2 p2)
	{
		float num = ((float)p1.X + 0.5f) * 128f;
		float num2 = ((float)p1.Y + 0.5f) * 128f;
		float num3 = (num - m_sortPosition.X) * (num - m_sortPosition.X) + (num2 - m_sortPosition.Y) * (num2 - m_sortPosition.Y);
		float num4 = ((float)p2.X + 0.5f) * 128f;
		float num5 = ((float)p2.Y + 0.5f) * 128f;
		float value = (num4 - m_sortPosition.X) * (num4 - m_sortPosition.X) + (num5 - m_sortPosition.Y) * (num5 - m_sortPosition.Y);
		return num3.CompareTo(value);
	}

	private Color GetColorForBlockBySound(Block block, int cellValue, int temp, int hum, SubsystemWeather.Season season)
	{
		switch (block.GetSoundMaterialName(m_subsystemTerrain, cellValue))
		{
		case "Splashing":
		{
			Color baseColor2 = BlockColorsMap.WaterColorsMap.Lookup(temp, hum);
			return BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(season, baseColor2);
		}
		case "Sand":
			return BlockColorsMap.SandColorsMap.LookupSand(temp, hum);
		case "Plant":
		{
			if (block is LeavesBlock)
			{
				BlockColorsMap blockColorsMap = BlockColorsMap.OakLeavesColorsMap;
				if (block is BirchLeavesBlock)
				{
					blockColorsMap = BlockColorsMap.BirchLeavesColorsMap;
				}
				else if (block is SpruceLeavesBlock)
				{
					blockColorsMap = BlockColorsMap.SpruceLeavesColorsMap;
				}
				else if (block is TallSpruceLeavesBlock)
				{
					blockColorsMap = BlockColorsMap.TallSpruceLeavesColorsMap;
				}
				else if (block is MimosaLeavesBlock)
				{
					blockColorsMap = BlockColorsMap.MimosaLeavesColorsMap;
				}
				Color baseColor = blockColorsMap.Lookup(temp, hum);
				return blockColorsMap.LookupSeasonalLeavesColor(season, baseColor, block.BlockIndex);
			}
			Color color = BlockColorsMap.GrassColorsMap.Lookup(temp, hum);
			return ApplySeasonalGrassTint(season, color);
		}
		case "Dirt":
			return BlockColorsMap.GrassColorsMap.LookupSeasonalDirtColor(season, m_subsystemTerrain.Terrain, 0, 0, 0);
		case "Stone":
		case "Metal":
			return new Color(100, 100, 100);
		case "Snow":
			return Color.White;
		case "Wood":
			return new Color(137, 104, 55);
		case "Fat":
			return new Color(181, 159, 46);
		case "Bones":
			return new Color(140, 44, 45);
		case "Soft":
			return new Color(215, 212, 201);
		case "Glass":
			return new Color(200, 230, 255);
		default:
			return Color.DarkGreen;
		}
	}

	private void CheckAndBuildWall(float neighborX, float neighborZ, float myY, int step, Vector3 vTop1, Vector3 vTop2, Color color, DynamicArray<VertexPositionColor> vertices, DynamicArray<int> indices)
	{
		float num = (float)Math.Floor(m_generator.CalculateHeight(neighborX, neighborZ));
		if (myY > num)
		{
			Color color2;
			if (myY - num <= 1.5f)
			{
				color2 = color;
			}
			else
			{
				byte a = color.A;
				color2 = Color.MultiplyColorOnly(color, 0.85f);
				color2.A = a;
			}
			Vector3 position = new Vector3(vTop1.X, num, vTop1.Z);
			Vector3 position2 = new Vector3(vTop2.X, num, vTop2.Z);
			int count = vertices.Count;
			vertices.Add(new VertexPositionColor(vTop1, color2));
			vertices.Add(new VertexPositionColor(position, color2));
			vertices.Add(new VertexPositionColor(position2, color2));
			vertices.Add(new VertexPositionColor(vTop2, color2));
			indices.Add(count);
			indices.Add(count + 1);
			indices.Add(count + 2);
			indices.Add(count);
			indices.Add(count + 2);
			indices.Add(count + 3);
		}
	}

	private void CheckAndBuildWall(float neighborX, float neighborZ, float myY, int step, Vector3 vTop1, Vector3 vTop2, Vector3 normal, Color color, DynamicArray<VertexPositionNormalColor> vertices, DynamicArray<int> indices)
	{
		float num = (float)Math.Floor(m_generator.CalculateHeight(neighborX, neighborZ));
		if (myY > num)
		{
			Vector3 normal2;
			Color color2;
			if (myY - num <= 1.5f)
			{
				normal2 = Vector3.UnitY;
				color2 = color;
			}
			else
			{
				normal2 = normal;
				byte a = color.A;
				color2 = Color.MultiplyColorOnly(color, 0.85f);
				color2.A = a;
			}
			Vector3 position = new Vector3(vTop1.X, num, vTop1.Z);
			Vector3 position2 = new Vector3(vTop2.X, num, vTop2.Z);
			int count = vertices.Count;
			vertices.Add(new VertexPositionNormalColor(vTop1, normal2, color2));
			vertices.Add(new VertexPositionNormalColor(position, normal2, color2));
			vertices.Add(new VertexPositionNormalColor(position2, normal2, color2));
			vertices.Add(new VertexPositionNormalColor(vTop2, normal2, color2));
			indices.Add(count);
			indices.Add(count + 1);
			indices.Add(count + 2);
			indices.Add(count);
			indices.Add(count + 2);
			indices.Add(count + 3);
		}
	}

	private Color CalculateVertexColor(bool isHighDetail, SubsystemWeather.Season currentSeason, float brightness, float noiseCoordX, float noiseCoordZ, Block block, int cellValue, int x, int y, int z, int temp, int hum, float oceanLevel, TerrainContentsGenerator23.TerrainLODPoint lodPoint, int terrainType, bool isFrozen, float slope, float lodX, float lodY, float lodZ, int lodTemp, int lodHum, int light)
	{
		if ((!(block is FocalorsWaterBlock) || oceanLevel <= 0f) & isHighDetail)
		{
			Color white = Color.White;
			if (block is GrassBlock)
			{
				white = BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, m_subsystemTerrain.Terrain, x, y, z);
			}
			else
			{
				white = ((!(block is FocalorsWaterBlock)) ? GetColorForBlockBySound(block, cellValue, temp, hum, currentSeason) : BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(currentSeason, BlockColorsMap.WaterColorsMap.Lookup(temp, hum)));
			}
			uint num = MathUtils.Hash((uint)x);
			uint num2 = MathUtils.Hash((uint)z);
			int num3 = (int)(MathUtils.Hash(num ^ (num2 * 49157)) % 5);
			float num4 = 0f;
			switch (num3)
			{
			case 0:
				num4 = -0.08f;
				break;
			case 1:
				num4 = -0.04f;
				break;
			case 2:
				num4 = 0f;
				break;
			case 3:
				num4 = 0.04f;
				break;
			case 4:
				num4 = 0.08f;
				break;
			}
			white = Color.MultiplyColorOnly(white, 1f + num4);
			float num5 = ((block is LeavesBlock || block is GrassBlock) ? 0.74f : 1f);
			Color result = Color.MultiplyColorOnly(white, brightness * num5);
			result.A = (byte)MathUtils.Clamp(light * 23, 0, 255);
			return result;
		}
		if (lodPoint.Height < oceanLevel)
		{
			int num6 = MathUtils.Clamp(lodPoint.Temperature + SubsystemWeather.GetTemperatureAdjustmentAtHeight((int)lodPoint.Height), 0, 15);
			int humidity = lodPoint.Humidity;
			Color c = BlockColorsMap.WaterColorsMap.Lookup(num6, humidity);
			float num7 = MathUtils.Saturate((float)num6 / 15f);
			float num8 = MathUtils.Saturate((float)humidity / 15f);
			float f = MathUtils.Lerp(0f, 1f, (1f - num8) * num7);
			Color c2 = new Color(204, 153, 51);
			c = Color.Lerp(c, c2, f);
			c = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(currentSeason, c);
			float num9 = oceanLevel - lodPoint.Height;
			float f2 = MathUtils.Clamp(1f - num9 * 0.05f, 0.4f, 1f);
			Color result2 = Color.MultiplyColorOnly(c, MathUtils.Lerp(0.78f, 1f, f2) * brightness);
			result2.A = (byte)MathUtils.Clamp(light * 23, 0, 255);
			return result2;
		}
		Color white2;
		bool flag5 = false;
		if (isFrozen && terrainType != 62)
		{
			white2 = Color.White;
			white2 = Color.MultiplyColorOnly(white2, brightness * 1.15f);
		}
		else if (terrainType <= 62)
		{
			switch (terrainType)
			{
			case 3:
				white2 = new Color(100, 100, 100);
				white2 = Color.MultiplyColorOnly(white2, brightness);
				break;
			case 4:
			case 5:
				white2 = Color.White;
				flag5 = true;
				break;
			case 6:
				white2 = new Color(109, 103, 94);
				white2 = Color.MultiplyColorOnly(white2, brightness);
				break;
			case 7:
				int temperature = MathUtils.Clamp(lodTemp + SubsystemWeather.GetTemperatureAdjustmentAtHeight((int)lodPoint.Height), 0, 15);
				white2 = BlockColorsMap.SandColorsMap.LookupSand(temperature, lodHum);
				white2 = Color.MultiplyColorOnly(white2, brightness);
				break;
			case 62:
				white2 = new Color(225, 240, 255);
				break;
			default:
				white2 = Color.White;
				flag5 = true;
				break;
			}
		}
		else if (terrainType == 66)
		{
			white2 = new Color(210, 215, 210);
			white2 = Color.MultiplyColorOnly(white2, brightness);
		}
		else
		{
			if (terrainType != 72)
			{
				white2 = Color.White;
				flag5 = true;
			}
			else
			{
				white2 = new Color(135, 142, 146);
				white2 = Color.MultiplyColorOnly(white2, brightness);
			}
		}
		if (flag5)
		{
			if (slope > 8f)
			{
				white2 = new Color(100, 100, 100);
				white2 = Color.MultiplyColorOnly(white2, brightness);
			}
			else
			{
				int x2 = lodTemp + SubsystemWeather.GetTemperatureAdjustmentAtHeight((int)lodY);
				x2 = MathUtils.Clamp(x2, 0, 15);
				Color c3 = BlockColorsMap.GrassColorsMap.Lookup(x2, lodHum);
				float num10 = MathUtils.Saturate((float)x2 / 15f);
				float num11 = MathUtils.Saturate((float)lodHum / 15f);
				float f3 = MathUtils.Lerp(0f, 1f, (1f - num11) * num10);
				Color c4 = new Color(204, 153, 51);
				Color color = Color.Lerp(c3, c4, f3);
				white2 = ApplySeasonalGrassTint(currentSeason, color);
				white2 = Color.MultiplyColorOnly(white2, 0.74f * brightness);
			}
		}
		float num12 = MathUtils.Clamp(1f - slope / 8f * 0.2f, 0.5f, 1f);
		uint num13 = MathUtils.Hash((uint)x);
		uint num14 = MathUtils.Hash((uint)z);
		int num15 = (int)(MathUtils.Hash(num13 ^ (num14 * 49157)) % 5);
		float num16 = 0f;
		switch (num15)
		{
		case 0:
			num16 = -0.08f;
			break;
		case 1:
			num16 = -0.04f;
			break;
		case 2:
			num16 = 0f;
			break;
		case 3:
			num16 = 0.04f;
			break;
		case 4:
			num16 = 0.08f;
			break;
		}
		num12 += num16;
		white2 = Color.MultiplyColorOnly(white2, MathUtils.Clamp(num12 + 0.1f, 0f, 1.2f));
		white2.A = (byte)(light * 17);
		return white2;
	}

	private int CalculateNeighborLight(TerrainChunk chunk, int x, int y, int z)
	{
		int num = 0;
		int num2 = 4;
		int num3 = MathUtils.Max(x - num2, 0);
		int num4 = MathUtils.Min(x + num2, 15);
		int num5 = MathUtils.Max(z - num2, 0);
		int num6 = MathUtils.Min(z + num2, 15);
		int num7 = MathUtils.Max(y - 1, 0);
		int num8 = MathUtils.Min(y + 5, 255);
		for (int i = num3; i <= num4; i++)
		{
			for (int j = num5; j <= num6; j++)
			{
				for (int k = num7; k <= num8; k++)
				{
					int cellValueFast = chunk.GetCellValueFast(i, k, j);
					int num9 = Terrain.ExtractContents(cellValueFast);
					if (num9 == 0)
					{
						continue;
					}
					int emittedLightAmount = BlocksManager.Blocks[num9].GetEmittedLightAmount(cellValueFast);
					if (emittedLightAmount <= 0)
					{
						continue;
					}
					int num10 = MathUtils.Abs(i - x) + MathUtils.Abs(k - y) + MathUtils.Abs(j - z);
					int num11 = emittedLightAmount - num10;
					if (num11 > num)
					{
						num = num11;
						if (num >= 15)
						{
							return 15;
						}
					}
				}
			}
		}
		return num;
	}

	private int SortChunksByDistance(Point2 p1, Point2 p2)
	{
		float num = ((float)p1.X + 0.5f) * 128f;
		float num2 = ((float)p1.Y + 0.5f) * 128f;
		float num3 = (num - m_sortPosition.X) * (num - m_sortPosition.X) + (num2 - m_sortPosition.Y) * (num2 - m_sortPosition.Y);
		float num4 = ((float)p2.X + 0.5f) * 128f;
		float num5 = ((float)p2.Y + 0.5f) * 128f;
		float value = (num4 - m_sortPosition.X) * (num4 - m_sortPosition.X) + (num5 - m_sortPosition.Y) * (num5 - m_sortPosition.Y);
		return num3.CompareTo(value);
	}

	private int CalculateStepForDistanceSquared(float distSqr)
	{
		if (SettingsManager.DistantChunksDetailLevel < 2)
		{
			return 64;
		}
		if (distSqr < m_lodDistancesSquared[0])
		{
			return 1;
		}
		if (distSqr < m_lodDistancesSquared[1])
		{
			return 2;
		}
		if (distSqr < m_lodDistancesSquared[2])
		{
			return 4;
		}
		if (distSqr < m_lodDistancesSquared[3])
		{
			return 8;
		}
		if (distSqr < m_lodDistancesSquared[4])
		{
			return 16;
		}
		if (distSqr < m_lodDistancesSquared[5])
		{
			return 32;
		}
		return 64;
	}
}
