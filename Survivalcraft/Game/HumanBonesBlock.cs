using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class HumanBonesBlock : Block, IPaintableBlock, IWaterloggable
{
	public const int Index = 363;

	private BlockMesh m_blockMesh;

	private BoundingBox[] m_collisionBoxes;

	private Matrix[] m_defaultBoneTransforms;

	private static readonly Vector2[] m_textureOffsets;

	private static Dictionary<Tuple<Point2, int>, BlockMesh> m_poseMeshes;

	private static Dictionary<int, BlockMesh> m_iconMeshes;

	public const int WaterLoggedBit = 65536;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/HumanBones");
		m_defaultBoneTransforms = new Matrix[model.Bones.Count];
		model.CopyAbsoluteBoneTransformsTo(m_defaultBoneTransforms);
		m_blockMesh = GeneratePoseMesh(0, 0);
		m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.1f, 1f))
		};
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geo, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsWaterLogged(num) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(g.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(g.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(g, x, y, z, sideColor, topColor, geo.TransparentSubsetsByFace);
		}
		Color color2 = SubsystemPalette.GetColor(g, PaintedCubeBlock.GetColor(num));
		int variant = (num >> 5) & 3;
		int num2 = (num >> 7) & 7;
		int num3 = (num >> 10) & 0xF;
		float num4 = (num >> 14) & 3;
		if (num2 >= m_textureOffsets.Length)
		{
			num2 = 0;
		}
		int item = (num3 << 3) | num2;
		Tuple<Point2, int> key = Tuple.Create(new Point2(x, z), item);
		if (!m_poseMeshes.TryGetValue(key, out var value2))
		{
			value2 = GeneratePoseMesh(x, z, num2, num3);
			m_poseMeshes[key] = value2;
		}
		Vector3 scaleByVariant = GetScaleByVariant(variant);
		float radians = num4 * ((float)Math.PI / 2f);
		Matrix value3 = Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f) * Matrix.CreateScale(scaleByVariant);
		g.GenerateMeshVertices(this, x, y, z, value2, color2, value3, geo.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = Terrain.ExtractData(value);
		int variant = (num >> 5) & 3;
		int num2 = (num >> 7) & 7;
		color *= SubsystemPalette.GetColor(environmentData, PaintedCubeBlock.GetColor(num));
		if (!m_iconMeshes.TryGetValue(num2, out var value2))
		{
			value2 = GenerateIconMesh(num2);
			m_iconMeshes[num2] = value2;
		}
		if (environmentData != null && environmentData.BillboardDirection.HasValue && environmentData.SubsystemTerrain != null)
		{
			Vector3 vector = new Vector3(0.5f, 0f, 0.5f);
			Matrix matrix2 = Matrix.CreateTranslation(-vector) * Matrix.CreateScale(1.5f) * Matrix.CreateTranslation(vector) * matrix;
			BlocksManager.DrawMeshBlock(primitivesRenderer, value2, color, size, ref matrix2, environmentData);
		}
		else
		{
			Vector3 scaleByVariant = GetScaleByVariant(variant);
			Matrix matrix3 = matrix * Matrix.CreateScale(scaleByVariant);
			BlocksManager.DrawMeshBlock(primitivesRenderer, value2, color, size, ref matrix3, environmentData);
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int variant = (Terrain.ExtractData(value) >> 5) & 3;
		Vector3 scaleByVariant = GetScaleByVariant(variant);
		BoundingBox[] collisionBoxes = m_collisionBoxes;
		BoundingBox[] array = new BoundingBox[collisionBoxes.Length];
		Matrix m = Matrix.CreateScale(scaleByVariant);
		for (int i = 0; i < collisionBoxes.Length; i++)
		{
			BoundingBox boundingBox = collisionBoxes[i];
			Vector3 min = Vector3.Transform(boundingBox.Min - new Vector3(0.5f, 0f, 0.5f), m) + new Vector3(0.5f, 0f, 0.5f);
			Vector3 max = Vector3.Transform(boundingBox.Max - new Vector3(0.5f, 0f, 0.5f), m) + new Vector3(0.5f, 0f, 0.5f);
			array[i] = new BoundingBox(min, max);
		}
		return array;
	}

	private Vector2 GenerateRotation(int x, int z, float offset, float degrees)
	{
		float block2 = SimplexNoise.Noise((float)x * 0.2f + offset, (float)z * 0.2f + offset);
		float f = SimplexNoise.Noise((float)x * 0.2f + offset + 100f, (float)z * 0.2f + offset + 100f);
		float x2 = MathUtils.Lerp(0f - MathUtils.DegToRad(degrees), MathUtils.DegToRad(degrees), block2);
		float y = MathUtils.Lerp(0f - MathUtils.DegToRad(degrees), MathUtils.DegToRad(degrees), f);
		return new Vector2(x2, y);
	}

	static HumanBonesBlock()
	{
		m_textureOffsets = new Vector2[6]
		{
			new Vector2(0.0625f, 0.625f),
			new Vector2(0.1875f, 0.6875f),
			new Vector2(0.9375f, 0.1875f),
			new Vector2(0.375f, 0.6875f),
			new Vector2(0.1875f, 0.875f),
			new Vector2(0.375f, 0.375f)
		};
		m_iconMeshes = new Dictionary<int, BlockMesh>();
		m_poseMeshes = new Dictionary<Tuple<Point2, int>, BlockMesh>();
	}

	private Vector3 GetScaleByVariant(int variant)
	{
		return variant switch
		{
			0 => new Vector3(0.85f, 1f, 0.85f), 
			1 => new Vector3(1.1f, 1.1f, 1.1f), 
			2 => new Vector3(1f, 1f, 1f), 
			3 => new Vector3(0.5f, 1f, 0.5f), 
			_ => Vector3.One, 
		};
	}

	public int? GetPaintColor(int value)
	{
		return PaintedCubeBlock.GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		data = PaintedCubeBlock.SetColor(data, color);
		return Terrain.ReplaceData(value, data);
	}

	private BlockMesh GeneratePoseMesh(int x, int z, int textureVariant = 0, int poseIndex = 0)
	{
		Model model = ContentManager.Get<Model>("Models/HumanBones");
		Matrix[] array = new Matrix[model.Bones.Count];
		model.CopyAbsoluteBoneTransformsTo(array);
		float yaw = 0f;
		float num = 0f;
		float num2 = 0f;
		switch (poseIndex)
		{
		case 0:
		{
			switch ((int)MathUtils.Floor(MathUtils.Saturate(SimplexNoise.Noise((float)x * 0.2f + 2100f, (float)z * 0.2f + 2100f)) * 4f))
			{
			case 0:
				num = MathUtils.DegToRad(90f);
				break;
			case 1:
				num = MathUtils.DegToRad(-90f);
				break;
			case 2:
				num2 = MathUtils.DegToRad(90f);
				break;
			case 3:
				num2 = MathUtils.DegToRad(-90f);
				break;
			}
			yaw = MathUtils.Lerp(0f - MathUtils.DegToRad(90f), MathUtils.DegToRad(90f), SimplexNoise.Noise((float)x * 0.2f + 2200f, (float)z * 0.2f + 2200f));
			float num3 = MathUtils.Lerp(0f - MathUtils.DegToRad(15f), MathUtils.DegToRad(15f), SimplexNoise.Noise((float)x * 0.2f + 2300f, (float)z * 0.2f + 2300f));
			num += num3;
			num2 += num3;
			break;
		}
		case 3:
			num = MathUtils.DegToRad(-90f);
			break;
		case 8:
			num = MathUtils.DegToRad(90f);
			break;
		case 4:
			num2 = MathUtils.DegToRad(180f);
			break;
		}
		Matrix matrix = Matrix.CreateFromYawPitchRoll(yaw, num, num2);
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = Vector2.Zero;
		Vector2 vector3 = Vector2.Zero;
		Vector2 vector4 = Vector2.Zero;
		Vector2 vector5 = Vector2.Zero;
		switch (poseIndex)
		{
		case 0:
			vector = GenerateRotation(x, z, 1100f, 45f);
			vector2 = GenerateRotation(x, z, 1300f, 90f);
			vector3 = GenerateRotation(x, z, 1500f, 90f);
			vector4 = GenerateRotation(x, z, 1700f, 90f);
			vector5 = GenerateRotation(x, z, 1900f, 90f);
			break;
		case 1:
			vector = new Vector2(MathUtils.DegToRad(20f), 0f);
			vector2 = new Vector2(MathUtils.DegToRad(-130f), MathUtils.DegToRad(30f));
			vector3 = new Vector2(MathUtils.DegToRad(-130f), MathUtils.DegToRad(-30f));
			break;
		case 2:
			vector = new Vector2(MathUtils.DegToRad(20f), 0f);
			vector5 = new Vector2(MathUtils.DegToRad(90f), 0f);
			vector4 = vector5;
			break;
		case 3:
			vector2 = new Vector2(0f, MathUtils.DegToRad(-90f));
			vector3 = new Vector2(0f, MathUtils.DegToRad(90f));
			vector4 = new Vector2(0f, MathUtils.DegToRad(-45f));
			vector5 = new Vector2(0f, MathUtils.DegToRad(45f));
			break;
		case 4:
			vector3 = new Vector2(MathUtils.DegToRad(-180f), 0f);
			vector2 = vector3;
			vector4 = new Vector2(MathUtils.DegToRad(140f), 0f);
			break;
		case 5:
			vector = new Vector2(MathUtils.DegToRad(-60f), 0f);
			break;
		case 6:
			vector = new Vector2(MathUtils.DegToRad(-40f), 0f);
			vector2 = new Vector2(MathUtils.DegToRad(40f), MathUtils.DegToRad(-30f));
			vector3 = new Vector2(MathUtils.DegToRad(40f), MathUtils.DegToRad(30f));
			vector5 = new Vector2(MathUtils.DegToRad(90f), 0f);
			vector4 = vector5;
			break;
		case 7:
			vector3 = new Vector2(MathUtils.DegToRad(100f), 0f);
			vector2 = vector3;
			break;
		case 8:
			vector2 = new Vector2(MathUtils.DegToRad(-170f), MathUtils.DegToRad(-40f));
			vector3 = new Vector2(MathUtils.DegToRad(-170f), MathUtils.DegToRad(40f));
			vector4 = new Vector2(0f, MathUtils.DegToRad(-25f));
			vector5 = new Vector2(0f, MathUtils.DegToRad(25f));
			break;
		}
		BlockMesh blockMesh = new BlockMesh();
		foreach (ModelMesh mesh in model.Meshes)
		{
			Vector2 vector6 = Vector2.Zero;
			switch (mesh.Name)
			{
			case "Leg2":
				vector6 = vector5;
				break;
			case "Leg1":
				vector6 = vector4;
				break;
			case "Hand2":
				vector6 = vector3;
				break;
			case "Hand1":
				vector6 = vector2;
				break;
			case "Head":
				vector6 = vector;
				break;
			}
			Matrix matrix2 = Matrix.CreateRotationY(vector6.Y) * Matrix.CreateRotationX(vector6.X) * array[mesh.ParentBone.Index] * matrix * Matrix.CreateTranslation(0.5f, 0.1f, 0.5f);
			blockMesh.AppendModelMeshPart(mesh.MeshParts[0], matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		BoundingBox boundingBox = blockMesh.CalculateBoundingBox();
		Vector3 vector7 = (boundingBox.Min + boundingBox.Max) * 0.5f;
		float num4;
		switch (poseIndex)
		{
		default:
			num4 = 0.7f;
			break;
		case 2:
		case 6:
			num4 = 0.5f;
			break;
		case 0:
		case 3:
		case 8:
			num4 = 0.1f;
			break;
		}
		float y = num4;
		float num5;
		switch (poseIndex)
		{
		default:
			num5 = 0f;
			break;
		case 4:
		case 5:
			num5 = 0.03f;
			break;
		case 2:
		case 6:
			num5 = 0.06f;
			break;
		}
		float num6 = num5;
		blockMesh.TransformPositions(Matrix.CreateTranslation(new Vector3(0.5f, y, 0.5f + num6) - vector7));
		if (textureVariant < m_textureOffsets.Length)
		{
			Vector2 vector8 = m_textureOffsets[textureVariant];
			blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(vector8.X, vector8.Y, 0f));
		}
		return blockMesh;
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		int num = Terrain.ExtractData(oldValue);
		num &= -80897;
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.ReplaceData(oldValue, num),
			Count = 1
		});
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		for (int tex = 0; tex < m_textureOffsets.Length; tex++)
		{
			int num = 0;
			num |= (tex & 7) << 7;
			yield return Terrain.MakeBlockValue(BlockIndex, 0, num);
		}
	}

	private BlockMesh GenerateIconMesh(int textureVariant)
	{
		ModelMesh modelMesh = ContentManager.Get<Model>("Models/Skeleton_Icon").FindMesh("Skeleton_Icon");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		if (textureVariant < m_textureOffsets.Length)
		{
			Vector2 vector = m_textureOffsets[textureVariant];
			blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(vector.X, vector.Y, 0f));
		}
		return blockMesh;
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x10000) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (isWaterLogged)
		{
			return data | 0x10000;
		}
		return data & -65537;
	}
}
