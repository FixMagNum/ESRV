using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Engine.Media;
using Game.NetWork;
using Game.NetWork.Packages;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemAmbientMusic : Subsystem, IUpdateable
{
	private readonly List<string> m_musicTracks;

	private Random m_random;

	private StreamingSound m_currentSound;

	private readonly Dictionary<string, double> m_trackDurations;

	private string m_currentTrackName;

	private double m_currentTrackPosition;

	private bool m_resumePending;

	private SubsystemRadio m_subsystemRadio;

	private float m_radioDuck;

	private SubsystemGameInfo m_gameInfo;

	private readonly List<string> m_embraceSlowTracks;

	private readonly List<string> m_embraceEpicTracks;

	private readonly string m_embraceBossTrack;

	private StreamingSound m_embraceFightSound;

	private StreamingSound m_embraceBossSound;

	private bool m_embraceOverrideActive;

	private bool m_isEmbraceFightActive;

	private bool m_isEmbraceBossActive;

	public SubsystemTime SubsystemTime { get; private set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public double MusicStartTime { get; private set; }

	public double MusicEndTime { get; private set; }

	private float Volume => MathUtils.Clamp(SettingsManager.MusicVolume / 10f * 4f, 0f, 1f);

	public bool IsPlaying
	{
		get
		{
			if (m_currentSound != null)
			{
				return m_currentSound.State > SoundState.Stopped;
			}
			return false;
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		SubsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_gameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: false);
		m_subsystemRadio = Project.FindSubsystem<SubsystemRadio>(throwOnError: false);
		m_random = new Random();
		ValuesDictionary valuesDictionary2 = valuesDictionary.GetValue<ValuesDictionary>("AmbientMusic", null) ?? new ValuesDictionary();
		MusicStartTime = Math.Round(valuesDictionary2.GetValue("MusicStartTime", 0.0), 3);
		MusicEndTime = Math.Round(valuesDictionary2.GetValue("MusicEndTime", 0.0), 3);
		m_currentTrackName = valuesDictionary2.GetValue("CurrentTrackName", string.Empty);
		m_currentTrackPosition = valuesDictionary2.GetValue("CurrentTrackPosition", 0.0);
		if (m_trackDurations.TryGetValue(m_currentTrackName, out var value))
		{
			m_currentTrackPosition = MathUtils.Clamp(m_currentTrackPosition, 0.0, value - 0.001);
		}
		m_resumePending = !string.IsNullOrEmpty(m_currentTrackName);
		if (CommonLib.WorkType != WorkType.Client && !m_resumePending && (SubsystemTime.GameTime >= MusicEndTime || MusicEndTime == 0.0))
		{
			ScheduleIdleDelay();
		}
		ScreensManager.OnEnterScreen += OnScreenEnter;
		m_radioDuck = 0f;
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("AmbientMusic", valuesDictionary2);
		double value = m_currentTrackPosition;
		if (m_currentSound != null && m_currentSound.State == SoundState.Playing)
		{
			value = m_currentSound.CurrentPosition;
		}
		valuesDictionary2.SetValue("MusicStartTime", Math.Round(MusicStartTime, 3));
		valuesDictionary2.SetValue("MusicEndTime", Math.Round(MusicEndTime, 3));
		valuesDictionary2.SetValue("CurrentTrackName", m_currentTrackName ?? string.Empty);
		valuesDictionary2.SetValue("CurrentTrackPosition", Math.Round(value, 3));
	}

	public void Update(float dt)
	{
		float x = ((m_subsystemRadio != null) ? m_subsystemRadio.GetListenerPresenceFactor() : 0f);
		m_radioDuck = MathUtils.Lerp(m_radioDuck, x, MathUtils.Saturate(3f * Time.FrameDuration));
		float volume = Volume * (1f - m_radioDuck);
		bool flag = ScreensManager.CurrentScreen is GameScreen;
		if ((m_resumePending & flag) && Volume > 0f && !m_embraceOverrideActive)
		{
			m_resumePending = false;
			PlayTrack(m_currentTrackName, m_currentTrackPosition, isServerRequest: false);
		}
		if (flag)
		{
			if (m_currentSound != null && m_currentSound.State == SoundState.Playing)
			{
				m_currentSound.Volume = volume;
			}
			if (m_embraceFightSound != null && m_embraceFightSound.State == SoundState.Playing)
			{
				m_embraceFightSound.Volume = volume;
			}
			if (m_embraceBossSound != null && m_embraceBossSound.State == SoundState.Playing)
			{
				m_embraceBossSound.Volume = volume;
			}
			if (m_currentSound != null && m_currentSound.State == SoundState.Paused)
			{
				m_currentSound.Volume = volume;
				m_currentSound.Play();
			}
			if (m_embraceFightSound != null && m_embraceFightSound.State == SoundState.Paused)
			{
				m_embraceFightSound.Volume = volume;
				m_embraceFightSound.Play();
			}
			if (m_embraceBossSound != null && m_embraceBossSound.State == SoundState.Paused)
			{
				m_embraceBossSound.Volume = volume;
				m_embraceBossSound.Play();
			}
			if (m_embraceFightSound != null && m_embraceFightSound.State == SoundState.Stopped)
			{
				m_embraceFightSound.Dispose();
				m_embraceFightSound = null;
				m_isEmbraceFightActive = false;
				if (!m_isEmbraceBossActive)
				{
					m_embraceOverrideActive = false;
					if (CommonLib.WorkType != WorkType.Client)
					{
						ScheduleIdleDelay();
					}
				}
			}
			if (m_embraceBossSound != null && m_embraceBossSound.State == SoundState.Stopped)
			{
				m_embraceBossSound.Dispose();
				m_embraceBossSound = null;
				m_isEmbraceBossActive = false;
				m_embraceOverrideActive = false;
				if (CommonLib.WorkType != WorkType.Client)
				{
					ScheduleIdleDelay();
				}
			}
			if (m_currentSound != null && m_currentSound.State == SoundState.Stopped)
			{
				StopMusic(true);
				if (CommonLib.WorkType != WorkType.Client)
				{
					ScheduleIdleDelay();
				}
			}
			if (CommonLib.WorkType != WorkType.Client && !m_embraceOverrideActive && !IsPlaying && SubsystemTime.GameTime >= MusicEndTime)
			{
				PlayRandomTrack();
			}
			if (m_currentSound != null && m_currentSound.State == SoundState.Playing)
			{
				m_currentTrackPosition = m_currentSound.CurrentPosition;
			}
		}
		else
		{
			if (m_currentSound != null)
			{
				m_currentSound.Pause();
				m_currentSound.Volume = 0f;
			}
			if (m_embraceFightSound != null)
			{
				m_embraceFightSound.Pause();
				m_embraceFightSound.Volume = 0f;
			}
			if (m_embraceBossSound != null)
			{
				m_embraceBossSound.Pause();
				m_embraceBossSound.Volume = 0f;
			}
		}
	}

	public void PlayRandomTrack()
	{
		if (CommonLib.WorkType != WorkType.Client && !m_embraceOverrideActive)
		{
			List<string> ambientTracks = GetAmbientTracks();
			if (ambientTracks != null && ambientTracks.Count != 0 && !IsPlaying)
			{
				string trackName = ambientTracks[m_random.Int(0, ambientTracks.Count - 1)];
				PlayTrack(trackName, 0.0, isServerRequest: true);
			}
		}
	}

	public SubsystemAmbientMusic()
	{
		m_trackDurations = new Dictionary<string, double>
		{
			{ "Music/NativeAmericanFluteSpirit", 129.0 },
			{ "Music/AloneForever", 128.0 },
			{ "Music/NativeAmerican", 142.0 },
			{ "Music/NativeAmericanHeart", 122.0 },
			{ "Music/NativeAmericanPeaceFlute", 126.0 }
		};
		m_currentTrackName = string.Empty;
		m_embraceSlowTracks = new List<string> { "Music/Embrace/Embrace slow (1)", "Music/Embrace/Embrace slow (2)", "Music/Embrace/Embrace slow (3)", "Music/Embrace/Embrace slow (4)", "Music/Embrace/Embrace slow (5)", "Music/Embrace/Embrace slow (6)", "Music/Embrace/Embrace slow (7)", "Music/Embrace/Embrace slow (8)", "Music/Embrace/Embrace slow (9)", "Music/Embrace/Embrace slow (10)" };
		m_embraceEpicTracks = new List<string>();
		m_embraceBossTrack = string.Empty;
		m_musicTracks = new List<string> { "Music/NativeAmericanFluteSpirit", "Music/AloneForever", "Music/NativeAmerican", "Music/NativeAmericanHeart", "Music/NativeAmericanPeaceFlute" };
	}

	public void StopMusic()
	{
		if (m_currentSound != null)
		{
			m_currentSound.Stop();
			m_currentSound.Dispose();
			m_currentSound = null;
		}
	}

	public void PlayTrack(string trackName, double startTime)
	{
		PlayTrack(trackName, startTime, isServerRequest: true);
	}

	private void ScheduleNextMusic(string trackName, double startSeconds)
	{
		MusicStartTime = Math.Round(SubsystemTime.GameTime - startSeconds, 3);
		if (!m_trackDurations.TryGetValue(trackName, out var value))
		{
			value = 180.0;
		}
		MusicEndTime = Math.Round(MusicStartTime + value, 3);
	}

	private void ScheduleIdleDelay()
	{
		MusicStartTime = Math.Round(SubsystemTime.GameTime, 3);
		int num;
		int num2;
		if (m_gameInfo != null)
		{
			num = ((m_gameInfo.WorldSettings.GameMode == GameMode.Embrace) ? 1 : 0);
			if (num != 0)
			{
				num2 = 400;
			}
			else
			{
				num2 = 800;
			}
		}
		else
		{
			num = 0;
			num2 = 800;
		}
		int min = num2;
		int max = ((num != 0) ? 850 : 1700);
		MusicEndTime = Math.Round(MusicStartTime + (double)m_random.Int(min, max), 3);
	}

	private void StopMusic(bool resetInfo = true)
	{
		if (m_currentSound != null)
		{
			m_currentSound.Stop();
			m_currentSound.Dispose();
			m_currentSound = null;
		}
		if (resetInfo)
		{
			m_currentTrackName = string.Empty;
			m_currentTrackPosition = 0.0;
		}
	}

	private void ClearTrackInfo()
	{
		m_currentTrackName = string.Empty;
		m_currentTrackPosition = 0.0;
	}

	private static void EnsureKey<T>(ValuesDictionary d, string key, T defVal)
	{
		if (!d.ContainsKey(key))
		{
			d.SetValue(key, defVal);
		}
	}

	private void OnScreenEnter()
	{
		if (!(ScreensManager.CurrentScreen is GameScreen) && m_currentSound != null)
		{
			m_currentSound.Pause();
			m_currentSound.Volume = 0f;
		}
	}

	private List<string> GetAmbientTracks()
	{
		if (m_gameInfo != null && m_gameInfo.WorldSettings.GameMode == GameMode.Embrace)
		{
			return m_embraceSlowTracks;
		}
		return m_musicTracks;
	}

	public void StartEmbraceFightMusic()
	{
		if (m_gameInfo == null || m_gameInfo.WorldSettings.GameMode != GameMode.Embrace || m_embraceEpicTracks.Count == 0)
		{
			return;
		}
		StopEmbraceBossMusic();
		StopMusic();
		m_embraceOverrideActive = true;
		int num = m_random.Int(0, m_embraceEpicTracks.Count);
		if (num >= m_embraceEpicTracks.Count)
		{
			num = m_embraceEpicTracks.Count - 1;
		}
		string name = m_embraceEpicTracks[num];
		try
		{
			if (m_embraceFightSound != null)
			{
				m_embraceFightSound.Stop();
				m_embraceFightSound.Dispose();
				m_embraceFightSound = null;
			}
			StreamingSource streamingSource = ContentManager.Get<StreamingSource>(name).Duplicate();
			m_embraceFightSound = new StreamingSound(streamingSource, Volume, 1f, 0f, isLooped: true, disposeOnStop: false, 1f);
			m_embraceFightSound.Volume = Volume;
			m_embraceFightSound.Play();
			m_isEmbraceFightActive = true;
		}
		catch
		{
		}
	}

	public void StopEmbraceFightMusic()
	{
	}

	public void StartEmbraceBossMusic()
	{
	}

	public void StopEmbraceBossMusic()
	{
		if (m_embraceBossSound != null)
		{
			m_embraceBossSound.Stop();
			m_embraceBossSound.Dispose();
			m_embraceBossSound = null;
		}
		m_isEmbraceBossActive = false;
		m_embraceOverrideActive = false;
		ScheduleIdleDelay();
	}

	public void PlayTrack(string trackName, double startTime, bool isServerRequest)
	{
		StopMusic(resetInfo: false);
		if (isServerRequest && CommonLib.WorkType == WorkType.Server)
		{
			MusicStartTime = SubsystemTime.GameTime - startTime;
			CommonLib.Net.QueuePackage(new AmbientMusicPackage(trackName, MusicStartTime));
		}
		if (!m_trackDurations.TryGetValue(trackName, out var value))
		{
			value = 180.0;
		}
		startTime = MathUtils.Clamp(startTime, 0.0, value - 0.001);
		try
		{
			StreamingSource streamingSource = ContentManager.Get<StreamingSource>(trackName).Duplicate();
			long x = (long)(startTime * (double)streamingSource.SamplingFrequency) * 2 * streamingSource.ChannelsCount;
			x = MathUtils.Clamp(x, 0L, streamingSource.BytesCount);
			streamingSource.Position = x;
			m_currentSound = new StreamingSound(streamingSource, Volume, 1f, 0f, isLooped: false, disposeOnStop: false, 1f);
			m_currentSound.Volume = Volume;
			m_currentSound.Play();
			m_currentTrackName = trackName;
			m_currentTrackPosition = startTime;
			ScheduleNextMusic(trackName, startTime);
		}
		catch (Exception ex)
		{
			Log.Error("Failed to play ambient music '{0}': {1}", trackName, ex.Message);
		}
	}

	public void SyncMusicFromPackage(string trackName, double musicGlobalStartTime)
	{
		if (CommonLib.WorkType != WorkType.Server)
		{
			MusicStartTime = musicGlobalStartTime;
			double num = SubsystemTime.GameTime - musicGlobalStartTime;
			if ((!m_trackDurations.TryGetValue(trackName, out var value) || !(num >= value)) && (!(m_currentTrackName == trackName) || !IsPlaying || !(Math.Abs((double)m_currentSound.CurrentPosition - num) < 0.5)))
			{
				PlayTrack(trackName, Math.Max(0.0, num), isServerRequest: false);
			}
		}
	}
}
