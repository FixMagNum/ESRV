using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class StringBlock : Block, IPaintableBlock
{
	public enum StringKind
	{
		Thread,
		Tendon,
		Bark,
		BoiledBark
	}

	public const int Index = 198;

	public BoundingBox[] m_collisionBoxesByFace = new BoundingBox[6];

	public static StringKind GetKind(int data)
	{
		return (StringKind)((data >> 6) & 3);
	}

	public static int SetKind(int data, StringKind kind)
	{
		return (data & -193) | ((int)(kind & StringKind.BoiledBark) << 6);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, StringKind.Thread));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, StringKind.Tendon));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, StringKind.Bark));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, StringKind.BoiledBark));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int componentCreature = (int)GetKind(Terrain.ExtractData(value));
		string flag = LanguageControl.GetBlock($"StringBlock:{componentCreature}", "DisplayName");
		return SubsystemPalette.GetName(subsystemTerrain, GetPaintColor(value), flag);
	}

	public override string GetDescription(int value)
	{
		int kind = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"StringBlock:{kind}", "Description");
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		switch (GetKind(Terrain.ExtractData(value)))
		{
		case StringKind.Tendon:
			return 236;
		case StringKind.Bark:
		case StringKind.BoiledBark:
			return 237;
		default:
			return base.GetFaceTextureSlot(face, value);
		}
	}

	public override void GetDropValues(SubsystemTerrain sst, int oldValue, int newValue, int toolLevel, List<BlockDropValue> drops, out bool showDebris)
	{
		int faceMask = GetFaceMask(Terrain.ExtractData(oldValue));
		int faceMask2 = GetFaceMask(Terrain.ExtractData(newValue));
		StringKind kind = GetKind(Terrain.ExtractData(oldValue));
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0 && (faceMask2 & (1 << i)) == 0)
			{
				num++;
			}
		}
		if (num > 0)
		{
			drops.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, kind)),
				Count = num
			});
		}
		showDebris = num > 0;
	}

	public override string GetCategory(int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == StringKind.BoiledBark)
		{
			return "Food";
		}
		if (GetPaintColor(value).HasValue)
		{
			return "Painted";
		}
		return "Items";
	}

	public static bool FaceExists(int data, int face)
	{
		return (data & (1 << face)) != 0;
	}

	public static int GetFaceMask(int data)
	{
		return data & 0x3F;
	}

	public static int SetFaceMask(int data, int faceMask)
	{
		return (data & -64) | (faceMask & 0x3F);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int? componentPlayer = GetPaintColor(value);
		Color vector = SubsystemPalette.GetColor(environmentData, componentPlayer);
		if (GetKind(Terrain.ExtractData(value)) == StringKind.BoiledBark)
		{
			vector *= new Color(160, 130, 100);
		}
		BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color * vector, isEmissive: false, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int num = CellFace.OppositeFace(raycastResult.CellFace.Face);
		StringKind kind = GetKind(Terrain.ExtractData(value));
		int? color = GetColor(Terrain.ExtractData(value));
		Point3 point = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
		Point3 point2 = new Point3(raycastResult.CellFace.X + point.X, raycastResult.CellFace.Y + point.Y, raycastResult.CellFace.Z + point.Z);
		int cellValue = subsystemTerrain.Terrain.GetCellValue(point2.X, point2.Y, point2.Z);
		int num2 = 1 << num;
		if (Terrain.ExtractContents(cellValue) == BlockIndex)
		{
			int data = Terrain.ExtractData(cellValue);
			int? color2 = GetColor(data);
			if (color != color2)
			{
				if (componentMiner.ComponentPlayer != null)
				{
					string text;
					if (color2.HasValue)
					{
						string name = SubsystemPalette.GetName(subsystemTerrain, color2, "");
						text = string.Format(LanguageControl.Get("StringBlock", "NeedColored"), name);
					}
					else
					{
						text = LanguageControl.Get("StringBlock", "NeedUncolored");
					}
					componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(text, Color.White, blinking: false, playNotificationSound: true);
				}
				return default;
			}
			num2 |= GetFaceMask(data);
		}
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, SetFaceMask(SetColor(SetKind(0, kind), color), num2)),
			CellFace = raycastResult.CellFace
		};
	}

	public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		int data = Terrain.ExtractData(value);
		int num = GetFaceMask(data);
		StringKind kind = GetKind(data);
		int num2 = 0;
		int num3 = -1;
		for (int i = 0; i < 6; i++)
		{
			if ((num & (1 << i)) != 0)
			{
				if (num2 == raycastResult.CollisionBoxIndex)
				{
					num3 = i;
					break;
				}
				num2++;
			}
		}
		if (num3 != -1)
		{
			num &= ~(1 << num3);
		}
		if (num == 0)
		{
			return new BlockPlacementData
			{
				Value = 0,
				CellFace = raycastResult.CellFace
			};
		}
		int data2 = SetFaceMask(SetKind(0, kind), num);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data2),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void Initialize()
	{
		float num = 0.05f;
		float num2 = 0.325f;
		float num3 = 0.675f;
		m_collisionBoxesByFace[0] = new BoundingBox(new Vector3(num2, num2, 1f - num), new Vector3(num3, num3, 1f));
		m_collisionBoxesByFace[1] = new BoundingBox(new Vector3(1f - num, num2, num2), new Vector3(1f, num3, num3));
		m_collisionBoxesByFace[2] = new BoundingBox(new Vector3(num2, num2, 0f), new Vector3(num3, num3, num));
		m_collisionBoxesByFace[3] = new BoundingBox(new Vector3(0f, num2, num2), new Vector3(num, num3, num3));
		m_collisionBoxesByFace[4] = new BoundingBox(new Vector3(num2, 1f - num, num2), new Vector3(num3, 1f, num3));
		m_collisionBoxesByFace[5] = new BoundingBox(new Vector3(num2, 0f, num2), new Vector3(num3, num, num3));
		base.Initialize();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int faceMask = GetFaceMask(Terrain.ExtractData(value));
		if (faceMask == 16 || faceMask == 32 || faceMask == 48 || faceMask == 0)
		{
			return new BoundingBox[1]
			{
				new BoundingBox(new Vector3(0.325f, 0f, 0.325f), new Vector3(0.675f, 1f, 0.675f))
			};
		}
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				num++;
			}
		}
		BoundingBox[] array = new BoundingBox[num];
		int num2 = 0;
		for (int j = 0; j < 6; j++)
		{
			if ((faceMask & (1 << j)) != 0)
			{
				array[num2++] = m_collisionBoxesByFace[j];
			}
		}
		return array;
	}

	public override bool IsInteractive(SubsystemTerrain subsystemTerrain, int value)
	{
		return true;
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		return Terrain.ReplaceData(value, SetColor(data, color));
	}

	public static int? GetColor(int data)
	{
		if ((data & 0x100) != 0)
		{
			return (data >> 9) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -7937) | 0x100 | ((color.Value & 0xF) << 9);
		}
		return data & -7937;
	}

	public override FoodType GetFoodType(int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == StringKind.BoiledBark)
		{
			return FoodType.Grass;
		}
		return base.GetFoodType(value);
	}

	public override float GetNutritionalValue(int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == StringKind.BoiledBark)
		{
			return 0.15f;
		}
		return base.GetNutritionalValue(value);
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		int num = SetKind(0, StringKind.Bark);
		int data = SetKind(0, StringKind.BoiledBark);
		CraftingRecipe craftingRecipe = new CraftingRecipe
		{
			ResultCount = 1,
			ResultValue = Terrain.MakeBlockValue(BlockIndex, 0, data),
			RemainsValue = CraftingRecipesManager.DecodeResult("EmptyBucketBlock"),
			RemainsCount = 1,
			RequiredHeatLevel = 1f,
			Description = LanguageControl.GetBlock("StringBlock", "BoiledBarkRecipe")
		};
		craftingRecipe.Ingredients[0] = CraftingId + ":" + num;
		craftingRecipe.Ingredients[1] = "waterbucket";
		yield return craftingRecipe;
	}
}
