using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Engine;
using Engine.Serialization;
using XmlUtilities;

namespace Game;

public static class SettingsManager
{
	public enum VegetationRenderMode
	{
		Full,
		HideDistant,
		OpaqueLow
	}

	public static float m_soundsVolume;

	public static float m_musicVolume;

	public static float m_brightness;

	public static ResolutionMode m_resolutionMode;

	public static WindowMode m_windowMode;

	public static Point2 m_resizableWindowPosition;

	public static Point2 m_resizableWindowSize;

	public static float BuildHoldTimerStart;

	public static float BuildHoldTimerInterval;

	[CompilerGenerated]
	private static int TranslationProviderState__BackingField;

	private static ThemeMode m_themeMode;

	public static float m_stagePrecipitation;

	private static bool m_nightVisionEnabled;

	public static int m_treeSwayBandHeight;

	private static VegetationRenderMode m_vegetationMode;

	public static readonly string CurrentModVersion;

	public static bool UsePrimaryMemoryBank { get; set; }

	public static bool AllowInitialIntro { get; set; }

	public static int ServerPort { get; set; }

	public static int BroadcastPort { get; set; }

	public static string OfflineToken { get; set; }

	public static float SoundsVolume
	{
		get
		{
			return m_soundsVolume;
		}
		set
		{
			m_soundsVolume = MathUtils.Saturate(value);
		}
	}

	public static float MusicVolume
	{
		get
		{
			return m_musicVolume;
		}
		set
		{
			m_musicVolume = MathUtils.Saturate(value);
		}
	}

	public static int VisibilityRange { get; set; }

	public static bool UseVr { get; set; }

	public static float UIScale { get; set; }

	public static ResolutionMode ResolutionMode
	{
		get
		{
			return m_resolutionMode;
		}
		set
		{
			if (value != m_resolutionMode)
			{
				m_resolutionMode = value;
				SettingChanged?.Invoke("ResolutionMode");
			}
		}
	}

	public static ViewAngleMode ViewAngleMode { get; set; }

	public static SkyRenderingMode SkyRenderingMode { get; set; }

	public static bool TerrainMipmapsEnabled { get; set; }

	public static bool ObjectsShadowsEnabled { get; set; }

	public static float Brightness
	{
		get
		{
			return m_brightness;
		}
		set
		{
			value = MathUtils.Clamp(value, 0f, 1f);
			if (value != m_brightness)
			{
				m_brightness = value;
				SettingChanged?.Invoke("Brightness");
			}
		}
	}

	public static int PresentationInterval { get; set; }

	public static bool ShowGuiInScreenshots { get; set; }

	public static bool ShowLogoInScreenshots { get; set; }

	public static ScreenshotSize ScreenshotSize { get; set; }

	public static WindowMode WindowMode
	{
		get
		{
			return m_windowMode;
		}
		set
		{
			if (value != m_windowMode)
			{
				switch (value)
				{
				case WindowMode.Borderless:
					m_resizableWindowSize = Window.Size;
					Window.Position = Point2.Zero;
					Window.Size = Window.ScreenSize;
					break;
				case WindowMode.Resizable:
					Window.Position = m_resizableWindowPosition;
					Window.Size = m_resizableWindowSize;
					break;
				}
				Window.WindowMode = value;
				m_windowMode = value;
			}
		}
	}

	public static GuiSize GuiSize { get; set; }

	public static bool HideMoveLookPads { get; set; }

	public static string BlocksTextureFileName { get; set; }

	public static MoveControlMode MoveControlMode { get; set; }

	public static LookControlMode LookControlMode { get; set; }

	public static bool LeftHandedLayout { get; set; }

	public static bool FlipVerticalAxis { get; set; }

	public static float MoveSensitivity { get; set; }

	public static float LookSensitivity { get; set; }

	public static float GamepadDeadZone { get; set; }

	public static float GamepadCursorSpeed { get; set; }

	public static float CreativeDigTime { get; set; }

	public static float CreativeReach { get; set; }

	public static float MinimumHoldDuration { get; set; }

	public static float MinimumDragDistance { get; set; }

	public static bool AutoJump { get; set; }

	public static bool HorizontalCreativeFlight { get; set; }

	public static string DropboxAccessToken { get; set; }

	public static string MotdUpdateUrl { get; set; }

	public static string MotdUpdateCheckUrl { get; set; }

