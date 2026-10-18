using System;
using System.Collections.Generic;
using System.IO;
using SimpleJson;

namespace Game;

public static class LanguageControl
{
	public static JsonObject KeyWords = new JsonObject();

	public static string Ok = null;

	public static string Cancel = null;

	public static string None = null;

	public static string Nothing = null;

	public static string Error = null;

	public static string On = null;

	public static string Off = null;

	public static string Disable = null;

	public static string Enable = null;

	public static string Warning = null;

	public static string Back = null;

	public static string Allowed = null;

	public static string NAllowed = null;

	public static string Unknown = null;

	public static string Yes = null;

	public static string No = null;

	public static string Unavailable = null;

	public static string Exists = null;

	public static string Success = null;

	public static string Delete = null;

	public static List<string> LanguageTypes = new List<string>();

	public static void Initialize(string languageType)
	{
		Ok = null;
		Cancel = null;
		None = null;
		Nothing = null;
		Error = null;
		On = null;
		Off = null;
		Disable = null;
		Enable = null;
		Warning = null;
		Back = null;
		Allowed = null;
		NAllowed = null;
		Unknown = null;
		Yes = null;
		No = null;
		Unavailable = null;
		Exists = null;
		Success = null;
		Delete = null;
		KeyWords.Clear();
		ModsManager.SetConfig("Language", languageType);
	}

	public static void loadJson(Stream stream)
	{
		string text = new StreamReader(stream).ReadToEnd();
		if (text.Length > 0)
		{
			object obj = global::SimpleJson.SimpleJson.DeserializeObject(text);
			loadJsonLogic(KeyWords, obj);
		}
		if (Ok == null)
		{
			Ok = Get("Usual", "ok");
		}
		if (Cancel == null)
		{
			Cancel = Get("Usual", "cancel");
		}
		if (None == null)
		{
			None = Get("Usual", "none");
		}
		if (Nothing == null)
		{
			Nothing = Get("Usual", "nothing");
		}
		if (Error == null)
		{
			Error = Get("Usual", "error");
		}
		if (On == null)
		{
			On = Get("Usual", "on");
		}
		if (Off == null)
		{
			Off = Get("Usual", "off");
		}
		if (Disable == null)
		{
			Disable = Get("Usual", "disable");
		}
		if (Enable == null)
		{
			Enable = Get("Usual", "enable");
		}
		if (Warning == null)
		{
			Warning = Get("Usual", "warning");
		}
		if (Back == null)
		{
			Back = Get("Usual", "back");
		}
		if (Allowed == null)
		{
			Allowed = Get("Usual", "allowed");
		}
		if (NAllowed == null)
		{
			NAllowed = Get("Usual", "not allowed");
		}
		if (Unknown == null)
		{
			Unknown = Get("Usual", "unknown");
		}
		if (Yes == null)
		{
			Yes = Get("Usual", "yes");
		}
		if (No == null)
		{
			No = Get("Usual", "no");
		}
		if (Unavailable == null)
		{
			Unavailable = Get("Usual", "Unavailable");
		}
		if (Exists == null)
		{
			Exists = Get("Usual", "exist");
		}
		if (Success == null)
		{
			Success = Get("Usual", "success");
		}
		if (Delete == null)
		{
			Success = Get("Usual", "delete");
		}
	}

	public static void loadJsonLogic(JsonObject node, object obj)
	{
		if (obj is JsonObject)
		{
			foreach (KeyValuePair<string, object> item in obj as JsonObject)
			{
				if (item.Value is string)
				{
					if (node.ContainsKey(item.Key))
					{
						node[item.Key] = item.Value;
					}
					else
					{
						node.Add(item.Key, item.Value);
					}
				}
				else if (node.ContainsKey(item.Key))
				{
					loadJsonLogic(node[item.Key] as JsonObject, item.Value);
				}
				else
				{
					node.Add(item.Key, item.Value);
				}
			}
			return;
		}
		if (!(obj is JsonArray))
		{
			return;
		}
		JsonArray jsonArray = obj as JsonArray;
		for (int i = 0; i < jsonArray.Count; i++)
		{
			if (jsonArray[i] is string)
			{
				if (node.ContainsKey(i.ToString()))
				{
					node[i.ToString()] = jsonArray[i];
				}
				else
				{
					node.Add(i.ToString(), jsonArray[i]);
				}
				continue;
			}
			JsonObject jsonObject = new JsonObject();
			if (node.ContainsKey(i.ToString()))
			{
				node[i.ToString()] = jsonObject;
			}
			else
			{
				node.Add(i.ToString(), jsonArray[i]);
			}
			loadJsonLogic(jsonObject, jsonArray[i]);
		}
	}

	public static string LName()
	{
		return ModsManager.Configs["Language"];
	}

	public static string Get(string className, int key)
	{
		return Get(className, key.ToString());
	}

	public static string AutoGet(object className, int key)
	{
		return Get(className.GetType().Name, key.ToString());
	}

	public static string GetWorldPalette(int index)
	{
		return Get("WorldPalette", "Colors", index.ToString());
	}

	public static string Get(params string[] key)
	{
		bool r;
		return Get(out r, key);
	}

	public static string Get(out bool r, params string[] key)
	{
		r = false;
		JsonObject jsonObject = KeyWords;
		JsonArray jsonArray = null;
		for (int i = 0; i < key.Length; i++)
		{
			bool flag = false;
			object value;
			if (jsonArray != null)
			{
				int.TryParse(key[i], out var result);
				object obj = jsonArray[result];
				if (obj is JsonObject jsonObject2)
				{
					jsonObject = jsonObject2;
					jsonArray = null;
					flag = true;
				}
				else
				{
					if (!(obj is JsonArray jsonArray2))
					{
						r = true;
						return obj.ToString();
					}
					jsonObject = null;
					jsonArray = jsonArray2;
					flag = true;
				}
			}
			else if (jsonObject.TryGetValue(key[i], out value))
			{
				if (value is JsonObject jsonObject3)
				{
					jsonObject = jsonObject3;
					jsonArray = null;
					flag = true;
				}
				else
				{
					if (!(value is JsonArray jsonArray3))
					{
						r = true;
						return value.ToString();
					}
					jsonObject = null;
					jsonArray = jsonArray3;
					flag = true;
				}
			}
			if (!flag)
			{
				return key[i];
			}
		}
		string text = "";
		foreach (string text2 in key)
		{
			text = text + text2 + ":";
		}
		return text;
	}

	public static string GetBlock(string blockName, string prop)
	{
		TryGetBlock(blockName, prop, out var result);
		return result;
	}

	public static bool TryGetBlock(string blockName, string prop, out string result)
	{
		string[] array = blockName.Split(new char[1] { ':' }, StringSplitOptions.None);
		result = Get(out var r, "Blocks", (array.Length < 2) ? (blockName + ":0") : blockName, prop);
		if (!r)
		{
			result = Get(out r, "Blocks", array[0] + ":0", prop);
		}
		return r;
	}

	public static string GetContentWidgets(string name, string prop)
	{
		return Get("ContentWidgets", name, prop);
	}

	public static string GetContentWidgets(string name, int pos)
	{
		return Get("ContentWidgets", name, pos.ToString());
	}

	public static string GetDatabase(string name, string prop)
	{
		return Get("Database", name, prop);
	}

	public static string GetFireworks(string name, string prop)
	{
		return Get("FireworksBlock", name, prop);
	}
}
