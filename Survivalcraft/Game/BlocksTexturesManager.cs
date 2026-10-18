using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game.NetWork;

namespace Game;

public static class BlocksTexturesManager
{
	public static List<string> m_blockTextureNames;

	public static Texture2D DefaultBlocksTexture { get; set; }

	public static ReadOnlyList<string> BlockTexturesNames => new ReadOnlyList<string>(m_blockTextureNames);

	public static event Action<string> BlocksTextureDeleted;

	public static void Initialize()
	{
		Storage.CreateDirectory("app:/TexturePacks");
		DefaultBlocksTexture = ContentManager.Get<Texture2D>("Textures/Blocks");
	}

	public static bool IsBuiltIn(string name)
	{
		return string.IsNullOrEmpty(name);
	}

	public static string GetFileName(string name)
	{
		if (IsBuiltIn(name))
		{
			return null;
		}
		return Storage.CombinePaths("app:/TexturePacks", name);
	}

	public static string GetDisplayName(string name)
	{
		if (IsBuiltIn(name))
		{
			return "Echo Ellihuma";
		}
		return Storage.GetFileNameWithoutExtension(name);
	}

	public static DateTime GetCreationDate(string name)
	{
		try
		{
			if (!IsBuiltIn(name))
			{
				return Storage.GetFileLastWriteTime(GetFileName(name));
			}
		}
		catch
		{
		}
		return new DateTime(2000, 1, 1);
	}

	public static Texture2D LoadTexture(string name)
	{
		if (CommonLib.WorkType == WorkType.Client && CommonLib.BlockTexture != null)
		{
			return CommonLib.BlockTexture;
		}
		Texture2D texture2D = null;
		if (!IsBuiltIn(name))
		{
			try
			{
				using Stream stream = Storage.OpenFile(GetFileName(name), OpenFileMode.Read);
				ValidateBlocksTexture(stream);
				stream.Position = 0L;
				texture2D = Texture2D.Load(stream);
			}
			catch (Exception ex)
			{
				Log.Warning("Could not load blocks texture \"" + name + "\". Reason: " + ex.Message + ".");
			}
		}
		if (texture2D == null)
		{
			texture2D = DefaultBlocksTexture;
		}
		return texture2D;
	}

	public static string ImportBlocksTexture(string name, Stream stream)
	{
		Exception ex = ExternalContentManager.VerifyExternalContentName(name);
		if (ex != null)
		{
			throw ex;
		}
		if (Storage.GetExtension(name) != ".scbtex")
		{
			name += ".scbtex";
		}
		ValidateBlocksTexture(stream);
		stream.Position = 0L;
		using Stream destination = Storage.OpenFile(GetFileName(name), OpenFileMode.Create);
		stream.CopyTo(destination);
		return name;
	}

	public static void DeleteBlocksTexture(string name)
	{
		try
		{
			string fileName = GetFileName(name);
			if (!string.IsNullOrEmpty(fileName))
			{
				Storage.DeleteFile(fileName);
				BlocksTextureDeleted?.Invoke(name);
			}
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser("Unable to delete blocks texture \"" + name + "\"", e);
		}
	}

	public static void UpdateBlocksTexturesList()
	{
		m_blockTextureNames.Clear();
		m_blockTextureNames.Add(string.Empty);
		foreach (string item in Storage.ListFileNames("app:/TexturePacks"))
		{
			m_blockTextureNames.Add(item);
		}
	}

	public static void ValidateBlocksTexture(Stream stream)
	{
		Image image = Image.Load(stream);
		if (image.Width > 65535 || image.Height > 65535)
		{
			throw new InvalidOperationException($"Blocks texture is larger than 65535x65535 pixels (size={image.Width}x{image.Height})");
		}
		if (!MathUtils.IsPowerOf2(image.Width) || !MathUtils.IsPowerOf2(image.Height))
		{
			throw new InvalidOperationException($"Blocks texture does not have power-of-two size (size={image.Width}x{image.Height})");
		}
	}

	static BlocksTexturesManager()
	{
		m_blockTextureNames = new List<string>();
	}
}
