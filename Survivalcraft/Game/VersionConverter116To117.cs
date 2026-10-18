using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter116To117 : VersionConverter
{
	public override string SourceVersion => "1.16";

	public override string TargetVersion => "1.17";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string path = Storage.CombinePaths(directoryName, "Project.xml");
		XElement movingBlockSet;
		using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read))
		{
			movingBlockSet = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
		}
		ConvertProjectXml(movingBlockSet);
		using Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(movingBlockSet, stream2, null, throwOnError: true);
	}
}
