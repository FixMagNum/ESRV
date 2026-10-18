using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemSimpleWaterlog : SubsystemBlockBehavior, IUpdateable
{
	private int[] m_handledBlocks;

	private Queue<Point3> m_searchQueue = new Queue<Point3>();

	private HashSet<Point3> m_visited = new HashSet<Point3>();

	public SubsystemTime m_subsystemTime;

	private HashSet<Point3> m_blocksToUpdate = new HashSet<Point3>();

	private static readonly Point3[] m_directions = new Point3[6]
	{
		new Point3(1, 0, 0),
		new Point3(-1, 0, 0),
		new Point3(0, 1, 0),
		new Point3(0, -1, 0),
		new Point3(0, 0, 1),
		new Point3(0, 0, -1)
	};

	public override int[] HandledBlocks => m_handledBlocks;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public SubsystemSimpleWaterlog()
	{
		List<int> list = new List<int>();
		Block[] blocks = BlocksManager.Blocks;
		foreach (Block block in blocks)
		{
			if (block is IWaterloggable && !(block is FocalorsFluidBlock) && block.BlockIndex != 18)
			{
				list.Add(block.BlockIndex);
			}
		}
		m_handledBlocks = list.ToArray();
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		m_blocksToUpdate.Add(new Point3(x, y, z));
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		m_blocksToUpdate.Add(new Point3(x, y, z));
	}

	private void UpdateWaterlogged(int value, int x, int y, int z)
	{
		if (BlocksManager.Blocks[Terrain.ExtractContents(value)] is IWaterloggable waterloggable)
		{
			int data = Terrain.ExtractData(value);
			bool flag = waterloggable.IsWaterLogged(data);
			bool flag2 = IsConnectedToWaterSource(x, y, z);
			if (flag != flag2)
			{
				int data2 = waterloggable.SetWaterLogged(data, flag2);
				SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(value, data2));
			}
		}
	}

	private bool IsConnectedToWaterSource(int startX, int startY, int startZ)
	{
		m_searchQueue.Clear();
		m_visited.Clear();
		m_searchQueue.Enqueue(new Point3(startX, startY, startZ));
		m_visited.Add(new Point3(startX, startY, startZ));
		int num = 500;
		int num2 = 0;
		while (m_searchQueue.Count > 0 && num2 < num)
		{
			Point3 point = m_searchQueue.Dequeue();
			num2++;
			for (int i = 0; i < 6; i++)
			{
				Point3 item = point + m_directions[i];
				if (m_visited.Contains(item) || item.Y < 0 || item.Y > 255)
				{
					continue;
				}
				bool flag = item.Y < point.Y;
				bool flag2 = item.Y > point.Y;
				if (!flag)
				{
					int cellValue = SubsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z);
					int num3 = Terrain.ExtractContents(cellValue);
					if (num3 == 18)
					{
						return true;
					}
					Block block = BlocksManager.Blocks[num3];
					if (block is FocalorsFluidBlock)
					{
						return true;
					}
					if (block is IWaterloggable waterloggable && waterloggable.IsWaterLogged(Terrain.ExtractData(cellValue)) && !flag2)
					{
						m_visited.Add(item);
						m_searchQueue.Enqueue(item);
					}
				}
			}
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public void Update(float dt)
	{
		if (m_blocksToUpdate.Count <= 0)
		{
			return;
		}
		foreach (Point3 item in m_blocksToUpdate)
		{
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z);
			UpdateWaterlogged(cellValue, item.X, item.Y, item.Z);
		}
		m_blocksToUpdate.Clear();
	}
}
