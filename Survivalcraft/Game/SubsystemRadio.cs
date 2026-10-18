using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Engine.Media;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemRadio : Subsystem, IUpdateable
{
	private class RadioInstance
	{
		public StreamingSound Sound;

		public int TrackIndex;

		public string TrackPath;

		public Vector3 Position;

		public double StartGameTime;

		public double StartOffset;

		public SoundParticleSystem Particles;

		public double NextEmitTime;

		public double NextNoiseTime;

		public bool IsNoise;
	}

	private struct SavedState
	{
		public int TrackIndex;

		public double Offset;

		public double SavedTime;
	}

	private List<string> m_tracks;

	private Dictionary<string, string> m_trackDisplayNames;

	private Dictionary<Point3, RadioInstance> m_instances;

	private List<Point3> m_toStop;

	private Dictionary<Point3, SavedState> m_saved;

	private Dictionary<string, double> m_trackDurations;

	private SubsystemParticles m_subsystemParticles;

	private SubsystemNoise m_subsystemNoise;

	private Random m_rand = new Random();

	private Dictionary<string, Vector3> m_trackBaseHsv = new Dictionary<string, Vector3>();

	private string m_noiseTrackPath = "Music/Radio/RadioNoise";

	private SubsystemRadioAntennaBlockBehavior m_subsystemRadioAntenna;

	public SubsystemAudio SubsystemAudio { get; private set; }

	public SubsystemTime SubsystemTime { get; private set; }

	public SubsystemTerrain SubsystemTerrain { get; private set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	private float Volume => MathUtils.Clamp(SettingsManager.MusicVolume / 10f * 4f, 0f, 1f);

	public int TracksCount => m_tracks.Count;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		SubsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
		m_subsystemRadioAntenna = Project.FindSubsystem<SubsystemRadioAntennaBlockBehavior>(throwOnError: true);
		m_tracks = new List<string> { "Music/Radio/Cataclysmic Touch", "Music/Radio/Embrace - Solar Ferrous", "Music/Radio/Sanctum of Pastra", "Music/AloneForever", "Music/Radio/Embrace - the Horse Innokentiy, infected by light", "Music/Radio/Aberrant Vitality", "Music/Radio/Dance of the Black Veil" };
		m_trackDisplayNames = new Dictionary<string, string>
		{
			{ "Music/Radio/Cataclysmic Touch", "1. Cataclysmic Touch" },
			{ "Music/Radio/Embrace - Solar Ferrous", "2. Embrace - Solar Ferrous" },
			{ "Music/Radio/Sanctum of Pastra", "3. Sanctum of Pastra" },
			{ "Music/AloneForever", "4. Survivalcraft - AloneForever" },
			{ "Music/Radio/Embrace – the Horse Innokentiy, infected by light", "5. Embrace - the Horse Innokentiy, infected by light" },
			{ "Music/Radio/Aberrant Vitality", "6. Aberrant Vitality" },
			{ "Music/Radio/Dance of the Black Veil", "7. Dance of the Black Veil" },
			{ m_noiseTrackPath, "White Noise" }
		};
		m_trackDurations = new Dictionary<string, double>
		{
			{ "Music/Radio/Cataclysmic Touch", 105.0 },
			{ "Music/Radio/Embrace - Solar Ferrous", 286.0 },
			{ "Music/Radio/Sanctum of Pastra", 220.0 },
			{ "Music/AloneForever", 128.0 },
			{ "Music/Radio/Embrace - the Horse Innokentiy, infected by light", 194.0 },
			{ "Music/Radio/Aberrant Vitality", 186.0 },
			{ "Music/Radio/Dance of the Black Veil", 302.0 },
			{ m_noiseTrackPath, 8.0 }
		};
		m_trackBaseHsv.Clear();
		m_trackBaseHsv["Music/Radio/Cataclysmic Touch"] = new Vector3(180f, 0.3f, 0.7f);
		m_trackBaseHsv["Music/Radio/Embrace - Solar Ferrous"] = new Vector3(210f, 0.35f, 0.65f);
		m_trackBaseHsv["Music/Radio/Sanctum of Pastra"] = new Vector3(35f, 0.4f, 0.72f);
		m_trackBaseHsv["Music/AloneForever"] = new Vector3(330f, 0.3f, 0.68f);
		m_trackBaseHsv["Music/Radio/Embrace - the Horse Innokentiy, infected by light"] = new Vector3(55f, 0.45f, 0.7f);
		m_trackBaseHsv["Music/Radio/Aberrant Vitality"] = new Vector3(120f, 0.4f, 0.6f);
		m_trackBaseHsv["Music/Radio/Dance of the Black Veil"] = new Vector3(0f, 0.1f, 0.2f);
		m_trackBaseHsv[m_noiseTrackPath] = new Vector3(200f, 0.05f, 0.4f);
		m_saved.Clear();
		ValuesDictionary valuesDictionary2;
		if (valuesDictionary.ContainsKey("Radio"))
		{
			valuesDictionary2 = valuesDictionary.GetValue<ValuesDictionary>("Radio");
		}
		else
		{
			valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.Add("Radio", valuesDictionary2);
		}
		if (!valuesDictionary2.ContainsKey("States"))
		{
			return;
		}
		ValuesDictionary value = valuesDictionary2.GetValue<ValuesDictionary>("States");
		foreach (string key2 in value.Keys)
		{
			ValuesDictionary value2 = value.GetValue<ValuesDictionary>(key2);
			Point3 key = ParseCellKey(key2);
			SavedState value3 = new SavedState
			{
				TrackIndex = value2.GetValue<int>("TrackIndex"),
				Offset = value2.GetValue<double>("Offset"),
				SavedTime = value2.GetValue<double>("SavedTime")
			};
			m_saved[key] = value3;
		}
	}

	public override void Dispose()
	{
		StopAll();
		base.Dispose();
	}

	public void Update(float dt)
	{
		RadioLabels.Cleanup(SubsystemTime.GameTime);
		m_toStop.Clear();
		double gameTime = SubsystemTime.GameTime;
		foreach (KeyValuePair<Point3, RadioInstance> instance in m_instances)
		{
			Point3 key = instance.Key;
			RadioInstance value = instance.Value;
			bool flag = false;
			if (SubsystemTerrain.Terrain.GetChunkAtCell(key.X, key.Z) != null)
			{
				int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
				if (Terrain.ExtractContents(cellValue) != 417 || RadioBlock.GetTrackIndex(cellValue) == 0)
				{
					flag = true;
				}
			}
			if (flag)
			{
				m_toStop.Add(key);
				continue;
			}
			float num = SubsystemAudio.CalculateListenerDistanceSquared(value.Position);
			float num2 = 48f;
			float num3 = 1f - MathUtils.Saturate(num / (num2 * num2));
			if (!(ScreensManager.CurrentScreen is GameScreen))
			{
				num3 = 0f;
			}
			float volume = Volume * num3;
			if (value.Sound != null)
			{
				if (value.Sound.State == SoundState.Stopped)
				{
					value.Sound.Play();
				}
				value.Sound.Volume = volume;
			}
			if (m_subsystemNoise != null && gameTime >= value.NextNoiseTime)
			{
				float loudness = 0.25f + 0.35f * num3;
				float range = MathUtils.Lerp(10f, 24f, num3);
				m_subsystemNoise.MakeNoise(value.Position, loudness, range);
				value.NextNoiseTime = gameTime + 0.45 + (double)m_rand.Float(0f, 0.25f);
			}
			if (m_subsystemParticles != null && num3 > 0.02f && gameTime >= value.NextEmitTime)
			{
				Color radioParticleColor = GetRadioParticleColor(value, gameTime);
				EmitRadioParticle(value, radioParticleColor);
				value.NextEmitTime = gameTime + 0.15 + (double)m_rand.Float(0f, 0.25f);
			}
		}
		for (int i = 0; i < m_toStop.Count; i++)
		{
			Point3 cell = m_toStop[i];
			StopAt(cell);
			if (CommonLib.WorkType != WorkType.Client)
			{
				int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(cell.X, cell.Y, cell.Z);
				if (Terrain.ExtractContents(cellValue2) == 417 && RadioBlock.GetTrackIndex(cellValue2) != 0)
				{
					int value2 = RadioBlock.SetTrackIndex(cellValue2, 0);
					SubsystemTerrain.ChangeCell(cell.X, cell.Y, cell.Z, value2);
				}
			}
		}
	}

	public string GetTrackPathByIndex(int index)
	{
		if (index <= 0 || index > m_tracks.Count)
		{
			return null;
		}
		return m_tracks[index - 1];
	}

	public string GetTrackDisplayNameByIndex(int index)
	{
		string trackPathByIndex = GetTrackPathByIndex(index);
		if (string.IsNullOrEmpty(trackPathByIndex))
		{
			return "??????";
		}
		if (!m_trackDisplayNames.TryGetValue(trackPathByIndex, out var value))
		{
			return trackPathByIndex;
		}
		return value;
	}

	public SubsystemRadio()
	{
		m_instances = new Dictionary<Point3, RadioInstance>();
		m_saved = new Dictionary<Point3, SavedState>();
		m_toStop = new List<Point3>();
	}

	public void StartAt(Point3 cell, int trackIndex)
	{
		StartAtWithOffset(cell, trackIndex, 0.0);
	}

	public void StopAt(Point3 cell)
	{
		if (!m_instances.TryGetValue(cell, out var value))
		{
			return;
		}
		try
		{
			if (value.Sound != null)
			{
				value.Sound.Stop();
				value.Sound.Dispose();
			}
		}
		catch
		{
		}
		m_instances.Remove(cell);
	}

	public void StopAll()
	{
		foreach (RadioInstance value in m_instances.Values)
		{
			try
			{
				if (value.Sound != null)
				{
					value.Sound.Stop();
					value.Sound.Dispose();
				}
			}
			catch
			{
			}
		}
		m_instances.Clear();
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary gameMode2;
		if (valuesDictionary.ContainsKey("Radio"))
		{
			gameMode2 = valuesDictionary.GetValue<ValuesDictionary>("Radio");
		}
		else
		{
			gameMode2 = new ValuesDictionary();
			valuesDictionary.Add("Radio", gameMode2);
		}
		ValuesDictionary stackPanelWidget = new ValuesDictionary();
		foreach (KeyValuePair<Point3, RadioInstance> labelWidget2 in m_instances)
		{
			Point3 text2 = labelWidget2.Key;
			RadioInstance widget2 = labelWidget2.Value;
			if (!widget2.IsNoise)
			{
				double currentPositionSeconds = GetCurrentPositionSeconds(widget2);
				ValuesDictionary valuesDictionary2 = new ValuesDictionary();
				valuesDictionary2.SetValue("TrackIndex", widget2.TrackIndex);
				valuesDictionary2.SetValue("Offset", currentPositionSeconds);
				valuesDictionary2.SetValue("SavedTime", SubsystemTime.GameTime);
				stackPanelWidget.Add(MakeCellKey(text2), valuesDictionary2);
			}
		}
		if (gameMode2.ContainsKey("States"))
		{
			gameMode2.SetValue("States", stackPanelWidget);
		}
		else
		{
			gameMode2.Add("States", stackPanelWidget);
		}
	}

	public void StartAtWithOffset(Point3 cell, int trackIndex, double offsetSeconds)
	{
		StopAt(cell);
		string trackPathByIndex = GetTrackPathByIndex(trackIndex);
		if (string.IsNullOrEmpty(trackPathByIndex))
		{
			return;
		}
		offsetSeconds = NormalizeOffset(trackPathByIndex, offsetSeconds);
		try
		{
			StreamingSource streamingSource = ContentManager.Get<StreamingSource>(trackPathByIndex).Duplicate();
			long x = (long)(offsetSeconds * (double)streamingSource.SamplingFrequency) * 2 * streamingSource.ChannelsCount;
			x = MathUtils.Clamp(x, 0L, streamingSource.BytesCount);
			streamingSource.Position = x;
			StreamingSound streamingSound = new StreamingSound(streamingSource, Volume, 1f, 0f, isLooped: true, disposeOnStop: false, 1f);
			streamingSound.Volume = 0f;
			streamingSound.Play();
			Vector3 position = new Vector3((float)cell.X + 0.5f, (float)cell.Y + 0.2f, (float)cell.Z + 0.5f);
			RadioInstance value = new RadioInstance
			{
				Sound = streamingSound,
				TrackIndex = trackIndex,
				TrackPath = trackPathByIndex,
				Position = position,
				StartGameTime = SubsystemTime.GameTime,
				StartOffset = offsetSeconds,
				Particles = new SoundParticleSystem(SubsystemTerrain, position, Vector3.UnitY),
				NextEmitTime = SubsystemTime.GameTime + 0.1 + (double)m_rand.Float(0f, 0.25f),
				NextNoiseTime = SubsystemTime.GameTime + 0.3 + (double)m_rand.Float(0f, 0.25f),
				IsNoise = false
			};
			m_instances[cell] = value;
		}
		catch
		{
		}
	}

	public void StartFromSaved(Point3 cell, int trackIndex)
	{
		double offsetSeconds = 0.0;
		if (m_saved.TryGetValue(cell, out var value) && value.TrackIndex == trackIndex)
		{
			double val = SubsystemTime.GameTime - value.SavedTime;
			string trackPathByIndex = GetTrackPathByIndex(trackIndex);
			GetDuration(trackPathByIndex);
			offsetSeconds = NormalizeOffset(trackPathByIndex, value.Offset + Math.Max(0.0, val));
		}
		StartAtWithOffset(cell, trackIndex, offsetSeconds);
	}

	public float GetListenerPresenceFactor()
	{
		float num = 0f;
		foreach (KeyValuePair<Point3, RadioInstance> instance in m_instances)
		{
			float num2 = SubsystemAudio.CalculateListenerDistanceSquared(instance.Value.Position);
			float num3 = 48f;
			float num4 = 1f - MathUtils.Saturate(num2 / (num3 * num3));
			if (!(ScreensManager.CurrentScreen is GameScreen))
			{
				num4 = 0f;
			}
			if (num4 > num)
			{
				num = num4;
			}
		}
		return num;
	}

	private double GetCurrentPositionSeconds(RadioInstance ri)
	{
		double duration = GetDuration(ri.TrackPath);
		double num = SubsystemTime.GameTime - ri.StartGameTime + ri.StartOffset;
		if (duration > 0.001)
		{
			num = MathUtils.Remainder(num, duration);
			if (num < 0.0)
			{
				num += duration;
			}
		}
		return MathUtils.Clamp(num, 0.0, Math.Max(0.0, duration - 0.001));
	}

	private double GetDuration(string path)
	{
		if (!m_trackDurations.TryGetValue(path, out var value))
		{
			return 180.0;
		}
		return value;
	}

	private double NormalizeOffset(string path, double offset)
	{
		double duration = GetDuration(path);
		if (duration <= 0.001)
		{
			return 0.0;
		}
		offset = MathUtils.Remainder(offset, duration);
		if (offset < 0.0)
		{
			offset += duration;
		}
		return MathUtils.Clamp(offset, 0.0, duration - 0.001);
	}

	private static string MakeCellKey(Point3 c)
	{
		return c.X + "," + c.Y + "," + c.Z;
	}

	private static Point3 ParseCellKey(string s)
	{
		int num = s.IndexOf(',');
		int num2 = ((num >= 0) ? s.IndexOf(',', num + 1) : (-1));
		if (num < 0 || num2 < 0)
		{
			return new Point3(0, 0, 0);
		}
		int x = int.Parse(s.Substring(0, num));
		int y = int.Parse(s.Substring(num + 1, num2 - num - 1));
		int z = int.Parse(s.Substring(num2 + 1));
		return new Point3(x, y, z);
	}

	private void EmitRadioParticle(RadioInstance ri, Color color)
	{
		if (ri.Particles != null)
		{
			if (ri.Particles.SubsystemParticles == null && m_subsystemParticles != null)
			{
				m_subsystemParticles.AddParticleSystem(ri.Particles);
			}
			ri.Particles.m_direction = Vector3.Normalize(Vector3.UnitY + m_rand.Vector3(0.35f));
			ri.Particles.m_position = ri.Position + new Vector3(m_rand.Float(-0.12f, 0.12f), m_rand.Float(0.05f, 0.18f), m_rand.Float(-0.12f, 0.12f));
			ri.Particles.AddNote(color);
		}
	}

	private Color GetRadioParticleColor(RadioInstance ri, double now)
	{
		if (!m_trackBaseHsv.TryGetValue(ri.TrackPath, out var value))
		{
			value = new Vector3(200f, 0.3f, 0.6f);
		}
		float x = value.X + m_rand.Float(-6f, 6f);
		float y = MathUtils.Clamp(value.Y + m_rand.Float(-0.05f, 0.05f), 0f, 1f);
		float z = MathUtils.Clamp(value.Z + m_rand.Float(-0.04f, 0.04f), 0f, 1f);
		if (ri.TrackPath == "Music/Radio/Dance of the Black Veil")
		{
			double duration = GetDuration(ri.TrackPath);
			double num = ((duration > 0.001) ? (GetCurrentPositionSeconds(ri) / duration) : 0.0);
			z = MathUtils.Clamp(MathUtils.Lerp(0.18f, 0.06f, (float)num) + m_rand.Float(-0.02f, 0.01f), 0f, 1f);
			y = MathUtils.Clamp(0.1f + m_rand.Float(-0.03f, 0.03f), 0f, 1f);
			x = value.X + m_rand.Float(-3f, 3f);
		}
		return new Color(Color.HsvToRgb(new Vector3(x, y, z)));
	}

	public void StartNoiseAt(Point3 cell)
	{
		StopAt(cell);
		if (string.IsNullOrEmpty(m_noiseTrackPath))
		{
			return;
		}
		try
		{
			StreamingSource streamingSource = ContentManager.Get<StreamingSource>(m_noiseTrackPath).Duplicate();
			long x = 0L;
			streamingSource.Position = MathUtils.Clamp(x, 0L, streamingSource.BytesCount);
			StreamingSound streamingSound = new StreamingSound(streamingSource, Volume, 1f, 0f, isLooped: true, disposeOnStop: false, 1f);
			streamingSound.Volume = 0f;
			streamingSound.Play();
			Vector3 position = new Vector3((float)cell.X + 0.5f, (float)cell.Y + 0.2f, (float)cell.Z + 0.5f);
			RadioInstance value = new RadioInstance
			{
				Sound = streamingSound,
				TrackIndex = 0,
				TrackPath = m_noiseTrackPath,
				Position = position,
				StartGameTime = SubsystemTime.GameTime,
				StartOffset = 0.0,
				Particles = new SoundParticleSystem(SubsystemTerrain, position, Vector3.UnitY),
				NextEmitTime = SubsystemTime.GameTime + 0.1 + (double)m_rand.Float(0f, 0.25f),
				NextNoiseTime = SubsystemTime.GameTime + 0.3 + (double)m_rand.Float(0f, 0.25f),
				IsNoise = true
			};
			m_instances[cell] = value;
		}
		catch
		{
		}
	}
}
