using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CarpetBlock : CubeBlock, IPaintableBlock
{
	public const int Index = 208;

	public new static string fName;

	public BoundingBox[] m_collisionBoxesByFace = new BoundingBox[6];

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		return true;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		int color = GetColor(data);
		Color fabricColor = SubsystemPalette.GetFabricColor(generator, color);
		int num = GetFaceMask(data);
		if (num == 0)
		{
			num = 16;
		}
		for (int i = 0; i < 6; i++)
		{
			if ((num & (1 << i)) != 0)
			{
				int mountingFace = i;
				switch (i)
				{
				case 0:
					mountingFace = 2;
					break;
				case 1:
					mountingFace = 3;
					break;
				case 2:
					mountingFace = 0;
					break;
				case 3:
					mountingFace = 1;
					break;
				}
				generator.GenerateFlatVertices(this, value, x, y, z, fabricColor, mountingFace, 0.0625f, geometry.OpaqueSubsetsByFace);
			}
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int modelMesh = Terrain.ExtractData(value);
		color *= SubsystemPalette.GetFabricColor(environmentData, GetColor(modelMesh));
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size, 0.0625f * size, size), ref matrix, color, color, environmentData);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		for (int i = 0; i < 16; i++)
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
		}
		for (int i = 0; i < 16; i++)
		{
			int data = SetColor(0, i);
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetMaterial(data, 2));
		}
		for (int i = 0; i < 16; i++)
		{
			int data2 = SetColor(0, i);
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetMaterial(data2, 1));
		}
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int num = GetFaceMask(Terrain.ExtractData(oldValue));
		int faceMask = GetFaceMask(Terrain.ExtractData(newValue));
		int color = GetColor(Terrain.ExtractData(oldValue));
		int num2 = 0;
		for (int i = 0; i < 6; i++)
		{
			bool flag = (num & (1 << i)) != 0;
			bool flag2 = (faceMask & (1 << i)) != 0;
			if (flag && !flag2)
			{
				num2++;
			}
		}
		if (num2 > 0)
		{
			int material = GetMaterial(Terrain.ExtractData(oldValue));
			int data = SetMaterial(SetColor(0, color), material);
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
				Count = num2
			});
		}
		showDebris = num2 > 0;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int data = GetFaceMask(Terrain.ExtractData(value));
		int focalorsWaterBlock = 0;
		for (int topColor = 0; topColor < 6; topColor++)
		{
			if ((data & (1 << topColor)) != 0)
			{
				focalorsWaterBlock++;
			}
		}
		BoundingBox[] num = new BoundingBox[focalorsWaterBlock];
		int sideColor = 0;
		for (int i = 0; i < 6; i++)
		{
			if ((data & (1 << i)) != 0)
			{
				num[sideColor++] = m_collisionBoxesByFace[i];
			}
		}
		return num;
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int data = Terrain.ExtractData(value);
		int material = GetMaterial(data);
		string text = LanguageControl.GetBlock(fName + ":0", "DisplayName");
		switch (material)
		{
		case 1:
			text = LanguageControl.Get("CarpetBlock", "FurPrefix") + " " + text;
			break;
		case 2:
			text = LanguageControl.Get("CarpetBlock", "LeatherPrefix") + " " + text;
			break;
		}
		return SubsystemPalette.GetName(subsystemTerrain, GetColor(data), text);
	}

	public static int GetColor(int data)
	{
		return data & 0xF;
	}

	public static int SetColor(int data, int color)
	{
		return (data & -16) | (color & 0xF);
	}

	static CarpetBlock()
	{
		fName = "CarpetBlock";
	}

	public override void Initialize()
	{
		float num = 0.0625f;
		m_collisionBoxesByFace[0] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, num));
		m_collisionBoxesByFace[1] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(num, 1f, 1f));
		m_collisionBoxesByFace[2] = new BoundingBox(new Vector3(0f, 0f, 1f - num), new Vector3(1f, 1f, 1f));
		m_collisionBoxesByFace[3] = new BoundingBox(new Vector3(1f - num, 0f, 0f), new Vector3(1f, 1f, 1f));
		m_collisionBoxesByFace[4] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, num, 1f));
		m_collisionBoxesByFace[5] = new BoundingBox(new Vector3(0f, 1f - num, 0f), new Vector3(1f, 1f, 1f));
		base.Initialize();
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Point3 voltageLevel = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
		int face = raycastResult.CellFace.X + voltageLevel.X;
		int data = raycastResult.CellFace.Y + voltageLevel.Y;
		int z = raycastResult.CellFace.Z + voltageLevel.Z;
		int cellValue = subsystemTerrain.Terrain.GetCellValue(face, data, z);
		int num = Terrain.ExtractContents(cellValue);
		int data2 = Terrain.ExtractData(cellValue);
		int face2 = raycastResult.CellFace.Face;
		if (num == BlockIndex)
		{
			int mask = GetFaceMask(data2) | (1 << face2);
			int color = GetColor(data2);
			int material = GetMaterial(data2);
			int data3 = SetFaceMask(SetMaterial(SetColor(0, color), material), mask);
			return new BlockPlacementData
			{
				Value = Terrain.MakeBlockValue(BlockIndex, 0, data3),
				CellFace = raycastResult.CellFace
			};
		}
		int mask2 = 1 << face2;
		int color2 = GetColor(Terrain.ExtractData(value));
		int material2 = GetMaterial(Terrain.ExtractData(value));
		int data4 = SetFaceMask(SetMaterial(SetColor(0, color2), material2), mask2);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data4),
			CellFace = raycastResult.CellFace
		};
	}

	public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		int data = Terrain.ExtractData(value);
		int faceMask = GetFaceMask(data);
		int num = 0;
		int num2 = -1;
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				if (num == raycastResult.CollisionBoxIndex)
				{
					num2 = i;
					break;
				}
				num++;
			}
		}
		if (num2 == -1)
		{
			return base.GetDigValue(subsystemTerrain, componentMiner, value, toolValue, raycastResult);
		}
		int num3 = faceMask & ~(1 << num2);
		if (num3 == 0)
		{
			return new BlockPlacementData
			{
				Value = 0,
				CellFace = raycastResult.CellFace
			};
		}
		int color = GetColor(data);
		int material = GetMaterial(data);
		int data2 = SetFaceMask(SetMaterial(SetColor(0, color), material), num3);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data2),
			CellFace = raycastResult.CellFace
		};
	}

	public static int GetFaceMask(int data)
	{
		return (data >> 4) & 0x3F;
	}

	public static int SetFaceMask(int data, int mask)
	{
		return (data & -1009) | ((mask & 0x3F) << 4);
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain subsystemTerrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		return Terrain.ReplaceData(value, SetColor(data, color ?? 0));
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		string furIngredient = "fur:2";
		string leatherIngredient = "leather:2";
		for (int i = 0; i < 16; i++)
		{
			int baseData = SetColor(0, i);
			int data = SetMaterial(baseData, 1);
			int leatherData = SetMaterial(baseData, 2);
			CraftingRecipe craftingRecipe = new CraftingRecipe();
			craftingRecipe.ResultCount = 1;
			craftingRecipe.ResultValue = Terrain.MakeBlockValue(BlockIndex, 0, data);
			craftingRecipe.RequiredHeatLevel = 0f;
			craftingRecipe.Description = LanguageControl.GetBlock("CarpetBlock", "CRDescription");
			craftingRecipe.Ingredients = new string[9]
			{
				CraftingId + ":" + baseData,
				furIngredient,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			yield return craftingRecipe;
			craftingRecipe = new CraftingRecipe();
			craftingRecipe.ResultCount = 1;
			craftingRecipe.ResultValue = Terrain.MakeBlockValue(BlockIndex, 0, leatherData);
			craftingRecipe.RequiredHeatLevel = 0f;
			craftingRecipe.Description = LanguageControl.GetBlock("CarpetBlock", "CRDescription");
			craftingRecipe.Ingredients = new string[9]
			{
				CraftingId + ":" + baseData,
				leatherIngredient,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			yield return craftingRecipe;
		}
	}

	public override bool IsInteractive(SubsystemTerrain subsystemTerrain, int value)
	{
		return true;
	}

	public static int GetMaterial(int data)
	{
		return (data >> 10) & 3;
	}

	public static int SetMaterial(int data, int material)
	{
		return (data & -3073) | ((material & 3) << 10);
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		return GetMaterial(Terrain.ExtractData(value)) switch
		{
			1 => 60, 
			2 => 77, 
			_ => 38, 
		};
	}
}
