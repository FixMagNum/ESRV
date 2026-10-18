using System.Collections.Generic;
using Engine;

namespace Game;

public abstract class WoodBlock : CubeBlock
{
	public int m_cutTextureSlot;

	public int m_sideTextureSlot;

	public WoodBlock(int cutTextureSlot, int sideTextureSlot)
	{
		m_cutTextureSlot = cutTextureSlot;
		m_sideTextureSlot = sideTextureSlot;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		int data = Terrain.ExtractData(value);
		TerrainGeometrySubset[] subsetsByFace = (IsPlayerPlaced(data) ? geom.OpaqueSubsetsByFace : geom.AlphaTestSubsetsByFace);
		switch (GetCutFace(data))
		{
		case 0:
			g.GenerateCubeVertices(this, value, x, y, z, 1, 0, 0, Color.White, subsetsByFace);
			break;
		case 4:
			g.GenerateCubeVertices(this, value, x, y, z, Color.White, subsetsByFace);
			break;
		default:
			g.GenerateCubeVertices(this, value, x, y, z, 0, 1, 1, Color.White, subsetsByFace);
			break;
		}
	}

	public override int GetFaceTextureSlot(int face, int value)
	{
		int boxSize = GetCutFace(Terrain.ExtractData(value));
		if (boxSize == face || CellFace.OppositeFace(boxSize) == face)
		{
			return m_cutTextureSlot;
		}
		return m_sideTextureSlot;
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float num = float.NegativeInfinity;
		int cutFace = 0;
		for (int i = 0; i < 6; i++)
		{
			float num2 = Vector3.Dot(CellFace.FaceToVector3(i), forward);
			if (num2 > num)
			{
				num = num2;
				cutFace = i;
			}
		}
		int data = SetCutFace(0, cutFace) | 4;
		int value2 = Terrain.MakeBlockValue(BlockIndex, 0, data);
		return new BlockPlacementData
		{
			Value = value2,
			CellFace = raycastResult.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		int data = Terrain.ExtractData(oldValue);
		data = SetCutFace(data, 4);
		dropValues.Add(new BlockDropValue
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			Count = 1
		});
		showDebris = true;
	}

	public static int GetCutFace(int data)
	{
		data &= 3;
		return data switch
		{
			0 => 4, 
			1 => 0, 
			_ => 1, 
		};
	}

	public static int SetCutFace(int data, int cutFace)
	{
		data &= -4;
		switch (cutFace)
		{
		case 0:
		case 2:
			return data | 1;
		case 1:
		case 3:
			return data | 2;
		default:
			return data;
		}
	}

	public static bool IsPlayerPlaced(int data)
	{
		return (data & 4) != 0;
	}

	private bool IsAxe(int value)
	{
		int num = Terrain.ExtractContents(value);
		if (num != 165 && num != 325 && num != 114 && num != 37)
		{
			return num == 29;
		}
		return true;
	}
}
