using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Game.IContentReader;

namespace Game;

public class ModEntity
{
	public string ResourcesMd5 = string.Empty;

	public ModInfo modInfo;

	public Texture2D Icon;

	public ZipArchive ModArchive;

	public Dictionary<string, ZipArchiveEntry> ModFiles = new Dictionary<string, ZipArchiveEntry>();

	public List<Block> Blocks = new List<Block>();

	public bool IsChecked;

	private ModLoader ModLoader_;

	public virtual bool IsSystemMod => false;

	public ModLoader Loader
	{
		get
		{
			return ModLoader_;
		}
		set
		{
			ModLoader_ = value;
		}
	}

	public ModEntity()
	{
	}

	public ModEntity(Stream scmodSteam)
	{
		byte[] coordinates = new byte[scmodSteam.Length];
		scmodSteam.Read(coordinates, 0, (int)scmodSteam.Length);
		scmodSteam.Seek(0L, SeekOrigin.Begin);
		ResourcesMd5 = ModsManager.GetMd5(coordinates);
		ModArchive = ZipArchive.Open(scmodSteam);
		InitResources();
	}

	public virtual void LoadIcon(Stream stream)
	{
		Icon = Texture2D.Load(stream);
		stream.Close();
	}

	public virtual void GetFiles(string extension, Action<string, Stream> action)
	{
		new List<Stream>();
		foreach (ZipArchiveEntry item in ModArchive.ReadCentralDir())
		{
			if (!(Storage.GetExtension(item.FilenameInZip) == extension))
			{
				continue;
			}
			MemoryStream memoryStream = new MemoryStream();
			ModArchive.ExtractFile(item, memoryStream);
			memoryStream.Position = 0L;
			try
			{
				action(item.FilenameInZip, memoryStream);
			}
			catch (Exception ex)
			{
				Log.Error($"GetFile {item.FilenameInZip} Error:{ex.Message}");
			}
			finally
			{
				memoryStream.Dispose();
			}
		}
	}

	public virtual bool GetFile(string filename, Action<Stream> stream)
	{
		if (ModFiles.TryGetValue(filename, out var result))
		{
			using MemoryStream memoryStream = new MemoryStream();
			ModArchive.ExtractFile(result, memoryStream);
			memoryStream.Position = 0L;
			try
			{
				stream?.Invoke(memoryStream);
			}
			catch (Exception ex)
			{
				LoadingScreen.Error("GetFile " + filename + " Error:" + ex.Message);
			}
		}
		return false;
	}

	public virtual bool GetAssetsFile(string filename, Action<Stream> stream)
	{
		return GetFile("Assets/" + filename, stream);
	}

	public virtual void LoadLauguage()
	{
		LoadingScreen.Info("加载语言:" + modInfo?.Name);
		GetAssetsFile("Lang/" + ModsManager.Configs["Language"] + ".json", (Stream stream) =>
		{
			LanguageControl.loadJson(stream);
		});
	}

	public virtual void ModInitialize()
	{
		LoadingScreen.Info("初始化Mod方法:" + modInfo?.Name);
		ModLoader_?.__ModInitialize();
	}

	public virtual void InitResources()
	{
		ModFiles.Clear();
		if (ModArchive == null)
		{
			return;
		}
		foreach (ZipArchiveEntry item in ModArchive.ReadCentralDir())
		{
			if (item.FileSize != 0)
			{
				ModFiles.Add(item.FilenameInZip, item);
			}
		}
		GetFile("modinfo.json", (Stream stream) =>
		{
			modInfo = ModsManager.DeserializeJson<ModInfo>(ModsManager.StreamToString(stream));
		});
		if (modInfo == null)
		{
			return;
		}
		GetFile("icon.png", (Stream stream) =>
		{
			LoadIcon(stream);
		});
		foreach (KeyValuePair<string, ZipArchiveEntry> modFile in ModFiles)
		{
			ZipArchiveEntry value = modFile.Value;
			string filenameInZip = value.FilenameInZip;
			if (!value.IsFilenameUtf8)
			{
				ModsManager.AddException(new Exception("文件名[" + value.FilenameInZip + "]编码不是Utf8，请进行修正，相关Mod[" + modInfo.Name + "]"));
			}
			if (filenameInZip.StartsWith("Assets/"))
			{
				MemoryStream memoryStream = new MemoryStream();
				ContentInfo contentInfo = new ContentInfo(filenameInZip.Substring(7));
				ModArchive.ExtractFile(value, memoryStream);
				contentInfo.SetContentStream(memoryStream);
				ContentManager.Add(contentInfo);
			}
		}
		LoadingScreen.Info("加载资源:" + modInfo?.Name + " 共" + ModFiles.Count + "文件");
	}

	public virtual void LoadBlocksData()
	{
		LoadingScreen.Info("加载方块数据:" + modInfo?.Name);
		GetFiles(".csv", (string filename, Stream stream) =>
		{
			BlocksManager.LoadBlocksData(ModsManager.StreamToString(stream));
		});
	}

