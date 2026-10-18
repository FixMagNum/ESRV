using System;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemBucketBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemParticles m_subsystemParticles;

	public Random m_random = new Random();

	public SubsystemGameInfo m_subsystemGameInfo;

	public override int[] HandledBlocks => new int[11]
	{
		90, 91, 93, 110, 245, 251, 252, 129, 128, 352,
		392
	};

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		IInventory num = componentMiner.Inventory;
		int nutritionalValue = componentMiner.ActiveBlockValue;
		int sicknessProbability = Terrain.ExtractContents(nutritionalValue);
		int data = Terrain.ExtractData(nutritionalValue);
		switch (sicknessProbability)
		{
		case 90:
		case 91:
		case 93:
		{
			int num3 = 0;
			int contents = 0;
			bool flag = false;
			switch (sicknessProbability)
			{
			case 91:
				num3 = 18;
				contents = 91;
				flag = true;
				break;
			case 93:
				num3 = 92;
				contents = 93;
				flag = true;
				break;
			}
			if (flag)
			{
				float level = FocalorsFluidBlock.GetLevel(data);
				float num4 = 1f - level;
				if (num4 < 0.99f && componentMiner.Raycast(ray, RaycastMode.Gathering, raycastTerrain: false) is TerrainRaycastResult terrainRaycastResult2)
				{
					int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(terrainRaycastResult2.CellFace.X, terrainRaycastResult2.CellFace.Y, terrainRaycastResult2.CellFace.Z);
					int num5 = Terrain.ExtractContents(cellValue2);
					int data2 = Terrain.ExtractData(cellValue2);
					if (num5 == num3)
					{
						float level2 = FocalorsFluidBlock.GetLevel(data2);
						float num6 = 1f - level2;
						float num7 = Math.Min(1f - num4, num6);
						if (num7 > 0.001f)
						{
							float num8 = num4 + num7;
							if (num8 > 0.99f)
							{
								num8 = 1f;
							}
							int data3 = FocalorsFluidBlock.SetLevel(0, 1f - num8);
							int newItemValue = Terrain.MakeBlockValue(contents, 0, data3);
							if (ReplaceBucketInInventory(num, newItemValue, componentMiner))
							{
								float num9 = num6 - num7;
								if (num9 <= 0.01f)
								{
									SubsystemTerrain.DestroyCell(0, terrainRaycastResult2.CellFace.X, terrainRaycastResult2.CellFace.Y, terrainRaycastResult2.CellFace.Z, 0, noDrop: false, noParticleSystem: false);
								}
								else
								{
									int data4 = FocalorsFluidBlock.SetLevel(data2, 1f - num9);
									int value2 = Terrain.ReplaceData(cellValue2, data4);
									SubsystemTerrain.ChangeCell(terrainRaycastResult2.CellFace.X, terrainRaycastResult2.CellFace.Y, terrainRaycastResult2.CellFace.Z, value2);
								}
								m_subsystemAudio.PlaySound("Audio/BucketSplash", 1f, 0f, ray.Position, 2f, autoDelay: true);
								return true;
							}
						}
					}
				}
			}
			if (flag)
			{
				TerrainRaycastResult? terrainRaycastResult3 = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction, raycastTerrain: false);
				if (terrainRaycastResult3.HasValue)
				{
					int data5 = FocalorsFluidBlock.SetLevel(0, FocalorsFluidBlock.GetLevel(data));
					data5 = FocalorsFluidBlock.SetIsTop(data5, isTop: true);
					int value3 = Terrain.MakeBlockValue(num3, 0, data5);
					if (componentMiner.Place(terrainRaycastResult3.Value, value3))
					{
						int newItemValue2 = Terrain.MakeBlockValue(90);
						ReplaceBucketInInventory(num, newItemValue2, componentMiner);
						m_subsystemAudio.PlaySound("Audio/BucketSplash", 1f, 0f, ray.Position, 2f, autoDelay: true);
						return true;
					}
				}
				return false;
			}
			if (sicknessProbability == 90)
			{
				object obj = componentMiner.Raycast(ray, RaycastMode.Gathering, raycastTerrain: false);
				object obj2;
				if (obj is TerrainRaycastResult terrainRaycastResult4)
				{
					int cellValue3 = SubsystemTerrain.Terrain.GetCellValue(terrainRaycastResult4.CellFace.X, terrainRaycastResult4.CellFace.Y, terrainRaycastResult4.CellFace.Z);
					int num10 = Terrain.ExtractContents(cellValue3);
					int data6 = Terrain.ExtractData(cellValue3);
					Block block = BlocksManager.Blocks[num10];
					int num11 = 0;
					if (block is QuicksandBlock && FluidBlock.GetLevel(data6) == 0)
					{
						int data7 = ((QuicksandBlock.GetKind(data6) == QuicksandBlock.QuicksandKind.Mud) ? 1 : 0);
						int newItemValue3 = Terrain.MakeBlockValue(392, 0, data7);
						if (ReplaceBucketInInventory(num, newItemValue3, componentMiner))
						{
							SubsystemTerrain.DestroyCell(0, terrainRaycastResult4.CellFace.X, terrainRaycastResult4.CellFace.Y, terrainRaycastResult4.CellFace.Z, 0, noDrop: false, noParticleSystem: false);
							return true;
						}
					}
					else if (block is FocalorsWaterBlock || num10 == 18)
					{
						num11 = 91;
					}
					else if (block is FocalorsMagmaBlock || num10 == 92)
					{
						num11 = 93;
					}
					if (num11 != 0)
					{
						float level3 = FocalorsFluidBlock.GetLevel(data6);
						if (1f - level3 > 0.01f)
						{
							int data8 = FocalorsFluidBlock.SetLevel(0, level3);
							int newItemValue4 = Terrain.MakeBlockValue(num11, 0, data8);
							if (ReplaceBucketInInventory(num, newItemValue4, componentMiner))
							{
								SubsystemTerrain.DestroyCell(0, terrainRaycastResult4.CellFace.X, terrainRaycastResult4.CellFace.Y, terrainRaycastResult4.CellFace.Z, 0, noDrop: false, noParticleSystem: false);
								m_subsystemAudio.PlaySound("Audio/BucketSplash", 1f, 0f, ray.Position, 2f, autoDelay: true);
								return true;
							}
						}
						return true;
					}
				}
				else if ((obj2 = obj) is BodyRaycastResult)
				{
					ComponentUdder componentUdder = ((BodyRaycastResult)obj2).ComponentBody.Entity.FindComponent<ComponentUdder>();
					if (componentUdder != null && componentUdder.Milk(componentMiner))
					{
						int newItemValue5 = Terrain.MakeBlockValue(110);
						if (ReplaceBucketInInventory(num, newItemValue5, componentMiner))
						{
							m_subsystemAudio.PlaySound("Audio/Milked", 1f, 0f, ray.Position, 2f, autoDelay: true);
						}
						return true;
					}
				}
			}
			return false;
		}
		case 392:
		{
			TerrainRaycastResult? terrainRaycastResult5 = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction, raycastTerrain: false);
			if (terrainRaycastResult5.HasValue)
			{
				QuicksandBlock.QuicksandKind kind = ((Terrain.ExtractData(nutritionalValue) == 1) ? QuicksandBlock.QuicksandKind.Mud : QuicksandBlock.QuicksandKind.Sand);
				int data9 = 0;
				data9 = FluidBlock.SetLevel(data9, 0);
				data9 = FluidBlock.SetIsTop(data9, isTop: true);
				data9 = QuicksandBlock.SetKind(data9, kind);
				data9 |= 0x20;
				int value4 = Terrain.MakeBlockValue(391, 0, data9);
				if (componentMiner.Place(terrainRaycastResult5.Value, value4))
				{
					ReplaceBucketInInventory(num, Terrain.MakeBlockValue(90), componentMiner);
					m_subsystemAudio.PlaySound("Audio/BucketSplash", 1f, 0f, ray.Position, 2f, autoDelay: true);
					return true;
				}
			}
			return false;
		}
		case 128:
		case 129:
		{
			TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging, raycastTerrain: false);
			if (terrainRaycastResult.HasValue)
			{
				CellFace cellFace = terrainRaycastResult.Value.CellFace;
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				int num2 = Terrain.ExtractContents(cellValue);
				if (BlocksManager.Blocks[num2] is IPaintableBlock paintableBlock)
				{
					int? color = ((sicknessProbability == 128) ? ((int?)null) : new int?(PaintBucketBlock.GetColor(data)));
					Color color2 = (color.HasValue ? SubsystemPalette.GetColor(SubsystemTerrain, color.Value) : new Color(128, 128, 128, 128));
					int value = paintableBlock.Paint(SubsystemTerrain, cellValue, color);
					SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value);
					ForcePaintRegen(cellFace.X, cellFace.Y, cellFace.Z);
					componentMiner.DamageActiveTool(1);
					m_subsystemAudio.PlayRandomSound("Audio/Paint", 0.4f, m_random.Float(-0.1f, 0.1f), componentMiner.ComponentCreature.ComponentBody.Position, 2f, autoDelay: true);
					m_subsystemParticles.AddParticleSystem(new PaintParticleSystem(SubsystemTerrain, terrainRaycastResult.Value.HitPoint(), CellFace.FaceToVector3(cellFace.Face), color2));
					return true;
				}
			}
			return false;
		}
		default:
			return false;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	private void ForcePaintRegen(int x, int y, int z)
	{
		TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
		if (chunkAtCell != null)
		{
			int num = MathUtils.Clamp(y >> 4, 0, 15);
			chunkAtCell.GeneratedSliceContentsHashes[num] = 0;
			SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 0, TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: false);
		}
	}

	private bool ReplaceBucketInInventory(IInventory inventory, int newItemValue, ComponentMiner componentMiner)
	{
		if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
		{
			return true;
		}
		int activeSlotIndex = inventory.ActiveSlotIndex;
		int slotCount = inventory.GetSlotCount(activeSlotIndex);
		if (slotCount == 1)
		{
			inventory.RemoveNetSlotItems(activeSlotIndex, 1);
			inventory.AddNetSlotItems(activeSlotIndex, newItemValue, 1);
			return true;
		}
		if (slotCount <= 1)
		{
			return false;
		}
		int num = ComponentInventoryBase.FindAcquireSlotForItem(inventory, newItemValue);
		if (num >= 0)
		{
			inventory.RemoveNetSlotItems(activeSlotIndex, 1);
			inventory.AddNetSlotItems(num, newItemValue, 1);
			return true;
		}
		componentMiner?.ComponentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get("ComponentVitalStats", "inventoryfull"), Color.Yellow, blinking: true, playNotificationSound: false);
		return false;
	}
}
