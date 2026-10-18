using System;
using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemFireBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	private class FireData
	{
		public Point3 Point;

		public float Time0;

		public float Time1;

		public float Time2;

		public float Time3;

		public float Time5;

		public float FireExpandability;
	}

	private SubsystemTime m_subsystemTime;

	private SubsystemGameWidgets m_subsystemViews;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemAmbientSounds m_subsystemAmbientSounds;

	private Dictionary<Point3, float> m_expansionProbabilities;

	private Dictionary<Point3, FireData> m_fireData;

	private DynamicArray<Point3> m_firePointsCopy;

	private Dictionary<Point3, float> m_toBurnAway;

	private Dictionary<Point3, float> m_toExpand;

	private int m_copyIndex;

	private float m_remainderToScan;

	private double m_lastScanTime;

	private float m_lastScanDuration;

	private Random m_random;

	private SubsystemWeather m_subsystemWeather;

	private Dictionary<Point3, NaturalSmokeParticleSystem> m_fireSmokeParticleSystemsByCell;

	private SubsystemTerrain m_subsystemTerrain;

	private Dictionary<Point3, bool> m_fireSmokeEnabled;

	private Dictionary<Point3, FakeSmokeParticleSystem> m_fakeSmokeParticleSystemsByCell;

	private List<Point3> m_tempBurnKeys;

	private List<Point3> m_tempExpandKeys;

	private Dictionary<Point3, RealFireParticleSystem> m_fireParticleSystems = new Dictionary<Point3, RealFireParticleSystem>();

	private float m_fireSoundVolume;

	private float m_fireSoundIntensity;

	private SubsystemGameInfo m_subsystemGameInfo;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[1] { 104 };

	public bool IsCellOnFire(int x, int y, int z)
	{
		for (int i = 0; i < 4; i++)
		{
			Point3 point = CellFace.FaceToPoint3(i);
			int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
			if (Terrain.ExtractContents(cellValue) == 104)
			{
				bool flag = Terrain.ExtractData(cellValue) != 0;
				int num = CellFace.OppositeFace(i);
				if (((flag ? 1 : 0) & (1 << num)) != 0)
				{
					return true;
				}
			}
		}
		int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (Terrain.ExtractContents(cellValue2) == 104)
		{
			return Terrain.ExtractData(cellValue2) == 0;
		}
		return false;
	}

	public bool SetCellOnFire(int x, int y, int z, float fireExpandability, ComponentMiner miner = null)
	{
		bool blockMesh;
		if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var model))
		{
			if (miner == null || !miner.ComponentPlayer.PlayerData.Friends.Contains(model.OwnerGuid) || !model.ApplyToFirend)
			{
				Guid ownerGuid = model.OwnerGuid;
				Guid? guid = miner?.ComponentPlayer.PlayerData.PlayerGUID;
				if (!(ownerGuid == guid))
				{
					blockMesh = model.AllowDig;
				}
				else
				{
					blockMesh = true;
				}
			}
			else
			{
				blockMesh = true;
			}
			bool blockMesh2 = blockMesh;
			if (model.BlockFire || !blockMesh2)
			{
				miner?.ComponentPlayer.ComponentGui.DisplaySmallMessage(model.BlockFire ? "Огонь заблокирован на этой территории." : "У вас нет разрешения использовать это.", Color.Red, blinking: false, playNotificationSound: true);
				return false;
			}
		}
		if (!IsBlockFlammable(SubsystemTerrain.Terrain.GetCellValue(x, y, z)))
		{
			return false;
		}
		bool boneAbsoluteTransform = false;
		SubsystemWeather.Season boneAbsoluteTransform2 = m_subsystemWeather.CurrentSeason;
		if (IsColdSeason(boneAbsoluteTransform2))
		{
			for (int i = 1; i < 5; i++)
			{
				Point3 point = CellFace.FaceToPoint3(i);
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
				int num = Terrain.ExtractContents(cellValue);
				if (num == 0 || num == 104 || num == 61)
				{
					int num2 = ((num == 104) ? Terrain.ExtractData(cellValue) : 0);
					int num3 = CellFace.OppositeFace(i);
					num2 |= (1 << num3) & 0xF;
					int value = Terrain.ReplaceData(Terrain.ReplaceContents(0, 104), num2);
					AddFire(x + point.X, y + point.Y, z + point.Z, fireExpandability);
					SubsystemTerrain.ChangeCell(x + point.X, y + point.Y, z + point.Z, value);
					boneAbsoluteTransform = true;
				}
			}
		}
		else if (IsSummerSeason(boneAbsoluteTransform2))
		{
			for (int j = 0; j < 5; j++)
			{
				Point3 point2 = CellFace.FaceToPoint3(j);
				int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x + point2.X * 2, y + point2.Y * 2, z + point2.Z * 2);
				int num4 = Terrain.ExtractContents(cellValue2);
				if (num4 == 0 || num4 == 104 || num4 == 61)
				{
					int num5 = ((num4 == 104) ? Terrain.ExtractData(cellValue2) : 0);
					int num6 = CellFace.OppositeFace(j);
					num5 |= (1 << num6) & 0xF;
					int value2 = Terrain.ReplaceData(Terrain.ReplaceContents(0, 104), num5);
					AddFire(x + point2.X * 2, y + point2.Y * 2, z + point2.Z * 2, fireExpandability);
					SubsystemTerrain.ChangeCell(x + point2.X * 2, y + point2.Y * 2, z + point2.Z * 2, value2);
					boneAbsoluteTransform = true;
				}
			}
		}
		else
		{
			for (int k = 0; k < 5; k++)
			{
				Point3 point3 = CellFace.FaceToPoint3(k);
				int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(x + point3.X, y + point3.Y, z + point3.Z);
				int num7 = Terrain.ExtractContents(cellValue3);
				if (num7 == 0 || num7 == 104 || num7 == 61)
				{
					int num8 = ((num7 == 104) ? Terrain.ExtractData(cellValue3) : 0);
					int num9 = CellFace.OppositeFace(k);
					num8 |= (1 << num9) & 0xF;
					int value3 = Terrain.ReplaceData(Terrain.ReplaceContents(0, 104), num8);
					AddFire(x + point3.X, y + point3.Y, z + point3.Z, fireExpandability);
					SubsystemTerrain.ChangeCell(x + point3.X, y + point3.Y, z + point3.Z, value3);
					boneAbsoluteTransform = true;
				}
			}
		}
		if (boneAbsoluteTransform && miner != null && miner.ComponentCreature != null)
		{
			PlayerAchievements playerAchievements = miner.ComponentCreature.PlayerAchievements;
			if (playerAchievements != null && playerAchievements.FireIsMyDomain == 0L)
			{
				playerAchievements.FireIsMyDomain = 1L;
				playerAchievements.EnqueueFireIsMyDomainAchievement();
			}
		}
		return boneAbsoluteTransform;
	}

	public void Update(float dt)
	{
		float currentWindStrength = m_subsystemWeather.CurrentWindStrength;
		float windMod = 1f;
		if (currentWindStrength >= 1f && currentWindStrength <= 6f)
		{
			windMod = MathUtils.Lerp(1f, 2f, (currentWindStrength - 1f) / 5f);
		}
		else if (currentWindStrength >= 10f && currentWindStrength <= 16f)
		{
			windMod = MathUtils.Lerp(1f, 0.33f, (currentWindStrength - 10f) / 6f);
		}
		if (m_firePointsCopy.Count == 0)
		{
			m_firePointsCopy.Count += m_fireData.Count;
			m_fireData.Keys.CopyTo(m_firePointsCopy.Array, 0);
			m_copyIndex = 0;
			m_lastScanDuration = (float)(m_subsystemTime.GameTime - m_lastScanTime);
			m_lastScanTime = m_subsystemTime.GameTime;
			if (m_firePointsCopy.Count == 0)
			{
				m_fireSoundVolume = 0f;
			}
		}
		if (m_firePointsCopy.Count > 0)
		{
			float num = MathUtils.Min(1f * dt * (float)m_firePointsCopy.Count + m_remainderToScan, 50f);
			int num2 = (int)num;
			m_remainderToScan = num - (float)num2;
			int num3 = MathUtils.Min(m_copyIndex + num2, m_firePointsCopy.Count);
			while (m_copyIndex < num3)
			{
				Point3 key = m_firePointsCopy.Array[m_copyIndex];
				if (m_fireData.TryGetValue(key, out var value))
				{
					m_fireSoundIntensity += 4f / (m_subsystemAudio.CalculateListenerDistanceSquared(new Vector3(key.X, key.Y, key.Z)) + 0.01f);
					if (m_subsystemGameInfo.WorldSettings.FireSpreadEnabled)
					{
						int num4 = Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z));
						if ((num4 & 1) != 0)
						{
							UpdateFireFace(value, key.X, key.Y, key.Z, 0, 0, 1, ref value.Time0, windMod);
						}
						if ((num4 & 2) != 0)
						{
							UpdateFireFace(value, key.X, key.Y, key.Z, 1, 0, 0, ref value.Time1, windMod);
						}
						if ((num4 & 4) != 0)
						{
							UpdateFireFace(value, key.X, key.Y, key.Z, 0, 0, -1, ref value.Time2, windMod);
						}
						if ((num4 & 8) != 0)
						{
							UpdateFireFace(value, key.X, key.Y, key.Z, -1, 0, 0, ref value.Time3, windMod);
						}
						if (num4 == 0)
						{
							UpdateFireFace(value, key.X, key.Y, key.Z, 0, -1, 0, ref value.Time5, windMod);
						}
					}
				}
				m_copyIndex++;
			}
			if (m_copyIndex >= m_firePointsCopy.Count)
			{
				m_fireSoundVolume = 0.75f * m_fireSoundIntensity;
				m_firePointsCopy.Clear();
				m_fireSoundIntensity = 0f;
			}
		}
		ProcessBurnAwayQueue();
		ProcessExpandQueue();
		m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_fireSoundVolume);
		float num5 = ((m_fireSoundVolume > 0.5f) ? ((m_fireSoundVolume - 0.5f) * 2f) : 0f);
		float num6 = MathUtils.Saturate((float)(m_fireData.Count - 20) / 30f);
		num5 *= num6;
		m_subsystemAmbientSounds.BlazeSoundVolume = MathUtils.Lerp(m_subsystemAmbientSounds.BlazeSoundVolume, num5, 5f * dt);
	}

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int num = Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z));
		if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) == 104)
		{
			Point3 point = new Point3(x, y, z);
			if (m_fireParticleSystems.ContainsKey(point))
			{
				m_fireParticleSystems[point].IsStopped = true;
				m_fireParticleSystems.Remove(point);
			}
			UpdateFireParticles(point);
		}
		if ((num & 1) != 0 && !IsBlockFlammable(SubsystemTerrain.Terrain.GetCellValue(x, y, z + 1)))
		{
			num &= -2;
		}
		if ((num & 2) != 0 && !IsBlockFlammable(SubsystemTerrain.Terrain.GetCellValue(x + 1, y, z)))
		{
			num &= -3;
		}
		if ((num & 4) != 0 && !IsBlockFlammable(SubsystemTerrain.Terrain.GetCellValue(x, y, z - 1)))
		{
			num &= -5;
		}
		if ((num & 8) != 0 && !IsBlockFlammable(SubsystemTerrain.Terrain.GetCellValue(x - 1, y, z)))
		{
			num &= -9;
		}
		if (m_fireData.TryGetValue(new Point3(x, y, z), out var value))
		{
			if ((num & 1) != 0 && neighborX == x && neighborY == y && neighborZ == z + 1)
			{
				InitializeFireDataTime(value, 0);
			}
			if ((num & 2) != 0 && neighborX == x + 1 && neighborY == y && neighborZ == z)
			{
				InitializeFireDataTime(value, 1);
			}
			if ((num & 4) != 0 && neighborX == x && neighborY == y && neighborZ == z - 1)
			{
				InitializeFireDataTime(value, 2);
			}
			if ((num & 8) != 0 && neighborX == x - 1 && neighborY == y && neighborZ == z)
			{
				InitializeFireDataTime(value, 3);
			}
			if (num == 0 && neighborX == x && neighborY == y - 1 && neighborZ == z)
			{
				InitializeFireDataTime(value, 5);
			}
		}
		int contents = 104;
		if (num == 0 && !IsBlockFlammable(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)))
		{
			contents = 0;
		}
		int value2 = Terrain.ReplaceData(Terrain.ReplaceContents(0, contents), num);
		SubsystemTerrain.ChangeCell(x, y, z, value2);
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		AddFire(x, y, z, 1f);
		UpdateFireParticles(new Point3(x, y, z));
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		RemoveFire(x, y, z);
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		AddFire(x, y, z, 1f);
		UpdateFireParticles(new Point3(x, y, z));
	}

	public override void OnChunkDiscarding(TerrainChunk chunk)
	{
		List<Point3> list = new List<Point3>();
		foreach (Point3 key in m_fireData.Keys)
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

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		m_fireParticleSystems.Clear();
		for (int i = -2; i <= 2; i++)
		{
			for (int j = -1; j <= 2; j++)
			{
				for (int k = -2; k <= 2; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						float num = ((j < 0) ? 1.5f : 2.5f);
						if (MathUtils.Sqrt(i * i + j * j + k * k) <= num)
						{
							float num2 = MathUtils.Sqrt(i * i + k * k);
							float num3 = ((j > 0) ? (0.5f * (float)j) : (0f - (float)j));
							m_expansionProbabilities[new Point3(i, j, k)] = 0.02f / (num2 + num3);
						}
					}
				}
			}
		}
	}

	private void AddFire(int x, int y, int z, float expandability)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new ComponentOnFirePackage(x, y, z, expandability));
			AddFireNet(x, y, z, expandability);
		}
	}

	public void AddFireNet(int x, int y, int z, float expandability)
	{
		Point3 point = new Point3(x, y, z);
		if (m_fireData.ContainsKey(point))
		{
			return;
		}
		FireData fireData = new FireData();
		fireData.Point = point;
		fireData.FireExpandability = expandability;
		InitializeFireDataTimes(fireData);
		m_fireData[point] = fireData;
		if (m_fireSmokeParticleSystemsByCell.Count < 150)
		{
			if (!m_fireSmokeEnabled.TryGetValue(point, out var value))
			{
				float num = (float)SettingsManager.SmokeDensity / 100f;
				value = m_random.Float(0f, 1f) < num;
				m_fireSmokeEnabled[point] = value;
			}
			if (value)
			{
				AddFireSmokeParticleSystem(x, y, z, Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(x, y, z)));
				return;
			}
		}
		else
		{
			m_fireSmokeEnabled[point] = false;
		}
		UpdateFireParticles(point);
	}

	private void RemoveFire(int x, int y, int z)
	{
		if (CommonLib.WorkType != WorkType.Client)
		{
			CommonLib.Net.QueuePackage(new ComponentOnFirePackage(x, y, z));
			RemoveFireNet(x, y, z);
		}
	}

	public void RemoveFireNet(int x, int y, int z)
	{
		Point3 key = new Point3(x, y, z);
		m_fireData.Remove(key);
		RemoveFireSmokeParticleSystem(x, y, z);
		m_fireSmokeEnabled.Remove(key);
		if (m_fireParticleSystems.TryGetValue(key, out var value))
		{
			value.IsStopped = true;
			m_fireParticleSystems.Remove(key);
		}
	}

	private void InitializeFireDataTimes(FireData fireData)
	{
		InitializeFireDataTime(fireData, 0);
		InitializeFireDataTime(fireData, 1);
		InitializeFireDataTime(fireData, 2);
		InitializeFireDataTime(fireData, 3);
		InitializeFireDataTime(fireData, 5);
	}

	private void InitializeFireDataTime(FireData fireData, int face)
	{
		Point3 blockMesh = CellFace.FaceToPoint3(face);
		int x = fireData.Point.X + blockMesh.X;
		int y = fireData.Point.Y + blockMesh.Y;
		int z = fireData.Point.Z + blockMesh.Z;
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y, z);
		Block block = BlocksManager.Blocks[cellContents];
		switch (face)
		{
		case 0:
			fireData.Time0 = block.FireDuration * m_random.Float(0.75f, 1.25f);
			break;
		case 1:
			fireData.Time1 = block.FireDuration * m_random.Float(0.75f, 1.25f);
			break;
		case 2:
			fireData.Time2 = block.FireDuration * m_random.Float(0.75f, 1.25f);
			break;
		case 3:
			fireData.Time3 = block.FireDuration * m_random.Float(0.75f, 1.25f);
			break;
		case 4:
			break;
		case 5:
			fireData.Time5 = block.FireDuration * m_random.Float(0.75f, 1.25f);
			break;
		}
	}

	private void QueueBurnAway(int x, int y, int z, float expandability)
	{
		Point3 key = new Point3(x, y, z);
		if (!m_toBurnAway.ContainsKey(key))
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
			int num = 0;
			if (block is WoodBlock || block is ChestBlock || block is DispenserBlock)
			{
				num = Terrain.MakeBlockValue(378);
			}
			else if (block is LeavesBlock && m_random.Float(0f, 1f) < 0.1f)
			{
				num = Terrain.MakeBlockValue(377);
			}
			else if (block is CrossBlock && m_random.Float(0f, 1f) < 0.8f)
			{
				num = Terrain.MakeBlockValue(377);
			}
			else if (block is FenceBlock || block is FenceGateBlock || block is DoorBlock || block is TrapdoorBlock)
			{
				num = Terrain.MakeBlockValue(377);
			}
			else if ((block is PlanksBlock || block is CoalBlock) && m_random.Float(0f, 1f) < 0.5f)
			{
				num = Terrain.MakeBlockValue(378);
			}
			if (num != 0)
			{
				m_subsystemTerrain.ChangeCell(x, y, z, num);
				return;
			}
			if (Terrain.ExtractContents(cellValue) == 378)
			{
				m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(376));
				return;
			}
			float num2 = ((m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Anabiosis || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Winter) ? 2f : 1f);
			m_toBurnAway.Add(key, expandability * num2);
		}
	}

	public SubsystemFireBlockBehavior()
	{
		m_tempBurnKeys = new List<Point3>();
		m_tempExpandKeys = new List<Point3>();
		m_fakeSmokeParticleSystemsByCell = new Dictionary<Point3, FakeSmokeParticleSystem>();
		m_fireSmokeEnabled = new Dictionary<Point3, bool>();
		m_fireSmokeParticleSystemsByCell = new Dictionary<Point3, NaturalSmokeParticleSystem>();
		m_expansionProbabilities = new Dictionary<Point3, float>();
		m_fireData = new Dictionary<Point3, FireData>();
		m_firePointsCopy = new DynamicArray<Point3>();
		m_toBurnAway = new Dictionary<Point3, float>();
		m_toExpand = new Dictionary<Point3, float>();
		m_random = new Random();
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_random.Float(0f, 1f) < 1f)
		{
			PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
			if (precipitationShaftInfo.Intensity > 0f && y >= precipitationShaftInfo.YLimit - 1)
			{
				SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
			}
		}
	}

	private void RemoveFireSmokeParticleSystem(int x, int y, int z)
	{
		Point3 data = new Point3(x, y, z);
		NaturalSmokeParticleSystem connectorDirection;
		if (SettingsManager.SimplifySmokePhysics)
		{
			if (m_fakeSmokeParticleSystemsByCell.TryGetValue(data, out var value))
			{
				value.IsStopped = true;
				m_fakeSmokeParticleSystemsByCell.Remove(data);
			}
		}
		else if (m_fireSmokeParticleSystemsByCell.TryGetValue(data, out connectorDirection))
		{
			connectorDirection.IsStopped = true;
			m_fireSmokeParticleSystemsByCell.Remove(data);
		}
	}

	private void AddFireSmokeParticleSystem(int x, int y, int z, int fireData)
	{
		float particleSize = 0.5f;
		float num = 0.5f;
		Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
		switch (fireData)
		{
		case 0:
			position = new Vector3((float)x + 0.5f, (float)y + 1f, (float)z + 0.5f);
			break;
		case 1:
			position.Y += 0.5f;
			break;
		case 2:
			position.X += 0.5f;
			break;
		case 4:
			position.Y -= 0.5f;
			break;
		case 8:
			position.X -= 0.5f;
			break;
		}
		Point3 key = new Point3(x, y, z);
		if (SettingsManager.SimplifySmokePhysics)
		{
			FakeSmokeParticleSystem fakeSmokeParticleSystem = new FakeSmokeParticleSystem(m_subsystemTerrain, m_subsystemWeather, position, 256f)
			{
				Position = position,
				ParticleSize = particleSize,
				SpawnRateMultiplier = num * m_random.Float(0.8f, 1.2f),
				Color = Color.White
			};
			m_subsystemParticles.AddParticleSystem(fakeSmokeParticleSystem);
			m_fakeSmokeParticleSystemsByCell[key] = fakeSmokeParticleSystem;
		}
		else
		{
			float maxVisibilityDistance = 256f;
			float spawnRateMultiplier = num * m_random.Float(0.8f, 1.2f);
			float verticalSpeed = MathUtils.Lerp(0.5f, 1.5f, m_random.Float(0.8f, 1.2f));
			NaturalSmokeParticleSystem naturalSmokeParticleSystem = new NaturalSmokeParticleSystem(m_subsystemTerrain, m_subsystemWeather, position, maxVisibilityDistance)
			{
				Position = position,
				Direction = new Vector3(0f, 1f, 0f),
				ParticleSize = particleSize,
				SpawnRateMultiplier = spawnRateMultiplier,
				Color = Color.White,
				VerticalSpeed = verticalSpeed,
				RandomOffsetEnabled = true
			};
			m_subsystemParticles.AddParticleSystem(naturalSmokeParticleSystem);
			m_fireSmokeParticleSystemsByCell[key] = naturalSmokeParticleSystem;
		}
	}

	private bool IsColdSeason(SubsystemWeather.Season season)
	{
		if (season != SubsystemWeather.Season.Anabiosis && season != SubsystemWeather.Season.Winter)
		{
			return season == SubsystemWeather.Season.LateWinter;
		}
		return true;
	}

	private bool IsSummerSeason(SubsystemWeather.Season season)
	{
		if (season != SubsystemWeather.Season.Summer && season != SubsystemWeather.Season.EarlySummer)
		{
			return season == SubsystemWeather.Season.LateSummer;
		}
		return true;
	}

	private void UpdateFireFace(FireData fireData, int x, int y, int z, int dx, int dy, int dz, ref float time, float windMod)
	{
		time -= m_lastScanDuration * windMod;
		if (time <= 0f)
		{
			float currentSeason = ((m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Anabiosis || m_subsystemWeather.CurrentSeason == SubsystemWeather.Season.Winter) ? 2f : 1f);
			QueueBurnAway(x + dx, y + dy, z + dz, fireData.FireExpandability * 0.85f * currentSeason);
		}
		foreach (KeyValuePair<Point3, float> color2 in m_expansionProbabilities)
		{
			if (m_random.Float(0f, 1f) < color2.Value * m_lastScanDuration * fireData.FireExpandability)
			{
				Point3 distance = new Point3(x + dx + color2.Key.X, y + dy + color2.Key.Y, z + dz + color2.Key.Z);
				m_toExpand[distance] = fireData.FireExpandability * 0.85f;
			}
		}
	}

	private void ProcessBurnAwayQueue()
	{
		if (m_toBurnAway.Count <= 0)
		{
			return;
		}
		int flag = 0;
		int wormKind = 0;
		m_tempBurnKeys.Clear();
		foreach (Point3 humidity in m_toBurnAway.Keys)
		{
			m_tempBurnKeys.Add(humidity);
			if (m_tempBurnKeys.Count >= 6)
			{
				break;
			}
		}
		foreach (Point3 tempBurnKey in m_tempBurnKeys)
		{
			float num = m_toBurnAway[tempBurnKey];
			m_toBurnAway.Remove(tempBurnKey);
			SubsystemTerrain.ChangeCell(tempBurnKey.X, tempBurnKey.Y, tempBurnKey.Z, Terrain.ReplaceContents(0, 0));
			if (num > 0.25f)
			{
				for (int i = 0; i < 5; i++)
				{
					Point3 point = CellFace.FaceToPoint3(i);
					SetCellOnFire(tempBurnKey.X + point.X, tempBurnKey.Y + point.Y, tempBurnKey.Z + point.Z, num);
				}
			}
			float num2 = m_subsystemViews.CalculateDistanceFromNearestView(new Vector3(tempBurnKey));
			if (flag < 5 && num2 < 16f)
			{
				m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(SubsystemTerrain, tempBurnKey.X, tempBurnKey.Y, tempBurnKey.Z));
				flag++;
			}
			if (wormKind < 2 && num2 < 10f)
			{
				m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.25f, 0.25f), new Vector3(tempBurnKey), 3f, autoDelay: true);
				wormKind++;
			}
		}
	}

	private void ProcessExpandQueue()
	{
		if (m_toExpand.Count <= 0)
		{
			return;
		}
		m_tempExpandKeys.Clear();
		foreach (Point3 key in m_toExpand.Keys)
		{
			m_tempExpandKeys.Add(key);
			if (m_tempExpandKeys.Count >= 64)
			{
				break;
			}
		}
		foreach (Point3 tempExpandKey in m_tempExpandKeys)
		{
			if (m_toExpand.TryGetValue(tempExpandKey, out var value))
			{
				m_toExpand.Remove(tempExpandKey);
				SetCellOnFire(tempExpandKey.X, tempExpandKey.Y, tempExpandKey.Z, value);
			}
		}
	}

	private void UpdateFireParticles(Point3 point)
	{
		int i = 0;
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point.X + 1, point.Y, point.Z)) == 104)
		{
			i++;
		}
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point.X - 1, point.Y, point.Z)) == 104)
		{
			i++;
		}
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z + 1)) == 104)
		{
			i++;
		}
		if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z - 1)) == 104)
		{
			i++;
		}
		bool flag = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y + 1, point.Z)) == 104;
		float j = 0.25f + (float)i * 0.15f;
		if (flag)
		{
			j += 0.4f;
		}
		j = MathUtils.Min(j, 1.2f);
		int num = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z));
		Vector3 k = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.5f, (float)point.Z + 0.5f);
		Vector3 num2 = Vector3.Zero;
		int num3 = 0;
		if ((num & 1) != 0)
		{
			num2 += new Vector3(0f, 0f, 0.4f);
			num3++;
		}
		if ((num & 2) != 0)
		{
			num2 += new Vector3(0.4f, 0f, 0f);
			num3++;
		}
		if ((num & 4) != 0)
		{
			num2 += new Vector3(0f, 0f, -0.4f);
			num3++;
		}
		if ((num & 8) != 0)
		{
			num2 += new Vector3(-0.4f, 0f, 0f);
			num3++;
		}
		if (num3 == 0)
		{
			num2 = new Vector3(0f, -0.4f, 0f);
		}
		else
		{
			num2 /= (float)num3;
		}
		Vector3 position = k + num2;
		if (m_fireParticleSystems.TryGetValue(point, out var value))
		{
			value.Position = position;
			return;
		}
		RealFireParticleSystem realFireParticleSystem = new RealFireParticleSystem(position, j, 40f);
		realFireParticleSystem.Initialize(m_subsystemWeather);
		m_subsystemParticles.AddParticleSystem(realFireParticleSystem);
		m_fireParticleSystems[point] = realFireParticleSystem;
	}

	public int ClearAllFires()
	{
		int x = 0;
		foreach (Point3 z in new List<Point3>(m_fireData.Keys))
		{
			if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(z.X, z.Y, z.Z)) == 104)
			{
				m_subsystemTerrain.ChangeCell(z.X, z.Y, z.Z, 0);
			}
			RemoveFireNet(z.X, z.Y, z.Z);
			x++;
		}
		m_toBurnAway.Clear();
		m_toExpand.Clear();
		return x;
	}

	public bool IsBlockFlammable(int cellValue)
	{
		Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
		if (block.FireDuration != 0f && (!(block is LeavesBlock leavesBlock) || !leavesBlock.IsInvisible(SubsystemTerrain, cellValue)))
		{
			if (block is IvyBlock ivyBlock)
			{
				return !ivyBlock.IsInvisible(SubsystemTerrain, cellValue);
			}
			return true;
		}
		return false;
	}
}
