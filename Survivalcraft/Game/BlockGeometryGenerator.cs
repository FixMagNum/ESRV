using System;
using System.Runtime.CompilerServices;
using Engine;

namespace Game;

public class BlockGeometryGenerator
{
	public struct CornerLights
	{
		public float L000;

		public float L001;

		public float L010;

		public float L011;

		public float L100;

		public float L101;

		public float L110;

		public float L111;
	}

	public static Vector2[] m_textureCoordinates;

	public readonly Terrain Terrain;

	public readonly SubsystemTerrain SubsystemTerrain;

	public readonly SubsystemElectricity SubsystemElectricity;

	public readonly SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior;

	public readonly SubsystemMetersBlockBehavior SubsystemMetersBlockBehavior;

	public readonly SubsystemPalette SubsystemPalette;

	public DynamicArray<ElectricConnectionPath> m_tmpConnectionPaths = new DynamicArray<ElectricConnectionPath>();

	public Point3 m_cornerLightsPosition;

	public CornerLights[] m_cornerLightsByFace = new CornerLights[6];

	public bool[] m_visibleSides = new bool[6];

	public BlockGeometryGenerator(Terrain terrain, SubsystemTerrain subsystemTerrain, SubsystemElectricity subsystemElectricity, SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior, SubsystemMetersBlockBehavior subsystemMetersBlockBehavior, SubsystemPalette subsystemPalette)
	{
		Terrain = terrain;
		SubsystemTerrain = subsystemTerrain;
		SubsystemElectricity = subsystemElectricity;
		SubsystemFurnitureBlockBehavior = subsystemFurnitureBlockBehavior;
		SubsystemMetersBlockBehavior = subsystemMetersBlockBehavior;
		SubsystemPalette = subsystemPalette;
		ResetCache();
	}

	public void ResetCache()
	{
		m_cornerLightsPosition = new Point3(int.MaxValue);
	}

