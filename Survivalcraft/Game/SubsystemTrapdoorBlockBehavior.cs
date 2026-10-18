using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemTrapdoorBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemElectricity m_subsystemElectricity;

	public static Random m_random;

	public override int[] HandledBlocks => new int[2] { 83, 84 };

	public bool IsTrapdoorElectricallyConnected(int x, int y, int z)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		int data = Terrain.ExtractData(cellValue);
		if (BlocksManager.Blocks[num] is TrapdoorBlock)
		{
			ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(x, y, z, TrapdoorBlock.GetMountingFace(data));
			if (electricElement != null && electricElement.Connections.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool OpenCloseTrapdoor(int x, int y, int z, bool open)
	{
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		if (BlocksManager.Blocks[num] is TrapdoorBlock)
		{
			int data = TrapdoorBlock.SetOpen(Terrain.ExtractData(cellValue), open);
			int value = Terrain.ReplaceData(cellValue, data);
			SubsystemTerrain.ChangeCell(x, y, z, value);
			string name = (open ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose");
			SubsystemTerrain.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound(name, 0.7f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 4f, autoDelay: true);
			return true;
		}
		return false;
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		CellFace componentPlayer = raycastResult.CellFace;
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(componentPlayer.X, componentPlayer.Y, componentPlayer.Z);
		int widgetInputDevice = Terrain.ExtractContents(cellValue);
		int num = Terrain.ExtractData(cellValue);
		if (widgetInputDevice == 83 || !IsTrapdoorElectricallyConnected(componentPlayer.X, componentPlayer.Y, componentPlayer.Z))
		{
			bool r = TrapdoorBlock.GetOpen(num);
			return OpenCloseTrapdoor(componentPlayer.X, componentPlayer.Y, componentPlayer.Z, !r);
		}
		return true;
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int widgetInputDevice2 = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(widgetInputDevice2);
		Block obj = BlocksManager.Blocks[num];
		int widgetInputDevice3 = Terrain.ExtractData(widgetInputDevice2);
		if (obj is TrapdoorBlock)
		{
			TrapdoorBlock.GetRotation(widgetInputDevice3);
			TrapdoorBlock.GetUpsideDown(widgetInputDevice3);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(throwOnError: true);
	}

	static SubsystemTrapdoorBlockBehavior()
	{
		m_random = new Random();
	}

	public override bool OnInteract(MovingBlocksRaycastResult raycastResult, Point3 offset, ComponentMiner componentMiner)
	{
		if (!(raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet movingBlockSet))
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < movingBlockSet.Blocks.Count; i++)
		{
			if (movingBlockSet.Blocks[i].Offset == offset)
			{
				num = movingBlockSet.Blocks[i].Value;
				break;
			}
		}
		if (num == 0)
		{
			return false;
		}
		int num2 = Terrain.ExtractContents(num);
		int data = Terrain.ExtractData(num);
		if (num2 != 83 && num2 != 84)
		{
			return false;
		}
		bool open = TrapdoorBlock.GetOpen(data);
		int data2 = TrapdoorBlock.SetOpen(data, !open);
		int newValue = Terrain.ReplaceData(num, data2);
		Project.FindSubsystem<SubsystemFloatingBlockBehavior>(throwOnError: true).ModifyMovingBlock(movingBlockSet, offset, newValue);
		string name = ((!open) ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose");
		componentMiner.m_subsystemAudio.PlaySound(name, 0.7f, 0f, raycastResult.HitPoint(), 4f, autoDelay: true);
		componentMiner.Poke(forceRestart: false);
		return true;
	}
}