	public virtual void LoadXdb(ref XElement xElement)
	{
		XElement element = xElement;
		LoadingScreen.Info("加载数据库:" + modInfo?.Name);
		GetFiles(".xdb", (string filename, Stream stream) =>
		{
			ModsManager.CombineDataBase(element, stream);
		});
		if (Loader != null)
		{
			Loader.OnXdbLoad(xElement);
		}
	}

	public virtual void LoadClo(ClothingBlock block, ref XElement xElement)
	{
		XElement element = xElement;
		LoadingScreen.Info("加载衣物数据:" + modInfo?.Name);
		GetFiles(".clo", (string filename, Stream stream) =>
		{
			ModsManager.CombineClo(element, stream);
		});
	}

	public virtual void LoadCr(ref XElement xElement)
	{
		XElement element = xElement;
		LoadingScreen.Info("加载合成谱:" + modInfo?.Name);
		GetFiles(".cr", (string filename, Stream stream) =>
		{
			ModsManager.CombineCr(element, stream);
		});
	}

	public virtual void LoadDll()
	{
		LoadingScreen.Info("加载程序集:" + modInfo?.PackageName);
		GetFiles(".dll", (string filename, Stream stream) =>
		{
			LoadDllLogic(stream);
		});
	}

	public void LoadDllLogic(Stream stream)
	{
		Assembly assembly = Assembly.Load(ModsManager.StreamToBytes(stream));
		List<Type> list = new List<Type>();
		Type[] types = assembly.GetTypes();
		for (int i = 0; i < types.Length; i++)
		{
			Type type = types[i];
			if (type.IsSubclassOf(typeof(ModLoader)) && !type.IsAbstract)
			{
				ModLoader modLoader = Activator.CreateInstance(types[i]) as ModLoader;
				modLoader.Entity = this;
				Loader = modLoader;
				modLoader.__ModInitialize();
				ModsManager.ModLoaders.Add(modLoader);
			}
			if (type.IsSubclassOf(typeof(Game.IContentReader.IContentReader)) && !type.IsAbstract)
			{
				Game.IContentReader.IContentReader contentReader = Activator.CreateInstance(type) as Game.IContentReader.IContentReader;
				if (!ContentManager.ReaderList.ContainsKey(contentReader.Type))
				{
					ContentManager.ReaderList.Add(contentReader.Type, contentReader);
				}
			}
			if (type.IsSubclassOf(typeof(Block)) && !type.IsAbstract)
			{
				list.Add(type);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			Type type2 = list[j];
			FieldInfo fieldInfo = type2.GetRuntimeFields().FirstOrDefault((FieldInfo p) => p.Name == "Index" && p.IsPublic && p.IsStatic);
			if (fieldInfo == null || fieldInfo.FieldType != typeof(int))
			{
				LoadingScreen.Warning("Block type \"" + type2.FullName + "\" does not have static field Index of type int.");
				continue;
			}
			int blockIndex = (int)fieldInfo.GetValue(null);
			Block block = (Block)Activator.CreateInstance(type2.GetTypeInfo().AsType());
			block.BlockIndex = blockIndex;
			Blocks.Add(block);
		}
	}

	public virtual void CheckDependencies()
	{
		LoadingScreen.Info("检查依赖项:" + modInfo?.PackageName);
		for (int i = 0; i < modInfo.Dependencies.Count; i++)
		{
			int index = i;
			string text = modInfo.Dependencies[index];
			string dn = "";
			Version dnversion = new Version();
			if (text.Contains(":"))
			{
				string[] array = text.Split(':');
				if (array.Length == 2)
				{
					dn = array[0];
					dnversion = new Version(array[1]);
				}
			}
			else
			{
				dn = text;
			}
			ModEntity modEntity = ModsManager.ModList.Find((ModEntity px) => px.modInfo.PackageName == dn && new Version(px.modInfo.Version) == dnversion);
			if (modEntity != null)
			{
				modEntity.CheckDependencies();
				IsChecked = true;
				continue;
			}
			ModsManager.AddException(new Exception("[" + modInfo.Name + "]缺少依赖项" + text));
			break;
		}
	}

	public virtual void SaveSettings(XElement xElement)
	{
		if (Loader != null)
		{
			Loader.SaveSettings(xElement);
		}
	}

	public virtual void LoadSettings(XElement xElement)
	{
		if (Loader != null)
		{
			Loader.LoadSettings(xElement);
		}
	}

	public virtual void OnBlocksInitalized()
	{
		if (Loader != null)
		{
			Loader.BlocksInitalized();
		}
	}

	public virtual void Dispose()
	{
		try
		{
			if (Loader != null)
			{
				Loader.ModDispose();
			}
		}
		catch
		{
		}
		ModArchive?.ZipFileStream.Close();
	}
}
