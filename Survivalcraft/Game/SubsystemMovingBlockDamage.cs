using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemMovingBlockDamage : Subsystem, IUpdateable
{
	private const float DamageSpeedMin = 4f;

	private const float DamageSpeedMax = 16f;

	private SubsystemMovingBlocks _movingBlocks;

	private SubsystemBodies _bodies;

	private SubsystemAudio _audio;

	private readonly Dictionary<IMovingBlockSet, HashSet<Entity>> _damagedEntities = new Dictionary<IMovingBlockSet, HashSet<Entity>>();

	private SubsystemGameInfo _gameInfo;

	UpdateOrder IUpdateable.UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		_movingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		_bodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		_audio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		_gameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		_movingBlocks.Stopped += OnMovingBlockSetStopped;
	}

	public void Update(float dt)
	{
		if (_movingBlocks.MovingBlockSets.Count == 0)
		{
			return;
		}
		foreach (IMovingBlockSet movingBlockSet in _movingBlocks.MovingBlockSets)
		{
			if (movingBlockSet.Id == "Piston")
			{
				continue;
			}
			float num = movingBlockSet.CurrentVelocity.Length();
			if (!(num >= 4f))
			{
				continue;
			}
			BoundingBox boundingBox = movingBlockSet.BoundingBox(extendToFillCells: true);
			float num2 = MathUtils.Lerp(0.2f, 1f, MathUtils.Saturate((num - 4f) / 12f));
			if (!_damagedEntities.TryGetValue(movingBlockSet, out var value))
			{
				value = new HashSet<Entity>();
				_damagedEntities[movingBlockSet] = value;
			}
			foreach (ComponentBody body in _bodies.Bodies)
			{
				if (!boundingBox.Intersection(body.BoundingBox) || value.Contains(body.Entity))
				{
					continue;
				}
				ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					float num3 = num2;
					ComponentClothing componentClothing = componentCreature.Entity.FindComponent<ComponentClothing>();
					if (componentClothing != null)
					{
						num3 = componentClothing.ApplyArmorProtection(num3);
					}
					num3 = MathUtils.Max(num3, num2 * 0.1f);
					if (_gameInfo.WorldSettings.GameMode == GameMode.Harmless)
					{
						num3 = MathUtils.Min(num3, 0.95f);
					}
					if (num3 > 0f)
					{
						string cause = LanguageControl.Get("ComponentHealth", "killed_by_collapse");
						componentCreature.ComponentHealth.Injure(num3, null, ignoreInvulnerability: false, cause);
						_audio?.PlayRandomSound("Audio/MovingBlockImpact", 1f, 0f, body.Position, 16f, 0.2f);
						value.Add(body.Entity);
					}
				}
			}
		}
	}

	private void OnMovingBlockSetStopped(IMovingBlockSet set)
	{
		_damagedEntities.Remove(set);
	}
}
