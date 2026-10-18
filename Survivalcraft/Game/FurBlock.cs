using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class FurBlock : Block
{
	public enum FurState
	{
		Raw,
		Oiled,
		Tanned
	}

	public const int Index = 207;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public static readonly Color RawColor;

	public static readonly Color OiledColor;

	public static readonly Color TannedColor;

	public override void Initialize()
	{
		Model resolution = ContentManager.Get<Model>("Models/Fur");
		Matrix box = BlockMesh.GetBoneAbsoluteTransform(resolution.FindMesh("Fur").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(resolution.FindMesh("Fur").MeshParts[0], box * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(resolution.FindMesh("Fur").MeshParts[0], box * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: false, new Color(128, 128, 160));
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		FurState resolution = GetState(Terrain.ExtractData(value));
		Color num = TannedColor;
		switch (resolution)
		{
		case FurState.Raw:
			num = RawColor;
			break;
		case FurState.Oiled:
			num = OiledColor;
			break;
		}
		Color color2 = color * num;
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color2, 2f * size, ref matrix, environmentData);
	}

	public static FurState GetState(int data)
	{
		return (FurState)(data & 3);
	}

	public static int SetState(int data, FurState state)
	{
		return (data & -4) | (int)(state & (FurState)3);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, FurState.Raw));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, FurState.Oiled));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, FurState.Tanned));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		FurState state = GetState(Terrain.ExtractData(value));
		string block = LanguageControl.GetBlock("FurBlock:0", "DisplayName");
		return state switch
		{
			FurState.Oiled => LanguageControl.Get("FurBlock", "OiledPrefix") + " " + block, 
			FurState.Tanned => LanguageControl.Get("FurBlock", "TannedPrefix") + " " + block, 
			_ => LanguageControl.Get("FurBlock", "RawPrefix") + " " + block, 
		};
	}

	static FurBlock()
	{
		RawColor = new Color(255, 100, 100);
		OiledColor = new Color(130, 130, 130);
		TannedColor = Color.White;
	}
}
