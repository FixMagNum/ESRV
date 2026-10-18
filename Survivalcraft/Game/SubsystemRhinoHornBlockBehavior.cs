using System;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemRhinoHornBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemBodies m_subsystemBodies;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemNoise m_subsystemNoise;

	public Random m_random;

	public SubsystemTime m_subsystemTime;

	private DateTime lastUseTime = DateTime.MinValue;

	public override int[] HandledBlocks => new int[1] { 349 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		if ((DateTime.Now - lastUseTime).TotalSeconds < 6.0)
		{
			return false;
		}
		lastUseTime = DateTime.Now;
		m_subsystemAudio.PlayRandomSound("Audio/RhinoHorn", 5f, m_random.Float(-0.6f, 0f), ray.Position, 160f, autoDelay: true);
		m_subsystemNoise.MakeNoise(componentMiner.ComponentCreature.ComponentBody, 0.5f, 30f);
		componentMiner.DamageActiveTool(1);
		componentMiner.ComponentPlayer.ComponentVitalStats.Stamina -= 0.35f;
		TriggerRunAwayBehavior(ray.Position, 40f);
		m_subsystemTime.QueueGameTimeDelayedExecution(0.1, () =>
		{
			ReducePlayerSpeed(componentMiner.ComponentPlayer, 10f);
		});
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
	}

	public SubsystemRhinoHornBlockBehavior()
	{
		m_random = new Random();
	}

	private void ReducePlayerSpeed(ComponentPlayer player, float duration)
	{
		float originalWalkSpeed = player.ComponentLocomotion.WalkSpeed;
		player.ComponentLocomotion.WalkSpeed *= 0.5f;
		m_subsystemTime.QueueGameTimeDelayedExecution(duration, () =>
		{
			player.ComponentLocomotion.WalkSpeed = originalWalkSpeed;
		});
	}

	private void TriggerRunAwayBehavior(Vector3 position, float radius)
	{
		foreach (ComponentBody body in m_subsystemBodies.Bodies)
		{
			ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
			if (componentCreature != null && Vector3.Distance(componentCreature.ComponentBody.Position, position) <= radius)
			{
				componentCreature.Entity.FindComponent<ComponentRunAwayBehavior>()?.RunAwayFrom(componentCreature.ComponentBody);
			}
		}
	}
}
