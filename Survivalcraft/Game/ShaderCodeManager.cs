using System;
using System.IO;
using System.Text.RegularExpressions;
using Engine;

namespace Game;

public class ShaderCodeManager
{
	public static string GetFast(string fname)
	{
		string result = string.Empty;
		string[] array = fname.Split('.');
		if (array.Length > 1)
		{
			result = ModsManager.GetInPakOrStorageFile<string>(array[0], "." + array[1]);
		}
		return result;
	}

	public static string Get(string fname)
	{
		return GetIncludeText(string.Empty, fname, external: false);
	}

	public static string GetExternal(string fname)
	{
		return GetIncludeText(string.Empty, fname, external: true);
	}

	public static string GetIncludeText(string shaderText, string includefname, bool external)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		try
		{
			if (external)
			{
				using StreamReader streamReader = new StreamReader(Storage.OpenFile(Storage.CombinePaths("app:/", includefname), OpenFileMode.Read));
				text2 = streamReader.ReadToEnd();
			}
			else if (includefname.Contains(".txt"))
			{
				includefname = includefname.Split('.')[0];
				text2 = ContentManager.Get<string>(includefname);
			}
			else
			{
				text2 = GetFast(includefname);
			}
			if (string.IsNullOrEmpty(text2))
			{
				Log.Warning("[ShaderCodeManager] Файл пуст или не найден: " + includefname);
				return string.Empty;
			}
			string[] array = text2.Replace("\r\n", "\n").Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text3 = array[i].Trim();
				if (string.IsNullOrEmpty(text3))
				{
					text += "\n";
				}
				else if (text3.StartsWith("//"))
				{
					text = ((!text3.Contains("<") || !text3.Contains("/>")) ? (text + "\n") : (text + text3 + "\n"));
				}
				else if (text3.StartsWith("#include"))
				{
					string includefname2 = new Regex("\"[^\"]*\"").Match(text3).Value.Replace("\"", "");
					text += GetIncludeText(string.Empty, includefname2, external);
				}
				else
				{
					text = text + text3 + "\n";
				}
			}
			shaderText += text;
		}
		catch (Exception ex)
		{
			Log.Error("[ShaderCodeManager] КРИТИЧЕСКАЯ ОШИБКА " + includefname + ": " + ex.Message);
		}
		return shaderText;
	}
}
