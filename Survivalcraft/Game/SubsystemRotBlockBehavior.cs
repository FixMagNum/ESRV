using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemRotBlockBehavior : SubsystemPollableBlockBehavior
{
	public SubsystemItemsScanner m_subsystemItemsScanner;

	public SubsystemGameInfo m_subsystemGameInfo;

	public double m_lastRotTime;

	public int m_rotStep;

	public bool m_isRotEnabled;

	private Random m_random = new Random();

	public const int MaxRot = 1;

	public const float m_rotPeriod = 60f;

	public SubsystemFluidDynamics m_subsystemFluidDynamics;

	public override int[] HandledBlocks => new int[0];

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemItemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(throwOnError: true);
		m_lastRotTime = valuesDictionary.GetValue<double>("LastRotTime");
		m_rotStep = valuesDictionary.GetValue<int>("RotStep");
		m_subsystemFluidDynamics = Project.FindSubsystem<SubsystemFluidDynamics>(throwOnError: true);
		m_subsystemItemsScanner.ItemsScanned += ItemsScanned;
		m_isRotEnabled = m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Adventure;
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		valuesDictionary.SetValue("LastRotTime", m_lastRotTime);
		valuesDictionary.SetValue("RotStep", m_rotStep);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (!m_isRotEnabled || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		int num = Terrain.ExtractContents(value);
		Block num2 = BlocksManager.Blocks[num];
		int num3 = num2.GetRotPeriod(value);
		if (num3 <= 0)
		{
			return;
		}
		float x2 = m_subsystemFluidDynamics.GetTemperatureAtPoint(new Point3(x, y, z));
		bool num4 = (Terrain.ExtractData(value) & 0x10000) != 0;
		if (x2 <= 0f && !num4)
		{
			value = Terrain.ReplaceData(value, Terrain.ExtractData(value) | 0x10000);
			SubsystemTerrain.ChangeCell(x, y, z, value);
			return;
		}
		if ((x2 > 0f) & num4)
		{
			value = Terrain.ReplaceData(value, Terrain.ExtractData(value) & -65537);
			SubsystemTerrain.ChangeCell(x, y, z, value);
			num4 = false;
		}
		if (!num4 && pollPass % num3 == 0)
		{
			float num5 = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
			if (m_random.Float(0f, 1f) < num5)
			{
				int num6 = num2.GetDamage(value) + 1;
				value = ((num6 > 1) ? num2.GetDamageDestructionValue(value) : num2.SetDamage(value, num6));
				SubsystemTerrain.ChangeCell(x, y, z, value);
			}
		}
	}

	public void ItemsScanned(ReadOnlyList<ScannedItemData> items)
	{
		double num = (m_subsystemGameInfo.TotalElapsedGameTime - m_lastRotTime) / 60.0;
		float itemDurabilityMultiplier = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
		int num2 = (int)(num * (double)itemDurabilityMultiplier);
		if (num2 <= 0)
		{
			return;
		}
		if (m_isRotEnabled && CommonLib.WorkType != WorkType.Client)
		{
			Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			foreach (ScannedItemData item in items)
			{
				int num3 = item.Value;
				int num4 = Terrain.ExtractContents(num3);
				Block block = BlocksManager.Blocks[num4];
				int rotPeriod = block.GetRotPeriod(num3);
				if (rotPeriod <= 0)
				{
					continue;
				}
				bool num5 = block is FoodBlock || block is AmazingFoodBlock || block is NewFoodBlock || block is BasePumpkinBlock;
				Pickable pickable = null;
				bool flag = (Terrain.ExtractData(num3) & 0x10000) != 0;
				if (num5)
				{
					if (item.Container is Pickable)
					{
						pickable = (Pickable)item.Container;
					}
					if (pickable != null)
					{
						float temperatureAtPoint = m_subsystemFluidDynamics.GetTemperatureAtPoint(new Point3((int)pickable.Position.X, (int)pickable.Position.Y, (int)pickable.Position.Z));
						if (temperatureAtPoint <= 0f && !flag)
						{
							num3 = Terrain.ReplaceData(num3, Terrain.ExtractData(num3) | 0x10000);
							m_subsystemItemsScanner.TryModifyItem(item, num3);
							flag = true;
						}
						else if ((temperatureAtPoint > 0f) & flag)
						{
							num3 = Terrain.ReplaceData(num3, Terrain.ExtractData(num3) & -65537);
							m_subsystemItemsScanner.TryModifyItem(item, num3);
							flag = false;
						}
					}
					else if (flag)
					{
						num3 = Terrain.ReplaceData(num3, Terrain.ExtractData(num3) & -65537);
						m_subsystemItemsScanner.TryModifyItem(item, num3);
						flag = false;
					}
					if (flag)
					{
						continue;
					}
				}
				bool flag2 = false;
				if (pickable == null && item.Container is Pickable)
				{
					pickable = (Pickable)item.Container;
				}
				if (pickable != null && pickable.IsSpecialPickable)
				{
					flag2 = true;
				}
				if (flag2)
				{
					continue;
				}
				int num6 = block.GetDamage(num3);
				for (int i = 0; i < num2; i++)
				{
					if (num6 > 1)
					{
						break;
					}
					if ((i + m_rotStep) % rotPeriod == 0 && m_random.Float(0f, 1f) < 1f)
					{
						num6++;
					}
				}
				if (num6 > block.GetDamage(num3))
				{
					if (num6 <= 1)
					{
						m_subsystemItemsScanner.TryModifyItem(item, block.SetDamage(num3, num6));
					}
					else
					{
						m_subsystemItemsScanner.TryModifyItem(item, block.GetDamageDestructionValue(num3));
					}
				}
			}
		}
		m_rotStep += num2;
		double num7 = ((itemDurabilityMultiplier > 0.001f) ? (60.0 / (double)itemDurabilityMultiplier) : 0.0);
		m_lastRotTime += (double)num2 * num7;
	}

	private Pickable FindPickableAtPosition(Vector3 position)
	{
		foreach (Pickable pickable in Project.FindSubsystem<SubsystemPickables>(throwOnError: true).Pickables)
		{
			if (Vector3.Distance(pickable.Position, position) < 0.1f)
			{
				return pickable;
			}
		}
		return null;
	}

	private Pickable FindPickableByValue(int value)
	{
		foreach (Pickable pickable in Project.FindSubsystem<SubsystemPickables>(throwOnError: true).Pickables)
		{
			if (pickable.Value == value)
			{
				return pickable;
			}
		}
		return null;
	}
}
