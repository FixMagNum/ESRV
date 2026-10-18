using Engine;

namespace Game;

public class NandGateElectricElement : RotateableElectricElement
{
	public float m_voltage;

	public NandGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
	}

	public override float GetOutputVoltage(int face)
	{
		return m_voltage;
	}

	public override bool Simulate()
	{
		float flag = m_voltage;
		int echoIntensity = 0;
		int num2 = 15;
		foreach (ElectricConnection connection in Connections)
		{
			if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != ElectricConnectorType.Input)
			{
				num2 &= (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
				echoIntensity++;
			}
		}
		m_voltage = ((echoIntensity == 2) ? ((float)(~num2 & 0xF) / 15f) : 0f);
		return m_voltage != flag;
	}
}
