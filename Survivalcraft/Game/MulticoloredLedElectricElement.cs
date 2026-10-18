using Engine;

namespace Game;

public class MulticoloredLedElectricElement : MountedElectricElement
{
	public SubsystemGlow m_subsystemGlow;

	public float m_voltage;

	public GlowPoint m_glowPoint;

	public MulticoloredLedElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
		m_subsystemGlow = subsystemElectricity.Project.FindSubsystem<SubsystemGlow>(throwOnError: true);
	}

	public override void OnAdded()
	{
		m_glowPoint = m_subsystemGlow.AddGlowPoint();
		CellFace num = CellFaces[0];
		int mountingFace = MulticoloredLedBlock.GetMountingFace(Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(num.X, num.Y, num.Z)));
		Vector3 vector = new Vector3((float)num.X + 0.5f, (float)num.Y + 0.5f, (float)num.Z + 0.5f);
		m_glowPoint.Position = vector - 0.4375f * CellFace.FaceToVector3(mountingFace);
		m_glowPoint.Forward = CellFace.FaceToVector3(mountingFace);
		m_glowPoint.Up = ((mountingFace < 4) ? Vector3.UnitY : Vector3.UnitX);
		m_glowPoint.Right = Vector3.Cross(m_glowPoint.Forward, m_glowPoint.Up);
		m_glowPoint.Color = Color.Transparent;
		m_glowPoint.Size = 0.0324f;
		m_glowPoint.FarSize = 0.0324f;
		m_glowPoint.FarDistance = 0f;
		m_glowPoint.Type = GlowPointType.Square;
	}

	public override void OnRemoved()
	{
		m_subsystemGlow.RemoveGlowPoint(m_glowPoint);
	}

	public override bool Simulate()
	{
		float batch = m_voltage;
		m_voltage = 0f;
		foreach (ElectricConnection num2 in Connections)
		{
			if (num2.ConnectorType != ElectricConnectorType.Output && num2.NeighborConnectorType != ElectricConnectorType.Input)
			{
				m_voltage = MathUtils.Max(m_voltage, num2.NeighborElectricElement.GetOutputVoltage(num2.NeighborConnectorFace));
			}
		}
		if (m_voltage != batch)
		{
			int num3 = (int)MathUtils.Round(m_voltage * 15f);
			m_glowPoint.Color = ((num3 >= 8) ? LedBlock.LedColors[MathUtils.Clamp(num3 - 8, 0, 7)] : Color.Transparent);
		}
		return false;
	}
}
