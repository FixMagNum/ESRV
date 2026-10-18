using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Engine;
using Engine.Media;
using Game.IContentReader;

namespace Game;

public class SurvivalCraftModEntity : ModEntity
{
	public override bool IsSystemMod => true;

	public SurvivalCraftModEntity()
	{
		AddReader(new BitmapFontReader());
		AddReader(new DaeModelReader());
		AddReader(new ImageReader());
		AddReader(new JsonArrayReader());
		AddReader(new JsonObjectReader());
		AddReader(new JsonModelReader());
		AddReader(new MtllibStructReader());
		AddReader(new ObjModelReader());
		AddReader(new ShaderReader());
		AddReader(new SoundBufferReader());
		AddReader(new StreamingSourceReader());
		AddReader(new Game.IContentReader.StringReader());
		AddReader(new SubtextureReader());
		AddReader(new Texture2DReader());
		AddReader(new XmlReader());
		string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content.elh");
		Stream stream = null;
		if (File.Exists(path))
		{
			stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}
		else
		{
			try
			{
				stream = Storage.OpenFile("app:Content.elh", OpenFileMode.Read);
			}
			catch
			{
				throw new Exception("CRITICAL ERROR: Content.elh not found!");
			}
		}
		Stream decodedStream = GetDecodedStream(stream);
		stream?.Dispose();
		ResourcesMd5 = "";
		ModArchive = ZipArchive.Open(decodedStream, keepStreamOpen: true);
		InitResources();
		LabelWidget.BitmapFont = ContentManager.Get<BitmapFont>("Fonts/Pericles");
		LoadingScreen.Info("Ресурсы загружены: " + (modInfo?.Name ?? "Ellihum"));
	}

	public override void LoadBlocksData()
	{
		LoadingScreen.Info("加载方块数据:" + modInfo?.Name);
		BlocksManager.LoadBlocksData(ContentManager.Get<string>("BlocksData"));
		ContentManager.Dispose("BlocksData");
	}

	public override void LoadDll()
	{
		List<Type> flag = new List<Type>();
		Type[] flag2 = typeof(BlocksManager).Assembly.GetTypes();
		for (int season = 0; season < flag2.Length; season++)
		{
			Type text = flag2[season];
			if (text.IsSubclassOf(typeof(ModLoader)) && !text.IsAbstract)
			{
				ModLoader text2 = Activator.CreateInstance(flag2[season]) as ModLoader;
				text2.Entity = this;
				text2.__ModInitialize();
				Loader = text2;
				ModsManager.ModLoaders.Add(text2);
			}
			if (text.IsSubclassOf(typeof(Block)) && !text.IsAbstract)
			{
				flag.Add(text);
			}
		}
		for (int i = 0; i < flag.Count; i++)
		{
			Type type = flag[i];
			FieldInfo fieldInfo = type.GetRuntimeFields().FirstOrDefault((FieldInfo p) => p.Name == "Index" && p.IsPublic && p.IsStatic);
			if (fieldInfo == null || fieldInfo.FieldType != typeof(int))
			{
				ModsManager.AddException(new InvalidOperationException("Block type \"" + type.FullName + "\" does not have static field Index of type int."));
				continue;
			}
			int blockIndex = (int)fieldInfo.GetValue(null);
			Block block = (Block)Activator.CreateInstance(type.GetTypeInfo().AsType());
			block.BlockIndex = blockIndex;
			Blocks.Add(block);
		}
	}

	public override void LoadXdb(ref XElement xElement)
	{
		LoadingScreen.Info("加载数据库:" + modInfo?.Name);
		xElement = ContentManager.Get<XElement>("Database");
		ContentManager.Dispose("Database");
	}

	public override void LoadCr(ref XElement xElement)
	{
		LoadingScreen.Info("加载合成谱:" + modInfo?.Name);
		xElement = ContentManager.Get<XElement>("CraftingRecipes");
		ContentManager.Dispose("CraftingRecipes");
	}

	public override void LoadClo(ClothingBlock block, ref XElement xElement)
	{
		LoadingScreen.Info("加载衣物数据:" + modInfo?.Name);
		xElement = ContentManager.Get<XElement>("Clothes");
		ContentManager.Dispose("Clothes");
	}

	public override void SaveSettings(XElement xElement)
	{
	}

	public override void LoadSettings(XElement xElement)
	{
	}

	public override void OnBlocksInitalized()
	{
		BlocksManager.AddCategory("Terrain");
		BlocksManager.AddCategory("Plants");
		BlocksManager.AddCategory("Construction");
		BlocksManager.AddCategory("Items");
		BlocksManager.AddCategory("Tools");
		BlocksManager.AddCategory("Weapons");
		BlocksManager.AddCategory("Clothes");
		BlocksManager.AddCategory("Electrics");
		BlocksManager.AddCategory("Food");
		BlocksManager.AddCategory("Spawner Eggs");
		BlocksManager.AddCategory("Painted");
		BlocksManager.AddCategory("Dyed");
		BlocksManager.AddCategory("Fireworks");
	}

	private void AddReader(object reader)
	{
		if (reader is Game.IContentReader.IContentReader text && !ContentManager.ReaderList.ContainsKey(text.Type))
		{
			ContentManager.ReaderList.Add(text.Type, text);
		}
	}

	private Stream GetDecodedStream(Stream sourceStream)
	{
		byte[] array;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			sourceStream.CopyTo(memoryStream);
			array = memoryStream.ToArray();
		}
		string s = "TheFatDuck";
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		if (array.Length < bytes.Length)
		{
			throw new InvalidOperationException("Invalid ELH file: too short.");
		}
		for (int i = 0; i < bytes.Length; i++)
		{
			if (array[i] != bytes[i])
			{
				throw new InvalidOperationException("Invalid ELH signature.");
			}
		}
		int num = 64;
		int num2 = 90;
		for (int j = bytes.Length; j < array.Length; j++)
		{
			int num3 = (j - bytes.Length) / num;
			int num4 = (num2 + num3 * 31) ^ 0xA7;
			array[j] ^= (byte)num4;
		}
		for (int k = bytes.Length; k < array.Length; k += num)
		{
			int num5 = k;
			int num6 = Math.Min(k + num - 1, array.Length - 1);
			while (num5 < num6)
			{
				byte b = array[num5];
				array[num5] = array[num6];
				array[num6] = b;
				num5++;
				num6--;
			}
		}
		return new MemoryStream(array, bytes.Length, array.Length - bytes.Length);
	}
}
