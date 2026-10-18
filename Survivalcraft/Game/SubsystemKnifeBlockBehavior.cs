using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemKnifeBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemTerrain m_terrain;

	public SubsystemAudio m_audio;

	public SubsystemGameInfo m_gameInfo;

	public Random m_rand = new Random();

	public Dictionary<ComponentBody, Dictionary<int, double>> m_lastCutTimeByTarget = new Dictionary<ComponentBody, Dictionary<int, double>>();

	public SubsystemPickables m_subsystemPickables;

	public SubsystemParticles m_subsystemParticles;

	public override int[] HandledBlocks => new int[23]
	{
		79, 29, 325, 165, 406, 37, 114, 221, 124, 125,
		413, 414, 415, 0, 169, 219, 171, 172, 409, 113,
		36, 222, 407
	};

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_terrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_audio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_gameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
	}

	public override bool OnUse(Ray3 ray, ComponentMiner miner)
	{
		TerrainRaycastResult? terrainRaycastResult = miner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
		if (terrainRaycastResult.HasValue)
		{
			int x = terrainRaycastResult.Value.CellFace.X;
			int y = terrainRaycastResult.Value.CellFace.Y;
			int z = terrainRaycastResult.Value.CellFace.Z;
			int cellValue = m_terrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			if (num == 443)
			{
				if (miner.ActiveBlockValue == 0)
				{
					if (miner.ComponentCreature != null && miner.ComponentCreature.ComponentHealth != null)
					{
						miner.ComponentCreature.ComponentHealth.Injure(0.1f, null, ignoreInvulnerability: false, "Scratched by bark");
					}
				}
				else
				{
					miner.DamageActiveTool(1);
				}
				float num2 = m_rand.Float(0f, 1f);
				Vector3 position = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
				if (num2 <= 0.1f)
				{
					int value = Terrain.MakeBlockValue(198, 0, StringBlock.SetKind(0, StringBlock.StringKind.Bark));
					m_subsystemPickables.AddPickable(value, 1, position, null, null);
				}
				else if (num2 <= 0.8f)
				{
					int count = m_rand.Int(1, 2);
					m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(23), count, position, null, null);
				}
				m_audio.PlayRandomSound("Audio/Impacts/Wood", 0.5f, 0f, position, 3f, autoDelay: true);
				Block block = BlocksManager.Blocks[num];
				m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_terrain, position, cellValue, 0.5f));
				m_terrain.ChangeCell(x, y, z, 0);
				return true;
			}
		}
		bool flag = m_gameInfo.WorldSettings.GameMode != GameMode.Creative && m_gameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled;
		bool flag2 = m_gameInfo.WorldSettings.GameMode == GameMode.Embrace;
		BodyRaycastResult? bodyRaycastResult = miner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
		if (!bodyRaycastResult.HasValue)
		{
			return false;
		}
		ComponentBody componentBody = bodyRaycastResult.Value.ComponentBody;
		if (componentBody == null || componentBody.Entity == null)
		{
			return false;
		}
		ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
		if (componentCreature == null || componentCreature.ComponentHealth == null || componentCreature.ComponentHealth.Health > 0f)
		{
			return false;
		}
		ComponentCreatureModel componentCreatureModel = componentBody.Entity.FindComponent<ComponentCreatureModel>();
		if (componentCreatureModel == null)
		{
			return false;
		}
		int activeBlockValue = miner.ActiveBlockValue;
		int num3 = Terrain.ExtractContents(activeBlockValue);
		if (activeBlockValue == 0 && !flag2)
		{
			return false;
		}
		bool flag3 = flag2 && activeBlockValue == 0;
		if (flag3)
		{
			num3 = 413;
		}
		GetToolParams(num3, out var harvestPercent, out var cooldownSeconds);
		if (IsOnCooldown(componentBody, num3, cooldownSeconds))
		{
			return false;
		}
		if (!componentCreatureModel.CutNextDetachablePart())
		{
			return false;
		}
		PlayCutSound(num3, componentBody.Position);
		SetCooldown(componentBody, num3, cooldownSeconds);
		bool flag4 = true;
		if (componentCreature.ComponentHealth.DeathTime.HasValue && m_gameInfo.TotalElapsedGameTime - componentCreature.ComponentHealth.DeathTime.Value > 1000.0)
		{
			flag4 = false;
		}
		ComponentLoot componentLoot = componentBody.Entity.FindComponent<ComponentLoot>();
		if ((componentLoot != null) & flag4)
		{
			componentLoot.SetLootScaleOnce(harvestPercent);
			if (componentCreatureModel.IsFullyDismembered)
			{
				componentLoot.HarvestAllRemaining();
			}
			else
			{
				componentLoot.HarvestForCut(null);
			}
		}
		if (flag4)
		{
			int dropletsCount = m_rand.Int(3, 7);
			SubsystemParticles subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			Vector3 origin = componentBody.Position + new Vector3(0f, componentBody.BoxSize.Y * 0.5f, 0f);
			float num4 = m_rand.Float(-1f, 1f);
			float num5 = m_rand.Float(-1f, 1f);
			if (num4 * num4 + num5 * num5 < 0.0001f)
			{
				num4 = 1f;
				num5 = 0f;
			}
			subsystemParticles.AddParticleSystem(new BloodDropletParticleSystem(direction: Vector3.Normalize(new Vector3(num4, 0f, num5)), subsystemTerrain: m_terrain, origin: origin, dropletsCount: dropletsCount, spreadDegrees: 180f, minSpeed: 3f, maxSpeed: 6f, gravity: -9.81f));
		}
		if (flag && !flag3)
		{
			miner.DamageActiveTool(1);
		}
		return true;
	}

	private void GetToolParams(int contents, out float harvestPercent, out double cooldownSeconds)
	{
		switch (contents)
		{
		case 413:
		case 414:
		case 415:
			harvestPercent = 1f;
			cooldownSeconds = 0.1;
			break;
		case 124:
		case 125:
		case 221:
			harvestPercent = 0.7f;
			cooldownSeconds = 1.0;
			break;
		case 29:
		case 36:
		case 37:
		case 79:
		case 113:
		case 114:
		case 165:
		case 169:
		case 171:
		case 172:
		case 219:
		case 222:
		case 325:
		case 406:
		case 407:
		case 409:
			harvestPercent = 0.35f;
			cooldownSeconds = 2.0;
			break;
		default:
			harvestPercent = 1f;
			cooldownSeconds = 0.0;
			break;
		}
	}

	private bool IsOnCooldown(ComponentBody body, int toolContents, double cooldownSeconds)
	{
		if (cooldownSeconds <= 0.0)
		{
			return false;
		}
		double totalElapsedGameTime = m_gameInfo.TotalElapsedGameTime;
		if (m_lastCutTimeByTarget.TryGetValue(body, out var value) && value.TryGetValue(toolContents, out var value2))
		{
			return totalElapsedGameTime - value2 < cooldownSeconds;
		}
		return false;
	}

	private void SetCooldown(ComponentBody body, int toolContents, double cooldownSeconds)
	{
		if (!(cooldownSeconds <= 0.0))
		{
			double totalElapsedGameTime = m_gameInfo.TotalElapsedGameTime;
			if (!m_lastCutTimeByTarget.TryGetValue(body, out var value))
			{
				value = new Dictionary<int, double>();
				m_lastCutTimeByTarget[body] = value;
			}
			value[toolContents] = totalElapsedGameTime;
		}
	}

	private void PlayCutSound(int toolContents, Vector3 position)
	{
		string directory = ((toolContents == 413 || toolContents == 414 || toolContents == 415) ? "Audio/CuttingKnifes" : "Audio/CuttingAxes");
		m_audio.PlayRandomSound(directory, 1f, m_rand.Float(-0.2f, 0.2f), position, 4f, autoDelay: false);
	}
}
