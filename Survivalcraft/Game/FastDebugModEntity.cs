using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Engine;

namespace Game;

public class FastDebugModEntity : ModEntity
{
	public Dictionary<string, FileInfo> FModFiles = new Dictionary<string, FileInfo>();

	public override bool IsSystemMod => true;

	public FastDebugModEntity()
	{
		modInfo = new ModInfo
		{
			Name = "[Debug]",
			PackageName = "debug"
		};
		InitResources();
	}

	public override void InitResources()
	{
		ReadDirResouces(ModsManager.ModsPath, "");
		if (!GetFile("modinfo.json", (Stream stream) =>
		{
			modInfo = ModsManager.DeserializeJson<ModInfo>(ModsManager.StreamToString(stream));
			modInfo.Name = "[Debug]" + modInfo.Name;
		}))
		{
			modInfo = new ModInfo
			{
				Name = "FastDebug",
				Version = "1.0.0",
				ApiVersion = "1.42",
				Author = "Mod",
				Description = "调试Mod插件",
				ScVersion = "2.3.0.0",
				PackageName = "com.fastdebug"
			};
		}
		GetFile("icon.png", (Stream stream) =>
		{
			LoadIcon(stream);
		});
		foreach (KeyValuePair<string, FileInfo> fModFile in FModFiles)
		{
			GetFile(fModFile.Key, (Stream stream) =>
			{
				byte[] array = new byte[stream.Length];
				stream.Read(array, 0, array.Length);
				ResourcesMd5 += ModsManager.GetMd5(array);
			});
		}
	}

	public void ReadDirResouces(string basepath, string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			path = basepath;
		}
		foreach (string item in Storage.ListDirectoryNames(path))
		{
			ReadDirResouces(basepath, path + "/" + item);
		}
		foreach (string item2 in Storage.ListFileNames(path))
		{
			string text = path + "/" + item2;
			string text2 = text.Substring(basepath.Length + 1);
			if (text2.StartsWith("Assets/"))
			{
				ContentInfo contentInfo = new ContentInfo(text2.Substring(7));
				MemoryStream memoryStream = new MemoryStream();
				using Stream stream = Storage.OpenFile(text, OpenFileMode.Read);
				stream.CopyTo(memoryStream);
				contentInfo.SetContentStream(memoryStream);
				ContentManager.Add(contentInfo);
			}
			FModFiles.Add(text2, new FileInfo(Storage.GetSystemPath(text)));
		}
	}

	public override void LoadDll()
	{
		foreach (string item in Storage.ListFileNames(ModsManager.ModsPath))
		{
			if (item.EndsWith(".dll") && !item.StartsWith("EntitySystem") && !item.StartsWith("Engine") && !item.StartsWith("Survivalcraft") && !item.StartsWith("OpenTK"))
			{
				LoadDllLogic(Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, item), OpenFileMode.Read));
				break;
			}
		}
	}

	public override void LoadClo(ClothingBlock block, ref XElement xElement)
	{
		foreach (string num in Storage.ListFileNames(ModsManager.ModsPath))
		{
			if (num.EndsWith(".clo"))
			{
				ModsManager.CombineClo(xElement, Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, num), OpenFileMode.Read));
			}
		}
	}

	public override void LoadCr(ref XElement xElement)
	{
		foreach (string position in Storage.ListFileNames(ModsManager.ModsPath))
		{
			if (position.EndsWith(".cr"))
			{
				ModsManager.CombineCr(xElement, Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, position), OpenFileMode.Read));
			}
		}
	}

	public override void LoadLauguage()
	{
		foreach (string item in Storage.ListFileNames(ModsManager.ModsPath))
		{
			if (item == ModsManager.Configs["Language"] + ".json")
			{
				LanguageControl.loadJson(Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, item), OpenFileMode.Read));
			}
		}
	}

	public override void LoadBlocksData()
	{
		foreach (string item in Storage.ListFileNames(ModsManager.ModsPath))
		{
			if (item.EndsWith(".csv"))
			{
				BlocksManager.LoadBlocksData(ModsManager.StreamToString(Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, item), OpenFileMode.Read)));
			}
		}
	}

	public override void LoadXdb(ref XElement xElement)
	{
		foreach (string cellValue in Storage.ListFileNames(ModsManager.ModsPath))
		{
			if (cellValue.EndsWith(".xdb"))
			{
				ModsManager.CombineDataBase(xElement, Storage.OpenFile(Storage.CombinePaths(ModsManager.ModsPath, cellValue), OpenFileMode.Read));
			}
		}
		Loader?.OnXdbLoad(xElement);
	}

	public override void GetFiles(string extension, Action<string, Stream> action)
	{
		foreach (KeyValuePair<string, FileInfo> fModFile in FModFiles)
		{
			if (!fModFile.Key.EndsWith(extension))
			{
				continue;
			}
			using Stream arg = fModFile.Value.OpenRead();
			try
			{
				action?.Invoke(fModFile.Key, arg);
			}
			catch (Exception ex)
			{
				Log.Error($"GetFile {fModFile.Key} Error:{ex.Message}");
			}
		}
	}

	public override bool GetFile(string filename, Action<Stream> stream)
	{
		if (FModFiles.TryGetValue(filename, out var value))
		{
			using (Stream obj = value.OpenRead())
			{
				try
				{
					stream?.Invoke(obj);
				}
				catch (Exception ex)
				{
					Log.Error($"GetFile {filename} Error:{ex.Message}");
				}
			}
			return true;
		}
		return false;
	}

	public override bool GetAssetsFile(string filename, Action<Stream> stream)
	{
		return GetFile("Assets/" + filename, stream);
	}
}
