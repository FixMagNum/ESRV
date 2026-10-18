using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemFertilizerBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemParticles m_subsystemParticles;

	public SubsystemAudio m_subsystemAudio;

	public override int[] HandledBlocks => new int[2] { 102, 369 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
		if (terrainRaycastResult.HasValue && terrainRaycastResult.Value.CellFace.Face == 4)
		{
			int x = terrainRaycastResult.Value.CellFace.X;
			int y = terrainRaycastResult.Value.CellFace.Y;
			int z = terrainRaycastResult.Value.CellFace.Z;
			int num = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
			int num2 = ((num == 369) ? 1 : 3);
			List<Point3> list = new List<Point3>();
			for (int i = x - 1; i <= x + 1; i++)
			{
				for (int j = z - 1; j <= z + 1; j++)
				{
					switch (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(i, y, j)))
					{
					case 8:
						list.Add(new Point3(i, y, j));
						break;
					case 2:
						m_subsystemTerrain.ChangeCell(i, y, j, Terrain.MakeBlockValue(8));
						break;
					}
				}
			}
			int num3 = ((num != 369) ? 1 : 0);
			for (int k = x - num3; k <= x + num3; k++)
			{
				for (int l = z - num3; l <= z + num3; l++)
				{
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(k, y, l);
					if (Terrain.ExtractContents(cellValue) == 168)
					{
						int nitrogen = SoilBlock.GetNitrogen(Terrain.ExtractData(cellValue));
						int data = SoilBlock.SetNitrogen(Terrain.ExtractData(cellValue), MathUtils.Clamp(nitrogen + num2, 0, 3));
						int value = Terrain.ReplaceData(cellValue, data);
						m_subsystemTerrain.ChangeCell(k, y, l, value);
					}
				}
			}
			if (list.Count > 0)
			{
				Random random = new Random();
				int num4 = MathUtils.Clamp(random.Int(3, 9), 1, list.Count);
				for (int m = 0; m < num4; m++)
				{
					Point3 point = list[random.Int(0, list.Count - 1)];
					int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
					int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(m_subsystemTerrain.Terrain.GetShaftValue(point.X, point.Z));
					int humidity = m_subsystemTerrain.Terrain.GetShaftValue(point.X, point.Z) & 0xF;
					int num5 = PlantsManager.GenerateRandomPlantValue(random, cellValue2, seasonalTemperature, humidity, point.Y + 1);
					if (num5 != 0)
					{
						m_subsystemTerrain.ChangeCell(point.X, point.Y + 1, point.Z, num5);
					}
				}
			}
			m_subsystemAudio.PlayRandomSound("Audio/Impacts/Dirt", 0.5f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
			Vector3 position = new Vector3((float)x + 0.5f, (float)y + 1.5f, (float)z + 0.5f);
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(componentMiner.ActiveBlockValue)];
			m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(m_subsystemTerrain, position, componentMiner.ActiveBlockValue, 1.25f));
			componentMiner.RemoveActiveTool(1);
			return true;
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
	}
}
