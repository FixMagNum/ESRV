using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class MugBlock : Block
{
	public enum DrinkType
	{
		None,
		Beer,
		Kvass,
		PumpkinTincture,
		BlackBrew
	}

	public const int Index = 429;

	private BlockMesh m_mugMesh;

	private BlockMesh m_liquidMesh;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Mug");
		ModelMesh modelMesh = model.FindMesh("Mug");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		m_mugMesh = new BlockMesh();
		m_mugMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		ModelMesh modelMesh2 = model.FindMesh("Liquid", throwIfNotFound: false) ?? model.FindMesh("Contents", throwIfNotFound: false);
		if (modelMesh2 != null)
		{
			Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone);
			m_liquidMesh = new BlockMesh();
			m_liquidMesh.AppendModelMeshPart(modelMesh2.MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		base.Initialize();
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_mugMesh, color, size, ref matrix, environmentData);
		DrinkType drink = GetDrink(value);
		if (drink != DrinkType.None && m_liquidMesh != null)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_liquidMesh, GetDrinkColor(drink), size, ref matrix, environmentData);
		}
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateShadedMeshVertices(this, x, y, z, m_mugMesh, Color.White, null, null, geometry.SubsetAlphaTest);
		DrinkType drink = GetDrink(value);
		if (drink != DrinkType.None && m_liquidMesh != null)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_liquidMesh, GetDrinkColor(drink), null, null, geometry.SubsetAlphaTest);
		}
	}

	public static DrinkType GetDrink(int value)
	{
		return (DrinkType)(Terrain.ExtractData(value) & 7);
	}

	public static int SetDrink(int data, DrinkType drink)
	{
		return (data & -8) | (int)(drink & (DrinkType)7);
	}

	public static int MakeValue(DrinkType drink)
	{
		return Terrain.MakeBlockValue(429, 0, SetDrink(0, drink));
	}

	private static Color GetDrinkColor(DrinkType drink)
	{
		return drink switch
		{
			DrinkType.Beer => new Color(212, 168, 64), 
			DrinkType.Kvass => new Color(120, 78, 40), 
			DrinkType.PumpkinTincture => new Color(216, 136, 31), 
			DrinkType.BlackBrew => Color.Black, 
			_ => Color.Transparent, 
		};
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetDrink(0, DrinkType.None));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetDrink(0, DrinkType.Beer));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetDrink(0, DrinkType.Kvass));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetDrink(0, DrinkType.PumpkinTincture));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetDrink(0, DrinkType.BlackBrew));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int drink = (int)GetDrink(value);
		return LanguageControl.GetBlock($"MugBlock:{drink}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int drink = (int)GetDrink(value);
		return LanguageControl.GetBlock($"MugBlock:{drink}", "Description");
	}

	public override string GetCategory(int value)
	{
		return "Items";
	}
}
