using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemHotStoneBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public SubsystemProjectiles m_subsystemProjectiles;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAudio m_subsystemAudio;

	public Dictionary<Projectile, bool> m_projectiles;

	public SubsystemBodies m_subsystemBodies;

	public Random m_random = new Random();

	public SubsystemPlayers m_subsystemPlayers;

	public override int[] HandledBlocks => new int[0];

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		foreach (Projectile projectile in m_subsystemProjectiles.Projectiles)
		{
			ScanProjectile(projectile);
		}
		m_subsystemProjectiles.ProjectileAdded += (Projectile projectile) =>
		{
			ScanProjectile(projectile);
		};
		m_subsystemProjectiles.ProjectileRemoved += (Projectile projectile) =>
		{
			m_projectiles.Remove(projectile);
		};
	}

	public void ScanProjectile(Projectile projectile)
	{
		if (!m_projectiles.ContainsKey(projectile))
		{
			int value = projectile.Value;
			int num = Terrain.ExtractContents(value);
			int data = Terrain.ExtractData(value);
			if (num == 79 && StoneChunkBlock.GetKind(data) == StoneChunkBlock.StoneKind.Heated)
			{
				m_projectiles.Add(projectile, value: true);
				projectile.ProjectileStoppedAction = ProjectileStoppedAction.DoNothing;
				m_subsystemProjectiles.AddTrail(projectile, new Vector3(0f, 0f, 0f), new SmokeTrailParticleSystem(60, 0.6f, 2.5f, new Color(255, 80, 10)));
			}
		}
	}

	public void Update(float dt)
	{
		if (!m_subsystemTime.PeriodicGameTimeEvent(0.1, 0.0))
		{
			return;
		}
		List<Projectile> list = new List<Projectile>();
		DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
		bool flag = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
		foreach (Projectile key in m_projectiles.Keys)
		{
			if (flag && MathUtils.Abs(key.Velocity.Y) > 0.5f)
			{
				Vector3 vector = Vector3.Zero;
				float num = 144f;
				bool flag2 = false;
				foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
				{
					float num2 = Vector3.DistanceSquared(key.Position, componentPlayer.ComponentBody.Position);
					if (num2 < num)
					{
						num = num2;
						vector = componentPlayer.ComponentBody.Position;
						vector.Y++;
						flag2 = true;
					}
				}
				if (flag2)
				{
					Vector3 vector2 = vector - key.Position;
					float num3 = vector2.Length();
					if (num3 > 0.1f)
					{
						vector2 /= num3;
						key.Velocity += vector2 * 0.6f;
					}
				}
			}
			int x = Terrain.ToCell(key.Position.X);
			int y = Terrain.ToCell(key.Position.Y);
			int z = Terrain.ToCell(key.Position.Z);
			m_subsystemTerrain.Terrain.GetCellContents(x, y, z);
			dynamicArray.Clear();
			m_subsystemBodies.FindBodiesAroundPoint(new Vector2(key.Position.X, key.Position.Z), 1.5f, dynamicArray);
			for (int i = 0; i < dynamicArray.Count; i++)
			{
				ComponentBody componentBody = dynamicArray.Array[i];
				if (MathUtils.Abs(componentBody.Position.Y - key.Position.Y) < 2f)
				{
					componentBody.Entity.FindComponent<ComponentOnFire>()?.SetOnFire(key.Owner, m_random.Float(6f, 10f));
				}
			}
			if (!(key.Velocity.LengthSquared() < 0.1f) && !key.ToRemove)
			{
				continue;
			}
			int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, y, z));
			if (!BlocksManager.Blocks[num4].IsCollidable || num4 == 0)
			{
				if (!SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var _))
				{
					int data = FocalorsFluidBlock.SetLevel(0, 0.92f);
					data = FocalorsFluidBlock.SetIsTop(data, isTop: true);
					int value = Terrain.MakeBlockValue(92, 0, data);
					m_subsystemTerrain.ChangeCell(x, y, z, value);
					m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, key.Position, 3f, autoDelay: true);
				}
				else
				{
					m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 0.6f, 0.8f, key.Position, 2f, autoDelay: true);
				}
			}
			key.ToRemove = true;
			list.Add(key);
		}
		foreach (Projectile item in list)
		{
			m_projectiles.Remove(item);
		}
	}

	public SubsystemHotStoneBlockBehavior()
	{
		m_projectiles = new Dictionary<Projectile, bool>();
	}

	public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem)
	{
		if (!(worldItem is Projectile { Value: var value } projectile))
		{
			return false;
		}
		if (Terrain.ExtractContents(value) == 79 && StoneChunkBlock.GetKind(Terrain.ExtractData(value)) == StoneChunkBlock.StoneKind.Heated)
		{
			if (componentBody != null)
			{
				return true;
			}
			if (cellFace.HasValue)
			{
				Vector3 vector = m_random.Vector3(1f);
				projectile.Velocity += vector;
				return false;
			}
		}
		return false;
	}
}
