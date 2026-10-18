using System.Collections.Generic;
using Engine;
using TemplatesDatabase;

namespace Game;

public class SubsystemRadioBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	private SubsystemTime m_subsystemTime;

	private SubsystemTerrain m_subsystemTerrain;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemRadio m_subsystemRadio;

	private Dictionary<Point3, double> m_cooldowns;

	private List<Point3> m_tmp;

	private SubsystemRadioAntennaBlockBehavior m_subsystemRadioAntenna;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override int[] HandledBlocks => new int[1] { 417 };

	private int m_tracksMax
	{
		get
		{
			if (m_subsystemRadio != null)
			{
				return MathUtils.Clamp(m_subsystemRadio.TracksCount, 0, 7);
			}
			return 0;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemRadio = Project.FindSubsystem<SubsystemRadio>(throwOnError: true);
		m_subsystemRadioAntenna = Project.FindSubsystem<SubsystemRadioAntennaBlockBehavior>(throwOnError: true);
		m_cooldowns = new Dictionary<Point3, double>();
		m_tmp = new List<Point3>();
	}

	public void Update(float dt)
	{
		if (m_cooldowns == null || m_cooldowns.Count <= 0)
		{
			return;
		}
		m_tmp.Clear();
		foreach (KeyValuePair<Point3, double> cooldown in m_cooldowns)
		{
			if (m_subsystemTime.GameTime >= cooldown.Value)
			{
				m_tmp.Add(cooldown.Key);
			}
		}
		for (int i = 0; i < m_tmp.Count; i++)
		{
			m_cooldowns.Remove(m_tmp[i]);
		}
	}

	public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
	{
		int x = raycastResult.CellFace.X;
		int y = raycastResult.CellFace.Y;
		int z = raycastResult.CellFace.Z;
		Point3 point = new Point3(x, y, z);
		if (m_cooldowns.TryGetValue(point, out var value) && m_subsystemTime.GameTime < value)
		{
			return true;
		}
		int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
		int trackIndex = RadioBlock.GetTrackIndex(cellValue);
		Vector3 pos = new Vector3((float)x + 0.5f, (float)y + 0.2f, (float)z + 0.5f);
		bool flag = false;
		int bestTopY = 0;
		int bestHeight = 0;
		int num = 0;
		if (m_subsystemRadioAntenna != null && m_subsystemRadioAntenna.TryGetBestAntennaForRadio(x, y, z, out bestTopY, out bestHeight))
		{
			num = SubsystemRadioAntennaBlockBehavior.GetTracksAvailableByTopY(bestTopY);
			if (num > 0)
			{
				flag = true;
			}
		}
		int num2 = 0;
		if (m_subsystemRadio != null)
		{
			num2 = MathUtils.Clamp(m_subsystemRadio.TracksCount, 0, 7);
		}
		int num3 = ((trackIndex == 0) ? 1 : ((trackIndex < 7) ? (trackIndex + 1) : 0));
		int value2 = RadioBlock.SetTrackIndex(cellValue, num3);
		m_subsystemTerrain.ChangeCell(x, y, z, value2);
		if (m_subsystemRadio == null)
		{
			return true;
		}
		if (num3 == 0)
		{
			m_subsystemRadio.StopAt(point);
			RadioLabels.Show(point, "Off", 1.2, m_subsystemTime.GameTime);
			SafeClick(pos, on: false);
			m_cooldowns[point] = m_subsystemTime.GameTime + 1.0;
			return true;
		}
		if (flag && num3 <= num && num3 <= num2)
		{
			m_subsystemRadio.StartAt(point, num3);
			string trackDisplayNameByIndex = m_subsystemRadio.GetTrackDisplayNameByIndex(num3);
			RadioLabels.Show(point, trackDisplayNameByIndex, 2.0, m_subsystemTime.GameTime);
		}
		else
		{
			m_subsystemRadio.StartNoiseAt(point);
			RadioLabels.Show(point, "No signal", 2.0, m_subsystemTime.GameTime);
		}
		SafeClick(pos, on: true);
		return true;
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		Point3 point = new Point3(x, y, z);
		m_subsystemRadio.StopAt(point);
		m_cooldowns.Remove(point);
	}

	public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
	{
		int trackIndex = RadioBlock.GetTrackIndex(value);
		if (trackIndex > 0)
		{
			m_subsystemRadio.StartFromSaved(new Point3(x, y, z), trackIndex);
		}
	}

	public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
	{
		int trackIndex = RadioBlock.GetTrackIndex(value);
		if (trackIndex > 0)
		{
			m_subsystemRadio.StartFromSaved(new Point3(x, y, z), trackIndex);
		}
	}

	private void SafeClick(Vector3 pos, bool on)
	{
		try
		{
			string name = (on ? "Audio/BlockPlaced" : "Audio/BlockPlaced");
			m_subsystemAudio.PlaySound(name, 1f, 0f, pos, 2f, autoDelay: false);
		}
		catch
		{
		}
	}
}
