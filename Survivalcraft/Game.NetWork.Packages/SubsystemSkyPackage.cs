using Engine;

namespace Game.NetWork.Packages;

public class SubsystemSkyPackage : IPackage
{
	public Vector3 LightningStrikePosition;

	public Vector3 m_direction;

	public bool isRequest;

	public byte LightningType;

	public byte ID => 9;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.Playing;

	public SubsystemSkyPackage()
	{
		LightningType = 0;
	}

	public SubsystemSkyPackage(Vector3 position)
	{
		LightningStrikePosition = position;
		LightningType = 0;
	}

	public SubsystemSkyPackage(Vector3 position, Vector3 direction)
	{
		LightningStrikePosition = position;
		m_direction = direction;
		isRequest = true;
		LightningType = 0;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(LightningStrikePosition);
		writer.Write(isRequest);
		writer.Write(m_direction);
		writer.Write(LightningType);
	}

	public void ReadData(PackageStreamReader reader)
	{
		LightningStrikePosition = reader.ReadVector3();
		isRequest = reader.ReadBoolean();
		m_direction = reader.ReadVector3();
		LightningType = reader.ReadByte();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		if ((projectNet.FindSubsystem<SubsystemGameInfo>().WorldSettings.GameMode != GameMode.Creative) & isServer)
		{
			Log.Information(From.PlayerData.Name + " 打算在非创造模式下使用闪电");
			return;
		}
		SubsystemSky subsystemSky = projectNet.FindSubsystem<SubsystemSky>();
		if (isRequest)
		{
			projectNet.FindSubsystem<SubsystemWeather>().ManualLightingStrike(LightningStrikePosition, m_direction);
			subsystemSky.MakeLightningStrike(LightningStrikePosition, LightningType);
		}
		else
		{
			subsystemSky.NetMakeLightningStrike(LightningStrikePosition, LightningType);
		}
	}

	public SubsystemSkyPackage(Vector3 position, byte lightningType)
	{
		LightningStrikePosition = position;
		LightningType = lightningType;
	}

	public SubsystemSkyPackage(Vector3 position, Vector3 direction, byte lightningType)
	{
		LightningStrikePosition = position;
		m_direction = direction;
		isRequest = true;
		LightningType = lightningType;
	}
}
