using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.Graphics;
using Engine.Media;
using TemplatesDatabase;

namespace Game;

public static class CharacterSkinsManager
{
	public class NetCharacterSkin
	{
		public Dictionary<string, Image> Cache = new Dictionary<string, Image>();
	}

	public static List<string> m_characterSkinNames;

	public static Dictionary<PlayerClass, Model> m_playerModels;

	public static Dictionary<PlayerClass, Model> m_outerClothingModels;

	public static List<string> waitReplyList;

	public static ReadOnlyList<string> CharacterSkinsNames => new ReadOnlyList<string>(m_characterSkinNames);

	public static event Action<string> CharacterSkinDeleted;

	public static void Initialize()
	{
		Storage.CreateDirectory("app:/CharacterSkins");
	}

	public static bool IsBuiltIn(string name)
	{
		return name.StartsWith("$");
	}

	public static bool HasSkinRes(string name)
	{
		string filename;
		if (!IsBuiltIn(name))
		{
			return GetFileName(name, out filename);
		}
		return true;
	}

	public static void SaveSkinToFile(string name, byte[] skinData)
	{
		using Stream stream = Storage.OpenFile(Path.Combine("app:/CharacterSkins", name), OpenFileMode.CreateOrOpen);
		stream.Write(skinData, 0, skinData.Length);
	}

	public static PlayerClass? GetPlayerClass(string name)
	{
		name = name.ToLower();
		if (name.Contains("female") || name.Contains("girl") || name.Contains("woman"))
		{
			return PlayerClass.Female;
		}
		if (name.Contains("male") || name.Contains("boy") || name.Contains("man"))
		{
			return PlayerClass.Male;
		}
		return null;
	}

	public static bool GetFileName(string name, out string filename)
	{
		filename = null;
		if (IsBuiltIn(name))
		{
			return false;
		}
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		filename = Storage.CombinePaths("app:/CharacterSkins", name);
		return Storage.FileExists(filename);
	}

	public static string GetDisplayName(string name)
	{
		if (!IsBuiltIn(name))
		{
			return Storage.GetFileNameWithoutExtension(name);
		}
		if (name.Contains("Female"))
		{
			if (name.Contains("1"))
			{
				return "Doris";
			}
			if (name.Contains("2"))
			{
				return "Mabel";
			}
			if (name.Contains("3"))
			{
				return "Ada";
			}
			if (name.Contains("4"))
			{
				return "Shirley";
			}
			if (name.Contains("5"))
			{
				return "Dasuke";
			}
		}
		else
		{
			if (name.Contains("1"))
			{
				return "Walter";
			}
			if (name.Contains("2"))
			{
				return "Basil";
			}
			if (name.Contains("3"))
			{
				return "Geoffrey";
			}
			if (name.Contains("4"))
			{
				return "Zachary";
			}
			if (name.Contains("5"))
			{
				return "Danedale";
			}
			if (name.Contains("6"))
			{
				return "Paulfort";
			}
			if (name.Contains("7"))
			{
				return "Anikson";
			}
			if (name.Contains("8"))
			{
				return "Berbley";
			}
			if (name.Contains("9"))
			{
				return "Roger";
			}
		}
		return "Unknown";
	}

	public static DateTime GetCreationDate(string name)
	{
		try
		{
			if (GetFileName(name, out var filename))
			{
				return Storage.GetFileLastWriteTime(filename);
			}
		}
		catch
		{
		}
		return new DateTime(2000, 1, 1);
	}

