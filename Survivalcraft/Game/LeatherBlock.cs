using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class LeatherBlock : Block
{
	public enum LeatherState
	{
		Raw,
		Oiled,
		Tanned
	}

	public const int Index = 159;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public static readonly Color RawColor;

	public static readonly Color OiledColor;

	public static readonly Color TannedColor;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Leather");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Leather").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Leather").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Leather").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: false, new Color(128, 128, 160));
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		LeatherState blockMesh = GetState(Terrain.ExtractData(value));
		Color blockMesh2 = TannedColor;
		switch (blockMesh)
		{
		case LeatherState.Raw:
			blockMesh2 = RawColor;
			break;
		case LeatherState.Oiled:
			blockMesh2 = OiledColor;
			break;
		}
		Color color2 = color * blockMesh2;
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color2, 2f * size, ref matrix, environmentData);
	}

	public static LeatherState GetState(int data)
	{
		return (LeatherState)(data & 3);
	}

	public static int SetState(int data, LeatherState state)
	{
		return (data & -4) | (int)(state & (LeatherState)3);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, LeatherState.Raw));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, LeatherState.Oiled));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, LeatherState.Tanned));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		LeatherState state = GetState(Terrain.ExtractData(value));
		string block = LanguageControl.GetBlock("LeatherBlock:0", "DisplayName");
		return state switch
		{
			LeatherState.Oiled => LanguageControl.Get("LeatherBlock", "OiledPrefix") + " " + block, 
			LeatherState.Tanned => LanguageControl.Get("LeatherBlock", "TannedPrefix") + " " + block, 
			_ => LanguageControl.Get("LeatherBlock", "RawPrefix") + " " + block, 
		};
	}

	static LeatherBlock()
	{
		RawColor = new Color(255, 100, 100);
		OiledColor = new Color(130, 130, 130);
		TannedColor = Color.White;
	}
}
