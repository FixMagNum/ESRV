using Engine;

namespace Game;

public class SubsystemLoomBlockBehavior : SubsystemBlockBehavior
{
	public override int[] HandledBlocks => new int[1] { 441 };

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z, ComponentMiner miner)
	{
		int data = Terrain.ExtractData(value);
		bool flag = LoomBlock.IsMainPart(data);
		int rotation = LoomBlock.GetRotation(data);
		Point3[] array = new Point3[4]
		{
			new Point3(0, 0, 1),
			new Point3(1, 0, 0),
			new Point3(0, 0, -1),
			new Point3(-1, 0, 0)
		};
		Point3 point = (flag ? array[rotation] : (-array[rotation]));
		Point3 point2 = new Point3(x, y, z) + point;
		if (SubsystemTerrain.Terrain.GetCellContents(point2.X, point2.Y, point2.Z) != 441)
		{
			int data2 = LoomBlock.SetIsMainPart(LoomBlock.SetRotation(0, rotation), !flag);
			SubsystemTerrain.ChangeCell(point2.X, point2.Y, point2.Z, Terrain.MakeBlockValue(441, 0, data2));
		}
		if (flag && SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(x, y, z) == null)
		{
			SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).CreateBlockEntity("Loom", new Point3(x, y, z), miner);
		}
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		bool flag = LoomBlock.IsMainPart(data);
		int rotation = LoomBlock.GetRotation(data);
		Point3[] array = new Point3[4]
		{
			new Point3(0, 0, 1),
			new Point3(1, 0, 0),
			new Point3(0, 0, -1),
			new Point3(-1, 0, 0)
		};
		Point3 point = (flag ? array[rotation] : (-array[rotation]));
		Point3 point2 = new Point3(x, y, z) + point;
		if (SubsystemTerrain.Terrain.GetCellContents(point2.X, point2.Y, point2.Z) == 441)
		{
			SubsystemTerrain.DestroyCell(0, point2.X, point2.Y, point2.Z, 0, noDrop: false, noParticleSystem: false);
		}
		if (!flag)
		{
			return;
		}
		ComponentBlockEntity blockEntity = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(x, y, z);
		if (blockEntity == null)
		{
			return;
		}
		blockEntity.Entity.FindComponent<ComponentLoom>()?.SafeguardDesign();
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
		foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
		{
			item.DropAllItems(position);
		}
		SubsystemTerrain.Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int data = Terrain.ExtractData(raycastResult.Value);
		bool flag = LoomBlock.IsMainPart(data);
		int rotation = LoomBlock.GetRotation(data);
		Point3 point = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (!flag)
		{
			Point3[] array = new Point3[4]
			{
				new Point3(0, 0, 1),
				new Point3(1, 0, 0),
				new Point3(0, 0, -1),
				new Point3(-1, 0, 0)
			};
			point -= array[rotation];
		}
		ComponentBlockEntity blockEntity = SubsystemTerrain.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true).GetBlockEntity(point.X, point.Y, point.Z);
		if (blockEntity != null && componentMiner.ComponentPlayer != null)
		{
			ComponentLoom componentLoom = blockEntity.Entity.FindComponent<ComponentLoom>();
			if (componentLoom != null)
			{
				componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new LoomWidget(componentMiner.Inventory, componentLoom);
				return true;
			}
		}
		return false;
	}
}
