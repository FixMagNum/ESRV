using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemPaintGunBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemTerrain m_terrain;

	private SubsystemAudio m_audio;

	private SubsystemParticles m_particles;

	private Random m_rand;

	private bool m_has;

	private bool m_hold;

	private float m_timer;

	private static readonly Func<int, float, bool> m_filter;

	private int m_useCounter;

	public SubsystemGameInfo m_subsystemGameInfo;

	public override int[] HandledBlocks => new int[2] { 410, 411 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary dict)
	{
		base.Load(dict);
		m_terrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_audio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_particles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_rand = new Random();
	}

	public override bool OnUse(Ray3 ray, ComponentMiner miner)
	{
		m_useCounter = (m_useCounter + 1) % 3;
		bool flag = m_useCounter == 0;
		bool num = m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled;
		int activeBlockValue = miner.ActiveBlockValue;
		bool flag2 = Terrain.ExtractContents(activeBlockValue) == 411;
		int num2 = Terrain.ExtractData(activeBlockValue);
		int num3 = PaintGunNearBlock.GetFluid(num2);
		if (num)
		{
			if (num3 > 0)
			{
				if (flag)
				{
					miner.DamageActiveTool(1);
				}
				activeBlockValue = miner.ActiveBlockValue;
				num2 = Terrain.ExtractData(activeBlockValue);
				num3 = PaintGunNearBlock.GetFluid(num2);
				num3--;
				num2 = PaintGunNearBlock.SetFluid(num2, num3);
			}
			if (num3 == 0)
			{
				num2 = PaintGunNearBlock.SetCleaner(num2, b: false);
				num2 = PaintGunNearBlock.SetColor(num2, 0);
			}
			activeBlockValue = Terrain.ReplaceData(activeBlockValue, num2);
			miner.Inventory.RemoveSlotItems(miner.Inventory.ActiveSlotIndex, 1);
			miner.Inventory.AddSlotItems(miner.Inventory.ActiveSlotIndex, activeBlockValue, 1);
		}
		bool cleaner = PaintGunNearBlock.GetCleaner(num2);
		int color = PaintGunNearBlock.GetColor(num2);
		if (color == 0 && !cleaner)
		{
			miner.Entity.FindComponent<ComponentPlayer>()?.ComponentGui.DisplaySmallMessage(LanguageControl.Get("PaintGunWidget", 4), Color.White, blinking: true, playNotificationSound: false);
			return false;
		}
		float num4 = (flag2 ? 24f : 5f) + 0.5f;
		Vector3 vector = ray.Position + ray.Direction;
		Vector3 direction = ray.Direction;
		float num5 = 1f;
		Color color2 = SubsystemPalette.GetColor(m_terrain, color);
		if (flag2)
		{
			for (float num6 = 0f; num6 <= num4; num6 += num5)
			{
				Vector3 vector2 = vector + direction * num6;
				m_particles.AddParticleSystem(new PaintParticleSystem(m_terrain, vector2 + new Vector3(0f, -0.5f, 0f), direction, color2));
				if (m_terrain.Raycast(vector2, vector2 + direction * num5, useInteractionBoxes: false, skipAirBlocks: true, m_filter).HasValue)
				{
					break;
				}
			}
		}
		else
		{
			Vector3 vector3 = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitX));
			if (vector3.Length() < 0.01f)
			{
				vector3 = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitZ));
			}
			Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector3, direction));
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (float num7 = 0f; num7 <= num4; num7 += num5)
					{
						Vector3 vector5 = vector + vector4 * i * (num7 / num4) + vector3 * j * (num7 / num4);
						m_particles.AddParticleSystem(new PaintParticleSystem(m_terrain, vector5 + direction * num7 + new Vector3(0f, -0.5f, 0f), direction, color2));
						if (m_terrain.Raycast(vector5, vector5 + direction * num5, useInteractionBoxes: false, skipAirBlocks: true, m_filter).HasValue)
						{
							break;
						}
					}
				}
			}
		}
		if (flag2)
		{
			TerrainRaycastResult? terrainRaycastResult = m_terrain.Raycast(vector, vector + direction * num4, useInteractionBoxes: true, skipAirBlocks: true, null);
			bool num8 = terrainRaycastResult.HasValue && PaintSingle(terrainRaycastResult.Value, cleaner ? ((int?)null) : new int?(color), miner);
			bool flag3 = PaintCreatures(vector, direction, num4, cleaner ? ((int?)null) : new int?(color), miner);
			return num8 | flag3;
		}
		bool flag4 = false;
		Vector3 vector6 = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitX));
		if (vector6.Length() < 0.01f)
		{
			vector6 = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitZ));
		}
		Vector3 vector7 = Vector3.Normalize(Vector3.Cross(vector6, direction));
		for (int k = -1; k <= 1; k++)
		{
			for (int l = -1; l <= 1; l++)
			{
				Vector3 vector8 = vector + vector7 * k + vector6 * l;
				TerrainRaycastResult? terrainRaycastResult2 = m_terrain.Raycast(vector8, vector8 + direction * num4, useInteractionBoxes: true, skipAirBlocks: true, m_filter);
				if (terrainRaycastResult2.HasValue && PaintSingle(terrainRaycastResult2.Value, cleaner ? ((int?)null) : new int?(color), miner))
				{
					m_audio.PlayRandomSound("Audio/Paint", 0.4f, m_rand.Float(-0.2f, 0.2f), terrainRaycastResult2.Value.HitPoint(), 2f, autoDelay: true);
					flag4 = true;
				}
			}
		}
		bool flag5 = PaintCreatures(vector, direction, num4, cleaner ? ((int?)null) : new int?(color), miner);
		return flag4 | flag5;
	}

	public override bool OnEditInventoryItem(IInventory inv, int slot, ComponentPlayer pl)
	{
		pl.ComponentGui.ModalPanelWidget = ((pl.ComponentGui.ModalPanelWidget == null) ? new PaintGunWidget(inv, slot) : null);
		return true;
	}

	public override int GetProcessInventoryItemCapacity(IInventory inv, int slot, int value)
	{
		int num = Terrain.ExtractContents(inv.GetSlotValue(slot));
		if (num != 410 && num != 411)
		{
			return 0;
		}
		switch (Terrain.ExtractContents(value))
		{
		case 128:
		case 129:
			return 1;
		default:
			return 0;
		case 90:
			if (PaintGunNearBlock.GetFluid(Terrain.ExtractData(inv.GetSlotValue(slot))) <= 0)
			{
				return 0;
			}
			return 1;
		}
	}

	public override void ProcessInventoryItem(IInventory inv, int slot, int value, int count, int pc, out int pv, out int pcnt)
	{
		if (pc == 1)
		{
			int slotValue = inv.GetSlotValue(slot);
			int contents = Terrain.ExtractContents(slotValue);
			int d = Terrain.ExtractData(slotValue);
			int num = Terrain.ExtractContents(value);
			bool flag = m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled;
			switch (num)
			{
			case 128:
			case 129:
			{
				d = PaintGunNearBlock.SetFluid(d, 0);
				d = PaintGunNearBlock.SetColor(d, 0);
				d = PaintGunNearBlock.SetCleaner(d, b: false);
				Block obj = BlocksManager.Blocks[num];
				int durability2 = obj.Durability;
				int damage2 = obj.GetDamage(value);
				int f = MathUtils.Clamp(durability2 - damage2, 0, 60);
				if (num == 129)
				{
					int color2 = PaintBucketBlock.GetColor(Terrain.ExtractData(value));
					d = PaintGunNearBlock.SetColor(d, color2);
					d = PaintGunNearBlock.SetCleaner(d, b: false);
				}
				else
				{
					d = PaintGunNearBlock.SetCleaner(d, b: true);
				}
				d = PaintGunNearBlock.SetFluid(d, f);
				int value4 = Terrain.MakeBlockValue(contents, 0, d);
				inv.RemoveSlotItems(slot, 1);
				inv.AddSlotItems(slot, value4, 1);
				if (flag)
				{
					pv = Terrain.MakeBlockValue(90);
					pcnt = 1;
				}
				else
				{
					pv = 0;
					pcnt = 0;
				}
				return;
			}
			case 90:
			{
				int fluid = PaintGunNearBlock.GetFluid(d);
				if (fluid == 0)
				{
					pv = value;
					pcnt = count;
					return;
				}
				bool cleaner = PaintGunNearBlock.GetCleaner(d);
				int color = PaintGunNearBlock.GetColor(d);
				int num2 = (cleaner ? 128 : 129);
				Block block = BlocksManager.Blocks[num2];
				int durability = block.Durability;
				int damage = MathUtils.Clamp(durability - fluid, 0, durability);
				int data = 0;
				if (!cleaner)
				{
					data = PaintBucketBlock.SetColor(data, color);
				}
				int value2 = Terrain.MakeBlockValue(num2, 0, data);
				value2 = block.SetDamage(value2, damage);
				int num3 = ComponentInventoryBase.FindAcquireSlotForItem(inv, value2);
				if (num3 < 0 && inv.GetSlotCount(slot) == 1)
				{
					num3 = slot;
				}
				if (num3 < 0)
				{
					pv = value;
					pcnt = count;
					return;
				}
				d = PaintGunNearBlock.SetFluid(d, 0);
				d = PaintGunNearBlock.SetColor(d, 0);
				d = PaintGunNearBlock.SetCleaner(d, b: false);
				int value3 = Terrain.MakeBlockValue(contents, 0, d);
				inv.RemoveSlotItems(slot, 1);
				inv.AddSlotItems(slot, value3, 1);
				inv.AddSlotItems(num3, value2, 1);
				pv = 0;
				pcnt = 0;
				return;
			}
			}
		}
		pv = value;
		pcnt = count;
	}

	private bool PaintSingle(TerrainRaycastResult res, int? color, ComponentMiner miner)
	{
		CellFace cellFace = res.CellFace;
		if (IsPaintingBlockedByPlacement(miner, cellFace.X, cellFace.Z))
		{
			return false;
		}
		int cellValue = m_terrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
		if (!(BlocksManager.Blocks[Terrain.ExtractContents(cellValue)] is IPaintableBlock paintableBlock))
		{
			return false;
		}
		int value = paintableBlock.Paint(m_terrain, cellValue, PaintGunNearBlock.GetCleaner(Terrain.ExtractData(miner.ActiveBlockValue)) ? ((int?)null) : color);
		m_terrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value);
		ForcePaintRegen(cellFace.X, cellFace.Y, cellFace.Z);
		Vector3 position = res.HitPoint();
		Vector3 normal = CellFace.FaceToVector3(cellFace.Face);
		Color color2 = (color.HasValue ? SubsystemPalette.GetColor(m_terrain, color) : new Color(255, 255, 255, 255));
		m_particles.AddParticleSystem(new PaintParticleSystem(m_terrain, position, normal, color2));
		m_audio.PlayRandomSound("Audio/Paint", 0.4f, m_rand.Float(-0.2f, 0.2f), position, 2f, autoDelay: true);
		return true;
	}

	public void Update(float dt)
	{
		ComponentPlayer mainPlayer = Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).MainPlayer;
		if (mainPlayer == null)
		{
			return;
		}
		int num = Terrain.ExtractContents(mainPlayer.ComponentMiner.ActiveBlockValue);
		if (num != 410 && num != 411)
		{
			m_has = false;
			m_hold = false;
			return;
		}
		PlayerInput craftingRecipe = mainPlayer.ComponentInput.PlayerInput;
		if (craftingRecipe.Aim.HasValue && !m_hold)
		{
			if (!m_has)
			{
				m_has = true;
				m_timer = SettingsManager.BuildHoldTimerStart;
				m_hold = true;
			}
		}
		else if (!craftingRecipe.Aim.HasValue)
		{
			m_has = false;
			m_hold = false;
		}
		if (m_hold)
		{
			m_timer -= dt;
			if (m_timer <= 0f)
			{
				OnUse(craftingRecipe.Aim.Value, mainPlayer.ComponentMiner);
				m_timer = SettingsManager.BuildHoldTimerInterval;
			}
		}
	}

	private static bool IsSolid(int value, float dist)
	{
		Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
		if (block.IsCollidable)
		{
			return !(block is CrossBlock);
		}
		return false;
	}

	static SubsystemPaintGunBlockBehavior()
	{
		m_filter = IsSolid;
	}

	private bool PaintCreatures(Vector3 start, Vector3 direction, float length, int? color, ComponentMiner miner)
	{
		Vector3 vector = Vector3.Normalize(direction);
		float num = 0.5f;
		Vector3 rgb = Vector3.One;
		if (color.HasValue)
		{
			Color color2 = SubsystemPalette.GetColor(m_terrain, color);
			rgb = new Vector3((float)(int)color2.R / 255f, (float)(int)color2.G / 255f, (float)(int)color2.B / 255f);
		}
		bool result = false;
		foreach (Entity entity in Project.Entities)
		{
			ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
			if (componentCreature == null)
			{
				continue;
			}
			ComponentCreatureModel componentCreatureModel = entity.FindComponent<ComponentCreatureModel>();
			if (componentCreatureModel == null)
			{
				continue;
			}
			BoundingBox boundingBox = componentCreature.ComponentBody.BoundingBox;
			for (float num2 = 0f; num2 <= length; num2 += num)
			{
				Vector3 p = start + vector * num2;
				if (boundingBox.Contains(p))
				{
					componentCreatureModel.ApplyPaint(rgb);
					m_audio.PlayRandomSound("Audio/Paint", 0.4f, m_rand.Float(-0.2f, 0.2f), componentCreature.ComponentBody.Position, 2f, autoDelay: true);
					result = true;
					break;
				}
			}
		}
		return result;
	}

	private void ForcePaintRegen(int x, int y, int z)
	{
		TerrainChunk chunkAtCell = m_terrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null)
		{
			int num = MathUtils.Clamp(y >> 4, 0, 15);
			chunkAtCell.GeneratedSliceContentsHashes[num] = 0;
			m_terrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 0, TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
		}
	}

	private static bool IsPaintingBlockedByPlacement(ComponentMiner miner, int x, int z)
	{
		SubsystemGameInfo subsystemGameInfo = miner.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		bool flag = SubsystemBedrockBlockBehavior.CheckIsInTerritoriy(x, z, out var territoriy);
		if (subsystemGameInfo.WorldSettings.RestrictWorldInteraction && miner.ComponentPlayer != null && !miner.ComponentPlayer.PlayerData.ServerManager && !flag)
		{
			return true;
		}
		if (!flag)
		{
			return false;
		}
		PlayerData playerData = miner.ComponentPlayer.PlayerData;
		if (territoriy.OwnerGuid == playerData.PlayerGUID)
		{
			return false;
		}
		if (playerData.Friends.Contains(territoriy.OwnerGuid))
		{
			return !territoriy.ApplyToFirend;
		}
		return !territoriy.AllowPlace;
	}
}
