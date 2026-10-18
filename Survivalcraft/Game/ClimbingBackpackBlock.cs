using Engine;
using Engine.Graphics;

namespace Game;

public class ClimbingBackpackBlock : Block
{
	public const int Index = 422;

	private BlockMesh m_mesh;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/ClimbingBackpack");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("ClimbingBackpack").ParentBone);
		m_mesh = new BlockMesh();
		m_mesh.AppendModelMeshPart(model.FindMesh("ClimbingBackpack").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geo, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		int num = GetColor(Terrain.ExtractData(value));
		Color num2 = SubsystemPalette.GetFabricColor(env, num);
		BlocksManager.DrawMeshBlock(r, m_mesh, color * num2, size, ref m, env);
	}

	public static int GetBagId(int data)
	{
		return data & 0xFFF;
	}

	public static int SetBagId(int data, int bagId)
	{
		return (data & -4096) | MathUtils.Clamp(bagId, 0, 4095);
	}

	public override string GetDisplayName(SubsystemTerrain st, int value)
	{
		string displayName = base.GetDisplayName(st, value);
		int color = GetColor(Terrain.ExtractData(value));
		if (color == 0)
		{
			return displayName;
		}
		return SubsystemPalette.GetName(st, color, displayName);
	}

	public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, ComponentPlayer player)
	{
		if (heatLevel < 1f)
		{
			return null;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (!string.IsNullOrEmpty(ingredients[i]))
			{
				CraftingRecipesManager.DecodeIngredient(ingredients[i], out var craftingId, out var data);
				if (craftingId == CraftingId)
				{
					num = Terrain.MakeBlockValue(422, 0, data ?? 0);
				}
				else if (craftingId == BlocksManager.Blocks[129].CraftingId)
				{
					num2 = Terrain.MakeBlockValue(129, 0, data ?? 0);
				}
				else if (craftingId == BlocksManager.Blocks[128].CraftingId)
				{
					num3 = Terrain.MakeBlockValue(128, 0, data ?? 0);
				}
			}
		}
		if (num == 0)
		{
			return null;
		}
		int data2 = Terrain.ExtractData(num);
		int bagId = GetBagId(data2);
		int color = GetColor(data2);
		if (num2 != 0)
		{
			int num4 = PaintBucketBlock.CombineColors(color, PaintBucketBlock.GetColor(Terrain.ExtractData(num2)));
			if (num4 != color)
			{
				int data3 = SetColor(SetBagId(0, bagId), num4);
				int damage = BlocksManager.Blocks[129].GetDamage(num2);
				Block block = BlocksManager.Blocks[129];
				CraftingRecipe craftingRecipe = new CraftingRecipe();
				craftingRecipe.ResultCount = 1;
				craftingRecipe.ResultValue = Terrain.MakeBlockValue(422, 0, data3);
				craftingRecipe.RemainsCount = 1;
				craftingRecipe.RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(129, 0, PaintBucketBlock.GetColor(Terrain.ExtractData(num2))), damage + MathUtils.Max(block.Durability / 4, 1));
				craftingRecipe.RequiredHeatLevel = 1f;
				craftingRecipe.Description = LanguageControl.Get("BlocksManager", "Dyed") + " " + SubsystemPalette.GetName(terrain, num4, null);
				craftingRecipe.Ingredients = (string[])ingredients.Clone();
				return craftingRecipe;
			}
		}
		if (num3 != 0 && color != 0)
		{
			int data4 = SetColor(SetBagId(0, bagId), 0);
			int damage2 = BlocksManager.Blocks[128].GetDamage(num3);
			Block block2 = BlocksManager.Blocks[128];
			CraftingRecipe craftingRecipe = new CraftingRecipe();
			craftingRecipe.ResultCount = 1;
			craftingRecipe.ResultValue = Terrain.MakeBlockValue(422, 0, data4);
			craftingRecipe.RemainsCount = 1;
			craftingRecipe.RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(128, 0, 0), damage2 + MathUtils.Max(block2.Durability / 4, 1));
			craftingRecipe.RequiredHeatLevel = 1f;
			craftingRecipe.Description = LanguageControl.Get("BlocksManager", "Not Dyed") + " " + base.GetDisplayName(terrain, 0);
			craftingRecipe.Ingredients = (string[])ingredients.Clone();
			return craftingRecipe;
		}
		return null;
	}

	public static int GetColor(int data)
	{
		return (data >> 12) & 0xF;
	}

	public static int SetColor(int data, int color)
	{
		return (data & -61441) | ((color & 0xF) << 12);
	}
}
