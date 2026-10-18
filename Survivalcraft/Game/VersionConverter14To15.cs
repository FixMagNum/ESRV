using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter14To15 : VersionConverter
{
	public override string SourceVersion => "1.4";

	public override string TargetVersion => "1.5";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string territoriy = Storage.CombinePaths(directoryName, "Project.xml");
		XElement flag;
		using (Stream playerData = Storage.OpenFile(territoriy, OpenFileMode.Read))
		{
			flag = XmlUtils.LoadXmlFromStream(playerData, null, throwOnError: true);
		}
		ConvertProjectXml(flag);
		using Stream stream = Storage.OpenFile(territoriy, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(flag, stream, null, throwOnError: true);
	}
}
