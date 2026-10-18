using Engine;

namespace Game.NetWork.Packages;

public class SubsystemTimePackage : IPackage
{
	public double m_time;

	public double m_timeOfDayOffset;

	public float m_timeScale = 1f;

	public byte ID => 8;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public SubsystemTimePackage()
	{
	}

	public SubsystemTimePackage(double TotalElapsedGameTime, double TimeOfDayOffset)
	{
		m_time = TotalElapsedGameTime;
		m_timeOfDayOffset = TimeOfDayOffset;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_time);
		writer.Write(m_timeOfDayOffset);
		writer.Write(m_timeScale);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_time = reader.ReadDouble();
		m_timeOfDayOffset = reader.ReadDouble();
		try
		{
			m_timeScale = reader.ReadSingle();
		}
		catch
		{
			m_timeScale = 1f;
		}
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		SubsystemGameInfo subsystemGameInfo = projectNet.FindSubsystem<SubsystemGameInfo>();
		SubsystemTime subsystemTime = projectNet.FindSubsystem<SubsystemTime>();
		if (!isServer)
		{
			subsystemGameInfo.TotalElapsedGameTime = m_time;
			subsystemGameInfo.TimeOfDay.TimeOfDayOffset = m_timeOfDayOffset;
			subsystemTime.SetTimeControl(m_timeScale);
			return;
		}
		if (((!(From?.PlayerData?.ServerManager)) ?? true) && From != null)
		{
			Log.Information((From?.PlayerData?.Name ?? "unknown") + " tried to change time without permission");
			return;
		}
		subsystemTime.SetTimeControl(m_timeScale);
		if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			subsystemGameInfo.TotalElapsedGameTime = m_time;
			subsystemGameInfo.TimeOfDay.TimeOfDayOffset = m_timeOfDayOffset;
		}
		SubsystemTimePackage package = new SubsystemTimePackage(subsystemGameInfo.TotalElapsedGameTime, subsystemGameInfo.TimeOfDay.TimeOfDayOffset, subsystemTime.TimeControlFactor)
		{
			Except = From
		};
		CommonLib.Net.QueuePackage(package);
	}

	public SubsystemTimePackage(double TotalElapsedGameTime, double TimeOfDayOffset, float timeScale = 1f)
	{
		m_time = TotalElapsedGameTime;
		m_timeOfDayOffset = TimeOfDayOffset;
		m_timeScale = timeScale;
	}
}
