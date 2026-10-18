using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter110To111 : VersionConverter
{
	public override string SourceVersion => "1.10";

	public override string TargetVersion => "1.11";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string num = Storage.CombinePaths(directoryName, "Project.xml");
		XElement x;
		using (Stream num2 = Storage.OpenFile(num, OpenFileMode.Read))
		{
			x = XmlUtils.LoadXmlFromStream(num2, null, throwOnError: true);
		}
		ConvertProjectXml(x);
		using Stream stream = Storage.OpenFile(num, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(x, stream, null, throwOnError: true);
	}
}
