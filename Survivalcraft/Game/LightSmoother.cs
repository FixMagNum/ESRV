using System.Collections.Generic;
using Engine;
using GameEntitySystem;

namespace Game;

public class LightSmoother
{
	private float m_smoothedLight;

	private float m_rawLight;

	private float m_lightStableTime;

	public float SmoothedLight => m_smoothedLight;

	public int SmoothedLightInt => (int)(m_smoothedLight * 15f);

	public void Update(float dt, Vector3 position, SubsystemTerrain terrain, Project project)
	{
		int x = Terrain.ToCell(position.X);
		int y = Terrain.ToCell(position.Y);
		int z = Terrain.ToCell(position.Z);
		if (!terrain.Terrain.IsCellValid(x, y, z))
		{
			return;
		}
		int cellLight = terrain.Terrain.GetCellLight(x, y, z);
		int x2 = CalculateMinersLight(position, project);
		float num = MathUtils.Saturate((float)MathUtils.Max(cellLight, x2) / 15f);
		if (num >= m_rawLight)
		{
			m_smoothedLight = num;
			m_rawLight = num;
			m_lightStableTime = 0f;
			return;
		}
		m_lightStableTime += dt;
		if (m_lightStableTime > 0.5f)
		{
			m_smoothedLight = num;
			m_rawLight = num;
			m_lightStableTime = 0f;
		}
	}

	public int CalculateMinersLight(Vector3 position, Project project)
	{
		int currentMax = 0;
		SubsystemMiningLightBehavior subsystemMiningLightBehavior = project.FindSubsystem<SubsystemMiningLightBehavior>(throwOnError: false);
		if (subsystemMiningLightBehavior != null)
		{
			ProcessLights(position, subsystemMiningLightBehavior.ActiveLights, ref currentMax);
			ProcessLights(position, subsystemMiningLightBehavior.ActiveHeadLights, ref currentMax);
		}
		return currentMax;
	}

	public void ProcessLights<T>(Vector3 pos, Dictionary<T, SubsystemMiningLightBehavior.MiningLightData> lights, ref int currentMax)
	{
		foreach (SubsystemMiningLightBehavior.MiningLightData value in lights.Values)
		{
			float num = Vector3.DistanceSquared(pos, value.Position);
			float num2 = value.Intensity * 2f;
			if (num < num2 * num2)
			{
				float num3 = MathUtils.Sqrt(num);
				int num4 = (int)((1f - num3 / num2) * 15f);
				if (num4 > currentMax)
				{
					currentMax = num4;
				}
			}
		}
	}
}
