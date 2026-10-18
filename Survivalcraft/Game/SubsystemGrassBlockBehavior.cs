using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemGrassBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public Dictionary<Point3, int> m_toUpdate = new Dictionary<Point3, int>();

	public Random m_random = new Random();

	public SubsystemWeather m_subsystemWeather;

	public override int[] HandledBlocks => new int[1] { 8 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (Terrain.ExtractData(value) != 0 || m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		int num = Terrain.ExtractContents(cellValue);
		if (num == 0)
		{
			SubsystemWeather.Season currentSeason = m_subsystemWeather.CurrentSeason;
			float num2 = m_random.Float(0f, 1f);
			if (currentSeason >= SubsystemWeather.Season.EarlySpring && currentSeason <= SubsystemWeather.Season.EarlySummer && num2 < 1E-05f)
			{
				int temperature = SubsystemTerrain.Terrain.GetTemperature(x, z);
				int humidity = SubsystemTerrain.Terrain.GetHumidity(x, z);
				TreeType? treeType = PlantsManager.GenerateRandomTreeType(m_random, temperature, humidity, y);
				if (treeType.HasValue)
				{
					int data = 0;
					switch (treeType.Value)
					{
					case TreeType.Oak:
					case TreeType.OldOak:
						data = 0;
						break;
					case TreeType.Birch:
					case TreeType.OldBirch:
						data = 1;
						break;
					case TreeType.Spruce:
					case TreeType.OldSpruce:
						data = 2;
						break;
					case TreeType.TallSpruce:
					case TreeType.OldTallSpruce:
						data = 3;
						break;
					case TreeType.Mimosa:
					case TreeType.OldMimosa:
						data = 4;
						break;
					}
					m_toUpdate[new Point3(x, y + 1, z)] = Terrain.MakeBlockValue(119, 0, data);
					return;
				}
			}
			if (currentSeason >= SubsystemWeather.Season.EarlySpring && currentSeason <= SubsystemWeather.Season.LateSummer && num2 < 0.005f)
			{
				int temperature2 = SubsystemTerrain.Terrain.GetTemperature(x, z);
				int humidity2 = SubsystemTerrain.Terrain.GetHumidity(x, z);
				int num3 = PlantsManager.GenerateRandomPlantValue(m_random, value, temperature2, humidity2, y + 1, 0);
				if (num3 != 0)
				{
					m_toUpdate[new Point3(x, y + 1, z)] = num3;
				}
			}
		}
		if (num == 355 || num == 356)
		{
			m_toUpdate[new Point3(x, y, z)] = Terrain.MakeBlockValue(2, 0, 0);
			return;
		}
		int num4 = Terrain.ExtractLight(cellValue);
		if (num4 == 0)
		{
			m_toUpdate[new Point3(x, y, z)] = Terrain.ReplaceContents(value, 8);
		}
		if (num4 < 13)
		{
			return;
		}
		for (int i = x - 1; i <= x + 1; i++)
		{
			for (int j = z - 1; j <= z + 1; j++)
			{
				for (int k = y - 2; k <= y + 1; k++)
				{
					int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(i, k, j);
					if (Terrain.ExtractContents(cellValue2) != 2)
					{
						continue;
					}
					int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(i, k + 1, j);
					if (KillsGrassIfOnTopOfIt(cellValue3) || Terrain.ExtractLight(cellValue3) < 13 || !(m_random.Float(0f, 1f) < 0.1f))
					{
						continue;
					}
					int num5 = Terrain.ReplaceContents(cellValue2, 8);
					m_toUpdate[new Point3(i, k, j)] = num5;
					if (Terrain.ExtractContents(cellValue3) == 0)
					{
						int temperature3 = SubsystemTerrain.Terrain.GetTemperature(i, j);
						int humidity3 = SubsystemTerrain.Terrain.GetHumidity(i, j);
						int num6 = PlantsManager.GenerateRandomPlantValue(m_random, num5, temperature3, humidity3, k + 1);
						if (num6 != 0)
						{
							m_toUpdate[new Point3(i, k + 1, j)] = num6;
						}
					}
				}
			}
		}
		if (num == 18 && FocalorsFluidBlock.GetLevelHeight(FocalorsFluidBlock.GetLevel(Terrain.ExtractData(cellValue))) <= 0.1f && m_random.Float(0f, 1f) < 0.009f)
		{
			Vector3 position = new Vector3((float)x + 0.5f, (float)y + 1f, (float)z + 0.5f);
			int value2 = Terrain.MakeBlockValue(318, 0, WormBlock.SetKind(0, WormBlock.WormKind.Earthworm));
			Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(value2, 1, position, null, null);
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int node = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		int i = Terrain.ExtractContents(node);
		switch (i)
		{
		case 2:
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(2, 0, 0));
			return;
		case 352:
			return;
		case 61:
		case 348:
		case 350:
		{
			int cellValueFast2 = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
			int data2 = (Terrain.ExtractData(cellValueFast2) & -2) | 1;
			int value2 = Terrain.ReplaceData(cellValueFast2, data2);
			SubsystemTerrain.ChangeCell(x, y, z, value2);
			break;
		}
		default:
		{
			int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
			int data = Terrain.ExtractData(cellValueFast) & -2;
			int value = Terrain.ReplaceData(cellValueFast, data);
			SubsystemTerrain.ChangeCell(x, y, z, value);
			break;
		}
		}
		if (KillsGrassIfOnTopOfIt(node) && i != 18)
		{
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(2, 0, 0));
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		base.Load(valuesDictionary);
	}

	public void NetUpdate()
	{
		foreach (KeyValuePair<Point3, int> item in m_toUpdate)
		{
			if (Terrain.ExtractContents(item.Value) == 8)
			{
				if (SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z) != 2)
				{
					continue;
				}
			}
			else
			{
				int cellContents = SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y - 1, item.Key.Z);
				if ((cellContents != 8 && cellContents != 2) || SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z) != 0)
				{
					continue;
				}
			}
			SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
		}
		m_toUpdate.Clear();
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Client && m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living && m_subsystemTime.PeriodicGameTimeEvent(60.0, 0.0) && (CommonLib.WorkType == WorkType.Local || CommonLib.WorkType == WorkType.Server))
		{
			NetUpdate();
		}
	}

	public bool KillsGrassIfOnTopOfIt(int value)
	{
		int node = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[node];
		if (!(block is FocalorsFluidBlock) && !(block is FluidBlock))
		{
			if (!block.IsFaceTransparent(SubsystemTerrain, 5, value))
			{
				return block.IsCollidable;
			}
			return false;
		}
		return true;
	}
}
