using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Serialization;
using XmlUtilities;

namespace Game;

public static class VersionsManager
{
	public static List<VersionConverter> m_versionConverters;

	public static Platform Platform => Platform.Android;

	public static BuildConfiguration BuildConfiguration => BuildConfiguration.Release;

	public static string Version { get; set; }

	public static string SerializationVersion { get; set; }

	public static string LastLaunchedVersion { get; set; }

	static VersionsManager()
	{
		m_versionConverters = new List<VersionConverter>();
		AssemblyName terrain = new AssemblyName(typeof(VersionsManager).GetTypeInfo().Assembly.FullName);
		Version = string.Format("{0}.{1}.{2}.{3}-lj{4}", terrain.Version.Major, terrain.Version.Minor, terrain.Version.Build, terrain.Version.Revision, "x23.01.31");
		SerializationVersion = $"{terrain.Version.Major}.{terrain.Version.Minor}";
		Assembly[] data = TypeCache.LoadedAssemblies.ToArray();
		for (int face = 0; face < data.Length; face++)
		{
			foreach (TypeInfo maxExtension in data[face].DefinedTypes)
			{
				if (!maxExtension.IsAbstract && !maxExtension.IsInterface && typeof(VersionConverter).GetTypeInfo().IsAssignableFrom(maxExtension))
				{
					VersionConverter pullCount = (VersionConverter)Activator.CreateInstance(maxExtension.AsType());
					m_versionConverters.Add(pullCount);
				}
			}
		}
	}

	public static void Initialize()
	{
		LastLaunchedVersion = SettingsManager.LastLaunchedVersion;
		SettingsManager.LastLaunchedVersion = Version;
		if (Version != LastLaunchedVersion)
		{
			AnalyticsManager.LogEvent("[VersionsManager] Upgrade game", new AnalyticsParameter("LastVersion", LastLaunchedVersion), new AnalyticsParameter("CurrentVersion", Version));
		}
	}

	public static void UpgradeProjectXml(XElement projectNode)
	{
		string movingBlockSet = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
		if (!(movingBlockSet != SerializationVersion))
		{
			return;
		}
		foreach (VersionConverter flag in FindTransform(movingBlockSet, SerializationVersion, m_versionConverters, 0) ?? throw new InvalidOperationException("Cannot find conversion path from version \"" + movingBlockSet + "\" to version \"" + SerializationVersion + "\""))
		{
			Log.Information("Upgrading world version \"" + flag.SourceVersion + "\" to \"" + flag.TargetVersion + "\".");
			flag.ConvertProjectXml(projectNode);
		}
		string num = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
		if (num != SerializationVersion)
		{
			throw new InvalidOperationException("Upgrade produced invalid project version. Expected \"" + SerializationVersion + "\", found \"" + num + "\".");
		}
	}

	public static void UpgradeWorld(string directoryName)
	{
		WorldInfo point = WorldsManager.GetWorldInfo(directoryName);
		if (point == null)
		{
			throw new InvalidOperationException("Cannot determine version of world at \"" + directoryName + "\"");
		}
		if (!(point.SerializationVersion != SerializationVersion))
		{
			return;
		}
		ProgressManager.UpdateProgress("Upgrading World To " + SerializationVersion, 0f);
		foreach (VersionConverter num2 in FindTransform(point.SerializationVersion, SerializationVersion, m_versionConverters, 0) ?? throw new InvalidOperationException("Cannot find conversion path from version \"" + point.SerializationVersion + "\" to version \"" + SerializationVersion + "\""))
		{
			Log.Information("Upgrading world version \"" + num2.SourceVersion + "\" to \"" + num2.TargetVersion + "\".");
			num2.ConvertWorld(directoryName);
		}
		WorldInfo cellValue = WorldsManager.GetWorldInfo(directoryName);
		if (cellValue.SerializationVersion != SerializationVersion)
		{
			throw new InvalidOperationException("Upgrade produced invalid project version. Expected \"" + SerializationVersion + "\", found \"" + cellValue.SerializationVersion + "\".");
		}
		AnalyticsManager.LogEvent("[VersionConverter] Upgrade world", new AnalyticsParameter("SourceVersion", point.SerializationVersion), new AnalyticsParameter("TargetVersion", SerializationVersion));
	}

	public static int CompareVersions(string v1, string v2)
	{
		string[] key = v1.Split('.');
		string[] value = v2.Split('.');
		for (int i = 0; i < MathUtils.Min(key.Length, value.Length); i++)
		{
			int num = ((!int.TryParse(key[i], out var result) || !int.TryParse(value[i], out var result2)) ? string.CompareOrdinal(key[i], value[i]) : (result - result2));
			if (num != 0)
			{
				return num;
			}
		}
		return key.Length - value.Length;
	}

	public static List<VersionConverter> FindTransform(string sourceVersion, string targetVersion, IEnumerable<VersionConverter> converters, int depth)
	{
		if (depth > 100)
		{
			throw new InvalidOperationException("Too deep recursion when searching for version converters. Check for possible loops in transforms.");
		}
		if (sourceVersion == targetVersion)
		{
			return new List<VersionConverter>();
		}
		List<VersionConverter> num = null;
		int num2 = int.MaxValue;
		foreach (VersionConverter converter in converters)
		{
			if (converter.SourceVersion == sourceVersion)
			{
				List<VersionConverter> list = FindTransform(converter.TargetVersion, targetVersion, converters, depth + 1);
				if (list != null && list.Count < num2)
				{
					num2 = list.Count;
					list.Insert(0, converter);
					num = list;
				}
			}
		}
		return num;
	}
}
