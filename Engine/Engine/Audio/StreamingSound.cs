using System;
using System.Threading;
using System.Threading.Tasks;
using Engine.Media;

namespace Engine.Audio;

public sealed class StreamingSound : BaseSound
{
	private Task m_task;

	private ManualResetEvent m_stopTaskEvent = new ManualResetEvent(initialState: false);

	private bool m_noMoreData;

	private float m_bufferDuration;

	public StreamingSource StreamingSource { get; private set; }

	private int ReadStreamingSource(byte[] buffer, int count)
	{
		int num = 0;
		if (StreamingSource.BytesCount > 0)
		{
			while (count > 0)
			{
				int num2 = StreamingSource.Read(buffer, num, count);
				if (num2 > 0)
				{
					num += num2;
					count -= num2;
					continue;
				}
				if (!m_isLooped)
				{
					break;
				}
				StreamingSource.Position = 0L;
			}
		}
		return num;
	}

	private void VerifyStreamingSource(StreamingSource streamingSource)
	{
		if (streamingSource == null)
		{
			throw new ArgumentNullException("streamingSource");
		}
		if (streamingSource.ChannelsCount < 1 || streamingSource.ChannelsCount > 2)
		{
			throw new InvalidOperationException("Unsupported channels count.");
		}
		if (streamingSource.SamplingFrequency < 8000 || streamingSource.SamplingFrequency > 48000)
		{
			throw new InvalidOperationException("Unsupported frequency.");
		}
	}

	public StreamingSound(StreamingSource streamingSource, float volume = 1f, float pitch = 1f, float pan = 0f, bool isLooped = false, bool disposeOnStop = false, float bufferDuration = 0.3f)
	{
		VerifyStreamingSource(streamingSource);
		StreamingSource = streamingSource;
		base.ChannelsCount = streamingSource.ChannelsCount;
		base.SamplingFrequency = streamingSource.SamplingFrequency;
		base.Volume = volume;
		base.Pitch = pitch;
		base.Pan = pan;
		base.IsLooped = isLooped;
		base.DisposeOnStop = disposeOnStop;
		m_bufferDuration = MathUtils.Clamp(bufferDuration, 0.01f, 10f);
	}

	internal override void InternalPlay()
	{
	}

	internal override void InternalPause()
	{
	}

	internal override void InternalStop()
	{
		StreamingSource.Position = 0L;
		m_noMoreData = false;
	}

	internal override void InternalDispose()
	{
		if (m_stopTaskEvent != null && m_task != null)
		{
			m_stopTaskEvent.Set();
			m_task.Wait();
			m_task = null;
			m_stopTaskEvent.Dispose();
			m_stopTaskEvent = null;
		}
		if (StreamingSource != null)
		{
			StreamingSource.Dispose();
			StreamingSource = null;
		}
		base.InternalDispose();
	}

	private void StreamingThreadFunction()
	{
	}
}
