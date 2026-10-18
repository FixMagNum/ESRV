using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter124To125 : VersionConverter
{
	public override string SourceVersion => "1.24";

	public override string TargetVersion => "1.25";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string path = Storage.CombinePaths(directoryName, "Project.xml");
		XElement xElement;
		using (Stream item = Storage.OpenFile(path, OpenFileMode.Read))
		{
			xElement = XmlUtils.LoadXmlFromStream(item, null, throwOnError: true);
		}
		ConvertProjectXml(xElement);
		using Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(xElement, stream, null, throwOnError: true);
	}
}
