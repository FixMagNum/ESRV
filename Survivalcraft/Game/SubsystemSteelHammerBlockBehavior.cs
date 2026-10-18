using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemSteelHammerBlockBehavior : SubsystemBlockBehavior
{
	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemTime m_subsystemTime;

	private Random m_random;

	private ComponentMiner m_lastMinerUsed;

	private Dictionary<Guid, double> playerLastUseTime = new Dictionary<Guid, double>();

	public override int[] HandledBlocks => new int[1] { 367 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		Guid playerGUID = componentMiner.ComponentPlayer.PlayerData.PlayerGUID;
		double gameTime = m_subsystemTime.GameTime;
		if (playerLastUseTime.ContainsKey(playerGUID) && gameTime - playerLastUseTime[playerGUID] < 6.0)
		{
			return false;
		}
		ComponentVitalStats componentVitalStats = componentMiner.Entity.FindComponent<ComponentVitalStats>();
		if (componentVitalStats != null && componentVitalStats.Stamina >= 0.4f)
		{
			componentVitalStats.Stamina -= 0.5f;
			playerLastUseTime[playerGUID] = gameTime;
			Vector3 vector = -Vector3.Normalize(ray.Direction) * 15f;
			componentMiner.ComponentCreature.ComponentBody.ApplyImpulse(vector);
			componentMiner.DamageActiveTool(1);
			m_subsystemAudio.PlaySound("Audio/SplashHit", 4f, m_random.Float(-0.3f, 0f), componentMiner.ComponentCreature.ComponentBody.Position, 7f, autoDelay: false);
			Vector3 position = componentMiner.ComponentCreature.ComponentBody.Position;
			CreateSplashSmokeEffect(position);
			Vector3 position2 = position + vector * 0.2f;
			CreateSingleSmokeEffect(position2);
			Time.QueueTimeDelayedExecution(Time.FrameStartTime + 6.0, () =>
			{
				m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, m_random.Float(-0.9f, -0.7f), componentMiner.ComponentCreature.ComponentBody.Position, 2f, autoDelay: false);
			});
			return true;
		}
		return false;
	}

	public SubsystemSteelHammerBlockBehavior()
	{
		m_random = new Random();
	}

	private void CreateSplashSmokeEffect(Vector3 basePosition)
	{
		Point3[] array = new Point3[9]
		{
			new Point3(-1, 0, -1),
			new Point3(0, 0, -1),
			new Point3(1, 0, -1),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 1),
			new Point3(0, 0, 1),
			new Point3(1, 0, 1)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Point3 point = array[i];
			Vector3 position = basePosition + new Vector3(point.X, 0f, point.Z);
			CreateSingleSmokeEffect(position);
		}
	}

	private void CreateSingleSmokeEffect(Vector3 position)
	{
		GunSmokeParticleSystem particleSystem = new GunSmokeParticleSystem(m_subsystemTerrain, position, Vector3.UnitY * 0.1f)
		{
			ParticleSize = 0.6f,
			SpawnRateMultiplier = 0.1f
		};
		m_subsystemParticles.AddParticleSystem(particleSystem);
	}
}
