namespace Game.Server.PlayerEvent;

public interface IPlayerIntoPlayingHandle
{
	byte FirstLevel { get; }

	void PlayerIntoPlayingEvent(ComponentPlayer componentPlayer);
}
