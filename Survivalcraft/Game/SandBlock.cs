using Engine;
using Engine.Graphics;

namespace Game;

public class SandBlock : CubeBlock
{
	public const int Index = 7;

	private readonly BlockColorsMap m_map;

	public SandBlock()
	{
		m_map = BlockColorsMap.SandColorsMap;
	}

	public override void GenerateTerrainVertices(BlockGeometryGenerator g, TerrainGeometry geom, int value, int x, int y, int z)
	{
		Color color = m_map.LookupSand(g.Terrain, x, y, z);
		g.GenerateNaturalCubeVertices(this, value, x, y, z, color, geom.OpaqueSubsetsByFace);
	}

	public override BlockPlacementData GetPlacementValue(SubsystemTerrain sst, ComponentMiner m, int value, TerrainRaycastResult hit)
	{
		return new BlockPlacementData
		{
			Value = value,
			CellFace = hit.CellFace
		};
	}

	public override void DrawBlock(PrimitivesRenderer3D r, int value, Color color, float size, ref Matrix m, DrawBlockEnvironmentData env)
	{
		color *= m_map.LookupSand(env.Temperature, env.Humidity);
		BlocksManager.DrawCubeBlock(r, value, new Vector3(size), ref m, color, color, env);
	}

	public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain sst, Vector3 pos, int value, float str)
	{
		Color color = m_map.LookupSand(sst.Terrain, Terrain.ToCell(pos.X), Terrain.ToCell(pos.Y), Terrain.ToCell(pos.Z));
		return new BlockDebrisParticleSystem(sst, pos, str, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
	}
}
