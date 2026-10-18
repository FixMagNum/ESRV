using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemSoilBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public Random m_random = new Random();

	public Dictionary<Point3, bool> m_toDegrade = new Dictionary<Point3, bool>();

	public Dictionary<Point3, bool> m_toHydrate = new Dictionary<Point3, bool>();

	public SubsystemWeather m_subsystemWeather;

	public SubsystemPlayers m_subsystemPlayers;

	public SubsystemGameInfo m_subsystemGameInfo;

	private Dictionary<Point3, double> m_toReplace;

	public override int[] HandledBlocks => new int[1] { 168 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
	{
		float num = 0f;
		ComponentPlayer componentPlayer = componentBody.Entity.FindComponent<ComponentPlayer>();
		if (componentPlayer != null)
		{
			ComponentClothing componentClothing = componentPlayer.Entity.FindComponent<ComponentClothing>();
			if (componentClothing != null)
			{
				foreach (ClothingSlot value in Enum.GetValues(typeof(ClothingSlot)))
				{
					foreach (int item in componentClothing.GetClothes(value))
					{
						ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item)].GetClothingData(Terrain.ExtractData(item));
						if (clothingData != null)
						{
							num += clothingData.Weight;
						}
					}
				}
			}
		}
		float num2 = 1f;
		bool flag = false;
		if (num >= 7f)
		{
			flag = true;
		}
		else if (num >= 6f)
		{
			num2 = 10f;
		}
		if (!(componentBody.Mass > 20f) || componentBody.IsSneaking)
		{
			return;
		}
		Vector3 velocity2 = componentBody.Velocity;
		if (flag || velocity2.Y < -3f || (velocity2.Y < 0f && m_random.Float(0f, 1f) < 1.5f * num2 * m_subsystemTime.GameTimeDelta && velocity2.LengthSquared() > 1f))
		{
			SubsystemTerrain.ChangeCell(cellFace.Point.X, cellFace.Point.Y, cellFace.Point.Z, Terrain.MakeBlockValue(8));
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(cellFace.Point.X, cellFace.Point.Y + 1, cellFace.Point.Z);
			if (IsCrop(cellValue))
			{
				SubsystemTerrain.ChangeCell(cellFace.Point.X, cellFace.Point.Y + 1, cellFace.Point.Z, Terrain.MakeBlockValue(19));
			}
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		bool flag = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless;
		bool hydration = SoilBlock.GetHydration(Terrain.ExtractData(value));
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		bool flag2 = IsCrop(cellValue) || Terrain.ExtractContents(cellValue) == 19;
		int radius = (flag ? 2 : 5);
		if (CheckForBlockInRadius(x, y, z, radius, 92))
		{
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(2));
			if (flag2)
			{
				SubsystemTerrain.ChangeCell(x, y + 1, z, Terrain.MakeBlockValue(28));
			}
			return;
		}
		PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
		if (precipitationShaftInfo.Intensity > 0f && y >= precipitationShaftInfo.YLimit - 1)
		{
			m_toHydrate[new Point3(x, y, z)] = true;
		}
		else
		{
			Point3? point = FindBlockInRadius(x, y, z, 3, 18);
			if (point.HasValue)
			{
				m_toHydrate[new Point3(x, y, z)] = true;
				if (flag2)
				{
					float num = (flag ? 0.005f : 0.02f);
					if (m_random.Float(0f, 1f) < num)
					{
						ConsumeWaterChunk(point.Value.X, point.Value.Y, point.Value.Z);
					}
				}
			}
			else if (hydration)
			{
				m_toHydrate[new Point3(x, y, z)] = false;
			}
		}
		float num2 = (flag ? 0.05f : 0.2f);
		int radius2 = (flag ? 2 : 3);
		if (m_random.Float(0f, 1f) < num2 && CheckForBlockInRadius(x, y, z, radius2, 8))
		{
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(8));
			if (flag2)
			{
				SubsystemTerrain.ChangeCell(x, y + 1, z, Terrain.MakeBlockValue(19));
			}
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (DegradesSoilIfOnTopOfIt(cellValue))
		{
			int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceContents(cellValue2, 2));
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	public void Update(float dt)
	{
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			ComponentMiner componentMiner = componentPlayer.Entity.FindComponent<ComponentMiner>();
			if (componentMiner != null && componentMiner.DigCellFace.HasValue)
			{
				SubsystemTerrain.Terrain.GetCellValue(componentMiner.DigCellFace.Value.X, componentMiner.DigCellFace.Value.Y, componentMiner.DigCellFace.Value.Z);
			}
		}
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living || !m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
		{
			return;
		}
		foreach (KeyValuePair<Point3, bool> item in m_toHydrate)
		{
			Point3 key = item.Key;
			bool value = item.Value;
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
			if (Terrain.ExtractContents(cellValue) == 168)
			{
				int num = Terrain.ExtractData(cellValue);
				int num2 = SoilBlock.SetHydration(num, value);
				if (num != num2)
				{
					int value2 = Terrain.ReplaceData(cellValue, num2);
					SubsystemTerrain.ChangeCell(key.X, key.Y, key.Z, value2);
				}
			}
		}
		m_toHydrate.Clear();
		m_toDegrade.Clear();
	}

	public bool DegradesSoilIfOnTopOfIt(int value)
	{
		int node = Terrain.ExtractContents(value);
		Block i = BlocksManager.Blocks[node];
		if (!i.IsFaceTransparent(SubsystemTerrain, 5, value))
		{
			return i.IsCollidable;
		}
		return false;
	}

	public bool DetermineHydration(int x, int y, int z, int steps)
	{
		if (steps > 0 && y > 0 && y < 254)
		{
			if (DetermineHydrationHelper(x - 1, y, z, steps - 1))
			{
				return true;
			}
			if (DetermineHydrationHelper(x + 1, y, z, steps - 1))
			{
				return true;
			}
			if (DetermineHydrationHelper(x, y, z - 1, steps - 1))
			{
				return true;
			}
			if (DetermineHydrationHelper(x, y, z + 1, steps - 1))
			{
				return true;
			}
			if (steps >= 2)
			{
				if (DetermineHydrationHelper(x, y - 1, z, steps - 2))
				{
					return true;
				}
				if (DetermineHydrationHelper(x, y + 1, z, steps - 2))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool DetermineHydrationHelper(int x, int y, int z, int steps)
	{
		int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
		int num = Terrain.ExtractContents(cellValueFast);
		int data = Terrain.ExtractData(cellValueFast);
		switch (num)
		{
		case 18:
			return true;
		case 168:
			if (SoilBlock.GetHydration(data))
			{
				return DetermineHydration(x, y, z, steps);
			}
			break;
		}
		if (num == 2)
		{
			return DetermineHydration(x, y, z, steps);
		}
		return false;
	}

	private bool IsRake(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num != 169 && num != 219 && num != 171 && num != 172)
		{
			return num == 409;
		}
		return true;
	}

	public void OnBlockDestroyed(ComponentMiner miner, int x, int y, int z, int value)
	{
	}

	private bool IsCrop(int value)
	{
		int furnitureSet = Terrain.ExtractContents(value);
		if (furnitureSet != 204 && furnitureSet != 174)
		{
			return furnitureSet == 131;
		}
		return true;
	}

	private void ConsumeWaterChunk(int x, int y, int z)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) == 18)
		{
			int data = Terrain.ExtractData(cellValue);
			float level = FocalorsFluidBlock.GetLevel(data);
			float num = 0.03f;
			float num2 = level - num;
			if (num2 <= 0.01f)
			{
				SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(0));
				return;
			}
			int data2 = FocalorsFluidBlock.SetLevel(data, num2);
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(cellValue, data2));
		}
	}

	private bool CheckForBlockInRadius(int x, int y, int z, int radius, int blockId)
	{
		for (int num = -radius; num <= radius; num++)
		{
			for (int i = -radius; i <= radius; i++)
			{
				for (int j = -radius; j <= radius; j++)
				{
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x + num, y + i, z + j)) == blockId)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private Point3? FindBlockInRadius(int x, int y, int z, int radius, int blockId)
	{
		for (int furnitureSet = -radius; furnitureSet <= radius; furnitureSet++)
		{
			for (int i = -radius; i <= radius; i++)
			{
				for (int j = -radius; j <= radius; j++)
				{
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x + furnitureSet, y + i, z + j)) == blockId)
					{
						return new Point3(x + furnitureSet, y + i, z + j);
					}
				}
			}
		}
		return null;
	}
}
