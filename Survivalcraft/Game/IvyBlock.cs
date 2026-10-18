using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class IvyBlock : Block
{
	public const int Index = 197;

	private const int NoSeasonBit = 4;

	public BoundingBox[] m_collisionBoxesByFace = new BoundingBox[6];

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		int faceMask = GetFaceMask(Terrain.ExtractData(value));
		List<BoundingBox> list = new List<BoundingBox>();
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				list.Add(m_collisionBoxesByFace[i]);
			}
		}
		return list.ToArray();
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
	{
		int num = CellFace.OppositeFace(raycastResult.CellFace.Face);
		int num2 = 1 << num;
		Point3 point = CellFace.FaceToPoint3(raycastResult.CellFace.Face);
		int x = raycastResult.CellFace.X + point.X;
		int y = raycastResult.CellFace.Y + point.Y;
		int z = raycastResult.CellFace.Z + point.Z;
		int cellValue = subsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) == BlockIndex)
		{
			num2 |= GetFaceMask(Terrain.ExtractData(cellValue));
		}
		int data = SetFaceMask(0, num2) | 0x40;
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		if (IsInvisible(generator.SubsystemTerrain, value))
		{
			return;
		}
		int data = Terrain.ExtractData(value);
		int filename = GetFaceMask(data);
		SubsystemWeather.Season currentSeason = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = (GetNoSeason(data) ? BlockColorsMap.IvyColorsMap.Lookup(generator.Terrain, x, y, z) : BlockColorsMap.IvyColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, x, y, z));
		for (int i = 0; i < 6; i++)
		{
			if ((filename & (1 << i)) == 0)
			{
				continue;
			}
			int num = x;
			int num2 = z;
			int y2;
			switch (i)
			{
			case 4:
				y2 = y + 1;
				break;
			case 5:
				y2 = y - 1;
				break;
			default:
			{
				Point3 point = CellFace.FaceToPoint3(i);
				num += point.X;
				y2 = y + point.Y;
				num2 += point.Z;
				break;
			}
			}
			int mountingFace = i;
			switch (i)
			{
			case 4:
				mountingFace = 5;
				break;
			case 5:
				mountingFace = 4;
				break;
			}
			int num3 = Terrain.ExtractContents(generator.Terrain.GetCellValueFast(num, y2, num2));
			Color color2 = color;
			float num4 = 1f;
			switch (num3)
			{
			case 62:
			case 72:
			case 350:
				color2 = Color.Lerp(color, new Color(150, 200, 255), 0.8f);
				break;
			case 4:
			case 6:
				color2 = Color.Lerp(color, new Color(230, 220, 140), 0.8f);
				break;
			case 67:
			case 376:
			case 378:
				color2 = new Color(30, 30, 30);
				break;
			}
			if (i == 5)
			{
				num4 = 0.85f;
				color2 *= num4;
			}
			generator.GenerateFlatVertices(this, value, x, y, z, color2, mountingFace, 0.0625f, geometry.AlphaTestSubsetsByFace);
			if (num3 != 8 && num3 != 2)
			{
				continue;
			}
			int num5 = -1;
			uint num6 = BlockGeometryGenerator.FastHash(num, y2, num2);
			switch (num3)
			{
			case 2:
				if (num6 % 4 == 0)
				{
					num5 = 28;
				}
				break;
			case 8:
			{
				if (num6 % 15 == 0)
				{
					num5 = 28;
					break;
				}
				int num7 = (int)(num6 % 10);
				if (num7 >= 6)
				{
					if (num7 >= 8)
					{
						num5 = ((num7 != 8) ? 13 : 12);
					}
					else
					{
						num5 = 85;
					}
				}
				else
				{
					num5 = 84;
				}
				break;
			}
			}
			if (num5 != -1)
			{
				Color color3;
				if (num5 != 12 && num5 != 13 && num5 != 28)
				{
					color3 = ((num3 != 378 && num3 != 376) ? BlockColorsMap.GrassColorsMap.LookupSeasonalGrassColor(currentSeason, generator.Terrain, num, y2, num2) : new Color(30, 30, 30));
				}
				else
				{
					color3 = new Color(255, 255, 255);
				}
				color3 *= num4;
				GenerateRotatedCrossface(generator, value, x, y, z, color3, num5, mountingFace, num6, geometry.SubsetAlphaTest);
			}
		}
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		color *= BlockColorsMap.IvyColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
		BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: true, environmentData);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		if (IsInvisible(subsystemTerrain, value))
		{
			strength = 0f;
		}
		Color color = BlockColorsMap.IvyColorsMap.Lookup(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, DefaultTextureSlot);
	}

	public static int GetFace(int data)
	{
		int faceMask = GetFaceMask(data);
		for (int i = 0; i < 6; i++)
		{
			if ((faceMask & (1 << i)) != 0)
			{
				return i;
			}
		}
		return 0;
	}

	public static int SetFace(int data, int face)
	{
		int mask = 1 << (face & 7);
		return SetFaceMask(data, mask);
	}

	public static bool IsGrowthStopCell(int x, int y, int z)
	{
		return MathUtils.Hash((uint)(x + y * 451 + z * 77437)) % 5 == 0;
	}

	public override void Initialize()
	{
		float image = 0.0625f;
		m_collisionBoxesByFace[0] = new BoundingBox(new Vector3(0f, 0f, 1f - image), new Vector3(1f, 1f, 1f));
		m_collisionBoxesByFace[1] = new BoundingBox(new Vector3(1f - image, 0f, 0f), new Vector3(1f, 1f, 1f));
		m_collisionBoxesByFace[2] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, image));
		m_collisionBoxesByFace[3] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(image, 1f, 1f));
		m_collisionBoxesByFace[4] = new BoundingBox(new Vector3(0f, 1f - image, 0f), new Vector3(1f, 1f, 1f));
		m_collisionBoxesByFace[5] = new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, image, 1f));
		base.Initialize();
	}

	public static int GetFaceMask(int data)
	{
		return data & 0x3F;
	}

	public static int SetFaceMask(int data, int mask)
	{
		return (data & -64) | (mask & 0x3F);
	}

	public static bool GetNoSeason(int data)
	{
		return (data & 0x40) != 0;
	}

	private void GenerateRotatedCrossface(BlockGeometryGenerator generator, int value, int x, int y, int z, Color color, int textureSlot, int mountingFace, uint hash, TerrainGeometrySubset subset)
	{
		int value2 = Terrain.ExtractLight(value);
		float value3 = LightingManager.LightIntensityByLightValueAndFace[value2 + 64];
		color = new Color((byte)((float)(int)color.R * value3), (byte)((float)(int)color.G * value3), (byte)((float)(int)color.B * value3), color.A);
		DynamicArray<TerrainVertex> vertices = subset.Vertices;
		DynamicArray<int> indices = subset.Indices;
		int textureSlotCount = GetTextureSlotCount(value);
		Vector3 vector = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
		Vector3 vector2 = CellFace.FaceToVector3(mountingFace);
		Vector3 vector3 = vector + vector2 * 0.5f;
		Matrix matrix;
		if (mountingFace == 4)
		{
			matrix = Matrix.Identity;
			vector3 = new Vector3((float)x + 0.5f, (float)y + 0.0625f, (float)z + 0.5f);
		}
		else if (mountingFace == 5)
		{
			matrix = Matrix.CreateRotationX((float)Math.PI);
			vector3 = new Vector3((float)x + 0.5f, (float)y + 1f - 0.0625f, (float)z + 0.5f);
		}
		else
		{
			matrix = mountingFace switch
			{
				0 => Matrix.CreateRotationX((float)Math.PI / 2f), 
				2 => Matrix.CreateRotationX(-(float)Math.PI / 2f), 
				1 => Matrix.CreateRotationZ(-(float)Math.PI / 2f), 
				_ => Matrix.CreateRotationZ((float)Math.PI / 2f), 
			};
			matrix = Matrix.CreateRotationX((float)Math.PI) * matrix;
		}
		matrix = Matrix.CreateRotationY((float)(hash % 100) / 100f * ((float)Math.PI * 2f)) * matrix;
		for (int i = 0; i < 2; i++)
		{
			float num = 1.45f;
			float y2 = 1f;
			float x2 = ((i == 0) ? 0f : ((float)Math.PI / 2f));
			float num2 = MathUtils.Sin(x2) * num * 0.5f;
			float num3 = MathUtils.Cos(x2) * num * 0.5f;
			Vector3 vector4 = new Vector3(num2, 0f, num3);
			Vector3 vector5 = new Vector3(0f - num2, 0f, 0f - num3);
			Vector3 v = vector5 + new Vector3(0f, y2, 0f);
			Vector3 v2 = vector4 + new Vector3(0f, y2, 0f);
			vector4 = Vector3.Transform(vector4, matrix) + vector3;
			vector5 = Vector3.Transform(vector5, matrix) + vector3;
			v = Vector3.Transform(v, matrix) + vector3;
			v2 = Vector3.Transform(v2, matrix) + vector3;
			int count = vertices.Count;
			vertices.Count += 4;
			BlockGeometryGenerator.SetupLitCornerVertex(vector4.X, vector4.Y, vector4.Z, color, textureSlot, textureSlotCount, 0, ref vertices.Array[count]);
			BlockGeometryGenerator.SetupLitCornerVertex(vector5.X, vector5.Y, vector5.Z, color, textureSlot, textureSlotCount, 1, ref vertices.Array[count + 1]);
			BlockGeometryGenerator.SetupLitCornerVertex(v.X, v.Y, v.Z, color, textureSlot, textureSlotCount, 2, ref vertices.Array[count + 2]);
			BlockGeometryGenerator.SetupLitCornerVertex(v2.X, v2.Y, v2.Z, color, textureSlot, textureSlotCount, 3, ref vertices.Array[count + 3]);
			int count2 = indices.Count;
			indices.Count += 12;
			indices.Array[count2] = count;
			indices.Array[count2 + 1] = count + 1;
			indices.Array[count2 + 2] = count + 2;
			indices.Array[count2 + 3] = count + 2;
			indices.Array[count2 + 4] = count + 3;
			indices.Array[count2 + 5] = count;
			indices.Array[count2 + 6] = count + 2;
			indices.Array[count2 + 7] = count + 1;
			indices.Array[count2 + 8] = count;
			indices.Array[count2 + 9] = count;
			indices.Array[count2 + 10] = count + 3;
			indices.Array[count2 + 11] = count + 2;
		}
	}

	public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
	{
		int value2 = Terrain.ExtractData(value);
		int value3 = GetFaceMask(value2);
		int num = 0;
		int num2 = -1;
		for (int i = 0; i < 6; i++)
		{
			if ((value3 & (1 << i)) != 0)
			{
				if (num == raycastResult.CollisionBoxIndex)
				{
					num2 = i;
					break;
				}
				num++;
			}
		}
		if (num2 == -1)
		{
			return base.GetDigValue(subsystemTerrain, componentMiner, value, toolValue, raycastResult);
		}
		int num3 = value3 & ~(1 << num2);
		if (num3 == 0)
		{
			return new BlockPlacementData
			{
				Value = 0,
				CellFace = raycastResult.CellFace
			};
		}
		int data = SetFaceMask(value2, num3);
		return new BlockPlacementData
		{
			Value = Terrain.MakeBlockValue(BlockIndex, 0, data),
			CellFace = raycastResult.CellFace
		};
	}

	public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
	{
		if (IsInvisible(subsystemTerrain, oldValue))
		{
			showDebris = false;
			return;
		}
		int image = GetFaceMask(Terrain.ExtractData(oldValue));
		int faceMask = GetFaceMask(Terrain.ExtractData(newValue));
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			bool flag = (image & (1 << i)) != 0;
			bool flag2 = (faceMask & (1 << i)) != 0;
			if (flag && !flag2)
			{
				num++;
			}
		}
		if (num > 0)
		{
			dropValues.Add(new BlockDropValue
			{
				Value = Terrain.MakeBlockValue(BlockIndex, 0, 0),
				Count = num
			});
		}
		showDebris = num > 0;
	}

	public bool IsInvisible(SubsystemTerrain sst, int value)
	{
		if (GetNoSeason(Terrain.ExtractData(value)))
		{
			return false;
		}
		SubsystemWeather.Season currentSeason = sst.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		if (currentSeason != SubsystemWeather.Season.Anabiosis && currentSeason != SubsystemWeather.Season.Winter && currentSeason != SubsystemWeather.Season.LateWinter)
		{
			return currentSeason == SubsystemWeather.Season.EarlySpring;
		}
		return true;
	}

	public override string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value)
	{
		if (subsystemTerrain != null && IsInvisible(subsystemTerrain, value))
		{
			return string.Empty;
		}
		return base.GetSoundMaterialName(subsystemTerrain, value);
	}
}
