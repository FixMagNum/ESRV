using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class FishRodBlock : Block
{
	public enum RodKind
	{
		Normal,
		FireHardened
	}

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public const int Index = 431;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/FishRod");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("FishRod").ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(model.FindMesh("FishRod").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendBlockMesh(blockMesh);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		Color tintedColor = GetTintedColor(Color.White, value);
		g.GenerateShadedMeshVertices(this, x, y, z, m_standaloneBlockMesh, tintedColor, null, null, geom.SubsetAlphaTest);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Color tintedColor = GetTintedColor(color, value);
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, tintedColor, size, ref matrix, environmentData);
	}

	public FishRodBlock()
	{
		Initialize();
	}

	public static RodKind GetKind(int data)
	{
		return (RodKind)(data & 1);
	}

	public static int SetKind(int data, RodKind kind)
	{
		return (data & -2) | (int)(kind & RodKind.FireHardened);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, RodKind.Normal));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, RodKind.FireHardened));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int kind = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"FishRodBlock:{kind}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int kind = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"FishRodBlock:{kind}", "Description");
	}

	public override int GetEmittedLightAmount(int value)
	{
		return 0;
	}

	public override string GetCategory(int value)
	{
		return "Items";
	}

	public override void GetDropValues(SubsystemTerrain sst, int oldValue, int newValue, int toolLevel, List<BlockDropValue> drops, out bool showDebris)
	{
		int data = Terrain.ExtractData(oldValue);
		drops.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			Count = 1
		});
		showDebris = true;
	}

	private Color GetTintedColor(Color baseColor, int value)
	{
		if (GetKind(Terrain.ExtractData(value)) == RodKind.FireHardened)
		{
			Color c = new Color(128, 16, 32);
			return Color.Lerp(baseColor, c, 0.3f);
		}
		return baseColor;
	}
}
