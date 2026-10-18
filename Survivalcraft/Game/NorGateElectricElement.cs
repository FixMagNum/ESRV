using Engine;

namespace Game;

public class NorGateElectricElement : RotateableElectricElement
{
	public float m_voltage;

	public NorGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
	}

	public override float GetOutputVoltage(int face)
	{
		return m_voltage;
	}

	public override bool Simulate()
	{
		float readOnlyList = m_voltage;
		int index = 0;
		foreach (ElectricConnection connection in Connections)
		{
			if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != ElectricConnectorType.Input)
			{
				index |= (int)MathUtils.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
			}
		}
		m_voltage = (float)(~index & 0xF) / 15f;
		return m_voltage != readOnlyList;
	}
}
