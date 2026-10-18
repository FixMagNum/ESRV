using Engine;

namespace Game;

public abstract class RotateableElectricElement : MountedElectricElement
{
	public int Rotation
	{
		get
		{
			CellFace cellFace = CellFaces[0];
			return RotateableMountedElectricElementBlock.GetRotation(Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z)));
		}
		set
		{
			CellFace humidity = CellFaces[0];
			int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(humidity.X, humidity.Y, humidity.Z);
			int num = Terrain.ReplaceData(cellValue, RotateableMountedElectricElementBlock.SetRotation(Terrain.ExtractData(cellValue), value % 4));
			SubsystemElectricity.SubsystemTerrain.ChangeCell(humidity.X, humidity.Y, humidity.Z, num);
			SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(humidity.X, humidity.Y, humidity.Z), 2f, autoDelay: true);
		}
	}

	public RotateableElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
		: base(subsystemElectricity, cellFace)
	{
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int humidity = Rotation + 1;
		Rotation = humidity;
		return true;
	}
}
