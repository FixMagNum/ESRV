using System;
using System.Collections.Generic;
using System.Text;
using Engine.Input;

namespace Game;

public static class KeyBindings
{
	private struct Pair
	{
		public Key? Primary;

		public Key? Secondary;
	}

	private static readonly Dictionary<KeyAction, Pair> _map;

	public static IEnumerable<KeyAction> AllActions => (KeyAction[])Enum.GetValues(typeof(KeyAction));

	static KeyBindings()
	{
		_map = new Dictionary<KeyAction, Pair>(64);
		ResetToDefaults();
	}

	public static Key GetPrimary(KeyAction a)
	{
		return GetPrimaryNullable(a) ?? Key.Escape;
	}

	public static Key GetSecondary(KeyAction a)
	{
		return GetSecondaryNullable(a) ?? Key.Escape;
	}

	public static void SetPrimary(KeyAction a, Key k)
	{
		SetPrimaryNullable(a, k);
	}

	public static void SetSecondary(KeyAction a, Key k)
	{
		SetSecondaryNullable(a, k);
	}

	public static bool IsActionDown(WidgetInput input, KeyAction a)
	{
		Key? empty = GetPrimaryNullable(a);
		Key? empty2 = GetSecondaryNullable(a);
		if (!empty.HasValue || !input.IsKeyDown(empty.Value))
		{
			if (empty2.HasValue)
			{
				return input.IsKeyDown(empty2.Value);
			}
			return false;
		}
		return true;
	}

	public static bool IsActionDownOnce(WidgetInput input, KeyAction a)
	{
		Key? primaryNullable = GetPrimaryNullable(a);
		Key? secondaryNullable = GetSecondaryNullable(a);
		if (!primaryNullable.HasValue || !input.IsKeyDownOnce(primaryNullable.Value))
		{
			if (secondaryNullable.HasValue)
			{
				return input.IsKeyDownOnce(secondaryNullable.Value);
			}
			return false;
		}
		return true;
	}

	public static void ResetToDefaults()
	{
		_map.Clear();
		SetPrimary(KeyAction.MoveForward, Key.W);
		SetPrimary(KeyAction.MoveBackward, Key.S);
		SetPrimary(KeyAction.MoveLeft, Key.A);
		SetPrimary(KeyAction.MoveRight, Key.D);
		SetPrimary(KeyAction.Jump, Key.Space);
		SetPrimary(KeyAction.FlyUp, Key.Space);
		SetPrimary(KeyAction.FlyDown, Key.Shift);
		SetPrimary(KeyAction.ToggleSneak, Key.Shift);
		SetPrimary(KeyAction.ToggleCreativeFly, Key.F);
		SetPrimary(KeyAction.ToggleMount, Key.R);
		SetPrimary(KeyAction.EditItem, Key.G);
		SetPrimary(KeyAction.ToggleInventory, Key.E);
		SetPrimary(KeyAction.ToggleClothing, Key.C);
		SetPrimary(KeyAction.Drop, Key.Q);
		SetPrimary(KeyAction.TakeScreenshot, Key.P);
		SetPrimary(KeyAction.SwitchCameraMode, Key.V);
		SetPrimary(KeyAction.TimeOfDay, Key.T);
		SetPrimary(KeyAction.Lighting, Key.L);
		SetPrimary(KeyAction.KeyboardHelp, Key.H);
		SetPrimary(KeyAction.Hotbar1, Key.Number1);
		SetPrimary(KeyAction.Hotbar2, Key.Number2);
		SetPrimary(KeyAction.Hotbar3, Key.Number3);
		SetPrimary(KeyAction.Hotbar4, Key.Number4);
		SetPrimary(KeyAction.Hotbar5, Key.Number5);
		SetPrimary(KeyAction.Hotbar6, Key.Number6);
		SetPrimary(KeyAction.Hotbar7, Key.Number7);
		SetPrimary(KeyAction.Hotbar8, Key.Number8);
		SetPrimary(KeyAction.Hotbar9, Key.Number9);
		SetPrimary(KeyAction.Hotbar0, Key.Number0);
		SetPrimary(KeyAction.OpenChat, Key.Tab);
		SetPrimary(KeyAction.ToggleSpeedCreativeFly, Key.CapsLock);
		SetPrimary(KeyAction.SendChat, Key.Enter);
		SetPrimary(KeyAction.ToggleFullscreen, Key.F11);
		SetPrimary(KeyAction.WeSetP1, Key.F1);
		SetPrimary(KeyAction.WeSetP2, Key.F2);
		SetPrimary(KeyAction.WeSetP3, Key.F3);
		SetPrimary(KeyAction.WeEmbedFromMemory, Key.F5);
		SetPrimary(KeyAction.WeFillRegion, Key.F6);
		SetPrimary(KeyAction.WeReplaceInRegion, Key.F7);
		SetPrimary(KeyAction.WeClearRegion, Key.F8);
		SetPrimary(KeyAction.WeCopyToMemory, Key.F9);
		SetPrimary(KeyAction.WePasteFromMemory, Key.F10);
		SetPrimary(KeyAction.WeOpenMenu, Key.F11);
		SetPrimaryNullable(KeyAction.WeUndo, null);
		SetPrimaryNullable(KeyAction.WeRedo, null);
		SetPrimaryNullable(KeyAction.WeToggleOverlay, null);
		SetPrimaryNullable(KeyAction.WeRotateH, null);
		SetPrimaryNullable(KeyAction.WeRotateV, null);
		SetPrimaryNullable(KeyAction.WeMirrorH, null);
		SetPrimaryNullable(KeyAction.WeMirrorV, null);
		SetPrimaryNullable(KeyAction.WeToggleWe, null);
		SetPrimary(KeyAction.FastSlotShifting, Key.Shift);
	}

