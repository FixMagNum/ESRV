using System.Collections.Generic;
using Engine;

namespace Game;

public interface IWorldEditOverlayContext
{
	bool HasP1 { get; }

	bool HasP2 { get; }

	bool HasP3 { get; }

	TerrainRaycastResult P1 { get; }

	TerrainRaycastResult P2 { get; }

	TerrainRaycastResult P3 { get; }

	int MemCount { get; }

	IReadOnlyList<BlockMem> Memory { get; }

	SubsystemTerrain SubsystemTerrain { get; }

	void GetMemBounds(out Point3 min, out Point3 max);

	FurnitureDesign TryGetMemFurnitureDesign(int contents, int designIndex);
}
