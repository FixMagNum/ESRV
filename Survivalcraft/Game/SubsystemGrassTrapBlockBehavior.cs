using System.Collections.Generic;
using Engine;

namespace Game;

public class SubsystemGrassTrapBlockBehavior : SubsystemBlockBehavior, IUpdateable
{
	public class TrapValue
	{
		public float Damage;
	}

	public Dictionary<Point3, TrapValue> m_trapValues = new Dictionary<Point3, TrapValue>();

	public List<Point3> m_toRemove = new List<Point3>();

	public override int[] HandledBlocks => new int[1] { 87 };

	public UpdateOrder UpdateOrder => UpdateOrder.Default;

	public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
	{
		if (cellFace.Face == 4 && componentBody.Mass > 20f)
		{
			Point3 key = new Point3(cellFace.X, cellFace.Y, cellFace.Z);
			if (!m_trapValues.TryGetValue(key, out var value))
			{
				value = new TrapValue();
				m_trapValues.Add(key, value);
			}
			value.Damage += 0f - velocity;
		}
	}

	public void Update(float dt)
	{
		foreach (KeyValuePair<Point3, TrapValue> parameterSetType in m_trapValues)
		{
			if (parameterSetType.Value.Damage > 1f)
			{
				for (int model = -1; model <= 1; model++)
				{
					for (int boneAbsoluteTransform = -1; boneAbsoluteTransform <= 1; boneAbsoluteTransform++)
					{
						if (MathUtils.Abs(model) + MathUtils.Abs(boneAbsoluteTransform) <= 1 && SubsystemTerrain.Terrain.GetCellContents(parameterSetType.Key.X + model, parameterSetType.Key.Y, parameterSetType.Key.Z + boneAbsoluteTransform) == 87)
						{
							SubsystemTerrain.DestroyCell(0, parameterSetType.Key.X + model, parameterSetType.Key.Y, parameterSetType.Key.Z + boneAbsoluteTransform, 0, noDrop: false, noParticleSystem: false);
						}
					}
				}
				parameterSetType.Value.Damage = 0f;
			}
			else
			{
				parameterSetType.Value.Damage -= 0.5f * dt;
			}
			if (parameterSetType.Value.Damage <= 0f)
			{
				m_toRemove.Add(parameterSetType.Key);
			}
		}
		foreach (Point3 item in m_toRemove)
		{
			m_trapValues.Remove(item);
		}
		m_toRemove.Clear();
	}
}
