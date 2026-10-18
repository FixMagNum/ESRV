using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Engine;
using Game;
using SimpleJson;
using XmlUtilities;

public static class ModsManager
{
	public class ModSettings
	{
		public string languageType;
	}

	public class ModHook
	{
		public string HookName;

		public Dictionary<ModLoader, bool> Loaders = new Dictionary<ModLoader, bool>();

		public Dictionary<ModLoader, string> DisableReason = new Dictionary<ModLoader, string>();

		public ModHook(string name)
		{
			HookName = name;
		}

		public void Add(ModLoader modLoader)
		{
			if (!Loaders.TryGetValue(modLoader, out var _))
			{
				Loaders.Add(modLoader, value: true);
			}
		}

		public void Remove(ModLoader modLoader)
		{
			if (Loaders.TryGetValue(modLoader, out var _))
			{
				Loaders.Remove(modLoader);
			}
		}

		public void Disable(ModLoader from, ModLoader toDisable, string reason)
		{
			if (Loaders.TryGetValue(toDisable, out var _) && !DisableReason.TryGetValue(from, out var _))
			{
				DisableReason.Add(from, reason);
			}
		}
	}

	public const string APIVersion = "1.42";

	public const string SCVersion = "2.3.0.0";

	public const int Apiv = 3;

	public const string ExternelPath = "app:";

	public const string UserDataPath = "app:/UserId.dat";

	public const string CharacterSkinsDirectoryName = "app:/CharacterSkins";

	public const string FurniturePacksDirectoryName = "app:/FurniturePacks";

	public const string BlockTexturesDirectoryName = "app:/TexturePacks";

	public const string WorldsDirectoryName = "app:/Worlds";

	public const string CommunityContentCachePath = "app:CommunityContentCache.xml";

	public const string ModsSetPath = "app:/ModSettings.xml";

	public const string SettingPath = "app:/Settings.xml";

	public const string ModCachePath = "app:/ModsCache";

	public const string LogPath = "app:/Bugs";

	public const bool IsAndroid = false;

	public static string ModsPath = "app:/NetMods";

	public static string path;

	internal static ModEntity SurvivalCraftModEntity;

	internal static bool ConfigLoaded = false;

	internal static List<NetPlayScreen.Connect> Connects = new List<NetPlayScreen.Connect>();

	private static bool AllowContinue = true;

	public static Dictionary<string, string> Configs = new Dictionary<string, string>();

	public static List<ModEntity> ModListAll = new List<ModEntity>();

	public static List<ModEntity> ModList = new List<ModEntity>();

	public static List<ModLoader> ModLoaders = new List<ModLoader>();

	public static List<ModInfo> DisabledMods = new List<ModInfo>();

	public static Dictionary<string, ModHook> ModHooks = new Dictionary<string, ModHook>();

	public static bool GetModEntity(string packagename, out ModEntity modEntity)
	{
		modEntity = ModList.Find((ModEntity px) => px.modInfo.PackageName == packagename);
		return modEntity != null;
	}

	public static bool GetAllowContinue()
	{
		return AllowContinue;
	}

	internal static void Reboot()
	{
		SettingsManager.SaveSettings();
		SettingsManager.LoadSettings();
		foreach (ModEntity mod in ModList)
		{
			mod.Dispose();
		}
		ScreensManager.SwitchScreen("Loading");
	}

	public static void HookAction(string HookName, Func<ModLoader, bool> action)
	{
		if (!ModHooks.TryGetValue(HookName, out var value))
		{
			return;
		}
		foreach (ModLoader key in value.Loaders.Keys)
		{
			if (action(key))
			{
				break;
			}
		}
	}

	public static void RegisterHook(string HookName, ModLoader modLoader)
	{
		if (!ModHooks.TryGetValue(HookName, out var value))
		{
			value = new ModHook(HookName);
			ModHooks.Add(HookName, value);
		}
		value.Add(modLoader);
	}

