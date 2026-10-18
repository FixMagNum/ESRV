using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class SpikedPlankBlock : MountedElectricElementBlock, IPaintableBlock
{
	public const int Index = 86;

	public BlockMesh m_standaloneBlockMesh;

	public BlockMesh[] m_blockMeshesByData;

	public BoundingBox[][] m_collisionBoxesByData;

	public BlockMesh m_standaloneBlockMeshPainted = new BlockMesh();

	public BlockMesh[] m_blockMeshesByDataPainted = new BlockMesh[12];

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/SpikedPlanks");
		string[] array = new string[2] { "SpikedPlankRetracted", "SpikedPlank" };
		Matrix matrix = Matrix.CreateTranslation(new Vector3(0.1875f, 0.0625f, 0f));
		for (int i = 0; i < 2; i++)
		{
			string name = array[i];
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(name).ParentBone);
			for (int j = 0; j < 6; j++)
			{
				int num = SetMountingFace(SetSpikesState(0, i != 0), j);
				Matrix matrix2;
				if (j >= 4)
				{
					matrix2 = ((j != 4) ? (Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f)) : Matrix.CreateTranslation(0.5f, 0f, 0.5f));
				}
				else
				{
					matrix2 = Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY((float)j * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
				}
				m_blockMeshesByData[num] = new BlockMesh();
				m_blockMeshesByData[num].AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_blockMeshesByDataPainted[num] = new BlockMesh();
				m_blockMeshesByDataPainted[num].AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
				m_blockMeshesByDataPainted[num].TransformTextureCoordinates(matrix);
				m_collisionBoxesByData[num] = new BoundingBox[1] { m_blockMeshesByData[num].CalculateBoundingBox() };
			}
			Matrix identity = Matrix.Identity;
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * identity, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMeshPainted.AppendModelMeshPart(model.FindMesh(name).MeshParts[0], boneAbsoluteTransform * identity, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMeshPainted.TransformTextureCoordinates(matrix);
		}
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		int mountingFace = GetMountingFace(Terrain.ExtractData(value));
		return face != CellFace.OppositeFace(mountingFace);
	}

	public override bool ShouldAvoid(int value)
	{
		return GetSpikesState(Terrain.ExtractData(value));
	}

	public static bool GetSpikesState(int data)
	{
		return (data & 1) == 0;
	}

	public static int SetSpikesState(int data, bool spikesState)
	{
		int num = data & -16;
		int num2 = data & 0xF;
		num2 = ((!spikesState) ? (num2 | 1) : (num2 & -2));
		return num | num2;
	}

	public static int GetMountingFace(int data)
	{
		return (((data & 0xE) >> 1) + 4) % 6;
	}

	public static int SetMountingFace(int data, int face)
	{
		return (data & -15) | ((((face + 2) % 6) & 7) << 1);
	}

	public override int GetFace(int value)
	{
		return GetMountingFace(Terrain.ExtractData(value));
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int num = Terrain.ExtractData(value);
		int num2 = num & -16;
		int data = num & 0xF;
		data = SetMountingFace(SetSpikesState(data, spikesState: true), raycastResult.CellFace.Face);
		return new BlockPlacementData
		{
			Value = Terrain.ReplaceData(value, data | num2),
			CellFace = raycastResult.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int componentPlayer = Terrain.ExtractData(value) & 0xF;
		if (componentPlayer >= m_collisionBoxesByData.Length)
		{
			return base.GetCustomCollisionBoxes(terrain, value);
		}
		return m_collisionBoxesByData[componentPlayer];
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int num = Terrain.ExtractData(value);
		if (IsWaterLogged(num) && BlocksManager.Blocks[18] is FocalorsWaterBlock focalorsWaterBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsWaterBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int? paintColor = GetPaintColor(value);
		Color color2 = SubsystemPalette.GetColor(generator, paintColor);
		int num2 = num & 0xF;
		if (num2 < m_blockMeshesByData.Length)
		{
			BlockMesh blockMesh = (paintColor.HasValue ? m_blockMeshesByDataPainted[num2] : m_blockMeshesByData[num2]);
			if (blockMesh != null)
			{
				generator.GenerateShadedMeshVertices(this, x, y, z, blockMesh, color2, null, null, geometry.SubsetOpaque);
				generator.GenerateWireVertices(value, x, y, z, GetFace(value), 1f, Vector2.Zero, geometry.SubsetOpaque);
			}
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int? paintColor = GetPaintColor(value);
		Color color2 = SubsystemPalette.GetColor(environmentData, paintColor);
		BlockMesh blockMesh = (paintColor.HasValue ? m_standaloneBlockMeshPainted : m_standaloneBlockMesh);
		BlocksManager.DrawMeshBlock(primitivesRenderer, blockMesh, color * color2, 1f * size, ref matrix, environmentData);
	}

	public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return new SpikedPlankElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));
	}

	public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int face2 = GetFace(value);
		if (face == face2 && SubsystemElectricity.GetConnectorDirection(face2, 0, connectorFace).HasValue)
		{
			return ElectricConnectorType.Input;
		}
		return null;
	}

	public SpikedPlankBlock()
	{
		m_standaloneBlockMesh = new BlockMesh();
		m_blockMeshesByData = new BlockMesh[12];
		m_collisionBoxesByData = new BoundingBox[12][];
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		return Terrain.ReplaceData(value, SetColor(data, color));
	}

	public static int? GetColor(int data)
	{
		if ((data & 0x10) != 0)
		{
			return (data >> 5) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -497) | 0x10 | ((color.Value & 0xF) << 5);
		}
		return data & -497;
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int? paintColor = GetPaintColor(value);
		return SubsystemPalette.GetName(subsystemTerrain, paintColor, base.GetDisplayName(subsystemTerrain, value));
	}

	public override string GetCategory(int value)
	{
		if (GetPaintColor(value).HasValue)
		{
			return "Painted";
		}
		return base.GetCategory(value);
	}
}
