using Engine.Graphics;

namespace Game.IContentReader;

public class Texture2DReader : IContentReader
{
	public override string Type => "Engine.Graphics.Texture2D";

	public override string[] DefaultSuffix => new string[3] { "png", "jpg", "jpeg" };

	public override object Get(ContentInfo[] contents)
	{
		return Texture2D.Load(contents[0].Duplicate());
	}
}
