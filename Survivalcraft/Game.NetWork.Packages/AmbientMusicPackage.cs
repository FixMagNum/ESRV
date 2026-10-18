namespace Game.NetWork.Packages;

public class AmbientMusicPackage : IPackage
{
	public string TrackName;

	public double MusicStartTime;

	public byte ID => 39;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public AmbientMusicPackage()
	{
	}

	public AmbientMusicPackage(string trackName, double startTime)
	{
		TrackName = trackName;
		MusicStartTime = startTime;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(TrackName);
		writer.Write(MusicStartTime);
	}

	public void ReadData(PackageStreamReader reader)
	{
		TrackName = reader.ReadString();
		MusicStartTime = reader.ReadDouble();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindSubsystem<SubsystemAmbientMusic>()?.SyncMusicFromPackage(TrackName, MusicStartTime);
	}
}
