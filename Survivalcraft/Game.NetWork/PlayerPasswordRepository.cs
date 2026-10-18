using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Engine;

namespace Game.NetWork;

public static class PlayerPasswordRepository
{
	private struct Entry
	{
		public string Nick;

		public string Salt;

		public string Hash;

		public int Version;
	}

	private static readonly Dictionary<Guid, Entry> s_cache = new Dictionary<Guid, Entry>();

	private static string s_filePath;

	private static bool s_inited;

	private const int CurrentVersion = 1;

	private const int SaltSize = 16;

	private const int HashIterations = 60000;

	public static void Initialize(WorldSettings ws)
	{
		if (ws == null)
		{
			throw new InvalidOperationException("WorldSettings is null.");
		}
		string text = Storage.CombinePaths("app:/Worlds", ws.Name);
		if (GameManager.WorldInfo != null && GameManager.WorldInfo.WorldSettings.Name == ws.Name)
		{
			text = GameManager.WorldInfo.DirectoryName;
		}
		else
		{
			WorldInfo worldInfo = WorldsManager.WorldInfos.FirstOrDefault((WorldInfo w) => w.WorldSettings.Name == ws.Name);
			if (worldInfo != null)
			{
				text = worldInfo.DirectoryName;
			}
		}
		Storage.CreateDirectory(text);
		s_filePath = Storage.CombinePaths(text, "Passwords.txt");
		if (!Storage.FileExists(s_filePath))
		{
			using (Storage.OpenFile(s_filePath, OpenFileMode.Create))
			{
			}
		}
		Load();
		s_inited = true;
	}

	private static void EnsureInit()
	{
		if (!s_inited)
		{
			throw new InvalidOperationException("PlayerPasswordRepository is not initialized.");
		}
	}

	private static void Load()
	{
		s_cache.Clear();
		using Stream stream = Storage.OpenFile(s_filePath, OpenFileMode.CreateOrOpen);
		stream.Position = 0L;
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
		while (true)
		{
			string text = streamReader.ReadLine();
			if (text == null)
			{
				break;
			}
			string text2 = streamReader.ReadLine();
			if (text2 == null)
			{
				break;
			}
			string text3 = streamReader.ReadLine();
			if (text3 == null)
			{
				break;
			}
			if (Guid.TryParse(text2, out var result))
			{
				string nick = text ?? string.Empty;
				string[] array = (text3 ?? string.Empty).Split('|');
				if (array.Length == 3 && int.TryParse(array[0], out var result2))
				{
					Entry value = new Entry
					{
						Nick = nick,
						Version = result2,
						Salt = (array[1] ?? string.Empty),
						Hash = (array[2] ?? string.Empty)
					};
					s_cache[result] = value;
				}
			}
			streamReader.ReadLine();
		}
	}

	public static bool TryGetPassword(Guid userId, out string password)
	{
		EnsureInit();
		if (s_cache.TryGetValue(userId, out var value))
		{
			password = $"{value.Version}|{value.Salt ?? string.Empty}|{value.Hash ?? string.Empty}";
			return true;
		}
		password = null;
		return false;
	}

	public static void Append(string nick, Guid userId, string password)
	{
		EnsureInit();
		Entry value = CreateEntry(nick, password);
		using (Stream stream = Storage.OpenFile(s_filePath, OpenFileMode.CreateOrOpen))
		{
			stream.Position = stream.Length;
			using StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
			streamWriter.WriteLine(value.Nick ?? string.Empty);
			streamWriter.WriteLine(userId.ToString());
			streamWriter.WriteLine($"{value.Version}|{value.Salt ?? string.Empty}|{value.Hash ?? string.Empty}");
			streamWriter.WriteLine();
			streamWriter.Flush();
		}
		s_cache[userId] = value;
	}

	private static void SaveAll()
	{
		EnsureInit();
		using Stream stream = Storage.OpenFile(s_filePath, OpenFileMode.Create);
		using StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
		foreach (KeyValuePair<Guid, Entry> item in s_cache)
		{
			streamWriter.WriteLine(item.Value.Nick ?? string.Empty);
			streamWriter.WriteLine(item.Key.ToString());
			streamWriter.WriteLine($"{item.Value.Version}|{item.Value.Salt ?? string.Empty}|{item.Value.Hash ?? string.Empty}");
			streamWriter.WriteLine();
		}
		streamWriter.Flush();
	}

	public static void Set(string nick, Guid userId, string password)
	{
		EnsureInit();
		Entry value = CreateEntry(nick, password);
		s_cache[userId] = value;
		SaveAll();
	}

	public static void UpdateNick(Guid userId, string nick)
	{
		EnsureInit();
		if (s_cache.TryGetValue(userId, out var value))
		{
			value.Nick = nick ?? string.Empty;
			s_cache[userId] = value;
			SaveAll();
		}
	}

	private static string GenerateSalt()
	{
		byte[] array = new byte[16];
		using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array);
		}
		return Convert.ToBase64String(array);
	}

	private static string ComputeHash(string password, string saltBase64, int version)
	{
		if (password == null)
		{
			password = string.Empty;
		}
		byte[] array;
		try
		{
			array = Convert.FromBase64String(saltBase64 ?? string.Empty);
		}
		catch
		{
			array = new byte[0];
		}
		byte[] bytes = Encoding.UTF8.GetBytes(password);
		byte[] array2 = new byte[array.Length + bytes.Length + array.Length];
		Buffer.BlockCopy(array, 0, array2, 0, array.Length);
		Buffer.BlockCopy(bytes, 0, array2, array.Length, bytes.Length);
		Buffer.BlockCopy(array, 0, array2, array.Length + bytes.Length, array.Length);
		using SHA256 sHA = SHA256.Create();
		byte[] array3 = array2;
		for (int i = 0; i < 60000; i++)
		{
			array3 = sHA.ComputeHash(array3);
		}
		return Convert.ToBase64String(array3);
	}

	private static Entry CreateEntry(string nick, string password)
	{
		Entry result = default;
		result.Nick = nick ?? string.Empty;
		result.Salt = GenerateSalt();
		result.Version = 1;
		result.Hash = ComputeHash(password ?? string.Empty, result.Salt, result.Version);
		return result;
	}

	public static bool VerifyPassword(Guid userId, string password)
	{
		EnsureInit();
		if (s_cache.TryGetValue(userId, out var value) && !string.IsNullOrEmpty(value.Salt) && !string.IsNullOrEmpty(value.Hash))
		{
			return string.Equals(ComputeHash(password ?? string.Empty, value.Salt, value.Version), value.Hash, StringComparison.Ordinal);
		}
		return false;
	}

	public static bool HasEntry(Guid userId)
	{
		EnsureInit();
		return s_cache.ContainsKey(userId);
	}

	public static void Remove(Guid userId)
	{
		EnsureInit();
		if (s_cache.Remove(userId))
		{
			SaveAll();
		}
	}

	public static void ClearAll()
	{
		EnsureInit();
		s_cache.Clear();
		if (Storage.FileExists(s_filePath))
		{
			Storage.DeleteFile(s_filePath);
			using (Storage.OpenFile(s_filePath, OpenFileMode.Create))
			{
			}
		}
	}

	public static int GetPasswordsCount()
	{
		if (!s_inited)
		{
			return 0;
		}
		return s_cache.Count;
	}
}
