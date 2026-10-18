namespace Game;

public class TruthTableCircuitBlock : RotateableMountedElectricElementBlock
{
	public const int Index = 188;

	public TruthTableCircuitBlock()
		: base("Models/Gates", "TruthTableCircuit", 0.5f)
	{
	}

	public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return new TruthTableCircuitElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));
	}

	public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (GetFace(value) == face)
		{
			ElectricConnectorDirection? num2 = SubsystemElectricity.GetConnectorDirection(GetFace(value), RotateableMountedElectricElementBlock.GetRotation(num), connectorFace);
			if (num2 == ElectricConnectorDirection.Right || num2 == ElectricConnectorDirection.Left || num2 == ElectricConnectorDirection.Bottom || num2 == ElectricConnectorDirection.Top)
			{
				return ElectricConnectorType.Input;
			}
			if (num2 == ElectricConnectorDirection.In)
			{
				return ElectricConnectorType.Output;
			}
		}
		return null;
	}
}
