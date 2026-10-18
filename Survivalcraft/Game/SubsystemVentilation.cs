using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class SubsystemVentilation : SubsystemBlockBehavior, IUpdateable, IDrawable
{
	private class VentilationTask
	{
		public Point3 StartPoint;

		public Queue<Point3> Queue = new Queue<Point3>();

		public HashSet<Point3> Visited = new HashSet<Point3>();

		public int CurrentVolume;

		public bool IsCompleted;

		public RoomData Result;

		public VentilationTask(Point3 start)
		{
			StartPoint = start;
			Queue.Enqueue(start);
			Visited.Add(start);
			Result = new RoomData
			{
				IsProcessing = true
			};
		}
	}

	public class RoomData
	{
		public bool IsProcessing;

		public bool IsVentilated;

		public bool IsStale;

		public int Volume;

		public Point3? BreachCoordinate;

		public HashSet<Point3> AirPositions = new HashSet<Point3>();

		public HashSet<Point3> SurfacePositions = new HashSet<Point3>();

		public double LastAccessTime;

		public bool IsFloor(Point3 surfacePos)
		{
			return AirPositions.Contains(new Point3(surfacePos.X, surfacePos.Y + 1, surfacePos.Z));
		}

		public bool IsCeiling(Point3 surfacePos)
		{
			return AirPositions.Contains(new Point3(surfacePos.X, surfacePos.Y - 1, surfacePos.Z));
		}
	}

	private Dictionary<Point3, VentilationTask> m_activeTasks;

	private SubsystemTerrain m_subsystemTerrain;

	private const int StepsPerFrame = 2000;

	private const int MaxRoomVolume = 10000;

	private static readonly Point3[] m_neighborOffsets;

	private Dictionary<Point3, RoomData> m_cache;

	public bool ShowDebugVisuals = true;

	private PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

	public override int[] HandledBlocks => new int[0];

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => new int[1] { 1000 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		if (!ShouldIgnoreBlock(value))
		{
			InvalidateCache(x, y, z);
		}
	}

	public override void OnBlockRemoved(int value, int oldValue, int x, int y, int z)
	{
		if (!ShouldIgnoreBlock(oldValue))
		{
			InvalidateCache(x, y, z);
		}
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		if (!ShouldIgnoreBlock(value) || !ShouldIgnoreBlock(oldValue))
		{
			InvalidateCache(x, y, z);
		}
	}

	private void InvalidateCache(int x, int y, int z)
	{
		List<Point3> list = new List<Point3>();
		foreach (KeyValuePair<Point3, RoomData> item in m_cache)
		{
			if (item.Value.AirPositions.Contains(new Point3(x, y, z)) || item.Value.SurfacePositions.Contains(new Point3(x, y, z)) || IsNear(item.Key, x, y, z))
			{
				list.Add(item.Key);
			}
		}
		foreach (Point3 item2 in list)
		{
			if (m_cache.TryGetValue(item2, out var value))
			{
				value.IsStale = true;
			}
			if (m_activeTasks.ContainsKey(item2))
			{
				m_activeTasks.Remove(item2);
			}
			m_activeTasks[item2] = new VentilationTask(item2);
		}
	}

	public void Update(float dt)
	{
		if (m_activeTasks.Count == 0)
		{
			return;
		}
		int num = 2000;
		List<Point3> list = new List<Point3>();
		foreach (KeyValuePair<Point3, VentilationTask> activeTask in m_activeTasks)
		{
			VentilationTask value = activeTask.Value;
			while (num > 0 && value.Queue.Count > 0)
			{
				num--;
				Point3 point = value.Queue.Dequeue();
				value.Result.AirPositions.Add(point);
				value.CurrentVolume++;
				int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
				if (point.Y > topHeight)
				{
					value.Result.IsVentilated = true;
					value.Result.BreachCoordinate = point;
					value.IsCompleted = true;
				}
				if (value.CurrentVolume >= 10000)
				{
					value.Result.IsVentilated = true;
					value.Result.BreachCoordinate = point;
					value.IsCompleted = true;
					break;
				}
				for (int i = 0; i < 6; i++)
				{
					Point3 p = point + m_neighborOffsets[i];
					if (p.Y >= 0 && p.Y <= 255)
					{
						ProcessNeighbor(value, p);
					}
				}
			}
			if (value.Queue.Count == 0)
			{
				value.IsCompleted = true;
			}
			if (value.IsCompleted)
			{
				value.Result.Volume = value.CurrentVolume;
				value.Result.IsProcessing = false;
				value.Result.IsStale = false;
				list.Add(activeTask.Key);
			}
			if (num <= 0)
			{
				break;
			}
		}
		foreach (Point3 item in list)
		{
			if (m_activeTasks.TryGetValue(item, out var value2))
			{
				m_cache[item] = value2.Result;
				m_activeTasks.Remove(item);
			}
		}
		if (!Time.PeriodicEvent(60.0, 0.0))
		{
			return;
		}
		List<Point3> list2 = new List<Point3>();
		foreach (KeyValuePair<Point3, RoomData> item2 in m_cache)
		{
			if (Time.FrameStartTime - item2.Value.LastAccessTime > 60.0)
			{
				list2.Add(item2.Key);
			}
		}
		foreach (Point3 item3 in list2)
		{
			m_cache.Remove(item3);
		}
	}

	public SubsystemVentilation()
	{
		m_cache = new Dictionary<Point3, RoomData>();
		m_activeTasks = new Dictionary<Point3, VentilationTask>();
	}

	private bool IsNear(Point3 p1, int x, int y, int z)
	{
		if (MathUtils.Abs(p1.X - x) < 32 && MathUtils.Abs(p1.Y - y) < 32)
		{
			return MathUtils.Abs(p1.Z - z) < 32;
		}
		return false;
	}

	static SubsystemVentilation()
	{
		m_neighborOffsets = new Point3[6]
		{
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
	}

	public VentilationStatus GetVentilationStatus(Point3 point)
	{
		throw new NotImplementedException("Use GetRoomData instead.");
	}

	private bool ShouldIgnoreBlock(int value)
	{
		int num = Terrain.ExtractContents(value);
		return BlocksManager.Blocks[num] is FocalorsFluidBlock;
	}

	private void ProcessNeighbor(VentilationTask task, Point3 p)
	{
		if (!task.Visited.Contains(p))
		{
			int num = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z));
			Block block = BlocksManager.Blocks[num];
			bool num2 = num == 0 || num == 439 || (!block.IsCollidable && !(block is FluidBlock) && !(block is FocalorsFluidBlock));
			task.Visited.Add(p);
			if (num2)
			{
				task.Queue.Enqueue(p);
			}
			else
			{
				task.Result.SurfacePositions.Add(p);
			}
		}
	}

	public RoomData GetRoomData(Point3 point, bool startProcessIfMissing = true)
	{
		if (m_cache.TryGetValue(point, out var value))
		{
			value.LastAccessTime = Time.FrameStartTime;
			return value;
		}
		if (m_activeTasks.TryGetValue(point, out var value2))
		{
			return value2.Result;
		}
		if (startProcessIfMissing)
		{
			VentilationTask ventilationTask = new VentilationTask(point);
			m_activeTasks[point] = ventilationTask;
			return ventilationTask.Result;
		}
		return null;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (!SettingsManager.DebugMode || !ShowDebugVisuals)
		{
			return;
		}
		FlatBatch3D batch = m_primitivesRenderer.FlatBatch(0, DepthStencilState.DepthRead, RasterizerState.CullNone, BlendState.AlphaBlend);
		Vector3 viewPosition = camera.ViewPosition;
		foreach (VentilationTask value in m_activeTasks.Values)
		{
			DrawRoomData(batch, value.Result, viewPosition, isProcessing: true);
		}
		foreach (RoomData value2 in m_cache.Values)
		{
			if (!value2.IsStale)
			{
				DrawRoomData(batch, value2, viewPosition, isProcessing: false);
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	private void DrawRoomData(FlatBatch3D batch, RoomData room, Vector3 camPos, bool isProcessing)
	{
		Color color;
		if (isProcessing)
		{
			color = Color.Yellow;
		}
		else
		{
			color = ((!room.IsVentilated) ? Color.Green : Color.Cyan);
		}
		Color red = Color.Red;
		float num = 32f;
		foreach (Point3 airPosition in room.AirPositions)
		{
			Vector3 vector = new Vector3((float)airPosition.X + 0.5f, (float)airPosition.Y + 0.5f, (float)airPosition.Z + 0.5f);
			float num2 = Vector3.Distance(vector, camPos);
			if (num2 <= num)
			{
				float num3 = 1f - num2 / num;
				num3 = MathUtils.Clamp(num3 * 0.6f, 0f, 1f);
				if (num3 > 0.05f)
				{
					QueuePoint(batch, vector, color * num3, 0.1f);
				}
			}
		}
		foreach (Point3 surfacePosition in room.SurfacePositions)
		{
			Vector3 vector2 = new Vector3((float)surfacePosition.X + 0.5f, (float)surfacePosition.Y + 0.5f, (float)surfacePosition.Z + 0.5f);
			float num4 = Vector3.Distance(vector2, camPos);
			if (num4 <= num)
			{
				float num5 = 1f - num4 / num;
				num5 = MathUtils.Clamp(num5 * 0.5f, 0f, 1f);
				if (num5 > 0.05f)
				{
					QueuePoint(batch, vector2, red * num5, 0.15f);
				}
			}
		}
		if (room.BreachCoordinate.HasValue)
		{
			Vector3 vector3 = new Vector3((float)room.BreachCoordinate.Value.X + 0.5f, (float)room.BreachCoordinate.Value.Y + 0.5f, (float)room.BreachCoordinate.Value.Z + 0.5f);
			if (Vector3.Distance(vector3, camPos) < num * 1.5f)
			{
				QueuePoint(batch, vector3, Color.Blue, 0.5f);
			}
		}
	}

	private void QueuePoint(FlatBatch3D batch, Vector3 center, Color color, float size)
	{
		Vector3 vector = new Vector3(size, 0f, 0f);
		Vector3 vector2 = new Vector3(0f, size, 0f);
		Vector3 vector3 = new Vector3(0f, 0f, size);
		batch.QueueLine(center - vector, center + vector, color);
		batch.QueueLine(center - vector2, center + vector2, color);
		batch.QueueLine(center - vector3, center + vector3, color);
	}
}
