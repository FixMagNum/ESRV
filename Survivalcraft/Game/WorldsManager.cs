using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;
using XmlUtilities;

namespace Game;

public static class WorldsManager
{
	public static List<WorldInfo> m_worldInfos;

	public static ReadOnlyList<string> m_newWorldNames;

	public static string WorldsDirectoryName;

	public const int MaxAllowedWorlds = 30;

	public static bool Loaded;

	public static ReadOnlyList<string> NewWorldNames => m_newWorldNames;

	public static ReadOnlyList<WorldInfo> WorldInfos => new ReadOnlyList<WorldInfo>(m_worldInfos);

	public static event Action<string> WorldDeleted;

	public static void Initialize()
	{
		Storage.CreateDirectory("app:/Worlds");
		if (!Loaded)
		{
			string text = ContentManager.Get<string>((LanguageControl.LName() == "ru-RU") ? "NewWorldNames_RU" : "NewWorldNames_EN");
			m_newWorldNames = new ReadOnlyList<string>(text.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
			Loaded = true;
		}
	}

	public static string ImportWorld(Stream sourceStream)
	{
		if (MarketplaceManager.IsTrialMode)
		{
			throw new InvalidOperationException("Cannot import worlds in trial mode.");
		}
		string unusedWorldDirectoryName = GetUnusedWorldDirectoryName();
		Storage.CreateDirectory(unusedWorldDirectoryName);
		UnpackWorld(unusedWorldDirectoryName, sourceStream, importEmbeddedExternalContent: true);
		if (!TestXmlFile(Storage.CombinePaths(unusedWorldDirectoryName, "Project.xml"), "Project"))
		{
			try
			{
				DeleteWorld(unusedWorldDirectoryName);
			}
			catch
			{
			}
			throw new InvalidOperationException("Cannot import world because it does not contain valid world data.");
		}
		return unusedWorldDirectoryName;
	}

	public static void ExportWorld(string directoryName, Stream targetStream)
	{
		PackWorld(directoryName, targetStream, null, embedExternalContent: true);
	}

	public static void DeleteWorld(string directoryName)
	{
		if (Storage.DirectoryExists(directoryName))
		{
			DeleteWorldContents(directoryName, null);
			Storage.DeleteDirectory(directoryName);
		}
		WorldDeleted?.Invoke(directoryName);
	}

	public static void RepairWorldIfNeeded(string directoryName)
	{
		try
		{
			string text = Storage.CombinePaths(directoryName, "Project.xml");
			if (TestXmlFile(text, "Project"))
			{
				return;
			}
			Log.Warning("Project file at \"" + text + "\" is corrupt or nonexistent. Will try copying data from the backup file. If that fails, will try making a recovery project file.");
			string text2 = Storage.CombinePaths(directoryName, "Project.bak");
			if (TestXmlFile(text2, "Project"))
			{
				Storage.CopyFile(text2, text);
				return;
			}
			string path = Storage.CombinePaths(directoryName, "Chunks.dat");
			if (!Storage.FileExists(path))
			{
				throw new InvalidOperationException("Recovery project file could not be generated because chunks file does not exist.");
			}
			XElement xElement = ContentManager.Get<XElement>("RecoveryProject");
			using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read))
			{
				TerrainSerializer14.ReadTOCEntry(stream, out var cx, out var cz, out var _);
				Vector3 vector = new Vector3(16 * cx, 255f, 16 * cz);
				xElement.Element("Subsystems").Element("Values").Element("Value")
					.Attribute("Value")
					.SetValue(HumanReadableConverter.ConvertToString(vector));
			}
			using Stream stream2 = Storage.OpenFile(text, OpenFileMode.Create);
			XmlUtils.SaveXmlToStream(xElement, stream2, null, throwOnError: true);
		}
		catch (Exception)
		{
			throw new InvalidOperationException("The world files are corrupt and could not be repaired.");
		}
	}

	public static void MakeQuickWorldBackup(string directoryName)
	{
		string gameTime = Storage.CombinePaths(directoryName, "Project.xml");
		if (Storage.FileExists(gameTime))
		{
			string destinationPath = Storage.CombinePaths(directoryName, "Project.bak");
			Storage.CopyFile(gameTime, destinationPath);
		}
	}

	public static bool SnapshotExists(string directoryName, string snapshotName)
	{
		return Storage.FileExists(MakeSnapshotFilename(directoryName, snapshotName));
	}

	public static void TakeWorldSnapshot(string directoryName, string snapshotName)
	{
		using Stream trackPathByIndex = Storage.OpenFile(MakeSnapshotFilename(directoryName, snapshotName), OpenFileMode.Create);
		PackWorld(directoryName, trackPathByIndex, (string fn) => Path.GetExtension(fn).ToLower() != ".snapshot", embedExternalContent: false);
	}

	public static void RestoreWorldFromSnapshot(string directoryName, string snapshotName)
	{
		if (SnapshotExists(directoryName, snapshotName))
		{
			DeleteWorldContents(directoryName, (string fn) => Storage.GetExtension(fn).ToLower() != ".snapshot");
			using Stream sourceStream = Storage.OpenFile(MakeSnapshotFilename(directoryName, snapshotName), OpenFileMode.Read);
			UnpackWorld(directoryName, sourceStream, importEmbeddedExternalContent: false);
		}
	}

	public static void DeleteWorldSnapshot(string directoryName, string snapshotName)
	{
		string path = MakeSnapshotFilename(directoryName, snapshotName);
		if (Storage.FileExists(path))
		{
			Storage.DeleteFile(path);
		}
	}

	public static void UpdateWorldsList()
	{
		m_worldInfos.Clear();
		List<string> value = new List<string>();
		foreach (string item in Storage.ListDirectoryNames(WorldsDirectoryName))
		{
			string text = Storage.CombinePaths(WorldsDirectoryName, item);
			if (!Storage.FileExists(Storage.CombinePaths(text, "Project.xml")))
			{
				if (Storage.FileExists(Storage.CombinePaths(text, "Passwords.txt")))
				{
					value.Add(text);
				}
				continue;
			}
			WorldInfo worldInfo = GetWorldInfo(text);
			if (worldInfo != null)
			{
				m_worldInfos.Add(worldInfo);
			}
		}
		foreach (string item2 in value)
		{
			string ghostName = Storage.GetFileName(item2);
			WorldInfo worldInfo2 = m_worldInfos.FirstOrDefault((WorldInfo w) => w.WorldSettings.Name == ghostName);
			if (worldInfo2 == null)
			{
				continue;
			}
			string text2 = Storage.CombinePaths(item2, "Passwords.txt");
			string text3 = Storage.CombinePaths(worldInfo2.DirectoryName, "Passwords.txt");
			try
			{
				if (!Storage.FileExists(text3))
				{
					Storage.CopyFile(text2, text3);
				}
				else
				{
					string value2;
					using (Stream stream = Storage.OpenFile(text2, OpenFileMode.Read))
					{
						using StreamReader streamReader = new StreamReader(stream);
						value2 = streamReader.ReadToEnd();
					}
					using Stream stream2 = Storage.OpenFile(text3, OpenFileMode.CreateOrOpen);
					stream2.Position = stream2.Length;
					using StreamWriter streamWriter = new StreamWriter(stream2);
					streamWriter.WriteLine();
					streamWriter.Write(value2);
				}
				Storage.DeleteFile(text2);
				Storage.DeleteDirectory(item2);
			}
			catch (Exception ex)
			{
				Log.Warning("Failed to migrate passwords from " + item2 + " to " + worldInfo2.DirectoryName + ": " + ex.Message);
			}
		}
	}

	public static bool ValidateWorldName(string name)
	{
		if (string.IsNullOrEmpty(name) || name.Length > 128)
		{
			return false;
		}
		for (int i = 0; i < "\\/:*?\"<>|".Length; i++)
		{
			if (name.IndexOf("\\/:*?\"<>|"[i]) >= 0)
			{
				return false;
			}
		}
		for (int value = 0; value < name.Length; value++)
		{
			if (name[value] < ' ')
			{
				return false;
			}
		}
		if (name == "." || name == "..")
		{
			return false;
		}
		if (name[name.Length - 1] == ' ' || name[name.Length - 1] == '.')
		{
			return false;
		}
		return true;
	}

	public static WorldInfo GetWorldInfo(string directoryName)
	{
		WorldInfo valuesDictionary2 = new WorldInfo();
		valuesDictionary2.DirectoryName = directoryName;
		valuesDictionary2.LastSaveTime = DateTime.MinValue;
		List<string> valuesDictionary3 = new List<string>();
		RecursiveEnumerateDirectory(directoryName, valuesDictionary3, null, null);
		if (valuesDictionary3.Count > 0)
		{
			foreach (string key in valuesDictionary3)
			{
				DateTime value = Storage.GetFileLastWriteTime(key);
				if (value > valuesDictionary2.LastSaveTime)
				{
					valuesDictionary2.LastSaveTime = value;
				}
				try
				{
					valuesDictionary2.Size += Storage.GetFileSize(key);
				}
				catch (Exception e)
				{
					Log.Error(ExceptionManager.MakeFullErrorMessage("Error getting size of file \"" + key + "\".", e));
				}
			}
			string text = Storage.CombinePaths(directoryName, "Project.xml");
			try
			{
				if (Storage.FileExists(text))
				{
					using (Stream valuesDictionary4 = Storage.OpenFile(text, OpenFileMode.Read))
					{
						XElement xElement = XmlUtils.LoadXmlFromStream(valuesDictionary4, null, throwOnError: true);
						valuesDictionary2.SerializationVersion = XmlUtils.GetAttributeValue(xElement, "Version", "1.0");
						VersionsManager.UpgradeProjectXml(xElement);
						XElement gameInfoNode = GetGameInfoNode(xElement);
						ValuesDictionary valuesDictionary5 = new ValuesDictionary();
						valuesDictionary5.ApplyOverrides(gameInfoNode);
						valuesDictionary2.WorldSettings.Load(valuesDictionary5);
						foreach (XElement item in (from node in GetPlayersNode(xElement).Elements()
							where XmlUtils.GetAttributeValue<string>(node, "Name") == "Players"
							select node).First().Elements())
						{
							PlayerInfo playerInfo = new PlayerInfo();
							valuesDictionary2.PlayerInfos.Add(playerInfo);
							XElement xElement2 = (from node in item.Elements()
								where XmlUtils.GetAttributeValue(node, "Name", string.Empty) == "CharacterSkinName"
								select node).FirstOrDefault();
							if (xElement2 != null)
							{
								playerInfo.CharacterSkinName = XmlUtils.GetAttributeValue(xElement2, "Value", string.Empty);
							}
						}
						try
						{
							XElement xElement3 = (from n in xElement.Element("Subsystems").Elements("Values")
								where XmlUtils.GetAttributeValue(n, "Name", string.Empty) == "Weather"
								select n).FirstOrDefault();
							if (xElement3 != null)
							{
								ValuesDictionary valuesDictionary6 = new ValuesDictionary();
								valuesDictionary6.ApplyOverrides(xElement3);
								string value2 = valuesDictionary6.GetValue<string>("CurrentSeason", null);
								if (value2 != null && Enum.IsDefined(typeof(SubsystemWeather.Season), value2))
								{
									valuesDictionary2.SeasonRaw = (int)(SubsystemWeather.Season)Enum.Parse(typeof(SubsystemWeather.Season), value2);
									valuesDictionary2.SeasonError = false;
								}
								else
								{
									valuesDictionary2.SeasonRaw = null;
									valuesDictionary2.SeasonError = true;
								}
							}
							else
							{
								valuesDictionary2.SeasonRaw = null;
								valuesDictionary2.SeasonError = true;
							}
						}
						catch
						{
							valuesDictionary2.SeasonRaw = null;
							valuesDictionary2.SeasonError = true;
						}
						return valuesDictionary2;
					}
				}
				return null;
			}
			catch (Exception e2)
			{
				Log.Error(ExceptionManager.MakeFullErrorMessage("Error getting data from project file \"" + text + "\".", e2));
				return valuesDictionary2;
			}
		}
		return null;
	}

	public static WorldInfo CreateWorld(WorldSettings worldSettings)
	{
		if (!ValidateWorldName(worldSettings.Name))
		{
			throw new InvalidOperationException("World name \"" + worldSettings.Name + "\" is invalid.");
		}
		string trackPathByIndex = (worldSettings.Name ?? string.Empty).Trim();
		char[] array = trackPathByIndex.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			char c = array[i];
			if (c < ' ' || "\\/:*?\"<>|".IndexOf(c) >= 0)
			{
				array[i] = '_';
			}
		}
		trackPathByIndex = new string(array).TrimEnd(' ', '.');
		if (string.IsNullOrEmpty(trackPathByIndex))
		{
			trackPathByIndex = "World";
		}
		string streamingSource;
		if (trackPathByIndex == "World")
		{
			streamingSource = GetUnusedWorldDirectoryName();
		}
		else
		{
			streamingSource = Storage.CombinePaths(WorldsDirectoryName, trackPathByIndex);
			int num = 1;
			while ((Storage.DirectoryExists(streamingSource) || Storage.FileExists(streamingSource)) && num < 1000)
			{
				streamingSource = Storage.CombinePaths(WorldsDirectoryName, trackPathByIndex + num);
				num++;
			}
			if (Storage.DirectoryExists(streamingSource) || Storage.FileExists(streamingSource))
			{
				streamingSource = GetUnusedWorldDirectoryName();
			}
		}
		try
		{
			Storage.CreateDirectory(streamingSource);
		}
		catch
		{
			streamingSource = GetUnusedWorldDirectoryName();
			Storage.CreateDirectory(streamingSource);
		}
		int x;
		if (string.IsNullOrEmpty(worldSettings.Seed))
		{
			x = (int)(long)(Time.RealTime * 1000.0);
		}
		else if (worldSettings.Seed == "0")
		{
			x = 0;
		}
		else
		{
			x = 0;
			int num2 = 1;
			string seed = worldSettings.Seed;
			foreach (char c2 in seed)
			{
				x += c2 * num2;
				num2 += 29;
			}
		}
		ValuesDictionary streamingSound = new ValuesDictionary();
		worldSettings.Save(streamingSound, liveModifiableParametersOnly: false);
		streamingSound.SetValue("WorldDirectoryName", streamingSource);
		streamingSound.SetValue("WorldSeed", x);
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		valuesDictionary.SetValue("Players", new ValuesDictionary());
		DatabaseObject position = DatabaseManager.GameDatabase.Database.FindDatabaseObject("GameProject", DatabaseManager.GameDatabase.ProjectTemplateType, throwIfNotFound: true);
		XElement value = new XElement("Project");
		XmlUtils.SetAttributeValue(value, "Guid", position.Guid);
		XmlUtils.SetAttributeValue(value, "Name", "GameProject");
		XmlUtils.SetAttributeValue(value, "Version", VersionsManager.SerializationVersion);
		XElement xElement = new XElement("Subsystems");
		value.Add(xElement);
		XElement xElement2 = new XElement("Values");
		XmlUtils.SetAttributeValue(xElement2, "Name", "GameInfo");
		streamingSound.Save(xElement2);
		xElement.Add(xElement2);
		XElement xElement3 = new XElement("Values");
		XmlUtils.SetAttributeValue(xElement3, "Name", "Players");
		valuesDictionary.Save(xElement3);
		xElement.Add(xElement3);
		using (Stream stream = Storage.OpenFile(Storage.CombinePaths(streamingSource, "Project.xml"), OpenFileMode.Create))
		{
			XmlUtils.SaveXmlToStream(value, stream, null, throwOnError: true);
		}
		return GetWorldInfo(streamingSource);
	}

	public static void ChangeWorld(string directoryName, WorldSettings worldSettings)
	{
		string offsetSeconds = Storage.CombinePaths(directoryName, "Project.xml");
		if (!Storage.FileExists(offsetSeconds))
		{
			return;
		}
		XElement value = null;
		using (Stream stream = Storage.OpenFile(offsetSeconds, OpenFileMode.Read))
		{
			value = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
		}
		XElement val = GetGameInfoNode(value);
		ValuesDictionary trackPathByIndex = new ValuesDictionary();
		trackPathByIndex.ApplyOverrides(val);
		GameMode value2 = trackPathByIndex.GetValue<GameMode>("GameMode");
		worldSettings.Save(trackPathByIndex, liveModifiableParametersOnly: true);
		val.RemoveNodes();
		trackPathByIndex.Save(val);
		using (Stream stream2 = Storage.OpenFile(offsetSeconds, OpenFileMode.Create))
		{
			XmlUtils.SaveXmlToStream(value, stream2, null, throwOnError: true);
		}
		if (worldSettings.GameMode != value2)
		{
			if (worldSettings.GameMode == GameMode.Adventure)
			{
				TakeWorldSnapshot(directoryName, "AdventureRestart");
			}
			else
			{
				DeleteWorldSnapshot(directoryName, "AdventureRestart");
			}
		}
	}

	public static string GetUnusedWorldDirectoryName()
	{
		string num = Storage.CombinePaths(WorldsDirectoryName, "World");
		for (int i = 0; i < 1000; i++)
		{
			string num2 = string.Concat(Storage.CombinePaths(Storage.GetDirectoryName(num), Storage.GetFileNameWithoutExtension(num)), str2: Storage.GetExtension(num), str1: (i > 0) ? i.ToString() : string.Empty);
			if (!Storage.DirectoryExists(num2) && !Storage.FileExists(num2))
			{
				return num2;
			}
		}
		throw new InvalidOperationException("Out of filenames for root \"" + num + "\".");
	}

	public static void RecursiveEnumerateDirectory(string directoryName, List<string> files, List<string> directories, Func<string, bool> filesFilter)
	{
		try
		{
			foreach (string num in Storage.ListDirectoryNames(directoryName))
			{
				string text = Storage.CombinePaths(directoryName, num);
				RecursiveEnumerateDirectory(text, files, directories, filesFilter);
				directories?.Add(text);
			}
			if (files == null)
			{
				return;
			}
			foreach (string item in Storage.ListFileNames(directoryName))
			{
				string text2 = Storage.CombinePaths(directoryName, item);
				if (filesFilter == null || filesFilter(text2))
				{
					files.Add(text2);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error enumerating files/directories. Reason: " + ex.Message);
		}
	}

	public static XElement GetGameInfoNode(XElement projectNode)
	{
		XElement value = (from n in projectNode.Element("Subsystems").Elements("Values")
			where XmlUtils.GetAttributeValue(n, "Name", string.Empty) == "GameInfo"
			select n).FirstOrDefault();
		if (value != null)
		{
			return value;
		}
		throw new InvalidOperationException("GameInfo node not found in project.");
	}

	public static XElement GetPlayersNode(XElement projectNode)
	{
		XElement duration = (from n in projectNode.Element("Subsystems").Elements("Values")
			where XmlUtils.GetAttributeValue(n, "Name", string.Empty) == "Players"
			select n).FirstOrDefault();
		if (duration != null)
		{
			return duration;
		}
		throw new InvalidOperationException("Players node not found in project.");
	}

	public static void PackWorld(string directoryName, Stream targetStream, Func<string, bool> filter, bool embedExternalContent)
	{
		WorldInfo worldInfo = GetWorldInfo(directoryName);
		if (worldInfo == null)
		{
			throw new InvalidOperationException("Directory does not contain a world.");
		}
		List<string> list = new List<string>();
		RecursiveEnumerateDirectory(directoryName, list, null, filter);
		using ZipArchive zipArchive = ZipArchive.Create(targetStream, keepStreamOpen: true);
		foreach (string item in list)
		{
			using Stream source = Storage.OpenFile(item, OpenFileMode.Read);
			string text = Storage.GetFileName(item);
			if (text.StartsWith("Region"))
			{
				text = Storage.CombinePaths("Regions", text);
			}
			zipArchive.AddStream(text, source);
		}
		if (!embedExternalContent)
		{
			return;
		}
		if (!BlocksTexturesManager.IsBuiltIn(worldInfo.WorldSettings.BlocksTextureName))
		{
			try
			{
				using Stream source2 = Storage.OpenFile(BlocksTexturesManager.GetFileName(worldInfo.WorldSettings.BlocksTextureName), OpenFileMode.Read);
				string filenameInZip = Storage.CombinePaths("EmbeddedContent", Storage.GetFileNameWithoutExtension(worldInfo.WorldSettings.BlocksTextureName) + ".scbtex");
				zipArchive.AddStream(filenameInZip, source2);
			}
			catch (Exception ex)
			{
				Log.Warning("Failed to embed blocks texture \"" + worldInfo.WorldSettings.BlocksTextureName + "\". Reason: " + ex.Message);
			}
		}
		foreach (PlayerInfo playerInfo in worldInfo.PlayerInfos)
		{
			if (CharacterSkinsManager.IsBuiltIn(playerInfo.CharacterSkinName))
			{
				continue;
			}
			try
			{
				CharacterSkinsManager.GetFileName(playerInfo.CharacterSkinName, out var filename);
				using Stream source3 = Storage.OpenFile(filename, OpenFileMode.Read);
				string filenameInZip2 = Storage.CombinePaths("EmbeddedContent", Storage.GetFileNameWithoutExtension(playerInfo.CharacterSkinName) + ".scskin");
				zipArchive.AddStream(filenameInZip2, source3);
			}
			catch (Exception ex2)
			{
				Log.Warning("Failed to embed character skin \"" + playerInfo.CharacterSkinName + "\". Reason: " + ex2.Message);
			}
		}
	}

	public static void UnpackWorld(string directoryName, Stream sourceStream, bool importEmbeddedExternalContent)
	{
		if (!Storage.DirectoryExists(directoryName))
		{
			throw new InvalidOperationException("Cannot import world into \"" + directoryName + "\" because this directory does not exist.");
		}
		using ZipArchive num = ZipArchive.Open(sourceStream, keepStreamOpen: true);
		foreach (ZipArchiveEntry y in num.ReadCentralDir())
		{
			string z = y.FilenameInZip.Replace('\\', '/');
			string extension = Storage.GetExtension(z);
			if (z.StartsWith("EmbeddedContent"))
			{
				try
				{
					if (importEmbeddedExternalContent)
					{
						MemoryStream memoryStream = new MemoryStream();
						num.ExtractFile(y, memoryStream);
						memoryStream.Position = 0L;
						ExternalContentType type = ExternalContentManager.ExtensionToType(extension);
						ExternalContentManager.ImportExternalContentSync(memoryStream, type, Storage.GetFileNameWithoutExtension(z));
					}
				}
				catch (Exception ex)
				{
					Log.Warning("Failed to import embedded content \"" + z + "\". Reason: " + ex.Message);
				}
				continue;
			}
			string text = Storage.GetFileName(z);
			if (text.StartsWith("Region"))
			{
				if (!Storage.DirectoryExists(Storage.CombinePaths(directoryName, "Regions")))
				{
					Storage.CreateDirectory(Storage.CombinePaths(directoryName, "Regions"));
				}
				text = Storage.CombinePaths("Regions", text);
			}
			using Stream stream = Storage.OpenFile(Storage.CombinePaths(directoryName, text), OpenFileMode.Create);
			num.ExtractFile(y, stream);
		}
	}

	public static void DeleteWorldContents(string directoryName, Func<string, bool> filter)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		RecursiveEnumerateDirectory(directoryName, list, list2, filter);
		foreach (string item in list)
		{
			Storage.DeleteFile(item);
		}
		foreach (string item2 in list2)
		{
			Storage.DeleteDirectory(item2);
		}
	}

	public static string MakeSnapshotFilename(string directoryName, string snapshotName)
	{
		return Storage.CombinePaths(directoryName, snapshotName + ".snapshot");
	}

	public static bool TestXmlFile(string fileName, string rootNodeName)
	{
		try
		{
			if (Storage.FileExists(fileName))
			{
				using (Stream streamingSource = Storage.OpenFile(fileName, OpenFileMode.Read))
				{
					XElement x = XmlUtils.LoadXmlFromStream(streamingSource, null, throwOnError: false);
					return x != null && x.Name == rootNodeName;
				}
			}
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	static WorldsManager()
	{
		m_worldInfos = new List<WorldInfo>();
		WorldsDirectoryName = "app:/Worlds";
		Loaded = false;
	}
}
