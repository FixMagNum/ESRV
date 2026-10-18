using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentShapeshifter : Component, IUpdateable
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemSky m_subsystemSky;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemAudio m_subsystemAudio;

	public ComponentSpawn m_componentSpawn;

	public ComponentBody m_componentBody;

	public ComponentHealth m_componentHealth;

	public ShapeshiftParticleSystem m_particleSystem;

	public string m_nightEntityTemplateName;

	public string m_dayEntityTemplateName;

	public float m_timeToSwitch;

	public string m_spawnEntityTemplateName;

	public static Random s_random;

	public SubsystemPlayers m_subsystemPlayers;

	public bool IsEnabled { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public void Update(float dt)
	{
		bool areSupernaturalCreaturesEnabled = m_subsystemGameInfo.WorldSettings.AreSupernaturalCreaturesEnabled;
		bool flag = false;
		foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
		{
			if (componentPlayer.PlayerData.IsCheater)
			{
				flag = true;
				break;
			}
		}
		if (IsEnabled && !m_componentSpawn.IsDespawning && m_componentHealth.Health > 0f && !flag)
		{
			bool isSolarEclipse = m_subsystemSky.IsSolarEclipse;
			if (!areSupernaturalCreaturesEnabled && !string.IsNullOrEmpty(m_dayEntityTemplateName))
			{
				ShapeshiftTo(m_dayEntityTemplateName);
			}
			else if (m_subsystemSky.SkyLightIntensity > 0.25f && !isSolarEclipse && !string.IsNullOrEmpty(m_dayEntityTemplateName))
			{
				m_timeToSwitch -= 2f * dt;
				if (m_timeToSwitch <= 0f)
				{
					ShapeshiftTo(m_dayEntityTemplateName);
				}
			}
			else if (areSupernaturalCreaturesEnabled && ((m_subsystemSky.SkyLightIntensity < 0.1f && (m_subsystemSky.MoonPhase == 0 || m_subsystemSky.MoonPhase == 4)) | isSolarEclipse) && !string.IsNullOrEmpty(m_nightEntityTemplateName))
			{
				m_timeToSwitch -= dt;
				if (m_timeToSwitch <= 0f)
				{
					ShapeshiftTo(m_nightEntityTemplateName);
				}
			}
		}
		if (!string.IsNullOrEmpty(m_spawnEntityTemplateName))
		{
			if (m_particleSystem == null)
			{
				m_particleSystem = new ShapeshiftParticleSystem();
				m_subsystemParticles.AddParticleSystem(m_particleSystem);
			}
			m_particleSystem.BoundingBox = m_componentBody.BoundingBox;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_componentSpawn = Entity.FindComponent<ComponentSpawn>(throwOnError: true);
		m_componentBody = Entity.FindComponent<ComponentBody>(throwOnError: true);
		m_componentHealth = Entity.FindComponent<ComponentHealth>(throwOnError: true);
		m_dayEntityTemplateName = valuesDictionary.GetValue<string>("DayEntityTemplateName");
		m_nightEntityTemplateName = valuesDictionary.GetValue<string>("NightEntityTemplateName");
		float value = valuesDictionary.GetValue<float>("Probability");
		if (!string.IsNullOrEmpty(m_dayEntityTemplateName))
		{
			DatabaseManager.FindEntityValuesDictionary(m_dayEntityTemplateName, throwIfNotFound: true);
		}
		if (!string.IsNullOrEmpty(m_nightEntityTemplateName))
		{
			DatabaseManager.FindEntityValuesDictionary(m_nightEntityTemplateName, throwIfNotFound: true);
		}
		m_timeToSwitch = s_random.Float(3f, 15f);
		IsEnabled = s_random.Float(0f, 1f) < value;
		m_componentSpawn.Despawned += ComponentSpawn_Despawned;
	}

	public void ShapeshiftTo(string entityTemplateName)
	{
		if (string.IsNullOrEmpty(m_spawnEntityTemplateName))
		{
			m_spawnEntityTemplateName = entityTemplateName;
			m_componentSpawn.DespawnDuration = 3f;
			m_componentSpawn.Despawn();
			m_subsystemAudio.PlaySound("Audio/Shapeshift", 1f, 0f, m_componentBody.Position, 3f, autoDelay: true);
		}
	}

	public void ComponentSpawn_Despawned(ComponentSpawn componentSpawn)
	{
		if (m_componentHealth.Health > 0f && !string.IsNullOrEmpty(m_spawnEntityTemplateName) && CommonLib.WorkType != WorkType.Client)
		{
			Entity entity = DatabaseManager.CreateEntity(Project, m_spawnEntityTemplateName, throwIfNotFound: true);
			ComponentBody componentBody = entity.FindComponent<ComponentBody>(throwOnError: true);
			componentBody.Position = m_componentBody.Position;
			componentBody.Rotation = m_componentBody.Rotation;
			componentBody.Velocity = m_componentBody.Velocity;
			entity.FindComponent<ComponentSpawn>(throwOnError: true).SpawnDuration = 0.5f;
			Project.AddEntity(entity);
		}
		if (m_particleSystem != null)
		{
			m_particleSystem.Stopped = true;
		}
	}

	public override void OnEntityRemoved()
	{
		if (m_particleSystem != null)
		{
			m_particleSystem.Stopped = true;
		}
	}

	static ComponentShapeshifter()
	{
		s_random = new Random();
	}
}
