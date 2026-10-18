using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class LitFurnaceBlock2 : Block
{
	public const int Index = 304;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshesByData = new BlockMesh[4];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/LitFurnace2");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("LitFurnace2").ParentBone);
		for (int i = 0; i < 4; i++)
		{
			m_blockMeshesByData[i] = new BlockMesh();
			Matrix identity = Matrix.Identity;
			identity *= Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_blockMeshesByData[i].AppendModelMeshPart(model.FindMesh("LitFurnace2").MeshParts[0], boneAbsoluteTransform * identity, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("LitFurnace2").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		return false;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (num < m_blockMeshesByData.Length)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshesByData[num], Color.White, null, null, geometry.SubsetAlphaTest);
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
		float x = Vector3.Dot(forward, -Vector3.UnitX);
		int num4 = 0;
		float num5 = MathUtils.Max(num, num2, num3, x);
		if (num == num5)
		{
			num4 = 2;
		}
		else
		{
			num4 = ((num2 == num5) ? 3 : ((num3 != num5) ? 1 : 0));
		}
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, BlockIndex), num4),
			CellFace = raycastResult.CellFace
		};
	}
}
