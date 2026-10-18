using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using LiteNetLib.Utils;
using TemplatesDatabase;

namespace Game;

public class SubsystemExplosions : Subsystem, IUpdateable, IDrawable
{
	public class SparseSpatialArray<T>
	{
		public const int m_bits1 = 4;

		public const int m_bits2 = 4;

		public const int m_mask1 = 15;

		public const int m_mask2 = 15;

		public const int m_diameter = 256;

		public int m_originX;

		public int m_originY;

		public int m_originZ;

		public T[][] m_data;

		public T m_outside;

		public SparseSpatialArray(int centerX, int centerY, int centerZ, T outside)
		{
			m_data = new T[4096][];
			m_originX = centerX - 128;
			m_originY = centerY - 128;
			m_originZ = centerZ - 128;
			m_outside = outside;
		}

		public T Get(int x, int y, int z)
		{
			x -= m_originX;
			y -= m_originY;
			z -= m_originZ;
			if (x < 0 || x >= 256 || y < 0 || y >= 256 || z < 0 || z >= 256)
			{
				return m_outside;
			}
			int num = x >> 4;
			int num2 = y >> 4;
			int num3 = z >> 4;
			int num4 = num + (num2 << 4) + (num3 << 4 << 4);
			T[] array = m_data[num4];
			if (array != null)
			{
				int num5 = x & 0xF;
				int num6 = y & 0xF;
				int num7 = z & 0xF;
				int num8 = num5 + (num6 << 4) + (num7 << 4 << 4);
				return array[num8];
			}
			return default;
		}

		public void Set(int x, int y, int z, T value)
		{
			x -= m_originX;
			y -= m_originY;
			z -= m_originZ;
			if (x >= 0 && x < 256 && y >= 0 && y < 256 && z >= 0 && z < 256)
			{
				int num = x >> 4;
				int num2 = y >> 4;
				int num3 = z >> 4;
				int num4 = num + (num2 << 4) + (num3 << 4 << 4);
				T[] array = m_data[num4];
				if (array == null)
				{
					array = new T[4096];
					m_data[num4] = array;
				}
				int num5 = x & 0xF;
				int num6 = y & 0xF;
				int num7 = z & 0xF;
				int num8 = num5 + (num6 << 4) + (num7 << 4 << 4);
				array[num8] = value;
			}
		}

		public void Clear()
		{
			for (int data = 0; data < m_data.Length; data++)
			{
				m_data[data] = null;
			}
		}

		public IEnumerable<KeyValuePair<Point3, T>> Enumerate()
		{
			for (int i = 0; i < m_data.Length; i++)
			{
				T[] array = m_data[i];
				if (array == null)
				{
					continue;
				}
				int num = m_originX + ((i & 0xF) << 4);
				int num2 = m_originY + (((i >> 4) & 0xF) << 4);
				int num3 = m_originZ + (((i >> 8) & 0xF) << 4);
				for (int j = 0; j < array.Length; j++)
				{
					if (!object.Equals(array[j], default(T)))
					{
						int num4 = j & 0xF;
						int num5 = (j >> 4) & 0xF;
						int num6 = (j >> 8) & 0xF;
						yield return new KeyValuePair<Point3, T>(new Point3(num + num4, num2 + num5, num3 + num6), array[j]);
					}
				}
			}
		}
	}

	public struct ExplosionData
	{
		public int X;

		public int Y;

		public int Z;

		public float Pressure;

		public bool IsIncendiary;

		public bool NoExplosionSound;

		public PlayerData Miner;

		public bool DisableItemSpawn;
	}

	public struct ProcessPoint
	{
		public int X;

		public int Y;

		public int Z;

		public int Axis;
	}

	public struct SurroundingPressurePoint
	{
		public float Pressure;

		public bool IsIncendiary;
	}

	public class DebugExplosionPoint
	{
		public Vector3 Position;

		public float Pressure;

		public float TimeAlive;

		public float Duration = 20f;
	}

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemNoise m_subsystemNoise;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemProjectiles m_subsystemProjectiles;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	public List<ExplosionData> m_queuedExplosions;

	public SparseSpatialArray<float> m_pressureByPoint;

	public SparseSpatialArray<SurroundingPressurePoint> m_surroundingPressureByPoint;

	public int m_projectilesCount;

