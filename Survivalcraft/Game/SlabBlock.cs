using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public abstract class SlabBlock : Block, IPaintableBlock, IWaterloggable
{
	public int m_coloredTextureSlot;

	public int m_fullBlockIndex;

	public BoundingBox[][] m_collisionBoxes;

	public const int WaterLoggedBit = 1024;

	public BlockMesh m_standaloneColoredBlockMesh = new BlockMesh();

	public BlockMesh m_standaloneUncoloredBlockMesh = new BlockMesh();

	public SlabBlock(int coloredTextureSlot, int fullBlockIndex)
	{
		m_collisionBoxes = new BoundingBox[2][];
		m_coloredTextureSlot = coloredTextureSlot;
		m_fullBlockIndex = fullBlockIndex;
	}

	public override void Initialize()
	{
		Model model = ContentManager.Get<Model>("Models/Slab");
		ModelMeshPart standingOnVelocity = model.FindMesh("Slab").MeshParts[0];
		Matrix flag2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Slab").ParentBone);
		m_standaloneUncoloredBlockMesh.AppendModelMeshPart(standingOnVelocity, flag2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneUncoloredBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation((float)(DefaultTextureSlot % 16) / 16f, (float)(DefaultTextureSlot / 16) / 16f, 0f));
		m_standaloneColoredBlockMesh.AppendModelMeshPart(standingOnVelocity, flag2 * Matrix.CreateTranslation(0f, -0.5f, 0f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		m_standaloneColoredBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation((float)(m_coloredTextureSlot % 16) / 16f, (float)(m_coloredTextureSlot / 16) / 16f, 0f));
		m_collisionBoxes[0] = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f))
		};
		m_collisionBoxes[1] = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f))
		};
		base.Initialize();
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		if (GetIsTop(Terrain.ExtractData(value)))
		{
			return face != 4;
		}
		return face != 5;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		if (IsWaterLogged(data) && BlocksManager.Blocks[18] is FocalorsFluidBlock focalorsFluidBlock)
		{
			Color sideColor;
			Color color = (sideColor = BlockColorsMap.WaterColorsMap.LookupSeasonalWaterColor(generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason, BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z)));
			sideColor.A = byte.MaxValue;
			Color topColor = color;
			topColor.A = 0;
			focalorsFluidBlock.GenerateWaterloggedFluidVertices(generator, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
		int mountingFace = (GetIsTop(data) ? 5 : 4);
		int? color2 = GetColor(data);
		Color color3 = (color2.HasValue ? SubsystemPalette.GetColor(generator, color2) : Color.White);
		generator.GenerateFlatVertices(this, value, x, y, z, color3, mountingFace, 0.5f, geometry.OpaqueSubsetsByFace);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int num = Terrain.ExtractContents(value);
		int data = Terrain.ExtractData(value);
		int num2 = Terrain.ExtractContents(raycastResult.Value);
		int data2 = Terrain.ExtractData(raycastResult.Value);
		if (num2 == num && ((GetIsTop(data2) && raycastResult.CellFace.Face == 5) || (!GetIsTop(data2) && raycastResult.CellFace.Face == 4)))
		{
			int value2 = Terrain.MakeBlockValue(m_fullBlockIndex, 0, 0);
			if (BlocksManager.Blocks[m_fullBlockIndex] is IPaintableBlock paintableBlock)
			{
				int? color = GetColor(data);
				value2 = paintableBlock.Paint(subsystemTerrain, value2, color);
			}
			CellFace cellFace = raycastResult.CellFace;
			cellFace.Point -= CellFace.FaceToPoint3(cellFace.Face);
			return new BlockPlacementData
			{
				Value = value2,
				CellFace = cellFace
			};
		}
		bool isTop = ((raycastResult.CellFace.Face >= 4) ? (raycastResult.CellFace.Face == 5) : (raycastResult.HitPoint().Y - (float)raycastResult.CellFace.Y > 0.5f));
		bool isWaterLogged = IsWaterLogged(data);
		int data3 = SetIsTop(data, isTop);
		data3 = SetWaterLogged(data3, isWaterLogged);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data3),
			CellFace = raycastResult.CellFace
		};
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int block = (GetIsTop(Terrain.ExtractData(value)) ? 1 : 0);
		return m_collisionBoxes[block];
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		if (Terrain.ExtractContents(newValue) != m_fullBlockIndex)
		{
			int data = Terrain.ExtractData(oldValue);
			int data2 = SetColor(0, GetColor(data));
			int value = Terrain.MakeBlockValue(BlockIndex, 0, data2);
			dropValues.Add(new BlockDropValue
			{
				Value = value,
				Count = 1
			});
			showDebris = true;
		}
		else
		{
			showDebris = false;
		}
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		int? block = GetColor(Terrain.ExtractData(value));
		if (block.HasValue)
		{
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, SubsystemPalette.GetColor(subsystemTerrain, block), m_coloredTextureSlot);
		}
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, GetFaceTextureSlot(0, value));
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		int? random = GetColor(Terrain.ExtractData(value));
		if (random.HasValue)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneColoredBlockMesh, color * SubsystemPalette.GetColor(environmentData, random), size, ref matrix, environmentData);
		}
		else
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneUncoloredBlockMesh, color, size, ref matrix, environmentData);
		}
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int? color = GetColor(Terrain.ExtractData(value));
		return SubsystemPalette.GetName(subsystemTerrain, color, base.GetDisplayName(subsystemTerrain, value));
	}

	public override string GetCategory(int value)
	{
		if (!GetColor(Terrain.ExtractData(value)).HasValue)
		{
			return base.GetCategory(value);
		}
		return LanguageControl.Get("BlocksManager", "Painted");
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
		int i = 0;
		while (i < 16)
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
			int num = i + 1;
			i = num;
		}
	}

	public int? GetPaintColor(int value)
	{
		return GetColor(Terrain.ExtractData(value));
	}

	public int Paint(SubsystemTerrain terrain, int value, int? color)
	{
		int data = Terrain.ExtractData(value);
		bool isWaterLogged = IsWaterLogged(data);
		int data2 = SetColor(data, color);
		data2 = SetWaterLogged(data2, isWaterLogged);
		return Terrain.MakeBlockValue(BlockIndex, 0, data2);
	}

	public static bool GetIsTop(int data)
	{
		return (data & 1) != 0;
	}

	public static int SetIsTop(int data, bool isTop)
	{
		return (data & -2) | (isTop ? 1 : 0);
	}

	public static int? GetColor(int data)
	{
		if ((data & 2) != 0)
		{
			return (data >> 2) & 0xF;
		}
		return null;
	}

	public static int SetColor(int data, int? color)
	{
		if (color.HasValue)
		{
			return (data & -63) | 2 | ((color.Value & 0xF) << 2);
		}
		return data & -63;
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

	public override int GetFaceTextureSlot(int face, int value)
	{
		if (GetColor(Terrain.ExtractData(value)).HasValue)
		{
			return m_coloredTextureSlot;
		}
		return DefaultTextureSlot;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		bool isTop = GetIsTop(Terrain.ExtractData(value));
		int num = Terrain.ExtractContents(neighborValue);
		if (BlocksManager.Blocks[num] is SlabBlock)
		{
			bool isTop2 = GetIsTop(Terrain.ExtractData(neighborValue));
			if (face >= 0 && face <= 3 && isTop == isTop2)
			{
				return false;
			}
			if (((face == 4) & isTop) && !isTop2)
			{
				return false;
			}
			if ((face == 5 && !isTop) & isTop2)
			{
				return false;
			}
		}
		if ((face != 4 || isTop) && !((face == 5) & isTop))
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return true;
	}
}
