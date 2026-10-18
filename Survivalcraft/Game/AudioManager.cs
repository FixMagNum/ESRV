using Engine;
using Engine.Audio;

namespace Game;

public static class AudioManager
{
	public static float MinAudibleVolume => 0.05f * SettingsManager.SoundsVolume;

	public static void PlaySound(string name, float volume, float pitch, float pan)
	{
		if (SettingsManager.SoundsVolume > 0f && volume * SettingsManager.SoundsVolume > MinAudibleVolume)
		{
			try
			{
				new Sound(ContentManager.Get<SoundBuffer>(name), volume * SettingsManager.SoundsVolume, ToEnginePitch(pitch), pan, isLooped: false, disposeOnStop: true).Play();
			}
			catch
			{
			}
		}
	}

	public static float ToEnginePitch(float pitch)
	{
		return MathUtils.Pow(2f, pitch);
	}
}
