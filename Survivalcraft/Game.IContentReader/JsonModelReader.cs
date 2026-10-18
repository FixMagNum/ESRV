namespace Game.IContentReader;

public class JsonModelReader : IContentReader
{
	public override string Type => "Game.JsonModel";

	public override string[] DefaultSuffix => new string[1] { "json" };

	public override object Get(ContentInfo[] contents)
	{
		return Game.JsonModelReader.Load(contents[0].Duplicate());
	}
}
