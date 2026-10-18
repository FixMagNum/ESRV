using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CactusChunkBlock : FoodBlock
{
	public enum CookState
	{
		Raw,
		Boiled,
		BoiledAndDried
	}

	public const int Index = 442;

	public BlockMesh m_rawMesh = new BlockMesh();

	public BlockMesh m_boiledMesh = new BlockMesh();

	public BlockMesh m_driedMesh = new BlockMesh();

	public BlockMesh m_spikesMesh = new BlockMesh();

	public CactusChunkBlock()
		: base("Models/StoneCactus", Matrix.Identity, Color.White, 0)
	{
	}

	public static bool HasSpikes(int data)
	{
		return (data & 1) != 0;
	}

	public static int SetSpikes(int data, bool hasSpikes)
	{
		if (!hasSpikes)
		{
			return data & -2;
		}
		return data | 1;
	}

	public static CookState GetCookState(int data)
	{
		return (CookState)((data >> 1) & 3);
	}

	public static int SetCookState(int data, CookState state)
	{
		return (data & -7) | ((int)(state & (CookState)3) << 1);
	}

	public override void Initialize()
	{
		base.Initialize();
		Model model = ContentManager.Get<Model>("Models/StoneCactus");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("RawCactusChunk").ParentBone);
		m_rawMesh.AppendModelMeshPart(model.FindMesh("RawCactusChunk").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("BoiledCactusChunk").ParentBone);
		m_boiledMesh.AppendModelMeshPart(model.FindMesh("BoiledCactusChunk").MeshParts[0], boneAbsoluteTransform2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("BoiledAndDriedCactusChunk").ParentBone);
		m_driedMesh.AppendModelMeshPart(model.FindMesh("BoiledAndDriedCactusChunk").MeshParts[0], boneAbsoluteTransform3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Spikes").ParentBone);
		m_spikesMesh.AppendModelMeshPart(model.FindMesh("Spikes").MeshParts[0], boneAbsoluteTransform4, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(442, 0, SetSpikes(SetCookState(0, CookState.Raw), hasSpikes: true));
		yield return Terrain.MakeBlockValue(442, 0, SetSpikes(SetCookState(0, CookState.Raw), hasSpikes: false));
		yield return Terrain.MakeBlockValue(442, 0, SetSpikes(SetCookState(0, CookState.Boiled), hasSpikes: false));
		yield return Terrain.MakeBlockValue(442, 0, SetSpikes(SetCookState(0, CookState.BoiledAndDried), hasSpikes: false));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int data = Terrain.ExtractData(value);
		int cookState = (int)GetCookState(data);
		string block = LanguageControl.GetBlock($"CactusChunkBlock:{cookState}", "DisplayName");
		block = ((!HasSpikes(data)) ? (LanguageControl.Get("CactusChunkBlock", "CleanPrefix") + " " + block) : (LanguageControl.Get("CactusChunkBlock", "SpikyPrefix") + " " + block));
		if (FoodBlock.GetFrozen(data))
		{
			block = LanguageControl.Get("FoodBlock", "Frozen") + " " + block;
		}
		return block;
	}

	public override string GetDescription(int value)
	{
		int cookState = (int)GetCookState(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"CactusChunkBlock:{cookState}", "Description");
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int data = Terrain.ExtractData(value);
		CookState cookState = GetCookState(data);
		bool flag = HasSpikes(data);
		BlockMesh blockMesh = m_rawMesh;
		Color color2 = color;
		switch (cookState)
		{
		case CookState.Boiled:
			blockMesh = m_boiledMesh;
			break;
		case CookState.BoiledAndDried:
			blockMesh = m_driedMesh;
			color2 = color * new Color(160, 170, 90);
			break;
		}
		if (FoodBlock.GetFrozen(data))
		{
			color2 *= new Color(160, 200, 255);
		}
		BlocksManager.DrawMeshBlock(primitivesRenderer, blockMesh, color2, size, ref matrix, environmentData);
		if (flag)
		{
			Color color3 = color;
			if (FoodBlock.GetFrozen(data))
			{
				color3 *= new Color(160, 200, 255);
			}
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_spikesMesh, color3, size, ref matrix, environmentData);
		}
	}

	public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, ComponentPlayer componentPlayer)
	{
		string[] array = "stoneknife|bronzeknife|ironknife|coppermachete|ironmachete|diamondmachete|stoneaxe|basaltaxe|copperaxe|ironaxe|diamondaxe|bronzeaxe|coppershovel|ironshovel|diamondshovel|bronzeshovel|stonerake|copperrake|ironrake|diamondrake|bronzerake".Split('|');
		int num = 0;
		int num2 = 0;
		string remainsMatch = null;
		int data = 0;
		for (int i = 0; i < 9; i++)
		{
			if (string.IsNullOrEmpty(ingredients[i]))
			{
				continue;
			}
			CraftingRecipesManager.DecodeIngredient(ingredients[i], out var craftingId, out var data2);
			bool flag = false;
			string[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				if (array2[j] == craftingId)
				{
					flag = true;
					remainsMatch = craftingId;
					break;
				}
			}
			if (flag)
			{
				num++;
				continue;
			}
			if (!(craftingId == CraftingId))
			{
				return null;
			}
			num2++;
			data = (data2.HasValue ? data2.Value : 0);
		}
		if (num == 1 && num2 == 1 && HasSpikes(data))
		{
			CookState cookState = GetCookState(data);
			int data3 = SetSpikes(SetCookState(0, cookState), hasSpikes: false);
			if (FoodBlock.GetFrozen(data))
			{
				data3 = FoodBlock.SetFrozen(data3, frozen: true);
			}
			return new CraftingRecipe
			{
				ResultCount = 1,
				ResultValue = Terrain.MakeBlockValue(442, 0, data3),
				RemainsMatch = remainsMatch,
				RequiredHeatLevel = 0f,
				Description = LanguageControl.GetBlock("CactusChunkBlock", "CRDescription"),
				Ingredients = (string[])ingredients.Clone(),
				IsAdHocCraftingRecipe = true
			};
		}
		return null;
	}

	public override float GetProjectilePower(int value)
	{
		if (HasSpikes(Terrain.ExtractData(value)))
		{
			return 4f;
		}
		return base.GetProjectilePower(value);
	}

	public override float GetMeleePower(int value)
	{
		if (HasSpikes(Terrain.ExtractData(value)))
		{
			return 4f;
		}
		return base.GetMeleePower(value);
	}

	public override FoodType GetFoodType(int value)
	{
		if (HasSpikes(Terrain.ExtractData(value)))
		{
			return FoodType.None;
		}
		return base.GetFoodType(value);
	}

	public override float GetSicknessProbability(int value)
	{
		if (GetCookState(Terrain.ExtractData(value)) == CookState.Raw)
		{
			return 0.3f;
		}
		return 0f;
	}

	public override int GetRotPeriod(int value)
	{
		if (GetCookState(Terrain.ExtractData(value)) == CookState.BoiledAndDried)
		{
			return 2000;
		}
		return base.GetRotPeriod(value);
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		string toolsString = "stoneknife|bronzeknife|ironknife|coppermachete|ironmachete|diamondmachete|stoneaxe|basaltaxe|copperaxe|ironaxe|diamondaxe|bronzeaxe|coppershovel|ironshovel|diamondshovel|bronzeshovel|stonerake|copperrake|ironrake|diamondrake|bronzerake";
		CookState[] array = new CookState[3]
		{
			CookState.Raw,
			CookState.Boiled,
			CookState.BoiledAndDried
		};
		CookState[] array2 = array;
		foreach (CookState state in array2)
		{
			int num = SetSpikes(SetCookState(0, state), hasSpikes: true);
			int data = SetSpikes(SetCookState(0, state), hasSpikes: false);
			CraftingRecipe craftingRecipe = new CraftingRecipe
			{
				ResultCount = 1,
				ResultValue = Terrain.MakeBlockValue(442, 0, data),
				RemainsMatch = toolsString,
				RequiredHeatLevel = 0f,
				Description = LanguageControl.GetBlock("CactusChunkBlock", "CRDescription")
			};
			craftingRecipe.Ingredients[0] = toolsString;
			craftingRecipe.Ingredients[1] = CraftingId + ":" + num;
			yield return craftingRecipe;
		}
		bool[] array3 = new bool[2] { true, false };
		bool[] array4 = array3;
		foreach (bool hasSpikes in array4)
		{
			int num2 = SetSpikes(SetCookState(0, CookState.Raw), hasSpikes);
			int data2 = SetSpikes(SetCookState(0, CookState.Boiled), hasSpikes);
			CraftingRecipe craftingRecipe2 = new CraftingRecipe
			{
				ResultCount = 1,
				ResultValue = Terrain.MakeBlockValue(442, 0, data2),
				RemainsValue = CraftingRecipesManager.DecodeResult("EmptyBucketBlock"),
				RemainsCount = 1,
				RequiredHeatLevel = 1f,
				Description = LanguageControl.GetBlock("CactusChunkBlock", "CRDescription")
			};
			craftingRecipe2.Ingredients[0] = CraftingId + ":" + num2;
			craftingRecipe2.Ingredients[1] = "waterbucket";
			yield return craftingRecipe2;
		}
	}

	public override int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 8) & 0xFF;
	}

	public override int SetDamage(int value, int damage)
	{
		int num = Terrain.ExtractData(value);
		num = (num & -65281) | ((damage & 0xFF) << 8);
		return Terrain.ReplaceData(value, num);
	}
}