	public static void DisableHook(ModLoader from, string HookName, string packageName, string reason)
	{
		ModEntity modEntity = ModList.Find((ModEntity p) => p.modInfo.PackageName == packageName);
		if (ModHooks.TryGetValue(HookName, out var value))
		{
			value.Disable(from, modEntity.Loader, reason);
		}
	}

	public static T GetInPakOrStorageFile<T>(string filepath, string suffix = ".txt") where T : class
	{
		return ContentManager.Get<T>(filepath, suffix);
	}

	public static T DeserializeJson<T>(string text) where T : class
	{
		JsonObject jsonObject = (JsonObject)global::SimpleJson.SimpleJson.DeserializeObject(text, typeof(JsonObject));
		T val = Activator.CreateInstance(typeof(T)) as T;
		Type type = val.GetType();
		foreach (KeyValuePair<string, object> item in jsonObject)
		{
			FieldInfo field = type.GetField(item.Key, BindingFlags.Instance | BindingFlags.Public);
			if (field == null)
			{
				continue;
			}
			if (item.Value is JsonArray)
			{
				JsonArray obj = item.Value as JsonArray;
				Type[] genericArguments = field.FieldType.GetGenericArguments();
				object obj2 = Activator.CreateInstance(typeof(List<>).MakeGenericType(genericArguments));
				foreach (object item2 in obj)
				{
					MethodInfo method = obj2.GetType().GetMethod("Add");
					if (genericArguments.Length != 1)
					{
						continue;
					}
					Type type2 = genericArguments[0];
					switch (type2.Name.ToLower())
					{
					case "int32":
					{
						int.TryParse(item2.ToString(), out var result3);
						method.Invoke(obj2, new object[1] { result3 });
						continue;
					}
					case "int64":
					{
						long.TryParse(item2.ToString(), out var result2);
						method.Invoke(obj2, new object[1] { result2 });
						continue;
					}
					case "single":
					{
						float.TryParse(item2.ToString(), out var result);
						method.Invoke(obj2, new object[1] { result });
						continue;
					}
					case "double":
					{
						double.TryParse(item2.ToString(), out var result5);
						method.Invoke(obj2, new object[1] { result5 });
						continue;
					}
					case "bool":
					{
						bool.TryParse(item2.ToString(), out var result4);
						method.Invoke(obj2, new object[1] { result4 });
						continue;
					}
					}
					if (!(item2 is JsonObject jsonObject2))
					{
						continue;
					}
					object obj3 = Activator.CreateInstance(type2);
					FieldInfo[] fields = type2.GetFields();
					foreach (FieldInfo fieldInfo in fields)
					{
						if (jsonObject2.TryGetValue(fieldInfo.Name, out var value))
						{
							fieldInfo.SetValue(obj3, value);
						}
					}
					method.Invoke(obj2, new object[1] { obj3 });
				}
				if (obj2 != null)
				{
					field.SetValue(val, obj2);
				}
			}
			else
			{
				field.SetValue(val, item.Value);
			}
		}
		return val;
	}