	public static string CommunityAccessToken { get; set; }

	public static bool MotdUseBackupUrl { get; set; }

	public static double MotdUpdatePeriodHours { get; set; }

	public static DateTime MotdLastUpdateTime { get; set; }

	public static string MotdLastDownloadedData { get; set; }

	public static string UserId { get; set; }

	public static string LastLaunchedVersion { get; set; }

	public static CommunityContentMode CommunityContentMode { get; set; }

	public static bool MultithreadedTerrainUpdate { get; set; }

	public static bool UseReducedZRange { get; set; }

	public static bool EnableMod { get; set; }

	public static int IsolatedStorageMigrationCounter { get; set; }

	public static bool DisplayFpsCounter { get; set; }

	public static bool DisplayFpsRibbon { get; set; }

	public static int NewYearCelebrationLastYear { get; set; }

	public static ScreenLayout ScreenLayout1 { get; set; }

	public static ScreenLayout ScreenLayout2 { get; set; }

	public static ScreenLayout ScreenLayout3 { get; set; }

	public static ScreenLayout ScreenLayout4 { get; set; }

	public static bool UpsideDownLayout { get; set; }

	public static bool FullScreenMode
	{
		get
		{
			return Window.WindowMode == WindowMode.Fullscreen;
		}
		set
		{
			Window.WindowMode = (value ? WindowMode.Fullscreen : WindowMode.Resizable);
		}
	}

	public static bool DisplayLog { get; set; }

	public static string BulletinTime { get; set; }

	public static bool FirstPersonCameraEnabled { get; set; }

	public static int TranslationProviderState
	{
		[CompilerGenerated]
		get
		{
			return TranslationProviderState__BackingField;
		}
		[CompilerGenerated]
		set
		{
			TranslationProviderState__BackingField = value;
		}
	}

	public static bool FireParticlesInFurnaces { get; set; }

	public static bool FireParticlesFromLights { get; set; }

	public static bool FireParticlesFromCampfires { get; set; }

	public static bool VomitParticles { get; set; }

	public static bool CottonBlockParticles { get; set; }

	public static bool PuddleBlockParticles { get; set; }

	public static bool SmokeFromBothCampfires { get; set; }

	public static bool SimplifySmokePhysics { get; set; }

	public static int SmokeDensity { get; set; }

	public static bool BlockAnimationsEnabled { get; set; }

	public static bool RenderWeatherPrecipitationOutsideView { get; set; }

	public static bool RenderFog { get; set; }

	public static bool BlockBreakParticles { get; set; }

	public static ThemeMode ThemeMode
	{
		get
		{
			return m_themeMode;
		}
		set
		{
			if (value != m_themeMode)
			{
				m_themeMode = value;
				SettingChanged?.Invoke("ThemeMode");
			}
		}
	}

	public static float StagePrecipitation
	{
		get
		{
			return m_stagePrecipitation;
		}
		set
		{
			value = MathUtils.Clamp(value, 0f, 1f);
			if (value != m_stagePrecipitation)
			{
				m_stagePrecipitation = value;
				SettingChanged?.Invoke("StagePrecipitation");
			}
		}
	}

	public static bool RandomVegetationRotation { get; set; }

	public static bool RandomNaturalBlocksRotation { get; set; }

	public static bool ShowBlockHighlight { get; set; }

	public static bool BlockDebris3D { get; set; }

	public static bool NightVisionEnabled
	{
		get
		{
			bool cellValue = GameManager.WorldInfo != null && GameManager.WorldInfo.WorldSettings.GameMode == GameMode.Creative;
			if (m_nightVisionEnabled && !cellValue)
			{
				m_nightVisionEnabled = false;
				SettingChanged?.Invoke("NightVisionEnabled");
			}
			return m_nightVisionEnabled;
		}
		set
		{
			bool value2 = GameManager.WorldInfo != null && GameManager.WorldInfo.WorldSettings.GameMode == GameMode.Creative;
			if ((!value || value2) && m_nightVisionEnabled != value)
			{
				m_nightVisionEnabled = value;
				SettingChanged?.Invoke("NightVisionEnabled");
			}
		}
	}

	public static bool DisplayPaintParticles { get; set; }

	public static bool PrecisePrecipitationHits { get; set; }

	public static float UITransparent { get; set; }

	public static bool DisplayBloodParticles { get; set; }

	public static string PlayerName { get; set; }

