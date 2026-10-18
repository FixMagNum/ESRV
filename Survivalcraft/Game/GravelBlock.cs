using System.Collections.Generic;
using Engine;

namespace Game;

public class GravelBlock : CubeBlock
{
	public const int Index = 6;

	private static readonly Random _rand = new Random();

	public override void GetDropValues(SubsystemTerrain sst, int oldValue, int newValue, int tl, List<BlockDropValue> drops, out bool showDebris)
	{
		showDebris = true;
		if (tl < RequiredToolLevel)
		{
			return;
		}
		if (_rand.Float(0f, 1f) < 0.33f)
		{
			base.GetDropValues(sst, oldValue, newValue, tl, drops, out showDebris);
			return;
		}
		int num = _rand.Int(1, 3);
		for (int i = 0; i < num; i++)
		{
			drops.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(79),
				Count = 1
			});
		}
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		g.GenerateNaturalCubeVertices(this, value, x, y, z, Color.White, geom.OpaqueSubsetsByFace);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner m, int value, TerrainRaycastResult hit)
	{
		return new BlockPlacementData
		{
			Value = value,
			CellFace = hit.CellFace
		};
	}
}
