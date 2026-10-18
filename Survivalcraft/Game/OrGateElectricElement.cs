using Engine;

namespace Game;

public class OrGateElectricElement : RotateableElectricElement
{
	public float m_voltage;

	public OrGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
	}

	public override float GetOutputVoltage(int face)
	{
		return m_voltage;
	}

	public override bool Simulate()
	{
		float num = m_voltage;
		int data = 0;
		foreach (ElectricConnection num2 in Connections)
		{
			if (num2.ConnectorType != ElectricConnectorType.Output && num2.NeighborConnectorType != ElectricConnectorType.Input)
			{
				data |= (int)MathUtils.Round(num2.NeighborElectricElement.GetOutputVoltage(num2.NeighborConnectorFace) * 15f);
			}
		}
		m_voltage = (float)data / 15f;
		return m_voltage != num;
	}
}