	public static bool GetChatacterSkinImage(string name, out Texture2D image)
	{
		image = null;
		try
		{
			if (GetFileName(name, out var filename))
			{
				if (!Storage.FileExists(filename))
				{
					return false;
				}
				using Stream stream = Storage.OpenFile(filename, OpenFileMode.Read);
				ValidateCharacterSkin(stream);
				stream.Position = 0L;
				image = Texture2D.Load(stream);
			}
			else
			{
				image = ContentManager.Get<Texture2D>("Textures/Creatures/Human" + name.Substring(1).Replace(" ", ""));
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static Texture2D LoadTexture(string name)
	{
		GetChatacterSkinImage(name, out var image);
		return image;
	}

	public static string ImportCharacterSkin(string name, Stream stream)
	{
		Exception ex = ExternalContentManager.VerifyExternalContentName(name);
		if (ex != null)
		{
			throw ex;
		}
		if (Storage.GetExtension(name) != ".scskin")
		{
			name += ".scskin";
		}
		ValidateCharacterSkin(stream);
		stream.Position = 0L;
		GetFileName(name, out var filename);
		using Stream destination = Storage.OpenFile(filename, OpenFileMode.Create);
		stream.CopyTo(destination);
		return name;
	}

	public static void DeleteCharacterSkin(string name)
	{
		try
		{
			if (GetFileName(name, out var filename))
			{
				Storage.DeleteFile(filename);
				CharacterSkinDeleted?.Invoke(name);
			}
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser("Unable to delete character skin \"" + name + "\"", e);
		}
	}

	public static void UpdateCharacterSkinsList()
	{
		m_characterSkinNames.Clear();
		m_characterSkinNames.Add("$Male1");
		m_characterSkinNames.Add("$Male2");
		m_characterSkinNames.Add("$Male3");
		m_characterSkinNames.Add("$Male4");
		m_characterSkinNames.Add("$Male5");
		m_characterSkinNames.Add("$Male6");
		m_characterSkinNames.Add("$Male7");
		m_characterSkinNames.Add("$Male8");
		m_characterSkinNames.Add("$Male9");
		m_characterSkinNames.Add("$Female1");
		m_characterSkinNames.Add("$Female2");
		m_characterSkinNames.Add("$Female3");
		m_characterSkinNames.Add("$Female4");
		m_characterSkinNames.Add("$Female5");
		foreach (string item in Storage.ListFileNames("app:/CharacterSkins"))
		{
			if (Storage.GetExtension(item).ToLower() == ".scskin")
			{
				m_characterSkinNames.Add(item);
			}
		}
	}

	public static Model GetPlayerModel(PlayerClass playerClass)
	{
		if (!m_playerModels.TryGetValue(playerClass, out var value))
		{
			string value2 = (playerClass switch
			{
				PlayerClass.Female => DatabaseManager.FindEntityValuesDictionary("FemalePlayer", throwIfNotFound: true), 
				PlayerClass.Male => DatabaseManager.FindEntityValuesDictionary("MalePlayer", throwIfNotFound: true), 
				_ => throw new InvalidOperationException("Unknown player class."), 
			}).GetValue<ValuesDictionary>("HumanModel").GetValue<string>("ModelName");
			try
			{
				value = Model.Load(value2, keepSourceVertexDataInTags: true);
			}
			catch
			{
				value = ContentManager.Get<Model>(value2);
			}
			m_playerModels.Add(playerClass, value);
		}
		return value;
	}

	public static Model GetOuterClothingModel(PlayerClass playerClass)
	{
		if (!m_outerClothingModels.TryGetValue(playerClass, out var value))
		{
			string value2 = (playerClass switch
			{
				PlayerClass.Female => DatabaseManager.FindEntityValuesDictionary("FemalePlayer", throwIfNotFound: true), 
				PlayerClass.Male => DatabaseManager.FindEntityValuesDictionary("MalePlayer", throwIfNotFound: true), 
				_ => throw new InvalidOperationException("Unknown player class."), 
			}).GetValue<ValuesDictionary>("OuterClothingModel").GetValue<string>("ModelName");
			try
			{
				value = Model.Load(value2, keepSourceVertexDataInTags: true);
			}
			catch
			{
				value = ContentManager.Get<Model>(value2);
			}
			m_outerClothingModels.Add(playerClass, value);
		}
		return value;
	}

	public static void ValidateCharacterSkin(Stream stream)
	{
		Image image = Image.Load(stream);
		if (image.Width > 256 || image.Height > 256)
		{
			throw new InvalidOperationException($"Character skin is larger than 256x256 pixels (size={image.Width}x{image.Height})");
		}
		if (!MathUtils.IsPowerOf2(image.Width) || !MathUtils.IsPowerOf2(image.Height))
		{
			throw new InvalidOperationException($"Character skin does not have power-of-two size (size={image.Width}x{image.Height})");
		}
	}

	static CharacterSkinsManager()
	{
		m_characterSkinNames = new List<string>();
		m_playerModels = new Dictionary<PlayerClass, Model>();
		m_outerClothingModels = new Dictionary<PlayerClass, Model>();
		waitReplyList = new List<string>();
	}
}
