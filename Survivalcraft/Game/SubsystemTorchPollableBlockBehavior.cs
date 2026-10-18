using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemTorchPollableBlockBehavior : SubsystemPollableBlockBehavior
{
	public SubsystemParticles m_subsystemParticles;

	public Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell;

	public SubsystemWeather m_subsystemWeather;

	private Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 31 };

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
		switch (Terrain.ExtractContents(cellValueFast))
		{
		case 132:
		{
			int cellContents2 = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
			if (!BlocksManager.Blocks[cellContents2].IsCollidable)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
		case 31:
		{
			Point3 point = CellFace.FaceToPoint3(Terrain.ExtractData(cellValueFast));
			int x2 = x - point.X;
			int y2 = y - point.Y;
			int z2 = z - point.Z;
			int cellContents = SubsystemTerrain.Terrain.GetCellContents(x2, y2, z2);
			if (!BlocksManager.Blocks[cellContents].IsCollidable)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
			break;
		}
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		AddTorch(value, x, y, z);
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveTorch(x, y, z);
	}

	public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
	{
		RemoveTorch(x, y, z);
		AddTorch(value, x, y, z);
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		AddTorch(value, x, y, z);
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
			RemoveTorch(item.X, item.Y, item.Z);
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
	}

	public void AddTorch(int value, int x, int y, int z)
	{
		if (SettingsManager.FireParticlesFromLights && !m_particleSystemsByCell.ContainsKey(new Point3(x, y, z)))
		{
			Vector3 vector;
			float size;
			switch (Terrain.ExtractContents(value))
			{
			default:
				vector = new Vector3(0.5f, 0.2f, 0.5f);
				size = 0.2f;
				break;
			case 132:
				vector = new Vector3(0.5f, 0.1f, 0.5f);
				size = 0.1f;
				break;
			case 31:
				vector = Terrain.ExtractData(value) switch
				{
					0 => new Vector3(0.5f, 0.58f, 0.27f), 
					1 => new Vector3(0.27f, 0.58f, 0.5f), 
					2 => new Vector3(0.5f, 0.58f, 0.73f), 
					3 => new Vector3(0.73f, 0.58f, 0.5f), 
					_ => new Vector3(0.5f, 0.53f, 0.5f), 
				};
				size = 0.15f;
				break;
			}
			FireParticleSystem fireParticleSystem = new FireParticleSystem(new Vector3(x, y, z) + vector, size, 24f);
			m_subsystemParticles.AddParticleSystem(fireParticleSystem);
			m_particleSystemsByCell[new Point3(x, y, z)] = fireParticleSystem;
		}
	}

	public void RemoveTorch(int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		FireParticleSystem particleSystem = m_particleSystemsByCell[key];
		m_subsystemParticles.RemoveParticleSystem(particleSystem);
		m_particleSystemsByCell.Remove(key);
	}

	public SubsystemTorchPollableBlockBehavior()
	{
		m_particleSystemsByCell = new Dictionary<Point3, FireParticleSystem>();
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_random.Float(0f, 1f) < 0.25f)
		{
			PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
			if (precipitationShaftInfo.Intensity > 0f && y >= precipitationShaftInfo.YLimit - 1)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
			}
		}
	}
}
