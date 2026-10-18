using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class CairnBlock : Block
{
	public const int Index = 258;

	private BlockMesh m_mesh = new BlockMesh();

	private BlockMesh m_standaloneMesh = new BlockMesh();

	private BoundingBox[] m_collisionBoxes;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Cairn");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Cairn").ParentBone);
		Color i = Color.White;
		BlockMesh num = new BlockMesh();
		num.AppendModelMeshPart(model.FindMesh("Cairn").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, i);
		BlockMesh num2 = new BlockMesh();
		num2.AppendModelMeshPart(model.FindMesh("Wood").MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, i);
		m_mesh.AppendBlockMesh(num);
		m_mesh.AppendBlockMesh(num2);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Cairn").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(1.3f) * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, i);
		m_standaloneMesh.AppendModelMeshPart(model.FindMesh("Wood").MeshParts[0], boneAbsoluteTransform * Matrix.CreateScale(1.3f) * Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: true, flipNormals: false, i);
		m_collisionBoxes = new BoundingBox[1] { num.CalculateBoundingBox() };
		base.Initialize();
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateMeshVertices(this, x, y, z, m_mesh, Color.White, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneMesh, color, size, ref matrix, environmentData);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = false;
		Random random = new Random();
		int num = Terrain.ExtractData(oldValue);
		int num2 = (10 + 4 * num) * 3;
		for (int i = 0; i < 3; i++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 79,
				Count = 1
			});
		}
		for (int j = 0; j < num2; j++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 248,
				Count = 1
			});
		}
		for (int k = 0; k < 2; k++)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = 23,
				Count = 1
			});
		}
		List<BlockDropValue> list = new List<BlockDropValue>
		{
			new BlockDropValue
			{
				Value = 150,
				Count = 1
			},
			new BlockDropValue
			{
				Value = 40,
				Count = random.Int(4, 8)
			},
			new BlockDropValue
			{
				Value = 115,
				Count = 1
			},
			new BlockDropValue
			{
				Value = 324,
				Count = 1
			},
			new BlockDropValue
			{
				Value = 330,
				Count = random.Int(2, 12)
			},
			new BlockDropValue
			{
				Value = 331,
				Count = random.Int(2, 12)
			},
			new BlockDropValue
			{
				Value = 111,
				Count = 1
			},
			new BlockDropValue
			{
				Value = 367,
				Count = 1
			}
		};
		list.Add(new BlockDropValue
		{
			Value = 427,
			Count = random.Int(1, 3)
		});
		int index = random.Int(0, list.Count - 1);
		dropValues.Add(list[index]);
	}
}
