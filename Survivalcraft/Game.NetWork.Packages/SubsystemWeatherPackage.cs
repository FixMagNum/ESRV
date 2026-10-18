namespace Game.NetWork.Packages;

public class SubsystemWeatherPackage : IPackage
{
	public double m_precipitationStartTime;

	public double m_precipitationEndTime;

	public float m_lightningIntensity;

	public double m_windStartTime;

	public double m_windEndTime;

	public float m_targetWindStrength;

	public string m_windDirection;

	public float m_targetLightningIntensity;

	public float m_currentWindStrength;

	public int m_currentSeason;

	public bool m_isGusty;

	public byte ID => 10;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public SubsystemWeatherPackage()
	{
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_precipitationStartTime);
		writer.Write(m_precipitationEndTime);
		writer.Write(m_lightningIntensity);
		writer.Write(m_targetLightningIntensity);
		writer.Write(m_windStartTime);
		writer.Write(m_windEndTime);
		writer.Write(m_targetWindStrength);
		writer.Write(m_windDirection ?? "N");
		writer.Write(m_currentWindStrength);
		writer.Write(m_currentSeason);
		writer.Write(m_isGusty);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_precipitationStartTime = reader.ReadDouble();
		m_precipitationEndTime = reader.ReadDouble();
		m_lightningIntensity = reader.ReadSingle();
		m_targetLightningIntensity = reader.ReadSingle();
		m_windStartTime = reader.ReadDouble();
		m_windEndTime = reader.ReadDouble();
		m_targetWindStrength = reader.ReadSingle();
		m_windDirection = reader.ReadString();
		m_currentWindStrength = reader.ReadSingle();
		m_currentSeason = reader.ReadInt32();
		m_isGusty = reader.ReadBoolean();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if (!isServer || !((!(From?.PlayerData?.ServerManager)) ?? true) || From == null)
		{
			SubsystemWeather subsystemWeather = projectNet.FindSubsystem<SubsystemWeather>();
			subsystemWeather.m_precipitationStartTime = m_precipitationStartTime;
			subsystemWeather.m_precipitationEndTime = m_precipitationEndTime;
			subsystemWeather.m_lightningIntensity = m_lightningIntensity;
			subsystemWeather.TargetLightningIntensity = m_targetLightningIntensity;
			subsystemWeather.WindStartTime = m_windStartTime;
			subsystemWeather.WindEndTime = m_windEndTime;
			subsystemWeather.TargetWindStrength = m_targetWindStrength;
			subsystemWeather.WindDirection = m_windDirection;
			subsystemWeather.CurrentWindStrength = m_currentWindStrength;
			subsystemWeather.CurrentSeason = (SubsystemWeather.Season)m_currentSeason;
			subsystemWeather.IsGustyWind = m_isGusty;
		}
	}

	public SubsystemWeatherPackage(double start, double end, float light, float targetLight)
	{
		m_precipitationEndTime = end;
		m_precipitationStartTime = start;
		m_lightningIntensity = light;
		m_targetLightningIntensity = targetLight;
		m_windStartTime = 0.0;
		m_windEndTime = 0.0;
		m_targetWindStrength = 0f;
		m_windDirection = "N";
		m_currentSeason = 0;
		m_isGusty = false;
	}

	public SubsystemWeatherPackage(double precipitationStartTime, double precipitationEndTime, float lightningIntensity, float targetLightningIntensity, double windStartTime, double windEndTime, float targetWindStrength, string windDirection, float currentWindStrength, int currentSeason)
	{
		m_precipitationStartTime = precipitationStartTime;
		m_precipitationEndTime = precipitationEndTime;
		m_lightningIntensity = lightningIntensity;
		m_targetLightningIntensity = targetLightningIntensity;
		m_windStartTime = windStartTime;
		m_windEndTime = windEndTime;
		m_targetWindStrength = targetWindStrength;
		m_windDirection = windDirection;
		m_currentWindStrength = currentWindStrength;
		m_currentSeason = currentSeason;
	}

	public SubsystemWeatherPackage(double precipitationStartTime, double precipitationEndTime, float lightningIntensity, float targetLightningIntensity, double windStartTime, double windEndTime, float targetWindStrength, string windDirection, float currentWindStrength, int currentSeason, bool isGusty)
	{
		m_precipitationStartTime = precipitationStartTime;
		m_precipitationEndTime = precipitationEndTime;
		m_lightningIntensity = lightningIntensity;
		m_targetLightningIntensity = targetLightningIntensity;
		m_windStartTime = windStartTime;
		m_windEndTime = windEndTime;
		m_targetWindStrength = targetWindStrength;
		m_windDirection = windDirection;
		m_currentWindStrength = currentWindStrength;
		m_currentSeason = currentSeason;
		m_isGusty = isGusty;
	}
}
