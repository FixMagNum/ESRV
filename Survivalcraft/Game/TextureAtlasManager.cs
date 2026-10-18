using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public static class TextureAtlasManager
{
	public static Dictionary<string, Subtexture> m_subtextures = new Dictionary<string, Subtexture>();

	public static Texture2D AtlasTexture;

	public static void Clear()
	{
		m_subtextures.Clear();
	}

	public static void Initialize()
	{
		Texture2D atlasTexture_ = ContentManager.Get<Texture2D>("Atlases/AtlasTexture");
		string face = ContentManager.Get<string>("Atlases/Atlas");
		LoadAtlases(atlasTexture_, face);
	}

	public static void LoadAtlases(Texture2D AtlasTexture_, string Atlas)
	{
		Clear();
		AtlasTexture = AtlasTexture_;
		LoadTextureAtlas(AtlasTexture, Atlas, "Textures/Atlas/");
	}

	public static Subtexture GetSubtexture(string name)
	{
		if (!m_subtextures.TryGetValue(name, out var value))
		{
			try
			{
				value = new Subtexture(ContentManager.Get(typeof(Texture2D), name) as Texture2D, Vector2.Zero, Vector2.One);
				m_subtextures.Add(name, value);
				return value;
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException("Required subtexture " + name + " not found in TextureAtlasManager.", innerException);
			}
		}
		return value;
	}

	public static void LoadTextureAtlas(Texture2D texture, string atlasDefinition, string prefix)
	{
		string[] flag = atlasDefinition.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		int rotation = 0;
		while (true)
		{
			if (rotation < flag.Length)
			{
				string[] array = flag[rotation].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length < 5)
				{
					break;
				}
				string point = prefix + array[0];
				int point2 = int.Parse(array[1], CultureInfo.InvariantCulture);
				int data2 = int.Parse(array[2], CultureInfo.InvariantCulture);
				int num = int.Parse(array[3], CultureInfo.InvariantCulture);
				int num2 = int.Parse(array[4], CultureInfo.InvariantCulture);
				Vector2 topLeft = new Vector2((float)point2 / (float)texture.Width, (float)data2 / (float)texture.Height);
				Vector2 bottomRight = new Vector2((float)(point2 + num) / (float)texture.Width, (float)(data2 + num2) / (float)texture.Height);
				Subtexture value = new Subtexture(texture, topLeft, bottomRight);
				m_subtextures.Add(point, value);
				rotation++;
				continue;
			}
			return;
		}
		throw new InvalidOperationException("Invalid texture atlas definition.");
	}
}