	public Dictionary<Projectile, bool> m_generatedProjectiles;

	public static Random m_random = new Random();

	public ExplosionParticleSystem m_explosionParticleSystem;

	public bool ShowExplosionPressure;

	private Dictionary<Point2, List<(Point3, float)>> m_explosionCells;

	public bool m_disableItemSpawnCurrent;

	public SubsystemGameInfo m_subsystemGameInfo;

	public PrimitivesRenderer3D m_primitivesRenderer;

	private List<DebugExplosionPoint> m_debugExplosionPoints = new List<DebugExplosionPoint>();

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => new int[1] { 2000 };

	public void SendExplodeCells()
	{
		foreach (KeyValuePair<Point2, List<(Point3, float)>> explosionCell in m_explosionCells)
		{
			Package package = CommonLib.Net.MakePackage(PackageFlag.ExplodeCells);
			NetDataWriter writer = package.Writer;
			writer.PutPoint2(explosionCell.Key);
			foreach (var (point, value) in explosionCell.Value)
			{
				writer.Put((byte)((point.X & 0xF) | ((point.Z & 0xF) << 4)));
				writer.Put((byte)point.Y);
				writer.Put(value);
			}
			CommonLib.Net.Send(package);
		}
		m_explosionCells.Clear();
	}

	public void RecieveExplodeCells(Package p)
	{
		NetDataReader reader = p.Reader;
		Point2 point = reader.GetPoint2();
		while (!reader.EndOfData)
		{
			int num = reader.GetByte();
			int y = reader.GetByte();
			float strength = reader.GetFloat();
			m_explosionParticleSystem.SetExplosionCell(new Point3((point.X << 4) | (num & 0xF), y, (point.Y << 4) | ((num >> 4) & 0xF)), strength);
		}
	}

