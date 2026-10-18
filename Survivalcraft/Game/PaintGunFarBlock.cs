using Engine;
using Engine.Graphics;

namespace Game;

public class PaintGunFarBlock : Block
{
	public const int Index = 411;

	private BlockMesh m_mesh;

	private BlockMesh[] m_canisterMeshes;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/PaintGun");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("PaintGun").ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("PaintGunFar").ParentBone);
		m_mesh = new BlockMesh();
		m_mesh.AppendModelMeshPart(model.FindMesh("PaintGun").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_mesh.AppendModelMeshPart(model.FindMesh("PaintGunFar").MeshParts[0], boneAbsoluteTransform2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_canisterMeshes = new BlockMesh[6];
		for (int i = 0; i < 6; i++)
		{
			ModelMesh modelMesh = model.FindMesh("Canister" + (i + 1));
			if (modelMesh != null)
			{
				Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
				BlockMesh blockMesh = new BlockMesh();
				blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_canisterMeshes[i] = blockMesh;
			}
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geo, int value, int x, int y, int z)
	{
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		int d = Terrain.ExtractData(value);
		int fluid = PaintGunNearBlock.GetFluid(d);
		bool cleaner = PaintGunNearBlock.GetCleaner(d);
		int color2 = PaintGunNearBlock.GetColor(d);
		BlocksManager.DrawMeshBlock(r, m_mesh, color, 2f * size, ref m, env);
		if (fluid > 0)
		{
			int num = MathUtils.Clamp((fluid - 1) / 10, 0, 5);
			BlockMesh blockMesh = m_canisterMeshes[num];
			if (blockMesh != null)
			{
				Color color3 = (cleaner ? Color.White : SubsystemPalette.GetColor(env, color2));
				BlocksManager.DrawMeshBlock(r, blockMesh, color3, 2f * size, ref m, env);
			}
		}
	}

	public static int GetColor(int d)
	{
		return d & 0xF;
	}

	public static int SetColor(int d, int c)
	{
		return (d & -16) | (c & 0xF);
	}

	public static bool GetCleaner(int d)
	{
		return (d & 0x400) != 0;
	}

	public static int SetCleaner(int d, bool b)
	{
		if (!b)
		{
			return d & -1025;
		}
		return d | 0x400;
	}

	public static int GetFluid(int d)
	{
		return (d >> 4) & 0x3F;
	}

	public static int SetFluid(int d, int f)
	{
		d &= -1009;
		return d | ((MathUtils.Clamp(f, 0, 63) & 0x3F) << 4);
	}

	public override int GetDamage(int value)
	{
		return (Terrain.ExtractData(value) >> 11) & 0x7F;
	}

	public override int SetDamage(int value, int damage)
	{
		int num = Terrain.ExtractData(value);
		num &= -260097;
		num |= MathUtils.Clamp(damage, 0, 127) << 11;
		return Terrain.ReplaceData(value, num);
	}
}
