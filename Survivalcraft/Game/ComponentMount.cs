using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentMount : Component
{
	public ComponentBody ComponentBody { get; set; }

	public Vector3 MountOffset { get; set; }

	public Vector3 DismountOffset { get; set; }

	public ComponentRider Rider
	{
		get
		{
			foreach (ComponentBody childBody in ComponentBody.ChildBodies)
			{
				ComponentRider boneAbsoluteTransform2 = childBody.Entity.FindComponent<ComponentRider>();
				if (boneAbsoluteTransform2 != null)
				{
					return boneAbsoluteTransform2;
				}
			}
			return null;
		}
	}

	public int SaddleType { get; set; }

	public float RideTime { get; set; }

	public bool IsBroken { get; set; }

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		ComponentBody = Entity.FindComponent<ComponentBody>(throwOnError: true);
		MountOffset = valuesDictionary.GetValue<Vector3>("MountOffset");
		DismountOffset = valuesDictionary.GetValue<Vector3>("DismountOffset");
		SaddleType = valuesDictionary.GetValue("SaddleType", 0);
		RideTime = valuesDictionary.GetValue("RideTime", 0f);
		IsBroken = valuesDictionary.GetValue("IsBroken", defaultValue: false);
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		valuesDictionary.SetValue("SaddleType", SaddleType);
		valuesDictionary.SetValue("RideTime", RideTime);
		valuesDictionary.SetValue("IsBroken", IsBroken);
	}

	public override void OnEntityAdded()
	{
		ComponentFourLeggedModel flag = Entity.FindComponent<ComponentFourLeggedModel>();
		if (flag == null)
		{
			return;
		}
		if (SaddleType == 1)
		{
			string currentScreen = Entity.ValuesDictionary.DatabaseObject.Name;
			if (!currentScreen.EndsWith("_Saddled"))
			{
				return;
			}
			ValuesDictionary flag2 = DatabaseManager.FindEntityValuesDictionary(currentScreen.Substring(0, currentScreen.Length - 8), throwIfNotFound: true);
			if (flag2 != null)
			{
				ValuesDictionary flag3 = flag2.GetValue<ValuesDictionary>("FourLeggedModel", null);
				if (flag3 != null)
				{
					flag.Model = ContentManager.Get<Model>(flag3.GetValue<string>("ModelName"));
				}
			}
		}
		else
		{
			ValuesDictionary num = DatabaseManager.FindEntityValuesDictionary(Entity.ValuesDictionary.DatabaseObject.Name, throwIfNotFound: true).GetValue<ValuesDictionary>("FourLeggedModel", null);
			if (num != null)
			{
				flag.Model = ContentManager.Get<Model>(num.GetValue<string>("ModelName"));
			}
		}
	}
}
