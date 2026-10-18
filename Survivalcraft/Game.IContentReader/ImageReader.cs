using Engine.Media;

namespace Game.IContentReader;

public class ImageReader : IContentReader
{
	public override string Type => "Engine.Media.Image";

	public override string[] DefaultSuffix => new string[3] { "png", "jpeg", "jpg" };

	public override object Get(ContentInfo[] contents)
	{
		return Image.Load(contents[0].Duplicate());
	}
}
