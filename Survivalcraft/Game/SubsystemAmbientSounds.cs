using Engine;
using Engine.Audio;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemAmbientSounds : Subsystem, IUpdateable
{
	public Sound m_fireSound;

	public Sound m_waterSound;

	public Sound m_magmaSound;

	public Random m_random = new Random();

	private Sound m_blazeSound;

	private Sound m_clayfurnaceSound;

	private Sound m_brickfurnaceSound;

	public Sound m_cricketSound1;

	public Sound m_cricketSound2;

	public Sound m_cricketSound3;

	public Sound m_cricketSound4;

	private Sound m_fermentationSound;

	private Sound m_electricHumSound;

	private Sound m_riverCalmSound;

	private Sound m_riverFightSound;

	private Sound m_riverWhirlpoolSound;

	private Sound m_oceanSurfSound;

	private Sound m_oceanRoughSound;

	private Sound m_oceanStormSound;

	public SubsystemAudio SubsystemAudio { get; set; }

	public float FireSoundVolume { get; set; }

	public float WaterSoundVolume { get; set; }

	public float MagmaSoundVolume { get; set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public float BlazeSoundVolume { get; set; }

	public float ClayFurnaceSoundVolume { get; set; }

	public float BrickFurnaceSoundVolume { get; set; }

	public float CricketSoundVolume1 { get; set; }

	public float CricketSoundVolume2 { get; set; }

	public float CricketSoundVolume3 { get; set; }

	public float CricketSoundVolume4 { get; set; }

	public float FermentationSoundVolume { get; set; }

	public float ElectricHumSoundVolume { get; set; }

	public float RiverCalmVolume { get; set; }

	public float RiverFightVolume { get; set; }

	public float RiverWhirlpoolVolume { get; set; }

	public float OceanSurfVolume { get; set; }

	public float OceanRoughVolume { get; set; }

	public float OceanStormVolume { get; set; }

	public void Update(float dt)
	{
		m_fireSound.Volume = MathUtils.Lerp(m_fireSound.Volume, SettingsManager.SoundsVolume * FireSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_fireSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_fireSound.Play();
		}
		else
		{
			m_fireSound.Pause();
		}
		m_riverCalmSound.Volume = MathUtils.Lerp(m_riverCalmSound.Volume, SettingsManager.SoundsVolume * RiverCalmVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_riverCalmSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_riverCalmSound.Play();
		}
		else
		{
			m_riverCalmSound.Pause();
		}
		m_riverFightSound.Volume = MathUtils.Lerp(m_riverFightSound.Volume, SettingsManager.SoundsVolume * RiverFightVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_riverFightSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_riverFightSound.Play();
		}
		else
		{
			m_riverFightSound.Pause();
		}
		m_riverWhirlpoolSound.Volume = MathUtils.Lerp(m_riverWhirlpoolSound.Volume, SettingsManager.SoundsVolume * RiverWhirlpoolVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_riverWhirlpoolSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_riverWhirlpoolSound.Play();
		}
		else
		{
			m_riverWhirlpoolSound.Pause();
		}
		m_oceanSurfSound.Volume = MathUtils.Lerp(m_oceanSurfSound.Volume, SettingsManager.SoundsVolume * OceanSurfVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_oceanSurfSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_oceanSurfSound.Play();
		}
		else
		{
			m_oceanSurfSound.Pause();
		}
		m_oceanRoughSound.Volume = MathUtils.Lerp(m_oceanRoughSound.Volume, SettingsManager.SoundsVolume * OceanRoughVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_oceanRoughSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_oceanRoughSound.Play();
		}
		else
		{
			m_oceanRoughSound.Pause();
		}
		m_oceanStormSound.Volume = MathUtils.Lerp(m_oceanStormSound.Volume, SettingsManager.SoundsVolume * OceanStormVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_oceanStormSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_oceanStormSound.Play();
		}
		else
		{
			m_oceanStormSound.Pause();
		}
		m_waterSound.Volume = MathUtils.Lerp(m_waterSound.Volume, SettingsManager.SoundsVolume * WaterSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_waterSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_waterSound.Play();
		}
		else
		{
			m_waterSound.Pause();
		}
		m_magmaSound.Volume = MathUtils.Lerp(m_magmaSound.Volume, SettingsManager.SoundsVolume * MagmaSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_magmaSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_magmaSound.Play();
		}
		else
		{
			m_magmaSound.Pause();
		}
		m_blazeSound.Volume = MathUtils.Lerp(m_blazeSound.Volume, SettingsManager.SoundsVolume * BlazeSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_blazeSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_blazeSound.Play();
		}
		else
		{
			m_blazeSound.Pause();
		}
		m_clayfurnaceSound.Volume = MathUtils.Lerp(m_clayfurnaceSound.Volume, SettingsManager.SoundsVolume * ClayFurnaceSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_clayfurnaceSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_clayfurnaceSound.Play();
		}
		else
		{
			m_clayfurnaceSound.Pause();
		}
		m_brickfurnaceSound.Volume = MathUtils.Lerp(m_brickfurnaceSound.Volume, SettingsManager.SoundsVolume * BrickFurnaceSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_brickfurnaceSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_brickfurnaceSound.Play();
		}
		else
		{
			m_brickfurnaceSound.Pause();
		}
		m_fermentationSound.Volume = MathUtils.Lerp(m_fermentationSound.Volume, SettingsManager.SoundsVolume * FermentationSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_fermentationSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_fermentationSound.Play();
		}
		else
		{
			m_fermentationSound.Pause();
		}
		if (m_magmaSound.State == SoundState.Playing && m_random.Bool(0.2f * dt))
		{
			SubsystemAudio.PlayRandomSound("Audio/Sizzles", m_magmaSound.Volume, m_random.Float(-0.2f, 0.2f), 0f, 0f);
		}
		m_cricketSound1.Volume = MathUtils.Lerp(m_cricketSound1.Volume, SettingsManager.SoundsVolume * CricketSoundVolume1, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_cricketSound1.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_cricketSound1.Play();
		}
		else
		{
			m_cricketSound1.Pause();
		}
		m_cricketSound2.Volume = MathUtils.Lerp(m_cricketSound2.Volume, SettingsManager.SoundsVolume * CricketSoundVolume2, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_cricketSound2.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_cricketSound2.Play();
		}
		else
		{
			m_cricketSound2.Pause();
		}
		m_cricketSound3.Volume = MathUtils.Lerp(m_cricketSound3.Volume, SettingsManager.SoundsVolume * CricketSoundVolume3, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_cricketSound3.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_cricketSound3.Play();
		}
		else
		{
			m_cricketSound3.Pause();
		}
		m_cricketSound4.Volume = MathUtils.Lerp(m_cricketSound4.Volume, SettingsManager.SoundsVolume * CricketSoundVolume4, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_cricketSound4.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_cricketSound4.Play();
		}
		else
		{
			m_cricketSound4.Pause();
		}
		m_electricHumSound.Volume = MathUtils.Lerp(m_electricHumSound.Volume, SettingsManager.SoundsVolume * ElectricHumSoundVolume, MathUtils.Saturate(3f * Time.FrameDuration));
		if (m_electricHumSound.Volume > 0.5f * AudioManager.MinAudibleVolume)
		{
			m_electricHumSound.Play();
		}
		else
		{
			m_electricHumSound.Pause();
		}
		ElectricHumSoundVolume = 0f;
		FireSoundVolume = 0f;
		WaterSoundVolume = 0f;
		MagmaSoundVolume = 0f;
		RiverCalmVolume = 0f;
		RiverFightVolume = 0f;
		RiverWhirlpoolVolume = 0f;
		OceanSurfVolume = 0f;
		OceanRoughVolume = 0f;
		OceanStormVolume = 0f;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_electricHumSound = SubsystemAudio.CreateSound("Audio/ElectricHum");
		m_electricHumSound.IsLooped = true;
		m_electricHumSound.Volume = 0f;
		m_fireSound = SubsystemAudio.CreateSound("Audio/Fire");
		m_fireSound.IsLooped = true;
		m_fireSound.Volume = 0f;
		m_waterSound = SubsystemAudio.CreateSound("Audio/Water");
		m_waterSound.IsLooped = true;
		m_waterSound.Volume = 0f;
		m_riverCalmSound = SubsystemAudio.CreateSound("Audio/Water/RiverCalm");
		m_riverCalmSound.IsLooped = true;
		m_riverCalmSound.Volume = 0f;
		m_riverFightSound = SubsystemAudio.CreateSound("Audio/Water/RiverFight");
		m_riverFightSound.IsLooped = true;
		m_riverFightSound.Volume = 0f;
		m_riverWhirlpoolSound = SubsystemAudio.CreateSound("Audio/Water/RiverWhirlpool");
		m_riverWhirlpoolSound.IsLooped = true;
		m_riverWhirlpoolSound.Volume = 0f;
		m_oceanSurfSound = SubsystemAudio.CreateSound("Audio/Water/OceanSurf");
		m_oceanSurfSound.IsLooped = true;
		m_oceanSurfSound.Volume = 0f;
		m_oceanRoughSound = SubsystemAudio.CreateSound("Audio/Water/OceanRough");
		m_oceanRoughSound.IsLooped = true;
		m_oceanRoughSound.Volume = 0f;
		m_oceanStormSound = SubsystemAudio.CreateSound("Audio/Water/OceanStorm");
		m_oceanStormSound.IsLooped = true;
		m_oceanStormSound.Volume = 0f;
		m_magmaSound = SubsystemAudio.CreateSound("Audio/Magma");
		m_magmaSound.IsLooped = true;
		m_magmaSound.Volume = 0f;
		m_blazeSound = SubsystemAudio.CreateSound("Audio/Blaze");
		m_blazeSound.IsLooped = true;
		m_blazeSound.Volume = 0f;
		m_clayfurnaceSound = SubsystemAudio.CreateSound("Audio/ClayFurnace");
		m_clayfurnaceSound.IsLooped = true;
		m_clayfurnaceSound.Volume = 0f;
		m_brickfurnaceSound = SubsystemAudio.CreateSound("Audio/BrickFurnace");
		m_brickfurnaceSound.IsLooped = true;
		m_brickfurnaceSound.Volume = 0f;
		m_cricketSound1 = SubsystemAudio.CreateSound("Audio/Cricket/Cricket1");
		m_cricketSound1.IsLooped = true;
		m_cricketSound1.Volume = 0f;
		m_cricketSound2 = SubsystemAudio.CreateSound("Audio/Cricket/Cricket2");
		m_cricketSound2.IsLooped = true;
		m_cricketSound2.Volume = 0f;
		m_cricketSound3 = SubsystemAudio.CreateSound("Audio/Cricket/Cricket3");
		m_cricketSound3.IsLooped = true;
		m_cricketSound3.Volume = 0f;
		m_cricketSound4 = SubsystemAudio.CreateSound("Audio/Cricket/Cricket4");
		m_cricketSound4.IsLooped = true;
		m_cricketSound4.Volume = 0f;
		m_fermentationSound = SubsystemAudio.CreateSound("Audio/Fermentation");
		m_fermentationSound.IsLooped = true;
		m_fermentationSound.Volume = 0f;
	}
}
