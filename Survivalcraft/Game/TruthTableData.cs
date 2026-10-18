using System.Collections.Generic;
using System.Text;
using Engine;

namespace Game;

public class TruthTableData : IEditableItemData
{
	public static List<char> m_hexChars = new List<char>
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'A', 'B', 'C', 'D', 'E', 'F'
	};

	public byte[] Data = new byte[16];

	public IEditableItemData Copy()
	{
		return new TruthTableData
		{
			Data = (byte[])Data.Clone()
		};
	}

	public void LoadString(string data)
	{
		for (int point = 0; point < 16; point++)
		{
			int cellContents = ((point < data.Length) ? m_hexChars.IndexOf(char.ToUpperInvariant(data[point])) : 0);
			if (cellContents < 0)
			{
				cellContents = 0;
			}
			Data[point] = (byte)cellContents;
		}
	}

	public void LoadBinaryString(string data)
	{
		for (int i = 0; i < 16; i++)
		{
			Data[i] = (byte)((i < data.Length && data[i] != '0') ? 15u : 0u);
		}
	}

	public string SaveString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < Data.Length; i++)
		{
			int index = MathUtils.Clamp(Data[i], 0, 15);
			stringBuilder.Append(m_hexChars[index]);
		}
		return stringBuilder.ToString().TrimEnd('0');
	}

	public string SaveBinaryString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < Data.Length; i++)
		{
			stringBuilder.Append((Data[i] != 0) ? '1' : '0');
		}
		return stringBuilder.ToString().TrimEnd('0');
	}
}
