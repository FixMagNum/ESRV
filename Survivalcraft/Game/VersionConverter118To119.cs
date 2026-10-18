using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter118To119 : VersionConverter
{
	public override string SourceVersion => "1.18";

	public override string TargetVersion => "1.19";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string boundingBox = Storage.CombinePaths(directoryName, "Project.xml");
		XElement i;
		using (Stream movingBlockSet2 = Storage.OpenFile(boundingBox, OpenFileMode.Read))
		{
			i = XmlUtils.LoadXmlFromStream(movingBlockSet2, null, throwOnError: true);
		}
		ConvertProjectXml(i);
		using Stream stream = Storage.OpenFile(boundingBox, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(i, stream, null, throwOnError: true);
	}
}
