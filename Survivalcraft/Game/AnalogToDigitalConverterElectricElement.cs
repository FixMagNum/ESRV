using Engine;

namespace Game;

public class AnalogToDigitalConverterElectricElement : RotateableElectricElement
{
	public int m_bits;

	public AnalogToDigitalConverterElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
	}

	public override float GetOutputVoltage(int face)
	{
		ElectricConnectorDirection? data = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, Rotation, face);
		if (data.HasValue)
		{
			if (data.Value == ElectricConnectorDirection.Top)
			{
				return ((m_bits & 1) != 0) ? 1 : 0;
			}
			if (data.Value == ElectricConnectorDirection.Right)
			{
				return ((m_bits & 2) != 0) ? 1 : 0;
			}
			if (data.Value == ElectricConnectorDirection.Bottom)
			{
				return ((m_bits & 4) != 0) ? 1 : 0;
			}
			if (data.Value == ElectricConnectorDirection.Left)
			{
				return ((m_bits & 8) != 0) ? 1 : 0;
			}
		}
		return 0f;
	}

	public override bool Simulate()
	{
		int num = m_bits;
		int rotation = Rotation;
		foreach (ElectricConnection connection in Connections)
		{
			if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != ElectricConnectorType.Input)
			{
				ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, rotation, connection.ConnectorFace);
				if (connectorDirection.HasValue && connectorDirection.Value == ElectricConnectorDirection.In)
				{
					float outputVoltage = connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace);
					m_bits = (int)MathUtils.Round(outputVoltage * 15f);
				}
			}
		}
		return m_bits != num;
	}
}
