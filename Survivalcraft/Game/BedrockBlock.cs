using System.Collections.Generic;
using Engine;

namespace Game;

public class BedrockBlock : CubeBlock
{
	public const int Index = 1;

	private string[] needIngredians;

	public override void Initialize()
	{
		base.Initialize();
		IsPlaceable = true;
		CraftingId = "BedrockBlock";
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (Terrain.ExtractData(value) == 1)
		{
			return 255;
		}
		return base.GetFaceTextureSlot(face, value);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		return new int[1] { Terrain.MakeBlockValue(1, 0, 1) };
	}

	public override BlockDigMethod GetBlockDigMethod(int value)
	{
		if (Terrain.ExtractData(value) == 1)
		{
			return BlockDigMethod.Quarry;
		}
		return BlockDigMethod.None;
	}

	public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain subsystemTerrain, string[] ingredients, float heatLevel, ComponentPlayer player)
	{
		if (player != null)
		{
			int num = 5;
			int num2 = (int)player.PlayerData.Level;
			bool flag = true;
			for (int i = 0; i < 9; i++)
			{
				if (ingredients[i] != needIngredians[i])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				if (subsystemTerrain != null)
				{
					SubsystemGameInfo subsystemGameInfo = subsystemTerrain.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: false);
					if (subsystemGameInfo != null && subsystemGameInfo.WorldSettings != null && subsystemGameInfo.WorldSettings.RegionPrivateLimit == 0)
					{
						string text = LanguageControl.Get("ComponentMiner", "region_null_blocks");
						player.ComponentGui.DisplaySmallMessage(text, Color.Red, blinking: false, playNotificationSound: true);
						return null;
					}
				}
				if (num2 < num)
				{
					player.ComponentGui.DisplaySmallMessage(string.Format(LanguageControl.Get("SubsystemBedrockBlockBehavior", 7), num.ToString(), num2.ToString()), Color.Red, blinking: false, playNotificationSound: true);
					return null;
				}
				return new CraftingRecipe
				{
					ResultValue = Terrain.MakeBlockValue(1, 0, 1),
					ResultCount = 1,
					Ingredients = needIngredians
				};
			}
		}
		return null;
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		if (needIngredians == null)
		{
			needIngredians = new string[9]
			{
				BlocksManager.Blocks[42].CraftingId,
				BlocksManager.Blocks[111].CraftingId,
				BlocksManager.Blocks[40].CraftingId,
				BlocksManager.Blocks[42].CraftingId,
				BlocksManager.Blocks[111].CraftingId,
				BlocksManager.Blocks[40].CraftingId,
				BlocksManager.Blocks[3].CraftingId,
				BlocksManager.Blocks[3].CraftingId,
				BlocksManager.Blocks[3].CraftingId
			};
			for (int i = 0; i < needIngredians.Length; i++)
			{
				needIngredians[i] += ":0";
			}
		}
		return new CraftingRecipe[1]
		{
			new CraftingRecipe
			{
				ResultValue = Terrain.MakeBlockValue(1, 0, 1),
				ResultCount = 1,
				Ingredients = needIngredians,
				IsAdHocCraftingRecipe = true
			}
		};
	}

	public override int GetMaxStacking(int value)
	{
		if (Terrain.ExtractData(value) == 1)
		{
			return 1;
		}
		return MaxStacking;
	}

	public override float GetDigResilience(int value)
	{
		if (Terrain.ExtractData(value) == 1)
		{
			return 12f;
		}
		return float.PositiveInfinity;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		if (Terrain.ExtractData(value) == 1)
		{
			return new BlockPlacementData
			{
				Value = value,
				CellFace = raycastResult.CellFace
			};
		}
		return base.GetPlacementValue(subsystemTerrain, componentMiner, value, raycastResult);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
		if (Terrain.ExtractData(oldValue) == 1)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(1, 0, 1),
				Count = 1
			});
		}
		else
		{
			dropValues.Clear();
		}
	}
}