	public static string OnlinePassword { get; set; }

	public static bool PlantWindAnimationsEnabled { get; set; }

	public static bool HeatAndUnderwaterBlurEnabled { get; set; }

	public static bool TreeSwayEnabled { get; set; }

	public static bool WaterWavesAnimationEnabled { get; set; }

	public static int ShadersRenderRange { get; set; }

	public static int TreeSwayBandHeight
	{
		get
		{
			return m_treeSwayBandHeight;
		}
		set
		{
			value = MathUtils.Clamp(value, 6, 50);
			if (value != m_treeSwayBandHeight)
			{
				m_treeSwayBandHeight = value;
				SettingChanged?.Invoke("TreeSwayBandHeight");
			}
		}
	}

	public static bool VisionDefectsEnabled { get; set; }

	public static bool DisableCycloneFlybyDebris { get; set; }

	public static bool DisableTornadoDust { get; set; }

	public static bool DisableTornadoDebris { get; set; }

	public static bool UiDimmingEnabled { get; set; }

	public static ButtonStyleMode ButtonStyleMode { get; set; }

	public static string KeyBindingsData { get; set; }

	public static VegetationRenderMode VegetationMode
	{
		get
		{
			return m_vegetationMode;
		}
		set
		{
			if (value != m_vegetationMode)
			{
				m_vegetationMode = value;
				SettingChanged?.Invoke("VegetationMode");
			}
		}
	}

	public static bool SkyLightUseParticles { get; set; }

	public static bool DisplayWindParticles { get; set; }

	public static bool BuildSpamEnabled { get; set; }

	public static bool NewPanoramaEnabled { get; set; }

	public static bool DebugMode { get; set; }

	public static int DistantChunksRange { get; set; }

	public static int DistantChunksDetailLevel { get; set; }

	public static Color CustomInterfaceColor { get; set; }

	public static bool LowResSignsTexture { get; set; }

	public static bool DisplayWaterParticles { get; set; }

	public static bool DisplayMagmaParticles { get; set; }

	public static event Action<string> SettingChanged;

