using Engine;

namespace Game;

public class TerrainGeometrySubset
{
	public DynamicArray<TerrainVertex> Vertices = new DynamicArray<TerrainVertex>();

	public DynamicArray<int> Indices = new DynamicArray<int>();

	public TerrainGeometrySubset()
	{
	}

	public TerrainGeometrySubset(DynamicArray<TerrainVertex> vertices, DynamicArray<int> indices)
	{
		Vertices = vertices;
		Indices = indices;
	}
}
