using Engine;
using Engine.Graphics;

namespace Game;

public class TargetBlock : MountedElectricElementBlock
{
	public const int Index = 199;

	public BoundingBox[][] m_boundingBoxes = new BoundingBox[4][]
	{
		new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 0.0625f))
		},
		new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(0.0625f, 1f, 1f))
		},
		new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0.9375f), new Vector3(1f, 1f, 1f))
		},
		new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0.9375f, 0f, 0f), new Vector3(1f, 1f, 1f))
		}
	};

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int timeOfDayMultiplier = GetMountingFace(Terrain.ExtractData(value));
		if (timeOfDayMultiplier >= 0 && timeOfDayMultiplier < 4)
		{
			return m_boundingBoxes[timeOfDayMultiplier];
		}
		return base.GetCustomCollisionBoxes(terrain, value);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		BlockPlacementData timeOfDay = default;
		if (raycastResult.CellFace.Face < 4)
		{
			timeOfDay.CellFace = raycastResult.CellFace;
			timeOfDay.Value = Terrain.MakeBlockValue(199, 0, SetMountingFace(0, raycastResult.CellFace.Face));
		}
		return timeOfDay;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		TerrainGeometrySubset subsetAlphaTest = geometry.SubsetAlphaTest;
		DynamicArray<TerrainVertex> x2 = subsetAlphaTest.Vertices;
		DynamicArray<int> z2 = subsetAlphaTest.Indices;
		int num = x2.Count;
		int data = Terrain.ExtractData(value);
		DrawWaterInside(generator, geometry, x, y, z, data);
		int num2 = Terrain.ExtractLight(value);
		int mountingFace = GetMountingFace(data);
		float num3 = LightingManager.LightIntensityByLightValueAndFace[num2 + 16 * mountingFace];
		Color color = Color.White * num3;
		switch (mountingFace)
		{
		case 0:
			x2.Count += 4;
			BlockGeometryGenerator.SetupLitCornerVertex(x, y, z, color, DefaultTextureSlot, 0, ref x2.Array[num]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y, z, color, DefaultTextureSlot, 1, ref x2.Array[num + 1]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y + 1, z, color, DefaultTextureSlot, 2, ref x2.Array[num + 2]);
			BlockGeometryGenerator.SetupLitCornerVertex(x, y + 1, z, color, DefaultTextureSlot, 3, ref x2.Array[num + 3]);
			z2.Add(num);
			z2.Add(num + 2);
			z2.Add(num + 1);
			z2.Add(num + 1);
			z2.Add(num + 2);
			z2.Add(num);
			z2.Add(num + 2);
			z2.Add(num);
			z2.Add(num + 3);
			z2.Add(num + 3);
			z2.Add(num);
			z2.Add(num + 2);
			break;
		case 1:
			x2.Count += 4;
			BlockGeometryGenerator.SetupLitCornerVertex(x, y, z, color, DefaultTextureSlot, 0, ref x2.Array[num]);
			BlockGeometryGenerator.SetupLitCornerVertex(x, y + 1, z, color, DefaultTextureSlot, 3, ref x2.Array[num + 1]);
			BlockGeometryGenerator.SetupLitCornerVertex(x, y + 1, z + 1, color, DefaultTextureSlot, 2, ref x2.Array[num + 2]);
			BlockGeometryGenerator.SetupLitCornerVertex(x, y, z + 1, color, DefaultTextureSlot, 1, ref x2.Array[num + 3]);
			z2.Add(num);
			z2.Add(num + 2);
			z2.Add(num + 1);
			z2.Add(num + 1);
			z2.Add(num + 2);
			z2.Add(num);
			z2.Add(num + 2);
			z2.Add(num);
			z2.Add(num + 3);
			z2.Add(num + 3);
			z2.Add(num);
			z2.Add(num + 2);
			break;
		case 2:
			x2.Count += 4;
			BlockGeometryGenerator.SetupLitCornerVertex(x, y, z + 1, color, DefaultTextureSlot, 0, ref x2.Array[num]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y, z + 1, color, DefaultTextureSlot, 1, ref x2.Array[num + 1]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y + 1, z + 1, color, DefaultTextureSlot, 2, ref x2.Array[num + 2]);
			BlockGeometryGenerator.SetupLitCornerVertex(x, y + 1, z + 1, color, DefaultTextureSlot, 3, ref x2.Array[num + 3]);
			z2.Add(num);
			z2.Add(num + 1);
			z2.Add(num + 2);
			z2.Add(num + 2);
			z2.Add(num + 1);
			z2.Add(num);
			z2.Add(num + 2);
			z2.Add(num + 3);
			z2.Add(num);
			z2.Add(num);
			z2.Add(num + 3);
			z2.Add(num + 2);
			break;
		case 3:
			x2.Count += 4;
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y, z, color, DefaultTextureSlot, 0, ref x2.Array[num]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y + 1, z, color, DefaultTextureSlot, 3, ref x2.Array[num + 1]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y + 1, z + 1, color, DefaultTextureSlot, 2, ref x2.Array[num + 2]);
			BlockGeometryGenerator.SetupLitCornerVertex(x + 1, y, z + 1, color, DefaultTextureSlot, 1, ref x2.Array[num + 3]);
			z2.Add(num);
			z2.Add(num + 1);
			z2.Add(num + 2);
			z2.Add(num + 2);
			z2.Add(num + 1);
			z2.Add(num);
			z2.Add(num + 2);
			z2.Add(num + 3);
			z2.Add(num);
			z2.Add(num);
			z2.Add(num + 3);
			z2.Add(num + 2);
			break;
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		BlocksManager.DrawFlatOrImageExtrusionBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: false, environmentData);
	}

	public static int GetMountingFace(int data)
	{
		return data & 3;
	}

	public static int SetMountingFace(int data, int face)
	{
		return (data & -4) | (face & 3);
	}

	public override int GetFace(int value)
	{
		return GetMountingFace(Terrain.ExtractData(value));
	}

	public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
	{
		return new TargetElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));
	}

	public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
	{
		int face2 = GetFace(value);
		if (face == face2 && SubsystemElectricity.GetConnectorDirection(face2, 0, connectorFace).HasValue)
		{
			return ElectricConnectorType.Output;
		}
		return null;
	}
}
