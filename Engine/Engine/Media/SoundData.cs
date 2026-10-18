using System;
using System.IO;
using System.Threading.Tasks;

namespace Engine.Media;

public class SoundData
{
	public int ChannelsCount { get; internal set; }

	public int SamplingFrequency { get; internal set; }

	public short[] Data { get; internal set; }

	public bool IsReady { get; internal set; }

	public SoundData(int channelsCount, int samplingFrequency, int bytesCount)
	{
		if (channelsCount < 1 || channelsCount > 2)
		{
			throw new ArgumentOutOfRangeException("channelsCount");
		}
		if (samplingFrequency < 8000 || samplingFrequency > 48000)
		{
			throw new ArgumentOutOfRangeException("samplingFrequency");
		}
		if (bytesCount < 0)
		{
			throw new ArgumentOutOfRangeException("bytesCount");
		}
		int num = 2 * channelsCount;
		int num2 = bytesCount % num;
		if (num2 != 0)
		{
			bytesCount -= num2;
		}
		ChannelsCount = channelsCount;
		SamplingFrequency = samplingFrequency;
		Data = new short[bytesCount / 2];
		IsReady = true;
	}

	public static SoundFileFormat DetermineFileFormat(string extension)
	{
		if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
		{
			return SoundFileFormat.Wav;
		}
		if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
		{
			return SoundFileFormat.Ogg;
		}
		throw new InvalidOperationException("Unsupported sound file format.");
	}

	public static SoundFileFormat DetermineFileFormat(Stream stream)
	{
		if (Wav.IsWavStream(stream))
		{
			return SoundFileFormat.Wav;
		}
		if (Ogg.IsOggStream(stream))
		{
			return SoundFileFormat.Ogg;
		}
		throw new InvalidOperationException("Unsupported sound file format.");
	}

	public static StreamingSource Stream(Stream stream, SoundFileFormat format)
	{
		return format switch
		{
			SoundFileFormat.Wav => Wav.Stream(stream), 
			SoundFileFormat.Ogg => Ogg.Stream(stream), 
			_ => throw new InvalidOperationException("Unsupported sound file format."), 
		};
	}

	public static StreamingSource Stream(string fileName, SoundFileFormat format)
	{
		using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read);
		return Stream(stream, format);
	}

	public static StreamingSource Stream(Stream stream)
	{
		PeekStream peekStream = new PeekStream(stream, 64);
		SoundFileFormat format = DetermineFileFormat(peekStream.GetInitialBytesStream());
		return Stream(peekStream, format);
	}

	public static StreamingSource Stream(string fileName)
	{
		using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read);
		return Stream(stream);
	}

	public static SoundData Load(Stream stream, SoundFileFormat format)
	{
		switch (format)
		{
		case SoundFileFormat.Wav:
			return Wav.Load(stream);
		default:
			throw new InvalidOperationException("Unsupported sound file format.");
		case SoundFileFormat.Ogg:
		{
			SoundData result = new SoundData();
			result.IsReady = false;
			MemoryStream copiedStream = new MemoryStream();
			stream.Position = 0L;
			stream.CopyTo(copiedStream);
			copiedStream.Position = 0L;
			Task.Run(delegate
			{
				try
				{
					using StreamingSource streamingSource = Ogg.Stream(copiedStream);
					using MemoryStream memoryStream = new MemoryStream();
					byte[] array = new byte[16384];
					int count;
					while ((count = streamingSource.Read(array, 0, array.Length)) > 0)
					{
						memoryStream.Write(array, 0, count);
					}
					byte[] array2 = memoryStream.ToArray();
					int num = array2.Length;
					result.ChannelsCount = streamingSource.ChannelsCount;
					result.SamplingFrequency = streamingSource.SamplingFrequency;
					result.Data = new short[num / 2];
					Buffer.BlockCopy(array2, 0, result.Data, 0, num);
					result.IsReady = true;
				}
				catch (Exception ex)
				{
					Log.Error("Failed async OGG decode: " + ex.Message);
				}
			});
			return result;
		}
		}
	}

	public static SoundData Load(string fileName, SoundFileFormat format)
	{
		using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read);
		return Load(stream, format);
	}

	public static SoundData Load(Stream stream)
	{
		PeekStream peekStream = new PeekStream(stream, 64);
		SoundFileFormat format = DetermineFileFormat(peekStream.GetInitialBytesStream());
		return Load(peekStream, format);
	}

	public static SoundData Load(string fileName)
	{
		using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read);
		return Load(stream);
	}

	public static void Save(SoundData soundData, Stream stream, SoundFileFormat format)
	{
		if (format == SoundFileFormat.Wav)
		{
			Wav.Save(soundData, stream);
			return;
		}
		throw new InvalidOperationException("Unsupported sound file format.");
	}

	public static void Save(SoundData soundData, string fileName, SoundFileFormat format)
	{
		using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create);
		Save(soundData, stream, format);
	}

	public static void StereoToMono(SoundData soundData)
	{
		if (soundData.ChannelsCount != 2)
		{
			throw new InvalidOperationException("SoundData is not stereo.");
		}
		short[] array = new short[soundData.Data.Length / 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (short)((soundData.Data[2 * i] + soundData.Data[2 * i + 1]) / 2);
		}
		soundData.ChannelsCount = 1;
		soundData.Data = array;
	}

	internal SoundData()
	{
	}
}