	public static void SaveModSettings(XElement xElement)
	{
		foreach (ModEntity mod in ModList)
		{
			mod.SaveSettings(xElement);
		}
		using Stream stream = Storage.OpenFile("app:/ModSettings.xml", OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(xElement, stream, null, throwOnError: true);
	}

	public static void SaveSettings(XElement xElement)
	{
		XElement xElement2 = new XElement("Configs");
		XElement xElement3 = new XElement("Servers");
		foreach (KeyValuePair<string, string> config in Configs)
		{
			xElement2.SetAttributeValue(config.Key, config.Value);
		}
		foreach (NetPlayScreen.Connect connect in Connects)
		{
			XElement xElement4 = XmlUtils.AddElement(xElement3, "Server");
			xElement4.SetAttributeValue("IP", connect.ip);
			xElement4.SetAttributeValue("Name", connect.name);
			xElement4.SetAttributeValue("Pass", connect.savedPassword);
		}
		xElement.Add(xElement2);
		xElement.Add(xElement3);
	}

	public static void LoadSettings(XElement xElement)
	{
		Connects.Clear();
		XElement xElement2 = xElement.Element("Configs");
		XElement xElement3 = xElement.Element("Servers");
		if (xElement2 != null)
		{
			foreach (XAttribute item in xElement2.Attributes())
			{
				if (!Configs.ContainsKey(item.Name.LocalName))
				{
					SetConfig(item.Name.LocalName, item.Value);
				}
			}
			ConfigLoaded = true;
		}
		if (xElement3 == null)
		{
			return;
		}
		foreach (XElement item2 in xElement3.Elements())
		{
			Connects.Add(new NetPlayScreen.Connect
			{
				ip = item2.Attribute("IP").Value,
				name = item2.Attribute("Name").Value,
				savedPassword = item2.Attribute("Pass").Value
			});
		}
	}

	public static void LoadModSettings(XElement xElement)
	{
		foreach (ModEntity mod in ModList)
		{
			mod.LoadSettings(xElement);
		}
	}

	public static void SetConfig(string key, string value)
	{
		Configs[key] = value;
	}

	public static string ImportMod(string name, Stream stream)
	{
		if (!Storage.DirectoryExists("app:/ModsCache"))
		{
			Storage.CreateDirectory("app:/ModsCache");
		}
		string text = Storage.CombinePaths("app:/ModsCache", name + ".scmod");
		int num = 1;
		while (Storage.FileExists(text))
		{
			text = Storage.CombinePaths("app:/ModsCache", name + "(" + num + ").scmod");
			num++;
		}
		using (Stream destination = Storage.OpenFile(text, OpenFileMode.CreateOrOpen))
		{
			stream.CopyTo(destination);
		}
		DialogsManager.ShowDialog(null, new MessageDialog("Mod下载成功", "请到Mod管理器中进行手动安装，是否跳转", "前往", "返回", (MessageDialogButton result) =>
		{
			if (result == MessageDialogButton.Button1)
			{
				ScreensManager.SwitchScreen("ModsManageContent");
			}
		}));
		return "Mod下载成功";
	}

	public static void ModListAllDo(Action<ModEntity> entity)
	{
		for (int i = 0; i < ModList.Count; i++)
		{
			entity?.Invoke(ModList[i]);
		}
	}

	public static void Initialize()
	{
		if (!Storage.DirectoryExists(ModsPath))
		{
			Storage.CreateDirectory(ModsPath);
		}
		ModHooks.Clear();
		ModListAll.Clear();
		ModList.Clear();
		ModLoaders.Clear();
		SurvivalCraftModEntity = new SurvivalCraftModEntity();
		ModEntity listItem = new FastDebugModEntity();
		ModList.Add(SurvivalCraftModEntity);
		ModList.Add(listItem);
		if (SettingsManager.EnableMod)
		{
			GetScmods(ModsPath);
		}
		ModListAll.AddRange(ModList);
		List<ModInfo> containerWidget = new List<ModInfo>();
		containerWidget.AddRange(DisabledMods);
		DisabledMods.Clear();
		List<ModEntity> node2 = new List<ModEntity>();
		foreach (ModEntity labelWidget in ModList)
		{
			ModInfo modInfo = labelWidget.modInfo;
			ModInfo texture = containerWidget.Find((ModInfo l) => l.PackageName == modInfo.PackageName);
			if (texture != null && texture.PackageName != SurvivalCraftModEntity.modInfo.PackageName && texture.PackageName != listItem.modInfo.PackageName)
			{
				containerWidget.Add(modInfo);
				node2.Add(labelWidget);
			}
			else if (!labelWidget.IsChecked)
			{
				if (ModList.FindAll((ModEntity px) => px.modInfo.PackageName == modInfo.PackageName).Count > 1)
				{
					AddException(new Exception("Multiple installed [" + modInfo.PackageName + "]"));
				}
				labelWidget.IsChecked = true;
			}
		}
		DisabledMods.Clear();
		foreach (ModInfo playerModelWidget in containerWidget)
		{
			DisabledMods.Add(playerModelWidget);
		}
		foreach (ModEntity labelWidget5 in node2)
		{
			ModList.Remove(labelWidget5);
		}
	}

	public static void AddException(Exception e, bool AllowContinue_ = false)
	{
		LoadingScreen.Error(e.Message);
		AllowContinue = !SettingsManager.DisplayLog || AllowContinue_;
	}

	public static void GetScmods(string path)
	{
		foreach (string path2 in Storage.ListFileNames(path))
		{
			string extension = Storage.GetExtension(path2);
			using Stream scmodSteam = Storage.OpenFile(Storage.CombinePaths(path, path2), OpenFileMode.Read);
			try
			{
				if (extension == ".scmod")
				{
					ModEntity text = new ModEntity(scmodSteam);
					if (text.modInfo != null && !string.IsNullOrEmpty(text.modInfo.PackageName))
					{
						ModList.Add(text);
					}
				}
			}
			catch (Exception e)
			{
				AddException(e);
			}
		}
		foreach (string stream in Storage.ListDirectoryNames(path))
		{
			GetScmods(Storage.CombinePaths(path, stream));
		}
	}

	public static string StreamToString(Stream stream)
	{
		stream.Seek(0L, SeekOrigin.Begin);
		return new StreamReader(stream).ReadToEnd();
	}

	public static byte[] StreamToBytes(Stream stream)
	{
		byte[] array = new byte[stream.Length];
		stream.Seek(0L, SeekOrigin.Begin);
		stream.Read(array, 0, array.Length);
		return array;
	}

	public static string GetMd5(string input)
	{
		return GetMd5(Encoding.Default.GetBytes(input));
	}

	public static string GetMd5(byte[] input)
	{
		byte[] array = MD5.Create().ComputeHash(input);
		StringBuilder installMod = new StringBuilder();
		for (int uninstallMod = 0; uninstallMod < array.Length; uninstallMod++)
		{
			installMod.Append(array[uninstallMod].ToString("x2"));
		}
		return installMod.ToString();
	}

	public static bool FindElement(XElement xElement, Func<XElement, bool> func, out XElement elementout)
	{
		foreach (XElement item in xElement.Elements())
		{
			if (func(item))
			{
				elementout = item;
				return true;
			}
			if (FindElement(item, func, out var extension))
			{
				elementout = extension;
				return true;
			}
		}
		elementout = null;
		return false;
	}

	public static bool FindElementByGuid(XElement xElement, string guid, out XElement elementout)
	{
		foreach (XElement item in xElement.Elements())
		{
			foreach (XAttribute item2 in item.Attributes())
			{
				if (item2.Name.ToString() == "Guid" && item2.Value == guid)
				{
					elementout = item;
					return true;
				}
			}
			if (FindElementByGuid(item, guid, out var elementout2))
			{
				elementout = elementout2;
				return true;
			}
		}
		elementout = null;
		return false;
	}

	public static bool HasAttribute(XElement element, Func<string, bool> func, out XAttribute xAttributeout)
	{
		foreach (XAttribute item in element.Attributes())
		{
			if (func(item.Name.LocalName))
			{
				xAttributeout = item;
				return true;
			}
		}
		xAttributeout = null;
		return false;
	}

	public static void CombineClo(XElement xElement, Stream cloorcr)
	{
		XElement num = XmlUtils.LoadXmlFromStream(cloorcr, Encoding.UTF8, throwOnError: true);
		foreach (XElement element in num.Elements())
		{
			XAttribute xAttributeout;
			if (HasAttribute(element, (string name) => name.StartsWith("new-"), out var num2))
			{
				if (HasAttribute(element, (string name) => name == "Index", out var xAttribute) && FindElement(xElement, (XElement ele) => element.Attribute("Index").Value == xAttribute.Value, out var elementout))
				{
					string[] array = num2.Name.ToString().Split(new string[1] { "new-" }, StringSplitOptions.RemoveEmptyEntries);
					if (array.Length == 1)
					{
						elementout.SetAttributeValue(array[0], num2.Value);
					}
				}
			}
			else if (HasAttribute(element, (string name) => name.StartsWith("r-"), out xAttributeout))
			{
				if (HasAttribute(element, (string name) => name == "Index", out var xAttribute2) && FindElement(xElement, (XElement ele) => element.Attribute("Index").Value == xAttribute2.Value, out var elementout2))
				{
					elementout2.Remove();
					element.Remove();
				}
			}
			xElement.Add(num);
		}
	}

	public static void CombineCr(XElement xElement, Stream cloorcr)
	{
		XElement modItem = XmlUtils.LoadXmlFromStream(cloorcr, Encoding.UTF8, throwOnError: true);
		CombineCrLogic(xElement, modItem);
	}

	public static void CombineCrLogic(XElement xElement, XElement needCombine)
	{
		foreach (XElement element in needCombine.Elements())
		{
			if (HasAttribute(element, (string name) => name == "Result", out var _))
			{
				XAttribute attribute2;
				if (HasAttribute(element, (string name) => name.StartsWith("new-"), out var attribute))
				{
					string[] array = attribute.Name.ToString().Split(new string[1] { "new-" }, StringSplitOptions.RemoveEmptyEntries);
					if (array.Length == 1)
					{
						_ = array[0];
					}
					if (FindElement(xElement, (XElement ele) =>
					{
						foreach (XAttribute xAttribute in element.Attributes())
						{
							if (!(xAttribute.Name == attribute.Name) && !HasAttribute(ele, (string tname) => tname == xAttribute.Name, out var _))
							{
								return false;
							}
						}
						return true;
					}, out var elementout) && array.Length == 1)
					{
						elementout.SetAttributeValue(array[0], attribute.Value);
						elementout.SetValue(element.Value);
					}
				}
				else if (HasAttribute(element, (string name) => name.StartsWith("r-"), out attribute2))
				{
					if (FindElement(xElement, (XElement ele) =>
					{
						foreach (XAttribute xAttribute in element.Attributes())
						{
							if (!(xAttribute.Name == attribute2.Name) && !HasAttribute(ele, (string tname) => tname == xAttribute.Name, out var _))
							{
								return false;
							}
						}
						return true;
					}, out var elementout2))
					{
						elementout2.Remove();
						element.Remove();
					}
				}
				else
				{
					xElement.Add(element);
				}
			}
			CombineCrLogic(xElement, element);
		}
	}

	public static void Modify(XElement source, XElement change)
	{
		if (FindElement(source, (XElement item) => (item.Name.LocalName == change.Name.LocalName && item.Attribute("Guid") != null && change.Attribute("Guid") != null && item.Attribute("Guid").Value == change.Attribute("Guid").Value) ? true : false, out var elementout))
		{
			foreach (XElement item in change.Elements())
			{
				Modify(elementout, item);
			}
			return;
		}
		if (change.Name.LocalName.StartsWith("Parameter") || change.Name.LocalName == "MemberComponentTemplate")
		{
			if (FindElement(source, (XElement item) => (item.Name.LocalName == change.Name.LocalName && item.Attribute("Name").Name == change.Attribute("Name").Name) ? true : false, out var elementout2))
			{
				Console.WriteLine("重复的参数" + elementout2.Name.LocalName + ":" + elementout2.Attribute("Name").Value + "设置");
			}
			else
			{
				source.Add(change);
			}
		}
		else
		{
			source.Add(change);
		}
	}

	public static void CombineDataBase(XElement DataBaseXml, Stream Xdb)
	{
		XElement xElement = XmlUtils.LoadXmlFromStream(Xdb, Encoding.UTF8, throwOnError: true);
		XElement result = DataBaseXml.Element("DatabaseObjects");
		foreach (XElement item in xElement.Elements())
		{
			if (HasAttribute(item, (string str) => str.Contains("new-"), out var xAttributeout) && HasAttribute(item, (string str) => str == "Guid", out var xAttributeout2) && FindElementByGuid(result, xAttributeout2.Value, out var elementout))
			{
				string[] array2 = xAttributeout.Name.ToString().Split(new string[1] { "new-" }, StringSplitOptions.RemoveEmptyEntries);
				if (array2.Length == 1)
				{
					elementout.SetAttributeValue(array2[0], xAttributeout.Value);
				}
			}
			Modify(result, item);
		}
	}
}
