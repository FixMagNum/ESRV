using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public class LanternHumanSkullBlock : Block
{
	public const int Index = 420;

	private BlockMesh[,] m_blockMeshes = new BlockMesh[8, 8];

	private BlockMesh[] m_standaloneByTexture;

	private BoundingBox[] m_collisionBoxes;

	public const int OrientationMask = 7;

	public const int TextureVariantShift = 7;

	public const int TextureVariantMask = 896;

	private static readonly Vector2[] m_textureOffsets;

	public override void Initialize()
	{
		ModelMesh modelMesh = ContentManager.Get<Model>("Models/HumanBones").FindMesh("Head");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		Matrix matrix = Matrix.CreateRotationX(MathUtils.DegToRad(20f)) * boneAbsoluteTransform;
		BlockMesh blockMesh = new BlockMesh();
		blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		BoundingBox boundingBox = blockMesh.CalculateBoundingBox();
		Vector3 vector = (boundingBox.Min + boundingBox.Max) * 0.5f;
		Vector3 position = new Vector3(0.5f, 0f, 0.5f) - new Vector3(vector.X, boundingBox.Min.Y, vector.Z);
		blockMesh.TransformPositions(Matrix.CreateTranslation(position));
		int num = m_textureOffsets.Length;
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < num; j++)
			{
				BlockMesh blockMesh2 = new BlockMesh();
				blockMesh2.AppendModelMeshPart(modelMesh.MeshParts[0], matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				blockMesh2.TransformPositions(Matrix.CreateTranslation(position));
				Matrix matrix2 = Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateRotationY((float)i * ((float)Math.PI / 4f)) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				blockMesh2.TransformPositions(matrix2);
				Vector2 vector2 = m_textureOffsets[j];
				blockMesh2.TransformTextureCoordinates(Matrix.CreateTranslation(vector2.X, vector2.Y, 0f));
				m_blockMeshes[i, j] = blockMesh2;
			}
		}
		m_standaloneByTexture = new BlockMesh[num];
		for (int k = 0; k < num; k++)
		{
			BlockMesh blockMesh3 = new BlockMesh();
			blockMesh3.AppendModelMeshPart(modelMesh.MeshParts[0], matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			blockMesh3.TransformPositions(Matrix.CreateTranslation(position));
			blockMesh3.TransformPositions(Matrix.CreateTranslation(0f, -0.5f, 0f));
			Vector2 vector3 = m_textureOffsets[k];
			blockMesh3.TransformTextureCoordinates(Matrix.CreateTranslation(vector3.X, vector3.Y, 0f));
			m_standaloneByTexture[k] = blockMesh3;
		}
		BoundingBox boundingBox2 = blockMesh.CalculateBoundingBox();
		Vector3 min = new Vector3(MathUtils.Clamp(boundingBox2.Min.X, 0f, 1f), MathUtils.Clamp(boundingBox2.Min.Y, 0f, 1f), MathUtils.Clamp(boundingBox2.Min.Z, 0f, 1f));
		Vector3 max = new Vector3(MathUtils.Clamp(boundingBox2.Max.X, 0f, 1f), MathUtils.Clamp(boundingBox2.Max.Y, 0f, 1f), MathUtils.Clamp(boundingBox2.Max.Z, 0f, 1f));
		m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(min, max)
		};
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geo, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		int num2 = num & 7;
		int num3 = (num >> 7) & 7;
		if (num3 >= m_textureOffsets.Length)
		{
			num3 = 0;
		}
		g.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshes[num2, num3], Color.White, null, null, geo.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = (Terrain.ExtractData(value) >> 7) & 7;
		if (num >= m_textureOffsets.Length)
		{
			num = 0;
		}
		if (environmentData != null && environmentData.BillboardDirection.HasValue && environmentData.SubsystemTerrain != null)
		{
			Vector3 vector = new Vector3(0.5f, -0.2f, 0.5f);
			Matrix matrix2 = Matrix.CreateTranslation(-vector) * Matrix.CreateScale(2.7f) * Matrix.CreateTranslation(vector) * matrix;
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneByTexture[num], color, size, ref matrix2, environmentData);
		}
		else
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneByTexture[num], color, size, ref matrix, environmentData);
		}
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		int num = ((int)Math.Round(MathUtils.Atan2(forward.X, forward.Z) / ((float)Math.PI / 4f)) % 8 + 8) % 8;
		int num2 = (Terrain.ExtractData(value) >> 7) & 7;
		int array = (num & 7) | ((num2 & 7) << 7);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, array),
			CellFace = raycastResult.CellFace
		};
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		for (int tex = 0; tex < m_textureOffsets.Length; tex++)
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, (tex & 7) << 7);
		}
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int num = Terrain.ExtractData(oldValue);
		num &= -8;
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.ReplaceData(oldValue, num),
			Count = 1
		});
		showDebris = true;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public static int GetOrientation(int value)
	{
		return Terrain.ExtractData(value) & 7;
	}

	public static int SetOrientation(int value, int orientation)
	{
		int num = Terrain.ExtractData(value);
		num = (num & -8) | (orientation & 7);
		return Terrain.ReplaceData(value, num);
	}

	public static int GetTextureVariant(int value)
	{
		return (Terrain.ExtractData(value) >> 7) & 7;
	}

	public static int SetTextureVariant(int value, int variant)
	{
		int num = Terrain.ExtractData(value);
		num = (num & -897) | ((MathUtils.Clamp(variant, 0, 7) << 7) & 0x380);
		return Terrain.ReplaceData(value, num);
	}

	static LanternHumanSkullBlock()
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
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		string skullId = BlocksManager.Blocks[419].CraftingId;
		string torchId = BlocksManager.Blocks[31].CraftingId;
		int texCount = m_textureOffsets.Length;
		for (int t = 0; t < texCount; t++)
		{
			string text = skullId + ":" + ((t & 7) << 7).ToString(CultureInfo.InvariantCulture);
			int resultValue = Terrain.MakeBlockValue(BlockIndex, 0, (t & 7) << 7);
			CraftingRecipe craftingRecipe = new CraftingRecipe
			{
				ResultCount = 1,
				ResultValue = resultValue,
				Description = "Skull Lantern"
			};
			craftingRecipe.Ingredients[0] = text;
			craftingRecipe.Ingredients[1] = torchId;
			yield return craftingRecipe;
		}
	}
}
