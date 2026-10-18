using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Engine;
using TemplatesDatabase;

namespace Game;

public static class FurniturePacksManager
{
	public static List<string> m_furniturePackNames;

	public static ReadOnlyList<string> FurniturePackNames => new ReadOnlyList<string>(m_furniturePackNames);

	public static event Action<string> FurniturePackDeleted;

	static FurniturePacksManager()
	{
		m_furniturePackNames = new List<string>();
	}

	public static void Initialize()
	{
		Storage.CreateDirectory("app:/FurniturePacks");
	}

	public static string GetFileName(string name)
	{
		return Storage.CombinePaths("app:/FurniturePacks", name);
	}

	public static string GetDisplayName(string name)
	{
		return Storage.GetFileNameWithoutExtension(name);
	}

	public static DateTime GetCreationDate(string name)
	{
		try
		{
			return Storage.GetFileLastWriteTime(GetFileName(name));
		}
		catch
		{
			return new DateTime(2000, 1, 1);
		}
	}

	public static string ImportFurniturePack(string name, Stream stream)
	{
		if (MarketplaceManager.IsTrialMode)
		{
			throw new InvalidOperationException("Cannot import furniture packs in trial mode.");
		}
		ValidateFurniturePack(stream);
		stream.Position = 0L;
		string data = Storage.GetFileNameWithoutExtension(name);
		name = data + ".scfpack";
		string connectorDirection = GetFileName(name);
		int num = 0;
		while (Storage.FileExists(connectorDirection))
		{
			num++;
			if (num > 9)
			{
				throw new InvalidOperationException("Duplicate name. Delete existing content with conflicting names.");
			}
			name = $"{data} ({num}).scfpack";
			connectorDirection = GetFileName(name);
		}
		using Stream destination = Storage.OpenFile(connectorDirection, OpenFileMode.Create);
		stream.CopyTo(destination);
		return name;
	}

	public static void ExportFurniturePack(string name, Stream stream)
	{
		using Stream num = Storage.OpenFile(GetFileName(name), OpenFileMode.Read);
		num.CopyTo(stream);
	}

	public static string CreateFurniturePack(string name, ICollection<FurnitureDesign> designs)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (ZipArchive zipArchive = ZipArchive.Create(memoryStream, keepStreamOpen: true))
		{
			ValuesDictionary valuesDictionary = new ValuesDictionary();
			SubsystemFurnitureBlockBehavior.SaveFurnitureDesigns(valuesDictionary, designs);
			XElement xElement = new XElement("FurnitureDesigns");
			valuesDictionary.Save(xElement);
			MemoryStream memoryStream2 = new MemoryStream();
			xElement.Save((Stream)memoryStream2);
			memoryStream2.Position = 0L;
			zipArchive.AddStream("FurnitureDesigns.xml", memoryStream2);
		}
		memoryStream.Position = 0L;
		return ImportFurniturePack(name, memoryStream);
	}

	public static void DeleteFurniturePack(string name)
	{
		try
		{
			Storage.DeleteFile(GetFileName(name));
			FurniturePackDeleted?.Invoke(name);
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser("Unable to delete furniture pack \"" + name + "\"", e);
		}
	}

	public static void UpdateFurniturePacksList()
	{
		m_furniturePackNames.Clear();
		foreach (string item in Storage.ListFileNames("app:/FurniturePacks"))
		{
			if (Storage.GetExtension(item).ToLower() == ".scfpack")
			{
				m_furniturePackNames.Add(item);
			}
		}
	}

	public static List<FurnitureDesign> LoadFurniturePack(SubsystemTerrain subsystemTerrain, string name)
	{
		using Stream stream = Storage.OpenFile(GetFileName(name), OpenFileMode.Read);
		return LoadFurniturePack(subsystemTerrain, stream);
	}

	public static void ValidateFurniturePack(Stream stream)
	{
		LoadFurniturePack(null, stream);
	}

	public static List<FurnitureDesign> LoadFurniturePack(SubsystemTerrain subsystemTerrain, Stream stream)
	{
		using ZipArchive zipArchive = ZipArchive.Open(stream, keepStreamOpen: true);
		List<ZipArchiveEntry> list = zipArchive.ReadCentralDir();
		if (list.Count != 1 || list[0].FilenameInZip != "FurnitureDesigns.xml")
		{
			throw new InvalidOperationException("Invalid furniture pack.");
		}
		MemoryStream memoryStream = new MemoryStream();
		zipArchive.ExtractFile(list[0], memoryStream);
		memoryStream.Position = 0L;
		XElement overridesNode = XElement.Load((Stream)memoryStream);
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		valuesDictionary.ApplyOverrides(overridesNode);
		return SubsystemFurnitureBlockBehavior.LoadFurnitureDesigns(subsystemTerrain, valuesDictionary);
	}
}
