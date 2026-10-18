using System.Collections.Generic;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemNatureSlabBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	private SubsystemTime m_subsystemTime;

	private SubsystemGameInfo m_subsystemGameInfo;

	private Random m_random = new Random();

	private Dictionary<Point3, int> m_toUpdate = new Dictionary<Point3, int>();

	private SubsystemWeather m_subsystemWeather;

	public override int[] HandledBlocks => new int[2] { 355, 356 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
	}

	public void Update(float dt)
	{
		if (CommonLib.WorkType != WorkType.Client && m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living && m_subsystemTime.PeriodicGameTimeEvent(60.0, 0.0) && (CommonLib.WorkType == WorkType.Local || CommonLib.WorkType == WorkType.Server))
		{
			NetUpdate();
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		switch (Terrain.ExtractContents(value))
		{
		case 356:
		{
			TrySpreadToNearbyBlocks(x, y, z, 355, 356);
			if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z)) != 0)
			{
				break;
			}
			SubsystemWeather.Season currentSeason = m_subsystemWeather.CurrentSeason;
			if (currentSeason >= SubsystemWeather.Season.EarlySpring && currentSeason <= SubsystemWeather.Season.LateSummer && m_random.Float(0f, 1f) < 0.005f)
			{
				int temperature = SubsystemTerrain.Terrain.GetTemperature(x, z);
				int humidity = SubsystemTerrain.Terrain.GetHumidity(x, z);
				int num = PlantsManager.GenerateRandomPlantValue(m_random, value, temperature, humidity, y + 1, 0);
				if (num != 0)
				{
					m_toUpdate[new Point3(x, y + 1, z)] = num;
				}
			}
			break;
		}
		case 355:
			TryTransformToGrassSlab(x, y, z, 8, 356);
			break;
		}
	}

	public void NetUpdate()
	{
		foreach (KeyValuePair<Point3, int> item in m_toUpdate)
		{
			SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
		}
		m_toUpdate.Clear();
	}

	private void TrySpreadToNearbyBlocks(int x, int y, int z, int targetContent, int newContent)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (i == 0 && j == 0 && k == 0)
					{
						continue;
					}
					int x2 = x + i;
					int num = y + j;
					int z2 = z + k;
					int cellValue = SubsystemTerrain.Terrain.GetCellValue(x2, num, z2);
					if (Terrain.ExtractContents(cellValue) != targetContent || !(m_random.Float(0f, 1f) < 0.1f))
					{
						continue;
					}
					float sizeFromValue = GrassSlabBlock.GetSizeFromValue(cellValue);
					int num2 = GrassSlabBlock.SetSizeInValue(Terrain.MakeBlockValue(newContent), sizeFromValue);
					m_toUpdate[new Point3(x2, num, z2)] = num2;
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x2, num + 1, z2)) == 0)
					{
						int temperature = SubsystemTerrain.Terrain.GetTemperature(x2, z2);
						int humidity = SubsystemTerrain.Terrain.GetHumidity(x2, z2);
						int num3 = PlantsManager.GenerateRandomPlantValue(m_random, num2, temperature, humidity, num + 1, 0);
						if (num3 != 0)
						{
							m_toUpdate[new Point3(x2, num + 1, z2)] = num3;
						}
					}
				}
			}
		}
	}

	private void TryTransformToGrassSlab(int x, int y, int z, int checkContent, int newContent)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (i == 0 && j == 0 && k == 0)
					{
						continue;
					}
					int x2 = x + i;
					int y2 = y + j;
					int z2 = z + k;
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x2, y2, z2)) != checkContent || !(m_random.Float(0f, 1f) < 0.1f))
					{
						continue;
					}
					float sizeFromValue = GrassSlabBlock.GetSizeFromValue(cellValue);
					int num = GrassSlabBlock.SetSizeInValue(Terrain.MakeBlockValue(newContent), sizeFromValue);
					m_toUpdate[new Point3(x, y, z)] = num;
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z)) == 0)
					{
						int temperature = SubsystemTerrain.Terrain.GetTemperature(x, z);
						int humidity = SubsystemTerrain.Terrain.GetHumidity(x, z);
						int num2 = PlantsManager.GenerateRandomPlantValue(m_random, num, temperature, humidity, y + 1, 0);
						if (num2 != 0)
						{
							m_toUpdate[new Point3(x, y + 1, z)] = num2;
						}
					}
				}
			}
		}
	}
}
