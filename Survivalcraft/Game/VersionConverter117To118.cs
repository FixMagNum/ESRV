using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter117To118 : VersionConverter
{
	public override string SourceVersion => "1.17";

	public override string TargetVersion => "1.18";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string value = Storage.CombinePaths(directoryName, "Project.xml");
		XElement xElement;
		using (Stream value2 = Storage.OpenFile(value, OpenFileMode.Read))
		{
			xElement = XmlUtils.LoadXmlFromStream(value2, null, throwOnError: true);
		}
		ConvertProjectXml(xElement);
		using Stream ex = Storage.OpenFile(value, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(xElement, ex, null, throwOnError: true);
	}
}
