using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Audio;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentVitalStats : Component, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

	public SubsystemWeather m_subsystemWeather;

	public ComponentPlayer m_componentPlayer;

	public Random m_random;

	public Sound m_pantingSound;

	public float m_food;

	public float m_stamina;

	public float m_sleep;

	public float m_temperature;

	public float m_wetness;

	public float m_lastFood;

	public float m_lastStamina;

	public float m_lastSleep;

	public float m_lastTemperature;

	public float m_lastWetness;

	public Dictionary<int, float> m_satiation;

	public List<KeyValuePair<int, float>> m_satiationList;

	public float m_densityModifierApplied;

	public double? m_lastAttackedTime;

	public float m_sleepBlackoutFactor;

	public float m_sleepBlackoutDuration;

	public float m_environmentTemperature;

	public float m_environmentTemperatureFlux;

	public float m_temperatureBlackoutFactor;

	public float m_temperatureBlackoutDuration;

	public static string fName = "ComponentVitalStats";

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemParticles m_subsystemParticles;

	public BreathSteamParticleSystem m_breathSteamParticleSystem;

	public Sound m_heartbeatSound;

	public ComponentMiner m_componentMiner;

	private float m_umbrellaDamageTimer;

	private int m_durabilityWormStreak;

	private float m_heatHazeSmoothed;

	private double m_lastHeatTime;

	private const float m_heatAttackTime = 1.5f;

	private const float m_heatReleaseTime = 2.5f;

	public ComponentAlcohol m_componentAlcohol;

	public SubsystemPickables m_subsystemPickables;

	private WaterDripsParticleSystem m_waterDripsParticleSystem;

	private float m_steamTimer;

	public SubsystemFocalorsWaterBlockBehavior m_subsystemWater;

	public double m_lastHotWaterMessageTime = -100.0;

	private bool m_wasStiffFrozen;

	private float m_netLastFood;

	private float m_netLastStamina;

	private float m_netLastSleep;

	private float m_netLastTemperature;

	private float m_netLastWetness;

	public float Food
	{
		get
		{
			return m_food;
		}
		set
		{
			m_food = MathUtils.Saturate(value);
		}
	}

	public float Stamina
	{
		get
		{
			return m_stamina;
		}
		set
		{
			m_stamina = MathUtils.Saturate(value);
		}
	}

	public float Sleep
	{
		get
		{
			return m_sleep;
		}
		set
		{
			m_sleep = MathUtils.Saturate(value);
		}
	}

	public float Temperature
	{
		get
		{
			return m_temperature;
		}
		set
		{
			m_temperature = MathUtils.Clamp(value, 0f, 24f);
		}
	}

	public float Wetness
	{
		get
		{
			return m_wetness;
		}
		set
		{
			m_wetness = MathUtils.Saturate(value);
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public bool IsUnderSnowfall { get; private set; }

	public float HeatHazeFactor
	{
		get
		{
			if (!SettingsManager.HeatAndUnderwaterBlurEnabled)
			{
				m_heatHazeSmoothed = 0f;
				m_lastHeatTime = 0.0;
				return 0f;
			}
			float num = MathUtils.Saturate((GetRawEnvironmentalTemperature() - 18f) / 12f);
			double num2 = m_componentPlayer.Project.FindSubsystem<SubsystemTime>(throwOnError: true)?.GameTime ?? 0.0;
			if (m_lastHeatTime == 0.0)
			{
				m_lastHeatTime = num2;
				m_heatHazeSmoothed = num;
			}
			else
			{
				float num3 = (float)(num2 - m_lastHeatTime);
				m_lastHeatTime = num2;
				float val = ((num > m_heatHazeSmoothed) ? 1.5f : 2.5f);
				float x = 1f - (float)Math.Exp((0.0 - (double)num3) / (double)Math.Max(0.0001f, val));
				m_heatHazeSmoothed += (num - m_heatHazeSmoothed) * MathUtils.Saturate(x);
			}
			bool flag = false;
			SubsystemSky subsystemSky = m_componentPlayer.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			if (subsystemSky != null && subsystemSky.ViewUnderWaterDepth > 0f)
			{
				flag = true;
			}
			else
			{
				ComponentBody componentBody = m_componentPlayer.ComponentBody;
				ComponentCreatureModel componentCreatureModel = m_componentPlayer.ComponentCreatureModel;
				if (componentBody != null && componentCreatureModel != null)
				{
					float num4 = componentCreatureModel.EyePosition.Y - componentBody.Position.Y;
					flag = componentBody.ImmersionDepth > num4;
				}
			}
			float x2 = 0.7f * m_heatHazeSmoothed;
			float x3 = (flag ? 1.5f : 0f);
			return MathUtils.Max(x2, x3);
		}
	}

	public bool Eat(IInventory inventory, int slotIndex, int value)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		if ((num2 & 0x10000) != 0)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "food_frozen"), Color.White, blinking: true, playNotificationSound: true);
			return false;
		}
		if (num != 138 && num != 110 && num != 322 && num != 226 && (num != 442 || !CactusChunkBlock.HasSpikes(num2)) && Food >= 0.98f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 2), Color.White, blinking: true, playNotificationSound: true);
			return false;
		}
		if (CommonLib.WorkType == WorkType.Local)
		{
			PlayEatSoundsAndNotifications(value);
			return NetEat(inventory, slotIndex, value);
		}
		PlayEatSoundsAndNotifications(value);
		if (CommonLib.WorkType == WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new ComponentVitalStatPackage(this, inventory, slotIndex, value));
			return true;
		}
		return NetEat(inventory, slotIndex, value);
	}

	public bool NetEat(IInventory inventory, int slotIndex, int value)
	{
		int num = Terrain.ExtractContents(value);
		int num2 = Terrain.ExtractData(value);
		if ((num2 & 0x10000) != 0)
		{
			return false;
		}
		if (num != 138 && num != 110 && num != 322 && num != 226 && (num != 442 || !CactusChunkBlock.HasSpikes(num2)) && Food >= 0.98f)
		{
			return false;
		}
		Block obj = BlocksManager.Blocks[num];
		float num3 = obj.GetNutritionalValue(value);
		float sicknessProbability = obj.GetSicknessProbability(value);
		if (num == 138)
		{
			return true;
		}
		if (num == 110)
		{
			m_componentPlayer.ComponentHealth.Heal(0.15f);
			if (m_random.Float(0f, 1f) < 0.01f)
			{
				m_componentPlayer.ComponentSickness.StartSickness();
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "sick_after_milk"), Color.White, blinking: true, playNotificationSound: true);
			}
			return true;
		}
		if (num == 322)
		{
			if (m_random.Float(0f, 1f) < 0.5f)
			{
				if (m_componentPlayer.ComponentSickness.IsSick)
				{
					m_componentPlayer.ComponentSickness.Cure();
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "charcoal_cured"), Color.White, blinking: true, playNotificationSound: true);
				}
				else
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "charcoal_no_effect"), Color.White, blinking: true, playNotificationSound: true);
				}
			}
			else
			{
				m_componentPlayer.ComponentSickness.StartSickness();
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "charcoal_worse"), Color.White, blinking: true, playNotificationSound: true);
			}
			return true;
		}
		if (num == 226 || (num == 442 && CactusChunkBlock.HasSpikes(num2)))
		{
			string cause = ((num == 442) ? "Spikes" : "Sea Urchin");
			m_componentPlayer.ComponentHealth.Injure(0.2f, null, ignoreInvulnerability: true, cause);
			if (m_componentPlayer.PlayerStats != null && num == 226)
			{
				m_componentPlayer.PlayerStats.WaterCreatureKills++;
			}
		}
		if (m_componentPlayer.ComponentSickness.IsSick)
		{
			num3 *= 0.75f;
		}
		Food += num3;
		m_satiation.TryGetValue(num, out var value2);
		value2 += MathUtils.Max(num3, 0.5f);
		m_satiation[num] = value2;
		bool flag = m_random.Bool(sicknessProbability) || value2 > 3.5f;
		if (!flag && sicknessProbability >= 0.55f)
		{
			ComponentCreature componentCreature = Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null && componentCreature.PlayerAchievements != null && componentCreature.PlayerAchievements.Scavenger == 0L && CheckIfHasNormalFood())
			{
				componentCreature.PlayerAchievements.Scavenger = 1L;
				componentCreature.PlayerAchievements.EnqueueScavengerAchievement();
			}
		}
		if (flag)
		{
			m_componentPlayer.ComponentSickness.StartSickness();
		}
		switch (num)
		{
		case 318:
			if (m_componentPlayer.PlayerStats != null)
			{
				m_componentPlayer.PlayerStats.LandCreatureKills++;
			}
			if (!flag && !m_componentPlayer.ComponentSickness.IsSick)
			{
				m_durabilityWormStreak++;
				if (m_durabilityWormStreak >= 7)
				{
					ComponentCreature componentCreature2 = Entity.FindComponent<ComponentCreature>();
					if (componentCreature2 != null && componentCreature2.PlayerAchievements != null && componentCreature2.PlayerAchievements.DurabilityCheck == 0L)
					{
						componentCreature2.PlayerAchievements.DurabilityCheck = 1L;
						componentCreature2.PlayerAchievements.EnqueueDurabilityCheckAchievement();
					}
					m_durabilityWormStreak = 0;
				}
			}
			else
			{
				m_durabilityWormStreak = 0;
			}
			break;
		case 326:
			m_durabilityWormStreak = 0;
			if (m_componentPlayer.PlayerStats != null)
			{
				m_componentPlayer.PlayerStats.WaterCreatureKills++;
			}
			break;
		default:
			m_durabilityWormStreak = 0;
			break;
		}
		m_componentPlayer.PlayerStats.FoodItemsEaten++;
		return true;
	}

	public void MakeSleepy(float sleepValue)
	{
		Sleep = MathUtils.Min(Sleep, sleepValue);
	}

	public void Update(float dt)
	{
		if (m_componentPlayer.ComponentBody.ImmersionFactor > 0.3f)
		{
			Vector3 position = m_componentPlayer.ComponentBody.Position;
			int num = Terrain.ToCell(position.X);
			int num2 = Terrain.ToCell(position.Y + 0.1f);
			int num3 = Terrain.ToCell(position.Z);
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(num, num2, num3)) == 18 && m_subsystemWater.IsWaterHot(num, num2, num3))
			{
				m_componentPlayer.ComponentHealth.Heal(0.005f * dt);
				if (m_subsystemTime.GameTime - m_lastHotWaterMessageTime > 120.0)
				{
					m_lastHotWaterMessageTime = m_subsystemTime.GameTime;
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "hot_water_regen"), Color.LightGreen, blinking: true, playNotificationSound: true);
				}
			}
		}
		if (m_componentPlayer.ComponentHealth.Health > 0f)
		{
			UpdateFood();
			UpdateStamina();
			UpdateSleep();
			UpdateTemperature();
			UpdateWetness();
			double x = (double)(GetHashCode() % 1000) / 1000.0;
			if (!Time.PeriodicEvent(1.0, x))
			{
				return;
			}
			bool white = false;
			if (CommonLib.WorkType == WorkType.Client)
			{
				if (Math.Abs(Stamina - m_netLastStamina) > 0.02f || (Stamina == 0f && m_netLastStamina != 0f) || (Stamina == 1f && m_netLastStamina != 1f))
				{
					white = true;
				}
			}
			else if (Math.Abs(Food - m_netLastFood) > 0.01f || Math.Abs(Sleep - m_netLastSleep) > 0.01f || Math.Abs(Wetness - m_netLastWetness) > 0.05f || Math.Abs(Temperature - m_netLastTemperature) > 0.5f)
			{
				white = true;
			}
			else if ((Food == 0f && m_netLastFood != 0f) || (Food == 1f && m_netLastFood != 1f) || (Sleep == 0f && m_netLastSleep != 0f) || (Sleep == 1f && m_netLastSleep != 1f) || (Wetness == 0f && m_netLastWetness != 0f) || (Wetness == 1f && m_netLastWetness != 1f) || (Temperature == 0f && m_netLastTemperature != 0f) || (Temperature == 24f && m_netLastTemperature != 24f))
			{
				white = true;
			}
			if (white)
			{
				CommonLib.Net.QueuePackage(new ComponentVitalStatPackage(this));
				m_netLastFood = Food;
				m_netLastStamina = Stamina;
				m_netLastSleep = Sleep;
				m_netLastTemperature = Temperature;
				m_netLastWetness = Wetness;
			}
		}
		else
		{
			m_pantingSound.Stop();
			m_heartbeatSound.Stop();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemWater = Project.FindSubsystem<SubsystemFocalorsWaterBlockBehavior>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>(throwOnError: true);
		m_componentMiner = Entity.FindComponent<ComponentMiner>(throwOnError: true);
		m_waterDripsParticleSystem = new WaterDripsParticleSystem(m_subsystemTerrain, m_subsystemWeather, m_componentPlayer.ComponentBody.Position, isBlockSource: false);
		m_waterDripsParticleSystem.UseHangingPhase = false;
		m_subsystemParticles.AddParticleSystem(m_waterDripsParticleSystem);
		m_pantingSound = m_subsystemAudio.CreateSound("Audio/HumanPanting");
		m_pantingSound.IsLooped = true;
		m_heartbeatSound = m_subsystemAudio.CreateSound("Audio/HumanHeartbeat");
		m_heartbeatSound.IsLooped = true;
		Food = valuesDictionary.GetValue<float>("Food");
		Stamina = valuesDictionary.GetValue<float>("Stamina");
		Sleep = valuesDictionary.GetValue<float>("Sleep");
		Temperature = valuesDictionary.GetValue<float>("Temperature");
		Wetness = valuesDictionary.GetValue<float>("Wetness");
		m_wasStiffFrozen = valuesDictionary.GetValue("WasStiffFrozen", defaultValue: false);
		m_lastFood = Food;
		m_lastStamina = Stamina;
		m_lastSleep = Sleep;
		m_lastTemperature = Temperature;
		m_lastWetness = Wetness;
		m_environmentTemperature = Temperature;
		foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Satiation"))
		{
			m_satiation[int.Parse(item.Key, CultureInfo.InvariantCulture)] = (float)item.Value;
		}
		m_componentPlayer.ComponentHealth.Attacked += (ComponentCreature attacker) =>
		{
			m_lastAttackedTime = m_subsystemTime.GameTime;
		};
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Food", Food);
		valuesDictionary.SetValue("Stamina", Stamina);
		valuesDictionary.SetValue("Sleep", Sleep);
		valuesDictionary.SetValue("Temperature", Temperature);
		valuesDictionary.SetValue("Wetness", Wetness);
		valuesDictionary.SetValue("WasStiffFrozen", m_wasStiffFrozen);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Satiation", valuesDictionary2);
		foreach (KeyValuePair<int, float> item in m_satiation)
		{
			if (item.Value > 0f)
			{
				valuesDictionary2.SetValue(item.Key.ToString(CultureInfo.InvariantCulture), item.Value);
			}
		}
	}

	public override void OnEntityRemoved()
	{
		m_pantingSound.Stop();
		m_heartbeatSound.Stop();
		if (m_waterDripsParticleSystem != null)
		{
			m_subsystemParticles.RemoveParticleSystem(m_waterDripsParticleSystem);
			m_waterDripsParticleSystem = null;
		}
		m_wasStiffFrozen = false;
	}

	public void UpdateFood()
	{
		float model = m_subsystemTime.GameTimeDelta;
		float boneAbsoluteTransform = (m_componentPlayer.ComponentLocomotion.LastWalkOrder.HasValue ? m_componentPlayer.ComponentLocomotion.LastWalkOrder.Value.Length() : 0f);
		float boneAbsoluteTransform2 = m_componentPlayer.ComponentLocomotion.LastJumpOrder;
		float num = m_componentPlayer.ComponentCreatureModel.EyePosition.Y - m_componentPlayer.ComponentBody.Position.Y;
		bool flag = m_componentPlayer.ComponentBody.ImmersionDepth > num;
		bool flag2 = m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water;
		bool flag3 = m_componentPlayer.ComponentBody.ImmersionFactor > 0.33f && !m_componentPlayer.ComponentBody.StandingOnValue.HasValue;
		bool flag4 = m_subsystemTime.PeriodicGameTimeEvent(240.0, 13.0) && !m_componentPlayer.ComponentSickness.IsSick;
		float num2;
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			num2 = m_componentPlayer.ComponentLevel.HungerFactor;
			if (CommonLib.WorkType == WorkType.Server && m_componentPlayer.ComponentSleep.m_subsystemUpdate.UpdatesPerFrame == 1 && m_componentPlayer.ComponentSleep.IsSleeping)
			{
				num2 *= m_subsystemGameInfo.WorldSettings.RecoverFator / 3.5f;
			}
			Food -= num2 * model / 4320f;
			bool num3;
			if (!flag2)
			{
				num3 = flag3 | flag;
			}
			else if (flag3)
			{
				num3 = false;
			}
			else
			{
				num3 = !flag;
			}
			if (num3)
			{
				Food -= num2 * model * boneAbsoluteTransform / 2160f;
			}
			else
			{
				Food -= num2 * model * boneAbsoluteTransform / 4320f;
			}
			Food -= num2 * boneAbsoluteTransform2 / 1200f;
			if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue)
			{
				Food -= num2 * model / 2880f;
			}
			if (!m_componentPlayer.ComponentSleep.IsSleeping)
			{
				if (Food <= 0f)
				{
					if (m_subsystemTime.PeriodicGameTimeEvent(50.0, 0.0))
					{
						bool flag5 = CheckIfHasNormalFood();
						m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 9));
						if (((m_componentPlayer.ComponentHealth.Health <= 0f) & flag5) && m_componentPlayer.PlayerAchievements != null && m_componentPlayer.PlayerAchievements.Ascetic == 0L)
						{
							m_componentPlayer.PlayerAchievements.Ascetic = 1L;
							m_componentPlayer.PlayerAchievements.EnqueueAsceticAchievement();
						}
						m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 10), Color.Brown, blinking: true, playNotificationSound: false);
						m_componentPlayer.ComponentGui.FoodBarWidget.Flash(10);
					}
				}
				else if (Food < 0.1f && ((m_lastFood >= 0.1f) | flag4))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 11), Color.Brown, blinking: true, playNotificationSound: true);
				}
				else if (Food < 0.25f && ((m_lastFood >= 0.25f) | flag4))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 12), Color.Brown, blinking: true, playNotificationSound: true);
				}
				else if (Food < 0.5f && ((m_lastFood >= 0.5f) | flag4))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 13), Color.Brown, blinking: true, playNotificationSound: false);
				}
			}
		}
		else
		{
			Food = 0.9f;
		}
		if (m_subsystemTime.PeriodicGameTimeEvent(1.0, -0.01))
		{
			m_satiationList.Clear();
			m_satiationList.AddRange(m_satiation);
			m_satiation.Clear();
			foreach (KeyValuePair<int, float> satiation in m_satiationList)
			{
				float num4 = MathUtils.Max(satiation.Value - 0.00041666668f, 0f);
				if (num4 > 0f)
				{
					m_satiation.Add(satiation.Key, num4);
				}
			}
		}
		m_lastFood = Food;
		m_componentPlayer.ComponentGui.FoodBarWidget.Value = Food;
		return;
	}

	public void UpdateStamina()
	{
		float gameTimeDelta = m_subsystemTime.GameTimeDelta;
		float num = (m_componentPlayer.ComponentLocomotion.LastWalkOrder.HasValue ? m_componentPlayer.ComponentLocomotion.LastWalkOrder.Value.Length() : 0f);
		float lastJumpOrder = m_componentPlayer.ComponentLocomotion.LastJumpOrder;
		float num2 = m_componentPlayer.ComponentCreatureModel.EyePosition.Y - m_componentPlayer.ComponentBody.Position.Y;
		bool flag = m_componentPlayer.ComponentBody.ImmersionDepth > num2;
		bool flag2 = m_componentPlayer.ComponentBody.ImmersionFactor > 0.33f && !m_componentPlayer.ComponentBody.StandingOnValue.HasValue;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless || !m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			Stamina = 1f;
			ApplyDensityModifier(0f);
			return;
		}
		float num3 = 1f / MathUtils.Max(m_componentPlayer.ComponentLevel.SpeedFactor, 0.75f);
		if (m_componentPlayer.ComponentSickness.IsSick)
		{
			num3 *= 5f;
		}
		else if (m_componentPlayer.ComponentFlu.HasFlu)
		{
			num3 *= 2.5f;
		}
		float num4 = 1f;
		bool flag3 = m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water;
		if (flag3 ? (flag | flag2) : (!flag))
		{
			if (m_componentPlayer.ComponentVitalStats.Food > 0.75f)
			{
				Stamina += gameTimeDelta * 0.07f * 1.3f;
			}
			if (Food < 0.1f)
			{
				Stamina += gameTimeDelta * 0.07f * 0.1f;
			}
			else
			{
				Stamina += gameTimeDelta * 0.07f;
			}
		}
		Stamina -= 0.025f * lastJumpOrder * num3 * num4;
		bool num5;
		if (!flag3)
		{
			num5 = flag2 | flag;
		}
		else if (flag2)
		{
			num5 = false;
		}
		else
		{
			num5 = !flag;
		}
		if (num5)
		{
			float num6 = 0f;
			float num7 = 0.01f;
			if (!flag3)
			{
				if (flag)
				{
					num6 = 0.05f;
					num7 = 0.05f;
				}
				else if (flag2)
				{
					num6 = 0f;
					num7 = 0.15f;
				}
			}
			Stamina -= gameTimeDelta * (num6 + num7 * num * num3) * num4;
		}
		else
		{
			Stamina -= gameTimeDelta * (0.07f + 0.006f * num3) * num * num4;
		}
		if (!flag2 && !flag && Stamina < 0.33f && m_lastStamina >= 0.33f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 14), Color.Olive, blinking: true, playNotificationSound: false);
		}
		if ((flag2 | flag) && Stamina < 0.4f && m_lastStamina >= 0.4f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 15), Color.Olive, blinking: true, playNotificationSound: true);
		}
		if (Stamina < 0.15f)
		{
			if (m_lastStamina >= 0.15f)
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "gasping"), Color.Olive, blinking: true, playNotificationSound: true);
			}
			ComponentMiner componentMiner = m_componentPlayer.Entity.FindComponent<ComponentMiner>();
			if (componentMiner != null)
			{
				componentMiner.DigCellFace = null;
				componentMiner.m_digProgress = 0f;
				componentMiner.PokingPhase = 0f;
				componentMiner.AttackPower = 0f;
			}
		}
		else
		{
			ComponentMiner componentMiner2 = m_componentPlayer.Entity.FindComponent<ComponentMiner>();
			if (componentMiner2 != null)
			{
				componentMiner2.AttackPower = 1f;
			}
		}
		m_lastStamina = Stamina;
		float num8 = MathUtils.Saturate(2f * (0.5f - Stamina));
		if (flag3 == flag && num8 > 0f)
		{
			float size = m_componentPlayer.ComponentBody.Size;
			float num9 = MathUtils.Clamp(1f - size, -1.2f, 1.2f);
			float num10 = ((m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.2f : 0f);
			m_pantingSound.Volume = 1f * SettingsManager.SoundsVolume * MathUtils.Saturate(num8) * MathUtils.Lerp(0.8f, 1f, SimplexNoise.Noise((float)MathUtils.Remainder(3.0 * Time.RealTime + 100.0, 1000.0)));
			m_pantingSound.Pitch = AudioManager.ToEnginePitch(num10 + num9 + MathUtils.Lerp(-0.15f, 0.05f, num8) * MathUtils.Lerp(0.8f, 1.2f, SimplexNoise.Noise((float)MathUtils.Remainder(3.0 * Time.RealTime + 200.0, 1000.0))));
			m_pantingSound.Play();
		}
		else
		{
			m_pantingSound.Stop();
		}
		bool num11 = m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer;
		float health = m_componentPlayer.ComponentHealth.Health;
		if (num11 && (health <= 0.3f || Temperature <= 0f))
		{
			float size2 = m_componentPlayer.ComponentBody.Size;
			float num12 = MathUtils.Clamp(1f - size2, -1.2f, 1.2f);
			float num13 = 1f;
			if (health < 0.3f)
			{
				float f = MathUtils.Saturate((0.3f - health) / 0.3f);
				num13 = MathUtils.Lerp(1f, 2f, f);
			}
			if (Temperature <= 0f)
			{
				num13 *= 0.5f;
			}
			float volume = SettingsManager.SoundsVolume * MathUtils.Saturate(1f - health) * 0.5f;
			float pitch = AudioManager.ToEnginePitch(MathUtils.Lerp(-0.2f, 0.3f, num13 - 1f) + num12);
			m_heartbeatSound.Volume = volume;
			m_heartbeatSound.Pitch = pitch;
			m_heartbeatSound.Play();
		}
		else
		{
			m_heartbeatSound.Stop();
		}
		float num14 = MathUtils.Saturate(3f * (0.33f - Stamina));
		if (num14 > 0f && SimplexNoise.Noise((float)MathUtils.Remainder(Time.RealTime, 1000.0)) < num14)
		{
			ApplyDensityModifier(0.6f);
		}
		else
		{
			ApplyDensityModifier(0f);
		}
		return;
	}

	public void UpdateSleep()
	{
		float gameTimeDelta = m_subsystemTime.GameTimeDelta;
		bool flag = m_componentPlayer.ComponentBody.ImmersionFactor > 0.05f;
		bool flag2 = m_subsystemTime.PeriodicGameTimeEvent(240.0, 9.0);
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			if (m_componentPlayer.ComponentSleep.SleepFactor == 1f)
			{
				Sleep += 0.05f * gameTimeDelta;
			}
			else if (!flag && (!m_lastAttackedTime.HasValue || m_subsystemTime.GameTime - m_lastAttackedTime > 10.0))
			{
				float num = gameTimeDelta / 2700f;
				if (m_componentAlcohol == null)
				{
					m_componentAlcohol = Entity.FindComponent<ComponentAlcohol>(throwOnError: false);
				}
				if (m_componentAlcohol != null)
				{
					float sleepinessMultiplier = m_componentAlcohol.GetSleepinessMultiplier();
					num *= sleepinessMultiplier;
				}
				Sleep -= num;
				if (Sleep < 0.075f && ((m_lastSleep >= 0.075f) | flag2))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 19), Color.DarkMagenta, blinking: true, playNotificationSound: true);
					m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
				}
				else if (Sleep < 0.2f && ((m_lastSleep >= 0.2f) | flag2))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 20), Color.DarkMagenta, blinking: true, playNotificationSound: true);
					m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
				}
				else if (Sleep < 0.33f && ((m_lastSleep >= 0.33f) | flag2))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 21), Color.DarkMagenta, blinking: true, playNotificationSound: false);
				}
				else if (Sleep < 0.5f && ((m_lastSleep >= 0.5f) | flag2))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 22), Color.DarkMagenta, blinking: true, playNotificationSound: false);
				}
				if (Sleep < 0.075f)
				{
					float num2 = MathUtils.Lerp(0.05f, 0.2f, (0.075f - Sleep) / 0.075f);
					float x = ((Sleep < 0.0375f) ? m_random.Float(3f, 6f) : m_random.Float(2f, 4f));
					if (m_random.Float(0f, 1f) < num2 * gameTimeDelta)
					{
						m_sleepBlackoutDuration = MathUtils.Max(m_sleepBlackoutDuration, x);
						m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
					}
				}
				if (Sleep <= 0f && !m_componentPlayer.ComponentSleep.IsSleeping)
				{
					m_componentPlayer.ComponentSleep.Sleep(allowManualWakeup: false);
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 23), Color.DarkMagenta, blinking: true, playNotificationSound: true);
					m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
				}
			}
		}
		else
		{
			Sleep = 0.9f;
		}
		m_lastSleep = Sleep;
		m_sleepBlackoutDuration -= gameTimeDelta;
		float num3 = MathUtils.Saturate(0.5f * m_sleepBlackoutDuration);
		m_sleepBlackoutFactor = MathUtils.Saturate(m_sleepBlackoutFactor + 2f * gameTimeDelta * (num3 - m_sleepBlackoutFactor));
		if (!m_componentPlayer.ComponentSleep.IsSleeping)
		{
			m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_sleepBlackoutFactor, m_componentPlayer.ComponentScreenOverlays.BlackoutFactor);
			if ((double)m_sleepBlackoutFactor > 0.01)
			{
				m_componentPlayer.ComponentScreenOverlays.FloatingMessage = LanguageControl.Get(fName, 24);
				m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_sleepBlackoutFactor - 0.9f));
			}
		}
	}

	public void UpdateTemperature()
	{
		float gameTimeDelta = m_subsystemTime.GameTimeDelta;
		bool flag = m_subsystemTime.PeriodicGameTimeEvent(300.0, 17.0);
		float insulation = m_componentPlayer.ComponentClothing.Insulation;
		string arg = m_componentPlayer.ComponentClothing.LeastInsulatedSlot switch
		{
			ClothingSlot.Head => LanguageControl.Get(fName, 41), 
			ClothingSlot.Torso => LanguageControl.Get(fName, 42), 
			ClothingSlot.Legs => LanguageControl.Get(fName, 43), 
			_ => LanguageControl.Get(fName, 44), 
		};
		if (m_subsystemTime.PeriodicGameTimeEvent(2.0, 2.0 * (double)GetHashCode() % 1000.0 / 1000.0))
		{
			Vector3 position = m_componentPlayer.ComponentBody.Position;
			int x = Terrain.ToCell(position.X);
			int num = Terrain.ToCell(position.Y + 0.1f);
			int z = Terrain.ToCell(position.Z);
			int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num, z));
			Block block = BlocksManager.Blocks[num2];
			if (num2 != 0 && !block.IsCollidable)
			{
				int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num + 1, z));
				if (!BlocksManager.Blocks[num3].IsCollidable)
				{
					num++;
				}
			}
			m_subsystemMetersBlockBehavior.CalculateTemperature(x, num, z, 12f, insulation, out m_environmentTemperature, out m_environmentTemperatureFlux);
			if (m_componentPlayer.ComponentBody.ImmersionFactor <= 0.1f)
			{
				m_environmentTemperature -= 12f * Wetness;
			}
		}
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			float num4 = ((insulation >= 4f) ? ((insulation - 3f) * 3f) : 0f);
			float num5 = m_environmentTemperature + num4;
			float num6 = 0.01f + 0.005f * m_environmentTemperatureFlux;
			Temperature += MathUtils.Saturate(num6 * gameTimeDelta) * (num5 - Temperature);
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
			{
				Temperature = MathUtils.Max(Temperature, 2f);
			}
		}
		else
		{
			Temperature = 12f;
		}
		bool flag2 = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
		bool flag3 = m_subsystemGameInfo.WorldSettings.BreathingMode == BreathingMode.Water;
		bool flag4 = (flag3 ? (m_componentPlayer.ComponentBody.ImmersionFactor > 0.2f) : (m_componentPlayer.ComponentBody.ImmersionFactor <= 0.2f));
		if (((m_environmentTemperature < 4f && Temperature > 6f) & flag4) && !flag2)
		{
			Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
			Vector3 forwardVector = m_componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
			if (m_breathSteamParticleSystem != null && !m_breathSteamParticleSystem.IsStopped)
			{
				m_breathSteamParticleSystem.Position = eyePosition + 0.1f * forwardVector;
				m_breathSteamParticleSystem.Direction = forwardVector;
				m_breathSteamParticleSystem.AlphaMultiplier = (flag3 ? 0.15f : 1f);
			}
			else if (m_subsystemTime.PeriodicGameTimeEvent(2.7, (double)GetHashCode() % 1000.0 / 1000.0))
			{
				m_breathSteamParticleSystem = new BreathSteamParticleSystem(m_subsystemTerrain, m_subsystemWeather);
				m_breathSteamParticleSystem.Position = eyePosition + 0.1f * forwardVector;
				m_breathSteamParticleSystem.Direction = forwardVector;
				m_breathSteamParticleSystem.AlphaMultiplier = (flag3 ? 0.15f : 1f);
				m_subsystemParticles.AddParticleSystem(m_breathSteamParticleSystem);
			}
		}
		else if (m_breathSteamParticleSystem != null)
		{
			m_breathSteamParticleSystem.IsStopped = true;
			m_breathSteamParticleSystem = null;
		}
		if (Temperature <= 0f)
		{
			m_wasStiffFrozen = true;
			float temperatureBlackoutFactor = MathUtils.Saturate((0.55f - m_componentPlayer.ComponentHealth.Health) * 1.8181818f);
			m_temperatureBlackoutFactor = temperatureBlackoutFactor;
			m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_temperatureBlackoutFactor, m_componentPlayer.ComponentScreenOverlays.BlackoutFactor);
			if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
			{
				m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 26));
			}
			if (m_componentPlayer.ComponentHealth.Health <= 0.55f)
			{
				m_componentPlayer.ComponentHealth.Injure(1f, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 26));
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 23), Color.DarkMagenta, blinking: true, playNotificationSound: true);
				m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
			}
		}
		if (m_wasStiffFrozen && Temperature >= 8f)
		{
			if (m_componentPlayer.PlayerAchievements != null && m_componentPlayer.PlayerAchievements.FrozenHeart == 0L)
			{
				m_componentPlayer.PlayerAchievements.FrozenHeart = 1L;
				m_componentPlayer.PlayerAchievements.EnqueueFrozenHeartAchievement();
			}
			m_wasStiffFrozen = false;
		}
		if (Temperature < 6f && ((m_lastTemperature >= 6f) | flag))
		{
			string text;
			if (Wetness > 0f)
			{
				text = string.Format(LanguageControl.Get(fName, 30), arg);
			}
			else
			{
				text = ((insulation >= 1f) ? string.Format(LanguageControl.Get(fName, 31), arg) : string.Format(LanguageControl.Get(fName, 32), arg));
			}
			m_componentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Cyan, blinking: true, playNotificationSound: true);
			m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
		}
		else if (Temperature < 8f && ((m_lastTemperature >= 8f) | flag))
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 33), Color.Cyan, blinking: true, playNotificationSound: false);
			m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
		}
		if (Temperature >= 24f)
		{
			if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 34), Color.Orange, blinking: true, playNotificationSound: false);
				if (insulation > 5.5f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 50), Color.Orange, blinking: true, playNotificationSound: false);
				}
				m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 35));
				m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
			}
			if (m_subsystemTime.PeriodicGameTimeEvent(8.0, 0.0))
			{
				m_temperatureBlackoutDuration = MathUtils.Max(m_temperatureBlackoutDuration, 6f);
				m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
			}
		}
		else if (Temperature > 20f && m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 36), Color.Orange, blinking: true, playNotificationSound: false);
			if (insulation > 4.5f)
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 50), Color.Orange, blinking: true, playNotificationSound: false);
			}
			m_temperatureBlackoutDuration = MathUtils.Max(m_temperatureBlackoutDuration, 3f);
			m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
			m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
		}
		m_lastTemperature = Temperature;
		float num7 = ((insulation >= 4f) ? ((insulation - 3f) * 3f) : 0f);
		float num8 = 8f;
		float num9 = ((m_environmentTemperature < num8) ? (m_environmentTemperature / num8) : 1f);
		float num10 = m_environmentTemperature + num7 * num9;
		if (num10 > 22f)
		{
			m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature6");
		}
		else if (num10 > 18f)
		{
			m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature5");
		}
		else if (num10 > 14f)
		{
			m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature4");
		}
		else if (num10 > 10f)
		{
			m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature3");
		}
		else if (num10 > 6f)
		{
			m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature2");
		}
		else
		{
			m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ((num10 > 2f) ? ContentManager.Get<Subtexture>("Textures/Atlas/Temperature1") : ContentManager.Get<Subtexture>("Textures/Atlas/Temperature0"));
		}
		m_temperatureBlackoutDuration -= gameTimeDelta;
		float num11 = MathUtils.Saturate(0.5f * m_temperatureBlackoutDuration);
		m_temperatureBlackoutFactor = MathUtils.Saturate(m_temperatureBlackoutFactor + 2f * gameTimeDelta * (num11 - m_temperatureBlackoutFactor));
		m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_temperatureBlackoutFactor, m_componentPlayer.ComponentScreenOverlays.BlackoutFactor);
		if (m_temperatureBlackoutFactor > 0.01f)
		{
			m_componentPlayer.ComponentScreenOverlays.FloatingMessage = LanguageControl.Get(fName, 37);
			m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_temperatureBlackoutFactor - 0.9f));
		}
		float num12 = 1f - Temperature / 6f;
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
		{
			num12 = MathUtils.Min(num12, 0.9f);
		}
		m_componentPlayer.ComponentScreenOverlays.IceFactor = MathUtils.Saturate(num12);
	}

	public void UpdateWetness()
	{
		float gameTimeDelta = m_subsystemTime.GameTimeDelta;
		float lightningIntensity = m_subsystemWeather.m_lightningIntensity;
		float num = 7.42f;
		float num2 = 0.75f;
		float x = 0.3f;
		float insulation = m_componentPlayer.ComponentClothing.Insulation;
		float num3 = MathUtils.Lerp(1f, x, MathUtils.Saturate((insulation - num2) / (num - num2)));
		float num4;
		if (lightningIntensity == 0f)
		{
			num4 = 0.15f;
		}
		else
		{
			num4 = ((!(lightningIntensity > 0.1f) || !(lightningIntensity < 0.5f)) ? 1.5f : 1f);
		}
		if (m_componentPlayer.ComponentBody.ImmersionFactor > 0.2f && m_componentPlayer.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsWaterBlock)
		{
			float num5 = 2f * m_componentPlayer.ComponentBody.ImmersionFactor;
			Wetness += MathUtils.Saturate(3f * gameTimeDelta) * (num5 - Wetness);
		}
		int x2 = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.X);
		int z = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Z);
		int num6 = Terrain.ToCell(m_componentPlayer.ComponentBody.BoundingBox.Max.Y);
		PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x2, z);
		bool flag = false;
		if (m_componentMiner != null && Terrain.ExtractContents(m_componentMiner.ActiveBlockValue) == 379 && (Terrain.ExtractData(m_componentMiner.ActiveBlockValue) & 1) == 1)
		{
			flag = true;
		}
		bool flag2 = false;
		if (m_subsystemPickables != null && m_componentPlayer.ComponentCreatureModel != null)
		{
			Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
			Vector2 v = new Vector2(eyePosition.X, eyePosition.Z);
			ReadOnlyList<Pickable> pickables = m_subsystemPickables.Pickables;
			for (int i = 0; i < pickables.Count; i++)
			{
				Pickable pickable = pickables[i];
				if (Terrain.ExtractContents(pickable.Value) == 379 && (Terrain.ExtractData(pickable.Value) & 1) != 0 && Vector2.DistanceSquared(new Vector2(pickable.Position.X, pickable.Position.Z), v) <= 2f && eyePosition.Y <= pickable.Position.Y + 2.7f)
				{
					flag2 = true;
					break;
				}
			}
		}
		if (!flag && !flag2 && num6 >= precipitationShaftInfo.YLimit && precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
		{
			Wetness += 0.05f * precipitationShaftInfo.Intensity * gameTimeDelta * num3 * num4;
		}
		if (flag && num6 >= precipitationShaftInfo.YLimit && precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
		{
			float umbrellaDamageTimer = Math.Max((10f - 9f * precipitationShaftInfo.Intensity) / num4, 1f);
			m_umbrellaDamageTimer -= gameTimeDelta;
			if (m_umbrellaDamageTimer <= 0f)
			{
				m_componentMiner.DamageActiveTool(1);
				m_umbrellaDamageTimer = umbrellaDamageTimer;
			}
		}
		else
		{
			m_umbrellaDamageTimer = 0f;
		}
		if (m_subsystemWeather.CurrentWindStrength >= 10f)
		{
			Vector3 position = m_componentPlayer.ComponentBody.Position;
			float windExposureFactor = m_subsystemWeather.GetWindExposureFactor(position);
			if (windExposureFactor > 0f && precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
			{
				Wetness += 0.03f * precipitationShaftInfo.Intensity * gameTimeDelta * windExposureFactor * num3 * num4;
			}
		}
		float num7 = 180f;
		if (m_environmentTemperature > 8f)
		{
			num7 = 120f;
		}
		if (m_environmentTemperature > 16f)
		{
			num7 = 60f;
		}
		if (m_environmentTemperature > 24f)
		{
			num7 = 30f;
		}
		if (flag)
		{
			IsUnderSnowfall = false;
		}
		else
		{
			IsUnderSnowfall = (num6 >= precipitationShaftInfo.YLimit && precipitationShaftInfo.Type == PrecipitationType.Snow && precipitationShaftInfo.Intensity > 0f) || (m_subsystemWeather.CurrentWindStrength >= 10f && m_subsystemWeather.GetWindExposureFactor(m_componentPlayer.ComponentBody.Position) > 0f && precipitationShaftInfo.Type == PrecipitationType.Snow && precipitationShaftInfo.Intensity > 0f);
		}
		Wetness -= gameTimeDelta / num7;
		if (m_waterDripsParticleSystem != null)
		{
			m_waterDripsParticleSystem.Position = m_componentPlayer.ComponentBody.Position;
			float num8 = m_componentPlayer.ComponentBody.BoundingBox.Max.Y - m_componentPlayer.ComponentBody.BoundingBox.Min.Y;
			m_waterDripsParticleSystem.EmitterHeight = num8;
			if (Wetness > 0.2f && m_componentPlayer.ComponentBody.ImmersionFactor <= 0.5f)
			{
				float num9 = (Wetness - 0.2f) / 0.8f;
				m_waterDripsParticleSystem.WetnessLevel = Wetness * (1f + num9);
			}
			else
			{
				m_waterDripsParticleSystem.WetnessLevel = 0f;
			}
			if (Wetness > 0.01f && m_componentPlayer.ComponentBody.ImmersionFactor == 0f && !IsUnderSnowfall && m_environmentTemperature > 10f)
			{
				float num10 = 1f / MathUtils.Max(num7, 1f) * Wetness * 1200f * gameTimeDelta;
				m_steamTimer += num10;
				if (m_steamTimer >= 1f)
				{
					m_steamTimer = 0f;
					Vector3 position2 = m_componentPlayer.ComponentBody.Position;
					Vector3 vector = new Vector3(y: m_random.Float(num8 * 0.2f, num8 * 0.8f), x: m_random.Float(-0.25f, 0.25f), z: m_random.Float(-0.25f, 0.25f));
					Vector3 position3 = position2 + vector;
					Vector3 vector2 = new Vector3(m_subsystemWeather.CurrentWindVector.X, 0f, m_subsystemWeather.CurrentWindVector.Y);
					Vector3 direction = new Vector3(0f, 0.2f, 0f) + vector2 * 0.05f;
					BreathSteamParticleSystem particleSystem = new BreathSteamParticleSystem(m_subsystemTerrain, m_subsystemWeather)
					{
						Position = position3,
						Direction = direction,
						AlphaMultiplier = 0.15f
					};
					m_subsystemParticles.AddParticleSystem(particleSystem);
					m_waterDripsParticleSystem.WetnessLevel *= 0.5f;
				}
			}
			else
			{
				m_steamTimer = 0f;
			}
		}
		if (Wetness > 0.8f && m_lastWetness <= 0.8f)
		{
			Time.QueueTimeDelayedExecution(Time.FrameStartTime + 2.0, () =>
			{
				if (Wetness > 0.8f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 38), Color.LightBlue, blinking: true, playNotificationSound: true);
				}
			});
		}
		else if (Wetness > 0.2f && m_lastWetness <= 0.2f)
		{
			Time.QueueTimeDelayedExecution(Time.FrameStartTime + 2.0, () =>
			{
				if (Wetness > 0.2f && Wetness <= 0.8f && Wetness > m_lastWetness)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 39), Color.LightBlue, blinking: true, playNotificationSound: true);
				}
			});
		}
		else if (Wetness <= 0f && m_lastWetness > 0f)
		{
			Time.QueueTimeDelayedExecution(Time.FrameStartTime + 2.0, () =>
			{
				if (Wetness <= 0f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 40), Color.LightBlue, blinking: true, playNotificationSound: true);
				}
			});
		}
		m_lastWetness = Wetness;
	}

	public void ApplyDensityModifier(float modifier)
	{
		float num = modifier - m_densityModifierApplied;
		if (num != 0f)
		{
			m_densityModifierApplied = modifier;
			m_componentPlayer.ComponentBody.Density += num;
		}
	}

	public ComponentVitalStats()
	{
		m_random = new Random();
		m_satiation = new Dictionary<int, float>();
		m_satiationList = new List<KeyValuePair<int, float>>();
	}

	private void PlayEatSoundsAndNotifications(int value)
	{
		float size = m_componentPlayer.ComponentBody.Size;
		float num = MathUtils.Clamp(1f - size, -1.2f, 1.2f);
		int num2 = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		Block block = BlocksManager.Blocks[num2];
		if (block is AmazingFoodBlock)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Creatures/HumanAmazingEat", 1f, m_random.Float(-0.2f, 0.2f) + num, m_componentPlayer.ComponentBody.Position, 4f, 0f);
		}
		else if (block is NewFoodBlock)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Creatures/HumanCrunchEat", 1f, m_random.Float(-0.4f, 0.1f) + num, m_componentPlayer.ComponentBody.Position, 4f, 0f);
		}
		else if (block is MilkBucketBlock)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Creatures/HumanDrink", 1f, m_random.Float(-0.2f, 0.2f) + num, m_componentPlayer.ComponentBody.Position, 4f, 0f);
		}
		else
		{
			m_subsystemAudio.PlayRandomSound("Audio/Creatures/HumanEat", 1f, m_random.Float(-0.2f, 0.2f) + num, m_componentPlayer.ComponentBody.Position, 4f, 0f);
		}
		switch (num2)
		{
		case 138:
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "battery_eat"), Color.White, blinking: true, playNotificationSound: true);
			return;
		case 110:
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "tasty_milk"), Color.White, blinking: true, playNotificationSound: true);
			return;
		case 226:
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "stung_by_sea_urchin"), Color.White, blinking: true, playNotificationSound: true);
			return;
		case 442:
			if (CactusChunkBlock.HasSpikes(data))
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "stung_by_cactus"), Color.White, blinking: true, playNotificationSound: true);
				return;
			}
			break;
		}
		float nutritionalValue = block.GetNutritionalValue(value);
		float sicknessProbability = block.GetSicknessProbability(value);
		float value2 = 0f;
		m_satiation.TryGetValue(num2, out value2);
		float num3 = value2 + MathUtils.Max(nutritionalValue, 0.5f);
		float num4 = nutritionalValue;
		if (m_componentPlayer.ComponentSickness.IsSick)
		{
			num4 *= 0.75f;
		}
		float num5 = MathUtils.Saturate(Food + num4);
		if (m_componentPlayer.ComponentSickness.IsSick && sicknessProbability > 0f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 1), Color.White, blinking: true, playNotificationSound: true);
		}
		else if (m_componentPlayer.ComponentSickness.IsSick)
		{
			m_componentPlayer.ComponentSickness.NauseaEffect();
		}
		else if (sicknessProbability >= 0.5f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 3), Color.White, blinking: true, playNotificationSound: true);
		}
		else if (sicknessProbability > 0f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 4), Color.White, blinking: true, playNotificationSound: true);
		}
		else if (num3 > 3.5f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 5), Color.White, blinking: true, playNotificationSound: true);
		}
		else if (num3 > 2f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 6), Color.White, blinking: true, playNotificationSound: true);
		}
		else if (block is AmazingFoodBlock && num5 > 0.85f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 800), Color.White, blinking: true, playNotificationSound: true);
		}
		else if (num5 > 0.85f)
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 7), Color.White, blinking: true, playNotificationSound: true);
		}
		else
		{
			m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", 8), Color.White, blinking: true, playNotificationSound: false);
		}
	}

	public float GetRawEnvironmentalTemperature()
	{
		if (!SettingsManager.HeatAndUnderwaterBlurEnabled)
		{
			return 18f;
		}
		Vector3 position = m_componentPlayer.ComponentBody.Position;
		int data = Terrain.ToCell(position.X);
		int flag = Terrain.ToCell(position.Y + 0.1f);
		int color = Terrain.ToCell(position.Z);
		int overrideTopTextureSlot = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(data, flag, color));
		Block cellValueFast = BlocksManager.Blocks[overrideTopTextureSlot];
		if (overrideTopTextureSlot != 0 && !cellValueFast.IsCollidable)
		{
			int cellValueFast4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(data, flag + 1, color));
			if (!BlocksManager.Blocks[cellValueFast4].IsCollidable)
			{
				flag++;
			}
		}
		m_subsystemMetersBlockBehavior.CalculateTemperature(data, flag, color, 12f, 0f, out var cellValueFast5, out var _);
		return cellValueFast5;
	}

	private bool CheckIfHasNormalFood()
	{
		ComponentMiner componentMiner = m_componentMiner;
		if (componentMiner == null || componentMiner.Inventory == null)
		{
			return false;
		}
		SubsystemBags subsystemBags = Project.FindSubsystem<SubsystemBags>(throwOnError: true);
		IInventory inventory = componentMiner.Inventory;
		for (int i = 0; i < inventory.SlotsCount; i++)
		{
			int slotValue = inventory.GetSlotValue(i);
			if (inventory.GetSlotCount(i) <= 0 || slotValue == 0)
			{
				continue;
			}
			if (IsGoodFood(slotValue))
			{
				return true;
			}
			int num = Terrain.ExtractContents(slotValue);
			if (num != 364 && num != 416 && num != 421 && num != 422)
			{
				continue;
			}
			int data = Terrain.ExtractData(slotValue);
			int num2 = 0;
			if (BlocksManager.Blocks[num] is BagBlock)
			{
				num2 = BagBlock.GetBagId(data);
			}
			else if (BlocksManager.Blocks[num] is LargeBagBlock)
			{
				num2 = LargeBagBlock.GetBagId(data);
			}
			else if (BlocksManager.Blocks[num] is BackpackBlock)
			{
				num2 = BackpackBlock.GetBagId(data);
			}
			else if (BlocksManager.Blocks[num] is ClimbingBackpackBlock)
			{
				num2 = ClimbingBackpackBlock.GetBagId(data);
			}
			if (num2 <= 0)
			{
				continue;
			}
			BagInventory bagInventory = subsystemBags.Find(num2);
			if (bagInventory == null)
			{
				continue;
			}
			for (int j = 0; j < bagInventory.SlotsCount; j++)
			{
				int slotValue2 = bagInventory.GetSlotValue(j);
				if (bagInventory.GetSlotCount(j) > 0 && IsGoodFood(slotValue2))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsGoodFood(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 318 || num == 226 || num == 326)
		{
			return false;
		}
		Block obj = BlocksManager.Blocks[num];
		float nutritionalValue = obj.GetNutritionalValue(value);
		float sicknessProbability = obj.GetSicknessProbability(value);
		if (nutritionalValue > 0f)
		{
			return sicknessProbability < 0.1f;
		}
		return false;
	}
}
