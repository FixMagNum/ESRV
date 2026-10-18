using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game;

public static class CraftingRecipesManager
{
	private struct CraftingOutcome
	{
		public int ResultValue;

		public int ResultCount;

		public int RemainsValue;

		public int RemainsCount;
	}

	public static List<CraftingRecipe> m_recipes;

	public static Action Initialize1;

	public static Action Initialized;

	public static string fName;

	private static List<CraftingRecipe> m_defaultRecipes;

	public static ReadOnlyList<CraftingRecipe> Recipes => new ReadOnlyList<CraftingRecipe>(m_recipes);

	public static void Initialize()
	{
		if (Initialize1 != null)
		{
			Initialize1();
			return;
		}
		foreach (XElement item in ContentManager.Get<XElement>("CraftingRecipes").Descendants("Recipe"))
		{
			CraftingRecipe craftingRecipe = new CraftingRecipe();
			string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Result");
			string text = XmlUtils.GetAttributeValue<string>(item, "Description");
			if (text.StartsWith("[") && text.EndsWith("]"))
			{
				text = LanguageControl.GetBlock(attributeValue, "CRDescription:" + text.Substring(1, text.Length - 2));
			}
			craftingRecipe.ResultValue = DecodeResult(attributeValue);
			craftingRecipe.ResultCount = XmlUtils.GetAttributeValue<int>(item, "ResultCount");
			string attributeValue2 = XmlUtils.GetAttributeValue(item, "Remains", string.Empty);
			if (!string.IsNullOrEmpty(attributeValue2))
			{
				craftingRecipe.RemainsValue = DecodeResult(attributeValue2);
				craftingRecipe.RemainsCount = XmlUtils.GetAttributeValue<int>(item, "RemainsCount");
			}
			craftingRecipe.RequiredHeatLevel = XmlUtils.GetAttributeValue<float>(item, "RequiredHeatLevel");
			craftingRecipe.RequiredPlayerLevel = XmlUtils.GetAttributeValue(item, "RequiredPlayerLevel", 1f);
			craftingRecipe.Description = text;
			craftingRecipe.Message = XmlUtils.GetAttributeValue<string>(item, "Message", null);
			if (craftingRecipe.ResultCount > BlocksManager.Blocks[Terrain.ExtractContents(craftingRecipe.ResultValue)].MaxStacking)
			{
				throw new InvalidOperationException("In recipe for \"" + attributeValue + "\" ResultCount is larger than max stacking of result block.");
			}
			if (craftingRecipe.RemainsValue != 0 && craftingRecipe.RemainsCount > BlocksManager.Blocks[Terrain.ExtractContents(craftingRecipe.RemainsValue)].MaxStacking)
			{
				throw new InvalidOperationException("In Recipe for \"" + attributeValue2 + "\" RemainsCount is larger than max stacking of remains block.");
			}
			Dictionary<char, string> dictionary = new Dictionary<char, string>();
			foreach (XAttribute item2 in from a in item.Attributes()
				where a.Name.LocalName.Length == 1 && char.IsLower(a.Name.LocalName[0])
				select a)
			{
				string[] array = item2.Value.Split('|');
				foreach (string text2 in array)
				{
					DecodeIngredient(text2, out var craftingId, out var data);
					if (BlocksManager.FindBlocksByCraftingId(craftingId).Length == 0)
					{
						throw new InvalidOperationException("Block with craftingId \"" + text2 + "\" not found.");
					}
					if (data.HasValue && (data.Value < 0 || data.Value > 262143))
					{
						throw new InvalidOperationException("Data in recipe ingredient \"" + text2 + "\" must be between 0 and 0x3FFFF.");
					}
				}
				dictionary.Add(item2.Name.LocalName[0], item2.Value);
			}
			string[] array2 = item.Value.Trim().Split(new string[1] { "\n" }, StringSplitOptions.None);
			for (int num2 = 0; num2 < array2.Length; num2++)
			{
				int num3 = array2[num2].IndexOf('"');
				int num4 = array2[num2].LastIndexOf('"');
				if (num3 < 0 || num4 < 0 || num4 <= num3)
				{
					throw new InvalidOperationException("Invalid recipe line.");
				}
				string text3 = array2[num2].Substring(num3 + 1, num4 - num3 - 1);
				for (int num5 = 0; num5 < text3.Length; num5++)
				{
					char c = text3[num5];
					if (char.IsLower(c))
					{
						string text4 = dictionary[c];
						craftingRecipe.Ingredients[num5 + num2 * 3] = text4;
					}
				}
			}
			string attributeValue3 = XmlUtils.GetAttributeValue(item, "RemainsSlot", string.Empty);
			if (!string.IsNullOrEmpty(attributeValue3) && dictionary.ContainsKey(attributeValue3[0]))
			{
				craftingRecipe.RemainsMatch = dictionary[attributeValue3[0]];
			}
			m_recipes.Add(craftingRecipe);
		}
		Block[] blocks = BlocksManager.Blocks;
		foreach (Block block in blocks)
		{
			m_recipes.AddRange(block.GetProceduralCraftingRecipes());
		}
		Initialized?.Invoke();
		m_recipes.Sort((CraftingRecipe r1, CraftingRecipe r2) =>
		{
			int y = r1.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
			int x = r2.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
			return Comparer<int>.Default.Compare(x, y);
		});
		m_defaultRecipes = new List<CraftingRecipe>(m_recipes);
	}

