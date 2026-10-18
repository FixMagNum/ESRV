using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentHealth : Component, IUpdateable
{
	private struct CorpseFly
	{
		public FlyParticleSystem Ps;

		public Vector3 LocalOffset;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemPickables m_subsystemPickables;

	public ComponentCreature m_componentCreature;

	public ComponentPlayer m_componentPlayer;

	public ComponentOnFire m_componentOnFire;

	public float m_lastHealth;

	public bool m_wasStanding;

	public float m_redScreenFactor;

	public static string fName;

	public Random m_random;

	public UnderwaterBubblesParticleSystem m_underwaterBubblesParticleSystem;

	public SubsystemBlockEntities m_subsystemBlockEntities;

	private bool m_hblTrigger;

	private float m_hblTimer;

	private readonly Dictionary<string, double> m_recentAttackerSpecies;

	private const double WhatTheH_Window = 60.0;

	private float m_fallStartY;

	private bool m_isFalling;

	private bool m_hasLanded;

	private float m_landingTimer;

	private int m_fliesSpawned;

	private readonly List<CorpseFly> m_corpseFlies;

	public SubsystemAudio m_subsystemAudio;

	private Sound m_fliesLoop;

	private const float FliesMinDistance = 2f;

	private const float FliesBaseVolume = 0.35f;

	public SubsystemSmokeAccumulation m_subsystemSmokeAccumulation;

	private double m_nextSmokeHintTime;

	private double m_nextSmokeCritHintTime;

	private double m_nextSmokeDamageTime;

	private Point3 m_lastFacedSmokeCell;

	private float m_lastFacedSmokeLevel;

	private double m_nextSmokeCoughTime;

	private double? m_corpseInLavaStartTime;

	private bool m_embraceFallBoostApplied;

	public SubsystemExplosions m_subsystemExplosions;

	public SubsystemPlayers m_subsystemPlayers;

	public float m_lastSentHealth = -1f;

	public string CauseOfDeath { get; set; }

	public bool IsInvulnerable { get; set; }

	public float Health { get; set; }

	public float HealthChange { get; set; }

	public BreathingMode BreathingMode { get; set; }

	public float Air { get; set; }

	public float AirCapacity { get; set; }

	public bool CanStrand { get; set; }

	public float AttackResilience { get; set; }

	public float FallResilience { get; set; }

	public float FireResilience { get; set; }

	public double? DeathTime { get; set; }

	public float CorpseDuration { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public event Action<ComponentCreature> Attacked;

	public void Heal(float amount)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			if (CommonLib.WorkType == WorkType.Server && m_componentPlayer != null && m_componentPlayer.ComponentSleep.m_subsystemUpdate.UpdatesPerFrame == 1 && m_componentPlayer.ComponentSleep.IsSleeping)
			{
				amount *= m_subsystemGameInfo.WorldSettings.RecoverFator;
			}
			if (amount > 0f)
			{
				Health = MathUtils.Min(Health + amount, 1f);
			}
		}
	}

	public void Injure(float amount, ComponentCreature attacker, bool ignoreInvulnerability, string cause)
	{
		if ((m_componentCreature != null && m_componentCreature.ComponentBody != null && m_componentCreature.ComponentBody.EmbraceAirCrouchLatched) || amount <= 0f || (!ignoreInvulnerability && IsInvulnerable) || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		bool flag = false;
		foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
		{
			if (playersDatum.IsMainPlayer && playersDatum.IsCheater)
			{
				flag = true;
				break;
			}
		}
		if (m_componentPlayer != null)
		{
			float maxHealthMultiplier = m_subsystemGameInfo.WorldSettings.MaxHealthMultiplier;
			if (maxHealthMultiplier > 0f)
			{
				amount /= maxHealthMultiplier;
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless && Health - amount <= 0f && amount > 0.5f && m_random.Float(0f, 1f) < 0.75f)
			{
				amount = MathUtils.Max(Health - 0.05f, 0f);
			}
		}
		else if (flag)
		{
			amount /= 3f;
		}
		if (attacker != null && cause != "Karma")
		{
			ComponentPlayer componentPlayer = attacker.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer != null && componentPlayer.PlayerData.IsCheater)
			{
				float amount2 = MathUtils.Max(amount * 0.3f, 0.5f);
				attacker.ComponentHealth.Injure(amount2, m_componentCreature, ignoreInvulnerability: true, "Karma");
			}
		}
		NetInjure(amount, attacker, cause);
		CommonLib.Net.QueuePackage(new ComponentHealthPackage(this, attacker, amount, cause, ignoreInvulnerability));
	}

	public void NetInjure(float amount, ComponentCreature attacker, string cause)
	{
		if (m_componentCreature != null && m_componentCreature.ComponentBody != null)
		{
			float size = m_componentCreature.ComponentBody.Size;
			if (size > 0.01f)
			{
				amount /= size;
			}
		}
		if (Health > 0f)
		{
			if (attacker != null && attacker != m_componentCreature)
			{
				double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
				string text = attacker.Entity.ValuesDictionary.DatabaseObject.Name ?? "Unknown";
				if (text.EndsWith("_Saddled", StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(0, text.Length - "_Saddled".Length);
				}
				if (text.EndsWith("_Tamed", StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(0, text.Length - "_Tamed".Length);
				}
				m_recentAttackerSpecies[text] = totalElapsedGameTime;
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, double> recentAttackerSpecy in m_recentAttackerSpecies)
				{
					if (totalElapsedGameTime - recentAttackerSpecy.Value > 60.0)
					{
						list.Add(recentAttackerSpecy.Key);
					}
				}
				foreach (string item in list)
				{
					m_recentAttackerSpecies.Remove(item);
				}
			}
			if (m_componentCreature.PlayerStats != null)
			{
				if (attacker != null)
				{
					m_componentCreature.PlayerStats.HitsReceived++;
				}
				m_componentCreature.PlayerStats.TotalHealthLost += MathUtils.Min(amount, Health);
			}
			Health = MathUtils.Max(Health - amount, 0f);
			if (Health == 0f)
			{
				if (m_componentCreature.PlayerAchievements != null && m_componentCreature.PlayerAchievements.WhatTheH == 0L)
				{
					double totalElapsedGameTime2 = m_subsystemGameInfo.TotalElapsedGameTime;
					int num = 0;
					foreach (KeyValuePair<string, double> recentAttackerSpecy2 in m_recentAttackerSpecies)
					{
						if (totalElapsedGameTime2 - recentAttackerSpecy2.Value <= 60.0)
						{
							num++;
						}
					}
					if (num >= 4)
					{
						m_componentCreature.PlayerAchievements.WhatTheH = 1L;
						m_componentCreature.PlayerAchievements.EnqueueWhatTheHAchievement();
					}
					m_recentAttackerSpecies.Clear();
				}
				CauseOfDeath = cause;
				if (m_componentCreature.PlayerAchievements != null && m_componentPlayer != null && m_componentPlayer.ComponentSleep.IsSleeping && m_componentCreature.PlayerAchievements.SweetDreams == 0L)
				{
					m_componentCreature.PlayerAchievements.SweetDreams = 1L;
					m_componentCreature.PlayerAchievements.EnqueueSweetDreamsAchievement();
				}
				if (m_componentCreature.PlayerStats != null)
				{
					m_componentCreature.PlayerStats.AddDeathRecord(new PlayerStats.DeathRecord
					{
						Day = m_subsystemTimeOfDay.Day,
						Location = m_componentCreature.ComponentBody.Position,
						Cause = cause
					});
				}
				if (attacker != null)
				{
					ComponentPlayer componentPlayer = attacker.Entity.FindComponent<ComponentPlayer>();
					if (componentPlayer != null)
					{
						if (m_componentPlayer != null)
						{
							componentPlayer.PlayerStats.PlayerKills++;
						}
						else if (m_componentCreature.Category == CreatureCategory.LandPredator || m_componentCreature.Category == CreatureCategory.LandOther)
						{
							componentPlayer.PlayerStats.LandCreatureKills++;
						}
						else if (m_componentCreature.Category == CreatureCategory.WaterPredator || m_componentCreature.Category == CreatureCategory.WaterOther)
						{
							componentPlayer.PlayerStats.WaterCreatureKills++;
						}
						else
						{
							componentPlayer.PlayerStats.AirCreatureKills++;
						}
						int num2 = (int)MathUtils.Ceiling(m_componentCreature.ComponentHealth.AttackResilience / 12f);
						for (int i = 0; i < num2; i++)
						{
							Vector2 vector = m_random.Vector2(2.5f, 3.5f);
							m_subsystemPickables.AddPickable(248, 1, m_componentCreature.ComponentBody.Position, new Vector3(vector.X, 6f, vector.Y), null);
						}
					}
				}
			}
		}
		if (attacker != null)
		{
			Attacked?.Invoke(attacker);
		}
	}

	public void Update(float dt)
	{
		if (m_componentPlayer != null)
		{
			PlayerData memoryStream = m_componentPlayer.PlayerData;
			string text = memoryStream.CurrentHardwareId;
			if (string.IsNullOrEmpty(text) && (CommonLib.WorkType == WorkType.Local || memoryStream.IsMainPlayer))
			{
				text = CommonLib.HardwareId;
			}
			if (m_subsystemPlayers.IsBanned(memoryStream.PlayerGUID, null, text))
			{
				Health = 0f;
				HealthChange = -100f;
				DeathTime = m_subsystemGameInfo.TotalElapsedGameTime;
				CauseOfDeath = ":)";
			}
		}
		Vector3 position = m_componentCreature.ComponentBody.Position;
		bool item = Air == 0f;
		bool flag = Air < 0.5f * AirCapacity;
		if (Health > 0f && Health < 0.4f && m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0))
		{
			int dropletsCount = m_random.Int(1, 3);
			Vector3 origin = position + new Vector3(0f, m_componentCreature.ComponentBody.BoxSize.Y * 0.6f, 0f);
			Vector3 direction = Vector3.Normalize(m_random.Vector3(0.2f) + Vector3.UnitY * -1f);
			m_subsystemParticles.AddParticleSystem(new BloodDropletParticleSystem(m_subsystemTerrain, origin, direction, dropletsCount, 15f, 1f, 2f, -9.81f));
		}
		if (Health > 0f && Health < 1f)
		{
			if (CommonLib.WorkType != WorkType.Client && Time.PeriodicEvent(2.0, 0.0) && MathUtils.Abs(Health - m_lastSentHealth) > 0.01f)
			{
				CommonLib.Net.QueuePackage(new ComponentHealthPackage(this));
				m_lastSentHealth = Health;
			}
			float num = 0f;
			if (m_componentPlayer != null)
			{
				if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
				{
					num = 1f / 60f;
				}
				else if (m_componentPlayer.ComponentSleep.SleepFactor == 1f && m_componentPlayer.ComponentVitalStats.Food > 0f)
				{
					num = 0.0016666667f;
				}
				else if (m_componentPlayer.ComponentVitalStats.Food > 0.5f)
				{
					num = 0.0011111111f;
				}
			}
			else
			{
				num = 0.0011111111f;
			}
			Heal(m_subsystemGameInfo.TotalElapsedGameTimeDelta * num);
		}
		float modFile = 1f;
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace)
		{
			BreathingMode breathingMode = BreathingMode;
			if (m_componentPlayer != null)
			{
				breathingMode = m_subsystemGameInfo.WorldSettings.BreathingMode;
			}
			bool flag2 = false;
			foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
			{
				if (playersDatum.IsMainPlayer && playersDatum.IsCheater)
				{
					flag2 = true;
				}
			}
			if ((m_componentPlayer == null) & flag2)
			{
				Air = 1f;
			}
			else if (breathingMode == BreathingMode.Air || breathingMode == BreathingMode.Water)
			{
				Vector3 eyePosition = m_componentCreature.ComponentCreatureModel.EyePosition;
				int x = Terrain.ToCell(eyePosition.X);
				int num2 = Terrain.ToCell(eyePosition.Y);
				int z = Terrain.ToCell(eyePosition.Z);
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num2, z);
				FluidBlock surfaceFluidBlock = null;
				FocalorsFluidBlock surfaceFluidBlock2 = null;
				float? surfaceHeight = m_subsystemTerrain.Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true).GetSurfaceHeight(x, num2, z, out surfaceFluidBlock);
				float? num3 = m_subsystemTerrain.Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: false)?.GetSurfaceHeight(x, num2, z, out surfaceFluidBlock2);
				float? num4;
				if (surfaceHeight.HasValue && surfaceFluidBlock is WaterBlock)
				{
					num4 = surfaceHeight;
				}
				else if (num3.HasValue && surfaceFluidBlock2 is FocalorsWaterBlock)
				{
					num4 = num3;
				}
				else
				{
					num4 = ((BlocksManager.Blocks[Terrain.ExtractContents(cellValue)] is IWaterloggable waterloggable && waterloggable.IsWaterLogged(Terrain.ExtractData(cellValue))) ? new float?((float)num2 + 1f) : ((float?)null));
				}
				SubsystemOceanScanner subsystemOceanScanner;
				if (num4.HasValue && (subsystemOceanScanner = m_subsystemTerrain.Project.FindSubsystem<SubsystemOceanScanner>(throwOnError: false)) != null)
				{
					num4 = subsystemOceanScanner.GetBlockWaveSurfaceY(eyePosition, num4.Value);
				}
				bool flag3 = num4.HasValue && eyePosition.Y < num4.Value;
				if (breathingMode == BreathingMode.Air)
				{
					Air = ((flag3 || position.Y > 500f) ? MathUtils.Saturate(Air - dt * modFile / AirCapacity) : 1f);
				}
				else
				{
					Air = (flag3 ? 1f : MathUtils.Saturate(Air - dt * modFile / AirCapacity));
				}
			}
		}
		bool skipBubbleStop = false;
		if (Health > 0f && (flag | item) && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
		{
			if (m_componentCreature.ComponentBody.ImmersionFluidBlock is WaterBlock || m_componentCreature.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsWaterBlock)
			{
				if (m_underwaterBubblesParticleSystem == null || m_underwaterBubblesParticleSystem.IsStopped)
				{
					float generationSpeedFactor = ((m_componentPlayer != null) ? 0.5f : 0.2f);
					m_underwaterBubblesParticleSystem = new UnderwaterBubblesParticleSystem(m_subsystemTerrain, generationSpeedFactor);
					m_subsystemParticles.AddParticleSystem(m_underwaterBubblesParticleSystem);
				}
				Vector3 position2 = m_componentCreature.ComponentBody.Position + new Vector3(0f, m_componentCreature.ComponentBody.BoxSize.Y / 2f, 0f);
				int x2 = Terrain.ToCell(position2.X);
				int y = Terrain.ToCell(position2.Y + 0.5f);
				int z2 = Terrain.ToCell(position2.Z);
				if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x2, y, z2)) == 18)
				{
					m_underwaterBubblesParticleSystem.Position = position2;
					if (item)
					{
						if (m_subsystemTime.PeriodicGameTimeEvent(0.2, 0.0))
						{
							m_underwaterBubblesParticleSystem.m_toGenerate += m_random.Float(2f, 4f);
						}
					}
					else if (((AirCapacity > 0f) ? (Air / AirCapacity) : 0f) < 0.2f && m_subsystemTime.PeriodicGameTimeEvent(0.5, 0.0))
					{
						m_underwaterBubblesParticleSystem.m_toGenerate++;
					}
				}
				skipBubbleStop = true;
			}
		}
		if (!skipBubbleStop)
		{
			if (m_underwaterBubblesParticleSystem != null && !m_underwaterBubblesParticleSystem.IsStopped)
			{
				m_underwaterBubblesParticleSystem.IsStopped = true;
			}
		}
		if (m_componentCreature.ComponentBody.ImmersionFactor > 0f)
		{
			if (m_componentCreature.ComponentBody.ImmersionFluidBlock is MagmaBlock || m_componentCreature.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsMagmaBlock)
			{
				string cause = LanguageControl.Get(fName, 1);
				Injure(2f * m_componentCreature.ComponentBody.ImmersionFactor * dt, null, ignoreInvulnerability: false, cause);
				float num5 = 1.1f + 0.1f * (float)MathUtils.Sin(12.0 * m_subsystemTime.GameTime);
				m_redScreenFactor = MathUtils.Max(m_redScreenFactor, num5 * 1.5f * m_componentCreature.ComponentBody.ImmersionFactor);
			}
		}
		if (m_subsystemSmokeAccumulation != null && Health > 0f && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
		{
			Vector3 eyePosition2 = m_componentCreature.ComponentCreatureModel.EyePosition;
			Vector3 vector = m_componentCreature.ComponentBody.Matrix.Forward;
			if (vector.LengthSquared() < 1E-06f)
			{
				vector = Vector3.UnitZ;
			}
			Vector3 v = new Vector3(vector.X, 0f, vector.Z);
			if (v.LengthSquared() < 1E-06f)
			{
				v = Vector3.UnitZ;
			}
			v = Vector3.Normalize(v);
			Vector3 vector2 = eyePosition2 + v * 0.8f;
			Point3 point = new Point3(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Y), Terrain.ToCell(vector2.Z));
			Point3 point2 = new Point3(Terrain.ToCell(eyePosition2.X), Terrain.ToCell(eyePosition2.Y), Terrain.ToCell(eyePosition2.Z));
			float smokeLevel = m_subsystemSmokeAccumulation.GetSmokeLevel(point.X, point.Y, point.Z);
			float smokeLevel2 = m_subsystemSmokeAccumulation.GetSmokeLevel(point2.X, point2.Y, point2.Z);
			Point3 point3 = ((smokeLevel >= smokeLevel2) ? point : point2);
			float num6 = MathUtils.Max(smokeLevel, smokeLevel2);
			float num7 = (m_lastFacedSmokeCell.Equals(point3) ? m_lastFacedSmokeLevel : 0f);
			bool flag4 = num7 < 0.15f && num6 >= 0.15f;
			bool flag5 = num6 > num7 + 0.005f;
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			if (m_componentPlayer != null && (flag4 || ((num6 >= 0.15f) & flag5)) && totalElapsedGameTime >= m_nextSmokeHintTime)
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 8), Color.White, blinking: true, playNotificationSound: false);
				m_nextSmokeHintTime = totalElapsedGameTime + 20.0;
			}
			if (num6 >= 0.6f)
			{
				if (m_componentPlayer != null && totalElapsedGameTime >= m_nextSmokeCritHintTime)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 9), Color.Red, blinking: true, playNotificationSound: false);
					m_nextSmokeCritHintTime = totalElapsedGameTime + 12.0;
				}
				if (totalElapsedGameTime >= m_nextSmokeDamageTime)
				{
					float amount = 0.05f;
					if (m_componentPlayer != null && m_componentPlayer.ComponentSleep.IsSleeping)
					{
						amount = 1f;
					}
					Injure(amount, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 7));
					m_nextSmokeDamageTime = totalElapsedGameTime + 4.5;
					if (totalElapsedGameTime >= m_nextSmokeCoughTime)
					{
						m_componentCreature.ComponentCreatureSounds?.PlayCoughSound();
						m_nextSmokeCoughTime = totalElapsedGameTime + 9.0;
					}
				}
			}
			else
			{
				m_nextSmokeDamageTime = totalElapsedGameTime + 0.25;
				m_nextSmokeCoughTime = totalElapsedGameTime + 0.25;
			}
			m_lastFacedSmokeCell = point3;
			m_lastFacedSmokeLevel = num6;
		}
		float value = MathUtils.Abs(m_componentCreature.ComponentBody.CollisionVelocityChange.Y);
		float filenameInZip = FallResilience;
		if (m_componentPlayer != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			filenameInZip /= 1.35f;
		}
		if (!m_wasStanding && value > filenameInZip && m_subsystemGameInfo.WorldSettings.FallDamageEnabled)
		{
			float num8 = MathUtils.Sqr(MathUtils.Max(value - filenameInZip, 0f)) / 15f;
			if (m_componentPlayer != null)
			{
				num8 /= m_componentPlayer.ComponentLevel.ResilienceFactor;
				if (m_componentPlayer.ComponentVitalStats != null)
				{
					float num9 = num8 * 1.5f;
					m_componentPlayer.ComponentVitalStats.Stamina -= num9;
				}
			}
			string text2 = LanguageControl.Get(fName, 2);
			if (Health <= 0.5f)
			{
				m_componentCreature.ComponentCreatureSounds.PlayFallDamageSound();
			}
			if (CommonLib.WorkType == WorkType.Local)
			{
				Injure(num8, null, ignoreInvulnerability: false, text2);
			}
			if (CommonLib.WorkType == WorkType.Server)
			{
				bool flag6 = m_componentCreature.ComponentBody.ChildBodies.Count > 0 && m_componentCreature.ComponentBody.ChildBodies[0].Player != null && m_componentCreature.ComponentBody.ChildBodies[0].Player.PlayerData.IsMainPlayer;
				bool num10 = m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer;
				bool flag7 = m_componentCreature.ComponentBody.ChildBodies.Count == 0;
				if (num10 | flag6 | flag7)
				{
					Injure(num8, null, ignoreInvulnerability: false, text2);
				}
			}
			else
			{
				bool num11 = m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer;
				bool flag8 = m_componentCreature.ComponentBody.ChildBodies.Count > 0 && m_componentCreature.ComponentBody.ChildBodies[0].Player != null && m_componentCreature.ComponentBody.ChildBodies[0].Player.PlayerData.IsMainPlayer;
				if (num11 | flag8)
				{
					CommonLib.Net.QueuePackage(new ComponentHealthPackage(this, null, num8, text2, ignoreInvulnerability: false, isRequest: true));
				}
			}
		}
		m_wasStanding = m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.StandingOnBody != null;
		bool flag9 = m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0);
		if (flag9 && Air == 0f)
		{
			float num12 = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless) ? 0.08f : 0.12f);
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				num12 = 0.25f;
			}
			if (m_componentPlayer != null)
			{
				num12 /= m_componentPlayer.ComponentLevel.ResilienceFactor;
			}
			Injure(num12, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 7));
		}
		if (flag9 && Health > 0f && (m_componentOnFire.IsOnFire || m_componentOnFire.TouchesFire))
		{
			bool flag10 = m_componentCreature.ComponentBody.ChildBodies.Count > 0 && m_componentCreature.ComponentBody.ChildBodies[0].Player != null && m_componentCreature.ComponentBody.ChildBodies[0].Player.PlayerData.IsMainPlayer;
			bool flag11 = m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer;
			bool flag12 = m_componentCreature.ComponentBody.ChildBodies.Count == 0;
			float num13 = 1f / FireResilience;
			if (m_componentPlayer != null)
			{
				num13 /= m_componentPlayer.ComponentLevel.ResilienceFactor;
			}
			string cuase = LanguageControl.Get(fName, 5);
			if (CommonLib.WorkType == WorkType.Client && (flag11 | flag10))
			{
				CommonLib.Net.QueuePackage(new ComponentHealthPackage(this, null, num13, cuase, ignoreInvulnerability: false, isRequest: true));
			}
			if (CommonLib.WorkType == WorkType.Server)
			{
				if (flag11 | flag10 | flag12)
				{
					Injure(num13, m_componentOnFire.Attacker, ignoreInvulnerability: false, LanguageControl.Get(fName, 5));
				}
			}
			else
			{
				Injure(num13, m_componentOnFire.Attacker, ignoreInvulnerability: false, LanguageControl.Get(fName, 5));
			}
		}
		if (flag9 && CanStrand && m_componentCreature.ComponentBody.ImmersionFactor < 0.25f && (m_componentCreature.ComponentBody.StandingOnValue != 0 || m_componentCreature.ComponentBody.StandingOnBody != null))
		{
			Injure(0.05f, null, ignoreInvulnerability: false, LanguageControl.Get(fName, 6));
		}
		HealthChange = Health - m_lastHealth;
		if (!m_hblTrigger && Health > 0f && Health <= 0.05f && m_lastHealth > 0.05f)
		{
			m_hblTrigger = true;
			m_hblTimer = 0f;
		}
		if (CommonLib.WorkType != WorkType.Client && Health != m_lastHealth && (Health == 1f || Health == 0f))
		{
			CommonLib.Net.QueuePackage(new ComponentHealthPackage(this));
			m_lastSentHealth = Health;
		}
		m_lastHealth = Health;
		if (m_redScreenFactor > 0.01f)
		{
			m_redScreenFactor *= MathUtils.Pow(0.2f, dt);
		}
		else
		{
			m_redScreenFactor = 0f;
		}
		if (HealthChange < 0f)
		{
			m_componentCreature.ComponentCreatureSounds.PlayPainSound();
			m_redScreenFactor += -4f * HealthChange;
			m_componentPlayer?.ComponentGui.HealthBarWidget.Flash(MathUtils.Clamp((int)((0f - HealthChange) * 30f), 0, 10));
		}
		if (m_componentPlayer != null)
		{
			m_componentPlayer.ComponentScreenOverlays.RedoutFactor = MathUtils.Max(m_componentPlayer.ComponentScreenOverlays.RedoutFactor, m_redScreenFactor);
		}
		if (m_componentPlayer != null)
		{
			m_componentPlayer.ComponentGui.HealthBarWidget.Value = Health;
		}
		if (m_hblTrigger)
		{
			if (Health <= 0f)
			{
				m_hblTrigger = false;
				m_hblTimer = 0f;
			}
			else
			{
				m_hblTimer += m_subsystemGameInfo.TotalElapsedGameTimeDelta;
				if (m_hblTimer >= 3600f)
				{
					ComponentCreature componentCreature = m_componentCreature;
					if (componentCreature != null && componentCreature.PlayerAchievements != null && componentCreature.PlayerAchievements.HeavyBurdenLife == 0L)
					{
						componentCreature.PlayerAchievements.HeavyBurdenLife = 1L;
						componentCreature.PlayerAchievements.EnqueueHeavyBurdenLifeAchievement();
					}
					m_hblTrigger = false;
				}
			}
		}
		if (Health == 0f && HealthChange < 0f)
		{
			if (!((m_componentOnFire == null || (!m_componentOnFire.IsOnFire && !m_componentOnFire.TouchesFire)) && !(m_componentCreature.ComponentBody.ImmersionFluidBlock is MagmaBlock) && !(m_componentCreature.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsMagmaBlock)))
			{
				Entity.FindComponent<ComponentLoot>()?.MarkCharred();
			}
			Vector3 position3 = m_componentCreature.ComponentBody.Position + new Vector3(0f, m_componentCreature.ComponentBody.BoxSize.Y / 2f, 0f);
			float x3 = m_componentCreature.ComponentBody.BoxSize.X;
			m_subsystemParticles.AddParticleSystem(new KillParticleSystem(m_subsystemTerrain, position3, x3));
			Vector3 position4 = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
			bool flag13 = true;
			if (m_componentPlayer != null && m_subsystemGameInfo.WorldSettings.KeepInventoryAfterDeath)
			{
				flag13 = false;
			}
			if (flag13)
			{
				foreach (IInventory item2 in Entity.FindComponents<IInventory>())
				{
					item2.DropAllItems(position4);
				}
			}
			Vector3 position5 = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) * 0.5f;
			int x4 = Terrain.ToCell(position5.X);
			int num14 = Terrain.ToCell(position5.Y);
			int z3 = Terrain.ToCell(position5.Z);
			int num15 = 2;
			if (m_componentPlayer != null)
			{
				switch (m_componentPlayer.PlayerData.Tag)
				{
				case PlayerData.PlayerTag.Short:
					num15 = 0;
					break;
				case PlayerData.PlayerTag.Fat:
					num15 = 1;
					break;
				case PlayerData.PlayerTag.Tall:
					num15 = 2;
					break;
				case PlayerData.PlayerTag.Dwarf:
					num15 = 3;
					break;
				}
			}
			int num16 = 0;
			if (m_componentPlayer != null)
			{
				float level = m_componentPlayer.PlayerData.Level;
				num16 = ((!(level <= 6f)) ? ((level <= 9f) ? 1 : ((level <= 15f) ? 2 : ((level <= 21f) ? 3 : ((!(level <= 30f)) ? 5 : 4)))) : 0);
			}
			if (CommonLib.WorkType != WorkType.Client && m_componentPlayer != null)
			{
				int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(x4, num14 - 1, z3);
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)];
				int num17 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x4, num14, z3));
				Block block2 = BlocksManager.Blocks[num17];
				bool flag14 = true;
				if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x4, z3, out var territoriy))
				{
					bool flag15;
					if (m_componentPlayer == null || !m_componentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid) || !territoriy.ApplyToFirend)
					{
						Guid playerGUID = m_componentPlayer.PlayerData.PlayerGUID;
						flag15 = territoriy.OwnerGuid == playerGUID || territoriy.AllowDig;
					}
					else
					{
						flag15 = true;
					}
					flag14 = flag15;
				}
				if (flag14 && m_subsystemGameInfo.WorldSettings.RestrictWorldInteraction && !m_componentPlayer.PlayerData.ServerManager && !SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x4, z3, out var _))
				{
					flag14 = false;
				}
				if (flag14 && block.IsCollidable && !block2.IsCollidable)
				{
					int data = (num15 << 5) | (num16 << 7);
					int value2 = Terrain.MakeBlockValue(363, 0, data);
					m_subsystemTerrain.ChangeCell(x4, num14, z3, value2);
					if (m_componentPlayer != null)
					{
						m_subsystemGameInfo.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).CreateBlockEntity(miner: m_componentPlayer.Entity.FindComponent<ComponentMiner>(), EntityName: "HumanBones", point: new Point3(x4, num14, z3));
					}
				}
				else
				{
					int data2 = (num15 << 5) | (num16 << 7);
					m_subsystemPickables.CreatePickable(null, Terrain.MakeBlockValue(363, 0, data2), 1, position5, null, null);
				}
			}
			DeathTime = m_subsystemGameInfo.TotalElapsedGameTime;
			Project.FindSubsystem<SubsystemCreatureSpawn>(throwOnError: true)?.EnforceCorpseLimit(15);
		}
		if (Health <= 0f)
		{
			ComponentCreatureModel componentCreatureModel = m_componentCreature.ComponentCreatureModel;
			if (m_componentPlayer == null && componentCreatureModel != null && AreAllDetachablePartsHidden(componentCreatureModel))
			{
				Entity.FindComponent<ComponentLoot>()?.DropRemainingNow();
				StopCorpseFlies();
				m_componentCreature.ComponentSpawn.Despawn();
				return;
			}
		}
		if (Health <= 0f && CorpseDuration > 0f)
		{
			double? num18 = m_subsystemGameInfo.TotalElapsedGameTime - DeathTime;
			if ((num18.GetValueOrDefault() > (double)CorpseDuration) & num18.HasValue)
			{
				StopCorpseFlies();
				if (m_componentPlayer == null)
				{
					m_componentCreature.ComponentSpawn.Despawn();
				}
			}
		}
		if (Health <= 0f && DeathTime.HasValue)
		{
			double elapsedSeconds = m_subsystemGameInfo.TotalElapsedGameTime - DeathTime.Value;
			EnsureCorpseFlies(elapsedSeconds);
			if (m_componentOnFire != null && (m_componentOnFire.IsOnFire || m_componentOnFire.TouchesFire))
			{
				ComponentLoot componentLoot = Entity.FindComponent<ComponentLoot>();
				if (componentLoot != null && !componentLoot.m_charred)
				{
					componentLoot.MarkCharred();
				}
			}
			if (m_componentCreature.ComponentBody.ImmersionFluidBlock is MagmaBlock || m_componentCreature.ComponentBody.ImmersionFocalorsFluidBlock is FocalorsMagmaBlock)
			{
				if (!m_corpseInLavaStartTime.HasValue)
				{
					m_corpseInLavaStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
				}
				else if (m_subsystemGameInfo.TotalElapsedGameTime - m_corpseInLavaStartTime.Value >= 10.0)
				{
					StopCorpseFlies();
					if (m_componentPlayer == null)
					{
						m_componentCreature.ComponentSpawn.Despawn();
					}
					return;
				}
			}
			else
			{
				m_corpseInLavaStartTime = null;
			}
		}
		else
		{
			StopCorpseFlies();
		}
		if (m_componentPlayer != null && m_componentPlayer.PlayerData.IsMainPlayer && Health > 0f)
		{
			if (!m_componentCreature.ComponentBody.StandingOnValue.HasValue && m_componentCreature.ComponentBody.StandingOnBody == null && m_componentCreature.ComponentBody.ImmersionFactor < 0.1f)
			{
				if (!m_isFalling)
				{
					m_isFalling = true;
					m_fallStartY = m_componentCreature.ComponentBody.Position.Y;
					m_hasLanded = false;
				}
			}
			else if (m_isFalling && !m_hasLanded)
			{
				m_isFalling = false;
				if (m_fallStartY - m_componentCreature.ComponentBody.Position.Y >= 250f)
				{
					m_landingTimer = 0f;
					m_hasLanded = true;
				}
			}
			else if (m_hasLanded)
			{
				m_landingTimer += m_subsystemGameInfo.TotalElapsedGameTimeDelta;
				if (m_landingTimer >= 1f && m_componentCreature.PlayerAchievements != null && m_componentCreature.PlayerAchievements.Impossible == 0L && Health > 0f)
				{
					m_componentCreature.PlayerAchievements.Impossible = 1L;
					m_componentCreature.PlayerAchievements.EnqueueImpossibleAchievement();
					m_hasLanded = false;
				}
			}
		}
		if (m_componentCreature.ComponentBody.Position.Y >= 270f && m_componentCreature.PlayerAchievements != null && m_componentCreature.PlayerAchievements.SkyDemon == 0L)
		{
			m_componentCreature.PlayerAchievements.SkyDemon = 1L;
			m_componentCreature.PlayerAchievements.EnqueueSkyDemonAchievement();
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_componentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_componentOnFire = Entity.FindComponent<ComponentOnFire>(throwOnError: true);
		AttackResilience = valuesDictionary.GetValue<float>("AttackResilience");
		FallResilience = valuesDictionary.GetValue<float>("FallResilience");
		FireResilience = valuesDictionary.GetValue<float>("FireResilience");
		CorpseDuration = valuesDictionary.GetValue<float>("CorpseDuration");
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		BreathingMode = valuesDictionary.GetValue<BreathingMode>("BreathingMode");
		CanStrand = valuesDictionary.GetValue<bool>("CanStrand");
		Health = valuesDictionary.GetValue<float>("Health");
		Air = valuesDictionary.GetValue<float>("Air");
		AirCapacity = valuesDictionary.GetValue<float>("AirCapacity");
		double value = valuesDictionary.GetValue<double>("DeathTime");
		DeathTime = ((value >= 0.0) ? new double?(value) : ((double?)null));
		CauseOfDeath = valuesDictionary.GetValue<string>("CauseOfDeath");
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && Entity.FindComponent<ComponentPlayer>() != null)
		{
			IsInvulnerable = true;
		}
		m_subsystemSmokeAccumulation = Project.FindSubsystem<SubsystemSmokeAccumulation>(throwOnError: true);
		m_nextSmokeHintTime = 0.0;
		m_nextSmokeCritHintTime = 0.0;
		m_nextSmokeDamageTime = 0.0;
		m_lastFacedSmokeCell = new Point3(int.MinValue, int.MinValue, int.MinValue);
		m_lastFacedSmokeLevel = 0f;
		m_nextSmokeCoughTime = 0.0;
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Health", Health);
		valuesDictionary.SetValue("Air", Air);
		if (DeathTime.HasValue)
		{
			valuesDictionary.SetValue("DeathTime", DeathTime);
		}
		if (!string.IsNullOrEmpty(CauseOfDeath))
		{
			valuesDictionary.SetValue("CauseOfDeath", CauseOfDeath);
		}
	}

	public ComponentHealth()
	{
		m_corpseFlies = new List<CorpseFly>();
		m_recentAttackerSpecies = new Dictionary<string, double>();
		m_random = new Random();
	}

	static ComponentHealth()
	{
		fName = "ComponentHealth";
	}

	private bool AreAllDetachablePartsHidden(ComponentCreatureModel model)
	{
		model.BuildDetachablePartsIfNeeded();
		if (model.m_detachables == null || model.m_detachables.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < model.m_detachables.Count; i++)
		{
			if (!model.m_detachables[i].Hidden)
			{
				return false;
			}
		}
		return true;
	}

	private void EnsureCorpseFlies(double elapsedSeconds)
	{
		int list;
		if (elapsedSeconds >= 2000.0)
		{
			list = 3;
		}
		else
		{
			list = ((elapsedSeconds >= 1000.0) ? 2 : ((elapsedSeconds >= 500.0) ? 1 : 0));
		}
		if (list > m_fliesSpawned)
		{
			SpawnCorpseFlies(list - m_fliesSpawned);
			m_fliesSpawned = list;
		}
		UpdateCorpseFliesPositions();
		UpdateCorpseFliesAudio(list);
	}

	private void SpawnCorpseFlies(int count)
	{
		Vector3 i = GetCorpseFliesAnchor();
		for (int j = 0; j < count; j++)
		{
			Vector3 index = m_random.Vector3(0.08f, 0.03f);
			FlyParticleSystem text = new FlyParticleSystem(m_subsystemTerrain, i + index, 16f);
			text.PopulationMultiplier = 1f;
			m_subsystemParticles.AddParticleSystem(text);
			m_corpseFlies.Add(new CorpseFly
			{
				Ps = text,
				LocalOffset = index
			});
		}
	}

	private void StopCorpseFlies()
	{
		if (m_corpseFlies.Count > 0)
		{
			for (int i = 0; i < m_corpseFlies.Count; i++)
			{
				CorpseFly corpseFly = m_corpseFlies[i];
				if (corpseFly.Ps != null)
				{
					corpseFly.Ps.IsStopped = true;
					m_subsystemParticles.RemoveParticleSystem(corpseFly.Ps);
				}
			}
			m_corpseFlies.Clear();
			m_fliesSpawned = 0;
		}
		if (m_fliesLoop != null)
		{
			m_fliesLoop.Pause();
			m_fliesLoop.Dispose();
			m_fliesLoop = null;
		}
	}

	private Vector3 GetCorpseFliesAnchor()
	{
		BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
		Vector3 result = (boundingBox.Min + boundingBox.Max) * 0.5f;
		result.Y += 0.05f;
		return result;
	}

	private void UpdateCorpseFliesPositions()
	{
		if (m_corpseFlies.Count != 0)
		{
			Vector3 corpseFliesAnchor = GetCorpseFliesAnchor();
			for (int i = 0; i < m_corpseFlies.Count; i++)
			{
				CorpseFly value = m_corpseFlies[i];
				value.Ps.SetCenter(corpseFliesAnchor + value.LocalOffset);
				m_corpseFlies[i] = value;
			}
		}
	}

	private void UpdateCorpseFliesAudio(int desiredCount)
	{
		if (desiredCount <= 0)
		{
			if (m_fliesLoop != null)
			{
				m_fliesLoop.Volume = MathUtils.Lerp(m_fliesLoop.Volume, 0f, MathUtils.Saturate(4f * Time.FrameDuration));
				if (m_fliesLoop.Volume <= 0.5f * AudioManager.MinAudibleVolume)
				{
					m_fliesLoop.Pause();
				}
			}
			return;
		}
		if (m_fliesLoop == null)
		{
			m_fliesLoop = m_subsystemAudio.CreateSound("Audio/RottenFly");
			m_fliesLoop.IsLooped = true;
			m_fliesLoop.Volume = 0f;
			m_fliesLoop.Pitch = m_random.Float(0.95f, 1.05f);
		}
		float distance = m_subsystemAudio.CalculateListenerDistance(GetCorpseFliesAnchor());
		float num = m_subsystemAudio.CalculateVolume(distance, 2f);
		float num2 = 0.35f * ((float)desiredCount / 3f) * num;
		m_fliesLoop.Volume = MathUtils.Lerp(m_fliesLoop.Volume, SettingsManager.SoundsVolume * num2, MathUtils.Saturate(4f * Time.FrameDuration));
		if (m_fliesLoop.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_fliesLoop.Play();
		}
		else
		{
			m_fliesLoop.Pause();
		}
	}
}
