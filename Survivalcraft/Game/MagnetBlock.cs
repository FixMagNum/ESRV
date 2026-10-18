using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class MagnetBlock : Block, IWaterloggable
{
	public const int Index = 167;

	public BlockMesh[] m_meshesByData = new BlockMesh[2];

	public BlockMesh m_standaloneMesh = new BlockMesh();

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[2][];

	public const int WaterLoggedBit = 1024;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Magnet");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Magnet").ParentBone);
		for (int i = 0; i < 2; i++)
		{
			m_meshesByData[i] = new BlockMesh();
			m_meshesByData[i].AppendModelMeshPart(model.FindMesh("Magnet").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationY((float)Math.PI / 2f * (float)i) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
			m_collisionBoxesByData[i] = new BoundingBox[1] { m_meshesByData[i].CalculateBoundingBox() };
		}
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Magnet").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(1.5f) * Matrix.CreateTranslation(0f, -0.25f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = Terrain.ExtractData(value) & 1;
		if (num < m_collisionBoxesByData.Length)
		{
			return m_collisionBoxesByData[num];
		}
		return null;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsWaterLogged(num) && BlocksManager.Blocks[18] is FocalorsFluidBlock clothingData)
		{
			Color matrix2;
			Color color = (matrix2 = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			matrix2.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			clothingData.GenerateWaterloggedFluidVertices(generator, x, y, z, matrix2, topColor, geometry.TransparentSubsetsByFace);
		}
		int clothingColor = num & 1;
		if (clothingColor < m_collisionBoxesByData.Length)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_meshesByData[clothingColor], Color.White, null, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		int data = ((MathUtils.Abs(forward.X) <= MathUtils.Abs(forward.Z)) ? 1 : 0);
		return new BlockPlacementData
		{
			CellFace = raycastResult.CellFace,
			Value = Terrain.ReplaceData(value, data)
		};
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x400) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (isWaterLogged)
		{
			return data | 0x400;
		}
		return data & -1025;
	}
}
