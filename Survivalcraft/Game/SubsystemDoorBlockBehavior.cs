using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemDoorBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemElectricity m_subsystemElectricity;

	public static Random m_random = new Random();

	private int m_onBlockAddedDepth;

	public override int[] HandledBlocks => new int[3] { 56, 57, 58 };

	public bool OpenCloseDoor(int x, int y, int z, bool open)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		if (BlocksManager.Blocks[num] is DoorBlock)
		{
			int data = DoorBlock.SetOpen(Terrain.ExtractData(cellValue), open);
			int value = Terrain.ReplaceData(cellValue, data);
			SubsystemTerrain.ChangeCell(x, y, z, value);
			string name = (open ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose");
			SubsystemTerrain.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound(name, 0.7f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 4f, autoDelay: true);
			return true;
		}
		return false;
	}

	public bool IsDoorElectricallyConnected(int x, int y, int z)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		int data = Terrain.ExtractData(cellValue);
		if (BlocksManager.Blocks[num] is DoorBlock)
		{
			int num2 = (DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z) ? y : (y - 1));
			for (int i = num2; i <= num2 + 1; i++)
			{
				ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(x, i, z, DoorBlock.GetHingeFace(data));
				if (electricElement != null && electricElement.Connections.Count > 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		CellFace cellFace = raycastResult.CellFace;
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
		int num = Terrain.ExtractContents(cellValue);
		int data = Terrain.ExtractData(cellValue);
		if (num == 56 || !IsDoorElectricallyConnected(cellFace.X, cellFace.Y, cellFace.Z))
		{
			bool open = DoorBlock.GetOpen(data);
			return OpenCloseDoor(cellFace.X, cellFace.Y, cellFace.Z, !open);
		}
		return true;
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		if (m_onBlockAddedDepth >= 256)
		{
			return;
		}
		int node = SubsystemTerrain.Terrain.GetCellContents(x, y + 1, z);
		Block i = BlocksManager.Blocks[node];
		if ((node == 0 || i is WaterBlock || i is FocalorsFluidBlock) && !DoorBlock.IsTopPart(SubsystemTerrain.Terrain, x, y, z))
		{
			bool num = i is WaterBlock || i is FocalorsFluidBlock;
			int widget = value;
			if (num)
			{
				widget = Terrain.ReplaceData(widget, Terrain.ExtractData(widget) | 0x400);
			}
			m_onBlockAddedDepth++;
			try
			{
				SubsystemTerrain.ChangeCell(x, y + 1, z, widget);
				return;
			}
			finally
			{
				m_onBlockAddedDepth--;
			}
		}
		int j = Terrain.ExtractData(value);
		j = DoorBlock.SetOpen(j, open: false);
		SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(value, j));
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		if (DoorBlock.IsTopPart(SubsystemTerrain.Terrain, x, y, z))
		{
			SubsystemTerrain.ChangeCell(x, y - 1, z, 0);
		}
		if (DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z))
		{
			SubsystemTerrain.ChangeCell(x, y + 1, z, 0);
		}
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int block = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(block);
		Block obj = BlocksManager.Blocks[num];
		int child = Terrain.ExtractData(block);
		if (!(obj is DoorBlock) || neighborX != x || neighborY != y || neighborZ != z)
		{
			return;
		}
		if (DoorBlock.IsBottomPart(SubsystemTerrain.Terrain, x, y, z))
		{
			int widgetCell = y + 1;
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, widgetCell, z);
			if (Terrain.ExtractContents(cellValue) == num)
			{
				int num2 = Terrain.ExtractData(cellValue);
				bool flag = (num2 & 0x400) != 0;
				int num3 = child & -1025;
				if (flag)
				{
					num3 |= 0x400;
				}
				if (num2 != num3)
				{
					int value = Terrain.ReplaceData(cellValue, num3);
					SubsystemTerrain.ChangeCell(x, widgetCell, z, value);
				}
			}
		}
		if (!DoorBlock.IsTopPart(SubsystemTerrain.Terrain, x, y, z))
		{
			return;
		}
		int y2 = y - 1;
		int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y2, z);
		if (Terrain.ExtractContents(cellValue2) == num)
		{
			int num4 = Terrain.ExtractData(cellValue2);
			bool flag2 = (num4 & 0x400) != 0;
			int num5 = child & -1025;
			if (flag2)
			{
				num5 |= 0x400;
			}
			if (num4 != num5)
			{
				int value2 = Terrain.ReplaceData(cellValue2, num5);
				SubsystemTerrain.ChangeCell(x, y2, z, value2);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(throwOnError: true);
	}

	public override bool OnInteract(MovingBlocksRaycastResult raycastResult, Point3 offset, ComponentMiner componentMiner)
	{
		if (!(raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet vector))
		{
			return false;
		}
		int rotation = 0;
		for (int i = 0; i < vector.Blocks.Count; i++)
		{
			if (vector.Blocks[i].Offset == offset)
			{
				rotation = vector.Blocks[i].Value;
				break;
			}
		}
		if (rotation == 0)
		{
			return false;
		}
		int num = Terrain.ExtractContents(rotation);
		int num2 = Terrain.ExtractData(rotation);
		if (num != 56 && num != 57 && num != 58)
		{
			return false;
		}
		bool mode = !DoorBlock.GetOpen(num2);
		int data = DoorBlock.SetOpen(num2, mode);
		int cellContents = Terrain.ReplaceData(rotation, data);
		SubsystemFloatingBlockBehavior block = Project.FindSubsystem<SubsystemFloatingBlockBehavior>(throwOnError: true);
		if (block == null)
		{
			return false;
		}
		block.ModifyMovingBlock(vector, offset, cellContents);
		string isWaterLogged = (mode ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose");
		componentMiner.m_subsystemAudio.PlaySound(isWaterLogged, 0.7f, 0f, raycastResult.HitPoint(), 4f, autoDelay: true);
		componentMiner.Poke(forceRestart: false);
		return true;
	}

	private bool IsTopPart(int data)
	{
		return (data & 8) != 0;
	}
}
