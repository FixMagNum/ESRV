using System;
using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemIceBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	private SubsystemBodies m_subsystemBodies;

	private SubsystemTime m_subsystemTime;

	private SubsystemTerrain m_subsystemTerrain;

	private Random m_random = new Random();

	private static readonly List<Point3> NeighborOffsets;

	public const int IceBlockIndex = 62;

	public const int LayeredIceBlockIndex = 352;

	public const int WaterBlockIndex = 18;

	private SubsystemAudio m_subsystemAudio;

	private static readonly Point3[] DisplacementOffsets;

	public override int[] HandledBlocks => new int[2] { 62, 352 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		CheckForIceCollapse(x, y, z);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		CheckForIceCollapse(x, y, z);
	}

	public void Update(float dt)
	{
		m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0);
	}

	private void CheckForIceCollapse(int x, int y, int z)
	{
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2((float)x + 0.5f, (float)z + 0.5f), 1.5f, dynamicArray);
		foreach (ComponentBody item in dynamicArray)
		{
			ComponentPlayer componentPlayer = item.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer == null || !(item.Position.Y >= (float)y) || !(item.Position.Y < (float)y + 1.8f))
			{
				continue;
			}
			ComponentClothing componentClothing = componentPlayer.Entity.FindComponent<ComponentClothing>();
			if (componentClothing == null)
			{
				continue;
			}
			switch (num)
			{
			case 62:
			{
				float num4 = 0f;
				foreach (object value2 in Enum.GetValues(typeof(ClothingSlot)))
				{
					foreach (int item2 in componentClothing.GetClothes((ClothingSlot)value2))
					{
						ClothingData clothingData2 = BlocksManager.Blocks[Terrain.ExtractContents(item2)].GetClothingData(Terrain.ExtractData(item2));
						if (clothingData2 != null)
						{
							num4 += clothingData2.Weight;
						}
					}
				}
				if (item.Velocity.Length() > 0.1f && m_random.Float(0f, 1f) < MathUtils.Clamp(num4 / 8f, 0f, 1f))
				{
					m_subsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
					CheckNeighboringIceBlocks(x, y, z);
					return;
				}
				break;
			}
			case 352:
			{
				float num2 = 0f;
				foreach (object value3 in Enum.GetValues(typeof(ClothingSlot)))
				{
					foreach (int item3 in componentClothing.GetClothes((ClothingSlot)value3))
					{
						ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item3)].GetClothingData(Terrain.ExtractData(item3));
						if (clothingData != null)
						{
							num2 += clothingData.Weight;
						}
					}
				}
				if (!(item.Velocity.Length() > 0.1f))
				{
					break;
				}
				float num3 = MathUtils.Clamp(num2 / 6f, 0f, 1f);
				if (m_random.Float(0f, 1f) < num3)
				{
					float sizeFromValue = LayeredIceBlock.GetSizeFromValue(cellValue);
					int x2 = (int)((1f - sizeFromValue) * 7f);
					x2 = MathUtils.Clamp(x2, 0, 7);
					int data = FluidBlock.SetLevel(0, x2);
					int value = Terrain.MakeBlockValue(18, 0, data);
					m_subsystemTerrain.ChangeCell(x, y, z, value);
					if (m_subsystemAudio != null)
					{
						m_subsystemAudio.PlayRandomSound("Audio/BlockBroken", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
					}
					return;
				}
				break;
			}
			}
		}
	}

	private void CheckNeighboringIceBlocks(int x, int y, int z)
	{
		foreach (Point3 neighborOffset in NeighborOffsets)
		{
			int x2 = x + neighborOffset.X;
			int y2 = y + neighborOffset.Y;
			int z2 = z + neighborOffset.Z;
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x2, y2, z2)) == 62 && m_random.Float(0f, 1f) < 0.05f)
			{
				m_subsystemTerrain.DestroyCell(0, x2, y2, z2, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}

	static SubsystemIceBlockBehavior()
	{
		NeighborOffsets = new List<Point3>
		{
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
		DisplacementOffsets = new Point3[5]
		{
			new Point3(0, 1, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 1),
			new Point3(0, 0, -1)
		};
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		if (Terrain.ExtractContents(value) != 352)
		{
			return;
		}
		int num = Terrain.ExtractContents(newValue);
		switch (num)
		{
		case 18:
			break;
		case 0:
		{
			float sizeFromValue = LayeredIceBlock.GetSizeFromValue(value);
			float level = MathUtils.Clamp(1f - sizeFromValue, 0f, 1f);
			int data = FocalorsFluidBlock.SetLevel(0, level);
			data = FocalorsFluidBlock.SetIsTop(data, isTop: true);
			int value2 = Terrain.MakeBlockValue(18, 0, data);
			m_subsystemTerrain.ChangeCell(x, y, z, value2);
			break;
		}
		default:
			if (BlocksManager.Blocks[num].IsFluidBlocker_(newValue))
			{
				DisplaceOrRevert(x, y, z, value);
			}
			break;
		}
	}

	private void DisplaceOrRevert(int x, int y, int z, int oldIceValue)
	{
		float sizeFromValue = LayeredIceBlock.GetSizeFromValue(oldIceValue);
		bool flag = false;
		Point3[] displacementOffsets = DisplacementOffsets;
		for (int i = 0; i < displacementOffsets.Length; i++)
		{
			Point3 point = displacementOffsets[i];
			int x2 = x + point.X;
			int num = y + point.Y;
			int z2 = z + point.Z;
			if (num < 0 || num >= 256)
			{
				continue;
			}
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x2, num, z2);
			int num2 = Terrain.ExtractContents(cellValue);
			int data = Terrain.ExtractData(cellValue);
			switch (num2)
			{
			case 0:
			{
				float level3 = MathUtils.Clamp(1f - sizeFromValue, 0f, 1f);
				int data3 = FocalorsFluidBlock.SetLevel(0, level3);
				data3 = FocalorsFluidBlock.SetIsTop(data3, isTop: true);
				int value2 = Terrain.MakeBlockValue(18, 0, data3);
				m_subsystemTerrain.ChangeCell(x2, num, z2, value2);
				PlaySplash(x2, num, z2);
				flag = true;
				break;
			}
			case 18:
			{
				float level = FocalorsFluidBlock.GetLevel(data);
				float num3 = 1f - level;
				if (!(num3 + sizeFromValue <= 1.001f))
				{
					continue;
				}
				float num4 = MathUtils.Clamp(num3 + sizeFromValue, 0.01f, 1f);
				float level2 = MathUtils.Clamp(1f - num4, 0f, 1f);
				int data2 = FocalorsFluidBlock.SetLevel(data, level2);
				int value = Terrain.MakeBlockValue(18, 0, data2);
				m_subsystemTerrain.ChangeCell(x2, num, z2, value);
				PlaySplash(x2, num, z2);
				flag = true;
				break;
			}
			default:
				continue;
			}
			break;
		}
		if (!flag)
		{
			int num5 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, y, z));
			if (num5 != 0 && num5 != 18)
			{
				m_subsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: true);
			}
			float level4 = MathUtils.Clamp(1f - sizeFromValue, 0f, 1f);
			int data4 = FocalorsFluidBlock.SetLevel(0, level4);
			data4 = FocalorsFluidBlock.SetIsTop(data4, isTop: true);
			int value3 = Terrain.MakeBlockValue(18, 0, data4);
			m_subsystemTerrain.ChangeCell(x, y, z, value3);
		}
	}

	private void PlaySplash(int x, int y, int z)
	{
		if (m_subsystemAudio != null)
		{
			m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, 0f, new Vector3(x, y, z), 4f, autoDelay: true);
		}
	}
}
