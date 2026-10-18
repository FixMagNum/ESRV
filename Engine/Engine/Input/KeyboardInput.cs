using System.Collections.Generic;

namespace Engine.Input;

public class KeyboardInput
{
	public static List<char> Chars = new List<char>();

	public static bool _DeletePressed;

	public static bool DeletePressed
	{
		get
		{
			bool deletePressed = _DeletePressed;
			if (deletePressed)
			{
				_DeletePressed = false;
			}
			return deletePressed;
		}
		set
		{
			_DeletePressed = value;
		}
	}

	public static string GetInput()
	{
		if (Chars.Count > 0)
		{
			string result = new string(Chars.ToArray());
			Chars.Clear();
			return result;
		}
		return string.Empty;
	}
}
