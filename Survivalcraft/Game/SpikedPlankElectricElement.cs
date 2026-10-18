namespace Game;

public class SpikedPlankElectricElement : MountedElectricElement
{
	public int m_lastChangeCircuitStep;

	public bool m_needsReset;

	public float m_voltage;

	public SpikedPlankElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
		m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
		m_needsReset = true;
	}

	public override bool Simulate()
	{
		int num = SubsystemElectricity.CircuitStep - m_lastChangeCircuitStep;
		float num3 = ((CalculateHighInputsCount() > 0) ? 1 : 0);
		if (ElectricElement.IsSignalHigh(num3) != ElectricElement.IsSignalHigh(m_voltage))
		{
			m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
		}
		m_voltage = num3;
		if (!ElectricElement.IsSignalHigh(m_voltage))
		{
			m_needsReset = false;
		}
		if (!m_needsReset)
		{
			if (num >= 10)
			{
				if (ElectricElement.IsSignalHigh(m_voltage))
				{
					CellFace vector = CellFaces[0];
					int vector2 = Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(vector.X, vector.Y, vector.Z));
					SubsystemElectricity.Project.FindSubsystem<SubsystemSpikesBlockBehavior>(throwOnError: true).RetractExtendSpikes(vector.X, vector.Y, vector.Z, !SpikedPlankBlock.GetSpikesState(vector2));
				}
			}
			else
			{
				SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 10 - num);
			}
		}
		return false;
	}
}
