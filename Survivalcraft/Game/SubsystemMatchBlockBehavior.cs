using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemMatchBlockBehavior : SubsystemBlockBehavior
{
	public SubsystemAudio m_subsystemAudio;

	public SubsystemGameInfo m_subsystemGameInfo;

	public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

	public SubsystemExplosivesBlockBehavior m_subsystemExplosivesBlockBehavior;

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[1] { 108 };

	public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
	{
		object node = componentMiner.Raycast(ray, RaycastMode.Digging);
		if (node is TerrainRaycastResult)
		{
			CellFace text = ((TerrainRaycastResult)node).CellFace;
			if (m_subsystemExplosivesBlockBehavior.IgniteFuse(text.X, text.Y, text.Z, componentMiner.ComponentPlayer.PlayerData))
			{
				m_subsystemAudio.PlaySound("Audio/Match", 1f, m_random.Float(-0.1f, 0.1f), ray.Position, 1f, autoDelay: true);
				componentMiner.RemoveActiveTool(1);
				return true;
			}
			Point3 componentPlayer = new Point3(0, 0, 0);
			bool ex = false;
			Point3 point = CellFace.FaceToPoint3(text.Face);
			int x = text.X + point.X;
			int y = text.Y + point.Y;
			int z = text.Z + point.Z;
			if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) == 0)
			{
				componentPlayer = new Point3(x, y, z);
				ex = true;
			}
			else
			{
				for (int i = 0; i < 6; i++)
				{
					Point3 point2 = CellFace.FaceToPoint3(i);
					int x2 = text.X + point2.X;
					int y2 = text.Y + point2.Y;
					int z2 = text.Z + point2.Z;
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(x2, y2, z2)) == 0)
					{
						componentPlayer = new Point3(x2, y2, z2);
						ex = true;
						break;
					}
				}
			}
			if (ex)
			{
				int num = 0;
				if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(componentPlayer.X, componentPlayer.Y - 1, componentPlayer.Z)) == 0)
				{
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(componentPlayer.X, componentPlayer.Y, componentPlayer.Z + 1)) != 0)
					{
						num |= 1;
					}
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(componentPlayer.X + 1, componentPlayer.Y, componentPlayer.Z)) != 0)
					{
						num |= 2;
					}
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(componentPlayer.X, componentPlayer.Y, componentPlayer.Z - 1)) != 0)
					{
						num |= 4;
					}
					if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(componentPlayer.X - 1, componentPlayer.Y, componentPlayer.Z)) != 0)
					{
						num |= 8;
					}
				}
				SubsystemTerrain.ChangeCell(componentPlayer.X, componentPlayer.Y, componentPlayer.Z, Terrain.MakeBlockValue(104, 0, num));
				m_subsystemAudio.PlaySound("Audio/Match", 1f, m_random.Float(-0.1f, 0.1f), ray.Position, 1f, autoDelay: true);
				componentMiner.RemoveActiveTool(1);
				return true;
			}
		}
		else if (node is BodyRaycastResult)
		{
			ComponentOnFire componentOnFire = ((BodyRaycastResult)node).ComponentBody.Entity.FindComponent<ComponentOnFire>();
			if (componentOnFire != null)
			{
				if (m_subsystemGameInfo.WorldSettings.GameMode < GameMode.Challenging || m_random.Float(0f, 1f) < 0.33f)
				{
					componentOnFire.SetOnFire(componentMiner.ComponentCreature, m_random.Float(6f, 8f));
				}
				m_subsystemAudio.PlaySound("Audio/Match", 1f, m_random.Float(-0.1f, 0.1f), ray.Position, 1f, autoDelay: true);
				componentMiner.RemoveActiveTool(1);
				return true;
			}
		}
		return false;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
		m_subsystemExplosivesBlockBehavior = Project.FindSubsystem<SubsystemExplosivesBlockBehavior>(throwOnError: true);
	}
}
