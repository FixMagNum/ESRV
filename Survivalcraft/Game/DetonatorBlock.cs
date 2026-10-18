using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class DetonatorBlock : MountedElectricElementBlock
{
	public const int Index = 147;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshesByData = new BlockMesh[6];

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[6][];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Detonator");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Detonator").ParentBone);
		for (int i = 0; i < 6; i++)
		{
			int num = i;
			Matrix matrix;
			if (i >= 4)
			{
				matrix = ((i != 4) ? (Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f)) : Matrix.CreateTranslation(0.5f, 0f, 0.5f));
			}
			else
			{
				matrix = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
			}
			m_blockMeshesByData[num] = new BlockMesh();
			m_blockMeshesByData[num].AppendModelMeshPart(model.FindMesh("Detonator").MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_collisionBoxesByData[num] = new BoundingBox[1] { m_blockMeshesByData[num].CalculateBoundingBox() };
		}
		Matrix matrix2 = Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateRotationZ((float)Math.PI / 2f);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Detonator").MeshParts[0], boneAbsoluteTransform * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
	}

	public override int GetFace(int value)
	{
		return Terrain.ExtractData(value) & 7;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(value, raycastResult.CellFace.Face),
			CellFace = raycastResult.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = Terrain.ExtractData(value) & 7;
		if (num >= m_collisionBoxesByData.Length || m_collisionBoxesByData[num] == null)
		{
			return base.GetCustomCollisionBoxes(terrain, value);
		}
		return m_collisionBoxesByData[num];
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		DrawWaterInside(generator, geometry, x, y, z, num);
		int num2 = num & 7;
		if (num2 < m_blockMeshesByData.Length && m_blockMeshesByData[num2] != null)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByData[num2], Color.White, null, geometry.SubsetOpaque);
			generator.GenerateWireVertices(value, x, y, z, GetFace(value), 0.125f, Vector2.Zero, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 4f * size, ref matrix, environmentData);
	}

	public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return new DetonatorElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));
	}

	public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int num = GetFace(value);
		if (face == num && SubsystemElectricity.GetConnectorDirection(num, 0, connectorFace).HasValue)
		{
			return ElectricConnectorType.Input;
		}
		return null;
	}
}
