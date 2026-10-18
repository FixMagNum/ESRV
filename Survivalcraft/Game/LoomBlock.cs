using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class LoomBlock : Block
{
	public const int Index = 441;

	private BlockMesh m_standaloneBlockMesh = new BlockMesh();

	private BlockMesh[] m_meshesByData = new BlockMesh[16];

	private BoundingBox[][] m_collisionBoxesByData = new BoundingBox[16][];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Radio") ?? ContentManager.Get<Model>("Models/Dirt");
		Matrix matrix = Matrix.CreateTranslation(0f, -0.5f, 0f);
		ModelMesh modelMesh = model.FindMesh("Radio") ?? model.Meshes[0];
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone) * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		for (int i = 0; i < 16; i++)
		{
			float num = GetRotation(i);
			bool flag = IsMainPart(i);
			m_meshesByData[i] = new BlockMesh();
			Matrix matrix2 = Matrix.CreateRotationY(num * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			if (flag)
			{
				m_meshesByData[i].AppendModelMeshPart(modelMesh.MeshParts[0], BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone) * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			}
			else
			{
				ModelMesh modelMesh2 = model.FindMesh("Antenna");
				if (modelMesh2 != null)
				{
					m_meshesByData[i].AppendModelMeshPart(modelMesh2.MeshParts[0], BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone) * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.Gray);
				}
			}
			m_collisionBoxesByData[i] = new BoundingBox[1]
			{
				new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f))
			};
		}
		base.Initialize();
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Point3 point = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
		Point3 point2 = new Point3(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z) + point;
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		int num = 0;
		num = ((!(MathUtils.Abs(forward.X) > MathUtils.Abs(forward.Z))) ? ((!(forward.Z > 0f)) ? 2 : 0) : ((forward.X > 0f) ? 1 : 3));
		Point3[] array = new Point3[4]
		{
			new Point3(0, 0, 1),
			new Point3(1, 0, 0),
			new Point3(0, 0, -1),
			new Point3(-1, 0, 0)
		};
		Point3 p = point2 + array[num];
		if (IsCellFree(subsystemTerrain, point2) && IsCellFree(subsystemTerrain, p) && HasSupport(subsystemTerrain, point2) && HasSupport(subsystemTerrain, p))
		{
			return new BlockPlacementData
			{
				Value = Terrain.MakeBlockValue(441, 0, SetIsMainPart(SetRotation(0, num), isMain: true)),
				CellFace = raycastResult.CellFace
			};
		}
		int num2 = (num + 2) % 4;
		Point3 p2 = point2 + array[num2];
		if (IsCellFree(subsystemTerrain, point2) && IsCellFree(subsystemTerrain, p2) && HasSupport(subsystemTerrain, point2) && HasSupport(subsystemTerrain, p2))
		{
			return new BlockPlacementData
			{
				Value = Terrain.MakeBlockValue(441, 0, SetIsMainPart(SetRotation(0, num), isMain: false)),
				CellFace = raycastResult.CellFace
			};
		}
		return default;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsMainPart(num))
		{
			generator.GenerateMeshVertices(this, x, y, z, m_meshesByData[num], Color.White, null, geometry.SubsetOpaque);
		}
		else
		{
			generator.GenerateMeshVertices(this, x, y, z, m_meshesByData[num], Color.White, null, geometry.SubsetOpaque);
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = Terrain.ExtractData(value);
		return m_collisionBoxesByData[num];
	}

	public static int GetRotation(int data)
	{
		return data & 3;
	}

	public static int SetRotation(int data, int rotation)
	{
		return (data & -4) | (rotation & 3);
	}

	public static bool IsMainPart(int data)
	{
		return (data & 4) != 0;
	}

	public static int SetIsMainPart(int data, bool isMain)
	{
		return (data & -5) | (isMain ? 4 : 0);
	}

	private bool IsCellFree(SubsystemTerrain terrain, Point3 p)
	{
		int cellContents = terrain.Terrain.GetCellContents(p.X, p.Y, p.Z);
		Block block = BlocksManager.Blocks[cellContents];
		if (cellContents != 0)
		{
			return block is CrossBlock;
		}
		return true;
	}

	private bool HasSupport(SubsystemTerrain terrain, Point3 p)
	{
		int cellContents = terrain.Terrain.GetCellContents(p.X, p.Y - 1, p.Z);
		return BlocksManager.Blocks[cellContents].IsCollidable;
	}
}
