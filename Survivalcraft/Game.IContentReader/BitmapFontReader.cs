using System;
using Engine.Media;

namespace Game.IContentReader;

public class BitmapFontReader : IContentReader
{
	public override string Type => "Engine.Media.BitmapFont";

	public override string[] DefaultSuffix => new string[2] { "lst", "png" };

	public override object Get(ContentInfo[] contents)
	{
		if (contents.Length != 2)
		{
			throw new Exception("not matches content count");
		}
		return BitmapFont.Initialize(contents[1].Duplicate(), contents[0].Duplicate());
	}
}
