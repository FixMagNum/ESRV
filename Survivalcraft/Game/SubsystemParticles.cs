using System;
using System.Collections.Generic;
using Engine.Graphics;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemParticles : Subsystem, IDrawable, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public Dictionary<ParticleSystemBase, bool> m_particleSystems = new Dictionary<ParticleSystemBase, bool>();

	public PrimitivesRenderer3D PrimitivesRenderer = new PrimitivesRenderer3D();

	public bool ParticleSystemsDraw = true;

	public bool ParticleSystemsSimulate = true;

	public int[] m_drawOrders = new int[1] { 300 };

	public List<ParticleSystemBase> m_endedParticleSystems = new List<ParticleSystemBase>();

	private List<ParticleSystemBase> m_safeUpdateList = new List<ParticleSystemBase>();

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => m_drawOrders;

	public void AddParticleSystem(ParticleSystemBase particleSystem)
	{
		AddNetParticleSystem(particleSystem);
	}

	public void AddNetParticleSystem(ParticleSystemBase particleSystem)
	{
		if (particleSystem.SubsystemParticles == null)
		{
			m_particleSystems.Add(particleSystem, value: true);
			particleSystem.SubsystemParticles = this;
			particleSystem.OnAdded();
			return;
		}
		throw new InvalidOperationException("Particle system is already added.");
	}

	public void RemoveParticleSystem(ParticleSystemBase particleSystem)
	{
		RemoveNetParticleSystem(particleSystem);
	}

	public void RemoveNetParticleSystem(ParticleSystemBase particleSystem)
	{
		if (particleSystem.SubsystemParticles == this && m_particleSystems.ContainsKey(particleSystem))
		{
			particleSystem.OnRemoved();
			m_particleSystems.Remove(particleSystem);
			particleSystem.SubsystemParticles = null;
		}
	}

	public bool ContainsParticleSystem(ParticleSystemBase particleSystem)
	{
		return particleSystem.SubsystemParticles == this;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		if (CommonLib.WorkType != WorkType.Client)
		{
			ParticleSystemsSimulate = false;
			ParticleSystemsDraw = false;
		}
	}

	public void Update(float dt)
	{
		if (!ParticleSystemsSimulate)
		{
			return;
		}
		m_safeUpdateList.Clear();
		m_safeUpdateList.AddRange(m_particleSystems.Keys);
		m_endedParticleSystems.Clear();
		foreach (ParticleSystemBase safeUpdate in m_safeUpdateList)
		{
			if (m_particleSystems.ContainsKey(safeUpdate) && safeUpdate.Simulate(m_subsystemTime.GameTimeDelta))
			{
				m_endedParticleSystems.Add(safeUpdate);
			}
		}
		foreach (ParticleSystemBase endedParticleSystem in m_endedParticleSystems)
		{
			RemoveParticleSystem(endedParticleSystem);
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (!ParticleSystemsDraw)
		{
			return;
		}
		foreach (ParticleSystemBase key in m_particleSystems.Keys)
		{
			key.Draw(camera);
		}
		PrimitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}
}
