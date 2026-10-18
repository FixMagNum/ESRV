using Engine;

namespace Game;

public class RealTimeClockElectricElement : RotateableElectricElement
{
	public SubsystemTimeOfDay m_subsystemTimeOfDay;

	public int m_lastClockValue = -1;

	public const int m_periodsPerDay = 4096;

	public RealTimeClockElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
		m_subsystemTimeOfDay = SubsystemElectricity.Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
	}

	public override float GetOutputVoltage(int face)
	{
		ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, Rotation, face);
		if (connectorDirection.HasValue)
		{
			if (connectorDirection.Value == ElectricConnectorDirection.Top)
			{
				return (float)(GetClockValue() & 0xF) / 15f;
			}
			if (connectorDirection.Value == ElectricConnectorDirection.Right)
			{
				return (float)((GetClockValue() >> 4) & 0xF) / 15f;
			}
			if (connectorDirection.Value == ElectricConnectorDirection.Bottom)
			{
				return (float)((GetClockValue() >> 8) & 0xF) / 15f;
			}
			if (connectorDirection.Value == ElectricConnectorDirection.Left)
			{
				return (float)((GetClockValue() >> 12) & 0xF) / 15f;
			}
			if (connectorDirection.Value == ElectricConnectorDirection.In)
			{
				return (float)((GetClockValue() >> 16) & 0xF) / 15f;
			}
		}
		return 0f;
	}

	public override bool Simulate()
	{
		double temperature = m_subsystemTimeOfDay.Day;
		int humidity = (int)(((MathUtils.Ceiling(temperature * 4096.0) + 0.5) / 4096.0 - temperature) * 1200.0 / 0.009999999776482582);
		int num2 = MathUtils.Max(SubsystemElectricity.FrameStartCircuitStep + humidity, SubsystemElectricity.CircuitStep + 1);
		SubsystemElectricity.QueueElectricElementForSimulation(this, num2);
		int clockValue = GetClockValue();
		if (clockValue != m_lastClockValue)
		{
			m_lastClockValue = clockValue;
			return true;
		}
		return false;
	}

	public int GetClockValue()
	{
		return (int)(m_subsystemTimeOfDay.Day * 4096.0);
	}
}
