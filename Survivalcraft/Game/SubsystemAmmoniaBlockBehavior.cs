using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class SubsystemAmmoniaBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	private SubsystemBodies m_subsystemBodies;

	private SubsystemTime m_subsystemTime;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemParticles m_subsystemParticles;

	private Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell;

	public override int[] HandledBlocks => new int[1] { 312 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_particleSystemsByCell = new Dictionary<Point3, FireParticleSystem>();
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		CheckForFireAndDamage(x, y, z);
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		CheckForFireAndDamage(x, y, z);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveFire(x, y, z);
	}

	private void RemoveFire(int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		if (m_particleSystemsByCell.TryGetValue(key, out var value))
		{
			m_subsystemParticles.RemoveParticleSystem(value);
			m_particleSystemsByCell.Remove(key);
		}
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		CheckForFireAndDamage(x, y, z);
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> list = new List<Point3>();
		foreach (Point3 key in m_particleSystemsByCell.Keys)
		{
			if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
			{
				list.Add(key);
			}
		}
		foreach (Point3 item in list)
		{
			RemoveFire(item.X, item.Y, item.Z);
		}
	}

	private void AddFire(int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		if (!m_particleSystemsByCell.ContainsKey(key))
		{
			FireParticleSystem fireParticleSystem = new FireParticleSystem(new Vector3(x, y, z) + new Vector3(0.5f, -0.5f, 0.5f), 1f, 64f)
			{
				Texture = ContentManager.Get<Texture2D>("Textures/PurpleFireParticle")
			};
			m_subsystemParticles.AddParticleSystem(fireParticleSystem);
			m_particleSystemsByCell[key] = fireParticleSystem;
		}
	}

	public void Update(float dt)
	{
		if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0))
		{
			List<Point3> list = new List<Point3>(m_particleSystemsByCell.Keys);
			for (int i = 0; i < list.Count; i++)
			{
				Point3 point = list[i];
				CheckForFireAndDamage(point.X, point.Y, point.Z);
			}
		}
	}

	private void CheckForFireAndDamage(int x, int y, int z)
	{
		bool flag = false;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (m_subsystemTerrain.Terrain.GetCellContents(x + i, y + j, z + k) == 104)
					{
						flag = true;
						AddFire(x, y, z);
					}
				}
			}
		}
		if (!flag)
		{
			RemoveFire(x, y, z);
		}
		if (!flag)
		{
			return;
		}
		DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
		m_subsystemBodies.FindBodiesAroundPoint(new Vector2(x, z), 2f, dynamicArray);
		for (int l = 0; l < dynamicArray.Count; l++)
		{
			ComponentHealth componentHealth = dynamicArray.Array[l].Entity.FindComponent<ComponentHealth>();
			if (componentHealth != null && !componentHealth.IsInvulnerable)
			{
				string cause = LanguageControl.Get("ComponentVitalStats", "ammonia_suffocation");
				componentHealth.Injure(0.1f, null, ignoreInvulnerability: false, cause);
			}
		}
	}
}
