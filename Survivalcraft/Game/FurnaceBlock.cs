using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class FurnaceBlock : Block
{
	public const int Index = 64;

	private BlockMesh m_standaloneBlockMesh;

	public const int LitIndex = 65;

	private BlockMesh[] m_baseMeshes = new BlockMesh[4];

	private BlockMesh[] m_leftPipeMeshes = new BlockMesh[4];

	private BlockMesh[] m_upperPipeMeshes = new BlockMesh[4];

	private BlockMesh[] m_lidMeshes = new BlockMesh[4];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/ClayFurnace");
		ModelMesh modelMesh = model.FindMesh("Furnace");
		ModelMesh modelMesh2 = model.FindMesh("Lid");
		ModelMesh modelMesh3 = model.FindMesh("LeftPipe");
		ModelMesh modelMesh4 = model.FindMesh("UpperPipe");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone);
		Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(modelMesh3.ParentBone);
		Matrix boneAbsoluteTransform4 = BlockMesh.GetBoneAbsoluteTransform(modelMesh4.ParentBone);
		for (int i = 0; i < 4; i++)
		{
			m_baseMeshes[i] = new BlockMesh();
			m_lidMeshes[i] = new BlockMesh();
			m_leftPipeMeshes[i] = new BlockMesh();
			m_upperPipeMeshes[i] = new BlockMesh();
			Matrix matrix = Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_baseMeshes[i].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_lidMeshes[i].AppendModelMeshPart(modelMesh2.MeshParts[0], boneAbsoluteTransform2 * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_leftPipeMeshes[i].AppendModelMeshPart(modelMesh3.MeshParts[0], boneAbsoluteTransform3 * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_upperPipeMeshes[i].AppendModelMeshPart(modelMesh4.MeshParts[0], boneAbsoluteTransform4 * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		Matrix matrix2 = Matrix.CreateTranslation(0f, -0.5f, 0f);
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh2.MeshParts[0], boneAbsoluteTransform2 * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int node = Terrain.ExtractData(value);
		if (node >= 4)
		{
			node = 0;
		}
		generator.GenerateShadedMeshVertices(this, x, y, z, m_baseMeshes[node], Color.White, null, null, geometry.SubsetAlphaTest);
		SubsystemTerrain subsystemTerrain = generator.SubsystemTerrain;
		if (Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x, y + 1, z)) != 439)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_lidMeshes[node], Color.White, null, null, geometry.SubsetAlphaTest);
		}
		Point2 leftOffset = GetLeftOffset(node);
		int num = Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x + leftOffset.X, y, z + leftOffset.Y));
		if (num == 64 || num == 65)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_leftPipeMeshes[node], Color.White, null, null, geometry.SubsetAlphaTest);
		}
		int num2 = Terrain.ExtractContents(subsystemTerrain.Terrain.GetCellValue(x, y - 1, z));
		if (num2 == 64 || num2 == 65)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_upperPipeMeshes[node], Color.White, null, null, geometry.SubsetAlphaTest);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float num = Vector3.Dot(forward, Vector3.UnitZ);
		float num2 = Vector3.Dot(forward, Vector3.UnitX);
		float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
		float num4 = Vector3.Dot(forward, -Vector3.UnitX);
		int data = 0;
		if (num == MathUtils.Max(num, num2, num3, num4))
		{
			data = 2;
		}
		else if (num2 == MathUtils.Max(num, num2, num3, num4))
		{
			data = 3;
		}
		else if (num3 == MathUtils.Max(num, num2, num3, num4))
		{
			data = 0;
		}
		else if (num4 == MathUtils.Max(num, num2, num3, num4))
		{
			data = 1;
		}
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, 64), data),
			CellFace = raycastResult.CellFace
		};
	}

	public FurnaceBlock()
	{
		m_standaloneBlockMesh = new BlockMesh();
	}

	private Point2 GetLeftOffset(int data)
	{
		return data switch
		{
			0 => new Point2(-1, 0), 
			1 => new Point2(0, 1), 
			2 => new Point2(1, 0), 
			3 => new Point2(0, -1), 
			_ => new Point2(0, 0), 
		};
	}
}
