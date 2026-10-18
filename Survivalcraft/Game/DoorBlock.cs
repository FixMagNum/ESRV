using System;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class DoorBlock : Block, IElectricElementBlock, IWaterloggable
{
	public float m_pivotDistance;

	public string m_modelName;

	public BlockMesh m_standaloneBlockMesh = new BlockMesh();

	public BlockMesh[] m_blockMeshesByData = new BlockMesh[16];

	public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[16][];

	public float tx = 3f / 32f;

	public float ty = 0.375f;

	public const int WaterLoggedBit = 1024;

	public DoorBlock(string modelName, float pivotDistance)
	{
		m_modelName = modelName;
		m_pivotDistance = pivotDistance;
	}

	public override void Initialize()
	{
		Model num = ContentManager.Get<Model>(m_modelName);
		Matrix num3 = BlockMesh.GetBoneAbsoluteTransform(num.FindMesh("Door").ParentBone);
		for (int i = 0; i < 16; i++)
		{
			int num6 = GetRotation(i);
			bool num7 = GetOpen(i);
			bool num8 = GetRightHanded(i);
			float num9 = ((!num8) ? 1 : (-1));
			m_blockMeshesByData[i] = new BlockMesh();
			Matrix num10 = Matrix.Identity;
			num10 *= Matrix.CreateScale(0f - num9, 1f, 1f);
			num10 *= Matrix.CreateTranslation((0.5f - m_pivotDistance) * num9, 0f, 0f) * Matrix.CreateRotationY(num7 ? (num9 * (float)Math.PI / 2f) : 0f) * Matrix.CreateTranslation((0f - (0.5f - m_pivotDistance)) * num9, 0f, 0f);
			num10 *= Matrix.CreateTranslation(0f, 0f, 0.5f - m_pivotDistance) * Matrix.CreateRotationY((float)num6 * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
			m_blockMeshesByData[i].AppendModelMeshPart(num.FindMesh("Door").MeshParts[0], num3 * num10, makeEmissive: false, !num8, doubleSided: false, flipNormals: false, Color.White);
			BoundingBox x = m_blockMeshesByData[i].CalculateBoundingBox();
			x.Max.Y = 1f;
			m_collisionBoxesByData[i] = new BoundingBox[1] { x };
		}
		m_standaloneBlockMesh.AppendModelMeshPart(num.FindMesh("Door").MeshParts[0], num3 * Matrix.CreateTranslation(0f, -1f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
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
		if (IsBottomPart(generator.Terrain, x, y, z) && num2 < m_blockMeshesByData.Length)
		{
			generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByData[num2], Color.White, null, geometry.SubsetAlphaTest);
		}
		Vector2 centerOffset = (GetRightHanded(num2) ? new Vector2(-0.45f, 0f) : new Vector2(0.45f, 0f));
		generator.GenerateWireVertices(value, x, y, z, GetHingeFace(num2), 0.01f, centerOffset, geometry.SubsetOpaque);
	}

	public override TerrainVertex SetDiggingCrackingTextureTransform(TerrainVertex vertex)
	{
		float num = (float)vertex.Tx / 32767f;
		float num2 = (float)vertex.Ty / 37267f;
		if (num <= tx)
		{
			vertex.Tx = 0;
		}
		else
		{
			vertex.Tx = short.MaxValue;
		}
		if (num2 <= ty)
		{
			vertex.Ty = 0;
		}
		else
		{
			vertex.Ty = short.MaxValue;
		}
		return vertex;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 0.75f * size, ref matrix, environmentData);
	}

	public override int GetShadowStrength(int value)
	{
		if (!GetOpen(Terrain.ExtractData(value)))
		{
			return DefaultShadowStrength;
		}
		return 4;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Point3 tx = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
		int ty = raycastResult.CellFace.X + tx.X;
		int num = raycastResult.CellFace.Y + tx.Y;
		int num2 = raycastResult.CellFace.Z + tx.Z;
		int cellContents = subsystemTerrain.Terrain.GetCellContents(ty, num + 1, num2);
		Block block = BlocksManager.Blocks[cellContents];
		if (cellContents != 0 && !(block is WaterBlock) && !(block is FocalorsFluidBlock))
		{
			return default;
		}
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float num3 = Vector3.Dot(forward, Vector3.UnitZ);
		float num4 = Vector3.Dot(forward, Vector3.UnitX);
		float num5 = Vector3.Dot(forward, -Vector3.UnitZ);
		float num6 = Vector3.Dot(forward, -Vector3.UnitX);
		int num7 = 0;
		if (num3 == MathUtils.Max(num3, num4, num5, num6))
		{
			num7 = 2;
		}
		else if (num4 == MathUtils.Max(num3, num4, num5, num6))
		{
			num7 = 3;
		}
		else if (num5 == MathUtils.Max(num3, num4, num5, num6))
		{
			num7 = 0;
		}
		else if (num6 == MathUtils.Max(num3, num4, num5, num6))
		{
			num7 = 1;
		}
		bool rightHanded = true;
		switch (num7)
		{
		case 0:
			rightHanded = BlocksManager.Blocks[subsystemTerrain.Terrain.GetCellContents(ty - 1, num, num2)].IsTransparent;
			break;
		case 1:
			rightHanded = BlocksManager.Blocks[subsystemTerrain.Terrain.GetCellContents(ty, num, num2 + 1)].IsTransparent;
			break;
		case 2:
			rightHanded = BlocksManager.Blocks[subsystemTerrain.Terrain.GetCellContents(ty + 1, num, num2)].IsTransparent;
			break;
		case 3:
			rightHanded = BlocksManager.Blocks[subsystemTerrain.Terrain.GetCellContents(ty, num, num2 - 1)].IsTransparent;
			break;
		}
		int data = SetRightHanded(SetOpen(SetRotation(0, num7), open: false), rightHanded);
		int cellContents2 = subsystemTerrain.Terrain.GetCellContents(ty, num, num2);
		Block block2 = BlocksManager.Blocks[cellContents2];
		bool isWaterLogged = cellContents2 == 18 || block2 is FocalorsFluidBlock;
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
		return null;
	}

	public override bool ShouldAvoid(int value)
	{
		return !GetOpen(Terrain.ExtractData(value));
	}

	public override bool IsHeatBlocker(int value)
	{
		return !GetOpen(Terrain.ExtractData(value));
	}

	public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		int chunkAtCell = Terrain.ExtractData(value);
		return new DoorElectricElement(subsystemElectricity, new CellFace(x, y, z, GetHingeFace(chunkAtCell)));
	}

	public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int chunkAtCell = GetHingeFace(Terrain.ExtractData(value));
		if (face == chunkAtCell)
		{
			ElectricConnectorDirection? chunkAtCell3 = SubsystemElectricity.GetConnectorDirection(chunkAtCell, 0, connectorFace);
			if (chunkAtCell3.HasValue && (chunkAtCell3.Value == ElectricConnectorDirection.Right || chunkAtCell3.Value == ElectricConnectorDirection.Left || chunkAtCell3.Value == ElectricConnectorDirection.In))
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

	public static bool GetRightHanded(int data)
	{
		return (data & 8) == 0;
	}

	public static int SetRotation(int data, int rotation)
	{
		return (data & -4) | (rotation & 3);
	}

	public static int SetOpen(int data, bool open)
	{
		return (data & -5) | (open ? 4 : 0);
	}

	public static int SetRightHanded(int data, bool rightHanded)
	{
		return (data & -9) | ((!rightHanded) ? 8 : 0);
	}

	public static bool IsTopPart(Terrain terrain, int x, int y, int z)
	{
		return BlocksManager.Blocks[terrain.GetCellContents(x, y - 1, z)] is DoorBlock;
	}

	public static bool IsBottomPart(Terrain terrain, int x, int y, int z)
	{
		return BlocksManager.Blocks[terrain.GetCellContents(x, y + 1, z)] is DoorBlock;
	}

	public static int GetHingeFace(int data)
	{
		int light = GetRotation(data);
		int shadow = ((light - 1 < 0) ? 3 : (light - 1));
		if (!GetRightHanded(data))
		{
			shadow = CellFace.OppositeFace(shadow);
		}
		return shadow;
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
