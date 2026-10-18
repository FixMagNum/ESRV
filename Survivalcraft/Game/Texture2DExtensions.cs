using System;
using Engine;
using Engine.Graphics;

namespace Game;

public static class Texture2DExtensions
{
	public static bool IsDisposed(this Texture2D texture)
	{
		if (texture == null)
		{
			return true;
		}
		if (texture.NativeHandle == IntPtr.Zero)
		{
			return true;
		}
		return false;
	}

	public static void GetData<T>(this Texture2D texture, T[] data, int sourceStartIndex = 0) where T : struct
	{
		if (texture == null || data == null)
		{
			return;
		}
		if (texture.Tag is Engine.Media.Image image && typeof(T) == typeof(Color))
		{
			Color[] pixels = image.Pixels;
			int count = MathUtils.Min(pixels.Length, data.Length - sourceStartIndex);
			if (data is Color[] targetColors)
			{
				Array.Copy(pixels, 0, targetColors, sourceStartIndex, count);
				return;
			}
		}
	}
}
