using Engine;
using Game.NetWork;

namespace Game;

public class SubsystemBottomSuckerBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[2] { 226, 229 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int num = FocalorsAttachedBlock.GetFace(Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
		Point3 num2 = CellFace.FaceToPoint3(num);
		int num3 = x - num2.X;
		int sicknessProbability = y - num2.Y;
		int value2 = z - num2.Z;
		int flag = SubsystemTerrain.Terrain.GetCellValue(num3, sicknessProbability, value2);
		if (!IsSupport(flag, CellFace.OppositeFace(num)))
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
	{
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z)) == 226 && CommonLib.WorkType != WorkType.Client)
		{
			componentBody.Entity.FindComponent<ComponentCreature>()?.ComponentHealth.Injure(0.01f * MathUtils.Abs(velocity), null, ignoreInvulnerability: false, "Spiked by a sea creature");
		}
	}

	public bool IsSupport(int value, int face)
	{
		Block x = BlocksManager.Blocks[Terrain.ExtractContents(value)];
		if (x.IsCollidable)
		{
			return !x.IsFaceTransparent(SubsystemTerrain, face, value);
		}
		return false;
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
		Point3[] gameTimeDelta = new Point3[6]
		{
			new Point3(0, 1, 0),
			new Point3(0, -1, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
		for (int num = 0; num < gameTimeDelta.Length; num++)
		{
			Point3 lastJumpOrder = gameTimeDelta[num];
			int num2 = SubsystemTerrain.Terrain.GetCellValue(x + lastJumpOrder.X, y + lastJumpOrder.Y, z + lastJumpOrder.Z);
			int flag = Terrain.ExtractContents(num2);
			if (flag == 18)
			{
				return true;
			}
			if (BlocksManager.Blocks[flag] is IWaterloggable flag2 && flag2.IsWaterLogged(Terrain.ExtractData(num2)))
			{
				return true;
			}
		}
		return false;
	}
}
