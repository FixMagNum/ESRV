using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game;

public class SoilBlock : CubeBlock
{
	public const int Index = 168;

	public new static string fName;

	public static BoundingBox[] m_collisionBoxes;

	public override int GetFaceTextureSlot(int face, int value)
	{
		int nitrogen = GetNitrogen(Terrain.ExtractData(value));
		if (face != 4)
		{
			return 2;
		}
		if (nitrogen <= 0)
		{
			return 37;
		}
		return 53;
	}

	public static bool GetHydration(int data)
	{
		return (data & 1) != 0;
	}

	public static int GetNitrogen(int data)
	{
		return (data >> 1) & 3;
	}

	public static int SetHydration(int data, bool hydration)
	{
		if (!hydration)
		{
			return data & -2;
		}
		return data | 1;
	}

	public static int SetNitrogen(int data, int nitrogen)
	{
		nitrogen = MathUtils.Clamp(nitrogen, 0, 3);
		return (data & -7) | ((nitrogen & 3) << 1);
	}

	public override IEnumerable<int> GetCreativeValues()
	{
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(0, hydration: false));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(0, hydration: true));
		yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(SetNitrogen(0, 3), hydration: true));
	}

	public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
	{
		int data = Terrain.ExtractData(value);
		int nitrogen = GetNitrogen(data);
		bool hydration = GetHydration(data);
		if ((nitrogen > 0) & hydration)
		{
			LanguageControl.Get(fName, 2);
			return LanguageControl.Get(fName, 1);
		}
		if (nitrogen > 0)
		{
			LanguageControl.Get(fName, 2);
			return LanguageControl.Get(fName, 2);
		}
		if (hydration)
		{
			return LanguageControl.Get(fName, 3);
		}
		return LanguageControl.Get(fName, 4);
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
	{
		SubsystemWeather.Season subsystemInventories = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		Color color = new BlockColorsMap(Color.White, Color.White, Color.White, Color.White).LookupSeasonalDirtColor(subsystemInventories, generator.Terrain, x, y, z);
		Color value2 = (GetHydration(Terrain.ExtractData(value)) ? new Color(180, 170, 150) : Color.White);
		Color i = color * value2;
		generator.GenerateCubeVertices(this, value, x, y, z, 0.9375f, 0.9375f, 0.9375f, 0.9375f, i, i, i, i, i, -1, geometry.OpaqueSubsetsByFace);
	}

	public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
	{
		Color valuesDictionary2 = (GetHydration(Terrain.ExtractData(value)) ? new Color(180, 170, 150) : Color.White);
		base.DrawBlock(primitivesRenderer, value, color * valuesDictionary2, size, ref matrix, environmentData);
	}

	public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
	{
		return m_collisionBoxes;
	}

	public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
	{
		return face != 5;
	}

	static SoilBlock()
	{
		fName = "SoilBlock";
		m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.9375f, 1f))
		};
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
	{
		SubsystemWeather.Season currentSeason = subsystemTerrain.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).CurrentSeason;
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		Color color = new BlockColorsMap(Color.White, Color.White, Color.White, Color.White).LookupSeasonalDirtColor(currentSeason, subsystemTerrain.Terrain, x, y, z);
		int faceTextureSlot = GetFaceTextureSlot(4, value);
		return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, faceTextureSlot);
	}

	public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
	{
		if (Terrain.ExtractContents(neighborValue) != 168)
		{
			return base.ShouldGenerateFace(subsystemTerrain, face, value, neighborValue);
		}
		return false;
	}
}
