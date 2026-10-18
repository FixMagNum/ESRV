using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemTimeOfDay : Subsystem
{
	public SubsystemGameInfo m_subsystemGameInfo;

	public bool TimeOfDayEnabled;

	public float DayDuration;

	private double OffsetFix;

	public float SunriseOffset;

	public float DayOffset;

	public float SunsetOffset;

	public float NightOffset;

	public float TimeOfDay
	{
		get
		{
			if (!TimeOfDayEnabled)
			{
				return SunsetOffset;
			}
			return m_subsystemGameInfo.WorldSettings.TimeOfDayMode switch
			{
				TimeOfDayMode.Changing => CalculateTimeOfDay(), 
				TimeOfDayMode.Day => DayOffset, 
				TimeOfDayMode.Night => NightOffset, 
				TimeOfDayMode.Sunrise => SunriseOffset, 
				TimeOfDayMode.Sunset => SunsetOffset, 
				_ => SunsetOffset, 
			};
		}
	}

	public double Day => CalculateDay(m_subsystemGameInfo.TotalElapsedGameTime);

	public double TimeOfDayOffset { get; set; }

	public static string GetTimeOfDayText(float timeOfDay)
	{
		if (timeOfDay >= 0f && timeOfDay < 0.25f)
		{
			return LanguageControl.Get(ComponentGui.fName, 18);
		}
		if (timeOfDay >= 0.25f && timeOfDay < 0.5f)
		{
			return LanguageControl.Get(ComponentGui.fName, 15);
		}
		if (timeOfDay >= 0.5f && timeOfDay < 0.75f)
		{
			return LanguageControl.Get(ComponentGui.fName, 16);
		}
		return LanguageControl.Get(ComponentGui.fName, 17);
	}

	public double CalculateDay(double totalElapsedGameTime)
	{
		return (totalElapsedGameTime + (TimeOfDayOffset + OffsetFix) * (double)DayDuration) / (double)DayDuration;
	}

	public float CalculateTimeOfDay()
	{
		return (float)MathUtils.Remainder(m_subsystemGameInfo.TotalElapsedGameTime + (TimeOfDayOffset + OffsetFix) * (double)DayDuration, DayDuration) / DayDuration;
	}

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		TimeOfDayOffset = valuesDictionary.GetValue<double>("TimeOfDayOffset");
		float dayCycleDuration = m_subsystemGameInfo.WorldSettings.DayCycleDuration;
		if (dayCycleDuration <= 0f)
		{
			DayDuration = 6400f;
		}
		else
		{
			DayDuration = dayCycleDuration;
		}
	}

	public override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("TimeOfDayOffset", TimeOfDayOffset);
	}

	public SubsystemTimeOfDay()
	{
		TimeOfDayEnabled = true;
		DayDuration = 6400f;
		OffsetFix = 0.30000001192092896;
		SunriseOffset = 0.25f;
		DayOffset = 0.5f;
		SunsetOffset = 0.75f;
		NightOffset = 1f;
	}
}
