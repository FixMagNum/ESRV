using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter129To130 : VersionConverter
{
	public override string SourceVersion => "1.29";

	public override string TargetVersion => "1.30";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string flag = Storage.CombinePaths(directoryName, "Project.xml");
		XElement activeBlockValue;
		using (Stream flag2 = Storage.OpenFile(flag, OpenFileMode.Read))
		{
			activeBlockValue = XmlUtils.LoadXmlFromStream(flag2, null, throwOnError: true);
		}
		ConvertProjectXml(activeBlockValue);
		using Stream num2 = Storage.OpenFile(flag, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(activeBlockValue, num2, null, throwOnError: true);
	}
}
