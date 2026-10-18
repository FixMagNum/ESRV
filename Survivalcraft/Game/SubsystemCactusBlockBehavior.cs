using System.Collections.Generic;
using Engine;
using Game.NetWork;
using Game.NetWork.Packages;
using TemplatesDatabase;

namespace Game;

public class SubsystemCactusBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
{
	public SubsystemTime m_subsystemTime;

	public SubsystemGameInfo m_subsystemGameInfo;

	public Dictionary<Point3, int> m_toUpdate = new Dictionary<Point3, int>();

	public Random m_random = new Random();

	public override int[] HandledBlocks => new int[2] { 127, 375 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
	{
		int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
		if (cellContents != 7 && cellContents != 127 && cellContents != 375)
		{
			SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
		}
	}

	public override void OnPoll(int value, int x, int y, int z, int pollPass)
	{
		if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != EnvironmentBehaviorMode.Living)
		{
			return;
		}
		int eyePosition = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
		if (Terrain.ExtractContents(eyePosition) == 0 && Terrain.ExtractLight(eyePosition) >= 12)
		{
			int x2 = Terrain.ExtractContents(value);
			int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
			int num = SubsystemTerrain.Terrain.GetCellContents(x, y - 2, z);
			if ((cellContents != x2 || num != x2) && m_random.Float(0f, 1f) < 0.25f)
			{
				m_toUpdate[new Point3(x, y + 1, z)] = Terrain.MakeBlockValue(x2, 0, 0);
			}
		}
	}

	public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
	{
		ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
		if (componentCreature != null)
		{
			string text = LanguageControl.Get("ComponentHealth", "killed_by_cactus");
			float amount = 0.03f * MathUtils.Abs(velocity);
			if (CommonLib.WorkType != WorkType.Client)
			{
				componentCreature.ComponentHealth.Injure(amount, null, ignoreInvulnerability: false, text);
			}
			else
			{
				CommonLib.Net.QueuePackage(new ComponentHealthPackage(componentCreature.ComponentHealth, null, amount, text, ignoreInvulnerability: false, isRequest: true));
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		base.Load(valuesDictionary);
	}

	public void Update(float dt)
	{
		if (!m_subsystemTime.PeriodicGameTimeEvent(60.0, 0.0))
		{
			return;
		}
		foreach (KeyValuePair<Point3, int> item in m_toUpdate)
		{
			if (SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z) == 0)
			{
				SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
			}
		}
		m_toUpdate.Clear();
	}
}
