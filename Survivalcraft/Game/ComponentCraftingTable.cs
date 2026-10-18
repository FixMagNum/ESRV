using System;
using System.Globalization;
using Engine;
using Engine.Graphics;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCraftingTable : ComponentInventoryBase, IUpdateable, IDrawable
{
	public int m_craftingGridSize;

	public string[] m_matchedIngredients;

	public CraftingRecipe m_matchedRecipe;

	public SubsystemTerrain m_subsystemTerrain;

	public ComponentBlockEntity m_componentBlockEntity;

	public PrimitivesRenderer3D m_primitivesRenderer;

	public LightSmoother m_lightSmoother = new LightSmoother();

	private bool m_isVisible;

	public int RemainsSlotIndex => SlotsCount - 1;

	public int ResultSlotIndex => SlotsCount - 2;

	public int[] DrawOrders => new int[1] { 10 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int GetSlotCapacity(int slotIndex, int value)
	{
		if (slotIndex < SlotsCount - 2)
		{
			return base.GetSlotCapacity(slotIndex, value);
		}
		return 0;
	}

	public override void AddSlotItems(int slotIndex, int value, int count)
	{
		base.AddSlotItems(slotIndex, value, count);
		UpdateCraftingResult();
	}

	public override int RemoveSlotItems(int slotIndex, int count)
	{
		int num = 0;
		if (slotIndex == ResultSlotIndex)
		{
			if (m_matchedRecipe != null)
			{
				if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
				{
					if (m_slots[RemainsSlotIndex].Count == 0 || m_slots[RemainsSlotIndex].Value == m_matchedRecipe.RemainsValue)
					{
						int num2 = BlocksManager.Blocks[Terrain.ExtractContents(m_matchedRecipe.RemainsValue)].GetMaxStacking(m_matchedRecipe.RemainsValue) - m_slots[RemainsSlotIndex].Count;
						count = MathUtils.Min(count, num2 / m_matchedRecipe.RemainsCount * m_matchedRecipe.ResultCount);
					}
					else
					{
						count = 0;
					}
				}
				count = count / m_matchedRecipe.ResultCount * m_matchedRecipe.ResultCount;
				num = base.RemoveSlotItems(slotIndex, count);
				if (num > 0)
				{
					for (int i = 0; i < 9; i++)
					{
						if (!string.IsNullOrEmpty(m_matchedIngredients[i]))
						{
							int num3 = i % 3 + m_craftingGridSize * (i / 3);
							m_slots[num3].Count = MathUtils.Max(m_slots[num3].Count - num / m_matchedRecipe.ResultCount, 0);
							OnSlotChange(num3);
						}
					}
					if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
					{
						m_slots[RemainsSlotIndex].Value = m_matchedRecipe.RemainsValue;
						m_slots[RemainsSlotIndex].Count += num / m_matchedRecipe.ResultCount * m_matchedRecipe.RemainsCount;
						OnSlotChange(RemainsSlotIndex);
					}
					ComponentPlayer componentPlayer = FindInteractingPlayer();
					if (componentPlayer != null && componentPlayer.PlayerStats != null)
					{
						componentPlayer.PlayerStats.ItemsCrafted += num;
					}
				}
			}
		}
		else
		{
			num = base.RemoveSlotItems(slotIndex, count);
		}
		UpdateCraftingResult();
		return num;
	}

	public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		base.Load(valuesDictionary, idToEntityMap);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
		m_craftingGridSize = (int)MathUtils.Sqrt(SlotsCount - 2);
		UpdateCraftingResult();
	}

	public void UpdateCraftingResult()
	{
		int num = int.MaxValue;
		for (int i = 0; i < m_craftingGridSize; i++)
		{
			for (int j = 0; j < m_craftingGridSize; j++)
			{
				int num2 = i + j * 3;
				int slotIndex = i + j * m_craftingGridSize;
				int slotValue = GetSlotValue(slotIndex);
				int num3 = Terrain.ExtractContents(slotValue);
				int num4 = Terrain.ExtractData(slotValue);
				int slotCount = GetSlotCount(slotIndex);
				if (slotCount > 0)
				{
					Block block = BlocksManager.Blocks[num3];
					m_matchedIngredients[num2] = block.CraftingId + ":" + num4.ToString(CultureInfo.InvariantCulture);
					num = MathUtils.Min(num, slotCount);
				}
				else
				{
					m_matchedIngredients[num2] = null;
				}
			}
		}
		ComponentPlayer componentPlayer = FindInteractingPlayer();
		CraftingRecipe craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(Project.FindSubsystem<SubsystemTerrain>(throwOnError: true), m_matchedIngredients, 0f, componentPlayer);
		if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
		{
			m_matchedRecipe = craftingRecipe;
			m_slots[ResultSlotIndex].Value = craftingRecipe.ResultValue;
			m_slots[ResultSlotIndex].Count = craftingRecipe.ResultCount * num;
		}
		else
		{
			m_matchedRecipe = null;
			m_slots[ResultSlotIndex].Value = 0;
			m_slots[ResultSlotIndex].Count = 0;
		}
		OnSlotChange(ResultSlotIndex);
		if (craftingRecipe != null && !string.IsNullOrEmpty(craftingRecipe.Message))
		{
			componentPlayer?.ComponentGui.DisplaySmallMessage(craftingRecipe.Message, Color.White, blinking: true, playNotificationSound: true);
		}
	}

	public ComponentCraftingTable()
	{
		m_primitivesRenderer = new PrimitivesRenderer3D();
		m_matchedIngredients = new string[9];
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (m_componentBlockEntity == null)
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
		if (num > 1024f)
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
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(val)];
				return block.IsCollidable && !block.IsTransparent;
			});
			if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.Distance * terrainRaycastResult.Value.Distance < num - 0.5f)
			{
				return;
			}
		}
		Vector3 position = new Vector3((float)coordinates.X + 0.5f, coordinates.Y, (float)coordinates.Z + 0.5f);
		int data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z));
		Matrix matrix = Matrix.CreateRotationY((float)CraftingTableBlock.GetRotation(data) * (float)Math.PI / 2f);
		float num2 = ((CraftingTableBlock.GetMode(data) == CraftingTableBlock.CraftingTableMode.Small) ? (-0.55f) : 0f);
		DrawBlockEnvironmentData drawBlockEnvironmentData = new DrawBlockEnvironmentData
		{
			SubsystemTerrain = m_subsystemTerrain,
			Light = m_lightSmoother.SmoothedLightInt,
			Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(coordinates.X, coordinates.Z),
			Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(coordinates.X, coordinates.Z)
		};
		for (int num3 = 0; num3 < 9; num3++)
		{
			if (m_slots[num3].Count > 0)
			{
				float x = (float)(num3 % 3 - 1) * 0.22f;
				float z = (float)(num3 / 3 - 1) * -0.22f;
				int value = m_slots[num3].Value;
				Block obj = BlocksManager.Blocks[Terrain.ExtractContents(value)];
				Vector3 position2 = new Vector3(x, 1.02f + num2, z);
				Matrix matrix2 = (drawBlockEnvironmentData.InWorldMatrix = GetItemMatrix(value, 0f) * Matrix.CreateTranslation(position2) * matrix * Matrix.CreateTranslation(position));
				obj.DrawBlock(m_primitivesRenderer, value, Color.White, 0.15f, ref matrix2, drawBlockEnvironmentData);
			}
		}
		if (m_slots[ResultSlotIndex].Count > 0)
		{
			int value2 = m_slots[ResultSlotIndex].Value;
			Block obj2 = BlocksManager.Blocks[Terrain.ExtractContents(value2)];
			Vector3 position3 = new Vector3(0f, 1.35f + num2, 0f);
			float rotationRadians = (float)(Time.FrameStartTime * 1.5);
			Matrix matrix3 = (drawBlockEnvironmentData.InWorldMatrix = GetItemMatrix(value2, rotationRadians) * Matrix.CreateTranslation(position3) * matrix * Matrix.CreateTranslation(position));
			obj2.DrawBlock(m_primitivesRenderer, value2, Color.White * 0.5f, 0.45f, ref matrix3, drawBlockEnvironmentData);
		}
		m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
	}

	private Matrix GetItemMatrix(int value, float rotationRadians)
	{
		Matrix primaryNullable = Matrix.CreateRotationY(rotationRadians);
		if (SubsystemPickables.IsSpecialItem(value))
		{
			primaryNullable = Matrix.CreateScale(1.1f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(rotationRadians);
		}
		else if (SubsystemPickables.IsBigAss(value))
		{
			primaryNullable = Matrix.CreateScale(3f) * Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(MathUtils.DegToRad(90f)) * Matrix.CreateRotationY(rotationRadians);
		}
		else if (SubsystemPickables.IsHorizontalItem(value))
		{
			primaryNullable = Matrix.CreateRotationX(MathUtils.DegToRad(90f)) * Matrix.CreateRotationZ(0f) * Matrix.CreateRotationY(rotationRadians);
		}
		return primaryNullable;
	}

	public void Update(float dt)
	{
		if (m_componentBlockEntity != null && CommonLib.WorkType != WorkType.Server && m_isVisible)
		{
			m_isVisible = false;
			Point3 coordinates = m_componentBlockEntity.Coordinates;
			Vector3 position = new Vector3((float)coordinates.X + 0.5f, (float)coordinates.Y + 0.5f, (float)coordinates.Z + 0.5f);
			m_lightSmoother.Update(dt, position, m_subsystemTerrain, Project);
		}
	}
}
