using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class SnowBlock : CubeBlock
{
	public const int Index = 61;

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		Terrain terrain = generator.Terrain;
		int cellValueFast = terrain.GetCellValueFast(x, y - 1, z);
		bool flag = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast)] is LeavesBlock;
		float sizeFromValue = GetSizeFromValue(value);
		int cellValueFast2 = terrain.GetCellValueFast(x - 1, y, z);
		int cellValueFast3 = terrain.GetCellValueFast(x + 1, y, z);
		int cellValueFast4 = terrain.GetCellValueFast(x, y, z - 1);
		int cellValueFast5 = terrain.GetCellValueFast(x, y, z + 1);
		int cellValueFast6 = terrain.GetCellValueFast(x - 1, y, z - 1);
		int cellValueFast7 = terrain.GetCellValueFast(x + 1, y, z - 1);
		int cellValueFast8 = terrain.GetCellValueFast(x - 1, y, z + 1);
		int cellValueFast9 = terrain.GetCellValueFast(x + 1, y, z + 1);
		float x2 = CalculateNeighborSnowHeight(cellValueFast2);
		float x3 = CalculateNeighborSnowHeight(cellValueFast3);
		float x4 = CalculateNeighborSnowHeight(cellValueFast4);
		float x5 = CalculateNeighborSnowHeight(cellValueFast5);
		float x6 = CalculateNeighborSnowHeight(cellValueFast6);
		float x7 = CalculateNeighborSnowHeight(cellValueFast7);
		float x8 = CalculateNeighborSnowHeight(cellValueFast8);
		float x9 = CalculateNeighborSnowHeight(cellValueFast9);
		float height = MathUtils.Max(sizeFromValue, x2, x4, x6);
		float height2 = MathUtils.Max(sizeFromValue, x3, x4, x7);
		float height3 = MathUtils.Max(sizeFromValue, x3, x5, x9);
		float height4 = MathUtils.Max(sizeFromValue, x2, x5, x8);
		Color color = (flag ? new Color(255, 255, 255, 254) : new Color(255, 255, 255, 255));
		TerrainGeometrySubset[] subsetsByFace = (flag ? geometry.AlphaTestSubsetsByFace : geometry.OpaqueSubsetsByFace);
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, height, height2, height3, height4, color, color, color, color, color, -1, subsetsByFace);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if (toolLevel >= RequiredToolLevel)
		{
			int num = Random.Int(1, 2);
			for (int i = 0; i < num; i++)
			{
				dropValues.Add(new BlockDropValue
				{
					Value = Terrain.MakeBlockValue(85),
					Count = 1
				});
			}
		}
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, GetSizeFromValue(value), 1f))
		};
	}

	public static float GetSizeFromValue(int value)
	{
		int num = Terrain.ExtractData(value);
		return 0.07f * (float)num;
	}

	public static int SetSizeInValue(int value, float size)
	{
		int data = MathUtils.Clamp((int)(size / 0.07f), 0, int.MaxValue);
		return Terrain.ReplaceData(value, data);
	}

	private float CalculateNeighborSnowHeight(int value)
	{
		if (Terrain.ExtractContents(value) == 61)
		{
			return GetSizeFromValue(value);
		}
		return 0f;
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Matrix matrix2 = Matrix.CreateScale(1f, 0.1f, 1f) * matrix;
		BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix2, color, color, environmentData);
	}

	public override bool IsInteractive(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null)
		{
			SubsystemPlayers subsystemPlayers = subsystemTerrain.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			if (subsystemPlayers.MainPlayer != null && Terrain.ExtractContents(subsystemPlayers.MainPlayer.ComponentMiner.ActiveBlockValue) == 61)
			{
				return true;
			}
		}
		return false;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		int num = Terrain.ExtractContents(neighborValue);
		if (num == 61)
		{
			return false;
		}
		if (face == 5 && num == 348)
		{
			return false;
		}
		return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
	}
}
