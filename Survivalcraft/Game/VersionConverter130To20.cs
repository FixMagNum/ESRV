using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter130To20 : VersionConverter
{
	public override string SourceVersion => "1.30";

	public override string TargetVersion => "2.0";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string mainPlayer = Storage.CombinePaths(directoryName, "Project.xml");
		XElement num;
		using (Stream playerInput = Storage.OpenFile(mainPlayer, OpenFileMode.Read))
		{
			num = XmlUtils.LoadXmlFromStream(playerInput, null, throwOnError: true);
		}
		ConvertProjectXml(num);
		using Stream stream = Storage.OpenFile(mainPlayer, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(num, stream, null, throwOnError: true);
	}
}
