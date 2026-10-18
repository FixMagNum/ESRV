using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class FocalorsAttachedBlock : Block, IWaterloggable
{
	public const int WaterLoggedBit = 1024;

	protected BlockMesh[] m_blockMeshes = new BlockMesh[24];

	protected BlockMesh m_standaloneBlockMesh;

	protected BoundingBox[][] m_collisionBoxes = new BoundingBox[24][];

	protected abstract string GetModelName();

	protected abstract string GetMeshName();

	protected abstract Color GetColor(int variant);

	protected abstract float GetScale();

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>(GetModelName());
		string meshName = GetMeshName();
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(meshName).ParentBone);
		Matrix matrix = Matrix.Identity;
		bool flag = false;
		if (model.FindMesh("Bottom", throwIfNotFound: false) != null)
		{
			matrix = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bottom").ParentBone);
			flag = true;
		}
		Vector2[] array = new Vector2[4]
		{
			new Vector2(-0.2f, -0.2f),
			new Vector2(0.2f, -0.2f),
			new Vector2(-0.2f, 0.2f),
			new Vector2(0.2f, 0.2f)
		};
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				int num = 4 * i + j;
				m_blockMeshes[num] = new BlockMesh();
				for (int k = 0; k <= j && k <= 3; k++)
				{
					Matrix matrix2 = Matrix.CreateRotationY((float)k * 1.5f + 0.2f) * Matrix.CreateTranslation(array[k].X, 0f, array[k].Y);
					switch (i)
					{
					case 0:
						matrix2 *= Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0f);
						break;
					case 1:
						matrix2 *= Matrix.CreateRotationZ(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.5f, 0.5f);
						break;
					case 2:
						matrix2 *= Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 1f);
						break;
					case 3:
						matrix2 *= Matrix.CreateRotationZ((float)Math.PI / 2f) * Matrix.CreateTranslation(1f, 0.5f, 0.5f);
						break;
					case 4:
						matrix2 *= Matrix.CreateTranslation(0.5f, 0f, 0.5f);
						break;
					case 5:
						matrix2 *= Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f);
						break;
					}
					m_blockMeshes[num].AppendModelMeshPart(model.FindMesh(meshName).MeshParts[0], boneAbsoluteTransform * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				}
				m_collisionBoxes[num] = new BoundingBox[1] { m_blockMeshes[num].CalculateBoundingBox() };
			}
		}
		m_standaloneBlockMesh = new BlockMesh();
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh(meshName).MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		if (flag)
		{
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Bottom").MeshParts[0], matrix * Matrix.CreateTranslation(0f, -0.2f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color color = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z));
			color.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, color, topColor, geometry.TransparentSubsetsByFace);
		}
		int face = GetFace(data);
		int variant = GetVariant(data);
		int num = 4 * face + variant;
		generator.GenerateMeshVertices(this, x, y, z, m_blockMeshes[num], GetColor(variant), null, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color * GetColor(0), GetScale() * size, ref matrix, environmentData);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int data = Terrain.ExtractData(value);
		return m_collisionBoxes[4 * GetFace(data) + GetVariant(data)];
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int data = 0;
		data = SetFace(data, raycastResult.CellFace.Face);
		int variant = 0;
		data = SetVariant(data, variant);
		int cellContents = subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		Block block = BlocksManager.Blocks[cellContents];
		bool isWaterLogged = block is WaterBlock || block is FocalorsFluidBlock;
		data = SetWaterLogged(data, isWaterLogged);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex),
			Count = 1
		});
		showDebris = true;
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		return new BlockDebrisParticleSystem(subsystemTerrain, position, 0.75f * strength, DestructionDebrisScale, new Color(64, 64, 64), DefaultTextureSlot);
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x400) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (!isWaterLogged)
		{
			return data & -1025;
		}
		return data | 0x400;
	}

	public static int GetFace(int data)
	{
		return data & 7;
	}

	public static int SetFace(int data, int face)
	{
		return (data & -8) | (face & 7);
	}

	public static int GetVariant(int data)
	{
		return (data >> 3) & 3;
	}

	public static int SetVariant(int data, int variant)
	{
		return (data & -25) | ((variant & 3) << 3);
	}
}
