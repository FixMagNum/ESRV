using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using XmlUtilities;

namespace Game;

public class ClothingBlock : Block
{
	public const int Index = 203;

	public static DynamicArray<ClothingData> m_clothingData;

	public BlockMesh m_innerMesh;

	public BlockMesh m_outerMesh;

	public static Matrix[] m_slotTransforms;

	public void LoadClothingData(XElement item)
	{
		if (item.Name.LocalName != "ClothingData")
		{
			foreach (XElement item2 in item.Elements())
			{
				LoadClothingData(item2);
			}
			return;
		}
		int attributeValue = XmlUtils.GetAttributeValue<int>(item, "Index");
		XAttribute xAttribute = item.Attribute("DisplayName");
		string displayName;
		if (xAttribute != null)
		{
			string value = xAttribute.Value;
			if (value.StartsWith("[") && value.EndsWith("]"))
			{
				string block = LanguageControl.GetBlock(value.Substring(1, value.Length - 2), "DisplayName");
				displayName = ((!string.IsNullOrEmpty(block)) ? block : value);
			}
			else
			{
				displayName = value;
			}
		}
		else
		{
			displayName = LanguageControl.GetBlock($"{GetType().Name}:{attributeValue}", "DisplayName");
		}
		XAttribute xAttribute2 = item.Attribute("Description");
		string description;
		if (xAttribute2 != null)
		{
			string value2 = xAttribute2.Value;
			if (value2.StartsWith("[") && value2.EndsWith("]"))
			{
				string block2 = LanguageControl.GetBlock(value2.Substring(1, value2.Length - 2), "Description");
				description = ((!string.IsNullOrEmpty(block2)) ? block2 : value2);
			}
			else
			{
				description = value2;
			}
		}
		else
		{
			description = LanguageControl.GetBlock($"{GetType().Name}:{attributeValue}", "Description");
		}
		ClothingData value3 = new ClothingData
		{
			Index = attributeValue,
			DisplayIndex = DisplayOrder,
			DisplayName = displayName,
			Description = description,
			Slot = XmlUtils.GetAttributeValue<ClothingSlot>(item, "Slot"),
			ArmorProtection = XmlUtils.GetAttributeValue<float>(item, "ArmorProtection"),
			Sturdiness = XmlUtils.GetAttributeValue<float>(item, "Sturdiness"),
			Insulation = XmlUtils.GetAttributeValue<float>(item, "Insulation"),
			MovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item, "MovementSpeedFactor"),
			SteedMovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item, "SteedMovementSpeedFactor"),
			DensityModifier = XmlUtils.GetAttributeValue<float>(item, "DensityModifier"),
			IsOuter = XmlUtils.GetAttributeValue<bool>(item, "IsOuter"),
			CanBeDyed = XmlUtils.GetAttributeValue<bool>(item, "CanBeDyed"),
			Layer = XmlUtils.GetAttributeValue<int>(item, "Layer"),
			PlayerLevelRequired = XmlUtils.GetAttributeValue<int>(item, "PlayerLevelRequired"),
			Texture = ContentManager.Get<Texture2D>(XmlUtils.GetAttributeValue<string>(item, "TextureName")),
			ImpactSoundsFolder = XmlUtils.GetAttributeValue<string>(item, "ImpactSoundsFolder"),
			MinerSpeed = XmlUtils.GetAttributeValue(item, "MinerSpeed", 1f),
			Shade = XmlUtils.GetAttributeValue(item, "Shade", 0f),
			Weight = XmlUtils.GetAttributeValue(item, "Weight", 0f),
			EmitsLight = XmlUtils.GetAttributeValue<bool>(item, "EmitsLight"),
			ReplacementIndex = XmlUtils.GetAttributeValue(item, "ReplacementIndex", -1),
			IsExplodes = XmlUtils.GetAttributeValue<bool>(item, "IsExplodes")
		};
		if (attributeValue >= m_clothingData.Count)
		{
			m_clothingData.Count = attributeValue + 1;
		}
		m_clothingData[attributeValue] = value3;
		foreach (XElement item3 in item.Elements())
		{
			LoadClothingData(item3);
		}
	}

	public override void Initialize()
	{
		XElement item = ContentManager.Get<XElement>("Clothes");
		LoadClothingData(item);
		Model playerModel = CharacterSkinsManager.GetPlayerModel(PlayerClass.Male);
		Matrix[] array = new Matrix[playerModel.Bones.Count];
		playerModel.CopyAbsoluteBoneTransformsTo(array);
		int index = playerModel.FindBone("Hand1").Index;
		int index2 = playerModel.FindBone("Hand2").Index;
		array[index] = Matrix.CreateRotationY(0.1f) * array[index];
		array[index2] = Matrix.CreateRotationY(-0.1f) * array[index2];
		m_innerMesh = new BlockMesh();
		foreach (ModelMesh mesh in playerModel.Meshes)
		{
			Matrix matrix = array[mesh.ParentBone.Index];
			foreach (ModelMeshPart meshPart in mesh.MeshParts)
			{
				Color color = Color.White * 0.8f;
				color.A = byte.MaxValue;
				m_innerMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_innerMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, color);
			}
		}
		Model outerClothingModel = CharacterSkinsManager.GetOuterClothingModel(PlayerClass.Male);
		Matrix[] array2 = new Matrix[outerClothingModel.Bones.Count];
		outerClothingModel.CopyAbsoluteBoneTransformsTo(array2);
		int index3 = outerClothingModel.FindBone("Leg1").Index;
		int index4 = outerClothingModel.FindBone("Leg2").Index;
		array2[index3] = Matrix.CreateTranslation(-0.02f, 0f, 0f) * array2[index3];
		array2[index4] = Matrix.CreateTranslation(0.02f, 0f, 0f) * array2[index4];
		m_outerMesh = new BlockMesh();
		foreach (ModelMesh mesh2 in outerClothingModel.Meshes)
		{
			Matrix matrix2 = array2[mesh2.ParentBone.Index];
			foreach (ModelMeshPart meshPart2 in mesh2.MeshParts)
			{
				Color color2 = Color.White * 0.8f;
				color2.A = byte.MaxValue;
				m_outerMesh.AppendModelMeshPart(meshPart2, matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_outerMesh.AppendModelMeshPart(meshPart2, matrix2, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, color2);
			}
		}
		base.Initialize();
		Durability = 15;
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int waterloggable = Terrain.ExtractData(value);
		ClothingData clothingData = GetClothingData(waterloggable);
		int data = GetClothingColor(waterloggable);
		string flag = clothingData.DisplayName;
		if (data != 0)
		{
			return SubsystemPalette.GetName(subsystemTerrain, data, flag);
		}
		return flag;
	}

	public override string GetDescription(int value)
	{
		int num = Terrain.ExtractData(value);
		return GetClothingData(num).Description;
	}

	public override string GetCategory(int value)
	{
		if (GetClothingColor(Terrain.ExtractData(value)) == 0)
		{
			return base.GetCategory(value);
		}
		return "Dyed";
	}

	public override int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 8) & 0xF;
	}

	public override int SetDamage(int value, int damage)
	{
		int num = Terrain.ExtractData(value);
		num = (num & -3841) | ((damage & 0xF) << 8);
		return Terrain.ReplaceData(value, num);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		IEnumerable<ClothingData> enumerable = m_clothingData.OrderBy((ClothingData cd) => cd.DisplayIndex);
		foreach (ClothingData clothingData in enumerable)
		{
			if (clothingData != null)
			{
				int colorsCount = ((!clothingData.CanBeDyed) ? 1 : 16);
				int color = 0;
				while (color < colorsCount)
				{
					int data = SetClothingColor(SetClothingIndex(0, clothingData.Index), color);
					yield return Terrain.MakeBlockValue(203, 0, data);
					int num = color + 1;
					color = num;
				}
			}
		}
	}

	public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, ComponentPlayer player)
	{
		if (heatLevel < 1f)
		{
			return null;
		}
		List<string> list = ingredients.Where((string i) => !string.IsNullOrEmpty(i)).ToList();
		if (list.Count == 2)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (string item in list)
			{
				CraftingRecipesManager.DecodeIngredient(item, out var craftingId, out var data);
				if (craftingId == BlocksManager.Blocks[203].CraftingId)
				{
					num3 = Terrain.MakeBlockValue(203, 0, data.HasValue ? data.Value : 0);
				}
				else if (craftingId == BlocksManager.Blocks[129].CraftingId)
				{
					num = Terrain.MakeBlockValue(129, 0, data.HasValue ? data.Value : 0);
				}
				else if (craftingId == BlocksManager.Blocks[128].CraftingId)
				{
					num2 = Terrain.MakeBlockValue(128, 0, data.HasValue ? data.Value : 0);
				}
			}
			if (num != 0 && num3 != 0)
			{
				int num4 = Terrain.ExtractData(num3);
				int clothingColor = GetClothingColor(num4);
				int clothingIndex = GetClothingIndex(num4);
				bool canBeDyed = GetClothingData(num4).CanBeDyed;
				int damage = BlocksManager.Blocks[203].GetDamage(num3);
				int color = PaintBucketBlock.GetColor(Terrain.ExtractData(num));
				int damage2 = BlocksManager.Blocks[129].GetDamage(num);
				Block block = BlocksManager.Blocks[129];
				Block block2 = BlocksManager.Blocks[203];
				if (!canBeDyed)
				{
					return null;
				}
				int num5 = PaintBucketBlock.CombineColors(clothingColor, color);
				if (num5 != clothingColor)
				{
					CraftingRecipe craftingRecipe = new CraftingRecipe();
					craftingRecipe.ResultCount = 1;
					craftingRecipe.ResultValue = block2.SetDamage(Terrain.MakeBlockValue(203, 0, SetClothingIndex(SetClothingColor(0, num5), clothingIndex)), damage);
					craftingRecipe.RemainsCount = 1;
					craftingRecipe.RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(129, 0, color), damage2 + MathUtils.Max(block.Durability / 4, 1));
					craftingRecipe.RequiredHeatLevel = 1f;
					craftingRecipe.Description = LanguageControl.Get("BlocksManager", "Dyed") + " " + SubsystemPalette.GetName(terrain, color, null);
					craftingRecipe.Ingredients = (string[])ingredients.Clone();
					return craftingRecipe;
				}
			}
			if (num2 != 0 && num3 != 0)
			{
				int num6 = Terrain.ExtractData(num3);
				int clothingColor2 = GetClothingColor(num6);
				int clothingIndex2 = GetClothingIndex(num6);
				bool canBeDyed2 = GetClothingData(num6).CanBeDyed;
				int damage3 = BlocksManager.Blocks[203].GetDamage(num3);
				int damage4 = BlocksManager.Blocks[128].GetDamage(num2);
				Block block3 = BlocksManager.Blocks[128];
				Block block4 = BlocksManager.Blocks[203];
				if (!canBeDyed2)
				{
					return null;
				}
				if (clothingColor2 != 0)
				{
					CraftingRecipe craftingRecipe = new CraftingRecipe();
					craftingRecipe.ResultCount = 1;
					craftingRecipe.ResultValue = block4.SetDamage(Terrain.MakeBlockValue(203, 0, SetClothingIndex(SetClothingColor(0, 0), clothingIndex2)), damage3);
					craftingRecipe.RemainsCount = 1;
					craftingRecipe.RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(128, 0, 0), damage4 + MathUtils.Max(block3.Durability / 4, 1));
					craftingRecipe.RequiredHeatLevel = 1f;
					craftingRecipe.Description = LanguageControl.Get("BlocksManager", "Not Dyed") + " " + LanguageControl.Get("BlocksManager", "Clothes");
					craftingRecipe.Ingredients = (string[])ingredients.Clone();
					return craftingRecipe;
				}
			}
		}
		return null;
	}

	public static int GetClothingIndex(int data)
	{
		return data & 0xFF;
	}

	public static int SetClothingIndex(int data, int clothingIndex)
	{
		return (data & -256) | (clothingIndex & 0xFF);
	}

	public override ClothingData GetClothingData(int data)
	{
		int clothingIndex = GetClothingIndex(data);
		ClothingData clothingData = m_clothingData[clothingIndex];
		if (IsCustom(data))
		{
			SubsystemCustomClothing instance = SubsystemCustomClothing.Instance;
			if (instance != null)
			{
				int designId = GetDesignId(data);
				CustomClothingData customClothingByIndex = instance.GetCustomClothingByIndex(designId);
				if (customClothingByIndex != null)
				{
					return new ClothingData
					{
						Index = clothingData.Index,
						DisplayIndex = clothingData.DisplayIndex,
						Slot = clothingData.Slot,
						ArmorProtection = clothingData.ArmorProtection,
						Sturdiness = clothingData.Sturdiness,
						Insulation = clothingData.Insulation,
						MovementSpeedFactor = clothingData.MovementSpeedFactor,
						SteedMovementSpeedFactor = clothingData.SteedMovementSpeedFactor,
						DensityModifier = clothingData.DensityModifier,
						IsOuter = clothingData.IsOuter,
						CanBeDyed = clothingData.CanBeDyed,
						Layer = clothingData.Layer,
						PlayerLevelRequired = clothingData.PlayerLevelRequired,
						ImpactSoundsFolder = clothingData.ImpactSoundsFolder,
						MinerSpeed = clothingData.MinerSpeed,
						Shade = clothingData.Shade,
						Weight = clothingData.Weight,
						EmitsLight = clothingData.EmitsLight,
						ReplacementIndex = clothingData.ReplacementIndex,
						IsExplodes = clothingData.IsExplodes,
						DisplayName = customClothingByIndex.Id,
						Texture = (instance.GetOrCreateTexture(designId) ?? clothingData.Texture)
					};
				}
			}
		}
		return clothingData;
	}

	public static int GetClothingColor(int data)
	{
		return (data >> 12) & 0xF;
	}

	public static int SetClothingColor(int data, int color)
	{
		return (data & -61441) | ((color & 0xF) << 12);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = Terrain.ExtractData(value);
		int clothingColor = GetClothingColor(num);
		ClothingData clothingData = GetClothingData(num);
		Matrix matrix2 = m_slotTransforms[(int)clothingData.Slot] * Matrix.CreateScale(size) * matrix;
		if (clothingData.IsOuter)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_outerMesh, clothingData.Texture, color * SubsystemPalette.GetFabricColor(environmentData, clothingColor), 1f, ref matrix2, environmentData);
		}
		else
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_innerMesh, clothingData.Texture, color * SubsystemPalette.GetFabricColor(environmentData, clothingColor), 1f, ref matrix2, environmentData);
		}
	}

	static ClothingBlock()
	{
		m_clothingData = new DynamicArray<ClothingData>();
		m_slotTransforms = new Matrix[6]
		{
			Matrix.CreateTranslation(0f, -1.5f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -1.1f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -0.5f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -0.1f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0.1f, -0.85f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(-0.1f, -0.85f, 0f) * Matrix.CreateScale(2.7f)
		};
	}

	public static bool IsCustom(int data)
	{
		return ((data >> 16) & 1) != 0;
	}

	public static int SetCustom(int data, bool isCustom)
	{
		if (isCustom)
		{
			return data | 0x10000;
		}
		return data & -65537;
	}

	public static int GetDesignId(int data)
	{
		return (data >> 8) & 0xFF;
	}

	public static int SetDesignId(int data, int designId)
	{
		return (data & -65281) | ((designId & 0xFF) << 8);
	}
}
