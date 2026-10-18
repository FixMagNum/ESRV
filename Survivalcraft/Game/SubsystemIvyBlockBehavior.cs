using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemIvyBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public Random m_random;

	public Dictionary<Point3, int> m_toUpdate = new Dictionary<Point3, int>();

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[1] { 197 };

	public void Update(float dt)
	{
		if (!m_subsystemTime.PeriodicGameTimeEvent(60.0, 0.0))
		{
			return;
		}
		foreach (KeyValuePair<Point3, int> item in m_toUpdate)
		{
			int cellContents = SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z);
			if (cellContents == 0 || cellContents == 197)
			{
				SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
			}
		}
		m_toUpdate.Clear();
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int data = Terrain.ExtractData(cellValue);
		int faceMask = IvyBlock.GetFaceMask(data);
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				Point3 point = CellFace.FaceToPoint3(i);
				int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
				if (IsSupport(cellValue2))
				{
					num |= 1 << i;
				}
			}
		}
		int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (Terrain.ExtractContents(cellValue3) == 197)
		{
			int faceMask2 = IvyBlock.GetFaceMask(Terrain.ExtractData(cellValue3));
			num |= faceMask & faceMask2;
		}
		if (num != faceMask)
		{
			if (num == 0)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: true);
				return;
			}
			int value = Terrain.ReplaceData(cellValue, IvyBlock.SetFaceMask(data, num));
			SubsystemTerrain.ChangeCell(x, y, z, value);
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		int num = Terrain.ExtractData(value);
		bool noSeason = IvyBlock.GetNoSeason(num);
		SubsystemWeather.Season currentSeason = Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		if (!noSeason && (currentSeason == SubsystemWeather.Season.Anabiosis || currentSeason == SubsystemWeather.Season.Winter || currentSeason == SubsystemWeather.Season.LateWinter || currentSeason == SubsystemWeather.Season.EarlySpring))
		{
			return;
		}
		int num2 = Terrain.ExtractLight(value);
		if (num2 < 4)
		{
			return;
		}
		float num3 = ((num2 < 10) ? 0.5f : 1f);
		if (!(m_random.Float(0f, 1f) < 0.08f * num3) || IvyBlock.IsGrowthStopCell(x, y, z))
		{
			return;
		}
		int faceMask = IvyBlock.GetFaceMask(num);
		bool flag = false;
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				Point3 point = CellFace.FaceToPoint3(i);
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
				if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)] is LeavesBlock)
				{
					flag = true;
					break;
				}
			}
		}
		int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (!flag && Terrain.ExtractContents(cellValue2) == 197)
		{
			flag = true;
		}
		if (flag)
		{
			int num4 = y - 1;
			if (num4 > 0 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, num4, z)) == 0)
			{
				int data = IvyBlock.SetFaceMask(0, faceMask) | (num & 0x40);
				int value2 = Terrain.MakeBlockValue(197, 0, data);
				m_toUpdate[new Point3(x, num4, z)] = value2;
			}
		}
		else
		{
			if (!(m_random.Float(0f, 1f) < 0.33f))
			{
				return;
			}
			if (m_random.Float(0f, 1f) < 0.5f)
			{
				int num5 = m_random.Int(0, 5);
				if ((faceMask & (1 << num5)) == 0)
				{
					Point3 point2 = CellFace.FaceToPoint3(num5);
					int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x + point2.X, y + point2.Y, z + point2.Z);
					if (IsSupport(cellValue3))
					{
						int data2 = IvyBlock.SetFaceMask(num, faceMask | (1 << num5)) | (num & 0x40);
						m_toUpdate[new Point3(x, y, z)] = Terrain.ReplaceData(value, data2);
						return;
					}
				}
			}
			Point3 point3 = CellFace.FaceToPoint3(m_random.Int(0, 5));
			int num6 = x + point3.X;
			int num7 = y + point3.Y;
			int num8 = z + point3.Z;
			if (num7 <= 0 || num7 >= 255 || SubsystemTerrain.Terrain.GetCellContents(num6, num7, num8) != 0)
			{
				return;
			}
			int num9 = 0;
			for (int j = 0; j < 6; j++)
			{
				Point3 point4 = CellFace.FaceToPoint3(j);
				int cellValue4 = SubsystemTerrain.Terrain.GetCellValue(num6 + point4.X, num7 + point4.Y, num8 + point4.Z);
				if (IsSupport(cellValue4))
				{
					num9 |= 1 << j;
				}
			}
			if (num9 != 0)
			{
				int value3 = Terrain.MakeBlockValue(197, 0, IvyBlock.SetFaceMask(0, num9) | (num & 0x40));
				m_toUpdate[new Point3(num6, num7, num8)] = value3;
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public SubsystemIvyBlockBehavior()
	{
		m_random = new Random();
	}

	private bool IsSupport(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return false;
		}
		Block block = BlocksManager.Blocks[num];
		if ((!block.IsTransparent || block is LeavesBlock) && !(block is SandBlock) && !(block is FocalorsFluidBlock))
		{
			if (!block.IsCollidable)
			{
				return block is LeavesBlock;
			}
			return true;
		}
		return false;
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		if (!isLoaded)
		{
			return;
		}
		int num = Terrain.ExtractData(value);
		int faceMask = IvyBlock.GetFaceMask(num);
		int num2 = 0;
		for (int i = 0; i < 6; i++)
		{
			Point3 point = CellFace.FaceToPoint3(i);
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
			if (IsSupport(cellValue))
			{
				num2 |= 1 << i;
			}
		}
		int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (Terrain.ExtractContents(cellValue2) == 197)
		{
			int faceMask2 = IvyBlock.GetFaceMask(Terrain.ExtractData(cellValue2));
			num2 |= faceMask & faceMask2;
		}
		if (faceMask == 0 || (faceMask & num2) == 0)
		{
			int mask = ((num2 == 0) ? 1 : num2);
			bool flag = (num & 0x40) != 0;
			int num3 = IvyBlock.SetFaceMask(0, mask);
			if (flag)
			{
				num3 |= 0x40;
			}
			int value2 = Terrain.MakeBlockValue(197, 0, num3);
			SubsystemTerrain.ChangeCell(x, y, z, value2);
		}
	}
}
