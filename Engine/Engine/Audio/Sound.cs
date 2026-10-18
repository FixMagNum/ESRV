using System;

namespace Engine.Audio;

public sealed class Sound : BaseSound
{
	private SoundBuffer m_soundBuffer;

	public SoundBuffer SoundBuffer => m_soundBuffer;

	public float Duration => 0f;

	public override void Dispose()
	{
		base.Dispose();
		if (m_soundBuffer != null)
		{
			m_soundBuffer.UseCount--;
			m_soundBuffer = null;
		}
	}

	internal void Initialize(SoundBuffer soundBuffer)
	{
		if (soundBuffer == null)
		{
			throw new ArgumentNullException("soundBuffer");
		}
		m_soundBuffer = soundBuffer;
		m_soundBuffer.UseCount++;
	}

	public Sound(SoundBuffer soundBuffer, float volume = 1f, float pitch = 1f, float pan = 0f, bool isLooped = false, bool disposeOnStop = false)
	{
		if (soundBuffer == null)
		{
			throw new ArgumentNullException("soundBuffer");
		}
		Initialize(soundBuffer);
		base.ChannelsCount = soundBuffer.ChannelsCount;
		base.SamplingFrequency = soundBuffer.SamplingFrequency;
		base.Volume = volume;
		base.Pitch = pitch;
		base.Pan = pan;
		base.IsLooped = isLooped;
		base.DisposeOnStop = disposeOnStop;
		Mixer.m_soundsToStopPoll.Add(this);
	}

	internal override void InternalPlay()
	{
	}

	internal override void InternalPause()
	{
	}

	internal override void InternalStop()
	{
	}

	internal override void InternalDispose()
	{
		base.InternalDispose();
		Mixer.m_soundsToStopPoll.Remove(this);
	}
}
