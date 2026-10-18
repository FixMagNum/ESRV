using System;
using System.Collections.Generic;
using Engine;
using Engine.Audio;
using Game.NetWork;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemAudio : Subsystem, IUpdateable
{
	public class Congestion
	{
		public double LastUpdateTime;

		public double LastPlayedTime;

		public float LastPlayedVolume;

		public float Value;
	}

	public struct SoundInfo
	{
		public double Time;

		public string Name;

		public float Volume;

		public float Pitch;

		public float Pan;
	}

	public SubsystemTime m_subsystemTime;

	public SubsystemGameWidgets m_subsystemViews;

	public Random m_random = new Random();

	public List<Vector3> m_listenerPositions = new List<Vector3>();

	public Dictionary<string, Congestion> m_congestions = new Dictionary<string, Congestion>();

	public double m_nextSoundTime;

	public List<SoundInfo> m_queuedSounds = new List<SoundInfo>();

	public List<Sound> m_sounds = new List<Sound>();

	public Dictionary<Sound, bool> m_mutedSounds = new Dictionary<Sound, bool>();

	public SubsystemGameInfo m_subsystemGameInfo;

	public ReadOnlyList<Vector3> ListenerPositions => new ReadOnlyList<Vector3>(m_listenerPositions);

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public float CalculateListenerDistanceSquared(Vector3 p)
	{
		float num = float.MaxValue;
		for (int i = 0; i < m_listenerPositions.Count; i++)
		{
			float num2 = Vector3.DistanceSquared(m_listenerPositions[i], p);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	public float CalculateListenerDistance(Vector3 p)
	{
		return MathUtils.Sqrt(CalculateListenerDistanceSquared(p));
	}

	public void Mute()
	{
		foreach (Sound sound in m_sounds)
		{
			if (sound.State == SoundState.Playing)
			{
				m_mutedSounds[sound] = true;
				sound.Pause();
			}
		}
	}

	public void Unmute()
	{
		foreach (Sound key in m_mutedSounds.Keys)
		{
			key.Play();
		}
		m_mutedSounds.Clear();
	}

	public void PlaySound(string name, float volume, float pitch, float pan, float delay)
	{
		bool num = m_subsystemGameInfo != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Embrace;
		bool flag = IsCreatureSound(name);
		float echoIntensity = ((!num || !flag) ? ComputeEchoIntensity() : 0f);
		double num2 = m_subsystemTime.GameTime + (double)delay;
		m_nextSoundTime = MathUtils.Min(m_nextSoundTime, num2);
		m_queuedSounds.Add(new SoundInfo
		{
			Time = num2,
			Name = name,
			Volume = volume,
			Pitch = pitch,
			Pan = pan
		});
		if (num & flag)
		{
			ApplyEmbraceCreatureVoices(name, volume, pitch, pan, delay);
		}
		else
		{
			ApplyEchoEffect(name, volume, pitch, pan, delay, echoIntensity);
		}
	}

	public void PlaySound(string name, float volume, float pitch, Vector3 position, float minDistance, float delay)
	{
		float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
		PlaySound(name, volume * num, pitch, 0f, delay);
	}

	public void PlaySound(string name, float volume, float pitch, Vector3 position, float minDistance, bool autoDelay)
	{
		float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
		PlaySound(name, volume * num, pitch, 0f, autoDelay ? CalculateDelay(position) : 0f);
	}

	public void PlayRandomSound(string directory, float volume, float pitch, float pan, float delay)
	{
		ReadOnlyList<ContentInfo> readOnlyList = ContentManager.List(directory);
		if (readOnlyList.Count > 0)
		{
			int index = m_random.Int(0, readOnlyList.Count - 1);
			PlaySound(readOnlyList[index].ContentPath, volume, pitch, pan, delay);
		}
		else
		{
			Log.Warning("Sounds directory \"{0}\" not found or empty.", directory);
		}
	}

	public void PlayRandomSound(string directory, float volume, float pitch, Vector3 position, float minDistance, float delay)
	{
		float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
		PlayRandomSound(directory, volume * num, pitch, 0f, delay);
	}

	public void PlayRandomSound(string directory, float volume, float pitch, Vector3 position, float minDistance, bool autoDelay)
	{
		float num = CalculateVolume(CalculateListenerDistance(position), minDistance);
		PlayRandomSound(directory, volume * num, pitch, 0f, autoDelay ? CalculateDelay(position) : 0f);
	}

	public Sound CreateSound(string name)
	{
		Sound sound = new Sound(ContentManager.Get<SoundBuffer>(name));
		m_sounds.Add(sound);
		return sound;
	}

	public float CalculateVolume(float distance, float minDistance, float rolloffFactor = 2f)
	{
		if (distance > minDistance)
		{
			return minDistance / (minDistance + MathUtils.Max(rolloffFactor * (distance - minDistance), 0f));
		}
		return 1f;
	}

	public float CalculateDelay(Vector3 position)
	{
		return CalculateDelay(CalculateListenerDistance(position));
	}

	public float CalculateDelay(float distance)
	{
		return MathUtils.Min(distance / 100f, 5f);
	}

	public void Update(float dt)
	{
		m_listenerPositions.Clear();
		if (CommonLib.WorkType == WorkType.Client)
		{
			foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
			{
				if (gameWidget.PlayerData.IsMainPlayer)
				{
					m_listenerPositions.Add(gameWidget.ActiveCamera.ViewPosition);
				}
			}
		}
		else
		{
			foreach (GameWidget gameWidget2 in m_subsystemViews.GameWidgets)
			{
				m_listenerPositions.Add(gameWidget2.ActiveCamera.ViewPosition);
			}
		}
		if (m_subsystemTime.GameTime < m_nextSoundTime)
		{
			return;
		}
		m_nextSoundTime = double.MaxValue;
		int num = 0;
		while (num < m_queuedSounds.Count)
		{
			SoundInfo soundInfo = m_queuedSounds[num];
			if (m_subsystemTime.GameTime >= soundInfo.Time)
			{
				if (m_subsystemTime.GameTimeFactor == 1f && !m_subsystemTime.FixedTimeStep.HasValue && soundInfo.Volume * SettingsManager.SoundsVolume > AudioManager.MinAudibleVolume && UpdateCongestion(soundInfo.Name, soundInfo.Volume))
				{
					AudioManager.PlaySound(soundInfo.Name, soundInfo.Volume, soundInfo.Pitch, soundInfo.Pan);
				}
				m_queuedSounds.RemoveAt(num);
			}
			else
			{
				m_nextSoundTime = MathUtils.Min(m_nextSoundTime, soundInfo.Time);
				num++;
			}
		}
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
	}

	public override void Dispose()
	{
		foreach (Sound sound in m_sounds)
		{
			sound.Dispose();
		}
	}

	public bool UpdateCongestion(string name, float volume)
	{
		if (!m_congestions.TryGetValue(name, out var value))
		{
			value = new Congestion();
			m_congestions.Add(name, value);
		}
		double value2 = Time.RealTime;
		double lastUpdateTime = value.LastUpdateTime;
		double lastPlayedTime = value.LastPlayedTime;
		float num = ((lastUpdateTime > 0.0) ? ((float)(value2 - lastUpdateTime)) : 0f);
		value.Value = MathUtils.Max(value.Value - 10f * num, 0f);
		value.LastUpdateTime = value2;
		if (value.Value <= 6f && (lastPlayedTime == 0.0 || volume > value.LastPlayedVolume || value2 - lastPlayedTime >= 0.0))
		{
			value.LastPlayedTime = value2;
			value.LastPlayedVolume = volume;
			value.Value++;
			return true;
		}
		return false;
	}

	private float ComputeEchoIntensity()
	{
		float result = 0f;
		if (m_listenerPositions.Count > 0)
		{
			float y = m_listenerPositions[0].Y;
			if (y <= 55f)
			{
				result = ((!(y >= 45f)) ? 1f : ((55f - y) / 10f));
			}
		}
		return result;
	}

	private void ApplyEchoEffect(string name, float volume, float pitch, float pan, float delay, float echoIntensity)
	{
		if (echoIntensity > 0f)
		{
			int num = 2;
			float num2 = 0.3f;
			for (int i = 1; i <= num; i++)
			{
				float num3 = delay + (float)i * num2;
				float num4 = MathUtils.Lerp(0.3f, 0f, (float)i / (float)num);
				float volume2 = volume * num4 * echoIntensity;
				double num5 = m_subsystemTime.GameTime + (double)num3;
				m_nextSoundTime = MathUtils.Min(m_nextSoundTime, num5);
				m_queuedSounds.Add(new SoundInfo
				{
					Time = num5,
					Name = name,
					Volume = volume2,
					Pitch = pitch,
					Pan = pan
				});
			}
		}
	}

	private bool IsCreatureSound(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (name.IndexOf("Creatures/", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (name.IndexOf("Creatures\\", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		return false;
	}

	private void ApplyEmbraceCreatureVoices(string name, float volume, float pitch, float pan, float delay)
	{
		int num = 4;
		float x = 0.7f;
		float x2 = 1.4f;
		for (int i = 0; i < num; i++)
		{
			float f = ((num > 1) ? ((float)i / (float)(num - 1)) : 0f);
			float num2 = MathUtils.Lerp(x2, x, f);
			float num3 = pitch * num2;
			float num4 = delay + 0.02f * (float)(i + 1);
			float num5 = MathUtils.Lerp(0.7f, 0.3f, f);
			float num6 = volume * num5;
			float pan2 = pan;
			if (i == num - 1)
			{
				num3 *= 0.6f;
				num4 = delay + 0.12f;
				num6 *= 0.9f;
				pan2 = MathUtils.Clamp(pan * -1.3f, -1f, 1f);
			}
			double num7 = m_subsystemTime.GameTime + (double)num4;
			m_nextSoundTime = MathUtils.Min(m_nextSoundTime, num7);
			m_queuedSounds.Add(new SoundInfo
			{
				Time = num7,
				Name = name,
				Volume = num6,
				Pitch = num3,
				Pan = pan2
			});
		}
	}
}
