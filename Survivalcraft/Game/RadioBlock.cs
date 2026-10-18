using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class RadioBlock : Block
{
	public const int Index = 417;

	private BlockMesh[] m_blockMeshesByData = new BlockMesh[8];

	private BlockMesh m_standaloneBlockMesh;

	private BoundingBox[] m_collisionBoxes;

	public const int OrientationMask = 7;

	public const int TrackMask = 56;

	public const int TrackShift = 3;

	private static readonly Dictionary<Tuple<Point2, int>, BlockMesh> m_antennaPoseMeshes;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Radio");
		ModelMesh dictionary = model.FindMesh("Radio");
		ModelMesh modelMesh = model.FindMesh("Antenna");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(dictionary.ParentBone);
		Matrix screen = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		for (int value = 0; value < 8; value++)
		{
			m_blockMeshesByData[value] = new BlockMesh();
			Matrix matrix = Matrix.CreateRotationY((float)value * ((float)Math.PI / 4f)) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_blockMeshesByData[value].AppendModelMeshPart(dictionary.MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMeshesByData[value].AppendModelMeshPart(modelMesh.MeshParts[0], screen * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		m_standaloneBlockMesh = new BlockMesh();
		m_standaloneBlockMesh.AppendModelMeshPart(dictionary.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], screen * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.6f, 1f))
		};
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value) & 7;
		int trackIndex = GetTrackIndex(value);
		if (num < 0 || num > 7)
		{
			return;
		}
		if (trackIndex == 0)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshesByData[num], Color.White, null, null, geometry.SubsetOpaque);
			return;
		}
		int item = (num & 7) | ((trackIndex & 7) << 3);
		Tuple<Point2, int> result = Tuple.Create(new Point2(x, z), item);
		if (!m_antennaPoseMeshes.TryGetValue(result, out var uri))
		{
			uri = GenerateAntennaMesh(num, x, z, trackIndex);
			m_antennaPoseMeshes[result] = uri;
		}
		generator.GenerateShadedMeshVertices(this, x, y, z, uri, Color.White, null, null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		int data = ((int)Math.Round(MathUtils.Atan2(forward.X, forward.Z) / ((float)Math.PI / 4f)) % 8 + 8) % 8;
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, 0);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, 0),
			Count = 1
		});
		showDebris = true;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public static int GetTrackIndex(int value)
	{
		return (Terrain.ExtractData(value) & 0x38) >> 3;
	}

	public static int SetTrackIndex(int value, int trackIndex)
	{
		int num = Terrain.ExtractData(value);
		num = (num & -57) | ((MathUtils.Clamp(trackIndex, 0, 7) << 3) & 0x38);
		return Terrain.ReplaceData(value, num);
	}

	private BlockMesh GenerateAntennaMesh(int orientation, int x, int z, int trackIndex)
	{
		Model model = ContentManager.Get<Model>("Models/Radio");
		ModelMesh modelMesh = model.FindMesh("Radio");
		ModelMesh modelMesh2 = model.FindMesh("Antenna");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone);
		Matrix matrix = Matrix.CreateRotationY((float)orientation * ((float)Math.PI / 4f)) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
		Matrix matrix2 = ComputeAntennaLocalRotation(x, z, trackIndex);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		blockMesh.AppendModelMeshPart(modelMesh2.MeshParts[0], matrix2 * boneAbsoluteTransform2 * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		return blockMesh;
	}

	private Matrix ComputeAntennaLocalRotation(int x, int z, int trackIndex)
	{
		float num = (float)x * 0.2f;
		float num2 = (float)z * 0.2f;
		float num3 = 1000f + (float)(trackIndex * 31);
		float num4 = 2000f + (float)(trackIndex * 47);
		float degrees = MathUtils.Lerp(0f, 360f, SimplexNoise.Noise(num + num3, num2 + num3));
		float degrees2 = MathUtils.Lerp(10f, 160f, SimplexNoise.Noise(num + num4, num2 + num4));
		float radians = MathUtils.DegToRad(degrees);
		float num5 = MathUtils.DegToRad(degrees2);
		return Matrix.CreateRotationY(radians) * Matrix.CreateRotationX(0f - num5);
	}

	static RadioBlock()
	{
		m_antennaPoseMeshes = new Dictionary<Tuple<Point2, int>, BlockMesh>();
	}
}
