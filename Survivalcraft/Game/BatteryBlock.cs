using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class BatteryBlock : Block, IElectricElementBlock, IWaterloggable
{
	public const int Index = 138;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshesByFace = new BlockMesh[6];

	public BoundingBox[][] m_collisionBoxesByFace = new BoundingBox[6][];

	public const int WaterLoggedBit = 1024;

	public override void Initialize()
	{
		ModelMesh modelMesh = ContentManager.Get<Model>("Models/Battery").FindMesh("Battery");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(modelMesh.ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		for (int i = 0; i < 6; i++)
		{
			m_blockMeshesByFace[i] = new BlockMesh();
			Matrix matrix = Matrix.Identity;
			switch (i)
			{
			case 0:
				matrix = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0f);
				break;
			case 1:
				matrix = Matrix.CreateRotationZ(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0.5f, 0.5f);
				break;
			case 2:
				matrix = Matrix.CreateRotationX(-(float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 1f);
				break;
			case 3:
				matrix = Matrix.CreateRotationZ((float)Math.PI / 2f) * Matrix.CreateTranslation(1f, 0.5f, 0.5f);
				break;
			case 4:
				matrix = Matrix.Identity * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
				break;
			case 5:
				matrix = Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f);
				break;
			}
			m_blockMeshesByFace[i].AppendModelMeshPart(modelMesh.MeshParts[0], boneAbsoluteTransform * matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_collisionBoxesByFace[i] = new BoundingBox[1] { m_blockMeshesByFace[i].CalculateBoundingBox() };
		}
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if (toolLevel >= RequiredToolLevel)
		{
			int voltageLevel = GetVoltageLevel(Terrain.ExtractData(oldValue));
			int data = SetVoltageLevel(SetMountingFace(0, 4), voltageLevel);
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(138, 0, data),
				Count = 1
			});
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int model = GetMountingFace(Terrain.ExtractData(value));
		if (model < 0 || model > 5)
		{
			model = 4;
		}
		return m_collisionBoxesByFace[model];
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsWaterBlock focalorsWaterBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsWaterBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int num = GetMountingFace(data);
		if (num < 0 || num > 5)
		{
			num = 4;
		}
		generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByFace[num], Color.White, null, geometry.SubsetOpaque);
		generator.GenerateWireVertices(value, x, y, z, num, 0.2f, Vector2.Zero, geometry.SubsetOpaque);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 1f * size, ref matrix, environmentData);
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		int mountingFace = GetMountingFace(Terrain.ExtractData(value));
		return new BatteryElectricElement(subsystemElectricity, new CellFace(x, y, z, mountingFace));
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int mountingFace = GetMountingFace(Terrain.ExtractData(value));
		if (face == mountingFace && SubsystemElectricity.GetConnectorDirection(mountingFace, 0, connectorFace).HasValue)
		{
			return ElectricConnectorType.Output;
		}
		return null;
	}

	public int GetConnectionMask(int value)
	{
		return int.MaxValue;
	}

	public static int GetVoltageLevel(int data)
	{
		return 15 - (data & 0xF);
	}

	public static int SetVoltageLevel(int data, int voltageLevel)
	{
		return (data & -16) | (15 - (voltageLevel & 0xF));
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int voltageLevel = GetVoltageLevel(Terrain.ExtractData(value));
		int face = raycastResult.CellFace.Face;
		int data = SetMountingFace(0, face);
		data = SetVoltageLevel(data, voltageLevel);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public static int GetMountingFace(int data)
	{
		return (data >> 4) & 7;
	}

	public static int SetMountingFace(int data, int face)
	{
		return (data & -113) | ((face & 7) << 4);
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
}
