using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentLevel : Component, IUpdateable
{
	public struct Factor
	{
		public string Description;

		public float Value;
	}

	public Random m_random = new Random();

	public static string fName;

	public List<Factor> m_factors = new List<Factor>();

	public float? m_lastLevelTextValue;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemTime m_subsystemTime;

	public ComponentPlayer m_componentPlayer;

	public const float FemaleStrengthFactor = 0.8f;

	public const float FemaleResilienceFactor = 0.8f;

	public const float FemaleSpeedFactor = 1.1f;

	public const float FemaleHungerFactor = 0.7f;

	public float StrengthFactor { get; set; }

	public float ResilienceFactor { get; set; }

	public float SpeedFactor { get; set; }

	public float HungerFactor { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	private bool IsEmbrace
	{
		get
		{
			if (m_subsystemGameInfo != null)
			{
				return m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
			}
			return false;
		}
	}

	public void AddExperience(int count, bool playSound)
	{
		if (!m_subsystemGameInfo.WorldSettings.IsImmutableWorldLevelEnabled && CommonLib.WorkType != WorkType.Client)
		{
			NetAddExperience(count, playSound);
			CommonLib.Net.QueuePackage(new ComponentPlayerPackage(m_componentPlayer.PlayerData, count, playSound, m_componentPlayer.PlayerData.Level));
		}
	}

	public void NetAddExperience(int count, bool playSound)
	{
		if (playSound)
		{
			m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 0.2f, m_random.Float(-0.1f, 0.4f), m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
		}
		for (int i = 0; i < count; i++)
		{
			float num = 0.012f / MathUtils.Pow(1.08f, MathUtils.Floor(m_componentPlayer.PlayerData.Level - 1f)) * m_subsystemGameInfo.WorldSettings.ExperienceGainMultiplier;
			if (MathUtils.Floor(m_componentPlayer.PlayerData.Level + num) > MathUtils.Floor(m_componentPlayer.PlayerData.Level))
			{
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.0, () =>
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, blinking: true, playNotificationSound: false);
				});
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.0, () =>
				{
					m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.2f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
				});
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.15000000596046448, () =>
				{
					m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.03333333f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
				});
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.30000001192092896, () =>
				{
					m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, 2f / 15f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
				});
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.45000001788139343, () =>
				{
					m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, 23f / 60f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
				});
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.75, () =>
				{
					m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.03333333f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
				});
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.9000000357627869, () =>
				{
					m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, 23f / 60f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
				});
			}
			m_componentPlayer.PlayerData.Level += num;
		}
	}

	public float CalculateStrengthFactor(ICollection<Factor> factors)
	{
		float num = ((m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.8f : 1f);
		float num2 = 1f * num;
		if (factors != null)
		{
			Factor item = new Factor
			{
				Value = num,
				Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
			};
			factors.Add(item);
		}
		float level = m_componentPlayer.PlayerData.Level;
		float num3 = ScaledLevelForStats(level);
		float num4 = 1f + 0.05f * MathUtils.Floor(num3 - 1f);
		if (IsEmbrace)
		{
			PlayerData.PlayerTag tag = m_componentPlayer.PlayerData.Tag;
			if (tag.ToString() == "Fat")
			{
				num4 *= 2f;
			}
			if (tag.ToString() == "Dwarf")
			{
				num4 *= 0f;
			}
		}
		float num5 = num2 * num4;
		if (factors != null)
		{
			Factor item2 = new Factor
			{
				Value = num4,
				Description = string.Format(LanguageControl.Get(fName, 2), MathUtils.Floor(level).ToString())
			};
			factors.Add(item2);
		}
		float stamina = m_componentPlayer.ComponentVitalStats.Stamina;
		float num6 = MathUtils.Lerp(0.5f, 1f, MathUtils.Saturate(4f * stamina)) * MathUtils.Lerp(0.9f, 1f, MathUtils.Saturate(stamina));
		float num7 = num5 * num6;
		if (factors != null)
		{
			Factor item3 = new Factor
			{
				Value = num6,
				Description = string.Format(LanguageControl.Get(fName, 3), $"{stamina * 100f:0}")
			};
			factors.Add(item3);
		}
		float num8 = (m_componentPlayer.ComponentSickness.IsSick ? 0.75f : 1f);
		float num9 = num7 * num8;
		if (factors != null)
		{
			Factor item4 = new Factor
			{
				Value = num8,
				Description = (m_componentPlayer.ComponentSickness.IsSick ? LanguageControl.Get(fName, 4) : LanguageControl.Get(fName, 5))
			};
			factors.Add(item4);
		}
		float num10 = ((!m_componentPlayer.ComponentSickness.IsPuking) ? 1 : 0);
		float num11 = num9 * num10;
		if (factors != null)
		{
			Factor item5 = new Factor
			{
				Value = num10,
				Description = (m_componentPlayer.ComponentSickness.IsPuking ? LanguageControl.Get(fName, 6) : LanguageControl.Get(fName, 7))
			};
			factors.Add(item5);
		}
		float num12 = (m_componentPlayer.ComponentFlu.HasFlu ? 0.75f : 1f);
		float num13 = num11 * num12;
		if (factors != null)
		{
			Factor item6 = new Factor
			{
				Value = num12,
				Description = (m_componentPlayer.ComponentFlu.HasFlu ? LanguageControl.Get(fName, 8) : LanguageControl.Get(fName, 9))
			};
			factors.Add(item6);
		}
		float num14 = ((!m_componentPlayer.ComponentFlu.IsCoughing) ? 1 : 0);
		float num15 = num13 * num14;
		if (factors != null)
		{
			Factor item7 = new Factor
			{
				Value = num14,
				Description = (m_componentPlayer.ComponentFlu.IsCoughing ? LanguageControl.Get(fName, 10) : LanguageControl.Get(fName, 11))
			};
			factors.Add(item7);
		}
		float num16 = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless) ? 1.25f : 1f);
		float result = num15 * num16;
		if (factors != null)
		{
			Factor item8 = new Factor
			{
				Value = num16,
				Description = string.Format(LanguageControl.Get(fName, 12), m_subsystemGameInfo.WorldSettings.GameMode.ToString())
			};
			factors.Add(item8);
		}
		return result;
	}

	public float CalculateResilienceFactor(ICollection<Factor> factors)
	{
		float num = ((m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.8f : 1f);
		float num2 = 1f * num;
		if (factors != null)
		{
			Factor item = new Factor
			{
				Value = num,
				Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
			};
			factors.Add(item);
		}
		float num3 = m_componentPlayer.PlayerData.Level;
		float num4 = ScaledLevelForStats(num3);
		float num5 = 1f + 0.05f * MathUtils.Floor(num4 - 1f);
		float num6 = num2 * num5;
		if (factors != null)
		{
			Factor item2 = new Factor
			{
				Value = num5,
				Description = string.Format(LanguageControl.Get(fName, 2), MathUtils.Floor(num3).ToString())
			};
			factors.Add(item2);
		}
		if (m_componentPlayer.ComponentVitalStats.Food >= 0.75f)
		{
			float num7 = 1.2f;
			num6 *= num7;
			if (factors != null)
			{
				Factor item3 = new Factor
				{
					Value = num7,
					Description = LanguageControl.Get(fName, 13)
				};
				factors.Add(item3);
			}
		}
		float num8 = (m_componentPlayer.ComponentSickness.IsSick ? 0.75f : 1f);
		float num9 = num6 * num8;
		if (factors != null)
		{
			Factor item4 = new Factor
			{
				Value = num8,
				Description = (m_componentPlayer.ComponentSickness.IsSick ? LanguageControl.Get(fName, 4) : LanguageControl.Get(fName, 5))
			};
			factors.Add(item4);
		}
		float num10 = (m_componentPlayer.ComponentFlu.HasFlu ? 0.75f : 1f);
		float num11 = num9 * num10;
		if (factors != null)
		{
			Factor item5 = new Factor
			{
				Value = num10,
				Description = (m_componentPlayer.ComponentFlu.HasFlu ? LanguageControl.Get(fName, 8) : LanguageControl.Get(fName, 9))
			};
			factors.Add(item5);
		}
		float num12 = 1f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
		{
			num12 = 1.5f;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			num12 = float.PositiveInfinity;
		}
		float result = num11 * num12;
		if (factors != null)
		{
			Factor item6 = new Factor
			{
				Value = num12,
				Description = string.Format(LanguageControl.Get(fName, 12), m_subsystemGameInfo.WorldSettings.GameMode.ToString())
			};
			factors.Add(item6);
		}
		return result;
	}

	public float CalculateSpeedFactor(ICollection<Factor> factors)
	{
		float volume = 1f;
		float streamingSource = ((m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 1.1f : 1f);
		volume *= streamingSource;
		if (IsEmbrace)
		{
			PlayerData.PlayerTag tag = m_componentPlayer.PlayerData.Tag;
			if (tag.ToString() == "Short")
			{
				volume *= 1.15f;
				if (factors != null)
				{
					Factor item = new Factor
					{
						Value = 1.15f,
						Description = "Short (Agile)"
					};
					factors.Add(item);
				}
			}
			if (tag.ToString() == "Fat")
			{
				volume *= 0.85f;
				if (factors != null)
				{
					Factor item2 = new Factor
					{
						Value = 0.85f,
						Description = "Fat (Heavy)"
					};
					factors.Add(item2);
				}
			}
		}
		if (factors != null)
		{
			Factor item3 = new Factor
			{
				Value = streamingSource,
				Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
			};
			factors.Add(item3);
		}
		float num = m_componentPlayer.PlayerData.Level;
		float num2 = ScaledLevelForStats(num);
		float ex = 1f + 0.02f * MathUtils.Floor(num2 - 1f);
		volume *= ex;
		if (factors != null)
		{
			Factor item4 = new Factor
			{
				Value = ex,
				Description = string.Format(LanguageControl.Get(fName, 2), MathUtils.Floor(num).ToString())
			};
			factors.Add(item4);
		}
		float food = m_componentPlayer.ComponentVitalStats.Food;
		if (food >= 0.75f)
		{
			volume *= 1.1f;
			if (factors != null)
			{
				Factor item5 = new Factor
				{
					Value = 1.1f,
					Description = LanguageControl.Get(fName, 13)
				};
				factors.Add(item5);
			}
		}
		else if (food <= 0.1f)
		{
			volume *= 0.9f;
			if (factors != null)
			{
				Factor item6 = new Factor
				{
					Value = 0.9f,
					Description = LanguageControl.Get(fName, 14)
				};
				factors.Add(item6);
			}
		}
		float temperature = m_componentPlayer.ComponentVitalStats.Temperature;
		if (temperature <= 6f)
		{
			float num3 = MathUtils.Clamp(temperature / 6f, 0f, 1f);
			volume *= num3;
			if (temperature == 0f)
			{
				volume = 0f;
			}
			if (factors != null)
			{
				Factor item7 = new Factor
				{
					Value = num3,
					Description = string.Format(LanguageControl.Get(fName, 15), temperature)
				};
				factors.Add(item7);
			}
		}
		float clothingFactor = 1f;
		foreach (int item14 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Head))
		{
			AddClothingFactor(item14, ref clothingFactor, factors);
		}
		foreach (int item15 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Torso))
		{
			AddClothingFactor(item15, ref clothingFactor, factors);
		}
		foreach (int item16 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Legs))
		{
			AddClothingFactor(item16, ref clothingFactor, factors);
		}
		foreach (int item17 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Feet))
		{
			AddClothingFactor(item17, ref clothingFactor, factors);
		}
		volume *= clothingFactor;
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
		{
			IInventory inventory = Entity.FindComponent<ComponentMiner>()?.Inventory;
			if (inventory != null)
			{
				int slotsCount = inventory.SlotsCount;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				for (int i = 0; i < slotsCount; i++)
				{
					int slotValue = inventory.GetSlotValue(i);
					int slotCount = inventory.GetSlotCount(i);
					if (slotValue != 0 && slotCount > 0)
					{
						switch (Terrain.ExtractContents(slotValue))
						{
						case 364:
							num4++;
							break;
						case 416:
							num5++;
							break;
						case 421:
							num6++;
							break;
						case 422:
							num7++;
							break;
						}
					}
				}
				float num8 = 1f;
				if (num4 > 0)
				{
					num8 *= MathUtils.Pow(0.98f, num4);
				}
				if (num5 > 0)
				{
					num8 *= MathUtils.Pow(0.97f, num5);
				}
				if (num6 > 0)
				{
					num8 *= MathUtils.Pow(0.955f, num6);
				}
				if (num7 > 0)
				{
					num8 *= MathUtils.Pow(0.92f, num7);
				}
				num8 = MathUtils.Clamp(num8, 0.35f, 1f);
				volume *= num8;
				if (factors != null && num8 != 1f)
				{
					Factor item8 = new Factor
					{
						Value = num8,
						Description = "Bags penalty"
					};
					factors.Add(item8);
				}
			}
		}
		float stamina = m_componentPlayer.ComponentVitalStats.Stamina;
		float num9 = MathUtils.Lerp(0.5f, 1f, MathUtils.Saturate(4f * stamina)) * MathUtils.Lerp(0.9f, 1f, MathUtils.Saturate(stamina));
		volume *= num9;
		if (factors != null)
		{
			Factor item9 = new Factor
			{
				Value = num9,
				Description = string.Format(LanguageControl.Get(fName, 3), $"{stamina * 100f:0}")
			};
			factors.Add(item9);
		}
		float num10 = (m_componentPlayer.ComponentSickness.IsSick ? 0.75f : 1f);
		volume *= num10;
		if (factors != null)
		{
			Factor item10 = new Factor
			{
				Value = num10,
				Description = (m_componentPlayer.ComponentSickness.IsSick ? LanguageControl.Get(fName, 4) : LanguageControl.Get(fName, 5))
			};
			factors.Add(item10);
		}
		float num11 = ((!m_componentPlayer.ComponentSickness.IsPuking) ? 1 : 0);
		volume *= num11;
		if (factors != null)
		{
			Factor item11 = new Factor
			{
				Value = num11,
				Description = (m_componentPlayer.ComponentSickness.IsPuking ? LanguageControl.Get(fName, 6) : LanguageControl.Get(fName, 7))
			};
			factors.Add(item11);
		}
		float num12 = (m_componentPlayer.ComponentFlu.HasFlu ? 0.75f : 1f);
		volume *= num12;
		if (factors != null)
		{
			Factor item12 = new Factor
			{
				Value = num12,
				Description = (m_componentPlayer.ComponentFlu.HasFlu ? LanguageControl.Get(fName, 8) : LanguageControl.Get(fName, 9))
			};
			factors.Add(item12);
		}
		float num13 = ((!m_componentPlayer.ComponentFlu.IsCoughing) ? 1 : 0);
		volume *= num13;
		if (factors != null)
		{
			Factor item13 = new Factor
			{
				Value = num13,
				Description = (m_componentPlayer.ComponentFlu.IsCoughing ? LanguageControl.Get(fName, 10) : LanguageControl.Get(fName, 11))
			};
			factors.Add(item13);
		}
		return volume;
	}

	public float CalculateHungerFactor(ICollection<Factor> factors)
	{
		float num = ((m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.7f : 1f);
		float num2 = 1f * num;
		if (factors != null)
		{
			Factor item = new Factor
			{
				Value = num,
				Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
			};
			factors.Add(item);
		}
		float level = m_componentPlayer.PlayerData.Level;
		float num3 = ScaledLevelForStats(level);
		float num4 = 1f - 0.01f * MathUtils.Floor(num3 - 1f);
		if (IsEmbrace)
		{
			num4 = MathUtils.Max(num4, 0f);
		}
		float num5 = num2 * num4;
		if (factors != null)
		{
			Factor item2 = new Factor
			{
				Value = num4,
				Description = string.Format(LanguageControl.Get(fName, 2), MathUtils.Floor(level).ToString())
			};
			factors.Add(item2);
		}
		float num6 = 1f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
		{
			num6 = 0.66f;
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			num6 = 0f;
		}
		float result = num5 * num6;
		if (factors != null)
		{
			Factor item3 = new Factor
			{
				Value = num6,
				Description = string.Format(LanguageControl.Get(fName, 12), m_subsystemGameInfo.WorldSettings.GameMode.ToString())
			};
			factors.Add(item3);
		}
		return result;
	}

	public void Update(float dt)
	{
		if (m_subsystemGameInfo.WorldSettings.IsImmutableWorldLevelEnabled)
		{
			m_componentPlayer.PlayerData.Level = m_subsystemGameInfo.WorldSettings.ImmutableWorldLevel;
		}
		else if (m_subsystemTime.PeriodicGameTimeEvent(180.0, 179.0))
		{
			AddExperience(1, playSound: false);
		}
		StrengthFactor = CalculateStrengthFactor(null);
		SpeedFactor = CalculateSpeedFactor(null);
		HungerFactor = CalculateHungerFactor(null);
		ResilienceFactor = CalculateResilienceFactor(null);
		if (!m_lastLevelTextValue.HasValue || m_lastLevelTextValue.Value != MathUtils.Floor(m_componentPlayer.PlayerData.Level))
		{
			m_componentPlayer.ComponentGui.LevelLabelWidget.Text = string.Format(LanguageControl.Get(fName, 2), MathUtils.Floor(m_componentPlayer.PlayerData.Level).ToString());
			m_lastLevelTextValue = MathUtils.Floor(m_componentPlayer.PlayerData.Level);
		}
		m_componentPlayer.PlayerStats.HighestLevel = MathUtils.Max(m_componentPlayer.PlayerStats.HighestLevel, m_componentPlayer.PlayerData.Level);
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		StrengthFactor = 1f;
		SpeedFactor = 1f;
		HungerFactor = 1f;
		ResilienceFactor = 1f;
	}

	public static void AddClothingFactor(int clothingValue, ref float clothingFactor, ICollection<Factor> factors)
	{
		ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(clothingValue)].GetClothingData(Terrain.ExtractData(clothingValue));
		if (clothingData.MovementSpeedFactor != 1f)
		{
			clothingFactor *= clothingData.MovementSpeedFactor;
			factors?.Add(new Factor
			{
				Value = clothingData.MovementSpeedFactor,
				Description = clothingData.DisplayName
			});
		}
	}

	static ComponentLevel()
	{
		fName = "ComponentLevel";
	}

	private float ScaledLevelForStats(float level)
	{
		if (!IsEmbrace)
		{
			return MathUtils.Clamp(level, 1f, 21f);
		}
		return MathUtils.Max(level, 1f);
	}

	public void RemoveLevels(int levelsToRemove)
	{
		if (levelsToRemove > 0)
		{
			m_componentPlayer.PlayerData.Level = MathUtils.Max(1f, m_componentPlayer.PlayerData.Level - (float)levelsToRemove);
			if (m_lastLevelTextValue.HasValue)
			{
				m_lastLevelTextValue = null;
			}
			m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.6f, m_componentPlayer.ComponentBody.Position, 2f, autoDelay: false);
		}
	}
}