	public static void Initialize()
	{
		VegetationMode = VegetationRenderMode.Full;
		ButtonStyleMode = ButtonStyleMode.WoodFramed;
		ThemeMode = ThemeMode.Dark;
		OfflineToken = Guid.NewGuid().ToString();
		ServerPort = 28887;
		BroadcastPort = 28888;
		CustomInterfaceColor = new Color(100, 150, 200, 255);
		DisplayLog = false;
		EnableMod = false;
		DebugMode = false;
		VisibilityRange = 192;
		DistantChunksRange = 0;
		DistantChunksDetailLevel = 0;
		PlayerName = string.Empty;
		LowResSignsTexture = false;
		OnlinePassword = string.Empty;
		m_resolutionMode = ResolutionMode.High;
		ViewAngleMode = ViewAngleMode.Normal;
		SkyRenderingMode = SkyRenderingMode.Full;
		TerrainMipmapsEnabled = true;
		ObjectsShadowsEnabled = true;
		m_soundsVolume = 0.5f;
		DisplayWaterParticles = true;
		DisplayMagmaParticles = true;
		m_musicVolume = 0.5f;
		m_brightness = 1f;
		m_stagePrecipitation = 0.3f;
		BuildHoldTimerStart = 0.2f;
		BuildHoldTimerInterval = 0.2f;
		PresentationInterval = 0;
		NightVisionEnabled = false;
		ShowGuiInScreenshots = false;
		ShowLogoInScreenshots = true;
		DisableCycloneFlybyDebris = true;
		DisableTornadoDust = true;
		DisableTornadoDebris = true;
		PlantWindAnimationsEnabled = true;
		HeatAndUnderwaterBlurEnabled = true;
		TreeSwayEnabled = true;
		WaterWavesAnimationEnabled = true;
		ShadersRenderRange = VisibilityRange;
		ScreenshotSize = ScreenshotSize.ScreenSize;
		MoveControlMode = MoveControlMode.Pad;
		HideMoveLookPads = false;
		AllowInitialIntro = true;
		FirstPersonCameraEnabled = true;
		UiDimmingEnabled = true;
		BlocksTextureFileName = string.Empty;
		LookControlMode = LookControlMode.EntireScreen;
		FlipVerticalAxis = false;
		UIScale = 0.8f;
		UITransparent = 0.3f;
		MoveSensitivity = 0.5f;
		LookSensitivity = 0.5f;
		GamepadDeadZone = 0.16f;
		GamepadCursorSpeed = 1f;
		CreativeDigTime = 0f;
		CreativeReach = 7.5f;
		MinimumHoldDuration = 0.5f;
		MinimumDragDistance = 10f;
		AutoJump = true;
		HorizontalCreativeFlight = false;
		BuildSpamEnabled = true;
		VisionDefectsEnabled = true;
		FireParticlesInFurnaces = true;
		ShowBlockHighlight = false;
		NewPanoramaEnabled = true;
		FireParticlesFromLights = true;
		FireParticlesFromCampfires = true;
		VomitParticles = true;
		CottonBlockParticles = true;
		PuddleBlockParticles = true;
		SmokeFromBothCampfires = true;
		SimplifySmokePhysics = false;
		SmokeDensity = 50;
		TreeSwayBandHeight = 40;
		DisplayPaintParticles = true;
		SkyLightUseParticles = false;
		DisplayBloodParticles = true;
		DisplayWindParticles = true;
		PrecisePrecipitationHits = true;
		BlockAnimationsEnabled = true;
		RenderWeatherPrecipitationOutsideView = true;
		RenderFog = true;
		BlockBreakParticles = true;
		BlockDebris3D = true;
		RandomVegetationRotation = true;
		RandomNaturalBlocksRotation = true;
		DropboxAccessToken = string.Empty;
		CommunityAccessToken = string.Empty;
		MotdUpdateUrl = "https://m.schub.top/com/motd?v={0}&l={1}";
		MotdUpdateCheckUrl = "https://m.schub.top/com/motd?v={0}&cmd=version_check&platform={1}&apiv={2}&l={3}";
		MotdUpdatePeriodHours = 12.0;
		MotdLastUpdateTime = DateTime.MinValue;
		MotdLastDownloadedData = string.Empty;
		UserId = string.Empty;
		LastLaunchedVersion = string.Empty;
		CommunityContentMode = CommunityContentMode.Normal;
		MultithreadedTerrainUpdate = true;
		NewYearCelebrationLastYear = 2015;
		ScreenLayout1 = ScreenLayout.Single;
		ScreenLayout2 = (((float)Window.ScreenSize.X / (float)Window.ScreenSize.Y > 1.3333334f) ? ScreenLayout.DoubleVertical : ScreenLayout.DoubleHorizontal);
		ScreenLayout3 = (((float)Window.ScreenSize.X / (float)Window.ScreenSize.Y > 1.3333334f) ? ScreenLayout.TripleVertical : ScreenLayout.TripleHorizontal);
		ScreenLayout4 = ScreenLayout.Quadruple;
		BulletinTime = string.Empty;
		HorizontalCreativeFlight = true;
		LoadSettings();
		try
		{
			if (string.IsNullOrEmpty(KeyBindingsData) || !KeyBindings.TryDeserialize(KeyBindingsData))
			{
				KeyBindingsData = KeyBindings.Serialize();
			}
		}
		catch (Exception ex)
		{
			Log.Warning("Failed to apply KeyBindingsData. " + ex.Message);
		}
		VersionsManager.CompareVersions(LastLaunchedVersion, "1.29");
		if (VersionsManager.CompareVersions(LastLaunchedVersion, "2.1") < 0)
		{
			MinimumDragDistance = 10f;
		}
		if (VersionsManager.CompareVersions(LastLaunchedVersion, "2.2") < 0)
		{
			if (Utilities.GetTotalAvailableMemory() < 524288000)
			{
				VisibilityRange = MathUtils.Min(64, VisibilityRange);
			}
			else if (Utilities.GetTotalAvailableMemory() < 1048576000)
			{
				VisibilityRange = MathUtils.Min(112, VisibilityRange);
			}
		}
		Window.Deactivated += () =>
		{
			SaveSettings();
		};
	}

