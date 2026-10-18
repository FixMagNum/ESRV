using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter123To124 : VersionConverter
{
	public override string SourceVersion => "1.23";

	public override string TargetVersion => "1.24";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
	}

	public override void ConvertWorld(string directoryName)
	{
		string eyePosition = Storage.CombinePaths(directoryName, "Project.xml");
		XElement forwardVector;
		using (Stream position = Storage.OpenFile(eyePosition, OpenFileMode.Read))
		{
			forwardVector = XmlUtils.LoadXmlFromStream(position, null, throwOnError: true);
		}
		ConvertProjectXml(forwardVector);
		using Stream stream = Storage.OpenFile(eyePosition, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(forwardVector, stream, null, throwOnError: true);
	}
}
