using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemNaturalTreeScanner : Subsystem, IUpdateable
{
	public class TreeInfo
	{
		public List<Point3> Logs;

		public List<Point3> Leaves;

		public int RootY;

		public int TopY;

		public int Height;

		public Point3 RootPos;
	}

	private static readonly HashSet<int> s_woodIds;

	private static readonly HashSet<int> s_leavesIds;

	private static readonly Point3[] s_dirs;

	private SubsystemTerrain m_terrain;

	private SubsystemAudio m_audio;

	private const int ScanRadius = 80;

	private const int CellsPerFrame = 4000;

	private bool m_cursorDone;

	private int m_centerY;

	private const int MoveThreshold = 20;

	public readonly Dictionary<Point2, TreeInfo> Trees = new Dictionary<Point2, TreeInfo>();

	private bool m_scanning;

	private Point2 m_scanCenter;

	private Point2 m_lastScanCenter = new Point2(0, 0);

	private int m_scanCenterY;

	private int m_cx;

	private int m_cy;

	private int m_cz;

	private readonly HashSet<Point3> m_seen;

	private Dictionary<Point2, TreeInfo> m_newTrees;

	private const int MaxScanRadius = 80;

	private const int InitialScanRadius = 24;

	private const int RadiusStep = 16;

	private const int DesiredTrees = 40;

	private const int MinTreesForArea = 12;

	private int m_currentRadius = 24;

	private int m_foundCount;

	private int m_lastFoundCount;

	private float m_targetAvgRootY;

	private float m_smoothedRootY;

	private const float SmoothLerpPerSec = 2f;

	private int m_boundR;

	private readonly HashSet<Point3> m_seenWood = new HashSet<Point3>();

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public float SmoothedRootY => m_smoothedRootY;

	private static bool IsNaturalWood(int c, int d)
	{
		if (s_woodIds.Contains(c))
		{
			return (d & 4) == 0;
		}
		return false;
	}

	private static bool IsNaturalLeaves(int c, int d)
	{
		if (s_leavesIds.Contains(c))
		{
			return (d & 1) == 0;
		}
		return false;
	}

	public void Update(float dt)
	{
		if (SettingsManager.TreeSwayEnabled)
		{
			Vector3 vector = Vector3.Zero;
			if (m_audio != null && m_audio.ListenerPositions.Count > 0)
			{
				vector = m_audio.ListenerPositions[0];
			}
			float targetAvgRootY = MathUtils.Clamp(MathUtils.Floor(vector.Y) - 1f, 0f, 255f);
			m_targetAvgRootY = targetAvgRootY;
			float num = MathUtils.Abs(m_targetAvgRootY - m_smoothedRootY);
			float num2 = MathUtils.Clamp(0.35f + num / 20f, 0.35f, 3f);
			float f = MathUtils.Saturate(dt / 2f * num2);
			m_smoothedRootY = MathUtils.Lerp(m_smoothedRootY, m_targetAvgRootY, f);
		}
	}

	public override void Load(ValuesDictionary dict)
	{
		base.Load(dict);
		m_terrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_audio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_smoothedRootY = 66f;
		m_targetAvgRootY = m_smoothedRootY;
	}

	public SubsystemNaturalTreeScanner()
	{
		m_seen = new HashSet<Point3>();
		m_newTrees = new Dictionary<Point2, TreeInfo>();
	}

	static SubsystemNaturalTreeScanner()
	{
		s_woodIds = new HashSet<int>(new int[4] { 9, 10, 11, 255 });
		s_leavesIds = new HashSet<int>(new int[5] { 12, 13, 14, 225, 256 });
		s_dirs = new Point3[6]
		{
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
	}

	private void StepCursor()
	{
		m_cx++;
		if (m_cx <= m_boundR)
		{
			return;
		}
		m_cx = -m_boundR;
		m_cy++;
		if (m_cy > m_boundR)
		{
			m_cy = -m_boundR;
			m_cz++;
			if (m_cz > m_boundR)
			{
				m_cursorDone = true;
			}
		}
	}

	private void StartScan(Point2 centerXZ, int playerY)
	{
		m_scanCenter = centerXZ;
		int centerY = MathUtils.Clamp(playerY, 0, 255);
		m_centerY = centerY;
		m_boundR = MathUtils.Clamp(m_currentRadius, 8, 80);
		m_cx = (m_cy = (m_cz = -m_boundR));
		m_cursorDone = false;
		m_seen.Clear();
		m_seenWood.Clear();
		m_newTrees.Clear();
		m_scanning = true;
		m_foundCount = 0;
	}

	private TreeInfo GatherTree(Point3 seed)
	{
		Point3 rootPos = seed;
		Queue<Point3> queue = new Queue<Point3>();
		queue.Enqueue(seed);
		m_seenWood.Add(seed);
		List<Point3> list = new List<Point3>();
		int y = seed.Y;
		int y2 = seed.Y;
		while (queue.Count > 0)
		{
			Point3 point = queue.Dequeue();
			if (point.Y < rootPos.Y)
			{
				rootPos = point;
			}
			list.Add(point);
			if (point.Y < y)
			{
				y = point.Y;
			}
			if (point.Y > y2)
			{
				y2 = point.Y;
			}
			for (int i = 0; i < s_dirs.Length; i++)
			{
				Point3 item = point + s_dirs[i];
				if (item.Y >= 0 && item.Y <= 255 && !m_seenWood.Contains(item))
				{
					int cellValue = m_terrain.Terrain.GetCellValue(item.X, item.Y, item.Z);
					int c = Terrain.ExtractContents(cellValue);
					int d = Terrain.ExtractData(cellValue);
					if (IsNaturalWood(c, d))
					{
						m_seenWood.Add(item);
						queue.Enqueue(item);
					}
				}
			}
		}
		return new TreeInfo
		{
			Logs = list,
			Leaves = new List<Point3>(),
			RootY = y,
			TopY = y2,
			Height = y2 - y + 1,
			RootPos = rootPos
		};
	}
}
