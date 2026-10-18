using System;
using System.Collections.Generic;
using System.IO;

namespace Game;

public class MtllibStruct
{
	public Dictionary<string, string> TexturePaths = new Dictionary<string, string>();

	public static MtllibStruct Load(Stream stream)
	{
		MtllibStruct mtllibStruct = new MtllibStruct();
		using (stream)
		{
			StreamReader streamReader = new StreamReader(stream);
			string text = null;
			while (!streamReader.EndOfStream)
			{
				string[] array = streamReader.ReadLine().Split(new char[2] { '\t', ' ' }, StringSplitOptions.None);
				string text2 = array[0];
				if (!(text2 == "newmtl"))
				{
					if (text2 == "map_Kd")
					{
						if (string.IsNullOrEmpty(text))
						{
							throw new Exception("请先newmtl");
						}
						mtllibStruct.TexturePaths.Add(text, array[1]);
					}
				}
				else
				{
					text = array[1];
				}
			}
			return mtllibStruct;
		}
	}
}
