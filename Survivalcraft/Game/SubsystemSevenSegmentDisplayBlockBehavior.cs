using Engine;

namespace Game;

public class SubsystemSevenSegmentDisplayBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[1] { 185 };

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int x = raycastResult.CellFace.X;
		int y = raycastResult.CellFace.Y;
		int z = raycastResult.CellFace.Z;
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int data = Terrain.ExtractData(cellValue);
		int mode = (SevenSegmentDisplayBlock.GetMode(data) + 1) % 3;
		int data2 = SevenSegmentDisplayBlock.SetMode(data, mode);
		int value = Terrain.ReplaceData(cellValue, data2);
		SubsystemTerrain.ChangeCell(x, y, z, value);
		SubsystemTerrain.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound("Audio/Click", 1f, 0f, new Vector3(x, y, z), 2f, autoDelay: true);
		return true;
	}
}
