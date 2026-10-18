using Engine;

namespace Game;

public class SoundGeneratorElectricElement : RotateableElectricElement
{
	public SubsystemNoise m_subsystemNoise;

	public SubsystemParticles m_subsystemParticles;

	public SoundParticleSystem m_particleSystem;

	public Random m_random = new Random();

	public int m_lastToneInput;

	public double m_playAllowedTime;

	public string[] m_tones = new string[16]
	{
		"", "Bell", "Organ", "Ping", "String", "Trumpet", "Voice", "Piano", "PianoLong", "Drums",
		"", "", "", "", "", "Piano"
	};

	public int[] m_maxOctaves = new int[16]
	{
		0, 5, 5, 5, 5, 5, 5, 6, 6, 0,
		0, 0, 0, 0, 0, 6
	};

	public string[] m_drums = new string[10] { "Snare", "BassDrum", "ClosedHiHat", "PedalHiHat", "OpenHiHat", "LowTom", "HighTom", "CrashCymbal", "RideCymbal", "HandClap" };

	public SoundGeneratorElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
		m_subsystemNoise = subsystemElectricity.Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemParticles = subsystemElectricity.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		Vector3 vector = CellFace.FaceToVector3(cellFace.Face);
		Vector3 position = new Vector3(cellFace.Point) + new Vector3(0.5f) - 0.2f * vector;
		m_particleSystem = new SoundParticleSystem(subsystemElectricity.SubsystemTerrain, position, vector);
	}

	public override bool Simulate()
	{
		int point = 0;
		int fireData = 15;
		int value = 2;
		int num = 0;
		int rotation = Rotation;
		foreach (ElectricConnection connection in Connections)
		{
			if (connection.ConnectorType == ElectricConnectorType.Output || connection.NeighborConnectorType == ElectricConnectorType.Input)
			{
				continue;
			}
			ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, rotation, connection.ConnectorFace);
			if (connectorDirection.HasValue)
			{
				if (connectorDirection.Value == ElectricConnectorDirection.In || connectorDirection.Value == ElectricConnectorDirection.Bottom)
				{
					num = (int)MathUtils.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
				}
				else if (connectorDirection.Value == ElectricConnectorDirection.Left)
				{
					point = (int)MathUtils.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
				}
				else if (connectorDirection.Value == ElectricConnectorDirection.Right)
				{
					value = (int)MathUtils.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
				}
				else if (connectorDirection.Value == ElectricConnectorDirection.Top)
				{
					fireData = (int)MathUtils.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
				}
			}
		}
		if (m_lastToneInput == 0 && num != 0 && point != 15 && SubsystemElectricity.SubsystemTime.GameTime >= m_playAllowedTime)
		{
			m_playAllowedTime = SubsystemElectricity.SubsystemTime.GameTime + 0.07999999821186066;
			string text = m_tones[num];
			float num2 = 0f;
			string text2 = null;
			if (text == "Drums")
			{
				num2 = 1f;
				if (point >= 0 && point < m_drums.Length)
				{
					text2 = "Audio/SoundGenerator/Drums" + m_drums[point];
				}
			}
			else if (!string.IsNullOrEmpty(text))
			{
				float num3 = 130.8125f * MathUtils.Pow(1.0594631f, point + 12 * value);
				int num4 = 0;
				for (int i = 4; i <= m_maxOctaves[num]; i++)
				{
					float num5 = num3 / (523.25f * MathUtils.Pow(2f, i - 5));
					if (num4 == 0 || (num5 >= 0.5f && num5 < num2))
					{
						num4 = i;
						num2 = num5;
					}
				}
				text2 = $"Audio/SoundGenerator/{text}C{num4}";
			}
			if (num2 != 0f && !string.IsNullOrEmpty(text2))
			{
				CellFace cellFace = CellFaces[0];
				Vector3 position = new Vector3(cellFace.X, cellFace.Y, cellFace.Z);
				float volume = (float)fireData / 15f;
				float pitch = MathUtils.Clamp(MathUtils.Log(num2) / MathUtils.Log(2f), -1f, 1f);
				float minDistance = 0.5f + 5f * (float)fireData / 15f;
				SubsystemElectricity.SubsystemAudio.PlaySound(text2, volume, pitch, position, minDistance, autoDelay: true);
				float loudness = ((fireData < 8) ? 0.25f : 0.5f);
				float range = MathUtils.Lerp(2f, 20f, (float)fireData / 15f);
				m_subsystemNoise.MakeNoise(position, loudness, range);
				if (m_particleSystem.SubsystemParticles == null)
				{
					m_subsystemParticles.AddParticleSystem(m_particleSystem);
				}
				Vector3 hsv = new Vector3(22.5f * (float)point + m_random.Float(0f, 22f), 0.5f + (float)fireData / 30f, 1f);
				m_particleSystem.AddNote(new Color(Color.HsvToRgb(hsv)));
			}
		}
		m_lastToneInput = num;
		return false;
	}
}
