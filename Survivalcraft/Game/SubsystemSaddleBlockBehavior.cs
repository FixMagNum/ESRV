using Engine;
using Engine.Graphics;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemSaddleBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 158 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		BodyRaycastResult? i = componentMiner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
		if (i.HasValue)
		{
			ComponentBody componentBody = i.Value.ComponentBody;
			ComponentHealth componentHealth = componentBody.Entity.FindComponent<ComponentHealth>();
			if (componentHealth == null || componentHealth.Health > 0f)
			{
				int type = (int)SaddleBlock.GetType(Terrain.ExtractData(componentMiner.ActiveBlockValue));
				string name = componentBody.Entity.ValuesDictionary.DatabaseObject.Name;
				if (name.EndsWith("_Saddled"))
				{
					if (CommonLib.WorkType != WorkType.Client)
					{
						ComponentMount componentMount = componentBody.Entity.FindComponent<ComponentMount>();
						if (componentMount != null)
						{
							if (componentMount.SaddleType == 0 && type == 1)
							{
								return true;
							}
							componentMount.SaddleType = type;
							componentMount.RideTime = 0f;
							componentMount.IsBroken = false;
						}
						ComponentFourLeggedModel componentFourLeggedModel = componentBody.Entity.FindComponent<ComponentFourLeggedModel>();
						if (componentFourLeggedModel != null)
						{
							if (type == 1)
							{
								ValuesDictionary valuesDictionary = DatabaseManager.FindEntityValuesDictionary(name.Substring(0, name.Length - 8), throwIfNotFound: true);
								if (valuesDictionary != null)
								{
									ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("FourLeggedModel", null);
									if (value != null)
									{
										componentFourLeggedModel.Model = ContentManager.Get<Model>(value.GetValue<string>("ModelName"));
									}
								}
							}
							else
							{
								ValuesDictionary valuesDictionary2 = DatabaseManager.FindEntityValuesDictionary(name, throwIfNotFound: true);
								if (valuesDictionary2 != null)
								{
									ValuesDictionary value2 = valuesDictionary2.GetValue<ValuesDictionary>("FourLeggedModel", null);
									if (value2 != null)
									{
										componentFourLeggedModel.Model = ContentManager.Get<Model>(value2.GetValue<string>("ModelName"));
									}
								}
							}
						}
						m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, m_random.Float(-0.1f, 0.1f), ray.Position, 1f, autoDelay: true);
						componentMiner.RemoveActiveTool(1);
					}
				}
				else
				{
					string entityTemplateName = name + "_Saddled";
					Entity entity = DatabaseManager.CreateEntity(Project, entityTemplateName, throwIfNotFound: false);
					if (entity != null && CommonLib.WorkType != WorkType.Client)
					{
						ComponentMount componentMount2 = entity.FindComponent<ComponentMount>();
						if (componentMount2 != null)
						{
							componentMount2.SaddleType = type;
							componentMount2.RideTime = 0f;
							componentMount2.IsBroken = false;
						}
						ComponentSteedBehavior componentSteedBehavior = entity.FindComponent<ComponentSteedBehavior>();
						if (componentSteedBehavior != null)
						{
							componentSteedBehavior.m_isEnabled = true;
						}
						ComponentFourLeggedModel componentFourLeggedModel2 = entity.FindComponent<ComponentFourLeggedModel>();
						if (componentFourLeggedModel2 != null && type == 1)
						{
							ComponentFourLeggedModel componentFourLeggedModel3 = componentBody.Entity.FindComponent<ComponentFourLeggedModel>();
							if (componentFourLeggedModel3 != null)
							{
								componentFourLeggedModel2.Model = componentFourLeggedModel3.Model;
							}
						}
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
