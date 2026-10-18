using System.Collections.Generic;

namespace Game;

public class ModInfo
{
	public string Name;

	public string Version;

	public string ApiVersion;

	public string Description;

	public string ScVersion;

	public string Link;

	public string Author;

	public string PackageName;

	public List<string> Dependencies = new List<string>();

	public override int GetHashCode()
	{
		return (PackageName + ApiVersion + Version).GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is ModInfo)
		{
			return obj.GetHashCode() == GetHashCode();
		}
		return false;
	}
}
