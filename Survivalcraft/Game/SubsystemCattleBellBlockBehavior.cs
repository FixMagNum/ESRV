using Engine;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemCattleBellBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 358 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		BodyRaycastResult? bodyRaycastResult = componentMiner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
		if (bodyRaycastResult.HasValue)
		{
			ComponentBody componentBody = bodyRaycastResult.Value.ComponentBody;
			ComponentHealth componentHealth = componentBody.Entity.FindComponent<ComponentHealth>();
			if (componentHealth == null || componentHealth.Health > 0f)
			{
				string entityTemplateName = componentBody.Entity.ValuesDictionary.DatabaseObject.Name + "_Tamed";
				Entity entity = DatabaseManager.CreateEntity(Project, entityTemplateName, throwIfNotFound: false);
				if (entity != null && CommonLib.WorkType != WorkType.Client)
				{
					ComponentBody componentBody2 = entity.FindComponent<ComponentBody>(throwOnError: true);
					componentBody2.Position = componentBody.Position;
					componentBody2.Rotation = componentBody.Rotation;
					componentBody2.Velocity = componentBody.Velocity;
					componentBody2.Size = componentBody.Size;
					entity.FindComponent<ComponentSpawn>(throwOnError: true).SpawnDuration = 0f;
					Project.RemoveEntity(componentBody.Entity, disposeEntity: true);
					Project.AddEntity(entity);
					m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, m_random.Float(-0.1f, 0.1f), ray.Position, 1f, autoDelay: true);
					componentMiner.RemoveActiveTool(1);
				}
			}
			return true;
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
	}
}
