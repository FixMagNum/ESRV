using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemSnowBlockBehavior : SubsystemBlockBehavior
{
	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemWeather m_subsystemWeather;

	private Random m_random = new Random();

	private SubsystemAudio m_subsystemAudio;

	public override int[] HandledBlocks => new int[3] { 61, 348, 368 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int num = Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z));
		if (num == 368 && !CanSupportDune(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)))
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
		if (num == 348 && IsLeavesBlock(Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z))))
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
		if (num == 348 && Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)) == 348)
		{
			SubsystemTerrain.ChangeCell(x, y - 1, z, Terrain.MakeBlockValue(350));
		}
		if (!CanSupportSnow(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)))
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public static bool CanSupportSnow(int value)
	{
		int matrix4 = Terrain.ExtractContents(value);
		if (matrix4 == 12 || matrix4 == 13)
		{
			return false;
		}
		Block matrix8 = BlocksManager.Blocks[matrix4];
		if (matrix8.IsTransparent && !(matrix8 is LeavesBlock) && !(matrix8 is SnowBlock))
		{
			return matrix8 is LargeSnowBlock;
		}
		return true;
	}

	public static bool IsLeavesBlock(int blockId)
	{
		if (blockId != 14 && blockId != 225)
		{
			return blockId == 256;
		}
		return true;
	}

	public static bool CanSupportDune(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num != 7)
		{
			return BlocksManager.Blocks[num] is SandBlock;
		}
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int activeBlockValue = componentMiner.ActiveBlockValue;
		if (Terrain.ExtractContents(activeBlockValue) == 61)
		{
			int count = MathUtils.Max(1, Terrain.ExtractData(activeBlockValue));
			if (AddSnowLayers(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z, count))
			{
				componentMiner.RemoveActiveTool(1);
				return true;
			}
		}
		return false;
	}

	public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
	{
		if (!worldItem.ToRemove)
		{
			int num = Terrain.ExtractContents(worldItem.Value);
			int num2 = 0;
			switch (num)
			{
			case 85:
				num2 = 1;
				break;
			case 61:
				num2 = MathUtils.Max(1, Terrain.ExtractData(worldItem.Value));
				break;
			}
			if (num2 > 0 && AddSnowLayers(cellFace.X, cellFace.Y, cellFace.Z, num2))
			{
				worldItem.ToRemove = true;
			}
		}
	}

	private bool AddSnowLayers(int x, int y, int z, int count)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractData(cellValue) + count;
		m_subsystemAudio.PlayRandomSound("Audio/Footsteps/Snow", 0.5f, 0f, new Vector3(x, y, z), 2f, autoDelay: true);
		if (num >= 15)
		{
			SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(348));
		}
		else
		{
			int value = Terrain.ReplaceData(cellValue, num);
			SubsystemTerrain.ChangeCell(x, y, z, value);
		}
		return true;
	}
}
