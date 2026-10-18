using System;
using System.Globalization;
using System.Text;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class ClothingDesign
{
	public static string fName;

	public const int DefaultWidth = 256;

	public const int DefaultHeight = 256;

	public int m_index = -1;

	public string m_name = string.Empty;

	public int m_loadTimeLinkedDesignIndex = -1;

	public int m_resolutionX;

	public int m_resolutionY;

	public int[] m_values;

	public int? m_hash;

	public Texture2D m_texture;

	public ClothingDesign m_linkedDesign;

	public int ResolutionX => m_resolutionX;

	public int ResolutionY => m_resolutionY;

	public int Hash
	{
		get
		{
			if (!m_hash.HasValue)
			{
				m_hash = m_resolutionX ^ m_resolutionY;
				for (int i = 0; i < m_values.Length; i++)
				{
					m_hash += m_values[i] * (1 + 113 * i);
				}
			}
			return m_hash.Value;
		}
	}

	public Texture2D Texture
	{
		get
		{
			if (m_texture == null)
			{
				CreateTexture();
			}
			return m_texture;
		}
	}

	public int Index
	{
		get
		{
			return m_index;
		}
		set
		{
			m_index = value;
		}
	}

	public string Name
	{
		get
		{
			return m_name;
		}
		set
		{
			if (value.Length > 0)
			{
				if (value[0] == ' ' || value[value.Length - 1] == ' ')
				{
					throw new InvalidOperationException("Invalid name format");
				}
				if (value.Length > 20)
				{
					value = value.Substring(0, 20);
				}
			}
			m_name = value;
		}
	}

	public ClothingDesign LinkedDesign
	{
		get
		{
			return m_linkedDesign;
		}
		set
		{
			if (value != m_linkedDesign)
			{
				m_linkedDesign = value;
				m_hash = null;
			}
		}
	}

	public ClothingDesign()
	{
	}

	public ClothingDesign(int index, ValuesDictionary valuesDictionary)
	{
		m_index = index;
		Name = valuesDictionary.GetValue("Name", string.Empty);
		int value = valuesDictionary.GetValue<int>("ResolutionX");
		int value2 = valuesDictionary.GetValue<int>("ResolutionY");
		m_loadTimeLinkedDesignIndex = valuesDictionary.GetValue("LinkedDesign", -1);
		string value3 = valuesDictionary.GetValue<string>("Values");
		int num = 0;
		int[] array = new int[value * value2];
		string[] array2 = value3.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(new char[1] { '*' }, StringSplitOptions.None);
			if (array3.Length != 2)
			{
				throw new InvalidOperationException("Invalid clothing design data");
			}
			int num2 = int.Parse(array3[0], CultureInfo.InvariantCulture);
			int num3 = int.Parse(array3[1], CultureInfo.InvariantCulture);
			for (int j = 0; j < num2; j++)
			{
				if (num < array.Length)
				{
					array[num] = num3;
					num++;
				}
			}
		}
		SetValues(value, value2, array);
	}

	public Color GetPixel(int x, int y)
	{
		if (x < 0 || x >= m_resolutionX || y < 0 || y >= m_resolutionY)
		{
			return Color.Transparent;
		}
		return new Color((uint)m_values[x + y * m_resolutionX]);
	}

	public void SetValues(int resX, int resY, int[] valuesPacked)
	{
		if (valuesPacked.Length != resX * resY)
		{
			throw new ArgumentException("Values array length does not match resolution");
		}
		m_resolutionX = resX;
		m_resolutionY = resY;
		if (m_values == null || m_values.Length != valuesPacked.Length)
		{
			m_values = new int[valuesPacked.Length];
		}
		valuesPacked.CopyTo(m_values, 0);
		m_hash = null;
		if (m_texture != null)
		{
			Utilities.Dispose(ref m_texture);
			m_texture = null;
		}
	}

	public void Paint(int x, int y, Color color)
	{
		if (x < 0 || x >= m_resolutionX || y < 0 || y >= m_resolutionY)
		{
			return;
		}
		int packedValue = (int)color.PackedValue;
		int num = x + y * m_resolutionX;
		if (m_values[num] != packedValue)
		{
			m_values[num] = packedValue;
			m_hash = null;
			if (m_texture != null)
			{
				m_texture.SetData(0, m_values, m_values.Length);
			}
		}
	}

	public void CreateTexture()
	{
		m_texture = new Texture2D(m_resolutionX, m_resolutionY, 1, ColorFormat.Rgba8888);
		m_texture.SetData(0, m_values, m_values.Length);
	}

	public ClothingDesign Clone()
	{
		ClothingDesign clothingDesign = new ClothingDesign();
		int[] array = new int[m_values.Length];
		m_values.CopyTo(array, 0);
		clothingDesign.SetValues(ResolutionX, ResolutionY, array);
		clothingDesign.Name = Name;
		clothingDesign.LinkedDesign = LinkedDesign;
		return clothingDesign;
	}

	public ValuesDictionary Save()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = m_values[0];
		int num2 = 1;
		for (int i = 1; i < m_values.Length; i++)
		{
			if (m_values[i] != num)
			{
				stringBuilder.Append(num2.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append('*');
				stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				num = m_values[i];
				num2 = 1;
			}
			else
			{
				num2++;
			}
		}
		stringBuilder.Append(num2.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append('*');
		stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
		stringBuilder.Append(',');
		ValuesDictionary valuesDictionary = new ValuesDictionary();
		if (!string.IsNullOrEmpty(Name))
		{
			valuesDictionary.SetValue("Name", Name);
		}
		valuesDictionary.SetValue("ResolutionX", m_resolutionX);
		valuesDictionary.SetValue("ResolutionY", m_resolutionY);
		if (LinkedDesign != null)
		{
			valuesDictionary.SetValue("LinkedDesign", LinkedDesign.Index);
		}
		valuesDictionary.SetValue("Values", stringBuilder.ToString());
		return valuesDictionary;
	}

	public void Dispose()
	{
		if (m_texture != null)
		{
			m_texture.Dispose();
			m_texture = null;
		}
	}

	static ClothingDesign()
	{
		fName = "ClothingDesign";
	}
}
