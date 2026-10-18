using Engine;

namespace Game;

public class BatteryElectricElement : ElectricElement
{
	public BatteryElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
	}

	public override float GetOutputVoltage(int face)
	{
		Point3 flag = CellFaces[0].Point;
		return (float)BatteryBlock.GetVoltageLevel(Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(flag.X, flag.Y, flag.Z))) / 15f;
	}

	public override void OnNeighborBlockChanged(CellFace cellFace, int neighborX, int neighborY, int neighborZ)
	{
		int face = cellFace.Face;
		Point3 point = CellFace.FaceToPoint3(face);
		int num = cellFace.X - point.X;
		int num2 = cellFace.Y - point.Y;
		int num3 = cellFace.Z - point.Z;
		if (neighborX == num && neighborY == num2 && neighborZ == num3)
		{
			int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(num, num2, num3);
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
			if (!block.IsCollidable || block.IsFaceTransparent(SubsystemElectricity.SubsystemTerrain, face, cellValue))
			{
				SubsystemElectricity.SubsystemTerrain.DestroyCell(0, cellFace.X, cellFace.Y, cellFace.Z, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}
}
