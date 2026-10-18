using Engine;

namespace Game;

public class SubsystemLadderBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[3] { 59, 213, 310 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) != 310)
		{
			int face = LadderBlock.GetFace(Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
			Point3 point = CellFace.FaceToPoint3(face);
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x - point.X, y - point.Y, z - point.Z);
			int num = Terrain.ExtractContents(cellValue);
			if (BlocksManager.Blocks[num].IsFaceTransparent(SubsystemTerrain, face, cellValue))
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}
}
