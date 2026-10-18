using System;
using System.Globalization;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentMiner : Component, IUpdateable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemBodies m_subsystemBodies;

	public SubsystemMovingBlocks m_subsystemMovingBlocks;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemTime m_subsystemTime;

	public SubsystemAudio m_subsystemAudio;

	public SubsystemSoundMaterials m_subsystemSoundMaterials;

	public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

	public Random m_random = new Random();

	public static Random s_random = new Random();

	public double m_digStartTime;

	public float m_digProgress;

	public double m_lastHitTime;

	public static string fName = "ComponentMiner";

	public int m_lastDigFrameIndex;

	public float m_lastPokingPhase;

	public bool DigFaceChange;

	private SubsystemPlayers m_subsystemPlayers;

	private double m_lastBarehandedDamageTime;

	public ComponentCreature ComponentCreature { get; set; }

	public ComponentPlayer ComponentPlayer { get; set; }

	public IInventory Inventory { get; set; }

	public int ActiveBlockValue
	{
		get
		{
			if (Inventory == null)
			{
				return 0;
			}
			return Inventory.GetSlotValue(Inventory.ActiveSlotIndex);
		}
	}

	public float AttackPower { get; set; }

	public float PokingPhase { get; set; }

	public CellFace? DigCellFace { get; set; }

	public float DigTime
	{
		get
		{
			if (!DigCellFace.HasValue)
			{
				return 0f;
			}
			return (float)(m_subsystemTime.GameTime - m_digStartTime);
		}
	}

	public float DigProgress
	{
		get
		{
			if (!DigCellFace.HasValue)
			{
				return 0f;
			}
			return m_digProgress;
		}
	}

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public float KnockbackPower { get; set; }

	public float VerticalKnockbackPower { get; set; }

	public float DigCracksProgress { get; set; }

	public int? SecondaryActiveSlotIndex
	{
		get
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Embrace || Inventory == null)
			{
				return null;
			}
			return (Inventory.ActiveSlotIndex == 0) ? 1 : 0;
		}
	}

	public int SecondaryBlockValue
	{
		get
		{
			int? secondaryActiveSlotIndex = SecondaryActiveSlotIndex;
			if (!secondaryActiveSlotIndex.HasValue)
			{
				return 0;
			}
			return Inventory.GetSlotValue(secondaryActiveSlotIndex.Value);
		}
	}

	public int PokingCounter { get; set; }

	public void Poke(bool forceRestart)
	{
		if (forceRestart)
		{
			PokingPhase = 0.0001f;
		}
		else if (Terrain.ExtractContents(ActiveBlockValue) == 367)
		{
			PokingPhase = 1E-05f;
		}
		else
		{
			PokingPhase = MathUtils.Max(0.0001f, PokingPhase);
		}
	}

	public bool Dig(TerrainRaycastResult raycastResult, bool isEnd = false)
	{
		bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(raycastResult.CellFace.X, raycastResult.CellFace.Z, out var face2);
		if (!ComponentPlayer.PlayerData.ServerManager & flag)
		{
			bool flag2 = ComponentPlayer.PlayerData.Friends.Contains(face2.OwnerGuid);
			if (((flag2 && !face2.ApplyToFirend) || !flag2) && face2.OwnerGuid != ComponentPlayer.PlayerData.PlayerGUID && !face2.AllowDig)
			{
				if (Time.PeriodicEvent(1.0, 0.0))
				{
					string text = LanguageControl.Get("ComponentMiner", "no_permission_dig");
					ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Yellow, blinking: false, playNotificationSound: false);
				}
				return false;
			}
		}
		if (m_subsystemGameInfo.WorldSettings.RestrictWorldInteraction && !ComponentPlayer.PlayerData.ServerManager && !flag)
		{
			if (Time.PeriodicEvent(1.0, 0.0))
			{
				string text2 = LanguageControl.Get("ComponentMiner", "no_permission_dig");
				ComponentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.Yellow, blinking: false, playNotificationSound: false);
			}
			return false;
		}
		bool result = false;
		m_lastDigFrameIndex = Time.FrameIndex;
		CellFace cellFace = raycastResult.CellFace;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
		int num = Terrain.ExtractContents(cellValue);
		if (num == 0)
		{
			return false;
		}
		Block block = BlocksManager.Blocks[num];
		int activeBlockValue = ActiveBlockValue;
		int num2 = Terrain.ExtractContents(activeBlockValue);
		Block block2 = BlocksManager.Blocks[num2];
		if (SettingsManager.BuildSpamEnabled && block2.IsPlaceable && ComponentPlayer.ComponentInput.IsControlledByTouch && ComponentPlayer.PlayerData.IsMainPlayer)
		{
			return false;
		}
		int num3 = (SecondaryActiveSlotIndex.HasValue ? SecondaryBlockValue : 0);
		int num4 = Terrain.ExtractContents(num3);
		Block block3 = BlocksManager.Blocks[num4];
		bool flag3 = CanUseTool(activeBlockValue);
		bool flag4 = SecondaryActiveSlotIndex.HasValue && CanUseTool(num3);
		int num5 = MathUtils.Max(flag3 ? block2.ToolLevel : 0, flag4 ? block3.ToolLevel : 0);
		bool flag5 = num == 209 && Terrain.ExtractData(cellValue) > 0;
		if (activeBlockValue == 0 && (((num == 92 || num == 127 || num == 375 || num == 354) | flag5) || num == 104 || num == 378 || num == 226 || num == 65 || num == 304))
		{
			double gameTime = m_subsystemTime.GameTime;
			if (gameTime - m_lastBarehandedDamageTime >= 0.3)
			{
				ComponentPlayer.ComponentHealth?.Injure(0.05f, ComponentCreature, ignoreInvulnerability: false, "Hazardous block mining");
				m_lastBarehandedDamageTime = gameTime;
			}
		}
		if (!DigCellFace.HasValue || DigCellFace.Value.X != cellFace.X || DigCellFace.Value.Y != cellFace.Y || DigCellFace.Value.Z != cellFace.Z)
		{
			m_digStartTime = m_subsystemTime.GameTime;
			DigCellFace = cellFace;
			DigFaceChange = true;
			DigCracksProgress = 0f;
		}
		float num6 = CalculateDigTime(cellValue, num2);
		m_digProgress = ((num6 > 0f) ? MathUtils.Saturate((float)(m_subsystemTime.GameTime - m_digStartTime) / num6) : 1f);
		if (m_digProgress > 0f && m_digProgress < 1f && ComponentCreature.ComponentBody.Size > 1.5f && Time.PeriodicEvent(0.5, 0.0))
		{
			SubsystemParticles subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			SubsystemWeather weather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			subsystemParticles?.AddParticleSystem(new CaveDustBurstParticleSystem(spawnInsideBlock: new Vector3((float)raycastResult.CellFace.X + 0.5f, (float)raycastResult.CellFace.Y + 0.5f, (float)raycastResult.CellFace.Z + 0.5f), terrain: m_subsystemTerrain, weather: weather));
		}
		if (!flag3 && !flag4)
		{
			m_digProgress = 0f;
			if (m_subsystemTime.PeriodicGameTimeEvent(5.0, m_digStartTime + 1.0))
			{
				ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), block2.PlayerLevelRequired, block2.GetDisplayName(m_subsystemTerrain, activeBlockValue)), Color.White, blinking: true, playNotificationSound: true);
			}
		}
		bool num7 = ComponentPlayer != null && !ComponentPlayer.ComponentInput.IsControlledByTouch && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
		bool flag6 = m_lastPokingPhase <= 0.5f && PokingPhase > 0.5f;
		bool flag7 = (num7 | flag6) || m_digProgress >= 1f;
		if (!ComponentPlayer.PlayerData.IsMainPlayer)
		{
			flag7 = isEnd;
			m_digProgress += 0.2f;
		}
		Point3 blockPosition = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		if (isEnd)
		{
			m_subsystemPlayers.StopMiningBlock(this, blockPosition);
		}
		else
		{
			m_subsystemPlayers.StartMiningBlock(this, blockPosition);
		}
		bool flag8 = IsRake(activeBlockValue) || (SecondaryActiveSlotIndex.HasValue && IsRake(num3));
		int data = Terrain.ExtractData(cellValue);
		if ((((num == 174 && RyeBlock.GetSize(data) == 7 && !RyeBlock.GetIsWild(data)) || (num == 204 && CottonBlock.GetSize(data) == 2 && !CottonBlock.GetIsWild(data)) || (num == 131 && BasePumpkinBlock.GetSize(data) == 7)) & flag8 & isEnd) && m_digProgress >= 1f)
		{
			m_subsystemAudio.PlayRandomSound("Audio/RakeBuff", 1f, m_random.Float(-0.2f, 0.2f), new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 4f, autoDelay: false);
		}
		if (flag7)
		{
			if (m_digProgress >= 1f)
			{
				DigCracksProgress = 0f;
				DigCellFace = null;
				Poke(forceRestart: true);
				if (num2 == 367)
				{
					Destroy3x3Around(cellFace);
					ComponentVitalStats componentVitalStats = ComponentPlayer.ComponentVitalStats;
					if (componentVitalStats != null)
					{
						componentVitalStats.Stamina = MathUtils.Max(componentVitalStats.Stamina - 0.5f, 0f);
					}
				}
				else
				{
					BlockPlacementData digValue = block.GetDigValue(m_subsystemTerrain, this, cellValue, activeBlockValue, raycastResult);
					SubsystemCaveInstability subsystemCaveInstability = Project.FindSubsystem<SubsystemCaveInstability>(throwOnError: true);
					if (subsystemCaveInstability != null && Array.IndexOf(subsystemCaveInstability.HandledBlocks, num) >= 0)
					{
						subsystemCaveInstability.MarkPlayerBreak(new Point3(digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z));
					}
					m_subsystemTerrain.DestroyCell(num5, digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z, digValue.Value, noDrop: false, noParticleSystem: false, this);
				}
				m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f);
				if (block.IsCollidable)
				{
					DamageActiveTool(1);
					if (SecondaryActiveSlotIndex.HasValue && SecondaryBlockValue != 0)
					{
						int activeSlotIndex = Inventory.ActiveSlotIndex;
						Inventory.ActiveSlotIndex = SecondaryActiveSlotIndex.Value;
						DamageActiveTool(1);
						Inventory.ActiveSlotIndex = activeSlotIndex;
					}
				}
				BlockDebrisParticleSystem particleSystem = block.CreateDebrisParticleSystem(m_subsystemTerrain, raycastResult.HitPoint(0.1f), cellValue, 0.7f);
				Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
				if (ComponentCreature.PlayerAchievements != null)
				{
					if (num == 10 || num == 255 || num == 9 || num == 11)
					{
						PlayerAchievements playerAchievements = ComponentCreature.PlayerAchievements;
						playerAchievements.TreesChopping++;
						if (playerAchievements.TreesChopping == 1)
						{
							playerAchievements.EnqueueTreeChoppingAchievement();
						}
					}
					if (num == 112 && num5 <= 2)
					{
						PlayerAchievements playerAchievements2 = ComponentCreature.PlayerAchievements;
						if (playerAchievements2.HatedDay == 0L)
						{
							playerAchievements2.HatedDay = 1L;
							playerAchievements2.EnqueueHatedDayAchievement();
						}
					}
				}
				if (ComponentCreature.PlayerStats != null)
				{
					ComponentCreature.PlayerStats.BlocksDug++;
				}
				result = true;
			}
			else if (m_lastPokingPhase <= 0.5f && PokingPhase > 0.5f)
			{
				PokingCounter++;
				m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 1f);
				BlockDebrisParticleSystem particleSystem2 = block.CreateDebrisParticleSystem(m_subsystemTerrain, raycastResult.HitPoint(0.1f), cellValue, 0.5f);
				Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem2);
				float num8 = (float)Math.Round(MathUtils.Max(1f, num6 * 1.5f));
				DigCracksProgress += 1f / num8;
				DigCracksProgress = MathUtils.Clamp(DigCracksProgress, 0.01f, 0.95f);
			}
		}
		return result;
	}

	public bool Place(TerrainRaycastResult raycastResult)
	{
		if (ComponentPlayer.ComponentVitalStats.Stamina < 0.15f && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative)
		{
			string text = LanguageControl.Get("ComponentVitalStats", "null_stamina");
			ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.White, blinking: true, playNotificationSound: true);
			return false;
		}
		bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(raycastResult.CellFace.X, raycastResult.CellFace.Z, out var territoriy);
		if (!ComponentPlayer.PlayerData.ServerManager && territoriy != null)
		{
			bool flag2 = ComponentPlayer.PlayerData.Friends.Contains(territoriy.OwnerGuid);
			if (((flag2 && !territoriy.ApplyToFirend) || !flag2) && territoriy.OwnerGuid != ComponentPlayer.PlayerData.PlayerGUID)
			{
				if (!territoriy.AllowBlockBehavior)
				{
					if (Time.PeriodicEvent(1.0, 0.0))
					{
						string text2 = LanguageControl.Get("ComponentMiner", "no_block_behavior_permission");
						ComponentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.Yellow, blinking: false, playNotificationSound: false);
					}
				}
				else if (!territoriy.AllowPlace && Time.PeriodicEvent(1.0, 0.0))
				{
					string text3 = LanguageControl.Get("ComponentMiner", "no_place_permission");
					ComponentPlayer.ComponentGui.DisplaySmallMessage(text3, Color.Yellow, blinking: false, playNotificationSound: false);
				}
				return false;
			}
		}
		int activeBlockValue = ActiveBlockValue;
		int num = Terrain.ExtractContents(activeBlockValue);
		int num2 = Terrain.ExtractData(activeBlockValue);
		bool flag3 = num == 1 && num2 == 1;
		if (m_subsystemGameInfo.WorldSettings.RestrictWorldInteraction && !ComponentPlayer.PlayerData.ServerManager && !flag3 && !flag)
		{
			if (Time.PeriodicEvent(1.0, 0.0))
			{
				string text4 = LanguageControl.Get("ComponentMiner", "no_place_permission");
				ComponentPlayer.ComponentGui.DisplaySmallMessage(text4, Color.Yellow, blinking: false, playNotificationSound: false);
			}
			return false;
		}
		int slotIndex = Inventory.ActiveSlotIndex;
		int value = ActiveBlockValue;
		Block obj = BlocksManager.Blocks[Terrain.ExtractContents(value)];
		int? secondaryActiveSlotIndex = SecondaryActiveSlotIndex;
		int secondaryBlockValue = SecondaryBlockValue;
		Block block = (secondaryActiveSlotIndex.HasValue ? BlocksManager.Blocks[Terrain.ExtractContents(secondaryBlockValue)] : null);
		if (!obj.IsPlaceable && secondaryActiveSlotIndex.HasValue && block != null && block.IsPlaceable)
		{
			slotIndex = secondaryActiveSlotIndex.Value;
			value = secondaryBlockValue;
		}
		if (SubsystemMagnetBlockBehavior.PlayerMagnets.ContainsKey(ComponentPlayer.PlayerData.PlayerGUID) && Terrain.ExtractContents(value) == 167)
		{
			string text5 = LanguageControl.Get("SubsystemMagnetBlockBehavior", "single_block_work");
			ComponentPlayer.ComponentGui.DisplaySmallMessage(text5, Color.Red, blinking: false, playNotificationSound: true);
			return false;
		}
		if (Place(raycastResult, value))
		{
			if (Inventory != null && CommonLib.WorkType != WorkType.Client)
			{
				Inventory.RemoveSlotItems(slotIndex, 1);
			}
			return true;
		}
		return false;
	}

	public bool Place(TerrainRaycastResult raycastResult, int value)
	{
		int num = Terrain.ExtractContents(value);
		if (m_subsystemGameInfo.WorldSettings.IsBlockDiable(value))
		{
			string text = LanguageControl.Get("ComponentMiner", "item_disabled");
			ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.Red, blinking: false, playNotificationSound: true);
			return false;
		}
		if (num == 1 && Terrain.ExtractData(value) == 1 && ComponentPlayer != null && !ComponentPlayer.PlayerData.ServerManager)
		{
			int regionPrivateLimit = m_subsystemGameInfo.WorldSettings.RegionPrivateLimit;
			if (regionPrivateLimit == 0)
			{
				string text2 = LanguageControl.Get("ComponentMiner", "region_null_blocks");
				ComponentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.Red, blinking: false, playNotificationSound: true);
				return false;
			}
			if (regionPrivateLimit > 0 && SubsystemBedrockBlockBehavior.Territories.TryGetValue(ComponentPlayer.PlayerData.PlayerGUID, out var value2) && (value2?.Count ?? 0) >= regionPrivateLimit)
			{
				string text3 = string.Format(LanguageControl.Get("ComponentMiner", "region_limite"), regionPrivateLimit);
				ComponentPlayer.ComponentGui.DisplaySmallMessage(text3, Color.Red, blinking: false, playNotificationSound: true);
				return false;
			}
		}
		if (BlocksManager.Blocks[num].IsPlaceable)
		{
			Block block = BlocksManager.Blocks[num];
			BlockPlacementData placementData = block.GetPlacementValue(m_subsystemTerrain, this, value, raycastResult);
			if (placementData.Value != 0)
			{
				Point3 point = CellFace.FaceToPoint3(placementData.CellFace.Face);
				int num2 = placementData.CellFace.X + point.X;
				int num3 = placementData.CellFace.Y + point.Y;
				int num4 = placementData.CellFace.Z + point.Z;
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(num2, num3, num4);
				if (cellValue == placementData.Value)
				{
					return false;
				}
				int num5 = Terrain.ExtractContents(cellValue);
				if (num5 != 0)
				{
					bool num6 = num5 == 18 || num5 == 19 || num5 == 92 || num5 == 93;
					Block block2 = BlocksManager.Blocks[num5];
					bool flag = block2 is SlabBlock || block2 is StairsBlock || block2 is FurnitureBlock || block2 is FurnitureBlock2;
					if (!num6 && !flag && block2.GetDigResilience(cellValue) > 25f)
					{
						return false;
					}
				}
				bool flag2 = false;
				if (block.IsCollidable)
				{
					BoundingBox boundingBox = ComponentCreature.ComponentBody.BoundingBox;
					boundingBox.Min += new Vector3(0.2f);
					boundingBox.Max -= new Vector3(0.2f);
					BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, placementData.Value);
					for (int i = 0; i < customCollisionBoxes.Length; i++)
					{
						BoundingBox box = customCollisionBoxes[i];
						box.Min += new Vector3(num2, num3, num4);
						box.Max += new Vector3(num2, num3, num4);
						if (boundingBox.Intersection(box))
						{
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(placementData.Value), this, new Point3(num2, num3, num4));
					for (int j = 0; j < blockBehaviors.Length; j++)
					{
						blockBehaviors[j].OnBlockPlaced(this, num2, num3, num4, ref placementData, value);
					}
					m_subsystemTerrain.DestroyCell(0, num2, num3, num4, placementData.Value, noDrop: false, noParticleSystem: false, this);
					int contents = Terrain.ExtractContents(placementData.Value);
					m_subsystemSoundMaterials.PlayImpactSound(Terrain.MakeBlockValue(contents), new Vector3(placementData.CellFace.X, placementData.CellFace.Y, placementData.CellFace.Z), 1f, -0.3f);
					Poke(forceRestart: false);
					if (ComponentCreature.PlayerStats != null)
					{
						ComponentCreature.PlayerStats.BlocksPlaced++;
					}
					return true;
				}
			}
		}
		return false;
	}

	public bool Use(Ray3 ray)
	{
		for (ComponentBody componentBody = (ComponentCreature?.ComponentBody)?.ParentBody; componentBody != null; componentBody = componentBody.ParentBody)
		{
			ComponentShipModel componentShipModel = componentBody.Entity?.FindComponent<ComponentShipModel>();
			if (componentShipModel != null)
			{
				if (ActiveBlockValue != 0)
				{
					break;
				}
				ComponentShipModel.SailMode sailMode = componentShipModel.GetSailMode();
				componentShipModel.CycleSail();
				ComponentShipModel.SailMode sailMode2 = componentShipModel.GetSailMode();
				Poke(forceRestart: false);
				if (sailMode2 != sailMode)
				{
					m_subsystemAudio?.PlayRandomSound("Audio/SailBoat", 1f, m_random.Float(-0.2f, 0.2f), componentBody.Position, 3f, autoDelay: false);
				}
				return true;
			}
		}
		float size = ComponentCreature.ComponentBody.Size;
		float num = ((size > 1f) ? size : 1f);
		float num2 = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? SettingsManager.CreativeReach : 5f);
		float reach = num2 * num;
		Vector3 creaturePosition = ComponentCreature.ComponentCreatureModel.EyePosition;
		Vector3 start = ray.Position;
		Vector3 direction = Vector3.Normalize(ray.Direction);
		Vector3 end = ray.Position + direction * MathUtils.Max(15f, reach + 2f);
		float inflateAmount = 0.35f * num;
		BodyRaycastResult? bodyRaycastResult = m_subsystemBodies.Raycast(start, end, inflateAmount, (ComponentBody body, float distance) => Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach && body.Entity != Entity && !body.IsChildOfBody(ComponentCreature.ComponentBody) && !ComponentCreature.ComponentBody.IsChildOfBody(body) && Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f);
		MovingBlocksRaycastResult? movingBlocksRaycastResult = m_subsystemMovingBlocks.Raycast(start, end, extendToFillCells: true);
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(start, end, useInteractionBoxes: true, skipAirBlocks: true, (int value, float distance) =>
		{
			if (Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach)
			{
				Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(value)];
				if (block2.IsPlacementTransparent)
				{
					return block2.IsInteractive(m_subsystemTerrain, value);
				}
				return true;
			}
			return false;
		});
		float num3 = (bodyRaycastResult.HasValue ? bodyRaycastResult.Value.Distance : float.PositiveInfinity);
		float num4 = (movingBlocksRaycastResult.HasValue ? movingBlocksRaycastResult.Value.Distance : float.PositiveInfinity);
		float num5 = (terrainRaycastResult.HasValue ? terrainRaycastResult.Value.Distance : float.PositiveInfinity);
		if (num4 < num3 && num4 < num5 && num4 < 100f)
		{
			if (num4 * num4 <= reach * reach && Interact(movingBlocksRaycastResult.Value))
			{
				Poke(forceRestart: false);
				return true;
			}
		}
		else if (num3 < num4 && num3 < num5)
		{
			for (ComponentBody componentBody2 = bodyRaycastResult.Value.ComponentBody; componentBody2 != null; componentBody2 = componentBody2.ParentBody)
			{
				Entity entity = componentBody2.Entity;
				if (entity != null)
				{
					ComponentShipModel componentShipModel2 = entity.FindComponent<ComponentShipModel>();
					if (componentShipModel2 != null && ActiveBlockValue == 0)
					{
						ComponentShipModel.SailMode sailMode3 = componentShipModel2.GetSailMode();
						componentShipModel2.CycleSail();
						if (componentShipModel2.GetSailMode() != sailMode3 && m_subsystemAudio != null)
						{
							m_subsystemAudio.PlayRandomSound("Audio/SailBoat", 1f, m_random.Float(-0.2f, 0.2f), componentBody2.Position, 3f, autoDelay: false);
						}
						Poke(forceRestart: false);
						return true;
					}
				}
			}
		}
		else if (num5 < num4 && num5 < num3 && Interact(terrainRaycastResult.Value))
		{
			return true;
		}
		int activeSlotIndex = ((Inventory != null) ? Inventory.ActiveSlotIndex : (-1));
		int? secondaryActiveSlotIndex = SecondaryActiveSlotIndex;
		int num6 = ((!secondaryActiveSlotIndex.HasValue || SecondaryBlockValue == 0) ? 1 : 2);
		bool flag = false;
		Point3? point = ((num5 < num4 && num5 < num3 && num5 < reach) ? new Point3?(Terrain.ToCell(terrainRaycastResult.Value.Ray.Position + terrainRaycastResult.Value.Ray.Direction * num5)) : ((Point3?)null));
		for (int num7 = 0; num7 < num6; num7++)
		{
			if (num7 == 1)
			{
				Inventory.ActiveSlotIndex = secondaryActiveSlotIndex.Value;
			}
			if (!CanUseTool(ActiveBlockValue))
			{
				int num8 = Terrain.ExtractContents(ActiveBlockValue);
				Block block = BlocksManager.Blocks[num8];
				ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), block.PlayerLevelRequired, block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)), Color.White, blinking: true, playNotificationSound: true);
				Poke(forceRestart: false);
				if (num7 == 0 && num6 == 1)
				{
					return false;
				}
				continue;
			}
			int contents = Terrain.ExtractContents(ActiveBlockValue);
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents, this, point);
			for (int num9 = 0; num9 < blockBehaviors.Length; num9++)
			{
				if (blockBehaviors[num9].OnUse(ray, this))
				{
					Poke(forceRestart: false);
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (Inventory != null)
		{
			Inventory.ActiveSlotIndex = activeSlotIndex;
		}
		return flag;
	}

	public bool Interact(TerrainRaycastResult raycastResult)
	{
		int cellContents = m_subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(cellContents, this, raycastResult.CellFace.Point);
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i].OnInteract(raycastResult, this))
			{
				if (ComponentCreature.PlayerStats != null)
				{
					ComponentCreature.PlayerStats.BlocksInteracted++;
				}
				Poke(forceRestart: false);
				return true;
			}
		}
		return false;
	}

	public void Hit(ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection)
	{
		int cellFace = Terrain.ExtractContents(ActiveBlockValue);
		float data = ((cellFace == 367) ? 1.98f : 0.66f);
		if (cellFace == 413 || cellFace == 414 || cellFace == 415)
		{
			data *= 0.5f;
		}
		if (m_subsystemTime.GameTime - m_lastHitTime <= (double)data)
		{
			return;
		}
		m_lastHitTime = m_subsystemTime.GameTime;
		int mountingFace = Terrain.ExtractContents(ActiveBlockValue);
		Block vector = BlocksManager.Blocks[mountingFace];
		m_subsystemAudio.PlayRandomSound("Audio/Attack", 1f, m_random.Float(-0.2f, 0.2f), componentBody.Position, 3f, autoDelay: false);
		if (mountingFace == 367)
		{
			SplashAttack(hitPoint, hitDirection);
			m_subsystemAudio.PlaySound("Audio/SplashHit", 1f, m_random.Float(0.1f, 0.4f), componentBody.Position, 5f, autoDelay: false);
			ComponentVitalStats componentVitalStats = ComponentPlayer.ComponentVitalStats;
			if (componentVitalStats != null)
			{
				componentVitalStats.Stamina = MathUtils.Max(componentVitalStats.Stamina - 0.3f, 0f);
			}
			return;
		}
		if (!CanUseTool(ActiveBlockValue))
		{
			ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), vector.PlayerLevelRequired, vector.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)), Color.White, blinking: true, playNotificationSound: true);
			Poke(forceRestart: false);
			return;
		}
		float vector2 = 0.66f;
		float vector3;
		if (ActiveBlockValue != 0)
		{
			vector3 = vector.GetMeleePower(ActiveBlockValue);
			vector2 = vector.GetMeleeHitProbability(ActiveBlockValue);
		}
		else
		{
			vector3 = 1f;
		}
		float right = 0f;
		int? secondaryActiveSlotIndex = SecondaryActiveSlotIndex;
		int secondaryBlockValue = SecondaryBlockValue;
		if (secondaryActiveSlotIndex.HasValue && secondaryBlockValue != 0)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(secondaryBlockValue)];
			if (block.GetMeleePower(secondaryBlockValue) > 1f)
			{
				right = block.GetMeleePower(secondaryBlockValue);
				vector2 = MathUtils.Min(vector2, block.GetMeleeHitProbability(secondaryBlockValue));
			}
		}
		float num = (vector3 + right) * AttackPower * m_random.Float(0.8f, 1.2f);
		float probability = vector2;
		bool flag = IsTargetSleeping(componentBody.Entity);
		if (flag)
		{
			probability = 1f;
			num *= 2f;
		}
		bool flag2;
		if (ComponentPlayer != null)
		{
			flag2 = flag || m_random.Bool(probability);
			num *= ComponentPlayer.ComponentLevel.StrengthFactor;
		}
		else
		{
			flag2 = true;
		}
		ComponentAlcohol componentAlcohol = componentBody.Entity.FindComponent<ComponentAlcohol>();
		if (!flag && componentAlcohol != null && componentAlcohol.Alcohol >= 0.5f)
		{
			float num2 = MathUtils.Floor(componentAlcohol.Alcohol / 0.5f);
			float probability2 = MathUtils.Clamp(0.05f * num2, 0f, 0.95f);
			if (m_random.Bool(probability2))
			{
				flag2 = false;
			}
		}
		if (!flag)
		{
			SubsystemGameInfo subsystemGameInfo = m_subsystemGameInfo;
			if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				ComponentAdrenaline componentAdrenaline = componentBody.Entity.FindComponent<ComponentAdrenaline>();
				if (componentAdrenaline != null && componentAdrenaline.Adrenaline > 0f)
				{
					float probability3 = MathUtils.Clamp(0.3f * componentAdrenaline.Adrenaline, 0f, 0.3f);
					if (m_random.Bool(probability3))
					{
						flag2 = false;
					}
				}
			}
		}
		if (flag2)
		{
			AttackBody(componentBody, ComponentCreature, hitPoint, hitDirection, num, isMeleeAttack: true);
			DamageActiveTool(1);
			if (right > 0f && SecondaryActiveSlotIndex.HasValue)
			{
				int activeSlotIndex = Inventory.ActiveSlotIndex;
				Inventory.ActiveSlotIndex = SecondaryActiveSlotIndex.Value;
				DamageActiveTool(1);
				Inventory.ActiveSlotIndex = activeSlotIndex;
			}
			if (flag)
			{
				m_subsystemAudio.PlayRandomSound("Audio/CuttingAxes", 1f, m_random.Float(-0.15f, 0.15f), componentBody.Position, 3f, autoDelay: false);
			}
		}
		else if (ComponentCreature is ComponentPlayer)
		{
			Vector3 position = hitPoint + 0.75f * hitDirection;
			Vector3 velocity = 1f * hitDirection + ComponentCreature.ComponentBody.Velocity;
			string text = LanguageControl.Get(fName, 2);
			if (CommonLib.WorkType != WorkType.Client)
			{
				CommonLib.Net.QueuePackage(new ComponentHealthPackage(position, velocity, Color.White, text));
				HitValueParticleSystem particleSystem = new HitValueParticleSystem(position, velocity, Color.White, text);
				Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
			}
		}
		if (ComponentCreature.PlayerStats != null)
		{
			ComponentCreature.PlayerStats.MeleeAttacks++;
			if (flag2)
			{
				ComponentCreature.PlayerStats.MeleeHits++;
			}
		}
		Poke(forceRestart: false);
	}

	public bool Aim(Ray3 aim, AimState state)
	{
		int num = Terrain.ExtractContents(ActiveBlockValue);
		Block block = BlocksManager.Blocks[num];
		if (block.IsAimable)
		{
			if (!CanUseTool(ActiveBlockValue))
			{
				ComponentPlayer?.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get(fName, 1), block.PlayerLevelRequired, block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)), Color.White, blinking: true, playNotificationSound: true);
				Poke(forceRestart: false);
				return true;
			}
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num, this, Terrain.ToCell(aim.Position));
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				if (blockBehaviors[i].OnAim(aim, this, state))
				{
					return true;
				}
			}
		}
		return false;
	}

	public object Raycast(Ray3 ray, RaycastMode mode, bool raycastTerrain = true, bool raycastBodies = true, bool raycastMovingBlocks = true)
	{
		float size = ComponentCreature.ComponentBody.Size;
		float connection = ((size > 1f) ? size : 1f);
		float num = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? SettingsManager.CreativeReach : 5f);
		float reach = num * connection;
		Vector3 creaturePosition = ComponentCreature.ComponentCreatureModel.EyePosition;
		Vector3 start = ray.Position;
		Vector3 direction = Vector3.Normalize(ray.Direction);
		Vector3 end = ray.Position + direction * MathUtils.Max(15f, reach + 2f);
		Point3 startCell = Terrain.ToCell(start);
		float inflateAmount = 0.35f * connection;
		BodyRaycastResult? bodyRaycastResult = m_subsystemBodies.Raycast(start, end, inflateAmount, (ComponentBody body, float distance) => Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach && body.Entity != Entity && !body.IsChildOfBody(ComponentCreature.ComponentBody) && !ComponentCreature.ComponentBody.IsChildOfBody(body) && Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f);
		MovingBlocksRaycastResult? movingBlocksRaycastResult = m_subsystemMovingBlocks.Raycast(start, end, extendToFillCells: true);
		TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(start, end, useInteractionBoxes: true, skipAirBlocks: true, (int value, float distance) =>
		{
			if (Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach)
			{
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
				if (block is IvyBlock ivyBlock && ivyBlock.IsInvisible(m_subsystemTerrain, value))
				{
					return false;
				}
				if (block is LeavesBlock leavesBlock && leavesBlock.IsInvisible(m_subsystemTerrain, value))
				{
					return false;
				}
				if (distance == 0f && block is CrossBlock && Vector3.Dot(direction, new Vector3(startCell) + new Vector3(0.5f) - start) < 0f)
				{
					return false;
				}
				if (mode == RaycastMode.Digging)
				{
					return !block.IsDiggingTransparent;
				}
				if (mode == RaycastMode.Interaction)
				{
					if (block.IsPlacementTransparent)
					{
						return block.IsInteractive(m_subsystemTerrain, value);
					}
					return true;
				}
				if (mode == RaycastMode.Gathering)
				{
					return block.IsGatherable;
				}
			}
			return false;
		});
		float num2 = (bodyRaycastResult.HasValue ? bodyRaycastResult.Value.Distance : float.PositiveInfinity);
		float num3 = (movingBlocksRaycastResult.HasValue ? movingBlocksRaycastResult.Value.Distance : float.PositiveInfinity);
		float num4 = (terrainRaycastResult.HasValue ? terrainRaycastResult.Value.Distance : float.PositiveInfinity);
		if (num2 < num3 && num2 < num4)
		{
			return bodyRaycastResult.Value;
		}
		if (num3 < num2 && num3 < num4)
		{
			return movingBlocksRaycastResult.Value;
		}
		if (num4 < num2 && num4 < num3)
		{
			return terrainRaycastResult.Value;
		}
		return new Ray3(start, direction);
	}

	public T? Raycast<T>(Ray3 ray, RaycastMode mode, bool raycastTerrain = true, bool raycastBodies = true, bool raycastMovingBlocks = true) where T : struct
	{
		object obj = Raycast(ray, mode, raycastTerrain, raycastBodies, raycastMovingBlocks);
		if (!(obj is T))
		{
			return null;
		}
		return (T)obj;
	}

	public void RemoveActiveTool(int removeCount)
	{
		if (Inventory != null && CommonLib.WorkType != WorkType.Client)
		{
			Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, removeCount);
		}
	}

	public void DamageActiveTool(int damageCount)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || Inventory == null || CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		float itemDurabilityMultiplier = m_subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
		int num = 0;
		if (itemDurabilityMultiplier > 0f)
		{
			float num2 = (float)damageCount * itemDurabilityMultiplier;
			int num3 = (int)num2;
			float num4 = num2 - (float)num3;
			num = num3 + ((m_random.Float(0f, 1f) < num4) ? 1 : 0);
		}
		if (num == 0)
		{
			return;
		}
		int num5 = BlocksManager.DamageItem(ActiveBlockValue, num);
		if (num5 != 0)
		{
			int slotCount = Inventory.GetSlotCount(Inventory.ActiveSlotIndex);
			Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, slotCount);
			if (Inventory.GetSlotCount(Inventory.ActiveSlotIndex) == 0)
			{
				Inventory.AddSlotItems(Inventory.ActiveSlotIndex, num5, slotCount);
			}
		}
		else
		{
			Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
		}
	}

	public static void AttackBody(ComponentBody target, ComponentCreature attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower, bool isMeleeAttack)
	{
		if (attacker != null && attacker.Entity == null)
		{
			attacker = null;
		}
		if (attacker != null && attacker is ComponentPlayer && target.Player != null && !target.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.IsFriendlyFireEnabled)
		{
			attacker.Entity.FindComponent<ComponentGui>(throwOnError: true).DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, blinking: true, playNotificationSound: true);
			return;
		}
		if (attacker != null && attacker is ComponentPlayer && target.Player != null)
		{
			attacker.Entity.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			Point3 point = Terrain.ToCell(target.Position);
			if (SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(point.X, point.Z, out var territoriy) && territoriy.BlockPvp)
			{
				attacker.Entity.FindComponent<ComponentGui>(throwOnError: true).DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, blinking: true, playNotificationSound: true);
				return;
			}
		}
		if (attacker != null)
		{
			ComponentMiner componentMiner = attacker.Entity.FindComponent<ComponentMiner>();
			if (componentMiner != null && Terrain.ExtractContents(componentMiner.ActiveBlockValue) == 367)
			{
				target.Entity.FindComponent<ComponentHealth>()?.Injure(0.3f, attacker, ignoreInvulnerability: false, "Sledgehammer blow");
				float stunTime = 0.5f;
				Vector3 impulse = 5f * Vector3.Normalize(hitDirection + s_random.Vector3(0.1f));
				target.ApplyImpulse(impulse);
				ComponentLocomotion componentLocomotion = target.Entity.FindComponent<ComponentLocomotion>();
				if (componentLocomotion != null)
				{
					componentLocomotion.StunTime = stunTime;
				}
				return;
			}
		}
		bool flag = IsTargetSleeping(target.Entity);
		if (attackPower > 0f)
		{
			ComponentClothing componentClothing = target.Entity.FindComponent<ComponentClothing>();
			if (componentClothing != null)
			{
				attackPower = componentClothing.ApplyArmorProtection(attackPower);
			}
			ComponentLevel componentLevel = target.Entity.FindComponent<ComponentLevel>();
			if (componentLevel != null)
			{
				attackPower /= componentLevel.ResilienceFactor;
			}
			ComponentHealth componentHealth = target.Entity.FindComponent<ComponentHealth>();
			if (componentHealth != null)
			{
				float num = attackPower / componentHealth.AttackResilience;
				string cause;
				if (attacker == null)
				{
					cause = s_random.Int(0, 5) switch
					{
						0 => LanguageControl.Get(fName, 5), 
						1 => LanguageControl.Get(fName, 6), 
						2 => LanguageControl.Get(fName, 7), 
						3 => LanguageControl.Get(fName, 8), 
						4 => LanguageControl.Get(fName, 9), 
						_ => LanguageControl.Get(fName, 10), 
					};
				}
				else
				{
					string text = attacker.KillVerbs[s_random.Int(0, attacker.KillVerbs.Count - 1)];
					string displayName = attacker.DisplayName;
					cause = string.Format(LanguageControl.Get(fName, 4), displayName, LanguageControl.Get(fName, text));
				}
				float health = componentHealth.Health;
				componentHealth.Injure(num, attacker, ignoreInvulnerability: false, cause);
				if (num > 0f)
				{
					if (attacker != null && target.Player != null)
					{
						SubsystemGameInfo subsystemGameInfo = attacker.Entity.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
						if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace && health - componentHealth.Health > 0f)
						{
							target.Entity.FindComponent<ComponentAdrenaline>()?.AddAdrenaline(0.01f);
						}
					}
					target.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlayRandomSound("Audio/Impacts/Body", 1f, s_random.Float(-0.3f, 0.3f), target.Position, 4f, autoDelay: false);
					if (isMeleeAttack && attacker != null)
					{
						SubsystemGameInfo subsystemGameInfo2 = attacker.Entity.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
						if (subsystemGameInfo2 != null && subsystemGameInfo2.WorldSettings.GameMode == GameMode.Embrace)
						{
							ComponentAdrenaline componentAdrenaline = attacker.Entity.FindComponent<ComponentAdrenaline>();
							if (attacker.Entity.FindComponent<ComponentPlayer>() != null && componentAdrenaline != null)
							{
								if (componentAdrenaline.Adrenaline < 0.3f)
								{
									componentAdrenaline.AddAdrenaline(0.02f);
								}
								else
								{
									componentAdrenaline.AddAdrenaline(0.01f);
								}
							}
						}
					}
					if (isMeleeAttack && attacker != null)
					{
						SubsystemGameInfo subsystemGameInfo3 = attacker.Entity.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
						if (subsystemGameInfo3 != null && subsystemGameInfo3.WorldSettings.GameMode == GameMode.Embrace && attacker.Entity.FindComponent<ComponentPlayer>() != null)
						{
							float num2 = health - componentHealth.Health;
							if (num2 > 0f)
							{
								attacker.Entity.FindComponent<ComponentHealth>()?.Heal(num2);
							}
						}
					}
					float num3 = (health - componentHealth.Health) * componentHealth.AttackResilience;
					if (attacker is ComponentPlayer && num3 > 0f)
					{
						string text2 = (0f - num3).ToString("0", CultureInfo.InvariantCulture);
						Vector3 position = hitPoint + 0.75f * hitDirection;
						Vector3 velocity = 1f * hitDirection + attacker.ComponentBody.Velocity;
						Color color = (flag ? Color.Red : Color.White);
						if (CommonLib.WorkType != WorkType.Client)
						{
							CommonLib.Net.QueuePackage(new ComponentHealthPackage(position, velocity, color, text2));
							HitValueParticleSystem particleSystem = new HitValueParticleSystem(position, velocity, color, text2);
							target.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
						}
					}
					if (attacker is ComponentPlayer && health > 0f && componentHealth.Health == 0f)
					{
						ComponentMiner componentMiner2 = attacker.Entity.FindComponent<ComponentMiner>();
						if (componentMiner2 != null && Terrain.ExtractContents(componentMiner2.ActiveBlockValue) == 23)
						{
							string name = target.Entity.ValuesDictionary.DatabaseObject.Name;
							if (!string.IsNullOrEmpty(name) && name.StartsWith("Rhino", StringComparison.OrdinalIgnoreCase))
							{
								PlayerAchievements playerAchievements = attacker.PlayerAchievements;
								if (playerAchievements != null && playerAchievements.AthleticEndurance == 0L)
								{
									playerAchievements.AthleticEndurance = 1L;
									playerAchievements.EnqueueAthleticEnduranceAchievement();
								}
							}
						}
					}
					if (attacker is ComponentPlayer && health > 0f && componentHealth.Health == 0f)
					{
						string name2 = target.Entity.ValuesDictionary.DatabaseObject.Name;
						if (!string.IsNullOrEmpty(name2) && name2.Equals("Bear_Polar", StringComparison.OrdinalIgnoreCase))
						{
							PlayerAchievements playerAchievements2 = attacker.PlayerAchievements;
							if (playerAchievements2 != null && playerAchievements2.PolarStarJaws == 0L)
							{
								playerAchievements2.PolarStarJaws = 1L;
								playerAchievements2.EnqueuePolarStarJawsAchievement();
							}
						}
					}
					if (target.Player != null && attacker != null && componentHealth.Health <= 0f && attacker.Entity != target.Entity)
					{
						PlayerAchievements playerAchievements3 = attacker.PlayerAchievements;
						if (playerAchievements3 != null && playerAchievements3.Abomination == 0L)
						{
							playerAchievements3.Abomination = 1L;
							playerAchievements3.EnqueueAbominationAchievement();
						}
					}
					if (target.Player != null && attacker != null && componentHealth.Health <= 0f && attacker.Entity == target.Entity)
					{
						PlayerAchievements playerAchievements4 = attacker.PlayerAchievements;
						if (playerAchievements4 != null && playerAchievements4.MostAccurateShooterInWest == 0L)
						{
							playerAchievements4.MostAccurateShooterInWest = 1L;
							playerAchievements4.EnqueueMostAccurateShooterInWestAchievement();
						}
					}
				}
			}
			ComponentDamage componentDamage = target.Entity.FindComponent<ComponentDamage>();
			if (componentDamage != null)
			{
				float num4 = attackPower / componentDamage.AttackResilience;
				componentDamage.Damage(num4);
				if (num4 > 0f)
				{
					target.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlayRandomSound(componentDamage.DamageSoundName, 1f, s_random.Float(-0.3f, 0.3f), target.Position, 4f, autoDelay: false);
				}
			}
			bool flag2 = false;
			ComponentHealth componentHealth2 = target.Entity.FindComponent<ComponentHealth>();
			if (componentHealth2 != null && componentHealth2.Health <= 0f && componentHealth2.DeathTime.HasValue)
			{
				SubsystemGameInfo subsystemGameInfo4 = target.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
				if (subsystemGameInfo4 != null && subsystemGameInfo4.TotalElapsedGameTime - componentHealth2.DeathTime.Value > 1000.0)
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				Entity entity = target.Entity;
				string text3 = ((entity != null && entity.ValuesDictionary != null && entity.ValuesDictionary.DatabaseObject != null) ? entity.ValuesDictionary.DatabaseObject.Name : null);
				if ((entity == null || entity.FindComponent<ComponentBoat>() == null) && (entity == null || entity.FindComponent<ComponentShipModel>() == null) && (string.IsNullOrEmpty(text3) || (!text3.Equals("Boat", StringComparison.OrdinalIgnoreCase) && !text3.Equals("Raft", StringComparison.OrdinalIgnoreCase) && !text3.Equals("Sailing_Boat", StringComparison.OrdinalIgnoreCase))))
				{
					float num5 = componentHealth2?.AttackResilience ?? 20f;
					float num6;
					if (num5 <= 15f)
					{
						num6 = 0.25f;
					}
					else
					{
						num6 = ((!(num5 <= 150f)) ? MathUtils.Lerp(1f, 1.3f, MathUtils.Clamp((num5 - 150f) / 255f, 0f, 1f)) : MathUtils.Lerp(0.5f, 1f, (num5 - 15f) / 135f));
					}
					int num7 = s_random.Int(3, 7);
					int num8 = MathUtils.Max(1, (int)MathUtils.Round((float)num7 * num6));
					SubsystemParticles subsystemParticles = target.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
					SubsystemTerrain subsystemTerrain = target.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
					Vector3 v = ((attacker != null) ? Vector3.Normalize(hitPoint - attacker.ComponentBody.Position) : hitDirection);
					Vector3 v2 = Vector3.Normalize(target.Position - hitPoint);
					Vector3 direction = Vector3.Normalize(Vector3.Lerp(Vector3.Lerp(Vector3.Normalize(s_random.Vector3(1f)), v2, 0.3f), v, 0.3f));
					int num9 = 0;
					int num10 = 0;
					for (int i = 0; i < num8; i++)
					{
						if (s_random.Float(0f, 1f) > 0.4f)
						{
							num9++;
						}
						else
						{
							num10++;
						}
					}
					if (num9 > 0)
					{
						subsystemParticles.AddParticleSystem(new BloodDropletParticleSystem(subsystemTerrain, hitPoint, direction, num9, 25f, 3f, 6f, -9.81f));
					}
					if (num10 > 0)
					{
						subsystemParticles.AddParticleSystem(new BloodDropletParticleSystem(subsystemTerrain, hitPoint, direction, num10, 4f, 1f, 6f, -9.81f));
					}
				}
			}
		}
		float num11 = 0f;
		float x = 0f;
		if (isMeleeAttack && attacker != null)
		{
			float num12 = ((attackPower >= 2f) ? 1.25f : 1f);
			float num13 = MathUtils.Pow(attacker.ComponentBody.Mass / target.Mass, 0.5f);
			float x2 = num12 * num13;
			num11 = 5.5f * MathUtils.Saturate(x2);
			x = 0.25f * MathUtils.Saturate(x2);
		}
		else if (attackPower > 0f)
		{
			num11 = 2f;
			x = 0.2f;
		}
		if (!(num11 > 0f))
		{
			return;
		}
		float num14 = 0f;
		float num15 = 0f;
		float num16 = 1f;
		if (attacker != null)
		{
			ComponentMiner componentMiner3 = attacker.Entity.FindComponent<ComponentMiner>();
			if (componentMiner3 != null)
			{
				num14 = componentMiner3.KnockbackPower;
				num15 = componentMiner3.VerticalKnockbackPower;
			}
			num16 = attacker.ComponentBody.Size;
		}
		if (num16 > 1f)
		{
			float num17 = MathUtils.Pow(num16, 1.5f);
			num14 += num17 * 2f;
			num15 += num16 * 0.5f;
		}
		num11 *= num14;
		float num18 = num15;
		Vector3 vector = num18 * Vector3.UnitY;
		if (num11 > 0f || num18 > 0f)
		{
			Vector3 impulse2 = num11 * Vector3.Normalize(hitDirection + s_random.Vector3(0.1f)) + vector;
			target.ApplyImpulse(impulse2);
			ComponentLocomotion componentLocomotion2 = target.Entity.FindComponent<ComponentLocomotion>();
			if (componentLocomotion2 != null)
			{
				componentLocomotion2.StunTime = MathUtils.Max(componentLocomotion2.StunTime, x);
			}
		}
		if (num16 > 1.5f)
		{
			SubsystemTerrain terrain = target.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			SubsystemWeather weather = target.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			target.Project.FindSubsystem<SubsystemParticles>(throwOnError: true)?.AddParticleSystem(new CaveDustBurstParticleSystem(terrain, weather, hitPoint));
		}
	}

	public void Update(float dt)
	{
		m_lastPokingPhase = PokingPhase;
		if (DigCellFace.HasValue)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(DigCellFace.Value.X, DigCellFace.Value.Y, DigCellFace.Value.Z);
			int toolContents = Terrain.ExtractContents(ActiveBlockValue);
			float num = CalculateDigTime(cellValue, toolContents);
			if (num > 0f && num < 1000f)
			{
				float num2 = (float)Math.Round(MathUtils.Max(1f, num * 1.5f));
				float num3 = 0.1f;
				float num4 = 1f - num3 / num;
				if (num4 < 0.5f)
				{
					num4 = 0.9f;
				}
				float num5 = num2 - 1f + 0.5f;
				float x = m_digProgress / num4 * num5;
				PokingPhase = MathUtils.Remainder(x, 1f);
			}
			else
			{
				float num6 = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? 10f : 1.5f);
				PokingPhase = MathUtils.Remainder(PokingPhase + num6 * dt, 1f);
			}
		}
		else if (PokingPhase > 0f)
		{
			if (m_lastPokingPhase <= 0.5f && PokingPhase > 0.5f)
			{
				PokingCounter++;
			}
			float num7 = ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? (0.5f / SettingsManager.CreativeDigTime) : 4f);
			if (PokingPhase >= 0.5f)
			{
				PokingPhase += num7 * dt;
			}
			else
			{
				PokingPhase += num7 * dt;
			}
			if (PokingPhase > 1f)
			{
				PokingPhase = 0f;
			}
		}
		if (DigCellFace.HasValue && (double)(Time.FrameIndex - m_lastDigFrameIndex) > 1.0)
		{
			DigCellFace = null;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
		ComponentCreature = Entity.FindComponent<ComponentCreature>(throwOnError: true);
		ComponentPlayer = Entity.FindComponent<ComponentPlayer>();
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && ComponentPlayer != null)
		{
			Inventory = Entity.FindComponent<ComponentCreativeInventory>();
		}
		else
		{
			Inventory = Entity.FindComponent<ComponentInventory>();
		}
		AttackPower = valuesDictionary.GetValue<float>("AttackPower");
		KnockbackPower = valuesDictionary.GetValue<float>("KnockbackPower");
		VerticalKnockbackPower = valuesDictionary.GetValue<float>("VerticalKnockbackPower");
	}

	public static bool IsBlockPlacingAllowed(ComponentBody componentBody)
	{
		if (componentBody.StandingOnBody != null || componentBody.StandingOnValue.HasValue)
		{
			return true;
		}
		if (componentBody.ImmersionFactor > 0.01f)
		{
			return true;
		}
		if (componentBody.ParentBody != null && IsBlockPlacingAllowed(componentBody.ParentBody))
		{
			return true;
		}
		return componentBody.Entity.FindComponent<ComponentLocomotion>()?.LadderValue.HasValue ?? false;
	}

	public float CalculateDigTime(int digValue, int toolContents)
	{
		Block obj = BlocksManager.Blocks[Terrain.ExtractContents(digValue)];
		BlockDigMethod flag = obj.GetBlockDigMethod(digValue);
		float num = obj.GetDigResilience(digValue);
		if (ComponentPlayer != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			if (num < float.PositiveInfinity)
			{
				return 0f;
			}
			return float.PositiveInfinity;
		}
		float i = 0f;
		int particle = (SecondaryActiveSlotIndex.HasValue ? Terrain.ExtractContents(SecondaryBlockValue) : 0);
		for (int j = 0; j < 2; j++)
		{
			if (j != 1 || SecondaryActiveSlotIndex.HasValue)
			{
				int num2 = ((j == 0) ? toolContents : particle);
				Block block = BlocksManager.Blocks[num2];
				float num3 = 0f;
				switch (flag)
				{
				case BlockDigMethod.Shovel:
				{
					SubsystemWeather subsystemWeather = ComponentPlayer?.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
					num3 = ((subsystemWeather == null || (subsystemWeather.CurrentSeason != SubsystemWeather.Season.LateAutumn && subsystemWeather.CurrentSeason != SubsystemWeather.Season.Autumn && subsystemWeather.CurrentSeason != SubsystemWeather.Season.EarlyAutumn && subsystemWeather.CurrentSeason != SubsystemWeather.Season.Anabiosis && subsystemWeather.CurrentSeason != SubsystemWeather.Season.Winter && subsystemWeather.CurrentSeason != SubsystemWeather.Season.LateWinter)) ? block.ShovelPower : (block.ShovelPower * 0.5f));
					break;
				}
				case BlockDigMethod.Quarry:
					num3 = block.QuarryPower;
					break;
				case BlockDigMethod.Hack:
					num3 = block.HackPower;
					break;
				}
				if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure && num3 < 2f)
				{
					num3 = 0f;
				}
				i += num3;
			}
		}
		if (i <= 0f)
		{
			return float.PositiveInfinity;
		}
		if (ComponentPlayer != null)
		{
			i *= ComponentPlayer.ComponentLevel.StrengthFactor;
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
			{
				i *= 3f;
			}
			ComponentClothing componentClothing = ComponentPlayer.Entity.FindComponent<ComponentClothing>();
			if (componentClothing != null)
			{
				foreach (ClothingSlot value in Enum.GetValues(typeof(ClothingSlot)))
				{
					foreach (int item in componentClothing.GetClothes(value))
					{
						ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(item)].GetClothingData(Terrain.ExtractData(item));
						i *= clothingData.MinerSpeed;
					}
				}
			}
			ComponentVitalStats componentVitalStats = ComponentPlayer.Entity.FindComponent<ComponentVitalStats>();
			if (componentVitalStats != null)
			{
				if (componentVitalStats.Food < 0.1f)
				{
					i *= 0.5f;
				}
				else if (componentVitalStats.Food > 0.75f)
				{
					float num4 = 1f + 0.5f * ((componentVitalStats.Food - 0.75f) / 0.25f);
					i *= num4;
				}
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace)
			{
				ComponentAdrenaline componentAdrenaline = Entity.FindComponent<ComponentAdrenaline>();
				if (componentAdrenaline != null && componentAdrenaline.Adrenaline > 0f)
				{
					float num5 = MathUtils.Pow(2f, componentAdrenaline.Adrenaline);
					i *= num5;
				}
			}
		}
		return MathUtils.Max(num / i, 0f);
	}

	public bool CanUseTool(int toolValue)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
		{
			Block num = BlocksManager.Blocks[Terrain.ExtractContents(toolValue)];
			if (ComponentPlayer != null && ComponentPlayer.PlayerData.Level < (float)num.PlayerLevelRequired)
			{
				return false;
			}
		}
		return true;
	}

	public bool CanPlaceBlocks()
	{
		return ComponentPlayer.ComponentVitalStats.Stamina >= 0.15f;
	}

	public void Destroy3x3Around(CellFace cellFace)
	{
		Vector3 data = Vector3.Normalize(ComponentCreature.ComponentCreatureModel.EyePosition - new Vector3(cellFace.X, cellFace.Y, cellFace.Z));
		Point3[] connectorDirection;
		if (Math.Abs(data.Y) > Math.Abs(data.X) && Math.Abs(data.Y) > Math.Abs(data.Z))
		{
			connectorDirection = new Point3[9]
			{
				new Point3(-1, 0, -1),
				new Point3(0, 0, -1),
				new Point3(1, 0, -1),
				new Point3(-1, 0, 0),
				new Point3(0, 0, 0),
				new Point3(1, 0, 0),
				new Point3(-1, 0, 1),
				new Point3(0, 0, 1),
				new Point3(1, 0, 1)
			};
		}
		else
		{
			connectorDirection = ((!(Math.Abs(data.X) > Math.Abs(data.Z))) ? new Point3[9]
			{
				new Point3(-1, -1, 0),
				new Point3(0, -1, 0),
				new Point3(1, -1, 0),
				new Point3(-1, 0, 0),
				new Point3(0, 0, 0),
				new Point3(1, 0, 0),
				new Point3(-1, 1, 0),
				new Point3(0, 1, 0),
				new Point3(1, 1, 0)
			} : new Point3[9]
			{
				new Point3(0, -1, -1),
				new Point3(0, 0, -1),
				new Point3(0, 1, -1),
				new Point3(0, -1, 0),
				new Point3(0, 0, 0),
				new Point3(0, 1, 0),
				new Point3(0, -1, 1),
				new Point3(0, 0, 1),
				new Point3(0, 1, 1)
			});
		}
		SubsystemParticles subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		SubsystemCaveInstability subsystemCaveInstability = Project.FindSubsystem<SubsystemCaveInstability>(throwOnError: true);
		Point3[] array = connectorDirection;
		for (int i = 0; i < array.Length; i++)
		{
			Point3 point = array[i];
			int num = cellFace.X + point.X;
			int num2 = cellFace.Y + point.Y;
			int num3 = cellFace.Z + point.Z;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(num, num2, num3);
			int value = Terrain.ExtractContents(cellValue);
			if (subsystemCaveInstability != null && Array.IndexOf(subsystemCaveInstability.HandledBlocks, value) >= 0)
			{
				subsystemCaveInstability.MarkPlayerBreak(new Point3(num, num2, num3));
			}
			m_subsystemTerrain.DestroyCell(0, num, num2, num3, 0, noDrop: false, noParticleSystem: false, this);
			m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(num, num2, num3), 1f);
			Vector3 vector = new Vector3(m_random.Float(-0.2f, 0.2f), m_random.Float(-0.2f, 0.2f), m_random.Float(-0.2f, 0.2f));
			Vector3 position = new Vector3((float)num + 0.5f, (float)num2 + 0.5f, (float)num3 + 0.5f) + vector;
			Vector3 direction = Vector3.UnitY * 0.003f;
			GunSmokeParticleSystem particleSystem = new GunSmokeParticleSystem(m_subsystemTerrain, position, direction)
			{
				ParticleSize = 0.6f,
				SpawnRateMultiplier = 0.1f
			};
			subsystemParticles.AddParticleSystem(particleSystem);
		}
	}

	private Point3[] GetOffsetsForFace(int face)
	{
		return face switch
		{
			0 => new Point3[9]
			{
				new Point3(-1, 0, -1),
				new Point3(0, 0, -1),
				new Point3(1, 0, -1),
				new Point3(-1, 0, 0),
				new Point3(0, 0, 0),
				new Point3(1, 0, 0),
				new Point3(-1, 0, 1),
				new Point3(0, 0, 1),
				new Point3(1, 0, 1)
			}, 
			1 => new Point3[9]
			{
				new Point3(-1, 0, -1),
				new Point3(0, 0, -1),
				new Point3(1, 0, -1),
				new Point3(-1, 0, 0),
				new Point3(0, 0, 0),
				new Point3(1, 0, 0),
				new Point3(-1, 0, 1),
				new Point3(0, 0, 1),
				new Point3(1, 0, 1)
			}, 
			2 => new Point3[9]
			{
				new Point3(-1, -1, 0),
				new Point3(0, -1, 0),
				new Point3(1, -1, 0),
				new Point3(-1, 0, 0),
				new Point3(0, 0, 0),
				new Point3(1, 0, 0),
				new Point3(-1, 1, 0),
				new Point3(0, 1, 0),
				new Point3(1, 1, 0)
			}, 
			3 => new Point3[9]
			{
				new Point3(-1, -1, 0),
				new Point3(0, -1, 0),
				new Point3(1, -1, 0),
				new Point3(-1, 0, 0),
				new Point3(0, 0, 0),
				new Point3(1, 0, 0),
				new Point3(-1, 1, 0),
				new Point3(0, 1, 0),
				new Point3(1, 1, 0)
			}, 
			4 => new Point3[9]
			{
				new Point3(0, -1, -1),
				new Point3(0, 0, -1),
				new Point3(0, 1, -1),
				new Point3(0, -1, 0),
				new Point3(0, 0, 0),
				new Point3(0, 1, 0),
				new Point3(0, -1, 1),
				new Point3(0, 0, 1),
				new Point3(0, 1, 1)
			}, 
			5 => new Point3[9]
			{
				new Point3(0, -1, -1),
				new Point3(0, 0, -1),
				new Point3(0, 1, -1),
				new Point3(0, -1, 0),
				new Point3(0, 0, 0),
				new Point3(0, 1, 0),
				new Point3(0, -1, 1),
				new Point3(0, 0, 1),
				new Point3(0, 1, 1)
			}, 
			_ => new Point3[0], 
		};
	}

	private void SplashAttack(Vector3 hitPoint, Vector3 hitDirection)
	{
		Point3[] array = new Point3[9]
		{
			new Point3(-1, 0, -1),
			new Point3(0, 0, -1),
			new Point3(1, 0, -1),
			new Point3(-1, 0, 0),
			new Point3(0, 0, 0),
			new Point3(1, 0, 0),
			new Point3(-1, 0, 1),
			new Point3(0, 0, 1),
			new Point3(1, 0, 1)
		};
		Vector3 impulse = -hitDirection * 1.5f;
		ComponentCreature.ComponentBody.ApplyImpulse(impulse);
		if (ComponentPlayer != null)
		{
			Poke(forceRestart: true);
		}
		SubsystemParticles subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		Point3[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Point3 point = array2[i];
			Vector3 vector = hitPoint + new Vector3(point.X, point.Y, point.Z);
			GunSmokeParticleSystem particleSystem = new GunSmokeParticleSystem(m_subsystemTerrain, vector, Vector3.UnitY * 0.1f)
			{
				ParticleSize = 0.6f,
				SpawnRateMultiplier = 0.1f
			};
			subsystemParticles.AddParticleSystem(particleSystem);
			foreach (ComponentBody body in m_subsystemBodies.Bodies)
			{
				if (Vector3.Distance(body.Position, vector) <= 1.5f)
				{
					AttackBody(body, ComponentCreature, vector, hitDirection, 0.3f, isMeleeAttack: true);
				}
			}
		}
	}

	private void PerformSingleHit(ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection)
	{
		int voltage = ActiveBlockValue;
		Block num = BlocksManager.Blocks[Terrain.ExtractContents(voltage)];
		float num2;
		float connection;
		if (voltage != 0)
		{
			num2 = num.GetMeleePower(voltage);
			connection = num.GetMeleeHitProbability(voltage);
		}
		else
		{
			num2 = 1f;
			connection = 0.66f;
		}
		float num3 = 0f;
		if (SecondaryActiveSlotIndex.HasValue && SecondaryBlockValue != 0)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(SecondaryBlockValue)];
			float meleePower = block.GetMeleePower(SecondaryBlockValue);
			if (meleePower > 1f)
			{
				num3 = meleePower;
				connection = MathUtils.Min(connection, block.GetMeleeHitProbability(SecondaryBlockValue));
			}
		}
		float attackPower = (num2 + num3) * AttackPower * m_random.Float(0.8f, 1.2f);
		if (m_random.Bool(connection))
		{
			AttackBody(componentBody, ComponentCreature, hitPoint, hitDirection, attackPower, isMeleeAttack: true);
			DamageActiveTool(1);
			if (num3 > 0f && SecondaryActiveSlotIndex.HasValue)
			{
				int activeSlotIndex = Inventory.ActiveSlotIndex;
				Inventory.ActiveSlotIndex = SecondaryActiveSlotIndex.Value;
				DamageActiveTool(1);
				Inventory.ActiveSlotIndex = activeSlotIndex;
			}
		}
	}

	private bool IsRake(int value)
	{
		int model = Terrain.ExtractContents(value);
		if (model != 169 && model != 219 && model != 171 && model != 172)
		{
			return model == 409;
		}
		return true;
	}

	private static bool IsTargetSleeping(Entity e)
	{
		ComponentWalkAroundBehavior color2 = e.FindComponent<ComponentWalkAroundBehavior>();
		if (color2 != null && color2.IsSleeping)
		{
			return true;
		}
		ComponentFlyAroundBehavior componentFlyAroundBehavior = e.FindComponent<ComponentFlyAroundBehavior>();
		if (componentFlyAroundBehavior != null && componentFlyAroundBehavior.IsSleeping)
		{
			return true;
		}
		return e.FindComponent<ComponentPeacefulFlyAroundBehavior>()?.IsSleeping ?? false;
	}

	public bool Interact(MovingBlocksRaycastResult raycastResult)
	{
		if (!(raycastResult.MovingBlockSet is SubsystemMovingBlocks.MovingBlockSet movingBlockSet))
		{
			return false;
		}
		Matrix m = Matrix.Invert(Matrix.CreateFromQuaternion(movingBlockSet.Rotation) * Matrix.CreateTranslation(movingBlockSet.Position));
		Vector3 position = Vector3.Transform(raycastResult.Ray.Position, m);
		Vector3 v = Vector3.TransformNormal(raycastResult.Ray.Direction, m);
		Ray3 ray = new Ray3(position, Vector3.Normalize(v));
		float num = float.MaxValue;
		MovingBlock? movingBlock = null;
		foreach (MovingBlock block in movingBlockSet.Blocks)
		{
			BoundingBox box = new BoundingBox(new Vector3(block.Offset), new Vector3(block.Offset + new Point3(1, 1, 1)));
			float? num2 = ray.Intersection(box);
			if (num2.HasValue && num2.Value < num)
			{
				num = num2.Value;
				movingBlock = block;
			}
		}
		if (!movingBlock.HasValue)
		{
			return false;
		}
		MovingBlock value = movingBlock.Value;
		int contents = Terrain.ExtractContents(value.Value);
		SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents);
		for (int i = 0; i < blockBehaviors.Length; i++)
		{
			if (blockBehaviors[i].OnInteract(raycastResult, value.Offset, this))
			{
				if (ComponentCreature.PlayerStats != null)
				{
					ComponentCreature.PlayerStats.BlocksInteracted++;
				}
				Poke(forceRestart: false);
				return true;
			}
		}
		return false;
	}
}
