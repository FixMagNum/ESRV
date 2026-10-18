using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public class TextureAtlas
{
	public Texture2D m_texture;

	public Dictionary<string, Engine.Rectangle> m_rectangles = new Dictionary<string, Engine.Rectangle>();

	public Texture2D Texture => m_texture;

	public TextureAtlas(Texture2D texture, string atlasDefinition, string prefix)
	{
		m_texture = texture;
		string[] value = atlasDefinition.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		int slotIndex = 0;
		while (true)
		{
			if (slotIndex < value.Length)
			{
				string[] array = value[slotIndex].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length < 5)
				{
					break;
				}
				string key = prefix + array[0];
				Engine.Rectangle value2 = new Engine.Rectangle
				{
					Left = int.Parse(array[1], CultureInfo.InvariantCulture),
					Top = int.Parse(array[2], CultureInfo.InvariantCulture),
					Width = int.Parse(array[3], CultureInfo.InvariantCulture),
					Height = int.Parse(array[4], CultureInfo.InvariantCulture)
				};
				m_rectangles.Add(key, value2);
				slotIndex++;
				continue;
			}
			return;
		}
		throw new InvalidOperationException("Invalid texture atlas definition.");
	}

	public bool ContainsTexture(string textureName)
	{
		return m_rectangles.ContainsKey(textureName);
	}

	public Vector4? GetTextureCoordinates(string textureName)
	{
		if (m_rectangles.TryGetValue(textureName, out var value))
		{
			return new Vector4
			{
				X = (float)value.Left / (float)m_texture.Width,
				Y = (float)value.Top / (float)m_texture.Height,
				Z = (float)value.Right / (float)m_texture.Width,
				W = (float)value.Bottom / (float)m_texture.Height
			};
		}
		return null;
	}
}
