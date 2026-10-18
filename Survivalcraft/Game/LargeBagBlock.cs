using Engine;
using Engine.Graphics;

namespace Game;

public class LargeBagBlock : Block
{
	public const int Index = 416;

	private BlockMesh m_mesh;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/LargeBag");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("LargeBag").ParentBone);
		m_mesh = new BlockMesh();
		m_mesh.AppendModelMeshPart(model.FindMesh("LargeBag").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geo, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		int color2 = GetColor(Terrain.ExtractData(value));
		Color fabricColor = SubsystemPalette.GetFabricColor(env, color2);
		BlocksManager.DrawMeshBlock(r, m_mesh, color * fabricColor, size, ref m, env);
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
		int text = 0;
		int image = 0;
		int text2 = 0;
		for (int clothingBlock = 0; clothingBlock < ingredients.Length; clothingBlock++)
		{
			if (!string.IsNullOrEmpty(ingredients[clothingBlock]))
			{
				CraftingRecipesManager.DecodeIngredient(ingredients[clothingBlock], out var stream, out var clothingData);
				if (stream == CraftingId)
				{
					text = Terrain.MakeBlockValue(416, 0, clothingData ?? 0);
				}
				else if (stream == BlocksManager.Blocks[129].CraftingId)
				{
					image = Terrain.MakeBlockValue(129, 0, clothingData ?? 0);
				}
				else if (stream == BlocksManager.Blocks[128].CraftingId)
				{
					text2 = Terrain.MakeBlockValue(128, 0, clothingData ?? 0);
				}
			}
		}
		if (text == 0)
		{
			return null;
		}
		int data = Terrain.ExtractData(text);
		int subsystemCustomClothing = GetBagId(data);
		int value = GetColor(data);
		if (image != 0)
		{
			int data2 = PaintBucketBlock.CombineColors(value, PaintBucketBlock.GetColor(Terrain.ExtractData(image)));
			if (data2 != value)
			{
				int num7 = SetColor(SetBagId(0, subsystemCustomClothing), data2);
				int ex = BlocksManager.Blocks[129].GetDamage(image);
				Block block = BlocksManager.Blocks[129];
				CraftingRecipe craftingRecipe = new CraftingRecipe();
				craftingRecipe.ResultCount = 1;
				craftingRecipe.ResultValue = Terrain.MakeBlockValue(416, 0, num7);
				craftingRecipe.RemainsCount = 1;
				craftingRecipe.RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(129, 0, PaintBucketBlock.GetColor(Terrain.ExtractData(image))), ex + MathUtils.Max(block.Durability / 4, 1));
				craftingRecipe.RequiredHeatLevel = 1f;
				craftingRecipe.Description = LanguageControl.Get("BlocksManager", "Dyed") + " " + SubsystemPalette.GetName(terrain, data2, null);
				craftingRecipe.Ingredients = (string[])ingredients.Clone();
				return craftingRecipe;
			}
		}
		if (text2 != 0 && value != 0)
		{
			int data3 = SetColor(SetBagId(0, subsystemCustomClothing), 0);
			int damage = BlocksManager.Blocks[128].GetDamage(text2);
			Block block2 = BlocksManager.Blocks[128];
			CraftingRecipe craftingRecipe = new CraftingRecipe();
			craftingRecipe.ResultCount = 1;
			craftingRecipe.ResultValue = Terrain.MakeBlockValue(416, 0, data3);
			craftingRecipe.RemainsCount = 1;
			craftingRecipe.RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(128, 0, 0), damage + MathUtils.Max(block2.Durability / 4, 1));
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
