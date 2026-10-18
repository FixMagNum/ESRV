using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class AnimalBonesBlock : Block, IWaterloggable
{
	public const int Index = 362;

	private readonly BlockMesh[,] m_blockMeshes;

	private readonly BlockMesh[,] m_standaloneMeshes;

	private BoundingBox[] m_collision;

	private readonly Random m_random;

	private BlockMesh m_iconMesh;

	private readonly BlockMesh[,,] m_blockMeshesEx = new BlockMesh[4, 4, 3];

	private readonly BlockMesh[,,] m_standaloneMeshesEx = new BlockMesh[4, 4, 3];

	private Vector2[] m_textureOffsets;

	public const int WaterLoggedBit = 1024;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/AnimalBones");
		m_textureOffsets = new Vector2[3]
		{
			new Vector2(0.0625f, 0.625f),
			new Vector2(0.9375f, 0.0625f),
			new Vector2(0.125f, 0.5625f)
		};
		for (int i = 0; i < 4; i++)
		{
			string name = ((i == 0) ? "Bones" : ("Bones" + i));
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(name).ParentBone);
			for (int j = 0; j < 4; j++)
			{
				Matrix matrix = Matrix.CreateRotationY((float)((double)j * Math.PI * 0.5)) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				for (int k = 0; k < 3; k++)
				{
					BlockMesh blockMesh = new BlockMesh();
					blockMesh.AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					if (k < m_textureOffsets.Length)
					{
						blockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(m_textureOffsets[k].X, m_textureOffsets[k].Y, 0f));
					}
					m_blockMeshesEx[i, j, k] = blockMesh;
					BlockMesh blockMesh2 = new BlockMesh();
					blockMesh2.AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					if (k < m_textureOffsets.Length)
					{
						blockMesh2.TransformTextureCoordinates(Matrix.CreateTranslation(m_textureOffsets[k].X, m_textureOffsets[k].Y, 0f));
					}
					m_standaloneMeshesEx[i, j, k] = blockMesh2;
				}
			}
		}
		m_iconMesh = new BlockMesh();
		Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bones").ParentBone);
		m_iconMesh.AppendModelMeshPart(model.FindMesh("Bones").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_collision = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.1f, 1f))
		};
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geo, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(g.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(g.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(g, x, y, z, sideColor, topColor, geo.TransparentSubsetsByFace);
		}
		int rotation = GetRotation(data);
		int variant = GetVariant(data);
		int textureIndex = GetTextureIndex(data);
		g.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshesEx[variant, rotation, textureIndex], Color.White, null, null, geo.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D pr, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData env)
	{
		if (env.BillboardDirection.HasValue)
		{
			BlocksManager.DrawMeshBlock(pr, m_iconMesh, color, size, ref matrix, env);
			return;
		}
		int rotation = GetRotation(Terrain.ExtractData(value));
		int variant = GetVariant(Terrain.ExtractData(value));
		int textureIndex = GetTextureIndex(Terrain.ExtractData(value));
		BlocksManager.DrawMeshBlock(pr, m_standaloneMeshesEx[variant, rotation, textureIndex], color, size, ref matrix, env);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain st, ComponentMiner miner, int value, TerrainRaycastResult rr)
	{
		Vector3 forward = Matrix.CreateFromQuaternion(miner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float num = MathUtils.Max(Vector3.Dot(forward, Vector3.UnitZ), Vector3.Dot(forward, Vector3.UnitX), Vector3.Dot(forward, -Vector3.UnitZ), Vector3.Dot(forward, -Vector3.UnitX));
		int num2 = 0;
		if (Vector3.Dot(forward, Vector3.UnitZ) == num)
		{
			num2 = 2;
		}
		else if (Vector3.Dot(forward, Vector3.UnitX) == num)
		{
			num2 = 3;
		}
		else if (Vector3.Dot(forward, -Vector3.UnitZ) == num)
		{
			num2 = 0;
		}
		else if (Vector3.Dot(forward, -Vector3.UnitX) == num)
		{
			num2 = 1;
		}
		int textureIndex = GetTextureIndex(Terrain.ExtractData(value));
		int num3 = m_random.Int(0, 3);
		int data = (textureIndex << 4) | (num3 << 2) | (num2 & 3);
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, BlockIndex), data),
			CellFace = rr.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain st, int oldVal, int newVal, int toolLvl, List<BlockDropValue> drops, out bool showDebris)
	{
		int num28 = GetTextureIndex(Terrain.ExtractData(oldVal)) << 4;
		drops.Add(new BlockDropValue
		{
			Value = Terrain.ReplaceData(oldVal, num28),
			Count = 1
		});
		showDebris = true;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain t, int value)
	{
		return m_collision;
	}

	public AnimalBonesBlock()
	{
		m_blockMeshes = new BlockMesh[4, 4];
		m_standaloneMeshes = new BlockMesh[4, 4];
		m_random = new Random();
	}

	public static int GetRotation(int data)
	{
		return data & 3;
	}

	public static int GetVariant(int data)
	{
		return (data >> 2) & 3;
	}

	public static int GetTextureIndex(int data)
	{
		return (data >> 4) & 3;
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
