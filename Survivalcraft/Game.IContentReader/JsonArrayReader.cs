using System.IO;
using SimpleJson;

namespace Game.IContentReader;

public class JsonArrayReader : IContentReader
{
	public override string Type => "SimpleJson.JsonArray";

	public override string[] DefaultSuffix => new string[1] { "json" };

	public override object Get(ContentInfo[] contents)
	{
		return global::SimpleJson.SimpleJson.DeserializeObject<JsonArray>(new StreamReader(contents[0].Duplicate()).ReadToEnd());
	}
}
