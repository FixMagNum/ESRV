namespace Game;

public class NandGateBlock : RotateableMountedElectricElementBlock
{
	public const int Index = 134;

	public NandGateBlock()
		: base("Models/Gates", "NandGate", 0.5f)
	{
	}

	public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return new NandGateElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));
	}

	public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int i = Terrain.ExtractData(value);
		if (GetFace(value) == face)
		{
			ElectricConnectorDirection? j = SubsystemElectricity.GetConnectorDirection(GetFace(value), RotateableMountedElectricElementBlock.GetRotation(i), connectorFace);
			if (j == ElectricConnectorDirection.Right || j == ElectricConnectorDirection.Left)
			{
				return ElectricConnectorType.Input;
			}
			if (j == ElectricConnectorDirection.Top || j == ElectricConnectorDirection.In)
			{
				return ElectricConnectorType.Output;
			}
		}
		return null;
	}
}
