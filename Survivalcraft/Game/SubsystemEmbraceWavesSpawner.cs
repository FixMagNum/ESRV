using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemEmbraceWavesSpawner : Subsystem, IUpdateable
{
	private class PlayerState
	{
		public ComponentPlayer Player;

		public ComponentAdrenaline Adrenaline;

		public ComponentBody Body;

		public double NextHarasserTime;

		public double NextWaveTime;

		public float LastAdrenaline;

		public float AdrenalineFallTimer;

		public int CurrentWave;

		public BiomeType Biome;

		public Entity Harasser;

		public List<Entity> Spawned = new List<Entity>();

		public bool CombatMusicPlaying;
	}

	private enum BiomeType
	{
		Moderate,
		Winter,
		Desert
	}

	private struct WaveDef
	{
		public int MinCount;

		public int MaxCount;

		public float MinSize;

		public float MaxSize;

		public bool IsBoss;

		public int BossMinions;

		public float BossSize;

		public float BossMinionSize;
	}

	private const float HarasserAdrenalineThreshold = 0.4f;

	private const float WaveAdrenalineThreshold = 0.4f;

	private const double HarasserPeriodSec = 60.0;

	private const double WaveStepSec = 15.0;

	private const float AdrenalineFallEps = 0.01f;

	private const float AdrenalineFallGraceSec = 3f;

	private const int MaxWaveMobsPerPlayer = 40;

	private const float SpawnRadiusMin = 18f;

	private const float SpawnRadiusMax = 32f;

	private const int SpawnTries = 14;

	public SubsystemGameInfo m_gameInfo;

	public SubsystemTime m_time;

	public SubsystemTerrain m_terrain;

	public SubsystemSky m_sky;

	public SubsystemBodies m_bodies;

	public SubsystemAudio m_audio;

	private Random m_random = new Random();

	private readonly List<WaveDef> m_waves = new List<WaveDef>(32);

	private readonly string[] PoolModerateLight = new string[6] { "Wolf_Gray", "Wolf_Coyote", "Hyena", "Wildboar", "Jaguar", "Leopard" };

	private readonly string[] PoolModerateHeavy = new string[8] { "Bear_Brown", "Bear_Black", "Lion", "Tiger", "Rhino", "Bison", "Moose", "Gnu" };

	private readonly string[] PoolWinterLight = new string[5] { "Wolf_White", "Wolf_Gray", "Hyena", "Leopard", "Wildboar" };

	private readonly string[] PoolWinterHeavy = new string[5] { "Bear_Polar", "Bear_Brown", "Bear_Black", "Tiger_White", "Moose" };

	private readonly string[] PoolDesertLight = new string[5] { "Wolf_Coyote", "Wolf_Gray", "Hyena", "Wildboar", "Jaguar" };

	private readonly string[] PoolDesertHeavy = new string[5] { "Lion", "Tiger", "Rhino", "Bison", "Bear_Brown" };

	private readonly string[] Difficulty = new string[31]
	{
		"Piece of cake", "Barely a warm-up", "Is that all?", "Light breeze", "Need a candle maybe", "Now we’re jogging", "Getting interesting", "Mildly inconvenient", "Stop daydreaming", "Bring a snack",
		"Okay, focus", "Wiggle those toes", "Who ordered chaos?", "Your ego is sweating", "Still not serious", "Knock knock, karma", "Try dodging", "Your mind twitched", "Oh, that stung", "Now we dance",
		"Whispers of panic", "Silence the thinker", "Sparks in the void", "Chew your fear", "Ego on thin ice", "Look who’s humble", "Breath meets fire", "Mind unravels", "Self is optional", "Nobody home",
		"Advaita: ego off"
	};

	public SubsystemWeather m_weather;

	private readonly Dictionary<int, PlayerState> m_playerStates = new Dictionary<int, PlayerState>();

	private readonly List<ComponentPlayer> m_tempPlayers = new List<ComponentPlayer>(16);

	private readonly List<int> m_toRemove = new List<int>(8);

	public SubsystemAmbientMusic m_music;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void Update(float dt)
	{
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_gameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_time = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_terrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_sky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_bodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_audio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_weather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_music = Project.FindSubsystem<SubsystemAmbientMusic>(throwOnError: false);
		BuildWaves();
	}

	private void BuildWaves()
	{
		m_waves.Clear();
		m_waves.Add(default);
		for (int i = 1; i <= 31; i++)
		{
			int num = 4 + i * 2;
			int num2 = MathUtils.Clamp(num - 3, 3, 32);
			int maxCount = MathUtils.Clamp(num + 2, num2 + 1, 40);
			float num3 = 0.55f + 0.05f * (float)i;
			float x = num3 + 0.6f;
			num3 = MathUtils.Clamp(num3, 0.5f, 3f);
			x = MathUtils.Clamp(x, num3, 3f);
			bool flag = i % 5 == 0 && i < 31;
			WaveDef item = new WaveDef
			{
				MinCount = num2,
				MaxCount = maxCount,
				MinSize = num3,
				MaxSize = x,
				IsBoss = flag,
				BossMinions = 0,
				BossSize = 0f,
				BossMinionSize = 0f
			};
			if (flag)
			{
				item.BossMinions = MathUtils.Clamp(num2 + 8, 12, 39);
				item.BossSize = 3.5f + 0.3f * (float)(i / 5);
				item.BossMinionSize = MathUtils.Clamp(num3 + 0.6f, 1.2f, 2.8f);
			}
			m_waves.Add(item);
		}
	}

	private void SpawnWave(PlayerState st)
	{
		int num = MathUtils.Clamp(st.CurrentWave, 1, 31);
		WaveDef waveDef = m_waves[num];
		PruneSpawned(st);
		if (st.Spawned.Count >= 40)
		{
			return;
		}
		string smallText = FormatWaveBanner(st.Biome, num, Difficulty[num - 1]);
		st.Player.ComponentGui.DisplayLargeMessage(null, smallText, 3.5f, 0f);
		GetPools(st.Biome, out var lightPool, out var heavyPool);
		if (num >= 3)
		{
			StartCombatMusic(st);
		}
		if (waveDef.IsBoss)
		{
			string templateName = Choose(heavyPool);
			if (TrySpawnAtPlayerArea(st, templateName, waveDef.BossSize, out var entity))
			{
				st.Spawned.Add(entity);
			}
			int bossMinions = waveDef.BossMinions;
			for (int i = 0; i < bossMinions; i++)
			{
				if (st.Spawned.Count >= 40)
				{
					break;
				}
				string templateName2 = ((i % 2 == 0) ? Choose(lightPool) : Choose(heavyPool));
				if (TrySpawnAtPlayerArea(st, templateName2, waveDef.BossMinionSize, out var entity2))
				{
					st.Spawned.Add(entity2);
				}
			}
			return;
		}
		int num2 = m_random.Int(waveDef.MinCount, waveDef.MaxCount);
		for (int j = 0; j < num2; j++)
		{
			if (st.Spawned.Count >= 40)
			{
				break;
			}
			string templateName3 = ((j % 3 == 2 || (num >= 12 && m_random.Float(0f, 1f) < 0.4f)) ? Choose(heavyPool) : Choose(lightPool));
			float x = ((waveDef.MinSize == waveDef.MaxSize) ? waveDef.MinSize : m_random.Float(waveDef.MinSize, waveDef.MaxSize));
			x = MathUtils.Clamp(x, 0.5f, 3f);
			if (TrySpawnAtPlayerArea(st, templateName3, x, out var entity3))
			{
				st.Spawned.Add(entity3);
			}
		}
	}

	private void ResetWaves(PlayerState st, bool clearHarasser)
	{
		st.CurrentWave = 0;
		st.NextWaveTime = 0.0;
		if (clearHarasser)
		{
			st.Harasser = null;
		}
		PruneSpawned(st);
		StopCombatMusic(st);
	}

	private string FormatWaveBanner(BiomeType biome, int wave, string diff)
	{
		return string.Format("Wave {0} — {1} — Difficulty: {2}", wave, biome switch
		{
			BiomeType.Winter => (object)"Winter", 
			BiomeType.Moderate => "Moderate", 
			_ => "Desert", 
		}, diff);
	}

	private int AdrenalineToStartWave(float a)
	{
		if (a < 0.6f)
		{
			return 1;
		}
		if (a < 0.9f)
		{
			return 3;
		}
		if (a < 1.3f)
		{
			return 6;
		}
		if (a < 1.8f)
		{
			return 9;
		}
		if (a < 2.5f)
		{
			return 12;
		}
		return 15;
	}

	private bool TrySpawnHarasser(PlayerState st, out Entity entity)
	{
		entity = null;
		GetPools(st.Biome, out var lightPool, out var _);
		string templateName = Choose(lightPool);
		return TrySpawnAtPlayerArea(st, templateName, 0.5f, out entity);
	}

	private bool IsHarasserValid(PlayerState st)
	{
		if (st.Harasser == null)
		{
			return false;
		}
		ComponentBody componentBody = st.Harasser.FindComponent<ComponentBody>();
		ComponentHealth componentHealth = st.Harasser.FindComponent<ComponentHealth>();
		if (componentBody == null || componentHealth == null)
		{
			return false;
		}
		if (componentHealth.Health <= 0f)
		{
			return false;
		}
		Vector3 position = st.Body.Position;
		Vector3 position2 = componentBody.Position;
		return (position2.X - position.X) * (position2.X - position.X) + (position2.Z - position.Z) * (position2.Z - position.Z) <= 1764f;
	}

	private bool TrySpawnAtPlayerArea(PlayerState st, string templateName, float size, out Entity entity)
	{
		entity = null;
		Point3? point = FindSpawnPointNear(st.Body.Position);
		if (!point.HasValue)
		{
			return false;
		}
		try
		{
			Entity entity2 = DatabaseManager.CreateEntity(Project, templateName, throwIfNotFound: true);
			ComponentBody componentBody = entity2.FindComponent<ComponentBody>(throwOnError: true);
			ComponentCreature cc = entity2.FindComponent<ComponentCreature>(throwOnError: true);
			if (!IsAllowedCategory(cc))
			{
				return false;
			}
			componentBody.Position = new Vector3((float)point.Value.X + m_random.Float(0.4f, 0.6f), (float)point.Value.Y + 0.1f, (float)point.Value.Z + m_random.Float(0.4f, 0.6f));
			componentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
			componentBody.Size = size;
			Project.AddEntity(entity2);
			entity = entity2;
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("Embrace spawn failed for \"" + templateName + "\": " + ex.Message);
			return false;
		}
	}

	private bool IsAllowedCategory(ComponentCreature cc)
	{
		if (cc == null)
		{
			return false;
		}
		CreatureCategory category = cc.Category;
		switch (category)
		{
		default:
			return category == CreatureCategory.LandOther;
		case CreatureCategory.LandPredator:
			return true;
		case CreatureCategory.WaterPredator:
		case CreatureCategory.WaterOther:
		case CreatureCategory.Bird:
			return false;
		}
	}

	private Point3? FindSpawnPointNear(Vector3 center)
	{
		Terrain.ToCell(center.X);
		int num = MathUtils.Clamp(Terrain.ToCell(center.Y), 2, 254);
		Terrain.ToCell(center.Z);
		for (int i = 0; i < 14; i++)
		{
			float num2 = m_random.Float(18f, 32f);
			float x = m_random.Float(0f, (float)Math.PI * 2f);
			int x2 = Terrain.ToCell(center.X + num2 * MathUtils.Cos(x));
			int z = Terrain.ToCell(center.Z + num2 * MathUtils.Sin(x));
			int y = MathUtils.Clamp(num + m_random.Int(-8, 8), 2, 254);
			Point3? result = ProcessSpawnPoint(new Point3(x2, y, z));
			if (result.HasValue)
			{
				return result;
			}
		}
		return null;
	}

	private Point3? ProcessSpawnPoint(Point3 pt)
	{
		int x = pt.X;
		int num = MathUtils.Clamp(pt.Y, 2, 254);
		int z = pt.Z;
		TerrainChunk chunkAtCell = m_terrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidPropagatedLight)
		{
			return null;
		}
		for (int i = 0; i < 24; i++)
		{
			Point3 point = new Point3(x, num + i, z);
			if (TestSurfaceOrCave(point))
			{
				return point;
			}
			Point3 point2 = new Point3(x, num - i, z);
			if (TestSurfaceOrCave(point2))
			{
				return point2;
			}
		}
		return null;
	}

	private bool TestSurfaceOrCave(Point3 p)
	{
		if (IsInPrivateTerritory(p))
		{
			return false;
		}
		int x = p.X;
		int y = p.Y;
		int z = p.Z;
		if (y <= 3 || y >= 253)
		{
			return false;
		}
		int cellLightFast = m_terrain.Terrain.GetCellLightFast(x, y, z);
		if (m_sky.SkyLightValue - cellLightFast <= 3)
		{
			int cellContentsFast = m_terrain.Terrain.GetCellContentsFast(x, y - 1, z);
			int cellContentsFast2 = m_terrain.Terrain.GetCellContentsFast(x, y, z);
			int cellContentsFast3 = m_terrain.Terrain.GetCellContentsFast(x, y + 1, z);
			Block block = BlocksManager.Blocks[cellContentsFast];
			Block block2 = BlocksManager.Blocks[cellContentsFast2];
			Block block3 = BlocksManager.Blocks[cellContentsFast3];
			if ((block.IsCollidable || block is FocalorsWaterBlock) && !block2.IsCollidable && !(block2 is FocalorsWaterBlock) && !block3.IsCollidable && !(block3 is FocalorsWaterBlock))
			{
				return true;
			}
		}
		int cellLightFast2 = m_terrain.Terrain.GetCellLightFast(x, y, z);
		if (m_sky.SkyLightValue - cellLightFast2 >= 5)
		{
			int cellContentsFast4 = m_terrain.Terrain.GetCellContentsFast(x, y - 1, z);
			int cellContentsFast5 = m_terrain.Terrain.GetCellContentsFast(x, y, z);
			int cellContentsFast6 = m_terrain.Terrain.GetCellContentsFast(x, y + 1, z);
			Block block4 = BlocksManager.Blocks[cellContentsFast4];
			Block block5 = BlocksManager.Blocks[cellContentsFast5];
			Block block6 = BlocksManager.Blocks[cellContentsFast6];
			if ((block4.IsCollidable || block4 is FocalorsWaterBlock) && !block5.IsCollidable && !(block5 is FocalorsWaterBlock) && !block6.IsCollidable && !(block6 is FocalorsWaterBlock))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsInPrivateTerritory(Point3 p)
	{
		int x = p.X;
		int z = p.Z;
		Territoriy territoriy;
		return SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out territoriy);
	}

	private void GetPools(BiomeType biome, out string[] lightPool, out string[] heavyPool)
	{
		switch (biome)
		{
		case BiomeType.Winter:
			lightPool = PoolWinterLight;
			heavyPool = PoolWinterHeavy;
			break;
		case BiomeType.Desert:
			lightPool = PoolDesertLight;
			heavyPool = PoolDesertHeavy;
			break;
		default:
			lightPool = PoolModerateLight;
			heavyPool = PoolModerateHeavy;
			break;
		}
	}

	private string Choose(string[] pool)
	{
		if (pool == null || pool.Length == 0)
		{
			return "Wolf_Gray";
		}
		int num = m_random.Int(0, pool.Length - 1);
		return pool[num];
	}

	private BiomeType GetBiome(Vector3 pos)
	{
		int x = Terrain.ToCell(pos.X);
		int z = Terrain.ToCell(pos.Z);
		PrecipitationShaftInfo precipitationShaftInfo = m_weather.GetPrecipitationShaftInfo(x, z);
		if (precipitationShaftInfo.Type == PrecipitationType.Snow)
		{
			return BiomeType.Winter;
		}
		if (precipitationShaftInfo.Type == PrecipitationType.Sand)
		{
			return BiomeType.Desert;
		}
		return BiomeType.Moderate;
	}

	private void PruneSpawned(PlayerState st)
	{
		for (int num = st.Spawned.Count - 1; num >= 0; num--)
		{
			Entity entity = st.Spawned[num];
			if (entity == null)
			{
				st.Spawned.RemoveAt(num);
			}
			else
			{
				ComponentHealth componentHealth = entity.FindComponent<ComponentHealth>();
				if (componentHealth != null && componentHealth.Health <= 0f)
				{
					st.Spawned.RemoveAt(num);
				}
			}
		}
	}

	private void EnsurePlayerStates()
	{
		m_tempPlayers.Clear();
		foreach (ComponentBody body in m_bodies.Bodies)
		{
			if (body != null && body.Entity != null)
			{
				ComponentPlayer componentPlayer = body.Entity.FindComponent<ComponentPlayer>();
				if (componentPlayer != null && componentPlayer.PlayerData != null)
				{
					m_tempPlayers.Add(componentPlayer);
				}
			}
		}
		for (int i = 0; i < m_tempPlayers.Count; i++)
		{
			ComponentPlayer componentPlayer2 = m_tempPlayers[i];
			int entityId = componentPlayer2.Entity.EntityId;
			if (!m_playerStates.TryGetValue(entityId, out var value))
			{
				value = new PlayerState();
				value.Player = componentPlayer2;
				value.Body = componentPlayer2.Entity.FindComponent<ComponentBody>();
				value.Adrenaline = componentPlayer2.Entity.FindComponent<ComponentAdrenaline>();
				value.LastAdrenaline = ((value.Adrenaline != null) ? value.Adrenaline.Adrenaline : 0f);
				value.NextHarasserTime = m_time.GameTime + 1.0;
				value.CurrentWave = 0;
				m_playerStates.Add(entityId, value);
			}
			else
			{
				value.Player = componentPlayer2;
				value.Body = componentPlayer2.Entity.FindComponent<ComponentBody>();
				value.Adrenaline = componentPlayer2.Entity.FindComponent<ComponentAdrenaline>();
			}
		}
		m_toRemove.Clear();
		foreach (KeyValuePair<int, PlayerState> playerState in m_playerStates)
		{
			if (!HasPlayer(playerState.Key))
			{
				m_toRemove.Add(playerState.Key);
			}
		}
		for (int j = 0; j < m_toRemove.Count; j++)
		{
			m_playerStates.Remove(m_toRemove[j]);
		}
	}

	private bool HasPlayer(int entityId)
	{
		for (int i = 0; i < m_tempPlayers.Count; i++)
		{
			if (m_tempPlayers[i].Entity.EntityId == entityId)
			{
				return true;
			}
		}
		return false;
	}

	private string ChooseFromBoth(string[] pool1, string[] pool2)
	{
		int num = pool1?.Length ?? 0;
		int num2 = pool2?.Length ?? 0;
		int num3 = num + num2;
		if (num3 <= 0)
		{
			return "Wolf_Gray";
		}
		int num4 = m_random.Int(0, num3 - 1);
		if (num4 < num)
		{
			return pool1[num4];
		}
		return pool2[num4 - num];
	}

	private void SpawnRegularWave(PlayerState st, int waveIndex, WaveDef waveDef, string[] lightPool, string[] heavyPool)
	{
		int num = m_random.Int(waveDef.MinCount, waveDef.MaxCount);
		int num2 = 40 - st.Spawned.Count;
		if (num > num2)
		{
			num = num2;
		}
		if (num <= 0)
		{
			return;
		}
		bool num3 = waveIndex >= 4 && waveIndex % 4 == 0;
		bool flag = waveIndex >= 7 && waveIndex % 3 == 1;
		string text = null;
		if (num3)
		{
			text = ChooseFromBoth(lightPool, heavyPool);
		}
		for (int i = 0; i < num; i++)
		{
			if (st.Spawned.Count >= 40)
			{
				break;
			}
			string templateName;
			if (text != null)
			{
				templateName = text;
			}
			else if (flag)
			{
				templateName = ChooseFromBoth(lightPool, heavyPool);
			}
			else
			{
				bool flag2;
				if (waveIndex <= 2)
				{
					flag2 = false;
				}
				else if (waveIndex <= 5)
				{
					flag2 = i % 4 == 3;
				}
				else
				{
					float x = 0.15f + 0.02f * (float)waveIndex;
					x = MathUtils.Clamp(x, 0.15f, 0.75f);
					flag2 = m_random.Float(0f, 1f) < x;
				}
				templateName = ((!flag2 || heavyPool == null || heavyPool.Length == 0) ? Choose(lightPool) : Choose(heavyPool));
			}
			float min = MathUtils.Clamp(waveDef.MinSize, 0.5f, 3f);
			float max = MathUtils.Clamp(waveDef.MaxSize, min, 3f);
			float x2 = m_random.Float(min, max);
			x2 = MathUtils.Clamp(x2, 0.5f, 3f);
			if (TrySpawnAtPlayerArea(st, templateName, x2, out var entity))
			{
				st.Spawned.Add(entity);
			}
		}
	}

	private void SpawnBossWave(PlayerState st, int waveIndex, WaveDef waveDef, string[] lightPool, string[] heavyPool)
	{
		int num = 40 - st.Spawned.Count;
		if (num <= 0)
		{
			return;
		}
		string templateName = ChooseFromBoth(heavyPool, lightPool);
		float size = MathUtils.Max(3f, waveDef.BossSize);
		if (TrySpawnAtPlayerArea(st, templateName, size, out var entity))
		{
			st.Spawned.Add(entity);
			num--;
		}
		int num2 = MathUtils.Clamp(waveDef.BossMinions, 8, 39);
		if (num2 > num)
		{
			num2 = num;
		}
		for (int i = 0; i < num2; i++)
		{
			if (st.Spawned.Count >= 40)
			{
				break;
			}
			string templateName2 = (((i % 3 != 2 && (waveIndex < 15 || !(m_random.Float(0f, 1f) < 0.4f))) || heavyPool == null || heavyPool.Length == 0) ? ChooseFromBoth(lightPool, heavyPool) : Choose(heavyPool));
			float size2 = MathUtils.Clamp(waveDef.BossMinionSize, 1f, 3f);
			if (TrySpawnAtPlayerArea(st, templateName2, size2, out var entity2))
			{
				st.Spawned.Add(entity2);
			}
		}
	}

	private void StartCombatMusic(PlayerState st)
	{
		if (!st.CombatMusicPlaying && m_music != null && !(SettingsManager.MusicVolume <= 0f))
		{
			m_music.StartEmbraceFightMusic();
			st.CombatMusicPlaying = true;
		}
	}

	private void StopCombatMusic(PlayerState st)
	{
		if (st.CombatMusicPlaying)
		{
			if (m_music != null)
			{
				m_music.StopEmbraceFightMusic();
			}
			st.CombatMusicPlaying = false;
		}
	}
}