	public static void LoadSettings()
	{
		try
		{
			if (!Storage.FileExists("app:/Settings.xml"))
			{
				return;
			}
			using (Stream stream = Storage.OpenFile("app:/Settings.xml", OpenFileMode.Read))
			{
				ModsManager.DisabledMods.Clear();
				XElement item = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
				ModsManager.LoadSettings(item);
				foreach (XElement value in item.Elements())
				{
					string name = "<unknown>";
					try
					{
						if (value.Name.LocalName == "Setting")
						{
							name = XmlUtils.GetAttributeValue<string>(value, "Name");
							string item2 = XmlUtils.GetAttributeValue<string>(value, "Value");
							PropertyInfo key2 = typeof(SettingsManager).GetRuntimeProperties().FirstOrDefault((PropertyInfo pi) => pi.Name == name && pi.GetMethod.IsStatic && pi.GetMethod.IsPublic && pi.SetMethod.IsPublic);
							if (key2 != null)
							{
								object value2 = HumanReadableConverter.ConvertFromString(key2.PropertyType, item2);
								key2.SetValue(null, value2, null);
							}
						}
						else
						{
							if (!(value.Name.LocalName == "DisableMods"))
							{
								continue;
							}
							foreach (XElement item4 in value.Elements())
							{
								ModInfo item3 = new ModInfo
								{
									PackageName = item4.Attribute("PackageName").Value,
									Version = item4.Attribute("Version").Value
								};
								ModsManager.DisabledMods.Add(item3);
							}
							continue;
						}
					}
					catch (Exception ex)
					{
						Log.Warning("Setting \"" + name + "\" could not be loaded. Reason: " + ex.Message);
					}
				}
				BuildHoldTimerStart = XmlUtils.GetAttributeValue(item, "BuildHoldTimerStart", BuildHoldTimerStart);
				BuildHoldTimerInterval = XmlUtils.GetAttributeValue(item, "BuildHoldTimerInterval", BuildHoldTimerInterval);
			}
			Log.Information("Loaded settings.");
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser("Loading settings failed.", e);
		}
	}

	public static void SaveSettings()
	{
		try
		{
			XElement valuesDictionary2 = new XElement("Settings");
			foreach (PropertyInfo item in from pi in typeof(SettingsManager).GetRuntimeProperties()
				where pi.GetMethod.IsStatic && pi.GetMethod.IsPublic && pi.SetMethod.IsPublic
				select pi)
			{
				try
				{
					string value = HumanReadableConverter.ConvertToString(item.GetValue(null, null));
					XElement node = XmlUtils.AddElement(valuesDictionary2, "Setting");
					XmlUtils.SetAttributeValue(node, "Name", item.Name);
					XmlUtils.SetAttributeValue(node, "Value", value);
				}
				catch (Exception ex)
				{
					Log.Warning("Setting \"" + item.Name + "\" could not be saved. Reason: " + ex.Message);
				}
			}
			XmlUtils.SetAttributeValue(valuesDictionary2, "BuildHoldTimerStart", BuildHoldTimerStart.ToString());
			XmlUtils.SetAttributeValue(valuesDictionary2, "BuildHoldTimerInterval", BuildHoldTimerInterval.ToString());
			XElement valuesDictionary3 = new XElement("DisableMods");
			XElement xElement = new XElement("ModSettings");
			foreach (ModEntity item2 in ModsManager.ModListAll)
			{
				if (ModsManager.DisabledMods.Contains(item2.modInfo))
				{
					XElement xElement2 = new XElement("Mod");
					xElement2.SetAttributeValue("PackageName", item2.modInfo.PackageName);
					xElement2.SetAttributeValue("Version", item2.modInfo.Version);
					valuesDictionary3.Add(xElement2);
				}
			}
			valuesDictionary2.Add(valuesDictionary3);
			ModsManager.SaveSettings(valuesDictionary2);
			ModsManager.SaveModSettings(xElement);
			using (Stream stream = Storage.OpenFile("app:/Settings.xml", OpenFileMode.Create))
			{
				XmlUtils.SaveXmlToStream(valuesDictionary2, stream, null, throwOnError: true);
			}
			using (Stream stream2 = Storage.OpenFile("app:/ModSettings.xml", OpenFileMode.Create))
			{
				XmlUtils.SaveXmlToStream(xElement, stream2, null, throwOnError: true);
			}
			Log.Information("Saved settings");
		}
		catch (Exception e)
		{
			ExceptionManager.ReportExceptionToUser("Saving settings failed.", e);
		}
	}

	static SettingsManager()
	{
		TranslationProviderState__BackingField = 0;
		BuildHoldTimerStart = 0.2f;
		BuildHoldTimerInterval = 0.2f;
		CurrentModVersion = "1.4.2.5";
	}
}
