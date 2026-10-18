using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using TemplatesDatabase;
using XmlUtilities;

namespace GameEntitySystem;

public class EntityDataList
{
	public List<EntityData> EntitiesData;

	public EntityDataList()
	{
	}

	public EntityDataList(GameDatabase gameDatabase, XElement entitiesNode, bool ignoreInvalidEntities)
	{
		EntitiesData = new List<EntityData>(entitiesNode.Elements().Count());
		foreach (XElement item in entitiesNode.Elements())
		{
			try
			{
				EntitiesData.Add(new EntityData(gameDatabase, item));
			}
			catch (Exception ex)
			{
				if (!ignoreInvalidEntities)
				{
					throw ex;
				}
				Log.Warning("Ignoring invalid entity. Reason: {0}", ex.Message);
			}
		}
	}

	public EntityDataList(GameDatabase gameDatabase, ValuesDictionary valuesDictionary, bool ignoreInvalidEntities)
	{
		EntitiesData = new List<EntityData>(valuesDictionary.Values.Count());
		foreach (ValuesDictionary value in valuesDictionary.Values)
		{
			try
			{
				EntitiesData.Add(new EntityData(gameDatabase, value));
			}
			catch (Exception ex)
			{
				if (!ignoreInvalidEntities)
				{
					throw ex;
				}
				Log.Warning("Ignoring invalid entity. Reason: {0}", ex.Message);
			}
		}
	}

	public void Save(XElement entitiesNode)
	{
		foreach (EntityData entitiesDatum in EntitiesData)
		{
			XElement entityNode = XmlUtils.AddElement(entitiesNode, "Entity");
			entitiesDatum.Save(entityNode);
		}
	}

	public void Save(ValuesDictionary valuesDictionary)
	{
		int num = 1;
		foreach (EntityData entitiesDatum in EntitiesData)
		{
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue(num.ToString(), valuesDictionary2);
			entitiesDatum.Save(valuesDictionary2);
			num++;
		}
	}
}
