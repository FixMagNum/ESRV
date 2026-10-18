using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemCustomClothing : Subsystem
{
	public Dictionary<string, CustomClothingData> m_customClothes = new Dictionary<string, CustomClothingData>();

	public List<CustomClothingData> m_orderedCustomClothes = new List<CustomClothingData>();

	private Dictionary<int, Texture2D> m_cachedTextures = new Dictionary<int, Texture2D>();

	public static SubsystemCustomClothing Instance { get; private set; }

	public void AddCustomClothing(CustomClothingData data)
	{
		if (string.IsNullOrEmpty(data.Id))
		{
			data.Id = Guid.NewGuid().ToString();
		}
		if (!m_customClothes.ContainsKey(data.Id))
		{
			m_customClothes[data.Id] = data;
			m_orderedCustomClothes.Add(data);
			Log.Information($"[CustomClothing] NEW DESIGN ADDED! ID: {data.Id}, Count now: {m_orderedCustomClothes.Count}");
			return;
		}
		m_customClothes[data.Id] = data;
		int designIndex = GetDesignIndex(data.Id);
		if (m_cachedTextures.ContainsKey(designIndex))
		{
			m_cachedTextures[designIndex].Dispose();
			m_cachedTextures.Remove(designIndex);
		}
		Log.Information("[CustomClothing] Design '" + data.Id + "' updated.");
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		Instance = this;
		m_customClothes.Clear();
		m_orderedCustomClothes.Clear();
		m_cachedTextures.Clear();
		ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("CustomClothes", null);
		if (value == null)
		{
			return;
		}
		Log.Information($"[CustomClothing] Loading world data. Found {value.Count} designs.");
		foreach (object value2 in value.Values)
		{
			try
			{
				ValuesDictionary valuesDictionary2 = (ValuesDictionary)value2;
				CustomClothingData customClothingData = new CustomClothingData
				{
					Id = valuesDictionary2.GetValue<string>("Id"),
					ClothingIndex = valuesDictionary2.GetValue<int>("ClothingIndex"),
					TextureName = valuesDictionary2.GetValue<string>("TextureName")
				};
				m_customClothes[customClothingData.Id] = customClothingData;
				m_orderedCustomClothes.Add(customClothingData);
			}
			catch (Exception ex)
			{
				Log.Error("[CustomClothing] Load entry failed: " + ex.Message);
			}
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("CustomClothes", valuesDictionary2);
		int num = 0;
		foreach (KeyValuePair<string, CustomClothingData> customClothe in m_customClothes)
		{
			ValuesDictionary valuesDictionary3 = new ValuesDictionary();
			valuesDictionary3.SetValue("Id", customClothe.Value.Id);
			valuesDictionary3.SetValue("ClothingIndex", customClothe.Value.ClothingIndex);
			valuesDictionary3.SetValue("TextureName", customClothe.Value.TextureName);
			valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
		}
	}

	private string PackTextureData(Color[] data)
	{
		return "";
	}

	private Color[] UnpackTextureData(string str, int width, int height)
	{
		return null;
	}

	public int GetDesignIndex(string id)
	{
		for (int i = 0; i < m_orderedCustomClothes.Count; i++)
		{
			if (m_orderedCustomClothes[i].Id == id)
			{
				return i;
			}
		}
		return -1;
	}

	public CustomClothingData GetCustomClothingByIndex(int index)
	{
		if (index >= 0 && index < m_orderedCustomClothes.Count)
		{
			return m_orderedCustomClothes[index];
		}
		return null;
	}

	public override void Dispose()
	{
		foreach (Texture2D value in m_cachedTextures.Values)
		{
			value.Dispose();
		}
		m_cachedTextures.Clear();
		Instance = null;
	}

	public Texture2D GetOrCreateTexture(int index)
	{
		if (m_cachedTextures.TryGetValue(index, out var value))
		{
			return value;
		}
		CustomClothingData customClothingByIndex = GetCustomClothingByIndex(index);
		if (customClothingByIndex != null && !string.IsNullOrEmpty(customClothingByIndex.TextureName))
		{
			string path = Storage.CombinePaths("app:/LoomDesigns", customClothingByIndex.TextureName);
			if (Storage.FileExists(path))
			{
				try
				{
					using Stream stream = Storage.OpenFile(path, OpenFileMode.Read);
					Texture2D texture2D = Texture2D.Load(stream);
					m_cachedTextures[index] = texture2D;
					Log.Information("[CustomClothing] Texture loaded from: " + customClothingByIndex.TextureName);
					return texture2D;
				}
				catch (Exception ex)
				{
					Log.Error("[CustomClothing] Load error: " + ex.Message);
				}
			}
		}
		return null;
	}
}
