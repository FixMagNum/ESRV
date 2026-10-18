using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class BoiledMeatBlock : FoodBlock
{
	public const int Index = 346;

	private BlockMesh m_driedMesh;

	public BoiledMeatBlock()
		: base("Models/BoiledMeat", Matrix.Identity, new Color(188, 143, 143), 146)
	{
	}

	public static int GetState(int data)
	{
		return data & 3;
	}

	public static int SetState(int data, int state)
	{
		return (data & -4) | (state & 3);
	}

	public override void Initialize()
	{
		base.Initialize();
		Model model = ContentManager.Get<Model>("Models/Dried_Meat");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.Meshes[0].ParentBone);
		m_driedMesh = new BlockMesh();
		m_driedMesh.AppendModelMeshPart(model.Meshes[0].MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_driedMesh.TransformTextureCoordinates(m_tcTransform);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, 0));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, 1));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetState(0, 2));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int state = GetState(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"BoiledMeatBlock:{state}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int state = GetState(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"BoiledMeatBlock:{state}", "Description");
	}

	public override void DrawBlock(PrimitivesRenderer3D pr, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		int data = Terrain.ExtractData(value);
		int state = GetState(data);
		if (FoodBlock.GetFrozen(data))
		{
			color *= new Color(160, 200, 255);
		}
		Color color2;
		switch (state)
		{
		case 0:
			BlocksManager.DrawMeshBlock(pr, m_standaloneBlockMesh, color, 2f * size, ref m, env);
			return;
		default:
			color2 = new Color(130, 85, 60);
			break;
		case 1:
			color2 = new Color(180, 100, 60);
			break;
		}
		Color color3 = color2;
		BlocksManager.DrawMeshBlock(pr, m_driedMesh, color * color3, 2f * size, ref m, env);
	}

	public override int GetRotPeriod(int value)
	{
		return BoiledBirdBlock.GetState(Terrain.ExtractData(value)) switch
		{
			1 => 1600, 
			2 => 750, 
			_ => DefaultRotPeriod, 
		};
	}

	public override int GetDamageDestructionValue(int value)
	{
		return 240;
	}
}
