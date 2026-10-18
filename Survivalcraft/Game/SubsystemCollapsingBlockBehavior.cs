using System.Collections.Generic;
using System.Linq;
using Engine;
using Game.NetWork;
using TemplatesDatabase;

namespace Game;

public class SubsystemCollapsingBlockBehavior : SubsystemBlockBehavior
{
	public const string IdString = "CollapsingBlock";

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public SubsystemMovingBlocks m_subsystemMovingBlocks;

	public static int[] m_handledBlocks = new int[4] { 7, 6, 376, 377 };

	public SubsystemParticles m_subsystemParticles;

	public SubsystemFocalorsFluidBlockBehavior m_focalorsFluidBlockBehavior;

	public SubsystemPickables m_subsystemPickables;

	public override int[] HandledBlocks => m_handledBlocks;

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
		{
			TryCollapseColumn(new Point3(x, y, z));
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_focalorsFluidBlockBehavior = Project.FindSubsystem<SubsystemFocalorsFluidBlockBehavior>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemMovingBlocks.Stopped += MovingBlocksStopped;
		m_subsystemMovingBlocks.CollidedWithTerrain += MovingBlocksCollidedWithTerrain;
	}

	public void MovingBlocksCollidedWithTerrain(IMovingBlockSet movingBlockSet, Point3 p)
	{
		if (!(movingBlockSet.Id == "CollapsingBlock"))
		{
			return;
		}
		int cellValue = SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
		int num = Terrain.ExtractContents(cellValue);
		if (num != 104 && num != 18 && num != 92 && num != 391)
		{
			if (IsCollapseSupportBlock(cellValue))
			{
				movingBlockSet.Stop();
			}
			else if (IsCollapseDestructibleBlock(cellValue))
			{
				SubsystemTerrain.DestroyCell(0, p.X, p.Y, p.Z, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}

	public void MovingBlocksStopped(IMovingBlockSet movingBlockSet)
	{
		if (movingBlockSet.Id != "CollapsingBlock")
		{
			return;
		}
		Point3 cellValue = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.X), MathUtils.Round(movingBlockSet.Position.Y), MathUtils.Round(movingBlockSet.Position.Z));
		foreach (MovingBlock flag5 in movingBlockSet.Blocks)
		{
			Point3 point = cellValue + flag5.Offset;
			int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			switch (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z)))
			{
			case 18:
			case 92:
			{
				if (m_focalorsFluidBlockBehavior.TryDisplaceFluid(point.X, point.Y, point.Z, cellValue2))
				{
					SubsystemTerrain.DestroyCell(0, point.X, point.Y, point.Z, flag5.Value, noDrop: false, noParticleSystem: false);
					break;
				}
				Vector3 position = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.8f, (float)point.Z + 0.5f);
				Vector3 value = new Vector3(0f, 2f, 0f);
				m_subsystemPickables.AddPickable(flag5.Value, 1, position, value, null);
				break;
			}
			default:
				SubsystemTerrain.DestroyCell(0, point.X, point.Y, point.Z, flag5.Value, noDrop: false, noParticleSystem: false);
				break;
			case 104:
			case 391:
				break;
			}
		}
		m_subsystemMovingBlocks.RemoveMovingBlockSetLogic(movingBlockSet);
		if (movingBlockSet.Blocks.Count == 0)
		{
			return;
		}
		int flag6 = movingBlockSet.Blocks[0].Value;
		Block territoriy = BlocksManager.Blocks[Terrain.ExtractContents(flag6)];
		m_subsystemSoundMaterials.PlayImpactSound(flag6, movingBlockSet.Position, 1f);
		BoundingBox flag7 = movingBlockSet.BoundingBox(extendToFillCells: true);
		Vector3 worldSettings = new Vector3((flag7.Min.X + flag7.Max.X) * 0.5f, flag7.Min.Y + 1.0625f, (flag7.Min.Z + flag7.Max.Z) * 0.5f);
		float flag8 = MathUtils.Lerp(0.8f, 4f, MathUtils.Saturate((float)movingBlockSet.Blocks.Count / 30f));
		Vector3[] array = new Vector3[4]
		{
			new Vector3(0.7f, 0f, 0f),
			new Vector3(-0.7f, 0f, 0f),
			new Vector3(0f, 0f, 0.7f),
			new Vector3(0f, 0f, -0.7f)
		};
		foreach (Vector3 vector in array)
		{
			Vector3 position2 = worldSettings + vector;
			int x = Terrain.ToCell(position2.X);
			int y = Terrain.ToCell(position2.Y);
			int z = Terrain.ToCell(position2.Z);
			if (SubsystemTerrain.Terrain.GetCellContents(x, y, z) == 0)
			{
				BlockDebrisParticleSystem particleSystem = territoriy.CreateDebrisParticleSystem(SubsystemTerrain, position2, flag6, flag8 * 1.5f);
				m_subsystemParticles.AddParticleSystem(particleSystem);
			}
		}
	}

	public void TryCollapseColumn(Point3 p)
	{
		if (CommonLib.WorkType == WorkType.Client || p.Y <= 0)
		{
			return;
		}
		int isClient = SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y - 1, p.Z);
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z)) == 377 && Terrain.ExtractContents(isClient) == 377)
		{
			SubsystemTerrain.DestroyCell(0, p.X, p.Y, p.Z, 0, noDrop: false, noParticleSystem: false);
		}
		else
		{
			if (IsCollapseSupportBlock(isClient))
			{
				return;
			}
			List<MovingBlock> num = new List<MovingBlock>();
			for (int i = p.Y; i < 256; i++)
			{
				int num2 = SubsystemTerrain.Terrain.GetCellValue(p.X, i, p.Z);
				if (!IsCollapsibleBlock(num2))
				{
					break;
				}
				num.Add(new MovingBlock
				{
					Value = num2,
					Offset = new Point3(0, i - p.Y, 0)
				});
			}
			if (num.Count == 0 || m_subsystemMovingBlocks.AddMovingBlockSet(new Vector3(p), new Vector3(p.X, 0f - (float)num.Count - 1f, p.Z), 0f, 10f, 0.08f, new Vector2(0f), num, "CollapsingBlock", p, testCollision: true) == null)
			{
				return;
			}
			foreach (MovingBlock j in num)
			{
				Point3 cellDelta = p + j.Offset;
				SubsystemTerrain.ChangeCell(cellDelta.X, cellDelta.Y, cellDelta.Z, 0);
			}
		}
	}

	public static bool IsCollapsibleBlock(int value)
	{
		return m_handledBlocks.Contains(Terrain.ExtractContents(value));
	}

	public bool IsCollapseSupportBlock(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num == 0)
		{
			return false;
		}
		int data = Terrain.ExtractData(value);
		if (num == 104 || num == 391)
		{
			return false;
		}
		Block block = BlocksManager.Blocks[num];
		if (block is TrapdoorBlock)
		{
			if (TrapdoorBlock.GetUpsideDown(data))
			{
				return !TrapdoorBlock.GetOpen(data);
			}
			return false;
		}
		if (block.BlockIndex != 238 && block.IsFaceTransparent(SubsystemTerrain, 4, value))
		{
			return block is SoilBlock;
		}
		return true;
	}

	public static bool IsCollapseDestructibleBlock(int value)
	{
		int num = Terrain.ExtractContents(value);
		Block block = BlocksManager.Blocks[num];
		if (num == 104 || num == 391 || num == 18 || num == 92)
		{
			return false;
		}
		if (block is TrapdoorBlock)
		{
			int data = Terrain.ExtractData(value);
			if (TrapdoorBlock.GetUpsideDown(data) && TrapdoorBlock.GetOpen(data))
			{
				return false;
			}
		}
		else if (block is FluidBlock)
		{
			return false;
		}
		return true;
	}
}
