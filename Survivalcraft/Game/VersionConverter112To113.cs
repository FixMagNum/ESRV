using System.IO;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public class VersionConverter112To113 : VersionConverter
{
	public override string SourceVersion => "1.12";

	public override string TargetVersion => "1.13";

	public override void ConvertProjectXml(XElement projectNode)
	{
		XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
		ProcessNode(projectNode);
	}

	public override void ConvertWorld(string directoryName)
	{
		string path = Storage.CombinePaths(directoryName, "Project.xml");
		XElement xElement;
		using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read))
		{
			xElement = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
		}
		ConvertProjectXml(xElement);
		using Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create);
		XmlUtils.SaveXmlToStream(xElement, stream2, null, throwOnError: true);
	}

	public void ProcessNode(XElement node)
	{
		foreach (XAttribute visibilityRangeYMultiplier in node.Attributes())
		{
			ProcessAttribute(visibilityRangeYMultiplier);
		}
		foreach (XElement subsystemMiningLightBehavior in node.Elements())
		{
			ProcessNode(subsystemMiningLightBehavior);
		}
	}

	public void ProcessAttribute(XAttribute attribute)
	{
		if (attribute.Name == "Value" && attribute.Value == "Dangerous")
		{
			attribute.Value = "Normal";
		}
	}
}
