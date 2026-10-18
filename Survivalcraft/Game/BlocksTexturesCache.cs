using System.Collections.Generic;
using Engine.Graphics;

namespace Game;

public class BlocksTexturesCache
{
	public Dictionary<string, Texture2D> m_textures = new Dictionary<string, Texture2D>();

	public Texture2D GetTexture(string name)
	{
		if (!m_textures.TryGetValue(name, out var value))
		{
			value = BlocksTexturesManager.LoadTexture(name);
			m_textures.Add(name, value);
		}
		return value;
	}

	public void Clear()
	{
		foreach (Texture2D value in m_textures.Values)
		{
			if (!ContentManager.IsContent(value))
			{
				value.Dispose();
			}
		}
		m_textures.Clear();
	}
}
