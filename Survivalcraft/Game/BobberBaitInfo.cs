using System.Collections.Generic;

namespace Game;

public static class BobberBaitInfo
{
	private static readonly Dictionary<int, int> m_strengthByContents;

	public static readonly int[] AllowedContents;

	public static bool IsAllowedBait(int contents)
	{
		return m_strengthByContents.ContainsKey(contents);
	}

	public static int GetBaitStrength(int contents)
	{
		if (!m_strengthByContents.TryGetValue(contents, out var value))
		{
			return 0;
		}
		return value;
	}

	public static string GetRadiusString(int strength)
	{
		return strength switch
		{
			1 => "1x1", 
			2 => "2x2", 
			3 => "4x4", 
			4 => "10x10", 
			_ => "0x0", 
		};
	}

	static BobberBaitInfo()
	{
		AllowedContents = new int[21]
		{
			318, 326, 316, 177, 328, 335, 329, 88, 89, 346,
			161, 345, 162, 77, 78, 347, 332, 333, 336, 331,
			310
		};
		m_strengthByContents = new Dictionary<int, int>
		{
			{ 318, 1 },
			{ 326, 1 },
			{ 316, 2 },
			{ 177, 2 },
			{ 328, 2 },
			{ 335, 2 },
			{ 329, 2 },
			{ 88, 3 },
			{ 89, 3 },
			{ 346, 3 },
			{ 161, 3 },
			{ 345, 3 },
			{ 162, 3 },
			{ 77, 3 },
			{ 78, 3 },
			{ 347, 3 },
			{ 332, 4 },
			{ 333, 4 },
			{ 336, 4 },
			{ 331, 4 },
			{ 310, 4 }
		};
	}

	public static int GetIndex(int contents)
	{
		for (int i = 0; i < AllowedContents.Length; i++)
		{
			if (AllowedContents[i] == contents)
			{
				return i + 1;
			}
		}
		return 0;
	}

	public static int GetContentsByIndex(int index)
	{
		if (index <= 0 || index > AllowedContents.Length)
		{
			return 0;
		}
		return AllowedContents[index - 1];
	}

	public static bool IsAllowedBaitValue(int value)
	{
		return IsAllowedBait(Terrain.ExtractContents(value));
	}

	public static int GetBaitStrengthByValue(int value)
	{
		int num = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		if (num == 318)
		{
			switch (WormBlock.GetKind(data))
			{
			case WormBlock.WormKind.Normal:
				return 1;
			case WormBlock.WormKind.StripedCaterpillar:
			case WormBlock.WormKind.Earthworm:
				return 2;
			case WormBlock.WormKind.TitanitusColossus:
				return 3;
			default:
				return 1;
			}
		}
		return GetBaitStrength(num);
	}
}
