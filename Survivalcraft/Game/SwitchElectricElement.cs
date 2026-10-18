using Engine;

namespace Game;

public class SwitchElectricElement : MountedElectricElement
{
	public float m_voltage;

	public SwitchElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int value)
		: base(subsystemElectricity, cellFace)
	{
		m_voltage = (SwitchBlock.GetLeverState(value) ? 1 : 0);
	}

	public override float GetOutputVoltage(int face)
	{
		return m_voltage;
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		CellFace angularVelocity2 = CellFaces[0];
		int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(angularVelocity2.X, angularVelocity2.Y, angularVelocity2.Z);
		int value = SwitchBlock.SetLeverState(cellValue, !SwitchBlock.GetLeverState(cellValue));
		SubsystemElectricity.SubsystemTerrain.ChangeCell(angularVelocity2.X, angularVelocity2.Y, angularVelocity2.Z, value);
		SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(angularVelocity2.X, angularVelocity2.Y, angularVelocity2.Z), 2f, autoDelay: true);
		return true;
	}
}
