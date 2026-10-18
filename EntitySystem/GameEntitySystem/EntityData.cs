using System;
using System.Xml.Linq;
using TemplatesDatabase;
using XmlUtilities;

namespace GameEntitySystem;

public class EntityData
{
	public int Id;

	public ValuesDictionary ValuesDictionary;

	public EntityData()
	{
	}

	public EntityData(GameDatabase gameDatabase, XElement entityNode)
	{
		Id = XmlUtils.GetAttributeValue<int>(entityNode, "Id");
		Guid attributeValue = XmlUtils.GetAttributeValue(entityNode, "Guid", Guid.Empty);
		string attributeValue2 = XmlUtils.GetAttributeValue(entityNode, "Name", string.Empty);
		DatabaseObject databaseObject;
		if (attributeValue != Guid.Empty)
		{
			databaseObject = gameDatabase.Database.FindDatabaseObject(attributeValue, gameDatabase.EntityTemplateType, throwIfNotFound: true);
		}
		else
		{
			if (string.IsNullOrEmpty(attributeValue2))
			{
				throw new InvalidOperationException("Entity template guid or name must be specified.");
			}
			databaseObject = gameDatabase.Database.FindDatabaseObject(attributeValue2, gameDatabase.EntityTemplateType, throwIfNotFound: true);
		}
		ValuesDictionary = new ValuesDictionary();
		ValuesDictionary.PopulateFromDatabaseObject(databaseObject);
		ValuesDictionary.ApplyOverrides(entityNode);
	}

	public EntityData(GameDatabase gameDatabase, ValuesDictionary valuesDictionary)
	{
		Id = valuesDictionary.GetValue<int>("Id");
		Guid value = valuesDictionary.GetValue("Guid", Guid.Empty);
		string value2 = valuesDictionary.GetValue("Name", string.Empty);
		DatabaseObject databaseObject;
		if (value != Guid.Empty)
		{
			databaseObject = gameDatabase.Database.FindDatabaseObject(value, gameDatabase.EntityTemplateType, throwIfNotFound: true);
		}
		else
		{
			if (string.IsNullOrEmpty(value2))
			{
				throw new InvalidOperationException("Entity template guid or name must be specified.");
			}
			databaseObject = gameDatabase.Database.FindDatabaseObject(value2, gameDatabase.EntityTemplateType, throwIfNotFound: true);
		}
		ValuesDictionary = new ValuesDictionary();
		ValuesDictionary.PopulateFromDatabaseObject(databaseObject);
		ValuesDictionary.ApplyOverrides(valuesDictionary.GetValue<ValuesDictionary>("Overrides"));
	}

	public void Save(XElement entityNode)
	{
		XmlUtils.SetAttributeValue(entityNode, "Id", Id);
		XmlUtils.SetAttributeValue(entityNode, "Guid", ValuesDictionary.DatabaseObject.Guid);
		XmlUtils.SetAttributeValue(entityNode, "Name", ValuesDictionary.DatabaseObject.Name);
		ValuesDictionary.Save(entityNode);
	}

	public void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("Id", Id);
		valuesDictionary.SetValue("Guid", ValuesDictionary.DatabaseObject.Guid);
		valuesDictionary.SetValue("Name", ValuesDictionary.DatabaseObject.Name);
		valuesDictionary.SetValue("Overrides", ValuesDictionary);
	}
}
