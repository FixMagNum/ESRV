using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class RadioAntennaBlock : Block
{
	public const int Index = 418;

	private BlockMesh m_mastMesh;

	private BlockMesh m_mastheadMesh;

	private BlockMesh m_itemMesh;

	private BoundingBox[] m_collision = new BoundingBox[1]
	{
		new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f))
	};

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/RadioAntenna");
		ModelMesh modelMesh = model.FindMesh("Mast");
		ModelMesh modelMesh2 = model.FindMesh("Masthead");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone);
		m_mastMesh = new BlockMesh();
		m_mastMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_mastheadMesh = new BlockMesh();
		m_mastheadMesh.AppendModelMeshPart(modelMesh2.MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_itemMesh = new BlockMesh();
		m_itemMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(418, 0, 0);
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		return 0;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collision;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_itemMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner miner, int value, TerrainRaycastResult rr)
	{
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceContents(value, 418),
			CellFace = rr.CellFace
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator gen, TerrainGeometry g, int value, int x, int y, int z)
	{
		SubsystemTerrain subsystemTerrain = gen.SubsystemTerrain;
		if (Terrain.ExtractContents(value) == 418)
		{
			int i;
			for (i = y; i + 1 <= 255 && Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x, i + 1, z)) == 418; i++)
			{
			}
			int num = y;
			while (num - 1 >= 0 && Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x, num - 1, z)) == 418)
			{
				num--;
			}
			int num2 = i - num + 1;
			bool flag = y == i;
			float num3 = MathUtils.Saturate((float)(num2 - 1) / 100f) * 2f;
			float num4 = ((num2 <= 1) ? 0f : ((float)(y - num) / (float)(num2 - 1)));
			float num5 = (flag ? 1f : MathUtils.Clamp(1f + (1f - num4) * num3, 1f, 3f));
			TerrainGeometrySubset subsetAlphaTest = g.SubsetAlphaTest;
			Matrix? matrix = null;
			if (!flag && num5 != 1f)
			{
				matrix = Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateScale(num5, 1f, num5) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			}
			BlockMesh blockMesh = (flag ? m_mastheadMesh : m_mastMesh);
			gen.GenerateShadedMeshVertices(this, x, y, z, blockMesh, Color.White, matrix, null, subsetAlphaTest);
		}
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (Terrain.ExtractContents(neighborValue) != 418)
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}
}
