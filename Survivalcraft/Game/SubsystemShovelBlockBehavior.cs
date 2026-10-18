using System;
using System.Collections.Generic;
using System.Reflection;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemShovelBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemAudio m_subsystemAudio;

	public Random m_random;

	private SubsystemFireBlockBehavior m_subsystemFire;

	private FieldInfo m_fiToBurnAway;

	private FieldInfo m_fiToExpand;

	public override int[] HandledBlocks => new int[4] { 113, 36, 222, 407 };

	public override bool OnUse(Ray3 ray, ComponentMiner miner)
	{
		TerrainRaycastResult? terrainRaycastResult = miner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
		if (!terrainRaycastResult.HasValue)
		{
			return false;
		}
		Point3 point = terrainRaycastResult.Value.CellFace.Point;
		bool flag = false;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					int num = point.X + i;
					int num2 = point.Y + j;
					int num3 = point.Z + k;
					if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(num, num2, num3)) == 104)
					{
						flag = true;
						m_subsystemTerrain.ChangeCell(num, num2, num3, 0);
						if (m_subsystemFire != null)
						{
							Point3 key = new Point3(num, num2, num3);
							FieldInfo fiToBurnAway = m_fiToBurnAway;
							Dictionary<Point3, float> dictionary = (Dictionary<Point3, float>)((fiToBurnAway != null) ? fiToBurnAway.GetValue(m_subsystemFire) : null);
							FieldInfo fiToExpand = m_fiToExpand;
							Dictionary<Point3, float> dictionary2 = (Dictionary<Point3, float>)((fiToExpand != null) ? fiToExpand.GetValue(m_subsystemFire) : null);
							dictionary?.Remove(key);
							dictionary2?.Remove(key);
						}
						string directory = ((m_random.Float(0f, 1f) <= 0.7f) ? "Audio/Sizzles" : "Audio/Impacts/Metal");
						m_subsystemAudio.PlayRandomSound(directory, 1f, m_random.Float(-0.1f, 0.1f), new Vector3((float)num + 0.5f, (float)num2 + 0.5f, (float)num3 + 0.5f), 3f, autoDelay: true);
					}
				}
			}
		}
		if (flag)
		{
			miner.DamageActiveTool(1);
		}
		if (flag)
		{
			return flag;
		}
		int x = terrainRaycastResult.Value.CellFace.X;
		int y = terrainRaycastResult.Value.CellFace.Y;
		int z = terrainRaycastResult.Value.CellFace.Z;
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		if (Terrain.ExtractContents(cellValue) != 363)
		{
			return false;
		}
		int num4 = Terrain.ExtractData(cellValue);
		int num5 = (num4 >> 10) & 0xF;
		num5 = num5 % 8 + 1;
		num4 = (num4 & -15361) | (num5 << 10);
		Vector3 forward = Matrix.CreateFromQuaternion(miner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
		float num6 = Vector3.Dot(forward, Vector3.UnitZ);
		float num7 = Vector3.Dot(forward, Vector3.UnitX);
		float num8 = Vector3.Dot(forward, -Vector3.UnitZ);
		float num9 = Vector3.Dot(forward, -Vector3.UnitX);
		int num10 = (((num6 >= num7 && num6 >= num8 && num6 >= num9) ? 2 : ((num7 >= num6 && num7 >= num8 && num7 >= num9) ? 3 : ((!(num8 >= num6) || !(num8 >= num7) || !(num8 >= num9)) ? 1 : 0))) + 2) & 3;
		num4 = (num4 & -49153) | (num10 << 14);
		m_subsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceData(cellValue, num4));
		m_subsystemAudio.PlayRandomSound("Audio/Impacts/Bones", 0.5f, 0f, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), 3f, autoDelay: true);
		m_subsystemAudio.PlayRandomSound("Audio/Impacts/Metal", 0.5f, 0f, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), 3f, autoDelay: true);
		miner.DamageActiveTool(1);
		return true;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemFire = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_random = new Random();
		if (m_subsystemFire != null)
		{
			Type smokeTrailParticleSystem = typeof(SubsystemFireBlockBehavior);
			m_fiToBurnAway = smokeTrailParticleSystem.GetField("m_toBurnAway", BindingFlags.Instance | BindingFlags.NonPublic);
			m_fiToExpand = smokeTrailParticleSystem.GetField("m_toExpand", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		m_random = new Random();
	}
}
