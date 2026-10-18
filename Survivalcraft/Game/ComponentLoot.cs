using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentLoot : Component, IUpdateable
{
	public struct Loot
	{
		public int Value;

		public int MinCount;

		public int MaxCount;

		public float Probability;
	}

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemPickables m_subsystemPickables;

	public ComponentCreature m_componentCreature;

	public List<Loot> m_lootList;

	public List<Loot> m_lootOnFireList;

	public Random m_random;

	public bool m_lootDropped;

	public SubsystemTerrain m_subsystemTerrain;

	public float HarvestPercentPerCut;

	public List<int> m_generatedLoot;

	public int m_harvestedCount;

	public float m_harvestRemainder;

	public bool m_lootScaleApplied;

	public float m_lootScale = 1f;

	public bool m_charred;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public bool IsCharred => m_charred;

	public static List<Loot> ParseLootList(ValuesDictionary lootVd)
	{
		List<Loot> gameMode2 = new List<Loot>();
		foreach (string value in lootVd.Values)
		{
			gameMode2.Add(ParseLoot(value));
		}
		gameMode2.Sort((Loot l1, Loot l2) => l1.Value - l2.Value);
		return gameMode2;
	}

	public void Update(float dt)
	{
		if (m_lootDropped || !m_componentCreature.ComponentHealth.DeathTime.HasValue || m_subsystemGameInfo.TotalElapsedGameTime < m_componentCreature.ComponentHealth.DeathTime.Value + (double)m_componentCreature.ComponentHealth.CorpseDuration)
		{
			return;
		}
		m_lootDropped = true;
		Vector3 position = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
		int x = Terrain.ToCell(position.X);
		int num = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num - 1, z);
		Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
		int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num, z));
		Block block2 = BlocksManager.Blocks[num2];
		if (!SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var _) && block.IsCollidable && !block2.IsCollidable)
		{
			float num3 = m_random.Float(0f, 1f);
			int num4 = ((!(num3 < 0.8f)) ? ((num3 < 0.9f) ? 1 : 2) : 0);
			int num5 = m_random.Int(0, 3);
			int num6 = m_random.Int(0, 3);
			int data = (num4 << 4) | (num5 << 2) | num6;
			int value = Terrain.MakeBlockValue(362, 0, data);
			m_subsystemTerrain.ChangeCell(x, num, z, value);
		}
		else
		{
			int num7 = m_random.Int(1, 2);
			for (int i = 0; i < num7; i++)
			{
				m_subsystemPickables.CreatePickable(null, Terrain.MakeBlockValue(362, 0, 0), 1, position, null, null);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_lootDropped = valuesDictionary.GetValue<bool>("LootDropped");
		m_lootList = ParseLootList(valuesDictionary.GetValue<ValuesDictionary>("Loot"));
		m_lootOnFireList = ParseLootList(valuesDictionary.GetValue<ValuesDictionary>("LootOnFire"));
		HarvestPercentPerCut = (valuesDictionary.ContainsKey("HarvestPercentPerCut") ? valuesDictionary.GetValue<float>("HarvestPercentPerCut") : 0.2f);
		m_harvestedCount = (valuesDictionary.ContainsKey("HarvestedCount") ? valuesDictionary.GetValue<int>("HarvestedCount") : 0);
		m_harvestRemainder = (valuesDictionary.ContainsKey("HarvestRemainder") ? valuesDictionary.GetValue<float>("HarvestRemainder") : 0f);
		m_generatedLoot = null;
		if (valuesDictionary.ContainsKey("GeneratedLoot"))
		{
			string value = valuesDictionary.GetValue<string>("GeneratedLoot");
			if (!string.IsNullOrEmpty(value))
			{
				string[] array = value.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				m_generatedLoot = new List<int>(array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					if (int.TryParse(array[i], out var result))
					{
						m_generatedLoot.Add(result);
					}
				}
			}
		}
		m_lootScaleApplied = valuesDictionary.ContainsKey("LootScaleApplied") && valuesDictionary.GetValue<bool>("LootScaleApplied");
		m_lootScale = (valuesDictionary.ContainsKey("LootScale") ? valuesDictionary.GetValue<float>("LootScale") : 1f);
		m_charred = valuesDictionary.ContainsKey("Charred") && valuesDictionary.GetValue<bool>("Charred");
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("LootDropped", m_lootDropped);
		valuesDictionary.SetValue("HarvestPercentPerCut", HarvestPercentPerCut);
		valuesDictionary.SetValue("HarvestedCount", m_harvestedCount);
		valuesDictionary.SetValue("HarvestRemainder", m_harvestRemainder);
		if (m_generatedLoot != null && m_generatedLoot.Count > 0)
		{
			StringBuilder data = new StringBuilder();
			for (int connectorDirection = 0; connectorDirection < m_generatedLoot.Count; connectorDirection++)
			{
				if (connectorDirection > 0)
				{
					data.Append(",");
				}
				data.Append(m_generatedLoot[connectorDirection].ToString());
			}
			valuesDictionary.SetValue("GeneratedLoot", data.ToString());
		}
		else
		{
			valuesDictionary.SetValue("GeneratedLoot", string.Empty);
		}
		valuesDictionary.SetValue("LootScaleApplied", m_lootScaleApplied);
		valuesDictionary.SetValue("LootScale", m_lootScale);
		valuesDictionary.SetValue("Charred", m_charred);
	}

	public static Loot ParseLoot(string lootString)
	{
		string[] array = lootString.Split(new string[1] { ";" }, StringSplitOptions.None);
		if (array.Length >= 3)
		{
			int value = CraftingRecipesManager.DecodeResult(array[0]);
			return new Loot
			{
				Value = value,
				MinCount = int.Parse(array[1], CultureInfo.InvariantCulture),
				MaxCount = int.Parse(array[2], CultureInfo.InvariantCulture),
				Probability = ((array.Length >= 4) ? float.Parse(array[3], CultureInfo.InvariantCulture) : 1f)
			};
		}
		throw new InvalidOperationException("Invalid loot string.");
	}

	public ComponentLoot()
	{
		m_random = new Random();
	}

	public void HarvestForCut(float? percentOverride)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		ComponentHealth voltage = m_componentCreature.ComponentHealth;
		if (voltage != null && voltage.DeathTime.HasValue && m_subsystemGameInfo.TotalElapsedGameTime - voltage.DeathTime.Value > 1000.0)
		{
			return;
		}
		float num = MathUtils.Saturate(percentOverride.HasValue ? percentOverride.Value : HarvestPercentPerCut);
		if (num <= 0f)
		{
			return;
		}
		Vector3 position = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
		EnsureGeneratedLoot();
		if (m_generatedLoot != null && m_generatedLoot.Count != 0)
		{
			int connection = m_generatedLoot.Count;
			float num2 = num * (float)connection + m_harvestRemainder;
			int num3 = (int)MathUtils.Floor(num2);
			m_harvestRemainder = num2 - (float)num3;
			int max = connection - m_harvestedCount;
			int num4 = MathUtils.Clamp(num3, 0, max);
			for (int i = 0; i < num4; i++)
			{
				int value = m_generatedLoot[m_harvestedCount + i];
				m_subsystemPickables.CreatePickable(null, value, 1, position, null, null);
			}
			m_harvestedCount += num4;
		}
	}

	public void DropRemainingNow()
	{
		if (m_generatedLoot != null)
		{
			m_harvestedCount = m_generatedLoot.Count;
		}
	}

	public void SetLootScaleOnce(float scale)
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		scale = MathUtils.Saturate(scale);
		if (m_lootScaleApplied)
		{
			return;
		}
		EnsureGeneratedLoot();
		if (m_generatedLoot == null || m_generatedLoot.Count == 0)
		{
			m_lootScaleApplied = true;
			m_lootScale = scale;
			return;
		}
		int count = m_generatedLoot.Count;
		int x = (int)MathUtils.Floor((float)count * scale);
		x = MathUtils.Clamp(x, 0, count);
		if (x < count)
		{
			m_generatedLoot.RemoveRange(x, count - x);
			if (m_harvestedCount > x)
			{
				m_harvestedCount = x;
			}
		}
		m_lootScaleApplied = true;
		m_lootScale = scale;
	}

	public void MarkCharred()
	{
		m_charred = true;
	}

	private void EnsureGeneratedLoot()
	{
		if (m_generatedLoot != null)
		{
			return;
		}
		List<Loot> list = (m_charred ? m_lootOnFireList : m_lootList);
		m_generatedLoot = new List<int>(8);
		foreach (Loot item in list)
		{
			if (m_random.Float(0f, 1f) < item.Probability)
			{
				int num = m_random.Int(item.MinCount, item.MaxCount);
				for (int i = 0; i < num; i++)
				{
					m_generatedLoot.Add(item.Value);
				}
			}
		}
		for (int num2 = m_generatedLoot.Count - 1; num2 > 0; num2--)
		{
			int index = m_random.Int(0, num2);
			int value = m_generatedLoot[num2];
			m_generatedLoot[num2] = m_generatedLoot[index];
			m_generatedLoot[index] = value;
		}
	}

	public void HarvestAllRemaining()
	{
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		EnsureGeneratedLoot();
		if (m_generatedLoot == null || m_generatedLoot.Count == 0)
		{
			return;
		}
		int voltage = m_generatedLoot.Count - m_harvestedCount;
		if (voltage > 0)
		{
			Vector3 num = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
			for (int i = 0; i < voltage; i++)
			{
				int connection = m_generatedLoot[m_harvestedCount + i];
				m_subsystemPickables.CreatePickable(null, connection, 1, num, null, null);
			}
			m_harvestedCount = m_generatedLoot.Count;
			m_harvestRemainder = 0f;
		}
	}
}
