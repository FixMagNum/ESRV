using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

public static class EnumUtils
{
	public struct NamesValues
	{
		public ReadOnlyList<string> Names;

		public ReadOnlyList<int> Values;
	}

	public static class Cache
	{
		public static Dictionary<Type, NamesValues> m_namesValuesByType = new Dictionary<Type, NamesValues>();

		public static NamesValues Query(Type type)
		{
			lock (m_namesValuesByType)
			{
				if (!m_namesValuesByType.TryGetValue(type, out var i))
				{
					i = new NamesValues
					{
						Names = new ReadOnlyList<string>(new List<string>(Enum.GetNames(type))),
						Values = new ReadOnlyList<int>(new List<int>(Enum.GetValues(type).Cast<int>()))
					};
					m_namesValuesByType.Add(type, i);
				}
				return i;
			}
		}
	}

	public static string GetEnumName(Type type, int value)
	{
		int num = GetEnumValues(type).IndexOf(value);
		if (num >= 0)
		{
			return GetEnumNames(type)[num];
		}
		return "<invalid enum>";
	}

	public static IList<string> GetEnumNames(Type type)
	{
		return Cache.Query(type).Names;
	}

	public static IList<int> GetEnumValues(Type type)
	{
		return Cache.Query(type).Values;
	}
}
