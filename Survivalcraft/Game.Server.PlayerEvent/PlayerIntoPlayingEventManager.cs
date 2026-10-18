using System.Collections.Generic;

namespace Game.Server.PlayerEvent;

public static class PlayerIntoPlayingEventManager
{
	private static List<IPlayerIntoPlayingHandle> playerIntoPlayingHandleList = new List<IPlayerIntoPlayingHandle>();

	public static void AddObject(IPlayerIntoPlayingHandle handleObject)
	{
		if (!playerIntoPlayingHandleList.Contains(handleObject))
		{
			playerIntoPlayingHandleList.Add(handleObject);
		}
	}

	public static void RemoveObject(IPlayerIntoPlayingHandle handleObject)
	{
		if (playerIntoPlayingHandleList.Contains(handleObject))
		{
			playerIntoPlayingHandleList.Remove(handleObject);
		}
	}

	public static void PlayerIntoPlayingEvent(ComponentPlayer componentPlayer)
	{
		foreach (IPlayerIntoPlayingHandle playerIntoPlayingHandle in playerIntoPlayingHandleList)
		{
			playerIntoPlayingHandle.PlayerIntoPlayingEvent(componentPlayer);
		}
	}
}
