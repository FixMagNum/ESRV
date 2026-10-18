using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public class GravestoneBlock : Block, IWaterloggable
{
	public const int Index = 189;

	public BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[16];

	public BlockMesh[] m_blockMeshes = new BlockMesh[16];

	public BoundingBox[][] m_collisionBoxes = new BoundingBox[16][];

	public const int WaterLoggedBit = 1024;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Graves");
		for (int i = 0; i < 16; i++)
		{
			int variant = GetVariant(i);
			float radians = ((GetRotation(i) == 0) ? 0f : ((float)Math.PI / 2f));
			string name = "Grave" + (variant % 4 + 1).ToString(CultureInfo.InvariantCulture);
			bool flag = variant >= 4;
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(name).ParentBone);
			m_blockMeshes[i] = new BlockMesh();
			m_blockMeshes[i].AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMeshes[i] = new BlockMesh();
			m_standaloneBlockMeshes[i].AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			if (flag)
			{
				Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Plinth").ParentBone);
				m_blockMeshes[i].AppendModelMeshPart(model.FindMesh("Plinth").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateRotationY(radians) * Matrix.CreateTranslation(0.5f, 0f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_standaloneBlockMeshes[i].AppendModelMeshPart(model.FindMesh("Plinth").MeshParts[0], boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			}
			m_collisionBoxes[i] = new BoundingBox[1] { m_blockMeshes[i].CalculateBoundingBox() };
		}
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsWaterLogged(num) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int num2 = num & 0xF;
		if (num2 < m_blockMeshes.Length)
		{
			int value2 = ((y > 0) ? generator.Terrain.GetCellValueFast(x, y - 1, z) : 0);
			int num3 = Terrain.ExtractContents(value2);
			bool flag = BlocksManager.Blocks[num3].GetBlockDigMethod(value2) != BlockDigMethod.Shovel;
			bool flag2 = num3 == 7 || num3 == 4 || num3 == 52;
			int num4 = (int)(MathUtils.Hash((uint)(x + 172 * y + 18271 * z)) & 0xFFFF);
			Matrix value3 = Matrix.Identity;
			if (!flag)
			{
				float radians = 0.2f * ((float)(num4 % 16) / 7.5f - 1f);
				float radians2 = 0.1f * ((float)((num4 >> 4) % 16) / 7.5f - 1f);
				value3 = ((GetRotation(num2) != 0) ? (Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateRotationZ(radians) * Matrix.CreateRotationY(radians2) * Matrix.CreateTranslation(0.5f, 0f, 0.5f)) : (Matrix.CreateTranslation(-0.5f, 0f, -0.5f) * Matrix.CreateRotationX(radians) * Matrix.CreateRotationY(radians2) * Matrix.CreateTranslation(0.5f, 0f, 0.5f)));
			}
			float f = (flag ? 0f : MathUtils.Sqr((float)((num4 >> 8) % 16) / 15f));
			Color color2 = ((!flag2) ? Color.Lerp(Color.White, new Color(255, 233, 199), f) : Color.Lerp(new Color(217, 206, 123), new Color(229, 206, 123), f));
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshes[num2], color2, value3, geometry.SubsetOpaque);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int num = Terrain.ExtractData(value) & 0xF;
		if (num < m_blockMeshes.Length)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[num], color, size, ref matrix, environmentData);
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = Terrain.ExtractData(value) & 0xF;
		if (num < m_collisionBoxes.Length)
		{
			return m_collisionBoxes[num];
		}
		return base.GetCustomCollisionBoxes(terrain, value);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int data = Terrain.ExtractData(value);
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float num = MathUtils.Abs(Vector3.Dot(forward, Vector3.UnitX));
		data = ((!(MathUtils.Abs(Vector3.Dot(forward, Vector3.UnitZ)) > num)) ? SetRotation(data, 1) : SetRotation(data, 0));
		int cellContents = subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
		Block block = BlocksManager.Blocks[cellContents];
		bool isWaterLogged = block is WaterBlock || block is FocalorsFluidBlock;
		data = SetWaterLogged(data, isWaterLogged);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(189, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		int i = 0;
		while (i < 8)
		{
			int data = SetVariant(0, i);
			yield return Terrain.MakeBlockValue(189, 0, data);
			int num = i + 1;
			i = num;
		}
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		int data = Terrain.ExtractData(oldValue) & -1025;
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(189, 0, data),
			Count = 1
		});
	}

	public static int GetRotation(int data)
	{
		return (data & 8) >> 3;
	}

	public static int SetRotation(int data, int rotation)
	{
		return (data & -9) | ((rotation << 3) & 8);
	}

	public static int GetVariant(int data)
	{
		return data & 7;
	}

	public static int SetVariant(int data, int variant)
	{
		return (data & -8) | (variant & 7);
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
