using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game;

public class EggBlock : Block
{
	public class EggType
	{
		public int EggTypeIndex;

		public bool ShowEgg;

		public string DisplayName;

		public string TemplateName;

		public float NutritionalValue;

		public int TextureSlot;

		public Color Color;

		public Vector2 ScaleUV;

		public bool SwapUV;

		public float Scale;

		public BlockMesh BlockMesh;
	}

	public new static string fName;

	public const int Index = 118;

	public DynamicArray<EggType> m_eggTypes = new DynamicArray<EggType>();

	public ReadOnlyList<EggType> EggTypes => new ReadOnlyList<EggType>(m_eggTypes);

	public override void Initialize()
	{
		m_eggTypes.Clear();
		DatabaseObjectType light = DatabaseManager.GameDatabase.ParameterSetType;
		Guid eggParameterSetGuid = new Guid("300ff557-775f-4c7c-a88a-26655369f00b");
		foreach (DatabaseObject item in from o in DatabaseManager.GameDatabase.Database.Root.GetExplicitNestingChildren(light, directChildrenOnly: false)
			where o.EffectiveInheritanceRoot.Guid == eggParameterSetGuid
			select o)
		{
			int nestedValue = item.GetNestedValue<int>("EggTypeIndex");
			if (nestedValue >= 0)
			{
				string text = item.GetNestedValue<string>("DisplayName");
				if (text.StartsWith("[") && text.EndsWith("]"))
				{
					string[] array = text.Substring(1, text.Length - 2).Split(new string[1] { ":" }, StringSplitOptions.RemoveEmptyEntries);
					text = LanguageControl.GetDatabase("DisplayName", array[1]);
				}
				if (nestedValue >= m_eggTypes.Count)
				{
					m_eggTypes.Count = nestedValue + 1;
				}
				m_eggTypes[nestedValue] = new EggType
				{
					EggTypeIndex = nestedValue,
					ShowEgg = item.GetNestedValue<bool>("ShowEgg"),
					DisplayName = text,
					TemplateName = item.NestingParent.Name,
					NutritionalValue = item.GetNestedValue<float>("NutritionalValue"),
					Color = item.GetNestedValue<Color>("Color"),
					ScaleUV = item.GetNestedValue<Vector2>("ScaleUV"),
					SwapUV = item.GetNestedValue<bool>("SwapUV"),
					Scale = item.GetNestedValue<float>("Scale"),
					TextureSlot = item.GetNestedValue<int>("TextureSlot")
				};
			}
		}
		Model model = ContentManager.Get<Model>("Models/Egg");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Egg").ParentBone);
		foreach (EggType eggType in m_eggTypes)
		{
			if (eggType != null)
			{
				eggType.BlockMesh = new BlockMesh();
				eggType.BlockMesh.AppendModelMeshPart(model.FindMesh("Egg").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, eggType.Color);
				Matrix identity = Matrix.Identity;
				if (eggType.SwapUV)
				{
					identity.M11 = 0f;
					identity.M12 = 1f;
					identity.M21 = 1f;
					identity.M22 = 0f;
				}
				identity *= Matrix.CreateScale(0.0625f * eggType.ScaleUV.X, 0.0625f * eggType.ScaleUV.Y, 1f);
				identity *= Matrix.CreateTranslation((float)(eggType.TextureSlot % 16) / 16f, (float)(eggType.TextureSlot / 16) / 16f, 0f);
				eggType.BlockMesh.TransformTextureCoordinates(identity);
			}
		}
		base.Initialize();
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		EggType y2 = GetEggType(Terrain.ExtractData(value));
		int data = Terrain.ExtractData(value);
		bool light = GetIsCooked(data);
		bool isLaid = GetIsLaid(data);
		if (light)
		{
			return LanguageControl.Get(fName, 1) + " " + y2.DisplayName;
		}
		if (!isLaid)
		{
			return y2.DisplayName;
		}
		return LanguageControl.Get(fName, 2) + " " + y2.DisplayName;
	}

	public override string GetCategory(int value)
	{
		return "Spawner Eggs";
	}

