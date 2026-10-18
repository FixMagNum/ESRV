using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemFluidDynamics : Subsystem, IUpdateable
{
	public enum FluidTemperatureState
	{
		Freezing,
		Cold,
		Normal,
		Hot,
		Boiling
	}

	public SubsystemWeather m_subsystemWeather;

	public SubsystemSky m_subsystemSky;

	public SubsystemTime m_subsystemTime;

	public SubsystemTerrain m_subsystemTerrain;

	public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
		m_subsystemSky = Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		m_subsystemTime = Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
		m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
	}

	public void Update(float dt)
	{
	}

	public float GetTemperatureAtPoint(Point3 point)
	{
		int x = point.X;
		int num = point.Y;
		int z = point.Z;
		int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num, z));
		Block block = BlocksManager.Blocks[num2];
		if (num2 != 0 && !block.IsCollidable)
		{
			int num3 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(x, num + 1, z));
			if (!BlocksManager.Blocks[num3].IsCollidable)
			{
				num++;
			}
		}
		m_subsystemMetersBlockBehavior.CalculateTemperature(x, num, z, 0f, 0f, out var temperature, out var _);
		return temperature;
	}

	public PrecipitationType? GetPrecipitationAtPoint(Point3 point)
	{
		if (m_subsystemWeather.GlobalPrecipitationIntensity <= 0.01f)
		{
			return null;
		}
		PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(point.X, point.Z);
		if (precipitationShaftInfo.Intensity <= 0.01f)
		{
			return null;
		}
		if (point.Y >= precipitationShaftInfo.YLimit)
		{
			return null;
		}
		return precipitationShaftInfo.Type;
	}

	public bool IsExposedToSky(Point3 point)
	{
		int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
		return point.Y >= topHeight;
	}

	public FluidTemperatureState GetFluidTemperatureState(float temperature)
	{
		if (temperature <= 0f)
		{
			return FluidTemperatureState.Freezing;
		}
		if (temperature < 8f)
		{
			return FluidTemperatureState.Cold;
		}
		if (temperature < 25f)
		{
			return FluidTemperatureState.Normal;
		}
		if (temperature < 100f)
		{
			return FluidTemperatureState.Hot;
		}
		return FluidTemperatureState.Boiling;
	}
}