	public static Key? GetPrimaryNullable(KeyAction a)
	{
		if (!_map.TryGetValue(a, out var value))
		{
			return null;
		}
		return value.Primary;
	}

	public static Key? GetSecondaryNullable(KeyAction a)
	{
		if (!_map.TryGetValue(a, out var value))
		{
			return null;
		}
		return value.Secondary;
	}

	public static string FormatKey(Key? key)
	{
		if (!key.HasValue)
		{
			return "—";
		}
		return key.Value switch
		{
			Key.BackSpace => "Backspace", 
			Key.Number0 => "0", 
			Key.Number1 => "1", 
			Key.Number2 => "2", 
			Key.Number3 => "3", 
			Key.Number4 => "4", 
			Key.Number5 => "5", 
			Key.Number6 => "6", 
			Key.Number7 => "7", 
			Key.Number8 => "8", 
			Key.Number9 => "9", 
			Key.Tilde => "`", 
			Key.Minus => "-", 
			Key.Plus => "=", 
			Key.LeftBracket => "[", 
			Key.RightBracket => "]", 
			Key.Semicolon => ";", 
			Key.Quote => "'", 
			Key.Comma => ",", 
			Key.Period => ".", 
			Key.Slash => "/", 
			_ => key.Value.ToString(), 
		};
	}

	public static string Serialize()
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (KeyAction allAction in AllActions)
		{
			if (!flag)
			{
				stringBuilder.Append('|');
			}
			else
			{
				flag = false;
			}
			string value = GetPrimaryNullable(allAction)?.ToString() ?? "";
			string value2 = GetSecondaryNullable(allAction)?.ToString() ?? "";
			stringBuilder.Append(allAction).Append('=').Append(value)
				.Append(',')
				.Append(value2);
		}
		return stringBuilder.ToString();
	}

	public static bool TryDeserialize(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return false;
		}
		Dictionary<KeyAction, Pair> dictionary = new Dictionary<KeyAction, Pair>(64);
		string[] array = data.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			if (num <= 0 || !Enum.TryParse<KeyAction>(text.Substring(0, num), out var result))
			{
				continue;
			}
			string[] array2 = text.Substring(num + 1).Split(',');
			Key? primary = null;
			Key? secondary = null;
			if (array2.Length != 0 && !string.IsNullOrEmpty(array2[0]))
			{
				int result3;
				if (Enum.TryParse<Key>(array2[0], ignoreCase: true, out var result2))
				{
					primary = result2;
				}
				else if (int.TryParse(array2[0], out result3))
				{
					return false;
				}
			}
			if (array2.Length > 1 && !string.IsNullOrEmpty(array2[1]))
			{
				int result5;
				if (Enum.TryParse<Key>(array2[1], ignoreCase: true, out var result4))
				{
					secondary = result4;
				}
				else if (int.TryParse(array2[1], out result5))
				{
					return false;
				}
			}
			dictionary[result] = new Pair
			{
				Primary = primary,
				Secondary = secondary
			};
		}
		foreach (KeyValuePair<KeyAction, Pair> item in dictionary)
		{
			_map[item.Key] = item.Value;
		}
		return true;
	}

	public static void SetPrimaryNullable(KeyAction a, Key? k)
	{
		if (!_map.TryGetValue(a, out var value))
		{
			value = default;
		}
		value.Primary = k;
		_map[a] = value;
	}

	public static void SetSecondaryNullable(KeyAction a, Key? k)
	{
		if (!_map.TryGetValue(a, out var value))
		{
			value = default;
		}
		value.Secondary = k;
		_map[a] = value;
	}
}
