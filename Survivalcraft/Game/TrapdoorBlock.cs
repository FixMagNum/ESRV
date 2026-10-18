using System;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class TrapdoorBlock : Block, IElectricElementBlock, IWaterloggable
{
	public string m_modelName;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshesByData = new BlockMesh[16];

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[16][];

	public const int WaterLoggedBit = 1024;

	public TrapdoorBlock(string modelName)
	{
		m_modelName = modelName;
	}

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>(m_modelName);
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Trapdoor").ParentBone);
		for (int i = 0; i < 16; i++)
		{
			int rotation = GetRotation(i);
			bool open = GetOpen(i);
			bool upsideDown = GetUpsideDown(i);
			m_blockMeshesByData[i] = new BlockMesh();
			Matrix identity = Matrix.Identity;
			identity *= Matrix.CreateTranslation(0f, -0.0625f, 0.4375f) * Matrix.CreateRotationX(open ? (-(float)Math.PI / 2f) : 0f) * Matrix.CreateTranslation(0f, 0.0625f, -0.4375f);
			identity *= Matrix.CreateRotationZ(upsideDown ? ((float)Math.PI) : 0f);
			identity *= Matrix.CreateRotationY((float)rotation * (float)Math.PI / 2f);
			identity *= Matrix.CreateTranslation(new Vector3(0.5f, upsideDown ? 1 : 0, 0.5f));
			m_blockMeshesByData[i].AppendModelMeshPart(model.FindMesh("Trapdoor").MeshParts[0], boneAbsoluteTransform * identity, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_blockMeshesByData[i].GenerateSidesData();
			m_collisionBoxesByData[i] = new BoundingBox[1] { m_blockMeshesByData[i].CalculateBoundingBox() };
		}
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Trapdoor").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		base.Initialize();
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int value2 = Terrain.ExtractData(value);
		if (IsWaterLogged(value2) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int drinkType = value2 & 0xF;
		if (drinkType < m_blockMeshesByData.Length)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMeshesByData[drinkType], Color.White, null, null, geometry.SubsetAlphaTest);
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int num;
		bool value2;
		if (raycastResult.CellFace.Face < 4)
		{
			num = raycastResult.CellFace.Face;
			value2 = raycastResult.HitPoint().Y - (float)raycastResult.CellFace.Y > 0.5f;
		}
		else
		{
			Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
			float num2 = Vector3.Dot(forward, Vector3.UnitZ);
			float num3 = Vector3.Dot(forward, Vector3.UnitX);
			float num4 = Vector3.Dot(forward, -Vector3.UnitZ);
			float num5 = Vector3.Dot(forward, -Vector3.UnitX);
			if (num2 == MathUtils.Max(num2, num3, num4, num5))
			{
				num = 2;
			}
			else
			{
				num = ((num3 == MathUtils.Max(num2, num3, num4, num5)) ? 3 : ((num4 != MathUtils.Max(num2, num3, num4, num5)) ? ((num5 == MathUtils.Max(num2, num3, num4, num5)) ? 1 : 0) : 0));
			}
			value2 = raycastResult.CellFace.Face == 5;
		}
		int data = SetOpen(SetRotation(SetUpsideDown(0, value2), num), open: false);
		int data2 = Terrain.ExtractData(value);
		bool isWaterLogged = IsWaterLogged(data2);
		data = SetWaterLogged(data, isWaterLogged);
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(Terrain.ReplaceContents(0, BlockIndex), data),
			CellFace = raycastResult.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int num = Terrain.ExtractData(value) & 0xF;
		if (num < m_collisionBoxesByData.Length)
		{
			return m_collisionBoxesByData[num];
		}
		return base.GetCustomCollisionBoxes(terrain, value);
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		int coordinates = Terrain.ExtractData(value);
		return new TrapDoorElectricElement(subsystemElectricity, new CellFace(x, y, z, GetMountingFace(coordinates)));
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (face == GetMountingFace(data))
		{
			int rotation = GetRotation(data);
			if (SubsystemElectricity.GetConnectorDirection(4, (4 - rotation) % 4, connectorFace) == ElectricConnectorDirection.Top)
			{
				return ElectricConnectorType.Input;
			}
		}
		return null;
	}

	public int GetConnectionMask(int value)
	{
		return int.MaxValue;
	}

	public static int GetRotation(int data)
	{
		return data & 3;
	}

	public static bool GetOpen(int data)
	{
		return (data & 4) != 0;
	}

	public static bool GetUpsideDown(int data)
	{
		return (data & 8) != 0;
	}

	public static int SetRotation(int data, int rotation)
	{
		return (data & -4) | (rotation & 3);
	}

	public static int SetOpen(int data, bool open)
	{
		if (!open)
		{
			return data & -5;
		}
		return data | 4;
	}

	public static int SetUpsideDown(int data, bool upsideDown)
	{
		if (!upsideDown)
		{
			return data & -9;
		}
		return data | 8;
	}

	public static int GetMountingFace(int data)
	{
		if (!GetUpsideDown(data))
		{
			return 4;
		}
		return 5;
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
