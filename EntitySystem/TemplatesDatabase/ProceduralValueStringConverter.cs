using System;
using Engine.Serialization;

namespace TemplatesDatabase;

[HumanReadableConverter(typeof(ProceduralValue))]
public class ProceduralValueStringConverter : IHumanReadableConverter
{
	public string ConvertToString(object value)
	{
		return ((ProceduralValue)value).Procedure;
	}

	public object ConvertFromString(Type type, string data)
	{
		return new ProceduralValue
		{
			Procedure = data
		};
	}
}
