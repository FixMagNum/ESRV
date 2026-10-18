using System;
using Engine;
using Engine.Audio;
using Engine.Media;

namespace Game;

public static class MusicManager
{
	public enum Mix
	{
		None,
		Menu
	}

	public const float m_fadeSpeed = 0.33f;

	public const float m_fadeWait = 2f;

	public static StreamingSound m_fadeSound;

	public static StreamingSound m_sound;

	public static double m_fadeStartTime;

	public static Mix m_currentMix;

	public static double m_nextSongTime;

	public static Random m_random;

	public static readonly string[] m_menuTracks;

	public static Mix CurrentMix
	{
		get
		{
			return m_currentMix;
		}
		set
		{
			if (value != m_currentMix)
			{
				m_currentMix = value;
				m_nextSongTime = 0.0;
			}
		}
	}

	public static bool IsPlaying
	{
		get
		{
			if (m_sound != null)
			{
				return m_sound.State > SoundState.Stopped;
			}
			return false;
		}
	}

	public static float Volume => SettingsManager.MusicVolume * 0.6f;

	public static void Update()
	{
		if (Volume <= 0f)
		{
			if (m_sound != null || m_fadeSound != null)
			{
				StopMusic();
			}
			return;
		}
		if (m_fadeSound != null)
		{
			m_fadeSound.Volume = MathUtils.Min(m_fadeSound.Volume - 0.33f * Volume * Time.FrameDuration, Volume);
			if (m_fadeSound.Volume <= 0f)
			{
				m_fadeSound.Dispose();
				m_fadeSound = null;
			}
		}
		if (m_sound != null && Time.FrameStartTime >= m_fadeStartTime)
		{
			m_sound.Volume = MathUtils.Min(m_sound.Volume + 0.33f * Volume * Time.FrameDuration, Volume);
		}
		if (m_currentMix == Mix.None)
		{
			StopMusic();
		}
		else if (m_currentMix == Mix.Menu && !IsPlaying && Time.FrameStartTime >= m_nextSongTime && m_menuTracks != null && m_menuTracks.Length != 0)
		{
			int num = m_menuTracks.Length - 1;
			if (num >= 0)
			{
				int num2 = m_random.Int(0, num);
				PlayMusic(m_menuTracks[num2], 0f);
				m_nextSongTime = Time.FrameStartTime + 5.0;
			}
		}
	}

	public static void PlayMusic(string name, float startPercentage)
	{
		if (Volume <= 0f)
		{
			StopMusic();
			return;
		}
		if (string.IsNullOrEmpty(name))
		{
			StopMusic();
			return;
		}
		try
		{
			StopMusic();
			m_fadeStartTime = Time.FrameStartTime + 2.0;
			float volume = ((m_fadeSound != null) ? 0f : Volume);
			StreamingSource streamingSource = (ContentManager.Get<StreamingSource>(name) ?? throw new InvalidOperationException($"Music source \"{name}\" not found or not loaded.")).Duplicate();
			if (streamingSource == null)
			{
				throw new InvalidOperationException($"Music source \"{name}\" duplicate returned null.");
			}
			if (streamingSource.ChannelsCount <= 0)
			{
				throw new InvalidOperationException($"Music source \"{name}\" has invalid ChannelsCount={streamingSource.ChannelsCount}.");
			}
			long num = streamingSource.BytesCount / streamingSource.ChannelsCount / 2;
			long num2 = (long)(MathUtils.Saturate(startPercentage) * (float)num);
			num2 = num2 / 16 * 16;
			streamingSource.Position = num2;
			m_sound = new StreamingSound(streamingSource, volume, 1f, 0f, isLooped: false, disposeOnStop: false, 1f);
			m_sound.Play();
		}
		catch (Exception ex)
		{
			Log.Warning("Error playing music \"{0}\". {1}", name, ex.Message);
		}
	}

	public static void StopMusic()
	{
		if (m_sound != null)
		{
			if (m_fadeSound != null)
			{
				m_fadeSound.Dispose();
			}
			m_fadeSound = m_sound;
			m_sound = null;
		}
	}

	static MusicManager()
	{
		m_menuTracks = new string[10] { "Music/Embrace/Embrace slow (1)", "Music/Embrace/Embrace slow (2)", "Music/Embrace/Embrace slow (3)", "Music/Embrace/Embrace slow (4)", "Music/Embrace/Embrace slow (5)", "Music/Embrace/Embrace slow (6)", "Music/Embrace/Embrace slow (7)", "Music/Embrace/Embrace slow (8)", "Music/Embrace/Embrace slow (9)", "Music/Embrace/Embrace slow (10)" };
		m_random = new Random();
	}
}
