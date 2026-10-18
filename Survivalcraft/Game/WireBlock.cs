using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class WireBlock : Block, IElectricWireElementBlock, IElectricElementBlock, IPaintableBlock, IWaterloggable
{
	public const int Index = 133;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BoundingBox[] m_collisionBoxesByFace = new BoundingBox[6];

	public static readonly Color WireColor;

	public const int WaterLoggedBit = 2048;

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Wire");
		Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Wire").ParentBone);
		m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Wire").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.9375f, 0f, 0f));
		for (int i = 0; i < 6; i++)
		{
			Vector3 vector = CellFace.FaceToVector3(i);
			Vector3 vector2 = new Vector3(0.5f, 0.5f, 0.5f) - 0.5f * vector;
			Vector3 vector3;
			Vector3 vector4;
			if (vector.X != 0f)
			{
				vector3 = new Vector3(0f, 1f, 0f);
				vector4 = new Vector3(0f, 0f, 1f);
			}
			else if (vector.Y != 0f)
			{
				vector3 = new Vector3(1f, 0f, 0f);
				vector4 = new Vector3(0f, 0f, 1f);
			}
			else
			{
				vector3 = new Vector3(1f, 0f, 0f);
				vector4 = new Vector3(0f, 1f, 0f);
			}
			Vector3 v = vector2 - 0.5f * vector3 - 0.5f * vector4;
			Vector3 v2 = vector2 + 0.5f * vector3 + 0.5f * vector4 + 0.05f * vector;
			m_collisionBoxesByFace[i] = new BoundingBox(Vector3.Min(v, v2), Vector3.Max(v, v2));
		}
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return null;
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		if (!WireExistsOnFace(value, face))
		{
			return null;
		}
		return ElectricConnectorType.InputOutput;
	}

	public int GetConnectionMask(int value)
	{
		int? color = GetColor(Terrain.ExtractData(value));
		if (!color.HasValue)
		{
			return int.MaxValue;
		}
		return 1 << color.Value;
	}

	public int GetConnectedWireFacesMask(int value, int face)
	{
		int num = 0;
		if (WireExistsOnFace(value, face))
		{
			int num2 = CellFace.OppositeFace(face);
			bool flag = false;
			for (int i = 0; i < 6; i++)
			{
				if (i == face)
				{
					num |= 1 << i;
				}
				else if (i != num2 && WireExistsOnFace(value, i))
				{
					num |= 1 << i;
					flag = true;
				}
			}
			if (flag && WireExistsOnFace(value, num2))
			{
				num |= 1 << num2;
			}
		}
		return num;
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		BoundingBox[] array = new BoundingBox[6];
		for (int i = 0; i < 6; i++)
		{
			array[i] = (WireExistsOnFace(value, i) ? m_collisionBoxesByFace[i] : default(BoundingBox));
		}
		return array;
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
		for (int i = 0; i < 6; i++)
		{
			if (WireExistsOnFace(value, i))
			{
				generator.GenerateWireVertices(value, x, y, z, i, 0f, Vector2.Zero, geometry.SubsetOpaque);
			}
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int? paintColor = GetPaintColor(value);
		Color color2 = (paintColor.HasValue ? (color * SubsystemPalette.GetColor(environmentData, paintColor)) : (1.25f * WireColor * color));
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color2, 2f * size, ref matrix, environmentData);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Point3 num = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
		int cellValue = subsystemTerrain.Terrain.GetCellValue(raycastResult.CellFace.X + num.X, raycastResult.CellFace.Y + num.Y, raycastResult.CellFace.Z + num.Z);
		int num2 = Terrain.ExtractContents(cellValue);
		Block num3 = BlocksManager.Blocks[num2];
		int i = GetWireFacesBitmask(cellValue);
		int j = i | (1 << raycastResult.CellFace.Face);
		if (j != i || !(num3 is WireBlock))
		{
			return new BlockPlacementData
			{
				Value = SetWireFacesBitmask(value, j),
				CellFace = raycastResult.CellFace
			};
		}
		return default;
	}

	public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		int wireFacesBitmask = GetWireFacesBitmask(value);
		wireFacesBitmask &= ~(1 << raycastResult.CollisionBoxIndex);
		return new BlockPlacementData
		{
			Value = SetWireFacesBitmask(value, wireFacesBitmask),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int? paintColor = GetPaintColor(oldValue);
		for (int i = 0; i < 6; i++)
		{
			if (WireExistsOnFace(oldValue, i) && !WireExistsOnFace(newValue, i))
			{
				dropValues.Add(new BlockDropValue
				{
					Value = Terrain.MakeBlockValue(133, 0, SetColor(0, paintColor)),
					Count = 1
				});
			}
		}
		showDebris = dropValues.Count > 0;
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(133);
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 0));
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 8));
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 15));
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 11));
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 12));
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 13));
		yield return Terrain.MakeBlockValue(133, 0, SetColor(0, 14));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int? x = GetPaintColor(value);
		return SubsystemPalette.GetName(subsystemTerrain, x, base.GetDisplayName(subsystemTerrain, value));
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain subsystemTerrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		return Terrain.ReplaceData(value, SetColor(data, color));
	}

	public static bool WireExistsOnFace(int value, int face)
	{
		return (GetWireFacesBitmask(value) & (1 << face)) != 0;
	}

	public static int GetWireFacesBitmask(int value)
	{
		if (Terrain.ExtractContents(value) == 133)
		{
			return Terrain.ExtractData(value) & 0x3F;
		}
		return 0;
	}

	public static int SetWireFacesBitmask(int value, int bitmask)
	{
		int num = Terrain.ExtractData(value);
		num &= -64;
		num |= bitmask & 0x3F;
		return Terrain.ReplaceData(Terrain.ReplaceContents(value, 133), num);
	}

	public static int? GetColor(int data)
	{
		if ((data & 0x40) != 0)
		{
			return (data >> 7) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -1985) | 0x40 | ((color.Value & 0xF) << 7);
		}
		return data & -1985;
	}

	static WireBlock()
	{
		WireColor = new Color(79, 36, 21);
	}

	public bool IsWaterLogged(int data)
	{
		return (data & 0x800) != 0;
	}

	public int SetWaterLogged(int data, bool isWaterLogged)
	{
		if (!isWaterLogged)
		{
			return data & -2049;
		}
		return data | 0x800;
	}
}
