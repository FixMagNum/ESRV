using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class ChimneyBlock : Block
{
	public enum ChimneyType
	{
		Cobblestone,
		Brick
	}

	public const int Index = 439;

	private BoundingBox[] m_collisionBoxes;

	private BlockMesh m_standaloneBlockMeshCobblestone;

	private BlockMesh m_standaloneBlockMeshBrick;

	private BlockMesh m_cobblestoneMesh;

	private BlockMesh m_cobblestoneFillerMesh;

	private BlockMesh m_brickMesh;

	private BlockMesh m_brickFillerMesh;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Chimney");
		ModelMesh modelMesh = model.FindMesh("Chimney");
		ModelMesh modelMesh2 = model.FindMesh("Filler");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone);
		m_cobblestoneMesh = new BlockMesh();
		m_cobblestoneMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_cobblestoneFillerMesh = new BlockMesh();
		m_cobblestoneFillerMesh.AppendModelMeshPart(modelMesh2.MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMeshCobblestone = new BlockMesh();
		m_standaloneBlockMeshCobblestone.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		ModelMesh modelMesh3 = model.FindMesh("Brick_Chimney", throwIfNotFound: false);
		ModelMesh modelMesh4 = model.FindMesh("Brick_Filler", throwIfNotFound: false);
		m_brickMesh = new BlockMesh();
		m_brickFillerMesh = new BlockMesh();
		m_standaloneBlockMeshBrick = new BlockMesh();
		if (modelMesh3 != null && modelMesh4 != null)
		{
			Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(modelMesh3.ParentBone);
			Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(modelMesh4.ParentBone);
			m_brickMesh.AppendModelMeshPart(modelMesh3.MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_brickFillerMesh.AppendModelMeshPart(modelMesh4.MeshParts[0], boneAbsoluteTransform4 * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMeshBrick.AppendModelMeshPart(modelMesh3.MeshParts[0], boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		else
		{
			m_brickMesh = m_cobblestoneMesh;
			m_brickFillerMesh = m_cobblestoneFillerMesh;
			m_standaloneBlockMeshBrick = m_standaloneBlockMeshCobblestone;
		}
		m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(0.35f, 0f, 0.35f, 0.65f, 1f, 0.65f)
		};
		IsCollidable = true;
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		BlockMesh blockMesh;
		BlockMesh blockMesh2;
		if ((Terrain.ExtractData(value) & 1) == 1)
		{
			blockMesh = m_brickMesh;
			blockMesh2 = m_brickFillerMesh;
		}
		else
		{
			blockMesh = m_cobblestoneMesh;
			blockMesh2 = m_cobblestoneFillerMesh;
		}
		generator.GenerateShadedMeshVertices(this, x, y, z, blockMesh, Color.White, null, null, geometry.SubsetOpaque);
		if (IsSurroundedBySolids(generator.SubsystemTerrain, x, y, z))
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, blockMesh2, Color.White, null, null, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlockMesh blockMesh = (((Terrain.ExtractData(value) & 1) == 1) ? m_standaloneBlockMeshBrick : m_standaloneBlockMeshCobblestone);
		BlocksManager.DrawMeshBlock(primitivesRenderer, blockMesh, color, size, ref matrix, environmentData);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int data = Terrain.ExtractData(oldValue);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			Count = 1
		});
		showDebris = true;
	}

	private bool IsSurroundedBySolids(SubsystemTerrain subsystemTerrain, int x, int y, int z)
	{
		if (Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x + 1, y, z)) != 0 && Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x - 1, y, z)) != 0 && Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x, y, z + 1)) != 0)
		{
			return Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x, y, z - 1)) != 0;
		}
		return false;
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, 0);
		yield return Terrain.MakeBlockValue(BlockIndex, 0, 1);
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int kind = (int)StringBlock.GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"ChimneyBlock:{kind}", "DisplayName");
	}

	public override string GetDescription(int value)
	{
		int kind = (int)StringBlock.GetKind(Terrain.ExtractData(value));
		return LanguageControl.GetBlock($"ChimneyBlock:{kind}", "Description");
	}
}
