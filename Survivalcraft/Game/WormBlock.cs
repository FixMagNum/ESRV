using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class WormBlock : FoodBlock
{
	public enum WormKind
	{
		Normal,
		StripedCaterpillar,
		Earthworm,
		TitanitusColossus
	}

	public const int Index = 318;

	private BlockMesh m_meshNormal;

	private BlockMesh m_meshCaterpillar;

	private BlockMesh m_meshEarthworm;

	private BlockMesh m_meshTitan;

	public WormBlock()
		: base("Models/Worm", Matrix.Identity, Color.White, 242)
	{
	}

	public static WormKind GetKind(int data)
	{
		return (WormKind)(data & 3);
	}

	public static int SetKind(int data, WormKind k)
	{
		return (data & -4) | (int)(k & WormKind.TitanitusColossus);
	}

	public override void Initialize()
	{
		base.Initialize();
		m_meshNormal = BuildMesh("Models/Worm");
		m_meshCaterpillar = BuildMesh("Models/Worm_CaterpillarStriped");
		m_meshEarthworm = BuildMesh("Models/Worm_Earth");
		m_meshTitan = BuildMesh("Models/Worm_Titan");
	}

	private static BlockMesh BuildMesh(string modelPath)
	{
		Model model = ContentManager.Get<Model>(modelPath);
		BlockMesh blockMesh = new BlockMesh();
		foreach (ModelMesh mesh in model.Meshes)
		{
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(mesh.ParentBone);
			for (int i = 0; i < mesh.MeshParts.Count; i++)
			{
				blockMesh.AppendModelMeshPart(mesh.MeshParts[i], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			}
		}
		return blockMesh;
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData env)
	{
		BlockMesh blockMesh = m_meshNormal;
		switch (GetKind(Terrain.ExtractData(value)))
		{
		case WormKind.StripedCaterpillar:
			if (m_meshCaterpillar != null)
			{
				blockMesh = m_meshCaterpillar;
			}
			break;
		case WormKind.Earthworm:
			if (m_meshEarthworm != null)
			{
				blockMesh = m_meshEarthworm;
			}
			break;
		case WormKind.TitanitusColossus:
			if (m_meshTitan != null)
			{
				blockMesh = m_meshTitan;
			}
			break;
		}
		BlocksManager.DrawMeshBlock(r, blockMesh, color, 1f * size, ref matrix, env);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, WormKind.Normal));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, WormKind.StripedCaterpillar));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, WormKind.Earthworm));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetKind(0, WormKind.TitanitusColossus));
	}

	public override string GetDisplayName(SubsystemTerrain st, int value)
	{
		int kind = (int)GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"WormBlock:{kind}", "DisplayName");
	}
}
