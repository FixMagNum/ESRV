using Engine;

namespace Game;

public class SubsystemWaterPlantBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[2] { 232, 233 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		bool flag = (Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) & 0x400) != 0;
		int data = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z));
		if (!flag || (data != 2 && data != 7 && data != 8 && data != 6 && data != 232 && data != 233 && data != 391))
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		CheckAndSetWaterlogged(value, x, y, z);
	}

	private void CheckAndSetWaterlogged(int value, int x, int y, int z)
	{
		if (!(BlocksManager.Blocks[Terrain.ExtractContents(value)] is IWaterloggable waterloggable))
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		if (!waterloggable.IsWaterLogged(data))
		{
			bool flag = IsSurroundedByWater(x, y, z);
			if (waterloggable.IsWaterLogged(data) != flag)
			{
				int data2 = waterloggable.SetWaterLogged(data, flag);
				SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(value, data2));
			}
		}
	}

	private bool IsSurroundedByWater(int x, int y, int z)
	{
		Point3[] array = new Point3[6]
		{
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Point3 point = array[i];
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
			int num = Terrain.ExtractContents(cellValue);
			if (num == 18)
			{
				return true;
			}
			if (BlocksManager.Blocks[num] is IWaterloggable waterloggable && waterloggable.IsWaterLogged(Terrain.ExtractData(cellValue)))
			{
				return true;
			}
		}
		return false;
	}
}
