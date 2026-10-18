namespace Game.IContentReader;

public class MtllibStructReader : IContentReader
{
	public override string Type => "Game.MtllibStruct";

	public override string[] DefaultSuffix => new string[1] { "mtl" };

	public override object Get(ContentInfo[] contents)
	{
		return MtllibStruct.Load(contents[0].Duplicate());
	}
}
