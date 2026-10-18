using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemWindParticles : Subsystem, IUpdateable
{
	private SubsystemParticles m_particles;

	private SubsystemTerrain m_terrain;

	private readonly Random m_random = new Random();

	private readonly Dictionary<GameWidget, Vector2> m_lastCenterByWidget;

	private SubsystemWeather m_weather;

	private SubsystemGameWidgets m_gameWidgets;

	private readonly Dictionary<GameWidget, Dictionary<Point2, WindShaftParticleSystem>> m_activeShafts = new Dictionary<GameWidget, Dictionary<Point2, WindShaftParticleSystem>>();

	private readonly List<Point2> m_toRemovePoints;

	private static readonly int[] s_moderateSlots;

	private static readonly int[] s_snowSlots;

	private static readonly int[] s_desertSlots;

	private static readonly int[] s_highSlots;

	private readonly Dictionary<GameWidget, Dictionary<Point2, CycloneWindStreakParticleSystem>> m_activeStreaks;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_particles = Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		m_terrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_weather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_gameWidgets = Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
	}

	public void Update(float dt)
	{
		if (m_weather == null || m_gameWidgets == null)
		{
			return;
		}
		if (!SettingsManager.DisplayWindParticles || SettingsManager.StagePrecipitation >= 1f)
		{
			ClearAllShafts();
			return;
		}
		float currentWindStrength = m_weather.CurrentWindStrength;
		float x = 0f;
		if (currentWindStrength > 0.1f)
		{
			if (currentWindStrength <= 8f)
			{
				x = MathUtils.Saturate(currentWindStrength / 8f) * 0.35f;
			}
			else
			{
				x = ((!(currentWindStrength <= 12f)) ? (0.7f + MathUtils.Saturate((currentWindStrength - 12f) / 4f) * 0.3f) : (0.35f + MathUtils.Saturate((currentWindStrength - 8f) / 4f) * 0.35f));
			}
		}
		x = MathUtils.Saturate(x);
		float f = x * x;
		ReadOnlyList<GameWidget> gameWidgets = m_gameWidgets.GameWidgets;
		if (gameWidgets.Count == 0)
		{
			return;
		}
		if (x <= 0f)
		{
			foreach (GameWidget item in gameWidgets)
			{
				if (item != null && m_activeStreaks.TryGetValue(item, out var value))
				{
					ClearWidgetStreaks(item, value);
				}
			}
			return;
		}
		MathUtils.Lerp(0.01f, 0.15f, f);
		foreach (GameWidget item2 in gameWidgets)
		{
			if (item2 == null)
			{
				continue;
			}
			Camera activeCamera = item2.ActiveCamera;
			if (activeCamera == null)
			{
				continue;
			}
			if (m_activeShafts.TryGetValue(item2, out var value2) && value2.Count > 0)
			{
				ClearWidgetShafts(item2, value2);
			}
			if (!m_activeStreaks.TryGetValue(item2, out var value3))
			{
				value3 = new Dictionary<Point2, CycloneWindStreakParticleSystem>();
				m_activeStreaks.Add(item2, value3);
			}
			int num = ((SettingsManager.VisibilityRange > 128) ? 6 : ((SettingsManager.VisibilityRange <= 64) ? 4 : 5)) + 12;
			Vector2 vector = new Vector2(activeCamera.ViewPosition.X, activeCamera.ViewPosition.Z);
			if (currentWindStrength > 0.1f)
			{
				Vector2 vector2 = m_weather.CurrentWindVector;
				if (vector2.LengthSquared() < 1E-06f)
				{
					vector2 = Vector2.UnitY;
				}
				float num2 = 10f;
				float num3 = MathUtils.Lerp(8f, 8f + num2, x);
				vector -= vector2 * ((float)num + num3);
			}
			if (m_lastCenterByWidget.TryGetValue(item2, out var value4) && !(Vector2.DistanceSquared(value4, vector) > 1f))
			{
				continue;
			}
			m_lastCenterByWidget[item2] = vector;
			Point2 point = Terrain.ToCell(vector);
			Vector2 vector3 = m_weather.CurrentWindVector;
			if (vector3.LengthSquared() < 1E-06f)
			{
				vector3 = Vector2.UnitY;
			}
			Vector2 vector4 = new Vector2(0f - vector3.Y, vector3.X);
			m_toRemovePoints.Clear();
			foreach (KeyValuePair<Point2, CycloneWindStreakParticleSystem> item3 in value3)
			{
				float num4 = (float)item3.Key.X + 0.5f - vector.X;
				float num5 = (float)item3.Key.Y + 0.5f - vector.Y;
				float num6 = Math.Abs(num4 * vector3.X + num5 * vector3.Y);
				float num7 = Math.Abs(num4 * vector4.X + num5 * vector4.Y);
				if (num6 > 4f || num7 > 16f)
				{
					m_toRemovePoints.Add(item3.Key);
				}
			}
			foreach (Point2 toRemovePoint in m_toRemovePoints)
			{
				CycloneWindStreakParticleSystem particleSystem = value3[toRemovePoint];
				if (m_particles.ContainsParticleSystem(particleSystem))
				{
					m_particles.RemoveParticleSystem(particleSystem);
				}
				value3.Remove(toRemovePoint);
			}
			m_toRemovePoints.Clear();
			float num8 = MathUtils.Lerp(0.1f, 1f, f);
			for (int i = point.X - num; i <= point.X + num; i++)
			{
				for (int j = point.Y - num; j <= point.Y + num; j++)
				{
					float num9 = (float)i + 0.5f - vector.X;
					float num10 = (float)j + 0.5f - vector.Y;
					float num11 = Math.Abs(num9 * vector3.X + num10 * vector3.Y);
					float num12 = Math.Abs(num9 * vector4.X + num10 * vector4.Y);
					if (num11 <= 3f && num12 <= 15f)
					{
						Point2 point2 = new Point2(i, j);
						if (!value3.ContainsKey(point2) && (float)Math.Abs((point2.X * 536870911 + point2.Y * 268435459) % 1000) / 1000f <= num8)
						{
							CycloneWindStreakParticleSystem cycloneWindStreakParticleSystem = new CycloneWindStreakParticleSystem(item2, m_weather, m_random, point2);
							m_particles.AddParticleSystem(cycloneWindStreakParticleSystem);
							value3.Add(point2, cycloneWindStreakParticleSystem);
						}
					}
				}
			}
		}
	}

	public SubsystemWindParticles()
	{
		m_lastCenterByWidget = new Dictionary<GameWidget, Vector2>();
		m_toRemovePoints = new List<Point2>();
		m_activeStreaks = new Dictionary<GameWidget, Dictionary<Point2, CycloneWindStreakParticleSystem>>();
	}

	private void ClearAllShafts()
	{
		foreach (KeyValuePair<GameWidget, Dictionary<Point2, WindShaftParticleSystem>> activeShaft in m_activeShafts)
		{
			ClearWidgetShafts(activeShaft.Key, activeShaft.Value);
		}
		foreach (KeyValuePair<GameWidget, Dictionary<Point2, CycloneWindStreakParticleSystem>> activeStreak in m_activeStreaks)
		{
			ClearWidgetStreaks(activeStreak.Key, activeStreak.Value);
		}
		m_activeShafts.Clear();
		m_activeStreaks.Clear();
		m_lastCenterByWidget.Clear();
	}

	private void ClearWidgetShafts(GameWidget widget, Dictionary<Point2, WindShaftParticleSystem> active)
	{
		foreach (WindShaftParticleSystem value in active.Values)
		{
			if (m_particles.ContainsParticleSystem(value))
			{
				m_particles.RemoveParticleSystem(value);
			}
		}
		active.Clear();
		m_lastCenterByWidget.Remove(widget);
	}

	private int[] GetWindTextureSlots(int x, int z, float cameraY)
	{
		if (cameraY > 85f)
		{
			return s_highSlots;
		}
		int shaftValue = m_terrain.Terrain.GetShaftValue(x, z);
		int seasonalTemperature = m_terrain.Terrain.GetSeasonalTemperature(shaftValue);
		int seasonalHumidity = m_terrain.Terrain.GetSeasonalHumidity(shaftValue);
		int y = Terrain.ExtractTopHeight(shaftValue);
		int snowTemperatureThreshold = m_weather.GetSnowTemperatureThreshold();
		if (SubsystemWeather.IsPlaceFrozenExtended(seasonalTemperature, y, snowTemperatureThreshold))
		{
			return s_snowSlots;
		}
		if (seasonalTemperature > 8 && seasonalHumidity < 8)
		{
			return s_desertSlots;
		}
		return s_moderateSlots;
	}

	static SubsystemWindParticles()
	{
		s_moderateSlots = new int[4] { 0, 1, 2, 3 };
		s_snowSlots = new int[4] { 4, 5, 6, 7 };
		s_desertSlots = new int[4] { 8, 9, 10, 11 };
		s_highSlots = new int[4] { 12, 13, 14, 15 };
	}

	private void ClearWidgetStreaks(GameWidget widget, Dictionary<Point2, CycloneWindStreakParticleSystem> active)
	{
		foreach (CycloneWindStreakParticleSystem value in active.Values)
		{
			if (m_particles.ContainsParticleSystem(value))
			{
				m_particles.RemoveParticleSystem(value);
			}
		}
		active.Clear();
	}
}