	public bool TryExplodeBlock(int x, int y, int z, int value, PlayerData miner = null)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			return TryNetExplodeBlock(x, y, z, value, miner);
		}
		return false;
	}

	public bool TryNetExplodeBlock(int x, int y, int z, int value, PlayerData miner = null)
	{
		int num = Terrain.ExtractContents(value);
		Block obj = BlocksManager.Blocks[num];
		float explosionPressure = obj.GetExplosionPressure(value);
		bool explosionIncendiary = obj.GetExplosionIncendiary(value);
		if (explosionPressure > 0f)
		{
			AddExplosion(x, y, z, explosionPressure, explosionIncendiary, noExplosionSound: false, miner);
			return true;
		}
		return false;
	}

	public void AddExplosion(int x, int y, int z, float pressure, bool isIncendiary, bool noExplosionSound, PlayerData miner = null)
	{
		if (CommonLib.WorkType != WorkType.Client && pressure > 0f)
		{
			m_queuedExplosions.Add(new ExplosionData
			{
				X = x,
				Y = y,
				Z = z,
				Pressure = pressure,
				IsIncendiary = isIncendiary,
				NoExplosionSound = noExplosionSound,
				Miner = miner,
				DisableItemSpawn = false
			});
		}
	}

	public void Update(float dt)
	{
		for (int num = m_debugExplosionPoints.Count - 1; num >= 0; num--)
		{
			m_debugExplosionPoints[num].TimeAlive += dt;
			if (m_debugExplosionPoints[num].TimeAlive > m_debugExplosionPoints[num].Duration)
			{
				m_debugExplosionPoints.RemoveAt(num);
			}
		}
		if (m_queuedExplosions.Count <= 0)
		{
			return;
		}
		int vector = m_queuedExplosions[0].X;
		int identity = m_queuedExplosions[0].Y;
		int z = m_queuedExplosions[0].Z;
		m_pressureByPoint = new SparseSpatialArray<float>(vector, identity, z, 0f);
		m_surroundingPressureByPoint = new SparseSpatialArray<SurroundingPressurePoint>(vector, identity, z, new SurroundingPressurePoint
		{
			IsIncendiary = false,
			Pressure = 0f
		});
		m_projectilesCount = 0;
		m_generatedProjectiles.Clear();
		bool flag = false;
		bool flag2 = true;
		bool flag3 = false;
		if (m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings != null)
		{
			flag3 = !m_subsystemGameInfo.WorldSettings.AreExplosionDropsEnabled;
		}
		int num2 = 0;
		while (num2 < m_queuedExplosions.Count)
		{
			ExplosionData explosionData = m_queuedExplosions[num2];
			if (MathUtils.Abs(explosionData.X - vector) <= 4 && MathUtils.Abs(explosionData.Y - identity) <= 4 && MathUtils.Abs(explosionData.Z - z) <= 4)
			{
				m_queuedExplosions.RemoveAt(num2);
				m_disableItemSpawnCurrent = explosionData.DisableItemSpawn | flag3;
				SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary, explosionData.Miner);
				flag |= !explosionData.NoExplosionSound;
				flag2 &= explosionData.DisableItemSpawn | flag3;
			}
			else
			{
				num2++;
			}
		}
		m_disableItemSpawnCurrent = false;
		PostprocessExplosions(flag, flag2);
		ShowExplosionPressure = SettingsManager.DebugMode;
		if (!ShowExplosionPressure)
		{
			m_pressureByPoint = null;
			m_surroundingPressureByPoint = null;
		}
		SendExplodeCells();
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_explosionParticleSystem = new ExplosionParticleSystem();
		m_subsystemParticles.AddParticleSystem(m_explosionParticleSystem);
	}

	public void SimulateExplosion(int x, int y, int z, float pressure, bool isIncendiary, PlayerData miner = null)
	{
		float num = MathUtils.Max(0.13f * MathUtils.Pow(pressure, 0.5f), 1f);
		if (m_subsystemGameInfo.WorldSettings.ExplosionsDestroyTerrain)
		{
			m_subsystemTerrain.ChangeCell(x, y, z, 0);
		}
		else
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num2 = Terrain.ExtractContents(cellValue);
			if (BlocksManager.Blocks[num2].GetExplosionPressure(cellValue) > 0f)
			{
				m_subsystemTerrain.ChangeCell(x, y, z, 0);
			}
		}
		SparseSpatialArray<bool> processed = new SparseSpatialArray<bool>(x, y, z, outside: true);
		List<ProcessPoint> list = new List<ProcessPoint>();
		List<ProcessPoint> list2 = new List<ProcessPoint>();
		List<ProcessPoint> list3 = new List<ProcessPoint>();
		TryAddPoint(x, y, z, -1, pressure, isIncendiary, list, processed, miner);
		int num3 = 0;
		int num4 = 0;
		while (list.Count > 0 || list2.Count > 0)
		{
			num3 += list.Count;
			num4++;
			float num5 = 5f * (float)MathUtils.Max(num4 - 7, 0);
			float num6 = pressure / (MathUtils.Pow(num3, 0.66f) + num5);
			if (num6 >= num)
			{
				foreach (ProcessPoint item in list)
				{
					float num7 = m_pressureByPoint.Get(item.X, item.Y, item.Z);
					float num8 = num6 + num7;
					m_pressureByPoint.Set(item.X, item.Y, item.Z, num8);
					if (item.Axis == 0)
					{
						TryAddPoint(item.X - 1, item.Y, item.Z, 0, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X + 1, item.Y, item.Z, 0, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y - 1, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y + 1, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z - 1, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z + 1, -1, num8, isIncendiary, list2, processed, miner);
					}
					else if (item.Axis == 1)
					{
						TryAddPoint(item.X - 1, item.Y, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X + 1, item.Y, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y - 1, item.Z, 1, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y + 1, item.Z, 1, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z - 1, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z + 1, -1, num8, isIncendiary, list2, processed, miner);
					}
					else if (item.Axis == 2)
					{
						TryAddPoint(item.X - 1, item.Y, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X + 1, item.Y, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y - 1, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y + 1, item.Z, -1, num8, isIncendiary, list2, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z - 1, 2, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z + 1, 2, num8, isIncendiary, list3, processed, miner);
					}
					else
					{
						TryAddPoint(item.X - 1, item.Y, item.Z, 0, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X + 1, item.Y, item.Z, 0, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y - 1, item.Z, 1, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y + 1, item.Z, 1, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z - 1, 2, num8, isIncendiary, list3, processed, miner);
						TryAddPoint(item.X, item.Y, item.Z + 1, 2, num8, isIncendiary, list3, processed, miner);
					}
				}
			}
			List<ProcessPoint> list4 = list;
			list4.Clear();
			list = list2;
			list2 = list3;
			list3 = list4;
		}
	}

	public void TryAddPoint(int x, int y, int z, int axis, float currentPressure, bool isIncendiary, List<ProcessPoint> toProcess, SparseSpatialArray<bool> processed, PlayerData playerData = null)
	{
		if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var territoriy))
		{
			if (playerData == null)
			{
				return;
			}
			bool flag = playerData.Friends.Contains(territoriy.OwnerGuid);
			if (((flag && !territoriy.ApplyToFirend) || !flag) && territoriy.OwnerGuid != playerData.PlayerGUID && !territoriy.AllowDig)
			{
				playerData.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(GetType().Name, 1), Color.Yellow, blinking: false, playNotificationSound: true);
				return;
			}
		}
		if (processed.Get(x, y, z))
		{
			return;
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		int num = Terrain.ExtractContents(cellValue);
		if (num != 0)
		{
			int num2 = (int)(MathUtils.Hash((uint)(x + 913 * y + 217546 * z)) % 100);
			float num3 = MathUtils.Lerp(1f, 2f, (float)num2 / 100f);
			if (num2 % 8 == 0)
			{
				num3 *= 3f;
			}
			Block block = BlocksManager.Blocks[num];
			float num4 = m_pressureByPoint.Get(x - 1, y, z) + m_pressureByPoint.Get(x + 1, y, z) + m_pressureByPoint.Get(x, y - 1, z) + m_pressureByPoint.Get(x, y + 1, z) + m_pressureByPoint.Get(x, y, z - 1) + m_pressureByPoint.Get(x, y, z + 1);
			if (m_subsystemGameInfo.WorldSettings.ExplosionsDestroyTerrain)
			{
				float num5 = MathUtils.Max(block.ExplosionResilience * num3, 1f);
				float num6 = num4 / num5;
				bool flag2 = false;
				if (num6 > 1f)
				{
					float probability = ((num6 > 5f) ? 0.95f : 0.75f);
					if (m_random.Bool(probability))
					{
						flag2 = TryNetExplodeBlock(x, y, z, cellValue);
					}
					if (!flag2)
					{
						int newValue = Terrain.MakeBlockValue(0);
						m_subsystemTerrain.DestroyCell(0, x, y, z, newValue, noDrop: true, noParticleSystem: true);
						CalculateImpulseAndDamage(new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), 60f, 2f * num4, out var impulse, out var _);
						bool flag3 = false;
						List<BlockDropValue> list = new List<BlockDropValue>();
						block.GetDropValues(m_subsystemTerrain, cellValue, newValue, 0, list, out var _);
						if (list.Count == 0)
						{
							list.Add(new BlockDropValue
							{
								Value = cellValue,
								Count = 1
							});
							flag3 = true;
						}
						if (!m_disableItemSpawnCurrent)
						{
							foreach (BlockDropValue item in list)
							{
								int num7 = Terrain.ExtractContents(item.Value);
								if (BlocksManager.Blocks[num7] is FluidBlock)
								{
									continue;
								}
								float num8;
								if (m_projectilesCount < 40)
								{
									num8 = 1f;
								}
								else if (m_projectilesCount < 60)
								{
									num8 = 0.5f;
								}
								else
								{
									num8 = ((m_projectilesCount >= 80) ? 0.125f : 0.25f);
								}
								if (!(m_random.Float(0f, 1f) < num8))
								{
									continue;
								}
								Vector3 velocity = impulse + m_random.Vector3(0.05f * impulse.Length());
								if (m_projectilesCount >= 1)
								{
									velocity *= m_random.Float(0.5f, 1f);
									velocity += m_random.Vector3(0.2f * velocity.Length());
								}
								float num9 = (flag3 ? 0f : MathUtils.Lerp(1f, 0f, (float)m_projectilesCount / 20f));
								Projectile projectile = m_subsystemProjectiles.AddProjectile(item.Value, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), velocity, m_random.Vector3(0f, 20f), null);
								projectile.ProjectileStoppedAction = ((m_random.Float(0f, 1f) >= num9) ? ProjectileStoppedAction.Disappear : ProjectileStoppedAction.TurnIntoPickable);
								if (m_random.Float(0f, 1f) < 0.5f && m_projectilesCount < 35)
								{
									float num10 = ((num4 > 60f) ? m_random.Float(3f, 7f) : m_random.Float(1f, 3f));
									if (isIncendiary)
									{
										num10 += 10f;
									}
									m_subsystemProjectiles.AddTrail(projectile, Vector3.Zero, new SmokeTrailParticleSystem(15, m_random.Float(0.75f, 1.5f), num10, isIncendiary ? new Color(255, 140, 192) : Color.White));
									projectile.IsIncendiary = isIncendiary;
								}
								m_generatedProjectiles.Add(projectile, value: true);
								m_projectilesCount++;
							}
						}
					}
				}
				if (!flag2)
				{
					m_surroundingPressureByPoint.Set(x, y, z, new SurroundingPressurePoint
					{
						Pressure = num4,
						IsIncendiary = isIncendiary
					});
					if (block.IsCollidable)
					{
						processed.Set(x, y, z, value: true);
						return;
					}
				}
			}
		}
		toProcess.Add(new ProcessPoint
		{
			X = x,
			Y = y,
			Z = z,
			Axis = axis
		});
		processed.Set(x, y, z, value: true);
	}

	public void PlayExplosionSound(Vector3 position, float level, float delay, bool playExplosionSound)
	{
		if (level > 1000000f)
		{
			if (playExplosionSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExplosionEnormous", 1f, m_random.Float(-0.1f, 0.1f), position, 40f, delay);
			}
			m_subsystemNoise.MakeNoise(position, 1f, 100f);
		}
		else if (level > 100000f)
		{
			if (playExplosionSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExplosionHuge", 1f, m_random.Float(-0.2f, 0.2f), position, 30f, delay);
			}
			m_subsystemNoise.MakeNoise(position, 1f, 70f);
		}
		else if (level > 20000f)
		{
			if (playExplosionSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExplosionLarge", 1f, m_random.Float(-0.2f, 0.2f), position, 26f, delay);
			}
			m_subsystemNoise.MakeNoise(position, 1f, 50f);
		}
		else if (level > 4000f)
		{
			if (playExplosionSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExplosionMedium", 1f, m_random.Float(-0.2f, 0.2f), position, 24f, delay);
			}
			m_subsystemNoise.MakeNoise(position, 1f, 40f);
		}
		else if (level > 100f)
		{
			if (playExplosionSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExplosionSmall", 1f, m_random.Float(-0.2f, 0.2f), position, 22f, delay);
			}
			m_subsystemNoise.MakeNoise(position, 1f, 35f);
		}
		else if (level > 0f)
		{
			if (playExplosionSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExplosionTiny", 1f, m_random.Float(-0.2f, 0.2f), position, 20f, delay);
			}
			m_subsystemNoise.MakeNoise(position, 1f, 30f);
		}
	}

	public void CalculateImpulseAndDamage(ComponentBody componentBody, float? obstaclePressure, out Vector3 impulse, out float damage)
	{
		CalculateImpulseAndDamage(0.5f * (componentBody.BoundingBox.Min + componentBody.BoundingBox.Max), componentBody.Mass, obstaclePressure, out impulse, out damage);
	}

	public void CalculateImpulseAndDamage(Vector3 position, float mass, float? obstaclePressure, out Vector3 impulse, out float damage)
	{
		Point3 zero = Terrain.ToCell(position);
		if (!obstaclePressure.HasValue)
		{
			obstaclePressure = m_pressureByPoint.Get(zero.X, zero.Y, zero.Z);
		}
		float vector = 0f;
		Vector3 flag = Vector3.Zero;
		for (int unitY = -1; unitY <= 1; unitY++)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int num2 = -1; num2 <= 1; num2++)
				{
					int zero2 = zero.X + unitY;
					int vector3 = zero.Y + i;
					int count = zero.Z + num2;
					float num3 = ((m_subsystemTerrain.Terrain.GetCellContents(zero2, vector3, count) != 0) ? obstaclePressure.Value : m_pressureByPoint.Get(zero2, vector3, count));
					if (unitY != 0 || i != 0 || num2 != 0)
					{
						flag += num3 * Vector3.Normalize(new Vector3(zero.X - zero2, zero.Y - vector3, zero.Z - count));
					}
					vector += num3;
				}
			}
		}
		float direction = MathUtils.Max(MathUtils.Pow(mass, 0.5f), 1f);
		impulse = 9.259259f * Vector3.Normalize(flag) * MathUtils.Pow(flag.Length(), 0.5f) / direction;
		damage = 2.5925925f * MathUtils.Pow(vector, 0.5f) / direction;
	}

	public SubsystemExplosions()
	{
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_queuedExplosions = new List<ExplosionData>();
		m_generatedProjectiles = new Dictionary<Projectile, bool>();
		m_explosionCells = new Dictionary<Point2, List<(Point3, float)>>();
	}

	public void AddExplosion(int x, int y, int z, float pressure, bool isIncendiary, bool noExplosionSound, PlayerData miner, bool disableItemSpawn)
	{
		if (CommonLib.WorkType != WorkType.Client && pressure > 0f)
		{
			m_queuedExplosions.Add(new ExplosionData
			{
				X = x,
				Y = y,
				Z = z,
				Pressure = pressure,
				IsIncendiary = isIncendiary,
				NoExplosionSound = noExplosionSound,
				Miner = miner,
				DisableItemSpawn = disableItemSpawn
			});
		}
	}

	public void PostprocessExplosions(bool playExplosionSound, bool disableItemSpawn)
	{
		Point3 point = Point3.Zero;
		float num = float.MaxValue;
		float num2 = 0f;
		foreach (KeyValuePair<Point3, float> item in m_pressureByPoint.Enumerate())
		{
			num2 += item.Value;
			float num3 = m_subsystemAudio.CalculateListenerDistance(new Vector3(item.Key));
			if (num3 < num)
			{
				num = num3;
				point = item.Key;
			}
			float num4 = 0.001f * MathUtils.Pow(num2, 0.5f);
			float num5 = MathUtils.Saturate(item.Value / 15f - num4) * m_random.Float(0.2f, 1f);
			if (!(num5 > 0.1f))
			{
				continue;
			}
			if (CommonLib.WorkType == WorkType.Server)
			{
				Point2 key = new Point2(item.Key.X >> 4, item.Key.Z >> 4);
				if (!m_explosionCells.ContainsKey(key))
				{
					m_explosionCells.Add(key, new List<(Point3, float)>());
				}
				m_explosionCells[key].Add((item.Key, num5));
			}
			if (m_random.Float(0f, 1f) < 0.33f)
			{
				m_explosionParticleSystem.SetExplosionCell(item.Key, num5);
			}
		}
		foreach (KeyValuePair<Point3, SurroundingPressurePoint> item2 in m_surroundingPressureByPoint.Enumerate())
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(item2.Key.X, item2.Key.Y, item2.Key.Z);
			int num6 = Terrain.ExtractContents(cellValue);
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num6);
			if (blockBehaviors.Length != 0)
			{
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					blockBehaviors[i].OnExplosion(cellValue, item2.Key.X, item2.Key.Y, item2.Key.Z, item2.Value.Pressure);
				}
			}
			float probability = (item2.Value.IsIncendiary ? 0.5f : 0.2f);
			Block block = BlocksManager.Blocks[num6];
			if (block.FireDuration > 0f && item2.Value.Pressure / block.ExplosionResilience > 0.2f && m_random.Bool(probability))
			{
				float fireExpandability = (item2.Value.IsIncendiary ? 1f : 0.3f);
				m_subsystemFireBlockBehavior.SetCellOnFire(item2.Key.X, item2.Key.Y, item2.Key.Z, fireExpandability);
			}
		}
		foreach (ComponentBody body in m_subsystemBodies.Bodies)
		{
			CalculateImpulseAndDamage(body, null, out var impulse, out var damage);
			impulse *= m_random.Float(0.5f, 1.5f);
			damage *= m_random.Float(0.5f, 1.5f);
			float num7 = damage;
			if (body.Size > 1f)
			{
				num7 /= body.Size;
			}
			body.ApplyImpulse(impulse);
			body.Entity.FindComponent<ComponentHealth>()?.Injure(num7, null, ignoreInvulnerability: false, LanguageControl.Get(GetType().Name, 0));
			body.Entity.FindComponent<ComponentDamage>()?.Damage(num7);
			ComponentOnFire componentOnFire = body.Entity.FindComponent<ComponentOnFire>();
			if (componentOnFire != null && m_random.Float(0f, 1f) < MathUtils.Min(num7 - 0.1f, 0.5f))
			{
				float duration = m_random.Float(6f, 8f);
				componentOnFire.SetOnFire(null, duration);
			}
		}
		foreach (Pickable pickable in m_subsystemPickables.Pickables)
		{
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
			CalculateImpulseAndDamage(pickable.Position + new Vector3(0f, 0.5f, 0f), 20f, null, out var impulse2, out var damage2);
			if (damage2 / block2.ExplosionResilience > 0.1f)
			{
				TryNetExplodeBlock(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y), Terrain.ToCell(pickable.Position.Z), pickable.Value);
				pickable.ToRemove = true;
				continue;
			}
			Vector3 vector = (impulse2 + new Vector3(0f, 0.1f * impulse2.Length(), 0f)) * m_random.Float(0.75f, 1f);
			if (vector.Length() > 10f)
			{
				if (!disableItemSpawn)
				{
					Projectile projectile = m_subsystemProjectiles.AddProjectile(pickable.Value, pickable.Position, pickable.Velocity + vector, m_random.Vector3(0f, 20f), null);
					if (m_random.Float(0f, 1f) < 0.33f)
					{
						m_subsystemProjectiles.AddTrail(projectile, Vector3.Zero, new SmokeTrailParticleSystem(15, m_random.Float(0.75f, 1.5f), m_random.Float(1f, 6f), Color.White));
					}
					pickable.ToRemove = true;
				}
				else
				{
					pickable.Velocity += vector;
				}
			}
			else
			{
				pickable.Velocity += vector;
			}
		}
		foreach (Projectile projectile2 in m_subsystemProjectiles.Projectiles)
		{
			if (!m_generatedProjectiles.ContainsKey(projectile2))
			{
				CalculateImpulseAndDamage(projectile2.Position + new Vector3(0f, 0.5f, 0f), 20f, null, out var impulse3, out var _);
				projectile2.Velocity += (impulse3 + new Vector3(0f, 0.1f * impulse3.Length(), 0f)) * m_random.Float(0.75f, 1f);
			}
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		Vector3 position = new Vector3(point.X, point.Y, point.Z);
		float delay = m_subsystemAudio.CalculateDelay(num);
		PlayExplosionSound(position, num2, delay, playExplosionSound);
		if (playExplosionSound)
		{
			CommonLib.Net.QueuePackage(new ExplosionsPackage(position, num2, delay));
		}
		if (!SettingsManager.DebugMode || m_pressureByPoint == null)
		{
			return;
		}
		foreach (KeyValuePair<Point3, float> item3 in m_pressureByPoint.Enumerate())
		{
			if (item3.Value > 1f)
			{
				m_debugExplosionPoints.Add(new DebugExplosionPoint
				{
					Position = new Vector3((float)item3.Key.X + 0.5f, (float)item3.Key.Y + 0.5f, (float)item3.Key.Z + 0.5f),
					Pressure = item3.Value,
					TimeAlive = 0f
				});
			}
		}
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (!SettingsManager.DebugMode || m_debugExplosionPoints.Count == 0)
		{
			return;
		}
		FlatBatch3D flatBatch3D = m_primitivesRenderer.FlatBatch(0, DepthStencilState.DepthRead, RasterizerState.CullNone, BlendState.AlphaBlend);
		foreach (DebugExplosionPoint debugExplosionPoint in m_debugExplosionPoints)
		{
			float num = debugExplosionPoint.TimeAlive / debugExplosionPoint.Duration;
			float num2 = 1f - num;
			if (num2 > 0.01f)
			{
				float num3 = MathUtils.Saturate(debugExplosionPoint.Pressure / 50f);
				Color color = Color.Lerp(Color.Green, Color.Red, num3) * num2;
				float num4 = (0.1f + 0.4f * num3) * num2;
				flatBatch3D.QueueLine(debugExplosionPoint.Position - Vector3.UnitX * num4, debugExplosionPoint.Position + Vector3.UnitX * num4, color);
				flatBatch3D.QueueLine(debugExplosionPoint.Position - Vector3.UnitY * num4, debugExplosionPoint.Position + Vector3.UnitY * num4, color);
				flatBatch3D.QueueLine(debugExplosionPoint.Position - Vector3.UnitZ * num4, debugExplosionPoint.Position + Vector3.UnitZ * num4, color);
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}
}
