using System.Collections.Generic;
using Engine;

namespace Game;

public class LargeSnowBlock : CubeBlock
{
	public const int Index = 348;

	public BoundingBox[] m_collisionBoxes = new BoundingBox[1]
	{
		new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f))
	};

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		generator.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geometry.OpaqueSubsetsByFace);
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		showDebris = true;
		if (toolLevel >= RequiredToolLevel)
		{
			int num = Random.Int(6, 8);
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
		return m_collisionBoxes;
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		int num = Terrain.ExtractContents(neighborValue);
		if (num == 348)
		{
			return false;
		}
		if (face == 4 && num == 61)
		{
			return false;
		}
		return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
	}
}
