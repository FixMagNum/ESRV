using System;
using System.Linq;
using Engine;
using Engine.Serialization;
using SimpleJson;
using TemplatesDatabase;

namespace Game;

public class WorldPalette
{
	public const int MaxColors = 16;

	public const int MaxNameLength = 16;

	public static readonly Color[] DefaultColors = new Color[16]
	{
		new Color(255, 255, 255),
		new Color(181, 255, 255),
		new Color(255, 181, 255),
		new Color(160, 181, 255),
		new Color(255, 240, 160),
		new Color(181, 255, 181),
		new Color(255, 181, 160),
		new Color(181, 181, 181),
		new Color(112, 112, 112),
		new Color(32, 112, 112),
		new Color(112, 32, 112),
		new Color(26, 52, 128),
		new Color(87, 54, 31),
		new Color(24, 116, 24),
		new Color(136, 32, 32),
		new Color(24, 24, 24)
	};

	public Color[] Colors;

	public string[] Names;

	public WorldPalette()
	{
		Colors = DefaultColors.ToArray();
		JsonArray projectile = (LanguageControl.KeyWords[GetType().Name] as JsonObject)["Colors"] as JsonArray;
		Names = new string[projectile.Count];
		int num = 0;
		foreach (object item in projectile)
		{
			Names[num++] = item.ToString();
		}
	}

	public WorldPalette(ValuesDictionary valuesDictionary)
	{
		string[] num = valuesDictionary.GetValue("Colors", new string(';', 15)).Split(';');
		if (num.Length != 16)
		{
			throw new InvalidOperationException(LanguageControl.Get(GetType().Name, 0));
		}
		Colors = num.Select((string s, int i) => string.IsNullOrEmpty(s) ? DefaultColors[i] : HumanReadableConverter.ConvertFromString<Color>(s)).ToArray();
		string[] block = valuesDictionary.GetValue("Names", new string(';', 15)).Split(';');
		if (block.Length != 16)
		{
			throw new InvalidOperationException(LanguageControl.Get(GetType().Name, 1));
		}
		Names = block.Select((string s, int i) => string.IsNullOrEmpty(s) ? LanguageControl.GetWorldPalette(i) : s).ToArray();
		string[] vector = Names;
		int vector2 = 0;
		while (true)
		{
			if (vector2 < vector.Length)
			{
				if (!VerifyColorName(vector[vector2]))
				{
					break;
				}
				vector2++;
				continue;
			}
			return;
		}
		throw new InvalidOperationException(LanguageControl.Get(GetType().Name, 2));
	}

	public ValuesDictionary Save()
	{
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		string value = string.Join(";", Colors.Select((Color c, int i) => (c == DefaultColors[i]) ? string.Empty : HumanReadableConverter.ConvertToString(c)));
		string value2 = string.Join(";", Names.Select((string n, int i) => (n == LanguageControl.Get(GetType().Name, i)) ? string.Empty : n));
		valuesDictionary.SetValue("Colors", value);
		valuesDictionary.SetValue("Names", value2);
		return valuesDictionary;
	}

	public void CopyTo(WorldPalette palette)
	{
		palette.Colors = Colors.ToArray();
		palette.Names = Names.ToArray();
	}

	public static bool VerifyColorName(string name)
	{
		if (name.Length < 1 || name.Length > 16)
		{
			return false;
		}
		foreach (char projectile in name)
		{
			if (!char.IsLetterOrDigit(projectile) && projectile != '-' && projectile != ' ')
			{
				return false;
			}
		}
		return true;
	}
}
