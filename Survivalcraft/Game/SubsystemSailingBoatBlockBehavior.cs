using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemSailingBoatBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemCreatureSpawn m_creatureSpawn;

	public Random m_random = new Random();

	public static string fName;

	public override int[] HandledBlocks => new int[1] { 412 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		if (Terrain.ExtractContents(componentMiner.ActiveBlockValue) == 412)
		{
			TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
			if (terrainRaycastResult.HasValue)
			{
				Vector3 position = terrainRaycastResult.Value.HitPoint();
				DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
				m_subsystemBodies.FindBodiesInArea(new Vector2(position.X, position.Z) - new Vector2(8f), new Vector2(position.X, position.Z) + new Vector2(8f), dynamicArray);
				if (dynamicArray.Count((ComponentBody b) => b.Entity.ValuesDictionary.DatabaseObject.Name == "Sailing_Boat") < 6)
				{
					m_creatureSpawn.CreateEntity(new SpawnEntityData
					{
						TemplateName = "Sailing_Boat",
						Position = position,
						ConstantSpawn = true
					}, componentMiner, m_subsystemAudio);
				}
				else
				{
					componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(SubsystemBoatBlockBehavior.fName, 1), Color.White, blinking: true, playNotificationSound: false);
				}
				return true;
			}
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_creatureSpawn = Project.FindSubsystem<SubsystemCreatureSpawn>();
	}

	static SubsystemSailingBoatBlockBehavior()
	{
		fName = "SubsystemSailingBoatBlockBehavior";
	}
}
