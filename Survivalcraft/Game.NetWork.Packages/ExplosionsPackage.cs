using Engine;

namespace Game.NetWork.Packages;

public class ExplosionsPackage : IPackage
{
	public Vector3 m_position;

	public float m_level;

	public float m_delay;

	public byte ID => 25;

	public IClient To { get; set; }

	public IClient Except { get; set; }

	public IClient From { get; set; }

	public ClientState MinNeedState => ClientState.ProjectLoaded;

	public ExplosionsPackage()
	{
	}

	public ExplosionsPackage(Vector3 position, float level, float delay)
	{
		m_position = position;
		m_level = level;
		m_delay = delay;
	}

	public void WriteData(PackageStreamWriter writer)
	{
		writer.Write(m_position);
		writer.Write(m_level);
		writer.Write(m_delay);
	}

	public void ReadData(PackageStreamReader reader)
	{
		m_position = reader.ReadVector3();
		m_level = reader.ReadSingle();
		m_delay = reader.ReadSingle();
	}

	public void Handle(ProjectNet projectNet, NetNode netNode, bool isServer)
	{
		projectNet.FindSubsystem<SubsystemExplosions>().PlayExplosionSound(m_position, m_level, m_delay, playExplosionSound: true);
	}
}
