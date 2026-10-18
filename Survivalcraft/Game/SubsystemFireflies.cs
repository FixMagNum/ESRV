using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemFireflies : Subsystem, IUpdateable
{
	private const int MaxFireflies = 30;

	private const float SpawnRadiusMin = 10f;

	private const float SpawnRadiusMax = 25f;

	private const float RemoveDistance = 70f;

	private const float DrawDistance = 40f;

	private readonly Random m_random = new Random();

	private readonly List<FireflyParticleSystem> m_active = new List<FireflyParticleSystem>();

	private SubsystemParticles m_subsystemParticles;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemSky m_subsystemSky;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemTime m_subsystemTime;

	private double m_nextSpawnTime;

	private SubsystemWeather m_subsystemWeather;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
	}

	public void Update(float dt)
	{
		double gameTime = m_subsystemTime.GameTime;
		Vector3 vector = ((m_subsystemAudio != null && m_subsystemAudio.ListenerPositions.Count > 0) ? m_subsystemAudio.ListenerPositions[0] : Vector3.Zero);
		for (int num = m_active.Count - 1; num >= 0; num--)
		{
			FireflyParticleSystem fireflyParticleSystem = m_active[num];
			if (Vector3.DistanceSquared(vector, fireflyParticleSystem.Position) > 14400f)
			{
				try
				{
					m_subsystemParticles.RemoveParticleSystem(fireflyParticleSystem);
				}
				catch
				{
				}
				m_active.RemoveAt(num);
			}
		}
		bool flag = m_subsystemWeather.CurrentWindStrength >= 8f;
		bool flag2 = m_subsystemWeather.m_lightningIntensity >= 0.8f;
		bool flag3 = (flag | flag2) || !IsNight();
		foreach (FireflyParticleSystem item in m_active)
		{
			item.VisibilityFactor = (flag3 ? 0f : 1f);
			item.IsStopped = flag3;
		}
		if (!flag3 && gameTime >= m_nextSpawnTime && m_active.Count < 30)
		{
			int num2 = MathUtils.Min(5, 30 - m_active.Count);
			for (int i = 0; i < num2; i++)
			{
				SpawnFireflies(vector);
			}
			m_nextSpawnTime = gameTime + (double)m_random.Float(2f, 4f);
		}
	}

	private bool IsNight()
	{
		float timeOfDay = m_subsystemSky.m_subsystemTimeOfDay.TimeOfDay;
		if (!(timeOfDay < 0.25f))
		{
			return timeOfDay > 0.75f;
		}
		return true;
	}

	private void SpawnFireflies(Vector3 listener)
	{
		for (int i = 0; i < 60; i++)
		{
			float x = m_random.Float(0f, (float)Math.PI * 2f);
			float num = m_random.Float(10f, 40f);
			int num2 = Terrain.ToCell(listener.X + MathUtils.Cos(x) * num);
			int num3 = Terrain.ToCell(listener.Z + MathUtils.Sin(x) * num);
			int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(num2, num3);
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(num2, topHeight, num3)) != 19)
			{
				continue;
			}
			Vector3 vector = new Vector3((float)num2 + 0.5f, (float)topHeight + 1f + m_random.Float(0f, 0.6f), (float)num3 + 0.5f);
			int num4 = 0;
			foreach (FireflyParticleSystem item in m_active)
			{
				if (Vector3.DistanceSquared(item.Position, vector) < 1f)
				{
					num4++;
				}
			}
			if (num4 < 3)
			{
				FireflyParticleSystem fireflyParticleSystem = new FireflyParticleSystem(m_subsystemTerrain, vector, 40f)
				{
					PopulationMultiplier = m_random.Float(0.4f, 0.8f)
				};
				try
				{
					m_subsystemParticles.AddParticleSystem(fireflyParticleSystem);
					m_active.Add(fireflyParticleSystem);
					break;
				}
				catch (Exception ex)
				{
					Log.Warning("Failed to spawn fireflies: " + ex.Message);
					break;
				}
			}
		}
	}

	public override void Dispose()
	{
		foreach (FireflyParticleSystem item in m_active)
		{
			try
			{
				m_subsystemParticles.RemoveParticleSystem(item);
			}
			catch
			{
			}
		}
		m_active.Clear();
	}
}