	public static void SetupCornerVertex(float x, float y, float z, Color color, int light, int face, int textureSlot, int textureSlotCount, int corner, ref TerrainVertex vertex)
	{
		float num = LightingManager.LightIntensityByLightValueAndFace[light + 16 * face];
		Color color2 = new Color((byte)((float)(int)color.R * num), (byte)((float)(int)color.G * num), (byte)((float)(int)color.B * num), color.A);
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % textureSlotCount)) / (float)textureSlotCount;
		float ty = (m_textureCoordinates[corner].Y + (float)(textureSlot / textureSlotCount)) / (float)textureSlotCount;
		SetupVertex(x, y, z, color2, tx, ty, ref vertex);
	}

	public static void SetupLitCornerVertex(float x, float y, float z, Color color, int textureSlot, int corner, ref TerrainVertex vertex)
	{
		SetupLitCornerVertex(x, y, z, color, textureSlot, 16, corner, ref vertex);
	}

	public static void SetupLitCornerVertex(float x, float y, float z, Color color, int textureSlot, int textureSlotCount, int corner, ref TerrainVertex vertex)
	{
		float tx = (m_textureCoordinates[corner].X + (float)(textureSlot % textureSlotCount)) / (float)textureSlotCount;
		float ty = (m_textureCoordinates[corner].Y + (float)(textureSlot / textureSlotCount)) / (float)textureSlotCount;
		SetupVertex(x, y, z, color, tx, ty, ref vertex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetupVertex(float x, float y, float z, Color color, float tx, float ty, ref TerrainVertex vertex)
	{
		vertex.X = x;
		vertex.Y = y;
		vertex.Z = z;
		vertex.Tx = (short)(tx * 32767f);
		vertex.Ty = (short)(ty * 32767f);
		vertex.Color = color;
	}

	public void GenerateCrossfaceVertices(Block block, int value, int x, int y, int z, Color color, int textureSlot, TerrainGeometrySubset subset)
	{
		int num = Terrain.ExtractLight(value);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 64];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		DynamicArray<int> indices = subset.Indices;
		int count = vertices.Count;
		vertices.Count += 8;
		int textureSlotCount = block.GetTextureSlotCount(value);
		float num3 = (float)x + 0.5f;
		float num4 = (float)z + 0.5f;
		float num5 = 0f;
		if (block is CrossBlock)
		{
			int cellValueFast = Terrain.GetCellValueFast(x, y - 1, z);
			int num6 = Terrain.ExtractContents(cellValueFast);
			if (num6 == 355 || num6 == 356)
			{
				num5 = DirtSlabBlock.GetSizeFromValue(cellValueFast) - 1f;
			}
		}
		Vector2 vector;
		Vector2 vector2;
		int corner;
		int corner2;
		int corner3;
		int corner4;
		int corner5;
		int corner6;
		int corner7;
		int corner8;
		if (SettingsManager.RandomVegetationRotation)
		{
			float x2 = SimplexNoise.Noise((float)x * 0.25f, (float)z * 0.25f) * (float)Math.PI * 2f;
			float num7 = 0.70710677f * MathUtils.Cos(x2);
			float num8 = 0.70710677f * MathUtils.Sin(x2);
			vector = new Vector2(num7, num8);
			vector2 = new Vector2(0f - num8, num7);
			corner = 0;
			corner2 = 1;
			corner3 = 2;
			corner4 = 3;
			corner5 = 0;
			corner6 = 1;
			corner7 = 2;
			corner8 = 3;
		}
		else
		{
			vector = new Vector2(0.5f, 0.5f);
			vector2 = new Vector2(0.5f, -0.5f);
			if ((x & 1) == 0)
			{
				corner = 0;
				corner2 = 1;
				corner3 = 2;
				corner4 = 3;
			}
			else
			{
				corner = 1;
				corner2 = 0;
				corner3 = 3;
				corner4 = 2;
			}
			if ((z & 1) == 0)
			{
				corner5 = 0;
				corner6 = 1;
				corner7 = 2;
				corner8 = 3;
			}
			else
			{
				corner5 = 1;
				corner6 = 0;
				corner7 = 3;
				corner8 = 2;
			}
		}
		SetupLitCornerVertex(num3 - vector.X, (float)y + num5, num4 - vector.Y, color2, textureSlot, textureSlotCount, corner, ref vertices.Array[count]);
		SetupLitCornerVertex(num3 + vector.X, (float)y + num5, num4 + vector.Y, color2, textureSlot, textureSlotCount, corner2, ref vertices.Array[count + 1]);
		SetupLitCornerVertex(num3 + vector.X, (float)(y + 1) + num5, num4 + vector.Y, color2, textureSlot, textureSlotCount, corner3, ref vertices.Array[count + 2]);
		SetupLitCornerVertex(num3 - vector.X, (float)(y + 1) + num5, num4 - vector.Y, color2, textureSlot, textureSlotCount, corner4, ref vertices.Array[count + 3]);
		SetupLitCornerVertex(num3 - vector2.X, (float)y + num5, num4 - vector2.Y, color2, textureSlot, textureSlotCount, corner5, ref vertices.Array[count + 4]);
		SetupLitCornerVertex(num3 + vector2.X, (float)y + num5, num4 + vector2.Y, color2, textureSlot, textureSlotCount, corner6, ref vertices.Array[count + 5]);
		SetupLitCornerVertex(num3 + vector2.X, (float)(y + 1) + num5, num4 + vector2.Y, color2, textureSlot, textureSlotCount, corner7, ref vertices.Array[count + 6]);
		SetupLitCornerVertex(num3 - vector2.X, (float)(y + 1) + num5, num4 - vector2.Y, color2, textureSlot, textureSlotCount, corner8, ref vertices.Array[count + 7]);
		int count2 = indices.Count;
		indices.Count += 24;
		int[] array = indices.Array;
		array[count2] = count;
		array[count2 + 1] = count + 1;
		array[count2 + 2] = count + 2;
		array[count2 + 3] = count + 2;
		array[count2 + 4] = count + 1;
		array[count2 + 5] = count;
		array[count2 + 6] = count + 2;
		array[count2 + 7] = count + 3;
		array[count2 + 8] = count;
		array[count2 + 9] = count;
		array[count2 + 10] = count + 3;
		array[count2 + 11] = count + 2;
		array[count2 + 12] = count + 4;
		array[count2 + 13] = count + 5;
		array[count2 + 14] = count + 6;
		array[count2 + 15] = count + 6;
		array[count2 + 16] = count + 5;
		array[count2 + 17] = count + 4;
		array[count2 + 18] = count + 6;
		array[count2 + 19] = count + 7;
		array[count2 + 20] = count + 4;
		array[count2 + 21] = count + 4;
		array[count2 + 22] = count + 7;
		array[count2 + 23] = count + 6;
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, 0, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, 2, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, 3, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 2;
			indices.Array[count2 + 2] = count + 1;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count;
			indices.Array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, 2, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, 3, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, 0, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = count3;
			indices2.Array[count4 + 1] = count3 + 2;
			indices2.Array[count4 + 2] = count3 + 1;
			indices2.Array[count4 + 3] = count3 + 2;
			indices2.Array[count4 + 4] = count3;
			indices2.Array[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, 0, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, 3, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, 2, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = count5;
			indices3.Array[count6 + 1] = count5 + 1;
			indices3.Array[count6 + 2] = count5 + 2;
			indices3.Array[count6 + 3] = count5 + 2;
			indices3.Array[count6 + 4] = count5 + 3;
			indices3.Array[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, 0, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, 3, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, 2, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = count7;
			indices4.Array[count8 + 1] = count7 + 1;
			indices4.Array[count8 + 2] = count7 + 2;
			indices4.Array[count8 + 3] = count7 + 2;
			indices4.Array[count8 + 4] = count7 + 3;
			indices4.Array[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value))
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, 3, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, 2, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, 1, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, 0, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = count9;
			indices5.Array[count10 + 1] = count9 + 1;
			indices5.Array[count10 + 2] = count9 + 2;
			indices5.Array[count10 + 3] = count9 + 2;
			indices5.Array[count10 + 4] = count9 + 3;
			indices5.Array[count10 + 5] = count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, 0, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = count11;
			indices6.Array[count12 + 1] = count11 + 2;
			indices6.Array[count12 + 2] = count11 + 1;
			indices6.Array[count12 + 3] = count11 + 2;
			indices6.Array[count12 + 4] = count11;
			indices6.Array[count12 + 5] = count11 + 3;
		}
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, float height11, float height21, float height22, float height12, Color sideColor, Color topColor11, Color topColor21, Color topColor22, Color topColor12, int overrideTopTextureSlot, TerrainGeometrySubset[] subsetsByFace)
	{
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, 0, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, height22, 2, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, height12, 3, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 2;
			indices.Array[count2 + 2] = count + 1;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count;
			indices.Array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, height21, 2, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, height22, 3, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, 0, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = count3;
			indices2.Array[count4 + 1] = count3 + 2;
			indices2.Array[count4 + 2] = count3 + 1;
			indices2.Array[count4 + 3] = count3 + 2;
			indices2.Array[count4 + 4] = count3;
			indices2.Array[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, 0, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, height21, 3, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, height11, 2, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = count5;
			indices3.Array[count6 + 1] = count5 + 1;
			indices3.Array[count6 + 2] = count5 + 2;
			indices3.Array[count6 + 3] = count5 + 2;
			indices3.Array[count6 + 4] = count5 + 3;
			indices3.Array[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, 0, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, height11, 3, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, height12, 2, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = count7;
			indices4.Array[count8 + 1] = count7 + 1;
			indices4.Array[count8 + 2] = count7 + 2;
			indices4.Array[count8 + 3] = count7 + 2;
			indices4.Array[count8 + 4] = count7 + 3;
			indices4.Array[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value) || height11 < 1f || height12 < 1f || height21 < 1f || height22 < 1f)
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int textureSlot = ((overrideTopTextureSlot >= 0) ? overrideTopTextureSlot : block.GetFaceTextureSlot(4, value));
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, height11, 3, textureSlot, textureSlotCount, topColor11, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, height21, 2, textureSlot, textureSlotCount, topColor21, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, height22, 1, textureSlot, textureSlotCount, topColor22, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, height12, 0, textureSlot, textureSlotCount, topColor12, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = count9;
			indices5.Array[count10 + 1] = count9 + 1;
			indices5.Array[count10 + 2] = count9 + 2;
			indices5.Array[count10 + 3] = count9 + 2;
			indices5.Array[count10 + 4] = count9 + 3;
			indices5.Array[count10 + 5] = count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, 0, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = count11;
			indices6.Array[count12 + 1] = count11 + 2;
			indices6.Array[count12 + 2] = count11 + 1;
			indices6.Array[count12 + 3] = count11 + 2;
			indices6.Array[count12 + 4] = count11;
			indices6.Array[count12 + 5] = count11 + 3;
		}
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, int rotationX, int rotationY, int rotationZ, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, rotationZ, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1 + rotationZ, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, 2 + rotationZ, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, 3 + rotationZ, faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 2;
			indices.Array[count2 + 2] = count + 1;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count;
			indices.Array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1 + rotationX, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, 2 + rotationX, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, 3 + rotationX, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, rotationX, faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = count3;
			indices2.Array[count4 + 1] = count3 + 2;
			indices2.Array[count4 + 2] = count3 + 1;
			indices2.Array[count4 + 3] = count3 + 2;
			indices2.Array[count4 + 4] = count3;
			indices2.Array[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1 + rotationZ, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, rotationZ, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, 3 + rotationZ, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, 2 + rotationZ, faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = count5;
			indices3.Array[count6 + 1] = count5 + 1;
			indices3.Array[count6 + 2] = count5 + 2;
			indices3.Array[count6 + 3] = count5 + 2;
			indices3.Array[count6 + 4] = count5 + 3;
			indices3.Array[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, rotationX, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, 3 + rotationX, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, 2 + rotationX, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1 + rotationX, faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = count7;
			indices4.Array[count8 + 1] = count7 + 1;
			indices4.Array[count8 + 2] = count7 + 2;
			indices4.Array[count8 + 3] = count7 + 2;
			indices4.Array[count8 + 4] = count7 + 3;
			indices4.Array[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value))
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, 3 + rotationY, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, 2 + rotationY, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, 1 + rotationY, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, rotationY, faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = count9;
			indices5.Array[count10 + 1] = count9 + 1;
			indices5.Array[count10 + 2] = count9 + 2;
			indices5.Array[count10 + 3] = count9 + 2;
			indices5.Array[count10 + 4] = count9 + 3;
			indices5.Array[count10 + 5] = count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, rotationY, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1 + rotationY, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2 + rotationY, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3 + rotationY, faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = count11;
			indices6.Array[count12 + 1] = count11 + 2;
			indices6.Array[count12 + 2] = count11 + 1;
			indices6.Array[count12 + 3] = count11 + 2;
			indices6.Array[count12 + 4] = count11;
			indices6.Array[count12 + 5] = count11 + 3;
		}
	}

	public void GenerateMeshVertices(Block block, int x, int y, int z, BlockMesh blockMesh, Color color, Matrix? matrix, TerrainGeometrySubset subset)
	{
		DynamicArray<TerrainVertex> color2 = subset.Vertices;
		DynamicArray<int> indices = subset.Indices;
		int count = color2.Count;
		int cellLightFast = Terrain.GetCellLightFast(x, y, z);
		float num = LightingManager.LightIntensityByLightValue[cellLightFast] * (1f / 255f);
		color2.Count += blockMesh.Vertices.Count;
		bool hasValue = matrix.HasValue;
		Matrix m = (hasValue ? matrix.Value : default(Matrix));
		float num2 = (int)color.R;
		float num3 = (int)color.G;
		float num4 = (int)color.B;
		for (int i = 0; i < blockMesh.Vertices.Count; i++)
		{
			BlockMeshVertex blockMeshVertex = blockMesh.Vertices.Array[i];
			Vector3 v = blockMeshVertex.Position;
			if (hasValue)
			{
				v = Vector3.Transform(v, m);
			}
			SetupVertex(color: (!blockMeshVertex.IsEmissive) ? new Color((byte)(num2 * (float)(int)blockMeshVertex.Color.R * num), (byte)(num3 * (float)(int)blockMeshVertex.Color.G * num), (byte)(num4 * (float)(int)blockMeshVertex.Color.B * num)) : new Color((byte)(num2 * (float)(int)blockMeshVertex.Color.R * (1f / 255f)), (byte)(num3 * (float)(int)blockMeshVertex.Color.G * (1f / 255f)), (byte)(num4 * (float)(int)blockMeshVertex.Color.B * (1f / 255f))), x: (float)x + v.X, y: (float)y + v.Y, z: (float)z + v.Z, tx: blockMeshVertex.TextureCoordinates.X, ty: blockMeshVertex.TextureCoordinates.Y, vertex: ref color2.Array[count + i]);
		}
		if (blockMesh.Sides != null)
		{
			for (int j = 0; j < 6; j++)
			{
				Point3 point = CellFace.FaceToPoint3(j);
				int cellValueFastChunkExists = Terrain.GetCellValueFastChunkExists(x + point.X, y + point.Y, z + point.Z);
				m_visibleSides[j] = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFastChunkExists)].IsFaceTransparent(SubsystemTerrain, CellFace.OppositeFace(j), cellValueFastChunkExists);
			}
			for (int k = 0; k < blockMesh.Indices.Count / 3; k++)
			{
				int num5 = ((blockMesh.Sides == null) ? (-1) : blockMesh.Sides.Array[k]);
				if (num5 < 0 || m_visibleSides[num5])
				{
					indices.Add(blockMesh.Indices.Array[3 * k] + count);
					indices.Add(blockMesh.Indices.Array[3 * k + 1] + count);
					indices.Add(blockMesh.Indices.Array[3 * k + 2] + count);
				}
			}
		}
		else
		{
			for (int l = 0; l < blockMesh.Indices.Count; l++)
			{
				indices.Add(blockMesh.Indices.Array[l] + count);
			}
		}
	}

	public void GenerateShadedMeshVertices(Block block, int x, int y, int z, BlockMesh blockMesh, Color color, Matrix? matrix, int[] facesMap, TerrainGeometrySubset subset)
	{
		CalculateCornerLights(x, y, z);
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		DynamicArray<int> indices = subset.Indices;
		int count = vertices.Count;
		vertices.Count += blockMesh.Vertices.Count;
		float num = (int)color.R;
		float num2 = (int)color.G;
		float num3 = (int)color.B;
		bool hasValue = matrix.HasValue;
		Matrix m = (hasValue ? matrix.Value : default(Matrix));
		for (int i = 0; i < blockMesh.Vertices.Count; i++)
		{
			BlockMeshVertex blockMeshVertex = blockMesh.Vertices.Array[i];
			Vector3 vector = blockMeshVertex.Position;
			if (hasValue)
			{
				vector = Vector3.Transform(vector, m);
			}
			Color color2;
			if (blockMeshVertex.IsEmissive)
			{
				color2 = new Color((byte)(num * (float)(int)blockMeshVertex.Color.R * (1f / 255f)), (byte)(num2 * (float)(int)blockMeshVertex.Color.G * (1f / 255f)), (byte)(num3 * (float)(int)blockMeshVertex.Color.B * (1f / 255f)));
			}
			else
			{
				int face = ((facesMap != null) ? facesMap[blockMeshVertex.Face] : blockMeshVertex.Face);
				float num4 = InterpolateCornerLights(face, vector) * (1f / 255f);
				color2 = new Color((byte)(num * (float)(int)blockMeshVertex.Color.R * num4), (byte)(num2 * (float)(int)blockMeshVertex.Color.G * num4), (byte)(num3 * (float)(int)blockMeshVertex.Color.B * num4));
			}
			SetupVertex((float)x + vector.X, (float)y + vector.Y, (float)z + vector.Z, color2, blockMeshVertex.TextureCoordinates.X, blockMeshVertex.TextureCoordinates.Y, ref vertices.Array[count + i]);
		}
		if (blockMesh.Sides != null)
		{
			for (int j = 0; j < 6; j++)
			{
				Point3 point = CellFace.FaceToPoint3(j);
				int cellValueFastChunkExists = Terrain.GetCellValueFastChunkExists(x + point.X, y + point.Y, z + point.Z);
				m_visibleSides[j] = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFastChunkExists)].IsFaceTransparent(SubsystemTerrain, CellFace.OppositeFace(j), cellValueFastChunkExists);
			}
			for (int k = 0; k < blockMesh.Indices.Count / 3; k++)
			{
				int num5 = ((blockMesh.Sides == null) ? (-1) : blockMesh.Sides.Array[k]);
				if (num5 < 0 || m_visibleSides[(facesMap != null) ? facesMap[num5] : num5])
				{
					indices.Add(blockMesh.Indices.Array[3 * k] + count);
					indices.Add(blockMesh.Indices.Array[3 * k + 1] + count);
					indices.Add(blockMesh.Indices.Array[3 * k + 2] + count);
				}
			}
		}
		else
		{
			for (int l = 0; l < blockMesh.Indices.Count; l++)
			{
				indices.Add(blockMesh.Indices.Array[l] + count);
			}
		}
	}

	public void GenerateWireVertices(int value, int x, int y, int z, int mountingFace, float centerBoxSize, Vector2 centerOffset, TerrainGeometrySubset subset)
	{
		if (SubsystemElectricity == null)
		{
			return;
		}
		Color color = WireBlock.WireColor;
		int num = Terrain.ExtractContents(value);
		if (num == 133)
		{
			int? color2 = WireBlock.GetColor(Terrain.ExtractData(value));
			if (color2.HasValue)
			{
				color = SubsystemPalette.GetColor(this, color2);
			}
		}
		int num2 = Terrain.ExtractLight(value);
		float num3 = LightingManager.LightIntensityByLightValue[num2];
		Vector3 vector = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f) - 0.5f * CellFace.FaceToVector3(mountingFace);
		Vector3 vector2 = CellFace.FaceToVector3(mountingFace);
		Vector2 vector3 = new Vector2(0.9376f, 0.0001f);
		Vector2 vector4 = new Vector2(1f / 32f, 0.0055078124f);
		Point3 point = CellFace.FaceToPoint3(mountingFace);
		int cellContents = Terrain.GetCellContents(x - point.X, y - point.Y, z - point.Z);
		bool flag = cellContents == 2 || cellContents == 7 || cellContents == 8 || cellContents == 6 || cellContents == 62 || cellContents == 72;
		Vector3 vector5 = CellFace.FaceToVector3(SubsystemElectricity.GetConnectorFace(mountingFace, ElectricConnectorDirection.Top));
		Vector3 vector6 = CellFace.FaceToVector3(SubsystemElectricity.GetConnectorFace(mountingFace, ElectricConnectorDirection.Left)) * centerOffset.X + vector5 * centerOffset.Y;
		int num4 = 0;
		m_tmpConnectionPaths.Clear();
		SubsystemElectricity.GetAllConnectedNeighbors(x, y, z, mountingFace, m_tmpConnectionPaths);
		foreach (ElectricConnectionPath tmpConnectionPath in m_tmpConnectionPaths)
		{
			if ((num4 & (1 << tmpConnectionPath.ConnectorFace)) != 0)
			{
				continue;
			}
			ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(mountingFace, 0, tmpConnectionPath.ConnectorFace);
			if (centerOffset == Vector2.Zero && connectorDirection == ElectricConnectorDirection.In)
			{
				continue;
			}
			num4 |= 1 << tmpConnectionPath.ConnectorFace;
			Color color3 = color;
			if (num != 133)
			{
				int cellValue = Terrain.GetCellValue(x + tmpConnectionPath.NeighborOffsetX, y + tmpConnectionPath.NeighborOffsetY, z + tmpConnectionPath.NeighborOffsetZ);
				if (Terrain.ExtractContents(cellValue) == 133)
				{
					int? color4 = WireBlock.GetColor(Terrain.ExtractData(cellValue));
					if (color4.HasValue)
					{
						color3 = SubsystemPalette.GetColor(this, color4);
					}
				}
			}
			Vector3 vector7 = ((connectorDirection != ElectricConnectorDirection.In) ? CellFace.FaceToVector3(tmpConnectionPath.ConnectorFace) : (-Vector3.Normalize(vector6)));
			Vector3 vector8 = Vector3.Cross(vector2, vector7);
			float num5 = ((centerBoxSize >= 0f) ? MathUtils.Max(1f / 32f, centerBoxSize / 2f) : (centerBoxSize / 2f));
			float num6 = ((connectorDirection == ElectricConnectorDirection.In) ? (1f / 32f) : 0.5f);
			float num7;
			if (connectorDirection == ElectricConnectorDirection.In)
			{
				num7 = 0f;
			}
			else if (tmpConnectionPath.ConnectorFace == tmpConnectionPath.NeighborFace)
			{
				num7 = num6 + 1f / 32f;
			}
			else
			{
				num7 = ((tmpConnectionPath.ConnectorFace != CellFace.OppositeFace(tmpConnectionPath.NeighborFace)) ? num6 : (num6 - 1f / 32f));
			}
			Vector3 vector9 = vector - vector8 * (1f / 32f) + vector7 * num5 + vector6;
			Vector3 vector10 = vector - vector8 * (1f / 32f) + vector7 * num6;
			Vector3 vector11 = vector + vector8 * (1f / 32f) + vector7 * num6;
			Vector3 vector12 = vector + vector8 * (1f / 32f) + vector7 * num5 + vector6;
			Vector3 vector13 = vector + vector2 * (1f / 32f) + vector7 * (centerBoxSize / 2f) + vector6;
			Vector3 vector14 = vector + vector2 * (1f / 32f) + vector7 * num7;
			if (flag && centerBoxSize == 0f)
			{
				Vector3 vector15 = 0.25f * GetRandomWireOffset(0.5f * (vector9 + vector12), vector2);
				vector9 += vector15;
				vector12 += vector15;
				vector13 += vector15;
			}
			Vector2 vector16 = vector3 + vector4 * new Vector2(MathUtils.Max(0.0625f, centerBoxSize), 0f);
			Vector2 vector17 = vector3 + vector4 * new Vector2(num6 * 2f, 0f);
			Vector2 vector18 = vector3 + vector4 * new Vector2(num6 * 2f, 1f);
			Vector2 vector19 = vector3 + vector4 * new Vector2(MathUtils.Max(0.0625f, centerBoxSize), 1f);
			Vector2 vector20 = vector3 + vector4 * new Vector2(centerBoxSize, 0.5f);
			Vector2 vector21 = vector3 + vector4 * new Vector2(num7 * 2f, 0.5f);
			int num8 = Terrain.ExtractLight(Terrain.GetCellValue(x + tmpConnectionPath.NeighborOffsetX, y + tmpConnectionPath.NeighborOffsetY, z + tmpConnectionPath.NeighborOffsetZ));
			float num9 = LightingManager.LightIntensityByLightValue[num8];
			float num10 = 0.5f * (num3 + num9);
			float num11 = LightingManager.CalculateLighting(-vector8);
			float num12 = LightingManager.CalculateLighting(vector8);
			float num13 = LightingManager.CalculateLighting(vector2);
			float num14 = num11 * num3;
			float num15 = num11 * num10;
			float num16 = num12 * num10;
			float num17 = num12 * num3;
			float num18 = num13 * num3;
			float num19 = num13 * num10;
			Color color5 = new Color((byte)((float)(int)color3.R * num14), (byte)((float)(int)color3.G * num14), (byte)((float)(int)color3.B * num14));
			Color color6 = new Color((byte)((float)(int)color3.R * num15), (byte)((float)(int)color3.G * num15), (byte)((float)(int)color3.B * num15));
			Color color7 = new Color((byte)((float)(int)color3.R * num16), (byte)((float)(int)color3.G * num16), (byte)((float)(int)color3.B * num16));
			Color color8 = new Color((byte)((float)(int)color3.R * num17), (byte)((float)(int)color3.G * num17), (byte)((float)(int)color3.B * num17));
			Color color9 = new Color((byte)((float)(int)color3.R * num18), (byte)((float)(int)color3.G * num18), (byte)((float)(int)color3.B * num18));
			Color color10 = new Color((byte)((float)(int)color3.R * num19), (byte)((float)(int)color3.G * num19), (byte)((float)(int)color3.B * num19));
			int count = subset.Vertices.Count;
			subset.Vertices.Count += 6;
			TerrainVertex[] array = subset.Vertices.Array;
			SetupVertex(vector9.X, vector9.Y, vector9.Z, color5, vector16.X, vector16.Y, ref array[count]);
			SetupVertex(vector10.X, vector10.Y, vector10.Z, color6, vector17.X, vector17.Y, ref array[count + 1]);
			SetupVertex(vector11.X, vector11.Y, vector11.Z, color7, vector18.X, vector18.Y, ref array[count + 2]);
			SetupVertex(vector12.X, vector12.Y, vector12.Z, color8, vector19.X, vector19.Y, ref array[count + 3]);
			SetupVertex(vector13.X, vector13.Y, vector13.Z, color9, vector20.X, vector20.Y, ref array[count + 4]);
			SetupVertex(vector14.X, vector14.Y, vector14.Z, color10, vector21.X, vector21.Y, ref array[count + 5]);
			int count2 = subset.Indices.Count;
			DynamicArray<int> indices = subset.Indices;
			int count3 = indices.Count;
			indices.Count = count3 + ((connectorDirection == ElectricConnectorDirection.In) ? 15 : 12);
			int[] array2 = subset.Indices.Array;
			array2[count2] = count;
			array2[count2 + 1] = count + 5;
			array2[count2 + 2] = count + 1;
			array2[count2 + 3] = count + 5;
			array2[count2 + 4] = count;
			array2[count2 + 5] = count + 4;
			array2[count2 + 6] = count + 4;
			array2[count2 + 7] = count + 2;
			array2[count2 + 8] = count + 5;
			array2[count2 + 9] = count + 2;
			array2[count2 + 10] = count + 4;
			array2[count2 + 11] = count + 3;
			if (connectorDirection == ElectricConnectorDirection.In)
			{
				array2[count2 + 12] = count + 2;
				array2[count2 + 13] = count + 1;
				array2[count2 + 14] = count + 5;
			}
		}
		if (centerBoxSize != 0f || (num4 == 0 && num != 133))
		{
			return;
		}
		for (int i = 0; i < 6; i++)
		{
			if (i != mountingFace && i != CellFace.OppositeFace(mountingFace) && (num4 & (1 << i)) == 0)
			{
				Vector3 vector22 = CellFace.FaceToVector3(i);
				Vector3 vector23 = Vector3.Cross(vector2, vector22);
				Vector3 vector24 = vector - vector23 * (1f / 32f) + vector22 * (1f / 32f);
				Vector3 vector25 = vector + vector23 * (1f / 32f) + vector22 * (1f / 32f);
				Vector3 vector26 = vector + vector2 * (1f / 32f);
				if (flag)
				{
					Vector3 vector27 = 0.25f * GetRandomWireOffset(0.5f * (vector24 + vector25), vector2);
					vector24 += vector27;
					vector25 += vector27;
					vector26 += vector27;
				}
				Vector2 vector28 = vector3 + vector4 * new Vector2(0.0625f, 0f);
				Vector2 vector29 = vector3 + vector4 * new Vector2(0.0625f, 1f);
				Vector2 vector30 = vector3 + vector4 * new Vector2(0f, 0.5f);
				float num20 = LightingManager.CalculateLighting(vector22) * num3;
				float num21 = LightingManager.CalculateLighting(vector2) * num3;
				Color color11 = new Color((byte)((float)(int)color.R * num20), (byte)((float)(int)color.G * num20), (byte)((float)(int)color.B * num20));
				Color color12 = new Color((byte)((float)(int)color.R * num21), (byte)((float)(int)color.G * num21), (byte)((float)(int)color.B * num21));
				int count4 = subset.Vertices.Count;
				subset.Vertices.Count += 3;
				TerrainVertex[] array3 = subset.Vertices.Array;
				SetupVertex(vector24.X, vector24.Y, vector24.Z, color11, vector28.X, vector28.Y, ref array3[count4]);
				SetupVertex(vector25.X, vector25.Y, vector25.Z, color11, vector29.X, vector29.Y, ref array3[count4 + 1]);
				SetupVertex(vector26.X, vector26.Y, vector26.Z, color12, vector30.X, vector30.Y, ref array3[count4 + 2]);
				int count5 = subset.Indices.Count;
				subset.Indices.Count += 3;
				int[] array4 = subset.Indices.Array;
				array4[count5] = count4;
				array4[count5 + 1] = count4 + 2;
				array4[count5 + 2] = count4 + 1;
			}
		}
	}

	public static void CalculateCubeVertexLight(int value, ref int light, ref int shadow)
	{
		int color = Terrain.ExtractContents(value);
		if (color == 0)
		{
			light = Math.Max(light, Terrain.ExtractLight(value));
			return;
		}
		light = Math.Max(light, Terrain.ExtractLight(value));
		shadow += BlocksManager.Blocks[color].GetShadowStrength(value);
	}

	public static int CombineLightAndShadow(int light, int shadow)
	{
		int num = shadow / 7;
		int num2 = ((num > 0) ? num : 0);
		int num3 = light - num2;
		if (num3 <= 0)
		{
			return 0;
		}
		return num3;
	}

	public int CalculateVertexLightFace0(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z);
		int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, z & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z);
		int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	public int CalculateVertexLightFace1(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z - 1);
		int num = TerrainChunk.CalculateCellIndex(x & 0xF, y, (z - 1) & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z);
		int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	public int CalculateVertexLightFace2(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z - 1);
		int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, (z - 1) & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z - 1);
		int num2 = TerrainChunk.CalculateCellIndex(x & 0xF, y, (z - 1) & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	public int CalculateVertexLightFace3(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x - 1, z - 1);
		int num = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, (z - 1) & 0xF);
		int cellValueFast = chunkAtCell.GetCellValueFast(num - 1);
		int cellValueFast2 = chunkAtCell.GetCellValueFast(num);
		CalculateCubeVertexLight(cellValueFast, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast2, ref light, ref shadow);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x - 1, z);
		int num2 = TerrainChunk.CalculateCellIndex((x - 1) & 0xF, y, z & 0xF);
		int cellValueFast3 = chunkAtCell2.GetCellValueFast(num2 - 1);
		int cellValueFast4 = chunkAtCell2.GetCellValueFast(num2);
		CalculateCubeVertexLight(cellValueFast3, ref light, ref shadow);
		CalculateCubeVertexLight(cellValueFast4, ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	public int CalculateVertexLightFace4(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y, z), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y, z), ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	public int CalculateVertexLightFace5(int x, int y, int z)
	{
		int light = 0;
		int shadow = 0;
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y - 1, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y - 1, z - 1), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x - 1, y - 1, z), ref light, ref shadow);
		CalculateCubeVertexLight(Terrain.GetCellValueFastChunkExists(x, y - 1, z), ref light, ref shadow);
		return CombineLightAndShadow(light, shadow);
	}

	public void SetupCubeVertexFace0(int x, int y, int z, float height, int corner, int textureSlot, int textureSlotCount, Color color, ref TerrainVertex vertex)
	{
		float y2 = (float)y + height - 1f;
		int light = CalculateVertexLightFace0(x, y, z);
		SetupCornerVertex(x, y2, z, color, light, 0, textureSlot, textureSlotCount, corner, ref vertex);
	}

	public void SetupCubeVertexFace1(int x, int y, int z, float height, int corner, int textureSlot, int textureSlotCount, Color color, ref TerrainVertex vertex)
	{
		float y2 = (float)y + height - 1f;
		int light = CalculateVertexLightFace1(x, y, z);
		SetupCornerVertex(x, y2, z, color, light, 1, textureSlot, textureSlotCount, corner, ref vertex);
	}

	public void SetupCubeVertexFace2(int x, int y, int z, float height, int corner, int textureSlot, int textureSlotCount, Color color, ref TerrainVertex vertex)
	{
		float y2 = (float)y + height - 1f;
		int light = CalculateVertexLightFace2(x, y, z);
		SetupCornerVertex(x, y2, z, color, light, 2, textureSlot, textureSlotCount, corner, ref vertex);
	}

	public void SetupCubeVertexFace3(int x, int y, int z, float height, int corner, int textureSlot, int textureSlotCount, Color color, ref TerrainVertex vertex)
	{
		float y2 = (float)y + height - 1f;
		int light = CalculateVertexLightFace3(x, y, z);
		SetupCornerVertex(x, y2, z, color, light, 3, textureSlot, textureSlotCount, corner, ref vertex);
	}

	public void SetupCubeVertexFace4(int x, int y, int z, float height, int corner, int textureSlot, int textureSlotCount, Color color, ref TerrainVertex vertex)
	{
		float y2 = (float)y + height - 1f;
		int light = CalculateVertexLightFace4(x, y, z);
		SetupCornerVertex(x, y2, z, color, light, 4, textureSlot, textureSlotCount, corner, ref vertex);
	}

	public void SetupCubeVertexFace5(int x, int y, int z, float height, int corner, int textureSlot, int textureSlotCount, Color color, ref TerrainVertex vertex)
	{
		float y2 = (float)y + height - 1f;
		int light = CalculateVertexLightFace5(x, y, z);
		SetupCornerVertex(x, y2, z, color, light, 5, textureSlot, textureSlotCount, corner, ref vertex);
	}

	public static Vector3 GetRandomWireOffset(Vector3 position, Vector3 normal)
	{
		int hashCode = Vector3.Round(2f * position).GetHashCode();
		return new Vector3
		{
			X = ((normal.X == 0f) ? ((float)(MathUtils.Hash((uint)hashCode) % 255) / 255f - 0.5f) : 0f),
			Y = ((normal.Y == 0f) ? ((float)(MathUtils.Hash((uint)(hashCode + 1)) % 255) / 255f - 0.5f) : 0f),
			Z = ((normal.Z == 0f) ? ((float)(MathUtils.Hash((uint)(hashCode + 2)) % 255) / 255f - 0.5f) : 0f)
		};
	}

	public void CalculateCornerLights(int x, int y, int z)
	{
		if (!(m_cornerLightsPosition == new Point3(x, y, z)))
		{
			m_cornerLightsPosition = new Point3(x, y, z);
			m_cornerLightsByFace[0].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y, z)];
			m_cornerLightsByFace[0].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y, z + 1)];
			m_cornerLightsByFace[0].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y + 1, z)];
			m_cornerLightsByFace[0].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x, y + 1, z + 1)];
			m_cornerLightsByFace[0].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y, z)];
			m_cornerLightsByFace[0].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y, z + 1)];
			m_cornerLightsByFace[0].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y + 1, z)];
			m_cornerLightsByFace[0].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace0(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[1].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y, z)];
			m_cornerLightsByFace[1].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y, z + 1)];
			m_cornerLightsByFace[1].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y + 1, z)];
			m_cornerLightsByFace[1].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x, y + 1, z + 1)];
			m_cornerLightsByFace[1].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y, z)];
			m_cornerLightsByFace[1].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y, z + 1)];
			m_cornerLightsByFace[1].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y + 1, z)];
			m_cornerLightsByFace[1].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace1(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[2].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y, z)];
			m_cornerLightsByFace[2].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y, z + 1)];
			m_cornerLightsByFace[2].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y + 1, z)];
			m_cornerLightsByFace[2].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x, y + 1, z + 1)];
			m_cornerLightsByFace[2].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y, z)];
			m_cornerLightsByFace[2].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y, z + 1)];
			m_cornerLightsByFace[2].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y + 1, z)];
			m_cornerLightsByFace[2].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace2(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[3].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y, z)];
			m_cornerLightsByFace[3].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y, z + 1)];
			m_cornerLightsByFace[3].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y + 1, z)];
			m_cornerLightsByFace[3].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x, y + 1, z + 1)];
			m_cornerLightsByFace[3].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y, z)];
			m_cornerLightsByFace[3].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y, z + 1)];
			m_cornerLightsByFace[3].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y + 1, z)];
			m_cornerLightsByFace[3].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace3(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[4].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y, z)];
			m_cornerLightsByFace[4].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y, z + 1)];
			m_cornerLightsByFace[4].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y + 1, z)];
			m_cornerLightsByFace[4].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x, y + 1, z + 1)];
			m_cornerLightsByFace[4].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y, z)];
			m_cornerLightsByFace[4].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y, z + 1)];
			m_cornerLightsByFace[4].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y + 1, z)];
			m_cornerLightsByFace[4].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace4(x + 1, y + 1, z + 1)];
			m_cornerLightsByFace[5].L000 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y, z)];
			m_cornerLightsByFace[5].L001 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y, z + 1)];
			m_cornerLightsByFace[5].L010 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y + 1, z)];
			m_cornerLightsByFace[5].L011 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x, y + 1, z + 1)];
			m_cornerLightsByFace[5].L100 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y, z)];
			m_cornerLightsByFace[5].L101 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y, z + 1)];
			m_cornerLightsByFace[5].L110 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y + 1, z)];
			m_cornerLightsByFace[5].L111 = LightingManager.LightIntensityByLightValue[CalculateVertexLightFace5(x + 1, y + 1, z + 1)];
		}
	}

	public float InterpolateCornerLights(int face, Vector3 position)
	{
		CornerLights cornerLights = m_cornerLightsByFace[face];
		float x = position.X;
		float y = position.Y;
		float z = position.Z;
		float num = cornerLights.L000 + x * (cornerLights.L100 - cornerLights.L000);
		float num2 = cornerLights.L001 + x * (cornerLights.L101 - cornerLights.L001);
		float num3 = cornerLights.L010 + x * (cornerLights.L110 - cornerLights.L010);
		float num4 = cornerLights.L011 + x * (cornerLights.L111 - cornerLights.L011);
		float num5 = num + y * (num3 - num);
		float num6 = num2 + y * (num4 - num2);
		return num5 + z * (num6 - num5);
	}

	static BlockGeometryGenerator()
	{
		m_textureCoordinates = new Vector2[8]
		{
			new Vector2(0.001f, 0.999f),
			new Vector2(0.999f, 0.999f),
			new Vector2(0.999f, 0.001f),
			new Vector2(0.001f, 0.001f),
			new Vector2(0.001f, 0.999f),
			new Vector2(0.999f, 0.999f),
			new Vector2(0.999f, 0.001f),
			new Vector2(0.001f, 0.001f)
		};
	}

	public void GenerateCubeVertices(Block block, int value, int x, int y, int z, float height11, float height21, float height22, float height12, Color sideColor, Color topColor11, Color topColor21, Color topColor22, Color topColor12, int overrideTopTextureSlot, TerrainGeometrySubset[] subsetsByFace, int rotationIndex)
	{
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, rotationIndex, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, (1 + rotationIndex) % 4, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, height22, (2 + rotationIndex) % 4, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, height12, (3 + rotationIndex) % 4, faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 2;
			indices.Array[count2 + 2] = count + 1;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count;
			indices.Array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, (1 + rotationIndex) % 4, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, height21, (2 + rotationIndex) % 4, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, height22, (3 + rotationIndex) % 4, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, rotationIndex, faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = count3;
			indices2.Array[count4 + 1] = count3 + 2;
			indices2.Array[count4 + 2] = count3 + 1;
			indices2.Array[count4 + 3] = count3 + 2;
			indices2.Array[count4 + 4] = count3;
			indices2.Array[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, (1 + rotationIndex) % 4, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, rotationIndex, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, height21, (3 + rotationIndex) % 4, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, height11, (2 + rotationIndex) % 4, faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = count5;
			indices3.Array[count6 + 1] = count5 + 1;
			indices3.Array[count6 + 2] = count5 + 2;
			indices3.Array[count6 + 3] = count5 + 2;
			indices3.Array[count6 + 4] = count5 + 3;
			indices3.Array[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, rotationIndex, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, height11, (3 + rotationIndex) % 4, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, height12, (2 + rotationIndex) % 4, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, (1 + rotationIndex) % 4, faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = count7;
			indices4.Array[count8 + 1] = count7 + 1;
			indices4.Array[count8 + 2] = count7 + 2;
			indices4.Array[count8 + 3] = count7 + 2;
			indices4.Array[count8 + 4] = count7 + 3;
			indices4.Array[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value) || height11 < 1f || height12 < 1f || height21 < 1f || height22 < 1f)
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int textureSlot = ((overrideTopTextureSlot >= 0) ? overrideTopTextureSlot : block.GetFaceTextureSlot(4, value));
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, height11, (3 + rotationIndex) % 4, textureSlot, textureSlotCount, topColor11, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, height21, (2 + rotationIndex) % 4, textureSlot, textureSlotCount, topColor21, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, height22, (1 + rotationIndex) % 4, textureSlot, textureSlotCount, topColor22, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, height12, rotationIndex, textureSlot, textureSlotCount, topColor12, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = count9;
			indices5.Array[count10 + 1] = count9 + 1;
			indices5.Array[count10 + 2] = count9 + 2;
			indices5.Array[count10 + 3] = count9 + 2;
			indices5.Array[count10 + 4] = count9 + 3;
			indices5.Array[count10 + 5] = count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, rotationIndex, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, (1 + rotationIndex) % 4, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, (2 + rotationIndex) % 4, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, (3 + rotationIndex) % 4, faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = count11;
			indices6.Array[count12 + 1] = count11 + 2;
			indices6.Array[count12 + 2] = count11 + 1;
			indices6.Array[count12 + 3] = count11 + 2;
			indices6.Array[count12 + 4] = count11;
			indices6.Array[count12 + 5] = count11 + 3;
		}
	}

	public void GenerateCrossfaceVertices(Block block, int value, int x, int y, int z, Color color, int textureSlot, TerrainGeometrySubset subset, Vector3 windDisplacementMain, Vector3 windDisplacementSide)
	{
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		DynamicArray<int> indices = subset.Indices;
		int num = Terrain.ExtractLight(value);
		float num2 = LightingManager.LightIntensityByLightValueAndFace[num + 64];
		Color color2 = new Color((byte)((float)(int)color.R * num2), (byte)((float)(int)color.G * num2), (byte)((float)(int)color.B * num2), color.A);
		int count = vertices.Count;
		vertices.Count += 8;
		SetupLitCornerVertex(x, y, z, color2, textureSlot, 0, ref vertices.Array[count]);
		SetupLitCornerVertex(x + 1, y, z + 1, color2, textureSlot, 1, ref vertices.Array[count + 1]);
		SetupLitCornerVertex((float)(x + 1) + windDisplacementMain.X, (float)(y + 1) + windDisplacementMain.Y, (float)(z + 1) + windDisplacementMain.Z, color2, textureSlot, 2, ref vertices.Array[count + 2]);
		SetupLitCornerVertex((float)x + windDisplacementMain.X, (float)(y + 1) + windDisplacementMain.Y, (float)z + windDisplacementMain.Z, color2, textureSlot, 3, ref vertices.Array[count + 3]);
		SetupLitCornerVertex(x, y, z + 1, color2, textureSlot, 0, ref vertices.Array[count + 4]);
		SetupLitCornerVertex(x + 1, y, z, color2, textureSlot, 1, ref vertices.Array[count + 5]);
		SetupLitCornerVertex((float)(x + 1) + windDisplacementSide.X, (float)(y + 1) + windDisplacementSide.Y, (float)z + windDisplacementSide.Z, color2, textureSlot, 2, ref vertices.Array[count + 6]);
		SetupLitCornerVertex((float)x + windDisplacementSide.X, (float)(y + 1) + windDisplacementSide.Y, (float)(z + 1) + windDisplacementSide.Z, color2, textureSlot, 3, ref vertices.Array[count + 7]);
		int count2 = indices.Count;
		indices.Count += 24;
		indices.Array[count2] = count;
		indices.Array[count2 + 1] = count + 1;
		indices.Array[count2 + 2] = count + 2;
		indices.Array[count2 + 3] = count + 2;
		indices.Array[count2 + 4] = count + 1;
		indices.Array[count2 + 5] = count;
		indices.Array[count2 + 6] = count + 2;
		indices.Array[count2 + 7] = count + 3;
		indices.Array[count2 + 8] = count;
		indices.Array[count2 + 9] = count;
		indices.Array[count2 + 10] = count + 3;
		indices.Array[count2 + 11] = count + 2;
		indices.Array[count2 + 12] = count + 4;
		indices.Array[count2 + 13] = count + 5;
		indices.Array[count2 + 14] = count + 6;
		indices.Array[count2 + 15] = count + 6;
		indices.Array[count2 + 16] = count + 5;
		indices.Array[count2 + 17] = count + 4;
		indices.Array[count2 + 18] = count + 6;
		indices.Array[count2 + 19] = count + 7;
		indices.Array[count2 + 20] = count + 4;
		indices.Array[count2 + 21] = count + 4;
		indices.Array[count2 + 22] = count + 7;
		indices.Array[count2 + 23] = count + 6;
	}

	public void GenerateNaturalCubeVertices(Block block, int value, int x, int y, int z, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		int rot = 0;
		bool flipX = false;
		bool flipY = false;
		if (SettingsManager.RandomNaturalBlocksRotation)
		{
			rot = (int)(FastHash(x, y, z) & 3);
			flipX = false;
			flipY = false;
		}
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 0, value: value))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			int[] array = indices.Array;
			array[count2] = count;
			array[count2 + 1] = count + 2;
			array[count2 + 2] = count + 1;
			array[count2 + 3] = count + 2;
			array[count2 + 4] = count;
			array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			int[] array2 = indices2.Array;
			array2[count4] = count3;
			array2[count4 + 1] = count3 + 2;
			array2[count4 + 2] = count3 + 1;
			array2[count4 + 3] = count3 + 2;
			array2[count4 + 4] = count3;
			array2[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			int[] array3 = indices3.Array;
			array3[count6] = count5;
			array3[count6 + 1] = count5 + 1;
			array3[count6 + 2] = count5 + 2;
			array3[count6 + 3] = count5 + 2;
			array3[count6 + 4] = count5 + 3;
			array3[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			int[] array4 = indices4.Array;
			array4[count8] = count7;
			array4[count8 + 1] = count7 + 1;
			array4[count8 + 2] = count7 + 2;
			array4[count8 + 3] = count7 + 2;
			array4[count8 + 4] = count7 + 3;
			array4[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value))
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			int[] array5 = indices5.Array;
			array5[count10] = count9;
			array5[count10 + 1] = count9 + 1;
			array5[count10 + 2] = count9 + 2;
			array5[count10 + 3] = count9 + 2;
			array5[count10 + 4] = count9 + 3;
			array5[count10 + 5] = count9;
		}
		int cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			int[] array6 = indices6.Array;
			array6[count12] = count11;
			array6[count12 + 1] = count11 + 2;
			array6[count12 + 2] = count11 + 1;
			array6[count12 + 3] = count11 + 2;
			array6[count12 + 4] = count11;
			array6[count12 + 5] = count11 + 3;
		}
	}

	internal static int ConvertCorner(int corner, int rot, bool flipX, bool flipY)
	{
		int num = corner & 1;
		int num2 = (corner >> 1) & 1;
		if (flipX)
		{
			num ^= 1;
		}
		if (flipY)
		{
			num2 ^= 1;
		}
		return (((num2 << 1) | num) + rot) & 3;
	}

	public static uint FastHash(int x, int y, int z)
	{
		uint num = 2166136261u;
		num ^= (uint)(x * 73244475);
		num = (num << 13) | (num >> 19);
		num ^= (uint)(y * 668265261);
		num = (num << 11) | (num >> 21);
		num ^= (uint)(z * 374761393);
		num ^= num >> 16;
		num *= 2146121005;
		num ^= num >> 15;
		num *= 2221713035u;
		return num ^ (num >> 16);
	}

	public void GenerateNaturalCubeVertices(Block block, int value, int x, int y, int z, float height11, float height21, float height22, float height12, Color sideColor, Color topColor11, Color topColor21, Color topColor22, Color topColor12, int overrideTopTextureSlot, TerrainGeometrySubset[] subsetsByFace)
	{
		int rot = 0;
		bool flipX = false;
		bool flipY = false;
		if (SettingsManager.RandomNaturalBlocksRotation)
		{
			rot = (int)(FastHash(x, y, z) & 3);
			flipX = false;
			flipY = false;
		}
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int cellValueFast = chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, height22, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, height12, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot, textureSlotCount, sideColor, ref vertices.Array[count + 3]);
			FixSquashedUVs(vertices, count, count + 3, height12);
			FixSquashedUVs(vertices, count + 1, count + 2, height22);
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 2;
			indices.Array[count2 + 2] = count + 1;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count;
			indices.Array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, height21, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, height22, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, sideColor, ref vertices2.Array[count3 + 3]);
			FixSquashedUVs(vertices2, count3, count3 + 1, height21);
			FixSquashedUVs(vertices2, count3 + 3, count3 + 2, height22);
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = count3;
			indices2.Array[count4 + 1] = count3 + 2;
			indices2.Array[count4 + 2] = count3 + 1;
			indices2.Array[count4 + 3] = count3 + 2;
			indices2.Array[count4 + 4] = count3;
			indices2.Array[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, height21, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, height11, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, sideColor, ref vertices3.Array[count5 + 3]);
			FixSquashedUVs(vertices3, count5 + 1, count5 + 2, height21);
			FixSquashedUVs(vertices3, count5, count5 + 3, height11);
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = count5;
			indices3.Array[count6 + 1] = count5 + 1;
			indices3.Array[count6 + 2] = count5 + 2;
			indices3.Array[count6 + 3] = count5 + 2;
			indices3.Array[count6 + 4] = count5 + 3;
			indices3.Array[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, height11, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, height12, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, sideColor, ref vertices4.Array[count7 + 3]);
			FixSquashedUVs(vertices4, count7, count7 + 1, height11);
			FixSquashedUVs(vertices4, count7 + 3, count7 + 2, height12);
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = count7;
			indices4.Array[count8 + 1] = count7 + 1;
			indices4.Array[count8 + 2] = count7 + 2;
			indices4.Array[count8 + 3] = count7 + 2;
			indices4.Array[count8 + 4] = count7 + 3;
			indices4.Array[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value) || height11 < 1f || height12 < 1f || height21 < 1f || height22 < 1f)
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int textureSlot = ((overrideTopTextureSlot >= 0) ? overrideTopTextureSlot : block.GetFaceTextureSlot(4, value));
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, height11, ConvertCorner(3, rot, flipX, flipY), textureSlot, textureSlotCount, topColor11, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, height21, ConvertCorner(2, rot, flipX, flipY), textureSlot, textureSlotCount, topColor21, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, height22, ConvertCorner(1, rot, flipX, flipY), textureSlot, textureSlotCount, topColor22, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, height12, ConvertCorner(0, rot, flipX, flipY), textureSlot, textureSlotCount, topColor12, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = count9;
			indices5.Array[count10 + 1] = count9 + 1;
			indices5.Array[count10 + 2] = count9 + 2;
			indices5.Array[count10 + 3] = count9 + 2;
			indices5.Array[count10 + 4] = count9 + 3;
			indices5.Array[count10 + 5] = count9;
		}
		cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, sideColor, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = count11;
			indices6.Array[count12 + 1] = count11 + 2;
			indices6.Array[count12 + 2] = count11 + 1;
			indices6.Array[count12 + 3] = count11 + 2;
			indices6.Array[count12 + 4] = count11;
			indices6.Array[count12 + 5] = count11 + 3;
		}
	}

	public void GenerateNaturalLeavesVertices(Block block, int value, int x, int y, int z, Color color, TerrainGeometrySubset[] subsetsByFace)
	{
		int rot = 0;
		bool flipX = false;
		bool flipY = false;
		if (SettingsManager.RandomNaturalBlocksRotation)
		{
			rot = (int)(FastHash(x, y, z) & 3);
			flipX = false;
			flipY = false;
		}
		TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
		TerrainChunk chunkAtCell2 = Terrain.GetChunkAtCell(x, z + 1);
		TerrainChunk chunkAtCell3 = Terrain.GetChunkAtCell(x + 1, z);
		TerrainChunk chunkAtCell4 = Terrain.GetChunkAtCell(x, z - 1);
		TerrainChunk chunkAtCell5 = Terrain.GetChunkAtCell(x - 1, z);
		int textureSlotCount = block.GetTextureSlotCount(value);
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell2.GetCellValueFast(x & 0xF, y, (z + 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 0, value: value))
		{
			DynamicArray<TerrainVertex> vertices = subsetsByFace[0].Vertices;
			DynamicArray<int> indices = subsetsByFace[0].Indices;
			int faceTextureSlot = block.GetFaceTextureSlot(0, value);
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot, textureSlotCount, color, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 6;
			int[] array = indices.Array;
			array[count2] = count;
			array[count2 + 1] = count + 2;
			array[count2 + 2] = count + 1;
			array[count2 + 3] = count + 2;
			array[count2 + 4] = count;
			array[count2 + 5] = count + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell3.GetCellValueFast((x + 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 1, value: value))
		{
			DynamicArray<TerrainVertex> vertices2 = subsetsByFace[1].Vertices;
			DynamicArray<int> indices2 = subsetsByFace[1].Indices;
			int faceTextureSlot2 = block.GetFaceTextureSlot(1, value);
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot2, textureSlotCount, color, ref vertices2.Array[count3 + 3]);
			int count4 = indices2.Count;
			indices2.Count += 6;
			int[] array2 = indices2.Array;
			array2[count4] = count3;
			array2[count4 + 1] = count3 + 2;
			array2[count4 + 2] = count3 + 1;
			array2[count4 + 3] = count3 + 2;
			array2[count4 + 4] = count3;
			array2[count4 + 5] = count3 + 3;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell4.GetCellValueFast(x & 0xF, y, (z - 1) & 0xF), subsystemTerrain: SubsystemTerrain, face: 2, value: value))
		{
			DynamicArray<TerrainVertex> vertices3 = subsetsByFace[2].Vertices;
			DynamicArray<int> indices3 = subsetsByFace[2].Indices;
			int faceTextureSlot3 = block.GetFaceTextureSlot(2, value);
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot3, textureSlotCount, color, ref vertices3.Array[count5 + 3]);
			int count6 = indices3.Count;
			indices3.Count += 6;
			int[] array3 = indices3.Array;
			array3[count6] = count5;
			array3[count6 + 1] = count5 + 1;
			array3[count6 + 2] = count5 + 2;
			array3[count6 + 3] = count5 + 2;
			array3[count6 + 4] = count5 + 3;
			array3[count6 + 5] = count5;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell5.GetCellValueFast((x - 1) & 0xF, y, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 3, value: value))
		{
			DynamicArray<TerrainVertex> vertices4 = subsetsByFace[3].Vertices;
			DynamicArray<int> indices4 = subsetsByFace[3].Indices;
			int faceTextureSlot4 = block.GetFaceTextureSlot(3, value);
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot4, textureSlotCount, color, ref vertices4.Array[count7 + 3]);
			int count8 = indices4.Count;
			indices4.Count += 6;
			int[] array4 = indices4.Array;
			array4[count8] = count7;
			array4[count8 + 1] = count7 + 1;
			array4[count8 + 2] = count7 + 2;
			array4[count8 + 3] = count7 + 2;
			array4[count8 + 4] = count7 + 3;
			array4[count8 + 5] = count7;
		}
		if (block.ShouldGenerateFace(neighborValue: chunkAtCell.GetCellValueFast(x & 0xF, y + 1, z & 0xF), subsystemTerrain: SubsystemTerrain, face: 4, value: value))
		{
			DynamicArray<TerrainVertex> vertices5 = subsetsByFace[4].Vertices;
			DynamicArray<int> indices5 = subsetsByFace[4].Indices;
			int faceTextureSlot5 = block.GetFaceTextureSlot(4, value);
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot5, textureSlotCount, color, ref vertices5.Array[count9 + 3]);
			int count10 = indices5.Count;
			indices5.Count += 6;
			int[] array5 = indices5.Array;
			array5[count10] = count9;
			array5[count10 + 1] = count9 + 1;
			array5[count10 + 2] = count9 + 2;
			array5[count10 + 3] = count9 + 2;
			array5[count10 + 4] = count9 + 3;
			array5[count10 + 5] = count9;
		}
		int cellValueFast = chunkAtCell.GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		if (block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast))
		{
			DynamicArray<TerrainVertex> vertices6 = subsetsByFace[5].Vertices;
			DynamicArray<int> indices6 = subsetsByFace[5].Indices;
			int faceTextureSlot6 = block.GetFaceTextureSlot(5, value);
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, ConvertCorner(0, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, ConvertCorner(1, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, ConvertCorner(2, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, ConvertCorner(3, rot, flipX, flipY), faceTextureSlot6, textureSlotCount, color, ref vertices6.Array[count11 + 3]);
			int count12 = indices6.Count;
			indices6.Count += 6;
			int[] array6 = indices6.Array;
			array6[count12] = count11;
			array6[count12 + 1] = count11 + 2;
			array6[count12 + 2] = count11 + 1;
			array6[count12 + 3] = count11 + 2;
			array6[count12 + 4] = count11;
			array6[count12 + 5] = count11 + 3;
		}
	}

	public void GenerateFlatVertices(Block block, int value, int x, int y, int z, Color color, int mountingFace, float thickness, TerrainGeometrySubset[] subsets)
	{
		int num = mountingFace;
		int num2 = 0;
		if (block is IvyBlock)
		{
			switch (mountingFace)
			{
			case 4:
				num = 5;
				break;
			case 5:
				num = 4;
				break;
			}
		}
		else if (block is CarpetBlock)
		{
			num2 = 4;
			if (mountingFace < 4)
			{
				num = (mountingFace + 2) % 4;
			}
		}
		float num3 = 0f;
		float num4 = 1f;
		float num5 = 0f;
		float num6 = 1f;
		float num7 = 0f;
		float num8 = 1f;
		switch (mountingFace)
		{
		case 0:
			num3 = 0f;
			num4 = 1f;
			num5 = 0f;
			num6 = 1f;
			num7 = 1f - thickness;
			num8 = 1f;
			break;
		case 1:
			num3 = 1f - thickness;
			num4 = 1f;
			num5 = 0f;
			num6 = 1f;
			num7 = 0f;
			num8 = 1f;
			break;
		case 2:
			num3 = 0f;
			num4 = 1f;
			num5 = 0f;
			num6 = 1f;
			num7 = 0f;
			num8 = thickness;
			break;
		case 3:
			num3 = 0f;
			num4 = thickness;
			num5 = 0f;
			num6 = 1f;
			num7 = 0f;
			num8 = 1f;
			break;
		case 4:
			num3 = 0f;
			num4 = 1f;
			num5 = 0f;
			num6 = thickness;
			num7 = 0f;
			num8 = 1f;
			break;
		case 5:
			num3 = 0f;
			num4 = 1f;
			num5 = 1f - thickness;
			num6 = 1f;
			num7 = 0f;
			num8 = 1f;
			break;
		}
		int textureSlotCount = block.GetTextureSlotCount(value);
		int cellValueFast = Terrain.GetChunkAtCell(x, z + 1).GetCellValueFast(x & 0xF, y, (z + 1) & 0xF);
		bool flag = false;
		if (block is SlabBlock)
		{
			flag = !block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast);
		}
		else if (mountingFace != 0 && mountingFace != 2 && Terrain.ExtractContents(cellValueFast) == block.BlockIndex && ((Terrain.ExtractData(cellValueFast) >> num2) & 0x3F & (1 << num)) != 0)
		{
			flag = true;
		}
		if (!flag && (mountingFace != 0 || thickness < 1f || block.ShouldGenerateFace(SubsystemTerrain, 0, value, cellValueFast)))
		{
			DynamicArray<TerrainVertex> vertices = subsets[0].Vertices;
			DynamicArray<int> indices = subsets[0].Indices;
			int count = vertices.Count;
			vertices.Count += 4;
			SetupCubeVertexFace0(x, y, z + 1, 1f, 0, block.GetFaceTextureSlot(0, value), textureSlotCount, color, ref vertices.Array[count]);
			SetupCubeVertexFace0(x + 1, y, z + 1, 1f, 1, block.GetFaceTextureSlot(0, value), textureSlotCount, color, ref vertices.Array[count + 1]);
			SetupCubeVertexFace0(x + 1, y + 1, z + 1, 1f, 2, block.GetFaceTextureSlot(0, value), textureSlotCount, color, ref vertices.Array[count + 2]);
			SetupCubeVertexFace0(x, y + 1, z + 1, 1f, 3, block.GetFaceTextureSlot(0, value), textureSlotCount, color, ref vertices.Array[count + 3]);
			TerrainVertex[] array = vertices.Array;
			array[count].X = (float)x + num3;
			array[count].Y = (float)y + num5;
			array[count].Z = (float)z + num8;
			array[count + 1].X = (float)x + num4;
			array[count + 1].Y = (float)y + num5;
			array[count + 1].Z = (float)z + num8;
			array[count + 2].X = (float)x + num4;
			array[count + 2].Y = (float)y + num6;
			array[count + 2].Z = (float)z + num8;
			array[count + 3].X = (float)x + num3;
			array[count + 3].Y = (float)y + num6;
			array[count + 3].Z = (float)z + num8;
			switch (mountingFace)
			{
			case 4:
			case 5:
				FixSquashedUVs(vertices, count, count + 3, thickness);
				FixSquashedUVs(vertices, count + 1, count + 2, thickness);
				break;
			default:
			{
				float num9 = array[count].Tx;
				float num10 = array[count + 1].Tx;
				float num11 = num9 + (num10 - num9) * thickness;
				array[count + 1].Tx = (short)num11;
				array[count + 2].Tx = (short)num11;
				break;
			}
			case 0:
			case 2:
				break;
			}
			int count2 = indices.Count;
			indices.Count += 6;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 2;
			indices.Array[count2 + 2] = count + 1;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count;
			indices.Array[count2 + 5] = count + 3;
		}
		int cellValueFast2 = Terrain.GetChunkAtCell(x + 1, z).GetCellValueFast((x + 1) & 0xF, y, z & 0xF);
		bool flag2 = false;
		if (block is SlabBlock)
		{
			flag2 = !block.ShouldGenerateFace(SubsystemTerrain, 1, value, cellValueFast2);
		}
		else if (mountingFace != 1 && mountingFace != 3 && Terrain.ExtractContents(cellValueFast2) == block.BlockIndex && ((Terrain.ExtractData(cellValueFast2) >> num2) & 0x3F & (1 << num)) != 0)
		{
			flag2 = true;
		}
		if (!flag2 && (mountingFace != 1 || thickness < 1f || block.ShouldGenerateFace(SubsystemTerrain, 1, value, cellValueFast2)))
		{
			DynamicArray<TerrainVertex> vertices2 = subsets[1].Vertices;
			DynamicArray<int> indices2 = subsets[1].Indices;
			int count3 = vertices2.Count;
			vertices2.Count += 4;
			SetupCubeVertexFace1(x + 1, y, z, 1f, 1, block.GetFaceTextureSlot(1, value), textureSlotCount, color, ref vertices2.Array[count3]);
			SetupCubeVertexFace1(x + 1, y + 1, z, 1f, 2, block.GetFaceTextureSlot(1, value), textureSlotCount, color, ref vertices2.Array[count3 + 1]);
			SetupCubeVertexFace1(x + 1, y + 1, z + 1, 1f, 3, block.GetFaceTextureSlot(1, value), textureSlotCount, color, ref vertices2.Array[count3 + 2]);
			SetupCubeVertexFace1(x + 1, y, z + 1, 1f, 0, block.GetFaceTextureSlot(1, value), textureSlotCount, color, ref vertices2.Array[count3 + 3]);
			TerrainVertex[] array2 = vertices2.Array;
			array2[count3].X = (float)x + num4;
			array2[count3].Y = (float)y + num5;
			array2[count3].Z = (float)z + num7;
			array2[count3 + 1].X = (float)x + num4;
			array2[count3 + 1].Y = (float)y + num6;
			array2[count3 + 1].Z = (float)z + num7;
			array2[count3 + 2].X = (float)x + num4;
			array2[count3 + 2].Y = (float)y + num6;
			array2[count3 + 2].Z = (float)z + num8;
			array2[count3 + 3].X = (float)x + num4;
			array2[count3 + 3].Y = (float)y + num5;
			array2[count3 + 3].Z = (float)z + num8;
			switch (mountingFace)
			{
			case 4:
			case 5:
				FixSquashedUVs(vertices2, count3, count3 + 1, thickness);
				FixSquashedUVs(vertices2, count3 + 3, count3 + 2, thickness);
				break;
			default:
			{
				float num12 = array2[count3].Tx;
				float num13 = array2[count3 + 3].Tx;
				float num14 = num12 + (num13 - num12) * thickness;
				array2[count3 + 3].Tx = (short)num14;
				array2[count3 + 2].Tx = (short)num14;
				break;
			}
			case 1:
			case 3:
				break;
			}
			int count4 = indices2.Count;
			indices2.Count += 6;
			indices2.Array[count4] = count3;
			indices2.Array[count4 + 1] = count3 + 2;
			indices2.Array[count4 + 2] = count3 + 1;
			indices2.Array[count4 + 3] = count3 + 2;
			indices2.Array[count4 + 4] = count3;
			indices2.Array[count4 + 5] = count3 + 3;
		}
		int cellValueFast3 = Terrain.GetChunkAtCell(x, z - 1).GetCellValueFast(x & 0xF, y, (z - 1) & 0xF);
		bool flag3 = false;
		if (block is SlabBlock)
		{
			flag3 = !block.ShouldGenerateFace(SubsystemTerrain, 2, value, cellValueFast3);
		}
		else if (mountingFace != 2 && mountingFace != 0 && Terrain.ExtractContents(cellValueFast3) == block.BlockIndex && ((Terrain.ExtractData(cellValueFast3) >> num2) & 0x3F & (1 << num)) != 0)
		{
			flag3 = true;
		}
		if (!flag3 && (mountingFace != 2 || thickness < 1f || block.ShouldGenerateFace(SubsystemTerrain, 2, value, cellValueFast3)))
		{
			DynamicArray<TerrainVertex> vertices3 = subsets[2].Vertices;
			DynamicArray<int> indices3 = subsets[2].Indices;
			int count5 = vertices3.Count;
			vertices3.Count += 4;
			SetupCubeVertexFace2(x, y, z, 1f, 1, block.GetFaceTextureSlot(2, value), textureSlotCount, color, ref vertices3.Array[count5]);
			SetupCubeVertexFace2(x + 1, y, z, 1f, 0, block.GetFaceTextureSlot(2, value), textureSlotCount, color, ref vertices3.Array[count5 + 1]);
			SetupCubeVertexFace2(x + 1, y + 1, z, 1f, 3, block.GetFaceTextureSlot(2, value), textureSlotCount, color, ref vertices3.Array[count5 + 2]);
			SetupCubeVertexFace2(x, y + 1, z, 1f, 2, block.GetFaceTextureSlot(2, value), textureSlotCount, color, ref vertices3.Array[count5 + 3]);
			TerrainVertex[] array3 = vertices3.Array;
			array3[count5].X = (float)x + num3;
			array3[count5].Y = (float)y + num5;
			array3[count5].Z = (float)z + num7;
			array3[count5 + 1].X = (float)x + num4;
			array3[count5 + 1].Y = (float)y + num5;
			array3[count5 + 1].Z = (float)z + num7;
			array3[count5 + 2].X = (float)x + num4;
			array3[count5 + 2].Y = (float)y + num6;
			array3[count5 + 2].Z = (float)z + num7;
			array3[count5 + 3].X = (float)x + num3;
			array3[count5 + 3].Y = (float)y + num6;
			array3[count5 + 3].Z = (float)z + num7;
			switch (mountingFace)
			{
			case 4:
			case 5:
				FixSquashedUVs(vertices3, count5 + 1, count5 + 2, thickness);
				FixSquashedUVs(vertices3, count5, count5 + 3, thickness);
				break;
			default:
			{
				float num15 = array3[count5 + 1].Tx;
				float num16 = array3[count5].Tx;
				float num17 = num15 + (num16 - num15) * thickness;
				array3[count5].Tx = (short)num17;
				array3[count5 + 3].Tx = (short)num17;
				break;
			}
			case 0:
			case 2:
				break;
			}
			int count6 = indices3.Count;
			indices3.Count += 6;
			indices3.Array[count6] = count5;
			indices3.Array[count6 + 1] = count5 + 1;
			indices3.Array[count6 + 2] = count5 + 2;
			indices3.Array[count6 + 3] = count5 + 2;
			indices3.Array[count6 + 4] = count5 + 3;
			indices3.Array[count6 + 5] = count5;
		}
		int cellValueFast4 = Terrain.GetChunkAtCell(x - 1, z).GetCellValueFast((x - 1) & 0xF, y, z & 0xF);
		bool flag4 = false;
		if (block is SlabBlock)
		{
			flag4 = !block.ShouldGenerateFace(SubsystemTerrain, 3, value, cellValueFast4);
		}
		else if (mountingFace != 3 && mountingFace != 1 && Terrain.ExtractContents(cellValueFast4) == block.BlockIndex && ((Terrain.ExtractData(cellValueFast4) >> num2) & 0x3F & (1 << num)) != 0)
		{
			flag4 = true;
		}
		if (!flag4 && (mountingFace != 3 || thickness < 1f || block.ShouldGenerateFace(SubsystemTerrain, 3, value, cellValueFast4)))
		{
			DynamicArray<TerrainVertex> vertices4 = subsets[3].Vertices;
			DynamicArray<int> indices4 = subsets[3].Indices;
			int count7 = vertices4.Count;
			vertices4.Count += 4;
			SetupCubeVertexFace3(x, y, z, 1f, 0, block.GetFaceTextureSlot(3, value), textureSlotCount, color, ref vertices4.Array[count7]);
			SetupCubeVertexFace3(x, y + 1, z, 1f, 3, block.GetFaceTextureSlot(3, value), textureSlotCount, color, ref vertices4.Array[count7 + 1]);
			SetupCubeVertexFace3(x, y + 1, z + 1, 1f, 2, block.GetFaceTextureSlot(3, value), textureSlotCount, color, ref vertices4.Array[count7 + 2]);
			SetupCubeVertexFace3(x, y, z + 1, 1f, 1, block.GetFaceTextureSlot(3, value), textureSlotCount, color, ref vertices4.Array[count7 + 3]);
			TerrainVertex[] array4 = vertices4.Array;
			array4[count7].X = (float)x + num3;
			array4[count7].Y = (float)y + num5;
			array4[count7].Z = (float)z + num7;
			array4[count7 + 1].X = (float)x + num3;
			array4[count7 + 1].Y = (float)y + num6;
			array4[count7 + 1].Z = (float)z + num7;
			array4[count7 + 2].X = (float)x + num3;
			array4[count7 + 2].Y = (float)y + num6;
			array4[count7 + 2].Z = (float)z + num8;
			array4[count7 + 3].X = (float)x + num3;
			array4[count7 + 3].Y = (float)y + num5;
			array4[count7 + 3].Z = (float)z + num8;
			switch (mountingFace)
			{
			case 4:
			case 5:
				FixSquashedUVs(vertices4, count7, count7 + 1, thickness);
				FixSquashedUVs(vertices4, count7 + 3, count7 + 2, thickness);
				break;
			default:
			{
				float num18 = array4[count7 + 3].Tx;
				float num19 = array4[count7].Tx;
				float num20 = num18 + (num19 - num18) * thickness;
				array4[count7].Tx = (short)num20;
				array4[count7 + 1].Tx = (short)num20;
				break;
			}
			case 1:
			case 3:
				break;
			}
			int count8 = indices4.Count;
			indices4.Count += 6;
			indices4.Array[count8] = count7;
			indices4.Array[count8 + 1] = count7 + 1;
			indices4.Array[count8 + 2] = count7 + 2;
			indices4.Array[count8 + 3] = count7 + 2;
			indices4.Array[count8 + 4] = count7 + 3;
			indices4.Array[count8 + 5] = count7;
		}
		int cellValueFast5 = Terrain.GetChunkAtCell(x, z).GetCellValueFast(x & 0xF, y + 1, z & 0xF);
		bool flag5 = false;
		if (block is SlabBlock)
		{
			flag5 = !block.ShouldGenerateFace(SubsystemTerrain, 4, value, cellValueFast5);
		}
		else if (mountingFace != 4 && mountingFace != 5 && Terrain.ExtractContents(cellValueFast5) == block.BlockIndex && ((Terrain.ExtractData(cellValueFast5) >> num2) & 0x3F & (1 << num)) != 0)
		{
			flag5 = true;
		}
		if (!flag5 && (mountingFace != 4 || thickness < 1f || block.ShouldGenerateFace(SubsystemTerrain, 4, value, cellValueFast5)))
		{
			DynamicArray<TerrainVertex> vertices5 = subsets[4].Vertices;
			DynamicArray<int> indices5 = subsets[4].Indices;
			int count9 = vertices5.Count;
			vertices5.Count += 4;
			SetupCubeVertexFace4(x, y + 1, z, 1f, 3, block.GetFaceTextureSlot(4, value), textureSlotCount, color, ref vertices5.Array[count9]);
			SetupCubeVertexFace4(x + 1, y + 1, z, 1f, 2, block.GetFaceTextureSlot(4, value), textureSlotCount, color, ref vertices5.Array[count9 + 1]);
			SetupCubeVertexFace4(x + 1, y + 1, z + 1, 1f, 1, block.GetFaceTextureSlot(4, value), textureSlotCount, color, ref vertices5.Array[count9 + 2]);
			SetupCubeVertexFace4(x, y + 1, z + 1, 1f, 0, block.GetFaceTextureSlot(4, value), textureSlotCount, color, ref vertices5.Array[count9 + 3]);
			TerrainVertex[] array5 = vertices5.Array;
			array5[count9].X = (float)x + num3;
			array5[count9].Y = (float)y + num6;
			array5[count9].Z = (float)z + num7;
			array5[count9 + 1].X = (float)x + num4;
			array5[count9 + 1].Y = (float)y + num6;
			array5[count9 + 1].Z = (float)z + num7;
			array5[count9 + 2].X = (float)x + num4;
			array5[count9 + 2].Y = (float)y + num6;
			array5[count9 + 2].Z = (float)z + num8;
			array5[count9 + 3].X = (float)x + num3;
			array5[count9 + 3].Y = (float)y + num6;
			array5[count9 + 3].Z = (float)z + num8;
			switch (mountingFace)
			{
			case 0:
			case 2:
				FixSquashedUVs(vertices5, count9, count9 + 3, thickness);
				FixSquashedUVs(vertices5, count9 + 1, count9 + 2, thickness);
				break;
			default:
			{
				float num21 = array5[count9].Tx;
				float num22 = array5[count9 + 1].Tx;
				float num23 = num21 + (num22 - num21) * thickness;
				array5[count9 + 1].Tx = (short)num23;
				array5[count9 + 2].Tx = (short)num23;
				break;
			}
			case 4:
			case 5:
				break;
			}
			int count10 = indices5.Count;
			indices5.Count += 6;
			indices5.Array[count10] = count9;
			indices5.Array[count10 + 1] = count9 + 1;
			indices5.Array[count10 + 2] = count9 + 2;
			indices5.Array[count10 + 3] = count9 + 2;
			indices5.Array[count10 + 4] = count9 + 3;
			indices5.Array[count10 + 5] = count9;
		}
		int cellValueFast6 = Terrain.GetChunkAtCell(x, z).GetCellValueFast(x & 0xF, y - 1, z & 0xF);
		bool flag6 = false;
		if (block is SlabBlock)
		{
			flag6 = !block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast6);
		}
		else if (mountingFace != 5 && mountingFace != 4 && Terrain.ExtractContents(cellValueFast6) == block.BlockIndex && ((Terrain.ExtractData(cellValueFast6) >> num2) & 0x3F & (1 << num)) != 0)
		{
			flag6 = true;
		}
		if (!flag6 && (mountingFace != 5 || thickness < 1f || block.ShouldGenerateFace(SubsystemTerrain, 5, value, cellValueFast6)))
		{
			DynamicArray<TerrainVertex> vertices6 = subsets[5].Vertices;
			DynamicArray<int> indices6 = subsets[5].Indices;
			int count11 = vertices6.Count;
			vertices6.Count += 4;
			SetupCubeVertexFace5(x, y, z, 1f, 0, block.GetFaceTextureSlot(5, value), textureSlotCount, color, ref vertices6.Array[count11]);
			SetupCubeVertexFace5(x + 1, y, z, 1f, 1, block.GetFaceTextureSlot(5, value), textureSlotCount, color, ref vertices6.Array[count11 + 1]);
			SetupCubeVertexFace5(x + 1, y, z + 1, 1f, 2, block.GetFaceTextureSlot(5, value), textureSlotCount, color, ref vertices6.Array[count11 + 2]);
			SetupCubeVertexFace5(x, y, z + 1, 1f, 3, block.GetFaceTextureSlot(5, value), textureSlotCount, color, ref vertices6.Array[count11 + 3]);
			TerrainVertex[] array6 = vertices6.Array;
			array6[count11].X = (float)x + num3;
			array6[count11].Y = (float)y + num5;
			array6[count11].Z = (float)z + num7;
			array6[count11 + 1].X = (float)x + num4;
			array6[count11 + 1].Y = (float)y + num5;
			array6[count11 + 1].Z = (float)z + num7;
			array6[count11 + 2].X = (float)x + num4;
			array6[count11 + 2].Y = (float)y + num5;
			array6[count11 + 2].Z = (float)z + num8;
			array6[count11 + 3].X = (float)x + num3;
			array6[count11 + 3].Y = (float)y + num5;
			array6[count11 + 3].Z = (float)z + num8;
			switch (mountingFace)
			{
			case 0:
			case 2:
				FixSquashedUVs(vertices6, count11, count11 + 3, thickness);
				FixSquashedUVs(vertices6, count11 + 1, count11 + 2, thickness);
				break;
			default:
			{
				float num24 = array6[count11].Tx;
				float num25 = array6[count11 + 1].Tx;
				float num26 = num24 + (num25 - num24) * thickness;
				array6[count11 + 1].Tx = (short)num26;
				array6[count11 + 2].Tx = (short)num26;
				break;
			}
			case 4:
			case 5:
				break;
			}
			int count12 = indices6.Count;
			indices6.Count += 6;
			indices6.Array[count12] = count11;
			indices6.Array[count12 + 1] = count11 + 2;
			indices6.Array[count12 + 2] = count11 + 1;
			indices6.Array[count12 + 3] = count11 + 2;
			indices6.Array[count12 + 4] = count11;
			indices6.Array[count12 + 5] = count11 + 3;
		}
	}

	private static void FixSquashedUVs(DynamicArray<TerrainVertex> vertices, int bottomIndex, int topIndex, float height)
	{
		if (!(height >= 0.999f) && !(height <= 0.001f))
		{
			short tx = vertices.Array[bottomIndex].Tx;
			short ty = vertices.Array[bottomIndex].Ty;
			short tx2 = vertices.Array[topIndex].Tx;
			short ty2 = vertices.Array[topIndex].Ty;
			float num = (float)tx2 + ((float)tx - (float)tx2) * height;
			float num2 = (float)ty2 + ((float)ty - (float)ty2) * height;
			vertices.Array[bottomIndex].Tx = (short)num;
			vertices.Array[bottomIndex].Ty = (short)num2;
		}
	}
}
