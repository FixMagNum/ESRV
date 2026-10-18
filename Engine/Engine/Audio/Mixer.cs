using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.Audio;

public static class Mixer
{
	private static float m_masterVolume = 1f;

	private static List<Sound> m_soundsToStop = new List<Sound>();

	internal static HashSet<Sound> m_soundsToStopPoll = new HashSet<Sound>();

	public static float MasterVolume
	{
		get
		{
			return m_masterVolume;
		}
		set
		{
			value = MathUtils.Saturate(value);
			if (value != m_masterVolume)
			{
				m_masterVolume = value;
				InternalSetMasterVolume(value);
			}
		}
	}

	internal static void Initialize()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
		string fullPath = Path.GetFullPath(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory));
		string path = (Environment.Is64BitProcess ? "x64" : "x86");
		string text = Path.Combine(fullPath, "OpenAL", path);
		Environment.SetEnvironmentVariable("PATH", text + ";" + environmentVariable, EnvironmentVariableTarget.Process);
	}

	internal static void Dispose()
	{
	}

	internal static void BeforeFrame()
	{
	}

	internal static void AfterFrame()
	{
	}

	internal static void InternalSetMasterVolume(float volume)
	{
	}

	internal static void CheckALError()
	{
	}
}
