using System.IO;
using System.Text;
using Engine;

namespace Game;

public static class PlayerProfiles
{
	public struct Entry
	{
		public string Token;

		public string Name;

		public string Password;

		public string PhotoPath;

		public bool IsEmpty => string.IsNullOrEmpty(Token);
	}

	public const int Max = 5;

	public static readonly string FilePath = Storage.CombinePaths(ZoneDialog.Path_mod, "profiles.dat");

	public static Entry[] LoadAll()
	{
		Entry[] array = new Entry[5];
		try
		{
			if (!Storage.FileExists(FilePath))
			{
				return array;
			}
			using Stream stream = Storage.OpenFile(FilePath, OpenFileMode.Read);
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			int num = 0;
			while (!streamReader.EndOfStream && num < 5)
			{
				string text = streamReader.ReadLine();
				if (string.IsNullOrEmpty(text))
				{
					num++;
					continue;
				}
				string[] array2 = text.Split('|');
				if (array2.Length >= 4)
				{
					array[num].Token = Unesc(array2[0]);
					array[num].Name = Unesc(array2[1]);
					array[num].Password = Unesc(array2[2]);
					array[num].PhotoPath = Unesc(array2[3]);
				}
				num++;
			}
		}
		catch
		{
		}
		return array;
	}

	public static void SaveAll(Entry[] arr)
	{
		try
		{
			Storage.CreateDirectory(ZoneDialog.Path_mod);
			using Stream stream = Storage.OpenFile(FilePath, OpenFileMode.Create);
			using StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
			for (int i = 0; i < 5; i++)
			{
				Entry entry = ((i < arr.Length) ? arr[i] : default(Entry));
				string value = string.Join("|", Esc(entry.Token), Esc(entry.Name), Esc(entry.Password), Esc(entry.PhotoPath));
				streamWriter.WriteLine(value);
			}
		}
		catch
		{
		}
	}

	public static int Count(Entry[] arr)
	{
		int num = 0;
		for (int i = 0; i < arr.Length; i++)
		{
			if (!arr[i].IsEmpty)
			{
				num++;
			}
		}
		return num;
	}

	public static int FirstEmpty(Entry[] arr)
	{
		for (int i = 0; i < 5; i++)
		{
			if (i >= arr.Length || arr[i].IsEmpty)
			{
				return i;
			}
		}
		return -1;
	}

	public static void SetAt(ref Entry[] arr, int index, Entry e)
	{
		if (arr == null || arr.Length != 5)
		{
			arr = new Entry[5];
		}
		if (index >= 0 && index < 5)
		{
			arr[index] = e;
		}
	}

	public static void ClearAt(ref Entry[] arr, int index)
	{
		if (arr != null && index >= 0 && index < 5)
		{
			arr[index] = default;
		}
	}

	private static string Esc(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Replace("|", "%7C");
		}
		return "";
	}

	private static string Unesc(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Replace("%7C", "|");
		}
		return "";
	}
}
