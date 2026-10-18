using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemRakeBlockBehavior : SubsystemBlockBehavior
{
	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemExplosions m_subsystemExplosions;

	private SubsystemTime m_subsystemTime;

	private float m_timer;

	private bool m_isCountingDown;

	private Random m_random = new Random();

	private SubsystemPickables m_subsystemPickables;

	private static readonly int[] m_bronzeArmorIDs;

	private static readonly int[] m_bronzeToolsIDs;

	public override int[] HandledBlocks => new int[5] { 169, 219, 171, 172, 409 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		TerrainRaycastResult? boneAbsoluteTransform = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
		if (boneAbsoluteTransform.HasValue)
		{
			int x = boneAbsoluteTransform.Value.CellFace.X;
			int y = boneAbsoluteTransform.Value.CellFace.Y;
			int z = boneAbsoluteTransform.Value.CellFace.Z;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			Block block = BlocksManager.Blocks[num];
			switch (num)
			{
			case 2:
			case 8:
			case 71:
			case 355:
			case 356:
			case 440:
				switch (num)
				{
				case 2:
				{
					int value2 = Terrain.MakeBlockValue(168, 0, 0);
					m_subsystemTerrain.ChangeCell(x, y, z, value2);
					m_subsystemAudio.PlayRandomSound("Audio/Impacts/Dirt", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
					Vector3 position2 = new Vector3((float)x + 0.5f, (float)y + 1.25f, (float)z + 0.5f);
					m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position2, cellValue, 0.5f));
					break;
				}
				case 8:
				{
					int value3 = Terrain.ReplaceContents(cellValue, 2);
					m_subsystemTerrain.ChangeCell(x, y, z, value3);
					m_subsystemAudio.PlayRandomSound("Audio/Impacts/Plant", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
					Vector3 position3 = new Vector3((float)x + 0.5f, (float)y + 1.2f, (float)z + 0.5f);
					m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position3, cellValue, 0.75f));
					break;
				}
				case 355:
				case 356:
				case 440:
				{
					float num2 = ((num == 440) ? ArchaeologicalDirtSlabBlock.GetSizeFromValue(cellValue) : DirtSlabBlock.GetSizeFromValue(cellValue));
					num2 -= ((num == 440) ? 0.07f : 0.1f);
					if (num2 < 0.1f)
					{
						m_subsystemTerrain.ChangeCell(x, y, z, 0);
						m_subsystemAudio.PlayRandomSound("Audio/Impacts/Dirt", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
						Vector3 position4 = new Vector3((float)x + 0.5f, y, (float)z + 0.5f);
						m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position4, cellValue, 0.5f));
					}
					else
					{
						cellValue = ((num == 440) ? ArchaeologicalDirtSlabBlock.SetSizeInValue(cellValue, num2) : DirtSlabBlock.SetSizeInValue(cellValue, num2));
						m_subsystemTerrain.ChangeCell(x, y, z, cellValue);
						m_subsystemAudio.PlayRandomSound("Audio/Impacts/Dirt", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
						float y2 = (float)y + num2;
						Vector3 position5 = new Vector3((float)x + 0.5f, y2, (float)z + 0.5f);
						m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position5, cellValue, 0.5f));
					}
					if (num == 440)
					{
						TryDropArchaeologyLoot(new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f));
					}
					else
					{
						TryDropLoot(new Vector3((float)x + 0.5f, (float)y + 1.25f, (float)z + 0.5f));
					}
					break;
				}
				case 71:
					if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, y - 1, z)) == 199 && !m_isCountingDown)
					{
						m_isCountingDown = true;
						m_timer = 0f;
						m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + 1.0, () =>
						{
							CountdownTick(componentMiner.ComponentPlayer, x, y, z);
						});
					}
					break;
				}
				componentMiner.DamageActiveTool(1);
				return true;
			case 168:
			{
				int value = DirtSlabBlock.SetSizeInValue(Terrain.MakeBlockValue(355), 0.9f);
				m_subsystemTerrain.ChangeCell(x, y, z, value);
				m_subsystemAudio.PlayRandomSound("Audio/Impacts/Dirt", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
				Vector3 position = new Vector3((float)x + 0.5f, (float)y + 1.25f, (float)z + 0.5f);
				m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position, cellValue, 0.5f));
				componentMiner.DamageActiveTool(1);
				return true;
			}
			}
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
	}

	private void CountdownTick(ComponentPlayer player, int x, int y, int z)
	{
		if (m_timer < 5f)
		{
			m_timer++;
			m_subsystemAudio.PlayRandomSound("Audio/RakeBuff", 1f, m_random.Float(-0.2f, 0.2f), player.ComponentBody.Position, 4f, 0f);
			m_subsystemTime.QueueGameTimeDelayedExecution(m_subsystemTime.GameTime + 1.0, () =>
			{
				CountdownTick(player, x, y, z);
			});
			return;
		}
		BoundingBox boundingBox = new BoundingBox(new Vector3(x - 5, y - 5, z - 5), new Vector3(x + 5, y + 5, z + 5));
		foreach (Pickable pickable in m_subsystemPickables.Pickables)
		{
			if (boundingBox.Contains(pickable.Position))
			{
				pickable.ToRemove = true;
			}
		}
		m_subsystemExplosions.AddExplosion(x, y, z, 500000f, isIncendiary: false, noExplosionSound: true, null, disableItemSpawn: true);
		m_isCountingDown = false;
	}

	private void TryDropLoot(Vector3 position)
	{
		float num = m_random.Float(0f, 1f);
		if (num <= 0.3716f)
		{
			num = m_random.Float(0f, 1f);
			int num2 = 0;
			if (num <= 0.2691f)
			{
				num2 = 318;
			}
			else if (num <= 0.5383f)
			{
				num2 = 23;
			}
			else if (num <= 0.8074f)
			{
				num2 = 79;
			}
			else if (num <= 0.8742f)
			{
				num2 = 22;
			}
			else if (num <= 0.9409f)
			{
				num2 = 43;
			}
			else if (num <= 0.9625f)
			{
				num2 = 249;
			}
			else if (num <= 0.9841f)
			{
				num2 = 250;
			}
			else if (num <= 1f)
			{
				num2 = 72;
			}
			switch (num2)
			{
			case 318:
			{
				int x = Terrain.ToCell(position.X);
				int z = Terrain.ToCell(position.Z);
				WormBlock.WormKind wormKind = (IsJungleAt(x, z) ? WormBlock.WormKind.StripedCaterpillar : WormBlock.WormKind.Normal);
				WormBlock.WormKind k = ((m_random.Float(0f, 1f) < 0.01f) ? WormBlock.WormKind.TitanitusColossus : wormKind);
				int value = Terrain.MakeBlockValue(318, 0, WormBlock.SetKind(0, k));
				m_subsystemPickables.AddPickable(value, 1, position, null, null);
				break;
			}
			default:
				m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(num2), 1, position, null, null);
				break;
			case 0:
				break;
			}
		}
		else if (num <= 0.3717f)
		{
			m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(111), 1, position, null, null);
		}
	}

	private bool IsJungleAt(int x, int z)
	{
		int temperature = m_subsystemTerrain.Terrain.GetTemperature(x, z);
		int humidity = m_subsystemTerrain.Terrain.GetHumidity(x, z);
		if (temperature >= 12)
		{
			return humidity >= 8;
		}
		return false;
	}

	private void TryDropArchaeologyLoot(Vector3 position)
	{
		if (m_random.Float(0f, 1f) > 0.4f)
		{
			return;
		}
		float state = m_random.Float(0f, 100f);
		if (state < 1.5f)
		{
			m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(40), 1, position, null, null);
			return;
		}
		if (state < 5.5f)
		{
			m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(415), 1, position, null, null);
			return;
		}
		if (state < 20.5f)
		{
			if (m_random.Bool())
			{
				int clothingIndex = m_bronzeArmorIDs[m_random.Int(0, m_bronzeArmorIDs.Length - 1)];
				int value = Terrain.MakeBlockValue(203, 0, ClothingBlock.SetClothingIndex(0, clothingIndex));
				m_subsystemPickables.AddPickable(value, 1, position, null, null);
			}
			else
			{
				int contents = m_bronzeToolsIDs[m_random.Int(0, m_bronzeToolsIDs.Length - 1)];
				m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(contents), 1, position, null, null);
			}
			return;
		}
		if (state < 40.5f)
		{
			int count = m_random.Int(1, 5);
			m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(403), count, position, null, null);
			return;
		}
		if (state < 70.5f)
		{
			int count2 = m_random.Int(3, 12);
			m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(398), count2, position, null, null);
			return;
		}
		int data = 0;
		if (m_random.Bool())
		{
			int value2 = m_random.Int(0, 15);
			data = SlabBlock.SetColor(data, value2);
		}
		int value3 = Terrain.MakeBlockValue(70, 0, data);
		m_subsystemPickables.AddPickable(value3, 1, position, null, null);
	}

	static SubsystemRakeBlockBehavior()
	{
		m_bronzeArmorIDs = new int[15]
		{
			110, 114, 118, 120, 119, 121, 122, 123, 124, 111,
			115, 112, 116, 113, 117
		};
		m_bronzeToolsIDs = new int[6] { 405, 406, 407, 414, 409, 408 };
	}
}
