using GameEntitySystem;

namespace Game;

public class ComponentNetworkTag : Component
{
	public readonly ushort Id;

	public ComponentNetworkTag()
	{
	}

	public ComponentNetworkTag(ushort id)
	{
		Id = id;
	}
}
