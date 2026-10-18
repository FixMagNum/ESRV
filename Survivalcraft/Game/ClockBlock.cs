using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public class ClockBlock : Block
{
	public enum ClockType
	{
		Iron,
		Fur,
		Leather,
		Diamond,
		Copper
	}

	public const int Index = 380;

	private BlockMesh m_hourHandMesh;

	private BlockMesh m_secondHandMesh;

	private const int TypesCount = 5;

	private BlockMesh[][] m_blockMeshes = new BlockMesh[5][];

	private BoundingBox[][] m_collisionBoxes = new BoundingBox[5][];

	private BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[5];

	public override void Initialize()
	{
		string[] array = new string[5] { "Models/Clock", "Models/Fur_Clock", "Models/Leather_Clock", "Models/Diamond_Clock", "Models/Copper_Clock" };
		for (int i = 0; i < 5; i++)
		{
			ModelMesh modelMesh = ContentManager.Get<Model>(array[i]).FindMesh("Clock");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
			m_blockMeshes[i] = new BlockMesh[6];
			m_collisionBoxes[i] = new BoundingBox[6];
			for (int j = 0; j < 6; j++)
			{
				Matrix matrix = Matrix.Identity;
				switch (j)
				{
				case 0:
					matrix = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f);
					break;
				case 1:
					matrix = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY((float)Math.PI / 2f);
					break;
				case 2:
					matrix = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY((float)Math.PI);
					break;
				case 3:
					matrix = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY(-(float)Math.PI / 2f);
					break;
				case 4:
					matrix = Matrix.CreateTranslation(0.5f, 0f, 0.5f);
					break;
				case 5:
					matrix = Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f);
					break;
				}
				matrix = boneAbsoluteTransform * matrix;
				BlockMesh blockMesh = new BlockMesh();
				blockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_blockMeshes[i][j] = blockMesh;
				m_collisionBoxes[i][j] = blockMesh.CalculateBoundingBox();
			}
			Matrix matrix2 = Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateRotationZ((float)Math.PI / 2f);
			m_standaloneBlockMeshes[i] = new BlockMesh();
			m_standaloneBlockMeshes[i].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		Model model = ContentManager.Get<Model>("Models/Clock");
		ModelMesh modelMesh2 = model.FindMesh("Hour hand");
		ModelMesh modelMesh3 = model.FindMesh("Second hand");
		Matrix matrix3 = BlockMesh.GetBoneAbsoluteTransform(modelMesh2.ParentBone) * Matrix.CreateTranslation(-0.1f, 0f, 0f);
		Matrix matrix4 = BlockMesh.GetBoneAbsoluteTransform(modelMesh3.ParentBone) * Matrix.CreateTranslation(-0.1f, 0f, 0f);
		m_hourHandMesh = new BlockMesh();
		m_secondHandMesh = new BlockMesh();
		m_hourHandMesh.AppendModelMeshPart(modelMesh2.MeshParts[0], matrix3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_secondHandMesh.AppendModelMeshPart(modelMesh3.MeshParts[0], matrix4, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain _, ComponentMiner miner, int value, TerrainRaycastResult hit)
	{
		int face = hit.CellFace.Face;
		if (face < 0 || face > 5)
		{
			return default;
		}
		int data = Terrain.ExtractData(value);
		data = SetMountingFace(data, face);
		int value2 = Terrain.ReplaceData(value, data);
		return new BlockPlacementData
		{
			Value = value2,
			CellFace = hit.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain _, int value)
	{
		int data = Terrain.ExtractData(value);
		int clockType = (int)GetClockType(data);
		int mountingFace = GetMountingFace(data);
		return new BoundingBox[1] { m_collisionBoxes[clockType][mountingFace] };
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		int clockType = (int)GetClockType(data);
		int mountingFace = GetMountingFace(data);
		g.GenerateMeshVertices(this, x, y, z, m_blockMeshes[clockType][mountingFace], Color.White, null, geom.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData env)
	{
		int clockType = (int)GetClockType(Terrain.ExtractData(value));
		BlocksManager.DrawMeshBlock(r, m_standaloneBlockMeshes[clockType], color, 2f * size, ref matrix, env);
		SubsystemTimeOfDay subsystemTimeOfDay = env?.SubsystemTerrain?.Project?.FindSubsystem<SubsystemTimeOfDay>(throwOnError: false);
		SubsystemTimeOfDay subsystemTimeOfDay2 = subsystemTimeOfDay;
		if (subsystemTimeOfDay2 == null)
		{
			BlocksManager.DrawMeshBlock(r, m_hourHandMesh, color, 2f * size, ref matrix, env);
			BlocksManager.DrawMeshBlock(r, m_secondHandMesh, color, 2f * size, ref matrix, env);
			return;
		}
		DateTime dateTime = DateTime.MinValue.AddDays(subsystemTimeOfDay2.Day).AddHours(6.0);
		float degrees = (float)(((double)dateTime.Hour + (double)dateTime.Minute / 60.0 + (double)dateTime.Second / 3600.0) * 15.0);
		float degrees2 = (float)(((double)dateTime.Minute + (double)dateTime.Second / 60.0) * 6.0);
		Matrix matrix2 = Matrix.CreateRotationX(MathUtils.DegToRad(degrees)) * matrix;
		Matrix matrix3 = Matrix.CreateRotationX(MathUtils.DegToRad(degrees2)) * matrix;
		BlocksManager.DrawMeshBlock(r, m_hourHandMesh, color, 2f * size, ref matrix2, env);
		BlocksManager.DrawMeshBlock(r, m_secondHandMesh, color, 2f * size, ref matrix3, env);
	}

	public static int GetMountingFace(int data)
	{
		return data & 7;
	}

	public static int SetMountingFace(int data, int face)
	{
		return (data & -8) | (face & 7);
	}

	public static ClockType GetClockType(int data)
	{
		return (ClockType)((data >> 3) & 7);
	}

	public static int SetClockType(int data, ClockType t)
	{
		return (data & 7) | ((int)(t & (ClockType)7) << 3);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		for (int t = 0; t < 5; t++)
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetClockType(0, (ClockType)t));
		}
	}

	public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
	{
		string ironIngredient = "clock:" + SetClockType(0, ClockType.Iron).ToString(CultureInfo.InvariantCulture);
		(int, ClockType)[] array = new (int, ClockType)[4]
		{
			(159, ClockType.Leather),
			(207, ClockType.Fur),
			(111, ClockType.Diamond),
			(42, ClockType.Copper)
		};
		(int, ClockType)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(int, ClockType) tuple = array2[i];
			CraftingRecipe craftingRecipe = new CraftingRecipe
			{
				ResultCount = 1,
				ResultValue = Terrain.MakeBlockValue(BlockIndex, 0, SetClockType(0, tuple.Item2)),
				Description = "Clock Reforge"
			};
			craftingRecipe.Ingredients[0] = ironIngredient;
			craftingRecipe.Ingredients[1] = BlocksManager.Blocks[tuple.Item1].CraftingId;
			yield return craftingRecipe;
		}
	}
}
