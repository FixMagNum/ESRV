using System.Collections.Generic;
using Engine.Graphics;

namespace Game;

public class TerrainGeometry
{
	public TerrainGeometrySubset SubsetOpaque;

	public TerrainGeometrySubset SubsetAlphaTest;

	public TerrainGeometrySubset SubsetTransparent;

	public TerrainGeometrySubset[] OpaqueSubsetsByFace;

	public TerrainGeometrySubset[] AlphaTestSubsetsByFace;

	public TerrainGeometrySubset[] TransparentSubsetsByFace;

	public TerrainGeometrySubset[] Subsets;

	public Dictionary<Texture2D, TerrainGeometry[]> Draws;

	public int slice;

	public TerrainGeometry(Dictionary<Texture2D, TerrainGeometry[]> Draws, int slice = 0)
	{
		InitSubsets();
		this.Draws = Draws;
		this.slice = slice;
	}

	public TerrainGeometry()
	{
		InitSubsets();
	}

	public void InitSubsets()
	{
		Subsets = new TerrainGeometrySubset[7];
		for (int i = 0; i < 7; i++)
		{
			Subsets[i] = new TerrainGeometrySubset();
		}
		SubsetOpaque = Subsets[4];
		SubsetAlphaTest = Subsets[5];
		SubsetTransparent = Subsets[6];
		OpaqueSubsetsByFace = new TerrainGeometrySubset[6]
		{
			Subsets[0],
			Subsets[1],
			Subsets[2],
			Subsets[3],
			Subsets[4],
			Subsets[4]
		};
		AlphaTestSubsetsByFace = new TerrainGeometrySubset[6]
		{
			Subsets[5],
			Subsets[5],
			Subsets[5],
			Subsets[5],
			Subsets[5],
			Subsets[5]
		};
		TransparentSubsetsByFace = new TerrainGeometrySubset[6]
		{
			Subsets[6],
			Subsets[6],
			Subsets[6],
			Subsets[6],
			Subsets[6],
			Subsets[6]
		};
	}

	public TerrainGeometry GetGeometry(Texture2D texture)
	{
		if (Draws.TryGetValue(texture, out var value))
		{
			return value[slice];
		}
		TerrainGeometry[] array = new TerrainGeometry[16];
		for (int i = 0; i < 16; i++)
		{
			TerrainGeometry terrainGeometry = new TerrainGeometry(Draws, i);
			array[i] = terrainGeometry;
		}
		Draws.Add(texture, array);
		return array[slice];
	}
}
