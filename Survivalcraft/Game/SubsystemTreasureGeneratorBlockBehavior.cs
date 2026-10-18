using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemTreasureGeneratorBlockBehavior : SubsystemBlockBehavior
{
	public struct TreasureData
	{
		public int Value;

		public float Probability;

		public int MaxCount;
	}

	public SubsystemPickables m_subsystemPickables;

	public Random m_random = new Random();

	public static TreasureData[] m_treasureData;

	public override int[] HandledBlocks => new int[1] { 190 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(neighborX, neighborY, neighborZ);
		if (cellContents != 0 && cellContents != 18)
		{
			return;
		}
		SubsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(0));
		if (!m_random.Bool(0.25f))
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		float max = m_treasureData.Sum((TreasureData t) => t.Probability);
		float num3 = m_random.Float(0f, max);
		TreasureData[] treasureData = m_treasureData;
		for (int num4 = 0; num4 < treasureData.Length; num4++)
		{
			TreasureData treasureData2 = treasureData[num4];
			num3 -= treasureData2.Probability;
			if (num3 <= 0f)
			{
				num = treasureData2.Value;
				num2 = m_random.Int(1, treasureData2.MaxCount);
				break;
			}
		}
		if (num == 0 || num2 <= 0)
		{
			return;
		}
		for (int num5 = 0; num5 < num2; num5++)
		{
			m_subsystemPickables.AddPickable(num, 1, new Vector3(x, y, z) + m_random.Vector3(0.1f, 0.4f) + new Vector3(0.5f), Vector3.Zero, null);
		}
		int num6 = m_random.Int(3, 6);
		for (int num7 = 0; num7 < num6; num7++)
		{
			m_subsystemPickables.AddPickable(248, 1, new Vector3(x, y, z) + m_random.Vector3(0.1f, 0.4f) + new Vector3(0.5f), Vector3.Zero, null);
		}
		for (int num8 = 0; num8 <= 102; num8++)
		{
			if (m_random.Bool(0.01f))
			{
				int value = Terrain.MakeBlockValue(203, 0, ClothingBlock.SetClothingIndex(ClothingBlock.SetClothingColor(0, m_random.Int(0, 15)), num8));
				m_subsystemPickables.AddPickable(value, 1, new Vector3(x, y, z) + m_random.Vector3(0f, 0.1f) + new Vector3(0.5f), Vector3.Zero, null);
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
	}

	static SubsystemTreasureGeneratorBlockBehavior()
	{
		m_treasureData = new TreasureData[61]
		{
			new TreasureData
			{
				Value = 79,
				Probability = 4f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 111,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 43,
				Probability = 4f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 40,
				Probability = 2f,
				MaxCount = 3
			},
			new TreasureData
			{
				Value = 42,
				Probability = 4f,
				MaxCount = 3
			},
			new TreasureData
			{
				Value = 22,
				Probability = 4f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 103,
				Probability = 2f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 150,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 21,
				Probability = 2f,
				MaxCount = 16
			},
			new TreasureData
			{
				Value = 159,
				Probability = 2f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 207,
				Probability = 2f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 17,
				Probability = 2f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 31,
				Probability = 4f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 108,
				Probability = 4f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = 109,
				Probability = 2f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 105,
				Probability = 1f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 106,
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 107,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 234,
				Probability = 1f,
				MaxCount = 4
			},
			new TreasureData
			{
				Value = 235,
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 236,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 132,
				Probability = 2f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(173, 0, 6),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(173, 0, 7),
				Probability = 8f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(173, 0, 5),
				Probability = 8f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(173, 0, 6),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(119, 0, 0),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(119, 0, 1),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(119, 0, 2),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(119, 0, 3),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(119, 0, 4),
				Probability = 2f,
				MaxCount = 8
			},
			new TreasureData
			{
				Value = 191,
				Probability = 4f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.CopperArrow)),
				Probability = 2f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.IronArrow)),
				Probability = 2f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.DiamondArrow)),
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.FireArrow)),
				Probability = 2f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 200,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.IronBolt)),
				Probability = 2f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.DiamondBolt)),
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = Terrain.MakeBlockValue(192, 0, ArrowBlock.SetArrowType(0, ArrowBlock.ArrowType.ExplosiveBolt)),
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 212,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 124,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 125,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 82,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 116,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 36,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 113,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 38,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 115,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 37,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 114,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 171,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 172,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 90,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 160,
				Probability = 1f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 158,
				Probability = 2f,
				MaxCount = 1
			},
			new TreasureData
			{
				Value = 133,
				Probability = 1f,
				MaxCount = 10
			},
			new TreasureData
			{
				Value = 179,
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 142,
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 141,
				Probability = 1f,
				MaxCount = 2
			},
			new TreasureData
			{
				Value = 237,
				Probability = 1f,
				MaxCount = 2
			}
		};
	}
}