	public static CraftingRecipe FindMatchingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, ComponentPlayer componentPlayer)
	{
		bool flag = false;
		for (int i = 0; i < 9; i++)
		{
			if (!string.IsNullOrEmpty(ingredients[i]))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return null;
		}
		float num = componentPlayer?.PlayerData.Level ?? 1f;
		CraftingRecipe craftingRecipe = null;
		Block[] blocks = BlocksManager.Blocks;
		for (int j = 0; j < blocks.Length; j++)
		{
			CraftingRecipe adHocCraftingRecipe = blocks[j].GetAdHocCraftingRecipe(terrain, ingredients, heatLevel, componentPlayer);
			if (adHocCraftingRecipe != null && MatchRecipe(adHocCraftingRecipe.Ingredients, ingredients))
			{
				craftingRecipe = adHocCraftingRecipe;
				break;
			}
		}
		if (craftingRecipe == null)
		{
			foreach (CraftingRecipe recipe in Recipes)
			{
				if (!recipe.IsAdHocCraftingRecipe && MatchRecipe(recipe.Ingredients, ingredients))
				{
					craftingRecipe = recipe;
					break;
				}
			}
		}
		if (craftingRecipe != null)
		{
			if (heatLevel < craftingRecipe.RequiredHeatLevel)
			{
				CraftingRecipe result;
				if (heatLevel > 0f)
				{
					(result = new CraftingRecipe()).Message = LanguageControl.Get(fName, 1);
				}
				else
				{
					(result = new CraftingRecipe()).Message = LanguageControl.Get(fName, 0);
				}
				return result;
			}
			if (terrain.Project.FindSubsystem<SubsystemGameInfo>().WorldSettings.RequiresLevelsForCrafting && num < craftingRecipe.RequiredPlayerLevel)
			{
				CraftingRecipe result2;
				if (craftingRecipe.RequiredHeatLevel > 0f)
				{
					(result2 = new CraftingRecipe()).Message = string.Format(LanguageControl.Get(fName, 3), craftingRecipe.RequiredPlayerLevel);
				}
				else
				{
					(result2 = new CraftingRecipe()).Message = string.Format(LanguageControl.Get(fName, 2), craftingRecipe.RequiredPlayerLevel);
				}
				return result2;
			}
		}
		if (craftingRecipe != null && terrain.Project.FindSubsystem<SubsystemGameInfo>().WorldSettings.IsBlockDiable(craftingRecipe.ResultValue))
		{
			componentPlayer?.ComponentGui.DisplaySmallMessage("This item has been disabled", Color.Red, blinking: false, playNotificationSound: true);
			return null;
		}
		if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
		{
			bool flag2 = !string.IsNullOrEmpty(craftingRecipe.RemainsMatch);
			bool flag3 = craftingRecipe.RemainsValue != 0 && craftingRecipe.RemainsCount > 0;
			if (flag2 | flag3)
			{
				int num2 = craftingRecipe.RemainsValue;
				int num3 = craftingRecipe.RemainsCount;
				int? data = null;
				int num4 = 0;
				if (flag2)
				{
					for (int k = 0; k < 9; k++)
					{
						if (!string.IsNullOrEmpty(ingredients[k]) && CompareIngredients(craftingRecipe.RemainsMatch, ingredients[k]))
						{
							DecodeIngredient(ingredients[k], out var craftingId, out data);
							Block[] array = BlocksManager.FindBlocksByCraftingId(craftingId);
							if (array.Length != 0)
							{
								num4 = array[0].BlockIndex;
								num2 = Terrain.MakeBlockValue(num4, 0, data ?? 0);
								num3 = 1;
							}
							break;
						}
					}
				}
				else if (flag3)
				{
					num4 = Terrain.ExtractContents(num2);
					string craftingId2 = BlocksManager.Blocks[num4].CraftingId;
					for (int l = 0; l < 9; l++)
					{
						if (!string.IsNullOrEmpty(ingredients[l]))
						{
							DecodeIngredient(ingredients[l], out var craftingId3, out var data2);
							if (craftingId3 == craftingId2 && data2.HasValue)
							{
								data = data2;
								break;
							}
						}
					}
				}
				if (data.HasValue && num2 != 0)
				{
					int num5 = 10;
					if (terrain != null && terrain.Project != null)
					{
						SubsystemGameInfo subsystemGameInfo = terrain.Project.FindSubsystem<SubsystemGameInfo>();
						if (subsystemGameInfo != null)
						{
							float itemDurabilityMultiplier = subsystemGameInfo.WorldSettings.ItemDurabilityMultiplier;
							if (itemDurabilityMultiplier <= 0f)
							{
								num5 = 0;
							}
							else
							{
								float num6 = 10f * itemDurabilityMultiplier;
								num5 = (int)num6;
								if (new Random().Float(0f, 1f) < num6 - (float)num5)
								{
									num5++;
								}
							}
						}
					}
					int num7 = BlocksManager.DamageItem(Terrain.MakeBlockValue(num4, 0, data.Value), num5);
					if (num7 != craftingRecipe.RemainsValue || num7 == 0)
					{
						craftingRecipe = new CraftingRecipe
						{
							ResultValue = craftingRecipe.ResultValue,
							ResultCount = craftingRecipe.ResultCount,
							RemainsValue = num7,
							RemainsCount = ((num7 != 0) ? num3 : 0),
							RequiredHeatLevel = craftingRecipe.RequiredHeatLevel,
							RequiredPlayerLevel = craftingRecipe.RequiredPlayerLevel,
							Description = craftingRecipe.Description,
							Message = craftingRecipe.Message,
							IsAdHocCraftingRecipe = craftingRecipe.IsAdHocCraftingRecipe,
							RemainsMatch = craftingRecipe.RemainsMatch,
							Ingredients = (string[])craftingRecipe.Ingredients.Clone()
						};
					}
				}
			}
		}
		return craftingRecipe;
	}

	public static int DecodeResult(string result)
	{
		if (!string.IsNullOrEmpty(result))
		{
			string[] array = result.Split(new char[1] { ':' }, StringSplitOptions.None);
			Block block = BlocksManager.FindBlockByTypeName(array[0], throwIfNotFound: true);
			return Terrain.MakeBlockValue(data: (array.Length >= 2) ? int.Parse(array[1], CultureInfo.InvariantCulture) : 0, contents: block.BlockIndex, light: 0);
		}
		return 0;
	}

	public static void DecodeIngredient(string ingredient, out string craftingId, out int? data)
	{
		string[] movingBlockSet = ingredient.Split(new char[1] { ':' }, StringSplitOptions.None);
		craftingId = movingBlockSet[0];
		data = ((movingBlockSet.Length >= 2) ? new int?(int.Parse(movingBlockSet[1], CultureInfo.InvariantCulture)) : ((int?)null));
	}

	public static bool MatchRecipe(string[] requiredIngredients, string[] actualIngredients)
	{
		string[] array = new string[9];
		for (int i = 0; i < 2; i++)
		{
			for (int j = -3; j <= 3; j++)
			{
				for (int k = -3; k <= 3; k++)
				{
					bool flip = i != 0;
					if (!TransformRecipe(array, requiredIngredients, k, j, flip))
					{
						continue;
					}
					bool flag = true;
					for (int l = 0; l < 9; l++)
					{
						if (!CompareIngredients(array[l], actualIngredients[l]))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static bool TransformRecipe(string[] transformedIngredients, string[] ingredients, int shiftX, int shiftY, bool flip)
	{
		for (int i = 0; i < 9; i++)
		{
			transformedIngredients[i] = null;
		}
		for (int j = 0; j < 3; j++)
		{
			for (int k = 0; k < 3; k++)
			{
				int num = (flip ? (3 - k - 1) : k) + shiftX;
				int num2 = j + shiftY;
				string text = ingredients[k + j * 3];
				if (num >= 0 && num2 >= 0 && num < 3 && num2 < 3)
				{
					transformedIngredients[num + num2 * 3] = text;
				}
				else if (!string.IsNullOrEmpty(text))
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool CompareIngredients(string requiredIngredient, string actualIngredient)
	{
		if (requiredIngredient == null)
		{
			return actualIngredient == null;
		}
		if (actualIngredient == null)
		{
			return requiredIngredient == null;
		}
		DecodeIngredient(actualIngredient, out var craftingId, out var data);
		if (!data.HasValue)
		{
			throw new InvalidOperationException("Actual ingredient data not specified.");
		}
		string[] array = requiredIngredient.Split('|');
		for (int i = 0; i < array.Length; i++)
		{
			DecodeIngredient(array[i], out var craftingId2, out var data2);
			if (craftingId2 == craftingId && (!data2.HasValue || data2.Value == data.Value))
			{
				return true;
			}
		}
		return false;
	}

	static CraftingRecipesManager()
	{
		m_recipes = new List<CraftingRecipe>();
		fName = "CraftingRecipesManager";
	}

	public static void SetActiveRecipes(List<CraftingRecipe> newRecipes)
	{
		if (newRecipes != null)
		{
			m_recipes = newRecipes;
		}
	}

	public static void RestoreDefaultRecipes()
	{
		if (m_defaultRecipes != null)
		{
			m_recipes = new List<CraftingRecipe>(m_defaultRecipes);
		}
	}

	public static List<CraftingRecipe> GetDefaultRecipes()
	{
		return m_defaultRecipes;
	}

	public static List<CraftingRecipe> GenerateShuffledRecipes(int seed)
	{
		List<CraftingRecipe> defaultRecipes = GetDefaultRecipes();
		if (defaultRecipes == null || defaultRecipes.Count == 0)
		{
			return null;
		}
		System.Random random = new System.Random(seed);
		List<CraftingOutcome> list = new List<CraftingOutcome>();
		foreach (CraftingRecipe item2 in defaultRecipes)
		{
			list.Add(new CraftingOutcome
			{
				ResultValue = item2.ResultValue,
				ResultCount = item2.ResultCount,
				RemainsValue = item2.RemainsValue,
				RemainsCount = item2.RemainsCount
			});
		}
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = random.Next(num + 1);
			CraftingOutcome value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
		List<CraftingRecipe> list2 = new List<CraftingRecipe>();
		for (int i = 0; i < defaultRecipes.Count; i++)
		{
			CraftingRecipe craftingRecipe = defaultRecipes[i];
			CraftingOutcome craftingOutcome = list[i];
			CraftingRecipe item = new CraftingRecipe
			{
				Ingredients = (string[])craftingRecipe.Ingredients.Clone(),
				RequiredHeatLevel = craftingRecipe.RequiredHeatLevel,
				RequiredPlayerLevel = craftingRecipe.RequiredPlayerLevel,
				Description = craftingRecipe.Description,
				Message = craftingRecipe.Message,
				IsAdHocCraftingRecipe = craftingRecipe.IsAdHocCraftingRecipe,
				ResultValue = craftingOutcome.ResultValue,
				ResultCount = craftingOutcome.ResultCount,
				RemainsValue = craftingOutcome.RemainsValue,
				RemainsCount = craftingOutcome.RemainsCount
			};
			list2.Add(item);
		}
		list2.Sort((CraftingRecipe r1, CraftingRecipe r2) =>
		{
			int y = r1.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
			int x = r2.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
			return Comparer<int>.Default.Compare(x, y);
		});
		return list2;
	}
}
