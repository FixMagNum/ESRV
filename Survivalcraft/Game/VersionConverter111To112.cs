using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter111To112 : VersionConverter
{
	public override string SourceVersion => "1.11";

	public override string TargetVersion => "1.12";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string modelsToDraw = Storage.CombinePaths(directoryName, "Project.xml");
		XElement i;
		using (Stream stream = Storage.OpenFile(modelsToDraw, OpenFileMode.Read))
		{
			i = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
		}
		ConvertProjectXml(i);
		using Stream value = Storage.OpenFile(modelsToDraw, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(i, value, null, throwOnError: true);
	}
}