	public override string GetDescription(int value)
	{
		return LanguageControl.GetBlock(fName + ":0", "Description");
	}

	public override float GetNutritionalValue(int value)
	{
		EggType cornerLights = GetEggType(Terrain.ExtractData(value));
		if (!GetIsCooked(Terrain.ExtractData(value)))
		{
			return cornerLights.NutritionalValue;
		}
		return 1.5f * cornerLights.NutritionalValue;
	}

	public override float GetSicknessProbability(int value)
	{
		if (!GetIsCooked(Terrain.ExtractData(value)))
		{
			return DefaultSicknessProbability;
		}
		return 0f;
	}

	public override int GetRotPeriod(int value)
	{
		if (GetNutritionalValue(value) > 0f)
		{
			return base.GetRotPeriod(value);
		}
		return 0;
	}

	public override int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 16) & 1;
	}

	public override int SetDamage(int value, int damage)
	{
		int rot = Terrain.ExtractData(value);
		rot = (rot & -65537) | ((damage & 1) << 16);
		return Terrain.ReplaceData(value, rot);
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 246;
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		foreach (EggType eggType in m_eggTypes)
		{
			if (eggType != null && eggType.ShowEgg)
			{
				yield return Terrain.MakeBlockValue(118, 0, SetEggType(0, eggType.EggTypeIndex));
				if (eggType.NutritionalValue > 0f)
				{
					yield return Terrain.MakeBlockValue(118, 0, SetIsCooked(SetEggType(0, eggType.EggTypeIndex), isCooked: true));
				}
			}
		}
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		foreach (EggType eggType in EggTypes)
		{
			if (eggType != null && eggType.NutritionalValue > 0f)
			{
				int rot = 0;
				while (rot <= 1)
				{
					CraftingRecipe craftingRecipe = new CraftingRecipe
					{
						ResultCount = 1,
						ResultValue = Terrain.MakeBlockValue(118, 0, SetEggType(SetIsCooked(0, isCooked: true), eggType.EggTypeIndex)),
						RemainsCount = 1,
						RemainsValue = Terrain.MakeBlockValue(91),
						RequiredHeatLevel = 1f,
						Description = LanguageControl.Get(fName, 4)
					};
					int i = SetEggType(SetIsLaid(0, isLaid: true), eggType.EggTypeIndex);
					int num = SetDamage(Terrain.MakeBlockValue(118, 0, i), rot);
					craftingRecipe.Ingredients[0] = "egg:" + Terrain.ExtractData(num).ToString(CultureInfo.InvariantCulture);
					craftingRecipe.Ingredients[1] = "waterbucket";
					yield return craftingRecipe;
					int j = rot + 1;
					rot = j;
				}
			}
		}
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = Terrain.ExtractData(value);
		EggType num2 = GetEggType(num);
		BlocksManager.DrawMeshBlock(primitivesRenderer, num2.BlockMesh, color, num2.Scale * size, ref matrix, environmentData);
	}

	public EggType GetEggType(int data)
	{
		int tx = (data >> 4) & 0xFFF;
		return m_eggTypes[tx];
	}

	public EggType GetEggTypeByCreatureTemplateName(string templateName)
	{
		return m_eggTypes.FirstOrDefault((EggType e) => e != null && e.TemplateName == templateName);
	}

	public static bool GetIsCooked(int data)
	{
		return (data & 1) != 0;
	}

	public static int SetIsCooked(int data, bool isCooked)
	{
		if (!isCooked)
		{
			return data & -2;
		}
		return data | 1;
	}

	public static bool GetIsLaid(int data)
	{
		return (data & 2) != 0;
	}

	public static int SetIsLaid(int data, bool isLaid)
	{
		if (!isLaid)
		{
			return data & -3;
		}
		return data | 2;
	}

	public static int SetEggType(int data, int eggTypeIndex)
	{
		data &= -65521;
		data |= (eggTypeIndex & 0xFFF) << 4;
		return data;
	}

	static EggBlock()
	{
		fName = "EggBlock";
	}
}
