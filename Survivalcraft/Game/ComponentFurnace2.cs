using System;
using System.Globalization;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentFurnace2 : ComponentInventoryBase, IUpdateable, IDrawable
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemExplosions m_subsystemExplosions;

	public ComponentBlockEntity m_componentBlockEntity;

	public string[] m_matchedIngredients;

	public float m_fireTimeRemaining;

	public float m_heatLevel;

	public bool m_updateSmeltingRecipe;

	public CraftingRecipe m_smeltingRecipe;

	public float m_smeltingProgress;

	private bool isRun;

	public int m_furnaceSize;

	public PrimitivesRenderer3D m_primitivesRenderer;

	private static readonly Vector2[] m_fuelOffsets = new Vector2[5]
	{
		new Vector2(0f, 0f),
		new Vector2(0.15f, 0.1f),
		new Vector2(-0.1f, -0.15f),
		new Vector2(0.12f, -0.1f),
		new Vector2(-0.15f, 0.12f)
	};

	private static readonly Vector3[] m_outputOffsets = new Vector3[5]
	{
		new Vector3(0f, 0f, 0f),
		new Vector3(-0.12f, 0f, 0.05f),
		new Vector3(0.12f, 0f, 0.05f),
		new Vector3(-0.06f, 0.18f, 0.02f),
		new Vector3(0.06f, 0.18f, 0.02f)
	};

	public LightSmoother m_lightSmoother = new LightSmoother();

	private bool m_isVisible;

	public int RemainsSlotIndex => SlotsCount - 1;

	public int ResultSlotIndex => SlotsCount - 2;

	public int FuelSlotIndex => SlotsCount - 3;

	public float HeatLevel => m_heatLevel;

	public float SmeltingProgress => m_smeltingProgress;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public int[] DrawOrders => new int[1] { 20 };

	public override int GetSlotCapacity(int slotIndex, int value)
	{
		if (slotIndex != FuelSlotIndex)
		{
			return base.GetSlotCapacity(slotIndex, value);
		}
		if (BlocksManager.Blocks[Terrain.ExtractContents(value)].FuelHeatLevel > 0f)
		{
			return base.GetSlotCapacity(slotIndex, value);
		}
		return 0;
	}

	public override void AddSlotItems(int slotIndex, int value, int count)
	{
		m_updateSmeltingRecipe = true;
		base.AddSlotItems(slotIndex, value, count);
	}

	public override int RemoveSlotItems(int slotIndex, int count)
	{
		m_updateSmeltingRecipe = true;
		return base.RemoveSlotItems(slotIndex, count);
	}

	public override void SetSlotValue(int slotIndex, object obj)
	{
		m_updateSmeltingRecipe = true;
		base.SetSlotValue(slotIndex, obj);
	}

	public void Update(float dt)
	{
		Point3 coordinates = m_componentBlockEntity.Coordinates;
		if (m_isVisible)
		{
			m_lightSmoother.Update(dt, new Vector3(coordinates.X, coordinates.Y, coordinates.Z), m_subsystemTerrain, Project);
			m_isVisible = false;
		}
		if (CommonLib.WorkType == WorkType.Client)
		{
			return;
		}
		if (m_heatLevel > 0f)
		{
			m_fireTimeRemaining = MathUtils.Max(0f, m_fireTimeRemaining - dt);
			if (m_fireTimeRemaining == 0f)
			{
				m_heatLevel = 0f;
			}
		}
		if (m_updateSmeltingRecipe)
		{
			m_updateSmeltingRecipe = false;
			float heatLevel = 0f;
			if (m_heatLevel > 0f)
			{
				heatLevel = m_heatLevel;
			}
			else
			{
				Slot slot = m_slots[FuelSlotIndex];
				if (slot.Count > 0)
				{
					int num = Terrain.ExtractContents(slot.Value);
					heatLevel = BlocksManager.Blocks[num].FuelHeatLevel;
				}
			}
			CraftingRecipe craftingRecipe = FindSmeltingRecipe(heatLevel);
			if (craftingRecipe != m_smeltingRecipe)
			{
				m_smeltingRecipe = ((craftingRecipe != null && craftingRecipe.ResultValue != 0) ? craftingRecipe : null);
				m_smeltingProgress = 0f;
			}
		}
		if (m_smeltingRecipe == null)
		{
			m_heatLevel = 0f;
			m_fireTimeRemaining = 0f;
		}
		if (m_smeltingRecipe != null && m_fireTimeRemaining <= 0f)
		{
			Slot slot2 = m_slots[FuelSlotIndex];
			if (slot2.Count > 0)
			{
				int num2 = Terrain.ExtractContents(slot2.Value);
				Block block = BlocksManager.Blocks[num2];
				if (block.GetExplosionPressure(slot2.Value) > 0f)
				{
					slot2.Count = 0;
					m_subsystemExplosions.TryExplodeBlock(coordinates.X, coordinates.Y, coordinates.Z, slot2.Value, m_componentBlockEntity.OwnPlayerData);
				}
				else if (block.FuelHeatLevel > 0f)
				{
					slot2.Count--;
					m_fireTimeRemaining = block.FuelFireDuration;
					m_heatLevel = block.FuelHeatLevel;
				}
				OnSlotChange(FuelSlotIndex);
			}
		}
		if (m_fireTimeRemaining <= 0f)
		{
			m_smeltingRecipe = null;
			m_smeltingProgress = 0f;
		}
		if (m_smeltingRecipe != null)
		{
			m_smeltingProgress = MathUtils.Min(m_smeltingProgress + 0.1f * dt, 1f);
			isRun = true;
			if (m_smeltingProgress >= 1f)
			{
				for (int i = 0; i < m_furnaceSize; i++)
				{
					if (m_slots[i].Count > 0)
					{
						m_slots[i].Count--;
						OnSlotChange(i);
					}
				}
				m_slots[ResultSlotIndex].Value = m_smeltingRecipe.ResultValue;
				m_slots[ResultSlotIndex].Count += m_smeltingRecipe.ResultCount;
				if (m_smeltingRecipe.RemainsValue != 0 && m_smeltingRecipe.RemainsCount > 0)
				{
					m_slots[RemainsSlotIndex].Value = m_smeltingRecipe.RemainsValue;
					m_slots[RemainsSlotIndex].Count += m_smeltingRecipe.RemainsCount;
					OnSlotChange(RemainsSlotIndex);
				}
				OnSlotChange(ResultSlotIndex);
				m_smeltingRecipe = null;
				m_smeltingProgress = 0f;
				m_updateSmeltingRecipe = true;
			}
		}
		TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(coordinates.X, coordinates.Z);
		if (chunkAtCell != null && chunkAtCell.State == TerrainChunkState.Valid)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z);
			m_subsystemTerrain.ChangeCell(coordinates.X, coordinates.Y, coordinates.Z, Terrain.ReplaceContents(cellValue, (m_heatLevel > 0f) ? 304 : 303));
		}
		if (isRun && CommonLib.WorkType != WorkType.Client)
		{
			if (m_smeltingRecipe == null && m_fireTimeRemaining == 0f)
			{
				isRun = false;
				CommonLib.Net.QueuePackage(new ComponentFurnacePackage2(Entity.EntityId, m_fireTimeRemaining, m_smeltingProgress, m_heatLevel));
			}
			else if (Time.PeriodicEvent(0.1, 0.0))
			{
				CommonLib.Net.QueuePackage(new ComponentFurnacePackage2(Entity.EntityId, m_fireTimeRemaining, m_smeltingProgress, m_heatLevel));
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
		m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
		m_furnaceSize = SlotsCount - 3;
		if (m_furnaceSize < 1 || m_furnaceSize > 3)
		{
			throw new InvalidOperationException("Invalid furnace size.");
		}
		m_fireTimeRemaining = valuesDictionary.GetValue<float>("FireTimeRemaining");
		m_heatLevel = valuesDictionary.GetValue<float>("HeatLevel");
		m_updateSmeltingRecipe = true;
	}

	public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		base.Save(valuesDictionary, entityToIdMap);
		valuesDictionary.SetValue("FireTimeRemaining", m_fireTimeRemaining);
		valuesDictionary.SetValue("HeatLevel", m_heatLevel);
	}

	public CraftingRecipe FindSmeltingRecipe(float heatLevel)
	{
		if (heatLevel > 0f)
		{
			for (int i = 0; i < m_furnaceSize; i++)
			{
				int slotValue = GetSlotValue(i);
				int num = Terrain.ExtractContents(slotValue);
				int num2 = Terrain.ExtractData(slotValue);
				if (GetSlotCount(i) > 0)
				{
					Block block = BlocksManager.Blocks[num];
					m_matchedIngredients[i] = block.CraftingId + ":" + num2.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					m_matchedIngredients[i] = null;
				}
			}
			ComponentPlayer componentPlayer = FindInteractingPlayer();
			CraftingRecipe craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(m_subsystemTerrain, m_matchedIngredients, heatLevel, componentPlayer);
			if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
			{
				if (craftingRecipe.RequiredHeatLevel <= 0f)
				{
					craftingRecipe = null;
				}
				if (craftingRecipe != null)
				{
					Slot slot = m_slots[ResultSlotIndex];
					int num3 = Terrain.ExtractContents(craftingRecipe.ResultValue);
					if (slot.Count != 0 && (craftingRecipe.ResultValue != slot.Value || craftingRecipe.ResultCount + slot.Count > BlocksManager.Blocks[num3].MaxStacking))
					{
						craftingRecipe = null;
					}
				}
				if (craftingRecipe != null && craftingRecipe.RemainsValue != 0 && craftingRecipe.RemainsCount > 0)
				{
					if (m_slots[RemainsSlotIndex].Count == 0 || m_slots[RemainsSlotIndex].Value == craftingRecipe.RemainsValue)
					{
						if (BlocksManager.Blocks[Terrain.ExtractContents(craftingRecipe.RemainsValue)].MaxStacking - m_slots[RemainsSlotIndex].Count < craftingRecipe.RemainsCount)
						{
							craftingRecipe = null;
						}
					}
					else
					{
						craftingRecipe = null;
					}
				}
			}
			if (craftingRecipe != null && !string.IsNullOrEmpty(craftingRecipe.Message))
			{
				componentPlayer?.ComponentGui.DisplaySmallMessage(craftingRecipe.Message, Color.White, blinking: true, playNotificationSound: true);
			}
			return craftingRecipe;
		}
		return null;
	}

	public ComponentFurnace2()
	{
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_matchedIngredients = new string[9];
	}

	public int GetActiveBlockID()
	{
		if (m_heatLevel <= 0f)
		{
			return 303;
		}
		return 304;
	}

	public bool TickMoving(float dt)
	{
		float heatLevel = m_heatLevel;
		if (m_heatLevel > 0f)
		{
			m_fireTimeRemaining = MathUtils.Max(0f, m_fireTimeRemaining - dt);
			if (m_fireTimeRemaining == 0f)
			{
				m_heatLevel = 0f;
			}
		}
		if (m_updateSmeltingRecipe)
		{
			m_updateSmeltingRecipe = false;
			float num = ((m_heatLevel > 0f) ? m_heatLevel : 0f);
			if (num <= 0f)
			{
				Slot slot = m_slots[FuelSlotIndex];
				if (slot.Count > 0)
				{
					int num2 = Terrain.ExtractContents(slot.Value);
					num = BlocksManager.Blocks[num2].FuelHeatLevel;
				}
			}
			CraftingRecipe craftingRecipe = FindSmeltingRecipe(num);
			if (craftingRecipe != m_smeltingRecipe)
			{
				m_smeltingRecipe = ((craftingRecipe != null && craftingRecipe.ResultValue != 0) ? craftingRecipe : null);
				m_smeltingProgress = 0f;
			}
		}
		if (m_smeltingRecipe == null)
		{
			m_heatLevel = 0f;
			m_fireTimeRemaining = 0f;
		}
		if (m_smeltingRecipe != null && m_fireTimeRemaining <= 0f)
		{
			Slot slot2 = m_slots[FuelSlotIndex];
			if (slot2.Count > 0)
			{
				int num3 = Terrain.ExtractContents(slot2.Value);
				Block block = BlocksManager.Blocks[num3];
				if (block.FuelHeatLevel > 0f)
				{
					slot2.Count--;
					m_fireTimeRemaining = block.FuelFireDuration;
					m_heatLevel = block.FuelHeatLevel;
					OnSlotChange(FuelSlotIndex);
				}
			}
		}
		if (m_fireTimeRemaining <= 0f)
		{
			m_smeltingRecipe = null;
			m_smeltingProgress = 0f;
		}
		if (m_smeltingRecipe != null)
		{
			m_smeltingProgress = MathUtils.Min(m_smeltingProgress + 0.1f * dt, 1f);
			isRun = true;
			if (m_smeltingProgress >= 1f)
			{
				for (int i = 0; i < m_furnaceSize; i++)
				{
					if (m_slots[i].Count > 0)
					{
						m_slots[i].Count--;
						OnSlotChange(i);
					}
				}
				m_slots[ResultSlotIndex].Value = m_smeltingRecipe.ResultValue;
				m_slots[ResultSlotIndex].Count += m_smeltingRecipe.ResultCount;
				if (m_smeltingRecipe.RemainsValue != 0 && m_smeltingRecipe.RemainsCount > 0)
				{
					m_slots[RemainsSlotIndex].Value = m_smeltingRecipe.RemainsValue;
					m_slots[RemainsSlotIndex].Count += m_smeltingRecipe.RemainsCount;
					OnSlotChange(RemainsSlotIndex);
				}
				OnSlotChange(ResultSlotIndex);
				m_smeltingRecipe = null;
				m_smeltingProgress = 0f;
				m_updateSmeltingRecipe = true;
			}
		}
		bool flag = m_heatLevel > 0f;
		bool flag2 = heatLevel > 0f;
		return flag != flag2;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (SlotsCount < 3)
		{
			return;
		}
		Point3 coordinates = m_componentBlockEntity.Coordinates;
		if (m_subsystemTerrain.Terrain.GetChunkAtCell(coordinates.X, coordinates.Z) == null)
		{
			return;
		}
		Vector3 vector = new Vector3((float)coordinates.X + 0.5f, (float)coordinates.Y + 0.5f, (float)coordinates.Z + 0.5f);
		float num = Vector3.DistanceSquared(camera.ViewPosition, vector);
		if (num > 1600f)
		{
			return;
		}
		m_isVisible = true;
		Vector3 v = Vector3.Normalize(vector - camera.ViewPosition);
		if (Vector3.Dot(camera.ViewDirection, v) < 0.3f)
		{
			return;
		}
		if (num > 9f)
		{
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(camera.ViewPosition, vector, useInteractionBoxes: false, skipAirBlocks: true, (int val, float d) =>
			{
				int num13 = Terrain.ExtractContents(val);
				Block block4 = BlocksManager.Blocks[num13];
				return block4.IsCollidable && !block4.IsTransparent && num13 != 64 && num13 != 65;
			});
			if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.Distance * terrainRaycastResult.Value.Distance < num - 0.5f)
			{
				return;
			}
		}
		float num2 = (float)(((coordinates.X * 374761393) ^ (coordinates.Y * 668265261) ^ (coordinates.Z * 1234567)) % 1000) / 1000f * ((float)Math.PI * 2f);
		Vector3 position = new Vector3((float)coordinates.X + 0.5f, coordinates.Y, (float)coordinates.Z + 0.5f);
		Matrix matrix = Matrix.CreateRotationY((float)Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z)) * (float)Math.PI / 2f + (float)Math.PI);
		DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData();
		drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
		drawBlockEnvironmentData.Light = m_lightSmoother.SmoothedLightInt;
		drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(coordinates.X, coordinates.Z);
		drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(coordinates.X, coordinates.Z);
		if (m_slots[FuelSlotIndex].Count > 0)
		{
			int value = m_slots[FuelSlotIndex].Value;
			float num3 = m_slots[FuelSlotIndex].Count;
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
			int num4 = MathUtils.Clamp((int)MathUtils.Ceiling(num3 / 20f), 1, 5);
			for (int num5 = 0; num5 < num4; num5++)
			{
				Vector3 position2 = new Vector3(m_fuelOffsets[num5].X, 0.02f, m_fuelOffsets[num5].Y);
				Matrix matrix2 = (drawBlockEnvironmentData.InWorldMatrix = GetItemMatrix(value, (float)((double)num5 * 1234.5) + num2) * Matrix.CreateTranslation(position2) * matrix * Matrix.CreateTranslation(position));
				block.DrawBlock(m_primitivesRenderer, value, Color.White, 0.35f, ref matrix2, drawBlockEnvironmentData);
			}
		}
		for (int num6 = 0; num6 < m_furnaceSize; num6++)
		{
			if (m_slots[num6].Count > 0)
			{
				int value2 = m_slots[num6].Value;
				Block obj = BlocksManager.Blocks[Terrain.ExtractContents(value2)];
				float x = ((m_furnaceSize > 1) ? (-0.15f + (float)num6 * 0.3f) : 0f);
				Vector3 position3 = new Vector3(x, 0.55f, 0f);
				float rotationRadians = ((m_heatLevel > 0f) ? ((float)(Time.FrameStartTime * 0.75)) : 0f) + num2;
				Matrix matrix3 = (drawBlockEnvironmentData.InWorldMatrix = GetItemMatrix(value2, rotationRadians) * Matrix.CreateTranslation(position3) * matrix * Matrix.CreateTranslation(position));
				obj.DrawBlock(m_primitivesRenderer, value2, Color.White, 0.5f, ref matrix3, drawBlockEnvironmentData);
			}
		}
		if (m_slots[ResultSlotIndex].Count > 0)
		{
			int value3 = m_slots[ResultSlotIndex].Value;
			float num7 = m_slots[ResultSlotIndex].Count;
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(value3)];
			int num8 = MathUtils.Clamp((int)MathUtils.Ceiling(num7 / 20f), 1, 5);
			for (int num9 = 0; num9 < num8; num9++)
			{
				Vector3 vector2 = m_outputOffsets[num9];
				Vector3 position4 = new Vector3(0.35f + vector2.X, 0.07f + vector2.Y + (float)num9 * 0.002f, -0.72f + vector2.Z);
				Matrix matrix4 = (drawBlockEnvironmentData.InWorldMatrix = GetItemMatrix(value3, (float)((double)num9 * 999.0) + num2) * Matrix.CreateTranslation(position4) * matrix * Matrix.CreateTranslation(position));
				block2.DrawBlock(m_primitivesRenderer, value3, Color.White, 0.35f, ref matrix4, drawBlockEnvironmentData);
			}
		}
		if (m_slots[RemainsSlotIndex].Count > 0)
		{
			int value4 = m_slots[RemainsSlotIndex].Value;
			float num10 = m_slots[RemainsSlotIndex].Count;
			Block block3 = BlocksManager.Blocks[Terrain.ExtractContents(value4)];
			int num11 = MathUtils.Clamp((int)MathUtils.Ceiling(num10 / 20f), 1, 5);
			for (int num12 = 0; num12 < num11; num12++)
			{
				Vector3 vector3 = m_outputOffsets[num12];
				Vector3 position5 = new Vector3(-0.35f + vector3.X, 0.07f + vector3.Y + (float)num12 * 0.002f, -0.72f + vector3.Z);
				Matrix matrix5 = (drawBlockEnvironmentData.InWorldMatrix = GetItemMatrix(value4, (float)((double)num12 * 777.0) + 0.5f + num2) * Matrix.CreateTranslation(position5) * matrix * Matrix.CreateTranslation(position));
				block3.DrawBlock(m_primitivesRenderer, value4, Color.White, 0.3f, ref matrix5, drawBlockEnvironmentData);
			}
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	public override void OnEntityAdded()
	{
		base.OnEntityAdded();
	}

	public override void OnEntityRemoved()
	{
		base.OnEntityRemoved();
	}

	private Matrix GetItemMatrix(int value, float rotationRadians)
	{
		Matrix result = Matrix.CreateRotationY(rotationRadians);
		if (SubsystemPickables.IsSpecialItem(value))
		{
			result = Matrix.CreateScale(1.1f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(rotationRadians);
		}
		else if (SubsystemPickables.IsBigAss(value))
		{
			result = Matrix.CreateScale(3f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(rotationRadians);
		}
		else if (SubsystemPickables.IsHorizontalItem(value))
		{
			result = Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(0f) * Matrix.CreateRotationY(rotationRadians);
		}
		return result;
	}
}
