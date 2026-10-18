using System.IO;
using SimpleJson;

namespace Game.IContentReader;

public class JsonObjectReader : IContentReader
{
	public override string Type => "SimpleJson.JsonObject";

	public override string[] DefaultSuffix => new string[1] { "json" };

	public override object Get(ContentInfo[] contents)
	{
		return global::SimpleJson.SimpleJson.DeserializeObject<JsonObject>(new StreamReader(contents[0].Duplicate()).ReadToEnd());
	}
}
