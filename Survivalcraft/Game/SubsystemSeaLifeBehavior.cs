using System.Collections.Generic;
using System.Linq;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemSeaLifeBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemPickables m_subsystemPickables;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemTime m_subsystemTime;

	public Random m_random = new Random();

	public HashSet<Pickable> m_livePickables = new HashSet<Pickable>();

	public Dictionary<Point3, double> m_growthTimers = new Dictionary<Point3, double>();

	public float HuntingSpeed = 2.5f;

	public float WanderSpeed = 1f;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[2] { 226, 229 };

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemPickables.PickableAdded += (Pickable p) =>
		{
			int num = Terrain.ExtractContents(p.Value);
			if (num == 226 || num == 229)
			{
				m_livePickables.Add(p);
			}
		};
		m_subsystemPickables.PickableRemoved += (Pickable p) =>
		{
			m_livePickables.Remove(p);
		};
	}

	public void Update(float dt)
	{
		UpdatePickablesAI(dt);
		if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0))
		{
			UpdateBlocksGrowth();
		}
	}

	private void UpdatePickablesAI(float dt)
	{
		foreach (Pickable item in m_livePickables.ToList())
		{
			if (item.ToRemove)
			{
				continue;
			}
			int num = Terrain.ExtractContents(item.Value);
			Vector3 position = item.Position;
			int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z)));
			bool flag = num2 == 18 || num2 == 92;
			if (flag && m_random.Float(0f, 1f) < 0.02f)
			{
				TrySettlePickable(item);
				continue;
			}
			switch (num)
			{
			case 229:
			{
				Pickable pickable2 = FindClosest(position, 226);
				if (pickable2 != null)
				{
					Vector3 vector2 = Vector3.Normalize(pickable2.Position - position);
					float num4 = (flag ? 1f : 2f);
					item.Velocity += vector2 * HuntingSpeed * num4 * dt;
					if (Vector3.Distance(position, pickable2.Position) < 0.6f)
					{
						EatUrchinPickable(item, pickable2);
					}
				}
				else
				{
					Wander(item, dt);
				}
				break;
			}
			case 226:
			{
				Pickable pickable = FindClosest(position, 229);
				if (pickable != null)
				{
					Vector3 vector = Vector3.Normalize(position - pickable.Position);
					float num3 = (flag ? 1f : 2f);
					item.Velocity += vector * HuntingSpeed * num3 * dt;
				}
				else
				{
					Wander(item, dt);
				}
				break;
			}
			}
			if (!flag)
			{
				item.Velocity.Y -= 5f * dt;
				if (item.Velocity.Length() > 0.1f && MathUtils.Abs(item.Velocity.Y) < 0.01f)
				{
					item.Velocity.Y += 0.15f;
				}
			}
		}
	}

	private void Wander(Pickable p, float dt)
	{
		p.Velocity += new Vector3(m_random.Float(0f - WanderSpeed, WanderSpeed), 0f, m_random.Float(0f - WanderSpeed, WanderSpeed)) * dt;
	}

	private void UpdateBlocksGrowth()
	{
	}

	private Pickable FindClosest(Vector3 pos, int contents)
	{
		return m_livePickables.FirstOrDefault((Pickable p) => !p.ToRemove && Terrain.ExtractContents(p.Value) == contents && Vector3.DistanceSquared(pos, p.Position) < 36f);
	}

	private void EatUrchinPickable(Pickable star, Pickable urchin)
	{
		urchin.ToRemove = true;
		int data = Terrain.ExtractData(star.Value);
		int variant = FocalorsAttachedBlock.GetVariant(data);
		if (variant < 2)
		{
			star.Value = Terrain.ReplaceData(star.Value, FocalorsAttachedBlock.SetVariant(data, variant + 1));
		}
		star.Position = urchin.Position;
		m_subsystemAudio.PlayRandomSound("Audio/Impacts/DirtySquishy", 1f, m_random.Float(-0.2f, 0.2f), urchin.Position, 3f, autoDelay: true);
	}

	private void TrySettlePickable(Pickable p)
	{
		Point3 point = Terrain.ToCell(p.Position);
		int num = Terrain.ExtractContents(p.Value);
		if (!(BlocksManager.Blocks[num] is IWaterloggable waterloggable))
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			Point3 point2 = point + CellFace.FaceToPoint3(i);
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point2.X, point2.Y, point2.Z);
			if (!BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable)
			{
				continue;
			}
			int data = FocalorsAttachedBlock.SetFace(0, CellFace.OppositeFace(i));
			data = waterloggable.SetWaterLogged(data, isWaterLogged: true);
			data = FocalorsAttachedBlock.SetVariant(data, 0);
			int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			if (Terrain.ExtractContents(cellValue2) == num)
			{
				int data2 = Terrain.ExtractData(cellValue2);
				int variant = FocalorsAttachedBlock.GetVariant(data2);
				if (variant < 2)
				{
					int data3 = FocalorsAttachedBlock.SetVariant(data2, variant + 1);
					m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, Terrain.ReplaceData(cellValue2, data3));
				}
				else
				{
					TrySpreadUrchin(point.X, point.Y, point.Z, data2);
				}
			}
			else
			{
				m_subsystemTerrain.ChangeCell(point.X, point.Y, point.Z, Terrain.MakeBlockValue(num, 0, data));
			}
			p.ToRemove = true;
			break;
		}
	}

	private void ProcessUrchinGrowth(int value, int x, int y, int z, int data)
	{
		Point3 point = CellFace.FaceToPoint3(FocalorsAttachedBlock.GetFace(data));
		Point3 point2 = new Point3(x, y, z) - point;
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, point2.Y, point2.Z)) == 8)
		{
			int[] array = new int[3] { 2, 6, 7 };
			int contents = array[m_random.Int(0, array.Length - 1)];
			m_subsystemTerrain.ChangeCell(point2.X, point2.Y, point2.Z, Terrain.MakeBlockValue(contents));
			m_subsystemAudio.PlayRandomSound("Audio/Impacts/DirtySquishy", 1f, m_random.Float(-0.2f, 0.2f), new Vector3(x, y, z), 3f, autoDelay: true);
			int variant = FocalorsAttachedBlock.GetVariant(data);
			if (variant < 2)
			{
				int data2 = FocalorsAttachedBlock.SetVariant(data, variant + 1);
				m_subsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(value, data2));
			}
			else
			{
				TrySpreadUrchin(x, y, z, data);
			}
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		int num = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		if (!(BlocksManager.Blocks[num] is IWaterloggable waterloggable) || !waterloggable.IsWaterLogged(data))
		{
			int num2 = FocalorsAttachedBlock.GetVariant(data) + 1;
			m_subsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
			for (int i = 0; i < num2; i++)
			{
				Vector3 vector = new Vector3(m_random.Float(-0.2f, 0.2f), 0.1f, m_random.Float(-0.2f, 0.2f));
				m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(num), 1, new Vector3(x, y, z) + new Vector3(0.5f) + vector, null, null);
			}
		}
		else
		{
			if (num == 226)
			{
				ProcessUrchinGrowth(value, x, y, z, data);
			}
			if (num == 229)
			{
				ProcessStarfishHunting(value, x, y, z, data);
			}
		}
	}

	private void TrySpreadUrchin(int x, int y, int z, int data)
	{
		if (!(BlocksManager.Blocks[226] is IWaterloggable waterloggable))
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			Point3 point = CellFace.FaceToPoint3(i);
			Point3 point2 = new Point3(x + point.X, y + point.Y, z + point.Z);
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point2.X, point2.Y, point2.Z)) != 18)
			{
				continue;
			}
			for (int j = 0; j < 6; j++)
			{
				Point3 point3 = point2 + CellFace.FaceToPoint3(j);
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point3.X, point3.Y, point3.Z);
				if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsCollidable)
				{
					int data2 = FocalorsAttachedBlock.SetFace(0, CellFace.OppositeFace(j));
					data2 = waterloggable.SetWaterLogged(data2, isWaterLogged: true);
					data2 = FocalorsAttachedBlock.SetVariant(data2, 0);
					m_subsystemTerrain.ChangeCell(point2.X, point2.Y, point2.Z, Terrain.MakeBlockValue(226, 0, data2));
					return;
				}
			}
		}
	}

	private void ProcessStarfishHunting(int value, int x, int y, int z, int data)
	{
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x + i, y + j, z + k)) == 226)
					{
						m_subsystemTerrain.DestroyCell(0, x + i, y + j, z + k, 0, noDrop: true, noParticleSystem: false);
						m_subsystemAudio.PlayRandomSound("Audio/Impacts/DirtySquishy", 1f, m_random.Float(-0.2f, 0.2f), new Vector3(x + i, y + j, z + k), 3f, autoDelay: true);
						int variant = FocalorsAttachedBlock.GetVariant(data);
						if (variant < 2)
						{
							int data2 = FocalorsAttachedBlock.SetVariant(data, variant + 1);
							m_subsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(value, data2));
						}
						return;
					}
				}
			}
		}
	}
}
