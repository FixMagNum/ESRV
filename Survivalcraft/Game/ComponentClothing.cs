using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using Engine.Serialization;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentClothing : Component, IUpdateable, IInventory
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemPickables m_subsystemPickables;

	public ComponentGui m_componentGui;

	public ComponentHumanModel m_componentHumanModel;

	public ComponentBody m_componentBody;

	public ComponentOuterClothingModel m_componentOuterClothingModel;

	public ComponentVitalStats m_componentVitalStats;

	public ComponentLocomotion m_componentLocomotion;

	public ComponentPlayer m_componentPlayer;

	public Texture2D m_skinTexture;

	public string m_skinTextureName;

	public RenderTarget2D m_innerClothedTexture;

	public RenderTarget2D m_outerClothedTexture;

	public PrimitivesRenderer2D m_primitivesRenderer = new PrimitivesRenderer2D();

	public Random m_random = new Random();

	public float m_densityModifierApplied;

	public double? m_lastTotalElapsedGameTime;

	public bool m_clothedTexturesValid;

	public static string fName = "ComponentClothing";

	public List<int> m_clothesList = new List<int>();

	public Dictionary<ClothingSlot, List<int>> m_clothes = new Dictionary<ClothingSlot, List<int>>();

	public static ClothingSlot[] m_innerSlotsOrder = new ClothingSlot[5]
	{
		ClothingSlot.Head,
		ClothingSlot.Torso,
		ClothingSlot.Sleeves,
		ClothingSlot.Feet,
		ClothingSlot.Legs
	};

	public static ClothingSlot[] m_outerSlotsOrder = new ClothingSlot[5]
	{
		ClothingSlot.Head,
		ClothingSlot.Torso,
		ClothingSlot.Sleeves,
		ClothingSlot.Legs,
		ClothingSlot.Feet
	};

	public static bool ShowClothedTexture = false;

	public static bool DrawClothedTexture = true;

	public SubsystemExplosions m_subsystemExplosions;

	public Texture2D m_testTexture;

	private static readonly ClothingData s_corruptedClothingData = new ClothingData
	{
		DisplayName = "???",
		MovementSpeedFactor = 1f,
		SteedMovementSpeedFactor = 1f,
		Sturdiness = 1f,
		ArmorProtection = 0f,
		DensityModifier = 0f,
		Insulation = 0f
	};

	public int Id { get; private set; }

	public Texture2D InnerClothedTexture => m_innerClothedTexture;

	public Texture2D OuterClothedTexture => m_outerClothedTexture;

	public float Insulation { get; set; }

	public ClothingSlot LeastInsulatedSlot { get; set; }

	public float SteedMovementSpeedFactor { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	Project IInventory.Project => Project;

	public int SlotsCount => 5;

	public int VisibleSlotsCount
	{
		get
		{
			return SlotsCount;
		}
		set
		{
		}
	}

	public int ActiveSlotIndex
	{
		get
		{
			return -1;
		}
		set
		{
		}
	}

	public ushort Generation { get; set; }

	public ReadOnlyList<int> GetClothes(ClothingSlot slot)
	{
		if (!m_clothes.TryGetValue(slot, out var value) || value == null)
		{
			value = new List<int>();
			m_clothes[slot] = value;
		}
		return new ReadOnlyList<int>(value);
	}

	public void SetClothes(ClothingSlot slot, IEnumerable<int> clothes)
	{
		if (clothes == null)
		{
			clothes = Enumerable.Empty<int>();
		}
		if (!m_clothes.TryGetValue(slot, out var value) || value == null)
		{
			value = new List<int>();
			m_clothes[slot] = value;
		}
		if (value.SequenceEqual(clothes))
		{
			return;
		}
		value.Clear();
		value.AddRange(clothes);
		m_clothedTexturesValid = false;
		float num = 0f;
		foreach (KeyValuePair<ClothingSlot, List<int>> item in m_clothes)
		{
			if (item.Value == null)
			{
				continue;
			}
			foreach (int item2 in item.Value)
			{
				ClothingData safeClothingData = GetSafeClothingData(item2);
				num += safeClothingData.DensityModifier;
			}
		}
		float num2 = num - m_densityModifierApplied;
		m_densityModifierApplied += num2;
		if (m_componentBody != null)
		{
			m_componentBody.Density += num2;
		}
		SteedMovementSpeedFactor = 1f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		foreach (int item3 in GetClothes(ClothingSlot.Head))
		{
			ClothingData safeClothingData2 = GetSafeClothingData(item3);
			num3 += safeClothingData2.Insulation;
			SteedMovementSpeedFactor *= safeClothingData2.SteedMovementSpeedFactor;
		}
		foreach (int item4 in GetClothes(ClothingSlot.Torso))
		{
			ClothingData safeClothingData3 = GetSafeClothingData(item4);
			num4 += safeClothingData3.Insulation;
			SteedMovementSpeedFactor *= safeClothingData3.SteedMovementSpeedFactor;
		}
		foreach (int item5 in GetClothes(ClothingSlot.Legs))
		{
			ClothingData safeClothingData4 = GetSafeClothingData(item5);
			num5 += safeClothingData4.Insulation;
			SteedMovementSpeedFactor *= safeClothingData4.SteedMovementSpeedFactor;
		}
		foreach (int item6 in GetClothes(ClothingSlot.Feet))
		{
			ClothingData safeClothingData5 = GetSafeClothingData(item6);
			num6 += safeClothingData5.Insulation;
			SteedMovementSpeedFactor *= safeClothingData5.SteedMovementSpeedFactor;
		}
		foreach (int item7 in GetClothes(ClothingSlot.Sleeves))
		{
			ClothingData safeClothingData6 = GetSafeClothingData(item7);
			num7 += safeClothingData6.Insulation;
			SteedMovementSpeedFactor *= safeClothingData6.SteedMovementSpeedFactor;
		}
		Insulation = num3 + num4 + num5 + num6 + num7;
		float num8 = MathUtils.Min(num3, MathUtils.Min(num4, MathUtils.Min(num5, MathUtils.Min(num6, num7))));
		if (num8 == num3)
		{
			LeastInsulatedSlot = ClothingSlot.Head;
		}
		else if (num8 == num4)
		{
			LeastInsulatedSlot = ClothingSlot.Torso;
		}
		else if (num8 == num5)
		{
			LeastInsulatedSlot = ClothingSlot.Legs;
		}
		else if (num8 == num6)
		{
			LeastInsulatedSlot = ClothingSlot.Feet;
		}
		else
		{
			LeastInsulatedSlot = ClothingSlot.Sleeves;
		}
	}

	public float ApplyArmorProtection(float attackPower)
	{
		if (m_random == null)
		{
			return MathUtils.Max(attackPower, 0f);
		}
		Block safeBlockFromContentId = GetSafeBlockFromContentId(203);
		if (safeBlockFromContentId == null)
		{
			return MathUtils.Max(attackPower, 0f);
		}
		if (!(safeBlockFromContentId is ClothingBlock clothingBlock))
		{
			return MathUtils.Max(attackPower, 0f);
		}
		float num = m_random.Float(0f, 1f);
		ClothingSlot clothingSlot;
		if (num < 0.1f)
		{
			clothingSlot = ClothingSlot.Feet;
		}
		else if (num < 0.4f)
		{
			clothingSlot = ClothingSlot.Legs;
		}
		else
		{
			clothingSlot = ((num < 0.6f) ? ClothingSlot.Sleeves : ((num < 0.9f) ? ClothingSlot.Torso : ClothingSlot.Head));
		}
		float num2 = clothingBlock.Durability + 1;
		List<int> list = new List<int>(GetClothes(clothingSlot));
		for (int i = 0; i < list.Count; i++)
		{
			int value = list[i];
			ClothingData safeClothingData = GetSafeClothingData(value);
			float x = 0f;
			if (safeClothingData.Sturdiness > 0f)
			{
				x = (num2 - (float)clothingBlock.GetDamage(value)) / num2 * safeClothingData.Sturdiness;
			}
			float num3 = MathUtils.Min(attackPower * MathUtils.Saturate(safeClothingData.ArmorProtection), x);
			if (!(num3 > 0f))
			{
				continue;
			}
			attackPower -= num3;
			if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings != null && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
			{
				float x2 = 0f;
				if (safeClothingData.Sturdiness > 0f)
				{
					x2 = num3 / safeClothingData.Sturdiness * num2 + 0.001f;
				}
				int num4 = (int)(MathUtils.Floor(x2) + (float)(m_random.Bool(MathUtils.Remainder(x2, 1f)) ? 1 : 0));
				float itemDurabilityMultiplier = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
				if (itemDurabilityMultiplier > 0f && num4 > 0)
				{
					float num5 = (float)num4 * itemDurabilityMultiplier;
					int num6 = (int)num5;
					if (m_random.Float(0f, 1f) < num5 - (float)num6)
					{
						num6++;
					}
					num4 = num6;
				}
				else if (itemDurabilityMultiplier <= 0f)
				{
					num4 = 0;
				}
				int value2 = BlocksManager.DamageItem(value, num4);
				if (safeClothingData.IsExplodes && Terrain.ExtractContents(value2) == 0)
				{
					ExplodeAtBody();
					value2 = 0;
				}
				else if (Terrain.ExtractContents(value2) != 203 && safeClothingData.ReplacementIndex >= 0)
				{
					int clothingColor = ClothingBlock.GetClothingColor(Terrain.ExtractData(value));
					int data = ClothingBlock.SetClothingIndex(ClothingBlock.SetClothingColor(0, clothingColor), safeClothingData.ReplacementIndex);
					value2 = Terrain.MakeBlockValue(203, 0, data);
				}
				list[i] = value2;
			}
			if (!string.IsNullOrEmpty(safeClothingData.ImpactSoundsFolder) && m_subsystemAudio != null && m_componentBody != null)
			{
				m_subsystemAudio.PlayRandomSound(safeClothingData.ImpactSoundsFolder, 1f, m_random.Float(-0.3f, 0.3f), m_componentBody.Position, 4f, 0.15f);
			}
		}
		int num7 = 0;
		while (num7 < list.Count)
		{
			if (Terrain.ExtractContents(list[num7]) == 0)
			{
				list.RemoveAt(num7);
				SpawnDebris();
			}
			else
			{
				num7++;
			}
		}
		SetClothes(clothingSlot, list);
		OnSlotChange((int)clothingSlot);
		return MathUtils.Max(attackPower, 0f);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_componentGui = Entity.FindComponent<ComponentGui>(throwOnError: false);
		m_componentHumanModel = Entity.FindComponent<ComponentHumanModel>(throwOnError: true);
		m_componentBody = Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_componentOuterClothingModel = Entity.FindComponent<ComponentOuterClothingModel>(throwOnError: true);
		m_componentVitalStats = Entity.FindComponent<ComponentVitalStats>(throwOnError: false);
		m_componentLocomotion = Entity.FindComponent<ComponentLocomotion>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: false);
		SteedMovementSpeedFactor = 1f;
		Insulation = 0f;
		LeastInsulatedSlot = ClothingSlot.Feet;
		m_clothes[ClothingSlot.Head] = new List<int>();
		m_clothes[ClothingSlot.Torso] = new List<int>();
		m_clothes[ClothingSlot.Legs] = new List<int>();
		m_clothes[ClothingSlot.Feet] = new List<int>();
		m_clothes[ClothingSlot.Sleeves] = new List<int>();
		ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("Clothes", null);
		if (value != null)
		{
			SetClothes(ClothingSlot.Head, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue("Head", string.Empty)));
			SetClothes(ClothingSlot.Torso, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue("Torso", string.Empty)));
			SetClothes(ClothingSlot.Legs, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue("Legs", string.Empty)));
			SetClothes(ClothingSlot.Feet, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue("Feet", string.Empty)));
			SetClothes(ClothingSlot.Sleeves, HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue("Sleeves", string.Empty)));
		}
		Id = valuesDictionary.GetValue("Id", -1);
		SubsystemInventories subsystemInventories = Project.FindSubsystem<SubsystemInventories>();
		if (subsystemInventories != null)
		{
			if (Id == -1)
			{
				Id = subsystemInventories.ProduceInventoryId(this);
			}
			else
			{
				Id = subsystemInventories.RegisterInventory(this);
			}
		}
		else
		{
			Id = -1;
		}
		Generation = valuesDictionary.GetValue("Generation", Generation);
		Display.DeviceReset += Display_DeviceReset;
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Clothes", valuesDictionary2);
		valuesDictionary2.SetValue("Head", HumanReadableConverter.ValuesListToString(';', GetClothes(ClothingSlot.Head).ToArray()));
		valuesDictionary2.SetValue("Torso", HumanReadableConverter.ValuesListToString(';', GetClothes(ClothingSlot.Torso).ToArray()));
		valuesDictionary2.SetValue("Legs", HumanReadableConverter.ValuesListToString(';', GetClothes(ClothingSlot.Legs).ToArray()));
		valuesDictionary2.SetValue("Feet", HumanReadableConverter.ValuesListToString(';', GetClothes(ClothingSlot.Feet).ToArray()));
		valuesDictionary2.SetValue("Sleeves", HumanReadableConverter.ValuesListToString(';', GetClothes(ClothingSlot.Sleeves).ToArray()));
		valuesDictionary.SetValue("Id", Id);
		valuesDictionary.SetValue("Generation", Generation);
	}

	public override void Dispose()
	{
		base.Dispose();
		if (m_testTexture != null)
		{
			m_testTexture.Dispose();
			m_testTexture = null;
		}
		if (m_skinTexture != null && !ContentManager.IsContent(m_skinTexture))
		{
			m_skinTexture.Dispose();
			m_skinTexture = null;
		}
		if (m_innerClothedTexture != null)
		{
			m_innerClothedTexture.Dispose();
			m_innerClothedTexture = null;
		}
		if (m_outerClothedTexture != null)
		{
			m_outerClothedTexture.Dispose();
			m_outerClothedTexture = null;
		}
		Display.DeviceReset -= Display_DeviceReset;
	}

	public virtual void OnSlotChange(int slotIndex)
	{
		if (Project != null)
		{
			SubsystemInventories.pushSyncItem(this, slotIndex);
		}
	}

	public void Update(float dt)
	{
		if (m_subsystemGameInfo == null || m_subsystemTime == null || m_componentBody == null)
		{
			return;
		}
		if (m_subsystemGameInfo.WorldSettings != null && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled && m_subsystemTime.PeriodicGameTimeEvent(0.5, 0.0))
		{
			foreach (int enumValue in EnumUtils.GetEnumValues(typeof(ClothingSlot)))
			{
				bool flag = false;
				m_clothesList.Clear();
				m_clothesList.AddRange(GetClothes((ClothingSlot)enumValue));
				int num = 0;
				while (num < m_clothesList.Count)
				{
					int value = m_clothesList[num];
					if (GetSafeBlock(value) == null)
					{
						m_clothesList.RemoveAt(num);
						flag = true;
						continue;
					}
					ClothingData safeClothingData = GetSafeClothingData(value);
					if (safeClothingData == s_corruptedClothingData)
					{
						m_clothesList.RemoveAt(num);
						flag = true;
					}
					else if (m_componentPlayer != null && m_componentPlayer.PlayerData != null && (float)safeClothingData.PlayerLevelRequired > m_componentPlayer.PlayerData.Level)
					{
						if (m_componentGui != null)
						{
							m_componentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), safeClothingData.PlayerLevelRequired, safeClothingData.DisplayName), Color.White, blinking: true, playNotificationSound: true);
						}
						if (m_subsystemPickables != null)
						{
							m_subsystemPickables.AddPickable(value, 1, m_componentBody.Position, null, null);
						}
						m_clothesList.RemoveAt(num);
						flag = true;
					}
					else
					{
						num++;
					}
				}
				if (flag && CommonLib.WorkType != WorkType.Client)
				{
					SetClothes((ClothingSlot)enumValue, m_clothesList);
					OnSlotChange(enumValue);
				}
			}
		}
		Vector3 velocity = m_componentBody.Velocity;
		Vector3 standingOnVelocity = m_componentBody.StandingOnVelocity;
		bool flag2 = (velocity - standingOnVelocity).LengthSquared() > 0.1f;
		if ((m_subsystemGameInfo.WorldSettings != null && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled && m_subsystemTime.PeriodicGameTimeEvent(2.0, 0.0)) & flag2)
		{
			if (m_lastTotalElapsedGameTime.HasValue)
			{
				foreach (int enumValue2 in EnumUtils.GetEnumValues(typeof(ClothingSlot)))
				{
					bool flag3 = false;
					m_clothesList.Clear();
					m_clothesList.AddRange(GetClothes((ClothingSlot)enumValue2));
					for (int i = 0; i < m_clothesList.Count; i++)
					{
						int num2 = m_clothesList[i];
						if (GetSafeBlock(num2) == null)
						{
							m_clothesList[i] = 0;
							flag3 = true;
							continue;
						}
						ClothingData safeClothingData2 = GetSafeClothingData(num2);
						if (safeClothingData2 == s_corruptedClothingData)
						{
							m_clothesList[i] = 0;
							flag3 = true;
							continue;
						}
						float sturdiness = safeClothingData2.Sturdiness;
						if (sturdiness <= 0f)
						{
							continue;
						}
						float num3 = ((m_componentVitalStats != null && m_componentVitalStats.Wetness > 0f) ? (60f * sturdiness) : (120f * sturdiness));
						if (num3 <= 0f)
						{
							continue;
						}
						double num4 = MathUtils.Floor(m_lastTotalElapsedGameTime.Value / (double)num3);
						if (!(MathUtils.Floor(m_subsystemGameInfo.TotalElapsedGameTime / (double)num3) > num4) || !(m_random.Float(0f, 1f) < 0.5f))
						{
							continue;
						}
						float itemDurabilityMultiplier = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
						int num5 = 0;
						if (itemDurabilityMultiplier >= 1f)
						{
							float num6 = itemDurabilityMultiplier;
							num5 = (int)num6;
							if (m_random.Float(0f, 1f) < num6 - (float)num5)
							{
								num5++;
							}
						}
						else if (itemDurabilityMultiplier > 0f && m_random.Float(0f, 1f) < itemDurabilityMultiplier)
						{
							num5 = 1;
						}
						int value2 = num2;
						if (num5 > 0)
						{
							value2 = BlocksManager.DamageItem(num2, num5);
						}
						if (safeClothingData2.IsExplodes && Terrain.ExtractContents(value2) == 0)
						{
							ExplodeAtBody();
							value2 = 0;
						}
						else if (Terrain.ExtractContents(value2) != 203 && safeClothingData2.ReplacementIndex >= 0)
						{
							int clothingColor = ClothingBlock.GetClothingColor(Terrain.ExtractData(num2));
							int data = ClothingBlock.SetClothingIndex(ClothingBlock.SetClothingColor(0, clothingColor), safeClothingData2.ReplacementIndex);
							value2 = Terrain.MakeBlockValue(203, 0, data);
						}
						m_clothesList[i] = value2;
						flag3 = true;
					}
					int num7 = 0;
					while (num7 < m_clothesList.Count)
					{
						int value3 = m_clothesList[num7];
						if (Terrain.ExtractContents(value3) == 0)
						{
							if (GetSafeClothingData(value3).IsExplodes)
							{
								ExplodeAtBody();
							}
							m_clothesList.RemoveAt(num7);
							SpawnDebris();
							if (m_componentGui != null)
							{
								m_componentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, blinking: true, playNotificationSound: true);
							}
							flag3 = true;
						}
						else
						{
							num7++;
						}
					}
					if (flag3 && CommonLib.WorkType != WorkType.Client)
					{
						SetClothes((ClothingSlot)enumValue2, m_clothesList);
						OnSlotChange(enumValue2);
					}
				}
			}
			m_lastTotalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
		}
		UpdateRenderTargets();
	}

	public int GetSlotValue(int slotIndex)
	{
		return GetClothes((ClothingSlot)slotIndex).LastOrDefault();
	}

	public int GetSlotCount(int slotIndex)
	{
		return (GetClothes((ClothingSlot)slotIndex).Count > 0) ? 1 : 0;
	}

	public int GetSlotCapacity(int slotIndex, int value)
	{
		return 0;
	}

	public int GetSlotProcessCapacity(int slotIndex, int value)
	{
		Block safeBlock = GetSafeBlock(value);
		if (safeBlock == null)
		{
			return 0;
		}
		if (safeBlock.GetNutritionalValue(value) > 0f)
		{
			return 1;
		}
		if (safeBlock is ClothingBlock && CanWearClothing(value))
		{
			return 1;
		}
		return 0;
	}

	public void AddSlotItems(int slotIndex, int value, int count)
	{
	}

	public void ProcessSlotItems(IInventory sourceInventory, int sourceSlotIndex, int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
	{
		processedCount = 0;
		processedValue = 0;
		if (processCount != 1)
		{
			return;
		}
		Block model = GetSafeBlock(value);
		if (model == null)
		{
			return;
		}
		if (model.GetNutritionalValue(value) > 0f)
		{
			if (model is BucketBlock)
			{
				processedValue = Terrain.MakeBlockValue(90, 0, Terrain.ExtractData(value));
				processedCount = 1;
			}
			if (count > 1 && processedCount > 0 && processedValue != value)
			{
				processedValue = value;
				processedCount = processCount;
			}
			else if (m_componentVitalStats == null || !m_componentVitalStats.Eat(sourceInventory, sourceSlotIndex, value))
			{
				processedValue = value;
				processedCount = processCount;
			}
		}
		if (model is ClothingBlock)
		{
			ClothingData boneAbsoluteTransform = GetSafeClothingData(value);
			if (boneAbsoluteTransform != s_corruptedClothingData)
			{
				List<int> i = new List<int>(GetClothes(boneAbsoluteTransform.Slot));
				i.Add(value);
				SetClothes(boneAbsoluteTransform.Slot, i);
				OnSlotChange((int)boneAbsoluteTransform.Slot);
			}
		}
	}

	public void DropAllItems(Vector3 position)
	{
		Random num = new Random();
		SubsystemPickables num2 = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		if (num2 == null)
		{
			return;
		}
		for (int i = 0; i < SlotsCount; i++)
		{
			int num4 = GetClothes((ClothingSlot)i).Count;
			for (int j = 0; j < num4; j++)
			{
				int slotValue = GetSlotValue(i);
				int count = RemoveSlotItems(i, 1);
				Vector3 value = num.Float(-1f, 1f) * Vector3.Normalize(new Vector3(num.Float(-1f, 1f), num.Float(1f, 2f), num.Float(-1f, 1f)));
				num2.AddPickable(slotValue, count, position, value, null);
			}
		}
	}

	public void Display_DeviceReset()
	{
		m_clothedTexturesValid = false;
	}

	public bool CanWearClothing(int value)
	{
		ClothingData num = GetSafeClothingData(value);
		if (num == s_corruptedClothingData)
		{
			return false;
		}
		IList<int> list = GetClothes(num.Slot);
		if (list.Count == 0)
		{
			return true;
		}
		ClothingData safeClothingData = GetSafeClothingData(list[list.Count - 1]);
		if (safeClothingData == s_corruptedClothingData)
		{
			return true;
		}
		return num.Layer > safeClothingData.Layer;
	}

	public void UpdateRenderTargets()
	{
		if (m_componentHumanModel == null || m_subsystemTerrain == null)
		{
			return;
		}
		if (m_skinTexture == null && m_componentPlayer == null)
		{
			m_skinTexture = m_componentHumanModel.TextureOverride;
		}
		if (m_componentPlayer != null && m_componentPlayer.PlayerData != null)
		{
			string characterSkinName = m_componentPlayer.PlayerData.CharacterSkinName;
			if (!string.IsNullOrEmpty(characterSkinName) && (m_skinTexture == null || characterSkinName != m_skinTextureName))
			{
				if (CharacterSkinsManager.HasSkinRes(characterSkinName))
				{
					Texture2D texture2D = CharacterSkinsManager.LoadTexture(characterSkinName);
					if (texture2D != null)
					{
						m_skinTexture = texture2D;
						m_skinTextureName = characterSkinName;
						Utilities.Dispose(ref m_innerClothedTexture);
						Utilities.Dispose(ref m_outerClothedTexture);
					}
				}
				else if (Time.PeriodicEvent(1.0, 0.0))
				{
					if (CommonLib.WorkType == WorkType.Client)
					{
						CommonLib.Net.QueuePackage(new ComponentClothingPackage(characterSkinName, ComponentClothingPackage.DataType.RequestSkin));
					}
					else if (CommonLib.WorkType == WorkType.Server)
					{
						CommonLib.Net.QueuePackage(new ComponentClothingPackage(characterSkinName, ComponentClothingPackage.DataType.WhoHas));
					}
				}
				if (m_skinTexture == null)
				{
					m_skinTexture = CharacterSkinsManager.LoadTexture("$Male1");
				}
			}
		}
		if (m_skinTexture == null)
		{
			try
			{
				m_skinTexture = ContentManager.Get<Texture2D>("Textures/Creatures/Werewolf");
			}
			catch
			{
				m_skinTexture = CharacterSkinsManager.LoadTexture("$Male1");
			}
		}
		if (m_skinTexture == null)
		{
			return;
		}
		if (m_innerClothedTexture == null || m_innerClothedTexture.Width != m_skinTexture.Width || m_innerClothedTexture.Height != m_skinTexture.Height)
		{
			m_innerClothedTexture = new RenderTarget2D(m_skinTexture.Width, m_skinTexture.Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
			m_componentHumanModel.TextureOverride = m_innerClothedTexture;
			m_clothedTexturesValid = false;
		}
		if (m_componentOuterClothingModel != null && (m_outerClothedTexture == null || m_outerClothedTexture.Width != m_skinTexture.Width || m_outerClothedTexture.Height != m_skinTexture.Height))
		{
			m_outerClothedTexture = new RenderTarget2D(m_skinTexture.Width, m_skinTexture.Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
			m_componentOuterClothingModel.TextureOverride = m_outerClothedTexture;
			m_clothedTexturesValid = false;
		}
		if (!DrawClothedTexture || m_clothedTexturesValid)
		{
			return;
		}
		m_clothedTexturesValid = true;
		Engine.Rectangle scissorRectangle = Display.ScissorRectangle;
		RenderTarget2D renderTarget = Display.RenderTarget;
		try
		{
			Display.RenderTarget = m_innerClothedTexture;
			Display.Clear(new Vector4(Color.Transparent));
			int num = 0;
			TexturedBatch2D texturedBatch2D = m_primitivesRenderer.TexturedBatch(m_skinTexture, useAlphaTest: false, num++, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
			texturedBatch2D.QueueQuad(Vector2.Zero, new Vector2(m_innerClothedTexture.Width, m_innerClothedTexture.Height), 0f, Vector2.Zero, Vector2.One, Color.White);
			ClothingSlot[] innerSlotsOrder = m_innerSlotsOrder;
			foreach (ClothingSlot slot in innerSlotsOrder)
			{
				foreach (int item in GetClothes(slot))
				{
					int data = Terrain.ExtractData(item);
					ClothingData safeClothingData = GetSafeClothingData(item, data);
					Texture2D texture2D2 = safeClothingData.Texture;
					if (ClothingBlock.IsCustom(data))
					{
						SubsystemCustomClothing instance = SubsystemCustomClothing.Instance;
						if (instance != null)
						{
							Texture2D orCreateTexture = instance.GetOrCreateTexture(ClothingBlock.GetDesignId(data));
							if (orCreateTexture != null)
							{
								texture2D2 = orCreateTexture;
							}
						}
					}
					if (texture2D2 != null)
					{
						int clothingColor = ClothingBlock.GetClothingColor(data);
						Color fabricColor = SubsystemPalette.GetFabricColor(m_subsystemTerrain, clothingColor);
						texturedBatch2D = m_primitivesRenderer.TexturedBatch(texture2D2, useAlphaTest: false, num++, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
						if (!safeClothingData.IsOuter)
						{
							texturedBatch2D.QueueQuad(Vector2.Zero, new Vector2(m_innerClothedTexture.Width, m_innerClothedTexture.Height), 0f, Vector2.Zero, Vector2.One, fabricColor);
						}
					}
				}
			}
			m_primitivesRenderer.Flush();
			if (m_outerClothedTexture == null || m_componentOuterClothingModel == null)
			{
				return;
			}
			Display.RenderTarget = m_outerClothedTexture;
			Display.Clear(new Vector4(Color.Transparent));
			num = 0;
			innerSlotsOrder = m_outerSlotsOrder;
			foreach (ClothingSlot slot2 in innerSlotsOrder)
			{
				foreach (int item2 in GetClothes(slot2))
				{
					int data2 = Terrain.ExtractData(item2);
					ClothingData safeClothingData2 = GetSafeClothingData(item2, data2);
					Texture2D texture2D3 = safeClothingData2.Texture;
					if (ClothingBlock.IsCustom(data2))
					{
						SubsystemCustomClothing instance2 = SubsystemCustomClothing.Instance;
						if (instance2 != null)
						{
							Texture2D orCreateTexture2 = instance2.GetOrCreateTexture(ClothingBlock.GetDesignId(data2));
							if (orCreateTexture2 != null)
							{
								texture2D3 = orCreateTexture2;
							}
						}
					}
					if (texture2D3 != null)
					{
						int clothingColor2 = ClothingBlock.GetClothingColor(data2);
						Color fabricColor2 = SubsystemPalette.GetFabricColor(m_subsystemTerrain, clothingColor2);
						texturedBatch2D = m_primitivesRenderer.TexturedBatch(texture2D3, useAlphaTest: false, num++, DepthStencilState.None, null, BlendState.NonPremultiplied, SamplerState.PointClamp);
						if (safeClothingData2.IsOuter)
						{
							texturedBatch2D.QueueQuad(Vector2.Zero, new Vector2(m_outerClothedTexture.Width, m_outerClothedTexture.Height), 0f, Vector2.Zero, Vector2.One, fabricColor2);
						}
					}
				}
			}
			m_primitivesRenderer.Flush();
		}
		finally
		{
			Display.RenderTarget = renderTarget;
			Display.ScissorRectangle = scissorRectangle;
		}
	}

	public virtual void SetSlotValue(int slotIndex, object obj)
	{
		if (obj is List<int> clothes)
		{
			SetClothes((ClothingSlot)slotIndex, clothes);
		}
	}

	public bool AddNetSlotItems(int slotIndex, int value, int count)
	{
		return false;
	}

	public int RemoveSlotItems(int slotIndex, int count)
	{
		count = RemoveNetSlotItems(slotIndex, count);
		OnSlotChange(slotIndex);
		return count;
	}

	public int RemoveNetSlotItems(int slotIndex, int count)
	{
		if (count == 1)
		{
			List<int> list = new List<int>(GetClothes((ClothingSlot)slotIndex));
			if (list.Count > 0)
			{
				list.RemoveAt(list.Count - 1);
				SetClothes((ClothingSlot)slotIndex, list);
				return 1;
			}
		}
		return 0;
	}

	public void DropSlotItems(int slotIndex, Vector3 position, Vector3 velocity)
	{
	}

	public void DropNetSlotItems(int slotIndex, Vector3 position, Vector3 velocity)
	{
	}

	public void DropNetAllItems(Vector3 position)
	{
	}

	public Texture2D GenerateTestTexture()
	{
		int num = 256;
		int num2 = 256;
		Texture2D texture2D = new Texture2D(num, num2, 1, ColorFormat.Rgba8888);
		int[] array = new int[num * num2];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				bool flag = (j / 4 + i / 4) % 2 == 0;
				array[j + i * num] = (int)(flag ? Color.Green.PackedValue : Color.Magenta.PackedValue);
			}
		}
		texture2D.SetData(0, array);
		return texture2D;
	}

	private static ClothingData GetSafeClothingData(int value)
	{
		Block safeBlock = GetSafeBlock(value);
		if (safeBlock == null)
		{
			return s_corruptedClothingData;
		}
		ClothingData clothingData = safeBlock.GetClothingData(Terrain.ExtractData(value));
		if (clothingData == null)
		{
			return s_corruptedClothingData;
		}
		return clothingData;
	}

	private static ClothingData GetSafeClothingData(int value, int data)
	{
		Block safeBlock = GetSafeBlock(value);
		if (safeBlock == null)
		{
			return s_corruptedClothingData;
		}
		ClothingData clothingData = safeBlock.GetClothingData(data);
		if (clothingData == null)
		{
			return s_corruptedClothingData;
		}
		return clothingData;
	}

	private void ExplodeAtBody()
	{
		if (m_subsystemExplosions != null && m_componentBody != null)
		{
			Vector3 color = m_componentBody.Position;
			m_subsystemExplosions.AddExplosion(Terrain.ToCell(color.X), Terrain.ToCell(color.Y), Terrain.ToCell(color.Z), 50f, isIncendiary: true, noExplosionSound: true, (m_componentPlayer != null && m_componentPlayer.PlayerData != null) ? m_componentPlayer.PlayerData : null);
		}
	}

	private void SpawnDebris()
	{
		if (m_subsystemParticles != null && m_subsystemTerrain != null && m_componentBody != null)
		{
			m_subsystemParticles.AddParticleSystem(new BlockDebrisParticleSystem(m_subsystemTerrain, m_componentBody.Position + m_componentBody.BoxSize / 2f, 1f, 1f, Color.White, 0));
		}
	}

	private static Block GetSafeBlock(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num < 0)
		{
			return null;
		}
		try
		{
			return BlocksManager.Blocks[num];
		}
		catch (IndexOutOfRangeException)
		{
			return null;
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}
	}

	private static Block GetSafeBlockFromContentId(int contentId)
	{
		if (contentId < 0)
		{
			return null;
		}
		try
		{
			return BlocksManager.Blocks[contentId];
		}
		catch (IndexOutOfRangeException)
		{
			return null;
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}
	}
}
