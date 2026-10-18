using System.IO;
using System.Text.RegularExpressions;
using Engine.Graphics;

namespace Game.IContentReader;

public class ShaderReader : IContentReader
{
	public override string Type => "Engine.Graphics.Shader";

	public override string[] DefaultSuffix => new string[2] { "vsh", "psh" };

	public override object Get(ContentInfo[] contents)
	{
		string input = new StreamReader(contents[0].Duplicate()).ReadToEnd();
		string input2 = new StreamReader(contents[1].Duplicate()).ReadToEnd();
		string vertexShaderCode = Regex.Replace(input, "#include\\s+\"([^\"]+)\"", (Match m) => ShaderCodeManager.GetIncludeText(string.Empty, m.Groups[1].Value, external: false));
		input2 = Regex.Replace(input2, "#include\\s+\"([^\"]+)\"", (Match m) => ShaderCodeManager.GetIncludeText(string.Empty, m.Groups[1].Value, external: false));
		return new Shader(vertexShaderCode, input2, new ShaderMacro("empty"));
	}
}
