using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Engine;
using Engine.Audio;
using Engine.Graphics;
using Engine.Media;
using Game.IContentReader;
using SimpleJson;

namespace Game;

public static class ContentManager
{
	internal static Dictionary<string, ContentInfo> Resources = new Dictionary<string, ContentInfo>();

	internal static Dictionary<string, Game.IContentReader.IContentReader> ReaderList = new Dictionary<string, Game.IContentReader.IContentReader>();

	internal static Dictionary<string, List<object>> Caches = new Dictionary<string, List<object>>();

	internal static object syncobj = new object();

	public static void Initialize()
	{
		ReaderList.Clear();
		Resources.Clear();
		Caches.Clear();
		Display.DeviceReset += Display_DeviceReset;
	}

	public static T Get<T>(string name, string suffix = null) where T : class
	{
		return Get(typeof(T), name, suffix) as T;
	}

	public static object Get(Type type, string name, string suffix = null)
	{
		lock (syncobj)
		{
			object obj = null;
			string key = ((suffix == null) ? name : (name + "." + suffix));
			if (type == typeof(Subtexture))
			{
				return TextureAtlasManager.GetSubtexture(name);
			}
			if (Caches.TryGetValue(key, out var value))
			{
				obj = value.Find((object f) => f.GetType() == type);
			}
			if (obj != null)
			{
				return obj;
			}
			if (ReaderList.TryGetValue(type.FullName, out var value2))
			{
				List<ContentInfo> list = new List<ContentInfo>();
				string empty = string.Empty;
				if (suffix == null)
				{
					for (int num = 0; num < value2.DefaultSuffix.Length; num++)
					{
						empty = name + "." + value2.DefaultSuffix[num];
						if (Resources.TryGetValue(empty, out var value3))
						{
							list.Add(value3);
						}
					}
				}
				else
				{
					empty = name + suffix;
					if (Resources.TryGetValue(empty, out var value4))
					{
						list.Add(value4);
					}
				}
				if (list.Count == 0)
				{
					throw new Exception("未能找到资源[" + name + "][" + type.FullName + "]");
				}
				obj = value2.Get(list.ToArray());
			}
			if (value == null)
			{
				value = new List<object>();
				Caches.Add(key, value);
			}
			value.Add(obj);
			return obj;
		}
	}

	public static object StreamConvertType(Type type, Stream stream)
	{
		return type.FullName switch
		{
			"SimpleJson.JsonObject" => global::SimpleJson.SimpleJson.DeserializeObject(new StreamReader(stream).ReadToEnd()), 
			"Engine.Media.StreamingSource" => SoundData.Stream(stream), 
			"Engine.Audio.SoundBuffer" => SoundBuffer.Load(stream), 
			"Engine.Graphics.Texture2D" => Texture2D.Load(stream), 
			"System.String" => new StreamReader(stream).ReadToEnd(), 
			"Engine.Media.Image" => Image.Load(stream), 
			"Game.ObjModel" => ObjModelReader.Load(stream), 
			"Game.JsonModel" => JsonModelReader.Load(stream), 
			"System.Xml.Linq.XElement" => XElement.Load(stream), 
			"Engine.Graphics.Model" => Model.Load(stream, keepSourceVertexDataInTags: true), 
			"Game.MtllibStruct" => MtllibStruct.Load(stream), 
			_ => null, 
		};
	}

	public static void Add(ContentInfo contentInfo)
	{
		lock (syncobj)
		{
			if (!Resources.TryGetValue(contentInfo.AbsolutePath, out var _))
			{
				Resources.Add(contentInfo.AbsolutePath, contentInfo);
			}
			else
			{
				Resources[contentInfo.AbsolutePath] = contentInfo;
			}
		}
	}

	public static void Dispose(string name)
	{
		lock (syncobj)
		{
			if (!Caches.TryGetValue(name, out var value))
			{
				return;
			}
			List<object> list = new List<object>();
			foreach (object item in value)
			{
				if (item is IDisposable disposable)
				{
					disposable.Dispose();
				}
				list.Add(item);
			}
			foreach (object item2 in list)
			{
				value.Remove(item2);
			}
		}
	}

	public static bool ContainsKey(string key)
	{
		return Resources.ContainsKey(key);
	}

	public static bool IsContent(object content)
	{
		foreach (List<object> value in Caches.Values)
		{
			foreach (object item in value)
			{
				if (item == content)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void Display_DeviceReset()
	{
		foreach (KeyValuePair<string, List<object>> cache in Caches)
		{
			string key = cache.Key;
			for (int i = 0; i < cache.Value.Count; i++)
			{
				object obj = cache.Value[i];
				if (obj is Texture2D || obj is Model || obj is BitmapFont)
				{
					cache.Value[i] = Get(obj.GetType(), key);
				}
			}
		}
	}

	public static ReadOnlyList<ContentInfo> List()
	{
		return new ReadOnlyList<ContentInfo>(Resources.Values.ToDynamicArray());
	}

	public static ReadOnlyList<ContentInfo> List(string directory)
	{
		List<ContentInfo> list = new List<ContentInfo>();
		if (!directory.EndsWith("/"))
		{
			directory += "/";
		}
		foreach (ContentInfo value in Resources.Values)
		{
			if (value.ContentPath.StartsWith(directory))
			{
				list.Add(value);
			}
		}
		return new ReadOnlyList<ContentInfo>(list);
	}
}
